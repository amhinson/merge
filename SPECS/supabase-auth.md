# Supabase Auth — Full Implementation Spec

## Overview

Replace the current `device_uuid` identity system with Supabase Auth. Users start as anonymous auth users and can optionally link an Apple or Google account to persist their identity across devices/reinstalls.

---

## Auth Lifecycle

### 1. First Launch (Onboarding)

- **Do NOT create an anonymous user at app start.** This avoids orphaning anonymous users if someone signs into an existing account during onboarding.
- In the **top-right corner of the first onboarding screen**, show a subtle "Sign in" text button (DMMono, OC.muted). Tapping it opens the sign-in flow (Apple/Google). This is completely optional — most users will skip it.
- If the user signs in here, they authenticate directly with their Apple/Google account — no anonymous user is ever created.
- If the user skips sign-in, the **anonymous session is created when they tap "LET'S PLAY"** at the end of onboarding. This is the commitment point — they've chosen to play without an account.
- The session token is stored by the Supabase client SDK and persists across app restarts.

**Edge case — fresh install, signs into existing account:**
- No anonymous user exists yet (deferred), so the user signs in directly to their existing account. Their old scores, streak, and display name are all intact. No orphaned data.

**Edge case — played some games anonymously, then links account:**
- `linkIdentity()` upgrades the anonymous user in place. Same `user.id`, scores stay attached.

**Edge case — played anonymously, then signs into a DIFFERENT existing account:**
- This is a conflict: anonymous user has scores, existing account has scores.
- For v1: warn the user ("You have scores on this device that won't transfer. Continue?"). If they continue, sign out anonymous, sign in to existing account. Anonymous scores are orphaned.
- Future: offer to merge scores.

### 2. During Normal Use

- All API calls use the Supabase JWT from the auth session instead of (or alongside) the API key.
- The `user.id` from the Supabase session replaces `device_uuid` everywhere.
- Anonymous users function identically to linked users — they can play, submit scores, appear on leaderboards.

### 3. Account Linking Prompt (2-Day Streak)

- **Trigger:** After completing a scored game, if `current_streak == 2` AND the user is anonymous AND the prompt hasn't been shown before.
- **Location:** Home screen (HomePlayed), shown as a dismissable card/banner above the CTA buttons.
- **Content:**
  - Heading: "Keep your scores safe" (PressStart2P, small)
  - Body: "Connect an account so your scores and streak survive if you switch devices or reinstall." (DMMono, muted)
  - Privacy note: "We only store your player ID. No personal data." (DMMono, dim, smaller)
  - Primary button: "CONNECT ACCOUNT" (cyan primary)
  - Dismiss: "x" button or "Not now" ghost button
- **Behavior:**
  - Tapping "CONNECT ACCOUNT" opens the sign-in sheet (Apple/Google).
  - Tapping dismiss hides the card and sets `PlayerPrefs("link_prompt_dismissed", 1)` — never shown again.
  - On successful link, the anonymous user is upgraded via `supabase.auth.linkIdentity()`. The `user.id` stays the same.

### 4. Settings — Account Section

Add a new section to NewSettingsScreen between the controls and beta feedback blocks:

**If anonymous (not linked):**
```
ACCOUNT
┌─────────────────────────────────────┐
│  Connect account                  > │
│  We only store your player ID.      │
│  No personal data.                  │
└─────────────────────────────────────┘
```
- "Connect account" row with chevron, tapping opens sign-in sheet.
- Privacy note below in dim text.

**If linked:**
```
ACCOUNT
┌─────────────────────────────────────┐
│  Signed in with Apple             ✓ │
│  player_id@privaterelay.com         │
├─────────────────────────────────────┤
│  Sign out                           │
└─────────────────────────────────────┘
```
- Shows provider name + email (if available, otherwise just provider).
- Green checkmark.
- "Sign out" row — tapping shows a confirmation: "Sign out? You'll continue as an anonymous player. Your scores stay on the leaderboard."
- After sign out, creates a new anonymous session. Old scores remain under the old user ID.

---

## Sign-In Sheet

A modal overlay (similar to ShareSheet pattern) with:

```
┌──────────────────────────────────┐
│         Connect Account          │
│                                  │
│  ┌────────────────────────────┐  │
│  │  ◉  Continue with Apple   │  │
│  └────────────────────────────┘  │
│  ┌────────────────────────────┐  │
│  │  ◉  Continue with Google  │  │
│  └────────────────────────────┘  │
│                                  │
│          Cancel                  │
└──────────────────────────────────┘
```

