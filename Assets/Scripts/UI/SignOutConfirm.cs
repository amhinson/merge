using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using MergeGame.Data;
using MergeGame.Visual;

namespace MergeGame.UI
{
    /// <summary>
    /// Simple modal confirmation for signing out.
    /// Self-managing singleton.
    /// </summary>
    public class SignOutConfirm : MonoBehaviour
    {
        public static SignOutConfirm Instance { get; private set; }

        private GameObject overlayGO;
        private CanvasGroup canvasGroup;
        private bool isBuilt;
        private Action onConfirm;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            if (Instance != null) return;
            var go = new GameObject("SignOutConfirm");
            go.AddComponent<SignOutConfirm>();
        }

        public void Show(Action confirmCallback)
        {
            if (!isBuilt) { BuildUI(); isBuilt = true; }
            onConfirm = confirmCallback;
            overlayGO.SetActive(true);
            canvasGroup.alpha = 1f;
        }

        public void Hide()
        {
            if (overlayGO != null) overlayGO.SetActive(false);
        }

        private void BuildUI()
        {
            var canvas = FindAnyObjectByType<Canvas>();
            if (canvas == null) return;

            overlayGO = new GameObject("SignOutOverlay");
            overlayGO.transform.SetParent(canvas.transform, false);

            var rt = overlayGO.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            canvasGroup = overlayGO.AddComponent<CanvasGroup>();
            canvasGroup.blocksRaycasts = true;

            var overlayCanvas = overlayGO.AddComponent<Canvas>();
            overlayCanvas.overrideSorting = true;
            overlayCanvas.sortingOrder = 110;
            overlayGO.AddComponent<GraphicRaycaster>();

            // Scrim
            var scrimImg = overlayGO.AddComponent<Image>();
            scrimImg.color = new Color(0.031f, 0.031f, 0.055f, 0.88f);

            // Card
            var card = MurgeUI.CreateUIObject("Card", overlayGO.transform);
            var cardRT = card.GetComponent<RectTransform>();
            cardRT.anchorMin = new Vector2(0.5f, 0.5f);
            cardRT.anchorMax = new Vector2(0.5f, 0.5f);
            cardRT.pivot = new Vector2(0.5f, 0.5f);
            cardRT.sizeDelta = new Vector2(280, 200);

            var cardBg = card.AddComponent<Image>();
            cardBg.sprite = PixelUIGenerator.GetRoundedRect9Slice();
            cardBg.type = Image.Type.Sliced;
            cardBg.color = OC.surface;
            cardBg.raycastTarget = true;

            var border = MurgeUI.CreateUIObject("Border", card.transform);
            MurgeUI.StretchFill(border.GetComponent<RectTransform>());
            var bImg = border.AddComponent<Image>();
            bImg.sprite = PixelUIGenerator.GetRoundedRect9Slice();
            bImg.type = Image.Type.Sliced;
            bImg.color = OC.border;
            bImg.raycastTarget = false;

            // Content
            var content = MurgeUI.CreateUIObject("Content", card.transform);
            var cRT = content.GetComponent<RectTransform>();
            cRT.anchorMin = Vector2.zero; cRT.anchorMax = Vector2.one;
            cRT.offsetMin = new Vector2(20, 20); cRT.offsetMax = new Vector2(-20, -20);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 12;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment = TextAnchor.MiddleCenter;

            // Title
            var title = MurgeUI.CreateLabel(content.transform, "Sign out?",
                MurgeUI.PressStart2P, 12, Color.white, "Title");
            title.alignment = TextAlignmentOptions.Center;
            title.gameObject.AddComponent<LayoutElement>().preferredHeight = 22;

            // Body
            var body = MurgeUI.CreateLabel(content.transform,
                "You'll continue as a new anonymous player. Sign back in anytime to recover your scores.",
                MurgeUI.DMMono, 11, OC.muted, "Body");
            body.alignment = TextAlignmentOptions.Center;
            body.textWrappingMode = TextWrappingModes.Normal;
            body.lineSpacing = 16;

            // Buttons
            var btnRow = MurgeUI.CreateUIObject("Buttons", content.transform);
            var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            var btnLE = btnRow.AddComponent<LayoutElement>();
            btnLE.preferredHeight = 40;

            // Sign out (pink)
            var signOutGO = MurgeUI.CreateUIObject("SignOutBtn", btnRow.transform);
            var soBgImg = signOutGO.AddComponent<Image>();
            soBgImg.sprite = PixelUIGenerator.GetRoundedRect9Slice();
            soBgImg.type = Image.Type.Sliced;
            soBgImg.color = OC.A(OC.pink, 0.3f);
            var soFill = MurgeUI.CreateUIObject("Fill", signOutGO.transform);
            var sfRT = soFill.GetComponent<RectTransform>();
            sfRT.anchorMin = Vector2.zero; sfRT.anchorMax = Vector2.one;
            sfRT.offsetMin = new Vector2(1, 1); sfRT.offsetMax = new Vector2(-1, -1);
            var sfImg = soFill.AddComponent<Image>();
            sfImg.sprite = PixelUIGenerator.GetRoundedRect9Slice();
            sfImg.type = Image.Type.Sliced;
            sfImg.color = OC.surface;
            sfImg.raycastTarget = false;
            var soLabelGO = MurgeUI.CreateUIObject("Label", signOutGO.transform);
            MurgeUI.StretchFill(soLabelGO.GetComponent<RectTransform>());
            var soLabel = soLabelGO.AddComponent<TextMeshProUGUI>();
            soLabel.text = "SIGN OUT";
            soLabel.font = MurgeUI.PressStart2P;
            soLabel.fontSize = 9;
            soLabel.color = OC.pink;
            soLabel.alignment = TextAlignmentOptions.Center;
            soLabel.raycastTarget = false;
            var soBtn = signOutGO.AddComponent<Button>();
            soBtn.targetGraphic = soBgImg;
            soBtn.onClick.AddListener(() =>
            {
                Hide();
                onConfirm?.Invoke();
            });

            // Cancel — border + transparent fill
            var cancelGO = MurgeUI.CreateUIObject("CancelBtn", btnRow.transform);
            var caBgImg = cancelGO.AddComponent<Image>();
            caBgImg.sprite = PixelUIGenerator.GetRoundedRect9Slice();
            caBgImg.type = Image.Type.Sliced;
            caBgImg.color = OC.border;
            var caFill = MurgeUI.CreateUIObject("Fill", cancelGO.transform);
            var caFillRT = caFill.GetComponent<RectTransform>();
            caFillRT.anchorMin = Vector2.zero; caFillRT.anchorMax = Vector2.one;
            caFillRT.offsetMin = new Vector2(1, 1); caFillRT.offsetMax = new Vector2(-1, -1);
            var caFillImg = caFill.AddComponent<Image>();
            caFillImg.sprite = PixelUIGenerator.GetRoundedRect9Slice();
            caFillImg.type = Image.Type.Sliced;
            caFillImg.color = OC.surface;
            caFillImg.raycastTarget = false;
            var caLabelGO = MurgeUI.CreateUIObject("Label", cancelGO.transform);
            MurgeUI.StretchFill(caLabelGO.GetComponent<RectTransform>());
            var caLabel = caLabelGO.AddComponent<TextMeshProUGUI>();
            caLabel.text = "CANCEL";
            caLabel.font = MurgeUI.PressStart2P;
            caLabel.fontSize = OFont.label;
            caLabel.color = OC.muted;
            caLabel.characterSpacing = 1;
            caLabel.alignment = TextAlignmentOptions.Center;
            caLabel.raycastTarget = false;
            var caBtn = cancelGO.AddComponent<Button>();
            caBtn.targetGraphic = caBgImg;
            caBtn.onClick.AddListener(Hide);

            overlayGO.SetActive(false);
        }
    }
}
