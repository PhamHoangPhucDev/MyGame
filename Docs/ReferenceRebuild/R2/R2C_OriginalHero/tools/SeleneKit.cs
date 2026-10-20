using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

// Selene (witch-mage) full art set — approved candidate selene/SELENE_CANDIDATE_v2.png (short staff, round gem).
// Body frames, staff (8 dirs x long / short), hand, orbs, sparkle, charge glow, per-step anchors, combo + skill VFX.
// Conventions = Aren/Vex/Brom/Lyra: 32x32 body cell, sole row 29, pivot (0.5, 0.0625); Side rows/sprites MIRRORED.
public static class SeleneKit
{
    const int W = 14, H = 21;
    static readonly string[] Views = { "Down", "Up", "Side" };
    static readonly int[] MirrorDir = { 4, 3, 2, 1, 0, 7, 6, 5 };
    static readonly string[] Fx = SeleneGen.FX;   // white, pink-light, pink-violet, deep violet, star blue
    static Color C(string h) { return HeroConceptGen.Hex(h); }

    // ================= body =================
    static void Row(char[,] g, int y, string s) { for (int x = 0; x < W; x++) g[x, y] = s[x]; }

    // Idle: hat tip sways (F1 one way, F3 the other), breath dip of hat + head (F2). 0.25 / 0.15 / 0.25 / 0.15 s.
    public static char[,] Idle(string v, int f)
    {
        var b = SeleneGen.Base(v); var g = EquipGen.Grid(b);
        if (f == 2)
        {
            for (int y = 11; y >= 1; y--) for (int x = 0; x < W; x++) { char s = b[y - 1][x]; g[x, y] = (y == 11 && s == '.') ? b[11][x] : s; }
            Row(g, 0, "..............");
            return g;
        }
        if (f == 1 || f == 3)
        {
            int sh = f == 1 ? 1 : -1;
            for (int y = 0; y <= 1; y++)
            {
                var src = b[y];
                for (int x = 0; x < W; x++) { int sx = x - sh; g[x, y] = sx >= 0 && sx < W ? src[sx] : '.'; }
            }
        }
        return g;
    }

    // Walk: F0/F2 contact (robe drops 1 px, hem on row 28, one shoe planted on row 29), F1/F3 passing. 4 x 0.125 s.
    public static char[,] Walk(string v, int f)
    {
        if (f == 1) return Idle(v, 1);
        if (f == 3) return Idle(v, 3);
        var b = SeleneGen.Base(v); var g = EquipGen.Grid(b);
        for (int y = 19; y >= 1; y--) for (int x = 0; x < W; x++) g[x, y] = b[y - 1][x];
        Row(g, 0, "..............");
        if (v == "Side") Row(g, 20, f == 0 ? "..kBBk..kBBk.." : "...kBBkkBBk...");
        else Row(g, 20, f == 0 ? "...kBBk.kk...." : "....kk.kBBk...");
        return g;
    }

