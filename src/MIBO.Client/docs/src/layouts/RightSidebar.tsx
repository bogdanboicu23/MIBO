import { cn } from "@/utils/cn";
import { useTableOfContents } from "@/hooks/useTableOfContents";

export function RightSidebar() {
    const { headings, activeId } = useTableOfContents();

    if (headings.length === 0) return null;

    return (
        <aside className="sticky top-14 hidden h-[calc(100vh-3.5rem)] w-56 shrink-0 overflow-y-auto p-4 xl:block">
            <h4 className="mb-3 text-xs font-semibold uppercase tracking-wider text-zinc-500 dark:text-zinc-400">
                On this page
            </h4>
            <nav className="space-y-1">
                {headings.map((h) => (
                    <a
                        key={h.id}
                        href={`#${h.id}`}
                        className={cn(
                            "block text-sm transition",
                            h.level === 3 && "pl-3",
                            activeId === h.id
                                ? "font-medium text-zinc-900 dark:text-zinc-100"
                                : "text-zinc-500 hover:text-zinc-700 dark:text-zinc-400 dark:hover:text-zinc-200"
                        )}
                    >
                        {h.text}
                    </a>
                ))}
            </nav>
        </aside>
    );
}
