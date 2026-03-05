import { useMemo, useState } from "react";
import type { NavItem } from "@/config/navigation";
import { navItems, searchIndex } from "@/config/navigation";

export interface SearchResult {
    title: string;
    description: string;
    slug: string;
    category: string;
}

function flattenNav(items: NavItem[]): SearchResult[] {
    const results: SearchResult[] = [];
    for (const item of items) {
        if (item.slug) {
            results.push({
                title: item.title,
                description: item.description ?? "",
                slug: item.slug,
                category: item.category ?? "",
            });
        }
        if (item.children) {
            results.push(...flattenNav(item.children));
        }
    }
    return results;
}

export function useSearch() {
    const [query, setQuery] = useState("");

    const allPages = useMemo(() => {
        if (searchIndex.length > 0) return searchIndex;
        return flattenNav(navItems);
    }, []);

    const results = useMemo(() => {
        if (!query.trim()) return [];
        const q = query.toLowerCase();
        return allPages.filter(
            (p) =>
                p.title.toLowerCase().includes(q) ||
                p.description.toLowerCase().includes(q)
        );
    }, [query, allPages]);

    return { query, setQuery, results };
}
