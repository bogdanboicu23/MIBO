import { extractArray, resolveFromDataKey } from "@/components/sandbox/registry/dataResolver";
import type { UiFieldHints } from "@/types/ui";

export type ChartPoint = { label: string; value: number; color?: string };
export type ChartSeries = { name: string; data: ChartPoint[]; color?: string };

type ChartOptions = {
    labelKey?: string;
    valueKey?: string;
    fieldHints?: UiFieldHints | null;
};

const DEFAULT_LABEL_KEYS = ["label", "name", "title", "date", "category", "slug", "id", "x"];
const DEFAULT_VALUE_KEYS = [
    "value",
    "amount",
    "price",
    "count",
    "percentage",
    "total",
    "balance",
    "income",
    "expenses",
    "savings",
    "rating",
    "stock",
    "discountPercentage",
    "spent",
    "remaining",
    "quantity",
    "y",
];

export function resolveChartSource(props: any, data: Record<string, any> | undefined): unknown {
    const dataKey = String(props?.dataKey ?? "").trim();
    const inlineData = props?.data;
    const hasInlineData = Array.isArray(inlineData)
        ? inlineData.length > 0
        : !!inlineData && (typeof inlineData !== "object" || Object.keys(inlineData).length > 0);

    if (hasInlineData) return inlineData;
    if (dataKey) {
        const resolved = resolveFromDataKey((data ?? {}) as Record<string, any>, dataKey);
        if (resolved !== undefined) return resolved;
    }

    return inlineData ?? null;
}

export function normalizeFlatChartData(raw: unknown, options: ChartOptions = {}): ChartPoint[] {
    const hintedCollection = typeof options.fieldHints?.collectionPath === "string"
        ? options.fieldHints.collectionPath.trim()
        : "";
    const hintedSource = hintedCollection && raw && typeof raw === "object"
        ? resolveFromDataKey(raw as Record<string, any>, hintedCollection)
        : undefined;
    const source = Array.isArray(raw)
        ? raw
        : Array.isArray(hintedSource)
            ? hintedSource
            : extractArray(raw, ["series", "rows", "items", "data", "results", "byCategory", "timeSeries"]);
    if (!Array.isArray(source)) return [];

    return source
        .map((entry, index) => normalizeChartPoint(entry, index, options))
        .filter((entry): entry is ChartPoint => entry !== null);
}

export function normalizeSeriesChartData(
    raw: unknown,
    _palette: string[],
    options: ChartOptions = {}
): ChartSeries[] {
    if (!raw) return [];

    if (Array.isArray(raw) && raw.some((item) => item && typeof item === "object" && Array.isArray((item as any).data))) {
        const mapped: Array<ChartSeries | null> = raw
            .map((series, index) => {
                if (!series || typeof series !== "object") return null;
                const points = normalizeFlatChartData((series as any).data, options);
                if (points.length === 0) return null;
                return {
                    name: String((series as any).name ?? `Series ${index + 1}`),
                    color: typeof (series as any).color === "string" ? String((series as any).color) : undefined,
                    data: points,
                };
            });

        return mapped.filter((series): series is ChartSeries => series !== null);
    }

    const points = normalizeFlatChartData(raw, options);
    if (points.length === 0) return [];

    return [
        {
            name: "Value",
            color: undefined,
            data: points,
        },
    ];
}

function normalizeChartPoint(entry: unknown, index: number, options: ChartOptions): ChartPoint | null {
    if (typeof entry === "number" && Number.isFinite(entry)) {
        return {
            label: `Item ${index + 1}`,
            value: entry,
        };
    }

    if (!entry || typeof entry !== "object") return null;

    const label = pickLabel(entry as Record<string, unknown>, index, options.labelKey ?? options.fieldHints?.labelField ?? undefined);
    const value = pickValue(entry as Record<string, unknown>, options.valueKey ?? options.fieldHints?.valueField ?? undefined);
    if (!label || value === null) return null;

    const color = typeof (entry as any).color === "string" ? (entry as any).color : undefined;
    return { label, value, color };
}

function pickLabel(entry: Record<string, unknown>, index: number, explicitKey?: string): string {
    const keys = dedupeKeys(explicitKey ? [explicitKey, ...DEFAULT_LABEL_KEYS] : DEFAULT_LABEL_KEYS);
    for (const key of keys) {
        const raw = entry[key];
        if (raw === undefined || raw === null || raw === "") continue;
        return String(raw);
    }

    return `Item ${index + 1}`;
}

function pickValue(entry: Record<string, unknown>, explicitKey?: string): number | null {
    const keys = dedupeKeys(explicitKey ? [explicitKey, ...DEFAULT_VALUE_KEYS] : DEFAULT_VALUE_KEYS);
    for (const key of keys) {
        const parsed = toFiniteNumber(entry[key]);
        if (parsed !== null) return parsed;
    }

    const fallback = Object.values(entry)
        .map((value) => toFiniteNumber(value))
        .find((value): value is number => value !== null);

    return fallback ?? null;
}

function dedupeKeys(keys: string[]): string[] {
    const seen = new Set<string>();
    const normalized: string[] = [];
    for (const key of keys) {
        const value = String(key ?? "").trim();
        if (!value || seen.has(value)) continue;
        seen.add(value);
        normalized.push(value);
    }
    return normalized;
}

function toFiniteNumber(value: unknown): number | null {
    const parsed = Number(value);
    return Number.isFinite(parsed) ? parsed : null;
}
