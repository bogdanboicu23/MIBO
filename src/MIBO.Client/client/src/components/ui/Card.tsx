import * as React from "react";
import { cn } from "../../utils/cn.ts";

export type CardProps = React.HTMLAttributes<HTMLDivElement>;

export const Card = React.forwardRef<HTMLDivElement, CardProps>(({ className, ...props }, ref) => {
    return (
        <div
            ref={ref}
            className={cn(
                "rounded-2xl border border-zinc-200 bg-white shadow-sm " +
                "dark:border-zinc-800 dark:bg-zinc-950",
                className
            )}
            {...props}
        />
    );
});
Card.displayName = "Card";

export const CardHeader = React.forwardRef<HTMLDivElement, CardProps>(({ className, ...props }, ref) => (
    <div
        ref={ref}
        className={cn("border-b border-zinc-200 px-4 py-3 dark:border-zinc-800", className)}
        {...props}
    />
));
CardHeader.displayName = "CardHeader";

export const CardTitle = React.forwardRef<HTMLHeadingElement, React.HTMLAttributes<HTMLHeadingElement>>(
    ({ className, ...props }, ref) => (
        <h2 ref={ref} className={cn("text-sm font-semibold text-zinc-900 dark:text-zinc-100", className)} {...props} />
    )
);
CardTitle.displayName = "CardTitle";

export const CardDescription = React.forwardRef<HTMLParagraphElement, React.HTMLAttributes<HTMLParagraphElement>>(
    ({ className, ...props }, ref) => (
        <p ref={ref} className={cn("text-xs text-zinc-600 dark:text-zinc-400", className)} {...props} />
    )
);
CardDescription.displayName = "CardDescription";

export const CardContent = React.forwardRef<HTMLDivElement, CardProps>(({ className, ...props }, ref) => (
    <div ref={ref} className={cn("px-4 py-4", className)} {...props} />
));
CardContent.displayName = "CardContent";

export const CardFooter = React.forwardRef<HTMLDivElement, CardProps>(({ className, ...props }, ref) => (
    <div ref={ref} className={cn("border-t border-zinc-200 px-4 py-3 dark:border-zinc-800", className)} {...props} />
));
CardFooter.displayName = "CardFooter";
