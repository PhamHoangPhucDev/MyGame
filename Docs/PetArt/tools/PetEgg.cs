using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

// Mystery Pet Egg: cream shell with violet spots, ink outline, light from the top-left.
// Same palette and rules as the UI kit (Docs/UI/tools). No anti-aliasing, no semi-transparent pixels.
public static class PetEgg
{
    static readonly Color Hi = P.TextW, Lt = P.ParchL, Base = P.H("E8E0C8"), Sh = P.Parch, Dk = P.ParchD;
    static readonly Color SpotL = P.PurpleL, Spot = P.Purple, SpotD = P.PurpleD;
    static readonly Color Glow = P.GoldW, GlowM = P.GoldL, GlowD = P.Gold;

    // spots in normalised egg space (x,y in -1..1), radius in the same units
    static readonly double[,] Spots = { { -0.35, -0.30, 0.22 }, { 0.40, 0.05, 0.26 }, { -0.20, 0.45, 0.20 }, { 0.15, -0.65, 0.14 } };

    public const int Cell = 48, CellH = 64;   // hatch cells: 48 wide, 64 tall (16 px headroom for the flying cap / pet pop)
    // large egg: shell 32x40, placed so the outline touches the bottom row of the 48x48 cell
    const int EW = 32, EH = 40, OX = (Cell - EW) / 2, OY = Cell - EH - 1;   // shell spans x 8..39, y 7..46
    const int PivX = 24, PivY = 46;

    /// <summary>Egg of w x h shell pixels inside a (w+2) x (h+2) canvas, outlined.</summary>
    public static Bitmap Egg(int w, int h) { return Img.Outline(Raw(w, h, 1), P.Ink); }

