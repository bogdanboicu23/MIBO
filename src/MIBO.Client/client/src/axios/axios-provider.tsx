import type { ReactNode } from "react";
import type { ApiErrorKeyResponse } from "../types/axios";
import type {
    AxiosError,
    AxiosInstance,
    AxiosResponse,
    AxiosRequestConfig,
    InternalAxiosRequestConfig,
} from "axios";

import axios from "axios";
import { toast } from "react-toastify";
import { useRef, useMemo, useState, useEffect, useCallback } from "react";

import { paths } from "src/routes/paths";
import { useRouter } from "src/routes/hooks";

import { CONFIG } from "src/global-config";

import { endpoints } from "./endpoints";
import { AxiosContext } from "./context/axios-context";
import { getJwt, isJwtExpired, JWT_STORAGE_KEY } from "@/auth/context/jwt";

type RefreshResponse = { jwtToken: string };

type RetriableRequestConfig = InternalAxiosRequestConfig & {
    _retry?: boolean;
    _skipToast?: boolean;
};

type StreamOptions = {
    signal?: AbortSignal;
    onToken: (token: string) => void;
    onDone?: () => void;
    onError?: (err: unknown) => void;
    onEvent?: (eventType: string, data: any) => void;
};

const getTimeZone = () => Intl.DateTimeFormat().resolvedOptions().timeZone;

type ApiErrorBody = { message?: string };

const joinApiUrl = (baseUrl: string, path: string): string => {
    if (/^https?:\/\//i.test(path)) {
        return path;
    }

    const normalizedBase = baseUrl.replace(/\/+$/, "");
    const normalizedPath = path.startsWith("/") ? path : `/${path}`;

    if (normalizedBase.endsWith("/api") && normalizedPath.startsWith("/api/")) {
        return `${normalizedBase}${normalizedPath.slice(4)}`;
    }

    return `${normalizedBase}${normalizedPath}`;
};

const buildRequestHeaders = (
    headers: HeadersInit | undefined,
    token?: string,
    includeJsonContentType = false
): Headers => {
    const nextHeaders = new Headers(headers);

    nextHeaders.set("X-User-Time-Zone", getTimeZone());

    if (includeJsonContentType && !nextHeaders.has("Content-Type")) {
        nextHeaders.set("Content-Type", "application/json");
    }

    if (token) {
        nextHeaders.set("Authorization", `Bearer ${token}`);
    } else {
        nextHeaders.delete("Authorization");
    }

    return nextHeaders;
};

