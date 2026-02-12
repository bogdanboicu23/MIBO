import type { Conversation } from "../types/chat";

function uid() {
    return Math.random().toString(36).slice(2) + Date.now().toString(36);
}

export function getMockConversations(): Conversation[] {
    const now = Date.now();
    return [
        {
            id: uid(),
            title: "Demo: UI ChatGPT-like",
            updatedAt: now,
            messages: [
                {
                    id: uid(),
                    role: "assistant",
                    content:
                        "Salut! Sunt UI demo. Scrie un mesaj ca să vezi conversația + layout-ul full screen. 😊",
                    createdAt: now - 60_000,
                },
            ],
        },
        {
            id: uid(),
            title: "Idei agent AI",
            updatedAt: now - 3_600_000,
            messages: [
                {
                    id: uid(),
                    role: "assistant",
                    content:
                        "Putem construi: UI + API + orchestrator + tools (Sheets/Email/DB).",
                    createdAt: now - 3_600_000,
                },
            ],
        },
    ];
}

export function uidStr() {
    return Math.random().toString(36).slice(2) + Date.now().toString(36);
}
