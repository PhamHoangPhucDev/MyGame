using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

// Lyra skill body frames. Same template / anchors as the approved Lyra set (LyraKit): bow = sheet index
// state*8+dir (24 = back tip), hand = bow fist, off (off_view Hand) = draw fist; shooting arms baked into the body.
//  FanShot 2f (full draw, release sweep) · LeapBack 2f (crouch, airborne) · ArrowRain 2f (aim at the sky, release)
//  ForestPierce 2f (kneeling deep draw, release) · WindStep = Walk (no new frames)
public static class LyraSkillGen
{
    const int W = 14, H = 21;
    static readonly int[] MirrorDir = { 4, 3, 2, 1, 0, 7, 6, 5 };
    static readonly string[] Views = { "Down", "Up", "Side" };

    public class Frame
    {
        public string View; public bool Mirror; public char[,] Grid; public int Drop, Lift;
        public LyraGen.P Shot;      // null = bow on the back (no arms)
        public string Note;
    }

    static void Row(char[,] g, int y, string s) { for (int x = 0; x < W; x++) g[x, y] = s[x]; }

    public static char[,] Crouch(string v)
    {
        var b = LyraGen.Base(v); var g = EquipGen.Grid(b);
        for (int y = 18; y >= 1; y--) for (int x = 0; x < W; x++) g[x, y] = b[y - 1][x];
        Row(g, 0, "..............");
        if (v == "Down") { Row(g, 19, "...kBbk..kBbk."); Row(g, 20, "..kBBbk..kBBbk"); }
        else if (v == "Up") { Row(g, 19, "..kbBk..kbBk.."); Row(g, 20, ".kbBBk..kbBBk."); }
        else { Row(g, 19, "..kBbk..kBbk.."); Row(g, 20, ".kBBbk..kBBbk."); }
        return g;
    }

    // airborne: legs tucked (boots pulled up, no sole row); painted 2 px higher
    public static char[,] Tucked(string v)
    {
        var g = EquipGen.Grid(LyraGen.Base(v));
        Row(g, 18, v == "Up" ? "...kbBkbBk...." : "....kBbkBbk...");
        Row(g, 19, "..............");
        Row(g, 20, "..............");
        return g;
    }

    static LyraGen.P Copy(LyraGen.P s, int dx, int dy, int dir, int draw, bool arrow, int handDraw, int shoulderDy)
    {
        return new LyraGen.P
        {
            X = s.X + dx, Y = s.Y + dy, Dir = dir, Draw = draw, Arrow = arrow, Front = s.Front, Shot = true, HandDraw = handDraw,
            BowShoulder = new Point(s.BowShoulder.X, s.BowShoulder.Y + shoulderDy), DrawShoulder = new Point(s.DrawShoulder.X, s.DrawShoulder.Y + shoulderDy),
            BowArmFront = s.BowArmFront, DrawArmFront = s.DrawArmFront,
        };
    }

    static char[,] Base(string v) { return EquipGen.Grid(LyraGen.Base(v)); }

    public static Frame[] FanShot(string v)
    {
        var full = LyraGen.Shot(v, 1);
        int sweep = v == "Down" ? 5 : v == "Up" ? 1 : 1;   // bow swept one step across the fan
        int sdy = v == "Side" ? -2 : 0, sdx = v == "Down" ? -1 : 0;
        return new[]
        {
            new Frame { View = v, Grid = Base(v), Shot = full, Note = "keo cang day" },
            new Frame { View = v, Grid = Base(v), Shot = Copy(full, sdx, sdy, sweep, 0, false, 4, 0), Note = "buong, vung cung quet quat" },
        };
    }

    public static Frame[] LeapBack(string v)
    {
        return new[]
        {
            new Frame { View = v, Grid = Crouch(v), Drop = 1, Note = "khuyu goi lay da" },
            new Frame { View = v, Grid = Tucked(v), Lift = 2, Note = "bat len (tren khong), co chan" },
        };
    }

