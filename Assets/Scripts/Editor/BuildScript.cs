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
            PlayerSettings.SetApplicationIdentifier(BuildTargetGroup.iOS, bundleId);

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
            }
            else
            {
                Debug.LogError($"Build failed: {report.summary.result}");
                // Exit with error code for CI
                if (Application.isBatchMode)
                    EditorApplication.Exit(1);
            }
        }
    }
}
