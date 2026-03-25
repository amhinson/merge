using UnityEngine;
using System;
using System.Collections.Generic;
using MergeGame.Data;

namespace MergeGame.Core
{
    public enum AttemptType
    {
        Scored,
        Replay
    }

    public class DailySeedManager : MonoBehaviour
    {
        public static DailySeedManager Instance { get; private set; }

        [SerializeField] private BallTierConfig tierConfig;

        // Hardcoded launch date — Day 1
        private static readonly DateTime LaunchDate = new DateTime(2026, 3, 20);

        private string currentGameDate;
        private SeededRandom seededRng;
        private List<int> dailySequence;
        private int sequenceIndex;

        public string GameDate => currentGameDate;
        public int DayNumber { get; private set; }
        public int SequenceIndex => sequenceIndex;
        public AttemptType CurrentAttemptType { get; private set; }

        public event Action<AttemptType> OnAttemptTypeChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        /// <summary>
        /// Initialize or refresh for the current local date.
        /// Call this at game start and when starting a new game.
        /// </summary>
        public void RefreshDay()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");

            if (currentGameDate != today)
            {
                currentGameDate = today;
                DayNumber = CalculateDayNumber(today);
                GenerateSequence();
            }
        }

        /// <summary>
        /// Prepare for a new game. Resets the sequence index.
        /// </summary>
        public void PrepareNewGame()
        {
            RefreshDay();
            sequenceIndex = 0;
            CurrentAttemptType = HasCompletedScoredAttempt() ? AttemptType.Replay : AttemptType.Scored;
            OnAttemptTypeChanged?.Invoke(CurrentAttemptType);
        }

        /// <summary>Restore sequence state for save/resume.</summary>
        public void RestoreSequence(int index, AttemptType attempt)
        {
            RefreshDay();
            sequenceIndex = index;
            CurrentAttemptType = attempt;
        }

        /// <summary>
        /// Get the next ball tier index from the daily sequence.
        /// </summary>
        public BallData GetNextBall()
        {
            if (dailySequence == null || dailySequence.Count == 0)
            {
                GenerateSequence();
            }

            // Extend sequence if needed
            while (sequenceIndex >= dailySequence.Count)
            {
                int tierIdx = seededRng.Range(Mathf.Min(tierConfig.maxDropTier, tierConfig.tiers.Length));
                dailySequence.Add(tierIdx);
            }

            int idx = dailySequence[sequenceIndex];
            sequenceIndex++;
            return tierConfig.GetTier(idx);
        }

        /// <summary>
        /// Peek at the next ball without advancing the sequence.
        /// </summary>
        public BallData PeekNextBall()
        {
            if (dailySequence == null || dailySequence.Count == 0)
            {
                GenerateSequence();
            }

            while (sequenceIndex >= dailySequence.Count)
            {
                int tierIdx = seededRng.Range(Mathf.Min(tierConfig.maxDropTier, tierConfig.tiers.Length));
                dailySequence.Add(tierIdx);
            }

            return tierConfig.GetTier(dailySequence[sequenceIndex]);
        }

        public bool HasCompletedScoredAttempt()
        {
            return PlayerPrefs.GetInt(ScoredAttemptKey(), 0) == 1;
        }

        public void MarkScoredAttemptComplete()
        {
            PlayerPrefs.SetInt(ScoredAttemptKey(), 1);
            PlayerPrefs.Save();
        }

        private string ScoredAttemptKey()
        {
            return $"scored_attempt_{currentGameDate}";
        }

        private void GenerateSequence()
        {
            int seed = SeededRandom.SeedFromDate(currentGameDate);
            seededRng = new SeededRandom(seed);

            // Pre-generate a reasonable initial sequence
            dailySequence = new List<int>(200);
            int maxTier = Mathf.Min(tierConfig.maxDropTier, tierConfig.tiers.Length);
            for (int i = 0; i < 200; i++)
            {
                dailySequence.Add(seededRng.Range(maxTier));
            }
        }

        private int CalculateDayNumber(string dateStr)
        {
            if (DateTime.TryParse(dateStr, out DateTime date))
            {
                int days = (date.Date - LaunchDate.Date).Days;
                return Mathf.Max(1, days + 1);
            }
            return 1;
        }
    }
}