    /// <summary>Shell pixels only (no outline) with a pad-pixel empty border.</summary>
    public static Bitmap Raw(int w, int h, int pad)
    {
        var cv = new Cv(w + 2 * pad, h + 2 * pad);
        double A = w / 2.0, B = h / 2.0;
        for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                double x = (i + 0.5 - A) / A, y = (j + 0.5 - B) / B;
                double half = Math.Sqrt(Math.Max(0, 1 - y * y)) * (0.86 + 0.14 * y); // narrower top
                if (Math.Abs(x) > half) continue;
                double nx = half > 0 ? x / half : 0;
                double lit = -(nx * 0.75 + y * 0.65);            // + toward top-left
                double rim = Math.Sqrt(nx * nx + y * y);

                Color c;
                bool spot = false; double sd = 9;
                for (int s = 0; s < Spots.GetLength(0); s++)
                {
                    double dx = x - Spots[s, 0], dy = (y - Spots[s, 1]) * 1.2;
                    double d = Math.Sqrt(dx * dx + dy * dy) / Spots[s, 2];
                    if (d < 1) { spot = true; sd = Math.Min(sd, d); }
                }
                if (spot) c = lit > 0.35 && sd < 0.6 ? SpotL : (lit < -0.45 || sd > 0.8 ? SpotD : Spot);
                else if (lit > 0.70 && rim < 0.85) c = Hi;
                else if (lit > 0.30) c = Lt;
                else if (lit > -0.30) c = Base;
                else if (lit > -0.70 || rim < 0.9) c = Sh;
                else c = Dk;
                cv.Set(i + pad, j + pad, c);
            }
        return cv.B;
    }

    // ---------- large egg helpers (all in 48x48 cell coordinates) ----------

    static Bitmap ShellCell()
    {
        var cv = new Cv(Cell, Cell);
        cv.Blit(Raw(EW, EH, 0), OX, OY);
        return cv.B;
    }

    /// <summary>Nearest-neighbour rotation about (px,py), degrees clockwise.</summary>
    static Bitmap Rotate(Bitmap src, double deg, double px, double py)
    {
        var o = new Cv(src.Width, src.Height);
        double a = -deg * Math.PI / 180, ca = Math.Cos(a), sa = Math.Sin(a);
        for (int j = 0; j < src.Height; j++)
            for (int i = 0; i < src.Width; i++)
            {
                double dx = i + 0.5 - px, dy = j + 0.5 - py;
                int sx = (int)Math.Floor(px + dx * ca - dy * sa), sy = (int)Math.Floor(py + dx * sa + dy * ca);
                if (sx < 0 || sy < 0 || sx >= src.Width || sy >= src.Height) continue;
                Color c = src.GetPixel(sx, sy);
                if (c.A > 0) o.Set(i, j, c);
            }
        return o.B;
    }

    static Bitmap Shift(Bitmap src, int dx, int dy)
    {
        var o = new Cv(src.Width, src.Height);
        o.Blit(src, dx, dy);
        return o.B;
    }

    static bool Solid(Bitmap b, int x, int y) { return x >= 0 && y >= 0 && x < b.Width && y < b.Height && b.GetPixel(x, y).A > 0; }

    /// <summary>Draw ink along a polyline, only where the shell exists (cracks never leave the egg).</summary>
    static void Crack(Bitmap b, int[] pts, Color leak, bool leakBelow)
    {
        var cv = new Cv(b);
        for (int p = 0; p + 3 < pts.Length; p += 2)
        {
            int x0 = pts[p], y0 = pts[p + 1], x1 = pts[p + 2], y1 = pts[p + 3];
            int n = Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0));
            for (int s = 0; s <= n; s++)
            {
                int x = x0 + (int)Math.Round((x1 - x0) * (double)s / Math.Max(1, n));
                int y = y0 + (int)Math.Round((y1 - y0) * (double)s / Math.Max(1, n));
                if (!Solid(b, x, y)) continue;
                cv.Set(x, y, P.Ink);
                // shadow side of the crack gives it depth
                if (Solid(b, x + 1, y) && b.GetPixel(x + 1, y) != P.Ink) cv.Set(x + 1, y, Dk);
                if (leak.A > 0 && leakBelow && Solid(b, x, y + 1) && b.GetPixel(x, y + 1) != P.Ink && (x + y) % 3 == 0) cv.Set(x, y + 1, leak);
            }
        }
    }

    // zigzag split line across the egg (y on the line for column x)
    static int SplitY(int x) { int t = ((x % 6) + 6) % 6; return 25 + (t < 3 ? t : 6 - t) - 1; }

    static readonly int[] CrackTop = { 26, 7, 23, 10, 27, 13, 24, 16, 26, 19, 23, 22 };
    static readonly int[] CrackSide = { 25, 19, 29, 21, 32, 19, 35, 22 };
    static readonly int[] CrackLeft = { 24, 16, 20, 19, 17, 17, 14, 20 };

    static List<int> SplitPolyline()
    {
        var l = new List<int>();
        for (int x = OX - 1; x <= OX + EW; x += 3) { l.Add(x); l.Add(SplitY(x)); }
        return l;
    }

    static Bitmap Split(Bitmap shell, bool top)
    {
        var o = new Cv(Cell, Cell);
        for (int j = 0; j < Cell; j++)
            for (int i = 0; i < Cell; i++)
            {
                Color c = shell.GetPixel(i, j);
                if (c.A == 0) continue;
                bool above = j < SplitY(i);
                if (above != top) continue;
                // inside of the bottom shell shows as a dark rim along the broken edge
                if (!top && j <= SplitY(i) + 1) c = j == SplitY(i) ? Dk : Sh;
                o.Set(i, j, c);
            }
        return o.B;
    }

    static void Rays(Cv cv, int cx, int cy, int len, bool big)
    {
        double[] angles = { -90, -60, -120, -30, -150, -10, -170 };
        for (int r = 0; r < angles.Length; r++)
        {
            double a = angles[r] * Math.PI / 180;
            int L = len - (r % 2) * (len / 3);
            for (int s = 3; s <= L; s++)
            {
                int x = cx + (int)Math.Round(Math.Cos(a) * s), y = cy + (int)Math.Round(Math.Sin(a) * s);
                Color c = s < L * 0.5 ? Glow : (s < L * 0.8 ? GlowM : GlowD);
                cv.Set(x, y, c);
                if (big && s < L * 0.6 && r == 0) cv.Set(x + 1, y, GlowM);
            }
        }
    }

    static void Sparkle(Cv cv, int x, int y, bool large)
    {
        cv.Set(x, y, Glow);
        cv.Set(x - 1, y, GlowM); cv.Set(x + 1, y, GlowM); cv.Set(x, y - 1, GlowM); cv.Set(x, y + 1, GlowM);
        if (large) { cv.Set(x - 2, y, GlowD); cv.Set(x + 2, y, GlowD); cv.Set(x, y - 2, GlowD); cv.Set(x, y + 2, GlowD); }
    }

    static Bitmap Chip(int w, int h, Color c)
    {
        var cv = new Cv(w + 2, h + 2);
        cv.Fill(1, 1, w, h, c);
        cv.Set(1, 1, Lt);
        return Img.Outline(cv.B, P.Ink);
    }

    // ---------- frames ----------

    public static Bitmap Idle() { return Img.Outline(ShellCell(), P.Ink); }

    public static Bitmap Tilt(double deg) { return Img.Outline(Rotate(ShellCell(), deg, PivX, PivY), P.Ink); }

    public static Bitmap Crack1()
    {
        var b = Idle();
        Crack(b, CrackTop, Color.Empty, false);
        return b;
    }

    public static Bitmap Crack2()
    {
        var b = Idle();
        Crack(b, CrackTop, Color.Empty, false);
        Crack(b, CrackSide, Glow, true);
        Crack(b, CrackLeft, Glow, true);
        var cv = new Cv(b);
        // light leaking out of the widest part of the crack
        cv.Set(25, 18, Glow); cv.Set(24, 17, GlowM); cv.Set(26, 20, GlowM);
        Sparkle(cv, 38, 12, false);
        return b;
    }

    public static Bitmap Burst()
    {
        var shell = ShellCell();
        const int T = CellH - Cell;              // headroom above the 48x48 egg area
        var cv = new Cv(Cell, CellH);
        // glow behind everything, coming out of the opening
        Rays(cv, 24, 24 + T, 24, true);
        var bottom = Img.Outline(Split(shell, false), P.Ink);
        cv.Blit(bottom, 0, T);
        // top cap flies up and tips left (drawn on the tall canvas so it is never clipped)
        var capTall = new Cv(Cell, CellH); capTall.Blit(Split(shell, true), 0, T);
        var top = Img.Outline(Rotate(Shift(capTall.B, -3, -13), -20, 21, 16 + T - 13), P.Ink);
        cv.Blit(top, 0, 0);
        // glow fills the opening
        for (int i = 10; i <= 37; i++)
            for (int j = SplitY(i) - 3; j <= SplitY(i); j++)
                if (!Solid(bottom, i, j) && !Solid(top, i, j + T)) cv.Set(i, j + T, j == SplitY(i) ? GlowM : Glow);
        // shell chips
        cv.Blit(Chip(2, 2, Base), 39, 12 + T);
        cv.Blit(Chip(3, 2, Lt), 4, 18 + T);
        cv.Blit(Chip(2, 2, Sh), 42, 24 + T);
        Sparkle(cv, 6, 6, true);
        Sparkle(cv, 41, 4, false);
        return cv.B;
    }

    public static Bitmap Open()
    {
        var shell = ShellCell();
        var cv = new Cv(Cell, CellH);
        const int T = CellH - Cell;
        var bottom = Img.Outline(Split(shell, false), P.Ink);
        cv.Blit(bottom, 0, T);
        Sparkle(cv, 12, 14 + T, true);
        Sparkle(cv, 37, 8 + T, true);
        Sparkle(cv, 24, 2 + T, false);
        Sparkle(cv, 6, 28 + T, false);
        Sparkle(cv, 42, 24 + T, false);
        return cv.B;
    }

    static Bitmap Tall(Bitmap b) { return Pad(b, Cell, CellH); }

    static Bitmap Pad(Bitmap src, int w, int h)
    {
        var o = new Cv(w, h);
        o.Blit(src, (w - src.Width) / 2, h - src.Height);
        return o.B;
    }

    public static void Run(string outDir)
    {
        Directory.CreateDirectory(outDir);
        // Bag icon: 16x16 canvas, shell 11x13
        var icon = new Cv(16, 16);
        var small = Egg(11, 13);
        icon.Blit(small, (16 - small.Width) / 2, 1);
        icon.Save(Path.Combine(outDir, "UI_Item_MysteryPetEgg.png"));

        var frames = new List<KeyValuePair<string, Bitmap>>
        {
            new KeyValuePair<string, Bitmap>("Idle", Tall(Idle())),
            new KeyValuePair<string, Bitmap>("TiltL", Tall(Tilt(-8))),
            new KeyValuePair<string, Bitmap>("TiltR", Tall(Tilt(8))),
            new KeyValuePair<string, Bitmap>("Crack1", Tall(Crack1())),
            new KeyValuePair<string, Bitmap>("Crack2", Tall(Crack2())),
            new KeyValuePair<string, Bitmap>("Burst", Burst()),
            new KeyValuePair<string, Bitmap>("Open", Open()),
        };
        var sheet = new Cv(Cell * frames.Count, CellH);
        for (int f = 0; f < frames.Count; f++)
        {
            sheet.Blit(frames[f].Value, f * Cell, 0);
            new Cv(frames[f].Value).Save(Path.Combine(outDir, "frames", "Pet_Egg_" + frames[f].Key + ".png"));
        }
        sheet.Save(Path.Combine(outDir, "Pet_Egg_Hatch_Sheet.png"));

        // Review board (not for Unity): bag slot + sheet x3 on the dark UI panel colour
        var slot = Img.Load("Docs/UI/export/Slots/UI_Inventory_Slot.png");
        var s = new Cv(slot.Width, slot.Height); s.Blit(slot, 0, 0); s.Blit(icon.B, 3, 3);
        int k = 3, gap = 12;
        var board = new Cv(16 + frames.Count * (Cell * k + gap), CellH * k + 32 + 22 * 4 + 16);
        board.Clear(P.Char);
        for (int f = 0; f < frames.Count; f++) board.Blit(frames[f].Value, 16 + f * (Cell * k + gap), 16, k);
        board.Blit(s.B, 16, CellH * k + 32, 4);
        board.Save(Path.Combine(outDir, "Egg_Review_Board.png"));
    }
}
