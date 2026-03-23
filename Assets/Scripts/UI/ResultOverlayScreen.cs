using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using MergeGame.Core;
using MergeGame.Data;
using MergeGame.Backend;

namespace MergeGame.UI
{
    public class ResultOverlayScreen : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BallTierConfig tierConfig;

        private TextMeshProUGUI scoreValue;
        private TextMeshProUGUI finalScoreLabel;
        private TextMeshProUGUI rankLabel;
        private TextMeshProUGUI rankSubLabel;
        private Transform mergeRow;
        private CanvasGroup contentCG;
        private RectTransform contentPanel;
        private GameObject rankWrapper;
        private GameObject practiceInfoWrapper;
        private TextMeshProUGUI practiceLabel;
        private TextMeshProUGUI todayScoreLabel;
        private GameObject shareBtn;
        private GameObject playAgainBtn;
        private Image playAgainBg;
        private bool isBuilt;

        // Ball sizes per level (largest=level1 to smallest=level11)
        // In code: tier10=level1, tier0=level11
        private static readonly float[] BaseSizes = { 14, 18, 22, 26, 32, 38, 46, 54, 64, 76, 92 };

        private Image backdropImage;

        private void OnEnable()
        {
            if (!isBuilt) { BuildUI(); isBuilt = true; }
            Populate();
            // Don't fetch rank here — GameManager fetches it after score submission completes
            // and calls Populate() to update the UI
            StartCoroutine(CaptureAndBlur());
        }

        private IEnumerator CaptureAndBlur()
        {
            // Hide overlay briefly so we capture the game screen underneath
            if (backdropImage != null) backdropImage.color = Color.clear;
            if (contentCG != null) contentCG.alpha = 0f;

            yield return new WaitForEndOfFrame();

            // Capture screen
            // Capture screen pixels directly (fully qualify Screen to avoid enum conflict)
            int sw = UnityEngine.Screen.width;
            int sh = UnityEngine.Screen.height;
            var screenTex = new Texture2D(sw, sh, TextureFormat.RGB24, false);
            screenTex.ReadPixels(new Rect(0, 0, sw, sh), 0, 0);
            screenTex.Apply();

            // Downscale + blur for performance (blur at 1/4 resolution)
            int blurW = screenTex.width / 4;
            int blurH = screenTex.height / 4;
            var small = new RenderTexture(blurW, blurH, 0);
            Graphics.Blit(screenTex, small);

            // Read back and apply box blur
            var blurTex = new Texture2D(blurW, blurH, TextureFormat.RGB24, false);
            RenderTexture.active = small;
            blurTex.ReadPixels(new Rect(0, 0, blurW, blurH), 0, 0);
            blurTex.Apply();
            RenderTexture.active = null;

            // Simple 2-pass box blur
            BoxBlur(blurTex, 3);
            BoxBlur(blurTex, 3);

            // Apply as backdrop sprite with dark tint
            if (backdropImage != null)
            {
                var sprite = Sprite.Create(blurTex, new Rect(0, 0, blurW, blurH),
                    new Vector2(0.5f, 0.5f));
                backdropImage.sprite = sprite;
                backdropImage.type = Image.Type.Simple;
                backdropImage.color = new Color(0.4f, 0.4f, 0.45f, 1f); // darken the blurred image
            }

            // Cleanup
            Object.Destroy(screenTex);
            small.Release();
            Object.Destroy(small);

            // Now run entrance animation
            StartCoroutine(EntranceAnimation());
        }

