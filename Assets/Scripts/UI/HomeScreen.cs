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
    ///   - Logo block: OVER / TONE (stacked), A DAILY DROP, settings icon top-right
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

        public virtual void Refresh()
        {
            RefreshPuzzleRow();
            FetchLeaderboard();
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

            // === BOTTOM SECTION: puzzle row, leaderboard, CTA ===
            // Hook for subclass content (stats row for HomePlayed)
            BuildMiddleSection(content.transform);

            BuildPuzzleRow(content.transform);
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

            // Bottom padding
            AddSpacer(content.transform, 24);
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
            settingsRT.anchoredPosition = new Vector2(-20, -(OS.safeAreaTop + 12));
            settingsRT.sizeDelta = new Vector2(32, 32);

            // Background — subtle border square
            var bgImg = settingsGO.AddComponent<Image>();
            bgImg.sprite = Visual.PixelUIGenerator.GetRoundedRect9Slice();
            bgImg.type = Image.Type.Sliced;
            bgImg.color = Color.clear;

            // Border
            var outline = OvertoneUI.CreateUIObject("Outline", settingsGO.transform);
            var outlineImg = outline.AddComponent<Image>();
            outlineImg.sprite = Visual.PixelUIGenerator.GetRoundedRect9Slice();
            outlineImg.type = Image.Type.Sliced;
            outlineImg.color = OC.border;
            outlineImg.raycastTarget = false;
            OvertoneUI.StretchFill(outline.GetComponent<RectTransform>());

            // Gear icon — use a pixel-art gear sprite instead of a glyph
            var gearGO = OvertoneUI.CreateUIObject("GearIcon", settingsGO.transform);
            var gearImg = gearGO.AddComponent<Image>();
            gearImg.sprite = Visual.PixelUIGenerator.CreateGearIcon(16, OC.muted);
            gearImg.preserveAspect = true;
            gearImg.raycastTarget = false;
            var gearRT = gearGO.GetComponent<RectTransform>();
            gearRT.anchorMin = new Vector2(0.5f, 0.5f);
            gearRT.anchorMax = new Vector2(0.5f, 0.5f);
            gearRT.pivot = new Vector2(0.5f, 0.5f);
            gearRT.anchoredPosition = Vector2.zero;
            gearRT.sizeDelta = new Vector2(18, 18);

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
            overTMP.enableWordWrapping = false;
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
            toneTMP.enableWordWrapping = false;
            toneTMP.overflowMode = TextOverflowModes.Overflow;
            toneTMP.raycastTarget = false;

            // Tagline
            var tagGO = OvertoneUI.CreateUIObject("Tagline", inner.transform);
            tagGO.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 16);
            var tagTMP = tagGO.AddComponent<TextMeshProUGUI>();
            tagTMP.text = "A DAILY DROP";
            tagTMP.font = OvertoneUI.DMMono;
            tagTMP.fontSize = 10;
            tagTMP.color = OC.muted;
            tagTMP.characterSpacing = 5;
            tagTMP.enableWordWrapping = false;
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

            // Ball order: pink (76px), cyan (92px), amber (64px)
            int[] tiers = { 1, 0, 2 };
            float[] sizes = { 76f, 92f, 64f };

            for (int i = 0; i < 3; i++)
            {
                var ballGO = OvertoneUI.CreateUIObject($"ClusterBall{i}", cluster.transform);
                var ballRT = ballGO.GetComponent<RectTransform>();
                ballRT.sizeDelta = new Vector2(sizes[i], sizes[i]);

                var img = ballGO.AddComponent<Image>();
                Color color = OC.cyan;
                if (tierConfig != null)
                {
                    var data = tierConfig.GetTier(tiers[i]);
                    if (data != null) color = data.color;
                }

                // Smooth circle — same approach as onboarding
                img.sprite = GetCircleSprite();
                img.type = Image.Type.Simple;
                img.preserveAspect = true;
                img.color = color;

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
            dayNumberLabel.enableWordWrapping = false;
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
            dateLabel.enableWordWrapping = false;
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
            // Padded wrapper
            var wrapper = OvertoneUI.CreateUIObject("LeaderboardWrapper", parent);
            var wrapperVLG = wrapper.AddComponent<VerticalLayoutGroup>();
            wrapperVLG.padding = new RectOffset(24, 24, 0, 4);
            wrapperVLG.childControlWidth = true;
            wrapperVLG.childControlHeight = false;
            wrapperVLG.childForceExpandWidth = true;
            // No minHeight — let content determine size

            // Card
            var card = OvertoneUI.CreateCard(wrapper.transform, "LeaderboardCard");
            var cardVLG = card.AddComponent<VerticalLayoutGroup>();
            cardVLG.childControlWidth = true;
            cardVLG.childControlHeight = false;
            cardVLG.childForceExpandWidth = true;
            cardVLG.spacing = 0;

            var cardLE = card.GetComponent<LayoutElement>();
            if (cardLE == null) cardLE = card.AddComponent<LayoutElement>();
            cardLE.flexibleWidth = 1;

            // Header row
            var header = OvertoneUI.CreateUIObject("CardHeader", card.transform);
            var headerHLG = header.AddComponent<HorizontalLayoutGroup>();
            headerHLG.spacing = 0;
            headerHLG.childAlignment = TextAnchor.MiddleLeft;
            headerHLG.childControlWidth = true;
            headerHLG.childControlHeight = true;
            headerHLG.childForceExpandWidth = false;
            headerHLG.padding = new RectOffset(10, 10, 7, 7);
            header.AddComponent<LayoutElement>().preferredHeight = 28;

            var headerLabel = OvertoneUI.CreateLabel(header.transform, "TODAY'S TOP",
                OvertoneUI.PressStart2P, OFont.labelXs, OC.muted, "HeaderLabel");
            headerLabel.enableWordWrapping = false;

            var headerSpacer = OvertoneUI.CreateUIObject("Spacer", header.transform);
            headerSpacer.AddComponent<LayoutElement>().flexibleWidth = 1;

            var allBtnGO = OvertoneUI.CreateUIObject("AllButton", header.transform);
            var allTMP = allBtnGO.AddComponent<TextMeshProUGUI>();
            allTMP.text = "ALL \u2192";
            allTMP.font = OvertoneUI.PressStart2P;
            allTMP.fontSize = OFont.labelXs;
            allTMP.color = OC.cyan;
            allTMP.enableWordWrapping = false;
            allTMP.alignment = TextAlignmentOptions.Right;
            var allBtn = allBtnGO.AddComponent<Button>();
            allBtn.onClick.AddListener(() =>
            {
                if (ScreenManager.Instance != null)
                    ScreenManager.Instance.NavigateTo(Screen.Leaderboard);
            });

            OvertoneUI.CreateDivider(card.transform, "HeaderDivider");

            // Row container
            var rowContainer = OvertoneUI.CreateUIObject("Rows", card.transform);
            var rowVLG = rowContainer.AddComponent<VerticalLayoutGroup>();
            rowVLG.childControlWidth = true;
            rowVLG.childControlHeight = false;
            rowVLG.childForceExpandWidth = true;
            rowVLG.spacing = 0;
            leaderboardRowContainer = rowContainer.transform;

            // Loading indicator
            loadingIndicator = OvertoneUI.CreateUIObject("Loading", card.transform);
            var loadingTMP = loadingIndicator.AddComponent<TextMeshProUGUI>();
            loadingTMP.text = "...";
            loadingTMP.font = OvertoneUI.DMMono;
            loadingTMP.fontSize = OFont.body;
            loadingTMP.color = OC.muted;
            loadingTMP.alignment = TextAlignmentOptions.Center;
            loadingIndicator.AddComponent<LayoutElement>().preferredHeight = 32;
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
            if (loadingIndicator != null) loadingIndicator.SetActive(true);

            if (LeaderboardService.Instance != null)
            {
                LeaderboardService.Instance.FetchLeaderboard(GameSession.TodayDateStr, (entries) =>
                {
                    cachedEntries = entries;
                    if (loadingIndicator != null) loadingIndicator.SetActive(false);
                    PopulateLeaderboardRows(entries);
                });
            }
            else
            {
                if (loadingIndicator != null) loadingIndicator.SetActive(false);
            }
        }

        protected virtual void PopulateLeaderboardRows(List<LeaderboardEntry> entries)
        {
            if (leaderboardRowContainer == null) return;

            // Always hide loading indicator when populating
            if (loadingIndicator != null) loadingIndicator.SetActive(false);

            foreach (Transform child in leaderboardRowContainer)
                Destroy(child.gameObject);

            if (entries == null || entries.Count == 0)
            {
                // Empty state
                var emptyGO = OvertoneUI.CreateUIObject("EmptyState", leaderboardRowContainer);
                var emptyTMP = emptyGO.AddComponent<TextMeshProUGUI>();
                emptyTMP.text = "No scores yet today";
                emptyTMP.font = OvertoneUI.DMMono;
                emptyTMP.fontSize = OFont.bodySm;
                emptyTMP.color = OC.dim;
                emptyTMP.alignment = TextAlignmentOptions.Center;
                emptyTMP.raycastTarget = false;
                emptyGO.AddComponent<LayoutElement>().preferredHeight = 32;
                return;
            }

            string[] medals = { "\U0001F947", "\U0001F948", "\U0001F949" };

            int count = Mathf.Min(entries.Count, 3);
            for (int i = 0; i < count; i++)
            {
                var entry = entries[i];
                var (rowGO, rankTMP, nameTMP, scoreTMP) =
                    OvertoneUI.CreateLeaderboardRow(leaderboardRowContainer, $"Row{i}");

                string rankText = i < 3 ? medals[i] : $"#{entry.rank}";
                rankTMP.text = rankText;
                if (i < 3) rankTMP.color = OC.amber;
                nameTMP.text = entry.display_name ?? "???";
                scoreTMP.text = entry.score.ToString("N0");

                bool isMe = !string.IsNullOrEmpty(entry.device_uuid) &&
                            entry.device_uuid == GameSession.DeviceUUID;
                if (isMe)
                    OvertoneUI.HighlightLeaderboardRow(rowGO, rankTMP, nameTMP, scoreTMP);
            }
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
