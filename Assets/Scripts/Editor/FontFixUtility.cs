using UnityEngine;
using UnityEditor;
using TMPro;

namespace MergeGame.Editor
{
    public static class FontFixUtility
    {
        [MenuItem("MergeGame/Fix Font Transparency", false, 10)]
        public static void FixFontTransparency()
        {
            // Find all TMP font assets in the project
            string[] guids = AssetDatabase.FindAssets("t:TMP_FontAsset");

            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
                if (font == null) continue;

                // Fix the material
                Material mat = font.material;
                if (mat == null) continue;

                // Ensure shader supports transparency
                if (mat.shader.name.Contains("Bitmap"))
                {
                    // For bitmap/raster fonts, the shader should handle transparency
                    // Make sure the texture format is correct
                    if (font.atlasTexture != null)
                    {
                        string texPath = AssetDatabase.GetAssetPath(font.atlasTexture);
                        if (!string.IsNullOrEmpty(texPath))
                        {
                            TextureImporter importer = AssetImporter.GetAtPath(texPath) as TextureImporter;
                            if (importer != null)
                            {
                                importer.alphaIsTransparency = true;
                                importer.alphaSource = TextureImporterAlphaSource.FromInput;
                                importer.SaveAndReimport();
                            }
                        }
                    }
                }

                // Force face color to be fully opaque white (text color is set per-component)
                if (mat.HasProperty("_FaceColor"))
                {
                    mat.SetColor("_FaceColor", Color.white);
                }

                // Disable underlay (can cause background boxes)
                if (mat.HasProperty("_UnderlayColor"))
                {
                    mat.SetColor("_UnderlayColor", Color.clear);
                }
                if (mat.HasProperty("_UnderlayOffsetX"))
                {
                    mat.SetFloat("_UnderlayOffsetX", 0);
                    mat.SetFloat("_UnderlayOffsetY", 0);
                }

                EditorUtility.SetDirty(mat);
                Debug.Log($"Fixed font material: {path} (shader: {mat.shader.name})");
            }

            AssetDatabase.SaveAssets();
            Debug.Log("MergeGame: Font transparency fix applied to all TMP font assets.");
        }
    }
}
