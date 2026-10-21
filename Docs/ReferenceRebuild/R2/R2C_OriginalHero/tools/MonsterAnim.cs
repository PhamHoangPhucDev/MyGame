using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

// Monster animation sets + Unity export (baked: the weapon is part of each frame, no anchors needed).
// Goblin 48x48 (sole row 37); Gobrider / Elite 48x48 (sole 41); Warlord 64x64 (sole 57).
// Sheet: 4 cols x 15 rows = Idle D/U/S, Walk D/U/S, Attack D/U/S, Hurt D/U/S, Death D/U/S. Side rows face LEFT.
public static class MonsterAnim
{
    public class Mon { public string Name, Kind; public int S, Sole; }
    public static readonly Mon[] All =
    {
        new Mon { Name = "Goblin", Kind = "Goblin", S = 48, Sole = 37 },
        new Mon { Name = "GoblinArcher", Kind = "Archer", S = 48, Sole = 37 },
        new Mon { Name = "Satyr", Kind = "Satyr", S = 48, Sole = 37 },
        new Mon { Name = "Dryad", Kind = "Dryad", S = 48, Sole = 37 },
        new Mon { Name = "DryadRider", Kind = "Stag", S = 48, Sole = 41 },
        new Mon { Name = "Gobrider", Kind = "Gobrider", S = 48, Sole = 41 },
        new Mon { Name = "GobriderElite", Kind = "Elite", S = 48, Sole = 41 },
        new Mon { Name = "GobriderWarlord", Kind = "Warlord", S = 64, Sole = 57 },
    };
    public static readonly string[] Anims = { "Idle", "Walk", "Attack", "Hurt", "Death" };
    public static readonly int[] Frames = { 4, 4, 3, 2, 4 };
    public static readonly int[] Fps = { 6, 8, 10, 10, 8 };
    public static readonly string[] Views = { "Down", "Up", "Side" };

    // ---------- pixel helpers ----------
    static Bitmap Blank(int s) { return new Bitmap(s, s, PixelFormat.Format32bppArgb); }

    static Bitmap Shift(Bitmap b, int dx, int dy)
    {
        var o = Blank(b.Width);
        for (int y = 0; y < b.Height; y++) for (int x = 0; x < b.Width; x++)
            {
                var c = b.GetPixel(x, y); if (c.A == 0) continue;
                int X = x + dx, Y = y + dy; if (X >= 0 && Y >= 0 && X < o.Width && Y < o.Height) o.SetPixel(X, Y, c);
            }
        return o;
    }

    // rows above `split` move down dy and are drawn over the rows below (squash / kneel); feet stay planted
    static Bitmap ShiftAbove(Bitmap b, int split, int dy)
    {
        var o = Blank(b.Width);
        for (int y = split; y < b.Height; y++) for (int x = 0; x < b.Width; x++) { var c = b.GetPixel(x, y); if (c.A > 0) o.SetPixel(x, y, c); }
        for (int y = 0; y < split; y++) for (int x = 0; x < b.Width; x++) { var c = b.GetPixel(x, y); if (c.A > 0 && y + dy < o.Height) o.SetPixel(x, y + dy, c); }
        return o;
    }

    static Bitmap Tint(Bitmap b, Color t, double a)
    {
        var o = Blank(b.Width);
        for (int y = 0; y < b.Height; y++) for (int x = 0; x < b.Width; x++)
            {
                var c = b.GetPixel(x, y); if (c.A == 0) continue;
                o.SetPixel(x, y, Color.FromArgb(c.A, (int)(c.R + (t.R - c.R) * a), (int)(c.G + (t.G - c.G) * a), (int)(c.B + (t.B - c.B) * a)));
            }
        return o;
    }

    static Bitmap Fade(Bitmap b, int alpha)
    {
        var o = Blank(b.Width);
        for (int y = 0; y < b.Height; y++) for (int x = 0; x < b.Width; x++) { var c = b.GetPixel(x, y); if (c.A > 0) o.SetPixel(x, y, Color.FromArgb(alpha, c)); }
        return o;
    }

