import type { UiSpec } from "./specTypes";

export function normalizeUiSpec(spec: UiSpec): UiSpec {
    const kind = spec.view.kind.trim();

    return {
        version: "1",
        view: {
            kind,
            title: typeof spec.view.title === "string" ? spec.view.title.trim() : undefined,
            props: spec.view.props ?? undefined,
            state: spec.view.state ?? undefined,
            capabilities: Array.isArray(spec.view.capabilities) ? spec.view.capabilities : undefined,
        },
    };
}
