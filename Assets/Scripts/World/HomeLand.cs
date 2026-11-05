using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Home terrain painted in build mode (tab DAT): path / plot / grass cells baked into the ground,
    /// and a player-dug pond stored as water vertices driving marching-squares tiles (WILD_START_HANDOFF section 10).
    /// Digging a cell adds its four corners, grass removes them. Cell (x, y) covers Home-local [x, x+1) x [y, y+1).
    /// </summary>
    public static class HomeLand
    {
        public const int Path = 1, Plot = 2, Grass = 3;
        public const int PlotCap = 16;
        public enum Brush { Path, Plot, Dig, Grass }

        public static readonly string[] BrushNames = { "DUONG DAT", "LUONG RAU", "DAO AO", "LAP CO" };
        public const int DigOre = 1;
        public const int AreaCap = 64;

        public struct AreaPlan
        {
            public List<Vector2Int> ok;
            public List<Vector2Int> blocked;
            public List<string> why;
            public int ore;
        }

        const string LandKey = WorldCatalog.SavePrefix + "home.land";
        const string WaterKey = WorldCatalog.SavePrefix + "home.water";
        const string WaterSheet = "World/HomeDistrict/Garden/PD_WaterTiles";

        static Dictionary<Vector2Int, int> _cells;
        static HashSet<Vector2Int> _verts;
        static Sprite[] _tiles;
        static readonly List<Vector2Int> _deep = new List<Vector2Int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _cells = null;
            _verts = null;
            _tiles = null;
            _deep.Clear();
        }

        // ---------- queries ----------

        public static Vector2Int CellOf(Vector2 local) => new Vector2Int(Mathf.FloorToInt(local.x), Mathf.FloorToInt(local.y));

        public static int Kind(Vector2Int c)
        {
            Load();
            return _cells.TryGetValue(c, out int k) ? k : 0;
        }

        public static int Count(int kind)
        {
            Load();
            int n = 0;
            foreach (var kv in _cells)
                if (kv.Value == kind) n++;
            return n;
        }

        public static void Collect(int kind, System.Collections.Generic.List<Vector2Int> into)
        {
            Load();
            into.Clear();
            foreach (var kv in _cells)
                if (kv.Value == kind) into.Add(kv.Key);
        }

        public static bool HasWater { get { Load(); return _verts.Count > 0; } }

        /// <summary>Corner mask of a cell: 1 top-left, 2 top-right, 4 bottom-right, 8 bottom-left.</summary>
        public static int Mask(Vector2Int c)
        {
            Load();
            int m = 0;
            if (_verts.Contains(new Vector2Int(c.x, c.y + 1))) m |= 1;
            if (_verts.Contains(new Vector2Int(c.x + 1, c.y + 1))) m |= 2;
            if (_verts.Contains(new Vector2Int(c.x + 1, c.y))) m |= 4;
            if (_verts.Contains(new Vector2Int(c.x, c.y))) m |= 8;
            return m;
        }

        /// <summary>Any water tile is blocked for walking (player, eggs). Ducks ignore PondEdge.</summary>
        public static bool Blocks(Vector2Int c) => Mask(c) != 0;

        /// <summary>True when the cell under p shows any water.</summary>
        public static bool InWater(Vector2 p)
        {
            Load();
            return _verts.Count > 0 && Mask(CellOf(p)) != 0;
        }

        /// <summary>Every cell that shows a water tile (mask above 0).</summary>
        public static HashSet<Vector2Int> TileCells()
        {
            Load();
            var set = new HashSet<Vector2Int>();
            foreach (var v in _verts)
                for (int dx = -1; dx <= 0; dx++)
                    for (int dy = -1; dy <= 0; dy++)
                        set.Add(new Vector2Int(v.x + dx, v.y + dy));
            return set;
        }

        /// <summary>Dug water: grow 0 means an open-water cell; above 0 means within that distance of a water vertex.</summary>
        public static bool InDugWater(Vector2 p, float grow = 0f)
        {
            Load();
            if (_verts.Count == 0) return false;
            if (grow <= 0f) return Mask(CellOf(p)) == 15;
            float r = 0.5f + grow, r2 = r * r;
            int x0 = Mathf.FloorToInt(p.x - r), x1 = Mathf.CeilToInt(p.x + r);
            int y0 = Mathf.FloorToInt(p.y - r), y1 = Mathf.CeilToInt(p.y + r);
            for (int x = x0; x <= x1; x++)
                for (int y = y0; y <= y1; y++)
                    if (_verts.Contains(new Vector2Int(x, y)) && (new Vector2(x, y) - p).sqrMagnitude < r2) return true;
            return false;
        }

        /// <summary>A random open-water point (deep) or a point on the shore band around the dug pond.</summary>
        public static Vector2 RandomWater(bool deep, float grow = 0.8f)
        {
            Load();
            if (_deep.Count > 0 && deep)
            {
                var c = _deep[Random.Range(0, _deep.Count)];
                return new Vector2(c.x + Random.Range(0.2f, 0.8f), c.y + Random.Range(0.2f, 0.8f));
            }
            if (_verts.Count == 0) return Vector2.zero;
            int skip = Random.Range(0, _verts.Count);
            foreach (var v in _verts)
            {
                if (skip-- > 0) continue;
                return new Vector2(v.x, v.y) + Random.insideUnitCircle.normalized * grow;
            }
            return Vector2.zero;
        }

        /// <summary>Marching-squares tile for a corner mask (sheet rows count from the top).</summary>
        public static Sprite WaterTile(int mask)
        {
            if (mask <= 0 || mask > 15) return null;
            if (_tiles == null)
            {
                _tiles = new Sprite[16];
                var tex = Resources.Load<Texture2D>(WaterSheet);
                if (tex == null || tex.width < 64 || tex.height < 64) { Debug.LogWarning("HomeLand: PD_WaterTiles missing or undersized."); return null; }
                for (int m = 1; m < 16; m++)
                {
                    int row = m / 4, col = m % 4;
                    var s = Sprite.Create(tex, new Rect(col * 16, tex.height - (row + 1) * 16, 16, 16), new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect);
                    s.name = "PD_Water_" + m;
                    _tiles[m] = s;
                }
            }
            return _tiles[mask];
        }

        // ---------- painting ----------

        /// <summary>Empty when the brush may paint this cell, otherwise the reason.</summary>
        public static string Check(Brush brush, Vector2Int c, bool pay = true)
        {
            Load();
            var r = new Rect(c.x, c.y, 1f, 1f);
            if (!HomeArea.Open(new Vector2(c.x + 0.5f, c.y + 0.5f))) return "SUONG MU";
            if (brush == Brush.Grass)
            {
                if (BuildMode.SiteCovers(c)) return "VUONG DO KHAC";
                if (Kind(c) == Path || Kind(c) == Plot || Mask(c) != 0) return "";
                return "KHONG DOI";
            }
            if (BuildMode.SiteCovers(c)) return "VUONG DO KHAC";
            string fixedWhy = HomeDecor.FixedBlock(r);
            if (fixedWhy.Length > 0) return fixedWhy;
            if (HomeClear.Overlaps(r)) return "DON TRUOC";
            var pieces = HomeLayout.Pieces;
            for (int i = 0; i < pieces.Count; i++)
            {
                string d = pieces[i].def;
                if (d == "slab" || d == "flower_carpet" || d == "stone_bridge") continue;
                if (r.Overlaps(HomeDecor.Foot(d, pieces[i].pos))) return "VUONG DO KHAC";
            }
            if (brush == Brush.Dig)
            {
                if (Mask(c) == 15) return "DA CO NUOC";
            }
            else
            {
                if (Mask(c) != 0) return "VUONG NUOC";
                if (brush == Brush.Path && Kind(c) == Path) return "DA VE";
                if (brush == Brush.Plot)
                {
                    if (Kind(c) == Plot) return "DA VE";
                    if (Kind(c) != Path) return "VE DUONG DAT TRUOC";
                    if (Count(Plot) >= PlotCap) return "DU O";
                }
            }
            return "";
        }

        /// <summary>The cell already draws as grass (no path, plot, pond or baked sand).</summary>
        static bool AlreadyGrass(Vector2Int c)
        {
            if (Mask(c) != 0) return false;
            if (_cells.TryGetValue(c, out int k)) return k == Grass;
            return HomeDistrict.BaseKind(new Vector2(c.x + 0.5f, c.y + 0.5f)) == 0;
        }

        /// <summary>Owner rule: farm plots only go on expanded land (a painted path or the baked sand yard / trails / garden).</summary>
        public static bool IsSand(Vector2Int c)
        {
            int k = Kind(c);
            if (k == Path || k == Plot) return true;
            if (k == Grass) return false;
            return HomeDistrict.BaseKind(new Vector2(c.x + 0.5f, c.y + 0.5f)) == 1;
        }

        /// <summary>Paints one cell; true when something changed (the map then needs a rebuild).</summary>
        public static bool Paint(Brush brush, Vector2Int c)
        {
            Load();
            string why = Check(brush, c);
            if (why.Length > 0) return false;
            if (brush == Brush.Grass && AlreadyGrass(c)) return false;
            var tx = WorldTx.Begin();
            Apply(brush, c);
            RebuildDeep();
            tx.SetString(LandKey, EncodeCells()).SetString(WaterKey, EncodeWater()).Commit();
            return true;
        }

        /// <summary>Read-only region check. Ore is the dig total, not tested per cell.</summary>
        public static AreaPlan PlanArea(Brush brush, RectInt cells)
        {
            var plan = new AreaPlan
            {
                ok = new List<Vector2Int>(),
                blocked = new List<Vector2Int>(),
                why = new List<string>()
            };
            int w = Mathf.Clamp(cells.width, 0, 8);
            int h = Mathf.Clamp(cells.height, 0, 8);
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var c = new Vector2Int(cells.x + x, cells.y + y);
                    if (brush == Brush.Grass && AlreadyGrass(c))
                    {
                        plan.blocked.Add(c);
                        plan.why.Add("KHONG DOI");
                        continue;
                    }
                    string why = Check(brush, c, false);
                    if (why.Length == 0) plan.ok.Add(c);
                    else { plan.blocked.Add(c); plan.why.Add(why); }
                }
            plan.ore = 0;
            return plan;
        }

        /// <summary>Paints the still-valid cells in one save. False leaves land, water and ore untouched.</summary>
        public static bool PaintArea(Brush brush, IList<Vector2Int> cells, out int oreSpent)
        {
            oreSpent = 0;
            if (cells == null || cells.Count == 0) return false;
            Load();
            var ok = new List<Vector2Int>();
            int n = Mathf.Min(cells.Count, AreaCap);
            for (int i = 0; i < n; i++)
            {
                var c = cells[i];
                if (brush == Brush.Grass && AlreadyGrass(c)) continue;
                if (Check(brush, c, false).Length > 0) continue;
                ok.Add(c);
            }
            if (ok.Count == 0) return false;
            var tx = WorldTx.Begin();
            for (int i = 0; i < ok.Count; i++) Apply(brush, ok[i]);
            RebuildDeep();
            tx.SetString(LandKey, EncodeCells()).SetString(WaterKey, EncodeWater()).Commit();
            if (brush == Brush.Grass || brush == Brush.Path)
                for (int i = 0; i < ok.Count; i++) HomeFarm.DropBed(ok[i]);
            HomeDistrict.Rebuild();
            oreSpent = 0;
            return true;
        }

        public static IReadOnlyCollection<Vector2Int> WaterVerts { get { Load(); return _verts; } }

        public static HashSet<Vector2Int> VertsWith(IEnumerable<Vector2Int> extraCells)
        {
            Load();
            var set = new HashSet<Vector2Int>(_verts);
            if (extraCells == null) return set;
            foreach (var c in extraCells)
                foreach (var v in Corners(c)) set.Add(v);
            return set;
        }

        static void Apply(Brush brush, Vector2Int c)
        {
            switch (brush)
            {
                case Brush.Path: _cells[c] = Path; break;
                case Brush.Plot: _cells[c] = Plot; break;
                case Brush.Dig:
                    foreach (var v in Corners(c)) _verts.Add(v);
                    _cells.Remove(c);
                    break;
                case Brush.Grass:
                    if (Mask(c) != 0)
                        foreach (var v in Corners(c)) _verts.Remove(v);
                    if (HomeDistrict.BaseKind(new Vector2(c.x + 0.5f, c.y + 0.5f)) == 0) _cells.Remove(c);
                    else _cells[c] = Grass;
                    break;
            }
        }

        // ---------- save ----------

        static void Load()
        {
            if (_cells != null) return;
            _cells = new Dictionary<Vector2Int, int>();
            _verts = new HashSet<Vector2Int>();
            foreach (var row in PlayerPrefs.GetString(LandKey, "").Split(';'))
            {
                var f = row.Split(',');
                if (f.Length < 3) continue;
                if (int.TryParse(f[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x)
                    && int.TryParse(f[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y)
                    && int.TryParse(f[2], NumberStyles.Integer, CultureInfo.InvariantCulture, out int k) && k >= Path && k <= Grass)
                    _cells[new Vector2Int(x, y)] = k;
            }
            foreach (var row in PlayerPrefs.GetString(WaterKey, "").Split(';'))
            {
                var f = row.Split(',');
                if (f.Length < 2) continue;
                if (int.TryParse(f[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int x)
                    && int.TryParse(f[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int y))
                    _verts.Add(new Vector2Int(x, y));
            }
            RebuildDeep();
        }

        static IEnumerable<Vector2Int> Corners(Vector2Int c)
        {
            yield return new Vector2Int(c.x, c.y);
            yield return new Vector2Int(c.x + 1, c.y);
            yield return new Vector2Int(c.x, c.y + 1);
            yield return new Vector2Int(c.x + 1, c.y + 1);
        }

        static void RebuildDeep()
        {
            _deep.Clear();
            foreach (var c in TileCells())
                if (Mask(c) == 15) _deep.Add(c);
        }

        static string EncodeCells()
        {
            var sb = new StringBuilder();
            foreach (var kv in _cells)
            {
                if (sb.Length > 0) sb.Append(';');
                sb.Append(kv.Key.x).Append(',').Append(kv.Key.y).Append(',').Append(kv.Value);
            }
            return sb.ToString();
        }

        /// <summary>Whole land and water maps, so one brush stroke can be undone.</summary>
        public static void ReadSave(out string land, out string water)
        {
            Load();
            land = EncodeCells();
            water = EncodeWater();
        }

        public static void WriteSave(string land, string water, int refundOre = 0)
        {
            _cells = null;
            _verts = null;
            int ore = WorldSave.GetQty("ore");
            var tx = WorldTx.Begin().SetString(LandKey, land ?? "").SetString(WaterKey, water ?? "");
            if (refundOre > 0) tx.SetInt(WorldSave.KeyItem("ore"), ore + refundOre);
            tx.Commit();
            Load();
            HomeDistrict.Rebuild();
        }

        static string EncodeWater()
        {
            var sb = new StringBuilder();
            foreach (var v in _verts)
            {
                if (sb.Length > 0) sb.Append(';');
                sb.Append(v.x).Append(',').Append(v.y);
            }
            return sb.ToString();
        }
    }
}
