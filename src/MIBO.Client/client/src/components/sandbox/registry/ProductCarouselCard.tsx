import { useMemo, useRef, useState, useEffect } from "react";
import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";

type Product = {
    id: string | number;
    title?: string;
    name?: string;
    description?: string;
    price?: number;
    thumbnail?: string;
    images?: string[];
    image?: string;
    brand?: string;
    category?: string;
};

function isRecord(v: unknown): v is Record<string, unknown> {
    return !!v && typeof v === "object" && !Array.isArray(v);
}

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

function resolveProducts(
    props: Record<string, unknown> | undefined,
    data: Record<string, unknown> | undefined
): Product[] {
    const p: any = props ?? {};
    const d: any = data ?? {};

    const direct = (p.items ?? p.products ?? p.data) as unknown;
    if (Array.isArray(direct)) return direct as Product[];

    const dataKey = typeof p.dataKey === "string" ? p.dataKey.trim() : "";
    if (dataKey) {
        const v1 = d[dataKey];
        if (Array.isArray(v1)) return v1 as Product[];

        const v2 = getDot(d, dataKey);
        if (Array.isArray(v2)) return v2 as Product[];

        const last = dataKey.split(".").filter(Boolean).at(-1);
        if (last) {
            const v3 = d[last];
            if (Array.isArray(v3)) return v3 as Product[];
        }
    }

    for (const key of Object.keys(d)) {
        const val = d[key];
        if (isRecord(val) && Array.isArray((val as any).products)) {
            return (val as any).products as Product[];
        }
    }

    const arrays = Object.values(d).filter(Array.isArray) as unknown[][];
    if (arrays.length === 1) return arrays[0] as Product[];

    return [];
}

function normalizeActionType(raw: unknown): string {
    const t = String(raw ?? "").trim();
    if (!t) return "shop.buy";
    if (t === "buy") return "shop.buy";
    return t;
}

function pickImage(p: Product): string | null {
    return (
        p.thumbnail ??
        p.image ??
        (Array.isArray(p.images) && p.images.length ? p.images[0] : null) ??
        null
    );
}

