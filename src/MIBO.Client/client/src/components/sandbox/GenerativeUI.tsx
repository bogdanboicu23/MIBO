import { UiRenderer } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";

type Props = {
    activeUi: any | null;
    handleAction: (type: string, payload: Record<string, unknown>) => void;
};
export const GenerativeUI = ({ activeUi, handleAction }: Props) => {
    return (
        <div className="mt-4 w-full">
            <div className="w-fit max-w-5xl min-w-0" style={{ contain: 'layout' }}>
                <UiRenderer
                    ui={activeUi}
                    onAction={handleAction}
                />
            </div>
        </div>
    );
}
    
