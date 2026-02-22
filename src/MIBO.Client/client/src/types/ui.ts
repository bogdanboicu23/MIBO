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
    id?: string; // optional but useful
}
    | {
    type: "component";
    name: string; // registry key: pieChart, productCarousel, ...
    props?: Record<string, unknown>;
    children?: UiNode[];
    id?: string;
};

export type UiBinding = {
    componentPath: string; // e.g. "/root/children/0"
    prop: string;          // e.g. "dataKey"
    from: string;          // key in ui.data (tool result or computed key)
};

export type UiSubscription = {
    event: string; // e.g. "finance.expense_created"
    refresh: Array<{
        tool: string;
        args: Record<string, unknown>;
        patchPath: string; // where refreshed tool result should be written in ui.data
    }>;
};

export type UiPatchV1 = {
    schema: "ui.patch.v1";
    uiInstanceId?: string | null;
    ops: PatchOp[];
};

export type PatchOp =
    | { op: "set"; path: string; value: unknown }
    | { op: "merge"; path: string; value: Record<string, unknown> }
    | { op: "remove"; path: string };