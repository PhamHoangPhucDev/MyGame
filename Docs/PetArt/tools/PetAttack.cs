using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

// Pet attack sheets + shared hit FX + dragon fire.
// Attack frames are built from the approved species sheets (Docs/PetArt/Species) with whole-pixel moves only:
// shift, drop one body row (squash) or repeat one body row (stretch). No scaling, no rotation, so no new pixel noise.
// No anti-aliasing, no semi-transparent pixels.
public static class PetAttack
{
    public const int CW = 32, CH = 24;          // attack cell: the 24x20 body sits at (4,4); feet stay on the bottom row
    const int BX = 4, BY = 4;
    public static readonly string[] Names = { "Dog", "Cat", "Fox", "Rabbit", "Wolf", "BabyDragon" };

    enum Kind { Bite, Claw, Hop, Fire }
    static Kind KindOf(int sp) { return sp == 3 ? Kind.Hop : sp == 5 ? Kind.Fire : (sp == 1 || sp == 2) ? Kind.Claw : Kind.Bite; }

    static Bitmap Body(int sp, int row, int col) { return Img.Cell("Docs/PetArt/Species/Pet_" + Names[sp] + ".png", 24, 20, col, row); }

    static void Rows(Bitmap b, out int top, out int bottom)
    {
        top = b.Height; bottom = -1;
        for (int y = 0; y < b.Height; y++)
            for (int x = 0; x < b.Width; x++)
                if (b.GetPixel(x, y).A > 0) { top = Math.Min(top, y); bottom = Math.Max(bottom, y); }
    }

    /// <summary>Drop the middle body row: 1 px shorter, feet unchanged.</summary>
    static Bitmap Squash(Bitmap b)
    {
        int t, btm; Rows(b, out t, out btm);
        int m = (t + btm) / 2;
        var o = new Cv(b.Width, b.Height);
        for (int y = 0; y < b.Height; y++)
            for (int x = 0; x < b.Width; x++)
            {
                Color c = b.GetPixel(x, y);
                if (c.A == 0) continue;
                if (y < m) o.Set(x, y + 1, c); else if (y > m) o.Set(x, y, c);
            }
        return o.B;
    }

    /// <summary>Repeat the middle body row: 1 px taller, feet unchanged.</summary>
    static Bitmap Stretch(Bitmap b)
    {
        int t, btm; Rows(b, out t, out btm);
        int m = (t + btm) / 2;
        var o = new Cv(b.Width, b.Height);
        for (int y = 0; y < b.Height; y++)
            for (int x = 0; x < b.Width; x++)
            {
                Color c = b.GetPixel(x, y);
                if (c.A == 0) continue;
                if (y <= m) o.Set(x, y - 1, c);
                if (y >= m) o.Set(x, y, c);
            }
        return o.B;
    }

    static Bitmap Place(Bitmap body, int dx, int dy)
    {
        var cv = new Cv(CW, CH);
        cv.Blit(body, BX + dx, BY + dy);
        return cv.B;
    }

    /// <summary>Front-most mouth pixel for the side view (right-most solid pixel in the upper half).</summary>
    static Point Mouth(Bitmap cell)
    {
        int bx = -1, by = 0;
        for (int y = 0; y < CH * 2 / 3; y++)
            for (int x = CW - 1; x > bx; x--)
                if (cell.GetPixel(x, y).A > 0) { bx = x; by = y; break; }
        return new Point(bx, by);
    }

    static void Ember(Cv cv, int x, int y, int size)
    {
        cv.Set(x, y, P.GoldW);
        if (size >= 1) { cv.Set(x + 1, y, P.OrangeL); cv.Set(x, y - 1, P.Orange); cv.Set(x, y + 1, P.Orange); }
        if (size >= 2) { cv.Set(x + 2, y, P.Orange); cv.Set(x + 1, y - 1, P.Red); cv.Set(x + 1, y + 1, P.Red); cv.Set(x + 3, y, P.Red); }
    }