export const AxiosProvider = ({ children }: { children: ReactNode }) => {
    const router = useRouter();

    // Keep a state for UI / context consumers
    const [jwt, setJwtState] = useState<string | undefined>(getJwt());

    // Single source of truth for token used by interceptors (avoids races)
    const jwtRef = useRef<string | undefined>(jwt);
    useEffect(() => {
        jwtRef.current = jwt;
    }, [jwt]);

    useEffect(() => {
        const syncJwtFromStorage = () => {
            const storedJwt = getJwt();
            if (storedJwt === jwtRef.current) {
                return;
            }

            jwtRef.current = storedJwt;
            setJwtState(storedJwt);
        };

        const handleVisibilityChange = () => {
            if (!document.hidden) {
                syncJwtFromStorage();
            }
        };

        window.addEventListener("storage", syncJwtFromStorage);
        window.addEventListener("focus", syncJwtFromStorage);
        window.addEventListener("pageshow", syncJwtFromStorage);
        document.addEventListener("visibilitychange", handleVisibilityChange);

        return () => {
            window.removeEventListener("storage", syncJwtFromStorage);
            window.removeEventListener("focus", syncJwtFromStorage);
            window.removeEventListener("pageshow", syncJwtFromStorage);
            document.removeEventListener("visibilitychange", handleVisibilityChange);
        };
    }, []);

    const setJwt = useCallback((token?: string) => {
        if (token) localStorage.setItem(JWT_STORAGE_KEY, token);
        else localStorage.removeItem(JWT_STORAGE_KEY);

        jwtRef.current = token;
        setJwtState(token);
    }, []);

    const axiosInstance: AxiosInstance = useMemo(() => axios.create({
        baseURL: CONFIG.apiServerUrl as string,
    }), []);

    const axiosLogin: AxiosInstance = useMemo(() => axios.create({
        baseURL: CONFIG.apiServerUrl as string,
        withCredentials: true,
    }), []);

    const axiosDefault: AxiosInstance = useMemo(() => axios.create({
        baseURL: CONFIG.apiServerUrl as string,
    }), []);

    const refreshPromiseRef = useRef<Promise<string> | null>(null);

    const logoutAndRedirect = useCallback(() => {
        setJwt(undefined);
        toast.error("Your session has expired. Please log in again.");
        if (router?.push) router.push(paths.auth.login);
    }, [router, setJwt]);

    const refreshJwt = useCallback(async (): Promise<string> => {
        if (refreshPromiseRef.current) return refreshPromiseRef.current;

        const doRefresh = (async () => {
            const currentJwt = jwtRef.current || getJwt();
            const response = await axiosLogin.post<RefreshResponse>(
                endpoints.auth.refresh,
                currentJwt ? { jwtToken: currentJwt } : {}
            );

            if (response.status !== 200 || !response.data?.jwtToken) {
                throw new Error("Refresh failed.");
            }

            const newToken = response.data.jwtToken;
            setJwt(newToken);
            return newToken;
        })();

        refreshPromiseRef.current = doRefresh;

        try {
            return await doRefresh;
        } finally {
            refreshPromiseRef.current = null;
        }
    }, [axiosLogin, setJwt]);

    const ensureValidJwt = useCallback(async (): Promise<string | undefined> => {
        const currentJwt = jwtRef.current || getJwt();

        if (currentJwt && !isJwtExpired(currentJwt, 30)) {
            return currentJwt;
        }

        try {
            return await refreshJwt();
        } catch {
            setJwt(undefined);
            return undefined;
        }
    }, [refreshJwt, setJwt]);

    const fetchWithAuth = useCallback(
        async (url: string, options?: RequestInit): Promise<Response> => {
            const requestUrl = joinApiUrl(CONFIG.apiServerUrl, url);
            const doFetch = async (token?: string) => {
                return fetch(requestUrl, {
                    ...options,
                    headers: buildRequestHeaders(options?.headers, token),
                    credentials: options?.credentials ?? "include",
                });
            };

            let token = jwtRef.current || getJwt();
            const response = await doFetch(token);

            if (response.status !== 401) {
                return response;
            }

            try {
                token = await refreshJwt();
                return await doFetch(token);
            } catch (error) {
                logoutAndRedirect();
                throw error;
            }
        },
        [refreshJwt, logoutAndRedirect]
    );

    const postStream = useCallback(
        async (url: string, data: any, opts: StreamOptions): Promise<void> => {
            const { signal, onToken, onDone, onError } = opts;

            const handleError = (err: unknown) => {
                onError?.(err);
                throw err;
            };

            try {
                const res = await fetchWithAuth(url, {
                    method: "POST",
                    headers: buildRequestHeaders(undefined, undefined, true),
                    body: JSON.stringify(data),
                    signal,
                });

                if (!res.ok) {
                    const text = await res.text().catch(() => "");
                    return handleError(new Error(`Stream failed: ${res.status} ${res.statusText} ${text}`));
                }

                if (!res.body) {
                    return handleError(new Error("ReadableStream not supported in this environment."));
                }

                const reader = res.body.getReader();
                const decoder = new TextDecoder("utf-8");
                let buffer = "";

                // read chunks forever
                while (true) {
                    const { value, done } = await reader.read();
                    if (done) break;

                    buffer += decoder.decode(value, { stream: true });

                    // SSE events separated by blank line
                    let idx: number;
                    while ((idx = buffer.indexOf("\n\n")) !== -1) {
                        const rawEvent = buffer.slice(0, idx);
                        buffer = buffer.slice(idx + 2);

                        if (!rawEvent.trim()) continue;

                        let currentEvent: string | null = null;
                        const dataLines: string[] = [];

                        for (const line of rawEvent.split("\n")) {
                            if (line.startsWith("event:")) {
                                currentEvent = line.slice("event:".length).trim();
                                continue;
                            }

                            if (line.startsWith("data:")) {
                                // IMPORTANT: do NOT trim -> preserve spaces/newlines in tokens
                                const payload = line.startsWith("data: ")
                                    ? line.slice("data: ".length)
                                    : line.slice("data:".length);

                                dataLines.push(payload);
                            }
                        }

                        if (dataLines.length === 0) continue;

                        // SSE can have multiple data lines; join with \n
                        const payload = dataLines.join("\n");

                        // handle named events (production controller)
                        if (currentEvent === "done") {
                            onDone?.();
                            return;
                        }

                        if (currentEvent === "error") {
                            try {
                                const obj = JSON.parse(payload);
                                return handleError(new Error(obj?.message ?? "Stream error"));
                            } catch {
                                return handleError(new Error(payload || "Stream error"));
                            }
                        }

                        // named events (e.g. "ui") -> route through onEvent callback
                        if (currentEvent && opts.onEvent) {
                            try {
                                const obj = JSON.parse(payload);
                                opts.onEvent(currentEvent, obj);
                            } catch {
                                opts.onEvent(currentEvent, payload);
                            }
                            continue;
                        }

                        // default: token chunks
                        // production controller sends JSON: {"t":"..."} or {"done":true}
                        try {
                            const obj = JSON.parse(payload);

                            if (typeof obj?.t === "string") {
                                onToken(obj.t);
                                continue;
                            }

                            if (obj?.done === true) {
                                onDone?.();
                                return;
                            }

                            if (typeof obj?.message === "string") {
                                // sometimes servers send message without event:error
                                return handleError(new Error(obj.message));
                            }

                            // unknown JSON -> ignore
                        } catch {
                            // fallback if server sends plain text
                            if (payload === "[DONE]") {
                                onDone?.();
                                return;
                            }
                            onToken(payload);
                        }
                    }
                }

                // stream ended without explicit done
                onDone?.();
            } catch (err) {
                // If user aborted, treat as normal cancel
                if ((err as any)?.name === "AbortError") return;

                onError?.(err);
                throw err;
            }
        },
        [fetchWithAuth]
    );

    useEffect(() => {
        const reqId = axiosInstance.interceptors.request.use(
            (config: InternalAxiosRequestConfig) => {
                config.headers = config.headers ?? {};

                config.headers["X-User-Time-Zone"] = getTimeZone();

                const token = jwtRef.current || getJwt();
                if (token) config.headers.Authorization = `Bearer ${token}`;
                else delete config.headers.Authorization;

                return config;
            },
            (error) => Promise.reject(error)
        );

        const resId = axiosInstance.interceptors.response.use(
            (response) => response,
            async (error: AxiosError<ApiErrorBody>) => {
                const originalRequest = error.config as RetriableRequestConfig | undefined;

                if (!originalRequest) return Promise.reject(error);

                if (error.code === "ERR_NETWORK") {
                    if (!originalRequest._skipToast) {
                        toast.warning("Server is temporarily unavailable. Please try again later.");
                    }
                    return Promise.reject(error);
                }

                const status = error.response?.status;

                if (status === 403) {
                    if (!originalRequest._skipToast) {
                        toast.error("You don't have permission to perform this action.")
                    }
                    return Promise.reject(error);
                }

                if (status === 401 && !originalRequest._retry) {
                    originalRequest._retry = true;

                    const url = (originalRequest.url || "").toString();
                    if (url.includes(endpoints.auth.refresh)) {
                        logoutAndRedirect();
                        return Promise.reject(error);
                    }

                    try {
                        const newToken = await refreshJwt();

                        originalRequest.headers = originalRequest.headers ?? {};
                        originalRequest.headers.Authorization = `Bearer ${newToken}`;

                        return axiosInstance(originalRequest);
                    } catch (e) {
                        logoutAndRedirect();
                        return Promise.reject(e);
                    }
                }

                return Promise.reject(error);
            }
        );

        return () => {
            axiosInstance.interceptors.request.eject(reqId);
            axiosInstance.interceptors.response.eject(resId);
        };
    }, [axiosInstance, refreshJwt, logoutAndRedirect]);

    const requestHandler = useCallback(
        async <T = any, >(request: () => Promise<AxiosResponse<T>>): Promise<T> => {
            try {
                const response = await request();
                return response.data;
            } catch (error) {
                if (axios.isAxiosError(error)) {
                    const status = error.response?.status;
                    const data = error.response?.data as ApiErrorKeyResponse;

                    if (status === 401 || status === 403) {
                        toast.error("You don't have permission to perform this action.");
                        throw error;
                    }

                    if (status === 400 && data?.translationKey) {
                        toast.error(data.translationKey);
                        throw error;
                    }

                    toast.error("Something went wrong. Please try again.");
                }
                throw error;
            }
        },
        []
    );

    const api = useMemo(
        () => ({
            get: <T = any, >(url: string, options?: AxiosRequestConfig) =>
                requestHandler(() => axiosInstance.get<T>(url, options)),
            post: <T = any, >(url: string, data?: any, options?: AxiosRequestConfig) =>
                requestHandler(() => axiosInstance.post<T>(url, data, options)),
            put: <T = any, >(url: string, data?: any, options?: AxiosRequestConfig) =>
                requestHandler(() => axiosInstance.put<T>(url, data, options)),
            patch: <T = any, >(url: string, data?: any, options?: AxiosRequestConfig) =>
                requestHandler(() => axiosInstance.patch<T>(url, data, options)),
            delete: <T = any, >(url: string, options?: AxiosRequestConfig) =>
                requestHandler(() => axiosInstance.delete<T>(url, options)),
            fetch: (url: string, options?: RequestInit) =>
                fetchWithAuth(url, options),

            postBlob: async (url: string, data: any): Promise<Blob> => {
                const response = await axiosInstance.post(url, data, { responseType: "blob" });
                return response.data as Blob;
            },

            uploadFile: <T = any, >(url: string, formData: FormData, options?: AxiosRequestConfig) =>
                requestHandler(() =>
                    axiosInstance.post<T>(url, formData, {
                        ...options,
                        headers: {
                            ...(options?.headers || {}),
                            "Content-Type": "multipart/form-data",
                        },
                    })
                ),

            postStream,
        }),
        [axiosInstance, requestHandler, fetchWithAuth, postStream]
    );

    return (
        <AxiosContext.Provider
            value={{
                jwt,
                axiosLogin,
                axiosDefault,
                setJwt,
                ensureValidJwt,
                api,
            }}
        >
            {children}
        </AxiosContext.Provider>
    );
};
