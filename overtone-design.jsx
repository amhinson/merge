import { useState } from "react";

const CSS = `
  @import url('https://fonts.googleapis.com/css2?family=Press+Start+2P&family=DM+Mono:wght@400;500&display=swap');
  *, *::before, *::after { box-sizing: border-box; margin: 0; padding: 0; }
  @keyframes bannerFadeOut {
    0%, 55% { opacity: 1; }
    100%     { opacity: 0; pointer-events: none; }
  }
  input:focus { outline: none; }
  ::-webkit-scrollbar { width: 0; }
`;

const C = {
  bg:      '#0f1117',
  surface: '#161b24',
  border:  '#232838',
  cyan:    '#4dd9c0',
  pink:    '#e8587a',
  amber:   '#f0b429',
  lime:    '#a3e635',
  violet:  '#a78bfa',
  orange:  '#fb923c',
  sky:     '#38bdf8',
  rose:    '#fb7185',
  muted:   'rgba(255,255,255,0.22)',
  dim:     'rgba(255,255,255,0.10)',
};
const MONO  = "'DM Mono', monospace";
const PIXEL = "'Press Start 2P', monospace";

// ─── Ball definitions ─────────────────────────────────────────────────────────
const BALLS = [
  { id:1,  color:C.cyan,   size:92, freq:3, pixelW:6, pixelH:4, waveType:'sine'     },
  { id:2,  color:C.pink,   size:76, freq:4, pixelW:5, pixelH:4, waveType:'sine'     },
  { id:3,  color:C.amber,  size:64, freq:2, pixelW:4, pixelH:3, waveType:'triangle' },
  { id:4,  color:C.violet, size:54, freq:5, pixelW:4, pixelH:3, waveType:'sine'     },
  { id:5,  color:C.lime,   size:46, freq:3, pixelW:3, pixelH:3, waveType:'sawtooth' },
  { id:6,  color:C.sky,    size:38, freq:6, pixelW:3, pixelH:2, waveType:'sine'     },
  { id:7,  color:C.orange, size:32, freq:4, pixelW:3, pixelH:2, waveType:'square'   },
  { id:8,  color:C.rose,   size:26, freq:3, pixelW:2, pixelH:2, waveType:'triangle' },
  { id:9,  color:C.cyan,   size:22, freq:5, pixelW:2, pixelH:2, waveType:'sine'     },
  { id:10, color:C.amber,  size:18, freq:4, pixelW:2, pixelH:2, waveType:'sawtooth' },
  { id:11, color:C.pink,   size:14, freq:3, pixelW:2, pixelH:2, waveType:'sine'     },
];

// ─── WaveformBall ─────────────────────────────────────────────────────────────
function WaveformBall({ color, size, freq=3, pixelW=4, pixelH=3, waveType='sine' }) {
  const r    = size / 2;
  const uid  = `b_${color.replace('#','')}${size}`;
  const cols = Math.floor(size / pixelW);
  const amp  = r * 0.28, cy = r * 0.72;

  const waveY = i => {
    const t = i / cols;
    switch (waveType) {
      case 'square':   return cy + amp * (Math.sin(freq*2*Math.PI*t) >= 0 ? 1 : -1);
      case 'sawtooth': return cy + amp * (2*((freq*t)%1)-1);
      case 'triangle': return cy + amp * (2*Math.abs(2*((freq*t+0.25)%1)-1)-1);
      default:         return cy + amp * Math.sin(freq*2*Math.PI*t);
    }
  };

  const pixelRects = [], edgeRects = [];
  for (let col = 0; col < cols; col++) {
    const x = col*pixelW, top = waveY(col);
    const rows = Math.ceil((size - top) / pixelH);
    for (let row = 0; row < rows; row++) {
      const y = top + row*pixelH;
      if (y+pixelH > size) continue;
      pixelRects.push(
        <rect key={`${col}-${row}`} x={x+0.5} y={y+0.5} width={pixelW-1} height={pixelH-1}
          fill={color} fillOpacity={row===0 ? 0.9 : Math.max(0.08, 0.38-row*0.045)}/>
      );
    }
    edgeRects.push(
      <rect key={`e${col}`} x={x+0.5} y={waveY(col)+0.5} width={pixelW-1} height={pixelH-1}
        fill={color} fillOpacity={0.95}/>
    );
  }

  return (
    <div style={{ width:size, height:size, flexShrink:0, filter:`drop-shadow(0 0 5px ${color}50)` }}>
      <svg width={size} height={size} viewBox={`0 0 ${size} ${size}`} style={{ overflow:'hidden', display:'block' }}>
        <defs>
          <clipPath id={`cp-${uid}`}><circle cx={r} cy={r} r={r-0.5}/></clipPath>
          <radialGradient id={`rg-${uid}`} cx="38%" cy="32%" r="65%">
            <stop offset="0%"   stopColor={color} stopOpacity="0.45"/>
            <stop offset="100%" stopColor={color} stopOpacity="0.10"/>
          </radialGradient>
        </defs>
        <circle cx={r} cy={r} r={r-1} fill={`url(#rg-${uid})`} stroke={color} strokeWidth="1.5" strokeOpacity="0.85"/>
        <g clipPath={`url(#cp-${uid})`}>{pixelRects}{edgeRects}</g>
        <ellipse cx={r*0.62} cy={r*0.40} rx={r*0.20} ry={r*0.10} fill="white" fillOpacity="0.13" clipPath={`url(#cp-${uid})`}/>
      </svg>
    </div>
  );
}

// ─── Shell ────────────────────────────────────────────────────────────────────
function Shell({ children, pt = 32 }) {
  return (
    <div style={{
      position:'absolute', inset:0, background:C.bg,
      fontFamily:MONO, color:'#fff',
      display:'flex', flexDirection:'column',
    }}>
      <div style={{
        position:'absolute', top:0, left:0, right:0, height:200,
        background:`linear-gradient(to bottom, ${C.cyan}07, transparent)`,
        pointerEvents:'none',
      }}/>
      {/* Safe area top spacer */}
      <div style={{ height: pt, flexShrink:0 }}/>
      {children}
    </div>
  );
}

