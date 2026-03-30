#if UNITY_IOS
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEditor.iOS.Xcode;
using System.IO;

namespace MergeGame.Editor
{
    /// <summary>
    /// Post-build processor for iOS: adds entitlements and frameworks
    /// needed for Apple Sign In.
    /// </summary>
    public static class iOSPostBuild
    {
        [PostProcessBuild(100)]
        public static void OnPostProcessBuild(BuildTarget target, string path)
        {
            if (target != BuildTarget.iOS) return;

            AddSignInWithAppleCapability(path);
            AddAuthenticationServicesFramework(path);
            AddGoogleSignInURLScheme(path);
            SetLaunchScreenBackground(path);
            CreatePodfileIfNeeded(path);
            RunPodInstall(path);
            AddPodsHeaderSearchPaths(path);
        }

        private static void AddSignInWithAppleCapability(string path)
        {
            // Create or update the entitlements file
            string entitlementsFileName = "Unity-iPhone.entitlements";
            string entitlementsPath = Path.Combine(path, entitlementsFileName);

            PlistDocument entitlements;
            if (File.Exists(entitlementsPath))
            {
                entitlements = new PlistDocument();
                entitlements.ReadFromFile(entitlementsPath);
            }
            else
            {
                entitlements = new PlistDocument();
            }

            // Add Sign in with Apple entitlement
            var root = entitlements.root;
            var signInArray = root.CreateArray("com.apple.developer.applesignin");
            signInArray.AddString("Default");

            entitlements.WriteToFile(entitlementsPath);

            // Add entitlements file to Xcode project
            string projectPath = PBXProject.GetPBXProjectPath(path);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);

            string mainTarget = project.GetUnityMainTargetGuid();
            project.AddFile(entitlementsPath, entitlementsFileName);
            project.SetBuildProperty(mainTarget, "CODE_SIGN_ENTITLEMENTS", entitlementsFileName);

            project.WriteToFile(projectPath);
        }

        private static void AddPodsHeaderSearchPaths(string path)
        {
            string projectPath = PBXProject.GetPBXProjectPath(path);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);

            string mainTarget = project.GetUnityMainTargetGuid();

            // Append Pods header paths (using AddBuildProperty preserves existing + $(inherited))
            project.AddBuildProperty(mainTarget, "HEADER_SEARCH_PATHS", "\"${PODS_ROOT}/Headers/Public\"");
            project.AddBuildProperty(mainTarget, "HEADER_SEARCH_PATHS", "\"${PODS_CONFIGURATION_BUILD_DIR}/GoogleSignIn/GoogleSignIn.framework/Headers\"");

