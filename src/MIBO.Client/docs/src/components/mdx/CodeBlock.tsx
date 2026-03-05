import { useEffect, useState } from "react";
import { getHighlighter } from "@/utils/shiki";
import { cn } from "@/utils/cn";

function isDarkMode(): boolean {
    if (typeof document === "undefined") return true;
    return document.documentElement.classList.contains("dark");
}

function CopyButton({ text }: { text: string }) {
    const [copied, setCopied] = useState(false);

    return (
        <button
            type="button"
            onClick={async () => {
                await navigator.clipboard.writeText(text);
                setCopied(true);
                setTimeout(() => setCopied(false), 900);
            }}
            className="rounded-lg border border-zinc-700/60 bg-zinc-900/60 px-2 py-1 text-xs text-zinc-100 hover:bg-zinc-900 dark:border-white/10 dark:bg-white/5"
        >
            {copied ? "Copied" : "Copy"}
        </button>
    );
}

interface CodeBlockProps {
    children: string;
    language?: string;
    title?: string;
    className?: string;
}

export function CodeBlock({ children, language = "typescript", title, className }: CodeBlockProps) {
    const [html, setHtml] = useState("");
    const [dark, setDark] = useState(isDarkMode);

    useEffect(() => {
        if (typeof document === "undefined") return;
        const el = document.documentElement;
        const obs = new MutationObserver(() => setDark(el.classList.contains("dark")));
        obs.observe(el, { attributes: true, attributeFilter: ["class"] });
        return () => obs.disconnect();
    }, []);

    const code = children.trim();
    const theme = dark ? "material-theme-ocean" : "github-light";

    useEffect(() => {
        let cancelled = false;
        getHighlighter().then((highlighter) => {
            if (cancelled) return;
            const result = highlighter.codeToHtml(code, {
                lang: language,
                theme,
                transformers: [
                    {
                        pre(node) {
                            node.properties.style = "";
                        },
                    },
                ],
            });
            setHtml(result);
        });
        return () => { cancelled = true; };
    }, [code, language, theme]);

    return (
        <div
            className={cn("group relative my-4 overflow-hidden rounded-2xl border shadow-sm", className)}
            style={{
                backgroundColor: dark ? "#0F111A" : "#ffffff",
                borderColor: dark ? "rgba(255,255,255,0.10)" : "rgba(24,24,27,0.10)",
            }}
        >
            <div
                className="flex items-center justify-between border-b px-3 py-2"
                style={{
                    backgroundColor: dark ? "rgba(255,255,255,0.03)" : "rgba(24,24,27,0.03)",
                    borderColor: dark ? "rgba(255,255,255,0.10)" : "rgba(24,24,27,0.10)",
                }}
            >
                <span className="text-xs text-zinc-400">
                    {title ?? language}
                </span>
                <CopyButton text={code} />
            </div>

            {html ? (
                <div
                    className="overflow-x-auto p-3 text-sm leading-6 [&>pre]:m-0 [&>pre]:p-0 [&>pre]:bg-transparent [&_code]:font-mono [&_code]:text-[13px]"
                    dangerouslySetInnerHTML={{ __html: html }}
                />
            ) : (
                <pre className="m-0 overflow-x-auto bg-transparent p-3 text-sm leading-6">
                    <code className="font-mono text-[13px]">{code}</code>
                </pre>
            )}
        </div>
    );
}
