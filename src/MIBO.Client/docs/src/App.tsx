import { BrowserRouter, Routes, Route, Navigate } from "react-router-dom";
import { Suspense, lazy, useMemo } from "react";
import { DocsLayout } from "@/layouts/DocsLayout";
import { Breadcrumbs } from "@/layouts/Breadcrumbs";
import { mdxComponents } from "@/components/mdx";
import { ImageLightboxProvider } from "@/components/mdx/ImageLightbox";
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
        <ImageLightboxProvider>
            <Breadcrumbs />
            <article
                data-mdx-content
                className="prose prose-lg max-w-none prose-zinc dark:prose-invert
                prose-headings:scroll-mt-24 prose-headings:tracking-tight
                prose-p:my-4 prose-p:max-w-[76ch] prose-p:text-zinc-650 dark:prose-p:text-zinc-300
                prose-strong:font-semibold prose-strong:text-zinc-950 dark:prose-strong:text-white
                prose-a:font-medium prose-a:text-zinc-950 prose-a:underline prose-a:decoration-zinc-300 prose-a:underline-offset-4
                hover:prose-a:decoration-zinc-500 dark:prose-a:text-zinc-100 dark:prose-a:decoration-zinc-700 dark:hover:prose-a:decoration-zinc-400
                prose-li:my-1 prose-li:text-zinc-650 dark:prose-li:text-zinc-300
                prose-code:rounded-md prose-code:border prose-code:border-zinc-200 prose-code:bg-zinc-50 prose-code:px-1.5 prose-code:py-0.5 prose-code:font-mono prose-code:text-[0.84em] prose-code:font-medium prose-code:text-zinc-900 prose-code:before:content-none prose-code:after:content-none
                dark:prose-code:border-white/10 dark:prose-code:bg-white/5 dark:prose-code:text-zinc-100
                prose-pre:p-0 prose-pre:bg-transparent
                prose-img:rounded-2xl
                prose-table:m-0 prose-table:w-full
                prose-th:text-left prose-th:align-bottom
                prose-td:align-top
                prose-hr:my-12 prose-hr:border-zinc-200 dark:prose-hr:border-zinc-800"
            >
                <Suspense fallback={<div className="flex justify-center py-12"><Spinner className="h-6 w-6" /></div>}>
                    <Component components={mdxComponents} />
                </Suspense>
            </article>
        </ImageLightboxProvider>
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
