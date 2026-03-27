## NEXT UP

App Store Screenshots and info

## EVENTUALLY

Would be cool to have a simple list of past scores and ranking per day (might need to store the rank somehow?). Related to the Streak.
Note any top 3 finished on profile.

Merge reactions are still a bit weird when space is tight, especially with large balls. It jumps immediately.

Ball design doesn't inspire me. Waves seem stale. Colors a bit boring?

Wavelength react somehow to nearby?

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

---

After all rounds

- MergeGame > Screenshot Setup > Reset All Screenshot Data
- MergeGame > Screenshot Setup > Unfreeze Merge Ball

All screenshots land in Screenshots/ at the project root, named by device size.

Round 2 and 4 need manual F12 presses because you're controlling the game state by playing. Rounds 1 and 3 can use the automated Capture
All Sizes since the screen is static.

New workflow:

1. Run MergeGame > Screenshot Setup > Add Screenshot Sizes to Game View (once)
2. Set data + freeze ball
3. Enter Play mode
4. Select a resolution from the Game View dropdown (bottom-left of Game View), press F12
5. Switch to next resolution, press F12 again
6. Repeat
