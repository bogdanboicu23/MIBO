import React from "react";
import { registry } from "./registry";

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
    name: string; // ex: pieChart, dataTable, productCarousel, kpiCard
    props?: Record<string, unknown>;
    children?: UiNode[];
    id?: string;
};

export type UiBinding = {
    componentPath: string; // "/root/children/0"
    prop: string;          // "dataKey"
    from: string;          // key în ui.data
};

export type UiSubscription = {
    event: string;
    refresh: Array<{ tool: string; args: Record<string, unknown>; patchPath: string }>;
};

export type UiComponentProps = {
    props?: Record<string, unknown>;
    data?: Record<string, unknown>;
    onAction?: (type: string, payload: Record<string, unknown>) => void;
};

export function UiRenderer(props: {
    ui: UiV1;
    onAction: (type: string, payload: Record<string, unknown>) => void;
}) {
    const { ui, onAction } = props;
    return <div>{renderNode(ui.root, ui.data ?? {}, onAction)}</div>;
}

function renderNode(
    node: UiNode,
    data: Record<string, unknown>,
    onAction: (type: string, payload: Record<string, unknown>) => void
): React.ReactNode {
    if (node.type === "layout") {
        const children = (node.children ?? []).map((c, idx) => (
            <React.Fragment key={c.id ?? idx}>{renderNode(c, data, onAction)}</React.Fragment>
        ));

        if (node.name === "row") return <div style={{ display: "flex", gap: 12 }}>{children}</div>;
        if (node.name === "grid") return <div style={{ display: "grid", gap: 12 }}>{children}</div>;

        return <div style={{ display: "flex", flexDirection: "column", gap: 12 }}>{children}</div>;
    }

    const Cmp = registry[node.name] ?? registry["__unknown__"];
    return <Cmp props={node.props} data={data} onAction={onAction} />;
}