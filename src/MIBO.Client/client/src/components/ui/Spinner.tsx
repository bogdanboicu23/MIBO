import * as React from "react";
import { cn } from "../../utils/cn.ts";

export const Spinner = React.forwardRef<HTMLSpanElement, React.HTMLAttributes<HTMLSpanElement>>(
    ({ className, ...props }, ref) => (
        <span
            ref={ref}
            className={cn("inline-block h-4 w-4 animate-spin rounded-full border-2 border-current border-t-transparent", className)}
            {...props}
        />
    )
);
Spinner.displayName = "Spinner";
