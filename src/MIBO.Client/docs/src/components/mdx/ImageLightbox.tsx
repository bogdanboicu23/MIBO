import type { ImgHTMLAttributes, KeyboardEvent as ReactKeyboardEvent, PointerEvent as ReactPointerEvent, WheelEvent as ReactWheelEvent } from "react";
import { createContext, useCallback, useContext, useEffect, useMemo, useRef, useState } from "react";
import { createPortal } from "react-dom";
import { cn } from "@/utils/cn";

interface LightboxImage {
    src: string;
    alt?: string;
    title?: string;
}

interface ImageLightboxContextValue {
    openImage: (image: LightboxImage) => void;
}

interface DragState {
    pointerId: number;
    startX: number;
    startY: number;
    originX: number;
    originY: number;
}

interface PointerSnapshot {
    x: number;
    y: number;
}

interface PinchState {
    initialDistance: number;
    initialZoom: number;
}

const MIN_ZOOM = 1;
const MAX_ZOOM = 6;
const ZOOM_STEP = 0.35;

const ImageLightboxContext = createContext<ImageLightboxContextValue | null>(null);

function clamp(value: number, min: number, max: number) {
    return Math.min(max, Math.max(min, value));
}

function clampZoom(value: number) {
    return clamp(Number(value.toFixed(2)), MIN_ZOOM, MAX_ZOOM);
}

function getDistance(a: PointerSnapshot, b: PointerSnapshot) {
    const dx = a.x - b.x;
    const dy = a.y - b.y;
    return Math.hypot(dx, dy);
}

