using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MergeGame.Editor
{
    public static class BuildScript
    {
        [MenuItem("MergeGame/Build iOS (Dev)", false, 30)]
        public static void BuildiOSDev()
        {
            Build(true);
        }

        [MenuItem("MergeGame/Build iOS (Prod)", false, 31)]
        public static void BuildiOSProd()
        {
            Build(false);
        }

        /// <summary>
        /// Called from command line: -executeMethod BuildScript.BuildiOS
        /// Checks for -development flag to determine environment.
        /// </summary>
        public static void BuildiOS()
        {
            bool isDev = false;
            string buildNumber = null;
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-development") isDev = true;
                if (args[i] == "-buildNumber" && i + 1 < args.Length) buildNumber = args[i + 1];
            }
            Build(isDev, buildNumber);
        }

        private static void Build(bool development, string buildNumber = null)
        {
            string buildPath = "Build/iOS";

            // Set bundle ID based on environment
            string bundleId = development ? "com.murge.game.dev" : "com.murge.game";
#pragma warning disable CS0618
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, bundleId);
#pragma warning restore CS0618

            // App metadata
            PlayerSettings.productName = "Murge";
            PlayerSettings.companyName = "Murge";
            PlayerSettings.bundleVersion = "0.3.0";              // display version (CFBundleShortVersionString)
            PlayerSettings.iOS.buildNumber = buildNumber ?? "1";  // build number (CFBundleVersion)
            PlayerSettings.iOS.applicationDisplayName = "Murge";
            PlayerSettings.iOS.appInBackgroundBehavior = iOSAppInBackgroundBehavior.Suspend;

            // Lock to portrait only
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/GameScene.unity" },
                locationPathName = buildPath,
                target = BuildTarget.iOS,
                options = development
                    ? BuildOptions.Development
                    : BuildOptions.None,
            };

            WriteBuildNumberResource(buildNumber ?? "1");
            Debug.Log($"Building iOS ({(development ? "DEV" : "PROD")}) bundle={bundleId} to {buildPath}");

            BuildReport report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Build succeeded: {report.summary.totalSize} bytes");
                PatchXcodeProject(buildPath);
            }
            else
            {
                Debug.LogError($"Build failed: {report.summary.result}");
                // Exit with error code for CI
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
            }
        }
        // ===== Android =====

        [MenuItem("MergeGame/Build Android (Dev)", false, 40)]
        public static void BuildAndroidDev()
        {
            BuildAndroidInternal(true);
        }

        [MenuItem("MergeGame/Build Android (Prod AAB)", false, 41)]
        public static void BuildAndroidProd()
        {
            BuildAndroidInternal(false);
        }

        [MenuItem("MergeGame/Build Android (Prod APK)", false, 42)]
        public static void BuildAndroidProdApk()
        {
            BuildAndroidInternal(false, null, forceApk: true);
        }

        /// <summary>
        /// Called from command line: -executeMethod BuildScript.BuildAndroid
        /// </summary>
        public static void BuildAndroid()
        {
            bool isDev = false;
            bool forceApk = false;
            string buildNumber = null;
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-development") isDev = true;
                if (args[i] == "-forceApk") forceApk = true;
                if (args[i] == "-buildNumber" && i + 1 < args.Length) buildNumber = args[i + 1];
            }
            BuildAndroidInternal(isDev, buildNumber, forceApk);
        }

        private static void BuildAndroidInternal(bool development, string buildNumber = null, bool forceApk = false)
        {
            string bundleId = development ? "com.murge.game.dev" : "com.murge.game";
#pragma warning disable CS0618
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.Android, bundleId);
#pragma warning restore CS0618

            PlayerSettings.productName = "Murge";
            PlayerSettings.companyName = "Murge";
            PlayerSettings.bundleVersion = "0.3.0";

            int versionCode = 1;
            if (buildNumber != null && int.TryParse(buildNumber, out int parsed))
                versionCode = parsed;
            PlayerSettings.Android.bundleVersionCode = versionCode;

            // Lock to portrait only
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;

            // Keystore — only use if keystore exists AND passwords are available
            string keystorePath = System.IO.Path.Combine(
                System.IO.Path.GetDirectoryName(Application.dataPath), "murge.keystore");
            string ksPass = System.Environment.GetEnvironmentVariable("MURGE_KEYSTORE_PASS");
            string kaPass = System.Environment.GetEnvironmentVariable("MURGE_KEY_ALIAS_PASS");

            if (!development && System.IO.File.Exists(keystorePath)
                && !string.IsNullOrEmpty(ksPass) && !string.IsNullOrEmpty(kaPass))
            {
                PlayerSettings.Android.useCustomKeystore = true;
                PlayerSettings.Android.keystoreName = keystorePath;
                PlayerSettings.Android.keystorePass = ksPass;
                PlayerSettings.Android.keyaliasPass = kaPass;
                PlayerSettings.Android.keyaliasName = "murge";
                Debug.Log("Android: Using custom keystore (signed release)");
            }
            else
            {
                PlayerSettings.Android.useCustomKeystore = false;
                if (!development)
                    Debug.Log("Android: No keystore/passwords — building unsigned (debug signed)");
            }

            // APK for dev (or if forced), AAB for prod
            bool buildAAB = !development && !forceApk;
            EditorUserBuildSettings.buildAppBundle = buildAAB;

            string ext = buildAAB ? "aab" : "apk";
            string buildPath = $"Build/Android/murge-{(development ? "dev" : "prod")}.{ext}";

            // Ensure output directory exists
            string dir = System.IO.Path.GetDirectoryName(buildPath);
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/GameScene.unity" },
                locationPathName = buildPath,
                target = BuildTarget.Android,
                options = development ? BuildOptions.Development : BuildOptions.None,
            };

            WriteBuildNumberResource(buildNumber ?? versionCode.ToString());
            Debug.Log($"Building Android ({(development ? "DEV" : "PROD")}) bundle={bundleId} format={ext} to {buildPath}");

            BuildReport report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"Android build succeeded: {report.summary.totalSize} bytes → {buildPath}");
            }
            else
            {
                Debug.LogError($"Android build failed: {report.summary.result}");
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
            }
        }

        // ===== WebGL =====

        [MenuItem("MergeGame/Build WebGL (Dev)", false, 50)]
        public static void BuildWebGLDev()
        {
            BuildWebGLInternal(true);
        }

        [MenuItem("MergeGame/Build WebGL (Prod)", false, 51)]
        public static void BuildWebGLProd()
        {
            BuildWebGLInternal(false);
        }

        /// <summary>
        /// Called from command line: -executeMethod BuildScript.BuildWebGL
        /// </summary>
        public static void BuildWebGL()
        {
            bool isDev = false;
            string buildNumber = null;
            var args = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-development") isDev = true;
                if (args[i] == "-buildNumber" && i + 1 < args.Length) buildNumber = args[i + 1];
            }
            BuildWebGLInternal(isDev, buildNumber);
        }

        private static void BuildWebGLInternal(bool development, string buildNumber = null)
        {
            PlayerSettings.productName = "Murge";
            PlayerSettings.companyName = "Murge";
            PlayerSettings.bundleVersion = "0.3.0";

            // WebGL-specific settings
            PlayerSettings.WebGL.compressionFormat = development
                ? WebGLCompressionFormat.Disabled
                : WebGLCompressionFormat.Brotli;
            PlayerSettings.WebGL.decompressionFallback = true;
            PlayerSettings.WebGL.template = "PROJECT:Murge";

            // Lock to portrait-ish — WebGL respects the browser viewport, but set a default
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;

            string buildPath = $"Build/WebGL/murge-{(development ? "dev" : "prod")}";

            // Ensure output directory exists
            if (!System.IO.Directory.Exists(buildPath))
                System.IO.Directory.CreateDirectory(buildPath);

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/GameScene.unity" },
                locationPathName = buildPath,
                target = BuildTarget.WebGL,
                options = development ? BuildOptions.Development : BuildOptions.None,
            };

            WriteBuildNumberResource(buildNumber ?? "1");
            Debug.Log($"Building WebGL ({(development ? "DEV" : "PROD")}) to {buildPath}");

            BuildReport report = BuildPipeline.BuildPlayer(options);

            if (report.summary.result == BuildResult.Succeeded)
            {
                Debug.Log($"WebGL build succeeded: {report.summary.totalSize} bytes → {buildPath}");
            }
            else
            {
                Debug.LogError($"WebGL build failed: {report.summary.result}");
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
            }
        }

        // ===== Shared Helpers =====

        private static void WriteBuildNumberResource(string buildNumber)
        {
            string dir = "Assets/Resources";
            if (!System.IO.Directory.Exists(dir))
                System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllText(System.IO.Path.Combine(dir, "build-number.txt"), buildNumber);
            AssetDatabase.Refresh();
        }

        // ===== iOS Helpers =====

        private static void PatchXcodeProject(string buildPath)
        {
#if UNITY_IOS
            string projPath = System.IO.Path.Combine(buildPath, "Unity-iPhone.xcodeproj/project.pbxproj");
            if (!System.IO.File.Exists(projPath)) return;

            string content = System.IO.File.ReadAllText(projPath);

            // Add CODE_SIGN_ALLOW_ENTITLEMENTS_MODIFICATION = YES to all build configs
            if (!content.Contains("CODE_SIGN_ALLOW_ENTITLEMENTS_MODIFICATION"))
            {
                content = content.Replace(
                    "CODE_SIGN_IDENTITY",
                    "CODE_SIGN_ALLOW_ENTITLEMENTS_MODIFICATION = YES;\n\t\t\t\tCODE_SIGN_IDENTITY");
                System.IO.File.WriteAllText(projPath, content);
                Debug.Log("Xcode project patched: CODE_SIGN_ALLOW_ENTITLEMENTS_MODIFICATION = YES");
            }

            // Patch Info.plist — add app category
            string plistPath = System.IO.Path.Combine(buildPath, "Info.plist");
            if (System.IO.File.Exists(plistPath))
            {
                string plist = System.IO.File.ReadAllText(plistPath);
                if (!plist.Contains("LSApplicationCategoryType"))
                {
                    plist = plist.Replace(
                        "</dict>\n</plist>",
                        "\t<key>LSApplicationCategoryType</key>\n\t<string>public.app-category.games</string>\n</dict>\n</plist>");
                    Debug.Log("Info.plist patched: LSApplicationCategoryType = Games");
                }
                if (!plist.Contains("ITSAppUsesNonExemptEncryption"))
                {
                    plist = plist.Replace(
                        "</dict>\n</plist>",
                        "\t<key>ITSAppUsesNonExemptEncryption</key>\n\t<false/>\n</dict>\n</plist>");
                    Debug.Log("Info.plist patched: ITSAppUsesNonExemptEncryption = NO");
                }
                System.IO.File.WriteAllText(plistPath, plist);
            }
#endif
        }
    }
}
