using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using GameplayPrototype;

/// <summary>
/// Builds Assets/Scenes/ReferenceCombatSandbox.unity from Phase4Test baseline,
/// paints Rebuild art into an isolated forest, disables LevelFlow progress.
/// Does NOT modify GameplayPrototype_Phase4Test.
/// </summary>
public static class R1SandboxBuilder
{
    const string SourceScene = "Assets/Scenes/GameplayPrototype_Phase4Test.unity";
    const string SandboxScene = "Assets/Scenes/ReferenceCombatSandbox.unity";
    const string LandPath = "Assets/Data/AspectProfiles/Landscape_16x9.asset";
    const string PortPath = "Assets/Data/AspectProfiles/Portrait_9x16.asset";
    const string TreeA = "Assets/Art/Rebuild/Trees/Tree_Forest_A.png";
    const string TreeB = "Assets/Art/Rebuild/Trees/Tree_Forest_B.png";
    const string ShadowA = "Assets/Art/Rebuild/Trees/Tree_Shadow_A.png";
    const string ShadowB = "Assets/Art/Rebuild/Trees/Tree_Shadow_B.png";

    [MenuItem("Tools/Gameplay Prototype/R1/Build ReferenceCombatSandbox Scene")]
    public static void Build()
    {
        AspectProfileMenu.EnsureAssets();
        R1ArtGenerator.GenerateAll();

        if (!File.Exists(SourceScene))
        {
            Debug.LogError("[Sandbox] Missing source scene " + SourceScene);
            return;
        }

        if (File.Exists(SandboxScene))
            AssetDatabase.DeleteAsset(SandboxScene);
        AssetDatabase.CopyAsset(SourceScene, SandboxScene);
        AssetDatabase.Refresh();

        var scene = EditorSceneManager.OpenScene(SandboxScene, OpenSceneMode.Single);
        ConfigureSandbox(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Sandbox] Built " + SandboxScene);
    }

    static void ConfigureSandbox(Scene scene)
    {
        // Hide main campaign maps — sandbox uses its own Rebuild forest root.
        var maps = GameObject.Find("LevelMaps");
        if (maps != null) maps.SetActive(false);

        var forest = GameObject.Find("SandboxForest");
        if (forest != null) Object.DestroyImmediate(forest);
        forest = new GameObject("SandboxForest");
        SceneManager.MoveGameObjectToScene(forest, scene);

        BuildTilemap(forest.transform);
        SpawnTree(forest.transform, "Tree_A", new Vector3(-2.6f, 1.8f, 0f), TreeA, ShadowA);
        SpawnTree(forest.transform, "Tree_B", new Vector3(2.8f, 2.2f, 0f), TreeB, ShadowB);

        var systems = GameObject.Find("Systems");
        if (systems == null)
        {
            systems = new GameObject("Systems");
            SceneManager.MoveGameObjectToScene(systems, scene);
        }

        var flow = systems.GetComponent<LevelFlow>();
        if (flow != null) flow.enabled = false;
        var flowUi = systems.GetComponent<LevelFlowUI>();
        if (flowUi != null) flowUi.enabled = false;
        var mapCtrl = systems.GetComponent<LevelMapController>();
        if (mapCtrl != null) mapCtrl.enabled = false;
        var goCtrl = systems.GetComponent<GameOverController>();
        if (goCtrl != null) goCtrl.enabled = false;

        var aspect = systems.GetComponent<AspectProfileService>();
        if (aspect == null) aspect = systems.AddComponent<AspectProfileService>();
        var land = AssetDatabase.LoadAssetAtPath<AspectVisualProfile>(LandPath);
        var port = AssetDatabase.LoadAssetAtPath<AspectVisualProfile>(PortPath);
        var so = new SerializedObject(aspect);
        so.FindProperty("landscape").objectReferenceValue = land;
        so.FindProperty("portrait").objectReferenceValue = port;
        so.FindProperty("activeId").enumValueIndex = (int)AspectProfileId.Landscape;
        so.ApplyModifiedPropertiesWithoutUndo();

        var sandbox = systems.GetComponent<ReferenceSandboxController>();
        if (sandbox == null) sandbox = systems.AddComponent<ReferenceSandboxController>();
        var sso = new SerializedObject(sandbox);
        sso.FindProperty("startProfile").enumValueIndex = (int)AspectProfileId.Landscape;
        sso.ApplyModifiedPropertiesWithoutUndo();

        var player = GameObject.Find("Player");
        if (player != null)
            player.transform.position = new Vector3(0f, -1.5f, 0f);

        var cam = Camera.main;
        if (cam != null)
        {
            var ppc = cam.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
            if (ppc != null)
            {
                ppc.assetsPPU = 16;
                ppc.refResolutionX = 480;
                ppc.refResolutionY = 270;
            }
            var follow = cam.GetComponent<CameraFollow>();
            if (follow != null && player != null)
            {
                follow.SetTarget(player.transform);
                follow.SetBounds(new Vector2(-6f, -6f), new Vector2(6f, 8f));
            }
            cam.transform.position = new Vector3(0f, 0.5f, -10f);
        }

        // Ensure sandbox is in build settings (optional last).
        EnsureInBuildSettings(SandboxScene);
    }