            project.WriteToFile(projectPath);
        }

        private static void SetLaunchScreenBackground(string path)
        {
            // Patch the launch storyboard to use dark background matching OC.bg (#0F1117)
            string storyboardPath = Path.Combine(path, "LaunchScreen-iPhone.storyboard");
            if (!File.Exists(storyboardPath))
                storyboardPath = Path.Combine(path, "LaunchScreen.storyboard");
            if (!File.Exists(storyboardPath)) return;

            string content = File.ReadAllText(storyboardPath);

            // Replace the default system background color with our dark bg
            // The storyboard uses systemBackgroundColor by default (white in light mode)
            if (content.Contains("systemBackgroundColor"))
            {
                content = content.Replace(
                    "systemBackgroundColor",
                    "systemBackgroundColor\" key=\"REPLACED");
                // Actually, just replace the color element directly
            }

            // Simpler approach: find the view's background color and replace it
            // The storyboard XML has a <color> element for the background
            string oldColor = "<color key=\"backgroundColor\" systemColor=\"systemBackgroundColor\"/>";
            string newColor = "<color key=\"backgroundColor\" red=\"0.059\" green=\"0.067\" blue=\"0.090\" alpha=\"1\" colorSpace=\"custom\" customColorSpace=\"sRGB\"/>";

            if (content.Contains(oldColor))
            {
                content = content.Replace(oldColor, newColor);
                File.WriteAllText(storyboardPath, content);
                UnityEngine.Debug.Log("[iOSPostBuild] Launch screen background set to dark");
            }
            else
            {
                // Try alternate format
                string altOld = "systemColor=\"systemBackgroundColor\"";
                string altNew = "red=\"0.059\" green=\"0.067\" blue=\"0.090\" alpha=\"1\" colorSpace=\"custom\" customColorSpace=\"sRGB\"";
                if (content.Contains(altOld))
                {
                    content = content.Replace(altOld, altNew);
                    File.WriteAllText(storyboardPath, content);
                    UnityEngine.Debug.Log("[iOSPostBuild] Launch screen background set to dark (alt format)");
                }
            }
        }

        private static void AddGoogleSignInURLScheme(string path)
        {
            // Google Sign In requires a URL scheme matching the reversed iOS client ID
            string plistPath = Path.Combine(path, "Info.plist");
            var plist = new PlistDocument();
            plist.ReadFromFile(plistPath);

            // The URL scheme is the reversed client ID
            string reversedClientId = "com.googleusercontent.apps.192938409753-bmputs91st1210bnvhk4295g5ccubhjj";

            var urlTypes = plist.root["CFBundleURLTypes"]?.AsArray();
            if (urlTypes == null)
            {
                urlTypes = plist.root.CreateArray("CFBundleURLTypes");
            }

            // Check if already added
            bool found = false;
            foreach (var item in urlTypes.values)
            {
                var dict = item.AsDict();
                var schemes = dict?["CFBundleURLSchemes"]?.AsArray();
                if (schemes != null)
                {
                    foreach (var s in schemes.values)
                    {
                        if (s.AsString() == reversedClientId) { found = true; break; }
                    }
                }
                if (found) break;
            }

            if (!found)
            {
                var newEntry = urlTypes.AddDict();
                newEntry.SetString("CFBundleTypeRole", "Editor");
                var schemes = newEntry.CreateArray("CFBundleURLSchemes");
                schemes.AddString(reversedClientId);
            }

            plist.WriteToFile(plistPath);
        }

        private static void AddAuthenticationServicesFramework(string path)
        {
            string projectPath = PBXProject.GetPBXProjectPath(path);
            var project = new PBXProject();
            project.ReadFromFile(projectPath);

            // Add to both targets — plugins compile in UnityFramework
            string mainTarget = project.GetUnityMainTargetGuid();
            string frameworkTarget = project.GetUnityFrameworkTargetGuid();
            project.AddFrameworkToProject(mainTarget, "AuthenticationServices.framework", false);
            project.AddFrameworkToProject(frameworkTarget, "AuthenticationServices.framework", false);

            project.WriteToFile(projectPath);
        }

        /// <summary>
        /// Create a Podfile for Google Sign In SDK if it doesn't exist.
        /// Run `pod install` manually after building.
        /// </summary>
        private static void CreatePodfileIfNeeded(string path)
        {
            string podfilePath = Path.Combine(path, "Podfile");
            if (File.Exists(podfilePath)) return;

            string podfile = @"platform :ios, '13.0'
use_frameworks! :linkage => :static

target 'UnityFramework' do
  pod 'GoogleSignIn', '~> 7.0'
end

post_install do |installer|
  installer.pods_project.targets.each do |target|
    target.build_configurations.each do |config|
      config.build_settings['IPHONEOS_DEPLOYMENT_TARGET'] = '13.0'
    end
  end
end
";
            File.WriteAllText(podfilePath, podfile);
            UnityEngine.Debug.Log("[iOSPostBuild] Created Podfile for Google Sign In.");
        }

        private static void RunPodInstall(string path)
        {
            string podfilePath = Path.Combine(path, "Podfile");
            if (!File.Exists(podfilePath)) return;

            try
            {
                var process = new System.Diagnostics.Process();
                process.StartInfo.FileName = "/bin/bash";
                process.StartInfo.Arguments = $"-c \"cd '{path}' && pod install\"";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.Start();
                process.WaitForExit(60000);

                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();

                if (process.ExitCode == 0)
                    UnityEngine.Debug.Log($"[iOSPostBuild] pod install completed:\n{output}");
                else
                    UnityEngine.Debug.LogWarning($"[iOSPostBuild] pod install failed:\n{error}");
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogWarning($"[iOSPostBuild] Could not run pod install: {e.Message}");
            }
        }
    }
}
#endif
