import { useEffect, useState } from "react";
import { Sidebar } from "../components/chat/Sidebar";
import { ChatHeader } from "../components/chat/ChatHeader";
import { MessageList } from "../components/chat/MessageList";
import { Composer } from "../components/chat/Composer";
import { useChat } from "../hooks/useChat";
import { useMediaQuery } from "../hooks/useMediaQuery";
import { EmptyState } from "../sections/chat/EmptyState";

export default function ChatPage() {
    const isDesktop = useMediaQuery("(min-width: 768px)");
    const [sidebarOpen, setSidebarOpen] = useState(false);

    const chat = useChat();

    useEffect(() => {
        if (isDesktop) setSidebarOpen(false);
    }, [isDesktop]);

    // autoscroll
    useEffect(() => {
        const el = chat.listRef.current;
        if (!el) return;
        el.scrollTop = el.scrollHeight;
    }, [chat.active?.messages.length, chat.isTyping, chat.active?.uiV1]);

    const activeTitle = chat.active?.title ?? "Chat";
    const activeMessages = chat.active?.messages ?? [];

    return (
        <div className="flex h-screen w-screen overflow-hidden">
            {/* Desktop sidebar */}
            <aside className="hidden w-[300px] shrink-0 border-r border-zinc-200/70 bg-zinc-50 dark:border-zinc-800/70 dark:bg-zinc-950 md:block">
                <Sidebar
                    conversations={chat.conversations}
                    activeId={chat.activeId}
                    onNewChat={() => {
                        void chat.newChat().catch(() => undefined);
                    }}
                    onSelect={chat.selectConversation}
                    onRename={(id, title) => {
                        void chat.renameConversation(id, title).catch(() => undefined);
                    }}
                    onDelete={(id) => {
                        void chat.deleteConversation(id).catch(() => undefined);
                    }}
                />
            </aside>

            {/* Mobile sidebar overlay */}
            {sidebarOpen && (
                <div className="fixed inset-0 z-40 md:hidden">
                    <div
                        className="absolute inset-0 bg-black/40"
                        onClick={() => setSidebarOpen(false)}
                    />
                    <div className="absolute left-0 top-0 h-full w-[86%] max-w-[320px] border-r border-zinc-200 bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-950">
                        <Sidebar
                            conversations={chat.conversations}
                            activeId={chat.activeId}
                            onNewChat={() => {
                                void chat.newChat().catch(() => undefined);
                                setSidebarOpen(false);
                            }}
                            onSelect={(id) => {
                                chat.selectConversation(id);
                                setSidebarOpen(false);
                            }}
                            onRename={(id, title) => {
                                void chat.renameConversation(id, title).catch(() => undefined);
                            }}
                            onDelete={(id) => {
                                void chat.deleteConversation(id).catch(() => undefined);
                            }}
                            mobile
                            onClose={() => setSidebarOpen(false)}
                        />
                    </div>
                </div>
            )}

            {/* Main */}
            <main className="flex min-w-0 flex-1 flex-col">
                <ChatHeader
                    title={activeTitle}
                    model={chat.model}
                    onModelChange={chat.setModel}
                    onNewChat={() => {
                        void chat.newChat().catch(() => undefined);
                    }}
                    onOpenSidebarMobile={() => setSidebarOpen(true)}
                />

                <div className="flex min-h-0 flex-1 justify-center">
                    <div className="flex w-full max-w-5xl min-w-0 flex-1 flex-col">
                        <MessageList
                            messages={activeMessages}
                            isTyping={chat.isTyping}
                            listRef={chat.listRef}
                            onUiAction={(type, payload) => void chat.sendAction(type, payload)}
                            empty={<EmptyState onPick={(t) => void chat.send(t)} />}
                        />
                    </div>
                </div>

                <Composer onSend={chat.send} disabled={chat.isTyping} />
            </main>
        </div>
    );
}
