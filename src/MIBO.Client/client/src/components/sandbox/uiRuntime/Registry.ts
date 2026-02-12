import type { UiPlugin } from "./UiPlugin";

const registry = new Map<string, UiPlugin<unknown>>();

export function registerPlugin<TProps = unknown>(plugin: UiPlugin<TProps>) {
    const kind = plugin.kind.trim();
    if (!kind) throw new Error("Plugin.kind cannot be empty.");

    if (registry.has(kind)) {
        throw new Error(`Plugin for kind '${kind}' is already registered.`);
    }

    registry.set(kind, plugin as UiPlugin<unknown>);
}

export function getPlugin<TProps = unknown>(kind: string): UiPlugin<TProps> | undefined {
    return registry.get(kind) as UiPlugin<TProps> | undefined;
}

export function listPlugins(): UiPlugin<unknown>[] {
    return Array.from(registry.values());
}
