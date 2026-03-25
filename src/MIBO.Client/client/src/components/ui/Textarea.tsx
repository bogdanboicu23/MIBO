import { forwardRef, type TextareaHTMLAttributes } from "react";
import { cn } from "../../utils/cn";

export const Textarea = forwardRef<HTMLTextAreaElement, TextareaHTMLAttributes<HTMLTextAreaElement>>(function Textarea(
    props,
    ref
) {
    const { className, ...rest } = props;
    return (
        <textarea
            ref={ref}
            className={cn(
                "w-full resize-none bg-transparent px-3 py-2 text-sm outline-none placeholder:text-zinc-400 dark:placeholder:text-zinc-500",
                className
            )}
            {...rest}
        />
    );
});
