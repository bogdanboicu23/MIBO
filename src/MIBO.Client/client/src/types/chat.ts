import type { UiV1 } from "@/types/ui.ts";

export type Role = "user" | "assistant" | "system";

export type Message = {
    id: string;
    role: Role;
    content?: string;
    createdAt: number;
    uiV1?: UiV1 | null;
    status?: string;
};

export type Conversation = {
    id: string;
    sessionId?: string | null;
    title: string;
    messages: Message[];
    updatedAt: number;
    uiV1: UiV1 | null;
};
