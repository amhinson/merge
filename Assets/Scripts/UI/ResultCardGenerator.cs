using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using MergeGame.Core;
using MergeGame.Data;

namespace MergeGame.UI
{
    /// <summary>
    /// Generates a shareable result card image after a scored round.
    /// Each element is a modular piece — layout to be directed separately.
    /// </summary>
    public class ResultCardGenerator : MonoBehaviour
    {
        public static ResultCardGenerator Instance { get; private set; }

        [Header("Card Camera")]
        [SerializeField] private Camera cardCamera;
        [SerializeField] private RenderTexture cardRenderTexture;

        [Header("Card Elements — Modular, repositionable")]
        [SerializeField] private TextMeshProUGUI dayLabel;       // "Overtone #47"
        [SerializeField] private TextMeshProUGUI scoreLabel;     // Score in large type
        [SerializeField] private TextMeshProUGUI streakLabel;    // Current streak
        [SerializeField] private Transform topMergesContainer;   // Parent for merge ball icons
        [SerializeField] private GameObject mergeBallIconPrefab; // Small colored circle

        [Header("Card Background")]
        [SerializeField] private Image cardBackground;

        [Header("References")]
        [SerializeField] private BallTierConfig tierConfig;

        private Texture2D generatedCard;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void PopulateCard(int dayNumber, int score, int streak, int[] topMergeTiers)
        {
            if (dayLabel != null)
                dayLabel.text = $"Overtone #{dayNumber}";

            if (scoreLabel != null)
                scoreLabel.text = score.ToString();

            if (streakLabel != null)
                streakLabel.text = streak > 0 ? $"{streak} day streak" : "";

            // Populate top merge icons
            if (topMergesContainer != null)
            {
                // Clear existing icons
                foreach (Transform child in topMergesContainer)
                    Destroy(child.gameObject);

                if (topMergeTiers != null && tierConfig != null)
                {
                    foreach (int tier in topMergeTiers)
                    {
                        var data = tierConfig.GetTier(tier);
                        if (data == null) continue;

                        if (mergeBallIconPrefab != null)
                        {
                            var icon = Instantiate(mergeBallIconPrefab, topMergesContainer);
                            var img = icon.GetComponent<Image>();
                            if (img != null)
                            {
                                img.color = data.color;
                                // Scale by tier
                                float scale = 0.5f + (tier / 10f) * 0.5f;
                                icon.transform.localScale = Vector3.one * scale;
                            }
                        }
                    }
                }
            }
        }

        public void CaptureCard(System.Action<Texture2D> onCaptured)
        {
            StartCoroutine(CaptureCoroutine(onCaptured));
        }

        private IEnumerator CaptureCoroutine(System.Action<Texture2D> onCaptured)
        {
            yield return new WaitForEndOfFrame();

            if (cardCamera != null && cardRenderTexture != null)
            {
                cardCamera.Render();

                RenderTexture.active = cardRenderTexture;
                generatedCard = new Texture2D(cardRenderTexture.width, cardRenderTexture.height, TextureFormat.RGB24, false);
                generatedCard.ReadPixels(new Rect(0, 0, cardRenderTexture.width, cardRenderTexture.height), 0, 0);
                generatedCard.Apply();
                RenderTexture.active = null;
            }

            onCaptured?.Invoke(generatedCard);
        }

        public void ShareCard()
        {
            if (generatedCard == null)
            {
                CaptureCard(tex => DoShare(tex));
            }
            else
            {
                DoShare(generatedCard);
            }
        }

        private void DoShare(Texture2D tex)
        {
            if (tex == null) return;

#if UNITY_WEBGL && !UNITY_EDITOR
            // WebGL: copy image to clipboard via JS interop
            byte[] png = tex.EncodeToPNG();
            string base64 = System.Convert.ToBase64String(png);
            CopyImageToClipboardJS(base64);
#elif (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
            // Mobile: use native share sheet
            ShareNative(tex);
#else
            Debug.Log("Share: would share result card image");
#endif
        }

#if UNITY_WEBGL && !UNITY_EDITOR
        [System.Runtime.InteropServices.DllImport("__Internal")]
        private static extern void CopyImageToClipboardJS(string base64Png);
#endif

#if (UNITY_IOS || UNITY_ANDROID) && !UNITY_EDITOR
        private void ShareNative(Texture2D tex)
        {
            string path = System.IO.Path.Combine(Application.temporaryCachePath, "overtone_result.png");
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            // TODO: Use a native share plugin (e.g., NativeShare) for production
            Debug.Log($"Share: saved card to {path}");
        }
#endif
    }
}
