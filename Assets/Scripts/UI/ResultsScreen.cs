using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MergeGame.Core;
using MergeGame.Data;

namespace MergeGame.UI
{
    /// <summary>
    /// Results screen shown after game over.
    /// </summary>
    public class ResultsScreen : MonoBehaviour
    {
        [Header("Score Display")]
        [SerializeField] private TextMeshProUGUI dayLabel;
        [SerializeField] private ScoreTickUp scoreTickUp;
        [SerializeField] private TextMeshProUGUI streakText;

        [Header("Merge Display")]
        [SerializeField] private Transform mergeIconsContainer;
        [SerializeField] private BallTierConfig tierConfig;

        [Header("Rank")]
        [SerializeField] private TextMeshProUGUI rankText;
        [SerializeField] private TextMeshProUGUI replayLabel;

        [Header("Buttons")]
        [SerializeField] private Button shareButton;
        [SerializeField] private Button playAgainButton;
        [SerializeField] private Button homeButton;

        public Button ShareButton => shareButton;
        public Button PlayAgainButton => playAgainButton;
        public Button HomeButton => homeButton;

        public void Populate(int dayNumber, int finalScore, int streak, int[] topMergeTiers,
            bool isScored, int rank, string percentile)
        {
            if (dayLabel != null)
                dayLabel.text = $"Daily Drop #{dayNumber}";

            if (scoreTickUp != null)
            {
                scoreTickUp.SetImmediate(0);
                scoreTickUp.AnimateTo(finalScore);
            }

            if (streakText != null)
                streakText.text = streak > 0 ? $"{streak} day streak" : "";

            PopulateMergeIcons();

            if (rankText != null)
            {
                if (isScored && rank > 0)
                    rankText.text = percentile;
                else
                    rankText.text = "";
            }

            if (replayLabel != null)
                replayLabel.gameObject.SetActive(!isScored);
        }

        private void PopulateMergeIcons()
        {
            if (mergeIconsContainer == null || tierConfig == null) return;

            // Clear existing
            foreach (Transform child in mergeIconsContainer)
                Destroy(child.gameObject);

            if (MergeTracker.Instance == null) return;

            // Show every tier that was merged at least once, sorted by tier ascending
            for (int tier = 0; tier < 11; tier++)
            {
                int count = MergeTracker.Instance.GetTierCreationCount(tier);
                if (count <= 0) continue;

                var data = tierConfig.GetTier(tier);
                if (data == null) continue;

                // Container for ball + count
                var iconObj = new GameObject($"Tier{tier}");
                iconObj.transform.SetParent(mergeIconsContainer, false);
                var iconRT = iconObj.AddComponent<RectTransform>();
                iconRT.sizeDelta = new Vector2(75, 85);
                var iconLE = iconObj.AddComponent<LayoutElement>();
                iconLE.preferredWidth = 75;
                iconLE.preferredHeight = 85;

                // Ball sprite
                var circleObj = new GameObject("Ball");
                circleObj.transform.SetParent(iconObj.transform, false);
                var circleRT = circleObj.AddComponent<RectTransform>();
                circleRT.anchorMin = new Vector2(0.5f, 0.4f);
                circleRT.anchorMax = new Vector2(0.5f, 0.4f);
                circleRT.pivot = new Vector2(0.5f, 0.5f);
                circleRT.anchoredPosition = new Vector2(0, 12);
                // Size proportional to tier
                float ballSize = 25f + (tier * 2.5f);
                circleRT.sizeDelta = new Vector2(ballSize, ballSize);

                var img = circleObj.AddComponent<Image>();
                img.color = data.color;
                if (data.sprite != null) img.sprite = data.sprite;
                img.preserveAspect = true;

                // Count text below
                var countObj = new GameObject("Count");
                countObj.transform.SetParent(iconObj.transform, false);
                var countRT = countObj.AddComponent<RectTransform>();
                countRT.anchorMin = new Vector2(0, 0);
                countRT.anchorMax = new Vector2(1, 0.3f);
                countRT.offsetMin = Vector2.zero;
                countRT.offsetMax = Vector2.zero;

                var countTMP = countObj.AddComponent<TextMeshProUGUI>();
                countTMP.text = $"{count}x";
                countTMP.fontSize = 16;
                countTMP.alignment = TextAlignmentOptions.Center;
                countTMP.color = new Color(0.75f, 0.75f, 0.8f);
            }
        }
    }
}
