// src/uiRuntime/applyBindings.ts
import type { UiV1, UiNode } from "./UiRenderer";

function getNodeByPath(root: any, path: string): any {
    const parts = path.split("/").filter(Boolean);
    let cur: any = { root };
    for (const p of parts) {
        if (p === "root") cur = cur.root;
        else if (/^\d+$/.test(p)) cur = cur[Number(p)];
        else cur = cur?.[p];
    }
    return cur;
}

export function applyBindings(ui: UiV1): UiV1 {
    if (!ui.bindings?.length) return ui;

    const next: UiV1 = structuredClone(ui);

    for (const b of ui.bindings) {
        const node = getNodeByPath(next.root, b.componentPath) as UiNode | undefined;
        if (!node || node.type !== "component") continue;

        // ✅ IMPORTANT: don't overwrite if "from" is missing
        if (!Object.prototype.hasOwnProperty.call(next.data ?? {}, b.from)) continue;

        const fromVal = (next.data ?? {})[b.from];
        node.props = { ...(node.props ?? {}), [b.prop]: fromVal };
    }

    return next;
}