using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using MergeGame.Core;
using MergeGame.Data;

namespace MergeGame.UI
{
    /// <summary>
    /// Handles share flow: populates a share card, screenshots it with a dedicated camera,
    /// then invokes the native share sheet via NativeShare.
    /// </summary>
    public class ShareManager : MonoBehaviour
    {
        public static ShareManager Instance { get; private set; }

        private GameObject shareCardRoot;
        private Camera shareCamera;

        // Share card elements
        private TextMeshProUGUI dateLine;
        private TextMeshProUGUI scoreText;
        private Transform ballRow;
        private Transform shareBallRow2;
        private TextMeshProUGUI shareChainLabel;
        private TextMeshProUGUI footerText;

        private static readonly float[] BaseSizes = { 14, 18, 22, 26, 32, 38, 46, 54, 64, 76, 92 };

        private void Awake()
        {
            Instance = this;
            BuildShareCard();
        }

        public void ShareResult()
        {
            StartCoroutine(CaptureAndShare());
        }

        private IEnumerator CaptureAndShare()
        {
            // Populate with current data
            PopulateCard();

            // Show share card
            shareCardRoot.SetActive(true);

            // Wait for render
            yield return new WaitForEndOfFrame();

            // Capture — compact card format
            int w = 700, h = 500;
            RenderTexture rt = new RenderTexture(w, h, 24);
            shareCamera.targetTexture = rt;
            shareCamera.Render();

            Texture2D screenshot = new Texture2D(w, h, TextureFormat.RGB24, false);
            RenderTexture.active = rt;
            screenshot.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            screenshot.Apply();

            shareCamera.targetTexture = null;
            RenderTexture.active = null;
            Destroy(rt);

            // Hide share card
            shareCardRoot.SetActive(false);

            // Save to file
            string path = Application.persistentDataPath + "/murge_share.png";
            System.IO.File.WriteAllBytes(path, screenshot.EncodeToPNG());
            Destroy(screenshot);

            // Native share
#if UNITY_EDITOR
            Debug.Log($"[ShareManager] Would share: {path}");
#else
            try
            {
                new NativeShare()
                    .AddFile(path)
                    .SetText($"{GameSession.AppName} #{GameSession.TodayDayNumber} — {GameSession.TodayScore:N0} pts")
                    .Share();
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"NativeShare failed: {e.Message}");
            }
#endif
        }

