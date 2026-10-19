using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Draws hero and enemy sprites and assigns them. Does not change combat numbers or the camera.
/// </summary>
public static class CharacterSpritePainter
{
    const string ScenePath = "Assets/Scenes/GameplayPrototype.unity";

    [MenuItem("Tools/Gameplay Prototype/Paint Character Sprites")]
    public static void Paint()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art/Characters"))
            AssetDatabase.CreateFolder("Assets/Art", "Characters");

        var idle = Write("Assets/Art/Placeholders/Player.png", 16, 20, HeroIdle, new Vector2(0.5f, 0.5f));
        var walkA = Write("Assets/Art/Characters/HeroWalkA.png", 16, 20, HeroWalkA, new Vector2(0.5f, 0.5f));
        var walkB = Write("Assets/Art/Characters/HeroWalkB.png", 16, 20, HeroWalkB, new Vector2(0.5f, 0.5f));
        var strike = Write("Assets/Art/Characters/HeroStrike.png", 16, 20, HeroStrike, new Vector2(0.5f, 0.5f));
        Write("Assets/Art/Placeholders/Sword.png", 6, 16, Sword, new Vector2(0.5f, 0.12f));
        var goblin = Write("Assets/Art/Placeholders/Goblin.png", 16, 18, GoblinIdle, new Vector2(0.5f, 0.5f));
        var goblinWalk = Write("Assets/Art/Characters/GoblinWalk.png", 16, 18, GoblinWalk, new Vector2(0.5f, 0.5f));
        var rider = Write("Assets/Art/Placeholders/Gobrider.png", 22, 22, RiderIdle, new Vector2(0.5f, 0.5f));
        var riderWalk = Write("Assets/Art/Characters/GobriderWalk.png", 22, 22, RiderWalk, new Vector2(0.5f, 0.5f));

        CopyOver("Assets/Art/Placeholders/Player.png", "Assets/Resources/Placeholders/Player.png");
        CopyOver("Assets/Art/Placeholders/Sword.png", "Assets/Resources/Placeholders/Sword.png");
        CopyOver("Assets/Art/Placeholders/Goblin.png", "Assets/Resources/Placeholders/Goblin.png");
        CopyOver("Assets/Art/Placeholders/Gobrider.png", "Assets/Resources/Placeholders/Gobrider.png");

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        idle = Load("Assets/Art/Placeholders/Player.png");
        walkA = Load("Assets/Art/Characters/HeroWalkA.png");
        walkB = Load("Assets/Art/Characters/HeroWalkB.png");
        strike = Load("Assets/Art/Characters/HeroStrike.png");
        var sword = Load("Assets/Art/Placeholders/Sword.png");
        goblin = Load("Assets/Art/Placeholders/Goblin.png");
        goblinWalk = Load("Assets/Art/Characters/GoblinWalk.png");
        rider = Load("Assets/Art/Placeholders/Gobrider.png");
        riderWalk = Load("Assets/Art/Characters/GobriderWalk.png");

        var player = GameObject.FindGameObjectWithTag("Player");
        var visual = player.GetComponent<GameplayPrototype.PlayerVisual>();
        var so = new SerializedObject(visual);
        so.FindProperty("idleSprite").objectReferenceValue = idle;
        so.FindProperty("walkASprite").objectReferenceValue = walkA;
        so.FindProperty("walkBSprite").objectReferenceValue = walkB;
        so.FindProperty("strikeSprite").objectReferenceValue = strike;
        so.ApplyModifiedPropertiesWithoutUndo();

        var body = player.transform.Find("Visual");
        if (body != null)
        {
            var sr = body.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = idle;
        }
        var swordTr = player.transform.Find("Sword");
        if (swordTr != null)
        {
            var sr = swordTr.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = sword;
        }

