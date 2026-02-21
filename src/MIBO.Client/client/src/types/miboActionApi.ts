import type { AxiosInstance } from "axios";
import { endpoints } from "@/axios/endpoints.ts";

export type ActionRequestV1 = {
    schema: "action.v1";
    conversationId: string;
    userId: string;
    action: {
        type: string; // "shop.buy" | "ui.setFilter" | ...
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
    uiPatch?: any | null;
    correlationId?: string;
};

export async function postAction(axios: AxiosInstance, req: ActionRequestV1) {
    const { data } = await axios.post<ActionResultV1>(endpoints.conversations.action, req);
    return data;
}