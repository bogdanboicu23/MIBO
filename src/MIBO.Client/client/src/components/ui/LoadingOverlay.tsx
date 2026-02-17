type LoadingOverlayProps = {
    show: boolean;
    fullscreen?: boolean;
    label?: string;
};

export function LoadingOverlay({ show,
                                   fullscreen = false,
                                   label = "Se procesează...",
                               }: LoadingOverlayProps) {
    if (!show) return null;

    return (
        <div
            className={[
                fullscreen ? "fixed inset-0" : "absolute inset-0",
                "z-50 grid place-items-center",
                "bg-black/20 dark:bg-black/40 backdrop-blur-sm",
            ].join(" ")}
            role="status"
            aria-live="polite"
            aria-busy="true"
        >
            <div
                className="flex flex-col items-center gap-3 rounded-xl border border-zinc-200 bg-white px-6 py-4 shadow-lg dark:border-zinc-800 dark:bg-zinc-900">
                <div
                    className="h-8 w-8 animate-spin rounded-full border-2 border-zinc-300 border-t-zinc-900 dark:border-zinc-700 dark:border-t-white" />
                <div className="text-sm font-medium text-zinc-800 dark:text-zinc-100">
                    {label}
                </div>
            </div>
        </div>
    );
}
