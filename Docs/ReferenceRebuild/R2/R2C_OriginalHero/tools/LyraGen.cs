using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

// Lyra (elf archer) — CANDIDATE: Up / Side base frames (Down = approved roster v3), 8-direction bow with string draw,
// nock / draw / release poses, arrow projectiles, charged-shot key frame. Same 14x21 template, 32x32 cell, sole on row 29.
public static class LyraGen
{
    const int W = 14, H = 21;
    public static string[] Down { get { return HeroConceptGen.Roster.First(r => r.Id == "2").Rows; } }
    public static Dictionary<char, string> Pal { get { return HeroConceptGen.Roster.First(r => r.Id == "2").Pal; } }

    // Back: blonde hair with the leaf clip on the left, ear tips at both edges, quiver slung diagonally across the
    // back (fletching over the right shoulder), braid hanging on the left, green skirt points, boots.
    public static readonly string[] Up =
    {
        "..FfKLXXLK....",
        "..FfKHLLHHK...",
        "..KHHLXXLHHK..",
        ".KHHHLLXLLHHK.",
        ".KHHHLLLLHHHK.",
        "KDHHHHHHHHHHDK",
        "KDHHLHHHHLHHDK",
        "sKDHHHLHHHHDKs",
        "SKDHHHHHHHaaKS",
        ".KDHHLHHHHaaK.",
        ".KDHHHHHHHqqK.",
        ".kLHDHHHHHGVk.",
        "kLHgOoooOGVgk.",
        "kHDgBbbbGVbgk.",
        "kLHglbbGVlbgk.",
        "kDkGgoOBVogGk.",
        "..kGgoooogGk..",
        ".kVGgOooOgGVk.",
        "..Vksskssk.V..",
        "...kbBkbBk....",
        "..kbBBkbBBk...",
    };

    // 3/4 facing right: face and eyes shifted right, hair mass + ear tip on the left, quiver fletching over the back
    // shoulder, braid falling over the chest, toes pointing right.
    public static readonly string[] Side =
    {
        "....KLXXLKfF..",
        "...KHHLLHHKfF.",
        "..KHHLXXLHHHK.",
        ".KHHLLXLLLHHHK",
        ".KHHHLLLLHHHHK",
        "KDHHHHHHHHHHHK",
        "KDHHHHHHHsHHsK",
        "sKDHHHssssssHK",
        "SKDHHssseesseK",
        "aKDHHsssEwssEK",
        "aKDHHssssspssK",
        "qGkDHHSsssSk..",
        "GVkgOooHLOgk..",
        "GVkgbBBDHBgk..",
        "GVkgblbHLbgk..",
        "GVkGgoOOogGk..",
        "BVkGgoooogGk..",
        ".kVGgOooOgGVk.",
        "..V.kssksskV..",
        "....kBbkBbk...",
        "....kBBbkBBbk.",
    };

    public static string[] Base(string v) { return v == "Down" ? Down : v == "Up" ? Up : Side; }

    static Color C(string h) { return HeroConceptGen.Hex(h); }

    static void Line(EquipGen.Spr s, Point a, Point b, string col, bool keep)
    {
        int n = Math.Max(Math.Abs(b.X - a.X), Math.Abs(b.Y - a.Y));
        for (int i = 0; i <= n; i++)
        {
            double t = n == 0 ? 0 : i / (double)n;
            var q = new Point((int)Math.Round(a.X + (b.X - a.X) * t), (int)Math.Round(a.Y + (b.Y - a.Y) * t));
            if (keep && s.Px.ContainsKey(q)) continue;
            s.Set(q.X, q.Y, C(col));
        }
    }

    // ---------- bow: grip = anchor (0,0) = hand; wood bulges toward the aim dir, string behind it ----------
    // draw = how far the string middle is pulled back (0 rest .. 4 full); arrow = nocked arrow on the string.
    public static EquipGen.Spr Bow(int dir, int draw, bool arrow)
    {
        var d = EquipGen.Dir[dir]; var p = new Point(-d.Y, d.X);
        bool diag = d.X != 0 && d.Y != 0; int L = diag ? 4 : 5; int bulge; Shape(dir, ref draw, out bulge);
        var s = new EquipGen.Spr(); Point prev = Point.Empty;
        for (int t = -L; t <= L; t++)
        {
            int back = (int)Math.Round(bulge * t * t / (double)(L * L));
            var q = new Point(p.X * t - d.X * back, p.Y * t - d.Y * back);
            string col = Math.Abs(t) <= 1 ? "5E8A34" : Math.Abs(t) == L ? "B07A48" : "8A5A34";
            if (t > -L) Line(s, prev, q, col, false); else s.Set(q.X, q.Y, C(col));
            prev = q;
        }
        var A = new Point(p.X * L - d.X * bulge, p.Y * L - d.Y * bulge);
        var B = new Point(-p.X * L - d.X * bulge, -p.Y * L - d.Y * bulge);
        var M = new Point(-d.X * (bulge + draw), -d.Y * (bulge + draw));
        Line(s, A, M, "EEE8DC", true); Line(s, M, B, "EEE8DC", true);
        if (arrow)
        {
            int k0 = -(bulge + draw);
            for (int k = k0; k <= 3; k++)
            {
                string col = k >= 2 ? "D8DCE4" : k <= k0 + 1 ? "6AC040" : "B07A48";
                s.Set(d.X * k, d.Y * k, C(col));
            }
        }
        return OutlineOutside(s, C("2A2218"));
    }

