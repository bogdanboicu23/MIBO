import type { Action } from "./actionTypes";

export type ActionHandler = (action: Action) => void | Promise<void>;

type Subscription = {
    id: string;
    handler: ActionHandler;
    filter?: (action: Action) => boolean;
};

function uid() {
    return Math.random().toString(16).slice(2) + Date.now().toString(16);
}

/**
 * ActionBus = Mediator/EventBus light + Command dispatcher.
 * - Widgets emit actions (commands)
 * - App shell (ChatPage) can listen and forward to backend or show toasts
 */
export class ActionBus {
    private subs: Subscription[] = [];

    subscribe(handler: ActionHandler, filter?: (action: Action) => boolean) {
        const id = uid();
        this.subs.push({ id, handler, filter });
        return () => {
            this.subs = this.subs.filter((s) => s.id !== id);
        };
    }

    async dispatch(action: Action) {
        const targets = this.subs.filter((s) => (s.filter ? s.filter(action) : true));
        for (const s of targets) {
            await s.handler(action);
        }
    }
}

/**
 * Singleton bus for the app. (Poți înlocui cu DI mai târziu)
 */
export const actionBus = new ActionBus();
