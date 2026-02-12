import { IconButton } from "../ui/IconButton";
import { useThemeContext } from "../../app/providers/ThemeProvider";

export function ThemeToggle() {
    const { theme, setTheme } = useThemeContext();
    return (
        <IconButton onClick={() => setTheme(theme === "dark" ? "light" : "dark")} title="Toggle theme">
            {theme === "dark" ? "🌙" : "☀️"}
        </IconButton>
    );
}
