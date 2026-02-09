export function EmptyState({ onPick }: { onPick: (text: string) => void }) {
    const examples = [
        "Ajută-mă să fac un plan pentru un agent AI cu tools.",
        "Generează un prompt de sistem pentru agentul meu.",
        "Cum conectez UI-ul la un backend cu streaming?",
    ];

    return (
        <div className="mx-auto mt-16 max-w-xl text-center">
            <div className="text-2xl font-semibold">Welcome 👋</div>
            <div className="mt-2 text-sm text-zinc-500 dark:text-zinc-400">
                Alege un exemplu sau scrie un mesaj mai jos.
            </div>

            <div className="mt-6 grid gap-2">
                {examples.map((e) => (
                    <button
                        key={e}
                        onClick={() => onPick(e)}
                        className="rounded-2xl border border-zinc-200 bg-white px-4 py-3 text-left text-sm hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-900 dark:hover:bg-zinc-800"
                    >
                        {e}
                    </button>
                ))}
            </div>
        </div>
    );
}