    // rotate the content 90 deg clockwise (falls over), bottom on the sole row, centred
    static Bitmap LieDown(Bitmap b, int sole)
    {
        int x0 = b.Width, y0 = b.Height, x1 = -1, y1 = -1;
        for (int y = 0; y < b.Height; y++) for (int x = 0; x < b.Width; x++)
                if (b.GetPixel(x, y).A > 0) { x0 = Math.Min(x0, x); x1 = Math.Max(x1, x); y0 = Math.Min(y0, y); y1 = Math.Max(y1, y); }
        int w = y1 - y0 + 1, h = x1 - x0 + 1;
        int ox = b.Width / 2 - w / 2, oy = sole + 1 - h;
        var o = Blank(b.Width);
        for (int y = y0; y <= y1; y++) for (int x = x0; x <= x1; x++)
            {
                var c = b.GetPixel(x, y); if (c.A == 0) continue;
                int X = ox + (y1 - y), Y = oy + (x - x0);
                if (X >= 1 && Y >= 1 && X < o.Width - 1 && Y < o.Height - 1) o.SetPixel(X, Y, c);
            }
        return o;
    }

    // facing direction in the exported sheet (Side faces left)
    static Point Fwd(string v) { return v == "Down" ? new Point(0, 1) : v == "Up" ? new Point(0, -1) : new Point(-1, 0); }

    // base pose (Side already mirrored to face left)
    static Bitmap Pose(Mon m, string anim, string v, int f)
    {
        bool side = v == "Side";
        if (m.Kind == "Goblin")
        {
            bool moving = anim == "Walk" || anim == "Attack";
            return MonsterGen.Goblin(moving ? anim : "Idle", v, moving ? f : 0, side);
        }
        if (m.Kind == "Archer")
        {
            // walk: contact frames 0/2 drop 1 px with alternate feet; attack: aim, loose (arrow spawns), lower the bow
            if (anim == "Walk") return ArcherGen.Compose(v, 0, side, 0, f == 0 ? 1 : f == 2 ? 2 : 0);
            if (anim == "Attack") return ArcherGen.Compose(v, f == 0 ? 1 : f == 1 ? 2 : 0, side);
            return ArcherGen.Compose(v, 0, side);
        }
        if (m.Kind == "Satyr" || m.Kind == "Dryad")
        {
            // walk as the archer; Dryad attack: gather orb, cast (big orb, bolt spawns), lower hands
            if (anim == "Walk") return CreatureGen.Compose(m.Kind, v, 0, side, f == 0 ? 1 : f == 2 ? 2 : 0);
            if (anim == "Attack" && m.Kind == "Dryad") return CreatureGen.Compose(m.Kind, v, f == 0 ? 1 : f == 1 ? 2 : 0, side);
            return CreatureGen.Compose(m.Kind, v, 0, side);
        }
        if (m.Kind == "Stag")
        {
            // walk: alternate leg pairs with a torso bob on frames 1/3; attack: gather orb, cast (bolt spawns), lower
            if (anim == "Walk") return CreatureGen.Stag(v, 0, side, f == 0 ? 0 : f == 2 ? 1 : -1, f % 2);
            if (anim == "Attack") return CreatureGen.Stag(v, f == 0 ? 1 : f == 1 ? 2 : 0, side);
            return CreatureGen.Stag(v, 0, side);
        }
        if (anim == "Walk") return RiderGen.Compose(m.Kind, v, side, f % 2, f == 0 ? 0 : f == 2 ? 1 : -1, 0);
        if (anim == "Attack") return RiderGen.Compose(m.Kind, v, side, f == 0 ? -1 : 0, -1, f == 0 ? 1 : f == 1 ? 2 : 0);
        return RiderGen.Compose(m.Kind, v, side, 0, -1, 0);
    }

    static readonly Color Flash = Color.White, HurtRed = Color.FromArgb(255, 220, 40, 40), Dusk = Color.FromArgb(255, 40, 30, 40);

