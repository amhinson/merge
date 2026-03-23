using UnityEngine;
using System;
using System.Collections.Generic;

namespace MergeGame.Backend
{
    [Serializable]
    public class LeaderboardEntry
    {
        public string device_uuid;
        public string display_name;
        public int score;
        public int[] largest_merges;
        public int rank;
    }

    [Serializable]
    public class LeaderboardResponse
    {
        public LeaderboardEntry[] entries;
    }

    [Serializable]
    public class PlayerRankResponse
    {
        public int rank;
        public int total_players;
        public string percentile;
    }

    [Serializable]
    public class PlayerProfileResponse
    {
        public string display_name;
        public int today_score;    // 0 if not played
        public int day_number;
        public int current_streak;
        public int longest_streak;
    }

    [Serializable]
    public class SubmitScoreRequest
    {
        public string device_uuid;
        public string display_name;
        public int score;
        public string game_date;
        public int day_number;
        public int[] largest_merges;
    }

    public class LeaderboardService : MonoBehaviour
    {
        public static LeaderboardService Instance { get; private set; }

        // Cached leaderboard for live rank comparison
        private List<LeaderboardEntry> cachedLeaderboard = new List<LeaderboardEntry>();
        private float lastLeaderboardFetch;
        private const float LeaderboardCacheInterval = 45f; // seconds

        public event Action<List<LeaderboardEntry>> OnLeaderboardUpdated;
        public event Action<int, string> OnPlayerRankUpdated; // rank, percentile

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void SubmitScore(int score, string gameDate, int dayNumber, int[] largestMerges, Action<bool> callback = null)
        {
            if (SupabaseClient.Instance == null)
            {
                callback?.Invoke(false);
                return;
            }

            var identity = MergeGame.Core.PlayerIdentity.Instance;
            if (identity == null)
            {
                callback?.Invoke(false);
                return;
            }

            var request = new SubmitScoreRequest
            {
                device_uuid = identity.DeviceUUID,
                display_name = identity.DisplayName,
                score = score,
                game_date = gameDate,
                day_number = dayNumber,
                largest_merges = largestMerges
            };

            string json = JsonUtility.ToJson(request);
            SupabaseClient.Instance.CallFunction("submit-score", json, (success, response) =>
            {
                callback?.Invoke(success);
            });
        }

        public void FetchLeaderboard(string gameDate, Action<List<LeaderboardEntry>> callback = null)
        {
            if (SupabaseClient.Instance == null)
            {
                callback?.Invoke(new List<LeaderboardEntry>());
                return;
            }

            SupabaseClient.Instance.CallFunctionGet("get-leaderboard", $"game_date={gameDate}", (success, response) =>
            {
                Debug.Log($"Leaderboard fetch success={success}, response={response}");
                if (success && !string.IsNullOrEmpty(response))
                {
                    try
                    {
                        // Response is a JSON array — wrap it for JsonUtility
                        string wrapped = $"{{\"entries\":{response}}}";
                        var parsed = JsonUtility.FromJson<LeaderboardResponse>(wrapped);
                        cachedLeaderboard = new List<LeaderboardEntry>(parsed.entries ?? new LeaderboardEntry[0]);
                        lastLeaderboardFetch = Time.time;
                        Debug.Log($"Parsed {cachedLeaderboard.Count} leaderboard entries");
                        OnLeaderboardUpdated?.Invoke(cachedLeaderboard);
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Failed to parse leaderboard: {e.Message}\nResponse: {response}");
                        cachedLeaderboard = new List<LeaderboardEntry>();
                    }
                }
                callback?.Invoke(cachedLeaderboard);
            });
        }

        public void FetchPlayerRank(string gameDate, Action<int, string> callback = null)
        {
            if (SupabaseClient.Instance == null)
            {
                callback?.Invoke(-1, "");
                return;
            }

            var identity = MergeGame.Core.PlayerIdentity.Instance;
            if (identity == null)
            {
                callback?.Invoke(-1, "");
                return;
            }

            string query = $"device_uuid={identity.DeviceUUID}&game_date={gameDate}";
            SupabaseClient.Instance.CallFunctionGet("get-player-rank", query, (success, response) =>
            {
                if (success)
                {
                    try
                    {
                        var parsed = JsonUtility.FromJson<PlayerRankResponse>(response);
                        OnPlayerRankUpdated?.Invoke(parsed.rank, parsed.percentile);
                        callback?.Invoke(parsed.rank, parsed.percentile);
                    }
                    catch
                    {
                        callback?.Invoke(-1, "");
                    }
                }
                else
                {
                    callback?.Invoke(-1, "");
                }
            });
        }

        /// <summary>
        /// Get approximate live rank by comparing score against cached leaderboard.
        /// Does not make a network call.
        /// </summary>
        public int GetApproximateLiveRank(int currentScore)
        {
            if (cachedLeaderboard == null || cachedLeaderboard.Count == 0)
                return -1;

            int rank = 1;
            foreach (var entry in cachedLeaderboard)
            {
                if (currentScore >= entry.score) break;
                rank++;
            }
            return rank;
        }

        /// <summary>
        /// Refresh cached leaderboard if stale. Call during gameplay periodically.
        /// </summary>
        public void RefreshIfStale(string gameDate)
        {
            if (Time.time - lastLeaderboardFetch >= LeaderboardCacheInterval)
            {
                FetchLeaderboard(gameDate);
            }
        }

        /// <summary>
        /// Fetch player profile for today: today's score, day number, streak.
        /// Used at app launch to determine Fresh vs Played home screen.
        /// </summary>
        public void FetchPlayerProfile(string deviceUUID, string gameDate, Action<PlayerProfileResponse> callback)
        {
            if (SupabaseClient.Instance == null)
            {
                callback?.Invoke(null);
                return;
            }

            string json = $"{{\"device_uuid\":\"{deviceUUID}\",\"game_date\":\"{gameDate}\"}}";
            SupabaseClient.Instance.CallFunction("get-player-profile", json, (success, response) =>
            {
                if (success && !string.IsNullOrEmpty(response))
                {
                    try
                    {
                        var parsed = JsonUtility.FromJson<PlayerProfileResponse>(response);
                        callback?.Invoke(parsed);
                        return;
                    }
                    catch (Exception e)
                    {
                        Debug.LogWarning($"Failed to parse player profile: {e.Message}");
                    }
                }
                callback?.Invoke(null);
            });
        }

        public void UpdateDisplayName(string newName, Action<bool> callback = null)
        {
            if (SupabaseClient.Instance == null)
            {
                callback?.Invoke(false);
                return;
            }

            var identity = MergeGame.Core.PlayerIdentity.Instance;
            if (identity == null)
            {
                callback?.Invoke(false);
                return;
            }

            string json = $"{{\"device_uuid\":\"{identity.DeviceUUID}\",\"display_name\":\"{newName}\"}}";
            SupabaseClient.Instance.CallFunction("update-display-name", json, (success, _) =>
            {
                callback?.Invoke(success);
            });
        }
    }
}
