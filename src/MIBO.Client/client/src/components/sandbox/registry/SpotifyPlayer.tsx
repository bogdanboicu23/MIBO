import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";

export function SpotifyPlayer({ props }: UiComponentProps) {
    const name = String(props?.name ?? "");
    const artist = String(props?.artist ?? "");
    const album = String(props?.album ?? "");
    const albumArt = String(props?.albumArt ?? "");
    const isPlaying = Boolean(props?.isPlaying);
    const progressMs = Number(props?.progressMs ?? 0);
    const durationMs = Number(props?.durationMs ?? 1);
    const spotifyUrl = String(props?.spotifyUrl ?? "");
    const message = String(props?.message ?? "");

    if (message && !name) {
        return (
            <div className="rounded-2xl border border-zinc-200/70 bg-white p-5 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
                <p className="text-sm text-zinc-500 dark:text-zinc-400">{message}</p>
            </div>
        );
    }

    const progressPercent = durationMs > 0 ? (progressMs / durationMs) * 100 : 0;
    const formatTime = (ms: number) => {
        const totalSec = Math.floor(ms / 1000);
        const min = Math.floor(totalSec / 60);
        const sec = totalSec % 60;
        return `${min}:${sec.toString().padStart(2, "0")}`;
    };

    return (
        <div className="overflow-hidden rounded-2xl border border-zinc-200/70 bg-white shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
            <div className="flex gap-4 p-4">
                {albumArt && (
                    <img
                        src={albumArt}
                        alt={album}
                        className="h-24 w-24 flex-shrink-0 rounded-xl object-cover shadow-md"
                    />
                )}

                <div className="flex min-w-0 flex-1 flex-col justify-between">
                    <div>
                        <div className="truncate text-base font-semibold leading-tight">{name}</div>
                        <div className="mt-0.5 truncate text-sm text-zinc-600 dark:text-zinc-400">{artist}</div>
                        <div className="mt-0.5 truncate text-xs text-zinc-400 dark:text-zinc-500">{album}</div>
                    </div>

                    <div className="mt-2 flex items-center gap-2">
                        <span
                            className={[
                                "inline-flex items-center gap-1 rounded-full px-2 py-0.5 text-xs font-medium",
                                isPlaying
                                    ? "bg-green-100 text-green-700 dark:bg-green-900/40 dark:text-green-400"
                                    : "bg-zinc-100 text-zinc-500 dark:bg-zinc-800 dark:text-zinc-400",
                            ].join(" ")}
                        >
                            <span className={isPlaying ? "animate-pulse" : ""}>●</span>
                            {isPlaying ? "Playing" : "Paused"}
                        </span>

                        {spotifyUrl && (
                            <a
                                href={spotifyUrl}
                                target="_blank"
                                rel="noopener noreferrer"
                                className="ml-auto text-xs font-medium text-green-600 hover:underline dark:text-green-400"
                            >
                                Open in Spotify ↗
                            </a>
                        )}
                    </div>
                </div>
            </div>

            {/* Progress bar */}
            <div className="px-4 pb-3">
                <div className="h-1 w-full rounded-full bg-zinc-100 dark:bg-zinc-800">
                    <div
                        className="h-full rounded-full bg-green-500 transition-all"
                        style={{ width: `${Math.min(progressPercent, 100)}%` }}
                    />
                </div>
                <div className="mt-1 flex justify-between text-[10px] text-zinc-400 dark:text-zinc-500">
                    <span>{formatTime(progressMs)}</span>
                    <span>{formatTime(durationMs)}</span>
                </div>
            </div>
        </div>
    );
}
