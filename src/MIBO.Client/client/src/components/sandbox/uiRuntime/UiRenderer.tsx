import React from "react";
import type { ComponentType } from "react";
import { registry as defaultRegistry } from "./registry";

/** -------------------- Types -------------------- */

export type UiV1 = {
    schema: "ui.v1";
    uiInstanceId?: string;
    root: UiNode;
    data: Record<string, unknown>;
    bindings: UiBinding[];
    subscriptions: UiSubscription[];
    meta?: Record<string, unknown>;
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
    refresh: Array<
        | { dataSourceId: string; args?: Record<string, unknown> }
        | { tool: string; args: Record<string, unknown>; patchPath: string }
    >;
};

export type UiComponentProps = {
    props?: Record<string, unknown>;
    data?: Record<string, unknown>;
    meta?: Record<string, unknown>;
    onAction?: (type: string, payload: Record<string, unknown>) => void;

    /**
     * Allows meta components (Carousel) to render other registry components dynamically.
     */
    registry?: Record<string, ComponentType<UiComponentProps>>;
};

function isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === "object" && value !== null && !Array.isArray(value);
}

function hasOwn(value: unknown, key: string): boolean {
    return isRecord(value) && Object.prototype.hasOwnProperty.call(value, key);
}

/** getByPath(data, "shop.listProducts.products") */
export function getByPath(obj: unknown, path: string): unknown {
    if (!path) return undefined;
    const parts = path.split(".").filter(Boolean);
    let cur: any = obj;
    let i = 0;
    while (i < parts.length) {
        if (cur == null) return undefined;
        // Try greedy match: "finance.getSummary" as a single key before splitting
        if (isRecord(cur)) {
            let found = false;
            for (let j = parts.length; j > i; j--) {
                const composite = parts.slice(i, j).join(".");
                if (hasOwn(cur, composite)) {
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

function normalizeComponentKey(name: string): string {
    return name.replace(/[^a-zA-Z0-9]/g, "").toLowerCase();
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

const FULL_REF_REGEX = /^\$\{([^}]+)\}$/;
const INLINE_REF_REGEX = /\{\{([^}]+)\}\}/g;

function resolveReferenceToken(token: string, data: Record<string, unknown>): unknown {
    const value = token.trim();
    if (!value) return undefined;

    if (value.startsWith("tool:")) {
        const path = value.slice(5).trim();
        return getByPath(data, path) ?? (data as any)[path];
    }

    if (value.startsWith("step:")) {
        const path = value.slice(5).trim();
        return getByPath(data, path) ?? (data as any)[path];
    }

    if (value.startsWith("ui.") || value.startsWith("context.")) {
        return getByPath(data, value) ?? (data as any)[value];
    }

    return getByPath(data, value) ?? (data as any)[value];
}

function resolveTemplateProps(value: unknown, data: Record<string, unknown>): unknown {
    if (Array.isArray(value)) return value.map((item) => resolveTemplateProps(item, data));

    if (isRecord(value)) {
        const next: Record<string, unknown> = {};
        for (const [key, child] of Object.entries(value as Record<string, unknown>)) {
            next[key] = resolveTemplateProps(child, data);
        }
        return next;
    }

    if (typeof value !== "string") return value;

    const full = FULL_REF_REGEX.exec(value.trim());
    if (full) {
        const resolved = resolveReferenceToken(full[1], data);
        return resolved === undefined ? value : resolved;
    }

    INLINE_REF_REGEX.lastIndex = 0;
    if (!INLINE_REF_REGEX.test(value)) return value;

    INLINE_REF_REGEX.lastIndex = 0;
    return value.replace(INLINE_REF_REGEX, (_, rawToken: string) => {
        const resolved = resolveReferenceToken(rawToken, data);
        return resolved === undefined || resolved === null ? "" : String(resolved);
    });
}

/** -------------------- Adapters (normalize/resolve per component) -------------------- */

type PropsAdapter = (props: Record<string, unknown>, data: Record<string, unknown>) => Record<string, unknown>;

function normalizeDataTableProps(props: Record<string, unknown>, data: Record<string, unknown>) {
    const p: any = { ...props };
    const resolveTemplateValue = (raw: unknown): unknown => {
        if (typeof raw !== "string") return undefined;
        const s = raw.trim();
        const m = /^\$\{(?:tool|step):(.+)\}$/.exec(s);
        const path = (m?.[1] ?? s).trim();
        if (!path) return undefined;
        return getByPath(data, path) ?? (data as any)[path];
    };

    // density mapping (legacy -> new)
    if (p.density === "medium") p.density = "normal";
    if (p.dense === true) p.density = "compact"; // accept old "dense"

    // columns mapping legacy: {name, selector} -> {header, key}
    if (Array.isArray(p.columns) && p.columns.length) {
        const first = p.columns[0];
        const isLegacy =
            hasOwn(first, "selector")
            || hasOwn(first, "name");
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
        const templateRows = resolveTemplateValue(p.rows);
        if (Array.isArray(templateRows)) {
            p.rows = templateRows;
        }

        let v = getByPath(data, p.dataKey) ?? (data as any)[p.dataKey];
        if (v === undefined && !p.dataKey.includes(".")) {
            // tolerate short dataKey like "products" when data is {"shop.listProducts": {products:[...]}}
            const byNestedKey = Object.values(data as any).find((obj: any) => hasOwn(obj, p.dataKey));
            if (isRecord(byNestedKey)) v = byNestedKey[p.dataKey];
        }
        if (Array.isArray(v)) {
            p.rows = v;
        } else if (isRecord(v)) {
            // Find the first array property (products, items, transactions, expenses, etc.)
            const arrProp = Object.values(v as any).find(Array.isArray);
            if (arrProp) p.rows = arrProp;
        }
    }

    // If columns keys don't exist in rows, try common mappings:
    // Example: LLM used selector "name" but data has "title"
    if (Array.isArray(p.rows) && p.rows.length && Array.isArray(p.columns) && p.columns.length) {
        const sample = isRecord(p.rows[0]) ? p.rows[0] : {};
        p.columns = p.columns.map((c: any) => {
            if (!isRecord(c) || typeof c.key !== "string" || !c.key) return c;
            if (hasOwn(sample, c.key)) return c;
            // heuristic mapping
            if (c.key === "name" && hasOwn(sample, "title")) return { ...c, key: "title" };
            return c;
        });
    }

    // sort: {} -> null (altfel e truthy și îți strică inițializarea)
    if (isRecord(p.sort) && !p.sort.key) p.sort = null;

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
        if (isRecord(v)) p.product = v;
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

function injectSourceFieldHints(
    props: Record<string, unknown>,
    meta: Record<string, unknown> | undefined
): Record<string, unknown> {
    if (!meta || isRecord(props.fieldHints)) {
        return props;
    }

    const registry = meta.sourceRegistry;
    if (!isRecord(registry)) {
        return props;
    }

    const dataKey = typeof props.dataKey === "string" && props.dataKey.trim()
        ? props.dataKey.trim()
        : typeof props.sourceKey === "string" && props.sourceKey.trim()
            ? props.sourceKey.trim()
            : "";

    if (!dataKey) {
        return props;
    }

    const sourceState = (registry as Record<string, any>)[dataKey];
    const fallbackSourceKey = dataKey.split(".").filter(Boolean)[0] ?? "";
    const fieldHints = sourceState?.fieldHints
        ?? ((registry as Record<string, any>)[fallbackSourceKey]?.fieldHints);
    if (!isRecord(fieldHints)) {
        return props;
    }

    return {
        ...props,
        fieldHints,
    };
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
    const meta = (ui?.meta ?? {}) as Record<string, unknown>;

    return <div>{renderNode(ui.root, "/root", data, bindings, meta, onAction, reg)}</div>;
}

function renderNode(
    node: UiNode,
    path: string,
    data: Record<string, unknown>,
    bindings: UiBinding[],
    meta: Record<string, unknown>,
    onAction: (type: string, payload: Record<string, unknown>) => void,
    reg: Record<string, ComponentType<UiComponentProps>>
): React.ReactNode {
    if (node.type === "layout") {
        const children = (node.children ?? []).map((c, idx) => {
            const childPath = pathJoin(path, `children/${idx}`);
            return <React.Fragment key={c.id ?? idx}>{renderNode(c, childPath, data, bindings, meta, onAction, reg)}</React.Fragment>;
        });

        const gap = (node.props?.gap as number) ?? 12;

        if (node.name === "row") return <div style={{ display: "flex", gap }}>{children}</div>;
        if (node.name === "grid") return <div style={{ display: "grid", gap }}>{children}</div>;

        return <div style={{ display: "flex", flexDirection: "column", gap }}>{children}</div>;
    }

    const component =
        reg[node.name] ??
        reg[
            Object.keys(reg).find((k) => normalizeComponentKey(k) === normalizeComponentKey(node.name)) ?? "__missing__"
        ];
    const Cmp = component ?? reg["__unknown__"];

    // 1) apply bindings for this component path
    const nodeBindings = bindingsFor(path, bindings);
    let mergedProps = applyBindingsToProps(node.props, data, nodeBindings);
    mergedProps = resolveTemplateProps(mergedProps, data) as Record<string, unknown>;

    // 2) normalize/resolve props for known components
    mergedProps = adaptProps(node.name, mergedProps, data);
    mergedProps = injectSourceFieldHints(mergedProps, meta);
    if (!component) {
        mergedProps = {
            ...mergedProps,
            __unknownComponent: node.name,
        };
    }

    // 3) render component with standardized UI metadata attached to payload
    const onActionFromComponent = (type: string, payload: Record<string, unknown>) => {
        const safePayload = isRecord(payload) ? payload : {};
        const nextPayload = {
            ...safePayload,
            __ui: {
                ...(safePayload as any).__ui,
                component: node.name,
                path,
                ts: new Date().toISOString(),
            },
        };
        onAction(type, nextPayload);
    };

    return <Cmp props={mergedProps} data={data} meta={meta} onAction={onActionFromComponent} registry={reg} />;
}
