using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MergeGame.Core;
using MergeGame.Data;
using MergeGame.Backend;

namespace MergeGame.UI
{
    public class NewSettingsScreen : MonoBehaviour
    {
        private TMP_InputField nameInput;
        private TextMeshProUGUI charCountLabel;
        private Image toggleTrack;
        private RectTransform toggleThumb;
        private bool hapticOn;

        private Image sfxToggleTrack;
        private RectTransform sfxToggleThumb;
        private bool sfxOn;
        private bool isBuilt;

        private void OnEnable()
        {
            if (!isBuilt) { BuildUI(); isBuilt = true; }
            Refresh();
        }

        public void Refresh()
        {
            if (nameInput != null && PlayerIdentity.Instance != null)
                nameInput.text = PlayerIdentity.Instance.DisplayName;
            UpdateCharCount();
            hapticOn = HapticManager.Instance != null && HapticManager.Instance.IsEnabled;
            sfxOn = MergeGame.Audio.AudioManager.Instance != null && MergeGame.Audio.AudioManager.Instance.IsSfxEnabled;
            UpdateToggleVisual(false);
        }

        private void BuildUI()
        {
            var bg = gameObject.GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            bg.color = OC.bg;

            // Header (top, fixed)
            BuildHeader(transform);

            // Content area (between header and save button)
            var content = MurgeUI.CreateUIObject("Content", transform);
            var cRT = content.GetComponent<RectTransform>();
            cRT.anchorMin = Vector2.zero;
            cRT.anchorMax = Vector2.one;
            cRT.offsetMin = new Vector2(24, 80 + (int)OS.safeAreaBottom); // above save button area
            cRT.offsetMax = new Vector2(-24, -(OS.safeAreaTop + 56)); // below header

            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 0;

            AddSpacer(content.transform, 8);
            BuildUsernameSection(content.transform);
            AddSpacer(content.transform, 20);
            BuildControlsSection(content.transform);
            AddSpacer(content.transform, 20);
            BuildGameCenterRow(content.transform);
            AddSpacer(content.transform, 20);
            BuildComingSoon(content.transform);

            // Save button (pinned to bottom)
            BuildSaveButton(transform);
        }

        private void BuildHeader(Transform parent)
        {
            var header = MurgeUI.CreateUIObject("Header", parent);
            var hRT = header.GetComponent<RectTransform>();
            hRT.anchorMin = new Vector2(0, 1);
            hRT.anchorMax = new Vector2(1, 1);
            hRT.pivot = new Vector2(0.5f, 1);
            hRT.anchoredPosition = new Vector2(0, -(OS.safeAreaTop + 8));
            hRT.sizeDelta = new Vector2(0, 40);

            // Back button
            var backGO = MurgeUI.CreateUIObject("BackBtn", header.transform);
            var backRT = backGO.GetComponent<RectTransform>();
            backRT.anchorMin = new Vector2(0, 0); backRT.anchorMax = new Vector2(0, 1);
            backRT.pivot = new Vector2(0, 0.5f);
            backRT.anchoredPosition = new Vector2(24, 0);
            backRT.sizeDelta = new Vector2(42, 0);
            // Border
            var bdr = MurgeUI.CreateUIObject("Border", backGO.transform);
            MurgeUI.StretchFill(bdr.GetComponent<RectTransform>());
            var bdrImg = bdr.AddComponent<Image>();
            bdrImg.sprite = MurgeUI.SmoothRoundedRect;
            bdrImg.type = Image.Type.Sliced;
            bdrImg.color = OC.border;
            bdrImg.raycastTarget = false;
            // Fill
            var fill = MurgeUI.CreateUIObject("Fill", backGO.transform);
            var fRT = fill.GetComponent<RectTransform>();
            fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one;
            fRT.offsetMin = new Vector2(1.5f, 1.5f); fRT.offsetMax = new Vector2(-1.5f, -1.5f);
            fill.AddComponent<Image>().sprite = MurgeUI.SmoothRoundedRect;
            fill.GetComponent<Image>().type = Image.Type.Sliced;
            fill.GetComponent<Image>().color = OC.bg;
            fill.GetComponent<Image>().raycastTarget = false;
            // Hit area + button
            backGO.AddComponent<Image>().color = Color.clear;
            backGO.AddComponent<Button>().targetGraphic = bdrImg;
            backGO.GetComponent<Button>().onClick.AddListener(OnBackClicked);
            // Arrow
            var arrow = MurgeUI.CreateLabel(backGO.transform, "<",
                MurgeUI.DMMono, 14, new Color(1, 1, 1, 0.3f), "Arrow");
            arrow.alignment = TextAlignmentOptions.Center;
            MurgeUI.StretchFill(arrow.GetComponent<RectTransform>());

            // Title
            var title = MurgeUI.CreateLabel(header.transform, "SETTINGS",
                MurgeUI.PressStart2P, 11, OC.white, "Title");
            title.characterSpacing = 2;
            title.alignment = TextAlignmentOptions.Left;
            title.verticalAlignment = VerticalAlignmentOptions.Middle;
            var tRT = title.GetComponent<RectTransform>();
            tRT.anchorMin = new Vector2(0, 0); tRT.anchorMax = new Vector2(1, 1);
            tRT.offsetMin = new Vector2(80, 0); tRT.offsetMax = Vector2.zero;
        }

