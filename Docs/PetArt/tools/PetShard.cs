using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

// Pet Egg Shard: a broken piece of the Mystery Pet Egg shell (same cream shell, violet spot, ink outline,
// light from the top-left). Built on PetEgg.Raw so the shard always matches the egg.
// No anti-aliasing, no semi-transparent pixels.
public static class PetShard
{
    static readonly Color Lt = P.ParchL, Sh = P.Parch, Dk = P.ParchD;

    // Shard outline in normalised egg space (x,y in -1..1, y down). Upper-left piece that holds one violet spot.
    static readonly double[] Poly =
    {
        -0.98, -0.35,  -0.55, -0.85,  -0.20, -0.98,   0.08, -0.72,  -0.12, -0.58,
        -0.02, -0.46,  -0.26, -0.36,  -0.20, -0.18,  -0.30, -0.02,  -0.18,  0.14,
        -0.45,  0.22,  -0.62,  0.42,  -0.80,  0.26,  -0.98,  0.20
    };

    static bool Inside(double x, double y)
    {
        bool c = false;
        int n = Poly.Length / 2;
        for (int i = 0, j = n - 1; i < n; j = i++)
        {
            double xi = Poly[2 * i], yi = Poly[2 * i + 1], xj = Poly[2 * j], yj = Poly[2 * j + 1];
            if ((yi > y) != (yj > y) && x < (xj - xi) * (y - yi) / (yj - yi) + xi) c = !c;
        }
        return c;
    }

    /// <summary>Shard cut from an egg of w x h shell pixels, cropped and outlined.</summary>
    public static Bitmap Shard(int w, int h)
    {
        var egg = PetEgg.Raw(w, h, 0);
        double A = w / 2.0, B = h / 2.0;
        var keep = new bool[w, h];
        int x0 = w, y0 = h, x1 = -1, y1 = -1;
        for (int j = 0; j < h; j++)
            for (int i = 0; i < w; i++)
            {
                if (egg.GetPixel(i, j).A == 0) continue;
                double x = (i + 0.5 - A) / A, y = (j + 0.5 - B) / B;
                if (!Inside(x, y)) continue;
                keep[i, j] = true;
                x0 = Math.Min(x0, i); y0 = Math.Min(y0, j); x1 = Math.Max(x1, i); y1 = Math.Max(y1, j);
            }
        var cv = new Cv(x1 - x0 + 4, y1 - y0 + 4);
        // shell thickness: the piece seen 1 px lower-right, dark, under the face
        for (int j = y0; j <= y1; j++)
            for (int i = x0; i <= x1; i++)
                if (keep[i, j]) cv.Set(i - x0 + 2, j - y0 + 2, Sh);
        for (int j = y0; j <= y1; j++)
            for (int i = x0; i <= x1; i++)
            {
                if (!keep[i, j]) continue;
                Color c = egg.GetPixel(i, j);
                // broken edge: a neighbour that is still shell but not in the shard. Shows the shell thickness.
                bool brokeR = Shell(egg, i + 1, j) && !Keep(keep, i + 1, j, w, h);
                bool brokeB = Shell(egg, i, j + 1) && !Keep(keep, i, j + 1, w, h);
                bool brokeL = Shell(egg, i - 1, j) && !Keep(keep, i - 1, j, w, h);
                bool brokeT = Shell(egg, i, j - 1) && !Keep(keep, i, j - 1, w, h);
                if (brokeR || brokeB) c = Dk;
                else if (brokeL || brokeT) c = Lt;
                cv.Set(i - x0 + 1, j - y0 + 1, c);
            }
        return Img.Outline(cv.B, P.Ink);
    }

    static bool Shell(Bitmap b, int x, int y) { return x >= 0 && y >= 0 && x < b.Width && y < b.Height && b.GetPixel(x, y).A > 0; }
    static bool Keep(bool[,] k, int x, int y, int w, int h) { return x >= 0 && y >= 0 && x < w && y < h && k[x, y]; }

    /// <summary>4-point gold sparkle, 3 px (small) or 5 px (big) across.</summary>
    public static void Sparkle(Cv cv, int x, int y, bool big)
    {
        cv.Set(x, y, P.GoldW);
        cv.Set(x - 1, y, P.GoldL); cv.Set(x + 1, y, P.GoldL); cv.Set(x, y - 1, P.GoldL); cv.Set(x, y + 1, P.GoldL);
        if (!big) return;
        cv.Set(x - 2, y, P.Gold); cv.Set(x + 2, y, P.Gold); cv.Set(x, y - 2, P.Gold); cv.Set(x, y + 2, P.Gold);
    }

    /// <summary>Bag icon, 16x16.</summary>
    public static Bitmap Icon16()
    {
        var cv = new Cv(16, 16);
        var s = Shard(24, 30);
        cv.Blit(s, (16 - s.Width) / 2, 16 - s.Height - 1);
        Sparkle(cv, 13, 2, false);
        return cv.B;
    }

