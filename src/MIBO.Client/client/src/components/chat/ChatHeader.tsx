import { Link } from "react-router-dom";
import { Button } from "../ui/Button";
import { ThemeToggle } from "./ThemeToggle";

export function ChatHeader(props: {
    title: string;
    model: string;
    onModelChange: (v: "AI Agent" | "AI Agent Pro") => void;
    onNewChat: () => void;
    onOpenSidebarMobile: () => void;
}) {
    const { title, model, onModelChange, onNewChat, onOpenSidebarMobile } = props;

    return (
        <div className="sticky top-0 z-20 border-b border-zinc-200/70 bg-white/70 backdrop-blur dark:border-zinc-800/70 dark:bg-zinc-950/40">
            <div className="mx-auto flex w-full max-w-5xl items-center gap-3 px-3 py-3 md:px-6">
                <button
                    onClick={onOpenSidebarMobile}
                    className="rounded-xl border border-zinc-200 bg-white px-3 py-2 text-sm hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-900 dark:hover:bg-zinc-800 md:hidden"
                    aria-label="Open sidebar"
                >
                    ☰
                </button>

                <div className="min-w-0 flex-1">
                    <div className="truncate text-sm font-semibold">{title}</div>
                    <div className="text-xs text-zinc-500 dark:text-zinc-400">
                        Model{" "}
                        <select
                            value={model}
                            onChange={(e) => onModelChange(e.target.value as "AI Agent" | "AI Agent Pro")}
                            className="ml-2 rounded-lg border border-zinc-200 bg-white px-2 py-1 text-xs dark:border-zinc-800 dark:bg-zinc-900"
                        >
                            <option>AI Agent</option>
                            <option>AI Agent Pro</option>
                        </select>
                    </div>
                </div>

                <div className="hidden items-center gap-2 md:flex">
                    <Button onClick={onNewChat}>+ New chat</Button>
                    <ThemeToggle />
                    <Link
                        to="/settings"
                        className="rounded-xl border border-zinc-200 bg-white p-2 text-zinc-600 transition hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-400 dark:hover:bg-zinc-800"
                        aria-label="Settings"
                    >
                        <svg viewBox="0 0 20 20" fill="currentColor" className="h-4 w-4">
                            <path fillRule="evenodd" d="M7.84 1.804A1 1 0 018.82 1h2.36a1 1 0 01.98.804l.331 1.652a6.993 6.993 0 011.929 1.115l1.598-.54a1 1 0 011.186.447l1.18 2.044a1 1 0 01-.205 1.251l-1.267 1.113a7.047 7.047 0 010 2.228l1.267 1.113a1 1 0 01.206 1.25l-1.18 2.045a1 1 0 01-1.187.447l-1.598-.54a6.993 6.993 0 01-1.929 1.115l-.33 1.652a1 1 0 01-.98.804H8.82a1 1 0 01-.98-.804l-.331-1.652a6.993 6.993 0 01-1.929-1.115l-1.598.54a1 1 0 01-1.186-.447l-1.18-2.044a1 1 0 01.205-1.251l1.267-1.114a7.05 7.05 0 010-2.227L1.821 7.773a1 1 0 01-.206-1.25l1.18-2.045a1 1 0 011.187-.447l1.598.54A6.993 6.993 0 017.51 3.456l.33-1.652zM10 13a3 3 0 100-6 3 3 0 000 6z" clipRule="evenodd" />
                        </svg>
                    </Link>
                </div>

                <div className="flex items-center gap-2 md:hidden">
                    <ThemeToggle />
                    <Link
                        to="/settings"
                        className="rounded-xl border border-zinc-200 bg-white p-2 text-zinc-600 transition hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-400 dark:hover:bg-zinc-800"
                        aria-label="Settings"
                    >
                        <svg viewBox="0 0 20 20" fill="currentColor" className="h-4 w-4">
                            <path fillRule="evenodd" d="M7.84 1.804A1 1 0 018.82 1h2.36a1 1 0 01.98.804l.331 1.652a6.993 6.993 0 011.929 1.115l1.598-.54a1 1 0 011.186.447l1.18 2.044a1 1 0 01-.205 1.251l-1.267 1.113a7.047 7.047 0 010 2.228l1.267 1.113a1 1 0 01.206 1.25l-1.18 2.045a1 1 0 01-1.187.447l-1.598-.54a6.993 6.993 0 01-1.929 1.115l-.33 1.652a1 1 0 01-.98.804H8.82a1 1 0 01-.98-.804l-.331-1.652a6.993 6.993 0 01-1.929-1.115l-1.598.54a1 1 0 01-1.186-.447l-1.18-2.044a1 1 0 01.205-1.251l1.267-1.114a7.05 7.05 0 010-2.227L1.821 7.773a1 1 0 01-.206-1.25l1.18-2.045a1 1 0 011.187-.447l1.598.54A6.993 6.993 0 017.51 3.456l.33-1.652zM10 13a3 3 0 100-6 3 3 0 000 6z" clipRule="evenodd" />
                        </svg>
                    </Link>
                </div>
            </div>
        </div>
    );
}
