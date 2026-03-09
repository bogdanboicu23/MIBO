import { ComponentPreview } from "@/components/mdx/ComponentPreview";
import { FeatureCard, FeatureGrid } from "@/components/mdx/FeatureGrid";
import { Playground } from "@/components/mdx/Playground";
import { PropsTable } from "@/components/mdx/PropsTable";
import { SectionDivider, SectionHeader } from "@/components/mdx/SectionDivider";
import { registryDocs } from "@/components/sandbox/registry/docsMetadata";
import { createActionLogger, sandboxData, sandboxRegistry } from "@/components/sandbox/registry/sandboxData";

interface RegistryComponentDocProps {
    componentId: string;
}

export function RegistryComponentDoc({ componentId }: RegistryComponentDocProps) {
    const meta = registryDocs[componentId];

    if (!meta) {
        return (
            <div className="rounded-xl border border-amber-300 bg-amber-50 p-4 text-sm text-amber-900 dark:border-amber-900/80 dark:bg-amber-950/30 dark:text-amber-200">
                Unknown component id: <code>{componentId}</code>
            </div>
        );
    }

    const Component = meta.component;
    const actionLogger = createActionLogger(meta.title);

    return (
        <>
            <p>{meta.overview}</p>

            <SectionDivider />

            <SectionHeader
                title="Reference Preview"
                description="Baseline implementation with source code and complete props table."
            />

            <ComponentPreview
                code={meta.previewCode}
                propsSlot={<PropsTable props={meta.props} />}
            >
                <div className={meta.previewContainerClassName ?? "w-full max-w-3xl"}>
                    <Component
                        props={meta.previewProps}
                        data={sandboxData}
                        onAction={meta.requiresActions ? actionLogger : undefined}
                        registry={meta.requiresRegistry ? sandboxRegistry : undefined}
                    />
                </div>
            </ComponentPreview>

            <SectionDivider />

            <SectionHeader
                title="Adjustment Matrix"
                description="Core customization vectors you can combine in production."
            />

            <FeatureGrid columns={2}>
                {meta.adjustments.map((adjustment) => (
                    <FeatureCard
                        key={adjustment.title}
                        title={adjustment.title}
                        description={adjustment.description}
                    />
                ))}
            </FeatureGrid>

            <SectionDivider />

            <SectionHeader
                title="Interactive Sandbox"
                description="Edit JSX and props in real time to validate states and behavior."
            />
            <Playground code={meta.sandboxCode} />
        </>
    );
}
