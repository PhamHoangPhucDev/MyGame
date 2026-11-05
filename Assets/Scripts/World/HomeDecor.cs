using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GameplayPrototype
{
    /// <summary>
    /// Build mode step 2: free placement of Home pieces on a 1-unit grid (TRANG TRI tab) and the remove tool (X tab).
    /// Pieces come from and go back to a per-piece stock. The Home map rebuilds from HomeLayout after each change.
    /// </summary>
    public static class HomeDecor
    {
        public struct Def
        {
            public string id, name;
            public Vector2 foot;      // width, height in units
            public bool centred;      // foot centred on the position (pond, garden); otherwise it sits on the feet
            public float beauty;      // beauty score once placed (WILD_START_HANDOFF section 5 / 10)
        }

        public const float SaplingDay = WorldContent.DayLengthSeconds;

        public static bool IsSapling(string id) => id == "sapling_leaf" || id == "sapling_teal";

        /// <summary>0 sprout, 1 sapling, 2 young tree, 3 grown: one stage per in-game day.</summary>
        public static int SaplingStage(double planted)
        {
            if (planted < 0d) return 3;
            return Mathf.Clamp((int)((GameplayClock.Now - planted) / SaplingDay), 0, 3);
        }

        public static readonly Def[] Defs =
        {
            new Def { id = "pond", name = "AO", foot = new Vector2(5.4f, 2.8f), centred = true },
            new Def { id = "garden", name = "VUON RAU", foot = new Vector2(4.2f, 4.2f), centred = true },
            new Def { id = "coop", name = "CHUONG GA", foot = new Vector2(2.0f, 0.6f) },
            new Def { id = "tree_leaf", name = "CAY LA", foot = new Vector2(1.0f, 0.6f) },
            new Def { id = "tree_teal", name = "CAY XANH", foot = new Vector2(1.0f, 0.6f) },
            new Def { id = "tree_c", name = "CAY RUNG", foot = new Vector2(1.0f, 0.6f) },
            new Def { id = "bush_leaf", name = "BUI LA", foot = new Vector2(1.0f, 0.5f) },
            new Def { id = "bush_teal", name = "BUI XANH", foot = new Vector2(1.0f, 0.5f) },
            new Def { id = "fence", name = "HANG RAO", foot = new Vector2(2.0f, 0.3f) },
            new Def { id = "low_fence", name = "RAO THAP", foot = new Vector2(2.0f, 0.3f) },
            new Def { id = "flower_row_0", name = "HOA 1", foot = new Vector2(2.0f, 0.4f) },
            new Def { id = "flower_row_1", name = "HOA 2", foot = new Vector2(2.0f, 0.4f) },
            new Def { id = "flower_row_2", name = "HOA 3", foot = new Vector2(2.0f, 0.4f) },
            new Def { id = "planter_a", name = "CHAU HOA 1", foot = new Vector2(1.4f, 0.5f) },
            new Def { id = "planter_b", name = "CHAU HOA 2", foot = new Vector2(1.4f, 0.5f) },
            new Def { id = "scarecrow", name = "BU NHIN", foot = new Vector2(0.8f, 0.4f) },
            new Def { id = "bench", name = "GHE", foot = new Vector2(1.4f, 0.4f) },
            new Def { id = "hay", name = "ROM", foot = new Vector2(1.0f, 0.5f) },
            new Def { id = "rocks", name = "DA", foot = new Vector2(1.0f, 0.5f) },
            new Def { id = "stump", name = "GOC CAY", foot = new Vector2(0.8f, 0.4f) },
            new Def { id = "sapling_leaf", name = "CAY NON LA", foot = new Vector2(1.0f, 0.6f), beauty = 1f },
            new Def { id = "sapling_teal", name = "CAY NON XANH", foot = new Vector2(1.0f, 0.6f), beauty = 1f },
            new Def { id = "slab", name = "DA LAT", foot = new Vector2(1.0f, 1.0f), centred = true, beauty = 0.25f },
            new Def { id = "garden_gate", name = "CONG VUON", foot = new Vector2(2.0f, 0.3f), beauty = 2f },
            new Def { id = "swing", name = "XICH DU", foot = new Vector2(2.0f, 0.4f), beauty = 3f },
            new Def { id = "hang_lantern", name = "DEN LONG", foot = new Vector2(0.6f, 0.3f), beauty = 2f },
            new Def { id = "white_fence", name = "HANG RAO TRANG", foot = new Vector2(2.0f, 0.3f), beauty = 1f },
            new Def { id = "potted_plant", name = "CHAU CAY", foot = new Vector2(0.8f, 0.4f), beauty = 1f },
            new Def { id = "stone_bridge", name = "CAU DA", foot = new Vector2(3.0f, 1.5f), centred = true, beauty = 3f },
            new Def { id = "statue", name = "TUONG", foot = new Vector2(0.6f, 0.3f), beauty = 4f },
            new Def { id = "flower_shelf", name = "KE HOA", foot = new Vector2(2.0f, 0.3f), beauty = 2f },
            new Def { id = "flower_carpet", name = "THAM HOA", foot = new Vector2(2.0f, 1.0f), centred = true, beauty = 2f },
            new Def { id = "lamp_post", name = "COT DEN", foot = new Vector2(0.6f, 0.5f) },
            new Def { id = "mailbox", name = "HOP THU", foot = new Vector2(0.8f, 0.5f) },
        };

        public const int PageSize = 4;
        public static int Pages => (Defs.Length + PageSize - 1) / PageSize;

        static readonly Rect HouseFoot = Rect.MinMaxRect(-3.6f, 2.4f, 3.6f, 8.6f);
        static readonly Rect Road = Rect.MinMaxRect(-0.8f, -8f, 0.8f, 0.6f);

        public static int Pick = -1;
        public static int Page;
        static Vector2 _lastMouse;
        static float _mouseAt = -99f;
        static bool _mouseSeen;
        static int _hover = -1;
        static SpriteRenderer _ghost, _cells;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Pick = -1;
            Page = 0;
            _mouseAt = -99f;
            _mouseSeen = false;
            _hover = -1;
            _ghost = _cells = null;
        }

        public static int IndexOf(string id)
        {
            for (int i = 0; i < Defs.Length; i++) if (Defs[i].id == id) return i;
            return -1;
        }

        static string StockKey(string id) => WorldCatalog.SavePrefix + "home.stock." + id;
        public static int Stock(string id) => PlayerPrefs.GetInt(StockKey(id), 0);

        public static void AddStock(string id, int n)
        {
            if (n <= 0 || string.IsNullOrEmpty(id)) return;
            PlayerPrefs.SetInt(StockKey(id), Stock(id) + n);
        }

        /// <summary>Zone-local foot rectangle of a piece at a position.</summary>
        public static Rect Foot(string id, Vector2 pos)
        {
            int i = IndexOf(id);
            var f = i >= 0 ? Defs[i].foot : Vector2.one;
            bool centred = i >= 0 && Defs[i].centred;
            return new Rect(pos.x - f.x / 2f, centred ? pos.y - f.y / 2f : pos.y - 0.1f, f.x, f.y);
        }

        // ---------- cursor ----------

        static Transform Root()
        {
            var go = GameObject.Find("HomeDistrict");
            return go != null ? go.transform : null;
        }

        /// <summary>Zone-local cell under the mouse (when it moved recently) or one step in front of the hero.</summary>
        public static Vector2 Cursor()
        {
            var p = CursorRaw();
            return new Vector2(Mathf.Round(p.x), Mathf.Round(p.y));
        }

        /// <summary>Unsnapped zone-local cursor (terrain brushes take the cell under it).</summary>
        public static Vector2 CursorRaw()
        {
            if (BuildAim.Active) return BuildAim.Local;
            var root = Root();
            if (!PointerOverUi)
            {
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    var m = mouse.position.ReadValue();
                    if (!_mouseSeen) { _mouseSeen = true; _lastMouse = m; }
                    else if ((m - _lastMouse).sqrMagnitude > 4f) { _lastMouse = m; _mouseAt = Time.unscaledTime; }
                }
                if (MobileTouch.TryFinger(out var finger, out _, out _, out _))
                {
                    _mouseSeen = true;
                    _lastMouse = finger;
                    _mouseAt = Time.unscaledTime;
                }
            }
            Vector2 world;
            var cam = Camera.main;
            if (cam != null && Time.unscaledTime - _mouseAt < 3f)
                world = cam.ScreenToWorldPoint(new Vector3(_lastMouse.x, _lastMouse.y, -cam.transform.position.z));
            else
            {
                var hero = GameObject.FindGameObjectWithTag("Player");
                if (hero == null) return Vector2.zero;
                var combat = hero.GetComponent<PlayerCombat>();
                var face = combat != null && combat.Facing.sqrMagnitude > 0.01f ? combat.Facing.normalized : Vector2.down;
                world = (Vector2)hero.transform.position + face * 1.5f;
            }
            return root != null ? (Vector2)root.InverseTransformPoint(world) : world;
        }

        public static bool PointerOverUi
        {
            get
            {
                if (MobileTouch.TryFinger(out _, out _, out _, out int id) && MobileTouch.OverUi(id)) return true;
                return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            }
        }

        // ---------- rules ----------

        /// <summary>Empty string when the piece fits, otherwise the reason shown to the player.</summary>
        public static string Check(string id, Vector2 pos, int ignore = -1)
        {
            var r = Foot(id, pos);
            string fixedWhy = FixedBlock(r);
            if (fixedWhy.Length > 0) return fixedWhy;
            string fog = HomeArea.Block(r);
            if (fog.Length > 0) return fog;
            if (id != "stone_bridge" && HomeLand.HasWater)
                for (int x = Mathf.FloorToInt(r.xMin); x < Mathf.CeilToInt(r.xMax); x++)
                    for (int y = Mathf.FloorToInt(r.yMin); y < Mathf.CeilToInt(r.yMax); y++)
                        if (HomeLand.Mask(new Vector2Int(x, y)) != 0) return "VUONG NUOC";
            if (HomeClear.Overlaps(r)) return "DON TRUOC";
            var pieces = HomeLayout.Pieces;
            for (int i = 0; i < pieces.Count; i++)
                if (i != ignore && r.Overlaps(Foot(pieces[i].def, pieces[i].pos))) return "VUONG DO KHAC";
            return "";
        }

        /// <summary>The house and the gate road stay blocked. Open fog is the placement edge.</summary>
        public static string FixedBlock(Rect r)
        {
            if (r.Overlaps(HouseFoot)) return "VUONG NHA";
            if (r.Overlaps(Road)) return "VUONG DUONG";
            return "";
        }

        /// <summary>Placed piece under the point: a tree's trunk or canopy, otherwise the grown foot. -1 when none.</summary>
        public static int PieceAt(Vector2 cell)
        {
            int tree = TreeSpriteAt(cell);
            if (tree >= 0) return tree;
            var pieces = HomeLayout.Pieces;
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < pieces.Count; i++)
            {
                var r = Foot(pieces[i].def, pieces[i].pos);
                r.xMin -= 0.4f; r.xMax += 0.4f; r.yMin -= 0.4f; r.yMax += 0.6f;
                if (!r.Contains(cell)) continue;
                float d = (r.center - cell).sqrMagnitude;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        /// <summary>The drawn sprite under the point, including a lit lamp glow. -1 when the click misses the art.</summary>
        public static int SpriteAt(Vector2 local)
        {
            var root = Root();
            if (root == null) return -1;
            Vector3 world = root.TransformPoint(local);
            var pieces = HomeLayout.Pieces;
            int best = -1;
            float bestD = float.MaxValue;
            foreach (var tag in root.GetComponentsInChildren<HomePieceRef>())
            {
                int i = tag.index;
                if ((uint)i >= (uint)pieces.Count) continue;
                foreach (var sr in tag.GetComponentsInChildren<SpriteRenderer>())
                {
                    if (sr.sprite == null || !sr.enabled || sr.color.a < 0.2f) continue;
                    var b = sr.bounds;
                    if (world.x < b.min.x || world.x > b.max.x || world.y < b.min.y || world.y > b.max.y) continue;
                    float dx = b.center.x - world.x, dy = b.center.y - world.y;
                    if (dx * dx + dy * dy < bestD) { bestD = dx * dx + dy * dy; best = i; }
                }
            }
            return best;
        }

        /// <summary>Trunk and canopy sprites, so a click on the leaves selects the tree.</summary>
        static int TreeSpriteAt(Vector2 local)
        {
            var root = Root();
            if (root == null) return -1;
            Vector3 world = root.TransformPoint(local);
            var pieces = HomeLayout.Pieces;
            int best = -1;
            float bestD = float.MaxValue;
            foreach (var tag in root.GetComponentsInChildren<HomePieceRef>())
            {
                int i = tag.index;
                if ((uint)i >= (uint)pieces.Count) continue;
                string def = pieces[i].def;
                if (def != "tree_leaf" && def != "tree_teal" && def != "tree_c" && def != "sapling_leaf" && def != "sapling_teal") continue;
                foreach (var sr in tag.GetComponentsInChildren<SpriteRenderer>())
                {
                    if (sr.sprite == null) continue;
                    if (sr.gameObject != tag.gameObject && sr.gameObject.name != "Canopy") continue;
                    var b = sr.bounds;
                    if (world.x < b.min.x || world.x > b.max.x || world.y < b.min.y || world.y > b.max.y) continue;
                    float dx = b.center.x - world.x, dy = b.center.y - world.y;
                    if (dx * dx + dy * dy < bestD) { bestD = dx * dx + dy * dy; best = i; }
                }
            }
            return best;
        }

        // ---------- actions ----------

        public static bool TryPlace(bool flip, bool confirm = false)
        {
            if (Pick < 0) return WorldServices.Fail(Deny.Invalid, "CHON DO");
            var def = Defs[Pick];
            if (Stock(def.id) <= 0) return WorldServices.Fail(Deny.InsufficientResources, "HET " + def.name);
            var cell = Cursor();
            string why = Check(def.id, cell);
            if (why.Length > 0) return WorldServices.Fail(Deny.Invalid, why);
            YardHand.PushDecor();
            var list = new List<HomePiece>(HomeLayout.Pieces) { new HomePiece(def.id, cell.x, cell.y, flip, IsSapling(def.id) ? GameplayClock.Now : -1d, BuildMode.Quarter) };
            int left = Stock(def.id) - 1;
            HomeLayout.Set(list, tx => tx.SetInt(StockKey(def.id), left));
            HomeDistrict.Rebuild();
            SpendConfirm.Clear();
            if (left <= 0) Pick = -1;
            return WorldServices.Ok("DA DAT " + def.name);
        }

        public static bool TryRemove()
        {
            int i = PieceAt(CursorRaw());
            if (i < 0) return WorldServices.Fail(Deny.Invalid, "KHONG CO DO");
            return RemoveIndex(i);
        }

        /// <summary>Puts one placed piece back in stock. False when the index is gone.</summary>
        public static bool RemoveIndex(int i)
        {
            var list = new List<HomePiece>(HomeLayout.Pieces);
            if ((uint)i >= (uint)list.Count) return false;
            YardHand.PushDecor();
            string id = list[i].def;
            if (IsSapling(id) && SaplingStage(list[i].t) >= 3) id = id == "sapling_leaf" ? "tree_leaf" : "tree_teal";
            list.RemoveAt(i);
            int have = Stock(id) + 1;
            HomeLayout.Set(list, tx => tx.SetInt(StockKey(id), have));
            _hover = -1;
            HomeDistrict.Rebuild();
            int d = IndexOf(id);
            return WorldServices.Ok("DA CAT " + (d >= 0 ? Defs[d].name : id));
        }

        /// <summary>Moves a placed piece. The old spot stays if the new one is blocked.</summary>
        public static bool TryMove(int index, Vector2 pos, bool flip, int turn)
        {
            var list = new List<HomePiece>(HomeLayout.Pieces);
            if ((uint)index >= (uint)list.Count) return WorldServices.Fail(Deny.Invalid, "KHONG CO DO");
            var piece = list[index];
            string why = Check(piece.def, pos, index);
            if (why.Length > 0) return WorldServices.Fail(Deny.Invalid, why);
            YardHand.PushDecor();
            piece.pos = pos;
            piece.flip = flip;
            piece.turn = turn & 3;
            list[index] = piece;
            HomeLayout.Set(list);
            HomeDistrict.Rebuild();
            return WorldServices.Ok("DA DOI");
        }

        /// <summary>Turns one placed decor piece around. The previous layout stays on the undo stack.</summary>
        public static bool FlipIndex(int index)
        {
            var list = new List<HomePiece>(HomeLayout.Pieces);
            if ((uint)index >= (uint)list.Count) return false;
            YardHand.PushDecor();
            var piece = list[index];
            piece.flip = !piece.flip;
            piece.turn = 0;
            list[index] = piece;
            HomeLayout.Set(list);
            HomeDistrict.Rebuild();
            return true;
        }

        public static void SetStock(string id, int n)
        {
            if (string.IsNullOrEmpty(id)) return;
            PlayerPrefs.SetInt(StockKey(id), Mathf.Max(0, n));
        }

        // ---------- visuals ----------

        public static void Paint(int tab, bool flip)
        {
            var root = Root();
            if (root == null) { Hide(); return; }
            var cell = Cursor();
            int moving = YardHand.Moving;
            if (moving >= 0)
            {
                var pieces = HomeLayout.Pieces;
                if ((uint)moving >= (uint)pieces.Count) { Hide(); return; }
                var piece = pieces[moving];
                ShowGhost(piece.def, cell, flip, Check(piece.def, cell, moving).Length == 0, BuildMode.Quarter);
                return;
            }
            if (tab == 1 && Pick >= 0)
            {
                var def = Defs[Pick];
                ShowGhost(def.id, cell, flip, Stock(def.id) > 0 && Check(def.id, cell).Length == 0, BuildMode.Quarter);
            }
            else Hide();
            // Remove tool tints red; a selected piece (build UX v2) tints gold.
            _hoverTint = tab == 2 && YardHand.Selected < 0 ? RemoveTint : SelectTint;
            SetHover(tab == 2 || YardHand.Selected >= 0 ? (YardHand.Selected >= 0 ? YardHand.Selected : PieceAt(CursorRaw())) : -1, root);
        }

        static readonly Color RemoveTint = new Color(1f, 0.55f, 0.55f, 1f);
        static readonly Color SelectTint = new Color(1f, 0.93f, 0.55f, 1f);
        static Color _hoverTint = RemoveTint;

        static void ShowGhost(string id, Vector2 cell, bool flip, bool ok, int turn = 0)
        {
            var root = Root();
            if (root == null) return;
            EnsureGhost();
            _ghost.sprite = HomeDistrict.PieceSprite(id);
            _ghost.flipX = flip;
            _ghost.transform.localEulerAngles = new Vector3(0f, 0f, -(turn & 3) * 90f);
            _ghost.color = ok ? new Color(0.6f, 1f, 0.6f, 0.45f) : new Color(1f, 0.45f, 0.45f, 0.45f);
            _ghost.transform.position = root.TransformPoint(cell);
            var r = Foot(id, cell);
            _cells.transform.position = root.TransformPoint(r.center);
            _cells.transform.localScale = new Vector3(r.width, r.height, 1f);
            _cells.color = ok ? new Color(0.49f, 0.84f, 0.49f, 0.55f) : new Color(0.88f, 0.28f, 0.28f, 0.55f);
            _ghost.enabled = _ghost.sprite != null;
            _cells.enabled = true;
        }

        public static void Hide()
        {
            if (_ghost != null) _ghost.enabled = false;
            if (_cells != null) _cells.enabled = false;
            var root = Root();
            if (root != null) SetHover(-1, root);
        }

        static void EnsureGhost()
        {
            if (_ghost != null) return;
            _ghost = new GameObject("DecorGhost").AddComponent<SpriteRenderer>();
            _ghost.sortingOrder = 5000;
            _cells = new GameObject("DecorGhostFoot").AddComponent<SpriteRenderer>();
            var tile = Resources.Load<Sprite>("World/HomeDistrict/Build/BD_BlueprintTile");
            _cells.sprite = tile;
            _cells.sortingOrder = 4999;
            if (tile != null) _cells.transform.localScale = Vector3.one;
        }

        static void SetHover(int index, Transform root)
        {
            if (index == _hover) return;
            Tint(root, _hover, Color.white);
            _hover = index;
            Tint(root, _hover, _hoverTint);
        }

        static void Tint(Transform root, int index, Color c)
        {
            if (index < 0) return;
            foreach (var tag in root.GetComponentsInChildren<HomePieceRef>(true))
                if (tag.index == index && tag.TryGetComponent<SpriteRenderer>(out var sr)) sr.color = new Color(c.r, c.g, c.b, sr.color.a);
        }
    }

    /// <summary>Marks the objects built for one HomeLayout piece (index into HomeLayout.Pieces).</summary>
    public class HomePieceRef : MonoBehaviour
    {
        public int index;
    }
}
