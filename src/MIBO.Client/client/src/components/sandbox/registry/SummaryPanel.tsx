import { useState } from "react";
import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";

// ── types ─────────────────────────────────────────────────────────────────────
type TrendDirection = "up" | "down" | "neutral";

type SummaryItem = {
    label: string;
    value: string | number;
    subValue?: string;
    trend?: number;       // e.g. 12.5 → +12.5%
    trendLabel?: string;  // e.g. "față de luna trecută"
    icon?: string;        // emoji or short string
    color?: string;       // accent color hex
    sparkline?: number[]; // mini chart values
};

// ── palette – same as PieChartCard / LineChartCard / BarChartCard ─────────────
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
function trendDir(trend?: number): TrendDirection {
    if (trend === undefined || trend === null) return "neutral";
    if (trend > 0) return "up";
    if (trend < 0) return "down";
    return "neutral";
}

// ── TrendBadge ────────────────────────────────────────────────────────────────
function TrendBadge({
                        trend,
                        trendLabel,
                        positiveIsGood,
                    }: {
    trend: number;
    trendLabel?: string;
    positiveIsGood: boolean;
}) {
    const dir = trendDir(trend);
    const isGood =
        dir === "neutral" ? null : positiveIsGood ? dir === "up" : dir === "down";

    const colorClass =
        isGood === null
            ? "text-zinc-500 bg-zinc-100 dark:text-zinc-400 dark:bg-zinc-800/60"
            : isGood
                ? "text-emerald-600 bg-emerald-50 dark:text-emerald-400 dark:bg-emerald-950/60"
                : "text-rose-600 bg-rose-50 dark:text-rose-400 dark:bg-rose-950/60";

    const arrow = dir === "up" ? "↑" : dir === "down" ? "↓" : "→";

    return (
        <div className="flex items-center gap-1.5 flex-wrap mt-2">
      <span
          className={`inline-flex items-center gap-0.5 rounded-full px-2 py-0.5 text-[10px] font-semibold tabular-nums ${colorClass}`}
      >
        {arrow} {Math.abs(trend).toFixed(1)}%
      </span>
            {trendLabel && (
                <span className="text-[10px] text-zinc-400 dark:text-zinc-500">
          {trendLabel}
        </span>
            )}
        </div>
    );
}

// ── Sparkline ─────────────────────────────────────────────────────────────────
function Sparkline({ values, color }: { values: number[]; color: string }) {
    if (!values || values.length < 2) return null;
    const W = 64, H = 26;
    const min = Math.min(...values);
    const max = Math.max(...values);
    const span = max - min || 1;
    const pts = values.map((v, i) => ({
        x: (i / (values.length - 1)) * W,
        y: H - 4 - ((v - min) / span) * (H - 8),
    }));
    const line = pts.map((p, i) => `${i === 0 ? "M" : "L"} ${p.x} ${p.y}`).join(" ");
    const area = line + ` L ${pts[pts.length - 1].x} ${H} L 0 ${H} Z`;
    const gradId = `sg-${color.replace("#", "")}`;

    return (
        <svg viewBox={`0 0 ${W} ${H}`} width={W} height={H} className="overflow-visible flex-shrink-0">
            <defs>
                <linearGradient id={gradId} x1="0" y1="0" x2="0" y2="1">
                    <stop offset="0%" stopColor={color} stopOpacity="0.28" />
                    <stop offset="100%" stopColor={color} stopOpacity="0.02" />
                </linearGradient>
            </defs>
            <path d={area} fill={`url(#${gradId})`} />
            <path
                d={line}
                fill="none"
                stroke={color}
                strokeWidth="1.5"
                strokeLinecap="round"
                strokeLinejoin="round"
                style={{ filter: `drop-shadow(0 1px 3px ${color}66)` }}
            />
            <circle
                cx={pts[pts.length - 1].x}
                cy={pts[pts.length - 1].y}
                r="2.5"
                fill={color}
                stroke="white"
                strokeWidth="1"
                className="dark:stroke-zinc-950"
            />
        </svg>
    );
}

