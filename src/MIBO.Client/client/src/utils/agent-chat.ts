import { CONFIG } from "@/global-config";
import type {
    UiActionState,
    UiBinding,
    UiDataSourceState,
    UiDataTransform,
    UiFieldHints,
    UiNode,
    UiSubscription,
    UiV1,
} from "@/types/ui";

export type AgentComponentSpec = {
    type: string;
    props?: Record<string, unknown>;
    data_source?: string;
    data_source_params?: Record<string, unknown>;
};

export type AgentDataSourceSpec = {
    id: string;
    handler?: string;
    default_args?: Record<string, unknown>;
    refresh_on_load?: boolean;
    refresh_on_conversation_open?: boolean;
    stale_after_ms?: number | null;
    transforms?: AgentDataTransformSpec[];
    field_hints?: AgentFieldHintsSpec | null;
};

export type AgentActionSpec = {
    id: string;
    action_type?: string;
    handler?: string;
    data_source_id?: string | null;
    default_args?: Record<string, unknown>;
    refresh_data_source_ids?: string[];
};

export type AgentFinalResponse = {
    text?: string;
    components?: AgentComponentSpec[];
    data_sources?: AgentDataSourceSpec[];
    actions?: AgentActionSpec[];
    subscriptions?: UiSubscription[];
};

export type AgentFieldDescriptor = {
    name: string;
    kind: string;
};

export type AgentFieldHintsSpec = {
    entity_type?: string | null;
    collection_path?: string | null;
    label_field?: string | null;
    value_field?: string | null;
    title_field?: string | null;
    image_field?: string | null;
    category_field?: string | null;
    search_field?: string | null;
    default_table_fields?: string[];
    text_fields?: string[];
    numeric_fields?: string[];
    fields?: AgentFieldDescriptor[];
};

export type AgentTransformFieldSpec = {
    key: string;
    label?: string | null;
    source_field?: string | null;
    value?: unknown;
    expression?: Record<string, unknown> | null;
    kind?: string | null;
};

export type AgentDataTransformSpec = {
    id?: string | null;
    type: string;
    input_path?: string | null;
    output_path?: string | null;
    fields?: AgentTransformFieldSpec[];
    field_hints?: AgentFieldHintsSpec | null;
};

type SourceContext = {
    key: string;
    dataSourceId: string;
    params: Record<string, unknown>;
    definition: AgentDataSourceSpec;
};

type GatewayQueryResponse = {
    dataSourceId?: string;
    handler?: string;
    data?: Record<string, unknown>;
    fieldHints?: UiFieldHints | null;
    fetchedAtUtc?: string;
};

type GatewayActionExecutionResponse = {
    actionId?: string;
    actionType?: string;
    dataSourceId?: string | null;
    data?: Record<string, unknown>;
    fieldHints?: UiFieldHints | null;
    fetchedAtUtc?: string | null;
    refreshes?: GatewayQueryResponse[];
};

const COMPONENT_NAME_ALIASES: Record<string, string> = {
    actionpanel: "actionPanel",
    actions: "actionPanel",
    barchart: "barChart",
    bar_chart: "barChart",
    carousel: "carousel",
    cartsummary: "cartSummary",
    categorychips: "categoryChips",
    data_table: "dataTable",
    datatable: "dataTable",
    finance_dashboard: "summaryPanel",
    gmail_composer: "emailComposer",
    gmailcomposer: "emailComposer",
    financeDashboard: "summaryPanel",
    email_composer: "emailComposer",
    emailcomposer: "emailComposer",
    formcard: "formCard",
    json: "jsonViewer",
    jsonviewer: "jsonViewer",
    kpi: "kpiCard",
    kpi_card: "kpiCard",
    kpicard: "kpiCard",
    line_chart: "lineChart",
    linechart: "lineChart",
    markdown: "markdown",
    page_title: "pageTitle",
    pagetitle: "pageTitle",
    pagination: "pagination",
    pie_chart: "pieChart",
    piechart: "pieChart",
    product_card: "productDetail",
    product_detail: "productDetail",
    product_list: "productCarousel",
    productcarousel: "productCarousel",
    productdetail: "productDetail",
    search: "searchBar",
    search_bar: "searchBar",
    searchbar: "searchBar",
    sort: "sortDropdown",
    sortdropdown: "sortDropdown",
    summary: "summaryPanel",
    summarypanel: "summaryPanel",
    text: "markdown",
    timeline: "timelineCard",
    timelinecard: "timelineCard",
};

