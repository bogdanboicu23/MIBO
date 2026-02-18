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
import { useTranslation } from "react-i18next";
import { useRef, useMemo, useState, useEffect, useCallback } from "react";

import { paths } from "src/routes/paths";
import { useRouter } from "src/routes/hooks";

import { CONFIG } from "src/global-config";

import { endpoints } from "./endpoints";
import { AxiosContext } from "./context/axios-context";
import { getJwt, JWT_STORAGE_KEY } from "@/auth/context/jwt";
import { i18n_keys } from "@/app/providers/I18nProvider.tsx";

type RefreshResponse = { jwtToken: string };

type RetriableRequestConfig = InternalAxiosRequestConfig & {
    _retry?: boolean;
    _skipToast?: boolean;
};

const getTimeZone = () => Intl.DateTimeFormat().resolvedOptions().timeZone;

type ApiErrorBody = { message?: string };

export const AxiosProvider = ({ children }: { children: ReactNode }) => {
    const router = useRouter();
    const { t } = useTranslation();

    // Keep a state for UI / context consumers
    const [jwt, setJwtState] = useState<string | undefined>(getJwt());

    // Single source of truth for token used by interceptors (avoids races)
    const jwtRef = useRef<string | undefined>(jwt);
    useEffect(() => {
        jwtRef.current = jwt;
    }, [jwt]);

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
        toast.error(t(i18n_keys.toast.sessionExpired));
        if (router?.push) router.push(paths.auth.login);
    }, [router, setJwt, t]);

    const refreshJwt = useCallback(async (): Promise<string> => {
        if (refreshPromiseRef.current) return refreshPromiseRef.current;

        const doRefresh = (async () => {
            const currentJwt = jwtRef.current || getJwt();

            if (!currentJwt) {
                throw new Error("Missing JWT for refresh.");
            }

            const response = await axiosLogin.post<RefreshResponse>(
                endpoints.auth.refresh,
                { jwtToken: currentJwt }
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
                        toast.warning(t(i18n_keys.toast.maintenanceWarning));
                    }
                    return Promise.reject(error);
                }

                const status = error.response?.status;

                if (status === 403) {
                    if (!originalRequest._skipToast) {
                        toast.error(t(i18n_keys.toast.permissionDenied))
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
    }, [axiosInstance, refreshJwt, logoutAndRedirect, t]);

    const requestHandler = useCallback(
        async <T = any,>(request: () => Promise<AxiosResponse<T>>): Promise<T> => {
            try {
                const response = await request();
                return response.data;
            } catch (error) {
                if (axios.isAxiosError(error)) {
                    const status = error.response?.status;
                    const data = error.response?.data as ApiErrorKeyResponse;

                    if (status === 401 || status === 403) {
                        toast.error(t(i18n_keys.toast.permissionDenied));
                    } else if (status === 400 && data?.translationKey) {
                        toast.error(t(data.translationKey));
                        throw error;
                    }
                    toast.error(t(i18n_keys.toast.requestError));
                }
                throw error;
            }
        },
        [t]
    );

    const api = useMemo(
        () => ({
            get: <T = any,>(url: string, options?: AxiosRequestConfig) =>
                requestHandler(() => axiosInstance.get<T>(url, options)),
            post: <T = any,>(url: string, data?: any, options?: AxiosRequestConfig) =>
                requestHandler(() => axiosInstance.post<T>(url, data, options)),
            put: <T = any,>(url: string, data?: any, options?: AxiosRequestConfig) =>
                requestHandler(() => axiosInstance.put<T>(url, data, options)),
            patch: <T = any,>(url: string, data?: any, options?: AxiosRequestConfig) =>
                requestHandler(() => axiosInstance.patch<T>(url, data, options)),
            delete: <T = any,>(url: string, options?: AxiosRequestConfig) =>
                requestHandler(() => axiosInstance.delete<T>(url, options)),

            postBlob: async (url: string, data: any): Promise<Blob> => {
                const response = await axiosInstance.post(url, data, { responseType: "blob" });
                return response.data as Blob;
            },

            uploadFile: <T = any,>(url: string, formData: FormData, options?: AxiosRequestConfig) =>
                requestHandler(() =>
                    axiosInstance.post<T>(url, formData, {
                        ...options,
                        headers: {
                            ...(options?.headers || {}),
                            "Content-Type": "multipart/form-data",
                        },
                    })
                ),
        }),
        [axiosInstance, requestHandler]
    );

    return (
        <AxiosContext.Provider
            value={{
                jwt,
                axiosLogin,
                axiosDefault,
                setJwt,
                api,
            }}
        >
            {children}
        </AxiosContext.Provider>
    );
};
