using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

// Selene skill body frames. Same template / anchors as the approved Selene set (SeleneKit): main = staff sheet index
// len*8+dir (len 0 long, 1 short), hand = fist on the grip.
//  StarOrb 2f (raise high, thrust) · Blink 2f (tap the ground, reappear staff across) · RuneCircle 2f (gem to the
//  ground, rise) · ManaShield 1f (staff upright, focus) · MeteorShower 2f (staff back overhead, sweep forward)
public static class SeleneSkillGen
{
    const int W = 14, H = 21;
    static readonly int[] MirrorDir = { 4, 3, 2, 1, 0, 7, 6, 5 };
    static readonly string[] Views = { "Down", "Up", "Side" };

    public class Frame { public string View; public bool Mirror; public char[,] Body; public SeleneKit.P Gear; public string Note; }

    static SeleneKit.P P(int x, int y, int idx, int order) { return new SeleneKit.P { MX = x, MY = y, MI = idx, MO = order, Hand = true }; }
    static void Row(char[,] g, int y, string s) { for (int x = 0; x < W; x++) g[x, y] = s[x]; }
    static char[,] Base(string v) { return EquipGen.Grid(SeleneGen.Base(v)); }

    // robe crouch: everything drops 1 px, shoes spread on row 20
    public static char[,] Crouch(string v)
    {
        var b = SeleneGen.Base(v); var g = EquipGen.Grid(b);
        for (int y = 19; y >= 1; y--) for (int x = 0; x < W; x++) g[x, y] = b[y - 1][x];
        Row(g, 0, "..............");
        Row(g, 20, v == "Side" ? "..kBBk..kBBk.." : "..kBBk..kBBk..");
        return g;
    }

    public static Frame[] StarOrb(string v)
    {
        var raise = v == "Down" ? P(10, 18, 2, 2) : v == "Up" ? P(21, 18, 2, -2) : P(21, 18, 2, 2);
        return new[]
        {
            new Frame { View = v, Body = Base(v), Gear = raise, Note = "gio gay that cao, tu phep" },
            new Frame { View = v, Body = Base(v), Gear = SeleneKit.Cast(v, 1), Note = "chia gay phong cau" },
        };
    }

    public static Frame[] Blink(string v)
    {
        var tap = v == "Down" ? P(10, 27, 2, 2) : v == "Up" ? P(21, 27, 2, 2) : P(22, 27, 2, 2);
        var across = v == "Down" ? P(11, 22, 0, 2) : v == "Up" ? P(11, 22, 0, 2) : P(13, 22, 0, 2);
        return new[]
        {
            new Frame { View = v, Body = Crouch(v), Gear = tap, Note = "go gay xuong dat" },
            new Frame { View = v, Body = Base(v), Gear = across, Note = "hien ra, gay ngang nguc" },
        };
    }

    public static Frame[] RuneCircle(string v)
    {
        var draw = v == "Down" ? P(16, 19, 14, 2) : v == "Up" ? P(21, 19, 14, 2) : P(21, 19, 14, 2);
        return new[]
        {
            new Frame { View = v, Body = Crouch(v), Gear = draw, Note = "chia ngoc xuong dat ve vong" },
            new Frame { View = v, Body = Base(v), Gear = SeleneKit.Cast(v, 0), Note = "dung thang, gio gay" },
        };
    }

    public static Frame[] ManaShield(string v)
    {
        var g = v == "Down" ? P(16, 25, 2, 2) : v == "Up" ? P(21, 25, 2, 2) : P(20, 25, 2, 2);
        return new[] { new Frame { View = v, Body = Base(v), Gear = g, Note = "dung gay giua than, tap trung" } };
    }

    public static Frame[] MeteorShower(string v)
    {
        var back = v == "Down" ? P(12, 17, 3, -2) : v == "Up" ? P(20, 17, 1, 2) : P(18, 17, 3, 2);
        var sweep = v == "Side" ? P(21, 22, 15, 2) : SeleneKit.Cast(v, 1);
        return new[]
        {
            new Frame { View = v, Body = Base(v), Gear = back, Note = "gio gay nga ra sau dau" },
            new Frame { View = v, Body = Base(v), Gear = sweep, Note = "vung bo ve phia truoc" },
        };
    }

    public static Bitmap Compose(Frame fr)
    {
        var body = EquipGen.Body(fr.Body, SeleneGen.Pal); var p = fr.Gear;
        var layers = new List<EquipGen.Layer> { new EquipGen.Layer { S = SeleneKit.StaffSprite(p.MI), X = p.MX, Y = p.MY }, new EquipGen.Layer { S = SeleneGen.Hand, X = p.MX - 1, Y = p.MY - 1 } };
        var bmp = p.MO > 0 ? EquipGen.Compose(body, new List<EquipGen.Layer>(), layers) : EquipGen.Compose(body, layers, new List<EquipGen.Layer>());
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
        foreach (var skill in new[] { "StarOrb", "Blink", "RuneCircle", "ManaShield", "MeteorShower" })
            foreach (var v in Views)
            {
                Frame[] fs = skill == "StarOrb" ? StarOrb(v) : skill == "Blink" ? Blink(v) : skill == "RuneCircle" ? RuneCircle(v) : skill == "ManaShield" ? ManaShield(v) : MeteorShower(v);
                if (v == "Side") foreach (var f in fs) f.Mirror = true;
                rows.Add(new SheetRow { Skill = skill, View = v, Frames = fs });
            }
        return rows;
    }

    static string Csv(string key, Frame fr)
    {
        var p = fr.Gear; bool m = fr.Mirror;
        int mx = m ? 31 - p.MX : p.MX, mi = m ? (p.MI / 8) * 8 + MirrorDir[p.MI % 8] : p.MI, hx = m ? 31 - p.MX : p.MX - 1;
        return string.Format("{0},{1},{2},{3},{4},{5},{6},-99,0,{7},0,-1,0,-1\n", key, mx, p.MY, mi, p.MO, hx, p.MY - 1, fr.View);
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
                var fr = row.Frames[f]; var body = EquipGen.Body(fr.Body, SeleneGen.Pal);
                for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                        if (body[x, y].HasValue) sheet.SetPixel(f * 32 + (fr.Mirror ? 31 - x : x), r * 32 + y, body[x, y].Value);
                string key = "Skill_" + row.Skill + "_" + row.View + "_" + f;
                csv.Append(Csv(key, fr)); keys.Add(key);
            }
            layout.AppendFormat("{0},{1},{2},{3},{4}\n", r, row.Skill, row.View, row.Frames.Length, string.Join(" ", keys));
        }
        sheet.Save(System.IO.Path.Combine(outDir, "Selene_SkillSheet.png"), ImageFormat.Png); sheet.Dispose();
        System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "Selene_SkillAnchors.csv"), csv.ToString(), new System.Text.UTF8Encoding(false));
        System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "SELENE_SKILL_LAYOUT.csv"), layout.ToString(), new System.Text.UTF8Encoding(false));
        return "rows " + rows.Count;
    }
}
