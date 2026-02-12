import type { ExpensesDashboardProps, ExpensesQuery, ExpensesResponse, Category } from "./expenses.types";
import { CATEGORIES } from "./expenses.types";
import { expensesApi } from "./expenses.api";
import {
    Badge,
    Button,
    Card,
    CardContent,
    CardHeader,
    CardTitle,
    CardDescription,
    Input,
    Select,
    Table,
    THead,
    TBody,
    TR,
    TH,
    TD,
    Spinner,
} from "../../components/ui";
import { endOfLastMonth, startOfLastMonth, startOfLastNDays } from "./expenses.mock";
import { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { actionBus } from "../../actions";

function formatMDL(n: number) {
    return `${n.toFixed(2)} MDL`;
}

function toISODate(d: Date) {
    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, "0");
    const dd = String(d.getDate()).padStart(2, "0");
    return `${yyyy}-${mm}-${dd}`;
}

function resolveRange(
    preset: ExpensesDashboardProps["defaultRange"] | undefined
): { dateFrom: string; dateTo: string } {
    const now = new Date();
    if (preset === "last_7_days") {
        return { dateFrom: toISODate(startOfLastNDays(7, now)), dateTo: toISODate(now) };
    }
    // last_month default
    return { dateFrom: toISODate(startOfLastMonth(now)), dateTo: toISODate(endOfLastMonth(now)) };
}

