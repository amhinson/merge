using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using MergeGame.Core;
using MergeGame.Data;

namespace MergeGame.UI
{
    /// <summary>
    /// Interactive onboarding: user taps to drop balls, watches them merge.
    /// Two merge cycles, then LET'S PLAY appears.
    /// </summary>
    public class OnboardingScreen : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BallTierConfig tierConfig;

        // State machine
        private enum State { WaitingForDrop1, Dropping1, Merging1, WaitingForDrop2, Dropping2, Merging2, Done }
        private State state;

        // UI elements
        private TextMeshProUGUI headingLabel;
        private GameObject startButton;
        private CanvasGroup startButtonCG;
        private GameObject ctaTagline;
        private RectTransform demoArena;

        // Active balls in the arena
        private GameObject dropperBall;
        private GameObject sittingBall;
        private GameObject scorePop;

        private Coroutine animCoroutine;
        private bool isBuilt;

        // Ball sizes per tier (UI pixels)
        private const float Tier0Size = 32f;
        private const float Tier1Size = 40f;
        private const float Tier2Size = 52f;

        // Arena positions
        private const float DropperY = 115f;
        private const float FloorY = -100f;

        private void OnEnable()
        {
            if (!isBuilt) { BuildUI(); isBuilt = true; }
            StartSequence();
        }

        private void Update()
        {
            if (state != State.WaitingForDrop1 && state != State.WaitingForDrop2) return;
            if (Input.GetMouseButtonDown(0))
            {
                if (state == State.WaitingForDrop1)
                {
                    state = State.Dropping1;
                    if (animCoroutine != null) StopCoroutine(animCoroutine);
                    animCoroutine = StartCoroutine(DropAndMerge1());
                }
                else if (state == State.WaitingForDrop2)
                {
                    state = State.Dropping2;
                    if (animCoroutine != null) StopCoroutine(animCoroutine);
                    animCoroutine = StartCoroutine(DropAndMerge2());
                }
            }
        }

        // ───── Sequence ─────

        private void StartSequence()
        {
            // Clean any existing balls
            ClearArena();

            state = State.WaitingForDrop1;

            headingLabel.text = "DROP";

            // Place a tier-0 ball at the bottom
            sittingBall = CreateDemoBall(demoArena.transform, 0, new Vector2(0, FloorY), Tier0Size);

            // Dropper at top
            dropperBall = CreateDemoBall(demoArena.transform, 0, new Vector2(0, DropperY), Tier0Size);

            // Start dropper bob
            if (animCoroutine != null) StopCoroutine(animCoroutine);
            animCoroutine = StartCoroutine(BobDropper());

            // Hide start button
            if (startButton != null) startButton.SetActive(false);
        }

        private IEnumerator DropAndMerge1()
        {
            // Drop the ball
            yield return DropBall(dropperBall, FloorY + Tier0Size);

            // Heading: MERGE

            headingLabel.text = "MERGE";

            // Absorb animation
            yield return AbsorbBalls(dropperBall, sittingBall, new Vector2(0, FloorY));

            // Particles
            SpawnMergeParticles(demoArena.transform, new Vector2(0, FloorY));

            // Destroy old balls
            Destroy(dropperBall);
            Destroy(sittingBall);
            dropperBall = null;
            sittingBall = null;

            // New tier-1 ball scales in at merge point
            sittingBall = CreateDemoBall(demoArena.transform, 1, new Vector2(0, FloorY), Tier1Size);
            sittingBall.GetComponent<RectTransform>().localScale = Vector3.one * (Tier0Size / Tier1Size);
            yield return ScaleObject(sittingBall, sittingBall.GetComponent<RectTransform>().localScale, Vector3.one * 1.1f, 0.15f);
            yield return ScaleObject(sittingBall, Vector3.one * 1.1f, Vector3.one, 0.08f);

            yield return new WaitForSeconds(0.3f);

            // Heading: SCORE, show score pop

            headingLabel.text = "SCORE";
            yield return ShowScorePop(new Vector2(0, FloorY + Tier1Size * 0.6f), "+ 5");

            yield return new WaitForSeconds(0.3f);

            // New tier-1 dropper appears at top — no heading change, let user figure it out
            dropperBall = CreateDemoBall(demoArena.transform, 1, new Vector2(0, DropperY), Tier1Size);
            dropperBall.GetComponent<RectTransform>().localScale = Vector3.zero;
            yield return ScaleObject(dropperBall, Vector3.zero, Vector3.one * 1.1f, 0.15f);
            yield return ScaleObject(dropperBall, Vector3.one * 1.1f, Vector3.one, 0.08f);

            // Allow drops immediately, fade heading in background
            state = State.WaitingForDrop2;
            animCoroutine = StartCoroutine(BobDropper());
            StartCoroutine(FadeHeadingDelayed(0.5f, 0.3f));
        }

