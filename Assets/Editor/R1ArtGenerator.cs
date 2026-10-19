using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// R1 reference rebuild art — writes ONLY under Assets/Art/Rebuild (+ Resources/VFX
/// for runtime slash sheets). Never overwrites Phase 4 / Tiles / GoblinCampPhase4.
/// </summary>
public static class R1ArtGenerator
{
    const string TileDir = "Assets/Art/Rebuild/Tiles";
    const string TreeDir = "Assets/Art/Rebuild/Trees";
    const string VfxDir = "Assets/Art/Rebuild/VFX";
    const string ResourcesVfxDir = "Assets/Resources/VFX";

    // Palette (sampled = median from reference frames, see manifest).
    static readonly Color32 GrassBase = Hex("74A587");    // sampled avg C25/C22/C04
    static readonly Color32 GrassNoise = Hex("6E9F81");
    static readonly Color32 GrassTuft = Hex("5E8F73");
    static readonly Color32 GrassHi = Hex("8BBF9C");
    static readonly Color32 DarkBase = Hex("62907A");     // kept close to GrassBase: dark patches are square cells today
    static readonly Color32 DarkNoise = Hex("5B8872");
    static readonly Color32 DarkTuft = Hex("4F7D66");
    static readonly Color32 DirtBase = Hex("D59C6B");     // sampled C25/C26
    static readonly Color32 DirtLight = Hex("DDA877");
    static readonly Color32 DirtShade = Hex("C4895B");
    static readonly Color32 DirtPebble = Hex("CD8766");   // sampled C26
    static readonly Color32 EdgeOutline = Hex("3F635C");  // sampled C25
    static readonly Color32 Clear = new(0, 0, 0, 0);

    const int EndDepth = 3; // fringe depth at tile corners so neighbouring edge tiles line up

    [MenuItem("Tools/Gameplay Prototype/R1/Generate All R1 Art")]
    public static void GenerateAll()
    {
        GenerateGround();
        GenerateTrees();
        GenerateSlash();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[R1] Ground, tree and slash art generated.");
    }

    // ---------------------------------------------------------------- Ground

    [MenuItem("Tools/Gameplay Prototype/R1/Generate Ground Tiles")]
    public static void GenerateGround()
    {
        Directory.CreateDirectory(TileDir);
        WriteNewTile("GrassSoft", Grass(11, 4));
        WriteNewTile("GrassSoft_V1", Grass(23, 2));
        WriteNewTile("GrassSoft_V2", Grass(37, 6));
        WriteNewTile("GrassDark", GrassDarkTile(51));
        WriteNewTile("Dirt", DirtFill(7));
        for (int mask = 0; mask < 16; mask++)
            WriteNewTile($"DirtM{mask}", DirtEdge(mask));
        // Inner corners: diagonal grass (NE=1, SE=2, SW=4, NW=8) with both adjacent sides dirt.
        for (int diag = 1; diag < 16; diag++)
            WriteNewTile(InnerCornerName(diag), InnerCorner(diag), transparent: true);
    }

    public static string InnerCornerName(int diag)
    {
        var parts = new List<string>();
        if ((diag & 1) != 0) parts.Add("NE");
        if ((diag & 2) != 0) parts.Add("SE");
        if ((diag & 4) != 0) parts.Add("SW");
        if ((diag & 8) != 0) parts.Add("NW");
        return "DirtIC_" + string.Join("_", parts);
    }

    static Color32[] Grass(int seed, int tufts)
    {
        var px = Fill(16, 16, GrassBase);
        for (int i = 0; i < px.Length; i++)
            if (Hash(i % 16, i / 16, seed) < 0.16f) px[i] = GrassNoise;
        // V-shaped tufts kept 1 px away from the borders so tiling never cuts one in half.
        for (int t = 0; t < tufts; t++)
        {
            int x = 1 + (int)(Hash(t, 3, seed) * 12f);
            int y = 1 + (int)(Hash(t, 9, seed) * 12f);
            Put(px, 16, x, y, GrassTuft);
            Put(px, 16, x + 2, y, GrassTuft);
            Put(px, 16, x + 1, y - 1, GrassTuft);
            if (Hash(t, 17, seed) > 0.5f) Put(px, 16, x, y + 1, GrassHi);
        }
        return px;
    }