export function ProductCarouselCard({ props, data, onAction }: UiComponentProps) {
    const title = String(props?.title ?? "Available Products");
    const actionType = normalizeActionType((props as any)?.actionType);
    const maxItems = Number((props as any)?.limit ?? 12);

    const products = useMemo(() => resolveProducts(props, data).slice(0, maxItems), [props, data, maxItems]);

    const scrollerRef = useRef<HTMLDivElement | null>(null);
    const [canPrev, setCanPrev] = useState(false);
    const [canNext, setCanNext] = useState(false);

    const updateNav = () => {
        const el = scrollerRef.current;
        if (!el) return;
        const maxScrollLeft = el.scrollWidth - el.clientWidth;
        const left = el.scrollLeft;

        setCanPrev(left > 2);
        setCanNext(left < maxScrollLeft - 2);
    };

    useEffect(() => {
        updateNav();
        const el = scrollerRef.current;
        if (!el) return;

        const onScroll = () => updateNav();
        el.addEventListener("scroll", onScroll, { passive: true });

        const ro = new ResizeObserver(() => updateNav());
        ro.observe(el);

        return () => {
            el.removeEventListener("scroll", onScroll);
            ro.disconnect();
        };
    }, [products.length]);

    const scrollByPage = (dir: -1 | 1) => {
        const el = scrollerRef.current;
        if (!el) return;

        // scroll “cam o pagină”, minus un pic ca să păstreze context
        const delta = Math.max(240, Math.floor(el.clientWidth * 0.9));
        el.scrollBy({ left: dir * delta, behavior: "smooth" });
    };

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
            <div className="flex items-center justify-between gap-3">
                <div>
                    <div className="text-sm font-semibold">{title}</div>
                    <div className="text-xs opacity-70">{products.length ? `${products.length} items` : ""}</div>
                </div>

                <div className="flex items-center gap-2">
                    <button
                        className="rounded-lg border border-zinc-300 bg-white px-3 py-1.5 text-sm font-medium hover:bg-zinc-50 disabled:opacity-40 dark:border-zinc-700 dark:bg-zinc-950 dark:hover:bg-zinc-900"
                        disabled={!canPrev}
                        onClick={() => scrollByPage(-1)}
                        aria-label="Previous"
                        type="button"
                    >
                        Prev
                    </button>
                    <button
                        className="rounded-lg border border-zinc-300 bg-white px-3 py-1.5 text-sm font-medium hover:bg-zinc-50 disabled:opacity-40 dark:border-zinc-700 dark:bg-zinc-950 dark:hover:bg-zinc-900"
                        disabled={!canNext}
                        onClick={() => scrollByPage(1)}
                        aria-label="Next"
                        type="button"
                    >
                        Next
                    </button>
                </div>
            </div>

            {products.length === 0 ? (
                <div className="mt-3 text-sm opacity-70">
                    Nu am găsit produse de afișat.
                    <div className="mt-2 text-xs opacity-60">
                        Aștept fie <code>props.items</code>, fie <code>props.dataKey</code> care să pointeze către o listă,
                        sau <code>ui.data[toolName].products</code>.
                    </div>
                </div>
            ) : (
                <div className="mt-3">
                    <div
                        ref={scrollerRef}
                        className="
              flex gap-3 overflow-x-auto pb-2
              snap-x snap-mandatory
              [-ms-overflow-style:none] [scrollbar-width:none]
            "
                        style={{ scrollBehavior: "smooth" }}
                    >
                        {/* hide scrollbar in webkit */}
                        <style>{`
              .hide-scrollbar::-webkit-scrollbar { display: none; }
            `}</style>

                        {products.map((p) => {
                            const name = p.title ?? p.name ?? `Product ${String(p.id)}`;
                            const price = p.price != null ? String(p.price) : "N/A";
                            const img = pickImage(p);

                            return (
                                <div
                                    key={String(p.id)}
                                    className="
                    snap-start
                    w-[82%] sm:w-[46%] lg:w-[31%]
                    flex-shrink-0
                    rounded-xl border border-zinc-200/70 p-3
                    dark:border-zinc-800/70
                  "
                                >
                                    {img ? (
                                        <img
                                            src={img}
                                            alt={name}
                                            className="h-32 w-full rounded-lg object-cover"
                                            loading="lazy"
                                        />
                                    ) : (
                                        <div className="flex h-32 items-center justify-center rounded-lg bg-zinc-100 text-xs opacity-70 dark:bg-zinc-900">
                                            no image
                                        </div>
                                    )}

                                    <div className="mt-2 line-clamp-2 text-sm font-medium">{name}</div>

                                    <div className="mt-1 flex items-center justify-between text-sm">
                                        <span className="opacity-70">{p.brand ?? p.category ?? ""}</span>
                                        <span className="font-semibold">{price}</span>
                                    </div>

                                    {p.description ? (
                                        <div className="mt-2 line-clamp-2 text-xs opacity-70">{p.description}</div>
                                    ) : null}

                                    <button
                                        className="mt-3 w-full rounded-lg border border-zinc-300 bg-white px-3 py-2 text-sm font-medium hover:bg-zinc-50 dark:border-zinc-700 dark:bg-zinc-950 dark:hover:bg-zinc-900"
                                        onClick={() => onAction?.(actionType, { productId: p.id, quantity: 1 })}
                                        type="button"
                                    >
                                        Buy
                                    </button>
                                </div>
                            );
                        })}
                    </div>

                    {/* optional: little progress hint */}
                    <div className="mt-1 text-xs opacity-60">
                        Tip: poți da scroll orizontal (trackpad / swipe) sau folosește Prev/Next.
                    </div>
                </div>
            )}
        </div>
    );
}