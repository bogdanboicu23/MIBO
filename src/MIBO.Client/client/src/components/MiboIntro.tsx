import { useEffect, useState, useRef } from "react";
// ─────────────────────────────────────────────────────────────────────────────
// CANVAS: Asteroids + Comet trails + Stars
// ─────────────────────────────────────────────────────────────────────────────
function SpaceCanvas() {
    const ref = useRef<HTMLCanvasElement>(null);

    useEffect(() => {
        const canvas = ref.current!;
        const ctx = canvas.getContext("2d")!;
        const resize = () => { canvas.width = window.innerWidth; canvas.height = window.innerHeight; };
        resize();
        window.addEventListener("resize", resize);

        type Asteroid = { x:number;y:number;vx:number;vy:number;r:number;rot:number;rotV:number;sides:number;opacity:number;trail:{x:number;y:number}[] };
        type Comet = { x:number;y:number;vx:number;vy:number;len:number;opacity:number;width:number };
        type Star = { x:number;y:number;r:number;twinkle:number;phase:number };

        const stars: Star[] = Array.from({length:130}, () => ({
            x: Math.random(), y: Math.random(),
            r: 0.4 + Math.random() * 1.6,
            twinkle: 0.2 + Math.random() * 0.8,
            phase: Math.random() * Math.PI * 2,
        }));

        const spawnAsteroid = (): Asteroid => {
            const W = canvas.width, H = canvas.height;
            const edge = Math.floor(Math.random() * 4);
            let x=0, y=0;
            if (edge===0){x=Math.random()*W;y=-50;}
            else if(edge===1){x=W+50;y=Math.random()*H;}
            else if(edge===2){x=Math.random()*W;y=H+50;}
            else{x=-50;y=Math.random()*H;}
            const angle = Math.atan2(H/2-y, W/2-x) + (Math.random()-0.5)*1.4;
            const speed = 0.3 + Math.random() * 1.4;
            return { x, y, vx:Math.cos(angle)*speed, vy:Math.sin(angle)*speed,
                r:3+Math.random()*20, rot:Math.random()*Math.PI*2,
                rotV:(Math.random()-0.5)*0.022, sides:5+Math.floor(Math.random()*4),
                opacity:0.12+Math.random()*0.3, trail:[] };
        };

        const spawnComet = (): Comet => {
            const W = canvas.width, H = canvas.height;
            const fromLeft = Math.random()>0.5;
            return { x:fromLeft?-120:W+120, y:Math.random()*H*0.75,
                vx:fromLeft?3+Math.random()*6:-(3+Math.random()*6),
                vy:(Math.random()-0.35)*2.5, len:70+Math.random()*140,
                opacity:0.35+Math.random()*0.55, width:1+Math.random()*2 };
        };

        const asteroids: Asteroid[] = Array.from({length:16}, spawnAsteroid);
        const comets: Comet[] = [];
        let t = 0, lastComet = 0, frame = 0;

        const draw = () => {
            const W = canvas.width, H = canvas.height;
            ctx.clearRect(0, 0, W, H);

            // Stars
            stars.forEach(s => {
                const a = s.twinkle * (0.4 + 0.6 * Math.sin(t*0.018 + s.phase));
                ctx.globalAlpha = a;
                ctx.fillStyle = "#ffffff";
                ctx.beginPath();
                ctx.arc(s.x*W, s.y*H, s.r, 0, Math.PI*2);
                ctx.fill();
            });

            // Asteroids
            asteroids.forEach((a, i) => {
                a.trail.push({x:a.x, y:a.y});
                if(a.trail.length > 32) a.trail.shift();

                // Trail
                for(let j=1; j<a.trail.length; j++) {
                    const prog = j/a.trail.length;
                    ctx.globalAlpha = prog * a.opacity * 0.22;
                    ctx.strokeStyle = "#a1a1aa";
                    ctx.lineWidth = prog * a.r * 0.3;
                    ctx.lineCap = "round";
                    ctx.beginPath();
                    ctx.moveTo(a.trail[j-1].x, a.trail[j-1].y);
                    ctx.lineTo(a.trail[j].x, a.trail[j].y);
                    ctx.stroke();
                }

                // Shape
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

                    if (s === 0) ctx.moveTo(px, py);
                    else ctx.lineTo(px, py);
                }
                ctx.closePath();
                ctx.strokeStyle = "#71717a";
                ctx.lineWidth = 0.8;
                ctx.stroke();
                ctx.fillStyle = "rgba(25,25,28,0.7)";
                ctx.fill();
                ctx.restore();

                a.x += a.vx; a.y += a.vy; a.rot += a.rotV;
                if(a.x<-70||a.x>W+70||a.y<-70||a.y>H+70) {
                    Object.assign(asteroids[i], spawnAsteroid());
                }
            });

            // Comets
            if(t - lastComet > 80 + Math.random()*70) {
                comets.push(spawnComet());
                lastComet = t;
            }
            comets.forEach((c) => {
                ctx.save();
                const angle = Math.atan2(c.vy, c.vx);
                ctx.translate(c.x, c.y);
                ctx.rotate(angle);
                const grad = ctx.createLinearGradient(-c.len, 0, 8, 0);
                grad.addColorStop(0, "rgba(255,255,255,0)");
                grad.addColorStop(0.7, `rgba(220,230,255,${c.opacity*0.35})`);
                grad.addColorStop(1, `rgba(255,255,255,${c.opacity})`);
                ctx.globalAlpha = 1;
                ctx.strokeStyle = grad;
                ctx.lineWidth = c.width;
                ctx.lineCap = "round";
                ctx.beginPath(); ctx.moveTo(-c.len, 0); ctx.lineTo(0, 0); ctx.stroke();
                ctx.beginPath(); ctx.arc(0, 0, c.width*1.8, 0, Math.PI*2);
                ctx.fillStyle = `rgba(255,255,255,${c.opacity})`; ctx.fill();
                ctx.restore();
                c.x += c.vx; c.y += c.vy;
            });

            // Prune comets
            for(let i=comets.length-1; i>=0; i--) {
                const c=comets[i];
                if(c.x<-200||c.x>W+200||c.y<-200||c.y>H+200) comets.splice(i,1);
            }

            ctx.globalAlpha = 1;
            t++;
            frame = requestAnimationFrame(draw);
        };
        draw();
        return () => { cancelAnimationFrame(frame); window.removeEventListener("resize", resize); };
    }, []);

    return <canvas ref={ref} style={{position:"absolute",inset:0,pointerEvents:"none",zIndex:1}} />;
}

