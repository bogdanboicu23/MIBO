import React, { useMemo, useState } from "react";
import { I18nContext } from "../../hooks/useI18n";
import { locales, type LocaleKey } from "../../locales";

function getByPath(obj: any, path: string) {
    return path.split(".").reduce((acc, k) => (acc ? acc[k] : undefined), obj);
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
