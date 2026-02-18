import type { Message } from "@/types/chat.ts";
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
                    {messages.map((m, idx) => {
                        const isLast = idx === messages.length - 1;
                        return (
                            <MessageBubble
                                key={m.id}
                                msg={m}
                                showTypingDots={isTyping && isLast && m.role === "assistant" && (m.content ?? "") === ""}
                            />
                        );
                    })}
                </div>
            )}
        </div>
    );
}
