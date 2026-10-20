using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

// R2C equipment layer prototype.
// Body sheets never contain weapons. Every weapon / shield is its own sprite with an anchor pixel (0,0 = hand).
// Each body frame publishes anchors (grip_main, grip_off) + weapon pose (8 directions, drawn per pose, never
// rotated at runtime) + draw order. Swapping gear = swapping the sprite set; swapping skin = swapping the body sheet
// (or a palette remap of it).
public static class EquipGen
{
    // ---------- sprites ----------
    public class Spr
    {
        public Dictionary<Point, Color> Px = new Dictionary<Point, Color>();
        public void Set(int x, int y, Color c) { Px[new Point(x, y)] = c; }
    }

    public static Color Hex(string h) { return HeroConceptGen.Hex(h); }

    // 0 right, 1 up-right, 2 up, 3 up-left, 4 left, 5 down-left, 6 down, 7 down-right (screen y down)
    public static readonly Point[] Dir =
    {
        new Point(1, 0), new Point(1, -1), new Point(0, -1), new Point(-1, -1),
        new Point(-1, 0), new Point(-1, 1), new Point(0, 1), new Point(1, 1),
    };
    public static readonly string[] DirName = { "R", "UR", "U", "UL", "L", "DL", "D", "DR" };

    static bool Diag(Point d) { return d.X != 0 && d.Y != 0; }

    // Dark outline on every empty 4-neighbour: keeps thin weapons readable on grass.
    public static Spr Outline(Spr s, Color k)
    {
        var o = new Spr();
        foreach (var kv in s.Px) o.Px[kv.Key] = kv.Value;
        foreach (var p in s.Px.Keys.ToList())
            foreach (var n in new[] { new Point(p.X + 1, p.Y), new Point(p.X - 1, p.Y), new Point(p.X, p.Y + 1), new Point(p.X, p.Y - 1) })
                if (!o.Px.ContainsKey(n)) o.Px[n] = k;
        return o;
    }

    public class BladeDef
    {
        public string Id, Name;
        public int Length = 12, GuardHalf = 2;
        public string Core = "E4ECF4", Band = "A0AABA", Guard = "D8B048", Wrap = "2E3A78", Pommel = "D8B048", Outline = "1A1C24";
    }

    // Sword / dagger: pommel -2, grip -1..0 (hand at 0), guard at +1, blade +2..+1+L (2 px band), tip at +2+L.
    public static Spr Blade(BladeDef w, int dir)
    {
        var d = Dir[dir]; var s = new Spr();
        Func<int, Point> at = k => new Point(d.X * k, d.Y * k);
        var p = new Point(-d.Y, d.X);
        s.Set(at(-2).X, at(-2).Y, Hex(w.Pommel));
        s.Set(at(-1).X, at(-1).Y, Hex(w.Wrap)); s.Set(0, 0, Hex(w.Wrap));
        var g = at(1); s.Set(g.X, g.Y, Hex(w.Guard));
        for (int i = 1; i <= w.GuardHalf; i++)
        {
            if (Diag(d) && i > 1) break;
            s.Set(g.X + p.X * i, g.Y + p.Y * i, Hex(w.Guard)); s.Set(g.X - p.X * i, g.Y - p.Y * i, Hex(w.Guard));
        }
        for (int k = 2; k <= 1 + w.Length; k++)
        {
            var c = at(k); s.Set(c.X, c.Y, Hex(w.Core));
            var e = Diag(d) ? new Point(c.X + d.X, c.Y) : new Point(c.X + p.X, c.Y + p.Y);
            if (!s.Px.ContainsKey(e)) s.Set(e.X, e.Y, Hex(w.Band));
        }
        var t = at(2 + w.Length); s.Set(t.X, t.Y, Hex(w.Core));
        return Outline(s, Hex(w.Outline));
    }

