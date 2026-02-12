import type { Category, Expense, ExpensesQuery, ExpensesResponse } from "./expenses.types";
import { CATEGORIES } from "./expenses.types";

function toISODate(d: Date) {
    const yyyy = d.getFullYear();
    const mm = String(d.getMonth() + 1).padStart(2, "0");
    const dd = String(d.getDate()).padStart(2, "0");
    return `${yyyy}-${mm}-${dd}`;
}

function uid() {
    return Math.random().toString(16).slice(2) + Date.now().toString(16);
}

export function startOfLastMonth(now = new Date()) {
    return new Date(now.getFullYear(), now.getMonth() - 1, 1);
}

export function endOfLastMonth(now = new Date()) {
    return new Date(now.getFullYear(), now.getMonth(), 0);
}

export function startOfLastNDays(n: number, now = new Date()) {
    const d = new Date(now);
    d.setDate(now.getDate() - (n - 1));
    return d;
}

const MOCK_EXPENSES: Expense[] = (() => {
    const now = new Date();
    const merchantsByCategory: Record<Category, string[]> = {
        Food: ["Linella", "Kaufland", "Andy's Pizza", "La Placinte", "Star Kebab", "Coffee Point"],
        Transport: ["Trolleybus", "Taxi", "Benzinarie", "Parkingi"],
        Bills: ["Electricitate", "Internet", "Gaz", "Apă"],
        Shopping: ["MallDova", "Online Store", "Pharmashop", "Electro Market"],
        Health: ["Farmacie", "Clinica", "Vitamins"],
        Entertainment: ["Cinema", "Concert", "Bar", "Games"],
        Subscriptions: ["Netflix", "Spotify", "GitHub", "ChatGPT"],
        Other: ["Cadouri", "Servicii", "Comision Bancar"],
    };

    let seed = 42;
    const rand = () => {
        seed = (seed * 1664525 + 1013904223) % 4294967296;
        return seed / 4294967296;
    };

    const out: Expense[] = [];
    for (let i = 0; i < 200; i++) {
        const daysAgo = Math.floor(rand() * 60);
        const date = new Date(now);
        date.setDate(now.getDate() - daysAgo);

        const category = CATEGORIES[Math.floor(rand() * CATEGORIES.length)];
        const merchant = merchantsByCategory[category][Math.floor(rand() * merchantsByCategory[category].length)];

        const base =
            category === "Bills"
                ? 120
                : category === "Shopping"
                    ? 200
                    : category === "Transport"
                        ? 60
                        : category === "Subscriptions"
                            ? 90
                            : category === "Health"
                                ? 110
                                : category === "Entertainment"
                                    ? 130
                                    : 80;

        const amount = Math.round((base * (0.3 + rand() * 1.4)) * 100) / 100;

        out.push({
            id: `exp_${i}_${uid()}`,
            date: toISODate(date),
            merchant,
            category,
            note: rand() > 0.65 ? "—" : "",
            amount,
            currency: "MDL",
        });
    }

    out.sort((a, b) => (a.date < b.date ? 1 : -1));
    return out;
})();

export async function mockFetchExpenses(query: ExpensesQuery): Promise<ExpensesResponse> {
    await new Promise((r) => setTimeout(r, 250));

    const min = query.minAmount.trim() ? Number(query.minAmount) : null;
    const max = query.maxAmount.trim() ? Number(query.maxAmount) : null;
    const q = query.q.trim().toLowerCase();

    let filtered = MOCK_EXPENSES.filter((e) => {
        if (e.date < query.dateFrom || e.date > query.dateTo) return false;
        if (query.category !== "All" && e.category !== query.category) return false;
        if (min !== null && !Number.isNaN(min) && e.amount < min) return false;
        if (max !== null && !Number.isNaN(max) && e.amount > max) return false;

        if (q) {
            const hay = `${e.merchant} ${e.category} ${e.note ?? ""}`.toLowerCase();
            if (!hay.includes(q)) return false;
        }
        return true;
    });

    filtered = filtered.slice().sort((a, b) => {
        switch (query.sortBy) {
            case "date_desc":
                return a.date < b.date ? 1 : -1;
            case "date_asc":
                return a.date > b.date ? 1 : -1;
            case "amount_desc":
                return a.amount < b.amount ? 1 : -1;
            case "amount_asc":
                return a.amount > b.amount ? 1 : -1;
        }
    });

    const total = filtered.length;
    const totalAmount = filtered.reduce((s, e) => s + e.amount, 0);

    const byCategoryMap = new Map<Category, number>();
    for (const c of CATEGORIES) byCategoryMap.set(c, 0);
    for (const e of filtered) byCategoryMap.set(e.category, (byCategoryMap.get(e.category) || 0) + e.amount);

    const byCategory = Array.from(byCategoryMap.entries())
        .map(([category, amount]) => ({ category, amount }))
        .filter((x) => x.amount > 0)
        .sort((a, b) => b.amount - a.amount);

    const start = (query.page - 1) * query.pageSize;
    const end = start + query.pageSize;
    const items = filtered.slice(start, end);

    return { items, total, totalAmount, byCategory };
}
