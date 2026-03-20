using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MergeGame.Core;

namespace MergeGame.UI
{
    /// <summary>
    /// Title screen — shown on every app launch.
    /// </summary>
    public class TitleScreen : MonoBehaviour
    {
        [Header("Display")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI dayText;
        [SerializeField] private TextMeshProUGUI streakText;

        [Header("Buttons")]
        [SerializeField] private Button playButton;
        [SerializeField] private TextMeshProUGUI playButtonLabel;
        [SerializeField] private Button leaderboardButton;
        [SerializeField] private Button settingsButton;

        public Button PlayButton => playButton;
        public Button LeaderboardButton => leaderboardButton;
        public Button SettingsButton => settingsButton;

        public void Refresh()
        {
            // Day number
            if (dayText != null && DailySeedManager.Instance != null)
            {
                DailySeedManager.Instance.RefreshDay();
                dayText.text = $"Day #{DailySeedManager.Instance.DayNumber}";
            }

            // Streak
            if (streakText != null && StreakManager.Instance != null)
            {
                int streak = StreakManager.Instance.CurrentStreak;
                streakText.text = streak > 0 ? $"{streak} day streak" : "";
                streakText.gameObject.SetActive(streak > 0);
            }

            // Play button label — indicate if scored attempt is done
            if (playButtonLabel != null && DailySeedManager.Instance != null)
            {
                DailySeedManager.Instance.RefreshDay();
                bool alreadyScored = DailySeedManager.Instance.HasCompletedScoredAttempt();
                playButtonLabel.text = alreadyScored ? "Play Again" : "Play";
            }
        }
    }
}
