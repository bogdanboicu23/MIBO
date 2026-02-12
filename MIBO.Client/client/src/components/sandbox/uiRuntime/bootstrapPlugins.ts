import { registerPlugin } from "./Registry";
import { expensesDashboardPlugin } from "../../../features/expenses/expenses.plugin.ts";

export function bootstrapPlugins() {
    registerPlugin(expensesDashboardPlugin);
}