function isRecord(value: unknown): value is Record<string, unknown> {
    return typeof value === "object" && value !== null && !Array.isArray(value);
}

function toNumber(value: unknown, fallback = 0): number {
    const numeric = Number(value);
    return Number.isFinite(numeric) ? numeric : fallback;
}

function toStringValue(value: unknown, fallback = ""): string {
    return typeof value === "string" ? value : fallback;
}

function compactTypeName(rawType: string): string {
    return rawType.replace(/[^a-zA-Z0-9_]/g, "").trim();
}

function toSandboxComponentName(rawType: unknown): string {
    const normalized = compactTypeName(String(rawType ?? ""));
    if (!normalized) return "markdown";
    const lowered = normalized.toLowerCase();
    return COMPONENT_NAME_ALIASES[normalized] ?? COMPONENT_NAME_ALIASES[lowered] ?? normalized;
}

function toUiFieldHints(fieldHints: AgentFieldHintsSpec | null | undefined): UiFieldHints | null {
    if (!isRecord(fieldHints)) {
        return null;
    }

    const fields = Array.isArray(fieldHints.fields)
        ? fieldHints.fields
            .filter((field) => isRecord(field) && typeof field.name === "string" && typeof field.kind === "string")
            .map((field) => ({
                name: String(field.name),
                kind: String(field.kind),
            }))
        : [];

    return {
        entityType: typeof fieldHints.entity_type === "string" ? fieldHints.entity_type : null,
        collectionPath: typeof fieldHints.collection_path === "string" ? fieldHints.collection_path : null,
        labelField: typeof fieldHints.label_field === "string" ? fieldHints.label_field : null,
        valueField: typeof fieldHints.value_field === "string" ? fieldHints.value_field : null,
        titleField: typeof fieldHints.title_field === "string" ? fieldHints.title_field : null,
        imageField: typeof fieldHints.image_field === "string" ? fieldHints.image_field : null,
        categoryField: typeof fieldHints.category_field === "string" ? fieldHints.category_field : null,
        searchField: typeof fieldHints.search_field === "string" ? fieldHints.search_field : null,
        defaultTableFields: Array.isArray(fieldHints.default_table_fields)
            ? fieldHints.default_table_fields.filter((field): field is string => typeof field === "string")
            : [],
        textFields: Array.isArray(fieldHints.text_fields)
            ? fieldHints.text_fields.filter((field): field is string => typeof field === "string")
            : [],
        numericFields: Array.isArray(fieldHints.numeric_fields)
            ? fieldHints.numeric_fields.filter((field): field is string => typeof field === "string")
            : [],
        fields,
    };
}

function toUiDataTransform(transform: AgentDataTransformSpec): UiDataTransform | null {
    if (!transform || typeof transform.type !== "string" || !transform.type.trim()) {
        return null;
    }

    return {
        id: typeof transform.id === "string" ? transform.id : null,
        type: transform.type,
        inputPath: typeof transform.input_path === "string" ? transform.input_path : null,
        outputPath: typeof transform.output_path === "string" ? transform.output_path : null,
        fields: Array.isArray(transform.fields)
            ? transform.fields
                .filter((field) => isRecord(field) && typeof field.key === "string" && field.key.trim())
                .map((field) => ({
                    key: String(field.key),
                    label: typeof field.label === "string" ? field.label : null,
                    sourceField: typeof field.source_field === "string" ? field.source_field : null,
                    value: field.value,
                    expression: isRecord(field.expression) ? field.expression : null,
                    kind: typeof field.kind === "string" ? field.kind : null,
                }))
            : [],
        fieldHints: toUiFieldHints(transform.field_hints),
    };
}

