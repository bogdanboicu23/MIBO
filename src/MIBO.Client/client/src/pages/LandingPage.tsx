import { useEffect, useState } from "react";
import { useRouter } from "@/routes/hooks";
import { paths } from "@/routes/paths.ts";
import { useThemeContext } from "@/app/providers/useThemeContext.ts";

// ── Typing hook ──────────────────────────────────────────────────────────────
function useTyping(phrases: string[], speed = 65) {
    const [text, setText] = useState("");
    const [pi, setPi] = useState(0);
    const [ci, setCi] = useState(0);
    const [deleting, setDeleting] = useState(false);

    useEffect(() => {
        const phrase = phrases[pi % phrases.length];
        const delay = deleting ? speed / 2.5 : speed;

        const t = setTimeout(() => {
            if (!deleting) {
                setText(phrase.slice(0, ci + 1));
                if (ci + 1 === phrase.length) setTimeout(() => setDeleting(true), 2000);
                else setCi(ci + 1);
            } else {
                setText(phrase.slice(0, ci - 1));
                if (ci - 1 === 0) {
                    setDeleting(false);
                    setPi((p) => p + 1);
                    setCi(0);
                } else setCi(ci - 1);
            }
        }, delay);

        return () => clearTimeout(t);
    }, [ci, deleting, pi, phrases, speed]);

    return text;
}

// ── Reveal on scroll ─────────────────────────────────────────────────────────
function useReveal() {
    useEffect(() => {
        const els = document.querySelectorAll(".reveal");
        const io = new IntersectionObserver(
            (entries) =>
                entries.forEach((e) => {
                    if (e.isIntersecting) e.target.classList.add("in");
                }),
            { threshold: 0.1 }
        );
        els.forEach((el) => io.observe(el));
        return () => io.disconnect();
    }, []);
}

// ── Animated Chat Demo ───────────────────────────────────────────────────────
type DemoUi = "chart" | "compare";

type DemoStep = {
    role: "ai" | "user";
    text: string;
    ui?: DemoUi; // optional
};

const DEMO_STEPS: DemoStep[] = [
    { role: "ai", text: "Bună! Cu ce te pot ajuta azi?" },
    { role: "user", text: "Arată-mi un dashboard de vânzări" },
    { role: "ai", text: "Iată datele tale din această lună:", ui: "chart" },
    { role: "user", text: "Compară cu luna trecută" },
    { role: "ai", text: "Creștere de +23% față de februarie ↑", ui: "compare" },
];

