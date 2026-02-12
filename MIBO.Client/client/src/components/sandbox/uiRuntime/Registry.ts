import type { UiPlugin } from "./UiPlugin";

const registry = new Map<string, UiPlugin<any>>();

export function registerPlugin(plugin: UiPlugin<any>) {
    const kind = plugin.kind.trim();
    if (!kind) throw new Error("Plugin.kind cannot be empty.");

    if (registry.has(kind)) {
        throw new Error(`Plugin for kind '${kind}' is already registered.`);
    }

    registry.set(kind, plugin);
}

export function getPlugin(kind: string): UiPlugin<any> | undefined {
    return registry.get(kind);
}

export function listPlugins(): UiPlugin<any>[] {
    return Array.from(registry.values());
}