function buildSourceSignature(dataSourceId: string, params: Record<string, unknown>): string {
    return `${dataSourceId}::${JSON.stringify(
        Object.entries(params)
            .sort(([left], [right]) => left.localeCompare(right))
            .map(([key, value]) => [key, value])
    )}`;
}

function ensurePayloadTemplate(
    props: Record<string, unknown>,
    sourceKey: string,
    params: Record<string, unknown>
): Record<string, unknown> {
    const payloadTemplate = isRecord(props.payloadTemplate) ? props.payloadTemplate : {};
    return {
        ...payloadTemplate,
        ...params,
        sourceKey,
    };
}

function normalizeRows(
    rows: unknown,
    columns: unknown
): Record<string, unknown>[] {
    if (!Array.isArray(rows)) return [];
    if (rows.every((row) => isRecord(row))) {
        return rows as Record<string, unknown>[];
    }

    if (!rows.every(Array.isArray) || !Array.isArray(columns)) {
        return [];
    }

    const headers = columns.map((column, index) => {
        if (typeof column === "string" && column.trim()) return column.trim();
        if (isRecord(column) && typeof column.key === "string" && column.key.trim()) return column.key.trim();
        if (isRecord(column) && typeof column.header === "string" && column.header.trim()) {
            return column.header.trim();
        }
        return `column_${index + 1}`;
    });

    return rows.map((row) =>
        headers.reduce<Record<string, unknown>>((accumulator, header, index) => {
            accumulator[header] = (row as unknown[])[index];
            return accumulator;
        }, {})
    );
}

function mapFinanceItems(source: Record<string, unknown>): Array<Record<string, unknown>> {
    const candidateItems = source.items;
    if (Array.isArray(candidateItems)) {
        return candidateItems.filter(isRecord);
    }

    return [
        source.balance != null ? { label: "Balance", value: source.balance } : null,
        source.income != null ? { label: "Income", value: source.income } : null,
        source.expenses != null ? { label: "Expenses", value: source.expenses } : null,
        source.savings != null ? { label: "Savings", value: source.savings } : null,
    ].filter(Boolean) as Array<Record<string, unknown>>;
}

function deriveSourceData(
    componentName: string,
    props: Record<string, unknown>,
    params: Record<string, unknown>
): Record<string, unknown> | null {
    switch (componentName) {
        case "productCarousel": {
            const products = Array.isArray(props.products)
                ? props.products
                : Array.isArray(props.items)
                    ? props.items
                    : [];
            const query = toStringValue(params.q ?? params.query ?? props.initial_query ?? props.initialValue);
            const category = toStringValue(params.category ?? props.category);
            const sortBy = toStringValue(params.sortBy);
            const order = toStringValue(params.order);
            const sort = toStringValue(
                params.sort ?? (sortBy ? `${sortBy}:${order || "asc"}` : props.sort)
            );
            const limit = toNumber(params.limit ?? props.limit, products.length || 10);
            const skip = toNumber(params.skip ?? props.skip, 0);

            return {
                products,
                items: products,
                total: toNumber(props.total, products.length),
                limit,
                skip,
                query,
                category,
                sort,
                sortBy,
                order: order || "asc",
            };
        }

        case "dataTable": {
            const rows = normalizeRows(props.rows, props.columns);
            return {
                rows,
                items: rows,
                columns: Array.isArray(props.columns) ? props.columns : [],
                total: rows.length,
                limit: rows.length || 10,
                skip: 0,
            };
        }

        case "pieChart":
        case "barChart":
        case "lineChart":
            return {
                data: Array.isArray(props.data) ? props.data : [],
            };

        case "summaryPanel":
            return {
                items: mapFinanceItems(props),
                balance: props.balance,
                income: props.income,
                expenses: props.expenses,
                savings: props.savings,
            };

        case "categoryChips":
            return {
                categories: Array.isArray(props.items)
                    ? props.items
                    : Array.isArray(props.categories)
                        ? props.categories
                        : [],
                category: toStringValue(params.category),
            };

        case "kpiCard":
            return {
                label: props.label,
                value: props.value,
            };

        case "productDetail":
            return isRecord(props.product) ? props.product : isRecord(props.data) ? props.data : null;

        default:
            return null;
    }
}

