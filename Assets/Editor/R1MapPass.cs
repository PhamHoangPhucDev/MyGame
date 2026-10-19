using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Applies R1 ground and tree art to the maps already baked in the scene (LevelMaps/*), visuals only:
/// - recomputes the 4-bit dirt edge mask for every dirt cell (L4/L5 were baked with DirtM2 everywhere),
/// - places DirtIC_* inner-corner overlays on a "DirtCorners" tilemap above Ground,
/// - mixes GrassSoft_V1/V2 into GrassSoft cells to break tiling,
/// - gives tree ContactShadow children the new Tree_Shadow_A/B sprites.
/// Dirt cells stay dirt, no colliders or spawn data change. Safe to re-run; re-run after any map rebuild.
/// </summary>
public static class R1MapPass
{
    const string TileDir = "Assets/Art/Tiles";

    [MenuItem("Tools/Gameplay Prototype/R1/Apply Ground + Tree Pass To Scene Maps")]
    public static string Apply()
    {
        var root = GameObject.Find("LevelMaps");
        if (root == null) return "[R1] LevelMaps not found in the open scene.";

        var dirt = Load("DirtTile");
        var dirtM = new TileBase[16];
        for (int i = 0; i < 16; i++) dirtM[i] = Load($"DirtM{i}Tile");
        var grassSoft = Load("GrassSoftTile");
        var grassV1 = Load("GrassSoft_V1Tile");
        var grassV2 = Load("GrassSoft_V2Tile");
        var corners = new TileBase[16];
        for (int d = 1; d < 16; d++) corners[d] = Load(R1ArtGenerator.InnerCornerName(d) + "Tile");

        var report = new System.Text.StringBuilder();
        foreach (Transform map in root.transform)
        {
            Tilemap ground = null;
            foreach (var tm in map.GetComponentsInChildren<Tilemap>(true))
                if (tm.name == "Ground") { ground = tm; break; }
            if (ground == null) continue;

            int remasked = 0, cornerCount = 0, variants = 0;
            var cornerMap = GetOrCreateCornerMap(ground);
            cornerMap.ClearAllTiles();

            var bounds = ground.cellBounds;
            // Snapshot first so recomputing never reads a cell we already changed.
            var isDirt = new HashSet<Vector3Int>();
            var isGrass = new HashSet<Vector3Int>();
            foreach (var p in bounds.allPositionsWithin)
            {
                var t = ground.GetTile(p);
                if (t == null) continue;
                if (t.name.StartsWith("Dirt")) isDirt.Add(p); else isGrass.Add(p);
            }

            foreach (var p in isDirt)
            {
                bool n = isGrass.Contains(p + Vector3Int.up);
                bool e = isGrass.Contains(p + Vector3Int.right);
                bool s = isGrass.Contains(p + Vector3Int.down);
                bool w = isGrass.Contains(p + Vector3Int.left);
                int mask = (n ? 1 : 0) | (e ? 2 : 0) | (s ? 4 : 0) | (w ? 8 : 0);
                var want = mask == 0 ? dirt : dirtM[mask];
                if (ground.GetTile(p) != want) { ground.SetTile(p, want); remasked++; }

                int diag = 0;
                if (!n && !e && isGrass.Contains(p + new Vector3Int(1, 1, 0))) diag |= 1;
                if (!s && !e && isGrass.Contains(p + new Vector3Int(1, -1, 0))) diag |= 2;
                if (!s && !w && isGrass.Contains(p + new Vector3Int(-1, -1, 0))) diag |= 4;
                if (!n && !w && isGrass.Contains(p + new Vector3Int(-1, 1, 0))) diag |= 8;
                if (diag != 0 && corners[diag] != null) { cornerMap.SetTile(p, corners[diag]); cornerCount++; }
            }

            foreach (var p in isGrass)
            {
                var t = ground.GetTile(p);
                if (t == null || !t.name.StartsWith("GrassSoft")) continue;
                float h = Hash(p.x, p.y);
                var want = h < 0.15f ? grassV1 : h < 0.30f ? grassV2 : grassSoft;
                if (want != null && t != want) { ground.SetTile(p, want); variants++; }
            }

            int shadows = 0;
            foreach (var sr in map.GetComponentsInChildren<SpriteRenderer>(true))
            {
                if (sr.name != "ContactShadow" || sr.transform.parent == null) continue;
                var parentSr = sr.transform.parent.GetComponent<SpriteRenderer>();
                if (parentSr == null || parentSr.sprite == null) continue;
                string tree = parentSr.sprite.name;
                Sprite want = tree.StartsWith("Tree_Forest_A") ? LoadSprite("Tree_Shadow_A")
                    : tree.StartsWith("Tree_Forest_B") ? LoadSprite("Tree_Shadow_B") : null;
                if (want == null || sr.sprite == want) continue;
                Undo.RecordObject(sr, "R1 tree shadow");
                sr.sprite = want;
                shadows++;
            }

            report.Append($"{map.name}: dirt remasked {remasked}, inner corners {cornerCount}, grass variants {variants}, tree shadows {shadows}\n");
        }

        var scene = root.scene;
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        string result = "[R1] Map pass applied and scene saved.\n" + report;
        Debug.Log(result);
        return result;
    }

    static Tilemap GetOrCreateCornerMap(Tilemap ground)
    {
        var parent = ground.transform.parent;
        var existing = parent.Find("DirtCorners");
        Tilemap tm;
        if (existing != null) tm = existing.GetComponent<Tilemap>();
        else
        {
            var go = new GameObject("DirtCorners");
            Undo.RegisterCreatedObjectUndo(go, "R1 DirtCorners");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = ground.transform.localPosition;
            tm = go.AddComponent<Tilemap>();
            go.AddComponent<TilemapRenderer>();
        }
        tm.tileAnchor = ground.tileAnchor;
        var gr = ground.GetComponent<TilemapRenderer>();
        var cr = tm.GetComponent<TilemapRenderer>();
        cr.sortingLayerID = gr.sortingLayerID;
        cr.sortingOrder = gr.sortingOrder + 1; // above Ground, below Details/props
        cr.sharedMaterial = gr.sharedMaterial;
        return tm;
    }

    static TileBase Load(string name) => AssetDatabase.LoadAssetAtPath<TileBase>($"{TileDir}/{name}.asset");

    static Sprite LoadSprite(string name) =>
        AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Art/GoblinCampPhase4/{name}.png");

    static float Hash(int x, int y)
    {
        unchecked
        {
            uint h = (uint)(x * 73856093 ^ y * 19349663 ^ 0x5bd1e995);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0x1000000;
        }
    }
}
