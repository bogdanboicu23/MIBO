import { jwtDecode } from "jwt-decode";

import type { UserType } from "../../types";

import { JWT_STORAGE_KEY } from "./constant";

const NAME_CLAIM = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name";
const NAME_IDENTIFIER_CLAIM = "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier";
const ROLE_CLAIM = "http://schemas.microsoft.com/ws/2008/06/identity/claims/role";

type JwtPayload = {
    exp?: number;
    sub?: string;
    email?: string;
    username?: string;
    unique_name?: string;
    firstName?: string;
    lastName?: string;
    given_name?: string;
    family_name?: string;
    role?: string | string[];
    [NAME_CLAIM]?: string;
    [NAME_IDENTIFIER_CLAIM]?: string;
    [ROLE_CLAIM]?: string | string[];
};

export const getJwt = (): string | undefined => {
    try {
        return localStorage.getItem(JWT_STORAGE_KEY) ?? undefined;
    } catch {
        return undefined;
    }
};

export function isJwtExpired(token: string, clockSkewSeconds = 0): boolean {
    try {
        const decoded = jwtDecode<JwtPayload>(token);
        if (typeof decoded.exp !== "number") {
            return false;
        }

        return decoded.exp <= Math.floor(Date.now() / 1000) + Math.max(0, clockSkewSeconds);
    } catch {
        return true;
    }
}

export function parseUserFromJwt(token: string): UserType | undefined {
    try {
        const decoded = jwtDecode<JwtPayload>(token);
        const role = normalizeClaimValue(decoded.role ?? decoded[ROLE_CLAIM]);

        return {
            id: decoded.sub ?? decoded[NAME_IDENTIFIER_CLAIM],
            email: decoded.email ?? "",
            username: decoded.username ?? decoded.unique_name ?? decoded[NAME_CLAIM] ?? "",
            firstName: decoded.firstName ?? decoded.given_name ?? "",
            lastName: decoded.lastName ?? decoded.family_name ?? "",
            role,
        };
    } catch {
        return undefined;
    }
}

function normalizeClaimValue(value: string | string[] | undefined): string | undefined {
    if (Array.isArray(value)) {
        return value.find((entry) => entry.trim().length > 0);
    }

    return typeof value === "string" && value.trim().length > 0
        ? value
        : undefined;
}
