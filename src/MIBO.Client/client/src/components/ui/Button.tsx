import { cn } from "../../utils/cn";

export function Button(props: React.ButtonHTMLAttributes<HTMLButtonElement> & {
    variant?: "solid" | "outline" | "secondary" | "ghost";
    size?: "sm" | "md" | "lg";
}) {
    const { className, variant = "outline", size = "md", ...rest } = props;

    const sizeStyles = {
        sm: "px-2 py-1 text-xs",
        md: "px-3 py-2 text-sm",
        lg: "px-4 py-3 text-base"
    };

    const base =
        `inline-flex items-center justify-center rounded-xl ${sizeStyles[size]} font-medium transition`;

    let styles = "";
    switch (variant) {
        case "solid":
            styles = "bg-zinc-900 text-white hover:opacity-90 dark:bg-white dark:text-zinc-900";
            break;
        case "secondary":
            styles = "bg-zinc-100 text-zinc-900 hover:bg-zinc-200 dark:bg-zinc-800 dark:text-zinc-100 dark:hover:bg-zinc-700";
            break;
        case "ghost":
            styles = "hover:bg-zinc-100 dark:hover:bg-zinc-800";
            break;
        default: // outline
            styles = "border border-zinc-200 bg-white hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-900 dark:hover:bg-zinc-800";
    }

    return <button className={cn(base, styles, className)} {...rest} />;
}