        private void BuildUsernameSection(Transform parent)
        {
            // Section label
            var label = MurgeUI.CreateLabel(parent, "USERNAME",
                MurgeUI.PressStart2P, 7, OC.dim, "UsernameLabel");
            label.characterSpacing = 1;
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = 14;

            AddSpacer(parent, 8);

            // Input row
            var row = MurgeUI.CreateUIObject("UsernameField", parent);
            var rowLE = row.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 42; rowLE.minHeight = 42;

            // Border
            var rowBdr = MurgeUI.CreateUIObject("Border", row.transform);
            MurgeUI.StretchFill(rowBdr.GetComponent<RectTransform>());
            rowBdr.AddComponent<Image>().sprite = MurgeUI.SmoothRoundedRect;
            rowBdr.GetComponent<Image>().type = Image.Type.Sliced;
            rowBdr.GetComponent<Image>().color = OC.border;
            rowBdr.GetComponent<Image>().raycastTarget = false;
            // Fill
            var rowFill = MurgeUI.CreateUIObject("Fill", row.transform);
            var rfRT = rowFill.GetComponent<RectTransform>();
            rfRT.anchorMin = Vector2.zero; rfRT.anchorMax = Vector2.one;
            rfRT.offsetMin = new Vector2(1, 1); rfRT.offsetMax = new Vector2(-1, -1);
            rowFill.AddComponent<Image>().sprite = MurgeUI.SmoothRoundedRect;
            rowFill.GetComponent<Image>().type = Image.Type.Sliced;
            rowFill.GetComponent<Image>().color = OC.surface;
            rowFill.GetComponent<Image>().raycastTarget = false;

            // @ symbol — vertically centered in the row
            var atTMP = MurgeUI.CreateLabel(row.transform, "@",
                MurgeUI.DMMono, 14, OC.cyan, "AtSign");
            var atRT = atTMP.GetComponent<RectTransform>();
            atRT.anchorMin = new Vector2(0, 0); atRT.anchorMax = new Vector2(0, 1);
            atRT.pivot = new Vector2(0, 0.5f);
            atRT.anchoredPosition = new Vector2(14, 0);
            atRT.sizeDelta = new Vector2(16, 0);
            atTMP.alignment = TextAlignmentOptions.Center;
            atTMP.verticalAlignment = VerticalAlignmentOptions.Middle;
            atTMP.margin = Vector4.zero; // no extra margin

            // Input field area — vertically centered like @
            var inputArea = MurgeUI.CreateUIObject("InputArea", row.transform);
            var iaRT = inputArea.GetComponent<RectTransform>();
            iaRT.anchorMin = new Vector2(0, 0.5f); iaRT.anchorMax = new Vector2(1, 0.5f);
            iaRT.pivot = new Vector2(0, 0.5f);
            iaRT.anchoredPosition = new Vector2(34, 0);
            iaRT.sizeDelta = new Vector2(-84, 24); // -84 = -34 left - 50 right, height = font line

            // Text area for TMP_InputField
            var textArea = MurgeUI.CreateUIObject("TextArea", inputArea.transform);
            MurgeUI.StretchFill(textArea.GetComponent<RectTransform>());
            textArea.AddComponent<RectMask2D>();

            var textGO = MurgeUI.CreateUIObject("Text", textArea.transform);
            MurgeUI.StretchFill(textGO.GetComponent<RectTransform>());
            var textTMP = textGO.AddComponent<TextMeshProUGUI>();
            textTMP.font = MurgeUI.DMMono;
            textTMP.fontSize = 14;
            textTMP.color = OC.white;
            textTMP.verticalAlignment = VerticalAlignmentOptions.Middle;

            var placeholderGO = MurgeUI.CreateUIObject("Placeholder", textArea.transform);
            MurgeUI.StretchFill(placeholderGO.GetComponent<RectTransform>());
            var phTMP = placeholderGO.AddComponent<TextMeshProUGUI>();
            phTMP.text = "enter name...";
            phTMP.font = MurgeUI.DMMono;
            phTMP.fontSize = 14;
            phTMP.color = OC.dim;
            phTMP.fontStyle = FontStyles.Italic;
            phTMP.verticalAlignment = VerticalAlignmentOptions.Middle;

            nameInput = inputArea.AddComponent<TMP_InputField>();
            nameInput.textViewport = textArea.GetComponent<RectTransform>();
            nameInput.textComponent = textTMP;
            nameInput.placeholder = phTMP;
            nameInput.characterLimit = 16;
            nameInput.contentType = TMP_InputField.ContentType.Alphanumeric;
            nameInput.caretColor = OC.cyan;
            nameInput.selectionColor = OC.A(OC.cyan, 0.3f);
            nameInput.onValueChanged.AddListener((_) => UpdateCharCount());

            // Char counter
            charCountLabel = MurgeUI.CreateLabel(row.transform, "0/16",
                MurgeUI.PressStart2P, 7, OC.dim, "CharCount");
            var ccRT = charCountLabel.GetComponent<RectTransform>();
            ccRT.anchorMin = new Vector2(1, 0); ccRT.anchorMax = new Vector2(1, 1);
            ccRT.pivot = new Vector2(1, 0.5f);
            ccRT.anchoredPosition = new Vector2(-12, 0);
            ccRT.sizeDelta = new Vector2(36, 0);
            charCountLabel.alignment = TextAlignmentOptions.Right;
            charCountLabel.verticalAlignment = VerticalAlignmentOptions.Middle;
        }

