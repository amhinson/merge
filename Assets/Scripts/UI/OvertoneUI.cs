using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MergeGame.Data;
using MergeGame.Visual;

namespace MergeGame.UI
{
    /// <summary>
    /// Factory methods for creating Overtone design-system UI elements at runtime.
    /// All methods return the created GameObject so callers can further customize.
    /// </summary>
    public static class OvertoneUI
    {
        // Font asset names — loaded from Resources on first use
        private const string PressStart2PAsset = "Fonts/PressStart2P SDF";
        private const string DMMonoAsset = "Fonts/DMMono-Medium SDF";

        private static TMP_FontAsset _pressStart2P;
        private static TMP_FontAsset _dmMono;

        public static TMP_FontAsset PressStart2P
        {
            get
            {
                if (_pressStart2P != null) return _pressStart2P;
                _pressStart2P = Resources.Load<TMP_FontAsset>(PressStart2PAsset);
                if (_pressStart2P == null)
                    Debug.LogWarning("OvertoneUI: PressStart2P SDF not found in Resources/Fonts/");
                return _pressStart2P;
            }
        }

        private static bool _dmMonoSearched;

        public static TMP_FontAsset DMMono
        {
            get
            {
                if (_dmMono != null && _dmMonoSearched) return _dmMono;
                _dmMonoSearched = true;
                _dmMono = Resources.Load<TMP_FontAsset>(DMMonoAsset);
                if (_dmMono == null)
                {
                    Debug.LogWarning("OvertoneUI: DMMono-Medium SDF not found, falling back to default font");
                    _dmMono = TMP_Settings.defaultFontAsset;
                }
                return _dmMono;
            }
        }

        /// <summary>Call to force re-search for fonts (e.g. after importing new font assets).</summary>
        public static void ResetFontCache()
        {
            _pressStart2P = null;
            _dmMono = null;
            _dmMonoSearched = false;
        }

        // ───── Card / Panel ─────

        /// <summary>
        /// Dark surface panel with optional border outline.
        /// </summary>
        public static GameObject CreateCard(Transform parent, string name = "Card")
        {
            var go = CreateUIObject(name, parent);
            var img = go.AddComponent<Image>();
            img.sprite = PixelUIGenerator.GetRoundedRect9Slice();
            img.type = Image.Type.Sliced;
            img.color = OC.surface;

            // Border outline child
            var outline = CreateUIObject("Outline", go.transform);
            var outlineImg = outline.AddComponent<Image>();
            outlineImg.sprite = PixelUIGenerator.GetRoundedRect9Slice();
            outlineImg.type = Image.Type.Sliced;
            outlineImg.color = OC.border;
            outlineImg.raycastTarget = false;
            var outlineRT = outline.GetComponent<RectTransform>();
            outlineRT.anchorMin = Vector2.zero;
            outlineRT.anchorMax = Vector2.one;
            outlineRT.offsetMin = Vector2.zero;
            outlineRT.offsetMax = Vector2.zero;

            return go;
        }

        // ───── Primary Button ─────

        /// <summary>
        /// Filled cyan button with dark text. Returns (buttonGO, label TMP).
        /// </summary>
        public static (GameObject go, TextMeshProUGUI label) CreatePrimaryButton(
            Transform parent, string text, float height = 52f, string name = "PrimaryButton")
        {
            var go = CreateUIObject(name, parent);
            var img = go.AddComponent<Image>();
            img.sprite = PixelUIGenerator.GetRoundedRect9Slice();
            img.type = Image.Type.Sliced;
            img.color = OC.cyan;

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleWidth = 1;

            // Scanline overlay (subtle CRT texture)
            var scanGO = CreateUIObject("Scanlines", go.transform);
            var scanImg = scanGO.AddComponent<Image>();
            scanImg.sprite = GetScanlineSprite();
            scanImg.type = Image.Type.Simple;
            scanImg.color = new Color(0, 0, 0, 0.25f);
            scanImg.raycastTarget = false;
            StretchFill(scanGO.GetComponent<RectTransform>());
            // Clip scanlines to button shape
            go.AddComponent<RectMask2D>();

            var labelGO = CreateUIObject("Label", go.transform);
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.font = PressStart2P;
            tmp.fontSize = OFont.heading;
            tmp.color = OC.bg;
            tmp.characterSpacing = 2;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            StretchFill(labelGO.GetComponent<RectTransform>());

            return (go, tmp);
        }

