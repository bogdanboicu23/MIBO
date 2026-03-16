import { useEffect, useMemo, useRef, useState } from "react";
import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";
import { extractArray, resolveFromDataKey } from "@/components/sandbox/registry/dataResolver";
import { resolveActionPayload, resolveActionType } from "@/components/sandbox/registry/actionResolver";

type Product = {
    id: number | string;
    title?: string;
    description?: string;
    price?: number;
    rating?: number;
    brand?: string;
    category?: string;
    thumbnail?: string;
    images?: string[];
    stock?: number;
};

function resolveProducts(toolResult: any): Product[] {
    return extractArray(toolResult, ["products", "items", "results"]) as Product[];
}

function formatMoney(value: unknown): string {
    const n = Number(value);
    if (!Number.isFinite(n)) return "N/A";
    return `$${n}`;
}

function clamp(n: number, min: number, max: number): number {
    return Math.max(min, Math.min(max, n));
}

export function ProductCarouselCard({ props, data, onAction }: UiComponentProps) {
    const title = String((props as any)?.title ?? "Products");
    const dataKey = String((props as any)?.dataKey ?? "").trim();
    const dataSourceId = String((props as any)?.dataSourceId ?? (props as any)?.data_source ?? "").trim();
    const actionId = String((props as any)?.actionId ?? "").trim();

    // click on card
    const actionType = resolveActionType((props as any) ?? {}, actionId ? "ui.action.execute" : "data.query");

    // optional button action inside card
    const secondaryActionId = String((props as any)?.secondaryActionId ?? "").trim();
    const secondaryActionType = String((props as any)?.secondaryActionType ?? "").trim();
    const userId = Number((props as any)?.userId ?? 1);

    // card width
    const cardWidthPx = clamp(Number((props as any)?.cardWidthPx ?? 260), 180, 420);

    // how many cards to scroll per "Next/Prev"
    const scrollCards = clamp(Number((props as any)?.scrollCards ?? 3), 1, 10);

    const toolResult = useMemo(() => {
        const candidate = (props as any)?.products ?? (props as any)?.items ?? (props as any)?.data;

        // Planner can emit unresolved templates like "${step_1.products}".
        // Ignore scalar/template values and resolve from dataKey instead.
        const isTemplateString =
            typeof candidate === "string" &&
            candidate.includes("${") &&
            candidate.includes("}");
        const isUsableDirect =
            candidate != null &&
            !isTemplateString &&
            (Array.isArray(candidate) || typeof candidate === "object");

        if (isUsableDirect) return candidate;
        return resolveFromDataKey((data ?? {}) as Record<string, any>, dataKey);
    }, [props, data, dataKey]);
    const products = useMemo(() => resolveProducts(toolResult), [toolResult]);

    const railRef = useRef<HTMLDivElement | null>(null);
    const [canPrev, setCanPrev] = useState(false);
    const [canNext, setCanNext] = useState(false);

    const updateNav = () => {
        const el = railRef.current;
        if (!el) return;
        const maxScroll = el.scrollWidth - el.clientWidth;
        setCanPrev(el.scrollLeft > 4);
        setCanNext(el.scrollLeft < maxScroll - 4);
    };

    useEffect(() => {
        updateNav();
        const el = railRef.current;
        if (!el) return;

        const onScroll = () => updateNav();
        const onResize = () => updateNav();

        el.addEventListener("scroll", onScroll, { passive: true });
        window.addEventListener("resize", onResize);

        return () => {
            el.removeEventListener("scroll", onScroll);
            window.removeEventListener("resize", onResize);
        };
    }, [products.length, cardWidthPx]);

    const scrollByCards = (dir: -1 | 1) => {
        const el = railRef.current;
        if (!el) return;
        const gap = 12;
        const delta = dir * (scrollCards * (cardWidthPx + gap));
        el.scrollBy({ left: delta, behavior: "smooth" });
    };

    const firePrimary = (p: Product) => {
        const fallbackPayload = actionId
            ? { actionId, dataSourceId, id: p.id, productId: p.id }
            : dataSourceId
                ? { dataSourceId, id: p.id, productId: p.id }
                : { productId: p.id };
        const payload = resolveActionPayload(
            (props as any) ?? {},
            fallbackPayload,
            { data: (data ?? {}) as Record<string, any>, item: p as any, extra: { productId: p.id } }
        );
        onAction?.(actionType, payload);
    };

    const fireSecondary = (p: Product) => {
        if (!secondaryActionType && !secondaryActionId) return;
        const secondaryType = resolveActionType(
            { actionType: secondaryActionType },
            secondaryActionId ? "ui.action.execute" : secondaryActionType
        );

        if (secondaryActionType === "shop.add_to_cart" || secondaryActionId) {
            const fallbackPayload = {
                actionId: secondaryActionId || undefined,
                userId,
                products: [{ id: p.id, quantity: 1 }],
            };
            onAction?.(
                secondaryType,
                resolveActionPayload(
                    { payloadTemplate: (props as any)?.secondaryPayloadTemplate },
                    fallbackPayload,
                    { data: (data ?? {}) as Record<string, any>, item: p as any, extra: fallbackPayload }
                )
            );
            return;
        }

        const genericPayload = { actionId: secondaryActionId || undefined, productId: p.id, quantity: 1 };
        onAction?.(
            secondaryType,
            resolveActionPayload(
                { payloadTemplate: (props as any)?.secondaryPayloadTemplate },
                genericPayload,
                { data: (data ?? {}) as Record<string, any>, item: p as any, extra: genericPayload }
            )
        );
    };

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
            <div className="flex items-start justify-between gap-3">
                <div>
                    <div className="text-sm font-semibold">{title}</div>
                    <div className="mt-0.5 text-xs opacity-60">
                        {products.length ? `${products.length} items` : "No data"}
                        {dataKey ? ` • dataKey: ${dataKey}` : ""}
                    </div>
                </div>

                <div className="flex items-center gap-2">
                    <button
                        type="button"
                        disabled={!canPrev}
                        onClick={() => scrollByCards(-1)}
                        className="rounded-xl border border-zinc-300 bg-white px-3 py-2 text-sm font-medium hover:bg-zinc-50 disabled:opacity-40 dark:border-zinc-700 dark:bg-zinc-950 dark:hover:bg-zinc-900"
                    >
                        Prev
                    </button>
                    <button
                        type="button"
                        disabled={!canNext}
                        onClick={() => scrollByCards(1)}
                        className="rounded-xl border border-zinc-300 bg-white px-3 py-2 text-sm font-medium hover:bg-zinc-50 disabled:opacity-40 dark:border-zinc-700 dark:bg-zinc-950 dark:hover:bg-zinc-900"
                    >
                        Next
                    </button>
                </div>
            </div>

            {!products.length ? (
                <div className="mt-3 text-sm opacity-70">
                    Nu există produse în date. Aștept:
                    <div className="mt-1 font-mono text-xs opacity-70">
                        {dataKey || "shop.searchProducts"} → {"{ products: [...] }"} (sau direct array)
                    </div>
                </div>
            ) : (
                <>
                    {/* Rail (real carousel) */}
                    <div
                        ref={railRef}
                        className="mt-4 flex gap-3 overflow-x-auto pb-2"
                        style={{
                            scrollSnapType: "x mandatory",
                            WebkitOverflowScrolling: "touch",
                        }}
                    >
                        {products.map((p) => {
                            const img = p.thumbnail ?? (Array.isArray(p.images) ? p.images[0] : undefined);

                            return (
                                <div
                                    key={String(p.id)}
                                    className="flex-shrink-0 snap-start"
                                    style={{ width: `${cardWidthPx}px`, scrollSnapAlign: "start" as any }}
                                >
                                    <div className="group overflow-hidden rounded-2xl border border-zinc-200/70 bg-white shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
                                        <button type="button" onClick={() => firePrimary(p)} className="w-full text-left">
                                            <div className="h-40 w-full bg-zinc-100 dark:bg-zinc-900">
                                                {img ? (
                                                    <img src={img} alt={p.title ?? "product"} className="h-full w-full object-cover" />
                                                ) : (
                                                    <div className="flex h-full items-center justify-center text-xs opacity-60">no image</div>
                                                )}
                                            </div>

                                            <div className="p-3">
                                                <div className="line-clamp-1 text-sm font-semibold">{p.title ?? `Product ${p.id}`}</div>
                                                <div className="mt-1 line-clamp-2 text-xs opacity-70">{p.description ?? ""}</div>

                                                <div className="mt-2 flex items-center justify-between">
                                                    <div className="text-sm font-semibold">{formatMoney(p.price)}</div>
                                                    <div className="text-xs opacity-70">{p.rating != null ? `⭐ ${p.rating}` : ""}</div>
                                                </div>

                                                <div className="mt-1 text-xs opacity-60">
                                                    {p.brand ? p.brand : ""}
                                                    {p.brand && p.category ? " • " : ""}
                                                    {p.category ? p.category : ""}
                                                </div>
                                            </div>
                                        </button>

                                        {secondaryActionType || secondaryActionId ? (
                                            <div className="px-3 pb-3">
                                                <button
                                                    type="button"
                                                    onClick={() => fireSecondary(p)}
                                                    className="w-full rounded-xl border border-zinc-300 bg-white px-3 py-2 text-sm font-medium hover:bg-zinc-50 dark:border-zinc-700 dark:bg-zinc-950 dark:hover:bg-zinc-900"
                                                >
                                                    {secondaryActionType === "shop.add_to_cart" ? "Add to cart" : "Action"}
                                                </button>
                                            </div>
                                        ) : null}
                                    </div>
                                </div>
                            );
                        })}
                    </div>

                    {/* Hint */}
                    <div className="mt-2 text-xs opacity-60">Poți face scroll cu mouse/trackpad. Snap e activ.</div>
                </>
            )}
        </div>
    );
}
