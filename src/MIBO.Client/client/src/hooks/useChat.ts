import { useMemo, useRef, useState, useCallback } from "react";
import type { Conversation, Message } from "../types/chat";
import { getMockConversations, uidStr } from "../_mock/conversations";
import { endpoints } from "@/axios/endpoints.ts";
import { useAxios } from "@/axios/hooks";

export function useChat() {
    const { api } = useAxios();

    const [conversations, setConversations] = useState<Conversation[]>(() => getMockConversations());
    const [activeId, setActiveId] = useState(conversations[0]?.id ?? "");
    const [model, setModel] = useState<"AI Agent" | "AI Agent Pro">("AI Agent");
    const [isTyping, setIsTyping] = useState(false);

    const active = useMemo(
        () => conversations.find((c) => c.id === activeId),
        [conversations, activeId]
    );

    const listRef = useRef<HTMLDivElement>(null as any);

    // pentru Stop streaming
    const abortRef = useRef<AbortController | null>(null);

    const updateConversation = useCallback((convId: string, updater: (c: Conversation) => Conversation) => {
        setConversations((p) => p.map((c) => (c.id === convId ? updater(c) : c)));
    }, []);

    function newChat() {
        const now = Date.now();
        const c: Conversation = {
            id: uidStr(),
            title: "New chat",
            updatedAt: now,
            messages: [
                { id: uidStr(), role: "assistant", content: "Începe o conversație nouă. Cu ce te pot ajuta?", createdAt: now },
            ],
        };
        setConversations((p) => [c, ...p]);
        setActiveId(c.id);
    }

    function selectConversation(id: string) {
        setActiveId(id);
    }

    function renameConversation(id: string, title: string) {
        setConversations((p) => p.map((c) => (c.id === id ? { ...c, title } : c)));
    }

    function deleteConversation(id: string) {
        setConversations((p) => p.filter((c) => c.id !== id));
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

    async function send(text: string) {
        if (!active || !text.trim() || isTyping) return;

        // oprește stream anterior dacă există
        abortRef.current?.abort();

        const now = Date.now();
        const trimmed = text.trim();

        const userMsg: Message = { id: uidStr(), role: "user", content: trimmed, createdAt: now };

        // mesaj assistant placeholder (gol) – aici facem append tokens
        const assistantId = uidStr();
        const assistantMsg: Message = { id: assistantId, role: "assistant", content: "", createdAt: now };

        // 1) update conversație: adaugă user + placeholder
        updateConversation(active.id, (c) => ({
            ...c,
            messages: [...c.messages, userMsg, assistantMsg],
            updatedAt: now,
            title:
                c.title === "New chat"
                    ? trimmed.slice(0, 28) + (trimmed.length > 28 ? "…" : "")
                    : c.title,
        }));

        // 2) pornește streaming
        setIsTyping(true);
        const ac = new AbortController();
        abortRef.current = ac;

        try {
            await api.postStream(
                endpoints.conversations.stream,
                {
                    message: trimmed,
                    // dacă backend-ul tău acceptă model, trimite-l:
                    // model: model === "AI Agent Pro" ? "..." : "..."
                },
                {
                    signal: ac.signal,
                    onToken: (tok) => {
                        // append la mesajul assistant
                        updateConversation(active.id, (c) => ({
                            ...c,
                            messages: c.messages.map((m) =>
                                m.id === assistantId ? { ...m, content: m.content + tok } : m
                            ),
                            updatedAt: Date.now(),
                        }));
                    },
                    onDone: () => {
                        setIsTyping(false);
                        abortRef.current = null;
                    },
                    onError: () => {
                        setIsTyping(false);
                        abortRef.current = null;

                        // fallback dacă nu a venit nimic
                        updateConversation(active.id, (c) => ({
                            ...c,
                            messages: c.messages.map((m) =>
                                m.id === assistantId && m.content.trim() === ""
                                    ? { ...m, content: "Eroare la răspunsul AI. Încearcă din nou." }
                                    : m
                            ),
                            updatedAt: Date.now(),
                        }));
                    },
                }
            );
        } catch (e) {
            // dacă a fost abort, e ok
            if ((e as any)?.name === "AbortError") return;

            setIsTyping(false);
            abortRef.current = null;

            updateConversation(active.id, (c) => ({
                ...c,
                messages: c.messages.map((m) =>
                    m.id === assistantId && m.content.trim() === ""
                        ? { ...m, content: "Eroare la conexiune/stream." }
                        : m
                ),
                updatedAt: Date.now(),
            }));
        }
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
    };
}
