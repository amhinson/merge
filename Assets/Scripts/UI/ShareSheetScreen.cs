using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using MergeGame.Core;
using MergeGame.Data;

namespace MergeGame.UI
{
    /// <summary>
    /// Bottom sheet overlay with branded share card preview and share target buttons.
    /// Slides up from bottom. Dismiss by tapping scrim or Cancel button.
    /// </summary>
    public class ShareSheetScreen : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private BallTierConfig tierConfig;

        private RectTransform sheetPanel;
        private float sheetHeight = 520f;
        private bool isBuilt;

        // Share card elements
        private TextMeshProUGUI cardScoreValue;
        private TextMeshProUGUI cardDateLine;
        private Transform cardMergeGrid;

        private void OnEnable()
        {
            if (!isBuilt) { BuildUI(); isBuilt = true; }
            RefreshCard();
            StartCoroutine(SlideIn());
        }

        private void BuildUI()
        {
            var rt = GetComponent<RectTransform>();
            if (rt == null) rt = gameObject.AddComponent<RectTransform>();

            // Full-screen scrim
            var scrim = MurgeUI.CreateUIObject("Scrim", transform);
            var scrimImg = scrim.AddComponent<Image>();
            scrimImg.color = new Color(0, 0, 0, 0.55f);
            MurgeUI.StretchFill(scrim.GetComponent<RectTransform>());
            var scrimBtn = scrim.AddComponent<Button>();
            scrimBtn.onClick.AddListener(Dismiss);

            // Sheet panel (anchored bottom)
            var panelGO = MurgeUI.CreateUIObject("SheetPanel", transform);
            sheetPanel = panelGO.GetComponent<RectTransform>();
            sheetPanel.anchorMin = new Vector2(0, 0);
            sheetPanel.anchorMax = new Vector2(1, 0);
            sheetPanel.pivot = new Vector2(0.5f, 0);
            sheetPanel.sizeDelta = new Vector2(0, sheetHeight);
            sheetPanel.anchoredPosition = new Vector2(0, -sheetHeight); // start offscreen

            var panelImg = panelGO.AddComponent<Image>();
            panelImg.sprite = Visual.PixelUIGenerator.GetRoundedRect9Slice();
            panelImg.type = Image.Type.Sliced;
            panelImg.color = new Color(0.102f, 0.122f, 0.18f, 1f); // #1A1F2E

            var vlg = panelGO.AddComponent<VerticalLayoutGroup>();
            vlg.childAlignment = TextAnchor.UpperCenter;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.spacing = 0;
            vlg.padding = new RectOffset(0, 0, 0, 0);

            // Drag handle
            var handle = MurgeUI.CreateUIObject("DragHandle", panelGO.transform);
            var handleImg = handle.AddComponent<Image>();
            handleImg.sprite = Visual.PixelUIGenerator.GetRoundedRect9Slice();
            handleImg.type = Image.Type.Sliced;
            handleImg.color = OC.border;
            handleImg.raycastTarget = false;
            var handleRT = handle.GetComponent<RectTransform>();
            handle.AddComponent<LayoutElement>().preferredHeight = 4;
            // Center the handle with fixed width
            var handleHLG = MurgeUI.CreateUIObject("HandleWrapper", panelGO.transform);
            handle.transform.SetParent(handleHLG.transform, false);
            var wrapHLG = handleHLG.AddComponent<HorizontalLayoutGroup>();
            wrapHLG.childAlignment = TextAnchor.MiddleCenter;
            wrapHLG.childForceExpandWidth = false;
            wrapHLG.childControlWidth = false;
            wrapHLG.padding = new RectOffset(0, 0, 10, 6);
            handleHLG.AddComponent<LayoutElement>().preferredHeight = 20;
            handleRT.sizeDelta = new Vector2(36, 4);

            // Share card preview
            BuildShareCard(panelGO.transform);

            // Share targets row
            BuildShareTargets(panelGO.transform);

            // Cancel button
            var cancelPadding = MurgeUI.CreateUIObject("CancelPad", panelGO.transform);
            var cancelVLG = cancelPadding.AddComponent<VerticalLayoutGroup>();
            cancelVLG.padding = new RectOffset(20, 20, 0, 44 + (int)OS.safeAreaBottom);
            cancelVLG.childControlWidth = true;
            cancelVLG.childControlHeight = false;
            cancelVLG.childForceExpandWidth = true;

            var (cancelGO, cancelLabel) = MurgeUI.CreateGhostButton(cancelPadding.transform, "Cancel", 44, "CancelButton");
            cancelLabel.font = MurgeUI.DMMono;
            cancelLabel.fontSize = OFont.body;
            cancelGO.GetComponent<Button>().onClick.AddListener(Dismiss);
        }

