import { startTransition, useDeferredValue, useEffect, useState } from "react";
import {
    type ExternalServiceAuditItem,
    type ExternalServiceStatusItem,
    type ExternalServiceStatusSummary,
    fetchStatusSummary,
} from "./api";
import { useTheme } from "./useTheme";

type AuditMode = "unsuccessful" | "failed" | "queued" | "all";
type ActivityState = "empty" | "operational" | "queued" | "failed";
type IncidentState = "degraded" | "outage" | "resolved";
type ThemeMode = "light" | "dark";
type ComponentKind = "platform" | "agent" | "service";
type ComponentKey = "all" | "platform" | "ai-agent" | string;

type ActivitySlot = {
    key: string;
    hourKey: string;
    label: string;
    state: ActivityState;
    eventCount: number;
    failureCount: number;
    queuedCount: number;
    successCount: number;
    summary: string;
};

type StatusComponent = {
    componentKey: Exclude<ComponentKey, "all">;
    kind: ComponentKind;
    serviceName: string;
    displayName: string;
    baseUrl: string;
    status: string;
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

type IncidentItem = {
    serviceKey: string;
    displayName: string;
    title: string;
    summary: string;
    state: IncidentState;
    startedAtUtc: string;
    resolvedAtUtc: string | null;
    lastUpdatedAtUtc: string;
    eventCount: number;
    handler: string | null;
    correlationId: string | null;
    nextRetryAtUtc: string | null;
    interactive: boolean;
};

const hourlyWindowHours = 24;

const statusLabel: Record<string, string> = {
    monitoring: "Monitoring",
    operational: "Operational",
    degraded: "Degraded",
    outage: "Major outage",
};

const statusTone: Record<string, string> = {
    monitoring: "border-zinc-200 bg-zinc-100 text-zinc-700 dark:border-white/10 dark:bg-white/10 dark:text-zinc-200",
    operational: "border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-500/20 dark:bg-emerald-500/10 dark:text-emerald-300",
    degraded: "border-amber-200 bg-amber-50 text-amber-700 dark:border-amber-500/20 dark:bg-amber-500/10 dark:text-amber-300",
    outage: "border-rose-200 bg-rose-50 text-rose-700 dark:border-rose-500/20 dark:bg-rose-500/10 dark:text-rose-300",
};

const bannerTone: Record<string, string> = {
    monitoring: "border-zinc-200 bg-white text-zinc-900 dark:border-white/10 dark:bg-white/[0.04] dark:text-zinc-100",
    operational: "border-emerald-200 bg-emerald-50/70 text-emerald-950 dark:border-emerald-500/20 dark:bg-emerald-500/10 dark:text-emerald-100",
    degraded: "border-amber-200 bg-amber-50/80 text-amber-950 dark:border-amber-500/20 dark:bg-amber-500/10 dark:text-amber-100",
    outage: "border-rose-200 bg-rose-50/80 text-rose-950 dark:border-rose-500/20 dark:bg-rose-500/10 dark:text-rose-100",
};

const outcomeTone: Record<string, string> = {
    succeeded: "border-emerald-200 bg-emerald-50 text-emerald-700 dark:border-emerald-500/20 dark:bg-emerald-500/10 dark:text-emerald-300",
    failed: "border-rose-200 bg-rose-50 text-rose-700 dark:border-rose-500/20 dark:bg-rose-500/10 dark:text-rose-300",
    failed_queued: "border-amber-200 bg-amber-50 text-amber-700 dark:border-amber-500/20 dark:bg-amber-500/10 dark:text-amber-300",
};

const outcomeLabel: Record<string, string> = {
    succeeded: "Recovered",
    failed: "Failed",
    failed_queued: "Retry queued",
};

const activityTone: Record<ActivityState, string> = {
    empty: "bg-zinc-200 dark:bg-white/10",
    operational: "bg-emerald-400",
    queued: "bg-amber-400",
    failed: "bg-rose-400",
};

const incidentTone: Record<IncidentState, string> = {
    degraded: "border-amber-200 bg-amber-50/70 text-amber-800 dark:border-amber-500/20 dark:bg-amber-500/10 dark:text-amber-300",
    outage: "border-rose-200 bg-rose-50/70 text-rose-800 dark:border-rose-500/20 dark:bg-rose-500/10 dark:text-rose-300",
    resolved: "border-emerald-200 bg-emerald-50/70 text-emerald-800 dark:border-emerald-500/20 dark:bg-emerald-500/10 dark:text-emerald-300",
};

const shellCardClass =
    "rounded-[28px] border border-zinc-200 bg-white/95 shadow-[0_18px_60px_-32px_rgba(15,23,42,0.32)] dark:border-white/10 dark:bg-zinc-950/80 dark:shadow-[0_30px_90px_-45px_rgba(0,0,0,0.85)]";

const softCardClass =
    "rounded-2xl border border-zinc-200 bg-white/80 dark:border-white/10 dark:bg-white/[0.04]";

function formatAbsoluteDate(value: string | null) {
    if (!value) {
        return "Never";
    }

    return new Intl.DateTimeFormat("en-GB", {
        dateStyle: "medium",
        timeStyle: "short",
    }).format(new Date(value));
}

function formatRelativeDate(value: string | null) {
    if (!value) {
        return "Never";
    }

    const deltaMinutes = Math.max(0, Math.round((Date.now() - new Date(value).getTime()) / 60000));
    if (deltaMinutes < 1) return "just now";
    if (deltaMinutes < 60) return `${deltaMinutes} min ago`;

    const deltaHours = Math.round(deltaMinutes / 60);
    if (deltaHours < 24) return `${deltaHours} h ago`;

    const deltaDays = Math.round(deltaHours / 24);
    return `${deltaDays} d ago`;
}

function formatTimeOnly(value: string) {
    return new Intl.DateTimeFormat("en-GB", {
        hour: "2-digit",
        minute: "2-digit",
    }).format(new Date(value));
}

function formatHourTick(value: Date) {
    return new Intl.DateTimeFormat("en-GB", {
        day: "2-digit",
        month: "short",
        hour: "2-digit",
        minute: "2-digit",
    }).format(value);
}

function formatActivitySummary(slot: Pick<ActivitySlot, "state" | "eventCount" | "failureCount" | "queuedCount" | "successCount">) {
    if (slot.state === "failed") {
        return slot.failureCount === 1 ? "1 failure recorded" : `${slot.failureCount} failures recorded`;
    }

    if (slot.state === "queued") {
        if (slot.failureCount > 0) {
            return `${slot.failureCount} failures and retries queued`;
        }

        return slot.queuedCount === 1 ? "1 retry queued" : `${slot.queuedCount} retries queued`;
    }

    if (slot.state === "operational") {
        return "No incidents";
    }

    return "No activity recorded";
}

function formatDayLabel(value: string) {
    return new Intl.DateTimeFormat("en-GB", {
        month: "short",
        day: "numeric",
    }).format(new Date(value));
}

function formatWeekday(value: string) {
    return new Intl.DateTimeFormat("en-GB", {
        weekday: "short",
    }).format(new Date(value));
}

function formatOrigin(origin: string | null) {
    if (!origin) {
        return "Unknown origin";
    }

    return origin.replaceAll("_", " ");
}

function getAvailability(item: { totalRequests: number; totalFailures: number }) {
    if (item.totalRequests === 0) {
        return null;
    }

    return ((1 - item.totalFailures / item.totalRequests) * 100).toFixed(2);
}

function getBannerCopy(summary: ExternalServiceStatusSummary | null, incidents: StatusComponent[]) {
    if (!summary?.enabled) {
        return {
            title: "Monitoring pipeline is not enabled",
            description: "Enable retry monitoring in AppHost.",
        };
    }

    if (summary.overallStatus === "operational") {
        return {
            title: "All monitored services are operational",
            description: null,
        };
    }

    if (summary.overallStatus === "degraded") {
        return {
            title: `${incidents.length} component${incidents.length === 1 ? "" : "s"} currently degraded`,
            description: null,
        };
    }

    if (summary.overallStatus === "outage") {
        return {
            title: "One or more components are experiencing a major outage",
            description: null,
        };
    }

    return {
        title: "Status monitoring is warming up",
        description: null,
    };
}

function maxDate(values: Array<string | null | undefined>) {
    const timestamps = values
        .filter((value): value is string => Boolean(value))
        .map((value) => new Date(value).getTime());

    if (timestamps.length === 0) {
        return null;
    }

    return new Date(Math.max(...timestamps)).toISOString();
}

function minDate(values: Array<string | null | undefined>) {
    const timestamps = values
        .filter((value): value is string => Boolean(value))
        .map((value) => new Date(value).getTime());

    if (timestamps.length === 0) {
        return null;
    }

    return new Date(Math.min(...timestamps)).toISOString();
}

function startOfHour(value: Date) {
    const date = new Date(value);
    date.setMinutes(0, 0, 0);
    return date;
}

function toHourKey(value: string | Date) {
    const date = startOfHour(typeof value === "string" ? new Date(value) : value);
    return date.toISOString();
}

function sameHour(left: string | Date | null, right: Date) {
    if (!left) {
        return false;
    }

    return toHourKey(left) === toHourKey(right);
}

function statusToActivityState(status: string): ActivityState {
    if (status === "outage") {
        return "failed";
    }

    if (status === "degraded") {
        return "queued";
    }

    if (status === "operational") {
        return "operational";
    }

    return "empty";
}

function getLatestAudit(
    audits: ExternalServiceAuditItem[],
    predicate: (audit: ExternalServiceAuditItem) => boolean,
) {
    return audits.find(predicate) ?? null;
}

function matchesAuditFilter(componentKey: ComponentKey, audit: ExternalServiceAuditItem) {
    if (componentKey === "all") {
        return true;
    }

    return audit.serviceKey === componentKey;
}

function matchesHourSelection(selectedHourKey: string | null, ...values: Array<string | null | undefined>) {
    if (!selectedHourKey) {
        return true;
    }

    return values.some((value) => value ? toHourKey(value) === selectedHourKey : false);
}

function buildSyntheticComponents(
    summary: ExternalServiceStatusSummary | null,
    services: ExternalServiceStatusItem[],
    audits: ExternalServiceAuditItem[],
): StatusComponent[] {
    const existingKeys = new Set(services.map((service) => service.serviceKey.toLowerCase()));
    const latestFailure = getLatestAudit(audits, (audit) => audit.outcome !== "succeeded");
    const latestAudit = audits[0] ?? null;
    const interactiveAudits = audits.filter((audit) => audit.origin === "interactive");
    const interactiveFailures = interactiveAudits.filter((audit) => audit.outcome !== "succeeded");
    const interactiveLatestFailure = getLatestAudit(interactiveAudits, (audit) => audit.outcome !== "succeeded");
    const interactiveLatestSuccess = getLatestAudit(interactiveAudits, (audit) => audit.outcome === "succeeded");
    const interactiveServices = services.filter((service) =>
        service.lastOrigin === "interactive"
        || interactiveAudits.some((audit) => audit.serviceKey === service.serviceKey),
    );

    const hasInteractiveOutage = interactiveServices.some((service) => service.status === "outage");
    const hasInteractiveDegraded = interactiveServices.some((service) => service.status === "degraded");

    const aiStatus = !summary?.enabled
        ? "monitoring"
        : hasInteractiveOutage
            ? "outage"
            : hasInteractiveDegraded || interactiveFailures.some((audit) => audit.outcome === "failed_queued")
                ? "degraded"
                : interactiveAudits.length > 0
                    ? "operational"
                    : "monitoring";

    const platform: StatusComponent = {
        componentKey: "platform",
        kind: "platform",
        serviceName: "MIBO Platform",
        displayName: "MIBO Platform",
        baseUrl: "api-gateway / conversation / action / identity / langchain",
        status: summary?.enabled ? summary.overallStatus : "monitoring",
        lastCheckedAtUtc: maxDate(services.map((service) => service.lastCheckedAtUtc)),
        lastSuccessAtUtc: maxDate(services.map((service) => service.lastSuccessAtUtc)),
        lastFailureAtUtc: maxDate(services.map((service) => service.lastFailureAtUtc)),
        openIncidentSinceUtc: minDate(services.map((service) => service.openIncidentSinceUtc)),
        consecutiveFailures: Math.max(0, ...services.map((service) => service.consecutiveFailures)),
        lastDurationMs: null,
        lastErrorMessage: latestFailure?.errorMessage ?? null,
        lastHandler: latestAudit?.handler ?? null,
        lastCorrelationId: latestAudit?.correlationId ?? null,
        lastOrigin: latestAudit?.origin ?? null,
        totalRequests: services.reduce((sum, service) => sum + service.totalRequests, 0),
        totalFailures: services.reduce((sum, service) => sum + service.totalFailures, 0),
    };

    const aiAgent: StatusComponent = {
        componentKey: "ai-agent",
        kind: "agent",
        serviceName: "AI Agent",
        displayName: "AI Agent",
        baseUrl: "langchain-service / action-service / interactive requests",
        status: aiStatus,
        lastCheckedAtUtc: maxDate([
            ...interactiveServices.map((service) => service.lastCheckedAtUtc),
            interactiveAudits[0]?.createdAtUtc ?? null,
        ]),
        lastSuccessAtUtc: interactiveLatestSuccess?.createdAtUtc ?? null,
        lastFailureAtUtc: interactiveLatestFailure?.createdAtUtc ?? null,
        openIncidentSinceUtc: aiStatus === "operational"
            ? null
            : minDate([
                ...interactiveServices.map((service) => service.openIncidentSinceUtc),
                interactiveLatestFailure?.createdAtUtc ?? null,
            ]),
        consecutiveFailures: interactiveFailures.length,
        lastDurationMs: interactiveAudits[0]?.durationMs ?? null,
        lastErrorMessage: aiStatus !== "degraded" && aiStatus !== "outage"
            ? null
            : interactiveLatestFailure?.errorMessage ?? "Interactive requests are retrying external calls.",
        lastHandler: interactiveAudits[0]?.handler ?? null,
        lastCorrelationId: interactiveAudits[0]?.correlationId ?? null,
        lastOrigin: interactiveAudits[0]?.origin ?? null,
        totalRequests: interactiveAudits.length,
        totalFailures: interactiveFailures.length,
    };

    const components: StatusComponent[] = [];

    if (!existingKeys.has("platform")) {
        components.push(platform);
    }

    if (!existingKeys.has("ai-agent")) {
        components.push(aiAgent);
    }

    return components;
}

function buildComponents(
    summary: ExternalServiceStatusSummary | null,
    services: ExternalServiceStatusItem[],
    audits: ExternalServiceAuditItem[],
) {
    const serviceComponents: StatusComponent[] = services.map((service) => ({
        componentKey: service.serviceKey,
        kind: "service",
        serviceName: service.serviceName,
        displayName: service.displayName,
        baseUrl: service.baseUrl,
        status: service.status,
        lastCheckedAtUtc: service.lastCheckedAtUtc,
        lastSuccessAtUtc: service.lastSuccessAtUtc,
        lastFailureAtUtc: service.lastFailureAtUtc,
        openIncidentSinceUtc: service.openIncidentSinceUtc,
        consecutiveFailures: service.consecutiveFailures,
        lastDurationMs: service.lastDurationMs,
        lastErrorMessage: service.lastErrorMessage,
        lastHandler: service.lastHandler,
        lastCorrelationId: service.lastCorrelationId,
        lastOrigin: service.lastOrigin,
        totalRequests: service.totalRequests,
        totalFailures: service.totalFailures,
    }));

    return [
        ...buildSyntheticComponents(summary, services, audits),
        ...serviceComponents,
    ].sort((left, right) => {
        const leftPriority = left.componentKey === "platform" ? 0 : left.componentKey === "ai-agent" ? 1 : 10;
        const rightPriority = right.componentKey === "platform" ? 0 : right.componentKey === "ai-agent" ? 1 : 10;
        return leftPriority - rightPriority || left.displayName.localeCompare(right.displayName);
    });
}

function buildHourlyActivitySlots(
    component: StatusComponent,
    audits: ExternalServiceAuditItem[],
    generatedAtUtc: string | null,
    hourCount = hourlyWindowHours,
): ActivitySlot[] {
    const anchor = generatedAtUtc ? new Date(generatedAtUtc) : new Date();
    const endHour = startOfHour(anchor);
    const componentAudits = audits.filter((audit) => matchesAuditFilter(component.componentKey, audit));
    const statsByHour = new Map<string, {
        failureCount: number;
        queuedCount: number;
        successCount: number;
    }>();

    for (const audit of componentAudits) {
        const hourKey = toHourKey(audit.createdAtUtc);
        const current = statsByHour.get(hourKey) ?? {
            failureCount: 0,
            queuedCount: 0,
            successCount: 0,
        };

        if (audit.outcome === "failed") {
            current.failureCount += 1;
        } else if (audit.outcome === "failed_queued") {
            current.queuedCount += 1;
        } else {
            current.successCount += 1;
        }

        statsByHour.set(hourKey, current);
    }

    return Array.from({ length: hourCount }, (_, index) => {
        const slotDate = new Date(endHour);
        slotDate.setHours(slotDate.getHours() - (hourCount - index - 1));
        const hourKey = slotDate.toISOString();
        const slotStats = statsByHour.get(hourKey) ?? {
            failureCount: 0,
            queuedCount: 0,
            successCount: 0,
        };

        let state: ActivityState =
            slotStats.failureCount > 0
                ? "failed"
                : slotStats.queuedCount > 0
                    ? "queued"
                    : slotStats.successCount > 0
                        ? "operational"
                        : "empty";

        if (state === "empty" && index === hourCount - 1 && sameHour(component.lastCheckedAtUtc, slotDate)) {
            state = statusToActivityState(component.status);
        }

        const eventCount = slotStats.failureCount + slotStats.queuedCount + slotStats.successCount;
        return {
            key: `${component.componentKey}-${hourKey}`,
            hourKey,
            label: formatHourTick(slotDate),
            state,
            eventCount,
            failureCount: slotStats.failureCount,
            queuedCount: slotStats.queuedCount,
            successCount: slotStats.successCount,
            summary: formatActivitySummary({
                state,
                eventCount,
                failureCount: slotStats.failureCount,
                queuedCount: slotStats.queuedCount,
                successCount: slotStats.successCount,
            }),
        };
    });
}

function buildIncidentTimeline(services: ExternalServiceStatusItem[], audits: ExternalServiceAuditItem[]): IncidentItem[] {
    const byService = new Map(services.map((service) => [service.serviceKey, service]));
    const openIncidents = new Map<string, IncidentItem>();
    const incidents: IncidentItem[] = [];
    const orderedAudits = [...audits].sort((left, right) => new Date(left.createdAtUtc).getTime() - new Date(right.createdAtUtc).getTime());

    for (const audit of orderedAudits) {
        const service = byService.get(audit.serviceKey);
        const current = openIncidents.get(audit.serviceKey);

        if (audit.outcome === "succeeded") {
            if (!current) {
                continue;
            }

            incidents.push({
                ...current,
                state: "resolved",
                resolvedAtUtc: audit.createdAtUtc,
                lastUpdatedAtUtc: audit.createdAtUtc,
                interactive: current.interactive || audit.origin === "interactive",
            });
            openIncidents.delete(audit.serviceKey);
            continue;
        }

        const nextState: IncidentState = audit.outcome === "failed" ? "outage" : "degraded";
        const nextTitle =
            nextState === "outage"
                ? `${service?.displayName ?? audit.displayName} is failing requests`
                : `${service?.displayName ?? audit.displayName} is retrying requests`;
        const nextSummary =
            audit.errorMessage
            ?? (audit.outcome === "failed_queued"
                ? "Polly exhausted local retries and a delayed retry was queued in RabbitMQ."
                : "The external request did not complete successfully.");

        if (!current) {
            openIncidents.set(audit.serviceKey, {
                serviceKey: audit.serviceKey,
                displayName: service?.displayName ?? audit.displayName,
                title: nextTitle,
                summary: nextSummary,
                state: nextState,
                startedAtUtc: service?.openIncidentSinceUtc ?? audit.createdAtUtc,
                resolvedAtUtc: null,
                lastUpdatedAtUtc: audit.createdAtUtc,
                eventCount: 1,
                handler: audit.handler,
                correlationId: audit.correlationId,
                nextRetryAtUtc: audit.nextRetryAtUtc,
                interactive: audit.origin === "interactive",
            });
            continue;
        }

        openIncidents.set(audit.serviceKey, {
            ...current,
            title: nextTitle,
            summary: audit.errorMessage ?? current.summary,
            state: current.state === "outage" || nextState === "outage" ? "outage" : "degraded",
            lastUpdatedAtUtc: audit.createdAtUtc,
            eventCount: current.eventCount + 1,
            handler: audit.handler,
            correlationId: audit.correlationId,
            nextRetryAtUtc: audit.nextRetryAtUtc ?? current.nextRetryAtUtc,
            interactive: current.interactive || audit.origin === "interactive",
        });
    }

    for (const [serviceKey, incident] of openIncidents) {
        const service = byService.get(serviceKey);
        incidents.push({
            ...incident,
            title:
                service?.status === "outage"
                    ? `${service.displayName} is failing requests`
                    : incident.title,
            summary: service?.lastErrorMessage ?? incident.summary,
            state: service?.status === "outage" ? "outage" : incident.state,
            startedAtUtc: service?.openIncidentSinceUtc ?? incident.startedAtUtc,
            lastUpdatedAtUtc: service?.lastFailureAtUtc ?? incident.lastUpdatedAtUtc,
            interactive: incident.interactive || service?.lastOrigin === "interactive",
        });
    }

    for (const service of services) {
        const isOpen = service.status === "degraded" || service.status === "outage";
        const alreadyTracked = incidents.some((incident) => incident.serviceKey === service.serviceKey && incident.resolvedAtUtc === null);
        if (!isOpen || alreadyTracked) {
            continue;
        }

        incidents.push({
            serviceKey: service.serviceKey,
            displayName: service.displayName,
            title: `${service.displayName} needs attention`,
            summary: service.lastErrorMessage ?? "Recent health checks indicate repeated issues with this integration.",
            state: service.status === "outage" ? "outage" : "degraded",
            startedAtUtc: service.openIncidentSinceUtc ?? service.lastFailureAtUtc ?? service.lastCheckedAtUtc ?? new Date().toISOString(),
            resolvedAtUtc: null,
            lastUpdatedAtUtc: service.lastFailureAtUtc ?? service.lastCheckedAtUtc ?? new Date().toISOString(),
            eventCount: Math.max(service.consecutiveFailures, 1),
            handler: service.lastHandler,
            correlationId: service.lastCorrelationId,
            nextRetryAtUtc: null,
            interactive: service.lastOrigin === "interactive",
        });
    }

    return incidents.sort((left, right) => {
        const leftTime = new Date(left.resolvedAtUtc ?? left.lastUpdatedAtUtc).getTime();
        const rightTime = new Date(right.resolvedAtUtc ?? right.lastUpdatedAtUtc).getTime();
        return rightTime - leftTime;
    });
}

function matchesIncidentFilter(componentKey: ComponentKey, incident: IncidentItem) {
    if (componentKey === "all") {
        return true;
    }

    return incident.serviceKey === componentKey;
}

function groupIncidentsByDay(incidents: IncidentItem[]) {
    const groups = new Map<string, { key: string; dayLabel: string; weekday: string; items: IncidentItem[] }>();

    for (const incident of incidents) {
        const anchor = incident.resolvedAtUtc ?? incident.lastUpdatedAtUtc;
        const date = new Date(anchor);
        const key = date.toISOString().slice(0, 10);
        const current = groups.get(key);

        if (current) {
            current.items.push(incident);
            continue;
        }

        groups.set(key, {
            key,
            dayLabel: formatDayLabel(anchor),
            weekday: formatWeekday(anchor),
            items: [incident],
        });
    }

    return Array.from(groups.values());
}

function scrollToSection(id: string) {
    document.getElementById(id)?.scrollIntoView({ behavior: "smooth", block: "start" });
}

function StatusPill({ value }: { value: string }) {
    return (
        <span className={`inline-flex items-center gap-2 rounded-full border px-3 py-1 text-xs font-semibold ${statusTone[value] ?? statusTone.monitoring}`}>
            <span className="h-2 w-2 rounded-full bg-current/70" />
            {statusLabel[value] ?? value}
        </span>
    );
}

function ThemeToggle({
    theme,
    onToggle,
}: {
    theme: ThemeMode;
    onToggle: () => void;
}) {
    return (
        <button
            type="button"
            onClick={onToggle}
            className="inline-flex items-center gap-3 rounded-full border border-zinc-200 bg-white/90 px-3 py-2 text-sm font-medium text-zinc-700 transition hover:border-zinc-300 hover:text-zinc-950 dark:border-white/10 dark:bg-white/[0.05] dark:text-zinc-200 dark:hover:border-white/20 dark:hover:text-white"
            aria-label={`Switch to ${theme === "dark" ? "light" : "dark"} mode`}
        >
            <span
                className={`relative inline-flex h-6 w-11 items-center rounded-full border transition ${
                    theme === "dark"
                        ? "border-emerald-500/20 bg-emerald-500/15"
                        : "border-zinc-200 bg-zinc-100"
                }`}
            >
                <span
                    className={`absolute h-4 w-4 rounded-full shadow-sm transition ${
                        theme === "dark"
                            ? "translate-x-6 bg-emerald-300"
                            : "translate-x-1 bg-white"
                    }`}
                />
            </span>
            <span>{theme === "dark" ? "Dark" : "Light"}</span>
        </button>
    );
}

function OutcomePill({ value }: { value: ExternalServiceAuditItem["outcome"] }) {
    return (
        <span className={`inline-flex rounded-full border px-2.5 py-1 text-[11px] font-semibold ${outcomeTone[value]}`}>
            {outcomeLabel[value]}
        </span>
    );
}

function FilterChip({
    active,
    label,
    onClick,
}: {
    active: boolean;
    label: string;
    onClick: () => void;
}) {
    return (
        <button
            type="button"
            onClick={onClick}
            className={`rounded-full border px-3 py-1.5 text-sm font-medium transition ${
                active
                    ? "border-zinc-900 bg-zinc-900 text-white dark:border-emerald-400/20 dark:bg-emerald-400/15 dark:text-emerald-100"
                    : "border-zinc-200 bg-white text-zinc-600 hover:border-zinc-300 hover:text-zinc-950 dark:border-white/10 dark:bg-white/[0.04] dark:text-zinc-300 dark:hover:border-white/20 dark:hover:text-white"
            }`}
        >
            {label}
        </button>
    );
}

function MetricBadge({ label, value }: { label: string; value: string }) {
    return (
        <div className={`${softCardClass} px-4 py-3`}>
            <p className="text-[11px] font-semibold uppercase tracking-[0.18em] text-zinc-400 dark:text-zinc-500">{label}</p>
            <p className="mt-1 text-lg font-semibold tracking-tight text-zinc-950 dark:text-zinc-50">{value}</p>
        </div>
    );
}

function EmptyPanel({ title, description }: { title: string; description: string }) {
    return (
        <div className="rounded-3xl border border-dashed border-zinc-200 bg-zinc-50/70 px-6 py-10 text-center dark:border-white/10 dark:bg-white/[0.03]">
            <h3 className="text-base font-semibold text-zinc-900 dark:text-zinc-100">{title}</h3>
            <p className="mt-2 text-sm text-zinc-500 dark:text-zinc-400">{description}</p>
        </div>
    );
}

function ActivityBarButton({
    component,
    slot,
    selected,
    onClick,
}: {
    component: StatusComponent;
    slot: ActivitySlot;
    selected: boolean;
    onClick: () => void;
}) {
    return (
        <button
            type="button"
            onClick={onClick}
            aria-label={`${component.displayName} ${slot.label}: ${slot.summary}`}
            className={`group relative h-9 flex-1 rounded-[6px] transition focus:outline-none focus-visible:-translate-y-0.5 focus-visible:ring-2 focus-visible:ring-emerald-400/50 ${
                selected ? "ring-2 ring-zinc-900/70 dark:ring-emerald-300/70" : "hover:-translate-y-0.5"
            }`}
        >
            <span
                className={`block h-full w-full rounded-[6px] ${activityTone[slot.state]} ${
                    slot.state === "empty" ? "border border-zinc-300/70 dark:border-white/10" : ""
                }`}
            />

            <span className="pointer-events-none absolute left-1/2 top-full z-20 mt-2 w-max min-w-[12rem] -translate-x-1/2 rounded-2xl border border-zinc-200 bg-white px-4 py-3 text-left opacity-0 shadow-[0_18px_35px_-20px_rgba(15,23,42,0.55)] transition duration-150 group-hover:translate-y-0 group-hover:opacity-100 group-focus-visible:translate-y-0 group-focus-visible:opacity-100 dark:border-white/10 dark:bg-zinc-950 dark:shadow-[0_24px_40px_-24px_rgba(0,0,0,0.9)]">
                <span className="block text-xs font-medium text-zinc-500 dark:text-zinc-400">{slot.label}</span>
                <span className="mt-2 flex items-center gap-2 text-sm font-semibold text-zinc-950 dark:text-zinc-50">
                    <span
                        className={`h-2.5 w-2.5 rounded-full ${
                            slot.state === "failed"
                                ? "bg-rose-400"
                                : slot.state === "queued"
                                    ? "bg-amber-400"
                                    : slot.state === "operational"
                                        ? "bg-emerald-400"
                                        : "bg-zinc-400"
                        }`}
                    />
                    {slot.summary}
                </span>
                {slot.eventCount > 0 ? (
                    <span className="mt-1 block text-xs text-zinc-500 dark:text-zinc-400">
                        {slot.eventCount} audit event{slot.eventCount === 1 ? "" : "s"} for {component.displayName}
                    </span>
                ) : null}
            </span>
        </button>
    );
}

function ComponentRow({
    component,
    audits,
    generatedAtUtc,
    selectedHourKey,
    onViewHistory,
    onViewAudit,
    onInspectHour,
}: {
    component: StatusComponent;
    audits: ExternalServiceAuditItem[];
    generatedAtUtc: string | null;
    selectedHourKey: string | null;
    onViewHistory: (componentKey: ComponentKey) => void;
    onViewAudit: (componentKey: ComponentKey) => void;
    onInspectHour: (componentKey: ComponentKey, hourKey: string) => void;
}) {
    const availability = getAvailability(component);
    const slots = buildHourlyActivitySlots(component, audits, generatedAtUtc);

    return (
        <article className="px-6 py-5 transition hover:bg-zinc-50/70 dark:hover:bg-white/[0.02]">
            <div className="flex flex-col gap-4 lg:flex-row lg:items-start lg:justify-between">
                <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-3">
                        <span
                            className={`h-2.5 w-2.5 rounded-full ${
                                component.status === "operational"
                                    ? "bg-emerald-500"
                                    : component.status === "degraded"
                                        ? "bg-amber-500"
                                        : component.status === "outage"
                                            ? "bg-rose-500"
                                            : "bg-zinc-400"
                            }`}
                        />
                        <h3 className="text-base font-semibold text-zinc-950 dark:text-zinc-50">{component.displayName}</h3>
                        <StatusPill value={component.status} />
                    </div>
                    <p className="mt-1 truncate text-sm text-zinc-500 dark:text-zinc-400">{component.baseUrl}</p>
                </div>

                <div className="grid gap-3 sm:grid-cols-3">
                    <MetricBadge label="Uptime" value={availability ? `${availability}%` : "N/A"} />
                    <MetricBadge label="Requests" value={String(component.totalRequests)} />
                    <MetricBadge label="Last checked" value={formatRelativeDate(component.lastCheckedAtUtc)} />
                </div>
            </div>

            <div className="mt-4 flex flex-wrap items-center justify-between gap-3">
                <div className="flex flex-wrap items-center gap-x-4 gap-y-2 text-xs font-medium text-zinc-500 dark:text-zinc-400">
                    <span>Success {formatRelativeDate(component.lastSuccessAtUtc)}</span>
                    <span>{component.openIncidentSinceUtc ? `Incident ${formatAbsoluteDate(component.openIncidentSinceUtc)}` : "No open incident"}</span>
                    <span>Past 24h</span>
                </div>

                <div className="flex flex-wrap gap-2">
                    <button
                        type="button"
                        onClick={() => onViewHistory(component.componentKey)}
                        className="rounded-full border border-zinc-200 bg-white px-3 py-1.5 text-xs font-semibold text-zinc-700 transition hover:border-zinc-300 hover:text-zinc-950 dark:border-white/10 dark:bg-white/[0.04] dark:text-zinc-200 dark:hover:border-white/20 dark:hover:text-white"
                    >
                        View history
                    </button>
                    <button
                        type="button"
                        onClick={() => onViewAudit(component.componentKey)}
                        className="rounded-full border border-zinc-200 bg-white px-3 py-1.5 text-xs font-semibold text-zinc-700 transition hover:border-zinc-300 hover:text-zinc-950 dark:border-white/10 dark:bg-white/[0.04] dark:text-zinc-200 dark:hover:border-white/20 dark:hover:text-white"
                    >
                        View audit
                    </button>
                </div>
            </div>

            <div className={`${softCardClass} mt-4 p-4`}>
                <div className="flex gap-1.5">
                    {slots.map((slot) => (
                        <ActivityBarButton
                            key={slot.key}
                            component={component}
                            slot={slot}
                            selected={selectedHourKey === slot.hourKey}
                            onClick={() => onInspectHour(component.componentKey, slot.hourKey)}
                        />
                    ))}
                </div>
            </div>

            <div className="mt-3 flex flex-wrap items-center gap-x-4 gap-y-2 text-xs text-zinc-500 dark:text-zinc-400">
                <span>Latest handler: {component.lastHandler ?? "No activity yet"}</span>
                <span>Failures: {component.totalFailures}</span>
                <span>Consecutive failures: {component.consecutiveFailures}</span>
            </div>

            {component.lastErrorMessage ? (
                <div className="mt-4 rounded-2xl border border-rose-200 bg-rose-50/70 px-4 py-3 text-sm text-rose-800 dark:border-rose-500/20 dark:bg-rose-500/10 dark:text-rose-200">
                    {component.lastErrorMessage}
                </div>
            ) : null}
        </article>
    );
}

function HistoryEntry({ incident }: { incident: IncidentItem }) {
    return (
        <article className="rounded-2xl border border-zinc-200 bg-zinc-50/70 p-4 dark:border-white/10 dark:bg-white/[0.03]">
            <div className="flex flex-col gap-3 md:flex-row md:items-start md:justify-between">
                <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-2">
                        <span className={`inline-flex rounded-full border px-2.5 py-1 text-[11px] font-semibold ${incidentTone[incident.state]}`}>
                            {incident.state === "resolved" ? "Resolved" : incident.state === "outage" ? "Major outage" : "Degraded"}
                        </span>
                        <h3 className="text-sm font-semibold text-zinc-950 dark:text-zinc-50">{incident.title}</h3>
                    </div>
                    <p className="mt-2 text-sm text-zinc-600 dark:text-zinc-300">
                        {incident.state === "resolved"
                            ? "All affected calls have now recovered."
                            : incident.summary}
                    </p>
                    <div className="mt-3 flex flex-wrap gap-2 text-xs text-zinc-500 dark:text-zinc-400">
                        <span className="rounded-full border border-zinc-200 bg-white px-2.5 py-1 dark:border-white/10 dark:bg-white/[0.04]">{incident.displayName}</span>
                        <span className="rounded-full border border-zinc-200 bg-white px-2.5 py-1 dark:border-white/10 dark:bg-white/[0.04]">{incident.eventCount} audit event{incident.eventCount === 1 ? "" : "s"}</span>
                        {incident.handler ? <span className="rounded-full border border-zinc-200 bg-white px-2.5 py-1 dark:border-white/10 dark:bg-white/[0.04]">Handler {incident.handler}</span> : null}
                        {incident.nextRetryAtUtc && incident.resolvedAtUtc === null ? (
                            <span className="rounded-full border border-zinc-200 bg-white px-2.5 py-1 dark:border-white/10 dark:bg-white/[0.04]">Next retry {formatAbsoluteDate(incident.nextRetryAtUtc)}</span>
                        ) : null}
                    </div>
                </div>

                <div className="shrink-0 text-left text-xs text-zinc-500 dark:text-zinc-400 md:text-right">
                    <p>{formatTimeOnly(incident.startedAtUtc)}</p>
                    <p className="mt-1">
                        {incident.resolvedAtUtc
                            ? `Recovered ${formatTimeOnly(incident.resolvedAtUtc)}`
                            : `Open since ${formatRelativeDate(incident.startedAtUtc)}`}
                    </p>
                </div>
            </div>
        </article>
    );
}

function AuditRow({ audit }: { audit: ExternalServiceAuditItem }) {
    return (
        <article className="px-6 py-5 transition hover:bg-zinc-50/70 dark:hover:bg-white/[0.02]">
            <div className="flex flex-col gap-4 xl:flex-row xl:items-start xl:justify-between">
                <div className="min-w-0">
                    <div className="flex flex-wrap items-center gap-2">
                        <OutcomePill value={audit.outcome} />
                        <h3 className="text-sm font-semibold text-zinc-950 dark:text-zinc-50">{audit.displayName}</h3>
                        <span className="text-xs text-zinc-500 dark:text-zinc-400">{formatAbsoluteDate(audit.createdAtUtc)}</span>
                    </div>
                    {audit.errorMessage || audit.outcome === "failed_queued" ? (
                        <p className="mt-2 text-sm text-zinc-700 dark:text-zinc-300">
                            {audit.errorMessage ?? "Delayed retry queued."}
                        </p>
                    ) : null}

                    <div className="mt-3 flex flex-wrap gap-2 text-xs text-zinc-500 dark:text-zinc-400">
                        <span className="rounded-full border border-zinc-200 bg-white px-2.5 py-1 dark:border-white/10 dark:bg-white/[0.04]">{audit.handler}</span>
                        <span className="rounded-full border border-zinc-200 bg-white px-2.5 py-1 dark:border-white/10 dark:bg-white/[0.04]">{formatOrigin(audit.origin)}</span>
                        <span className="rounded-full border border-zinc-200 bg-white px-2.5 py-1 dark:border-white/10 dark:bg-white/[0.04]">Correlation {audit.correlationId}</span>
                        {audit.errorType ? <span className="rounded-full border border-zinc-200 bg-white px-2.5 py-1 dark:border-white/10 dark:bg-white/[0.04]">{audit.errorType}</span> : null}
                        {audit.nextRetryAtUtc ? <span className="rounded-full border border-zinc-200 bg-white px-2.5 py-1 dark:border-white/10 dark:bg-white/[0.04]">Next retry {formatAbsoluteDate(audit.nextRetryAtUtc)}</span> : null}
                    </div>
                </div>

                <div className="grid grid-cols-2 gap-3 sm:grid-cols-4 xl:min-w-[24rem]">
                    <MetricBadge label="Attempt" value={`${audit.attempt}/${audit.maxAttempts}`} />
                    <MetricBadge label="Local retries" value={String(audit.localRetryCount)} />
                    <MetricBadge label="Latency" value={`${audit.durationMs} ms`} />
                    <MetricBadge label="Queued" value={audit.queuedRetry ? "Yes" : "No"} />
                </div>
            </div>
        </article>
    );
}

function App() {
    const { theme, toggleTheme } = useTheme();
    const [summary, setSummary] = useState<ExternalServiceStatusSummary | null>(null);
    const [loading, setLoading] = useState(true);
    const [refreshing, setRefreshing] = useState(false);
    const [error, setError] = useState<string | null>(null);
    const [selectedComponentKey, setSelectedComponentKey] = useState<ComponentKey>("all");
    const [selectedHourKey, setSelectedHourKey] = useState<string | null>(null);
    const [auditMode, setAuditMode] = useState<AuditMode>("unsuccessful");
    const [refreshSequence, setRefreshSequence] = useState(0);

    useEffect(() => {
        let mounted = true;
        const controllers = new Set<AbortController>();

        const load = async (background = false) => {
            const controller = new AbortController();
            controllers.add(controller);

            if (background) {
                setRefreshing(true);
            } else {
                setLoading(true);
            }

            try {
                const data = await fetchStatusSummary(controller.signal);
                if (!mounted) {
                    return;
                }

                startTransition(() => {
                    setSummary(data);
                    setError(null);
                });
            } catch (fetchError) {
                if (!mounted || controller.signal.aborted) {
                    return;
                }

                setError(fetchError instanceof Error ? fetchError.message : "Failed to load status summary.");
            } finally {
                if (mounted) {
                    setLoading(false);
                    setRefreshing(false);
                }

                controllers.delete(controller);
            }
        };

        void load(refreshSequence > 0);

        const intervalId = window.setInterval(() => {
            void load(true);
        }, 30000);

        return () => {
            mounted = false;
            window.clearInterval(intervalId);
            controllers.forEach((controller) => controller.abort());
        };
    }, [refreshSequence]);

    const services = summary?.services ?? [];
    const audits = summary?.audits ?? [];
    const components = buildComponents(summary, services, audits);
    const highlightedComponents = components.filter((component) => component.status === "degraded" || component.status === "outage");
    const timeline = buildIncidentTimeline(services, audits);

    const deferredComponentKey = useDeferredValue(selectedComponentKey);
    const deferredSelectedHourKey = useDeferredValue(selectedHourKey);
    const deferredAuditMode = useDeferredValue(auditMode);

    const filteredTimeline = timeline.filter((incident) =>
        matchesIncidentFilter(deferredComponentKey, incident)
        && matchesHourSelection(
            deferredSelectedHourKey,
            incident.startedAtUtc,
            incident.lastUpdatedAtUtc,
            incident.resolvedAtUtc,
        ));
    const timelineGroups = groupIncidentsByDay(filteredTimeline);

    const filteredAudits = audits.filter((audit) => {
        if (!matchesAuditFilter(deferredComponentKey, audit)) {
            return false;
        }

        if (!matchesHourSelection(deferredSelectedHourKey, audit.createdAtUtc)) {
            return false;
        }

        if (deferredAuditMode === "all") {
            return true;
        }

        if (deferredAuditMode === "unsuccessful") {
            return audit.outcome !== "succeeded";
        }

        if (deferredAuditMode === "failed") {
            return audit.outcome === "failed";
        }

        return audit.outcome === "failed_queued";
    });

    const generatedAtLabel = summary ? formatAbsoluteDate(summary.generatedAtUtc) : "Loading";
    const bannerCopy = getBannerCopy(summary, highlightedComponents);
    const unsuccessfulAuditCount = audits.filter((audit) => audit.outcome !== "succeeded").length;
    const selectedComponentLabel = deferredComponentKey === "all"
        ? "All components"
        : components.find((component) => component.componentKey === deferredComponentKey)?.displayName ?? deferredComponentKey;
    const selectedHourLabel = deferredSelectedHourKey ? formatHourTick(new Date(deferredSelectedHourKey)) : null;

    const handleRefresh = () => {
        setRefreshSequence((current) => current + 1);
    };

    const handleOpenHistory = (componentKey: ComponentKey = "all", hourKey: string | null = null) => {
        setSelectedComponentKey(componentKey);
        setSelectedHourKey(hourKey);
        scrollToSection("history");
    };

    const handleOpenAudit = (
        componentKey: ComponentKey = "all",
        hourKey: string | null = null,
        nextAuditMode: AuditMode = "unsuccessful",
    ) => {
        setSelectedComponentKey(componentKey);
        setSelectedHourKey(hourKey);
        setAuditMode(nextAuditMode);
        scrollToSection("audit");
    };

    const clearHourSelection = () => {
        setSelectedHourKey(null);
    };

    return (
        <div className="min-h-screen bg-[radial-gradient(circle_at_top,_rgba(16,185,129,0.09),_transparent_24%),linear-gradient(180deg,_#fafaf9_0%,_#f4f4f5_100%)] text-zinc-950 dark:bg-[radial-gradient(circle_at_top,_rgba(16,185,129,0.18),_transparent_24%),linear-gradient(180deg,_#09090b_0%,_#0f172a_100%)] dark:text-zinc-100">
            <header className="border-b border-zinc-200/80 bg-white/85 backdrop-blur dark:border-white/10 dark:bg-zinc-950/70">
                <div className="mx-auto flex max-w-5xl flex-col gap-4 px-5 py-5 sm:px-6 lg:px-8 md:flex-row md:items-center md:justify-between">
                    <div>
                        <p className="text-xs font-semibold uppercase tracking-[0.22em] text-zinc-400 dark:text-zinc-500">MIBO Platform</p>
                        <h1 className="mt-2 text-3xl font-semibold tracking-tight text-zinc-950 dark:text-white">Status</h1>
                    </div>

                    <div className="flex flex-wrap items-center gap-2">
                        <ThemeToggle theme={theme} onToggle={toggleTheme} />
                        <button
                            type="button"
                            onClick={() => handleOpenHistory()}
                            className="rounded-full border border-zinc-200 bg-white px-4 py-2 text-sm font-medium text-zinc-700 transition hover:border-zinc-300 hover:text-zinc-950 dark:border-white/10 dark:bg-white/[0.04] dark:text-zinc-200 dark:hover:border-white/20 dark:hover:text-white"
                        >
                            View history
                        </button>
                        <button
                            type="button"
                            onClick={() => handleOpenAudit()}
                            className="rounded-full border border-zinc-200 bg-white px-4 py-2 text-sm font-medium text-zinc-700 transition hover:border-zinc-300 hover:text-zinc-950 dark:border-white/10 dark:bg-white/[0.04] dark:text-zinc-200 dark:hover:border-white/20 dark:hover:text-white"
                        >
                            View audit
                        </button>
                        <button
                            type="button"
                            onClick={handleRefresh}
                            className="rounded-full border border-zinc-900 bg-zinc-900 px-4 py-2 text-sm font-medium text-white transition hover:bg-zinc-800 dark:border-emerald-400/20 dark:bg-emerald-400/10 dark:text-emerald-100 dark:hover:bg-emerald-400/15"
                        >
                            {refreshing ? "Refreshing..." : "Refresh"}
                        </button>
                    </div>
                </div>
            </header>

            <main className="mx-auto max-w-5xl px-5 py-8 sm:px-6 lg:px-8">
                <section
                    className={`rounded-[28px] border px-6 py-6 shadow-[0_18px_60px_-32px_rgba(15,23,42,0.32)] dark:shadow-[0_30px_90px_-45px_rgba(0,0,0,0.85)] ${bannerTone[summary?.overallStatus ?? "monitoring"]}`}
                >
                    <div className="flex flex-col gap-6 lg:flex-row lg:items-start lg:justify-between">
                        <div className="max-w-2xl">
                            <div className="flex flex-wrap items-center gap-3">
                                <StatusPill value={summary?.overallStatus ?? "monitoring"} />
                                <span className="text-sm font-medium text-zinc-500 dark:text-zinc-400">Generated {generatedAtLabel}</span>
                            </div>
                            <h2 className="mt-4 text-2xl font-semibold tracking-tight">{bannerCopy.title}</h2>
                            {bannerCopy.description ? (
                                <p className="mt-2 text-sm text-zinc-600 dark:text-zinc-300">{bannerCopy.description}</p>
                            ) : null}
                        </div>

                        <div className="grid gap-3 sm:grid-cols-3 lg:min-w-[22rem]">
                            <MetricBadge label="Components" value={String(components.length)} />
                            <MetricBadge label="Open incidents" value={String(highlightedComponents.length)} />
                            <MetricBadge label="Failed audits" value={String(unsuccessfulAuditCount)} />
                        </div>
                    </div>
                </section>

                {error ? (
                    <section className="mt-6 rounded-[28px] border border-rose-200 bg-rose-50/80 px-6 py-5 text-rose-800 shadow-sm dark:border-rose-500/20 dark:bg-rose-500/10 dark:text-rose-200">
                        <p className="text-xs font-semibold uppercase tracking-[0.18em]">Status API error</p>
                        <p className="mt-2 text-sm">{error}</p>
                    </section>
                ) : null}

                {highlightedComponents.length > 0 ? (
                    <section className="mt-6 rounded-[28px] border border-amber-200 bg-amber-50/80 px-6 py-5 shadow-sm dark:border-amber-500/20 dark:bg-amber-500/10">
                        <div className="flex flex-col gap-4 md:flex-row md:items-start md:justify-between">
                            <div>
                                <h2 className="text-xl font-semibold tracking-tight text-amber-950 dark:text-amber-100">Components currently requiring attention</h2>
                            </div>
                            <button
                                type="button"
                                onClick={() => handleOpenHistory()}
                                className="rounded-full border border-amber-200 bg-white px-4 py-2 text-sm font-medium text-amber-800 transition hover:border-amber-300 dark:border-amber-500/20 dark:bg-white/[0.04] dark:text-amber-100 dark:hover:border-amber-400/30"
                            >
                                Open incident history
                            </button>
                        </div>

                        <div className="mt-5 grid gap-3 md:grid-cols-2">
                            {highlightedComponents.map((component) => (
                                <button
                                    key={component.componentKey}
                                    type="button"
                                    onClick={() => handleOpenAudit(component.componentKey)}
                                    className="rounded-2xl border border-white/70 bg-white/80 px-4 py-4 text-left transition hover:border-amber-200 dark:border-white/10 dark:bg-white/[0.04] dark:hover:border-amber-400/30"
                                >
                                    <div className="flex flex-wrap items-center justify-between gap-3">
                                        <div>
                                            <p className="text-sm font-semibold text-zinc-950 dark:text-zinc-50">{component.displayName}</p>
                                            <p className="mt-1 text-sm text-zinc-500 dark:text-zinc-400">{component.lastErrorMessage ?? "Retries are currently active for this component."}</p>
                                        </div>
                                        <StatusPill value={component.status} />
                                    </div>
                                </button>
                            ))}
                        </div>
                    </section>
                ) : null}

                {summary && !summary.enabled ? (
                    <section className={`${shellCardClass} mt-6 px-6 py-5`}>
                        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-zinc-400 dark:text-zinc-500">Configuration</p>
                        <p className="mt-2 text-sm text-zinc-600 dark:text-zinc-300">
                            Enable <code className="rounded bg-zinc-100 px-1 py-0.5 text-xs dark:bg-white/[0.08]">RetryPolicy__Enabled</code>,{" "}
                            <code className="rounded bg-zinc-100 px-1 py-0.5 text-xs dark:bg-white/[0.08]">RetryPolicy__AuditEnabled</code> and{" "}
                            <code className="rounded bg-zinc-100 px-1 py-0.5 text-xs dark:bg-white/[0.08]">RetryPolicy__StatusPageEnabled</code> to populate this page with live data.
                        </p>
                    </section>
                ) : null}

                <section className="mt-6 rounded-[28px] border border-zinc-200 bg-white/70 px-4 py-4 shadow-sm backdrop-blur dark:border-white/10 dark:bg-white/[0.04]">
                    <div className="flex flex-col gap-3 lg:flex-row lg:items-center lg:justify-between">
                        <div className="flex flex-wrap gap-2">
                            <FilterChip active={false} label="System status" onClick={() => scrollToSection("status")} />
                            <FilterChip active={false} label="History" onClick={() => handleOpenHistory()} />
                            <FilterChip active={false} label="Audit log" onClick={() => handleOpenAudit()} />
                        </div>
                        <div className="flex flex-wrap items-center gap-3 text-xs text-zinc-500 dark:text-zinc-400">
                            <span>Each bar represents one hour</span>
                            <span className="inline-flex items-center gap-2">
                                <span className="h-2.5 w-2.5 rounded-full bg-emerald-400" />
                                Success
                            </span>
                            <span className="inline-flex items-center gap-2">
                                <span className="h-2.5 w-2.5 rounded-full bg-amber-400" />
                                Retry queued
                            </span>
                            <span className="inline-flex items-center gap-2">
                                <span className="h-2.5 w-2.5 rounded-full bg-rose-400" />
                                Failure
                            </span>
                        </div>
                    </div>
                </section>

                <section id="status" className={`${shellCardClass} mt-6`}>
                    <div className="border-b border-zinc-200 px-6 py-5 dark:border-white/10">
                        <div className="flex flex-col gap-4 md:flex-row md:items-center md:justify-between">
                            <div>
                                <p className="text-xs font-semibold uppercase tracking-[0.18em] text-zinc-400 dark:text-zinc-500">System status</p>
                                <h2 className="mt-2 text-2xl font-semibold tracking-tight text-zinc-950 dark:text-white">Platform and service components</h2>
                            </div>

                            <div className="flex flex-wrap gap-3 text-xs text-zinc-500 dark:text-zinc-400">
                                <span className="rounded-full border border-zinc-200 bg-white px-3 py-1 dark:border-white/10 dark:bg-white/[0.04]">Past 24 hours</span>
                            </div>
                        </div>
                    </div>

                    <div className="divide-y divide-zinc-200 dark:divide-white/10">
                        {loading && components.length === 0 ? (
                            Array.from({ length: 5 }, (_, index) => (
                                <div key={index} className="px-6 py-5">
                                    <div className="h-24 animate-pulse rounded-3xl bg-zinc-100 dark:bg-white/[0.05]" />
                                </div>
                            ))
                        ) : components.length > 0 ? (
                            components.map((component) => (
                                <ComponentRow
                                    key={component.componentKey}
                                    component={component}
                                    audits={audits}
                                    generatedAtUtc={summary?.generatedAtUtc ?? null}
                                    selectedHourKey={selectedHourKey}
                                    onViewHistory={handleOpenHistory}
                                    onViewAudit={handleOpenAudit}
                                    onInspectHour={(componentKey, hourKey) => handleOpenAudit(componentKey, hourKey, "all")}
                                />
                            ))
                        ) : (
                            <div className="px-6 py-6">
                                <EmptyPanel
                                    title="No monitored components yet"
                                    description="Components will appear here when data is available."
                                />
                            </div>
                        )}
                    </div>
                </section>

                <section id="history" className={`${shellCardClass} mt-6`}>
                    <div className="border-b border-zinc-200 px-6 py-5 dark:border-white/10">
                        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-zinc-400 dark:text-zinc-500">History</p>
                        <div className="mt-2 flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
                            <div>
                                <h2 className="text-2xl font-semibold tracking-tight text-zinc-950 dark:text-white">Incident timeline</h2>
                            </div>
                            <p className="text-sm text-zinc-500 dark:text-zinc-400">
                                Showing <span className="font-semibold text-zinc-900 dark:text-zinc-100">{selectedComponentLabel}</span>
                            </p>
                        </div>

                        <div className="mt-4 flex flex-wrap gap-2">
                            <FilterChip active={selectedComponentKey === "all"} label="All components" onClick={() => setSelectedComponentKey("all")} />
                            {components.map((component) => (
                                <FilterChip
                                    key={component.componentKey}
                                    active={selectedComponentKey === component.componentKey}
                                    label={component.displayName}
                                    onClick={() => setSelectedComponentKey(component.componentKey)}
                                />
                            ))}
                        </div>

                        {selectedHourLabel ? (
                            <div className="mt-4 flex flex-wrap items-center gap-2">
                                <span className="rounded-full border border-zinc-200 bg-white px-3 py-1.5 text-sm font-medium text-zinc-700 dark:border-white/10 dark:bg-white/[0.04] dark:text-zinc-200">
                                    Hour {selectedHourLabel}
                                </span>
                                <button
                                    type="button"
                                    onClick={clearHourSelection}
                                    className="rounded-full border border-zinc-200 bg-white px-3 py-1.5 text-sm font-medium text-zinc-600 transition hover:border-zinc-300 hover:text-zinc-950 dark:border-white/10 dark:bg-white/[0.04] dark:text-zinc-300 dark:hover:border-white/20 dark:hover:text-white"
                                >
                                    Clear hour
                                </button>
                            </div>
                        ) : null}
                    </div>

                    <div className="px-6 py-6">
                        {timelineGroups.length > 0 ? (
                            <div className="space-y-6">
                                {timelineGroups.map((group) => (
                                    <section key={group.key} className="grid gap-4 md:grid-cols-[5.5rem_1fr]">
                                        <div className="pt-1">
                                            <p className="text-lg font-semibold tracking-tight text-zinc-950 dark:text-zinc-100">{group.dayLabel}</p>
                                            <p className="text-sm text-zinc-500 dark:text-zinc-400">{group.weekday}</p>
                                        </div>
                                        <div className="space-y-3">
                                            {group.items.map((incident) => (
                                                <HistoryEntry key={`${incident.serviceKey}-${incident.startedAtUtc}-${incident.lastUpdatedAtUtc}`} incident={incident} />
                                            ))}
                                        </div>
                                    </section>
                                ))}
                            </div>
                        ) : (
                            <EmptyPanel
                                title="No incident history for the selected filter"
                                description="No incidents match the current filter."
                            />
                        )}
                    </div>
                </section>

                <section id="audit" className={`${shellCardClass} mt-6`}>
                    <div className="border-b border-zinc-200 px-6 py-5 dark:border-white/10">
                        <p className="text-xs font-semibold uppercase tracking-[0.18em] text-zinc-400 dark:text-zinc-500">Audit log</p>
                        <div className="mt-2 flex flex-col gap-4 lg:flex-row lg:items-end lg:justify-between">
                            <div>
                                <h2 className="text-2xl font-semibold tracking-tight text-zinc-950 dark:text-white">Execution and retry audit</h2>
                            </div>
                            <p className="text-sm text-zinc-500 dark:text-zinc-400">
                                {filteredAudits.length} event{filteredAudits.length === 1 ? "" : "s"}
                            </p>
                        </div>

                        <div className="mt-4 flex flex-wrap gap-2">
                            <FilterChip active={selectedComponentKey === "all"} label="All components" onClick={() => setSelectedComponentKey("all")} />
                            {components.map((component) => (
                                <FilterChip
                                    key={component.componentKey}
                                    active={selectedComponentKey === component.componentKey}
                                    label={component.displayName}
                                    onClick={() => setSelectedComponentKey(component.componentKey)}
                                />
                            ))}
                        </div>

                        <div className="mt-4 flex flex-wrap gap-2">
                            <FilterChip active={auditMode === "unsuccessful"} label="Unsuccessful" onClick={() => setAuditMode("unsuccessful")} />
                            <FilterChip active={auditMode === "failed"} label="Failed only" onClick={() => setAuditMode("failed")} />
                            <FilterChip active={auditMode === "queued"} label="Queued only" onClick={() => setAuditMode("queued")} />
                            <FilterChip active={auditMode === "all"} label="All events" onClick={() => setAuditMode("all")} />
                        </div>

                        {selectedHourLabel ? (
                            <div className="mt-4 flex flex-wrap items-center gap-2">
                                <span className="rounded-full border border-zinc-200 bg-white px-3 py-1.5 text-sm font-medium text-zinc-700 dark:border-white/10 dark:bg-white/[0.04] dark:text-zinc-200">
                                    Hour {selectedHourLabel}
                                </span>
                                <button
                                    type="button"
                                    onClick={clearHourSelection}
                                    className="rounded-full border border-zinc-200 bg-white px-3 py-1.5 text-sm font-medium text-zinc-600 transition hover:border-zinc-300 hover:text-zinc-950 dark:border-white/10 dark:bg-white/[0.04] dark:text-zinc-300 dark:hover:border-white/20 dark:hover:text-white"
                                >
                                    Clear hour
                                </button>
                            </div>
                        ) : null}
                    </div>

                    <div className="divide-y divide-zinc-200 dark:divide-white/10">
                        {loading && !summary ? (
                            Array.from({ length: 4 }, (_, index) => (
                                <div key={index} className="px-6 py-5">
                                    <div className="h-24 animate-pulse rounded-3xl bg-zinc-100 dark:bg-white/[0.05]" />
                                </div>
                            ))
                        ) : filteredAudits.length > 0 ? (
                            filteredAudits.map((audit) => (
                                <AuditRow key={`${audit.correlationId}-${audit.createdAtUtc}-${audit.attempt}`} audit={audit} />
                            ))
                        ) : (
                            <div className="px-6 py-6">
                                <EmptyPanel
                                    title="No audit events for the selected filter"
                                    description="No audit events match the current filter."
                                />
                            </div>
                        )}
                    </div>
                </section>
            </main>
        </div>
    );
}

export default App;
