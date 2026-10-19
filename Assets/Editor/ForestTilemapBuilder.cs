using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Paints a small designed forest camp. Does not touch combat, movement, AI, or the pixel-perfect camera.
/// </summary>
public static class ForestTilemapBuilder
{
    const string ScenePath = "Assets/Scenes/GameplayPrototype.unity";
    const string TileFolder = "Assets/Art/Tiles";
    const string PropFolder = "Assets/Art/Environment";

    static readonly Color32 Clear = new Color32(0, 0, 0, 0);
    static readonly Color32 G0 = new Color32(168, 198, 112, 255);
    static readonly Color32 G1 = new Color32(142, 176, 96, 255);
    static readonly Color32 G2 = new Color32(108, 146, 74, 255);
    static readonly Color32 GDark = new Color32(86, 122, 64, 255);
    static readonly Color32 Moss = new Color32(70, 108, 78, 255);
    static readonly Color32 Flower = new Color32(186, 150, 196, 255);
    static readonly Color32 D0 = new Color32(188, 156, 104, 255);
    static readonly Color32 D1 = new Color32(164, 132, 84, 255);
    static readonly Color32 D2 = new Color32(136, 106, 70, 255);
    static readonly Color32 Pebble = new Color32(112, 102, 88, 255);
    static readonly Color32 C0 = new Color32(132, 92, 60, 255);
    static readonly Color32 C1 = new Color32(96, 66, 44, 255);
    static readonly Color32 C2 = new Color32(72, 48, 34, 255);
    static readonly Color32 Outline = new Color32(42, 32, 40, 255);
    static readonly Color32 LeafHi = new Color32(198, 220, 138, 255);
    static readonly Color32 Leaf = new Color32(78, 128, 70, 255);
    static readonly Color32 LeafDk = new Color32(46, 72, 58, 255);
    static readonly Color32 Trunk = new Color32(128, 82, 48, 255);
    static readonly Color32 TrunkDk = new Color32(84, 52, 32, 255);
    static readonly Color32 Bone = new Color32(228, 220, 196, 255);
    static readonly Color32 BoneDk = new Color32(176, 164, 140, 255);