function Stat({ label, value, color, last }) {
  return (
    <div style={{ flex:1, textAlign:'center', padding:'13px 0', borderRight: last ? 'none' : `1px solid ${C.border}` }}>
      <div style={{ fontSize:22, color, fontWeight:500 }}>{value}</div>
      <div style={{ fontSize:8, color:C.muted, marginTop:5, fontFamily:PIXEL, letterSpacing:0.5 }}>{label}</div>
    </div>
  );
}

// ─── Onboarding Screen ────────────────────────────────────────────────────────
/*
  ANIMATION NOTES FOR CLAUDE CODE:
  The onboarding is a 3-step auto-advancing demo:

  Step 1 — "DROP":
    Two small identical balls (C9, size 22) float at the top-center with a gentle
    bobbing float animation (translateY ±6px, 1.8s ease-in-out infinite).
    One ball slowly falls toward the other (gravity simulation: ease-in over 600ms).

  Step 2 — "MERGE":
    On contact, both balls shrink to zero scale simultaneously (scale 1→0, 120ms ease-in),
    then a new larger ball (C8, size 26) bursts in at the collision point with a
    spring overshoot: scale 0→1.15→1.0 over 300ms. A brief radial particle burst
    (6–8 pixel-square sparks in the ball's color) fires outward from center and fades
    over 250ms. The score counter ticks up with a quick +NNN pop animation.

  Step 3 — "REPEAT":
    The merged ball settles at the bottom. Another small ball appears at top and
    the cycle restarts. After 3 full merges the CTA "TAP TO START" pulses in.

  All steps use spring physics (react-spring or custom spring easing).
  Between steps, a 400ms pause before the next drop begins.
*/
function OnboardingScreen({ onStart }) {
  const [step, setStep] = useState(0); // 0=drop, 1=merge, 2=merged

  const steps = [
    { label: 'DROP', sub: 'drop matching balls' },
    { label: 'MERGE', sub: 'same size = they combine' },
    { label: 'SCORE', sub: 'bigger merges = more points' },
  ];

  return (
    <Shell pt={48}>
      {/* Logo */}
      <div style={{ textAlign:'center', padding:'0 24px 0' }}>
        <div style={{ fontFamily:PIXEL, fontSize:20, letterSpacing:3, lineHeight:1.4 }}>
          OVER<span style={{ color:C.cyan }}>TONE</span>
        </div>
        <div style={{ fontSize:10, color:C.muted, marginTop:8, letterSpacing:5 }}>A DAILY DROP</div>
      </div>

      {/* Demo stage */}
      <div style={{
        flex:1, display:'flex', alignItems:'center', justifyContent:'center',
        position:'relative',
      }}>

        {/* Arena outline */}
        <div style={{
          width:220, height:260, position:'relative',
          background:C.surface, border:`1px solid ${C.border}`, borderRadius:6,
          overflow:'hidden',
        }}>
          {/* Faint grid */}
          <div style={{
            position:'absolute', inset:0, pointerEvents:'none',
            backgroundImage:`linear-gradient(${C.border} 1px, transparent 1px), linear-gradient(90deg, ${C.border} 1px, transparent 1px)`,
            backgroundSize:'28px 28px', opacity:0.5,
          }}/>

          {/* Step 0: two small balls about to drop */}
          {step === 0 && <>
            {/* Dropper */}
            <div style={{ position:'absolute', left:'50%', top:12, transform:'translateX(-50%)' }}>
              <WaveformBall color={C.cyan} size={22} freq={5} pixelW={2} pixelH={2} waveType="sine"/>
            </div>
            {/* Already-placed ball */}
            <div style={{ position:'absolute', left:'50%', bottom:16, transform:'translateX(-50%)' }}>
              <WaveformBall color={C.cyan} size={22} freq={5} pixelW={2} pixelH={2} waveType="sine"/>
            </div>
            {/* Drop line */}
            <div style={{
              position:'absolute', left:'50%', top:34, bottom:38,
              width:1, transform:'translateX(-50%)',
              background:`linear-gradient(to bottom, ${C.cyan}40, transparent)`,
            }}/>
          </>}

          {/* Step 1: collision flash — both small balls visible at merge point */}
          {step === 1 && <>
            <div style={{ position:'absolute', left:'50%', bottom:16, transform:'translateX(-50%)' }}>
              <WaveformBall color={C.cyan} size={22} freq={5} pixelW={2} pixelH={2} waveType="sine"/>
            </div>
            <div style={{ position:'absolute', left:'50%', bottom:38, transform:'translateX(-50%)' }}>
              <WaveformBall color={C.cyan} size={22} freq={5} pixelW={2} pixelH={2} waveType="sine"/>
            </div>
            {/* Merge ring flash */}
            <div style={{
              position:'absolute', left:'50%', bottom:27,
              width:40, height:40, borderRadius:'50%',
              transform:'translate(-50%, 50%)',
              border:`2px solid ${C.cyan}`,
              opacity:0.5,
            }}/>
          </>}

          {/* Step 2: merged ball + smaller ball above */}
          {step === 2 && <>
            {/* New dropper coming in */}
            <div style={{ position:'absolute', left:'50%', top:12, transform:'translateX(-50%)' }}>
              <WaveformBall color={C.rose} size={26} freq={3} pixelW={2} pixelH={2} waveType="triangle"/>
            </div>
            {/* Merged result */}
            <div style={{ position:'absolute', left:'50%', bottom:12, transform:'translateX(-50%)' }}>
              <WaveformBall color={C.rose} size={26} freq={3} pixelW={2} pixelH={2} waveType="triangle"/>
            </div>
            {/* Score tick */}
            <div style={{
              position:'absolute', left:'50%', bottom:52, transform:'translateX(-50%)',
              fontFamily:PIXEL, fontSize:7, color:C.amber, whiteSpace:'nowrap',
            }}>+ 100</div>
          </>}
        </div>

        {/* Step indicators */}
        <div style={{
          position:'absolute', bottom:24, left:0, right:0,
          display:'flex', justifyContent:'center', gap:6,
        }}>
          {steps.map((_,i) => (
            <div key={i} style={{
              width: i===step ? 20 : 6, height:6, borderRadius:3,
              background: i===step ? C.cyan : C.border,
              transition:'all 0.3s',
            }}/>
          ))}
        </div>
      </div>

      {/* Step label */}
      <div style={{ textAlign:'center', padding:'0 24px 16px' }}>
        <div style={{ fontFamily:PIXEL, fontSize:12, color:'#fff', letterSpacing:2, marginBottom:8 }}>
          {steps[step].label}
        </div>
        <div style={{ fontSize:12, color:C.muted }}>{steps[step].sub}</div>
      </div>

      {/* Step nav / CTA */}
      <div style={{ padding:'0 24px 48px', display:'flex', gap:8 }}>
        {step < steps.length - 1 ? (
          <>
            <button onClick={() => setStep(s => s+1)} style={{
              flex:1, padding:'14px 0',
              background:C.cyan, border:'none', borderRadius:4,
              fontFamily:PIXEL, fontSize:10, letterSpacing:2, color:C.bg, cursor:'pointer',
            }}>NEXT →</button>
            <button onClick={onStart} style={{
              padding:'14px 16px',
              background:'transparent', border:`1px solid ${C.border}`, borderRadius:4,
              fontFamily:PIXEL, fontSize:9, letterSpacing:1, color:C.dim, cursor:'pointer',
            }}>SKIP</button>
          </>
        ) : (
          <button onClick={onStart} style={{
            flex:1, padding:'16px 0',
            background:C.cyan, border:'none', borderRadius:4,
            fontFamily:PIXEL, fontSize:11, letterSpacing:2, color:C.bg, cursor:'pointer',
          }}>LET'S PLAY</button>
        )}
      </div>
    </Shell>
  );
}

