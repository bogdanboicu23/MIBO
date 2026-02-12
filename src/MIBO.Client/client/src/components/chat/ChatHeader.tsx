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
                            onChange={(e) => onModelChange(e.target.value as any)}
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
                </div>

                <div className="md:hidden">
                    <ThemeToggle />
                </div>
            </div>
        </div>
    );
}
