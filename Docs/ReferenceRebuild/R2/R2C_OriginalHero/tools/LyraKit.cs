using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

// Lyra (elf archer) full art set — approved candidate lyra/LYRA_CANDIDATE_v6.png.
// Body frames (shooting arms baked into Attack), bow (8 dirs x rest / nock / full draw + back tip), hands, arrows,
// arrow trails, per-step anchors, combo + skill VFX, Unity-ready export.
// Conventions = Aren/Vex/Brom: 32x32 body cell, sole row 29, pivot (0.5, 0.0625); Side rows/sprites MIRRORED (face left).
public static class LyraKit
{
    const int W = 14, H = 21;
    static readonly string[] Views = { "Down", "Up", "Side" };
    static readonly int[] MirrorDir = { 4, 3, 2, 1, 0, 7, 6, 5 };
    static readonly string[] Fx = LyraGen.FX;   // core, light, mid, edge, gold
    static Color C(string h) { return HeroConceptGen.Hex(h); }

    // ================= body =================
    static void Row(char[,] g, int y, string s) { for (int x = 0; x < W; x++) g[x, y] = s[x]; }

    // Idle: leaf clip flutter + skirt point sway (F1 left / F3 right), breath dip (F2). 0.25 / 0.15 / 0.25 / 0.15 s.
    public static char[,] Idle(string v, int f)
    {
        var b = LyraGen.Base(v); var g = EquipGen.Grid(b);
        if (f == 2)
        {
            for (int y = 11; y >= 1; y--) for (int x = 0; x < W; x++) { char s = b[y - 1][x]; g[x, y] = (y == 11 && s == '.') ? b[11][x] : s; }
            Row(g, 0, "..............");
            return g;
        }
        if (f == 1 || f == 3)
        {
            for (int y = 0; y <= 1; y++) for (int x = 0; x < W; x++) { if (g[x, y] == 'f') g[x, y] = 'F'; else if (g[x, y] == 'F') g[x, y] = 'f'; }
            int first = -1, last = -1;
            for (int x = 0; x < W; x++) if (g[x, 18] == 'V') { if (first < 0) first = x; last = x; }
            if (f == 1 && first > 0) { g[first, 18] = '.'; g[first - 1, 18] = 'V'; }
            if (f == 3 && last >= 0 && last < W - 1) { g[last, 18] = '.'; g[last + 1, 18] = 'V'; }
        }
        return g;
    }

    // Walk: F0/F2 contact (body drops 1 px, one boot planted on row 29, the other lifted), F1/F3 passing. 4 x 0.125 s.
    public static char[,] Walk(string v, int f)
    {
        if (f == 1) return Idle(v, 1);
        if (f == 3) return Idle(v, 3);
        var b = LyraGen.Base(v); var g = EquipGen.Grid(b);
        for (int y = 18; y >= 1; y--) for (int x = 0; x < W; x++) g[x, y] = b[y - 1][x];
        Row(g, 0, "..............");
        if (v == "Down")
        {
            Row(g, 19, "....kBbkBbk...");
            Row(g, 20, f == 0 ? "...kBBbk.kk..." : "....kk.kBBbk..");
        }
        else if (v == "Up")
        {
            Row(g, 19, "...kbBkbBk....");
            Row(g, 20, f == 0 ? "..kbBBk.kk...." : "...kk.kbBBk...");
        }
        else
        {
            if (f == 0) { Row(g, 19, "..kBbk..kBbk.."); Row(g, 20, ".kBBbk..kBBbk."); }
            else { Row(g, 19, "...kBbk.kBbk.."); Row(g, 20, "..kBBbkkBBbk.."); }
        }
        return g;
    }

