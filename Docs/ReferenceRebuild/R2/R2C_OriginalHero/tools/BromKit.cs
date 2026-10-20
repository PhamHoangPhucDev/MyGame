using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

// Brom (dwarf tank) full art set — approved candidate brom/BROM_CANDIDATE_v1.png.
// Body frames, hammer (8 true poses), shield views, glove, per-step anchors, combo + skill VFX, Unity-ready export.
// Conventions = Aren/Vex: 32x32 body cell, sole row 29, pivot (0.5, 0.0625); Side rows/sprites MIRRORED (face left).
public static class BromKit
{
    const int W = 14, H = 21;
    static readonly string[] Views = { "Down", "Up", "Side" };
    static readonly int[] MirrorDir = { 4, 3, 2, 1, 0, 7, 6, 5 };
    public static readonly string[] Fx = { "FFF0C8", "F0A050", "C8692E", "7A4426", "D8D8E0" };   // core, light, mid, edge, spark
    static readonly string[] Rock = { "8A8A8A", "6A6A6A", "A8A8A8" };
    static readonly string[] Steel = { "F0F4F8", "C8D0DC", "8A94A4", "5A6070", "3A3E48" };
    static Color C(string h) { return HeroConceptGen.Hex(h); }

    // ================= body =================
    static char[,] G(string[] rows) { return BromGen.Grid(rows); }
    static void Row(char[,] g, int y, string s) { for (int x = 0; x < W; x++) g[x, y] = s[x]; }

    static void Nod(char[,] g, string[] b)   // breath: helm + beard rows 0-7 drop 1 px
    {
        for (int y = 8; y >= 1; y--) for (int x = 0; x < W; x++) { char s = b[y - 1][x]; g[x, y] = (y == 8 && s == '.') ? b[8][x] : s; }
        Row(g, 0, "..............");
    }

    static void Drop(char[,] g, string[] b)
    {
        for (int y = 18; y >= 1; y--) for (int x = 0; x < W; x++) g[x, y] = b[y - 1][x];
        Row(g, 0, "..............");
    }

    // Idle: beard sway (F1 / F3), breath nod (F2). 0.25 / 0.15 / 0.25 / 0.15 s.
    public static char[,] Idle(string v, int f)
    {
        var b = BromGen.Base(v); var g = G(b);
        if (f == 2) { Nod(g, b); return g; }
        if (v == "Down")
        {
            // beard tip (row 14 "KK" at cols 6-7) swings one pixel left / right
            if (f == 1) { g[5, 14] = 'K'; g[7, 14] = 'a'; }
            if (f == 3) { g[8, 14] = 'K'; g[6, 14] = 'a'; }
        }
        else if (v == "Side")
        {
            // beard front edge (col 13) flutters
            if (f == 1) { g[13, 10] = '.'; g[12, 10] = 'K'; }
            if (f == 3) { g[13, 9] = '.'; g[12, 9] = 'K'; }
        }
        else
        {
            // beard tips peeking at the sides of the back
            if (f == 1) { g[0, 9] = '.'; g[1, 9] = 'K'; }
            if (f == 3) { g[13, 9] = '.'; g[12, 9] = 'K'; }
        }
        return g;
    }

    // Walk: F0/F2 contact (drop 1 px, one boot planted on row 29, the other lifted), F1/F3 passing. 4 x 0.125 s.
    public static char[,] Walk(string v, int f)
    {
        var b = BromGen.Base(v); var g = G(b);
        bool side = v == "Side";
        if (f == 0 || f == 2)
        {
            Drop(g, b);
            if (side)
            {
                if (f == 0) { Row(g, 19, ".kBBk...kAAk.."); Row(g, 20, "kBBBBk..kBBBBk"); }
                else { Row(g, 19, "..kAAk.kBBk..."); Row(g, 20, ".kBBBBkkBBBBk."); }
            }
            else
            {
                if (f == 0) { Row(g, 19, "..kAAk..kBBk.."); Row(g, 20, ".kBBBBk..kk..."); }
                else { Row(g, 19, "..kBBk..kAAk.."); Row(g, 20, "...kk..kBBBBk."); }
            }
        }
        else if (f == 1) return Idle(v, 1);
        else return Idle(v, 3);
        return g;
    }

    // Attack (3 frames): F0 windup = base (hammer raised by anchors), F1 strike = braced drop, F2 recover = base.
    public static char[,] Attack(string v, int f) { return f == 1 ? BromGen.Strike(v) : G(BromGen.Base(v)); }

