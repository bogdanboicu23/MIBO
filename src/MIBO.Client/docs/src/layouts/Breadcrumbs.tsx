import { Link, useLocation } from "react-router-dom";

export function Breadcrumbs() {
    const location = useLocation();
    const segments = location.pathname.split("/").filter(Boolean);

    if (segments.length === 0) return null;

    const crumbs = segments.map((seg, i) => ({
        label: seg.replace(/-/g, " ").replace(/\b\w/g, (c) => c.toUpperCase()),
        href: "/" + segments.slice(0, i + 1).join("/"),
    }));

    return (
        <nav className="mb-4 flex items-center gap-1.5 text-sm text-zinc-500 dark:text-zinc-400">
            <Link to="/" className="hover:text-zinc-700 dark:hover:text-zinc-200">Docs</Link>
            {crumbs.map((crumb, i) => (
                <span key={crumb.href} className="flex items-center gap-1.5">
                    <svg className="h-3 w-3" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                        <path strokeLinecap="round" strokeLinejoin="round" d="M9 5l7 7-7 7" />
                    </svg>
                    {i === crumbs.length - 1 ? (
                        <span className="font-medium text-zinc-900 dark:text-zinc-100">{crumb.label}</span>
                    ) : (
                        <Link to={crumb.href} className="hover:text-zinc-700 dark:hover:text-zinc-200">{crumb.label}</Link>
                    )}
                </span>
            ))}
        </nav>
    );
}
