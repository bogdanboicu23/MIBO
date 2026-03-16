import { useEffect, useMemo, useRef, useState } from "react";

type ColorCustomizerItem = {
    key: string;
    label: string;
    color: string;
};

type ColorCustomizerSection = {
    title: string;
    items: ColorCustomizerItem[];
    onChange: (key: string, color: string) => void;
};

type ColorCustomizerProps = {
    sections: ColorCustomizerSection[];
    onReset?: () => void;
};

const HEX_3_COLOR_REGEX = /^#([0-9a-f]{3})$/i;
const HEX_6_COLOR_REGEX = /^#([0-9a-f]{6})$/i;
const FALLBACK_COLOR = "#6366f1";

export function ColorCustomizer({ sections, onReset }: ColorCustomizerProps) {
    const availableSections = sections.filter((section) => section.items.length > 0);
    const [open, setOpen] = useState(false);
    const rootRef = useRef<HTMLDivElement>(null);

    const editableCount = availableSections.reduce((total, section) => total + section.items.length, 0);
    const previewColors = useMemo(
        () =>
            availableSections
                .flatMap((section) => section.items.map((item) => item.color))
                .filter(Boolean)
                .slice(0, 3),
        [availableSections]
    );

    useEffect(() => {
        if (!open) return undefined;

        const handlePointerDown = (event: MouseEvent) => {
            if (!rootRef.current?.contains(event.target as Node)) {
                setOpen(false);
            }
        };

        const handleEscape = (event: KeyboardEvent) => {
            if (event.key === "Escape") {
                setOpen(false);
            }
        };

        document.addEventListener("mousedown", handlePointerDown);
        document.addEventListener("keydown", handleEscape);

        return () => {
            document.removeEventListener("mousedown", handlePointerDown);
            document.removeEventListener("keydown", handleEscape);
        };
    }, [open]);

    if (availableSections.length === 0) {
        return null;
    }

    return (
        <div ref={rootRef} className="relative flex-shrink-0">
            <button
                type="button"
                className="inline-flex items-center gap-2 rounded-full border border-zinc-200 bg-white px-2.5 py-1.5 text-[11px] font-medium text-zinc-700 shadow-sm transition-colors hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-200 dark:hover:bg-zinc-800"
                onClick={() => setOpen((current) => !current)}
            >
                <span className="inline-flex items-center gap-1.5">
                    <PaletteIcon />
                    Colors
                </span>
                <span className="flex items-center -space-x-1">
                    {previewColors.map((color, index) => (
                        <span
                            key={`${color}-${index}`}
                            className="h-3 w-3 rounded-full border border-white dark:border-zinc-900"
                            style={{ background: color }}
                        />
                    ))}
                </span>
            </button>

            {open && (
                <div className="absolute right-0 top-[calc(100%+10px)] z-20 w-[320px] overflow-hidden rounded-2xl border border-zinc-200 bg-white shadow-[0_20px_60px_rgba(15,23,42,0.14)] dark:border-zinc-800 dark:bg-zinc-950">
                    <div className="flex items-start justify-between gap-3 border-b border-zinc-200 px-4 py-3 dark:border-zinc-800">
                        <div>
                            <div className="text-sm font-semibold text-zinc-900 dark:text-zinc-100">Color editor</div>
                            <div className="text-[11px] text-zinc-500 dark:text-zinc-400">
                                {editableCount} editable {editableCount === 1 ? "item" : "items"}
                            </div>
                        </div>

                        <div className="flex items-center gap-2">
                            {onReset && (
                                <button
                                    type="button"
                                    className="rounded-full border border-zinc-200 px-2 py-1 text-[10px] font-medium uppercase tracking-[0.12em] text-zinc-500 transition-colors hover:bg-zinc-50 dark:border-zinc-800 dark:text-zinc-400 dark:hover:bg-zinc-900"
                                    onClick={onReset}
                                >
                                    Reset
                                </button>
                            )}
                            <button
                                type="button"
                                className="inline-flex h-7 w-7 items-center justify-center rounded-full border border-zinc-200 text-zinc-500 transition-colors hover:bg-zinc-50 dark:border-zinc-800 dark:text-zinc-400 dark:hover:bg-zinc-900"
                                onClick={() => setOpen(false)}
                                aria-label="Close color editor"
                            >
                                <CloseIcon />
                            </button>
                        </div>
                    </div>

                    <div className="max-h-[360px] space-y-4 overflow-y-auto p-4">
                        {availableSections.map((section, sectionIndex) => (
                            <div key={`${section.title}-${sectionIndex}`} className="space-y-2.5">
                                {availableSections.length > 1 && (
                                    <div className="text-[10px] font-semibold uppercase tracking-[0.16em] text-zinc-400 dark:text-zinc-500">
                                        {section.title}
                                    </div>
                                )}

                                <div className="space-y-2">
                                    {section.items.map((item) => (
                                        <label
                                            key={item.key}
                                            className="flex items-center gap-3 rounded-xl border border-zinc-200 bg-zinc-50 px-3 py-2.5 transition-colors hover:border-zinc-300 dark:border-zinc-800 dark:bg-zinc-900/70 dark:hover:border-zinc-700"
                                        >
                                            <span
                                                className="h-4 w-4 flex-shrink-0 rounded-full border border-black/10 shadow-[inset_0_0_0_1px_rgba(255,255,255,0.25)]"
                                                style={{ background: item.color }}
                                            />
                                            <span className="min-w-0 flex-1">
                                                <span className="block truncate text-xs font-medium text-zinc-800 dark:text-zinc-100">
                                                    {item.label}
                                                </span>
                                                <span className="block text-[10px] uppercase tracking-[0.14em] text-zinc-400 dark:text-zinc-500">
                                                    {item.color}
                                                </span>
                                            </span>
                                            <input
                                                type="color"
                                                value={toColorInputValue(item.color)}
                                                onChange={(event) => section.onChange(item.key, event.target.value)}
                                                className="h-9 w-11 cursor-pointer rounded-lg border border-zinc-300 bg-white p-1 dark:border-zinc-700 dark:bg-zinc-950"
                                            />
                                        </label>
                                    ))}
                                </div>
                            </div>
                        ))}
                    </div>
                </div>
            )}
        </div>
    );
}

