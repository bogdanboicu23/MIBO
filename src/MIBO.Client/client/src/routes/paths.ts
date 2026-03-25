export const paths = {
    root: '/',
    chat: '/chat',
    settings: '/settings',
    auth: {
        login: '/auth/login',
        signup: '/auth/signup',
        forgotPassword: '/auth/forgot-password'
    },
};

export function normalizeReturnTo(returnTo?: string | null) {
    if (!returnTo || !returnTo.startsWith('/')) {
        return paths.chat;
    }

    if (returnTo === '/intro' || returnTo.startsWith('/auth')) {
        return paths.chat;
    }

    return returnTo;
}
