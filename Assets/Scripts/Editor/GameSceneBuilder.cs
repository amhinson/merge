using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.UI;
using TMPro;
using MergeGame.Core;
using MergeGame.Data;
using MergeGame.UI;
using MergeGame.Audio;
using MergeGame.Visual;
using MergeGame.Backend;

namespace MergeGame.Editor
{
    public static class GameSceneBuilder
    {
        // Color palette
        private static readonly Color BgColor = HexColor("121218");
        private static readonly Color PanelColor = HexColor("1E1E26");
        private static readonly Color PanelBorder = HexColor("3A3A45");
        private static readonly Color PanelHighlight = HexColor("4A4A55");
        private static readonly Color PanelShadow = HexColor("101015");
        private static readonly Color TextColor = new Color(0.92f, 0.92f, 0.95f);
        private static readonly Color MutedText = new Color(0.55f, 0.55f, 0.60f);
        private static readonly Color AccentTeal = HexColor("0D9488");

        private static readonly Color[] TierColors = new[]
        {
            HexColor("00FFE0"), // Tier 1:  Electric cyan
            HexColor("4D9FFF"), // Tier 2:  Soft blue
            HexColor("8B5CF6"), // Tier 3:  Violet
            HexColor("FF2D95"), // Tier 4:  Hot magenta
            HexColor("FF6BC2"), // Tier 5:  Neon pink
            HexColor("FF4545"), // Tier 6:  Warm red
            HexColor("FF8A2D"), // Tier 7:  Neon orange
            HexColor("FFBB33"), // Tier 8:  Warm amber
            HexColor("39FF6B"), // Tier 9:  Neon green
            HexColor("E8F0FF"), // Tier 10: White-hot
            HexColor("FFD700"), // Tier 11: Gold
        };

        // Cached sprites
        private static Sprite buttonSprite;
        private static Sprite buttonSmallSprite;

        [MenuItem("MergeGame/Build Game Scene", false, 1)]
        public static void BuildGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            var cam = Camera.main;
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.transform.position = new Vector3(0f, 0.5f, -10f);
            cam.backgroundColor = BgColor;

            // Generate button sprites
            buttonSprite = PixelUIGenerator.CreateRoundedRect(64, 24, 3, PanelColor, PanelBorder, PanelHighlight, PanelShadow);
            buttonSmallSprite = PixelUIGenerator.CreateRoundedRect(32, 32, 3, PanelColor, PanelBorder, PanelHighlight, PanelShadow);

            // Assets
            var physicsConfig = CreatePhysicsConfig();
            var tierConfig = CreateTierConfig();
            var physicsMat = CreatePhysicsMaterial(physicsConfig);
            var ballPrefab = CreateBallPrefab(physicsMat);

            // Scene objects
            CreateContainer(physicsConfig);
            CreateDeathLine(physicsConfig);
            var guideLine = CreateGuideLine();

            // Managers
            var managers = new GameObject("Managers");
            var gameManager = managers.AddComponent<GameManager>();
            var scoreManager = managers.AddComponent<ScoreManager>();
            var dropController = managers.AddComponent<DropController>();
            var audioManager = managers.AddComponent<AudioManager>();
            var dailySeedManager = managers.AddComponent<DailySeedManager>();
            managers.AddComponent<PlayerIdentity>();
            managers.AddComponent<StreakManager>();
            managers.AddComponent<MergeTracker>();
            managers.AddComponent<HapticManager>();
            managers.AddComponent<AchievementManager>();
            managers.AddComponent<SupabaseClient>();
            managers.AddComponent<LeaderboardService>();
            managers.AddComponent<MergeParticles>();

            // Debug overlay
            var debugOverlay = managers.AddComponent<DebugOverlay>();
            SetProperty(debugOverlay, "physicsConfig", physicsConfig);

            // Wire DailySeedManager
            SetProperty(dailySeedManager, "tierConfig", tierConfig);

            // Canvas
            var (canvasObj, uiManager, screenManager, nextBallAnchorRT) = CreateFullUI(tierConfig);

            // Wire GameManager
            var gmSO = new SerializedObject(gameManager);
            gmSO.FindProperty("dropController").objectReferenceValue = dropController;
            gmSO.FindProperty("scoreManager").objectReferenceValue = scoreManager;
            gmSO.FindProperty("uiManager").objectReferenceValue = uiManager;
            gmSO.FindProperty("audioManager").objectReferenceValue = audioManager;
            gmSO.ApplyModifiedPropertiesWithoutUndo();

            // Wire DropController
            var dcSO = new SerializedObject(dropController);
            dcSO.FindProperty("tierConfig").objectReferenceValue = tierConfig;
            dcSO.FindProperty("physicsConfig").objectReferenceValue = physicsConfig;
            dcSO.FindProperty("ballPrefab").objectReferenceValue = ballPrefab;
            dcSO.FindProperty("guideLine").objectReferenceValue = guideLine;
            dcSO.FindProperty("dropY").floatValue = physicsConfig.dropHeight;
            dcSO.FindProperty("leftWallX").floatValue = -physicsConfig.containerWidth / 2f;
            dcSO.FindProperty("rightWallX").floatValue = physicsConfig.containerWidth / 2f;
            dcSO.FindProperty("cooldownDuration").floatValue = physicsConfig.cooldownDuration;
            dcSO.FindProperty("previewScale").floatValue = 0.6f;
            dcSO.FindProperty("nextBallAnchor").objectReferenceValue = nextBallAnchorRT;
            dcSO.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.SaveScene(scene, "Assets/Scenes/GameScene.unity");
            AssetDatabase.Refresh();
            Debug.Log("MergeGame: Game scene built! Press Play to test.");
        }

