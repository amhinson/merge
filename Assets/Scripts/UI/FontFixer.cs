using UnityEngine;
using TMPro;

namespace MergeGame.UI
{
    /// <summary>
    /// Fixes pixel font rendering: forces the Bitmap shader on raster fonts
    /// and strips all outline/glow/underlay effects.
    /// Runs for several frames to catch late-initialized TMP components.
    /// </summary>
    public class FontFixer : MonoBehaviour
    {
        private int fixFrames = 5;

        private void Update()
        {
            if (fixFrames > 0)
            {
                fixFrames--;
                FixAllTMPComponents();
            }
        }

        public static void FixAllTMPComponents()
        {
            // Find the bitmap shader
            Shader bitmapShader = Shader.Find("TextMeshPro/Bitmap");

            var uiTexts = FindObjectsByType<TextMeshProUGUI>(FindObjectsSortMode.None);
            foreach (var tmp in uiTexts)
                FixTMP(tmp, bitmapShader);

            var worldTexts = FindObjectsByType<TextMeshPro>(FindObjectsSortMode.None);
            foreach (var tmp in worldTexts)
            {
                if (tmp == null) continue;
                tmp.outlineWidth = 0f;
                tmp.outlineColor = Color.clear;
            }
        }

        private static void FixTMP(TextMeshProUGUI tmp, Shader bitmapShader)
        {
            if (tmp == null) return;

            // Remove outline and effects set on the component
            tmp.outlineWidth = 0f;
            tmp.outlineColor = Color.clear;
            tmp.fontStyle &= ~FontStyles.Bold;

            // Switch to bitmap shader if available — this eliminates SDF smoothing/outline
            if (bitmapShader != null && tmp.fontMaterial != null)
            {
                if (!tmp.fontMaterial.shader.name.Contains("Bitmap"))
                {
                    tmp.fontMaterial.shader = bitmapShader;
                }
            }

            // Also fix the shared material on the font asset itself
            if (tmp.font != null && tmp.font.material != null)
            {
                Material fontMat = tmp.font.material;

                if (bitmapShader != null && !fontMat.shader.name.Contains("Bitmap"))
                {
                    fontMat.shader = bitmapShader;
                }

                // Strip all effects regardless of shader
                StripEffects(fontMat);
            }

            // Strip effects on the instance material too
            if (tmp.fontMaterial != null)
            {
                StripEffects(tmp.fontMaterial);
            }
        }

        private static void StripEffects(Material mat)
        {
            if (mat == null) return;

            if (mat.HasProperty("_OutlineWidth"))
                mat.SetFloat("_OutlineWidth", 0f);
            if (mat.HasProperty("_OutlineColor"))
                mat.SetColor("_OutlineColor", Color.clear);
            if (mat.HasProperty("_UnderlayColor"))
                mat.SetColor("_UnderlayColor", Color.clear);
            if (mat.HasProperty("_UnderlayOffsetX"))
                mat.SetFloat("_UnderlayOffsetX", 0f);
            if (mat.HasProperty("_UnderlayOffsetY"))
                mat.SetFloat("_UnderlayOffsetY", 0f);
            if (mat.HasProperty("_UnderlaySoftness"))
                mat.SetFloat("_UnderlaySoftness", 0f);
            if (mat.HasProperty("_GlowColor"))
                mat.SetColor("_GlowColor", Color.clear);
            if (mat.HasProperty("_GlowPower"))
                mat.SetFloat("_GlowPower", 0f);
            if (mat.HasProperty("_FaceColor"))
            {
                Color face = mat.GetColor("_FaceColor");
                face.a = 1f;
                mat.SetColor("_FaceColor", face);
            }
            if (mat.HasProperty("_Sharpness"))
                mat.SetFloat("_Sharpness", 1f);
        }
    }
}
