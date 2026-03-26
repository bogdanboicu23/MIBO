import { useState, useCallback, useEffect } from "react";
import { Outlet, useLocation } from "react-router-dom";
import { useTheme } from "@/hooks/useTheme";
import { Navbar } from "./Navbar";
import { Sidebar } from "./Sidebar";
import { RightSidebar } from "./RightSidebar";
import { Footer } from "./Footer";
import { MobileMenu } from "./MobileMenu";
import { SearchDialog } from "@/components/search/SearchDialog";

export function DocsLayout() {
    const { theme, setTheme } = useTheme();
    const location = useLocation();
    const [mobileOpen, setMobileOpen] = useState(false);
    const [searchOpen, setSearchOpen] = useState(false);

    const toggleTheme = useCallback(() => {
        setTheme(theme === "dark" ? "light" : "dark");
    }, [theme, setTheme]);

    useEffect(() => {
        function onKeyDown(e: KeyboardEvent) {
            if ((e.metaKey || e.ctrlKey) && e.key === "k") {
                e.preventDefault();
                setSearchOpen(true);
            }
        }
        document.addEventListener("keydown", onKeyDown);
        return () => document.removeEventListener("keydown", onKeyDown);
    }, []);

    useEffect(() => {
        window.scrollTo({ top: 0, left: 0, behavior: "auto" });
    }, [location.pathname]);

    return (
        <div className="min-h-screen bg-white text-zinc-900 dark:bg-zinc-950 dark:text-zinc-100">
            <Navbar
                onMenuToggle={() => setMobileOpen(true)}
                onSearchOpen={() => setSearchOpen(true)}
                theme={theme}
                onThemeToggle={toggleTheme}
            />
            <MobileMenu open={mobileOpen} onClose={() => setMobileOpen(false)} />
            <SearchDialog open={searchOpen} onClose={() => setSearchOpen(false)} />

            <div className="flex pt-14">
                <Sidebar />
                <main className="min-w-0 flex-1 bg-zinc-50 dark:bg-transparent">
                    <div className="mx-auto max-w-4xl px-6 py-10 lg:px-10">
                        <Outlet />
                    </div>
                    <Footer />
                </main>
                <RightSidebar />
            </div>
        </div>
    );
}