// ─── Home Screen — First visit (no score yet today) ───────────────────────────
function HomeScreenFresh({ onPlay, onSettings, onLeaderboard }) {
  const [c1,c2,c3] = [BALLS[0],BALLS[1],BALLS[2]];
  return (
    <Shell>
      <div style={{ display:'flex', justifyContent:'flex-end', padding:'0 24px 0' }}>
        <button onClick={onSettings} style={{
          background:'transparent', border:`1px solid ${C.border}`,
          borderRadius:4, padding:'4px 8px', cursor:'pointer',
          color:C.muted, fontSize:14, lineHeight:1,
        }}>⚙</button>
      </div>

      <div style={{ padding:'20px 24px 0' }}>
        <div style={{ fontFamily:PIXEL, fontSize:24, letterSpacing:2, lineHeight:1.4 }}>
          OVER<br/><span style={{ color:C.cyan }}>TONE</span>
        </div>
        <div style={{ fontSize:11, color:C.muted, marginTop:10, letterSpacing:5 }}>A DAILY DROP</div>
      </div>

      {/* Balls */}
      <div style={{ flex:1, display:'flex', alignItems:'center', justifyContent:'center', gap:24 }}>
        <WaveformBall {...c2}/>
        <WaveformBall {...c1}/>
        <WaveformBall {...c3}/>
      </div>

      {/* Puzzle row */}
      <div style={{ display:'flex', alignItems:'center', gap:14, padding:'0 24px 18px' }}>
        <span style={{ fontFamily:PIXEL, fontSize:10, color:C.cyan, letterSpacing:1 }}>#142</span>
        <div style={{ flex:1, height:1, background:C.border }}/>
        <span style={{ fontSize:11, color:C.muted, letterSpacing:1 }}>MAR 22</span>
      </div>

      {/* Top 3 */}
      <div style={{ margin:'0 24px 0', border:`1px solid ${C.border}`, borderRadius:4, overflow:'hidden', background:C.surface }}>
        <div style={{ padding:'7px 10px', borderBottom:`1px solid ${C.border}`, display:'flex', justifyContent:'space-between', alignItems:'center' }}>
          <span style={{ fontFamily:PIXEL, fontSize:7, color:C.muted, letterSpacing:1 }}>TODAY'S TOP</span>
          <button onClick={onLeaderboard} style={{ background:'transparent', border:'none', cursor:'pointer', padding:0, fontFamily:PIXEL, fontSize:7, color:C.cyan, letterSpacing:1 }}>ALL →</button>
        </div>
        {[
          {rank:1, name:'zander',   score:9150, medal:'🥇'},
          {rank:2, name:'drumr99',  score:7840, medal:'🥈'},
          {rank:3, name:'mollywop', score:6210, medal:'🥉'},
        ].map((row,i,arr) => (
          <div key={row.rank} style={{ display:'flex', alignItems:'center', padding:'7px 10px', borderBottom: i<arr.length-1 ? `1px solid ${C.border}` : 'none' }}>
            <span style={{ fontSize:13, marginRight:8 }}>{row.medal}</span>
            <span style={{ flex:1, fontFamily:MONO, fontSize:13, color:C.muted }}>{row.name}</span>
            <span style={{ fontFamily:MONO, fontSize:13, color:'rgba(255,255,255,0.35)' }}>{row.score.toLocaleString()}</span>
          </div>
        ))}
      </div>

      {/* CTA */}
      <div style={{ padding:'16px 24px 44px' }}>
        <button onClick={onPlay} style={{
          width:'100%', padding:'16px 0',
          background:C.cyan, border:'none', borderRadius:4,
          fontFamily:PIXEL, fontSize:11, letterSpacing:2, color:C.bg, cursor:'pointer',
        }}>PLAY</button>
        <div style={{ textAlign:'center', marginTop:10, fontSize:10, color:C.dim, letterSpacing:1 }}>
          1 scored play per day
        </div>
      </div>
    </Shell>
  );
}

