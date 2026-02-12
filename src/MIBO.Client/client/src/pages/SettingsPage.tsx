import { Link } from "react-router-dom";
import { paths } from "../app/routes/paths";
import { useThemeContext } from "../app/providers/ThemeProvider";

export default function SettingsPage() {
    const { theme, setTheme } = useThemeContext();

    return (
        <div className="mx-auto max-w-3xl px-6 py-10">
            <div className="text-2xl font-semibold">Settings</div>
            <div className="mt-2 text-sm text-zinc-500 dark:text-zinc-400">
                Placeholder pentru setări (API keys, model config, tools).
            </div>

            <div className="mt-8">
                <div className="text-sm font-medium">Theme</div>

                <div className="mt-3 inline-flex rounded-2xl border border-zinc-200 bg-white p-1 dark:border-zinc-800 dark:bg-zinc-900">
                    <button
                        type="button"
                        onClick={() => setTheme("light")}
                        className={[
                            "rounded-xl px-3 py-1.5 text-sm transition",
                            theme === "light"
                                ? "bg-zinc-100 text-zinc-900 dark:bg-zinc-800 dark:text-zinc-100"
                                : "text-zinc-600 hover:bg-zinc-50 dark:text-zinc-300 dark:hover:bg-zinc-800/60",
                        ].join(" ")}
                    >
                        Light
                    </button>

                    <button
                        type="button"
                        onClick={() => setTheme("dark")}
                        className={[
                            "rounded-xl px-3 py-1.5 text-sm transition",
                            theme === "dark"
                                ? "bg-zinc-100 text-zinc-900 dark:bg-zinc-800 dark:text-zinc-100"
                                : "text-zinc-600 hover:bg-zinc-50 dark:text-zinc-300 dark:hover:bg-zinc-800/60",
                        ].join(" ")}
                    >
                        Dark
                    </button>
                </div>

                <div className="mt-2 text-xs text-zinc-500 dark:text-zinc-400">
                    Tema se salvează local (storage) și se aplică pe <code className="px-1">html</code> prin clasa <code className="px-1">dark</code>.
                </div>
            </div>

            <Link
                to={paths.chat}
                className="mt-10 inline-flex rounded-xl border border-zinc-200 bg-white px-4 py-2 text-sm hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-900 dark:hover:bg-zinc-800"
            >
                ← Back to chat
            </Link>
        </div>
    );
}
