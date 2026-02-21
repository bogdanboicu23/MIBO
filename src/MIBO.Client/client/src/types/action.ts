import type { UiPatchV1 } from "@/types/ui.ts";

export type ActionRequestV1 = {
    schema: "action.v1";
    conversationId: string;
    userId: string;
    action: {
        type: string; // "shop.buy" | "ui.setFilter" | "ui.paginate" | ...
        payload: Record<string, unknown>;
    };
    uiContext?: {
        uiInstanceId?: string | null;
        focusedComponentId?: string | null;
    };
};

export type ActionResultV1 = {
    schema: "action.result.v1";
    text?: string | null;
    uiPatch?: UiPatchV1 | null;
    correlationId?: string;
};
