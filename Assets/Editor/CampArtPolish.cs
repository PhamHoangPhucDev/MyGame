using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Repaints the existing camp tiles and props. Does not rebuild the map or touch gameplay.
/// </summary>
public static class CampArtPolish
{
    const string ScenePath = "Assets/Scenes/GameplayPrototype.unity";
    const string Tiles = "Assets/Art/Tiles";
    const string Env = "Assets/Art/Environment";

    static readonly Color32 G0 = new Color32(164, 194, 112, 255);
    static readonly Color32 G1 = new Color32(146, 178, 98, 255);
    static readonly Color32 G2 = new Color32(118, 150, 78, 255);
    static readonly Color32 Moss = new Color32(96, 128, 74, 255);
    static readonly Color32 D0 = new Color32(176, 146, 98, 255);
    static readonly Color32 D1 = new Color32(156, 126, 82, 255);
    static readonly Color32 Leaf = new Color32(86, 132, 72, 255);
    static readonly Color32 LeafDk = new Color32(52, 86, 58, 255);
    static readonly Color32 LeafHi = new Color32(176, 204, 118, 255);
    static readonly Color32 LeafLine = new Color32(36, 58, 40, 255);

    [MenuItem("Tools/Gameplay Prototype/Polish Camp Art")]
    public static void Polish()
    {
        WriteTile("GrassA", (px, w, h) => QuietGrass(px, w, h, 0));
        WriteTile("GrassB", (px, w, h) => QuietGrass(px, w, h, 1));
        WriteTile("GrassC", (px, w, h) => QuietGrass(px, w, h, 2));
        WriteTile("GrassDark", (px, w, h) => QuietGrass(px, w, h, 3));
        WriteTile("Flower", DrawQuietFlower);
        WriteTile("Pebble", DrawQuietPebble);
        for (int mask = 0; mask < 16; mask++)
        {
            int captured = mask;
            WriteTile("DirtM" + captured, (px, w, h) => QuietDirt(px, w, h, captured));
        }

        WriteProp("BushA", 24, 18, BushA);
        WriteProp("BushB", 20, 16, BushB);
        WriteProp("BushC", 22, 16, BushC);
        WriteProp("BushD", 26, 18, BushD);
        WriteProp("BushE", 18, 16, BushE);
        WriteProp("TentA", 40, 48, null, (px, w, h) => DrawTent(px, w, h, 0));
        WriteProp("TentB", 40, 48, null, (px, w, h) => DrawTent(px, w, h, 1));
        WriteProp("Shadow", 18, 8, null, DrawContactShadow, SpriteAlignment.Center);
        WriteProp("Campfire", 16, 16, null, DrawCampfire);
        WriteProp("Stake", 6, 14, null, DrawStake);

        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var details = GameObject.Find("Details")?.GetComponent<Tilemap>();
        if (details != null)
        {
            // Drop lone specks. Keep two quiet clusters off the path.
            Vector3Int[] drop =
            {
                new(-6, -9, 0), new(7, -3, 0), new(8, -1, 0), new(7, 1, 0),
                new(8, 4, 0), new(8, 6, 0), new(2, 7, 0), new(5, 6, 0),
                new(6, 7, 0), new(7, 11, 0), new(3, 12, 0)
            };
            foreach (var c in drop)
                details.SetTile(c, null);
        }

        var props = GameObject.Find("ForestProps").transform;
        AssignBush(props, new Vector2(-2.7f, -6.2f), "BushC");
        AssignBush(props, new Vector2(2.6f, -4.8f), "BushD");
        AssignBush(props, new Vector2(-2.5f, -1.6f), "BushE");
        AssignBush(props, new Vector2(3.2f, 0.2f), "BushA");
        AssignBush(props, new Vector2(-4.6f, 5.8f), "BushB");
        AssignBush(props, new Vector2(4.5f, 5.6f), "BushC");

        RemoveNamed(props, "Campfire");
        RemoveNamed(props, "Stake");
        var shadow = AssetDatabase.LoadAssetAtPath<Sprite>(Env + "/Shadow.png");
        Place(props, "Campfire", Load("Campfire"), new Vector2(1.15f, 4.25f), null, shadow);
        Place(props, "Stake", Load("Stake"), new Vector2(-2.55f, 3.55f), new Vector2(0.16f, 0.2f), shadow);
        Place(props, "Stake", Load("Stake"), new Vector2(2.45f, 2.55f), new Vector2(0.16f, 0.2f), shadow);
        Place(props, "Rock", LoadExistingRock(), new Vector2(-3.15f, 5.15f), new Vector2(0.5f, 0.24f), shadow);

        var cam = Camera.main;
        if (cam != null)
        {
            cam.transparencySortMode = TransparencySortMode.CustomAxis;
            cam.transparencySortAxis = new Vector3(0f, 1f, 0f);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Camp] Polished grass, dirt edges, bushes, tents, and a few camp props. Layout kept.");
    }

