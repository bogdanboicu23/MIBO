import { useState } from "react";
import type { Conversation } from "../../types/chat";
import { cn } from "../../utils/cn";
import { Button } from "../ui/Button";
import { ThemeToggle } from "./ThemeToggle";

export function Sidebar(props: {
    conversations: Conversation[];
    activeId: string;
    onNewChat: () => void;
    onSelect: (id: string) => void;
    onRename: (id: string, title: string) => void;
    onDelete: (id: string) => void;
    onClose?: () => void;
    mobile?: boolean;
}) {
    const { conversations, activeId, onNewChat, onSelect, onRename, onDelete, onClose, mobile } = props;

    return (
        <div className="flex h-full flex-col bg-zinc-50 dark:bg-zinc-950">
            <div className="flex items-center gap-2 px-3 py-3">
                <Button variant="solid" className="flex-1" onClick={onNewChat}>
                    + New chat
                </Button>
                {mobile && (
                    <Button onClick={onClose} className="px-3">
                        ✕
                    </Button>
                )}
            </div>

            <div className="px-3">
                <div className="rounded-2xl border border-zinc-200 bg-white p-2 dark:border-zinc-800 dark:bg-zinc-900">
                    <div className="flex items-center justify-between px-1">
                        <div className="text-xs font-semibold text-zinc-600 dark:text-zinc-300">Conversations</div>
                        <ThemeToggle />
                    </div>

                    <div className="mt-2 max-h-[62vh] overflow-y-auto pr-1">
                        {conversations.map((c) => (
                            <ConversationRow
                                key={c.id}
                                title={c.title}
                                updatedAt={c.updatedAt}
                                active={c.id === activeId}
                                onClick={() => onSelect(c.id)}
                                onRename={(t) => onRename(c.id, t)}
                                onDelete={() => onDelete(c.id)}
                            />
                        ))}
                    </div>
                </div>
            </div>

            <div className="mt-auto px-3 py-3">
                <div className="rounded-2xl border border-zinc-200 bg-white p-3 text-xs text-zinc-600 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-300">
                    <div className="font-semibold">Your AI Agent</div>
                    <div className="mt-1 text-zinc-500 dark:text-zinc-400">UI full screen • dark/light • responsive</div>
                </div>
            </div>
        </div>
    );
}

function ConversationRow(props: {
    title: string;
    updatedAt: number;
    active: boolean;
    onClick: () => void;
    onRename: (t: string) => void;
    onDelete: () => void;
}) {
    const { title, updatedAt, active, onClick, onRename, onDelete } = props;
    const [editing, setEditing] = useState(false);
    const [value, setValue] = useState(title);

    return (
        <div
            className={cn(
                "group mt-1 flex items-center gap-2 rounded-xl px-2 py-2",
                active ? "bg-zinc-100 dark:bg-zinc-800" : "hover:bg-zinc-100 dark:hover:bg-zinc-800/70"
            )}
        >
            <button onClick={onClick} className="min-w-0 flex-1 text-left">
                {editing ? (
                    <input
                        autoFocus
                        value={value}
                        onChange={(e) => setValue(e.target.value)}
                        onBlur={() => {
                            setEditing(false);
                            onRename(value.trim() || "Untitled");
                        }}
                        onKeyDown={(e) => {
                            if (e.key === "Enter") {
                                setEditing(false);
                                onRename(value.trim() || "Untitled");
                            }
                            if (e.key === "Escape") {
                                setEditing(false);
                                setValue(title);
                            }
                        }}
                        className="w-full rounded-lg border border-zinc-200 bg-white px-2 py-1 text-sm outline-none dark:border-zinc-700 dark:bg-zinc-900"
                    />
                ) : (
                    <div className="truncate text-sm">{title}</div>
                )}
                <div className="text-[11px] text-zinc-500 dark:text-zinc-400">
                    Updated {new Date(updatedAt).toLocaleDateString()}
                </div>
            </button>

            <div className="hidden items-center gap-1 group-hover:flex">
                <button
                    onClick={() => setEditing(true)}
                    className="rounded-lg px-2 py-1 text-xs hover:bg-zinc-200 dark:hover:bg-zinc-700"
                    title="Rename"
                >
                    ✎
                </button>
                <button
                    onClick={onDelete}
                    className="rounded-lg px-2 py-1 text-xs hover:bg-zinc-200 dark:hover:bg-zinc-700"
                    title="Delete"
                >
                    🗑
                </button>
            </div>
        </div>
    );
}
