using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

// Selene (witch-mage) — CANDIDATE: Up / Side base frames (Down = approved roster sprite), staff poses (rest upright,
// raise to charge, tilt toward the target to cast), magic orb, star-burst key frame. 14x21 template, sole on row 29.
public static class SeleneGen
{
    public static string[] Down { get { return HeroConceptGen.Roster.First(r => r.Id == "3").Rows; } }
    public static Dictionary<char, string> Pal { get { return HeroConceptGen.Roster.First(r => r.Id == "3").Pal; } }

    // Back: hat tip leaning left (mirror of the front), gold hat band, long silver-violet hair down the back to the
    // waist, navy robe back, gold hem, shoes.
    public static readonly string[] Up =
    {
        "....kk........",
        "...kqpk.......",
        "...kPqpk......",
        "...kPqppk.....",
        "..kPPppppk....",
        "..kPPmmmmPk...",
        "kPPpppppppqqpk",
        "KDHHHHHHHHHHDK",
        "KDHHLHHHHLHHDK",
        "KDHHLHHHHLHHDK",
        "KDHHLHHHHLHHDK",
        ".KDHLHHHHLHDK.",
        ".KDHDHHHHDHDK.",
        ".KDDHHHHHHDDK.",
        "..kRrHDDHruk..",
        "..kRrrrrruuk..",
        ".kRRrrrrrruuk.",
        ".kRRrrrrrruuk.",
        ".kmmmmmmmmmmk.",
        "....kBkkBk....",
        "...kBBkkBBk...",
    };

    // 3/4 facing right: hat tip flops back (left), buckle front-right, face + eyes shifted right, hair mass on the
    // left, collar and clasp shifted right, toes pointing right.
    public static readonly string[] Side =
    {
        "...kk.........",
        "...kqpk.......",
        "....kqpk......",
        "....kPqppk....",
        "....kPppypk...",
        "...kPPmmmmPk..",
        "kpqqpppppppPPk",
        "KDHHHHsHHHsHHK",
        "KDHHHsssssssHK",
        "KDHHHssseesseK",
        "KDHHHsssEwssEK",
        ".KDHHssssssspK",
        ".KDHDkuccccrk.",
        ".KDDkurmmrrk..",
        "..kuurrmrrRk..",
        "..kuurrmrrRk..",
        ".kuurrrmrrRRk.",
        ".kuurrrmrrRRk.",
        ".kmmmmmmmmmmk.",
        "....kBkkBk....",
        "....kBBkkBBk..",
    };

    public static string[] Base(string v) { return v == "Down" ? Down : v == "Up" ? Up : Side; }

    static Color C(string h) { return HeroConceptGen.Hex(h); }

    // VFX palette: white, pink-light, pink-violet, deep violet, star blue
    public static readonly string[] FX = { "FFFFFF", "FFB8F0", "D860C8", "7A3AA8", "B8D8FF" };

    // ---------- staff (v2): grip = anchor (0,0) = hand; 2 px butt, wooden shaft, gold band + two gold prongs,
    // round pink gem (plus shape, always axis-aligned) with a white glint. Total ~14 px: up to the hat brim.
    public const int ShaftLong = 9, ShaftShort = 6;   // rest / raise ; tilted cast (diagonal)
    public static EquipGen.Spr Staff(int dir, bool shortShaft)
    {
        var d = EquipGen.Dir[dir]; var p = new Point(-d.Y, d.X); var s = new EquipGen.Spr();
        int L = shortShaft ? ShaftShort : ShaftLong;
        for (int k = -2; k < L - 1; k++) s.Set(d.X * k, d.Y * k, C(k % 3 == 0 ? "9A6A3C" : "6A4428"));
        s.Set(d.X * (L - 1), d.Y * (L - 1), C("E8C048"));                                   // gold band
        s.Set(d.X * L + p.X, d.Y * L + p.Y, C("E8C048")); s.Set(d.X * L - p.X, d.Y * L - p.Y, C("E8C048")); // prongs
        s.Set(d.X * L, d.Y * L, C("9A6A3C"));
        var g = new Point(d.X * (L + 2), d.Y * (L + 2));
        // 3x3 gem, lit from top-left: glint, light, mid, dark rim at bottom-right
        string[,] gem = { { "FFFFFF", "FFD0F0", "D860C8" }, { "FFD0F0", "D860C8", "C04A8A" }, { "D860C8", "C04A8A", "7A2A5A" } };
        for (int gy = 0; gy < 3; gy++) for (int gx = 0; gx < 3; gx++) s.Set(g.X - 1 + gx, g.Y - 1 + gy, C(gem[gy, gx]));
        return EquipGen.Outline(s, C("1A1426"));
    }

