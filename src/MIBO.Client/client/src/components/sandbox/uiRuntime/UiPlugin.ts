import type React from "react";

export type AdaptResult<TProps> =
    | { ok: true; props: TProps }
    | { ok: false; error: string };

export type UiPlugin<TProps = any> = {
    kind: string;

    /**
     * Transformă props "unknown" din UiSpec -> props tipate pentru componentă.
     * Aici aplici default-uri de feature și validezi shape-ul.
     */
    adapt: (rawProps: unknown) => AdaptResult<TProps>;

    /**
     * Componenta React care va fi randată.
     * Trebuie să fie "pure" cât mai mult; data fetching e OK în feature layer.
     */
    Component: React.ComponentType<TProps>;
};
