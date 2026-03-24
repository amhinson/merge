using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MergeGame.Core;
using MergeGame.Data;

namespace MergeGame.UI
{
    /// <summary>
    /// Home screen when the player has NOT played today.
    /// Shows PLAY button and "1 scored play per day" hint.
    /// </summary>
    public class HomeFreshScreen : HomeScreen
    {
        protected override void BuildCTABlock(Transform parent)
        {
            var (playGO, playLabel) = OvertoneUI.CreatePrimaryButton(parent, "PLAY", 44, "PlayButton");
            playGO.GetComponent<LayoutElement>().flexibleHeight = 0;
            playGO.GetComponent<Button>().onClick.AddListener(OnPlayClicked);

            var hint = OvertoneUI.CreateLabel(parent, "1 scored play per day",
                OvertoneUI.DMMono, OFont.caption, OC.dim, "HintLabel");
            hint.alignment = TextAlignmentOptions.Center;
            hint.characterSpacing = 1;
            hint.gameObject.AddComponent<LayoutElement>().preferredHeight = 16;
        }

        private void OnPlayClicked()
        {
            GameSession.IsPractice = false;
            if (GameManager.Instance != null)
                GameManager.Instance.OnPlayButtonPressed();
        }
    }
}
