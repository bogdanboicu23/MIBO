import * as React from "react";
import { cn } from "../../utils/cn.ts";

export const Table = React.forwardRef<HTMLTableElement, React.TableHTMLAttributes<HTMLTableElement>>(
    ({ className, ...props }, ref) => (
        <table
            ref={ref}
            className={cn("min-w-full divide-y divide-zinc-200 dark:divide-zinc-800", className)}
            {...props}
        />
    )
);
Table.displayName = "Table";

export const THead = React.forwardRef<HTMLTableSectionElement, React.HTMLAttributes<HTMLTableSectionElement>>(
    ({ className, ...props }, ref) => (
        <thead ref={ref} className={cn("bg-zinc-50 dark:bg-zinc-900", className)} {...props} />
    )
);
THead.displayName = "THead";

export const TBody = React.forwardRef<HTMLTableSectionElement, React.HTMLAttributes<HTMLTableSectionElement>>(
    ({ className, ...props }, ref) => (
        <tbody
            ref={ref}
            className={cn("divide-y divide-zinc-200 bg-white dark:divide-zinc-800 dark:bg-zinc-950", className)}
            {...props}
        />
    )
);
TBody.displayName = "TBody";

export const TR = React.forwardRef<HTMLTableRowElement, React.HTMLAttributes<HTMLTableRowElement>>(
    ({ className, ...props }, ref) => (
        <tr ref={ref} className={cn("hover:bg-zinc-50 dark:hover:bg-zinc-900/40", className)} {...props} />
    )
);
TR.displayName = "TR";

export const TH = React.forwardRef<HTMLTableCellElement, React.ThHTMLAttributes<HTMLTableCellElement>>(
    ({ className, ...props }, ref) => (
        <th
            ref={ref}
            className={cn("px-4 py-3 text-left text-xs font-semibold text-zinc-700 dark:text-zinc-200", className)}
            {...props}
        />
    )
);
TH.displayName = "TH";

export const TD = React.forwardRef<HTMLTableCellElement, React.TdHTMLAttributes<HTMLTableCellElement>>(
    ({ className, ...props }, ref) => (
        <td ref={ref} className={cn("px-4 py-3 text-sm text-zinc-800 dark:text-zinc-200", className)} {...props} />
    )
);
TD.displayName = "TD";
