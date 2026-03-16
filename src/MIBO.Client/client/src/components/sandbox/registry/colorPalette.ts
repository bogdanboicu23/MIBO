const DEFAULT_PALETTE = [
    "#6366f1",
    "#f59e0b",
    "#10b981",
    "#f43f5e",
    "#3b82f6",
    "#8b5cf6",
    "#ec4899",
    "#14b8a6",
    "#f97316",
    "#a3e635",
];

export function getComponentPalette(
    props: Record<string, unknown> | undefined,
    fallback: string[] = DEFAULT_PALETTE
): string[] {
    const fromPalette = normalizePalette(props?.palette);
    if (fromPalette.length > 0) return fromPalette;

    const fromColors = normalizePalette(props?.colors);
    if (fromColors.length > 0) return fromColors;

    return [...fallback];
}

export function getSeriesColorMap(props: Record<string, unknown> | undefined): Record<string, string> {
    return normalizeColorMap(props?.seriesColors);
}

export function getValueColorMap(props: Record<string, unknown> | undefined): Record<string, string> {
    return normalizeColorMap(props?.valueColors);
}

export function resolveMappedColor(
    colorMap: Record<string, string>,
    key: unknown
): string | undefined {
    const normalizedKey = normalizeColorKey(key);
    return normalizedKey ? colorMap[normalizedKey] : undefined;
}

export function resolvePaletteColor(palette: string[], index: number): string {
    const source = palette.length > 0 ? palette : DEFAULT_PALETTE;
    return source[((index % source.length) + source.length) % source.length];
}

export function readColorProp(value: unknown): string | undefined {
    if (typeof value !== "string") return undefined;
    const normalized = value.trim();
    return normalized || undefined;
}

export function normalizeColorKey(value: unknown): string {
    return String(value ?? "").trim().toLowerCase();
}

export { DEFAULT_PALETTE };

function normalizePalette(value: unknown): string[] {
    if (!Array.isArray(value)) return [];
    return value
        .map((entry) => (typeof entry === "string" ? entry.trim() : ""))
        .filter(Boolean);
}

function normalizeColorMap(value: unknown): Record<string, string> {
    if (!value || typeof value !== "object" || Array.isArray(value)) return {};

    const normalized: Record<string, string> = {};
    Object.entries(value as Record<string, unknown>).forEach(([key, rawColor]) => {
        const normalizedKey = normalizeColorKey(key);
        const color = readColorProp(rawColor);
        if (!normalizedKey || !color) return;
        normalized[normalizedKey] = color;
    });

    return normalized;
}
