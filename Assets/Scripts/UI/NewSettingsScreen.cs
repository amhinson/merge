using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MergeGame.Core;
using MergeGame.Data;
using MergeGame.Backend;

namespace MergeGame.UI
{
    /// <summary>
    /// Settings screen per spec: username with @prefix + char count, haptic toggle
    /// with subtitle, coming soon card, save button.
    /// </summary>
    public class NewSettingsScreen : MonoBehaviour
    {
        // Built UI
        private TMP_InputField nameInputField;
        private TextMeshProUGUI charCountLabel;
        private Toggle hapticToggle;
        private bool isBuilt;

        private void OnEnable()
        {
            if (!isBuilt) { BuildUI(); isBuilt = true; }
            Refresh();
        }

        public void Refresh()
        {
            if (nameInputField != null && PlayerIdentity.Instance != null)
                nameInputField.text = PlayerIdentity.Instance.DisplayName;
            UpdateCharCount();

            if (hapticToggle != null && HapticManager.Instance != null)
                hapticToggle.isOn = HapticManager.Instance.IsEnabled;
        }

        private void BuildUI()
        {
            var bg = gameObject.GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            bg.color = OC.bg;

            OvertoneUI.CreateTopGradient(transform);

            // Main content
            var content = OvertoneUI.CreateUIObject("Content", transform);
            OvertoneUI.StretchFill(content.GetComponent<RectTransform>());
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 0;
            vlg.padding = new RectOffset(0, 0, 0, 0);

            BuildHeaderRow(content.transform);
            BuildUsernameSection(content.transform);
            BuildHapticSection(content.transform);
            BuildComingSoonCard(content.transform);

            // Flexible spacer
            var flex = OvertoneUI.CreateUIObject("Flex", content.transform);
            flex.AddComponent<LayoutElement>().flexibleHeight = 1;

            // Save button
            BuildSaveButton(content.transform);
        }

        private void BuildHeaderRow(Transform parent)
        {
            var row = OvertoneUI.CreateUIObject("HeaderRow", parent);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 14;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.padding = new RectOffset(24, 24, (int)OS.safeAreaTop, 24);
            row.AddComponent<LayoutElement>().preferredHeight = OS.safeAreaTop + 60;

            var (backGO, backBtn) = OvertoneUI.CreateBackButton(row.transform);
            backBtn.onClick.AddListener(OnBackClicked);

            var title = OvertoneUI.CreateLabel(row.transform, "SETTINGS",
                OvertoneUI.PressStart2P, OFont.heading, OC.white, "PageTitle");
            title.characterSpacing = 2;
        }

