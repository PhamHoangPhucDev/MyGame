using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;

// Aren designed kit — production VFX sprite sheets (approved candidate: aren_kit/AREN_KIT_VFX_CANDIDATE.png).
// Body-anchored sheets: each cell has a fixed pivot P = Aren body pivot; body cell (32x32, pivot (16,30)) sits at
// offset (P.x-16, P.y-30) inside the cell. Side frames face LEFT (like the body sheet). Views order: Down, Up, Side.
public static class ArenKitSheets
{
    public static readonly string[] Ice = { "FFFFFF", "D8F0FF", "8CC8F0", "4A8AC8" };
    public static readonly string[] Gold = { "FFFFFF", "FFF4C8", "F0C850", "C89A30" };
    static readonly string[] Dust = { "E8DCC8", "C8B8A0", "A89880" };
    static readonly string[] Views = { "Down", "Up", "Side" };
    static Color C(string h) { return HeroConceptGen.Hex(h); }
    static double Ang(double dx, double dy) { return Math.Atan2(dy, dx) * 180.0 / Math.PI; }

    // ---------- arbitrary-size pixel canvas (body coordinates + offset) ----------
    public class Cv
    {
        public int W, H, Ox, Oy;
        public Color?[,] Px;
        public Cv(int w, int h, int ox, int oy) { W = w; H = h; Ox = ox; Oy = oy; Px = new Color?[w, h]; }
        public void Set(double bx, double by, Color c) { int x = (int)Math.Floor(bx) + Ox, y = (int)Math.Floor(by) + Oy; if (x > 0 && y > 0 && x < W - 1 && y < H - 1) Px[x, y] = c; }
        public void SetIfEmpty(double bx, double by, Color c) { int x = (int)Math.Floor(bx) + Ox, y = (int)Math.Floor(by) + Oy; if (x > 0 && y > 0 && x < W - 1 && y < H - 1 && !Px[x, y].HasValue) Px[x, y] = c; }
    }

    public static void Crescent(Cv cv, string[] p, double cx, double cy, double R, double tipAng, double sweep, double thick, double yScale = 1.0, double tFrom = 0.0, bool faded = false)
    {
        for (int y = 0; y < cv.H; y++)
            for (int x = 0; x < cv.W; x++)
            {
                double bx = x - cv.Ox + 0.5, by = y - cv.Oy + 0.5;
                double dx = bx - cx, dy = (by - cy) / yScale, d = Math.Sqrt(dx * dx + dy * dy);
                double rel = Ang(dx, dy) - (tipAng + sweep);
                while (rel > 180) rel -= 360; while (rel < -180) rel += 360;
                double t = -rel / sweep;
                if (t < tFrom || t > 1) continue;
                double th = 1.0 + (thick - 1.0) * t;
                if (d > R + 0.5 || d < R - th) continue;
                Color c;
                if (faded) c = d >= R - 0.5 ? C(p[2]) : C(p[3]);
                else
                {
                    c = d >= R - 0.5 ? (t > 0.35 ? C(p[0]) : C(p[1])) : (d >= R - 1.5 ? C(p[1]) : C(p[2]));
                    if (t < tFrom + 0.12) c = C(p[3]);
                }
                if (x > 0 && y > 0 && x < cv.W - 1 && y < cv.H - 1) cv.Px[x, y] = c;
            }
    }

    public static void Ring(Cv cv, Color c, double cx, double cy, double R, double th, double yScale, Func<double, double, bool> keep)
    {
        for (int y = 0; y < cv.H; y++)
            for (int x = 0; x < cv.W; x++)
            {
                double bx = x - cv.Ox + 0.5, by = y - cv.Oy + 0.5;
                double dx = bx - cx, dy = (by - cy) / yScale, d = Math.Sqrt(dx * dx + dy * dy);
                if (Math.Abs(d - R) > th / 2) continue;
                if (keep != null && !keep(bx, by)) continue;
                if (x > 0 && y > 0 && x < cv.W - 1 && y < cv.H - 1) cv.Px[x, y] = c;
            }
    }

