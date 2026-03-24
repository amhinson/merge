using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using MergeGame.Core;
using MergeGame.Data;

namespace MergeGame.UI
{
    /// <summary>
    /// 3-step onboarding tutorial: DROP → MERGE → SCORE.
    /// Shown on first launch only. Self-contained — builds its own UI hierarchy.
    /// </summary>
    public class OnboardingScreen : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BallTierConfig tierConfig;

        // Step state
        private int stepIndex;
        private const int StepCount = 3;

        // Root containers
        private GameObject[] stepGroups;
        private Image[] stepDots;
        private TextMeshProUGUI headingLabel;
        private TextMeshProUGUI subLabel;
        private GameObject nextButton;
        private GameObject skipButton;
        private GameObject startButton;

        // Demo arena objects
        private RectTransform demoArena;
        private GameObject dropperBall;
        private GameObject placedBall;
        private GameObject dropLine;
        private GameObject mergeRing;
        private GameObject ballA;
        private GameObject ballB;
        private GameObject mergedBall;
        private GameObject scorePop;
        private GameObject newDropper;

        // Step text
        private static readonly string[] Headings = { "DROP", "MERGE", "SCORE" };
        private static readonly string[] Subs = {
            "drop matching balls",
            "same size = they combine",
            "bigger merges = more points"
        };

        private Coroutine animCoroutine;

        // Spec ball sizes (from BALLS array in JSX)
        // tier 9 (index 8) = 22px, tier 8 (index 7) = 26px
        private const float DemoBallSizeSmall = 32f;  // 2nd smallest (tier 1, amber)
        private const float DemoBallSizeMedium = 46f; // 4th largest (tier 7, violet)

        private void OnEnable()
        {
            if (stepGroups == null)
                BuildUI();
            SetStep(0);
        }

        private void BuildUI()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();

            // Background
            var bg = gameObject.GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            bg.color = OC.bg;

            // No top gradient on onboarding screen

            // Main vertical layout — childControlHeight MUST be true for flexibleHeight to work
            var content = OvertoneUI.CreateUIObject("Content", transform);
            var contentRT = content.GetComponent<RectTransform>();
            OvertoneUI.StretchFill(contentRT);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(0, 0, 0, 0);
            vlg.spacing = 0;

            // Logo block (top, with padding)
            BuildLogoBlock(content.transform);

            // Flexible spacer — pushes arena toward vertical center
            var topFlex = OvertoneUI.CreateUIObject("TopFlex", content.transform);
            var topFlexLE = topFlex.AddComponent<LayoutElement>();
            topFlexLE.flexibleHeight = 1;
            topFlexLE.minHeight = 0;

            // Demo arena (centered, fixed aspect ratio)
            BuildDemoArena(content.transform);

            // Bottom flex — slightly less than top to bias arena upward
            var botFlex = OvertoneUI.CreateUIObject("BotFlex", content.transform);
            var botFlexLE = botFlex.AddComponent<LayoutElement>();
            botFlexLE.flexibleHeight = 0.6f;
            botFlexLE.minHeight = 0;

            // Step indicators + label — tightly grouped
            BuildStepIndicators(content.transform);
            AddSpacer(content.transform, 4);
            BuildStepLabel(content.transform);

            // Button row
            AddSpacer(content.transform, 8);
            BuildButtonRow(content.transform);

