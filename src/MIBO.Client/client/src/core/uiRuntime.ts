// UI Runtime for plugin system
export interface UIRuntime {
    registerPlugin: (plugin: Plugin) => void;
    registerAdapter: (adapter: Adapter) => void;
}

export interface Plugin {
    id: string;
    name: string;
    version: string;
    initialize?: () => void;
}

export interface Adapter {
    id: string;
    name: string;
    type: string;
    initialize?: () => void;
}

export interface UiPlugin<T = any> {
    kind: string;
    adapt: (props: any) => AdaptResult<T>;
    Component: React.ComponentType<T>;
}

export type AdaptResult<T = any> =
    | { ok: true; value: T }
    | { ok: false; error: string }

// Default UI Runtime implementation
export const uiRuntime: UIRuntime = {
    registerPlugin: (plugin: Plugin) => {
        console.log(`Registering plugin: ${plugin.name}`);
        plugin.initialize?.();
    },
    registerAdapter: (adapter: Adapter) => {
        console.log(`Registering adapter: ${adapter.name}`);
        adapter.initialize?.();
    }
};

export default uiRuntime;