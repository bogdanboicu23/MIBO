import { useState, useRef, useCallback } from "react";
import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";
import { extractArray, resolveFromDataKey } from "@/components/sandbox/registry/dataResolver";

// ── types ─────────────────────────────────────────────────────────────────────
type DataPoint = { label: string; value: number; color?: string };
type Series = { name: string; data: DataPoint[]; color?: string };

// ── palette – same as PieChartCard & LineChartCard ────────────────────────────
const PALETTE = [
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

// ── helpers ───────────────────────────────────────────────────────────────────
function normalizeSeries(raw: any, palette: string[]): Series[] {
    if (!raw) return [];

    // Single series: flat array of { label, value }
    if (
        Array.isArray(raw) &&
        raw.length > 0 &&
        ("value" in (raw[0] ?? {}) || "label" in (raw[0] ?? {}))
    ) {
        return [
            {
                name: "Value",
                color: palette[0],
                data: raw.map((x: any, i: number) => ({
                    label: String(x?.label ?? x?.name ?? `Item ${i + 1}`),
                    value: Number(x?.value ?? 0),
                    color: x?.color ?? undefined,
                })),
            },
        ];
    }

    // Multi-series: array of { name, data[], color? }
    if (Array.isArray(raw)) {
        return raw.map((s: any, i: number) => ({
            name: String(s?.name ?? `Series ${i + 1}`),
            color: s?.color ?? palette[i % palette.length],
            data: Array.isArray(s?.data)
                ? s.data.map((x: any, j: number) => ({
                    label: String(x?.label ?? x?.name ?? `Item ${j + 1}`),
                    value: Number(x?.value ?? 0),
                }))
                : [],
        }));
    }

    return [];
}

// ── component ─────────────────────────────────────────────────────────────────
export function BarChartCard({ props, data }: UiComponentProps) {
    const title = String(props?.title ?? "Bar Chart");
    const dataKey = String(props?.dataKey ?? "");
    const horizontal: boolean = props?.horizontal === true;
    const stacked: boolean = props?.stacked === true;
    const showGrid: boolean = props?.showGrid !== false;
    const showLegend: boolean = props?.showLegend !== false;
    const showValues: boolean = props?.showValues === true;
    const barRadius: number = Number(props?.barRadius ?? 6);
    const formatCfg = props?.formatValue;
    const formatValue = (v: number): string => {
        if (typeof formatCfg === "function") {
            try {
                return String((formatCfg as (n: number) => unknown)(v));
            } catch {
                return String(v);
            }
        }
        if (typeof formatCfg === "string" && formatCfg.trim()) {
            const tpl = formatCfg.trim();
            return tpl.includes("{value}") ? tpl.replaceAll("{value}", String(v)) : `${v} ${tpl}`;
        }
        return String(v);
    };

    const rawSource =
        (props?.data as any) ??
        (dataKey ? resolveFromDataKey((data ?? {}) as Record<string, any>, dataKey) : null) ??
        null;
    const raw = Array.isArray(rawSource) ? rawSource : extractArray(rawSource, ["series", "items", "data"]);

    const series: Series[] = normalizeSeries(raw, PALETTE).map((s, i) => ({
        ...s,
        color: s.color ?? PALETTE[i % PALETTE.length],
    }));

    const isSingleSeries = series.length === 1;
    const allLabels = series.length > 0 ? series[0].data.map((d) => d.label) : [];
    const dataLen = allLabels.length;

    // ── chart geometry ──────────────────────────────────────────────────────────
    const W = 480;
    const H = 220;
    const PAD = { top: 16, right: 16, bottom: 36, left: 48 };
    const chartW = W - PAD.left - PAD.right;
    const chartH = H - PAD.top - PAD.bottom;

    // value range
    let maxV: number;
    if (stacked) {
        const stackedTotals = allLabels.map((_, gi) =>
            series.reduce((sum, s) => sum + (s.data[gi]?.value ?? 0), 0)
        );
        maxV = Math.max(...stackedTotals, 1);
    } else {
        const allValues = series.flatMap((s) => s.data.map((d) => d.value));
        maxV = Math.max(...allValues, 1);
    }
    const vMax = maxV * 1.1; // 10% headroom

    const toLength = (v: number) =>
        horizontal
            ? (v / vMax) * chartW
            : (v / vMax) * chartH;

    // bar sizing
    const GROUP_GAP = 0.25; // fraction of group width used as gap
    const groupW = horizontal ? chartH / dataLen : chartW / dataLen;
    const groupGap = groupW * GROUP_GAP;
    const barsInGroup = stacked ? 1 : series.length;
    const barW = (groupW - groupGap) / barsInGroup;

    const groupOffset = (gi: number) =>
        horizontal
            ? PAD.top + gi * groupW + groupGap / 2
            : PAD.left + gi * groupW + groupGap / 2;

    const barOffset = (si: number) => (stacked ? 0 : si * barW);

    // grid lines
    const GRID_COUNT = 4;
    const gridValues = Array.from({ length: GRID_COUNT + 1 }, (_, i) =>
        (vMax / GRID_COUNT) * i
    );

    // ── hover state ─────────────────────────────────────────────────────────────
    const [hovered, setHovered] = useState<{ gi: number; si: number } | null>(null);
    const [tooltipPos, setTooltipPos] = useState<{ x: number; y: number } | null>(null);
    const svgRef = useRef<SVGSVGElement>(null);

    const handleBarEnter = useCallback(
        (e: React.MouseEvent, gi: number, si: number) => {
            setHovered({ gi, si });
            const rect = svgRef.current?.getBoundingClientRect();
            if (rect)
                setTooltipPos({ x: e.clientX - rect.left, y: e.clientY - rect.top });
        },
        []
    );
    const handleBarMove = useCallback((e: React.MouseEvent) => {
        const rect = svgRef.current?.getBoundingClientRect();
        if (rect)
            setTooltipPos({ x: e.clientX - rect.left, y: e.clientY - rect.top });
    }, []);
    const handleBarLeave = useCallback(() => {
        setHovered(null);
        setTooltipPos(null);
    }, []);

    // ── rounded rect path ────────────────────────────────────────────────────────
    function roundedBar(
        x: number,
        y: number,
        w: number,
        h: number,
        r: number,
        top = true
    ): string {
        const rr = Math.min(r, w / 2, Math.abs(h) / 2);
        if (!top || h <= 0)
            return `M${x},${y} h${w} v${h} h${-w} Z`;
        return [
            `M${x + rr},${y}`,
            `h${w - 2 * rr}`,
            `q${rr},0 ${rr},${rr}`,
            `v${h - rr}`,
            `h${-w}`,
            `v${-(h - rr)}`,
            `q0,${-rr} ${rr},${-rr}`,
            "Z",
        ].join(" ");
    }

    function roundedBarH(
        x: number,
        y: number,
        w: number,
        h: number,
        r: number
    ): string {
        const rr = Math.min(r, Math.abs(h) / 2, Math.abs(w) / 2);
        return [
            `M${x},${y + rr}`,
            `q0,${-rr} ${rr},${-rr}`,
            `h${w - rr}`,
            `q${rr},0 ${rr},${rr}`,
            `v${h - 2 * rr}`,
            `q0,${rr} ${-rr},${rr}`,
            `h${-(w - rr)}`,
            `q${-rr},0 ${-rr},${-rr}`,
            "Z",
        ].join(" ");
    }

    // ── render bars ──────────────────────────────────────────────────────────────
    const bars: React.ReactNode[] = [];

    allLabels.forEach((_, gi) => {
        let stackOffset = 0;

        series.forEach((s, si) => {
            const val = s.data[gi]?.value ?? 0;
            const len = toLength(val);
            const color =
                isSingleSeries && s.data[gi]?.color
                    ? s.data[gi].color!
                    : (s.color ?? PALETTE[si % PALETTE.length]);

            const isHov =
                hovered?.gi === gi && (hovered?.si === si || stacked);
            const dimmed =
                hovered !== null && hovered.gi !== gi && !stacked;
            const opacity = dimmed ? 0.35 : 1;

            if (horizontal) {
                const bx = PAD.left + stackOffset;
                const by = groupOffset(gi) + barOffset(si);
                const bw = Math.max(len, 0);
                const bh = barW;
                const isLast = stacked && si === series.length - 1;
                bars.push(
                    <path
                        key={`bar-${gi}-${si}`}
                        d={roundedBarH(bx, by, bw, bh, stacked && !isLast ? 0 : barRadius)}
                        fill={color}
                        opacity={opacity}
                        style={{
                            transition: "opacity .15s, filter .15s",
                            cursor: "pointer",
                            filter: isHov ? `drop-shadow(0 0 6px ${color}88)` : "none",
                        }}
                        onMouseEnter={(e) => handleBarEnter(e, gi, si)}
                        onMouseMove={handleBarMove}
                        onMouseLeave={handleBarLeave}
                    />
                );
                if (stacked) stackOffset += len;
            } else {
                const bx = groupOffset(gi) + barOffset(si);
                const bh = Math.max(len, 0);
                const by = PAD.top + chartH - stackOffset - bh;
                const bw = barW;
                const isLast = stacked && si === series.length - 1;
                bars.push(
                    <path
                        key={`bar-${gi}-${si}`}
                        d={roundedBar(bx, by, bw, bh, stacked && !isLast ? 0 : barRadius)}
                        fill={color}
                        opacity={opacity}
                        style={{
                            transition: "opacity .15s, filter .15s",
                            cursor: "pointer",
                            filter: isHov ? `drop-shadow(0 2px 8px ${color}99)` : "none",
                        }}
                        onMouseEnter={(e) => handleBarEnter(e, gi, si)}
                        onMouseMove={handleBarMove}
                        onMouseLeave={handleBarLeave}
                    />
                );
                // value label on top
                if (showValues) {
                    bars.push(
                        <text
                            key={`val-${gi}-${si}`}
                            x={bx + bw / 2}
                            y={by - 4}
                            textAnchor="middle"
                            fontSize="8"
                            fill={color}
                            fontWeight="600"
                            style={{ pointerEvents: "none" }}
                        >
                            {formatValue(val)}
                        </text>
                    );
                }
                if (stacked) stackOffset += len;
            }
        });
    });

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-5 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950 select-none">
            {/* Header */}
            <div className="flex items-center justify-between mb-4">
        <span className="text-sm font-semibold text-zinc-800 dark:text-zinc-100">
          {title}
        </span>
                {hovered !== null && (
                    <span className="text-xs text-zinc-400 dark:text-zinc-500 tabular-nums">
            {allLabels[hovered.gi]}
          </span>
                )}
            </div>

            {series.length === 0 || dataLen === 0 ? (
                <div className="py-8 text-center text-sm text-zinc-400 dark:text-zinc-600">
                    No data — provide <code className="text-xs">props.data</code> or{" "}
                    <code className="text-xs">data[dataKey]</code>.
                </div>
            ) : (
                <div className="flex flex-col gap-3">
                    {/* SVG */}
                    <div
                        className="relative w-full"
                        style={{ paddingBottom: `${(H / W) * 100}%` }}
                    >
                        <svg
                            ref={svgRef}
                            viewBox={`0 0 ${W} ${H}`}
                            className="absolute inset-0 w-full h-full overflow-visible"
                        >
                            {/* Grid lines */}
                            {showGrid &&
                                gridValues.map((v, i) => {
                                    if (horizontal) {
                                        const x = PAD.left + toLength(v);
                                        return (
                                            <g key={i}>
                                                <line
                                                    x1={x} y1={PAD.top}
                                                    x2={x} y2={PAD.top + chartH}
                                                    stroke="currentColor"
                                                    strokeWidth="0.5"
                                                    className="text-zinc-200 dark:text-zinc-800"
                                                    strokeDasharray="4 4"
                                                />
                                                <text
                                                    x={x}
                                                    y={PAD.top + chartH + 14}
                                                    textAnchor="middle"
                                                    fontSize="9"
                                                    className="fill-zinc-400 dark:fill-zinc-600"
                                                    fill="currentColor"
                                                >
                                                    {formatValue(Math.round(v))}
                                                </text>
                                            </g>
                                        );
                                    } else {
                                        const y = PAD.top + chartH - toLength(v);
                                        return (
                                            <g key={i}>
                                                <line
                                                    x1={PAD.left} y1={y}
                                                    x2={PAD.left + chartW} y2={y}
                                                    stroke="currentColor"
                                                    strokeWidth="0.5"
                                                    className="text-zinc-200 dark:text-zinc-800"
                                                    strokeDasharray="4 4"
                                                />
                                                <text
                                                    x={PAD.left - 6}
                                                    y={y + 4}
                                                    textAnchor="end"
                                                    fontSize="9"
                                                    className="fill-zinc-400 dark:fill-zinc-600"
                                                    fill="currentColor"
                                                >
                                                    {formatValue(Math.round(v))}
                                                </text>
                                            </g>
                                        );
                                    }
                                })}

                            {/* Baseline */}
                            {!horizontal && (
                                <line
                                    x1={PAD.left}
                                    y1={PAD.top + chartH}
                                    x2={PAD.left + chartW}
                                    y2={PAD.top + chartH}
                                    stroke="currentColor"
                                    strokeWidth="1"
                                    className="text-zinc-200 dark:text-zinc-800"
                                />
                            )}
                            {horizontal && (
                                <line
                                    x1={PAD.left}
                                    y1={PAD.top}
                                    x2={PAD.left}
                                    y2={PAD.top + chartH}
                                    stroke="currentColor"
                                    strokeWidth="1"
                                    className="text-zinc-200 dark:text-zinc-800"
                                />
                            )}

                            {/* X / Y labels */}
                            {allLabels.map((lbl, gi) => {
                                if (horizontal) {
                                    const y = groupOffset(gi) + (groupW - groupGap) / 2;
                                    return (
                                        <text
                                            key={gi}
                                            x={PAD.left - 6}
                                            y={y + 4}
                                            textAnchor="end"
                                            fontSize="9"
                                            className="fill-zinc-500 dark:fill-zinc-400"
                                            fill="currentColor"
                                        >
                                            {lbl}
                                        </text>
                                    );
                                } else {
                                    const step = Math.max(1, Math.ceil(dataLen / 10));
                                    if (gi % step !== 0 && gi !== dataLen - 1) return null;
                                    const cx =
                                        groupOffset(gi) +
                                        ((groupW - groupGap) / 2);
                                    return (
                                        <text
                                            key={gi}
                                            x={cx}
                                            y={PAD.top + chartH + 16}
                                            textAnchor="middle"
                                            fontSize="9"
                                            className="fill-zinc-500 dark:fill-zinc-400"
                                            fill="currentColor"
                                        >
                                            {lbl}
                                        </text>
                                    );
                                }
                            })}

                            {/* Bars */}
                            {bars}
                        </svg>

                        {/* Tooltip */}
                        {hovered !== null && tooltipPos && (
                            <div
                                className="pointer-events-none absolute z-10 rounded-xl border border-zinc-200/80 bg-white/95 px-3 py-2 shadow-lg backdrop-blur-sm dark:border-zinc-700/80 dark:bg-zinc-900/95"
                                style={{
                                    left: tooltipPos.x + 14,
                                    top: tooltipPos.y - 10,
                                    transform: tooltipPos.x > 300 ? "translateX(-110%)" : undefined,
                                    minWidth: 120,
                                }}
                            >
                                <p className="mb-1.5 text-[10px] font-semibold text-zinc-500 dark:text-zinc-400">
                                    {allLabels[hovered.gi]}
                                </p>
                                {series.map((s, si) => {
                                    const val = s.data[hovered.gi]?.value ?? 0;
                                    const color = isSingleSeries && s.data[hovered.gi]?.color
                                        ? s.data[hovered.gi].color!
                                        : (s.color ?? PALETTE[si % PALETTE.length]);
                                    return (
                                        <div key={si} className="flex items-center gap-1.5">
                      <span
                          className="inline-block h-2 w-2 rounded-full"
                          style={{ background: color }}
                      />
                                            <span className="text-[10px] text-zinc-600 dark:text-zinc-300">
                        {isSingleSeries ? allLabels[hovered.gi] : s.name}
                      </span>
                                            <span
                                                className="ml-auto text-[10px] font-semibold tabular-nums"
                                                style={{ color }}
                                            >
                        {formatValue(val)}
                      </span>
                                        </div>
                                    );
                                })}
                            </div>
                        )}
                    </div>

                    {/* Legend – only for multi-series */}
                    {showLegend && series.length > 1 && (
                        <div className="flex flex-wrap gap-3">
                            {series.map((s, i) => (
                                <div key={i} className="flex items-center gap-1.5">
                  <span
                      className="inline-block h-2.5 w-2.5 rounded-sm"
                      style={{ background: s.color ?? PALETTE[i % PALETTE.length] }}
                  />
                                    <span className="text-xs text-zinc-600 dark:text-zinc-400">
                    {s.name}
                  </span>
                                </div>
                            ))}
                        </div>
                    )}
                </div>
            )}
        </div>
    );
}

