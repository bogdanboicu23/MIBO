import { useEffect, useMemo, useState } from "react";
import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";
import { extractObject, resolveFromDataKey } from "@/components/sandbox/registry/dataResolver";
import { resolveActionPayload, resolveActionType } from "@/components/sandbox/registry/actionResolver";

type Field = {
    key: string;
    label: string;
    type?: "text" | "number" | "textarea" | "select" | "checkbox" | "date";
    placeholder?: string;
    required?: boolean;
    options?: Array<{ label: string; value: string | number }>;
};

function normalizeFields(raw: unknown): Field[] {
    if (!Array.isArray(raw)) return [];
    return raw
        .map((f: any) => {
            if (!f || typeof f !== "object") return null;
            const key = String(f.key ?? "").trim();
            if (!key) return null;
            return {
                key,
                label: String(f.label ?? key),
                type: f.type,
                placeholder: f.placeholder,
                required: f.required === true,
                options: Array.isArray(f.options)
                    ? f.options.map((o: any) => ({ label: String(o?.label ?? o?.value ?? "Option"), value: o?.value ?? o?.label ?? "" }))
                    : undefined,
            } as Field;
        })
        .filter(Boolean) as Field[];
}

function toInitialValues(fields: Field[], source: Record<string, any>): Record<string, any> {
    const out: Record<string, any> = {};
    for (const field of fields) {
        if (Object.prototype.hasOwnProperty.call(source, field.key)) out[field.key] = source[field.key];
        else out[field.key] = field.type === "checkbox" ? false : "";
    }
    return out;
}

export function FormCard({ props, data, onAction }: UiComponentProps) {
    const title = String((props as any)?.title ?? "Form");
    const subtitle = String((props as any)?.subtitle ?? "");
    const actionId = String((props as any)?.actionId ?? "").trim();
    const actionType = resolveActionType((props as any) ?? {}, actionId ? "ui.action.execute" : "ui.form.submit");

    const fields = useMemo(() => normalizeFields((props as any)?.fields), [props]);

    const initialFromProps = extractObject((props as any)?.initialValues) ?? {};
    const initialDataKey = String((props as any)?.initialDataKey ?? (props as any)?.dataKey ?? "").trim();
    const initialFromData = initialDataKey
        ? extractObject(resolveFromDataKey((data ?? {}) as Record<string, any>, initialDataKey)) ?? {}
        : {};

    const computedInitialValues = useMemo(
        () => toInitialValues(fields, { ...initialFromData, ...initialFromProps }),
        [fields, initialFromData, initialFromProps]
    );

    const [form, setForm] = useState<Record<string, any>>(computedInitialValues);

    useEffect(() => {
        setForm(computedInitialValues);
    }, [computedInitialValues]);

    if (!fields.length) {
        return (
            <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
                <div className="text-sm font-semibold">{title}</div>
                {subtitle ? <div className="mt-1 text-xs opacity-70">{subtitle}</div> : null}
                <div className="mt-2 text-sm opacity-70">No form fields defined.</div>
            </div>
        );
    }

    const submit = () => {
        const missing = fields.filter((f) => f.required && !String(form[f.key] ?? "").trim());
        if (missing.length) return;

        const fallbackPayload = { actionId, values: form, source: "formCard" } as Record<string, unknown>;
        const payload = resolveActionPayload(
            (props as any) ?? {},
            fallbackPayload,
            { data: (data ?? {}) as Record<string, any>, form, extra: fallbackPayload }
        );
        onAction?.(actionType, payload);
    };

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
            <div className="text-sm font-semibold">{title}</div>
            {subtitle ? <div className="mt-1 text-xs opacity-70">{subtitle}</div> : null}

            <div className="mt-3 grid gap-3">
                {fields.map((field) => {
                    const value = form[field.key];
                    const baseClass = "w-full rounded-xl border border-zinc-300 bg-white px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-zinc-300 dark:border-zinc-700 dark:bg-zinc-950 dark:focus:ring-zinc-700";

                    return (
                        <label key={field.key} className="grid gap-1">
                            <span className="text-xs font-medium opacity-80">
                                {field.label}
                                {field.required ? " *" : ""}
                            </span>

                            {field.type === "textarea" ? (
                                <textarea
                                    className={`${baseClass} min-h-24`}
                                    value={String(value ?? "")}
                                    placeholder={field.placeholder}
                                    onChange={(e) => setForm((s) => ({ ...s, [field.key]: e.target.value }))}
                                />
                            ) : field.type === "select" ? (
                                <select
                                    className={baseClass}
                                    value={String(value ?? "")}
                                    onChange={(e) => setForm((s) => ({ ...s, [field.key]: e.target.value }))}
                                >
                                    <option value="">Select...</option>
                                    {(field.options ?? []).map((o) => (
                                        <option key={String(o.value)} value={String(o.value)}>
                                            {o.label}
                                        </option>
                                    ))}
                                </select>
                            ) : field.type === "checkbox" ? (
                                <input
                                    className="h-4 w-4"
                                    type="checkbox"
                                    checked={Boolean(value)}
                                    onChange={(e) => setForm((s) => ({ ...s, [field.key]: e.target.checked }))}
                                />
                            ) : (
                                <input
                                    className={baseClass}
                                    type={field.type === "number" ? "number" : field.type === "date" ? "date" : "text"}
                                    value={String(value ?? "")}
                                    placeholder={field.placeholder}
                                    onChange={(e) => setForm((s) => ({ ...s, [field.key]: e.target.value }))}
                                />
                            )}
                        </label>
                    );
                })}
            </div>

            <div className="mt-4 flex gap-2">
                <button
                    type="button"
                    onClick={submit}
                    className="rounded-xl border border-zinc-900 bg-zinc-900 px-4 py-2 text-sm font-medium text-white hover:bg-zinc-800 dark:border-white dark:bg-white dark:text-zinc-900 dark:hover:bg-zinc-100"
                >
                    Submit
                </button>
                <button
                    type="button"
                    onClick={() => setForm(computedInitialValues)}
                    className="rounded-xl border border-zinc-300 bg-white px-4 py-2 text-sm font-medium hover:bg-zinc-50 dark:border-zinc-700 dark:bg-zinc-950 dark:hover:bg-zinc-900"
                >
                    Reset
                </button>
            </div>
        </div>
    );
}