// ─── Home Screen — Return visit (already played) ──────────────────────────────
function HomeScreenPlayed({ onShare, onPractice, onSettings, onLeaderboard }) {
  const [c1,c2,c3] = [BALLS[0],BALLS[1],BALLS[2]];
  return (
    <Shell>
      <div style={{ display:'flex', justifyContent:'flex-end', padding:'0 24px 0' }}>
        <button onClick={onSettings} style={{
          background:'transparent', border:`1px solid ${C.border}`,
          borderRadius:4, padding:'4px 8px', cursor:'pointer',
          color:C.muted, fontSize:14, lineHeight:1,
        }}>⚙</button>
      </div>

      <div style={{ padding:'20px 24px 0' }}>
        <div style={{ fontFamily:PIXEL, fontSize:24, letterSpacing:2, lineHeight:1.4 }}>
          OVER<br/><span style={{ color:C.cyan }}>TONE</span>
        </div>
        <div style={{ fontSize:11, color:C.muted, marginTop:10, letterSpacing:5 }}>A DAILY DROP</div>
      </div>

      <div style={{ flex:1, display:'flex', alignItems:'center', justifyContent:'center', gap:24 }}>
        <WaveformBall {...c2}/>
        <WaveformBall {...c1}/>
        <WaveformBall {...c3}/>
      </div>

      <div style={{ display:'flex', alignItems:'center', gap:14, padding:'0 24px 18px' }}>
        <span style={{ fontFamily:PIXEL, fontSize:10, color:C.cyan, letterSpacing:1 }}>#142</span>
        <div style={{ flex:1, height:1, background:C.border }}/>
        <span style={{ fontSize:11, color:C.muted, letterSpacing:1 }}>MAR 22</span>
      </div>

      <div style={{ display:'flex', borderTop:`1px solid ${C.border}` }}>
        <Stat label="TODAY" value="2,340" color="#fff"/>
        <Stat label="BEST"  value="4,820" color={C.cyan} last/>
      </div>

      {/* Top 3 + your rank */}
      <div style={{ margin:'12px 24px 0', border:`1px solid ${C.border}`, borderRadius:4, overflow:'hidden', background:C.surface }}>
        <div style={{ padding:'7px 10px', borderBottom:`1px solid ${C.border}`, display:'flex', justifyContent:'space-between', alignItems:'center' }}>
          <span style={{ fontFamily:PIXEL, fontSize:7, color:C.muted, letterSpacing:1 }}>TODAY'S TOP</span>
          <button onClick={onLeaderboard} style={{ background:'transparent', border:'none', cursor:'pointer', padding:0, fontFamily:PIXEL, fontSize:7, color:C.cyan, letterSpacing:1 }}>ALL →</button>
        </div>
        {[
          {rank:1, name:'zander',   score:9150, medal:'🥇'},
          {rank:2, name:'drumr99',  score:7840, medal:'🥈'},
          {rank:3, name:'mollywop', score:6210, medal:'🥉'},
        ].map((row,i,arr) => (
          <div key={row.rank} style={{ display:'flex', alignItems:'center', padding:'7px 10px', borderBottom: i<arr.length-1 ? `1px solid ${C.border}` : 'none' }}>
            <span style={{ fontSize:13, marginRight:8 }}>{row.medal}</span>
            <span style={{ flex:1, fontFamily:MONO, fontSize:13, color:C.muted }}>{row.name}</span>
            <span style={{ fontFamily:MONO, fontSize:13, color:'rgba(255,255,255,0.35)' }}>{row.score.toLocaleString()}</span>
          </div>
        ))}
        <div style={{ display:'flex', alignItems:'center', padding:'7px 10px', borderTop:`1px solid ${C.border}`, background:`${C.cyan}0e` }}>
          <span style={{ fontFamily:PIXEL, fontSize:7, color:C.cyan, marginRight:10, flexShrink:0 }}>#20</span>
          <span style={{ flex:1, fontFamily:MONO, fontSize:13, color:C.cyan }}>YOU</span>
          <span style={{ fontFamily:MONO, fontSize:13, color:C.cyan }}>2,340</span>
        </div>
      </div>

      {/* Post-play CTA */}
      <div style={{ padding:'12px 24px 44px' }}>
        <div style={{
          display:'flex', alignItems:'center', justifyContent:'center', gap:10,
          border:`1px solid ${C.border}`, borderRadius:4, padding:'11px 0', marginBottom:10,
          fontFamily:PIXEL, fontSize:10, letterSpacing:2, color:C.muted,
        }}>
          <span>🔒</span><span>SCORED</span>
          <span style={{ fontFamily:MONO, fontSize:14, color:C.cyan, letterSpacing:0 }}>2,340</span>
        </div>
        <div style={{ display:'flex', gap:8 }}>
          <button onClick={onShare} style={{
            flex:1, padding:'14px 0', background:C.cyan, border:'none', borderRadius:4,
            fontFamily:PIXEL, fontSize:10, letterSpacing:1, color:C.bg, cursor:'pointer',
          }}>SHARE</button>
          <button onClick={onPractice} style={{
            flex:1, padding:'14px 0', background:'transparent', border:`1px solid ${C.border}`, borderRadius:4,
            fontFamily:PIXEL, fontSize:9, letterSpacing:1, color:C.muted, cursor:'pointer',
          }}>PLAY AGAIN</button>
        </div>
        <div style={{ textAlign:'center', marginTop:8, fontSize:10, color:C.dim }}>
          only first score of the day is counted
        </div>
      </div>
    </Shell>
  );
}

