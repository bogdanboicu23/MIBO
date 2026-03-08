import type { UiComponentProps } from "./UiRenderer";
import type { ComponentType } from "react";

import { ProductCarouselCard } from "@/components/sandbox/registry/ProductCarouselCard";
import { DataTableCard } from "@/components/sandbox/registry/DataTableCard";
import { PieChartCard } from "@/components/sandbox/registry/PieChartCard";
import { KpiCard } from "@/components/sandbox/registry/KpiCard";
import { PageTitle } from "@/components/sandbox/registry/PageTitle";
import { CategoryChips } from "@/components/sandbox/registry/CategoryChips";
import { SearchBar } from "@/components/sandbox/registry/SearchBar";
import { SortDropdown } from "@/components/sandbox/registry/SortDropdown";
import { ProductDetailCard } from "@/components/sandbox/registry/ProductDetailCard";
import { Pagination } from "@/components/sandbox/registry/Pagination";
import { CartSummaryCard } from "@/components/sandbox/registry/CartSummaryCard";
import { Carousel } from "@/components/sandbox/registry/Carousel";
import { LineChartCard } from "@/components/sandbox/registry/LineChartCard.tsx";
import { BarChartCard } from "@/components/sandbox/registry/BarChartCard.tsx";
import { SummaryPanel } from "@/components/sandbox/registry/SummaryPanel.tsx";
import { Markdown } from "@/components/sandbox/registry/Markdown.tsx";
import { ActionPanel } from "@/components/sandbox/registry/ActionPanel.tsx";
import { FormCard } from "@/components/sandbox/registry/FormCard.tsx";
import { TimelineCard } from "@/components/sandbox/registry/TimelineCard.tsx";
import { JsonViewerCard } from "@/components/sandbox/registry/JsonViewerCard.tsx";

// fallback sigur
export function Unknown(props: UiComponentProps) {
    const componentName = String((props?.props as any)?.__unknownComponent ?? "unknown");
    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
            <div className="text-sm font-semibold">Unsupported component</div>
            <div className="mt-1 text-xs opacity-75">
                Component <code>{componentName}</code> is not registered in frontend registry.
            </div>
        </div>
    );
}

export const registry: Record<string, ComponentType<UiComponentProps>> = {
    pieChart: PieChartCard,
    dataTable: DataTableCard,
    productCarousel: ProductCarouselCard,
    kpiCard: KpiCard,
    pageTitle: PageTitle,
    categoryChips: CategoryChips,
    searchBar: SearchBar,
    sortDropdown: SortDropdown,
    productDetail: ProductDetailCard,
    pagination: Pagination,
    cartSummary: CartSummaryCard,
    carousel: Carousel,
    lineChart: LineChartCard,
    barChart: BarChartCard,
    summaryPanel: SummaryPanel,
    markdown: Markdown,
    actionPanel: ActionPanel,
    formCard: FormCard,
    timelineCard: TimelineCard,
    jsonViewer: JsonViewerCard,

    // aliases for planner/backend naming variance
    piechart: PieChartCard,
    "pie-chart": PieChartCard,
    linechart: LineChartCard,
    "line-chart": LineChartCard,
    barchart: BarChartCard,
    "bar-chart": BarChartCard,
    datatable: DataTableCard,
    "data-table": DataTableCard,
    productdetail: ProductDetailCard,
    "product-detail": ProductDetailCard,
    productcarousel: ProductCarouselCard,
    "product-carousel": ProductCarouselCard,
    kpicard: KpiCard,
    "kpi-card": KpiCard,
    summary: SummaryPanel,
    "summary-card": SummaryPanel,
    pagetitle: PageTitle,
    "page-title": PageTitle,
    timeline: TimelineCard,
    form: FormCard,
    actions: ActionPanel,
    json: JsonViewerCard,
    "json-viewer": JsonViewerCard,

    __unknown__: Unknown,
};
