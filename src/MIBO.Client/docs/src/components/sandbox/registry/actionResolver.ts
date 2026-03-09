import { getByPath, resolveFromDataKey } from "@/components/sandbox/registry/dataResolver";

export type ActionContext = {
    data?: Record<string, any>;
    item?: Record<string, any> | null;
    form?: Record<string, any> | null;
    value?: unknown;
    extra?: Record<string, any>;
};

function resolveToken(raw: unknown, ctx: ActionContext): unknown {
    if (typeof raw !== "string") return raw;

    if (raw.startsWith("$item.")) return getByPath(ctx.item, raw.slice(6));
    if (raw === "$item") return ctx.item;

    if (raw.startsWith("$form.")) return getByPath(ctx.form, raw.slice(6));
    if (raw === "$form") return ctx.form;

    if (raw.startsWith("$data.")) return getByPath(ctx.data, raw.slice(6));
    if (raw === "$data") return ctx.data;

    if (raw === "$value") return ctx.value;

    if (raw.startsWith("$extra.")) return getByPath(ctx.extra, raw.slice(7));
    if (raw === "$extra") return ctx.extra;

    return raw;
}

export function deepResolveTemplate(template: unknown, ctx: ActionContext): unknown {
    if (template == null) return template;

    if (Array.isArray(template)) {
        return template.map((x) => deepResolveTemplate(x, ctx));
    }

    if (typeof template === "object") {
        const out: Record<string, unknown> = {};
        for (const [k, v] of Object.entries(template as Record<string, unknown>)) {
            out[k] = deepResolveTemplate(v, ctx);
        }
        return out;
    }

    return resolveToken(template, ctx);
}

export function resolveActionType(props: Record<string, any>, fallbackType: string): string {
    const actionType = String(props.actionType ?? fallbackType).trim();
    return actionType || fallbackType;
}

export function resolveActionPayload(
    props: Record<string, any>,
    fallbackPayload: Record<string, unknown>,
    ctx: ActionContext
): Record<string, unknown> {
    const payloadTemplate = props.payloadTemplate;
    if (payloadTemplate && typeof payloadTemplate === "object") {
        const resolved = deepResolveTemplate(payloadTemplate, ctx);
        if (resolved && typeof resolved === "object") {
            return resolved as Record<string, unknown>;
        }
    }

    const payloadDataKey = String(props.payloadDataKey ?? "").trim();
    if (payloadDataKey && ctx.data) {
        const fromData = resolveFromDataKey(ctx.data, payloadDataKey);
        if (fromData && typeof fromData === "object") {
            return fromData as Record<string, unknown>;
        }
    }

    return fallbackPayload;
}
