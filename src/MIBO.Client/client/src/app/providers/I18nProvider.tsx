import React, { useMemo, useState } from "react";
import { I18nContext } from "../../hooks/useI18n";
import { locales, type LocaleKey } from "../../locales";

function getByPath(obj: Record<string, unknown>, path: string): unknown {
    return path.split(".").reduce<unknown>((acc, k) => {
        if (acc && typeof acc === 'object' && k in acc) {
            return (acc as Record<string, unknown>)[k];
        }
        return undefined;
    }, obj);
}

export function I18nProvider({ children }: { children: React.ReactNode }) {
    const [locale] = useState<LocaleKey>("ro"); // poți adăuga toggle ulterior
    const dict = locales[locale];

    const value = useMemo(
        () => ({
            locale,
            t: <T,>(path: string) => (getByPath(dict, path) as T),
        }),
        [locale, dict]
    );

    return <I18nContext.Provider value={value}>{children}</I18nContext.Provider>;
}