    public static Bitmap Frame(Mon m, string anim, string v, int f)
    {
        var fw = Fwd(v); bool gob = m.Kind != "Gobrider" && m.Kind != "Elite" && m.Kind != "Warlord" && m.Kind != "Stag";   // on foot (not mounted / four-legged)
        bool archer = m.Kind == "Archer";
        switch (anim)
        {
            case "Idle":
                {
                    // breathe: frames 1-2 sink 1 px (goblin squashes above the feet, rider bobs on the boar)
                    if (f == 0 || f == 3) return Pose(m, "Idle", v, 0);
                    if (m.Kind == "Stag") return CreatureGen.Stag(v, 0, v == "Side", -1, 1);
                    return gob ? ShiftAbove(Pose(m, "Idle", v, 0), m.Sole - 1, 1) : RiderGen.Compose(m.Kind, v, v == "Side", 1, -1, 0);
                }
            case "Walk": return Pose(m, "Walk", v, f);
            case "Attack":
                {
                    // goblin: windup, stab (+1 px lunge), recover. riders: rear back 1 px, charge 2 px + club strike, recover
                    var b = Pose(m, "Attack", v, f);
                    if (archer) return f == 1 ? Shift(b, -fw.X, -fw.Y) : b;   // recoil 1 px on release
                    if (m.Kind == "Dryad" || m.Kind == "Stag") return b;       // casts in place
                    if (m.Kind == "Satyr")                                     // crouch back, head-butt lunge 2 px, recover
                        return f == 0 ? Shift(ShiftAbove(b, m.Sole - 1, 1), -fw.X, -fw.Y) : f == 1 ? Shift(b, fw.X * 2, fw.Y * 2) : b;
                    if (gob) return f == 1 ? Shift(b, fw.X, fw.Y) : b;
                    return f == 0 ? Shift(b, -fw.X, -fw.Y) : f == 1 ? Shift(b, fw.X * 2, fw.Y * 2) : b;
                }
            case "Hurt":
                {
                    // knocked back 1 px: white flash, then a light red tint
                    var b = Shift(Pose(m, "Idle", v, 0), -fw.X, -fw.Y);
                    return f == 0 ? Tint(b, Flash, 0.6) : Tint(b, HurtRed, 0.2);
                }
            default:
                {
                    // death: flash, slump 2 px, collapse (goblin falls over 90 deg; the boar drops onto its belly with
                    // the rider slumped on it), fade to half alpha (hold last)
                    var b = Shift(Pose(m, "Idle", v, 0), -fw.X, -fw.Y);
                    if (f == 0) return Tint(b, Flash, 0.6);
                    if (f == 1) return Tint(ShiftAbove(b, m.Sole - 1, 2), Dusk, 0.15);
                    var lie = Tint(gob ? LieDown(b, m.Sole) : ShiftAbove(ShiftAbove(b, m.Sole - 1, 3), m.Sole - 12, 1), Dusk, 0.25);
                    return f == 2 ? lie : Fade(lie, 128);
                }
        }
    }

    public static Bitmap Sheet(Mon m)
    {
        var sh = new Bitmap(m.S * 4, m.S * 15, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(sh))
            for (int a = 0; a < Anims.Length; a++) for (int vi = 0; vi < 3; vi++) for (int f = 0; f < Frames[a]; f++)
                        g.DrawImageUnscaled(Frame(m, Anims[a], Views[vi], f), f * m.S, (a * 3 + vi) * m.S);
        return sh;
    }

