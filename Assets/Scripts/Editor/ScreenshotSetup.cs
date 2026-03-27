using UnityEngine;
using UnityEditor;
using System.Reflection;
using MergeGame.UI;

namespace MergeGame.Editor
{
    /// <summary>
    /// Sets up fake player data and Game View sizes for App Store screenshots.
    ///
    /// Workflow:
    /// 1. "Add Screenshot Sizes" — adds all required resolutions to Game View dropdown
    /// 2. Set data (Home/Results/Onboarding) + Freeze Merge Ball
    /// 3. Enter Play mode, select a size from Game View dropdown, press F12
    /// 4. Repeat for each size and screen
    /// 5. "Reset All Screenshot Data" when done
    /// </summary>
    public static class ScreenshotSetup
    {
        // ───── Game View sizes ─────

        private static readonly (string name, int w, int h)[] Sizes = new[]
        {
            ("iPhone 6.9in",  1320, 2868),  // iPhone 16 Pro Max
            ("iPhone 6.7in",  1290, 2796),  // iPhone 15 Pro Max
            ("iPhone 6.5in",  1284, 2778),  // iPhone 14 Plus
            ("iPhone 5.5in",  1242, 2208),  // iPhone 8 Plus
            ("Android phone", 1080, 1920),  // Google Play phone
        };

        [MenuItem("MergeGame/Screenshot Setup/Add Screenshot Sizes to Game View", false, 50)]
        public static void AddScreenshotSizes()
        {
            int added = 0;
            foreach (var s in Sizes)
            {
                if (!CustomSizeExists(s.w, s.h))
                {
                    AddGameViewSize(s.w, s.h, s.name);
                    added++;
                }
            }

            if (added > 0)
                Debug.Log($"[ScreenshotSetup] Added {added} sizes to Game View dropdown. Select one and press F12 to capture.");
            else
                Debug.Log("[ScreenshotSetup] All screenshot sizes already exist in Game View.");
        }

        // ───── Merge ball freeze ─────

        [MenuItem("MergeGame/Screenshot Setup/Freeze Merge Ball (Tier 10)", false, 55)]
        public static void FreezeMergeBall()
        {
            HomeMergeLoop.ScreenshotStaticTier = 10;
            Debug.Log("[ScreenshotSetup] Merge animation frozen at tier 10. Enter Play mode to see it.");
        }

        [MenuItem("MergeGame/Screenshot Setup/Unfreeze Merge Ball", false, 56)]
        public static void UnfreezeMergeBall()
        {
            HomeMergeLoop.ScreenshotStaticTier = null;
            Debug.Log("[ScreenshotSetup] Merge animation restored to normal loop.");
        }

        // ───── Data setup ─────

        [MenuItem("MergeGame/Screenshot Setup/Set Home Screen Data", false, 60)]
        public static void SetHomeScreenData()
        {
            PlayerPrefs.SetInt("HighScore", 847);
            PlayerPrefs.SetInt("current_streak", 5);
            PlayerPrefs.SetString("last_streak_date",
                System.DateTime.Now.ToString("yyyy-MM-dd"));

            string today = System.DateTime.Now.ToString("yyyy-MM-dd");
            PlayerPrefs.SetInt($"scored_attempt_{today}", 1);
            PlayerPrefs.SetString($"merge_counts_{today}", "4,6,5,3,2,1,1,0,0,0,0");
            PlayerPrefs.SetString("player_display_name", "You");
            PlayerPrefs.SetInt("screenshot_rank", 4);
            PlayerPrefs.SetInt("screenshot_total_players", 28);

            PlayerPrefs.Save();
            Debug.Log("[ScreenshotSetup] Home screen data set. Enter Play mode to see it.");
        }

        [MenuItem("MergeGame/Screenshot Setup/Set Results Data", false, 61)]
        public static void SetResultsData()
        {
            PlayerPrefs.SetInt("HighScore", 847);
            PlayerPrefs.SetInt("current_streak", 5);
            PlayerPrefs.SetString("last_streak_date",
                System.DateTime.Now.ToString("yyyy-MM-dd"));
            PlayerPrefs.SetString("player_display_name", "You");
            PlayerPrefs.SetInt("screenshot_rank", 4);
            PlayerPrefs.SetInt("screenshot_total_players", 28);

            PlayerPrefs.Save();
            Debug.Log("[ScreenshotSetup] Results data set. Play a game and finish for the results screen.");
        }

        [MenuItem("MergeGame/Screenshot Setup/Set First Launch (Onboarding)", false, 62)]
        public static void SetFirstLaunch()
        {
            PlayerPrefs.DeleteKey("player_uuid_initialized");
            PlayerPrefs.Save();
            Debug.Log("[ScreenshotSetup] First launch set. Enter Play mode to see onboarding.");
        }

        [MenuItem("MergeGame/Screenshot Setup/Reset All Screenshot Data", false, 70)]
        public static void ResetScreenshotData()
        {
            PlayerPrefs.DeleteAll();
            PlayerPrefs.Save();
            Debug.Log("[ScreenshotSetup] All PlayerPrefs cleared. Fresh state.");
        }

        // ───── Game View reflection helpers ─────

        private static object GetCurrentGroup()
        {
            var sizesType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizes");
            var singleType = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instance = singleType.GetProperty("instance",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic).GetValue(null);
            // Use the current build target's group (iOS, Android, etc.) — not Standalone
            var currentGroupProp = sizesType.GetProperty("currentGroupIndex",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            int groupIndex = currentGroupProp != null
                ? (int)currentGroupProp.GetValue(instance)
                : (int)GameViewSizeGroupType.Standalone;
            return sizesType.GetMethod("GetGroup")
                .Invoke(instance, new object[] { groupIndex });
        }

        private static void AddGameViewSize(int width, int height, string label)
        {
            var group = GetCurrentGroup();
            var gvSizeType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSize");
            var gvSizeTypeEnum = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSizeType");
            var fixedRes = System.Enum.GetValues(gvSizeTypeEnum).GetValue(1);
            var ctor = gvSizeType.GetConstructor(new[] { gvSizeTypeEnum, typeof(int), typeof(int), typeof(string) });
            var newSize = ctor.Invoke(new object[] { fixedRes, width, height, label });
            group.GetType().GetMethod("AddCustomSize").Invoke(group, new[] { newSize });
        }

        private static bool CustomSizeExists(int width, int height)
        {
            var group = GetCurrentGroup();
            var groupType = group.GetType();
            int total = (int)groupType.GetMethod("GetTotalCount").Invoke(group, null);
            var getSize = groupType.GetMethod("GetGameViewSize");
            var gvSizeType = typeof(UnityEditor.Editor).Assembly.GetType("UnityEditor.GameViewSize");
            var widthProp = gvSizeType.GetProperty("width");
            var heightProp = gvSizeType.GetProperty("height");

            for (int i = 0; i < total; i++)
            {
                var size = getSize.Invoke(group, new object[] { i });
                int w = (int)widthProp.GetValue(size);
                int h = (int)heightProp.GetValue(size);
                if (w == width && h == height) return true;
            }
            return false;
        }
    }
}
