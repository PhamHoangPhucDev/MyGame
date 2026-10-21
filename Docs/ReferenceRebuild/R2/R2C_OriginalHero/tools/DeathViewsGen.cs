using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

// Death frames for the Up and Side views (all 5 classes), added after the class art audit.
// Built from each class's own exported art so it matches exactly; run LAST (after the class exports and the
// back-gear patch), it is idempotent:
//   Death_{Up,Side}_0  kneel   - the class's Idle frame 0 of that view with the upper body (rows < 26) dropped 3 px
//                                 onto the bent legs; anchors = Hurt_{view}_0 (weapons on the back) moved down 3 px
//   Death_{Up,Side}_1  fall    - Side: Death_Down_1, Up: Death_Down_1 mirrored; no gear (same as Down)
//   Death_{Up,Side}_2  lying   - Side: Death_Down_2, Up: Death_Down_2 mirrored; dropped-gear anchors mirrored with it
// 32x32 cells, pivot (16,30) = (0.5, 0.0625) like every body frame. Side frames face LEFT (sheet convention).
public static class DeathViewsGen
{
    static readonly int[] MirrorDir = { 4, 3, 2, 1, 0, 7, 6, 5 };
    const int Drop = 3, KneeRow = 26;

    static Bitmap Crop(Bitmap src, int col, int row)
    {
        var b = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) b.SetPixel(x, y, src.GetPixel(col * 32 + x, row * 32 + y));
        return b;
    }

    static Bitmap Kneel(Bitmap idle)
    {
        var o = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
        for (int y = KneeRow; y < 32; y++) for (int x = 0; x < 32; x++) { var c = idle.GetPixel(x, y); if (c.A > 0) o.SetPixel(x, y, c); }
        for (int y = 0; y < KneeRow; y++) for (int x = 0; x < 32; x++)
            {
                var c = idle.GetPixel(x, y); if (c.A == 0 || y + Drop > 31) continue;
                o.SetPixel(x, y + Drop, c);
            }
        return o;
    }

    static Bitmap Flip(Bitmap b)
    {
        var o = new Bitmap(b.Width, b.Height, PixelFormat.Format32bppArgb);
        for (int y = 0; y < b.Height; y++) for (int x = 0; x < b.Width; x++) o.SetPixel(b.Width - 1 - x, y, b.GetPixel(x, y));
        return o;
    }

    // weapon index mirror: Lyra bow / Selene staff use group*8 + dir for indices < 24 (24 = back tip);
    // Aren sword / Vex dagger use 0-7 for directions and 8+ for back peeks (never mirrored)
    static int MirrorIndex(int d, bool grouped)
    {
        if (d < 0) return d;
        if (grouped ? d >= 24 : d >= 8) return d;
        return d - d % 8 + MirrorDir[d % 8];
    }

    static string[] Shifted(string[] c, int dy)
    {
        var o = (string[])c.Clone();
        foreach (var p in new[] { new[] { 1, 2 }, new[] { 5, 6 }, new[] { 7, 8 } })   // (x, y) pairs: main, hand, off
            if (o[p[0]] != "-99") o[p[1]] = (int.Parse(o[p[1]]) + dy).ToString();
        return o;
    }

    static string[] Mirrored(string[] c, bool grouped)
    {
        var o = (string[])c.Clone();
        foreach (var xi in new[] { 1, 5, 7 }) if (o[xi] != "-99") o[xi] = (31 - int.Parse(o[xi])).ToString();
        if (o[1] != "-99") o[3] = MirrorIndex(int.Parse(o[3]), grouped).ToString();
        if (o.Length > 13 && o[7] != "-99") o[13] = MirrorIndex(int.Parse(o[13]), false).ToString();
        return o;
    }

    // cls: Aren / Vex / Brom / Lyra / Selene ; dir: export folder
    public static string Run(string cls, string dir)
    {
        var log = new StringBuilder();
        bool grouped = cls == "Lyra" || cls == "Selene";
        string extra = Path.Combine(dir, "Extra");
        using (var sheet = new Bitmap(Path.Combine(dir, cls + "_Sheet.png")))
        using (var d1 = new Bitmap(Path.Combine(extra, cls + "_Death_Down_1.png")))
        using (var d2 = new Bitmap(Path.Combine(extra, cls + "_Death_Down_2.png")))
        {
            foreach (var v in new[] { "Up", "Side" })
            {
                int row = v == "Up" ? 1 : 2;   // Idle rows: Down 0, Up 1, Side 2
                using (var idle = Crop(sheet, 0, row)) using (var k = Kneel(idle)) k.Save(Path.Combine(extra, cls + "_Death_" + v + "_0.png"), ImageFormat.Png);
                using (var a = v == "Up" ? Flip(d1) : new Bitmap(d1)) a.Save(Path.Combine(extra, cls + "_Death_" + v + "_1.png"), ImageFormat.Png);
                using (var b = v == "Up" ? Flip(d2) : new Bitmap(d2)) b.Save(Path.Combine(extra, cls + "_Death_" + v + "_2.png"), ImageFormat.Png);
            }
        }
        // anchors
        var path = Path.Combine(dir, cls + "_Anchors.csv");
        var lines = File.ReadAllLines(path).Where(l => l.Length > 0 && !l.StartsWith("Death_Up_") && !l.StartsWith("Death_Side_")).ToList();
        Func<string, string[]> get = key => lines.First(l => l.StartsWith(key + ",")).Split(',');
        var add = new List<string>();
        foreach (var v in new[] { "Up", "Side" })
        {
            var k0 = Shifted(get("Hurt_" + v + "_0"), Drop); k0[0] = "Death_" + v + "_0";
            var k1 = get("Death_Down_1"); k1[0] = "Death_" + v + "_1";
            var k2 = v == "Up" ? Mirrored(get("Death_Down_2"), grouped) : get("Death_Down_2"); k2[0] = "Death_" + v + "_2";
            add.Add(string.Join(",", k0)); add.Add(string.Join(",", k1)); add.Add(string.Join(",", k2));
        }
        // keep the Death rows together: insert right after Death_Down_2
        int at = lines.FindIndex(l => l.StartsWith("Death_Down_2,")) + 1;
        lines.InsertRange(at, add);
        File.WriteAllText(path, string.Join("\n", lines) + "\n", new UTF8Encoding(false));
        log.AppendFormat("{0}: 6 death frames, {1} anchor rows\n", cls, add.Count);
        return log.ToString();
    }
}