        private IEnumerator DropAndMerge2()
        {
            // Drop the ball
            yield return DropBall(dropperBall, FloorY + Tier1Size);

            // Absorb
            yield return AbsorbBalls(dropperBall, sittingBall, new Vector2(0, FloorY));

            // Particles
            SpawnMergeParticles(demoArena.transform, new Vector2(0, FloorY));

            // Destroy old balls
            Destroy(dropperBall);
            Destroy(sittingBall);
            dropperBall = null;
            sittingBall = null;

            // New tier-2 ball scales in
            sittingBall = CreateDemoBall(demoArena.transform, 2, new Vector2(0, FloorY), Tier2Size);
            sittingBall.GetComponent<RectTransform>().localScale = Vector3.one * (Tier1Size / Tier2Size);
            yield return ScaleObject(sittingBall, sittingBall.GetComponent<RectTransform>().localScale, Vector3.one * 1.1f, 0.15f);
            yield return ScaleObject(sittingBall, Vector3.one * 1.1f, Vector3.one, 0.08f);

            yield return new WaitForSeconds(0.2f);

            // Show score pop
            yield return ShowScorePop(new Vector2(0, FloorY + Tier2Size * 0.6f), "+ 10");

            state = State.Done;

            // Show tagline + LET'S PLAY with fade-in
            yield return new WaitForSeconds(0.3f);

            if (ctaTagline != null)
            {
                ctaTagline.SetActive(true);
                var tagCG = ctaTagline.GetComponent<CanvasGroup>();
                if (tagCG != null) tagCG.alpha = 0f;
            }

            if (startButton != null)
            {
                startButton.SetActive(true);
                if (startButtonCG != null) startButtonCG.alpha = 0f;

                float elapsed = 0f;
                while (elapsed < 0.3f)
                {
                    elapsed += Time.deltaTime;
                    float a = Mathf.Clamp01(elapsed / 0.3f);
                    if (startButtonCG != null) startButtonCG.alpha = a;
                    var tagCG = ctaTagline != null ? ctaTagline.GetComponent<CanvasGroup>() : null;
                    if (tagCG != null) tagCG.alpha = a;
                    yield return null;
                }
                if (startButtonCG != null) startButtonCG.alpha = 1f;
                var finalCG = ctaTagline != null ? ctaTagline.GetComponent<CanvasGroup>() : null;
                if (finalCG != null) finalCG.alpha = 1f;
            }
        }

        // ───── Animations ─────

        private IEnumerator BobDropper()
        {
            if (dropperBall == null) yield break;
            var rt = dropperBall.GetComponent<RectTransform>();
            while (state == State.WaitingForDrop1 || state == State.WaitingForDrop2)
            {
                if (rt == null) yield break;
                float t = Mathf.PingPong(Time.time * 0.7f, 1f);
                float eased = Mathf.Sin(t * Mathf.PI);
                rt.anchoredPosition = new Vector2(0, DropperY + eased * 6f);
                yield return null;
            }
        }

