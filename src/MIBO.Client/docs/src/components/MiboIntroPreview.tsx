import { useEffect, useRef } from "react";

// ─────────────────────────────────────────────────────────────────────────────
// CANVAS: Stars + Asteroids + Comets — ambient loop
// ─────────────────────────────────────────────────────────────────────────────
function SpaceCanvas({ container }: { container: React.RefObject<HTMLDivElement | null> }) {
    const ref = useRef<HTMLCanvasElement>(null);

    useEffect(() => {
        const canvas = ref.current!;
        const ctx = canvas.getContext("2d")!;
        const box = container.current!;

        const resize = () => {
            canvas.width = box.clientWidth;
            canvas.height = box.clientHeight;
        };
        resize();
        const ro = new ResizeObserver(resize);
        ro.observe(box);

        type Star = { x: number; y: number; r: number; twinkle: number; phase: number };
        type Asteroid = { x: number; y: number; vx: number; vy: number; r: number; rot: number; rotV: number; sides: number; opacity: number; trail: { x: number; y: number }[] };
        type Comet = { x: number; y: number; vx: number; vy: number; len: number; opacity: number; width: number };

        const stars: Star[] = Array.from({ length: 80 }, () => ({
            x: Math.random(), y: Math.random(),
            r: 0.3 + Math.random() * 1.0,
            twinkle: 0.2 + Math.random() * 0.8,
            phase: Math.random() * Math.PI * 2,
        }));

        const spawnAsteroid = (): Asteroid => {
            const W = canvas.width, H = canvas.height;
            const edge = Math.floor(Math.random() * 4);
            let x = 0, y = 0;
            if (edge === 0) { x = Math.random() * W; y = -30; }
            else if (edge === 1) { x = W + 30; y = Math.random() * H; }
            else if (edge === 2) { x = Math.random() * W; y = H + 30; }
            else { x = -30; y = Math.random() * H; }
            const angle = Math.atan2(H / 2 - y, W / 2 - x) + (Math.random() - 0.5) * 1.4;
            const speed = 0.2 + Math.random() * 0.8;
            return { x, y, vx: Math.cos(angle) * speed, vy: Math.sin(angle) * speed, r: 2 + Math.random() * 10, rot: Math.random() * Math.PI * 2, rotV: (Math.random() - 0.5) * 0.022, sides: 5 + Math.floor(Math.random() * 4), opacity: 0.12 + Math.random() * 0.3, trail: [] };
        };

        const spawnComet = (): Comet => {
            const W = canvas.width, H = canvas.height;
            const fromLeft = Math.random() > 0.5;
            return { x: fromLeft ? -60 : W + 60, y: Math.random() * H * 0.75, vx: fromLeft ? 2 + Math.random() * 4 : -(2 + Math.random() * 4), vy: (Math.random() - 0.35) * 1.5, len: 40 + Math.random() * 80, opacity: 0.35 + Math.random() * 0.55, width: 0.8 + Math.random() * 1.2 };
        };

        const asteroids: Asteroid[] = Array.from({ length: 8 }, spawnAsteroid);
        const comets: Comet[] = [];
        let t = 0, lastComet = 0, frame = 0;

        const draw = () => {
            const W = canvas.width, H = canvas.height;
            ctx.clearRect(0, 0, W, H);

            stars.forEach(s => {
                const a = s.twinkle * (0.4 + 0.6 * Math.sin(t * 0.018 + s.phase));
                ctx.globalAlpha = a;
                ctx.fillStyle = "#ffffff";
                ctx.beginPath();
                ctx.arc(s.x * W, s.y * H, s.r, 0, Math.PI * 2);
                ctx.fill();
            });

            asteroids.forEach((a, i) => {
                a.trail.push({ x: a.x, y: a.y });
                if (a.trail.length > 24) a.trail.shift();
                for (let j = 1; j < a.trail.length; j++) {
                    const prog = j / a.trail.length;
                    ctx.globalAlpha = prog * a.opacity * 0.22;
                    ctx.strokeStyle = "#a1a1aa";
                    ctx.lineWidth = prog * a.r * 0.3;
                    ctx.lineCap = "round";
                    ctx.beginPath();
                    ctx.moveTo(a.trail[j - 1].x, a.trail[j - 1].y);
                    ctx.lineTo(a.trail[j].x, a.trail[j].y);
                    ctx.stroke();
                }
                ctx.globalAlpha = a.opacity;
                ctx.save();
                ctx.translate(a.x, a.y);
                ctx.rotate(a.rot);
                ctx.beginPath();
                for (let s = 0; s < a.sides; s++) {
                    const angle = (s / a.sides) * Math.PI * 2;
                    const jitter = 0.72 + 0.28 * Math.sin(s * 3.9);
                    const px = Math.cos(angle) * a.r * jitter;
                    const py = Math.sin(angle) * a.r * jitter;
                    if (s === 0) ctx.moveTo(px, py); else ctx.lineTo(px, py);
                }
                ctx.closePath();
                ctx.strokeStyle = "#71717a";
                ctx.lineWidth = 0.8;
                ctx.stroke();
                ctx.fillStyle = "rgba(25,25,28,0.7)";
                ctx.fill();
                ctx.restore();
                a.x += a.vx; a.y += a.vy; a.rot += a.rotV;
                if (a.x < -40 || a.x > W + 40 || a.y < -40 || a.y > H + 40) {
                    Object.assign(asteroids[i], spawnAsteroid());
                }
            });

            if (t - lastComet > 100 + Math.random() * 80) { comets.push(spawnComet()); lastComet = t; }
            comets.forEach(c => {
                ctx.save();
                const angle = Math.atan2(c.vy, c.vx);
                ctx.translate(c.x, c.y);
                ctx.rotate(angle);
                const grad = ctx.createLinearGradient(-c.len, 0, 8, 0);
                grad.addColorStop(0, "rgba(255,255,255,0)");
                grad.addColorStop(0.7, `rgba(220,230,255,${c.opacity * 0.35})`);
                grad.addColorStop(1, `rgba(255,255,255,${c.opacity})`);
                ctx.globalAlpha = 1;
                ctx.strokeStyle = grad;
                ctx.lineWidth = c.width;
                ctx.lineCap = "round";
                ctx.beginPath(); ctx.moveTo(-c.len, 0); ctx.lineTo(0, 0); ctx.stroke();
                ctx.beginPath(); ctx.arc(0, 0, c.width * 1.8, 0, Math.PI * 2);
                ctx.fillStyle = `rgba(255,255,255,${c.opacity})`; ctx.fill();
                ctx.restore();
                c.x += c.vx; c.y += c.vy;
            });
            for (let i = comets.length - 1; i >= 0; i--) {
                const c = comets[i];
                if (c.x < -120 || c.x > W + 120 || c.y < -120 || c.y > H + 120) comets.splice(i, 1);
            }
            ctx.globalAlpha = 1;
            t++;
            frame = requestAnimationFrame(draw);
        };
        draw();
        return () => { cancelAnimationFrame(frame); ro.disconnect(); };
    }, [container]);

    return <canvas ref={ref} style={{ position: "absolute", inset: 0, pointerEvents: "none", zIndex: 1 }} />;
}