    public static char[,] Kneel()
    {
        var b = EquipGen.Grid(LyraGen.Down);
        var g = new char[W, H];
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) g[x, y] = '.';
        for (int y = 0; y <= 17; y++) for (int x = 0; x < W; x++) g[x, y + 2] = b[x, y];
        Row(g, 20, "..kBBbkkBBbk..");
        return g;
    }

    // 32x32 right-facing body cell; Attack frames get the raised arms (front views over the body, Up behind it)
    public static Color?[,] Body32(string anim, string v, int f)
    {
        char[,] g = anim == "Idle" ? Idle(v, f) : anim == "Walk" ? Walk(v, f) : LyraGen.ShotBody(v);
        var c = EquipGen.Body(g, LyraGen.Pal);
        if (anim != "Attack") return c;
        var p = LyraGen.Shot(v, f);
        var m = LyraGen.StringMid(p.Dir, p.HandDraw);
        var arms = new[] { Tuple.Create(LyraGen.Arm(p.BowShoulder, new Point(p.X, p.Y)), p.BowArmFront), Tuple.Create(LyraGen.Arm(p.DrawShoulder, new Point(p.X + m.X, p.Y + m.Y)), p.DrawArmFront) };
        foreach (var a in arms)
            foreach (var kv in a.Item1.Px)
            {
                int x = kv.Key.X, y = kv.Key.Y;
                if (x < 1 || y < 1 || x > 30 || y > 30) continue;
                if (a.Item2 || !c[x, y].HasValue) c[x, y] = kv.Value;
            }
        return c;
    }

    // ================= gear =================
    // Bow sheet index = state * 8 + dir (state 0 rest/release, 1 nock, 2 full draw); 24 = back-slung tip (Down idle).
    public const int BackTipIndex = 24;
    static readonly Point BackTipAnchor = new Point(22, 18);

    public static EquipGen.Spr BowSprite(int idx)
    {
        if (idx == BackTipIndex)
        {
            var o = new EquipGen.Spr();
            foreach (var kv in LyraGen.BackTip().Px) o.Px[new Point(kv.Key.X - BackTipAnchor.X, kv.Key.Y - BackTipAnchor.Y)] = kv.Value;
            return o;
        }
        int state = idx / 8, dir = idx % 8;
        return LyraGen.Bow(dir, state == 0 ? 0 : state == 1 ? 1 : 4, state > 0);
    }

    // MX,MY = bow pivot (grip), MI = bow sheet index, MO = order; hand = fist at the grip; OX,OY = draw-hand fist pos
    public class P { public int MX = -99, MY, MI, MO; public bool Hand; public int OX = -99, OY, OO; }

    public static P Rest(string v, int dropY)
    {
        if (v == "Down") return new P { MX = BackTipAnchor.X, MY = BackTipAnchor.Y + dropY, MI = BackTipIndex, MO = -2 };
        var r = LyraGen.Rest(v);
        return new P { MX = r.X, MY = r.Y + dropY, MI = r.Dir, MO = r.Front ? 2 : -2 };
    }

    public static P Move(string anim, string v, int f) { return Rest(v, anim == "Walk" && (f == 0 || f == 2) ? 1 : 0); }

    // f: 0 nock, 1 full draw, 2 release
    public static P Shot(string v, int f)
    {
        var s = LyraGen.Shot(v, f);
        int state = f == 0 ? 1 : f == 1 ? 2 : 0;
        var m = LyraGen.StringMid(s.Dir, s.HandDraw);
        return new P { MX = s.X, MY = s.Y, MI = state * 8 + s.Dir, MO = s.Front ? 2 : -2, Hand = true, OX = s.X + m.X, OY = s.Y + m.Y, OO = s.DrawArmFront ? 3 : -3 };
    }

    // aim direction + grip per view (right-facing art)
    static Point AimDir(string v) { return v == "Down" ? new Point(0, 1) : v == "Up" ? new Point(0, -1) : new Point(1, 0); }
    static Point Grip(string v) { var s = LyraGen.Shot(v, 1); return new Point(s.X, s.Y); }

    // ================= VFX helpers =================
    static ArenKitSheets.Cv Cell(int w, int h, int pivX, int pivY) { return ArenKitSheets.BodyCell(w, h, pivX, pivY); }

    static void Leaf(ArenKitSheets.Cv cv, double x, double y, int turn)
    {
        cv.Set(x, y, C(Fx[1])); cv.Set(x + 1, y, C(Fx[2]));
        cv.Set(x + (turn % 2 == 0 ? 1 : 0), y + (turn < 2 ? 1 : -1), C(Fx[3]));
    }

    // combo VFX 48x48, pivot = body pivot (24,38). fr 0 = draw frame (F1), fr 1 = release frame (F2).
    public static ArenKitSheets.Cv ComboVfx(string v, int step, int fr)
    {
        var cv = Cell(48, 48, 24, 38);
        var d = AimDir(v); var g = Grip(v); var pp = new Point(-d.Y, d.X);
        double tx = g.X + d.X * 3, ty = g.Y + d.Y * 3;     // arrow tip at full draw
        double mx = g.X + d.X * 5, my = g.Y + d.Y * 5;     // muzzle
        bool charged = step == 2;
        if (fr == 0)
        {
            if (!charged) ArenKitSheets.Star(cv, new[] { Fx[0], Fx[1], Fx[2] }, tx + d.X, ty + d.Y, 1);
            else
            {
                ArenKitSheets.Ring(cv, C(Fx[2]), tx + d.X, ty + d.Y, 4.5, 1.2, 1.0, null);
                ArenKitSheets.Ring(cv, C(Fx[1]), tx + d.X, ty + d.Y, 2.5, 1.0, 1.0, null);
                for (int k = 0; k < 4; k++) { double a = k * Math.PI / 2 + 0.6; Leaf(cv, tx + d.X + Math.Cos(a) * 6, ty + d.Y + Math.Sin(a) * 6, k); }
                ArenKitSheets.Star(cv, new[] { Fx[0], Fx[4], Fx[1] }, tx + d.X, ty + d.Y, 2);
            }
        }
        else
        {
            int len = charged ? 12 : 8;
            ArenKitSheets.Line(cv, C(Fx[2]), mx, my, mx + d.X * len, my + d.Y * len, charged ? 3 : 1);
            ArenKitSheets.Line(cv, C(Fx[1]), mx, my, mx + d.X * (len - 2), my + d.Y * (len - 2), 1);
            ArenKitSheets.Line(cv, C(Fx[0]), mx, my, mx + d.X * (len / 2), my + d.Y * (len / 2), 1);
            foreach (int s in new[] { -1, 1 })
            {
                double sx = mx + pp.X * s * 3 + d.X * 2, sy = my + pp.Y * s * 3 + d.Y * 2;
                ArenKitSheets.Line(cv, C(Fx[2]), sx, sy, sx + d.X * 4, sy + d.Y * 4, 1);
            }
            if (charged)
            {
                ArenKitSheets.Disc(cv, C(Fx[1]), mx, my, 2.6);
                ArenKitSheets.Disc(cv, C(Fx[0]), mx, my, 1.3);
                Leaf(cv, mx + pp.X * 5 + d.X * 6, my + pp.Y * 5 + d.Y * 6, 0);
                Leaf(cv, mx - pp.X * 5 + d.X * 9, my - pp.Y * 5 + d.Y * 9, 3);
                ArenKitSheets.Dots(cv, new[] { Fx[4], Fx[1] }, 200 + fr, mx + d.X * 6, my + d.Y * 6, 5, 5, 6);
            }
        }
        return cv;
    }

    // arrow trail 32x32, pivot (16,16) = arrow sprite pivot; trail starts at the arrow tail and runs back along -d
    public static ArenKitSheets.Cv Trail(int dir, bool charged, int fr)
    {
        var cv = new ArenKitSheets.Cv(32, 32, 0, 0);
        var d = EquipGen.Dir[dir]; bool diag = d.X != 0 && d.Y != 0;
        int tail = diag ? 3 : 4, len = charged ? (diag ? 10 : 14) : (diag ? 7 : 10);
        if (fr == 1) len = len * 2 / 3;
        double x0 = 16 - d.X * tail, y0 = 16 - d.Y * tail, x1 = x0 - d.X * len, y1 = y0 - d.Y * len;
        if (charged)
        {
            ArenKitSheets.Line(cv, C(Fx[3]), x0, y0, x1, y1, fr == 0 ? 3 : 2);
            ArenKitSheets.Line(cv, C(Fx[2]), x0, y0, x1 + d.X * 2, y1 + d.Y * 2, fr == 0 ? 2 : 1);
            ArenKitSheets.Line(cv, C(Fx[0]), x0, y0, x0 - d.X * len / 2, y0 - d.Y * len / 2, 1);
            var pp = new Point(-d.Y, d.X);
            int sgn = fr == 0 ? 1 : -1;
            Leaf(cv, x0 - d.X * (len * 0.4) + pp.X * 3 * sgn, y0 - d.Y * (len * 0.4) + pp.Y * 3 * sgn, fr);
            Leaf(cv, x0 - d.X * (len * 0.8) - pp.X * 3 * sgn, y0 - d.Y * (len * 0.8) - pp.Y * 3 * sgn, 2 + fr);
            cv.Set(x0 - d.X * (len * 0.6), y0 - d.Y * (len * 0.6) + (fr == 0 ? -2 : 2), C(Fx[4]));
        }
        else
        {
            if (fr == 0)
            {
                ArenKitSheets.Line(cv, C(Fx[2]), x0, y0, x1, y1, 1);
                ArenKitSheets.Line(cv, C(Fx[1]), x0, y0, x0 - d.X * len / 2, y0 - d.Y * len / 2, 1);
            }
            else for (int k = 0; k <= len; k += 2) cv.Set(x0 - d.X * k, y0 - d.Y * k, C(k < len / 2 ? Fx[2] : Fx[3]));
        }
        return cv;
    }

    // ---- skill sheets ----
    // 2 Fan Shot: muzzle burst of 5 rays (60 deg), 64x64 pivot (32,42); rows Down / Up / Side, 3 frames
    public static ArenKitSheets.Cv FanBurst(string v, int f)
    {
        var cv = Cell(64, 64, 32, 42);
        var d = AimDir(v); var g = Grip(v);
        double mx = g.X + d.X * 4, my = g.Y + d.Y * 4, baseAng = Math.Atan2(d.Y, d.X);
        foreach (int a in new[] { -30, -15, 0, 15, 30 })
        {
            double r = baseAng + a * Math.PI / 180, cx = Math.Cos(r), cy = Math.Sin(r);
            double s0 = 2 + f * 3, s1 = f == 0 ? 7 : f == 1 ? 12 : 13;
            if (f < 2)
            {
                ArenKitSheets.Line(cv, C(a == 0 ? Fx[1] : Fx[2]), mx + cx * s0, my + cy * s0, mx + cx * s1, my + cy * s1, 1);
                if (f == 0) cv.Set(mx + cx * s0, my + cy * s0, C(Fx[0]));
                else Leaf(cv, mx + cx * s1, my + cy * s1, (a + 30) / 15 % 4);
            }
            else for (double k = 9; k <= 13; k += 2) cv.Set(mx + cx * k, my + cy * k, C(Fx[3]));
        }
        if (f == 0) { ArenKitSheets.Disc(cv, C(Fx[1]), mx, my, 2.2); ArenKitSheets.Disc(cv, C(Fx[0]), mx, my, 1.1); }
        return cv;
    }

    // 3 Leap Back: leaf burst at take-off, body cell 32x32 (pivot 16,30), 3 frames
    public static ArenKitSheets.Cv LeafBurst(int f)
    {
        var cv = Cell(32, 32, 16, 30);
        double cx = 16, cy = 22, R = 4 + f * 4;
        for (int k = 0; k < 9; k++)
        {
            if (f == 2 && k % 2 == 1) continue;
            double a = k * Math.PI * 2 / 9 + f * 0.35;
            Leaf(cv, cx + Math.Cos(a) * R, cy + Math.Sin(a) * R * 0.8, k % 4);
        }
        if (f == 0) ArenKitSheets.Ring(cv, C(Fx[1]), cx, 29, 6, 1.0, 0.4, null);
        return cv;
    }

    // 3 Vine snare left on the ground: 64x40, pivot centre (32,20); 0 sprout, 1 grow, 2-3 hold loop, 4 wither
    public static ArenKitSheets.Cv VineSnare(int f)
    {
        var cv = new ArenKitSheets.Cv(64, 40, 0, 0);
        double cx = 32, cy = 20, R = 24, ys = 0.42;
        bool wither = f == 4;
        string edge = wither ? "7A6A50" : Fx[3], mid = wither ? "9A8A60" : Fx[2];
        double grow = f == 0 ? 0.35 : f == 1 ? 0.7 : 1.0;
        if (f >= 1) ArenKitSheets.Ring(cv, C(edge), cx, cy, R * grow, f == 1 ? 1.0 : 1.4, ys, (bx, by) => wither ? ((int)((Math.Atan2(by - cy, bx - cx) * 180 / Math.PI + 360) / 20) % 3) != 0 : true);
        for (int k = 0; k < 6; k++)
        {
            double a0 = k * Math.PI / 3 + (f == 3 ? 0.18 : 0);
            Point? last = null;
            for (double t = 0; t <= grow; t += 0.08)
            {
                double r = R * (1 - 0.45 * t), a = a0 + t * 1.4;
                var q = new Point((int)Math.Round(cx + Math.Cos(a) * r), (int)Math.Round(cy + Math.Sin(a) * r * ys));
                if (last.HasValue && last.Value == q) continue;
                cv.Set(q.X, q.Y, C(mid)); last = q;
            }
            if (last.HasValue && !wither && f >= 2) Leaf(cv, last.Value.X, last.Value.Y - 1, k % 4);
            if (f == 0) cv.Set(cx + Math.Cos(a0) * R * 0.35, cy + Math.Sin(a0) * R * 0.35 * ys - 1, C(Fx[1]));
        }
        return cv;
    }

    // 4 Arrow Rain ground marker: 80x48, pivot centre (40,24), radius 32 px (2 tiles); 2 frames (dash phase)
    public static ArenKitSheets.Cv RainZone(int f)
    {
        var cv = new ArenKitSheets.Cv(80, 48, 0, 0);
        double cx = 40, cy = 24;
        ArenKitSheets.Ring(cv, C(Fx[2]), cx, cy, 32, 1.3, 0.42, (bx, by) => ((int)((Math.Atan2(by - cy, bx - cx) * 180 / Math.PI + 360 + f * 10) / 20) % 2) == 0);
        ArenKitSheets.Ring(cv, C(Fx[3]), cx, cy, 27, 1.0, 0.42, (bx, by) => ((int)((Math.Atan2(by - cy, bx - cx) * 180 / Math.PI + 360 - f * 10) / 20) % 2) == 1);
        foreach (var t in new[] { 0, 90, 180, 270 })
        {
            double a = (t + f * 10) * Math.PI / 180;
            cv.Set(cx + Math.Cos(a) * 30, cy + Math.Sin(a) * 30 * 0.42, C(Fx[1]));
        }
        return cv;
    }

    // 4 single falling arrow: 16x32, pivot bottom (8,30); 0 falling, 1 impact spark, 2 fade
    public static ArenKitSheets.Cv RainArrow(int f)
    {
        var cv = new ArenKitSheets.Cv(16, 32, 0, 0);
        if (f == 0)
        {
            ArenKitSheets.Line(cv, C(Fx[1]), 12, 2, 10, 10, 1);
            for (int k = 0; k <= 12; k++)
            {
                double x = 10 - k * 0.25, y = 12 + k;
                cv.Set(x, y, C(k >= 11 ? "D8DCE4" : k <= 1 ? Fx[2] : "B07A48"));
            }
        }
        else
        {
            for (int k = 0; k <= 4; k++) cv.Set(9 - k * 0.25 + 0.5, 25 + k, C(k >= 4 ? "D8DCE4" : k <= 1 ? Fx[2] : "B07A48"));
            if (f == 1)
            {
                ArenKitSheets.Ring(cv, C(Fx[1]), 8, 29, 4, 1.0, 0.5, null);
                ArenKitSheets.Star(cv, new[] { Fx[0], Fx[1], Fx[2] }, 8, 28, 1);
                ArenKitSheets.Dots(cv, new[] { Fx[1], Fx[4] }, 300, 8, 26, 5, 3, 4);
            }
            else ArenKitSheets.Ring(cv, C(Fx[3]), 8, 29, 6, 1.0, 0.45, (bx, by) => ((int)bx % 2) == 0);
        }
        return cv;
    }

    // V Wind Step: wind wisps around the legs, body cell 32x32 (pivot 16,30), 4-frame loop
    public static ArenKitSheets.Cv WindAura(int f)
    {
        var cv = Cell(32, 32, 16, 30);
        for (int i = 0; i < 3; i++)
        {
            int y = 21 + i * 3, x0 = 4 + ((f * 4 + i * 5) % 10), len = 6 - i;
            for (int k = 0; k < len; k++) cv.Set(x0 + k, y - (k == len - 1 ? 1 : 0), C(k == len - 1 ? Fx[0] : k < 2 ? Fx[2] : Fx[1]));
            int x1 = 27 - ((f * 4 + i * 7) % 10);
            for (int k = 0; k < len - 2; k++) cv.Set(x1 - k, y + 1, C(k == 0 ? Fx[1] : Fx[2]));
        }
        return cv;
    }

    // V dodge trigger: leaf swirl around the body, body cell 32x32, 3 frames
    public static ArenKitSheets.Cv DodgePuff(int f)
    {
        var cv = Cell(32, 32, 16, 30);
        double cx = 16, cy = 20, R = 6 + f * 3;
        ArenKitSheets.Ring(cv, C(f == 0 ? Fx[0] : f == 1 ? Fx[1] : Fx[3]), cx, cy, R, 1.0, 1.0, (bx, by) => ((int)((Math.Atan2(by - cy, bx - cx) * 180 / Math.PI + 360 + f * 25) / 30) % 2) == 0);
        for (int k = 0; k < 5; k++) { if (f == 2 && k % 2 == 0) continue; double a = k * Math.PI * 2 / 5 + f * 0.5; Leaf(cv, cx + Math.Cos(a) * (R + 1), cy + Math.Sin(a) * (R + 1), k % 4); }
        return cv;
    }

    // C Forest Pierce beam: 32x32 cells, horizontal art pointing RIGHT (row 0) and vertical pointing DOWN (row 1).
    // cols: Start f0-f2 | Mid f0-f2 (tileable along the beam: touches both cell ends on purpose) | End f0-f2.
    static void BeamBand(Color?[,] px, int x0, int x1, int f, bool tile)
    {
        int cy = 16; int[] half = { 3, 2, 1, 0 }; int wob = f == 1 ? 0 : (f == 0 ? 0 : -1);
        for (int layer = 0; layer < 4; layer++)
        {
            int h = half[layer] + (layer == 0 ? wob + (f == 0 ? 1 : 0) : 0);
            string col = layer == 0 ? Fx[3] : layer == 1 ? Fx[2] : layer == 2 ? Fx[1] : Fx[0];
            for (int x = x0; x <= x1; x++) for (int y = cy - h; y <= cy + h; y++)
                    if (tile || (x > 0 && x < 31)) if (y > 0 && y < 31) px[x, y] = C(col);
        }
        // spiralling leaves (period 16 -> seamless every 32 px) + gold sparks
        for (int x = x0; x <= x1; x++)
        {
            if (!tile && (x <= 0 || x >= 31)) continue;
            if ((x + f * 4) % 8 == 0)
            {
                int y = cy + (int)Math.Round(6 * Math.Sin((x + f * 4) * Math.PI * 2 / 16));
                if (y > 0 && y < 31) { px[x, y] = C(Fx[1]); if (x + 1 <= x1 && (tile || x + 1 < 31)) px[x + 1, y] = C(Fx[2]); }
            }
            if ((x + f * 5) % 11 == 0) { int y = cy + ((x / 11) % 2 == 0 ? -5 : 5); if (y > 0 && y < 31) px[x, y] = C(Fx[4]); }
        }
    }

    public static Color?[,] Beam(int part, int f)
    {
        var px = new Color?[32, 32];
        if (part == 0)
        {
            BeamBand(px, 8, 31, f, true);   // joins the first Mid tile at x 31
            var cv = new ArenKitSheets.Cv(32, 32, 0, 0);
            ArenKitSheets.Disc(cv, C(Fx[2]), 8, 16, 6 - f * 0.5);
            ArenKitSheets.Disc(cv, C(Fx[1]), 8, 16, 4 - f * 0.5);
            ArenKitSheets.Disc(cv, C(Fx[0]), 8, 16, 2);
            if (f == 0) ArenKitSheets.Star(cv, new[] { Fx[0], Fx[4], Fx[1] }, 8, 16, 3);
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) if (cv.Px[x, y].HasValue) px[x, y] = cv.Px[x, y];
        }
        else if (part == 1) BeamBand(px, 0, 31, f, true);
        else
        {
            BeamBand(px, 0, 20, f, true);   // joins the last Mid tile at x 0
            for (int k = 0; k <= 7; k++)
            {
                int h = Math.Max(0, 4 - k / 2);
                for (int y = 16 - h; y <= 16 + h; y++) px[20 + k, y] = C(k >= 6 ? Fx[0] : Math.Abs(y - 16) == h ? Fx[2] : Fx[1]);
            }
            var cv = new ArenKitSheets.Cv(32, 32, 0, 0);
            ArenKitSheets.Dots(cv, new[] { Fx[4], Fx[1], Fx[0] }, 400 + f, 24, 16, 6, 8, 6);
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) if (cv.Px[x, y].HasValue) px[x, y] = cv.Px[x, y];
        }
        return px;
    }

    // ================= preview (right-facing, unmirrored) =================
    public static Bitmap Preview(string anim, string v, int f, int step, bool withShot)
    {
        var body = Body32(anim, v, f);
        P p = anim == "Attack" ? Shot(v, f) : Move(anim, v, f);
        var behind = new List<EquipGen.Layer>(); var front = new List<EquipGen.Layer>();
        (p.MO > 0 ? front : behind).Add(new EquipGen.Layer { S = BowSprite(p.MI), X = p.MX, Y = p.MY });
        if (p.Hand) (p.MO > 0 ? front : behind).Add(new EquipGen.Layer { S = LyraGen.Hand, X = p.MX - 1, Y = p.MY - 1 });
        if (p.OX > -99) (p.OO > 0 ? front : behind).Add(new EquipGen.Layer { S = LyraGen.Hand, X = p.OX - 1, Y = p.OY - 1 });
        var bmp = EquipGen.Compose(body, behind, front);
        if (anim == "Attack" && f > 0)
        {
            var cv = ComboVfx(v, step, f - 1);
            for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++)
                {
                    if (!cv.Px[x, y].HasValue) continue;
                    if (v == "Up" && bmp.GetPixel(x, y).A > 0) continue;
                    bmp.SetPixel(x, y, cv.Px[x, y].Value);
                }
        }
        if (withShot && anim == "Attack" && f == 2)
        {
            // arrow + trail in flight, 12 px past the muzzle (illustration only; runtime spawns a projectile)
            var d = AimDir(v); var g = Grip(v); int dir = v == "Down" ? 6 : v == "Up" ? 2 : 0;
            int ax = 8 + g.X + d.X * 14, ay = 8 + g.Y + d.Y * 14;
            var tr = Trail(dir, step == 2, 0); var ar = LyraGen.Arrow(dir, step == 2, 0);
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) { int X = ax - 16 + x, Y = ay - 16 + y; if (tr.Px[x, y].HasValue && X >= 0 && Y >= 0 && X < 48 && Y < 48) bmp.SetPixel(X, Y, tr.Px[x, y].Value); }
            for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) { int X = ax - 8 + x, Y = ay - 8 + y; var c = ar.GetPixel(x, y); if (c.A > 0 && X >= 0 && Y >= 0 && X < 48 && Y < 48) bmp.SetPixel(X, Y, c); }
        }
        return bmp;
    }

    // ================= export =================
    public class SheetInfo { public string File; public int CellW, CellH, Cols, Rows, PivotX, PivotY; public string Layout; }
    public static readonly List<SheetInfo> Manifest = new List<SheetInfo>();

    static void SaveCells(string dir, string file, List<Color?[,]> cells, int cw, int ch, int cols, int pivX, int pivY, string layout, bool[] mirror = null)
    {
        int rows = (cells.Count + cols - 1) / cols;
        var bmp = new Bitmap(cw * cols, ch * rows, PixelFormat.Format32bppArgb);
        for (int i = 0; i < cells.Count; i++)
        {
            int ox = (i % cols) * cw, oy = (i / cols) * ch; bool m = mirror != null && mirror[i];
            if (cells[i] == null) continue;
            for (int y = 0; y < ch; y++) for (int x = 0; x < cw; x++)
                    if (cells[i][x, y].HasValue) bmp.SetPixel(ox + (m ? cw - 1 - x : x), oy + y, cells[i][x, y].Value);
        }
        bmp.Save(Path.Combine(dir, file), ImageFormat.Png); bmp.Dispose();
        Manifest.Add(new SheetInfo { File = file.Replace('\\', '/'), CellW = cw, CellH = ch, Cols = cols, Rows = rows, PivotX = pivX, PivotY = pivY, Layout = layout });
    }

    static Color?[,] Px(ArenKitSheets.Cv cv) { return cv.Px; }
    static Color?[,] Px(Bitmap b) { var p = new Color?[b.Width, b.Height]; for (int y = 0; y < b.Height; y++) for (int x = 0; x < b.Width; x++) { var c = b.GetPixel(x, y); if (c.A > 0) p[x, y] = c; } return p; }
    static Color?[,] Px(EquipGen.Spr s, int w, int h, int ax, int ay) { var p = new Color?[w, h]; foreach (var kv in s.Px) { int x = ax + kv.Key.X, y = ay + kv.Key.Y; if (x >= 0 && y >= 0 && x < w && y < h) p[x, y] = kv.Value; } return p; }
    static Color?[,] Transpose(Color?[,] a) { var p = new Color?[32, 32]; for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) p[y, x] = a[x, y]; return p; }

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
        int mx = p.MX, mi = p.MI, hx = -99, hy = 0, ox = -99, oy = 0;
        if (m && mx > -99) { mx = 31 - mx; if (mi < 24) mi = (mi / 8) * 8 + MirrorDir[mi % 8]; }
        if (p.Hand && p.MX > -99) { hx = m ? 31 - p.MX : p.MX - 1; hy = p.MY - 1; }
        if (p.OX > -99) { ox = m ? 31 - p.OX : p.OX - 1; oy = p.OY - 1; }
        // key,main_x,main_y,main_dir,main_order,hand_x,hand_y,off_x,off_y,off_view,off_order,slash,slash_order,off_dir
        return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},-1\n", key, mx, p.MY, mi, p.MO, hx, hy, ox, oy, p.OX > -99 ? "Hand" : v, p.OO, slash, slashOrder);
    }

    public static string Run(string outDir)
    {
        Manifest.Clear();
        foreach (var sub in new[] { "Extra", "Gear", "Skills" }) Directory.CreateDirectory(Path.Combine(outDir, sub));
        // body sheet (same layout as Aren / Vex / Brom)
        var sheet = new Bitmap(128, 288, PixelFormat.Format32bppArgb);
        var anims = new[] { "Idle", "Walk", "Attack" };
        for (int a = 0; a < 3; a++) for (int vi = 0; vi < 3; vi++) for (int f = 0; f < (a == 2 ? 3 : 4); f++)
                    PaintCell(sheet, f * 32, (a * 3 + vi) * 32, Body32(anims[a], Views[vi], f), Views[vi] == "Side", null);
        sheet.Save(Path.Combine(outDir, "Lyra_Sheet.png"), ImageFormat.Png); sheet.Dispose();
        Manifest.Add(new SheetInfo { File = "Lyra_Sheet.png", CellW = 32, CellH = 32, Cols = 4, Rows = 9, PivotX = 16, PivotY = 30, Layout = "rows Idle D/U/S, Walk D/U/S, Attack D/U/S (Attack 3 cols: nock, draw, release; arms baked in); Side faces left" });
        foreach (var v in Views)
            for (int f = 0; f < 2; f++)
            {
                var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
                PaintCell(bmp, 0, 0, Body32(f == 0 ? "Idle" : "Walk", v, 0), v == "Side", f == 0 ? Tint(Color.White, 0.8) : Tint(C("FF3030"), 0.35));
                bmp.Save(Path.Combine(outDir, "Extra", "Lyra_Hurt_" + v + "_" + f + ".png"), ImageFormat.Png); bmp.Dispose();
            }
        {
            var d0 = new Bitmap(32, 32, PixelFormat.Format32bppArgb); PaintCell(d0, 0, 0, EquipGen.Body(Kneel(), LyraGen.Pal), false, null);
            d0.Save(Path.Combine(outDir, "Extra", "Lyra_Death_Down_0.png"), ImageFormat.Png); d0.Dispose();
            var lying = FxGen.Lying(LyraGen.Down);
            for (int f = 1; f <= 2; f++)
            {
                var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
                for (int y = 0; y < 14; y++) for (int x = 0; x < 21; x++) if (lying[x, y] != '.') bmp.SetPixel(5 + x, 16 + y, C(LyraGen.Pal[lying[x, y]]));
                bmp.Save(Path.Combine(outDir, "Extra", "Lyra_Death_Down_" + f + ".png"), ImageFormat.Png); bmp.Dispose();
            }
        }
        foreach (var file in new[] { "Lyra_Hurt_Down_0", "Lyra_Hurt_Down_1", "Lyra_Hurt_Up_0", "Lyra_Hurt_Up_1", "Lyra_Hurt_Side_0", "Lyra_Hurt_Side_1", "Lyra_Death_Down_0", "Lyra_Death_Down_1", "Lyra_Death_Down_2" })
            Manifest.Add(new SheetInfo { File = "Extra/" + file + ".png", CellW = 32, CellH = 32, Cols = 1, Rows = 1, PivotX = 16, PivotY = 30, Layout = file.Contains("Hurt") ? "hurt: 0 white flash, 1 red tint" : "death: 0 kneel, 1 fall, 2 lying (hold)" });

        // gear
        var bows = new List<Color?[,]>();
        for (int i = 0; i < 25; i++) bows.Add(Px(BowSprite(i), 32, 32, 16, 16));
        SaveCells(Path.Combine(outDir, "Gear"), "Lyra_Bow.png", bows, 32, 32, 8, 16, 16, "index = state*8 + dir (dir R UR U UL L DL D DR; state 0 rest/release, 1 nock, 2 full draw); 24 = back-slung tip (Down idle); pivot = grip");
        Manifest[Manifest.Count - 1].File = "Gear/Lyra_Bow.png";
        SaveCells(Path.Combine(outDir, "Gear"), "Lyra_Hand.png", new List<Color?[,]> { Px(LyraGen.Hand, 4, 4, 1, 1) }, 4, 4, 1, 1, 1, "fist: bow hand (hand slot) and draw hand (off slot, off_view=Hand)");
        Manifest[Manifest.Count - 1].File = "Gear/Lyra_Hand.png";
        var arrows = new List<Color?[,]>();
        for (int r = 0; r < 3; r++) for (int d = 0; d < 8; d++) arrows.Add(Px(LyraGen.Arrow(d, r > 0, r == 2 ? 1 : 0)));
        SaveCells(Path.Combine(outDir, "Gear"), "Lyra_Arrow.png", arrows, 16, 16, 8, 8, 8, "rows: normal | charged f0 | charged f1; cols dir R UR U UL L DL D DR (drawn, never rotated)");
        Manifest[Manifest.Count - 1].File = "Gear/Lyra_Arrow.png";
        var trails = new List<Color?[,]>();
        for (int r = 0; r < 4; r++) for (int d = 0; d < 8; d++) trails.Add(Px(Trail(d, r >= 2, r % 2)));
        SaveCells(Path.Combine(outDir, "Gear"), "Lyra_ArrowTrail.png", trails, 32, 32, 8, 16, 16, "rows: normal f0 | normal f1 | charged f0 | charged f1; cols dir; pivot = arrow pivot");
        Manifest[Manifest.Count - 1].File = "Gear/Lyra_ArrowTrail.png";
        var combo = new List<Color?[,]>(); var mir = new List<bool>();
        for (int step = 0; step < 3; step++) foreach (var v in Views) for (int fr = 0; fr < 2; fr++) { combo.Add(Px(ComboVfx(v, step, fr))); mir.Add(v == "Side"); }
        SaveCells(Path.Combine(outDir, "Gear"), "Lyra_ComboVfx.png", combo, 48, 48, 18, 24, 38, "18 cells: step*6 + view*2 + frame (views Down/Up/Side; frame 0 = draw F1, 1 = release F2; Side faces left)", mir.ToArray());
        Manifest[Manifest.Count - 1].File = "Gear/Lyra_ComboVfx.png";

        // skills
        var sk = Path.Combine(outDir, "Skills");
        Action<string, List<Color?[,]>, int, int, int, int, int, string, bool[]> save = (file, cells, cw, ch, cols, px, py, layout, m) =>
        { SaveCells(sk, file, cells, cw, ch, cols, px, py, layout, m); Manifest[Manifest.Count - 1].File = "Skills/" + file; };
        var l = new List<Color?[,]>(); var lm = new List<bool>();
        foreach (var v in Views) for (int f = 0; f < 3; f++) { l.Add(Px(FanBurst(v, f))); lm.Add(v == "Side"); }
        save("Lyra_FanBurst.png", l, 64, 64, 3, 32, 42, "2 Ten Quat: rows Down/Up/Side (Side faces left), 3 frames; muzzle burst, 5 arrows are runtime projectiles", lm.ToArray());
        l = new List<Color?[,]>(); for (int f = 0; f < 3; f++) l.Add(Px(LeafBurst(f)));
        save("Lyra_LeafBurst.png", l, 32, 32, 3, 16, 30, "3 Nhay Lui: leaf burst at take-off, 3 frames", null);
        l = new List<Color?[,]>(); for (int f = 0; f < 5; f++) l.Add(Px(VineSnare(f)));
        save("Lyra_VineSnare.png", l, 64, 40, 5, 32, 20, "3 Nhay Lui: vine snare on the ground (radius 24 px = 1.5 tiles); 0 sprout, 1 grow, 2-3 hold loop, 4 wither; BEHIND characters", null);
        l = new List<Color?[,]>(); for (int f = 0; f < 2; f++) l.Add(Px(RainZone(f)));
        save("Lyra_RainZone.png", l, 80, 48, 2, 40, 24, "4 Mua Ten: target zone (radius 32 px = 2 tiles), 2-frame loop; ground layer", null);
        l = new List<Color?[,]>(); for (int f = 0; f < 3; f++) l.Add(Px(RainArrow(f)));
        save("Lyra_RainArrow.png", l, 16, 32, 3, 8, 30, "4 Mua Ten: one falling arrow; 0 falling, 1 impact, 2 fade; pivot = impact point", null);
        l = new List<Color?[,]>(); for (int f = 0; f < 4; f++) l.Add(Px(WindAura(f)));
        save("Lyra_WindAura.png", l, 32, 32, 4, 16, 30, "V Buoc Gio: wind wisps at the legs, 4-frame loop while the buff lasts", null);
        l = new List<Color?[,]>(); for (int f = 0; f < 3; f++) l.Add(Px(DodgePuff(f)));
        save("Lyra_DodgePuff.png", l, 32, 32, 3, 16, 30, "V Buoc Gio: dodge triggered (hit negated), 3 frames", null);
        l = new List<Color?[,]>();
        for (int o = 0; o < 2; o++) for (int part = 0; part < 3; part++) for (int f = 0; f < 3; f++) { var b = Beam(part, f); l.Add(o == 0 ? b : Transpose(b)); }
        save("Lyra_Beam.png", l, 32, 32, 9, 16, 16, "C Ten Xuyen Rung: row 0 horizontal (points RIGHT, flipX for left), row 1 vertical (points DOWN, flipY for up); cols Start f0-2 | Mid f0-2 (tile every 32 px) | End f0-2", null);

        // anchors
        var csv = new StringBuilder("key,main_x,main_y,main_dir,main_order,hand_x,hand_y,off_x,off_y,off_view,off_order,slash,slash_order,off_dir\n");
        foreach (var anim in new[] { "Idle", "Walk" }) foreach (var v in Views) for (int f = 0; f < 4; f++) csv.Append(CsvRow(anim + "_" + v + "_" + f, v, Move(anim, v, f), -1, 0));
        for (int vi = 0; vi < 3; vi++)
        {
            var v = Views[vi]; int so = v == "Up" ? -3 : 3;
            csv.Append(CsvRow("Attack_" + v + "_0", v, Shot(v, 0), -1, 0));
            csv.Append(CsvRow("Attack_" + v + "_1", v, Shot(v, 1), vi * 2, so));
            csv.Append(CsvRow("Attack_" + v + "_2", v, Shot(v, 2), vi * 2 + 1, so));
            for (int step = 0; step < 3; step++)
            {
                csv.Append(CsvRow("Attack_" + v + "_1@" + step, v, Shot(v, 1), vi * 2, so));
                csv.Append(CsvRow("Attack_" + v + "_2@" + step, v, Shot(v, 2), vi * 2 + 1, so));
            }
            csv.Append(CsvRow("Hurt_" + v + "_0", v, Move("Idle", v, 0), -1, 0));
            csv.Append(CsvRow("Hurt_" + v + "_1", v, Move("Walk", v, 0), -1, 0));
        }
        csv.Append(CsvRow("Death_Down_0", "Down", Rest("Down", 2), -1, 0));
        csv.Append("Death_Down_1,-99,0,0,0,-99,0,-99,0,Down,0,-1,0,-1\n");
        csv.Append(CsvRow("Death_Down_2", "Down", new P { MX = 25, MY = 28, MI = 2, MO = 2 }, -1, 0));
        File.WriteAllText(Path.Combine(outDir, "Lyra_Anchors.csv"), csv.ToString(), new UTF8Encoding(false));

        // manifest
        var mf = new StringBuilder("file,cell_w,cell_h,cols,rows,pivot_px_x,pivot_px_y_from_top,unity_pivot_x,unity_pivot_y,layout\n");
        foreach (var s in Manifest)
            mf.AppendFormat(System.Globalization.CultureInfo.InvariantCulture, "{0},{1},{2},{3},{4},{5},{6},{7:0.####},{8:0.####},\"{9}\"\n", s.File, s.CellW, s.CellH, s.Cols, s.Rows, s.PivotX, s.PivotY, s.PivotX / (double)s.CellW, (s.CellH - s.PivotY) / (double)s.CellH, s.Layout);
        File.WriteAllText(Path.Combine(outDir, "LYRA_MANIFEST.csv"), mf.ToString(), new UTF8Encoding(false));
        return "ok";
    }
}
