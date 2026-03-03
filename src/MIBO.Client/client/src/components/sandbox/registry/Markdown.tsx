import React, { useEffect, useMemo, useState } from "react";
import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";
import { codeToHtml } from "shiki";

type ShikiTheme = "material-theme-ocean" | "github-light";

function getByPath(obj: unknown, path: string): unknown {
    if (!path) return undefined;
    const parts = path.split(".").filter(Boolean);
    let cur: any = obj;
    for (const k of parts) {
        if (cur == null) return undefined;
        cur = cur[k];
    }
    return cur;
}

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

function InlineCode({ children }: { children: React.ReactNode }) {
    return (
        <code className="rounded-md border border-zinc-200 bg-zinc-50 px-1.5 py-0.5 font-mono text-[0.9em] text-zinc-900 dark:border-white/10 dark:bg-white/5 dark:text-zinc-100">
            {children}
        </code>
    );
}

function ShikiCodeBlock({
                            code,
                            lang,
                            theme,
                            dark,
                        }: {
    code: string;
    lang: string;
    theme: ShikiTheme;
    dark: boolean;
}) {
    const [html, setHtml] = useState<string>("");

    useEffect(() => {
        let alive = true;

        (async () => {
            const out = await codeToHtml(code, {
                lang: lang || "text",
                theme,
                // 🔥 eliminate inline background from Shiki
                transformers: [
                    {
                        pre(node) {
                            node.properties.style = "";
                        },
                    },
                ],
            });

            if (alive) setHtml(out);
        })();

        return () => {
            alive = false;
        };
    }, [code, lang, theme]);

    return (
        <div
            className="my-4 overflow-hidden rounded-2xl border shadow-sm"
            style={{
                backgroundColor: dark ? "#0F111A" : "#ffffff",
                borderColor: dark
                    ? "rgba(255,255,255,0.10)"
                    : "rgba(24,24,27,0.10)",
            }}
        >
            <div
                className="flex items-center justify-between border-b px-3 py-2"
                style={{
                    backgroundColor: dark
                        ? "rgba(255,255,255,0.03)"
                        : "rgba(24,24,27,0.03)",
                    borderColor: dark
                        ? "rgba(255,255,255,0.10)"
                        : "rgba(24,24,27,0.10)",
                }}
            >
                <span className="text-xs text-zinc-400">
                    {lang || "text"}
                </span>
                <CopyButton text={code} />
            </div>

            <div className="overflow-x-auto p-3 text-sm leading-6">
                <div
                    className="[&>pre]:m-0 [&>pre]:p-0 [&>pre]:bg-transparent [&_code]:font-mono [&_code]:text-[13px]"
                    dangerouslySetInnerHTML={{ __html: html }}
                />
            </div>
        </div>
    );
}

export function Markdown(uiProps: UiComponentProps) {
    const p = (uiProps.props ?? {}) as Record<string, unknown>;
    const data = (uiProps.data ?? {}) as Record<string, unknown>;

    const [dark, setDark] = useState<boolean>(isDarkMode());

    useEffect(() => {
        if (typeof document === "undefined") return;
        const el = document.documentElement;
        const obs = new MutationObserver(() =>
            setDark(el.classList.contains("dark"))
        );
        obs.observe(el, { attributes: true, attributeFilter: ["class"] });
        return () => obs.disconnect();
    }, []);

    const content = useMemo(() => {
        if (typeof p.content === "string") return p.content;

        if (typeof p.dataKey === "string") {
            const v = getByPath(data, p.dataKey) ?? (data as any)[p.dataKey];
            if (typeof v === "string") return v;
        }

        return "";
    }, [p, data]);

    if (!content) return null;

    const theme: ShikiTheme = dark
        ? "material-theme-ocean"
        : "github-light";

    const isBlockCode = (className: unknown, code: string) => {
        const hasLang =
            typeof className === "string" &&
            className.includes("language-");
        return hasLang || code.includes("\n");
    };

    const extractLang = (className: unknown) => {
        if (typeof className !== "string") return "text";
        const m = /language-([A-Za-z0-9_-]+)/.exec(className);
        return m?.[1] ?? "text";
    };

    return (
        <div className="!text-left prose max-w-none prose-zinc dark:prose-invert prose-pre:p-0 prose-pre:bg-transparent">
            <ReactMarkdown
                remarkPlugins={[remarkGfm]}
                components={{
                    code({ className, children }) {
                        const raw = String(children ?? "");
                        const code = raw.replace(/\n$/, "");

                        if (isBlockCode(className, code)) {
                            return (
                                <ShikiCodeBlock
                                    code={code}
                                    lang={extractLang(className)}
                                    theme={theme}
                                    dark={dark}
                                />
                            );
                        }

                        return <InlineCode>{children}</InlineCode>;
                    },
                }}
            >
                {content}
            </ReactMarkdown>
        </div>
    );
}