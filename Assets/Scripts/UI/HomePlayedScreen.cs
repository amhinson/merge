using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using MergeGame.Core;
using MergeGame.Data;
using MergeGame.Backend;

namespace MergeGame.UI
{
    /// <summary>
    /// Home screen when the player HAS played today.
    /// Shows stats row, scored badge, Share + Play Again buttons, and YourRank in leaderboard.
    /// </summary>
    public class HomePlayedScreen : HomeScreen
    {
        private TextMeshProUGUI todayValue;
        private TextMeshProUGUI bestValue;
        private TextMeshProUGUI scoredValue;

        protected override void BuildMiddleSection(Transform parent)
        {
            // Divider above stats
            var topDiv = MurgeUI.CreateUIObject("StatsTopDiv", parent);
            topDiv.AddComponent<Image>().color = OC.border;
            topDiv.GetComponent<Image>().raycastTarget = false;
            var tdLE = topDiv.AddComponent<LayoutElement>();
            tdLE.preferredHeight = 1; tdLE.minHeight = 1;

            // Stats row — manual positioning for precise alignment
            var statsRow = MurgeUI.CreateUIObject("StatsRow", parent);
            var statsLE = statsRow.AddComponent<LayoutElement>();
            statsLE.preferredHeight = 70; statsLE.minHeight = 70;

            // TODAY (left half)
            var todayBlock = MurgeUI.CreateUIObject("TodayBlock", statsRow.transform);
            var tbRT = todayBlock.GetComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(0, 0); tbRT.anchorMax = new Vector2(0.5f, 1);
            tbRT.offsetMin = Vector2.zero; tbRT.offsetMax = Vector2.zero;

            var todayValGO = MurgeUI.CreateUIObject("TodayValue", todayBlock.transform);
            var tvRT = todayValGO.GetComponent<RectTransform>();
            tvRT.anchorMin = new Vector2(0, 0.4f); tvRT.anchorMax = new Vector2(1, 1);
            tvRT.offsetMin = Vector2.zero; tvRT.offsetMax = Vector2.zero;
            todayValue = todayValGO.AddComponent<TextMeshProUGUI>();
            todayValue.text = "0";
            todayValue.font = MurgeUI.DMMono;
            todayValue.fontSize = 22;
            todayValue.fontStyle = TMPro.FontStyles.Bold;
            todayValue.color = OC.white;
            todayValue.alignment = TMPro.TextAlignmentOptions.Center;
            todayValue.raycastTarget = false;

            var todayLabelGO = MurgeUI.CreateUIObject("TodayLabel", todayBlock.transform);
            var tlRT = todayLabelGO.GetComponent<RectTransform>();
            tlRT.anchorMin = new Vector2(0, 0); tlRT.anchorMax = new Vector2(1, 0.4f);
            tlRT.offsetMin = Vector2.zero; tlRT.offsetMax = Vector2.zero;
            var todayLabelTMP = todayLabelGO.AddComponent<TextMeshProUGUI>();
            todayLabelTMP.text = "TODAY";
            todayLabelTMP.font = MurgeUI.PressStart2P;
            todayLabelTMP.fontSize = OFont.labelSm;
            todayLabelTMP.color = OC.muted;
            todayLabelTMP.alignment = TMPro.TextAlignmentOptions.Center;
            todayLabelTMP.raycastTarget = false;

            // Vertical divider (center)
            var vDiv = MurgeUI.CreateUIObject("VDivider", statsRow.transform);
            var vdRT = vDiv.GetComponent<RectTransform>();
            vdRT.anchorMin = new Vector2(0.5f, 0.15f); vdRT.anchorMax = new Vector2(0.5f, 0.85f);
            vdRT.pivot = new Vector2(0.5f, 0.5f);
            vdRT.sizeDelta = new Vector2(1, 0);
            vdRT.anchoredPosition = Vector2.zero;
            vDiv.AddComponent<Image>().color = OC.border;
            vDiv.GetComponent<Image>().raycastTarget = false;

            // BEST (right half)
            var bestBlock = MurgeUI.CreateUIObject("BestBlock", statsRow.transform);
            var bbRT = bestBlock.GetComponent<RectTransform>();
            bbRT.anchorMin = new Vector2(0.5f, 0); bbRT.anchorMax = new Vector2(1, 1);
            bbRT.offsetMin = Vector2.zero; bbRT.offsetMax = Vector2.zero;

            var bestValGO = MurgeUI.CreateUIObject("BestValue", bestBlock.transform);
            var bvRT = bestValGO.GetComponent<RectTransform>();
            bvRT.anchorMin = new Vector2(0, 0.4f); bvRT.anchorMax = new Vector2(1, 1);
            bvRT.offsetMin = Vector2.zero; bvRT.offsetMax = Vector2.zero;
            bestValue = bestValGO.AddComponent<TextMeshProUGUI>();
            bestValue.text = "0";
            bestValue.font = MurgeUI.DMMono;
            bestValue.fontSize = 22;
            bestValue.fontStyle = TMPro.FontStyles.Bold;
            bestValue.color = OC.cyan;
            bestValue.alignment = TMPro.TextAlignmentOptions.Center;
            bestValue.raycastTarget = false;

            var bestLabelGO = MurgeUI.CreateUIObject("BestLabel", bestBlock.transform);
            var blRT = bestLabelGO.GetComponent<RectTransform>();
            blRT.anchorMin = new Vector2(0, 0); blRT.anchorMax = new Vector2(1, 0.4f);
            blRT.offsetMin = Vector2.zero; blRT.offsetMax = Vector2.zero;
            var bestLabelTMP = bestLabelGO.AddComponent<TextMeshProUGUI>();
            bestLabelTMP.text = "BEST";
            bestLabelTMP.font = MurgeUI.PressStart2P;
            bestLabelTMP.fontSize = OFont.labelSm;
            bestLabelTMP.color = OC.muted;
            bestLabelTMP.alignment = TMPro.TextAlignmentOptions.Center;
            bestLabelTMP.raycastTarget = false;

            // Divider below stats
            var botDiv = MurgeUI.CreateUIObject("StatsBotDiv", parent);
            botDiv.AddComponent<Image>().color = OC.border;
            botDiv.GetComponent<Image>().raycastTarget = false;
            var bdLE = botDiv.AddComponent<LayoutElement>();
            bdLE.preferredHeight = 1; bdLE.minHeight = 1;
        }

