using UnityEngine;

namespace MergeGame.Core
{
    /// <summary>
    /// Singleton wrapper around GameAnalytics custom events.
    /// All methods fail silently — analytics should never crash the game.
    ///
    /// Setup: Add the GameAnalytics prefab to the scene and configure
    /// game keys in its inspector. This class just fires events.
    /// </summary>
    public class MurgeAnalytics : MonoBehaviour
    {
        public static MurgeAnalytics Instance { get; private set; }

        private System.Collections.IEnumerator InitializeGA()
        {
#if UNITY_EDITOR
            // GameAnalytics doesn't support the editor — skip initialization
            yield break;
#else
            // Wait a frame so all MonoBehaviours are set up
            yield return null;

            try
            {
                GameAnalyticsSDK.GameAnalytics.Initialize();
                Debug.Log("[Analytics] GameAnalytics initialized");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Analytics] GameAnalytics init failed: {e.Message}");
            }
#endif
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Initialize GameAnalytics from the Settings asset (configured via
            // Window > GameAnalytics > Select Settings). No prefab needed.
            StartCoroutine(InitializeGA());
        }

        /// <summary>Player starts a new puzzle (scored or practice).</summary>
        public void TrackPuzzleStarted()
        {
            try
            {
                bool isScored = DailySeedManager.Instance != null &&
                    DailySeedManager.Instance.CurrentAttemptType == AttemptType.Scored;

                GameAnalyticsSDK.GameAnalytics.NewProgressionEvent(
                    GameAnalyticsSDK.GAProgressionStatus.Start,
                    "puzzle",
                    isScored ? "scored" : "practice"
                );

                GameAnalyticsSDK.GameAnalytics.NewDesignEvent("puzzle:start", isScored ? 1f : 0f);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Analytics] TrackPuzzleStarted failed: {e.Message}");
            }
        }

        /// <summary>Player completes a puzzle (game over with score).</summary>
        public void TrackPuzzleCompleted(int score, float timeElapsed)
        {
            try
            {
                bool isScored = DailySeedManager.Instance != null &&
                    DailySeedManager.Instance.CurrentAttemptType == AttemptType.Scored;

                GameAnalyticsSDK.GameAnalytics.NewProgressionEvent(
                    GameAnalyticsSDK.GAProgressionStatus.Complete,
                    "puzzle",
                    isScored ? "scored" : "practice",
                    score
                );

                GameAnalyticsSDK.GameAnalytics.NewDesignEvent("puzzle:score", score);
                GameAnalyticsSDK.GameAnalytics.NewDesignEvent("puzzle:duration_seconds", timeElapsed);

                if (MergeTracker.Instance != null)
                {
                    GameAnalyticsSDK.GameAnalytics.NewDesignEvent(
                        "puzzle:highest_tier", MergeTracker.Instance.HighestTierCreated);
                    GameAnalyticsSDK.GameAnalytics.NewDesignEvent(
                        "puzzle:total_merges", MergeTracker.Instance.TotalMerges);
                    GameAnalyticsSDK.GameAnalytics.NewDesignEvent(
                        "puzzle:longest_chain", MergeTracker.Instance.LongestChain);
                }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Analytics] TrackPuzzleCompleted failed: {e.Message}");
            }
        }

        /// <summary>Player's puzzle ends (alias for completed, kept for clarity).</summary>
        public void TrackPuzzleFailed(int score)
        {
            try
            {
                bool isScored = DailySeedManager.Instance != null &&
                    DailySeedManager.Instance.CurrentAttemptType == AttemptType.Scored;

                GameAnalyticsSDK.GameAnalytics.NewProgressionEvent(
                    GameAnalyticsSDK.GAProgressionStatus.Fail,
                    "puzzle",
                    isScored ? "scored" : "practice",
                    score
                );
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Analytics] TrackPuzzleFailed failed: {e.Message}");
            }
        }

        /// <summary>Player taps the share button.</summary>
        public void TrackShareCard()
        {
            try
            {
                GameAnalyticsSDK.GameAnalytics.NewDesignEvent("social:share_card");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Analytics] TrackShareCard failed: {e.Message}");
            }
        }

        /// <summary>Player's streak updated after a scored game.</summary>
        public void TrackStreakUpdated(int streakLength)
        {
            try
            {
                GameAnalyticsSDK.GameAnalytics.NewDesignEvent("engagement:streak", streakLength);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Analytics] TrackStreakUpdated failed: {e.Message}");
            }
        }

        /// <summary>Player completes onboarding.</summary>
        public void TrackOnboardingComplete()
        {
            try
            {
                GameAnalyticsSDK.GameAnalytics.NewDesignEvent("onboarding:complete");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Analytics] TrackOnboardingComplete failed: {e.Message}");
            }
        }

        /// <summary>Player uses a shake.</summary>
        public void TrackShakeUsed(int remaining)
        {
            try
            {
                GameAnalyticsSDK.GameAnalytics.NewDesignEvent("gameplay:shake_used", remaining);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Analytics] TrackShakeUsed failed: {e.Message}");
            }
        }

        /// <summary>Player changes their display name.</summary>
        public void TrackNameChanged()
        {
            try
            {
                GameAnalyticsSDK.GameAnalytics.NewDesignEvent("engagement:name_changed");
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[Analytics] TrackNameChanged failed: {e.Message}");
            }
        }
    }
}