    public static EquipGen.Spr Hand { get { return EquipGen.Hand("FCDCC0", "E8B494", "1A1426"); } }

    public class P { public int X, Y, Dir; public bool Short, Front = true; }

    // rest: staff upright in the right hand (Down viewer-left, Up viewer-right, Side the near/front hand)
    public static P Rest(string v)
    {
        if (v == "Down") return new P { X = 10, Y = 25, Dir = 2 };
        if (v == "Up") return new P { X = 21, Y = 25, Dir = 2 };
        return new P { X = 22, Y = 25, Dir = 2 };
    }

    // f: 0 raise (charge, crystal high), 1 cast (tilt toward the aim, crystal = spawn point), 2 recover
    public static P Cast(string v, int f)
    {
        if (f == 2) { var r = Rest(v); r.Y += 1; return r; }
        if (f == 0)
        {
            if (v == "Down") return new P { X = 10, Y = 21, Dir = 2 };
            if (v == "Up") return new P { X = 21, Y = 21, Dir = 2, Front = false };
            return new P { X = 21, Y = 21, Dir = 2 };
        }
        if (v == "Down") return new P { X = 19, Y = 23, Dir = 7, Short = true };          // crystal forward-down at the camera
        if (v == "Up") return new P { X = 19, Y = 19, Dir = 1, Short = true, Front = false }; // crystal up-forward, past the hat
        return new P { X = 20, Y = 22, Dir = 1, Short = true };                          // crystal up-forward to the right
    }

    // gem centre (shaft + 2 along dir)
    public static Point Crystal(P p) { var d = EquipGen.Dir[p.Dir]; int k = (p.Short ? ShaftShort : ShaftLong) + 2; return new Point(p.X + d.X * k, p.Y + d.Y * k); }

