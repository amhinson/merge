using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MergeGame.Core;
using MergeGame.Backend;

namespace MergeGame.UI
{
    /// <summary>
    /// Display name editing screen. Self-contained, repositionable component.
    /// </summary>
    public class NameEditUI : MonoBehaviour
    {
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private Button saveButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TextMeshProUGUI errorText;
        [SerializeField] private TextMeshProUGUI currentNameDisplay;

        public event System.Action OnNameSaved;
        public event System.Action OnCancelled;

        private void Start()
        {
            if (saveButton != null)
                saveButton.onClick.AddListener(OnSave);
            if (cancelButton != null)
                cancelButton.onClick.AddListener(OnCancel);
        }

        public void Show()
        {
            gameObject.SetActive(true);

            if (PlayerIdentity.Instance != null && nameInput != null)
            {
                nameInput.text = PlayerIdentity.Instance.DisplayName;
            }

            if (errorText != null) errorText.text = "";
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        public void UpdateCurrentNameDisplay()
        {
            if (currentNameDisplay != null && PlayerIdentity.Instance != null)
            {
                currentNameDisplay.text = PlayerIdentity.Instance.DisplayName;
            }
        }

        private void OnSave()
        {
            if (nameInput == null || PlayerIdentity.Instance == null) return;

            string newName = nameInput.text;
            bool success = PlayerIdentity.Instance.TrySetDisplayName(newName);

            if (success)
            {
                if (errorText != null) errorText.text = "";

                // Sync to backend
                if (LeaderboardService.Instance != null)
                {
                    LeaderboardService.Instance.UpdateDisplayName(newName);
                }

                OnNameSaved?.Invoke();
                Hide();
            }
            else
            {
                if (errorText != null)
                    errorText.text = "Name must be 3-16 characters, alphanumeric only";
            }
        }

        private void OnCancel()
        {
            OnCancelled?.Invoke();
            Hide();
        }
    }
}