            // Bottom safe area padding
            var bottomPad = OvertoneUI.CreateUIObject("BottomPad", content.transform);
            var bottomPadLE = bottomPad.AddComponent<LayoutElement>();
            bottomPadLE.preferredHeight = 44 + OS.safeAreaBottom;
            bottomPadLE.minHeight = 44 + OS.safeAreaBottom;
        }

        private void BuildLogoBlock(Transform parent)
        {
            var block = OvertoneUI.CreateUIObject("LogoBlock", parent);
            var blockLE = block.AddComponent<LayoutElement>();
            blockLE.preferredHeight = 90 + OS.safeAreaTop;

            // Use a nested VLG for internal layout
            var inner = OvertoneUI.CreateUIObject("LogoInner", block.transform);
            var innerRT = inner.GetComponent<RectTransform>();
            // Position manually — anchor to top-center with padding
            innerRT.anchorMin = new Vector2(0, 0);
            innerRT.anchorMax = new Vector2(1, 1);
            innerRT.offsetMin = new Vector2(0, 0);
            innerRT.offsetMax = new Vector2(0, 0);
            var innerVLG = inner.AddComponent<VerticalLayoutGroup>();
            innerVLG.childAlignment = TextAnchor.MiddleCenter;
            innerVLG.spacing = 6;
            innerVLG.childControlWidth = false;
            innerVLG.childControlHeight = false;
            innerVLG.childForceExpandWidth = false;
            innerVLG.childForceExpandHeight = false;
            innerVLG.padding = new RectOffset(0, 0, (int)OS.safeAreaTop + 20, 0);

            // Single TMP with rich text for OVER + TONE — avoids layout gap entirely
            var titleGO = OvertoneUI.CreateUIObject("Title", inner.transform);
            var titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.sizeDelta = new Vector2(300, 30);
            var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            string cyanHex = ColorUtility.ToHtmlStringRGB(OC.cyan);
            titleTMP.text = $"OVER<color=#{cyanHex}>TONE</color>";
            titleTMP.font = OvertoneUI.PressStart2P;
            titleTMP.fontSize = 20;
            titleTMP.color = OC.white;
            titleTMP.characterSpacing = 3;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.textWrappingMode = TextWrappingModes.NoWrap;
            titleTMP.overflowMode = TextOverflowModes.Overflow;
            titleTMP.richText = true;
            titleTMP.raycastTarget = false;

            // Tagline
            var taglineGO = OvertoneUI.CreateUIObject("Tagline", inner.transform);
            var taglineRT = taglineGO.GetComponent<RectTransform>();
            taglineRT.sizeDelta = new Vector2(300, 16);
            var tagline = taglineGO.AddComponent<TextMeshProUGUI>();
            tagline.text = "A DAILY MERGE GAME";
            tagline.font = OvertoneUI.DMMono;
            tagline.fontSize = 10;
            tagline.color = OC.muted;
            tagline.characterSpacing = 5;
            tagline.alignment = TextAlignmentOptions.Center;
            tagline.textWrappingMode = TextWrappingModes.NoWrap;
            tagline.raycastTarget = false;
        }

        private void BuildDemoArena(Transform parent)
        {
            // Wrapper to center the arena without stretching to full width
            var wrapper = OvertoneUI.CreateUIObject("ArenaWrapper", parent);
            var wrapperHLG = wrapper.AddComponent<HorizontalLayoutGroup>();
            wrapperHLG.childAlignment = TextAnchor.MiddleCenter;
            wrapperHLG.childControlWidth = false;
            wrapperHLG.childControlHeight = false;
            wrapperHLG.childForceExpandWidth = false;
            var wrapperLE = wrapper.AddComponent<LayoutElement>();
            wrapperLE.preferredHeight = 300;
            wrapperLE.flexibleHeight = 0.5f; // can grow but not dominate

            var arenaGO = OvertoneUI.CreateUIObject("DemoArena", wrapper.transform);
            demoArena = arenaGO.GetComponent<RectTransform>();
            demoArena.sizeDelta = new Vector2(260, 300);
            var le = arenaGO.AddComponent<LayoutElement>();
            le.preferredWidth = 260;
            le.preferredHeight = 300;

            // Background
            var arenaBG = arenaGO.AddComponent<Image>();
            arenaBG.color = OC.surface;
            arenaBG.sprite = Visual.PixelUIGenerator.GetRoundedRect9Slice();
            arenaBG.type = Image.Type.Sliced;

            // Outline
            var outline = OvertoneUI.CreateUIObject("Outline", arenaGO.transform);
            var outlineImg = outline.AddComponent<Image>();
            outlineImg.sprite = Visual.PixelUIGenerator.GetRoundedRect9Slice();
            outlineImg.type = Image.Type.Sliced;
            outlineImg.color = OC.border;
            outlineImg.raycastTarget = false;
            OvertoneUI.StretchFill(outline.GetComponent<RectTransform>());

            // Grid overlay (subtle dot grid)
            BuildGridOverlay(arenaGO.transform);

            // Step groups — all anchored to fill the arena
            stepGroups = new GameObject[StepCount];

            // Step 0: DROP — dropper at top, placed ball at bottom
            stepGroups[0] = OvertoneUI.CreateUIObject("Step0Group", arenaGO.transform);
            OvertoneUI.StretchFill(stepGroups[0].GetComponent<RectTransform>());
            dropperBall = CreateDemoBall(stepGroups[0].transform, 1, new Vector2(0, 115), DemoBallSizeSmall);
            placedBall = CreateDemoBall(stepGroups[0].transform, 1, new Vector2(0, -110), DemoBallSizeSmall);
            dropLine = CreateDropLine(stepGroups[0].transform);

            // Step 1: MERGE — two balls close together
            stepGroups[1] = OvertoneUI.CreateUIObject("Step1Group", arenaGO.transform);
            OvertoneUI.StretchFill(stepGroups[1].GetComponent<RectTransform>());
            ballA = CreateDemoBall(stepGroups[1].transform, 1, new Vector2(0, -80), DemoBallSizeSmall);
            ballB = CreateDemoBall(stepGroups[1].transform, 1, new Vector2(0, -110), DemoBallSizeSmall);
            // mergeRing removed — merge effect uses particle burst + ball pop instead

            // Step 2: SCORE — merged result ball + score pop
            stepGroups[2] = OvertoneUI.CreateUIObject("Step2Group", arenaGO.transform);
            OvertoneUI.StretchFill(stepGroups[2].GetComponent<RectTransform>());
            // Both balls in step 2 are the same tier (the merge result)
            newDropper = CreateDemoBall(stepGroups[2].transform, 2, new Vector2(0, 115), DemoBallSizeMedium);
            mergedBall = CreateDemoBall(stepGroups[2].transform, 2, new Vector2(0, -95), DemoBallSizeMedium);
            scorePop = CreateScorePop(stepGroups[2].transform, new Vector2(0, -60));
        }

        private void BuildGridOverlay(Transform parent)
        {
            var gridGO = OvertoneUI.CreateUIObject("GridOverlay", parent);
            OvertoneUI.StretchFill(gridGO.GetComponent<RectTransform>());
            gridGO.AddComponent<RectMask2D>(); // clip lines to arena bounds

            // Grid uses fractional anchors so lines scale with arena size
            // ~6 columns, ~7 rows based on 28px cell at 260x300
            int gridCols = 6;
            int gridRows = 7;

            // Use a lighter color than OC.border since it's on the dark OC.surface background
            Color lineColor = new Color(1f, 1f, 1f, 0.06f);

            // Vertical lines (evenly spaced using anchors)
            for (int i = 1; i < gridCols; i++)
            {
                float frac = (float)i / gridCols;
                var line = OvertoneUI.CreateUIObject($"VLine{i}", gridGO.transform);
                var lineRT = line.GetComponent<RectTransform>();
                lineRT.anchorMin = new Vector2(frac, 0);
                lineRT.anchorMax = new Vector2(frac, 1);
                lineRT.pivot = new Vector2(0.5f, 0.5f);
                lineRT.anchoredPosition = Vector2.zero;
                lineRT.sizeDelta = new Vector2(1, 0);
                var img = line.AddComponent<Image>();
                img.color = lineColor;
                img.raycastTarget = false;
            }

            // Horizontal lines
            for (int i = 1; i < gridRows; i++)
            {
                float frac = (float)i / gridRows;
                var line = OvertoneUI.CreateUIObject($"HLine{i}", gridGO.transform);
                var lineRT = line.GetComponent<RectTransform>();
                lineRT.anchorMin = new Vector2(0, frac);
                lineRT.anchorMax = new Vector2(1, frac);
                lineRT.pivot = new Vector2(0.5f, 0.5f);
                lineRT.anchoredPosition = Vector2.zero;
                lineRT.sizeDelta = new Vector2(0, 1);
                var img = line.AddComponent<Image>();
                img.color = lineColor;
                img.raycastTarget = false;
            }
        }

        private void BuildStepIndicators(Transform parent)
        {
            var row = OvertoneUI.CreateUIObject("StepIndicators", parent);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 8;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            var rowLE = row.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 14;
            rowLE.minHeight = 14;

            // Create a pill-shaped sprite for the dots (fully rounded)
            var pillSprite = CreatePillSprite();

            stepDots = new Image[StepCount];
            for (int i = 0; i < StepCount; i++)
            {
                var dot = OvertoneUI.CreateUIObject($"Dot{i}", row.transform);
                var dotImg = dot.AddComponent<Image>();
                dotImg.sprite = OvertoneUI.SmoothRoundedRect;
                dotImg.type = Image.Type.Sliced;
                var dotRT = dot.GetComponent<RectTransform>();
                // Inactive: small circle 8x8
                dotRT.sizeDelta = new Vector2(8, 8);
                dot.AddComponent<LayoutElement>();
                stepDots[i] = dotImg;
            }
        }

        private static Sprite cachedPillSprite;
        private static Sprite CreatePillSprite()
        {
            if (cachedPillSprite != null) return cachedPillSprite;

            // 64x64 white circle for smooth rendering at UI sizes
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

            var border = new Vector4(radius, radius, radius, radius);
            cachedPillSprite = Sprite.Create(
                tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), 100f, 0,
                SpriteMeshType.FullRect, border
            );
            cachedPillSprite.name = "PillSprite";
            return cachedPillSprite;
        }

        private void BuildStepLabel(Transform parent)
        {
            var block = OvertoneUI.CreateUIObject("StepLabel", parent);
            var vlgBlock = block.AddComponent<VerticalLayoutGroup>();
            vlgBlock.childAlignment = TextAnchor.MiddleCenter;
            vlgBlock.spacing = 2;
            vlgBlock.childControlWidth = true;
            vlgBlock.childControlHeight = false;
            vlgBlock.childForceExpandWidth = true;
            vlgBlock.padding = new RectOffset(24, 24, 0, 0);
            var stepLE = block.AddComponent<LayoutElement>();
            stepLE.preferredHeight = 42;
            stepLE.minHeight = 42;

            headingLabel = OvertoneUI.CreateLabel(block.transform, "DROP",
                OvertoneUI.PressStart2P, 12, OC.white, "Heading");
            headingLabel.characterSpacing = 2;
            headingLabel.alignment = TextAlignmentOptions.Center;
            headingLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 18;

            subLabel = OvertoneUI.CreateLabel(block.transform, "drop matching balls",
                OvertoneUI.DMMono, 12, OC.muted, "Sub");
            subLabel.alignment = TextAlignmentOptions.Center;
            subLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 18;
        }

        private void BuildButtonRow(Transform parent)
        {
            var row = OvertoneUI.CreateUIObject("ButtonRow", parent);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.padding = new RectOffset(24, 24, 0, 0);
            hlg.childControlWidth = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
            var btnRowLE = row.AddComponent<LayoutElement>();
            btnRowLE.preferredHeight = 44;
            btnRowLE.minHeight = 44;
            btnRowLE.flexibleHeight = 0;

            // Next button
            var (nextGO, nextLabelTMP) = OvertoneUI.CreatePrimaryButton(row.transform, "NEXT  \u2192", 44, "NextButton");
            nextGO.GetComponent<LayoutElement>().flexibleWidth = 3;
            nextGO.GetComponent<Button>().onClick.AddListener(OnNextClicked);
            nextButton = nextGO;

            // Skip button — smooth border, transparent inside
            var skipGO = OvertoneUI.CreateUIObject("SkipButton", row.transform);
            var skipLE = skipGO.AddComponent<LayoutElement>();
            skipLE.preferredHeight = 44;
            skipLE.flexibleWidth = 1;
            // Border (behind)
            var skipBorderGO = OvertoneUI.CreateUIObject("Border", skipGO.transform);
            OvertoneUI.StretchFill(skipBorderGO.GetComponent<RectTransform>());
            var skipBdrImg = skipBorderGO.AddComponent<Image>();
            skipBdrImg.sprite = OvertoneUI.SmoothRoundedRect;
            skipBdrImg.type = Image.Type.Sliced;
            skipBdrImg.color = OC.border;
            skipBdrImg.raycastTarget = false;
            // Fill (inset, matches bg)
            var skipFillGO = OvertoneUI.CreateUIObject("Fill", skipGO.transform);
            var sfRT = skipFillGO.GetComponent<RectTransform>();
            sfRT.anchorMin = Vector2.zero; sfRT.anchorMax = Vector2.one;
            sfRT.offsetMin = new Vector2(1, 1); sfRT.offsetMax = new Vector2(-1, -1);
            var sfImg = skipFillGO.AddComponent<Image>();
            sfImg.sprite = OvertoneUI.SmoothRoundedRect;
            sfImg.type = Image.Type.Sliced;
            sfImg.color = OC.bg;
            sfImg.raycastTarget = false;
            // Hit area
            var skipImg = skipGO.AddComponent<Image>();
            skipImg.color = Color.clear;
            var skipBtn = skipGO.AddComponent<Button>();
            skipBtn.targetGraphic = skipBdrImg;
            // Label — same color as border
            var skipLabelGO = OvertoneUI.CreateUIObject("Label", skipGO.transform);
            var skipLabelTMP = skipLabelGO.AddComponent<TextMeshProUGUI>();
            skipLabelTMP.text = "SKIP";
            skipLabelTMP.font = OvertoneUI.PressStart2P;
            skipLabelTMP.fontSize = OFont.label;
            skipLabelTMP.color = OC.muted;
            skipLabelTMP.characterSpacing = 1;
            skipLabelTMP.alignment = TextAlignmentOptions.Center;
            skipLabelTMP.raycastTarget = false;
            OvertoneUI.StretchFill(skipLabelGO.GetComponent<RectTransform>());
            skipBtn.onClick.AddListener(OnSkipClicked);
            skipButton = skipGO;

            // Start button (hidden initially, full width)
            var (startGO, startLabelTMP) = OvertoneUI.CreatePrimaryButton(row.transform, "LET'S PLAY", 28, "StartButton");
            startGO.GetComponent<LayoutElement>().flexibleWidth = 1;
            startGO.GetComponent<Button>().onClick.AddListener(OnStartClicked);
            startButton = startGO;
            startButton.SetActive(false);
        }

        // ───── Step management ─────

        private void SetStep(int index)
        {
            stepIndex = Mathf.Clamp(index, 0, StepCount - 1);

            for (int i = 0; i < StepCount; i++)
            {
                if (stepGroups[i] != null)
                    stepGroups[i].SetActive(i == stepIndex);
            }

            // Reset visibility of objects within each step
            if (ballA != null) { ballA.SetActive(true); ballA.GetComponent<RectTransform>().localScale = Vector3.one; }
            if (ballB != null) { ballB.SetActive(true); ballB.GetComponent<RectTransform>().localScale = Vector3.one; }

            // Ensure merged ball is in step 2's group (it may have been reparented to step 1 during merge anim)
            if (mergedBall != null && stepGroups[2] != null)
            {
                mergedBall.transform.SetParent(stepGroups[2].transform, false);
                mergedBall.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, -95);
                mergedBall.GetComponent<RectTransform>().localScale = Vector3.one;
            }

            // Indicators — active: wide cyan pill, inactive: small dark circle
            for (int i = 0; i < stepDots.Length; i++)
            {
                bool active = i == stepIndex;
                stepDots[i].color = active ? OC.cyan : new Color(0.22f, 0.24f, 0.30f); // darker than OC.border
                stepDots[i].rectTransform.sizeDelta = active ? new Vector2(18, 8) : new Vector2(8, 8);
            }

            // Labels
            if (headingLabel != null) headingLabel.text = Headings[stepIndex];
            if (subLabel != null) subLabel.text = Subs[stepIndex];

            // Buttons
            bool isLast = stepIndex == StepCount - 1;
            if (nextButton != null) nextButton.SetActive(!isLast);
            if (skipButton != null) skipButton.SetActive(!isLast);
            if (startButton != null) startButton.SetActive(isLast);

            // Start step animation
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(AnimateStep(stepIndex));
        }

        // ───── Animations ─────

        private IEnumerator AnimateStep(int step)
        {
            switch (step)
            {
                case 0: yield return AnimateStep0(); break;
                case 1: yield return AnimateStep1(); break;
                case 2: yield return AnimateStep2(); break;
            }
        }

        private IEnumerator AnimateStep0()
        {
            var dropperRT = dropperBall.GetComponent<RectTransform>();
            float baseY = 115f;
            while (stepIndex == 0)
            {
                float t = Mathf.PingPong(Time.time * 0.7f, 1f);
                float eased = Mathf.Sin(t * Mathf.PI);
                dropperRT.anchoredPosition = new Vector2(0, baseY + eased * 6f);
                yield return null;
            }
        }

        private IEnumerator AnimateStep1()
        {
            // Both balls shrink into the merge point immediately
            float shrinkDuration = 0.15f;
            float shrinkElapsed = 0f;
            var rtA = ballA != null ? ballA.GetComponent<RectTransform>() : null;
            var rtB = ballB != null ? ballB.GetComponent<RectTransform>() : null;

            while (shrinkElapsed < shrinkDuration)
            {
                shrinkElapsed += Time.deltaTime;
                float t = Mathf.Clamp01(shrinkElapsed / shrinkDuration);
                float scale = 1f - t * t; // ease in
                if (rtA != null) rtA.localScale = Vector3.one * scale;
                if (rtB != null) rtB.localScale = Vector3.one * scale;
                yield return null;
            }

            if (ballA != null) ballA.SetActive(false);
            if (ballB != null) ballB.SetActive(false);

            // Particles burst from merge point
            SpawnMergeParticles(stepGroups[1].transform, new Vector2(0, -95f));

            yield return new WaitForSeconds(0.1f);

            // Merged ball pops in with a bounce
            if (mergedBall != null)
            {
                mergedBall.transform.SetParent(stepGroups[1].transform, false);
                var mbRT = mergedBall.GetComponent<RectTransform>();
                mbRT.anchoredPosition = new Vector2(0, -95);
                mergedBall.SetActive(true);
                mbRT.localScale = Vector3.zero;
                yield return ScaleObject(mergedBall, Vector3.zero, Vector3.one * 1.15f, 0.18f);
                yield return ScaleObject(mergedBall, Vector3.one * 1.15f, Vector3.one, 0.08f);
            }

            // Wait for user to press NEXT — don't auto-advance
        }

        private IEnumerator AnimateStep2()
        {
            if (mergedBall != null)
            {
                var rt = mergedBall.GetComponent<RectTransform>();
                rt.localScale = Vector3.zero;
                yield return ScaleObject(mergedBall, Vector3.zero, Vector3.one * 1.15f, 0.2f);
                yield return ScaleObject(mergedBall, Vector3.one * 1.15f, Vector3.one, 0.1f);
            }

            if (scorePop != null)
            {
                var popRT = scorePop.GetComponent<RectTransform>();
                var popCG = scorePop.GetComponent<CanvasGroup>();
                if (popCG == null) popCG = scorePop.AddComponent<CanvasGroup>();

                scorePop.SetActive(true);
                popCG.alpha = 0f;
                float baseY = popRT.anchoredPosition.y;

                float elapsed = 0f;
                while (elapsed < 0.3f)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / 0.3f);
                    popCG.alpha = t;
                    popRT.anchoredPosition = new Vector2(0, baseY + t * 16f);
                    yield return null;
                }

                yield return new WaitForSeconds(0.5f);

                elapsed = 0f;
                while (elapsed < 0.3f)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / 0.3f);
                    popCG.alpha = 1f - t;
                    yield return null;
                }
                scorePop.SetActive(false);
                popRT.anchoredPosition = new Vector2(0, baseY);
            }

            // Dropper bobbing — loops until user presses LET'S PLAY
            if (newDropper != null)
            {
                var dropperRT = newDropper.GetComponent<RectTransform>();
                float baseY = 115f;

                while (stepIndex == 2)
                {
                    float t = Mathf.PingPong(Time.time * 0.7f, 1f);
                    float eased = Mathf.Sin(t * Mathf.PI);
                    dropperRT.anchoredPosition = new Vector2(0, baseY + eased * 6f);
                    yield return null;
                }
            }
        }

        private void SpawnMergeParticles(Transform parent, Vector2 center)
        {
            for (int i = 0; i < 6; i++)
            {
                var particle = OvertoneUI.CreateUIObject($"Particle{i}", parent);
                var prt = particle.GetComponent<RectTransform>();
                prt.anchorMin = new Vector2(0.5f, 0.5f);
                prt.anchorMax = new Vector2(0.5f, 0.5f);
                prt.pivot = new Vector2(0.5f, 0.5f);
                prt.anchoredPosition = center;
                prt.sizeDelta = new Vector2(4, 4);

                var img = particle.AddComponent<Image>();
                img.color = OC.cyan;
                img.raycastTarget = false;

                var cg = particle.AddComponent<CanvasGroup>();
                StartCoroutine(AnimateParticle(prt, cg, i));
            }
        }

        private IEnumerator AnimateParticle(RectTransform prt, CanvasGroup cg, int index)
        {
            float angle = index * (360f / 6f) * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            Vector2 startPos = prt.anchoredPosition;
            float speed = 60f;
            float duration = 0.25f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                prt.anchoredPosition = startPos + dir * speed * t;
                cg.alpha = 1f - t;
                yield return null;
            }

            Destroy(prt.gameObject);
        }

        private IEnumerator ScaleObject(GameObject obj, Vector3 from, Vector3 to, float duration)
        {
            if (obj == null) yield break;
            var rt = obj.GetComponent<RectTransform>();
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - (1f - t) * (1f - t);
                rt.localScale = Vector3.Lerp(from, to, eased);
                yield return null;
            }
            rt.localScale = to;
        }

        // ───── Button handlers ─────

        private void OnNextClicked()
        {
            if (stepIndex == 0)
            {
                if (animCoroutine != null) StopCoroutine(animCoroutine);
                animCoroutine = StartCoroutine(DropTransitionToStep1());
            }
            else if (stepIndex < StepCount - 1)
            {
                SetStep(stepIndex + 1);
            }
        }

        private IEnumerator DropTransitionToStep1()
        {
            if (dropperBall != null)
            {
                var dropperRT = dropperBall.GetComponent<RectTransform>();
                float startY = dropperRT.anchoredPosition.y;
                // Land just above the placed ball (not overlapping)
                float targetY = -75f;
                float duration = 0.5f;
                float elapsed = 0f;

                if (dropLine != null) dropLine.SetActive(false);

                // Ease-in (accelerating, like gravity)
                while (elapsed < duration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / duration);
                    float eased = t * t;
                    dropperRT.anchoredPosition = new Vector2(0, Mathf.Lerp(startY, targetY, eased));
                    yield return null;
                }
            }

            // Transition immediately — no visible pause
            SetStep(1);
        }

        private void OnSkipClicked()
        {
            FinishOnboarding();
        }

        private void OnStartClicked()
        {
            FinishOnboarding();
        }

        private void FinishOnboarding()
        {
            GameSession.MarkOnboardingComplete();
            if (ScreenManager.Instance != null)
                ScreenManager.Instance.NavigateTo(Screen.HomeFresh);
        }

        // ───── Demo ball helpers ─────

        private GameObject CreateDemoBall(Transform parent, int tierIndex, Vector2 pos, float forcedSize)
        {
            var go = OvertoneUI.CreateUIObject($"Ball_T{tierIndex}", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(forcedSize, forcedSize);

            var img = go.AddComponent<Image>();

            // Generate a real ball sprite at UI-friendly size
            float uiRadius = forcedSize / (2f * Visual.NeonBallRenderer.PixelsPerUnit);
            var png = Visual.NeonBallRenderer.GenerateBallPNG(tierIndex, Color.white, uiRadius, 0f);
            int expectedSize = Mathf.Max(8, Mathf.RoundToInt(uiRadius * 2f * Visual.NeonBallRenderer.PixelsPerUnit)) + 8;
            var tex = new Texture2D(expectedSize, expectedSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.LoadImage(png);
            img.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), tex.width);
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white;
            return go;
        }

        private GameObject CreateDropLine(Transform parent)
        {
            var go = OvertoneUI.CreateUIObject("DropLine", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(1, 180);

            var img = go.AddComponent<Image>();
            img.color = OC.A(OC.cyan, 0.25f);
            img.raycastTarget = false;

            return go;
        }

        private GameObject CreateMergeRing(Transform parent, Vector2 pos)
        {
            var go = OvertoneUI.CreateUIObject("MergeRing", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(50, 50);

            var img = go.AddComponent<Image>();
            img.sprite = Visual.PixelUIGenerator.GetRoundedRect9Slice();
            img.type = Image.Type.Sliced;
            img.color = OC.A(OC.cyan, 0.5f);
            img.raycastTarget = false;

            return go;
        }

        private GameObject CreateScorePop(Transform parent, Vector2 pos)
        {
            var go = OvertoneUI.CreateUIObject("ScorePop", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(80, 20);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "+ 100";
            tmp.font = OvertoneUI.PressStart2P;
            tmp.fontSize = OFont.labelXs;
            tmp.color = OC.amber;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;

            go.SetActive(false);
            return go;
        }

        private void AddSpacer(Transform parent, float height)
        {
            var spacer = OvertoneUI.CreateUIObject("Spacer", parent);
            var le = spacer.AddComponent<LayoutElement>();
            le.preferredHeight = height;
            le.minHeight = height;
        }
    }
}