    [MenuItem("Tools/Gameplay Prototype/Build Forest Tilemap")]
    public static void Build()
    {
        EnsureSortingLayer("Ground");
        EnsureFolders();

        MakeTile("GrassA", (px, w, h) => DrawGrass(px, w, h, 0), Tile.ColliderType.None);
        MakeTile("GrassB", (px, w, h) => DrawGrass(px, w, h, 1), Tile.ColliderType.None);
        MakeTile("GrassC", (px, w, h) => DrawGrass(px, w, h, 2), Tile.ColliderType.None);
        MakeTile("GrassDark", (px, w, h) => DrawGrass(px, w, h, 3), Tile.ColliderType.None);
        MakeTile("CliffA", (px, w, h) => DrawCliff(px, w, h, 0), Tile.ColliderType.None);
        MakeTile("CliffB", (px, w, h) => DrawCliff(px, w, h, 1), Tile.ColliderType.None);
        MakeTile("Flower", DrawFlower, Tile.ColliderType.None);
        MakeTile("Pebble", DrawPebble, Tile.ColliderType.None);
        for (int mask = 0; mask < 16; mask++)
        {
            int captured = mask;
            MakeTile("DirtM" + captured, (px, w, h) => DrawDirt(px, w, h, captured), Tile.ColliderType.None);
        }

        var treeSprites = new Sprite[5];
        for (int i = 0; i < 5; i++)
        {
            int v = i;
            treeSprites[i] = MakeSprite(PropFolder, "Tree" + (char)('A' + v), 32, 48, (px, w, h) => DrawTree(px, w, h, v), SpriteAlignment.BottomCenter);
        }
        var bushA = MakeSprite(PropFolder, "BushA", 24, 18, (px, w, h) => DrawBush(px, w, h, 0), SpriteAlignment.BottomCenter);
        var bushB = MakeSprite(PropFolder, "BushB", 20, 16, (px, w, h) => DrawBush(px, w, h, 1), SpriteAlignment.BottomCenter);
        var tentA = MakeSprite(PropFolder, "TentA", 40, 48, (px, w, h) => DrawTent(px, w, h, 0), SpriteAlignment.BottomCenter);
        var tentB = MakeSprite(PropFolder, "TentB", 40, 48, (px, w, h) => DrawTent(px, w, h, 1), SpriteAlignment.BottomCenter);
        var crate = MakeSprite(PropFolder, "Crate", 16, 16, DrawCrate, SpriteAlignment.BottomCenter);
        var stump = MakeSprite(PropFolder, "Stump", 16, 14, DrawStump, SpriteAlignment.BottomCenter);
        var rock = MakeSprite(PropFolder, "Rock", 16, 12, DrawRock, SpriteAlignment.BottomCenter);
        var barrel = MakeSprite(PropFolder, "Barrel", 16, 16, DrawBarrel, SpriteAlignment.BottomCenter);
        var cauldron = MakeSprite(PropFolder, "Cauldron", 20, 18, DrawCauldron, SpriteAlignment.BottomCenter);
        var flag = MakeSprite(PropFolder, "Flag", 14, 28, DrawFlag, SpriteAlignment.BottomCenter);
        var bones = MakeSprite(PropFolder, "Bones", 18, 12, DrawBones, SpriteAlignment.BottomCenter);
        var shadow = MakeSprite(PropFolder, "Shadow", 18, 8, DrawShadow, SpriteAlignment.Center);
        AssetDatabase.SaveAssets();

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var grass = new[] { LoadTile("GrassATile"), LoadTile("GrassBTile"), LoadTile("GrassCTile") };
        var grassDark = LoadTile("GrassDarkTile");
        var cliffs = new[] { LoadTile("CliffATile"), LoadTile("CliffBTile") };
        var flower = LoadTile("FlowerTile");
        var pebble = LoadTile("PebbleTile");
        var dirt = new Tile[16];
        for (int i = 0; i < 16; i++)
            dirt[i] = LoadTile("DirtM" + i + "Tile");

        for (int i = 0; i < 5; i++)
            treeSprites[i] = AssetDatabase.LoadAssetAtPath<Sprite>($"{PropFolder}/Tree{(char)('A' + i)}.png");
        bushA = AssetDatabase.LoadAssetAtPath<Sprite>($"{PropFolder}/BushA.png");
        bushB = AssetDatabase.LoadAssetAtPath<Sprite>($"{PropFolder}/BushB.png");
        tentA = AssetDatabase.LoadAssetAtPath<Sprite>($"{PropFolder}/TentA.png");
        tentB = AssetDatabase.LoadAssetAtPath<Sprite>($"{PropFolder}/TentB.png");
        crate = AssetDatabase.LoadAssetAtPath<Sprite>($"{PropFolder}/Crate.png");
        stump = AssetDatabase.LoadAssetAtPath<Sprite>($"{PropFolder}/Stump.png");
        rock = AssetDatabase.LoadAssetAtPath<Sprite>($"{PropFolder}/Rock.png");
        barrel = AssetDatabase.LoadAssetAtPath<Sprite>($"{PropFolder}/Barrel.png");
        cauldron = AssetDatabase.LoadAssetAtPath<Sprite>($"{PropFolder}/Cauldron.png");
        flag = AssetDatabase.LoadAssetAtPath<Sprite>($"{PropFolder}/Flag.png");
        bones = AssetDatabase.LoadAssetAtPath<Sprite>($"{PropFolder}/Bones.png");
        shadow = AssetDatabase.LoadAssetAtPath<Sprite>($"{PropFolder}/Shadow.png");

        DestroyRoots("ForestRoot");
        DestroyRoots("ForestGrid");
        DestroyRoots("ForestProps");

        var builder = Object.FindFirstObjectByType<GameplayPrototype.PrototypeForestBuilder>();
        if (builder != null)
            builder.enabled = false;

        var gridGo = new GameObject("ForestGrid");
        var grid = gridGo.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);
        var ground = CreateTilemap(gridGo.transform, "Ground", "Ground", 0);
        var cliffMap = CreateTilemap(gridGo.transform, "Cliffs", "Ground", 1);
        var details = CreateTilemap(gridGo.transform, "Details", "Ground", 2);

        int groundCount = 0;
        int cliffCount = 0;
        for (int x = -12; x <= 11; x++)
        for (int y = -11; y <= 14; y++)
        {
            var cell = new Vector3Int(x, y, 0);
            if (IsBorder(x, y))
            {
                cliffMap.SetTile(cell, cliffs[(x + y) & 1]);
                cliffCount++;
                continue;
            }

            if (IsDirt(x, y))
            {
                int mask = 0;
                if (IsGrass(x, y + 1)) mask |= 1;
                if (IsGrass(x + 1, y)) mask |= 2;
                if (IsGrass(x, y - 1)) mask |= 4;
                if (IsGrass(x - 1, y)) mask |= 8;
                ground.SetTile(cell, dirt[mask]);
            }
            else if (IsDarkPatch(x, y))
                ground.SetTile(cell, grassDark);
            else
                ground.SetTile(cell, grass[Hash(x, y) % 3]);
            groundCount++;

            if (IsGrass(x, y) && Hash(x, y) % 23 == 0)
                details.SetTile(cell, Hash(x, y) % 2 == 0 ? flower : pebble);
        }

