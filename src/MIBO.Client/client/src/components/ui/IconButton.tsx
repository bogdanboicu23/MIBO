import { cn } from "../../utils/cn";

export function IconButton(props: React.ButtonHTMLAttributes<HTMLButtonElement>) {
    const { className, ...rest } = props;
    return (
        <button
            className={cn(
                "inline-flex h-10 w-10 items-center justify-center rounded-xl border border-zinc-200 bg-white hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-900 dark:hover:bg-zinc-800",
                className
            )}
            {...rest}
        />
    );
}