function createSourceAwareProps(
    componentName: string,
    props: Record<string, unknown>,
    source: SourceContext
): { props: Record<string, unknown>; bindings: UiBinding[] } {
    const nextProps: Record<string, unknown> = {
        ...props,
        dataSourceId: source.dataSourceId,
        sourceKey: source.key,
        payloadTemplate: ensurePayloadTemplate(props, source.key, source.params),
    };
    const bindings: UiBinding[] = [];

    switch (componentName) {
        case "productCarousel":
            delete nextProps.products;
            delete nextProps.items;
            delete nextProps.data;
            nextProps.dataKey = source.key;
            break;

        case "productDetail":
            delete nextProps.product;
            delete nextProps.data;
            nextProps.dataKey = source.key;
            break;

        case "dataTable":
            delete nextProps.rows;
            nextProps.dataKey = source.key;
            break;

        case "pieChart":
        case "barChart":
        case "lineChart":
            delete nextProps.data;
            nextProps.dataKey = source.key;
            break;

        case "summaryPanel":
            delete nextProps.items;
            nextProps.dataKey = source.key;
            break;

        case "kpiCard":
            nextProps.dataKey = source.key;
            nextProps.valueKey = toStringValue(nextProps.valueKey, "value");
            break;

        case "searchBar":
            nextProps.initialValueKey = `${source.key}.query`;
            break;

        case "sortDropdown":
            nextProps.selectedKey = `${source.key}.sort`;
            break;

        case "categoryChips":
            nextProps.dataKey = source.key;
            bindings.push({
                componentPath: "",
                prop: "selectedKey",
                from: `${source.key}.category`,
            });
            break;

        case "pagination":
            nextProps.totalKey = `${source.key}.total`;
            nextProps.limitKey = `${source.key}.limit`;
            nextProps.skipKey = `${source.key}.skip`;
            break;

        default:
            break;
    }

    return { props: nextProps, bindings };
}

function inferInteractiveActionId(componentName: string, source: SourceContext): string | null {
    switch (componentName) {
        case "searchBar":
            return `${source.dataSourceId}.search`;
        case "sortDropdown":
            return `${source.dataSourceId}.sort`;
        case "categoryChips":
            return `${source.dataSourceId}.filter`;
        case "pagination":
            return `${source.dataSourceId}.paginate`;
        default:
            return null;
    }
}

function shouldPromoteTextToMarkdownUi(text: string): boolean {
    const candidate = text.trim();
    if (!candidate) {
        return false;
    }

    return (
        candidate.includes("```")
        || candidate.includes("\n")
        || /^#{1,6}\s/m.test(candidate)
        || /^\s*[-*+]\s/m.test(candidate)
        || /^\s*\d+\.\s/m.test(candidate)
    );
}

function inferLegacyDataSourceDefinition(dataSourceId: string, params: Record<string, unknown>): AgentDataSourceSpec {
    const normalized = dataSourceId.toLowerCase();

    if (normalized.includes("categories")) {
        return {
            id: dataSourceId,
            handler: "products.categories.list",
            default_args: {},
        };
    }

    if (normalized.includes("finance") || normalized.includes("finances")) {
        return {
            id: dataSourceId,
            handler: "finance.user.summary",
            default_args: { userId: toNumber(params.userId, 1) || 1 },
            refresh_on_conversation_open: true,
            stale_after_ms: 0,
        };
    }

    if (normalized.includes("detail") || normalized.includes("{id}") || normalized.endsWith("/products")) {
        return {
            id: dataSourceId,
            handler: "products.detail.get",
            default_args: {},
        };
    }

    return {
        id: dataSourceId,
        handler: "products.catalog.query",
        default_args: {},
    };
}

