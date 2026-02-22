import { useMemo } from "react";
import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";

function getDot(obj: unknown, path: string): unknown {
    if (!path) return undefined;
    const parts = path.split(".").filter(Boolean);
    let cur: any = obj;
    for (const p of parts) {
        if (cur == null) return undefined;
        cur = cur[p];
    }
    return cur;
}

function resolveCategories(props: any, data: any): string[] {
    const direct = props?.items ?? props?.categories ?? props?.data;
    if (Array.isArray(direct)) return direct.map((x) => String((x as any)?.name ?? x));

    const dataKey = typeof props?.dataKey === "string" ? props.dataKey.trim() : "";
    if (dataKey) {
        const v1 = data?.[dataKey];
        if (Array.isArray(v1)) return v1.map((x: any) => String(x?.name ?? x));

        const v2 = getDot(data, dataKey);
        if (Array.isArray(v2)) return v2.map((x: any) => String(x?.name ?? x));

        const last = dataKey.split(".").filter(Boolean).at(-1);
        if (last) {
            const v3 = data?.[last];
            if (Array.isArray(v3)) return v3.map((x: any) => String(x?.name ?? x));
        }
    }

    // common tool-result shape: { categories: [...] }
    for (const key of Object.keys(data ?? {})) {
        const val = data?.[key];
        if (val && typeof val === "object" && Array.isArray((val as any).categories)) {
            return (val as any).categories.map((x: any) => String(x?.name ?? x));
        }
    }

    return [];
}

export function CategoryChips({ props, data, onAction }: UiComponentProps) {
    const actionType = String((props as any)?.actionType ?? "shop.select_category").trim() || "shop.select_category";
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
                                onClick={() => onAction?.(actionType, { category: c })}
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