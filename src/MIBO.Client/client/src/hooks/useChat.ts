import { useMemo, useRef, useState, useCallback } from "react";
import type { Conversation, Message } from "../types/chat";
import { getMockConversations, uidStr } from "../_mock/conversations";
import { endpoints } from "@/axios/endpoints.ts";
import { useAxios } from "@/axios/hooks";

/**
 * MIBO useChat (integrat cu noul backend .NET):
 * - POST /v1/chat (non-stream) -> { text, uiV1, correlationId }
 * - POST /v1/action (non-LLM)  -> { text?, uiPatch?, correlationId? }
 * - Realtime patches vin prin SignalR (UiHub) si se aplica prin applyPatchFromRealtime(patch)
 *
 * IMPORTANT:
 * - Hook-ul nu hardcodeaza finance/shop; doar aplica ce vine din backend.
 * - UI este pastrat per conversatie (last uiV1).
 */
export function useChat() {
    const { api } = useAxios();

    const [conversations, setConversations] = useState<Conversation[]>([]);
    const [activeId, setActiveId] = useState(conversations[0]?.id ?? "");
    const [model, setModel] = useState<"AI Agent" | "AI Agent Pro">("AI Agent");
    const [isTyping, setIsTyping] = useState(false);

    const active = useMemo(
        () => conversations.find((c) => c.id === activeId),
        [conversations, activeId]
    );

    const listRef = useRef<HTMLDivElement>(null as any);

    // cancel request (no streaming, but we keep stop)
    const abortRef = useRef<AbortController | null>(null);

    const updateConversation = useCallback(
        (convId: string, updater: (c: any) => any) => {
            setConversations((p: any) => p.map((c: any) => (c.id === convId ? updater(c) : c)));
        },
        []
    );

    function newChat() {
        const now = Date.now();
        const c: any = {
            id: uidStr(),
            title: "New chat",
            updatedAt: now,
            messages: [
                {
                    id: uidStr(),
                    role: "assistant",
                    content: "Începe o conversație nouă. Cu ce te pot ajuta?",
                    createdAt: now,
                },
            ],
            // NEW: last UI for this conversation
            uiV1: null,
            correlationId: null,
        };
        setConversations((p: any) => [c, ...p]);
        setActiveId(c.id);
    }

    function selectConversation(id: string) {
        setActiveId(id);
    }

    function renameConversation(id: string, title: string) {
        setConversations((p: any) => p.map((c: any) => (c.id === id ? { ...c, title } : c)));
    }

    function deleteConversation(id: string) {
        setConversations((p: any) => p.filter((c: any) => c.id !== id));
        if (id === activeId) {
            const next = conversations.find((c) => c.id !== id)?.id;
            setActiveId(next ?? "");
        }
    }

    function stop() {
        abortRef.current?.abort();
        abortRef.current = null;
        setIsTyping(false);
    }

    /**
     * Send user prompt -> /v1/chat
     * Backend returns text + optional uiV1 (ui.v1)
     */
    async function send(text: string) {
        if (!active || !text.trim() || isTyping) return;

        // cancel previous request
        abortRef.current?.abort();

        const now = Date.now();
        const trimmed = text.trim();

        const userMsg: Message = { id: uidStr(), role: "user", content: trimmed, createdAt: now };

        const assistantId = uidStr();
        const assistantMsg: any = {
            id: assistantId,
            role: "assistant",
            content: "",
            createdAt: now,
            uiV1: null,
        };

        // optimistic update
        updateConversation(active.id, (c: any) => ({
            ...c,
            messages: [...c.messages, userMsg, assistantMsg],
            updatedAt: now,
            title:
                c.title === "New chat"
                    ? trimmed.slice(0, 28) + (trimmed.length > 28 ? "…" : "")
                    : c.title,
        }));

        setIsTyping(true);
        const ac = new AbortController();
        abortRef.current = ac;

        try {
            // NOTE: adapt userId from auth later; for now keep demo user id
            const payload = {
                conversationId: active.id,
                userId: "u-demo-001",
                prompt: trimmed,
            };

            const res = await api.post(endpoints.conversations.chat, payload);
            console.log("send", res);

            const data = res as { text: string; uiV1: any | null; correlationId: string };

            updateConversation(active.id, (c: any) => ({
                ...c,
                messages: c.messages.map((m: any) =>
                    m.id === assistantId
                        ? { ...m, content: data.text ?? "", uiV1: data.uiV1 ?? null }
                        : m
                ),
                updatedAt: Date.now(),
                uiV1: data.uiV1 ?? null,
                correlationId: data.correlationId ?? null,
            }));

            setIsTyping(false);
            abortRef.current = null;
        } catch (e) {
            if ((e as any)?.name === "CanceledError" || (e as any)?.name === "AbortError") return;

            setIsTyping(false);
            abortRef.current = null;

            updateConversation(active.id, (c: any) => ({
                ...c,
                messages: c.messages.map((m: any) =>
                    m.id === assistantId && (m.content ?? "").trim() === ""
                        ? { ...m, content: "Eroare la răspunsul AI / conexiune. Încearcă din nou." }
                        : m
                ),
                updatedAt: Date.now(),
            }));
        }
    }

    /**
     * Send UI/Shop action -> /v1/action
     * (NO LLM on backend for actions)
     */
    async function sendAction(type: string, payload: Record<string, unknown>, uiContext?: any) {
        if (!active) return;

        const req = {
            schema: "action.v1",
            conversationId: active.id,
            userId: "u-demo-001",
            action: { type, payload },
            uiContext: uiContext ?? {},
        };

        const res = await api.post(endpoints.conversations.action, req);
        const data = res.data as { schema: string; text?: string | null; uiPatch?: any | null; correlationId?: string };

        // optional assistant text
        if (data.text) {
            const now = Date.now();
            const msg: any = { id: uidStr(), role: "assistant", content: data.text, createdAt: now };
            updateConversation(active.id, (c: any) => ({
                ...c,
                messages: [...c.messages, msg],
                updatedAt: now,
            }));
        }

        // optional immediate patch
        if (data.uiPatch) {
            applyPatchToConversation(active.id, data.uiPatch);
        }
    }

    /**
     * Called by SignalR UiHub ("UiPatch") handler
     */
    function applyPatchFromRealtime(patch: any) {
        if (!active) return;
        applyPatchToConversation(active.id, patch);
    }

    function applyPatchToConversation(conversationId: string, patch: any) {
        updateConversation(conversationId, (c: any) => {
            if (!c.uiV1) return c;
            const nextUi = applyUiPatch(c.uiV1, patch);
            return { ...c, uiV1: nextUi, updatedAt: Date.now() };
        });
    }

    /**
     * Minimal patch engine:
     * patch = { schema:"ui.patch.v1", ops:[{op:"set"|"merge"|"remove", path:"/data/x", value:any}] }
     */
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

            if (op.op === "set") cur[key] = op.value;
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

        // NEW:
        sendAction,
        applyPatchFromRealtime,
    };
}