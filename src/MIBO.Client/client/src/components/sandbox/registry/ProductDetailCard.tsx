import { useMemo, useState } from "react";
import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";

type Product = {
    id: string | number;
    title?: string;
    description?: string;
    price?: number;
    brand?: string;
    category?: string;
    thumbnail?: string;
    images?: string[];
    rating?: number;
    stock?: number;
};

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

function resolveProduct(props: any, data: any): Product | null {
    const direct = props?.product ?? props?.data;
    if (direct && typeof direct === "object") return direct as Product;

    const dataKey = typeof props?.dataKey === "string" ? props.dataKey.trim() : "";
    if (dataKey) {
        const v1 = data?.[dataKey];
        if (v1 && typeof v1 === "object") return v1 as Product;

        const v2 = getDot(data, dataKey);
        if (v2 && typeof v2 === "object") return v2 as Product;

        const last = dataKey.split(".").filter(Boolean).at(-1);
        if (last) {
            const v3 = data?.[last];
            if (v3 && typeof v3 === "object") return v3 as Product;
        }
    }

    // tool result often is { ...product fields } under some key:
    for (const key of Object.keys(data ?? {})) {
        const val = data?.[key];
        if (val && typeof val === "object" && (val as any).id != null && ((val as any).title || (val as any).price != null)) {
            return val as Product;
        }
    }

    return null;
}

function pickImage(p: Product, idx: number): string | null {
    const list = Array.isArray(p.images) && p.images.length ? p.images : [];
    return list[idx] ?? p.thumbnail ?? (list.length ? list[0] : null) ?? null;
}

export function ProductDetailCard({ props, data, onAction }: UiComponentProps) {
    const addToCartActionType = String((props as any)?.addToCartActionType ?? "shop.add_to_cart").trim() || "shop.add_to_cart";
    const userId = Number((props as any)?.userId ?? 1);

    const product = useMemo(() => resolveProduct(props as any, data as any), [props, data]);
    const [qty, setQty] = useState(1);
    const [imgIdx, setImgIdx] = useState(0);

    if (!product) {
        return (
            <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
                <div className="text-sm font-semibold">Product</div>
                <div className="mt-2 text-sm opacity-70">
                    No product (provide props.product or data[dataKey]).
                </div>
            </div>
        );
    }

    const img = pickImage(product, imgIdx);
    const images = Array.isArray(product.images) ? product.images : [];

    const addToCart = () => {
        onAction?.(addToCartActionType, {
            userId,
            products: [{ id: product.id, quantity: Math.max(1, qty) }],
        });
    };

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
            <div className="text-sm font-semibold">{product.title ?? `Product ${String(product.id)}`}</div>

            <div className="mt-3 grid gap-4 md:grid-cols-2">
                <div>
                    {img ? (
                        <img src={img} alt={product.title ?? "product"} className="h-64 w-full rounded-xl object-cover" />
                    ) : (
                        <div className="flex h-64 items-center justify-center rounded-xl bg-zinc-100 text-xs opacity-70 dark:bg-zinc-900">
                            no image
                        </div>
                    )}

                    {images.length > 1 ? (
                        <div className="mt-2 flex gap-2 overflow-x-auto pb-1">
                            {images.slice(0, 12).map((u, i) => (
                                <button
                                    key={u}
                                    type="button"
                                    onClick={() => setImgIdx(i)}
                                    className={[
                                        "h-12 w-12 flex-shrink-0 overflow-hidden rounded-lg border",
                                        i === imgIdx
                                            ? "border-zinc-900 dark:border-white"
                                            : "border-zinc-200/70 dark:border-zinc-800/70",
                                    ].join(" ")}
                                >
                                    <img src={u} alt={`thumb-${i}`} className="h-full w-full object-cover" />
                                </button>
                            ))}
                        </div>
                    ) : null}
                </div>

                <div>
                    <div className="text-sm opacity-80">{product.brand ?? product.category ?? ""}</div>

                    <div className="mt-2 flex items-center justify-between">
                        <div className="text-2xl font-semibold">{product.price != null ? String(product.price) : "N/A"}</div>
                        <div className="text-xs opacity-70">
                            {product.rating != null ? `rating: ${product.rating}` : ""}
                            {product.stock != null ? ` • stock: ${product.stock}` : ""}
                        </div>
                    </div>

                    {product.description ? (
                        <div className="mt-3 text-sm opacity-80">{product.description}</div>
                    ) : null}

                    <div className="mt-4 flex items-center gap-2">
                        <div className="text-sm opacity-70">Qty</div>
                        <button
                            type="button"
                            className="rounded-lg border border-zinc-300 px-2 py-1 text-sm dark:border-zinc-700"
                            onClick={() => setQty((q) => Math.max(1, q - 1))}
                        >
                            -
                        </button>
                        <div className="min-w-8 text-center text-sm font-medium">{qty}</div>
                        <button
                            type="button"
                            className="rounded-lg border border-zinc-300 px-2 py-1 text-sm dark:border-zinc-700"
                            onClick={() => setQty((q) => q + 1)}
                        >
                            +
                        </button>
                    </div>

                    <button
                        type="button"
                        onClick={addToCart}
                        className="mt-4 w-full rounded-xl border border-zinc-300 bg-white px-4 py-2 text-sm font-medium hover:bg-zinc-50 dark:border-zinc-700 dark:bg-zinc-950 dark:hover:bg-zinc-900"
                    >
                        Add to cart
                    </button>

                    <div className="mt-2 text-xs opacity-60">
                        Payload: <code>{"{ userId, products: [{ id, quantity }] }"}</code>
                    </div>
                </div>
            </div>
        </div>
    );
}