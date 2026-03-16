import { getByPath, resolveFromDataKey } from "@/components/sandbox/registry/dataResolver";

export type ActionContext = {
    data?: Record<string, any>;
    item?: Record<string, any> | null;
    form?: Record<string, any> | null;
    value?: unknown;
    extra?: Record<string, any>;
};

const MUSTACHE_FULL_TOKEN = /^\{\{\s*([^}]+?)\s*\}\}$/;
const MUSTACHE_INLINE_TOKEN = /\{\{\s*([^}]+?)\s*\}\}/g;

function resolveNamedToken(token: string, ctx: ActionContext): unknown {
    const raw = token.trim();
    if (!raw) return undefined;

    if (raw.startsWith("$")) {
        return resolveNamedToken(raw.slice(1), ctx);
    }

    if (raw.startsWith("item.")) return getByPath(ctx.item, raw.slice(5));
    if (raw === "item") return ctx.item;

    if (raw.startsWith("form.")) return getByPath(ctx.form, raw.slice(5));
    if (raw === "form") return ctx.form;

    if (raw.startsWith("data.")) return getByPath(ctx.data, raw.slice(5));
    if (raw === "data") return ctx.data;

    if (raw === "value") return ctx.value;

    if (raw.startsWith("extra.")) return getByPath(ctx.extra, raw.slice(6));
    if (raw === "extra") return ctx.extra;

    if (ctx.extra && Object.prototype.hasOwnProperty.call(ctx.extra, raw)) return ctx.extra[raw];
    if (ctx.form && Object.prototype.hasOwnProperty.call(ctx.form, raw)) return ctx.form[raw];
    if (ctx.item && Object.prototype.hasOwnProperty.call(ctx.item, raw)) return ctx.item[raw];
    if (ctx.data && Object.prototype.hasOwnProperty.call(ctx.data, raw)) return ctx.data[raw];

    return raw;
}

function resolveToken(raw: unknown, ctx: ActionContext): unknown {
    if (typeof raw !== "string") return raw;

    const trimmed = raw.trim();
    const fullMustache = MUSTACHE_FULL_TOKEN.exec(trimmed);
    if (fullMustache) {
        const resolved = resolveNamedToken(fullMustache[1], ctx);
        return resolved === undefined ? raw : resolved;
    }

    MUSTACHE_INLINE_TOKEN.lastIndex = 0;
    if (MUSTACHE_INLINE_TOKEN.test(raw)) {
        MUSTACHE_INLINE_TOKEN.lastIndex = 0;
        return raw.replace(MUSTACHE_INLINE_TOKEN, (_, token: string) => {
            const resolved = resolveNamedToken(token, ctx);
            return resolved === undefined || resolved === null ? "" : String(resolved);
        });
    }

    return resolveNamedToken(raw, ctx);
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
            return {
                ...fallbackPayload,
                ...(resolved as Record<string, unknown>),
            };
        }
    }

    const payloadDataKey = String(props.payloadDataKey ?? "").trim();
    if (payloadDataKey && ctx.data) {
        const fromData = resolveFromDataKey(ctx.data, payloadDataKey);
        if (fromData && typeof fromData === "object") {
            return {
                ...fallbackPayload,
                ...(fromData as Record<string, unknown>),
            };
        }
    }

    return fallbackPayload;
}