        private void BuildShareCard(Transform parent)
        {
            var wrapper = MurgeUI.CreateUIObject("CardPreview", parent);
            var wrapperVLG = wrapper.AddComponent<VerticalLayoutGroup>();
            wrapperVLG.padding = new RectOffset(20, 20, 14, 14);
            wrapperVLG.childControlWidth = true;
            wrapperVLG.childControlHeight = false;
            wrapperVLG.childForceExpandWidth = true;

            var card = MurgeUI.CreateUIObject("ShareCard", wrapper.transform);
            var cardImg = card.AddComponent<Image>();
            cardImg.sprite = Visual.PixelUIGenerator.GetRoundedRect9Slice();
            cardImg.type = Image.Type.Sliced;
            cardImg.color = OC.shareCardBg;

            // Card outline
            var outline = MurgeUI.CreateUIObject("Outline", card.transform);
            var outlineImg = outline.AddComponent<Image>();
            outlineImg.sprite = Visual.PixelUIGenerator.GetRoundedRect9Slice();
            outlineImg.type = Image.Type.Sliced;
            outlineImg.color = OC.shareCardBorder;
            outlineImg.raycastTarget = false;
            MurgeUI.StretchFill(outline.GetComponent<RectTransform>());

            var cardVLG = card.AddComponent<VerticalLayoutGroup>();
            cardVLG.childAlignment = TextAnchor.UpperCenter;
            cardVLG.childControlWidth = true;
            cardVLG.childControlHeight = false;
            cardVLG.childForceExpandWidth = true;
            cardVLG.spacing = 0;
            cardVLG.padding = new RectOffset(20, 20, 20, 20);

            // Branding: OVER + TONE
            var brandRow = MurgeUI.CreateUIObject("BrandingLine", card.transform);
            var brandHLG = brandRow.AddComponent<HorizontalLayoutGroup>();
            brandHLG.childAlignment = TextAnchor.MiddleCenter;
            brandHLG.spacing = 0;
            brandHLG.childControlWidth = false;
            brandHLG.childControlHeight = true;
            brandHLG.childForceExpandWidth = false;
            brandRow.AddComponent<LayoutElement>().preferredHeight = 18;

            var brandTMP = MurgeUI.CreateLabel(brandRow.transform, Core.GameSession.AppName,
                MurgeUI.PressStart2P, OFont.heading, OC.cyan, "Title");
            brandTMP.characterSpacing = 2;

            // Date line
            AddSpacer(card.transform, 4);
            cardDateLine = MurgeUI.CreateLabel(card.transform, "",
                MurgeUI.DMMono, OFont.bodyXs, OC.A(OC.white, 0.55f), "DateLine");
            cardDateLine.characterSpacing = 2;
            cardDateLine.alignment = TextAlignmentOptions.Center;
            cardDateLine.gameObject.AddComponent<LayoutElement>().preferredHeight = 16;

            // Score
            AddSpacer(card.transform, 12);
            cardScoreValue = MurgeUI.CreateLabel(card.transform, "0",
                MurgeUI.DMMono, 44, OC.cyan, "CardScore");
            cardScoreValue.fontStyle = FontStyles.Bold;
            cardScoreValue.alignment = TextAlignmentOptions.Center;
            cardScoreValue.gameObject.AddComponent<LayoutElement>().preferredHeight = 52;

            // Mini merge grid
            AddSpacer(card.transform, 12);
            var gridGO = MurgeUI.CreateUIObject("MiniMergeGrid", card.transform);
            var grid = gridGO.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(30, 30);
            grid.spacing = new Vector2(5, 5);
            grid.constraint = GridLayoutGroup.Constraint.Flexible;
            grid.childAlignment = TextAnchor.MiddleCenter;
            cardMergeGrid = gridGO.transform;

            // Footer
            AddSpacer(card.transform, 14);
            var footer = MurgeUI.CreateLabel(card.transform, Core.GameSession.AppDomain,
                MurgeUI.DMMono, OFont.caption, OC.A(OC.white, 0.40f), "Footer");
            footer.characterSpacing = 2;
            footer.alignment = TextAlignmentOptions.Center;
            footer.gameObject.AddComponent<LayoutElement>().preferredHeight = 14;
        }

        private void BuildShareTargets(Transform parent)
        {
            var row = MurgeUI.CreateUIObject("ShareTargetRow", parent);
            var hlg = row.AddComponent<HorizontalLayoutGroup>();
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.spacing = 0;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = true;
            hlg.padding = new RectOffset(20, 20, 8, 12);
            row.AddComponent<LayoutElement>().preferredHeight = 80;

            string[] icons = { "\U0001F4AC", "\U0001D54F", "\U0001F4F7", "\U0001F4CB", "\u00B7\u00B7\u00B7" };
            string[] labels = { "Messages", "X", "Instagram", "Copy", "More" };

            for (int i = 0; i < icons.Length; i++)
            {
                var target = MurgeUI.CreateUIObject($"Target_{labels[i]}", row.transform);
                var targetVLG = target.AddComponent<VerticalLayoutGroup>();
                targetVLG.childAlignment = TextAnchor.MiddleCenter;
                targetVLG.spacing = 6;
                targetVLG.childControlWidth = true;
                targetVLG.childControlHeight = false;
                targetVLG.childForceExpandWidth = true;

                // Icon button
                var iconGO = MurgeUI.CreateUIObject("Icon", target.transform);
                var iconImg = iconGO.AddComponent<Image>();
                iconImg.sprite = Visual.PixelUIGenerator.GetRoundedRect9Slice();
                iconImg.type = Image.Type.Sliced;
                iconImg.color = OC.surface;
                iconGO.GetComponent<RectTransform>().sizeDelta = new Vector2(52, 52);
                iconGO.AddComponent<LayoutElement>().preferredHeight = 52;

                var iconTMP = MurgeUI.CreateLabel(iconGO.transform, icons[i],
                    MurgeUI.DMMono, 18, OC.white, "IconText");
                iconTMP.alignment = TextAlignmentOptions.Center;
                MurgeUI.StretchFill(iconTMP.GetComponent<RectTransform>());

                int capturedIndex = i;
                var btn = iconGO.AddComponent<Button>();
                btn.onClick.AddListener(() => OnShareTargetClicked(capturedIndex));

                // App label
                var appLabel = MurgeUI.CreateLabel(target.transform, labels[i],
                    MurgeUI.DMMono, OFont.caption, OC.muted, "AppLabel");
                appLabel.alignment = TextAlignmentOptions.Center;
                appLabel.gameObject.AddComponent<LayoutElement>().preferredHeight = 14;
            }
        }