    public static char[,] Kneel()
    {
        var b = G(BromGen.Down);
        var g = new char[W, H];
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) g[x, y] = '.';
        for (int y = 0; y <= 17; y++) for (int x = 0; x < W; x++) g[x, y + 2] = b[x, y];
        string knee = ".kBBBBkkBBBBk.";
        for (int x = 0; x < W; x++) g[x, 20] = knee[x];
        return g;
    }

    // ================= gear =================
    // Hammer with a real 8-direction head: shaft along d, head = rotated block (|u|<=2.5 across, |v|<=1.6 along).
    public static EquipGen.Spr Hammer(int dir)
    {
        var d = EquipGen.Dir[dir];
        double len = Math.Sqrt(d.X * d.X + d.Y * d.Y), dx = d.X / len, dy = d.Y / len, px = -dy, py = dx;
        var s = new EquipGen.Spr();
        bool diag = d.X != 0 && d.Y != 0;
        int shaft = diag ? 6 : 7;
        for (int k = -1; k <= shaft; k++) s.Set(d.X * k, d.Y * k, C(k % 3 == 0 ? "A06A40" : "7A4E2E"));
        double hc = (shaft + 2.2) * len, cx = dx * hc, cy = dy * hc;
        for (int y = (int)cy - 5; y <= (int)cy + 5; y++)
            for (int x = (int)cx - 5; x <= (int)cx + 5; x++)
            {
                double ox = x + 0.0 - cx, oy = y + 0.0 - cy, u = ox * px + oy * py, v = ox * dx + oy * dy;
                if (Math.Abs(u) > 2.6 || Math.Abs(v) > 1.7) continue;
                // light from top-left: higher screen y = darker
                string c = (u < -1.6 || v < -1.0) ? Steel[1] : (u > 1.6 || v > 1.0) ? Steel[3] : Steel[2];
                if (Math.Abs(u) < 0.6 && Math.Abs(v) < 0.6) c = "E8C048";   // gold band on the head
                s.Set(x, y, C(c));
            }
        return EquipGen.Outline(s, C("1C1A22"));
    }

    public static readonly EquipGen.Spr Glove = EquipGen.Hand("8A5A34", "5A3A22", "1C1A22");

    public class P { public int MX = -99, MY, MD, MO, OX = -99, OY, OO = 1; public bool Hand = true; }
    static P Pz(int mx, int my, int md, int mo, int ox, int oy, int oo = 1) { return new P { MX = mx, MY = my, MD = md, MO = mo, OX = ox, OY = oy, OO = oo }; }

    public static P Rest(string v)
    {
        if (v == "Down") return Pz(6, 24, 2, -2, 19, 23);
        if (v == "Up") return Pz(25, 24, 2, 2, 8, 23);
        return Pz(11, 24, 2, -2, 21, 23);
    }

    public static P Move(string anim, string v, int f) { var r = Rest(v); if (anim == "Walk" && (f == 0 || f == 2)) { r.MY++; r.OY++; } return r; }

    public static P Windup(string v)
    {
        if (v == "Down") return Pz(8, 18, 3, -2, 19, 23);
        if (v == "Up") return Pz(24, 18, 1, 2, 8, 23);
        return Pz(10, 18, 3, -2, 21, 23);
    }

    // step 0 horizontal swing, 1 overhead smash, 2 ground pound (finisher)
    public static P Strike(string v, int step)
    {
        if (v == "Down")
            return step == 0 ? Pz(12, 24, 0, 2, 20, 23) : step == 1 ? Pz(8, 24, 6, 2, 20, 23) : Pz(13, 25, 6, 2, 20, 24);
        if (v == "Up")
            return step == 0 ? Pz(12, 18, 4, -2, 8, 24) : step == 1 ? Pz(16, 16, 2, -2, 8, 24) : Pz(16, 18, 2, -2, 8, 24);
        return step == 0 ? Pz(20, 24, 0, 2, 18, 24) : step == 1 ? Pz(19, 22, 7, 2, 18, 24) : Pz(19, 24, 7, 2, 18, 25);
    }

    public static P Recover(string v) { var r = Rest(v); r.MY++; r.OY++; return r; }

    // ================= VFX helpers =================
    static ArenKitSheets.Cv Cell(int w, int h, int pivX, int pivY) { return ArenKitSheets.BodyCell(w, h, pivX, pivY); }

    static void Crack(ArenKitSheets.Cv cv, Color c, int seed, double cx, double cy, int n, int len, double yScale)
    {
        var rnd = new Random(seed);
        for (int k = 0; k < n; k++)
        {
            double a = (k + rnd.NextDouble() * 0.6) / n * Math.PI * 2, x = cx, y = cy;
            for (int i = 0; i < len; i++) { a += (rnd.NextDouble() - 0.5) * 0.8; x += Math.Cos(a); y += Math.Sin(a) * yScale; cv.Set(x, y, c); }
        }
    }

    // combo VFX cell 48x48, pivot = body pivot (24,38) -> body offset (8,8); right-facing art (Side mirrored on export)
    public static ArenKitSheets.Cv ComboVfx(string v, int step, int fr)
    {
        var cv = Cell(48, 48, 24, 38);
        bool strong = fr == 0;
        // hammer head landing point per view/step (body coords)
        double hx, hy;
        if (v == "Down") { hx = step == 0 ? 24 : (step == 1 ? 8 : 13); hy = step == 0 ? 25 : 33; }
        else if (v == "Up") { hx = step == 0 ? 3 : 16; hy = step == 0 ? 18 : 6; }
        else { hx = step == 0 ? 30 : 27; hy = step == 0 ? 24 : 30; }
        if (step == 0)
        {
            // horizontal swing arc (short, heavy) + dust
            double cx = v == "Up" ? 16 : 16, cy = v == "Up" ? 16 : 25, tip = v == "Down" ? 0 : (v == "Up" ? 180 : 0);
            double sweep = v == "Up" ? 110 : -110;
            if (strong) ArenKitSheets.Crescent(cv, Fx, cx, cy, 13, tip, sweep, 3.6, v == "Down" ? 0.55 : 1.0);
            else ArenKitSheets.Crescent(cv, Fx, cx, cy, 14, tip, sweep * 0.6, 1.6, v == "Down" ? 0.55 : 1.0, 0.0, true);
            ArenKitSheets.Dots(cv, new[] { Fx[1], Fx[2], Fx[3] }, 100 + step + fr, hx, hy, strong ? 5 : 7, 4, strong ? 8 : 5);
        }
        else if (step == 1)
        {
            // impact puff + small crack where the head lands
            double r = strong ? 3.5 : 5;
            ArenKitSheets.Disc(cv, C(strong ? Fx[2] : Fx[3]), hx, hy, r);
            ArenKitSheets.Disc(cv, C(strong ? Fx[1] : Fx[2]), hx - 1, hy - 1, r - 1.5);
            if (strong) ArenKitSheets.Disc(cv, C(Fx[0]), hx - 1, hy - 1.5, 1.2);
            if (v != "Up") Crack(cv, C(Fx[3]), 7 + fr, hx, hy + 1, 4, strong ? 4 : 6, 0.45);
            ArenKitSheets.Dots(cv, new[] { Rock[0], Rock[1], Fx[4] }, 110 + fr, hx, hy - 3, 6, 4, strong ? 8 : 4, strong ? 2 : 1);
        }
        else
        {
            // finisher ground pound: ring + cracks around the feet + sparks
            double cx = 16, cy = 29;
            ArenKitSheets.Ring(cv, C(strong ? Fx[2] : Fx[3]), cx, cy, strong ? 10 : 15, strong ? 2.2 : 1.4, 0.42, null);
            if (strong) ArenKitSheets.Ring(cv, C(Fx[1]), cx, cy, 6, 1.3, 0.42, null);
            Crack(cv, C(Fx[3]), 21 + fr, cx, cy, 6, strong ? 9 : 12, 0.45);
            ArenKitSheets.Dots(cv, new[] { Fx[4], Fx[1], Rock[0] }, 120 + fr, cx, cy - 8, 14, 8, strong ? 14 : 6, strong ? 2 : 1);
        }
        return cv;
    }

    // ---- skill sheets ----
    // 2 Ground Slam: 80x56, pivot (40,36), 4 frames (impact, spread, wide, fade)
    public static ArenKitSheets.Cv GroundSlam(int f)
    {
        var cv = Cell(80, 56, 40, 36);
        double cx = 16, cy = 29, R = 8 + f * 5;
        ArenKitSheets.Ring(cv, C(f < 3 ? Fx[2] : Fx[3]), cx, cy, R, f < 2 ? 2.6 : 1.6, 0.42, null);
        if (f < 3) ArenKitSheets.Ring(cv, C(Fx[1]), cx, cy, R - 4, 1.4, 0.42, null);
        if (f == 0) ArenKitSheets.Disc(cv, C(Fx[0]), cx, cy, 2.5);
        Crack(cv, C(Fx[3]), 30, cx, cy, 8, 6 + f * 3, 0.45);
        if (f < 3) ArenKitSheets.Dots(cv, new[] { Rock[0], Rock[1], Rock[2], Fx[2] }, 31 + f, cx, cy - 6 - f * 3, 8 + f * 4, 5 + f * 2, 10 + f * 2, 2);
        else ArenKitSheets.Dots(cv, new[] { Fx[3], Rock[1] }, 34, cx, cy, R, R * 0.42, 10);
        return cv;
    }

    // 3 Shield Charge: shield glow 24x24 (3 frames, pivot centre) — gold rim flare around the round shield
    public static ArenKitSheets.Cv ShieldGlow(int f)
    {
        var cv = new ArenKitSheets.Cv(24, 24, 0, 0);
        double r = 7 + f * 1.5;
        ArenKitSheets.Ring(cv, C(f == 0 ? "FFF4C8" : (f == 1 ? "F0C850" : "C89A30")), 12, 12, r, f == 2 ? 1.0 : 1.6, 1.0, null);
        if (f < 2) ArenKitSheets.Star(cv, new[] { "FFFFFF", "FFF4C8", "F0C850" }, 7, 7, f == 0 ? 3 : 2);
        return cv;
    }

    // 3 Shield Charge dust trail: 16x16, 3 frames, pivot bottom centre (brown, heavier than Aren's)
    public static ArenKitSheets.Cv Dust(int f)
    {
        var cv = new ArenKitSheets.Cv(16, 16, 0, 0);
        double r = 3 + f * 1.1;
        foreach (var sp in new[] { new[] { 8.0, 12.0, 1.0 }, new[] { 4.5, 13.0, 0.7 }, new[] { 11.5, 13.0, 0.7 } })
        {
            if (f == 2 && sp[2] == 1.0) continue;
            ArenKitSheets.Disc(cv, C(f < 2 ? "A08060" : "806450"), sp[0], sp[1] - f, r * sp[2]);
            ArenKitSheets.Disc(cv, C("C8A880"), sp[0] - 0.8, sp[1] - f - 0.8, Math.Max(0.6, r * sp[2] - 1.3));
        }
        return cv;
    }

    // 4 Roar (taunt): 64x64, pivot (32,48) -> body offset (16,18); sound rings from the head, 4 frames
    public static ArenKitSheets.Cv Roar(int f)
    {
        var cv = Cell(64, 64, 32, 48);
        double cx = 16, cy = 12;
        for (int k = 0; k < 3; k++)
        {
            double R = 7 + f * 5 + k * 6;
            if (R > 29) continue;
            string col = k == 0 ? Fx[1] : (k == 1 ? Fx[2] : "B83A2A");
            Func<double, double, bool> dashes = (bx, by) => ((int)((Math.Atan2(by - cy, bx - cx) * 180 / Math.PI + 360) / 15) % 2) == (k % 2);
            ArenKitSheets.Ring(cv, C(f == 3 && k == 2 ? Fx[3] : col), cx, cy, R, 1.4, 0.8, dashes);
        }
        if (f < 2) { ArenKitSheets.Star(cv, new[] { "FFFFFF", Fx[0], Fx[1] }, 16, 3, 2); }
        return cv;
    }

    // taunt mark over pulled enemies: 16x16, 2 frames (red "!")
    public static ArenKitSheets.Cv TauntMark(int f)
    {
        var cv = new ArenKitSheets.Cv(16, 16, 0, 0);
        int dy = f == 0 ? 0 : -1;
        for (int y = 3; y <= 9; y++) { cv.Set(7, y + dy, C("D83A2A")); cv.Set(8, y + dy, C(y < 5 ? "FF7A5A" : "B82A1A")); }
        cv.Set(7, 11 + dy, C("D83A2A")); cv.Set(8, 11 + dy, C("B82A1A")); cv.Set(7, 12 + dy, C("B82A1A")); cv.Set(8, 12 + dy, C("8A1A10"));
        return cv;
    }

    // 5 Iron Wall: steel dome 48x48, pivot (24,40); 0-1 appear, 2-3 hold (loop), 4-5 break; back / front halves
    public static ArenKitSheets.Cv IronWall(int f, bool front)
    {
        var tmp = Cell(48, 48, 24, 40);
        double cx = 16, cy = 29, R = 17;
        if (f <= 3)
        {
            double th = f == 0 ? 1.0 : 2.0;
            // dome: upper arc of a tall ellipse + ground ellipse
            Func<double, double, bool> upper = (bx, by) => by <= cy;
            ArenKitSheets.Ring(tmp, C(Steel[3]), cx, cy, R, th + 0.6, 1.25, upper);
            ArenKitSheets.Ring(tmp, C(f == 0 ? Steel[2] : Steel[1]), cx, cy, R - 1, 1.0, 1.25, upper);
            ArenKitSheets.Ring(tmp, C(Steel[3]), cx, cy, R, 1.2, 0.35, null);
            if (f >= 1)
            {
                foreach (var a in new[] { 200, 240, 270, 300, 340 })
                {
                    double r = a * Math.PI / 180;
                    tmp.Set(cx + Math.Cos(r) * (R - 1), cy + Math.Sin(r) * (R - 1) * 1.25, C(Steel[0]));
                }
                int glint = f == 2 ? 230 : 310;
                double gr = glint * Math.PI / 180;
                ArenKitSheets.Star(tmp, new[] { Steel[0], Steel[1], Steel[2] }, cx + Math.Cos(gr) * (R - 1), cy + Math.Sin(gr) * (R - 1) * 1.25, 2);
            }
        }
        else
        {
            var rnd = new Random(80 + f);
            for (int k = 0; k < 9; k++)
            {
                double a = Math.PI + k * Math.PI / 8, spread = f == 4 ? 1.1 : 1.35;
                double x0 = cx + Math.Cos(a) * R * spread, y0 = cy + Math.Sin(a) * R * 1.25 * spread;
                ArenKitSheets.Line(tmp, C(f == 4 ? Steel[2] : Steel[3]), x0, y0, x0 + Math.Cos(a + 1.5) * 2, y0 + Math.Sin(a + 1.5) * 2);
            }
            ArenKitSheets.Dots(tmp, new[] { Steel[1], Steel[2] }, 90 + f, cx, cy - 8, 18, 14, f == 4 ? 10 : 5);
        }
        var cv = Cell(48, 48, 24, 40);
        for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++)
            {
                if (!tmp.Px[x, y].HasValue) continue;
                bool isFront = (y - cv.Oy + 0.5) >= cy - 2;
                if (isFront == front) cv.Px[x, y] = tmp.Px[x, y];
            }
        return cv;
    }

    // C Earthquake: 112x64, pivot (56,40); 3 waves (radius 12 / 20 / 28) x 2 frames (strong, fade)
    public static ArenKitSheets.Cv Quake(int wave, int fr)
    {
        var cv = Cell(112, 64, 56, 40);
        double cx = 16, cy = 29, R = 12 + wave * 8;
        ArenKitSheets.Ring(cv, C(fr == 0 ? Fx[2] : Fx[3]), cx, cy, R, fr == 0 ? 2.6 : 1.4, 0.42, (bx, by) => ((int)((Math.Atan2(by - cy, bx - cx) * 180 / Math.PI + 360) / 20) % 4) != 3 || fr == 0);
        if (fr == 0) ArenKitSheets.Ring(cv, C(Fx[1]), cx, cy, R - 3, 1.2, 0.42, null);
        Crack(cv, C(Fx[3]), 40 + wave, cx, cy, 10, (int)(R * 0.6), 0.45);
        if (fr == 0) ArenKitSheets.Dots(cv, new[] { Rock[0], Rock[1], Rock[2], Fx[2] }, 50 + wave, cx, cy - 5, R, R * 0.42 + 4, 10 + wave * 4, 2);
        return cv;
    }

    // ================= preview (right-facing art, unmirrored) =================
    public static Bitmap Preview(string anim, string v, int f, int step)
    {
        char[,] g = anim == "Idle" ? Idle(v, f) : anim == "Walk" ? Walk(v, f) : Attack(v, f);
        var body = EquipGen.Body(g, BromGen.Pal);
        P p = anim == "Attack" ? (f == 0 ? Windup(v) : f == 1 ? Strike(v, step) : Recover(v)) : Move(anim, v, f);
        var behind = new List<EquipGen.Layer>(); var front = new List<EquipGen.Layer>();
        var main = new EquipGen.Layer { S = Hammer(p.MD), X = p.MX, Y = p.MY };
        var glove = new EquipGen.Layer { S = Glove, X = p.MX - 1, Y = p.MY - 1 };
        var off = new EquipGen.Layer { S = BromGen.Shield(v), X = p.OX, Y = p.OY };
        if (p.MO > 0) { front.Add(main); front.Add(glove); } else { behind.Add(main); behind.Add(glove); }
        if (p.OO > 0) front.Add(off); else behind.Add(off);
        var bmp = EquipGen.Compose(body, behind, front);
        if (anim == "Attack" && f > 0)
        {
            var cv = ComboVfx(v, step, f - 1);
            for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++)
                {
                    if (!cv.Px[x, y].HasValue) continue;
                    // ground-ring finisher and Up swings sit behind the body
                    bool back = (v == "Up") || step == 2;
                    if (back && bmp.GetPixel(x, y).A > 0) continue;
                    bmp.SetPixel(x, y, cv.Px[x, y].Value);
                }
        }
        return bmp;
    }

    // ================= export =================
    public class SheetInfo { public string File; public int CellW, CellH, Cols, Rows, PivotX, PivotY; public string Layout; }
    public static readonly List<SheetInfo> Manifest = new List<SheetInfo>();

    static void SaveCells(string dir, string file, List<ArenKitSheets.Cv> cells, int cols, int pivX, int pivY, string layout, bool[] mirror = null)
    {
        int cw = cells[0].W, ch = cells[0].H, rows = (cells.Count + cols - 1) / cols;
        var bmp = new Bitmap(cw * cols, ch * rows, PixelFormat.Format32bppArgb);
        for (int i = 0; i < cells.Count; i++)
        {
            int ox = (i % cols) * cw, oy = (i / cols) * ch; bool m = mirror != null && mirror[i];
            for (int y = 0; y < ch; y++) for (int x = 0; x < cw; x++)
                    if (cells[i].Px[x, y].HasValue) bmp.SetPixel(ox + (m ? cw - 1 - x : x), oy + y, cells[i].Px[x, y].Value);
        }
        bmp.Save(Path.Combine(dir, file), ImageFormat.Png); bmp.Dispose();
        Manifest.Add(new SheetInfo { File = file, CellW = cw, CellH = ch, Cols = cols, Rows = rows, PivotX = pivX, PivotY = pivY, Layout = layout });
    }

    static void PaintGrid(Bitmap bmp, int ox, int oy, char[,] g, bool mirror, Func<Color, Color> fx)
    {
        var pal = BromGen.Pal;
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
            {
                char ch = g[x, y]; if (ch == '.') continue;
                int cx = 9 + x; if (mirror) cx = 31 - cx;
                var c = C(pal[ch]); bmp.SetPixel(ox + cx, oy + 9 + y, fx == null ? c : fx(c));
            }
    }

    static Func<Color, Color> Tint(Color to, double k) { return c => Color.FromArgb(255, (int)(c.R + (to.R - c.R) * k), (int)(c.G + (to.G - c.G) * k), (int)(c.B + (to.B - c.B) * k)); }

    static void SaveSpr(Bitmap sheet, EquipGen.Spr s, int ox, int oy, int ax, int ay, bool mirror)
    {
        foreach (var kv in s.Px)
        {
            int x = ox + ax + (mirror ? -kv.Key.X : kv.Key.X), y = oy + ay + kv.Key.Y;
            if (x >= ox && y >= oy && x < sheet.Width && y < sheet.Height) sheet.SetPixel(x, y, kv.Value);
        }
    }

    static string Row(string key, string v, P p, int slash, int slashOrder)
    {
        bool m = v == "Side";
        int mx = p.MX, md = p.MD, ox = p.OX;
        if (m && mx > -99) { mx = 31 - mx; md = MirrorDir[md]; }
        if (m && ox > -99) ox = 31 - ox;
        int hx = -99, hy = 0;
        if (p.Hand && p.MX > -99) { hx = m ? 31 - p.MX : p.MX - 1; hy = p.MY - 1; }
        // key,main_x,main_y,main_dir,main_order,hand_x,hand_y,off_x,off_y,off_view,off_order,slash,slash_order,off_dir
        return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12},-1\n", key, mx, p.MY, md, p.MO, hx, hy, ox, p.OY, v, p.OO, slash, slashOrder);
    }

    public static string Run(string outDir)
    {
        Manifest.Clear();
        Directory.CreateDirectory(Path.Combine(outDir, "Extra")); Directory.CreateDirectory(Path.Combine(outDir, "Gear")); Directory.CreateDirectory(Path.Combine(outDir, "Skills"));
        // body sheet (same layout as Aren / Vex)
        var sheet = new Bitmap(128, 288, PixelFormat.Format32bppArgb);
        for (int a = 0; a < 3; a++) for (int vi = 0; vi < 3; vi++) for (int f = 0; f < (a == 2 ? 3 : 4); f++)
                {
                    var v = Views[vi];
                    char[,] g = a == 0 ? Idle(v, f) : a == 1 ? Walk(v, f) : Attack(v, f);
                    PaintGrid(sheet, f * 32, (a * 3 + vi) * 32, g, v == "Side", null);
                }
        sheet.Save(Path.Combine(outDir, "Brom_Sheet.png"), ImageFormat.Png); sheet.Dispose();
        foreach (var v in Views)
            for (int f = 0; f < 2; f++)
            {
                var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
                if (f == 0) PaintGrid(bmp, 0, 0, Idle(v, 0), v == "Side", Tint(Color.White, 0.8));
                else PaintGrid(bmp, 0, 0, Walk(v, 0), v == "Side", Tint(C("FF3030"), 0.35));
                bmp.Save(Path.Combine(outDir, "Extra", "Brom_Hurt_" + v + "_" + f + ".png"), ImageFormat.Png); bmp.Dispose();
            }
        {
            var d0 = new Bitmap(32, 32, PixelFormat.Format32bppArgb); PaintGrid(d0, 0, 0, Kneel(), false, null);
            d0.Save(Path.Combine(outDir, "Extra", "Brom_Death_Down_0.png"), ImageFormat.Png); d0.Dispose();
            var lying = FxGen.Lying(BromGen.Down);
            for (int f = 1; f <= 2; f++)
            {
                var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
                for (int y = 0; y < 14; y++) for (int x = 0; x < 21; x++) if (lying[x, y] != '.') bmp.SetPixel(5 + x, 16 + y, C(BromGen.Pal[lying[x, y]]));
                bmp.Save(Path.Combine(outDir, "Extra", "Brom_Death_Down_" + f + ".png"), ImageFormat.Png); bmp.Dispose();
            }
        }
        // gear
        var ham = new Bitmap(48 * 8, 48, PixelFormat.Format32bppArgb);
        for (int d = 0; d < 8; d++) SaveSpr(ham, Hammer(d), d * 48, 0, 24, 24, false);
        ham.Save(Path.Combine(outDir, "Gear", "Brom_Hammer.png"), ImageFormat.Png); ham.Dispose();
        var sh = new Bitmap(16 * 3, 16, PixelFormat.Format32bppArgb);
        for (int vi = 0; vi < 3; vi++) SaveSpr(sh, BromGen.Shield(Views[vi]), vi * 16, 0, 8, 8, Views[vi] == "Side");
        sh.Save(Path.Combine(outDir, "Gear", "Brom_Shield.png"), ImageFormat.Png); sh.Dispose();
        var hand = new Bitmap(4, 4, PixelFormat.Format32bppArgb); SaveSpr(hand, Glove, 0, 0, 1, 1, false);
        hand.Save(Path.Combine(outDir, "Gear", "Brom_Hand.png"), ImageFormat.Png); hand.Dispose();
        // combo VFX: blocks of 6 per step (Down0, Down1, Up0, Up1, Side0, Side1); Side mirrored
        var combo = new List<ArenKitSheets.Cv>(); var mir = new List<bool>();
        for (int step = 0; step < 3; step++) foreach (var v in Views) for (int fr = 0; fr < 2; fr++) { combo.Add(ComboVfx(v, step, fr)); mir.Add(v == "Side"); }
        SaveCells(Path.Combine(outDir, "Gear"), "Brom_ComboVfx.png", combo, 18, 24, 38, "18 cells: step*6 + view*2 + frame (views Down/Up/Side; Side faces left)", mir.ToArray());
        // skills
        var sk = Path.Combine(outDir, "Skills");
        var l = new List<ArenKitSheets.Cv>(); for (int f = 0; f < 4; f++) l.Add(GroundSlam(f));
        SaveCells(sk, "Brom_GroundSlam.png", l, 4, 40, 36, "2 Nen Dat: frames 0-3 (impact, spread, wide, fade); ground layer, draw BEHIND body");
        l = new List<ArenKitSheets.Cv>(); for (int f = 0; f < 3; f++) l.Add(ShieldGlow(f));
        SaveCells(sk, "Brom_ShieldGlow.png", l, 3, 12, 12, "3 Huc Khien: rim flare on the shield anchor, frames 0-2");
        l = new List<ArenKitSheets.Cv>(); for (int f = 0; f < 3; f++) l.Add(Dust(f));
        SaveCells(sk, "Brom_ChargeDust.png", l, 3, 8, 15, "3 Huc Khien: dust at the feet, frames 0-2");
        l = new List<ArenKitSheets.Cv>(); for (int f = 0; f < 4; f++) l.Add(Roar(f));
        SaveCells(sk, "Brom_Roar.png", l, 4, 32, 48, "4 Gam Thach: sound rings from the head, frames 0-3, in FRONT of body");
        l = new List<ArenKitSheets.Cv>(); for (int f = 0; f < 2; f++) l.Add(TauntMark(f));
        SaveCells(sk, "Brom_TauntMark.png", l, 2, 8, 15, "4 Gam Thach: '!' above taunted enemies, frames 0-1 loop");
        l = new List<ArenKitSheets.Cv>(); for (int f = 0; f < 6; f++) l.Add(IronWall(f, false));
        SaveCells(sk, "Brom_IronWall_Back.png", l, 6, 24, 40, "V Tuong Sat: 0-1 appear, 2-3 hold loop, 4-5 break; BEHIND body");
        l = new List<ArenKitSheets.Cv>(); for (int f = 0; f < 6; f++) l.Add(IronWall(f, true));
        SaveCells(sk, "Brom_IronWall_Front.png", l, 6, 24, 40, "V Tuong Sat: same frames; IN FRONT of body");
        l = new List<ArenKitSheets.Cv>(); for (int w = 0; w < 3; w++) for (int fr = 0; fr < 2; fr++) l.Add(Quake(w, fr));
        SaveCells(sk, "Brom_Quake.png", l, 2, 56, 40, "C Dong Dat: rows = wave 1-3 (R 12/20/28), cols = strong/fade; ground layer, BEHIND body");
        // anchors
        var csv = new StringBuilder("key,main_x,main_y,main_dir,main_order,hand_x,hand_y,off_x,off_y,off_view,off_order,slash,slash_order,off_dir\n");
        foreach (var anim in new[] { "Idle", "Walk" }) foreach (var v in Views) for (int f = 0; f < 4; f++) csv.Append(Row(anim + "_" + v + "_" + f, v, Move(anim, v, f), -1, 0));
        for (int vi = 0; vi < 3; vi++)
        {
            var v = Views[vi]; int so = v == "Up" ? -3 : 3;
            csv.Append(Row("Attack_" + v + "_0", v, Windup(v), -1, 0));
            csv.Append(Row("Attack_" + v + "_1", v, Strike(v, 0), vi * 2, so));
            csv.Append(Row("Attack_" + v + "_2", v, Recover(v), vi * 2 + 1, so));
            for (int step = 0; step < 3; step++)
            {
                csv.Append(Row("Attack_" + v + "_1@" + step, v, Strike(v, step), vi * 2, step == 2 ? -3 : so));
                csv.Append(Row("Attack_" + v + "_2@" + step, v, Recover(v), vi * 2 + 1, step == 2 ? -3 : so));
            }
            csv.Append(Row("Hurt_" + v + "_0", v, Move("Idle", v, 0), -1, 0));
            csv.Append(Row("Hurt_" + v + "_1", v, Move("Walk", v, 0), -1, 0));
        }
        csv.Append(Row("Death_Down_0", "Down", Pz(8, 27, 6, 2, 20, 26), -1, 0));
        csv.Append("Death_Down_1,-99,0,0,0,-99,0,-99,0,Down,0,-1,0,-1\n");
        var dead = Pz(6, 30, 0, -2, 27, 28); dead.Hand = false;
        csv.Append(Row("Death_Down_2", "Down", dead, -1, 0));
        File.WriteAllText(Path.Combine(outDir, "Brom_Anchors.csv"), csv.ToString(), new UTF8Encoding(false));
        // manifest
        var mf = new StringBuilder("file,cell_w,cell_h,cols,rows,pivot_px_x,pivot_px_y_from_top,unity_pivot_x,unity_pivot_y,layout\n");
        mf.Append("Brom_Sheet.png,32,32,4,9,16,30,0.5,0.0625,rows Idle D/U/S, Walk D/U/S, Attack D/U/S (Attack 3 cols); Side faces left\n");
        mf.Append("Gear/Brom_Hammer.png,48,48,8,1,24,24,0.5,0.5,poses R UR U UL L DL D DR; pivot = hand\n");
        mf.Append("Gear/Brom_Shield.png,16,16,3,1,8,8,0.5,0.5,Down (face) / Up (straps) / Side (edge, faces left)\n");
        mf.Append("Gear/Brom_Hand.png,4,4,1,1,1,1,0.25,0.75,glove over the hammer grip\n");
        foreach (var m in Manifest)
            mf.AppendFormat("{0}{1},{2},{3},{4},{5},{6},{7},{8:0.####},{9:0.####},{10}\n", m.File.StartsWith("Brom_ComboVfx") ? "Gear/" : "Skills/", m.File, m.CellW, m.CellH, m.Cols, m.Rows, m.PivotX, m.PivotY,
                (double)m.PivotX / m.CellW, (double)(m.CellH - m.PivotY) / m.CellH, m.Layout);
        File.WriteAllText(Path.Combine(outDir, "BROM_MANIFEST.csv"), mf.ToString(), new UTF8Encoding(false));
        return "ok";
    }
}
