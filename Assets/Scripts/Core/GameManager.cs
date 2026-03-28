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

    [System.Serializable]
    public class SyncStreakRequest
    {
        public string device_uuid;
        public int current_streak;
        public int longest_streak;
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

            // Ensure consistent 60fps on all devices
            Application.targetFrameRate = 60;
        }

        private void Start()
        {
            // Initialize daily seed
            if (DailySeedManager.Instance != null)
                DailySeedManager.Instance.RefreshDay();

            // Initialize session state
            GameSession.Init();
            lastKnownDate = System.DateTime.Now.ToString("yyyy-MM-dd");

            // Pre-load everything during loading screen
            StartCoroutine(PreloadAndNavigate());
        }

        /// <summary>
        /// Pre-loads all data during the loading screen so home screen appears instantly.
        /// Runs fetches in parallel, waits for all to complete (or timeout), then navigates.
        /// </summary>
        private System.Collections.IEnumerator PreloadAndNavigate()
        {
            float startTime = Time.realtimeSinceStartup;
            float minLoadTime = 1.5f;  // minimum loading screen duration
            float maxWaitTime = 5.0f;  // timeout — don't wait forever

            // If offline, skip all network waits
            if (!NetworkMonitor.QuickCheck())
            {
                GameSession.IsOffline = true;
                maxWaitTime = 0f;
                Debug.Log("[Preload] Offline — skipping network fetches");
            }

            // === Synchronous pre-loads (fast, ~1-5ms each) ===
            float t0 = Time.realtimeSinceStartup;

            // Force-load fonts
            var _ = MurgeUI.PressStart2P;
            var __ = MurgeUI.DMMono;

            Debug.Log($"[Preload] Fonts: {(Time.realtimeSinceStartup - t0) * 1000:F1}ms");

            // === Async pre-loads (network, ~100-500ms each) ===
            bool profileDone = false;
            bool leaderboardDone = false;
            bool rankDone = false;

            // 1. Player profile (~200-400ms)
            float profileStart = Time.realtimeSinceStartup;
            if (LeaderboardService.Instance != null && PlayerIdentity.Instance != null)
            {
                LeaderboardService.Instance.FetchPlayerProfile(
                    PlayerIdentity.Instance.DeviceUUID,
                    GameSession.TodayDateStr,
                    (profile) =>
                    {
                        Debug.Log($"[Preload] Profile: {(Time.realtimeSinceStartup - profileStart) * 1000:F0}ms");
                        if (profile != null)
                        {
                            GameSession.TodayScore = profile.today_score;
                            // Only override day number if server returned a valid value
                            if (profile.day_number > 0)
                                GameSession.TodayDayNumber = profile.day_number;

                            if (GameSession.CurrentPlayer == null)
                                GameSession.CurrentPlayer = new Player();
                            GameSession.CurrentPlayer.display_name = profile.display_name;
                            GameSession.CurrentPlayer.current_streak = profile.current_streak;
                            GameSession.CurrentPlayer.longest_streak = profile.longest_streak;

                            if (profile.merge_counts != null && profile.merge_counts.Length > 0)
                                GameSession.MergeCounts = profile.merge_counts;
                        }
                        profileDone = true;
                    });
            }
            else
            {
                profileDone = true;
            }

            // 2. Leaderboard for today (~200-400ms)
            float lbStart = Time.realtimeSinceStartup;
            if (LeaderboardService.Instance != null)
            {
                LeaderboardService.Instance.FetchLeaderboard(GameSession.TodayDateStr, (entries) =>
                {
                    Debug.Log($"[Preload] Leaderboard: {(Time.realtimeSinceStartup - lbStart) * 1000:F0}ms, {entries?.Count ?? 0} entries");
                    // Cached in LeaderboardService — home screen will read from cache
                    leaderboardDone = true;
                });
            }
            else
            {
                leaderboardDone = true;
            }

            // 3. Player rank (~150-300ms) — only if scored today
            float rankStart = Time.realtimeSinceStartup;
            bool hasPlayed = DailySeedManager.Instance != null && DailySeedManager.Instance.HasCompletedScoredAttempt();
            if (hasPlayed && LeaderboardService.Instance != null)
            {
                LeaderboardService.Instance.FetchPlayerRankFull(GameSession.TodayDateStr, (rank, total) =>
                {
                    Debug.Log($"[Preload] Rank: {(Time.realtimeSinceStartup - rankStart) * 1000:F0}ms, rank={rank}, total={total}");
                    if (rank > 0) GameSession.ResultRank = rank;
                    if (total > 0) GameSession.ResultTotalPlayers = total;
                    rankDone = true;
                });
            }
            else
            {
                rankDone = true;
            }

            // === Wait for all fetches (or timeout) ===
            while (!profileDone || !leaderboardDone || !rankDone)
            {
                if (Time.realtimeSinceStartup - startTime > maxWaitTime)
                {
                    Debug.LogWarning($"[Preload] Timeout after {maxWaitTime}s — proceeding with partial data");
                    break;
                }
                yield return null;
            }

            // === Pre-bake ball sprite frames (~50-200ms per tier, runs during remaining wait time) ===
            float bakeStart = Time.realtimeSinceStartup;
            // Pre-bake the most common small ball tiers (0-4) that appear in every game
            for (int tier = 0; tier < 5; tier++)
            {
                float radius = tier < 11 ? new float[] { 0.22f, 0.30f, 0.40f, 0.50f, 0.60f, 0.70f, 0.80f, 0.90f, 1.10f, 1.20f, 1.40f }[tier] : 0.5f;
                var color = Visual.BallRenderer.GetBallColor(tier);
                Visual.BallRenderer.GenerateBallPixels(tier, color, radius, 0f, out int _s);
                yield return null; // spread across frames to avoid hitch
            }
            Debug.Log($"[Preload] Ball sprites: {(Time.realtimeSinceStartup - bakeStart) * 1000:F0}ms (5 tiers)");

            // === Enforce minimum loading time ===
            float elapsed = Time.realtimeSinceStartup - startTime;
            if (elapsed < minLoadTime)
                yield return new WaitForSeconds(minLoadTime - elapsed);

            Debug.Log($"[Preload] Total: {(Time.realtimeSinceStartup - startTime) * 1000:F0}ms");

            // === Navigate ===
            if (GameSession.IsFirstLaunch && ScreenManager.Instance != null)
                ScreenManager.Instance.ShowImmediate(UI.Screen.Onboarding);
            else if (GameStateSaver.HasSavedGame())
                ResumeGame();
            else
                SetState(GameState.Menu);

            if (UI.LoadingScreen.Instance != null)
                UI.LoadingScreen.Instance.Dismiss();
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
            GameStateSaver.Clear();
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
            GameSession.MergeCounts = new int[11]; // reset merge counts for new game
            if (dropController != null) dropController.SetActive(true);
            if (uiManager != null) uiManager.ShowPlaying();

            // Show practice toast if replay
            if (GameSession.IsPractice && UI.PracticeToast.Instance != null)
                UI.PracticeToast.Instance.Show();

            // Analytics
            if (MurgeAnalytics.Instance != null)
                MurgeAnalytics.Instance.TrackPuzzleStarted();

            // Fetch initial leaderboard for live rank
            liveRankRefreshTimer = 0f;
            if (DailySeedManager.Instance != null && LeaderboardService.Instance != null)
            {
                LeaderboardService.Instance.FetchLeaderboard(DailySeedManager.Instance.GameDate);
            }
        }

        private void OnEnterGameOver()
        {
            GameStateSaver.Clear();
            if (dropController != null) dropController.SetActive(false);
            if (scoreManager != null) scoreManager.SaveHighScore();
            if (audioManager != null) audioManager.PlayGameOver();
            if (HapticManager.Instance != null) HapticManager.Instance.PlayGameOver();

            int finalScore = scoreManager != null ? scoreManager.CurrentScore : 0;
            int highScore = scoreManager != null ? scoreManager.HighScore : 0;

            // Analytics
            if (MurgeAnalytics.Instance != null)
            {
                float duration = MergeTracker.Instance != null ? MergeTracker.Instance.RoundDuration : 0f;
                MurgeAnalytics.Instance.TrackPuzzleCompleted(finalScore, duration);
            }

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
                {
                    StreakManager.Instance.RecordScoredAttempt();
                    if (MurgeAnalytics.Instance != null)
                        MurgeAnalytics.Instance.TrackStreakUpdated(StreakManager.Instance.CurrentStreak);
                }

                // Store score and merge counts in session BEFORE submitting
                GameSession.TodayScore = finalScore;
                GameSession.CaptureMergeCounts();

                if (LeaderboardService.Instance != null && MergeTracker.Instance != null)
                {
                    LeaderboardService.Instance.SubmitScore(
                        finalScore,
                        daily.GameDate,
                        daily.DayNumber,
                        GameSession.MergeCounts,
                        GameSession.LongestChain,
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

            var request = new SyncStreakRequest
            {
                device_uuid = PlayerIdentity.Instance.DeviceUUID,
                current_streak = StreakManager.Instance.CurrentStreak,
                longest_streak = StreakManager.Instance.LongestStreak
            };
            string json = JsonUtility.ToJson(request);

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

            if (MurgeAnalytics.Instance != null)
                MurgeAnalytics.Instance.TrackShakeUsed(ShakesRemaining);

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

        // ───── Save / Resume ─────

        private string lastKnownDate;
        private float pausedAtTime;
        private const float RefreshThreshold = 300f; // 5 minutes

        private void OnApplicationPause(bool paused)
        {
            if (paused)
            {
                pausedAtTime = Time.realtimeSinceStartup;
                if (CurrentState == GameState.Playing)
                    GameStateSaver.Save();
                return;
            }

            float backgroundDuration = Time.realtimeSinceStartup - pausedAtTime;
            HandleAppResume(backgroundDuration);
        }

        private void HandleAppResume(float backgroundDuration)
        {
            string today = System.DateTime.Now.ToString("yyyy-MM-dd");

            // Day rolled over while backgrounded — always handle regardless of duration
            if (!string.IsNullOrEmpty(lastKnownDate) && lastKnownDate != today)
            {
                Debug.Log($"[GameManager] Day changed: {lastKnownDate} -> {today}");

                // Clear any saved game from yesterday
                GameStateSaver.Clear();

                // Refresh daily seed
                if (DailySeedManager.Instance != null)
                    DailySeedManager.Instance.RefreshDay();

                GameSession.RefreshDay();

                // If on menu, refresh the home screen
                if (CurrentState == GameState.Menu)
                    SetState(GameState.Menu); // re-enters menu, refreshes UI

                // If mid-game, the game is now stale — trigger game over
                if (CurrentState == GameState.Playing)
                {
                    Debug.Log("[GameManager] Game stale after day rollover — ending");
                    TriggerGameOver();
                }
            }

            lastKnownDate = today;

            // Refresh data if on menu and backgrounded long enough
            if (CurrentState == GameState.Menu && backgroundDuration >= RefreshThreshold)
                RefreshMenuData();
        }

        private void RefreshMenuData()
        {
            if (DailySeedManager.Instance == null) return;
            string gameDate = DailySeedManager.Instance.GameDate;

            if (LeaderboardService.Instance != null)
                LeaderboardService.Instance.FetchLeaderboard(gameDate);

            if (PlayerIdentity.Instance != null && LeaderboardService.Instance != null)
            {
                LeaderboardService.Instance.FetchPlayerProfile(
                    PlayerIdentity.Instance.DeviceUUID, GameSession.TodayDateStr, (profile) =>
                    {
                        if (profile != null)
                        {
                            GameSession.TodayScore = profile.today_score;
                            if (profile.day_number > 0)
                                GameSession.TodayDayNumber = profile.day_number;
                        }
                    });
            }
        }

        /// <summary>Trigger a save once all balls have settled after a drop.</summary>
        public void ScheduleSaveAfterSettle()
        {
            if (CurrentState != GameState.Playing) return;
            StartCoroutine(WaitForSettleThenSave());
        }

        private System.Collections.IEnumerator WaitForSettleThenSave()
        {
            // Wait minimum time for merges to happen
            yield return new WaitForSeconds(0.6f);

            // Poll until settled
            float timeout = 5f;
            float waited = 0f;
            while (!GameStateSaver.AreAllBallsSettled() && waited < timeout)
            {
                yield return new WaitForSeconds(0.1f);
                waited += 0.1f;
            }

            if (CurrentState == GameState.Playing)
                GameStateSaver.Save();
        }

        private void ResumeGame()
        {
            var state = GameStateSaver.Load();
            if (state == null)
            {
                SetState(GameState.Menu);
                return;
            }

            Debug.Log($"[GameManager] Resuming game: score={state.currentScore}, balls={state.balls.Length}");

            CurrentState = GameState.Playing;

            // Restore sequence
            if (DailySeedManager.Instance != null)
                DailySeedManager.Instance.RestoreSequence(state.sequenceIndex, (AttemptType)state.attemptType);

            GameSession.IsPractice = state.attemptType == (int)AttemptType.Replay;

            // Restore score
            if (scoreManager != null) scoreManager.SetScore(state.currentScore);

            // Restore shakes
            ShakesRemaining = state.shakesRemaining;
            OnShakesChanged?.Invoke(ShakesRemaining);

            // Restore merge tracker
            if (MergeTracker.Instance != null)
                MergeTracker.Instance.RestoreState(
                    state.longestChain, state.totalMerges,
                    state.highestTier, state.tierCreationCounts);

            // Restore balls on screen
            if (dropController != null)
            {
                foreach (var bs in state.balls)
                {
                    GameObject newBall = Instantiate(
                        dropController.BallPrefab,
                        new Vector3(bs.x, bs.y, 0f),
                        Quaternion.Euler(0, 0, bs.rotation)
                    );

                    var controller = newBall.GetComponent<BallController>();
                    if (controller != null)
                    {
                        var ballData = GetTierConfig()?.GetTier(bs.tierIndex);
                        if (ballData != null)
                        {
                            controller.Initialize(ballData, GetTierConfig(), GetPhysicsConfig(), skipSpawnAnimation: true);
                            controller.RestoreState(bs.hasLanded, bs.timeAboveDeathLine);
                        }
                    }

                    // Start kinematic, switch to dynamic next frame
                    var rb = newBall.GetComponent<Rigidbody2D>();
                    if (rb != null)
                    {
                        rb.bodyType = RigidbodyType2D.Kinematic;
                        StartCoroutine(EnableDynamicNextFrame(rb));
                    }
                }

                // Restore drop controller with current/next ball
                var currentBall = GetTierConfig()?.GetTier(state.currentBallTier);
                var nextBall = GetTierConfig()?.GetTier(state.nextBallTier);
                if (currentBall != null && nextBall != null)
                    dropController.RestoreAndActivate(currentBall, nextBall);
            }

            if (uiManager != null) uiManager.ShowPlaying();
            // Override the score display after ShowPlaying resets it
            if (scoreManager != null) scoreManager.SetScore(state.currentScore);

            // Show practice toast if replay
            if (GameSession.IsPractice && UI.PracticeToast.Instance != null)
                UI.PracticeToast.Instance.Show();

            liveRankRefreshTimer = 0f;
            if (DailySeedManager.Instance != null && LeaderboardService.Instance != null)
                LeaderboardService.Instance.FetchLeaderboard(DailySeedManager.Instance.GameDate);
        }

        private System.Collections.IEnumerator EnableDynamicNextFrame(Rigidbody2D rb)
        {
            yield return null;
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Dynamic;
                rb.linearVelocity = Vector2.zero;
            }
        }

        private Data.BallTierConfig GetTierConfig()
        {
            if (DailySeedManager.Instance != null)
            {
                var field = typeof(DailySeedManager).GetField("tierConfig",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return field?.GetValue(DailySeedManager.Instance) as Data.BallTierConfig;
            }
            return null;
        }

        private Data.PhysicsConfig GetPhysicsConfig()
        {
            if (dropController != null)
            {
                var field = typeof(DropController).GetField("physicsConfig",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                return field?.GetValue(dropController) as Data.PhysicsConfig;
            }
            return null;
        }

        private void ClearAllBalls()
        {
            var balls = FindObjectsByType<BallController>(FindObjectsSortMode.None);
            foreach (var ball in balls)
            {
                // Disable physics and coroutines so no merges/score happen after clear
                ball.StopAllCoroutines();
                ball.enabled = false;
                var rb = ball.GetComponent<Rigidbody2D>();
                if (rb != null) rb.simulated = false;
                Destroy(ball.gameObject);
            }
        }
    }
}
