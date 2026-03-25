using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace MergeGame.Core
{
    /// <summary>
    /// Tracks merge statistics during a round for achievements, result cards, and leaderboard submission.
    /// </summary>
    public class MergeTracker : MonoBehaviour
    {
        public static MergeTracker Instance { get; private set; }

        // Top merges (sorted descending by tier)
        private List<int> topMergeTiers = new List<int>();
        private const int MaxTopMerges = 4;

        // Chain tracking
        private int currentChainLength;
        private float lastMergeTime;
        private const float ChainTimeout = 1.0f;

        // Per-round stats
        public int HighestTierCreated { get; private set; }
        public int LongestChain { get; private set; }
        public int CurrentChainLength => currentChainLength;
        public int TotalMerges { get; private set; }
        public float RoundStartTime { get; private set; }
        public float RoundDuration => Time.time - RoundStartTime;
        public bool MergedBeforeFloorTouch { get; private set; }
        public float LongestTimeAboveDeathLine { get; private set; }
        public int GameOverScreenTaps { get; private set; }
        public int ReplayCount { get; private set; }

        // Per-tier creation counts
        private int[] tierCreationCounts = new int[11];

        // Drop timing
        private float lastDropTime;
        public float LongestIdleTime { get; private set; }
        private bool firstBallLanded;

        public event System.Action<int, int, Vector3> OnMerge; // mergedTier, chainLength, worldPosition
        public event System.Action<int> OnChainComplete; // finalChainLength

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void ResetForNewRound()
        {
            topMergeTiers.Clear();
            currentChainLength = 0;
            lastMergeTime = -999f;
            HighestTierCreated = 0;
            LongestChain = 0;
            TotalMerges = 0;
            RoundStartTime = Time.time;
            MergedBeforeFloorTouch = false;
            LongestTimeAboveDeathLine = 0f;
            GameOverScreenTaps = 0;
            firstBallLanded = false;
            lastDropTime = Time.time;
            LongestIdleTime = 0f;
            tierCreationCounts = new int[11];
        }

        public void RecordMerge(int resultTier, Vector3 worldPosition = default)
        {
            TotalMerges++;

            if (resultTier > HighestTierCreated)
                HighestTierCreated = resultTier;

            if (resultTier >= 0 && resultTier < tierCreationCounts.Length)
                tierCreationCounts[resultTier]++;

            // Track top merges
            topMergeTiers.Add(resultTier);
            topMergeTiers.Sort((a, b) => b.CompareTo(a));
            if (topMergeTiers.Count > MaxTopMerges)
                topMergeTiers.RemoveAt(topMergeTiers.Count - 1);

            // Chain tracking
            float timeSinceLastMerge = Time.time - lastMergeTime;
            if (timeSinceLastMerge <= ChainTimeout)
            {
                currentChainLength++;
            }
            else
            {
                if (currentChainLength > 0)
                    OnChainComplete?.Invoke(currentChainLength);
                currentChainLength = 1;
            }

            if (currentChainLength > LongestChain)
                LongestChain = currentChainLength;

            lastMergeTime = Time.time;
            OnMerge?.Invoke(resultTier, currentChainLength, worldPosition);
        }

        public void RecordDrop()
        {
            float idleTime = Time.time - lastDropTime;
            if (idleTime > LongestIdleTime)
                LongestIdleTime = idleTime;
            lastDropTime = Time.time;
        }

        public void RecordFirstLanding()
        {
            firstBallLanded = true;
        }

        public void RecordMergeBeforeFloorTouch()
        {
            if (!firstBallLanded)
                MergedBeforeFloorTouch = true;
        }

        public void RecordDeathLineSurvival(float duration)
        {
            if (duration > LongestTimeAboveDeathLine)
                LongestTimeAboveDeathLine = duration;
        }

        public void RecordGameOverTap()
        {
            GameOverScreenTaps++;
        }

        public void IncrementReplayCount()
        {
            ReplayCount++;
        }

        public int GetTierCreationCount(int tier)
        {
            if (tier < 0 || tier >= tierCreationCounts.Length) return 0;
            return tierCreationCounts[tier];
        }

        /// <summary>Returns the top merge tiers (descending), up to 4.</summary>
        public int[] GetTopMergeTiers()
        {
            return topMergeTiers.ToArray();
        }
    }
}
