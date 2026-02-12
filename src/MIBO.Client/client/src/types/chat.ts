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
    content: string;
    createdAt: number;
};

export type Conversation = {
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