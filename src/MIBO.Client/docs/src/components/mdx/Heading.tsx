import { Children, cloneElement, isValidElement } from "react";
import { cn } from "@/utils/cn";

type Level = 1 | 2 | 3 | 4 | 5 | 6;
type HeadingAnchorElement = React.ReactElement<{ className?: string; children?: React.ReactNode }>;

const styles: Record<Level, string> = {
    1: "text-4xl font-black tracking-[-0.04em] mt-0 mb-7 text-zinc-950 dark:text-white",
    2: "text-[2.35rem] leading-tight font-black tracking-[-0.04em] mt-16 mb-6 pb-4 border-b border-zinc-200 dark:border-zinc-800 text-zinc-950 dark:text-zinc-50",
    3: "text-[1.45rem] leading-tight font-bold tracking-[-0.03em] mt-10 mb-4 text-zinc-900 dark:text-zinc-100",
    4: "text-[1.05rem] font-semibold mt-7 mb-2 text-zinc-800 dark:text-zinc-200",
    5: "text-sm font-semibold mt-4 mb-2 text-zinc-700 dark:text-zinc-300",
    6: "text-sm font-medium mt-4 mb-2 text-zinc-600 dark:text-zinc-400",
};

function createHeading(level: Level) {
    const Tag = `h${level}` as const;

    return function Heading({ children, id, className, ...props }: React.HTMLAttributes<HTMLHeadingElement>) {
        const marker = (
            <span className="ml-2 text-zinc-300 opacity-0 transition-opacity group-hover:opacity-100 dark:text-zinc-600">#</span>
        );

        const normalizedChildren = id ? Children.toArray(children) : [];
        const linkedChild = id && normalizedChildren.length === 1 && isValidElement(normalizedChildren[0]) && normalizedChildren[0].type === "a"
            ? normalizedChildren[0] as HeadingAnchorElement
            : null;

        const linkedHeading = linkedChild
            ? cloneElement(linkedChild, {
                className: cn("group no-underline hover:no-underline", linkedChild.props.className),
                children: (
                    <>
                        {linkedChild.props.children}
                        {marker}
                    </>
                ),
            })
            : null;

        return (
            <Tag
                id={id}
                className={cn(styles[level], "scroll-mt-20", className)}
                {...props}
            >
                {id ? (
                    linkedHeading ?? (
                    <a href={`#${id}`} className="group no-underline hover:no-underline">
                        {children}
                        {marker}
                    </a>
                    )
                ) : (
                    children
                )}
            </Tag>
        );
    };
}

export const H1 = createHeading(1);
export const H2 = createHeading(2);
export const H3 = createHeading(3);
export const H4 = createHeading(4);
export const H5 = createHeading(5);
export const H6 = createHeading(6);
