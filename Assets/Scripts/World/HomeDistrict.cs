using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Art dressing for the Housing zone ("Nha" / Village Expansion), approved candidate 3
    /// (Docs/WorldArt/HomeDistrict). Visual only: the four build slots, the return gate and Housing logic
    /// are untouched. The big house shows only once Housing.HasHouse is true; before that a foundation.
    /// </summary>
    public static class HomeDistrict
    {
        public static bool Enabled = true;
        const string Dir = "World/HomeDistrict/";
        const string Village = "World/OriginalVillage/";
        const int GroundOrder = -9000;
        const int Size = 18, Px = Size * 16;                          // ground rows local y -8..10 (zone top raised to 10 so the roof fits 9:16)
        const int WideU = 34, PxW = WideU * 16;                       // ground baked wider so 16:9 has no empty sides

        public static void Apply(Transform zone)
        {
            if (!Enabled || zone == null || zone.Find("HomeDistrict") != null) return;
            var root = new GameObject("HomeDistrict").transform;
            root.SetParent(zone, false);

            // graybox floor off, baked ground on
            var floor = zone.Find("Ground");
            if (floor != null && floor.TryGetComponent<SpriteRenderer>(out var fsr)) fsr.enabled = false;
            BakeGround(root);

            // build slots stay as housing anchors, with no lamp sprite of their own
            foreach (Transform t in zone)
            {
                if (!t.name.StartsWith("slot_")) continue;
                if (t.TryGetComponent<SpriteRenderer>(out var sr)) sr.enabled = false;
                var label = t.Find("Label");
                if (label != null) label.gameObject.SetActive(false);
                if (t.TryGetComponent<Collider2D>(out var col)) col.enabled = false;
            }
            // return gate keeps its trigger; two low lantern posts frame it
            var back = zone.Find("Back");
            if (back != null && back.TryGetComponent<SpriteRenderer>(out var bsr)) bsr.enabled = false;
            var backLabel = back != null ? back.Find("Label") : null;           // the "VE LANG" sign replaces the small label
            if (backLabel != null && H("HD_GateSign") != null) backLabel.gameObject.SetActive(false);

            // placeable pieces from the saved layout (the approved layout when nothing is saved)
            var art = Resources.Load<OriginalVillageArt>(Village + "OriginalVillageArt");
            var pieces = HomeLayout.Pieces;
            for (int i = 0; i < pieces.Count; i++)
            {
                int first = root.childCount;
                Build(root, pieces[i], art);
                for (int c = first; c < root.childCount; c++) root.GetChild(c).gameObject.AddComponent<HomePieceRef>().index = i;
            }

            // house or foundation (centre, behind the slot row)
            var house = Up(root, H("HD_BigHouse"), 0f, 3.1f);
            if (house != null)
            {
                house.AddComponent<BuildingFade>();
                Solid(house, 6.8f, 0.8f, 0.4f);
                var lit = new GameObject("NightWindows");
                lit.transform.SetParent(house.transform, false);
                var lsr = lit.AddComponent<SpriteRenderer>();
                lsr.sprite = DayNight.HouseWindows();
                lsr.sortingOrder = DayNight.GlowOrder;
                lit.AddComponent<NightGlow>().Init(false, true);
                DayNight.Glow(house.transform, new Vector3(-1.9f, 1.0f, 0f), "DN_Glow_Window", false, true);
                DayNight.Glow(house.transform, new Vector3(1.9f, 1.0f, 0f), "DN_Glow_Window", false, true);
            }
            var found = Up(root, H("HD_Foundation"), 0f, 3.0f);
            var scaffold = Up(root, Resources.Load<Sprite>(Dir + "Build/BD_Scaffold"), 0f, 3.1f);
            if (scaffold != null) scaffold.SetActive(false);
            root.gameObject.AddComponent<HomeDistrictView>().Bind(house, found, scaffold);

            Up(root, H("HD_GatePost"), -1.1f, -6.4f); Up(root, H("HD_GatePost"), 1.1f, -6.4f); Up(root, H("HD_GateSign"), 0f, -6.4f);
            DayNight.Glow(root, new Vector3(-1.1f, -4.8f, 0f), "DN_Glow_Lamp", true, false);
            DayNight.Glow(root, new Vector3(1.1f, -4.8f, 0f), "DN_Glow_Lamp", true, false);
            BuildWater(root);
            HomeAnimals.Spawn(root);
            if (HomeLayout.Has("garden") || HomeLand.Count(HomeLand.Plot) > 0) HomeFarm.Spawn(root);
            if (HomeLayout.Has("pond") || HomeLand.HasWater)
            {
                root.gameObject.AddComponent<HomeLife>();
                root.gameObject.AddComponent<HomePondBlock>();
            }
            HomeClear.Spawn(root);
            HomeArea.Spawn(root);
            HomeFogView.Spawn(root);
        }

        /// <summary>Rebuilds the Home map from the saved layout (after a piece is placed or removed).</summary>
        public static void Rebuild()
        {
            var old = GameObject.Find("HomeDistrict");
            if (old == null) return;
            var zone = old.transform.parent;
            old.name = "HomeDistrict_old";
            old.SetActive(false);
            Object.Destroy(old);
            Apply(zone);
        }

        /// <summary>Card / ghost picture of a placeable piece.</summary>
        public static Sprite PieceSprite(string def)
        {
            switch (def)
            {
                case "pond": return H("HD_PondBig");
                case "garden": return Resources.Load<Sprite>(Dir + "Build/BD_Swatch_Plot");
                case "coop": return H("HD_Coop");
                case "tree_leaf": return V("OV_Tree_Leaf");
                case "tree_teal": return V("OV_Tree_Teal");
                case "tree_c": var art = Resources.Load<OriginalVillageArt>(Village + "OriginalVillageArt"); return art != null ? art.tree : null;
                case "bush_teal": return V("OV_Bush_Teal");
                case "bush_leaf": return V("OV_Bush_Leaf");
                case "rocks": return V("OV_Rocks");
                case "stump": return V("OV_Stump");
                case "hay": return V("OV_Hay");
                case "bench": return V("OV_Bench");
                case "planter_a": return V("OV_Planter_A");
                case "planter_b": return V("OV_Planter_B");
                case "flower_row_0": return H("HD_FlowerRow_0");
                case "flower_row_1": return H("HD_FlowerRow_1");
                case "flower_row_2": return H("HD_FlowerRow_2");
                case "scarecrow": return H("HD_Scarecrow");
                case "fence": return V("OV_Fence");
                case "low_fence": return H("HD_LowFence");
                case "sapling_leaf": return G("SP_Leaf_0");
                case "sapling_teal": return G("SP_Teal_0");
                case "slab": return G("DC_Slab");
                case "garden_gate": return G("DC_GardenGate");
                case "swing": return G("DC_Swing");
                case "hang_lantern": return G("DC_HangLantern");
                case "white_fence": return G("DC_WhiteFence");
                case "potted_plant": return G("DC_PottedPlant");
                case "stone_bridge": return G("DC_StoneBridge");
                case "statue": return G("DC_Statue");
                case "flower_shelf": return G("DC_FlowerShelf");
                case "flower_carpet": return G("DC_FlowerCarpet");
                case "lamp_post": return H("HD_SlotPost");
                case "mailbox": return H("HD_Mailbox_Closed") ?? H("HD_Mailbox");
                default: return null;
            }
        }

        static void Build(Transform root, HomePiece p, OriginalVillageArt art)
        {
            float x = p.pos.x, y = p.pos.y;
            GameObject go = null;
            switch (p.def)
            {
                case "pond":
                    Flat(root, "HD_PondBig", x, y, GroundOrder + 40);
                    Flat(root, "HD_Bridge", x + 0.5f, y + 0.1f, GroundOrder + 50);
                    if (!HomeAnimals.Enabled)
                    {
                        Flat(root, "HD_Duck_0", x - 1.3f, y + 1.0f, GroundOrder + 60);
                        Flat(root, "HD_Duck_1", x + 1.7f, y - 0.9f, GroundOrder + 60);
                        Flat(root, "HD_Duck_0", x + 2.4f, y - 0.6f, GroundOrder + 60);
                    }
                    var edge = new GameObject("PondEdge");
                    edge.transform.SetParent(root, false);
                    edge.transform.localPosition = p.pos;
                    var cap = edge.AddComponent<CapsuleCollider2D>();
                    cap.direction = CapsuleDirection2D.Horizontal;
                    cap.size = new Vector2(5.4f, 2.8f);
                    return;
                case "garden":
                    if (!HomeFarm.Enabled) Flat(root, "HD_CropsBig", x, y, GroundOrder + 40);
                    return;
                case "coop":
                    go = Up(root, H("HD_Coop"), x, y);
                    if (go != null) Solid(go, 2.0f, 0.5f, 0.25f);
                    if (!HomeAnimals.Enabled) { Up(root, H("HD_Chicken_0"), x - 1.2f, y - 0.6f); Up(root, H("HD_Chicken_1"), x + 1.1f, y - 1.1f); }
                    break;
                case "tree_leaf": Tree(root, V("OV_Tree_Leaf"), x, y); return;
                case "tree_teal": Tree(root, V("OV_Tree_Teal"), x, y); return;
                case "tree_c": Tree(root, art != null ? art.tree : null, x, y); return;
                case "bush_teal": go = Up(root, V("OV_Bush_Teal"), x, y); break;
                case "bush_leaf": go = Up(root, V("OV_Bush_Leaf"), x, y); break;
                case "rocks": go = Up(root, V("OV_Rocks"), x, y); break;
                case "stump": go = Up(root, V("OV_Stump"), x, y); break;
                case "hay": go = Up(root, V("OV_Hay"), x, y); break;
                case "bench": go = Up(root, V("OV_Bench"), x, y); break;
                case "planter_a": go = Up(root, V("OV_Planter_A"), x, y); break;
                case "planter_b": go = Up(root, V("OV_Planter_B"), x, y); break;
                case "flower_row_0": go = Up(root, H("HD_FlowerRow_0"), x, y); break;
                case "flower_row_1": go = Up(root, H("HD_FlowerRow_1"), x, y); break;
                case "flower_row_2": go = Up(root, H("HD_FlowerRow_2"), x, y); break;
                case "scarecrow": go = Up(root, H("HD_Scarecrow"), x, y); break;
                case "fence":
                    go = Up(root, V("OV_Fence"), x, y);
                    if (go != null) Solid(go, 2.0f, 0.3f, 0.15f);
                    break;
                case "low_fence":
                    go = Up(root, H("HD_LowFence"), x, y);
                    if (go != null) Solid(go, 2.0f, 0.3f, 0.15f);
                    break;
                case "sapling_leaf":
                case "sapling_teal":
                    bool leaf = p.def == "sapling_leaf";
                    if (HomeDecor.SaplingStage(p.t) >= 3) { Tree(root, V(leaf ? "OV_Tree_Leaf" : "OV_Tree_Teal"), x, y); return; }
                    go = new GameObject("Sapling");
                    go.transform.SetParent(root, false);
                    go.transform.localPosition = p.pos;
                    go.AddComponent<SpriteRenderer>();
                    go.AddComponent<SpriteSorter>();
                    go.AddComponent<SaplingView>().Init(leaf ? "SP_Leaf_" : "SP_Teal_", p.t);
                    break;
                case "slab": Flat(root, G("DC_Slab"), x, y, GroundOrder + 10); return;
                case "flower_carpet": Flat(root, G("DC_FlowerCarpet"), x, y, GroundOrder + 10); return;
                case "stone_bridge": Flat(root, G("DC_StoneBridge"), x, y, GroundOrder + 50); return;
                case "potted_plant": go = Up(root, G("DC_PottedPlant"), x, y); break;
                case "garden_gate":
                    go = Up(root, G("DC_GardenGate"), x, y);
                    if (go != null) { SolidAt(go, 0.3f, 0.3f, -0.85f, 0.15f); SolidAt(go, 0.3f, 0.3f, 0.85f, 0.15f); }
                    break;
                case "swing":
                    go = Up(root, G("DC_Swing"), x, y);
                    if (go != null) Solid(go, 2.0f, 0.3f, 0.15f);
                    break;
                case "hang_lantern":
                    go = Up(root, G("DC_HangLantern"), x, y);
                    if (go != null) { Solid(go, 0.3f, 0.3f, 0.15f); DayNight.Glow(go.transform, new Vector3(0f, 1.6f, 0f), "DN_Glow_Lamp", true, false); }
                    break;
                case "lamp_post":
                    go = Up(root, H("HD_SlotPost"), x, y);
                    if (go != null) DayNight.Glow(go.transform, new Vector3(0f, 0.8f, 0f), "DN_Glow_Lamp", true, false);
                    break;
                case "mailbox":
                    go = Up(root, H("HD_Mailbox_Closed") ?? H("HD_Mailbox"), x, y);
                    if (go != null) { go.name = "Mailbox"; go.AddComponent<MailboxView>(); }
                    break;
                case "white_fence":
                    go = Up(root, G("DC_WhiteFence"), x, y);
                    if (go != null) Solid(go, 2.0f, 0.3f, 0.15f);
                    break;
                case "statue":
                    go = Up(root, G("DC_Statue"), x, y);
                    if (go != null) Solid(go, 0.6f, 0.3f, 0.15f);
                    break;
                case "flower_shelf":
                    go = Up(root, G("DC_FlowerShelf"), x, y);
                    if (go != null) Solid(go, 2.0f, 0.3f, 0.15f);
                    break;
                default:
                    Debug.LogWarning("HomeDistrict: unknown piece '" + p.def + "'.");
                    return;
            }
            if (go != null)
            {
                var sr = go.GetComponent<SpriteRenderer>();
                if (sr != null) sr.flipX = p.flip;
                go.transform.localEulerAngles = new Vector3(0f, 0f, -(p.turn & 3) * 90f);
            }
        }

        // ---------- ground ----------
        // Fixed yard: a dirt apron the full width of the foundation, then stepping stones on the grass
        // down to the gate. The grass brush can paint those cells back to grass.
        static int Kind(float lx, float ly)
        {
            float[] sx = { 0.12f, -0.16f, 0.1f, -0.14f, 0.12f, -0.06f };
            float[] sy = { 0.05f, -1.2f, -2.45f, -3.7f, -4.95f, -6.2f };
            for (int i = 0; i < 6; i++)
            {
                float dx = (lx - sx[i]) / 0.58f, dy = (ly - sy[i]) / 0.4f;
                if (dx * dx + dy * dy < 1f) return 3;
            }
            if (lx >= -3.6f && lx <= 3.6f && ly >= 0.45f && ly <= 3.15f) return 1;
            return 0;
        }

        /// <summary>Ground class before player painting. 0 grass, 1 the fixed yard, 3 a slab.</summary>
        public static int BaseKind(Vector2 local) => Kind(local.x, local.y);

        static Color32 Shade(Color32 c, int pct) => new Color32((byte)(c.r * pct / 100), (byte)(c.g * pct / 100), (byte)(c.b * pct / 100), 255);

        // disc-shaped erosion / dilation of a PxW x Px mask (pixels outside count as "same as here")
        static bool[] Morph(bool[] m, int r, bool dilate)
        {
            var o = new bool[m.Length];
            for (int y = 0; y < Px; y++) for (int x = 0; x < PxW; x++)
            {
                int i = y * PxW + x;
                bool hit = !dilate;
                for (int dy = -r; dy <= r; dy++)
                {
                    int yy = y + dy; if (yy < 0 || yy >= Px) continue;
                    for (int dx = -r; dx <= r; dx++)
                    {
                        if (dx * dx + dy * dy > r * r) continue;
                        int xx = x + dx; if (xx < 0 || xx >= PxW) continue;
                        bool v = m[yy * PxW + xx];
                        if (dilate && v) { hit = true; goto done; }
                        if (!dilate && !v) { hit = false; goto done; }
                    }
                }
            done:
                o[i] = hit;
            }
            return o;
        }

        static readonly int[] Bayer = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };

        // smooth value noise in 0..1, lattice every `cell` px
        static float Noise(int x, int y, int cell, int seed)
        {
            int gx = Mathf.FloorToInt((float)x / cell), gy = Mathf.FloorToInt((float)y / cell);
            float fx = (x - gx * cell) / (float)cell, fy = (y - gy * cell) / (float)cell;
            fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
            int sx = gx + seed * 101;
            float a = Hash(sx, gy) % 1000 / 1000f, b = Hash(sx + 1, gy) % 1000 / 1000f;
            float c = Hash(sx, gy + 1) % 1000 / 1000f, d = Hash(sx + 1, gy + 1) % 1000 / 1000f;
            return Mathf.Lerp(Mathf.Lerp(a, b, fx), Mathf.Lerp(c, d, fx), fy);
        }

        // small pixel stamps (rows top first, '.' = keep): grass tufts, a flower, pebbles, a leaf, a sand crack
        static readonly string[][] Tufts = {
            new[] { "l...l", ".d.d.", "dmdmd" },
            new[] { ".l...", ".d.l.", "dmdd." },
            new[] { "..l..", "l.d.l", "d.d.d", "dmdmd" },
            new[] { "l..", "d.l", "dmd" },
        };
        static readonly string[] StFlower = { ".p.", "pyp", ".p.", ".d." };
        static readonly string[] StPebbleG = { ".hh", "hgg", ".ss" };
        static readonly string[] StPebbleS = { ".hh.", "hGGG", ".ss." };
        static readonly string[] StPebbleS2 = { "hG", "ss" };
        static readonly string[] StLeaf = { "oL.", ".oo" };
        static readonly string[] StCrack = { "c.c", ".c." };
        static readonly Color32[] Petals = { new Color32(0xF4, 0xF0, 0xE0, 255), new Color32(0xE8, 0xA0, 0xB8, 255), new Color32(0xB8, 0xA0, 0xE0, 255), new Color32(0xF0, 0xD0, 0x60, 255) };

        static void Stamp(Color32[] px, byte[] free, string[] rows, int ox, int oy, byte need, Color32 petal)
        {
            int hgt = rows.Length;
            for (int r = 0; r < hgt; r++) for (int c = 0; c < rows[r].Length; c++)
            {
                char ch = rows[r][c]; if (ch == '.') continue;
                int x = ox + c, y = oy + (hgt - 1 - r);
                if (x < 0 || y < 0 || x >= PxW || y >= Px) continue;
                int i = y * PxW + x; if (free[i] != need) continue;
                Color32 col;
                switch (ch)
                {
                    case 'd': col = new Color32(0x5A, 0x90, 0x70, 255); break;
                    case 'm': col = new Color32(0x64, 0xA0, 0x78, 255); break;
                    case 'l': col = new Color32(0x86, 0xBE, 0x96, 255); break;
                    case 'p': col = petal; break;
                    case 'y': col = new Color32(0xF0, 0xC0, 0x48, 255); break;
                    case 'h': col = need == 1 ? new Color32(0xC8, 0xC4, 0xB8, 255) : new Color32(0xC8, 0xB4, 0x98, 255); break;
                    case 'g': col = new Color32(0x9C, 0x98, 0x8C, 255); break;
                    case 'G': col = new Color32(0xA8, 0x8C, 0x70, 255); break;
                    case 's': col = Shade(px[i], 80); break;
                    case 'o': col = new Color32(0xC0, 0x80, 0x40, 255); break;
                    case 'L': col = new Color32(0xE0, 0xAC, 0x60, 255); break;
                    case 'c': col = new Color32(0xB0, 0x87, 0x66, 255); break;
                    default: continue;
                }
                px[i] = col;
            }
        }

        // one candidate per 11 px block at a hashed offset, so details never line up with the 16 px cells
        static void ScatterDetail(Color32[] px, byte[] free)
        {
            const int B = 11;
            for (int by = 0; by < Px; by += B) for (int bx = 0; bx < PxW; bx += B)
            {
                int h = Hash(bx * 3 + 11, by * 5 + 7);
                int ox = bx + h % (B - 3), oy = by + (h >> 6) % (B - 3), roll = (h >> 12) % 100;
                int ci = Mathf.Clamp(oy + 1, 0, Px - 1) * PxW + Mathf.Clamp(ox + 1, 0, PxW - 1);
                if (free[ci] == 1)
                {
                    if (roll < 24) Stamp(px, free, Tufts[(h >> 20) % 4], ox, oy, 1, default);
                    else if (roll < 28) Stamp(px, free, StFlower, ox, oy, 1, Petals[(h >> 20) % 4]);
                    else if (roll < 30) Stamp(px, free, StPebbleG, ox, oy, 1, default);
                    else if (roll < 32) Stamp(px, free, StLeaf, ox, oy, 1, default);
                }
                else if (free[ci] == 2)
                {
                    if (roll < 8) Stamp(px, free, StPebbleS, ox, oy, 2, default);
                    else if (roll < 16) Stamp(px, free, StPebbleS2, ox, oy, 2, default);
                    else if (roll < 21) Stamp(px, free, StCrack, ox, oy, 2, default);
                    else if (roll < 24) Stamp(px, free, StLeaf, ox, oy, 2, default);
                }
            }
        }

        static void BakeGround(Transform root)
        {
            var grass = Pixels(V("OV_Grass"), out int gw, out int gh);
            var dirt = Pixels(V("OV_Dirt"), out int dw, out int dh);
            if (grass == null || dirt == null) { Debug.LogWarning("HomeDistrict: ground art missing or not readable."); return; }
            var soilTex = Resources.Load<Texture2D>(Dir + "Farm/FM_Soil");
            var soil = soilTex != null && soilTex.isReadable ? soilTex.GetPixels32() : null;
            int sw = soilTex != null ? soilTex.width : 0;
            // painted cells (HomeLand) override the procedural classes: path -> 1, plot -> 4 tilled soil, grass -> 0
            var paint = new int[WideU * Size];
            for (int cy = 0; cy < Size; cy++) for (int cx = 0; cx < WideU; cx++)
            {
                int k = HomeLand.Kind(new Vector2Int(cx - WideU / 2, cy - 8));
                paint[cy * WideU + cx] = k == HomeLand.Path ? 1 : k == HomeLand.Plot ? (soil != null ? 4 : 1) : k == HomeLand.Grass ? 0 : -1;
            }
            var cls = new int[PxW * Px];
            for (int y = 0; y < Px; y++) for (int x = 0; x < PxW; x++)
            {
                int p = paint[(y / 16) * WideU + x / 16];
                int baked = Kind(-WideU / 2f + (x + 0.5f) / 16f, -8f + (y + 0.5f) / 16f);
                cls[y * PxW + x] = p >= 0 ? p : baked;
            }
            var sTop = new Color32(0xB4, 0xAC, 0xA2, 255); var sMid = new Color32(0x9C, 0x94, 0x8A, 255); var sLow = new Color32(0x7A, 0x72, 0x6A, 255);
            // ---- approved field style (Docs/WorldArt/HomeDistrict/field/candidate9): sand with rounded corners, grass
            // edge = thin cool dark band + sparse blade strokes, raised turf (earth face + shadow), tilled plots ----
            // ground v2 (Docs/WorldArt/HomeDistrict/ground/candidate1): the sand edge is warped by low-frequency noise so it
            // never follows the 16 px cells, then both concave and convex corners are rounded
            int N = PxW * Px;
            var cellSand = new bool[N];
            for (int i = 0; i < N; i++) cellSand[i] = cls[i] == 1 || cls[i] == 4;
            var sandM = new bool[N];
            for (int y = 0; y < Px; y++) for (int x = 0; x < PxW; x++)
            {
                int wx = x + Mathf.RoundToInt((Noise(x, y, 20, 1) - 0.5f) * 6f + (Noise(x, y, 6, 3) - 0.5f) * 2f);
                int wy = y + Mathf.RoundToInt((Noise(x, y, 20, 2) - 0.5f) * 6f + (Noise(x, y, 6, 4) - 0.5f) * 2f);
                sandM[y * PxW + x] = cellSand[Mathf.Clamp(wy, 0, Px - 1) * PxW + Mathf.Clamp(wx, 0, PxW - 1)];
            }
            sandM = Morph(Morph(sandM, 4, true), 4, false);                       // closing: round the concave corners
            sandM = Morph(Morph(sandM, 4, false), 4, true);                       // opening: round the convex corners
            var plotM = new bool[N];
            for (int i = 0; i < N; i++) plotM[i] = cls[i] == 4;
            plotM = Morph(Morph(plotM, 3, false), 3, true);                       // opening: round the plot corners
            var plotPad = Morph(plotM, 2, true);
            for (int i = 0; i < N; i++) if (plotPad[i]) sandM[i] = true;          // a plot always sits on sand
            System.Func<int, int, bool> S = (x, y) => x >= 0 && y >= 0 && x < PxW && y < Px && sandM[y * PxW + x];
            System.Func<int, int, bool> Pm = (x, y) => x >= 0 && y >= 0 && x < PxW && y < Px && plotM[y * PxW + x];
            var egDark = new Color32(0x50, 0x78, 0x68, 255); var egMid = new Color32(0x60, 0x88, 0x78, 255); var egSoft = new Color32(0x68, 0xA0, 0x80, 255);
            var face1 = new Color32(0x9A, 0x6E, 0x48, 255); var face2 = new Color32(0x7E, 0x5A, 0x3A, 255); var lit = new Color32(0x86, 0xBE, 0x96, 255);
            var sdSpeck = new Color32(0xC0, 0x8E, 0x68, 255);
            var tpOut = new Color32(0x5A, 0x30, 0x16, 255); var tpRim = new Color32(0x7A, 0x46, 0x1E, 255); var tpFur = new Color32(0x64, 0x36, 0x12, 255);
            var tpHi = new Color32(0xA8, 0x6E, 0x3A, 255); var tpMid = new Color32(0x96, 0x60, 0x30, 255); var tpLo = new Color32(0x80, 0x50, 0x2A, 255);
            var outPx = new Color32[N];
            var free = new byte[N];                                               // 1 open grass, 2 open sand: room for a detail stamp
            // texture rows go up: "above" on screen = y + 1
            for (int y = 0; y < Px; y++) for (int x = 0; x < PxW; x++)
            {
                int i = y * PxW + x, c = cls[i];
                if (c == 3)
                {
                    bool top = y + 2 >= Px || cls[i + 2 * PxW] != 3;
                    bool low = y == 0 || cls[i - PxW] != 3;
                    outPx[i] = top ? sTop : (low ? sLow : sMid);
                    continue;
                }
                if (S(x, y))
                {
                    if (Pm(x, y))
                    {
                        bool e1 = !Pm(x - 1, y) || !Pm(x + 1, y) || !Pm(x, y - 1) || !Pm(x, y + 1);
                        bool e2 = !e1 && (!Pm(x - 2, y) || !Pm(x + 2, y) || !Pm(x, y - 2) || !Pm(x, y + 2));
                        if (e1) { outPx[i] = tpOut; continue; }
                        if (e2) { outPx[i] = tpRim; continue; }
                        int p = ((Px - 1 - y) % 12 + 12) % 12, hs = Hash(x, y) % 23;      // mounds every 12 px, counted from the top
                        outPx[i] = p == 0 ? tpHi : (p < 8 ? (hs == 0 ? tpHi : (hs < 4 ? tpLo : tpMid)) : (p == 8 ? tpLo : (hs < 3 ? tpLo : tpFur)));
                        continue;
                    }
                    var sc = dirt[(y % dh) * dw + (x % dw)];
                    if (Hash(x, y) % 61 == 0) sc = sdSpeck;
                    float sn = Noise(x, y, 28, 9) + (Bayer[(y & 3) * 4 + (x & 3)] / 16f - 0.5f) * 0.08f;   // worn / damp spots
                    if (sn < 0.28f) sc = Shade(sc, 95); else if (sn > 0.74f) sc = Shade(sc, 103);
                    // raised turf: grass above (y + 1..) -> earth face 2 px, shadow 2 px; grass on the left -> 1 px shadow
                    if (!S(x, y + 1)) sc = face1;
                    else if (!S(x, y + 2)) sc = face2;
                    else if (!S(x, y + 3)) sc = Shade(sc, 80);
                    else if (!S(x, y + 4)) sc = Shade(sc, 90);
                    else if (!S(x - 1, y)) sc = Shade(sc, 84);
                    else if (!plotPad[i] && S(x - 3, y) && S(x + 3, y) && S(x, y - 3) && S(x, y + 6)) free[i] = 2;
                    outPx[i] = sc;
                    continue;
                }
                // grass: distance to sand along the column and the row (up to 9 px)
                int dv = 99, dh2 = 99;
                for (int k = 1; k <= 9; k++) if (S(x, y + k) || S(x, y - k)) { dv = k; break; }
                for (int k = 1; k <= 9; k++) if (S(x + k, y) || S(x - k, y)) { dh2 = k; break; }
                int d = dv < dh2 ? dv : dh2;
                var gc = grass[(y % gh) * gw + (x % gw)];
                // big soft colour patches break the texture repeat: cool hollows and sunlit tufts, dithered borders
                float n = Noise(x, y, 44, 5) * 0.7f + Noise(x, y, 14, 6) * 0.3f + (Bayer[(y & 3) * 4 + (x & 3)] / 16f - 0.5f) * 0.07f;
                if (n < 0.33f) gc = new Color32((byte)(gc.r * 92 / 100), (byte)(gc.g * 95 / 100), (byte)(gc.b * 94 / 100), 255);
                else if (n > 0.67f) gc = new Color32((byte)Mathf.Min(255, gc.r * 108 / 100), (byte)Mathf.Min(255, gc.g * 105 / 100), (byte)Mathf.Min(255, gc.b * 104 / 100), 255);
                if (d <= 9)
                {
                    int line = dv <= dh2 ? x : y;
                    int hsl = Hash(line, dv <= dh2 ? 1 : 2);
                    bool stroke = hsl % 3 == 0 || (hsl % 7 == 1 && Hash(line + 1, 4) % 2 == 0);
                    int len = 3 + hsl % 4 + (hsl % 11 == 0 ? 2 : 0);
                    if (d == 1) gc = egDark;
                    else if (d == 2) gc = Hash(x, y) % 4 == 0 ? egSoft : egMid;
                    else if (stroke && d <= len) gc = d <= len - 1 ? egMid : egSoft;
                    else if (d == 3 && Hash(x + 7, y) % 3 == 0) gc = egSoft;
                }
                if (S(x, y + 1) && !S(x, y - 1)) gc = lit;                       // grass top facing the sun (sand above it)
                if (d > 6) free[i] = 1;
                outPx[i] = gc;
            }
            ScatterDetail(outPx, free);
            var tex = new Texture2D(PxW, Px, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "HomeGroundBaked" };
            tex.SetPixels32(outPx);
            tex.Apply(false, true);
            var go = new GameObject("HomeGroundBaked");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(-WideU / 2f, -8f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, PxW, Px), Vector2.zero, 16f, 0, SpriteMeshType.FullRect);
            sr.sortingOrder = GroundOrder;

            // flowers and tall grass on cells that are fully grass
            var flowers = new[] { V("OV_Flower_0"), V("OV_Flower_1"), V("OV_Flower_2") };
            var tall = new[] { V("OV_TallGrass_0"), V("OV_TallGrass_1") };
            for (int ty = 0; ty < Size; ty++) for (int tx = 0; tx < WideU; tx++)
            {
                int x0 = tx * 16, y0 = ty * 16;
                if (cls[(y0 + 8) * PxW + x0 + 8] != 0 || cls[y0 * PxW + x0] != 0 || cls[(y0 + 15) * PxW + x0 + 15] != 0) continue;
                int h = Hash(tx, 15 - ty);
                // fewer flowers, mostly purple/yellow (white only 1 in 5), a little more tall grass
                int pick = (h >> 4) % 5;
                Sprite s = h % 9 == 0 ? flowers[pick < 2 ? 0 : (pick < 4 ? 1 : 2)] : (h % 6 == 0 ? tall[(h >> 5) & 1] : null);
                if (s == null) continue;
                // ground v2: off the cell centre (no grid), a third of the tall grass dropped (baked tufts take over)
                int hj = Hash(tx * 7 + 3, ty * 13 + 5);
                if (h % 9 != 0 && hj % 3 == 0) continue;
                float jx = ((hj % 9) - 4) * 0.075f, jy = (((hj >> 5) % 7) - 3) * 0.07f;
                int px = Mathf.FloorToInt((tx + 0.5f + jx) * 16), py = Mathf.FloorToInt((ty + 0.5f + jy) * 16);
                if (px < 0 || py < 0 || px >= PxW || py >= Px || free[py * PxW + px] != 1) jx = jy = 0f;
                Flat(root, s, tx - WideU / 2 + 0.5f + jx, ty - 8 + 0.5f + jy, GroundOrder + 10);
            }
        }

        static bool Soft(int c) => c == 1 || c == 4;

        /// <summary>Dug pond: baked art from HomeLand (HomePondBake) plus one PondEdge body with a box per blocked cell.</summary>
        static void BuildWater(Transform root)
        {
            if (!HomeLand.HasWater) return;
            var host = new GameObject("DugPond").transform;
            host.SetParent(root, false);
            var edge = new GameObject("PondEdge");
            edge.transform.SetParent(host, false);
            var bridges = new System.Collections.Generic.List<Rect>();
            foreach (var p in HomeLayout.Pieces)
                if (p.def == "stone_bridge") bridges.Add(HomeDecor.Foot(p.def, p.pos));
            // art v2 (approved 2026-10-30): one baked pond sprite with glint frames instead of 16 corner tiles
            HomePondBake.Build(host, GroundOrder + 40);
            foreach (var c in HomeLand.TileCells())
            {
                if (!HomeLand.Blocks(c)) continue;
                var cell = new Rect(c.x, c.y, 1f, 1f);
                bool bridged = false;
                foreach (var b in bridges) if (b.Overlaps(cell)) { bridged = true; break; }
                if (bridged) continue;
                var box = edge.AddComponent<BoxCollider2D>();
                box.size = Vector2.one;
                box.offset = new Vector2(c.x + 0.5f, c.y + 0.5f);
            }
        }

        static Color32[] Pixels(Sprite s, out int w, out int h)
        {
            w = h = 0;
            if (s == null || !s.texture.isReadable) return null;
            w = s.texture.width; h = s.texture.height;
            return s.texture.GetPixels32();
        }

        // ---------- helpers ----------
        static Sprite H(string name) => Resources.Load<Sprite>(Dir + name);
        static Sprite V(string name) => Resources.Load<Sprite>(Village + name);
        static Sprite G(string name) => Resources.Load<Sprite>(Dir + "Garden/" + name);

        static GameObject Up(Transform root, Sprite sprite, float x, float y)
        {
            if (sprite == null) return null;
            var go = new GameObject(sprite.name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector2(x, y);
            go.AddComponent<SpriteRenderer>().sprite = sprite;
            go.AddComponent<SpriteSorter>();
            return go;
        }

        static void Flat(Transform root, string name, float x, float y, int order) => Flat(root, H(name), x, y, order);

        static void Flat(Transform root, Sprite sprite, float x, float y, int order)
        {
            if (sprite == null) return;
            var go = new GameObject(sprite.name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector2(x, y);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
        }

        static void Tree(Transform root, Sprite tree, float x, float y)
        {
            if (tree == null) return;
            if (!DayNight.AttachTreeShadow(root, new Vector2(x, y), tree, GroundOrder + 10))
            {
                var shadow = V("OV_Shadow_TreeV3");
                if (shadow != null) Flat(root, shadow, x, y + 0.15f, GroundOrder + 200);
            }
            var go = new GameObject("HomeTree");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector2(x, y);
            go.AddComponent<SpriteRenderer>().sprite = tree;
            go.AddComponent<SpriteSorter>();
            go.AddComponent<TreeCanopyOcclusion>();
            go.AddComponent<BoxCollider2D>();
            go.AddComponent<TreeForestCFoot>();
        }

        static void Solid(GameObject go, float w, float h, float oy)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(w, h);
            box.offset = new Vector2(0f, oy);
        }

        static void SolidAt(GameObject go, float w, float h, float ox, float oy)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(w, h);
            box.offset = new Vector2(ox, oy);
        }

        static int Hash(int x, int y) { unchecked { int h = x * 73856093 ^ y * 19349663 ^ 0x5bd1e995; return (h ^ (h >> 13)) & 0x7fffffff; } }
    }

    /// <summary>Sapling stages 0..2 (one per in-game day); at stage 3 the map rebuilds it as a full tree.</summary>
    public class SaplingView : MonoBehaviour
    {
        string _prefix;
        double _planted;
        int _stage = -1;
        float _next;
        SpriteRenderer _sr;

        public void Init(string prefix, double planted)
        {
            _prefix = prefix;
            _planted = planted;
            _sr = GetComponent<SpriteRenderer>();
            Refresh();
        }

        void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 2f;
            Refresh();
        }

        void Refresh()
        {
            int stage = HomeDecor.SaplingStage(_planted);
            if (stage == _stage) return;
            _stage = stage;
            if (stage >= 3) { HomeDistrict.Rebuild(); return; }
            _sr.sprite = Resources.Load<Sprite>("World/HomeDistrict/Garden/" + _prefix + stage);
            if (stage == 2 && GetComponent<BoxCollider2D>() == null)
            {
                var box = gameObject.AddComponent<BoxCollider2D>();
                box.size = new Vector2(0.5f, 0.3f);
                box.offset = new Vector2(0f, 0.15f);
            }
        }
    }

    /// <summary>Shows the big house once one is built, the foundation before that.</summary>
    public class HomeDistrictView : MonoBehaviour
    {
        GameObject _house, _foundation, _scaffold;
        HomeHouseBuildFx _fx;
        float _next;

        public void Bind(GameObject house, GameObject foundation, GameObject scaffold)
        {
            _house = house; _foundation = foundation; _scaffold = scaffold;
            if (_scaffold != null) _scaffold.SetActive(false);
            _fx = HomeHouseBuildFx.Spawn(transform, new Vector3(0f, 3.1f, 0f));   // timber frame + tools while the house goes up
            Refresh();
        }

        void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.5f;
            Refresh();
        }

        void Refresh()
        {
            if (_house != null && _house.GetComponent<BuildingFade>() == null) _house.AddComponent<BuildingFade>();
            bool building = BuildMode.HouseBuildProgress(out float progress);
            bool showHouse = Housing.HasHouse && !building;
            bool showScaffold = building && progress >= 0.3f && _fx == null;
            bool showFound = !showHouse && !showScaffold && !(building && _fx != null);
            if (_house != null && _house.activeSelf != showHouse) _house.SetActive(showHouse);
            if (_scaffold != null && _scaffold.activeSelf != showScaffold) _scaffold.SetActive(showScaffold);
            if (_foundation != null && _foundation.activeSelf != showFound) _foundation.SetActive(showFound);
        }
    }

    /// <summary>Keeps the hero and eggs off water. Ducks may still swim.</summary>
    public class HomePondBlock : MonoBehaviour
    {
        Transform _hero;
        Rigidbody2D _rb;
        float _search;

        void Start()
        {
            SealWater();
            var eggs = FindObjectsByType<FarmEgg>(FindObjectsSortMode.None);
            for (int i = 0; i < eggs.Length; i++)
            {
                if (eggs[i] == null || eggs[i].transform.parent == null) continue;
                if (eggs[i].transform.parent.parent != transform) continue;
                eggs[i].transform.localPosition = HomeAnimals.SnapToLand(eggs[i].transform.localPosition, true);
            }
        }

        void FixedUpdate()
        {
            if (_hero == null)
            {
                _search -= Time.fixedDeltaTime;
                if (_search > 0f) return;
                _search = 0.5f;
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go == null) return;
                _hero = go.transform;
                _rb = go.GetComponent<Rigidbody2D>();
            }
            Vector2 local = transform.InverseTransformPoint(_hero.position);
            if (!HomeAnimals.InPond(local) && !HomeLand.InWater(local)) return;
            if (OnBridge(local)) return;
            Vector2 land = HomeAnimals.SnapToLand(local);
            _hero.position = transform.TransformPoint(land);
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
        }

        void SealWater()
        {
            var dug = transform.Find("DugPond/PondEdge");
            var edge = dug != null ? dug : transform.Find("PondEdge");
            if (edge == null) return;
            var have = new System.Collections.Generic.HashSet<Vector2Int>();
            var boxes = edge.GetComponents<BoxCollider2D>();
            for (int i = 0; i < boxes.Length; i++)
            {
                var o = boxes[i].offset;
                have.Add(new Vector2Int(Mathf.FloorToInt(o.x), Mathf.FloorToInt(o.y)));
            }
            var bridges = new System.Collections.Generic.List<Rect>();
            foreach (var p in HomeLayout.Pieces)
                if (p.def == "stone_bridge") bridges.Add(HomeDecor.Foot(p.def, p.pos));
            foreach (var c in HomeLand.TileCells())
            {
                if (!HomeLand.Blocks(c) || have.Contains(c)) continue;
                var cell = new Rect(c.x, c.y, 1f, 1f);
                bool bridged = false;
                for (int i = 0; i < bridges.Count; i++)
                    if (bridges[i].Overlaps(cell)) { bridged = true; break; }
                if (bridged) continue;
                var box = edge.gameObject.AddComponent<BoxCollider2D>();
                box.size = Vector2.one;
                box.offset = new Vector2(c.x + 0.5f, c.y + 0.5f);
                have.Add(c);
            }
        }

        static bool OnBridge(Vector2 local)
        {
            foreach (var p in HomeLayout.Pieces)
            {
                if (p.def != "stone_bridge") continue;
                if (HomeDecor.Foot(p.def, p.pos).Contains(local)) return true;
            }
            return false;
        }
    }
}
