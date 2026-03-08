import { useMemo } from "react";
import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";
import { resolveFromDataKey } from "@/components/sandbox/registry/dataResolver";

export function JsonViewerCard({ props, data }: UiComponentProps) {
    const title = String((props as any)?.title ?? "Data");
    const dataKey = String((props as any)?.dataKey ?? "").trim();
    const collapsed = (props as any)?.collapsed === true;

    const source = useMemo(() => {
        if ((props as any)?.value !== undefined) return (props as any).value;
        if (dataKey) return resolveFromDataKey((data ?? {}) as Record<string, any>, dataKey);
        return data ?? {};
    }, [props, data, dataKey]);

    const text = useMemo(() => {
        try {
            return JSON.stringify(source ?? null, null, 2);
        } catch {
            return String(source);
        }
    }, [source]);

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
            <div className="text-sm font-semibold">{title}</div>
            <details className="mt-3" open={!collapsed}>
                <summary className="cursor-pointer text-xs opacity-70">Inspect JSON</summary>
                <pre className="mt-2 max-h-96 overflow-auto rounded-xl border border-zinc-200/70 bg-zinc-50 p-3 text-xs dark:border-zinc-800/70 dark:bg-zinc-900">
{text}
                </pre>
            </details>
        </div>
    );
}
