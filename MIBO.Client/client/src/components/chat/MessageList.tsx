import type { Message } from "../../types/chat";
import { MessageBubble } from "./MessageBubble";

export function MessageList(props: {
    messages: Message[];
    isTyping: boolean;
    listRef: React.RefObject<HTMLDivElement>;
    empty?: React.ReactNode;
}) {
    const { messages, isTyping, listRef, empty } = props;

    return (
        <div ref={listRef} className="min-h-0 flex-1 overflow-y-auto px-4 py-6 md:px-6">
            {messages.length === 0 ? (
                empty
            ) : (
                <div className="space-y-4">
                    {messages.map((m) => (
                        <MessageBubble key={m.id} msg={m} />
                    ))}
                    {isTyping && <Typing />}
                </div>
            )}
        </div>
    );
}

function Typing() {
    return (
        <div className="flex justify-start">
            <div className="flex max-w-[90%] gap-3 md:max-w-[78%]">
                <div className="grid h-9 w-9 place-items-center rounded-full bg-zinc-200 text-xs text-zinc-700 dark:bg-zinc-800 dark:text-zinc-200">
                    AI
                </div>
                <div className="rounded-2xl bg-zinc-100 px-4 py-3 text-sm shadow-sm dark:bg-zinc-900">
          <span className="inline-flex items-center gap-1">
            <span className="h-2 w-2 animate-bounce rounded-full bg-zinc-400 [animation-delay:-0.2s]" />
            <span className="h-2 w-2 animate-bounce rounded-full bg-zinc-400 [animation-delay:-0.1s]" />
            <span className="h-2 w-2 animate-bounce rounded-full bg-zinc-400" />
          </span>
                </div>
            </div>
        </div>
    );
}
