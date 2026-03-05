import { cn } from "@/utils/cn";

interface SectionDividerProps {
    className?: string;
}

export function SectionDivider({ className }: SectionDividerProps) {
    return (
        <div className={cn("not-prose my-10 flex items-center gap-4", className)}>
            <div className="h-px flex-1 bg-gradient-to-r from-transparent via-zinc-300 to-transparent dark:via-zinc-700" />
        </div>
    );
}

interface SectionHeaderProps {
    title: string;
    description?: string;
    className?: string;
}

export function SectionHeader({ title, description, className }: SectionHeaderProps) {
    return (
        <div className={cn("not-prose mb-6 mt-12 first:mt-0", className)}>
            <h2 className="text-xl font-bold tracking-tight text-zinc-900 dark:text-zinc-100">{title}</h2>
            {description && (
                <p className="mt-1.5 text-[15px] text-zinc-500 dark:text-zinc-400">{description}</p>
            )}
            <div className="mt-4 h-px bg-gradient-to-r from-zinc-200 to-transparent dark:from-zinc-800" />
        </div>
    );
}
