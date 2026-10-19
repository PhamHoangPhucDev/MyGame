using System.IO;
using GameplayPrototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Wires overlay labels, hitstop, player visual/sword. Does NOT change PixelPerfectCamera.
/// </summary>
public static class Stage3FeelSetup
{
    const string ScenePath = "Assets/Scenes/GameplayPrototype.unity";

    [MenuItem("Tools/Gameplay Prototype/Setup Combat Feel")]
    public static void Apply()
    {
        EnsureFontInResources();
        CreateSwordSprite();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        var systems = GameObject.Find("Systems");
        if (systems == null) systems = new GameObject("Systems");
        if (systems.GetComponent<HitStop>() == null)
            systems.AddComponent<HitStop>();
        if (systems.GetComponent<GameOverController>() == null)
            systems.AddComponent<GameOverController>();

        if (Object.FindFirstObjectByType<WorldLabelsCanvas>() == null)
            new GameObject("WorldLabelsCanvas").AddComponent<WorldLabelsCanvas>();

        var player = GameObject.Find("Player");
        if (player != null)
        {
            var visual = player.GetComponent<PlayerVisual>();
            if (visual == null) visual = player.AddComponent<PlayerVisual>();
            visual.EnsureHierarchy();

            // Assign sword sprite from Resources if present
            var sword = player.transform.Find("Sword");
            if (sword != null)
            {
                var sr = sword.GetComponent<SpriteRenderer>();
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/Placeholders/Sword.png");
                if (sr != null && sprite != null)
                    sr.sprite = sprite;
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Feel] WorldLabelsCanvas, HitStop, PlayerVisual/Sword wired. PPC untouched.");
    }

    static void EnsureFontInResources()
    {
        const string src = "Assets/Art/Fonts/PressStart2P-Regular.ttf";
        const string dstFolder = "Assets/Resources/Fonts";
        const string dst = "Assets/Resources/Fonts/PressStart2P-Regular.ttf";
        if (!File.Exists(src))
        {
            Debug.LogWarning("[Feel] OFL font missing at " + src);
            return;
        }
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder(dstFolder))
            AssetDatabase.CreateFolder("Assets/Resources", "Fonts");
        if (!File.Exists(dst))
            AssetDatabase.CopyAsset(src, dst);

        AssetDatabase.ImportAsset(dst, ImportAssetOptions.ForceUpdate);
    }

    static void CreateSwordSprite()
    {
        const string path = "Assets/Art/Placeholders/Sword.png";
        const int w = 6, h = 16;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        var px = new Color32[w * h];
        var blade = new Color32(190, 220, 255, 255);
        var edge = new Color32(255, 255, 255, 255);
        var hilt = new Color32(140, 90, 40, 255);
        for (int y = 4; y < h; y++)
        for (int x = 2; x <= 3; x++)
            px[y * w + x] = y >= h - 2 ? edge : blade;
        for (int y = 0; y < 4; y++)
        for (int x = 1; x <= 4; x++)
            px[y * w + x] = hilt;
        tex.SetPixels32(px);
        tex.Apply(false, false);
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        // Pivot near hilt
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0.5f, 0.12f);
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        // Also copy into Resources for runtime Load
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Placeholders"))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Resources"))
                AssetDatabase.CreateFolder("Assets", "Resources");
            AssetDatabase.CreateFolder("Assets/Resources", "Placeholders");
        }
        AssetDatabase.CopyAsset(path, "Assets/Resources/Placeholders/Sword.png");
    }
}
