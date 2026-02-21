import { useMemo } from "react";
import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";

export function DataTableCard({ props, data }: UiComponentProps) {
    const title = String(props?.title ?? "Data Table");
    const dataKey = String(props?.dataKey ?? "");

    const raw =
        (props?.rows as any) ??
        (props?.data as any) ??
        (dataKey && data ? (data as any)[dataKey] : null) ??
        [];

    const rows: Record<string, any>[] = Array.isArray(raw)
        ? raw.filter((x) => x && typeof x === "object")
        : [];

    const columns = useMemo(() => {
        const first = rows[0];
        if (!first) return [];
        return Object.keys(first).slice(0, 8); // limit pentru demo
    }, [rows]);

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
            <div className="text-sm font-semibold">{title}</div>

            {rows.length === 0 ? (
                <div className="mt-3 text-sm opacity-70">
                    No rows (provide props.rows or data[dataKey]).
                </div>
            ) : (
                <div className="mt-3 overflow-auto rounded-xl border border-zinc-200/70 dark:border-zinc-800/70">
                    <table className="w-full text-left text-sm">
                        <thead className="bg-zinc-50 dark:bg-zinc-900">
                        <tr>
                            {columns.map((c) => (
                                <th key={c} className="px-3 py-2 font-medium">
                                    {c}
                                </th>
                            ))}
                        </tr>
                        </thead>
                        <tbody>
                        {rows.slice(0, 12).map((r, idx) => (
                            <tr
                                key={idx}
                                className="border-t border-zinc-200/70 dark:border-zinc-800/70"
                            >
                                {columns.map((c) => (
                                    <td key={c} className="px-3 py-2 align-top">
                                        {String(r[c] ?? "")}
                                    </td>
                                ))}
                            </tr>
                        ))}
                        </tbody>
                    </table>
                </div>
            )}

            {rows.length > 12 ? (
                <div className="mt-2 text-xs opacity-60">
                    Showing 12 of {rows.length} rows (demo limit)
                </div>
            ) : null}
        </div>
    );
}