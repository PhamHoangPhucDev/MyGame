using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

// Aren designed kit — CANDIDATE key frames (one per effect) composited on the approved Aren sprites.
// Palette = approved Aren slash (FxGen): white / ice-blue / gold. Tile 96x64, hero 48-canvas at (8,8) => body cell at (16,16).
public static class ArenKitVfxGen
{
    const int W = VfxConceptGen.W, H = VfxConceptGen.H;
    public static readonly string[] Ice = { "FFFFFF", "D8F0FF", "8CC8F0", "4A8AC8" };
    public static readonly string[] Gold = { "FFFFFF", "FFF4C8", "F0C850", "C89A30" };
    static Color C(string h) { return HeroConceptGen.Hex(h); }

    public static readonly string[] Labels =
    {
        "R3 dam thang (ket) - Side", "2 Khien Dap - Side", "3 Xung Phong - Side",
        "4 Loc Kiem - Down", "V Thanh Luy - Down", "C Chem Binh Minh - Side",
    };

    static Bitmap HeroAttack(string view, int f) { return EquipGen.ArenAttackComposite(view, f, EquipGen.SteelLongsword, EquipGen.HeaterShield, null); }
    static Bitmap HeroMove(string anim, string view, int f) { return EquipGen.ArenMoveComposite(anim, view, f, EquipGen.SteelLongsword, EquipGen.HeaterShield, null); }

    static void DrawHero(Bitmap tile, Bitmap hero, int dx, int dy, Func<Color, Color> fx, bool scanline)
    {
        for (int y = 0; y < 48; y++)
        {
            if (scanline && (y % 2 == 1)) continue;
            for (int x = 0; x < 48; x++)
            {
                var c = hero.GetPixel(x, y);
                if (c.A == 0) continue;
                int tx = 8 + dx + x, ty = 8 + dy + y;
                if (tx < 0 || ty < 0 || tx >= W || ty >= H) continue;
                tile.SetPixel(tx, ty, fx == null ? c : fx(c));
            }
        }
    }

