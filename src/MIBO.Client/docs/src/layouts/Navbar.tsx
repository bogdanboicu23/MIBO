import { Link, useLocation } from "react-router-dom";
import { cn } from "@/utils/cn";
import { siteConfig } from "@/config/site";

interface NavbarProps {
    onMenuToggle: () => void;
    onSearchOpen: () => void;
    theme: string;
    onThemeToggle: () => void;
}

export function Navbar({ onMenuToggle, onSearchOpen, theme, onThemeToggle }: NavbarProps) {
    const location = useLocation();

    return (
        <header className="fixed top-0 z-40 w-full border-b border-zinc-200 bg-white/80 backdrop-blur dark:border-zinc-800 dark:bg-zinc-950/80">
            <div className="flex h-14 items-center gap-4 px-4 lg:px-6">
                <button
                    onClick={onMenuToggle}
                    className="inline-flex items-center justify-center rounded-lg p-2 hover:bg-zinc-100 lg:hidden dark:hover:bg-zinc-800"
                    aria-label="Toggle menu"
                >
                    <svg className="h-5 w-5 text-zinc-700 dark:text-zinc-300" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                        <path strokeLinecap="round" strokeLinejoin="round" d="M4 6h16M4 12h16M4 18h16" />
                    </svg>
                </button>

                <Link to="/" className="flex items-center gap-2 font-semibold text-zinc-900 dark:text-zinc-100">
                    <svg className="h-6 w-6 text-zinc-700 dark:text-zinc-300" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                        <path strokeLinecap="round" strokeLinejoin="round" d="M12 6.253v13m0-13C10.832 5.477 9.246 5 7.5 5S4.168 5.477 3 6.253v13C4.168 18.477 5.754 18 7.5 18s3.332.477 4.5 1.253m0-13C13.168 5.477 14.754 5 16.5 5c1.747 0 3.332.477 4.5 1.253v13C19.832 18.477 18.247 18 16.5 18c-1.746 0-3.332.477-4.5 1.253" />
                    </svg>
                    {siteConfig.title}
                </Link>

                <nav className="hidden items-center gap-1 lg:flex">
                    {siteConfig.sections.map((s) => (
                        <Link
                            key={s.href}
                            to={s.href}
                            className={cn(
                                "rounded-lg px-3 py-1.5 text-sm transition",
                                location.pathname.startsWith(s.href)
                                    ? "bg-zinc-100 font-medium text-zinc-900 dark:bg-zinc-800 dark:text-zinc-100"
                                    : "text-zinc-600 hover:text-zinc-900 dark:text-zinc-400 dark:hover:text-zinc-100"
                            )}
                        >
                            {s.label}
                        </Link>
                    ))}
                </nav>

                <div className="ml-auto flex items-center gap-2">
                    <button
                        onClick={onSearchOpen}
                        className="inline-flex items-center gap-2 rounded-xl border border-zinc-200 bg-white px-3 py-1.5 text-sm text-zinc-500 transition hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-900 dark:hover:bg-zinc-800"
                    >
                        <svg className="h-4 w-4" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                            <path strokeLinecap="round" strokeLinejoin="round" d="M21 21l-6-6m2-5a7 7 0 11-14 0 7 7 0 0114 0z" />
                        </svg>
                        <span className="hidden sm:inline">Search...</span>
                        <kbd className="hidden rounded bg-zinc-100 px-1.5 py-0.5 text-xs font-medium text-zinc-500 sm:inline dark:bg-zinc-800">⌘K</kbd>
                    </button>

                    <button
                        onClick={onThemeToggle}
                        className="inline-flex h-9 w-9 items-center justify-center rounded-xl border border-zinc-200 bg-white hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-900 dark:hover:bg-zinc-800"
                        aria-label="Toggle theme"
                    >
                        {theme === "dark" ? (
                            <svg className="h-4 w-4 text-zinc-400" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                                <path strokeLinecap="round" strokeLinejoin="round" d="M12 3v1m0 16v1m9-9h-1M4 12H3m15.364 6.364l-.707-.707M6.343 6.343l-.707-.707m12.728 0l-.707.707M6.343 17.657l-.707.707M16 12a4 4 0 11-8 0 4 4 0 018 0z" />
                            </svg>
                        ) : (
                            <svg className="h-4 w-4 text-zinc-600" fill="none" viewBox="0 0 24 24" stroke="currentColor" strokeWidth={2}>
                                <path strokeLinecap="round" strokeLinejoin="round" d="M20.354 15.354A9 9 0 018.646 3.646 9.003 9.003 0 0012 21a9.003 9.003 0 008.354-5.646z" />
                            </svg>
                        )}
                    </button>
                </div>
            </div>
        </header>
    );
}