// ─── Settings Screen ──────────────────────────────────────────────────────────
function SettingsScreen({ onBack }) {
  const [username, setUsername] = useState('phishphan');
  const [haptic, setHaptic]     = useState(true);
  return (
    <Shell>
      <div style={{ display:'flex', alignItems:'center', gap:14, padding:'0 24px 24px' }}>
        <button onClick={onBack} style={{
          background:'transparent', border:`1px solid ${C.border}`,
          borderRadius:4, padding:'6px 12px', cursor:'pointer',
          fontFamily:MONO, fontSize:12, color:C.muted,
        }}>←</button>
        <span style={{ fontFamily:PIXEL, fontSize:11, letterSpacing:2, color:'#fff' }}>SETTINGS</span>
      </div>

      <div style={{ flex:1, padding:'0 24px', display:'flex', flexDirection:'column', gap:2 }}>
        <div style={{ fontFamily:PIXEL, fontSize:7, color:C.dim, letterSpacing:1, marginBottom:8 }}>USERNAME</div>
        <div style={{ display:'flex', alignItems:'center', background:C.surface, border:`1px solid ${C.border}`, borderRadius:4, padding:'0 14px', marginBottom:24 }}>
          <span style={{ color:C.cyan, fontSize:14, marginRight:8 }}>@</span>
          <input
            value={username} onChange={e => setUsername(e.target.value)} maxLength={16}
            style={{ flex:1, background:'transparent', border:'none', fontFamily:MONO, fontSize:14, color:'#fff', padding:'14px 0' }}
          />
          <span style={{ fontFamily:PIXEL, fontSize:7, color:C.dim }}>{username.length}/16</span>
        </div>

        <div style={{ fontFamily:PIXEL, fontSize:7, color:C.dim, letterSpacing:1, marginBottom:8 }}>CONTROLS</div>
        <div style={{
          display:'flex', alignItems:'center', justifyContent:'space-between',
          background:C.surface, border:`1px solid ${C.border}`, borderRadius:4,
          padding:'14px', marginBottom:24,
        }}>
          <div>
            <div style={{ fontFamily:MONO, fontSize:14, color:'#fff', marginBottom:3 }}>Haptic feedback</div>
            <div style={{ fontFamily:MONO, fontSize:11, color:C.muted }}>Vibrate on merge</div>
          </div>
          <div onClick={() => setHaptic(h => !h)} style={{
            width:48, height:26, borderRadius:13, cursor:'pointer', flexShrink:0,
            background:haptic ? C.cyan : C.border,
            position:'relative', transition:'background 0.2s',
          }}>
            <div style={{
              position:'absolute', top:3, left:haptic ? 25 : 3,
              width:20, height:20, borderRadius:'50%',
              background:'#fff', transition:'left 0.2s',
              boxShadow:'0 1px 3px rgba(0,0,0,0.4)',
            }}/>
          </div>
        </div>

        <div style={{
          padding:'14px', borderRadius:4, border:`1px dashed ${C.border}`,
          fontFamily:PIXEL, fontSize:7, color:C.dim, letterSpacing:1, textAlign:'center', lineHeight:2,
        }}>MORE SETTINGS COMING SOON</div>
      </div>

      <div style={{ padding:'24px 24px 44px' }}>
        <button onClick={onBack} style={{
          width:'100%', padding:'16px 0', background:C.cyan, border:'none', borderRadius:4,
          fontFamily:PIXEL, fontSize:11, letterSpacing:2, color:C.bg, cursor:'pointer',
        }}>SAVE</button>
      </div>
    </Shell>
  );
}

// ─── Leaderboard Screen ───────────────────────────────────────────────────────
const SCORES = [
  {rank:1,  name:'zander',    score:9150},
  {rank:2,  name:'drumr99',   score:7840},
  {rank:3,  name:'mollywop',  score:6210},
  {rank:4,  name:'phishphan', score:5890},
  {rank:5,  name:'tunafish',  score:5440},
  {rank:6,  name:'ghost',     score:4990},
  {rank:7,  name:'reba',      score:4520},
  {rank:8,  name:'llama',     score:4100},
  {rank:9,  name:'carini',    score:3780},
  {rank:10, name:'antelope',  score:3450},
  {rank:11, name:'tweezer',   score:3120},
  {rank:12, name:'bathtub',   score:2980},
  {rank:13, name:'gin',       score:2750},
  {rank:14, name:'axilla',    score:2620},
  {rank:15, name:'wilson',    score:2480},
  {rank:16, name:'mockbird',  score:2410},
  {rank:17, name:'piper',     score:2380},
  {rank:18, name:'forbin',    score:2360},
  {rank:19, name:'harpua',    score:2350},
  {rank:20, name:'YOU',       score:2340, you:true},
  {rank:21, name:'batboy',    score:2200},
  {rank:22, name:'glide',     score:2100},
  {rank:23, name:'possum',    score:1980},
  {rank:24, name:'weekapaug', score:1850},
  {rank:25, name:'limb',      score:1700},
];
const DAYS = [{num:139,date:'MAR 19'},{num:140,date:'MAR 20'},{num:141,date:'MAR 21'},{num:142,date:'MAR 22'}];

function LeaderboardScreen({ onBack }) {
  const [dayIdx, setDayIdx] = useState(3);
  const day = DAYS[dayIdx];
  return (
    <Shell>
      <div style={{ display:'flex', alignItems:'center', gap:10, padding:'0 24px 12px' }}>
        <button onClick={onBack} style={{ background:'transparent', border:`1px solid ${C.border}`, borderRadius:4, padding:'6px 12px', cursor:'pointer', fontFamily:MONO, fontSize:12, color:C.muted, flexShrink:0 }}>←</button>
        <div style={{ flex:1, display:'flex', alignItems:'center', justifyContent:'center', gap:12, background:C.surface, border:`1px solid ${C.border}`, borderRadius:4, padding:'8px 14px' }}>
          <button onClick={() => setDayIdx(i => Math.max(0, i-1))} disabled={dayIdx===0}
            style={{ background:'transparent', border:'none', cursor:dayIdx===0?'default':'pointer', fontFamily:MONO, fontSize:16, color:dayIdx===0?C.dim:C.muted, padding:0 }}>‹</button>
          <div style={{ textAlign:'center', flex:1 }}>
            <div style={{ fontFamily:PIXEL, fontSize:9, color:C.cyan, letterSpacing:1 }}>#{day.num}</div>
            <div style={{ fontFamily:MONO, fontSize:11, color:C.muted, marginTop:2 }}>{day.date}</div>
          </div>
          <button onClick={() => setDayIdx(i => Math.min(DAYS.length-1, i+1))} disabled={dayIdx===DAYS.length-1}
            style={{ background:'transparent', border:'none', cursor:dayIdx===DAYS.length-1?'default':'pointer', fontFamily:MONO, fontSize:16, color:dayIdx===DAYS.length-1?C.dim:C.muted, padding:0 }}>›</button>
        </div>
      </div>
      <div style={{ flex:1, overflowY:'auto', padding:'0 24px 32px' }}>
        {SCORES.map(row => (
          <div key={row.rank} style={{
            display:'flex', alignItems:'center', padding:'9px 10px', borderRadius:4, marginBottom:2,
            background:row.you ? `${C.cyan}10` : 'transparent',
            border:row.you ? `1px solid ${C.cyan}28` : '1px solid transparent',
          }}>
            <div style={{ fontFamily:PIXEL, fontSize:7, color:row.rank<=3?C.amber:row.you?C.cyan:C.dim, width:32, flexShrink:0 }}>
              {row.rank===1?'🥇':row.rank===2?'🥈':row.rank===3?'🥉':`#${row.rank}`}
            </div>
            <div style={{ flex:1, fontFamily:MONO, fontSize:13, color:row.you?C.cyan:C.muted, fontWeight:row.you?500:400 }}>{row.name}</div>
            <div style={{ fontFamily:MONO, fontSize:13, color:row.you?C.cyan:'rgba(255,255,255,0.32)', fontWeight:row.you?500:400 }}>{row.score.toLocaleString()}</div>
          </div>
        ))}
      </div>
    </Shell>
  );
}

