using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using MergeGame.Data;
using MergeGame.Visual;

namespace MergeGame.UI
{
    /// <summary>
    /// Looping merge animation on the home screen.
    /// Tier 0 → 1 → 2 → ... → 10, then restarts.
    /// All animations match BallController.MergeCoroutine exactly.
    /// </summary>
    public class HomeMergeLoop : MonoBehaviour
    {
        private RectTransform container;
        private GameObject sittingBall;
        private GameObject droppingBall;
        private Coroutine loopCoroutine;

        // Ball sizes per tier (UI pixels)
        private static readonly float[] TierSizes =
            { 28f, 34f, 40f, 46f, 52f, 58f, 64f, 72f, 82f, 92f, 104f };

        private const float DropStartY = 500f; // above the visible screen
        private const float CenterY = -10f;
        private const int MaxTier = 10;

        // Timing — intentionally unhurried
        private const float InitialPause = 0.6f;
        private const float PreDropPause = 0.7f;
        private const float PostMergePause = 1.0f;
        private const float FinalPause = 1.5f;

        // Animation durations — matching BallController exactly
        private const float DropDuration = 1.0f;
        private const float DropFadeInDuration = 0.12f;
        private const float AbsorbDuration = 0.12f; // exact match: physicsConfig.mergeAbsorbDuration
        private const float ScaleDuration = 0.18f;  // exact match: physicsConfig.mergeScaleDuration
        private const float FadeDuration = 0.4f;

        // Pre-cached sprites per tier
        private Sprite[] cachedSprites;

        public void Initialize()
        {
            container = GetComponent<RectTransform>();

            PrewarmSprites();
            StartLoop();
        }

        private void PrewarmSprites()
        {
            cachedSprites = new Sprite[TierSizes.Length];
            for (int tier = 0; tier < TierSizes.Length; tier++)
            {
                float size = TierSizes[tier];
                float renderRadius = Mathf.Max(size / (2f * BallRenderer.PixelsPerUnit), 0.5f);
                var color = BallRenderer.GetBallColor(tier);
                float phase = tier * 0.09f;
                var pixels = BallRenderer.GenerateBallPixels(tier, color, renderRadius, phase, out int texSize);
                var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.SetPixels(pixels);
                tex.Apply(false, true);
                cachedSprites[tier] = Sprite.Create(tex, new Rect(0, 0, texSize, texSize),
                    new Vector2(0.5f, 0.5f), texSize);
            }
        }

        private void OnEnable()
        {
            if (container != null && loopCoroutine == null)
                StartLoop();
        }

        private void OnDisable()
        {
            if (loopCoroutine != null) { StopCoroutine(loopCoroutine); loopCoroutine = null; }
            ClearBalls();
        }

        private void StartLoop()
        {
            if (loopCoroutine != null) StopCoroutine(loopCoroutine);
            ClearBalls();
            loopCoroutine = StartCoroutine(MergeLoopCoroutine());
        }