// ─────────────────────────────────────────────────────────────────────────────
// MINI CHARTS
// ─────────────────────────────────────────────────────────────────────────────
function MiniBarChart() {
    const bars = [{ h: 55, c: "#6366f1" }, { h: 80, c: "#10b981" }, { h: 45, c: "#f59e0b" }, { h: 90, c: "#6366f1" }, { h: 65, c: "#3b82f6" }, { h: 72, c: "#10b981" }];
    return (
        <div style={{ width: 120, background: "rgba(9,9,11,0.88)", border: "1px solid rgba(255,255,255,0.09)", borderRadius: 10, padding: 8, backdropFilter: "blur(16px)", boxShadow: "0 12px 36px rgba(0,0,0,0.7)" }}>
            <div style={{ fontSize: 7, color: "#52525b", marginBottom: 6, fontFamily: "'Courier New',monospace", letterSpacing: "0.05em" }}>Monthly Revenue</div>
            <div style={{ display: "flex", alignItems: "flex-end", gap: 3, height: 36 }}>
                {bars.map((b, i) => <div key={i} style={{ flex: 1, borderRadius: 2, height: `${b.h}%`, background: b.c, opacity: 0.85 }} />)}
            </div>
        </div>
    );
}

function MiniDonut() {
    const slices = [{ pct: 38, c: "#6366f1", l: "Product" }, { pct: 27, c: "#10b981", l: "Services" }, { pct: 21, c: "#f59e0b", l: "Support" }, { pct: 14, c: "#f43f5e", l: "Other" }];
    let offset = 0;
    return (
        <div style={{ width: 115, background: "rgba(9,9,11,0.85)", border: "1px solid rgba(255,255,255,0.08)", borderRadius: 10, padding: 8, backdropFilter: "blur(12px)", boxShadow: "0 12px 36px rgba(0,0,0,0.6)" }}>
            <div style={{ fontSize: 7, color: "#52525b", marginBottom: 6, fontFamily: "'Courier New',monospace" }}>Revenue Split</div>
            <div style={{ display: "flex", alignItems: "center", gap: 6 }}>
                <svg width="36" height="36" viewBox="0 0 52 52">
                    {slices.map((s, i) => {
                        const r = 20, cx = 26, cy = 26, inn = 11, pct = s.pct / 100;
                        const start = offset * 360 - 90, end = (offset + pct) * 360 - 90; offset += pct;
                        const tr = (d: number) => (d * Math.PI) / 180;
                        const p1 = { x: cx + r * Math.cos(tr(start)), y: cy + r * Math.sin(tr(start)) };
                        const p2 = { x: cx + r * Math.cos(tr(end)), y: cy + r * Math.sin(tr(end)) };
                        const p3 = { x: cx + inn * Math.cos(tr(end)), y: cy + inn * Math.sin(tr(end)) };
                        const p4 = { x: cx + inn * Math.cos(tr(start)), y: cy + inn * Math.sin(tr(start)) };
                        const large = pct > 0.5 ? 1 : 0;
                        return <path key={i} fill={s.c} opacity={0.9} d={`M${p1.x},${p1.y} A${r},${r} 0 ${large} 1 ${p2.x},${p2.y} L${p3.x},${p3.y} A${inn},${inn} 0 ${large} 0 ${p4.x},${p4.y} Z`} />;
                    })}
                </svg>
                <div style={{ display: "flex", flexDirection: "column", gap: 3 }}>
                    {slices.map((s, i) => (
                        <div key={i} style={{ display: "flex", alignItems: "center", gap: 3 }}>
                            <div style={{ width: 4, height: 4, borderRadius: "50%", background: s.c }} />
                            <span style={{ fontSize: 6, color: "#71717a" }}>{s.l}</span>
                        </div>
                    ))}
                </div>
            </div>
        </div>
    );
}

