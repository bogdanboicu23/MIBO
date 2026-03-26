import { useEffect, useRef, useState, type ReactNode } from "react";
import { ThemeProvider } from "./providers/ThemeProvider";
import { I18nProvider } from "./providers/I18nProvider";
import { Bounce, ToastContainer } from "react-toastify";
import { useTheme } from "../hooks/useTheme.ts";
import { AxiosProvider } from "../axios/axios-provider";
import { AuthProvider } from "@/auth/context/jwt";
import { useAuthContext } from "@/auth/hooks";
import MiboIntro from "@/components/MiboIntro.tsx";
import { usePathname } from "@/routes/hooks";
import { clearQueuedIntroPlayback, hasQueuedIntroPlayback } from "@/utils/intro.ts";

type AppProps = {
    children: ReactNode;
};

function LoadingScreen({ progress }: { progress: number }) {
    return (
        <div
            style={{
                position: "fixed",
                inset: 0,
                zIndex: 9998,
                display: "grid",
                placeItems: "center",
                background: "#09090b",
                overflow: "hidden",
            }}
        >
            <style>{`
                @keyframes spin { from { transform: rotate(0deg); } to { transform: rotate(360deg); } }
                @keyframes pulse { 0%,100% { opacity:.35; } 50% { opacity:.75; } }
                @keyframes scan { from { transform: translateY(-120%); } to { transform: translateY(120%); } }
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

function IntroFlow({ onComplete }: { onComplete: () => void }) {
    const audioRef = useRef<HTMLAudioElement | null>(null);
    const [progress, setProgress] = useState(0);
    const [showIntro, setShowIntro] = useState(false);

    useEffect(() => {
        const audio = new Audio("/intro.mp3");
        audio.loop = false;
        audio.playbackRate = 1.15;
        audio.volume = 0.8;
        audio.preload = "auto";
        audioRef.current = audio;

        void audio.play().catch(() => undefined);

        const duration = 5000;
        const step = 50;
        const totalSteps = duration / step;
        let currentStep = 0;

        const interval = window.setInterval(() => {
            currentStep += 1;
            setProgress((currentStep / totalSteps) * 100);

            if (currentStep >= totalSteps) {
                window.clearInterval(interval);
                setShowIntro(true);
            }
        }, step);

        return () => {
            window.clearInterval(interval);

            if (audioRef.current) {
                audioRef.current.pause();
                audioRef.current.currentTime = 0;
                audioRef.current = null;
            }
        };
    }, []);

    return (
        <>
            {!showIntro ? <LoadingScreen progress={progress} /> : null}
            {showIntro ? <MiboIntro onComplete={onComplete} /> : null}
        </>
    );
}

export default function App({ children }: AppProps) {
    useScrollToTop();

    return (
        <AxiosProvider>
            <AuthProvider>
                <ThemeProvider>
                    <I18nProvider>
                        <AppContent>{children}</AppContent>
                    </I18nProvider>
                </ThemeProvider>
            </AuthProvider>
        </AxiosProvider>
    );
}

function AppContent({ children }: AppProps) {
    const { theme } = useTheme();
    const { authenticated, loading } = useAuthContext();
    const [introActive, setIntroActive] = useState(false);

    useEffect(() => {
        if (loading || !authenticated || introActive || !hasQueuedIntroPlayback()) {
            return;
        }

        clearQueuedIntroPlayback();
        setIntroActive(true);
    }, [authenticated, introActive, loading]);

    return (
        <>
            {children}

            {introActive ? <IntroFlow onComplete={() => setIntroActive(false)} /> : null}

            <ToastContainer
                position="top-right"
                autoClose={3000}
                theme={theme}
                transition={Bounce}
            />
        </>
    );
}

function useScrollToTop() {
    const pathname = usePathname();

    useEffect(() => {
        window.scrollTo(0, 0);
    }, [pathname]);
}
