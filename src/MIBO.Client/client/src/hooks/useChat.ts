import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import type { Conversation, Message } from "@/types/chat";
import type { UiActionState, UiDataSourceState, UiFieldHints, UiV1 } from "@/types/ui.ts";
import { endpoints } from "@/axios/endpoints.ts";
import { uidStr } from "@/_mock/conversations.ts";
import { useAxios } from "@/axios/hooks";
import {
    buildUiFromAgentResponse,
    type AgentFinalResponse,
    executeSandboxAction,
    querySandboxDataSource,
} from "@/utils/agent-chat";

type ChatModel = "AI Agent" | "AI Agent Pro";

type AgentStreamEvent = {
    type: "session" | "status" | "chunk" | "done" | "error";
    content?: string;
    session_id?: string;
};

type ConversationSummaryDto = {
    conversationId: string;
    userId: string;
    title: string;
    createdAt: string;
    updatedAt: string;
    lastMessageAt?: string | null;
    lastMessagePreview?: string | null;
    messageCount: number;
};

type ConversationMessageDto = {
    messageId: string;
    conversationId: string;
    userId: string;
    role: "user" | "assistant" | "system";
    text: string;
    uiV1?: UiV1 | null;
    assistantPayload?: AgentFinalResponse | null;
    correlationId?: string;
    createdAt: string;
};

type ConversationDetailsDto = {
    conversation: ConversationSummaryDto;
    messages: ConversationMessageDto[];
};

type CreateConversationDto = {
    title?: string;
};

function isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === "object" && value !== null && !Array.isArray(value);
}

function getSourceRegistry(ui: UiV1 | null | undefined): Record<string, UiDataSourceState> {
    const registry = ui?.meta?.sourceRegistry;
    return isRecord(registry) ? (registry as Record<string, UiDataSourceState>) : {};
}

function getActionRegistry(ui: UiV1 | null | undefined): Record<string, UiActionState> {
    const registry = ui?.meta?.actionRegistry;
    return isRecord(registry) ? (registry as Record<string, UiActionState>) : {};
}

function getFetchedAtMillis(value: string | null | undefined): number {
    if (!value) {
        return Date.now();
    }

    const parsed = Date.parse(value);
    return Number.isFinite(parsed) ? parsed : Date.now();
}

function patchUiDataSourceResult(
    ui: UiV1,
    sourceKeys: string[],
    result: { data?: Record<string, unknown>; fieldHints?: UiFieldHints | null; fetchedAtUtc?: string | null },
    requestArgs: Record<string, unknown>
): UiV1 {
    if (!sourceKeys.length || !result.data) {
        return ui;
    }

    const nextSourceRegistry = { ...getSourceRegistry(ui) };
    const nextData = { ...(ui.data ?? {}) };
    const fetchedAt = getFetchedAtMillis(result.fetchedAtUtc);

    for (const sourceKey of sourceKeys) {
        nextData[sourceKey] = result.data;
        const current = nextSourceRegistry[sourceKey];
        if (current) {
            nextSourceRegistry[sourceKey] = {
                ...current,
                lastArgs: requestArgs,
                fieldHints: result.fieldHints ?? current.fieldHints ?? null,
                lastFetchedAt: fetchedAt,
            };
        }
    }

    return {
        ...ui,
        data: nextData,
        meta: {
            ...(ui.meta ?? {}),
            sourceRegistry: nextSourceRegistry,
            dataSources: nextSourceRegistry,
        },
    };
}

function parseTimestamp(value: string | number | null | undefined): number {
    if (typeof value === "number" && Number.isFinite(value)) {
        return value;
    }

    if (typeof value === "string") {
        const parsed = Date.parse(value);
        if (Number.isFinite(parsed)) {
            return parsed;
        }
    }

    return Date.now();
}

