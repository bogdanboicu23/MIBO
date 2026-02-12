import type { UiPlugin } from "../../components/sandbox/uiRuntime/UiPlugin";
import { adaptExpensesDashboardProps } from "./expenses.adapter";
import { ExpensesDashboard } from "./ExpensesDashboard";
import type { ExpensesDashboardProps } from "./expenses.types";

export const expensesDashboardPlugin: UiPlugin<ExpensesDashboardProps> = {
    kind: "expenses.dashboard",
    adapt: adaptExpensesDashboardProps,
    Component: ExpensesDashboard,
};
