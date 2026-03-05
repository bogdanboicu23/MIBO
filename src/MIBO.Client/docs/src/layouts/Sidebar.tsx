import { useState } from "react";
import { Link, useLocation } from "react-router-dom";
import { cn } from "@/utils/cn";
import type { NavItem } from "@/config/navigation";
import { navItems } from "@/config/navigation";

function NavSection({ item, depth = 0 }: { item: NavItem; depth?: number }) {
    const location = useLocation();
    const isActive = item.slug ? location.pathname === item.slug : false;
    const isOpen = item.slug ? location.pathname.startsWith(item.slug) : item.children?.some((c) => c.slug && location.pathname.startsWith(c.slug));
    const [expanded, setExpanded] = useState(isOpen ?? false);
    const hasChildren = item.children && item.children.length > 0;

    if (hasChildren) {
        return (
            <div>
                <button
                    onClick={() => setExpanded(!expanded)}
                    className={cn(
                        "flex w-full items-center justify-between rounded-lg px-3 py-1.5 text-sm transition",
                        depth === 0 ? "font-semibold text-zinc-900 dark:text-zinc-100" : "text-zinc-600 dark:text-zinc-400",
                        "hover:bg-zinc-100 dark:hover:bg-zinc-800"
                    )}
                >
                    {item.slug ? (
                        <Link to={item.slug} className="flex-1 text-left" onClick={(e) => e.stopPropagation()}>
                            {item.title}
                        </Link>
                    ) : (
                        <span>{item.title}</span>
                    )}
                    <svg
                        className={cn("h-4 w-4 transition-transform", expanded && "rotate-90")}
                        fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}
                    >
                        <path strokeLinecap="round" strokeLinejoin="round" d="M9 5l7 7-7 7" />
                    </svg>
                </button>
                {expanded && (
                    <div className="ml-3 mt-1 space-y-0.5 border-l border-zinc-200 pl-3 dark:border-zinc-800">
                        {item.children!.map((child) => (
                            <NavSection key={child.slug ?? child.title} item={child} depth={depth + 1} />
                        ))}
                    </div>
                )}
            </div>
        );
    }

    if (!item.slug) return null;

    return (
        <Link
            to={item.slug}
            className={cn(
                "block rounded-lg px-3 py-1.5 text-sm transition",
                isActive
                    ? "bg-zinc-100 font-medium text-zinc-900 dark:bg-zinc-800 dark:text-zinc-100"
                    : "text-zinc-600 hover:bg-zinc-100 hover:text-zinc-900 dark:text-zinc-400 dark:hover:bg-zinc-800 dark:hover:text-zinc-100"
            )}
        >
            {item.title}
        </Link>
    );
}

export function Sidebar() {
    return (
        <aside className="sticky top-14 hidden h-[calc(100vh-3.5rem)] w-64 shrink-0 overflow-y-auto border-r border-zinc-200 p-4 lg:block dark:border-zinc-800">
            <nav className="space-y-1">
                {navItems.map((item) => (
                    <NavSection key={item.slug ?? item.title} item={item} />
                ))}
            </nav>
        </aside>
    );
}
