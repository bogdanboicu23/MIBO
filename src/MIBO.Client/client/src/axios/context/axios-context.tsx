import type { AxiosInstance } from "axios";

import { createContext } from "react";

interface ApiClient {
    get: <T = any>(url: string, options?: any) => Promise<T>;
    post: <T = any>(url: string, data?: any) => Promise<T>;
    put: <T = any>(url: string, data?: any) => Promise<T>;
    patch: <T = any>(url: string, data?: any) => Promise<T>;
    delete: <T = any>(url: string) => Promise<T>;
    postBlob: (url: string, data: any) => Promise<Blob>;
    uploadFile: <T = any>(url: string, formData: FormData) => Promise<T>;
    postStream: (
        url: string,
        data: any,
        options: {
            signal?: AbortSignal;
            onToken: (token: string) => void;
            onDone?: () => void;
            onError?: (err: unknown) => void;
        }
    ) => Promise<void>;
}

interface AxiosContextProps {
    jwt: string | undefined;
    axiosLogin: AxiosInstance;
    axiosDefault: AxiosInstance;
    setJwt: (jwt: string) => void;
    api: ApiClient;
}

export const AxiosContext = createContext<AxiosContextProps | undefined>(undefined);
