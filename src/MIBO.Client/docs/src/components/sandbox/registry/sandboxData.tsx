import type { ComponentType } from "react";
import {
    ActionPanel,
    BarChartCard,
    Carousel,
    CartSummaryCard,
    CategoryChips,
    DataTableCard,
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

function UnknownComponent({ props }: UiComponentProps) {
    return (
        <div className="rounded-xl border border-zinc-200 bg-zinc-50 p-4 text-sm dark:border-zinc-800 dark:bg-zinc-900">
            Unknown registry component: <code>{String(props?.component ?? "n/a")}</code>
        </div>
    );
}

export const sandboxRegistry: Record<string, ComponentType<UiComponentProps>> = {
    actionPanel: ActionPanel,
    barChartCard: BarChartCard,
    barChart: BarChartCard,
    carousel: Carousel,
    cartSummaryCard: CartSummaryCard,
    cartSummary: CartSummaryCard,
    categoryChips: CategoryChips,
    dataTableCard: DataTableCard,
    dataTable: DataTableCard,
    formCard: FormCard,
    form: FormCard,
    jsonViewerCard: JsonViewerCard,
    jsonViewer: JsonViewerCard,
    kpiCard: KpiCard,
    kpi: KpiCard,
    lineChartCard: LineChartCard,
    lineChart: LineChartCard,
    markdown: Markdown,
    pageTitle: PageTitle,
    pagination: Pagination,
    pieChartCard: PieChartCard,
    pieChart: PieChartCard,
    productCarouselCard: ProductCarouselCard,
    productCarousel: ProductCarouselCard,
    productDetailCard: ProductDetailCard,
    productDetail: ProductDetailCard,
    searchBar: SearchBar,
    sortDropdown: SortDropdown,
    summaryPanel: SummaryPanel,
    timelineCard: TimelineCard,
    timeline: TimelineCard,
    __unknown__: UnknownComponent,
};

export const sandboxData = {
    summary: {
        revenue: 24500,
        expenses: 18230,
        activeUsers: 1842,
        conversionRate: 3.8,
    },
    page: {
        title: "Product Dashboard",
        subtitle: "Q1 performance snapshot",
    },
    filters: {
        query: "wireless",
        selectedCategory: "Electronics",
        sort: "rating:desc",
        options: [
            { label: "Top rated", value: "rating:desc" },
            { label: "Price low-high", value: "price:asc" },
            { label: "Price high-low", value: "price:desc" },
        ],
    },
    categories: [
        { name: "Electronics" },
        { name: "Home" },
        { name: "Sports" },
        { name: "Groceries" },
        { name: "Beauty" },
    ],
    pagination: {
        total: 96,
        limit: 12,
        skip: 24,
    },
    productsResult: {
        products: [
            {
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
            },
            {
                id: 102,
                title: "Smart Fitness Watch",
                description: "Heart-rate tracking, GPS and sleep analytics.",
                price: 179,
                rating: 4.5,
                brand: "FitPulse",
                category: "Sports",
                thumbnail: "https://images.unsplash.com/photo-1523275335684-37898b6baf30?auto=format&fit=crop&w=800&q=80",
                images: [
                    "https://images.unsplash.com/photo-1523275335684-37898b6baf30?auto=format&fit=crop&w=800&q=80",
                    "https://images.unsplash.com/photo-1518443895914-6f5f5a211f0f?auto=format&fit=crop&w=800&q=80",
                ],
                stock: 29,
            },
            {
                id: 103,
                title: "Air Purifier Pro",
                description: "HEPA filtration with smart air quality sensor.",
                price: 199,
                rating: 4.6,
                brand: "CleanHome",
                category: "Home",
                thumbnail: "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?auto=format&fit=crop&w=800&q=80",
                images: [
                    "https://images.unsplash.com/photo-1582719478250-c89cae4dc85b?auto=format&fit=crop&w=800&q=80",
                ],
                stock: 8,
            },
        ],
    },
    cart: {
        id: "cart_001",
        total: 677,
        discountedTotal: 629,
        totalProducts: 2,
        totalQuantity: 3,
        products: [
            { id: 101, title: "Noise Canceling Headphones", quantity: 1, price: 249, total: 249 },
            { id: 102, title: "Smart Fitness Watch", quantity: 2, price: 179, total: 358 },
        ],
    },
    timeline: {
        items: [
            { title: "Import completed", description: "Catalog sync from ERP", timestamp: "09:12", status: "success" },
            { title: "Price update queued", description: "42 items pending review", timestamp: "10:03", status: "warning" },
            { title: "Checkout API degraded", description: "Latency above threshold", timestamp: "10:47", status: "error" },
        ],
    },
    forms: {
        profile: {
            fullName: "Alex Johnson",
            email: "alex@example.com",
            segment: "pro",
            newsletter: true,
        },
    },
    markdown: {
        content: `# Weekly Revenue Update

| Region | Revenue | Delta |
| --- | ---: | ---: |
| EU | 9,200 | +8.1% |
| US | 11,300 | +5.4% |
| APAC | 4,000 | +12.3% |

## Notes

- Campaign *Spring Boost* improved conversion.
- Returns dropped to **1.8%**.
\n\`\`\`ts
const margin = (revenue - cost) / revenue;
console.log(margin);
\`\`\``,
    },
    charts: {
        lineRevenue: [
            { label: "Jan", value: 3200 },
            { label: "Feb", value: 4100 },
            { label: "Mar", value: 3800 },
            { label: "Apr", value: 5200 },
            { label: "May", value: 4700 },
            { label: "Jun", value: 6100 },
        ],
        lineMulti: [
            {
                name: "Revenue",
                color: "#10b981",
                data: [
                    { label: "Jan", value: 3200 },
                    { label: "Feb", value: 4100 },
                    { label: "Mar", value: 3800 },
                    { label: "Apr", value: 5200 },
                ],
            },
            {
                name: "Expenses",
                color: "#f43f5e",
                data: [
                    { label: "Jan", value: 2100 },
                    { label: "Feb", value: 2800 },
                    { label: "Mar", value: 3100 },
                    { label: "Apr", value: 3300 },
                ],
            },
        ],
        barByCategory: [
            { label: "Food", value: 420, color: "#6366f1" },
            { label: "Transport", value: 180, color: "#f59e0b" },
            { label: "Utilities", value: 230, color: "#10b981" },
            { label: "Health", value: 310, color: "#3b82f6" },
        ],
        pieExpenses: [
            { name: "Housing", value: 38 },
            { name: "Food", value: 22 },
            { name: "Transport", value: 14 },
            { name: "Leisure", value: 10 },
            { name: "Savings", value: 16 },
        ],
    },
    summaryPanel: {
        items: [
            { label: "Revenue", value: "$24.5k", trend: 12.4, icon: "💰", color: "#10b981", sparkline: [3200, 3800, 4100, 4500, 5200] },
            { label: "Expenses", value: "$18.2k", trend: -3.2, icon: "💳", color: "#f43f5e", sparkline: [2400, 2300, 2100, 1900, 1800] },
            { label: "Profit", value: "$6.3k", trend: 8.1, icon: "📈", color: "#6366f1", sparkline: [800, 1100, 1300, 1400, 1600] },
        ],
    },
    table: {
        rows: [
            { id: "T-001", customer: "Alice Chen", amount: 249, status: "paid", createdAt: "2026-03-02T09:12:00Z" },
            { id: "T-002", customer: "Marco Lee", amount: 179, status: "pending", createdAt: "2026-03-02T11:41:00Z" },
            { id: "T-003", customer: "Sarah Kim", amount: 199, status: "paid", createdAt: "2026-03-03T08:10:00Z" },
            { id: "T-004", customer: "John Baker", amount: 358, status: "failed", createdAt: "2026-03-03T14:27:00Z" },
        ],
    },
};

export function createActionLogger(prefix: string = "Sandbox") {
    return (actionType: string, payload?: Record<string, unknown>) => {
        // eslint-disable-next-line no-console
        console.log(`[${prefix}]`, actionType, payload ?? {});
    };
}
