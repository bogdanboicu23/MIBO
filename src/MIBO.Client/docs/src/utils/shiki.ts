import { createHighlighter, type Highlighter } from "shiki";

let highlighterPromise: Promise<Highlighter> | null = null;

export function getHighlighter(): Promise<Highlighter> {
    if (!highlighterPromise) {
        highlighterPromise = createHighlighter({
            themes: ["material-theme-ocean", "github-light"],
            langs: ["typescript", "tsx", "javascript", "jsx", "json", "bash", "css", "html", "csharp", "yaml", "dockerfile", "markdown"],
        });
    }
    return highlighterPromise;
}
