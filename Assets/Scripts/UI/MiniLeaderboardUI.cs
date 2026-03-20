using UnityEngine;
using TMPro;
using System.Collections.Generic;
using MergeGame.Core;
using MergeGame.Backend;

namespace MergeGame.UI
{
    /// <summary>
    /// Compact in-game leaderboard. Shows #1, players near you, and your rank.
    /// Hidden entirely on replay attempts.
    /// </summary>
    public class MiniLeaderboardUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI topPlayerText;
        [SerializeField] private TextMeshProUGUI playerRow;

        private List<LeaderboardEntry> cachedEntries = new List<LeaderboardEntry>();

        public void UpdateWithScore(int currentScore, bool isReplay)
        {
            gameObject.SetActive(true);

            if (LeaderboardService.Instance == null) return;
            var entries = cachedEntries;
            if (entries.Count == 0)
            {
                if (topPlayerText != null) topPlayerText.text = "";
                if (playerRow != null) playerRow.text = "";
                return;
            }

            // Top players
            if (topPlayerText != null)
            {
                string lines = "";
                int showCount = Mathf.Min(entries.Count, isReplay ? 4 : 2);
                for (int i = 0; i < showCount; i++)
                {
                    var e = entries[i];
                    if (i > 0) lines += "\n";
                    lines += FormatRow($"#{i + 1}", e.display_name, e.score);
                }
                topPlayerText.text = lines;
                topPlayerText.richText = true;
            }

            // Player row — only show on scored attempts
            if (playerRow != null)
            {
                if (isReplay)
                {
                    playerRow.text = "";
                }
                else
                {
                    int rank = LeaderboardService.Instance.GetApproximateLiveRank(currentScore);
                    string playerName = PlayerIdentity.Instance != null
                        ? PlayerIdentity.Instance.DisplayName : "You";

                    string rankStr = rank > 0 ? $"#{rank}" : "--";
                    string yourLine = FormatRow(rankStr, playerName, currentScore);

                    if (rank > 0 && rank < entries.Count)
                    {
                        var below = entries[rank];
                        yourLine += "\n" + FormatRow($"#{rank + 1}", below.display_name, below.score);
                    }

                    playerRow.text = yourLine;
                    playerRow.richText = true;
                }
            }
        }

        public void Clear()
        {
            if (topPlayerText != null) topPlayerText.text = "";
            if (playerRow != null) playerRow.text = "";
        }

        public void SetCachedEntries(List<LeaderboardEntry> entries)
        {
            cachedEntries = entries ?? new List<LeaderboardEntry>();
        }

        private static string FormatRow(string rank, string name, int score)
        {
            string truncName = Trunc(name, 6);
            return $"{rank.PadRight(3)} {truncName.PadRight(8)} {score}";
        }

        private static string Trunc(string s, int max)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Length <= max ? s : s.Substring(0, max) + "..";
        }
    }
}