        // ================================================================
        // FULL UI CREATION
        // ================================================================

        private static (GameObject, UIManager, ScreenManager, RectTransform) CreateFullUI(BallTierConfig tierConfig)
        {
            // Canvas
            GameObject canvasObj = new GameObject("Canvas");
            var canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;
            var scaler = canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // Safe area root
            GameObject safeArea = new GameObject("SafeArea");
            safeArea.transform.SetParent(canvasObj.transform, false);
            var safeRT = safeArea.AddComponent<RectTransform>();
            safeRT.anchorMin = Vector2.zero;
            safeRT.anchorMax = Vector2.one;
            safeRT.offsetMin = Vector2.zero;
            safeRT.offsetMax = Vector2.zero;
            safeArea.AddComponent<SafeAreaHandler>();

            // ===== TITLE SCREEN =====
            var (titlePanel, titleScreen) = CreateTitleScreenPanel(safeArea.transform);

            // ===== GAMEPLAY SCREEN =====
            var (gameplayPanel, scoreText, shakeButton, shakeCountTMP,
                nextBallImg, miniLB, scoreTickUp, nextBallPreviewUI, nextBallAnchorRT) = CreateGameplayPanel(safeArea.transform);

            // ===== RESULTS SCREEN =====
            var (resultsPanel, resultsScreen) = CreateResultsScreenPanel(safeArea.transform, tierConfig);

            // ===== LEADERBOARD SCREEN =====
            var (lbPanel, lbBackBtn) = CreateLeaderboardPanel(safeArea.transform);

            // ===== SETTINGS SCREEN =====
            var (settingsPanel, settingsScreen) = CreateSettingsPanel(safeArea.transform);

            // Add CanvasGroups for transitions
            var titleCG = titlePanel.AddComponent<CanvasGroup>();
            var gameplayCG = gameplayPanel.AddComponent<CanvasGroup>();
            var resultsCG = resultsPanel.AddComponent<CanvasGroup>();
            var lbCG = lbPanel.AddComponent<CanvasGroup>();
            var settingsCG = settingsPanel.AddComponent<CanvasGroup>();

            // Hide all except title
            gameplayPanel.SetActive(false);
            resultsPanel.SetActive(false);
            lbPanel.SetActive(false);
            settingsPanel.SetActive(false);

            // ScreenManager
            var screenManager = canvasObj.AddComponent<ScreenManager>();
            var smSO = new SerializedObject(screenManager);
            smSO.FindProperty("titleScreen").objectReferenceValue = titleCG;
            smSO.FindProperty("gameplayScreen").objectReferenceValue = gameplayCG;
            smSO.FindProperty("resultsScreen").objectReferenceValue = resultsCG;
            smSO.FindProperty("leaderboardScreen").objectReferenceValue = lbCG;
            smSO.FindProperty("settingsScreen").objectReferenceValue = settingsCG;
            smSO.ApplyModifiedPropertiesWithoutUndo();

            // UIManager
            var uiManager = canvasObj.AddComponent<UIManager>();
            var uiSO = new SerializedObject(uiManager);
            uiSO.FindProperty("menuPanel").objectReferenceValue = titlePanel;
            uiSO.FindProperty("playingPanel").objectReferenceValue = gameplayPanel;
            uiSO.FindProperty("gameOverPanel").objectReferenceValue = resultsPanel;
            uiSO.FindProperty("titleScreen").objectReferenceValue = titleScreen;
            uiSO.FindProperty("resultsScreen").objectReferenceValue = resultsScreen;
            uiSO.FindProperty("settingsScreen").objectReferenceValue = settingsScreen;
            uiSO.FindProperty("scoreText").objectReferenceValue = scoreText;
            uiSO.FindProperty("shakeButton").objectReferenceValue = shakeButton;
            uiSO.FindProperty("shakeCountText").objectReferenceValue = shakeCountTMP;
            uiSO.FindProperty("nextBallPreview").objectReferenceValue = nextBallImg;
            uiSO.FindProperty("nextBallPreviewUI").objectReferenceValue = nextBallPreviewUI;
            uiSO.FindProperty("miniLeaderboard").objectReferenceValue = miniLB;
            uiSO.FindProperty("scoreTickUp").objectReferenceValue = scoreTickUp;
            uiSO.FindProperty("playButton").objectReferenceValue = titleScreen.PlayButton;
            uiSO.FindProperty("restartButton").objectReferenceValue = resultsScreen.PlayAgainButton;
            uiSO.FindProperty("leaderboardBackButton").objectReferenceValue = lbBackBtn;
            uiSO.FindProperty("leaderboardUI").objectReferenceValue = lbPanel.GetComponent<LeaderboardUI>();
            uiSO.FindProperty("tierConfig").objectReferenceValue = tierConfig;
            uiSO.ApplyModifiedPropertiesWithoutUndo();

            return (canvasObj, uiManager, screenManager, nextBallAnchorRT);
        }

        // ===== TITLE SCREEN =====