// ─────────────────────────────────────────────────────────────────────────────
// MINI CHARTS
// ─────────────────────────────────────────────────────────────────────────────
function MiniBarChart() {
    const bars = [{h:55,c:"#6366f1"},{h:80,c:"#10b981"},{h:45,c:"#f59e0b"},{h:90,c:"#6366f1"},{h:65,c:"#3b82f6"},{h:72,c:"#10b981"}];
    return (
        <div style={{width:158,background:"rgba(9,9,11,0.88)",border:"1px solid rgba(255,255,255,0.09)",borderRadius:14,padding:"12px",backdropFilter:"blur(16px)",boxShadow:"0 16px 48px rgba(0,0,0,0.7)"}}>
            <div style={{fontSize:9,color:"#52525b",marginBottom:8,fontFamily:"'Courier New',monospace",letterSpacing:"0.05em"}}>Monthly Revenue</div>
            <div style={{display:"flex",alignItems:"flex-end",gap:4,height:56}}>
                {bars.map((b,i) => <div key={i} style={{flex:1,borderRadius:3,height:`${b.h}%`,background:b.c,opacity:0.85}} />)}
            </div>
            <div style={{display:"flex",justifyContent:"space-between",marginTop:6}}>
                {["Jan","Feb","Mar","Apr","May","Jun"].map(m => <span key={m} style={{fontSize:7,color:"#3f3f46"}}>{m}</span>)}
            </div>
        </div>
    );
}

function MiniDonut() {
    const slices = [{pct:38,c:"#6366f1",l:"Product"},{pct:27,c:"#10b981",l:"Services"},{pct:21,c:"#f59e0b",l:"Support"},{pct:14,c:"#f43f5e",l:"Other"}];
    let offset = 0;
    return (
        <div style={{width:150,background:"rgba(9,9,11,0.85)",border:"1px solid rgba(255,255,255,0.08)",borderRadius:14,padding:"12px",backdropFilter:"blur(12px)",boxShadow:"0 12px 40px rgba(0,0,0,0.6)"}}>
            <div style={{fontSize:9,color:"#52525b",marginBottom:8,fontFamily:"'Courier New',monospace"}}>Revenue Split</div>
            <div style={{display:"flex",alignItems:"center",gap:8}}>
                <svg width="52" height="52" viewBox="0 0 52 52">
                    {slices.map((s,i) => {
                        const r=20,cx=26,cy=26,inn=11,pct=s.pct/100;
                        const start=offset*360-90, end=(offset+pct)*360-90; offset+=pct;
                        const tr=(d:number)=>(d*Math.PI)/180;
                        const p1={x:cx+r*Math.cos(tr(start)),y:cy+r*Math.sin(tr(start))};
                        const p2={x:cx+r*Math.cos(tr(end)),y:cy+r*Math.sin(tr(end))};
                        const p3={x:cx+inn*Math.cos(tr(end)),y:cy+inn*Math.sin(tr(end))};
                        const p4={x:cx+inn*Math.cos(tr(start)),y:cy+inn*Math.sin(tr(start))};
                        const large=pct>0.5?1:0;
                        return <path key={i} fill={s.c} opacity={0.9} d={`M${p1.x},${p1.y} A${r},${r} 0 ${large} 1 ${p2.x},${p2.y} L${p3.x},${p3.y} A${inn},${inn} 0 ${large} 0 ${p4.x},${p4.y} Z`}/>;
                    })}
                    <text x="26" y="29" textAnchor="middle" fontSize="7" fill="#71717a">total</text>
                </svg>
                <div style={{display:"flex",flexDirection:"column",gap:4}}>
                    {slices.map((s,i) => (
                        <div key={i} style={{display:"flex",alignItems:"center",gap:4}}>
                            <div style={{width:6,height:6,borderRadius:"50%",background:s.c}}/>
                            <span style={{fontSize:7,color:"#71717a"}}>{s.l}</span>
                            <span style={{fontSize:7,color:"#52525b",marginLeft:"auto"}}>{s.pct}%</span>
                        </div>
                    ))}
                </div>
            </div>
        </div>
    );
}

function MiniChat() {
    return (
        <div style={{width:168,background:"rgba(9,9,11,0.85)",border:"1px solid rgba(255,255,255,0.08)",borderRadius:14,padding:"12px",backdropFilter:"blur(12px)",boxShadow:"0 12px 40px rgba(0,0,0,0.6)"}}>
            <div style={{fontSize:9,color:"#52525b",marginBottom:8,fontFamily:"'Courier New',monospace",display:"flex",alignItems:"center",gap:5}}>
                <div style={{width:5,height:5,borderRadius:"50%",background:"#22c55e"}}/>AI Agent · active
            </div>
            <div style={{display:"flex",flexDirection:"column",gap:5}}>
                <div style={{background:"rgba(255,255,255,0.05)",borderRadius:8,padding:"5px 8px",fontSize:8,color:"#a1a1aa",maxWidth:"80%"}}>Generează un raport de vânzări</div>
                <div style={{background:"rgba(255,255,255,0.08)",borderRadius:8,padding:"5px 8px",fontSize:8,color:"#d4d4d8",marginLeft:"auto",maxWidth:"85%"}}>Iată dashboard-ul tău ↓</div>
                <div style={{display:"flex",gap:3,alignItems:"center",padding:"2px 4px"}}>
                    {[0,1,2].map(i=><div key={i} style={{width:4,height:4,borderRadius:"50%",background:"#52525b",animation:`dotBounce 0.8s ${i*0.15}s ease-in-out infinite alternate`}}/>)}
                </div>
            </div>
        </div>
    );
}

