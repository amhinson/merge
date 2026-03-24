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
    /// Shared base for HomeFresh and HomePlayed screens.
    /// Layout (top to bottom):
    ///   - Logo block: OVER / TONE (stacked), A DAILY MERGE GAME, settings icon top-right
    ///   - Flex spacer
    ///   - Ball cluster (3 balls centered)
    ///   - Flex spacer
    ///   - Puzzle row (#142 ─── MAR 22)
    ///   - Leaderboard card
    ///   - CTA block (subclass: PLAY or Share/Play Again)
    ///   - Bottom padding
    /// </summary>
    public abstract class HomeScreen : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] protected BallTierConfig tierConfig;

        // Built UI elements
        protected TextMeshProUGUI dayNumberLabel;
        protected TextMeshProUGUI dateLabel;
        protected Transform leaderboardRowContainer;
        private GameObject leaderboardWrapper;
        protected GameObject loadingIndicator;
        protected Transform ctaContainer;
        protected RectTransform contentRoot;

        // Leaderboard state
        protected List<LeaderboardEntry> cachedEntries;

        // Pill sprite for smooth balls
        private static Sprite _circleSprite;

        private bool isBuilt;

        protected virtual void OnEnable()
        {
            if (!isBuilt) { BuildUI(); isBuilt = true; }
            Refresh();
        }

        private bool isFetching;

        public virtual void Refresh()
        {
            RefreshPuzzleRow();
            if (!isFetching) FetchLeaderboard();
        }

        // ───── UI Construction ─────

        protected virtual void BuildUI()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();

            var bg = gameObject.GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            bg.color = OC.bg;

            // No gradient on home screen

            // Settings button (top-right, absolute positioned)
            BuildSettingsButton();

            // Main vertical layout — childControlHeight true for flex spacers
            var content = OvertoneUI.CreateUIObject("Content", transform);
            contentRoot = content.GetComponent<RectTransform>();
            OvertoneUI.StretchFill(contentRoot);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(0, 0, 0, 0);
            vlg.spacing = 0;

            // === TOP: Logo ===
            BuildLogoBlock(content.transform);

            // === Flex spacer (pushes balls to center) ===
            AddFlex(content.transform, 1f);

            // === MIDDLE: Ball cluster ===
            BuildBallCluster(content.transform);

            // === Flex spacer ===
            AddFlex(content.transform, 1f);

            // === BOTTOM SECTION ===
            BuildPuzzleRow(content.transform);

            // Hook for subclass content (stats row for HomePlayed — sits between puzzle row and leaderboard)
            BuildMiddleSection(content.transform);

            BuildLeaderboardCard(content.transform);

            // CTA container
            ctaContainer = OvertoneUI.CreateUIObject("CTABlock", content.transform).transform;
            var ctaVLG = ctaContainer.gameObject.AddComponent<VerticalLayoutGroup>();
            ctaVLG.childAlignment = TextAnchor.MiddleCenter;
            ctaVLG.childControlWidth = true;
            ctaVLG.childControlHeight = false;
            ctaVLG.childForceExpandWidth = true;
            ctaVLG.padding = new RectOffset(24, 24, 6, 4);
            ctaVLG.spacing = 4;

            BuildCTABlock(ctaContainer);

            // Bottom padding (includes safe area for home indicator)
            AddSpacer(content.transform, 24 + OS.safeAreaBottom);
        }

        /// <summary>Override to insert content between flex and puzzle row.</summary>
        protected virtual void BuildMiddleSection(Transform parent) { }

        /// <summary>Override to build screen-specific CTA buttons.</summary>
        protected abstract void BuildCTABlock(Transform parent);

        private void BuildSettingsButton()
        {
            // Absolute positioned top-right
            var settingsGO = OvertoneUI.CreateUIObject("SettingsButton", transform);
            var settingsRT = settingsGO.GetComponent<RectTransform>();
            settingsRT.anchorMin = new Vector2(1, 1);
            settingsRT.anchorMax = new Vector2(1, 1);
            settingsRT.pivot = new Vector2(1, 1);
            settingsRT.anchoredPosition = new Vector2(-24, -OS.safeAreaTop);
            settingsRT.sizeDelta = new Vector2(34, 34);

            // Outline-only sprite (transparent fill, just the border ring)
            var bgImg = settingsGO.AddComponent<Image>();
            bgImg.sprite = GetOutlineRoundedRect();
            bgImg.type = Image.Type.Simple;
            bgImg.color = OC.border;

            // Gear icon — pixel-art sprite (TMP fonts don't have ⚙ glyph)
            var gearGO = OvertoneUI.CreateUIObject("GearIcon", settingsGO.transform);
            var gearRT = gearGO.GetComponent<RectTransform>();
            gearRT.anchorMin = new Vector2(0.5f, 0.5f);
            gearRT.anchorMax = new Vector2(0.5f, 0.5f);
            gearRT.pivot = new Vector2(0.5f, 0.5f);
            gearRT.anchoredPosition = Vector2.zero;
            gearRT.sizeDelta = new Vector2(16, 16);
            var gearImg = gearGO.AddComponent<Image>();
            gearImg.sprite = Visual.PixelUIGenerator.CreateGearIcon(32, new Color(1, 1, 1, 0.35f));
            gearImg.preserveAspect = true;
            gearImg.raycastTarget = false;

            var btn = settingsGO.AddComponent<Button>();
            btn.targetGraphic = bgImg;
            btn.onClick.AddListener(() =>
            {
                if (ScreenManager.Instance != null)
                    ScreenManager.Instance.NavigateTo(Screen.Settings);
            });
        }

        private void BuildLogoBlock(Transform parent)
        {
            var block = OvertoneUI.CreateUIObject("LogoBlock", parent);
            var blockLE = block.AddComponent<LayoutElement>();
            blockLE.minHeight = 100;
            blockLE.preferredHeight = 100;

            // Inner container with manual positioning (left-aligned)
            var inner = OvertoneUI.CreateUIObject("LogoInner", block.transform);
            var innerRT = inner.GetComponent<RectTransform>();
            innerRT.anchorMin = Vector2.zero;
            innerRT.anchorMax = Vector2.one;
            innerRT.offsetMin = Vector2.zero;
            innerRT.offsetMax = Vector2.zero;

            var innerVLG = inner.AddComponent<VerticalLayoutGroup>();
            innerVLG.childAlignment = TextAnchor.UpperLeft;
            innerVLG.spacing = 2;
            innerVLG.childControlWidth = false;
            innerVLG.childControlHeight = false;
            innerVLG.childForceExpandWidth = false;
            innerVLG.padding = new RectOffset(24, 24, (int)OS.safeAreaTop + 12, 0);

            // OVER (line 1)
            var overGO = OvertoneUI.CreateUIObject("TitleOver", inner.transform);
            overGO.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 28);
            var overTMP = overGO.AddComponent<TextMeshProUGUI>();
            overTMP.text = "OVER";
            overTMP.font = OvertoneUI.PressStart2P;
            overTMP.fontSize = OFont.title;
            overTMP.color = OC.white;
            overTMP.characterSpacing = 2;
            overTMP.textWrappingMode = TextWrappingModes.NoWrap;
            overTMP.overflowMode = TextOverflowModes.Overflow;
            overTMP.raycastTarget = false;

            // TONE (line 2)
            var toneGO = OvertoneUI.CreateUIObject("TitleTone", inner.transform);
            toneGO.GetComponent<RectTransform>().sizeDelta = new Vector2(200, 28);
            var toneTMP = toneGO.AddComponent<TextMeshProUGUI>();
            toneTMP.text = "TONE";
            toneTMP.font = OvertoneUI.PressStart2P;
            toneTMP.fontSize = OFont.title;
            toneTMP.color = OC.cyan;
            toneTMP.characterSpacing = 2;
            toneTMP.textWrappingMode = TextWrappingModes.NoWrap;
            toneTMP.overflowMode = TextOverflowModes.Overflow;
            toneTMP.raycastTarget = false;

            // Tagline
            var tagGO = OvertoneUI.CreateUIObject("Tagline", inner.transform);
            tagGO.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 16);
            var tagTMP = tagGO.AddComponent<TextMeshProUGUI>();
            tagTMP.text = "A DAILY MERGE GAME";
            tagTMP.font = OvertoneUI.DMMono;
            tagTMP.fontSize = 10;
            tagTMP.color = OC.muted;
            tagTMP.characterSpacing = 5;
            tagTMP.textWrappingMode = TextWrappingModes.NoWrap;
            tagTMP.raycastTarget = false;
        }

        private void BuildBallCluster(Transform parent)
        {
            var cluster = OvertoneUI.CreateUIObject("BallCluster", parent);
            var hlg = cluster.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 16;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            var clusterLE = cluster.AddComponent<LayoutElement>();
            clusterLE.preferredHeight = 100;
            clusterLE.minHeight = 80;

            // Design shows: pink (L2, tier 9), cyan (L1, tier 10, largest center), amber (L3, tier 8)
            int[] tiers = { 9, 10, 8 };
            float[] sizes = { 66f, 82f, 56f };

            for (int i = 0; i < 3; i++)
            {
                var ballGO = OvertoneUI.CreateUIObject($"ClusterBall{i}", cluster.transform);
                var ballRT = ballGO.GetComponent<RectTransform>();
                ballRT.sizeDelta = new Vector2(sizes[i], sizes[i]);

                var img = ballGO.AddComponent<Image>();

                // Use real ball sprite from NeonBallRenderer
                float uiRadius = sizes[i] / (2f * 48f);
                var png = Visual.NeonBallRenderer.GenerateBallPNG(tiers[i], Color.white, uiRadius, 0f);
                var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.LoadImage(png);
                img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                    new Vector2(0.5f, 0.5f), tex.width);
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
                img.color = Color.white;

                // Animate the waveform
                var anim = ballGO.AddComponent<Visual.UIBallAnimator>();
                anim.Initialize(tiers[i], uiRadius);

                ballGO.AddComponent<LayoutElement>();
            }
        }

        private void BuildPuzzleRow(Transform parent)
        {
            var row = OvertoneUI.CreateUIObject("PuzzleRow", parent);
            var rowLE = row.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 24;
            rowLE.minHeight = 24;

            // Manual positioning within the row — no layout group to avoid stretching the line
            var rowRT = row.GetComponent<RectTransform>();

            // Puzzle number (left)
            var dayGO = OvertoneUI.CreateUIObject("PuzzleNumber", row.transform);
            var dayRT = dayGO.GetComponent<RectTransform>();
            dayRT.anchorMin = new Vector2(0, 0);
            dayRT.anchorMax = new Vector2(0, 1);
            dayRT.pivot = new Vector2(0, 0.5f);
            dayRT.anchoredPosition = new Vector2(24, 0);
            dayRT.sizeDelta = new Vector2(60, 0);
            dayNumberLabel = dayGO.AddComponent<TextMeshProUGUI>();
            dayNumberLabel.font = OvertoneUI.PressStart2P;
            dayNumberLabel.fontSize = OFont.caption;
            dayNumberLabel.color = OC.cyan;
            dayNumberLabel.characterSpacing = 1;
            dayNumberLabel.alignment = TextAlignmentOptions.Left;
            dayNumberLabel.textWrappingMode = TextWrappingModes.NoWrap;
            dayNumberLabel.verticalAlignment = VerticalAlignmentOptions.Middle;
            dayNumberLabel.raycastTarget = false;

            // Date (right)
            var dateGO = OvertoneUI.CreateUIObject("DateLabel", row.transform);
            var dateRT = dateGO.GetComponent<RectTransform>();
            dateRT.anchorMin = new Vector2(1, 0);
            dateRT.anchorMax = new Vector2(1, 1);
            dateRT.pivot = new Vector2(1, 0.5f);
            dateRT.anchoredPosition = new Vector2(-24, 0);
            dateRT.sizeDelta = new Vector2(80, 0);
            dateLabel = dateGO.AddComponent<TextMeshProUGUI>();
            dateLabel.font = OvertoneUI.DMMono;
            dateLabel.fontSize = OFont.bodyXs;
            dateLabel.color = OC.muted;
            dateLabel.characterSpacing = 1;
            dateLabel.alignment = TextAlignmentOptions.Right;
            dateLabel.textWrappingMode = TextWrappingModes.NoWrap;
            dateLabel.verticalAlignment = VerticalAlignmentOptions.Middle;
            dateLabel.raycastTarget = false;

            // Thin line between them
            var lineGO = OvertoneUI.CreateUIObject("Line", row.transform);
            var lineRT = lineGO.GetComponent<RectTransform>();
            lineRT.anchorMin = new Vector2(0, 0.5f);
            lineRT.anchorMax = new Vector2(1, 0.5f);
            lineRT.pivot = new Vector2(0.5f, 0.5f);
            lineRT.offsetMin = new Vector2(84, -0.5f);  // left inset past #number
            lineRT.offsetMax = new Vector2(-104, 0.5f); // right inset before date
            var lineImg = lineGO.AddComponent<Image>();
            lineImg.color = OC.border;
            lineImg.raycastTarget = false;
        }

        protected void BuildLeaderboardCard(Transform parent)
        {
            // Spacer above leaderboard
            AddSpacer(parent, 8);

            // Fixed-height container — height updates dynamically when rows are populated
            var wrapper = OvertoneUI.CreateUIObject("LeaderboardWrapper", parent);
            var wrapperLE = wrapper.AddComponent<LayoutElement>();
            wrapperLE.preferredHeight = 60; // header + empty state, updated when rows arrive
            wrapperLE.minHeight = 40;
            wrapperLE.flexibleHeight = 0;
            leaderboardWrapper = wrapper;

            // Card background (manual anchoring, inset 24px from edges)
            var card = OvertoneUI.CreateUIObject("LeaderboardCard", wrapper.transform);
            var cardRT = card.GetComponent<RectTransform>();
            cardRT.anchorMin = Vector2.zero; cardRT.anchorMax = Vector2.one;
            cardRT.offsetMin = new Vector2(24, 0); cardRT.offsetMax = new Vector2(-24, 0);
            // Card border (rendered FIRST, slightly larger, behind the fill)
            var cardOutline = OvertoneUI.CreateUIObject("Border", card.transform);
            var coRT = cardOutline.GetComponent<RectTransform>();
            coRT.anchorMin = Vector2.zero; coRT.anchorMax = Vector2.one;
            coRT.offsetMin = new Vector2(-1, -1); coRT.offsetMax = new Vector2(1, 1);
            var coImg = cardOutline.AddComponent<Image>();
            coImg.sprite = Visual.PixelUIGenerator.GetRoundedRect9Slice();
            coImg.type = Image.Type.Sliced;
            coImg.color = OC.border;
            coImg.raycastTarget = false;
            // Card fill (on top of border, exact card size)
            var cardFill = OvertoneUI.CreateUIObject("Fill", card.transform);
            var cfRT = cardFill.GetComponent<RectTransform>();
            cfRT.anchorMin = Vector2.zero; cfRT.anchorMax = Vector2.one;
            cfRT.offsetMin = Vector2.zero; cfRT.offsetMax = Vector2.zero;
            var cardBg = cardFill.AddComponent<Image>();
            cardBg.sprite = Visual.PixelUIGenerator.GetRoundedRect9Slice();
            cardBg.type = Image.Type.Sliced;
            cardBg.color = OC.surface; // exactly #161B24
            cardBg.raycastTarget = false;

            // Header: "TODAY'S TOP" (left) + "ALL →" (right), 24px tall at top
            var headerLabelGO = OvertoneUI.CreateUIObject("HeaderLabel", card.transform);
            var hlRT = headerLabelGO.GetComponent<RectTransform>();
            hlRT.anchorMin = new Vector2(0, 1); hlRT.anchorMax = new Vector2(0.6f, 1);
            hlRT.pivot = new Vector2(0, 1);
            hlRT.anchoredPosition = new Vector2(10, 0);
            hlRT.sizeDelta = new Vector2(0, 24);
            var headerLabel = headerLabelGO.AddComponent<TextMeshProUGUI>();
            headerLabel.text = "TODAY'S TOP";
            headerLabel.font = OvertoneUI.PressStart2P;
            headerLabel.fontSize = OFont.labelXs;
            headerLabel.color = OC.muted;
            headerLabel.alignment = TextAlignmentOptions.Left;
            headerLabel.verticalAlignment = VerticalAlignmentOptions.Middle;
            headerLabel.textWrappingMode = TextWrappingModes.NoWrap;
            headerLabel.raycastTarget = false;

            var allBtnGO = OvertoneUI.CreateUIObject("AllButton", card.transform);
            var abRT = allBtnGO.GetComponent<RectTransform>();
            abRT.anchorMin = new Vector2(0.6f, 1); abRT.anchorMax = new Vector2(1, 1);
            abRT.pivot = new Vector2(1, 1);
            abRT.anchoredPosition = new Vector2(-10, 0);
            abRT.sizeDelta = new Vector2(0, 24);
            var allTMP = allBtnGO.AddComponent<TextMeshProUGUI>();
            allTMP.text = "ALL >";
            allTMP.font = OvertoneUI.PressStart2P;
            allTMP.fontSize = OFont.labelXs;
            allTMP.color = OC.cyan;
            allTMP.textWrappingMode = TextWrappingModes.NoWrap;
            allTMP.alignment = TextAlignmentOptions.Right;
            allTMP.verticalAlignment = VerticalAlignmentOptions.Middle;
            var allBtn = allBtnGO.AddComponent<Button>();
            allBtn.onClick.AddListener(() =>
            {
                if (ScreenManager.Instance != null)
                    ScreenManager.Instance.NavigateTo(Screen.Leaderboard);
            });

            // Divider line at y=-24 from top
            var divGO = OvertoneUI.CreateUIObject("Divider", card.transform);
            var divRT = divGO.GetComponent<RectTransform>();
            divRT.anchorMin = new Vector2(0, 1); divRT.anchorMax = new Vector2(1, 1);
            divRT.pivot = new Vector2(0.5f, 1);
            divRT.anchoredPosition = new Vector2(0, -24);
            divRT.sizeDelta = new Vector2(0, 1);
            divGO.AddComponent<Image>().color = OC.border;

            // Row container — VLG below the header (starts at y=-25)
            var rowContainer = OvertoneUI.CreateUIObject("Rows", card.transform);
            var rcRT = rowContainer.GetComponent<RectTransform>();
            rcRT.anchorMin = new Vector2(0, 0); rcRT.anchorMax = new Vector2(1, 1);
            rcRT.offsetMin = new Vector2(0, 0); rcRT.offsetMax = new Vector2(0, -25);
            var rowVLG = rowContainer.AddComponent<VerticalLayoutGroup>();
            rowVLG.childControlWidth = true;
            rowVLG.childControlHeight = true;  // MUST be true for LayoutElement.preferredHeight to work
            rowVLG.childForceExpandWidth = true;
            rowVLG.childForceExpandHeight = false; // don't expand rows beyond preferred
            rowVLG.spacing = 0;
            leaderboardRowContainer = rowContainer.transform;

            // Loading indicator
            loadingIndicator = OvertoneUI.CreateUIObject("Loading", rowContainer.transform);
            var loadingTMP = loadingIndicator.AddComponent<TextMeshProUGUI>();
            loadingTMP.text = "...";
            loadingTMP.font = OvertoneUI.DMMono;
            loadingTMP.fontSize = OFont.body;
            loadingTMP.color = OC.muted;
            loadingTMP.alignment = TextAlignmentOptions.Center;
            loadingIndicator.AddComponent<LayoutElement>().preferredHeight = 28;
        }

        // ───── Data refresh ─────

        private void RefreshPuzzleRow()
        {
            if (dayNumberLabel != null)
                dayNumberLabel.text = $"#{GameSession.TodayDayNumber}";
            if (dateLabel != null)
            {
                var now = System.DateTime.Now;
                dateLabel.text = now.ToString("MMM dd").ToUpper();
            }
        }

        protected void FetchLeaderboard()
        {
            isFetching = true;
            if (loadingIndicator != null) loadingIndicator.SetActive(true);

            if (LeaderboardService.Instance != null)
            {
                // FetchLeaderboard uses internal cache — if pre-loaded during startup,
                // the callback fires immediately with cached data
                LeaderboardService.Instance.FetchLeaderboard(GameSession.TodayDateStr, (entries) =>
                {
                    isFetching = false;
                    if (this == null || !gameObject.activeInHierarchy) return;
                    cachedEntries = entries;
                    if (loadingIndicator != null) loadingIndicator.SetActive(false);
                    PopulateLeaderboardRows(entries);
                });
            }
            else
            {
                isFetching = false;
                if (loadingIndicator != null) loadingIndicator.SetActive(false);
                PopulateLeaderboardRows(null);
            }
        }

        protected virtual void PopulateLeaderboardRows(List<LeaderboardEntry> entries)
        {
            if (leaderboardRowContainer == null) return;

            if (loadingIndicator != null) loadingIndicator.SetActive(false);

            foreach (Transform child in leaderboardRowContainer)
                Destroy(child.gameObject);

            int rowCount = 0;

            bool playerInTop3 = false;

            if (entries == null || entries.Count == 0)
            {
                // Empty state
                var emptyGO = OvertoneUI.CreateUIObject("EmptyState", leaderboardRowContainer);
                var emptyTMP = emptyGO.AddComponent<TextMeshProUGUI>();
                emptyTMP.text = "No scores yet today";
                emptyTMP.font = OvertoneUI.DMMono;
                emptyTMP.fontSize = 11;
                emptyTMP.color = OC.muted;
                emptyTMP.alignment = TextAlignmentOptions.Center;
                emptyTMP.raycastTarget = false;
                var emptyLE = emptyGO.AddComponent<LayoutElement>();
                emptyLE.preferredHeight = 40;
                emptyLE.minHeight = 40;
                rowCount = 1;
            }
            else
            {
                int count = Mathf.Min(entries.Count, 3);

                for (int i = 0; i < count; i++)
                {
                    var entry = entries[i];

                    // Divider between rows (not before first)
                    if (i > 0)
                    {
                        var div = OvertoneUI.CreateUIObject($"Div{i}", leaderboardRowContainer);
                        div.AddComponent<Image>().color = OC.border;
                        div.GetComponent<Image>().raycastTarget = false;
                        var divLE = div.AddComponent<LayoutElement>();
                        divLE.preferredHeight = 1;
                        divLE.minHeight = 1;
                    }

                    bool isMe = !string.IsNullOrEmpty(entry.device_uuid) &&
                                entry.device_uuid == GameSession.DeviceUUID;
                    if (isMe) playerInTop3 = true;

                    // Use rank from API (handles ties correctly)
                    string rankText = $"#{entry.rank}";
                    Color rankColor = entry.rank == 1 ? OC.amber
                        : entry.rank == 2 ? OC.A(Color.white, 0.5f)
                        : OC.A(OC.orange, 0.7f);

                    var rowGO = BuildScoreRow(
                        leaderboardRowContainer,
                        rankText, 8,
                        entry.display_name ?? "???",
                        entry.score.ToString("N0"),
                        isMe ? OC.cyan : OC.muted,
                        isMe ? OC.cyan : OC.A(Color.white, 0.35f),
                        isMe ? OC.A(OC.cyan, 0.06f) : Color.clear,
                        $"Row{i}",
                        true,
                        isMe ? OC.cyan : rankColor);
                }
                rowCount = count;
            }

            // "Your rank" row — only if played today AND not already in top 3
            bool hasPlayed = GameSession.HasPlayedToday ||
                (DailySeedManager.Instance != null && DailySeedManager.Instance.HasCompletedScoredAttempt());
            if (hasPlayed && !playerInTop3)
            {
                // Divider above your rank
                var yourDiv = OvertoneUI.CreateUIObject("YourDiv", leaderboardRowContainer);
                yourDiv.AddComponent<Image>().color = OC.border;
                yourDiv.GetComponent<Image>().raycastTarget = false;
                var ydLE = yourDiv.AddComponent<LayoutElement>();
                ydLE.preferredHeight = 1;
                ydLE.minHeight = 1;

                string rankText = GameSession.ResultRank > 0 ? $"#{GameSession.ResultRank}" : "#—";
                string playerName = GameSession.CurrentPlayer?.display_name ?? "YOU";

                // Use fallback for score if TodayScore is 0
                int yourScore = GameSession.TodayScore;
                if (yourScore <= 0 && ScoreManager.Instance != null)
                    yourScore = ScoreManager.Instance.HighScore;

                BuildScoreRow(
                    leaderboardRowContainer,
                    rankText, 7,                    // rank text, PressStart2P size 7
                    playerName,
                    yourScore.ToString("N0"),
                    OC.cyan, OC.cyan,               // all cyan
                    OC.A(OC.cyan, 0.04f),           // very subtle bg tint
                    "YourRank",
                    true,                           // use PressStart2P for rank
                    OC.cyan);                       // rank color cyan
                rowCount++;
            }

            // Resize wrapper: header(25) + rows(38 each) + dividers(~1 each)
            int dividers = Mathf.Max(0, rowCount - 1) + (hasPlayed ? 1 : 0);
            float totalHeight = 25 + rowCount * 38 + dividers;
            if (leaderboardWrapper != null)
            {
                var le = leaderboardWrapper.GetComponent<LayoutElement>();
                if (le != null) { le.preferredHeight = totalHeight; le.minHeight = totalHeight; }
            }
        }

        private GameObject BuildScoreRow(Transform parent, string rankText, float rankFontSize,
            string nameText, string scoreText, Color nameColor, Color scoreColor,
            Color bgColor, string name, bool usePixelFontForRank = false,
            Color? rankColor = null)
        {
            var row = OvertoneUI.CreateUIObject(name, parent);
            var rowLE = row.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 38;
            rowLE.minHeight = 38;
            rowLE.flexibleHeight = 0;

            // Background
            var rowBg = row.AddComponent<Image>();
            rowBg.color = bgColor;
            rowBg.raycastTarget = false;

            // Rank (left, 32pt wide)
            var rankGO = OvertoneUI.CreateUIObject("Rank", row.transform);
            var rankRT = rankGO.GetComponent<RectTransform>();
            rankRT.anchorMin = new Vector2(0, 0); rankRT.anchorMax = new Vector2(0, 1);
            rankRT.pivot = new Vector2(0, 0.5f);
            rankRT.anchoredPosition = new Vector2(10, 0);
            rankRT.sizeDelta = new Vector2(32, 0);
            var rankTMP = rankGO.AddComponent<TextMeshProUGUI>();
            rankTMP.text = rankText;
            rankTMP.font = usePixelFontForRank ? OvertoneUI.PressStart2P : OvertoneUI.DMMono;
            rankTMP.fontSize = rankFontSize;
            rankTMP.color = rankColor ?? (usePixelFontForRank ? nameColor : OC.dim);
            rankTMP.alignment = TextAlignmentOptions.Left;
            rankTMP.verticalAlignment = VerticalAlignmentOptions.Middle;
            rankTMP.textWrappingMode = TextWrappingModes.NoWrap;
            rankTMP.raycastTarget = false;

            // Name (center, flex)
            var nameGO = OvertoneUI.CreateUIObject("Name", row.transform);
            var nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0); nameRT.anchorMax = new Vector2(1, 1);
            nameRT.offsetMin = new Vector2(44, 0); nameRT.offsetMax = new Vector2(-90, 0);
            var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
            nameTMP.text = nameText;
            nameTMP.font = OvertoneUI.DMMono;
            nameTMP.fontSize = 13;
            nameTMP.color = nameColor;
            nameTMP.alignment = TextAlignmentOptions.Left;
            nameTMP.verticalAlignment = VerticalAlignmentOptions.Middle;
            nameTMP.textWrappingMode = TextWrappingModes.NoWrap;
            nameTMP.overflowMode = TextOverflowModes.Ellipsis;
            nameTMP.raycastTarget = false;

            // Score (right, 80pt)
            var scoreGO = OvertoneUI.CreateUIObject("Score", row.transform);
            var scoreRT = scoreGO.GetComponent<RectTransform>();
            scoreRT.anchorMin = new Vector2(1, 0); scoreRT.anchorMax = new Vector2(1, 1);
            scoreRT.pivot = new Vector2(1, 0.5f);
            scoreRT.anchoredPosition = new Vector2(-10, 0);
            scoreRT.sizeDelta = new Vector2(80, 0);
            var scoreTMP = scoreGO.AddComponent<TextMeshProUGUI>();
            scoreTMP.text = scoreText;
            scoreTMP.font = OvertoneUI.DMMono;
            scoreTMP.fontSize = 13;
            scoreTMP.color = scoreColor;
            scoreTMP.alignment = TextAlignmentOptions.Right;
            scoreTMP.verticalAlignment = VerticalAlignmentOptions.Middle;
            scoreTMP.textWrappingMode = TextWrappingModes.NoWrap;
            scoreTMP.raycastTarget = false;

            return row;
        }

        // ───── Helpers ─────

        private void AddFlex(Transform parent, float weight)
        {
            var flex = OvertoneUI.CreateUIObject("Flex", parent);
            var le = flex.AddComponent<LayoutElement>();
            le.flexibleHeight = weight;
            le.minHeight = 0;
        }

        private void AddSpacer(Transform parent, float height)
        {
            var spacer = OvertoneUI.CreateUIObject("Spacer", parent);
            var le = spacer.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
        }

        private static Sprite _outlineRoundedRect;
        protected static Sprite GetOutlineRoundedRect()
        {
            if (_outlineRoundedRect != null) return _outlineRoundedRect;

            int size = 64;
            int radius = 10;
            float borderWidth = 1.5f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            float center = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x - center + 0.5f) - (center - radius));
                    float dy = Mathf.Max(0, Mathf.Abs(y - center + 0.5f) - (center - radius));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);

                    // Inside the rounded rect
                    if (dist <= radius)
                    {
                        // Only draw the border ring, not the fill
                        float innerDist = radius - dist;
                        if (innerDist <= borderWidth)
                        {
                            float alpha = Mathf.Clamp01(innerDist / borderWidth);
                            // Also anti-alias outer edge
                            float outerAA = Mathf.Clamp01(radius - dist + 0.5f);
                            pixels[y * size + x] = new Color(1, 1, 1, alpha * outerAA);
                        }
                        // else: interior stays transparent
                    }
                    else if (dist <= radius + 1f)
                    {
                        // Anti-alias outer edge
                        float aa = Mathf.Clamp01(radius + 1f - dist);
                        pixels[y * size + x] = new Color(1, 1, 1, aa);
                    }
                }
            }

            tex.SetPixels(pixels);
            tex.Apply();

            _outlineRoundedRect = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), size); // PPU = size so it renders at 1 unit
            _outlineRoundedRect.name = "OutlineRoundedRect";
            return _outlineRoundedRect;
        }

        private static Sprite _smoothRoundedRect;
        protected static Sprite GetSmootherRoundedRect()
        {
            if (_smoothRoundedRect != null) return _smoothRoundedRect;

            int size = 64;
            int radius = 12;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

            float center = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    // Signed distance to rounded rect
                    float dx = Mathf.Max(0, Mathf.Abs(x - center + 0.5f) - (center - radius));
                    float dy = Mathf.Max(0, Mathf.Abs(y - center + 0.5f) - (center - radius));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= radius)
                        tex.SetPixel(x, y, Color.white);
                    else if (dist <= radius + 1f) // anti-alias edge
                        tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(radius + 1f - dist)));
                }
            }
            tex.Apply();

            var border = new Vector4(radius + 1, radius + 1, radius + 1, radius + 1);
            _smoothRoundedRect = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            _smoothRoundedRect.name = "SmoothRoundedRect";
            return _smoothRoundedRect;
        }

        private static Sprite GetCircleSprite()
        {
            if (_circleSprite != null) return _circleSprite;

            int size = 64;
            int radius = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;

            Color[] pixels = new Color[size * size];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;
            tex.SetPixels(pixels);

            float center = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - center + 0.5f;
                    float dy = y - center + 0.5f;
                    if (dx * dx + dy * dy <= radius * radius)
                        tex.SetPixel(x, y, Color.white);
                }
            }
            tex.Apply();

            _circleSprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f);
            _circleSprite.name = "CircleSprite";
            return _circleSprite;
        }
    }
}
