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
        private Transform mergeRow2;
        private GameObject chainBadge;
        private TextMeshProUGUI chainCountLabel;
        private Coroutine chainAnimCoroutine;
        private bool chainAnimPlayed;

        // Name prompt
        private GameObject namePromptCard;
        private TMP_InputField namePromptInput;
        private Button namePromptSaveBtn;
        private TextMeshProUGUI namePromptSaveLabel;
        private Image namePromptSaveBg;
        private TextMeshProUGUI namePromptCharCount;
        private bool namePromptSaving;
        private const string NamePromptShownKey = "name_prompt_shown";
        private CanvasGroup contentCG;
        private RectTransform contentPanel;
        private GameObject rankWrapper;
        private GameObject practiceInfoWrapper;
        private TextMeshProUGUI practiceLabel;
        private TextMeshProUGUI todayScoreLabel;
        private GameObject shareBtn;
        private GameObject doneScoredBtn;
        private GameObject donePracticeBtn;
        private TextMeshProUGUI quipLabel;
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
            chainAnimPlayed = false;
            if (quipLabel != null) quipLabel.text = "";
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

            // Quip — only generate once (first Populate call)
            if (quipLabel != null && string.IsNullOrEmpty(quipLabel.text))
                quipLabel.text = GameQuips.GetQuip();

            // Rank pill — only for scored games
            if (rankWrapper != null)
                rankWrapper.SetActive(!isPractice);
            if (!isPractice)
            {
                int rank = GameSession.ResultRank;
                int total = GameSession.ResultTotalPlayers;
                if (rankLabel != null)
                    rankLabel.text = rank > 0 ? $"#{rank}" : "";
                if (rankSubLabel != null)
                    rankSubLabel.text = rank > 0 && total > 0
                        ? $"of {total} players today"
                        : !Core.NetworkMonitor.IsOnline ? "offline — rank will update when connected" : "ranking...";
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
            // Scored mode: DONE (primary) + SHARE (ghost)
            // Practice mode: PLAY AGAIN (amber) + DONE (ghost)
            if (doneScoredBtn != null) doneScoredBtn.SetActive(!isPractice);
            if (shareBtn != null) shareBtn.SetActive(!isPractice);
            if (playAgainBtn != null) playAgainBtn.SetActive(isPractice);
            if (donePracticeBtn != null) donePracticeBtn.SetActive(isPractice);

            PopulateMergeRow();

            // Name prompt — show only on first scored game with auto-generated name
            if (namePromptCard != null)
                namePromptCard.SetActive(ShouldShowNamePrompt());
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
                    rankLabel.text = rank > 0 ? $"#{rank}" : "";
                if (rankSubLabel != null)
                    rankSubLabel.text = rank > 0 && totalPlayers > 0
                        ? $"of {totalPlayers} players today"
                        : !Core.NetworkMonitor.IsOnline ? "offline — rank will update when connected" : "ranking...";
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
            var panel = MurgeUI.CreateUIObject("ContentPanel", transform);
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
            finalScoreLabel = MurgeUI.CreateLabel(panel.transform, "FINAL SCORE",
                MurgeUI.PressStart2P, 8, OC.muted, "FinalScoreLabel");
            finalScoreLabel.characterSpacing = 3;
            finalScoreLabel.alignment = TextAlignmentOptions.Center;
            finalScoreLabel.textWrappingMode = TextWrappingModes.NoWrap;
            finalScoreLabel.overflowMode = TextOverflowModes.Ellipsis;
            var flLE = finalScoreLabel.gameObject.AddComponent<LayoutElement>();
            flLE.preferredHeight = 14; flLE.minHeight = 14;

            AddSpacer(panel.transform, 10);

            // Score value
            scoreValue = MurgeUI.CreateLabel(panel.transform, "0",
                MurgeUI.DMMono, 48, OC.cyan, "ScoreValue");
            scoreValue.alignment = TextAlignmentOptions.Center;
            scoreValue.textWrappingMode = TextWrappingModes.NoWrap;
            scoreValue.overflowMode = TextOverflowModes.Overflow;
            scoreValue.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 56);
            var svLE = scoreValue.gameObject.AddComponent<LayoutElement>();
            svLE.preferredHeight = 56; svLE.minHeight = 56;

            AddSpacer(panel.transform, 6);

            // Quip
            quipLabel = MurgeUI.CreateLabel(panel.transform, "",
                MurgeUI.DMMono, OFont.bodySm, OC.muted, "Quip");
            quipLabel.alignment = TextAlignmentOptions.Center;
            quipLabel.fontStyle = TMPro.FontStyles.Italic;
            quipLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 20;

            AddSpacer(panel.transform, 4);

            // Rank badge (scored mode)
            BuildRankBadge(panel.transform);

            // Practice info (practice mode)
            BuildPracticeInfo(panel.transform);

            AddSpacer(panel.transform, 8);

            // Name prompt (first scored game only)
            BuildNamePrompt(panel.transform);

            AddSpacer(panel.transform, 8);

            // Merges card (chain badge is inside)
            BuildMergeCard(panel.transform);

            AddSpacer(panel.transform, 12);

            // Button row
            BuildButtonRow(panel.transform);

            // Bottom flex
            AddFlex(panel.transform, 1f);
        }

        private void BuildRankBadge(Transform parent)
        {
            // Centering wrapper (scored mode only)
            var wrapper = MurgeUI.CreateUIObject("RankWrapper", parent);
            rankWrapper = wrapper;
            var wrapHLG = wrapper.AddComponent<HorizontalLayoutGroup>();
            wrapHLG.childAlignment = TextAnchor.MiddleCenter;
            wrapHLG.childControlWidth = false;
            wrapHLG.childControlHeight = false;
            wrapHLG.childForceExpandWidth = false;
            var wLE = wrapper.AddComponent<LayoutElement>();
            wLE.preferredHeight = 34; wLE.minHeight = 34;

            // Pill — content-width, centered by wrapper
            var pill = MurgeUI.CreateUIObject("RankPill", wrapper.transform);
            var pillRT = pill.GetComponent<RectTransform>();
            pillRT.sizeDelta = new Vector2(240, 34);

            // Border (cyan @ 28% — very subtle)
            var borderGO = MurgeUI.CreateUIObject("Border", pill.transform);
            MurgeUI.StretchFill(borderGO.GetComponent<RectTransform>());
            var bdrImg = borderGO.AddComponent<Image>();
            bdrImg.sprite = MurgeUI.SmoothRoundedRect;
            bdrImg.type = Image.Type.Sliced;
            bdrImg.color = new Color(0.302f, 0.851f, 0.753f, 0.28f); // #4DD9C0 @ 28%
            bdrImg.raycastTarget = false;

            // Fill inset (cyan @ 12% — barely visible)
            var fillGO = MurgeUI.CreateUIObject("Fill", pill.transform);
            var fRT = fillGO.GetComponent<RectTransform>();
            fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one;
            fRT.offsetMin = new Vector2(1, 1); fRT.offsetMax = new Vector2(-1, -1);
            var fImg = fillGO.AddComponent<Image>();
            fImg.sprite = MurgeUI.SmoothRoundedRect;
            fImg.type = Image.Type.Sliced;
            fImg.color = new Color32(18, 38, 34, 255); // #122622 — solid composited color
            fImg.raycastTarget = false;

            // Rank label (left)
            rankLabel = MurgeUI.CreateLabel(pill.transform, "",
                MurgeUI.PressStart2P, 8, OC.cyan, "RankLabel");
            rankLabel.characterSpacing = 1;
            rankLabel.textWrappingMode = TextWrappingModes.NoWrap;
            rankLabel.overflowMode = TextOverflowModes.Ellipsis;
            rankLabel.alignment = TextAlignmentOptions.Right;
            rankLabel.verticalAlignment = VerticalAlignmentOptions.Middle;
            var rlRT = rankLabel.GetComponent<RectTransform>();
            rlRT.anchorMin = new Vector2(0, 0); rlRT.anchorMax = new Vector2(0.3f, 1);
            rlRT.offsetMin = new Vector2(16, 0); rlRT.offsetMax = Vector2.zero;

            // Sub label (right)
            rankSubLabel = MurgeUI.CreateLabel(pill.transform, "ranking...",
                MurgeUI.DMMono, 12, OC.muted, "RankSub");
            rankSubLabel.textWrappingMode = TextWrappingModes.NoWrap;
            rankSubLabel.overflowMode = TextOverflowModes.Ellipsis;
            rankSubLabel.alignment = TextAlignmentOptions.Left;
            rankSubLabel.verticalAlignment = VerticalAlignmentOptions.Middle;
            var rsRT = rankSubLabel.GetComponent<RectTransform>();
            rsRT.anchorMin = new Vector2(0.3f, 0); rsRT.anchorMax = new Vector2(1, 1);
            rsRT.offsetMin = new Vector2(8, 0); rsRT.offsetMax = new Vector2(-16, 0);
        }

        private void BuildPracticeInfo(Transform parent)
        {
            practiceInfoWrapper = MurgeUI.CreateUIObject("PracticeInfo", parent);
            var vlg = practiceInfoWrapper.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 8;

            // "PRACTICE score not counted" pill
            var pillWrapper = MurgeUI.CreateUIObject("PracticePillWrap", practiceInfoWrapper.transform);
            var pwHLG = pillWrapper.AddComponent<HorizontalLayoutGroup>();
            pwHLG.childAlignment = TextAnchor.MiddleCenter;
            pwHLG.childControlWidth = false;
            pwHLG.childControlHeight = false;
            pwHLG.childForceExpandWidth = false;
            pillWrapper.AddComponent<LayoutElement>().preferredHeight = 34;

            var pill = MurgeUI.CreateUIObject("PracticePill", pillWrapper.transform);
            pill.GetComponent<RectTransform>().sizeDelta = new Vector2(230, 34);
            // Border (amber @ 28%)
            var pBorder = MurgeUI.CreateUIObject("Border", pill.transform);
            MurgeUI.StretchFill(pBorder.GetComponent<RectTransform>());
            var pbImg = pBorder.AddComponent<Image>();
            pbImg.sprite = MurgeUI.SmoothRoundedRect;
            pbImg.type = Image.Type.Sliced;
            pbImg.color = new Color(0.941f, 0.706f, 0.161f, 0.28f); // #F0B429 @ 28%
            pbImg.raycastTarget = false;
            // Fill (solid composited)
            var pFill = MurgeUI.CreateUIObject("Fill", pill.transform);
            var pfRT = pFill.GetComponent<RectTransform>();
            pfRT.anchorMin = Vector2.zero; pfRT.anchorMax = Vector2.one;
            pfRT.offsetMin = new Vector2(1, 1); pfRT.offsetMax = new Vector2(-1, -1);
            var pfImg = pFill.AddComponent<Image>();
            pfImg.sprite = MurgeUI.SmoothRoundedRect;
            pfImg.type = Image.Type.Sliced;
            pfImg.color = new Color32(22, 22, 16, 255); // composited amber @ 12% over dark bg
            pfImg.raycastTarget = false;
            // Text: "PRACTICE  score not counted"
            practiceLabel = MurgeUI.CreateLabel(pill.transform, "",
                MurgeUI.PressStart2P, 8, OC.muted, "PracticeText");
            string amberHex = ColorUtility.ToHtmlStringRGB(OC.amber);
            practiceLabel.text = $"<color=#{amberHex}>PRACTICE</color>  score not counted";
            practiceLabel.richText = true;
            practiceLabel.alignment = TextAlignmentOptions.Center;
            practiceLabel.verticalAlignment = VerticalAlignmentOptions.Middle;
            practiceLabel.textWrappingMode = TextWrappingModes.NoWrap;
            MurgeUI.StretchFill(practiceLabel.GetComponent<RectTransform>());

            // TODAY score box
            var todayBox = MurgeUI.CreateUIObject("TodayBox", practiceInfoWrapper.transform);
            var tbLE = todayBox.AddComponent<LayoutElement>();
            tbLE.preferredHeight = 38; tbLE.minHeight = 38;
            // Border
            var tbBorder = MurgeUI.CreateUIObject("Border", todayBox.transform);
            MurgeUI.StretchFill(tbBorder.GetComponent<RectTransform>());
            var tbbImg = tbBorder.AddComponent<Image>();
            tbbImg.sprite = MurgeUI.SmoothRoundedRect;
            tbbImg.type = Image.Type.Sliced;
            tbbImg.color = OC.border;
            tbbImg.raycastTarget = false;
            // Fill
            var tbFill = MurgeUI.CreateUIObject("Fill", todayBox.transform);
            var tbfRT = tbFill.GetComponent<RectTransform>();
            tbfRT.anchorMin = Vector2.zero; tbfRT.anchorMax = Vector2.one;
            tbfRT.offsetMin = new Vector2(1, 1); tbfRT.offsetMax = new Vector2(-1, -1);
            var tbfImg = tbFill.AddComponent<Image>();
            tbfImg.sprite = MurgeUI.SmoothRoundedRect;
            tbfImg.type = Image.Type.Sliced;
            tbfImg.color = OC.surface;
            tbfImg.raycastTarget = false;
            // Content: lock + TODAY + score
            var todayContent = MurgeUI.CreateUIObject("Content", todayBox.transform);
            MurgeUI.StretchFill(todayContent.GetComponent<RectTransform>());
            todayScoreLabel = todayContent.AddComponent<TextMeshProUGUI>();
            string cyanHex = ColorUtility.ToHtmlStringRGB(OC.cyan);
            todayScoreLabel.text = $"TODAY  <color=#{cyanHex}>0</color>";
            todayScoreLabel.font = MurgeUI.PressStart2P;
            todayScoreLabel.fontSize = 9;
            todayScoreLabel.color = OC.muted;
            todayScoreLabel.characterSpacing = 2;
            todayScoreLabel.alignment = TextAlignmentOptions.Center;
            todayScoreLabel.verticalAlignment = VerticalAlignmentOptions.Middle;
            todayScoreLabel.richText = true;
            todayScoreLabel.textWrappingMode = TextWrappingModes.NoWrap;
            todayScoreLabel.raycastTarget = false;

            practiceInfoWrapper.SetActive(false); // hidden by default
        }

        private void BuildMergeCard(Transform parent)
        {
            var card = MurgeUI.CreateUIObject("MergeCard", parent);
            var cardLE = card.AddComponent<LayoutElement>();
            cardLE.preferredHeight = 240; cardLE.minHeight = 220;

            // Border
            var borderGO = MurgeUI.CreateUIObject("Border", card.transform);
            MurgeUI.StretchFill(borderGO.GetComponent<RectTransform>());
            borderGO.AddComponent<Image>().color = OC.border;
            borderGO.GetComponent<Image>().sprite = MurgeUI.SmoothRoundedRect;
            borderGO.GetComponent<Image>().type = Image.Type.Sliced;
            borderGO.GetComponent<Image>().raycastTarget = false;

            // Fill
            var fillGO = MurgeUI.CreateUIObject("Fill", card.transform);
            var fRT = fillGO.GetComponent<RectTransform>();
            fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one;
            fRT.offsetMin = new Vector2(1, 1); fRT.offsetMax = new Vector2(-1, -1);
            fillGO.AddComponent<Image>().color = OC.surface;
            fillGO.GetComponent<Image>().sprite = MurgeUI.SmoothRoundedRect;
            fillGO.GetComponent<Image>().type = Image.Type.Sliced;
            fillGO.GetComponent<Image>().raycastTarget = false;

            // MERGES label
            var mergesLabel = MurgeUI.CreateLabel(card.transform, "MERGES",
                MurgeUI.PressStart2P, 7, OC.dim, "MergesLabel");
            mergesLabel.characterSpacing = 1;
            mergesLabel.textWrappingMode = TextWrappingModes.NoWrap;
            mergesLabel.overflowMode = TextOverflowModes.Ellipsis;
            var mlRT = mergesLabel.GetComponent<RectTransform>();
            mlRT.anchorMin = new Vector2(0, 1); mlRT.anchorMax = new Vector2(1, 1);
            mlRT.pivot = new Vector2(0, 1);
            mlRT.anchoredPosition = new Vector2(14, -10);
            mlRT.sizeDelta = new Vector2(-28, 12);

            // Two-row vertical container for balls
            var rowsContainer = MurgeUI.CreateUIObject("BallRows", card.transform);
            var rcRT = rowsContainer.GetComponent<RectTransform>();
            rcRT.anchorMin = new Vector2(0, 0); rcRT.anchorMax = new Vector2(1, 1);
            rcRT.offsetMin = new Vector2(8, 52); rcRT.offsetMax = new Vector2(-8, -26);
            var rcVLG = rowsContainer.AddComponent<VerticalLayoutGroup>();
            rcVLG.spacing = 0;
            rcVLG.childAlignment = TextAnchor.MiddleCenter;
            rcVLG.childControlWidth = true;
            rcVLG.childControlHeight = true;
            rcVLG.childForceExpandWidth = true;
            rcVLG.childForceExpandHeight = true;

            // Row 1: tiers 10-5 (6 largest)
            var row1GO = MurgeUI.CreateUIObject("BallRow1", rowsContainer.transform);
            var row1HLG = row1GO.AddComponent<HorizontalLayoutGroup>();
            row1HLG.spacing = 4;
            row1HLG.childAlignment = TextAnchor.MiddleCenter;
            row1HLG.childControlWidth = false;
            row1HLG.childControlHeight = false;
            row1HLG.childForceExpandWidth = false;
            mergeRow = row1GO.transform;

            // Row 2: tiers 4-0 (5 smallest)
            var row2GO = MurgeUI.CreateUIObject("BallRow2", rowsContainer.transform);
            var row2HLG = row2GO.AddComponent<HorizontalLayoutGroup>();
            row2HLG.spacing = 4;
            row2HLG.childAlignment = TextAnchor.MiddleCenter;
            row2HLG.childControlWidth = false;
            row2HLG.childControlHeight = false;
            row2HLG.childForceExpandWidth = false;
            mergeRow2 = row2GO.transform;

            // Chain badge (absolute positioned inside card, bottom center)
            BuildChainBadge(card.transform);
        }

        private void PopulateMergeRow()
        {
            if (mergeRow == null) return;

            foreach (Transform child in mergeRow)
                Destroy(child.gameObject);
            if (mergeRow2 != null)
                foreach (Transform child in mergeRow2)
                    Destroy(child.gameObject);

            int[] counts = GameSession.MergeCounts ?? new int[11];

            // Row 1: tiers 10-5 (6 largest), Row 2: tiers 4-0 (5 smallest)
            for (int tier = 10; tier >= 0; tier--)
            {
                Transform targetRow = tier >= 6 ? mergeRow : mergeRow2;
                if (targetRow == null) targetRow = mergeRow;

                int count = tier < counts.Length ? counts[tier] : 0;
                Color ballColor = Visual.BallRenderer.GetBallColor(tier);

                float baseSize = BaseSizes[tier];
                float gridSize = Mathf.Max(26f, baseSize * 0.72f);

                var cell = MurgeUI.CreateUIObject($"Ball{tier}", targetRow);
                var cellRT = cell.GetComponent<RectTransform>();
                cellRT.sizeDelta = new Vector2(gridSize + 4, gridSize + 14);

                if (count == 0)
                {
                    var cg = cell.AddComponent<CanvasGroup>();
                    cg.alpha = 0.25f;
                }

                // Ball
                var ballGO = MurgeUI.CreateUIObject("Ball", cell.transform);
                var ballRT = ballGO.GetComponent<RectTransform>();
                ballRT.anchorMin = new Vector2(0.5f, 1); ballRT.anchorMax = new Vector2(0.5f, 1);
                ballRT.pivot = new Vector2(0.5f, 1);
                ballRT.anchoredPosition = Vector2.zero;
                ballRT.sizeDelta = new Vector2(gridSize, gridSize);

                var ballImg = ballGO.AddComponent<Image>();
                float uiRadius = gridSize / (2f * Visual.BallRenderer.PixelsPerUnit);
                var pixels = Visual.BallRenderer.GenerateBallPixels(
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
                    var countGO = MurgeUI.CreateUIObject("Count", cell.transform);
                    var countRT = countGO.GetComponent<RectTransform>();
                    countRT.anchorMin = new Vector2(0.5f, 0);
                    countRT.anchorMax = new Vector2(0.5f, 0);
                    countRT.pivot = new Vector2(0.5f, 0);
                    countRT.anchoredPosition = Vector2.zero;
                    countRT.sizeDelta = new Vector2(gridSize + 4, 12);
                    var countTMP = countGO.AddComponent<TextMeshProUGUI>();
                    countTMP.text = $"{count}";
                    countTMP.font = MurgeUI.PressStart2P;
                    countTMP.fontSize = 9;
                    countTMP.color = ballColor;
                    countTMP.alignment = TextAlignmentOptions.Center;
                    countTMP.raycastTarget = false;
                }
            }

            // Chain badge — animate if chain >= 2
            if (chainBadge != null)
            {
                int chain = GameSession.LongestChain;
                if (chain >= 2)
                {
                    if (!chainAnimPlayed)
                    {
                        chainAnimPlayed = true;
                        if (chainAnimCoroutine != null) StopCoroutine(chainAnimCoroutine);
                        chainCountLabel.text = "CHAIN";
                        chainAnimCoroutine = StartCoroutine(AnimateChainBadge(chain));
                    }
                    // On subsequent Populate calls, just ensure final state is shown
                    else
                    {
                        chainCountLabel.text = $"x{chain} CHAIN";
                        chainBadge.GetComponent<CanvasGroup>().alpha = 1f;
                        chainBadge.transform.localScale = Vector3.one;
                    }
                }
                else
                {
                    chainBadge.GetComponent<CanvasGroup>().alpha = 0f;
                }
            }
        }

        private void BuildChainBadge(Transform parent)
        {
            // Absolute positioned inside card, bottom center
            chainBadge = MurgeUI.CreateUIObject("ChainBadge", parent);
            var badgeRT = chainBadge.GetComponent<RectTransform>();
            badgeRT.anchorMin = new Vector2(0.5f, 0);
            badgeRT.anchorMax = new Vector2(0.5f, 0);
            badgeRT.pivot = new Vector2(0.5f, 0);
            badgeRT.anchoredPosition = new Vector2(0, 18);
            badgeRT.sizeDelta = new Vector2(140, 28);

            // Pill background
            var bgImg = chainBadge.AddComponent<Image>();
            bgImg.sprite = MurgeUI.SmoothRoundedRect;
            bgImg.type = Image.Type.Sliced;
            bgImg.color = OC.A(OC.amber, 0.15f);
            bgImg.raycastTarget = false;

            // Chain text centered in pill
            var textGO = MurgeUI.CreateUIObject("ChainText", chainBadge.transform);
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero; textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero; textRT.offsetMax = Vector2.zero;
            chainCountLabel = textGO.AddComponent<TextMeshProUGUI>();
            chainCountLabel.text = "";
            chainCountLabel.font = MurgeUI.PressStart2P;
            chainCountLabel.fontSize = 8;
            chainCountLabel.color = OC.amber;
            chainCountLabel.characterSpacing = 1;
            chainCountLabel.alignment = TextAlignmentOptions.Center;
            chainCountLabel.raycastTarget = false;

            // Start hidden
            var cg = chainBadge.AddComponent<CanvasGroup>();
            cg.alpha = 0f;
        }

        private void BuildNamePrompt(Transform parent)
        {
            namePromptCard = MurgeUI.CreateUIObject("NamePrompt", parent);
            var cardLE = namePromptCard.AddComponent<LayoutElement>();
            cardLE.preferredHeight = 80;
            cardLE.minHeight = 80;

            // Border
            var border = MurgeUI.CreateUIObject("Border", namePromptCard.transform);
            MurgeUI.StretchFill(border.GetComponent<RectTransform>());
            var borderImg = border.AddComponent<Image>();
            borderImg.sprite = MurgeUI.SmoothRoundedRect;
            borderImg.type = Image.Type.Sliced;
            borderImg.color = OC.border;
            borderImg.raycastTarget = false;

            // Fill
            var fill = MurgeUI.CreateUIObject("Fill", namePromptCard.transform);
            var fillRT = fill.GetComponent<RectTransform>();
            fillRT.anchorMin = Vector2.zero; fillRT.anchorMax = Vector2.one;
            fillRT.offsetMin = new Vector2(1, 1); fillRT.offsetMax = new Vector2(-1, -1);
            var fillImg = fill.AddComponent<Image>();
            fillImg.sprite = MurgeUI.SmoothRoundedRect;
            fillImg.type = Image.Type.Sliced;
            fillImg.color = OC.surface;
            fillImg.raycastTarget = false;

            // "Claim your spot" label
            var label = MurgeUI.CreateLabel(namePromptCard.transform, "set your leaderboard name",
                MurgeUI.DMMono, OFont.bodySm, OC.muted, "PromptLabel");
            label.alignment = TextAlignmentOptions.Left;
            label.verticalAlignment = VerticalAlignmentOptions.Middle;
            var labelRT = label.GetComponent<RectTransform>();
            labelRT.anchorMin = new Vector2(0, 0.65f); labelRT.anchorMax = new Vector2(0.7f, 1);
            labelRT.offsetMin = new Vector2(14, 0); labelRT.offsetMax = new Vector2(0, -8);

            // Input row (input + save button)
            var inputRow = MurgeUI.CreateUIObject("InputRow", namePromptCard.transform);
            var rowRT = inputRow.GetComponent<RectTransform>();
            rowRT.anchorMin = new Vector2(0, 0); rowRT.anchorMax = new Vector2(1, 0.6f);
            rowRT.offsetMin = new Vector2(14, 8); rowRT.offsetMax = new Vector2(-14, 0);
            var rowHLG = inputRow.AddComponent<HorizontalLayoutGroup>();
            rowHLG.spacing = 8;
            rowHLG.childAlignment = TextAnchor.MiddleCenter;
            rowHLG.childControlWidth = true;
            rowHLG.childControlHeight = true;
            rowHLG.childForceExpandWidth = false;
            rowHLG.childForceExpandHeight = true;

            // Input field
            var inputGO = MurgeUI.CreateUIObject("NameInput", inputRow.transform);
            var inputBg = inputGO.AddComponent<Image>();
            inputBg.sprite = MurgeUI.SmoothRoundedRect;
            inputBg.type = Image.Type.Sliced;
            inputBg.color = OC.bg;
            inputGO.AddComponent<LayoutElement>().flexibleWidth = 1;

            var inputTextGO = MurgeUI.CreateUIObject("Text", inputGO.transform);
            MurgeUI.StretchFill(inputTextGO.GetComponent<RectTransform>());
            inputTextGO.GetComponent<RectTransform>().offsetMin = new Vector2(8, 0);
            inputTextGO.GetComponent<RectTransform>().offsetMax = new Vector2(-8, 0);
            var inputTMP = inputTextGO.AddComponent<TextMeshProUGUI>();
            inputTMP.font = MurgeUI.DMMono;
            inputTMP.fontSize = OFont.body;
            inputTMP.color = OC.white;
            inputTMP.alignment = TextAlignmentOptions.Left;
            inputTMP.verticalAlignment = VerticalAlignmentOptions.Middle;

            var placeholderGO = MurgeUI.CreateUIObject("Placeholder", inputGO.transform);
            MurgeUI.StretchFill(placeholderGO.GetComponent<RectTransform>());
            placeholderGO.GetComponent<RectTransform>().offsetMin = new Vector2(8, 0);
            placeholderGO.GetComponent<RectTransform>().offsetMax = new Vector2(-8, 0);
            var placeholderTMP = placeholderGO.AddComponent<TextMeshProUGUI>();
            placeholderTMP.text = "enter name";
            placeholderTMP.font = MurgeUI.DMMono;
            placeholderTMP.fontSize = OFont.body;
            placeholderTMP.color = OC.A(OC.white, 0.2f);
            placeholderTMP.alignment = TextAlignmentOptions.Left;
            placeholderTMP.verticalAlignment = VerticalAlignmentOptions.Middle;

            namePromptInput = inputGO.AddComponent<TMP_InputField>();
            namePromptInput.textComponent = inputTMP;
            namePromptInput.placeholder = placeholderTMP;
            namePromptInput.characterLimit = 16;
            namePromptInput.contentType = TMP_InputField.ContentType.Alphanumeric;
            namePromptInput.caretColor = OC.cyan;
            namePromptInput.selectionColor = OC.A(OC.cyan, 0.3f);

            // Save button
            var (saveGO, saveTMP) = MurgeUI.CreatePrimaryButton(inputRow.transform, "SAVE", 32, "SaveName");
            var saveLE = saveGO.GetComponent<LayoutElement>();
            saveLE.preferredWidth = 70;
            saveLE.flexibleWidth = 0;
            namePromptSaveBtn = saveGO.GetComponent<Button>();
            namePromptSaveLabel = saveTMP;
            namePromptSaveBg = saveGO.GetComponent<Image>();
            namePromptSaveBtn.onClick.AddListener(OnNamePromptSave);

            // Character count (same row as label, right-aligned)
            var charCountGO = MurgeUI.CreateUIObject("CharCount", namePromptCard.transform);
            var ccRT = charCountGO.GetComponent<RectTransform>();
            ccRT.anchorMin = new Vector2(0.7f, 0.65f); ccRT.anchorMax = new Vector2(1, 1);
            ccRT.offsetMin = new Vector2(0, 0); ccRT.offsetMax = new Vector2(-14, -8);
            namePromptCharCount = charCountGO.AddComponent<TextMeshProUGUI>();
            namePromptCharCount.text = "0/16";
            namePromptCharCount.font = MurgeUI.DMMono;
            namePromptCharCount.fontSize = OFont.bodyXs;
            namePromptCharCount.color = OC.dim;
            namePromptCharCount.alignment = TextAlignmentOptions.Right;
            namePromptCharCount.verticalAlignment = VerticalAlignmentOptions.Middle;
            namePromptCharCount.raycastTarget = false;

            // Update char count on input change
            namePromptInput.onValueChanged.AddListener((_) =>
            {
                int len = namePromptInput.text.Length;
                namePromptCharCount.text = $"{len}/16";
                namePromptCharCount.color = len > 0 ? OC.muted : OC.dim;
            });

            // Start hidden
            namePromptCard.SetActive(false);
        }

        private bool ShouldShowNamePrompt()
        {
            // Only show once, on first scored game, if name looks auto-generated
            if (PlayerPrefs.GetInt(NamePromptShownKey, 0) == 1) return false;
            if (GameSession.IsPractice) return false;
            if (PlayerIdentity.Instance == null) return false;

            string name = PlayerIdentity.Instance.DisplayName;
            if (string.IsNullOrEmpty(name)) return true;

            // Auto-generated names end with digits (e.g. "TweezyPossum42")
            return name.Length > 2 && char.IsDigit(name[name.Length - 1]);
        }

        private void OnNamePromptSave()
        {
            if (namePromptInput == null || PlayerIdentity.Instance == null) return;
            if (namePromptSaving) return;

            string newName = namePromptInput.text.Trim();
            if (newName.Length < 3)
            {
                Toast.Show("Name must be at least 3 characters");
                return;
            }

            if (!PlayerIdentity.Instance.TrySetDisplayName(newName)) return;

            // Loading state
            namePromptSaving = true;
            if (namePromptSaveLabel != null) namePromptSaveLabel.text = "...";
            if (namePromptSaveBtn != null) namePromptSaveBtn.interactable = false;
            if (namePromptSaveBg != null) namePromptSaveBg.color = OC.A(OC.cyan, 0.4f);

            if (Backend.LeaderboardService.Instance != null)
            {
                Backend.LeaderboardService.Instance.UpdateDisplayName(newName, (success, errorMsg) =>
                {
                    namePromptSaving = false;

                    if (!success)
                    {
                        // Revert
                        if (namePromptSaveLabel != null) namePromptSaveLabel.text = "SAVE";
                        if (namePromptSaveBtn != null) namePromptSaveBtn.interactable = true;
                        if (namePromptSaveBg != null) namePromptSaveBg.color = OC.cyan;

                        string msg = !string.IsNullOrEmpty(errorMsg) ? errorMsg : "Failed to update name";
                        Toast.Show(msg);
                    }
                    else
                    {
                        PlayerPrefs.SetInt(NamePromptShownKey, 1);
                        PlayerPrefs.Save();

                        // Refresh leaderboard cache so home screen shows the new name
                        if (Backend.LeaderboardService.Instance != null && DailySeedManager.Instance != null)
                            Backend.LeaderboardService.Instance.FetchLeaderboard(DailySeedManager.Instance.GameDate);

                        StartCoroutine(FadeOutNamePrompt());
                    }
                });
            }
            else
            {
                PlayerPrefs.SetInt(NamePromptShownKey, 1);
                PlayerPrefs.Save();
                StartCoroutine(FadeOutNamePrompt());
            }
        }

        private System.Collections.IEnumerator FadeOutNamePrompt()
        {
            if (namePromptCard == null) yield break;
            var cg = namePromptCard.GetComponent<CanvasGroup>();
            if (cg == null) cg = namePromptCard.AddComponent<CanvasGroup>();

            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                cg.alpha = 1f - Mathf.Clamp01(elapsed / 0.3f);
                yield return null;
            }
            namePromptCard.SetActive(false);
        }

        private void BuildButtonRow(Transform parent)
        {
            var row = MurgeUI.CreateUIObject("ButtonRow", parent);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            var rowLE = row.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 34; rowLE.minHeight = 34;
            rowLE.flexibleHeight = 0; // never expand

            // DONE — primary cyan (scored mode, left)
            var (doneGO2, doneTMP2) = MurgeUI.CreatePrimaryButton(row.transform, "DONE", 34, "DoneButtonScored");
            var doneLE2 = doneGO2.GetComponent<LayoutElement>();
            doneLE2.flexibleWidth = 2;
            doneLE2.flexibleHeight = 0;
            doneGO2.GetComponent<Button>().onClick.AddListener(OnDoneClicked);
            doneScoredBtn = doneGO2;

            // SHARE — ghost/outline (scored mode, right)
            var shareGO2 = MurgeUI.CreateUIObject("ShareButton", row.transform);
            var shBdr = MurgeUI.CreateUIObject("Border", shareGO2.transform);
            MurgeUI.StretchFill(shBdr.GetComponent<RectTransform>());
            var shBdrImg = shBdr.AddComponent<Image>();
            shBdrImg.sprite = MurgeUI.SmoothRoundedRect;
            shBdrImg.type = Image.Type.Sliced;
            shBdrImg.color = OC.border;
            shBdrImg.raycastTarget = false;
            var shareImg2 = shareGO2.AddComponent<Image>();
            shareImg2.color = Color.clear;
            var shareBtn2 = shareGO2.AddComponent<Button>();
            shareBtn2.targetGraphic = shBdrImg;
            shareBtn2.onClick.AddListener(OnShareClicked);
            var shareLE = shareGO2.AddComponent<LayoutElement>();
            shareLE.flexibleWidth = 1;
            shareLE.flexibleHeight = 0;
            var shareLblGO = MurgeUI.CreateUIObject("Label", shareGO2.transform);
            MurgeUI.StretchFill(shareLblGO.GetComponent<RectTransform>());
            var shareLblTMP = shareLblGO.AddComponent<TextMeshProUGUI>();
            shareLblTMP.text = "SHARE";
            shareLblTMP.font = MurgeUI.PressStart2P;
            shareLblTMP.fontSize = 9;
            shareLblTMP.color = OC.muted;
            shareLblTMP.characterSpacing = 1;
            shareLblTMP.alignment = TextAlignmentOptions.Center;
            shareLblTMP.textWrappingMode = TextWrappingModes.NoWrap;
            shareLblTMP.raycastTarget = false;
            shareBtn = shareGO2;

            // PLAY AGAIN — amber button (practice mode)
            var paGO = MurgeUI.CreateUIObject("PlayAgainButton", row.transform);
            var paBg = paGO.AddComponent<Image>();
            paBg.sprite = MurgeUI.SmoothRoundedRect;
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
            var paScanGO = MurgeUI.CreateUIObject("Scanlines", paGO.transform);
            MurgeUI.StretchFill(paScanGO.GetComponent<RectTransform>());
            var paScanImg = paScanGO.AddComponent<Image>();
            paScanImg.sprite = MurgeUI.GetScanlineSprite();
            paScanImg.type = Image.Type.Simple;
            paScanImg.color = new Color(0, 0, 0, 0.22f);
            paScanImg.raycastTarget = false;
            paGO.AddComponent<RectMask2D>();
            // Label
            var paLabelGO = MurgeUI.CreateUIObject("Label", paGO.transform);
            MurgeUI.StretchFill(paLabelGO.GetComponent<RectTransform>());
            var paLabelTMP = paLabelGO.AddComponent<TextMeshProUGUI>();
            paLabelTMP.text = "PLAY AGAIN";
            paLabelTMP.font = MurgeUI.PressStart2P;
            paLabelTMP.fontSize = 10;
            paLabelTMP.color = OC.bg;
            paLabelTMP.characterSpacing = 2;
            paLabelTMP.alignment = TextAlignmentOptions.Center;
            paLabelTMP.textWrappingMode = TextWrappingModes.NoWrap;
            paLabelTMP.raycastTarget = false;
            playAgainBtn = paGO;
            playAgainBtn.SetActive(false); // hidden by default

            // DONE — transparent + border only
            var doneGO = MurgeUI.CreateUIObject("DoneButton", row.transform);
            var doneBdr = MurgeUI.CreateUIObject("Border", doneGO.transform);
            MurgeUI.StretchFill(doneBdr.GetComponent<RectTransform>());
            var dbImg = doneBdr.AddComponent<Image>();
            dbImg.sprite = MurgeUI.SmoothRoundedRect;
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
            var doneLbl = MurgeUI.CreateUIObject("Label", doneGO.transform);
            MurgeUI.StretchFill(doneLbl.GetComponent<RectTransform>());
            var doneTMP = doneLbl.AddComponent<TextMeshProUGUI>();
            doneTMP.text = "DONE";
            doneTMP.font = MurgeUI.PressStart2P;
            doneTMP.fontSize = 9;
            doneTMP.color = OC.muted;
            doneTMP.characterSpacing = 1;
            doneTMP.alignment = TextAlignmentOptions.Center;
            doneTMP.textWrappingMode = TextWrappingModes.NoWrap;
            doneTMP.overflowMode = TextOverflowModes.Ellipsis;
            doneTMP.raycastTarget = false;
            donePracticeBtn = doneGO;
        }

        private void OnDoneClicked()
        {
            if (ScreenManager.Instance != null)
                ScreenManager.Instance.NavigateTo(Screen.HomePlayed);
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

        private System.Collections.IEnumerator AnimateChainBadge(int targetChain)
        {
            var cg = chainBadge.GetComponent<CanvasGroup>();
            cg.alpha = 0f;
            chainBadge.transform.localScale = Vector3.one * 0.5f;

            // Delay so it appears after merge card populates
            yield return new WaitForSeconds(0.4f);

            // Fade + scale in
            float elapsed = 0f;
            while (elapsed < 0.25f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 0.25f);
                float eased = 1f - (1f - t) * (1f - t);
                cg.alpha = eased;
                // Slight overshoot
                float scale = Mathf.LerpUnclamped(0.5f, 1f, eased + 0.1f * Mathf.Sin(t * Mathf.PI));
                chainBadge.transform.localScale = Vector3.one * scale;
                yield return null;
            }
            cg.alpha = 1f;
            chainBadge.transform.localScale = Vector3.one;

            // Tick up the counter from 0 to target
            for (int i = 1; i <= targetChain; i++)
            {
                chainCountLabel.text = $"x{i} CHAIN";

                // Quick scale pulse on each tick
                chainBadge.transform.localScale = Vector3.one * 1.08f;
                float pulseElapsed = 0f;
                while (pulseElapsed < 0.06f)
                {
                    pulseElapsed += Time.deltaTime;
                    float pt = Mathf.Clamp01(pulseElapsed / 0.06f);
                    chainBadge.transform.localScale = Vector3.Lerp(Vector3.one * 1.08f, Vector3.one, pt);
                    yield return null;
                }
                chainBadge.transform.localScale = Vector3.one;

                // Haptic tick
                if (HapticManager.Instance != null && i > 1)
                    HapticManager.Instance.PlayUITap();

                if (i < targetChain)
                    yield return new WaitForSeconds(0.08f);
            }
        }

        private void AddSpacer(Transform parent, float height)
        {
            var s = MurgeUI.CreateUIObject("Spacer", parent);
            var le = s.AddComponent<LayoutElement>();
            le.preferredHeight = height; le.minHeight = height;
        }

        private void AddFlex(Transform parent, float weight)
        {
            var f = MurgeUI.CreateUIObject("Flex", parent);
            var le = f.AddComponent<LayoutElement>();
            le.flexibleHeight = weight; le.minHeight = 0;
        }
    }
}