        private static void BoxBlur(Texture2D tex, int radius)
        {
            int w = tex.width;
            int h = tex.height;
            var src = tex.GetPixels();
            var dst = new Color[src.Length];

            // Horizontal pass
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color sum = Color.clear;
                    int count = 0;
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int sx = Mathf.Clamp(x + dx, 0, w - 1);
                        sum += src[y * w + sx];
                        count++;
                    }
                    dst[y * w + x] = sum / count;
                }
            }

            // Vertical pass
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    Color sum = Color.clear;
                    int count = 0;
                    for (int dy = -radius; dy <= radius; dy++)
                    {
                        int sy = Mathf.Clamp(y + dy, 0, h - 1);
                        sum += dst[sy * w + x];
                        count++;
                    }
                    src[y * w + x] = sum / count;
                }
            }

            tex.SetPixels(src);
            tex.Apply();
        }

        private IEnumerator EntranceAnimation()
        {
            if (contentCG == null) yield break;
            contentCG.alpha = 0f;
            if (contentPanel != null) contentPanel.localScale = Vector3.one * 0.96f;
            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / 0.3f), 2f);
                contentCG.alpha = t;
                if (contentPanel != null)
                    contentPanel.localScale = Vector3.Lerp(Vector3.one * 0.96f, Vector3.one, t);
                yield return null;
            }
            contentCG.alpha = 1f;
            if (contentPanel != null) contentPanel.localScale = Vector3.one;
        }

        public void Populate()
        {
            bool isPractice = GameSession.IsPractice;
            Color amber = OC.amber; // #F0B429

            // Score value — cyan for scored, amber for practice
            if (scoreValue != null)
            {
                int displayScore = isPractice
                    ? (ScoreManager.Instance != null ? ScoreManager.Instance.CurrentScore : 0)
                    : GameSession.TodayScore;
                scoreValue.text = displayScore.ToString("N0");
                scoreValue.color = isPractice ? amber : OC.cyan;
            }

            // Header label
            if (finalScoreLabel != null)
                finalScoreLabel.text = isPractice ? "PRACTICE SCORE" : "FINAL SCORE";

            // Rank pill — only for scored games
            if (rankWrapper != null)
                rankWrapper.SetActive(!isPractice);
            if (!isPractice)
            {
                int rank = GameSession.ResultRank;
                int total = GameSession.ResultTotalPlayers;
                if (rankLabel != null)
                    rankLabel.text = rank > 0 ? $"#{rank}" : "#—";
                if (rankSubLabel != null)
                    rankSubLabel.text = rank > 0 && total > 0
                        ? $"of {total} players today"
                        : "ranking...";
            }

            // Practice info — only for practice games
            if (practiceInfoWrapper != null)
                practiceInfoWrapper.SetActive(isPractice);
            if (isPractice && todayScoreLabel != null)
            {
                string cHex = ColorUtility.ToHtmlStringRGB(OC.cyan);
                todayScoreLabel.text = $"TODAY  <color=#{cHex}>{GameSession.TodayScore.ToString("N0")}</color>";
            }

            // Buttons — SHARE for scored, PLAY AGAIN for practice
            if (shareBtn != null) shareBtn.SetActive(!isPractice);
            if (playAgainBtn != null) playAgainBtn.SetActive(isPractice);

            PopulateMergeRow();
        }

        private void FetchRank()
        {
            if (GameSession.IsPractice) return; // no rank for practice games

            if (LeaderboardService.Instance == null || PlayerIdentity.Instance == null)
            {
                Debug.Log("[ResultOverlay] FetchRank skipped: no LeaderboardService or PlayerIdentity");
                return;
            }

            Debug.Log($"[ResultOverlay] FetchRank: date={GameSession.TodayDateStr}");

            LeaderboardService.Instance.FetchPlayerRankFull(GameSession.TodayDateStr, (rank, totalPlayers) =>
            {
                Debug.Log($"[ResultOverlay] FetchRank result: rank={rank}, total={totalPlayers}, active={gameObject?.activeInHierarchy}");

                if (this == null || !gameObject.activeInHierarchy) return;
                GameSession.ResultRank = rank;
                GameSession.ResultTotalPlayers = totalPlayers;
                if (rankLabel != null)
                    rankLabel.text = rank > 0 ? $"#{rank}" : "#—";
                if (rankSubLabel != null)
                    rankSubLabel.text = rank > 0 && totalPlayers > 0
                        ? $"of {totalPlayers} players today"
                        : "ranking...";
            });
        }

        private void BuildUI()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();

            // ===== FULL-SCREEN BACKDROP =====
            // Must cover EVERYTHING — anchor to canvas edges with zero inset
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            backdropImage = gameObject.GetComponent<Image>();
            if (backdropImage == null) backdropImage = gameObject.AddComponent<Image>();
            backdropImage.color = new Color(0.031f, 0.031f, 0.055f, 0.96f); // fallback until blur loads

            // Ensure this overlay renders on top of all siblings
            transform.SetAsLastSibling();

            // ===== CONTENT PANEL =====
            var panel = OvertoneUI.CreateUIObject("ContentPanel", transform);
            contentPanel = panel.GetComponent<RectTransform>();
            contentPanel.anchorMin = Vector2.zero;
            contentPanel.anchorMax = Vector2.one;
            contentPanel.offsetMin = new Vector2(28, 0);
            contentPanel.offsetMax = new Vector2(-28, 0);
            contentCG = panel.AddComponent<CanvasGroup>();

            var vlg = panel.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 0;

            // Top flex — centers content vertically
            AddFlex(panel.transform, 1f);

            // FINAL SCORE / PRACTICE SCORE label
            finalScoreLabel = OvertoneUI.CreateLabel(panel.transform, "FINAL SCORE",
                OvertoneUI.PressStart2P, 8, OC.muted, "FinalScoreLabel");
            finalScoreLabel.characterSpacing = 3;
            finalScoreLabel.alignment = TextAlignmentOptions.Center;
            finalScoreLabel.enableWordWrapping = false;
            finalScoreLabel.overflowMode = TextOverflowModes.Ellipsis;
            var flLE = finalScoreLabel.gameObject.AddComponent<LayoutElement>();
            flLE.preferredHeight = 14; flLE.minHeight = 14;

            AddSpacer(panel.transform, 10);

            // Score value
            scoreValue = OvertoneUI.CreateLabel(panel.transform, "0",
                OvertoneUI.DMMono, 48, OC.cyan, "ScoreValue");
            scoreValue.alignment = TextAlignmentOptions.Center;
            scoreValue.enableWordWrapping = false;
            scoreValue.overflowMode = TextOverflowModes.Overflow;
            scoreValue.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 56);
            var svLE = scoreValue.gameObject.AddComponent<LayoutElement>();
            svLE.preferredHeight = 56; svLE.minHeight = 56;

            AddSpacer(panel.transform, 8);

            // Rank badge (scored mode)
            BuildRankBadge(panel.transform);

            // Practice info (practice mode)
            BuildPracticeInfo(panel.transform);

            AddSpacer(panel.transform, 16);

            // Merges card
            BuildMergeCard(panel.transform);

            AddSpacer(panel.transform, 16);

            // Button row
            BuildButtonRow(panel.transform);

            // Bottom flex
            AddFlex(panel.transform, 1f);
        }

        private void BuildRankBadge(Transform parent)
        {
            // Centering wrapper (scored mode only)
            var wrapper = OvertoneUI.CreateUIObject("RankWrapper", parent);
            rankWrapper = wrapper;
            var wrapHLG = wrapper.AddComponent<HorizontalLayoutGroup>();
            wrapHLG.childAlignment = TextAnchor.MiddleCenter;
            wrapHLG.childControlWidth = false;
            wrapHLG.childControlHeight = false;
            wrapHLG.childForceExpandWidth = false;
            var wLE = wrapper.AddComponent<LayoutElement>();
            wLE.preferredHeight = 34; wLE.minHeight = 34;

            // Pill — content-width, centered by wrapper
            var pill = OvertoneUI.CreateUIObject("RankPill", wrapper.transform);
            var pillRT = pill.GetComponent<RectTransform>();
            pillRT.sizeDelta = new Vector2(240, 34);

            // Border (cyan @ 28% — very subtle)
            var borderGO = OvertoneUI.CreateUIObject("Border", pill.transform);
            OvertoneUI.StretchFill(borderGO.GetComponent<RectTransform>());
            var bdrImg = borderGO.AddComponent<Image>();
            bdrImg.sprite = OvertoneUI.SmoothRoundedRect;
            bdrImg.type = Image.Type.Sliced;
            bdrImg.color = new Color(0.302f, 0.851f, 0.753f, 0.28f); // #4DD9C0 @ 28%
            bdrImg.raycastTarget = false;

            // Fill inset (cyan @ 12% — barely visible)
            var fillGO = OvertoneUI.CreateUIObject("Fill", pill.transform);
            var fRT = fillGO.GetComponent<RectTransform>();
            fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one;
            fRT.offsetMin = new Vector2(1, 1); fRT.offsetMax = new Vector2(-1, -1);
            var fImg = fillGO.AddComponent<Image>();
            fImg.sprite = OvertoneUI.SmoothRoundedRect;
            fImg.type = Image.Type.Sliced;
            fImg.color = new Color32(18, 38, 34, 255); // #122622 — solid composited color
            fImg.raycastTarget = false;

            // Rank label (left)
            rankLabel = OvertoneUI.CreateLabel(pill.transform, "#—",
                OvertoneUI.PressStart2P, 8, OC.cyan, "RankLabel");
            rankLabel.characterSpacing = 1;
            rankLabel.enableWordWrapping = false;
            rankLabel.overflowMode = TextOverflowModes.Ellipsis;
            rankLabel.alignment = TextAlignmentOptions.Right;
            rankLabel.verticalAlignment = VerticalAlignmentOptions.Middle;
            var rlRT = rankLabel.GetComponent<RectTransform>();
            rlRT.anchorMin = new Vector2(0, 0); rlRT.anchorMax = new Vector2(0.3f, 1);
            rlRT.offsetMin = new Vector2(16, 0); rlRT.offsetMax = Vector2.zero;

            // Sub label (right)
            rankSubLabel = OvertoneUI.CreateLabel(pill.transform, "ranking...",
                OvertoneUI.DMMono, 12, OC.muted, "RankSub");
            rankSubLabel.enableWordWrapping = false;
            rankSubLabel.overflowMode = TextOverflowModes.Ellipsis;
            rankSubLabel.alignment = TextAlignmentOptions.Left;
            rankSubLabel.verticalAlignment = VerticalAlignmentOptions.Middle;
            var rsRT = rankSubLabel.GetComponent<RectTransform>();
            rsRT.anchorMin = new Vector2(0.3f, 0); rsRT.anchorMax = new Vector2(1, 1);
            rsRT.offsetMin = new Vector2(8, 0); rsRT.offsetMax = new Vector2(-16, 0);
        }

        private void BuildPracticeInfo(Transform parent)
        {
            practiceInfoWrapper = OvertoneUI.CreateUIObject("PracticeInfo", parent);
            var vlg = practiceInfoWrapper.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 8;

            // "PRACTICE score not counted" pill
            var pillWrapper = OvertoneUI.CreateUIObject("PracticePillWrap", practiceInfoWrapper.transform);
            var pwHLG = pillWrapper.AddComponent<HorizontalLayoutGroup>();
            pwHLG.childAlignment = TextAnchor.MiddleCenter;
            pwHLG.childControlWidth = false;
            pwHLG.childControlHeight = false;
            pwHLG.childForceExpandWidth = false;
            pillWrapper.AddComponent<LayoutElement>().preferredHeight = 34;

            var pill = OvertoneUI.CreateUIObject("PracticePill", pillWrapper.transform);
            pill.GetComponent<RectTransform>().sizeDelta = new Vector2(230, 34);
            // Border (amber @ 28%)
            var pBorder = OvertoneUI.CreateUIObject("Border", pill.transform);
            OvertoneUI.StretchFill(pBorder.GetComponent<RectTransform>());
            var pbImg = pBorder.AddComponent<Image>();
            pbImg.sprite = OvertoneUI.SmoothRoundedRect;
            pbImg.type = Image.Type.Sliced;
            pbImg.color = new Color(0.941f, 0.706f, 0.161f, 0.28f); // #F0B429 @ 28%
            pbImg.raycastTarget = false;
            // Fill (solid composited)
            var pFill = OvertoneUI.CreateUIObject("Fill", pill.transform);
            var pfRT = pFill.GetComponent<RectTransform>();
            pfRT.anchorMin = Vector2.zero; pfRT.anchorMax = Vector2.one;
            pfRT.offsetMin = new Vector2(1, 1); pfRT.offsetMax = new Vector2(-1, -1);
            var pfImg = pFill.AddComponent<Image>();
            pfImg.sprite = OvertoneUI.SmoothRoundedRect;
            pfImg.type = Image.Type.Sliced;
            pfImg.color = new Color32(22, 22, 16, 255); // composited amber @ 12% over dark bg
            pfImg.raycastTarget = false;
            // Text: "PRACTICE  score not counted"
            practiceLabel = OvertoneUI.CreateLabel(pill.transform, "",
                OvertoneUI.PressStart2P, 8, OC.muted, "PracticeText");
            string amberHex = ColorUtility.ToHtmlStringRGB(OC.amber);
            practiceLabel.text = $"<color=#{amberHex}>PRACTICE</color>  score not counted";
            practiceLabel.richText = true;
            practiceLabel.alignment = TextAlignmentOptions.Center;
            practiceLabel.verticalAlignment = VerticalAlignmentOptions.Middle;
            practiceLabel.enableWordWrapping = false;
            OvertoneUI.StretchFill(practiceLabel.GetComponent<RectTransform>());

            // TODAY score box
            var todayBox = OvertoneUI.CreateUIObject("TodayBox", practiceInfoWrapper.transform);
            var tbLE = todayBox.AddComponent<LayoutElement>();
            tbLE.preferredHeight = 38; tbLE.minHeight = 38;
            // Border
            var tbBorder = OvertoneUI.CreateUIObject("Border", todayBox.transform);
            OvertoneUI.StretchFill(tbBorder.GetComponent<RectTransform>());
            var tbbImg = tbBorder.AddComponent<Image>();
            tbbImg.sprite = OvertoneUI.SmoothRoundedRect;
            tbbImg.type = Image.Type.Sliced;
            tbbImg.color = OC.border;
            tbbImg.raycastTarget = false;
            // Fill
            var tbFill = OvertoneUI.CreateUIObject("Fill", todayBox.transform);
            var tbfRT = tbFill.GetComponent<RectTransform>();
            tbfRT.anchorMin = Vector2.zero; tbfRT.anchorMax = Vector2.one;
            tbfRT.offsetMin = new Vector2(1, 1); tbfRT.offsetMax = new Vector2(-1, -1);
            var tbfImg = tbFill.AddComponent<Image>();
            tbfImg.sprite = OvertoneUI.SmoothRoundedRect;
            tbfImg.type = Image.Type.Sliced;
            tbfImg.color = OC.surface;
            tbfImg.raycastTarget = false;
            // Content: lock + TODAY + score
            var todayContent = OvertoneUI.CreateUIObject("Content", todayBox.transform);
            OvertoneUI.StretchFill(todayContent.GetComponent<RectTransform>());
            todayScoreLabel = todayContent.AddComponent<TextMeshProUGUI>();
            string cyanHex = ColorUtility.ToHtmlStringRGB(OC.cyan);
            todayScoreLabel.text = $"TODAY  <color=#{cyanHex}>0</color>";
            todayScoreLabel.font = OvertoneUI.PressStart2P;
            todayScoreLabel.fontSize = 9;
            todayScoreLabel.color = OC.muted;
            todayScoreLabel.characterSpacing = 2;
            todayScoreLabel.alignment = TextAlignmentOptions.Center;
            todayScoreLabel.verticalAlignment = VerticalAlignmentOptions.Middle;
            todayScoreLabel.richText = true;
            todayScoreLabel.enableWordWrapping = false;
            todayScoreLabel.raycastTarget = false;

            practiceInfoWrapper.SetActive(false); // hidden by default
        }

        private void BuildMergeCard(Transform parent)
        {
            var card = OvertoneUI.CreateUIObject("MergeCard", parent);
            var cardLE = card.AddComponent<LayoutElement>();
            cardLE.preferredHeight = 90; cardLE.minHeight = 70;

            // Border
            var borderGO = OvertoneUI.CreateUIObject("Border", card.transform);
            OvertoneUI.StretchFill(borderGO.GetComponent<RectTransform>());
            borderGO.AddComponent<Image>().color = OC.border;
            borderGO.GetComponent<Image>().sprite = OvertoneUI.SmoothRoundedRect;
            borderGO.GetComponent<Image>().type = Image.Type.Sliced;
            borderGO.GetComponent<Image>().raycastTarget = false;

            // Fill
            var fillGO = OvertoneUI.CreateUIObject("Fill", card.transform);
            var fRT = fillGO.GetComponent<RectTransform>();
            fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one;
            fRT.offsetMin = new Vector2(1, 1); fRT.offsetMax = new Vector2(-1, -1);
            fillGO.AddComponent<Image>().color = OC.surface;
            fillGO.GetComponent<Image>().sprite = OvertoneUI.SmoothRoundedRect;
            fillGO.GetComponent<Image>().type = Image.Type.Sliced;
            fillGO.GetComponent<Image>().raycastTarget = false;

            // MERGES label
            var mergesLabel = OvertoneUI.CreateLabel(card.transform, "MERGES",
                OvertoneUI.PressStart2P, 7, OC.dim, "MergesLabel");
            mergesLabel.characterSpacing = 1;
            mergesLabel.enableWordWrapping = false;
            mergesLabel.overflowMode = TextOverflowModes.Ellipsis;
            var mlRT = mergesLabel.GetComponent<RectTransform>();
            mlRT.anchorMin = new Vector2(0, 1); mlRT.anchorMax = new Vector2(1, 1);
            mlRT.pivot = new Vector2(0, 1);
            mlRT.anchoredPosition = new Vector2(14, -10);
            mlRT.sizeDelta = new Vector2(-28, 12);

            // Horizontal ball row
            var rowGO = OvertoneUI.CreateUIObject("BallRow", card.transform);
            var rowRT = rowGO.GetComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0, 0); rowRT.anchorMax = new Vector2(1, 1);
            rowRT.offsetMin = new Vector2(8, 6); rowRT.offsetMax = new Vector2(-8, -26);
            var rowHLG = rowGO.AddComponent<HorizontalLayoutGroup>();
            rowHLG.spacing = 4;
            rowHLG.childAlignment = TextAnchor.MiddleCenter;
            rowHLG.childControlWidth = false;
            rowHLG.childControlHeight = false;
            rowHLG.childForceExpandWidth = false;
            mergeRow = rowGO.transform;
        }

        private void PopulateMergeRow()
        {
            if (mergeRow == null) return;

            foreach (Transform child in mergeRow)
                Destroy(child.gameObject);

            int[] counts = GameSession.MergeCounts ?? new int[11];

            // Tier 10 = level 1 (largest) down to tier 0 = level 11 (smallest)
            for (int tier = 10; tier >= 0; tier--)
            {
                int count = tier < counts.Length ? counts[tier] : 0;
                Color ballColor = Visual.NeonBallRenderer.GetBallColor(tier);

                float baseSize = BaseSizes[tier];
                float gridSize = Mathf.Max(16f, baseSize * 0.38f);

                var cell = OvertoneUI.CreateUIObject($"Ball{tier}", mergeRow);
                var cellRT = cell.GetComponent<RectTransform>();
                cellRT.sizeDelta = new Vector2(gridSize + 2, gridSize + 12);

                if (count == 0)
                {
                    var cg = cell.AddComponent<CanvasGroup>();
                    cg.alpha = 0.25f;
                }

                // Ball
                var ballGO = OvertoneUI.CreateUIObject("Ball", cell.transform);
                var ballRT = ballGO.GetComponent<RectTransform>();
                ballRT.anchorMin = new Vector2(0.5f, 1); ballRT.anchorMax = new Vector2(0.5f, 1);
                ballRT.pivot = new Vector2(0.5f, 1);
                ballRT.anchoredPosition = Vector2.zero;
                ballRT.sizeDelta = new Vector2(gridSize, gridSize);

                var ballImg = ballGO.AddComponent<Image>();
                float uiRadius = gridSize / (2f * 48f);
                var pixels = Visual.NeonBallRenderer.GenerateBallPixels(
                    tier, ballColor, uiRadius, 0f, out int texSize);
                var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.SetPixels(pixels);
                tex.Apply();
                ballImg.sprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize),
                    new Vector2(0.5f, 0.5f), texSize);
                ballImg.preserveAspect = true;
                ballImg.color = Color.white;

                // Count label (only if count > 0)
                if (count > 0)
                {
                    var countGO = OvertoneUI.CreateUIObject("Count", cell.transform);
                    var countRT = countGO.GetComponent<RectTransform>();
                    countRT.anchorMin = new Vector2(0.5f, 0);
                    countRT.anchorMax = new Vector2(0.5f, 0);
                    countRT.pivot = new Vector2(0.5f, 0);
                    countRT.anchoredPosition = Vector2.zero;
                    countRT.sizeDelta = new Vector2(gridSize + 2, 10);
                    var countTMP = countGO.AddComponent<TextMeshProUGUI>();
                    countTMP.text = $"\u00D7{count}";
                    countTMP.font = OvertoneUI.PressStart2P;
                    countTMP.fontSize = 6;
                    countTMP.color = ballColor;
                    countTMP.alignment = TextAlignmentOptions.Center;
                    countTMP.raycastTarget = false;
                }
            }
        }

        private void BuildButtonRow(Transform parent)
        {
            var row = OvertoneUI.CreateUIObject("ButtonRow", parent);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            var rowLE = row.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 34; rowLE.minHeight = 34;
            rowLE.flexibleHeight = 0; // never expand

            // DONE — transparent + border only
            var doneGO = OvertoneUI.CreateUIObject("DoneButton", row.transform);
            var doneBdr = OvertoneUI.CreateUIObject("Border", doneGO.transform);
            OvertoneUI.StretchFill(doneBdr.GetComponent<RectTransform>());
            var dbImg = doneBdr.AddComponent<Image>();
            dbImg.sprite = OvertoneUI.SmoothRoundedRect;
            dbImg.type = Image.Type.Sliced;
            dbImg.color = OC.border;
            dbImg.raycastTarget = false;
            var doneImg = doneGO.AddComponent<Image>();
            doneImg.color = Color.clear;
            var doneBtn = doneGO.AddComponent<Button>();
            doneBtn.targetGraphic = dbImg;
            doneBtn.onClick.AddListener(OnDoneClicked);
            var doneLE = doneGO.AddComponent<LayoutElement>();
            doneLE.flexibleWidth = 1;
            doneLE.flexibleHeight = 0;
            var doneLbl = OvertoneUI.CreateUIObject("Label", doneGO.transform);
            OvertoneUI.StretchFill(doneLbl.GetComponent<RectTransform>());
            var doneTMP = doneLbl.AddComponent<TextMeshProUGUI>();
            doneTMP.text = "DONE";
            doneTMP.font = OvertoneUI.PressStart2P;
            doneTMP.fontSize = 9;
            doneTMP.color = OC.muted;
            doneTMP.characterSpacing = 1;
            doneTMP.alignment = TextAlignmentOptions.Center;
            doneTMP.enableWordWrapping = false;
            doneTMP.overflowMode = TextOverflowModes.Ellipsis;
            doneTMP.raycastTarget = false;

            // SHARE — primary cyan with scanlines (scored mode)
            var (shareGO2, shareTMP) = OvertoneUI.CreatePrimaryButton(row.transform, "SHARE", 34, "ShareButton");
            var shareLE = shareGO2.GetComponent<LayoutElement>();
            shareLE.flexibleWidth = 2;
            shareLE.flexibleHeight = 0;
            shareGO2.GetComponent<Button>().onClick.AddListener(OnShareClicked);
            shareBtn = shareGO2;

            // PLAY AGAIN — amber button (practice mode)
            var paGO = OvertoneUI.CreateUIObject("PlayAgainButton", row.transform);
            var paBg = paGO.AddComponent<Image>();
            paBg.sprite = OvertoneUI.SmoothRoundedRect;
            paBg.type = Image.Type.Sliced;
            paBg.color = OC.amber; // #F0B429
            playAgainBg = paBg;
            var paBtn = paGO.AddComponent<Button>();
            paBtn.targetGraphic = paBg;
            paBtn.onClick.AddListener(OnPlayAgainClicked);
            var paLE = paGO.AddComponent<LayoutElement>();
            paLE.flexibleWidth = 2;
            paLE.flexibleHeight = 0;
            // Scanlines
            var paScanGO = OvertoneUI.CreateUIObject("Scanlines", paGO.transform);
            OvertoneUI.StretchFill(paScanGO.GetComponent<RectTransform>());
            var paScanImg = paScanGO.AddComponent<Image>();
            paScanImg.sprite = OvertoneUI.GetScanlineSprite();
            paScanImg.type = Image.Type.Simple;
            paScanImg.color = new Color(0, 0, 0, 0.22f);
            paScanImg.raycastTarget = false;
            paGO.AddComponent<RectMask2D>();
            // Label
            var paLabelGO = OvertoneUI.CreateUIObject("Label", paGO.transform);
            OvertoneUI.StretchFill(paLabelGO.GetComponent<RectTransform>());
            var paLabelTMP = paLabelGO.AddComponent<TextMeshProUGUI>();
            paLabelTMP.text = "PLAY AGAIN";
            paLabelTMP.font = OvertoneUI.PressStart2P;
            paLabelTMP.fontSize = 10;
            paLabelTMP.color = OC.bg;
            paLabelTMP.characterSpacing = 2;
            paLabelTMP.alignment = TextAlignmentOptions.Center;
            paLabelTMP.enableWordWrapping = false;
            paLabelTMP.raycastTarget = false;
            playAgainBtn = paGO;
            playAgainBtn.SetActive(false); // hidden by default
        }

        private void OnDoneClicked()
        {
            if (ScreenManager.Instance != null)
                ScreenManager.Instance.NavigateTo(Screen.HomePlayed);
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

        private void AddSpacer(Transform parent, float height)
        {
            var s = OvertoneUI.CreateUIObject("Spacer", parent);
            var le = s.AddComponent<LayoutElement>();
            le.preferredHeight = height; le.minHeight = height;
        }

        private void AddFlex(Transform parent, float weight)
        {
            var f = OvertoneUI.CreateUIObject("Flex", parent);
            var le = f.AddComponent<LayoutElement>();
            le.flexibleHeight = weight; le.minHeight = 0;
        }
    }
}