    /// <summary>4 frames for one view: 0 wind-up, 1 lunge, 2 hit, 3 recover.</summary>
    public static List<Bitmap> Frames(int sp, int row)
    {
        var k = KindOf(sp);
        var f = new List<Bitmap>();
        if (row == 2)   // side, faces right
        {
            switch (k)
            {
                case Kind.Hop:
                    f.Add(Place(Squash(Body(sp, 2, 0)), -1, 0));
                    f.Add(Place(Stretch(Body(sp, 2, 2)), 1, -5));
                    f.Add(Place(Squash(Body(sp, 2, 3)), 4, 0));
                    f.Add(Place(Body(sp, 2, 0), 2, 0));
                    break;
                case Kind.Fire:
                    f.Add(Place(Body(sp, 2, 0), -2, -1));
                    f.Add(Place(Body(sp, 2, 1), -2, -1));
                    f.Add(Place(Body(sp, 2, 2), 1, 0));
                    f.Add(Place(Body(sp, 2, 3), 0, 0));
                    // ember in the mouth on the inhale, a puff on the breath
                    { var m = Mouth(f[1]); var cv = new Cv(f[1]); Ember(cv, m.X + 1, m.Y + 1, 0); }
                    { var m = Mouth(f[2]); var cv = new Cv(f[2]); Ember(cv, m.X + 1, m.Y + 1, 2); }
                    break;
                default:    // bite / claw: crouch back, leap, strike, settle
                    f.Add(Place(Squash(Body(sp, 2, 0)), -2, 0));
                    f.Add(Place(Stretch(Body(sp, 2, 2)), 3, -2));
                    f.Add(Place(Body(sp, 2, 1), 5, 0));
                    f.Add(Place(Body(sp, 2, 0), 2, 0));
                    break;
            }
        }
        else if (row == 0)  // down (toward the camera)
        {
            if (k == Kind.Hop) { f.Add(Place(Squash(Body(sp, 0, 0)), 0, 0)); f.Add(Place(Body(sp, 0, 2), 0, -5)); f.Add(Place(Squash(Body(sp, 0, 3)), 0, 0)); f.Add(Place(Body(sp, 0, 0), 0, 0)); }
            else if (k == Kind.Fire) { f.Add(Place(Body(sp, 0, 0), 0, -2)); f.Add(Place(Body(sp, 0, 1), 0, -2)); f.Add(Place(Stretch(Body(sp, 0, 2)), 0, 0)); f.Add(Place(Body(sp, 0, 3), 0, -1)); }
            else { f.Add(Place(Squash(Body(sp, 0, 0)), 0, 0)); f.Add(Place(Stretch(Body(sp, 0, 1)), 0, -2)); f.Add(Place(Stretch(Stretch(Body(sp, 0, 1))), 0, 0)); f.Add(Place(Body(sp, 0, 0), 0, 0)); }
        }
        else                // up (away from the camera)
        {
            if (k == Kind.Hop) { f.Add(Place(Squash(Body(sp, 1, 0)), 0, 0)); f.Add(Place(Body(sp, 1, 2), 0, -6)); f.Add(Place(Squash(Body(sp, 1, 3)), 0, -2)); f.Add(Place(Body(sp, 1, 0), 0, 0)); }
            else if (k == Kind.Fire) { f.Add(Place(Body(sp, 1, 0), 0, 0)); f.Add(Place(Body(sp, 1, 1), 0, 0)); f.Add(Place(Body(sp, 1, 2), 0, -2)); f.Add(Place(Body(sp, 1, 3), 0, -1)); }
            else { f.Add(Place(Squash(Body(sp, 1, 0)), 0, 0)); f.Add(Place(Stretch(Body(sp, 1, 2)), 0, -3)); f.Add(Place(Body(sp, 1, 1), 0, -4)); f.Add(Place(Body(sp, 1, 0), 0, -1)); }
        }
        return f;
    }

    /// <summary>4 cols x 3 rows of 32x24: rows 0 down, 1 up, 2 side (faces right).</summary>
    public static Bitmap Sheet(int sp)
    {
        var cv = new Cv(CW * 4, CH * 3);
        for (int r = 0; r < 3; r++)
        {
            var fr = Frames(sp, r);
            for (int c = 0; c < 4; c++) cv.Blit(fr[c], c * CW, r * CH);
        }
        return cv.B;
    }