function MiniChat() {
    return (
        <div style={{ width: 128, background: "rgba(9,9,11,0.85)", border: "1px solid rgba(255,255,255,0.08)", borderRadius: 10, padding: 8, backdropFilter: "blur(12px)", boxShadow: "0 12px 36px rgba(0,0,0,0.6)" }}>
            <div style={{ fontSize: 7, color: "#52525b", marginBottom: 6, fontFamily: "'Courier New',monospace", display: "flex", alignItems: "center", gap: 4 }}>
                <div style={{ width: 4, height: 4, borderRadius: "50%", background: "#22c55e" }} />AI Agent
            </div>
            <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
                <div style={{ background: "rgba(255,255,255,0.05)", borderRadius: 6, padding: "3px 6px", fontSize: 6, color: "#a1a1aa", maxWidth: "80%" }}>Show my spending</div>
                <div style={{ background: "rgba(255,255,255,0.08)", borderRadius: 6, padding: "3px 6px", fontSize: 6, color: "#d4d4d8", marginLeft: "auto", maxWidth: "85%" }}>Here's your dashboard</div>
                <div style={{ display: "flex", gap: 2, alignItems: "center", padding: "1px 3px" }}>
                    {[0, 1, 2].map(i => <div key={i} style={{ width: 3, height: 3, borderRadius: "50%", background: "#52525b", animation: `dotBounce 0.8s ${i * 0.15}s ease-in-out infinite alternate` }} />)}
                </div>
            </div>
        </div>
    );
}

function MiniStat() {
    return (
        <div style={{ width: 108, background: "rgba(9,9,11,0.85)", border: "1px solid rgba(255,255,255,0.08)", borderRadius: 10, padding: 8, backdropFilter: "blur(12px)", boxShadow: "0 12px 36px rgba(0,0,0,0.6)" }}>
            <div style={{ fontSize: 7, color: "#52525b", marginBottom: 3, fontFamily: "'Courier New',monospace" }}>Total Users</div>
            <div style={{ fontSize: 16, fontWeight: 700, color: "#ffffff", marginBottom: 3, letterSpacing: "-0.02em" }}>24,831</div>
            <div style={{ fontSize: 7, color: "#22c55e", marginBottom: 6 }}>+18.4%</div>
            <div style={{ height: 2, background: "rgba(255,255,255,0.06)", borderRadius: 2 }}>
                <div style={{ height: 2, background: "#6366f1", borderRadius: 2, width: "73%" }} />
            </div>
        </div>
    );
}

