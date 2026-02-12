import * as React from "react";
import { cn } from "../../utils/cn.ts";

export type SelectProps = React.SelectHTMLAttributes<HTMLSelectElement> & {
    label?: string;
    hint?: string;
    error?: string;
};

export const Select = React.forwardRef<HTMLSelectElement, SelectProps>(
    ({ className, label, hint, error, id, children, ...props }, ref) => {
        const selectId = id ?? React.useId();

        return (
            <div className={cn("space-y-1", className)}>
                {label ? (
                    <label htmlFor={selectId} className="text-xs font-medium text-zinc-700 dark:text-zinc-300">
                        {label}
                    </label>
                ) : null}

                <select
                    ref={ref}
                    id={selectId}
                    className={cn(
                        "w-full rounded-xl border bg-white px-3 py-2 text-sm text-zinc-900 outline-none " +
                        "border-zinc-200 focus:border-zinc-400 " +
                        "dark:bg-zinc-950 dark:text-zinc-100 dark:border-zinc-800 dark:focus:border-zinc-600",
                        error ? "border-red-400 focus:border-red-500 dark:border-red-600 dark:focus:border-red-500" : undefined
                    )}
                    aria-invalid={!!error}
                    {...props}
                >
                    {children}
                </select>

                {error ? (
                    <div className="text-xs text-red-600 dark:text-red-400">{error}</div>
                ) : hint ? (
                    <div className="text-xs text-zinc-600 dark:text-zinc-400">{hint}</div>
                ) : null}
            </div>
        );
    }
);

Select.displayName = "Select";
