import * as React from "react";
import { cn } from "../../utils/cn.ts";

export const Divider = React.forwardRef<HTMLHRElement, React.HTMLAttributes<HTMLHRElement>>(
    ({ className, ...props }, ref) => (
        <hr
            ref={ref}
            className={cn("border-0 border-t border-zinc-200 dark:border-zinc-800", className)}
            {...props}
        />
    )
);

Divider.displayName = "Divider";