        private void BuildControlsSection(Transform parent)
        {
            var label = MurgeUI.CreateLabel(parent, "CONTROLS",
                MurgeUI.PressStart2P, 7, OC.dim, "ControlsLabel");
            label.characterSpacing = 1;
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = 14;

            AddSpacer(parent, 8);

            // Haptic row
            var row = MurgeUI.CreateUIObject("HapticRow", parent);
            var rowLE = row.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 56; rowLE.minHeight = 56;

            // Border + fill
            var rBdr = MurgeUI.CreateUIObject("Border", row.transform);
            MurgeUI.StretchFill(rBdr.GetComponent<RectTransform>());
            rBdr.AddComponent<Image>().sprite = MurgeUI.SmoothRoundedRect;
            rBdr.GetComponent<Image>().type = Image.Type.Sliced;
            rBdr.GetComponent<Image>().color = OC.border;
            rBdr.GetComponent<Image>().raycastTarget = false;
            var rFill = MurgeUI.CreateUIObject("Fill", row.transform);
            var rfRT = rFill.GetComponent<RectTransform>();
            rfRT.anchorMin = Vector2.zero; rfRT.anchorMax = Vector2.one;
            rfRT.offsetMin = new Vector2(1, 1); rfRT.offsetMax = new Vector2(-1, -1);
            rFill.AddComponent<Image>().sprite = MurgeUI.SmoothRoundedRect;
            rFill.GetComponent<Image>().type = Image.Type.Sliced;
            rFill.GetComponent<Image>().color = OC.surface;
            rFill.GetComponent<Image>().raycastTarget = false;

            // Title + subtitle (left)
            var titleTMP = MurgeUI.CreateLabel(row.transform, "Haptic feedback",
                MurgeUI.DMMono, 14, OC.white, "HapticTitle");
            var ttRT = titleTMP.GetComponent<RectTransform>();
            ttRT.anchorMin = new Vector2(0, 0.5f); ttRT.anchorMax = new Vector2(0.7f, 1);
            ttRT.offsetMin = new Vector2(14, 0); ttRT.offsetMax = new Vector2(0, -8);
            titleTMP.alignment = TextAlignmentOptions.Left;
            titleTMP.verticalAlignment = VerticalAlignmentOptions.Bottom;

            var subTMP = MurgeUI.CreateLabel(row.transform, "Vibrate on merge",
                MurgeUI.DMMono, 11, OC.muted, "HapticSub");
            var stRT = subTMP.GetComponent<RectTransform>();
            stRT.anchorMin = new Vector2(0, 0); stRT.anchorMax = new Vector2(0.7f, 0.5f);
            stRT.offsetMin = new Vector2(14, 8); stRT.offsetMax = Vector2.zero;
            subTMP.alignment = TextAlignmentOptions.Left;
            subTMP.verticalAlignment = VerticalAlignmentOptions.Top;

            // Toggle (right)
            BuildToggle(row.transform);

            AddSpacer(parent, 8);

            // SFX row
            var sfxRow = MurgeUI.CreateUIObject("SfxRow", parent);
            var sfxRowLE = sfxRow.AddComponent<LayoutElement>();
            sfxRowLE.preferredHeight = 56; sfxRowLE.minHeight = 56;

            // Border + fill
            var sfxBdr = MurgeUI.CreateUIObject("Border", sfxRow.transform);
            MurgeUI.StretchFill(sfxBdr.GetComponent<RectTransform>());
            sfxBdr.AddComponent<Image>().sprite = MurgeUI.SmoothRoundedRect;
            sfxBdr.GetComponent<Image>().type = Image.Type.Sliced;
            sfxBdr.GetComponent<Image>().color = OC.border;
            sfxBdr.GetComponent<Image>().raycastTarget = false;
            var sfxFill = MurgeUI.CreateUIObject("Fill", sfxRow.transform);
            var sfRT = sfxFill.GetComponent<RectTransform>();
            sfRT.anchorMin = Vector2.zero; sfRT.anchorMax = Vector2.one;
            sfRT.offsetMin = new Vector2(1, 1); sfRT.offsetMax = new Vector2(-1, -1);
            sfxFill.AddComponent<Image>().sprite = MurgeUI.SmoothRoundedRect;
            sfxFill.GetComponent<Image>().type = Image.Type.Sliced;
            sfxFill.GetComponent<Image>().color = OC.surface;
            sfxFill.GetComponent<Image>().raycastTarget = false;

            var sfxTitle = MurgeUI.CreateLabel(sfxRow.transform, "Sound effects",
                MurgeUI.DMMono, 14, OC.white, "SfxTitle");
            var sfxTtRT = sfxTitle.GetComponent<RectTransform>();
            sfxTtRT.anchorMin = new Vector2(0, 0.5f); sfxTtRT.anchorMax = new Vector2(0.7f, 1);
            sfxTtRT.offsetMin = new Vector2(14, 0); sfxTtRT.offsetMax = new Vector2(0, -8);
            sfxTitle.alignment = TextAlignmentOptions.Left;
            sfxTitle.verticalAlignment = VerticalAlignmentOptions.Bottom;

            var sfxSub = MurgeUI.CreateLabel(sfxRow.transform, "Merge sounds",
                MurgeUI.DMMono, 11, OC.muted, "SfxSub");
            var sfxStRT = sfxSub.GetComponent<RectTransform>();
            sfxStRT.anchorMin = new Vector2(0, 0); sfxStRT.anchorMax = new Vector2(0.7f, 0.5f);
            sfxStRT.offsetMin = new Vector2(14, 8); sfxStRT.offsetMax = Vector2.zero;
            sfxSub.alignment = TextAlignmentOptions.Left;
            sfxSub.verticalAlignment = VerticalAlignmentOptions.Top;

            BuildSfxToggle(sfxRow.transform);
        }