function MiniStat() {
    return (
        <div style={{width:138,background:"rgba(9,9,11,0.85)",border:"1px solid rgba(255,255,255,0.08)",borderRadius:14,padding:"12px",backdropFilter:"blur(12px)",boxShadow:"0 12px 40px rgba(0,0,0,0.6)"}}>
            <div style={{fontSize:9,color:"#52525b",marginBottom:4,fontFamily:"'Courier New',monospace"}}>Total Users</div>
            <div style={{fontSize:22,fontWeight:700,color:"#ffffff",marginBottom:4,letterSpacing:"-0.02em"}}>24,831</div>
            <div style={{fontSize:9,color:"#22c55e",marginBottom:8}}>▲ +18.4% this month</div>
            <div style={{height:2,background:"rgba(255,255,255,0.06)",borderRadius:2}}>
                <div style={{height:2,background:"#6366f1",borderRadius:2,width:"73%"}}/>
            </div>
        </div>
    );
}

function MiniLine() {
    const pts=[30,55,40,70,50,85,65,90,75,95];
    const max=Math.max(...pts); const W=136,H=44;
    const path=pts.map((v,i)=>`${i===0?"M":"L"}${(i/(pts.length-1))*W},${H-(v/max)*H}`).join(" ");
    const area=`${path} L${W},${H} L0,${H} Z`;
    return (
        <div style={{width:158,background:"rgba(9,9,11,0.85)",border:"1px solid rgba(255,255,255,0.08)",borderRadius:14,padding:"12px",backdropFilter:"blur(12px)",boxShadow:"0 12px 40px rgba(0,0,0,0.6)"}}>
            <div style={{fontSize:9,color:"#52525b",marginBottom:8,fontFamily:"'Courier New',monospace"}}>Growth Trend</div>
            <svg width={W} height={H} viewBox={`0 0 ${W} ${H}`} style={{overflow:"visible"}}>
                <defs><linearGradient id="lineGrad" x1="0" y1="0" x2="0" y2="1"><stop offset="0%" stopColor="#6366f1" stopOpacity="0.35"/><stop offset="100%" stopColor="#6366f1" stopOpacity="0"/></linearGradient></defs>
                <path d={area} fill="url(#lineGrad)"/>
                <path d={path} fill="none" stroke="#6366f1" strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
            </svg>
        </div>
    );
}

// ── NEW MINI COMPONENTS ──────────────────────────────────────────────────────

function MiniSummaryPanel() {
    const items = [
        { label:"Revenue",  value:"$24.5k", trend:+12.4, color:"#10b981", spark:[32,28,39,41,37,45,49] },
        { label:"Expenses", value:"$18.2k", trend:-3.2,  color:"#f43f5e", spark:[21,24,22,19,21,18,17] },
        { label:"Profit",   value:"$6.3k",  trend:+8.1,  color:"#6366f1", spark:[9,7,11,13,12,15,16] },
    ];
    return (
        <div style={{width:200,background:"rgba(9,9,11,0.88)",border:"1px solid rgba(255,255,255,0.09)",borderRadius:14,padding:"12px",backdropFilter:"blur(16px)",boxShadow:"0 16px 48px rgba(0,0,0,0.7)"}}>
            <div style={{fontSize:9,color:"#52525b",marginBottom:8,fontFamily:"'Courier New',monospace",letterSpacing:"0.06em"}}>Financial Summary</div>
            <div style={{display:"flex",flexDirection:"column",gap:6}}>
                {items.map((it,i) => {
                    const W2=44,H2=18,mn=Math.min(...it.spark),mx=Math.max(...it.spark),sp=mx-mn||1;
                    const pts2=it.spark.map((v,j)=>({x:(j/(it.spark.length-1))*W2,y:H2-2-((v-mn)/sp)*(H2-4)}));
                    const linePath=pts2.map((p,j)=>`${j===0?"M":"L"}${p.x},${p.y}`).join(" ");
                    return (
                        <div key={i} style={{display:"flex",alignItems:"center",gap:8,padding:"5px 7px",background:"rgba(255,255,255,0.03)",borderRadius:8,border:`1px solid rgba(255,255,255,0.04)`}}>
                            <div style={{flex:1}}>
                                <div style={{fontSize:8,color:"#52525b",marginBottom:2}}>{it.label}</div>
                                <div style={{fontSize:13,fontWeight:700,color:"#ffffff",letterSpacing:"-0.01em"}}>{it.value}</div>
                            </div>
                            <svg width={W2} height={H2} viewBox={`0 0 ${W2} ${H2}`} style={{flexShrink:0}}>
                                <path d={linePath} fill="none" stroke={it.color} strokeWidth="1.5" strokeLinecap="round" strokeLinejoin="round"/>
                                <circle cx={pts2[pts2.length-1].x} cy={pts2[pts2.length-1].y} r="2" fill={it.color}/>
                            </svg>
                            <div style={{fontSize:8,fontWeight:600,color:it.trend>0?"#22c55e":"#f43f5e",minWidth:32,textAlign:"right"}}>
                                {it.trend>0?"↑":"↓"}{Math.abs(it.trend)}%
                            </div>
                        </div>
                    );
                })}
            </div>
        </div>
    );
}

