using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using MergeGame.Core;
using MergeGame.Data;

namespace MergeGame.UI
{
    /// <summary>
    /// Result overlay shown on top of the frozen game arena.
    /// Displays final score, rank badge, full 11-tier merge grid, and Done/Share buttons.
    /// </summary>
    public class ResultOverlayScreen : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BallTierConfig tierConfig;

        // Built UI
        private TextMeshProUGUI scoreValue;
        private TextMeshProUGUI rankLabel;
        private TextMeshProUGUI rankSubLabel;
        private Transform mergeGrid;
        private RectTransform contentPanel;
        private CanvasGroup contentCG;
        private bool isBuilt;

        private void OnEnable()
        {
            if (!isBuilt) { BuildUI(); isBuilt = true; }
            Populate();
            StartCoroutine(EntranceAnimation());
        }

        private IEnumerator EntranceAnimation()
        {
            if (contentPanel == null || contentCG == null) yield break;

            contentCG.alpha = 0f;
            contentPanel.localScale = Vector3.one * 0.96f;

            float duration = 0.3f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - (1f - t) * (1f - t); // ease out quad
                contentCG.alpha = eased;
                contentPanel.localScale = Vector3.Lerp(Vector3.one * 0.96f, Vector3.one, eased);
                yield return null;
            }

