export type UiSpecVersion = "1";

export type UiSpec = {
    version: UiSpecVersion;
    view: UiView;
};

export type UiView = {
    /** Identificator unic pentru widget/componenta generativă */
    kind: string;
    title?: string;
    props?: unknown;

    /**
     * Optional: state serializabil al UI-ului (ex: filtre curente).
     * Util când trimiți POST /chat/action.
     */
    state?: unknown;

    /**
     * Optional: listă de "capabilități" pe care UI-ul le oferă (ex: export, drilldown)
     * Poate fi folosită de ChatShell ca să afișeze shortcuts globale.
     */
    capabilities?: string[];
};
