export const endpoints = {
    auth: {
        login: '/auth/login',
        signup: '/auth/register',
        refresh: '/auth/refresh-token',
    },
    conversations: {
        stream: "/conversations/stream",

        // NEW (backend .NET)
        chat: "/v1/chat",
        action: "/v1/action",
    },
    hubs: {
        ui: "/hubs/ui",
    },
}