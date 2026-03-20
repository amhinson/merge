using UnityEngine;
using TMPro;

namespace MergeGame.UI
{
    /// <summary>
    /// Displays current streak. Self-contained, repositionable component.
    /// </summary>
    public class StreakUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI streakText;

        public void UpdateStreak(int currentStreak)
        {
            if (streakText == null) return;

            if (currentStreak <= 0)
            {
                streakText.text = "";
                return;
            }

            streakText.text = $"{currentStreak} day streak";
        }
    }
}