function toUiDataSourceState(
    definition: AgentDataSourceSpec,
    params: Record<string, unknown>
): UiDataSourceState {
    const mergedArgs = {
        ...(isRecord(definition.default_args) ? definition.default_args : {}),
        ...params,
    };
    const transforms = Array.isArray(definition.transforms)
        ? definition.transforms
            .map((transform) => toUiDataTransform(transform))
            .filter((transform): transform is UiDataTransform => transform !== null)
        : [];

    return {
        id: definition.id,
        handler: definition.handler || definition.id,
        tool: definition.handler || definition.id,
        defaultArgs: mergedArgs,
        lastArgs: mergedArgs,
        refreshOnLoad: definition.refresh_on_load === true,
        refreshOnConversationOpen: definition.refresh_on_conversation_open === true,
        staleAfterMs: typeof definition.stale_after_ms === "number" ? definition.stale_after_ms : null,
        lastFetchedAt: null,
        params,
        transforms,
        fieldHints: toUiFieldHints(definition.field_hints),
    };
}

function toUiActionState(action: AgentActionSpec): UiActionState {
    return {
        id: action.id,
        actionType: action.action_type || "ui.action.execute",
        handler: action.handler || undefined,
        dataSourceId: action.data_source_id ?? null,
        defaultArgs: isRecord(action.default_args) ? action.default_args : {},
        refreshDataSourceIds: Array.isArray(action.refresh_data_source_ids)
            ? action.refresh_data_source_ids
            : [],
    };
}

