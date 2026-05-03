using UnityEditor;
using UnityEngine;
using TMPro;
using System.IO;

public static class ArabicFontFixer
{
    [MenuItem("Skrew/Fix Arabic Font")]
    public static void Fix()
    {
        string ttfPath = "Assets/Art/Fonts/Arial.ttf";
        string fontPath = "Assets/Art/Fonts/Arial_TMP.asset";
        
        Font ttf = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (ttf == null) { Debug.LogError("TTF not found at " + ttfPath); return; }

        // Create a new dynamic font asset
        TMP_FontAsset font = TMP_FontAsset.CreateFontAsset(ttf, 90, 9, UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
        font.name = "Arial_TMP";

        // Extract the atlas texture and save it as a sub-asset properly
        Texture2D atlas = font.atlasTextures[0];
        atlas.name = "Arial_TMP Atlas";

        // Remove old asset if exists (by moving to temp)
        if (File.Exists(Path.Combine(Application.dataPath, "Art/Fonts/Arial_TMP.asset")))
        {
            AssetDatabase.MoveAsset(fontPath, "Assets/Art/Fonts/Arial_TMP_Old.asset");
        }

        AssetDatabase.CreateAsset(font, fontPath);
        AssetDatabase.AddObjectToAsset(atlas, font);
        
        // Force refresh all TMP components
        foreach (var tmp in Object.FindObjectsOfType<TMP_Text>(true))
        {
            if (tmp.font != null && (tmp.font.name == "Arial_TMP" || tmp.font.name == "Arial_Old_TMP"))
            {
                tmp.font = font;
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Arabic Font Fixed successfully!");
    }
}
