using UnityEngine;
using TMPro;
using MergeGame.Core;
using MergeGame.Backend;

namespace MergeGame.UI
{
    /// <summary>
    /// Displays approximate live rank during gameplay.
    /// Self-contained, repositionable component.
    /// </summary>
    public class LiveRankUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI replayIndicator;

        private int lastDisplayedRank = -1;

        public void UpdateRank(int rank, bool isReplay)
        {
            if (rankText == null) return;

            if (rank <= 0)
            {
                rankText.text = "";
            }
            else
            {
                rankText.text = $"#{rank}";
                lastDisplayedRank = rank;
            }

            if (replayIndicator != null)
            {
                replayIndicator.gameObject.SetActive(isReplay);
            }
        }

        public void Clear()
        {
            if (rankText != null) rankText.text = "";
            if (replayIndicator != null) replayIndicator.gameObject.SetActive(false);
            lastDisplayedRank = -1;
        }
    }
}
