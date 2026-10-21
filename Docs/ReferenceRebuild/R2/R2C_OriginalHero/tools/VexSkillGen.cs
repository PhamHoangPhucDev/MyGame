using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

// Vex skill body frames. Same template / anchors as the approved Vex set (VexEquip): main = right dagger,
// off = left dagger (off_dir = dagger pose), both 8 drawn directions; no hand layer.
//  ShadowStep 2f (sink into shadow, reappear blades raised) · KnifeThrow 2f (wind up, throw) · SmokeBomb 1f (crouch,
//  toss) · Execute 2f (low dash, X cross) · BladeDance 4f (spin, view-independent)
public static class VexSkillGen
{
    const int W = 14, H = 21;
    static readonly int[] MirrorDir = { 4, 3, 2, 1, 0, 7, 6, 5 };
    static readonly string[] Views = { "Down", "Up", "Side" };

    public class Frame { public string View; public bool Mirror; public char[,] Body; public VexEquip.P Gear; public string Note; }

    static VexEquip.P P(int mx, int my, int md, int mo, int ox, int oy, int od, int oo) { return new VexEquip.P { MX = mx, MY = my, MD = md, MO = mo, OX = ox, OY = oy, OD = od, OO = oo }; }
    static void Row(char[,] g, int y, string s) { for (int x = 0; x < W; x++) g[x, y] = s[x]; }
    static char[,] Base(string v) { return VexGen.Grid(VexGen.Base(v)); }

    static char[,] Lean(char[,] g)
    {
        var o = (char[,])g.Clone();
        for (int y = 0; y <= 13; y++) { for (int x = W - 1; x >= 1; x--) o[x, y] = g[x - 1, y]; o[0, y] = '.'; }
        return o;
    }

    public static char[,] Crouch(string v)
    {
        var b = VexGen.Base(v); var g = VexGen.Grid(b);
        for (int y = 18; y >= 1; y--) for (int x = 0; x < W; x++) g[x, y] = b[y - 1][x];
        Row(g, 0, "..............");
        Row(g, 19, "..knnk..knnk..");
        Row(g, 20, ".kBBBk..kBBBk.");
        return g;
    }

    public static Frame[] ShadowStep(string v)
    {
        var sink = v == "Down" ? P(10, 25, 6, 2, 21, 25, 6, 1) : v == "Up" ? P(21, 25, 6, 2, 10, 25, 6, 1) : P(13, 25, 6, -2, 20, 25, 6, 1);
        var rise = v == "Down" ? P(12, 22, 7, 2, 20, 22, 5, 1) : v == "Up" ? P(12, 18, 3, -2, 20, 18, 1, -1) : P(21, 21, 0, 2, 22, 24, 0, 1);
        return new[]
        {
            new Frame { View = v, Body = Crouch(v), Gear = sink, Note = "thup nguoi, lan vao bong" },
            new Frame { View = v, Body = v == "Side" ? Lean(Crouch(v)) : Crouch(v), Gear = rise, Note = "hien sau lung dich, dao gio san" },
        };
    }

    public static Frame[] KnifeThrow(string v)
    {
        var thr = v == "Down" ? P(13, 26, 6, 2, 19, 26, 6, 1) : v == "Up" ? P(23, 18, 2, -2, 8, 18, 2, -1) : P(22, 22, 0, 2, 21, 25, 7, 1);
        return new[]
        {
            new Frame { View = v, Body = VexGen.Attack(v, 0), Gear = VexEquip.Windup(v), Note = "vung dao ra sau lay da" },
            new Frame { View = v, Body = v == "Side" ? Lean(VexGen.Attack(v, 1)) : VexGen.Attack(v, 1), Gear = thr, Note = "phong ca hai tay ve truoc" },
        };
    }

    public static Frame[] SmokeBomb(string v)
    {
        var g = v == "Down" ? P(10, 25, 5, 2, 20, 27, 6, 1) : v == "Up" ? P(21, 25, 7, 2, 10, 27, 6, 1) : P(13, 25, 5, -2, 21, 27, 6, 1);
        return new[] { new Frame { View = v, Body = Crouch(v), Gear = g, Note = "thup xuong, nem bom xuong dat" } };
    }

