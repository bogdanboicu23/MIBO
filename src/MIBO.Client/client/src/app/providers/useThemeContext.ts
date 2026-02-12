import { createContext, useContext } from "react";
import type { ThemeMode } from "../../theme/tokens";

type ThemeCtx = { theme: ThemeMode; setTheme: (t: ThemeMode) => void };

export const ThemeContext = createContext<ThemeCtx | null>(null);

export function useThemeContext() {
    const ctx = useContext(ThemeContext);
    if (!ctx) throw new Error("ThemeProvider missing");
    return ctx;
}