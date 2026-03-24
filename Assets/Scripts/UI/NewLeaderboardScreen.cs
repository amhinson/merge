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
    public class NewLeaderboardScreen : MonoBehaviour
    {
        private int selectedDayNumber;
        private string selectedDateStr;

        private TextMeshProUGUI dayNumberLabel;
        private TextMeshProUGUI dayDateLabel;
        private TextMeshProUGUI prevLabel;
        private TextMeshProUGUI nextLabel;
        private Transform rowContent;
        private GameObject loadingIndicator;
        private ScrollRect scrollRect;
        private bool isBuilt;

        private int currentPage;
        private bool isFetching;
        private bool allLoaded;
        private const int PageSize = 25;

        private static readonly DateTime LaunchDate = new DateTime(2026, 3, 20);

        private void OnEnable()
        {
            if (!isBuilt) { BuildUI(); isBuilt = true; }
            selectedDayNumber = GameSession.TodayDayNumber;
            selectedDateStr = GameSession.TodayDateStr;
            RefreshDayDisplay();
            LoadFresh();
        }

        private void BuildUI()
        {
            var bg = gameObject.GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            bg.color = OC.bg;

            // No layout group on root — use manual anchoring for header vs list
            // Header at top, list fills remaining space

            BuildHeader(transform);
            BuildScoreList(transform);
        }

        private void BuildHeader(Transform parent)
        {
            // Header container — fixed at top
            var header = OvertoneUI.CreateUIObject("Header", parent);
            var hRT = header.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0, 1);
            hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.anchoredPosition = new Vector2(0, -(OS.safeAreaTop + 8));
            hRT.sizeDelta = new Vector2(0, 44);

            // Back button (left)
            var backGO = OvertoneUI.CreateUIObject("BackBtn", header.transform);
            var backRT = backGO.GetComponent<RectTransform>();
            backRT.anchorMin = new Vector2(0, 0); backRT.anchorMax = new Vector2(0, 1);
            backRT.pivot = new Vector2(0, 0.5f);
            backRT.anchoredPosition = new Vector2(24, 0);
            backRT.sizeDelta = new Vector2(42, 0);
            // Outline-only border (same style as X button in game)
            var backBdr = OvertoneUI.CreateUIObject("Border", backGO.transform);
            OvertoneUI.StretchFill(backBdr.GetComponent<RectTransform>());
            var bbImg = backBdr.AddComponent<Image>();
            bbImg.sprite = OvertoneUI.SmoothRoundedRect;
            bbImg.type = Image.Type.Sliced;
            bbImg.color = OC.border;
            bbImg.raycastTarget = false;
            // Fill (inset, matches bg)
            var backFill = OvertoneUI.CreateUIObject("Fill", backGO.transform);
            var bfRT = backFill.GetComponent<RectTransform>();
            bfRT.anchorMin = Vector2.zero; bfRT.anchorMax = Vector2.one;
            bfRT.offsetMin = new Vector2(1.5f, 1.5f); bfRT.offsetMax = new Vector2(-1.5f, -1.5f);
            var bfImg = backFill.AddComponent<Image>();
            bfImg.sprite = OvertoneUI.SmoothRoundedRect;
            bfImg.type = Image.Type.Sliced;
            bfImg.color = OC.bg;
            bfImg.raycastTarget = false;
            // Hit area
            var backImg = backGO.AddComponent<Image>();
            backImg.color = Color.clear;
            backGO.AddComponent<Button>().targetGraphic = bbImg;
            backGO.GetComponent<Button>().onClick.AddListener(OnBackClicked);
            // Arrow label — visible muted white, like X button
            var arrowTMP = OvertoneUI.CreateLabel(backGO.transform, "<",
                OvertoneUI.DMMono, 14, new Color(1, 1, 1, 0.3f), "Arrow");
            arrowTMP.alignment = TextAlignmentOptions.Center;
            OvertoneUI.StretchFill(arrowTMP.GetComponent<RectTransform>());

            // Day nav card (right of back button)
            var navCard = OvertoneUI.CreateUIObject("DayNav", header.transform);
            var ncRT = navCard.GetComponent<RectTransform>();
            ncRT.anchorMin = new Vector2(0, 0); ncRT.anchorMax = new Vector2(1, 1);
            ncRT.offsetMin = new Vector2(74, 0); // 24 padding + 42 back + 8 gap
            ncRT.offsetMax = new Vector2(-24, 0);
            // Border
            var ncBdr = OvertoneUI.CreateUIObject("Border", navCard.transform);
            OvertoneUI.StretchFill(ncBdr.GetComponent<RectTransform>());
            ncBdr.AddComponent<Image>().sprite = OvertoneUI.SmoothRoundedRect;
            ncBdr.GetComponent<Image>().type = Image.Type.Sliced;
            ncBdr.GetComponent<Image>().color = OC.border;
            ncBdr.GetComponent<Image>().raycastTarget = false;
            // Fill
            var ncFill = OvertoneUI.CreateUIObject("Fill", navCard.transform);
            var ncfRT = ncFill.GetComponent<RectTransform>();
            ncfRT.anchorMin = Vector2.zero; ncfRT.anchorMax = Vector2.one;
            ncfRT.offsetMin = new Vector2(1, 1); ncfRT.offsetMax = new Vector2(-1, -1);
            ncFill.AddComponent<Image>().sprite = OvertoneUI.SmoothRoundedRect;
            ncFill.GetComponent<Image>().type = Image.Type.Sliced;
            ncFill.GetComponent<Image>().color = OC.surface;
            ncFill.GetComponent<Image>().raycastTarget = false;

            // Prev ‹
            prevLabel = OvertoneUI.CreateLabel(navCard.transform, "<",
                OvertoneUI.DMMono, 14, OC.muted, "Prev");
            prevLabel.raycastTarget = true; // must be true for Button to receive clicks
            var prevRT = prevLabel.GetComponent<RectTransform>();
            prevRT.anchorMin = new Vector2(0, 0); prevRT.anchorMax = new Vector2(0.15f, 1);
            prevRT.offsetMin = new Vector2(10, 0); prevRT.offsetMax = Vector2.zero;
            prevLabel.alignment = TextAlignmentOptions.Center;
            prevLabel.verticalAlignment = VerticalAlignmentOptions.Middle;
            var prevBtn = prevLabel.gameObject.AddComponent<Button>();
            prevBtn.onClick.AddListener(OnPrevDay);

            // Day number
            dayNumberLabel = OvertoneUI.CreateLabel(navCard.transform, "#4",
                OvertoneUI.PressStart2P, 9, OC.cyan, "DayNum");
            dayNumberLabel.characterSpacing = 1;
            dayNumberLabel.alignment = TextAlignmentOptions.Center;
            dayNumberLabel.verticalAlignment = VerticalAlignmentOptions.Bottom;
            var dnRT = dayNumberLabel.GetComponent<RectTransform>();
            dnRT.anchorMin = new Vector2(0.12f, 0.45f); dnRT.anchorMax = new Vector2(0.88f, 1);
            dnRT.offsetMin = Vector2.zero; dnRT.offsetMax = new Vector2(0, -2);

            // Day date
            dayDateLabel = OvertoneUI.CreateLabel(navCard.transform, "MAR 23",
                OvertoneUI.DMMono, 11, OC.muted, "DayDate");
            dayDateLabel.alignment = TextAlignmentOptions.Center;
            dayDateLabel.verticalAlignment = VerticalAlignmentOptions.Top;
            var ddRT = dayDateLabel.GetComponent<RectTransform>();
            ddRT.anchorMin = new Vector2(0.12f, 0); ddRT.anchorMax = new Vector2(0.88f, 0.5f);
            ddRT.offsetMin = new Vector2(0, 2); ddRT.offsetMax = Vector2.zero;

            // Next ›
            nextLabel = OvertoneUI.CreateLabel(navCard.transform, ">",
                OvertoneUI.DMMono, 14, OC.muted, "Next");
            nextLabel.raycastTarget = true;
            var nxRT = nextLabel.GetComponent<RectTransform>();
            nxRT.anchorMin = new Vector2(0.85f, 0); nxRT.anchorMax = new Vector2(1, 1);
            nxRT.offsetMin = Vector2.zero; nxRT.offsetMax = new Vector2(-10, 0);
            nextLabel.alignment = TextAlignmentOptions.Center;
            nextLabel.verticalAlignment = VerticalAlignmentOptions.Middle;
            var nextBtn = nextLabel.gameObject.AddComponent<Button>();
            nextBtn.onClick.AddListener(OnNextDay);
        }

        private void BuildScoreList(Transform parent)
        {
            // Scroll rect fills space below header
            var scrollGO = OvertoneUI.CreateUIObject("ScoreList", parent);
            var sRT = scrollGO.GetComponent<RectTransform>();
            sRT.anchorMin = Vector2.zero;
            sRT.anchorMax = Vector2.one;
            sRT.offsetMin = new Vector2(0, 0);
            sRT.offsetMax = new Vector2(0, -(OS.safeAreaTop + 56)); // below header

            scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 20;

            // Viewport with mask
            var viewport = OvertoneUI.CreateUIObject("Viewport", scrollGO.transform);
            OvertoneUI.StretchFill(viewport.GetComponent<RectTransform>());
            viewport.AddComponent<RectMask2D>(); // clips content
            // Invisible Image needed for ScrollRect to receive touch/drag events
            var vpImg = viewport.AddComponent<Image>();
            vpImg.color = Color.clear;

            // Content
            var content = OvertoneUI.CreateUIObject("Content", viewport.transform);
            var cRT = content.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1);
            cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1);
            cRT.sizeDelta = new Vector2(0, 0);

            var cVLG = content.AddComponent<VerticalLayoutGroup>();
            cVLG.childControlWidth = true;
            cVLG.childControlHeight = true;
            cVLG.childForceExpandWidth = true;
            cVLG.childForceExpandHeight = false;
            cVLG.spacing = 2;
            cVLG.padding = new RectOffset(24, 24, 8, 32 + (int)OS.safeAreaBottom);

            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scrollRect.viewport = viewport.GetComponent<RectTransform>();
            scrollRect.content = cRT;
            rowContent = content.transform;

            // Loading indicator
            loadingIndicator = OvertoneUI.CreateUIObject("Loading", content.transform);
            var loadTMP = loadingIndicator.AddComponent<TextMeshProUGUI>();
            loadTMP.text = "...";
            loadTMP.font = OvertoneUI.DMMono;
            loadTMP.fontSize = 11;
            loadTMP.color = OC.muted;
            loadTMP.alignment = TextAlignmentOptions.Center;
            var loadLE = loadingIndicator.AddComponent<LayoutElement>();
            loadLE.preferredHeight = 30; loadLE.minHeight = 30;

            scrollRect.onValueChanged.AddListener(OnScroll);
        }

        private void OnScroll(Vector2 pos)
        {
            if (allLoaded || isFetching) return;
            if (pos.y < 0.2f) FetchNextPage();
        }

        private void LoadFresh()
        {
            if (rowContent != null)
                foreach (Transform child in rowContent)
                    if (child.gameObject != loadingIndicator) Destroy(child.gameObject);
            currentPage = 0;
            allLoaded = false;
            if (loadingIndicator != null) loadingIndicator.SetActive(true);
            FetchNextPage();
        }

        private void FetchNextPage()
        {
            if (isFetching || allLoaded) return;
            isFetching = true;
            if (loadingIndicator != null)
            {
                loadingIndicator.SetActive(true);
                loadingIndicator.transform.SetAsLastSibling();
            }

            int offset = currentPage * PageSize;
            string uuid = PlayerIdentity.Instance != null ? PlayerIdentity.Instance.DeviceUUID : "";

            if (SupabaseClient.Instance == null)
            {
                isFetching = false;
                if (loadingIndicator != null) loadingIndicator.SetActive(false);
                if (currentPage == 0) ShowEmpty();
                return;
            }

            string query = $"game_date={selectedDateStr}&device_uuid={uuid}&limit={PageSize}&offset={offset}";
            Debug.Log($"[Leaderboard] Fetching: {query}");
            SupabaseClient.Instance.CallFunctionGet("get-leaderboard", query, (success, response) =>
            {
                isFetching = false;
                if (this == null || !gameObject.activeInHierarchy) return;
                if (loadingIndicator != null) loadingIndicator.SetActive(false);

                Debug.Log($"[Leaderboard] Response: success={success}, len={response?.Length ?? 0}, response={response?.Substring(0, Mathf.Min(200, response?.Length ?? 0))}");

                if (!success || string.IsNullOrEmpty(response))
                {
                    if (currentPage == 0) ShowEmpty();
                    return;
                }

                try
                {
                    string wrapped = $"{{\"entries\":{response}}}";
                    var parsed = JsonUtility.FromJson<LeaderboardResponse>(wrapped);
                    var entries = parsed.entries != null
                        ? new List<LeaderboardEntry>(parsed.entries)
                        : new List<LeaderboardEntry>();

                    if (entries.Count == 0 && currentPage == 0)
                    {
                        ShowEmpty();
                        return;
                    }

                    Debug.Log($"[Leaderboard] Appending {entries.Count} rows, content children before: {rowContent?.childCount}");
                    AppendRows(entries);
                    Debug.Log($"[Leaderboard] After append, content children: {rowContent?.childCount}");
                    currentPage++;
                    if (entries.Count < PageSize) allLoaded = true;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Leaderboard parse: {e.Message}");
                    if (currentPage == 0) ShowEmpty();
                }
            });
        }

        private void AppendRows(List<LeaderboardEntry> entries)
        {
            string uuid = PlayerIdentity.Instance != null ? PlayerIdentity.Instance.DeviceUUID : "";

            foreach (var entry in entries)
            {
                bool isMe = !string.IsNullOrEmpty(entry.device_uuid) && entry.device_uuid == uuid;

                var row = OvertoneUI.CreateUIObject($"R{entry.rank}", rowContent);
                var rowLE = row.AddComponent<LayoutElement>();
                rowLE.preferredHeight = 38; rowLE.minHeight = 38; rowLE.flexibleHeight = 0;

                // Row HLG
                var rowHLG = row.AddComponent<HorizontalLayoutGroup>();
                rowHLG.spacing = 6;
                rowHLG.padding = new RectOffset(10, 10, 0, 0);
                rowHLG.childAlignment = TextAnchor.MiddleLeft;
                rowHLG.childControlWidth = true;
                rowHLG.childControlHeight = true;
                rowHLG.childForceExpandWidth = false;
                rowHLG.childForceExpandHeight = true;

                if (isMe)
                {
                    var rowBg = row.AddComponent<Image>();
                    rowBg.sprite = OvertoneUI.SmoothRoundedRect;
                    rowBg.type = Image.Type.Sliced;
                    rowBg.color = new Color32(18, 38, 34, 255);
                }

                Color nameColor = isMe ? OC.cyan : OC.muted;
                Color scoreColor = isMe ? OC.cyan : OC.A(Color.white, 0.32f);
                Color rankColor = isMe ? OC.cyan : OC.dim;

                // Rank
                var rankTMP = OvertoneUI.CreateLabel(row.transform, $"#{entry.rank}",
                    OvertoneUI.PressStart2P, 7, rankColor, "Rank");
                rankTMP.textWrappingMode = TextWrappingModes.NoWrap;
                rankTMP.alignment = TextAlignmentOptions.Left;
                rankTMP.gameObject.AddComponent<LayoutElement>().preferredWidth = 32;

                // Name
                var nameTMP = OvertoneUI.CreateLabel(row.transform, entry.display_name ?? "???",
                    OvertoneUI.DMMono, 13, nameColor, "Name");
                nameTMP.textWrappingMode = TextWrappingModes.NoWrap;
                nameTMP.overflowMode = TextOverflowModes.Ellipsis;
                nameTMP.alignment = TextAlignmentOptions.Left;
                nameTMP.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;

                // Score
                var scoreTMP = OvertoneUI.CreateLabel(row.transform, entry.score.ToString("N0"),
                    OvertoneUI.DMMono, 13, scoreColor, "Score");
                scoreTMP.textWrappingMode = TextWrappingModes.NoWrap;
                scoreTMP.alignment = TextAlignmentOptions.Right;
                scoreTMP.gameObject.AddComponent<LayoutElement>().preferredWidth = 80;
            }

            if (loadingIndicator != null)
                loadingIndicator.transform.SetAsLastSibling();
        }

        private void ShowEmpty()
        {
            var empty = OvertoneUI.CreateUIObject("Empty", rowContent);
            var tmp = empty.AddComponent<TextMeshProUGUI>();
            tmp.text = "No scores yet";
            tmp.font = OvertoneUI.DMMono;
            tmp.fontSize = 11;
            tmp.color = OC.muted;
            tmp.alignment = TextAlignmentOptions.Center;
            empty.AddComponent<LayoutElement>().preferredHeight = 60;
        }

        private void OnPrevDay()
        {
            if (selectedDayNumber <= 1) return;
            selectedDayNumber--;
            selectedDateStr = DayNumberToDate(selectedDayNumber);
            RefreshDayDisplay();
            LoadFresh();
        }

        private void OnNextDay()
        {
            if (selectedDayNumber >= GameSession.TodayDayNumber) return;
            selectedDayNumber++;
            selectedDateStr = DayNumberToDate(selectedDayNumber);
            RefreshDayDisplay();
            LoadFresh();
        }

        private void RefreshDayDisplay()
        {
            if (dayNumberLabel != null) dayNumberLabel.text = $"#{selectedDayNumber}";
            if (dayDateLabel != null)
            {
                if (DateTime.TryParse(selectedDateStr, out DateTime dt))
                    dayDateLabel.text = dt.ToString("MMM dd").ToUpper();
            }
            if (prevLabel != null)
            {
                bool atFirst = selectedDayNumber <= 1;
                prevLabel.gameObject.SetActive(!atFirst);
            }
            if (nextLabel != null)
            {
                bool atToday = selectedDayNumber >= GameSession.TodayDayNumber;
                nextLabel.gameObject.SetActive(!atToday); // hide entirely when at today
            }
        }

        private string DayNumberToDate(int dayNumber)
        {
            return LaunchDate.AddDays(dayNumber - 1).ToString("yyyy-MM-dd");
        }

        private void OnBackClicked()
        {
            if (ScreenManager.Instance != null)
            {
                bool hasPlayed = GameSession.HasPlayedToday ||
                    (DailySeedManager.Instance != null && DailySeedManager.Instance.HasCompletedScoredAttempt());
                ScreenManager.Instance.NavigateTo(hasPlayed ? Screen.HomePlayed : Screen.HomeFresh);
            }
        }
    }
}
