export type UiV1 = {
    schema: "ui.v1";
    uiInstanceId?: string;
    root: UiNode;
    data?: Record<string, unknown>;
    bindings?: UiBinding[];
    subscriptions?: UiSubscription[];
    meta?: UiMeta;
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
    refresh: Array<
        | {
            dataSourceId: string;
            args?: Record<string, unknown>;
        }
        | {
            tool: string;
            args: Record<string, unknown>;
            patchPath: string; // where refreshed tool result should be written in ui.data
        }
    >;
};

export type UiMeta = {
    conversationId?: string;
    userId?: string;
    responseMode?: string | null;
    dataSources?: Record<string, UiDataSourceState>;
    actionRegistry?: Record<string, UiActionState>;
    sourceRegistry?: Record<string, UiDataSourceState>;
    runtime?: Record<string, unknown>;
    [key: string]: unknown;
};

export type UiFieldHints = {
    entityType?: string | null;
    collectionPath?: string | null;
    labelField?: string | null;
    valueField?: string | null;
    titleField?: string | null;
    imageField?: string | null;
    categoryField?: string | null;
    searchField?: string | null;
    defaultTableFields?: string[];
    textFields?: string[];
    numericFields?: string[];
    fields?: Array<{
        name: string;
        kind: string;
    }>;
};

export type UiTransformField = {
    key: string;
    label?: string | null;
    sourceField?: string | null;
    value?: unknown;
    expression?: Record<string, unknown> | null;
    kind?: string | null;
};

export type UiDataTransform = {
    id?: string | null;
    type: string;
    inputPath?: string | null;
    outputPath?: string | null;
    fields?: UiTransformField[];
    fieldHints?: UiFieldHints | null;
};

export type UiDataSourceState = {
    id: string;
    handler?: string;
    tool: string;
    resultKey?: string;
    patchPath?: string;
    mirrorPatchPaths?: string[];
    defaultArgs?: Record<string, unknown>;
    lastArgs?: Record<string, unknown>;
    activeTool?: string | null;
    refreshOnLoad?: boolean;
    refreshOnConversationOpen?: boolean;
    staleAfterMs?: number | null;
    lastFetchedAt?: number | null;
    params?: Record<string, unknown>;
    fieldHints?: UiFieldHints | null;
    transforms?: UiDataTransform[];
    variants?: Array<{
        tool: string;
        whenPayloadHasAny?: string[];
        whenPayloadHasAll?: string[];
        whenPayloadEquals?: Record<string, string>;
    }>;
};

export type UiActionState = {
    id: string;
    actionType: string;
    handler?: string;
    dataSourceId?: string | null;
    defaultArgs?: Record<string, unknown>;
    refreshDataSourceIds?: string[];
};

export type UiPatchV1 = {
    schema: "ui.patch.v1";
    uiInstanceId?: string | null;
    appliedAtUtc?: string;
    ops: PatchOp[];
};

export type PatchOp =
    | { op: "set"; path: string; value: unknown }
    | { op: "replace"; path: string; value: unknown }
    | { op: "merge"; path: string; value: Record<string, unknown> }
    | { op: "remove"; path: string };
