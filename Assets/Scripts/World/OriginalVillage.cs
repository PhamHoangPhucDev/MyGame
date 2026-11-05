using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// The Village in the locked reference style (Docs/WorldArt/OriginalVillage): cobble streets,
    /// slab forecourts and one frontage per service. Every NPC, gate and board keeps its own
    /// interaction; this only moves and dresses them. Set Enabled = false to get the old hub back.
    /// </summary>
    public static class OriginalVillage
    {
        public static readonly bool Enabled = true;

        // Hub-local bounds (world units = 16 px tiles).
        public const int MinX = -22, MaxX = 22, MinY = -11, MaxY = 26;
        const int GroundOrder = -9000, CurbOrder = -8900, ShadowOrder = -8800;

        struct Frontage
        {
            public string sprite, sign, npc;
            public float x;          // pivot x (bottom centre)
            public int baseY;        // plinth row; the 3-row forecourt sits under it
            public int doorCol;      // cobble path column through the forecourt
            public int minCol, maxCol;
        }

        // Door columns are chosen so each door lines up with a 16 px column.
        static readonly Frontage[] Frontages =
        {
            new Frontage { sprite = "OV_Shop_Apothecary", sign = "OV_Sign_Apothecary", npc = "npc_shop",    x = -16.5f - 31f / 16f + 3.5f, baseY = 3,  doorCol = -17, minCol = -19, maxCol = -11 },
            new Frontage { sprite = "OV_Shop_Smithy",     sign = "OV_Sign_Smithy",     npc = "npc_smith",   x = 13.5f - 82f / 16f + 3.5f,  baseY = 0,  doorCol = 13,  minCol = 8,   maxCol = 16 },
            new Frontage { sprite = "OV_Shop_GuildHall",  sign = "OV_Sign_GuildHall",  npc = "npc_quest",   x = -6.5f,                     baseY = 18, doorCol = -7,  minCol = -11, maxCol = -2 },
            new Frontage { sprite = "OV_Shop_Storehouse", sign = "OV_Sign_Storehouse", npc = "npc_storage", x = 6.5f,                      baseY = 18, doorCol = 6,   minCol = 3,   maxCol = 10 },
        };

        static readonly Vector2[] Trees =
        {
            new Vector2(-20.6f, 3.4f), new Vector2(-8.6f, 4.8f), new Vector2(5.0f, 3.6f), new Vector2(19.6f, 2.2f),
            new Vector2(20.2f, 7.6f), new Vector2(8.0f, -10.2f), new Vector2(14.4f, -9.6f), new Vector2(20.6f, -10.4f),
            new Vector2(-16.0f, 17.0f), new Vector2(-20.0f, 22.0f), new Vector2(-13.6f, 23.6f),
            new Vector2(14.6f, 17.4f), new Vector2(19.6f, 22.2f), new Vector2(12.8f, 23.8f)
        };

        struct Deco { public string sprite; public float x, y; public Deco(string s, float x, float y) { sprite = s; this.x = x; this.y = y; } }

        // feet positions, kept off the walking lines, doors, NPC spots and exits
        static readonly Deco[] Decor =
        {
            new Deco("OV_Well", -8.0f, 10.6f), new Deco("OV_Bench", 6.8f, 10.4f), new Deco("OV_Bench", -20.0f, 9.6f),
            new Deco("OV_LampV3", -4.4f, 3.0f), new Deco("OV_LampV3", 8.0f, 7.6f), new Deco("OV_LampV3", 12.4f, 7.6f), new Deco("OV_LampV3", -11.6f, 14.6f),
            new Deco("OV_Planter_A", -12.6f, 15.4f), new Deco("OV_Planter_B", 12.6f, 16.2f),
            new Deco("OV_Hay", 16.8f, -1.0f), new Deco("OV_Sacks", 18.4f, -0.6f), new Deco("OV_Pumpkins", 11.6f, 18.6f),
            new Deco("OV_Fence", -21.0f, 13.6f), new Deco("OV_Fence", -19.0f, 13.6f), new Deco("OV_Fence", -17.0f, 13.6f), new Deco("OV_Fence", -15.0f, 13.6f),
            new Deco("OV_Fence", 14.0f, 13.6f), new Deco("OV_Fence", 16.0f, 13.6f), new Deco("OV_Fence", 18.0f, 13.6f),
            new Deco("OV_Rocks", -19.4f, 19.4f), new Deco("OV_Rocks", 18.6f, 19.8f), new Deco("OV_Rocks", 16.4f, -10.8f), new Deco("OV_Stump", -14.8f, 21.0f)
        };

        // open grass, away from streets, doors and exits (feet positions)
        static readonly Vector2[] Bushes =
        {
            new Vector2(-6.0f, 2.2f), new Vector2(-21.0f, 6.6f), new Vector2(3.6f, 6.8f), new Vector2(6.4f, -1.4f),
            new Vector2(17.6f, -9.4f), new Vector2(11.2f, -10.6f), new Vector2(-18.4f, 15.2f), new Vector2(-11.8f, 19.8f),
            new Vector2(17.2f, 15.4f), new Vector2(11.4f, 20.6f), new Vector2(21.0f, 4.6f), new Vector2(-21.0f, 25.0f)
        };

        public static ZoneLayout Extend(ZoneLayout hub)
        {
            if (!Enabled) return hub;
            hub.boundsMin = hub.origin + new Vector2(MinX, MinY);
            hub.boundsMax = hub.origin + new Vector2(MaxX, MaxY);
            return hub;
        }

        public static void Apply(Transform hub)
        {
            if (!Enabled || hub == null) return;
            ClearOld(hub);
            var root = new GameObject("OriginalVillage").transform;
            root.SetParent(hub, false);
            var art = Resources.Load<OriginalVillageArt>("World/OriginalVillage/OriginalVillageArt");

            PaintGround(root, art != null ? art.grass : null);

            for (int i = 0; i < Frontages.Length; i++) BuildFrontage(root, hub, Frontages[i]);

            // forecourt wares (feet positions)
            Solid(Prop(root, "Table_Wares", new Vector2(-18.3f, 0.4f), S("OV_Table_Wares")), 2.0f, 0.4f);
            Solid(Prop(root, "Crate_Herbs", new Vector2(-14.1f, 0.5f), S("OV_Crate_Herbs")), 1.1f, 0.4f);
            Solid(Prop(root, "Barrel_A", new Vector2(-11.9f, 0.6f), S("OV_Barrel")), 0.7f, 0.4f);
            Solid(Prop(root, "Anvil", new Vector2(10.6f, -2.1f), S("OV_Anvil")), 1.2f, 0.4f);
            Solid(Prop(root, "Crate_Smith", new Vector2(8.9f, -2.6f), S("OV_Crate")), 0.9f, 0.4f);
            Solid(Prop(root, "Barrel_Smith", new Vector2(15.2f, -2.2f), S("OV_Barrel")), 0.7f, 0.4f);
            Solid(Prop(root, "Barrel_Hall", new Vector2(-10.3f, 15.6f), S("OV_Barrel")), 0.7f, 0.4f);
            Solid(Prop(root, "Crate_Hall", new Vector2(-3.0f, 15.5f), S("OV_Crate")), 0.9f, 0.4f);
            Solid(Prop(root, "Crate_Store_A", new Vector2(3.9f, 15.5f), S("OV_Crate")), 0.9f, 0.4f);
            Prop(root, "Crate_Store_B", new Vector2(4.1f, 16.25f), S("OV_Crate"), 30);
            Solid(Prop(root, "Barrel_Store_A", new Vector2(8.7f, 15.6f), S("OV_Barrel")), 0.7f, 0.4f);
            Solid(Prop(root, "Barrel_Store_B", new Vector2(9.5f, 15.7f), S("OV_Barrel")), 0.7f, 0.4f);
            Solid(Prop(root, "MapBoard", new Vector2(1.7f, 12.0f), S("OV_MapBoard")), 1.3f, 0.3f);

            // trees: the GoblinCamp tree mixed with the two clip-style variants (same 80x72 canvas and foot box)
            var treeShadow = S("OV_Shadow_TreeV3") ?? S("OV_Shadow_Tree");
            var treeKinds = new[] { art != null ? art.tree : null, S("OV_Tree_Leaf"), S("OV_Tree_Teal") };
            for (int i = 0; i < Trees.Length; i++)
            {
                var t = treeKinds[i % 3] ?? treeKinds[0];
                if (t != null) Tree(root, Trees[i], t, treeShadow);
            }
            // bushes: decoration only, no collider
            var bushTeal = S("OV_Bush_Teal"); var bushLeaf = S("OV_Bush_Leaf");
            for (int i = 0; i < Bushes.Length; i++) Prop(root, "Bush", Bushes[i], i % 2 == 0 ? bushTeal : bushLeaf);
            // town dressing (decoration only; the well has a small foot box)
            for (int i = 0; i < Decor.Length; i++)
            {
                var d = Decor[i];
                var go = Prop(root, d.sprite, new Vector2(d.x, d.y), S(d.sprite));
                if (d.sprite == "OV_Well") Solid(go, 2.0f, 0.5f);
                if (d.sprite == "OV_LampV3" && go != null) DayNight.Glow(go.transform, new Vector3(0f, 2.1f, 0f), "DN_Glow_Lamp", true, false);
            }

            // existing interactables: new spots + dressing
            Place(hub, "npc_map", new Vector2(0f, 12f), null);
            Place(hub, "Warden", new Vector2(-1.2f, 23.2f), null);
            Place(hub, "ToBoss", new Vector2(0.6f, 25.2f), S("OV_Signpost_Up"));
            Place(hub, "ToTraining", new Vector2(-21.2f, -4f), S("OV_Signpost_Left"));
            Place(hub, "ToResource", new Vector2(21.2f, -5f), S("OV_Signpost_Right"));
            Place(hub, "ToFarm", new Vector2(-21.2f, 12f), S("OV_Signpost_Left"));
            Place(hub, "FutureRegion", new Vector2(21.0f, 11.6f), S("OV_Barrier"));
            Place(hub, "Notice", new Vector2(3.4f, -9.4f), S("OV_NoticeBoard"));

            // villager art (replaces the graybox villagers)
            Npc(hub, "npc_shop", "OV_Npc_Mira");
            Npc(hub, "npc_smith", "OV_Npc_Bram");
            Npc(hub, "npc_quest", "OV_Npc_Lark");
            Npc(hub, "npc_storage", "OV_Npc_Nell");
            Npc(hub, "npc_map", "OV_Npc_Map");
            Npc(hub, "Warden", "OV_Npc_Warden");

            Walls(root);
        }

        // ---------- ground ----------
        enum G { Grass, Cobble, Slab, Split, Dirt }

        static G Kind(int cx, int cy)
        {
            for (int i = 0; i < Frontages.Length; i++)
            {
                var f = Frontages[i];
                if (cx >= f.minCol && cx < f.maxCol && cy >= f.baseY - 3 && cy < f.baseY)
                    return cx == f.doorCol ? G.Cobble : G.Slab;
            }
            if (cx >= -2 && cx < 2) return G.Cobble;                                   // main street
            if (cx >= -11 && cx < 11 && cy >= 9 && cy < 15) return G.Cobble;           // north square
            if (cx < -2 && cy >= -8 && cy < 0) return G.Cobble;                        // west street
            if (cx < -2 && cy < -8)                                                    // south-west, slab corner
            {
                int d = cx + cy;
                if (d <= -27) return G.Slab;
                if (d == -26) return G.Split;
                return G.Cobble;
            }
            if (cx >= 2 && cy >= -7 && cy < -3) return G.Cobble;                       // east street
            if (cy >= 11 && cy < 13 && (cx < -11 || cx >= 11)) return G.Dirt;          // farm + future paths
            return G.Grass;
        }

        static readonly System.Collections.Generic.Dictionary<Sprite, UnityEngine.Tilemaps.Tile> _tiles = new System.Collections.Generic.Dictionary<Sprite, UnityEngine.Tilemaps.Tile>();

        static UnityEngine.Tilemaps.Tilemap Layer(Transform grid, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(grid, false);
            var map = go.AddComponent<UnityEngine.Tilemaps.Tilemap>();
            var r = go.AddComponent<UnityEngine.Tilemaps.TilemapRenderer>();
            r.sortingOrder = order;
            r.mode = UnityEngine.Tilemaps.TilemapRenderer.Mode.Chunk;
            return map;
        }

        static void Set(UnityEngine.Tilemaps.Tilemap map, Sprite sprite, int x, int y)
        {
            if (sprite == null) return;
            if (!_tiles.TryGetValue(sprite, out var tile) || tile == null)
            {
                tile = ScriptableObject.CreateInstance<UnityEngine.Tilemaps.Tile>();
                tile.sprite = sprite;
                tile.name = sprite.name;
                _tiles[sprite] = tile;
            }
            map.SetTile(new Vector3Int(x, y, 0), tile);
        }

        static void PaintGround(Transform root, Sprite grass)
        {
            var gridGo = new GameObject("VillageGrid");
            gridGo.transform.SetParent(root, false);
            gridGo.AddComponent<Grid>().cellSize = Vector3.one;
            var curbs = Layer(gridGo.transform, "Curbs", CurbOrder);
            var curbsR = Layer(gridGo.transform, "CurbsRight", CurbOrder + 1);

            BakeGround(root);
            PaintGrassDetail(gridGo.transform);

            var top = S("OV_Curb_Top"); var left = S("OV_Curb_Left"); var right = S("OV_Curb_Right");
            for (int i = 0; i < Frontages.Length; i++)
            {
                var f = Frontages[i];
                int below = f.baseY - 4;
                for (int cx = f.minCol; cx < f.maxCol; cx++)
                    if (cx != f.doorCol && Kind(cx, below) == G.Cobble) Set(curbs, top, cx, below);
                for (int cy = f.baseY - 3; cy < f.baseY; cy++)
                {
                    Set(curbs, left, f.doorCol, cy);
                    Set(curbsR, right, f.doorCol, cy);
                }
            }
        }

        // ---------- baked ground (clip style) ----------
        // One texture for the whole village, classified per pixel from Kind(): street corners are cut at 45 deg
        // (closing with an L1 ball), and a slab band with thin curbs runs along every street edge.
        // Visual only: no collider depends on it.
        const int PxW = (MaxX - MinX) * 16, PxH = (MaxY - MinY) * 16;
        const int Chamfer = 40, Band = 18;
        enum P : byte { Grass, Street, Slab, Dirt, Band }
        static P[] _cls;                                     // per-pixel class after baking (bottom-up rows)

        static Color32[] Pixels(string name, out int w, out int h)
        {
            w = h = 0;
            var s = S(name);
            if (s == null || !s.texture.isReadable) return null;
            w = s.texture.width; h = s.texture.height;
            return s.texture.GetPixels32();
        }

        static void BakeGround(Transform root)
        {
            _cls = null;
            var cob = Pixels("OV_Cobble_V3", out int cw, out int ch);
            var slab = Pixels("OV_SlabV3", out int sw, out int sh);
            var grs = Pixels("OV_Grass", out int gw, out int gh);
            var drt = Pixels("OV_Dirt", out int dw, out int dh);
            if (cob == null || slab == null || grs == null || drt == null) { Debug.LogWarning("OriginalVillage: v3 ground art missing or not readable."); return; }

            int n = PxW * PxH;
            var baseCls = new P[n];
            for (int y = 0; y < PxH; y++) for (int x = 0; x < PxW; x++)
            {
                int cx = MinX + x / 16, cy = MinY + y / 16;
                P c;
                switch (Kind(cx, cy))
                {
                    case G.Cobble: c = P.Street; break;
                    case G.Dirt: c = P.Dirt; break;
                    case G.Grass: c = P.Grass; break;
                    default:
                        // forecourts stay slab; the south-west corner splits on a true diagonal
                        if (cx < -2 && cy < -8) c = SwSum(cx, cy, x, y) < -25f ? P.Slab : P.Street;
                        else c = P.Slab;
                        break;
                }
                baseCls[y * PxW + x] = c;
            }

            // closing of the street set (dilate, then erode); only grass may turn into street
            var dStreet = L1Distance(i => baseCls[i] == P.Street);
            var dOut = L1Distance(i => dStreet[i] > Chamfer);
            var cls = new P[n];
            for (int i = 0; i < n; i++)
                cls[i] = baseCls[i] == P.Grass && dOut[i] > Chamfer ? P.Street : baseCls[i];
            var dFinal = L1Distance(i => cls[i] == P.Street);

            var curbLt = new Color32(0xB4, 0xAE, 0xA6, 255);
            var curbMid = new Color32(0x8E, 0x88, 0x82, 255);
            var curbDk = new Color32(0x5E, 0x58, 0x52, 255);
            var outPx = new Color32[n];
            for (int y = 0; y < PxH; y++) for (int x = 0; x < PxW; x++)
            {
                int i = y * PxW + x;
                var c = cls[i];
                int d = dFinal[i];
                if ((c == P.Grass || c == P.Dirt) && d <= Band + 3)
                {
                    cls[i] = P.Band;
                    if (d == 1 || d == 4 || d == Band + 1 || d == Band + 3) outPx[i] = curbDk;
                    else if (d == 2 || d == Band + 2) outPx[i] = curbLt;
                    else if (d == 3) outPx[i] = curbMid;
                    else outPx[i] = slab[(y % sh) * sw + (x % sw)];
                    continue;
                }
                switch (c)
                {
                    case P.Street:
                        outPx[i] = cob[(y % ch) * cw + (x % cw)];
                        int cx = MinX + x / 16, cy = MinY + y / 16;
                        if (cx < -2 && cy < -8)
                        {
                            // curb on the street side of the south-west slab diagonal
                            float k = (SwSum(cx, cy, x, y) + 25f) * 16f;
                            if (k >= 0f && k < 1f) outPx[i] = curbDk;
                            else if (k >= 1f && k < 2f) outPx[i] = curbLt;
                            else if (k >= 2f && k < 3f) outPx[i] = curbMid;
                        }
                        break;
                    case P.Slab: outPx[i] = slab[(y % sh) * sw + (x % sw)]; break;
                    case P.Dirt: outPx[i] = drt[(y % dh) * dw + (x % dw)]; break;
                    default: outPx[i] = grs[(y % gh) * gw + (x % gw)]; break;
                }
            }
            _cls = cls;

            var tex = new Texture2D(PxW, PxH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "VillageGroundBaked" };
            tex.SetPixels32(outPx);
            tex.Apply(false, true);
            var go = new GameObject("VillageGroundBaked");
            go.transform.SetParent(root, false);
            go.transform.localPosition = new Vector3(MinX, MinY, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, PxW, PxH), Vector2.zero, 16f, 0, SpriteMeshType.FullRect);
            sr.sortingOrder = GroundOrder;
        }

        // continuous x + y (tile units) at a pixel centre, for the south-west diagonal
        static float SwSum(int cx, int cy, int x, int y) => cx + (x % 16 + 0.5f) / 16f + cy + (y % 16 + 0.5f) / 16f;

        // 4-neighbour BFS distance (L1, in px) from every pixel where seedAt(i) is true.
        static int[] L1Distance(System.Func<int, bool> seedAt)
        {
            int n = PxW * PxH;
            var dist = new int[n];
            var q = new int[n]; int head = 0, tail = 0;
            for (int i = 0; i < n; i++)
            {
                if (seedAt(i)) { dist[i] = 0; q[tail++] = i; }
                else dist[i] = int.MaxValue;
            }
            while (head < tail)
            {
                int i = q[head++], x = i % PxW, y = i / PxW, nd = dist[i] + 1;
                if (x > 0 && dist[i - 1] > nd) { dist[i - 1] = nd; q[tail++] = i - 1; }
                if (x < PxW - 1 && dist[i + 1] > nd) { dist[i + 1] = nd; q[tail++] = i + 1; }
                if (y > 0 && dist[i - PxW] > nd) { dist[i - PxW] = nd; q[tail++] = i - PxW; }
                if (y < PxH - 1 && dist[i + PxW] > nd) { dist[i + PxW] = nd; q[tail++] = i + PxW; }
            }
            return dist;
        }

        // Flowers on open grass, and the scalloped grass rim on dirt paths.
        static void PaintGrassDetail(Transform grid)
        {
            if (S("OV_GrassEdge_N") == null || _cls == null) return;
            var deco = Layer(grid, "GrassDeco", GroundOrder + 10);
            string[] sides = { "N", "E", "S", "W" };
            int[] dx = { 0, 1, 0, -1 }, dy = { 1, 0, -1, 0 };
            var edges = new UnityEngine.Tilemaps.Tilemap[4]; var edgeSprites = new Sprite[4];
            for (int i = 0; i < 4; i++) { edges[i] = Layer(grid, "GrassEdge" + sides[i], GroundOrder + 20 + i); edgeSprites[i] = S("OV_GrassEdge_" + sides[i]); }
            var flowers = new Sprite[3];
            for (int i = 0; i < 3; i++) flowers[i] = S("OV_Flower_" + i);
            for (int cy = MinY; cy < MaxY; cy++)
            for (int cx = MinX; cx < MaxX; cx++)
            {
                if (CellIs(cx, cy, P.Grass))
                {
                    int h = Hash(cx * 3 + 1, cy * 7 + 2);
                    if (h % 8 == 0) Set(deco, flowers[(h >> 4) % 3], cx, cy);
                    else if (h % 7 == 0) Set(deco, S("OV_TallGrass_" + ((h >> 5) & 1)), cx, cy);
                    continue;
                }
                if (!CellIs(cx, cy, P.Dirt)) continue;
                for (int i = 0; i < 4; i++)
                    if (CellIs(cx + dx[i], cy + dy[i], P.Grass)) Set(edges[i], edgeSprites[i], cx, cy);
            }
        }

        // true when the 16x16 cell is (sampled) entirely of class p
        static bool CellIs(int cx, int cy, P p)
        {
            if (cx < MinX || cx >= MaxX || cy < MinY || cy >= MaxY || _cls == null) return false;
            int x0 = (cx - MinX) * 16, y0 = (cy - MinY) * 16;
            for (int y = y0; y < y0 + 16; y += 5) for (int x = x0; x < x0 + 16; x += 5)
                if (_cls[y * PxW + x] != p) return false;
            return _cls[(y0 + 15) * PxW + x0 + 15] == p;
        }

        // ---------- frontages ----------
        static void BuildFrontage(Transform root, Transform hub, Frontage f)
        {
            var sprite = S(f.sprite);
            if (sprite == null) return;
            var shop = Prop(root, f.sprite, new Vector2(f.x, f.baseY), sprite);
            float w = sprite.rect.width / 16f;
            var box = shop.AddComponent<BoxCollider2D>();
            box.size = new Vector2(w - 0.4f, 1.0f);
            box.offset = new Vector2(0f, 0.5f);
            shop.AddComponent<BuildingFade>();

            // sign hangs off the left wall, bracket end on the corner post
            var sign = S(f.sign);
            if (sign != null)
            {
                float left = f.x - w * 0.5f;
                float wallTop = f.baseY + (sprite.rect.height - 58f) / 16f;
                float sy = wallTop - sign.rect.height / 16f + 0.1f;
                Prop(root, f.sign, new Vector2(left - 0.375f, sy), sign, Mathf.RoundToInt((sy - f.baseY) * 100f) + 5);
            }

            var npc = hub.Find(f.npc);
            if (npc != null) npc.localPosition = new Vector3(f.doorCol + 0.5f, f.baseY - 0.7f, 0f);
        }

        static void Place(Transform hub, string name, Vector2 local, Sprite sprite)
        {
            var t = hub.Find(name);
            if (t == null) return;
            t.localPosition = new Vector3(local.x, local.y, 0f);
            if (sprite == null) return;
            var sr = t.GetComponent<SpriteRenderer>();
            if (sr == null) return;
            sr.sprite = sprite;
            sr.color = Color.white;
            t.localScale = Vector3.one;
            if (t.GetComponent<SpriteSorter>() == null) t.gameObject.AddComponent<SpriteSorter>();
            var label = t.Find("Label");
            if (label != null) label.localPosition = new Vector3(0f, sprite.rect.height / 16f + 0.12f, 0f);
        }

        static void Npc(Transform hub, string name, string sheet)
        {
            var t = hub.Find(name);
            var frames = NpcIdle.Slice(S(sheet));
            if (t == null || frames == null) return;
            var sr = t.GetComponent<SpriteRenderer>();
            if (sr == null) return;
            sr.sprite = frames[0];
            sr.color = Color.white;
            t.localScale = Vector3.one;
            if (t.GetComponent<SpriteSorter>() == null) t.gameObject.AddComponent<SpriteSorter>();
            var idle = t.GetComponent<NpcIdle>() ?? t.gameObject.AddComponent<NpcIdle>();
            idle.frames = frames;
            // name label just above the head (the head top sits ~22 px above the feet)
            var label = t.Find("Label");
            if (label != null) label.localPosition = new Vector3(0f, 23f / 16f + 0.1f, 0f);
        }

        // Old hub dressing (VillageHubDress) is removed at runtime; its files stay untouched.
        static readonly string[] OldProps =
        {
            "EntranceL", "EntranceR", "ShopStall", "Forge", "QuestBanner", "StoreCrates",
            "TravelBoard", "PlazaWell", "DungeonL", "DungeonR", "BushW", "BushE", "VillageGround"
        };

        static void ClearOld(Transform hub)
        {
            for (int i = hub.childCount - 1; i >= 0; i--)
            {
                var c = hub.GetChild(i);
                bool old = c.name.StartsWith("VillageTree");
                for (int k = 0; !old && k < OldProps.Length; k++) old = c.name == OldProps[k];
                if (old) Object.Destroy(c.gameObject);
            }
        }

        static void Walls(Transform root)
        {
            var go = new GameObject("VillageEdge");
            go.transform.SetParent(root, false);
            var edge = go.AddComponent<EdgeCollider2D>();
            edge.points = new[]
            {
                new Vector2(MinX, MinY), new Vector2(MaxX, MinY), new Vector2(MaxX, MaxY),
                new Vector2(MinX, MaxY), new Vector2(MinX, MinY)
            };
        }

        // ---------- helpers ----------
        static GameObject Prop(Transform root, string name, Vector2 feet, Sprite sprite, int baseOrder = 0)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            go.transform.localPosition = feet;
            go.AddComponent<SpriteRenderer>().sprite = sprite;
            go.AddComponent<SpriteSorter>().baseOrder = baseOrder;
            return go;
        }

        static void Solid(GameObject go, float w, float h)
        {
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(w, h);
            box.offset = new Vector2(0f, h * 0.5f);
        }

        static void Tree(Transform root, Vector2 feet, Sprite tree, Sprite shadow)
        {
            if (!DayNight.AttachTreeShadow(root, feet, tree, ShadowOrder) && shadow != null)
            {
                var sh = new GameObject("TreeShadow");
                sh.transform.SetParent(root, false);
                sh.transform.localPosition = feet + new Vector2(0f, 0.15f);
                var ssr = sh.AddComponent<SpriteRenderer>();
                ssr.sprite = shadow;
                ssr.sortingOrder = ShadowOrder;
            }
            var go = new GameObject("VillageStreetTree");
            go.transform.SetParent(root, false);
            go.transform.localPosition = feet;
            go.AddComponent<SpriteRenderer>().sprite = tree;
            go.AddComponent<SpriteSorter>();
            go.AddComponent<TreeCanopyOcclusion>();
            go.AddComponent<BoxCollider2D>();
            go.AddComponent<TreeForestCFoot>();
        }

        static void Stamp(Transform parent, Sprite sprite, int x, int y, int order)
        {
            if (sprite == null) return;
            var go = new GameObject(sprite.name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(x + 0.5f, y + 0.5f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
        }

        static readonly System.Collections.Generic.Dictionary<string, Sprite> _subs = new System.Collections.Generic.Dictionary<string, Sprite>();

        // 16x16 cell (qx, qy counted from the top-left) of an n x n tile, so tiles repeat seamlessly.
        static Sprite Sub(Sprite tile, int qx, int qy, int n) => Sub(tile, qx, qy, n, n);

        static Sprite Sub(Sprite tile, int qx, int qy, int nx, int ny)
        {
            if (tile == null) return null;
            string key = tile.name + "_" + qx + "_" + qy;
            if (_subs.TryGetValue(key, out var s) && s != null) return s;
            var src = tile.rect;
            s = Sprite.Create(tile.texture, new Rect(src.x + qx * 16, src.y + (ny - 1 - qy) * 16, 16, 16), new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect);
            s.name = key;
            _subs[key] = s;
            return s;
        }

        static Sprite S(string name) => Resources.Load<Sprite>("World/OriginalVillage/" + name);
        static int Mod(int a, int m) => ((a % m) + m) % m;
        static int Div(int a, int m) => (a - Mod(a, m)) / m;
        static int Hash(int x, int y) { unchecked { int h = x * 73856093 ^ y * 19349663; return (h ^ (h >> 13)) & 0x7fffffff; } }
    }
}
