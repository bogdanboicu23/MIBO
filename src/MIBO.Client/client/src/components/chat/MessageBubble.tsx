import type { Message } from "@/types/chat.ts";
import { cn } from "@/utils/cn.ts";
import { applyBindings } from "@/components/sandbox/uiRuntime/applyBindings.ts";
import { GenerativeUI } from "@/components/sandbox/GenerativeUI.tsx";
import ReactMarkdown from "react-markdown";
import remarkGfm from "remark-gfm";


function formatTime(ts: number) {
    return new Date(ts).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
}

function Avatar({ label, isUser }: { label: string; isUser: boolean }) {
    return (
        <div
            className={cn(
                "grid h-9 w-9 place-items-center rounded-full text-[11px] font-medium",
                isUser
                    ? "bg-zinc-900 text-white dark:bg-white dark:text-zinc-900"
                    : "bg-zinc-200 text-zinc-700 dark:bg-zinc-800 dark:text-zinc-200"
            )}
            aria-label={label}
            title={label}
        >
            {label}
        </div>
    );
}

function TypingDots() {
    return (
        <span className="inline-flex items-center gap-1">
      <span className="h-2 w-2 animate-bounce rounded-full bg-zinc-400 [animation-delay:-0.2s]" />
      <span className="h-2 w-2 animate-bounce rounded-full bg-zinc-400 [animation-delay:-0.1s]" />
      <span className="h-2 w-2 animate-bounce rounded-full bg-zinc-400" />
    </span>
    );
}

function StatusLabel({ content }: { content: string }) {
    return (
        <span className="inline-flex flex-col items-start gap-2 text-zinc-600 dark:text-zinc-300">
            <span className="inline-flex items-center gap-1">
                <span className="h-2 w-2 animate-bounce rounded-full bg-zinc-400 [animation-delay:-0.2s]" />
                <span className="h-2 w-2 animate-bounce rounded-full bg-zinc-400 [animation-delay:-0.1s]" />
                <span className="h-2 w-2 animate-bounce rounded-full bg-zinc-400" />
            </span>
            <span className="text-xs text-zinc-500 dark:text-zinc-400">{content}</span>
        </span>
    );
}

function AssistantText({ content }: { content: string }) {
    return (
        <div className="[&>p]:my-1 [&>p:first-child]:mt-0 [&>p:last-child]:mb-0 [&_ul]:my-1 [&_ul]:list-disc [&_ul]:pl-5 [&_ol]:my-1 [&_ol]:list-decimal [&_ol]:pl-5 [&_li]:my-0.5 [&_h1]:my-1 [&_h2]:my-1 [&_h3]:my-1 [&_pre]:my-1 [&_pre]:overflow-x-auto [&_pre]:rounded-lg [&_pre]:bg-zinc-200/60 [&_pre]:p-2 [&_code]:rounded [&_code]:bg-zinc-200/60 [&_code]:px-1 [&_code]:py-0.5 dark:[&_pre]:bg-zinc-800/80 dark:[&_code]:bg-zinc-800/80">
            <ReactMarkdown remarkPlugins={[remarkGfm]}>
                {content}
            </ReactMarkdown>
        </div>
    );
}

export function MessageBubble({ msg, showTypingDots = false, onUiAction, }: {
    msg: Message;
    showTypingDots?: boolean;
    onUiAction?: (type: string, payload: Record<string, unknown>) => void;
}) {
    const isUser = msg.role === "user";

    // bubble styles:
    const bubbleClass = cn(
        "whitespace-pre-wrap rounded-2xl px-4 py-3 text-sm leading-relaxed shadow-sm break-words",
        isUser
            ? "ml-auto max-w-[88%] sm:max-w-[76%] lg:max-w-[62%] bg-zinc-900 text-white dark:bg-white dark:text-zinc-900"
            : "w-full bg-zinc-100 text-zinc-900 dark:bg-zinc-900 dark:text-zinc-100"
    );

    // UI exists only for assistant messages
    const rawUi = !isUser ? (msg as any).uiV1 : null;
    const ui = rawUi
        ? (() => {
            try {
                return applyBindings(rawUi);
            } catch {
                return rawUi;
            }
        })()
        : null;
    const status = !isUser ? (msg.status ?? "").trim() : "";
    const renderTextBubbleFromUi = ui?.meta?.renderTextBubble;
    const shouldRenderTextBubble =
        isUser
        || Boolean(status)
        || showTypingDots
        || renderTextBubbleFromUi !== false && (((msg.content ?? "").trim().length > 0) || !ui);

    return (
        <div className="w-full">
            <div className="grid w-full grid-cols-[36px_1fr_36px] items-start gap-3">
                {/* Left avatar slot (AI) */}
                <div className={cn(isUser ? "invisible" : "visible")}>
                    <Avatar label="AI" isUser={false} />
                </div>

                {/* Center bubble */}
                <div className="min-w-0 w-full">
                    {shouldRenderTextBubble ? (
                        <div className={bubbleClass}>
                            {!isUser && status ? (
                                <StatusLabel content={status} />
                            ) : !isUser && showTypingDots ? (
                                <TypingDots />
                            ) : isUser ? (
                                <>{msg.content}</>
                            ) : (
                                <AssistantText content={msg.content ?? ""} />
                            )}
                        </div>
                    ) : null}

                    {/* ✅ Generative UI INSIDE assistant message bubble (persistent in history) */}
                    {!isUser && ui ? (
                            <GenerativeUI
                                activeUi={ui}
                                handleAction={(type, payload) => onUiAction?.(type, payload)}
                            />
                    ) : null}

                    <div
                        className={cn(
                            "mt-1 text-[11px] text-zinc-500 dark:text-zinc-400",
                            isUser ? "text-right" : "text-left"
                        )}
                    >
                        {formatTime(msg.createdAt)}
                    </div>
                </div>

                {/* Right avatar slot (User) */}
                <div className={cn(isUser ? "visible" : "invisible")}>
                    <Avatar label="You" isUser={true} />
                </div>
            </div>
        </div>
    );
}
