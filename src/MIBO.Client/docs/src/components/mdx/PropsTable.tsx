interface PropDef {
    name: string;
    type: string;
    default?: string;
    description: string;
}

export function PropsTable({ props }: { props: PropDef[] }) {
    return (
        <div className="my-4 overflow-x-auto rounded-xl border border-zinc-200 dark:border-zinc-800">
            <table className="min-w-full text-sm">
                <thead>
                    <tr className="border-b border-zinc-200 bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-900">
                        <th className="px-4 py-2 text-left text-xs font-semibold text-zinc-600 dark:text-zinc-400">Prop</th>
                        <th className="px-4 py-2 text-left text-xs font-semibold text-zinc-600 dark:text-zinc-400">Type</th>
                        <th className="px-4 py-2 text-left text-xs font-semibold text-zinc-600 dark:text-zinc-400">Default</th>
                        <th className="px-4 py-2 text-left text-xs font-semibold text-zinc-600 dark:text-zinc-400">Description</th>
                    </tr>
                </thead>
                <tbody>
                    {props.map((p) => (
                        <tr key={p.name} className="border-b border-zinc-100 dark:border-zinc-800/50">
                            <td className="px-4 py-2 font-mono text-xs font-medium text-zinc-900 dark:text-zinc-100">{p.name}</td>
                            <td className="px-4 py-2 font-mono text-xs text-zinc-600 dark:text-zinc-400">{p.type}</td>
                            <td className="px-4 py-2 font-mono text-xs text-zinc-500">{p.default ?? "—"}</td>
                            <td className="px-4 py-2 text-xs text-zinc-600 dark:text-zinc-400">{p.description}</td>
                        </tr>
                    ))}
                </tbody>
            </table>
        </div>
    );
}
