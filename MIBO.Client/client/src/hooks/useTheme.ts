import { useEffect, useState } from "react";
import { getStorage, setStorage } from "../utils/storage";
import type { ThemeMode } from "../theme/tokens";
import { themeKey } from "../theme/tokens";

export function useTheme() {
    const [theme, setTheme] = useState<ThemeMode>(() => getStorage(themeKey, "dark"));

    useEffect(() => {
        const isDark = theme === "dark";
        document.documentElement.classList.toggle("dark", isDark);
        document.documentElement.style.colorScheme = isDark ? "dark" : "light";

        setStorage(themeKey, theme);
    }, [theme]);

    return { theme, setTheme };
}
