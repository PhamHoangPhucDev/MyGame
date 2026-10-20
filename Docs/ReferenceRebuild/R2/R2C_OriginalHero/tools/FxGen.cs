using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

// Aren: slash VFX attached to the sword tip, Hurt (flash + flinch), Death (kneel, fall, lie with dropped gear).
// All canvases are 48x48 with the 32x32 body cell at (8,8), same as EquipGen.Compose.
public static class FxGen
{
    // ---------- slash crescent ----------
    public class Slash { public double Cx, Cy, R, TipAng, Sweep; }  // angles in degrees, screen y down

    static double Ang(double dx, double dy) { return Math.Atan2(dy, dx) * 180.0 / Math.PI; }

    // Crescent passes through the sword tip of the strike frame (F1). Trailing end = where the swing came from.
    public static Slash ArenSlash(string view)
    {
        var p = EquipGen.ArenAttackPoses(view)[1];
        var d = EquipGen.Dir[p.MainDir];
        int len = 2 + EquipGen.SteelLongsword.Length;
        double tx = p.MainX + d.X * len, ty = p.MainY + d.Y * len;
        double cx, cy, sweep;
        if (view == "Down") { cx = 16; cy = 24; sweep = 140; }      // from the left, under the knight, to the tip
        else if (view == "Up") { cx = 16; cy = 18; sweep = -110; }  // from the right shoulder up over the helm
        else { cx = 16; cy = 21; sweep = -150; }                     // from behind/above, over the top, to the front
        var s = new Slash { Cx = cx, Cy = cy };
        s.R = Math.Sqrt((tx - cx) * (tx - cx) + (ty - cy) * (ty - cy));
        s.TipAng = Ang(tx - cx, ty - cy);
        s.Sweep = sweep;
        return s;
    }

    static readonly Color Core = HeroConceptGen.Hex("FFFFFF"), Light = HeroConceptGen.Hex("D8F0FF"),
                          Mid = HeroConceptGen.Hex("8CC8F0"), Edge = HeroConceptGen.Hex("4A8AC8");

    // frame 0 = full crescent (on F1), frame 1 = thinner, front half, carried 20 deg past the tip (on F2).
    public static Dictionary<Point, Color> SlashPixels(Slash s, int frame)
    {
        var px = new Dictionary<Point, Color>();
        double from = frame == 0 ? 0.0 : 0.45, extra = frame == 0 ? 0.0 : 20.0;
        for (int y = -8; y < 40; y++)
            for (int x = -8; x < 40; x++)
            {
                double dx = x + 0.5 - s.Cx, dy = y + 0.5 - s.Cy, dist = Math.Sqrt(dx * dx + dy * dy);
                double a = Ang(dx, dy);
                // t: 0 at the trailing end, 1 at the tip (beyond 1 = carried past the tip on frame 1)
                double rel = a - (s.TipAng + s.Sweep);
                while (rel > 180) rel -= 360; while (rel < -180) rel += 360;
                double t = -rel / s.Sweep;
                double tMax = 1.0 + extra / Math.Abs(s.Sweep);
                if (t < from || t > tMax) continue;
                double tt = Math.Min(1.0, t);
                double th = frame == 0 ? 1.0 + 2.2 * tt : 1.0 + 0.8 * tt;
                if (dist > s.R + 0.5 || dist < s.R - th) continue;
                Color c;
                if (frame == 0) c = dist >= s.R - 0.5 ? (tt > 0.35 ? Core : Light) : (dist >= s.R - 1.5 ? Light : Mid);
                else c = dist >= s.R - 0.5 ? Light : Mid;
                if (t < from + 0.12) c = Edge;
                px[new Point(x, y)] = c;
            }
        return px;
    }

