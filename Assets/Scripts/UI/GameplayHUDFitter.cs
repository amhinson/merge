using UnityEngine;
using MergeGame.Data;

namespace MergeGame.UI
{
    /// <summary>
    /// Adjusts gameplay HUD elements at runtime based on actual device safe area.
    /// The gameplay screen is built at editor time with safeTop=0.
    /// This component finds the key HUD elements and offsets them on first enable.
    /// </summary>
    public class GameplayHUDFitter : MonoBehaviour
    {
        private bool fitted;

        private void OnEnable()
        {
            if (fitted) return;
            fitted = true;

            float safeTop = OS.safeAreaTop;
            if (safeTop <= 0f) return; // no notch, nothing to adjust

            // Find and offset each HUD element by the safe area inset
            OffsetChild("MenuButton", safeTop);
            OffsetChild("ScoreLabel", safeTop);
            OffsetChild("Score", safeTop);
            OffsetChild("ComboUI", safeTop);
            OffsetChild("ShakeArea", safeTop);
            OffsetChild("NextBallCard", safeTop);
        }

        private void OffsetChild(string childName, float offset)
        {
            var child = transform.Find(childName);
            if (child == null) return;
            var rt = child.GetComponent<RectTransform>();
            if (rt == null) return;
            rt.anchoredPosition += new Vector2(0, -offset);
        }
    }
}