    // Hammer: pommel -1, shaft 0..L, head block (3 deep, +-2 wide) at the end. Axis directions only.
    public static Spr Hammer(int dir, int shaft, string wood, string woodHi, string steel, string steelHi, string steelLo, string outline)
    {
        var d = Dir[dir]; var p = new Point(-d.Y, d.X); var s = new Spr();
        for (int k = -1; k <= shaft; k++) s.Set(d.X * k, d.Y * k, Hex(k % 3 == 0 ? woodHi : wood));
        for (int k = shaft + 1; k <= shaft + 3; k++)
            for (int w = -2; w <= 2; w++)
            {
                string c = w == -2 || k == shaft + 1 ? steelHi : (w == 2 || k == shaft + 3 ? steelLo : steel);
                s.Set(d.X * k + p.X * w, d.Y * k + p.Y * w, Hex(c));
            }
        return Outline(s, Hex(outline));
    }

    // Staff: shaft -4..L, crystal (3x3 + glow tip) at the end.
    public static Spr Staff(int dir, int shaft, string wood, string woodHi, string gem, string gemHi, string gemLo, string outline)
    {
        var d = Dir[dir]; var p = new Point(-d.Y, d.X); var s = new Spr();
        for (int k = -4; k <= shaft; k++) s.Set(d.X * k, d.Y * k, Hex(k % 4 == 0 ? woodHi : wood));
        // prongs holding the crystal
        s.Set(d.X * shaft + p.X, d.Y * shaft + p.Y, Hex(wood)); s.Set(d.X * shaft - p.X, d.Y * shaft - p.Y, Hex(wood));
        for (int k = shaft + 1; k <= shaft + 3; k++)
            for (int w = -1; w <= 1; w++)
                s.Set(d.X * k + p.X * w, d.Y * k + p.Y * w, Hex(k == shaft + 2 && w == 0 ? gemHi : (w == 1 ? gemLo : gem)));
        s.Set(d.X * (shaft + 4), d.Y * (shaft + 4), Hex(gemHi));
        return Outline(s, Hex(outline));
    }

    public static Spr FromGrid(string[] rows, Dictionary<char, string> pal, int ax, int ay, string outline)
    {
        var s = new Spr();
        for (int y = 0; y < rows.Length; y++) for (int x = 0; x < rows[y].Length; x++)
                if (rows[y][x] != '.') s.Set(x - ax, y - ay, Hex(pal[rows[y][x]]));
        return Outline(s, Hex(outline));
    }

    public static Spr FlipX(Spr s)
    {
        var o = new Spr();
        foreach (var kv in s.Px) o.Px[new Point(-kv.Key.X, kv.Key.Y)] = kv.Value;
        return o;
    }

    // ---------- gear catalogue ----------
    public static readonly BladeDef SteelLongsword = new BladeDef { Id = "sword_steel", Name = "Kiem dai thep", Length = 6 };
    public static readonly BladeDef RuneBlade = new BladeDef
    {
        Id = "sword_rune", Name = "Kiem han (skin vu khi)", Length = 7, GuardHalf = 1,
        Core = "8A94A8", Band = "4A5262", Guard = "B02838", Wrap = "1C1A22", Pommel = "B02838", Outline = "0E0C12",
    };
    public static readonly BladeDef Dagger = new BladeDef
    {
        Id = "dagger_steel", Name = "Dao gam", Length = 4, GuardHalf = 1,
        Core = "D8DCE4", Band = "8A90A0", Guard = "6A6876", Wrap = "B02838", Pommel = "6A6876", Outline = "0E0C12",
    };

    static readonly Dictionary<char, string> ArenShieldPal = new Dictionary<char, string>
    {
        { 'Z', "E4ECF4" }, { 'a', "A0AABA" }, { 'A', "6A7484" },
        { 'u', "4A58A0" }, { 'c', "2E3A78" }, { 'C', "1E2450" }, { 'm', "E8C048" },
        { 'd', "8A5A34" }, { 'b', "5A3A22" },
    };

    // Heater shield: Down = face, Up = back with straps, Side = 3/4 edge. Anchor = handle.
    public static Spr HeaterShield(string view)
    {
        string[] r;
        int ax, ay;
        if (view == "Down") { r = new[] { "ZZaaaAA", "ZuucccA", "ZuuccCA", "ZmmmmmA", "ZucccCA", "ZucccCA", ".ZucCA.", "..ZCA..", "...A..." }; ax = 3; ay = 4; }
        else if (view == "Up") { r = new[] { "AAaaaAA", "AdbbbdA", "AbdddbA", "AdbbbdA", "AbdddbA", "AdbbbdA", ".AbdbA.", "..AbA..", "...A..." }; ax = 3; ay = 4; }
        else { r = new[] { "ZZaA", "ZucA", "ZucA", "mmmA", "ZucA", "uccA", ".cCA", ".cA.", "..A." }; ax = 1; ay = 4; }
        return FromGrid(r, ArenShieldPal, ax, ay, "1A1C24");
    }

