using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using MergeGame.Core;
using MergeGame.Data;
using MergeGame.Visual;
using MergeGame.Backend;

namespace MergeGame.UI
{
    /// <summary>
    /// Paused menu overlay — shows personal best, leaderboard, ball sizes, quit/resume.
    /// Builds itself procedurally on first enable.
    /// </summary>
    public class PausedScreen : MonoBehaviour
    {
        private bool isBuilt;
        private TextMeshProUGUI personalBestValue;
        private Transform leaderboardRowContainer;
        private GameObject leaderboardLoading;
        private void OnEnable()
        {
            if (!isBuilt) { BuildUI(); isBuilt = true; }
            if (DropController.Instance != null)
                DropController.Instance.DropBlocked = true;
            StartCoroutine(PopulateDeferred());
        }

        private IEnumerator PopulateDeferred()
        {
            yield return null;
            Populate();
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            if (DropController.Instance != null)
                DropController.Instance.DropBlocked = false;
        }

        // ───── Build ─────

        private void BuildUI()
        {
            var rt = gameObject.GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            // Dark scrim
            var scrim = gameObject.AddComponent<Image>();
            scrim.color = new Color(0.031f, 0.031f, 0.055f, 0.92f);
            scrim.raycastTarget = true;

            // Outer container — full screen with padding
            var outer = MurgeUI.CreateUIObject("Outer", transform);
            var outerRT = outer.GetComponent<RectTransform>();
            outerRT.anchorMin = Vector2.zero; outerRT.anchorMax = Vector2.one;
            outerRT.offsetMin = new Vector2(24, 24);
            outerRT.offsetMax = new Vector2(-24, -(OS.safeAreaTop + 16));
            var outerVLG = outer.AddComponent<VerticalLayoutGroup>();
            outerVLG.spacing = 16;
            outerVLG.childControlWidth = true;
            outerVLG.childControlHeight = true;
            outerVLG.childForceExpandWidth = true;
            outerVLG.childForceExpandHeight = false;

            // Header pinned at top
            BuildHeader(outer.transform);

            // Flex spacer above content to center it
            AddFlex(outer.transform, 1);

            // Content group — cards + buttons
            var contentGO = MurgeUI.CreateUIObject("Content", outer.transform);
            var contentVLG = contentGO.AddComponent<VerticalLayoutGroup>();
            contentVLG.spacing = 16;
            contentVLG.childControlWidth = true;
            contentVLG.childControlHeight = true;
            contentVLG.childForceExpandWidth = true;
            contentVLG.childForceExpandHeight = false;

            BuildPersonalBestCard(contentGO.transform);
            BuildLeaderboardCard(contentGO.transform);
            BuildBallSizesCard(contentGO.transform);
            BuildButtons(contentGO.transform);

            // Flex spacer below content to center it
            AddFlex(outer.transform, 1);
        }

        // ───── Header ─────

        private void BuildHeader(Transform parent)
        {
            var row = MurgeUI.CreateUIObject("Header", parent);
            var rowLE = row.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 40; rowLE.minHeight = 40;

            // PAUSED label
            var labelGO = MurgeUI.CreateUIObject("Label", row.transform);
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0, 0); labelRT.anchorMax = new Vector2(0.7f, 1);
            labelRT.offsetMin = Vector2.zero; labelRT.offsetMax = Vector2.zero;
            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = "PAUSED";
            label.font = MurgeUI.PressStart2P;
            label.fontSize = 18;
            label.color = Color.white;
            label.alignment = TextAlignmentOptions.Left;
            label.verticalAlignment = VerticalAlignmentOptions.Middle;
            label.raycastTarget = false;

        }

        // ───── Personal Best ─────

        private void BuildPersonalBestCard(Transform parent)
        {
            var card = MurgeUI.CreateCard(parent);
            card.name = "PersonalBestCard";
            var cardLE = card.AddComponent<LayoutElement>();
            cardLE.preferredHeight = 60; cardLE.minHeight = 60;

            // Label
            var labelGO = MurgeUI.CreateUIObject("Label", card.transform);
            var labelRT = labelGO.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0, 0); labelRT.anchorMax = new Vector2(0.5f, 1);
            labelRT.offsetMin = new Vector2(12, 0); labelRT.offsetMax = Vector2.zero;
            var label = labelGO.AddComponent<TextMeshProUGUI>();
            label.text = "PERSONAL BEST";
            label.font = MurgeUI.PressStart2P;
            label.fontSize = OFont.labelXs;
            label.color = OC.muted;
            label.alignment = TextAlignmentOptions.Left;
            label.verticalAlignment = VerticalAlignmentOptions.Middle;
            label.raycastTarget = false;

