using UnityEngine;

namespace MergeGame.UI
{
    /// <summary>
    /// Adjusts a RectTransform to respect the device safe area (notch/Dynamic Island).
    /// Attach to a panel that should sit inside the safe area.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SafeAreaHandler : MonoBehaviour
    {
        private RectTransform rectTransform;
        private Rect lastSafeArea;

        private void Awake()
        {
            rectTransform = GetComponent<RectTransform>();
            ApplySafeArea();
        }

        private void Update()
        {
            if (UnityEngine.Screen.safeArea != lastSafeArea)
            {
                ApplySafeArea();
            }
        }

        private void ApplySafeArea()
        {
            Rect safeArea = UnityEngine.Screen.safeArea;
            lastSafeArea = safeArea;

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= UnityEngine.Screen.width;
            anchorMin.y /= UnityEngine.Screen.height;
            anchorMax.x /= UnityEngine.Screen.width;
            anchorMax.y /= UnityEngine.Screen.height;

            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }
    }
}