    static void Blit(Bitmap tile, VfxConceptGen.Canvas cv)
    {
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) if (cv.Px[x, y].HasValue) tile.SetPixel(x, y, cv.Px[x, y].Value);
    }

    static Bitmap Grass()
    {
        var b = new Bitmap(W, H, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(b))
        {
            g.Clear(Color.FromArgb(255, 0x74, 0xA5, 0x87));
            using (var sh = new SolidBrush(Color.FromArgb(70, 0, 0, 0))) g.FillEllipse(sh, 26, 44, 12, 3);
        }
        return b;
    }

    // hexagon outline (pointy-top), ground-perspective squash on y
    static void Hex(VfxConceptGen.Canvas cv, Color c, double cx, double cy, double r, double yScale, int width)
    {
        var pts = new List<Point>();
        for (int k = 0; k < 6; k++)
        {
            double a = Math.PI / 6 + k * Math.PI / 3;
            pts.Add(new Point((int)Math.Round(cx + Math.Cos(a) * r), (int)Math.Round(cy + Math.Sin(a) * r * yScale)));
        }
        for (int k = 0; k < 6; k++) VfxConceptGen.Line(cv, c, pts[k].X, pts[k].Y, pts[(k + 1) % 6].X, pts[(k + 1) % 6].Y, width);
    }

    public static Bitmap Tile(int i)
    {
        var tile = Grass();
        var under = new VfxConceptGen.Canvas(); var over = new VfxConceptGen.Canvas();
        switch (i)
        {
            case 0: // finisher thrust: straight streak from the sword tip, gold spark at the end
            {
                DrawHero(tile, HeroAttack("Side", 1), 0, 0, null, false);
                VfxConceptGen.Line(over, C(Ice[3]), 44, 40, 78, 40, 5);
                VfxConceptGen.Line(over, C(Ice[2]), 44, 40, 79, 40, 3);
                VfxConceptGen.Line(over, C(Ice[0]), 46, 40, 80, 40, 1);
                for (int x = 48; x < 76; x += 7) { over.Set(x, 37, C(Ice[1])); over.Set(x + 3, 43, C(Ice[1])); }
                VfxConceptGen.Star(over, Gold, 82, 40, 4);
                VfxConceptGen.Dots(over, new[] { Gold[2], Gold[1] }, 41, 82, 40, 5, 4, 8);
                break;
            }
            case 1: // shield bash: gold cone shockwave + glint on the shield
            {
                DrawHero(tile, HeroAttack("Side", 1), 0, 0, null, false);
                for (int k = 0; k < 3; k++) VfxConceptGen.Crescent(over, Gold, 38, 40, 9 + k * 6, 32, -64, 2.6 - k * 0.5);
                VfxConceptGen.Star(over, Gold, 37, 39, 3);
                VfxConceptGen.Dots(over, new[] { Gold[1], Gold[2] }, 42, 58, 40, 6, 8, 10);
                break;
            }
            case 2: // charge: ice afterimages (2), speed lines, dust at the feet
            {
                var hero = HeroMove("Walk", "Side", 0);
                foreach (var d in new[] { new[] { 18, 46, 3 }, new[] { 12, 47, 2 }, new[] { 24, 47, 2 } })
                { VfxConceptGen.Disc(under, C("C8B8A0"), d[0], d[1], d[2]); VfxConceptGen.Disc(under, C("E8DCC8"), d[0] - 1, d[1] - 1, Math.Max(0.8, d[2] - 1.2)); }
                Blit(tile, under); under = new VfxConceptGen.Canvas();
                Func<Color, Color> far = c => C(Ice[2]), nearC = c => C(Ice[1]);
                DrawHero(tile, hero, -24, 0, far, true);
                DrawHero(tile, hero, -12, 0, nearC, true);
                DrawHero(tile, hero, 0, 0, null, false);
                foreach (var y in new[] { 26, 34, 42 }) { VfxConceptGen.Line(over, C(Ice[1]), 4, y, 14, y, 1); VfxConceptGen.Line(over, C(Ice[0]), 9, y, 14, y, 1); }
                break;
            }
            case 3: // whirl slash: near-full ellipse of crescents around the knight (top-down)
            {
                VfxConceptGen.Crescent(under, Ice, 32, 38, 20, 180, -170, 3.0, 0.5);
                Blit(tile, under); under = new VfxConceptGen.Canvas();
                DrawHero(tile, HeroAttack("Down", 1), 0, 0, null, false);
                VfxConceptGen.Crescent(over, Ice, 32, 38, 20, 10, -170, 3.4, 0.5);
                VfxConceptGen.Dots(over, new[] { Ice[0], Ice[1] }, 43, 32, 38, 20, 10, 10);
                break;
            }
            case 4: // bulwark: gold hexagonal barrier with honeycomb glints
            {
                Hex(under, C(Gold[3]), 32, 36, 17, 1.0, 1);
                Blit(tile, under); under = new VfxConceptGen.Canvas();
                DrawHero(tile, HeroMove("Idle", "Down", 0), 0, 0, null, false);
                Hex(over, C(Gold[2]), 32, 36, 17, 1.0, 1);
                Hex(over, C(Gold[1]), 32, 36, 15, 1.0, 1);
                foreach (var p in new[] { new[] { 24, 26 }, new[] { 40, 26 }, new[] { 20, 38 }, new[] { 44, 38 }, new[] { 26, 48 }, new[] { 38, 48 } })
                { over.Set(p[0], p[1], C(Gold[0])); over.Set(p[0] + 1, p[1], C(Gold[1])); over.Set(p[0], p[1] + 1, C(Gold[1])); }
                VfxConceptGen.Star(over, Gold, 32, 19, 3);
                break;
            }
            default: // dawnbreaker: large white-gold crescent with rays
            {
                DrawHero(tile, HeroAttack("Side", 1), 0, 0, null, false);
                VfxConceptGen.Crescent(over, Gold, 30, 38, 24, 22, -175, 5.5);
                VfxConceptGen.Crescent(over, Ice, 30, 38, 18, 22, -120, 2.0);
                foreach (var a in new[] { -20, 0, 20, 40 })
                {
                    double r = a * Math.PI / 180;
                    int x0 = 30 + (int)(Math.Cos(r) * 27), y0 = 38 + (int)(Math.Sin(r) * 27);
                    int x1 = 30 + (int)(Math.Cos(r) * 33), y1 = 38 + (int)(Math.Sin(r) * 33);
                    VfxConceptGen.Line(over, C(Gold[2]), x0, y0, x1, y1, 1);
                }
                VfxConceptGen.Dots(over, new[] { Gold[1], Gold[0] }, 44, 54, 34, 10, 14, 12);
                break;
            }
        }
        Blit(tile, under);
        // re-draw the hero over ground effects (whirl/bulwark already handled their order)
        Blit(tile, over);
        return tile;
    }
}
