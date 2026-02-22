import { useMemo } from "react";
import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";

type Opt = { label: string; value: string };

function normalizeOptions(raw: unknown): Opt[] {
    if (!Array.isArray(raw)) {
        return [
            { label: "Top rated", value: "rating:desc" },
            { label: "Price ↑", value: "price:asc" },
            { label: "Price ↓", value: "price:desc" },
            { label: "Title A→Z", value: "title:asc" },
            { label: "Title Z→A", value: "title:desc" },
        ];
    }
    return raw.map((x: any) => {
        if (typeof x === "string") return { label: x, value: x };
        return { label: String(x?.label ?? x?.value ?? "Option"), value: String(x?.value ?? x?.label ?? "") };
    });
}

export function SortDropdown({ props, onAction }: UiComponentProps) {
    const actionType = String((props as any)?.actionType ?? "shop.sort").trim() || "shop.sort";
    const selected = String((props as any)?.selected ?? "rating:desc");
    const options = useMemo(() => normalizeOptions((props as any)?.options), [props]);

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
            <div className="text-sm font-semibold">Sort</div>

            <div className="mt-3 flex items-center gap-2">
                <select
                    className="w-full rounded-xl border border-zinc-300 bg-white px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-950"
                    value={selected}
                    onChange={(e) => onAction?.(actionType, { sort: e.target.value })}
                >
                    {options.map((o) => (
                        <option key={o.value || o.label} value={o.value}>
                            {o.label}
                        </option>
                    ))}
                </select>
            </div>

            <div className="mt-2 text-xs opacity-60">
                Format recomandat pentru payload: <code>{"{ sort: 'price:asc' }"}</code>
            </div>
        </div>
    );
}