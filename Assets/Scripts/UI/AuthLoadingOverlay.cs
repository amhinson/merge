using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MergeGame.Data;

namespace MergeGame.UI
{
    /// <summary>
    /// Full-screen loading overlay shown during auth operations.
    /// Call Show() / Hide() from anywhere.
    /// </summary>
    public class AuthLoadingOverlay : MonoBehaviour
    {
        public static AuthLoadingOverlay Instance { get; private set; }

        private GameObject overlayGO;
        private TextMeshProUGUI statusText;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance != null) return;
            var go = new GameObject("AuthLoadingOverlay");
            go.AddComponent<AuthLoadingOverlay>();
        }

        public void Show(string message = "")
        {
            if (overlayGO == null) BuildUI();
            if (statusText != null)
                statusText.text = string.IsNullOrEmpty(message) ? "" : message;
            overlayGO.SetActive(true);
        }

        public void Hide()
        {
            if (overlayGO != null) overlayGO.SetActive(false);
        }

        private void BuildUI()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            overlayGO = new GameObject("AuthLoading");
            overlayGO.transform.SetParent(canvas.transform, false);

            var rt = overlayGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            // Ensure on top of everything
            var overlayCanvas = overlayGO.AddComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 200;
            overlayGO.AddComponent<GraphicRaycaster>();

            // Dark scrim
            var scrim = overlayGO.AddComponent<Image>();
            scrim.color = new Color(0.031f, 0.031f, 0.055f, 0.95f);
            scrim.raycastTarget = true;

            // Dots / status text
            var textGO = MurgeUI.CreateUIObject("Status", overlayGO.transform);
            var textRT = textGO.GetComponent<RectTransform>();
            textRT.anchorMin = new Vector2(0.5f, 0.5f);
            textRT.anchorMax = new Vector2(0.5f, 0.5f);
            textRT.pivot = new Vector2(0.5f, 0.5f);
            textRT.sizeDelta = new Vector2(200, 30);
            statusText = textGO.AddComponent<TextMeshProUGUI>();
            statusText.text = "";
            statusText.font = MurgeUI.DMMono;
            statusText.fontSize = 14;
            statusText.color = OC.muted;
            statusText.alignment = TextAlignmentOptions.Center;
            statusText.raycastTarget = false;

            overlayGO.SetActive(false);
        }
    }
}
