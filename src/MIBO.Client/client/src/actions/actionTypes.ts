export type Action =
    | ApplyExpensesFiltersAction
    | ExportExpensesAction
    | OpenExpenseDetailsAction
    | NotifyAction;

export type ActionMeta = {
    /** kind-ul widgetului care a emis action-ul */
    sourceKind?: string;
    /** correlationId pentru observability */
    correlationId?: string;
};

export type BaseAction<TType extends string, TPayload> = {
    type: TType;
    payload: TPayload;
    meta?: ActionMeta;
};

export type NotifyAction = BaseAction<
    "NOTIFY",
    { level: "info" | "success" | "warning" | "error"; message: string }
>;

export type ApplyExpensesFiltersAction = BaseAction<
    "EXPENSES/APPLY_FILTERS",
    {
        q?: string;
        category?: string;
        dateFrom?: string;
        dateTo?: string;
        minAmount?: string;
        maxAmount?: string;
        sortBy?: string;
        page?: number;
        pageSize?: number;
    }
>;

export type ExportExpensesAction = BaseAction<
    "EXPENSES/EXPORT",
    {
        format: "csv" | "xlsx";
        // server-side export uses filters
        filters: {
            q?: string;
            category?: string;
            dateFrom?: string;
            dateTo?: string;
            minAmount?: string;
            maxAmount?: string;
            sortBy?: string;
        };
    }
>;

export type OpenExpenseDetailsAction = BaseAction<
    "EXPENSES/OPEN_DETAILS",
    {
        expenseId: string;
    }
>;
