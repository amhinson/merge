using UnityEngine;
using UnityEngine.UI;
using TMPro;
using MergeGame.Core;
using MergeGame.Data;
using MergeGame.Backend;

namespace MergeGame.UI
{
    /// <summary>
    /// Central UI coordinator. Delegates to ScreenManager for transitions
    /// and individual screen components for content.
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        [Header("Panels (legacy — kept for compatibility)")]
        [SerializeField] private GameObject menuPanel;
        [SerializeField] private GameObject playingPanel;
        [SerializeField] private GameObject gameOverPanel;

        [Header("Screen Components")]
        [SerializeField] private TitleScreen titleScreen;
        [SerializeField] private ResultsScreen resultsScreen;
        [SerializeField] private SettingsScreen settingsScreen;

        [Header("Gameplay HUD")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private Image nextBallPreview;
        [SerializeField] private TextMeshProUGUI nextBallLabel;
        [SerializeField] private float previewBaseSize = 120f;
        [SerializeField] private NextBallPreviewUI nextBallPreviewUI;
        [SerializeField] private MiniLeaderboardUI miniLeaderboard;
        [SerializeField] private ScoreTickUp scoreTickUp;

        [Header("Shake UI")]
        [SerializeField] private Button shakeButton;
        [SerializeField] private TextMeshProUGUI shakeCountText;

        [Header("Game Over UI")]
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI finalHighScoreText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button playButton;

        [Header("Leaderboard")]
        [SerializeField] private Button leaderboardBackButton;
        [SerializeField] private LeaderboardUI leaderboardUI;

        [Header("References")]
        [SerializeField] private BallTierConfig tierConfig;

        private void Start()
        {
            // Button wiring
            if (playButton != null)
                playButton.onClick.AddListener(OnPlayClicked);
            if (restartButton != null)
                restartButton.onClick.AddListener(OnRestartClicked);
            if (shakeButton != null)
                shakeButton.onClick.AddListener(OnShakeClicked);

            // Title screen buttons
            if (titleScreen != null)
            {
                if (titleScreen.PlayButton != null)
                    titleScreen.PlayButton.onClick.AddListener(OnPlayClicked);
                if (titleScreen.LeaderboardButton != null)
                    titleScreen.LeaderboardButton.onClick.AddListener(OnLeaderboardClicked);
                if (titleScreen.SettingsButton != null)
                    titleScreen.SettingsButton.onClick.AddListener(OnSettingsClicked);
            }

            // Results screen buttons
            if (resultsScreen != null)
            {
                if (resultsScreen.PlayAgainButton != null)
                    resultsScreen.PlayAgainButton.onClick.AddListener(OnRestartClicked);
                if (resultsScreen.HomeButton != null)
                    resultsScreen.HomeButton.onClick.AddListener(OnHomeClicked);
                if (resultsScreen.ShareButton != null)
                    resultsScreen.ShareButton.onClick.AddListener(OnShareClicked);
            }

            // Settings back button
            if (settingsScreen != null && settingsScreen.BackButton != null)
                settingsScreen.BackButton.onClick.AddListener(OnHomeClicked);

            // Leaderboard back button
            if (leaderboardBackButton != null)
                leaderboardBackButton.onClick.AddListener(OnHomeClicked);

            // Subscribe to events
            if (GameManager.Instance != null)
                GameManager.Instance.OnShakesChanged += UpdateShakeCount;

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged += UpdateScore;
                ScoreManager.Instance.OnHighScoreChanged += UpdateHighScore;
            }

            if (DropController.Instance != null)
                DropController.Instance.OnNextBallChanged += UpdateNextBallPreview;

            if (LeaderboardService.Instance != null)
                LeaderboardService.Instance.OnLeaderboardUpdated += OnLeaderboardDataUpdated;
        }

