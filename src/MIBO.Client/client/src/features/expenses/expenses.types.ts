export type Category =
    | "Food"
    | "Transport"
    | "Bills"
    | "Shopping"
    | "Health"
    | "Entertainment"
    | "Subscriptions"
    | "Other";

export const CATEGORIES: Category[] = [
    "Food",
    "Transport",
    "Bills",
    "Shopping",
    "Health",
    "Entertainment",
    "Subscriptions",
    "Other",
];

export type Expense = {
    id: string;
    date: string; // YYYY-MM-DD
    merchant: string;
    category: Category;
    note?: string;
    amount: number;
    currency: "MDL";
};

export type ExpensesQuery = {
    q: string;
    category: Category | "All";
    dateFrom: string;
    dateTo: string;
    minAmount: string;
    maxAmount: string;
    sortBy: "date_desc" | "date_asc" | "amount_desc" | "amount_asc";
    page: number;
    pageSize: number;
};

export type ExpensesResponse = {
    items: Expense[];
    total: number;
    totalAmount: number;
    byCategory: { category: Category; amount: number }[];
};

export type ExpensesDashboardProps = {
    /** Title optional (UiRenderer afișează deja view.title; dar e ok să-l ai și aici) */
    title?: string;

    /** Range preset */
    defaultRange?: "last_month" | "last_7_days" | "custom";

    /** Default filters (optional) */
    defaultCategory?: Category | "All";
    allowExport?: boolean;
};