        private static (GameObject, TitleScreen) CreateTitleScreenPanel(Transform parent)
        {
            var panel = CreateFullPanel(parent, "TitleScreen");
            var bg = panel.AddComponent<Image>();
            bg.color = BgColor;

            // Title
            var title = CreateText(panel.transform, "Title", "OVERTONE", 64, TextColor);
            SetAnchors(title.rectTransform, 0.05f, 0.72f, 0.95f, 0.82f);

            // Tagline (directly below title)
            var tagline = CreateText(panel.transform, "Tagline", "a daily drop", 20, MutedText);
            SetAnchors(tagline.rectTransform, 0.2f, 0.68f, 0.8f, 0.73f);

            // Day number
            var dayText = CreateText(panel.transform, "DayText", "Day #1", 28, MutedText);
            SetAnchors(dayText.rectTransform, 0.2f, 0.62f, 0.8f, 0.68f);

            // Decorative ball (RawImage — rendered via offscreen camera)
            var ballImgObj = new GameObject("DecorativeBall");
            ballImgObj.transform.SetParent(panel.transform, false);
            var ballImgRT = ballImgObj.AddComponent<RectTransform>();
            SetAnchors(ballImgRT, 0.38f, 0.52f, 0.62f, 0.62f);
            var decorBallImg = ballImgObj.AddComponent<UnityEngine.UI.RawImage>();
            decorBallImg.color = Color.white;

            // Streak text
            var streakText = CreateText(panel.transform, "StreakText", "", 26, new Color(1f, 0.7f, 0.3f));
            SetAnchors(streakText.rectTransform, 0.15f, 0.46f, 0.85f, 0.52f);

            // Play button
            var playBtn = CreateStyledButton(panel.transform, "PlayButton", "PLAY", 36, 0.25f, 0.35f, 0.75f, 0.45f);
            var playBtnLabel = playBtn.GetComponentInChildren<TextMeshProUGUI>();

            // Leaderboard icon (top left)
            var lbBtn = CreateIconButton(panel.transform, "LeaderboardBtn", 0.02f, 0.90f, 0.14f, 0.98f);

            // Settings icon (top right)
            var settingsBtn = CreateIconButton(panel.transform, "SettingsBtn", 0.86f, 0.90f, 0.98f, 0.98f);

            // Wire TitleScreen
            var tierConfig = AssetDatabase.LoadAssetAtPath<BallTierConfig>("Assets/ScriptableObjects/BallTierConfig.asset");
            var titleScreen = panel.AddComponent<TitleScreen>();
            var so = new SerializedObject(titleScreen);
            so.FindProperty("titleText").objectReferenceValue = title;
            so.FindProperty("dayText").objectReferenceValue = dayText;
            so.FindProperty("streakText").objectReferenceValue = streakText;
            so.FindProperty("decorativeBallImage").objectReferenceValue = decorBallImg;
            so.FindProperty("tierConfig").objectReferenceValue = tierConfig;
            so.FindProperty("playButton").objectReferenceValue = playBtn.GetComponent<Button>();
            so.FindProperty("playButtonLabel").objectReferenceValue = playBtnLabel;
            so.FindProperty("leaderboardButton").objectReferenceValue = lbBtn.GetComponent<Button>();
            so.FindProperty("settingsButton").objectReferenceValue = settingsBtn.GetComponent<Button>();
            so.ApplyModifiedPropertiesWithoutUndo();

            return (panel, titleScreen);
        }

        // ===== GAMEPLAY SCREEN =====

