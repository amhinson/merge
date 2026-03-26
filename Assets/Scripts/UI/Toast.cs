using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using MergeGame.Data;

namespace MergeGame.UI
{
    /// <summary>
    /// Lightweight toast notification. Call Toast.Show("message") from anywhere.
    /// Displays a short message at the bottom of the screen that auto-fades.
    /// </summary>
    public class Toast : MonoBehaviour
    {
        private CanvasGroup canvasGroup;
        private TextMeshProUGUI label;
        private static Toast instance;

        private const float ShowDuration = 2.0f;
        private const float FadeDuration = 0.5f;

        /// <summary>Show a toast message. Creates the UI on first call.</summary>
        public static void Show(string message)
        {
            EnsureInstance();
            if (instance == null) return;
            instance.Display(message);
        }

        private static void EnsureInstance()
        {
            if (instance != null) return;

            // Find the main canvas
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            // Container — anchored bottom-center, above safe area
            var go = MurgeUI.CreateUIObject("Toast", canvas.transform);
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0);
            rt.anchorMax = new Vector2(0.5f, 0);
            rt.pivot = new Vector2(0.5f, 0);
            rt.anchoredPosition = new Vector2(0, OS.safeAreaBottom + 86);
            rt.sizeDelta = new Vector2(300, 36);

            // Background pill
            var bg = go.AddComponent<Image>();
            bg.sprite = Visual.PixelUIGenerator.GetRoundedRect9Slice();
            bg.type = Image.Type.Sliced;
            bg.color = new Color(0.9f, 0.25f, 0.3f, 0.95f); // red-ish error tone
            bg.raycastTarget = false;

            // Label
            var labelGO = MurgeUI.CreateUIObject("Label", go.transform);
            MurgeUI.StretchFill(labelGO.GetComponent<RectTransform>());
            var tmp = labelGO.AddComponent<TextMeshProUGUI>();
            tmp.font = MurgeUI.PressStart2P;
            tmp.fontSize = 7;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.verticalAlignment = VerticalAlignmentOptions.Middle;
            tmp.characterSpacing = 1;
            tmp.raycastTarget = false;

            var cg = go.AddComponent<CanvasGroup>();
            cg.alpha = 0;
            cg.blocksRaycasts = false;
            cg.interactable = false;

            // Ensure toast renders on top of everything
            go.transform.SetAsLastSibling();

            instance = go.AddComponent<Toast>();
            instance.canvasGroup = cg;
            instance.label = tmp;
        }

        private void Display(string message)
        {
            label.text = message;
            StopAllCoroutines();
            StartCoroutine(ShowAndFade());
        }

        private IEnumerator ShowAndFade()
        {
            // Ensure on top
            transform.SetAsLastSibling();

            // Fade in
            float elapsed = 0f;
            while (elapsed < 0.15f)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(elapsed / 0.15f);
                yield return null;
            }
            canvasGroup.alpha = 1f;

            // Hold
            yield return new WaitForSeconds(ShowDuration);

            // Fade out
            elapsed = 0f;
            while (elapsed < FadeDuration)
            {
                elapsed += Time.deltaTime;
                canvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / FadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;
        }

        private void OnDestroy()
        {
            if (instance == this) instance = null;
        }
    }
}
