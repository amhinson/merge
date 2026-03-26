using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using MergeGame.Core;
using MergeGame.Data;

namespace MergeGame.UI
{
    /// <summary>
    /// Simple loading screen shown on app startup.
    /// Displays the MURGE logo and a pulsing animated ball.
    /// Auto-hides once the game is ready.
    /// </summary>
    public class LoadingScreen : MonoBehaviour
    {
        private Image ballImage;
        private float pulseTimer;
        private bool isDismissing;

        public static LoadingScreen Instance { get; private set; }

        private void Awake()
        {
            Instance = this;
            BuildUI();

            // Pre-generate all ball sprites while loading screen is visible
            Visual.WaveformAnimator.PrewarmCache();
        }

        private void BuildUI()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();

            // Full screen dark background
            var bg = gameObject.AddComponent<Image>();
            bg.color = OC.bg;

            // Ensure it renders on top of everything
            var canvas = GetComponentInParent<Canvas>();
            if (canvas != null)
                transform.SetAsLastSibling();

            // MURGE title (centered upper area)
            var titleGO = MurgeUI.CreateUIObject("Title", transform);
            var titleRT = titleGO.GetComponent<RectTransform>();
            titleRT.anchorMin = new Vector2(0.5f, 0.5f);
            titleRT.anchorMax = new Vector2(0.5f, 0.5f);
            titleRT.pivot = new Vector2(0.5f, 0.5f);
            titleRT.anchoredPosition = new Vector2(0, 35);
            titleRT.sizeDelta = new Vector2(300, 30);
            var titleTMP = titleGO.AddComponent<TextMeshProUGUI>();
            string cyanHex = ColorUtility.ToHtmlStringRGB(OC.cyan);
            titleTMP.text = GameSession.AppName;
            titleTMP.font = MurgeUI.PressStart2P;
            titleTMP.fontSize = 20;
            titleTMP.color = OC.cyan;
            titleTMP.characterSpacing = 3;
            titleTMP.alignment = TextAlignmentOptions.Center;
            titleTMP.richText = true;
            titleTMP.raycastTarget = false;

            // Animated ball (centered)
            var ballGO = MurgeUI.CreateUIObject("Ball", transform);
            var ballRT = ballGO.GetComponent<RectTransform>();
            ballRT.anchorMin = new Vector2(0.5f, 0.5f);
            ballRT.anchorMax = new Vector2(0.5f, 0.5f);
            ballRT.pivot = new Vector2(0.5f, 0.5f);
            ballRT.anchoredPosition = new Vector2(0, -10);
            ballRT.sizeDelta = new Vector2(40, 40);

            ballImage = ballGO.AddComponent<Image>();
            // Generate a ball sprite
            float uiRadius = 40f / (2f * Visual.BallRenderer.PixelsPerUnit);
            var pixels = Visual.BallRenderer.GenerateBallPixels(
                10, Visual.BallRenderer.GetBallColor(10), uiRadius, 0f, out int texSize);
            var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.SetPixels(pixels);
            tex.Apply();
            ballImage.sprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize),
                new Vector2(0.5f, 0.5f), texSize);
            ballImage.preserveAspect = true;
            ballImage.color = Color.white;

            // Subtitle
            var subGO = MurgeUI.CreateUIObject("Sub", transform);
            var subRT = subGO.GetComponent<RectTransform>();
            subRT.anchorMin = new Vector2(0.5f, 0.5f);
            subRT.anchorMax = new Vector2(0.5f, 0.5f);
            subRT.pivot = new Vector2(0.5f, 0.5f);
            subRT.anchoredPosition = new Vector2(0, -55);
            subRT.sizeDelta = new Vector2(200, 16);
            var subTMP = subGO.AddComponent<TextMeshProUGUI>();
            subTMP.text = "A DAILY DROP";
            subTMP.font = MurgeUI.DMMono;
            subTMP.fontSize = 12;
            subTMP.color = OC.muted;
            subTMP.characterSpacing = 3;
            subTMP.alignment = TextAlignmentOptions.Center;
            subTMP.raycastTarget = false;
        }

        private void Update()
        {
            if (ballImage == null || isDismissing) return;

            // Gentle pulse animation
            pulseTimer += Time.deltaTime;
            float scale = 1f + 0.08f * Mathf.Sin(pulseTimer * 2f);
            ballImage.rectTransform.localScale = Vector3.one * scale;

            // Slowly rotate the ball's waveform by regenerating sprite
            if (Time.frameCount % 10 == 0)
            {
                float phase = (pulseTimer * 0.05f) % 1f;
                float uiRadius = 40f / (2f * Visual.BallRenderer.PixelsPerUnit);
                var pixels = Visual.BallRenderer.GenerateBallPixels(
                    10, Visual.BallRenderer.GetBallColor(10), uiRadius, phase, out int texSize);
                var tex = new Texture2D(texSize, texSize, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Bilinear;
                tex.SetPixels(pixels);
                tex.Apply();

                if (ballImage.sprite != null)
                {
                    if (ballImage.sprite.texture != null)
                        Destroy(ballImage.sprite.texture);
                    Destroy(ballImage.sprite);
                }

                ballImage.sprite = Sprite.Create(tex, new Rect(0, 0, texSize, texSize),
                    new Vector2(0.5f, 0.5f), texSize);
            }
        }

        public void Dismiss()
        {
            if (isDismissing) return;
            isDismissing = true;
            StartCoroutine(FadeOut());
        }

        private IEnumerator FadeOut()
        {
            var cg = gameObject.AddComponent<CanvasGroup>();
            float duration = 0.8f;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease out — slow start, fast end
                cg.alpha = 1f - (t * t);
                yield return null;
            }
            cg.alpha = 0f;
            gameObject.SetActive(false);
            Instance = null;
        }
    }
}
