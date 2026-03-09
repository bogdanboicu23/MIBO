import type { UiComponentProps } from "@/components/sandbox/registry/types";
import { resolveFromDataKey } from "@/components/sandbox/registry/dataResolver";

export function KpiCard({ props, data }: UiComponentProps) {
    const label = String(props?.label ?? "KPI");
    const valueKey = String(props?.valueKey ?? "");
    const dataKey = String(props?.dataKey ?? "");
    const source = resolveFromDataKey((data ?? {}) as Record<string, any>, dataKey);
    const valueFromKey = resolveFromDataKey((data ?? {}) as Record<string, any>, valueKey);
    const valueFromSource =
        valueKey && source && typeof source === "object"
            ? resolveFromDataKey(source as Record<string, any>, valueKey)
            : undefined;
    const value = valueFromKey ?? valueFromSource ?? props?.value ?? "N/A";

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
            <div className="text-xs uppercase tracking-wide opacity-70">{label}</div>
            <div className="mt-2 text-2xl font-semibold">{String(value ?? "N/A")}</div>
        </div>
    );
}
