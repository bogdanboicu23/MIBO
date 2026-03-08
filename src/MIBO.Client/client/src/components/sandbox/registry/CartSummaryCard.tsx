import { useMemo } from "react";
import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";
import { extractObject, resolveFromDataKey } from "@/components/sandbox/registry/dataResolver";
import { resolveActionPayload, resolveActionType } from "@/components/sandbox/registry/actionResolver";

function resolveCart(props: any, data: any): any {
    const direct = props?.cart ?? props?.data;
    if (direct && typeof direct === "object") return direct;

    const dataKey = typeof props?.dataKey === "string" ? props.dataKey.trim() : "";
    if (dataKey) {
        const resolved = resolveFromDataKey((data ?? {}) as Record<string, any>, dataKey);
        const object = extractObject(resolved, ["cart"]);
        if (object) return object;
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
    const actionTypeFromLegacy = String((props as any)?.checkoutActionType ?? "").trim();
    const checkoutActionType = resolveActionType((props as any) ?? { actionType: actionTypeFromLegacy }, "shop.checkout");
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
                onClick={() => {
                    const fallbackPayload = { cartId: cart.id };
                    const payload = resolveActionPayload(
                        (props as any) ?? {},
                        fallbackPayload,
                        { data: (data ?? {}) as Record<string, any>, item: cart as any, extra: fallbackPayload }
                    );
                    onAction?.(checkoutActionType, payload);
                }}
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
