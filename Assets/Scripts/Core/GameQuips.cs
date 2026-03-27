using UnityEngine;
using System.Collections.Generic;

namespace MergeGame.Core
{
    /// <summary>
    /// Generates quirky one-liner summaries based on game performance.
    /// No AI, no network — pure template matching with randomized variants.
    /// </summary>
    public static class GameQuips
    {
        /// <summary>
        /// Generate a quip based on the current game state.
        /// Call at game over, after merge counts and score are finalized.
        /// </summary>
        public static string GetQuip()
        {
            var context = GatherContext();
            var category = PickCategory(context);
            return PickVariant(category);
        }

        // ───── Context ─────

        private struct GameContext
        {
            public int score;
            public int highScore;
            public bool beatHighScore;
            public bool isNewHighScore; // first time beating it (not just matching)
            public int highestTier;
            public int longestChain;
            public int totalMerges;
            public int streak;
            public bool isPractice;
            public bool isFirstGame;
            public bool quitEarly; // quit via modal, not death line
            public int gamesPlayed; // total days played
        }

        private static GameContext GatherContext()
        {
            var ctx = new GameContext();
            ctx.score = ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0;
            ctx.highScore = ScoreManager.Instance != null ? ScoreManager.Instance.HighScore : 0;
            ctx.beatHighScore = ctx.score >= ctx.highScore && ctx.highScore > 0;
            ctx.isNewHighScore = ctx.score > ctx.highScore;
            ctx.isPractice = GameSession.IsPractice;

            if (MergeTracker.Instance != null)
            {
                ctx.highestTier = MergeTracker.Instance.HighestTierCreated;
                ctx.longestChain = MergeTracker.Instance.LongestChain;
                ctx.totalMerges = MergeTracker.Instance.TotalMerges;
            }

            ctx.streak = StreakManager.Instance != null ? StreakManager.Instance.CurrentStreak : 0;
            ctx.isFirstGame = !DailySeedManager.Instance?.HasCompletedScoredAttempt() ?? true;
            ctx.gamesPlayed = ctx.streak; // approximation — streak is the best proxy we have

            return ctx;
        }

        // ───── Category selection ─────

        private enum QuipCategory
        {
            FirstEverGame,
            NewHighScore,
            MatchedHighScore,
            HitMaxTier,
            LongChain,
            HighStreak,
            ModerateStreak,
            PracticeMode,
            LowScore,
            DecentScore,
            FallbackGeneric,
        }

        private static QuipCategory PickCategory(GameContext ctx)
        {
            // Priority order — most impressive/relevant first
            if (ctx.isFirstGame && !ctx.isPractice)
                return QuipCategory.FirstEverGame;

            if (ctx.isPractice)
                return QuipCategory.PracticeMode;

            if (ctx.isNewHighScore && ctx.score > 0)
                return QuipCategory.NewHighScore;

            if (ctx.beatHighScore)
                return QuipCategory.MatchedHighScore;

            if (ctx.highestTier >= 10)
                return QuipCategory.HitMaxTier;

            if (ctx.longestChain >= 5)
                return QuipCategory.LongChain;

            if (ctx.streak >= 7)
                return QuipCategory.HighStreak;

            if (ctx.streak >= 3)
                return QuipCategory.ModerateStreak;

            if (ctx.score <= 20)
                return QuipCategory.LowScore;

            if (ctx.score > 0)
                return QuipCategory.DecentScore;

            return QuipCategory.FallbackGeneric;
        }

        // ───── Variants ─────

        private static readonly Dictionary<QuipCategory, string[]> Variants = new()
        {
            [QuipCategory.FirstEverGame] = new[] {
                "Welcome to the daily drop.", 
            },
            [QuipCategory.NewHighScore] = new[] {
                "New high. Noted.",
                "That number didn't exist until just now.",
                "Previous best has been retired.",
                "Didn't know you had that in you.",
                "The scoreboard had to make room.",
                "Old record didn't survive the day.",
                "Your best just got better.",
            },
            [QuipCategory.MatchedHighScore] = new[] {
                "Same peak. Suspiciously consistent.",
                "Tied your best. The universe is testing you.",
                "Right on the line. Again.",
                "Exact same score. Weird.",
            },
            [QuipCategory.HitMaxTier] = new[] {
                "Most people never see this tier.",
                "Maximum size achieved. Now what.",
                "The top tier. It's quieter up here.",
                "That ball barely fit.",
                "The final form.",
            },
            [QuipCategory.LongChain] = new[] {
                "That chain was unreasonable.",
                "One drop, many consequences.",
                "The merges just kept going.",
                "Physics had nothing to do with it.",
                "That wasn't luck. Probably.",
                "The chain didn't want to stop.",
            },
            [QuipCategory.HighStreak] = new[] {
                "At this point it's a commitment.",
                "The streak lives. Don't jinx it.",
                "You haven't missed a day. We see you.",
                "Daily habit: confirmed.",
                "This is just what you do now.",
                "Missing a day would feel wrong.",
            },
            [QuipCategory.ModerateStreak] = new[] {
                "Streak's building. No pressure.",
                "A few days in. Momentum is real.",
                "You came back. That's the hard part.",
                "Getting into a rhythm.",
                "The streak appreciates your consistency.",
            },
            [QuipCategory.PracticeMode] = new[] {
                "Doesn't count. Still fun though.",
                "Off the record.",
                "Practice round. No judgement.",
                "Warm-up complete.",
                "Just between us.",
                "Rehearsal went well.",
                "Nobody's watching. Go wild.",
                "Free reps.",
                "The score vanishes at midnight anyway.",
                "Zero stakes. Full send.",
                "This is the sandbox.",
                "Tomorrow it's real. Today it's not.",
                "Unranked but not unnoticed.",
                "A run with no consequences.",
            },
            [QuipCategory.LowScore] = new[] {
                "Gravity won this round.",
                "Tough drop. Tomorrow resets everything.",
                "The balls were not cooperating today.",
                "We'll pretend this didn't happen.",
                "Some days the board wins.",
                "Brief. Very brief.",
                "That was over fast.",
            },
            [QuipCategory.DecentScore] = new[] {
                "Respectable.",
                "Clean game. Nothing to complain about.",
                "You knew what you were doing.",
                "Another day, another drop.",
                "Steady hands.",
                "The board respected you today.",
                "Quiet confidence.",
                "That was a proper game.",
            },
            [QuipCategory.FallbackGeneric] = new[] {
                "Done for today.",
                "See you tomorrow.",
                "The drop has been dropped.",
                "That happened.",
            },
        };

        private static string PickVariant(QuipCategory category)
        {
            if (!Variants.TryGetValue(category, out var options) || options.Length == 0)
                return "";
            return options[Random.Range(0, options.Length)];
        }
    }
}
