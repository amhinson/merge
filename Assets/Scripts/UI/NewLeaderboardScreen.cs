using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using MergeGame.Core;
using MergeGame.Data;
using MergeGame.Backend;

namespace MergeGame.UI
{
    /// <summary>
    /// Leaderboard screen with day navigation (prev/next).
    /// Fetches and displays entries with current-player highlighting.
    /// </summary>
    public class NewLeaderboardScreen : MonoBehaviour
    {
        // State
        private int selectedDayNumber;
        private string selectedDateStr;

        // UI
        private TextMeshProUGUI dayNumberLabel;
        private TextMeshProUGUI dayDateLabel;
        private Button prevButton;
        private Button nextButton;
        private TextMeshProUGUI prevLabel;
        private TextMeshProUGUI nextLabel;
        private Transform rowContainer;
        private GameObject loadingSpinner;
        private bool isBuilt;

        // Launch date (must match DailySeedManager)
        private static readonly DateTime LaunchDate = new DateTime(2026, 3, 20);

        private void OnEnable()
        {
            if (!isBuilt) { BuildUI(); isBuilt = true; }
            selectedDayNumber = GameSession.TodayDayNumber;
            selectedDateStr = GameSession.TodayDateStr;
            RefreshDayDisplay();
            FetchForSelectedDay();
        }

        private void BuildUI()
        {
            var bg = gameObject.GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            bg.color = OC.bg;

            OvertoneUI.CreateTopGradient(transform);

            var content = OvertoneUI.CreateUIObject("Content", transform);
            OvertoneUI.StretchFill(content.GetComponent<RectTransform>());
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 0;

            BuildHeaderRow(content.transform);
            BuildScoreList(content.transform);
        }

        private void BuildHeaderRow(Transform parent)
        {
            var row = OvertoneUI.CreateUIObject("HeaderRow", parent);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.padding = new RectOffset(24, 24, (int)OS.safeAreaTop, 12);
            row.AddComponent<LayoutElement>().preferredHeight = OS.safeAreaTop + 60;

            // Back button
            var (backGO, backBtn) = OvertoneUI.CreateBackButton(row.transform);
            backBtn.onClick.AddListener(OnBackClicked);

            // Day nav card
            var navCard = OvertoneUI.CreateCard(row.transform, "DayNavCard");
            var navCardLE = navCard.GetComponent<LayoutElement>();
            if (navCardLE == null) navCardLE = navCard.AddComponent<LayoutElement>();
            navCardLE.flexibleWidth = 1;

            var navHLG = navCard.AddComponent<HorizontalLayoutGroup>();
            navHLG.spacing = 12;
            navHLG.childAlignment = TextAnchor.MiddleCenter;
            navHLG.childControlWidth = false;
            navHLG.childControlHeight = true;
            navHLG.childForceExpandWidth = false;
            navHLG.padding = new RectOffset(14, 14, 8, 8);

            // Prev button
            var prevGO = OvertoneUI.CreateUIObject("Prev", navCard.transform);
            prevLabel = prevGO.AddComponent<TextMeshProUGUI>();
            prevLabel.text = "\u2039"; // ‹
            prevLabel.font = OvertoneUI.DMMono;
            prevLabel.fontSize = 16;
            prevLabel.color = OC.muted;
            prevLabel.alignment = TextAlignmentOptions.Center;
            prevButton = prevGO.AddComponent<Button>();
            prevButton.onClick.AddListener(OnPrevDay);

            // Day info (flex center)
            var dayInfo = OvertoneUI.CreateUIObject("DayInfo", navCard.transform);
            dayInfo.AddComponent<LayoutElement>().flexibleWidth = 1;
            var dayVLG = dayInfo.AddComponent<VerticalLayoutGroup>();
            dayVLG.childAlignment = TextAnchor.MiddleCenter;
            dayVLG.spacing = 2;
            dayVLG.childControlWidth = true;
            dayVLG.childControlHeight = false;
            dayVLG.childForceExpandWidth = true;

            dayNumberLabel = OvertoneUI.CreateLabel(dayInfo.transform, "#1",
                OvertoneUI.PressStart2P, OFont.label, OC.cyan, "DayNumber");
            dayNumberLabel.characterSpacing = 1;
            dayNumberLabel.alignment = TextAlignmentOptions.Center;
            dayNumberLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 14;

            dayDateLabel = OvertoneUI.CreateLabel(dayInfo.transform, "",
                OvertoneUI.DMMono, OFont.bodyXs, OC.muted, "DayDate");
            dayDateLabel.alignment = TextAlignmentOptions.Center;
            dayDateLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 14;

            // Next button
            var nextGO = OvertoneUI.CreateUIObject("Next", navCard.transform);
            nextLabel = nextGO.AddComponent<TextMeshProUGUI>();
            nextLabel.text = "\u203A"; // ›
            nextLabel.font = OvertoneUI.DMMono;
            nextLabel.fontSize = 16;
            nextLabel.color = OC.muted;
            nextLabel.alignment = TextAlignmentOptions.Center;
            nextButton = nextGO.AddComponent<Button>();
            nextButton.onClick.AddListener(OnNextDay);
        }

