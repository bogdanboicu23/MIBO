import type { UiSpec } from "./specTypes";
import { validateUiSpec } from "./specValidation";
import { normalizeUiSpec } from "./specNormalizer";
import { getPlugin } from "./Registry";
import { FallbackView } from "./FallbackView";

export type UiRendererProps = {
    spec: UiSpec | unknown | null | undefined;
};


export function UiRenderer({ spec }: UiRendererProps) {
    if (!spec) {
        return <FallbackView title="Nu există UI" reason="Asistentul nu a furnizat un UiSpec." />;
    }

    const validated = validateUiSpec(spec);
    if (!validated.ok) {
        return <FallbackView title="UiSpec invalid" reason={validated.error} />;
    }

    const normalized = normalizeUiSpec(validated.value);
    const kind = normalized.view.kind;

    const plugin = getPlugin(kind);
    if (!plugin) {
        return (
            <FallbackView
                title="Widget necunoscut"
                kind={kind}
                reason={`Nu există plugin înregistrat pentru kind='${kind}'. Înregistrează un plugin în Registry.`}
            />
        );
    }

    const adapted = plugin.adapt(normalized.view.props);
    if (!adapted.ok) {
        return <FallbackView title="Props invalide" kind={kind} reason={adapted.error} />;
    }

    const Component = plugin.Component;
    // Cast props to correct type since we know it's valid after adapt
    const componentProps = adapted.props as Record<string, unknown>;

    return (
        <div className="space-y-3">
            {normalized.view.title ? (
                <div className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">{normalized.view.title}</div>
            ) : null}
            <Component {...componentProps} />
        </div>
    );
}
