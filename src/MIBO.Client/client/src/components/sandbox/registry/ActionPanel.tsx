import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";
import { resolveActionPayload, resolveActionType } from "@/components/sandbox/registry/actionResolver";

type ActionItem = {
    label: string;
    actionId?: string;
    actionType?: string;
    payloadTemplate?: Record<string, unknown>;
    variant?: "primary" | "secondary" | "ghost";
    disabled?: boolean;
};

function normalizeActions(raw: unknown): ActionItem[] {
    if (!Array.isArray(raw)) return [];
    return raw
        .map((x: any) => {
            if (!x) return null;
            if (typeof x === "string") return { label: x, actionId: x, actionType: "ui.action.execute" } as ActionItem;
            return {
                label: String(x.label ?? x.title ?? "Action"),
                actionId: typeof x.actionId === "string" ? x.actionId : undefined,
                actionType: typeof x.actionType === "string" ? x.actionType : undefined,
                payloadTemplate: x.payloadTemplate && typeof x.payloadTemplate === "object" ? x.payloadTemplate : undefined,
                variant: x.variant,
                disabled: x.disabled === true,
            } as ActionItem;
        })
        .filter(Boolean) as ActionItem[];
}

function classForVariant(variant?: ActionItem["variant"]): string {
    if (variant === "primary") {
        return "border-zinc-900 bg-zinc-900 text-white hover:bg-zinc-800 dark:border-white dark:bg-white dark:text-zinc-900 dark:hover:bg-zinc-100";
    }
    if (variant === "secondary") {
        return "border-zinc-300 bg-zinc-100 text-zinc-900 hover:bg-zinc-200 dark:border-zinc-700 dark:bg-zinc-900 dark:text-zinc-100 dark:hover:bg-zinc-800";
    }
    return "border-zinc-300 bg-white text-zinc-900 hover:bg-zinc-50 dark:border-zinc-700 dark:bg-zinc-950 dark:text-zinc-100 dark:hover:bg-zinc-900";
}

export function ActionPanel({ props, data, onAction }: UiComponentProps) {
    const title = String((props as any)?.title ?? "Actions");
    const subtitle = String((props as any)?.subtitle ?? "");
    const defaultActionType = resolveActionType((props as any) ?? {}, "ui.action.execute");
    const actions = normalizeActions((props as any)?.actions);

    if (!actions.length) {
        return (
            <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
                <div className="text-sm font-semibold">{title}</div>
                {subtitle ? <div className="mt-1 text-xs opacity-70">{subtitle}</div> : null}
                <div className="mt-2 text-sm opacity-70">No actions available.</div>
            </div>
        );
    }

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
            <div className="text-sm font-semibold">{title}</div>
            {subtitle ? <div className="mt-1 text-xs opacity-70">{subtitle}</div> : null}

            <div className="mt-3 flex flex-wrap gap-2">
                {actions.map((action, idx) => {
                    const actionType = resolveActionType({ actionType: action.actionType }, defaultActionType);
                    const fallbackPayload = {
                        actionId: action.actionId,
                        source: "actionPanel",
                        index: idx,
                    } as Record<string, unknown>;
                    return (
                        <button
                            key={`${action.label}-${idx}`}
                            type="button"
                            disabled={action.disabled}
                            onClick={() => {
                                const payload = resolveActionPayload(
                                    { payloadTemplate: action.payloadTemplate },
                                    fallbackPayload,
                                    { data: (data ?? {}) as Record<string, any>, extra: fallbackPayload }
                                );
                                onAction?.(actionType, payload);
                            }}
                            className={`rounded-xl border px-3 py-2 text-sm font-medium disabled:opacity-40 ${classForVariant(action.variant)}`}
                        >
                            {action.label}
                        </button>
                    );
                })}
            </div>
        </div>
    );
}