        private IEnumerator MergeLoopCoroutine()
        {
            // Wait for layout and initial data fetches to settle
            yield return new WaitForSeconds(0.5f);

            while (true)
            {
                ClearBalls();

                // First tier-0 ball drops in from above to settle at center
                sittingBall = CreateBall(0, new Vector2(0, DropStartY));
                yield return DropWithFadeIn(sittingBall, CenterY);
                yield return new WaitForSeconds(PreDropPause);

                // Merge through tiers 0→1, 1→2, ... 9→10
                for (int tier = 0; tier < MaxTier; tier++)
                {
                    float oldSize = TierSizes[tier];
                    float newSize = TierSizes[tier + 1];

                    // Drop a matching ball — lands touching the sitting ball
                    // Merge point is at the LOWER ball (matches game: spawnPos = lower ball)
                    Vector2 mergePoint = new Vector2(0, CenterY);
                    float landingY = CenterY + oldSize; // edges touching

                    droppingBall = CreateBall(tier, new Vector2(0, DropStartY));

                    yield return DropWithFadeIn(droppingBall, landingY);

                    // Absorb immediately on contact — no pause (matches game)
                    yield return AbsorbBalls(droppingBall, sittingBall, mergePoint);

                    // Particles
                    SpawnParticles(tier + 1, mergePoint);

                    // Destroy originals
                    if (droppingBall != null) { Destroy(droppingBall); droppingBall = null; }
                    if (sittingBall != null) { Destroy(sittingBall); sittingBall = null; }

                    // New merged ball scales in
                    sittingBall = CreateBall(tier + 1, mergePoint);
                    float scaleRatio = oldSize / newSize;
                    yield return MergeScaleIn(sittingBall, scaleRatio);

                    yield return new WaitForSeconds(PostMergePause);
                }

                // Final merge: drop another max-tier ball, both disappear
                float maxSize = TierSizes[MaxTier];
                Vector2 finalMerge = new Vector2(0, CenterY);
                float finalLandingY = CenterY + maxSize;

                droppingBall = CreateBall(MaxTier, new Vector2(0, DropStartY));
                SetAlpha(droppingBall, 0f);
                yield return DropWithFadeIn(droppingBall, finalLandingY);
                yield return AbsorbBalls(droppingBall, sittingBall, finalMerge);

                SpawnParticles(MaxTier + 1, finalMerge);

                if (droppingBall != null) { Destroy(droppingBall); droppingBall = null; }
                if (sittingBall != null) { Destroy(sittingBall); sittingBall = null; }

                // Pause before restarting
                yield return new WaitForSeconds(1.5f);
            }
        }

        // ───── Drop with fade-in ─────

        private IEnumerator DropWithFadeIn(GameObject ball, float targetY)
        {
            if (ball == null) yield break;
            var rt = ball.GetComponent<RectTransform>();
            var cg = EnsureCG(ball);
            cg.alpha = 1f; // fully visible — mask clips it until it enters
            float startY = rt.anchoredPosition.y;
            float elapsed = 0f;

            while (elapsed < DropDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / DropDuration);
                float eased = t * t; // gravity ease-in
                rt.anchoredPosition = new Vector2(0, Mathf.Lerp(startY, targetY, eased));
                yield return null;
            }
            rt.anchoredPosition = new Vector2(0, targetY);
        }

        // ───── Absorb (exact match: BallController.MergeCoroutine) ─────

        private IEnumerator AbsorbBalls(GameObject a, GameObject b, Vector2 mergePoint)
        {
            if (a == null || b == null) yield break;
            var rtA = a.GetComponent<RectTransform>();
            var rtB = b.GetComponent<RectTransform>();
            Vector2 startA = rtA.anchoredPosition;
            Vector2 startB = rtB.anchoredPosition;
            Vector3 scaleA = rtA.localScale;
            Vector3 scaleB = rtB.localScale;

            // 0.12s, ease-in quadratic, shrink to 30%
            float elapsed = 0f;
            while (elapsed < AbsorbDuration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / AbsorbDuration;
                float eased = t * t; // ease-in (accelerate into merge point)

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

        // ───── Scale-in (exact match: BallController.MergeScaleCoroutine) ─────

        private IEnumerator MergeScaleIn(GameObject ball, float startRatio)
        {
            if (ball == null) yield break;
            var rt = ball.GetComponent<RectTransform>();
            Vector3 target = Vector3.one;
            rt.localScale = target * startRatio;

            // 0.18s, back ease-out with s=1.4 (overshoots ~10% then settles)
            float elapsed = 0f;
            while (elapsed < ScaleDuration)
            {
                if (ball == null) yield break;
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / ScaleDuration);
                float s = 1.4f;
                float eased = 1f + (t - 1f) * (t - 1f) * ((s + 1f) * (t - 1f) + s);
                rt.localScale = Vector3.LerpUnclamped(target * startRatio, target, eased);
                yield return null;
            }
            if (ball != null) rt.localScale = target;
        }

        // ───── Fade helpers ─────

        private IEnumerator FadeIn(GameObject ball, float duration)
        {
            if (ball == null) yield break;
            var cg = EnsureCG(ball);
            cg.alpha = 0f;
            ball.transform.localScale = Vector3.one * 0.8f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                cg.alpha = t;
                ball.transform.localScale = Vector3.Lerp(Vector3.one * 0.8f, Vector3.one, t);
                yield return null;
            }
            cg.alpha = 1f;
            ball.transform.localScale = Vector3.one;
        }

