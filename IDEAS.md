## NEXT UP

Reframe "Practice".
Note that it still counts towards your Personal Best.
Keep first score for leaderboard.
Add quip for the case where practice score is more than first score.
Explain briefly how the leaderboard mechanic works.
Results screen will need updates to make all this clearer. Make sure it shows the data from the game just played, not first score.

In-game menu to view leaderboard, see ball sizes, show personal best, and quit the game.

Add BETA? with click modal that things might change. Feedback welcome. Add some way to send feedback.

Use supabase anonymous auth. Optional sign in/up when app starts, but make it discrete.
Prompt after 2 day streak to connect account so you don't lose scores.
Also have a place in settings to connect & manage this.
Make big note about how it is only for convenience to persist scores. No personal identifiable data besides ID (?) is stored on our end.

## EVENTUALLY

Would be cool to have a simple list of past scores and ranking per day (might need to store the rank somehow?). Related to the Streak.
Note any top 3 finished on profile.

Merge reactions are still a bit weird when space is tight, especially with large balls. It jumps immediately.

Ball design doesn't inspire me. Waves seem stale. Colors a bit boring?

Wavelength react somehow to nearby?

Look into better logo.

MAYBE

NBA Jam style "He's on fire!" type comments when something big happens.

Recap push notification the next day. "You played 2nd out of 98 people!" "You were in the top 75% of all players yesterday"
"Well, someone had to get last, right?"
Configure recap update time. Make it seem informative and helpful, not pushy to play the next day.
Use AI so it's unique each day.
Can group segments. Top 10 get their own. Then remaining top 50%, then lower 40% and 10%, then last gets something unique and funny-ish.

Round 1: Home (played state)

Best screenshot — shows score, streak, leaderboard, merge ball.

1. MergeGame > Screenshot Setup > Freeze Merge Ball (Tier 10)
2. MergeGame > Screenshot Setup > Set Home Screen Data
3. Enter Play mode, wait for home screen to load
4. MergeGame > Screenshot Setup > Capture All Sizes
5. Exit Play mode

Round 2: Gameplay (mid-game)

Shows the actual drop/merge gameplay. This one you'll need to play manually.

1. MergeGame > Screenshot Setup > Unfreeze Merge Ball
2. MergeGame > Screenshot Setup > Reset All Screenshot Data
3. Enter Play mode, start a game
4. Play until the board looks good — a few merges in, maybe a combo showing
5. Press F12 at each Game View size (manually switch in the dropdown)
6. Exit Play mode

Round 3: Results

Shows the end-of-game overlay with merge card, quip, score.

1. MergeGame > Screenshot Setup > Set Results Data
2. Enter Play mode, start a game, let it end (or quit via the X)
3. On the results screen: MergeGame > Screenshot Setup > Capture All Sizes
4. Exit Play mode

Round 4: Onboarding

Shows the first-launch tutorial — clean, inviting.

1. MergeGame > Screenshot Setup > Set First Launch (Onboarding)
2. Enter Play mode — onboarding should appear
3. Tap through a couple drops so it looks active (maybe at tier 2-3)
4. Press F12 at each Game View size
5. Exit Play mode
