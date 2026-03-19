import type { ComponentType } from "react";
import {
    ActionPanel,
    BarChartCard,
    Carousel,
    CartSummaryCard,
    CategoryChips,
    DataTableCard,
    EmailComposerCard,
    FormCard,
    JsonViewerCard,
    KpiCard,
    LineChartCard,
    Markdown,
    PageTitle,
    Pagination,
    PieChartCard,
    ProductCarouselCard,
    ProductDetailCard,
    SearchBar,
    SortDropdown,
    SummaryPanel,
    TimelineCard,
} from "@/components/sandbox/registry";
import type { UiComponentProps } from "@/components/sandbox/registry/types";

import ActionPanelSource from "@/components/sandbox/registry/ActionPanel.tsx?raw";
import BarChartCardSource from "@/components/sandbox/registry/BarChartCard.tsx?raw";
import CarouselSource from "@/components/sandbox/registry/Carousel.tsx?raw";
import CartSummaryCardSource from "@/components/sandbox/registry/CartSummaryCard.tsx?raw";
import CategoryChipsSource from "@/components/sandbox/registry/CategoryChips.tsx?raw";
import DataTableCardSource from "@/components/sandbox/registry/DataTableCard.tsx?raw";
import EmailComposerCardSource from "@/components/sandbox/registry/EmailComposerCard.tsx?raw";
import FormCardSource from "@/components/sandbox/registry/FormCard.tsx?raw";
import JsonViewerCardSource from "@/components/sandbox/registry/JsonViewerCard.tsx?raw";
import KpiCardSource from "@/components/sandbox/registry/KpiCard.tsx?raw";
import LineChartCardSource from "@/components/sandbox/registry/LineChartCard.tsx?raw";
import MarkdownSource from "@/components/sandbox/registry/Markdown.tsx?raw";
import PageTitleSource from "@/components/sandbox/registry/PageTitle.tsx?raw";
import PaginationSource from "@/components/sandbox/registry/Pagination.tsx?raw";
import PieChartCardSource from "@/components/sandbox/registry/PieChartCard.tsx?raw";
import ProductCarouselCardSource from "@/components/sandbox/registry/ProductCarouselCard.tsx?raw";
import ProductDetailCardSource from "@/components/sandbox/registry/ProductDetailCard.tsx?raw";
import SearchBarSource from "@/components/sandbox/registry/SearchBar.tsx?raw";
import SortDropdownSource from "@/components/sandbox/registry/SortDropdown.tsx?raw";
import SummaryPanelSource from "@/components/sandbox/registry/SummaryPanel.tsx?raw";
import TimelineCardSource from "@/components/sandbox/registry/TimelineCard.tsx?raw";

export interface RegistryPropDoc {
    name: string;
    type: string;
    default?: string;
    description: string;
}

export interface RegistryAdjustment {
    title: string;
    description: string;
}

export interface RegistryComponentDocMeta {
    id: string;
    title: string;
    description: string;
    overview: string;
    component: ComponentType<UiComponentProps>;
    props: RegistryPropDoc[];
    adjustments: RegistryAdjustment[];
    previewCode: string;
    sandboxCode: string;
    previewProps: Record<string, any>;
    sourceCode: string;
    requiresActions?: boolean;
    requiresRegistry?: boolean;
    previewContainerClassName?: string;
}

