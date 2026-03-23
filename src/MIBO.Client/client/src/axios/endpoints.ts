export const endpoints = {
    auth: {
        login: '/api/auth/login',
        signup: '/api/auth/register',
        refresh: '/api/auth/refresh-token',
    },
    agent: {
        chat: "/api/chat",
    },
    actions: {
        query: "/api/actions/query",
        execute: "/api/actions/execute",
    },
    conversations: {
        stream: "/api/v1/chat/stream",

        chat: "/api/v1/chat",
        action: "/api/v1/action",
        list: "/api/v1/conversations",
        detail: (conversationId: string) => `/api/v1/conversations/${conversationId}`,
    },
    hubs: {
        ui: "/hubs/ui",
    },
    spotify: {
        authorize: "/api/auth/spotify/authorize",
        status: "/api/auth/spotify/status",
        disconnect: "/api/auth/spotify/disconnect",
    },
    finance: {
        status: "/api/auth/finance/status",
        connect: "/api/auth/finance/connect",
        disconnect: "/api/auth/finance/disconnect",
    },
}
