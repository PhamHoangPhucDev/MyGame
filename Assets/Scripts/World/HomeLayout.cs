using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace GameplayPrototype
{
    public struct HomePiece
    {
        public string def;
        public Vector2 pos;
        public bool flip;
        /// <summary>Quarter turns clockwise. 0..3.</summary>
        public int turn;
        /// <summary>GameplayClock time it was planted (saplings); -1 when unused.</summary>
        public double t;

        public HomePiece(string def, float x, float y, bool flip = false, double t = -1d, int turn = 0)
        {
            this.def = def;
            pos = new Vector2(x, y);
            this.flip = flip;
            this.t = t;
            this.turn = turn & 3;
        }
    }

    /// <summary>
    /// Saved layout of the Home map: every placeable piece as {def, x, y, flip} in Home-local units.
    /// Without a save the approved fixed layout is used. The gate, sign, mailbox and house anchor are not pieces.
    /// </summary>
    public static class HomeLayout
    {
        const string SaveKey = WorldCatalog.SavePrefix + "home.layout";

        public static readonly Vector2 DefaultPond = new Vector2(-4.7f, -3.9f);
        public static readonly Vector2 DefaultGarden = new Vector2(4.9f, -4.1f);
        public static readonly Vector2 DefaultCoop = new Vector2(6.5f, -0.6f);

        static List<HomePiece> _pieces;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => _pieces = null;

        public static IReadOnlyList<HomePiece> Pieces { get { Load(); return _pieces; } }
        public static bool Saved => PlayerPrefs.HasKey(SaveKey);

        public static Vector2 Pond => Pos("pond", DefaultPond);
        public static Vector2 Garden => Pos("garden", DefaultGarden);
        public static Vector2 Coop => Pos("coop", DefaultCoop);
        public static Vector2 PondShift => Pond - DefaultPond;
        public static Vector2 GardenShift => Garden - DefaultGarden;
        public static Vector2 CoopShift => Coop - DefaultCoop;

        public static bool Has(string def)
        {
            Load();
            for (int i = 0; i < _pieces.Count; i++) if (_pieces[i].def == def) return true;
            return false;
        }

        public static Vector2 Pos(string def, Vector2 fallback)
        {
            Load();
            for (int i = 0; i < _pieces.Count; i++) if (_pieces[i].def == def) return _pieces[i].pos;
            return fallback;
        }

        /// <summary>Replaces the whole layout and saves it. The Home map rebuilds on the next visit.</summary>
        public static void Set(List<HomePiece> pieces, System.Action<WorldTx> also = null)
        {
            _pieces = new List<HomePiece>(pieces);
            var tx = WorldTx.Begin().SetString(SaveKey, Encode(_pieces));
            also?.Invoke(tx);
            tx.Commit();
        }

        public static string Capture()
        {
            Load();
            return Encode(_pieces);
        }

        public static void Restore(string raw)
        {
            _pieces = Decode(raw ?? "");
            WorldTx.Begin().SetString(SaveKey, Encode(_pieces)).Commit();
            HomeDistrict.Rebuild();
        }

        /// <summary>Writes the current decor again. Used when build mode closes.</summary>
        public static void Flush()
        {
            Load();
            if (!PlayerPrefs.HasKey(SaveKey)) return;
            Set(new List<HomePiece>(_pieces));
        }

        static void Load()
        {
            if (_pieces != null) return;
            if (PlayerPrefs.HasKey(SaveKey)) _pieces = Decode(PlayerPrefs.GetString(SaveKey, ""));
            else if (HomeArea.WildStart) _pieces = new List<HomePiece>();
            else _pieces = Defaults();
            GrantFixtures();
        }

        /// <summary>The four lamp posts and the mailbox move into decor stock once, unless the layout already has them.</summary>
        static void GrantFixtures()
        {
            const string key = WorldCatalog.SavePrefix + "home.fixtures";
            if (PlayerPrefs.HasKey(key)) return;
            PlayerPrefs.SetInt(key, 1);
            int posts = 0, mail = 0;
            for (int i = 0; i < _pieces.Count; i++)
            {
                if (_pieces[i].def == "lamp_post") posts++;
                else if (_pieces[i].def == "mailbox") mail++;
            }
            HomeDecor.AddStock("lamp_post", 4 - posts);
            HomeDecor.AddStock("mailbox", 1 - mail);
            PlayerPrefs.Save();
        }

        static string Encode(List<HomePiece> list)
        {
            var sb = new StringBuilder();
            foreach (var p in list)
            {
                if (sb.Length > 0) sb.Append(';');
                sb.Append(p.def).Append(',')
                  .Append(p.pos.x.ToString("0.###", CultureInfo.InvariantCulture)).Append(',')
                  .Append(p.pos.y.ToString("0.###", CultureInfo.InvariantCulture)).Append(',')
                  .Append(p.flip ? 1 : 0).Append(',')
                  .Append(p.t.ToString("0.##", CultureInfo.InvariantCulture)).Append(',')
                  .Append(p.turn & 3);
            }
            return sb.ToString();
        }

        static List<HomePiece> Decode(string raw)
        {
            var list = new List<HomePiece>();
            foreach (var row in raw.Split(';'))
            {
                var f = row.Split(',');
                if (f.Length < 4 || string.IsNullOrEmpty(f[0])) continue;
                if (!float.TryParse(f[1], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)) continue;
                if (!float.TryParse(f[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)) continue;
                double t = -1d;
                if (f.Length > 4 && !double.TryParse(f[4], NumberStyles.Float, CultureInfo.InvariantCulture, out t)) t = -1d;
                int turn = 0;
                if (f.Length > 5) int.TryParse(f[5], NumberStyles.Integer, CultureInfo.InvariantCulture, out turn);
                list.Add(new HomePiece(f[0], x, y, f[3] == "1", t, turn));
            }
            return list;
        }

        /// <summary>The approved Home map (candidate 3) as pieces.</summary>
        public static List<HomePiece> Defaults()
        {
            var d = new List<HomePiece>
            {
                new HomePiece("pond", DefaultPond.x, DefaultPond.y),
                new HomePiece("garden", DefaultGarden.x, DefaultGarden.y),
                new HomePiece("coop", DefaultCoop.x, DefaultCoop.y),
                // trees, bushes, fences
                new HomePiece("tree_leaf", -6.6f, 6.6f), new HomePiece("tree_c", 6.6f, 6.8f),
                new HomePiece("tree_teal", -7.0f, 1.4f), new HomePiece("tree_leaf", 7.1f, 1.6f),
                new HomePiece("bush_teal", -4.6f, 6.0f), new HomePiece("bush_leaf", 4.4f, 6.2f),
                new HomePiece("bush_leaf", -7.2f, -6.9f), new HomePiece("bush_teal", 7.2f, -6.9f),
                new HomePiece("fence", -6.8f, 4.2f), new HomePiece("fence", -4.8f, 4.2f), new HomePiece("fence", 4.8f, 4.2f), new HomePiece("fence", 6.8f, 4.2f),
                // outer woods
                new HomePiece("tree_teal", -10.8f, 5.0f), new HomePiece("tree_leaf", -10.6f, -6.2f),
                new HomePiece("tree_leaf", -14.6f, 6.8f), new HomePiece("tree_teal", 11.0f, 5.4f),
                new HomePiece("tree_c", 10.6f, -6.6f),
                new HomePiece("bush_teal", -13.4f, -1.2f), new HomePiece("rocks", -12.4f, -2.2f), new HomePiece("stump", -12.2f, 3.8f),
                new HomePiece("bush_leaf", 13.4f, -0.6f), new HomePiece("stump", 12.4f, -4.2f),
                new HomePiece("bush_leaf", 14.4f, 6.6f), new HomePiece("rocks", 13.6f, 7.4f),
                new HomePiece("bush_leaf", -9.4f, 0.4f), new HomePiece("bush_teal", 9.6f, 1.6f), new HomePiece("bush_teal", -12.4f, -7.2f), new HomePiece("bush_leaf", 12.6f, -7.4f),
                new HomePiece("rocks", -11.6f, 2.4f), new HomePiece("rocks", 12.0f, 2.8f),
                new HomePiece("bush_leaf", -4.6f, 9.0f), new HomePiece("bush_teal", 4.8f, 9.2f),
                new HomePiece("tree_teal", -10.4f, 9.4f), new HomePiece("tree_leaf", 10.6f, 9.6f),
                // yard and garden dressing
                new HomePiece("planter_a", -3.4f, 0.0f), new HomePiece("planter_b", 3.4f, 0.0f),
                new HomePiece("flower_row_0", -1.6f, -1.1f), new HomePiece("flower_row_2", 1.6f, -1.1f), new HomePiece("flower_row_1", 1.6f, -6.6f),
                new HomePiece("low_fence", 2.6f, -1.8f),
                HomeFarm.Enabled ? new HomePiece("scarecrow", 7.3f, -3.6f) : new HomePiece("scarecrow", 4.9f, -3.0f),
                new HomePiece("bench", -5.4f, -6.4f),
                new HomePiece("hay", 7.5f, -2.4f), new HomePiece("rocks", -3.6f, -6.9f),
                new HomePiece("lamp_post", -2.2f, 1.5f), new HomePiece("lamp_post", -0.7f, 1.5f),
                new HomePiece("lamp_post", 0.8f, 1.5f), new HomePiece("lamp_post", 2.3f, 1.5f),
                new HomePiece("mailbox", 4.3f, 1.1f),
            };
            return d;
        }
    }
}