    static readonly Dictionary<char, string> WoodShieldPal = new Dictionary<char, string>
    {
        { 'i', "5A6272" }, { 'I', "8E98A8" }, { 'd', "9A6A3C" }, { 'l', "C08A50" }, { 'b', "6A4428" }, { 'g', "C8D0DC" },
    };

    public static Spr RoundWoodShield(string view)
    {
        string[] r;
        int ax, ay;
        if (view == "Down") { r = new[] { ".IIii.", "Ildddi", "Idggbi", "Idggbi", "Iddbbi", ".iiii." }; ax = 3; ay = 3; }
        else if (view == "Up") { r = new[] { ".iiii.", "ibbbbi", "ibddbi", "ibddbi", "ibbbbi", ".iiii." }; ax = 3; ay = 3; }
        else { r = new[] { "Ii", "ld", "gd", "gb", "db", "ii" }; ax = 1; ay = 3; }
        return FromGrid(r, WoodShieldPal, ax, ay, "1A1C24");
    }

    public static Spr BromShield()
    {
        var pal = new Dictionary<char, string> { { 'm', "E8C048" }, { 'Z', "C8D0DC" }, { 'a', "8A94A4" }, { 'A', "5A6070" }, { 'g', "F0E0A0" } };
        var r = new[] { "..mmmmm..", ".mZaaaAm.", "mZaaaaaAm", "maagggaAm", "maagggaAm", "maagggaAm", "maaaaaAAm", ".mAaaAAm.", "..mmmmm.." };
        return FromGrid(r, pal, 4, 4, "1C1A22");
    }

    public static Spr Bow()
    {
        var pal = new Dictionary<char, string> { { 'b', "8A5A34" }, { 'l', "B07A48" }, { 'G', "5E8A34" }, { 'w', "EEE8DC" } };
        // wider arc (wood bulges 3 px from the string), grip wrap at rows 5-6 = anchor
        var r = new[] { "..ll", ".lbw", ".b.w", "b..w", "b..w", "G..w", "G..w", "b..w", "b..w", ".b.w", ".lbw", "..ll" };
        return FromGrid(r, pal, 0, 5, "2A2218");
    }

    // Hand overlay drawn on top of a held weapon's grip (anchor = top-left of the fist), so the grip reads as held.
    public static Spr Hand(string skin, string shade, string outline)
    {
        var s = new Spr();
        s.Set(0, 0, Hex(skin)); s.Set(1, 0, Hex(skin)); s.Set(0, 1, Hex(shade)); s.Set(1, 1, Hex(shade));
        return Outline(s, Hex(outline));
    }

    // ---------- bodies ----------
    public static Color?[,] Body(char[,] g, Dictionary<char, string> pal)
    {
        var c = new Color?[32, 32];
        for (int y = 0; y < 21; y++) for (int x = 0; x < 14; x++)
                if (g[x, y] != '.') c[9 + x, 9 + y] = Hex(pal[g[x, y]]);
        return c;
    }

    public static char[,] Grid(string[] rows)
    {
        var g = new char[14, 21];
        for (int y = 0; y < 21; y++) for (int x = 0; x < 14; x++) g[x, y] = rows[y][x];
        return g;
    }

    // ---------- Aren attack ----------
    public class Pose
    {
        public int MainX, MainY, MainDir; public bool MainFront;
        public int OffX, OffY; public bool OffFront = true;
        public string Note;
    }

