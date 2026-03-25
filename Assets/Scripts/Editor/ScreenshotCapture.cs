using UnityEngine;
using System.IO;

namespace MergeGame.Core
{
    /// <summary>
    /// Press F12 during play to capture a screenshot.
    /// Saves to Screenshots/ folder in the project root.
    /// Editor-only — stripped from builds.
    /// </summary>
    public class ScreenshotCapture : MonoBehaviour
    {
#if UNITY_EDITOR
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F12))
            {
                string dir = Path.Combine(Application.dataPath, "..", "Screenshots");
                Directory.CreateDirectory(dir);

                string timestamp = System.DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
                int w = Screen.width;
                int h = Screen.height;
                string filename = $"screenshot_{w}x{h}_{timestamp}.png";
                string path = Path.Combine(dir, filename);

                ScreenCapture.CaptureScreenshot(path);
                Debug.Log($"[Screenshot] Saved: {path}");
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void AutoCreate()
        {
            var go = new GameObject("ScreenshotCapture");
            go.AddComponent<ScreenshotCapture>();
            go.hideFlags = HideFlags.HideInHierarchy;
        }
#endif
    }
}
