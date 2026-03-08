import { useEffect, useRef } from "react";
import { useNavigate } from "react-router-dom";
import { useSearch } from "@/hooks/useSearch";

export function SearchDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
    const { query, setQuery, results } = useSearch();
    const inputRef = useRef<HTMLInputElement>(null);
    const navigate = useNavigate();

    useEffect(() => {
        if (open) {
            setQuery("");
            setTimeout(() => inputRef.current?.focus(), 50);
        }
    }, [open, setQuery]);

    if (!open) return null;

    return (
        <div className="fixed inset-0 z-50 flex items-start justify-center pt-[20vh]">
            <div className="fixed inset-0 bg-black/50" onClick={onClose} />
            <div className="relative w-full max-w-lg rounded-2xl border border-zinc-200 bg-white shadow-2xl dark:border-zinc-800 dark:bg-zinc-950">
                <div className="flex items-center gap-3 border-b border-zinc-200 px-4 py-3 dark:border-zinc-800">
                    <svg className="h-5 w-5 text-zinc-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                        <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                    </svg>
                    <input
                        ref={inputRef}
                        value={query}
                        onChange={(e) => setQuery(e.target.value)}
                        placeholder="Search documentation..."
                        className="flex-1 bg-transparent text-sm outline-none text-zinc-900 placeholder:text-zinc-400 dark:text-zinc-100"
                    />
                    <kbd className="rounded bg-zinc-100 px-1.5 py-0.5 text-xs text-zinc-500 dark:bg-zinc-800">ESC</kbd>
                </div>

                {query.trim() && (
                    <div className="max-h-80 overflow-y-auto p-2">
                        {results.length === 0 ? (
                            <p className="px-3 py-6 text-center text-sm text-zinc-500">No results found.</p>
                        ) : (
                            results.map((r) => (
                                <button
                                    key={r.slug}
                                    onClick={() => {
                                        navigate(r.slug);
                                        onClose();
                                    }}
                                    className="flex w-full flex-col rounded-lg px-3 py-2 text-left transition hover:bg-zinc-100 dark:hover:bg-zinc-800"
                                >
                                    <span className="text-sm font-medium text-zinc-900 dark:text-zinc-100">{r.title}</span>
                                    {r.description && (
                                        <span className="text-xs text-zinc-500 dark:text-zinc-400">{r.description}</span>
                                    )}
                                </button>
                            ))
                        )}
                    </div>
                )}
            </div>
        </div>
    );
}
