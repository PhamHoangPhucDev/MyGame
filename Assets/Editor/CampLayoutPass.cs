using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Repositions the existing camp. Does not rebuild props from scratch or touch gameplay.
/// </summary>
public static class CampLayoutPass
{
    const string ScenePath = "Assets/Scenes/GameplayPrototype.unity";
    const string Tiles = "Assets/Art/Tiles";

    [MenuItem("Tools/Gameplay Prototype/Arrange Camp Layout")]
    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        RepaintGround();
        ArrangeProps();

        var cam = Camera.main;
        if (cam != null)
        {
            cam.transparencySortMode = TransparencySortMode.CustomAxis;
            cam.transparencySortAxis = new Vector3(0f, 1f, 0f);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
            player.transform.position = new Vector3(0.2f, -6.2f, 0f);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Camp] Layout arranged: south path, lower clearing, upper camp.");
    }

    static void RepaintGround()
    {
        var ground = GameObject.Find("Ground").GetComponent<Tilemap>();
        var details = GameObject.Find("Details").GetComponent<Tilemap>();
        var grass = new[]
        {
            Load("GrassATile"), Load("GrassBTile"), Load("GrassCTile")
        };
        var dark = Load("GrassDarkTile");
        var flower = Load("FlowerTile");
        var dirt = new Tile[16];
        for (int i = 0; i < 16; i++)
            dirt[i] = Load("DirtM" + i + "Tile");

        foreach (var cell in ground.cellBounds.allPositionsWithin)
        {
            if (!ground.HasTile(cell)) continue;
            int x = cell.x;
            int y = cell.y;
            if (IsDirt(x, y))
            {
                int mask = 0;
                if (!IsDirt(x, y + 1) && ground.HasTile(new Vector3Int(x, y + 1, 0))) mask |= 1;
                if (!IsDirt(x + 1, y) && ground.HasTile(new Vector3Int(x + 1, y, 0))) mask |= 2;
                if (!IsDirt(x, y - 1) && ground.HasTile(new Vector3Int(x, y - 1, 0))) mask |= 4;
                if (!IsDirt(x - 1, y) && ground.HasTile(new Vector3Int(x - 1, y, 0))) mask |= 8;
                ground.SetTile(cell, dirt[mask]);
            }
            else if (IsMoss(x, y))
                ground.SetTile(cell, dark);
            else
                ground.SetTile(cell, grass[Hash(x, y) % 3]);
        }

        foreach (var cell in details.cellBounds.allPositionsWithin)
            if (details.HasTile(cell)) details.SetTile(cell, null);

        Vector3Int[] blooms =
        {
            new(-2, -6, 0), new(2, -4, 0), new(-2, -2, 0), new(2, 0, 0), new(-4, 6, 0)
        };
        foreach (var c in blooms)
            if (ground.HasTile(c) && !IsDirt(c.x, c.y))
                details.SetTile(c, flower);
    }

    static bool IsDirt(int x, int y)
    {
        if (y == -9 && (x == 0 || x == 1)) return true;
        if (y == -8 && (x == -1 || x == 0)) return true;
        if (y == -7 && (x == -1 || x == 0 || x == 1)) return true;
        if (y == -6 && (x == 0 || x == 1)) return true;
        if (y == -5 && (x == -1 || x == 0 || x == 1)) return true;
        if (y == -4 && (x == -1 || x == 0)) return true;
        if (y == -3 && x >= -1 && x <= 2) return true;
        if (y == -2 && x >= -1 && x <= 1) return true;
        if (y == -1 && x >= -2 && x <= 1) return true;
        if (y == 0 && x >= -2 && x <= 2) return true;
        if (y == 1 && x >= -3 && x <= 3) return true;
        if (y == 2 && x >= -3 && x <= 3) return true;
        if (y == 3 && x >= -2 && x <= 2) return true;
        if (y == 4 && x >= -1 && x <= 2) return true;
        return false;
    }

    static bool IsMoss(int x, int y)
    {
        if (x >= -8 && x <= -7 && y >= 2 && y <= 3) return true;
        if (x >= 6 && x <= 7 && y >= -6 && y <= -5) return true;
        if (x >= -8 && x <= -7 && y >= -5 && y <= -4) return true;
        return false;
    }

    static int Hash(int x, int y)
    {
        int n = x * 374761393 + y * 668265263;
        n = (n ^ (n >> 13)) * 1274126177;
        return n & 0x7fffffff;
    }

    static Tile Load(string name) => AssetDatabase.LoadAssetAtPath<Tile>($"{Tiles}/{name}.asset");

    static void ArrangeProps()
    {
        var props = GameObject.Find("ForestProps").transform;
        MoveAll(props, "Tree", new[]
        {
            new Vector2(-3.6f, 7.2f), new Vector2(-4.8f, 8.0f), new Vector2(-5.8f, 6.4f),
            new Vector2(3.4f, 7.4f), new Vector2(4.8f, 6.8f), new Vector2(5.6f, 8.0f),
            new Vector2(-6.2f, 2.0f), new Vector2(-5.4f, -1.2f), new Vector2(-4.2f, -5.6f),
            new Vector2(-3.4f, -7.6f), new Vector2(4.6f, -6.4f), new Vector2(5.4f, -3.2f),
            new Vector2(6.0f, 1.6f), new Vector2(6.2f, 3.8f)
        });
        MoveAll(props, "Bush", new[]
        {
            new Vector2(-2.5f, -7.0f), new Vector2(2.15f, -5.4f), new Vector2(-2.35f, -3.4f),
            new Vector2(2.05f, -1.45f), new Vector2(-3.15f, 1.7f), new Vector2(3.05f, 2.25f)
        });
        MoveAll(props, "Tent", new[]
        {
            new Vector2(-2.2f, 3.7f), new Vector2(2.45f, 5.5f)
        });
        MoveAll(props, "Campfire", new[] { new Vector2(0.7f, 3.25f) });
        MoveAll(props, "Flag", new[] { new Vector2(-0.35f, 4.15f) });
        MoveAll(props, "Cauldron", new[] { new Vector2(-1.15f, 3.85f) });
        MoveAll(props, "Barrel", new[] { new Vector2(1.4f, 3.45f), new Vector2(0.15f, 3.55f) });
        MoveAll(props, "Crate", new[] { new Vector2(1.35f, 4.05f) });
        MoveAll(props, "Stake", new[] { new Vector2(-2.7f, 3.1f), new Vector2(1.9f, 5.0f) });
        MoveAll(props, "Bones", new[] { new Vector2(-2.8f, 4.5f), new Vector2(1.6f, 5.6f) });
        MoveAll(props, "Stump", new[] { new Vector2(-4.6f, 2.4f) });
        MoveAll(props, "Rock", new[]
        {
            new Vector2(-3.4f, -5.2f), new Vector2(3.2f, -3.6f), new Vector2(3.4f, 6.6f)
        });
    }

    static void MoveAll(Transform parent, string name, Vector2[] spots)
    {
        int i = 0;
        foreach (Transform child in parent)
        {
            if (child.name != name) continue;
            if (i >= spots.Length) break;
            child.position = spots[i];
            i++;
        }
    }
}
