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
        private static readonly Color BgColor = HexColor("0F1117"); // matches OC.bg
        private static readonly Color PanelColor = HexColor("1E1E26");
        private static readonly Color PanelBorder = HexColor("3A3A45");
        private static readonly Color PanelHighlight = HexColor("4A4A55");
        private static readonly Color PanelShadow = HexColor("101015");
        private static readonly Color TextColor = new Color(0.92f, 0.92f, 0.95f);
        private static readonly Color MutedText = new Color(0.55f, 0.55f, 0.60f);
        private static readonly Color AccentTeal = HexColor("0D9488");

        // Tier colors: tier 0 = smallest (Level 11), tier 10 = largest (Level 1)
        private static readonly Color[] TierColors = new[]
        {
            HexColor("E879F9"), // tier 0 (L11): Fuchsia
            HexColor("CBD5E1"), // tier 1 (L10): Silver
            HexColor("1D4ED8"), // tier 2 (L9):  Blue
            HexColor("EF4444"), // tier 3 (L8):  Red
            HexColor("38BDF8"), // tier 4 (L7):  Sky
            HexColor("F97316"), // tier 5 (L6):  Orange
            HexColor("A3E635"), // tier 6 (L5):  Lime
            HexColor("A78BFA"), // tier 7 (L4):  Violet
            HexColor("F0B429"), // tier 8 (L3):  Amber
            HexColor("E8587A"), // tier 9 (L2):  Pink
            HexColor("4DD9C0"), // tier 10 (L1): Cyan
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

            // Camera fitter — ensures container fills screen consistently across devices
            var cameraFitter = cam.gameObject.AddComponent<CameraFitter>();
            SetProperty(cameraFitter, "physicsConfig", CreatePhysicsConfig());

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
            CreateArenaGrid(physicsConfig);
            // Guide line removed — clean drop without visual guide

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
            managers.AddComponent<NetworkMonitor>();
            managers.AddComponent<OfflineSyncQueue>();
            managers.AddComponent<ShareManager>();
            managers.AddComponent<MergeParticles>();
            managers.AddComponent<GameAnalyticsSDK.GameAnalytics>();
            managers.AddComponent<MurgeAnalytics>();

            // Debug overlay removed from production builds

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
            // guideLine removed — no vertical drop guide
            // dcSO.FindProperty("guideLine").objectReferenceValue = null;
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
            scaler.referenceResolution = new Vector2(390, 844);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObj.AddComponent<GraphicRaycaster>();

            // EventSystem
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // Safe area root (content sits inside safe area)
            GameObject safeArea = new GameObject("SafeArea");
            safeArea.transform.SetParent(canvasObj.transform, false);
            var safeRT = safeArea.AddComponent<RectTransform>();
            safeRT.anchorMin = Vector2.zero;
            safeRT.anchorMax = Vector2.one;
            safeRT.offsetMin = Vector2.zero;
            safeRT.offsetMax = Vector2.zero;
            // No SafeAreaHandler — screens handle safe area padding internally via OS.safeAreaTop.
            // This lets screen backgrounds extend behind the notch/home indicator for a seamless look.

            // ===== TITLE SCREEN =====
            var (titlePanel, titleScreen) = CreateTitleScreenPanel(safeArea.transform);

            // ===== GAMEPLAY SCREEN =====
            var (gameplayPanel, scoreText, shakeButton, shakeCountTMP,
                nextBallImg, miniLB, scoreTickUp, nextBallPreviewUI, nextBallAnchorRT) = CreateGameplayPanel(safeArea.transform);

            // ===== RESULTS SCREEN =====
            var (resultsPanel, resultsScreen) = CreateResultsScreenPanel(safeArea.transform, tierConfig);

            // ===== LEADERBOARD SCREEN =====
            // Old leaderboard panel replaced by NewLeaderboardScreen
            var lbPanel = CreateFullPanel(safeArea.transform, "LeaderboardScreen_Legacy");
            lbPanel.SetActive(false);

            // ===== SETTINGS SCREEN =====
            var (settingsPanel, settingsScreen) = CreateSettingsPanel(safeArea.transform);

            // Add CanvasGroups for transitions (legacy screens)
            var titleCG = titlePanel.AddComponent<CanvasGroup>();
            var gameplayCG = gameplayPanel.AddComponent<CanvasGroup>();
            var resultsCG = resultsPanel.AddComponent<CanvasGroup>();
            var lbCG = lbPanel.AddComponent<CanvasGroup>();
            var settingsCG = settingsPanel.AddComponent<CanvasGroup>();

            // ===== NEW SPEC SCREENS =====
            // These self-build their UI on OnEnable via MurgeUI factory methods.
            var onboardingPanel = CreateNewScreenPanel(safeArea.transform, "OnboardingScreen", tierConfig);
            onboardingPanel.AddComponent<OnboardingScreen>();
            SetProperty(onboardingPanel.GetComponent<OnboardingScreen>(), "tierConfig", tierConfig);

            var homeFreshPanel = CreateNewScreenPanel(safeArea.transform, "HomeFreshScreen", tierConfig);
            homeFreshPanel.AddComponent<HomeFreshScreen>();
            SetProperty(homeFreshPanel.GetComponent<HomeFreshScreen>(), "tierConfig", tierConfig);

            var homePlayedPanel = CreateNewScreenPanel(safeArea.transform, "HomePlayedScreen", tierConfig);
            homePlayedPanel.AddComponent<HomePlayedScreen>();
            SetProperty(homePlayedPanel.GetComponent<HomePlayedScreen>(), "tierConfig", tierConfig);

            var resultOverlayPanel = CreateNewScreenPanel(safeArea.transform, "ResultOverlay", tierConfig);
            resultOverlayPanel.AddComponent<ResultOverlayScreen>();
            SetProperty(resultOverlayPanel.GetComponent<ResultOverlayScreen>(), "tierConfig", tierConfig);

            var shareSheetPanel = CreateNewScreenPanel(safeArea.transform, "ShareSheet", tierConfig);
            shareSheetPanel.AddComponent<ShareSheetScreen>();
            SetProperty(shareSheetPanel.GetComponent<ShareSheetScreen>(), "tierConfig", tierConfig);

            var newSettingsPanel = CreateNewScreenPanel(safeArea.transform, "NewSettingsScreen", tierConfig);
            newSettingsPanel.AddComponent<NewSettingsScreen>();

            var newLeaderboardPanel = CreateNewScreenPanel(safeArea.transform, "NewLeaderboardScreen", tierConfig);
            newLeaderboardPanel.AddComponent<NewLeaderboardScreen>();


            // CanvasGroups for new screens
            var onboardingCG = onboardingPanel.GetComponent<CanvasGroup>();
            var homeFreshCG = homeFreshPanel.GetComponent<CanvasGroup>();
            var homePlayedCG = homePlayedPanel.GetComponent<CanvasGroup>();
            var resultOverlayCG = resultOverlayPanel.GetComponent<CanvasGroup>();
            var shareSheetCG = shareSheetPanel.GetComponent<CanvasGroup>();
            var newSettingsCG = newSettingsPanel.GetComponent<CanvasGroup>();
            var newLeaderboardCG = newLeaderboardPanel.GetComponent<CanvasGroup>();

            // Hide everything — startup flow in GameManager decides what to show
            titlePanel.SetActive(false);
            gameplayPanel.SetActive(false);
            resultsPanel.SetActive(false);
            lbPanel.SetActive(false);
            settingsPanel.SetActive(false);
            onboardingPanel.SetActive(false);
            homeFreshPanel.SetActive(false);
            homePlayedPanel.SetActive(false);
            resultOverlayPanel.SetActive(false);
            shareSheetPanel.SetActive(false);
            newSettingsPanel.SetActive(false);
            newLeaderboardPanel.SetActive(false);

            // Loading screen — shows immediately on app start, covers everything
            var loadingPanel = CreateFullPanel(safeArea.transform, "LoadingScreen");
            loadingPanel.AddComponent<LoadingScreen>();
            loadingPanel.transform.SetAsLastSibling(); // render on top

            // ScreenManager — wire both legacy and new fields
            var screenManager = canvasObj.AddComponent<ScreenManager>();
            var smSO = new SerializedObject(screenManager);
            // Legacy fields
            smSO.FindProperty("titleScreen").objectReferenceValue = titleCG;
            smSO.FindProperty("gameplayScreen").objectReferenceValue = gameplayCG;
            smSO.FindProperty("resultsScreen").objectReferenceValue = resultsCG;
            // New spec fields
            smSO.FindProperty("onboardingScreen").objectReferenceValue = onboardingCG;
            smSO.FindProperty("homeFreshScreen").objectReferenceValue = homeFreshCG;
            smSO.FindProperty("homePlayedScreen").objectReferenceValue = homePlayedCG;
            smSO.FindProperty("gameScreen").objectReferenceValue = gameplayCG;
            smSO.FindProperty("resultOverlay").objectReferenceValue = resultOverlayCG;
            smSO.FindProperty("shareSheet").objectReferenceValue = shareSheetCG;
            smSO.FindProperty("settingsScreen").objectReferenceValue = newSettingsCG;
            smSO.FindProperty("leaderboardScreen").objectReferenceValue = newLeaderboardCG;
            smSO.ApplyModifiedPropertiesWithoutUndo();

            // UIManager
            var uiManager = canvasObj.AddComponent<UIManager>();
            var uiSO = new SerializedObject(uiManager);
            uiSO.FindProperty("titleScreen").objectReferenceValue = titleScreen;
            uiSO.FindProperty("resultsScreen").objectReferenceValue = resultsScreen;
            uiSO.FindProperty("settingsScreen").objectReferenceValue = settingsScreen;
            uiSO.FindProperty("scoreText").objectReferenceValue = scoreText;
            uiSO.FindProperty("shakeButton").objectReferenceValue = shakeButton;
            uiSO.FindProperty("shakeCountText").objectReferenceValue = shakeCountTMP;
            uiSO.FindProperty("nextBallPreview").objectReferenceValue = nextBallImg;
            uiSO.FindProperty("nextBallPreviewUI").objectReferenceValue = nextBallPreviewUI;
            uiSO.FindProperty("scoreTickUp").objectReferenceValue = scoreTickUp;
            uiSO.FindProperty("playButton").objectReferenceValue = titleScreen.PlayButton;
            uiSO.FindProperty("restartButton").objectReferenceValue = resultsScreen.PlayAgainButton;
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
            var title = CreateText(panel.transform, "Title", "MURGE", 64, TextColor);
            SetAnchors(title.rectTransform, 0.05f, 0.72f, 0.95f, 0.82f);

            // Tagline (directly below title)
            var tagline = CreateText(panel.transform, "Tagline", "a daily merge game", 20, MutedText);
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
            // No background — gameplay world shows through

            // ===== HEADER BAR (top) =====
            // Safe area inset is 0 at editor time — GameplayHUDFitter adjusts at runtime.
            float safeTop = 0f;
            // Back/close button (top-left) — smooth border, transparent bg, matching home screen style
            var backBtn = new GameObject("BackButton");
            backBtn.transform.SetParent(panel.transform, false);
            var backBtnRT = backBtn.AddComponent<RectTransform>();
            SetAnchors(backBtnRT, 0, 1, 0, 1);
            backBtnRT.anchoredPosition = new Vector2(16, -(safeTop + 14));
            backBtnRT.sizeDelta = new Vector2(34, 34);
            backBtnRT.pivot = new Vector2(0, 1);
            // Border only — no fill, camera bg shows through perfectly
            var backBorderGO = new GameObject("Border");
            backBorderGO.transform.SetParent(backBtn.transform, false);
            var bbRT = backBorderGO.AddComponent<RectTransform>();
            bbRT.anchorMin = Vector2.zero; bbRT.anchorMax = Vector2.one;
            bbRT.offsetMin = Vector2.zero; bbRT.offsetMax = Vector2.zero;
            var bbImg = backBorderGO.AddComponent<Image>();
            bbImg.sprite = CreateOutlineSprite();
            bbImg.type = Image.Type.Simple;
            bbImg.color = HexColor("232838");
            bbImg.raycastTarget = false;
            // X label (on top of everything)
            var backLabelGO = new GameObject("XLabel");
            backLabelGO.transform.SetParent(backBtn.transform, false);
            var blRT = backLabelGO.AddComponent<RectTransform>();
            blRT.anchorMin = Vector2.zero; blRT.anchorMax = Vector2.one;
            blRT.offsetMin = Vector2.zero; blRT.offsetMax = Vector2.zero;
            var backTMP = backLabelGO.AddComponent<TextMeshProUGUI>();
            var dmMonoFont = Resources.Load<TMP_FontAsset>("Fonts/DMMono-Medium SDF");
            backTMP.text = "x";
            backTMP.font = dmMonoFont;
            backTMP.fontSize = 14;
            backTMP.color = new Color(1, 1, 1, 0.3f); // visible muted white
            backTMP.alignment = TextAlignmentOptions.Center;
            backTMP.raycastTarget = false;
            // Hit area (invisible, must be last for proper layering)
            var backBtnImg = backBtn.AddComponent<Image>();
            backBtnImg.color = Color.clear;
            var backBtnComp = backBtn.AddComponent<Button>();
            backBtnComp.targetGraphic = bbImg;
            backTMP.alignment = TextAlignmentOptions.Center;
            backTMP.raycastTarget = false;

            // Score block (next to back button) — disable raycast on text so they don't block the button
            var scoreLabelTMP = CreateText(panel.transform, "ScoreLabel", "SCORE", 7, new Color(1, 1, 1, 0.22f));
            SetAnchors(scoreLabelTMP.rectTransform, 0, 1, 0, 1);
            scoreLabelTMP.rectTransform.anchoredPosition = new Vector2(60, -(safeTop + 14));
            scoreLabelTMP.rectTransform.sizeDelta = new Vector2(80, 12);
            scoreLabelTMP.rectTransform.pivot = new Vector2(0, 1);
            scoreLabelTMP.alignment = TextAlignmentOptions.Left;
            scoreLabelTMP.raycastTarget = false;

            var scoreText = CreateText(panel.transform, "Score", "0", 28, HexColor("4DD9C0"));
            SetAnchors(scoreText.rectTransform, 0, 1, 0, 1);
            scoreText.rectTransform.anchoredPosition = new Vector2(60, -(safeTop + 26));
            scoreText.rectTransform.sizeDelta = new Vector2(150, 34);
            scoreText.rectTransform.pivot = new Vector2(0, 1);
            scoreText.alignment = TextAlignmentOptions.Left;
            scoreText.raycastTarget = false;

            // Try to use DMMono for score number
            var dmMono = Resources.Load<TMP_FontAsset>("Fonts/DMMono-Medium SDF");
            if (dmMono != null) scoreText.font = dmMono;

            // ScoreTickUp
            var scoreTickUp = scoreText.gameObject.AddComponent<ScoreTickUp>();
            SetProperty(scoreTickUp, "scoreText", scoreText);

            // Combo indicator (below score)
            var comboGO = new GameObject("ComboUI");
            comboGO.transform.SetParent(panel.transform, false);
            var comboRT = comboGO.AddComponent<RectTransform>();
            SetAnchors(comboRT, 0, 1, 0, 1);
            comboRT.anchoredPosition = new Vector2(60, -(safeTop + 60));
            comboRT.sizeDelta = new Vector2(120, 18);
            comboRT.pivot = new Vector2(0, 1);
            comboGO.AddComponent<ComboUI>();

            // Next ball card (top-right) — large enough to contain any ball
            var nextCard = new GameObject("NextBallCard");
            nextCard.transform.SetParent(panel.transform, false);
            var nextCardRT = nextCard.AddComponent<RectTransform>();
            SetAnchors(nextCardRT, 1, 1, 1, 1); // top-right
            nextCardRT.anchoredPosition = new Vector2(-16, -(safeTop + 8));
            nextCardRT.sizeDelta = new Vector2(60, 60);
            nextCardRT.pivot = new Vector2(1, 1);
            // No background or border — passive display, not a button
            // "NEXT" label
            var nextLabelTMP = CreateText(nextCard.transform, "NextLabel", "NEXT", 6, new Color(1, 1, 1, 0.22f));
            var nextLabelRT = nextLabelTMP.rectTransform;
            nextLabelRT.anchorMin = new Vector2(0, 1); nextLabelRT.anchorMax = new Vector2(1, 1);
            nextLabelRT.pivot = new Vector2(0.5f, 1);
            nextLabelRT.anchoredPosition = new Vector2(0, -4);
            nextLabelRT.sizeDelta = new Vector2(0, 10);
            nextLabelTMP.alignment = TextAlignmentOptions.Center;
            nextLabelTMP.raycastTarget = false;
            // Next ball UI Image — renders inside the card as a UI element (not world-space)
            var nextBallImgObj = new GameObject("NextBallImage");
            nextBallImgObj.transform.SetParent(nextCard.transform, false);
            var nextBallImgRT = nextBallImgObj.AddComponent<RectTransform>();
            nextBallImgRT.anchorMin = new Vector2(0.1f, 0.05f);
            nextBallImgRT.anchorMax = new Vector2(0.9f, 0.72f);
            nextBallImgRT.offsetMin = Vector2.zero;
            nextBallImgRT.offsetMax = Vector2.zero;
            var nextImg = nextBallImgObj.AddComponent<Image>();
            nextImg.preserveAspect = true;
            nextImg.raycastTarget = false;
            nextImg.color = Color.white;

            // Anchor for DropController (still needed for world-space drop positioning)
            var nextAnchor = new GameObject("NextBallAnchor");
            nextAnchor.transform.SetParent(panel.transform, false);
            var anchorRT = nextAnchor.AddComponent<RectTransform>();
            SetAnchors(anchorRT, 0.5f, 0.95f, 0.5f, 0.95f); // top-center of screen for drop position

            NextBallPreviewUI nextBallUI = null;

            // Mini leaderboard (hidden — not shown in new design during gameplay)
            var mlbObj = new GameObject("MiniLeaderboard");
            mlbObj.transform.SetParent(panel.transform, false);
            var mlbRT = mlbObj.AddComponent<RectTransform>();
            SetAnchors(mlbRT, 0, 0.92f, 0.01f, 0.93f); // tiny, hidden
            mlbObj.SetActive(false);
            var topPlayerText = CreateText(mlbObj.transform, "TopPlayer", "", 7, MutedText);
            var playerRow = CreateText(mlbObj.transform, "PlayerRow", "", 7, MutedText);
            var miniLB = mlbObj.AddComponent<MiniLeaderboardUI>();
            var mlbSO = new SerializedObject(miniLB);
            mlbSO.FindProperty("topPlayerText").objectReferenceValue = topPlayerText;
            mlbSO.FindProperty("playerRow").objectReferenceValue = playerRow;
            mlbSO.ApplyModifiedPropertiesWithoutUndo();

            // Arena grid is created as a world-space object (not UI) in BuildGameScene
            // so it renders BEHIND balls. See CreateArenaGrid().

            // ===== SHAKE BUTTON (center-top, in header area) =====
            var shakeArea = new GameObject("ShakeArea");
            shakeArea.transform.SetParent(panel.transform, false);
            var shakeAreaRT = shakeArea.AddComponent<RectTransform>();
            // Anchor at top-center
            SetAnchors(shakeAreaRT, 0.5f, 1, 0.5f, 1);
            shakeAreaRT.anchoredPosition = new Vector2(0, -(safeTop + 14));
            shakeAreaRT.sizeDelta = new Vector2(120, 36);
            shakeAreaRT.pivot = new Vector2(0.5f, 1);

            // Shake button
            var shakeBtn = new GameObject("ShakeButton");
            shakeBtn.transform.SetParent(shakeArea.transform, false);
            var shakeBtnRT = shakeBtn.AddComponent<RectTransform>();
            shakeBtnRT.anchorMin = Vector2.zero; shakeBtnRT.anchorMax = Vector2.one;
            shakeBtnRT.offsetMin = Vector2.zero; shakeBtnRT.offsetMax = Vector2.zero;
            var shakeBtnImg = shakeBtn.AddComponent<Image>();
            shakeBtnImg.sprite = PixelUIGenerator.GetRoundedRect9Slice();
            shakeBtnImg.type = Image.Type.Sliced;
            shakeBtnImg.color = HexColor("161B24");
            shakeBtn.AddComponent<Button>().targetGraphic = shakeBtnImg;
            // Shake outline
            var shakeOutline = new GameObject("Outline");
            shakeOutline.transform.SetParent(shakeBtn.transform, false);
            var shakeOutlineRT = shakeOutline.AddComponent<RectTransform>();
            shakeOutlineRT.anchorMin = Vector2.zero; shakeOutlineRT.anchorMax = Vector2.one;
            shakeOutlineRT.offsetMin = Vector2.zero; shakeOutlineRT.offsetMax = Vector2.zero;
            var shakeOutlineImg = shakeOutline.AddComponent<Image>();
            shakeOutlineImg.sprite = PixelUIGenerator.GetRoundedRect9Slice();
            shakeOutlineImg.type = Image.Type.Sliced;
            shakeOutlineImg.color = HexColor("232838");
            shakeOutlineImg.raycastTarget = false;
            // Shake label with count inline — "Shake · 3"
            var shakeTMP = CreateText(shakeBtn.transform, "ShakeLabel", "Shake", 8, new Color(1, 1, 1, 0.35f));
            shakeTMP.alignment = TextAlignmentOptions.Center;
            var shakeTMPRT = shakeTMP.rectTransform;
            shakeTMPRT.anchorMin = new Vector2(0, 0); shakeTMPRT.anchorMax = new Vector2(0.7f, 1);
            shakeTMPRT.offsetMin = Vector2.zero; shakeTMPRT.offsetMax = Vector2.zero;

            // Count to the right of the label
            var shakeCountTMP = CreateText(shakeBtn.transform, "ShakeCount", "3", 8, new Color(1, 1, 1, 0.22f));
            shakeCountTMP.alignment = TextAlignmentOptions.Center;
            var shakeCountRT = shakeCountTMP.rectTransform;
            shakeCountRT.anchorMin = new Vector2(0.7f, 0); shakeCountRT.anchorMax = new Vector2(1, 1);
            shakeCountRT.offsetMin = Vector2.zero; shakeCountRT.offsetMax = Vector2.zero;

            // ===== EXIT CONFIRM MODAL =====
            var confirmPanel = new GameObject("ExitConfirm");
            confirmPanel.transform.SetParent(panel.transform, false);
            var cpRT = confirmPanel.AddComponent<RectTransform>();
            SetAnchors(cpRT, 0, 0, 1, 1);
            // Dark scrim
            var cpBg = confirmPanel.AddComponent<Image>();
            cpBg.color = new Color(0.031f, 0.031f, 0.055f, 0.88f); // OC.overlayDark

            // Modal card (centered)
            var modalCard = new GameObject("ModalCard");
            modalCard.transform.SetParent(confirmPanel.transform, false);
            var mcRT = modalCard.AddComponent<RectTransform>();
            mcRT.anchorMin = new Vector2(0.5f, 0.5f);
            mcRT.anchorMax = new Vector2(0.5f, 0.5f);
            mcRT.pivot = new Vector2(0.5f, 0.5f);
            mcRT.sizeDelta = new Vector2(260, 160);
            var mcBg = modalCard.AddComponent<Image>();
            mcBg.sprite = PixelUIGenerator.GetRoundedRect9Slice();
            mcBg.type = Image.Type.Sliced;
            mcBg.color = HexColor("161B24");
            // Card border
            var mcOutline = new GameObject("Outline");
            mcOutline.transform.SetParent(modalCard.transform, false);
            var mcOutRT = mcOutline.AddComponent<RectTransform>();
            mcOutRT.anchorMin = Vector2.zero; mcOutRT.anchorMax = Vector2.one;
            mcOutRT.offsetMin = Vector2.zero; mcOutRT.offsetMax = Vector2.zero;
            var mcOutImg = mcOutline.AddComponent<Image>();
            mcOutImg.sprite = PixelUIGenerator.GetRoundedRect9Slice();
            mcOutImg.type = Image.Type.Sliced;
            mcOutImg.color = HexColor("232838");
            mcOutImg.raycastTarget = false;

            // "Quit game?" text
            var confirmText = CreateText(modalCard.transform, "ConfirmText", "Quit game?", 12, TextColor);
            var ctRT = confirmText.rectTransform;
            ctRT.anchorMin = new Vector2(0, 0.55f); ctRT.anchorMax = new Vector2(1, 0.9f);
            ctRT.offsetMin = new Vector2(16, 0); ctRT.offsetMax = new Vector2(-16, 0);
            confirmText.alignment = TextAlignmentOptions.Center;

            // Subtitle (shown for scored games)
            var subtitleText = CreateText(modalCard.transform, "SubtitleText", "", 8, MutedText);
            var subRT = subtitleText.rectTransform;
            subRT.anchorMin = new Vector2(0, 0.38f); subRT.anchorMax = new Vector2(1, 0.55f);
            subRT.offsetMin = new Vector2(16, 0); subRT.offsetMax = new Vector2(-16, 0);
            subtitleText.alignment = TextAlignmentOptions.Center;

            // Button row (fixed 44px height, pinned to bottom of card)
            var btnRow = new GameObject("ButtonRow");
            btnRow.transform.SetParent(modalCard.transform, false);
            var brRT = btnRow.AddComponent<RectTransform>();
            brRT.anchorMin = new Vector2(0, 0); brRT.anchorMax = new Vector2(1, 0);
            brRT.pivot = new Vector2(0.5f, 0);
            brRT.anchoredPosition = new Vector2(0, 16);
            brRT.sizeDelta = new Vector2(-32, 44); // 16px padding each side, 44px height
            var brHLG = btnRow.AddComponent<HorizontalLayoutGroup>();
            brHLG.spacing = 8;
            brHLG.childAlignment = TextAnchor.MiddleCenter;
            brHLG.childControlWidth = true;
            brHLG.childControlHeight = true;
            brHLG.childForceExpandWidth = true;
            brHLG.childForceExpandHeight = true;

            // QUIT button (primary pink with scanlines)
            var quitBtnGO = new GameObject("YesBtn");
            quitBtnGO.transform.SetParent(btnRow.transform, false);
            var quitBtnImg = quitBtnGO.AddComponent<Image>();
            quitBtnImg.sprite = MurgeUI.SmoothRoundedRect;
            quitBtnImg.type = Image.Type.Sliced;
            quitBtnImg.color = HexColor("E8587A"); // pink/danger
            var yesBtn = quitBtnGO.AddComponent<Button>();
            yesBtn.targetGraphic = quitBtnImg;
            // Scanlines
            var quitScanGO = new GameObject("Scanlines");
            quitScanGO.transform.SetParent(quitBtnGO.transform, false);
            var qsRT = quitScanGO.AddComponent<RectTransform>();
            qsRT.anchorMin = Vector2.zero; qsRT.anchorMax = Vector2.one;
            qsRT.offsetMin = Vector2.zero; qsRT.offsetMax = Vector2.zero;
            var qsImg = quitScanGO.AddComponent<Image>();
            qsImg.sprite = MurgeUI.GetScanlineSprite();
            qsImg.type = Image.Type.Simple;
            qsImg.color = new Color(0, 0, 0, 0.22f);
            qsImg.raycastTarget = false;
            quitBtnGO.AddComponent<RectMask2D>(); // clip scanlines to rounded shape
            // Label
            var quitLabel = CreateText(quitBtnGO.transform, "Label", "QUIT", 11, HexColor("0F1117"));
            quitLabel.alignment = TextAlignmentOptions.Center;
            quitLabel.characterSpacing = 2;
            var qlRT = quitLabel.rectTransform;
            qlRT.anchorMin = Vector2.zero; qlRT.anchorMax = Vector2.one;
            qlRT.offsetMin = Vector2.zero; qlRT.offsetMax = Vector2.zero;

            // CANCEL button — outline style matching skip buttons
            var cancelBtnGO = new GameObject("NoBtn");
            cancelBtnGO.transform.SetParent(btnRow.transform, false);
            // Border background
            var cancelBtnImg = cancelBtnGO.AddComponent<Image>();
            cancelBtnImg.sprite = MurgeUI.SmoothRoundedRect;
            cancelBtnImg.type = Image.Type.Sliced;
            cancelBtnImg.color = HexColor("232838"); // OC.border
            // Inner fill (inset to create border effect)
            var cancelInner = new GameObject("Inner");
            cancelInner.transform.SetParent(cancelBtnGO.transform, false);
            var ciRT = cancelInner.AddComponent<RectTransform>();
            ciRT.anchorMin = Vector2.zero; ciRT.anchorMax = Vector2.one;
            ciRT.offsetMin = new Vector2(1.5f, 1.5f); ciRT.offsetMax = new Vector2(-1.5f, -1.5f);
            var ciImg = cancelInner.AddComponent<Image>();
            ciImg.sprite = MurgeUI.SmoothRoundedRect;
            ciImg.type = Image.Type.Sliced;
            ciImg.color = HexColor("0F1117"); // OC.bg
            ciImg.raycastTarget = false;
            // Label
            var noBtn = cancelBtnGO.AddComponent<Button>();
            noBtn.targetGraphic = cancelBtnImg;
            var cancelLabel = CreateText(cancelBtnGO.transform, "Label", "CANCEL", 9, new Color(1, 1, 1, 0.22f));
            cancelLabel.alignment = TextAlignmentOptions.Center;
            cancelLabel.characterSpacing = 1;
            var clRT = cancelLabel.rectTransform;
            clRT.anchorMin = Vector2.zero; clRT.anchorMax = Vector2.one;
            clRT.offsetMin = Vector2.zero; clRT.offsetMax = Vector2.zero;

            confirmPanel.SetActive(false);

            // Runtime safe area adjustment for HUD elements
            panel.AddComponent<GameplayHUDFitter>();

            // Exit confirmation — finds its own children by name at runtime
            panel.AddComponent<SimpleExitConfirm>();

            return (panel, scoreText, shakeBtn.GetComponent<Button>(), shakeCountTMP,
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
            var dayLabel = CreateText(panel.transform, "DayLabel", "Murge #1", 24, MutedText);
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

        /// <summary>
        /// Create a full-screen panel for new spec screens.
        /// These self-build their UI on OnEnable, so just need the panel + CanvasGroup.
        /// </summary>
        private static GameObject CreateNewScreenPanel(Transform parent, string name, BallTierConfig tierConfig)
        {
            var panel = CreateFullPanel(parent, name);
            panel.AddComponent<CanvasGroup>();
            return panel;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string text,
            float fontSize, Color color)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            obj.AddComponent<RectTransform>();
            var tmp = obj.AddComponent<TextMeshProUGUI>();
            var font = Resources.Load<TMP_FontAsset>("Fonts/PressStart2P SDF")
                    ?? Resources.Load<TMP_FontAsset>("Fonts/DMMono-Medium SDF");
            if (font != null) tmp.font = font;
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
            // Container fills screen width with small margin, height from below header to above shake
            config.containerWidth = 5.1f;
            config.containerHeight = 9.5f;
            config.containerBottomY = -3.5f;
            config.dropHeight = 5.5f;   // inside container top (6.0), below header UI
            config.deathLineY = 4.5f;   // below drop point (5.5), gives room for drop + settle
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
                byte[] png = BallRenderer.GenerateBallPNG(i, TierColors[i], def.radius, 0f);
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
                    imp.spritePixelsPerUnit = BallRenderer.PixelsPerUnit;
                    imp.filterMode = FilterMode.Bilinear;
                    imp.textureCompression = TextureImporterCompression.Uncompressed;
                    imp.spriteImportMode = SpriteImportMode.Single;
                    // Ensure sprite rect matches actual texture dimensions
                    imp.spriteBorder = Vector4.zero;
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

        /// <summary>
        /// Creates the arena grid + border as world-space SpriteRenderers.
        /// Renders at sortingOrder -2, behind balls (0+) and container walls (-1).
        /// </summary>
        private static void CreateArenaGrid(PhysicsConfig config)
        {
            float w = config.containerWidth;
            float h = config.containerHeight;
            float botY = config.containerBottomY;
            float cx = 0f;
            float cy = botY + h / 2f;

            // Grid matches the physics container EXACTLY — they are the same thing
            float left = -w / 2f;
            float right = w / 2f;
            float bottom = botY;
            float top = botY + h;
            float gridW = w;
            float gridH = h;

            var gridRoot = new GameObject("ArenaGrid");
            gridRoot.transform.position = Vector3.zero;

            Color lineColor = new Color(0.2f, 0.25f, 0.35f, 0.35f);
            float lineWidth = 0.02f;

            // Background fill
            var bgObj = new GameObject("ArenaBG");
            bgObj.transform.SetParent(gridRoot.transform);
            bgObj.transform.localPosition = new Vector3(cx, cy, 0.1f);
            var bgSR = bgObj.AddComponent<SpriteRenderer>();
            bgSR.sprite = CreatePixelSprite();
            bgSR.color = HexColor("161B24");
            bgSR.sortingOrder = -3;
            bgObj.transform.localScale = new Vector3(gridW, gridH, 1f);

            // Border (4 straight edges)
            CreateWorldLine(gridRoot.transform, "BorderTop", left, top, right, top, lineWidth, lineColor, -2);
            CreateWorldLine(gridRoot.transform, "BorderBot", left, bottom, right, bottom, lineWidth, lineColor, -2);
            CreateWorldLine(gridRoot.transform, "BorderLeft", left, bottom, left, top, lineWidth, lineColor, -2);
            CreateWorldLine(gridRoot.transform, "BorderRight", right, bottom, right, top, lineWidth, lineColor, -2);

            // Vertical grid lines
            int vLines = 4;
            for (int i = 1; i <= vLines; i++)
            {
                float x = left + (gridW * i / (vLines + 1));
                CreateWorldLine(gridRoot.transform, $"VGrid{i}", x, bottom, x, top, lineWidth, lineColor, -2);
            }

            // Horizontal grid lines
            int hLines = 9;
            for (int i = 1; i <= hLines; i++)
            {
                float y = bottom + (gridH * i / (hLines + 1));
                CreateWorldLine(gridRoot.transform, $"HGrid{i}", left, y, right, y, lineWidth, lineColor, -2);
            }

            // Danger line (near top, pink)
            float dangerY = config.deathLineY;
            CreateWorldLine(gridRoot.transform, "DangerLine", left, dangerY, right, dangerY,
                0.025f, new Color(0.91f, 0.345f, 0.478f, 0.35f), -2);
        }

        private static void CreateWorldLine(Transform parent, string name,
            float x1, float y1, float x2, float y2, float width, Color color, int sortOrder)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(parent);

            var lr = obj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(x1, y1, 0));
            lr.SetPosition(1, new Vector3(x2, y2, 0));
            lr.startWidth = width;
            lr.endWidth = width;
            lr.startColor = color;
            lr.endColor = color;
            lr.sortingOrder = sortOrder;
            lr.useWorldSpace = true;
            lr.material = new Material(Shader.Find("Sprites/Default"));
        }

        /// <summary>Outline-only rounded rect — transparent interior, border ring only.</summary>
        private static Sprite _outlineSprite;
        private static Sprite CreateOutlineSprite()
        {
            if (_outlineSprite != null) return _outlineSprite;
            int size = 64;
            int radius = 10;
            float border = 1.5f;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            Color[] px = new Color[size * size];
            for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
            float c = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = Mathf.Max(0, Mathf.Abs(x - c + 0.5f) - (c - radius));
                    float dy = Mathf.Max(0, Mathf.Abs(y - c + 0.5f) - (c - radius));
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    if (dist <= radius)
                    {
                        float inner = radius - dist;
                        if (inner <= border)
                        {
                            float a = Mathf.Clamp01(inner / border);
                            float outerAA = Mathf.Clamp01(radius - dist + 0.5f);
                            px[y * size + x] = new Color(1, 1, 1, a * outerAA);
                        }
                    }
                    else if (dist <= radius + 1f)
                    {
                        px[y * size + x] = new Color(1, 1, 1, Mathf.Clamp01(radius + 1f - dist));
                    }
                }
            }
            tex.SetPixels(px);
            tex.Apply();
            _outlineSprite = Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), size);
            return _outlineSprite;
        }

        private static Sprite CreatePixelSprite()
        {
            var tex = new Texture2D(4, 4);
            tex.filterMode = FilterMode.Point;
            Color[] c = new Color[16];
            for (int i = 0; i < 16; i++) c[i] = Color.white;
            tex.SetPixels(c);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
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
            // Visual danger line is drawn by CreateArenaGrid
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
