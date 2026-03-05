import { Link } from "react-router-dom";
import { cn } from "@/utils/cn";

interface LinkCardProps {
    title: string;
    description: string;
    href: string;
    icon?: string;
    className?: string;
}

export function LinkCard({ title, description, href, icon, className }: LinkCardProps) {
    return (
        <Link
            to={href}
            className={cn(
                "not-prose group block rounded-xl border border-zinc-200 bg-white p-5 no-underline transition-all hover:border-zinc-300 hover:shadow-sm",
                "dark:border-zinc-800 dark:bg-zinc-900/50 dark:hover:border-zinc-700",
                className
            )}
        >
            <div className="flex items-start gap-3">
                {icon && <span className="mt-0.5 text-xl">{icon}</span>}
                <div>
                    <h4 className="mb-1 text-[15px] font-semibold text-zinc-900 group-hover:text-zinc-950 dark:text-zinc-100 dark:group-hover:text-white">
                        {title}
                        <span className="ml-1.5 inline-block text-zinc-400 transition-transform group-hover:translate-x-0.5 dark:text-zinc-500">&rarr;</span>
                    </h4>
                    <p className="text-[13px] leading-relaxed text-zinc-500 dark:text-zinc-400">{description}</p>
                </div>
            </div>
        </Link>
    );
}

interface LinkCardGridProps {
    children: React.ReactNode;
}

export function LinkCardGrid({ children }: LinkCardGridProps) {
    return (
        <div className="not-prose my-6 grid grid-cols-1 gap-3 sm:grid-cols-2">
            {children}
        </div>
    );
}