        var props = new GameObject("ForestProps");
        var trees = new (Vector2 p, int v)[]
        {
            (new Vector2(-5.4f, 7.6f), 0), (new Vector2(-7.1f, 8.4f), 1), (new Vector2(-4.2f, 8.8f), 2),
            (new Vector2(5.2f, 7.4f), 1), (new Vector2(7.0f, 8.5f), 0), (new Vector2(4.3f, 8.9f), 4),
            (new Vector2(-6.4f, -1.2f), 2), (new Vector2(-5.0f, -3.4f), 3), (new Vector2(-7.4f, 2.2f), 0),
            (new Vector2(6.2f, -1.6f), 4), (new Vector2(5.0f, -3.6f), 1), (new Vector2(7.3f, 2.6f), 2),
            (new Vector2(-5.6f, -7.2f), 1), (new Vector2(5.8f, -7.4f), 3)
        };
        foreach (var t in trees)
            PlaceProp(props.transform, "Tree", treeSprites[t.v], t.p, new Vector2(0.42f, 0.45f), new Vector2(0f, 0.22f), shadow);

        PlaceProp(props.transform, "Bush", bushA, new Vector2(-2.7f, -6.2f), new Vector2(0.55f, 0.28f), new Vector2(0f, 0.12f), shadow);
        PlaceProp(props.transform, "Bush", bushB, new Vector2(2.6f, -4.8f), new Vector2(0.5f, 0.26f), new Vector2(0f, 0.1f), shadow);
        PlaceProp(props.transform, "Bush", bushA, new Vector2(-2.5f, -1.6f), new Vector2(0.55f, 0.28f), new Vector2(0f, 0.12f), shadow);
        PlaceProp(props.transform, "Bush", bushB, new Vector2(3.2f, 0.2f), new Vector2(0.5f, 0.26f), new Vector2(0f, 0.1f), shadow);
        PlaceProp(props.transform, "Bush", bushA, new Vector2(-4.6f, 5.8f), new Vector2(0.55f, 0.28f), new Vector2(0f, 0.12f), shadow);
        PlaceProp(props.transform, "Bush", bushB, new Vector2(4.5f, 5.6f), new Vector2(0.5f, 0.26f), new Vector2(0f, 0.1f), shadow);

        // Tents sit behind the existing spawn points so the clearing stays open.
        PlaceProp(props.transform, "Tent", tentA, new Vector2(-3.6f, 4.0f), new Vector2(1.35f, 0.5f), new Vector2(0f, 1.15f), shadow);
        PlaceProp(props.transform, "Tent", tentB, new Vector2(3.4f, 3.2f), new Vector2(1.35f, 0.5f), new Vector2(0f, 1.15f), shadow);
        PlaceProp(props.transform, "Cauldron", cauldron, new Vector2(0.1f, 4.9f), new Vector2(0.7f, 0.35f), new Vector2(0f, 0.15f), shadow);
        PlaceProp(props.transform, "Flag", flag, new Vector2(-1.15f, 5.35f), new Vector2(0.18f, 0.22f), new Vector2(0f, 0.1f), shadow);
        PlaceProp(props.transform, "Bones", bones, new Vector2(1.7f, 1.15f), null, null, null);
        PlaceProp(props.transform, "Bones", bones, new Vector2(-1.9f, 1.05f), null, null, null);
        PlaceProp(props.transform, "Barrel", barrel, new Vector2(-4.35f, 2.7f), new Vector2(0.55f, 0.4f), new Vector2(0f, 0.2f), shadow);
        PlaceProp(props.transform, "Barrel", barrel, new Vector2(4.35f, 2.35f), new Vector2(0.55f, 0.4f), new Vector2(0f, 0.2f), shadow);
        PlaceProp(props.transform, "Crate", crate, new Vector2(4.15f, 4.35f), new Vector2(0.7f, 0.4f), new Vector2(0f, 0.2f), shadow);
        PlaceProp(props.transform, "Stump", stump, new Vector2(-4.5f, 1.45f), new Vector2(0.55f, 0.3f), new Vector2(0f, 0.12f), shadow);
        PlaceProp(props.transform, "Rock", rock, new Vector2(2.7f, 5.7f), new Vector2(0.6f, 0.28f), new Vector2(0f, 0.1f), shadow);
        PlaceProp(props.transform, "Rock", rock, new Vector2(-2.4f, -0.2f), new Vector2(0.55f, 0.26f), new Vector2(0f, 0.1f), shadow);

