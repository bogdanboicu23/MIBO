import { useMemo, useState } from "react";
import { cn } from "@/utils/cn";
import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";

type Column = {
    key: string;
    header: string;
    type?: "text" | "number" | "money" | "date" | "badge";
    align?: "left" | "center" | "right";
    width?: string; // ex: "160px"
};

type DataTableProps = {
    title?: string;
    subtitle?: string;

    columns: Column[];
    rows: Record<string, any>[];

    rowKey?: string;

    pageSize?: number;

    search?: {
        placeholder?: string;
        keys?: string[];
    };

    sort?: {
        key: string;
        dir: "asc" | "desc";
    };

    density?: "compact" | "normal" | "comfortable";
    zebra?: boolean;
    stickyHeader?: boolean;

    actions?: {
        label: string;
        actionId: string;
        variant?: "primary" | "secondary" | "ghost";
    }[];

    rowAction?: {
        actionId: string; // ex: "shop.openProduct"
    };
};

function formatCell(type: Column["type"], value: any) {
    if (value == null) return "—";

    switch (type) {
        case "number":
            return typeof value === "number" ? value.toLocaleString() : String(value);
        case "money": {
            // acceptă fie { amount, currency } fie număr simplu
            if (typeof value === "object" && value) {
                const amount = Number(value.amount ?? 0);
                const currency = String(value.currency ?? "");
                return `${amount.toLocaleString()}${currency ? " " + currency : ""}`;
            }
            const n = Number(value);
            return Number.isFinite(n) ? n.toLocaleString() : String(value);
        }
        case "date": {
            const d = new Date(value);
            return isNaN(d.getTime()) ? String(value) : d.toLocaleString();
        }
        case "badge":
            return String(value);
        default:
            return String(value);
    }
}

function compare(a: any, b: any) {
    if (a == null && b == null) return 0;
    if (a == null) return -1;
    if (b == null) return 1;

    const na = typeof a === "number" ? a : Number(a);
    const nb = typeof b === "number" ? b : Number(b);
    const bothNum = Number.isFinite(na) && Number.isFinite(nb);

    if (bothNum) return na - nb;

    const sa = String(a).toLowerCase();
    const sb = String(b).toLowerCase();
    return sa.localeCompare(sb);
}