    // Anchors in 32x32 cell pixels (body template at x 9..22, y 9..29). Sword hand = viewer-left in Down,
    // viewer-right in Up, far arm in Side. Shield arm = the pauldron with the white cloth.
    public static Pose[] ArenAttackPoses(string view)
    {
        if (view == "Down") return new[]
        {
            new Pose { MainX = 10, MainY = 20, MainDir = 3, MainFront = false, OffX = 21, OffY = 23, Note = "windup: kiem gio sau vai" },
            new Pose { MainX = 13, MainY = 25, MainDir = 7, MainFront = true,  OffX = 21, OffY = 24, Note = "strike: chem cheo xuong truoc (impact)" },
            new Pose { MainX = 16, MainY = 25, MainDir = 6, MainFront = true,  OffX = 21, OffY = 23, Note = "recover: kiem chuc xuong" },
        };
        if (view == "Up") return new[]
        {
            new Pose { MainX = 21, MainY = 20, MainDir = 1, MainFront = true,  OffX = 10, OffY = 23, Note = "windup: kiem qua vai phai (gan camera)" },
            new Pose { MainX = 17, MainY = 17, MainDir = 2, MainFront = false, OffX = 10, OffY = 24, Note = "strike: chem len phia truoc (xa camera)" },
            new Pose { MainX = 14, MainY = 19, MainDir = 3, MainFront = false, OffX = 10, OffY = 23, Note = "recover" },
        };
        return new[]
        {
            new Pose { MainX = 12, MainY = 20, MainDir = 3, MainFront = false, OffX = 20, OffY = 23, Note = "windup: kiem gio ra sau" },
            new Pose { MainX = 20, MainY = 24, MainDir = 0, MainFront = true,  OffX = 19, OffY = 24, Note = "strike: chem ngang ve truoc (impact)" },
            new Pose { MainX = 20, MainY = 26, MainDir = 7, MainFront = true,  OffX = 20, OffY = 23, Note = "recover: kiem cheo xuong" },
        };
    }

    public static string[] ArenBase(string view)
    {
        if (view == "Down") return HeroConceptGen.Roster.First(r => r.Id == "4").Rows;
        return view == "Up" ? ArenIdleGen.Up : ArenIdleGen.Side;
    }

    // Body: F0 windup = idle F1 (cloak flares), F1 strike = walk contact (1 px lunge + stride), F2 recover = idle F0.
    public static char[,] ArenAttackBody(string view, int f)
    {
        var b = ArenBase(view);
        if (f == 0) return ArenIdleGen.Frame(view, b, 1);
        if (f == 1) return ArenWalkGen.Frame(view, b, 0);
        return Grid(b);
    }

    public static Dictionary<char, string> Remap(Dictionary<char, string> pal, Dictionary<char, string> over)
    {
        var d = new Dictionary<char, string>(pal);
        foreach (var kv in over) d[kv.Key] = kv.Value;
        return d;
    }

    // Skin demo: "Crimson Order" = palette remap of the navy cloth (plume + cloak) only.
    public static readonly Dictionary<char, string> CrimsonSkin = new Dictionary<char, string>
    {
        { 'P', "6E1E2A" }, { 'p', "A8323C" }, { 'q', "D85A5A" }, { 'C', "4A1420" }, { 'c', "7A2030" }, { 'u', "A8323C" },
    };

    // ---------- Aren idle / walk (rest pose: sword lowered at the side, shield on guard) ----------
    // Base anchors per view; Walk contact frames (F0, F2) drop 1 px with the body, Idle F2 (helmet nod) does not move hands.
    public static Pose ArenRestPose(string view)
    {
        if (view == "Down") return new Pose { MainX = 10, MainY = 23, MainDir = 6, MainFront = true, OffX = 21, OffY = 23, Note = "kiem ha ben hong, khien thu" };
        if (view == "Up") return new Pose { MainX = 21, MainY = 23, MainDir = 6, MainFront = true, OffX = 10, OffY = 23, Note = "kiem ha ben hong (tay phai), mat trong khien" };
        return new Pose { MainX = 12, MainY = 23, MainDir = 5, MainFront = false, OffX = 21, OffY = 23, Note = "kiem tay xa, mui cheo ra sau; khien tay gan" };
    }