function ChatDemo({ start }: { start: boolean }) {
    const [step, setStep] = useState(0);
    const [typing, setTyping] = useState(false);

    useEffect(() => {
        if (!start) return;
        if (step >= DEMO_STEPS.length) return;

        const msg = DEMO_STEPS[step];

        if (msg.role === "ai") {
            setTyping(true);
            const t = setTimeout(() => {
                setTyping(false);
                setStep((s) => s + 1);
            }, 1200);
            return () => clearTimeout(t);
        } else {
            const t = setTimeout(() => setStep((s) => s + 1), 800);
            return () => clearTimeout(t);
        }
    }, [step, start]);
    
        //set type for shown as role, text and ui (nullable)
        
        const shown = DEMO_STEPS.slice(0, step);

    return (
        <div className="rounded-2xl overflow-hidden border border-zinc-200 bg-white shadow-xl dark:border-zinc-800 dark:bg-zinc-950">
            {/* Chrome bar */}
            <div className="flex items-center gap-2 px-4 py-3 border-b border-zinc-100 bg-zinc-50/80 dark:border-zinc-800/60 dark:bg-zinc-900/60">
                <div className="w-2.5 h-2.5 rounded-full bg-zinc-300 dark:bg-zinc-700" />
                <div className="w-2.5 h-2.5 rounded-full bg-zinc-300 dark:bg-zinc-700" />
                <div className="w-2.5 h-2.5 rounded-full bg-zinc-300 dark:bg-zinc-700" />
                <span className="ml-3 text-xs text-zinc-400">mibo — chat</span>
                <div className="ml-auto flex items-center gap-1.5">
                    <span className="text-xs text-zinc-400">AI Agent</span>
                    <div className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse" />
                </div>
            </div>

            {/* Messages */}
            <div className="p-5 space-y-4 h-[650px]">
                {shown.map((msg, i) => (
                    <div
                        key={i}
                        className="flex gap-3 items-end"
                        style={{
                            justifyContent: msg.role === "user" ? "flex-end" : "flex-start",
                            animation: "slideUp 0.35s cubic-bezier(.22,1,.36,1) both",
                        }}
                    >
                        {msg.role === "ai" && (
                            <div className="w-8 h-8 rounded-full bg-zinc-200 dark:bg-zinc-800 border border-zinc-200 dark:border-zinc-700 flex items-center justify-center text-xs text-zinc-600 dark:text-zinc-300 shrink-0 font-medium">
                                AI
                            </div>
                        )}

                        <div className="max-w-[80%] space-y-2">
                            <div
                                className="rounded-2xl px-4 py-2.5 text-sm leading-relaxed shadow-sm"
                                style={
                                    msg.role === "user"
                                        ? { background: "rgb(24 24 27)", color: "rgb(244 244 245)" }
                                        : { background: "rgb(244 244 245)", color: "rgb(24 24 27)" }
                                }
                            >
                                {msg.text}
                            </div>

                            {msg.ui === "chart" && (
                                <div
                                    className="rounded-2xl p-4 border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-900 space-y-2.5"
                                    style={{
                                        animation:
                                            "slideUp 0.4s 0.1s cubic-bezier(.22,1,.36,1) both",
                                    }}
                                >
                                    <div className="text-xs text-zinc-400 mb-2">
                                        📊 Vânzări — Martie 2026
                                    </div>
                                    {[
                                        ["Săpt 1", "68%"],
                                        ["Săpt 2", "84%"],
                                        ["Săpt 3", "91%"],
                                        ["Săpt 4", "76%"],
                                    ].map(([w, v]) => (
                                        <div key={w} className="flex items-center gap-3">
                                            <span className="text-xs text-zinc-400 w-12">{w}</span>
                                            <div className="flex-1 h-1.5 rounded-full bg-zinc-100 dark:bg-zinc-800">
                                                <div
                                                    className="h-1.5 rounded-full bg-zinc-900 dark:bg-white"
                                                    style={{
                                                        width: v,
                                                        transition: "width 1s ease",
                                                    }}
                                                />
                                            </div>
                                            <span className="text-xs text-zinc-500 w-8 text-right">
                        {v}
                      </span>
                                        </div>
                                    ))}
                                </div>
                            )}

                            {msg.ui === "compare" && (
                                <div
                                    className="rounded-2xl p-4 border border-zinc-200 bg-white dark:border-zinc-800 dark:bg-zinc-900"
                                    style={{
                                        animation:
                                            "slideUp 0.4s 0.1s cubic-bezier(.22,1,.36,1) both",
                                    }}
                                >
                                    <div className="text-xs text-zinc-400 mb-3">
                                        📈 Comparație lunară
                                    </div>
                                    <div className="grid grid-cols-2 gap-2">
                                        {[
                                            ["Februarie", "18,240 lei", false],
                                            ["Martie", "22,435 lei", true],
                                        ].map(([m, v, highlighted]) => (
                                            <div
                                                key={String(m)}
                                                className={`rounded-xl p-3 border text-center ${
                                                    highlighted
                                                        ? "border-zinc-900 bg-zinc-900 dark:border-white dark:bg-white"
                                                        : "border-zinc-100 dark:border-zinc-800"
                                                }`}
                                            >
                                                <div
                                                    className={`text-xs mb-1 ${
                                                        highlighted
                                                            ? "text-zinc-400 dark:text-zinc-600"
                                                            : "text-zinc-400"
                                                    }`}
                                                >
                                                    {String(m)}
                                                </div>
                                                <div
                                                    className={`text-sm font-semibold ${
                                                        highlighted
                                                            ? "text-white dark:text-zinc-900"
                                                            : "text-zinc-900 dark:text-white"
                                                    }`}
                                                >
                                                    {String(v)}
                                                </div>
                                            </div>
                                        ))}
                                    </div>
                                    <div className="mt-3 text-center text-xs text-emerald-500">
                                        ▲ +23% creștere
                                    </div>
                                </div>
                            )}
                        </div>

                        {msg.role === "user" && (
                            <div className="w-8 h-8 rounded-full bg-zinc-900 dark:bg-white border border-zinc-200 flex items-center justify-center text-xs text-white dark:text-zinc-900 shrink-0 font-medium">
                                Tu
                            </div>
                        )}
                    </div>
                ))}

                {typing && (
                    <div className="flex gap-3 items-end" style={{ animation: "slideUp 0.3s both" }}>
                        <div className="w-8 h-8 rounded-full bg-zinc-200 dark:bg-zinc-800 flex items-center justify-center text-xs text-zinc-600 dark:text-zinc-300 shrink-0 font-medium">
                            AI
                        </div>
                        <div className="rounded-2xl px-4 py-3 border border-zinc-100 dark:border-zinc-800 bg-zinc-50 dark:bg-zinc-900">
              <span className="inline-flex gap-1.5">
                {[0, 1, 2].map((i) => (
                    <span
                        key={i}
                        className="w-1.5 h-1.5 rounded-full bg-zinc-400 dark:bg-zinc-600"
                        style={{
                            animation: `bounce 0.8s ${i * 0.15}s ease-in-out infinite alternate`,
                        }}
                    />
                ))}
              </span>
                        </div>
                    </div>
                )}
            </div>

            {/* Input bar — same as your app */}
            <div className="px-4 pb-4 pt-1 border-t border-zinc-100 dark:border-zinc-800/60">
                <div className="rounded-xl border border-zinc-200 dark:border-zinc-800 bg-zinc-50 dark:bg-zinc-900 px-4 py-3 flex items-center gap-3">
                    <span className="text-sm text-zinc-400 flex-1">Message your agent…</span>
                    <button className="rounded-lg bg-zinc-900 dark:bg-white px-3 py-1.5 text-xs text-white dark:text-zinc-900 font-medium">
                        Send
                    </button>
                </div>
                <p className="text-center text-xs text-zinc-300 dark:text-zinc-700 mt-2">
                    Enter to send · Shift+Enter for newline
                </p>
            </div>
        </div>
    );
}