    // Outline only the empty pixels reachable from outside, so the gap between wood and string stays see-through.
    static EquipGen.Spr OutlineOutside(EquipGen.Spr s, Color k)
    {
        int x0 = s.Px.Keys.Min(q => q.X) - 1, x1 = s.Px.Keys.Max(q => q.X) + 1, y0 = s.Px.Keys.Min(q => q.Y) - 1, y1 = s.Px.Keys.Max(q => q.Y) + 1;
        var outside = new HashSet<Point>(); var st = new Stack<Point>(); st.Push(new Point(x0, y0));
        while (st.Count > 0)
        {
            var q = st.Pop();
            if (q.X < x0 || q.X > x1 || q.Y < y0 || q.Y > y1 || outside.Contains(q) || s.Px.ContainsKey(q)) continue;
            outside.Add(q);
            st.Push(new Point(q.X + 1, q.Y)); st.Push(new Point(q.X - 1, q.Y)); st.Push(new Point(q.X, q.Y + 1)); st.Push(new Point(q.X, q.Y - 1));
        }
        var o = new EquipGen.Spr();
        foreach (var kv in s.Px) o.Px[kv.Key] = kv.Value;
        foreach (var q in s.Px.Keys.ToList())
            foreach (var n in new[] { new Point(q.X + 1, q.Y), new Point(q.X - 1, q.Y), new Point(q.X, q.Y + 1), new Point(q.X, q.Y - 1) })
                if (!o.Px.ContainsKey(n) && outside.Contains(n)) o.Px[n] = k;
        return o;
    }

    static void Shape(int dir, ref int draw, out int bulge) { var d = EquipGen.Dir[dir]; bool diag = d.X != 0 && d.Y != 0; bulge = diag ? 2 : 3; if (diag) draw = (int)Math.Round(draw * 0.7); }

    public static Point StringMid(int dir, int draw) { var d = EquipGen.Dir[dir]; int b; Shape(dir, ref draw, out b); return new Point(-d.X * (b + draw), -d.Y * (b + draw)); }

    public static EquipGen.Spr Hand { get { return EquipGen.Hand("FCDCC0", "E8B494", "2A2218"); } }

    // ---------- poses (32x32 cell coords, right-facing preview; Side is mirrored on export like Aren) ----------
    // X,Y = grip (bow hand). Shot poses also carry the two raised arms (shoulder -> hand), drawn into the body frame;
    // the hanging arms of the idle body are erased for those frames.
    public class P
    {
        public int X, Y, Dir, Draw; public bool Arrow, Front = true;
        public bool Shot, RestArm, OnBack; public int HandDraw;        // draw hand sits at the string middle for this draw amount
        public Point BowShoulder, DrawShoulder; public bool BowArmFront = true, DrawArmFront = true;
    }

    // Rest: arm hanging, fist at the end of the sleeve, bow upright beside the body — wood OUTSIDE, string toward the body.
    public static P Rest(string v)
    {
        // Idle / Walk: bow slung diagonally on the back, hands free (body arms untouched); drawn out only on Attack.
        // Down: behind the body, upper limb over the right shoulder. Up: on the back, crossing the quiver (X).
        // Side: behind the body, upper limb over the back shoulder.
        // Bow centre (grip - dir) sits on the spine: x 16, y 22 (Down/Up); Side = the back edge (x 9) so half shows.
        if (v == "Down") return new P { X = 17, Y = 23, Dir = 7, Front = false, OnBack = true };
        if (v == "Up") return new P { X = 15, Y = 23, Dir = 5, Front = true, OnBack = true };
        return new P { X = 8, Y = 23, Dir = 5, Front = false, OnBack = true };
    }

