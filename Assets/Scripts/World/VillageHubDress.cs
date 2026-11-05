using System.Collections.Generic;
using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Paints the hub only. Service positions, gates, and interact callbacks are unchanged.
    /// </summary>
    public static class VillageHubDress
    {
        // Hub world Y is around 80, and SpriteSorter uses -Y * 100.
        // Ground has to sit below those orders or the village draws under the grass.
        const int GroundOrder = -9000;
        const int PathOrder = -8000;

        public static void Apply(Transform hub)
        {
            if (hub == null) return;
            var art = Resources.Load<VillageArtSet>("World/VillageArtSet");
            var grass = Grass(art);
            var dirt = art != null && art.dirt != null ? art.dirt : DirtTile();
            HideFlat(hub);
            PaintGround(hub, grass, dirt);

            var tree = art != null ? art.tree : null;
            var small = art != null ? art.treeSmall : null;
            if (small != null)
            {
                Tree(hub, new Vector2(-6.6f, -6.2f), small, true);
                Tree(hub, new Vector2(6.6f, -6.2f), small, true);
                Tree(hub, new Vector2(-6.6f, 6.6f), small, true);
                Tree(hub, new Vector2(6.6f, 6.6f), small, true);
            }
            if (tree != null)
            {
                Tree(hub, new Vector2(-12.4f, -4f), tree, false);
                Tree(hub, new Vector2(12.4f, -4f), tree, false);
                Tree(hub, new Vector2(-12.4f, 4.5f), tree, false);
                Tree(hub, new Vector2(12.4f, 4.5f), tree, false);
                Tree(hub, new Vector2(-9.2f, 0.2f), tree, false);
                Tree(hub, new Vector2(9.2f, 0.2f), tree, false);
            }

            Prop(hub, "EntranceL", new Vector2(-1.7f, -6.5f), art != null && art.post != null ? art.post : Post());
            Prop(hub, "EntranceR", new Vector2(1.7f, -6.5f), art != null && art.post != null ? art.post : Post());
            Prop(hub, "ShopStall", new Vector2(-4.15f, -1.15f), art != null && art.stall != null ? art.stall : Stall());
            Prop(hub, "Forge", new Vector2(4.15f, -1.15f), art != null && art.forge != null ? art.forge : Forge());
            Prop(hub, "QuestBanner", new Vector2(-4.05f, -3.15f), art != null && art.banner != null ? art.banner : Banner(new Color32(72, 116, 186, 255)));
            Prop(hub, "StoreCrates", new Vector2(4.05f, -3.15f), art != null && art.crates != null ? art.crates : Crates());
            Prop(hub, "TravelBoard", new Vector2(1.85f, 1.15f), art != null && art.board != null ? art.board : Board());
            Prop(hub, "PlazaWell", new Vector2(-1.75f, 1.25f), art != null && art.well != null ? art.well : Well());
            Prop(hub, "DungeonL", new Vector2(-1.25f, 3.35f), art != null && art.post != null ? art.post : Post());
            Prop(hub, "DungeonR", new Vector2(1.25f, 3.35f), art != null && art.post != null ? art.post : Post());
            Prop(hub, "BushW", new Vector2(-4.7f, 4.3f), Bush());
            Prop(hub, "BushE", new Vector2(4.7f, 4.3f), Bush());

            Dress(hub, "npc_shop", Person(new Color32(186, 64, 58, 255)));
            Dress(hub, "npc_smith", Person(new Color32(96, 102, 112, 255)));
            Dress(hub, "npc_quest", Person(new Color32(66, 104, 176, 255)));
            Dress(hub, "npc_storage", Person(new Color32(122, 78, 48, 255)));
            Dress(hub, "npc_map", Person(new Color32(58, 122, 78, 255)));
            Dress(hub, "Warden", Person(new Color32(196, 154, 64, 255)));
            Dress(hub, "Notice", art != null && art.board != null ? art.board : Board());
            Dress(hub, "FutureRegion", art != null && art.arch != null ? art.arch : Arch(new Color32(90, 90, 98, 255)));
            Dress(hub, "ToTraining", art != null && art.arch != null ? art.arch : Arch(new Color32(176, 72, 64, 255)));
            Dress(hub, "ToFarm", art != null && art.arch != null ? art.arch : Arch(new Color32(86, 140, 62, 255)));
            Dress(hub, "ToResource", art != null && art.arch != null ? art.arch : Arch(new Color32(150, 112, 64, 255)));
            Dress(hub, "ToBoss", art != null && art.arch != null ? art.arch : Arch(new Color32(120, 48, 48, 255)));
        }

        static void HideFlat(Transform hub)
        {
            for (int i = 0; i < hub.childCount; i++)
            {
                var child = hub.GetChild(i);
                if (child.name != "Ground" && child.name != "Road") continue;
                var sr = child.GetComponent<SpriteRenderer>();
                if (sr != null) sr.enabled = false;
            }
        }

        static void PaintGround(Transform hub, Sprite[] grass, Sprite dirt)
        {
            var root = new GameObject("VillageGround");
            root.transform.SetParent(hub, false);
            for (int y = -11; y <= 10; y++)
            {
                for (int x = -16; x <= 16; x++)
                {
                    bool path = IsPath(x, y);
                    var sprite = path ? dirt : grass[(x * 3 + y * 5) & 3];
                    if (sprite == null) continue;
                    Stamp(root.transform, path ? "Path" : "Grass", sprite, x, y, path ? PathOrder : GroundOrder);
                }
            }
        }

        static bool IsPath(int x, int y)
        {
            if (x == 0 && y >= -8 && y <= 6) return true;
            if (y >= -7 && y <= -5 && x >= -1 && x <= 1) return true;
            if (y >= 0 && y <= 2 && x >= -2 && x <= 2) return true;
            if (y == -1 && x >= -4 && x <= 4) return true;
            if (y == -3 && x >= -4 && x <= -1) return true;
            if (y == -3 && x >= 1 && x <= 4) return true;
            if (x == -4 && y >= -3 && y <= -1) return true;
            if (x == 4 && y >= -3 && y <= -1) return true;
            if (y == 5 && x >= -4 && x <= 4) return true;
            if (y == 6 && (x == -3 || x == -1 || x == 1 || x == 3)) return true;
            if (y == 3 && x >= -1 && x <= 1) return true;
            return false;
        }

        static void Stamp(Transform parent, string name, Sprite sprite, int x, int y, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            float ppu = Mathf.Max(1f, sprite.pixelsPerUnit);
            go.transform.localPosition = new Vector3(x + sprite.pivot.x / ppu, y + sprite.pivot.y / ppu, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
        }

        static void Tree(Transform hub, Vector2 feet, Sprite sprite, bool trunk)
        {
            var go = new GameObject(trunk ? "VillageTree" : "VillageTreeFar");
            go.transform.SetParent(hub, false);
            go.transform.localPosition = feet;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;
            go.AddComponent<SpriteSorter>();
            go.AddComponent<TreeCanopyOcclusion>();
            if (!trunk) return;
            var box = go.AddComponent<BoxCollider2D>();
            box.size = new Vector2(0.4f, 0.3f);
            box.offset = new Vector2(0f, 0.18f);
        }

        static void Prop(Transform hub, string name, Vector2 feet, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(hub, false);
            go.transform.localPosition = feet;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = Color.white;
            go.AddComponent<SpriteSorter>();
        }

        static void Dress(Transform hub, string name, Sprite sprite)
        {
            var t = hub.Find(name);
            if (t == null || sprite == null) return;
            t.localScale = Vector3.one;
            var sr = t.GetComponent<SpriteRenderer>();
            if (sr == null) return;
            sr.sprite = sprite;
            sr.color = Color.white;
            if (t.GetComponent<SpriteSorter>() == null)
                t.gameObject.AddComponent<SpriteSorter>();
            var label = t.Find("Label");
            if (label == null) return;
            float h = sprite.rect.height / Mathf.Max(1f, sprite.pixelsPerUnit);
            label.localPosition = new Vector3(0f, h + 0.12f, 0f);
            var rt = label.GetComponent<RectTransform>();
            if (rt != null) rt.localScale = Vector3.one * 0.01f;
        }

        static Sprite[] Grass(VillageArtSet art)
        {
            var found = new List<Sprite>();
            if (art != null && art.grass != null)
            {
                for (int i = 0; i < art.grass.Length; i++)
                    if (art.grass[i] != null) found.Add(art.grass[i]);
            }
            if (found.Count == 0)
            {
                found.Add(GrassTile(0));
                found.Add(GrassTile(1));
                found.Add(GrassTile(2));
            }
            while (found.Count < 4) found.Add(found[found.Count - 1]);
            return found.ToArray();
        }

        static Sprite GrassTile(int variant)
        {
            var tex = NewTex(16, 16);
            var a = new Color32(78, 140, 62, 255);
            var b = new Color32(92, 158, 72, 255);
            var c = new Color32(58, 112, 48, 255);
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                int h = (x * 5 + y * 9 + variant * 3) % 13;
                tex.SetPixel(x, y, h == 0 ? b : h < 3 ? c : a);
            }
            return Finish(tex, "VillageGrass" + variant, new Vector2(0.5f, 0.5f));
        }

        static Sprite DirtTile()
        {
            var tex = NewTex(16, 16);
            var a = new Color32(176, 140, 92, 255);
            var b = new Color32(150, 112, 70, 255);
            var c = new Color32(198, 168, 122, 255);
            for (int y = 0; y < 16; y++)
            for (int x = 0; x < 16; x++)
            {
                int h = (x * 7 + y * 3) % 11;
                tex.SetPixel(x, y, h == 0 ? c : h < 3 ? b : a);
            }
            return Finish(tex, "VillageDirt", new Vector2(0.5f, 0.5f));
        }

        static Sprite Person(Color32 cloth)
        {
            var tex = NewTex(12, 18);
            var skin = new Color32(232, 188, 142, 255);
            var hair = new Color32(72, 48, 32, 255);
            var shoe = new Color32(48, 36, 28, 255);
            var line = new Color32(36, 28, 24, 255);
            Fill(tex, 4, 0, 2, 2, shoe);
            Fill(tex, 7, 0, 2, 2, shoe);
            Fill(tex, 4, 2, 2, 4, cloth);
            Fill(tex, 7, 2, 2, 4, cloth);
            Fill(tex, 3, 6, 6, 5, cloth);
            Fill(tex, 3, 6, 6, 1, line);
            Fill(tex, 4, 11, 4, 4, skin);
            Fill(tex, 4, 14, 4, 2, hair);
            Fill(tex, 3, 12, 1, 3, hair);
            Fill(tex, 8, 12, 1, 3, hair);
            tex.SetPixel(5, 13, line);
            tex.SetPixel(7, 13, line);
            return Finish(tex, "Villager", new Vector2(0.5f, 0f));
        }

        static Sprite Stall()
        {
            var tex = NewTex(28, 22);
            var wood = new Color32(112, 72, 40, 255);
            var dark = new Color32(72, 46, 28, 255);
            var cloth = new Color32(176, 64, 58, 255);
            var trim = new Color32(236, 214, 170, 255);
            Fill(tex, 2, 0, 3, 8, wood);
            Fill(tex, 23, 0, 3, 8, wood);
            Fill(tex, 1, 8, 26, 3, dark);
            for (int i = 0; i < 6; i++)
                Fill(tex, 2 + i * 4, 11, 4, 6, (i & 1) == 0 ? cloth : trim);
            Fill(tex, 2, 17, 24, 2, dark);
            return Finish(tex, "Stall", new Vector2(0.5f, 0f));
        }

        static Sprite Forge()
        {
            var tex = NewTex(26, 22);
            var stone = new Color32(96, 96, 104, 255);
            var dark = new Color32(48, 48, 54, 255);
            var iron = new Color32(70, 74, 82, 255);
            var fire = new Color32(232, 120, 40, 255);
            var coal = new Color32(255, 196, 64, 255);
            Fill(tex, 2, 0, 22, 6, stone);
            Fill(tex, 4, 6, 18, 8, dark);
            Fill(tex, 6, 8, 14, 4, iron);
            Fill(tex, 8, 4, 10, 3, fire);
            Fill(tex, 11, 5, 4, 2, coal);
            Fill(tex, 8, 14, 10, 3, stone);
            return Finish(tex, "Forge", new Vector2(0.5f, 0f));
        }

        static Sprite Crates()
        {
            var tex = NewTex(24, 16);
            var wood = new Color32(148, 102, 58, 255);
            var dark = new Color32(96, 64, 36, 255);
            Fill(tex, 1, 0, 10, 10, wood);
            Fill(tex, 1, 5, 10, 1, dark);
            Fill(tex, 6, 0, 1, 10, dark);
            Fill(tex, 12, 0, 10, 12, wood);
            Fill(tex, 12, 6, 10, 1, dark);
            Fill(tex, 17, 0, 1, 12, dark);
            return Finish(tex, "Crates", new Vector2(0.5f, 0f));
        }

        static Sprite Board()
        {
            var tex = NewTex(16, 22);
            var wood = new Color32(102, 68, 40, 255);
            var paper = new Color32(228, 214, 180, 255);
            var ink = new Color32(72, 64, 56, 255);
            Fill(tex, 7, 0, 2, 10, wood);
            Fill(tex, 2, 10, 12, 10, paper);
            Fill(tex, 2, 10, 12, 1, wood);
            Fill(tex, 4, 16, 8, 1, ink);
            Fill(tex, 4, 14, 6, 1, ink);
            Fill(tex, 4, 12, 7, 1, ink);
            return Finish(tex, "Board", new Vector2(0.5f, 0f));
        }

        static Sprite Banner(Color32 cloth)
        {
            var tex = NewTex(14, 24);
            var wood = new Color32(102, 68, 40, 255);
            var dark = new Color32(42, 36, 32, 255);
            Fill(tex, 6, 0, 2, 22, wood);
            Fill(tex, 8, 16, 5, 6, cloth);
            Fill(tex, 8, 16, 5, 1, dark);
            return Finish(tex, "Banner", new Vector2(0.5f, 0f));
        }

        static Sprite Well()
        {
            var tex = NewTex(18, 16);
            var stone = new Color32(128, 128, 136, 255);
            var dark = new Color32(72, 76, 84, 255);
            var water = new Color32(64, 112, 156, 255);
            Fill(tex, 2, 0, 14, 8, stone);
            Fill(tex, 4, 2, 10, 4, water);
            Fill(tex, 2, 7, 14, 2, dark);
            Fill(tex, 8, 9, 2, 6, dark);
            Fill(tex, 4, 14, 10, 2, dark);
            return Finish(tex, "Well", new Vector2(0.5f, 0f));
        }

        static Sprite Post()
        {
            var tex = NewTex(8, 20);
            var wood = new Color32(112, 74, 42, 255);
            var dark = new Color32(72, 46, 28, 255);
            Fill(tex, 3, 0, 2, 18, wood);
            Fill(tex, 2, 16, 4, 2, dark);
            return Finish(tex, "Post", new Vector2(0.5f, 0f));
        }

        static Sprite Arch(Color32 flag)
        {
            var tex = NewTex(18, 22);
            var wood = new Color32(112, 74, 42, 255);
            var dark = new Color32(64, 42, 26, 255);
            Fill(tex, 1, 0, 3, 16, wood);
            Fill(tex, 14, 0, 3, 16, wood);
            Fill(tex, 1, 16, 16, 3, dark);
            Fill(tex, 12, 10, 5, 4, flag);
            return Finish(tex, "Arch", new Vector2(0.5f, 0f));
        }

        static Sprite Bush()
        {
            var tex = NewTex(14, 10);
            var leaf = new Color32(54, 110, 48, 255);
            var lite = new Color32(86, 146, 64, 255);
            Fill(tex, 2, 0, 10, 6, leaf);
            Fill(tex, 4, 4, 6, 4, lite);
            Fill(tex, 1, 2, 3, 3, leaf);
            return Finish(tex, "Bush", new Vector2(0.5f, 0f));
        }

        static Texture2D NewTex(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            var clear = new Color32(0, 0, 0, 0);
            var px = new Color32[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = clear;
            tex.SetPixels32(px);
            return tex;
        }

        static void Fill(Texture2D tex, int x, int y, int w, int h, Color32 color)
        {
            for (int yy = y; yy < y + h; yy++)
            for (int xx = x; xx < x + w; xx++)
            {
                if (xx < 0 || yy < 0 || xx >= tex.width || yy >= tex.height) continue;
                tex.SetPixel(xx, yy, color);
            }
        }

        static Sprite Finish(Texture2D tex, string name, Vector2 pivot)
        {
            tex.Apply(false, false);
            tex.name = name;
            var sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), pivot, 16f, 0, SpriteMeshType.FullRect);
            sprite.name = name;
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