    public static Frame[] ArrowRain(string v)
    {
        var s = LyraGen.Shot(v, 1);
        LyraGen.P aim;
        if (v == "Down") aim = new LyraGen.P { X = 16, Y = 8, Dir = 2, Draw = 4, Arrow = true, Shot = true, HandDraw = 4, BowShoulder = new Point(19, 21), DrawShoulder = new Point(12, 21) };
        else if (v == "Up") aim = new LyraGen.P { X = 16, Y = 5, Dir = 2, Draw = 4, Arrow = true, Front = false, Shot = true, HandDraw = 4, BowShoulder = new Point(12, 21), DrawShoulder = new Point(20, 21), BowArmFront = false, DrawArmFront = false };
        else aim = new LyraGen.P { X = 22, Y = 8, Dir = 1, Draw = 4, Arrow = true, Shot = true, HandDraw = 4, BowShoulder = new Point(19, 21), DrawShoulder = new Point(12, 22) };
        return new[]
        {
            new Frame { View = v, Grid = Base(v), Shot = aim, Note = "giuong cung len troi, keo day" },
            new Frame { View = v, Grid = Base(v), Shot = Copy(aim, 0, 0, aim.Dir, 0, false, 4, 0), Note = "buong mua ten" },
        };
    }

    public static Frame[] ForestPierce(string v)
    {
        var full = LyraGen.Shot(v, 1); var rel = LyraGen.Shot(v, 2);
        return new[]
        {
            new Frame { View = v, Grid = Crouch(v), Drop = 1, Shot = Copy(full, 0, 1, full.Dir, 4, true, 4, 1), Note = "quy thap, keo day that sau" },
            new Frame { View = v, Grid = Crouch(v), Drop = 1, Shot = Copy(rel, v == "Side" ? -1 : 0, 1, rel.Dir, 0, false, 4, 1), Note = "buong tia xuyen, giat lui" },
        };
    }