    static Color32[] GrassDarkTile(int seed)
    {
        var px = Fill(16, 16, DarkBase);
        for (int i = 0; i < px.Length; i++)
            if (Hash(i % 16, i / 16, seed) < 0.18f) px[i] = DarkNoise;
        for (int t = 0; t < 3; t++)
        {
            int x = 1 + (int)(Hash(t, 5, seed) * 12f);
            int y = 1 + (int)(Hash(t, 7, seed) * 12f);
            Put(px, 16, x, y, DarkTuft);
            Put(px, 16, x + 2, y, DarkTuft);
            Put(px, 16, x + 1, y - 1, DarkTuft);
        }
        return px;
    }

    static Color32[] DirtFill(int seed)
    {
        var px = Fill(16, 16, DirtBase);
        for (int i = 0; i < px.Length; i++)
        {
            float h = Hash(i % 16, i / 16, seed);
            if (h < 0.08f) px[i] = DirtLight;
            else if (h > 0.95f) px[i] = DirtShade;
        }
        // Two small pebbles away from the borders.
        Put(px, 16, 4, 11, DirtPebble); Put(px, 16, 5, 11, DirtPebble);
        Put(px, 16, 11, 4, DirtPebble);
        return px;
    }

    // Jagged fringe depth along one side, i = 0..15. Ends are fixed so tiles line up.
    static int Depth(int side, int i)
    {
        if (i <= 0 || i >= 15) return EndDepth;
        int[] teeth = { 3, 4, 5, 4, 3, 5, 6, 5, 4, 3, 4, 6, 5, 4, 3, 3 };
        int v = teeth[(i + side * 5) % 16];
        return Mathf.Clamp(v, 2, 6);
    }

    // Mask bits (CampLayoutPass/ForestTilemapBuilder): grass at N=1, E=2, S=4, W=8.
    static Color32[] DirtEdge(int mask)
    {
        var px = DirtFill(7 + mask);
        var grass = Grass(101 + mask, 1);
        var isGrass = new bool[256];
        for (int y = 0; y < 16; y++)
        for (int x = 0; x < 16; x++)
        {
            bool g = false;
            if ((mask & 1) != 0 && 15 - y < Depth(0, x)) g = true;
            if ((mask & 2) != 0 && 15 - x < Depth(1, y)) g = true;
            if ((mask & 4) != 0 && y < Depth(2, x)) g = true;
            if ((mask & 8) != 0 && x < Depth(3, y)) g = true;
            isGrass[y * 16 + x] = g;
        }
        PaintFringe(px, grass, isGrass);
        return px;
    }

    static Color32[] InnerCorner(int diag)
    {
        var px = Fill(16, 16, Clear);
        var grass = Grass(211 + diag, 0);
        var isGrass = new bool[256];
        for (int y = 0; y < 16; y++)
        for (int x = 0; x < 16; x++)
        {
            int dx = 0, dy = 0;
            bool g = false;
            // Corner blob = EndDepth square plus a 1 px rounded tooth, matching the
            // fringe ends of the two neighbouring edge tiles.
            if ((diag & 1) != 0) { dx = 15 - x; dy = 15 - y; g |= InBlob(dx, dy); }
            if ((diag & 2) != 0) { dx = 15 - x; dy = y; g |= InBlob(dx, dy); }
            if ((diag & 4) != 0) { dx = x; dy = y; g |= InBlob(dx, dy); }
            if ((diag & 8) != 0) { dx = x; dy = 15 - y; g |= InBlob(dx, dy); }
            isGrass[y * 16 + x] = g;
        }
        PaintFringe(px, grass, isGrass, overlayOnly: true);
        return px;
    }

