import { Link, useSearchParams } from "react-router-dom";
import { paths } from "../app/routes/paths";
import { useThemeContext } from "../app/providers/useThemeContext";
import { useEffect, useState, useCallback } from "react";
import { endpoints } from "../axios/endpoints";
import { useAxios } from "../axios/hooks";

export default function SettingsPage() {
    const { theme, setTheme } = useThemeContext();
    const { api } = useAxios();
    const [searchParams, setSearchParams] = useSearchParams();
    const [spotifyConnected, setSpotifyConnected] = useState<boolean | null>(null);
    const [spotifyLoading, setSpotifyLoading] = useState(false);
    const [financeConnected, setFinanceConnected] = useState<boolean | null>(null);
    const [financeLoading, setFinanceLoading] = useState(false);

    const checkSpotifyStatus = useCallback(async () => {
        try {
            const data = await api.get<{ connected: boolean }>(endpoints.spotify.status);
            setSpotifyConnected(data.connected);
        } catch {
            setSpotifyConnected(false);
        }
    }, [api]);

    const checkFinanceStatus = useCallback(async () => {
        try {
            const data = await api.get<{ connected: boolean }>(endpoints.finance.status);
            setFinanceConnected(data.connected);
        } catch {
            setFinanceConnected(false);
        }
    }, [api]);

    useEffect(() => {
        checkSpotifyStatus();
        checkFinanceStatus();
    }, [checkSpotifyStatus, checkFinanceStatus]);

    useEffect(() => {
        if (searchParams.get("spotify") === "connected") {
            setSpotifyConnected(true);
            searchParams.delete("spotify");
            setSearchParams(searchParams, { replace: true });
        }
    }, [searchParams, setSearchParams]);

    const handleSpotifyConnect = async () => {
        setSpotifyLoading(true);
        try {
            const data = await api.get<{ url: string }>(endpoints.spotify.authorize);
            window.location.href = data.url;
        } catch {
            setSpotifyLoading(false);
        }
    };

    const handleSpotifyDisconnect = async () => {
        setSpotifyLoading(true);
        try {
            await api.delete(endpoints.spotify.disconnect);
            setSpotifyConnected(false);
        } finally {
            setSpotifyLoading(false);
        }
    };

    const handleFinanceConnect = async () => {
        setFinanceLoading(true);
        try {
            await api.post(endpoints.finance.connect);
            setFinanceConnected(true);
        } finally {
            setFinanceLoading(false);
        }
    };

    const handleFinanceDisconnect = async () => {
        setFinanceLoading(true);
        try {
            await api.delete(endpoints.finance.disconnect);
            setFinanceConnected(false);
        } finally {
            setFinanceLoading(false);
        }
    };

    return (
        <div className="mx-auto max-w-3xl px-6 py-10">
            <div className="text-2xl font-semibold">Settings</div>
            <div className="mt-2 text-sm text-zinc-500 dark:text-zinc-400">
                Manage your preferences and connected services.
            </div>

            {/* Theme */}
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
            </div>

            {/* Connected Services */}
            <div className="mt-10">
                <div className="text-sm font-medium">Connected Services</div>

                <div className="mt-3 rounded-2xl border border-zinc-200 bg-white p-4 dark:border-zinc-800 dark:bg-zinc-900">
                    <div className="flex items-center justify-between">
                        <div className="flex items-center gap-3">
                            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-green-100 dark:bg-green-900/40">
                                <svg viewBox="0 0 24 24" className="h-5 w-5 text-green-600 dark:text-green-400" fill="currentColor">
                                    <path d="M12 0C5.4 0 0 5.4 0 12s5.4 12 12 12 12-5.4 12-12S18.66 0 12 0zm5.521 17.34c-.24.359-.66.48-1.021.24-2.82-1.74-6.36-2.101-10.561-1.141-.418.122-.779-.179-.899-.539-.12-.421.18-.78.54-.9 4.56-1.021 8.52-.6 11.64 1.32.42.18.479.659.301 1.02zm1.44-3.3c-.301.42-.841.6-1.262.3-3.239-1.98-8.159-2.58-11.939-1.38-.479.12-1.02-.12-1.14-.6-.12-.48.12-1.021.6-1.141C9.6 9.9 15 10.561 18.72 12.84c.361.181.54.78.241 1.2zm.12-3.36C15.24 8.4 8.82 8.16 5.16 9.301c-.6.179-1.2-.181-1.38-.721-.18-.601.18-1.2.72-1.381 4.26-1.26 11.28-1.02 15.721 1.621.539.3.719 1.02.419 1.56-.299.421-1.02.599-1.559.3z"/>
                                </svg>
                            </div>
                            <div>
                                <div className="text-sm font-medium">Spotify</div>
                                <div className="text-xs text-zinc-500 dark:text-zinc-400">
                                    {spotifyConnected === null
                                        ? "Checking..."
                                        : spotifyConnected
                                            ? "Connected"
                                            : "Not connected"}
                                </div>
                            </div>
                        </div>

                        {spotifyConnected ? (
                            <button
                                type="button"
                                onClick={handleSpotifyDisconnect}
                                disabled={spotifyLoading}
                                className="rounded-xl border border-red-200 bg-white px-3 py-1.5 text-xs font-medium text-red-600 transition hover:bg-red-50 disabled:opacity-50 dark:border-red-800 dark:bg-zinc-900 dark:text-red-400 dark:hover:bg-red-900/20"
                            >
                                Disconnect
                            </button>
                        ) : (
                            <button
                                type="button"
                                onClick={handleSpotifyConnect}
                                disabled={spotifyLoading || spotifyConnected === null}
                                className="rounded-xl bg-green-600 px-3 py-1.5 text-xs font-medium text-white transition hover:bg-green-700 disabled:opacity-50"
                            >
                                Connect
                            </button>
                        )}
                    </div>
                </div>

                {/* Finance */}
                <div className="mt-3 rounded-2xl border border-zinc-200 bg-white p-4 dark:border-zinc-800 dark:bg-zinc-900">
                    <div className="flex items-center justify-between">
                        <div className="flex items-center gap-3">
                            <div className="flex h-10 w-10 items-center justify-center rounded-xl bg-blue-100 dark:bg-blue-900/40">
                                <svg viewBox="0 0 24 24" className="h-5 w-5 text-blue-600 dark:text-blue-400" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
                                    <rect x="1" y="4" width="22" height="16" rx="2" ry="2" />
                                    <line x1="1" y1="10" x2="23" y2="10" />
                                </svg>
                            </div>
                            <div>
                                <div className="text-sm font-medium">Finance</div>
                                <div className="text-xs text-zinc-500 dark:text-zinc-400">
                                    {financeConnected === null
                                        ? "Checking..."
                                        : financeConnected
                                            ? "Connected"
                                            : "Not connected"}
                                </div>
                            </div>
                        </div>

                        {financeConnected ? (
                            <button
                                type="button"
                                onClick={handleFinanceDisconnect}
                                disabled={financeLoading}
                                className="rounded-xl border border-red-200 bg-white px-3 py-1.5 text-xs font-medium text-red-600 transition hover:bg-red-50 disabled:opacity-50 dark:border-red-800 dark:bg-zinc-900 dark:text-red-400 dark:hover:bg-red-900/20"
                            >
                                Disconnect
                            </button>
                        ) : (
                            <button
                                type="button"
                                onClick={handleFinanceConnect}
                                disabled={financeLoading || financeConnected === null}
                                className="rounded-xl bg-blue-600 px-3 py-1.5 text-xs font-medium text-white transition hover:bg-blue-700 disabled:opacity-50"
                            >
                                Connect
                            </button>
                        )}
                    </div>
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
