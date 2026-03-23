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
            int w = 700, h = 380;
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
            string path = Application.persistentDataPath + "/overtone_share.png";
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
                    .SetText($"Overtone #{GameSession.TodayDayNumber} — {GameSession.TodayScore:N0} pts")
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

                int[] counts = GameSession.MergeCounts ?? new int[11];
                Debug.Log($"[ShareManager] MergeCounts: [{string.Join(",", counts)}]");

                for (int tier = 10; tier >= 0; tier--)
                {
                    int count = tier < counts.Length ? counts[tier] : 0;
                    Color ballColor = Visual.NeonBallRenderer.GetBallColor(tier);
                    float baseSize = BaseSizes[tier];
                    float gridSize = Mathf.Max(16f, baseSize * 0.38f);
                    // Scale for the compact share card
                    float shareSize = gridSize * 1.5f;

                    var ballGO = new GameObject($"Ball{tier}");
                    ballGO.layer = shareCardRoot.layer;
                    ballGO.transform.SetParent(ballRow, false);
                    var ballRT = ballGO.AddComponent<RectTransform>();
                    ballRT.sizeDelta = new Vector2(shareSize, shareSize);

                    var ballImg = ballGO.AddComponent<Image>();
                    float uiRadius = shareSize / (2f * 48f);
                    var pixels = Visual.NeonBallRenderer.GenerateBallPixels(
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
                        countGO.layer = ballGO.layer;
                        countGO.transform.SetParent(ballGO.transform, false);
                        var countRT = countGO.AddComponent<RectTransform>();
                        countRT.anchorMin = new Vector2(0.5f, 0);
                        countRT.anchorMax = new Vector2(0.5f, 0);
                        countRT.pivot = new Vector2(0.5f, 1);
                        countRT.anchoredPosition = new Vector2(0, -2);
                        countRT.sizeDelta = new Vector2(shareSize + 4, 14);
                        var countTMP = countGO.AddComponent<TextMeshProUGUI>();
                        countTMP.text = $"x{count}";
                        countTMP.font = OvertoneUI.PressStart2P;
                        countTMP.fontSize = 10;
                        countTMP.color = ballColor;
                        countTMP.alignment = TextAlignmentOptions.Center;
                        countTMP.raycastTarget = false;
                    }

                    ballGO.AddComponent<LayoutElement>();
                }
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
            shareCamera.orthographicSize = 1.9f; // 380 / 200
            shareCamera.aspect = 700f / 380f;
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
            canvasRT.sizeDelta = new Vector2(700, 380);
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

            // OVERTONE title
            var titleGO = CreateShareTMP(content.transform, shareLayer, "Title", 40);
            var titleTMP = titleGO.GetComponent<TextMeshProUGUI>();
            string cyanHex = ColorUtility.ToHtmlStringRGB(OC.cyan);
            titleTMP.text = $"OVER<color=#{cyanHex}>TONE</color>";
            titleTMP.font = OvertoneUI.PressStart2P;
            titleTMP.fontSize = 36;
            titleTMP.color = OC.white;
            titleTMP.characterSpacing = 3;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.richText = true;

            AddShareSpacer(content.transform, shareLayer, 4);

            // Date line
            var dateGO = CreateShareTMP(content.transform, shareLayer, "DateLine", 24);
            dateLine = dateGO.GetComponent<TextMeshProUGUI>();
            dateLine.text = "#1  ·  MAR 23";
            dateLine.font = OvertoneUI.DMMono;
            dateLine.fontSize = 18;
            dateLine.color = OC.A(OC.white, 0.55f);
            dateLine.characterSpacing = 2;
            dateLine.alignment = TextAlignmentOptions.Center;

            AddShareSpacer(content.transform, shareLayer, 8);

            // Score
            var scoreGO = CreateShareTMP(content.transform, shareLayer, "Score", 60);
            scoreText = scoreGO.GetComponent<TextMeshProUGUI>();
            scoreText.text = "0";
            scoreText.font = OvertoneUI.DMMono;
            scoreText.fontSize = 56;
            scoreText.color = OC.cyan;
            scoreText.alignment = TextAlignmentOptions.Center;

            // Ball row — directly after score, no spacer
            var ballRowGO = new GameObject("BallRow");
            ballRowGO.layer = shareLayer;
            ballRowGO.transform.SetParent(content.transform, false);
            ballRowGO.AddComponent<RectTransform>();
            var brHLG = ballRowGO.AddComponent<HorizontalLayoutGroup>();
            brHLG.childAlignment = TextAnchor.MiddleCenter;
            brHLG.spacing = 8;
            brHLG.childControlWidth = false;
            brHLG.childControlHeight = false;
            brHLG.childForceExpandWidth = false;
            ballRowGO.AddComponent<LayoutElement>().preferredHeight = 60;
            ballRow = ballRowGO.transform;

            // Footer — directly after balls
            var footerGO = CreateShareTMP(content.transform, shareLayer, "Footer", 24);
            footerText = footerGO.GetComponent<TextMeshProUGUI>();
            footerText.text = "overtone.app";
            footerText.font = OvertoneUI.DMMono;
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
