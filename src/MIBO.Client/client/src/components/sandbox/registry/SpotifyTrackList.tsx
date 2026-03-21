import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";

interface Track {
    name?: string;
    artist?: string;
    album?: string;
    albumArt?: string;
    durationMs?: number;
    spotifyUrl?: string;
}

export function SpotifyTrackList({ props }: UiComponentProps) {
    const title = String(props?.title ?? "");
    const rawTracks = (props?.tracks ?? props?.items ?? []) as Track[];
    const tracks = Array.isArray(rawTracks) ? rawTracks : [];

    const formatDuration = (ms: number) => {
        const totalSec = Math.floor(ms / 1000);
        const min = Math.floor(totalSec / 60);
        const sec = totalSec % 60;
        return `${min}:${sec.toString().padStart(2, "0")}`;
    };

    if (tracks.length === 0) {
        return (
            <div className="rounded-2xl border border-zinc-200/70 bg-white p-5 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
                <p className="text-sm text-zinc-500 dark:text-zinc-400">No tracks found.</p>
            </div>
        );
    }

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
            {title && (
                <div className="border-b border-zinc-100 px-4 py-3 dark:border-zinc-800">
                    <h3 className="text-sm font-semibold">{title}</h3>
                </div>
            )}

            <div className="divide-y divide-zinc-100 dark:divide-zinc-800">
                {tracks.map((track, i) => (
                    <div key={i} className="flex items-center gap-3 px-4 py-2.5">
                        <span className="w-5 flex-shrink-0 text-right text-xs text-zinc-400 dark:text-zinc-500">
                            {i + 1}
                        </span>

                        {track.albumArt && (
                            <img
                                src={track.albumArt}
                                alt={track.album ?? ""}
                                className="h-10 w-10 flex-shrink-0 rounded-md object-cover"
                            />
                        )}

                        <div className="min-w-0 flex-1">
                            <div className="truncate text-sm font-medium leading-tight">
                                {track.spotifyUrl ? (
                                    <a
                                        href={track.spotifyUrl}
                                        target="_blank"
                                        rel="noopener noreferrer"
                                        className="hover:underline"
                                    >
                                        {track.name}
                                    </a>
                                ) : (
                                    track.name
                                )}
                            </div>
                            <div className="truncate text-xs text-zinc-500 dark:text-zinc-400">
                                {track.artist}
                                {track.album ? ` · ${track.album}` : ""}
                            </div>
                        </div>

                        {track.durationMs != null && track.durationMs > 0 && (
                            <span className="flex-shrink-0 text-xs text-zinc-400 dark:text-zinc-500">
                                {formatDuration(track.durationMs)}
                            </span>
                        )}
                    </div>
                ))}
            </div>
        </div>
    );
}
