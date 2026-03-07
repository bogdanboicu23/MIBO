import { useMemo } from "react";
import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";
import { resolveFromDataKey, toNumber } from "@/components/sandbox/registry/dataResolver";
import { resolveActionPayload, resolveActionType } from "@/components/sandbox/registry/actionResolver";

function getNumber(data: Record<string, any>, key: string): number | null {
    return toNumber(resolveFromDataKey(data, key));
}

export function Pagination({ props, data, onAction }: UiComponentProps) {
    const actionType = resolveActionType((props as any) ?? {}, "shop.change_page");
    const totalKey = String((props as any)?.totalKey ?? "");
    const limitKey = String((props as any)?.limitKey ?? "");
    const skipKey = String((props as any)?.skipKey ?? "");

    const source = (data ?? {}) as Record<string, any>;
    const total = getNumber(source, totalKey) ?? Number((props as any)?.total ?? 0);
    const limit = Math.max(1, getNumber(source, limitKey) ?? Number((props as any)?.limit ?? 12));
    const skip = Math.max(0, getNumber(source, skipKey) ?? Number((props as any)?.skip ?? 0));

    const pageInfo = useMemo(() => {
        const currentPage = Math.floor(skip / limit) + 1;
        const totalPages = total > 0 ? Math.ceil(total / limit) : 1;
        return { currentPage, totalPages };
    }, [skip, limit, total]);

    const canPrev = skip > 0;
    const canNext = total > 0 ? skip + limit < total : false;

    const go = (nextSkip: number) => {
        const safeSkip = Math.max(0, nextSkip);
        const page = Math.floor(safeSkip / limit) + 1;
        const payload = resolveActionPayload(
            (props as any) ?? {},
            { skip: safeSkip, limit, page },
            { data: source, value: page, extra: { skip: safeSkip, limit, page, total } }
        );
        onAction?.(actionType, payload);
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
