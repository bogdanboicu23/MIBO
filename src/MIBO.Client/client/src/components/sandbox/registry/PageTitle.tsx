import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";
import { extractObject, resolveFromDataKey } from "@/components/sandbox/registry/dataResolver";

export function PageTitle({ props, data }: UiComponentProps) {
    const dataKey = String(props?.dataKey ?? "");
    const sourceRaw = resolveFromDataKey((data ?? {}) as Record<string, any>, dataKey);
    const source = extractObject(sourceRaw) ?? {};

    const textKey = String(props?.textKey ?? "");
    const subtitleKey = String(props?.subtitleKey ?? "");

    const text =
        String(
            props?.text ??
            (textKey ? resolveFromDataKey(source, textKey) : undefined) ??
            source.text ??
            source.title ??
            "Title"
        );
    const subtitle =
        String(
            props?.subtitle ??
            (subtitleKey ? resolveFromDataKey(source, subtitleKey) : undefined) ??
            source.subtitle ??
            ""
        );

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
            <div className="text-lg font-semibold">{text}</div>
            {subtitle ? <div className="mt-1 text-sm opacity-70">{subtitle}</div> : null}
        </div>
    );
}
