import React from "react";
import { useTheme } from "../../hooks/useTheme";
import { ThemeContext } from "./useThemeContext";

export function ThemeProvider({ children }: { children: React.ReactNode }) {
    const value = useTheme();
    return <ThemeContext.Provider value={value}>{children}</ThemeContext.Provider>;
}
