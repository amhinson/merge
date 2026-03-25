AUDIT

- No offline score queue — if network drops at game over, the scored attempt is lost. Should queue and retry.
- Privacy policy required — App Store requires one. You collect device UUID + display name + scores. Minimal, but needs to be documented.
- Hardcoded launch date in 2 files (DailySeedManager.cs, NewLeaderboardScreen.cs) — fragile if release date shifts. Can this be on the server somehow?
- No retry logic on network calls — single attempt, 30s timeout, silent failure.
- JSON built via string interpolation in some places — works due to sanitization but fragile. Should standardize to JsonUtility.ToJson().

Basic Analytics

Would be cool to have a simple list of past scores and ranking per day (might need to store the rank somehow?). Related to the Streak.
Create Gamecenter achievement.
Merge reactions are still a bit weird when space is tight, especially with large balls. It jumps immediately.

Affirmations afterwards, and really give an honest recap based on the users historical performance. Some light AI usage here.
Makes it a fun thing to look forward to after each game because it will always be unique.
Can Supabase host a lightweight AI model to generate this?
Maybe each day also has an AI generated funny Phish-adjacent phrase that is short, but lightly humorous.

Recap push notification the next day. "You played 2nd out of 98 people!" "You were in the top 75% of all players yesterday"
"Well, someone had to get last, right?"
Configure recap update time. Make it seem informative, not pushy to play the next day.
Use AI so it's unique each day.
Can group segements. Top 10 get their own. Then remaining top 50%, then lower 40% and 10%, then last gets something unique and funny-ish.

I really don't care about sharing much, for as much as I'm pushing it in the game. Maybe I shouldn't push the viral agenda too hard
and just appear more humble, and that I just wanted to build this game for myself, so I did.

Still not sold on the name or design. It should feel good to me, even if its simple. Make it genuine.
Ball design doesn't inspire me. Waves seem stale. Colors a bit boring?

Add username edit prompt after first game?
Wavelength react somehow to nearby?
Note any top 3 finished on profile.