    public static Pose ArenMovePose(string anim, string view, int f)
    {
        var p = ArenRestPose(view);
        var q = new Pose { MainX = p.MainX, MainY = p.MainY, MainDir = p.MainDir, MainFront = p.MainFront, OffX = p.OffX, OffY = p.OffY, OffFront = p.OffFront, Note = p.Note };
        if (anim == "Walk")
        {
            if (f == 0 || f == 2) { q.MainY += 1; q.OffY += 1; q.Note += "; tiep dat (ha 1 px)"; }
            // arm swing: sword tip swings back / forward on the passing frames
            if (f == 1) q.MainX -= 1;
            else if (f == 3) q.MainX += 1;
        }
        return q;
    }

    public static char[,] ArenMoveBody(string anim, string view, int f)
    {
        var b = ArenBase(view);
        return anim == "Walk" ? ArenWalkGen.Frame(view, b, f) : ArenIdleGen.Frame(view, b, f);
    }

    // Gauntlet drawn over the sword grip so the lowered sword reads as held.
    public static readonly Spr Gauntlet = Hand("A0AABA", "6A7484", "1A1C24");

    public static Bitmap ArenMoveComposite(string anim, string view, int f, BladeDef sword, Func<string, Spr> shield, Dictionary<char, string> skin)
    {
        var c = HeroConceptGen.Roster.First(r => r.Id == "4");
        var pal = skin == null ? c.Pal : Remap(c.Pal, skin);
        var pose = ArenMovePose(anim, view, f);
        var body = Body(ArenMoveBody(anim, view, f), pal);
        var main = new Layer { S = Blade(sword, pose.MainDir), X = pose.MainX, Y = pose.MainY };
        var hand = new Layer { S = Gauntlet, X = pose.MainX - 1, Y = pose.MainY - 1 };
        var off = new Layer { S = shield(view), X = pose.OffX, Y = pose.OffY };
        var behind = new List<Layer>(); var front = new List<Layer>();
        if (pose.MainFront) { front.Add(main); front.Add(hand); } else { behind.Add(main); behind.Add(hand); }
        if (pose.OffFront) front.Add(off); else behind.Add(off);
        return Compose(body, behind, front);
    }