    // ---------- tints ----------
    public static Bitmap Tint(Bitmap src, Color to, double k)
    {
        var o = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < src.Height; y++) for (int x = 0; x < src.Width; x++)
            {
                var c = src.GetPixel(x, y);
                if (c.A == 0) continue;
                o.SetPixel(x, y, Color.FromArgb(255, (int)(c.R + (to.R - c.R) * k), (int)(c.G + (to.G - c.G) * k), (int)(c.B + (to.B - c.B) * k)));
            }
        return o;
    }

    // ---------- attack + vfx composite ----------
    public static Bitmap ArenAttackWithFx(string view, int f)
    {
        var heater = new Func<string, EquipGen.Spr>(EquipGen.HeaterShield);
        var bmp = EquipGen.ArenAttackComposite(view, f, EquipGen.SteelLongsword, heater, null);
        if (f == 0) return bmp;
        var px = SlashPixels(ArenSlash(view), f - 1);
        bool behind = view == "Up";   // the Up slash travels away from the camera
        var o = new Bitmap(48, 48, PixelFormat.Format32bppArgb);
        if (behind) foreach (var kv in px) Put(o, kv.Key, kv.Value);
        for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++) { var c = bmp.GetPixel(x, y); if (c.A > 0) o.SetPixel(x, y, c); }
        if (!behind) foreach (var kv in px) Put(o, kv.Key, kv.Value);
        return o;
    }

    static void Put(Bitmap b, Point p, Color c) { int x = 8 + p.X, y = 8 + p.Y; if (x >= 0 && y >= 0 && x < 48 && y < 48) b.SetPixel(x, y, c); }

    // ---------- hurt ----------
    // H0 = white flash of the idle pose (0.06 s); H1 = red tint + 1 px flinch (walk contact body, gear lowered) (0.12 s).
    public static Bitmap ArenHurt(string view, int f)
    {
        var heater = new Func<string, EquipGen.Spr>(EquipGen.HeaterShield);
        if (f == 0) return Tint(EquipGen.ArenMoveComposite("Idle", view, 0, EquipGen.SteelLongsword, heater, null), Color.White, 0.8);
        var c = HeroConceptGen.Roster.First(r => r.Id == "4");
        var pose = EquipGen.ArenMovePose("Walk", view, 0);
        var body = EquipGen.Body(ArenWalkGen.Frame(view, EquipGen.ArenBase(view), 0), c.Pal);
        // flinch: plume blown back (idle F3 plume row), legs from the walk contact frame
        var main = new EquipGen.Layer { S = EquipGen.Blade(EquipGen.SteelLongsword, pose.MainDir), X = pose.MainX, Y = pose.MainY };
        var hand = new EquipGen.Layer { S = EquipGen.Gauntlet, X = pose.MainX - 1, Y = pose.MainY - 1 };
        var off = new EquipGen.Layer { S = EquipGen.HeaterShield(view), X = pose.OffX, Y = pose.OffY };
        var behindL = new List<EquipGen.Layer>(); var front = new List<EquipGen.Layer>();
        if (pose.MainFront) { front.Add(main); front.Add(hand); } else { behindL.Add(main); behindL.Add(hand); }
        front.Add(off);
        return Tint(EquipGen.Compose(body, behindL, front), HeroConceptGen.Hex("FF3030"), 0.35);
    }

    // ---------- death ----------
    // Rotate the 14x21 body grid 90 deg counter-clockwise (lossless for pixel art): head to the left, 21x14.
    public static char[,] Lying(string[] rows)
    {
        var g = new char[21, 14];
        for (int y = 0; y < 21; y++) for (int x = 0; x < 14; x++) g[y, 13 - x] = rows[y][x];
        return g;
    }

    public static Bitmap ArenDeath(int f)
    {
        var c = HeroConceptGen.Roster.First(r => r.Id == "4");
        var heater = new Func<string, EquipGen.Spr>(EquipGen.HeaterShield);
        if (f == 0)
        {
            // kneel: whole body 2 px lower (helmet + torso), legs folded into 1 row; sword tip to the ground
            var b = EquipGen.Grid(c.Rows);
            var g = new char[14, 21];
            for (int y = 0; y < 21; y++) for (int x = 0; x < 14; x++) g[x, y] = '.';
            for (int y = 0; y <= 17; y++) for (int x = 0; x < 14; x++) g[x, y + 2] = b[x, y];
            string knee = "..kAAAkkAAAk..";
            for (int x = 0; x < 14; x++) g[x, 20] = knee[x];
            var body = EquipGen.Body(g, c.Pal);
            var main = new EquipGen.Layer { S = EquipGen.Blade(EquipGen.SteelLongsword, 6), X = 10, Y = 25 };
            var hand = new EquipGen.Layer { S = EquipGen.Gauntlet, X = 9, Y = 24 };
            var off = new EquipGen.Layer { S = EquipGen.HeaterShield("Down"), X = 21, Y = 26 };
            return EquipGen.Compose(body, new List<EquipGen.Layer>(), new List<EquipGen.Layer> { main, hand, off });
        }
        var lying = Lying(c.Rows);
        var cells = new Color?[32, 32];
        int ox = 5, oy = 16;   // 21 wide at x 5..25, 14 tall at y 16..29 (sole row stays the ground line)
        for (int y = 0; y < 14; y++) for (int x = 0; x < 21; x++)
                if (lying[x, y] != '.') cells[ox + x, oy + y] = HeroConceptGen.Hex(c.Pal[lying[x, y]]);
        var frontL = new List<EquipGen.Layer>();
        var behindL = new List<EquipGen.Layer>();
        if (f == 2)
        {
            // dropped gear on the ground: sword flat in front, shield face-up beside the feet
            behindL.Add(new EquipGen.Layer { S = EquipGen.Blade(EquipGen.SteelLongsword, 0), X = 7, Y = 31 });
            frontL.Add(new EquipGen.Layer { S = EquipGen.HeaterShield("Down"), X = 28, Y = 27 });
        }
        return EquipGen.Compose(cells, behindL, frontL);
    }

    // ---------- exports ----------
    public static string Run(string outDir)
    {
        var log = new StringBuilder();
        foreach (var v in new[] { "Down", "Up", "Side" })
        {
            var s = ArenSlash(v);
            var sheet = new Bitmap(96, 48, PixelFormat.Format32bppArgb);
            for (int fr = 0; fr < 2; fr++)
                foreach (var kv in SlashPixels(s, fr))
                {
                    int x = fr * 48 + 8 + kv.Key.X, y = 8 + kv.Key.Y;
                    if (x >= fr * 48 && x < fr * 48 + 48 && y >= 0 && y < 48) sheet.SetPixel(x, y, kv.Value);
                }
            sheet.Save(System.IO.Path.Combine(outDir, "Aren_SlashVFX_" + v + "_v1.png"), ImageFormat.Png); sheet.Dispose();
            log.AppendFormat("{0}: centre ({1},{2}) R {3:F1} tip {4:F0} deg sweep {5}\n", v, s.Cx, s.Cy, s.R, s.TipAng, s.Sweep);
        }
        return log.ToString();
    }
}
