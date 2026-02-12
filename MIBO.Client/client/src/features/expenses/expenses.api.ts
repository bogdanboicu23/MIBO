import type { ExpensesQuery, ExpensesResponse } from "./expenses.types";

function cleanParams(obj: Record<string, string>) {
    const out = new URLSearchParams();
    for (const [k, v] of Object.entries(obj)) {
        if (v !== undefined && v !== null && String(v).trim() !== "") out.set(k, String(v));
    }
    return out;
}

export type ExpensesApi = {
    getExpenses: (query: ExpensesQuery) => Promise<ExpensesResponse>;
    exportExpenses: (args: { format: "csv" | "xlsx"; query: ExpensesQuery }) => Promise<void>;
};

export const expensesApi: ExpensesApi = {
    async getExpenses(query) {
        const params = cleanParams({
            q: query.q,
            category: query.category,
            dateFrom: query.dateFrom,
            dateTo: query.dateTo,
            minAmount: query.minAmount,
            maxAmount: query.maxAmount,
            sortBy: query.sortBy,
            page: String(query.page),
            pageSize: String(query.pageSize),
        });

        // TODO - add expenses API
        const res = await fetch(`/expenses?${params.toString()}`);
        if (!res.ok) {
            const txt = await res.text().catch(() => "");
            throw new Error(`GET /expenses failed: ${res.status} ${res.statusText}${txt ? ` — ${txt}` : ""}`);
        }

        return (await res.json()) as ExpensesResponse;
    },

    async exportExpenses() {
        // TODO - add export expenses API
        await new Promise((r) => setTimeout(r, 200));
    },
};