function MiniLine() {
    const pts = [30, 55, 40, 70, 50, 85, 65, 90, 75, 95];
    const max = Math.max(...pts); const W = 100, H = 32;
    const path = pts.map((v, i) => `${i === 0 ? "M" : "L"}${(i / (pts.length - 1)) * W},${H - (v / max) * H}`).join(" ");
    const area = `${path} L${W},${H} L0,${H} Z`;
    return (
        <div style={{ width: 120, background: "rgba(9,9,11,0.85)", border: "1px solid rgba(255,255,255,0.08)", borderRadius: 10, padding: 8, backdropFilter: "blur(12px)", boxShadow: "0 12px 36px rgba(0,0,0,0.6)" }}>
            <div style={{ fontSize: 7, color: "#52525b", marginBottom: 6, fontFamily: "'Courier New',monospace" }}>Growth Trend</div>
            <svg width={W} height={H} viewBox={`0 0 ${W} ${H}`} style={{ overflow: "visible" }}>
                <defs><linearGradient id="lgPrev" x1="0" y1="0" x2="0" y2="1"><stop offset="0%" stopColor="#6366f1" stopOpacity="0.35" /><stop offset="100%" stopColor="#6366f1" stopOpacity="0" /></linearGradient></defs>
                <path d={area} fill="url(#lgPrev)" />
                <path d={path} fill="none" stroke="#6366f1" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round" />
            </svg>
        </div>
    );
}

function MiniSummaryPanel() {
    const items = [
        { label: "Revenue", value: "$24.5k", trend: +12.4, color: "#10b981", spark: [32, 28, 39, 41, 37, 45, 49] },
        { label: "Expenses", value: "$18.2k", trend: -3.2, color: "#f43f5e", spark: [21, 24, 22, 19, 21, 18, 17] },
        { label: "Profit", value: "$6.3k", trend: +8.1, color: "#6366f1", spark: [9, 7, 11, 13, 12, 15, 16] },
    ];
    return (
        <div style={{ width: 150, background: "rgba(9,9,11,0.88)", border: "1px solid rgba(255,255,255,0.09)", borderRadius: 10, padding: 8, backdropFilter: "blur(16px)", boxShadow: "0 12px 36px rgba(0,0,0,0.7)" }}>
            <div style={{ fontSize: 7, color: "#52525b", marginBottom: 6, fontFamily: "'Courier New',monospace", letterSpacing: "0.06em" }}>Financial Summary</div>
            <div style={{ display: "flex", flexDirection: "column", gap: 4 }}>
                {items.map((it, i) => {
                    const W2 = 32, H2 = 14, mn = Math.min(...it.spark), mx = Math.max(...it.spark), sp = mx - mn || 1;
                    const pts2 = it.spark.map((v, j) => ({ x: (j / (it.spark.length - 1)) * W2, y: H2 - 2 - ((v - mn) / sp) * (H2 - 4) }));
                    const linePath = pts2.map((p, j) => `${j === 0 ? "M" : "L"}${p.x},${p.y}`).join(" ");
                    return (
                        <div key={i} style={{ display: "flex", alignItems: "center", gap: 6, padding: "3px 5px", background: "rgba(255,255,255,0.03)", borderRadius: 6, border: "1px solid rgba(255,255,255,0.04)" }}>
                            <div style={{ flex: 1 }}>
                                <div style={{ fontSize: 6, color: "#52525b", marginBottom: 1 }}>{it.label}</div>
                                <div style={{ fontSize: 10, fontWeight: 700, color: "#ffffff" }}>{it.value}</div>
                            </div>
                            <svg width={W2} height={H2} viewBox={`0 0 ${W2} ${H2}`} style={{ flexShrink: 0 }}>
                                <path d={linePath} fill="none" stroke={it.color} strokeWidth="1.2" strokeLinecap="round" strokeLinejoin="round" />
                                <circle cx={pts2[pts2.length - 1].x} cy={pts2[pts2.length - 1].y} r="1.5" fill={it.color} />
                            </svg>
                            <div style={{ fontSize: 6, fontWeight: 600, color: it.trend > 0 ? "#22c55e" : "#f43f5e", minWidth: 26, textAlign: "right" }}>
                                {it.trend > 0 ? "+" : ""}{it.trend}%
                            </div>
                        </div>
                    );
                })}
            </div>
        </div>
    );
}

