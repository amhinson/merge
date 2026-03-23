using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using MergeGame.Core;

namespace MergeGame.UI
{
    /// <summary>
    /// Handles exit confirmation. Uses Update() to detect clicks on the back button area
    /// since Button.onClick doesn't fire (likely blocked by DropController input handling).
    /// </summary>
    public class SimpleExitConfirm : MonoBehaviour
    {
        private RectTransform backBtnRT;
        private GameObject confirmPanel;
        private Button yesButton;
        private Button noButton;
        private Canvas parentCanvas;
        private Camera canvasCamera;
        private bool wired;

        private void OnEnable()
        {
            if (!wired) WireUp();
        }

        private void WireUp()
        {
            wired = true;

            var backBtnTransform = transform.Find("BackButton");
            var confirmPanelTransform = transform.Find("ExitConfirm");

            if (backBtnTransform == null || confirmPanelTransform == null) return;

            backBtnRT = backBtnTransform.GetComponent<RectTransform>();
            confirmPanel = confirmPanelTransform.gameObject;

            parentCanvas = GetComponentInParent<Canvas>();
            if (parentCanvas != null && parentCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                canvasCamera = parentCanvas.worldCamera;

            // Wire modal buttons normally — they work fine since no DropController interference
            var yesBtnTransform = confirmPanelTransform.Find("ModalCard/ButtonRow/YesBtn");
            var noBtnTransform = confirmPanelTransform.Find("ModalCard/ButtonRow/NoBtn");

            if (yesBtnTransform != null) yesButton = yesBtnTransform.GetComponent<Button>();
            if (noBtnTransform != null) noButton = noBtnTransform.GetComponent<Button>();

            if (yesButton != null)
            {
                yesButton.onClick.AddListener(() =>
                {
                    confirmPanel.SetActive(false);
                    if (GameManager.Instance != null)
                        GameManager.Instance.SetState(GameState.Menu);
                });
            }

            if (noButton != null)
            {
                noButton.onClick.AddListener(() =>
                {
                    confirmPanel.SetActive(false);
                });
            }
        }

        private void Update()
        {
            if (backBtnRT == null || confirmPanel == null) return;
            if (confirmPanel.activeSelf) return; // modal is open, don't re-trigger

            // Detect tap/click on the back button area manually
            if (Input.GetMouseButtonDown(0))
            {
                Vector2 localPoint;
                if (RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    backBtnRT, Input.mousePosition, canvasCamera, out localPoint))
                {
                    if (backBtnRT.rect.Contains(localPoint))
                    {
                        confirmPanel.SetActive(true);
                    }
                }
            }
        }
    }
}
