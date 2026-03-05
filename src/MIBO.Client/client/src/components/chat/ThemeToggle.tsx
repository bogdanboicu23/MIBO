import { IconButton } from "@/components/ui";
import { useThemeContext } from "@/app/providers/useThemeContext.ts";

export function ThemeToggle() {
    const { theme, setTheme } = useThemeContext();
    return (
        <IconButton onClick={() => setTheme(theme === "dark" ? "light" : "dark")} title="Toggle theme">
            {theme === "dark" ? "🌙" : "☀️"}
        </IconButton>
    );
}