        private void BuildUsernameSection(Transform parent)
        {
            var section = OvertoneUI.CreateUIObject("UsernameSection", parent);
            var vlg = section.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 8;
            vlg.padding = new RectOffset(24, 24, 0, 24);

            // Section label
            var sectionLabel = OvertoneUI.CreateLabel(section.transform, "USERNAME",
                OvertoneUI.PressStart2P, OFont.labelXs, OC.dim, "SectionLabel");
            sectionLabel.characterSpacing = 1;
            sectionLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 12;

            // Username field card
            var fieldCard = OvertoneUI.CreateCard(section.transform, "UsernameField");
            var fieldHLG = fieldCard.AddComponent<HorizontalLayoutGroup>();
            fieldHLG.spacing = 0;
            fieldHLG.childAlignment = TextAnchor.MiddleLeft;
            fieldHLG.childControlWidth = false;
            fieldHLG.childControlHeight = true;
            fieldHLG.childForceExpandWidth = false;
            fieldHLG.padding = new RectOffset(14, 14, 0, 0);
            fieldCard.GetComponent<LayoutElement>().preferredHeight = 52;

            // @ prefix
            var atSign = OvertoneUI.CreateLabel(fieldCard.transform, "@",
                OvertoneUI.DMMono, OFont.bodyLg, OC.cyan, "AtSign");

            // Input field
            var inputGO = OvertoneUI.CreateUIObject("InputField", fieldCard.transform);
            inputGO.AddComponent<LayoutElement>().flexibleWidth = 1;

            // TMP_InputField needs a Text Area child
            var textArea = OvertoneUI.CreateUIObject("Text Area", inputGO.transform);
            OvertoneUI.StretchFill(textArea.GetComponent<RectTransform>());
            textArea.AddComponent<RectMask2D>();

            // Placeholder
            var placeholderGO = OvertoneUI.CreateUIObject("Placeholder", textArea.transform);
            var placeholderTMP = placeholderGO.AddComponent<TextMeshProUGUI>();
            placeholderTMP.text = "enter name...";
            placeholderTMP.font = OvertoneUI.DMMono;
            placeholderTMP.fontSize = OFont.bodyLg;
            placeholderTMP.color = OC.dim;
            placeholderTMP.fontStyle = FontStyles.Italic;
            OvertoneUI.StretchFill(placeholderGO.GetComponent<RectTransform>());

            // Text component
            var textGO = OvertoneUI.CreateUIObject("Text", textArea.transform);
            var textTMP = textGO.AddComponent<TextMeshProUGUI>();
            textTMP.font = OvertoneUI.DMMono;
            textTMP.fontSize = OFont.bodyLg;
            textTMP.color = OC.white;
            OvertoneUI.StretchFill(textGO.GetComponent<RectTransform>());

            nameInputField = inputGO.AddComponent<TMP_InputField>();
            nameInputField.textViewport = textArea.GetComponent<RectTransform>();
            nameInputField.textComponent = textTMP;
            nameInputField.placeholder = placeholderTMP;
            nameInputField.characterLimit = 16;
            nameInputField.contentType = TMP_InputField.ContentType.Alphanumeric;
            nameInputField.caretColor = OC.cyan;
            nameInputField.selectionColor = OC.A(OC.cyan, 0.3f);
            nameInputField.onValueChanged.AddListener((_) => UpdateCharCount());

            // Char count
            charCountLabel = OvertoneUI.CreateLabel(fieldCard.transform, "0/16",
                OvertoneUI.PressStart2P, OFont.labelXs, OC.dim, "CharCount");
        }

        private void BuildHapticSection(Transform parent)
        {
            var section = OvertoneUI.CreateUIObject("HapticSection", parent);
            var vlg = section.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 8;
            vlg.padding = new RectOffset(24, 24, 0, 24);

            // Section label
            var sectionLabel = OvertoneUI.CreateLabel(section.transform, "CONTROLS",
                OvertoneUI.PressStart2P, OFont.labelXs, OC.dim, "SectionLabel");
            sectionLabel.characterSpacing = 1;
            sectionLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 12;

            // Haptic row card
            var card = OvertoneUI.CreateCard(section.transform, "HapticRow");
            var cardHLG = card.AddComponent<HorizontalLayoutGroup>();
            cardHLG.spacing = 0;
            cardHLG.childAlignment = TextAnchor.MiddleLeft;
            cardHLG.childControlWidth = false;
            cardHLG.childControlHeight = true;
            cardHLG.childForceExpandWidth = false;
            cardHLG.padding = new RectOffset(14, 14, 10, 10);
            card.GetComponent<LayoutElement>().preferredHeight = 60;

            // Text block
            var textBlock = OvertoneUI.CreateUIObject("TextBlock", card.transform);
            var textVLG = textBlock.AddComponent<VerticalLayoutGroup>();
            textVLG.spacing = 3;
            textVLG.childControlWidth = true;
            textVLG.childControlHeight = false;
            textVLG.childForceExpandWidth = true;
            textBlock.AddComponent<LayoutElement>().flexibleWidth = 1;

            OvertoneUI.CreateLabel(textBlock.transform, "Haptic feedback",
                OvertoneUI.DMMono, OFont.bodyLg, OC.white, "HapticTitle");
            OvertoneUI.CreateLabel(textBlock.transform, "Vibrate on merge",
                OvertoneUI.DMMono, OFont.bodyXs, OC.muted, "HapticSub");

            // Toggle
            bool hapticOn = HapticManager.Instance != null && HapticManager.Instance.IsEnabled;
            var (toggleGO, toggle) = OvertoneUI.CreateToggle(card.transform, hapticOn, "HapticToggle");
            hapticToggle = toggle;
            hapticToggle.onValueChanged.AddListener(OnHapticToggled);
        }