        private void BuildToggle(Transform parent)
        {
            var toggleGO = MurgeUI.CreateUIObject("Toggle", parent);
            var tgRT = toggleGO.GetComponent<RectTransform>();
            tgRT.anchorMin = new Vector2(1, 0.5f); tgRT.anchorMax = new Vector2(1, 0.5f);
            tgRT.pivot = new Vector2(1, 0.5f);
            tgRT.anchoredPosition = new Vector2(-14, 0);
            tgRT.sizeDelta = new Vector2(48, 26);

            // Track — smooth rounded rect
            toggleTrack = toggleGO.AddComponent<Image>();
            toggleTrack.sprite = GetSmoothPill();
            toggleTrack.type = Image.Type.Sliced;
            toggleTrack.color = OC.cyan;

            // Thumb — smooth circle
            var thumbGO = MurgeUI.CreateUIObject("Thumb", toggleGO.transform);
            toggleThumb = thumbGO.GetComponent<RectTransform>();
            toggleThumb.anchorMin = new Vector2(0, 0.5f);
            toggleThumb.anchorMax = new Vector2(0, 0.5f);
            toggleThumb.pivot = new Vector2(0, 0.5f);
            toggleThumb.sizeDelta = new Vector2(20, 20);
            toggleThumb.anchoredPosition = new Vector2(25, 0);

            var thumbImg = thumbGO.AddComponent<Image>();
            thumbImg.sprite = GetSmoothCircle();
            thumbImg.type = Image.Type.Simple;
            thumbImg.color = Color.white;

            // Button for tap
            var btn = toggleGO.AddComponent<Button>();
            btn.targetGraphic = toggleTrack;
            btn.onClick.AddListener(OnToggleHaptic);
        }

