export type ConfigValue = {
    apiServerUrl: string;
    hubServerUrl: string;
    dataServiceUrl: string;
    dummyJsonUrl: string;
};

export const CONFIG: ConfigValue = {
    apiServerUrl: import.meta.env.VITE_API_SERVER_URL ?? "https://localhost:7286",
    hubServerUrl: import.meta.env.VITE_HUB_SERVER_URL ?? "",
    dataServiceUrl: import.meta.env.VITE_DATA_SERVICE_URL
        ?? import.meta.env.VITE_API_SERVER_URL
        ?? "https://localhost:7286",
    dummyJsonUrl: import.meta.env.VITE_DUMMYJSON_URL ?? "https://dummyjson.com",
};
