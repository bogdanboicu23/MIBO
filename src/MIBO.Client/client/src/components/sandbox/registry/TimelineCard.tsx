import { useMemo } from "react";
import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";
import { extractArray, resolveFromDataKey } from "@/components/sandbox/registry/dataResolver";
import { resolveActionPayload, resolveActionType } from "@/components/sandbox/registry/actionResolver";

type TimelineItem = {
    title: string;
    description?: string;
    timestamp?: string;
    status?: "success" | "warning" | "error" | "info";
    raw?: any;
};

function normalizeItems(raw: unknown): TimelineItem[] {
    if (!Array.isArray(raw)) return [];
    return raw.map((x: any, i) => ({
        title: String(x?.title ?? x?.name ?? x?.event ?? `Event ${i + 1}`),
        description: x?.description ?? x?.text ?? undefined,
        timestamp: x?.timestamp ?? x?.time ?? x?.createdAt ?? undefined,
        status: x?.status,
        raw: x,
    }));
}

function dotClass(status?: string): string {
    if (status === "success") return "bg-emerald-500";
    if (status === "warning") return "bg-amber-500";
    if (status === "error") return "bg-rose-500";
    return "bg-sky-500";
}

export function TimelineCard({ props, data, onAction }: UiComponentProps) {
    const title = String((props as any)?.title ?? "Timeline");
    const dataKey = String((props as any)?.dataKey ?? "").trim();
    const actionType = resolveActionType((props as any) ?? {}, "ui.timeline.select");

    const source = (props as any)?.items ?? (dataKey ? resolveFromDataKey((data ?? {}) as Record<string, any>, dataKey) : null);
    const items = useMemo(() => normalizeItems(extractArray(source, ["items", "events", "timeline"])), [source]);

    if (!items.length) {
        return (
            <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
                <div className="text-sm font-semibold">{title}</div>
                <div className="mt-2 text-sm opacity-70">No timeline items available.</div>
            </div>
        );
    }

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
            <div className="text-sm font-semibold">{title}</div>

            <div className="mt-3 grid gap-3">
                {items.map((item, idx) => (
                    <button
                        key={`${item.title}-${idx}`}
                        type="button"
                        onClick={() => {
                            const fallbackPayload = { item: item.raw ?? item, index: idx, source: "timeline" } as Record<string, unknown>;
                            const payload = resolveActionPayload(
                                (props as any) ?? {},
                                fallbackPayload,
                                { data: (data ?? {}) as Record<string, any>, item: (item.raw ?? item) as any, extra: fallbackPayload }
                            );
                            onAction?.(actionType, payload);
                        }}
                        className="rounded-xl border border-zinc-200/70 p-3 text-left transition hover:bg-zinc-50 dark:border-zinc-800/70 dark:hover:bg-zinc-900"
                    >
                        <div className="flex items-center gap-2">
                            <span className={`inline-block h-2.5 w-2.5 rounded-full ${dotClass(item.status)}`} />
                            <span className="text-sm font-medium">{item.title}</span>
                            {item.timestamp ? <span className="ml-auto text-xs opacity-60">{String(item.timestamp)}</span> : null}
                        </div>
                        {item.description ? <div className="mt-1 text-xs opacity-75">{item.description}</div> : null}
                    </button>
                ))}
            </div>
        </div>
    );
}
