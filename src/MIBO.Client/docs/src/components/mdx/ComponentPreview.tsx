import { useState } from "react";
import { cn } from "@/utils/cn";
import { CodeBlock } from "./CodeBlock";

type Tab = "preview" | "code" | "props";

interface ComponentPreviewProps {
    children: React.ReactNode;
    code?: string;
    language?: string;
    propsSlot?: React.ReactNode;
}

export function ComponentPreview({ children, code, language = "tsx", propsSlot }: ComponentPreviewProps) {
    const [tab, setTab] = useState<Tab>("preview");

    const tabs: { id: Tab; label: string; show: boolean }[] = [
        { id: "preview", label: "Preview", show: true },
        { id: "code", label: "Code", show: !!code },
        { id: "props", label: "Props", show: !!propsSlot },
    ];

    return (
        <div className="my-6 overflow-hidden rounded-xl border border-zinc-200 dark:border-zinc-800">
            <div className="flex border-b border-zinc-200 bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-900">
                {tabs.filter((t) => t.show).map((t) => (
                    <button
                        key={t.id}
                        onClick={() => setTab(t.id)}
                        className={cn(
                            "px-4 py-2 text-sm transition",
                            tab === t.id
                                ? "border-b-2 border-zinc-900 font-medium text-zinc-900 dark:border-zinc-100 dark:text-zinc-100"
                                : "text-zinc-500 hover:text-zinc-700 dark:text-zinc-400 dark:hover:text-zinc-200"
                        )}
                    >
                        {t.label}
                    </button>
                ))}
            </div>

            <div className="p-6">
                {tab === "preview" && (
                    <div className="flex min-h-[80px] items-center justify-center gap-4 rounded-lg border border-dashed border-zinc-200 p-6 dark:border-zinc-800">
                        {children}
                    </div>
                )}
                {tab === "code" && code && <CodeBlock language={language}>{code}</CodeBlock>}
                {tab === "props" && propsSlot}
            </div>
        </div>
    );
}
