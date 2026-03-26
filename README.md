# Murge

A daily merge puzzle game. Drop balls, merge matching tones, climb the leaderboard. New puzzle every day — everyone plays the same sequence.

**Website:** [murgegame.com](https://murgegame.com)

## Prerequisites

- **Unity 6** (6000.3.11f1) — [Unity Hub](https://unity.com/download)
- **Xcode** (latest) — for iOS builds
- **Supabase CLI** — `brew install supabase`
- **Netlify CLI** — `npm install -g netlify-cli` (for website deploys)
- **Node.js** — for Netlify CLI

## Getting Started

1. Clone the repo
2. Open the project in Unity Hub (select Unity 6000.3.11f1)
3. Wait for package resolution (GameAnalytics via OpenUPM, NativeShare via GitHub)
4. Open `Assets/Scenes/GameScene.unity`
5. Press Play

### First-time setup

- **GameAnalytics:** Go to `Window > GameAnalytics > Select Settings` and enter your game key + secret key. Events only fire on device, not in the editor.
- **Supabase:** Dev/prod credentials are in `SupabaseClient.cs`. The editor uses dev credentials automatically.

## Project Structure

```
Assets/
  Scripts/
    Core/           Game logic (GameManager, BallController, DropController, etc.)
    UI/             All screens (procedurally built at runtime, no prefabs)
    Backend/        Supabase client, leaderboard service, offline sync
    Data/           ScriptableObjects, design tokens
    Visual/         Ball rendering, waveforms, particles
    Audio/          Sound manager (procedural SFX)
    Editor/         Scene builder, build scripts, screenshot capture
  ScriptableObjects/  Ball tier configs (11 tiers), physics config
  Shaders/          WaveformBall shader
  Scenes/           GameScene.unity (the only scene)
Supabase/
  supabase/functions/   Edge functions (7 endpoints)
  deploy.sh             Deploy to dev or prod
web/
  index.html            Landing page
  privacy.html          Privacy policy
  deploy.sh             Deploy to Netlify
```

## Build & Deploy

### Run in Editor

Open `GameScene.unity` and press Play. The gameplay screen is generated at editor time — if you modify `GameSceneBuilder.cs`, run **MergeGame > Build Game Scene** from the menu, then save (Cmd+S).

### iOS

```bash
./build-ios.sh dev      # Development build → TestFlight
./build-ios.sh prod     # Production build → TestFlight
./build-ios.sh all      # Both sequentially
```

Build number auto-increments from `.build-number`. Override: `BUILD_NUMBER=42 ./build-ios.sh prod`.

Bundle IDs: `com.murge.game` (prod), `com.murge.game.dev` (dev).

### Android

```bash
./build-android.sh dev      # Development APK (for testing)
./build-android.sh prod     # Release AAB (for Google Play)
./build-android.sh all      # Both sequentially
```

**First-time keystore setup:**
```bash
./setup-keystore.sh         # Generates murge.keystore (back it up!)
```

Before prod builds, set passwords:
```bash
export MURGE_KEYSTORE_PASS=yourpass
export MURGE_KEY_ALIAS_PASS=yourpass
```

Output: `Build/Android/murge-dev.apk` or `Build/Android/murge-prod.aab`.

### Supabase Edge Functions

```bash
cd Supabase && ./deploy.sh dev     # Deploy to dev project
cd Supabase && ./deploy.sh prod    # Deploy to prod project
```

### Website

```bash
cd web && ./deploy.sh              # Production deploy to Netlify
cd web && ./deploy.sh preview      # Draft/preview deploy
```

Live at [murgegame.com](https://murgegame.com). Privacy policy at [murgegame.com/privacy](https://murgegame.com/privacy).

## Database Setup

The Supabase projects need these tables:

```sql
CREATE TABLE players (
    device_uuid TEXT PRIMARY KEY,
    display_name TEXT NOT NULL DEFAULT 'Player',
    created_at TIMESTAMPTZ DEFAULT now(),
    current_streak INTEGER DEFAULT 0,
    longest_streak INTEGER DEFAULT 0
);

CREATE TABLE daily_scores (
    id BIGSERIAL PRIMARY KEY,
    device_uuid TEXT REFERENCES players(device_uuid),
    game_date DATE NOT NULL,
    score INTEGER NOT NULL,
    day_number INTEGER DEFAULT 1,
    merge_counts INTEGER[],
    longest_chain INTEGER DEFAULT 0,
    submitted_at TIMESTAMPTZ DEFAULT now(),
    UNIQUE (device_uuid, game_date)
);

CREATE TABLE rate_limits (
    id BIGSERIAL PRIMARY KEY,
    device_uuid TEXT NOT NULL,
    function_name TEXT NOT NULL,
    created_at TIMESTAMPTZ DEFAULT now()
);
CREATE INDEX idx_rate_limits_lookup ON rate_limits (device_uuid, function_name, created_at);
```

## App Store Screenshots

Press **F12** during Play mode to capture a screenshot. Saves to `Screenshots/` in the project root with resolution and timestamp in the filename.

To get each required App Store size, change the Game view resolution before capturing:

| Device          | Resolution  |
|-----------------|-------------|
| iPhone 6.7"     | 1290 x 2796 |
| iPhone 6.5"     | 1242 x 2688 |
| iPhone 5.5"     | 1242 x 2208 |

Tips:
- Play a game until the board has a good spread of balls
- Press F12 to capture the gameplay screen
- Navigate to home/results/settings for other screenshots
- Screenshots are gitignored

## Game Mechanics

### Daily Puzzle
Every day generates a new deterministic ball sequence from the date (seeded xorshift128 RNG). All players worldwide get the same drops in the same order. One scored attempt per day; replays are practice mode.

### Scoring
Point values per tier are in `Assets/ScriptableObjects/BallData_Tier[1-11].asset`. Chain merges apply a combo multiplier: `1.0 + (chainLength - 1) * 0.5`, capped at 4x.

### Save/Resume
Game state saves to PlayerPrefs after each drop settles and when the app is backgrounded. Includes ball positions, score, sequence index, merge stats, shakes, and attempt type. Discarded on day rollover or game over.

### Offline Mode
The app detects connectivity via `NetworkMonitor` (system reachability check + Supabase HEAD ping). When offline:
- Startup skips network fetches (no loading stall)
- Leaderboard shows "You're offline"
- Games play normally; scores queue for submission when connectivity returns
- Failed registrations and score submissions persist in `OfflineSyncQueue`

## Backend

### Edge Functions

| Function              | Purpose                                     |
|-----------------------|---------------------------------------------|
| `register-player`     | Create/upsert player, generate default name |
| `submit-score`        | Submit daily score with validation          |
| `get-leaderboard`     | Ranked player list for a date               |
| `get-player-rank`     | Player rank + total players                 |
| `get-player-profile`  | Today's score, streaks, merge counts        |
| `update-display-name` | Validate, sanitize, profanity filter        |
| `sync-streak`         | Update streak counts                        |

### Validation (submit-score)
- Score: 0 to 99,999, integer
- game_date: within 1.5 days of server time
- longest_chain: 0 to 99
- Rate limited: 5 requests per device per 60s

### Default Player Names
Generated server-side from two word lists (adjective + animal/noun + number). Phish-themed. ~85 adjectives, ~85 nouns, 0-99 suffix. Examples: "Tweezy Possum42", "Cosmic Llama7".

## Analytics

GameAnalytics SDK (`com.gameanalytics.sdk` via OpenUPM). Wrapper: `MurgeAnalytics.cs`. All events fail silently. Events only send on device (not in editor).

Configure keys: `Window > GameAnalytics > Select Settings`.

| Event                     | When                                  |
|---------------------------|---------------------------------------|
| `puzzle:start`            | Game begins                           |
| `puzzle:score`            | Game over (value = final score)       |
| `puzzle:duration_seconds` | Game over (value = round time)        |
| `puzzle:highest_tier`     | Game over (value = max tier)          |
| `puzzle:total_merges`     | Game over (value = merge count)       |
| `puzzle:longest_chain`    | Game over (value = best chain)        |
| `social:share_card`       | Share button tapped                   |
| `engagement:streak`       | Streak updated (value = streak days)  |
| `engagement:name_changed` | Display name saved                    |
| `onboarding:complete`     | Onboarding finished                   |
| `gameplay:shake_used`     | Shake used (value = remaining shakes) |

## Game Center Achievements

| ID                     | Trigger                           |
|------------------------|-----------------------------------|
| `first_game_completed` | Complete any game                 |
| `reach_tier10`         | Create a tier 10 ball             |
| `reach_tier11`         | Create the largest ball (tier 11) |
| `seven_day_streak`     | Maintain a 7-day play streak      |
| `hidden_secret`        | Hidden                            |

Configured in `AchievementManager.cs`. Register matching IDs in App Store Connect.

## Environment Config

| Setting         | Dev                        | Prod                        |
|-----------------|----------------------------|-----------------------------|
| Bundle ID       | `com.murge.game.dev`       | `com.murge.game`            |
| Supabase URL    | `qbgrghcpvmsnoyzmglgv`    | `negfbluxywxsadggnwwd`      |
| Build type      | Development (debug, logs)  | Release (optimized)         |
| Supabase select | `#if UNITY_EDITOR`         | `#else` block               |

## Resetting Local State

To reset the app to first-launch state (useful for testing onboarding):

```csharp
// In Unity console or a test script:
PlayerPrefs.DeleteAll();
PlayerPrefs.Save();
```

Or delete the app from the device/simulator.

## Troubleshooting

- **"Package has invalid dependencies"** — GameAnalytics needs Google EDM4U. Check `Packages/manifest.json` has the git dependency for `com.google.external-dependency-manager`.
- **Balls don't render** — Run **MergeGame > Build Game Scene** and save. The shader name must match (`Murge/WaveformBall`).
- **Score submission fails** — Check Supabase dashboard logs. Ensure `daily_scores` table has the `longest_chain` column.
- **GameAnalytics events not sending** — Events only fire on device, not in editor. Check Xcode console for GA logs.
- **Offline mode stuck** — Turn WiFi back on. `NetworkMonitor` polls every 30s and auto-recovers.
