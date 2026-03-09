import { useEffect, useMemo, useRef, useState } from "react";
import type { UiComponentProps } from "@/components/sandbox/registry/types";
import { extractArray, getByPath, resolveFromDataKey } from "@/components/sandbox/registry/dataResolver";

type AnyObj = Record<string, any>;

function resolveItems(toolResult: any, itemsPath?: string): any[] {
    const p = (itemsPath ?? "").trim();
    return extractArray(toolResult, p ? [p, "items", "products"] : ["items", "products"]);
}

function clamp(n: number, min: number, max: number): number {
    return Math.max(min, Math.min(max, n));
}

/**
 * Generic Carousel component
 *
 * props:
 * - title?: string
 * - dataKey: string                // tool name or dot-path into ui.data
 * - itemsPath?: string             // default "products"
 * - itemComponent: string          // registry key, ex: "productDetail"
 * - itemProps?: object             // base props passed to each item component
 * - itemPropMap?: object           // mapping to build item props (optional)
 *    - if empty, defaults to: { product: "$item" }
 *    - supported values:
 *        "$item"          -> whole item
 *        "$item.<path>"   -> getDot(item, path)
 *        any other value  -> literal
 * - cardWidthPx?: number           // default 360
 * - scrollCards?: number           // default 2 (Prev/Next buttons)
 */
export function Carousel({ props, data, onAction, registry }: UiComponentProps) {
    const title = String((props as any)?.title ?? "Carousel");

    const dataKey = String((props as any)?.dataKey ?? "").trim();
    const itemsPath = String((props as any)?.itemsPath ?? "products").trim();

    const itemComponent = String((props as any)?.itemComponent ?? "").trim();
    const baseItemProps = ((props as any)?.itemProps ?? {}) as AnyObj;
    const itemPropMap = ((props as any)?.itemPropMap ?? {}) as AnyObj;

    const cardWidthPx = clamp(Number((props as any)?.cardWidthPx ?? 360), 200, 640);
    const scrollCards = clamp(Number((props as any)?.scrollCards ?? 2), 1, 10);

    const toolResult = useMemo(
        () =>
            (props as any)?.items ??
            (props as any)?.data ??
            resolveFromDataKey((data ?? {}) as AnyObj, dataKey),
        [props, data, dataKey]
    );

    const items = useMemo(() => resolveItems(toolResult, itemsPath), [toolResult, itemsPath]);

    // resolve item renderer from registry
    const ItemCmp =
        (registry && itemComponent && registry[itemComponent]) ||
        (registry && registry["__unknown__"]);

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
    }, [items.length, cardWidthPx]);

    const scrollByCards = (dir: -1 | 1) => {
        const el = railRef.current;
        if (!el) return;
        const gap = 12;
        const delta = dir * (scrollCards * (cardWidthPx + gap));
        el.scrollBy({ left: delta, behavior: "smooth" });
    };

    const buildItemProps = (item: any): AnyObj => {
        const out: AnyObj = { ...baseItemProps };

        const mapEntries = Object.entries(itemPropMap ?? {});
        if (mapEntries.length === 0) {
            // default convention works great with ProductDetailCard expecting props.product
            out.product = item;
            return out;
        }

        for (const [k, v] of mapEntries) {
            if (typeof v === "string" && v.startsWith("$item")) {
                const rest = v.slice("$item".length); // "" or ".x.y"
                if (!rest) out[k] = item;
                else if (rest.startsWith(".")) out[k] = getByPath(item, rest.slice(1));
                else out[k] = item;
            } else {
                out[k] = v;
            }
        }

        return out;
    };

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
            <div className="flex items-start justify-between gap-3">
                <div>
                    <div className="text-sm font-semibold">{title}</div>
                    <div className="mt-0.5 text-xs opacity-60">
                        {items.length ? `${items.length} items` : "No data"}
                        {dataKey ? ` • dataKey: ${dataKey}` : ""}
                        {itemComponent ? ` • item: ${itemComponent}` : ""}
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

            {!items.length ? (
                <div className="mt-3 text-sm opacity-70">
                    No items found. Expected list at:
                    <div className="mt-1 font-mono text-xs opacity-70">
                        {dataKey || "<toolName>"} → .{itemsPath || "products"}
                    </div>
                </div>
            ) : !ItemCmp ? (
                <div className="mt-3 text-sm opacity-70">
                    Carousel cannot render because registry is missing.
                </div>
            ) : (
                <div
                    ref={railRef}
                    className="mt-4 flex gap-3 overflow-x-auto pb-2"
                    style={{ scrollSnapType: "x mandatory", WebkitOverflowScrolling: "touch" }}
                >
                    {items.map((item, idx) => (
                        <div
                            key={String((item as any)?.id ?? idx)}
                            className="flex-shrink-0 snap-start"
                            style={{ width: `${cardWidthPx}px`, scrollSnapAlign: "start" as any }}
                        >
                            <ItemCmp
                                props={buildItemProps(item)}
                                data={data}
                                onAction={onAction}
                                registry={registry}
                            />
                        </div>
                    ))}
                </div>
            )}
        </div>
    );
}
