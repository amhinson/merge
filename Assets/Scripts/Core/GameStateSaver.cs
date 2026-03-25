using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using MergeGame.Data;

namespace MergeGame.Core
{
    [System.Serializable]
    public class BallState
    {
        public int tierIndex;
        public float x;
        public float y;
        public float rotation;
        public bool hasLanded;
        public float timeAboveDeathLine;
    }

    [System.Serializable]
    public class SavedGameState
    {
        public string gameDate;
        public int attemptType; // 0=Scored, 1=Replay

        public int sequenceIndex;
        public int currentScore;
        public int shakesRemaining;

        // Merge tracker stats
        public int longestChain;
        public int totalMerges;
        public int highestTier;
        public int[] tierCreationCounts;

        // Drop controller
        public int currentBallTier;
        public int nextBallTier;

        // Balls on screen
        public BallState[] balls;
    }

    /// <summary>
    /// Saves and restores mid-game state to PlayerPrefs.
    /// </summary>
    public static class GameStateSaver
    {
        private const string SaveKey = "saved_game_state";

        public static bool HasSavedGame()
        {
            if (!PlayerPrefs.HasKey(SaveKey)) return false;
            // Quick date check without full deserialization
            string json = PlayerPrefs.GetString(SaveKey, "");
            if (string.IsNullOrEmpty(json)) return false;
            var state = JsonUtility.FromJson<SavedGameState>(json);
            return state != null && state.gameDate == System.DateTime.Now.ToString("yyyy-MM-dd");
        }

        public static SavedGameState Load()
        {
            string json = PlayerPrefs.GetString(SaveKey, "");
            if (string.IsNullOrEmpty(json)) return null;

            var state = JsonUtility.FromJson<SavedGameState>(json);
            if (state == null) return null;

            // Discard if day rolled over
            if (state.gameDate != System.DateTime.Now.ToString("yyyy-MM-dd"))
            {
                Clear();
                return null;
            }

            return state;
        }

        public static void Save()
        {
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing)
                return;

            var state = new SavedGameState();

            // Metadata
            state.gameDate = DailySeedManager.Instance != null
                ? DailySeedManager.Instance.GameDate
                : System.DateTime.Now.ToString("yyyy-MM-dd");
            state.attemptType = DailySeedManager.Instance != null
                ? (int)DailySeedManager.Instance.CurrentAttemptType : 0;

            // Sequence
            state.sequenceIndex = DailySeedManager.Instance != null
                ? DailySeedManager.Instance.SequenceIndex : 0;

            // Score
            state.currentScore = ScoreManager.Instance != null
                ? ScoreManager.Instance.CurrentScore : 0;
            state.shakesRemaining = GameManager.Instance.ShakesRemaining;

            // Merge tracker
            if (MergeTracker.Instance != null)
            {
                state.longestChain = MergeTracker.Instance.LongestChain;
                state.totalMerges = MergeTracker.Instance.TotalMerges;
                state.highestTier = MergeTracker.Instance.HighestTierCreated;
                state.tierCreationCounts = new int[11];
                for (int i = 0; i < 11; i++)
                    state.tierCreationCounts[i] = MergeTracker.Instance.GetTierCreationCount(i);
            }

            // Drop controller
            if (DropController.Instance != null)
            {
                state.currentBallTier = DropController.Instance.CurrentBallTier;
                state.nextBallTier = DropController.Instance.NextBallTier;
            }

            // All balls on screen
            var ballControllers = Object.FindObjectsByType<BallController>(FindObjectsSortMode.None);
            var ballStates = new List<BallState>();
            foreach (var ball in ballControllers)
            {
                // Skip the preview/dropper ball (not yet dropped)
                if (!ball.HasLanded) continue;
                if (ball.BallData == null) continue;

                ballStates.Add(new BallState
                {
                    tierIndex = ball.TierIndex,
                    x = ball.transform.position.x,
                    y = ball.transform.position.y,
                    rotation = ball.transform.eulerAngles.z,
                    hasLanded = ball.HasLanded,
                    timeAboveDeathLine = ball.TimeAboveDeathLine,
                });
            }
            state.balls = ballStates.ToArray();

            string json = JsonUtility.ToJson(state);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
            Debug.Log($"[GameStateSaver] Saved {state.balls.Length} balls, score={state.currentScore}, seqIdx={state.sequenceIndex}");
        }

        public static void Clear()
        {
            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            Debug.Log("[GameStateSaver] Cleared saved game state");
        }

        /// <summary>
        /// Check if all balls have settled (no merges, low velocity).
        /// </summary>
        public static bool AreAllBallsSettled()
        {
            var balls = Object.FindObjectsByType<BallController>(FindObjectsSortMode.None);
            foreach (var ball in balls)
            {
                if (ball.IsMerging) return false;
                var rb = ball.GetComponent<Rigidbody2D>();
                if (rb != null && rb.bodyType == RigidbodyType2D.Dynamic &&
                    rb.linearVelocity.magnitude > 0.15f)
                    return false;
            }
            return true;
        }
    }
}
