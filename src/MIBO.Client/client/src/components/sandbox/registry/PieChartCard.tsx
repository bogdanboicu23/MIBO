import { useState, useRef } from "react";
import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";
import { extractArray, resolveFromDataKey } from "@/components/sandbox/registry/dataResolver";

type PieDatum = { name: string; value: number; color?: string };

// ── palette ───────────────────────────────────────────────────────────────────
const PALETTE = [
    "#6366f1", // indigo
    "#f59e0b", // amber
    "#10b981", // emerald
    "#f43f5e", // rose
    "#3b82f6", // blue
    "#8b5cf6", // violet
    "#ec4899", // pink
    "#14b8a6", // teal
    "#f97316", // orange
    "#a3e635", // lime
];

// ── helpers ───────────────────────────────────────────────────────────────────
function polarToCartesian(cx: number, cy: number, r: number, deg: number) {
    const rad = ((deg - 90) * Math.PI) / 180;
    return { x: cx + r * Math.cos(rad), y: cy + r * Math.sin(rad) };
}

function slicePath(
    cx: number,
    cy: number,
    r: number,
    inner: number,
    startDeg: number,
    endDeg: number
): string {
    const gap = 0.8; // degrees gap between slices
    const s = startDeg + gap / 2;
    const e = endDeg - gap / 2;
    const p1 = polarToCartesian(cx, cy, r, s);
    const p2 = polarToCartesian(cx, cy, r, e);
    const p3 = polarToCartesian(cx, cy, inner, e);
    const p4 = polarToCartesian(cx, cy, inner, s);
    const large = e - s > 180 ? 1 : 0;
    return [
        `M ${p1.x} ${p1.y}`,
        `A ${r} ${r} 0 ${large} 1 ${p2.x} ${p2.y}`,
        `L ${p3.x} ${p3.y}`,
        `A ${inner} ${inner} 0 ${large} 0 ${p4.x} ${p4.y}`,
        "Z",
    ].join(" ");
}

