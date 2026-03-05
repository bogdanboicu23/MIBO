import { useState } from "react";
import { Link, useLocation } from "react-router-dom";
import { cn } from "@/utils/cn";
import type { NavItem } from "@/config/navigation";
import { navItems } from "@/config/navigation";

function MobileNavSection({ item, depth = 0, onClose }: { item: NavItem; depth?: number; onClose: () => void }) {
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
                        "flex w-full items-center justify-between rounded-lg px-3 py-2 text-sm",
                        depth === 0 ? "font-semibold text-zinc-900 dark:text-zinc-100" : "text-zinc-600 dark:text-zinc-400"
                    )}
                >
                    {item.slug ? (
                        <Link to={item.slug} className="flex-1 text-left" onClick={() => onClose()}>
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
                    <div className="ml-3 space-y-0.5 border-l border-zinc-200 pl-3 dark:border-zinc-800">
                        {item.children!.map((child) => (
                            <MobileNavSection key={child.slug ?? child.title} item={child} depth={depth + 1} onClose={onClose} />
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
            onClick={onClose}
            className={cn(
                "block rounded-lg px-3 py-2 text-sm",
                isActive
                    ? "bg-zinc-100 font-medium text-zinc-900 dark:bg-zinc-800 dark:text-zinc-100"
                    : "text-zinc-600 dark:text-zinc-400"
            )}
        >
            {item.title}
        </Link>
    );
}

export function MobileMenu({ open, onClose }: { open: boolean; onClose: () => void }) {
    if (!open) return null;

    return (
        <div className="fixed inset-0 z-50 lg:hidden">
            <div className="fixed inset-0 bg-black/50" onClick={onClose} />
            <div className="fixed inset-y-0 left-0 w-80 overflow-y-auto bg-white p-4 shadow-xl dark:bg-zinc-950">
                <div className="mb-4 flex items-center justify-between">
                    <span className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">Navigation</span>
                    <button onClick={onClose} className="rounded-lg p-1 hover:bg-zinc-100 dark:hover:bg-zinc-800">
                        <svg className="h-5 w-5 text-zinc-500" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                            <path strokeLinecap="round" strokeLinejoin="round" d="M6 18L18 6M6 6l12 12" />
                        </svg>
                    </button>
                </div>
                <nav className="space-y-1">
                    {navItems.map((item) => (
                        <MobileNavSection key={item.slug ?? item.title} item={item} onClose={onClose} />
                    ))}
                </nav>
            </div>
        </div>
    );
}