function MiniDataTable() {
    const rows = [
        {name:"Pro Plan",   users:1240, rev:"$8,400", status:"active"},
        {name:"Free Plan",  users:3820, rev:"$0",     status:"free"},
        {name:"Team Plan",  users:480,  rev:"$4,800", status:"active"},
        {name:"Enterprise", users:92,   rev:"$9,200", status:"active"},
    ];
    const statusColor = (s:string) => s==="active" ? {bg:"rgba(34,197,94,0.12)",color:"#22c55e"} : {bg:"rgba(113,113,122,0.12)",color:"#71717a"};
    return (
        <div style={{width:230,background:"rgba(9,9,11,0.88)",border:"1px solid rgba(255,255,255,0.09)",borderRadius:14,overflow:"hidden",backdropFilter:"blur(16px)",boxShadow:"0 16px 48px rgba(0,0,0,0.7)"}}>
            <div style={{padding:"10px 12px 8px",borderBottom:"1px solid rgba(255,255,255,0.06)"}}>
                <div style={{fontSize:9,color:"#52525b",fontFamily:"'Courier New',monospace",letterSpacing:"0.06em"}}>Plans Overview</div>
            </div>
            <div style={{padding:"4px 0"}}>
                {/* Header */}
                <div style={{display:"grid",gridTemplateColumns:"1fr 44px 56px 44px",gap:4,padding:"3px 12px",marginBottom:2}}>
                    {["Plan","Users","Rev",""].map((h,i)=>(
                        <div key={i} style={{fontSize:7,color:"#3f3f46",fontWeight:600,letterSpacing:"0.08em",textAlign:i>0?"right":"left"}}>{h}</div>
                    ))}
                </div>
                {rows.map((r,i)=>(
                    <div key={i} style={{display:"grid",gridTemplateColumns:"1fr 44px 56px 44px",gap:4,padding:"4px 12px",background:i%2===0?"rgba(255,255,255,0.015)":"transparent"}}>
                        <div style={{fontSize:8,color:"#a1a1aa",overflow:"hidden",whiteSpace:"nowrap",textOverflow:"ellipsis"}}>{r.name}</div>
                        <div style={{fontSize:8,color:"#71717a",textAlign:"right",tabularNums:true} as any}>{r.users.toLocaleString()}</div>
                        <div style={{fontSize:8,color:"#a1a1aa",textAlign:"right",fontWeight:600}}>{r.rev}</div>
                        <div style={{display:"flex",justifyContent:"flex-end"}}>
                            <span style={{fontSize:6,padding:"1px 5px",borderRadius:20,...statusColor(r.status),fontWeight:600}}>{r.status}</span>
                        </div>
                    </div>
                ))}
            </div>
        </div>
    );
}

function MiniMarkdown() {
    return (
        <div style={{width:185,background:"rgba(9,9,11,0.88)",border:"1px solid rgba(255,255,255,0.09)",borderRadius:14,padding:"12px",backdropFilter:"blur(16px)",boxShadow:"0 16px 48px rgba(0,0,0,0.7)"}}>
            <div style={{fontSize:9,color:"#52525b",marginBottom:8,fontFamily:"'Courier New',monospace",letterSpacing:"0.06em",display:"flex",alignItems:"center",gap:5}}>
                <span style={{color:"#6366f1"}}>MD</span> AI Response
            </div>
            <div style={{fontSize:8,lineHeight:1.6,color:"#71717a"}}>
                <div style={{color:"#ffffff",fontWeight:700,fontSize:10,marginBottom:4}}>Sales Report Q1</div>
                <div style={{color:"#a1a1aa",marginBottom:6}}>Revenue increased by <span style={{color:"#22c55e",fontWeight:600}}>+23%</span> compared to last quarter.</div>
                <div style={{background:"rgba(99,102,241,0.1)",border:"1px solid rgba(99,102,241,0.2)",borderRadius:6,padding:"5px 8px",fontFamily:"'Courier New',monospace",fontSize:7,color:"#818cf8",marginBottom:6}}>
                    <div style={{color:"#52525b",marginBottom:2}}>{"// typescript"}</div>
                    <div><span style={{color:"#6366f1"}}>const</span> revenue = <span style={{color:"#f59e0b"}}>24500</span>;</div>
                </div>
                <div style={{display:"flex",gap:4,flexWrap:"wrap" as any}}>
                    {["React","TypeScript","AI"].map(t=>(
                        <span key={t} style={{fontSize:6,padding:"2px 6px",background:"rgba(255,255,255,0.05)",border:"1px solid rgba(255,255,255,0.08)",borderRadius:20,color:"#71717a"}}>{t}</span>
                    ))}
                </div>
            </div>
        </div>
    );
}

function MiniCategoryChips() {
    const cats = ["Dashboard","Analytics","Reports","Users","Settings","Billing","API","Logs"];
    const [active, setActive] = useState("Analytics");
    return (
        <div style={{width:190,background:"rgba(9,9,11,0.88)",border:"1px solid rgba(255,255,255,0.09)",borderRadius:14,padding:"12px",backdropFilter:"blur(16px)",boxShadow:"0 16px 48px rgba(0,0,0,0.7)"}}>
            <div style={{fontSize:9,color:"#52525b",marginBottom:8,fontFamily:"'Courier New',monospace",letterSpacing:"0.06em"}}>Categories</div>
            <div style={{display:"flex",flexWrap:"wrap" as any,gap:4}}>
                {cats.map(c=>(
                    <button key={c} onClick={()=>setActive(c)} style={{
                        fontSize:7,padding:"3px 8px",borderRadius:20,border:"1px solid",cursor:"pointer",
                        background: c===active ? "#ffffff" : "rgba(255,255,255,0.04)",
                        borderColor: c===active ? "#ffffff" : "rgba(255,255,255,0.1)",
                        color: c===active ? "#09090b" : "#71717a",
                        fontWeight: c===active ? 700 : 400,
                        transition:"all 0.15s",
                    }}>{c}</button>
                ))}
            </div>
        </div>
    );
}