    static void Projectile(string root, StringBuilder man, StringBuilder fr, string mon, string name, string what, Func<int, Bitmap> draw)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        var file = mon + "/" + mon + "_" + name + ".png";
        using (var ar = new Bitmap(16 * 8, 16, PixelFormat.Format32bppArgb))
        {
            using (var g = Graphics.FromImage(ar)) for (int d = 0; d < 8; d++) g.DrawImageUnscaled(draw(d), d * 16, 0);
            ar.Save(Path.Combine(root, file), ImageFormat.Png);
        }
        man.Append(file + ",16,16,8,1,8,8,0.5,0.5," + what + "; cols R UR U UL L DL D DR (tip points that way); pick the col from the flight direction\n");
        var dirs = new[] { "R", "UR", "U", "UL", "L", "DL", "D", "DR" };
        for (int d = 0; d < 8; d++) fr.AppendFormat(inv, "{4}_{5}_{0},{1},{2},0,{3},0,16,16,{5},{0},0,1,0,static\n", dirs[d], file, d, d * 16, mon, name);
    }

    // ---------- Boss: Cyclops (96x96, sole 89, 2x size at hero pixel density) ----------
    // Rows: IdleCalm, WalkCalm (before it is hit), IdleAngry, WalkAngry, Attack, Hurt, Death - each Down/Up/Side.
    public static readonly string[] BossAnims = { "IdleCalm", "WalkCalm", "IdleAngry", "WalkAngry", "Attack", "Hurt", "Death" };
    public static readonly int[] BossFrames = { 4, 4, 4, 4, 4, 2, 4 };
    public static readonly int[] BossFps = { 5, 6, 6, 7, 8, 10, 6 };

    public static Bitmap BossFrame(string anim, string v, int f)
    {
        bool side = v == "Side"; var fw = Fwd(v); int S = BossGen.BigSole;
        bool angry = anim != "IdleCalm" && anim != "WalkCalm";
        switch (anim)
        {
            case "IdleCalm": case "IdleAngry":
                return BossGen.CyclopsBig(v, 0, side, f == 1 || f == 2 ? 2 : 0, angry);
            case "WalkCalm": case "WalkAngry":
                return BossGen.CyclopsBig(v, 0, side, f % 2 == 1 ? 2 : 0, angry, f == 0 ? 0 : f == 2 ? 1 : -1);
            case "Attack":
                // windup, rise (club high), SLAM (hit frame 2, squat + 2 px lunge), recover
                if (f == 0) return BossGen.CyclopsBig(v, 1, side, 0, true);
                if (f == 1) return BossGen.CyclopsBig(v, 1, side, -2, true);
                if (f == 2) return Shift(BossGen.CyclopsBig(v, 2, side, 2, true), fw.X * 2, fw.Y * 2);
                return BossGen.CyclopsBig(v, 0, side, 0, true);
            case "Hurt":
                {
                    var b = Shift(BossGen.CyclopsBig(v, 0, side, 0, true), -fw.X * 2, -fw.Y * 2);
                    return f == 0 ? Tint(b, Flash, 0.6) : Tint(b, HurtRed, 0.2);
                }
            default:
                {
                    var b = Shift(BossGen.CyclopsBig(v, 0, side, 0, true), -fw.X * 2, -fw.Y * 2);
                    if (f == 0) return Tint(b, Flash, 0.6);
                    if (f == 1) return Tint(ShiftAbove(b, S - 1, 4), Dusk, 0.15);
                    var lie = Tint(LieDown(b, S), Dusk, 0.25);
                    return f == 2 ? lie : Fade(lie, 128);
                }
        }
    }

    static void ExportBoss(string root, StringBuilder man, StringBuilder fr)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        const string mon = "Cyclops"; int C = BossGen.BigCell, rowsN = BossAnims.Length * 3;
        Directory.CreateDirectory(Path.Combine(root, mon));
        var file = mon + "/" + mon + "_Sheet.png";
        using (var sh = new Bitmap(C * 4, C * rowsN, PixelFormat.Format32bppArgb))
        {
            using (var g = Graphics.FromImage(sh))
                for (int a = 0; a < BossAnims.Length; a++) for (int vi = 0; vi < 3; vi++) for (int f = 0; f < BossFrames[a]; f++)
                            g.DrawImageUnscaled(BossFrame(BossAnims[a], Views[vi], f), f * C, (a * 3 + vi) * C);
            sh.Save(Path.Combine(root, file), ImageFormat.Png);
        }
        double py = (C - (BossGen.BigSole + 1)) / (double)C;
        man.AppendFormat(inv, "{0},{1},{1},4,{2},{3},{4},0.5,{5},BOSS rows IdleCalm D/U/S - WalkCalm - IdleAngry - WalkAngry - Attack - Hurt - Death; Side faces left; club baked\n",
            file, C, rowsN, C / 2, BossGen.BigSole + 1, py.ToString("0.####", inv));
        for (int a = 0; a < BossAnims.Length; a++) for (int vi = 0; vi < 3; vi++) for (int f = 0; f < BossFrames[a]; f++)
                {
                    int row = a * 3 + vi; string mode = a < 4 ? "loop" : a == 6 ? "hold_last" : "once";
                    fr.AppendFormat(inv, "{0}_{1}_{2}_{3},{4},{3},{5},{6},{7},{8},{8},{1},{2},{3},{9},{10},{11}\n",
                        mon, BossAnims[a], Views[vi], f, file, row, f * C, row * C, C, BossFrames[a], BossFps[a], mode);
                }
        // AoE telegraph: 6 frames 128x128 (ground layer), pivot centre
        var tfile = mon + "/" + mon + "_Telegraph.png";
        using (var t = new Bitmap(128 * 6, 128, PixelFormat.Format32bppArgb))
        {
            using (var g = Graphics.FromImage(t)) for (int f = 0; f < 6; f++) g.DrawImageUnscaled(BossGen.Telegraph(f), f * 128, 0);
            t.Save(Path.Combine(root, tfile), ImageFormat.Png);
        }
        man.Append(tfile + ",128,128,6,1,64,64,0.5,0.5,AoE warning ring (radius 62 px); frames 0-3 fill grows during windup; 4 impact flash; 5 fade; ground layer under units\n");
        for (int f = 0; f < 6; f++) fr.AppendFormat(inv, "{0}_Telegraph_{1},{2},{1},0,{3},0,128,128,Telegraph,Ground,{1},6,8,once\n", mon, f, tfile, f * 128);
        var cfile = mon + "/" + mon + "_Crack.png";
        using (var ck = BossGen.Crack()) ck.Save(Path.Combine(root, cfile), ImageFormat.Png);
        man.Append(cfile + ",96,48,1,1,48,24,0.5,0.5,ground crack decal left by the slam; ground layer; fade out in code\n");
        fr.AppendFormat(inv, "{0}_Crack,{1},0,0,0,0,96,48,Crack,Ground,0,1,0,static\n", mon, cfile);
    }

    public static void Export(string root)
    {
        var inv = System.Globalization.CultureInfo.InvariantCulture;
        Directory.CreateDirectory(root);
        var man = new StringBuilder("file,cell_w,cell_h,cols,rows,pivot_px_x,pivot_px_y_from_top,unity_pivot_x,unity_pivot_y,layout\n");
        var fr = new StringBuilder("sprite,file,col,row,x,y,w,h,anim,view,frame,frames,fps,mode\n");
        foreach (var m in All)
        {
            Directory.CreateDirectory(Path.Combine(root, m.Name));
            var file = m.Name + "/" + m.Name + "_Sheet.png";
            using (var sh = Sheet(m)) sh.Save(Path.Combine(root, file), ImageFormat.Png);
            double py = (m.S - (m.Sole + 1)) / (double)m.S;
            man.AppendFormat(inv, "{0},{1},{1},4,15,{2},{3},0.5,{4},rows Idle D/U/S - Walk D/U/S - Attack D/U/S - Hurt D/U/S - Death D/U/S; Side faces left; weapon baked\n",
                file, m.S, m.S / 2, m.Sole + 1, py.ToString("0.####", inv));
            for (int a = 0; a < Anims.Length; a++) for (int vi = 0; vi < 3; vi++) for (int f = 0; f < Frames[a]; f++)
                    {
                        int row = a * 3 + vi;
                        fr.AppendFormat(inv, "{0}_{1}_{2}_{3},{4},{3},{5},{6},{7},{8},{8},{1},{2},{3},{9},{10},{11}\n",
                            m.Name, Anims[a], Views[vi], f, file, row, f * m.S, row * m.S, m.S, Frames[a], Fps[a], a < 2 ? "loop" : a == 4 ? "hold_last" : "once");
                    }
        }
        // projectiles: 8 drawn directions, 16x16, pivot = cell centre (no runtime rotation)
        Projectile(root, man, fr, "GoblinArcher", "Arrow", "arrow projectile", ArcherGen.Arrow);
        Projectile(root, man, fr, "Dryad", "LeafBolt", "leaf bolt projectile", CreatureGen.LeafBolt);
        ExportBoss(root, man, fr);
        File.WriteAllText(Path.Combine(root, "MONSTERS_MANIFEST.csv"), man.ToString());
        File.WriteAllText(Path.Combine(root, "MONSTERS_FRAMES.csv"), fr.ToString());
    }
}
