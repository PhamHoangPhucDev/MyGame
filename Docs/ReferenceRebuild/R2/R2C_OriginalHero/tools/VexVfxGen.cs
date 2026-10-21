using System;
using System.Drawing;
using System.Drawing.Imaging;

// Vex skill VFX added after the class audit (crimson / ink black / silver palette, VexEquip.Crimson):
//  - ShadowStep: 32x32 x 4 frames, played at the departure point and at the arrival point behind the target.
//    Inky black-violet burst with sharp wisps, radiating crimson streaks and a flashing pair of red eyes.
//  - BladeDance: 64x48 x 6 frames (one per hit), crimson crescents whirling around Vex on a flattened top-down
//    ellipse. Split into _Back (drawn behind the body) and _Front (drawn in front) like Aren_WhirlRing.
public static class VexVfxGen
{
    static readonly string[] Cr = VexEquip.Crimson;   // FFE0E0, FF6060, C82838, 6A1020, C0C4D0
    static Color H(string s) { return HeroConceptGen.Hex(s); }

    static void Blit(VfxConceptGen.Canvas cv, Bitmap bmp, int ox, int sx, int sy, int w, int h)
    {
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                int X = sx + x, Y = sy + y;
                if (X < 0 || Y < 0 || X >= VfxConceptGen.W || Y >= VfxConceptGen.H) continue;
                if (cv.Px[X, Y].HasValue) bmp.SetPixel(ox + x, y, cv.Px[X, Y].Value);
            }
    }

    public static Bitmap ShadowStep()
    {
        var bmp = new Bitmap(32 * 4, 32, PixelFormat.Format32bppArgb);
        var ink = H("1A1420"); var vio = H("3A2A4A"); var vio2 = H("5A4670");
        // rising inky flame column standing on a ground puff (feet at row 28, like a body cell); tongues lick upward,
        // f2 breaks into drifting blobs, f3 leaves a few wisps. Crimson streaks cross it; red eyes at head height.
        int[] tongueX = { -5, -2, 1, 4 }; int[] tongueH = { 8, 13, 15, 10 };
        for (int f = 0; f < 4; f++)
        {
            var cv = new VfxConceptGen.Canvas();
            double cx = 16, gy = 27;
            // ground puff (widens every frame)
            double rx = 6 + f * 2, ry = 2 + f * 0.5;
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                {
                    double d = (x + 0.5 - cx) * (x + 0.5 - cx) / (rx * rx) + (y + 0.5 - gy) * (y + 0.5 - gy) / (ry * ry);
                    if (d <= 1 && (f < 3 || (x + y) % 3 != 0)) cv.Set(x, y, d > 0.55 ? (f >= 2 ? vio2 : vio) : ink);
                }
            // column + tongues (f0 tall and dense, f1 tallest, f2 breaking, f3 wisps)
            for (int i = 0; i < tongueX.Length; i++)
            {
                double h = tongueH[i] * (f == 0 ? 0.85 : f == 1 ? 1.0 : f == 2 ? 0.8 : 0.5), lift = f >= 2 ? (f - 1) * 3 : 0;
                for (double t = 0; t < h; t += 0.5)
                {
                    double w = Math.Max(0.6, 2.4 * (1 - t / h)), sway = Math.Sin(t * 0.5 + i) * 1.2;
                    double x = cx + tongueX[i] + sway, y = gy - 1 - t - lift;
                    if (f >= 2 && ((int)(t + i) % 4) == 0) continue;          // breaking apart
                    if (f == 3 && t < h * 0.4) continue;                       // only drifting tops left
                    for (double dx = -w; dx <= w; dx += 0.5)
                        cv.Set((int)Math.Round(x + dx), (int)Math.Round(y), Math.Abs(dx) < w * 0.45 && f < 3 ? vio : ink);
                }
            }
            // crimson streaks: a crossing pair of thin slashes (f0), sparks (f1)
            if (f == 0)
            {
                VfxConceptGen.Line(cv, H(Cr[1]), 8, 22, 24, 10, 1); VfxConceptGen.Line(cv, H(Cr[2]), 9, 23, 25, 11, 1);
                VfxConceptGen.Line(cv, H(Cr[1]), 9, 9, 22, 21, 1);
            }
            else if (f == 1) VfxConceptGen.Dots(cv, new[] { Cr[1], Cr[0], Cr[2] }, 5, 16, 16, 11, 9, 9);
            // red eyes at head height (cell rows 13-14 = Vex's eye line), bright f0, glowing f1
            if (f <= 1)
                foreach (var ex in new[] { 13, 18 })
                {
                    var eye = H(f == 0 ? Cr[0] : Cr[1]);
                    cv.Set(ex, 13, eye); cv.Set(ex + 1, 13, eye);
                    if (f == 0) cv.Set(ex + (ex < 16 ? -1 : 2), 13, H(Cr[2]));
                }
            Blit(cv, bmp, f * 32, 0, 0, 32, 32);
        }
        return bmp;
    }

    // layer: 0 back (upper half of the ellipse, behind the body), 1 front (lower half)
    public static Bitmap BladeDance(int layer)
    {
        const int CW = 64, CH = 48; double cx = 32, cy = 28, R = 21, ys = 0.55;
        var bmp = new Bitmap(CW * 6, CH, PixelFormat.Format32bppArgb);
        for (int f = 0; f < 6; f++)
        {
            var cv = new VfxConceptGen.Canvas();
            // faint trail of the previous hit, then the current crescent (60 deg further round)
            if (f > 0) VfxConceptGen.Crescent(cv, new[] { Cr[3], Cr[3], Cr[3], Cr[3] }, cx, cy, R - 1, (f - 1) * 60 - 90, 90, 1.5, ys);
            VfxConceptGen.Crescent(cv, Cr, cx, cy, R, f * 60 - 90, 120, 5, ys);
            // inner thin echo + silver glints at the tip
            VfxConceptGen.Crescent(cv, new[] { Cr[1], Cr[1], Cr[2], Cr[3] }, cx, cy, R - 6, f * 60 - 60, 80, 1.5, ys);
            double tip = (f * 60 - 90 + 120) * Math.PI / 180;
            int tx = (int)Math.Round(cx + Math.Cos(tip) * R), ty = (int)Math.Round(cy + Math.Sin(tip) * R * ys);
            cv.Set(tx, ty, H(Cr[4])); cv.Set(tx + 1, ty - 1, H("FFFFFF")); cv.Set(tx - 1, ty + 1, H(Cr[4]));
            // split by layer around the body's centre line
            for (int y = 0; y < CH; y++) for (int x = 0; x < CW; x++)
                {
                    if (!cv.Px[x, y].HasValue) continue;
                    bool back = y < cy;
                    if ((layer == 0) == back) bmp.SetPixel(f * CW + x, y, cv.Px[x, y].Value);
                }
        }
        return bmp;
    }
}
