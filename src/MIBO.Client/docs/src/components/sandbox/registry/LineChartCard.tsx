import { useState, useRef, useCallback } from "react";
import type { UiComponentProps } from "@/components/sandbox/registry/types";
import { extractArray, resolveFromDataKey } from "@/components/sandbox/registry/dataResolver";

// ── types ─────────────────────────────────────────────────────────────────────
type DataPoint = { label: string; value: number };
type Series = { name: string; data: DataPoint[]; color?: string };

// ── palette – same as PieChartCard ────────────────────────────────────────────
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

    // Single series: array of { label, value } objects
    if (Array.isArray(raw) && raw.length > 0 && "value" in (raw[0] ?? {})) {
        return [
            {
                name: "Value",
                color: palette[0],
                data: raw.map((x: any) => ({
                    label: String(x?.label ?? x?.name ?? ""),
                    value: Number(x?.value ?? 0),
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
                ? s.data.map((x: any) => ({
                    label: String(x?.label ?? x?.name ?? ""),
                    value: Number(x?.value ?? 0),
                }))
                : [],
        }));
    }

    return [];
}

function buildSvgPath(points: { x: number; y: number }[], smooth: boolean): string {
    if (points.length === 0) return "";
    if (points.length === 1) return `M ${points[0].x} ${points[0].y}`;

    if (!smooth) {
        return points.map((p, i) => `${i === 0 ? "M" : "L"} ${p.x} ${p.y}`).join(" ");
    }

    // Catmull-Rom → cubic bezier
    let d = `M ${points[0].x} ${points[0].y}`;
    for (let i = 0; i < points.length - 1; i++) {
        const p0 = points[Math.max(i - 1, 0)];
        const p1 = points[i];
        const p2 = points[i + 1];
        const p3 = points[Math.min(i + 2, points.length - 1)];
        const cp1x = p1.x + (p2.x - p0.x) / 6;
        const cp1y = p1.y + (p2.y - p0.y) / 6;
        const cp2x = p2.x - (p3.x - p1.x) / 6;
        const cp2y = p2.y - (p3.y - p1.y) / 6;
        d += ` C ${cp1x} ${cp1y}, ${cp2x} ${cp2y}, ${p2.x} ${p2.y}`;
    }
    return d;
}

function buildAreaPath(
    points: { x: number; y: number }[],
    smooth: boolean,
    baseY: number
): string {
    if (points.length === 0) return "";
    const linePath = buildSvgPath(points, smooth);
    return (
        linePath +
        ` L ${points[points.length - 1].x} ${baseY} L ${points[0].x} ${baseY} Z`
    );
}

// ── component ─────────────────────────────────────────────────────────────────
export function LineChartCard({ props, data }: UiComponentProps) {
    const title = String(props?.title ?? "Line Chart");
    const dataKey = String(props?.dataKey ?? "");
    const smooth: boolean = props?.smooth !== false;
    const showArea: boolean = props?.showArea !== false;
    const showDots: boolean = props?.showDots !== false;
    const showGrid: boolean = props?.showGrid !== false;
    const showLegend: boolean = props?.showLegend !== false;
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

    const series = normalizeSeries(raw, PALETTE).map((s, i) => ({
        ...s,
        color: s.color ?? PALETTE[i % PALETTE.length],
    }));

    // ── chart geometry ──────────────────────────────────────────────────────────
    const W = 480;
    const H = 200;
    const PAD = { top: 16, right: 16, bottom: 32, left: 44 };
    const chartW = W - PAD.left - PAD.right;
    const chartH = H - PAD.top - PAD.bottom;

    const allValues = series.flatMap((s) => s.data.map((d) => d.value));
    const allLabels =
        series.length > 0 ? series[0].data.map((d) => d.label) : [];
    const dataLen = allLabels.length;

    const minV = allValues.length > 0 ? Math.min(...allValues) : 0;
    const maxV = allValues.length > 0 ? Math.max(...allValues) : 1;
    const vRange = maxV - minV || 1;
    // add 10% padding top/bottom
    const vMin = minV - vRange * 0.1;
    const vMax = maxV + vRange * 0.1;
    const vSpan = vMax - vMin;

    const toX = (i: number) =>
        dataLen <= 1 ? PAD.left + chartW / 2 : PAD.left + (i / (dataLen - 1)) * chartW;
    const toY = (v: number) => PAD.top + chartH - ((v - vMin) / vSpan) * chartH;
    const baseY = toY(vMin);

    // Y gridlines
    const GRID_LINES = 4;
    const gridValues = Array.from({ length: GRID_LINES + 1 }, (_, i) =>
        vMin + (vSpan / GRID_LINES) * i
    );

    // ── tooltip / crosshair ─────────────────────────────────────────────────────
    const [crosshair, setCrosshair] = useState<number | null>(null); // data index
    const [tooltipPos, setTooltipPos] = useState<{ x: number; y: number } | null>(null);
    const svgRef = useRef<SVGSVGElement>(null);

    const handleMouseMove = useCallback(
        (e: React.MouseEvent<SVGRectElement>) => {
            if (dataLen === 0) return;
            const rect = svgRef.current?.getBoundingClientRect();
            if (!rect) return;
            const mx = e.clientX - rect.left;
            const my = e.clientY - rect.top;
            // Map client px → SVG units
            const scaleX = W / rect.width;
            const svgX = mx * scaleX;
            const idx = Math.round(((svgX - PAD.left) / chartW) * (dataLen - 1));
            const clamped = Math.max(0, Math.min(dataLen - 1, idx));
            setCrosshair(clamped);
            setTooltipPos({ x: mx, y: my });
        },
        [dataLen, chartW]
    );

    const handleMouseLeave = useCallback(() => {
        setCrosshair(null);
        setTooltipPos(null);
    }, []);

    const crosshairX = crosshair !== null ? toX(crosshair) : null;

    // ── per-series paths ────────────────────────────────────────────────────────
    const seriesPaths = series.map((s) => {
        const pts = s.data.map((d, i) => ({ x: toX(i), y: toY(d.value) }));
        return {
            ...s,
            pts,
            line: buildSvgPath(pts, smooth),
            area: showArea ? buildAreaPath(pts, smooth, baseY) : "",
        };
    });

    // unique gradient ids
    const gradIds = series.map((_, i) => `lcg-${i}`);

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-5 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950 select-none">
            {/* Header */}
            <div className="flex items-center justify-between mb-4">
        <span className="text-sm font-semibold text-zinc-800 dark:text-zinc-100">
          {title}
        </span>
                {crosshair !== null && (
                    <span className="text-xs text-zinc-400 dark:text-zinc-500 tabular-nums">
            {allLabels[crosshair]}
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
                    {/* SVG chart */}
                    <div className="relative w-full" style={{ paddingBottom: `${(H / W) * 100}%` }}>
                        <svg
                            ref={svgRef}
                            viewBox={`0 0 ${W} ${H}`}
                            className="absolute inset-0 w-full h-full overflow-visible"
                            style={{ cursor: "crosshair" }}
                        >
                            <defs>
                                {seriesPaths.map((s, i) => (
                                    <linearGradient
                                        key={gradIds[i]}
                                        id={gradIds[i]}
                                        x1="0"
                                        y1="0"
                                        x2="0"
                                        y2="1"
                                    >
                                        <stop offset="0%" stopColor={s.color} stopOpacity="0.25" />
                                        <stop offset="100%" stopColor={s.color} stopOpacity="0.01" />
                                    </linearGradient>
                                ))}
                            </defs>

                            {/* Grid */}
                            {showGrid &&
                                gridValues.map((v, i) => {
                                    const y = toY(v);
                                    return (
                                        <g key={i}>
                                            <line
                                                x1={PAD.left}
                                                y1={y}
                                                x2={PAD.left + chartW}
                                                y2={y}
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
                                })}

                            {/* X labels */}
                            {allLabels.map((lbl, i) => {
                                // show only a reasonable number of labels
                                const step = Math.max(1, Math.ceil(dataLen / 8));
                                if (i % step !== 0 && i !== dataLen - 1) return null;
                                return (
                                    <text
                                        key={i}
                                        x={toX(i)}
                                        y={H - PAD.bottom + 14}
                                        textAnchor="middle"
                                        fontSize="9"
                                        className="fill-zinc-400 dark:fill-zinc-600"
                                        fill="currentColor"
                                    >
                                        {lbl}
                                    </text>
                                );
                            })}

                            {/* Area fills */}
                            {showArea &&
                                seriesPaths.map((s, i) => (
                                    <path
                                        key={`area-${i}`}
                                        d={s.area}
                                        fill={`url(#${gradIds[i]})`}
                                        style={{ transition: "opacity .2s" }}
                                    />
                                ))}

                            {/* Lines */}
                            {seriesPaths.map((s, i) => (
                                <path
                                    key={`line-${i}`}
                                    d={s.line}
                                    fill="none"
                                    stroke={s.color}
                                    strokeWidth="2"
                                    strokeLinecap="round"
                                    strokeLinejoin="round"
                                    style={{
                                        filter: `drop-shadow(0 1px 4px ${s.color}66)`,
                                        transition: "opacity .2s",
                                    }}
                                />
                            ))}

                            {/* Dots */}
                            {showDots &&
                                seriesPaths.map((s, si) =>
                                    s.pts.map((pt, i) => {
                                        const isHov = crosshair === i;
                                        return (
                                            <circle
                                                key={`dot-${si}-${i}`}
                                                cx={pt.x}
                                                cy={pt.y}
                                                r={isHov ? 5 : 3}
                                                fill={s.color}
                                                stroke="white"
                                                strokeWidth="1.5"
                                                className="dark:stroke-zinc-950"
                                                style={{
                                                    transition: "r .15s, opacity .2s",
                                                    opacity: crosshair !== null && !isHov ? 0.4 : 1,
                                                    filter: isHov
                                                        ? `drop-shadow(0 0 5px ${s.color})`
                                                        : "none",
                                                }}
                                            />
                                        );
                                    })
                                )}

                            {/* Crosshair */}
                            {crosshairX !== null && (
                                <line
                                    x1={crosshairX}
                                    y1={PAD.top}
                                    x2={crosshairX}
                                    y2={PAD.top + chartH}
                                    stroke="currentColor"
                                    strokeWidth="1"
                                    className="text-zinc-300 dark:text-zinc-600"
                                    strokeDasharray="3 3"
                                    style={{ pointerEvents: "none" }}
                                />
                            )}

                            {/* Invisible hover capture rect */}
                            <rect
                                x={PAD.left}
                                y={PAD.top}
                                width={chartW}
                                height={chartH}
                                fill="transparent"
                                onMouseMove={handleMouseMove}
                                onMouseLeave={handleMouseLeave}
                            />
                        </svg>

                        {/* Tooltip */}
                        {crosshair !== null && tooltipPos && (
                            <div
                                className="pointer-events-none absolute z-10 rounded-xl border border-zinc-200/80 bg-white/95 px-3 py-2 shadow-lg backdrop-blur-sm dark:border-zinc-700/80 dark:bg-zinc-900/95"
                                style={{
                                    left: tooltipPos.x + 14,
                                    top: tooltipPos.y - 10,
                                    transform:
                                        tooltipPos.x > 300 ? "translateX(-110%)" : undefined,
                                    minWidth: 110,
                                }}
                            >
                                <p className="mb-1.5 text-[10px] font-semibold text-zinc-500 dark:text-zinc-400">
                                    {allLabels[crosshair]}
                                </p>
                                {series.map((s, i) => (
                                    <div key={i} className="flex items-center gap-1.5">
                    <span
                        className="inline-block h-2 w-2 rounded-full"
                        style={{ background: s.color ?? PALETTE[i] }}
                    />
                                        <span className="text-[10px] text-zinc-600 dark:text-zinc-300">
                      {s.name}
                    </span>
                                        <span
                                            className="ml-auto text-[10px] font-semibold tabular-nums"
                                            style={{ color: s.color ?? PALETTE[i] }}
                                        >
                      {formatValue(s.data[crosshair]?.value ?? 0)}
                    </span>
                                    </div>
                                ))}
                            </div>
                        )}
                    </div>

                    {/* Legend (only for multi-series) */}
                    {showLegend && series.length > 1 && (
                        <div className="flex flex-wrap gap-3">
                            {series.map((s, i) => (
                                <div key={i} className="flex items-center gap-1.5">
                  <span
                      className="inline-block h-2.5 w-2.5 rounded-full"
                      style={{ background: s.color ?? PALETTE[i] }}
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
// ① Single series (simple array):
// <LineChartCard
//   props={{
//     title: "Venituri lunare",
//     data: [
//       { label: "Ian", value: 3200 },
//       { label: "Feb", value: 4100 },
//       { label: "Mar", value: 3800 },
//       { label: "Apr", value: 5200 },
//       { label: "Mai", value: 4700 },
//       { label: "Iun", value: 6100 },
//     ],
//     formatValue: (v: number) => `${v} lei`,
//     smooth: true,
//     showArea: true,
//     showDots: true,
//   }}
//   data={{}}
// />
//
// ② Multi-series:
// <LineChartCard
//   props={{
//     title: "Venituri vs Cheltuieli",
//     data: [
//       {
//         name: "Venituri",
//         color: "#10b981",
//         data: [
//           { label: "Ian", value: 3200 },
//           { label: "Feb", value: 4100 },
//           { label: "Mar", value: 3800 },
//         ],
//       },
//       {
//         name: "Cheltuieli",
//         color: "#f43f5e",
//         data: [
//           { label: "Ian", value: 2100 },
//           { label: "Feb", value: 2800 },
//           { label: "Mar", value: 3100 },
//         ],
//       },
//     ],
//     formatValue: (v: number) => `${v} lei`,
//   }}
//   data={{}}
// />
//
// ③ Via dataKey:
// <LineChartCard
//   props={{ title: "Temperaturi", dataKey: "temps" }}
//   data={{ temps: [{ label: "Lun", value: 18 }, { label: "Mar", value: 21 }] }}
// />