        var spawner = Object.FindFirstObjectByType<GameplayPrototype.EnemySpawner>();
        var sp = new SerializedObject(spawner);
        sp.FindProperty("goblinSprite").objectReferenceValue = goblin;
        sp.FindProperty("gobriderSprite").objectReferenceValue = rider;
        sp.FindProperty("goblinWalkSprite").objectReferenceValue = goblinWalk;
        sp.FindProperty("gobriderWalkSprite").objectReferenceValue = riderWalk;
        sp.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Characters] painted. heroH={idle.bounds.size.y:F2} goblin={goblin.rect.width}x{goblin.rect.height} rider={rider.rect.width}x{rider.rect.height}");
    }

    static Sprite Load(string path) => AssetDatabase.LoadAssetAtPath<Sprite>(path);

    static void CopyOver(string src, string dst)
    {
        if (AssetDatabase.LoadAssetAtPath<Object>(dst) != null)
            AssetDatabase.DeleteAsset(dst);
        AssetDatabase.CopyAsset(src, dst);
    }

    static Sprite Write(string path, int w, int h, string[] rowsTop, Vector2 pivot)
    {
        if (rowsTop.Length != h)
            throw new System.Exception(path + " row count " + rowsTop.Length + " != " + h);
        var px = new Color32[w * h];
        for (int row = 0; row < h; row++)
        {
            if (rowsTop[row].Length != w)
                throw new System.Exception(path + " row " + row + " len " + rowsTop[row].Length);
            int y = h - 1 - row;
            for (int x = 0; x < w; x++)
                px[y * w + x] = Ink(rowsTop[row][x]);
        }
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
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
        importer.alphaIsTransparency = true;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spritePivot = pivot;
        settings.filterMode = FilterMode.Point;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Color32 Ink(char c)
    {
        switch (c)
        {
            case '.': return new Color32(0, 0, 0, 0);
            case '#': return new Color32(22, 16, 28, 255);
            case 'W': return new Color32(248, 248, 252, 255);
            case 'w': return new Color32(176, 188, 210, 255);
            case 'S': return new Color32(236, 196, 162, 255);
            case 'T': return new Color32(28, 72, 108, 255);
            case 't': return new Color32(92, 158, 186, 255);
            case 'G': return new Color32(186, 142, 58, 255);
            case 'B': return new Color32(36, 42, 64, 255);
            case 'b': return new Color32(52, 34, 28, 255);
            case 'P': return new Color32(92, 52, 112, 255);
            case 'p': return new Color32(52, 28, 68, 255);
            case 'g': return new Color32(112, 176, 64, 255);
            case 'R': return new Color32(210, 48, 52, 255);
            case 'C': return new Color32(48, 40, 62, 255);
            case 'c': return new Color32(28, 24, 36, 255);
            case 'D': return new Color32(72, 48, 40, 255);
            case 'd': return new Color32(40, 28, 24, 255);
            case 'E': return new Color32(232, 196, 96, 255);
            case 'L': return new Color32(220, 232, 255, 255);
            case 'l': return new Color32(120, 150, 190, 255);
            case 'H': return new Color32(92, 58, 32, 255);
            default: return new Color32(255, 0, 255, 255);
        }
    }

    static readonly string[] HeroIdle =
    {
        "................",
        "......####......",
        ".....######.....",
        "....##WWWW##....",
        "....#WWWWWW#....",
        "....#WWwwWW#....",
        ".....#SSSS#.....",
        ".....#S##S#.....",
        "....##TTTT##....",
        "...#tTTTTTt#....",
        "...#TTTTTTTT#...",
        "....#TTGGTT#....",
        ".....#BBBB#.....",
        ".....#B##B#.....",
        "......#BB#......",
        ".....#b..b#.....",
        ".....#b..b#.....",
        "......#..#......",
        "................",
        "................",
    };

    static readonly string[] HeroWalkA =
    {
        "................",
        "......####......",
        ".....######.....",
        "....##WWWW##....",
        "....#WWWWWW#....",
        "....#WWwwWW#....",
        ".....#SSSS#.....",
        ".....#S##S#.....",
        "....##TTTT##....",
        "...#tTTTTTt#....",
        "...#TTTTTTTT#...",
        "....#TTGGTT#....",
        ".....#BBBB#.....",
        "....#B#..#B#....",
        "....#B....B#....",
        "...#b......b#...",
        "...#b......b#...",
        "....#......#....",
        "................",
        "................",
    };

    static readonly string[] HeroWalkB =
    {
        "................",
        "......####......",
        ".....######.....",
        "....##WWWW##....",
        "....#WWWWWW#....",
        "....#WWwwWW#....",
        ".....#SSSS#.....",
        ".....#S##S#.....",
        "....##TTTT##....",
        "...#tTTTTTt#....",
        "...#TTTTTTTT#...",
        "....#TTGGTT#....",
        ".....#BBBB#.....",
        ".....#BBBB#.....",
        "......#BB#......",
        ".....#bb##......",
        ".....#bb#.......",
        "......#.........",
        "................",
        "................",
    };

    static readonly string[] HeroStrike =
    {
        "................",
        "......####......",
        ".....######.....",
        "....##WWWW##....",
        "....#WWWWWW#....",
        "....#WWwwWW#....",
        ".....#SSSS#.....",
        ".....#S##S#.....",
        "....##TTTT#.....",
        "...#tTTTTT##....",
        "...#TTTTTT#t#...",
        "....#TTGG#......",
        ".....#BBBB#.....",
        ".....#B##B#.....",
        "......#BB#......",
        ".....#b..b#.....",
        ".....#b..b#.....",
        "......#..#......",
        "................",
        "................",
    };

    static readonly string[] Sword =
    {
        "..LL..",
        "..LL..",
        ".#ll#.",
        ".#ll#.",
        ".#ll#.",
        ".#ll#.",
        ".#ll#.",
        ".#ll#.",
        ".#ll#.",
        ".#ll#.",
        ".#ll#.",
        ".#ll#.",
        "#GGGG#",
        "..HH..",
        "..HH..",
        "..HH..",
    };

    static readonly string[] GoblinIdle =
    {
        "................",
        "....########....",
        "...##########...",
        "...##PPPPPP##...",
        "...#PPPPPPPP#...",
        "...#PPggggPP#...",
        "...#PgRggRgP#...",
        "...#PPggggPP#...",
        "....#PPPPPP#....",
        ".....##PP##.....",
        "....##CCCC##....",
        "...#CCCCCCCC#...",
        "...#CC####CC#...",
        "....#C#CC#C#....",
        ".....#CCCC#.....",
        ".....#c..c#.....",
        "......#..#......",
        "................",
    };

    static readonly string[] GoblinWalk =
    {
        "................",
        "....########....",
        "...##########...",
        "...##PPPPPP##...",
        "...#PPPPPPPP#...",
        "...#PPggggPP#...",
        "...#PgRggRgP#...",
        "...#PPggggPP#...",
        "....#PPPPPP#....",
        ".....##PP##.....",
        "....##CCCC##....",
        "...#CCCCCCCC#...",
        "...#CC####CC#...",
        "....#C#CC#C#....",
        "....#C#CC#C.....",
        "...#c#....#c#...",
        "....#......#....",
        "................",
    };

    static readonly string[] RiderIdle =
    {
        "......................",
        "........######........",
        ".......##PPPP##.......",
        "......#PPPPPPPP#......",
        "......#PPggggPP#......",
        "......#PgRggRgP#......",
        "......#PPggggPP#......",
        ".......##PPPP##.......",
        "......###CCCC###......",
        ".....##CCCCCCCC##.....",
        "...##DDDDDDDDDDDD##...",
        "..#DDDDDDDDDDDDDDDD#..",
        "..#DD##DDDDDDDD##DD#..",
        "..#D#EDDDDDDDDDE#D#...",
        "...#DDD######DDDD#....",
        "...#DD#DDDDDD#DDD#....",
        "....#D#D#DD#D#DD#.....",
        ".....#DD#DD#DDD#......",
        ".....#d#....#d#.......",
        "......#......#........",
        "......................",
        "......................",
    };

    static readonly string[] RiderWalk =
    {
        "......................",
        "........######........",
        ".......##PPPP##.......",
        "......#PPPPPPPP#......",
        "......#PPggggPP#......",
        "......#PgRggRgP#......",
        "......#PPggggPP#......",
        ".......##PPPP##.......",
        "......###CCCC###......",
        ".....##CCCCCCCC##.....",
        "...##DDDDDDDDDDDD##...",
        "..#DDDDDDDDDDDDDDDD#..",
        "..#DD##DDDDDDDD##DD#..",
        "..#D#EDDDDDDDDDE#D#...",
        "...#DDD######DDDD#....",
        "...#DD#DDDDDD#DDD#....",
        "....#DD#D##D#DDD#.....",
        "...#d#..#DD#..#d#.....",
        "...#....#..#....#.....",
        "......................",
        "......................",
        "......................",
    };
}
