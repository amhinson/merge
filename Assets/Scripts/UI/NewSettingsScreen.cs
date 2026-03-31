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

        private Button saveButton;
        private TextMeshProUGUI saveLabel;
        private Image saveBgImage;
        private string originalName;
        private bool isSaving;

        private void OnEnable()
        {
            if (!isBuilt) { BuildUI(); isBuilt = true; }
            Refresh();
        }

        public void Refresh()
        {
            if (nameInput != null && PlayerIdentity.Instance != null)
                nameInput.text = PlayerIdentity.Instance.DisplayName;
            originalName = PlayerIdentity.Instance != null ? PlayerIdentity.Instance.DisplayName : "";
            isSaving = false;
            UpdateCharCount();
            UpdateSaveButtonState();
            hapticOn = HapticManager.Instance != null && HapticManager.Instance.IsEnabled;
            sfxOn = MergeGame.Audio.AudioManager.Instance != null && MergeGame.Audio.AudioManager.Instance.IsSfxEnabled;
            UpdateToggleVisual(false);
            UpdateSfxToggleVisual(false);
            RefreshAccountUI();
        }

        private void BuildUI()
        {
            var bg = gameObject.GetComponent<Image>();
            if (bg == null) bg = gameObject.AddComponent<Image>();
            bg.color = OC.bg;

            // Header (top, fixed)
            BuildHeader(transform);

            // Scrollable content area
            var scrollGO = MurgeUI.CreateUIObject("Scroll", transform);
            var scrollRT = scrollGO.GetComponent<RectTransform>();
            scrollRT.anchorMin = Vector2.zero;
            scrollRT.anchorMax = Vector2.one;
            scrollRT.offsetMin = new Vector2(24, (int)OS.safeAreaBottom + 16);
            scrollRT.offsetMax = new Vector2(-24, -(OS.safeAreaTop + 56));
            var scrollRect = scrollGO.AddComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Elastic;
            scrollRect.scrollSensitivity = 20f;

            var viewportGO = MurgeUI.CreateUIObject("Viewport", scrollGO.transform);
            MurgeUI.StretchFill(viewportGO.GetComponent<RectTransform>());
            viewportGO.AddComponent<RectMask2D>();
            // Transparent image on viewport so ScrollRect receives all touches
            var vpImg = viewportGO.AddComponent<Image>();
            vpImg.color = Color.clear;
            vpImg.raycastTarget = true;
            scrollRect.viewport = viewportGO.GetComponent<RectTransform>();

            var content = MurgeUI.CreateUIObject("Content", viewportGO.transform);
            var cRT = content.GetComponent<RectTransform>();
            cRT.anchorMin = new Vector2(0, 1); cRT.anchorMax = new Vector2(1, 1);
            cRT.pivot = new Vector2(0.5f, 1);
            cRT.offsetMin = Vector2.zero; cRT.offsetMax = Vector2.zero;
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperLeft;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.spacing = 0;
            var csf = content.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scrollRect.content = cRT;

            AddSpacer(content.transform, 8);
            BuildUsernameSection(content.transform);
            AddSpacer(content.transform, 20);
            BuildControlsSection(content.transform);
            AddSpacer(content.transform, 20);
            BuildGameCenterRow(content.transform);
            AddSpacer(content.transform, 20);
            BuildAccountSection(content.transform);
            AddSpacer(content.transform, 20);
            BuildSaveButton(content.transform);
            AddSpacer(content.transform, 20);
            BuildVersionLabel(content.transform);
            AddSpacer(content.transform, 20);
            BuildBetaFeedback(content.transform);
            AddSpacer(content.transform, 20);

            // Disable raycast on all non-interactive graphics so they don't block scrolling.
            // Only Buttons/Selectables need raycast for taps — everything else should pass through.
            foreach (var graphic in content.GetComponentsInChildren<Graphic>(true))
            {
                if (graphic.GetComponent<Selectable>() != null) continue; // keep buttons tappable
                if (graphic.GetComponentInParent<Selectable>() != null &&
                    graphic.transform.parent?.GetComponent<Selectable>() != null) continue; // keep button children
                graphic.raycastTarget = false;
            }
            // Add scroll passthrough to remaining interactive elements (buttons, toggles, input)
            foreach (var selectable in content.GetComponentsInChildren<Selectable>(true))
                if (selectable.GetComponent<ScrollPassthrough>() == null)
                    selectable.gameObject.AddComponent<ScrollPassthrough>();
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
            nameInput.characterLimit = OS.displayNameMaxLength;
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

        private GameObject accountCard;
        private TextMeshProUGUI accountStatusLabel;
        private TextMeshProUGUI accountDetailLabel;
        private GameObject connectRow;
        private GameObject signedInRow;
        private GameObject signOutRow;

        private void BuildAccountSection(Transform parent)
        {
            // Section label
            var sectionLabel = MurgeUI.CreateLabel(parent, "ACCOUNT",
                MurgeUI.PressStart2P, OFont.labelXs, OC.muted, "AccountLabel");
            sectionLabel.alignment = TextAlignmentOptions.Left;
            sectionLabel.characterSpacing = 3;
            sectionLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 16;
            AddSpacer(parent, 6);

            accountCard = MurgeUI.CreateCard(parent);
            accountCard.name = "AccountCard";

            // Connect row (anonymous state) — entire card is tappable
            connectRow = MurgeUI.CreateUIObject("ConnectRow", accountCard.transform);
            var crRT = connectRow.GetComponent<RectTransform>();
            crRT.anchorMin = Vector2.zero; crRT.anchorMax = Vector2.one;
            crRT.offsetMin = Vector2.zero; crRT.offsetMax = Vector2.zero;

            // Invisible hit area covering the whole card
            var crHitImg = connectRow.AddComponent<Image>();
            crHitImg.color = Color.clear;
            var crBtn = connectRow.AddComponent<Button>();
            crBtn.targetGraphic = crHitImg;
            crBtn.onClick.AddListener(OnConnectAccountClicked);

            // Content inside with padding
            var crContent = MurgeUI.CreateUIObject("Content", connectRow.transform);
            MurgeUI.StretchFill(crContent.GetComponent<RectTransform>());
            var ccRT = crContent.GetComponent<RectTransform>();
            ccRT.offsetMin = new Vector2(14, 12); ccRT.offsetMax = new Vector2(-14, -12);
            var crVLG = crContent.AddComponent<VerticalLayoutGroup>();
            crVLG.spacing = 4;
            crVLG.childControlWidth = true;
            crVLG.childControlHeight = true;
            crVLG.childForceExpandWidth = true;
            crVLG.childForceExpandHeight = false;

            var connectLabel = MurgeUI.CreateUIObject("Label", crContent.transform);
            var cbTMP = connectLabel.AddComponent<TextMeshProUGUI>();
            cbTMP.text = "Connect account  >";
            cbTMP.font = MurgeUI.DMMono;
            cbTMP.fontSize = 14;
            cbTMP.color = Color.white;
            cbTMP.alignment = TextAlignmentOptions.Left;
            cbTMP.raycastTarget = false;
            connectLabel.AddComponent<LayoutElement>().preferredHeight = 20;

            var privacyNote = MurgeUI.CreateLabel(crContent.transform,
                "Your provider may share your name and email. We only use it to link your scores.",
                MurgeUI.DMMono, 9, OC.dim, "Privacy");
            privacyNote.alignment = TextAlignmentOptions.Left;
            privacyNote.raycastTarget = false;
            privacyNote.gameObject.AddComponent<LayoutElement>().preferredHeight = 14;

            // Signed in row (linked state)
            signedInRow = MurgeUI.CreateUIObject("SignedInRow", accountCard.transform);
            var siRT = signedInRow.GetComponent<RectTransform>();
            siRT.anchorMin = Vector2.zero; siRT.anchorMax = Vector2.one;
            siRT.offsetMin = new Vector2(14, 0); siRT.offsetMax = new Vector2(-14, 0);
            var siVLG = signedInRow.AddComponent<VerticalLayoutGroup>();
            siVLG.spacing = 4;
            siVLG.childControlWidth = true;
            siVLG.childControlHeight = true;
            siVLG.childForceExpandWidth = true;
            siVLG.childForceExpandHeight = false;
            siVLG.padding = new RectOffset(0, 0, 12, 8);

            accountStatusLabel = MurgeUI.CreateLabel(signedInRow.transform, "",
                MurgeUI.DMMono, 14, Color.white, "Status").GetComponent<TextMeshProUGUI>();
            accountStatusLabel.alignment = TextAlignmentOptions.Left;
            accountStatusLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 20;

            accountDetailLabel = MurgeUI.CreateLabel(signedInRow.transform, "",
                MurgeUI.DMMono, 10, OC.muted, "Detail").GetComponent<TextMeshProUGUI>();
            accountDetailLabel.alignment = TextAlignmentOptions.Left;
            accountDetailLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 14;

            // Sign out button — inside the VLG, after email
            signOutRow = MurgeUI.CreateUIObject("SignOutBtn", signedInRow.transform);
            var soLE = signOutRow.AddComponent<LayoutElement>();
            soLE.preferredHeight = 28;
            var soTMP = signOutRow.AddComponent<TextMeshProUGUI>();
            soTMP.text = "Sign out";
            soTMP.font = MurgeUI.DMMono;
            soTMP.fontSize = 11;
            soTMP.color = OC.muted;
            soTMP.alignment = TextAlignmentOptions.Left;
            var soBtn = signOutRow.AddComponent<Button>();
            soBtn.targetGraphic = soTMP;
            soBtn.onClick.AddListener(OnSignOutClicked);

            var cardLE = accountCard.AddComponent<LayoutElement>();
            cardLE.preferredHeight = 86;
            cardLE.minHeight = 86;

            RefreshAccountUI();
        }

        private void RefreshAccountUI()
        {
            if (accountCard == null) return;

            bool isAnonymous = AuthManager.Instance == null || AuthManager.Instance.IsAnonymous;
            connectRow.SetActive(isAnonymous);
            signedInRow.SetActive(!isAnonymous);
            signOutRow.SetActive(!isAnonymous);

            if (!isAnonymous && AuthManager.Instance != null)
            {
                string provider = AuthManager.Instance.AuthProvider;
                string providerDisplay = provider == "apple" ? "Signed in with Apple" : "Signed in with Google";
                accountStatusLabel.text = providerDisplay;
                accountDetailLabel.text = AuthManager.Instance.AuthEmail ?? "";
            }
        }

        private void OnConnectAccountClicked()
        {
            if (SignInSheet.Instance != null)
                SignInSheet.Instance.Show((provider) =>
                {
                    if (Core.PlayerIdentity.Instance != null)
                        Core.PlayerIdentity.Instance.RegisterAfterAuth();
                    RefreshServerDataAndGoHome(() =>
                    {
                        if (AuthLoadingOverlay.Instance != null)
                            AuthLoadingOverlay.Instance.Hide();
                    });
                });
        }

        private void OnSignOutClicked()
        {
            if (AuthManager.Instance == null) return;

            // Show confirmation
            if (SignOutConfirm.Instance != null)
            {
                SignOutConfirm.Instance.Show(() =>
                {
                    PerformSignOut();
                });
            }
            else
            {
                PerformSignOut();
            }
        }

        private void PerformSignOut()
        {
            if (AuthLoadingOverlay.Instance != null)
                AuthLoadingOverlay.Instance.Show("Signing out...");

            AuthManager.Instance.SignOut((success) =>
            {
                // Clear all local player data — full reset
                PlayerPrefs.SetString("player_display_name", "");
                PlayerPrefs.SetInt("HighScore", 0);
                PlayerPrefs.SetInt("current_streak", 0);
                PlayerPrefs.SetInt("longest_streak", 0);
                PlayerPrefs.DeleteKey("last_streak_date");
                PlayerPrefs.DeleteKey("player_uuid");
                string today = System.DateTime.Now.ToString("yyyy-MM-dd");
                PlayerPrefs.DeleteKey($"scored_attempt_{today}");
                PlayerPrefs.DeleteKey($"merge_counts_{today}");
                PlayerPrefs.DeleteKey("saved_game_state");
                PlayerPrefs.DeleteKey("freeplay_toast_date");
                PlayerPrefs.DeleteKey("link_prompt_dismissed");
                PlayerPrefs.Save();

                // Clear in-memory display name before creating new session
                if (Core.PlayerIdentity.Instance != null)
                    Core.PlayerIdentity.Instance.ClearDisplayName();

                // Create new anonymous session + register, then go home
                AuthManager.Instance.CreateAnonymousSession((anonSuccess) =>
                {
                    if (anonSuccess && Core.PlayerIdentity.Instance != null)
                        Core.PlayerIdentity.Instance.RegisterAfterAuth();

                    // Reset runtime state
                    Core.GameSession.TodayScore = 0;
                    Core.GameSession.ResultRank = 0;
                    Core.GameSession.ResultTotalPlayers = 0;
                    Core.GameSession.MergeCounts = new int[11];
                    if (Core.ScoreManager.Instance != null)
                        Core.ScoreManager.Instance.ResetScore();
                    if (Core.StreakManager.Instance != null)
                        Core.StreakManager.Instance.ResetStreak();

                    // Refresh leaderboard cache and navigate
                    RefreshServerDataAndGoHome(() =>
                    {
                        if (AuthLoadingOverlay.Instance != null)
                            AuthLoadingOverlay.Instance.Hide();
                    });
                });
            });
        }


        /// <summary>
        /// Re-fetch profile + leaderboard for the current user, then navigate to the correct home screen.
        /// Call after any identity change (connect, sign out).
        /// </summary>
        private void RefreshServerDataAndGoHome(System.Action onComplete = null)
        {
            string userId = Core.PlayerIdentity.Instance != null ? Core.PlayerIdentity.Instance.DeviceUUID : "";
            string today = Core.GameSession.TodayDateStr;

            // Invalidate and re-fetch leaderboard cache with new user_id
            if (LeaderboardService.Instance != null)
            {
                LeaderboardService.Instance.InvalidateCache();
                LeaderboardService.Instance.FetchLeaderboard(today, null);
            }

            // Fetch profile to see if this user has played today
            if (LeaderboardService.Instance != null && !string.IsNullOrEmpty(userId))
            {
                LeaderboardService.Instance.FetchPlayerProfile(userId, today, (profile) =>
                {
                    if (profile != null)
                    {
                        Core.GameSession.TodayScore = profile.today_score;
                        if (profile.day_number > 0)
                            Core.GameSession.TodayDayNumber = profile.day_number;
                        if (Core.GameSession.CurrentPlayer == null)
                            Core.GameSession.CurrentPlayer = new Player();
                        Core.GameSession.CurrentPlayer.display_name = profile.display_name;
                        Core.GameSession.CurrentPlayer.current_streak = profile.current_streak;

                        if (profile.today_score > 0 && Core.DailySeedManager.Instance != null)
                            Core.DailySeedManager.Instance.MarkScoredAttemptComplete();
                    }

                    NavigateHome();
                    onComplete?.Invoke();
                });
            }
            else
            {
                NavigateHome();
                onComplete?.Invoke();
            }
        }

        private void NavigateHome()
        {
            if (ScreenManager.Instance == null) return;
            bool hasPlayed = Core.GameSession.HasPlayedToday ||
                (Core.DailySeedManager.Instance != null && Core.DailySeedManager.Instance.HasCompletedScoredAttempt());
            ScreenManager.Instance.NavigateTo(hasPlayed ? Screen.HomePlayed : Screen.HomeFresh);
        }

        private void BuildVersionLabel(Transform parent)
        {
            var res = Resources.Load<TextAsset>("build-number");
            string build = res != null ? res.text.Trim() : "";
            string version = Application.version;
            string text = string.IsNullOrEmpty(build) ? $"v{version}" : $"v{version} (build {build})";
            var label = MurgeUI.CreateLabel(parent, text,
                MurgeUI.DMMono, 10, OC.dim, "VersionLabel");
            label.alignment = TextAlignmentOptions.Center;
            label.gameObject.AddComponent<LayoutElement>().preferredHeight = 16;
        }

        private void BuildBetaFeedback(Transform parent)
        {
            // Container with border — uses VLG + padding directly
            var container = MurgeUI.CreateUIObject("BetaFeedback", parent);
            var containerImg = container.AddComponent<Image>();
            containerImg.sprite = MurgeUI.SmoothRoundedRect;
            containerImg.type = Image.Type.Sliced;
            containerImg.color = OC.A(OC.border, 0.4f);
            containerImg.raycastTarget = false;

            // Inner content area (acts as the "fill" with padding)
            var content = MurgeUI.CreateUIObject("Content", container.transform);
            MurgeUI.StretchFill(content.GetComponent<RectTransform>());
            var cRT = content.GetComponent<RectTransform>();
            cRT.offsetMin = new Vector2(1, 1); cRT.offsetMax = new Vector2(-1, -1);
            var innerBg = content.AddComponent<Image>();
            innerBg.sprite = MurgeUI.SmoothRoundedRect;
            innerBg.type = Image.Type.Sliced;
            innerBg.color = OC.bg;
            innerBg.raycastTarget = false;

            // Padded VLG inside the fill
            var padded = MurgeUI.CreateUIObject("Padded", content.transform);
            MurgeUI.StretchFill(padded.GetComponent<RectTransform>());
            var pRT = padded.GetComponent<RectTransform>();
            pRT.offsetMin = new Vector2(16, 16); pRT.offsetMax = new Vector2(-16, -16);
            var vlg = padded.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 10;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // BETA label
            var betaLabel = MurgeUI.CreateLabel(padded.transform, "BETA",
                MurgeUI.PressStart2P, 9, OC.amber, "BetaLabel");
            betaLabel.characterSpacing = 2;
            betaLabel.alignment = TextAlignmentOptions.Left;
            betaLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 14;

            // Body copy
            var body = MurgeUI.CreateLabel(padded.transform,
                "We're still finding our footing. Ball sizes, scoring, and formats may shift as we dial things in. Scores might reset along the way -- we'll give a heads up when that happens.",
                MurgeUI.DMMono, 12, OC.muted, "BodyText");
            body.alignment = TextAlignmentOptions.Left;
            body.textWrappingMode = TextWrappingModes.Normal;
            body.lineSpacing = 25;

            // Secondary copy
            var secondary = MurgeUI.CreateLabel(padded.transform,
                "Have thoughts? We'd love to hear them.",
                MurgeUI.DMMono, 12, OC.muted, "SecondaryText");
            secondary.alignment = TextAlignmentOptions.Left;
            secondary.textWrappingMode = TextWrappingModes.Normal;
            secondary.lineSpacing = 25;

            // SEND FEEDBACK button — hug content, not full width
            var btnWrapper = MurgeUI.CreateUIObject("BtnWrapper", padded.transform);
            var bwHLG = btnWrapper.AddComponent<HorizontalLayoutGroup>();
            bwHLG.childAlignment = TextAnchor.MiddleLeft;
            bwHLG.childControlWidth = false;
            bwHLG.childControlHeight = false;
            bwHLG.childForceExpandWidth = false;
            var bwLE = btnWrapper.AddComponent<LayoutElement>();
            bwLE.preferredHeight = 36;

            var btnGO = MurgeUI.CreateUIObject("FeedbackBtn", btnWrapper.transform);
            var btnRT = btnGO.GetComponent<RectTransform>();
            btnRT.sizeDelta = new Vector2(210, 36);
            // Border
            var btnBgImg = btnGO.AddComponent<Image>();
            btnBgImg.sprite = Visual.PixelUIGenerator.GetRoundedRect9Slice();
            btnBgImg.type = Image.Type.Sliced;
            btnBgImg.color = OC.border;
            // Fill
            var btnFill = MurgeUI.CreateUIObject("Fill", btnGO.transform);
            var bfRT = btnFill.GetComponent<RectTransform>();
            bfRT.anchorMin = Vector2.zero; bfRT.anchorMax = Vector2.one;
            bfRT.offsetMin = new Vector2(1, 1); bfRT.offsetMax = new Vector2(-1, -1);
            var bfImg = btnFill.AddComponent<Image>();
            bfImg.sprite = Visual.PixelUIGenerator.GetRoundedRect9Slice();
            bfImg.type = Image.Type.Sliced;
            bfImg.color = OC.bg;
            bfImg.raycastTarget = false;
            // Label
            var btnLabel = MurgeUI.CreateUIObject("Label", btnGO.transform);
            MurgeUI.StretchFill(btnLabel.GetComponent<RectTransform>());
            var btnTMP = btnLabel.AddComponent<TextMeshProUGUI>();
            btnTMP.text = "SEND FEEDBACK";
            btnTMP.font = MurgeUI.PressStart2P;
            btnTMP.fontSize = 9;
            btnTMP.color = OC.cyan;
            btnTMP.characterSpacing = 1;
            btnTMP.alignment = TextAlignmentOptions.Center;
            btnTMP.raycastTarget = false;
            // Button
            var btn = btnGO.AddComponent<Button>();
            btn.targetGraphic = btnBgImg;
            btn.onClick.AddListener(() =>
            {
                Application.OpenURL("mailto:murgegame@gmail.com?subject=Murge%20Feedback");
            });

            var containerLE = container.AddComponent<LayoutElement>();
            containerLE.preferredHeight = 230;
            containerLE.minHeight = 230;
        }

        private void BuildSaveButton(Transform parent)
        {
            var (saveGO, saveTMP) = MurgeUI.CreatePrimaryButton(parent, "SAVE", 44, "SaveButton");

            saveButton = saveGO.GetComponent<Button>();
            saveLabel = saveTMP;
            saveBgImage = saveGO.GetComponent<Image>();
            saveButton.onClick.AddListener(OnSaveClicked);

            // Listen for name input changes to enable/disable save
            if (nameInput != null)
                nameInput.onValueChanged.AddListener(_ => UpdateSaveButtonState());
        }

        private void UpdateSaveButtonState()
        {
            if (saveButton == null) return;

            if (isSaving)
            {
                saveButton.interactable = false;
                if (saveLabel != null) saveLabel.text = "SAVING...";
                if (saveBgImage != null) saveBgImage.color = OC.border;
                return;
            }

            bool hasChanges = nameInput != null && nameInput.text.Trim() != originalName;
            saveButton.interactable = hasChanges;
            if (saveLabel != null) saveLabel.text = "SAVE";
            if (saveBgImage != null) saveBgImage.color = hasChanges ? OC.cyan : OC.border;
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
            if (isSaving) return;

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

            // Show loading state
            isSaving = true;
            UpdateSaveButtonState();

            if (LeaderboardService.Instance != null)
            {
                // Wait for server validation before navigating away
                LeaderboardService.Instance.UpdateDisplayName(newName, (serverSuccess, errorMsg) =>
                {
                    isSaving = false;

                    if (!serverSuccess)
                    {
                        // Revert local name change
                        if (PlayerIdentity.Instance != null)
                            PlayerIdentity.Instance.TrySetDisplayName(originalName);

                        string msg = !string.IsNullOrEmpty(errorMsg) ? errorMsg : "Failed to update name";
                        Toast.Show(msg);
                        UpdateSaveButtonState();
                        return;
                    }

                    if (Core.MurgeAnalytics.Instance != null)
                        Core.MurgeAnalytics.Instance.TrackNameChanged();

                    // Refresh leaderboard cache so home screen shows the new name
                    if (Backend.LeaderboardService.Instance != null && DailySeedManager.Instance != null)
                        Backend.LeaderboardService.Instance.FetchLeaderboard(DailySeedManager.Instance.GameDate);

                    OnBackClicked();
                });
            }
            else
            {
                OnBackClicked();
            }
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
        public static Sprite GetSmoothPill()
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
        public static Sprite GetSmoothCircle()
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