export function buildUiFromAgentResponse(response: AgentFinalResponse): UiV1 | null {
    const responseText = typeof response.text === "string" ? response.text : "";
    const declaredComponents = Array.isArray(response.components) ? response.components : [];
    const components =
        declaredComponents.length === 0 && shouldPromoteTextToMarkdownUi(responseText)
            ? [
                {
                    type: "markdown",
                    props: {
                        content: responseText,
                    },
                },
            ]
            : declaredComponents;

    if (components.length === 0) {
        return null;
    }

    const definedDataSources = Array.isArray(response.data_sources) ? response.data_sources : [];
    const definedActions = Array.isArray(response.actions) ? response.actions : [];
    const definedSubscriptions = Array.isArray(response.subscriptions) ? response.subscriptions : [];

    const dataSourceDefinitions = new Map<string, AgentDataSourceSpec>();
    for (const dataSource of definedDataSources) {
        if (!dataSource || typeof dataSource.id !== "string" || !dataSource.id.trim()) {
            continue;
        }

        dataSourceDefinitions.set(dataSource.id, dataSource);
    }

    const sourceMap = new Map<string, SourceContext>();
    const data: Record<string, unknown> = {};
    const bindings: UiBinding[] = [];
    const metaSources: Record<string, UiDataSourceState> = {};
    const actionRegistry = definedActions.reduce<Record<string, UiActionState>>((accumulator, action) => {
        if (!action || typeof action.id !== "string" || !action.id.trim()) {
            return accumulator;
        }

        accumulator[action.id] = toUiActionState(action);
        return accumulator;
    }, {});

    const children: UiNode[] = components.map((component, index) => {
        const componentName = toSandboxComponentName(component.type);
        const baseProps = isRecord(component.props) ? { ...component.props } : {};
        const dataSourceId = toStringValue(component.data_source).trim();
        const dataSourceParams = isRecord(component.data_source_params) ? component.data_source_params : {};
        const path = `/root/children/${index}`;

        let finalProps = baseProps;

        if (dataSourceId) {
            const definition = dataSourceDefinitions.get(dataSourceId) ?? inferLegacyDataSourceDefinition(dataSourceId, dataSourceParams);
            const mergedSourceArgs = {
                ...(isRecord(definition.default_args) ? definition.default_args : {}),
                ...dataSourceParams,
            };
            const signature = buildSourceSignature(dataSourceId, mergedSourceArgs);
            let source = sourceMap.get(signature);
            if (!source) {
                source = {
                    key: `source_${sourceMap.size + 1}`,
                    dataSourceId,
                    params: mergedSourceArgs,
                    definition,
                };
                sourceMap.set(signature, source);
                data[source.key] = deriveSourceData(componentName, baseProps, mergedSourceArgs) ?? {};
                metaSources[source.key] = toUiDataSourceState(definition, mergedSourceArgs);
            } else {
                const candidateData = deriveSourceData(componentName, baseProps, mergedSourceArgs);
                if (isRecord(candidateData)) {
                    data[source.key] = {
                        ...(isRecord(data[source.key]) ? (data[source.key] as Record<string, unknown>) : {}),
                        ...candidateData,
                    };
                }
            }

            const sourceAware = createSourceAwareProps(componentName, baseProps, source);
            finalProps = sourceAware.props;
            const inferredActionId = inferInteractiveActionId(componentName, source);
            if (inferredActionId && typeof finalProps.actionId !== "string") {
                finalProps.actionId = inferredActionId;
            }
            bindings.push(
                ...sourceAware.bindings.map((binding) => ({
                    ...binding,
                    componentPath: path,
                }))
            );

            if (inferredActionId && !actionRegistry[inferredActionId]) {
                actionRegistry[inferredActionId] = {
                    id: inferredActionId,
                    actionType: "ui.action.execute",
                    handler: source.definition.handler || source.definition.id,
                    dataSourceId: source.definition.id,
                    defaultArgs: {
                        ...(isRecord(source.definition.default_args) ? source.definition.default_args : {}),
                        ...source.params,
                    },
                    refreshDataSourceIds: [],
                };
            }
        }

        if (componentName === "markdown" && typeof finalProps.content !== "string" && typeof responseText === "string") {
            finalProps.content = responseText;
        }

        return {
            type: "component",
            name: componentName,
            props: finalProps,
            children: [],
        };
    });

    return {
        schema: "ui.v1",
        uiInstanceId:
            typeof crypto !== "undefined" && typeof crypto.randomUUID === "function"
                ? crypto.randomUUID()
                : `ui_${Date.now()}`,
        root: {
            type: "layout",
            name: "column",
            props: { gap: 16 },
            children,
        },
        data,
        bindings,
        subscriptions: definedSubscriptions,
        meta: {
            sourceRegistry: metaSources,
            dataSources: metaSources,
            actionRegistry,
            renderTextBubble: !(
                declaredComponents.length === 0
                && shouldPromoteTextToMarkdownUi(responseText)
            ),
        },
    };
}

export function joinUrl(baseUrl: string, path: string): string {
    const normalizedBase = baseUrl.replace(/\/+$/, "");
    const normalizedPath = path.startsWith("/") ? path : `/${path}`;

    if (normalizedBase.endsWith("/api") && normalizedPath.startsWith("/api/")) {
        return `${normalizedBase}${normalizedPath.slice(4)}`;
    }

    return `${normalizedBase}${normalizedPath}`;
}

async function postJson<T>(path: string, body: Record<string, unknown>): Promise<T> {
    const response = await fetch(joinUrl(CONFIG.apiServerUrl, path), {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(body),
    });

    if (!response.ok) {
        throw new Error(`Request failed with status ${response.status}.`);
    }

    return response.json() as Promise<T>;
}

