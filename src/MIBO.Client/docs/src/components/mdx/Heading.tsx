import { cn } from "@/utils/cn";

type Level = 1 | 2 | 3 | 4 | 5 | 6;

const styles: Record<Level, string> = {
    1: "text-3xl font-extrabold tracking-tight mt-0 mb-6 text-zinc-950 dark:text-white",
    2: "text-2xl font-bold tracking-tight mt-12 mb-4 pb-3 border-b border-zinc-200 dark:border-zinc-800 text-zinc-900 dark:text-zinc-50",
    3: "text-lg font-semibold tracking-tight mt-8 mb-3 text-zinc-800 dark:text-zinc-200",
    4: "text-base font-semibold mt-6 mb-2 text-zinc-800 dark:text-zinc-200",
    5: "text-sm font-semibold mt-4 mb-2 text-zinc-700 dark:text-zinc-300",
    6: "text-sm font-medium mt-4 mb-2 text-zinc-600 dark:text-zinc-400",
};

function createHeading(level: Level) {
    const Tag = `h${level}` as const;

    return function Heading({ children, id, className, ...props }: React.HTMLAttributes<HTMLHeadingElement>) {
        return (
            <Tag
                id={id}
                className={cn(styles[level], "scroll-mt-20", className)}
                {...props}
            >
                {id ? (
                    <a href={`#${id}`} className="group no-underline hover:no-underline">
                        {children}
                        <span className="ml-2 text-zinc-300 opacity-0 transition-opacity group-hover:opacity-100 dark:text-zinc-600">#</span>
                    </a>
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
