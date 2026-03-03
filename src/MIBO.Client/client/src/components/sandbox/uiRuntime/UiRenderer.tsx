import React from "react";
import type { ComponentType } from "react";
import { registry as defaultRegistry } from "./registry";

/** -------------------- Types -------------------- */

export type UiV1 = {
    schema: "ui.v1";
    root: UiNode;
    data: Record<string, unknown>;
    bindings: UiBinding[];
    subscriptions: UiSubscription[];
};

export type UiNode =
    | {
    type: "layout";
    name: "column" | "row" | "grid";
    props?: Record<string, unknown>;
    children: UiNode[];
    id?: string;
}
    | {
    type: "component";
    name: string;
    props?: Record<string, unknown>;
    children?: UiNode[];
    id?: string;
};

export type UiBinding = {
    componentPath: string; // "/root/children/0"
    prop: string; // "dataKey" | "rows" | etc
    from: string; // key/path in ui.data
};

export type UiSubscription = {
    event: string;
    refresh: Array<{ tool: string; args: Record<string, unknown>; patchPath: string }>;
};

export type UiComponentProps = {
    props?: Record<string, unknown>;
    data?: Record<string, unknown>;
    onAction?: (type: string, payload: Record<string, unknown>) => void;

    /**
     * Allows meta components (Carousel) to render other registry components dynamically.
     */
    registry?: Record<string, ComponentType<UiComponentProps>>;
};

/** getByPath(data, "shop.listProducts.products") */
export function getByPath(obj: unknown, path: string): unknown {
    if (!path) return undefined;
    const parts = path.split(".").filter(Boolean);
    let cur: any = obj;
    let i = 0;
    while (i < parts.length) {
        if (cur == null) return undefined;
        // Try greedy match: "finance.getSummary" as a single key before splitting
        if (typeof cur === "object") {
            let found = false;
            for (let j = parts.length; j > i; j--) {
                const composite = parts.slice(i, j).join(".");
                if (Object.prototype.hasOwnProperty.call(cur, composite)) {
                    cur = cur[composite];
                    i = j;
                    found = true;
                    break;
                }
            }
            if (found) continue;
        }
        cur = cur[parts[i]];
        i++;
    }
    return cur;
}

/** Path generator for bindings: "/root/children/0/children/1" */
function pathJoin(base: string, next: string) {
    if (base.endsWith("/")) return base + next;
    return base + "/" + next;
}

/** Extract bindings for a node path */
function bindingsFor(path: string, bindings: UiBinding[]) {
    return bindings.filter((b) => b.componentPath === path);
}

/** Apply bindings: binds prop from ui.data into node.props */
function applyBindingsToProps(
    nodeProps: Record<string, unknown> | undefined,
    data: Record<string, unknown>,
    bindings: UiBinding[]
) {
    const props = { ...(nodeProps ?? {}) };

    for (const b of bindings) {
        // allow "from" to be either direct key or dot-path
        const val = getByPath(data, b.from) ?? (data as any)[b.from];
        props[b.prop] = val;
    }

    return props;
}

/** -------------------- Adapters (normalize/resolve per component) -------------------- */

type PropsAdapter = (props: Record<string, unknown>, data: Record<string, unknown>) => Record<string, unknown>;

function normalizeDataTableProps(props: Record<string, unknown>, data: Record<string, unknown>) {
    const p: any = { ...props };

    // density mapping (legacy -> new)
    if (p.density === "medium") p.density = "normal";
    if (p.dense === true) p.density = "compact"; // accept old "dense"

    // columns mapping legacy: {name, selector} -> {header, key}
    if (Array.isArray(p.columns) && p.columns.length) {
        const first = p.columns[0];
        const isLegacy = first && (("selector" in first) || ("name" in first));
        if (isLegacy) {
            p.columns = p.columns.map((c: any) => ({
                key: c.selector ?? c.key,
                header: c.name ?? c.header,
                type: c.type,
                align: c.align,
                width: c.width,
            }));
        }
    }

    // resolve rows from dataKey if rows empty
    const rowsIsEmpty = !Array.isArray(p.rows) || p.rows.length === 0;
    if (rowsIsEmpty && typeof p.dataKey === "string" && p.dataKey.length) {
        const v = getByPath(data, p.dataKey) ?? (data as any)[p.dataKey];
        if (Array.isArray(v)) {
            p.rows = v;
        } else if (v && typeof v === "object") {
            // Find the first array property (products, items, transactions, expenses, etc.)
            const arrProp = Object.values(v as any).find(Array.isArray);
            if (arrProp) p.rows = arrProp;
        }
    }

    // If columns keys don't exist in rows, try common mappings:
    // Example: LLM used selector "name" but data has "title"
    if (Array.isArray(p.rows) && p.rows.length && Array.isArray(p.columns) && p.columns.length) {
        const sample = p.rows[0] ?? {};
        p.columns = p.columns.map((c: any) => {
            if (!c?.key) return c;
            if (c.key in sample) return c;
            // heuristic mapping
            if (c.key === "name" && "title" in sample) return { ...c, key: "title" };
            return c;
        });
    }

    // sort: {} -> null (altfel e truthy și îți strică inițializarea)
    if (p.sort && typeof p.sort === "object" && !p.sort.key) p.sort = null;

    // search: {} -> { placeholder? } ok, but keep
    return p;
}