        private void BuildSfxToggle(Transform parent)
        {
            var toggleGO = MurgeUI.CreateUIObject("SfxToggle", parent);
            var tgRT = toggleGO.GetComponent<RectTransform>();
            tgRT.anchorMin = new Vector2(1, 0.5f); tgRT.anchorMax = new Vector2(1, 0.5f);
            tgRT.pivot = new Vector2(1, 0.5f);
            tgRT.anchoredPosition = new Vector2(-14, 0);
            tgRT.sizeDelta = new Vector2(48, 26);

            sfxToggleTrack = toggleGO.AddComponent<Image>();
            sfxToggleTrack.sprite = GetSmoothPill();
            sfxToggleTrack.type = Image.Type.Sliced;
            sfxToggleTrack.color = sfxOn ? OC.cyan : OC.border;

            var thumbGO = MurgeUI.CreateUIObject("Thumb", toggleGO.transform);
            sfxToggleThumb = thumbGO.GetComponent<RectTransform>();
            sfxToggleThumb.anchorMin = new Vector2(0, 0.5f);
            sfxToggleThumb.anchorMax = new Vector2(0, 0.5f);
            sfxToggleThumb.pivot = new Vector2(0, 0.5f);
            sfxToggleThumb.sizeDelta = new Vector2(20, 20);
            sfxToggleThumb.anchoredPosition = new Vector2(sfxOn ? 25 : 3, 0);

            var thumbImg = thumbGO.AddComponent<Image>();
            thumbImg.sprite = GetSmoothCircle();
            thumbImg.type = Image.Type.Simple;
            thumbImg.color = Color.white;

            var btn = toggleGO.AddComponent<Button>();
            btn.targetGraphic = sfxToggleTrack;
            btn.onClick.AddListener(OnToggleSfx);
        }

