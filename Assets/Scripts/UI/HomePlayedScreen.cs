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

            // SHARE — primary cyan button
            var (shareGO, shareLabel) = MurgeUI.CreatePrimaryButton(actionRow.transform, "SHARE", 44, "ShareButton");
            shareGO.GetComponent<LayoutElement>().flexibleWidth = 1;
            shareGO.GetComponent<Button>().onClick.AddListener(OnShareClicked);

            // PLAY AGAIN — smooth border, bg matches screen
            var replayGO = MurgeUI.CreateUIObject("PlayAgainButton", actionRow.transform);
            // Border
            var rpBorderGO = MurgeUI.CreateUIObject("Border", replayGO.transform);
            MurgeUI.StretchFill(rpBorderGO.GetComponent<RectTransform>());
            var rpBdrImg = rpBorderGO.AddComponent<Image>();
            rpBdrImg.sprite = GetSmootherRoundedRect();
            rpBdrImg.type = Image.Type.Sliced;
            rpBdrImg.color = OC.border;
            rpBdrImg.raycastTarget = false;
            // Fill (inset, matches bg)
            var rpFillGO = MurgeUI.CreateUIObject("Fill", replayGO.transform);
            var rpFillRT = rpFillGO.GetComponent<RectTransform>();
            rpFillRT.anchorMin = Vector2.zero; rpFillRT.anchorMax = Vector2.one;
            rpFillRT.offsetMin = new Vector2(1, 1); rpFillRT.offsetMax = new Vector2(-1, -1);
            var rpFillImg = rpFillGO.AddComponent<Image>();
            rpFillImg.sprite = GetSmootherRoundedRect();
            rpFillImg.type = Image.Type.Sliced;
            rpFillImg.color = OC.bg;
            rpFillImg.raycastTarget = false;
            // Hit area for button
            var replayImg = replayGO.AddComponent<Image>();
            replayImg.color = Color.clear;
            var replayBtn = replayGO.AddComponent<Button>();
            replayBtn.targetGraphic = rpBdrImg; // use border as target so it gets click
            replayBtn.onClick.AddListener(OnPlayAgainClicked);
            var replayLE = replayGO.AddComponent<LayoutElement>();
            replayLE.flexibleWidth = 1;
            // Label
            var replayLabelGO = MurgeUI.CreateUIObject("Label", replayGO.transform);
            var replayLabelTMP = replayLabelGO.AddComponent<TextMeshProUGUI>();
            replayLabelTMP.text = "PLAY AGAIN";
            replayLabelTMP.font = MurgeUI.PressStart2P;
            replayLabelTMP.fontSize = OFont.label;
            replayLabelTMP.color = OC.muted;
            replayLabelTMP.characterSpacing = 1;
            replayLabelTMP.alignment = TextAlignmentOptions.Center;
            replayLabelTMP.raycastTarget = false;
            MurgeUI.StretchFill(replayLabelGO.GetComponent<RectTransform>());

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
