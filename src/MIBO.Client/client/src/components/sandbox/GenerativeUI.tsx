import { UiRenderer } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";

type Props = {
    activeUi: any | null;
    handleAction: (type: string, payload: Record<string, unknown>) => void;
};
export const GenerativeUI = ({ activeUi, handleAction }: Props) => {
    return (
        <div className="lg:col-span-7 space-y-3 mt-4">
            <UiRenderer
                ui={activeUi}
                onAction={handleAction}
            />
        </div>
    );
}
    