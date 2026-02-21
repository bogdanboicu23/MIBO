import { useEffect, useState } from "react";
import { Sidebar } from "../components/chat/Sidebar";
import { ChatHeader } from "../components/chat/ChatHeader";
import { MessageList } from "../components/chat/MessageList";
import { Composer } from "../components/chat/Composer";
import { useChat } from "../hooks/useChat";
import { useMediaQuery } from "../hooks/useMediaQuery";
import { EmptyState } from "../sections/chat/EmptyState";
import { useUiHub } from "@/realtime/useUiHub.ts";
// import { applyBindings } from "@/components/sandbox/uiRuntime/applyBindings.ts";

// UI runtime

export default function ChatPage() {
    const isDesktop = useMediaQuery("(min-width: 768px)");
    const [sidebarOpen, setSidebarOpen] = useState(false);

    const chat = useChat();

    // Realtime UI patches (SignalR UiHub -> "UiPatch")
    useUiHub({
        conversationId: chat.activeId,
        onPatch: (patch) => chat.applyPatchFromRealtime(patch),
    });

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

    //TODO: Check this part
    // // Apply bindings (optional, but recommended) before render
    // const activeUi = useMemo(() => {
    //     const ui = (chat.active as any)?.uiV1 ?? null;
    //     if (!ui) return null;
    //     try {
    //         return applyBindings(ui);
    //     } catch {
    //         // if bindings are malformed, render raw UI
    //         return ui;
    //     }
    // }, [chat.active?.uiV1]);

    return (
        <div className="flex h-screen w-screen overflow-hidden">
            {/* Desktop sidebar */}
            <aside className="hidden w-[300px] shrink-0 border-r border-zinc-200/70 bg-zinc-50 dark:border-zinc-800/70 dark:bg-zinc-950 md:block">
                <Sidebar
                    conversations={chat.conversations}
                    activeId={chat.activeId}
                    onNewChat={chat.newChat}
                    onSelect={chat.selectConversation}
                    onRename={chat.renameConversation}
                    onDelete={chat.deleteConversation}
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
                                chat.newChat();
                                setSidebarOpen(false);
                            }}
                            onSelect={(id) => {
                                chat.selectConversation(id);
                                setSidebarOpen(false);
                            }}
                            onRename={chat.renameConversation}
                            onDelete={chat.deleteConversation}
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
                    onNewChat={chat.newChat}
                    onOpenSidebarMobile={() => setSidebarOpen(true)}
                />

                <div className="flex min-h-0 flex-1 justify-center">
                    <div className="flex w-full max-w-5xl min-w-0 flex-1 flex-col">
                        <MessageList
                            messages={activeMessages}
                            isTyping={chat.isTyping}
                            listRef={chat.listRef}
                            empty={<EmptyState onPick={(t) => void chat.send(t)} />}
                        />

                        {/*/!* Generative UI area (single UI instance per conversation) *!/*/}
                        {/*{activeUi ? (*/}
                        {/*    <div className="mx-4 mb-3 rounded-2xl border border-zinc-200/70 bg-white p-3 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">*/}
                        {/*        <UiRenderer*/}
                        {/*            ui={activeUi}*/}
                        {/*            onAction={(type, payload) => chat.sendAction(type, payload)}*/}
                        {/*        />*/}
                        {/*    </div>*/}
                        {/*) : null}*/}
                    </div>
                </div>

                <Composer onSend={chat.send} disabled={chat.isTyping} />
            </main>
        </div>
    );
}