export type LoginData = {
    email: string;
    password: string;
    turnstileToken?: string;
};

export type UserType = {
    role: string;
}

export type AuthState = {
    user: UserType | undefined;
    loading: boolean;
};

export type AuthContextValue = {
    user: UserType | undefined;
    loading: boolean;
    authenticated: boolean;
    unauthenticated: boolean;
    checkUserSession?: () => Promise<void>;
    login: (request: LoginData) => Promise<void>;
};