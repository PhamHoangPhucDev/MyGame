using System.IO;
using GameplayPrototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// One-shot Stage 1 builder: placeholder pixel sprites + GameplayPrototype scene.
/// Menu: Tools/Gameplay Prototype/Build Stage 1 Scene
/// </summary>
public static class Stage1PrototypeBuilder
{
    const string ArtFolder = "Assets/Art/Placeholders";
    const string ScenePath = "Assets/Scenes/GameplayPrototype.unity";

    [MenuItem("Tools/Gameplay Prototype/Build Stage 1 Scene")]
    public static void Build()
    {
        EnsureFolders();
        CreateAllSprites();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        BuildSceneContents();
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();

        // Portrait Game View hint (best-effort; depends on Editor layout)
        TrySetGameViewAspect9x16();

        Debug.Log($"[Stage1] Saved scene at {ScenePath}. Open Game View with Free Aspect or 9:16 and press Play. WASD to move.");
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art"))
            AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder(ArtFolder))
            AssetDatabase.CreateFolder("Assets/Art", "Placeholders");
        if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
            AssetDatabase.CreateFolder("Assets", "Scenes");
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/Placeholders"))
            AssetDatabase.CreateFolder("Assets/Resources", "Placeholders");
    }

    static void CreateAllSprites()
    {
        WritePixelSprite("Grass", 16, 16, DrawGrass);
        WritePixelSprite("GrassDark", 16, 16, DrawGrassDark);
        WritePixelSprite("DirtPath", 16, 16, DrawDirt);
        WritePixelSprite("Tree", 24, 32, DrawTree);
        WritePixelSprite("Bush", 18, 14, DrawBush);
        WritePixelSprite("Tent", 40, 36, DrawTent);
        WritePixelSprite("Cauldron", 18, 16, DrawCauldron);
        WritePixelSprite("Flag", 10, 28, DrawFlag);
        WritePixelSprite("Bones", 16, 10, DrawBones);
        WritePixelSprite("Rock", 18, 14, DrawRock);
        WritePixelSprite("Player", 16, 20, DrawPlayer);
    }

    delegate void PixelPainter(Color32[] px, int w, int h);

    static void WritePixelSprite(string name, int w, int h, PixelPainter paint)
    {
        var pixels = new Color32[w * h];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(0, 0, 0, 0);

        paint(pixels, w, h);

        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;
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

        // Mirror into Resources for runtime builder fallback
        string resPath = $"Assets/Resources/Placeholders/{name}.png";
        AssetDatabase.CopyAsset(path, resPath);
        var resImporter = (TextureImporter)AssetImporter.GetAtPath(resPath);
        if (resImporter != null)
        {
            resImporter.textureType = TextureImporterType.Sprite;
            resImporter.spriteImportMode = SpriteImportMode.Single;
            resImporter.spritePixelsPerUnit = 16f;
            resImporter.filterMode = FilterMode.Point;
            resImporter.textureCompression = TextureImporterCompression.Uncompressed;
            resImporter.mipmapEnabled = false;
            resImporter.alphaIsTransparency = true;
            resImporter.SaveAndReimport();
        }
    }

    static Sprite LoadSprite(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{name}.png");
    }

    static void BuildSceneContents()
    {
        // Physics defaults for top-down
        Physics2D.gravity = Vector2.zero;

        var forestRoot = new GameObject("ForestRoot");
        PlaceGround(forestRoot.transform);
        PlaceProps(forestRoot.transform);

        var player = CreatePlayer();
        var cameraGo = CreateCamera(player.transform);

        var systems = new GameObject("Systems");
        var builder = systems.AddComponent<PrototypeForestBuilder>();
        // Forest already placed in edit mode; keep component for future rebuilds.
        var so = new SerializedObject(builder);
        so.FindProperty("buildOnAwake").boolValue = false;
        so.FindProperty("parentRoot").objectReferenceValue = forestRoot.transform;
        so.ApplyModifiedPropertiesWithoutUndo();

        Selection.activeGameObject = player;
        SceneView.lastActiveSceneView?.FrameSelected();
    }

    static void PlaceGround(Transform parent)
    {
        var grass = LoadSprite("Grass");
        var grassDark = LoadSprite("GrassDark");
        var dirt = LoadSprite("DirtPath");

        for (int x = -10; x <= 10; x++)
        {
            for (int y = -14; y <= 14; y++)
            {
                bool isDirt = Mathf.Abs(x) <= 2 && Mathf.Abs(y) <= 6;
                bool dark = !isDirt && ((x + y * 3) % 7 == 0);
                var sprite = isDirt ? dirt : (dark ? grassDark : grass);
                var tile = CreateSpriteObject($"Tile_{x}_{y}", sprite, new Vector3(x * 0.9f, y * 0.9f, 0f), 0, parent);
                // Ground does not need Y-sort updates every frame
                Object.DestroyImmediate(tile.GetComponent<SpriteSorter>());
            }
        }
    }

    static void PlaceProps(Transform parent)
    {
        Vector2[] trees =
        {
            new(-6.5f, 4f), new(-7.2f, -2f), new(-5.8f, -7f),
            new(6.2f, 5f), new(7f, -1.5f), new(5.5f, -6.5f),
            new(-3f, 9f), new(2.5f, 10f), new(0f, -11f),
            new(-8f, 8f), new(8f, 8f), new(-8f, -10f), new(8f, -9f)
        };
        foreach (var p in trees)
            CreateProp("Tree", LoadSprite("Tree"), p, 5, parent, new Vector2(0.6f, 0.35f), new Vector2(0f, -0.55f));

        Vector2[] bushes =
        {
            new(-4f, 2f), new(-2.5f, -4f), new(3.2f, 3.5f),
            new(4.5f, -3f), new(-1f, 6.5f), new(1.5f, -8f),
            new(-5.5f, -5f), new(5.8f, 1f)
        };
        foreach (var p in bushes)
            CreateProp("Bush", LoadSprite("Bush"), p, 3, parent, new Vector2(0.55f, 0.35f), Vector2.zero);

        CreateProp("Tent", LoadSprite("Tent"), new Vector2(-2.2f, 2.8f), 8, parent, new Vector2(1.1f, 0.45f), new Vector2(0f, -0.5f));
        CreateProp("Tent", LoadSprite("Tent"), new Vector2(2.4f, 1.5f), 8, parent, new Vector2(1.1f, 0.45f), new Vector2(0f, -0.5f));
        CreateProp("Cauldron", LoadSprite("Cauldron"), new Vector2(0.2f, 1.2f), 6, parent, new Vector2(0.55f, 0.4f), Vector2.zero);
        CreateProp("Flag", LoadSprite("Flag"), new Vector2(-0.8f, 3.6f), 7, parent, new Vector2(0.15f, 0.2f), new Vector2(0f, -0.4f));
        CreateProp("Bones", LoadSprite("Bones"), new Vector2(1.2f, -0.8f), 2, parent, null, null);
        CreateProp("Bones", LoadSprite("Bones"), new Vector2(-3.4f, 0.6f), 2, parent, null, null);

        for (int i = -9; i <= 9; i++)
        {
            CreateProp("Rock", LoadSprite("Rock"), new Vector2(i * 0.95f, 12.5f), 4, parent, new Vector2(0.7f, 0.4f), Vector2.zero);
            CreateProp("Rock", LoadSprite("Rock"), new Vector2(i * 0.95f, -12.5f), 4, parent, new Vector2(0.7f, 0.4f), Vector2.zero);
        }
        for (int i = -12; i <= 12; i++)
        {
            CreateProp("Rock", LoadSprite("Rock"), new Vector2(-9.5f, i * 0.95f), 4, parent, new Vector2(0.5f, 0.55f), Vector2.zero);
            CreateProp("Rock", LoadSprite("Rock"), new Vector2(9.5f, i * 0.95f), 4, parent, new Vector2(0.5f, 0.55f), Vector2.zero);
        }
    }

    static GameObject CreatePlayer()
    {
        // Spawn south of camp props so the player is not stuck inside the cauldron collider.
        var player = CreateSpriteObject("Player", LoadSprite("Player"), new Vector3(0f, -2.5f, 0f), 20, null);
        var rb = player.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        var col = player.AddComponent<CircleCollider2D>();
        col.radius = 0.28f;
        col.offset = new Vector2(0f, -0.15f);

        player.AddComponent<PlayerMovement>();

        var sorter = player.GetComponent<SpriteSorter>();
        if (sorter != null)
            sorter.baseOrder = 20;

        return player;
    }

    static GameObject CreateCamera(Transform target)
    {
        var camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        var cam = camGo.AddComponent<Camera>();
        cam.orthographic = true;
        cam.orthographicSize = 7.5f;
        cam.backgroundColor = new Color(0.35f, 0.48f, 0.28f, 1f);
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.nearClipPlane = 0.1f;
        cam.farClipPlane = 100f;
        camGo.transform.position = new Vector3(0f, 0f, -10f);
        camGo.AddComponent<AudioListener>();
        camGo.AddComponent<PortraitPixelSetup>();

        var follow = camGo.AddComponent<CameraFollow>();
        var so = new SerializedObject(follow);
        so.FindProperty("target").objectReferenceValue = target;
        so.FindProperty("smoothTime").floatValue = 0.12f;
        so.FindProperty("offset").vector3Value = new Vector3(0f, 0f, -10f);
        so.ApplyModifiedPropertiesWithoutUndo();

        return camGo;
    }

    static GameObject CreateProp(string name, Sprite sprite, Vector2 pos, int order, Transform parent,
        Vector2? colliderSize, Vector2? colliderOffset)
    {
        var go = CreateSpriteObject(name, sprite, pos, order, parent);
        if (colliderSize.HasValue)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.size = colliderSize.Value;
            box.offset = colliderOffset ?? Vector2.zero;
        }
        return go;
    }

    static GameObject CreateSpriteObject(string name, Sprite sprite, Vector3 pos, int order, Transform parent)
    {
        var go = new GameObject(name);
        if (parent != null)
            go.transform.SetParent(parent, false);
        go.transform.position = pos;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingOrder = order;

        var sorter = go.AddComponent<SpriteSorter>();
        sorter.baseOrder = order;
        return go;
    }

    static void TrySetGameViewAspect9x16()
    {
        // Non-critical: user can set Game view to 9:16 manually.
        try
        {
            var gameViewType = System.Type.GetType("UnityEditor.GameView,UnityEditor");
            if (gameViewType == null) return;
            var window = EditorWindow.GetWindow(gameViewType, false, null, false);
            if (window == null) return;

            // Unity 6 GameView uses selectedSizeIndex; exact API varies — log guidance instead of brittle reflection.
            Debug.Log("[Stage1] Please set Game view Aspect to 9:16 (or 576x1024) for portrait framing.");
        }
        catch
        {
            // ignore
        }
    }

    // ---- Pixel painters (original placeholder art) ----

    static void FillRect(Color32[] px, int w, int h, int x0, int y0, int x1, int y1, Color32 c)
    {
        for (int y = y0; y <= y1 && y < h; y++)
            for (int x = x0; x <= x1 && x < w; x++)
                if (x >= 0 && y >= 0)
                    px[y * w + x] = c;
    }

    static void Set(Color32[] px, int w, int h, int x, int y, Color32 c)
    {
        if (x < 0 || y < 0 || x >= w || y >= h) return;
        px[y * w + x] = c;
    }

    static void OutlineRect(Color32[] px, int w, int h, int x0, int y0, int x1, int y1, Color32 c)
    {
        for (int x = x0; x <= x1; x++)
        {
            Set(px, w, h, x, y0, c);
            Set(px, w, h, x, y1, c);
        }
        for (int y = y0; y <= y1; y++)
        {
            Set(px, w, h, x0, y, c);
            Set(px, w, h, x1, y, c);
        }
    }

    static void DrawGrass(Color32[] px, int w, int h)
    {
        var baseC = new Color32(120, 168, 78, 255);
        var tip = new Color32(90, 140, 60, 255);
        FillRect(px, w, h, 0, 0, w - 1, h - 1, baseC);
        for (int i = 0; i < 8; i++)
            Set(px, w, h, (i * 3 + 1) % w, (i * 5 + 2) % h, tip);
    }

    static void DrawGrassDark(Color32[] px, int w, int h)
    {
        var baseC = new Color32(88, 132, 62, 255);
        var tip = new Color32(70, 110, 50, 255);
        FillRect(px, w, h, 0, 0, w - 1, h - 1, baseC);
        for (int i = 0; i < 10; i++)
            Set(px, w, h, (i * 2 + 1) % w, (i * 3 + 1) % h, tip);
    }

    static void DrawDirt(Color32[] px, int w, int h)
    {
        var baseC = new Color32(158, 138, 98, 255);
        var speck = new Color32(130, 112, 78, 255);
        FillRect(px, w, h, 0, 0, w - 1, h - 1, baseC);
        Set(px, w, h, 3, 4, speck);
        Set(px, w, h, 9, 11, speck);
        Set(px, w, h, 12, 6, speck);
    }

    static void DrawTree(Color32[] px, int w, int h)
    {
        var trunk = new Color32(92, 64, 42, 255);
        var leaf = new Color32(62, 128, 58, 255);
        var leafDark = new Color32(40, 90, 40, 255);
        var outline = new Color32(30, 40, 28, 255);
        FillRect(px, w, h, 10, 0, 13, 12, trunk);
        FillRect(px, w, h, 4, 10, 19, 28, leaf);
        FillRect(px, w, h, 7, 24, 16, 31, leaf);
        FillRect(px, w, h, 6, 12, 9, 18, leafDark);
        OutlineRect(px, w, h, 4, 10, 19, 28, outline);
    }

    static void DrawBush(Color32[] px, int w, int h)
    {
        var leaf = new Color32(58, 118, 52, 255);
        var dark = new Color32(36, 80, 36, 255);
        FillRect(px, w, h, 2, 2, 15, 11, leaf);
        FillRect(px, w, h, 1, 4, 4, 9, dark);
        OutlineRect(px, w, h, 2, 2, 15, 11, new Color32(28, 48, 28, 255));
    }

    static void DrawTent(Color32[] px, int w, int h)
    {
        var fabric = new Color32(78, 150, 70, 255);
        var dark = new Color32(48, 100, 48, 255);
        var horn = new Color32(220, 220, 210, 255);
        var door = new Color32(30, 30, 28, 255);
        var outline = new Color32(28, 40, 28, 255);
        // Triangle-ish tent body
        for (int y = 4; y < 32; y++)
        {
            int half = (y - 4) / 2 + 4;
            FillRect(px, w, h, 20 - half, y, 20 + half, y, fabric);
        }
        FillRect(px, w, h, 16, 18, 24, 31, door);
        FillRect(px, w, h, 8, 10, 12, 16, dark);
        // Horns
        FillRect(px, w, h, 10, 30, 12, 35, horn);
        FillRect(px, w, h, 27, 30, 29, 35, horn);
        OutlineRect(px, w, h, 8, 4, 31, 31, outline);
    }

    static void DrawCauldron(Color32[] px, int w, int h)
    {
        var wood = new Color32(120, 84, 52, 255);
        var bowl = new Color32(70, 55, 45, 255);
        FillRect(px, w, h, 2, 0, 15, 4, wood);
        FillRect(px, w, h, 3, 4, 14, 13, bowl);
        OutlineRect(px, w, h, 3, 4, 14, 13, new Color32(40, 30, 24, 255));
    }

    static void DrawFlag(Color32[] px, int w, int h)
    {
        var pole = new Color32(90, 70, 50, 255);
        var cloth = new Color32(90, 40, 100, 255);
        FillRect(px, w, h, 4, 0, 5, 27, pole);
        FillRect(px, w, h, 6, 18, 9, 26, cloth);
    }

    static void DrawBones(Color32[] px, int w, int h)
    {
        var bone = new Color32(220, 214, 190, 255);
        FillRect(px, w, h, 2, 3, 13, 5, bone);
        FillRect(px, w, h, 1, 2, 3, 7, bone);
        FillRect(px, w, h, 12, 2, 14, 7, bone);
    }

    static void DrawRock(Color32[] px, int w, int h)
    {
        var rock = new Color32(98, 90, 82, 255);
        var dark = new Color32(70, 64, 58, 255);
        FillRect(px, w, h, 2, 2, 15, 11, rock);
        FillRect(px, w, h, 3, 3, 7, 6, dark);
        OutlineRect(px, w, h, 2, 2, 15, 11, new Color32(45, 40, 36, 255));
    }

    static void DrawPlayer(Color32[] px, int w, int h)
    {
        var hair = new Color32(240, 240, 245, 255);
        var cloak = new Color32(35, 45, 70, 255);
        var skin = new Color32(230, 190, 160, 255);
        var sword = new Color32(200, 210, 220, 255);
        var outline = new Color32(20, 20, 30, 255);

        // Body / cloak
        FillRect(px, w, h, 4, 2, 11, 10, cloak);
        // Head
        FillRect(px, w, h, 5, 10, 10, 15, skin);
        // White hair
        FillRect(px, w, h, 4, 12, 11, 19, hair);
        FillRect(px, w, h, 3, 14, 4, 18, hair);
        FillRect(px, w, h, 11, 14, 12, 18, hair);
        // Sword on back
        FillRect(px, w, h, 12, 6, 13, 16, sword);
        Set(px, w, h, 12, 16, new Color32(200, 170, 60, 255));
        OutlineRect(px, w, h, 4, 2, 11, 10, outline);
    }
}
