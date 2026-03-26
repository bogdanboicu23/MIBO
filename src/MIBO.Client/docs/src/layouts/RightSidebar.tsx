import { cn } from "@/utils/cn";
import { useTableOfContents } from "@/hooks/useTableOfContents";

export function RightSidebar() {
    const { headings, activeId } = useTableOfContents();

    if (headings.length === 0) return null;

    return (
        <aside className="sticky top-14 hidden h-[calc(100vh-3.5rem)] w-60 shrink-0 overflow-y-auto p-4 xl:block">
            <div className="rounded-2xl border border-zinc-200 bg-white/70 p-4 shadow-sm backdrop-blur dark:border-zinc-800 dark:bg-zinc-950/60">
                <h4 className="mb-3 text-xs font-semibold uppercase tracking-[0.16em] text-zinc-500 dark:text-zinc-400">
                    On This Page
                </h4>
                <nav aria-label="On this page" className="space-y-1 border-l border-zinc-200 pl-3 dark:border-zinc-800">
                    {headings.map((h) => (
                        <a
                            key={h.id}
                            href={`#${h.id}`}
                            className={cn(
                                "block text-sm leading-6 transition",
                                h.level === 3 && "pl-3 text-[0.92rem]",
                                activeId === h.id
                                    ? "font-medium text-zinc-900 dark:text-zinc-100"
                                    : "text-zinc-500 hover:text-zinc-700 dark:text-zinc-400 dark:hover:text-zinc-200"
                            )}
                        >
                            {h.text}
                        </a>
                    ))}
                </nav>
            </div>
        </aside>
    );
}
