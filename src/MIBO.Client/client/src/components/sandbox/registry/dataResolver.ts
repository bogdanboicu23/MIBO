export type AnyRecord = Record<string, any>;

const COMMON_LIST_KEYS = [
    "items",
    "products",
    "rows",
    "data",
    "results",
    "list",
    "categories",
    "transactions",
    "expenses",
    "incomes",
    "kpis",
    "series",
];

export function getByPath(obj: unknown, path: string): unknown {
    if (!path) return undefined;
    const parts = path.split(".").filter(Boolean);
    let cur: any = obj;
    let i = 0;

    while (i < parts.length) {
        if (cur == null) return undefined;

        if (typeof cur === "object") {
            let found = false;
            for (let j = parts.length; j > i; j--) {
                const composite = parts.slice(i, j).join(".");
                if (Object.prototype.hasOwnProperty.call(cur, composite)) {
                    cur = cur[composite];
                    i = j;
                    found = true;
                    break;
                }
            }
            if (found) continue;
        }

        cur = cur?.[parts[i]];
        i++;
    }

    return cur;
}

export function resolveFromDataKey(data: AnyRecord | undefined, dataKey?: string): unknown {
    const key = (dataKey ?? "").trim();
    if (!key || !data) return undefined;

    if (Object.prototype.hasOwnProperty.call(data, key)) return data[key];

    const dotted = getByPath(data, key);
    if (dotted !== undefined) return dotted;

    const last = key.split(".").filter(Boolean).at(-1);
    if (last && Object.prototype.hasOwnProperty.call(data, last)) return data[last];

    return undefined;
}

export function extractArray(value: unknown, preferredPaths: string[] = []): any[] {
    if (Array.isArray(value)) return value;
    if (!value || typeof value !== "object") return [];

    for (const path of preferredPaths) {
        const v = getByPath(value, path);
        if (Array.isArray(v)) return v;
    }

    for (const k of COMMON_LIST_KEYS) {
        const candidate = (value as AnyRecord)[k];
        if (Array.isArray(candidate)) return candidate;
    }

    const firstArray = Object.values(value as AnyRecord).find(Array.isArray);
    return Array.isArray(firstArray) ? firstArray : [];
}

export function extractObject(value: unknown, preferredPaths: string[] = []): AnyRecord | null {
    if (value && typeof value === "object" && !Array.isArray(value)) return value as AnyRecord;

    for (const path of preferredPaths) {
        const v = getByPath(value, path);
        if (v && typeof v === "object" && !Array.isArray(v)) return v as AnyRecord;
    }

    return null;
}

export function toNumber(value: unknown): number | null {
    const n = Number(value);
    return Number.isFinite(n) ? n : null;
}
