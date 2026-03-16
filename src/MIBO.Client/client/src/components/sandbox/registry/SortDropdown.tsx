import { useMemo } from "react";
import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";
import { extractArray, resolveFromDataKey } from "@/components/sandbox/registry/dataResolver";
import { resolveActionPayload, resolveActionType } from "@/components/sandbox/registry/actionResolver";

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

export function SortDropdown({ props, data, onAction }: UiComponentProps) {
    const dataSourceId = String((props as any)?.dataSourceId ?? (props as any)?.data_source ?? "").trim();
    const actionId = String((props as any)?.actionId ?? "").trim();
    const sourceKey = String((props as any)?.sourceKey ?? "").trim();
    const actionType = actionId
        ? resolveActionType((props as any) ?? {}, "ui.action.execute")
        : dataSourceId
            ? "data.query"
            : resolveActionType((props as any) ?? {}, "data.query");
    const dataKey = String((props as any)?.dataKey ?? "").trim();
    const selected = useMemo(() => {
        if ((props as any)?.selected) return String((props as any).selected);
        const selectedKey = String((props as any)?.selectedKey ?? "").trim();
        if (selectedKey) {
            const v = resolveFromDataKey((data ?? {}) as Record<string, any>, selectedKey);
            if (v != null) return String(v);
        }
        return "rating:desc";
    }, [props, data]);
    const options = useMemo(() => {
        const direct = (props as any)?.options;
        if (Array.isArray(direct)) return normalizeOptions(direct);

        if (dataKey) {
            const resolved = resolveFromDataKey((data ?? {}) as Record<string, any>, dataKey);
            const list = extractArray(resolved, ["options", "items", "sorts"]);
            if (list.length) return normalizeOptions(list);
        }

        return normalizeOptions(undefined);
    }, [props, data, dataKey]);

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
            <div className="text-sm font-semibold">Sort</div>

            <div className="mt-3 flex items-center gap-2">
                <select
                    className="w-full rounded-xl border border-zinc-300 bg-white px-3 py-2 text-sm dark:border-zinc-700 dark:bg-zinc-950"
                    value={selected}
                    onChange={(e) => {
                        const nextSort = e.target.value;
                        const [sortBy, order] = nextSort.includes(":")
                            ? nextSort.split(":", 2)
                            : [nextSort, "asc"];
                        const fallbackPayload = actionId
                            ? { actionId, dataSourceId, sourceKey, sort: nextSort, sortBy, order, skip: 0 }
                            : dataSourceId
                                ? { dataSourceId, sourceKey, sort: nextSort, sortBy, order, skip: 0 }
                                : { sort: nextSort, sortBy, order };
                        const payload = resolveActionPayload(
                            (props as any) ?? {},
                            fallbackPayload,
                            { data: (data ?? {}) as Record<string, any>, value: nextSort, extra: { sort: nextSort } }
                        );
                        onAction?.(actionType, payload);
                    }}
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