        /// <summary>
        /// Scanline texture — tall enough to cover any button, with 1px lines every 3px.
        /// Used as Image.Type.Simple stretched to fill.
        /// </summary>
        public static Sprite GetScanlineSprite()
        {
            if (cachedScanlineSprite != null) return cachedScanlineSprite;

            int w = 4;
            int h = 128; // tall enough for any button
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Repeat;

            for (int y = 0; y < h; y++)
            {
                bool isLine = (y % 4) < 2; // 2px line, 2px gap
                Color c = isLine ? Color.white : Color.clear;
                for (int x = 0; x < w; x++)
                    tex.SetPixel(x, y, c);
            }
            tex.Apply();

            // PPU matches the canvas reference height so scanlines are pixel-accurate
            cachedScanlineSprite = Sprite.Create(tex, new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f), 100f);
            cachedScanlineSprite.name = "ScanlineOverlay";
            return cachedScanlineSprite;
        }

        private static Sprite cachedScanlineSprite;

        // ───── Ghost Button ─────

        /// <summary>
        /// Outlined surface button with muted text.
        /// </summary>
        public static (GameObject go, TextMeshProUGUI label) CreateGhostButton(
            Transform parent, string text, float height = 52f, string name = "GhostButton")
        {
            var go = CreateCard(parent, name);

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = go.GetComponent<Image>();

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.flexibleWidth = 1;

            var labelGO = CreateUIObject("Label", go.transform);
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.font = PressStart2P;
            tmp.fontSize = OFont.label;
            tmp.color = OC.muted;
            tmp.characterSpacing = 1;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            StretchFill(labelGO.GetComponent<RectTransform>());

            return (go, tmp);
        }

        // ───── Back Button ─────

        public static (GameObject go, Button button) CreateBackButton(Transform parent, string name = "BackButton")
        {
            var go = CreateUIObject(name, parent);
            var img = go.AddComponent<Image>();
            img.sprite = PixelUIGenerator.GetRoundedRect9Slice();
            img.type = Image.Type.Sliced;
            img.color = Color.clear;

            // Outline
            var outline = CreateUIObject("Outline", go.transform);
            var outlineImg = outline.AddComponent<Image>();
            outlineImg.sprite = PixelUIGenerator.GetRoundedRect9Slice();
            outlineImg.type = Image.Type.Sliced;
            outlineImg.color = OC.border;
            outlineImg.raycastTarget = false;
            StretchFill(outline.GetComponent<RectTransform>());

            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;

            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = 52;
            le.preferredHeight = 36;

            var labelGO = CreateUIObject("Label", go.transform);
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.text = "\u2190"; // ←
            tmp.font = DMMono;
            tmp.fontSize = OFont.bodySm;
            tmp.color = OC.muted;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            StretchFill(labelGO.GetComponent<RectTransform>());

            return (go, btn);
        }

        // ───── Divider Line ─────

        public static GameObject CreateDivider(Transform parent, string name = "Divider")
        {
            var go = CreateUIObject(name, parent);
            var img = go.AddComponent<Image>();
            img.color = OC.border;
            img.raycastTarget = false;

            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 1;
            le.flexibleWidth = 1;

            return go;
        }

        // ───── Top Gradient ─────

        /// <summary>
        /// Subtle cyan gradient at top of screen. 200px tall, fades to transparent.
        /// Uses a vertex-color gradient via a custom UIGradient or a simple 2-pixel texture.
        /// </summary>
        public static GameObject CreateTopGradient(Transform parent, string name = "TopGradient")
        {
            var go = CreateUIObject(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 1);
            rt.anchorMax = new Vector2(1, 1);
            rt.pivot = new Vector2(0.5f, 1);
            rt.sizeDelta = new Vector2(0, 200);
            rt.anchoredPosition = Vector2.zero;

            var img = go.AddComponent<Image>();
            img.sprite = CreateGradientSprite();
            img.color = OC.A(OC.cyan, 0.07f);
            img.raycastTarget = false;

            return go;
        }

        // ───── Stat Cell ─────