export function ImageLightboxProvider({ children }: { children: React.ReactNode }) {
    const [image, setImage] = useState<LightboxImage | null>(null);
    const [zoom, setZoom] = useState(1);
    const [offset, setOffset] = useState({ x: 0, y: 0 });
    const [dragState, setDragState] = useState<DragState | null>(null);
    const imageRef = useRef<HTMLImageElement | null>(null);
    const activePointersRef = useRef(new Map<number, PointerSnapshot>());
    const pinchStateRef = useRef<PinchState | null>(null);

    const clampOffset = useCallback((nextOffset: { x: number; y: number }, nextZoom: number) => {
        const baseWidth = imageRef.current?.clientWidth ?? 0;
        const baseHeight = imageRef.current?.clientHeight ?? 0;
        const maxX = Math.max(0, (baseWidth * (nextZoom - 1)) / 2);
        const maxY = Math.max(0, (baseHeight * (nextZoom - 1)) / 2);

        return {
            x: clamp(nextOffset.x, -maxX, maxX),
            y: clamp(nextOffset.y, -maxY, maxY),
        };
    }, []);

    const resetView = useCallback(() => {
        setZoom(1);
        setOffset({ x: 0, y: 0 });
        setDragState(null);
        activePointersRef.current.clear();
        pinchStateRef.current = null;
    }, []);

    const closeImage = useCallback(() => {
        resetView();
        setImage(null);
    }, [resetView]);

    const updateZoom = useCallback((updater: number | ((prev: number) => number)) => {
        setZoom((prevZoom) => {
            const candidate = typeof updater === "function" ? updater(prevZoom) : updater;
            const nextZoom = clampZoom(candidate);

            setOffset((currentOffset) => (nextZoom === 1
                ? { x: 0, y: 0 }
                : clampOffset(currentOffset, nextZoom)));

            return nextZoom;
        });
    }, [clampOffset]);

    const zoomIn = useCallback(() => {
        updateZoom((prevZoom) => prevZoom + ZOOM_STEP);
    }, [updateZoom]);

    const zoomOut = useCallback(() => {
        updateZoom((prevZoom) => prevZoom - ZOOM_STEP);
    }, [updateZoom]);

    const openImage = useCallback((nextImage: LightboxImage) => {
        setImage(nextImage);
        setZoom(1);
        setOffset({ x: 0, y: 0 });
        setDragState(null);
        activePointersRef.current.clear();
        pinchStateRef.current = null;
    }, []);

    useEffect(() => {
        if (!image) {
            return;
        }

        const previousOverflow = document.body.style.overflow;
        document.body.style.overflow = "hidden";

        return () => {
            document.body.style.overflow = previousOverflow;
        };
    }, [image]);

    useEffect(() => {
        if (!image) {
            return;
        }

        const onKeyDown = (event: KeyboardEvent) => {
            if (event.key === "Escape") {
                event.preventDefault();
                closeImage();
                return;
            }

            if (event.key === "+" || event.key === "=") {
                event.preventDefault();
                zoomIn();
                return;
            }

            if (event.key === "-" || event.key === "_") {
                event.preventDefault();
                zoomOut();
                return;
            }

            if (event.key === "0") {
                event.preventDefault();
                resetView();
            }
        };

        window.addEventListener("keydown", onKeyDown);
        return () => window.removeEventListener("keydown", onKeyDown);
    }, [closeImage, image, resetView, zoomIn, zoomOut]);

    const handleWheel = useCallback((event: ReactWheelEvent<HTMLDivElement>) => {
        event.preventDefault();
        updateZoom((prevZoom) => prevZoom + (event.deltaY < 0 ? ZOOM_STEP : -ZOOM_STEP));
    }, [updateZoom]);

    const handlePointerDown = useCallback((event: ReactPointerEvent<HTMLDivElement>) => {
        activePointersRef.current.set(event.pointerId, { x: event.clientX, y: event.clientY });

        if (event.pointerType === "touch") {
            event.preventDefault();
        }

        const pointers = Array.from(activePointersRef.current.values());
        if (pointers.length >= 2) {
            pinchStateRef.current = {
                initialDistance: getDistance(pointers[0], pointers[1]),
                initialZoom: zoom,
            };
            setDragState(null);
            return;
        }

        if (zoom <= 1) {
            return;
        }

        event.preventDefault();
        event.currentTarget.setPointerCapture(event.pointerId);
        setDragState({
            pointerId: event.pointerId,
            startX: event.clientX,
            startY: event.clientY,
            originX: offset.x,
            originY: offset.y,
        });
    }, [offset.x, offset.y, zoom]);

    const handlePointerMove = useCallback((event: ReactPointerEvent<HTMLDivElement>) => {
        if (activePointersRef.current.has(event.pointerId)) {
            activePointersRef.current.set(event.pointerId, { x: event.clientX, y: event.clientY });
        }

        const pointers = Array.from(activePointersRef.current.values());
        if (pinchStateRef.current && pointers.length >= 2) {
            event.preventDefault();
            const distance = getDistance(pointers[0], pointers[1]);
            if (distance > 0) {
                updateZoom(pinchStateRef.current.initialZoom * (distance / pinchStateRef.current.initialDistance));
            }
            return;
        }

        if (!dragState || dragState.pointerId !== event.pointerId) {
            return;
        }

        event.preventDefault();
        const nextOffset = {
            x: dragState.originX + (event.clientX - dragState.startX),
            y: dragState.originY + (event.clientY - dragState.startY),
        };

        setOffset(clampOffset(nextOffset, zoom));
    }, [clampOffset, dragState, updateZoom, zoom]);

    const finishDrag = useCallback((event: ReactPointerEvent<HTMLDivElement>) => {
        activePointersRef.current.delete(event.pointerId);
        if (activePointersRef.current.size < 2) {
            pinchStateRef.current = null;
        }

        if (event.currentTarget.hasPointerCapture(event.pointerId)) {
            event.currentTarget.releasePointerCapture(event.pointerId);
        }
        setDragState(null);
    }, []);

    const contextValue = useMemo<ImageLightboxContextValue>(() => ({ openImage }), [openImage]);

    return (
        <ImageLightboxContext.Provider value={contextValue}>
            {children}

            {image && typeof document !== "undefined"
                ? createPortal(
                    <div
                        className="fixed inset-0 z-[200] bg-black/88 backdrop-blur-md"
                        role="dialog"
                        aria-modal="true"
                        aria-label={image.alt || image.title || "Image viewer"}
                        onClick={closeImage}
                    >
                        <div className="absolute inset-0 p-2 sm:p-4 md:p-6">
                            <div
                                className={cn(
                                    "flex h-full w-full items-center justify-center overflow-hidden rounded-[1.5rem] border border-white/10 bg-white/[0.04] shadow-[0_40px_120px_rgba(0,0,0,0.45)] sm:rounded-[2rem]",
                                    zoom > 1 ? "cursor-grab touch-none" : "touch-none cursor-default",
                                    dragState ? "cursor-grabbing" : ""
                                )}
                                onClick={(event) => event.stopPropagation()}
                                onWheel={handleWheel}
                                onPointerDown={handlePointerDown}
                                onPointerMove={handlePointerMove}
                                onPointerUp={finishDrag}
                                onPointerCancel={finishDrag}
                            >
                                <img
                                    ref={imageRef}
                                    src={image.src}
                                    alt={image.alt || ""}
                                    className={cn(
                                        "max-h-[calc(100%-1.5rem)] max-w-[calc(100%-1.5rem)] select-none object-contain shadow-2xl sm:max-h-full sm:max-w-full",
                                        dragState ? "transition-none" : "transition-transform duration-150 ease-out"
                                    )}
                                    style={{
                                        transform: `translate(${offset.x}px, ${offset.y}px) scale(${zoom})`,
                                        transformOrigin: "center center",
                                    }}
                                    draggable={false}
                                    onLoad={() => {
                                        setOffset((currentOffset) => clampOffset(currentOffset, zoom));
                                    }}
                                    onDoubleClick={() => {
                                        updateZoom((prevZoom) => (prevZoom > 1 ? 1 : 2));
                                    }}
                                />
                            </div>
                        </div>
                    </div>,
                    document.body
                )
                : null}
        </ImageLightboxContext.Provider>
    );
}

