import { cn } from "@/utils/cn";

interface FeatureCardProps {
    title: string;
    description: string;
    icon?: string;
    className?: string;
}

export function FeatureCard({ title, description, icon, className }: FeatureCardProps) {
    return (
        <div className={cn(
            "rounded-xl border border-zinc-200 bg-zinc-50 p-5 transition-colors",
            "dark:border-zinc-800 dark:bg-zinc-900/50",
            className
        )}>
            {icon && <div className="mb-3 text-2xl">{icon}</div>}
            <h4 className="mb-1.5 text-[15px] font-semibold text-zinc-900 dark:text-zinc-100">{title}</h4>
            <p className="text-[13px] leading-relaxed text-zinc-600 dark:text-zinc-400">{description}</p>
        </div>
    );
}

interface FeatureGridProps {
    columns?: 2 | 3;
    children: React.ReactNode;
}

export function FeatureGrid({ columns = 2, children }: FeatureGridProps) {
    return (
        <div className={cn(
            "not-prose my-6 grid gap-4",
            columns === 2 ? "grid-cols-1 sm:grid-cols-2" : "grid-cols-1 sm:grid-cols-2 lg:grid-cols-3"
        )}>
            {children}
        </div>
    );
}