    /// <summary>Large icon for the sacrifice popup and the shard area, 32x32.</summary>
    public static Bitmap Icon32()
    {
        var cv = new Cv(32, 32);
        var s = Shard(46, 58);
        cv.Blit(s, (32 - s.Width) / 2, 32 - s.Height - 2);
        Sparkle(cv, 26, 4, true);
        Sparkle(cv, 4, 6, false);
        return cv.B;
    }

    // ---------- FX sheets ----------

    static void Ring(Cv cv, int cx, int cy, int rx, int ry, Color c)
    {
        for (int a = 0; a < 360; a += 3)
        {
            double r = a * Math.PI / 180;
            cv.Set(cx + (int)Math.Round(Math.Cos(r) * rx), cy + (int)Math.Round(Math.Sin(r) * ry), c);
        }
    }

    static void Disk(Cv cv, int cx, int cy, int r, Color c)
    {
        for (int j = -r; j <= r; j++)
            for (int i = -r; i <= r; i++)
                if (i * i + j * j <= r * r + r / 2) cv.Set(cx + i, cy + j, c);
    }

    static void Orb(Cv cv, int cx, int cy, int r)
    {
        Disk(cv, cx, cy, r + 1, P.Purple);
        Disk(cv, cx, cy, r, P.GoldL);
        Disk(cv, cx, cy, Math.Max(1, r - 2), P.GoldW);
        cv.Set(cx - r / 2, cy - r / 2, P.TextW);
    }

    /// <summary>Straight rays from the centre, dashed so they read as light, not lines.</summary>
    static void Rays(Cv cv, int cx, int cy, int r0, int r1, int count, double phase)
    {
        for (int k = 0; k < count; k++)
        {
            double a = phase + k * Math.PI * 2 / count;
            for (int d = r0; d <= r1; d++)
            {
                if (d % 3 == 2) continue;
                cv.Set(cx + (int)Math.Round(Math.Cos(a) * d), cy + (int)Math.Round(Math.Sin(a) * d), d < (r0 + r1) / 2 ? P.GoldW : P.GoldL);
            }
        }
    }

    /// <summary>Light motes: 1 px violet/gold dots in a column, rising with t.</summary>
    static void Motes(Cv cv, int cx, int bottom, int height, int t, int n)
    {
        for (int m = 0; m < n; m++)
        {
            int x = cx + ((m * 7) % 17) - 8;
            int y = bottom - ((m * 11 + t * 6) % height);
            Color c = m % 3 == 0 ? P.GoldW : (m % 3 == 1 ? P.PurpleL : P.GoldL);
            cv.Set(x, y, c);
            if (m % 4 == 0) cv.Set(x, y + 1, P.Purple);
        }
    }

    public const int SacCell = 48, ComCell = 64;

    /// <summary>
    /// Sacrifice overlay, 6 cells of 48x48, pivot bottom-centre (24,0). The pet stands on the bottom row.
    /// 0-2 play over the pet while code fades the pet out; 3-4 light gathers; 5 the shard is left.
    /// </summary>
    public static List<Bitmap> SacrificeFrames()
    {
        var l = new List<Bitmap>();
        const int cx = 24, ground = 44;
        var shard = Shard(46, 58);   // same piece as the approved 32 px icon
        for (int f = 0; f < 6; f++)
        {
            var cv = new Cv(SacCell, SacCell);
            if (f <= 2)
            {
                Ring(cv, cx, ground, 12, 3, f == 0 ? P.Gold : P.GoldL);
                if (f >= 1) Ring(cv, cx, ground, 14, 4, P.Purple);
                Motes(cv, cx, ground - 2, 14 + f * 12, f, 6 + f * 5);
                if (f == 2)
                    for (int y = 6; y < ground; y++)
                        if (y % 2 == 0) { cv.Set(cx - 6, y, P.GoldL); cv.Set(cx + 6, y, P.GoldL); }
            }
            else if (f == 3)
            {
                Orb(cv, cx, 26, 7);
                Rays(cv, cx, 26, 10, 15, 8, 0.2);
                Motes(cv, cx, 40, 30, f, 8);
            }
            else if (f == 4)
            {
                Orb(cv, cx, 26, 4);
                Rays(cv, cx, 26, 7, 11, 8, 0.6);
            }
            else
            {
                cv.Blit(shard, cx - shard.Width / 2, 26 - shard.Height / 2);
                Sparkle(cv, cx + 12, 12, true);
                Sparkle(cv, cx - 13, 20, false);
                Sparkle(cv, cx + 10, 38, false);
            }
            l.Add(cv.B);
        }
        return l;
    }

    static Bitmap Turn(Bitmap src, RotateFlipType t) { var b = (Bitmap)src.Clone(); b.RotateFlip(t); return b; }

