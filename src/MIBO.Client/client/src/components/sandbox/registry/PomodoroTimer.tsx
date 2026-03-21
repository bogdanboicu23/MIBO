import type { UiComponentProps } from "@/components/sandbox/uiRuntime/UiRenderer.tsx";
import { useState, useRef, useCallback, useEffect } from "react";

type Phase = "work" | "shortBreak" | "longBreak";

const PHASE_LABELS: Record<Phase, string> = {
    work: "Work",
    shortBreak: "Short Break",
    longBreak: "Long Break",
};

const PHASE_COLORS: Record<Phase, string> = {
    work: "#ef4444",
    shortBreak: "#22c55e",
    longBreak: "#3b82f6",
};

export function PomodoroTimer({ props }: UiComponentProps) {
    const workMinutes = Number(props?.workMinutes ?? 25);
    const shortBreakMinutes = Number(props?.shortBreakMinutes ?? 5);
    const longBreakMinutes = Number(props?.longBreakMinutes ?? 15);
    const sessionsBeforeLongBreak = Number(props?.sessionsBeforeLongBreak ?? 4);

    const [phase, setPhase] = useState<Phase>("work");
    const [session, setSession] = useState(1);
    const [secondsLeft, setSecondsLeft] = useState(workMinutes * 60);
    const [running, setRunning] = useState(false);
    const intervalRef = useRef<ReturnType<typeof setInterval> | null>(null);

    const totalSeconds =
        phase === "work"
            ? workMinutes * 60
            : phase === "shortBreak"
              ? shortBreakMinutes * 60
              : longBreakMinutes * 60;

    const progress = totalSeconds > 0 ? (totalSeconds - secondsLeft) / totalSeconds : 0;

    const clearTimer = useCallback(() => {
        if (intervalRef.current) {
            clearInterval(intervalRef.current);
            intervalRef.current = null;
        }
    }, []);

    const advancePhase = useCallback(() => {
        if (phase === "work") {
            if (session >= sessionsBeforeLongBreak) {
                setPhase("longBreak");
                setSecondsLeft(longBreakMinutes * 60);
            } else {
                setPhase("shortBreak");
                setSecondsLeft(shortBreakMinutes * 60);
            }
        } else {
            const nextSession = phase === "longBreak" ? 1 : session + 1;
            setSession(nextSession);
            setPhase("work");
            setSecondsLeft(workMinutes * 60);
        }
    }, [phase, session, sessionsBeforeLongBreak, workMinutes, shortBreakMinutes, longBreakMinutes]);

    useEffect(() => {
        if (!running) {
            clearTimer();
            return;
        }
        intervalRef.current = setInterval(() => {
            setSecondsLeft((prev) => {
                if (prev <= 1) {
                    clearTimer();
                    setTimeout(() => {
                        advancePhase();
                        setRunning(true);
                    }, 0);
                    return 0;
                }
                return prev - 1;
            });
        }, 1000);
        return clearTimer;
    }, [running, clearTimer, advancePhase]);

    const handleReset = () => {
        clearTimer();
        setRunning(false);
        setPhase("work");
        setSession(1);
        setSecondsLeft(workMinutes * 60);
    };

    const minutes = Math.floor(secondsLeft / 60);
    const seconds = secondsLeft % 60;
    const timeStr = `${String(minutes).padStart(2, "0")}:${String(seconds).padStart(2, "0")}`;

    const radius = 70;
    const circumference = 2 * Math.PI * radius;
    const strokeDashoffset = circumference * (1 - progress);
    const color = PHASE_COLORS[phase];

    return (
        <div className="rounded-2xl border border-zinc-200/70 bg-white p-6 shadow-sm dark:border-zinc-800/70 dark:bg-zinc-950">
            <div className="mb-4 text-center text-sm font-semibold uppercase tracking-wide opacity-70">
                {PHASE_LABELS[phase]}
            </div>

            <div className="flex justify-center">
                <svg width="180" height="180" viewBox="0 0 180 180">
                    <circle
                        cx="90"
                        cy="90"
                        r={radius}
                        fill="none"
                        stroke="currentColor"
                        strokeWidth="8"
                        className="opacity-10"
                    />
                    <circle
                        cx="90"
                        cy="90"
                        r={radius}
                        fill="none"
                        stroke={color}
                        strokeWidth="8"
                        strokeLinecap="round"
                        strokeDasharray={circumference}
                        strokeDashoffset={strokeDashoffset}
                        transform="rotate(-90 90 90)"
                        style={{ transition: "stroke-dashoffset 0.3s ease" }}
                    />
                    <text
                        x="90"
                        y="85"
                        textAnchor="middle"
                        dominantBaseline="central"
                        className="fill-current text-3xl font-bold"
                        style={{ fontSize: "28px", fontFamily: "monospace" }}
                    >
                        {timeStr}
                    </text>
                    <text
                        x="90"
                        y="112"
                        textAnchor="middle"
                        dominantBaseline="central"
                        className="fill-current text-xs opacity-50"
                        style={{ fontSize: "12px" }}
                    >
                        Session {session} of {sessionsBeforeLongBreak}
                    </text>
                </svg>
            </div>

            <div className="mt-4 flex justify-center gap-3">
                <button
                    onClick={() => setRunning((r) => !r)}
                    className="rounded-lg px-5 py-2 text-sm font-medium text-white transition-colors"
                    style={{ backgroundColor: color }}
                >
                    {running ? "Pause" : "Start"}
                </button>
                <button
                    onClick={handleReset}
                    className="rounded-lg border border-zinc-300 px-5 py-2 text-sm font-medium transition-colors hover:bg-zinc-100 dark:border-zinc-700 dark:hover:bg-zinc-800"
                >
                    Reset
                </button>
            </div>
        </div>
    );
}