function normalizeCarouselProps(props: Record<string, unknown>, data: Record<string, unknown>) {
    // Carousel meta component: usually uses dataKey+itemsPath and renders itemComponent.
    // We keep it light: just ensure itemsPath default.
    const p: any = { ...props };
    if (!p.itemsPath) p.itemsPath = "items";

    // optional: allow dataKey path to array directly
    // e.g. dataKey="shop.searchProducts.products"
    const dk = p.dataKey;
    if (typeof dk === "string" && dk.length) {
        const v = getByPath(data, dk) ?? (data as any)[dk];
        // nothing to do here; Carousel component should handle it, but you can precompute if you want
        p.__resolved = v
    }

    return p;
}

function normalizeProductDetailProps(props: Record<string, unknown>, data: Record<string, unknown>) {
    const p: any = { ...props };

    // if product not passed but dataKey exists, attempt to resolve
    if (!p.product && typeof p.dataKey === "string") {
        const v = getByPath(data, p.dataKey) ?? (data as any)[p.dataKey];
        if (v && typeof v === "object") p.product = v;
    }
    return p;
}

function normalizePieChartProps(props: Record<string, unknown>, data: Record<string, unknown>) {
    const p: any = { ...props };
    // some charts might use dataKey -> dataset; keep resolution flexible
    if (typeof p.dataKey === "string") {
        const v = getByPath(data, p.dataKey) ?? (data as any)[p.dataKey];
        if (v != null && p.data == null) p.data = v; // optional pattern if your chart reads props.data
    }
    return p;
}

function normalizeKpiCardProps(props: Record<string, unknown>, data: Record<string, unknown>) {
    const p: any = { ...props };
    // if valueKey exists, resolve value into "value" (optional – only if your KpiCard supports it)
    if (typeof p.valueKey === "string") {
        const v = getByPath(data, p.valueKey) ?? (data as any)[p.valueKey];
        if (p.value == null) p.value = v;
    }
    return p;
}

/**
 * Central adapter registry. Add new component adapters here, fără să modifici UiRenderer logic.
 */
const componentAdapters: Record<string, PropsAdapter> = {
    dataTable: normalizeDataTableProps,
    carousel: normalizeCarouselProps,
    productDetail: normalizeProductDetailProps,
    pieChart: normalizePieChartProps,
    kpiCard: normalizeKpiCardProps,
    // productCarousel: (p,d) => p, etc (optional)
};

function adaptProps(componentName: string, props: Record<string, unknown>, data: Record<string, unknown>) {
    const adapter = componentAdapters[componentName];
    return adapter ? adapter(props, data) : props;
}

/** -------------------- Renderer -------------------- */

export function UiRenderer(props: {
    ui: UiV1;
    onAction: (type: string, payload: Record<string, unknown>) => void;
    registry?: Record<string, ComponentType<UiComponentProps>>;
}) {
    const { ui, onAction } = props;

    const reg = props.registry ?? defaultRegistry;
    const data = (ui?.data ?? {}) as Record<string, unknown>;
    const bindings = ui?.bindings ?? [];

    return <div>{renderNode(ui.root, "/root", data, bindings, onAction, reg)}</div>;
}

function renderNode(
    node: UiNode,
    path: string,
    data: Record<string, unknown>,
    bindings: UiBinding[],
    onAction: (type: string, payload: Record<string, unknown>) => void,
    reg: Record<string, ComponentType<UiComponentProps>>
): React.ReactNode {
    if (node.type === "layout") {
        const children = (node.children ?? []).map((c, idx) => {
            const childPath = pathJoin(path, `children/${idx}`);
            return <React.Fragment key={c.id ?? idx}>{renderNode(c, childPath, data, bindings, onAction, reg)}</React.Fragment>;
        });

        const gap = (node.props?.gap as number) ?? 12;

        if (node.name === "row") return <div style={{ display: "flex", gap }}>{children}</div>;
        if (node.name === "grid") return <div style={{ display: "grid", gap }}>{children}</div>;

        return <div style={{ display: "flex", flexDirection: "column", gap }}>{children}</div>;
    }

    const Cmp = reg[node.name] ?? reg["__unknown__"];

    // 1) apply bindings for this component path
    const nodeBindings = bindingsFor(path, bindings);
    let mergedProps = applyBindingsToProps(node.props, data, nodeBindings);

    // 2) normalize/resolve props for known components
    mergedProps = adaptProps(node.name, mergedProps, data);

    // 3) render component
    return <Cmp props={mergedProps} data={data} onAction={onAction} registry={reg} />;
}