- Apple button: white bg, black text, Apple logo (SF Symbol on iOS, text fallback on Android)
- Google button: white bg, black text, Google "G" (or text fallback)
- Cancel: ghost button, dismisses overlay
- On iOS: only show Apple Sign In (Google optional). On Android: only show Google Sign In (Apple not available).
- Actually, show both on both platforms — Supabase supports both everywhere. But Apple Sign In on Android requires extra setup, so for v1: **Apple on iOS, Google on Android**.

---

## Database Changes

### Schema Changes (`Supabase/schema.sql`)

```sql
-- Rename device_uuid to user_id across tables
-- (Since no real users, we can do a clean migration)

ALTER TABLE players RENAME COLUMN device_uuid TO user_id;
ALTER TABLE daily_scores RENAME COLUMN device_uuid TO user_id;

-- Update unique constraints
-- players: user_id is already PK
-- daily_scores: update unique constraint
ALTER TABLE daily_scores DROP CONSTRAINT daily_scores_device_uuid_game_date_key;
ALTER TABLE daily_scores ADD CONSTRAINT daily_scores_user_id_game_date_key UNIQUE (user_id, game_date);

-- Add provider info to players (optional, for settings display)
ALTER TABLE players ADD COLUMN auth_provider TEXT; -- 'anonymous', 'apple', 'google'
ALTER TABLE players ADD COLUMN auth_email TEXT;     -- only if user linked

-- Enable RLS with auth
ALTER TABLE players ENABLE ROW LEVEL SECURITY;
ALTER TABLE daily_scores ENABLE ROW LEVEL SECURITY;

-- RLS policies: users can only read/write their own data
-- Public read for leaderboard
CREATE POLICY "Public read scores" ON daily_scores FOR SELECT USING (true);
CREATE POLICY "Users insert own scores" ON daily_scores FOR INSERT WITH CHECK (auth.uid()::text = user_id);
CREATE POLICY "Users update own scores" ON daily_scores FOR UPDATE USING (auth.uid()::text = user_id);

CREATE POLICY "Public read players" ON players FOR SELECT USING (true);
CREATE POLICY "Users manage own player" ON players FOR ALL USING (auth.uid()::text = user_id);
```

### Edge Function Changes

All edge functions need dual auth support (transition period):

```typescript
// _shared/auth.ts — updated
export function getAuthenticatedUserId(req: Request): string | null {
  // 1. Try Supabase JWT (preferred)
  const authHeader = req.headers.get("Authorization");
  if (authHeader?.startsWith("Bearer ")) {
    const jwt = authHeader.slice(7);
    // Verify JWT and extract user.id
    // Return user.id
  }

  // 2. Fall back to API key + device_uuid in body (legacy)
  // Check apikey header, extract device_uuid from request body
  // Return device_uuid

  // 3. Neither — reject
  return null;
}
```

Functions to update (replace `device_uuid` references with `user_id`):
- `register-player` — upsert using `user_id` from auth
- `submit-score` — use `user_id` from auth
- `get-player-rank` — use `user_id` from auth
- `get-player-profile` — use `user_id` from auth
- `update-display-name` — use `user_id` from auth
- `get-leaderboard` — no auth needed (public read)
- `sync-streak` — use `user_id` from auth

---

## Client Changes

### New Files

| File | Purpose |
|------|---------|
| `Backend/AuthManager.cs` | Manages Supabase auth state, sign-in, link, sign-out |
| `UI/SignInSheet.cs` | Modal overlay for Apple/Google sign-in |
| `UI/LinkPromptCard.cs` | Dismissable card on home screen |

### AuthManager.cs

Singleton, initializes on app start (before any API calls).

```
AuthManager
  - IsAuthenticated: bool
  - IsAnonymous: bool
  - UserId: string (replaces PlayerIdentity.DeviceUUID)
  - AuthProvider: string ("anonymous", "apple", "google")
  - AuthEmail: string (nullable)

  - InitializeAsync() — called at app start
    1. Check for existing Supabase session (persisted in secure storage)
    2. If valid session exists, refresh token if needed, set UserId etc.
    3. If no session, do nothing yet (lazy — see EnsureAuthenticated)

  - CreateAnonymousSession() — called when user taps "LET'S PLAY" in onboarding
    1. Call signInAnonymously()
    2. Set UserId, IsAnonymous, etc.
    3. Proceed to home screen / first game

  - EnsureAuthenticated() — safety net, called before any API call
    1. If already authenticated, return immediately
    2. If no session (shouldn't happen, but defensive), call signInAnonymously()
    3. Set UserId, IsAnonymous, etc.

  - SignInWithApple() — triggers Apple Sign In flow
    1. If anonymous: links Apple identity to current anonymous user
    2. If not authenticated: signs in directly with Apple
    3. Updates IsAnonymous, AuthProvider, AuthEmail

  - SignInWithGoogle() — same pattern for Google

  - SignOut()
    1. Signs out of Supabase
    2. Creates new anonymous session
    3. Resets local state

  - GetAccessToken(): string — returns current JWT for API calls
```