// ─────────────────────────────────────────────────────────────────────────────
// ORBITAL FLOATING CARDS — RAF-driven Lissajous paths
// ─────────────────────────────────────────────────────────────────────────────
interface OrbitalConfig {
    // Base center position (0-1 of screen)
    cx: number; cy: number;
    // Orbit radii in px
    rx: number; ry: number;
    // Speed multipliers (different = Lissajous figure-8)
    fx: number; fy: number;
    // Phase offsets so each card starts at different point
    px: number; py: number;
    // Slow rotation
    rotSpeed: number; rotBase: number;
    // Entry delay ms
    entryDelay: number;
    // Scale
    scl: number;
    // Exit direction multiplier
    exitMx: number; exitMy: number;
}

function OrbitalCard({ cfg, exiting, children }: {
    cfg: OrbitalConfig; exiting: boolean; children: React.ReactNode;
}) {
    const ref = useRef<HTMLDivElement>(null);
    const frameRef = useRef(0);
    const stateRef = useRef({
        t: cfg.px,          // start at phase offset so each card begins at different orbital position
        opacity: 0,
        fadeIn: false,
        exiting: false,
        exitVx: 0,
        exitVy: 0,
        exitRot: cfg.rotBase,
        curX: 0,
        curY: 0,
    });

    useEffect(() => {
        const el = ref.current;
        if (!el) return;

        const W = window.innerWidth;
        const H = window.innerHeight;
        const s = stateRef.current;

        // Pre-compute the initial position so there's NEVER a jump:
        // set transform immediately before first paint
        const bx = cfg.cx * W;
        const by = cfg.cy * H;
        const x0 = bx + Math.sin(s.t * cfg.fx) * cfg.rx;
        const y0 = by + Math.cos(s.t * cfg.fy) * cfg.ry;
        s.curX = x0; s.curY = y0;
        el.style.transform = `translate(${x0}px,${y0}px) scale(${cfg.scl}) rotate(${cfg.rotBase}deg)`;
        el.style.opacity = "0";

        // Delay fade-in
        const fadeTimer = setTimeout(() => { s.fadeIn = true; }, cfg.entryDelay);

        const animate = () => {
            if (!el) return;

            if (s.exiting) {
                // Drift outward with momentum, scale down, blur up
                s.curX += s.exitVx;
                s.curY += s.exitVy;
                s.exitRot += 3;
                s.opacity = Math.max(0, s.opacity - 0.025);
                el.style.transform = `translate(${s.curX}px,${s.curY}px) scale(${Math.max(0, cfg.scl - (1 - s.opacity) * cfg.scl)}) rotate(${s.exitRot}deg)`;
                el.style.opacity = String(s.opacity);
                el.style.filter = `blur(${(1 - s.opacity) * 8}px)`;
                if (s.opacity > 0) frameRef.current = requestAnimationFrame(animate);
                return;
            }

            // Smooth orbital motion — pure sine/cosine, no discontinuity
            s.t += 0.003;
            const t = s.t;
            const x = bx
                + Math.sin(t * cfg.fx + 0) * cfg.rx
                + Math.sin(t * cfg.fx * 0.41) * cfg.rx * 0.3;
            const y = by
                + Math.cos(t * cfg.fy + 0) * cfg.ry
                + Math.cos(t * cfg.fy * 0.53) * cfg.ry * 0.3;
            const rot = cfg.rotBase + Math.sin(t * cfg.rotSpeed * 0.7) * 5;

            s.curX = x; s.curY = y;

            // Smooth fade in
            if (s.fadeIn && s.opacity < 1) {
                s.opacity = Math.min(1, s.opacity + 0.012); // ~80 frames = ~1.3s fade
            }

            el.style.transform = `translate(${x}px,${y}px) scale(${cfg.scl}) rotate(${rot}deg)`;
            el.style.opacity = String(s.opacity);

            frameRef.current = requestAnimationFrame(animate);
        };

        frameRef.current = requestAnimationFrame(animate);

        return () => {
            cancelAnimationFrame(frameRef.current);
            clearTimeout(fadeTimer);
        };
    }, []); // run once only — no deps that could cause re-run

    // Exit animation — staggered delay + 3 phases: pause → suck-in → explode
    useEffect(() => {
        if (!exiting) return;
        cancelAnimationFrame(frameRef.current);

        const el = ref.current;
        if (!el) return;

        const s = stateRef.current;
        s.exiting = true;

        // Stagger: each card gets a random delay 0–400ms so they don't all leave at once
        const staggerDelay = Math.random() * 400;

        const W = window.innerWidth, H = window.innerHeight;

        // Direction away from screen center
        const dirX = s.curX - W / 2;
        const dirY = s.curY - H / 2;
        const len = Math.sqrt(dirX * dirX + dirY * dirY) || 1;
        const nx = dirX / len;
        const ny = dirY / len;

        // Random exit personality per card
        const speed = 9 + Math.random() * 10;
        const accel = 1.08 + Math.random() * 0.08; // exponential acceleration
        const rotDir = Math.random() > 0.5 ? 1 : -1;
        const rotSpeed = 3 + Math.random() * 7;
        const suckScale = cfg.scl * 1.15; // brief scale-up before flying

        s.exitVx = nx * speed;
        s.exitVy = ny * speed;

        let phase: "suck" | "fly" = "suck";
        let suckFrame = 0;
        const suckFrames = 8; // ~130ms at 60fps

        const loop = () => {
            if (!el) return;
            const s2 = stateRef.current;

            if (phase === "suck") {
                // Brief scale pulse before launch
                suckFrame++;
                const progress = suckFrame / suckFrames;
                const sc = cfg.scl + (suckScale - cfg.scl) * Math.sin(progress * Math.PI);
                el.style.transform = `translate(${s2.curX}px,${s2.curY}px) scale(${sc}) rotate(${s2.exitRot}deg)`;
                el.style.filter = `brightness(${1 + progress * 0.5})`;
                if (suckFrame >= suckFrames) {
                    phase = "fly";
                }
                frameRef.current = requestAnimationFrame(loop);
                return;
            }

            // Fly phase — exponential acceleration
            s2.exitVx *= accel;
            s2.exitVy *= accel;
            s2.curX += s2.exitVx;
            s2.curY += s2.exitVy;
            s2.exitRot += rotSpeed * rotDir;
            s2.opacity = Math.max(0, s2.opacity - 0.045);

            const traveled = Math.sqrt(s2.exitVx ** 2 + s2.exitVy ** 2);
            const blurAmt = Math.min(traveled * 0.6, 20);
            const sc = Math.max(0, cfg.scl * (0.3 + s2.opacity * 0.7));

            el.style.transform = `translate(${s2.curX}px,${s2.curY}px) scale(${sc}) rotate(${s2.exitRot}deg)`;
            el.style.opacity = String(s2.opacity);
            el.style.filter = `blur(${blurAmt}px) brightness(${1 + (1 - s2.opacity) * 0.3})`;

            if (s2.opacity > 0) {
                frameRef.current = requestAnimationFrame(loop);
            }
        };

        const t = setTimeout(() => {
            frameRef.current = requestAnimationFrame(loop);
        }, staggerDelay);

        return () => clearTimeout(t);
    }, [exiting]);

    return (
        <div
            ref={ref}
            style={{
                position: "absolute",
                top: 0, left: 0,
                zIndex: 3,
                userSelect: "none",
                willChange: "transform, opacity",
                // opacity & transform are set immediately in useEffect before first paint
            }}
        >
            {children}
        </div>
    );
}