function parseSseEvent(rawEvent: string): AgentStreamEvent | null {
    const dataLines = rawEvent
        .split("\n")
        .filter((line) => line.startsWith("data:"))
        .map((line) => (line.startsWith("data: ") ? line.slice(6) : line.slice(5)).trimStart());

    if (dataLines.length === 0) {
        return null;
    }

    return JSON.parse(dataLines.join("\n")) as AgentStreamEvent;
}

function updateLatestAssistantMessage(messages: Message[], update: (message: Message) => Message): Message[] {
    const nextMessages = [...messages];
    for (let index = nextMessages.length - 1; index >= 0; index -= 1) {
        if (nextMessages[index].role !== "assistant") {
            continue;
        }

        nextMessages[index] = update(nextMessages[index]);
        break;
    }
    return nextMessages;
}

function mapMessage(dto: ConversationMessageDto): Message {
    const assistantPayload = isRecord(dto.assistantPayload) ? (dto.assistantPayload as AgentFinalResponse) : null;
    const derivedUi =
        dto.uiV1
        ?? (assistantPayload ? buildUiFromAgentResponse(assistantPayload) : null);

    return {
        id: dto.messageId || uidStr(),
        role: dto.role,
        content: dto.text ?? "",
        createdAt: parseTimestamp(dto.createdAt),
        uiV1: derivedUi,
    };
}

function getLatestAssistantUi(messages: Message[]): UiV1 | null {
    return (
        [...messages]
            .reverse()
            .find((message) => message.role === "assistant" && message.uiV1)?.uiV1
        ?? null
    );
}

function mapSummaryToConversation(summary: ConversationSummaryDto, existing?: Conversation | null): Conversation {
    const nextMessages = existing?.messages ?? [];
    const nextUi = existing?.uiV1 ?? getLatestAssistantUi(nextMessages);

    return {
        id: summary.conversationId,
        sessionId: summary.conversationId,
        title: summary.title,
        messages: nextMessages,
        updatedAt: parseTimestamp(summary.updatedAt),
        uiV1: nextUi,
    };
}

function mapDetailsToConversation(details: ConversationDetailsDto): Conversation {
    const messages = Array.isArray(details.messages) ? details.messages.map(mapMessage) : [];
    return {
        id: details.conversation.conversationId,
        sessionId: details.conversation.conversationId,
        title: details.conversation.title,
        messages,
        updatedAt: parseTimestamp(details.conversation.updatedAt),
        uiV1: getLatestAssistantUi(messages),
    };
}

function mergeConversationSummaries(summaries: ConversationSummaryDto[], current: Conversation[]): Conversation[] {
    const currentMap = new Map(current.map((conversation) => [conversation.id, conversation]));
    return summaries.map((summary) => mapSummaryToConversation(summary, currentMap.get(summary.conversationId) ?? null));
}

