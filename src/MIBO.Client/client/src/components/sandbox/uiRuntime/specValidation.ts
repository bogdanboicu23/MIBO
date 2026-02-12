import type { UiSpec } from "./specTypes";

export type ValidationResult<T> =
    | { ok: true; value: T }
    | { ok: false; error: string };

function isObject(x: unknown): x is Record<string, unknown> {
    return typeof x === "object" && x !== null && !Array.isArray(x);
}

export function validateUiSpec(input: unknown): ValidationResult<UiSpec> {
    if (!isObject(input)) return { ok: false, error: "UiSpec must be an object." };

    const version = input["version"];
    if (version !== "1") return { ok: false, error: "UiSpec.version must be '1'." };

    const view = input["view"];
    if (!isObject(view)) return { ok: false, error: "UiSpec.view must be an object." };

    const kind = view["kind"];
    if (typeof kind !== "string" || !kind.trim()) return { ok: false, error: "UiSpec.view.kind must be a non-empty string." };

    const title = view["title"];
    if (title !== undefined && typeof title !== "string") return { ok: false, error: "UiSpec.view.title must be a string if provided." };

    const capabilities = view["capabilities"];
    if (capabilities !== undefined) {
        if (!Array.isArray(capabilities) || capabilities.some((c) => typeof c !== "string")) {
            return { ok: false, error: "UiSpec.view.capabilities must be string[] if provided." };
        }
    }

    return { ok: true, value: input as UiSpec };
}