        protected override void BuildCTABlock(Transform parent)
        {
            // Action row: Share (primary) + Play Again (ghost/outline)
            var actionRow = MurgeUI.CreateUIObject("ActionRow", parent);
            var actionHLG = actionRow.AddComponent<HorizontalLayoutGroup>();
            actionHLG.spacing = 8;
            actionHLG.childAlignment = TextAnchor.MiddleCenter;
            actionHLG.childControlWidth = true;
            actionHLG.childControlHeight = true;
            actionHLG.childForceExpandWidth = true;
            actionHLG.padding = new RectOffset(0, 0, 0, 0);
            var arLE = actionRow.AddComponent<LayoutElement>();
            arLE.preferredHeight = 44;
            arLE.minHeight = 44;

            // PLAY AGAIN — primary cyan button (2/3 width)
            var (replayGO, replayLabel) = MurgeUI.CreatePrimaryButton(actionRow.transform, "PLAY AGAIN", 44, "PlayAgainButton");
            replayGO.GetComponent<LayoutElement>().flexibleWidth = 2;
            replayGO.GetComponent<Button>().onClick.AddListener(OnPlayAgainClicked);

            // SHARE — ghost/outline button (1/3 width)
            var shareGO = MurgeUI.CreateUIObject("ShareButton", actionRow.transform);
            // Border
            var shBorderGO = MurgeUI.CreateUIObject("Border", shareGO.transform);
            MurgeUI.StretchFill(shBorderGO.GetComponent<RectTransform>());
            var shBdrImg = shBorderGO.AddComponent<Image>();
            shBdrImg.sprite = GetSmootherRoundedRect();
            shBdrImg.type = Image.Type.Sliced;
            shBdrImg.color = OC.border;
            shBdrImg.raycastTarget = false;
            // Fill (inset, matches bg)
            var shFillGO = MurgeUI.CreateUIObject("Fill", shareGO.transform);
            var shFillRT = shFillGO.GetComponent<RectTransform>();
            shFillRT.anchorMin = Vector2.zero; shFillRT.anchorMax = Vector2.one;
            shFillRT.offsetMin = new Vector2(1, 1); shFillRT.offsetMax = new Vector2(-1, -1);
            var shFillImg = shFillGO.AddComponent<Image>();
            shFillImg.sprite = GetSmootherRoundedRect();
            shFillImg.type = Image.Type.Sliced;
            shFillImg.color = OC.bg;
            shFillImg.raycastTarget = false;
            // Hit area for button
            var shareImg = shareGO.AddComponent<Image>();
            shareImg.color = Color.clear;
            var shareBtn = shareGO.AddComponent<Button>();
            shareBtn.targetGraphic = shBdrImg;
            shareBtn.onClick.AddListener(OnShareClicked);
            var shareLE = shareGO.AddComponent<LayoutElement>();
            shareLE.flexibleWidth = 1;
            // Label
            var shareLabelGO = MurgeUI.CreateUIObject("Label", shareGO.transform);
            var shareLabelTMP = shareLabelGO.AddComponent<TextMeshProUGUI>();
            shareLabelTMP.text = "SHARE";
            shareLabelTMP.font = MurgeUI.PressStart2P;
            shareLabelTMP.fontSize = OFont.label;
            shareLabelTMP.color = OC.muted;
            shareLabelTMP.characterSpacing = 1;
            shareLabelTMP.alignment = TextAlignmentOptions.Center;
            shareLabelTMP.raycastTarget = false;
            MurgeUI.StretchFill(shareLabelGO.GetComponent<RectTransform>());

            // Hint
            var hint = MurgeUI.CreateLabel(parent, "only first score of the day is counted",
                MurgeUI.DMMono, OFont.caption, OC.dim, "HintLabel");
            hint.alignment = TextAlignmentOptions.Center;
            hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 14;
        }

        public override void Refresh()
        {
            base.Refresh();
            RefreshStats();
        }

        private void RefreshStats()
        {
            // TodayScore might be 0 if profile fetch didn't return it — use ScoreManager as fallback
            int todayScore = GameSession.TodayScore;
            if (todayScore <= 0 && ScoreManager.Instance != null)
                todayScore = ScoreManager.Instance.HighScore;

            if (todayValue != null)
                todayValue.text = todayScore.ToString("N0");

            // Best score
            if (bestValue != null)
            {
                int best = GameSession.TodayScore;
                if (ScoreManager.Instance != null)
                    best = Mathf.Max(best, ScoreManager.Instance.HighScore);
                bestValue.text = best.ToString("N0");
            }
        }

        // Base class PopulateLeaderboardRows already handles the YourRank row


        private void OnShareClicked()
        {
            if (ShareManager.Instance != null)
                ShareManager.Instance.ShareResult();
        }

        private void OnPlayAgainClicked()
        {
            GameSession.IsPractice = true;
            if (GameManager.Instance != null)
                GameManager.Instance.OnPlayButtonPressed();
        }
    }
}