            contentCG.alpha = 1f;
            contentPanel.localScale = Vector3.one;
        }

        public void Populate()
        {
            if (scoreValue != null)
                scoreValue.text = GameSession.TodayScore.ToString("N0");

            if (rankLabel != null)
                rankLabel.text = GameSession.ResultRank > 0 ? $"#{GameSession.ResultRank}" : "#—";

            if (rankSubLabel != null)
            {
                int total = GameSession.ResultTotalPlayers;
                rankSubLabel.text = total > 0
                    ? $"of {total} players today"
                    : "ranking...";
            }

            PopulateMergeGrid();
        }

        private void BuildUI()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();

            // Full-screen backdrop
            var backdrop = gameObject.GetComponent<Image>();
            if (backdrop == null) backdrop = gameObject.AddComponent<Image>();
            backdrop.color = OC.overlayDark;

            // Content panel (centered)
            var panel = OvertoneUI.CreateUIObject("ContentPanel", transform);
            contentPanel = panel.GetComponent<RectTransform>();
            contentCG = panel.AddComponent<CanvasGroup>();
            var panelRT = contentPanel;
            panelRT.anchorMin = new Vector2(0, 0);
            panelRT.anchorMax = new Vector2(1, 1);
            panelRT.offsetMin = new Vector2(28, 80);
            panelRT.offsetMax = new Vector2(-28, -80);

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 8;

            // Flexible top spacer to center content
            var topSpacer = OvertoneUI.CreateUIObject("TopFlex", panel.transform);
            topSpacer.AddComponent<LayoutElement>().flexibleHeight = 1;

            // "FINAL SCORE" label
            var finalLabel = OvertoneUI.CreateLabel(panel.transform, "FINAL SCORE",
                OvertoneUI.PressStart2P, OFont.labelSm, OC.muted, "FinalScoreLabel");
            finalLabel.characterSpacing = 3;
            finalLabel.alignment = TextAlignmentOptions.Center;
            finalLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 14;

            // Score value
            scoreValue = OvertoneUI.CreateLabel(panel.transform, "0",
                OvertoneUI.DMMono, OFont.score, OC.cyan, "ScoreValue");
            scoreValue.alignment = TextAlignmentOptions.Center;
            scoreValue.fontStyle = FontStyles.Bold;
            scoreValue.gameObject.AddComponent<LayoutElement>().preferredHeight = 64;

            // Rank badge
            BuildRankBadge(panel.transform);

            // Spacer
            AddSpacer(panel.transform, 8);

            // Merge grid card
            BuildMergeGridCard(panel.transform);

            // Spacer
            AddSpacer(panel.transform, 8);

            // Button row
            BuildButtonRow(panel.transform);

            // Flexible bottom spacer
            var botSpacer = OvertoneUI.CreateUIObject("BotFlex", panel.transform);
            botSpacer.AddComponent<LayoutElement>().flexibleHeight = 1;
        }

        private void BuildRankBadge(Transform parent)
        {
            var badge = OvertoneUI.CreateUIObject("RankBadge", parent);
            var badgeImg = badge.AddComponent<Image>();
            badgeImg.sprite = Visual.PixelUIGenerator.GetRoundedRect9Slice();
            badgeImg.type = Image.Type.Sliced;
            badgeImg.color = OC.A(OC.cyan, 0.12f);

            var hlg = badge.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.padding = new RectOffset(16, 16, 7, 7);
            badge.AddComponent<LayoutElement>().preferredHeight = 32;

            rankLabel = OvertoneUI.CreateLabel(badge.transform, "#—",
                OvertoneUI.PressStart2P, OFont.labelSm, OC.cyan, "RankLabel");
            rankLabel.characterSpacing = 1;

            rankSubLabel = OvertoneUI.CreateLabel(badge.transform, "ranking...",
                OvertoneUI.DMMono, OFont.bodySm, OC.muted, "RankSubLabel");
        }

        private void BuildMergeGridCard(Transform parent)
        {
            var card = OvertoneUI.CreateCard(parent, "MergeCard");
            var cardVLG = card.AddComponent<VerticalLayoutGroup>();
            cardVLG.childControlWidth = true;
            cardVLG.childControlHeight = false;
            cardVLG.childForceExpandWidth = true;
            cardVLG.spacing = 12;
            cardVLG.padding = new RectOffset(12, 12, 14, 14);

            // MERGES label
            var mergesLabel = OvertoneUI.CreateLabel(card.transform, "MERGES",
                OvertoneUI.PressStart2P, OFont.labelXs, OC.dim, "MergesLabel");
            mergesLabel.characterSpacing = 1;
            mergesLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 12;

            // Grid container (flow layout — use GridLayoutGroup)
            var gridGO = OvertoneUI.CreateUIObject("BallGrid", card.transform);
            var grid = gridGO.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(50, 55);
            grid.spacing = new Vector2(6, 6);
            grid.constraint = GridLayoutGroup.Constraint.Flexible;
            grid.childAlignment = TextAnchor.UpperCenter;
            mergeGrid = gridGO.transform;
        }

        private void PopulateMergeGrid()
        {
            if (mergeGrid == null || tierConfig == null) return;

            // Clear existing
            foreach (Transform child in mergeGrid)
                Destroy(child.gameObject);

            int[] counts = GameSession.MergeCounts ?? new int[11];

            for (int tier = 0; tier < 11; tier++)
            {
                var data = tierConfig.GetTier(tier);
                if (data == null) continue;

                int count = tier < counts.Length ? counts[tier] : 0;
                Color ballColor = data.color;

                var cell = OvertoneUI.CreateUIObject($"Merge{tier}", mergeGrid);
                var cellVLG = cell.AddComponent<VerticalLayoutGroup>();
                cellVLG.childAlignment = TextAnchor.MiddleCenter;
                cellVLG.spacing = 2;
                cellVLG.childControlWidth = true;
                cellVLG.childControlHeight = false;
                cellVLG.childForceExpandWidth = true;

                // Dim if zero
                if (count == 0)
                {
                    var cg = cell.AddComponent<CanvasGroup>();
                    cg.alpha = 0.25f;
                }

                // Ball icon (scaled down)
                var ballGO = OvertoneUI.CreateUIObject("Ball", cell.transform);
                float displaySize = Mathf.Max(16f, Mathf.Floor(data.radius * 2f * 50f * 0.38f));
                displaySize = Mathf.Clamp(displaySize, 16f, 36f);
                ballGO.AddComponent<LayoutElement>().preferredHeight = displaySize;
                var ballImg = ballGO.AddComponent<Image>();
                if (data.sprite != null)
                {
                    ballImg.sprite = data.sprite;
                    ballImg.color = Color.white;
                }
                else
                {
                    ballImg.sprite = Visual.PixelUIGenerator.GetRoundedRect9Slice();
                    ballImg.type = Image.Type.Sliced;
                    ballImg.color = ballColor;
                }
                ballImg.preserveAspect = true;

                // Count label
                var countTMP = OvertoneUI.CreateLabel(cell.transform,
                    count > 0 ? $"\u00D7{count}" : "\u2014",
                    OvertoneUI.PressStart2P, OFont.labelXxs,
                    count > 0 ? ballColor : OC.dim, "Count");
                countTMP.alignment = TextAlignmentOptions.Center;
                countTMP.gameObject.AddComponent<LayoutElement>().preferredHeight = 10;
            }
        }

        private void BuildButtonRow(Transform parent)
        {
            var row = OvertoneUI.CreateUIObject("ButtonRow", parent);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            row.AddComponent<LayoutElement>().preferredHeight = 52;

            // Done button
            var (doneGO, doneLabel) = OvertoneUI.CreateGhostButton(row.transform, "DONE", 52, "DoneButton");
            doneLabel.fontSize = OFont.label;
            doneGO.GetComponent<LayoutElement>().flexibleWidth = 1;
            doneGO.GetComponent<Button>().onClick.AddListener(OnDoneClicked);

            // Share button
            var (shareGO, shareLabel) = OvertoneUI.CreatePrimaryButton(row.transform, "SHARE", 52, "ShareButton");
            shareGO.GetComponent<LayoutElement>().flexibleWidth = 2;
            shareGO.GetComponent<Button>().onClick.AddListener(OnShareClicked);
        }

        private void OnDoneClicked()
        {
            // Navigate to HomePlayed
            if (ScreenManager.Instance != null)
                ScreenManager.Instance.NavigateTo(Screen.HomePlayed);
        }

        private void OnShareClicked()
        {
            if (ScreenManager.Instance != null)
                ScreenManager.Instance.NavigateTo(Screen.ShareSheet);
        }

        private void AddSpacer(Transform parent, float height)
        {
            var s = OvertoneUI.CreateUIObject("Spacer", parent);
            s.AddComponent<LayoutElement>().preferredHeight = height;
        }
    }
}