        private void OnDestroy()
        {
            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged -= UpdateScore;
                ScoreManager.Instance.OnHighScoreChanged -= UpdateHighScore;
            }
            if (DropController.Instance != null)
                DropController.Instance.OnNextBallChanged -= UpdateNextBallPreview;
            if (GameManager.Instance != null)
                GameManager.Instance.OnShakesChanged -= UpdateShakeCount;
            if (LeaderboardService.Instance != null)
                LeaderboardService.Instance.OnLeaderboardUpdated -= OnLeaderboardDataUpdated;
        }

        // ===== Screen transitions =====

        public void ShowMenu()
        {
            if (titleScreen != null) titleScreen.Refresh();

            if (ScreenManager.Instance != null)
            {
                ScreenManager.Instance.TransitionTo(Screen.Title);
            }
            else
            {
                SetPanelActive(menuPanel, true);
                SetPanelActive(playingPanel, false);
                SetPanelActive(gameOverPanel, false);
            }
        }

        public void ShowPlaying()
        {
            if (ScreenManager.Instance != null)
            {
                ScreenManager.Instance.TransitionTo(Screen.Gameplay);
            }
            else
            {
                SetPanelActive(menuPanel, false);
                SetPanelActive(playingPanel, true);
                SetPanelActive(gameOverPanel, false);
            }

            UpdateScore(0);
            if (scoreTickUp != null) scoreTickUp.SetImmediate(0);
            if (ScoreManager.Instance != null)
                UpdateHighScore(ScoreManager.Instance.HighScore);
            if (miniLeaderboard != null) miniLeaderboard.Clear();
        }

        public void ShowGameOver(int finalScore, int highScore)
        {
            if (ScreenManager.Instance != null)
            {
                ScreenManager.Instance.TransitionTo(Screen.Results);
            }
            else
            {
                SetPanelActive(menuPanel, false);
                SetPanelActive(playingPanel, true);
                SetPanelActive(gameOverPanel, true);
            }

            // Legacy game over panel
            if (finalScoreText != null)
                finalScoreText.text = $"Score: {finalScore}";
            if (finalHighScoreText != null)
                finalHighScoreText.text = $"Best: {highScore}";

            // Results screen
            if (resultsScreen != null)
            {
                var daily = DailySeedManager.Instance;
                int dayNumber = daily != null ? daily.DayNumber : 0;
                bool isScored = daily != null && daily.CurrentAttemptType == AttemptType.Scored;
                int streak = StreakManager.Instance != null ? StreakManager.Instance.CurrentStreak : 0;
                int[] topMerges = MergeTracker.Instance != null ? MergeTracker.Instance.GetTopMergeTiers() : null;

                // Get rank
                int rank = GameManager.Instance != null ? GameManager.Instance.GetLiveRank() : -1;
                string percentile = rank > 0 ? $"#{rank}" : "";

                resultsScreen.Populate(dayNumber, finalScore, streak, topMerges, isScored, rank, percentile);
            }
        }

        // ===== Score updates =====

        private void UpdateScore(int score)
        {
            if (scoreText != null)
                scoreText.text = score.ToString();
            if (scoreTickUp != null)
                scoreTickUp.AnimateTo(score);

            // Update mini leaderboard with live rank
            if (miniLeaderboard != null)
            {
                bool isReplay = DailySeedManager.Instance != null &&
                    DailySeedManager.Instance.CurrentAttemptType == AttemptType.Replay;
                miniLeaderboard.UpdateWithScore(score, isReplay);
            }
        }

        private void UpdateHighScore(int highScore)
        {
            if (highScoreText != null)
                highScoreText.text = $"Best: {highScore}";
        }

        // ===== Next ball =====

        private void UpdateNextBallPreview(BallData nextBall)
        {
            // Legacy preview
            if (nextBallPreview != null && nextBall != null)
            {
                nextBallPreview.color = nextBall.color;
                if (nextBall.sprite != null)
                    nextBallPreview.sprite = nextBall.sprite;

                float scale = nextBall.radius / 0.85f;
                float size = previewBaseSize * scale;
                nextBallPreview.rectTransform.sizeDelta = new Vector2(size, size);
            }

            // New preview UI
            if (nextBallPreviewUI != null && nextBall != null)
                nextBallPreviewUI.TransitionToNext(nextBall);
        }

        // ===== Shake =====

        private void UpdateShakeCount(int remaining)
        {
            if (shakeCountText != null)
                shakeCountText.text = remaining > 0 ? $"SHAKE x{remaining}" : "SHAKE";
            if (shakeButton != null)
            {
                shakeButton.interactable = remaining > 0;
                // Dim the button visually when disabled
                var img = shakeButton.GetComponent<Image>();
                if (img != null)
                    img.color = remaining > 0 ? PanelColor : new Color(0.12f, 0.12f, 0.15f);
                if (shakeCountText != null)
                    shakeCountText.color = remaining > 0 ? TextColor : new Color(0.3f, 0.3f, 0.35f);
            }
        }

        private static readonly Color PanelColor = new Color(0.12f, 0.12f, 0.15f);
        private static readonly Color TextColor = new Color(0.92f, 0.92f, 0.95f);

        // ===== Leaderboard data =====

        private void OnLeaderboardDataUpdated(System.Collections.Generic.List<LeaderboardEntry> entries)
        {
            if (miniLeaderboard != null)
                miniLeaderboard.SetCachedEntries(entries);
        }

        // ===== Button handlers =====

        private void OnPlayClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnPlayButtonPressed();
        }

        private void OnRestartClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnRestartButtonPressed();
        }

        private void OnShakeClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.TriggerShake();
        }

        private void OnHomeClicked()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.SetState(GameState.Menu);
        }

        private void OnLeaderboardClicked()
        {
            if (ScreenManager.Instance != null)
                ScreenManager.Instance.TransitionTo(Screen.Leaderboard);

            // Fetch leaderboard data
            if (LeaderboardService.Instance != null && DailySeedManager.Instance != null)
            {
                DailySeedManager.Instance.RefreshDay();
                Debug.Log($"Fetching leaderboard for {DailySeedManager.Instance.GameDate}");
                LeaderboardService.Instance.FetchLeaderboard(DailySeedManager.Instance.GameDate,
                    (entries) =>
                    {
                        Debug.Log($"Leaderboard returned {entries?.Count ?? 0} entries");
                        if (leaderboardUI != null)
                            leaderboardUI.Populate(entries);
                    });
            }
        }

        private void OnSettingsClicked()
        {
            if (settingsScreen != null) settingsScreen.Refresh();
            if (ScreenManager.Instance != null)
                ScreenManager.Instance.TransitionTo(Screen.Settings);
        }

        private void OnShareClicked()
        {
            if (ResultCardGenerator.Instance != null)
                ResultCardGenerator.Instance.ShareCard();
        }

        private void SetPanelActive(GameObject panel, bool active)
        {
            if (panel != null) panel.SetActive(active);
        }
    }
}