    // ---------- body (+ baked arms) and gear ----------
    public static Color?[,] Body32(Frame fr)
    {
        var g = (char[,])fr.Grid.Clone();
        if (fr.Shot != null)
        {
            int a = 3, b = fr.View == "Up" ? 11 : 10;
            for (int r = 12 + fr.Drop; r <= 14 + fr.Drop; r++) { g[a, r] = g[a + 1, r]; g[b, r] = g[b - 1, r]; }
        }
        var c = EquipGen.Body(g, LyraGen.Pal);
        if (fr.Lift > 0)
        {
            var o = new Color?[32, 32];
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) if (c[x, y].HasValue && y - fr.Lift >= 1) o[x, y - fr.Lift] = c[x, y];
            c = o;
        }
        if (fr.Shot != null)
        {
            var p = fr.Shot; var m = LyraGen.StringMid(p.Dir, p.HandDraw);
            var arms = new[] { Tuple.Create(LyraGen.Arm(p.BowShoulder, new Point(p.X, p.Y)), p.BowArmFront), Tuple.Create(LyraGen.Arm(p.DrawShoulder, new Point(p.X + m.X, p.Y + m.Y)), p.DrawArmFront) };
            foreach (var arm in arms)
                foreach (var kv in arm.Item1.Px)
                {
                    int x = kv.Key.X, y = kv.Key.Y;
                    if (x < 1 || y < 1 || x > 30 || y > 30) continue;
                    if (arm.Item2 || !c[x, y].HasValue) c[x, y] = kv.Value;
                }
        }
        return c;
    }

    public static LyraKit.P Gear(Frame fr)
    {
        if (fr.Shot == null) return LyraKit.Rest(fr.View, fr.Drop - fr.Lift);
        var s = fr.Shot; int state = s.Arrow ? (s.Draw >= 4 ? 2 : 1) : 0;
        var m = LyraGen.StringMid(s.Dir, s.HandDraw);
        return new LyraKit.P { MX = s.X, MY = s.Y, MI = state * 8 + s.Dir, MO = s.Front ? 2 : -2, Hand = true, OX = s.X + m.X, OY = s.Y + m.Y, OO = s.DrawArmFront ? 3 : -3 };
    }

    public static Bitmap Compose(Frame fr)
    {
        var body = Body32(fr); var p = Gear(fr);
        var behind = new List<EquipGen.Layer>(); var front = new List<EquipGen.Layer>();
        (p.MO > 0 ? front : behind).Add(new EquipGen.Layer { S = LyraKit.BowSprite(p.MI), X = p.MX, Y = p.MY });
        if (p.Hand) (p.MO > 0 ? front : behind).Add(new EquipGen.Layer { S = LyraGen.Hand, X = p.MX - 1, Y = p.MY - 1 });
        if (p.OX > -99) (p.OO > 0 ? front : behind).Add(new EquipGen.Layer { S = LyraGen.Hand, X = p.OX - 1, Y = p.OY - 1 });
        var bmp = EquipGen.Compose(body, behind, front);
        if (!fr.Mirror) return bmp;
        var o = new Bitmap(48, 48, PixelFormat.Format32bppArgb);
        for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++) o.SetPixel(47 - x, y, bmp.GetPixel(x, y));
        return o;
    }

    // ================= export (after approval) =================
    public class SheetRow { public string Skill, View; public Frame[] Frames; }
    public static List<SheetRow> Rows()
    {
        var rows = new List<SheetRow>();
        foreach (var skill in new[] { "FanShot", "LeapBack", "ArrowRain", "ForestPierce" })
            foreach (var v in Views)
            {
                Frame[] fs = skill == "FanShot" ? FanShot(v) : skill == "LeapBack" ? LeapBack(v) : skill == "ArrowRain" ? ArrowRain(v) : ForestPierce(v);
                if (v == "Side") foreach (var f in fs) f.Mirror = true;
                rows.Add(new SheetRow { Skill = skill, View = v, Frames = fs });
            }
        return rows;
    }

    static string Csv(string key, Frame fr)
    {
        var p = Gear(fr); bool m = fr.Mirror;
        int mx = p.MX, mi = p.MI, hx = -99, hy = 0, ox = -99, oy = 0;
        if (m && mx > -99) { mx = 31 - mx; if (mi < 24) mi = (mi / 8) * 8 + MirrorDir[mi % 8]; }
        if (p.Hand && p.MX > -99) { hx = m ? 31 - p.MX : p.MX - 1; hy = p.MY - 1; }
        if (p.OX > -99) { ox = m ? 31 - p.OX : p.OX - 1; oy = p.OY - 1; }
        return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},-1,0,-1\n", key, mx, p.MY, mi, p.MO, hx, hy, ox, oy, p.OX > -99 ? "Hand" : fr.View, p.OO);
    }

    public static string Export(string outDir)
    {
        System.IO.Directory.CreateDirectory(outDir);
        var rows = Rows();
        var sheet = new Bitmap(128, 32 * rows.Count, PixelFormat.Format32bppArgb);
        var csv = new System.Text.StringBuilder("key,main_x,main_y,main_dir,main_order,hand_x,hand_y,off_x,off_y,off_view,off_order,slash,slash_order,off_dir\n");
        var layout = new System.Text.StringBuilder("row,skill,view,frames,keys\n");
        for (int r = 0; r < rows.Count; r++)
        {
            var row = rows[r]; var keys = new List<string>();
            for (int f = 0; f < row.Frames.Length; f++)
            {
                var fr = row.Frames[f]; var body = Body32(fr);
                for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                        if (body[x, y].HasValue) sheet.SetPixel(f * 32 + (fr.Mirror ? 31 - x : x), r * 32 + y, body[x, y].Value);
                string key = "Skill_" + row.Skill + "_" + row.View + "_" + f;
                csv.Append(Csv(key, fr)); keys.Add(key);
            }
            layout.AppendFormat("{0},{1},{2},{3},{4}\n", r, row.Skill, row.View, row.Frames.Length, string.Join(" ", keys));
        }
        sheet.Save(System.IO.Path.Combine(outDir, "Lyra_SkillSheet.png"), ImageFormat.Png); sheet.Dispose();
        System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "Lyra_SkillAnchors.csv"), csv.ToString(), new System.Text.UTF8Encoding(false));
        System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "LYRA_SKILL_LAYOUT.csv"), layout.ToString(), new System.Text.UTF8Encoding(false));
        return "rows " + rows.Count;
    }
}