// ── MetricCard ────────────────────────────────────────────────────────────────
function MetricCard({
                        item,
                        color,
                        layout,
                        positiveIsGood,
                    }: {
    item: SummaryItem;
    color: string;
    layout: "grid" | "list";
    positiveIsGood: boolean;
}) {
    const [hovered, setHovered] = useState(false);

    return (
        <div
            className="relative rounded-xl border bg-white p-4 dark:bg-zinc-900 transition-all duration-200 cursor-default"
            style={{
                borderColor: hovered ? `${color}66` : undefined,
                boxShadow: hovered
                    ? `0 4px 24px ${color}1a, 0 1px 4px rgba(0,0,0,.05)`
                    : "0 1px 3px rgba(0,0,0,.04)",
            }}
            onMouseEnter={() => setHovered(true)}
            onMouseLeave={() => setHovered(false)}
        >
            {/* Accent bar */}
            <div
                className="absolute left-4 right-4 top-0 h-[2px] rounded-full transition-opacity duration-200"
                style={{ background: color, opacity: hovered ? 1 : 0.35 }}
            />

            {layout === "list" ? (
                // ── LIST layout ──────────────────────────────────────────────────────
                <div className="flex items-center gap-3">
                    {item.icon && (
                        <span
                            className="flex h-9 w-9 flex-shrink-0 items-center justify-center rounded-lg text-lg"
                            style={{ background: `${color}18` }}
                        >
              {item.icon}
            </span>
                    )}
                    <div className="flex-1 min-w-0">
                        <p className="truncate text-xs font-medium text-zinc-500 dark:text-zinc-400">
                            {item.label}
                        </p>
                        {item.subValue && (
                            <p className="text-[10px] text-zinc-400 dark:text-zinc-500">
                                {item.subValue}
                            </p>
                        )}
                    </div>
                    {item.sparkline && (
                        <div className="opacity-80">
                            <Sparkline values={item.sparkline} color={color} />
                        </div>
                    )}
                    <div className="ml-auto text-right flex-shrink-0">
                        <p
                            className="text-xl font-bold tabular-nums leading-none transition-colors duration-150"
                            style={{ color: hovered ? color : undefined }}
                        >
                            {item.value}
                        </p>
                        {item.trend !== undefined && (
                            <TrendBadge
                                trend={item.trend}
                                trendLabel={item.trendLabel}
                                positiveIsGood={positiveIsGood}
                            />
                        )}
                    </div>
                </div>
            ) : (
                // ── GRID layout ──────────────────────────────────────────────────────
                <div className="flex flex-col gap-3">
                    <div className="flex items-start justify-between gap-2">
                        <div className="flex items-center gap-2 min-w-0">
                            {item.icon && (
                                <span
                                    className="flex h-8 w-8 flex-shrink-0 items-center justify-center rounded-lg text-base"
                                    style={{ background: `${color}18` }}
                                >
                  {item.icon}
                </span>
                            )}
                            <span className="truncate text-xs font-medium text-zinc-500 dark:text-zinc-400">
                {item.label}
              </span>
                        </div>
                        {item.sparkline && (
                            <div className="opacity-75 mt-0.5">
                                <Sparkline values={item.sparkline} color={color} />
                            </div>
                        )}
                    </div>
                    <div>
                        <p
                            className="text-2xl font-bold tabular-nums leading-none text-zinc-800 dark:text-zinc-100 transition-colors duration-150"
                            style={{ color: hovered ? color : undefined }}
                        >
                            {item.value}
                        </p>
                        {item.subValue && (
                            <p className="mt-0.5 text-[11px] text-zinc-400 dark:text-zinc-500">
                                {item.subValue}
                            </p>
                        )}
                        {item.trend !== undefined && (
                            <TrendBadge
                                trend={item.trend}
                                trendLabel={item.trendLabel}
                                positiveIsGood={positiveIsGood}
                            />
                        )}
                    </div>
                </div>
            )}
        </div>
    );
}