### PlayerIdentity.cs Changes

- Replace `DeviceUUID` with `AuthManager.Instance.UserId`
- Remove `SystemInfo.deviceUniqueIdentifier` usage
- Keep backward compat: if AuthManager isn't ready, fall back to device UUID

### SupabaseClient.cs Changes

- Add JWT auth header to all requests: `Authorization: Bearer {token}`
- Keep API key header during transition period
- Replace `device_uuid` in request bodies with `user_id`

### GameSession.cs Changes

- `DeviceUUID` → read from `AuthManager.Instance.UserId`

### OnboardingScreen.cs Changes

- Add "Sign in" button in top-right of first screen
- Tapping opens SignInSheet overlay
- If signed in, continue onboarding normally

### HomePlayedScreen.cs Changes

- After `OnEnable`, check if link prompt should show:
  ```
  if (AuthManager.Instance.IsAnonymous
      && StreakManager.Instance.CurrentStreak >= 2
      && PlayerPrefs.GetInt("link_prompt_dismissed", 0) == 0)
  {
      ShowLinkPrompt();
  }
  ```

### NewSettingsScreen.cs Changes

- Add "ACCOUNT" section between controls and beta feedback
- Content depends on `AuthManager.Instance.IsAnonymous`

---

## Platform-Specific Setup

### iOS — Apple Sign In

- Enable "Sign in with Apple" capability in Xcode project
- Add `com.apple.developer.applesignin` entitlement
- Unity plugin: `com.unity.sign-in-with-apple` or use native iOS bridge
- Post-build script adds entitlement to Xcode project

### Android — Google Sign In

- Configure OAuth 2.0 in Google Cloud Console
- Add SHA-1 fingerprint to Firebase/Google project
- Unity plugin: Google Sign-In for Unity or native bridge
- Configure `google-services.json`

### Supabase Dashboard

- Enable Apple and Google providers in Authentication > Providers
- Configure OAuth credentials for each
- Set redirect URLs

---

## Migration Path

Since there are no real users yet:

1. **Drop and recreate tables** with `user_id` instead of `device_uuid`
2. Update all edge functions
3. Update schema.sql
4. Deploy to dev, test thoroughly
5. Deploy to prod

If we had real users, we'd need a migration that:
- Adds `user_id` column alongside `device_uuid`
- Backfills `user_id` from `device_uuid` for existing rows
- Gradually shifts to `user_id` as primary key

---

## Implementation Order

1. **Supabase dashboard**: Enable anonymous auth, Apple provider, Google provider
2. **Schema**: Update tables (user_id, auth columns, RLS)
3. **Edge functions**: Add JWT auth support alongside API key
4. **AuthManager.cs**: Core auth state management
5. **SupabaseClient.cs**: Add JWT headers to requests
6. **PlayerIdentity.cs / GameSession.cs**: Wire up new user ID source
7. **Test**: Anonymous flow works end-to-end (play, submit, leaderboard)
8. **SignInSheet.cs**: Build the sign-in overlay UI
9. **OnboardingScreen.cs**: Add subtle sign-in button
10. **Platform setup**: Apple Sign In (iOS), Google Sign In (Android)
11. **Test**: Sign-in and account linking works
12. **LinkPromptCard.cs**: Build the home screen prompt
13. **NewSettingsScreen.cs**: Add account section
14. **Test**: Full flow — anonymous → prompted → linked → settings shows linked

---

## Notes

- **Offline handling**: Anonymous auth creates a session locally. If offline at first launch, queue the auth call and retry when connected (similar to OfflineSyncQueue pattern).
- **Token refresh**: Supabase tokens expire (default 1 hour). AuthManager should refresh tokens in the background. The Supabase client SDK handles this automatically if configured.
- **Security**: Once JWT auth is fully rolled out, remove API key auth from edge functions. Add a TODO/reminder for this.
- **Testing**: In editor, use a test Supabase project. Anonymous auth works without any platform-specific setup. Apple/Google sign-in only works on device.
