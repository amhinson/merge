using UnityEngine;
using MergeGame.Core;

namespace MergeGame.UI
{
    /// <summary>
    /// Detects taps on the hamburger menu button and opens the Paused overlay.
    /// Uses manual tap detection (same pattern as the old SimpleExitConfirm)
    /// because Button.onClick doesn't fire reliably due to DropController input handling.
    /// </summary>
    public class PauseMenuButton : MonoBehaviour
    {
        private RectTransform menuButtonRect;

        private void Start()
        {
            FindMenuButton();
        }

        private void FindMenuButton()
        {
            var t = transform.Find("MenuButton");
            if (t != null) menuButtonRect = t.GetComponent<RectTransform>();
        }

        private void Update()
        {
            if (menuButtonRect == null) return;
            if (ScreenManager.Instance == null) return;
            if (ScreenManager.Instance.CurrentScreen != Screen.Game) return;
            if (GameManager.Instance == null || GameManager.Instance.CurrentState != GameState.Playing) return;

            // Detect tap/click on menu button
            if (Input.GetMouseButtonDown(0))
            {
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                        menuButtonRect, Input.mousePosition, null, out Vector2 localPoint))
                {
                    if (menuButtonRect.rect.Contains(localPoint))
                    {
                        ScreenManager.Instance.NavigateTo(Screen.Paused);
                    }
                }
            }
        }
    }
}