// ─────────────────────────────────────────────────────────────────────────────
// ORBITAL CARD — smooth Lissajous orbit, always visible
// ─────────────────────────────────────────────────────────────────────────────
interface OrbitalConfig {
    cx: number; cy: number;
    rx: number; ry: number;
    fx: number; fy: number;
    px: number; py: number;
    rotSpeed: number; rotBase: number;
    scl: number;
}

function OrbitalCard({ cfg, container, children }: {
    cfg: OrbitalConfig; container: React.RefObject<HTMLDivElement | null>; children: React.ReactNode;
}) {
    const ref = useRef<HTMLDivElement>(null);
    const frameRef = useRef(0);
    const tRef = useRef(cfg.px);

    useEffect(() => {
        const el = ref.current;
        const box = container.current;
        if (!el || !box) return;

        const animate = () => {
            const W = box.clientWidth;
            const H = box.clientHeight;
            tRef.current += 0.003;
            const t = tRef.current;
            const bx = cfg.cx * W;
            const by = cfg.cy * H;
            const x = bx + Math.sin(t * cfg.fx) * cfg.rx + Math.sin(t * cfg.fx * 0.41) * cfg.rx * 0.3;
            const y = by + Math.cos(t * cfg.fy) * cfg.ry + Math.cos(t * cfg.fy * 0.53) * cfg.ry * 0.3;
            const rot = cfg.rotBase + Math.sin(t * cfg.rotSpeed * 0.7) * 5;
            el.style.transform = `translate(${x}px,${y}px) scale(${cfg.scl}) rotate(${rot}deg)`;
            frameRef.current = requestAnimationFrame(animate);
        };

        frameRef.current = requestAnimationFrame(animate);
        return () => cancelAnimationFrame(frameRef.current);
    }, [cfg, container]);

    return (
        <div ref={ref} style={{ position: "absolute", top: 0, left: 0, zIndex: 3, userSelect: "none", willChange: "transform" }}>
            {children}
        </div>
    );
}

