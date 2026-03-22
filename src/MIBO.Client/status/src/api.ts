export type ServiceState = "monitoring" | "operational" | "degraded" | "outage";

export type ExternalServiceStatusItem = {
    serviceKey: string;
    serviceName: string;
    displayName: string;
    baseUrl: string;
    status: ServiceState;
    lastCheckedAtUtc: string | null;
    lastSuccessAtUtc: string | null;
    lastFailureAtUtc: string | null;
    openIncidentSinceUtc: string | null;
    consecutiveFailures: number;
    lastDurationMs: number | null;
    lastErrorMessage: string | null;
    lastHandler: string | null;
    lastCorrelationId: string | null;
    lastOrigin: string | null;
    totalRequests: number;
    totalFailures: number;
};

export type ExternalServiceAuditItem = {
    serviceKey: string;
    serviceName: string;
    displayName: string;
    handler: string;
    correlationId: string;
    origin: string;
    outcome: "succeeded" | "failed" | "failed_queued";
    attempt: number;
    localRetryCount: number;
    maxAttempts: number;
    durationMs: number;
    queuedRetry: boolean;
    createdAtUtc: string;
    nextRetryAtUtc: string | null;
    errorType: string | null;
    errorMessage: string | null;
};

export type ExternalServiceStatusCounts = {
    total: number;
    monitoring: number;
    operational: number;
    degraded: number;
    outage: number;
};

export type ExternalServiceStatusSummary = {
    enabled: boolean;
    generatedAtUtc: string;
    overallStatus: ServiceState;
    counts: ExternalServiceStatusCounts;
    services: ExternalServiceStatusItem[];
    audits: ExternalServiceAuditItem[];
};

const defaultBaseUrl = "http://localhost:8080";
const configuredBaseUrl = import.meta.env.VITE_API_SERVER_URL || defaultBaseUrl;
const apiBaseUrl = configuredBaseUrl.replace(/\/+$/, "");

export async function fetchStatusSummary(signal?: AbortSignal): Promise<ExternalServiceStatusSummary> {
    const response = await fetch(`${apiBaseUrl}/api/actions/status/summary?auditLimit=1000`, {
        method: "GET",
        headers: {
            Accept: "application/json",
        },
        signal,
    });

    if (!response.ok) {
        throw new Error(`Status API returned ${response.status}.`);
    }

    return response.json() as Promise<ExternalServiceStatusSummary>;
}