    public static Bitmap Compose(string v, P p, bool mirror)
    {
        var body = EquipGen.Body(EquipGen.Grid(Base(v)), Pal);
        var layers = new List<EquipGen.Layer>
        {
            new EquipGen.Layer { S = Staff(p.Dir, p.Short), X = p.X, Y = p.Y },
            new EquipGen.Layer { S = Hand, X = p.X - 1, Y = p.Y - 1 },
        };
        var bmp = p.Front ? EquipGen.Compose(body, new List<EquipGen.Layer>(), layers) : EquipGen.Compose(body, layers, new List<EquipGen.Layer>());
        if (!mirror) return bmp;
        var o = new Bitmap(48, 48, PixelFormat.Format32bppArgb);
        for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++) o.SetPixel(47 - x, y, bmp.GetPixel(x, y));
        return o;
    }

    // ---------- VFX ----------
    // crystal glow while charging (F0): small ring + sparkle around the crystal, drawn into a 48x48 composite
    public static void ChargeGlow(Bitmap bmp, Point crystal)
    {
        var cv = new ArenKitSheets.Cv(48, 48, 8, 8);
        ArenKitSheets.Ring(cv, C(FX[2]), crystal.X, crystal.Y, 4, 1.0, 1.0, null);
        ArenKitSheets.Star(cv, new[] { FX[0], FX[1], FX[2] }, crystal.X, crystal.Y - 5, 2);
        cv.Set(crystal.X + 5, crystal.Y + 1, C(FX[4])); cv.Set(crystal.X - 5, crystal.Y + 2, C(FX[1]));
        for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++) if (cv.Px[x, y].HasValue) bmp.SetPixel(x, y, cv.Px[x, y].Value);
    }

    // magic orb projectile 16x16, pivot (8,8): violet rim, pink body, light highlight, white spark; 2-frame twinkle
    public static Bitmap Orb(bool big, int frame)
    {
        var cv = new ArenKitSheets.Cv(16, 16, 0, 0);
        double r = big ? 4.6 : 3.0;
        ArenKitSheets.Disc(cv, C(FX[3]), 8, 8, r + 0.7);
        ArenKitSheets.Disc(cv, C(FX[2]), 8, 8, r);
        ArenKitSheets.Disc(cv, C(FX[1]), 7.2, 7.2, r * 0.55);
        cv.Set(7, 6, C(FX[0]));
        if (frame == 1) { cv.Set(8 + (int)r + 1, 8, C(FX[4])); cv.Set(8, 8 - (int)r - 2, C(FX[0])); }
        else { cv.Set(8 - (int)r - 1, 9, C(FX[1])); }
        var bmp = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
        for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) if (cv.Px[x, y].HasValue) bmp.SetPixel(x, y, cv.Px[x, y].Value);
        return bmp;
    }

    // 4-point star burst (R3 finisher), drawn on a canvas at (cx, cy)
    public static void StarBurst(ArenKitSheets.Cv cv, double cx, double cy, int R)
    {
        ArenKitSheets.Ring(cv, C(FX[3]), cx, cy, R + 2, 1.2, 0.7, (bx, by) => ((int)((Math.Atan2(by - cy, bx - cx) * 180 / Math.PI + 360) / 30) % 2) == 0);
        for (int i = -R; i <= R; i++)
        {
            int a = Math.Abs(i); string col = a <= R / 3 ? FX[0] : a <= 2 * R / 3 ? FX[1] : FX[2];
            cv.Set(cx + i, cy, C(col)); cv.Set(cx, cy + i * 0.8, C(col));
            if (a <= R / 3) { cv.Set(cx + i, cy - 1, C(FX[1])); cv.Set(cx + i, cy + 1, C(FX[1])); cv.Set(cx - 1, cy + i * 0.8, C(FX[1])); cv.Set(cx + 1, cy + i * 0.8, C(FX[1])); }
        }
        for (int i = 1; i <= R / 3; i++) { cv.Set(cx + i, cy + i, C(FX[2])); cv.Set(cx - i, cy - i, C(FX[2])); cv.Set(cx + i, cy - i, C(FX[2])); cv.Set(cx - i, cy + i, C(FX[2])); }
        cv.Set(cx, cy, C(FX[0]));
        ArenKitSheets.Dots(cv, new[] { FX[4], FX[1], FX[0] }, 77, cx, cy, R + 3, R * 0.7 + 2, 8);
    }

    // key frame: Side cast, orb in flight with a sparkle trail, star burst at the target; 128x48, hero cell at (0,0)
    public static Bitmap KeyTile()
    {
        var bmp = new Bitmap(128, 48, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp)) g.Clear(Color.FromArgb(255, 0x74, 0xA5, 0x87));
        var hero = Compose("Side", Cast("Side", 1), false);
        for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++) { var c = hero.GetPixel(x, y); if (c.A > 0) bmp.SetPixel(x, y, c); }
        var fx = new ArenKitSheets.Cv(128, 48, 0, 0);
        var cr = Crystal(Cast("Side", 1)); int sx = 8 + cr.X, sy = 8 + cr.Y;
        // sparkle trail from the crystal to the orb (alternating twinkles, thinning)
        int ox = 66, oy = 24;
        for (int k = 0; k <= 20; k++)
        {
            double t = k / 20.0, x = sx + (ox - sx) * t, y = sy + (oy - sy) * t;
            if (k % 3 == 0) fx.Set(x, y, C(k % 6 == 0 ? FX[1] : FX[2]));
            if (k % 5 == 2) fx.Set(x, y - 2, C(FX[4]));
            if (k % 7 == 4) fx.Set(x, y + 2, C(FX[0]));
        }
        ArenKitSheets.Star(fx, new[] { FX[0], FX[1], FX[2] }, sx, sy, 2);
        StarBurst(fx, 104, 26, 9);
        for (int y = 0; y < 48; y++) for (int x = 0; x < 128; x++) if (fx.Px[x, y].HasValue) bmp.SetPixel(x, y, fx.Px[x, y].Value);
        var orb = Orb(false, 1);
        for (int y = 0; y < 16; y++) for (int x = 0; x < 16; x++) { var c = orb.GetPixel(x, y); if (c.A > 0) bmp.SetPixel(ox - 8 + x, oy - 8 + y, c); }
        return bmp;
    }
}
