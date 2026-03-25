using UnityEngine;
using System.Collections.Generic;

namespace MergeGame.Core
{
    public enum AchievementId
    {
        // Placeholder achievements — all will be replaced with final ones later
        FirstGameCompleted,
        FirstTier5,
        SevenDayStreak,
        HiddenAchievement,
    }

    [System.Serializable]
    public class AchievementDef
    {
        public AchievementId id;
        public string gameCenterId; // Apple Game Center ID
        public string displayName;
        public string description;
        public bool hidden;
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
            new AchievementDef
            {
                id = AchievementId.FirstGameCompleted,
                gameCenterId = "first_game_completed",
                displayName = "First Steps",
                description = "Complete your first game",
                hidden = false
            },
            new AchievementDef
            {
                id = AchievementId.FirstTier5,
                gameCenterId = "first_tier5",
                displayName = "Getting Bigger",
                description = "Create a Tier 5 ball",
                hidden = false
            },
            new AchievementDef
            {
                id = AchievementId.SevenDayStreak,
                gameCenterId = "seven_day_streak",
                displayName = "Dedicated",
                description = "Maintain a 7-day streak",
                hidden = false
            },
            new AchievementDef
            {
                id = AchievementId.HiddenAchievement,
                gameCenterId = "hidden_secret",
                displayName = "???",
                description = "You found a secret!",
                hidden = true
            },
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

            // Hidden: exact score of 1234
            if (finalScore == 1234)
            {
                ReportAchievement(AchievementId.HiddenAchievement);
            }
        }

        private void HandleMerge(int resultTier, int chainLength, Vector3 worldPos)
        {
            if (resultTier >= 4)
            {
                ReportAchievement(AchievementId.FirstTier5);
            }
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
