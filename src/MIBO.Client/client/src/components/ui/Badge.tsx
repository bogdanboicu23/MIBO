import * as React from "react";
import { cn } from "../../utils/cn.ts";

export type BadgeVariant = "default" | "subtle" | "outline";

export type BadgeProps = React.HTMLAttributes<HTMLSpanElement> & {
    variant?: BadgeVariant;
};

const variants: Record<BadgeVariant, string> = {
    default: "bg-zinc-900 text-white dark:bg-zinc-100 dark:text-zinc-900",
    subtle: "bg-zinc-100 text-zinc-800 dark:bg-zinc-900 dark:text-zinc-200",
    outline: "border border-zinc-200 text-zinc-700 dark:border-zinc-800 dark:text-zinc-200",
};

export const Badge = React.forwardRef<HTMLSpanElement, BadgeProps>(
    ({ className, variant = "outline", ...props }, ref) => {
        return (
            <span
                ref={ref}
                className={cn(
                    "inline-flex items-center rounded-full px-2 py-0.5 text-xs font-medium",
                    variants[variant],
                    className
                )}
                {...props}
            />
        );
    }
);

Badge.displayName = "Badge";