// ── SummaryPanel ──────────────────────────────────────────────────────────────
export function SummaryPanel({ props, data }: UiComponentProps) {
    const title = String(props?.title ?? "");
    const dataKey = String(props?.dataKey ?? "");
    const layout: "grid" | "list" = props?.layout === "list" ? "list" : "grid";
    const columns: number = Number(props?.columns ?? 0);
    const positiveIsGood: boolean = props?.positiveIsGood !== false;

    const raw =
        (props?.items as any) ??
        (dataKey && data ? (data as any)[dataKey] : null) ??
        null;

    const items: SummaryItem[] = Array.isArray(raw)
        ? raw.map((x: any) => ({
            label: String(x?.label ?? x?.name ?? ""),
            value: x?.value ?? "—",
            subValue: x?.subValue ?? x?.sub_value ?? undefined,
            trend: x?.trend !== undefined ? Number(x.trend) : undefined,
            trendLabel: x?.trendLabel ?? x?.trend_label ?? undefined,
            icon: x?.icon ?? undefined,
            color: x?.color ?? undefined,
            sparkline: Array.isArray(x?.sparkline) ? x.sparkline : undefined,
        }))
        : [];

    // Auto column count
    const cols =
        columns > 0
            ? columns
            : layout === "list"
                ? 1
                : items.length <= 2
                    ? items.length
                    : items.length <= 4
                        ? 2
                        : items.length <= 6
                            ? 3
                            : 4;

    const colClass: Record<number, string> = {
        1: "grid-cols-1",
        2: "grid-cols-1 sm:grid-cols-2",
        3: "grid-cols-1 sm:grid-cols-2 lg:grid-cols-3",
        4: "grid-cols-2 lg:grid-cols-4",
    };

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-5 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950 select-none">
            {title && (
                <div className="mb-4">
          <span className="text-sm font-semibold text-zinc-800 dark:text-zinc-100">
            {title}
          </span>
                </div>
            )}

            {items.length === 0 ? (
                <div className="py-8 text-center text-sm text-zinc-400 dark:text-zinc-600">
                    No items — provide <code className="text-xs">props.items</code> or{" "}
                    <code className="text-xs">data[dataKey]</code>.
                </div>
            ) : (
                <div className={`grid gap-3 ${colClass[Math.min(cols, 4)] ?? "grid-cols-2"}`}>
                    {items.map((item, i) => (
                        <MetricCard
                            key={i}
                            item={item}
                            color={item.color ?? PALETTE[i % PALETTE.length]}
                            layout={layout}
                            positiveIsGood={positiveIsGood}
                        />
                    ))}
                </div>
            )}
        </div>
    );
}

// ── Usage examples ─────────────────────────────────────────────────────────────
//
// ① Grid cu trend + sparkline:
// <SummaryPanel
//   props={{
//     title: "Sumar financiar",
//     items: [
//       {
//         label: "Venituri totale",
//         value: "24,500 lei",
//         subValue: "luna curentă",
//         trend: 12.4,
//         trendLabel: "față de luna trecută",
//         icon: "💰",
//         color: "#10b981",
//         sparkline: [3200, 2800, 3900, 4100, 3700, 4500, 4900],
//       },
//       {
//         label: "Cheltuieli",
//         value: "18,230 lei",
//         trend: -3.2,
//         trendLabel: "față de luna trecută",
//         icon: "💳",
//         color: "#f43f5e",
//         sparkline: [2100, 2400, 2200, 1900, 2100, 1800, 1750],
//       },
//       {
//         label: "Profit net",
//         value: "6,270 lei",
//         trend: 8.1,
//         icon: "📈",
//         color: "#6366f1",
//         sparkline: [900, 750, 1100, 1300, 1200, 1500, 1600],
//       },
//       {
//         label: "Clienți activi",
//         value: "1,842",
//         trend: 5.3,
//         trendLabel: "față de săptămâna trecută",
//         icon: "👥",
//         color: "#f59e0b",
//       },
//     ],
//   }}
//   data={{}}
// />
//
// ② Layout list – status sistem:
// <SummaryPanel
//   props={{
//     title: "Status sistem",
//     layout: "list",
//     positiveIsGood: false,
//     items: [
//       { label: "Uptime server",   value: "99.98%", icon: "🟢", color: "#10b981", trend: 0 },
//       { label: "Latență medie",   value: "42ms",   icon: "⚡", color: "#f59e0b", trend: -8.1, trendLabel: "mai rapid" },
//       { label: "Erori / oră",     value: "2",      icon: "🔴", color: "#f43f5e", trend: 150, trendLabel: "față de ieri" },
//       { label: "Utilizatori live",value: "318",    icon: "👁️", color: "#6366f1", trend: 24.2 },
//     ],
//   }}
//   data={{}}
// />
//
// ③ Via dataKey cu columns fix:
// <SummaryPanel
//   props={{ title: "KPIs", dataKey: "kpis", columns: 3 }}
//   data={{ kpis: [ /* array of items */ ] }}
// />