        private void PopulateCard()
        {
            if (dateLine != null)
            {
                var now = System.DateTime.Now;
                dateLine.text = $"#{GameSession.TodayDayNumber}  ·  {now.ToString("MMM dd").ToUpper()}";
            }

            int score = GameSession.TodayScore;
            if (score <= 0 && ScoreManager.Instance != null)
                score = ScoreManager.Instance.HighScore;
            if (scoreText != null)
                scoreText.text = score.ToString("N0");

            // Populate balls
            if (ballRow != null)
            {
                foreach (Transform child in ballRow)
                    Destroy(child.gameObject);
                if (shareBallRow2 != null)
                    foreach (Transform child in shareBallRow2)
                        Destroy(child.gameObject);

                int[] counts = GameSession.MergeCounts ?? new int[11];

                for (int tier = 10; tier >= 0; tier--)
                {
                    Transform targetRow = tier >= 6 ? ballRow : (shareBallRow2 ?? ballRow);

                    int count = tier < counts.Length ? counts[tier] : 0;
                    Color ballColor = Visual.BallRenderer.GetBallColor(tier);
                    float baseSize = BaseSizes[tier];
                    float gridSize = Mathf.Max(26f, baseSize * 0.72f);
                    float shareSize = gridSize * 1.5f;

                    var cellGO = new GameObject($"Ball{tier}");
                    cellGO.layer = shareCardRoot.layer;
                    cellGO.transform.SetParent(targetRow, false);
                    var cellRT = cellGO.AddComponent<RectTransform>();
                    cellRT.sizeDelta = new Vector2(shareSize + 4, shareSize + 16);

                    // Ball
                    var ballGO = new GameObject("Ball");
                    ballGO.layer = shareCardRoot.layer;
                    ballGO.transform.SetParent(cellGO.transform, false);
                    var ballRT = ballGO.AddComponent<RectTransform>();
                    ballRT.anchorMin = new Vector2(0.5f, 1); ballRT.anchorMax = new Vector2(0.5f, 1);
                    ballRT.pivot = new Vector2(0.5f, 1);
                    ballRT.anchoredPosition = Vector2.zero;
                    ballRT.sizeDelta = new Vector2(shareSize, shareSize);

                    var ballImg = ballGO.AddComponent<Image>();
                    float uiRadius = shareSize / (2f * Visual.BallRenderer.PixelsPerUnit);
                    var pixels = Visual.BallRenderer.GenerateBallPixels(
                        tier, ballColor, uiRadius, 0f, out int texSize);
                    var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
                    tex.filterMode = FilterMode.Bilinear;
                    tex.SetPixels(pixels);
                    tex.Apply();
                    ballImg.sprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize),
                        new Vector2(0.5f, 0.5f), texSize);
                    ballImg.preserveAspect = true;
                    ballImg.color = count > 0 ? Color.white : new Color(1, 1, 1, 0.25f);

                    // Count label under ball (only if count > 0)
                    if (count > 0)
                    {
                        var countGO = new GameObject("Count");
                        countGO.layer = cellGO.layer;
                        countGO.transform.SetParent(cellGO.transform, false);
                        var countRT = countGO.AddComponent<RectTransform>();
                        countRT.anchorMin = new Vector2(0.5f, 0);
                        countRT.anchorMax = new Vector2(0.5f, 0);
                        countRT.pivot = new Vector2(0.5f, 0);
                        countRT.anchoredPosition = Vector2.zero;
                        countRT.sizeDelta = new Vector2(shareSize + 4, 14);
                        var countTMP = countGO.AddComponent<TextMeshProUGUI>();
                        countTMP.text = $"{count}";
                        countTMP.font = MurgeUI.PressStart2P;
                        countTMP.fontSize = 12;
                        countTMP.color = ballColor;
                        countTMP.alignment = TextAlignmentOptions.Center;
                        countTMP.raycastTarget = false;
                    }

                    cellGO.AddComponent<LayoutElement>();
                }
            }

            // Chain label
            if (shareChainLabel != null)
            {
                int chain = GameSession.LongestChain;
                shareChainLabel.text = chain >= 2 ? $"x{chain} CHAIN" : "";
            }
        }

        private void BuildShareCard()
        {
            // Create a dedicated layer for the share card
            int shareLayer = 31; // use layer 31 (same as ball preview camera)

            // Share camera (offscreen)
            var camObj = new GameObject("ShareCamera");
            camObj.transform.SetParent(transform);
            camObj.transform.position = new Vector3(200, 200, -10);
            shareCamera = camObj.AddComponent<Camera>();
            shareCamera.orthographic = true;
            shareCamera.orthographicSize = 2.5f; // 500 / 200
            shareCamera.aspect = 700f / 500f;
            shareCamera.clearFlags = CameraClearFlags.SolidColor;
            shareCamera.backgroundColor = new Color(0.071f, 0.078f, 0.110f); // #12141C
            shareCamera.cullingMask = 1 << shareLayer;
            shareCamera.depth = -100;
            shareCamera.enabled = false; // only render on demand

            // Share card panel — world space canvas on the share layer
            shareCardRoot = new GameObject("ShareCard");
            shareCardRoot.layer = shareLayer;
            shareCardRoot.transform.SetParent(transform);

            var canvas = shareCardRoot.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = shareCamera;
            canvas.sortingOrder = 100;

            var canvasRT = shareCardRoot.GetComponent<RectTransform>();
            canvasRT.position = new Vector3(200, 200, 0);
            canvasRT.sizeDelta = new Vector2(700, 500);
            canvasRT.localScale = Vector3.one * 0.01f; // scale down to fit camera

            var scaler = shareCardRoot.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;

            shareCardRoot.AddComponent<GraphicRaycaster>();

            // Background
            var bg = new GameObject("BG");
            bg.layer = shareLayer;
            bg.transform.SetParent(shareCardRoot.transform, false);
            var bgRT = bg.AddComponent<RectTransform>();
            bgRT.anchorMin = Vector2.zero; bgRT.anchorMax = Vector2.one;
            bgRT.offsetMin = Vector2.zero; bgRT.offsetMax = Vector2.zero;
            var bgImg = bg.AddComponent<Image>();
            bgImg.color = new Color(0.071f, 0.078f, 0.110f); // #12141C

            // Border
            var border = new GameObject("Border");
            border.layer = shareLayer;
            border.transform.SetParent(shareCardRoot.transform, false);
            var bRT = border.AddComponent<RectTransform>();
            bRT.anchorMin = Vector2.zero; bRT.anchorMax = Vector2.one;
            bRT.offsetMin = Vector2.zero; bRT.offsetMax = Vector2.zero;
            var bImg = border.AddComponent<Image>();
            bImg.color = Color.clear; // border via outline
            // Simple border — 2px lines on edges
            CreateBorderLine(shareCardRoot.transform, shareLayer, 0, 0, 1, 0, 2); // bottom
            CreateBorderLine(shareCardRoot.transform, shareLayer, 0, 1, 1, 1, 2); // top
            CreateBorderLine(shareCardRoot.transform, shareLayer, 0, 0, 0, 1, 2); // left
            CreateBorderLine(shareCardRoot.transform, shareLayer, 1, 0, 1, 1, 2); // right

            // Content VLG
            var content = new GameObject("Content");
            content.layer = shareLayer;
            content.transform.SetParent(shareCardRoot.transform, false);
            var cRT = content.AddComponent<RectTransform>();
            cRT.anchorMin = Vector2.zero; cRT.anchorMax = Vector2.one;
            cRT.offsetMin = new Vector2(40, 0); cRT.offsetMax = new Vector2(-40, 0);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 0;

            // Top spacer
            AddShareSpacer(content.transform, shareLayer, 12);

            // MURGE title
            var titleGO = CreateShareTMP(content.transform, shareLayer, "Title", 40);
            var titleTMP = titleGO.GetComponent<TextMeshProUGUI>();
            titleTMP.text = Core.GameSession.AppName;
            titleTMP.font = MurgeUI.PressStart2P;
            titleTMP.fontSize = 36;
            titleTMP.color = OC.cyan;
            titleTMP.characterSpacing = 3;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.richText = true;

            AddShareSpacer(content.transform, shareLayer, 4);

            // Date line
            var dateGO = CreateShareTMP(content.transform, shareLayer, "DateLine", 24);
            dateLine = dateGO.GetComponent<TextMeshProUGUI>();
            dateLine.text = "#1  ·  MAR 23";
            dateLine.font = MurgeUI.DMMono;
            dateLine.fontSize = 18;
            dateLine.color = OC.A(OC.white, 0.55f);
            dateLine.characterSpacing = 2;
            dateLine.alignment = TextAlignmentOptions.Center;

            AddShareSpacer(content.transform, shareLayer, 8);

            // Score
            var scoreGO = CreateShareTMP(content.transform, shareLayer, "Score", 60);
            scoreText = scoreGO.GetComponent<TextMeshProUGUI>();
            scoreText.text = "0";
            scoreText.font = MurgeUI.DMMono;
            scoreText.fontSize = 56;
            scoreText.color = OC.cyan;
            scoreText.alignment = TextAlignmentOptions.Center;

            // Ball rows container — two rows
            var ballRowsContainer = new GameObject("BallRows");
            ballRowsContainer.layer = shareLayer;
            ballRowsContainer.transform.SetParent(content.transform, false);
            ballRowsContainer.AddComponent<RectTransform>();
            var brcVLG = ballRowsContainer.AddComponent<VerticalLayoutGroup>();
            brcVLG.spacing = 0;
            brcVLG.childAlignment = TextAnchor.MiddleCenter;
            brcVLG.childControlWidth = true;
            brcVLG.childControlHeight = false;
            brcVLG.childForceExpandWidth = true;
            brcVLG.childForceExpandHeight = false;

            // Row 1: tiers 10-5
            var ballRow1GO = new GameObject("BallRow1");
            ballRow1GO.layer = shareLayer;
            ballRow1GO.transform.SetParent(ballRowsContainer.transform, false);
            ballRow1GO.AddComponent<RectTransform>();
            var br1HLG = ballRow1GO.AddComponent<HorizontalLayoutGroup>();
            br1HLG.childAlignment = TextAnchor.MiddleCenter;
            br1HLG.spacing = 8;
            br1HLG.childControlWidth = false;
            br1HLG.childControlHeight = false;
            br1HLG.childForceExpandWidth = false;
            ballRow1GO.AddComponent<LayoutElement>().preferredHeight = 95;
            ballRow = ballRow1GO.transform;

            // Row 2: tiers 5-0
            var ballRow2GO = new GameObject("BallRow2");
            ballRow2GO.layer = shareLayer;
            ballRow2GO.transform.SetParent(ballRowsContainer.transform, false);
            ballRow2GO.AddComponent<RectTransform>();
            var br2HLG = ballRow2GO.AddComponent<HorizontalLayoutGroup>();
            br2HLG.childAlignment = TextAnchor.MiddleCenter;
            br2HLG.spacing = 8;
            br2HLG.childControlWidth = false;
            br2HLG.childControlHeight = false;
            br2HLG.childForceExpandWidth = false;
            ballRow2GO.AddComponent<LayoutElement>().preferredHeight = 68;
            shareBallRow2 = ballRow2GO.transform;

            // Chain label
            var chainGO = CreateShareTMP(content.transform, shareLayer, "Chain", 18);
            shareChainLabel = chainGO.GetComponent<TextMeshProUGUI>();
            shareChainLabel.text = "";
            shareChainLabel.font = MurgeUI.PressStart2P;
            shareChainLabel.fontSize = 10;
            shareChainLabel.color = OC.amber;
            shareChainLabel.characterSpacing = 1;
            shareChainLabel.alignment = TextAlignmentOptions.Center;

            // Flex spacer — pushes footer to bottom
            var flexGO = new GameObject("Flex");
            flexGO.layer = shareLayer;
            flexGO.transform.SetParent(content.transform, false);
            flexGO.AddComponent<RectTransform>();
            flexGO.AddComponent<LayoutElement>().flexibleHeight = 1;

            // Footer — pinned to bottom
            var footerGO = CreateShareTMP(content.transform, shareLayer, "Footer", 24);
            footerText = footerGO.GetComponent<TextMeshProUGUI>();
            footerText.text = Core.GameSession.AppDomain;
            footerText.font = MurgeUI.DMMono;
            footerText.fontSize = 16;
            footerText.color = OC.A(OC.white, 0.40f);
            footerText.characterSpacing = 2;
            footerText.alignment = TextAlignmentOptions.Center;

            // Start hidden
            shareCardRoot.SetActive(false);
        }

        private GameObject CreateShareTMP(Transform parent, int layer, string name, float height)
        {
            var go = new GameObject(name);
            go.layer = layer;
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            go.AddComponent<TextMeshProUGUI>().raycastTarget = false;
            go.AddComponent<LayoutElement>().preferredHeight = height;
            return go;
        }

        private void AddShareSpacer(Transform parent, int layer, float height)
        {
            var go = new GameObject("Spacer");
            go.layer = layer;
            go.transform.SetParent(parent, false);
            go.AddComponent<RectTransform>();
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height; le.minHeight = height;
        }

        private void CreateBorderLine(Transform parent, int layer,
            float xMin, float yMin, float xMax, float yMax, float thickness)
        {
            var line = new GameObject("BorderLine");
            line.layer = layer;
            line.transform.SetParent(parent, false);
            var lRT = line.AddComponent<RectTransform>();
            lRT.anchorMin = new Vector2(xMin, yMin);
            lRT.anchorMax = new Vector2(xMax, yMax);

            if (xMin == xMax) // vertical
            {
                lRT.sizeDelta = new Vector2(thickness, 0);
                lRT.anchoredPosition = new Vector2(xMin > 0.5f ? -thickness / 2 : thickness / 2, 0);
            }
            else // horizontal
            {
                lRT.sizeDelta = new Vector2(0, thickness);
                lRT.anchoredPosition = new Vector2(0, yMin > 0.5f ? -thickness / 2 : thickness / 2);
            }

            var img = line.AddComponent<Image>();
            img.color = new Color(0.208f, 0.227f, 0.314f); // #353A50
            img.raycastTarget = false;
        }
    }
}
