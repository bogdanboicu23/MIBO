import type { Message } from "@/types/chat.ts";
import { cn } from "@/utils/cn.ts";
import { applyBindings } from "@/components/sandbox/uiRuntime/applyBindings.ts";
import { GenerativeUI } from "@/components/sandbox/GenerativeUI.tsx";


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

    return (
        <div className="w-full">
            <div className="grid w-full grid-cols-[36px_1fr_36px] items-start gap-3">
                {/* Left avatar slot (AI) */}
                <div className={cn(isUser ? "invisible" : "visible")}>
                    <Avatar label="AI" isUser={false} />
                </div>

                {/* Center bubble */}
                <div className="min-w-0 w-full">
                    <div className={bubbleClass}>
                        {!isUser && showTypingDots ? <TypingDots /> : <>{msg.content}</>}
                    </div>

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