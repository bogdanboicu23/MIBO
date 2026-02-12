import type { ChatState } from "../../types/chat.ts";
import { UiRenderer } from "./uiRuntime";

type Props = {
    state: ChatState
};
export const GenerativeUI = ({ state }: Props) => {
    return (
        <div className="lg:col-span-7 space-y-3 mt-4">
            <UiRenderer spec={state.lastUiSpec} />
        </div>
    );
}
    