        private IEnumerator DropBall(GameObject ball, float targetY)
        {
            if (ball == null) yield break;
            var rt = ball.GetComponent<RectTransform>();
            float startY = rt.anchoredPosition.y;
            float duration = 0.4f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = t * t; // ease-in (gravity)
                rt.anchoredPosition = new Vector2(0, Mathf.Lerp(startY, targetY, eased));
                yield return null;
            }
            rt.anchoredPosition = new Vector2(0, targetY);
        }

        private IEnumerator AbsorbBalls(GameObject a, GameObject b, Vector2 mergePoint)
        {
            if (a == null || b == null) yield break;
            var rtA = a.GetComponent<RectTransform>();
            var rtB = b.GetComponent<RectTransform>();
            Vector2 startA = rtA.anchoredPosition;
            Vector2 startB = rtB.anchoredPosition;
            Vector3 scaleA = rtA.localScale;
            Vector3 scaleB = rtB.localScale;

            float duration = 0.12f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float eased = t * t;

                if (rtA != null)
                {
                    rtA.anchoredPosition = Vector2.Lerp(startA, mergePoint, eased);
                    rtA.localScale = Vector3.Lerp(scaleA, scaleA * 0.3f, eased);
                }
                if (rtB != null)
                {
                    rtB.anchoredPosition = Vector2.Lerp(startB, mergePoint, eased);
                    rtB.localScale = Vector3.Lerp(scaleB, scaleB * 0.3f, eased);
                }
                yield return null;
            }
        }

        private IEnumerator FadeHeadingDelayed(float delay, float duration)
        {
            yield return new WaitForSeconds(delay);
            yield return FadeHeading(duration);
        }

        private IEnumerator FadeHeading(float duration)
        {
            if (headingLabel == null) yield break;
            Color startColor = headingLabel.color;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                headingLabel.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
                yield return null;
            }
            headingLabel.text = "";
            headingLabel.color = startColor; // restore alpha for next use
        }

        private IEnumerator ShowScorePop(Vector2 pos, string text = "+ 10")
        {
            if (scorePop == null) yield break;
            var tmp = scorePop.GetComponent<TextMeshProUGUI>();
            if (tmp != null) tmp.text = text;
            var popRT = scorePop.GetComponent<RectTransform>();
            var popCG = scorePop.GetComponent<CanvasGroup>();
            if (popCG == null) popCG = scorePop.AddComponent<CanvasGroup>();

            popRT.anchoredPosition = pos;
            scorePop.SetActive(true);
            popCG.alpha = 0f;

            float elapsed = 0f;
            float baseY = pos.y;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 0.3f);
                popCG.alpha = t;
                popRT.anchoredPosition = new Vector2(0, baseY + t * 16f);
                yield return null;
            }

            yield return new WaitForSeconds(0.4f);

            elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / 0.3f);
                popCG.alpha = 1f - t;
                yield return null;
            }
            scorePop.SetActive(false);
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

        // ───── UI Construction ─────

        private void BuildUI()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();

            var bg = gameObject.GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            bg.color = OC.bg;

            var content = OvertoneUI.CreateUIObject("Content", transform);
            var contentRT = content.GetComponent<RectTransform>();
            OvertoneUI.StretchFill(contentRT);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 0;

            BuildLogoBlock(content.transform);
            AddFlex(content.transform, 1f);
            BuildDemoArena(content.transform);
            AddFlex(content.transform, 0.6f);
            BuildHeadingLabel(content.transform);
            AddSpacer(content.transform, 44 + OS.safeAreaBottom);

            // CTA tagline + button: absolute positioned so they don't shift layout
            BuildCtaTagline(transform);
            BuildStartButton(transform);
        }

        private void BuildLogoBlock(Transform parent)
        {
            var block = OvertoneUI.CreateUIObject("LogoBlock", parent);
            var blockLE = block.AddComponent<LayoutElement>();
            blockLE.preferredHeight = 90 + OS.safeAreaTop;

            var inner = OvertoneUI.CreateUIObject("LogoInner", block.transform);
            var innerRT = inner.GetComponent<RectTransform>();
            innerRT.anchorMin = Vector2.zero; innerRT.anchorMax = Vector2.one;
            innerRT.offsetMin = Vector2.zero; innerRT.offsetMax = Vector2.zero;
            var innerVLG = inner.AddComponent<VerticalLayoutGroup>();
            innerVLG.childAlignment = TextAnchor.MiddleCenter;
            innerVLG.spacing = 6;
            innerVLG.childControlWidth = false;
            innerVLG.childControlHeight = false;
            innerVLG.childForceExpandWidth = false;
            innerVLG.childForceExpandHeight = false;
            innerVLG.padding = new RectOffset(0, 0, (int)OS.safeAreaTop + 20, 0);

            var titleGO = OvertoneUI.CreateUIObject("Title", inner.transform);
            titleGO.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 30);
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

            var taglineGO = OvertoneUI.CreateUIObject("Tagline", inner.transform);
            taglineGO.GetComponent<RectTransform>().sizeDelta = new Vector2(300, 16);
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
            var wrapper = OvertoneUI.CreateUIObject("ArenaWrapper", parent);
            var wrapperHLG = wrapper.AddComponent<HorizontalLayoutGroup>();
            wrapperHLG.childAlignment = TextAnchor.MiddleCenter;
            wrapperHLG.childControlWidth = false;
            wrapperHLG.childControlHeight = false;
            wrapperHLG.childForceExpandWidth = false;
            var wrapperLE = wrapper.AddComponent<LayoutElement>();
            wrapperLE.preferredHeight = 300;
            wrapperLE.flexibleHeight = 0.5f;

            var arenaGO = OvertoneUI.CreateUIObject("DemoArena", wrapper.transform);
            demoArena = arenaGO.GetComponent<RectTransform>();
            demoArena.sizeDelta = new Vector2(260, 300);
            var le = arenaGO.AddComponent<LayoutElement>();
            le.preferredWidth = 260;
            le.preferredHeight = 300;

            var arenaBG = arenaGO.AddComponent<Image>();
            arenaBG.color = OC.surface;
            arenaBG.sprite = Visual.PixelUIGenerator.GetRoundedRect9Slice();
            arenaBG.type = Image.Type.Sliced;

            var outline = OvertoneUI.CreateUIObject("Outline", arenaGO.transform);
            var outlineImg = outline.AddComponent<Image>();
            outlineImg.sprite = Visual.PixelUIGenerator.GetRoundedRect9Slice();
            outlineImg.type = Image.Type.Sliced;
            outlineImg.color = OC.border;
            outlineImg.raycastTarget = false;
            OvertoneUI.StretchFill(outline.GetComponent<RectTransform>());

            BuildGridOverlay(arenaGO.transform);

            // Score pop (reusable, hidden by default)
            scorePop = CreateScorePop(arenaGO.transform);
        }

        private void BuildGridOverlay(Transform parent)
        {
            var gridGO = OvertoneUI.CreateUIObject("GridOverlay", parent);
            OvertoneUI.StretchFill(gridGO.GetComponent<RectTransform>());
            gridGO.AddComponent<RectMask2D>();

            int gridCols = 6;
            int gridRows = 7;
            Color lineColor = new Color(1f, 1f, 1f, 0.06f);

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
                line.AddComponent<Image>().color = lineColor;
                line.GetComponent<Image>().raycastTarget = false;
            }

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
                line.AddComponent<Image>().color = lineColor;
                line.GetComponent<Image>().raycastTarget = false;
            }
        }

        private void BuildHeadingLabel(Transform parent)
        {
            var block = OvertoneUI.CreateUIObject("HeadingBlock", parent);
            var blockLE = block.AddComponent<LayoutElement>();
            blockLE.preferredHeight = 24;
            blockLE.minHeight = 24;

            headingLabel = OvertoneUI.CreateLabel(block.transform, "DROP",
                OvertoneUI.PressStart2P, 12, OC.white, "Heading");
            headingLabel.characterSpacing = 2;
            headingLabel.alignment = TextAlignmentOptions.Center;
            OvertoneUI.StretchFill(headingLabel.GetComponent<RectTransform>());
        }

        private void BuildCtaTagline(Transform parent)
        {
            ctaTagline = OvertoneUI.CreateUIObject("CtaTagline", parent);
            var tagRT = ctaTagline.GetComponent<RectTransform>();
            tagRT.anchorMin = new Vector2(0, 0);
            tagRT.anchorMax = new Vector2(1, 0);
            tagRT.pivot = new Vector2(0.5f, 0);
            tagRT.anchoredPosition = new Vector2(0, 60 + OS.safeAreaBottom + 44 + 16);
            tagRT.sizeDelta = new Vector2(0, 36);

            var tmp = OvertoneUI.CreateLabel(ctaTagline.transform,
                "new sequence every day\neveryone plays the same drop",
                OvertoneUI.DMMono, OFont.bodySm, OC.muted, "TaglineText");
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.lineSpacing = 8;
            OvertoneUI.StretchFill(tmp.GetComponent<RectTransform>());

            ctaTagline.AddComponent<CanvasGroup>();
            ctaTagline.SetActive(false);
        }

        private void BuildStartButton(Transform parent)
        {
            // Absolute positioned at bottom, matching home screen CTA padding
            var wrapper = OvertoneUI.CreateUIObject("ButtonWrapper", parent);
            var wrapperRT = wrapper.GetComponent<RectTransform>();
            wrapperRT.anchorMin = new Vector2(0, 0);
            wrapperRT.anchorMax = new Vector2(1, 0);
            wrapperRT.pivot = new Vector2(0.5f, 0);
            wrapperRT.anchoredPosition = new Vector2(0, 24 + OS.safeAreaBottom);
            wrapperRT.sizeDelta = new Vector2(-48, 44); // -48 = 24px padding each side

            var (btnGO, _) = OvertoneUI.CreatePrimaryButton(wrapper.transform, "LET'S PLAY", 44, "StartButton");
            OvertoneUI.StretchFill(btnGO.GetComponent<RectTransform>());
            btnGO.GetComponent<Button>().onClick.AddListener(FinishOnboarding);
            startButton = btnGO;

            startButtonCG = btnGO.AddComponent<CanvasGroup>();
            startButton.SetActive(false);
        }

        private void ClearArena()
        {
            if (dropperBall != null) { Destroy(dropperBall); dropperBall = null; }
            if (sittingBall != null) { Destroy(sittingBall); sittingBall = null; }
        }

        // ───── Helpers ─────

        private void FinishOnboarding()
        {
            GameSession.MarkOnboardingComplete();
            if (ScreenManager.Instance != null)
                ScreenManager.Instance.NavigateTo(Screen.HomeFresh);
        }

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
            float uiRadius = forcedSize / (2f * Visual.BallRenderer.PixelsPerUnit);
            var png = Visual.BallRenderer.GenerateBallPNG(tierIndex, Color.white, uiRadius, 0f);
            int expectedSize = Mathf.Max(8, Mathf.RoundToInt(uiRadius * 2f * Visual.BallRenderer.PixelsPerUnit)) + 8;
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

        private GameObject CreateScorePop(Transform parent)
        {
            var go = OvertoneUI.CreateUIObject("ScorePop", parent);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(80, 20);

            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = "+ 10";
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

        private void AddFlex(Transform parent, float weight)
        {
            var flex = OvertoneUI.CreateUIObject("Flex", parent);
            var le = flex.AddComponent<LayoutElement>();
            le.flexibleHeight = weight;
            le.minHeight = 0;
        }
    }
}
