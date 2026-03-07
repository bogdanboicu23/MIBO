import { useMemo, useRef, useState, useCallback, useEffect } from "react";
import type { Conversation, Message } from "../types/chat";
import { endpoints } from "@/axios/endpoints.ts";
import { useAxios } from "@/axios/hooks";
import { uidStr } from "@/_mock/conversations.ts";

const DEMO_USER_ID = "u-demo-001";

type ServerConversationSummary = {
    conversationId: string;
    userId: string;
    title: string;
    createdAt: string;
    updatedAt: string;
    lastMessageAt?: string | null;
    lastMessagePreview?: string | null;
    messageCount: number;
};

type ServerConversationMessage = {
    messageId: string;
    conversationId: string;
    userId: string;
    role: "user" | "assistant" | "system";
    text: string;
    uiV1: any | null;
    correlationId: string;
    createdAt: string;
};

type ServerConversationDetails = {
    conversation: ServerConversationSummary;
    messages: ServerConversationMessage[];
};

function normalizeBackendUi(ui: any): any {
    if (!ui || typeof ui !== "object") return null;
    if (ui.schema !== "ui.v1") return ui;

    const root = normalizeUiNode(ui.root);
    if (!root) return null;

    const bindings = normalizeBindings(ui.bindings, root);
    const subscriptions = Array.isArray(ui.subscriptions) ? ui.subscriptions : [];
    const data = ui.data && typeof ui.data === "object" ? ui.data : {};

    return {
        schema: "ui.v1",
        root,
        data,
        bindings,
        subscriptions,
    };
}

function normalizeUiNode(node: any): any | null {
    if (!node) return null;

    if (node.root && Object.keys(node).length === 1) {
        return normalizeUiNode(node.root);
    }

    // Legacy layout shorthand:
    // { type: "column"|"row"|"grid", components: [...] }
    if (typeof node.type === "string" && ["column", "row", "grid"].includes(node.type)) {
        const childrenRaw = Array.isArray(node.components)
            ? node.components
            : Array.isArray(node.children)
                ? node.children
                : [];
        const children = childrenRaw.map((c: any) => normalizeUiNode(c)).filter(Boolean);
        return {
            type: "layout",
            name: node.type,
            props: node.props && typeof node.props === "object" ? node.props : {},
            children,
        };
    }

    // Legacy component shorthand:
    // { type: "dataTable", props: {...} } or { component: "dataTable", ... }
    if (typeof node.type === "string" && !["layout", "component"].includes(node.type)) {
        const props = node.props && typeof node.props === "object" ? node.props : {};
        const childrenRaw = Array.isArray(node.children)
            ? node.children
            : Array.isArray(node.components)
                ? node.components
                : [];
        const children = childrenRaw.map((c: any) => normalizeUiNode(c)).filter(Boolean);
        return {
            type: "component",
            name: node.type,
            props,
            children,
        };
    }

    if (node.type === "layout" || node.type === "component") {
        const inferredName =
            node.name ??
            (node.type === "component" ? node.component : undefined) ??
            (node.type === "layout" ? "column" : undefined);
        const name = String(inferredName ?? "").trim();
        if (!name) return null;
        const childrenRaw = Array.isArray(node.children)
            ? node.children
            : Array.isArray(node.components)
                ? node.components
                : [];
        const children = childrenRaw.map((c: any) => normalizeUiNode(c)).filter(Boolean);
        return {
            type: node.type,
            name,
            props: node.props && typeof node.props === "object" ? node.props : {},
            children,
        };
    }

    if (typeof node.component === "string") {
        const props = { ...node };
        delete props.component;
        delete props.children;
        delete props.root;
        return {
            type: "component",
            name: node.component,
            props,
            children: [],
        };
    }

    if (typeof node === "object") {
        const children = Object.entries(node)
            .filter(([k]) => !["root", "bindings", "subscriptions"].includes(k))
            .map(([k, v]) => ({
                type: "component",
                name: k,
                props: v && typeof v === "object" ? ((v as any).props ?? v) : {},
                children: [],
            }));

        if (children.length > 0) {
            return {
                type: "layout",
                name: "column",
                props: { gap: 12 },
                children,
            };
        }
    }

    return null;
}

