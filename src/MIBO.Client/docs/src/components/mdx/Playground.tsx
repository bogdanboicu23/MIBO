import { LiveProvider, LiveEditor, LivePreview, LiveError } from "react-live";
import * as UI from "@/components/ui";
import * as Registry from "@/components/sandbox/registry";
import { createActionLogger, sandboxData, sandboxRegistry } from "@/components/sandbox/registry/sandboxData";

const scope = {
    ...UI,
    ...Registry,
    sandboxData,
    sandboxRegistry,
    createActionLogger,
};

interface PlaygroundProps {
    code: string;
    noInline?: boolean;
}

export function Playground({ code, noInline = false }: PlaygroundProps) {
    return (
        <div className="my-6 overflow-hidden rounded-xl border border-zinc-200 dark:border-zinc-800">
            <div className="border-b border-zinc-200 bg-zinc-50 px-4 py-2 text-xs font-medium text-zinc-600 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-400">
                Interactive Playground
            </div>
            <LiveProvider code={code.trim()} scope={scope} noInline={noInline}>
                <div className="border-b border-zinc-200 dark:border-zinc-800">
                    <div className="border-b border-zinc-200 bg-zinc-50 px-4 py-2 text-xs font-medium text-zinc-600 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-400">
                        Preview
                    </div>
                    <div className="flex items-center justify-center p-6">
                        <LivePreview />
                    </div>
                </div>
                <div>
                    <div className="border-b border-zinc-200 bg-zinc-50 px-4 py-2 text-xs font-medium text-zinc-600 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-400">
                        Editable Source
                    </div>
                    <LiveEditor
                        className="!bg-white !font-mono !text-sm dark:!bg-zinc-950"
                        style={{ minHeight: 180 }}
                    />
                </div>
                <LiveError className="border-t border-red-200 bg-red-50 p-3 text-xs text-red-700 dark:border-red-900 dark:bg-red-950/30 dark:text-red-300" />
            </LiveProvider>
        </div>
    );
}