export const registryDocs: Record<string, RegistryComponentDocMeta> = {
    "action-panel": {
        id: "action-panel",
        title: "ActionPanel",
        description: "Configurable set of CTA buttons that trigger typed UI actions with payload templates.",
        overview: "Use ActionPanel whenever backend-defined workflows need explicit user confirmation paths. It supports per-action variants, disabled states, payload templates and runtime action dispatching.",
        component: ActionPanel,
        props: [
            { name: "title", type: "string", default: '"Actions"', description: "Panel heading." },
            { name: "subtitle", type: "string", default: '""', description: "Optional helper text shown below the title." },
            { name: "actions", type: "ActionItem[]", description: "List of action buttons. Each item supports label, actionType, payloadTemplate, variant, disabled." },
            { name: "actionType", type: "string", default: '"ui.action"', description: "Fallback action type used when an action item does not define its own type." },
            { name: "payloadTemplate", type: "object", description: "Template object with $data/$item/$form/$value/$extra token support." },
            { name: "payloadDataKey", type: "string", description: "Alternative source for payload object from runtime data." },
        ],
        adjustments: [
            { title: "Button semantics", description: "Choose per-button variant (primary, secondary, ghost) to communicate action criticality." },
            { title: "Action routing", description: "Define specific actionType per button or keep a single default for the panel." },
            { title: "Payload shaping", description: "Use payloadTemplate for deterministic payload contracts required by handlers." },
            { title: "Empty-state behavior", description: "When no actions exist, the component renders a safe informational state." },
        ],
        previewCode: `<ActionPanel
  props={{
    title: "Quick Actions",
    subtitle: "Choose the next workflow step",
    actions: [
      { label: "Open product", actionType: "shop.open_product", variant: "primary", payloadTemplate: { productId: 101 } },
      { label: "Add to cart", actionType: "shop.add_to_cart", variant: "secondary", payloadTemplate: { userId: 1, products: [{ id: 101, quantity: 1 }] } },
      { label: "Archive", actionType: "ui.archive", variant: "ghost" }
    ]
  }}
  data={sandboxData}
  onAction={createActionLogger("ActionPanel")}
/>`,
        sandboxCode: `<ActionPanel
  props={{
    title: "Quick Actions",
    subtitle: "Customize labels, variants and payload templates",
    actions: [
      { label: "Primary", actionType: "ui.primary", variant: "primary", payloadTemplate: { source: "docs", mode: "primary" } },
      { label: "Secondary", actionType: "ui.secondary", variant: "secondary" },
      { label: "Ghost", actionType: "ui.ghost", variant: "ghost", disabled: false }
    ]
  }}
  data={sandboxData}
  onAction={createActionLogger("ActionPanel Sandbox")}
/>`,
        previewProps: {
            title: "Quick Actions",
            subtitle: "Choose the next workflow step",
            actions: [
                { label: "Open product", actionType: "shop.open_product", variant: "primary", payloadTemplate: { productId: 101 } },
                { label: "Add to cart", actionType: "shop.add_to_cart", variant: "secondary", payloadTemplate: { userId: 1, products: [{ id: 101, quantity: 1 }] } },
                { label: "Archive", actionType: "ui.archive", variant: "ghost" },
            ],
        },
        sourceCode: ActionPanelSource,
        requiresActions: true,
    },
    "bar-chart-card": {
        id: "bar-chart-card",
        title: "BarChartCard",
        description: "SVG-powered bar chart supporting grouped, stacked and horizontal layouts.",
        overview: "BarChartCard is designed for dense dashboard scenarios where visual legibility and interactive hover insights matter. It supports flexible value formatting, axis/grid controls and value labels.",
        component: BarChartCard,
        props: [
            { name: "title", type: "string", default: '"Bar Chart"', description: "Card title." },
            { name: "data | dataKey", type: "Series[] | DataPoint[] | string", description: "Chart input as direct array or runtime lookup path." },
            { name: "horizontal", type: "boolean", default: "false", description: "Switches axis orientation to horizontal bars." },
            { name: "stacked", type: "boolean", default: "false", description: "Renders multi-series values stacked per category." },
            { name: "showGrid", type: "boolean", default: "true", description: "Toggles background grid and value guides." },
            { name: "showLegend", type: "boolean", default: "true", description: "Displays series legend when multiple series are present." },
            { name: "showValues", type: "boolean", default: "false", description: "Shows labels with exact values on bars." },
            { name: "barRadius", type: "number", default: "6", description: "Corner radius for bar shapes." },
            { name: "height", type: "number", default: "320", description: "Target chart viewport height." },
            { name: "minHeight", type: "number", default: "260", description: "Minimum responsive container height." },
            { name: "formatValue", type: "string | (n) => string", description: "Formatting strategy for ticks, tooltips and labels." },
        ],
        adjustments: [
            { title: "Layout mode", description: "Switch between vertical and horizontal to fit available space and label lengths." },
            { title: "Series composition", description: "Toggle grouped versus stacked representation for comparison versus cumulative reading." },
            { title: "Label density", description: "Enable value labels only when the chart has enough room for readability." },
            { title: "Formatting", description: "Inject a formatter for currency, percentages or domain-specific units." },
        ],
        previewCode: `<BarChartCard
  props={{
    title: "Category Spend",
    dataKey: "charts.barByCategory",
    showValues: true,
    formatValue: "{value} $"
  }}
  data={sandboxData}
/>`,
        sandboxCode: `<BarChartCard
  props={{
    title: "Experiment with grouped / stacked / horizontal",
    dataKey: "charts.barByCategory",
    stacked: false,
    horizontal: false,
    showGrid: true,
    showLegend: true,
    showValues: true,
    barRadius: 8,
    formatValue: "{value} USD"
  }}
  data={sandboxData}
/>`,
        previewProps: {
            title: "Category Spend",
            dataKey: "charts.barByCategory",
            showValues: true,
            formatValue: "{value} $",
        },
        sourceCode: BarChartCardSource,
        previewContainerClassName: "w-full",
    },
    carousel: {
        id: "carousel",
        title: "Carousel",
        description: "Generic horizontal renderer that delegates item UI to any component from the runtime registry.",
        overview: "Carousel is the composition component for dynamic UI pipelines: fetch a list once, then render each item through a registry-driven component with configurable prop mapping.",
        component: Carousel,
        props: [
            { name: "title", type: "string", default: '"Carousel"', description: "Card heading." },
            { name: "items | data | dataKey", type: "array | object | string", description: "Input source for the carousel dataset." },
            { name: "itemsPath", type: "string", default: '"products"', description: "Path used to extract list from object payloads." },
            { name: "itemComponent", type: "string", description: "Registry key of the renderer used for each item." },
            { name: "itemProps", type: "object", default: "{}", description: "Base props passed to every rendered item." },
            { name: "itemPropMap", type: "Record<string, token>", default: "{}", description: "Map item fields to child props using $item tokens." },
            { name: "cardWidthPx", type: "number", default: "360", description: "Width for each rendered card." },
            { name: "scrollCards", type: "number", default: "2", description: "How many cards are moved by Prev/Next controls." },
        ],
        adjustments: [
            { title: "Renderer injection", description: "Swap `itemComponent` to reuse the same rail with different visuals." },
            { title: "Prop mapping", description: "Use `itemPropMap` to project nested fields into child component APIs." },
            { title: "Density control", description: "Tune `cardWidthPx` and `scrollCards` for desktop versus compact panels." },
            { title: "Data adaptation", description: "Use `itemsPath` for heterogeneous payload contracts from multiple tools." },
        ],
        previewCode: `<Carousel
  props={{
    title: "Reusable Product Rail",
    dataKey: "productsResult",
    itemsPath: "products",
    itemComponent: "productDetail",
    itemPropMap: { product: "$item" },
    cardWidthPx: 330,
    scrollCards: 1
  }}
  data={sandboxData}
  registry={sandboxRegistry}
  onAction={createActionLogger("Carousel")}
/>`,
        sandboxCode: `<Carousel
  props={{
    title: "Generic Carousel",
    dataKey: "productsResult",
    itemsPath: "products",
    itemComponent: "productDetail",
    itemPropMap: { product: "$item" },
    cardWidthPx: 320,
    scrollCards: 2
  }}
  data={sandboxData}
  registry={sandboxRegistry}
  onAction={createActionLogger("Carousel Sandbox")}
/>`,
        previewProps: {
            title: "Reusable Product Rail",
            dataKey: "productsResult",
            itemsPath: "products",
            itemComponent: "productDetail",
            itemPropMap: { product: "$item" },
            cardWidthPx: 330,
            scrollCards: 1,
        },
        sourceCode: CarouselSource,
        requiresActions: true,
        requiresRegistry: true,
        previewContainerClassName: "w-full",
    },
    "cart-summary-card": {
        id: "cart-summary-card",
        title: "CartSummaryCard",
        description: "Order summary component with item table, totals and checkout action dispatch.",
        overview: "CartSummaryCard provides a compact checkout context for shopping flows and supports both direct cart injection and data-key resolution from tool outputs.",
        component: CartSummaryCard,
        props: [
            { name: "cart | data | dataKey", type: "object | string", description: "Cart payload source." },
            { name: "actionType", type: "string", default: '"shop.checkout"', description: "Checkout event action type." },
            { name: "checkoutActionType", type: "string", description: "Legacy alias for actionType." },
            { name: "payloadTemplate", type: "object", description: "Custom checkout payload template." },
            { name: "payloadDataKey", type: "string", description: "Take checkout payload from runtime data." },
        ],
        adjustments: [
            { title: "Payload strategy", description: "Use payload templates when checkout requires a strict external contract." },
            { title: "Cart discovery", description: "The component can auto-detect cart-like objects when dataKey is omitted." },
            { title: "Table density", description: "Large carts are clipped to first 20 rows to preserve readability." },
            { title: "Action hooks", description: "Wire checkout to orchestration workflows through onAction." },
        ],
        previewCode: `<CartSummaryCard
  props={{ dataKey: "cart", actionType: "shop.checkout" }}
  data={sandboxData}
  onAction={createActionLogger("CartSummaryCard")}
/>`,
        sandboxCode: `<CartSummaryCard
  props={{
    dataKey: "cart",
    actionType: "shop.checkout",
    payloadTemplate: { cartId: "$item.id", source: "docs-cart" }
  }}
  data={sandboxData}
  onAction={createActionLogger("CartSummaryCard Sandbox")}
/>`,
        previewProps: { dataKey: "cart", actionType: "shop.checkout" },
        sourceCode: CartSummaryCardSource,
        requiresActions: true,
        previewContainerClassName: "w-full",
    },
    "category-chips": {
        id: "category-chips",
        title: "CategoryChips",
        description: "Interactive category selector that emits actions for filtering experiences.",
        overview: "CategoryChips is optimized for fast filtering interactions. Categories can come from props, data key resolution or inferred runtime data lists.",
        component: CategoryChips,
        props: [
            { name: "items | categories | data", type: "array", description: "Direct category source." },
            { name: "dataKey", type: "string", description: "Path to categories source in runtime data." },
            { name: "selectedKey", type: "string", description: "Marks an active chip by exact category value." },
            { name: "actionType", type: "string", default: '"shop.select_category"', description: "Action emitted when chip is clicked." },
            { name: "payloadTemplate", type: "object", description: "Custom payload template for selected category." },
            { name: "payloadDataKey", type: "string", description: "Payload source path from runtime data." },
        ],
        adjustments: [
            { title: "Source precedence", description: "Direct props override inferred data, enabling deterministic behavior." },
            { title: "Selection state", description: "Set selectedKey for initial active style in controlled contexts." },
            { title: "Action payload", description: "Default payload is `{ category }`, but templates can enrich it." },
            { title: "Display cap", description: "Renders up to 40 chips to keep interaction performant." },
        ],
        previewCode: `<CategoryChips
  props={{ dataKey: "categories", selectedKey: "Electronics" }}
  data={sandboxData}
  onAction={createActionLogger("CategoryChips")}
/>`,
        sandboxCode: `<CategoryChips
  props={{
    dataKey: "categories",
    selectedKey: "Electronics",
    actionType: "shop.select_category",
    payloadTemplate: { category: "$value", source: "docs" }
  }}
  data={sandboxData}
  onAction={createActionLogger("CategoryChips Sandbox")}
/>`,
        previewProps: { dataKey: "categories", selectedKey: "Electronics" },
        sourceCode: CategoryChipsSource,
        requiresActions: true,
    },
    "data-table-card": {
        id: "data-table-card",
        title: "DataTableCard",
        description: "Full-featured table with search, sorting, pagination and row/header actions.",
        overview: "DataTableCard is the most configurable data-grid style component from the registry. It balances readability, client-side operations and action dispatch integration.",
        component: DataTableCard,
        props: [
            { name: "title | subtitle", type: "string", description: "Header texts." },
            { name: "columns", type: "Column[]", description: "Column schema including type, alignment and optional width." },
            { name: "rows", type: "Record<string, any>[]", description: "Direct row array." },
            { name: "dataKey / rowsKey / columnsKey", type: "string", description: "Path-based extraction options from runtime data." },
            { name: "rowKey", type: "string", default: '"id"', description: "Unique row identifier key." },
            { name: "pageSize", type: "number", default: "10", description: "Rows shown per page." },
            { name: "search", type: "{ placeholder?: string; keys?: string[] }", description: "Search bar configuration." },
            { name: "sort", type: "{ key: string; dir: 'asc' | 'desc' }", description: "Initial sort strategy." },
            { name: "density", type: '"compact" | "normal" | "comfortable"', default: '"normal"', description: "Cell spacing profile." },
            { name: "zebra", type: "boolean", default: "true", description: "Alternating row background." },
            { name: "stickyHeader", type: "boolean", default: "true", description: "Keeps header visible while scrolling." },
            { name: "actions", type: "TableAction[]", description: "Header-level actions with optional payload templates." },
            { name: "rowAction", type: "RowAction", description: "Action trigger for clicking a table row." },
        ],
        adjustments: [
            { title: "Data contracts", description: "Supports both explicit rows/columns and inference from runtime payloads." },
            { title: "Interaction model", description: "Enable header and row actions to integrate with orchestration flows." },
            { title: "Readability tuning", description: "Adjust density, zebra and sticky headers per context." },
            { title: "Client-side controls", description: "Search, sort and pagination are immediate with no server roundtrip." },
        ],
        previewCode: `<DataTableCard
  props={{
    title: "Recent Transactions",
    subtitle: "Interactive table with actions and sorting",
    columns: [
      { key: "id", header: "ID" },
      { key: "customer", header: "Customer" },
      { key: "amount", header: "Amount", type: "money", align: "right" },
      { key: "status", header: "Status", type: "badge" }
    ],
    rows: sandboxData.table.rows,
    search: { placeholder: "Search by customer or id", keys: ["id", "customer", "status"] },
    pageSize: 3,
    sort: { key: "amount", dir: "desc" },
    rowAction: { actionId: "table.open_row" }
  }}
  data={sandboxData}
  onAction={createActionLogger("DataTableCard")}
/>`,
        sandboxCode: `<DataTableCard
  props={{
    title: "Customize columns and actions",
    columns: [
      { key: "id", header: "ID" },
      { key: "customer", header: "Customer" },
      { key: "amount", header: "Amount", type: "number", align: "right" },
      { key: "status", header: "Status", type: "badge" }
    ],
    rows: sandboxData.table.rows,
    pageSize: 2,
    density: "normal",
    zebra: true,
    stickyHeader: true
  }}
  data={sandboxData}
  onAction={createActionLogger("DataTableCard Sandbox")}
/>`,
        previewProps: {
            title: "Recent Transactions",
            subtitle: "Interactive table with actions and sorting",
            columns: [
                { key: "id", header: "ID" },
                { key: "customer", header: "Customer" },
                { key: "amount", header: "Amount", type: "money", align: "right" },
                { key: "status", header: "Status", type: "badge" },
            ],
            rows: [
                { id: "T-001", customer: "Alice Chen", amount: 249, status: "paid" },
                { id: "T-002", customer: "Marco Lee", amount: 179, status: "pending" },
                { id: "T-003", customer: "Sarah Kim", amount: 199, status: "paid" },
                { id: "T-004", customer: "John Baker", amount: 358, status: "failed" },
            ],
            search: { placeholder: "Search by customer or id", keys: ["id", "customer", "status"] },
            pageSize: 3,
            sort: { key: "amount", dir: "desc" },
            rowAction: { actionId: "table.open_row" },
        },
        sourceCode: DataTableCardSource,
        requiresActions: true,
        previewContainerClassName: "w-full",
    },
    "email-composer": {
        id: "email-composer",
        title: "EmailComposer",
        description: "Editable Gmail draft composer that opens a populated compose window from runtime props or bound data.",
        overview: "EmailComposer is the handoff component for outreach, support follow-ups and approval flows where the AI prepares the draft but the user should review and send it through Gmail with one click.",
        component: EmailComposerCard,
        props: [
            { name: "title", type: "string", default: '"Email composer"', description: "Card heading." },
            { name: "subtitle", type: "string", default: '"Review the draft and open it directly in Gmail."', description: "Helper copy below the title." },
            { name: "initialValues", type: "object", description: "Direct draft payload with to, cc, bcc, subject and body." },
            { name: "draftDataKey | dataKey", type: "string", description: "Runtime path to a draft object resolved from UI data." },
            { name: "showCc | showBcc", type: "boolean", default: "false", description: "Forces Cc/Bcc fields visible even when empty." },
            { name: "sendLabel", type: "string", default: '"Open Gmail"', description: "Primary CTA label." },
            { name: "resetLabel", type: "string", default: '"Reset"', description: "Secondary CTA label." },
            { name: "helperText", type: "string", default: '"Separate multiple recipients with commas."', description: "Inline note shown below the editable fields." },
            { name: "openInNewTab", type: "boolean", default: "true", description: "Controls whether Gmail opens in a new tab." },
        ],
        adjustments: [
            { title: "AI-prefilled drafts", description: "Use initialValues or draftDataKey so the agent can generate a ready-to-edit outreach email." },
            { title: "Recipient control", description: "Enable showCc/showBcc when the workflow needs explicit stakeholder routing." },
            { title: "Gmail handoff", description: "The component converts live field values into Gmail compose query params on send." },
            { title: "Review-first UX", description: "Users can modify recipients, subject and body before leaving the app for Gmail." },
        ],
        previewCode: `<EmailComposerCard
  props={{
    title: "Partner follow-up",
    subtitle: "Draft generated by the AI and editable before sending",
    draftDataKey: "emailDrafts.partnerIntro",
    showCc: true,
    sendLabel: "Open in Gmail"
  }}
  data={sandboxData}
/>`,
        sandboxCode: `<EmailComposerCard
  props={{
    title: "Support reply",
    initialValues: {
      to: "customer@example.com",
      subject: "Your onboarding questions",
      body: "Hello,\\n\\nHere is the follow-up prepared by the AI assistant.\\n\\nBest regards,\\nMIBO Team"
    },
    showCc: false,
    showBcc: false,
    helperText: "Edit the draft, then open Gmail with all fields preserved."
  }}
  data={sandboxData}
/>`,
        previewProps: {
            title: "Partner follow-up",
            subtitle: "Draft generated by the AI and editable before sending",
            draftDataKey: "emailDrafts.partnerIntro",
            showCc: true,
            sendLabel: "Open in Gmail",
        },
        sourceCode: EmailComposerCardSource,
        previewContainerClassName: "w-full max-w-4xl",
    },
    "form-card": {
        id: "form-card",
        title: "FormCard",
        description: "Schema-driven form renderer with field types, prefill sources and submit/reset actions.",
        overview: "FormCard enables planner-driven form UIs where field definitions arrive at runtime. It handles initialization, required validation and action payload generation.",
        component: FormCard,
        props: [
            { name: "title | subtitle", type: "string", description: "Form heading content." },
            { name: "fields", type: "Field[]", description: "Form schema. Supports text, number, textarea, select, checkbox and date." },
            { name: "initialValues", type: "Record<string, any>", description: "Direct initial values." },
            { name: "initialDataKey | dataKey", type: "string", description: "Prefill values from runtime data path." },
            { name: "actionType", type: "string", default: '"ui.form.submit"', description: "Submit action type." },
            { name: "payloadTemplate", type: "object", description: "Custom submit payload template with token support." },
            { name: "payloadDataKey", type: "string", description: "Submit payload source path from runtime data." },
        ],
        adjustments: [
            { title: "Schema-first rendering", description: "Field metadata controls layout and controls without custom JSX." },
            { title: "Prefill strategy", description: "Combine initial values from props and runtime data for edit forms." },
            { title: "Validation baseline", description: "Required fields block submit until non-empty values are present." },
            { title: "Workflow wiring", description: "Submit emits structured action payloads compatible with orchestration." },
        ],
        previewCode: `<FormCard
  props={{
    title: "Customer Profile",
    subtitle: "Edit profile and notification preferences",
    fields: [
      { key: "fullName", label: "Full Name", required: true },
      { key: "email", label: "Email", type: "text", required: true },
      { key: "segment", label: "Segment", type: "select", options: [{ label: "Basic", value: "basic" }, { label: "Pro", value: "pro" }] },
      { key: "newsletter", label: "Newsletter", type: "checkbox" }
    ],
    initialDataKey: "forms.profile"
  }}
  data={sandboxData}
  onAction={createActionLogger("FormCard")}
/>`,
        sandboxCode: `<FormCard
  props={{
    title: "Configurable Form",
    fields: [
      { key: "name", label: "Name", required: true },
      { key: "budget", label: "Budget", type: "number" },
      { key: "notes", label: "Notes", type: "textarea" },
      { key: "startDate", label: "Start date", type: "date" },
      { key: "agreed", label: "Accepted terms", type: "checkbox" }
    ],
    actionType: "ui.form.submit",
    payloadTemplate: { values: "$form", source: "docs-form" }
  }}
  data={sandboxData}
  onAction={createActionLogger("FormCard Sandbox")}
/>`,
        previewProps: {
            title: "Customer Profile",
            subtitle: "Edit profile and notification preferences",
            fields: [
                { key: "fullName", label: "Full Name", required: true },
                { key: "email", label: "Email", type: "text", required: true },
                { key: "segment", label: "Segment", type: "select", options: [{ label: "Basic", value: "basic" }, { label: "Pro", value: "pro" }] },
                { key: "newsletter", label: "Newsletter", type: "checkbox" },
            ],
            initialDataKey: "forms.profile",
        },
        sourceCode: FormCardSource,
        requiresActions: true,
        previewContainerClassName: "w-full",
    },
    "json-viewer-card": {
        id: "json-viewer-card",
        title: "JsonViewerCard",
        description: "Structured JSON inspector with collapsible content and flexible value source.",
        overview: "JsonViewerCard is ideal for debug surfaces, action payload introspection and runtime context visibility in sandbox tools.",
        component: JsonViewerCard,
        props: [
            { name: "title", type: "string", default: '"Data"', description: "Card heading." },
            { name: "value", type: "unknown", description: "Direct value to serialize." },
            { name: "dataKey", type: "string", description: "Path to value in runtime data." },
            { name: "collapsed", type: "boolean", default: "false", description: "Initial collapsed state for details block." },
        ],
        adjustments: [
            { title: "Source priority", description: "Component prefers explicit `value`, then `dataKey`, then entire runtime data." },
            { title: "Detail toggling", description: "Use collapsed=true in dense dashboards where details are secondary." },
            { title: "Serialization fallback", description: "Automatically falls back to String(...) when JSON serialization fails." },
            { title: "Debug traceability", description: "Ideal for exposing intermediate planner/tool outputs." },
        ],
        previewCode: `<JsonViewerCard
  props={{ title: "Current Runtime Context", dataKey: "productsResult", collapsed: false }}
  data={sandboxData}
/>`,
        sandboxCode: `<JsonViewerCard
  props={{
    title: "Inspect any object",
    dataKey: "summary",
    collapsed: false
  }}
  data={sandboxData}
/>`,
        previewProps: { title: "Current Runtime Context", dataKey: "productsResult", collapsed: false },
        sourceCode: JsonViewerCardSource,
    },
    "kpi-card": {
        id: "kpi-card",
        title: "KpiCard",
        description: "Minimal KPI tile for single high-signal metric presentation.",
        overview: "KpiCard is designed for at-a-glance metric surfacing. It resolves values from runtime data keys and supports fallback chaining.",
        component: KpiCard,
        props: [
            { name: "label", type: "string", default: '"KPI"', description: "Metric label." },
            { name: "value", type: "unknown", description: "Direct metric value fallback." },
            { name: "valueKey", type: "string", description: "Metric path in runtime data or in object resolved by dataKey." },
            { name: "dataKey", type: "string", description: "Optional object source for nested value resolution." },
        ],
        adjustments: [
            { title: "Resolution chain", description: "Value is resolved from valueKey in data, then in dataKey source, then from direct value." },
            { title: "Composable sizing", description: "Works as standalone card or within metric grids and summary strips." },
            { title: "Fallback safety", description: "Renders N/A when no value can be resolved." },
            { title: "Semantic labels", description: "Use concise uppercase-like labels for fast scanning." },
        ],
        previewCode: `<KpiCard
  props={{ label: "Revenue", dataKey: "summary", valueKey: "revenue" }}
  data={sandboxData}
/>`,
        sandboxCode: `<KpiCard
  props={{
    label: "Conversion Rate",
    dataKey: "summary",
    valueKey: "conversionRate",
    value: "N/A"
  }}
  data={sandboxData}
/>`,
        previewProps: { label: "Revenue", dataKey: "summary", valueKey: "revenue" },
        sourceCode: KpiCardSource,
    },
    "line-chart-card": {
        id: "line-chart-card",
        title: "LineChartCard",
        description: "Interactive line chart with multi-series support, crosshair tooltip and optional area fill.",
        overview: "LineChartCard handles trend visualization for both single and multi-series scenarios. It includes crosshair interaction, responsive SVG layout and configurable visual layers.",
        component: LineChartCard,
        props: [
            { name: "title", type: "string", default: '"Line Chart"', description: "Card title." },
            { name: "data | dataKey", type: "Series[] | Point[] | string", description: "Source for series data." },
            { name: "smooth", type: "boolean", default: "true", description: "Enables Catmull-Rom smoothing." },
            { name: "showArea", type: "boolean", default: "true", description: "Renders gradient area below lines." },
            { name: "showDots", type: "boolean", default: "true", description: "Displays points on each data coordinate." },
            { name: "showGrid", type: "boolean", default: "true", description: "Toggles grid and value axis." },
            { name: "showLegend", type: "boolean", default: "true", description: "Shows series legend for multi-series mode." },
            { name: "formatValue", type: "string | (n) => string", description: "Axis and tooltip formatter." },
        ],
        adjustments: [
            { title: "Trend emphasis", description: "Use smooth + area for narrative charts and disable for analytical precision." },
            { title: "Interaction feedback", description: "Crosshair and tooltip expose per-index values across all series." },
            { title: "Performance", description: "Single SVG render keeps interaction fast for medium-sized datasets." },
            { title: "Readability", description: "Label density is automatically reduced for long series." },
        ],
        previewCode: `<LineChartCard
  props={{
    title: "Revenue vs Expenses",
    dataKey: "charts.lineMulti",
    showArea: true,
    smooth: true,
    formatValue: "{value} $"
  }}
  data={sandboxData}
/>`,
        sandboxCode: `<LineChartCard
  props={{
    title: "Line Chart Sandbox",
    dataKey: "charts.lineRevenue",
    smooth: true,
    showArea: true,
    showDots: true,
    showGrid: true,
    showLegend: false,
    formatValue: "{value} USD"
  }}
  data={sandboxData}
/>`,
        previewProps: {
            title: "Revenue vs Expenses",
            dataKey: "charts.lineMulti",
            showArea: true,
            smooth: true,
            formatValue: "{value} $",
        },
        sourceCode: LineChartCardSource,
        previewContainerClassName: "w-full",
    },
    markdown: {
        id: "markdown",
        title: "Markdown",
        description: "Markdown renderer with GFM support, syntax-highlighted code blocks and dark/light theme adaptation.",
        overview: "Markdown converts plain content streams into rich formatted output with automatic code highlighting and inline-code styling. It is suited for AI text responses, notes and reports.",
        component: Markdown,
        props: [
            { name: "content | markdown", type: "string", description: "Direct markdown source text." },
            { name: "dataKey", type: "string", description: "Path to markdown string or object containing markdown/content/text fields." },
        ],
        adjustments: [
            { title: "Input strategy", description: "Direct content works for static pages; dataKey is ideal for tool outputs." },
            { title: "Code rendering", description: "Block code is highlighted with Shiki, inline code keeps compact styling." },
            { title: "Theme awareness", description: "Automatically tracks dark/light mode and swaps code theme." },
            { title: "GFM support", description: "Tables, task lists and other GFM syntax are supported out of the box." },
        ],
        previewCode: `<Markdown
  props={{ dataKey: "markdown.content" }}
  data={sandboxData}
/>`,
        sandboxCode: `<Markdown
  props={{
    content: "# Markdown Sandbox\\n\\nEdit this text and test **GFM** tables or code blocks.\\n\\n\`inline code\`"
  }}
  data={sandboxData}
/>`,
        previewProps: { dataKey: "markdown.content" },
        sourceCode: MarkdownSource,
        previewContainerClassName: "w-full",
    },
    "page-title": {
        id: "page-title",
        title: "PageTitle",
        description: "Simple heading + subtitle component with runtime data binding.",
        overview: "PageTitle standardizes section headings for planner-generated screens and supports both explicit text and data-key sourced fields.",
        component: PageTitle,
        props: [
            { name: "text", type: "string", default: '"Title"', description: "Explicit title text." },
            { name: "subtitle", type: "string", default: '""', description: "Explicit subtitle text." },
            { name: "dataKey", type: "string", description: "Path to object containing title/subtitle fields." },
            { name: "textKey", type: "string", description: "Path used to resolve title from source object." },
            { name: "subtitleKey", type: "string", description: "Path used to resolve subtitle from source object." },
        ],
        adjustments: [
            { title: "Binding mode", description: "Choose explicit props for static titles or dataKey/textKey for dynamic headings." },
            { title: "Fallback chain", description: "Component attempts text, source.text, source.title and finally 'Title'." },
            { title: "Subtitle flexibility", description: "Subtitle can be omitted safely without affecting layout." },
            { title: "Section framing", description: "Works well as first element in cards and dashboard panels." },
        ],
        previewCode: `<PageTitle
  props={{ dataKey: "page", textKey: "title", subtitleKey: "subtitle" }}
  data={sandboxData}
/>`,
        sandboxCode: `<PageTitle
  props={{
    text: "Quarterly Performance",
    subtitle: "Edit title/subtitle or bind from dataKey"
  }}
  data={sandboxData}
/>`,
        previewProps: { dataKey: "page", textKey: "title", subtitleKey: "subtitle" },
        sourceCode: PageTitleSource,
    },
    pagination: {
        id: "pagination",
        title: "Pagination",
        description: "State-aware pager component that computes page info and emits navigation actions.",
        overview: "Pagination derives current page metrics from total, limit and skip values and dispatches normalized payloads for previous/next interactions.",
        component: Pagination,
        props: [
            { name: "total | limit | skip", type: "number", description: "Direct pagination values." },
            { name: "totalKey | limitKey | skipKey", type: "string", description: "Runtime data paths for pagination values." },
            { name: "actionType", type: "string", default: '"shop.change_page"', description: "Navigation action type." },
            { name: "payloadTemplate", type: "object", description: "Custom payload mapping for pagination clicks." },
            { name: "payloadDataKey", type: "string", description: "Payload source from runtime data." },
        ],
        adjustments: [
            { title: "Data source", description: "Use direct values for static demos and key-based values for runtime binding." },
            { title: "Payload schema", description: "Default payload includes skip, limit and page, but can be overridden." },
            { title: "Boundary safety", description: "Prev/Next controls auto-disable at dataset boundaries." },
            { title: "Page insight", description: "Current and total page values are computed and shown inline." },
        ],
        previewCode: `<Pagination
  props={{ totalKey: "pagination.total", limitKey: "pagination.limit", skipKey: "pagination.skip" }}
  data={sandboxData}
  onAction={createActionLogger("Pagination")}
/>`,
        sandboxCode: `<Pagination
  props={{
    total: 250,
    limit: 25,
    skip: 50,
    actionType: "shop.change_page",
    payloadTemplate: { page: "$value", skip: "$extra.skip", limit: "$extra.limit" }
  }}
  data={sandboxData}
  onAction={createActionLogger("Pagination Sandbox")}
/>`,
        previewProps: { totalKey: "pagination.total", limitKey: "pagination.limit", skipKey: "pagination.skip" },
        sourceCode: PaginationSource,
        requiresActions: true,
    },
    "pie-chart-card": {
        id: "pie-chart-card",
        title: "PieChartCard",
        description: "Interactive donut chart with animated hover state, tooltip center labels and legend.",
        overview: "PieChartCard is suitable for composition analysis and ratio-heavy reporting. It supports direct color overrides and custom value formatting strategies.",
        component: PieChartCard,
        props: [
            { name: "title", type: "string", default: '"Pie Chart"', description: "Card title." },
            { name: "data | dataKey", type: "PieDatum[] | string", description: "Pie segments source." },
            { name: "showLegend", type: "boolean", default: "true", description: "Shows list of segments with percentages." },
            { name: "formatValue", type: "string | (n) => string", description: "Custom formatter for total and segment values." },
        ],
        adjustments: [
            { title: "Visual emphasis", description: "Hovered slice expands and gets glow to improve focus." },
            { title: "Legend behavior", description: "Legend and chart hover state are synchronized both ways." },
            { title: "Formatting", description: "Use custom formatters to display currencies, units or percentages." },
            { title: "Data quality", description: "Only positive numeric values are rendered as slices." },
        ],
        previewCode: `<PieChartCard
  props={{ title: "Expense Distribution", dataKey: "charts.pieExpenses", formatValue: "{value}%" }}
  data={sandboxData}
/>`,
        sandboxCode: `<PieChartCard
  props={{
    title: "Pie Sandbox",
    dataKey: "charts.pieExpenses",
    showLegend: true,
    formatValue: "{value} units"
  }}
  data={sandboxData}
/>`,
        previewProps: { title: "Expense Distribution", dataKey: "charts.pieExpenses", formatValue: "{value}%" },
        sourceCode: PieChartCardSource,
        previewContainerClassName: "w-full",
    },
    "product-carousel-card": {
        id: "product-carousel-card",
        title: "ProductCarouselCard",
        description: "Opinionated product slider with primary card click action and optional secondary CTA.",
        overview: "ProductCarouselCard is a ready-to-use commerce visual block that includes product card rendering, snapping rail navigation and dual action channels.",
        component: ProductCarouselCard,
        props: [
            { name: "title", type: "string", default: '"Products"', description: "Section title." },
            { name: "products | items | data | dataKey", type: "array | object | string", description: "Product source input." },
            { name: "cardWidthPx", type: "number", default: "260", description: "Card width for each product item." },
            { name: "scrollCards", type: "number", default: "3", description: "Cards moved by navigation controls." },
            { name: "actionType", type: "string", default: '"shop.open_product"', description: "Primary card click action." },
            { name: "secondaryActionType", type: "string", description: "Optional CTA action rendered inside each card." },
            { name: "secondaryPayloadTemplate", type: "object", description: "Payload template for secondary action." },
            { name: "userId", type: "number", default: "1", description: "Used for default add-to-cart fallback payload." },
            { name: "payloadTemplate", type: "object", description: "Payload template for primary click action." },
        ],
        adjustments: [
            { title: "Primary vs secondary action", description: "Card body click and internal CTA can trigger independent workflows." },
            { title: "Template-aware data", description: "Safely ignores unresolved planner template strings before data resolution." },
            { title: "Density control", description: "Tune card width and scroll step for kiosk, tablet or desktop layouts." },
            { title: "Commerce fallback", description: "Built-in add-to-cart payload supports basic cart integration quickly." },
        ],
        previewCode: `<ProductCarouselCard
  props={{
    title: "Trending Products",
    dataKey: "productsResult",
    cardWidthPx: 280,
    scrollCards: 1,
    actionType: "shop.open_product",
    secondaryActionType: "shop.add_to_cart",
    userId: 7
  }}
  data={sandboxData}
  onAction={createActionLogger("ProductCarouselCard")}
/>`,
        sandboxCode: `<ProductCarouselCard
  props={{
    title: "Product Carousel Sandbox",
    dataKey: "productsResult",
    cardWidthPx: 260,
    scrollCards: 2,
    actionType: "shop.open_product",
    secondaryActionType: "shop.add_to_cart",
    userId: 1
  }}
  data={sandboxData}
  onAction={createActionLogger("ProductCarouselCard Sandbox")}
/>`,
        previewProps: {
            title: "Trending Products",
            dataKey: "productsResult",
            cardWidthPx: 280,
            scrollCards: 1,
            actionType: "shop.open_product",
            secondaryActionType: "shop.add_to_cart",
            userId: 7,
        },
        sourceCode: ProductCarouselCardSource,
        requiresActions: true,
        previewContainerClassName: "w-full",
    },
    "product-detail-card": {
        id: "product-detail-card",
        title: "ProductDetailCard",
        description: "Detailed product view with gallery, quantity controls and add-to-cart action.",
        overview: "ProductDetailCard is used for focused product context surfaces. It resolves a single product from multiple source patterns and handles quantity-aware add-to-cart actions.",
        component: ProductDetailCard,
        props: [
            { name: "product | data | dataKey", type: "object | string", description: "Single product source." },
            { name: "userId", type: "number", default: "1", description: "Used in fallback add-to-cart payload." },
            { name: "actionType", type: "string", default: '"shop.add_to_cart"', description: "Action emitted by add-to-cart button." },
            { name: "addToCartActionType", type: "string", description: "Legacy alias for actionType." },
            { name: "payloadTemplate", type: "object", description: "Custom payload template for add-to-cart action." },
            { name: "payloadDataKey", type: "string", description: "Payload source from runtime data." },
        ],
        adjustments: [
            { title: "Source fallback", description: "Can resolve product from direct prop, dataKey path, arrays or inferred runtime keys." },
            { title: "Image interaction", description: "Supports thumbnail strip and active image switching." },
            { title: "Quantity logic", description: "Quantity increment/decrement is clamped to minimum 1." },
            { title: "Payload control", description: "Emit standard cart payload or inject custom templates." },
        ],
        previewCode: `<ProductDetailCard
  props={{ product: sandboxData.productsResult.products[0], userId: 7, actionType: "shop.add_to_cart" }}
  data={sandboxData}
  onAction={createActionLogger("ProductDetailCard")}
/>`,
        sandboxCode: `<ProductDetailCard
  props={{
    product: sandboxData.productsResult.products[1],
    userId: 1,
    actionType: "shop.add_to_cart",
    payloadTemplate: { userId: "$extra.userId", products: [{ id: "$item.id", quantity: "$value" }] }
  }}
  data={sandboxData}
  onAction={createActionLogger("ProductDetailCard Sandbox")}
/>`,
        previewProps: { product: null, userId: 7, actionType: "shop.add_to_cart" },
        sourceCode: ProductDetailCardSource,
        requiresActions: true,
        previewContainerClassName: "w-full",
    },
    "search-bar": {
        id: "search-bar",
        title: "SearchBar",
        description: "Query input with enter/button submit and action payload generation.",
        overview: "SearchBar is the lightweight query trigger for tool-based search flows. It can initialize from runtime state and controls whether empty submissions are allowed.",
        component: SearchBar,
        props: [
            { name: "placeholder", type: "string", default: '"Search products..."', description: "Input placeholder text." },
            { name: "initialValue", type: "string", default: '""', description: "Direct initial query value." },
            { name: "initialValueKey", type: "string", description: "Path to initial query in runtime data." },
            { name: "submitOnEmpty", type: "boolean", default: "false", description: "Allows dispatching empty queries." },
            { name: "actionType", type: "string", default: '"shop.search"', description: "Action emitted on submit." },
            { name: "payloadTemplate", type: "object", description: "Template for custom search payload." },
            { name: "payloadDataKey", type: "string", description: "Payload source from runtime data." },
        ],
        adjustments: [
            { title: "Input state sync", description: "Re-syncs local input when initial value changes externally." },
            { title: "Submit behavior", description: "Supports Enter key and button click with optional empty submit." },
            { title: "Action payload", description: "Default payload includes `query`, but templates can reshape it." },
            { title: "Search UX", description: "Best used in combination with SortDropdown and CategoryChips." },
        ],
        previewCode: `<SearchBar
  props={{ placeholder: "Search products", initialValueKey: "filters.query", actionType: "shop.search" }}
  data={sandboxData}
  onAction={createActionLogger("SearchBar")}
/>`,
        sandboxCode: `<SearchBar
  props={{
    placeholder: "Search by keyword",
    initialValue: "headphones",
    submitOnEmpty: false,
    actionType: "shop.search",
    payloadTemplate: { query: "$value", source: "docs-search" }
  }}
  data={sandboxData}
  onAction={createActionLogger("SearchBar Sandbox")}
/>`,
        previewProps: { placeholder: "Search products", initialValueKey: "filters.query", actionType: "shop.search" },
        sourceCode: SearchBarSource,
        requiresActions: true,
    },
    "sort-dropdown": {
        id: "sort-dropdown",
        title: "SortDropdown",
        description: "Normalized sort selector with default presets and action dispatch.",
        overview: "SortDropdown centralizes sort strategy selection and can consume options directly or from runtime data sources.",
        component: SortDropdown,
        props: [
            { name: "options", type: "Array<string | {label,value}>", description: "Direct options list." },
            { name: "dataKey", type: "string", description: "Path to options source." },
            { name: "selected", type: "string", description: "Direct selected value." },
            { name: "selectedKey", type: "string", description: "Path to selected value in runtime data." },
            { name: "actionType", type: "string", default: '"shop.sort"', description: "Action emitted on option change." },
            { name: "payloadTemplate", type: "object", description: "Custom payload template for selected sort value." },
            { name: "payloadDataKey", type: "string", description: "Payload source from runtime data." },
        ],
        adjustments: [
            { title: "Preset fallback", description: "When no options are supplied, component uses sensible default sort options." },
            { title: "Selection source", description: "Supports controlled direct selection and runtime-data-based selection." },
            { title: "Action formatting", description: "Default payload format is `{ sort: 'price:asc' }`." },
            { title: "Data extraction", description: "Can consume options from `options`, `items` or `sorts` lists." },
        ],
        previewCode: `<SortDropdown
  props={{ dataKey: "filters", selectedKey: "filters.sort" }}
  data={sandboxData}
  onAction={createActionLogger("SortDropdown")}
/>`,
        sandboxCode: `<SortDropdown
  props={{
    options: [
      { label: "Top rated", value: "rating:desc" },
      { label: "Price ↑", value: "price:asc" },
      { label: "Price ↓", value: "price:desc" }
    ],
    selected: "rating:desc",
    actionType: "shop.sort"
  }}
  data={sandboxData}
  onAction={createActionLogger("SortDropdown Sandbox")}
/>`,
        previewProps: { options: [{ label: "Top rated", value: "rating:desc" }, { label: "Price ↑", value: "price:asc" }, { label: "Price ↓", value: "price:desc" }], selected: "rating:desc" },
        sourceCode: SortDropdownSource,
        requiresActions: true,
    },
    "summary-panel": {
        id: "summary-panel",
        title: "SummaryPanel",
        description: "Visual KPI collection panel with grid/list layouts, trend badges and sparklines.",
        overview: "SummaryPanel aggregates multiple KPIs into a single adaptive layout. It supports trend semantics, custom colors, sparkline trends and automatic column sizing.",
        component: SummaryPanel,
        props: [
            { name: "title", type: "string", default: '""', description: "Optional panel heading." },
            { name: "items | dataKey", type: "SummaryItem[] | string", description: "Summary entries source." },
            { name: "layout", type: '"grid" | "list"', default: '"grid"', description: "Layout profile for metrics." },
            { name: "columns", type: "number", description: "Forced grid column count (1-4)." },
            { name: "positiveIsGood", type: "boolean", default: "true", description: "Controls trend color semantics." },
        ],
        adjustments: [
            { title: "Layout strategy", description: "Use grid for balanced dashboards or list for operational status panels." },
            { title: "Trend semantics", description: "Flip positiveIsGood for contexts where decreases are favorable." },
            { title: "Visual encoding", description: "Per-item colors, icons and sparklines increase scan efficiency." },
            { title: "Adaptive columns", description: "When columns is omitted, component auto-computes an optimal grid." },
        ],
        previewCode: `<SummaryPanel
  props={{ title: "Financial Snapshot", dataKey: "summaryPanel.items", layout: "grid", positiveIsGood: true }}
  data={sandboxData}
/>`,
        sandboxCode: `<SummaryPanel
  props={{
    title: "Summary Sandbox",
    dataKey: "summaryPanel.items",
    layout: "grid",
    columns: 3,
    positiveIsGood: true
  }}
  data={sandboxData}
/>`,
        previewProps: { title: "Financial Snapshot", dataKey: "summaryPanel.items", layout: "grid", positiveIsGood: true },
        sourceCode: SummaryPanelSource,
        previewContainerClassName: "w-full",
    },
    "timeline-card": {
        id: "timeline-card",
        title: "TimelineCard",
        description: "Event timeline list with status dots and selectable items.",
        overview: "TimelineCard turns chronological event streams into actionable UI. Clicking items emits contextual action payloads suitable for detailed drill-down flows.",
        component: TimelineCard,
        props: [
            { name: "title", type: "string", default: '"Timeline"', description: "Card title." },
            { name: "items | dataKey", type: "array | string", description: "Timeline events source." },
            { name: "actionType", type: "string", default: '"ui.timeline.select"', description: "Action emitted for clicked event." },
            { name: "payloadTemplate", type: "object", description: "Template for click payload shape." },
            { name: "payloadDataKey", type: "string", description: "Payload source from runtime data." },
        ],
        adjustments: [
            { title: "Status mapping", description: "Supports success, warning, error and info status color coding." },
            { title: "Source normalization", description: "Accepts heterogeneous item schemas (title/name/event, timestamp/time)." },
            { title: "Interaction payload", description: "Default payload contains item, index and source metadata." },
            { title: "Compact chronology", description: "Designed for short operational event streams in side panels." },
        ],
        previewCode: `<TimelineCard
  props={{ title: "Activity Timeline", dataKey: "timeline.items", actionType: "timeline.select" }}
  data={sandboxData}
  onAction={createActionLogger("TimelineCard")}
/>`,
        sandboxCode: `<TimelineCard
  props={{
    title: "Timeline Sandbox",
    dataKey: "timeline.items",
    actionType: "timeline.select",
    payloadTemplate: { selected: "$item", atIndex: "$extra.index" }
  }}
  data={sandboxData}
  onAction={createActionLogger("TimelineCard Sandbox")}
/>`,
        previewProps: { title: "Activity Timeline", dataKey: "timeline.items", actionType: "timeline.select" },
        sourceCode: TimelineCardSource,
        requiresActions: true,
    },
};

// components with custom preview props that need runtime values
registryDocs["product-detail-card"].previewProps.product = {
    id: 101,
    title: "Noise Canceling Headphones",
    description: "Wireless over-ear headphones with 40h battery life.",
    price: 249,
    rating: 4.7,
    brand: "AudioMax",
    category: "Electronics",
    thumbnail: "https://images.unsplash.com/photo-1583394838336-acd977736f90?auto=format&fit=crop&w=800&q=80",
    images: [
        "https://images.unsplash.com/photo-1583394838336-acd977736f90?auto=format&fit=crop&w=800&q=80",
        "https://images.unsplash.com/photo-1546435770-a3e426bf472b?auto=format&fit=crop&w=800&q=80",
    ],
    stock: 14,
};

export const registryDocList = Object.values(registryDocs);