function serializeDataSourceState(dataSource: UiDataSourceState): Record<string, unknown> {
    return {
        id: dataSource.id,
        handler: dataSource.handler || dataSource.tool,
        defaultArgs: dataSource.defaultArgs ?? {},
        refreshOnLoad: dataSource.refreshOnLoad === true,
        refreshOnConversationOpen: dataSource.refreshOnConversationOpen === true,
        staleAfterMs: dataSource.staleAfterMs ?? null,
        fieldHints: dataSource.fieldHints
            ? {
                entityType: dataSource.fieldHints.entityType ?? null,
                collectionPath: dataSource.fieldHints.collectionPath ?? null,
                labelField: dataSource.fieldHints.labelField ?? null,
                valueField: dataSource.fieldHints.valueField ?? null,
                titleField: dataSource.fieldHints.titleField ?? null,
                imageField: dataSource.fieldHints.imageField ?? null,
                categoryField: dataSource.fieldHints.categoryField ?? null,
                searchField: dataSource.fieldHints.searchField ?? null,
                defaultTableFields: dataSource.fieldHints.defaultTableFields ?? [],
                textFields: dataSource.fieldHints.textFields ?? [],
                numericFields: dataSource.fieldHints.numericFields ?? [],
                fields: dataSource.fieldHints.fields ?? [],
            }
            : null,
        transforms: Array.isArray(dataSource.transforms)
            ? dataSource.transforms.map((transform) => ({
                id: transform.id ?? null,
                type: transform.type,
                inputPath: transform.inputPath ?? null,
                outputPath: transform.outputPath ?? null,
                fields: Array.isArray(transform.fields)
                    ? transform.fields.map((field) => ({
                        key: field.key,
                        label: field.label ?? null,
                        sourceField: field.sourceField ?? null,
                        value: field.value,
                        expression: field.expression ?? null,
                        kind: field.kind ?? null,
                    }))
                    : [],
                fieldHints: transform.fieldHints
                    ? {
                        entityType: transform.fieldHints.entityType ?? null,
                        collectionPath: transform.fieldHints.collectionPath ?? null,
                        labelField: transform.fieldHints.labelField ?? null,
                        valueField: transform.fieldHints.valueField ?? null,
                        titleField: transform.fieldHints.titleField ?? null,
                        imageField: transform.fieldHints.imageField ?? null,
                        categoryField: transform.fieldHints.categoryField ?? null,
                        searchField: transform.fieldHints.searchField ?? null,
                        defaultTableFields: transform.fieldHints.defaultTableFields ?? [],
                        textFields: transform.fieldHints.textFields ?? [],
                        numericFields: transform.fieldHints.numericFields ?? [],
                        fields: transform.fieldHints.fields ?? [],
                    }
                    : null,
            }))
            : [],
    };
}

function serializeActionState(action: UiActionState): Record<string, unknown> {
    return {
        id: action.id,
        actionType: action.actionType,
        handler: action.handler ?? "",
        dataSourceId: action.dataSourceId ?? null,
        defaultArgs: action.defaultArgs ?? {},
        refreshDataSourceIds: action.refreshDataSourceIds ?? [],
    };
}

export async function querySandboxDataSource(
    dataSource: UiDataSourceState,
    params: Record<string, unknown>
): Promise<GatewayQueryResponse> {
    const mergedArgs = {
        ...(dataSource.defaultArgs ?? {}),
        ...params,
    };

    return postJson<GatewayQueryResponse>("/api/actions/query", {
        dataSource: serializeDataSourceState({
            ...dataSource,
            lastArgs: mergedArgs,
        }),
        args: mergedArgs,
    });
}

export async function executeSandboxAction(input: {
    action: UiActionState;
    dataSource?: UiDataSourceState | null;
    dataSources?: Record<string, UiDataSourceState>;
    payload: Record<string, unknown>;
}): Promise<GatewayActionExecutionResponse> {
    const serializedDataSources = Object.entries(input.dataSources ?? {}).reduce<Record<string, unknown>>(
        (accumulator, [key, value]) => {
            accumulator[key] = serializeDataSourceState(value);
            return accumulator;
        },
        {}
    );

    return postJson<GatewayActionExecutionResponse>("/api/actions/execute", {
        action: serializeActionState(input.action),
        dataSource: input.dataSource ? serializeDataSourceState(input.dataSource) : null,
        dataSources: serializedDataSources,
        payload: {
            ...(input.action.defaultArgs ?? {}),
            ...input.payload,
        },
    });
}
