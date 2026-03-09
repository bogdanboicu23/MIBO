export const endpoints = {
    auth: {
        login: '/auth/login',
        signup: '/auth/register',
        refresh: '/auth/refresh-token',
    },
    conversations: {
        stream: "/v1/chat/stream",

        // NEW (backend .NET)
        chat: "/v1/chat",
        action: "/v1/action",
        list: "/v1/conversations",
        detail: (conversationId: string) => `/v1/conversations/${conversationId}`,
    },
    hubs: {
        ui: "/hubs/ui",
    },
}
