using UnityEngine;
using System;

namespace MergeGame.Core
{
    /// <summary>
    /// Centralized session state. Static, non-MonoBehaviour.
    /// Reads from existing managers but provides a single place for UI to query.
    /// </summary>
    [Serializable]
    public class Player
    {
        public string device_uuid;
        public string display_name;
        public string created_at;
        public int current_streak;
        public int longest_streak;
    }

    [Serializable]
    public class DailyScore
    {
        public long   id;
        public string device_uuid;
        public string game_date;
        public int    score;
        public int    day_number;
        public int[]  largest_merges;
        public string submitted_at;
    }

    [Serializable]
    public class PlayerRank
    {
        public int rank;
        public int total_players;
    }

    public static class GameSession
    {
        public static string  DeviceUUID     { get; private set; }
        public static Player  CurrentPlayer  { get; set; }
        public static int     TodayScore     { get; set; }
        public static bool    HasPlayedToday => TodayScore > 0;
        public static int     TodayDayNumber { get; set; }
        public static string  TodayDateStr   { get; set; }
        public static int[]   MergeCounts    { get; set; }  // index = ball tier 0-10, value = merge count
        public static bool    IsPractice     { get; set; }
        public static bool    IsFirstLaunch  { get; private set; }

        // Result data from submit-score response
        public static int     ResultRank         { get; set; }
        public static int     ResultTotalPlayers { get; set; }

        /// <summary>
        /// Call once at app startup, before any screen is shown.
        /// Pulls identity from PlayerIdentity and date from DailySeedManager.
        /// </summary>
        public static void Init()
        {
            // Identity
            if (PlayerIdentity.Instance != null)
            {
                DeviceUUID = PlayerIdentity.Instance.DeviceUUID;
                IsFirstLaunch = string.IsNullOrEmpty(
                    PlayerPrefs.GetString("player_uuid_initialized", ""));
            }
            else
            {
                DeviceUUID = SystemInfo.deviceUniqueIdentifier;
                IsFirstLaunch = false;
            }

            // Date
            TodayDateStr = DateTime.Now.ToString("yyyy-MM-dd");

            if (DailySeedManager.Instance != null)
            {
                DailySeedManager.Instance.RefreshDay();
                TodayDayNumber = DailySeedManager.Instance.DayNumber;
            }

            // Reset per-session state
            TodayScore = 0;
            MergeCounts = new int[11];
            IsPractice = false;
            ResultRank = 0;
            ResultTotalPlayers = 0;
        }

        /// <summary>
        /// Refresh date-dependent fields (call when returning to home screen).
        /// </summary>
        public static void RefreshDay()
        {
            TodayDateStr = DateTime.Now.ToString("yyyy-MM-dd");
            if (DailySeedManager.Instance != null)
            {
                DailySeedManager.Instance.RefreshDay();
                TodayDayNumber = DailySeedManager.Instance.DayNumber;
            }
        }

        /// <summary>
        /// Snapshot merge counts from MergeTracker at end of game.
        /// </summary>
        public static void CaptureMergeCounts()
        {
            MergeCounts = new int[11];
            if (MergeTracker.Instance == null) return;
            for (int i = 0; i < 11; i++)
                MergeCounts[i] = MergeTracker.Instance.GetTierCreationCount(i);
        }

        /// <summary>
        /// Mark that first-launch onboarding is complete.
        /// </summary>
        public static void MarkOnboardingComplete()
        {
            PlayerPrefs.SetString("player_uuid_initialized", "1");
            PlayerPrefs.Save();
            IsFirstLaunch = false;
        }
    }
}
