using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

// Brom skill body frames. Same 14x21 template / anchors as the approved Brom set (BromKit); skills get their own
// body variants + hammer (8 drawn directions) / round shield poses.
//  GroundSlam 3f (raise overhead, slam, hold impact) · ShieldCharge 2f loop · TauntRoar 2f (inhale, roar)
//  IronWall 1f hold (braced, shield planted) · Earthquake = GroundSlam F0->F1 looped x3 (no new frames)
public static class BromSkillGen
{
    const int W = 14, H = 21;
    static readonly int[] MirrorDir = { 4, 3, 2, 1, 0, 7, 6, 5 };
    static readonly string[] Views = { "Down", "Up", "Side" };

    public class Frame { public string View; public bool Mirror; public char[,] Body; public BromKit.P Gear; public string Note; }

    static BromKit.P P(int mx, int my, int md, int mo, int ox, int oy, int oo) { return new BromKit.P { MX = mx, MY = my, MD = md, MO = mo, OX = ox, OY = oy, OO = oo }; }

    // Side lean: upper body (rows 0-13) pushed 1 px toward the facing (right)
    static char[,] Lean(char[,] g)
    {
        var o = (char[,])g.Clone();
        for (int y = 0; y <= 13; y++) { for (int x = W - 1; x >= 1; x--) o[x, y] = g[x - 1, y]; o[0, y] = '.'; }
        return o;
    }

    static char[,] Base(string v) { return BromGen.Grid(BromGen.Base(v)); }

    public static Frame[] GroundSlam(string v)
    {
        BromKit.P raise = v == "Down" ? P(12, 16, 2, -2, 19, 23, 1) : v == "Up" ? P(19, 16, 2, 2, 8, 23, 1) : P(12, 16, 2, -2, 21, 23, 1);
        return new[]
        {
            new Frame { View = v, Body = Base(v), Gear = raise, Note = "gio bua qua dau" },
            new Frame { View = v, Body = BromGen.Strike(v), Gear = BromKit.Strike(v, 2), Note = "nen xuong dat" },
            new Frame { View = v, Body = BromGen.Strike(v), Gear = Lower(BromKit.Strike(v, 2)), Note = "giu luc va cham" },
        };
    }

    static BromKit.P Lower(BromKit.P p) { return new BromKit.P { MX = p.MX, MY = p.MY + 1, MD = p.MD, MO = p.MO, OX = p.OX, OY = p.OY, OO = p.OO, Hand = p.Hand }; }

    public static Frame[] ShieldCharge(string v)
    {
        BromKit.P g = v == "Down" ? P(7, 21, 3, -2, 16, 25, 1) : v == "Up" ? P(24, 23, 6, 2, 16, 12, -1) : P(11, 22, 4, -2, 24, 22, 1);
        var g2 = new BromKit.P { MX = g.MX, MY = g.MY - 1, MD = g.MD, MO = g.MO, OX = g.OX, OY = g.OY - 1, OO = g.OO };
        return new[]
        {
            new Frame { View = v, Body = v == "Side" ? Lean(BromKit.Walk(v, 0)) : BromKit.Walk(v, 0), Gear = g, Note = "lao: khien chan truoc" },
            new Frame { View = v, Body = v == "Side" ? Lean(BromKit.Walk(v, 2)) : BromKit.Walk(v, 2), Gear = g2, Note = "lao: chan doi" },
        };
    }

    public static Frame[] TauntRoar(string v)
    {
        BromKit.P inhale = BromKit.Rest(v);
        BromKit.P roar = v == "Down" ? P(5, 18, 3, 2, 23, 21, 1) : v == "Up" ? P(25, 17, 1, 2, 7, 21, 1) : P(10, 16, 2, -2, 23, 21, 1);
        return new[]
        {
            new Frame { View = v, Body = BromKit.Idle(v, 2), Gear = inhale, Note = "hit vao, cui dau" },
            new Frame { View = v, Body = Base(v), Gear = roar, Note = "gam: bua va khien dang rong" },
        };
    }

    public static Frame[] IronWall(string v)
    {
        BromKit.P g = v == "Down" ? P(8, 23, 2, -2, 16, 26, 1) : v == "Up" ? P(24, 24, 2, 2, 8, 25, 1) : P(11, 23, 2, -2, 23, 25, 1);
        return new[] { new Frame { View = v, Body = BromGen.Strike(v), Gear = g, Note = "tru vung, khien cam truoc mat" } };
    }

    public static Bitmap Compose(Frame fr)
    {
        var body = EquipGen.Body(fr.Body, BromGen.Pal);
        var p = fr.Gear;
        var behind = new List<EquipGen.Layer>(); var front = new List<EquipGen.Layer>();
        var main = new EquipGen.Layer { S = BromKit.Hammer(p.MD), X = p.MX, Y = p.MY };
        var glove = new EquipGen.Layer { S = BromKit.Glove, X = p.MX - 1, Y = p.MY - 1 };
        var off = new EquipGen.Layer { S = BromGen.Shield(fr.View), X = p.OX, Y = p.OY };
        if (p.MO > 0) { front.Add(main); front.Add(glove); } else { behind.Add(main); behind.Add(glove); }
        if (p.OO > 0) front.Add(off); else behind.Add(off);
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
        foreach (var skill in new[] { "GroundSlam", "ShieldCharge", "TauntRoar", "IronWall" })
            foreach (var v in Views)
            {
                Frame[] fs = skill == "GroundSlam" ? GroundSlam(v) : skill == "ShieldCharge" ? ShieldCharge(v) : skill == "TauntRoar" ? TauntRoar(v) : IronWall(v);
                if (v == "Side") foreach (var f in fs) f.Mirror = true;
                rows.Add(new SheetRow { Skill = skill, View = v, Frames = fs });
            }
        return rows;
    }

    static string Csv(string key, Frame fr)
    {
        var p = fr.Gear; bool m = fr.Mirror;
        int mx = m ? 31 - p.MX : p.MX, md = m ? MirrorDir[p.MD] : p.MD, hx = m ? 31 - p.MX : p.MX - 1, ox = m ? 31 - p.OX : p.OX;
        // key,main_x,main_y,main_dir,main_order,hand_x,hand_y,off_x,off_y,off_view,off_order,slash,slash_order,off_dir
        return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},-1,0,-1\n", key, mx, p.MY, md, p.MO, hx, p.MY - 1, ox, p.OY, fr.View, p.OO);
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
                var fr = row.Frames[f];
                var body = EquipGen.Body(fr.Body, BromGen.Pal);
                for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                        if (body[x, y].HasValue) sheet.SetPixel(f * 32 + (fr.Mirror ? 31 - x : x), r * 32 + y, body[x, y].Value);
                string key = "Skill_" + row.Skill + "_" + row.View + "_" + f;
                csv.Append(Csv(key, fr)); keys.Add(key);
            }
            layout.AppendFormat("{0},{1},{2},{3},{4}\n", r, row.Skill, row.View, row.Frames.Length, string.Join(" ", keys));
        }
        sheet.Save(System.IO.Path.Combine(outDir, "Brom_SkillSheet.png"), ImageFormat.Png); sheet.Dispose();
        System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "Brom_SkillAnchors.csv"), csv.ToString(), new System.Text.UTF8Encoding(false));
        System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "BROM_SKILL_LAYOUT.csv"), layout.ToString(), new System.Text.UTF8Encoding(false));
        return "rows " + rows.Count;
    }
}
