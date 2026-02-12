import { createContext, useContext } from "react";

export type Dict = Record<string, any>;
export type I18nCtx = { locale: "ro" | "en"; t: <T = any>(path: string) => T };

export const I18nContext = createContext<I18nCtx | null>(null);

export function useI18n() {
    const ctx = useContext(I18nContext);
    if (!ctx) throw new Error("I18nProvider missing");
    return ctx;
}