    static bool InBlob(int dx, int dy) => (dx < EndDepth && dy < EndDepth) || (dx + dy < EndDepth + 2 && dx < EndDepth + 1 && dy < EndDepth + 1);

    // Grass pixels where isGrass; 1 px dark outline on the grass lip; 1-2 px shade on the dirt below it.
    static void PaintFringe(Color32[] px, Color32[] grass, bool[] isGrass, bool overlayOnly = false)
    {
        var outPx = (Color32[])px.Clone();
        for (int y = 0; y < 16; y++)
        for (int x = 0; x < 16; x++)
        {
            int i = y * 16 + x;
            if (isGrass[i])
            {
                bool lip = (x > 0 && !isGrass[i - 1]) || (x < 15 && !isGrass[i + 1]) ||
                           (y > 0 && !isGrass[i - 16]) || (y < 15 && !isGrass[i + 16]);
                outPx[i] = lip ? EdgeOutline : grass[i];
                continue;
            }
            // Shade dirt touching the lip from below or the side (light comes from above).
            bool under = (y < 15 && isGrass[i + 16]) || (x > 0 && isGrass[i - 1]) || (x < 15 && isGrass[i + 1]);
            bool under2 = y < 14 && isGrass[i + 32];
            if (under) outPx[i] = DirtShade;
            else if (under2 && Hash(x, y, 77) > 0.4f) outPx[i] = DirtShade;
            else if (overlayOnly) outPx[i] = Clear;
        }
        Array.Copy(outPx, px, px.Length);
    }

    // ---------------------------------------------------------------- Trees

    static readonly Color32 CanopyHi = Hex("A3D6B5");
    static readonly Color32 CanopyLight = Hex("7FC09F");  // sampled C25
    static readonly Color32 CanopyMid = Hex("5E9C86");
    static readonly Color32 CanopyShadow = Hex("4A727C"); // sampled C19
    static readonly Color32 CanopyDeep = Hex("3F635C");   // sampled C25
    static readonly Color32 CanopyOutline = Hex("2B4644");
    static readonly Color32 Trunk = Hex("88574A");        // sampled C25
    static readonly Color32 TrunkDark = Hex("6D4F42");    // sampled C19
    static readonly Color32 TrunkLight = Hex("9C6A58");
    static readonly Color32 TrunkOutline = Hex("3E2A26");
    static readonly Color32 ShadowColor = new(0x1E, 0x2C, 0x28, 110); // same as Shadow_Large

    [MenuItem("Tools/Gameplay Prototype/R1/Generate Trees")]
    public static void GenerateTrees()
    {
        Directory.CreateDirectory(TreeDir);
        // Canvas sizes and trunk bands must match TreeCanopyOcclusion.TrunkPixels:
        // >=75 px tall -> 24 px trunk band, 65-74 px -> 20 px.
        // Pivot bottom-center so Y-sort / canopy split match Phase 4 trees.
        WriteNewSprite($"{TreeDir}/Tree_Forest_A.png", Tree(62, 78, 24, seed: 3, lean: 0), new Vector2(0.5f, 0f), 62, 78);
        WriteNewSprite($"{TreeDir}/Tree_Forest_B.png", Tree(54, 68, 20, seed: 8, lean: 2), new Vector2(0.5f, 0f), 54, 68);
        WriteNewSprite($"{TreeDir}/Tree_Shadow_A.png", ShadowEllipse(56, 14), new Vector2(0.5f, 0.5f), 56, 14);
        WriteNewSprite($"{TreeDir}/Tree_Shadow_B.png", ShadowEllipse(48, 12), new Vector2(0.5f, 0.5f), 48, 12);
    }