// ─────────────────────────────────────────────────────────────────────────────
// SCRAMBLE LETTER
// ─────────────────────────────────────────────────────────────────────────────
const CHARS = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789@#$%&*".split("");
function Letter({char,delay,phase}:{char:string;delay:number;phase:string}) {
    const [display,setDisplay] = useState("_");
    const [done,setDone] = useState(false);
    const [exitStyle,setExitStyle] = useState<React.CSSProperties>({});

    useEffect(()=>{
        if((phase==="assembling"||phase==="glitch"||phase==="hold") && !done) {
            const t=setTimeout(()=>{
                let n=0; const iv=setInterval(()=>{
                    setDisplay(CHARS[Math.floor(Math.random()*CHARS.length)]);
                    if(++n>10){clearInterval(iv);setDisplay(char);setDone(true);}
                },40);
                return ()=>clearInterval(iv);
            },delay);
            return ()=>clearTimeout(t);
        }
        if(phase==="glitch"&&done){
            const iv=setInterval(()=>{
                if(Math.random()>0.87){setDisplay(CHARS[Math.floor(Math.random()*CHARS.length)]);setTimeout(()=>setDisplay(char),75+Math.random()*60);}
            },110);
            return ()=>clearInterval(iv);
        }
        if(phase==="exit"){
            setExitStyle({
                transform:`translate(${(Math.random()-0.5)*700}px,${(Math.random()-0.5)*500}px) rotate(${(Math.random()-0.5)*720}deg) scale(0)`,
                opacity:0, filter:"blur(6px)",
                transition:`all ${0.5+Math.random()*0.5}s cubic-bezier(.55,.06,.68,.19) ${delay*0.18}ms`,
            });
        }
    },[phase,done]);

    return (
        <span style={{display:"inline-block",fontFamily:"'Courier New',monospace",fontWeight:900,color:done?"#ffffff":"#3f3f46",transition:"color 0.1s",position:"relative",...exitStyle}}>
      {display}
            {phase==="glitch"&&done&&<>
                <span style={{position:"absolute",top:0,left:0,color:"rgba(255,50,50,0.3)",transform:"translate(3px,-1px)",pointerEvents:"none",mixBlendMode:"screen"}}>{display}</span>
                <span style={{position:"absolute",top:0,left:0,color:"rgba(50,220,255,0.3)",transform:"translate(-3px,1px)",pointerEvents:"none",mixBlendMode:"screen"}}>{display}</span>
            </>}
    </span>
    );
}

