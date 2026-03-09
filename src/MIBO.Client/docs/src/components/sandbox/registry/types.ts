import type { ComponentType } from "react";

export interface UiComponentProps {
    props?: Record<string, any>;
    data?: Record<string, any>;
    onAction?: (actionType: string, payload?: Record<string, unknown>) => void;
    registry?: Record<string, ComponentType<UiComponentProps>>;
}
