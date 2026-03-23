using UnityEngine;

namespace MergeGame.Data
{
    /// <summary>
    /// Overtone design system — colors, spacing, and font sizes.
    /// Single source of truth for all UI styling constants.
    /// </summary>
    public static class OC
    {
        // Backgrounds
        public static readonly Color bg      = Hex("#0F1117");
        public static readonly Color surface = Hex("#161B24");
        public static readonly Color border  = Hex("#232838");

        // Accent colors (ball colors + UI accents)
        public static readonly Color cyan   = Hex("#4DD9C0");
        public static readonly Color pink   = Hex("#E8587A");
        public static readonly Color amber  = Hex("#F0B429");
        public static readonly Color lime   = Hex("#A3E635");
        public static readonly Color violet = Hex("#A78BFA");
        public static readonly Color orange = Hex("#FB923C");
        public static readonly Color sky    = Hex("#38BDF8");
        public static readonly Color rose   = Hex("#FB7185");

        // Text
        public static readonly Color muted = Hex("#FFFFFF", 0.22f);
        public static readonly Color dim   = Hex("#FFFFFF", 0.10f);
        public static readonly Color white = Color.white;

        // Overlays
        public static readonly Color overlayDark     = Hex("#08080E", 0.88f);
        public static readonly Color shareCardBg     = Hex("#12141C");
        public static readonly Color shareCardBorder = Hex("#353A50");

        /// <summary>Return a color with a custom alpha multiplier.</summary>
        public static Color A(Color c, float alpha)
        {
            c.a = alpha;
            return c;
        }

        private static Color Hex(string hex, float alpha = 1f)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            c.a = alpha;
            return c;
        }
    }

    public static class OS
    {
        public const float screenPadH = 24f;
        public const float screenPadV = 16f;
        public const float cardRadius = 4f;
        public const float borderWidth = 1f;
        public const float safeAreaTop = 32f;
    }

    public static class OFont
    {
        // Press Start 2P sizes
        public const float title    = 24f;
        public const float heading  = 11f;
        public const float label    = 9f;
        public const float labelSm  = 8f;
        public const float labelXs  = 7f;
        public const float labelXxs = 6f;

        // DM Mono sizes
        public const float score     = 56f;
        public const float scoreGame = 28f;
        public const float bodyLg    = 14f;
        public const float body      = 13f;
        public const float bodySm    = 12f;
        public const float bodyXs    = 11f;
        public const float caption   = 10f;
    }
}