export function MdxImage(props: ImgHTMLAttributes<HTMLImageElement>) {
    const context = useContext(ImageLightboxContext);
    const { src, alt = "", title, className, onClick, onKeyDown, ...rest } = props;
    const canOpen = Boolean(context && typeof src === "string" && src.length > 0);

    const openCurrentImage = useCallback(() => {
        if (!canOpen || !context || typeof src !== "string") {
            return;
        }

        context.openImage({
            src,
            alt,
            title: typeof title === "string" ? title : undefined,
        });
    }, [alt, canOpen, context, src, title]);

    const handleClick = useCallback((event: React.MouseEvent<HTMLImageElement>) => {
        onClick?.(event);
        if (!event.defaultPrevented) {
            openCurrentImage();
        }
    }, [onClick, openCurrentImage]);

    const handleKeyDown = useCallback((event: ReactKeyboardEvent<HTMLImageElement>) => {
        onKeyDown?.(event);
        if (event.defaultPrevented || !canOpen) {
            return;
        }

        if (event.key === "Enter" || event.key === " ") {
            event.preventDefault();
            openCurrentImage();
        }
    }, [canOpen, onKeyDown, openCurrentImage]);

    return (
        <img
            src={src}
            alt={alt}
            title={title}
            role={canOpen ? "button" : rest.role}
            tabIndex={canOpen ? 0 : rest.tabIndex}
            onClick={handleClick}
            onKeyDown={handleKeyDown}
            className={cn(
                "h-auto max-w-full",
                canOpen && "cursor-zoom-in outline-none transition duration-200 hover:opacity-95 focus-visible:ring-2 focus-visible:ring-zinc-400/70 focus-visible:ring-offset-4 focus-visible:ring-offset-white dark:focus-visible:ring-zinc-500 dark:focus-visible:ring-offset-zinc-950",
                className
            )}
            {...rest}
        />
    );
}