    public static char[,] Kneel()
    {
        var b = EquipGen.Grid(SeleneGen.Down);
        var g = new char[W, H];
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) g[x, y] = '.';
        // hat + head + upper robe drop 2 px, robe folds (row 17 dropped), gold hem on row 19, knees on row 20
        for (int y = 0; y <= 16; y++) for (int x = 0; x < W; x++) g[x, y + 2] = b[x, y];
        for (int x = 0; x < W; x++) g[x, 19] = b[x, 18];
        Row(g, 20, "...kBBkkBBk...");
        return g;
    }

    public static Color?[,] Body32(string anim, string v, int f)
    {
        char[,] g = anim == "Idle" ? Idle(v, f) : anim == "Walk" ? Walk(v, f) : EquipGen.Grid(SeleneGen.Base(v));
        return EquipGen.Body(g, SeleneGen.Pal);
    }

    // ================= gear =================
    // Staff sheet index = len * 8 + dir (len 0 long rest/raise, 1 short tilted cast).
    public static EquipGen.Spr StaffSprite(int idx) { return SeleneGen.Staff(idx % 8, idx >= 8); }

    public class P { public int MX = -99, MY, MI, MO; public bool Hand = true; }

    static P From(SeleneGen.P s, int dy) { return new P { MX = s.X, MY = s.Y + dy, MI = (s.Short ? 8 : 0) + s.Dir, MO = s.Front ? 2 : -2 }; }

    public static P Move(string anim, string v, int f) { return From(SeleneGen.Rest(v), anim == "Walk" && (f == 0 || f == 2) ? 1 : 0); }
    public static P Cast(string v, int f) { return From(SeleneGen.Cast(v, f), 0); }

    public static Point Gem(P p) { var d = EquipGen.Dir[p.MI % 8]; int k = (p.MI >= 8 ? SeleneGen.ShaftShort : SeleneGen.ShaftLong) + 2; return new Point(p.MX + d.X * k, p.MY + d.Y * k); }

    // aim per view (right-facing art)
    static Point AimDir(string v) { return v == "Down" ? new Point(0, 1) : v == "Up" ? new Point(0, -1) : new Point(1, 0); }

    // ================= VFX =================
    static ArenKitSheets.Cv Cell(int w, int h, int pivX, int pivY) { return ArenKitSheets.BodyCell(w, h, pivX, pivY); }

    static void Sparkle(ArenKitSheets.Cv cv, double x, double y, int size, int k)
    {
        if (size <= 0) { cv.Set(x, y, C(k % 2 == 0 ? Fx[0] : Fx[4])); return; }
        ArenKitSheets.Star(cv, new[] { Fx[0], Fx[1], Fx[2] }, x, y, size);
    }

    // charge glow on the gem during F0: 24x24, pivot centre = gem. rows: normal | finisher; 2 frames
    public static ArenKitSheets.Cv Charge(bool fin, int fr)
    {
        var cv = new ArenKitSheets.Cv(24, 24, 0, 0);
        double r = (fin ? 6 : 4) + fr;
        ArenKitSheets.Ring(cv, C(fr == 0 ? Fx[2] : Fx[3]), 12, 12, r, 1.0, 1.0, (bx, by) => fin || ((int)((Math.Atan2(by - 12, bx - 12) * 180 / Math.PI + 360 + fr * 30) / 45) % 2) == 0);
        if (fin) ArenKitSheets.Ring(cv, C(Fx[1]), 12, 12, r - 2.5, 1.0, 1.0, null);
        for (int k = 0; k < (fin ? 4 : 3); k++)
        {
            double a = k * Math.PI * 2 / (fin ? 4 : 3) + fr * 0.6 + 0.3;
            Sparkle(cv, 12 + Math.Cos(a) * (r + 3), 12 + Math.Sin(a) * (r + 3), 0, k + fr);
        }
        if (fin) ArenKitSheets.Star(cv, new[] { Fx[0], Fx[1], Fx[2] }, 12, 12, fr == 0 ? 3 : 2);
        return cv;
    }

    // combo VFX 48x48, pivot = body pivot (24,38). fr 0 = cast frame (F1) burst at the gem, fr 1 = recover (F2) fade
    public static ArenKitSheets.Cv ComboVfx(string v, int step, int fr)
    {
        var cv = Cell(48, 48, 24, 38);
        var gem = Gem(Cast(v, 1)); var d = AimDir(v);
        bool fin = step == 2;
        if (fr == 0)
        {
            ArenKitSheets.Ring(cv, C(Fx[3]), gem.X, gem.Y, fin ? 6 : 4, 1.0, 1.0, null);
            ArenKitSheets.Ring(cv, C(Fx[2]), gem.X, gem.Y, fin ? 4 : 2.5, 1.0, 1.0, null);
            ArenKitSheets.Star(cv, new[] { Fx[0], Fx[1], Fx[2] }, gem.X, gem.Y, fin ? 4 : 3);
            ArenKitSheets.Dots(cv, new[] { Fx[1], Fx[4], Fx[0] }, 500 + step, gem.X + d.X * 4, gem.Y + d.Y * 4, fin ? 8 : 5, fin ? 8 : 5, fin ? 10 : 6);
        }
        else
        {
            ArenKitSheets.Ring(cv, C(Fx[3]), gem.X, gem.Y, fin ? 8 : 6, 1.0, 1.0, (bx, by) => ((int)((Math.Atan2(by - gem.Y, bx - gem.X) * 180 / Math.PI + 360) / 30) % 2) == 0);
            for (int k = 0; k < (fin ? 5 : 3); k++) { double a = k * 1.3 + 0.4; Sparkle(cv, gem.X + Math.Cos(a) * (5 + k), gem.Y + Math.Sin(a) * (5 + k) - 1, 0, k); }
        }
        return cv;
    }

    // sparkle particle left behind a flying orb: 8x8, pivot centre, 3 frames (bright star, small star, dot)
    public static ArenKitSheets.Cv SparkleCell(int f)
    {
        var cv = new ArenKitSheets.Cv(8, 8, 0, 0);
        if (f == 0) ArenKitSheets.Star(cv, new[] { Fx[0], Fx[1], Fx[2] }, 4, 4, 2);
        else if (f == 1) { cv.Set(4, 4, C(Fx[1])); cv.Set(3, 4, C(Fx[2])); cv.Set(5, 4, C(Fx[2])); cv.Set(4, 3, C(Fx[2])); cv.Set(4, 5, C(Fx[2])); }
        else cv.Set(4, 4, C(Fx[4]));
        return cv;
    }

    // orb hit pop: 32x32, pivot centre, 3 frames
    public static ArenKitSheets.Cv OrbHit(int f)
    {
        var cv = new ArenKitSheets.Cv(32, 32, 0, 0);
        double r = 4 + f * 3;
        ArenKitSheets.Ring(cv, C(f == 0 ? Fx[1] : f == 1 ? Fx[2] : Fx[3]), 16, 16, r, f == 2 ? 1.0 : 1.5, 1.0, f == 2 ? (Func<double, double, bool>)((bx, by) => ((int)bx + (int)by) % 3 != 0) : null);
        if (f == 0) { ArenKitSheets.Disc(cv, C(Fx[1]), 16, 16, 2.2); cv.Set(16, 16, C(Fx[0])); }
        ArenKitSheets.Dots(cv, new[] { Fx[0], Fx[4], Fx[1] }, 600 + f, 16, 16, r + 3, r + 3, 6 - f);
        return cv;
    }

    // R3 finisher star burst at the target: 48x48, pivot centre, radius 1.2 tiles (19 px ground ring), 4 frames
    public static ArenKitSheets.Cv StarBurst(int f)
    {
        var cv = new ArenKitSheets.Cv(48, 48, 0, 0);
        if (f < 3) ArenKitSheets.Ring(cv, C(f == 0 ? Fx[2] : Fx[3]), 24, 28, 8 + f * 6, f == 0 ? 1.6 : 1.2, 0.5, f == 2 ? (Func<double, double, bool>)((bx, by) => ((int)((Math.Atan2(by - 28, bx - 24) * 180 / Math.PI + 360) / 20) % 2) == 0) : null);
        if (f <= 1) SeleneGen.StarBurst(cv, 24, 22, f == 0 ? 9 : 12);
        else if (f == 2) ArenKitSheets.Star(cv, new[] { Fx[1], Fx[2], Fx[3] }, 24, 22, 5);
        ArenKitSheets.Dots(cv, new[] { Fx[0], Fx[4], Fx[1] }, 700 + f, 24, 22, 10 + f * 4, 8 + f * 3, f == 3 ? 6 : 10);
        return cv;
    }

    // 2 Star Orb explosion: 80x56, pivot centre (40,30); ground ring to radius 1.8 tiles (29 px); 4 frames
    public static ArenKitSheets.Cv Nova(int f)
    {
        var cv = new ArenKitSheets.Cv(80, 56, 0, 0);
        double cx = 40, cy = 34, R = 10 + f * 6.5;
        ArenKitSheets.Ring(cv, C(f < 2 ? Fx[2] : Fx[3]), cx, cy, R, f < 2 ? 2.0 : 1.2, 0.5, f == 3 ? (Func<double, double, bool>)((bx, by) => ((int)((Math.Atan2(by - cy, bx - cx) * 180 / Math.PI + 360) / 15) % 3) != 0) : null);
        if (f < 3) ArenKitSheets.Ring(cv, C(Fx[1]), cx, cy, R - 4, 1.0, 0.5, null);
        if (f == 0) { ArenKitSheets.Disc(cv, C(Fx[2]), cx, cy - 10, 7); ArenKitSheets.Disc(cv, C(Fx[1]), cx, cy - 10, 5); }
        if (f <= 1) SeleneGen.StarBurst(cv, cx, cy - 10, f == 0 ? 12 : 16);
        else ArenKitSheets.Star(cv, new[] { Fx[1], Fx[2], Fx[3] }, cx, cy - 10, f == 2 ? 6 : 3);
        ArenKitSheets.Dots(cv, new[] { Fx[0], Fx[4], Fx[1], Fx[2] }, 800 + f, cx, cy - 6, R + 2, R * 0.5 + 6, 8 + f * 3);
        return cv;
    }

    // 3 Blink: body cell 32x32 (pivot 16,30), 4 frames: gather, light column, star dust, fade
    public static ArenKitSheets.Cv Blink(int f)
    {
        var cv = Cell(32, 32, 16, 30);
        if (f == 0)
        {
            for (int k = 0; k < 6; k++) { double a = k * Math.PI / 3; Sparkle(cv, 16 + Math.Cos(a) * 10, 19 + Math.Sin(a) * 10, k % 3 == 0 ? 1 : 0, k); }
            ArenKitSheets.Ring(cv, C(Fx[2]), 16, 29, 7, 1.0, 0.4, null);
        }
        else if (f == 1)
        {
            for (int y = 6; y <= 29; y++) { cv.Set(15, y, C(y % 4 == 0 ? Fx[0] : Fx[1])); cv.Set(16, y, C(Fx[0])); cv.Set(17, y, C(y % 4 == 2 ? Fx[0] : Fx[1])); cv.Set(14, y, C(Fx[2])); cv.Set(18, y, C(Fx[2])); }
            ArenKitSheets.Ring(cv, C(Fx[1]), 16, 29, 6, 1.2, 0.4, null);
        }
        else
        {
            ArenKitSheets.Dots(cv, new[] { Fx[0], Fx[1], Fx[2], Fx[4] }, 900 + f, 16, 19, f == 2 ? 9 : 12, f == 2 ? 10 : 12, f == 2 ? 16 : 7);
            if (f == 2) { Sparkle(cv, 10, 12, 1, 0); Sparkle(cv, 22, 16, 1, 1); Sparkle(cv, 16, 24, 1, 2); }
            ArenKitSheets.Ring(cv, C(Fx[3]), 16, 29, f == 2 ? 8 : 10, 1.0, 0.4, (bx, by) => ((int)bx % 3) != 0);
        }
        return cv;
    }

    // 4 Rune Circle: 80x40, pivot centre (40,20), radius 2.2 tiles (35 px); 0 appear, 1-3 loop (rotate), 4 fade
    public static ArenKitSheets.Cv Rune(int f)
    {
        var cv = new ArenKitSheets.Cv(80, 40, 0, 0);
        double cx = 40, cy = 20, R = 35, ys = 0.45, rot = (f % 3) * 12 * Math.PI / 180;
        bool fade = f == 4;
        double grow = f == 0 ? 0.6 : 1.0;
        string outer = fade ? Fx[3] : Fx[2], inner = Fx[3];
        ArenKitSheets.Ring(cv, C(outer), cx, cy, R * grow, 1.2, ys, fade ? (Func<double, double, bool>)((bx, by) => ((int)((Math.Atan2(by - cy, bx - cx) * 180 / Math.PI + 360) / 15) % 2) == 0) : null);
        ArenKitSheets.Ring(cv, C(inner), cx, cy, (R - 7) * grow, 1.0, ys, null);
        if (fade) return cv;
        // 4-point star (Selene's motif) inside the inner ring: 8 vertices alternating tip / waist, rotating
        var pts = new List<Point>();
        for (int k = 0; k < 8; k++)
        {
            double a = rot - Math.PI / 2 + k * Math.PI / 4, rr = (k % 2 == 0 ? R - 8 : (R - 8) * 0.32) * grow;
            pts.Add(new Point((int)Math.Round(cx + Math.Cos(a) * rr), (int)Math.Round(cy + Math.Sin(a) * rr * ys)));
        }
        for (int k = 0; k < 8; k++) { var a = pts[k]; var b = pts[(k + 1) % 8]; ArenKitSheets.Line(cv, C(Fx[2]), a.X, a.Y, b.X, b.Y, 1); }
        cv.Set(cx, cy, C(Fx[0]));
        // runes between the rings: tiny 3-px glyphs rotating with the circle
        for (int k = 0; k < 8; k++)
        {
            double a = rot + k * Math.PI / 4 + 0.2;
            double x = cx + Math.Cos(a) * (R - 3.5) * grow, y = cy + Math.Sin(a) * (R - 3.5) * grow * ys;
            cv.Set(x, y, C(Fx[1])); cv.Set(x + (k % 2 == 0 ? 1 : 0), y + (k % 2 == 0 ? 0 : -1), C(Fx[1])); cv.Set(x - 1, y + (k % 3 == 0 ? 1 : 0), C(Fx[0]));
        }
        return cv;
    }

    // V Mana Shield bubble: 48x48, pivot (24,40) = body pivot; 0 appear, 1-2 hold loop, 3 absorb flash, 4-5 pop
    public static ArenKitSheets.Cv Bubble(int f)
    {
        var cv = Cell(48, 48, 24, 40);
        double cx = 16, cy = 19, R = 14;
        if (f <= 3)
        {
            double r = f == 0 ? R - 4 : R;
            string rim = f == 3 ? Fx[1] : Fx[2];
            ArenKitSheets.Ring(cv, C(rim), cx, cy, r, f == 3 ? 1.8 : 1.2, 1.05, f == 2 ? (Func<double, double, bool>)((bx, by) => ((int)((Math.Atan2(by - cy, bx - cx) * 180 / Math.PI + 360) / 10) % 6) != 0) : null);
            if (f >= 1)
            {
                // sheen arc top-left + small glints
                ArenKitSheets.Ring(cv, C(Fx[0]), cx, cy, r - 2.5, 1.0, 1.05, (bx, by) => { double a = Math.Atan2(by - cy, bx - cx) * 180 / Math.PI; return a > -160 && a < -110; });
                Sparkle(cv, cx + r * 0.6, cy - r * 0.6, f == 3 ? 2 : 1, f);
                cv.Set(cx - r * 0.75, cy + r * 0.45, C(Fx[4]));
            }
            if (f == 3) ArenKitSheets.Ring(cv, C(Fx[0]), cx, cy, r + 1.5, 1.0, 1.05, (bx, by) => ((int)bx + (int)by) % 2 == 0);
        }
        else
        {
            for (int k = 0; k < 10; k++)
            {
                double a = k * Math.PI / 5 + (f == 5 ? 0.3 : 0), rr = R * (f == 4 ? 1.1 : 1.35);
                double x = cx + Math.Cos(a) * rr, y = cy + Math.Sin(a) * rr * 1.05;
                if (f == 4) ArenKitSheets.Line(cv, C(Fx[2]), x, y, x + Math.Cos(a) * 2, y + Math.Sin(a) * 2); else Sparkle(cv, x, y, 0, k);
            }
        }
        return cv;
    }

    // C Meteor: 32x48, pivot = impact point (16,44); 0-1 falling (diagonal from top-right), 2 impact, 3 fade
    public static ArenKitSheets.Cv Meteor(int f)
    {
        var cv = new ArenKitSheets.Cv(32, 48, 0, 0);
        if (f <= 1)
        {
            double hx = f == 0 ? 20 : 17, hy = f == 0 ? 20 : 34;
            ArenKitSheets.Line(cv, C(Fx[3]), hx + 9, hy - 18, hx, hy, 3);
            ArenKitSheets.Line(cv, C(Fx[2]), hx + 8, hy - 15, hx, hy, 2);
            ArenKitSheets.Line(cv, C(Fx[1]), hx + 5, hy - 10, hx, hy, 1);
            ArenKitSheets.Disc(cv, C(Fx[1]), hx, hy, 2.2); cv.Set(hx, hy, C(Fx[0])); cv.Set(hx - 1, hy - 1, C(Fx[0]));
            Sparkle(cv, hx + 11, hy - 10, 0, f); Sparkle(cv, hx + 4, hy - 16, 0, f + 1);
        }
        else if (f == 2)
        {
            ArenKitSheets.Ring(cv, C(Fx[2]), 16, 44, 10, 1.6, 0.4, null);
            SeleneGen.StarBurst(cv, 16, 38, 8);
        }
        else
        {
            ArenKitSheets.Ring(cv, C(Fx[3]), 16, 44, 13, 1.0, 0.4, (bx, by) => ((int)bx % 3) != 0);
            ArenKitSheets.Dots(cv, new[] { Fx[1], Fx[4] }, 950, 16, 38, 10, 6, 6);
        }
        return cv;
    }

    // C target zone: 112x56, pivot centre (56,28), radius 3 tiles (48 px); 2-frame loop
    public static ArenKitSheets.Cv MeteorZone(int f)
    {
        var cv = new ArenKitSheets.Cv(112, 56, 0, 0);
        double cx = 56, cy = 28;
        ArenKitSheets.Ring(cv, C(Fx[2]), cx, cy, 48, 1.2, 0.5, (bx, by) => ((int)((Math.Atan2(by - cy, bx - cx) * 180 / Math.PI + 360 + f * 8) / 16) % 2) == 0);
        ArenKitSheets.Ring(cv, C(Fx[3]), cx, cy, 42, 1.0, 0.5, (bx, by) => ((int)((Math.Atan2(by - cy, bx - cx) * 180 / Math.PI + 360 - f * 8) / 16) % 2) == 1);
        for (int k = 0; k < 4; k++) { double a = (k * 90 + 45 + f * 8) * Math.PI / 180; Sparkle(cv, cx + Math.Cos(a) * 45, cy + Math.Sin(a) * 45 * 0.5, 0, k + f); }
        return cv;
    }

    // ================= preview (right-facing, unmirrored) =================
    static void Over(Bitmap bmp, ArenKitSheets.Cv cv, int ox, int oy, bool behindBody, Bitmap bodyMask)
    {
        for (int y = 0; y < cv.H; y++) for (int x = 0; x < cv.W; x++)
            {
                if (!cv.Px[x, y].HasValue) continue;
                int X = ox + x, Y = oy + y; if (X < 0 || Y < 0 || X >= bmp.Width || Y >= bmp.Height) continue;
                if (behindBody && bodyMask != null && bodyMask.GetPixel(X, Y).A > 0) continue;
                bmp.SetPixel(X, Y, cv.Px[x, y].Value);
            }
    }

    public static Bitmap Preview(string anim, string v, int f, int step, bool withShot)
    {
        var body = Body32(anim, v, f);
        P p = anim == "Attack" ? Cast(v, f) : Move(anim, v, f);
        var layers = new List<EquipGen.Layer> { new EquipGen.Layer { S = StaffSprite(p.MI), X = p.MX, Y = p.MY }, new EquipGen.Layer { S = SeleneGen.Hand, X = p.MX - 1, Y = p.MY - 1 } };
        var bmp = p.MO > 0 ? EquipGen.Compose(body, new List<EquipGen.Layer>(), layers) : EquipGen.Compose(body, layers, new List<EquipGen.Layer>());
        bool up = v == "Up";
        if (anim == "Attack" && f == 0) { var g = Gem(p); Over(bmp, Charge(step == 2, 0), 8 + g.X - 12, 8 + g.Y - 12, up, bmp); }
        if (anim == "Attack" && f > 0) Over(bmp, ComboVfx(v, step, f - 1), 0, 0, up, bmp);
        if (withShot && anim == "Attack" && f == 2)
        {
            var g = Gem(Cast(v, 1)); var d = AimDir(v);
            int ox = 8 + g.X + d.X * 10, oy = 8 + g.Y + d.Y * 10;
            for (int k = 1; k <= 3; k++) Over(bmp, SparkleCell(k - 1), ox - d.X * 4 * k - 4, oy - d.Y * 4 * k - 4, false, null);
            var orb = SeleneGen.Orb(false, 0);
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) { int X = ox - 8 + x, Y = oy - 8 + y; var c = orb.GetPixel(x, y); if (c.A > 0 && X >= 0 && Y >= 0 && X < 48 && Y < 48) bmp.SetPixel(X, Y, c); }
        }
        return bmp;
    }

    // ================= export =================
    public class SheetInfo { public string File; public int CellW, CellH, Cols, Rows, PivotX, PivotY; public string Layout; }
    public static readonly List<SheetInfo> Manifest = new List<SheetInfo>();

    static void SaveCells(string dir, string sub, string file, List<Color?[,]> cells, int cw, int ch, int cols, int pivX, int pivY, string layout, bool[] mirror = null)
    {
        int rows = (cells.Count + cols - 1) / cols;
        var bmp = new Bitmap(cw * cols, ch * rows, PixelFormat.Format32bppArgb);
        for (int i = 0; i < cells.Count; i++)
        {
            int ox = (i % cols) * cw, oy = (i / cols) * ch; bool m = mirror != null && mirror[i];
            for (int y = 0; y < ch; y++) for (int x = 0; x < cw; x++)
                    if (cells[i][x, y].HasValue) bmp.SetPixel(ox + (m ? cw - 1 - x : x), oy + y, cells[i][x, y].Value);
        }
        bmp.Save(Path.Combine(dir, sub, file), ImageFormat.Png); bmp.Dispose();
        Manifest.Add(new SheetInfo { File = sub + "/" + file, CellW = cw, CellH = ch, Cols = cols, Rows = rows, PivotX = pivX, PivotY = pivY, Layout = layout });
    }

    static Color?[,] Px(ArenKitSheets.Cv cv) { return cv.Px; }
    static Color?[,] Px(Bitmap b) { var p = new Color?[b.Width, b.Height]; for (int y = 0; y < b.Height; y++) for (int x = 0; x < b.Width; x++) { var c = b.GetPixel(x, y); if (c.A > 0) p[x, y] = c; } return p; }
    static Color?[,] Px(EquipGen.Spr s, int w, int h, int ax, int ay) { var p = new Color?[w, h]; foreach (var kv in s.Px) { int x = ax + kv.Key.X, y = ay + kv.Key.Y; if (x >= 0 && y >= 0 && x < w && y < h) p[x, y] = kv.Value; } return p; }
    static Func<Color, Color> Tint(Color to, double k) { return c => Color.FromArgb(255, (int)(c.R + (to.R - c.R) * k), (int)(c.G + (to.G - c.G) * k), (int)(c.B + (to.B - c.B) * k)); }

    static void PaintCell(Bitmap bmp, int ox, int oy, Color?[,] c, bool mirror, Func<Color, Color> fx)
    {
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
            {
                if (!c[x, y].HasValue) continue;
                bmp.SetPixel(ox + (mirror ? 31 - x : x), oy + y, fx == null ? c[x, y].Value : fx(c[x, y].Value));
            }
    }

    static string CsvRow(string key, string v, P p, int slash, int slashOrder)
    {
        bool m = v == "Side";
        int mx = p.MX, mi = p.MI, hx = -99, hy = 0;
        if (m && mx > -99) { mx = 31 - mx; mi = (mi / 8) * 8 + MirrorDir[mi % 8]; }
        if (p.Hand && p.MX > -99) { hx = m ? 31 - p.MX : p.MX - 1; hy = p.MY - 1; }
        // key,main_x,main_y,main_dir,main_order,hand_x,hand_y,off_x,off_y,off_view,off_order,slash,slash_order,off_dir
        return string.Format("{0},{1},{2},{3},{4},{5},{6},-99,0,{7},0,{8},{9},-1\n", key, mx, p.MY, mi, p.MO, hx, hy, v, slash, slashOrder);
    }

    public static string Run(string outDir)
    {
        Manifest.Clear();
        foreach (var sub in new[] { "Extra", "Gear", "Skills" }) Directory.CreateDirectory(Path.Combine(outDir, sub));
        var sheet = new Bitmap(128, 288, PixelFormat.Format32bppArgb);
        var anims = new[] { "Idle", "Walk", "Attack" };
        for (int a = 0; a < 3; a++) for (int vi = 0; vi < 3; vi++) for (int f = 0; f < (a == 2 ? 3 : 4); f++)
                    PaintCell(sheet, f * 32, (a * 3 + vi) * 32, Body32(anims[a], Views[vi], f), Views[vi] == "Side", null);
        sheet.Save(Path.Combine(outDir, "Selene_Sheet.png"), ImageFormat.Png); sheet.Dispose();
        Manifest.Add(new SheetInfo { File = "Selene_Sheet.png", CellW = 32, CellH = 32, Cols = 4, Rows = 9, PivotX = 16, PivotY = 30, Layout = "rows Idle D/U/S, Walk D/U/S, Attack D/U/S (Attack 3 cols: raise, cast, recover — the staff carries the motion); Side faces left" });
        foreach (var v in Views)
            for (int f = 0; f < 2; f++)
            {
                var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
                PaintCell(bmp, 0, 0, Body32(f == 0 ? "Idle" : "Walk", v, 0), v == "Side", f == 0 ? Tint(Color.White, 0.8) : Tint(C("FF3030"), 0.35));
                bmp.Save(Path.Combine(outDir, "Extra", "Selene_Hurt_" + v + "_" + f + ".png"), ImageFormat.Png); bmp.Dispose();
                Manifest.Add(new SheetInfo { File = "Extra/Selene_Hurt_" + v + "_" + f + ".png", CellW = 32, CellH = 32, Cols = 1, Rows = 1, PivotX = 16, PivotY = 30, Layout = "hurt: 0 white flash, 1 red tint" });
            }
        {
            var d0 = new Bitmap(32, 32, PixelFormat.Format32bppArgb); PaintCell(d0, 0, 0, EquipGen.Body(Kneel(), SeleneGen.Pal), false, null);
            d0.Save(Path.Combine(outDir, "Extra", "Selene_Death_Down_0.png"), ImageFormat.Png); d0.Dispose();
            var lying = FxGen.Lying(SeleneGen.Down);
            for (int f = 1; f <= 2; f++)
            {
                var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
                for (int y = 0; y < 14; y++) for (int x = 0; x < 21; x++) if (lying[x, y] != '.') bmp.SetPixel(5 + x, 16 + y, C(SeleneGen.Pal[lying[x, y]]));
                bmp.Save(Path.Combine(outDir, "Extra", "Selene_Death_Down_" + f + ".png"), ImageFormat.Png); bmp.Dispose();
            }
            for (int f = 0; f <= 2; f++) Manifest.Add(new SheetInfo { File = "Extra/Selene_Death_Down_" + f + ".png", CellW = 32, CellH = 32, Cols = 1, Rows = 1, PivotX = 16, PivotY = 30, Layout = "death: 0 kneel, 1 fall, 2 lying (hold)" });
        }

        // gear
        var staffs = new List<Color?[,]>(); for (int i = 0; i < 16; i++) staffs.Add(Px(StaffSprite(i), 32, 32, 16, 16));
        SaveCells(outDir, "Gear", "Selene_Staff.png", staffs, 32, 32, 8, 16, 16, "index = len*8 + dir (dir R UR U UL L DL D DR; len 0 long rest/raise, 1 short tilted cast); pivot = grip");
        SaveCells(outDir, "Gear", "Selene_Hand.png", new List<Color?[,]> { Px(SeleneGen.Hand, 4, 4, 1, 1) }, 4, 4, 1, 1, 1, "fist on the staff grip (hand slot)");
        var orbs = new List<Color?[,]>(); foreach (var big in new[] { false, true }) for (int fr = 0; fr < 2; fr++) orbs.Add(Px(SeleneGen.Orb(big, fr)));
        SaveCells(outDir, "Gear", "Selene_Orb.png", orbs, 16, 16, 4, 8, 8, "cols: normal f0, normal f1, big f0, big f1 (round: no direction)");
        var sp = new List<Color?[,]>(); for (int f = 0; f < 3; f++) sp.Add(Px(SparkleCell(f)));
        SaveCells(outDir, "Gear", "Selene_Sparkle.png", sp, 8, 8, 3, 4, 4, "trail particle behind orbs: 3 frames (star, small star, dot)");
        var ch = new List<Color?[,]>(); foreach (var fin in new[] { false, true }) for (int fr = 0; fr < 2; fr++) ch.Add(Px(Charge(fin, fr)));
        SaveCells(outDir, "Gear", "Selene_Charge.png", ch, 24, 24, 2, 12, 12, "gem glow during Attack F0: rows normal | finisher, 2 frames; pivot = gem centre");
        var combo = new List<Color?[,]>(); var mir = new List<bool>();
        for (int step = 0; step < 3; step++) foreach (var v in Views) for (int fr = 0; fr < 2; fr++) { combo.Add(Px(ComboVfx(v, step, fr))); mir.Add(v == "Side"); }
        SaveCells(outDir, "Gear", "Selene_ComboVfx.png", combo, 48, 48, 18, 24, 38, "18 cells: step*6 + view*2 + frame (frame 0 = cast F1, 1 = recover F2; Side faces left)", mir.ToArray());
        var hit = new List<Color?[,]>(); for (int f = 0; f < 3; f++) hit.Add(Px(OrbHit(f)));
        SaveCells(outDir, "Gear", "Selene_OrbHit.png", hit, 32, 32, 3, 16, 16, "orb hit pop (combo 1-2), 3 frames");
        var sb = new List<Color?[,]>(); for (int f = 0; f < 4; f++) sb.Add(Px(StarBurst(f)));
        SaveCells(outDir, "Gear", "Selene_StarBurst.png", sb, 48, 48, 4, 24, 28, "combo 3 finisher: star burst at the target, ground ring radius 19 px (1.2 tiles); 4 frames; pivot = ground centre");

        // skills
        var l = new List<Color?[,]>(); for (int f = 0; f < 4; f++) l.Add(Px(Nova(f)));
        SaveCells(outDir, "Skills", "Selene_Nova.png", l, 80, 56, 4, 40, 34, "2 Cau Tinh Tu: explosion, ground ring to radius 29 px (1.8 tiles); 4 frames; pivot = ground centre");
        l = new List<Color?[,]>(); for (int f = 0; f < 4; f++) l.Add(Px(Blink(f)));
        SaveCells(outDir, "Skills", "Selene_Blink.png", l, 32, 32, 4, 16, 30, "3 Dich Chuyen: 0 gather, 1 light column, 2 star dust, 3 fade (origin 0-3, destination 1-3)");
        l = new List<Color?[,]>(); for (int f = 0; f < 5; f++) l.Add(Px(Rune(f)));
        SaveCells(outDir, "Skills", "Selene_Rune.png", l, 80, 40, 5, 40, 20, "4 Vong Phu Chu: radius 35 px (2.2 tiles); 0 appear, 1-3 loop, 4 fade; ground layer");
        l = new List<Color?[,]>(); for (int f = 0; f < 6; f++) l.Add(Px(Bubble(f)));
        SaveCells(outDir, "Skills", "Selene_Bubble.png", l, 48, 48, 6, 24, 40, "V Khien Ma Luc: 0 appear, 1-2 hold loop, 3 absorb flash, 4-5 pop; IN FRONT of body");
        l = new List<Color?[,]>(); for (int f = 0; f < 4; f++) l.Add(Px(Meteor(f)));
        SaveCells(outDir, "Skills", "Selene_Meteor.png", l, 32, 48, 4, 16, 44, "C Mua Sao Bang: one meteor; 0-1 falling, 2 impact, 3 fade; pivot = impact point");
        l = new List<Color?[,]>(); for (int f = 0; f < 2; f++) l.Add(Px(MeteorZone(f)));
        SaveCells(outDir, "Skills", "Selene_MeteorZone.png", l, 112, 56, 2, 56, 28, "C target zone radius 48 px (3 tiles), 2-frame loop; ground layer");

        // anchors
        var csv = new StringBuilder("key,main_x,main_y,main_dir,main_order,hand_x,hand_y,off_x,off_y,off_view,off_order,slash,slash_order,off_dir\n");
        foreach (var anim in new[] { "Idle", "Walk" }) foreach (var v in Views) for (int f = 0; f < 4; f++) csv.Append(CsvRow(anim + "_" + v + "_" + f, v, Move(anim, v, f), -1, 0));
        for (int vi = 0; vi < 3; vi++)
        {
            var v = Views[vi]; int so = v == "Up" ? -3 : 3;
            csv.Append(CsvRow("Attack_" + v + "_0", v, Cast(v, 0), -1, 0));
            csv.Append(CsvRow("Attack_" + v + "_1", v, Cast(v, 1), vi * 2, so));
            csv.Append(CsvRow("Attack_" + v + "_2", v, Cast(v, 2), vi * 2 + 1, so));
            for (int step = 0; step < 3; step++)
            {
                csv.Append(CsvRow("Attack_" + v + "_1@" + step, v, Cast(v, 1), vi * 2, so));
                csv.Append(CsvRow("Attack_" + v + "_2@" + step, v, Cast(v, 2), vi * 2 + 1, so));
            }
            csv.Append(CsvRow("Hurt_" + v + "_0", v, Move("Idle", v, 0), -1, 0));
            csv.Append(CsvRow("Hurt_" + v + "_1", v, Move("Walk", v, 0), -1, 0));
        }
        csv.Append(CsvRow("Death_Down_0", "Down", new P { MX = 10, MY = 27, MI = 2, MO = 2 }, -1, 0));
        csv.Append("Death_Down_1,-99,0,0,0,-99,0,-99,0,Down,0,-1,0,-1\n");
        csv.Append(CsvRow("Death_Down_2", "Down", new P { MX = 20, MY = 29, MI = 0, MO = 2, Hand = false }, -1, 0));
        File.WriteAllText(Path.Combine(outDir, "Selene_Anchors.csv"), csv.ToString(), new UTF8Encoding(false));

        var mf = new StringBuilder("file,cell_w,cell_h,cols,rows,pivot_px_x,pivot_px_y_from_top,unity_pivot_x,unity_pivot_y,layout\n");
        foreach (var s in Manifest)
            mf.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5},{6},{7:0.####},{8:0.####},\"{9}\"\n", s.File, s.CellW, s.CellH, s.Cols, s.Rows, s.PivotX, s.PivotY, s.PivotX / (double)s.CellW, (s.CellH - s.PivotY) / (double)s.CellH, s.Layout);
        File.WriteAllText(Path.Combine(outDir, "SELENE_MANIFEST.csv"), mf.ToString(), new UTF8Encoding(false));
        return "ok";
    }
}
