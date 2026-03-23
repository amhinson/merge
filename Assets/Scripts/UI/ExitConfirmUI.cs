using UnityEngine;
using UnityEngine.UI;
using MergeGame.Core;

namespace MergeGame.UI
{
    /// <summary>
    /// Exit button with confirmation dialog during gameplay.
    /// </summary>
    public class ExitConfirmUI : MonoBehaviour
    {
        [SerializeField] private Button exitButton;
        [SerializeField] private GameObject confirmPanel;
        [SerializeField] private Button yesButton;
        [SerializeField] private Button noButton;

        private bool wired;

        private void OnEnable()
        {
            WireButtons();
        }

        private void Start()
        {
            WireButtons();
        }

        private void WireButtons()
        {
            if (wired) return;
            if (exitButton == null) return; // not wired yet
            wired = true;

            exitButton.onClick.AddListener(ShowConfirm);
            if (yesButton != null)
                yesButton.onClick.AddListener(OnConfirmQuit);
            if (noButton != null)
                noButton.onClick.AddListener(HideConfirm);
        }

        private void ShowConfirm()
        {
            if (confirmPanel != null)
                confirmPanel.SetActive(true);
        }

        private void HideConfirm()
        {
            if (confirmPanel != null)
                confirmPanel.SetActive(false);
        }

        private void OnConfirmQuit()
        {
            HideConfirm();
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Menu);
        }
    }
}
