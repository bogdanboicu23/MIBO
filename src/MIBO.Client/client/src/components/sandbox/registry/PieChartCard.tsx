import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";

type PieDatum = { name: string; value: number };

export function PieChartCard({ props, data }: UiComponentProps) {
    const title = String(props?.title ?? "Pie Chart");
    const dataKey = String(props?.dataKey ?? "");

    // Acceptă fie:
    // - props.data direct (array)
    // - data[dataKey] (array)
    const raw =
        (props?.data as any) ??
        (dataKey && data ? (data as any)[dataKey] : null) ??
        [];

    const items: PieDatum[] = Array.isArray(raw)
        ? raw
            .map((x) => ({
                name: String((x as any)?.name ?? (x as any)?.label ?? "item"),
                value: Number((x as any)?.value ?? 0),
            }))
            .filter((x) => Number.isFinite(x.value))
        : [];

    const total = items.reduce((s, x) => s + x.value, 0);

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
            <div className="flex items-center justify-between">
                <div className="text-sm font-semibold">{title}</div>
                <div className="text-xs opacity-70">total: {total}</div>
            </div>

            {items.length === 0 ? (
                <div className="mt-3 text-sm opacity-70">
                    No data (provide props.data or data[dataKey]).
                </div>
            ) : (
                <ul className="mt-3 space-y-2 text-sm">
                    {items.map((it, idx) => (
                        <li key={idx} className="flex items-center justify-between">
                            <span className="opacity-80">{it.name}</span>
                            <span className="font-medium">{it.value}</span>
                        </li>
                    ))}
                </ul>
            )}
        </div>
    );
}