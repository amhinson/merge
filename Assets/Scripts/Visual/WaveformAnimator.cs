using UnityEngine;
using MergeGame.Data;

namespace MergeGame.Visual
{
    /// <summary>
    /// Smooth waveform scroll animation using a shader.
    /// The ball body is a static pre-cached texture. The wave is drawn
    /// procedurally by the shader using a continuously interpolated phase.
    /// No frame caching, no discrete steps — perfectly smooth.
    /// </summary>
    public class WaveformAnimator : MonoBehaviour
    {
        private SpriteRenderer spriteRenderer;
        private MaterialPropertyBlock mpb;
        private float phase;
        private float speed;

        private const float CycleSeconds = 30f;

        // Shader property IDs (cached for performance)
        private static int _Phase, _WaveColor, _Freq, _WaveType, _Amp;
        private static int _LineWidth, _HaloWidth, _BallRadiusUV;
        private static bool idsReady;

        private static Material sharedMaterial;
        private static Sprite[] bodyCache;

        private static readonly float[] DefaultRadii =
            { 0.22f, 0.30f, 0.40f, 0.50f, 0.60f, 0.70f, 0.80f, 0.90f, 1.10f, 1.20f, 1.40f };

        public void Initialize(BallData data, Color color)
        {
            int tier = data != null ? data.tierIndex : 0;
            float radius = data != null ? data.radius : 0.5f;
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer == null) return;

            EnsureSetup();
            EnsureBodyCached(tier, radius);

            spriteRenderer.sprite = bodyCache[tier];
            spriteRenderer.material = sharedMaterial;

            // Set per-instance wave properties
            mpb = new MaterialPropertyBlock();
            spriteRenderer.GetPropertyBlock(mpb);

            var ballColor = BallRenderer.GetBallColor(tier);
            var wc = tier < BallRenderer.WaveConfig.Length
                ? BallRenderer.WaveConfig[tier]
                : (3, 0, 0.80f);

            float ballRadiusUV = BallRenderer.GetBallRadiusUV(radius);
            // Convert fixed pixel widths to normalized ball coords
            float lineWidthNorm = 2.5f / ((radius * 2f * BallRenderer.PixelsPerUnit / 2f) - 2f);
            float haloWidthNorm = 6.0f / ((radius * 2f * BallRenderer.PixelsPerUnit / 2f) - 2f);

            mpb.SetColor(_WaveColor, new Color(ballColor.r, ballColor.g, ballColor.b, wc.Item3));
            mpb.SetFloat(_Freq, wc.Item1);
            mpb.SetFloat(_WaveType, wc.Item2);
            mpb.SetFloat(_Amp, 0.22f);
            mpb.SetFloat(_LineWidth, lineWidthNorm);
            mpb.SetFloat(_HaloWidth, haloWidthNorm);
            mpb.SetFloat(_BallRadiusUV, ballRadiusUV);

            // Random start so same-tier balls aren't in sync
            phase = Random.Range(0f, 1f);
            speed = 1f / CycleSeconds;

            mpb.SetFloat(_Phase, phase);
            spriteRenderer.SetPropertyBlock(mpb);
        }

        private void Update()
        {
            if (mpb == null || spriteRenderer == null) return;

            phase += Time.deltaTime * speed;
            if (phase >= 1f) phase -= 1f;

            mpb.SetFloat(_Phase, phase);
            spriteRenderer.SetPropertyBlock(mpb);
        }

        // ===== Static setup =====

        private static void EnsureSetup()
        {
            if (!idsReady)
            {
                _Phase = Shader.PropertyToID("_Phase");
                _WaveColor = Shader.PropertyToID("_WaveColor");
                _Freq = Shader.PropertyToID("_Freq");
                _WaveType = Shader.PropertyToID("_WaveType");
                _Amp = Shader.PropertyToID("_Amp");
                _LineWidth = Shader.PropertyToID("_LineWidth");
                _HaloWidth = Shader.PropertyToID("_HaloWidth");
                _BallRadiusUV = Shader.PropertyToID("_BallRadiusUV");
                idsReady = true;
            }

            if (sharedMaterial == null)
            {
                var shader = Shader.Find("Overtone/WaveformBall");
                if (shader != null)
                    sharedMaterial = new Material(shader);
                else
                    Debug.LogWarning("WaveformBall shader not found — falling back to default sprite shader");
            }

            if (bodyCache == null)
                bodyCache = new Sprite[11];
        }

        private static void EnsureBodyCached(int tier, float radius)
        {
            if (bodyCache[tier] != null) return;

            var color = BallRenderer.GetBallColor(tier);
            var pixels = BallRenderer.GenerateStaticBallPixels(tier, color, radius, out int texSize);

            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels(pixels);
            tex.Apply(false, true);

            bodyCache[tier] = Sprite.Create(tex, new Rect(0, 0, texSize, texSize),
                new Vector2(0.5f, 0.5f), BallRenderer.PixelsPerUnit);
        }

        /// <summary>
        /// Pre-generate static body sprites for all tiers. Call during loading.
        /// </summary>
        public static void PrewarmCache()
        {
            EnsureSetup();
            for (int t = 0; t < 11; t++)
            {
                float radius = t < DefaultRadii.Length ? DefaultRadii[t] : 0.5f;
                EnsureBodyCached(t, radius);
            }
        }

        public static void ClearCache()
        {
            bodyCache = null;
            sharedMaterial = null;
            idsReady = false;
        }
    }
}