        /// <summary>
        /// Value + label vertical cell for stats row.
        /// </summary>
        public static (GameObject go, TextMeshProUGUI valueLabel, TextMeshProUGUI keyLabel)
            CreateStatCell(Transform parent, string key, string value, Color valueColor, bool showDivider = true)
        {
            var go = CreateUIObject("StatCell_" + key, parent);
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.preferredHeight = 52;

            var vlg = go.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.MiddleCenter;
            vlg.spacing = 4;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;

            // Value
            var valGO = CreateUIObject("Value", go.transform);
            var valTMP = valGO.AddComponent<TextMeshProUGUI>();
            valTMP.text = value;
            valTMP.font = DMMono;
            valTMP.fontSize = 22;
            valTMP.fontStyle = FontStyles.Bold;
            valTMP.color = valueColor;
            valTMP.alignment = TextAlignmentOptions.Center;
            valTMP.raycastTarget = false;

            // Key
            var keyGO = CreateUIObject("Key", go.transform);
            var keyTMP = keyGO.AddComponent<TextMeshProUGUI>();
            keyTMP.text = key;
            keyTMP.font = PressStart2P;
            keyTMP.fontSize = OFont.labelSm;
            keyTMP.color = OC.muted;
            keyTMP.characterSpacing = 0.5f;
            keyTMP.alignment = TextAlignmentOptions.Center;
            keyTMP.raycastTarget = false;

            // Divider on right edge
            if (showDivider)
            {
                var divGO = CreateUIObject("RightDivider", go.transform);
                var divImg = divGO.AddComponent<Image>();
                divImg.color = OC.border;
                divImg.raycastTarget = false;
                var divRT = divGO.GetComponent<RectTransform>();
                divRT.anchorMin = new Vector2(1, 0.15f);
                divRT.anchorMax = new Vector2(1, 0.85f);
                divRT.pivot = new Vector2(1, 0.5f);
                divRT.sizeDelta = new Vector2(1, 0);
                divRT.anchoredPosition = Vector2.zero;
            }

            return (go, valTMP, keyTMP);
        }

        // ───── Leaderboard Row ─────

        public static (GameObject go, TextMeshProUGUI rankLabel, TextMeshProUGUI nameLabel, TextMeshProUGUI scoreLabel)
            CreateLeaderboardRow(Transform parent, string name = "LeaderboardRow")
        {
            var go = CreateUIObject(name, parent);
            var le = go.AddComponent<LayoutElement>();
            le.preferredHeight = 34;
            le.minHeight = 34;
            le.flexibleHeight = 0; // never expand beyond preferred
            le.flexibleWidth = 1;

            var bg = go.AddComponent<Image>();
            bg.color = Color.clear;

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 6;
            hlg.padding = new RectOffset(10, 10, 0, 0);
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;

            // Rank/Medal
            var rankGO = CreateUIObject("Rank", go.transform);
            var rankTMP = rankGO.AddComponent<TextMeshProUGUI>();
            rankTMP.font = DMMono;
            rankTMP.fontSize = OFont.body;
            rankTMP.color = OC.dim;
            rankTMP.alignment = TextAlignmentOptions.Left;
            rankTMP.raycastTarget = false;
            rankTMP.textWrappingMode = TextWrappingModes.NoWrap;
            var rankLE = rankGO.AddComponent<LayoutElement>();
            rankLE.preferredWidth = 24;

            // Name
            var nameGO = CreateUIObject("Name", go.transform);
            var nameTMP = nameGO.AddComponent<TextMeshProUGUI>();
            nameTMP.font = DMMono;
            nameTMP.fontSize = OFont.body;
            nameTMP.color = OC.muted;
            nameTMP.alignment = TextAlignmentOptions.Left;
            nameTMP.raycastTarget = false;
            var nameLE = nameGO.AddComponent<LayoutElement>();
            nameLE.flexibleWidth = 1;

            // Score
            var scoreGO = CreateUIObject("Score", go.transform);
            var scoreTMP = scoreGO.AddComponent<TextMeshProUGUI>();
            scoreTMP.font = DMMono;
            scoreTMP.fontSize = OFont.body;
            scoreTMP.color = OC.A(Color.white, 0.32f);
            scoreTMP.alignment = TextAlignmentOptions.Right;
            scoreTMP.raycastTarget = false;
            var scoreLE = scoreGO.AddComponent<LayoutElement>();
            scoreLE.preferredWidth = 80;

            return (go, rankTMP, nameTMP, scoreTMP);
        }