    public static void WriteMoveAnchors(string outDir)
    {
        var csv = new StringBuilder("character,anim,view,frame,timing_s,body_slice,main_grip_x,main_grip_y,main_pose,main_order,off_grip_x,off_grip_y,off_pose,off_order,note\n");
        foreach (var anim in new[] { "Idle", "Walk" })
            foreach (var v in new[] { "Down", "Up", "Side" })
                for (int f = 0; f < 4; f++)
                {
                    var p = ArenMovePose(anim, v, f);
                    string t = anim == "Walk" ? "0.125" : (f % 2 == 0 ? "0.25" : "0.15");
                    csv.AppendFormat("Aren,{0},{1},{2},{3},Aren_{0}_{1}_{2},{4},{5},{6},{7},{8},{9},shield_{1},{10},{11}\n",
                        anim, v, f, t, p.MainX, p.MainY, DirName[p.MainDir], p.MainFront ? "front" : "behind", p.OffX, p.OffY, p.OffFront ? "front" : "behind", p.Note);
                }
        System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "Aren_IdleWalk_anchors_v1.csv"), csv.ToString(), new UTF8Encoding(false));
    }

    // ---------- compositing ----------
    public class Layer { public Spr S; public int X, Y; }

    // Canvas 48x48, body cell drawn at (8,8). Order: behind layers, body, front layers (in list order).
    public static Bitmap Compose(Color?[,] body, List<Layer> behind, List<Layer> front)
    {
        var bmp = new Bitmap(48, 48, PixelFormat.Format32bppArgb);
        Action<Layer> draw = l =>
        {
            foreach (var kv in l.S.Px)
            {
                int x = 8 + l.X + kv.Key.X, y = 8 + l.Y + kv.Key.Y;
                if (x >= 0 && y >= 0 && x < 48 && y < 48) bmp.SetPixel(x, y, kv.Value);
            }
        };
        foreach (var l in behind) draw(l);
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) if (body[x, y].HasValue) bmp.SetPixel(8 + x, 8 + y, body[x, y].Value);
        foreach (var l in front) draw(l);
        return bmp;
    }

    public static Bitmap ArenAttackComposite(string view, int f, BladeDef sword, Func<string, Spr> shield, Dictionary<char, string> skin)
    {
        var c = HeroConceptGen.Roster.First(r => r.Id == "4");
        var pal = skin == null ? c.Pal : Remap(c.Pal, skin);
        var pose = ArenAttackPoses(view)[f];
        var body = Body(ArenAttackBody(view, f), pal);
        var main = new Layer { S = Blade(sword, pose.MainDir), X = pose.MainX, Y = pose.MainY };
        var off = new Layer { S = shield(view), X = pose.OffX, Y = pose.OffY };
        var behind = new List<Layer>(); var front = new List<Layer>();
        if (pose.OffFront) front.Add(off); else behind.Add(off);
        if (pose.MainFront) front.Add(main); else behind.Insert(0, main);
        return Compose(body, behind, front);
    }

    // ---------- exports ----------
    public static void SaveSpr(Spr s, string path, int cell)
    {
        var bmp = new Bitmap(cell, cell, PixelFormat.Format32bppArgb);
        foreach (var kv in s.Px)
        {
            int x = cell / 2 + kv.Key.X, y = cell / 2 + kv.Key.Y;
            if (x >= 0 && y >= 0 && x < cell && y < cell) bmp.SetPixel(x, y, kv.Value);
        }
        bmp.Save(path, ImageFormat.Png); bmp.Dispose();
    }

    public static string Run(string outDir)
    {
        var log = new StringBuilder();
        var c = HeroConceptGen.Roster.First(r => r.Id == "4");
        // body-only attack sheets
        foreach (var v in new[] { "Down", "Up", "Side" })
        {
            var sheet = new Bitmap(96, 32, PixelFormat.Format32bppArgb);
            for (int f = 0; f < 3; f++)
            {
                var body = Body(ArenAttackBody(v, f), c.Pal);
                for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) if (body[x, y].HasValue) sheet.SetPixel(f * 32 + x, y, body[x, y].Value);
            }
            sheet.Save(System.IO.Path.Combine(outDir, "Aren_Attack" + v + "_body_v1.png"), ImageFormat.Png); sheet.Dispose();
        }
        // weapon pose sheets: 8 directions, 48x48 cells, anchor (hand) at cell centre (24,24)
        foreach (var w in new[] { SteelLongsword, RuneBlade, Dagger })
        {
            var sheet = new Bitmap(48 * 8, 48, PixelFormat.Format32bppArgb);
            for (int d = 0; d < 8; d++)
                foreach (var kv in Blade(w, d).Px) sheet.SetPixel(d * 48 + 24 + kv.Key.X, 24 + kv.Key.Y, kv.Value);
            sheet.Save(System.IO.Path.Combine(outDir, "Weapon_" + w.Id + "_8dir.png"), ImageFormat.Png); sheet.Dispose();
        }
        foreach (var v in new[] { "Down", "Up", "Side" })
        {
            SaveSpr(HeaterShield(v), System.IO.Path.Combine(outDir, "Shield_heater_" + v + ".png"), 16);
            SaveSpr(RoundWoodShield(v), System.IO.Path.Combine(outDir, "Shield_roundwood_" + v + ".png"), 16);
        }
        // anchors
        var csv = new StringBuilder("character,view,frame,timing_s,body_slice,main_grip_x,main_grip_y,main_pose,main_order,off_grip_x,off_grip_y,off_pose,off_order,note\n");
        var times = new[] { "0.08", "0.10", "0.08" };
        foreach (var v in new[] { "Down", "Up", "Side" })
        {
            var ps = ArenAttackPoses(v);
            for (int f = 0; f < 3; f++)
            {
                var p = ps[f];
                csv.AppendFormat("Aren,{0},{1},{2},Aren_Attack_{0}_{1},{3},{4},{5},{6},{7},{8},shield_{0},{9},{10}\n",
                    v, f, times[f], p.MainX, p.MainY, DirName[p.MainDir], p.MainFront ? "front" : "behind", p.OffX, p.OffY, p.OffFront ? "front" : "behind", p.Note);
            }
        }
        System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "Aren_Attack_anchors_v1.csv"), csv.ToString(), new UTF8Encoding(false));
        log.Append("ok");
        return log.ToString();
    }
}