// ── Main Landing ─────────────────────────────────────────────────────────────
export default function LandingPage() {
    const { theme, setTheme } = useThemeContext();
    const router = useRouter();
    const goToDocs = () => router.push("docs.mibo.monster")
    const onEnterApp = () => router.push(paths.chat);
    const typed = useTyping(["ui interactiv", "formulare dinamice", "grafice live"]);

    useReveal();

    // ✅ Start demo animations only when #demo becomes visible (e.g., after clicking "Vezi demo")
    const [demoStart, setDemoStart] = useState(false);
    useEffect(() => {
        const el = document.getElementById("demo");
        if (!el) return;

        const io = new IntersectionObserver(
            ([entry]) => {
                if (entry.isIntersecting) {
                    setDemoStart(true);
                    io.disconnect();
                }
            },
            { threshold: 0.35 }
        );

        io.observe(el);
        return () => io.disconnect();
    }, []);

    return (
        <div className={theme ? "dark" : ""}>
            <div className="bg-white dark:bg-zinc-950 text-zinc-900 dark:text-zinc-100 min-h-screen antialiased">
                <style>{`
          @keyframes slideUp {
            from { opacity: 0; transform: translateY(16px); }
            to   { opacity: 1; transform: translateY(0); }
          }
          @keyframes fadeIn {
            from { opacity: 0; }
            to   { opacity: 1; }
          }
          @keyframes bounce {
            from { transform: translateY(0); }
            to   { transform: translateY(-5px); }
          }
          .reveal {
            opacity: 0;
            transform: translateY(20px);
            transition: opacity 0.55s cubic-bezier(.22,1,.36,1), transform 0.55s cubic-bezier(.22,1,.36,1);
          }
          .reveal.in { opacity: 1; transform: translateY(0); }
          .d1 { transition-delay: 0.07s; }
          .d2 { transition-delay: 0.14s; }
          .d3 { transition-delay: 0.21s; }
          .d4 { transition-delay: 0.28s; }
          .d5 { transition-delay: 0.35s; }
          .d6 { transition-delay: 0.42s; }
          .cursor-blink::after {
            content: '|';
            animation: fadeIn 0.6s step-end infinite alternate;
            opacity: 0.4;
          }
          .card-hover {
            transition: border-color 0.2s ease, box-shadow 0.2s ease;
          }
          .card-hover:hover {
            border-color: rgb(161 161 170) !important;
          }
          .dark .card-hover:hover {
            border-color: rgb(82 82 91) !important;
            box-shadow: 0 2px 20px rgba(0,0,0,0.3);
          }
        `}</style>

                {/* ── NAVBAR ── */}
                <header className="sticky top-0 z-50 border-b border-zinc-200/70 bg-white/70 backdrop-blur dark:border-zinc-800/70 dark:bg-zinc-950/40">
                    <div className="mx-auto flex max-w-5xl items-center gap-3 px-5 py-3 md:px-6">
                        {/* Logo */}
                        <div className="flex items-center gap-2.5 mr-2">
                            <div className="w-7 h-7 rounded-lg bg-zinc-900 dark:bg-white flex items-center justify-center">
                                <svg width="15" height="15" viewBox="0 0 15 15" fill="none">
                                    <circle cx="5" cy="7" r="1.8" fill="white" />
                                    <circle cx="10" cy="7" r="1.8" fill="white" />
                                    <circle cx="5" cy="7" r="0.85" fill="#22c55e" />
                                    <circle cx="10" cy="7" r="0.85" fill="#22c55e" />
                                    <rect x="4.5" y="10.8" width="6" height="1.1" rx="0.55" fill="white" />
                                    <rect x="2" y="2.5" width="11" height="8" rx="2.2" stroke="white" strokeWidth="1.2" fill="none" />
                                    <line x1="7.5" y1="1" x2="7.5" y2="2.5" stroke="white" strokeWidth="1.1" strokeLinecap="round" />
                                </svg>
                            </div>
                            <span className="text-sm font-bold tracking-[0.12em] text-zinc-900 dark:text-white">
                MIBO
              </span>
                        </div>

                        <nav className="hidden md:flex items-center gap-6 text-sm text-zinc-500 dark:text-zinc-400">
                            {["Features", "Demo", "Prețuri"].map((item) => (
                                <a
                                    key={item}
                                    href={`#${item.toLowerCase()}`}
                                    className="hover:text-zinc-900 dark:hover:text-zinc-100 transition-colors"
                                >
                                    {item}
                                </a>
                            ))}
                        </nav>

                        <div className="ml-auto flex items-center gap-2">
                            <button
                                onClick={() => setTheme(theme === "light" ? "dark" : "light")}
                                className="rounded-xl border border-zinc-200 bg-white px-3 py-2 text-sm hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-900 dark:hover:bg-zinc-800 transition-colors"
                            >
                                {theme === "light" ? "☀️" : "🌙"}
                            </button>
                            <button
                                onClick={onEnterApp}
                                className="rounded-xl bg-zinc-900 px-4 py-2 text-sm font-semibold text-white hover:bg-zinc-800 dark:bg-white dark:text-zinc-900 dark:hover:bg-zinc-100 transition-colors"
                            >
                                + New chat
                            </button>
                        </div>
                    </div>
                </header>

                {/* ── HERO ── */}
                <section className="relative overflow-hidden border-b border-zinc-100 dark:border-zinc-800/50">
                    <div
                        className="absolute inset-0 opacity-[0.025] dark:opacity-[0.04]"
                        style={{
                            backgroundImage: "radial-gradient(circle, #71717a 1px, transparent 1px)",
                            backgroundSize: "32px 32px",
                        }}
                    />

                    <div className="relative mx-auto max-w-5xl px-5 pt-24 pb-24 md:pt-32 md:pb-32">
                        <div
                            className="mb-8 inline-flex items-center gap-2 rounded-full border border-zinc-200 bg-zinc-50 px-3.5 py-1.5 text-xs text-zinc-500 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-400"
                            style={{ animation: "slideUp 0.45s both" }}
                        >
                            <span className="w-1.5 h-1.5 rounded-full bg-emerald-500 animate-pulse" />
                            Minimal Input Beautiful Output
                        </div>

                        <h1
                            className="mb-6 text-5xl font-bold leading-[1.1] tracking-tight text-zinc-900 dark:text-white md:text-7xl"
                            style={{ animation: "slideUp 0.45s 0.07s both" }}
                        >
                            Chatbot AI care
                            <br />
                            <span className="text-zinc-300 dark:text-zinc-600">generează </span>
                            <span className="cursor-blink">{typed}</span>
                        </h1>

                        <p
                            className="mb-10 max-w-lg text-lg leading-relaxed text-zinc-500 dark:text-zinc-400"
                            style={{ animation: "slideUp 0.45s 0.14s both" }}
                        >
                            MIBO nu răspunde doar cu text — creează componente React interactive direct în conversație.
                            Dashboards, tabele, formulare, carduri de date.
                        </p>

                        <div className="flex flex-wrap gap-3" style={{ animation: "slideUp 0.45s 0.2s both" }}>
                            <button
                                onClick={onEnterApp}
                                className="rounded-xl bg-zinc-900 px-6 py-3 text-sm font-semibold text-white hover:bg-zinc-800 dark:bg-white dark:text-zinc-900 dark:hover:bg-zinc-100 transition-colors"
                            >
                                Deschide MIBO →
                            </button>
                            <a
                                href="#demo"
                                className="rounded-xl border border-zinc-200 bg-white px-6 py-3 text-sm font-medium text-zinc-600 hover:bg-zinc-50 dark:border-zinc-800 dark:bg-zinc-900 dark:text-zinc-400 dark:hover:bg-zinc-800 transition-colors"
                            >
                                Vezi demo
                            </a>
                        </div>

                        <div
                            className="mt-20 flex flex-wrap gap-x-12 gap-y-5 border-t border-zinc-100 dark:border-zinc-800/60 pt-10"
                            style={{ animation: "slideUp 0.45s 0.28s both" }}
                        >
                            {[
                                ["2 modele AI", "Agent + Agent Pro"],
                                ["Generative UI", "Componente live"],
                                ["Dark / Light", "Temă adaptivă"],
                                ["Responsive", "Mobile + desktop"],
                            ].map(([val, label]) => (
                                <div key={val}>
                                    <div className="text-sm font-semibold text-zinc-900 dark:text-white">{val}</div>
                                    <div className="text-xs text-zinc-400 dark:text-zinc-600 mt-0.5">{label}</div>
                                </div>
                            ))}
                        </div>
                    </div>
                </section>

                {/* ── DEMO ── */}
                <section id="demo" className="border-b border-zinc-100 dark:border-zinc-800/50 py-24">
                    <div className="mx-auto max-w-5xl px-5">
                        <div className="grid md:grid-cols-2 gap-14 items-center">
                            <div>
                                <p className="reveal text-xs tracking-[0.25em] text-zinc-400 dark:text-zinc-600 uppercase mb-4">— Demo live</p>
                                <h2 className="reveal d1 text-3xl md:text-4xl font-bold text-zinc-900 dark:text-white leading-tight mb-5">
                                    Scrii un mesaj,
                                    <br />
                                    apare o interfață
                                </h2>
                                <p className="reveal d2 text-base text-zinc-500 dark:text-zinc-400 leading-relaxed mb-8">
                                    AI-ul înțelege când datele ar fi mai clare vizual și generează componentele potrivite. Persistente în istoricul conversației.
                                </p>
                                <div className="reveal d3 space-y-2">
                                    {[
                                        "„Arată-mi statisticile de azi",
                                        "„Creează un formular de contact",
                                        "„Compară opțiunile A și B vizual",
                                    ].map((ex) => (
                                        <div
                                            key={ex}
                                            className="flex items-center gap-3 rounded-xl border border-zinc-100 dark:border-zinc-800 bg-zinc-50 dark:bg-zinc-900/50 px-4 py-3 text-sm text-zinc-500 dark:text-zinc-400"
                                        >
                                            <span className="text-zinc-300 dark:text-zinc-700">→</span>
                                            <span className="italic">{ex}</span>
                                        </div>
                                    ))}
                                </div>
                            </div>

                            <div className="reveal d2">
                                {/* ✅ starts only after clicking "Vezi demo" (when section is visible) */}
                                <ChatDemo start={demoStart} />
                            </div>
                        </div>
                    </div>
                </section>

                {/* ── FEATURES ── */}
                <section id="features" className="border-b border-zinc-100 dark:border-zinc-800/50 py-24">
                    <div className="mx-auto max-w-5xl px-5">
                        <div className="mb-16">
                            <p className="reveal text-xs tracking-[0.25em] text-zinc-400 dark:text-zinc-600 uppercase mb-4">— Capabilități</p>
                            <h2 className="reveal d1 text-3xl md:text-4xl font-bold text-zinc-900 dark:text-white">
                                Mai mult decât un chatbot
                            </h2>
                        </div>

                        <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-3">
                            {[
                                {
                                    icon: "⚡",
                                    title: "Generative UI",
                                    desc: "Componente React generate și randate live în conversație — nu descrise, ci afișate.",
                                    d: "d1",
                                },
                                {
                                    icon: "🤖",
                                    title: "AI Agent Pro",
                                    desc: "Modelul avansat execută sarcini complexe în mai mulți pași cu raționament profund.",
                                    d: "d2",
                                },
                                {
                                    icon: "🌓",
                                    title: "Dark / Light mode",
                                    desc: "Backdrop-blur, tranziție fluidă, identic cu tema aplicației tale existente.",
                                    d: "d3",
                                },
                                {
                                    icon: "💾",
                                    title: "Istoric persistent",
                                    desc: "Conversațiile și UI-urile generate rămân salvate și funcționale la revenire.",
                                    d: "d4",
                                },
                                {
                                    icon: "🔒",
                                    title: "Sandbox izolat",
                                    desc: "UI-ul generat rulează securizat fără acces la date sensibile.",
                                    d: "d5",
                                },
                                {
                                    icon: "⚙️",
                                    title: "Model switchable",
                                    desc: "Comută între AI Agent și Pro direct din navbar fără să pierzi contextul.",
                                    d: "d6",
                                },
                            ].map((f) => (
                                <div
                                    key={f.title}
                                    className={`reveal ${f.d} card-hover rounded-2xl border border-zinc-100 dark:border-zinc-800/80 bg-white dark:bg-zinc-900/30 p-6`}
                                >
                                    <div className="mb-4 text-xl">{f.icon}</div>
                                    <div className="mb-2 text-sm font-semibold text-zinc-900 dark:text-zinc-100">{f.title}</div>
                                    <div className="text-sm leading-relaxed text-zinc-500 dark:text-zinc-500">{f.desc}</div>
                                </div>
                            ))}
                        </div>
                    </div>
                </section>

                {/* ── HOW IT WORKS ── */}
                <section className="border-b border-zinc-100 dark:border-zinc-800/50 py-24">
                    <div className="mx-auto max-w-3xl px-5">
                        <div className="mb-16">
                            <p className="reveal text-xs tracking-[0.25em] text-zinc-400 dark:text-zinc-600 uppercase mb-4">— Cum funcționează</p>
                            <h2 className="reveal d1 text-3xl md:text-4xl font-bold text-zinc-900 dark:text-white">3 pași simpli</h2>
                        </div>
                        <div className="space-y-3">
                            {[
                                {
                                    n: "01",
                                    title: "Scrii un mesaj natural",
                                    desc: "Nu ai nevoie de comenzi speciale. Vorbești cu MIBO ca și cum ai vorbi cu un coleg.",
                                },
                                {
                                    n: "02",
                                    title: "AI-ul înțelege intenția",
                                    desc: "MIBO detectează automat când datele ar fi mai clare vizual și generează componentele potrivite.",
                                },
                                {
                                    n: "03",
                                    title: "Interacționezi cu interfața",
                                    desc: "Componentele apar direct în chat — clicabile, funcționale, persistente în istoric.",
                                },
                            ].map((s, i) => (
                                <div
                                    key={s.n}
                                    className={`reveal d${i + 1} flex gap-6 items-start rounded-2xl border border-zinc-100 dark:border-zinc-800/80 bg-white dark:bg-zinc-900/30 p-6`}
                                >
                  <span className="text-3xl font-bold text-zinc-100 dark:text-zinc-800 tabular-nums shrink-0 leading-none">
                    {s.n}
                  </span>
                                    <div>
                                        <div className="mb-1.5 font-semibold text-zinc-900 dark:text-zinc-100 text-sm">{s.title}</div>
                                        <div className="text-sm leading-relaxed text-zinc-500 dark:text-zinc-500">{s.desc}</div>
                                    </div>
                                </div>
                            ))}
                        </div>
                    </div>
                </section>

                {/* ── PRICING ── */}
                <section id="prețuri" className="border-b border-zinc-100 dark:border-zinc-800/50 py-24">
                    <div className="mx-auto max-w-5xl px-5">
                        <div className="mb-16">
                            <p className="reveal text-xs tracking-[0.25em] text-zinc-400 dark:text-zinc-600 uppercase mb-4">— Prețuri</p>
                            <h2 className="reveal d1 text-3xl md:text-4xl font-bold text-zinc-900 dark:text-white">
                                Simplu și transparent
                            </h2>
                        </div>

                        <div className="grid md:grid-cols-3 gap-4">
                            {[
                                {
                                    name: "Free",
                                    price: "$0",
                                    sub: "pentru totdeauna",
                                    features: ["50 mesaje / zi", "AI Agent standard", "Generative UI", "Istoric 7 zile"],
                                    cta: "Începe gratuit",
                                    featured: false,
                                    d: "d1",
                                },
                                {
                                    name: "Pro",
                                    price: "$19",
                                    sub: "per lună",
                                    features: ["Mesaje nelimitate", "AI Agent Pro", "Generative UI avansat", "Istoric complet", "Support prioritar"],
                                    cta: "Alege Pro",
                                    featured: true,
                                    d: "d2",
                                },
                                {
                                    name: "Team",
                                    price: "$49",
                                    sub: "per lună",
                                    features: ["Tot din Pro", "5 utilizatori", "Workspace partajat", "API access", "Custom models"],
                                    cta: "Contactează-ne",
                                    featured: false,
                                    d: "d3",
                                },
                            ].map((plan) => (
                                <div
                                    key={plan.name}
                                    className={`reveal ${plan.d} card-hover rounded-2xl border p-6 flex flex-col ${
                                        plan.featured
                                            ? "border-zinc-900 bg-zinc-900 dark:border-zinc-100 dark:bg-white"
                                            : "border-zinc-100 bg-white dark:border-zinc-800/80 dark:bg-zinc-900/30"
                                    }`}
                                >
                                    {plan.featured && (
                                        <span
                                            className={`mb-4 self-start rounded-full border px-2.5 py-0.5 text-xs ${
                                                plan.featured
                                                    ? "border-white/20 text-white/60 dark:border-zinc-900/20 dark:text-zinc-900/60"
                                                    : ""
                                            }`}
                                        >
                      ★ Recomandat
                    </span>
                                    )}
                                    <div className={`text-sm font-semibold mb-1 ${plan.featured ? "text-white dark:text-zinc-900" : "text-zinc-900 dark:text-zinc-100"}`}>
                                        {plan.name}
                                    </div>
                                    <div className={`text-4xl font-bold mb-0.5 ${plan.featured ? "text-white dark:text-zinc-900" : "text-zinc-900 dark:text-white"}`}>
                                        {plan.price}
                                    </div>
                                    <div className={`text-xs mb-7 ${plan.featured ? "text-white/50 dark:text-zinc-900/50" : "text-zinc-400 dark:text-zinc-600"}`}>
                                        {plan.sub}
                                    </div>
                                    <ul className="space-y-2.5 mb-8 flex-1">
                                        {plan.features.map((f) => (
                                            <li
                                                key={f}
                                                className={`flex items-center gap-2 text-sm ${
                                                    plan.featured ? "text-white/80 dark:text-zinc-900/70" : "text-zinc-500 dark:text-zinc-400"
                                                }`}
                                            >
                        <span className={plan.featured ? "text-white/30 dark:text-zinc-900/30" : "text-zinc-300 dark:text-zinc-700"}>
                          ✓
                        </span>
                                                {f}
                                            </li>
                                        ))}
                                    </ul>
                                    <button
                                        onClick={onEnterApp}
                                        className={`rounded-xl py-2.5 text-sm font-semibold transition-colors ${
                                            plan.featured
                                                ? "bg-white text-zinc-900 hover:bg-zinc-100 dark:bg-zinc-900 dark:text-white dark:hover:bg-zinc-800"
                                                : "border border-zinc-200 bg-zinc-50 text-zinc-700 hover:bg-zinc-100 dark:border-zinc-700 dark:bg-zinc-800/50 dark:text-zinc-300 dark:hover:bg-zinc-800"
                                        }`}
                                    >
                                        {plan.cta}
                                    </button>
                                </div>
                            ))}
                        </div>
                    </div>
                </section>

                {/* ── CTA ── */}
                <section className="py-28 px-5">
                    <div className="mx-auto max-w-2xl text-center">
                        <h2 className="reveal text-4xl md:text-5xl font-bold text-zinc-900 dark:text-white mb-5 leading-tight">
                            Gata să construiești
                            <br />
                            cu MIBO?
                        </h2>
                        <p className="reveal d1 text-base text-zinc-500 dark:text-zinc-400 mb-10">
                            Primul chatbot care generează interfețe React live.
                            <br />
                            Gratuit, fără card necesar.
                        </p>
                        <div className="reveal d2 flex flex-wrap justify-center gap-3">
                            <button
                                onClick={onEnterApp}
                                className="rounded-xl bg-zinc-900 px-7 py-3.5 text-sm font-semibold text-white hover:bg-zinc-800 dark:bg-white dark:text-zinc-900 dark:hover:bg-zinc-100 transition-colors"
                            >
                                Deschide MIBO →
                            </button>
                            <button 
                                onClick={goToDocs}
                                className="rounded-xl border border-zinc-200 px-7 py-3.5 text-sm font-medium text-zinc-600 hover:bg-zinc-50 dark:border-zinc-800 dark:text-zinc-400 dark:hover:bg-zinc-900 transition-colors" >
                                Documentație
                            </button>
                        </div>
                    </div>
                </section>

                {/* ── FOOTER ── */}
                <footer className="border-t border-zinc-100 dark:border-zinc-800/60 py-8 px-5">
                    <div className="mx-auto max-w-5xl flex flex-col md:flex-row items-center justify-between gap-4">
                        <div className="flex items-center gap-2">
                            <div className="w-5 h-5 rounded bg-zinc-900 dark:bg-white flex items-center justify-center">
                                <span className="text-[8px] font-bold text-white dark:text-zinc-900">M</span>
                            </div>
                            <span className="text-xs font-bold tracking-widest text-zinc-900 dark:text-white">MIBO</span>
                        </div>
                        <p className="text-xs text-zinc-400 dark:text-zinc-600">
                            © 2026 MIBO AI by Mihai Musteața and Bogdan Boicu
                        </p>
                        <div className="flex gap-5 text-xs text-zinc-400 dark:text-zinc-600">
                            {["Privacy", "Terms", "GitHub"].map((l) => (
                                <a key={l} href="#" className="hover:text-zinc-900 dark:hover:text-white transition-colors">
                                    {l}
                                </a>
                            ))}
                        </div>
                    </div>
                </footer>
            </div>
        </div>
    );
}
