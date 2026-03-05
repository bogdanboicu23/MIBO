import { cn } from "@/utils/cn";

type CalloutType = "info" | "warning" | "tip" | "danger";

const calloutStyles: Record<CalloutType, { border: string; bg: string; icon: string; title: string; iconBg: string }> = {
    info: {
        border: "border-blue-300 dark:border-blue-700",
        bg: "bg-blue-50 dark:bg-blue-950/40",
        iconBg: "bg-blue-100 dark:bg-blue-900/50",
        icon: "💡",
        title: "Info",
    },
    warning: {
        border: "border-amber-300 dark:border-amber-700",
        bg: "bg-amber-50 dark:bg-amber-950/40",
        iconBg: "bg-amber-100 dark:bg-amber-900/50",
        icon: "⚠️",
        title: "Warning",
    },
    tip: {
        border: "border-emerald-300 dark:border-emerald-700",
        bg: "bg-emerald-50 dark:bg-emerald-950/40",
        iconBg: "bg-emerald-100 dark:bg-emerald-900/50",
        icon: "✅",
        title: "Tip",
    },
    danger: {
        border: "border-red-300 dark:border-red-700",
        bg: "bg-red-50 dark:bg-red-950/40",
        iconBg: "bg-red-100 dark:bg-red-900/50",
        icon: "🚨",
        title: "Danger",
    },
};

export function Callout({
    type = "info",
    title,
    children,
}: {
    type?: CalloutType;
    title?: string;
    children: React.ReactNode;
}) {
    const style = calloutStyles[type];

    return (
        <div className={cn("not-prose my-6 rounded-xl border-l-4 px-5 py-4", style.border, style.bg)}>
            <div className="mb-2 flex items-center gap-2.5">
                <span className={cn("inline-flex h-6 w-6 items-center justify-center rounded-md text-sm", style.iconBg)}>
                    {style.icon}
                </span>
                <span className="text-[14px] font-bold text-zinc-900 dark:text-zinc-100">
                    {title ?? style.title}
                </span>
            </div>
            <div className="text-[14px] leading-relaxed text-zinc-700 dark:text-zinc-300">{children}</div>
        </div>
    );
}