        private void BuildGameCenterRow(Transform parent)
        {
#if !UNITY_IOS
            return;
#else
            var row = MurgeUI.CreateUIObject("GameCenterRow", parent);
            var rowLE = row.AddComponent<LayoutElement>();
            rowLE.preferredHeight = 56; rowLE.minHeight = 56;

            // Border + fill
            var rBdr = MurgeUI.CreateUIObject("Border", row.transform);
            MurgeUI.StretchFill(rBdr.GetComponent<RectTransform>());
            rBdr.AddComponent<Image>().sprite = MurgeUI.SmoothRoundedRect;
            rBdr.GetComponent<Image>().type = Image.Type.Sliced;
            rBdr.GetComponent<Image>().color = OC.border;
            rBdr.GetComponent<Image>().raycastTarget = false;
            var rFill = MurgeUI.CreateUIObject("Fill", row.transform);
            var rfRT = rFill.GetComponent<RectTransform>();
            rfRT.anchorMin = Vector2.zero; rfRT.anchorMax = Vector2.one;
            rfRT.offsetMin = new Vector2(1, 1); rfRT.offsetMax = new Vector2(-1, -1);
            rFill.AddComponent<Image>().sprite = MurgeUI.SmoothRoundedRect;
            rFill.GetComponent<Image>().type = Image.Type.Sliced;
            rFill.GetComponent<Image>().color = OC.surface;
            rFill.GetComponent<Image>().raycastTarget = false;

            // Title + subtitle
            var titleTMP = MurgeUI.CreateLabel(row.transform, "Game Center",
                MurgeUI.DMMono, 14, OC.white, "GCTitle");
            var ttRT = titleTMP.GetComponent<RectTransform>();
            ttRT.anchorMin = new Vector2(0, 0.5f); ttRT.anchorMax = new Vector2(0.8f, 1);
            ttRT.offsetMin = new Vector2(14, 0); ttRT.offsetMax = new Vector2(0, -8);
            titleTMP.alignment = TextAlignmentOptions.Left;
            titleTMP.verticalAlignment = VerticalAlignmentOptions.Bottom;

            var subTMP = MurgeUI.CreateLabel(row.transform, "Achievements",
                MurgeUI.DMMono, 11, OC.muted, "GCSub");
            var stRT = subTMP.GetComponent<RectTransform>();
            stRT.anchorMin = new Vector2(0, 0); stRT.anchorMax = new Vector2(0.8f, 0.5f);
            stRT.offsetMin = new Vector2(14, 8); stRT.offsetMax = Vector2.zero;
            subTMP.alignment = TextAlignmentOptions.Left;
            subTMP.verticalAlignment = VerticalAlignmentOptions.Top;

            // Arrow (right side)
            var arrowTMP = MurgeUI.CreateLabel(row.transform, ">",
                MurgeUI.DMMono, 14, OC.muted, "Arrow");
            var arRT = arrowTMP.GetComponent<RectTransform>();
            arRT.anchorMin = new Vector2(1, 0); arRT.anchorMax = new Vector2(1, 1);
            arRT.pivot = new Vector2(1, 0.5f);
            arRT.anchoredPosition = new Vector2(-14, 0);
            arRT.sizeDelta = new Vector2(20, 0);
            arrowTMP.alignment = TextAlignmentOptions.Center;

            // Tap target
            var hitImg = row.AddComponent<Image>();
            hitImg.color = Color.clear;
            var btn = row.AddComponent<Button>();
            btn.targetGraphic = hitImg;
            btn.onClick.AddListener(() =>
            {
#if UNITY_IOS && !UNITY_EDITOR
                Social.ShowAchievementsUI();
#else
                Debug.Log("GameCenter: Not available on this platform");
#endif
            });
#endif
        }