// ─────────────────────────────────────────────────────────────────────────────
// MAIN INTRO
// ─────────────────────────────────────────────────────────────────────────────
export default function MiboIntro({onComplete}:{onComplete:()=>void}) {
    const [phase, setPhase] = useState<"assembling"|"glitch"|"hold"|"exit">("assembling");
    const [termStep,setTermStep] = useState(0);
    const [,setProgress] = useState(0);
    const [showSub,setShowSub] = useState(false);
    const [showBar,setShowBar] = useState(false);
    const [showTag,setShowTag] = useState(false);
    const [flash,setFlash] = useState(0);
    const [white,setWhite] = useState(0);
    const [alive,setAlive] = useState(true);
    const [cardsOut,setCardsOut] = useState(false);

    useEffect(()=>{
        const tl:[number,()=>void][]=[
            [250, ()=>setShowBar(true)],
            [450, ()=>setTermStep(1)],
            [950, ()=>setTermStep(2)],
            [1450,()=>setTermStep(3)],
            [1950,()=>setTermStep(4)],
            [2450,()=>{setTermStep(5);setProgress(100);}],
            [2900,()=>{setPhase("glitch");setShowSub(true);}],
            [3700,()=>setShowTag(true)],
            [4400,()=>setPhase("hold")],
            [5300,()=>{
                [0,100,180,280,340,440].forEach((d,i)=>
                    setTimeout(()=>setFlash(i%2===0?[1,0.7,0.4][Math.floor(i/2)]??0:0),d));
            }],
            [5700,()=>{setPhase("exit");setCardsOut(true);}],
            [6300,()=>setWhite(1)],
            [7000,()=>{setAlive(false);onComplete();}],
        ];
        let p=0;
        const piv=setInterval(()=>{if(p<99)setProgress(p++);else clearInterval(piv);},22);
        const timers=tl.map(([ms,fn])=>setTimeout(fn,ms));
        return ()=>{timers.forEach(clearTimeout);clearInterval(piv);};
    },[onComplete]);

    if(!alive) return null;

    const LINES=["Initializing AI core...","Loading generative UI engine...","Calibrating neural models...","Mounting component renderer...","System ready."];

    // Orbital configs — each card follows a unique Lissajous path across the screen
    const orbits: OrbitalConfig[] = [
        // Left column
        { cx:0.12, cy:0.20, rx:100, ry:75,  fx:1.0, fy:1.3, px:0,   py:1.2, rotSpeed:0.4, rotBase:-5, entryDelay:600,  scl:0.88, exitMx:-1.3, exitMy:-1.0 },
        { cx:0.10, cy:0.55, rx:85,  ry:110, fx:0.8, fy:1.5, px:1.0, py:3.0, rotSpeed:0.3, rotBase:3,  entryDelay:1100, scl:0.86, exitMx:-1.4, exitMy:0.3  },
        { cx:0.13, cy:0.85, rx:90,  ry:65,  fx:1.1, fy:0.8, px:2.4, py:0.8, rotSpeed:0.5, rotBase:2,  entryDelay:1700, scl:0.82, exitMx:-1.2, exitMy:1.3  },
        // Right column
        { cx:0.86, cy:0.18, rx:90,  ry:95,  fx:1.4, fy:1.0, px:2.1, py:0.5, rotSpeed:0.5, rotBase:4,  entryDelay:800,  scl:0.84, exitMx:1.3,  exitMy:-1.0 },
        { cx:0.88, cy:0.55, rx:80,  ry:100, fx:1.2, fy:0.9, px:3.5, py:1.8, rotSpeed:0.45,rotBase:-3, entryDelay:950,  scl:0.90, exitMx:1.2,  exitMy:0.4  },
        { cx:0.85, cy:0.85, rx:95,  ry:70,  fx:0.9, fy:1.2, px:0.7, py:2.5, rotSpeed:0.35,rotBase:-4, entryDelay:1500, scl:0.85, exitMx:1.4,  exitMy:1.2  },
        // Bottom center
        { cx:0.50, cy:0.88, rx:120, ry:55,  fx:0.9, fy:1.6, px:1.7, py:4.2, rotSpeed:0.35,rotBase:2,  entryDelay:1400, scl:0.82, exitMx:0.1,  exitMy:1.5  },
        // Top center-left & center-right (partially hidden behind title)
        { cx:0.30, cy:0.10, rx:100, ry:60,  fx:1.3, fy:0.7, px:3.1, py:1.0, rotSpeed:0.4, rotBase:-2, entryDelay:2000, scl:0.80, exitMx:-0.8, exitMy:-1.4 },
        { cx:0.72, cy:0.10, rx:100, ry:60,  fx:1.1, fy:0.8, px:2.0, py:2.2, rotSpeed:0.45,rotBase:3,  entryDelay:2200, scl:0.80, exitMx:0.8,  exitMy:-1.4 },
    ];
    const cardChildren = [
        <MiniBarChart/>, <MiniChat/>, <MiniSummaryPanel/>,
        <MiniDonut/>,    <MiniStat/>, <MiniDataTable/>,
        <MiniLine/>,     <MiniMarkdown/>, <MiniCategoryChips/>,
    ];

    return (
        <div style={{position:"fixed",inset:0,zIndex:9999,background:"#09090b",display:"flex",alignItems:"center",justifyContent:"center",overflow:"hidden"}}>
            <style>{`
        @keyframes dotBounce{from{transform:translateY(0)}to{transform:translateY(-4px)}}
        @keyframes ringRot{from{transform:rotate(0deg)}to{transform:rotate(360deg)}}
        @keyframes ringRotR{from{transform:rotate(0deg)}to{transform:rotate(-360deg)}}
        @keyframes pulseGlow{0%,100%{opacity:.03}50%{opacity:.09}}
        @keyframes scanMove{from{transform:translateY(-100%)}to{transform:translateY(100vh)}}
        @keyframes subIn{from{opacity:0;letter-spacing:.8em}to{opacity:1;letter-spacing:.3em}}
        @keyframes tagIn{from{opacity:0;transform:translateY(8px)}to{opacity:.45;transform:translateY(0)}}
        @keyframes termIn{from{opacity:0;transform:translateX(-8px)}to{opacity:1;transform:translateX(0)}}
        @keyframes blink{50%{opacity:0}}
        @keyframes cornerIn{from{width:0;height:0;opacity:0}to{width:22px;height:22px;opacity:.35}}
      `}</style>

            {/* Dot grid */}
            <div style={{position:"absolute",inset:0,backgroundImage:"radial-gradient(circle,rgba(255,255,255,.022) 1px,transparent 1px)",backgroundSize:"38px 38px",transform:phase==="exit"?"scale(1.1)":"scale(1)",transition:"transform 1.4s cubic-bezier(.22,1,.36,1)"}}/>
            {/* Glow */}
            <div style={{position:"absolute",inset:0,background:"radial-gradient(ellipse 55% 45% at 50% 50%,rgba(255,255,255,.035) 0%,transparent 70%)",animation:"pulseGlow 3.5s ease-in-out infinite"}}/>
            {/* Scanline */}
            <div style={{position:"absolute",inset:0,overflow:"hidden",pointerEvents:"none",zIndex:2}}>
                <div style={{position:"absolute",left:0,right:0,height:"25%",background:"linear-gradient(to bottom,transparent,rgba(255,255,255,.012),transparent)",animation:"scanMove 7s linear infinite"}}/>
            </div>

            {/* Canvas */}
            <SpaceCanvas/>

            {/* Orbital floating cards */}
            {orbits.map((cfg,i)=>(
                <OrbitalCard key={i} cfg={cfg} exiting={cardsOut}>
                    {cardChildren[i]}
                </OrbitalCard>
            ))}

            {/* Rings */}
            {[300,420,540].map((size,i)=>(
                <div key={size} style={{position:"absolute",width:size,height:size,border:`1px solid rgba(255,255,255,${0.06-i*0.015})`,borderTopColor:`rgba(255,255,255,${0.18-i*0.04})`,borderRadius:"50%",animation:`${i%2===0?"ringRot":"ringRotR"} ${10+i*5}s linear infinite`,zIndex:2,opacity:phase==="exit"?0:1,transition:"opacity 0.5s"}}/>
            ))}

            {/* Corners */}
            {[{top:"28%",left:"calc(50% - 280px)",borderTop:"1px solid",borderLeft:"1px solid"},
                {top:"28%",right:"calc(50% - 280px)",borderTop:"1px solid",borderRight:"1px solid"},
                {bottom:"28%",left:"calc(50% - 280px)",borderBottom:"1px solid",borderLeft:"1px solid"},
                {bottom:"28%",right:"calc(50% - 280px)",borderBottom:"1px solid",borderRight:"1px solid"}
            ].map((s,i)=>(
                <div key={i} style={{position:"absolute",borderColor:"rgba(255,255,255,.18)",animation:`cornerIn 0.6s ${0.2+i*0.08}s both`,zIndex:4,...s}}/>
            ))}

            {/* Main content — titlul nu se mișcă niciodată */}
            <div style={{position:"relative",zIndex:5,textAlign:"center",userSelect:"none",display:"flex",flexDirection:"column",alignItems:"center"}}>

                {/* Top label */}
                <div style={{fontFamily:"'Courier New',monospace",fontSize:10,letterSpacing:"0.38em",color:"#3f3f46",marginBottom:28,opacity:showSub?1:0,transition:"opacity 0.5s",height:18}}>
                    AI CHATBOT SYSTEM v2.0 · GENERATIVE UI
                </div>

                {/* MIBO — înălțime fixă, nu se mișcă */}
                <div style={{fontSize:"clamp(80px,20vw,172px)",lineHeight:1,display:"flex",gap:"0.03em",justifyContent:"center",marginBottom:12}}>
                    {"MIBO".split("").map((ch,i)=><Letter key={i} char={ch} delay={i*130} phase={phase}/>)}
                </div>

                {/* Subtitle */}
                <div style={{fontFamily:"'Courier New',monospace",fontSize:"clamp(9px,1.4vw,11px)",color:"#52525b",opacity:showSub?1:0,animation:showSub?"subIn 0.9s cubic-bezier(.22,1,.36,1) both":"none",letterSpacing:"0.3em",textTransform:"uppercase",height:16}}>
                    Generative UI · AI Chatbot · React + TypeScript
                </div>

                {/* Tagline — înălțime rezervată fix, apare cu fade */}
                <div style={{height:32,marginTop:16,display:"flex",alignItems:"center",justifyContent:"center"}}>
                    <div style={{fontFamily:"'Courier New',monospace",fontSize:11,letterSpacing:"0.12em",color:"#52525b",opacity:showTag?0.45:0,transition:"opacity 0.6s ease",transform:showTag?"translateY(0)":"translateY(6px)"}}>
                        Chatbot care gândește vizual
                    </div>
                </div>
            </div>

            {/* Terminal + loading bar — ABSOLUT, nu afectează layout-ul titlului */}
            {showBar && (
                <div style={{
                    position:"absolute",
                    bottom:"10%",
                    left:"50%",
                    transform:"translateX(-50%)",
                    zIndex:5,
                    display:"flex",
                    flexDirection:"column",
                    alignItems:"center",
                    gap:12,
                    pointerEvents:"none",
                }}>
                    {/* Terminal lines — înălțime fixă rezervată pentru toate 5 linii */}
                    <div style={{fontFamily:"'Courier New',monospace",fontSize:10,color:"#3f3f46",lineHeight:1.9,textAlign:"left",width:260,height:`${LINES.length * 19}px`}}>
                        {LINES.slice(0,termStep).map((line,i)=>(
                            <div key={i} style={{color:i===termStep-1?"#71717a":"#3f3f46",animation:"termIn .3s both"}}>
                                <span style={{color:"#52525b"}}>{">"} </span>{line}
                                {i===termStep-1&&<span style={{animation:"blink .8s step-end infinite"}}>_</span>}
                            </div>
                        ))}
                    </div>
                </div>
            )}

            {/* Flash */}
            <div style={{position:"absolute",inset:0,background:"white",opacity:flash,pointerEvents:"none",transition:"opacity .06s",zIndex:20}}/>
            {/* Fade to white */}
            <div style={{position:"absolute",inset:0,background:"white",opacity:white,pointerEvents:"none",transition:white?"opacity .7s ease":"none",zIndex:21}}/>
        </div>
    );
}