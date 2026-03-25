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
        [Header("Screen Components")]
        [SerializeField] private TitleScreen titleScreen;
        [SerializeField] private ResultsScreen resultsScreen;
        [SerializeField] private SettingsScreen settingsScreen;

        [Header("Gameplay HUD")]
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private Image nextBallPreview;
        [SerializeField] private NextBallPreviewUI nextBallPreviewUI;
        [SerializeField] private ScoreTickUp scoreTickUp;

        [Header("Shake UI")]
        [SerializeField] private Button shakeButton;
        [SerializeField] private TextMeshProUGUI shakeCountText;

        [Header("Game Over UI")]
        [SerializeField] private TextMeshProUGUI finalScoreText;
        [SerializeField] private TextMeshProUGUI finalHighScoreText;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button playButton;

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
            // OnLeaderboardUpdated unsubscribe removed
        }

        // ===== Screen transitions =====

        public void ShowMenu()
        {
            if (ScreenManager.Instance != null)
            {
                bool hasPlayed = Core.GameSession.HasPlayedToday ||
                    (Core.DailySeedManager.Instance != null && Core.DailySeedManager.Instance.HasCompletedScoredAttempt());
                var target = hasPlayed ? Screen.HomePlayed : Screen.HomeFresh;
                ScreenManager.Instance.TransitionTo(target);
            }
        }

        public void ShowPlaying()
        {
            if (ScreenManager.Instance != null)
                ScreenManager.Instance.TransitionTo(Screen.Gameplay);

            UpdateScore(0);
            if (scoreTickUp != null) scoreTickUp.SetImmediate(0);
            if (ScoreManager.Instance != null)
                UpdateHighScore(ScoreManager.Instance.HighScore);
        }

        public void ShowGameOver(int finalScore, int highScore)
        {
            Core.GameSession.CaptureMergeCounts();

            if (ScreenManager.Instance != null)
                ScreenManager.Instance.NavigateTo(Screen.ResultOverlay);

            // Results screen
            if (resultsScreen != null)
            {
                var daily = DailySeedManager.Instance;
                int dayNumber = daily != null ? daily.DayNumber : 0;
                bool isScored = daily != null && daily.CurrentAttemptType == AttemptType.Scored;
                int streak = StreakManager.Instance != null ? StreakManager.Instance.CurrentStreak : 0;
                // topMerges removed — merge counts now stored in GameSession.MergeCounts

                // Get rank
                int rank = GameManager.Instance != null ? GameManager.Instance.GetLiveRank() : -1;
                string percentile = rank > 0 ? $"#{rank}" : "";

                resultsScreen.Populate(dayNumber, finalScore, streak, null, isScored, rank, percentile);
            }
        }

        // ===== Score updates =====

        private void UpdateScore(int score)
        {
            if (scoreText != null)
                scoreText.text = score.ToString();
            if (scoreTickUp != null)
                scoreTickUp.AnimateTo(score);

        }

        private void UpdateHighScore(int highScore)
        {
            if (highScoreText != null)
                highScoreText.text = $"Best: {highScore}";
        }

        // ===== Next ball =====

        private void UpdateNextBallPreview(BallData nextBall)
        {
            // Generate a UI-friendly sprite at a fixed pixel size
            if (nextBallPreview != null && nextBall != null)
            {
                int previewSize = 64;
                var previewSprite = GenerateUIBallSprite(nextBall.tierIndex, previewSize);
                if (previewSprite != null)
                {
                    nextBallPreview.sprite = previewSprite;
                    nextBallPreview.color = Color.white;
                }
            }

            if (nextBallPreviewUI != null && nextBall != null)
                nextBallPreviewUI.TransitionToNext(nextBall);
        }

        /// <summary>
        /// Generate a ball sprite sized for UI (fixed pixel size, PPU=pixelSize so it maps 1:1).
        /// </summary>
        private Sprite GenerateUIBallSprite(int tier, int pixelSize)
        {
            float uiRadius = pixelSize / (2f * Visual.BallRenderer.PixelsPerUnit);
            var png = Visual.BallRenderer.GenerateBallPNG(tier, Color.white, uiRadius, 0f);
            int expectedSize = pixelSize + 8; // padding
            var tex = new Texture2D(expectedSize, expectedSize, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            tex.LoadImage(png);
            return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height),
                new Vector2(0.5f, 0.5f), tex.width); // PPU = width so sprite is 1 unit
        }

        // ===== Shake =====

        private void UpdateShakeCount(int remaining)
        {
            if (shakeCountText != null)
                shakeCountText.text = remaining.ToString();
            if (shakeButton != null)
            {
                shakeButton.interactable = remaining > 0;
                var img = shakeButton.GetComponent<Image>();
                if (img != null)
                    img.color = remaining > 0 ? new Color(0.086f, 0.106f, 0.141f) : new Color(0.06f, 0.06f, 0.08f);
            }
        }

        private static readonly Color PanelColor = new Color(0.12f, 0.12f, 0.15f);
        private static readonly Color TextColor = new Color(0.92f, 0.92f, 0.95f);

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

            // NewLeaderboardScreen handles its own data fetching
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
            if (Core.OvertoneAnalytics.Instance != null)
                Core.OvertoneAnalytics.Instance.TrackShareCard();
        }

    }
}
