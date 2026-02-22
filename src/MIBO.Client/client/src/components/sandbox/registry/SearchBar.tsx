import { useEffect, useState } from "react";
import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";

export function SearchBar({ props, onAction }: UiComponentProps) {
    const placeholder = String((props as any)?.placeholder ?? "Search products...");
    const actionType = String((props as any)?.actionType ?? "shop.search").trim() || "shop.search";
    const initialValue = String((props as any)?.initialValue ?? "");

    const [q, setQ] = useState(initialValue);

    useEffect(() => setQ(initialValue), [initialValue]);

    const submit = () => {
        const query = q.trim();
        if (!query) return;
        onAction?.(actionType, { query });
    };

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-4 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
            <div className="text-sm font-semibold">Search</div>

            <div className="mt-3 flex gap-2">
                <input
                    value={q}
                    onChange={(e) => setQ(e.target.value)}
                    onKeyDown={(e) => {
                        if (e.key === "Enter") submit();
                    }}
                    placeholder={placeholder}
                    className="w-full rounded-xl border border-zinc-300 bg-white px-3 py-2 text-sm outline-none focus:ring-2 focus:ring-zinc-300 dark:border-zinc-700 dark:bg-zinc-950 dark:focus:ring-zinc-700"
                />
                <button
                    type="button"
                    onClick={submit}
                    className="rounded-xl border border-zinc-300 bg-white px-4 py-2 text-sm font-medium hover:bg-zinc-50 dark:border-zinc-700 dark:bg-zinc-950 dark:hover:bg-zinc-900"
                >
                    Go
                </button>
            </div>

            <div className="mt-2 text-xs opacity-60">Tip: enter pentru căutare.</div>
        </div>
    );
}