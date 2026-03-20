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
        // New color palette — chromatic arc: cool to warm
        private static readonly Color[] TierColors = new[]
        {
            HexColor("0D9488"), // Tier 1: Deep teal
            HexColor("3B82F6"), // Tier 2: Blue
            HexColor("6366F1"), // Tier 3: Indigo
            HexColor("8B5CF6"), // Tier 4: Purple
            HexColor("A855F7"), // Tier 5: Violet
            HexColor("D946EF"), // Tier 6: Fuchsia
            HexColor("F43F5E"), // Tier 7: Rose
            HexColor("F97316"), // Tier 8: Orange
            HexColor("F59E0B"), // Tier 9: Amber
            HexColor("EAB308"), // Tier 10: Yellow
            HexColor("FFD700"), // Tier 11: Gold
        };

        [MenuItem("MergeGame/Build Game Scene", false, 1)]
        public static void BuildGameScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // Setup camera — dark warm background
            var cam = Camera.main;
            cam.orthographic = true;
            cam.orthographicSize = 6f;
            cam.transform.position = new Vector3(0f, 0.5f, -10f);
            cam.backgroundColor = HexColor("121218");

            // Create assets
            var physicsConfig = CreatePhysicsConfig();
            var tierConfig = CreateTierConfig();
            var physicsMat = CreatePhysicsMaterial(physicsConfig);
            var ballPrefab = CreateBallPrefab(physicsMat);

            // Create scene objects
            CreateContainer();
            CreateDeathLine();
            var guideLine = CreateGuideLine();

            // Create Managers object with all managers
            var managers = new GameObject("Managers");
            var gameManager = managers.AddComponent<GameManager>();
            var scoreManager = managers.AddComponent<ScoreManager>();
            var dropController = managers.AddComponent<DropController>();
            var audioManager = managers.AddComponent<AudioManager>();
            var dailySeedManager = managers.AddComponent<DailySeedManager>();
            var playerIdentity = managers.AddComponent<PlayerIdentity>();
            var streakManager = managers.AddComponent<StreakManager>();
            var mergeTracker = managers.AddComponent<MergeTracker>();
            var hapticManager = managers.AddComponent<HapticManager>();
            var achievementManager = managers.AddComponent<AchievementManager>();
            var supabaseClient = managers.AddComponent<SupabaseClient>();
            var leaderboardService = managers.AddComponent<LeaderboardService>();
            var mergeParticles = managers.AddComponent<MergeParticles>();

            // Create UI
            var (canvas, uiManager) = CreateUI(tierConfig);

            // Add debug overlay
            var debugOverlay = managers.AddComponent<DebugOverlay>();
            var debugSO = new SerializedObject(debugOverlay);
            debugSO.FindProperty("physicsConfig").objectReferenceValue = physicsConfig;
            debugSO.ApplyModifiedPropertiesWithoutUndo();

            // Wire up DailySeedManager
            var dsSO = new SerializedObject(dailySeedManager);
            dsSO.FindProperty("tierConfig").objectReferenceValue = tierConfig;
            dsSO.ApplyModifiedPropertiesWithoutUndo();

            // Wire up GameManager
            var gmSO = new SerializedObject(gameManager);
            gmSO.FindProperty("dropController").objectReferenceValue = dropController;
            gmSO.FindProperty("scoreManager").objectReferenceValue = scoreManager;
            gmSO.FindProperty("uiManager").objectReferenceValue = uiManager;
            gmSO.FindProperty("audioManager").objectReferenceValue = audioManager;
            gmSO.ApplyModifiedPropertiesWithoutUndo();

            // Wire up DropController
            var dcSO = new SerializedObject(dropController);
            dcSO.FindProperty("tierConfig").objectReferenceValue = tierConfig;
            dcSO.FindProperty("physicsConfig").objectReferenceValue = physicsConfig;
            dcSO.FindProperty("ballPrefab").objectReferenceValue = ballPrefab;
            dcSO.FindProperty("guideLine").objectReferenceValue = guideLine;
            dcSO.FindProperty("dropY").floatValue = physicsConfig.dropHeight;
            dcSO.FindProperty("leftWallX").floatValue = -physicsConfig.containerWidth / 2f;
            dcSO.FindProperty("rightWallX").floatValue = physicsConfig.containerWidth / 2f;
            dcSO.FindProperty("cooldownDuration").floatValue = physicsConfig.cooldownDuration;
            dcSO.ApplyModifiedPropertiesWithoutUndo();

            // Save scene
            string scenePath = "Assets/Scenes/GameScene.unity";
            EditorSceneManager.SaveScene(scene, scenePath);
            AssetDatabase.Refresh();

            Debug.Log("MergeGame: Game scene built successfully! Press Play to test.");
        }

        [MenuItem("MergeGame/Create All Assets", false, 0)]
        public static void CreateAllAssets()
        {
            CreatePhysicsConfig();
            CreateTierConfig();
            var physics = CreatePhysicsConfig();
            var mat = CreatePhysicsMaterial(physics);
            CreateBallPrefab(mat);
            AssetDatabase.Refresh();
            Debug.Log("MergeGame: All assets created!");
        }

        private static PhysicsConfig CreatePhysicsConfig()
        {
            EnsureDirectory("Assets/ScriptableObjects");
            string path = "Assets/ScriptableObjects/PhysicsConfig.asset";

            PhysicsConfig config = AssetDatabase.LoadAssetAtPath<PhysicsConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<PhysicsConfig>();
                AssetDatabase.CreateAsset(config, path);
            }
            EditorUtility.SetDirty(config);
            AssetDatabase.SaveAssets();
            return config;
        }

        private static BallTierConfig CreateTierConfig()
        {
            EnsureDirectory("Assets/ScriptableObjects");
            EnsureDirectory("Assets/Sprites");

            // Generate pixel-art ball sprites for each tier
            var tierDefs = new (int idx, float radius, int points, string name)[]
            {
                (0,  0.22f, 1,  "Cherry"),
                (1,  0.30f, 3,  "Strawberry"),
                (2,  0.40f, 6,  "Grape"),
                (3,  0.50f, 10, "Dekopon"),
                (4,  0.60f, 15, "Orange"),
                (5,  0.70f, 21, "Apple"),
                (6,  0.80f, 28, "Pear"),
                (7,  0.90f, 36, "Peach"),
                (8,  1.10f, 45, "Pineapple"),
                (9,  1.20f, 55, "Melon"),
                (10, 1.40f, 66, "Watermelon"),
            };

            BallData[] ballDatas = new BallData[tierDefs.Length];

            for (int i = 0; i < tierDefs.Length; i++)
            {
                var def = tierDefs[i];
                Color color = TierColors[i];

                // Generate pixel-art sprite
                string spritePath = $"Assets/Sprites/Ball_Tier{i + 1}.png";
                byte[] png = PixelBallRenderer.GenerateBallPNG(i, color, 32);
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
                data.color = color;
                data.pointValue = def.points;
                data.displayName = def.name;
                // Sprite will be assigned after reimport
                EditorUtility.SetDirty(data);
                ballDatas[i] = data;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Configure sprite imports and assign
            for (int i = 0; i < tierDefs.Length; i++)
            {
                string spritePath = $"Assets/Sprites/Ball_Tier{i + 1}.png";
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(spritePath);
                if (importer != null)
                {
                    importer.textureType = TextureImporterType.Sprite;
                    importer.spritePixelsPerUnit = 32;
                    importer.filterMode = FilterMode.Point; // Nearest-neighbor for pixel art
                    importer.textureCompression = TextureImporterCompression.Uncompressed;
                    importer.SaveAndReimport();
                }

                Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite != null)
                {
                    ballDatas[i].sprite = sprite;
                    EditorUtility.SetDirty(ballDatas[i]);
                }
            }

            // Create tier config
            string configPath = "Assets/ScriptableObjects/BallTierConfig.asset";
            BallTierConfig config = AssetDatabase.LoadAssetAtPath<BallTierConfig>(configPath);
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
            string path = "Assets/Materials/BallPhysicsMaterial.asset";
            EnsureDirectory("Assets/Materials");

            PhysicsMaterial2D mat = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(path);
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

            // Always rebuild
            GameObject existingPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (existingPrefab != null)
                AssetDatabase.DeleteAsset(prefabPath);

            // Use tier 1 sprite as default
            Sprite defaultSprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ball_Tier1.png");

            GameObject ball = new GameObject("Ball");

            var sr = ball.AddComponent<SpriteRenderer>();
            sr.sprite = defaultSprite;
            sr.color = Color.white;
            sr.sortingOrder = 0;

            var rb = ball.AddComponent<Rigidbody2D>();
            rb.gravityScale = 1f;
            rb.mass = 1f;
            rb.linearDamping = 0.1f;
            rb.angularDamping = 0.05f;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = ball.AddComponent<CircleCollider2D>();
            col.radius = 0.5f;
            if (physicsMat != null) col.sharedMaterial = physicsMat;

            var controller = ball.AddComponent<BallController>();

            // Tier label (hidden by default in visual overhaul, but kept for debugging)
            GameObject labelObj = new GameObject("TierLabel");
            labelObj.transform.SetParent(ball.transform);
            labelObj.transform.localPosition = Vector3.zero;
            labelObj.transform.localScale = Vector3.one;
            var tmp = labelObj.AddComponent<TextMeshPro>();
            tmp.text = "";
            tmp.fontSize = 4;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.sortingOrder = 1;
            tmp.enableAutoSizing = true;
            tmp.fontSizeMin = 1;
            tmp.fontSizeMax = 6;
            labelObj.SetActive(false);

            var so = new SerializedObject(controller);
            so.FindProperty("spriteRenderer").objectReferenceValue = sr;
            so.FindProperty("tierLabel").objectReferenceValue = tmp;
            so.ApplyModifiedPropertiesWithoutUndo();

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(ball, prefabPath);
            Object.DestroyImmediate(ball);
            return prefab;
        }

        private static void CreateContainer()
        {
            var physicsConfig = AssetDatabase.LoadAssetAtPath<PhysicsConfig>("Assets/ScriptableObjects/PhysicsConfig.asset");

            GameObject container = new GameObject("Container");
            var setup = container.AddComponent<ContainerSetup>();

            if (physicsConfig != null)
            {
                var so = new SerializedObject(setup);
                so.FindProperty("physicsConfig").objectReferenceValue = physicsConfig;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void CreateDeathLine()
        {
            var physicsConfig = AssetDatabase.LoadAssetAtPath<PhysicsConfig>("Assets/ScriptableObjects/PhysicsConfig.asset");
            float deathY = physicsConfig != null ? physicsConfig.deathLineY : 3.5f;

            GameObject deathLineObj = new GameObject("DeathLine");
            deathLineObj.transform.position = new Vector3(0f, deathY, 0f);

            var col = deathLineObj.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(6f, 5f);
            col.offset = new Vector2(0f, 2.5f);

            deathLineObj.AddComponent<DeathLine>();
            deathLineObj.AddComponent<DeathLinePulse>();

            // Visual line
            GameObject lineVisual = new GameObject("DeathLineVisual");
            lineVisual.transform.SetParent(deathLineObj.transform);
            lineVisual.transform.localPosition = Vector3.zero;

            var lr = lineVisual.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.SetPosition(0, new Vector3(-2.6f, deathY, 0f));
            lr.SetPosition(1, new Vector3(2.6f, deathY, 0f));
            lr.startWidth = 0.03f;
            lr.endWidth = 0.03f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(1f, 0.2f, 0.2f, 0.3f);
            lr.endColor = new Color(1f, 0.2f, 0.2f, 0.3f);
            lr.sortingOrder = 5;
            lr.useWorldSpace = true;
        }

        private static LineRenderer CreateGuideLine()
        {
            GameObject guideObj = new GameObject("GuideLine");
            var lr = guideObj.AddComponent<LineRenderer>();
            lr.positionCount = 2;
            lr.startWidth = 0.02f;
            lr.endWidth = 0.02f;
            lr.material = new Material(Shader.Find("Sprites/Default"));
            lr.startColor = new Color(1f, 1f, 1f, 0.2f);
            lr.endColor = new Color(1f, 1f, 1f, 0.05f);
            lr.sortingOrder = 5;
            lr.useWorldSpace = true;
            lr.enabled = false;
            lr.textureMode = LineTextureMode.Tile;
            return lr;
        }

        private static (GameObject, UIManager) CreateUI(BallTierConfig tierConfig)
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
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();

            // === PLAYING PANEL ===
            GameObject playingPanel = CreatePanel(canvasObj.transform, "PlayingPanel");

            var scoreText = CreateTMPText(playingPanel.transform, "ScoreText",
                "0", 72, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(1, 1),
                new Vector2(0, -20), new Vector2(0, -120));

            var highScoreText = CreateTMPText(playingPanel.transform, "HighScoreText",
                "Best: 0", 36, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(1, 1),
                new Vector2(0, -120), new Vector2(0, -180));

            // Next ball preview group
            GameObject nextBallGroup = new GameObject("NextBallGroup");
            nextBallGroup.transform.SetParent(playingPanel.transform, false);
            var nextGroupRect = nextBallGroup.AddComponent<RectTransform>();
            nextGroupRect.anchorMin = new Vector2(1, 1);
            nextGroupRect.anchorMax = new Vector2(1, 1);
            nextGroupRect.pivot = new Vector2(1, 1);
            nextGroupRect.anchoredPosition = new Vector2(-40, -30);
            nextGroupRect.sizeDelta = new Vector2(140, 160);

            CreateTMPText(nextBallGroup.transform, "NextLabel",
                "NEXT", 28, TextAlignmentOptions.Center,
                new Vector2(0, 1), new Vector2(0.5f, 1), new Vector2(1, 1),
                new Vector2(0, 0), new Vector2(0, -40));

            GameObject nextBallPreviewObj = new GameObject("NextBallPreview");
            nextBallPreviewObj.transform.SetParent(nextBallGroup.transform, false);
            var previewRect = nextBallPreviewObj.AddComponent<RectTransform>();
            previewRect.anchorMin = new Vector2(0.5f, 0);
            previewRect.anchorMax = new Vector2(0.5f, 1);
            previewRect.pivot = new Vector2(0.5f, 0.5f);
            previewRect.anchoredPosition = new Vector2(0, -80);
            previewRect.sizeDelta = new Vector2(80, 80);
            var nextBallImage = nextBallPreviewObj.AddComponent<Image>();
            Sprite tier1Sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Sprites/Ball_Tier1.png");
            if (tier1Sprite != null) nextBallImage.sprite = tier1Sprite;
            nextBallImage.preserveAspect = true;
            nextBallImage.color = Color.white;

            // Next ball label (hidden in visual overhaul — no numbers on balls)
            GameObject nextBallLabelObj = new GameObject("NextBallLabel");
            nextBallLabelObj.transform.SetParent(nextBallPreviewObj.transform, false);
            var labelRect = nextBallLabelObj.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var nextBallLabelTMP = nextBallLabelObj.AddComponent<TextMeshProUGUI>();
            nextBallLabelTMP.text = "";
            nextBallLabelTMP.alignment = TextAlignmentOptions.Center;
            nextBallLabelTMP.color = Color.white;
            nextBallLabelTMP.enableAutoSizing = true;
            nextBallLabelTMP.fontSizeMin = 8;
            nextBallLabelTMP.fontSizeMax = 40;
            nextBallLabelObj.SetActive(false); // Hidden — tiers by color/size only

            // Shake button
            var shakeButton = CreateButton(playingPanel.transform, "ShakeButton",
                "SHAKE", 32,
                new Color(0.8f, 0.4f, 0.2f),
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0));
            var shakeBtnRect = shakeButton.GetComponent<RectTransform>();
            shakeBtnRect.anchorMin = new Vector2(0, 0);
            shakeBtnRect.anchorMax = new Vector2(0, 0);
            shakeBtnRect.pivot = new Vector2(0, 0);
            shakeBtnRect.anchoredPosition = new Vector2(30, 30);
            shakeBtnRect.sizeDelta = new Vector2(200, 80);

            var shakeCountText = CreateTMPText(playingPanel.transform, "ShakeCountText",
                "Shake x3", 28, TextAlignmentOptions.Center,
                new Vector2(0, 0), new Vector2(0, 0), new Vector2(0, 0),
                Vector2.zero, Vector2.zero);
            var shakeCountRect = shakeCountText.GetComponent<RectTransform>();
            shakeCountRect.anchorMin = new Vector2(0, 0);
            shakeCountRect.anchorMax = new Vector2(0, 0);
            shakeCountRect.pivot = new Vector2(0, 0);
            shakeCountRect.anchoredPosition = new Vector2(30, 115);
            shakeCountRect.sizeDelta = new Vector2(200, 40);

            // Live rank display (modular component)
            GameObject liveRankObj = new GameObject("LiveRankDisplay");
            liveRankObj.transform.SetParent(playingPanel.transform, false);
            var liveRankRect = liveRankObj.AddComponent<RectTransform>();
            liveRankRect.anchorMin = new Vector2(0, 1);
            liveRankRect.anchorMax = new Vector2(0, 1);
            liveRankRect.pivot = new Vector2(0, 1);
            liveRankRect.anchoredPosition = new Vector2(30, -30);
            liveRankRect.sizeDelta = new Vector2(200, 50);
            var liveRankUI = liveRankObj.AddComponent<LiveRankUI>();
            var liveRankText = CreateTMPText(liveRankObj.transform, "RankText",
                "", 28, TextAlignmentOptions.Left,
                Vector2.zero, new Vector2(0, 0.5f), Vector2.one,
                Vector2.zero, Vector2.zero);

            // Streak display (modular component)
            GameObject streakObj = new GameObject("StreakDisplay");
            streakObj.transform.SetParent(playingPanel.transform, false);
            var streakRect = streakObj.AddComponent<RectTransform>();
            streakRect.anchorMin = new Vector2(0.5f, 1);
            streakRect.anchorMax = new Vector2(0.5f, 1);
            streakRect.pivot = new Vector2(0.5f, 1);
            streakRect.anchoredPosition = new Vector2(0, -180);
            streakRect.sizeDelta = new Vector2(300, 40);
            var streakUI = streakObj.AddComponent<StreakUI>();
            var streakText = CreateTMPText(streakObj.transform, "StreakText",
                "", 24, TextAlignmentOptions.Center,
                Vector2.zero, new Vector2(0.5f, 0.5f), Vector2.one,
                Vector2.zero, Vector2.zero);
            // Wire StreakUI
            var streakUISO = new SerializedObject(streakUI);
            streakUISO.FindProperty("streakText").objectReferenceValue = streakText;
            streakUISO.ApplyModifiedPropertiesWithoutUndo();

            // Wire LiveRankUI
            var liveRankSO = new SerializedObject(liveRankUI);
            liveRankSO.FindProperty("rankText").objectReferenceValue = liveRankText;
            liveRankSO.ApplyModifiedPropertiesWithoutUndo();

            // === MENU PANEL ===
            GameObject menuPanel = CreatePanel(canvasObj.transform, "MenuPanel");
            var menuBg = menuPanel.AddComponent<Image>();
            menuBg.color = new Color(0.07f, 0.07f, 0.1f, 0.9f);

            CreateTMPText(menuPanel.transform, "TitleText",
                "DAILY DROP", 96, TextAlignmentOptions.Center,
                new Vector2(0, 0.5f), new Vector2(0.5f, 0.7f), new Vector2(1, 0.9f),
                Vector2.zero, Vector2.zero);

            var playButton = CreateButton(menuPanel.transform, "PlayButton",
                "PLAY", 48,
                HexColor("0D9488"),
                new Vector2(0.3f, 0.25f), new Vector2(0.5f, 0.35f), new Vector2(0.7f, 0.45f));

            // === GAME OVER PANEL ===
            GameObject gameOverPanel = CreatePanel(canvasObj.transform, "GameOverPanel");
            var gameOverBg = gameOverPanel.AddComponent<Image>();
            gameOverBg.color = new Color(0.07f, 0.07f, 0.1f, 0.85f);

            CreateTMPText(gameOverPanel.transform, "GameOverTitle",
                "GAME OVER", 80, TextAlignmentOptions.Center,
                new Vector2(0, 0.6f), new Vector2(0.5f, 0.7f), new Vector2(1, 0.8f),
                Vector2.zero, Vector2.zero);

            var finalScoreText = CreateTMPText(gameOverPanel.transform, "FinalScoreText",
                "Score: 0", 56, TextAlignmentOptions.Center,
                new Vector2(0.1f, 0.5f), new Vector2(0.5f, 0.55f), new Vector2(0.9f, 0.65f),
                Vector2.zero, Vector2.zero);

            var finalHighScoreText = CreateTMPText(gameOverPanel.transform, "FinalHighScoreText",
                "Best: 0", 40, TextAlignmentOptions.Center,
                new Vector2(0.1f, 0.42f), new Vector2(0.5f, 0.47f), new Vector2(0.9f, 0.52f),
                Vector2.zero, Vector2.zero);

            var restartButton = CreateButton(gameOverPanel.transform, "RestartButton",
                "PLAY AGAIN", 44,
                HexColor("3B82F6"),
                new Vector2(0.25f, 0.2f), new Vector2(0.5f, 0.3f), new Vector2(0.75f, 0.4f));

            // Share button
            var shareButton = CreateButton(gameOverPanel.transform, "ShareButton",
                "SHARE", 36,
                HexColor("8B5CF6"),
                new Vector2(0.3f, 0.1f), new Vector2(0.5f, 0.15f), new Vector2(0.7f, 0.2f));

            gameOverPanel.SetActive(false);

            // === UIManager ===
            var uiManager = canvasObj.AddComponent<UIManager>();

            var uiSO = new SerializedObject(uiManager);
            uiSO.FindProperty("menuPanel").objectReferenceValue = menuPanel;
            uiSO.FindProperty("playingPanel").objectReferenceValue = playingPanel;
            uiSO.FindProperty("gameOverPanel").objectReferenceValue = gameOverPanel;
            uiSO.FindProperty("playButton").objectReferenceValue = playButton.GetComponent<Button>();
            uiSO.FindProperty("scoreText").objectReferenceValue = scoreText;
            uiSO.FindProperty("highScoreText").objectReferenceValue = highScoreText;
            uiSO.FindProperty("nextBallPreview").objectReferenceValue = nextBallImage;
            uiSO.FindProperty("nextBallLabel").objectReferenceValue = nextBallLabelTMP;
            uiSO.FindProperty("shakeButton").objectReferenceValue = shakeButton.GetComponent<Button>();
            uiSO.FindProperty("shakeCountText").objectReferenceValue = shakeCountText;
            uiSO.FindProperty("finalScoreText").objectReferenceValue = finalScoreText;
            uiSO.FindProperty("finalHighScoreText").objectReferenceValue = finalHighScoreText;
            uiSO.FindProperty("restartButton").objectReferenceValue = restartButton.GetComponent<Button>();
            uiSO.FindProperty("tierConfig").objectReferenceValue = tierConfig;
            uiSO.ApplyModifiedPropertiesWithoutUndo();

            return (canvasObj, uiManager);
        }

        // ========== HELPERS ==========

        private static Color HexColor(string hex)
        {
            ColorUtility.TryParseHtmlString($"#{hex}", out Color c);
            return c;
        }

        private static GameObject CreatePanel(Transform parent, string name)
        {
            GameObject panel = new GameObject(name);
            panel.transform.SetParent(parent, false);
            var rect = panel.AddComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            return panel;
        }

        private static TextMeshProUGUI CreateTMPText(Transform parent, string name,
            string text, float fontSize, TextAlignmentOptions alignment,
            Vector2 anchorMin, Vector2 pivot, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            var rect = obj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;

            var tmp = obj.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = alignment;
            tmp.color = Color.white;
            return tmp;
        }

        private static GameObject CreateButton(Transform parent, string name,
            string label, float fontSize, Color bgColor,
            Vector2 anchorMin, Vector2 pivot, Vector2 anchorMax)
        {
            GameObject btnObj = new GameObject(name);
            btnObj.transform.SetParent(parent, false);
            var rect = btnObj.AddComponent<RectTransform>();
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            var image = btnObj.AddComponent<Image>();
            image.color = bgColor;

            var button = btnObj.AddComponent<Button>();
            var colors = button.colors;
            colors.normalColor = bgColor;
            colors.highlightedColor = bgColor * 1.2f;
            colors.pressedColor = bgColor * 0.8f;
            button.colors = colors;

            GameObject textObj = new GameObject("Text");
            textObj.transform.SetParent(btnObj.transform, false);
            var textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;

            var tmp = textObj.AddComponent<TextMeshProUGUI>();
            tmp.text = label;
            tmp.fontSize = fontSize;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            return btnObj;
        }

        private static void EnsureDirectory(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string parent = System.IO.Path.GetDirectoryName(path).Replace("\\", "/");
                string folder = System.IO.Path.GetFileName(path);
                if (!AssetDatabase.IsValidFolder(parent))
                    EnsureDirectory(parent);
                AssetDatabase.CreateFolder(parent, folder);
            }
        }
    }
}