function PaletteIcon() {
    return (
        <svg width="12" height="12" viewBox="0 0 16 16" fill="none" aria-hidden="true">
            <path
                d="M8 1.5a6.5 6.5 0 1 0 0 13h1.25a1.75 1.75 0 0 0 0-3.5H8.9A1.4 1.4 0 0 1 7.5 9.6c0-.77.63-1.4 1.4-1.4h1.35A4.25 4.25 0 0 0 14.5 3.95 6.44 6.44 0 0 0 8 1.5Z"
                stroke="currentColor"
                strokeWidth="1.25"
            />
            <circle cx="5" cy="5.25" r="1" fill="currentColor" />
            <circle cx="8" cy="4" r="1" fill="currentColor" />
            <circle cx="10.75" cy="5.25" r="1" fill="currentColor" />
        </svg>
    );
}

function CloseIcon() {
    return (
        <svg width="12" height="12" viewBox="0 0 16 16" fill="none" aria-hidden="true">
            <path
                d="M4 4l8 8M12 4l-8 8"
                stroke="currentColor"
                strokeWidth="1.5"
                strokeLinecap="round"
            />
        </svg>
    );
}

function toColorInputValue(color: string): string {
    const normalized = String(color ?? "").trim();

    if (HEX_6_COLOR_REGEX.test(normalized)) {
        return normalized;
    }

    const shortHex = HEX_3_COLOR_REGEX.exec(normalized);
    if (!shortHex) {
        return FALLBACK_COLOR;
    }

    const [r, g, b] = shortHex[1].split("");
    return `#${r}${r}${g}${g}${b}${b}`;
}
