import type { ChatState, Message } from "../../types/chat";
import { cn } from "../../utils/cn";
import { GenerativeUI } from "../sandbox/GenerativeUI.tsx";
import { useState } from "react";
import { type UiSpec } from "../sandbox/uiRuntime";

function formatTime(ts: number) {
    return new Date(ts).toLocaleTimeString([], { hour: "2-digit", minute: "2-digit" });
}

const mockSpec: UiSpec = {
    version: "1",
    view: {
        kind: "expenses.dashboard",
        title: "Cheltuieli din ultima săptămână",
        props: {
            defaultRange: "last_7_days",
            defaultCategory: "All",
            allowExport: true,
        },
    },
};

export function MessageBubble({ msg }: { msg: Message }) {
    const isUser = msg.role === "user";

    const [mockState,] = useState<ChatState>({
        conversationId: "1",
        status: "idle",
        error: null,
        clientContext: {
            timezone: "test",
            locale: "ro"
        },
        messages: [],
        lastUiSpec: mockSpec
    });
    
    return (
        <div className={cn("flex", isUser ? "justify-end" : "justify-start")}>
            <div className={cn("flex max-w-[90%] gap-3 md:max-w-[78%]", isUser ? "flex-row-reverse" : "flex-row")}>
                <div
                    className={cn(
                        "grid h-9 w-9 shrink-0 place-items-center rounded-full text-xs",
                        isUser
                            ? "bg-zinc-900 text-white dark:bg-white dark:text-zinc-900"
                            : "bg-zinc-200 text-zinc-700 dark:bg-zinc-800 dark:text-zinc-200"
                    )}
                >
                    {isUser ? "You" : "AI"}
                </div>

                <div className="min-w-0">
                    <div
                        className={cn(
                            "whitespace-pre-wrap rounded-2xl px-4 py-3 text-sm leading-relaxed shadow-sm",
                            isUser
                                ? "bg-zinc-900 text-white dark:bg-white dark:text-zinc-900"
                                : "bg-zinc-100 text-zinc-900 dark:bg-zinc-900 dark:text-zinc-100"
                        )}
                    >
                        {msg.content}
                        <GenerativeUI state={mockState} />
                        
                    </div>
                    <div className={cn("mt-1 text-[11px] text-zinc-500 dark:text-zinc-400", isUser ? "text-right" : "text-left")}>
                        {formatTime(msg.createdAt)}
                    </div>
                </div>
            </div>
        </div>
    );
}
