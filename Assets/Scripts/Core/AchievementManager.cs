using UnityEngine;
using System.Collections.Generic;

namespace MergeGame.Core
{
    public enum AchievementId
    {
        // Placeholder achievements — all will be replaced with final ones later
        FirstGameCompleted,
        ReachTier10,
        ReachTier11,
        SevenDayStreak,
        HiddenAchievement,
    }

    [System.Serializable]
    public class AchievementDef
    {
        public AchievementId id;
        public string gameCenterId; // Apple Game Center ID
        public string displayName; // internal reference only — Game Center shows its own strings
    }

    /// <summary>
    /// Tracks stats that feed into achievements, and reports progress to Game Center.
    /// Decoupled from game logic — listens to events, does not get called inline.
    /// </summary>
    public class AchievementManager : MonoBehaviour
    {
        public static AchievementManager Instance { get; private set; }

        private bool gameCenterAuthenticated;
        private HashSet<string> reportedAchievements = new HashSet<string>();

        // Placeholder achievement definitions
        private static readonly AchievementDef[] Achievements = new[]
        {
            new AchievementDef { id = AchievementId.FirstGameCompleted, gameCenterId = "first_game_completed", displayName = "First game completed" },
            new AchievementDef { id = AchievementId.ReachTier10, gameCenterId = "reach_tier10", displayName = "Reach tier 10" },
            new AchievementDef { id = AchievementId.ReachTier11, gameCenterId = "reach_tier11", displayName = "Reach tier 11" },
            new AchievementDef { id = AchievementId.SevenDayStreak, gameCenterId = "seven_day_streak", displayName = "7-day streak" },
            new AchievementDef { id = AchievementId.HiddenAchievement, gameCenterId = "hidden_secret", displayName = "Hidden: tap coming soon 10x" },
        };

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            AuthenticateGameCenter();
            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            if (MergeTracker.Instance != null)
            {
                MergeTracker.Instance.OnMerge += HandleMerge;
            }

            if (StreakManager.Instance != null)
            {
                StreakManager.Instance.OnStreakUpdated += HandleStreakUpdated;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (MergeTracker.Instance != null)
            {
                MergeTracker.Instance.OnMerge -= HandleMerge;
            }

            if (StreakManager.Instance != null)
            {
                StreakManager.Instance.OnStreakUpdated -= HandleStreakUpdated;
            }
        }

        public void OnGameCompleted(int finalScore)
        {
            ReportAchievement(AchievementId.FirstGameCompleted);
        }

        /// <summary>Called externally to unlock the hidden achievement.</summary>
        public void UnlockHiddenAchievement()
        {
            ReportAchievement(AchievementId.HiddenAchievement);
        }

        private void HandleMerge(int resultTier, int chainLength, Vector3 worldPos)
        {
            if (resultTier >= 9) // tier index 9 = tier 10 (0-indexed)
                ReportAchievement(AchievementId.ReachTier10);
            if (resultTier >= 10) // tier index 10 = tier 11 (largest)
                ReportAchievement(AchievementId.ReachTier11);
        }

        private void HandleStreakUpdated(int current, int longest)
        {
            if (current >= 7)
            {
                ReportAchievement(AchievementId.SevenDayStreak);
            }
        }

        private void ReportAchievement(AchievementId id)
        {
            var def = System.Array.Find(Achievements, a => a.id == id);
            if (def == null) return;

            if (reportedAchievements.Contains(def.gameCenterId)) return;
            reportedAchievements.Add(def.gameCenterId);

            ReportToGameCenter(def.gameCenterId, 100.0);
        }

        // ===== Game Center Integration =====

        private void AuthenticateGameCenter()
        {
#if UNITY_IOS && !UNITY_EDITOR
            Social.localUser.Authenticate((success, error) =>
            {
                gameCenterAuthenticated = success;
                if (!success)
                {
                    Debug.Log($"Game Center auth failed: {error}");
                }
            });
#endif
        }

        private void ReportToGameCenter(string achievementId, double progress)
        {
#if UNITY_IOS && !UNITY_EDITOR
            if (!gameCenterAuthenticated) return;

            Social.ReportProgress(achievementId, progress, (success) =>
            {
                if (!success)
                {
                    Debug.Log($"Failed to report achievement: {achievementId}");
                    // Remove from reported set so it can be retried
                    reportedAchievements.Remove(achievementId);
                }
            });
#endif
        }
    }
}