function normalizeBindings(bindings: any, root: any): any[] {
    if (!Array.isArray(bindings)) return [];

    const normalizeFrom = (raw: unknown): string => {
        const s = String(raw ?? "").trim();
        const m = /^\$\{(?:tool|step):(.+)\}$/.exec(s);
        return m?.[1]?.trim() || s;
    };

    const normalizePath = (raw: unknown): string => {
        const p = String(raw ?? "").trim();
        if (!p) return "/root";
        if (p.startsWith("/")) return p;
        return findComponentPath(root, p) ?? "/root";
    };

    return bindings
        .map((b) => {
            if (b && typeof b === "object" && b.componentPath && b.prop && b.from) {
                return {
                    componentPath: normalizePath(b.componentPath),
                    prop: String(b.prop),
                    from: normalizeFrom(b.from),
                };
            }

            if (b && typeof b === "object" && b.component && b.prop && b.tool) {
                const comp = String(b.component);
                const arg = b.arg ? String(b.arg) : "";
                const from = normalizeFrom(arg ? `${String(b.tool)}.${arg}` : String(b.tool));
                const componentPath = findComponentPath(root, comp) ?? "/root";
                return {
                    componentPath,
                    prop: String(b.prop),
                    from: comp === "productDetail" && arg === "products" ? `${String(b.tool)}.products.0` : from,
                };
            }

            return null;
        })
        .filter(Boolean);
}

function findComponentPath(node: any, name: string, path = "/root"): string | null {
    if (!node || typeof node !== "object") return null;
    if (node.type === "component" && String(node.name) === name) return path;

    if (!Array.isArray(node.children)) return null;
    for (let i = 0; i < node.children.length; i++) {
        const found = findComponentPath(node.children[i], name, `${path}/children/${i}`);
        if (found) return found;
    }
    return null;
}

