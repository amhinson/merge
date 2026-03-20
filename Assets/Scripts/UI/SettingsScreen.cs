using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MergeGame.Core;
using MergeGame.Backend;

namespace MergeGame.UI
{
    /// <summary>
    /// Settings screen — display name edit and haptic toggle.
    /// </summary>
    public class SettingsScreen : MonoBehaviour
    {
        [Header("Name")]
        [SerializeField] private TMP_InputField nameInput;
        [SerializeField] private Button saveNameButton;
        [SerializeField] private TextMeshProUGUI nameErrorText;

        [Header("Haptics")]
        [SerializeField] private Toggle hapticToggle;

        [Header("Navigation")]
        [SerializeField] private Button backButton;

        private static readonly Color ToggleOnColor = new Color(0.05f, 0.58f, 0.53f); // AccentTeal
        private static readonly Color ToggleOffColor = new Color(0.25f, 0.25f, 0.30f);

        public Button BackButton => backButton;

        private void Start()
        {
            if (saveNameButton != null)
                saveNameButton.onClick.AddListener(OnSaveName);
            if (hapticToggle != null)
                hapticToggle.onValueChanged.AddListener(OnHapticToggled);
        }

        public void Refresh()
        {
            if (nameInput != null && PlayerIdentity.Instance != null)
                nameInput.text = PlayerIdentity.Instance.DisplayName;

            if (hapticToggle != null && HapticManager.Instance != null)
            {
                hapticToggle.isOn = HapticManager.Instance.IsEnabled;
                UpdateToggleVisual(hapticToggle.isOn);
            }

            if (nameErrorText != null)
                nameErrorText.text = "";
        }

        private void OnSaveName()
        {
            if (nameInput == null || PlayerIdentity.Instance == null) return;

            string newName = nameInput.text;
            bool success = PlayerIdentity.Instance.TrySetDisplayName(newName);

            if (success)
            {
                if (nameErrorText != null) nameErrorText.text = "";
                if (LeaderboardService.Instance != null)
                    LeaderboardService.Instance.UpdateDisplayName(newName);
            }
            else
            {
                if (nameErrorText != null)
                    nameErrorText.text = "3-16 chars, alphanumeric";
            }
        }

        private void OnHapticToggled(bool enabled)
        {
            if (HapticManager.Instance != null)
                HapticManager.Instance.SetEnabled(enabled);

            UpdateToggleVisual(enabled);
        }

        private void UpdateToggleVisual(bool isOn)
        {
            if (hapticToggle == null) return;

            // Update label
            var label = hapticToggle.GetComponentInChildren<TextMeshProUGUI>();
            if (label != null)
                label.text = isOn ? "ON" : "OFF";

            // Update color
            var img = hapticToggle.GetComponent<Image>();
            if (img != null)
                img.color = isOn ? ToggleOnColor : ToggleOffColor;
        }
    }
}
