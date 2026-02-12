import type { AdaptResult } from "../../core/uiRuntime";
import type { ExpensesDashboardProps, Category } from "./expenses.types";
import { CATEGORIES } from "./expenses.types";

function isObject(x: unknown): x is Record<string, unknown> {
    return typeof x === "object" && x !== null && !Array.isArray(x);
}

function isCategory(x: unknown): x is Category {
    return typeof x === "string" && (CATEGORIES as string[]).includes(x);
}

export function adaptExpensesDashboardProps(rawProps: unknown): AdaptResult<ExpensesDashboardProps> {
    // props sunt opționale; dacă lipsesc, folosim default-uri.
    if (rawProps === undefined || rawProps === null) {
        return { ok: true, props: { defaultRange: "last_month", defaultCategory: "All", allowExport: true } };
    }

    if (!isObject(rawProps)) {
        return { ok: false, error: "expenses.dashboard props must be an object." };
    }

    const defaultRangeRaw = rawProps["defaultRange"];
    const defaultRange =
        defaultRangeRaw === "last_month" || defaultRangeRaw === "last_7_days" || defaultRangeRaw === "custom"
            ? defaultRangeRaw
            : "last_month";

    const defaultCategoryRaw = rawProps["defaultCategory"];
    const defaultCategory =
        defaultCategoryRaw === "All" ? "All" : isCategory(defaultCategoryRaw) ? defaultCategoryRaw : "All";

    const allowExportRaw = rawProps["allowExport"];
    const allowExport = typeof allowExportRaw === "boolean" ? allowExportRaw : true;

    const titleRaw = rawProps["title"];
    const title = typeof titleRaw === "string" ? titleRaw : undefined;

    return {
        ok: true,
        props: { title, defaultRange, defaultCategory, allowExport },
    };
}
