import type { UiComponentProps } from "./UiRenderer";
import { ProductCarouselCard } from "@/components/sandbox/registry/ProductCarouselCard.tsx";
import { DataTableCard } from "@/components/sandbox/registry/DataTableCard.tsx";
import { PieChartCard } from "@/components/sandbox/registry/PieChartCard.tsx";
import { KpiCard } from "@/components/sandbox/registry/KpiCard.tsx";
import type { ComponentType } from "react";

// Importă componentele tale existente:

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

    __unknown__: Unknown,
};