        private static (GameObject, TextMeshProUGUI, Button, TextMeshProUGUI,
            Image, MiniLeaderboardUI, ScoreTickUp, NextBallPreviewUI, RectTransform)
            CreateGameplayPanel(Transform parent)
        {
            var panel = CreateFullPanel(parent, "GameplayScreen");
            // No background — gameplay shows through

            // Score (top center, large)
            var scoreText = CreateText(panel.transform, "Score", "0", 72, TextColor);
            SetAnchors(scoreText.rectTransform, 0.15f, 0.88f, 0.85f, 0.96f);

            // ScoreTickUp
            var scoreTickUp = scoreText.gameObject.AddComponent<ScoreTickUp>();
            SetProperty(scoreTickUp, "scoreText", scoreText);

            // "Next" label + anchor point for the real ball object
            var nextLabel = CreateText(panel.transform, "NextLabel", "Next", 18, MutedText);
            SetAnchors(nextLabel.rectTransform, 0.82f, 0.94f, 0.98f, 0.98f);

            // Anchor point — DropController converts this UI position to world space
            var nextAnchor = new GameObject("NextBallAnchor");
            nextAnchor.transform.SetParent(panel.transform, false);
            var anchorRT = nextAnchor.AddComponent<RectTransform>();
            SetAnchors(anchorRT, 0.88f, 0.92f, 0.92f, 0.96f);

            Image nextImg = null;
            NextBallPreviewUI nextBallUI = null;

            // Mini leaderboard (top left)
            var mlbObj = new GameObject("MiniLeaderboard");
            mlbObj.transform.SetParent(panel.transform, false);
            var mlbRT = mlbObj.AddComponent<RectTransform>();
            SetAnchors(mlbRT, 0.02f, 0.82f, 0.42f, 0.97f);

            var topPlayerText = CreateText(mlbObj.transform, "TopPlayer", "", 18, MutedText);
            topPlayerText.alignment = TextAlignmentOptions.TopLeft;
            topPlayerText.overflowMode = TextOverflowModes.Truncate;
            SetAnchors(topPlayerText.rectTransform, 0, 0.45f, 1, 1);

            var playerRow = CreateText(mlbObj.transform, "PlayerRow", "", 18, new Color(1f, 0.85f, 0.3f));
            playerRow.alignment = TextAlignmentOptions.TopLeft;
            playerRow.overflowMode = TextOverflowModes.Truncate;
            SetAnchors(playerRow.rectTransform, 0, 0, 1, 0.45f);

            var miniLB = mlbObj.AddComponent<MiniLeaderboardUI>();
            var mlbSO = new SerializedObject(miniLB);
            mlbSO.FindProperty("topPlayerText").objectReferenceValue = topPlayerText;
            mlbSO.FindProperty("playerRow").objectReferenceValue = playerRow;
            mlbSO.ApplyModifiedPropertiesWithoutUndo();

            // Shake button (bottom left, inline count)
            var shakeBtn = CreateStyledButton(panel.transform, "ShakeButton", "SHAKE x3", 24, 0.03f, 0.01f, 0.30f, 0.06f);
            var shakeTMP = shakeBtn.GetComponentInChildren<TextMeshProUGUI>();

            // Exit button (bottom right)
            var exitBtn = CreateIconButton(panel.transform, "ExitBtn", 0.88f, 0.01f, 0.98f, 0.06f);
            var exitIcon = exitBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (exitIcon != null) { exitIcon.text = "X"; exitIcon.fontSize = 20; }

            // Confirmation dialog (hidden by default)
            var confirmPanel = new GameObject("ExitConfirm");
            confirmPanel.transform.SetParent(panel.transform, false);
            var cpRT = confirmPanel.AddComponent<RectTransform>();
            SetAnchors(cpRT, 0, 0, 1, 1);
            var cpBg = confirmPanel.AddComponent<Image>();
            cpBg.color = new Color(0.05f, 0.05f, 0.08f, 0.85f);

            var confirmText = CreateText(confirmPanel.transform, "ConfirmText", "Quit game?", 32, TextColor);
            SetAnchors(confirmText.rectTransform, 0.1f, 0.55f, 0.9f, 0.65f);

            var yesBtn = CreateStyledButton(confirmPanel.transform, "YesBtn", "QUIT", 28, 0.15f, 0.38f, 0.48f, 0.50f);
            var noBtn = CreateStyledButton(confirmPanel.transform, "NoBtn", "CANCEL", 28, 0.52f, 0.38f, 0.85f, 0.50f);

            confirmPanel.SetActive(false);

            // Wire exit confirmation logic via a helper component
            var exitHelper = panel.AddComponent<ExitConfirmUI>();
            var ehSO = new SerializedObject(exitHelper);
            ehSO.FindProperty("exitButton").objectReferenceValue = exitBtn.GetComponent<Button>();
            ehSO.FindProperty("confirmPanel").objectReferenceValue = confirmPanel;
            ehSO.FindProperty("yesButton").objectReferenceValue = yesBtn.GetComponent<Button>();
            ehSO.FindProperty("noButton").objectReferenceValue = noBtn.GetComponent<Button>();
            ehSO.ApplyModifiedPropertiesWithoutUndo();

            return (panel, scoreText, shakeBtn.GetComponent<Button>(), shakeTMP,
                nextImg, miniLB, scoreTickUp, nextBallUI, anchorRT);
        }

        // ===== RESULTS SCREEN =====

        private static (GameObject, ResultsScreen) CreateResultsScreenPanel(Transform parent, BallTierConfig tierConfig)
        {
            var panel = CreateFullPanel(parent, "ResultsScreen");
            var bg = panel.AddComponent<Image>();
            bg.color = new Color(0.07f, 0.07f, 0.09f, 0.88f);

            // Home button (top left)
            var homeBtn = CreateIconButton(panel.transform, "HomeBtn", 0.02f, 0.90f, 0.14f, 0.98f);

            // Day label
            var dayLabel = CreateText(panel.transform, "DayLabel", "Overtone #1", 24, MutedText);
            SetAnchors(dayLabel.rectTransform, 0.1f, 0.78f, 0.9f, 0.83f);

            // Score (large, with tick-up)
            var scoreLabel = CreateText(panel.transform, "ScoreLabel", "0", 80, TextColor);
            SetAnchors(scoreLabel.rectTransform, 0.1f, 0.65f, 0.9f, 0.78f);
            var scoreTickUp = scoreLabel.gameObject.AddComponent<ScoreTickUp>();
            SetProperty(scoreTickUp, "scoreText", scoreLabel);

            // Streak
            var streakText = CreateText(panel.transform, "StreakText", "", 22, new Color(1f, 0.7f, 0.3f));
            SetAnchors(streakText.rectTransform, 0.2f, 0.60f, 0.8f, 0.65f);

            // Merge icons container
            var mergesContainer = new GameObject("MergeIcons");
            mergesContainer.transform.SetParent(panel.transform, false);
            var mcRT = mergesContainer.AddComponent<RectTransform>();
            SetAnchors(mcRT, 0.05f, 0.42f, 0.95f, 0.60f);
            // GridLayoutGroup wraps into two rows
            var glg = mergesContainer.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(75, 85);
            glg.spacing = new Vector2(8, 5);
            glg.childAlignment = TextAnchor.UpperCenter;
            glg.constraint = GridLayoutGroup.Constraint.Flexible;

            // Rank text
            var rankText = CreateText(panel.transform, "RankText", "", 28, AccentTeal);
            SetAnchors(rankText.rectTransform, 0.2f, 0.43f, 0.8f, 0.48f);

            // Replay label
            var replayLabel = CreateText(panel.transform, "ReplayLabel", "Practice Run", 20, MutedText);
            SetAnchors(replayLabel.rectTransform, 0.3f, 0.43f, 0.7f, 0.47f);
            replayLabel.gameObject.SetActive(false);

            // Share button
            var shareBtn = CreateStyledButton(panel.transform, "ShareButton", "SHARE", 32, 0.20f, 0.30f, 0.80f, 0.40f);

            // Play Again button
            var playAgainBtn = CreateStyledButton(panel.transform, "PlayAgainButton", "PLAY AGAIN", 28, 0.25f, 0.18f, 0.75f, 0.28f);

            // ResultsScreen component
            var resultsScreen = panel.AddComponent<ResultsScreen>();
            var rsSO = new SerializedObject(resultsScreen);
            rsSO.FindProperty("dayLabel").objectReferenceValue = dayLabel;
            rsSO.FindProperty("scoreTickUp").objectReferenceValue = scoreTickUp;
            rsSO.FindProperty("streakText").objectReferenceValue = streakText;
            rsSO.FindProperty("mergeIconsContainer").objectReferenceValue = mergesContainer.transform;
            rsSO.FindProperty("tierConfig").objectReferenceValue = tierConfig;
            rsSO.FindProperty("rankText").objectReferenceValue = rankText;
            rsSO.FindProperty("replayLabel").objectReferenceValue = replayLabel;
            rsSO.FindProperty("shareButton").objectReferenceValue = shareBtn.GetComponent<Button>();
            rsSO.FindProperty("playAgainButton").objectReferenceValue = playAgainBtn.GetComponent<Button>();
            rsSO.FindProperty("homeButton").objectReferenceValue = homeBtn.GetComponent<Button>();
            rsSO.ApplyModifiedPropertiesWithoutUndo();

            return (panel, resultsScreen);
        }