    public static Frame[] Execute(string v)
    {
        var dash = v == "Down" ? P(12, 26, 6, 2, 20, 26, 6, 1) : v == "Up" ? P(23, 19, 2, -2, 8, 19, 2, -1) : P(22, 22, 0, 2, 21, 24, 0, 1);
        return new[]
        {
            new Frame { View = v, Body = v == "Side" ? Lean(VexGen.Walk(v, 0)) : VexGen.Walk(v, 0), Gear = dash, Note = "lao thap, hai dao chia toi" },
            new Frame { View = v, Body = VexGen.Attack(v, 1), Gear = VexEquip.Strike(v, 3), Note = "chem chu X" },
        };
    }

    // view-independent spin: Down -> Side -> Up -> Side; Side frames drawn facing left (sheet convention)
    public static Frame[] BladeDance()
    {
        return new[]
        {
            new Frame { View = "Down", Body = Base("Down"), Gear = P(9, 22, 4, 2, 22, 22, 0, 1), Note = "xoay 1: dao chia hai ben" },
            new Frame { View = "Side", Mirror = true, Body = Base("Side"), Gear = P(16, 25, 6, 2, 16, 17, 2, -1), Note = "xoay 2" },
            new Frame { View = "Up", Body = Base("Up"), Gear = P(22, 22, 0, 2, 9, 22, 4, 1), Note = "xoay 3" },
            new Frame { View = "Side", Mirror = true, Body = Base("Side"), Gear = P(16, 17, 2, -2, 16, 25, 6, 1), Note = "xoay 4" },
        };
    }

    public static Bitmap Compose(Frame fr)
    {
        var body = EquipGen.Body(fr.Body, VexGen.Pal); var p = fr.Gear;
        var behind = new List<EquipGen.Layer>(); var front = new List<EquipGen.Layer>();
        var items = new List<KeyValuePair<int, EquipGen.Layer>>
        {
            new KeyValuePair<int, EquipGen.Layer>(p.MO, new EquipGen.Layer { S = VexEquip.DaggerSprite(p.MD), X = p.MX, Y = p.MY }),
            new KeyValuePair<int, EquipGen.Layer>(p.OO, new EquipGen.Layer { S = VexEquip.DaggerSprite(p.OD), X = p.OX, Y = p.OY }),
        };
        foreach (var it in items.OrderBy(i => i.Key)) (it.Key > 0 ? front : behind).Add(it.Value);
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
        foreach (var skill in new[] { "ShadowStep", "KnifeThrow", "SmokeBomb", "Execute" })
            foreach (var v in Views)
            {
                Frame[] fs = skill == "ShadowStep" ? ShadowStep(v) : skill == "KnifeThrow" ? KnifeThrow(v) : skill == "SmokeBomb" ? SmokeBomb(v) : Execute(v);
                if (v == "Side") foreach (var f in fs) f.Mirror = true;
                rows.Add(new SheetRow { Skill = skill, View = v, Frames = fs });
            }
        rows.Add(new SheetRow { Skill = "BladeDance", View = null, Frames = BladeDance() });
        return rows;
    }

    static string Csv(string key, Frame fr)
    {
        var p = fr.Gear; bool m = fr.Mirror;
        int mx = m ? 31 - p.MX : p.MX, md = m ? MirrorDir[p.MD] : p.MD, ox = m ? 31 - p.OX : p.OX, od = m ? MirrorDir[p.OD] : p.OD;
        return string.Format("{0},{1},{2},{3},{4},-99,0,{5},{6},{7},{8},-1,0,{9}\n", key, mx, p.MY, md, p.MO, ox, p.OY, fr.View, p.OO, od);
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
                var fr = row.Frames[f]; var body = EquipGen.Body(fr.Body, VexGen.Pal);
                for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                        if (body[x, y].HasValue) sheet.SetPixel(f * 32 + (fr.Mirror ? 31 - x : x), r * 32 + y, body[x, y].Value);
                string key = "Skill_" + row.Skill + (row.View != null ? "_" + row.View : "") + "_" + f;
                csv.Append(Csv(key, fr)); keys.Add(key);
            }
            layout.AppendFormat("{0},{1},{2},{3},{4}\n", r, row.Skill, row.View ?? "Any", row.Frames.Length, string.Join(" ", keys));
        }
        sheet.Save(System.IO.Path.Combine(outDir, "Vex_SkillSheet.png"), ImageFormat.Png); sheet.Dispose();
        System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "Vex_SkillAnchors.csv"), csv.ToString(), new System.Text.UTF8Encoding(false));
        System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "VEX_SKILL_LAYOUT.csv"), layout.ToString(), new System.Text.UTF8Encoding(false));
        return "rows " + rows.Count;
    }
}
