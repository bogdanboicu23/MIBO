import { useEffect, useState } from "react";

type ThemeMode = "light" | "dark";

export function useTheme() {
    const [theme, setTheme] = useState<ThemeMode>(() => {
        try {
            const stored = localStorage.getItem("docs-theme");
            return (stored === "light" || stored === "dark") ? stored : "dark";
        } catch {
            return "dark";
        }
    });

    useEffect(() => {
        const isDark = theme === "dark";
        document.documentElement.classList.toggle("dark", isDark);
        document.documentElement.style.colorScheme = isDark ? "dark" : "light";

        try {
            localStorage.setItem("docs-theme", theme);
        } catch { /* noop */ }
    }, [theme]);

    return { theme, setTheme };
}
