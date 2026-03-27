using UnityEngine;
using UnityEditor;
using System.IO;

namespace MergeGame.Editor
{
    /// <summary>
    /// Capture screenshot via menu: MergeGame > Screenshot Setup > Capture (Alt+S)
    /// Saves to Screenshots/ folder in the project root.
    /// </summary>
    public static class ScreenshotCapture
    {
        [MenuItem("MergeGame/Screenshot Setup/Capture _&s", false, 51)]
        public static void Capture()
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.LogWarning("[Screenshot] Enter Play mode first.");
                return;
            }

            string dir = Path.Combine(Application.dataPath, "..", "Screenshots");
            Directory.CreateDirectory(dir);

            string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
            string path = Path.Combine(dir, $"screenshot_{timestamp}.png");

            UnityEngine.ScreenCapture.CaptureScreenshot(path);
            Debug.Log($"[Screenshot] Saved: screenshot_{timestamp}.png");
        }
    }
}
