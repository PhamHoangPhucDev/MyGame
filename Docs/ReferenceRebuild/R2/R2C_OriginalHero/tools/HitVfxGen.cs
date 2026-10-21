using System;
using System.Drawing;
using System.Drawing.Imaging;

// Projectile impact VFX (added after the class audit). 24x24 cells, pivot = cell centre = the hit point, 4 frames.
//  - Lyra_ArrowHit.png: row 0 normal arrow (green starburst, ring, 2 leaves), row 1 charged arrow (bigger, gold sparks,
//    4 leaves). Lyra palette F8FFE0 / B8F078 / 6AC040 / 2E7A2E, gold F0D060.
//  - Vex_KnifeHit.png: silver X spark, short crimson slash, blood drops that fall and fade. Vex crimson palette.
public static class HitVfxGen
{
    const int N = 24;
    static Color H(string s) { return HeroConceptGen.Hex(s); }
    static readonly string[] Ly = VfxConceptGen.Pal["Lyra"];   // F8FFE0, B8F078, 6AC040, 2E7A2E, F0D060
    static readonly string[] Cr = VexEquip.Crimson;             // FFE0E0, FF6060, C82838, 6A1020, C0C4D0

    static void Blit(VfxConceptGen.Canvas cv, Bitmap bmp, int ox, int oy)
    {
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++) if (cv.Px[x, y].HasValue) bmp.SetPixel(ox + x, oy + y, cv.Px[x, y].Value);
    }

    // 3-px leaf: light tip, mid body, dark edge; flip mirrors it
    static void Leaf(VfxConceptGen.Canvas cv, int x, int y, bool flip)
    {
        int s = flip ? -1 : 1;
        cv.Set(x, y, H(Ly[1])); cv.Set(x + s, y - 1, H(Ly[0])); cv.Set(x - s, y + 1, H(Ly[2])); cv.Set(x, y + 1, H(Ly[3]));
    }

    static void Ring(VfxConceptGen.Canvas cv, Color c, double cx, double cy, double r, bool dashed)
    {
        for (int i = 0; i < 48; i++)
        {
            if (dashed && i % 3 == 0) continue;
            double a = i * Math.PI * 2 / 48;
            cv.Set((int)Math.Round(cx + Math.Cos(a) * r), (int)Math.Round(cy + Math.Sin(a) * r), c);
        }
    }

    public static Bitmap ArrowHit()
    {
        var bmp = new Bitmap(N * 4, N * 2, PixelFormat.Format32bppArgb);
        for (int row = 0; row < 2; row++)
        {
            bool ch = row == 1; double k = ch ? 1.35 : 1.0; int c = 12;
            for (int f = 0; f < 4; f++)
            {
                var cv = new VfxConceptGen.Canvas();
                if (f == 0)
                {
                    VfxConceptGen.Star(cv, ch ? new[] { Ly[0], Ly[4], Ly[1] } : Ly, c, c, ch ? 5 : 3);
                    foreach (var d in new[] { new[] { 1, 1 }, new[] { -1, 1 }, new[] { 1, -1 }, new[] { -1, -1 } })
                        VfxConceptGen.Line(cv, H(ch ? Ly[4] : Ly[1]), c + d[0] * 3, c + d[1] * 3, c + d[0] * (int)(5 * k), c + d[1] * (int)(5 * k), 1);
                }
                else
                {
                    double r = (2 + f * 2.2) * k;
                    if (f < 3) Ring(cv, H(f == 1 ? Ly[1] : Ly[2]), c, c, r, f == 2);
                    if (f == 1) { VfxConceptGen.Disc(cv, H(Ly[0]), c, c, 1.2 * k); }
                    // sparks flying outward
                    int n = ch ? 8 : 6;
                    for (int i = 0; i < n; i++)
                    {
                        if (f == 3 && i % 2 == 1) continue;
                        double a = i * Math.PI * 2 / n + 0.4; double rr = r + 1 + f;
                        var col = ch && i % 2 == 0 ? Ly[4] : f == 1 ? Ly[0] : Ly[1];
                        cv.Set((int)Math.Round(c + Math.Cos(a) * rr), (int)Math.Round(c + Math.Sin(a) * rr), H(col));
                    }
                    // leaves tossed up and drifting down
                    int leaves = ch ? 4 : 2;
                    for (int i = 0; i < leaves; i++)
                    {
                        double side = i % 2 == 0 ? -1 : 1, spread = 2 + f * 2.2 + (i / 2) * 2;
                        int lx = (int)Math.Round(c + side * spread), ly = (int)Math.Round(c - 3 - (i / 2) * 2 + (f - 1) * (f - 1) * 1.2 - f);
                        Leaf(cv, lx, ly, side > 0);
                    }
                }
                Blit(cv, bmp, f * N, row * N);
            }
        }
        return bmp;
    }

    public static Bitmap KnifeHit()
    {
        var bmp = new Bitmap(N * 4, N, PixelFormat.Format32bppArgb);
        string[] silver = { "FFFFFF", "C0C4D0", "8A8E9A" };
        int c = 12;
        var rnd = new Random(23);
        var drops = new double[7][];
        for (int i = 0; i < 7; i++) drops[i] = new[] { (rnd.NextDouble() - 0.5) * 2.4, -0.6 - rnd.NextDouble() * 1.2 };   // vx, vy
        for (int f = 0; f < 4; f++)
        {
            var cv = new VfxConceptGen.Canvas();
            if (f == 0)
            {
                VfxConceptGen.XSlash(cv, silver, c, c, 3, 1);
                cv.Set(c, c, H("FFFFFF"));
                VfxConceptGen.Dots(cv, new[] { Cr[1], Cr[2] }, 3, c, c, 6, 5, 5);
            }
            else
            {
                // short crimson slash across the hit point (fades)
                if (f < 3) { VfxConceptGen.Line(cv, H(f == 1 ? Cr[1] : Cr[2]), c - 5, c + 3, c + 5, c - 3, f == 1 ? 2 : 1); }
                if (f == 1) VfxConceptGen.Line(cv, H(Cr[0]), c - 3, c + 2, c + 3, c - 2, 1);
                // silver sparks (f1 only)
                if (f == 1) foreach (var d in new[] { new[] { 6, -1 }, new[] { -6, 1 }, new[] { 2, -6 }, new[] { -2, 6 } }) cv.Set(c + d[0], c + d[1], H(silver[1]));
                // blood drops: ballistic, fall with gravity, fewer and darker each frame
                for (int i = 0; i < drops.Length; i++)
                {
                    if (f == 3 && i % 2 == 0) continue;
                    double t = f * 1.6, x = c + drops[i][0] * t * 1.4, y = c + drops[i][1] * t * 1.4 + 0.45 * t * t;
                    var col = H(f == 1 ? Cr[1] : f == 2 ? Cr[2] : Cr[3]);
                    int X = (int)Math.Round(x), Y = (int)Math.Round(y);
                    cv.Set(X, Y, col); if (f < 3 && i % 3 == 0) cv.Set(X, Y + 1, H(Cr[3]));
                }
            }
            Blit(cv, bmp, f * N, 0);
        }
        return bmp;
    }
}
