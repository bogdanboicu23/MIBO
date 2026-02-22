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

function resolveCart(props: any, data: any): any {
    const direct = props?.cart ?? props?.data;
    if (direct && typeof direct === "object") return direct;

    const dataKey = typeof props?.dataKey === "string" ? props.dataKey.trim() : "";
    if (dataKey) {
        const v1 = data?.[dataKey];
        if (v1 && typeof v1 === "object") return v1;

        const v2 = getDot(data, dataKey);
        if (v2 && typeof v2 === "object") return v2;

        const last = dataKey.split(".").filter(Boolean).at(-1);
        if (last) {
            const v3 = data?.[last];
            if (v3 && typeof v3 === "object") return v3;
        }
    }

    for (const key of Object.keys(data ?? {})) {
        const val = data?.[key];
        if (val && typeof val === "object" && Array.isArray((val as any).products) && (val as any).total != null) {
            return val;
        }
    }

    return null;
}

export function CartSummaryCard({ props, data, onAction }: UiComponentProps) {
    const checkoutActionType = String((props as any)?.checkoutActionType ?? "shop.checkout").trim() || "shop.checkout";
    const cart = useMemo(() => resolveCart(props as any, data as any), [props, data]);

    if (!cart) {
        return (
            <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
                <div className="text-sm font-semibold">Cart</div>
                <div className="mt-2 text-sm opacity-70">No cart (provide props.cart or data[dataKey]).</div>
            </div>
        );
    }

    const items: any[] = Array.isArray(cart.products) ? cart.products : [];

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
            <div className="flex items-center justify-between gap-2">
                <div className="text-sm font-semibold">Cart Summary</div>
                <div className="text-xs opacity-70">cartId: {String(cart.id ?? "N/A")}</div>
            </div>

            <div className="mt-3 grid gap-3 sm:grid-cols-2">
                <div className="rounded-xl border border-zinc-200/70 p-3 dark:border-zinc-800/70">
                    <div className="text-xs opacity-70">Total</div>
                    <div className="mt-1 text-xl font-semibold">{String(cart.total ?? "N/A")}</div>
                </div>
                <div className="rounded-xl border border-zinc-200/70 p-3 dark:border-zinc-800/70">
                    <div className="text-xs opacity-70">Discounted</div>
                    <div className="mt-1 text-xl font-semibold">{String(cart.discountedTotal ?? "N/A")}</div>
                </div>
                <div className="rounded-xl border border-zinc-200/70 p-3 dark:border-zinc-800/70">
                    <div className="text-xs opacity-70">Items</div>
                    <div className="mt-1 text-xl font-semibold">{String(cart.totalProducts ?? items.length ?? "N/A")}</div>
                </div>
                <div className="rounded-xl border border-zinc-200/70 p-3 dark:border-zinc-800/70">
                    <div className="text-xs opacity-70">Quantity</div>
                    <div className="mt-1 text-xl font-semibold">{String(cart.totalQuantity ?? "N/A")}</div>
                </div>
            </div>

            {items.length ? (
                <div className="mt-4 overflow-auto rounded-xl border border-zinc-200/70 dark:border-zinc-800/70">
                    <table className="w-full text-left text-sm">
                        <thead className="bg-zinc-50 dark:bg-zinc-900">
                        <tr>
                            <th className="px-3 py-2 font-medium">Product</th>
                            <th className="px-3 py-2 font-medium">Qty</th>
                            <th className="px-3 py-2 font-medium">Price</th>
                            <th className="px-3 py-2 font-medium">Total</th>
                        </tr>
                        </thead>
                        <tbody>
                        {items.slice(0, 20).map((p, idx) => (
                            <tr key={idx} className="border-t border-zinc-200/70 dark:border-zinc-800/70">
                                <td className="px-3 py-2">{String(p.title ?? p.name ?? p.id ?? "")}</td>
                                <td className="px-3 py-2">{String(p.quantity ?? "")}</td>
                                <td className="px-3 py-2">{String(p.price ?? "")}</td>
                                <td className="px-3 py-2">{String(p.total ?? "")}</td>
                            </tr>
                        ))}
                        </tbody>
                    </table>
                </div>
            ) : (
                <div className="mt-3 text-sm opacity-70">Cart is empty.</div>
            )}

            <button
                type="button"
                onClick={() => onAction?.(checkoutActionType, { cartId: cart.id })}
                className="mt-4 w-full rounded-xl border border-zinc-300 bg-white px-4 py-2 text-sm font-medium hover:bg-zinc-50 dark:border-zinc-700 dark:bg-zinc-950 dark:hover:bg-zinc-900"
            >
                Checkout
            </button>

            <div className="mt-2 text-xs opacity-60">
                Payload: <code>{"{ cartId }"}</code>
            </div>
        </div>
    );
}