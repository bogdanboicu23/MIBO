import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";

export function PageTitle({ props }: UiComponentProps) {
    const text = String(props?.text ?? "Title");
    const subtitle = String(props?.subtitle ?? "");

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
            <div className="text-lg font-semibold">{text}</div>
            {subtitle ? <div className="mt-1 text-sm opacity-70">{subtitle}</div> : null}
        </div>
    );
}