        private void BuildComingSoon(Transform parent)
        {
            var card = MurgeUI.CreateUIObject("ComingSoon", parent);
            var cLE = card.AddComponent<LayoutElement>();
            cLE.preferredHeight = 44; cLE.minHeight = 44;

            // Transparent bg with subtle border (approximating dashed with low-opacity solid)
            var bdr = MurgeUI.CreateUIObject("Border", card.transform);
            MurgeUI.StretchFill(bdr.GetComponent<RectTransform>());
            var bdrImg = bdr.AddComponent<Image>();
            bdrImg.sprite = MurgeUI.SmoothRoundedRect;
            bdrImg.type = Image.Type.Sliced;
            bdrImg.color = OC.A(OC.border, 0.4f); // subtle, ~40% opacity
            bdrImg.raycastTarget = false;
            // Transparent fill inset
            var cFill = MurgeUI.CreateUIObject("Fill", card.transform);
            var cfRT = cFill.GetComponent<RectTransform>();
            cfRT.anchorMin = Vector2.zero; cfRT.anchorMax = Vector2.one;
            cfRT.offsetMin = new Vector2(1, 1); cfRT.offsetMax = new Vector2(-1, -1);
            var cfImg = cFill.AddComponent<Image>();
            cfImg.sprite = MurgeUI.SmoothRoundedRect;
            cfImg.type = Image.Type.Sliced;
            cfImg.color = OC.bg; // matches screen bg = transparent look
            cfImg.raycastTarget = false;

            var tmp = MurgeUI.CreateLabel(card.transform, "MORE SETTINGS\nCOMING SOON",
                MurgeUI.PressStart2P, 7, OC.dim, "Text");
            tmp.characterSpacing = 1;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.lineSpacing = 12;
            MurgeUI.StretchFill(tmp.GetComponent<RectTransform>());
        }

        private void BuildSaveButton(Transform parent)
        {
            var wrapper = MurgeUI.CreateUIObject("SaveWrapper", parent);
            var wRT = wrapper.GetComponent<RectTransform>();
            wRT.anchorMin = new Vector2(0, 0);
            wRT.anchorMax = new Vector2(1, 0);
            wRT.pivot = new Vector2(0.5f, 0);
            wRT.anchoredPosition = new Vector2(0, 30 + OS.safeAreaBottom);
            wRT.sizeDelta = new Vector2(-48, 44); // -48 = 24px padding each side

            var (saveGO, saveTMP) = MurgeUI.CreatePrimaryButton(wrapper.transform, "SAVE", 44, "SaveButton");
            MurgeUI.StretchFill(saveGO.GetComponent<RectTransform>());
            saveGO.GetComponent<Button>().onClick.AddListener(OnSaveClicked);
        }

        // ───── Handlers ─────

        private void UpdateCharCount()
        {
            if (charCountLabel != null && nameInput != null)
                charCountLabel.text = $"{nameInput.text.Length}/16";
        }

        private void OnToggleHaptic()
        {
            hapticOn = !hapticOn;
            if (HapticManager.Instance != null)
                HapticManager.Instance.SetEnabled(hapticOn);
            UpdateToggleVisual(true);
        }

        private void UpdateToggleVisual(bool animate)
        {
            if (toggleTrack == null || toggleThumb == null) return;

            Color targetColor = hapticOn ? OC.cyan : OC.border;
            Vector2 targetPos = new Vector2(hapticOn ? 25 : 3, 0);

            if (animate)
            {
                // Simple coroutine-based animation
                StartCoroutine(AnimateToggle(targetColor, targetPos));
            }
            else
            {
                toggleTrack.color = targetColor;
                toggleThumb.anchoredPosition = targetPos;
            }
        }

        private System.Collections.IEnumerator AnimateToggle(Color targetColor, Vector2 targetPos)
        {
            Color startColor = toggleTrack.color;
            Vector2 startPos = toggleThumb.anchoredPosition;
            float elapsed = 0f;
            float duration = 0.2f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - (1f - t) * (1f - t);
                toggleTrack.color = Color.Lerp(startColor, targetColor, eased);
                toggleThumb.anchoredPosition = Vector2.Lerp(startPos, targetPos, eased);
                yield return null;
            }

            toggleTrack.color = targetColor;
            toggleThumb.anchoredPosition = targetPos;
        }

        private void OnToggleSfx()
        {
            sfxOn = !sfxOn;
            if (MergeGame.Audio.AudioManager.Instance != null)
                MergeGame.Audio.AudioManager.Instance.SetSfxEnabled(sfxOn);
            UpdateSfxToggleVisual(true);
        }