    public static void Line(Cv cv, Color c, double x0d, double y0d, double x1d, double y1d, int width = 1)
    {
        int x0 = (int)Math.Round(x0d), y0 = (int)Math.Round(y0d), x1 = (int)Math.Round(x1d), y1 = (int)Math.Round(y1d);
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

    public static void Disc(Cv cv, Color c, double cx, double cy, double r)
    {
        for (int y = (int)(cy - r - 1); y <= cy + r + 1; y++)
            for (int x = (int)(cx - r - 1); x <= cx + r + 1; x++)
                if ((x + 0.5 - cx) * (x + 0.5 - cx) + (y + 0.5 - cy) * (y + 0.5 - cy) <= r * r) cv.Set(x, y, c);
    }

    public static void Star(Cv cv, string[] p, double cxd, double cyd, int s)
    {
        int cx = (int)Math.Round(cxd), cy = (int)Math.Round(cyd);
        for (int i = -s; i <= s; i++) { cv.Set(cx + i, cy, C(Math.Abs(i) <= s / 2 ? p[0] : p[1])); cv.Set(cx, cy + i, C(Math.Abs(i) <= s / 2 ? p[0] : p[1])); }
        for (int i = -s / 2; i <= s / 2; i++) { cv.SetIfEmpty(cx + i, cy + i, C(p[2])); cv.SetIfEmpty(cx + i, cy - i, C(p[2])); }
        cv.Set(cx, cy, C(p[0]));
    }

    public static void Dots(Cv cv, string[] cols, int seed, double cx, double cy, double rx, double ry, int n, int size = 1)
    {
        var rnd = new Random(seed);
        for (int i = 0; i < n; i++)
        {
            double a = rnd.NextDouble() * Math.PI * 2, r = Math.Sqrt(rnd.NextDouble());
            double x = cx + Math.Cos(a) * rx * r, y = cy + Math.Sin(a) * ry * r;
            var c = C(cols[rnd.Next(cols.Length)]);
            for (int a2 = 0; a2 < size; a2++) for (int b = 0; b < size; b++) cv.Set(x + a2, y + b, c);
        }
    }

    // ---------- sheet writing ----------
    public class SheetInfo { public string File; public int CellW, CellH, Cols, Rows, PivotX, PivotY; public string Layout; }
    public static readonly List<SheetInfo> Manifest = new List<SheetInfo>();

    static void Save(string dir, string file, List<Cv> cells, int cols, int pivotX, int pivotY, string layout)
    {
        int cw = cells[0].W, ch = cells[0].H, rows = (cells.Count + cols - 1) / cols;
        var bmp = new Bitmap(cw * cols, ch * rows, PixelFormat.Format32bppArgb);
        for (int i = 0; i < cells.Count; i++)
        {
            int ox = (i % cols) * cw, oy = (i / cols) * ch;
            for (int y = 0; y < ch; y++) for (int x = 0; x < cw; x++) if (cells[i].Px[x, y].HasValue) bmp.SetPixel(ox + x, oy + y, cells[i].Px[x, y].Value);
        }
        bmp.Save(Path.Combine(dir, file), ImageFormat.Png); bmp.Dispose();
        Manifest.Add(new SheetInfo { File = file, CellW = cw, CellH = ch, Cols = cols, Rows = rows, PivotX = pivotX, PivotY = pivotY, Layout = layout });
    }

    public static Cv BodyCell(int w, int h, int px, int py) { return new Cv(w, h, px - 16, py - 30); }

    // ---------- 1. Thrust (R3 finisher) 80x80, pivot (40,50) ----------
    static Cv Thrust(string v, int f)
    {
        var cv = BodyCell(80, 80, 40, 50);
        double x0, y0, dx, dy; int len = 26;
        if (v == "Down") { x0 = 16; y0 = 27; dx = 0; dy = 1; }
        else if (v == "Up") { x0 = 16; y0 = 13; dx = 0; dy = -1; }
        else { x0 = 6; y0 = 24; dx = -1; dy = 0; }
        double s = f == 0 ? 0 : (f == 1 ? 6 : 12);
        double e = f == 0 ? len : (f == 1 ? len + 1 : len + 2);
        double ax = x0 + dx * s, ay = y0 + dy * s, bx = x0 + dx * e, by = y0 + dy * e;
        if (f == 0)
        {
            Line(cv, C(Ice[3]), ax, ay, bx, by, 5); Line(cv, C(Ice[2]), ax, ay, bx, by, 3); Line(cv, C(Ice[0]), ax + dx * 2, ay + dy * 2, bx + dx, by + dy, 1);
            for (int k = 4; k < len - 2; k += 7) { cv.Set(x0 + dx * k + dy * 3, y0 + dy * k + dx * 3, C(Ice[1])); cv.Set(x0 + dx * (k + 3) - dy * 3, y0 + dy * (k + 3) - dx * 3, C(Ice[1])); }
            Star(cv, Gold, bx + dx * 2, by + dy * 2, 3);
            Dots(cv, new[] { Gold[2], Gold[1] }, 11 + (int)dx * 3 + (int)dy * 7, bx + dx * 2, by + dy * 2, 5, 5, 8);
        }
        else if (f == 1)
        {
            Line(cv, C(Ice[3]), ax, ay, bx, by, 3); Line(cv, C(Ice[1]), ax + dx, ay + dy, bx, by, 1);
            Star(cv, new[] { Gold[1], Gold[2], Gold[3] }, bx + dx * 2, by + dy * 2, 2);
        }
        else
        {
            for (double k = s; k < e; k += 3) cv.Set(x0 + dx * k, y0 + dy * k, C(k % 6 < 3 ? Ice[2] : Ice[3]));
            Dots(cv, new[] { Gold[3], Ice[3] }, 13, bx, by, 4, 4, 5);
        }
        return cv;
    }

    // ---------- 2. Shield Bash cone 64x64, pivot (32,40) ----------
    static Cv Bash(string v, int f)
    {
        var cv = BodyCell(64, 64, 32, 40);
        double cx, cy, ang;
        if (v == "Down") { cx = 16; cy = 28; ang = 90; } else if (v == "Up") { cx = 16; cy = 16; ang = -90; } else { cx = 8; cy = 23; ang = 180; }
        double r0 = 6 + f * 4;
        for (int k = 0; k < 3; k++)
        {
            double R = r0 + k * 4.5;
            if (f == 2 && k == 0) continue;
            Crescent(cv, Gold, cx, cy, R, ang + 32, -64, Math.Max(1.0, 2.8 - k * 0.6 - f * 0.4), 1.0, 0.0, f == 2);
        }
        if (f >= 1) Dots(cv, new[] { Gold[1], Gold[2], Gold[0] }, 21 + f, cx + Math.Cos(ang * Math.PI / 180) * (r0 + 9), cy + Math.Sin(ang * Math.PI / 180) * (r0 + 9), 6, 6, 6 + f * 3);
        return cv;
    }

    // ---------- 3. Shield glint 16x16 (2), stun stars 16x16 (4), charge dust 16x16 (3) ----------
    static Cv Glint(int f) { var cv = new Cv(16, 16, 0, 0); Star(cv, Gold, 8, 8, f == 0 ? 5 : 3); if (f == 0) { cv.Set(3, 3, C(Gold[1])); cv.Set(12, 12, C(Gold[1])); } return cv; }

    static Cv Stun(int f)
    {
        var cv = new Cv(16, 16, 0, 0);
        for (int k = 0; k < 3; k++)
        {
            double a = (f * 30 + k * 120) * Math.PI / 180;
            double x = 8 + Math.Cos(a) * 5.5, y = 8 + Math.Sin(a) * 2.5;
            bool frontStar = Math.Sin(a) > 0;
            Star(cv, frontStar ? Gold : new[] { Gold[1], Gold[2], Gold[3] }, x, y, frontStar ? 2 : 1);
        }
        return cv;
    }

    static Cv DustPuff(int f)
    {
        var cv = new Cv(16, 16, 0, 0);
        double r = 2.5 + f * 1.2;
        var spots = new[] { new[] { 8.0, 12.0 }, new[] { 5.0, 13.0 }, new[] { 11.0, 13.0 } };
        foreach (var sp in spots)
        {
            if (f == 2 && sp[0] == 8.0) continue;
            Disc(cv, C(f < 2 ? Dust[1] : Dust[2]), sp[0] + (sp[0] - 8) * f * 0.15, sp[1] - f * 1.0, r * (sp[0] == 8.0 ? 1.0 : 0.75));
            Disc(cv, C(Dust[0]), sp[0] + (sp[0] - 8) * f * 0.15 - 0.8, sp[1] - f * 1.0 - 0.8, Math.Max(0.6, r * (sp[0] == 8.0 ? 1.0 : 0.75) - 1.3));
        }
        return cv;
    }

    // ---------- 4. Whirl ring 64x48, pivot (32,34); back = upper half (behind body), front = lower half ----------
    static Cv Whirl(int f, bool front)
    {
        var cv = BodyCell(64, 48, 32, 34);
        double cx = 16, cy = 24, R = 22, ys = 0.5;
        Func<double, double, bool> half = (bx, by) => front ? by >= cy : by < cy;
        var tmp = BodyCell(64, 48, 32, 34);
        Ring(tmp, C(Ice[3]), cx, cy, R, 1.0, ys, null);
        double tip = f * 90 + 20;
        Crescent(tmp, Ice, cx, cy, R, tip, -150, 3.4, ys);
        Crescent(tmp, Ice, cx, cy, R, tip + 180, -100, 2.2, ys);
        for (int y = 0; y < cv.H; y++) for (int x = 0; x < cv.W; x++)
                if (tmp.Px[x, y].HasValue && half(x - cv.Ox + 0.5, y - cv.Oy + 0.5)) cv.Px[x, y] = tmp.Px[x, y];
        if (front) Dots(cv, new[] { Ice[0], Ice[1] }, 31 + f, cx + Math.Cos(tip * Math.PI / 180) * R, cy + Math.Sin(tip * Math.PI / 180) * R * ys, 4, 3, 4);
        return cv;
    }

    // ---------- 5. Bulwark hexagon 48x48, pivot (24,40): 2 appear + 4 loop + 2 break; back = upper half ----------
    static List<double[]> HexPts(double cx, double cy, double r)
    {
        var pts = new List<double[]>();
        for (int k = 0; k < 6; k++) { double a = Math.PI / 6 + k * Math.PI / 3; pts.Add(new[] { cx + Math.Cos(a) * r, cy + Math.Sin(a) * r }); }
        return pts;
    }

    static Cv Bulwark(int f, bool front)
    {
        var tmp = BodyCell(48, 48, 24, 40);
        double cx = 16, cy = 18;
        var outer = HexPts(cx, cy, 17); var inner = HexPts(cx, cy, 15);
        if (f == 0)
        {
            foreach (var p in outer) Star(tmp, Gold, p[0], p[1], 1);
            for (int k = 0; k < 6; k += 2) Line(tmp, C(Gold[3]), outer[k][0], outer[k][1], outer[(k + 1) % 6][0], outer[(k + 1) % 6][1]);
        }
        else if (f <= 5)
        {
            for (int k = 0; k < 6; k++)
            {
                Line(tmp, C(Gold[2]), outer[k][0], outer[k][1], outer[(k + 1) % 6][0], outer[(k + 1) % 6][1]);
                if (f >= 2) Line(tmp, C(Gold[1]), inner[k][0], inner[k][1], inner[(k + 1) % 6][0], inner[(k + 1) % 6][1]);
            }
            if (f >= 2)
            {
                // shimmer: a bright point travelling around the outer edge, glints on alternating vertices
                double t = (f - 2) / 4.0 * 6;
                int seg = (int)t; double u = t - seg;
                var a = outer[seg % 6]; var b = outer[(seg + 1) % 6];
                Star(tmp, Gold, a[0] + (b[0] - a[0]) * u, a[1] + (b[1] - a[1]) * u, 2);
                for (int k = f % 2; k < 6; k += 2) { tmp.Set(outer[k][0], outer[k][1], C(Gold[0])); }
                foreach (var g in new[] { new[] { 8.0, 8.0 }, new[] { 24.0, 8.0 }, new[] { 4.0, 20.0 }, new[] { 28.0, 20.0 }, new[] { 10.0, 30.0 }, new[] { 22.0, 30.0 } })
                    if (((int)g[0] + (int)g[1] + f) % 3 == 0) { tmp.Set(g[0], g[1], C(Gold[0])); tmp.Set(g[0] + 1, g[1], C(Gold[1])); }
            }
        }
        else
        {
            // break: shards flying outward
            var rnd = new Random(50 + f);
            for (int k = 0; k < 6; k++)
            {
                var a = outer[k]; var b = outer[(k + 1) % 6];
                double mx = (a[0] + b[0]) / 2, my = (a[1] + b[1]) / 2, ox = (mx - cx) * (f == 6 ? 0.15 : 0.35), oy = (my - cy) * (f == 6 ? 0.15 : 0.35);
                Line(tmp, C(f == 6 ? Gold[2] : Gold[3]), a[0] + ox + (b[0] - a[0]) * 0.25, a[1] + oy + (b[1] - a[1]) * 0.25, a[0] + ox + (b[0] - a[0]) * 0.65, a[1] + oy + (b[1] - a[1]) * 0.65);
            }
            Dots(tmp, new[] { Gold[1], Gold[2] }, 60 + f, cx, cy, 20, 20, f == 6 ? 12 : 6);
        }
        var cv = BodyCell(48, 48, 24, 40);
        for (int y = 0; y < cv.H; y++) for (int x = 0; x < cv.W; x++)
            {
                if (!tmp.Px[x, y].HasValue) continue;
                bool isFront = (y - cv.Oy + 0.5) >= cy;
                if (isFront == front) cv.Px[x, y] = tmp.Px[x, y];
            }
        return cv;
    }

    // ---------- 6. Dawnbreaker 96x96, pivot (48,64) ----------
    static Cv Dawn(string v, int f)
    {
        var cv = BodyCell(96, 96, 48, 64);
        double cx, cy, tip, sweep;
        if (v == "Down") { cx = 16; cy = 26; tip = 20; sweep = 170; }
        else if (v == "Up") { cx = 16; cy = 14; tip = -20; sweep = -140; }
        else { cx = 17; cy = 22; tip = 158; sweep = 175; }
        double R = 24;
        if (f == 0) { Crescent(cv, Gold, cx, cy, R - 2, tip, sweep * 0.8, 4.5); Crescent(cv, Ice, cx, cy, R - 7, tip, sweep * 0.6, 1.8); }
        else if (f == 1)
        {
            Crescent(cv, Gold, cx, cy, R, tip, sweep, 5.5);
            Crescent(cv, Ice, cx, cy, R - 6, tip, sweep * 0.7, 2.0);
            for (int k = 0; k < 4; k++)
            {
                double a = (tip + sweep * (0.05 + k * 0.17)) * Math.PI / 180;
                Line(cv, C(Gold[2]), cx + Math.Cos(a) * (R + 3), cy + Math.Sin(a) * (R + 3), cx + Math.Cos(a) * (R + 9), cy + Math.Sin(a) * (R + 9));
            }
            double ta = tip * Math.PI / 180;
            Dots(cv, new[] { Gold[1], Gold[0] }, 71, cx + Math.Cos(ta) * R, cy + Math.Sin(ta) * R, 10, 10, 12);
        }
        else
        {
            Crescent(cv, Gold, cx, cy, R + 1, tip, sweep * 0.55, 2.0, 1.0, 0.0, true);
            double ta = tip * Math.PI / 180;
            Dots(cv, new[] { Gold[2], Gold[3], Ice[2] }, 72, cx + Math.Cos(ta) * (R + 2), cy + Math.Sin(ta) * (R + 2), 14, 14, 10);
        }
        return cv;
    }

    // ---------- run ----------
    public static string Run(string outDir)
    {
        Directory.CreateDirectory(outDir);
        Manifest.Clear();
        var l = new List<Cv>();
        foreach (var v in Views) for (int f = 0; f < 3; f++) l.Add(Thrust(v, f));
        Save(outDir, "Aren_Thrust.png", l, 3, 40, 50, "rows Down/Up/Side, cols frame 0-2");
        l = new List<Cv>(); foreach (var v in Views) for (int f = 0; f < 3; f++) l.Add(Bash(v, f));
        Save(outDir, "Aren_ShieldBash.png", l, 3, 32, 40, "rows Down/Up/Side, cols frame 0-2");
        l = new List<Cv>(); for (int f = 0; f < 2; f++) l.Add(Glint(f));
        Save(outDir, "Aren_ShieldGlint.png", l, 2, 8, 8, "frames 0-1");
        l = new List<Cv>(); for (int f = 0; f < 4; f++) l.Add(Stun(f));
        Save(outDir, "Aren_StunStars.png", l, 4, 8, 8, "frames 0-3 (loop)");
        l = new List<Cv>(); for (int f = 0; f < 3; f++) l.Add(DustPuff(f));
        Save(outDir, "Aren_ChargeDust.png", l, 3, 8, 15, "frames 0-2");
        l = new List<Cv>(); for (int f = 0; f < 4; f++) l.Add(Whirl(f, false));
        Save(outDir, "Aren_WhirlRing_Back.png", l, 4, 32, 34, "frames 0-3, draw BEHIND body");
        l = new List<Cv>(); for (int f = 0; f < 4; f++) l.Add(Whirl(f, true));
        Save(outDir, "Aren_WhirlRing_Front.png", l, 4, 32, 34, "frames 0-3, draw IN FRONT of body");
        l = new List<Cv>(); for (int f = 0; f < 8; f++) l.Add(Bulwark(f, false));
        Save(outDir, "Aren_Bulwark_Back.png", l, 8, 24, 40, "0-1 appear, 2-5 loop, 6-7 break; BEHIND body");
        l = new List<Cv>(); for (int f = 0; f < 8; f++) l.Add(Bulwark(f, true));
        Save(outDir, "Aren_Bulwark_Front.png", l, 8, 24, 40, "0-1 appear, 2-5 loop, 6-7 break; IN FRONT of body");
        l = new List<Cv>(); foreach (var v in Views) for (int f = 0; f < 3; f++) l.Add(Dawn(v, f));
        Save(outDir, "Aren_Dawn.png", l, 3, 48, 64, "rows Down/Up/Side, cols frame 0-2");

        var sb = new StringBuilder("file,cell_w,cell_h,cols,rows,pivot_px_x,pivot_px_y_from_top,unity_pivot_x,unity_pivot_y,layout\n");
        foreach (var m in Manifest)
            sb.AppendFormat("{0},{1},{2},{3},{4},{5},{6},{7:0.####},{8:0.####},{9}\n", m.File, m.CellW, m.CellH, m.Cols, m.Rows, m.PivotX, m.PivotY,
                (double)m.PivotX / m.CellW, (double)(m.CellH - m.PivotY) / m.CellH, m.Layout);
        File.WriteAllText(Path.Combine(outDir, "AREN_KIT_VFX_MANIFEST.csv"), sb.ToString(), new UTF8Encoding(false));
        return "sheets " + Manifest.Count;
    }
}
