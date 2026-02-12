import { cn } from "../../utils/cn";

export function Button(props: React.ButtonHTMLAttributes<HTMLButtonElement> & { variant?: "solid" | "outline" }) {
    const { className, variant = "outline", ...rest } = props;

    const base =
        "inline-flex items-center justify-center rounded-xl px-3 py-2 text-sm font-medium transition";
    const styles =
        variant === "solid"
            ? "bg-zinc-900 text-white hover:opacity-90 dark:bg-white dark:text-zinc-900"
            : "border border-zinc-200 bg-white hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-900 dark:hover:bg-zinc-800";

    return <button className={cn(base, styles, className)} {...rest} />;
}