export function useChat() {
    const { api } = useAxios();

    const [conversations, setConversations] = useState<Conversation[]>([]);
    const [activeId, setActiveId] = useState("");
    const [model, setModel] = useState<ChatModel>("AI Agent");
    const [isTyping, setIsTyping] = useState(false);

    const listRef = useRef<HTMLDivElement>(null);
    const abortRef = useRef<AbortController | null>(null);
    const refreshedSourceRunsRef = useRef<Set<string>>(new Set());
    const loadedConversationIdsRef = useRef<Set<string>>(new Set());
    const loadingConversationIdsRef = useRef<Set<string>>(new Set());

    const active = useMemo(
        () => conversations.find((conversation) => conversation.id === activeId),
        [conversations, activeId]
    );

    const updateConversation = useCallback(
        (conversationId: string, updater: (conversation: Conversation) => Conversation) => {
            setConversations((previous) => {
                const index = previous.findIndex((conversation) => conversation.id === conversationId);
                if (index === -1) {
                    return previous;
                }

                const updated = updater(previous[index]);
                return [
                    updated,
                    ...previous.slice(0, index),
                    ...previous.slice(index + 1),
                ];
            });
        },
        []
    );

    const loadConversationDetail = useCallback(
        async (conversationId: string) => {
            if (!conversationId || loadedConversationIdsRef.current.has(conversationId) || loadingConversationIdsRef.current.has(conversationId)) {
                return;
            }

            loadingConversationIdsRef.current.add(conversationId);

            try {
                const details = await api.get<ConversationDetailsDto>(
                    endpoints.conversations.detail(conversationId)
                );

                if (loadedConversationIdsRef.current.has(conversationId)) {
                    return;
                }

                loadedConversationIdsRef.current.add(conversationId);
                setConversations((previous) => {
                    const hydrated = mapDetailsToConversation(details);
                    const index = previous.findIndex((conversation) => conversation.id === hydrated.id);
                    if (index === -1) {
                        return [hydrated, ...previous];
                    }

                    return [
                        hydrated,
                        ...previous.slice(0, index),
                        ...previous.slice(index + 1),
                    ];
                });
            } finally {
                loadingConversationIdsRef.current.delete(conversationId);
            }
        },
        [api]
    );

    useEffect(() => {
        loadedConversationIdsRef.current.clear();
        loadingConversationIdsRef.current.clear();
        setConversations([]);
        setActiveId("");

        let cancelled = false;

        void (async () => {
            try {
                const summaries = await api.get<ConversationSummaryDto[]>(
                    endpoints.conversations.list
                );

                if (cancelled) {
                    return;
                }

                setConversations((previous) => mergeConversationSummaries(Array.isArray(summaries) ? summaries : [], previous));
            } catch {
                if (!cancelled) {
                    setConversations([]);
                }
            }
        })();

        return () => {
            cancelled = true;
        };
    }, [api]);

    useEffect(() => {
        if (activeId && conversations.some((conversation) => conversation.id === activeId)) {
            return;
        }

        setActiveId(conversations[0]?.id ?? "");
    }, [activeId, conversations]);

    useEffect(() => {
        if (!activeId) {
            return;
        }

        void loadConversationDetail(activeId).catch(() => undefined);
    }, [activeId, loadConversationDetail]);

    const newChat = useCallback(async () => {
        const created = await api.post<ConversationSummaryDto>(
            endpoints.conversations.list,
            {} satisfies CreateConversationDto
        );

        const conversation = mapSummaryToConversation(created);
        loadedConversationIdsRef.current.add(conversation.id);

        setConversations((previous) => [
            conversation,
            ...previous.filter((item) => item.id !== conversation.id),
        ]);
        setActiveId(conversation.id);
        return conversation;
    }, [api]);

    const selectConversation = useCallback((conversationId: string) => {
        setActiveId(conversationId);
    }, []);

    const renameConversation = useCallback(async (conversationId: string, title: string) => {
        const nextTitle = title.trim() || "Untitled";
        await api.patch(
            endpoints.conversations.detail(conversationId),
            { title: nextTitle }
        );

        updateConversation(conversationId, (conversation) => ({
            ...conversation,
            title: nextTitle,
            updatedAt: Date.now(),
        }));
    }, [api, updateConversation]);

    const deleteConversation = useCallback(async (conversationId: string) => {
        await api.delete(endpoints.conversations.detail(conversationId));

        loadedConversationIdsRef.current.delete(conversationId);
        loadingConversationIdsRef.current.delete(conversationId);

        setConversations((previous) => previous.filter((conversation) => conversation.id !== conversationId));
        setActiveId((previous) => (previous === conversationId ? "" : previous));
    }, [api]);

    const stop = useCallback(() => {
        abortRef.current?.abort();
        abortRef.current = null;
        setIsTyping(false);
    }, []);

    const updateConversationUi = useCallback(
        (conversationId: string, updater: (ui: UiV1) => UiV1) => {
            updateConversation(conversationId, (conversation) => {
                const latestAssistantIndex = [...conversation.messages]
                    .reverse()
                    .findIndex((message) => message.role === "assistant");

                if (latestAssistantIndex === -1) {
                    return conversation;
                }

                const targetIndex = conversation.messages.length - 1 - latestAssistantIndex;
                const targetMessage = conversation.messages[targetIndex];
                const baseUi = (targetMessage.uiV1 ?? conversation.uiV1) as UiV1 | null;
                if (!baseUi) {
                    return conversation;
                }

                const nextUi = updater(baseUi);
                const nextMessages = [...conversation.messages];
                nextMessages[targetIndex] = {
                    ...targetMessage,
                    uiV1: nextUi,
                };

                return {
                    ...conversation,
                    messages: nextMessages,
                    uiV1: nextUi,
                    updatedAt: Date.now(),
                };
            });
        },
        [updateConversation]
    );

    const sendAction = useCallback(
        async (type: string, payload: Record<string, unknown>) => {
            if (!active?.uiV1) {
                return;
            }

            const ui = active.uiV1;
            const sourceRegistry = getSourceRegistry(ui);
            const actionRegistry = getActionRegistry(ui);
            const sourceKey = typeof payload.sourceKey === "string" ? payload.sourceKey : "";
            const dataSourceId = typeof payload.dataSourceId === "string"
                ? payload.dataSourceId
                : typeof payload.data_source === "string"
                    ? payload.data_source
                    : "";
            const actionId = typeof payload.actionId === "string" ? payload.actionId : "";
            const resolvedAction = actionId
                ? actionRegistry[actionId]
                : undefined;

            const directSource = sourceKey && sourceRegistry[sourceKey]
                ? sourceRegistry[sourceKey]
                : undefined;
            const fallbackSourceEntry = Object.entries(sourceRegistry).find(([, source]) => source.id === dataSourceId);
            const actionTargetSourceEntry = resolvedAction?.dataSourceId
                ? Object.entries(sourceRegistry).find(([, source]) => source.id === resolvedAction.dataSourceId)
                : undefined;
            const targetSourceKey = actionTargetSourceEntry?.[0] || sourceKey || fallbackSourceEntry?.[0] || "";
            const targetSource = actionTargetSourceEntry?.[1] ?? directSource ?? fallbackSourceEntry?.[1];
            const requestArgs = {
                ...(isRecord(targetSource?.lastArgs) ? targetSource.lastArgs : {}),
                ...payload,
                ...(resolvedAction?.dataSourceId ? { dataSourceId: resolvedAction.dataSourceId } : {}),
            };

            try {
                if (type === "data.query" && targetSource) {
                    const result = await querySandboxDataSource(api, targetSource, requestArgs);
                    updateConversationUi(active.id, (currentUi) =>
                        patchUiDataSourceResult(currentUi, [targetSourceKey], result, requestArgs)
                    );
                    return;
                }

                const inlineAction: UiActionState | null = resolvedAction ?? (
                    targetSource
                        ? {
                            id: actionId || dataSourceId || type,
                            actionType: type || "ui.action.execute",
                            handler: targetSource.handler,
                            dataSourceId: targetSource.id,
                            defaultArgs: targetSource.defaultArgs,
                            refreshDataSourceIds: [],
                        }
                        : null
                );

                if (!inlineAction) {
                    return;
                }

                const result = await executeSandboxAction(api, {
                    action: inlineAction,
                    dataSource: targetSource,
                    dataSources: sourceRegistry,
                    payload: requestArgs,
                });

                updateConversationUi(active.id, (currentUi) => {
                    let nextUi = currentUi;
                    const currentRegistry = getSourceRegistry(nextUi);

                    const primarySourceKeys = Object.entries(currentRegistry)
                        .filter(([key, source]) =>
                            key === targetSourceKey
                            || (resolvedAction?.dataSourceId && source.id === resolvedAction.dataSourceId)
                            || (result.dataSourceId && source.id === result.dataSourceId)
                        )
                        .map(([key]) => key);

                    nextUi = patchUiDataSourceResult(nextUi, primarySourceKeys, result, requestArgs);

                    for (const refresh of result.refreshes ?? []) {
                        const refreshKeys = Object.entries(getSourceRegistry(nextUi))
                            .filter(([, source]) => refresh.dataSourceId && source.id === refresh.dataSourceId)
                            .map(([key]) => key);
                        nextUi = patchUiDataSourceResult(
                            nextUi,
                            refreshKeys,
                            refresh,
                            getSourceRegistry(nextUi)[refreshKeys[0] ?? ""]?.defaultArgs ?? {}
                        );
                    }

                    return nextUi;
                });
            } catch {
                updateConversation(active.id, (conversation) => ({
                    ...conversation,
                    messages: updateLatestAssistantMessage(conversation.messages, (message) => ({
                        ...message,
                        content: [message.content ?? "", "Unable to refresh that component right now."]
                            .filter(Boolean)
                            .join("\n"),
                    })),
                    updatedAt: Date.now(),
                }));
            }
        },
        [active, updateConversation, updateConversationUi]
    );

    useEffect(() => {
        if (!active?.uiV1) {
            return;
        }

        const sourceRegistry = getSourceRegistry(active.uiV1);
        const refreshableSources = Object.entries(sourceRegistry).filter(([sourceKey, source]) => {
            const refreshOnLoad = source.refreshOnLoad === true;
            const refreshOnOpen = source.refreshOnConversationOpen === true;
            const staleAfterMs = typeof source.staleAfterMs === "number" ? source.staleAfterMs : null;
            const lastFetchedAt = typeof source.lastFetchedAt === "number" ? source.lastFetchedAt : null;
            const isStale = staleAfterMs !== null
                && staleAfterMs >= 0
                && typeof lastFetchedAt === "number"
                && Date.now() - lastFetchedAt >= staleAfterMs;
            const runKey = `${active.id}:${active.uiV1?.uiInstanceId ?? "ui"}:${sourceKey}`;
            const needsInitialLoad = refreshOnLoad && lastFetchedAt === null && !refreshedSourceRunsRef.current.has(runKey);

            return needsInitialLoad || refreshOnOpen || isStale;
        });

        if (!refreshableSources.length) {
            return;
        }

        let cancelled = false;

        void (async () => {
            for (const [sourceKey, source] of refreshableSources) {
                const runKey = `${active.id}:${active.uiV1?.uiInstanceId ?? "ui"}:${sourceKey}`;
                refreshedSourceRunsRef.current.add(runKey);

                try {
                    const result = await querySandboxDataSource(
                        api,
                        source,
                        (isRecord(source.lastArgs) ? source.lastArgs : source.defaultArgs) ?? {}
                    );

                    if (cancelled) {
                        return;
                    }

                    updateConversationUi(active.id, (ui) =>
                        patchUiDataSourceResult(
                            ui,
                            [sourceKey],
                            result,
                            (isRecord(source.lastArgs) ? source.lastArgs : source.defaultArgs) ?? {}
                        )
                    );
                } catch {
                    if (cancelled) {
                        return;
                    }
                }
            }
        })();

        return () => {
            cancelled = true;
        };
    }, [active?.id, active?.uiV1?.uiInstanceId, updateConversationUi]);

    const send = useCallback(async (rawText: string) => {
        const text = rawText.trim();
        if (!text || isTyping) {
            return;
        }

        let conversationId = active?.id ?? "";
        const assistantId = uidStr();
        const now = Date.now();
        let chunkBuffer = "";

        try {
            const conversation = active ?? await newChat();
            conversationId = conversation.id;

            loadedConversationIdsRef.current.add(conversationId);

            const userMessage: Message = {
                id: uidStr(),
                role: "user",
                content: text,
                createdAt: now,
            };
            const assistantMessage: Message = {
                id: assistantId,
                role: "assistant",
                content: "",
                createdAt: now,
                uiV1: null,
                status: "Opening pipeline...",
            };

            updateConversation(conversationId, (current) => ({
                ...current,
                title:
                    current.title === "New chat"
                        ? text.slice(0, 60) + (text.length > 60 ? "…" : "")
                        : current.title,
                messages: [...current.messages, userMessage, assistantMessage],
                updatedAt: now,
            }));
            setActiveId(conversationId);
            setIsTyping(true);

            abortRef.current?.abort();
            const controller = new AbortController();
            abortRef.current = controller;

            const response = await api.fetch(endpoints.conversations.chat, {
                method: "POST",
                headers: {
                    "Content-Type": "application/json",
                },
                body: JSON.stringify({
                    conversationId,
                    prompt: text,
                }),
                signal: controller.signal,
            });

            if (!response.ok || !response.body) {
                throw new Error("Unable to reach the AI gateway.");
            }

            const reader = response.body.getReader();
            const decoder = new TextDecoder();
            let buffer = "";

            while (true) {
                const { done, value } = await reader.read();
                if (done) {
                    break;
                }

                buffer += decoder.decode(value, { stream: true });

                while (true) {
                    const boundaryIndex = buffer.indexOf("\n\n");
                    if (boundaryIndex === -1) {
                        break;
                    }

                    const rawEvent = buffer.slice(0, boundaryIndex).trim();
                    buffer = buffer.slice(boundaryIndex + 2);

                    if (!rawEvent) {
                        continue;
                    }

                    const event = parseSseEvent(rawEvent);
                    if (!event) {
                        continue;
                    }

                    switch (event.type) {
                        case "session":
                            break;

                        case "status":
                            updateConversation(conversationId, (current) => ({
                                ...current,
                                messages: current.messages.map((message) =>
                                    message.id === assistantId
                                        ? { ...message, status: event.content ?? "Thinking..." }
                                        : message
                                ),
                                updatedAt: Date.now(),
                            }));
                            break;

                        case "chunk":
                            chunkBuffer += event.content ?? "";
                            break;

                        case "done": {
                            const rawPayload = (event.content ?? chunkBuffer).trim();
                            const parsed = JSON.parse(rawPayload || "{}") as AgentFinalResponse;
                            const ui = buildUiFromAgentResponse(parsed);

                            updateConversation(conversationId, (current) => ({
                                ...current,
                                messages: current.messages.map((message) =>
                                    message.id === assistantId
                                        ? {
                                            ...message,
                                            content: typeof parsed.text === "string" ? parsed.text : "",
                                            status: undefined,
                                            uiV1: ui,
                                        }
                                        : message
                                ),
                                uiV1: ui,
                                updatedAt: Date.now(),
                            }));

                            setIsTyping(false);
                            abortRef.current = null;
                            return;
                        }

                        case "error":
                            throw new Error(event.content ?? "The AI returned an error.");
                    }
                }
            }

            throw new Error("The stream closed before the final response arrived.");
        } catch (error) {
            if ((error as Error)?.name === "AbortError") {
                return;
            }

            const message = error instanceof Error ? error.message : "The request failed.";
            if (conversationId) {
                updateConversation(conversationId, (current) => ({
                    ...current,
                    messages: current.messages.map((entry) =>
                        entry.id === assistantId
                            ? {
                                ...entry,
                                content: message,
                                status: undefined,
                            }
                            : entry
                    ),
                    updatedAt: Date.now(),
                }));
            }
        } finally {
            setIsTyping(false);
            abortRef.current = null;
        }
    }, [active, api, isTyping, newChat, updateConversation]);

    return {
        conversations,
        activeId,
        active,
        model,
        setModel,
        isTyping,
        listRef,
        newChat,
        selectConversation,
        renameConversation,
        deleteConversation,
        send,
        sendAction,
        stop,
    };
}