            // Value
            var valueGO = MurgeUI.CreateUIObject("Value", card.transform);
            var valueRT = valueGO.GetComponent<RectTransform>();
            valueRT.anchorMin = new Vector2(0.5f, 0); valueRT.anchorMax = new Vector2(1, 1);
            valueRT.offsetMin = Vector2.zero; valueRT.offsetMax = new Vector2(-12, 0);
            personalBestValue = valueGO.AddComponent<TextMeshProUGUI>();
            personalBestValue.text = "0";
            personalBestValue.font = MurgeUI.DMMono;
            personalBestValue.fontSize = 22;
            personalBestValue.color = OC.cyan;
            personalBestValue.alignment = TextAlignmentOptions.Right;
            personalBestValue.verticalAlignment = VerticalAlignmentOptions.Middle;
            personalBestValue.raycastTarget = false;
        }

        // ───── Leaderboard ─────

        private void BuildLeaderboardCard(Transform parent)
        {
            var card = MurgeUI.CreateCard(parent);
            card.name = "LeaderboardCard";

            // Header row
            var headerGO = MurgeUI.CreateUIObject("Header", card.transform);
            var hRT = headerGO.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.anchoredPosition = Vector2.zero;
            hRT.sizeDelta = new Vector2(0, 24);

            var headerLabel = MurgeUI.CreateLabel(headerGO.transform, "TODAY'S TOP",
                MurgeUI.PressStart2P, OFont.labelXs, OC.muted, "HeaderLabel");
            var hlRT = headerLabel.GetComponent<RectTransform>();
            hlRT.anchorMin = new Vector2(0, 0); hlRT.anchorMax = new Vector2(0.6f, 1);
            hlRT.offsetMin = new Vector2(10, 0); hlRT.offsetMax = Vector2.zero;
            headerLabel.alignment = TextAlignmentOptions.Left;
            headerLabel.verticalAlignment = VerticalAlignmentOptions.Middle;

            var dropLabel = MurgeUI.CreateLabel(headerGO.transform,
                $"DROP #{GameSession.TodayDayNumber}",
                MurgeUI.PressStart2P, OFont.labelXs, OC.muted, "DropLabel");
            var dlRT = dropLabel.GetComponent<RectTransform>();
            dlRT.anchorMin = new Vector2(0.4f, 0); dlRT.anchorMax = new Vector2(1, 1);
            dlRT.offsetMin = Vector2.zero; dlRT.offsetMax = new Vector2(-10, 0);
            dropLabel.alignment = TextAlignmentOptions.Right;
            dropLabel.verticalAlignment = VerticalAlignmentOptions.Middle;

            // Divider
            var divGO = MurgeUI.CreateUIObject("Divider", card.transform);
            var divRT = divGO.GetComponent<RectTransform>();
            divRT.anchorMin = new Vector2(0, 1); divRT.anchorMax = new Vector2(1, 1);
            divRT.pivot = new Vector2(0.5f, 1);
            divRT.anchoredPosition = new Vector2(0, -24);
            divRT.sizeDelta = new Vector2(0, 1);
            divGO.AddComponent<Image>().color = OC.border;

            // Row container
            var rowContainer = MurgeUI.CreateUIObject("Rows", card.transform);
            var rcRT = rowContainer.GetComponent<RectTransform>();
            rcRT.anchorMin = new Vector2(0, 0); rcRT.anchorMax = new Vector2(1, 1);
            rcRT.offsetMin = new Vector2(1, 0); rcRT.offsetMax = new Vector2(-1, -25);
            var rowVLG = rowContainer.AddComponent<VerticalLayoutGroup>();
            rowVLG.childControlWidth = true;
            rowVLG.childControlHeight = true;
            rowVLG.childForceExpandWidth = true;
            rowVLG.childForceExpandHeight = false;
            rowVLG.spacing = 0;
            leaderboardRowContainer = rowContainer.transform;

            // Loading
            leaderboardLoading = MurgeUI.CreateUIObject("Loading", rowContainer.transform);
            var loadTMP = leaderboardLoading.AddComponent<TextMeshProUGUI>();
            loadTMP.text = "...";
            loadTMP.font = MurgeUI.DMMono;
            loadTMP.fontSize = 11;
            loadTMP.color = OC.muted;
            loadTMP.alignment = TextAlignmentOptions.Center;
            var loadLE = leaderboardLoading.AddComponent<LayoutElement>();
            loadLE.preferredHeight = 38;

            // Dynamic height — will resize after populate
            var cardLE = card.AddComponent<LayoutElement>();
            cardLE.preferredHeight = 62; // header + loading
            cardLE.minHeight = 62;
        }

        // ───── Ball Sizes ─────

        private void BuildBallSizesCard(Transform parent)
        {
            var card = MurgeUI.CreateCard(parent);
            card.name = "BallSizesCard";

            // Header
            var headerGO = MurgeUI.CreateUIObject("Header", card.transform);
            var hRT = headerGO.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0, 1); hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.anchoredPosition = Vector2.zero;
            hRT.sizeDelta = new Vector2(0, 24);
            var headerLabel = MurgeUI.CreateLabel(headerGO.transform, "BALL SIZES",
                MurgeUI.PressStart2P, OFont.labelXs, OC.muted, "Label");
            var hlRT = headerLabel.GetComponent<RectTransform>();
            hlRT.anchorMin = Vector2.zero; hlRT.anchorMax = Vector2.one;
            hlRT.offsetMin = new Vector2(10, 0); hlRT.offsetMax = Vector2.zero;
            headerLabel.alignment = TextAlignmentOptions.Left;
            headerLabel.verticalAlignment = VerticalAlignmentOptions.Middle;

            // Ball rows container
            var ballArea = MurgeUI.CreateUIObject("BallArea", card.transform);
            var baRT = ballArea.GetComponent<RectTransform>();
            baRT.anchorMin = new Vector2(0, 0); baRT.anchorMax = new Vector2(1, 1);
            baRT.offsetMin = new Vector2(8, 8); baRT.offsetMax = new Vector2(-8, -28);
            var baVLG = ballArea.AddComponent<VerticalLayoutGroup>();
            baVLG.spacing = 8;
            baVLG.childControlWidth = true;
            baVLG.childControlHeight = true;
            baVLG.childForceExpandWidth = true;
            baVLG.childForceExpandHeight = true;
            baVLG.childAlignment = TextAnchor.MiddleCenter;

            // Row 1: tiers 10-6 (large to small)
            var row1 = MurgeUI.CreateUIObject("Row1", ballArea.transform);
            var r1HLG = row1.AddComponent<HorizontalLayoutGroup>();
            r1HLG.spacing = 4;
            r1HLG.childAlignment = TextAnchor.MiddleCenter;
            r1HLG.childControlWidth = false;
            r1HLG.childControlHeight = false;
            r1HLG.childForceExpandWidth = true;

            // Row 2: tiers 5-0
            var row2 = MurgeUI.CreateUIObject("Row2", ballArea.transform);
            var r2HLG = row2.AddComponent<HorizontalLayoutGroup>();
            r2HLG.spacing = 4;
            r2HLG.childAlignment = TextAnchor.MiddleCenter;
            r2HLG.childControlWidth = false;
            r2HLG.childControlHeight = false;
            r2HLG.childForceExpandWidth = true;

            float[] uiSizes = { 28f, 34f, 40f, 46f, 52f, 58f, 64f, 72f, 82f, 92f, 104f };
            float maxSize = 56f;

            for (int tier = 10; tier >= 0; tier--)
            {
                Transform targetRow = tier >= 6 ? row1.transform : row2.transform;
                float displaySize = Mathf.Min(uiSizes[tier] * 0.55f, maxSize);

                var ballGO = MurgeUI.CreateUIObject($"Ball{tier}", targetRow);
                var brt = ballGO.GetComponent<RectTransform>();
                brt.sizeDelta = new Vector2(displaySize, displaySize);

                float renderRadius = Mathf.Max(uiSizes[tier] / (2f * BallRenderer.PixelsPerUnit), 0.5f);
                var color = BallRenderer.GetBallColor(tier);
                float phase = tier * 0.09f;
                var pixels = BallRenderer.GenerateBallPixels(tier, color, renderRadius, phase, out int texSize);
                var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.SetPixels(pixels);
                tex.Apply(false, true);
                var sprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize),
                    new Vector2(0.5f, 0.5f), texSize);

                var img = ballGO.AddComponent<Image>();
                img.sprite = sprite;
                img.raycastTarget = false;
                img.preserveAspect = true;
            }

            var cardLE = card.AddComponent<LayoutElement>();
            cardLE.preferredHeight = 160; cardLE.minHeight = 160;
        }

        // ───── Buttons ─────

        private void BuildButtons(Transform parent)
        {
            // QUIT GAME — pink ghost button
            var quitGO = MurgeUI.CreateUIObject("QuitButton", parent);
            var quitBgImg = quitGO.AddComponent<Image>();
            quitBgImg.sprite = PixelUIGenerator.GetRoundedRect9Slice();
            quitBgImg.type = Image.Type.Sliced;
            quitBgImg.color = OC.A(OC.pink, 0.3f);
            // Fill
            var quitFill = MurgeUI.CreateUIObject("Fill", quitGO.transform);
            var qfRT = quitFill.GetComponent<RectTransform>();
            qfRT.anchorMin = Vector2.zero; qfRT.anchorMax = Vector2.one;
            qfRT.offsetMin = new Vector2(1, 1); qfRT.offsetMax = new Vector2(-1, -1);
            var qfImg = quitFill.AddComponent<Image>();
            qfImg.sprite = PixelUIGenerator.GetRoundedRect9Slice();
            qfImg.type = Image.Type.Sliced;
            qfImg.color = OC.bg;
            qfImg.raycastTarget = false;
            var quitBtn = quitGO.AddComponent<Button>();
            quitBtn.targetGraphic = quitBgImg;
            quitBtn.onClick.AddListener(OnQuitClicked);
            var quitLE = quitGO.AddComponent<LayoutElement>();
            quitLE.preferredHeight = 52; quitLE.minHeight = 52;
            // Label
            var quitLabelGO = MurgeUI.CreateUIObject("Label", quitGO.transform);
            MurgeUI.StretchFill(quitLabelGO.GetComponent<RectTransform>());
            var quitLabel = quitLabelGO.AddComponent<TextMeshProUGUI>();
            quitLabel.text = "QUIT GAME";
            quitLabel.font = MurgeUI.PressStart2P;
            quitLabel.fontSize = OFont.label;
            quitLabel.color = OC.pink;
            quitLabel.alignment = TextAlignmentOptions.Center;
            quitLabel.characterSpacing = 1;
            quitLabel.raycastTarget = false;

            // RESUME — primary cyan button
            var (resumeGO, resumeLabel) = MurgeUI.CreatePrimaryButton(parent, "RESUME", 52, "ResumeButton");
            resumeGO.GetComponent<Button>().onClick.AddListener(OnResumeClicked);
        }

        // ───── Populate ─────

        private void Populate()
        {
            // Personal best
            if (personalBestValue != null && ScoreManager.Instance != null)
                personalBestValue.text = ScoreManager.Instance.HighScore.ToString("N0");

            // Leaderboard
            PopulateLeaderboard();
        }

        private void PopulateLeaderboard()
        {
            if (leaderboardRowContainer == null) return;

            // Clear existing rows (except loading)
            for (int i = leaderboardRowContainer.childCount - 1; i >= 0; i--)
            {
                var child = leaderboardRowContainer.GetChild(i);
                if (child.gameObject != leaderboardLoading)
                    Destroy(child.gameObject);
            }

            if (LeaderboardService.Instance == null) return;

            // Read from cache only — no network request from pause menu
            var entries = LeaderboardService.Instance.CachedEntries;
            if (entries == null || entries.Count == 0)
            {
                if (leaderboardLoading != null)
                {
                    leaderboardLoading.SetActive(true);
                    leaderboardLoading.GetComponent<TextMeshProUGUI>().text =
                        !NetworkMonitor.IsOnline ? "You're offline" : "No scores yet today";
                }
                UpdateLeaderboardCardHeight(1);
                return;
            }

            if (leaderboardLoading != null) leaderboardLoading.SetActive(false);

            int count = Mathf.Min(entries.Count, 3);
            for (int i = 0; i < count; i++)
            {
                var entry = entries[i];
                if (i > 0)
                {
                    var div = MurgeUI.CreateUIObject($"Div{i}", leaderboardRowContainer);
                    div.AddComponent<Image>().color = OC.border;
                    div.GetComponent<Image>().raycastTarget = false;
                    var divLE = div.AddComponent<LayoutElement>();
                    divLE.preferredHeight = 1; divLE.minHeight = 1;
                }

                bool isMe = !string.IsNullOrEmpty(entry.device_uuid) &&
                            entry.device_uuid == GameSession.DeviceUUID;

                Color rankColor = entry.rank == 1 ? OC.amber
                    : entry.rank == 2 ? OC.A(Color.white, 0.5f)
                    : OC.A(OC.orange, 0.7f);

                BuildScoreRow(leaderboardRowContainer,
                    $"#{entry.rank}", entry.display_name ?? "???",
                    entry.score.ToString("N0"),
                    isMe ? OC.cyan : OC.muted,
                    isMe ? OC.cyan : OC.A(Color.white, 0.35f),
                    isMe ? OC.A(OC.cyan, 0.06f) : Color.clear,
                    isMe ? OC.cyan : rankColor);
            }

            UpdateLeaderboardCardHeight(count);
        }

        private void UpdateLeaderboardCardHeight(int rowCount)
        {
            // Rows → card (parent)
            var card = leaderboardRowContainer?.parent?.gameObject;
            if (card == null) return;
            var le = card.GetComponent<LayoutElement>();
            if (le == null) return;
            int dividers = Mathf.Max(0, rowCount - 1);
            le.preferredHeight = 25 + rowCount * 38 + dividers;
        }

        private void BuildScoreRow(Transform parent, string rankText, string nameText,
            string scoreText, Color nameColor, Color scoreColor, Color bgColor, Color rankColor)
        {
            var row = MurgeUI.CreateUIObject("Row", parent);
            var rowLE = row.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 38; rowLE.minHeight = 38;

            var rowBg = row.AddComponent<Image>();
            rowBg.color = bgColor;
            rowBg.raycastTarget = false;

            // Rank
            var rankGO = MurgeUI.CreateUIObject("Rank", row.transform);
            var rankRT = rankGO.GetComponent<RectTransform>();
            rankRT.anchorMin = new Vector2(0, 0); rankRT.anchorMax = new Vector2(0, 1);
            rankRT.pivot = new Vector2(0, 0.5f);
            rankRT.anchoredPosition = new Vector2(10, 0);
            rankRT.sizeDelta = new Vector2(32, 0);
            var rankTMP = rankGO.AddComponent<TextMeshProUGUI>();
            rankTMP.text = rankText;
            rankTMP.font = MurgeUI.PressStart2P;
            rankTMP.fontSize = 8;
            rankTMP.color = rankColor;
            rankTMP.alignment = TextAlignmentOptions.Left;
            rankTMP.verticalAlignment = VerticalAlignmentOptions.Middle;
            rankTMP.textWrappingMode = TextWrappingModes.NoWrap;
            rankTMP.raycastTarget = false;

            // Name
            var nameGO = MurgeUI.CreateUIObject("Name", row.transform);
            var nameRT = nameGO.GetComponent<RectTransform>();
            nameRT.anchorMin = new Vector2(0, 0); nameRT.anchorMax = new Vector2(1, 1);
            nameRT.offsetMin = new Vector2(44, 0); nameRT.offsetMax = new Vector2(-90, 0);
            var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
            nameTMP.text = nameText;
            nameTMP.font = MurgeUI.DMMono;
            nameTMP.fontSize = 13;
            nameTMP.color = nameColor;
            nameTMP.alignment = TextAlignmentOptions.Left;
            nameTMP.verticalAlignment = VerticalAlignmentOptions.Middle;
            nameTMP.textWrappingMode = TextWrappingModes.NoWrap;
            nameTMP.overflowMode = TextOverflowModes.Ellipsis;
            nameTMP.raycastTarget = false;

            // Score
            var scoreGO = MurgeUI.CreateUIObject("Score", row.transform);
            var scoreRT = scoreGO.GetComponent<RectTransform>();
            scoreRT.anchorMin = new Vector2(1, 0); scoreRT.anchorMax = new Vector2(1, 1);
            scoreRT.pivot = new Vector2(1, 0.5f);
            scoreRT.anchoredPosition = new Vector2(-10, 0);
            scoreRT.sizeDelta = new Vector2(80, 0);
            var scoreTMP = scoreGO.AddComponent<TextMeshProUGUI>();
            scoreTMP.text = scoreText;
            scoreTMP.font = MurgeUI.DMMono;
            scoreTMP.fontSize = 13;
            scoreTMP.color = scoreColor;
            scoreTMP.alignment = TextAlignmentOptions.Right;
            scoreTMP.verticalAlignment = VerticalAlignmentOptions.Middle;
            scoreTMP.textWrappingMode = TextWrappingModes.NoWrap;
            scoreTMP.raycastTarget = false;
        }

        // ───── Helpers ─────

        private void AddFlex(Transform parent, float weight)
        {
            var flex = MurgeUI.CreateUIObject("Flex", parent);
            var le = flex.AddComponent<LayoutElement>();
            le.flexibleHeight = weight;
            le.minHeight = 0;
        }

        // ───── Actions ─────

        private void OnResumeClicked()
        {
            // Unblock drops and reset input state so no extra taps are needed
            if (DropController.Instance != null)
            {
                DropController.Instance.DropBlocked = false;
                DropController.Instance.ResetInputState();
            }
            if (ScreenManager.Instance != null)
                ScreenManager.Instance.DismissOverlay();
        }

        private void OnQuitClicked()
        {
            if (ScreenManager.Instance != null)
                ScreenManager.Instance.DismissOverlay();
            if (GameManager.Instance != null)
                GameManager.Instance.TriggerGameOver();
        }
    }
}
