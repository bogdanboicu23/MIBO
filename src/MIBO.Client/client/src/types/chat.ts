import type { UiV1 } from "@/types/ui.ts";
import type { AxiosInstance } from "axios";
import { endpoints } from "@/axios/endpoints.ts";

export type Role = "user" | "assistant" | "system";
export type ChatStatus = "idle" | "sending" | "error";

export type ClientContext = {
    timezone: string;
    locale: string;
};

export type ChatError = {
    message: string;
    details?: string;
};


export type Message = {
    id: string;
    role: Role;
    content?: string;
    createdAt: number;
    uiV1?: any | null;
};

export type Conversation = {
    uiV1: UiV1 | null;
    id: string;
    title: string;
    messages: Message[];
    updatedAt: number;
};

export type ChatState = {
    conversationId: string;
    status: ChatStatus;
    error: ChatError | null;
    clientContext: ClientContext;
    messages: Message[];
    lastUiSpec: any | null;
};

export type ChatRequestV1 = {
    conversationId: string;
    userId: string;
    prompt: string;
};

export type ChatResponseV1 = {
    text: string;
    uiV1: UiV1 | null;
    correlationId: string;
};

export async function postChat(axios: AxiosInstance, req: ChatRequestV1) {
    const { data } = await axios.post<ChatResponseV1>(endpoints.conversations.chat, req);
    return data;
}