        private void RefreshCard()
        {
            if (cardScoreValue != null)
                cardScoreValue.text = GameSession.TodayScore.ToString("N0");

            if (cardDateLine != null)
            {
                var now = System.DateTime.Now;
                cardDateLine.text = $"#{GameSession.TodayDayNumber} \u00B7 {now.ToString("MMM dd").ToUpper()}";
            }

            PopulateMiniGrid();
        }

        private void PopulateMiniGrid()
        {
            if (cardMergeGrid == null) return;

            foreach (Transform child in cardMergeGrid)
                Destroy(child.gameObject);

            int[] counts = GameSession.MergeCounts ?? new int[11];

            for (int tier = 0; tier < 11; tier++)
            {
                if (counts[tier] <= 0) continue;

                BallData data = tierConfig != null ? tierConfig.GetTier(tier) : null;
                if (data == null) continue;

                var cell = MurgeUI.CreateUIObject($"Mini{tier}", cardMergeGrid);
                var cellVLG = cell.AddComponent<VerticalLayoutGroup>();
                cellVLG.childAlignment = TextAnchor.MiddleCenter;
                cellVLG.spacing = 1;
                cellVLG.childControlWidth = true;
                cellVLG.childControlHeight = false;
                cellVLG.childForceExpandWidth = true;

                var ballGO = MurgeUI.CreateUIObject("Ball", cell.transform);
                var ballImg = ballGO.AddComponent<Image>();
                if (data.sprite != null)
                {
                    ballImg.sprite = data.sprite;
                    ballImg.color = Color.white;
                }
                else
                {
                    ballImg.color = data.color;
                }
                ballImg.preserveAspect = true;
                ballGO.AddComponent<LayoutElement>().preferredHeight = 18;

                var countTMP = MurgeUI.CreateLabel(cell.transform,
                    $"\u00D7{counts[tier]}", MurgeUI.PressStart2P,
                    OFont.labelXxs, data.color, "Count");
                countTMP.alignment = TextAlignmentOptions.Center;
                countTMP.gameObject.AddComponent<LayoutElement>().preferredHeight = 8;
            }
        }

        // ───── Slide animation ─────

        private IEnumerator SlideIn()
        {
            if (sheetPanel == null) yield break;
            float elapsed = 0f;
            float duration = 0.35f;
            float startY = -sheetHeight;
            float endY = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease out cubic
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                sheetPanel.anchoredPosition = new Vector2(0, Mathf.Lerp(startY, endY, eased));
                yield return null;
            }
            sheetPanel.anchoredPosition = new Vector2(0, endY);
        }

        private IEnumerator SlideOut(System.Action onComplete)
        {
            if (sheetPanel == null) { onComplete?.Invoke(); yield break; }
            float elapsed = 0f;
            float duration = 0.25f;
            float startY = sheetPanel.anchoredPosition.y;
            float endY = -sheetHeight;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease in cubic
                float eased = t * t * t;
                sheetPanel.anchoredPosition = new Vector2(0, Mathf.Lerp(startY, endY, eased));
                yield return null;
            }
            onComplete?.Invoke();
        }

        private void Dismiss()
        {
            StartCoroutine(SlideOut(() =>
            {
                if (ScreenManager.Instance != null)
                    ScreenManager.Instance.DismissOverlay();
            }));
        }

        private void OnShareTargetClicked(int targetIndex)
        {
            // For now, all share targets invoke the same native share
            // Index: 0=Messages, 1=X, 2=Instagram, 3=Copy, 4=More
            if (ResultCardGenerator.Instance != null)
                ResultCardGenerator.Instance.ShareCard();
            else
                Debug.Log($"Share target {targetIndex} clicked — ResultCardGenerator not available");
        }

        private void AddSpacer(Transform parent, float height)
        {
            var s = MurgeUI.CreateUIObject("Spacer", parent);
            s.AddComponent<LayoutElement>().preferredHeight = height;
        }
    }
}
