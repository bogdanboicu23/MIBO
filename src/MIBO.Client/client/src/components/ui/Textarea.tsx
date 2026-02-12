import { cn } from "../../utils/cn";

export function Textarea(props: React.TextareaHTMLAttributes<HTMLTextAreaElement>) {
    const { className, ...rest } = props;
    return (
        <textarea
            className={cn(
                "w-full resize-none bg-transparent px-3 py-2 text-sm outline-none placeholder:text-zinc-400 dark:placeholder:text-zinc-500",
                className
            )}
            {...rest}
        />
    );
}