export function ExpensesDashboard(props: ExpensesDashboardProps) {
    const { title, defaultRange = "last_month", defaultCategory = "All", allowExport = true } = props;

    const initialRange = useMemo(() => resolveRange(defaultRange), [defaultRange]);

    const [query, setQuery] = useState<ExpensesQuery>({
        q: "",
        category: defaultCategory as any,
        dateFrom: initialRange.dateFrom,
        dateTo: initialRange.dateTo,
        minAmount: "",
        maxAmount: "",
        sortBy: "date_desc",
        page: 1,
        pageSize: 10,
    });

    const [loading, setLoading] = useState(false);
    const [resp, setResp] = useState<ExpensesResponse | null>(null);
    const [error, setError] = useState<string | null>(null);

    const debounceRef = useRef<number | null>(null);

    const fetchData = useCallback(async (q: ExpensesQuery) => {
        setLoading(true);
        setError(null);
        try {
            const r = await expensesApi.getExpenses(q);
            setResp(r);
        } catch {
            setError("Eroare la încărcarea cheltuielilor.");
        } finally {
            setLoading(false);
        }
    }, []);

    const scheduleFetch = useCallback(
        (next: ExpensesQuery) => {
            if (debounceRef.current) window.clearTimeout(debounceRef.current);
            debounceRef.current = window.setTimeout(() => fetchData(next), 200);
        },
        [fetchData]
    );

    useEffect(() => {
        // initial fetch
        scheduleFetch(query);
        // eslint-disable-next-line react-hooks/exhaustive-deps
    }, []);

    const update = (patch: Partial<ExpensesQuery>, options?: { resetPage?: boolean }) => {
        const next: ExpensesQuery = {
            ...query,
            ...patch,
            page: options?.resetPage ? 1 : patch.page ?? query.page,
        };
        setQuery(next);
        scheduleFetch(next);

        // Emit action (for analytics / optional chat orchestration later)
        actionBus.dispatch({
            type: "EXPENSES/APPLY_FILTERS",
            payload: {
                q: next.q,
                category: next.category,
                dateFrom: next.dateFrom,
                dateTo: next.dateTo,
                minAmount: next.minAmount,
                maxAmount: next.maxAmount,
                sortBy: next.sortBy,
                page: next.page,
                pageSize: next.pageSize,
            },
            meta: { sourceKind: "expenses.dashboard" },
        });
    };

    const totalAmount = resp?.totalAmount ?? 0;
    const totalItems = resp?.total ?? 0;

    const topCategories = resp?.byCategory.slice(0, 4) ?? [];
    const maxCat = topCategories.reduce((m, x) => Math.max(m, x.amount), 0);

    const canNext = !!resp && query.page * query.pageSize < resp.total;

    const onExport = async (format: "csv" | "xlsx") => {
        actionBus.dispatch({
            type: "EXPENSES/EXPORT",
            payload: {
                format,
                filters: {
                    q: query.q,
                    category: query.category,
                    dateFrom: query.dateFrom,
                    dateTo: query.dateTo,
                    minAmount: query.minAmount,
                    maxAmount: query.maxAmount,
                    sortBy: query.sortBy,
                },
            },
            meta: { sourceKind: "expenses.dashboard" },
        });

        try {
            await expensesApi.exportExpenses({ format, query });
            actionBus.dispatch({
                type: "NOTIFY",
                payload: { level: "success", message: `Export (${format.toUpperCase()}) generat (mock).` },
                meta: { sourceKind: "expenses.dashboard" },
            });
        } catch {
            actionBus.dispatch({
                type: "NOTIFY",
                payload: { level: "error", message: "Export eșuat." },
                meta: { sourceKind: "expenses.dashboard" },
            });
        }
    };

    return (
        <Card>
            <CardHeader>
                <div className="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                    <div>
                        <div className="flex flex-wrap items-center gap-2">
                            <CardTitle>{title ?? "Cheltuieli"}</CardTitle>
                            <Badge variant="outline">
                                {query.dateFrom} → {query.dateTo}
                            </Badge>
                            <Badge variant="outline">{query.category === "All" ? "All" : query.category}</Badge>
                            {loading ? <Badge variant="subtle">loading…</Badge> : <Badge variant="subtle">ready</Badge>}
                        </div>
                        <CardDescription>Filtrează cheltuielile. Modificările simulează apeluri către backend.</CardDescription>
                    </div>

                    <div className="flex flex-wrap items-center gap-2">
                        <div className="rounded-xl border border-zinc-200 bg-zinc-50 px-3 py-2 text-sm text-zinc-800 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-100">
                            <div className="font-semibold">{formatMDL(totalAmount)}</div>
                            <div className="text-xs text-zinc-600 dark:text-zinc-400">{totalItems} tranzacții</div>
                        </div>

                        <Button
                            variant="secondary"
                            onClick={() => {
                                const r = resolveRange(defaultRange);
                                update(
                                    {
                                        q: "",
                                        category: defaultCategory as any,
                                        dateFrom: r.dateFrom,
                                        dateTo: r.dateTo,
                                        minAmount: "",
                                        maxAmount: "",
                                        sortBy: "date_desc",
                                        page: 1,
                                        pageSize: query.pageSize,
                                    },
                                    { resetPage: false }
                                );
                            }}
                        >
                            Reset
                        </Button>

                        {allowExport ? (
                            <div className="flex items-center gap-2">
                                <Button variant="ghost" onClick={() => onExport("csv")} disabled={loading}>
                                    Export CSV
                                </Button>
                                <Button variant="ghost" onClick={() => onExport("xlsx")} disabled={loading}>
                                    Export XLSX
                                </Button>
                            </div>
                        ) : null}
                    </div>
                </div>
            </CardHeader>

            <CardContent>
                {/* Filters */}
                <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 lg:grid-cols-12">
                    <div className="lg:col-span-4">
                        <Input
                            label="Căutare"
                            value={query.q}
                            onChange={(e) => update({ q: e.target.value }, { resetPage: true })}
                            placeholder="merchant / categorie / notă…"
                        />
                    </div>

                    <div className="lg:col-span-2">
                        <Select
                            label="Categorie"
                            value={query.category}
                            onChange={(e) => update({ category: e.target.value as Category | "All" }, { resetPage: true })}
                        >
                            <option value="All">All</option>
                            {CATEGORIES.map((c) => (
                                <option key={c} value={c}>
                                    {c}
                                </option>
                            ))}
                        </Select>
                    </div>

                    <div className="lg:col-span-2">
                        <Input
                            label="De la"
                            type="date"
                            value={query.dateFrom}
                            onChange={(e) => update({ dateFrom: e.target.value }, { resetPage: true })}
                        />
                    </div>

                    <div className="lg:col-span-2">
                        <Input
                            label="Până la"
                            type="date"
                            value={query.dateTo}
                            onChange={(e) => update({ dateTo: e.target.value }, { resetPage: true })}
                        />
                    </div>

                    <div className="lg:col-span-1">
                        <Input
                            label="Min"
                            inputMode="decimal"
                            value={query.minAmount}
                            onChange={(e) => update({ minAmount: e.target.value }, { resetPage: true })}
                            placeholder="0"
                        />
                    </div>

                    <div className="lg:col-span-1">
                        <Input
                            label="Max"
                            inputMode="decimal"
                            value={query.maxAmount}
                            onChange={(e) => update({ maxAmount: e.target.value }, { resetPage: true })}
                            placeholder="∞"
                        />
                    </div>

                    <div className="lg:col-span-12 flex flex-col gap-2 sm:flex-row sm:items-end sm:justify-between">
                        <div className="w-full sm:max-w-xs">
                            <Select
                                label="Sortare"
                                value={query.sortBy}
                                onChange={(e) => update({ sortBy: e.target.value as any }, { resetPage: true })}
                            >
                                <option value="date_desc">Data (nou → vechi)</option>
                                <option value="date_asc">Data (vechi → nou)</option>
                                <option value="amount_desc">Sumă (mare → mic)</option>
                                <option value="amount_asc">Sumă (mic → mare)</option>
                            </Select>
                        </div>

                        <div className="w-full sm:max-w-md">
                            <div className="text-xs font-medium text-zinc-700 dark:text-zinc-300">Top categorii</div>
                            <div className="mt-1 space-y-1">
                                {loading && !resp ? (
                                    <div className="flex items-center gap-2 text-xs text-zinc-500 dark:text-zinc-400">
                                        <Spinner /> se încarcă…
                                    </div>
                                ) : topCategories.length === 0 ? (
                                    <div className="text-xs text-zinc-500 dark:text-zinc-400">Nicio categorie (filtre restrictive).</div>
                                ) : (
                                    topCategories.map((x) => {
                                        const pct = maxCat ? Math.round((x.amount / maxCat) * 100) : 0;
                                        return (
                                            <div key={x.category} className="flex items-center gap-2">
                                                <div className="w-24 truncate text-xs text-zinc-600 dark:text-zinc-400">{x.category}</div>
                                                <div className="h-2 flex-1 overflow-hidden rounded-full bg-zinc-100 dark:bg-zinc-900">
                                                    <div className="h-2 rounded-full bg-zinc-700 dark:bg-zinc-200" style={{ width: `${pct}%` }} />
                                                </div>
                                                <div className="w-24 text-right text-xs text-zinc-600 dark:text-zinc-400">
                                                    {formatMDL(x.amount)}
                                                </div>
                                            </div>
                                        );
                                    })
                                )}
                            </div>
                        </div>
                    </div>
                </div>

                {/* Table */}
                <div className="mt-4 overflow-hidden rounded-2xl border border-zinc-200 dark:border-zinc-800">
                    <div className="overflow-x-auto">
                        <Table>
                            <THead>
                                <TR>
                                    <TH>Data</TH>
                                    <TH>Merchant</TH>
                                    <TH>Categorie</TH>
                                    <TH className="text-right">Sumă</TH>
                                </TR>
                            </THead>

                            <TBody>
                                {error ? (
                                    <TR>
                                        <TD colSpan={4} className="py-6 text-sm text-red-600 dark:text-red-400">
                                            {error}
                                        </TD>
                                    </TR>
                                ) : !resp && loading ? (
                                    Array.from({ length: 6 }).map((_, i) => (
                                        <TR key={i}>
                                            <TD><div className="h-4 w-24 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" /></TD>
                                            <TD><div className="h-4 w-56 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" /></TD>
                                            <TD><div className="h-4 w-28 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" /></TD>
                                            <TD className="text-right"><div className="ml-auto h-4 w-24 animate-pulse rounded bg-zinc-200 dark:bg-zinc-800" /></TD>
                                        </TR>
                                    ))
                                ) : resp && resp.items.length === 0 ? (
                                    <TR>
                                        <TD colSpan={4} className="py-6 text-sm text-zinc-600 dark:text-zinc-400">
                                            Nicio cheltuială pentru filtrele selectate.
                                        </TD>
                                    </TR>
                                ) : (
                                    resp?.items.map((e) => (
                                        <TR
                                            key={e.id}
                                            onClick={() =>
                                                actionBus.dispatch({
                                                    type: "EXPENSES/OPEN_DETAILS",
                                                    payload: { expenseId: e.id },
                                                    meta: { sourceKind: "expenses.dashboard" },
                                                })
                                            }
                                            className="cursor-pointer"
                                        >
                                            <TD className="whitespace-nowrap">{e.date}</TD>
                                            <TD>
                                                <div className="font-medium text-zinc-900 dark:text-zinc-100">{e.merchant}</div>
                                                {e.note ? <div className="text-xs text-zinc-500 dark:text-zinc-400">{e.note}</div> : null}
                                            </TD>
                                            <TD>
                                                <Badge variant="outline">{e.category}</Badge>
                                            </TD>
                                            <TD className="whitespace-nowrap text-right font-semibold text-zinc-900 dark:text-zinc-100">
                                                {formatMDL(e.amount)}
                                            </TD>
                                        </TR>
                                    ))
                                )}
                            </TBody>
                        </Table>
                    </div>

                    {/* Pagination */}
                    <div className="flex items-center justify-between gap-2 border-t border-zinc-200 bg-zinc-50 px-4 py-3 dark:border-zinc-800 dark:bg-zinc-900">
                        <div className="text-xs text-zinc-600 dark:text-zinc-400">
                            Pagina <span className="font-medium text-zinc-900 dark:text-zinc-100">{query.page}</span> •{" "}
                            {query.pageSize}/pagină
                        </div>

                        <div className="flex items-center gap-2">
                            <Select
                                value={String(query.pageSize)}
                                onChange={(e) => update({ pageSize: Number(e.target.value), page: 1 })}
                            >
                                {[5, 10, 20, 50].map((n) => (
                                    <option key={n} value={n}>
                                        {n}
                                    </option>
                                ))}
                            </Select>

                            <Button variant="secondary" size="sm" disabled={query.page <= 1 || loading} onClick={() => update({ page: query.page - 1 })}>
                                ← Prev
                            </Button>
                            <Button variant="secondary" size="sm" disabled={!canNext || loading} onClick={() => update({ page: query.page + 1 })}>
                                Next →
                            </Button>
                        </div>
                    </div>
                </div>
            </CardContent>
        </Card>
    );
}
