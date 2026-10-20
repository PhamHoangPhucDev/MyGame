using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

// Aren Walk, 4 frames per view (Down, Up, Side 3/4 right), from the approved Idle F0 of each view.
// F0 contact (left foot planted, right lifted), F1 passing, F2 contact (right planted), F3 passing.
// Contact frames drop the upper body 1 px (rows 0-17 -> 1-18), legs become 2 rows; sole stays on row 29.
// Plume and cloak hem swing opposite to the step. Loop 4 x 0.125 s = 0.5 s (same as the old Walk clip).
public static class ArenWalkGen
{
    const int Cell = 32, X0 = 9, Y0 = 9, W = 14, H = 21;

    static char[,] Grid(string[] rows)
    {
        var g = new char[W, H];
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) g[x, y] = rows[y][x];
        return g;
    }

    static void Row(char[,] g, int y, string s) { for (int x = 0; x < W; x++) g[x, y] = s[x]; }

    static void Drop(char[,] g, string[] b)
    {
        for (int y = 18; y >= 1; y--) for (int x = 0; x < W; x++) g[x, y] = b[y - 1][x];
        Row(g, 0, "..............");
    }

    public static char[,] Frame(string view, string[] b, int i)
    {
        var g = Grid(b);
        bool side = view == "Side";
        switch (i)
        {
            case 0: // contact: left foot planted (Down/Up) / near leg forward (Side)
                Drop(g, b);
                if (side) { Row(g, 19, "..kaAk..kZak.."); Row(g, 20, ".kaAk....kZaAk"); }
                else { Row(g, 19, "...kZakkaZak.."); Row(g, 20, "..kZaAk......."); }
                break;
            case 1: // passing: idle legs, cloak hem swings out
                Row(g, 18, side || view == "Down" ? ".kCCkAAkkAAk.." : "..kAAkkAACkCCk");
                break;
            case 2: // contact: right foot planted / far leg forward
                Drop(g, b);
                if (side) { Row(g, 19, "..kZak..kaAk.."); Row(g, 20, ".kZak....kaAAk"); }
                else { Row(g, 19, "..kZaAkkZak..."); Row(g, 20, ".......kZaAk.."); }
                break;
            case 3: // passing: idle legs, plume trails the other way
                if (view == "Down") { Row(g, 0, "......Ppq....."); g[13, 6] = 'p'; }
                else if (view == "Up") Row(g, 0, "......Ppq.....");
                else { Row(g, 0, "..qpP........."); g[0, 7] = 'p'; }
                break;
        }
        return g;
    }

    public static string Run(string outDir)
    {
        var c = HeroConceptGen.Roster.First(r => r.Id == "4");
        var views = new List<KeyValuePair<string, string[]>>
        {
            new KeyValuePair<string, string[]>("Down", c.Rows),
            new KeyValuePair<string, string[]>("Up", ArenIdleGen.Up),
            new KeyValuePair<string, string[]>("Side", ArenIdleGen.Side),
        };
        var log = new StringBuilder();
        foreach (var v in views)
        {
            var b = v.Value;
            var sheet = new Bitmap(Cell * 4, Cell, PixelFormat.Format32bppArgb);
            for (int f = 0; f < 4; f++)
            {
                var g = Frame(v.Key, b, f);
                int minX = 99, maxX = -1, minY = 99, maxY = -1;
                for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
                    {
                        if (g[x, y] == '.') continue;
                        sheet.SetPixel(f * Cell + X0 + x, Y0 + y, HeroConceptGen.Hex(c.Pal[g[x, y]]));
                        minX = Math.Min(minX, X0 + x); maxX = Math.Max(maxX, X0 + x); minY = Math.Min(minY, Y0 + y); maxY = Math.Max(maxY, Y0 + y);
                    }
                log.AppendFormat("{0} F{1}: bbox x {2}..{3} y {4}..{5} -> {6}x{7}\n", v.Key, f, minX, maxX, minY, maxY, maxX - minX + 1, maxY - minY + 1);
            }
            sheet.Save(System.IO.Path.Combine(outDir, "Aren_Walk" + v.Key + "_v1.png"), ImageFormat.Png);
            sheet.Dispose();
        }
        return log.ToString();
    }
}