    /// <summary>
    /// Combine, 8 cells of 64x64, pivot centre. Ten shards spiral in (0-4), flash (5), the egg appears (6-7).
    /// After cell 7 the game adds the egg to the bag (or plays the existing hatch sheet).
    /// </summary>
    public static List<Bitmap> CombineFrames()
    {
        var l = new List<Bitmap>();
        const int cx = 32, cy = 32;
        var s = Shard(20, 26);
        var turns = new[] { RotateFlipType.RotateNoneFlipNone, RotateFlipType.RotateNoneFlipX, RotateFlipType.Rotate90FlipNone,
                            RotateFlipType.Rotate270FlipNone, RotateFlipType.Rotate180FlipNone };
        int[] radius = { 24, 20, 15, 10, 5 };
        double[] phase = { 0, 0.25, 0.55, 0.95, 1.45 };
        var egg = PetEgg.Egg(22, 28);
        for (int f = 0; f < 8; f++)
        {
            var cv = new Cv(ComCell, ComCell);
            if (f <= 4)
            {
                if (f >= 2) Orb(cv, cx, cy, f - 1);
                for (int k = 0; k < 10; k++)
                {
                    double a = phase[f] + k * Math.PI * 2 / 10;
                    var b = Turn(s, turns[k % turns.Length]);
                    int x = cx + (int)Math.Round(Math.Cos(a) * radius[f]) - b.Width / 2;
                    int y = cy + (int)Math.Round(Math.Sin(a) * radius[f]) - b.Height / 2;
                    if (f >= 3 && k % 2 == 1) continue;     // the last steps merge: half the pieces are already inside the light
                    cv.Blit(b, x, y);
                }
                if (f >= 1) Motes(cv, cx, cy + 20, 40, f, 6);
            }
            else if (f == 5)
            {
                Rays(cv, cx, cy, 13, 26, 12, 0.1);
                Orb(cv, cx, cy, 11);
            }
            else
            {
                if (f == 6) Rays(cv, cx, cy, 17, 24, 12, 0.35);
                cv.Blit(egg, cx - egg.Width / 2, cy - egg.Height / 2);
                Sparkle(cv, cx + 15, cy - 14, true);
                Sparkle(cv, cx - 15, cy - 6, f == 6);
                Sparkle(cv, cx + 12, cy + 14, false);
            }
            l.Add(cv.B);
        }
        return l;
    }

    static Bitmap Strip(List<Bitmap> frames, int cw, int ch)
    {
        var cv = new Cv(cw * frames.Count, ch);
        for (int f = 0; f < frames.Count; f++) cv.Blit(frames[f], f * cw, 0);
        return cv.B;
    }

    public static void Run(string outDir)
    {
        RunSample(outDir);
        var sac = SacrificeFrames();
        var com = CombineFrames();
        new Cv(Strip(sac, SacCell, SacCell)).Save(Path.Combine(outDir, "Pet_Sacrifice_FX_Sheet.png"));
        new Cv(Strip(com, ComCell, ComCell)).Save(Path.Combine(outDir, "Pet_Shard_Combine_Sheet.png"));

        // Review board (not for Unity): sacrifice over the sitting dog (dog fades 100/60/25 %), then combine. x4 on slate.
        var dog = Img.Cell("Docs/PetArt/Species/Pet_Dog.png", 24, 20, 4, 0);
        int k = 4, gap = 8;
        var board = new Cv(16 + 8 * (ComCell * k + gap), 16 + SacCell * k + 24 + ComCell * k + 16);
        board.Clear(P.H("1C1F26"));
        float[] fade = { 1f, 0.6f, 0.25f, 0f, 0f, 0f };
        for (int f = 0; f < sac.Count; f++)
        {
            var c = new Cv(SacCell, SacCell);
            if (fade[f] > 0) c.Blit(dog, 12, SacCell - 20 - 3, 1, fade[f]);
            c.Blit(sac[f], 0, 0);
            board.Blit(c.B, 16 + f * (SacCell * k + gap), 16, k);
        }
        for (int f = 0; f < com.Count; f++) board.Blit(com[f], 16 + f * (ComCell * k + gap), 16 + SacCell * k + 24, k);
        board.Save(Path.Combine(outDir, "Shard_FX_Review_Board.png"));
    }

    public static void RunSample(string outDir)
    {
        Directory.CreateDirectory(outDir);
        var i16 = Icon16(); var i32 = Icon32();
        new Cv(i16).Save(Path.Combine(outDir, "UI_Item_PetEggShard.png"));
        new Cv(i32).Save(Path.Combine(outDir, "UI_PetEggShard_32.png"));

        // review: egg icon beside the shard icon in bag slots, and the 32 px version, on the slate menu colour
        var slot = Img.Load("Docs/UI/export/Slots/UI_Inventory_Slot.png");
        var egg = Img.Load("Docs/PetArt/Egg/UI_Item_MysteryPetEgg.png");
        Func<Bitmap, Bitmap> inSlot = ic => { var s = new Cv(slot.Width, slot.Height); s.Blit(slot, 0, 0); s.Blit(ic, 3, 3); return s.B; };
        int k = 6;
        var board = new Cv(16 + slot.Width * k * 2 + 16 + 32 * k + 16 + 16, 32 * k + 32);
        board.Clear(P.H("1C1F26"));
        board.Blit(inSlot(egg), 16, 16, k);
        board.Blit(inSlot(i16), 16 + slot.Width * k + 8, 16, k);
        board.Blit(i32, 16 + slot.Width * k * 2 + 32, 16, k);
        board.Save(Path.Combine(outDir, "Shard_Sample_Board.png"));
    }
}