        private void BuildComingSoonCard(Transform parent)
        {
            var section = OvertoneUI.CreateUIObject("ComingSoonSection", parent);
            var vlg = section.AddComponent<VerticalLayoutGroup>();
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.padding = new RectOffset(24, 24, 0, 0);

            var card = OvertoneUI.CreateUIObject("ComingSoonCard", section.transform);
            var img = card.AddComponent<Image>();
            img.sprite = Visual.PixelUIGenerator.GetRoundedRect9Slice();
            img.type = Image.Type.Sliced;
            img.color = Color.clear; // transparent background, dashed border implied

            // Border (use solid for now — dashed requires custom shader)
            var outline = OvertoneUI.CreateUIObject("Outline", card.transform);
            var outlineImg = outline.AddComponent<Image>();
            outlineImg.sprite = Visual.PixelUIGenerator.GetRoundedRect9Slice();
            outlineImg.type = Image.Type.Sliced;
            outlineImg.color = OC.border;
            outlineImg.raycastTarget = false;
            OvertoneUI.StretchFill(outline.GetComponent<RectTransform>());

            var cardVLG = card.AddComponent<VerticalLayoutGroup>();
            cardVLG.padding = new RectOffset(14, 14, 14, 14);
            cardVLG.childAlignment = TextAnchor.MiddleCenter;
            cardVLG.childControlWidth = true;
            cardVLG.childControlHeight = false;
            cardVLG.childForceExpandWidth = true;

            var text = OvertoneUI.CreateLabel(card.transform, "MORE SETTINGS\nCOMING SOON",
                OvertoneUI.PressStart2P, OFont.labelXs, OC.dim, "ComingSoonText");
            text.characterSpacing = 1;
            text.alignment = TextAlignmentOptions.Center;
            text.lineSpacing = 12;
            text.gameObject.AddComponent<LayoutElement>().preferredHeight = 40;
        }

        private void BuildSaveButton(Transform parent)
        {
            var wrapper = OvertoneUI.CreateUIObject("SaveWrapper", parent);
            var vlg = wrapper.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 0, 44);
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;

            var (saveGO, saveLabel) = OvertoneUI.CreatePrimaryButton(wrapper.transform, "SAVE", 52, "SaveButton");
            saveGO.GetComponent<Button>().onClick.AddListener(OnSaveClicked);
        }

        // ───── Handlers ─────

        private void UpdateCharCount()
        {
            if (charCountLabel != null && nameInputField != null)
                charCountLabel.text = $"{nameInputField.text.Length}/16";
        }

        private void OnHapticToggled(bool enabled)
        {
            if (HapticManager.Instance != null)
                HapticManager.Instance.SetEnabled(enabled);
        }

        private void OnSaveClicked()
        {
            if (nameInputField == null || PlayerIdentity.Instance == null) return;

            string newName = nameInputField.text;
            bool success = PlayerIdentity.Instance.TrySetDisplayName(newName);

            if (success && LeaderboardService.Instance != null)
                LeaderboardService.Instance.UpdateDisplayName(newName);

            // Navigate back
            OnBackClicked();
        }

        private void OnBackClicked()
        {
            if (ScreenManager.Instance != null)
            {
                var target = GameSession.HasPlayedToday ? Screen.HomePlayed : Screen.HomeFresh;
                ScreenManager.Instance.NavigateTo(target);
            }
        }
    }
}