        private IEnumerator FadeOut(GameObject ball, float duration)
        {
            if (ball == null) yield break;
            var cg = EnsureCG(ball);
            Vector3 startScale = ball.transform.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                cg.alpha = 1f - t;
                ball.transform.localScale = Vector3.Lerp(startScale, startScale * 0.8f, t);
                yield return null;
            }
            cg.alpha = 0f;
        }

        private void SetAlpha(GameObject go, float alpha)
        {
            var cg = EnsureCG(go);
            cg.alpha = alpha;
        }

        private CanvasGroup EnsureCG(GameObject go)
        {
            var cg = go.GetComponent<CanvasGroup>();
            if (cg == null) cg = go.AddComponent<CanvasGroup>();
            return cg;
        }

        // ───── Particles (matches MergeParticles exactly) ─────

        private void SpawnParticles(int resultTier, Vector2 center)
        {
            int count = 8 + resultTier;
            Color color = BallRenderer.GetBallColor(Mathf.Min(resultTier, 10));

            for (int i = 0; i < count; i++)
            {
                var particle = MurgeUI.CreateUIObject($"P{i}", transform);
                var prt = particle.GetComponent<RectTransform>();
                prt.anchorMin = new Vector2(0.5f, 0.5f);
                prt.anchorMax = new Vector2(0.5f, 0.5f);
                prt.pivot = new Vector2(0.5f, 0.5f);
                prt.anchoredPosition = center;
                prt.sizeDelta = new Vector2(4, 4);

                var img = particle.AddComponent<Image>();
                img.color = color;
                img.raycastTarget = false;

                var cg = particle.AddComponent<CanvasGroup>();
                cg.blocksRaycasts = false;
                StartCoroutine(AnimateParticle(prt, cg, i, count));
            }
        }

        private IEnumerator AnimateParticle(RectTransform prt, CanvasGroup cg, int index, int total)
        {
            float baseAngle = (360f / total) * index + Random.Range(-15f, 15f);
            float rad = baseAngle * Mathf.Deg2Rad;
            Vector2 dir = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));
            Vector2 startPos = prt.anchoredPosition;
            float speed = 80f * Random.Range(0.6f, 1.2f);
            const float lifetime = 0.4f; // matches MergeParticles.particleLifetime
            float elapsed = 0f;
            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / lifetime);
                prt.anchoredPosition = startPos + dir * speed * t;
                cg.alpha = 1f - t;
                yield return null;
            }
            Destroy(prt.gameObject);
        }

        // ───── Ball creation ─────

        private GameObject CreateBall(int tier, Vector2 pos)
        {
            float size = TierSizes[Mathf.Min(tier, TierSizes.Length - 1)];

            var go = MurgeUI.CreateUIObject($"MergeBall_T{tier}", transform);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(size, size);

            var img = go.AddComponent<Image>();
            img.type = Image.Type.Simple;
            img.preserveAspect = true;
            img.color = Color.white;
            img.raycastTarget = false;

            // Use pre-cached sprite (no per-ball texture generation)
            if (cachedSprites != null && tier < cachedSprites.Length && cachedSprites[tier] != null)
                img.sprite = cachedSprites[tier];

            return go;
        }

        private void ClearBalls()
        {
            if (sittingBall != null) { Destroy(sittingBall); sittingBall = null; }
            if (droppingBall != null) { Destroy(droppingBall); droppingBall = null; }
        }
    }
}