        // ===== LEADERBOARD SCREEN =====

        private static (GameObject, Button) CreateLeaderboardPanel(Transform parent)
        {
            var panel = CreateFullPanel(parent, "LeaderboardScreen");
            var bg = panel.AddComponent<Image>();
            bg.color = BgColor;

            // Back button
            var backBtn = CreateIconButton(panel.transform, "BackBtn", 0.02f, 0.90f, 0.14f, 0.98f);

            // Title
            var title = CreateText(panel.transform, "LBTitle", "Leaderboard", 36, TextColor);
            SetAnchors(title.rectTransform, 0.15f, 0.90f, 0.85f, 0.96f);

            var dateText = CreateText(panel.transform, "LBDate", "Day #1", 20, MutedText);
            SetAnchors(dateText.rectTransform, 0.2f, 0.87f, 0.8f, 0.91f);

            // Content area (simple panel with vertical layout — no ScrollRect)
            var content = new GameObject("Content");
            content.transform.SetParent(panel.transform, false);
            var contentRT = content.AddComponent<RectTransform>();
            SetAnchors(contentRT, 0.03f, 0.05f, 0.97f, 0.86f);
            var vlg = content.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 6;
            vlg.padding = new RectOffset(0, 0, 0, 0);
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;

            // Empty state is handled by LeaderboardUI.Populate()

            // Wire LeaderboardUI component
            var lbUI = panel.AddComponent<LeaderboardUI>();
            var lbSO = new SerializedObject(lbUI);
            lbSO.FindProperty("entriesContainer").objectReferenceValue = content.transform;
            lbSO.FindProperty("titleText").objectReferenceValue = title;
            lbSO.ApplyModifiedPropertiesWithoutUndo();

            return (panel, backBtn.GetComponent<Button>());
        }

        // ===== SETTINGS SCREEN =====