    static Color32[] Tree(int w, int h, int band, int seed, int lean)
    {
        var px = Fill(w, h, Clear);
        int cx = w / 2;

        // Trunk + roots: rows 0..band-1 only. Anything above the band would sit in the canopy
        // sprite and fade with it (seen as a faded trunk segment in occlusion screenshots).
        for (int y = 0; y < band; y++)
        {
            float t = y / (float)band;
            int half = Mathf.RoundToInt(Mathf.Lerp(7.5f, 4.5f, Mathf.Clamp01(t * 1.4f)));
            if (y < 3) half += 3 - y; // root flare
            int shift = Mathf.RoundToInt(lean * Mathf.Clamp01(t));
            for (int x = cx - half + shift; x <= cx + half + shift; x++)
            {
                if (x < 0 || x >= w) continue;
                int rel = x - (cx - half + shift);
                Color32 c = rel <= 1 ? TrunkDark : (rel >= 2 * half - 2 ? TrunkLight : Trunk);
                if (y < 2 && (rel == 2 || rel == 2 * half - 2)) c = TrunkDark; // root split
                px[y * w + x] = c;
            }
        }
        // Two short roots.
        for (int i = 0; i < 4; i++)
        {
            Put(px, w, cx - 9 - i / 2, i / 2, TrunkDark);
            Put(px, w, cx + 9 + i / 2, i / 2, TrunkDark);
        }

        // Canopy: union of puffs. Everything sits at or above the trunk band.
        var rnd = new System.Random(seed);
        int top = h - 1;
        int bottom = band; // canopy starts exactly where the trunk band ends
        float ccy = (top + bottom) * 0.5f + 1f;
        float rx = w * 0.5f - 1.5f;
        float ry = (top - bottom) * 0.5f;
        var puffs = new List<Vector3>();
        int count = 11;
        for (int i = 0; i < count; i++)
        {
            float a = i / (float)count * Mathf.PI * 2f + (float)rnd.NextDouble() * 0.3f;
            float pr = Mathf.Min(rx, ry) * (0.36f + (float)rnd.NextDouble() * 0.08f);
            // Keep each puff inside the canopy ellipse so the bottom never gets cut flat.
            float jitter = 0.9f + (float)rnd.NextDouble() * 0.08f;
            var c = new Vector2(cx + lean + Mathf.Cos(a) * (rx - pr) * jitter, ccy + Mathf.Sin(a) * (ry - pr) * jitter);
            puffs.Add(new Vector3(c.x, c.y, pr));
        }
        puffs.Add(new Vector3(cx + lean, ccy, Mathf.Min(rx, ry) * 0.75f));
        // Skirt puffs: a wide underside that touches the band row and covers the trunk top,
        // so the trunk-to-canopy seam is hidden under foliage like the reference trees.
        float skirt = Mathf.Min(rx, ry) * 0.42f;
        for (int k = -1; k <= 1; k++)
            puffs.Add(new Vector3(cx + lean * 0.5f + k * skirt * 0.9f, bottom + skirt - 0.5f, skirt));

        var inCanopy = new bool[w * h];
        for (int y = bottom; y < h; y++)
        for (int x = 0; x < w; x++)
        foreach (var p in puffs)
            if ((x - p.x) * (x - p.x) + (y - p.y) * (y - p.y) <= p.z * p.z) { inCanopy[y * w + x] = true; break; }

        // Upper "light layer": the same puffs shifted up and shrunk, giving the two-tone cloud look.
        var inLight = new bool[w * h];
        var lightPuff = new int[w * h];
        for (int y = bottom; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            if (!inCanopy[y * w + x]) continue;
            for (int pi = 0; pi < puffs.Count; pi++)
            {
                var p = puffs[pi];
                float lx = p.x - (p.x - (cx + lean)) * 0.18f;
                float ly = p.y + ry * 0.22f;
                float lr = p.z * 0.82f;
                if ((x - lx) * (x - lx) + (y - ly) * (y - ly) <= lr * lr) { inLight[y * w + x] = true; lightPuff[y * w + x] = pi; break; }
            }
        }

        for (int y = bottom; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            int i = y * w + x;
            if (!inCanopy[i]) continue;
            bool edge = x == 0 || x == w - 1 || y == h - 1 || !inCanopy[i - 1] || !inCanopy[i + 1] ||
                        !inCanopy[i - w] || (y + 1 < h && !inCanopy[i + w]);
            if (edge) { px[i] = CanopyOutline; continue; }
            if (inLight[i])
            {
                bool lightEdge = !inLight[i - w] || !inLight[i - 1] || !inLight[i + 1];
                // Scallop seams: where the clump below belongs to a different puff, draw a 1 px mid line
                // so the light top reads as overlapping leaf clusters (C19 0:08.4).
                bool seam = inLight[i - w] && lightPuff[i - w] != lightPuff[i] && lightPuff[i] != puffs.Count - 1;
                px[i] = lightEdge || seam ? CanopyMid : CanopyLight;
                // Small highlight flecks near the top of each light puff.
                if (y + 2 < h && inLight[i] && !inLight[i + 2 * w] && Hash(x, y, seed) > 0.55f) px[i] = CanopyHi;
                continue;
            }
            float lowT = (y - bottom) / (float)(h - bottom);
            px[i] = lowT < 0.18f ? CanopyDeep : CanopyShadow;
        }

        // Hard guarantee for TreeCanopyOcclusion: nothing but trunk below the band.
        for (int y = 0; y < band; y++)
        for (int x = 0; x < w; x++)
        {
            var c = px[y * w + x];
            if (c.a > 0 && !IsTrunkColor(c)) px[y * w + x] = Clear;
        }
        OutlineTrunk(px, w, band);
        return px;
    }