// ─── Game Screen ──────────────────────────────────────────────────────────────
const PLACED_DEFS = [
  {ballIdx:0, x:80,  y:515},
  {ballIdx:1, x:192, y:524},
  {ballIdx:2, x:303, y:518},
  {ballIdx:3, x:138, y:472},
  {ballIdx:4, x:265, y:477},
  {ballIdx:5, x:48,  y:471},
  {ballIdx:6, x:212, y:442},
  {ballIdx:7, x:94,  y:437},
  {ballIdx:8, x:308, y:445},
];

function GameScreen({ onGameOver, onBack, practice=false }) {
  const dropper = BALLS[5], next = BALLS[6];
  return (
    <Shell pt={16}>
      <div style={{ padding:'0 16px 8px' }}>
        <div style={{ display:'flex', alignItems:'center', gap:10 }}>
          <button onClick={onBack} style={{ background:'transparent', border:`1px solid ${C.border}`, borderRadius:4, padding:'6px 10px', cursor:'pointer', fontFamily:MONO, fontSize:11, color:C.muted, flexShrink:0 }}>←</button>
          <div style={{ flex:1 }}>
            <div style={{ fontFamily:PIXEL, fontSize:7, color:C.muted, letterSpacing:2, marginBottom:1 }}>SCORE</div>
            <div style={{ fontSize:28, color:C.cyan, fontWeight:500, lineHeight:1 }}>2,340</div>
          </div>
          <div style={{ display:'flex', flexDirection:'column', alignItems:'center', gap:3, background:C.surface, border:`1px solid ${C.border}`, borderRadius:6, padding:'5px 8px', flexShrink:0 }}>
            <div style={{ fontFamily:PIXEL, fontSize:6, color:C.dim, letterSpacing:1 }}>NEXT</div>
            <WaveformBall color={next.color} size={next.size} freq={next.freq} pixelW={next.pixelW} pixelH={next.pixelH} waveType={next.waveType}/>
          </div>
        </div>
      </div>

      <div style={{ flex:1, margin:'4px 16px 16px', position:'relative', overflow:'hidden', background:C.surface, border:`1px solid ${C.border}`, borderRadius:4 }}>
        <div style={{ position:'absolute', inset:0, pointerEvents:'none', backgroundImage:`linear-gradient(${C.border} 1px, transparent 1px), linear-gradient(90deg, ${C.border} 1px, transparent 1px)`, backgroundSize:'40px 40px', opacity:0.55 }}/>
        <div style={{ position:'absolute', left:0, right:0, top:70, height:1, background:`${C.pink}28`, pointerEvents:'none' }}>
          <span style={{ position:'absolute', right:8, top:-13, fontFamily:PIXEL, fontSize:7, color:`${C.pink}55` }}>DANGER</span>
        </div>

        {practice && (
          <div style={{
            position:'absolute', top:10, left:'50%', transform:'translateX(-50%)',
            zIndex:10, pointerEvents:'none',
            background:`${C.amber}18`, border:`1px solid ${C.amber}40`,
            borderRadius:3, padding:'4px 10px',
            fontFamily:PIXEL, fontSize:7, color:C.amber, letterSpacing:1, whiteSpace:'nowrap',
            animation:'bannerFadeOut 4s ease-in-out forwards',
          }}>only first score of the day is counted</div>
        )}

        {PLACED_DEFS.map((p,i) => {
          const b = BALLS[p.ballIdx];
          return (
            <div key={i} style={{ position:'absolute', left:p.x-b.size/2, top:p.y-b.size/2 }}>
              <WaveformBall color={b.color} size={b.size} freq={b.freq} pixelW={b.pixelW} pixelH={b.pixelH} waveType={b.waveType}/>
            </div>
          );
        })}
        <div style={{ position:'absolute', left:`calc(50% - ${dropper.size/2}px)`, top:20 }}>
          <WaveformBall color={dropper.color} size={dropper.size} freq={dropper.freq} pixelW={dropper.pixelW} pixelH={dropper.pixelH} waveType={dropper.waveType}/>
        </div>

        {/* Trigger for design demo */}
        <button onClick={onGameOver} style={{
          position:'absolute', bottom:8, right:8,
          background:`${C.pink}20`, border:`1px solid ${C.pink}40`,
          borderRadius:3, padding:'4px 8px', cursor:'pointer',
          fontFamily:PIXEL, fontSize:6, color:C.pink, letterSpacing:1,
        }}>END →</button>
      </div>
    </Shell>
  );
}



// ─── Result Overlay ───────────────────────────────────────────────────────────
const MERGE_COUNTS = [
  {ballIdx:0,count:1},{ballIdx:1,count:2},{ballIdx:2,count:3},
  {ballIdx:3,count:5},{ballIdx:4,count:7},{ballIdx:5,count:9},
  {ballIdx:6,count:6},{ballIdx:7,count:8},{ballIdx:8,count:4},
  {ballIdx:9,count:3},{ballIdx:10,count:0},
];