// ── Usage examples ─────────────────────────────────────────────────────────────
//
// ① Single series – culori individuale per bar:
// <BarChartCard
//   props={{
//     title: "Cheltuieli pe categorii",
//     data: [
//       { label: "Mâncare",       value: 420, color: "#6366f1" },
//       { label: "Transport",     value: 180, color: "#f59e0b" },
//       { label: "Utilități",     value: 230, color: "#10b981" },
//       { label: "Divertisment",  value: 95,  color: "#f43f5e" },
//       { label: "Sănătate",      value: 310, color: "#3b82f6" },
//     ],
//     formatValue: (v: number) => `${v} lei`,
//     showValues: true,
//   }}
//   data={{}}
// />
//
// ② Multi-series grouped:
// <BarChartCard
//   props={{
//     title: "Venituri vs Cheltuieli",
//     data: [
//       { name: "Venituri",   color: "#10b981", data: [{ label: "Ian", value: 3200 }, { label: "Feb", value: 4100 }] },
//       { name: "Cheltuieli", color: "#f43f5e", data: [{ label: "Ian", value: 2100 }, { label: "Feb", value: 2800 }] },
//     ],
//     formatValue: (v: number) => `${v} lei`,
//   }}
//   data={{}}
// />
//
// ③ Stacked:
// <BarChartCard
//   props={{
//     title: "Buget stacked",
//     stacked: true,
//     data: [ /* same multi-series format */ ],
//   }}
//   data={{}}
// />
//
// ④ Horizontal:
// <BarChartCard
//   props={{
//     title: "Top produse",
//     horizontal: true,
//     data: [
//       { label: "Produs A", value: 540 },
//       { label: "Produs B", value: 320 },
//       { label: "Produs C", value: 210 },
//     ],
//   }}
//   data={{}}
// />