        private void BuildScoreList(Transform parent)
        {
            // Loading spinner
            loadingSpinner = OvertoneUI.CreateUIObject("Loading", parent);
            var loadTMP = loadingSpinner.AddComponent<TextMeshProUGUI>();
            loadTMP.text = "Loading...";
            loadTMP.font = OvertoneUI.DMMono;
            loadTMP.fontSize = OFont.body;
            loadTMP.color = OC.muted;
            loadTMP.alignment = TextAlignmentOptions.Center;
            loadingSpinner.AddComponent<LayoutElement>().preferredHeight = 40;
            loadingSpinner.SetActive(false);

            // Scroll rect for rows
            var scrollGO = OvertoneUI.CreateUIObject("ScoreList", parent);
            scrollGO.AddComponent<LayoutElement>().flexibleHeight = 1;
            var scrollRT = scrollGO.GetComponent<RectTransform>();

            var scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollGO.AddComponent<RectMask2D>();

            var viewport = OvertoneUI.CreateUIObject("Viewport", scrollGO.transform);
            OvertoneUI.StretchFill(viewport.GetComponent<RectTransform>());

            var contentGO = OvertoneUI.CreateUIObject("Content", viewport.transform);
            var contentRT = contentGO.GetComponent<RectTransform>();
            contentRT.anchorMin = new Vector2(0, 1);
            contentRT.anchorMax = new Vector2(1, 1);
            contentRT.pivot = new Vector2(0.5f, 1);

            var contentVLG = contentGO.AddComponent<VerticalLayoutGroup>();
            contentVLG.childControlWidth = true;
            contentVLG.childControlHeight = false;
            contentVLG.childForceExpandWidth = true;
            contentVLG.spacing = 0;
            contentVLG.padding = new RectOffset(24, 24, 0, 32);

            var csf = contentGO.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = contentRT;

            rowContainer = contentGO.transform;
        }

        // ───── Day navigation ─────

        private void OnPrevDay()
        {
            if (selectedDayNumber <= 1) return;
            selectedDayNumber--;
            selectedDateStr = DayNumberToDate(selectedDayNumber);
            RefreshDayDisplay();
            FetchForSelectedDay();
        }

        private void OnNextDay()
        {
            if (selectedDayNumber >= GameSession.TodayDayNumber) return;
            selectedDayNumber++;
            selectedDateStr = DayNumberToDate(selectedDayNumber);
            RefreshDayDisplay();
            FetchForSelectedDay();
        }

        private void RefreshDayDisplay()
        {
            if (dayNumberLabel != null)
                dayNumberLabel.text = $"#{selectedDayNumber}";

            if (dayDateLabel != null)
            {
                if (DateTime.TryParse(selectedDateStr, out DateTime dt))
                    dayDateLabel.text = dt.ToString("MMM dd").ToUpper();
            }

            // Dim nav buttons at boundaries
            if (prevLabel != null)
                prevLabel.color = selectedDayNumber <= 1 ? OC.dim : OC.muted;
            if (nextLabel != null)
                nextLabel.color = selectedDayNumber >= GameSession.TodayDayNumber ? OC.dim : OC.muted;
        }

        private void FetchForSelectedDay()
        {
            if (loadingSpinner != null) loadingSpinner.SetActive(true);

            // Clear existing rows
            if (rowContainer != null)
            {
                foreach (Transform child in rowContainer)
                    Destroy(child.gameObject);
            }

            if (LeaderboardService.Instance != null)
            {
                LeaderboardService.Instance.FetchLeaderboard(selectedDateStr, (entries) =>
                {
                    if (loadingSpinner != null) loadingSpinner.SetActive(false);
                    PopulateRows(entries);
                });
            }
            else
            {
                if (loadingSpinner != null) loadingSpinner.SetActive(false);
            }
        }

        private void PopulateRows(List<LeaderboardEntry> entries)
        {
            if (rowContainer == null) return;

            foreach (Transform child in rowContainer)
                Destroy(child.gameObject);

            if (entries == null || entries.Count == 0)
            {
                var empty = OvertoneUI.CreateLabel(rowContainer, "No scores yet",
                    OvertoneUI.DMMono, OFont.body, OC.muted, "Empty");
                empty.alignment = TextAlignmentOptions.Center;
                empty.gameObject.AddComponent<LayoutElement>().preferredHeight = 60;
                return;
            }

            string[] medals = { "\U0001F947", "\U0001F948", "\U0001F949" };

            for (int i = 0; i < entries.Count; i++)
            {
                var entry = entries[i];
                var (rowGO, rankTMP, nameTMP, scoreTMP) =
                    OvertoneUI.CreateLeaderboardRow(rowContainer, $"Row{i}");

                string rankText = i < 3 ? medals[i] : $"#{entry.rank}";
                rankTMP.text = rankText;
                if (i < 3) rankTMP.color = OC.amber;
                nameTMP.text = entry.display_name ?? "???";
                scoreTMP.text = entry.score.ToString("N0");

                bool isMe = !string.IsNullOrEmpty(entry.device_uuid) &&
                            entry.device_uuid == GameSession.DeviceUUID;
                if (isMe)
                {
                    OvertoneUI.HighlightLeaderboardRow(rowGO, rankTMP, nameTMP, scoreTMP);
                    var bg = rowGO.GetComponent<Image>();
                    if (bg != null)
                    {
                        bg.sprite = Visual.PixelUIGenerator.GetRoundedRect9Slice();
                        bg.type = Image.Type.Sliced;
                        bg.color = OC.A(OC.cyan, 0.10f);
                    }
                }
            }
        }

        private string DayNumberToDate(int dayNumber)
        {
            var date = LaunchDate.AddDays(dayNumber - 1);
            return date.ToString("yyyy-MM-dd");
        }

        private void OnBackClicked()
        {
            if (ScreenManager.Instance != null)
            {
                var target = GameSession.HasPlayedToday ? Screen.HomePlayed : Screen.HomeFresh;
                ScreenManager.Instance.NavigateTo(target);
            }
        }
    }
}