function ResultOverlay({ onShare, onHome }) {
  const ds = s => Math.max(16, Math.round(s*0.38));
  return (
    <div style={{
      position:'absolute', inset:0, zIndex:100,
      background:'rgba(8,8,14,0.88)',
      display:'flex', flexDirection:'column', alignItems:'center', justifyContent:'center',
      padding:'0 28px',
      backdropFilter:'blur(2px)',
    }}>
      <div style={{ textAlign:'center', marginBottom:8 }}>
        <div style={{ fontFamily:PIXEL, fontSize:8, color:C.muted, letterSpacing:3, marginBottom:10 }}>FINAL SCORE</div>
        <div style={{ fontFamily:MONO, fontSize:56, color:C.cyan, fontWeight:500, lineHeight:1 }}>2,340</div>
      </div>

      <div style={{ display:'flex', alignItems:'center', gap:8, marginBottom:24, background:`${C.cyan}0c`, border:`1px solid ${C.cyan}28`, borderRadius:4, padding:'7px 16px' }}>
        <span style={{ fontFamily:PIXEL, fontSize:8, color:C.cyan, letterSpacing:1 }}>#20</span>
        <span style={{ fontFamily:MONO, fontSize:12, color:C.muted }}>of 847 players today</span>
      </div>

      <div style={{ width:'100%', background:C.surface, border:`1px solid ${C.border}`, borderRadius:4, padding:'14px 12px', marginBottom:24 }}>
        <div style={{ fontFamily:PIXEL, fontSize:7, color:C.dim, letterSpacing:1, marginBottom:12 }}>MERGES</div>
        <div style={{ display:'flex', flexWrap:'wrap', gap:'10px 6px' }}>
          {MERGE_COUNTS.map(({ballIdx,count}) => {
            const b = BALLS[ballIdx], sz = ds(b.size);
            return (
              <div key={ballIdx} style={{ display:'flex', flexDirection:'column', alignItems:'center', gap:4, opacity:count===0?0.25:1, minWidth:sz }}>
                <WaveformBall color={b.color} size={sz} freq={b.freq} pixelW={Math.max(2,Math.floor(b.pixelW*0.5))} pixelH={Math.max(2,Math.floor(b.pixelH*0.5))} waveType={b.waveType}/>
                <span style={{ fontFamily:PIXEL, fontSize:6, color:count>0?b.color:C.dim }}>{count>0?`×${count}`:'—'}</span>
              </div>
            );
          })}
        </div>
      </div>

      <div style={{ display:'flex', gap:10, width:'100%' }}>
        <button onClick={onHome} style={{ flex:1, padding:'14px 0', background:'transparent', border:`1px solid ${C.border}`, borderRadius:4, fontFamily:PIXEL, fontSize:9, letterSpacing:1, color:C.muted, cursor:'pointer' }}>DONE</button>
        <button onClick={onShare} style={{ flex:2, padding:'14px 0', background:C.cyan, border:'none', borderRadius:4, fontFamily:PIXEL, fontSize:10, letterSpacing:2, color:C.bg, cursor:'pointer' }}>SHARE</button>
      </div>
    </div>
  );
}

// ─── Share Sheet ──────────────────────────────────────────────────────────────
function ShareSheet({ onClose }) {
  const ds = s => Math.max(14, Math.round(s*0.30));
  return (
    <div style={{ position:'absolute', inset:0, zIndex:200, background:'rgba(0,0,0,0.55)', display:'flex', flexDirection:'column', justifyContent:'flex-end' }}>
      <div style={{ background:'#1a1f2e', borderRadius:'16px 16px 0 0', border:`1px solid ${C.border}`, borderBottom:'none', overflow:'hidden' }}>
        <div style={{ display:'flex', justifyContent:'center', padding:'10px 0 0' }}>
          <div style={{ width:36, height:4, borderRadius:2, background:C.border }}/>
        </div>

        {/* Card preview */}
        <div style={{ margin:'14px 20px', borderRadius:8, overflow:'hidden' }}>
          <div style={{
            background:'#12141c',
            border:`1px solid #353a50`,
            borderRadius:8, padding:'20px', textAlign:'center',
          }}>
            <div style={{ fontFamily:PIXEL, fontSize:11, letterSpacing:2, lineHeight:1.5, marginBottom:6, color:'#fff' }}>
              OVER<span style={{ color:C.cyan }}>TONE</span>
            </div>
            <div style={{ fontFamily:MONO, fontSize:11, color:'rgba(255,255,255,0.55)', marginBottom:16, letterSpacing:2 }}>#142 · MAR 22</div>
            <div style={{ fontFamily:MONO, fontSize:44, color:C.cyan, fontWeight:500, lineHeight:1, marginBottom:16 }}>2,340</div>
            <div style={{ display:'flex', justifyContent:'center', gap:5, flexWrap:'wrap', marginBottom:14 }}>
              {MERGE_COUNTS.filter(m => m.count > 0).map(({ballIdx,count}) => {
                const b = BALLS[ballIdx], sz = ds(b.size);
                return (
                  <div key={ballIdx} style={{ display:'flex', flexDirection:'column', alignItems:'center', gap:3 }}>
                    <WaveformBall color={b.color} size={sz} freq={b.freq} pixelW={Math.max(2,Math.floor(b.pixelW*0.45))} pixelH={Math.max(2,Math.floor(b.pixelH*0.45))} waveType={b.waveType}/>
                    <span style={{ fontFamily:PIXEL, fontSize:5, color:b.color }}>×{count}</span>
                  </div>
                );
              })}
            </div>
            <div style={{ fontFamily:MONO, fontSize:10, color:'rgba(255,255,255,0.4)', letterSpacing:2 }}>overtone.app</div>
          </div>
        </div>

        {/* Share targets */}
        <div style={{ padding:'0 20px 12px' }}>
          <div style={{ display:'flex', justifyContent:'space-around', padding:'8px 0' }}>
            {[{icon:'💬',label:'Messages'},{icon:'𝕏',label:'X'},{icon:'📷',label:'Instagram'},{icon:'📋',label:'Copy'},{icon:'···',label:'More'}].map(app => (
              <div key={app.label} style={{ display:'flex', flexDirection:'column', alignItems:'center', gap:6 }}>
                <div style={{ width:52, height:52, borderRadius:14, background:C.surface, border:`1px solid ${C.border}`, display:'flex', alignItems:'center', justifyContent:'center', fontSize:app.icon==='···'?18:22, cursor:'pointer', color:'#fff', fontFamily:app.icon==='𝕏'?'serif':'inherit', fontWeight:app.icon==='𝕏'?700:400 }}>{app.icon}</div>
                <span style={{ fontFamily:MONO, fontSize:10, color:C.muted }}>{app.label}</span>
              </div>
            ))}
          </div>
        </div>

        <div style={{ padding:'0 20px 44px' }}>
          <button onClick={onClose} style={{ width:'100%', padding:'14px 0', background:C.surface, border:`1px solid ${C.border}`, borderRadius:8, fontFamily:MONO, fontSize:13, color:C.muted, cursor:'pointer' }}>Cancel</button>
        </div>
      </div>
    </div>
  );
}