// ── component ─────────────────────────────────────────────────────────────────
export function PieChartCard({ props, data }: UiComponentProps) {
    const title = String(props?.title ?? "Pie Chart");
    const dataKey = String(props?.dataKey ?? "");
    const showLegend = props?.showLegend !== false;
    const formatCfg = props?.formatValue;
    const formatValue = (v: number) => {
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
    const raw = Array.isArray(rawSource) ? rawSource : extractArray(rawSource, ["items", "data", "series"]);

    const items: PieDatum[] = Array.isArray(raw)
        ? raw
            .map((x: any, i: number) => ({
                name: String(x?.name ?? x?.label ?? `Item ${i + 1}`),
                value: Number(x?.value ?? 0),
                color: x?.color ?? PALETTE[i % PALETTE.length],
            }))
            .filter((x: PieDatum) => Number.isFinite(x.value) && x.value > 0)
        : [];

    const total = items.reduce((s, x) => s + x.value, 0);

    const [hovered, setHovered] = useState<number | null>(null);
    const [tooltip, setTooltip] = useState<{ x: number; y: number; idx: number } | null>(null);
    const svgRef = useRef<SVGSVGElement>(null);

    // Build slices
    const CX = 100, CY = 100, R = 82, INNER = 48;
    let cursor = 0;
    const slices = items.map((item, i) => {
        const deg = (item.value / total) * 360;
        const start = cursor;
        const end = cursor + deg;
        cursor = end;
        const midDeg = start + deg / 2;
        const labelPt = polarToCartesian(CX, CY, (R + INNER) / 2, midDeg);
        return { ...item, start, end, midDeg, labelPt, idx: i };
    });

    const handleMouseEnter = (e: React.MouseEvent<SVGPathElement>, idx: number) => {
        setHovered(idx);
        const rect = svgRef.current?.getBoundingClientRect();
        if (rect) {
            setTooltip({ x: e.clientX - rect.left, y: e.clientY - rect.top, idx });
        }
    };
    const handleMouseMove = (e: React.MouseEvent<SVGPathElement>) => {
        const rect = svgRef.current?.getBoundingClientRect();
        if (rect && tooltip !== null) {
            setTooltip((t) => t && { ...t, x: e.clientX - rect.left, y: e.clientY - rect.top });
        }
    };
    const handleMouseLeave = () => {
        setHovered(null);
        setTooltip(null);
    };

    const hoveredItem = hovered !== null ? items[hovered] : null;
    const displayPct =
        hoveredItem ? ((hoveredItem.value / total) * 100).toFixed(1) : null;

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-5 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950 select-none">
            {/* Header */}
            <div className="flex items-center justify-between mb-4">
                <span className="text-sm font-semibold text-zinc-800 dark:text-zinc-100">{title}</span>
                <span className="text-xs text-zinc-400 dark:text-zinc-500 tabular-nums">
          total: {formatValue(total)}
        </span>
            </div>

            {items.length === 0 ? (
                <div className="py-8 text-center text-sm text-zinc-400 dark:text-zinc-600">
                    No data — provide <code className="text-xs">props.data</code> or{" "}
                    <code className="text-xs">data[dataKey]</code>.
                </div>
            ) : (
                <div className="flex flex-col items-center gap-4 sm:flex-row sm:items-start">
                    {/* SVG donut */}
                    <div className="relative flex-shrink-0">
                        <svg
                            ref={svgRef}
                            viewBox="0 0 200 200"
                            width={200}
                            height={200}
                            className="overflow-visible"
                        >
                            {slices.map((s) => {
                                const isHov = hovered === s.idx;
                                const outerR = isHov ? R + 7 : R;
                                return (
                                    <path
                                        key={s.idx}
                                        d={slicePath(CX, CY, outerR, INNER, s.start, s.end)}
                                        fill={s.color}
                                        opacity={hovered !== null && !isHov ? 0.45 : 1}
                                        style={{
                                            transition: "all 0.2s cubic-bezier(.4,0,.2,1)",
                                            cursor: "pointer",
                                            filter: isHov
                                                ? `drop-shadow(0 0 6px ${s.color}88)`
                                                : "none",
                                        }}
                                        onMouseEnter={(e) => handleMouseEnter(e, s.idx)}
                                        onMouseMove={handleMouseMove}
                                        onMouseLeave={handleMouseLeave}
                                    />
                                );
                            })}

                            {/* Center label */}
                            <text
                                x={CX}
                                y={CY - 6}
                                textAnchor="middle"
                                className="fill-zinc-800 dark:fill-zinc-100"
                                style={{ fontSize: 13, fontWeight: 700, transition: "all .2s" }}
                                fill="currentColor"
                            >
                                {hoveredItem ? formatValue(hoveredItem.value) : formatValue(total)}
                            </text>
                            <text
                                x={CX}
                                y={CY + 10}
                                textAnchor="middle"
                                style={{
                                    fontSize: 9,
                                    fill: hoveredItem ? hoveredItem.color : "#a1a1aa",
                                    transition: "all .2s",
                                    fontWeight: hoveredItem ? 600 : 400,
                                }}
                            >
                                {hoveredItem ? hoveredItem.name : "total"}
                            </text>
                            {hoveredItem && (
                                <text
                                    x={CX}
                                    y={CY + 22}
                                    textAnchor="middle"
                                    style={{ fontSize: 9, fill: "#a1a1aa" }}
                                >
                                    {displayPct}%
                                </text>
                            )}
                        </svg>
                    </div>

                    {/* Legend */}
                    {showLegend && (
                        <ul className="flex flex-col gap-1.5 w-full sm:max-w-[180px]">
                            {items.map((item, i) => {
                                const pct = ((item.value / total) * 100).toFixed(1);
                                const isHov = hovered === i;
                                return (
                                    <li
                                        key={i}
                                        className="flex items-center gap-2 rounded-lg px-2 py-1 cursor-pointer transition-colors"
                                        style={{
                                            background: isHov ? `${item.color}18` : "transparent",
                                        }}
                                        onMouseEnter={() => setHovered(i)}
                                        onMouseLeave={() => setHovered(null)}
                                    >
                    <span
                        className="inline-block h-2.5 w-2.5 rounded-full flex-shrink-0"
                        style={{ background: item.color }}
                    />
                                        <span
                                            className="flex-1 truncate text-xs text-zinc-700 dark:text-zinc-300"
                                            style={{ fontWeight: isHov ? 600 : 400 }}
                                        >
                      {item.name}
                    </span>
                                        <span className="ml-auto text-xs tabular-nums text-zinc-500 dark:text-zinc-400">
                      {pct}%
                    </span>
                                    </li>
                                );
                            })}
                        </ul>
                    )}
                </div>
            )}
        </div>
    );
}
