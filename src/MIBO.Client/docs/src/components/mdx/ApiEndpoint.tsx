import { cn } from "@/utils/cn";
import { CodeBlock } from "./CodeBlock";

type HttpMethod = "GET" | "POST" | "PUT" | "PATCH" | "DELETE";

const methodColors: Record<HttpMethod, { badge: string; accent: string }> = {
    GET: { badge: "bg-emerald-100 text-emerald-800 dark:bg-emerald-900/50 dark:text-emerald-300", accent: "border-t-emerald-400 dark:border-t-emerald-600" },
    POST: { badge: "bg-blue-100 text-blue-800 dark:bg-blue-900/50 dark:text-blue-300", accent: "border-t-blue-400 dark:border-t-blue-600" },
    PUT: { badge: "bg-amber-100 text-amber-800 dark:bg-amber-900/50 dark:text-amber-300", accent: "border-t-amber-400 dark:border-t-amber-600" },
    PATCH: { badge: "bg-orange-100 text-orange-800 dark:bg-orange-900/50 dark:text-orange-300", accent: "border-t-orange-400 dark:border-t-orange-600" },
    DELETE: { badge: "bg-red-100 text-red-800 dark:bg-red-900/50 dark:text-red-300", accent: "border-t-red-400 dark:border-t-red-600" },
};

interface Param {
    name: string;
    type: string;
    required?: boolean;
    description: string;
}

interface ApiEndpointProps {
    method: HttpMethod;
    path: string;
    description?: string;
    params?: Param[];
    body?: string;
    response?: string;
    auth?: boolean;
}

export function ApiEndpoint({ method, path, description, params, body, response, auth }: ApiEndpointProps) {
    const colors = methodColors[method];
    return (
        <div className={cn("not-prose my-8 overflow-hidden rounded-xl border border-zinc-200 border-t-2 dark:border-zinc-800", colors.accent)}>
            <div className="flex items-center gap-3 border-b border-zinc-200 bg-zinc-50/80 px-5 py-3.5 dark:border-zinc-800 dark:bg-zinc-900/60">
                <span className={cn("rounded-md px-2.5 py-1 text-xs font-extrabold uppercase tracking-wide", colors.badge)}>
                    {method}
                </span>
                <code className="text-[14px] font-semibold text-zinc-900 dark:text-zinc-100">{path}</code>
                {auth && (
                    <span className="ml-auto rounded-full border border-zinc-300 bg-zinc-100 px-2.5 py-0.5 text-xs font-medium text-zinc-600 dark:border-zinc-700 dark:bg-zinc-800 dark:text-zinc-400">
                        🔒 Auth required
                    </span>
                )}
            </div>

            <div className="space-y-5 px-5 py-4">
                {description && <p className="text-[14px] leading-relaxed text-zinc-600 dark:text-zinc-400">{description}</p>}

                {params && params.length > 0 && (
                    <div>
                        <h4 className="mb-2.5 text-xs font-bold uppercase tracking-widest text-zinc-500 dark:text-zinc-400">Parameters</h4>
                        <div className="overflow-x-auto rounded-lg border border-zinc-200 dark:border-zinc-800">
                            <table className="min-w-full text-sm">
                                <thead>
                                    <tr className="border-b border-zinc-200 bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-900/40">
                                        <th className="px-3 py-2.5 text-left text-xs font-semibold text-zinc-700 dark:text-zinc-300">Name</th>
                                        <th className="px-3 py-2.5 text-left text-xs font-semibold text-zinc-700 dark:text-zinc-300">Type</th>
                                        <th className="px-3 py-2.5 text-left text-xs font-semibold text-zinc-700 dark:text-zinc-300">Required</th>
                                        <th className="px-3 py-2.5 text-left text-xs font-semibold text-zinc-700 dark:text-zinc-300">Description</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    {params.map((p) => (
                                        <tr key={p.name} className="border-b border-zinc-100 last:border-0 dark:border-zinc-800/50">
                                            <td className="px-3 py-2.5 font-mono text-xs font-medium text-zinc-900 dark:text-zinc-100">{p.name}</td>
                                            <td className="px-3 py-2.5 font-mono text-xs text-zinc-500 dark:text-zinc-400">{p.type}</td>
                                            <td className="px-3 py-2.5 text-xs">{p.required ? <span className="font-semibold text-emerald-600 dark:text-emerald-400">Yes</span> : <span className="text-zinc-400">No</span>}</td>
                                            <td className="px-3 py-2.5 text-xs text-zinc-600 dark:text-zinc-400">{p.description}</td>
                                        </tr>
                                    ))}
                                </tbody>
                            </table>
                        </div>
                    </div>
                )}

                {body && (
                    <div>
                        <h4 className="mb-2.5 text-xs font-bold uppercase tracking-widest text-zinc-500 dark:text-zinc-400">Request Body</h4>
                        <CodeBlock language="json">{body}</CodeBlock>
                    </div>
                )}

                {response && (
                    <div>
                        <h4 className="mb-2.5 text-xs font-bold uppercase tracking-widest text-zinc-500 dark:text-zinc-400">Response</h4>
                        <CodeBlock language="json">{response}</CodeBlock>
                    </div>
                )}
            </div>
        </div>
    );
}
