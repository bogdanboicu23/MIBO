import { useEffect, useState } from "react";
import { useLocation } from "react-router-dom";

export interface TocEntry {
    id: string;
    text: string;
    level: number;
}

function extractHeadings(article: Element | null): TocEntry[] {
    if (!article) {
        return [];
    }

    return Array.from(article.querySelectorAll("h2, h3"))
        .map((el) => ({
            id: el.id,
            text: el.textContent?.trim() ?? "",
            level: el.tagName === "H2" ? 2 : 3,
        }))
        .filter((entry) => entry.id && entry.text);
}

export function useTableOfContents() {
    const location = useLocation();
    const [headings, setHeadings] = useState<TocEntry[]>([]);
    const [activeId, setActiveId] = useState("");

    useEffect(() => {
        const article = document.querySelector("[data-mdx-content]");
        if (!article) {
            setHeadings([]);
            setActiveId("");
            return;
        }

        const syncHeadings = () => {
            setHeadings(extractHeadings(article));
        };

        syncHeadings();

        const mutationObserver = new MutationObserver(syncHeadings);
        mutationObserver.observe(article, { childList: true, subtree: true });

        return () => mutationObserver.disconnect();
    }, [location.pathname]);

    useEffect(() => {
        if (location.hash) {
            setActiveId(decodeURIComponent(location.hash.slice(1)));
            return;
        }

        setActiveId(headings[0]?.id ?? "");
    }, [headings, location.hash]);

    useEffect(() => {
        if (headings.length === 0) {
            return;
        }

        const els = headings
            .map((heading) => document.getElementById(heading.id))
            .filter((el): el is HTMLElement => Boolean(el));

        if (els.length === 0) {
            return;
        }

        const observer = new IntersectionObserver(
            (entries) => {
                const visibleEntries = entries
                    .filter((entry) => entry.isIntersecting)
                    .sort((a, b) => a.boundingClientRect.top - b.boundingClientRect.top);

                if (visibleEntries.length > 0) {
                    setActiveId(visibleEntries[0]!.target.id);
                }
            },
            { rootMargin: "-80px 0px -60% 0px", threshold: 0.1 }
        );

        els.forEach((el) => observer.observe(el));
        return () => observer.disconnect();
    }, [headings]);

    return { headings, activeId };
}
