export function applyUiPatch(ui: any, patch: any): any {
    const next = structuredClone(ui);

    for (const op of patch?.ops ?? []) {
        const path = String(op.path ?? "");
        const parts = path.split("/").filter(Boolean);
        if (parts.length === 0) continue;

        let cur: any = next;
        for (let i = 0; i < parts.length - 1; i++) {
            const p = parts[i];
            cur[p] ??= {};
            cur = cur[p];
            if (cur == null) break;
        }
        if (cur == null) continue;

        const key = parts[parts.length - 1];

        if (op.op === "set") cur[key] = op.value;
        else if (op.op === "merge") cur[key] = { ...(cur[key] ?? {}), ...(op.value ?? {}) };
        else if (op.op === "remove") delete cur[key];
    }

    return next;
}