        BuildBorderWalls(gridGo.transform);

        var cam = Camera.main;
        if (cam != null)
        {
            cam.transparencySortMode = TransparencySortMode.CustomAxis;
            cam.transparencySortAxis = new Vector3(0f, 1f, 0f);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log($"[Map] env art saved. ground={groundCount} cliffs={cliffCount} trees={trees.Length} grass0={grass[0] != null} dirt0={dirt[0] != null} tree0={treeSprites[0] != null}");
    }

    static void DestroyRoots(string name)
    {
        var roots = SceneManager.GetActiveScene().GetRootGameObjects();
        foreach (var go in roots)
        {
            if (go.name == name)
                Object.DestroyImmediate(go);
        }
    }

    static bool IsBorder(int x, int y) => x <= -11 || x >= 10 || y <= -10 || y >= 13;

    static bool InArena(int x, int y) => x >= -12 && x <= 11 && y >= -11 && y <= 14;

    static bool IsDirt(int x, int y)
    {
        if (!InArena(x, y) || IsBorder(x, y)) return false;
        if (y == -9 && x >= -1 && x <= 0) return true;
        if (y == -8 && x >= -1 && x <= 1) return true;
        if (y == -7 && x >= -2 && x <= 1) return true;
        if (y == -6 && x >= -1 && x <= 1) return true;
        if (y == -5 && x >= -1 && x <= 2) return true;
        if (y == -4 && x >= 0 && x <= 2) return true;
        if (y == -3 && x >= 0 && x <= 2) return true;
        if (y == -2 && x >= -1 && x <= 2) return true;
        if (y == -1 && x >= -1 && x <= 3) return true;
        if (y == 0 && x >= -2 && x <= 3) return true;
        if (y == 1 && x >= -3 && x <= 3) return true;
        if (y == 2 && x >= -4 && x <= 4) return true;
        if (y == 3 && x >= -4 && x <= 4) return true;
        if (y == 4 && x >= -3 && x <= 3) return true;
        if (y == 5 && x >= -2 && x <= 2) return true;
        return false;
    }

    static bool IsGrass(int x, int y) => InArena(x, y) && !IsBorder(x, y) && !IsDirt(x, y);

    static bool IsDarkPatch(int x, int y)
    {
        if (!IsGrass(x, y)) return false;
        if (x >= -8 && x <= -7 && y >= -6 && y <= -5) return true;
        if (x >= 6 && x <= 7 && y >= 5 && y <= 6) return true;
        if (x >= -9 && x <= -8 && y >= 4 && y <= 5) return true;
        if (x >= 6 && x <= 7 && y >= -7 && y <= -6) return true;
        if (x >= -4 && x <= -3 && y >= 9 && y <= 10) return true;
        return false;
    }

    static void BuildBorderWalls(Transform parent)
    {
        var root = new GameObject("MapBounds");
        root.transform.SetParent(parent, false);
        AddWall(root.transform, "West", new Vector2(-11.5f, 1.5f), new Vector2(1.2f, 26f));
        AddWall(root.transform, "East", new Vector2(10.5f, 1.5f), new Vector2(1.2f, 26f));
        AddWall(root.transform, "South", new Vector2(-0.5f, -10.5f), new Vector2(24f, 1.2f));
        AddWall(root.transform, "North", new Vector2(-0.5f, 13.5f), new Vector2(24f, 1.2f));
    }

