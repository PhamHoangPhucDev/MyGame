using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

// Per-class VFX concept tiles (96x64 canvas, hero with rest gear at (8,8) => body cell (16..47, 16..47)).
// Pixel primitives only (no AA): crescents, rings (ground ellipses), streaks, arrows, orbs, stars, smoke, cracks, X slashes.
public static class VfxConceptGen
{
    public const int W = 96, H = 64;

    public class Canvas
    {
        public Color?[,] Px = new Color?[W, H];
        public void Set(int x, int y, Color c) { if (x >= 0 && y >= 0 && x < W && y < H) Px[x, y] = c; }
        public void SetIfEmpty(int x, int y, Color c) { if (x >= 0 && y >= 0 && x < W && y < H && !Px[x, y].HasValue) Px[x, y] = c; }
    }

    static Color C(string h) { return HeroConceptGen.Hex(h); }
    static double Ang(double dx, double dy) { return Math.Atan2(dy, dx) * 180.0 / Math.PI; }

    // palettes: core, light, mid, edge, accent
    public static readonly Dictionary<string, string[]> Pal = new Dictionary<string, string[]>
    {
        { "Aren",   new[] { "FFFFFF", "D8F0FF", "8CC8F0", "4A8AC8", "F0C850" } },
        { "Brom",   new[] { "FFF0C8", "F0A050", "C8692E", "7A4426", "D8D8E0" } },
        { "Lyra",   new[] { "F8FFE0", "B8F078", "6AC040", "2E7A2E", "F0D060" } },
        { "Selene", new[] { "FFFFFF", "FFB8F0", "D860C8", "7A3AA8", "B8D8FF" } },
        { "Vex",    new[] { "FFE0E0", "FF6060", "C82838", "6A1020", "C0C4D0" } },
    };

