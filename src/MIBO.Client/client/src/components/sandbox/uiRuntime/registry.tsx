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

// fallback sigur
export function Unknown(props: UiComponentProps) {
    return (
        <div style={{ border: "1px solid #555", borderRadius: 12, padding: 12 }}>
            <div style={{ fontWeight: 700 }}>Unknown component</div>
            <pre style={{ whiteSpace: "pre-wrap" }}>{JSON.stringify(props, null, 2)}</pre>
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

    __unknown__: Unknown,
};