    static void AssignBush(Transform props, Vector2 pos, string spriteName)
    {
        foreach (Transform child in props)
        {
            if (child.name != "Bush") continue;
            if (Vector2.Distance(child.position, pos) > 0.2f) continue;
            var sr = child.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sprite = Load(spriteName);
        }
    }

    static Sprite Load(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{Env}/{name}.png");

    static Sprite LoadExistingRock() => AssetDatabase.LoadAssetAtPath<Sprite>($"{Env}/Rock.png");

    static void RemoveNamed(Transform parent, string name)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            if (child.name == name)
                Object.DestroyImmediate(child.gameObject);
        }
    }

    static void Place(Transform parent, string name, Sprite sprite, Vector2 pos, Vector2? collider, Sprite shadow)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = "Default";
        var sorter = go.AddComponent<GameplayPrototype.SpriteSorter>();
        sorter.baseOrder = 0;
        if (collider.HasValue)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.size = collider.Value;
            box.offset = new Vector2(0f, collider.Value.y * 0.5f);
        }
        if (shadow != null && name != "Stake")
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

    static void WriteTile(string name, System.Action<Color32[], int, int> paint)
    {
        WritePng($"{Tiles}/{name}.png", 16, 16, paint, SpriteAlignment.Center);
    }

    static void WriteProp(string name, int w, int h, string[] rows, System.Action<Color32[], int, int> paint = null, SpriteAlignment align = SpriteAlignment.BottomCenter)
    {
        WritePng($"{Env}/{name}.png", w, h, (px, ww, hh) =>
        {
            if (rows != null) Blit(px, ww, hh, rows);
            else paint(px, ww, hh);
        }, align);
    }

    static void WritePng(string path, int w, int h, System.Action<Color32[], int, int> paint, SpriteAlignment align)
    {
        var px = new Color32[w * h];
        paint(px, w, h);
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
        settings.spriteAlignment = (int)align;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spritePivot = align == SpriteAlignment.BottomCenter ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f);
        settings.filterMode = FilterMode.Point;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    static void Blit(Color32[] px, int w, int h, string[] rows)
    {
        if (rows.Length != h) throw new System.Exception("rows " + rows.Length + " != " + h);
        for (int row = 0; row < h; row++)
        {
            if (rows[row].Length != w) throw new System.Exception("len " + rows[row].Length + " at " + row);
            int y = h - 1 - row;
            for (int x = 0; x < w; x++)
                px[y * w + x] = BushInk(rows[row][x]);
        }
    }

    static Color32 BushInk(char c)
    {
        switch (c)
        {
            case 'o': return LeafLine;
            case 'm': return Leaf;
            case 'd': return LeafDk;
            case 'h': return LeafHi;
            default: return new Color32(0, 0, 0, 0);
        }
    }

    static void Set(Color32[] px, int w, int h, int x, int y, Color32 c)
    {
        if ((uint)x >= (uint)w || (uint)y >= (uint)h) return;
        px[y * w + x] = c;
    }

    static int Hash(int x, int y)
    {
        int n = x * 374761393 + y * 668265263;
        n = (n ^ (n >> 13)) * 1274126177;
        return n & 0x7fffffff;
    }

    static void QuietGrass(Color32[] px, int w, int h, int variant)
    {
        var baseCol = variant == 3 ? Moss : G0;
        for (int i = 0; i < px.Length; i++) px[i] = baseCol;
        if (variant == 0)
        {
            Set(px, w, h, 11, 5, G1);
            Set(px, w, h, 11, 6, G2);
        }
        else if (variant == 1)
        {
            Set(px, w, h, 4, 10, new Color32(186, 150, 186, 255));
            Set(px, w, h, 5, 10, new Color32(186, 150, 186, 255));
            Set(px, w, h, 4, 11, new Color32(230, 220, 200, 255));
        }
        else if (variant == 2)
        {
            Set(px, w, h, 12, 3, G2);
            Set(px, w, h, 13, 3, G1);
            Set(px, w, h, 12, 4, G2);
        }
        else
        {
            Set(px, w, h, 6, 8, G1);
            Set(px, w, h, 7, 8, new Color32(78, 108, 64, 255));
        }
    }

    static void DrawQuietFlower(Color32[] px, int w, int h)
    {
        Set(px, w, h, 7, 7, new Color32(186, 150, 186, 255));
        Set(px, w, h, 8, 7, new Color32(230, 220, 200, 255));
        Set(px, w, h, 7, 8, G2);
    }

    static void DrawQuietPebble(Color32[] px, int w, int h)
    {
        Set(px, w, h, 6, 5, new Color32(120, 108, 92, 255));
        Set(px, w, h, 7, 5, D1);
    }

    static void QuietDirt(Color32[] px, int w, int h, int mask)
    {
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
            px[y * w + x] = D0;
        if ((mask & 1) == 0)
        {
            Set(px, w, h, 5, 4, D1);
            Set(px, w, h, 11, 11, new Color32(132, 108, 74, 255));
        }
        if ((mask & 1) != 0) GrassFringe(px, w, h, 0, mask);
        if ((mask & 2) != 0) GrassFringe(px, w, h, 1, mask);
        if ((mask & 4) != 0) GrassFringe(px, w, h, 2, mask);
        if ((mask & 8) != 0) GrassFringe(px, w, h, 3, mask);
    }

    static void GrassFringe(Color32[] px, int w, int h, int side, int seed)
    {
        int len = (side & 1) == 0 ? w : h;
        for (int i = 0; i < len; i++)
        {
            int wave = Hash(i + seed * 3, seed) % 9;
            int depth = wave < 2 ? 0 : wave < 5 ? 2 : wave < 7 ? 4 : 6;
            if (Hash(i, seed + 4) % 6 == 0) depth += 2;
            for (int d = 0; d < depth; d++)
            {
                int x, y;
                if (side == 0) { x = i; y = h - 1 - d; }
                else if (side == 1) { x = w - 1 - d; y = i; }
                else if (side == 2) { x = i; y = d; }
                else { x = d; y = i; }
                var c = (d == depth - 1 && Hash(x, y) % 3 == 0) ? G2 : G0;
                if (d < 2 && Hash(x + 1, y) % 4 == 0) c = G1;
                Set(px, w, h, x, y, c);
            }
        }
    }

    static void DrawTent(Color32[] px, int w, int h, int variant)
    {
        var cloth = variant == 0 ? new Color32(58, 108, 68, 255) : new Color32(112, 58, 74, 255);
        var fold = variant == 0 ? new Color32(36, 74, 52, 255) : new Color32(74, 36, 52, 255);
        var hi = variant == 0 ? new Color32(138, 176, 96, 255) : new Color32(168, 126, 110, 255);
        var bone = new Color32(228, 220, 198, 255);
        var boneDk = new Color32(168, 156, 132, 255);
        var wood = new Color32(112, 74, 44, 255);
        var woodDk = new Color32(72, 46, 30, 255);
        int[] half = { 15, 15, 14, 14, 14, 13, 13, 12, 12, 11, 11, 11, 10, 10, 9, 9, 8, 8, 7, 7, 6, 6, 5, 5, 4, 4, 4, 3, 3, 3, 3, 2, 2 };
        for (int i = 0; i < half.Length; i++)
        {
            int y = 8 + i;
            int mid = 20 + ((i % 7 == 0) ? -1 : 0);
            int span = half[i];
            for (int x = mid - span; x <= mid + span; x++)
            {
                bool edge = x == mid - span || x == mid + span;
                bool seam = x == mid - 5 || x == mid + 4;
                var c = cloth;
                if (seam) c = fold;
                if (x > mid + 1 && i % 6 == 2) c = hi;
                Set(px, w, h, x, y, edge ? LeafLine : c);
            }
            if (i > 6 && i < 12)
                Set(px, w, h, mid, y, hi);
        }
        for (int y = 10; y <= 16; y++)
        {
            int rad = 5 - Mathf.Abs(y - 13);
            for (int x = 20 - rad; x <= 20 + rad; x++)
                Set(px, w, h, x, y, new Color32(24, 16, 32, 255));
            Set(px, w, h, 16, y, fold);
        }
        for (int i = 0; i < 8; i++)
        {
            Set(px, w, h, 9 + i / 2, 7 + i, bone);
            Set(px, w, h, 30 - i / 2, 7 + i, bone);
            Set(px, w, h, 10 + i / 2, 8 + i, boneDk);
            Set(px, w, h, 29 - i / 2, 8 + i, boneDk);
        }
        for (int y = 36; y <= 42; y++)
        for (int x = 17; x <= 23; x++)
        {
            int dx = x - 20;
            int dy = y - 39;
            if (dx * dx + dy * dy <= 10)
                Set(px, w, h, x, y, dx * dx + dy * dy > 6 ? boneDk : bone);
        }
        Set(px, w, h, 18, 39, new Color32(48, 32, 36, 255));
        Set(px, w, h, 21, 39, new Color32(48, 32, 36, 255));
        for (int y = 6; y <= 40; y++)
        {
            Set(px, w, h, 7, y, y % 3 == 0 ? woodDk : wood);
            Set(px, w, h, 32, y, y % 3 == 0 ? woodDk : wood);
        }
        for (int i = 0; i < 8; i++)
        {
            Set(px, w, h, 8 + i, 9 + i, woodDk);
            Set(px, w, h, 31 - i, 9 + i, woodDk);
        }
        Set(px, w, h, 12, 14, bone);
        Set(px, w, h, 27, 14, bone);
    }

    static void DrawContactShadow(Color32[] px, int w, int h)
    {
        var c = new Color32(36, 58, 40, 120);
        for (int y = 1; y <= 4; y++)
        for (int x = 2; x <= 15; x++)
        {
            int dx = x - 8;
            int dy = y - 3;
            if (dx * dx / 4 + dy * dy <= 6)
                px[y * w + x] = c;
        }
    }

    static void DrawCampfire(Color32[] px, int w, int h)
    {
        var wood = new Color32(112, 72, 42, 255);
        var woodDk = new Color32(64, 40, 28, 255);
        var fire = new Color32(232, 120, 36, 255);
        var core = new Color32(255, 210, 96, 255);
        for (int i = 0; i < 8; i++)
        {
            Set(px, w, h, 4 + i, 3 + i / 3, wood);
            Set(px, w, h, 11 - i, 3 + i / 3, woodDk);
        }
        Set(px, w, h, 7, 6, fire);
        Set(px, w, h, 8, 6, fire);
        Set(px, w, h, 7, 7, core);
        Set(px, w, h, 8, 7, core);
        Set(px, w, h, 8, 8, core);
        Set(px, w, h, 6, 7, fire);
        Set(px, w, h, 9, 7, fire);
        Set(px, w, h, 8, 9, new Color32(255, 244, 210, 255));
    }

    static void DrawStake(Color32[] px, int w, int h)
    {
        var wood = new Color32(120, 78, 46, 255);
        var woodDk = new Color32(72, 46, 30, 255);
        for (int y = 0; y < 12; y++)
        {
            Set(px, w, h, 2, y, woodDk);
            Set(px, w, h, 3, y, wood);
        }
        Set(px, w, h, 2, 12, woodDk);
        Set(px, w, h, 1, 11, wood);
        Set(px, w, h, 4, 11, wood);
    }

    static readonly string[] BushA =
    {
        "........................",
        "...........oohh.........",
        ".........oomhhhho.......",
        ".......oommmhhhhho......",
        ".....oommmmmmmhhho......",
        "....ommmmdddmmmmmo......",
        "...ommmmdddddmmmmo......",
        "...ommddddddddmmo.......",
        "..oommddddddmmmo........",
        "..ommmddddddmmo.........",
        "..oommmddddmmo..........",
        "...oommmmddmo...........",
        ".....oommmmo............",
        ".......ooooo............",
        "........................",
        "........................",
        "........................",
        "........................",
    };

    static readonly string[] BushB =
    {
        "....................",
        "......oohh..........",
        "....oommhhho........",
        "...ommmmhhhho.......",
        "..ommmdddmmhho......",
        "..ommdddddmmmo......",
        ".oommddddddmmo......",
        ".oommmddddmmo.......",
        "..oommmmddmo........",
        "...oooommmo.........",
        ".......ooo..........",
        "....................",
        "....................",
        "....................",
        "....................",
        "....................",
    };

    static readonly string[] BushC =
    {
        "......................",
        "....oo................",
        "...ommo..oohh.........",
        "..ommmmoomhhhho.......",
        "..ommddmmmhhhhho......",
        ".oomdddddmmmmmmo......",
        ".ommmddddddmmmmo......",
        "..ommdddddddmmo.......",
        "...ommdddddmmo........",
        "....oommmmmo..........",
        "......ooooo...........",
        "......................",
        "......................",
        "......................",
        "......................",
        "......................",
    };

    static readonly string[] BushD =
    {
        "..........................",
        ".............ooh..........",
        "...........oomhho.........",
        "....oo...oommmhhho........",
        "...ommo.oommmmhhhho.......",
        "..ommddmmmmdddmmhhho......",
        "..omdddddddddddmmmo.......",
        ".oommdddddddddddmmo.......",
        ".oommmmddddddddmmo........",
        "..oommmmdddddmmo..........",
        "....oommmmmmmo............",
        "......oooooo..............",
        "..........................",
        "..........................",
        "..........................",
        "..........................",
        "..........................",
        "..........................",
    };

    static readonly string[] BushE =
    {
        "..................",
        "........oohh......",
        "......oomhhhho....",
        ".....ommmmhhho....",
        "....ommdddmmmo....",
        "...ommdddddmmo....",
        "...omddddddmo.....",
        "....ommdddmo......",
        ".....oommmo.......",
        ".......ooo........",
        "..................",
        "..................",
        "..................",
        "..................",
        "..................",
        "..................",
    };
}