// ─────────────────────────────────────────────────────────────────────────────
// MAIN PREVIEW — shows the "MIBO" title fully visible with ambient scene
// ─────────────────────────────────────────────────────────────────────────────
export function MiboIntroPreview() {
    const containerRef = useRef<HTMLDivElement>(null);

    const orbits: OrbitalConfig[] = [
        { cx: 0.08, cy: 0.18, rx: 40, ry: 30, fx: 1.0, fy: 1.3, px: 0, py: 1.2, rotSpeed: 0.4, rotBase: -5, scl: 0.72 },
        { cx: 0.06, cy: 0.55, rx: 35, ry: 45, fx: 0.8, fy: 1.5, px: 1.0, py: 3.0, rotSpeed: 0.3, rotBase: 3, scl: 0.70 },
        { cx: 0.09, cy: 0.82, rx: 38, ry: 28, fx: 1.1, fy: 0.8, px: 2.4, py: 0.8, rotSpeed: 0.5, rotBase: 2, scl: 0.68 },
        { cx: 0.88, cy: 0.16, rx: 38, ry: 40, fx: 1.4, fy: 1.0, px: 2.1, py: 0.5, rotSpeed: 0.5, rotBase: 4, scl: 0.70 },
        { cx: 0.90, cy: 0.55, rx: 32, ry: 42, fx: 1.2, fy: 0.9, px: 3.5, py: 1.8, rotSpeed: 0.45, rotBase: -3, scl: 0.74 },
        { cx: 0.87, cy: 0.82, rx: 40, ry: 30, fx: 0.9, fy: 1.2, px: 0.7, py: 2.5, rotSpeed: 0.35, rotBase: -4, scl: 0.70 },
    ];

    const cardChildren = [
        <MiniBarChart />, <MiniChat />, <MiniSummaryPanel />,
        <MiniDonut />, <MiniStat />, <MiniLine />,
    ];

    return (
        <div className="my-8 overflow-hidden rounded-2xl border border-zinc-800" style={{ height: 420 }}>
            <div
                ref={containerRef}
                style={{ position: "relative", width: "100%", height: "100%", background: "#09090b", display: "flex", alignItems: "center", justifyContent: "center", overflow: "hidden" }}
            >
                <style>{`
                    @keyframes dotBounce{from{transform:translateY(0)}to{transform:translateY(-3px)}}
                    @keyframes ringRot{from{transform:rotate(0deg)}to{transform:rotate(360deg)}}
                    @keyframes ringRotR{from{transform:rotate(0deg)}to{transform:rotate(-360deg)}}
                    @keyframes pulseGlow{0%,100%{opacity:.03}50%{opacity:.09}}
                    @keyframes scanMove{from{transform:translateY(-100%)}to{transform:translateY(100%)}}
                `}</style>

                {/* Dot grid */}
                <div style={{ position: "absolute", inset: 0, backgroundImage: "radial-gradient(circle,rgba(255,255,255,.022) 1px,transparent 1px)", backgroundSize: "28px 28px" }} />
                {/* Glow */}
                <div style={{ position: "absolute", inset: 0, background: "radial-gradient(ellipse 55% 45% at 50% 50%,rgba(255,255,255,.035) 0%,transparent 70%)", animation: "pulseGlow 3.5s ease-in-out infinite" }} />
                {/* Scanline */}
                <div style={{ position: "absolute", inset: 0, overflow: "hidden", pointerEvents: "none", zIndex: 2 }}>
                    <div style={{ position: "absolute", left: 0, right: 0, height: "25%", background: "linear-gradient(to bottom,transparent,rgba(255,255,255,.012),transparent)", animation: "scanMove 7s linear infinite" }} />
                </div>

                <SpaceCanvas container={containerRef} />

                {/* Orbital cards */}
                {orbits.map((cfg, i) => (
                    <OrbitalCard key={i} cfg={cfg} container={containerRef}>
                        {cardChildren[i]}
                    </OrbitalCard>
                ))}

                {/* Rings */}
                {[140, 200, 260].map((size, i) => (
                    <div key={size} style={{ position: "absolute", width: size, height: size, border: `1px solid rgba(255,255,255,${0.06 - i * 0.015})`, borderTopColor: `rgba(255,255,255,${0.18 - i * 0.04})`, borderRadius: "50%", animation: `${i % 2 === 0 ? "ringRot" : "ringRotR"} ${10 + i * 5}s linear infinite`, zIndex: 2 }} />
                ))}

                {/* Corners */}
                {[
                    { top: "20%", left: "calc(50% - 160px)", borderTop: "1px solid", borderLeft: "1px solid" },
                    { top: "20%", right: "calc(50% - 160px)", borderTop: "1px solid", borderRight: "1px solid" },
                    { bottom: "20%", left: "calc(50% - 160px)", borderBottom: "1px solid", borderLeft: "1px solid" },
                    { bottom: "20%", right: "calc(50% - 160px)", borderBottom: "1px solid", borderRight: "1px solid" },
                ].map((s, i) => (
                    <div key={i} style={{ position: "absolute", width: 16, height: 16, borderColor: "rgba(255,255,255,.18)", opacity: 0.35, zIndex: 4, ...s }} />
                ))}

                {/* Title — always fully visible */}
                <div style={{ position: "relative", zIndex: 5, textAlign: "center", userSelect: "none", display: "flex", flexDirection: "column", alignItems: "center" }}>
                    <div style={{ fontFamily: "'Courier New',monospace", fontSize: 8, letterSpacing: "0.38em", color: "#3f3f46", marginBottom: 16, height: 14 }}>
                        AI CHATBOT SYSTEM v2.0
                    </div>
                    <div style={{ fontSize: "clamp(48px, 12vw, 80px)", lineHeight: 1, display: "flex", gap: "0.03em", justifyContent: "center", marginBottom: 8 }}>
                        {"MIBO".split("").map((ch, i) => (
                            <span key={i} style={{ display: "inline-block", fontFamily: "'Courier New',monospace", fontWeight: 900, color: "#ffffff" }}>
                                {ch}
                            </span>
                        ))}
                    </div>
                    <div style={{ fontFamily: "'Courier New',monospace", fontSize: "clamp(7px, 1.2vw, 9px)", color: "#52525b", letterSpacing: "0.3em", textTransform: "uppercase", height: 12 }}>
                        Generative UI · AI Chatbot
                    </div>
                    <div style={{ height: 24, marginTop: 10, display: "flex", alignItems: "center", justifyContent: "center" }}>
                        <div style={{ fontFamily: "'Courier New',monospace", fontSize: 9, letterSpacing: "0.12em", color: "#52525b", opacity: 0.45 }}>
                            Chatbot care gândește vizual
                        </div>
                    </div>
                </div>
            </div>
        </div>
    );
}
