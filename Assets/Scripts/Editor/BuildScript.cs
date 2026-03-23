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
            foreach (string arg in System.Environment.GetCommandLineArgs())
            {
                if (arg == "-development") isDev = true;
            }
            Build(isDev);
        }

        private static void Build(bool development)
        {
            string buildPath = "Build/iOS";

            // Set bundle ID based on environment
            string bundleId = development ? "com.overtone.game.dev" : "com.overtone.game";
#pragma warning disable CS0618
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, bundleId);
#pragma warning restore CS0618

            var options = new BuildPlayerOptions
            {
                scenes = new[] { "Assets/Scenes/GameScene.unity" },
                locationPathName = buildPath,
                target = BuildTarget.iOS,
                options = development
                    ? BuildOptions.Development
                    : BuildOptions.None,
            };

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
#endif
        }
    }
}
