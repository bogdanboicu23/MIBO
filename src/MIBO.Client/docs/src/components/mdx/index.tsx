import type { ComponentType } from "react";
import { H1, H2, H3, H4, H5, H6 } from "./Heading";
import { CodeBlock } from "./CodeBlock";
import { Callout } from "./Callout";
import { ApiEndpoint } from "./ApiEndpoint";
import { PropsTable } from "./PropsTable";
import { ComponentPreview } from "./ComponentPreview";
import { Playground } from "./Playground";
import { Steps, Step } from "./Steps";
import { FeatureGrid, FeatureCard } from "./FeatureGrid";
import { LinkCard, LinkCardGrid } from "./LinkCard";
import { SectionDivider, SectionHeader } from "./SectionDivider";
import { MiboIntroPreview } from "../MiboIntroPreview";

// eslint-disable-next-line @typescript-eslint/no-explicit-any
export const mdxComponents: Record<string, ComponentType<any>> = {
    h1: H1,
    h2: H2,
    h3: H3,
    h4: H4,
    h5: H5,
    h6: H6,
    pre: ({ children }: { children: React.ReactElement<{ className?: string; children?: string }> }) => {
        const lang = children?.props?.className?.replace("language-", "") ?? "text";
        const code = children?.props?.children ?? "";
        return <CodeBlock language={lang}>{code}</CodeBlock>;
    },
    Callout,
    ApiEndpoint,
    PropsTable,
    ComponentPreview,
    Playground,
    Steps,
    Step,
    FeatureGrid,
    FeatureCard,
    LinkCard,
    LinkCardGrid,
    SectionDivider,
    SectionHeader,
    MiboIntroPreview,
};
