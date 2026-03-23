using UnityEngine;
using MergeGame.UI;
using MergeGame.Audio;
using MergeGame.Backend;

namespace MergeGame.Core
{
    public enum GameState
    {
        Menu,
        Playing,
        GameOver
    }

    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] private DropController dropController;
        [SerializeField] private ScoreManager scoreManager;
        [SerializeField] private UIManager uiManager;
        [SerializeField] private AudioManager audioManager;

        [Header("Shake Settings")]
        [SerializeField] private int maxShakes = 3;
        [SerializeField] private float shakeForce = 5f;
        [SerializeField] private float shakeDuration = 0.3f;

        public GameState CurrentState { get; private set; } = GameState.Menu;
        public int ShakesRemaining { get; private set; }

        public event System.Action<int> OnShakesChanged;

        // Live rank tracking
        private float liveRankRefreshTimer;
        private const float LiveRankRefreshInterval = 45f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // Initialize daily seed
            if (DailySeedManager.Instance != null)
                DailySeedManager.Instance.RefreshDay();

            // Initialize session state
            GameSession.Init();

            // Fetch player profile to determine home screen state
            FetchProfileAndShowHome();
        }

        private void FetchProfileAndShowHome()
        {
            if (LeaderboardService.Instance != null && PlayerIdentity.Instance != null)
            {
                LeaderboardService.Instance.FetchPlayerProfile(
                    PlayerIdentity.Instance.DeviceUUID,
                    GameSession.TodayDateStr,
                    (profile) =>
                    {
                        if (profile != null)
                        {
                            GameSession.TodayScore = profile.today_score;
                            GameSession.TodayDayNumber = profile.day_number;

                            if (GameSession.CurrentPlayer == null)
                                GameSession.CurrentPlayer = new Player();
                            GameSession.CurrentPlayer.display_name = profile.display_name;
                            GameSession.CurrentPlayer.current_streak = profile.current_streak;
                            GameSession.CurrentPlayer.longest_streak = profile.longest_streak;
                        }

                        NavigateToInitialScreen();
                    });
            }
            else
            {
                // No backend — go straight to home
                NavigateToInitialScreen();
            }
        }

        private void NavigateToInitialScreen()
        {
            if (GameSession.IsFirstLaunch && ScreenManager.Instance != null)
            {
                ScreenManager.Instance.ShowImmediate(UI.Screen.Onboarding);
                return;
            }

            SetState(GameState.Menu);
        }

        private void Update()
        {
            if (CurrentState != GameState.Playing) return;

            // Periodically refresh leaderboard cache for live rank
            liveRankRefreshTimer += Time.deltaTime;
            if (liveRankRefreshTimer >= LiveRankRefreshInterval)
            {
                liveRankRefreshTimer = 0f;
                if (DailySeedManager.Instance != null && LeaderboardService.Instance != null)
                {
                    LeaderboardService.Instance.RefreshIfStale(DailySeedManager.Instance.GameDate);
                }
            }
        }

        public void SetState(GameState newState)
        {
            CurrentState = newState;

            switch (newState)
            {
                case GameState.Menu:
                    OnEnterMenu();
                    break;
                case GameState.Playing:
                    OnEnterPlaying();
                    break;
                case GameState.GameOver:
                    OnEnterGameOver();
                    break;
            }
        }

        private void OnEnterMenu()
        {
            if (dropController != null) dropController.SetActive(false);
            if (uiManager != null) uiManager.ShowMenu();
        }

        private void OnEnterPlaying()
        {
            ClearAllBalls();
            ShakesRemaining = maxShakes;
            OnShakesChanged?.Invoke(ShakesRemaining);

            // Set practice mode based on whether already scored today
            GameSession.IsPractice = DailySeedManager.Instance != null &&
                DailySeedManager.Instance.HasCompletedScoredAttempt();

            // Prepare daily seed and attempt tracking
            if (DailySeedManager.Instance != null)
                DailySeedManager.Instance.PrepareNewGame();

            // Reset merge tracker
            if (MergeTracker.Instance != null)
                MergeTracker.Instance.ResetForNewRound();

            if (scoreManager != null) scoreManager.ResetScore();
            if (dropController != null) dropController.SetActive(true);
            if (uiManager != null) uiManager.ShowPlaying();

            // Fetch initial leaderboard for live rank
            liveRankRefreshTimer = 0f;
            if (DailySeedManager.Instance != null && LeaderboardService.Instance != null)
            {
                LeaderboardService.Instance.FetchLeaderboard(DailySeedManager.Instance.GameDate);
            }
        }

        private void OnEnterGameOver()
        {
            if (dropController != null) dropController.SetActive(false);
            if (scoreManager != null) scoreManager.SaveHighScore();
            if (audioManager != null) audioManager.PlayGameOver();
            if (HapticManager.Instance != null) HapticManager.Instance.PlayGameOver();

            int finalScore = scoreManager != null ? scoreManager.CurrentScore : 0;
            int highScore = scoreManager != null ? scoreManager.HighScore : 0;

            // Achievement tracking
            if (AchievementManager.Instance != null)
                AchievementManager.Instance.OnGameCompleted(finalScore);

            // Handle scored attempt
            var daily = DailySeedManager.Instance;
            if (daily == null)
            {
                Debug.LogWarning("GameManager: DailySeedManager.Instance is null — skipping score submission");
            }
            else if (daily.CurrentAttemptType == AttemptType.Scored)
            {
                Debug.Log($"GameManager: Submitting scored attempt — score={finalScore}, date={daily.GameDate}, day={daily.DayNumber}");
                daily.MarkScoredAttemptComplete();

                if (StreakManager.Instance != null)
                    StreakManager.Instance.RecordScoredAttempt();

                // Store score in session
                GameSession.TodayScore = finalScore;

                if (LeaderboardService.Instance != null && MergeTracker.Instance != null)
                {
                    LeaderboardService.Instance.SubmitScore(
                        finalScore,
                        daily.GameDate,
                        daily.DayNumber,
                        MergeTracker.Instance.GetTopMergeTiers(),
                        (success) =>
                        {
                            Debug.Log($"GameManager: Score submit result: {success}");
                            if (success)
                            {
                                // Now that score is saved, fetch rank
                                LeaderboardService.Instance.FetchPlayerRankFull(
                                    daily.GameDate, (rank, total) =>
                                    {
                                        Debug.Log($"GameManager: Rank after submit: rank={rank}, total={total}");
                                        GameSession.ResultRank = rank;
                                        GameSession.ResultTotalPlayers = total;
                                        // Update the result overlay if it's showing
                                        var overlay = FindAnyObjectByType<UI.ResultOverlayScreen>();
                                        if (overlay != null) overlay.Populate();
                                    });
                            }
                        }
                    );
                }
                else
                {
                    Debug.LogWarning($"GameManager: LeaderboardService={LeaderboardService.Instance != null}, MergeTracker={MergeTracker.Instance != null}");
                }

                SyncStreakToBackend();
            }
            else
            {
                Debug.Log($"GameManager: Replay attempt — not submitting (attemptType={daily.CurrentAttemptType})");
                if (MergeTracker.Instance != null)
                    MergeTracker.Instance.IncrementReplayCount();
            }

            if (uiManager != null) uiManager.ShowGameOver(finalScore, highScore);
        }

        private void SyncStreakToBackend()
        {
            if (SupabaseClient.Instance == null || PlayerIdentity.Instance == null) return;
            if (StreakManager.Instance == null) return;

            string json = $"{{\"device_uuid\":\"{PlayerIdentity.Instance.DeviceUUID}\"," +
                           $"\"current_streak\":{StreakManager.Instance.CurrentStreak}," +
                           $"\"longest_streak\":{StreakManager.Instance.LongestStreak}}}";

            // Best-effort sync
            SupabaseClient.Instance.CallFunction("sync-streak", json, null);
        }

        public void OnPlayButtonPressed()
        {
            SetState(GameState.Playing);
        }

        public void OnRestartButtonPressed()
        {
            SetState(GameState.Playing);
        }

        public void TriggerGameOver()
        {
            if (CurrentState == GameState.Playing)
            {
                SetState(GameState.GameOver);
            }
        }

        public void TriggerShake()
        {
            if (CurrentState != GameState.Playing) return;
            if (ShakesRemaining <= 0) return;

            ShakesRemaining--;
            OnShakesChanged?.Invoke(ShakesRemaining);

            if (HapticManager.Instance != null)
                HapticManager.Instance.PlayGameOver(); // Medium haptic for shake

            StartCoroutine(ShakeCoroutine());
        }

        private System.Collections.IEnumerator ShakeCoroutine()
        {
            var balls = FindObjectsByType<BallController>(FindObjectsSortMode.None);

            foreach (var ball in balls)
            {
                if (!ball.HasLanded) continue;
                var rb = ball.GetComponent<Rigidbody2D>();
                if (rb == null) continue;

                Vector2 randomDir = new Vector2(
                    Random.Range(-1f, 1f),
                    Random.Range(0.2f, 1f)
                ).normalized;
                rb.AddForce(randomDir * shakeForce, ForceMode2D.Impulse);
            }

            var cam = Camera.main;
            if (cam != null)
            {
                Vector3 originalPos = cam.transform.position;
                float elapsed = 0f;
                while (elapsed < shakeDuration)
                {
                    elapsed += Time.deltaTime;
                    float strength = (1f - elapsed / shakeDuration) * 0.15f;
                    cam.transform.position = originalPos + (Vector3)Random.insideUnitCircle * strength;
                    yield return null;
                }
                cam.transform.position = originalPos;
            }
        }

        /// <summary>
        /// Get approximate live rank for current score.
        /// </summary>
        public int GetLiveRank()
        {
            if (scoreManager == null || LeaderboardService.Instance == null) return -1;
            return LeaderboardService.Instance.GetApproximateLiveRank(scoreManager.CurrentScore);
        }

        private void ClearAllBalls()
        {
            var balls = FindObjectsByType<BallController>(FindObjectsSortMode.None);
            foreach (var ball in balls)
            {
                Destroy(ball.gameObject);
            }
        }
    }
}
