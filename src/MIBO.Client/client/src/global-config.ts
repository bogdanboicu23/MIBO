export type ConfigValue = {
    apiServerUrl: string;
    hubServerUrl: string;
};

export const CONFIG: ConfigValue = {
    apiServerUrl: import.meta.env.VITE_API_SERVER_URL ?? '',
    hubServerUrl: import.meta.env.VITE_HUB_SERVER_URL ?? "",
};