// ─── App ──────────────────────────────────────────────────────────────────────
export default function App() {
  const [screen, setScreen] = useState('onboarding');

  const isGame = ['game','practice','result','share'].includes(screen);
  const showArena = isGame;

  return (
    <>
      <style>{CSS}</style>
      <div style={{ display:'flex', justifyContent:'center', alignItems:'center', minHeight:'100vh', background:'#080808' }}>
        <div style={{
          width:390, height:844, position:'relative',
          borderRadius:44, overflow:'hidden',
          border:'1px solid #1a1a28',
          boxShadow:`0 0 0 6px #0c0c12, 0 0 0 7px #181828, 0 0 40px rgba(77,217,192,0.07), 0 50px 100px rgba(0,0,0,0.9)`,
        }}>

          {/* Nav: top-level screens */}
          {screen==='onboarding'  && <OnboardingScreen onStart={() => setScreen('home-fresh')}/>}
          {screen==='home-fresh'  && <HomeScreenFresh  onPlay={() => setScreen('game')} onSettings={() => setScreen('settings')} onLeaderboard={() => setScreen('leaderboard')}/>}
          {screen==='home-played' && <HomeScreenPlayed onShare={() => setScreen('share')} onPractice={() => setScreen('practice')} onSettings={() => setScreen('settings')} onLeaderboard={() => setScreen('leaderboard')}/>}
          {screen==='settings'    && <SettingsScreen   onBack={() => setScreen('home-played')}/>}
          {screen==='leaderboard' && <LeaderboardScreen onBack={() => setScreen('home-played')}/>}
          {screen==='showcase'    && (
            <Shell>
              <div style={{ padding:'0 24px 20px' }}>
                <div style={{ fontFamily:PIXEL, fontSize:10, color:C.muted, letterSpacing:2 }}>ALL 11 BALLS</div>
              </div>
              <div style={{ flex:1, display:'flex', flexDirection:'column', justifyContent:'center', padding:'16px 24px', gap:20 }}>
                {[BALLS.slice(0,3),BALLS.slice(3,7),BALLS.slice(7)].map((row,ri) => (
                  <div key={ri} style={{ display:'flex', justifyContent:'center', alignItems:'flex-end', gap:ri===0?18:ri===1?16:14 }}>
                    {row.map(b => (
                      <div key={b.id} style={{ display:'flex', flexDirection:'column', alignItems:'center', gap:7 }}>
                        <WaveformBall {...b}/><span style={{ fontFamily:PIXEL, fontSize:7, color:C.muted }}>C{b.id}</span>
                      </div>
                    ))}
                  </div>
                ))}
              </div>
              <div style={{ padding:'0 24px 44px' }}>
                <button onClick={() => setScreen('onboarding')} style={{ width:'100%', padding:'16px 0', background:C.cyan, border:'none', borderRadius:4, fontFamily:PIXEL, fontSize:11, letterSpacing:2, color:C.bg, cursor:'pointer' }}>ONBOARDING →</button>
              </div>
            </Shell>
          )}

          {/* Game arena (always rendered beneath overlays when in game flow) */}
          {showArena && (
            <GameScreen
              onGameOver={() => setScreen('result')}
              onBack={() => setScreen('home-fresh')}
              practice={screen==='practice'}
            />
          )}

          {/* Result overlay */}
          {(screen==='result'||screen==='share') && <ResultOverlay onShare={() => setScreen('share')} onHome={() => setScreen('home-played')}/>}

          {/* Share sheet */}
          {screen==='share' && <ShareSheet onClose={() => setScreen('result')}/>}

          {/* Scanlines */}
          <div style={{ position:'absolute', inset:0, pointerEvents:'none', zIndex:300, borderRadius:44, background:`repeating-linear-gradient(to bottom, transparent 0px, transparent 3px, rgba(0,0,0,0.055) 3px, rgba(0,0,0,0.055) 4px)` }}/>
        </div>

        {/* External nav (design tool only) */}
        <div style={{ position:'fixed', bottom:24, left:'50%', transform:'translateX(-50%)', display:'flex', gap:6, flexWrap:'wrap', justifyContent:'center', maxWidth:500 }}>
          {[
            ['showcase',    '11 balls'],
            ['onboarding',  'onboarding'],
            ['home-fresh',  'home (new)'],
            ['home-played', 'home (played)'],
            ['game',        'game'],
            ['result',      'result'],
            ['share',       'share'],
            ['settings',    'settings'],
            ['leaderboard', 'leaderboard'],
          ].map(([s,label]) => (
            <button key={s} onClick={() => setScreen(s)} style={{
              padding:'6px 12px', borderRadius:4, cursor:'pointer', fontSize:11, fontFamily:MONO,
              background: screen===s ? C.cyan : '#1a1f2e',
              color:       screen===s ? C.bg   : C.muted,
              border:      screen===s ? 'none' : `1px solid ${C.border}`,
            }}>{label}</button>
          ))}
        </div>
      </div>
    </>
  );
}