        /// <summary>
        /// Highlight a leaderboard row as the current player.
        /// </summary>
        public static void HighlightLeaderboardRow(GameObject row,
            TextMeshProUGUI rankLabel, TextMeshProUGUI nameLabel, TextMeshProUGUI scoreLabel)
        {
            var bg = row.GetComponent<Image>();
            if (bg != null) bg.color = OC.A(OC.cyan, 0.10f);
            rankLabel.color = OC.cyan;
            nameLabel.color = OC.cyan;
            scoreLabel.color = OC.cyan;
        }

        // ───── Toggle ─────

        public static (GameObject go, Toggle toggle) CreateToggle(Transform parent, bool initialValue = true, string name = "Toggle")
        {
            var go = CreateUIObject(name, parent);
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(48, 26);

            // Track
            var trackGO = CreateUIObject("Track", go.transform);
            var trackImg = trackGO.AddComponent<Image>();
            trackImg.sprite = PixelUIGenerator.GetRoundedRect9Slice();
            trackImg.type = Image.Type.Sliced;
            trackImg.color = initialValue ? OC.cyan : OC.border;
            StretchFill(trackGO.GetComponent<RectTransform>());

            // Thumb
            var thumbGO = CreateUIObject("Thumb", go.transform);
            var thumbImg = thumbGO.AddComponent<Image>();
            thumbImg.sprite = PixelUIGenerator.GetRoundedRect9Slice();
            thumbImg.type = Image.Type.Sliced;
            thumbImg.color = Color.white;
            var thumbRT = thumbGO.GetComponent<RectTransform>();
            thumbRT.sizeDelta = new Vector2(20, 20);
            thumbRT.anchorMin = new Vector2(0, 0.5f);
            thumbRT.anchorMax = new Vector2(0, 0.5f);
            thumbRT.pivot = new Vector2(0, 0.5f);
            thumbRT.anchoredPosition = new Vector2(initialValue ? 25 : 3, 0);

            // Unity Toggle component
            var toggle = go.AddComponent<Toggle>();
            toggle.isOn = initialValue;
            toggle.targetGraphic = trackImg;
            toggle.graphic = null; // we handle visuals manually

            // Toggle listener for visual update
            toggle.onValueChanged.AddListener((isOn) =>
            {
                trackImg.color = isOn ? OC.cyan : OC.border;
                thumbRT.anchoredPosition = new Vector2(isOn ? 25 : 3, 0);
            });

            return (go, toggle);
        }

        // ───── Text Helpers ─────

        public static TextMeshProUGUI CreateLabel(Transform parent, string text,
            TMP_FontAsset font, float fontSize, Color color, string name = "Label")
        {
            var go = CreateUIObject(name, parent);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }

        // ───── Utility ─────

        public static GameObject CreateUIObject(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go;
        }

        public static void StretchFill(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// A 1x2 texture used for vertical gradient (white top → transparent bottom).
        /// </summary>
        private static Sprite CreateGradientSprite()
        {
            if (cachedGradientSprite != null) return cachedGradientSprite;

            var tex = new Texture2D(1, 2, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixel(0, 1, Color.white);         // top
            tex.SetPixel(0, 0, Color.clear);          // bottom
            tex.Apply();

            cachedGradientSprite = Sprite.Create(tex, new Rect(0, 0, 1, 2), new Vector2(0.5f, 0.5f));
            cachedGradientSprite.name = "GradientTopDown";
            return cachedGradientSprite;
        }

        private static Sprite cachedGradientSprite;

        /// <summary>High-res anti-aliased rounded rect for smooth borders.</summary>
        private static Sprite _smoothRR;
        public static Sprite SmoothRoundedRect
        {
            get
            {
                if (_smoothRR != null) return _smoothRR;

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
                        float dx = Mathf.Max(0, Mathf.Abs(x - center + 0.5f) - (center - radius));
                        float dy = Mathf.Max(0, Mathf.Abs(y - center + 0.5f) - (center - radius));
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        if (dist <= radius)
                            pixels[y * size + x] = Color.white;
                        else if (dist <= radius + 1f)
                            pixels[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(radius + 1f - dist));
                    }
                }
                tex.SetPixels(pixels);
                tex.Apply();

                var border = new Vector4(radius + 1, radius + 1, radius + 1, radius + 1);
                _smoothRR = Sprite.Create(tex, new Rect(0, 0, size, size),
                    new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
                _smoothRR.name = "SmoothRoundedRect";
                return _smoothRR;
            }
        }
    }
}
