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
        private TextMeshProUGUI scoredTMP;

        protected override void BuildMiddleSection(Transform parent)
        {
            // Divider above stats
            var topDiv = OvertoneUI.CreateUIObject("StatsTopDiv", parent);
            topDiv.AddComponent<Image>().color = OC.border;
            topDiv.GetComponent<Image>().raycastTarget = false;
            var tdLE = topDiv.AddComponent<LayoutElement>();
            tdLE.preferredHeight = 1; tdLE.minHeight = 1;

            // Stats row — manual positioning for precise alignment
            var statsRow = OvertoneUI.CreateUIObject("StatsRow", parent);
            var statsLE = statsRow.AddComponent<LayoutElement>();
            statsLE.preferredHeight = 70; statsLE.minHeight = 70;

            // TODAY (left half)
            var todayBlock = OvertoneUI.CreateUIObject("TodayBlock", statsRow.transform);
            var tbRT = todayBlock.GetComponent<RectTransform>();
            tbRT.anchorMin = new Vector2(0, 0); tbRT.anchorMax = new Vector2(0.5f, 1);
            tbRT.offsetMin = Vector2.zero; tbRT.offsetMax = Vector2.zero;

            var todayValGO = OvertoneUI.CreateUIObject("TodayValue", todayBlock.transform);
            var tvRT = todayValGO.GetComponent<RectTransform>();
            tvRT.anchorMin = new Vector2(0, 0.4f); tvRT.anchorMax = new Vector2(1, 1);
            tvRT.offsetMin = Vector2.zero; tvRT.offsetMax = Vector2.zero;
            todayValue = todayValGO.AddComponent<TextMeshProUGUI>();
            todayValue.text = "0";
            todayValue.font = OvertoneUI.DMMono;
            todayValue.fontSize = 22;
            todayValue.fontStyle = TMPro.FontStyles.Bold;
            todayValue.color = OC.white;
            todayValue.alignment = TMPro.TextAlignmentOptions.Center;
            todayValue.raycastTarget = false;

            var todayLabelGO = OvertoneUI.CreateUIObject("TodayLabel", todayBlock.transform);
            var tlRT = todayLabelGO.GetComponent<RectTransform>();
            tlRT.anchorMin = new Vector2(0, 0); tlRT.anchorMax = new Vector2(1, 0.4f);
            tlRT.offsetMin = Vector2.zero; tlRT.offsetMax = Vector2.zero;
            var todayLabelTMP = todayLabelGO.AddComponent<TextMeshProUGUI>();
            todayLabelTMP.text = "TODAY";
            todayLabelTMP.font = OvertoneUI.PressStart2P;
            todayLabelTMP.fontSize = OFont.labelSm;
            todayLabelTMP.color = OC.muted;
            todayLabelTMP.alignment = TMPro.TextAlignmentOptions.Center;
            todayLabelTMP.raycastTarget = false;

            // Vertical divider (center)
            var vDiv = OvertoneUI.CreateUIObject("VDivider", statsRow.transform);
            var vdRT = vDiv.GetComponent<RectTransform>();
            vdRT.anchorMin = new Vector2(0.5f, 0.15f); vdRT.anchorMax = new Vector2(0.5f, 0.85f);
            vdRT.pivot = new Vector2(0.5f, 0.5f);
            vdRT.sizeDelta = new Vector2(1, 0);
            vdRT.anchoredPosition = Vector2.zero;
            vDiv.AddComponent<Image>().color = OC.border;
            vDiv.GetComponent<Image>().raycastTarget = false;

            // BEST (right half)
            var bestBlock = OvertoneUI.CreateUIObject("BestBlock", statsRow.transform);
            var bbRT = bestBlock.GetComponent<RectTransform>();
            bbRT.anchorMin = new Vector2(0.5f, 0); bbRT.anchorMax = new Vector2(1, 1);
            bbRT.offsetMin = Vector2.zero; bbRT.offsetMax = Vector2.zero;

            var bestValGO = OvertoneUI.CreateUIObject("BestValue", bestBlock.transform);
            var bvRT = bestValGO.GetComponent<RectTransform>();
            bvRT.anchorMin = new Vector2(0, 0.4f); bvRT.anchorMax = new Vector2(1, 1);
            bvRT.offsetMin = Vector2.zero; bvRT.offsetMax = Vector2.zero;
            bestValue = bestValGO.AddComponent<TextMeshProUGUI>();
            bestValue.text = "0";
            bestValue.font = OvertoneUI.DMMono;
            bestValue.fontSize = 22;
            bestValue.fontStyle = TMPro.FontStyles.Bold;
            bestValue.color = OC.cyan;
            bestValue.alignment = TMPro.TextAlignmentOptions.Center;
            bestValue.raycastTarget = false;

            var bestLabelGO = OvertoneUI.CreateUIObject("BestLabel", bestBlock.transform);
            var blRT = bestLabelGO.GetComponent<RectTransform>();
            blRT.anchorMin = new Vector2(0, 0); blRT.anchorMax = new Vector2(1, 0.4f);
            blRT.offsetMin = Vector2.zero; blRT.offsetMax = Vector2.zero;
            var bestLabelTMP = bestLabelGO.AddComponent<TextMeshProUGUI>();
            bestLabelTMP.text = "BEST";
            bestLabelTMP.font = OvertoneUI.PressStart2P;
            bestLabelTMP.fontSize = OFont.labelSm;
            bestLabelTMP.color = OC.muted;
            bestLabelTMP.alignment = TMPro.TextAlignmentOptions.Center;
            bestLabelTMP.raycastTarget = false;

            // Divider below stats
            var botDiv = OvertoneUI.CreateUIObject("StatsBotDiv", parent);
            botDiv.AddComponent<Image>().color = OC.border;
            botDiv.GetComponent<Image>().raycastTarget = false;
            var bdLE = botDiv.AddComponent<LayoutElement>();
            bdLE.preferredHeight = 1; bdLE.minHeight = 1;
        }

        protected override void BuildCTABlock(Transform parent)
        {
            // Scored badge — smooth border, transparent inside
            var badge = OvertoneUI.CreateUIObject("ScoredBadge", parent);
            var badgeLE = badge.AddComponent<LayoutElement>();
            badgeLE.preferredHeight = 30;
            badgeLE.minHeight = 30;

            // Border (slightly larger, behind)
            var badgeBorderGO = OvertoneUI.CreateUIObject("Border", badge.transform);
            var bdrRT = badgeBorderGO.GetComponent<RectTransform>();
            bdrRT.anchorMin = Vector2.zero; bdrRT.anchorMax = Vector2.one;
            bdrRT.offsetMin = Vector2.zero; bdrRT.offsetMax = Vector2.zero;
            var bdrImg = badgeBorderGO.AddComponent<Image>();
            bdrImg.sprite = GetSmootherRoundedRect();
            bdrImg.type = Image.Type.Sliced;
            bdrImg.color = OC.border;
            bdrImg.raycastTarget = false;
            // Fill (inset 1px, matches screen bg = transparent look)
            var badgeFillGO = OvertoneUI.CreateUIObject("Fill", badge.transform);
            var fillRT = badgeFillGO.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = new Vector2(1, 1); fillRT.offsetMax = new Vector2(-1, -1);
            var fillImg = badgeFillGO.AddComponent<Image>();
            fillImg.sprite = GetSmootherRoundedRect();
            fillImg.type = Image.Type.Sliced;
            fillImg.color = OC.bg; // matches screen background
            fillImg.raycastTarget = false;

            // Lock icon sprite (left of text)
            var lockGO = OvertoneUI.CreateUIObject("LockIcon", badge.transform);
            var lockRT = lockGO.GetComponent<RectTransform>();
            lockRT.anchorMin = new Vector2(0.5f, 0.5f);
            lockRT.anchorMax = new Vector2(0.5f, 0.5f);
            lockRT.pivot = new Vector2(0.5f, 0.5f);
            lockRT.anchoredPosition = new Vector2(-62, 0);
            lockRT.sizeDelta = new Vector2(10, 12);
            var lockImg = lockGO.AddComponent<Image>();
            lockImg.sprite = CreateLockSprite();
            lockImg.preserveAspect = true;
            lockImg.color = OC.muted;
            lockImg.raycastTarget = false;

            // SCORED + score text
            var contentGO = OvertoneUI.CreateUIObject("Content", badge.transform);
            OvertoneUI.StretchFill(contentGO.GetComponent<RectTransform>());
            var contentTMP = contentGO.AddComponent<TextMeshProUGUI>();
            string cyanHex = ColorUtility.ToHtmlStringRGB(OC.cyan);
            contentTMP.text = $"SCORED  <color=#{cyanHex}>0</color>";
            contentTMP.font = OvertoneUI.PressStart2P;
            contentTMP.fontSize = 9;
            contentTMP.characterSpacing = 2;
            contentTMP.color = OC.muted;
            contentTMP.alignment = TextAlignmentOptions.Center;
            contentTMP.verticalAlignment = VerticalAlignmentOptions.Middle;
            contentTMP.richText = true;
            contentTMP.raycastTarget = false;
            scoredTMP = contentTMP;

            // Action row: Share (primary) + Play Again (ghost/outline)
            var actionRow = OvertoneUI.CreateUIObject("ActionRow", parent);
            var actionHLG = actionRow.AddComponent<HorizontalLayoutGroup>();
            actionHLG.spacing = 8;
            actionHLG.childAlignment = TextAnchor.MiddleCenter;
            actionHLG.childControlWidth = true;
            actionHLG.childControlHeight = true;
            actionHLG.childForceExpandWidth = true;
            actionHLG.padding = new RectOffset(0, 0, 0, 0);
            var arLE = actionRow.AddComponent<LayoutElement>();
            arLE.preferredHeight = 28;
            arLE.minHeight = 28;

            // SHARE — primary cyan button
            var (shareGO, shareLabel) = OvertoneUI.CreatePrimaryButton(actionRow.transform, "SHARE", 28, "ShareButton");
            shareGO.GetComponent<LayoutElement>().flexibleWidth = 1;
            shareGO.GetComponent<Button>().onClick.AddListener(OnShareClicked);

            // PLAY AGAIN — smooth border, bg matches screen
            var replayGO = OvertoneUI.CreateUIObject("PlayAgainButton", actionRow.transform);
            // Border
            var rpBorderGO = OvertoneUI.CreateUIObject("Border", replayGO.transform);
            OvertoneUI.StretchFill(rpBorderGO.GetComponent<RectTransform>());
            var rpBdrImg = rpBorderGO.AddComponent<Image>();
            rpBdrImg.sprite = GetSmootherRoundedRect();
            rpBdrImg.type = Image.Type.Sliced;
            rpBdrImg.color = OC.border;
            rpBdrImg.raycastTarget = false;
            // Fill (inset, matches bg)
            var rpFillGO = OvertoneUI.CreateUIObject("Fill", replayGO.transform);
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
            var replayLabelGO = OvertoneUI.CreateUIObject("Label", replayGO.transform);
            var replayLabelTMP = replayLabelGO.AddComponent<TextMeshProUGUI>();
            replayLabelTMP.text = "PLAY AGAIN";
            replayLabelTMP.font = OvertoneUI.PressStart2P;
            replayLabelTMP.fontSize = OFont.label;
            replayLabelTMP.color = OC.muted;
            replayLabelTMP.characterSpacing = 1;
            replayLabelTMP.alignment = TextAlignmentOptions.Center;
            replayLabelTMP.raycastTarget = false;
            OvertoneUI.StretchFill(replayLabelGO.GetComponent<RectTransform>());

            // Hint
            var hint = OvertoneUI.CreateLabel(parent, "only first score of the day is counted",
                OvertoneUI.DMMono, OFont.caption, OC.dim, "HintLabel");
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

            // Update scored badge
            if (scoredTMP != null)
            {
                string cyanHex = ColorUtility.ToHtmlStringRGB(OC.cyan);
                scoredTMP.text = $"SCORED  <color=#{cyanHex}>{todayScore.ToString("N0")}</color>";
            }

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

        private static Sprite _lockSprite;
        private static Sprite CreateLockSprite()
        {
            if (_lockSprite != null) return _lockSprite;

            // 10x12 pixel-art padlock
            int w = 10, h = 12;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;

            // Shackle (top arc, rows 8-11)
            for (int y = 8; y < 12; y++)
                for (int x = 2; x < 8; x++)
                {
                    bool outer = (x == 2 || x == 7 || y == 11);
                    bool inner = (x >= 4 && x <= 5 && y <= 9);
                    if (outer || (y == 11 && x >= 2 && x <= 7))
                        px[y * w + x] = Color.white;
                    if (inner) px[y * w + x] = Color.clear;
                }
            // Top of shackle
            for (int x = 3; x <= 6; x++) px[11 * w + x] = Color.white;
            for (int x = 2; x <= 7; x++) px[10 * w + x] = Color.white;
            px[9 * w + 2] = Color.white; px[9 * w + 3] = Color.white;
            px[9 * w + 6] = Color.white; px[9 * w + 7] = Color.white;
            px[8 * w + 2] = Color.white; px[8 * w + 3] = Color.white;
            px[8 * w + 6] = Color.white; px[8 * w + 7] = Color.white;

            // Body (rectangle, rows 0-7)
            for (int y = 0; y <= 7; y++)
                for (int x = 1; x <= 8; x++)
                    px[y * w + x] = Color.white;

            // Keyhole (small dark spot in body center)
            px[4 * w + 4] = Color.clear; px[4 * w + 5] = Color.clear;
            px[3 * w + 4] = Color.clear; px[3 * w + 5] = Color.clear;
            px[2 * w + 5] = Color.clear;

            tex.SetPixels(px);
            tex.Apply();

            _lockSprite = Sprite.Create(tex, new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f), 1f);
            _lockSprite.name = "LockIcon";
            return _lockSprite;
        }

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