    // ---------- primitives ----------
    public static void Crescent(Canvas cv, string[] p, double cx, double cy, double R, double tipAng, double sweep, double thick, double yScale = 1.0)
    {
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                double dx = x + 0.5 - cx, dy = (y + 0.5 - cy) / yScale, d = Math.Sqrt(dx * dx + dy * dy);
                double rel = Ang(dx, dy) - (tipAng + sweep);
                while (rel > 180) rel -= 360; while (rel < -180) rel += 360;
                double t = -rel / sweep;
                if (t < 0 || t > 1) continue;
                double th = 1.0 + (thick - 1.0) * t;
                if (d > R + 0.5 || d < R - th) continue;
                Color c = d >= R - 0.5 ? (t > 0.35 ? C(p[0]) : C(p[1])) : (d >= R - 1.5 ? C(p[1]) : C(p[2]));
                if (t < 0.12) c = C(p[3]);
                cv.Set(x, y, c);
            }
    }

    public static void Ring(Canvas cv, Color c, double cx, double cy, double R, double th, double yScale, int dashEvery = 0)
    {
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                double dx = x + 0.5 - cx, dy = (y + 0.5 - cy) / yScale, d = Math.Sqrt(dx * dx + dy * dy);
                if (Math.Abs(d - R) > th / 2) continue;
                if (dashEvery > 0 && ((int)((Ang(dx, dy) + 360) / 12) % dashEvery) == 0) continue;
                cv.Set(x, y, c);
            }
    }

    public static void Line(Canvas cv, Color c, int x0, int y0, int x1, int y1, int width = 1)
    {
        int dx = Math.Abs(x1 - x0), dy = -Math.Abs(y1 - y0), sx = x0 < x1 ? 1 : -1, sy = y0 < y1 ? 1 : -1, err = dx + dy;
        while (true)
        {
            for (int a = 0; a < width; a++) for (int b = 0; b < width; b++) cv.Set(x0 + a - width / 2, y0 + b - width / 2, c);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }

    public static void Dots(Canvas cv, string[] cols, int seed, double cx, double cy, double rx, double ry, int n, int size = 1)
    {
        var rnd = new Random(seed);
        for (int i = 0; i < n; i++)
        {
            double a = rnd.NextDouble() * Math.PI * 2, r = Math.Sqrt(rnd.NextDouble());
            int x = (int)(cx + Math.Cos(a) * rx * r), y = (int)(cy + Math.Sin(a) * ry * r);
            var c = C(cols[rnd.Next(cols.Length)]);
            for (int a2 = 0; a2 < size; a2++) for (int b = 0; b < size; b++) cv.Set(x + a2, y + b, c);
        }
    }

    public static void Disc(Canvas cv, Color c, double cx, double cy, double r)
    {
        for (int y = (int)(cy - r - 1); y <= cy + r + 1; y++)
            for (int x = (int)(cx - r - 1); x <= cx + r + 1; x++)
                if ((x + 0.5 - cx) * (x + 0.5 - cx) + (y + 0.5 - cy) * (y + 0.5 - cy) <= r * r) cv.Set(x, y, c);
    }

    public static void Orb(Canvas cv, string[] p, double cx, double cy, double r, int trail)
    {
        for (int i = trail; i >= 1; i--) Disc(cv, C(i % 2 == 0 ? p[3] : p[2]), cx - i * 3, cy + (i % 2) * 0.5, Math.Max(0.6, r - i * 0.6));
        Disc(cv, C(p[3]), cx, cy, r + 0.6);
        Disc(cv, C(p[2]), cx, cy, r);
        Disc(cv, C(p[1]), cx - r * 0.25, cy - r * 0.25, r * 0.6);
        cv.Set((int)(cx - r * 0.35), (int)(cy - r * 0.35), C(p[0]));
    }

    public static void Star(Canvas cv, string[] p, int cx, int cy, int s)
    {
        for (int i = -s; i <= s; i++) { cv.Set(cx + i, cy, C(Math.Abs(i) < s / 2 ? p[0] : p[1])); cv.Set(cx, cy + i, C(Math.Abs(i) < s / 2 ? p[0] : p[1])); }
        for (int i = -s / 2; i <= s / 2; i++) { cv.SetIfEmpty(cx + i, cy + i, C(p[2])); cv.SetIfEmpty(cx + i, cy - i, C(p[2])); }
        cv.Set(cx, cy, C(p[0]));
    }

    public static void Arrow(Canvas cv, int x, int y, double ang, int len, string shaft, string head, string fletch)
    {
        double dx = Math.Cos(ang * Math.PI / 180), dy = Math.Sin(ang * Math.PI / 180);
        for (int i = 0; i < len; i++) cv.Set((int)Math.Round(x - dx * i), (int)Math.Round(y - dy * i), C(i < 2 ? head : (i >= len - 2 ? fletch : shaft)));
        cv.Set((int)Math.Round(x + dx), (int)Math.Round(y + dy), C(head));
        int fx = (int)Math.Round(x - dx * (len - 1)), fy = (int)Math.Round(y - dy * (len - 1));
        cv.Set(fx + (int)Math.Round(dy), fy - (int)Math.Round(dx), C(fletch)); cv.Set(fx - (int)Math.Round(dy), fy + (int)Math.Round(dx), C(fletch));
    }

    public static void Crack(Canvas cv, Color c, int seed, double cx, double cy, int n, int len, double yScale)
    {
        var rnd = new Random(seed);
        for (int k = 0; k < n; k++)
        {
            double a = (k + rnd.NextDouble() * 0.6) / n * Math.PI * 2, x = cx, y = cy;
            for (int i = 0; i < len; i++)
            {
                a += (rnd.NextDouble() - 0.5) * 0.8;
                x += Math.Cos(a); y += Math.Sin(a) * yScale;
                cv.Set((int)x, (int)y, c);
            }
        }
    }

    public static void XSlash(Canvas cv, string[] p, int cx, int cy, int s, int width)
    {
        Line(cv, C(p[2]), cx - s, cy - s, cx + s, cy + s, width + 1); Line(cv, C(p[2]), cx - s, cy + s, cx + s, cy - s, width + 1);
        Line(cv, C(p[1]), cx - s + 1, cy - s + 1, cx + s - 1, cy + s - 1, width); Line(cv, C(p[1]), cx - s + 1, cy + s - 1, cx + s - 1, cy - s + 1, width);
        Line(cv, C(p[0]), cx - s / 2, cy - s / 2, cx + s / 2, cy + s / 2, 1); Line(cv, C(p[0]), cx - s / 2, cy + s / 2, cx + s / 2, cy - s / 2, 1);
    }

    // ---------- hero bodies with rest gear ----------
    public static Bitmap Hero(string cls)
    {
        Func<string, EquipGen.Spr> heater = EquipGen.HeaterShield;
        if (cls == "Aren") return EquipGen.ArenMoveComposite("Idle", "Down", 0, EquipGen.SteelLongsword, heater, null);
        var id = cls == "Brom" ? "1" : cls == "Lyra" ? "2" : cls == "Selene" ? "3" : "5";
        var c = HeroConceptGen.Roster.First(r => r.Id == id);
        var body = EquipGen.Body(EquipGen.Grid(c.Rows), c.Pal);
        var behind = new List<EquipGen.Layer>(); var front = new List<EquipGen.Layer>();
        if (cls == "Brom")
        {
            behind.Add(new EquipGen.Layer { S = EquipGen.Hammer(2, 7, "7A4E2E", "A06A40", "8A94A4", "C8D0DC", "5A6070", "1C1A22"), X = 6, Y = 24 });
            front.Add(new EquipGen.Layer { S = EquipGen.BromShield(), X = 19, Y = 23 });
        }
        else if (cls == "Lyra")
        {
            front.Add(new EquipGen.Layer { S = EquipGen.Bow(), X = 21, Y = 22 });
            front.Add(new EquipGen.Layer { S = EquipGen.Hand("FCDCC0", "E8B494", "2A2218"), X = 20, Y = 22 });
        }
        else if (cls == "Selene") front.Add(new EquipGen.Layer { S = EquipGen.Staff(2, 14, "6A4428", "9A6A3C", "C04A8A", "FFD0F0", "7A2A5A", "1A1426"), X = 9, Y = 25 });
        else
        {
            front.Add(new EquipGen.Layer { S = EquipGen.Blade(EquipGen.Dagger, 5), X = 10, Y = 24 });
            front.Add(new EquipGen.Layer { S = EquipGen.Blade(EquipGen.Dagger, 7), X = 21, Y = 24 });
        }
        return EquipGen.Compose(body, behind, front);
    }

    // ---------- tiles ----------
    public static readonly Dictionary<string, string[]> Labels = new Dictionary<string, string[]>
    {
        { "Aren",   new[] { "R1 chem cheo", "R2 chem nguoc", "R3 dam thang (ket)", "2 Khien Dap", "C Chem Binh Minh" } },
        { "Brom",   new[] { "R1 vung bua", "R2 bo xuong", "R3 nen dat (ket)", "2 Nen Dat", "C Dong Dat" } },
        { "Lyra",   new[] { "R1 ban nhanh", "R2 ban nhanh", "R3 ten nap xuyen", "2 Ten Quat", "C Ten Xuyen Rung" } },
        { "Selene", new[] { "R1 cau phep", "R2 cau phep", "R3 no sao (ket)", "4 Vong Phu Chu", "C Mua Sao Bang" } },
        { "Vex",    new[] { "R1 dam trai", "R2 dam phai", "R4 chu X (ket)", "2 Buoc Bong", "C Doat Menh" } },
    };

    // fx drawn behind the hero when true (ground effects), otherwise on top
    public static Bitmap Tile(string cls, int i)
    {
        var p = Pal[cls];
        var under = new Canvas(); var over = new Canvas();
        if (cls == "Aren")
        {
            if (i == 0) Crescent(over, p, 34, 34, 12, 35, -150, 3.2);
            else if (i == 1) Crescent(over, p, 34, 34, 12, -35, 150, 3.2);
            else if (i == 2) { Line(over, C(p[2]), 40, 37, 74, 37, 3); Line(over, C(p[0]), 42, 37, 76, 37, 1); Dots(over, new[] { p[4], p[0] }, 3, 77, 37, 4, 3, 9); }
            else if (i == 3) { for (int k = 0; k < 3; k++) Crescent(over, new[] { p[0], p[4], "C8A040", "8A6A28" }, 38, 36, 9 + k * 5, 35, -70, 2.2 - k * 0.4); Dots(over, new[] { p[4], p[0] }, 4, 50, 36, 6, 6, 8); }
            else { Crescent(over, new[] { p[0], "FFF4C8", p[4], "C89A30" }, 30, 36, 22, 30, -170, 4.5); Dots(over, new[] { p[4], p[0] }, 5, 52, 40, 4, 4, 6); }
        }
        else if (cls == "Brom")
        {
            if (i == 0) { Crescent(over, p, 30, 36, 11, 15, -120, 3.4); Dots(over, new[] { p[1], p[2], p[3] }, 6, 44, 38, 5, 3, 10); }
            else if (i == 1) { Ring(under, C(p[2]), 50, 46, 7, 1.6, 0.45); Crack(under, C(p[3]), 20, 50, 46, 5, 6, 0.45); Dots(over, new[] { p[1], p[2], "8A8A8A" }, 7, 50, 40, 9, 6, 14, 2); Line(over, C(p[1]), 47, 26, 49, 42, 2); Line(over, C(p[0]), 48, 28, 49, 40, 1); }
            else if (i == 2) { Ring(under, C(p[2]), 32, 46, 12, 2, 0.42); Crack(under, C(p[3]), 8, 32, 46, 6, 9, 0.45); Dots(over, new[] { p[4], p[1] }, 9, 32, 42, 12, 4, 10); }
            else if (i == 3) { Ring(under, C(p[2]), 32, 46, 18, 2.4, 0.42); Ring(under, C(p[1]), 32, 46, 12, 1.5, 0.42); Crack(under, C(p[3]), 10, 32, 46, 8, 14, 0.45); Dots(over, new[] { p[2], p[3], "8A8A8A" }, 11, 32, 34, 16, 10, 16, 2); }
            else { for (int k = 0; k < 3; k++) Ring(under, C(k == 0 ? p[1] : (k == 1 ? p[2] : p[3])), 32, 46, 8 + k * 7, 1.8, 0.42, k == 2 ? 3 : 0); Crack(under, C(p[3]), 12, 32, 46, 10, 18, 0.45); Dots(over, new[] { p[2], "8A8A8A" }, 13, 32, 36, 22, 9, 14, 2); }
        }
        else if (cls == "Lyra")
        {
            string sh = "8A5A34", hd = "D8DCE4", fl = p[2];
            if (i == 0) { Line(over, C(p[3]), 38, 34, 50, 34, 1); Line(over, C(p[1]), 44, 34, 50, 34, 1); Arrow(over, 58, 34, 0, 9, sh, hd, fl); }
            else if (i == 1) { Line(over, C(p[3]), 50, 31, 64, 30, 1); Line(over, C(p[1]), 58, 31, 64, 30, 1); Arrow(over, 72, 30, -4, 9, sh, hd, fl); }
            else if (i == 2) { Line(over, C(p[3]), 38, 34, 74, 34, 3); Line(over, C(p[1]), 40, 34, 74, 34, 1); Arrow(over, 82, 34, 0, 10, "F0D060", p[0], p[1]); Dots(over, new[] { p[1], p[2], p[4] }, 14, 60, 34, 16, 4, 12); }
            else if (i == 3) { foreach (var a in new[] { -30, -15, 0, 15, 30 }) { double r = a * Math.PI / 180; int ex = 38 + (int)(Math.Cos(r) * 28), ey = 34 + (int)(Math.Sin(r) * 28); Line(over, C(p[2]), 38 + (int)(Math.Cos(r) * 10), 34 + (int)(Math.Sin(r) * 10), ex - (int)(Math.Cos(r) * 8), ey - (int)(Math.Sin(r) * 8), 1); Arrow(over, ex, ey, a, 8, sh, hd, fl); } }
            else { Line(over, C(p[3]), 38, 34, 95, 34, 5); Line(over, C(p[2]), 38, 34, 95, 34, 3); Line(over, C(p[0]), 38, 34, 95, 34, 1); for (int x = 40; x < 95; x += 6) { over.Set(x, 30 + (x / 6) % 2, C(p[4])); over.Set(x + 3, 38 - (x / 6) % 2, C(p[1])); } }
        }
        else if (cls == "Selene")
        {
            if (i == 0) { Orb(over, p, 56, 32, 3, 4); Dots(over, new[] { p[0], p[1] }, 15, 46, 32, 6, 3, 5); }
            else if (i == 1) { Orb(over, p, 72, 28, 3, 4); Dots(over, new[] { p[0], p[1] }, 16, 62, 29, 6, 3, 5); }
            else if (i == 2) { Ring(over, C(p[2]), 74, 34, 7, 1.2, 1.0); Star(over, p, 74, 34, 8); Dots(over, new[] { p[0], p[4], p[1] }, 17, 74, 34, 12, 12, 14); }
            else if (i == 3) { Ring(under, C(p[2]), 68, 46, 15, 1.4, 0.5); Ring(under, C(p[3]), 68, 46, 11, 1.2, 0.5, 2); for (int k = 0; k < 8; k++) { double a = k * Math.PI / 4; int x = 68 + (int)(Math.Cos(a) * 13), y = 46 + (int)(Math.Sin(a) * 6.5); under.Set(x, y, C(p[1])); under.Set(x + 1, y, C(p[0])); } }
            else { foreach (var m in new[] { new[] { 60, 46 }, new[] { 76, 40 }, new[] { 88, 52 } }) { Line(over, C(p[3]), m[0] + 14, m[1] - 22, m[0], m[1], 2); Line(over, C(p[1]), m[0] + 8, m[1] - 13, m[0], m[1], 1); Star(over, p, m[0], m[1], 3); } }
        }
        else // Vex
        {
            if (i == 0) { Line(over, C(p[2]), 38, 26, 54, 42, 2); Line(over, C(p[0]), 42, 30, 52, 40, 1); }
            else if (i == 1) { Line(over, C(p[2]), 38, 42, 54, 26, 2); Line(over, C(p[0]), 42, 38, 52, 28, 1); }
            else if (i == 2) { XSlash(over, p, 52, 34, 9, 2); Dots(over, new[] { p[1], p[0], p[4] }, 18, 52, 34, 12, 10, 12); }
            else if (i == 3) { foreach (var s in new[] { new[] { 22, 42, 6 }, new[] { 30, 36, 7 }, new[] { 40, 40, 6 }, new[] { 33, 46, 5 }, new[] { 26, 30, 4 } }) { Disc(over, C("3A3444"), s[0], s[1], s[2]); Disc(over, C("6A6476"), s[0] - 1, s[1] - 1, s[2] - 2.5); } Dots(over, new[] { "8A8496", "3A3444" }, 21, 32, 38, 14, 10, 14); over.Set(29, 35, C(p[1])); over.Set(33, 35, C(p[1])); over.Set(29, 36, C(p[0])); over.Set(33, 36, C(p[0])); }
            else { for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) under.Set(x, y, Color.FromArgb(255, 40, 30, 40)); XSlash(over, p, 62, 34, 15, 3); Dots(over, new[] { p[1], p[0] }, 19, 62, 34, 18, 14, 18); }
        }
        // compose: grass, under fx, hero, over fx
        var bmp = new Bitmap(W, H, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.Clear(Color.FromArgb(255, 0x74, 0xA5, 0x87));
            using (var sh = new SolidBrush(Color.FromArgb(70, 0, 0, 0))) g.FillEllipse(sh, 26, 44, 12, 3);
        }
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) if (under.Px[x, y].HasValue) bmp.SetPixel(x, y, under.Px[x, y].Value);
        var hero = Hero(cls);
        for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++) { var c = hero.GetPixel(x, y); if (c.A > 0) bmp.SetPixel(8 + x, 8 + y, c); }
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) if (over.Px[x, y].HasValue) bmp.SetPixel(x, y, over.Px[x, y].Value);
        return bmp;
    }
}