        private void UpdateSfxToggleVisual(bool animate)
        {
            if (sfxToggleTrack == null || sfxToggleThumb == null) return;

            Color targetColor = sfxOn ? OC.cyan : OC.border;
            Vector2 targetPos = new Vector2(sfxOn ? 25 : 3, 0);

            if (animate)
                StartCoroutine(AnimateSfxToggle(targetColor, targetPos));
            else
            {
                sfxToggleTrack.color = targetColor;
                sfxToggleThumb.anchoredPosition = targetPos;
            }
        }

        private System.Collections.IEnumerator AnimateSfxToggle(Color targetColor, Vector2 targetPos)
        {
            Color startColor = sfxToggleTrack.color;
            Vector2 startPos = sfxToggleThumb.anchoredPosition;
            float elapsed = 0f;
            float duration = 0.2f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - (1f - t) * (1f - t);
                sfxToggleTrack.color = Color.Lerp(startColor, targetColor, eased);
                sfxToggleThumb.anchoredPosition = Vector2.Lerp(startPos, targetPos, eased);
                yield return null;
            }

            sfxToggleTrack.color = targetColor;
            sfxToggleThumb.anchoredPosition = targetPos;
        }

        private void OnSaveClicked()
        {
            if (nameInput == null || PlayerIdentity.Instance == null) return;

            string newName = nameInput.text.Trim();

            if (string.IsNullOrEmpty(newName) || newName.Length < 3)
            {
                Toast.Show("Name must be at least 3 characters");
                return;
            }

            bool success = PlayerIdentity.Instance.TrySetDisplayName(newName);

            if (!success)
            {
                Toast.Show("Invalid name — letters, numbers, and spaces only");
                return;
            }

            if (LeaderboardService.Instance != null)
                LeaderboardService.Instance.UpdateDisplayName(newName);

            if (Core.MurgeAnalytics.Instance != null)
                Core.MurgeAnalytics.Instance.TrackNameChanged();

            OnBackClicked();
        }

        private void OnBackClicked()
        {
            if (ScreenManager.Instance != null)
            {
                bool hasPlayed = GameSession.HasPlayedToday ||
                    (DailySeedManager.Instance != null && DailySeedManager.Instance.HasCompletedScoredAttempt());
                ScreenManager.Instance.NavigateTo(hasPlayed ? Screen.HomePlayed : Screen.HomeFresh);
            }
        }

        private void AddSpacer(Transform parent, float height)
        {
            var s = MurgeUI.CreateUIObject("Spacer", parent);
            var le = s.AddComponent<LayoutElement>();
            le.preferredHeight = height; le.minHeight = height;
        }

        // ───── Smooth sprites ─────

        private static Sprite _smoothPill;
        private static Sprite GetSmoothPill()
        {
            if (_smoothPill != null) return _smoothPill;
            // 48x26 pill with 13px radius, anti-aliased
            int w = 48, h = 26;
            float r = 13f;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
            for (int y = 0; y < h; y++)
            {
                for (int x = 0; x < w; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x - w / 2f + 0.5f) - (w / 2f - r));
                    float dy = Mathf.Max(0, Mathf.Abs(y - h / 2f + 0.5f) - (h / 2f - r));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= r) px[y * w + x] = Color.white;
                    else if (dist <= r + 1f) px[y * w + x] = new Color(1, 1, 1, r + 1f - dist);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            var border = new Vector4(r + 1, r + 1, r + 1, r + 1);
            _smoothPill = Sprite.Create(tex, new Rect(0, 0, w, h),
                new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            return _smoothPill;
        }

        private static Sprite _smoothCircle;
        private static Sprite GetSmoothCircle()
        {
            if (_smoothCircle != null) return _smoothCircle;
            // 40x40 circle, anti-aliased, rendered at 20pt
            int size = 40;
            float r = size / 2f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] px = new Color[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - r + 0.5f;
                    float dy = y - r + 0.5f;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= r - 1f) px[y * size + x] = Color.white;
                    else if (dist <= r) px[y * size + x] = new Color(1, 1, 1, r - dist);
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            _smoothCircle = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), size); // PPU = size for 1:1 display
            return _smoothCircle;
        }
    }
}
