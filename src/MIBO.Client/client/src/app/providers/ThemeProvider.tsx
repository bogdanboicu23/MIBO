import React, { createContext, useContext } from "react";
import type { ThemeMode } from "../../theme/tokens";
import { useTheme } from "../../hooks/useTheme";

type ThemeCtx = { theme: ThemeMode; setTheme: (t: ThemeMode) => void };

const Ctx = createContext<ThemeCtx | null>(null);

export function ThemeProvider({ children }: { children: React.ReactNode }) {
    const value = useTheme();
    return <Ctx.Provider value={value}>{children}</Ctx.Provider>;
}

export function useThemeContext() {
    const ctx = useContext(Ctx);
    if (!ctx) throw new Error("ThemeProvider missing");
    return ctx;
}