    static void BuildTilemap(Transform parent)
    {
        var gridGo = new GameObject("RebuildGrid");
        gridGo.transform.SetParent(parent, false);
        var grid = gridGo.AddComponent<Grid>();
        grid.cellSize = new Vector3(1f, 1f, 0f);

        var groundGo = new GameObject("Ground");
        groundGo.transform.SetParent(gridGo.transform, false);
        var groundMap = groundGo.AddComponent<Tilemap>();
        var groundR = groundGo.AddComponent<TilemapRenderer>();
        groundR.sortingOrder = -20;
        groundMap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);

        var dirtGo = new GameObject("DirtPath");
        dirtGo.transform.SetParent(gridGo.transform, false);
        var dirtMap = dirtGo.AddComponent<Tilemap>();
        var dirtR = dirtGo.AddComponent<TilemapRenderer>();
        dirtR.sortingOrder = -19;

        Tile grass = LoadTile("GrassSoft");
        Tile grassV1 = LoadTile("GrassSoft_V1");
        Tile grassV2 = LoadTile("GrassSoft_V2");
        Tile dark = LoadTile("GrassDark");
        Tile dirt = LoadTile("Dirt");
        Tile[] dirtEdges = new Tile[16];
        for (int m = 0; m < 16; m++)
            dirtEdges[m] = LoadTile($"DirtM{m}");

