export function Steps({ children }: { children: React.ReactNode }) {
    return (
        <div className="not-prose my-8 ml-4 border-l-2 border-zinc-200 pl-8 dark:border-zinc-800 [counter-reset:step]">
            {children}
        </div>
    );
}

export function Step({ title, children }: { title: string; children: React.ReactNode }) {
    return (
        <div className="relative mb-10 last:mb-0 [counter-increment:step]">
            <div className="absolute -left-[41px] flex h-8 w-8 items-center justify-center rounded-full border-2 border-zinc-300 bg-white text-xs font-bold text-zinc-700 shadow-sm dark:border-zinc-600 dark:bg-zinc-900 dark:text-zinc-200 before:content-[counter(step)]" />
            <h3 className="mb-2 text-[15px] font-bold text-zinc-900 dark:text-zinc-100">{title}</h3>
            <div className="text-[14px] leading-relaxed text-zinc-600 dark:text-zinc-400">{children}</div>
        </div>
    );
}
