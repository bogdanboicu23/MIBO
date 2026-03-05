import { useEffect, useRef, useState, type ReactNode } from "react";
import { ThemeProvider } from "./providers/ThemeProvider";
import { I18nProvider } from "./providers/I18nProvider";
import { Bounce, ToastContainer } from "react-toastify";
import { useTheme } from "../hooks/useTheme.ts";
import { AxiosProvider } from "../axios/axios-provider";
import { AuthProvider } from "@/auth/context/jwt";
import { usePathname } from "@/routes/hooks";
import MiboIntro from "@/components/MiboIntro.tsx";

type AppProps = {
    children: ReactNode;
};

// LOADING SCREEN

function LoadingScreen({ progress }: { progress: number }) {
    return (
        <div
            style={{
                position: "fixed",
                inset: 0,
                zIndex: 10,
                display: "grid",
                placeItems: "center",
                background: "#09090b",
                overflow: "hidden",
            }}
        >
            <style>{`
        @keyframes spin { from{transform:rotate(0deg)} to{transform:rotate(360deg)} }
        @keyframes pulse { 0%,100%{opacity:.35} 50%{opacity:.75} }
        @keyframes scan { from{transform:translateY(-120%)} to{transform:translateY(120%)} }
      `}</style>

            <div
                style={{
                    position: "absolute",
                    inset: 0,
                    backgroundImage:
                        "radial-gradient(circle,rgba(255,255,255,.02) 1px,transparent 1px)",
                    backgroundSize: "38px 38px",
                }}
            />

            <div style={{ position: "absolute", inset: 0, overflow: "hidden" }}>
                <div
                    style={{
                        position: "absolute",
                        left: 0,
                        right: 0,
                        height: "28%",
                        background:
                            "linear-gradient(to bottom, transparent, rgba(255,255,255,.012), transparent)",
                        animation: "scan 3s linear infinite",
                    }}
                />
            </div>

            <div style={{ textAlign: "center" }}>
                <div
                    style={{
                        width: 54,
                        height: 54,
                        borderRadius: "50%",
                        border: "1px solid rgba(255,255,255,0.12)",
                        borderTopColor: "rgba(255,255,255,0.6)",
                        animation: "spin 1.2s linear infinite",
                        margin: "0 auto 20px",
                    }}
                />

                <div
                    style={{
                        fontFamily: "'Courier New', monospace",
                        fontSize: 11,
                        letterSpacing: "0.25em",
                        textTransform: "uppercase",
                        color: "#a1a1aa",
                        animation: "pulse 1.5s ease-in-out infinite",
                    }}
                >
                    Loading MIBO systems
                </div>

                <div
                    style={{
                        marginTop: 16,
                        width: 280,
                        height: 3,
                        background: "rgba(255,255,255,.08)",
                        borderRadius: 999,
                        overflow: "hidden",
                    }}
                >
                    <div
                        style={{
                            height: "100%",
                            width: `${progress}%`,
                            background: "linear-gradient(90deg,#3f3f46,#ffffff)",
                            borderRadius: 999,
                            transition: "width 80ms linear",
                        }}
                    />
                </div>

                <div
                    style={{
                        marginTop: 10,
                        fontFamily: "'Courier New', monospace",
                        fontSize: 10,
                        color: "#52525b",
                    }}
                >
                    {Math.round(progress)}%
                </div>
            </div>
        </div>
    );
}

// INTRO FLOW

function IntroFlow({
                       onComplete,
                       startMusic,
                   }: {
    onComplete: () => void;
    startMusic: () => void;
}) {
    const [started, setStarted] = useState(false);
    const [loading, setLoading] = useState(false);
    const [showIntro, setShowIntro] = useState(false);
    const [progress, setProgress] = useState(0);

    useEffect(() => {
        if (!started) return;

        startMusic();
        setLoading(true);

        const duration = 5000;
        const step = 50;
        const totalSteps = duration / step;

        let i = 0;

        const interval = setInterval(() => {
            i++;
            setProgress((i / totalSteps) * 100);

            if (i >= totalSteps) {
                clearInterval(interval);
                setLoading(false);
                setShowIntro(true);
            }
        }, step);

        return () => clearInterval(interval);
    }, [started]);

    return (
        <>
            {!started && (
                <div
                    style={{
                        position: "fixed",
                        inset: 0,
                        display: "grid",
                        placeItems: "center",
                        background: "#09090b",
                        zIndex: 20,
                    }}
                >
                    <button
                        onClick={() => setStarted(true)}
                        style={{
                            padding: "16px 22px",
                            borderRadius: 12,
                            border: "1px solid rgba(255,255,255,0.2)",
                            background: "#111",
                            color: "white",
                            cursor: "pointer",
                            fontFamily: "monospace",
                            letterSpacing: "0.1em",
                        }}
                    >
                        START MIBO
                    </button>
                </div>
            )}

            {loading && <LoadingScreen progress={progress} />}

            {showIntro && <MiboIntro onComplete={onComplete} />}
        </>
    );
}

// APP

export default function App({ children }: AppProps) {
    const { theme } = useTheme();
    useScrollToTop();

    const [introDone, setIntroDone] = useState(false);

    const audioRef = useRef<HTMLAudioElement | null>(null);
    const [musicStarted, setMusicStarted] = useState(false);

    useEffect(() => {
        if (audioRef.current) return;

        const audio = new Audio("/intro.mp3");

        audio.loop = false;
        audio.playbackRate = 1.15;
        audio.volume = 0.8;
        audio.preload = "auto";

        audioRef.current = audio;
    }, []);

    const startMusic = () => {
        if (musicStarted) return;

        setMusicStarted(true);

        const audio = audioRef.current;
        if (!audio) return;

        audio.play().catch(() => {});
    };

    if (!introDone) {
        return (
            <IntroFlow
                startMusic={startMusic}
                onComplete={() => setIntroDone(true)}
            />
        );
    }

    return (
        <AxiosProvider>
            <AuthProvider>
                <ThemeProvider>
                    <I18nProvider>
                        {children}

                        <ToastContainer
                            position="top-right"
                            autoClose={3000}
                            theme={theme}
                            transition={Bounce}
                        />
                    </I18nProvider>
                </ThemeProvider>
            </AuthProvider>
        </AxiosProvider>
    );
}

// Scroll helper

function useScrollToTop() {
    const pathname = usePathname();

    useEffect(() => {
        window.scrollTo(0, 0);
    }, [pathname]);
}