        PaintGlade(groundMap, dirtMap, null, grass, grassV1, grassV2, dark, dirt, dirtEdges);
    }

    /// <summary>Repaint sandbox ground only. Does not touch Level 1–4 maps or regenerate art.</summary>
    [MenuItem("Tools/Gameplay Prototype/R1/Repaint Sandbox Ground")]
    public static void RepaintSandboxGround()
    {
        var grid = GameObject.Find("RebuildGrid");
        if (grid == null)
        {
            Debug.LogError("[R1.1] RebuildGrid missing. Open ReferenceCombatSandbox.");
            return;
        }
        var ground = grid.transform.Find("Ground")?.GetComponent<Tilemap>();
        var dirtMap = grid.transform.Find("DirtPath")?.GetComponent<Tilemap>();
        if (ground == null || dirtMap == null) return;

        var icT = grid.transform.Find("DirtCorners");
        Tilemap corners;
        if (icT == null)
        {
            var go = new GameObject("DirtCorners");
            go.transform.SetParent(grid.transform, false);
            corners = go.AddComponent<Tilemap>();
            var r = go.AddComponent<TilemapRenderer>();
            r.sortingOrder = -18;
        }
        else corners = icT.GetComponent<Tilemap>();

        ground.ClearAllTiles();
        dirtMap.ClearAllTiles();
        corners.ClearAllTiles();

        Tile grass = LoadTile("GrassSoft");
        Tile grassV1 = LoadTile("GrassSoft_V1");
        Tile grassV2 = LoadTile("GrassSoft_V2");
        Tile dark = LoadTile("GrassDark");
        Tile dirt = LoadTile("Dirt");
        var dirtEdges = new Tile[16];
        for (int m = 0; m < 16; m++) dirtEdges[m] = LoadTile($"DirtM{m}");
        PaintGlade(ground, dirtMap, corners, grass, grassV1, grassV2, dark, dirt, dirtEdges);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(ground.gameObject.scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(ground.gameObject.scene);
        Debug.Log("[R1.1] Sandbox ground repainted.");
    }

    static void PaintGlade(Tilemap groundMap, Tilemap dirtMap, Tilemap corners,
        Tile grass, Tile grassV1, Tile grassV2, Tile dark, Tile dirt, Tile[] dirtEdges)
    {
        int x0 = -7, x1 = 6, y0 = -8, y1 = 9;
        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            Tile t = grass;
            // Irregular variants. Dark cells are rare and never adjacent, so they do not form a lattice.
            int h = HashTile(x, y);
            if (h % 11 == 0) t = grassV1;
            else if (h % 13 == 0) t = grassV2;
            else if (h % 29 == 0 && HashTile(x + 3, y - 2) % 5 != 0) t = dark;
            if (t != null) groundMap.SetTile(new Vector3Int(x, y, 0), t);
        }

        var dirtCells = new bool[32, 32];
        int ox = 16, oy = 16;
        bool Inside(int x, int y) => x >= x0 && x <= x1 && y >= y0 && y <= y1;
        void Mark(int x, int y)
        {
            if (!Inside(x, y)) return;
            dirtCells[x + ox, y + oy] = true;
        }

        for (int y = y0 + 1; y <= y1 - 1; y++)
        {
            float wobble = Mathf.Sin(y * 0.62f) * 1.7f + Mathf.Sin(y * 0.21f + 1.3f) * 0.9f;
            int cx = Mathf.RoundToInt(wobble);
            int half = 1;
            if (HashTile(cx, y) % 5 == 0) half = 2;
            if (HashTile(y, 4) % 7 == 0) half = 0;
            for (int dx = -half; dx <= half; dx++)
                Mark(cx + dx, y);
        }
        // Shoulder bulge and a short spur so the path is not a uniform ribbon.
        for (int y = 1; y <= 4; y++)
            Mark(Mathf.RoundToInt(Mathf.Sin(2f) * 1.7f) + 2, y);
        for (int x = -1; x <= 2; x++)
            Mark(x, -2);

        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
            if (dirtCells[x + ox, y + oy] && dirt != null)
                dirtMap.SetTile(new Vector3Int(x, y, 0), dirt);

        bool DirtAt(int x, int y) => Inside(x, y) && dirtCells[x + ox, y + oy];

        for (int y = y0; y <= y1; y++)
        for (int x = x0; x <= x1; x++)
        {
            if (!DirtAt(x, y)) continue;
            bool n = DirtAt(x, y + 1);
            bool e = DirtAt(x + 1, y);
            bool s = DirtAt(x, y - 1);
            bool w = DirtAt(x - 1, y);
            if (n && e && s && w) continue;
            // Generator bits: grass on N=1, E=2, S=4, W=8.
            int mask = 0;
            if (!n) mask |= 1;
            if (!e) mask |= 2;
            if (!s) mask |= 4;
            if (!w) mask |= 8;
            if (mask != 0 && dirtEdges[mask] != null)
                dirtMap.SetTile(new Vector3Int(x, y, 0), dirtEdges[mask]);

            if (corners == null) continue;
            int diag = 0;
            if (!n && !e && !DirtAt(x + 1, y + 1)) diag |= 1;
            if (!s && !e && !DirtAt(x + 1, y - 1)) diag |= 2;
            if (!s && !w && !DirtAt(x - 1, y - 1)) diag |= 4;
            if (!n && !w && !DirtAt(x - 1, y + 1)) diag |= 8;
            if (diag == 0) continue;
            var ic = LoadTile(R1ArtGenerator.InnerCornerName(diag));
            if (ic != null) corners.SetTile(new Vector3Int(x, y, 0), ic);
        }
    }

    static int HashTile(int x, int y)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            return (int)(h & 0x7fffffff);
        }
    }

    static Tile LoadTile(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Tile>($"Assets/Art/Rebuild/Tiles/{name}Tile.asset");
    }

    static void SpawnTree(Transform parent, string name, Vector3 pos, string spritePath, string shadowPath)
    {
        var shadowSp = AssetDatabase.LoadAssetAtPath<Sprite>(shadowPath);
        if (shadowSp != null)
        {
            var sh = new GameObject(name + "_Shadow");
            sh.transform.SetParent(parent, false);
            sh.transform.position = pos + new Vector3(0.15f, -0.05f, 0f);
            var ssr = sh.AddComponent<SpriteRenderer>();
            ssr.sprite = shadowSp;
            ssr.sortingOrder = -5;
            ssr.color = new Color(1f, 1f, 1f, 0.85f);
        }

        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        sr.sortingOrder = 10;
        var sorter = go.AddComponent<SpriteSorter>();
        sorter.baseOrder = 10;
        var box = go.AddComponent<BoxCollider2D>();
        box.size = new Vector2(0.55f, 0.45f);
        box.offset = new Vector2(0f, 0.2f);
        go.AddComponent<TreeCanopyOcclusion>();
    }

    static void EnsureInBuildSettings(string path)
    {
        var list = new System.Collections.Generic.List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
        foreach (var s in list)
            if (s.path == path) return;
        list.Add(new EditorBuildSettingsScene(path, false));
        EditorBuildSettings.scenes = list.ToArray();
    }
}