export function useChat() {
    const { api } = useAxios();

    const [conversations, setConversations] = useState<Conversation[]>([]);
    const [activeId, setActiveId] = useState("");
    const [model, setModel] = useState<"AI Agent" | "AI Agent Pro">("AI Agent");
    const [isTyping, setIsTyping] = useState(false);

    const active = useMemo(
        () => conversations.find((c) => c.id === activeId),
        [conversations, activeId]
    );

    const listRef = useRef<HTMLDivElement>(null as any);
    const abortRef = useRef<AbortController | null>(null);

    const updateConversation = useCallback(
        (convId: string, updater: (c: Conversation) => Conversation) => {
            setConversations((prev) => prev.map((c) => (c.id === convId ? updater(c) : c)));
        },
        []
    );

    const mapSummaryToConversation = useCallback((item: ServerConversationSummary): Conversation => {
        return {
            id: item.conversationId,
            title: item.title || "New chat",
            updatedAt: Date.parse(item.updatedAt) || Date.now(),
            messages: [],
            uiV1: null,
            correlationId: null,
            loaded: false,
            lastMessagePreview: item.lastMessagePreview ?? null,
        };
    }, []);

    const loadConversationList = useCallback(async () => {
        const data = await api.get<{ items: ServerConversationSummary[] }>(endpoints.conversations.list, {
            params: { userId: DEMO_USER_ID, skip: 0, limit: 100 },
        });

        const mapped = (data.items ?? []).map(mapSummaryToConversation);
        setConversations(mapped);

        if (mapped.length > 0) {
            setActiveId((prev) => prev || mapped[0].id);
        }
    }, [api, mapSummaryToConversation]);

    const loadConversationDetails = useCallback(
        async (conversationId: string) => {
            const data = await api.get<ServerConversationDetails>(endpoints.conversations.detail(conversationId), {
                params: { userId: DEMO_USER_ID, messagesLimit: 300 },
            });

            const messages: Message[] = (data.messages ?? []).map((m) => ({
                id: m.messageId,
                role: m.role,
                content: m.text,
                uiV1: normalizeBackendUi(m.uiV1),
                createdAt: Date.parse(m.createdAt) || Date.now(),
            }));

            const latestUi = [...messages].reverse().find((m) => m.role === "assistant" && m.uiV1)?.uiV1 ?? null;

            updateConversation(conversationId, (c) => ({
                ...c,
                title: data.conversation?.title || c.title,
                updatedAt: Date.parse(data.conversation?.updatedAt ?? "") || c.updatedAt,
                messages,
                uiV1: latestUi,
                loaded: true,
                lastMessagePreview: data.conversation?.lastMessagePreview ?? c.lastMessagePreview,
            }));
        },
        [api, updateConversation]
    );

    useEffect(() => {
        void loadConversationList();
    }, [loadConversationList]);

    useEffect(() => {
        if (!activeId) return;
        const conv = conversations.find((c) => c.id === activeId);
        if (!conv || conv.loaded) return;
        void loadConversationDetails(activeId);
    }, [activeId, conversations, loadConversationDetails]);

    async function newChat() {
        const created = await api.post<ServerConversationSummary>(endpoints.conversations.list, {
            title: "New chat",
        }, {
            params: { userId: DEMO_USER_ID },
        });

        const c = mapSummaryToConversation(created);
        c.loaded = true;

        setConversations((prev) => [c, ...prev]);
        setActiveId(c.id);
        return c;
    }

    function selectConversation(id: string) {
        setActiveId(id);
    }

    async function renameConversation(id: string, title: string) {
        const nextTitle = title.trim() || "Untitled";
        updateConversation(id, (c) => ({ ...c, title: nextTitle }));

        try {
            await api.patch(endpoints.conversations.detail(id), { title: nextTitle }, {
                params: { userId: DEMO_USER_ID },
            });
        } catch {
            await loadConversationList();
        }
    }

    async function deleteConversation(id: string) {
        const prev = conversations;
        setConversations((p) => p.filter((c) => c.id !== id));

        if (id === activeId) {
            const next = conversations.find((c) => c.id !== id)?.id;
            setActiveId(next ?? "");
        }

        try {
            await api.delete(endpoints.conversations.detail(id), {
                params: { userId: DEMO_USER_ID },
            });
        } catch {
            setConversations(prev);
        }
    }

    function stop() {
        abortRef.current?.abort();
        abortRef.current = null;
        setIsTyping(false);
    }

    async function send(text: string) {
        if (!text.trim() || isTyping) return;

        let currentActive = active;
        if (!currentActive) {
            currentActive = await newChat();
        }
        if (!currentActive) return;

        abortRef.current?.abort();

        const now = Date.now();
        const trimmed = text.trim();

        const userMsg: Message = { id: uidStr(), role: "user", content: trimmed, createdAt: now };

        const assistantId = uidStr();
        const assistantMsg: Message = {
            id: assistantId,
            role: "assistant",
            content: "",
            createdAt: now,
            uiV1: null,
        };

        updateConversation(currentActive.id, (c) => ({
            ...c,
            messages: [...c.messages, userMsg, assistantMsg],
            updatedAt: now,
            loaded: true,
            title:
                c.title === "New chat"
                    ? trimmed.slice(0, 60) + (trimmed.length > 60 ? "…" : "")
                    : c.title,
            lastMessagePreview: trimmed,
        }));

        setIsTyping(true);
        const ac = new AbortController();
        abortRef.current = ac;

        try {
            const payload = {
                conversationId: currentActive.id,
                userId: DEMO_USER_ID,
                prompt: trimmed,
                clientContext: {
                    timezone: Intl.DateTimeFormat().resolvedOptions().timeZone,
                    locale: navigator.language,
                },
            };

            const data = await api.post<{ text: string; uiV1: any | null; correlationId: string }>(
                endpoints.conversations.chat,
                payload
            );

            updateConversation(currentActive.id, (c) => ({
                ...c,
                messages: c.messages.map((m) =>
                    m.id === assistantId
                        ? { ...m, content: data.text ?? "", uiV1: normalizeBackendUi(data.uiV1) }
                        : m
                ),
                updatedAt: Date.now(),
                uiV1: normalizeBackendUi(data.uiV1) ?? c.uiV1 ?? null,
                correlationId: data.correlationId ?? null,
                loaded: true,
            }));
        } catch (e: any) {
            if (e?.name === "CanceledError" || e?.name === "AbortError") return;

            updateConversation(currentActive.id, (c) => ({
                ...c,
                messages: c.messages.map((m) =>
                    m.id === assistantId && (m.content ?? "").trim() === ""
                        ? { ...m, content: "Eroare la răspunsul AI / conexiune. Încearcă din nou." }
                        : m
                ),
                updatedAt: Date.now(),
            }));
        } finally {
            setIsTyping(false);
            abortRef.current = null;
        }
    }

    async function sendAction(type: string, payload: Record<string, unknown>, uiContext?: any) {
        if (!active) return;
        const payloadUi = (payload as any)?.__ui ?? null;
        const focusedComponentId =
            typeof payloadUi?.component === "string" ? payloadUi.component :
            typeof payloadUi?.path === "string" ? payloadUi.path :
            null;

        const req = {
            schema: "action.v1",
            conversationId: active.id,
            userId: DEMO_USER_ID,
            action: { type, payload },
            uiContext: uiContext ?? { focusedComponentId },
        };

        const data = await api.post<{ schema: string; text?: string | null; uiPatch?: any | null; uiV1?: any | null; correlationId?: string }>(
            endpoints.conversations.action,
            req
        );

        const hasPatch = Boolean(data.uiPatch);
        // UI actions should update current card in-place via patch, not append a new assistant bubble.
        if (!hasPatch && (data.text || data.uiV1)) {
            const now = Date.now();
            const msg: Message = {
                id: uidStr(),
                role: "assistant",
                content: data.text ?? "",
                uiV1: normalizeBackendUi(data.uiV1),
                createdAt: now,
            };
            updateConversation(active.id, (c) => ({
                ...c,
                messages: [...c.messages, msg],
                updatedAt: now,
                uiV1: normalizeBackendUi(data.uiV1) ?? c.uiV1,
            }));
        }

        if (data.uiPatch) {
            applyPatchToConversation(active.id, data.uiPatch);
        }
    }

    function applyPatchFromRealtime(patch: any) {
        if (!active) return;
        applyPatchToConversation(active.id, patch);
    }

    function applyPatchToConversation(conversationId: string, patch: any) {
        updateConversation(conversationId, (c) => {
            if (!c.uiV1) return c;
            const nextUi = applyUiPatch(c.uiV1, patch);
            const updatedMessages = [...c.messages];
            for (let i = updatedMessages.length - 1; i >= 0; i--) {
                if (updatedMessages[i].role === "assistant" && updatedMessages[i].uiV1) {
                    updatedMessages[i] = { ...updatedMessages[i], uiV1: nextUi };
                    break;
                }
            }
            return { ...c, uiV1: nextUi, messages: updatedMessages, updatedAt: Date.now() };
        });
    }

    function applyUiPatch(ui: any, patch: any) {
        const next = structuredClone(ui);

        for (const op of patch?.ops ?? []) {
            const path = String(op.path ?? "");
            const parts = path.split("/").filter(Boolean);
            if (parts.length === 0) continue;

            let cur: any = next;
            for (let i = 0; i < parts.length - 1; i++) {
                const p = parts[i];
                cur[p] ??= {};
                cur = cur[p];
                if (cur == null) break;
            }
            if (cur == null) continue;

            const key = parts[parts.length - 1];

            if (op.op === "set" || op.op === "replace") cur[key] = op.value;
            else if (op.op === "merge") cur[key] = { ...(cur[key] ?? {}), ...(op.value ?? {}) };
            else if (op.op === "remove") delete cur[key];
        }

        return next;
    }

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
        stop,

        sendAction,
        applyPatchFromRealtime,
    };
}
