import { JWT_STORAGE_KEY } from './constant';

export const getJwt = (): string | undefined => {
    try {
        return localStorage.getItem(JWT_STORAGE_KEY) ?? undefined;
    } catch {
        return undefined;
    }
};