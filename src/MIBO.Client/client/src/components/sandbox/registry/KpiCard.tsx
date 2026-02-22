import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";

export function KpiCard({ props, data }: UiComponentProps) {
    const label = String(props?.label ?? "KPI");
    const valueKey = String(props?.valueKey ?? "");
    const value =
        valueKey && data && Object.prototype.hasOwnProperty.call(data, valueKey)
            ? (data as any)[valueKey]
            : props?.value ?? "N/A";

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
            <div className="text-xs uppercase tracking-wide opacity-70">{label}</div>
            <div className="mt-2 text-2xl font-semibold">{String(value ?? "N/A")}</div>
        </div>
    );
}