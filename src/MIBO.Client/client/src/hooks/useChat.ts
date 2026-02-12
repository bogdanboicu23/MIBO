import { useMemo, useRef, useState } from "react";
import type { Conversation, Message } from "../types/chat";
import { getMockConversations, uidStr } from "../_mock/conversations";

export function useChat() {
    const [conversations, setConversations] = useState<Conversation[]>(() => getMockConversations());
    const [activeId, setActiveId] = useState(conversations[0]?.id ?? "");
    const [model, setModel] = useState<"AI Agent" | "AI Agent Pro">("AI Agent");
    const [isTyping, setIsTyping] = useState(false);

    const active = useMemo(
        () => conversations.find((c) => c.id === activeId),
        [conversations, activeId]
    );

    const listRef = useRef<HTMLDivElement>(null as any);

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

    async function send(text: string) {
        if (!active || !text.trim() || isTyping) return;

        const now = Date.now();
        const userMsg: Message = { id: uidStr(), role: "user", content: text.trim(), createdAt: now };

        setConversations((p) =>
            p.map((c) =>
                c.id === active.id
                    ? {
                        ...c,
                        messages: [...c.messages, userMsg],
                        updatedAt: now,
                        title:
                            c.title === "New chat"
                                ? userMsg.content.slice(0, 28) + (userMsg.content.length > 28 ? "…" : "")
                                : c.title,
                    }
                    : c
            )
        );

        setIsTyping(true);
        await new Promise((r) => setTimeout(r, 600));

        const assistantMsg: Message = {
            id: uidStr(),
            role: "assistant",
            content: `Mock reply (${model}):\n\nAm primit: "${userMsg.content}".`,
            createdAt: Date.now(),
        };

        setConversations((p) =>
            p.map((c) => (c.id === active.id ? { ...c, messages: [...c.messages, assistantMsg], updatedAt: Date.now() } : c))
        );
        setIsTyping(false);
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
    };
}
