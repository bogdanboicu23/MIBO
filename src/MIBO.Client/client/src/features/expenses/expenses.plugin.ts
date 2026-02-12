import type { UiPlugin } from "../../core/uiRuntime";
import { adaptExpensesDashboardProps } from "./expenses.adapter";
import { ExpensesDashboard } from "./ExpensesDashboard";
import type { ExpensesDashboardProps } from "./expenses.types";

export const expensesDashboardPlugin: UiPlugin<ExpensesDashboardProps> = {
    kind: "expenses.dashboard",
    adapt: adaptExpensesDashboardProps,
    Component: ExpensesDashboard,
};
