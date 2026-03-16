import { useMemo, useState } from "react";
import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";
import { extractObject, resolveFromDataKey } from "@/components/sandbox/registry/dataResolver";
import { resolveActionPayload, resolveActionType } from "@/components/sandbox/registry/actionResolver";

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

type GenericEntity = Record<string, unknown>;

function isRenderableProduct(value: unknown): value is Product {
    if (!value || typeof value !== "object") return false;
    const product = value as Product;
    return Boolean(
        (typeof product.title === "string" && product.title.trim()) ||
        typeof product.price === "number" ||
        (Array.isArray(product.images) && product.images.length > 0) ||
        (typeof product.thumbnail === "string" && product.thumbnail.trim())
    );
}

function resolveEntity(props: any, data: any): GenericEntity | null {
    const direct = props?.product ?? props?.data;
    if (direct && typeof direct === "object") return direct as GenericEntity;

    const dataKey = typeof props?.dataKey === "string" ? props.dataKey.trim() : "";
    if (dataKey) {
        const resolved = resolveFromDataKey((data ?? {}) as Record<string, any>, dataKey);
        if (Array.isArray(resolved) && resolved.length && typeof resolved[0] === "object") {
            return resolved[0] as GenericEntity;
        }
        const fromObject = extractObject(resolved, ["product", "item"]);
        if (fromObject) return fromObject as GenericEntity;
    }

    // tool result often is { ...product fields } under some key:
    for (const key of Object.keys(data ?? {})) {
        const val = data?.[key];
        if (val && typeof val === "object" && (val as any).id != null) {
            return val as GenericEntity;
        }
    }

    return null;
}

function resolveProduct(props: any, data: any): Product | null {
    const entity = resolveEntity(props, data);
    return isRenderableProduct(entity) ? entity : null;
}

function pickImage(p: Product, idx: number): string | null {
    const list = Array.isArray(p.images) && p.images.length ? p.images : [];
    return list[idx] ?? p.thumbnail ?? (list.length ? list[0] : null) ?? null;
}

export function ProductDetailCard({ props, data, onAction }: UiComponentProps) {
    const addToCartActionId = String((props as any)?.addToCartActionId ?? "").trim();
    const actionTypeFromLegacy = String((props as any)?.addToCartActionType ?? "").trim();
    const addToCartActionType = resolveActionType(
        (props as any) ?? { actionType: actionTypeFromLegacy },
        addToCartActionId ? "ui.action.execute" : "shop.add_to_cart"
    );
    const userId = Number((props as any)?.userId ?? 1);

    const entity = useMemo(() => resolveEntity(props as any, data as any), [props, data]);
    const product = useMemo(() => resolveProduct(props as any, data as any), [props, data]);
    const [qty, setQty] = useState(1);
    const [imgIdx, setImgIdx] = useState(0);

    if (!product && entity) {
        const title =
            String(
                (entity as any).accountName ??
                (entity as any).name ??
                (entity as any).title ??
                "Details"
            );
        const fields = Object.entries(entity)
            .filter(([key]) => !["images", "thumbnail", "description"].includes(key))
            .slice(0, 8);

        return (
            <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
                <div className="text-sm font-semibold">{title}</div>
                <div className="mt-3 grid gap-2">
                    {fields.map(([key, value]) => (
                        <div key={key} className="flex items-start justify-between gap-4 rounded-xl border border-zinc-200/70 px-3 py-2 text-sm dark:border-zinc-800/70">
                            <span className="text-zinc-500 dark:text-zinc-400">{key}</span>
                            <span className="text-right text-zinc-900 dark:text-zinc-100">{String(value ?? "—")}</span>
                        </div>
                    ))}
                </div>
            </div>
        );
    }

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
        const fallbackPayload = {
            actionId: addToCartActionId || undefined,
            userId,
            products: [{ id: product.id, quantity: Math.max(1, qty) }],
        };
        const payload = resolveActionPayload(
            (props as any) ?? {},
            fallbackPayload,
            { data: (data ?? {}) as Record<string, any>, item: product as any, value: qty, form: { quantity: qty }, extra: fallbackPayload }
        );
        onAction?.(addToCartActionType, payload);
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