    static void AddWall(Transform parent, string name, Vector2 center, Vector2 size)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = center;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = size;
    }

    static Tilemap CreateTilemap(Transform parent, string name, string sortingLayer, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var map = go.AddComponent<Tilemap>();
        var renderer = go.AddComponent<TilemapRenderer>();
        renderer.sortingLayerName = sortingLayer;
        renderer.sortingOrder = order;
        renderer.mode = TilemapRenderer.Mode.Chunk;
        return map;
    }

    static void PlaceProp(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2? colliderSize, Vector2? colliderOffset, Sprite shadow)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Default";
        var sorter = go.AddComponent<GameplayPrototype.SpriteSorter>();
        sorter.baseOrder = 0;
        if (colliderSize.HasValue)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.size = colliderSize.Value;
            box.offset = colliderOffset ?? Vector2.zero;
        }
        if (shadow != null)
        {
            var shade = new GameObject("Shadow");
            shade.transform.SetParent(go.transform, false);
            shade.transform.localPosition = new Vector3(0f, 0.02f, 0f);
            var sh = shade.AddComponent<SpriteRenderer>();
            sh.sprite = shadow;
            sh.sortingLayerName = "Ground";
            sh.sortingOrder = 3;
        }
    }

    static int Hash(int x, int y)
    {
        int n = x * 374761393 + y * 668265263;
        n = (n ^ (n >> 13)) * 1274126177;
        return n & 0x7fffffff;
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Art"))
            AssetDatabase.CreateFolder("Assets", "Art");
        if (!AssetDatabase.IsValidFolder(TileFolder))
            AssetDatabase.CreateFolder("Assets/Art", "Tiles");
        if (!AssetDatabase.IsValidFolder(PropFolder))
            AssetDatabase.CreateFolder("Assets/Art", "Environment");
    }

    static Tile LoadTile(string assetName)
    {
        return AssetDatabase.LoadAssetAtPath<Tile>($"{TileFolder}/{assetName}.asset");
    }

    static Tile MakeTile(string name, System.Action<Color32[], int, int> paint, Tile.ColliderType colliderType)
    {
        var sprite = MakeSprite(TileFolder, name, 16, 16, paint, SpriteAlignment.Center);
        string path = $"{TileFolder}/{name}Tile.asset";
        var existing = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (existing != null)
            AssetDatabase.DeleteAsset(path);
        var tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.colliderType = colliderType;
        AssetDatabase.CreateAsset(tile, path);
        AssetDatabase.SaveAssets();
        return AssetDatabase.LoadAssetAtPath<Tile>(path);
    }

    static Sprite MakeSprite(string folder, string name, int w, int h, System.Action<Color32[], int, int> paint, SpriteAlignment align)
    {
        var px = new Color32[w * h];
        paint(px, w, h);
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
        tex.SetPixels32(px);
        tex.Apply(false, false);
        string path = $"{folder}/{name}.png";
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
        settings.spriteAlignment = (int)align;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spritePivot = align == SpriteAlignment.BottomCenter ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f);
        settings.filterMode = FilterMode.Point;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void Set(Color32[] px, int w, int x, int y, Color32 c)
    {
        if ((uint)x >= (uint)w || y < 0 || y >= px.Length / w) return;
        px[y * w + x] = c;
    }

    static void FillEllipse(Color32[] px, int w, int h, float cx, float cy, float rx, float ry, Color32 c)
    {
        if (rx < 0.1f || ry < 0.1f) return;
        int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx - 1));
        int x1 = Mathf.Min(w - 1, Mathf.CeilToInt(cx + rx + 1));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry - 1));
        int y1 = Mathf.Min(h - 1, Mathf.CeilToInt(cy + ry + 1));
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            float nx = (x + 0.5f - cx) / rx;
            float ny = (y + 0.5f - cy) / ry;
            if (nx * nx + ny * ny <= 1f)
                px[y * w + x] = c;
        }
    }

    static Color32 GrassColor(int x, int y, int variant)
    {
        int h = Hash(x + variant * 5, y + 3);
        if (variant == 3)
            return h % 6 == 0 ? Moss : GDark;
        if (h % 9 == 0) return G2;
        if (h % 4 == 0) return G1;
        return G0;
    }

    static void DrawGrass(Color32[] px, int w, int h, int variant)
    {
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            px[y * w + x] = GrassColor(x, y, variant);
        if (variant == 0)
        {
            Tuft(px, w, 4, 5);
            Tuft(px, w, 11, 11);
        }
        else if (variant == 1)
        {
            Tuft(px, w, 8, 3);
            Set(px, w, 12, 9, Flower);
            Set(px, w, 13, 9, Flower);
            Set(px, w, 12, 10, new Color32(230, 220, 200, 255));
        }
        else if (variant == 2)
        {
            Tuft(px, w, 13, 6);
            Set(px, w, 3, 12, Pebble);
            Set(px, w, 4, 12, D2);
            Set(px, w, 3, 13, D1);
        }
        else
        {
            Tuft(px, w, 6, 8);
            Tuft(px, w, 12, 4);
        }
    }

    static void Tuft(Color32[] px, int w, int x, int y)
    {
        Set(px, w, x, y, G2);
        Set(px, w, x, y + 1, GDark);
        Set(px, w, x - 1, y, G2);
    }

    static void DrawDirt(Color32[] px, int w, int h, int mask)
    {
        int variant = (mask * 3) & 1;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int n = Hash(x + mask, y + 9);
            px[y * w + x] = n % 8 == 0 ? D2 : (n % 3 == 0 ? D1 : D0);
        }
        if (variant == 0)
        {
            Set(px, w, 5, 4, Pebble);
            Set(px, w, 11, 10, D2);
        }
        else
        {
            Set(px, w, 8, 7, Pebble);
            Set(px, w, 9, 7, D2);
            Set(px, w, 3, 12, D2);
        }
        // mask bits: N=1 E=2 S=4 W=8, grass creeps in with a jagged edge
        if ((mask & 1) != 0) GrassEdge(px, w, h, 0, mask);
        if ((mask & 2) != 0) GrassEdge(px, w, h, 1, mask);
        if ((mask & 4) != 0) GrassEdge(px, w, h, 2, mask);
        if ((mask & 8) != 0) GrassEdge(px, w, h, 3, mask);
    }

    static void GrassEdge(Color32[] px, int w, int h, int side, int seed)
    {
        int len = (side & 1) == 0 ? w : h;
        for (int i = 0; i < len; i++)
        {
            int depth = Hash(i * 3, seed * 5) % 8;
            if (Hash(i, seed) % 5 == 0) depth = 1;
            for (int d = 0; d < depth; d++)
            {
                int x, y;
                if (side == 0) { x = i; y = h - 1 - d; }
                else if (side == 1) { x = w - 1 - d; y = i; }
                else if (side == 2) { x = i; y = d; }
                else { x = d; y = i; }
                Set(px, w, x, y, GrassColor(x, y, seed & 3));
            }
        }
    }

    static void DrawCliff(Color32[] px, int w, int h, int variant)
    {
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (y >= 13)
                px[y * w + x] = GrassColor(x, y, 0);
            else if (y == 12)
                px[y * w + x] = C2;
            else
            {
                int stripe = (x + variant) & 3;
                px[y * w + x] = stripe == 0 ? C2 : stripe == 1 ? C0 : C1;
                if ((y + variant) % 5 == 0 && stripe != 0)
                    px[y * w + x] = C2;
            }
        }
    }

    static void DrawFlower(Color32[] px, int w, int h)
    {
        Set(px, w, 7, 6, Flower);
        Set(px, w, 8, 6, Flower);
        Set(px, w, 7, 7, new Color32(236, 226, 210, 255));
        Set(px, w, 6, 6, G2);
        Set(px, w, 9, 6, G2);
    }

    static void DrawPebble(Color32[] px, int w, int h)
    {
        Set(px, w, 5, 4, Pebble);
        Set(px, w, 6, 4, D2);
        Set(px, w, 5, 5, D1);
        Set(px, w, 11, 9, Pebble);
        Set(px, w, 12, 9, D1);
    }

    static void LeafClump(Color32[] px, int w, int h, float cx, float cy, float rx, float ry, Color32 fill, int seed)
    {
        FillEllipse(px, w, h, cx, cy, rx + 1.1f, ry + 1.1f, Outline);
        int x0 = Mathf.Max(0, Mathf.FloorToInt(cx - rx - 1));
        int x1 = Mathf.Min(w - 1, Mathf.CeilToInt(cx + rx + 1));
        int y0 = Mathf.Max(0, Mathf.FloorToInt(cy - ry - 1));
        int y1 = Mathf.Min(h - 1, Mathf.CeilToInt(cy + ry + 1));
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            float nx = (x + 0.5f - cx) / rx;
            float ny = (y + 0.5f - cy) / ry;
            float d = nx * nx + ny * ny;
            if (d > 1f) continue;
            if (d > 0.62f && Hash(x + seed, y) % 3 == 0) continue;
            var tone = fill;
            if (Hash(x, y + seed) % 5 == 0) tone = LeafDk;
            if (ny > 0.15f && Hash(x + 2, y) % 4 == 0) tone = LeafHi;
            Set(px, w, x, y, tone);
        }
    }

    static void DrawTree(Color32[] px, int w, int h, int variant)
    {
        int trunkW = variant == 3 ? 7 : 5;
        int trunkH = variant == 0 ? 18 : 14;
        int tx = (w - trunkW) / 2 + (variant == 4 ? -2 : 0);
        for (int y = 0; y < trunkH; y++)
        for (int x = 0; x < trunkW; x++)
        {
            bool edge = x == 0 || x == trunkW - 1;
            bool grain = ((x + y / 2) % 3) == 0;
            Set(px, w, tx + x, y, edge || grain ? TrunkDk : Trunk);
        }
        Set(px, w, tx + 2, 6, new Color32(92, 60, 40, 255));
        Set(px, w, tx + 3, 6, TrunkDk);
        Set(px, w, tx + 2, 7, TrunkDk);
        Set(px, w, tx - 2, 1, Trunk);
        Set(px, w, tx - 1, 2, TrunkDk);
        Set(px, w, tx + trunkW, 1, Trunk);
        Set(px, w, tx + trunkW + 1, 2, TrunkDk);

        float ox = variant == 4 ? -3f : variant == 2 ? 2f : 0f;
        float oy = variant == 1 ? 22f : 28f;
        LeafClump(px, w, h, 16f + ox, oy, variant == 1 ? 11f : 8f, variant == 0 ? 9f : 7f, Leaf, variant + 1);
        LeafClump(px, w, h, 8f + ox, oy + 2f, 6f, 5f, Leaf, variant + 4);
        LeafClump(px, w, h, 23f + ox, oy + 1f, 6.5f, 5.5f, Leaf, variant + 8);
        LeafClump(px, w, h, 15f + ox, oy + 6f, 5f, 4f, LeafHi, variant + 12);
        if (variant != 3)
            LeafClump(px, w, h, 12f + ox, oy - 4f, 5f, 4f, LeafDk, variant + 15);
    }

    static void DrawBush(Color32[] px, int w, int h, int variant)
    {
        float cx = w * 0.5f;
        LeafClump(px, w, h, cx, h * 0.42f, variant == 0 ? 8f : 6.5f, variant == 0 ? 5.5f : 4.5f, Leaf, variant + 2);
        LeafClump(px, w, h, cx - 4f, h * 0.38f, 4f, 3.5f, Leaf, variant + 6);
        LeafClump(px, w, h, cx + 4f, h * 0.5f, 3.5f, 3f, LeafHi, variant + 9);
    }

    static void DrawTent(Color32[] px, int w, int h, int variant)
    {
        var cloth = variant == 0 ? new Color32(64, 118, 72, 255) : new Color32(118, 62, 78, 255);
        var fold = variant == 0 ? new Color32(36, 78, 58, 255) : new Color32(78, 36, 52, 255);
        var rim = variant == 0 ? new Color32(154, 186, 96, 255) : new Color32(168, 196, 110, 255);
        for (int y = 8; y <= 40; y++)
        {
            float t = (y - 8) / 32f;
            int sag = (y > 10 && y < 16) ? 2 : 0;
            int half = Mathf.RoundToInt(Mathf.Lerp(16f, 4f, t)) + sag;
            int mid = 20;
            for (int x = mid - half; x <= mid + half; x++)
            {
                bool edge = x <= mid - half + 1 || x >= mid + half - 1;
                int foldBand = (y / 4) % 3;
                bool foldCol = x == mid - 7 || x == mid - 2 || x == mid + 4;
                var c = cloth;
                if (foldCol || foldBand == 0) c = fold;
                if (x > mid && (y % 5 == 0)) c = rim;
                Set(px, w, x, y, edge ? Outline : c);
            }
        }
        for (int y = 4; y <= 42; y++)
        {
            Set(px, w, 7, y, y > 36 ? Bone : TrunkDk);
            Set(px, w, 32, y, y > 36 ? Bone : TrunkDk);
        }
        FillEllipse(px, w, h, 20f, 14f, 5.5f, 6.5f, Outline);
        FillEllipse(px, w, h, 20f, 14f, 4.2f, 5.2f, new Color32(28, 18, 36, 255));
        for (int i = 0; i < 7; i++)
        {
            Set(px, w, 8 + i / 2, 6 + i, Bone);
            Set(px, w, 31 - i / 2, 6 + i, Bone);
            Set(px, w, 9 + i / 2, 7 + i, BoneDk);
            Set(px, w, 30 - i / 2, 7 + i, BoneDk);
        }
        FillEllipse(px, w, h, 20f, 40f, 4.2f, 3.2f, Outline);
        FillEllipse(px, w, h, 20f, 40f, 3.2f, 2.3f, Bone);
        Set(px, w, 18, 40, BoneDk);
        Set(px, w, 21, 40, BoneDk);
        Set(px, w, 6, 7, TrunkDk);
        Set(px, w, 33, 7, TrunkDk);
        Set(px, w, 6, 8, Trunk);
        Set(px, w, 33, 8, Trunk);
        for (int i = 0; i < 6; i++)
        {
            Set(px, w, 7 + i, 8 + i, TrunkDk);
            Set(px, w, 32 - i, 8 + i, TrunkDk);
        }
    }

    static void DrawCrate(Color32[] px, int w, int h)
    {
        for (int y = 1; y <= 12; y++)
        for (int x = 2; x <= 13; x++)
        {
            bool edge = x == 2 || x == 13 || y == 1 || y == 12;
            bool plank = y == 5 || y == 9;
            Set(px, w, x, y, edge ? Outline : plank ? TrunkDk : Trunk);
        }
        Set(px, w, 4, 8, D0);
        Set(px, w, 11, 4, D0);
    }

    static void DrawStump(Color32[] px, int w, int h)
    {
        FillEllipse(px, w, h, 8f, 4f, 6.5f, 3.2f, Outline);
        FillEllipse(px, w, h, 8f, 4.5f, 5.4f, 2.4f, Trunk);
        FillEllipse(px, w, h, 8f, 8f, 5.2f, 3.4f, Outline);
        FillEllipse(px, w, h, 8f, 8.2f, 4.2f, 2.5f, D0);
        FillEllipse(px, w, h, 8f, 8.2f, 2.2f, 1.3f, TrunkDk);
    }

    static void DrawRock(Color32[] px, int w, int h)
    {
        FillEllipse(px, w, h, 8f, 4.5f, 6.5f, 4f, Outline);
        FillEllipse(px, w, h, 8f, 5f, 5.4f, 3f, new Color32(120, 116, 108, 255));
        FillEllipse(px, w, h, 6.5f, 6f, 2.4f, 1.4f, new Color32(168, 164, 150, 255));
        Set(px, w, 11, 4, new Color32(88, 84, 78, 255));
    }

    static void DrawBarrel(Color32[] px, int w, int h)
    {
        FillEllipse(px, w, h, 8f, 8f, 5.2f, 6.4f, Outline);
        FillEllipse(px, w, h, 8f, 8f, 4.2f, 5.5f, Trunk);
        for (int x = 4; x <= 12; x++)
        {
            Set(px, w, x, 5, TrunkDk);
            Set(px, w, x, 10, TrunkDk);
        }
        Set(px, w, 6, 8, D0);
        Set(px, w, 10, 7, TrunkDk);
    }

    static void DrawCauldron(Color32[] px, int w, int h)
    {
        FillEllipse(px, w, h, 10f, 5f, 7f, 3f, Outline);
        FillEllipse(px, w, h, 10f, 5.2f, 6f, 2.2f, TrunkDk);
        FillEllipse(px, w, h, 10f, 10f, 6.5f, 5f, Outline);
        FillEllipse(px, w, h, 10f, 10.5f, 5.3f, 3.8f, new Color32(110, 72, 42, 255));
        FillEllipse(px, w, h, 10f, 12f, 3.2f, 1.6f, new Color32(64, 92, 48, 255));
        Set(px, w, 8, 13, LeafHi);
    }

    static void DrawFlag(Color32[] px, int w, int h)
    {
        for (int y = 0; y < 26; y++)
            Set(px, w, 2, y, y % 3 == 0 ? TrunkDk : Trunk);
        for (int y = 16; y <= 24; y++)
        for (int x = 3; x <= 11; x++)
        {
            if (x > 8 && (y == 17 || y == 23)) continue;
            Set(px, w, x, y, x == 11 ? Outline : new Color32(72, 48, 78, 255));
        }
        Set(px, w, 6, 20, Bone);
        Set(px, w, 7, 20, Bone);
    }

    static void DrawBones(Color32[] px, int w, int h)
    {
        FillEllipse(px, w, h, 5f, 4f, 3.2f, 2f, Bone);
        FillEllipse(px, w, h, 12f, 5f, 3.4f, 2.1f, Bone);
        Set(px, w, 8, 4, Bone);
        Set(px, w, 9, 5, Bone);
        Set(px, w, 7, 6, BoneDk);
        Set(px, w, 13, 7, BoneDk);
        Set(px, w, 4, 6, BoneDk);
    }

    static void DrawShadow(Color32[] px, int w, int h)
    {
        FillEllipse(px, w, h, 9f, 3.5f, 8f, 3f, new Color32(40, 62, 36, 150));
    }

    static void EnsureSortingLayer(string layerName)
    {
        var assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets == null || assets.Length == 0) return;
        var so = new SerializedObject(assets[0]);
        var layers = so.FindProperty("m_SortingLayers");
        for (int i = 0; i < layers.arraySize; i++)
        {
            if (layers.GetArrayElementAtIndex(i).FindPropertyRelative("name").stringValue == layerName)
                return;
        }
        layers.InsertArrayElementAtIndex(0);
        var layer = layers.GetArrayElementAtIndex(0);
        layer.FindPropertyRelative("name").stringValue = layerName;
        var id = layer.FindPropertyRelative("uniqueID");
        if (id.propertyType == SerializedPropertyType.Integer)
            id.intValue = Random.Range(100000, 2000000000);
        else
            id.longValue = Random.Range(100000, 2000000000);
        layer.FindPropertyRelative("locked").intValue = 0;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
