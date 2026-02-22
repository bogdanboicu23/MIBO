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

function getNumber(data: any, key: string): number | null {
    if (!key) return null;
    const v1 = data?.[key];
    if (Number.isFinite(Number(v1))) return Number(v1);
    const v2 = getDot(data, key);
    if (Number.isFinite(Number(v2))) return Number(v2);
    const last = key.split(".").filter(Boolean).at(-1);
    if (last && Number.isFinite(Number(data?.[last]))) return Number(data?.[last]);
    return null;
}

export function Pagination({ props, data, onAction }: UiComponentProps) {
    const actionType = String((props as any)?.actionType ?? "shop.change_page").trim() || "shop.change_page";
    const totalKey = String((props as any)?.totalKey ?? "");
    const limitKey = String((props as any)?.limitKey ?? "");
    const skipKey = String((props as any)?.skipKey ?? "");

    const total = getNumber(data as any, totalKey) ?? 0;
    const limit = Math.max(1, getNumber(data as any, limitKey) ?? 12);
    const skip = Math.max(0, getNumber(data as any, skipKey) ?? 0);

    const pageInfo = useMemo(() => {
        const currentPage = Math.floor(skip / limit) + 1;
        const totalPages = total > 0 ? Math.ceil(total / limit) : 1;
        return { currentPage, totalPages };
    }, [skip, limit, total]);

    const canPrev = skip > 0;
    const canNext = total > 0 ? skip + limit < total : false;

    const go = (nextSkip: number) => {
        onAction?.(actionType, { skip: Math.max(0, nextSkip), limit });
    };

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
            <div className="flex items-center justify-between gap-2">
                <div className="text-sm font-semibold">Pagination</div>
                <div className="text-xs opacity-70">
                    page {pageInfo.currentPage} / {pageInfo.totalPages}
                </div>
            </div>

            <div className="mt-3 flex items-center gap-2">
                <button
                    type="button"
                    disabled={!canPrev}
                    onClick={() => go(skip - limit)}
                    className="rounded-xl border border-zinc-300 bg-white px-3 py-2 text-sm font-medium hover:bg-zinc-50 disabled:opacity-40 dark:border-zinc-700 dark:bg-zinc-950 dark:hover:bg-zinc-900"
                >
                    Prev
                </button>
                <button
                    type="button"
                    disabled={!canNext}
                    onClick={() => go(skip + limit)}
                    className="rounded-xl border border-zinc-300 bg-white px-3 py-2 text-sm font-medium hover:bg-zinc-50 disabled:opacity-40 dark:border-zinc-700 dark:bg-zinc-950 dark:hover:bg-zinc-900"
                >
                    Next
                </button>

                <div className="ml-auto text-xs opacity-60">
                    skip: {skip} • limit: {limit} • total: {total}
                </div>
            </div>
        </div>
    );
}