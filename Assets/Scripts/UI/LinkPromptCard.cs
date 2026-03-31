using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MergeGame.Core;
using MergeGame.Data;
using MergeGame.Backend;
using MergeGame.Visual;

namespace MergeGame.UI
{
    /// <summary>
    /// Dismissable card prompting anonymous users to connect an account.
    /// Shown on the home screen after a 2-day streak, once only.
    /// </summary>
    public static class LinkPromptCard
    {
        private const string DismissedKey = "link_prompt_dismissed";

        public static bool ShouldShow()
        {
            if (AuthManager.Instance == null || !AuthManager.Instance.IsAnonymous) return false;
            if (StreakManager.Instance == null || StreakManager.Instance.CurrentStreak < 2) return false;
            if (PlayerPrefs.GetInt(DismissedKey, 0) == 1) return false;
            return true;
        }

        /// <summary>
        /// Build and insert the link prompt card into a VLG parent.
        /// Returns the card GameObject (caller can position it).
        /// </summary>
        public static GameObject Build(Transform parent)
        {
            var container = MurgeUI.CreateUIObject("LinkPrompt", parent);

            // Border
            var bdr = MurgeUI.CreateUIObject("Border", container.transform);
            MurgeUI.StretchFill(bdr.GetComponent<RectTransform>());
            var bdrImg = bdr.AddComponent<Image>();
            bdrImg.sprite = PixelUIGenerator.GetRoundedRect9Slice();
            bdrImg.type = Image.Type.Sliced;
            bdrImg.color = OC.A(OC.cyan, 0.3f);
            bdrImg.raycastTarget = false;

            // Fill
            var fill = MurgeUI.CreateUIObject("Fill", container.transform);
            var fRT = fill.GetComponent<RectTransform>();
            fRT.anchorMin = Vector2.zero; fRT.anchorMax = Vector2.one;
            fRT.offsetMin = new Vector2(1, 1); fRT.offsetMax = new Vector2(-1, -1);
            var fImg = fill.AddComponent<Image>();
            fImg.sprite = PixelUIGenerator.GetRoundedRect9Slice();
            fImg.type = Image.Type.Sliced;
            fImg.color = OC.surface;
            fImg.raycastTarget = false;

            // Content
            var content = MurgeUI.CreateUIObject("Content", container.transform);
            var cRT = content.GetComponent<RectTransform>();
            cRT.anchorMin = Vector2.zero; cRT.anchorMax = Vector2.one;
            cRT.offsetMin = new Vector2(14, 12); cRT.offsetMax = new Vector2(-14, -12);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 8;
            vlg.childControlWidth = true;
            vlg.childControlHeight = true;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            // Heading
            var heading = MurgeUI.CreateLabel(content.transform, "Keep your scores safe",
                MurgeUI.PressStart2P, 8, OC.cyan, "Heading");
            heading.alignment = TextAlignmentOptions.Left;
            heading.gameObject.AddComponent<LayoutElement>().preferredHeight = 16;

            // Body
            var body = MurgeUI.CreateLabel(content.transform,
                "Connect an account so your scores and streak survive if you switch devices or reinstall.",
                MurgeUI.DMMono, 11, OC.muted, "Body");
            body.alignment = TextAlignmentOptions.Left;
            body.textWrappingMode = TextWrappingModes.Normal;
            body.lineSpacing = 18;

            // Privacy note
            var privacy = MurgeUI.CreateLabel(content.transform,
                "Your provider may share your name and email. We only use it to link your scores.",
                MurgeUI.DMMono, 9, OC.dim, "Privacy");
            privacy.alignment = TextAlignmentOptions.Left;
            privacy.gameObject.AddComponent<LayoutElement>().preferredHeight = 14;

            // Button row
            var btnRow = MurgeUI.CreateUIObject("BtnRow", content.transform);
            var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 8;
            hlg.childAlignment = TextAnchor.MiddleLeft;
            hlg.childControlWidth = false;
            hlg.childControlHeight = false;
            hlg.childForceExpandWidth = false;
            var btnRowLE = btnRow.AddComponent<LayoutElement>();
            btnRowLE.preferredHeight = 38;

            // CONNECT ACCOUNT button
            var (connectGO, connectLabel) = MurgeUI.CreatePrimaryButton(btnRow.transform, "CONNECT", 36, "ConnectBtn");
            connectGO.GetComponent<RectTransform>().sizeDelta = new Vector2(160, 36);
            connectGO.GetComponent<Button>().onClick.AddListener(() =>
            {
                if (SignInSheet.Instance != null)
                    SignInSheet.Instance.Show((provider) =>
                    {
                        // Dismiss on successful link
                        Dismiss(container);
                    });
            });

            // Not now
            var notNowGO = MurgeUI.CreateUIObject("NotNow", btnRow.transform);
            notNowGO.GetComponent<RectTransform>().sizeDelta = new Vector2(80, 36);
            var notNowTMP = notNowGO.AddComponent<TextMeshProUGUI>();
            notNowTMP.text = "Not now";
            notNowTMP.font = MurgeUI.DMMono;
            notNowTMP.fontSize = 11;
            notNowTMP.color = OC.muted;
            notNowTMP.alignment = TextAlignmentOptions.Center;
            var notNowBtn = notNowGO.AddComponent<Button>();
            notNowBtn.targetGraphic = notNowTMP;
            notNowBtn.onClick.AddListener(() => Dismiss(container));

            // Container height
            var containerLE = container.AddComponent<LayoutElement>();
            containerLE.preferredHeight = 170;
            containerLE.minHeight = 170;

            return container;
        }

        private static void Dismiss(GameObject card)
        {
            PlayerPrefs.SetInt(DismissedKey, 1);
            PlayerPrefs.Save();
            if (card != null) card.SetActive(false);
        }
    }
}
