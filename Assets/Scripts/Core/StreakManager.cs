using UnityEngine;
using System;

namespace MergeGame.Core
{
    public class StreakManager : MonoBehaviour
    {
        public static StreakManager Instance { get; private set; }

        private const string CurrentStreakKey = "current_streak";
        private const string LongestStreakKey = "longest_streak";
        private const string LastPlayedDateKey = "last_played_date";

        public int CurrentStreak { get; private set; }
        public int LongestStreak { get; private set; }

        public event Action<int, int> OnStreakUpdated; // currentStreak, longestStreak

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            LoadStreak();
        }

        private void LoadStreak()
        {
            CurrentStreak = PlayerPrefs.GetInt(CurrentStreakKey, 0);
            LongestStreak = PlayerPrefs.GetInt(LongestStreakKey, 0);
            string lastPlayed = PlayerPrefs.GetString(LastPlayedDateKey, "");

            // Check if streak is still valid
            if (!string.IsNullOrEmpty(lastPlayed))
            {
                string today = DateTime.Now.ToString("yyyy-MM-dd");
                string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

                if (lastPlayed != today && lastPlayed != yesterday)
                {
                    // Streak broken — missed a day
                    CurrentStreak = 0;
                    SaveStreak();
                }
            }
        }

        /// <summary>
        /// Called when a scored attempt is completed.
        /// </summary>
        public void RecordScoredAttempt()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");
            string lastPlayed = PlayerPrefs.GetString(LastPlayedDateKey, "");

            if (lastPlayed == today)
            {
                // Already recorded today
                return;
            }

            string yesterday = DateTime.Now.AddDays(-1).ToString("yyyy-MM-dd");

            if (lastPlayed == yesterday)
            {
                // Consecutive day — increment streak
                CurrentStreak++;
            }
            else
            {
                // First day or streak broken
                CurrentStreak = 1;
            }

            if (CurrentStreak > LongestStreak)
            {
                LongestStreak = CurrentStreak;
            }

            PlayerPrefs.SetString(LastPlayedDateKey, today);
            SaveStreak();
            OnStreakUpdated?.Invoke(CurrentStreak, LongestStreak);
        }

        public void ResetStreak()
        {
            CurrentStreak = 0;
            LongestStreak = 0;
            SaveStreak();
        }

        private void SaveStreak()
        {
            PlayerPrefs.SetInt(CurrentStreakKey, CurrentStreak);
            PlayerPrefs.SetInt(LongestStreakKey, LongestStreak);
            PlayerPrefs.Save();
        }
    }
}