    // ---------- hit FX, 16x16, 3 frames per row ----------
    public const int HX = 16;

    static void Line(Cv cv, int x0, int y0, int x1, int y1, Color c, bool dotted)
    {
        int n = Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0));
        for (int s = 0; s <= n; s++)
        {
            if (dotted && s % 2 == 1) continue;
            cv.Set(x0 + (int)Math.Round((x1 - x0) * (double)s / Math.Max(1, n)), y0 + (int)Math.Round((y1 - y0) * (double)s / Math.Max(1, n)), c);
        }
    }

    static void Star(Cv cv, int x, int y, int r, Color core, Color edge)
    {
        for (int d = 1; d <= r; d++)
        {
            Color c = d == r ? edge : core;
            cv.Set(x + d, y, c); cv.Set(x - d, y, c); cv.Set(x, y + d, c); cv.Set(x, y - d, c);
            if (d < r) { cv.Set(x + d, y + d, edge); cv.Set(x - d, y - d, edge); cv.Set(x + d, y - d, edge); cv.Set(x - d, y + d, edge); }
        }
        cv.Set(x, y, P.TextW);
    }

    /// <summary>Curved jaw: red gum arc with two fangs pointing at the other jaw. up = upper jaw.</summary>
    static void Jaw(Cv cv, int cx, int cy, bool up)
    {
        int s = up ? -1 : 1;
        for (int i = -6; i <= 6; i++)
        {
            int y = cy + s * (i * i / 9);                // arc bends away from the bite
            cv.Set(cx + i, y + s, P.RedD);
            cv.Set(cx + i, y, P.Red);
        }
        foreach (int fx in new[] { -3, 3 })
        {
            int y = cy + s * (fx * fx / 9);
            cv.Set(fx < 0 ? cx + fx : cx + fx, y - s, P.TextW);
            cv.Set(cx + fx - (fx < 0 ? -1 : 1), y - s, P.TextW);
            cv.Set(cx + fx, y - 2 * s, P.TextW);          // fang tip
        }
        cv.Set(cx, cy - s, P.TextW);                      // small middle tooth
    }

    static Bitmap Bite(int f)
    {
        var cv = new Cv(HX, HX);
        int gap = f == 0 ? 3 : f == 1 ? 1 : 0;
        Jaw(cv, 8, 5 - gap, true);
        Jaw(cv, 8, 10 + gap, false);
        if (f == 2) { Star(cv, 2, 8, 2, P.RedL, P.Red); Star(cv, 13, 3, 1, P.TextW, P.RedL); cv.Set(14, 13, P.RedL); }
        return cv.B;
    }

    static Bitmap Claw(int f, bool ice)
    {
        var cv = new Cv(HX, HX);
        Color core = P.TextW, edge = ice ? P.BlueL : P.GoldL;
        int len = f == 0 ? 5 : 10;
        for (int k = 0; k < 3; k++)
        {
            int x0 = 3 + k * 4, y0 = 2;
            if (f == 2) { Line(cv, x0 + 1, y0 + 1, x0 + 1 + len - 3, y0 + len - 2, edge, true); continue; }
            Line(cv, x0, y0, x0 + len - 3, y0 + len - 1, core, false);
            Line(cv, x0 + 1, y0, x0 + len - 2, y0 + len - 1, edge, false);
        }
        if (f == 1) Star(cv, 13, 12, 1, core, edge);
        return cv.B;
    }

    static Bitmap Stomp(int f)
    {
        var cv = new Cv(HX, HX);
        int cx = 8, cy = 12;
        if (f == 0) { Star(cv, cx, cy - 1, 2, P.TextW, P.GoldL); return cv.B; }
        int rx = f == 1 ? 5 : 7, ry = f == 1 ? 2 : 3;
        Color c = f == 1 ? P.ParchL : P.Parch;
        for (int a = 0; a < 360; a += 15)
        {
            double r = a * Math.PI / 180;
            if (f == 2 && (a / 15) % 2 == 1) continue;
            cv.Set(cx + (int)Math.Round(Math.Cos(r) * rx), cy + (int)Math.Round(Math.Sin(r) * ry), c);
        }
        // dust chips kicked up
        int up = f == 1 ? 3 : 5;
        cv.Set(cx - rx, cy - up, P.ParchD); cv.Set(cx + rx, cy - up, P.ParchD); cv.Set(cx - 2, cy - up - 2, P.Parch); cv.Set(cx + 3, cy - up - 1, P.Parch);
        if (f == 1) Star(cv, cx, cy - 1, 1, P.TextW, P.GoldL);
        return cv.B;
    }

    /// <summary>Rows: 0 bite, 1 claw, 2 ice claw (wolf), 3 stomp. 3 frames each.</summary>
    public static Bitmap HitSheet()
    {
        var cv = new Cv(HX * 3, HX * 4);
        for (int f = 0; f < 3; f++)
        {
            cv.Blit(Bite(f), f * HX, 0);
            cv.Blit(Claw(f, false), f * HX, HX);
            cv.Blit(Claw(f, true), f * HX, HX * 2);
            cv.Blit(Stomp(f), f * HX, HX * 3);
        }
        return cv.B;
    }

    // ---------- dragon fire, 24x24 cells ----------
    public const int FX = 24;

    static void Blob(Cv cv, int cx, int cy, int r, Color c)
    {
        for (int j = -r; j <= r; j++)
            for (int i = -r; i <= r; i++)
                if (i * i + j * j <= r * r + r / 2) cv.Set(cx + i, cy + j, c);
    }

    /// <summary>Fireball flying right. Two-frame flicker loop.</summary>
    static Bitmap Fireball(int f)
    {
        var cv = new Cv(FX, FX);
        int cx = 15, cy = 12;
        // tail: three shrinking puffs behind
        Blob(cv, cx - 9, cy + (f == 0 ? -1 : 1), 1, P.RedD);
        Blob(cv, cx - 6, cy + (f == 0 ? 1 : -1), 2, P.Red);
        Blob(cv, cx - 1, cy, 4, P.Red);
        Blob(cv, cx, cy, 3, P.Orange);
        Blob(cv, cx + 1, cy - (f == 0 ? 0 : 1), 2, P.OrangeL);
        Blob(cv, cx + 1, cy - (f == 0 ? 0 : 1), 1, P.GoldW);
        cv.Set(cx - 12, cy + (f == 0 ? 2 : -2), P.Orange);
        cv.Set(cx - 4, cy - 5 + f, P.OrangeL);
        return Img.Outline(cv.B, P.RedD);
    }

    static Bitmap Impact(int f)
    {
        var cv = new Cv(FX, FX);
        int cx = 12, cy = 14;
        if (f == 0) { Blob(cv, cx, cy, 5, P.OrangeL); Blob(cv, cx, cy, 3, P.GoldW); }
        else if (f == 1)
        {
            Blob(cv, cx, cy, 8, P.Red); Blob(cv, cx, cy - 1, 6, P.Orange); Blob(cv, cx, cy - 2, 3, P.OrangeL);
            for (int k = 0; k < 8; k++)
            {
                double a = k * Math.PI / 4 + 0.3;
                cv.Set(cx + (int)Math.Round(Math.Cos(a) * 10), cy + (int)Math.Round(Math.Sin(a) * 10), P.OrangeL);
            }
        }
        else
        {
            // flames licking up, embers
            for (int k = -2; k <= 2; k++)
            {
                int h = 6 - Math.Abs(k) * 2 + (k == 0 ? 2 : 0);
                for (int y = 0; y < h; y++) cv.Set(cx + k * 3, cy + 4 - y, y > h - 2 ? P.OrangeL : P.Red);
                cv.Set(cx + k * 3 + 1, cy + 4 - h / 2, P.Orange);
            }
            cv.Set(cx - 7, cy - 6, P.GoldL); cv.Set(cx + 6, cy - 9, P.OrangeL); cv.Set(cx + 9, cy - 3, P.Gold); cv.Set(cx - 3, cy - 10, P.Orange);
        }
        return f == 2 ? cv.B : Img.Outline(cv.B, P.RedD);
    }

    /// <summary>One row, 5 cells of 24x24: 0-1 fireball loop (faces right), 2-4 impact.</summary>
    public static Bitmap FireSheet()
    {
        var cv = new Cv(FX * 5, FX);
        cv.Blit(Fireball(0), 0, 0); cv.Blit(Fireball(1), FX, 0);
        for (int f = 0; f < 3; f++) cv.Blit(Impact(f), FX * (2 + f), 0);
        return cv.B;
    }

    public static void Run(string outDir, bool sampleOnly)
    {
        Directory.CreateDirectory(outDir);
        var hit = HitSheet(); var fire = FireSheet();
        new Cv(hit).Save(Path.Combine(outDir, "Pet_Hit_FX_Sheet.png"));
        new Cv(fire).Save(Path.Combine(outDir, "Pet_Dragon_Fire_Sheet.png"));
        var sheets = new Bitmap[Names.Length];
        for (int sp = 0; sp < Names.Length; sp++)
        {
            sheets[sp] = Sheet(sp);
            if (!sampleOnly) new Cv(sheets[sp]).Save(Path.Combine(outDir, "Pet_" + Names[sp] + "_Attack.png"));
        }
        Board(outDir, sampleOnly ? new[] { 0, 5 } : new[] { 0, 1, 2, 3, 4, 5 }, sheets, hit, fire, sampleOnly ? "Attack_Sample_Board.png" : "Attack_Review_Board.png");
    }

    /// <summary>Review board (not for Unity): per species, the side attack with its FX at the hit frame, then all 3 rows. x4.</summary>
    static void Board(string outDir, int[] list, Bitmap[] sheets, Bitmap hit, Bitmap fire, string name)
    {
        int k = 4, gap = 6, rowH = CH * 3 * 2 + 20;
        int w = 16 + 4 * (CW * k + gap) + 24 + 4 * (CW * k + gap) + 8;
        var board = new Cv(w, 16 + list.Length * rowH);
        board.Clear(P.H("1C1F26"));
        for (int n = 0; n < list.Length; n++)
        {
            int sp = list[n], y0 = 16 + n * rowH;
            var kind = KindOf(sp);
            // left: side row with FX composited on frames 2-3 (what the player sees)
            for (int f = 0; f < 4; f++)
            {
                var c = new Cv(CW + 24, CH);
                c.Blit(Frames(sp, 2)[f], 0, 0);
                if (kind == Kind.Fire)
                {
                    if (f == 2) c.Blit(Crop(fire, 0, 0, FX, FX), 18, -2);
                    if (f == 3) c.Blit(Crop(fire, FX, 0, FX, FX), 26, -2);
                }
                else if (f >= 2)
                {
                    int hrow = kind == Kind.Bite ? (sp == 4 ? 0 : 0) : kind == Kind.Claw ? 1 : 3;
                    var fx = Crop(hit, (f - 2 + 1) * HX, hrow * HX, HX, HX);
                    c.Blit(fx, kind == Kind.Hop ? 22 : 26, kind == Kind.Hop ? 8 : 2);
                    if (sp == 4 && f == 3) c.Blit(Crop(hit, HX, 2 * HX, HX, HX), 28, 4);   // wolf adds the ice claw
                }
                board.Blit(c.B, 16 + f * (CW * k + gap), y0, k);
            }
            // right: full sheet, 3 rows (down, up, side), no FX
            board.Blit(sheets[sp], 16 + 4 * (CW * k + gap) + 24 + 64, y0, k / 2 > 0 ? 2 : 1);
        }
        board.Save(Path.Combine(outDir, name));
    }

    static Bitmap Crop(Bitmap b, int x, int y, int w, int h)
    {
        var cv = new Cv(w, h);
        for (int j = 0; j < h; j++) for (int i = 0; i < w; i++) cv.Set(i, j, b.GetPixel(x + i, y + j));
        return cv.B;
    }
}
