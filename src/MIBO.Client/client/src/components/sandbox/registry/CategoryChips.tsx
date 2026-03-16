import { useMemo } from "react";
import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";
import { extractArray, resolveFromDataKey } from "@/components/sandbox/registry/dataResolver";
import { resolveActionPayload, resolveActionType } from "@/components/sandbox/registry/actionResolver";

function resolveCategories(props: any, data: any): string[] {
    const direct = props?.items ?? props?.categories ?? props?.data;
    if (Array.isArray(direct)) return direct.map((x) => String((x as any)?.name ?? x));

    const dataKey = typeof props?.dataKey === "string" ? props.dataKey.trim() : "";
    if (dataKey) {
        const resolved = resolveFromDataKey((data ?? {}) as Record<string, any>, dataKey);
        const list = extractArray(resolved, ["categories", "items", "products"]);
        if (list.length) return list.map((x: any) => String(x?.name ?? x?.label ?? x));
    }

    const list = extractArray(data, ["categories"]);
    if (list.length) return list.map((x: any) => String(x?.name ?? x?.label ?? x));

    return [];
}

export function CategoryChips({ props, data, onAction }: UiComponentProps) {
    const dataSourceId = String((props as any)?.dataSourceId ?? (props as any)?.data_source ?? "").trim();
    const actionId = String((props as any)?.actionId ?? "").trim();
    const sourceKey = String((props as any)?.sourceKey ?? "").trim();
    const actionType = actionId
        ? resolveActionType((props as any) ?? {}, "ui.action.execute")
        : dataSourceId
            ? "data.query"
            : resolveActionType((props as any) ?? {}, "data.query");
    const selectedKey = String((props as any)?.selectedKey ?? "").trim();

    const categories = useMemo(() => resolveCategories(props as any, data as any), [props, data]);

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
            <div className="text-sm font-semibold">Categories</div>

            {categories.length === 0 ? (
                <div className="mt-2 text-sm opacity-70">No categories (provide props.items or data[dataKey]).</div>
            ) : (
                <div className="mt-3 flex flex-wrap gap-2">
                    {categories.slice(0, 40).map((c) => {
                        const active = selectedKey && c === selectedKey;
                        return (
                            <button
                                key={c}
                                type="button"
                                onClick={() => {
                                    const fallbackPayload = actionId
                                        ? { actionId, dataSourceId, sourceKey, category: c, skip: 0 }
                                        : dataSourceId
                                            ? { dataSourceId, sourceKey, category: c, skip: 0 }
                                            : { category: c };
                                    const payload = resolveActionPayload(
                                        (props as any) ?? {},
                                        fallbackPayload,
                                        { data: (data ?? {}) as Record<string, any>, value: c, extra: { category: c } }
                                    );
                                    onAction?.(actionType, payload);
                                }}
                                className={[
                                    "rounded-full border px-3 py-1.5 text-sm",
                                    active
                                        ? "border-zinc-900 bg-zinc-900 text-white dark:border-white dark:bg-white dark:text-zinc-900"
                                        : "border-zinc-300 bg-white hover:bg-zinc-50 dark:border-zinc-700 dark:bg-zinc-950 dark:hover:bg-zinc-900",
                                ].join(" ")}
                            >
                                {c}
                            </button>
                        );
                    })}
                </div>
            )}
        </div>
    );
}
