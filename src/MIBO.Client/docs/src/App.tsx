import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { Suspense, lazy, useMemo } from "react";
import { DocsLayout } from "@/layouts/DocsLayout";
import { Breadcrumbs } from "@/layouts/Breadcrumbs";
import { mdxComponents } from "@/components/mdx";
import { Spinner } from "@/components/ui/Spinner";

const mdxModules = import.meta.glob<{
    default: React.ComponentType<{ components?: Record<string, React.ComponentType> }>;
    frontmatter?: { title?: string; description?: string };
}>("./content/**/*.mdx");

// eslint-disable-next-line @typescript-eslint/no-explicit-any
type MdxComponent = React.ComponentType<any>;

interface PageRoute {
    path: string;
    Component: React.LazyExoticComponent<MdxComponent>;
}

function MdxPage({ Component }: { Component: React.LazyExoticComponent<MdxComponent> }) {
    return (
        <>
            <Breadcrumbs />
            <article data-mdx-content className="prose-zinc prose prose-lg dark:prose-invert max-w-none
                prose-headings:scroll-mt-20 prose-headings:tracking-tight
                prose-h1:text-3xl prose-h1:font-extrabold prose-h1:text-zinc-950 dark:prose-h1:text-white
                prose-h2:text-2xl prose-h2:font-bold prose-h2:text-zinc-900 prose-h2:mt-12 prose-h2:mb-4 dark:prose-h2:text-zinc-50
                prose-h3:text-lg prose-h3:font-semibold prose-h3:text-zinc-800 prose-h3:mt-8 dark:prose-h3:text-zinc-200
                prose-p:text-zinc-600 prose-p:leading-relaxed dark:prose-p:text-zinc-300
                prose-strong:text-zinc-900 dark:prose-strong:text-white prose-strong:font-semibold
                prose-a:text-zinc-900 prose-a:underline prose-a:decoration-zinc-300 hover:prose-a:decoration-zinc-500
                dark:prose-a:text-zinc-100 dark:prose-a:decoration-zinc-600 dark:hover:prose-a:decoration-zinc-400
                prose-li:text-zinc-600 dark:prose-li:text-zinc-300
                prose-code:rounded-md prose-code:border prose-code:border-zinc-200 prose-code:bg-zinc-50 prose-code:px-1.5 prose-code:py-0.5 prose-code:font-mono prose-code:text-[0.85em] prose-code:font-medium prose-code:text-zinc-800 prose-code:before:content-none prose-code:after:content-none
                dark:prose-code:border-white/10 dark:prose-code:bg-white/5 dark:prose-code:text-zinc-200
                prose-pre:p-0 prose-pre:bg-transparent
                prose-img:rounded-xl
                prose-table:text-sm
                prose-th:text-left prose-th:font-semibold prose-th:text-zinc-900 dark:prose-th:text-zinc-100
                prose-td:py-2.5 prose-td:text-zinc-600 dark:prose-td:text-zinc-300
                prose-hr:border-zinc-200 dark:prose-hr:border-zinc-800
            ">
                <Suspense fallback={<div className="flex justify-center py-12"><Spinner className="h-6 w-6" /></div>}>
                    <Component components={mdxComponents} />
                </Suspense>
            </article>
        </>
    );
}

export default function App() {
    const routes = useMemo<PageRoute[]>(() => {
        return Object.entries(mdxModules).map(([filePath, loader]) => {
            const path = filePath
                .replace("./content", "")
                .replace(/\.mdx$/, "")
                .replace(/\/index$/, "") || "/";

            const Component = lazy(loader as () => Promise<{ default: MdxComponent }>);

            return { path: path === "/" ? "/" : path, Component };
        });
    }, []);

    return (
        <BrowserRouter>
            <Routes>
                <Route element={<DocsLayout />}>
                    {routes.map((r) => (
                        <Route
                            key={r.path}
                            path={r.path}
                            element={<MdxPage Component={r.Component} />}
                        />
                    ))}
                    <Route path="/" element={<Navigate to="/getting-started" replace />} />
                    <Route path="*" element={<Navigate to="/getting-started" replace />} />
                </Route>
            </Routes>
        </BrowserRouter>
    );
}