        private static (GameObject, SettingsScreen) CreateSettingsPanel(Transform parent)
        {
            var panel = CreateFullPanel(parent, "SettingsScreen");
            var bg = panel.AddComponent<Image>();
            bg.color = BgColor;

            // Back button
            var backBtn = CreateIconButton(panel.transform, "BackBtn", 0.02f, 0.90f, 0.14f, 0.98f);

            // Title — above the card
            var settingsTitle = CreateText(panel.transform, "SettingsTitle", "Settings", 40, TextColor);
            SetAnchors(settingsTitle.rectTransform, 0.1f, 0.82f, 0.9f, 0.90f);

            // Settings card — simple dark panel, no sprite (avoids border artifacts)
            var card = new GameObject("SettingsCard");
            card.transform.SetParent(panel.transform, false);
            var cardRT = card.AddComponent<RectTransform>();
            SetAnchors(cardRT, 0.08f, 0.45f, 0.92f, 0.80f);
            var cardImg = card.AddComponent<Image>();
            cardImg.color = PanelColor;

            // === Name row (top half of card) ===
            var nameLabel = CreateText(card.transform, "NameLabel", "Name", 22, MutedText);
            SetAnchors(nameLabel.rectTransform, 0.05f, 0.70f, 0.25f, 0.92f);
            nameLabel.alignment = TextAlignmentOptions.Left;

            // Name input field
            var nameInputObj = new GameObject("NameInput");
            nameInputObj.transform.SetParent(card.transform, false);
            var niRT = nameInputObj.AddComponent<RectTransform>();
            SetAnchors(niRT, 0.28f, 0.70f, 0.72f, 0.92f);
            var niBg = nameInputObj.AddComponent<Image>();
            niBg.color = new Color(0.1f, 0.1f, 0.14f);

            var nameInput = nameInputObj.AddComponent<TMP_InputField>();
            nameInput.characterLimit = 16;

            // Input text area (with padding)
            var textArea = new GameObject("TextArea");
            textArea.transform.SetParent(nameInputObj.transform, false);
            var taRT = textArea.AddComponent<RectTransform>();
            SetAnchors(taRT, 0, 0, 1, 1);
            taRT.offsetMin = new Vector2(10, 0);
            taRT.offsetMax = new Vector2(-10, 0);

            var niText = CreateText(textArea.transform, "Text", "Player", 20, TextColor);
            SetAnchors(niText.rectTransform, 0, 0, 1, 1);
            niText.alignment = TextAlignmentOptions.Left;
            nameInput.textComponent = niText;
            nameInput.textViewport = taRT;

            var phText = CreateText(textArea.transform, "Placeholder", "Enter name...", 20, MutedText);
            SetAnchors(phText.rectTransform, 0, 0, 1, 1);
            phText.alignment = TextAlignmentOptions.Left;
            nameInput.placeholder = phText;

            // Save button
            var saveBtn = CreateStyledButton(card.transform, "SaveName", "SAVE", 18, 0.75f, 0.72f, 0.95f, 0.90f);

            // Name error text
            var nameError = CreateText(card.transform, "NameError", "", 16, new Color(1f, 0.4f, 0.4f));
            SetAnchors(nameError.rectTransform, 0.05f, 0.55f, 0.95f, 0.68f);

            // === Haptics row (bottom half of card) ===
            var hapLabel = CreateText(card.transform, "HapticsLabel", "Haptics", 22, MutedText);
            SetAnchors(hapLabel.rectTransform, 0.05f, 0.10f, 0.50f, 0.40f);
            hapLabel.alignment = TextAlignmentOptions.Left;

            // Haptics toggle — simple tappable toggle (no Button component, just Toggle)
            var toggleObj = new GameObject("HapticsToggle");
            toggleObj.transform.SetParent(card.transform, false);
            var togRT = toggleObj.AddComponent<RectTransform>();
            SetAnchors(togRT, 0.75f, 0.15f, 0.95f, 0.38f);

            var togImg = toggleObj.AddComponent<Image>();
            togImg.color = AccentTeal;

            var toggle = toggleObj.AddComponent<Toggle>();
            toggle.isOn = true;
            toggle.targetGraphic = togImg;

            // Label inside the toggle
            var togLabel = CreateText(toggleObj.transform, "Label", "ON", 18, TextColor);
            SetAnchors(togLabel.rectTransform, 0, 0, 1, 1);

            // SettingsScreen component
            var settingsScreen = panel.AddComponent<SettingsScreen>();
            var ssSO = new SerializedObject(settingsScreen);
            ssSO.FindProperty("nameInput").objectReferenceValue = nameInput;
            ssSO.FindProperty("saveNameButton").objectReferenceValue = saveBtn.GetComponent<Button>();
            ssSO.FindProperty("nameErrorText").objectReferenceValue = nameError;
            ssSO.FindProperty("hapticToggle").objectReferenceValue = toggle;
            ssSO.FindProperty("backButton").objectReferenceValue = backBtn.GetComponent<Button>();
            ssSO.ApplyModifiedPropertiesWithoutUndo();

            return (panel, settingsScreen);
        }

        // ================================================================
        // UI HELPERS
        // ================================================================

        private static GameObject CreateFullPanel(Transform parent, string name)
        {
            var panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rt = panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return panel;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text,
            float fontSize, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.enableAutoSizing = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }

        private static GameObject CreateStyledButton(Transform parent, string name, string label,
            float fontSize, float xMin, float yMin, float xMax, float yMax)
        {
            var btn = new GameObject(name);
            btn.transform.SetParent(parent, false);
            var rt = btn.AddComponent<RectTransform>();
            SetAnchors(rt, xMin, yMin, xMax, yMax);

            var img = btn.AddComponent<Image>();
            img.color = PanelColor;
            if (buttonSprite != null)
            {
                img.sprite = buttonSprite;
                img.type = Image.Type.Sliced;
            }

            var button = btn.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.1f, 1.1f, 1.1f);
            colors.pressedColor = new Color(0.85f, 0.85f, 0.85f);
            colors.disabledColor = new Color(0.4f, 0.4f, 0.4f);
            button.colors = colors;

            btn.AddComponent<PixelButton>();

            var textObj = new GameObject("Label");
            textObj.transform.SetParent(btn.transform, false);
            var textRT = textObj.AddComponent<RectTransform>();
            textRT.anchorMin = Vector2.zero;
            textRT.anchorMax = Vector2.one;
            textRT.offsetMin = Vector2.zero;
            textRT.offsetMax = Vector2.zero;

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.color = TextColor;
            tmp.alignment = TextAlignmentOptions.Center;