    // f: 0 nock, 1 full draw, 2 release (string straight, draw hand stays back at the anchor point)
    public static P Shot(string v, int f)
    {
        int full = 4;
        int draw = f == 0 ? 1 : f == 1 ? full : 0; int hd = f == 0 ? 1 : full;
        if (v == "Down") // aiming at the camera: bow arm foreshortened, string pulled to the chin
            return new P { X = 18, Y = 27, Dir = 6, Draw = draw, Arrow = f < 2, Shot = true, HandDraw = hd, BowShoulder = new Point(19, 21), DrawShoulder = new Point(12, 21) };
        if (v == "Up") // aiming away: bow raised above the head, draw hand hidden behind the head
            return new P { X = 16, Y = 6, Dir = 2, Draw = draw, Arrow = f < 2, Front = false, Shot = true, HandDraw = hd, BowShoulder = new Point(12, 21), DrawShoulder = new Point(20, 21), BowArmFront = false, DrawArmFront = false };
        // Side (right): bow arm straight forward at shoulder height, string to the cheek, draw elbow back
        return new P { X = 25, Y = 21, Dir = 0, Draw = draw, Arrow = f < 2, Shot = true, HandDraw = hd, BowShoulder = new Point(19, 21), DrawShoulder = new Point(12, 22) };
    }

    static readonly int[] ArmRows = { 12, 13, 14 };

    // both: erase both hanging arms (shot); otherwise only the bow-side arm (rest: Down/Side viewer-right, Up viewer-left)
    public static char[,] ShotBody(string v, bool both = true)
    {
        var g = EquipGen.Grid(Base(v));
        int a = 3, b = v == "Up" ? 11 : 10;
        foreach (int r in ArmRows)
        {
            if (both || v == "Up") g[a, r] = g[a + 1, r];
            if (both || v != "Up") g[b, r] = g[b - 1, r];
        }
        return g;
    }

    // Down view: the slung bow is hidden by the body, so its upper limb end peeks over the right shoulder (neck notch
    // at x 22). Drawn BEHIND the body: only the part outside the silhouette shows. Absolute 32x32 cell coords.
    public static EquipGen.Spr BackTip()
    {
        var s = new EquipGen.Spr();
        var wood = new[] { new Point(19, 22), new Point(20, 21), new Point(21, 20), new Point(22, 19), new Point(22, 18), new Point(23, 17), new Point(23, 16) };
        foreach (var q in wood) s.Set(q.X, q.Y, C("8A5A34"));
        s.Set(24, 15, C("B07A48")); s.Set(23, 15, C("EEE8DC")); s.Set(22, 16, C("EEE8DC"));
        return OutlineOutside(s, C("2A2218"));
    }

    public static EquipGen.Spr Arm(Point from, Point to)
    {
        var s = new EquipGen.Spr();
        int n = Math.Max(Math.Abs(to.X - from.X), Math.Abs(to.Y - from.Y));
        for (int i = 0; i <= n; i++)
        {
            double t = n == 0 ? 0 : i / (double)n;
            s.Set((int)Math.Round(from.X + (to.X - from.X) * t), (int)Math.Round(from.Y + (to.Y - from.Y) * t), C(i <= n / 2 ? "8AB04A" : "5E8A34"));
        }
        return OutlineOutside(s, C("2A2218"));
    }

