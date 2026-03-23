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
            // Stats row with border top/bottom
            OvertoneUI.CreateDivider(parent, "StatsTop");

            var statsRow = OvertoneUI.CreateUIObject("StatsRow", parent);
            var hlg = statsRow.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.spacing = 0;
            hlg.padding = new RectOffset(24, 24, 0, 0);
            statsRow.AddComponent<LayoutElement>().preferredHeight = 52;

            var (todayGO, todayVal, _) = OvertoneUI.CreateStatCell(
                statsRow.transform, "TODAY", "0", OC.white, true);
            todayGO.GetComponent<LayoutElement>().flexibleWidth = 1;
            todayValue = todayVal;

            var (bestGO, bestVal, _) = OvertoneUI.CreateStatCell(
                statsRow.transform, "BEST", "0", OC.cyan, false);
            bestGO.GetComponent<LayoutElement>().flexibleWidth = 1;
            bestValue = bestVal;

            OvertoneUI.CreateDivider(parent, "StatsBottom");
        }

        protected override void BuildCTABlock(Transform parent)
        {
            // Scored badge
            var badge = OvertoneUI.CreateCard(parent, "ScoredBadge");
            var badgeHLG = badge.AddComponent<HorizontalLayoutGroup>();
            badgeHLG.childAlignment = TextAnchor.MiddleCenter;
            badgeHLG.spacing = 10;
            badgeHLG.childControlWidth = false;
            badgeHLG.childControlHeight = true;
            badgeHLG.childForceExpandWidth = false;
            badgeHLG.padding = new RectOffset(14, 14, 11, 11);
            var badgeLE = badge.GetComponent<LayoutElement>();
            if (badgeLE == null) badgeLE = badge.AddComponent<LayoutElement>();
            badgeLE.preferredHeight = 40;

            OvertoneUI.CreateLabel(badge.transform, "\U0001F512",
                OvertoneUI.DMMono, OFont.body, OC.muted, "LockIcon");
            OvertoneUI.CreateLabel(badge.transform, "SCORED",
                OvertoneUI.PressStart2P, OFont.caption, OC.muted, "ScoredLabel")
                .characterSpacing = 2;
            scoredValue = OvertoneUI.CreateLabel(badge.transform, "0",
                OvertoneUI.DMMono, OFont.bodyLg, OC.cyan, "ScoreValue");

            // Action row: Share + Play Again
            var actionRow = OvertoneUI.CreateUIObject("ActionRow", parent);
            var actionHLG = actionRow.AddComponent<HorizontalLayoutGroup>();
            actionHLG.spacing = 8;
            actionHLG.childAlignment = TextAnchor.MiddleCenter;
            actionHLG.childControlWidth = false;
            actionHLG.childControlHeight = true;
            actionHLG.childForceExpandWidth = false;
            actionRow.AddComponent<LayoutElement>().preferredHeight = 52;

            var (shareGO, shareLabel) = OvertoneUI.CreatePrimaryButton(actionRow.transform, "SHARE", 52, "ShareButton");
            shareGO.GetComponent<LayoutElement>().flexibleWidth = 1;
            shareGO.GetComponent<Button>().onClick.AddListener(OnShareClicked);

            var (replayGO, replayLabel) = OvertoneUI.CreateGhostButton(actionRow.transform, "PLAY AGAIN", 52, "PlayAgainButton");
            replayLabel.fontSize = OFont.label;
            replayGO.GetComponent<LayoutElement>().flexibleWidth = 1;
            replayGO.GetComponent<Button>().onClick.AddListener(OnPlayAgainClicked);

            // Hint
            var hint = OvertoneUI.CreateLabel(parent, "only first score of the day is counted",
                OvertoneUI.DMMono, OFont.caption, OC.dim, "HintLabel");
            hint.alignment = TextAlignmentOptions.Center;
            hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 16;
        }

        public override void Refresh()
        {
            base.Refresh();
            RefreshStats();
        }

        private void RefreshStats()
        {
            if (todayValue != null)
                todayValue.text = GameSession.TodayScore.ToString("N0");
            if (scoredValue != null)
                scoredValue.text = GameSession.TodayScore.ToString("N0");

            // Best score — use high score from ScoreManager
            if (bestValue != null)
            {
                int best = GameSession.TodayScore;
                if (ScoreManager.Instance != null)
                    best = Mathf.Max(best, ScoreManager.Instance.HighScore);
                bestValue.text = best.ToString("N0");
            }
        }

        protected override void PopulateLeaderboardRows(List<LeaderboardEntry> entries)
        {
            base.PopulateLeaderboardRows(entries);

            // Add YourRank row at bottom
            if (leaderboardRowContainer == null) return;

            var (rowGO, rankTMP, nameTMP, scoreTMP) =
                OvertoneUI.CreateLeaderboardRow(leaderboardRowContainer, "YourRankRow");

            // Style as highlighted
            OvertoneUI.HighlightLeaderboardRow(rowGO, rankTMP, nameTMP, scoreTMP);

            // Add a subtle background + border
            var bg = rowGO.GetComponent<Image>();
            if (bg != null)
            {
                bg.sprite = Visual.PixelUIGenerator.GetRoundedRect9Slice();
                bg.type = Image.Type.Sliced;
                bg.color = OC.A(OC.cyan, 0.10f);
            }

            rankTMP.text = GameSession.ResultRank > 0 ? $"#{GameSession.ResultRank}" : "#—";
            nameTMP.text = GameSession.CurrentPlayer?.display_name ?? "YOU";
            scoreTMP.text = GameSession.TodayScore.ToString("N0");
        }

        private void OnShareClicked()
        {
            if (ScreenManager.Instance != null)
                ScreenManager.Instance.NavigateTo(Screen.ShareSheet);
        }

        private void OnPlayAgainClicked()
        {
            GameSession.IsPractice = true;
            if (GameManager.Instance != null)
                GameManager.Instance.OnPlayButtonPressed();
        }
    }
}