            return btn;
        }

        private static GameObject CreateIconButton(Transform parent, string name,
            float xMin, float yMin, float xMax, float yMax)
        {
            var btn = new GameObject(name);
            btn.transform.SetParent(parent, false);
            var rt = btn.AddComponent<RectTransform>();
            SetAnchors(rt, xMin, yMin, xMax, yMax);

            // Hit area — very low alpha but not zero (zero alpha blocks raycasts)
            var img = btn.AddComponent<Image>();
            img.color = new Color(0, 0, 0, 0.01f);

            var button = btn.AddComponent<Button>();
            btn.AddComponent<PixelButton>();

            // Icon label (using text as placeholder — replace with icon sprites later)
            // Use ASCII-safe characters for icons (compatible with Press Start 2P)
            string iconChar = name.Contains("Leaderboard") ? "#" :
                              name.Contains("Settings") ? "*" :
                              name.Contains("Home") ? "<" :
                              name.Contains("Back") ? "X" : "o";
            var iconText = CreateText(btn.transform, "Icon", iconChar, 40, MutedText);
            SetAnchors(iconText.rectTransform, 0, 0, 1, 1);

            return btn;
        }

        private static void SetAnchors(RectTransform rt, float xMin, float yMin, float xMax, float yMax)
        {
            rt.anchorMin = new Vector2(xMin, yMin);
            rt.anchorMax = new Vector2(xMax, yMax);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        private static void SetProperty(Object target, string propName, Object value)
        {
            var so = new SerializedObject(target);
            var prop = so.FindProperty(propName);
            if (prop != null)
            {
                prop.objectReferenceValue = value;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        // ================================================================
        // ASSET CREATION (unchanged logic, condensed)
        // ================================================================

        [MenuItem("MergeGame/Create All Assets", false, 0)]
        public static void CreateAllAssets()
        {
            CreatePhysicsConfig();
            CreateTierConfig();
            var p = CreatePhysicsConfig();
            CreateBallPrefab(CreatePhysicsMaterial(p));
            AssetDatabase.Refresh();
            Debug.Log("MergeGame: All assets created!");
        }

        private static PhysicsConfig CreatePhysicsConfig()
        {
            EnsureDirectory("Assets/ScriptableObjects");
            string path = "Assets/ScriptableObjects/PhysicsConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<PhysicsConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<PhysicsConfig>();
                AssetDatabase.CreateAsset(config, path);
            }
            // Ensure container fits on screen
            config.containerWidth = 4.5f;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        private static BallTierConfig CreateTierConfig()
        {
            EnsureDirectory("Assets/ScriptableObjects");
            EnsureDirectory("Assets/Sprites");

            var tierDefs = new (int idx, float radius, int points, string name)[]
            {
                (0, 0.22f, 1, "Cherry"),      // merge two tier 1 → 1 pt
                (1, 0.30f, 2, "Strawberry"),  // → 2 pts
                (2, 0.40f, 3, "Grape"),       // → 3 pts
                (3, 0.50f, 5, "Dekopon"),     // → 5 pts
                (4, 0.60f, 7, "Orange"),      // → 7 pts
                (5, 0.70f, 10, "Apple"),      // → 10 pts
                (6, 0.80f, 15, "Pear"),       // → 15 pts
                (7, 0.90f, 20, "Peach"),      // → 20 pts
                (8, 1.10f, 30, "Pineapple"),  // → 30 pts
                (9, 1.20f, 40, "Melon"),      // → 40 pts
                (10, 1.40f, 50, "Watermelon"),// → 50 pts
            };

            BallData[] ballDatas = new BallData[tierDefs.Length];
            for (int i = 0; i < tierDefs.Length; i++)
            {
                var def = tierDefs[i];
                string spritePath = $"Assets/Sprites/Ball_Tier{i + 1}.png";
                byte[] png = NeonBallRenderer.GenerateBallPNG(i, TierColors[i], def.radius, 0f);
                System.IO.File.WriteAllBytes(spritePath, png);

                string dataPath = $"Assets/ScriptableObjects/BallData_Tier{i + 1}.asset";
                BallData data = AssetDatabase.LoadAssetAtPath<BallData>(dataPath);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<BallData>();
                    AssetDatabase.CreateAsset(data, dataPath);
                }
                data.tierIndex = def.idx;
                data.radius = def.radius;
                data.color = TierColors[i];
                data.pointValue = def.points;
                data.displayName = def.name;
                EditorUtility.SetDirty(data);
                ballDatas[i] = data;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            for (int i = 0; i < tierDefs.Length; i++)
            {
                string spritePath = $"Assets/Sprites/Ball_Tier{i + 1}.png";
                TextureImporter imp = (TextureImporter)AssetImporter.GetAtPath(spritePath);
                if (imp != null)
                {
                    imp.textureType = TextureImporterType.Sprite;
                    imp.spritePixelsPerUnit = 48;
                    imp.filterMode = FilterMode.Point;
                    imp.textureCompression = TextureImporterCompression.Uncompressed;
                    imp.SaveAndReimport();
                }
                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite != null)
                {
                    ballDatas[i].sprite = sprite;
                    EditorUtility.SetDirty(ballDatas[i]);
                }
            }

            string configPath = "Assets/ScriptableObjects/BallTierConfig.asset";
            var config = AssetDatabase.LoadAssetAtPath<BallTierConfig>(configPath);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<BallTierConfig>();
                AssetDatabase.CreateAsset(config, configPath);
            }
            config.tiers = ballDatas;
            config.maxDropTier = 5;
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        private static PhysicsMaterial2D CreatePhysicsMaterial(PhysicsConfig config)
        {
            EnsureDirectory("Assets/Materials");
            string path = "Assets/Materials/BallPhysicsMaterial.asset";
            var mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
            if (mat == null)
            {
                mat = new PhysicsMaterial2D("BallPhysicsMaterial");
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.bounciness = config.baseBounciness;
            mat.friction = config.baseFriction;
            EditorUtility.SetDirty(mat);
            AssetDatabase.SaveAssets();
            return mat;
        }

        private static GameObject CreateBallPrefab(PhysicsMaterial2D physicsMat)
        {
            string prefabPath = "Assets/Prefabs/Ball.prefab";
            EnsureDirectory("Assets/Prefabs");

            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existing != null) AssetDatabase.DeleteAsset(prefabPath);

            Sprite defaultSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ball_Tier1.png");

            var ball = new GameObject("Ball");
            var sr = ball.AddComponent<SpriteRenderer>();
            sr.sprite = defaultSprite;
            sr.color = Color.white;

            var rb = ball.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            rb.mass = 1f;
            rb.linearDamping = 0.1f;
            rb.angularDamping = 0.05f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = ball.AddComponent<CircleCollider2D>();
            col.radius = 0.5f; // Overridden per-ball by BallController.ApplySize()
            if (physicsMat != null) col.sharedMaterial = physicsMat;

            var controller = ball.AddComponent<BallController>();

            // Hidden tier label (kept for debug)
            var labelObj = new GameObject("TierLabel");
            labelObj.transform.SetParent(ball.transform);
            labelObj.transform.localPosition = Vector3.zero;
            labelObj.transform.localScale = Vector3.one;
            var tmp = labelObj.AddComponent<TextMeshPro>();
            tmp.text = "";
            tmp.fontSize = 4;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.sortingOrder = 1;
            labelObj.SetActive(false);

            var so = new SerializedObject(controller);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("tierLabel").objectReferenceValue = tmp;
            so.ApplyModifiedPropertiesWithoutUndo();

            var prefab = PrefabUtility.SaveAsPrefabAsset(ball, prefabPath);
            Object.DestroyImmediate(ball);
            return prefab;
        }

        private static void CreateContainer(PhysicsConfig config)
        {
            var container = new GameObject("Container");
            var setup = container.AddComponent<ContainerSetup>();
            SetProperty(setup, "physicsConfig", config);
        }

        private static void CreateDeathLine(PhysicsConfig config)
        {
            float deathY = config.deathLineY;
            var obj = new GameObject("DeathLine");
            obj.transform.position = new Vector3(0f, deathY, 0f);

            var col = obj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(6f, 5f);
            col.offset = new Vector2(0f, 2.5f);

            obj.AddComponent<DeathLine>();
            obj.AddComponent<DeathLinePulse>();

            // Dashed line — multiple short segments
            var lineVis = new GameObject("DeathLineVisual");
            lineVis.transform.SetParent(obj.transform);
            lineVis.transform.localPosition = Vector3.zero;

            float lineLeft = -2.6f;
            float lineRight = 2.6f;
            float dashLen = 0.2f;
            float gapLen = 0.15f;

            var lr = lineVis.AddComponent<LineRenderer>();
            var positions = new System.Collections.Generic.List<Vector3>();
            float x = lineLeft;
            while (x < lineRight)
            {
                float dashEnd = Mathf.Min(x + dashLen, lineRight);
                positions.Add(new Vector3(x, deathY, 0f));
                positions.Add(new Vector3(dashEnd, deathY, 0f));
                // Add a zero-width break for the gap
                float gapEnd = dashEnd + gapLen;
                if (gapEnd < lineRight)
                {
                    positions.Add(new Vector3(dashEnd, deathY, 0f));
                    positions.Add(new Vector3(gapEnd, deathY, 0f));
                }
                x = gapEnd;
            }

            // Use individual line segments for true dashes
            // Simpler approach: use a single LineRenderer with alternating visible/invisible points
            // But LineRenderer doesn't support gaps — use multiple child LineRenderers instead
            Object.DestroyImmediate(lr);

            float cx = lineLeft;
            int dashIdx = 0;
            while (cx < lineRight)
            {
                float dashEnd = Mathf.Min(cx + dashLen, lineRight);
                var dashObj = new GameObject($"Dash{dashIdx}");
                dashObj.transform.SetParent(lineVis.transform);
                var dashLR = dashObj.AddComponent<LineRenderer>();
                dashLR.positionCount = 2;
                dashLR.SetPosition(0, new Vector3(cx, deathY, 0f));
                dashLR.SetPosition(1, new Vector3(dashEnd, deathY, 0f));
                dashLR.startWidth = 0.03f;
                dashLR.endWidth = 0.03f;
                dashLR.material = new Material(Shader.Find("Sprites/Default"));
                dashLR.startColor = new Color(1f, 0.6f, 0.3f, 0f);
                dashLR.endColor = new Color(1f, 0.6f, 0.3f, 0f);
                dashLR.sortingOrder = 5;
                dashLR.useWorldSpace = true;
                cx = dashEnd + gapLen;
                dashIdx++;
            }
        }

        private static LineRenderer CreateGuideLine()
        {
            var obj = new GameObject("GuideLine");
            var lr = obj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = 0.02f;
            lr.endWidth = 0.02f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(1f, 1f, 1f, 0.15f);
            lr.endColor = new Color(1f, 1f, 1f, 0.03f);
            lr.sortingOrder = 5;
            lr.useWorldSpace = true;
            lr.enabled = false;
            lr.textureMode = LineTextureMode.Tile;
            return lr;
        }

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString($"#{hex}", out Color c);
            return c;
        }

        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
                string folder = System.IO.Path.GetFileName(path);
                if (!AssetDatabase.IsValidFolder(parent)) EnsureDirectory(parent);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}
