export type ConfigValue = {
    apiServerUrl: string;
};

export const CONFIG: ConfigValue = {
    apiServerUrl: import.meta.env.VITE_API_SERVER_URL ?? '',
};