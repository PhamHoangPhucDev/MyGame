using System.Collections.Generic;
using System.IO;
using GameplayPrototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Wires Stage 2/3 combat into GameplayPrototype without touching SampleScene.
/// Menu: Tools/Gameplay Prototype/Setup Combat Systems
/// </summary>
public static class Stage2CombatSetup
{
    const string ScenePath = "Assets/Scenes/GameplayPrototype.unity";
    const string ArtFolder = "Assets/Art/Placeholders";

    [MenuItem("Tools/Gameplay Prototype/Setup Combat Systems")]
    public static void Setup()
    {
        if (!File.Exists(ScenePath))
        {
            Debug.LogError("[Stage2] GameplayPrototype scene missing. Run Stage 1 builder first.");
            return;
        }

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        CreateEnemySprites();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var player = GameObject.Find("Player");
        if (player == null)
        {
            Debug.LogError("[Stage2] Player not found in scene.");
            return;
        }

        player.tag = "Player";

        // Player health + combat
        var playerHealth = player.GetComponent<Health>();
        if (playerHealth == null) playerHealth = player.AddComponent<Health>();
        var soHealth = new SerializedObject(playerHealth);
        soHealth.FindProperty("maxHp").intValue = 400;
        soHealth.FindProperty("destroyOnDeath").boolValue = false;
        soHealth.FindProperty("invulnAfterHit").floatValue = 0.85f;
        soHealth.FindProperty("blinkWhileInvulnerable").boolValue = true;
        soHealth.ApplyModifiedPropertiesWithoutUndo();
        playerHealth.Configure(400, true);
        playerHealth.SetSurvival(0.85f, true);

        if (player.GetComponent<PlayerCombat>() == null)
            player.AddComponent<PlayerCombat>();

        // Systems object
        var systems = GameObject.Find("Systems");
        if (systems == null) systems = new GameObject("Systems");

        var combo = systems.GetComponent<HitComboCounter>();
        if (combo == null) combo = systems.AddComponent<HitComboCounter>();

        var hotbar = systems.GetComponent<SkillHotbarUI>();
        if (hotbar == null) hotbar = systems.AddComponent<SkillHotbarUI>();
        var soHotbar = new SerializedObject(hotbar);
        soHotbar.FindProperty("combat").objectReferenceValue = player.GetComponent<PlayerCombat>();
        soHotbar.FindProperty("combo").objectReferenceValue = combo;
        soHotbar.ApplyModifiedPropertiesWithoutUndo();
        hotbar.Bind(player.GetComponent<PlayerCombat>(), playerHealth, combo);

        var spawner = systems.GetComponent<EnemySpawner>();
        if (spawner == null) spawner = systems.AddComponent<EnemySpawner>();

        var gameOver = systems.GetComponent<GameOverController>();
        if (gameOver == null) gameOver = systems.AddComponent<GameOverController>();

        var soSpawner = new SerializedObject(spawner);
        soSpawner.FindProperty("maxAlive").intValue = 6;
        soSpawner.FindProperty("respawnCheckInterval").floatValue = 6f;
        soSpawner.FindProperty("minSpawnDistanceFromPlayer").floatValue = 5.5f;
        soSpawner.ApplyModifiedPropertiesWithoutUndo();
        var goblin = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/Goblin.png");
        var gobrider = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/Gobrider.png");
        var camps = new List<Vector2> { new(-2.2f, 2.8f), new(2.4f, 1.5f) };
        spawner.Configure(player.transform, goblin, gobrider, camps);

        // Ensure camera still follows player
        var cam = Camera.main;
        if (cam != null)
        {
            var follow = cam.GetComponent<CameraFollow>();
            if (follow != null)
            {
                var so = new SerializedObject(follow);
                so.FindProperty("target").objectReferenceValue = player.transform;
                so.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Stage2] Combat systems wired. Play Mode: WASD move, R/Space slash, 2 dash, 3 multi, 4 bite, V bone, C mace.");
    }

    static void CreateEnemySprites()
    {
        if (!AssetDatabase.IsValidFolder(ArtFolder))
        {
            if (!AssetDatabase.IsValidFolder("Assets/Art"))
                AssetDatabase.CreateFolder("Assets", "Art");
            AssetDatabase.CreateFolder("Assets/Art", "Placeholders");
        }

        WriteSprite("Goblin", 16, 18, DrawGoblin);
        WriteSprite("Gobrider", 22, 22, DrawGobrider);
    }

    delegate void Painter(Color32[] px, int w, int h);

    static void WriteSprite(string name, int w, int h, Painter paint)
    {
        var pixels = new Color32[w * h];
        paint(pixels, w, h);
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        tex.SetPixels32(pixels);
        tex.Apply(false, false);
        byte[] png = tex.EncodeToPNG();
        Object.DestroyImmediate(tex);

        string path = $"{ArtFolder}/{name}.png";
        File.WriteAllBytes(path, png);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16f;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.SaveAndReimport();
    }

    static void Set(Color32[] px, int w, int h, int x, int y, Color32 c)
    {
        if (x < 0 || y < 0 || x >= w || y >= h) return;
        px[y * w + x] = c;
    }

    static void Fill(Color32[] px, int w, int h, int x0, int y0, int x1, int y1, Color32 c)
    {
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
                Set(px, w, h, x, y, c);
    }

    static void DrawGoblin(Color32[] px, int w, int h)
    {
        var skin = new Color32(70, 170, 55, 255);
        var dark = new Color32(35, 95, 35, 255);
        var cloth = new Color32(120, 55, 40, 255);
        var eye = new Color32(220, 40, 40, 255);
        Fill(px, w, h, 5, 1, 10, 7, cloth);
        Fill(px, w, h, 4, 7, 11, 13, skin);
        Fill(px, w, h, 3, 12, 4, 15, skin);
        Fill(px, w, h, 11, 12, 12, 15, skin);
        Set(px, w, h, 6, 11, eye);
        Set(px, w, h, 9, 11, eye);
        Fill(px, w, h, 6, 0, 7, 2, dark);
        Fill(px, w, h, 8, 0, 9, 2, dark);
    }

    static void DrawGobrider(Color32[] px, int w, int h)
    {
        var skin = new Color32(55, 140, 50, 255);
        var mount = new Color32(55, 40, 70, 255);
        var cloth = new Color32(90, 40, 35, 255);
        var eye = new Color32(240, 50, 50, 255);
        // Mount body
        Fill(px, w, h, 3, 1, 18, 9, mount);
        Fill(px, w, h, 2, 3, 4, 6, mount);
        Fill(px, w, h, 17, 3, 19, 6, mount);
        // Rider
        Fill(px, w, h, 8, 8, 14, 14, cloth);
        Fill(px, w, h, 7, 13, 15, 19, skin);
        Set(px, w, h, 9, 17, eye);
        Set(px, w, h, 12, 17, eye);
        Fill(px, w, h, 6, 17, 7, 20, skin);
        Fill(px, w, h, 14, 17, 15, 20, skin);
    }
}
