import { useEffect, useState } from "react";

type ThemeMode = "light" | "dark";

const themeStorageKey = "status-theme";

function getInitialTheme(): ThemeMode {
    if (typeof window === "undefined") {
        return "dark";
    }

    try {
        const stored = localStorage.getItem(themeStorageKey);
        if (stored === "light" || stored === "dark") {
            return stored;
        }
    } catch {
        // noop
    }

    return window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
}

export function useTheme() {
    const [theme, setTheme] = useState<ThemeMode>(getInitialTheme);

    useEffect(() => {
        const isDark = theme === "dark";
        document.documentElement.classList.toggle("dark", isDark);
        document.documentElement.style.colorScheme = isDark ? "dark" : "light";

        try {
            localStorage.setItem(themeStorageKey, theme);
        } catch {
            // noop
        }
    }, [theme]);

    const toggleTheme = () => {
        setTheme((current) => (current === "dark" ? "light" : "dark"));
    };

    return { theme, setTheme, toggleTheme };
}
