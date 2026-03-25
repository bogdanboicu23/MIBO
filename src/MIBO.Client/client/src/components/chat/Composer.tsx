import { useLayoutEffect, useRef, useState } from "react";
import { Button } from "../ui/Button";
import { Textarea } from "../ui/Textarea";

export function Composer(props: { onSend: (text: string) => Promise<void> | void; disabled?: boolean }) {
    const { onSend, disabled } = props;
    const [value, setValue] = useState("");
    const textareaRef = useRef<HTMLTextAreaElement>(null);

    useLayoutEffect(() => {
        const textarea = textareaRef.current;
        if (!textarea) return;

        textarea.style.height = "0px";
        const nextHeight = Math.min(textarea.scrollHeight, 200);
        textarea.style.height = `${nextHeight}px`;
        textarea.style.overflowY = textarea.scrollHeight > 200 ? "auto" : "hidden";
    }, [value]);

    async function send() {
        if (!value.trim() || disabled) return;
        const v = value;
        setValue("");
        await onSend(v);
    }

    return (
        <div className="border-t border-zinc-200/70 bg-white/70 backdrop-blur dark:border-zinc-800/70 dark:bg-zinc-950/40">
            <div className="mx-auto w-full max-w-5xl px-4 py-4 md:px-6">
                <div className="rounded-2xl border border-zinc-200 bg-white p-2 shadow-sm dark:border-zinc-800 dark:bg-zinc-900">
                    <div className="flex items-end gap-2">
                        <Textarea
                            ref={textareaRef}
                            value={value}
                            onChange={(e) => setValue(e.target.value)}
                            placeholder="Message your agent…"
                            rows={1}
                            className="min-h-[44px] max-h-[200px]"
                            onKeyDown={(e) => {
                                if (e.key === "Enter" && !e.shiftKey) {
                                    e.preventDefault();
                                    void send();
                                }
                            }}
                        />
                        <Button variant="solid" onClick={() => void send()} disabled={!value.trim() || disabled}>
                            Send
                        </Button>
                    </div>
                    <div className="mt-2 flex items-center justify-between px-3 pb-1 text-[11px] text-zinc-500 dark:text-zinc-400">
                        <span>Enter to send • Shift+Enter for newline</span>
                        <span className="hidden md:inline">UI demo</span>
                    </div>
                </div>
            </div>
        </div>
    );
}