export function DataTableCard(props: UiComponentProps) {
    // aștept ca UiComponentProps să conțină props în props.props (sau direct).
    // dacă la tine structura e diferită, ajustezi cele 2 linii de mai jos.
    const spec = (props as any)?.props ?? (props as any);
    const p: DataTableProps = spec;

    const {
        title,
        subtitle,
        columns = [],
        rows = [],
        rowKey = "id",
        pageSize = 10,
        search,
        sort,
        density = "normal",
        zebra = true,
        stickyHeader = true,
        actions,
        rowAction,
    } = p;

    const [query, setQuery] = useState("");
    const [page, setPage] = useState(1);
    const [localSort, setLocalSort] = useState<{ key: string; dir: "asc" | "desc" } | null>(
        sort ?? null
    );

    const densityClasses =
        density === "compact"
            ? "text-sm"
            : density === "comfortable"
                ? "text-base"
                : "text-sm";

    const cellPad =
        density === "compact" ? "px-3 py-2" : density === "comfortable" ? "px-4 py-3" : "px-4 py-2.5";

    const searchableKeys = search?.keys?.length
        ? search.keys
        : columns.map((c) => c.key);

    const filtered = useMemo(() => {
        const q = query.trim().toLowerCase();
        if (!q) return rows;

        return rows.filter((r) =>
            searchableKeys.some((k) => {
                const v = r?.[k];
                if (v == null) return false;
                return String(v).toLowerCase().includes(q);
            })
        );
    }, [rows, query, searchableKeys]);

    const sorted = useMemo(() => {
        if (!localSort) return filtered;
        const { key, dir } = localSort;
        const copy = [...filtered];
        copy.sort((ra, rb) => {
            const c = compare(ra?.[key], rb?.[key]);
            return dir === "asc" ? c : -c;
        });
        return copy;
    }, [filtered, localSort]);

    const totalPages = Math.max(1, Math.ceil(sorted.length / pageSize));
    const safePage = Math.min(page, totalPages);

    const paged = useMemo(() => {
        const start = (safePage - 1) * pageSize;
        return sorted.slice(start, start + pageSize);
    }, [sorted, safePage, pageSize]);

    const onAction = (actionId: string, payload?: any) => {
        // dacă UiRenderer îți pasează un callback generic, îl folosești:
        // ex: props.onAction?.(actionId, payload)
        (props as any)?.onAction?.(actionId, payload);

        // fallback: poți emite un event global dacă vrei (opțional)
        // window.dispatchEvent(new CustomEvent("ui-action", { detail: { actionId, payload } }));
    };

    const toggleSort = (key: string) => {
        setPage(1);
        setLocalSort((cur) => {
            if (!cur || cur.key !== key) return { key, dir: "asc" };
            if (cur.dir === "asc") return { key, dir: "desc" };
            return null; // a 3-a apăsare -> fără sort
        });
    };

    return (
        <div className={cn("rounded-2xl border border-zinc-200 bg-white shadow-sm dark:border-zinc-800 dark:bg-zinc-950")}>
            {/* Header */}
            <div className="flex flex-col gap-3 border-b border-zinc-200 p-4 dark:border-zinc-800">
                <div className="flex items-start justify-between gap-4">
                    <div className="min-w-0">
                        {title ? (
                            <div className="text-base font-semibold text-zinc-900 dark:text-zinc-50">
                                {title}
                            </div>
                        ) : null}
                        {subtitle ? (
                            <div className="mt-1 text-sm text-zinc-600 dark:text-zinc-400">
                                {subtitle}
                            </div>
                        ) : null}
                    </div>

                    {actions?.length ? (
                        <div className="flex flex-wrap items-center gap-2">
                            {actions.map((a) => (
                                <button
                                    key={a.actionId}
                                    onClick={() => onAction(a.actionId, { source: "dataTable" })}
                                    className={cn(
                                        "rounded-xl px-3 py-2 text-sm font-medium transition",
                                        a.variant === "primary" &&
                                        "bg-zinc-900 text-white hover:bg-zinc-800 dark:bg-white dark:text-zinc-900 dark:hover:bg-zinc-100",
                                        a.variant === "secondary" &&
                                        "bg-zinc-100 text-zinc-900 hover:bg-zinc-200 dark:bg-zinc-900 dark:text-zinc-50 dark:hover:bg-zinc-800",
                                        (!a.variant || a.variant === "ghost") &&
                                        "bg-transparent text-zinc-700 hover:bg-zinc-100 dark:text-zinc-200 dark:hover:bg-zinc-900"
                                    )}
                                >
                                    {a.label}
                                </button>
                            ))}
                        </div>
                    ) : null}
                </div>

                {/* Toolbar */}
                <div className="flex flex-col gap-2 sm:flex-row sm:items-center sm:justify-between">
                    {search ? (
                        <div className="relative w-full sm:max-w-md">
                            <input
                                value={query}
                                onChange={(e) => {
                                    setQuery(e.target.value);
                                    setPage(1);
                                }}
                                placeholder={search.placeholder ?? "Caută..."}
                                className="w-full rounded-xl border border-zinc-200 bg-white px-3 py-2 text-sm outline-none transition focus:border-zinc-400 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-50 dark:focus:border-zinc-600"
                            />
                        </div>
                    ) : (
                        <div />
                    )}

                    <div className="flex items-center gap-2 text-sm text-zinc-600 dark:text-zinc-400">
            <span>
              {sorted.length.toLocaleString()} rând{sorted.length === 1 ? "" : "uri"}
            </span>
                        <span className="select-none">•</span>
                        <span>
              Pagina {safePage} / {totalPages}
            </span>
                    </div>
                </div>
            </div>

            {/* Table */}
            <div className="w-full overflow-x-auto">
                <table className={cn("w-full", densityClasses)}>
                    <thead className={cn(stickyHeader ? "sticky top-0 z-10" : "", "bg-zinc-50 dark:bg-zinc-900")}>
                    <tr className="border-b border-zinc-200 dark:border-zinc-800">
                        {columns.map((c) => {
                            const isSorted = localSort?.key === c.key;
                            const dir = localSort?.dir;
                            return (
                                <th
                                    key={c.key}
                                    style={c.width ? { width: c.width } : undefined}
                                    className={cn(
                                        cellPad,
                                        "text-left font-semibold text-zinc-800 dark:text-zinc-100",
                                        "whitespace-nowrap",
                                        "cursor-pointer select-none hover:bg-zinc-100 dark:hover:bg-zinc-800",
                                        c.align === "center" && "text-center",
                                        c.align === "right" && "text-right"
                                    )}
                                    onClick={() => toggleSort(c.key)}
                                    title="Click pentru sortare"
                                >
                                    <div className={cn("flex items-center gap-2", c.align === "right" && "justify-end", c.align === "center" && "justify-center")}>
                                        <span>{c.header}</span>
                                        <span className="text-xs text-zinc-500 dark:text-zinc-400">
                        {isSorted ? (dir === "asc" ? "▲" : "▼") : ""}
                      </span>
                                    </div>
                                </th>
                            );
                        })}
                    </tr>
                    </thead>

                    <tbody>
                    {paged.length === 0 ? (
                        <tr>
                            <td colSpan={columns.length} className={cn(cellPad, "text-center text-zinc-500 dark:text-zinc-400")}>
                                Nimic de afișat.
                            </td>
                        </tr>
                    ) : (
                        paged.map((r, idx) => {
                            const key = r?.[rowKey] ?? `${safePage}-${idx}`;
                            return (
                                <tr
                                    key={key}
                                    className={cn(
                                        "border-b border-zinc-100 dark:border-zinc-900",
                                        zebra && idx % 2 === 1 && "bg-zinc-50/60 dark:bg-zinc-900/40",
                                        rowAction?.actionId && "cursor-pointer hover:bg-zinc-100/70 dark:hover:bg-zinc-800/60"
                                    )}
                                    onClick={() => {
                                        if (!rowAction?.actionId) return;
                                        onAction(rowAction.actionId, { row: r, rowKey: key, source: "dataTable.row" });
                                    }}
                                >
                                    {columns.map((c) => {
                                        const raw = r?.[c.key];
                                        const formatted = formatCell(c.type, raw);

                                        if (c.type === "badge") {
                                            return (
                                                <td key={c.key} className={cn(cellPad, "whitespace-nowrap", c.align === "right" && "text-right", c.align === "center" && "text-center")}>
                            <span className="inline-flex items-center rounded-full border border-zinc-200 bg-white px-2 py-1 text-xs font-medium text-zinc-700 dark:border-zinc-800 dark:bg-zinc-950 dark:text-zinc-200">
                              {formatted}
                            </span>
                                                </td>
                                            );
                                        }

                                        return (
                                            <td
                                                key={c.key}
                                                className={cn(
                                                    cellPad,
                                                    "whitespace-nowrap text-zinc-800 dark:text-zinc-100",
                                                    c.align === "right" && "text-right",
                                                    c.align === "center" && "text-center"
                                                )}
                                            >
                                                {formatted}
                                            </td>
                                        );
                                    })}
                                </tr>
                            );
                        })
                    )}
                    </tbody>
                </table>
            </div>

            {/* Footer pagination */}
            <div className="flex items-center justify-between gap-3 p-4">
                <button
                    className="rounded-xl border border-zinc-200 px-3 py-2 text-sm font-medium text-zinc-700 hover:bg-zinc-50 disabled:opacity-40 dark:border-zinc-800 dark:text-zinc-200 dark:hover:bg-zinc-900"
                    disabled={safePage <= 1}
                    onClick={() => setPage((p) => Math.max(1, p - 1))}
                >
                    Înapoi
                </button>

                <div className="text-sm text-zinc-600 dark:text-zinc-400">
                    {((safePage - 1) * pageSize + 1).toLocaleString()}–{Math.min(safePage * pageSize, sorted.length).toLocaleString()} din{" "}
                    {sorted.length.toLocaleString()}
                </div>

                <button
                    className="rounded-xl border border-zinc-200 px-3 py-2 text-sm font-medium text-zinc-700 hover:bg-zinc-50 disabled:opacity-40 dark:border-zinc-800 dark:text-zinc-200 dark:hover:bg-zinc-900"
                    disabled={safePage >= totalPages}
                    onClick={() => setPage((p) => Math.min(totalPages, p + 1))}
                >
                    Înainte
                </button>
            </div>
        </div>
    );
}