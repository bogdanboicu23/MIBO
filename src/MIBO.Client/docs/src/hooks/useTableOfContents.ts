import { useEffect, useState } from "react";

export interface TocEntry {
    id: string;
    text: string;
    level: number;
}

export function useTableOfContents() {
    const [headings, setHeadings] = useState<TocEntry[]>([]);
    const [activeId, setActiveId] = useState("");

    useEffect(() => {
        const article = document.querySelector("[data-mdx-content]");
        if (!article) return;

        const els = article.querySelectorAll("h2, h3");
        const items: TocEntry[] = Array.from(els).map((el) => ({
            id: el.id,
            text: el.textContent ?? "",
            level: el.tagName === "H2" ? 2 : 3,
        }));
        setHeadings(items);

        const observer = new IntersectionObserver(
            (entries) => {
                for (const entry of entries) {
                    if (entry.isIntersecting) {
                        setActiveId(entry.target.id);
                    }
                }
            },
            { rootMargin: "-80px 0px -60% 0px", threshold: 0.1 }
        );

        els.forEach((el) => observer.observe(el));
        return () => observer.disconnect();
    }, []);

    return { headings, activeId };
}