    public static Bitmap Compose(string v, P p, bool mirror)
    {
        var body = EquipGen.Body(p.Shot ? ShotBody(v) : p.RestArm ? ShotBody(v, false) : EquipGen.Grid(Base(v)), Pal);
        var behind = new List<EquipGen.Layer>(); var front = new List<EquipGen.Layer>();
        var grip = new Point(p.X, p.Y);
        if (p.Shot)
        {
            var m = StringMid(p.Dir, p.HandDraw); var dh = new Point(p.X + m.X, p.Y + m.Y);
            (p.BowArmFront ? front : behind).Add(new EquipGen.Layer { S = Arm(p.BowShoulder, grip) });
            (p.DrawArmFront ? front : behind).Add(new EquipGen.Layer { S = Arm(p.DrawShoulder, dh) });
            (p.Front ? front : behind).Add(new EquipGen.Layer { S = Bow(p.Dir, p.Draw, p.Arrow), X = p.X, Y = p.Y });
            (p.DrawArmFront ? front : behind).Add(new EquipGen.Layer { S = Hand, X = dh.X - 1, Y = dh.Y - 1 });
            (p.BowArmFront ? front : behind).Add(new EquipGen.Layer { S = Hand, X = p.X - 1, Y = p.Y - 1 });
        }
        else if (p.OnBack)
        {
            (p.Front ? front : behind).Add(new EquipGen.Layer { S = Bow(p.Dir, 0, false), X = p.X, Y = p.Y });
            if (v == "Down") behind.Add(new EquipGen.Layer { S = BackTip() });
        }
        else
        {
            if (p.RestArm) front.Add(new EquipGen.Layer { S = Arm(p.BowShoulder, new Point(p.X - 1, p.Y - 1)) });
            front.Add(new EquipGen.Layer { S = Bow(p.Dir, p.Draw, p.Arrow), X = p.X, Y = p.Y });
            front.Add(new EquipGen.Layer { S = Hand, X = p.X - 1, Y = p.Y - 1 });
        }
        var bmp = EquipGen.Compose(body, behind, front);
        if (!mirror) return bmp;
        var o = new Bitmap(48, 48, PixelFormat.Format32bppArgb);
        for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++) o.SetPixel(47 - x, y, bmp.GetPixel(x, y));
        return o;
    }

    // ---------- arrow projectiles (16x16, pivot 8,8, pointing right; 8 directions are drawn, not rotated) ----------
    public static readonly string[] FX = { "F8FFE0", "B8F078", "6AC040", "2E7A2E", "F0D060" };

    public static Bitmap Arrow(int dir, bool charged, int frame)
    {
        var cv = new ArenKitSheets.Cv(16, 16, 0, 0);
        var d = EquipGen.Dir[dir]; bool diag = d.X != 0 && d.Y != 0; int len = diag ? 7 : 10;
        int hx = 8 + d.X * (diag ? 3 : 5), hy = 8 + d.Y * (diag ? 3 : 5);
        if (charged)
        {
            // soft green halo + trailing spark that flickers between frames
            for (int k = 0; k < len; k++)
            {
                int x = hx - d.X * k, y = hy - d.Y * k; var pp = diag ? new Point(1, 0) : new Point(-d.Y, d.X);
                if (k >= 2 && k < len - 1) { cv.Set(x + pp.X, y + pp.Y, C(FX[1])); cv.Set(x - pp.X, y - pp.Y, C(FX[1])); }
            }
            cv.Set(hx + d.X, hy + d.Y, C(FX[0]));
            int tk = len - 1 + (frame == 0 ? 1 : 2); cv.Set(hx - d.X * tk, hy - d.Y * tk, C(frame == 0 ? FX[4] : FX[1]));
        }
        for (int k = 0; k < len; k++)
        {
            string col = k < 2 ? (charged ? FX[0] : "D8DCE4") : k >= len - 2 ? (charged ? FX[4] : FX[2]) : (charged ? "F0D060" : "B07A48");
            cv.Set(hx - d.X * k, hy - d.Y * k, C(col));
        }
        var bmp = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
        for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) if (cv.Px[x, y].HasValue) bmp.SetPixel(x, y, cv.Px[x, y].Value);
        return bmp;
    }

    // ---------- key frame: charged shot (R3) leaving the bow, 112x48, hero cell at (0,0) ----------
    public static Bitmap ChargedTile()
    {
        var bmp = new Bitmap(112, 48, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp)) g.Clear(Color.FromArgb(255, 0x74, 0xA5, 0x87));
        var fx = new ArenKitSheets.Cv(112, 48, 0, 0);
        int y0 = 29; // arrow height in tile = hero cell y 21 + 8
        // trail: 3-px dark-green band -> 1-px light core, thinning toward the bow
        ArenKitSheets.Line(fx, C(FX[3]), 40, y0, 92, y0, 3);
        ArenKitSheets.Line(fx, C(FX[2]), 44, y0, 94, y0, 2);
        ArenKitSheets.Line(fx, C(FX[0]), 60, y0, 95, y0, 1);
        // leaves peeling off the trail
        var leaves = new[] { new[] { 50, -4 }, new[] { 58, 4 }, new[] { 67, -5 }, new[] { 76, 4 }, new[] { 84, -3 } };
        foreach (var l in leaves)
        {
            int x = l[0], y = y0 + l[1];
            fx.Set(x, y, C(FX[1])); fx.Set(x + 1, y, C(FX[2])); fx.Set(x + 1, y + (l[1] < 0 ? 1 : -1), C(FX[3]));
        }
        fx.Set(70, y0 - 3, C(FX[4])); fx.Set(81, y0 + 3, C(FX[4])); fx.Set(90, y0 - 2, C(FX[4]));
        for (int y = 0; y < 48; y++) for (int x = 0; x < 112; x++) if (fx.Px[x, y].HasValue) bmp.SetPixel(x, y, fx.Px[x, y].Value);
        var hero = Compose("Side", Shot("Side", 2), false);
        for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++) { var c = hero.GetPixel(x, y); if (c.A > 0) bmp.SetPixel(x, y, c); }
        var a = Arrow(0, true, 0);
        for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) { var c = a.GetPixel(x, y); if (c.A > 0) bmp.SetPixel(88 + x, y0 - 8 + y, c); }
        return bmp;
    }
}
