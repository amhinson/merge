# Overtone — Unity uGUI Implementation Spec

This document is the authoritative reference for implementing the Overtone UI in Unity using uGUI (Canvas / UnityEngine.UI / TextMeshPro). It covers the design system, Supabase API integration, all shared components, and every screen with full Canvas hierarchy and component settings.

**Visual reference:** Keep `overtone-design.jsx` open in a browser at all times. Every screen in this spec maps 1:1 to a screen in that file.

---

## Table of Contents

1. [Project Setup](#1-project-setup)
2. [Design System](#2-design-system)
3. [Supabase API Layer](#3-supabase-api-layer)
4. [Shared Components](#4-shared-components)
5. [Screen: Onboarding](#5-screen-onboarding)
6. [Screen: Home — Fresh (no score today)](#6-screen-home--fresh)
7. [Screen: Home — Played (score submitted today)](#7-screen-home--played)
8. [Screen: Game](#8-screen-game)
9. [Screen: Result Overlay](#9-screen-result-overlay)
10. [Screen: Share Sheet](#10-screen-share-sheet)
11. [Screen: Settings](#11-screen-settings)
12. [Screen: Leaderboard](#12-screen-leaderboard)
13. [Navigation / Screen Manager](#13-navigation--screen-manager)
14. [Animation Notes (from Onboarding)](#14-animation-notes)

---

## 1. Project Setup

### Canvas Settings
Every UI screen lives under a single **Screen Space — Overlay** Canvas.

```
Canvas
  Canvas Scaler:
    UI Scale Mode:          Scale With Screen Size
    Reference Resolution:   390 x 844
    Screen Match Mode:      Match Width Or Height
    Match:                  0.5
  Graphic Raycaster:        default
```

All RectTransform values in this document assume the 390×844 reference resolution.

### Sorting
Use a single Canvas with child Panel GameObjects per screen. Overlays (Result, Share Sheet) are children of the same Canvas with higher `Canvas.sortingOrder` or placed later in hierarchy.

### Safe Area
Wrap all screen content in a **SafeAreaPanel** script that reads `Screen.safeArea` and sets the RectTransform insets at runtime. All screens use a top padding of **32px** minimum (increases on notched phones).

### Fonts
Import two fonts via TextMeshPro Font Asset Creator:

| Font | TMP Asset Name | Use |
|------|---------------|-----|
| Press Start 2P (Google Fonts) | `PressStart2P SDF` | Labels, titles, pixel UI |
| DM Mono Medium (Google Fonts) | `DMMono-Medium SDF` | Numbers, names, body |

Both fonts must be added to the **TMP Settings** default font fallback list.

---

## 2. Design System

### Color Palette
Define all colors as a static `OvertoneColors` ScriptableObject or static class.

```csharp
public static class OC {
    // Backgrounds
    public static Color bg      = Hex("#0F1117");
    public static Color surface = Hex("#161B24");
    public static Color border  = Hex("#232838");

    // Accent colors (ball colors + UI accents)
    public static Color cyan    = Hex("#4DD9C0");
    public static Color pink    = Hex("#E8587A");
    public static Color amber   = Hex("#F0B429");
    public static Color lime    = Hex("#A3E635");
    public static Color violet  = Hex("#A78BFA");
    public static Color orange  = Hex("#FB923C");
    public static Color sky     = Hex("#38BDF8");
    public static Color rose    = Hex("#FB7185");

    // Text
    public static Color muted   = Hex("#FFFFFF", 0.22f);
    public static Color dim     = Hex("#FFFFFF", 0.10f);
    public static Color white   = Color.white;

    // Overlays
    public static Color overlayDark    = Hex("#08080E", 0.88f);
    public static Color shareCardBg    = Hex("#12141C");
    public static Color shareCardBorder= Hex("#353A50");

    private static Color Hex(string hex, float alpha = 1f) {
        ColorUtility.TryParseHtmlString(hex, out Color c);
        c.a = alpha;
        return c;
    }
}
```

### Spacing Constants
```csharp
public static class OS {
    public const float screenPadH = 24f;   // horizontal screen padding
    public const float screenPadV = 16f;   // vertical section padding
    public const float cardRadius = 4f;    // border radius (use Image with sprite)
    public const float borderWidth = 1f;   // outline thickness
    public const float safeAreaTop = 32f;  // minimum top safe area
}
```

### Font Sizes
```csharp
public static class OFont {
    // Press Start 2P sizes
    public const float title    = 24f;
    public const float heading  = 11f;
    public const float label    = 9f;
    public const float labelSm  = 8f;
    public const float labelXs  = 7f;
    public const float labelXxs = 6f;

    // DM Mono sizes
    public const float score    = 56f;
    public const float scoreGame= 28f;
    public const float bodyLg   = 14f;
    public const float body     = 13f;
    public const float bodySm   = 12f;
    public const float bodyXs   = 11f;
    public const float caption  = 10f;
}
```

### Rounded Rectangle Sprite
Create a **9-slice sprite** called `RoundedRect` (8px corner radius, white, 32×32px). Use this as the `Image.sprite` with `Image.type = Sliced` on all cards and buttons. Tint with `Image.color`.

---

## 3. Supabase API Layer

### Setup
- Use `UnityWebRequest` for all HTTP calls (no external packages required)
- Store `SUPABASE_URL` and `SUPABASE_ANON_KEY` in a `SupabaseConfig` ScriptableObject (exclude from version control)
- All writes go through Edge Functions using the anon key (Edge Functions use service role internally)
- All reads are direct REST calls using the anon key

```csharp
public static class SupabaseClient {
    private static string url    => SupabaseConfig.Instance.url;
    private static string anonKey => SupabaseConfig.Instance.anonKey;

    // Generic GET helper
    public static IEnumerator Get(string endpoint, Action<string> onSuccess, Action<string> onError) {
        using var req = UnityWebRequest.Get($"{url}/rest/v1/{endpoint}");
        req.SetRequestHeader("apikey", anonKey);
        req.SetRequestHeader("Authorization", $"Bearer {anonKey}");
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success) onSuccess(req.downloadHandler.text);
        else onError(req.error);
    }

    // Generic Edge Function POST helper
    public static IEnumerator EdgeFunction(string fnName, string body, Action<string> onSuccess, Action<string> onError) {
        using var req = new UnityWebRequest($"{url}/functions/v1/{fnName}", "POST");
        req.uploadHandler   = new UploadHandlerRaw(Encoding.UTF8.GetBytes(body));
        req.downloadHandler = new DownloadHandlerBuffer();
        req.SetRequestHeader("apikey", anonKey);
        req.SetRequestHeader("Authorization", $"Bearer {anonKey}");
        req.SetRequestHeader("Content-Type", "application/json");
        yield return req.SendWebRequest();
        if (req.result == UnityWebRequest.Result.Success) onSuccess(req.downloadHandler.text);
        else onError(req.error);
    }
}
```

### Data Models
```csharp
[System.Serializable]
public class Player {
    public string device_uuid;
    public string display_name;
    public string created_at;
    public int    current_streak;
    public int    longest_streak;
}

[System.Serializable]
public class DailyScore {
    public long   id;
    public string device_uuid;
    public string game_date;       // "YYYY-MM-DD"
    public int    score;
    public int    day_number;
    public int[]  largest_merges;  // array of ball IDs merged
    public string submitted_at;
}

[System.Serializable]
public class LeaderboardEntry {
    public int    rank;
    public string display_name;
    public int    score;
    public bool   is_current_player;
}

[System.Serializable]
public class PlayerRank {
    public int    rank;
    public int    total_players;
}
```

### Session State
```csharp
public static class GameSession {
    public static string  DeviceUUID     { get; private set; }
    public static Player  CurrentPlayer  { get; set; }
    public static int     TodayScore     { get; set; }   // 0 = not played
    public static bool    HasPlayedToday => TodayScore > 0;
    public static int     TodayDayNumber { get; set; }
    public static string  TodayDateStr   { get; set; }   // "YYYY-MM-DD"
    public static int[]   MergeCounts    { get; set; }   // index = ball ID 0-10, value = merge count

    public static void Init() {
        DeviceUUID = SystemInfo.deviceUniqueIdentifier;
        TodayDateStr = DateTime.UtcNow.ToString("yyyy-MM-dd");
    }
}
```

### API Calls by Screen

#### On App Launch (before any screen)
```
1. register-player  (idempotent — creates if not exists, returns existing if found)
   POST /functions/v1/register-player
   Body: { "device_uuid": "<uuid>" }
   Response: Player object
   → Store in GameSession.CurrentPlayer

2. get-player-profile  (fetch today score + streak)
   POST /functions/v1/get-player-profile
   Body: { "device_uuid": "<uuid>", "game_date": "YYYY-MM-DD" }
   Response: { player, today_score, day_number }
   → If today_score != null → GameSession.TodayScore = today_score
   → GameSession.TodayDayNumber = day_number
   → Navigate to Home (Fresh or Played based on HasPlayedToday)
```

#### submit-score (called when game ends)
```
POST /functions/v1/submit-score
Body: {
  "device_uuid": "<uuid>",
  "game_date": "YYYY-MM-DD",
  "score": 2340,
  "day_number": 142,
  "largest_merges": [1,2,3,3,4,4,5,5,5]
}
Response: { score, rank, total_players }
→ GameSession.TodayScore = score
→ Show Result Overlay with rank data
```

#### get-leaderboard
```
POST /functions/v1/get-leaderboard
Body: { "game_date": "YYYY-MM-DD", "device_uuid": "<uuid>" }
Response: [{ rank, display_name, score, is_current_player }, ...]
```

#### get-player-rank
```
POST /functions/v1/get-player-rank
Body: { "device_uuid": "<uuid>", "game_date": "YYYY-MM-DD" }
Response: { rank, total_players }
```

#### update-display-name
```
POST /functions/v1/update-display-name
Body: { "device_uuid": "<uuid>", "display_name": "newname" }
Response: { display_name }
```

#### sync-streak
```
POST /functions/v1/sync-streak
Body: { "device_uuid": "<uuid>" }
Response: { current_streak, longest_streak }
→ Call after submit-score, update GameSession.CurrentPlayer streak fields
```

---

## 4. Shared Components

### 4.1 WaveformBall

The WaveformBall is a custom Unity component drawn with a `MeshRenderer` on a `RawImage` using a `RenderTexture`, or alternatively a custom `Graphic` subclass. **Recommended approach: custom `MeshFilter + MeshRenderer` on a world-space object for the game arena; `RawImage + RenderTexture` for UI contexts.**

**Visual anatomy (from JSX):**
- Circle background: radial gradient from `color @ 45% opacity` (top-left) to `color @ 10% opacity` (bottom-right)
- Circle stroke: `color @ 85% opacity`, 1.5px
- Pixel waveform fill: columns of discrete pixel blocks, opacity fading downward
- Waveform edge: top row of each column, `color @ 95% opacity`
- Specular highlight: white ellipse, `13% opacity`, top-left quadrant
- Drop shadow: `drop-shadow(0 0 5px color@50%)`

**Ball Definitions (matches BALLS array in JSX):**

| ID | Color | Size (px) | pixelW | pixelH | WaveType |
|----|-------|-----------|--------|--------|----------|
| 1  | Cyan `#4DD9C0`   | 92 | 6 | 4 | Sine     |
| 2  | Pink `#E8587A`   | 76 | 5 | 4 | Sine     |
| 3  | Amber `#F0B429`  | 64 | 4 | 3 | Triangle |
| 4  | Violet `#A78BFA` | 54 | 4 | 3 | Sine     |
| 5  | Lime `#A3E635`   | 46 | 3 | 3 | Sawtooth |
| 6  | Sky `#38BDF8`    | 38 | 3 | 2 | Sine     |
| 7  | Orange `#FB923C` | 32 | 3 | 2 | Square   |
| 8  | Rose `#FB7185`   | 26 | 2 | 2 | Triangle |
| 9  | Cyan `#4DD9C0`   | 22 | 2 | 2 | Sine     |
| 10 | Amber `#F0B429`  | 18 | 2 | 2 | Sawtooth |
| 11 | Pink `#E8587A`   | 14 | 2 | 2 | Sine     |

**Waveform algorithm:**
```
cols = floor(size / pixelW)
amp  = radius * 0.28
cy   = radius * 0.72   ← this centers the wave vertically in the ball

waveY(col):
  t = col / cols
  sine:     cy + amp * sin(freq * 2π * t)
  square:   cy + amp * (sin(freq * 2π * t) >= 0 ? 1 : -1)
  sawtooth: cy + amp * (2 * ((freq * t) % 1) - 1)
  triangle: cy + amp * (2 * abs(2 * ((freq*t + 0.25) % 1) - 1) - 1)

For each column col (0..cols-1):
  x   = col * pixelW
  top = waveY(col)
  Draw edge pixel rect at (x+0.5, top+0.5), size (pixelW-1, pixelH-1), opacity 0.95
  For each row below top until bottom of circle:
    y       = top + row * pixelH
    opacity = row==0 ? 0.90 : max(0.08, 0.38 - row * 0.045)
    Draw fill pixel rect at (x+0.5, y+0.5), size (pixelW-1, pixelH-1), opacity
  All rects clipped to circle boundary.
```

### 4.2 StatCell Prefab
Used in the stats row on Home screens.

```
StatCell (RectTransform: flex child, height 52)
  ValueLabel  TMP  font=DMMono  size=22  color=varies  bold=true
  DividerLine Image height=1 color=OC.border (right edge, hidden on last cell)
  KeyLabel    TMP  font=PressStart2P  size=8  color=OC.muted  letterSpacing=0.5
```

### 4.3 LeaderboardRow Prefab
```
LeaderboardRow (height 38, horizontal layout)
  Background  Image  color=transparent (cyan tint when is_current_player)
  RankLabel   TMP    font=PressStart2P  size=7   width=32  color=OC.dim (gold for top3, cyan for you)
  NameLabel   TMP    font=DMMono        size=13  flex=1    color=OC.muted (cyan when you)
  ScoreLabel  TMP    font=DMMono        size=13  width=80  color=rgba(255,255,255,0.32) (cyan when you)
```

### 4.4 Toggle Prefab
```
Toggle (width 48, height 26)
  Track  Image  borderRadius=13  color=OC.cyan (on) or OC.border (off)
  Thumb  Image  circle  size=20  color=white  x=25 (on) or x=3 (off)
         shadow: 0 1 3 rgba(0,0,0,0.4)
```
Animate `Thumb.rectTransform.anchoredPosition.x` with `LeanTween` or `DOTween` on toggle.

### 4.5 Card / Panel Prefab
Dark surface panel used for leaderboards, stat blocks, settings fields.
```
Card (Image, sprite=RoundedRect, color=OC.surface, type=Sliced)
  Outline (Image, sprite=RoundedRect, color=OC.border, type=Sliced, raycastTarget=false)
          — position: stretch full, size delta -2 on all sides (1px inset border effect)
```

### 4.6 PrimaryButton Prefab
```
PrimaryButton (Image sprite=RoundedRect color=OC.cyan type=Sliced height=52)
  Label (TMP font=PressStart2P size=11 color=OC.bg letterSpacing=2)
```

### 4.7 GhostButton Prefab
```
GhostButton (Image sprite=RoundedRect color=OC.surface type=Sliced height=52
             Outline color=OC.border)
  Label (TMP font=PressStart2P size=9 color=OC.muted letterSpacing=1)
```

### 4.8 BackButton Prefab
```
BackButton (Image sprite=RoundedRect color=transparent Outline color=OC.border
            width=52 height=36)
  Label (TMP font=DMMono size=12 color=OC.muted text="←")
```

### 4.9 DividerLine Prefab
```
DividerLine (Image height=1 color=OC.border stretchHorizontal)
```

### 4.10 TopGradient
Every screen gets a subtle top accent. Place as first child of each screen panel:
```
TopGradient (Image height=200 stretchHorizontal anchored top
             color gradient: OC.cyan @ 7% opacity top → transparent bottom
             use UI vertex color gradient or shader)
```

---

## 5. Screen: Onboarding

**When shown:** First app launch (no `device_uuid` in PlayerPrefs). After register-player returns, navigate here.

**API calls:** `register-player` fires before this screen. No calls during onboarding itself.

**State:** `stepIndex` (0, 1, 2). Advance with NEXT button or auto-advance after 3 full demo cycles (implement later).

**Animation notes:** See [Section 14](#14-animation-notes) for full animation spec.

```
OnboardingScreen
├── TopGradient
├── LogoBlock (centered, paddingTop=48)
│     ├── TitleLabel  TMP  "OVER"  font=PressStart2P  size=20  color=white  letterSpacing=3
│     ├── TitleCyan   TMP  "TONE"  font=PressStart2P  size=20  color=OC.cyan letterSpacing=3
│     └── TaglineLabel TMP "A DAILY DROP"  font=DMMono  size=10  color=OC.muted  letterSpacing=5
│
├── DemoArena (width=220 height=260 centered flex=1)
│     ├── Background  Image  color=OC.surface
│     ├── GridOverlay Image  tiled dot-grid pattern  opacity=0.5  (see grid spec below)
│     ├── Step0Group  (visible when stepIndex==0)
│     │     ├── DropperBall    WaveformBall  id=9 (cyan, size=22)  pos=(110, 12)
│     │     ├── PlacedBall     WaveformBall  id=9 (cyan, size=22)  pos=(110, 227)
│     │     └── DropLine       Image  width=1  top=34 bottom=38  color gradient cyan→transparent
│     ├── Step1Group  (visible when stepIndex==1)
│     │     ├── Ball_A         WaveformBall  id=9  pos=(110, 227)
│     │     ├── Ball_B         WaveformBall  id=9  pos=(110, 205)
│     │     └── MergeRing      Image  circle outline  size=40  color=OC.cyan  opacity=0.5
│     └── Step2Group  (visible when stepIndex==2)
│           ├── NewDropper     WaveformBall  id=8 (rose, size=26)  pos=(110, 12)
│           ├── MergedBall     WaveformBall  id=8  pos=(110, 222)
│           └── ScorePop       TMP  "+ 100"  font=PressStart2P  size=7  color=OC.amber  pos above MergedBall
│
├── StepIndicators (horizontal, centered, gap=6, bottom of arena area)
│     └── [3x] Dot  Image  borderRadius=3
│                   active: width=20 height=6 color=OC.cyan
│                   inactive: width=6 height=6 color=OC.border
│
├── StepLabel (textAlign=center padding=0 24 16)
│     ├── HeadingLabel  TMP  font=PressStart2P  size=12  color=white  letterSpacing=2
│     │                 step0="DROP" step1="MERGE" step2="SCORE"
│     └── SubLabel      TMP  font=DMMono  size=12  color=OC.muted
│                       step0="drop matching balls"
│                       step1="same size = they combine"
│                       step2="bigger merges = more points"
│
└── ButtonRow (padding=0 24 48 gap=8 horizontal)
      ├── NextButton    PrimaryButton   text="NEXT →"    (hidden on step 2)
      ├── SkipButton    GhostButton     text="SKIP"  width=80  (hidden on step 2)
      └── StartButton   PrimaryButton   text="LET'S PLAY"  (visible only on step 2)
            → onClick: navigate to HomeScreen (Fresh)
```

**Grid overlay spec:**
Create a `GridOverlayMaterial` using UI shader with tiling. Tile size 28×28, line color `OC.border`, line width 1px. Or bake as a tileable texture.

---

## 6. Screen: Home — Fresh

**When shown:** `GameSession.HasPlayedToday == false`

**API calls on show:**
```
get-leaderboard → populate Top3List + (no YourRank row since not played)
```

```
HomeScreenFresh
├── TopGradient
├── SettingsButton  BackButton-style  icon="⚙"  anchored top-right  margin=24
│     → onClick: navigate to Settings
│
├── LogoBlock (paddingTop=0 paddingLeft=24)
│     ├── TitleLabel  TMP  "OVER"  font=PressStart2P  size=24  color=white  letterSpacing=2
│     ├── TitleCyan   TMP  "TONE"  font=PressStart2P  size=24  color=OC.cyan letterSpacing=2
│     └── TaglineLabel TMP "A DAILY DROP"  font=DMMono  size=11  color=OC.muted  letterSpacing=5
│
├── BallCluster (flex=1 centered horizontal gap=24)
│     ├── WaveformBall  id=2  (pink, 76px)
│     ├── WaveformBall  id=1  (cyan, 92px)   ← center, largest
│     └── WaveformBall  id=3  (amber, 64px)
│
├── PuzzleRow (horizontal gap=14 padding=0 24 18)
│     ├── PuzzleNumber  TMP  "#142"  font=PressStart2P  size=10  color=OC.cyan  letterSpacing=1
│     │                 → value: "#\(GameSession.TodayDayNumber)"
│     ├── DividerLine   flex=1
│     └── DateLabel     TMP  "MAR 22"  font=DMMono  size=11  color=OC.muted  letterSpacing=1
│
├── DividerLine  (full width, borderTop)
│
├── LeaderboardCard  Card  margin=0 24 0
│     ├── CardHeader (horizontal padding=7 10 borderBottom)
│     │     ├── HeaderLabel  TMP  "TODAY'S TOP"  font=PressStart2P  size=7  color=OC.muted
│     │     └── AllButton    TMP button  "ALL →"  font=PressStart2P  size=7  color=OC.cyan
│     │           → onClick: navigate to Leaderboard
│     ├── [3x] LeaderboardRow  (top 3, medal emoji for rank 1/2/3)
│     │         populated from get-leaderboard response, top 3 only
│     │         no YourRank row on fresh home
│     └── LoadingIndicator  (shown while fetching)
│
└── CTABlock (padding=16 24 44)
      ├── PlayButton    PrimaryButton  text="PLAY"
      │     → onClick: navigate to Game screen (scored mode)
      └── HintLabel     TMP  "1 scored play per day"  font=DMMono  size=10  color=OC.dim  letterSpacing=1
                        textAlign=center  marginTop=10
```

---

## 7. Screen: Home — Played

**When shown:** `GameSession.HasPlayedToday == true`

**API calls on show:**
```
get-leaderboard       → populate LeaderboardCard (top 3 + your rank row)
get-player-rank       → populate YourRankRow rank + total_players (for display)
```

Identical structure to Home Fresh with these differences:

```
HomeScreenPlayed
├── [same as Fresh until BallCluster]
│
├── [same BallCluster]
├── [same PuzzleRow]
│
├── StatsRow (horizontal borderTop borderBottom)
│     ├── StatCell  label="TODAY"  value=GameSession.TodayScore.ToString("N0")  color=white
│     └── StatCell  label="BEST"   value=personalBest.ToString("N0")  color=OC.cyan  last=true
│           personalBest = max of all-time scores for this player (fetch from get-player-profile)
│
├── LeaderboardCard  (same as Fresh but WITH YourRank row)
│     ├── CardHeader  (same, with ALL → button)
│     ├── [3x] LeaderboardRow  (top 3)
│     └── YourRankRow  LeaderboardRow  background=OC.cyan@10%  border=OC.cyan@28%
│                       rank="#20"  name="YOU" (or GameSession.CurrentPlayer.display_name)
│                       score=GameSession.TodayScore
│
└── CTABlock (padding=12 24 44)
      ├── ScoredBadge (horizontal centered gap=10 border=OC.border borderRadius=4 padding=11 0 marginBottom=10)
      │     ├── LockIcon   TMP  "🔒"  size=13
      │     ├── ScoredLabel TMP "SCORED"  font=PressStart2P  size=10  letterSpacing=2  color=OC.muted
      │     └── ScoreValue  TMP  "2,340"  font=DMMono  size=14  color=OC.cyan
      │                     → value: GameSession.TodayScore.ToString("N0")
      ├── ActionRow (horizontal gap=8)
      │     ├── ShareButton   PrimaryButton  text="SHARE"   flex=1
      │     │     → onClick: navigate to Share Sheet (skip Result Overlay, already seen)
      │     └── PlayAgainButton GhostButton  text="PLAY AGAIN"  flex=1  fontSize=9
      │           → onClick: navigate to Game screen (practice mode, sets GameSession.IsPractice=true)
      └── HintLabel  TMP  "only first score of the day is counted"
                     font=DMMono  size=10  color=OC.dim  textAlign=center  marginTop=8
```

---

## 8. Screen: Game

**Modes:** Scored (`GameSession.IsPractice == false`) and Practice (`IsPractice == true`).

**API calls:**
```
On game end (scored mode only):
  submit-score  → returns rank + total_players
  sync-streak   → update streak display
  → Navigate to Result Overlay
```

```
GameScreen
├── TopGradient
├── HeaderBar (horizontal gap=10 padding=0 16 8 paddingTop=safeAreaTop+16)
│     ├── BackButton  → onClick: navigate to Home
│     ├── ScoreBlock (flex=1)
│     │     ├── ScoreLabel  TMP  "SCORE"  font=PressStart2P  size=7  color=OC.muted  letterSpacing=2
│     │     └── ScoreValue  TMP  "0"      font=DMMono         size=28  color=OC.cyan  fontWeight=500
│     │                     → updated in real-time as merges occur
│     └── NextBallCard  Card  padding=5 8  gap=3  alignItems=center  flexShrink=0
│           ├── NextLabel  TMP  "NEXT"  font=PressStart2P  size=6  color=OC.dim  letterSpacing=1
│           └── NextBallDisplay  WaveformBall  (size=ball.size for whatever is next in sequence)
│
├── Arena (flex=1 margin=4 16 16 position=relative overflow=hidden)
│     ├── ArenaBackground  Image  color=OC.surface
│     ├── ArenaOutline     Image  sprite=RoundedRect  color=OC.border  borderWidth=1
│     ├── GridOverlay      (same tile grid as onboarding, opacity=0.55)
│     ├── DangerLine       Image  height=1  color=OC.pink@28%  positionY=fromTop:70
│     │     └── DangerLabel  TMP  "DANGER"  font=PressStart2P  size=7  color=OC.pink@55%
│     │                      anchored right, offsetY=-13
│     │
│     ├── PracticeBanner   (visible only in practice mode, auto-hide after 4s)
│     │     Image  background=OC.amber@18%  border=OC.amber@40%  borderRadius=3
│     │     TMP  "only first score of the day is counted"
│     │          font=PressStart2P  size=7  color=OC.amber  letterSpacing=1
│     │     Position: centered horizontally, top=10
│     │     Animation: CSS equivalent of bannerFadeOut keyframe
│     │       0%–55%: alpha=1, 55%–100%: alpha lerps to 0, total duration=4s
│     │
│     ├── [Placed balls]   WaveformBall instances, managed by physics engine
│     │                    positions set by Rigidbody2D + CircleCollider2D
│     │
│     └── ActiveDropper    WaveformBall  (current ball being aimed)
│                          positioned at finger X, fixed near top of arena
│                          dropped on tap/release
│
└── [No footer bar — arena takes all remaining space]
```

**Physics setup (for arena):**
- Arena is a 2D physics scene, camera orthographic
- Each ball: `Rigidbody2D` (Dynamic) + `CircleCollider2D` (radius = ball.size/2 in world units)
- Arena walls: `EdgeCollider2D` on left, right, bottom
- Danger line at `y = arenaHeight - 70px` (in world units)
- If any resting ball's top edge exceeds danger line for > 2 seconds: game over → `submit-score`
- On merge: destroy both balls, spawn next size up at collision midpoint with brief scale-in animation

---

## 9. Screen: Result Overlay

**When shown:** After `submit-score` returns. Overlaid on top of the frozen game arena.

**API calls on show:** None — data comes from `submit-score` response already in memory.

```
ResultOverlay (position=absolute inset=0 zIndex=100)
├── Backdrop  Image  color=OC.bg@88%  (backdropFilter blur=2, approximate with blur shader or just alpha)
│
├── ContentPanel (centered vertically, padding=0 28)
│     ├── ScoreBlock (textAlign=center marginBottom=8)
│     │     ├── FinalScoreLabel  TMP  "FINAL SCORE"  font=PressStart2P  size=8  color=OC.muted  letterSpacing=3
│     │     └── ScoreValue       TMP  "2,340"        font=DMMono         size=56  color=OC.cyan  fontWeight=500
│     │                          → GameSession.TodayScore.ToString("N0")
│     │
│     ├── RankBadge (horizontal gap=8 marginBottom=24 padding=7 16
│     │             background=OC.cyan@12  border=OC.cyan@28  borderRadius=4)
│     │     ├── RankLabel  TMP  "#20"  font=PressStart2P  size=8  color=OC.cyan  letterSpacing=1
│     │     │              → "#\(rankResult.rank)"
│     │     └── RankSubLabel TMP "of 847 players today"  font=DMMono  size=12  color=OC.muted
│     │                      → "of \(rankResult.total_players) players today"
│     │
│     ├── MergeGrid  Card  padding=14 12  marginBottom=24  width=stretch
│     │     ├── MergesLabel  TMP  "MERGES"  font=PressStart2P  size=7  color=OC.dim  letterSpacing=1  marginBottom=12
│     │     └── BallGrid  (wrapping flow layout, gap=10 6)
│     │           └── [11x] MergeCellItem
│     │                       ├── BallDisplay  WaveformBall  size=floor(ball.size * 0.38)  min=16
│     │                       │                pixelW/H also scaled by 0.5
│     │                       └── CountLabel   TMP  "×3" or "—"
│     │                                        font=PressStart2P  size=6
│     │                                        color=ball.color (if count>0) or OC.dim
│     │                       opacity=0.25 if count==0
│     │                       → MergeCounts from GameSession.MergeCounts[]
│     │
│     └── ButtonRow (horizontal gap=10 width=stretch)
│           ├── DoneButton   GhostButton  text="DONE"  flex=1  fontSize=9
│           │     → onClick: navigate to Home (Played)
│           └── ShareButton  PrimaryButton  text="SHARE"  flex=2
│                 → onClick: show Share Sheet overlay
```

---

## 10. Screen: Share Sheet

**When shown:** Slides up from bottom over Result Overlay (or Home Played). Separate Canvas layer or high sortingOrder.

**API calls:** None. Share image is generated from the ShareCard via `ScreenCapture.CaptureScreenshotAsTexture` cropped to the card bounds, then passed to `NativeShare` plugin or iOS/Android native share APIs.

```
ShareSheet (position=absolute inset=0 zIndex=200)
├── Scrim  Image  color=black@55%  fullscreen
│     → onClick: dismiss (same as Cancel)
│
└── SheetPanel (anchored bottom, width=full, background=#1A1F2E
               borderRadius top corners = 16px, border=OC.border top+sides only)
      ├── DragHandle  Image  width=36 height=4 borderRadius=2 color=OC.border  centered  marginTop=10
      │
      ├── CardPreview  margin=14 20  borderRadius=8  overflow=hidden
      │     └── ShareCard  (this is what gets screenshotted for sharing)
      │           background=#12141C  border=#353A50  borderRadius=8  padding=20  textAlign=center
      │           ├── BrandingLine  TMP  "OVERTONE"  font=PressStart2P  size=11  color=white  letterSpacing=2
      │           │                 ("OVER" white, "TONE" OC.cyan — use two TMP components side by side)
      │           ├── DateLine      TMP  "#142 · MAR 22"  font=DMMono  size=11  color=white@55%  letterSpacing=2
      │           │                 marginBottom=16
      │           │                 → "#\(day_number) · \(formatted_date)"
      │           ├── ScoreLine     TMP  "2,340"  font=DMMono  size=44  color=OC.cyan  fontWeight=500
      │           │                 marginBottom=16
      │           │                 → GameSession.TodayScore.ToString("N0")
      │           ├── MiniMergeGrid  (same as Result Overlay but filter count>0 only, smaller balls)
      │           │                  ball size = floor(ball.size * 0.30) min=14
      │           │                  horizontal wrapping  gap=5  centered  marginBottom=14
      │           └── FooterLine    TMP  "overtone.app"  font=DMMono  size=10  color=white@40%  letterSpacing=2
      │
      ├── ShareTargetRow (horizontal space-around padding=8 20 12)
      │     └── [5x] ShareTargetItem (vertical center gap=6)
      │                 ├── IconButton  Image  size=52x52  borderRadius=14
      │                 │              background=OC.surface  border=OC.border
      │                 │              TMP icon centered (emoji or icon)
      │                 └── AppLabel   TMP  font=DMMono  size=10  color=OC.muted
      │           Targets: Messages 💬, X 𝕏, Instagram 📷, Copy 📋, More ···
      │           → Each calls the appropriate share action on click:
      │             Copy: save screenshotted ShareCard texture to device gallery
      │             Others: invoke platform native share sheet with image
      │
      └── CancelButton (margin=0 20 44)
            GhostButton  text="Cancel"  borderRadius=8  font=DMMono  size=13
            → onClick: dismiss ShareSheet, return to Result Overlay
```

**Native Share implementation:**
Use `NativeShare` (open source Unity plugin) or implement `[DllImport]` calls for iOS `UIActivityViewController` and Android `Intent.ACTION_SEND`. Pass the screenshotted `Texture2D` of the ShareCard.

---

## 11. Screen: Settings

**API calls:**
```
On SAVE tap:
  update-display-name  → POST new display_name
  → show brief success indicator, navigate back
```

```
SettingsScreen
├── TopGradient
├── HeaderRow (horizontal gap=14 padding=0 24 24)
│     ├── BackButton  → onClick: navigate back (discard changes if not saved)
│     └── PageTitle  TMP  "SETTINGS"  font=PressStart2P  size=11  letterSpacing=2  color=white
│
├── ContentArea (flex=1 padding=0 24 scrollable)
│     ├── SectionLabel  TMP  "USERNAME"  font=PressStart2P  size=7  color=OC.dim  letterSpacing=1  marginBottom=8
│     ├── UsernameField  Card  height=52  horizontal  padding=0 14  marginBottom=24
│     │     ├── AtSign      TMP  "@"  font=DMMono  size=14  color=OC.cyan  marginRight=8
│     │     ├── InputField  TMP InputField  font=DMMono  size=14  color=white  flex=1
│     │     │               → pre-populated with GameSession.CurrentPlayer.display_name
│     │     │               maxCharacters=16  contentType=Alphanumeric
│     │     │               caretColor=OC.cyan  selectionColor=OC.cyan@30%
│     │     └── CharCount   TMP  "9/16"  font=PressStart2P  size=7  color=OC.dim
│     │                     → "\(inputField.text.Length)/16"  updated on every keystroke
│     │
│     ├── SectionLabel  TMP  "CONTROLS"  font=PressStart2P  size=7  color=OC.dim  letterSpacing=1  marginBottom=8
│     ├── HapticRow  Card  horizontal  padding=14  justifyContent=space-between  marginBottom=24
│     │     ├── HapticTextBlock (vertical gap=3)
│     │     │     ├── HapticTitle  TMP  "Haptic feedback"  font=DMMono  size=14  color=white
│     │     │     └── HapticSub    TMP  "Vibrate on merge"  font=DMMono  size=11  color=OC.muted
│     │     └── HapticToggle  Toggle prefab
│     │           → state stored in PlayerPrefs key "haptic_enabled" (bool, default true)
│     │           → onChange: PlayerPrefs.SetInt("haptic_enabled", value ? 1 : 0)
│     │
│     └── ComingSoonCard  (dashed border OC.border, borderRadius=4, padding=14, textAlign=center)
│           TMP  "MORE SETTINGS COMING SOON"  font=PressStart2P  size=7  color=OC.dim  letterSpacing=1  lineHeight=2
│
└── SaveButton  PrimaryButton  text="SAVE"  margin=24 24 44  width=stretch
      → onClick: call update-display-name, then navigate back
```

---

## 12. Screen: Leaderboard

**API calls on show:**
```
get-leaderboard  → for currently selected day (default: today)
→ Re-fetch when day changes
```

**State:** `selectedDayNumber` (int), `selectedDateStr` (string). Default = `GameSession.TodayDayNumber`.

```
LeaderboardScreen
├── TopGradient
├── HeaderRow (horizontal gap=10 padding=0 24 12)
│     ├── BackButton  → onClick: navigate back
│     └── DayNavCard  Card  flex=1  horizontal  padding=8 14  gap=12  alignItems=center
│           ├── PrevButton  TMP  "‹"  font=DMMono  size=16  color=OC.muted (dim if at oldest day)
│           │     → onClick: selectedDayNumber -= 1, re-fetch
│           ├── DayInfo (flex=1 textAlign=center)
│           │     ├── DayNumber  TMP  "#142"  font=PressStart2P  size=9  color=OC.cyan  letterSpacing=1
│           │     └── DayDate    TMP  "MAR 22"  font=DMMono  size=11  color=OC.muted  marginTop=2
│           └── NextButton  TMP  "›"  font=DMMono  size=16  color=OC.muted (dim if at today)
│                 → onClick: selectedDayNumber += 1, re-fetch (disabled at today)
│
├── LoadingSpinner  (visible while fetching)
│
└── ScoreList  ScrollRect  flex=1  padding=0 24 32
      └── [N×] LeaderboardRow  prefab
                → rank: medal emoji for 1/2/3, "#N" for others
                → highlight row where is_current_player==true
                   background=OC.cyan@10%  border=OC.cyan@28%
                   text colors all OC.cyan
```

---

## 13. Navigation / Screen Manager

Use a single `ScreenManager` MonoBehaviour that holds references to all screen GameObjects. Activate/deactivate as needed. For overlays (Result, ShareSheet), enable on top of the existing active screen.

```csharp
public enum Screen {
    Onboarding, HomeFresh, HomePlayed,
    Game, ResultOverlay, ShareSheet,
    Settings, Leaderboard
}

public class ScreenManager : MonoBehaviour {
    public static ScreenManager Instance;

    // Assign all screen root GameObjects in Inspector
    [SerializeField] GameObject onboardingScreen;
    [SerializeField] GameObject homeFreshScreen;
    [SerializeField] GameObject homePlayedScreen;
    [SerializeField] GameObject gameScreen;
    [SerializeField] GameObject resultOverlay;
    [SerializeField] GameObject shareSheet;
    [SerializeField] GameObject settingsScreen;
    [SerializeField] GameObject leaderboardScreen;

    private Screen currentScreen;

    public void NavigateTo(Screen screen) {
        // Deactivate all base screens (not overlays)
        // Activate target
        // For overlays, keep base screen active
        currentScreen = screen;
    }
}
```

**Navigation flow:**
```
App Launch
  └── register-player + get-player-profile
        ├── first launch → Onboarding → HomeFresh
        └── returning    → HomeFresh (no score) or HomePlayed (has score)

HomeFresh  → [PLAY]        → Game (scored)
           → [ALL →]       → Leaderboard
           → [⚙]           → Settings

HomePlayed → [SHARE]       → ShareSheet (over HomePlayed)
           → [PLAY AGAIN]  → Game (practice)
           → [ALL →]       → Leaderboard
           → [⚙]           → Settings

Game       → [game ends]   → ResultOverlay (over Game)
           → [←]           → HomeFresh / HomePlayed

ResultOverlay → [SHARE]    → ShareSheet (over ResultOverlay)
              → [DONE]     → HomePlayed

ShareSheet  → [Cancel]     → dismiss (back to whatever is behind)

Settings   → [SAVE / ←]   → back to previous Home screen
Leaderboard → [←]         → back to previous Home screen
```

---

## 14. Animation Notes

These are the animations for the Onboarding screen demo. Implement using **DOTween** (preferred) or Unity's built-in `Coroutine` + `Lerp`.

### Step 0 → Step 1 (DROP)
1. `DropperBall` starts at `y=12`, bobbing gently: `DOTween.Sequence` with `DOMoveY(18, 0.9f).SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo)` — starts as soon as step 0 is shown.
2. On transition to Step 1: stop bobbing, then `DOMoveY(targetY, 0.6f).SetEase(Ease.InQuad)` where `targetY` is 5px above the placed ball. Duration: 600ms.
3. On arrival: immediately switch to Step 1 group.

### Step 1 (MERGE flash)
1. Step 1 group appears: both balls visible at collision point.
2. `MergeRing` scale-out: `DOScale(2.0f, 0.3f).SetEase(Ease.OutQuad)` + `DOFade(0f, 0.3f)` simultaneously.
3. Both balls shrink: `DOScale(0f, 0.12f).SetEase(Ease.InQuad)`.
4. After 150ms: switch to Step 2 group.

### Step 2 (MERGED)
1. `MergedBall` appears at scale 0, spring in: `DOScale(1.15f, 0.2f).SetEase(Ease.OutQuad)` then `DOScale(1.0f, 0.1f).SetEase(Ease.InQuad)`.
2. `ScorePop` label starts at `alpha=0 y=0`, animates to `alpha=1 y=-16` over 300ms, then fades out over 300ms.
3. Particle burst (optional): 6–8 small square `Image` objects, each `DOMoveY` and `DOMoveX` outward from center, `DOFade(0)` over 250ms, then destroy.
4. After 1.2s pause: new dropper ball appears at top and begins bobbing. Step 2 loops indefinitely until user taps NEXT or LET'S PLAY.

### Practice Banner (Game Screen)
DOTween sequence:
```csharp
DOTween.Sequence()
  .AppendInterval(2.2f)        // hold visible
  .Append(banner.DOFade(0f, 1.8f).SetEase(Ease.InQuad))
  .OnComplete(() => banner.gameObject.SetActive(false));
```

### Score Counter (Game Screen)
When score increases, animate the numeric value with a count-up tween:
```csharp
DOTween.To(() => displayedScore, x => {
    displayedScore = x;
    scoreLabel.text = displayedScore.ToString("N0");
}, targetScore, 0.4f).SetEase(Ease.OutQuad);
```

### Result Overlay entrance
Fade in from `alpha=0` over 300ms. Optionally add `DOScale(0.96f → 1.0f, 0.3f).SetEase(Ease.OutQuad)` on the ContentPanel.

### Share Sheet entrance
Slide up from `y = -sheetHeight` to `y = 0` over 350ms `Ease.OutCubic`.
Dismiss: slide back down, 250ms `Ease.InCubic`.

---

## Appendix: JSX → Unity Quick Reference

| JSX concept | Unity equivalent |
|---|---|
| `flex: 1` | `LayoutElement.flexibleWidth = 1` |
| `display: flex` | `HorizontalLayoutGroup` or `VerticalLayoutGroup` |
| `gap: 8` | `spacing = 8` on Layout Group |
| `padding: 14 24` | `padding` on Layout Group |
| `border-radius` | 9-slice `RoundedRect` sprite |
| `background: rgba(x, y, z, a)` | `Image.color` with alpha |
| `border: 1px solid` | Child `Image` component, slightly smaller, placed behind (or outline shader) |
| `overflow: hidden` | `Mask` component or `RectMask2D` |
| `position: absolute` | `RectTransform` with manual anchors, outside Layout Group |
| `z-index` | Canvas `sortingOrder` or hierarchy order |
| `font-size: 11` | `TMP fontSize = 11` |
| `letter-spacing: 2` | `TMP characterSpacing = 2` |
| `filter: drop-shadow` | TMP `fontMaterial` shadow settings or `Shadow` component |
| `backdrop-filter: blur` | Camera stacking with blur post-process, or use darkened overlay |
| `opacity: 0.22` | `CanvasGroup.alpha = 0.22` or `Color.a` |