    static bool IsTrunkColor(Color32 c) =>
        Same(c, Trunk) || Same(c, TrunkDark) || Same(c, TrunkLight) || Same(c, TrunkOutline);

    static void OutlineTrunk(Color32[] px, int w, int rows)
    {
        var copy = (Color32[])px.Clone();
        for (int y = 0; y < rows; y++)
        for (int x = 0; x < w; x++)
        {
            int i = y * w + x;
            if (copy[i].a != 0) continue;
            bool touch = (x > 0 && IsTrunkColor(copy[i - 1])) || (x < w - 1 && IsTrunkColor(copy[i + 1])) ||
                         (y + 1 < rows && IsTrunkColor(copy[i + w]));
            if (touch) px[i] = TrunkOutline;
        }
    }

    static Color32[] ShadowEllipse(int w, int h)
    {
        var px = Fill(w, h, Clear);
        float rx = w * 0.5f, ry = h * 0.5f;
        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float nx = (x + 0.5f - rx) / rx, ny = (y + 0.5f - ry) / ry;
            if (nx * nx + ny * ny <= 1f) px[y * w + x] = ShadowColor;
        }
        return px;
    }

    // ---------------------------------------------------------------- Slash

    static readonly Color32 SlashCore = Hex("FFFFFF");
    static readonly Color32 SlashGlow = Hex("DFFBF8");
    static readonly Color32 SlashEdge = Hex("6CE1D6"); // sampled C25
    static readonly Color32 SlashRim = Hex("3FB8C8");
    static readonly Color32 SlashTip = Hex("1F7F99");

    [MenuItem("Tools/Gameplay Prototype/R1/Generate Slash VFX")]
    public static void GenerateSlash()
    {
        Directory.CreateDirectory(VfxDir);
        Directory.CreateDirectory(ResourcesVfxDir);
        // A: sweep top -> bottom. B: bottom -> top, slightly thinner/tighter. C: finisher.
        // Write to Rebuild (canonical) then Resources (runtime CombatVfx load path).
        foreach (var dir in new[] { VfxDir, ResourcesVfxDir })
        {
            WriteSheetTo(dir, "VFX_SwordSlash_A", 96, 4, f => SlashFrame(96, 12, 56, 21, f, downward: true, inner: false), new Vector2(12f / 96f, 0.5f));
            WriteSheetTo(dir, "VFX_SwordSlash_B", 96, 4, f => SlashFrame(96, 12, 52, 18, f, downward: false, inner: false), new Vector2(12f / 96f, 0.5f));
            WriteSheetTo(dir, "VFX_SwordSlash_C", 128, 4, f => SlashFrame(128, 12, 76, 28, f, downward: true, inner: true), new Vector2(12f / 128f, 0.5f));
        }
    }

    // Crescent between an outer circle (radius ro, centre C) and a larger inner circle centred d behind C.
    // d is solved so the crescent is `thick` px at 0 deg and closes to a point exactly at +/-maxAngle.
    static Color32[] SlashFrame(int size, int pivotX, float ro, float thick, int frame, bool downward, bool inner)
    {
        var px = Fill(size, size, Clear);
        float cx = pivotX, cy = size * 0.5f;
        const float maxAngle = 62f;
        float cosMax = Mathf.Cos(maxAngle * Mathf.Deg2Rad);
        float d = (ro * ro - (ro - thick) * (ro - thick)) / (2f * (ro - thick - ro * cosMax));
        // Visible angle window per frame (deg, + = up). Frame 0 enters, 1 full, 2 trailing, 3 fading.
        float lo, hi, thickScale; bool fade;
        switch (frame)
        {
            case 0: lo = 12f; hi = maxAngle; thickScale = 0.55f; fade = false; break;
            case 1: lo = -maxAngle; hi = maxAngle; thickScale = 1f; fade = false; break;
            case 2: lo = -maxAngle; hi = 28f; thickScale = 0.8f; fade = false; break;
            default: lo = -maxAngle; hi = -12f; thickScale = 0.5f; fade = true; break;
        }
        if (!downward) { float t = lo; lo = -hi; hi = -t; }
        float riF = ro - thick * thickScale + d;

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float px0 = x + 0.5f - cx, py0 = y + 0.5f - cy;
            float r = Mathf.Sqrt(px0 * px0 + py0 * py0);
            if (r > ro) continue;
            float ix = px0 + d;
            float rInner = Mathf.Sqrt(ix * ix + py0 * py0);
            if (rInner <= riF) continue;
            float ang = Mathf.Atan2(py0, px0) * Mathf.Rad2Deg;
            if (ang < lo || ang > hi) continue;

            // Local thickness position: 0 at the outer rim, 1 at the inner edge (exact ray/inner-circle hit).
            float ca = Mathf.Cos(ang * Mathf.Deg2Rad);
            float tIn = -d * ca + Mathf.Sqrt(Mathf.Max(0f, d * d * ca * ca - d * d + riF * riF));
            float local = Mathf.Max(0.001f, ro - tIn);
            float u = (ro - r) / local;
            float tipT = Mathf.Abs(ang) / maxAngle; // 0 centre .. 1 tips
            Color32 c;
            if (u < 0.12f) c = SlashRim;
            else if (u < 0.3f) c = SlashEdge;
            else if (u < 0.45f) c = SlashGlow;
            else c = SlashCore;
            if (tipT > 0.85f) c = tipT > 0.95f ? SlashTip : SlashEdge;
            if (fade)
            {
                if (((x + y) & 1) == 0) continue; // checker dither while it dissolves
                c = u < 0.3f ? SlashRim : SlashEdge;
            }
            px[y * size + x] = c;
        }

        if (inner && (frame == 1 || frame == 2))
        {
            // Finisher: thin secondary arc inside the main crescent.
            float r1 = ro - thick - 9f, r2 = r1 + 3f;
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float px0 = x + 0.5f - cx, py0 = y + 0.5f - cy;
                float r = Mathf.Sqrt(px0 * px0 + py0 * py0);
                float ang = Mathf.Atan2(py0, px0) * Mathf.Rad2Deg;
                if (r >= r1 && r <= r2 && ang > lo + 8f && ang < hi - 8f && px[y * size + x].a == 0)
                    px[y * size + x] = SlashEdge;
            }
        }
        return px;
    }

    // ---------------------------------------------------------------- IO helpers

    static void WriteNewTile(string name, Color32[] px, bool transparent = false)
    {
        string path = $"{TileDir}/{name}.png";
        WriteNewSprite(path, px, new Vector2(0.5f, 0.5f), 16, 16);
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        string tilePath = $"{TileDir}/{name}Tile.asset";
        var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, tilePath);
        }
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.None;
        EditorUtility.SetDirty(tile);
    }

    static void WriteNewSprite(string path, Color32[] px, Vector2 pivot, int w = -1, int h = -1)
    {
        if (w < 0) { w = (int)Mathf.Sqrt(px.Length); h = w; }
        // Shadows are not square: derive height from known widths.
        if (w * h != px.Length) { h = px.Length / w; }
        SavePng(path, px, w, h);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        ConfigurePixel(imp);
        imp.spriteImportMode = SpriteImportMode.Single;
        var s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteAlignment = (int)SpriteAlignment.Custom;
        s.spritePivot = pivot;
        s.spriteMeshType = SpriteMeshType.FullRect;
        imp.SetTextureSettings(s);
        imp.SaveAndReimport();
    }

    static void WriteNewSprite(string path, Color32[] px, Vector2 pivot)
    {
        // Shadow helper: width is encoded by the caller's canvas.
        int w = path.EndsWith("_A.png") ? 56 : 48;
        WriteNewSprite(path, px, pivot, w, px.Length / w);
    }

    static void WriteSheet(string name, int cell, int frames, Func<int, Color32[]> frameFn, Vector2 pivot)
        => WriteSheetTo(VfxDir, name, cell, frames, frameFn, pivot);

    static void WriteSheetTo(string dir, string name, int cell, int frames, Func<int, Color32[]> frameFn, Vector2 pivot)
    {
        int w = cell * frames, h = cell;
        var sheet = Fill(w, h, Clear);
        for (int f = 0; f < frames; f++)
        {
            var fr = frameFn(f);
            for (int y = 0; y < cell; y++)
            for (int x = 0; x < cell; x++)
                sheet[y * w + f * cell + x] = fr[y * cell + x];
        }
        string path = $"{dir}/{name}.png";
        SavePng(path, sheet, w, h);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        ConfigurePixel(imp);
        imp.spriteImportMode = SpriteImportMode.Multiple;
        var s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect;
        imp.SetTextureSettings(s);
        imp.SaveAndReimport();

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(imp);
        dp.InitSpriteEditorDataProvider();
        var rects = new SpriteRect[frames];
        for (int f = 0; f < frames; f++)
        {
            rects[f] = new SpriteRect
            {
                name = $"{name}_{f}",
                rect = new Rect(f * cell, 0, cell, cell),
                alignment = SpriteAlignment.Custom,
                pivot = pivot,
                spriteID = GUID.Generate()
            };
        }
        dp.SetSpriteRects(rects);
        var names = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (names != null)
        {
            var pairs = new List<SpriteNameFileIdPair>();
            foreach (var r in rects) pairs.Add(new SpriteNameFileIdPair(r.name, r.spriteID));
            names.SetNameFileIdPairs(pairs);
        }
        dp.Apply();
        imp.SaveAndReimport();
    }

    static void ConfigurePixel(TextureImporter imp)
    {
        imp.textureType = TextureImporterType.Sprite;
        imp.spritePixelsPerUnit = 16;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.mipmapEnabled = false;
        imp.alphaIsTransparency = true;
        imp.wrapMode = TextureWrapMode.Clamp;
    }

    static void SavePng(string path, Color32[] px, int w, int h)
    {
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(tex);
    }

    static Color32[] Fill(int w, int h, Color32 c)
    {
        var px = new Color32[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = c;
        return px;
    }

    static void Put(Color32[] px, int w, int x, int y, Color32 c)
    {
        int h = px.Length / w;
        if (x < 0 || y < 0 || x >= w || y >= h) return;
        px[y * w + x] = c;
    }

    static float Hash(int x, int y, int seed)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + seed * 2147483647);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / (float)0x1000000;
        }
    }

    static bool Same(Color32 a, Color32 b) => a.r == b.r && a.g == b.g && a.b == b.b && a.a == b.a;

    static Color32 Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }
}
