using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;

// Aren (Hero chinh) Idle, 4 frames per view, built from the approved roster F0 (HeroConceptGen.Roster id "4").
// Views: Down (approved v1), Up (back), Side (3/4 facing right; left = flipX in Unity).
// Motion (same for every view): plume sways, cloak hem swings out, helmet nods 1 px on F2 (breath).
// Everything stays inside the 14x21 template; sole stays on row 29.
public static class ArenIdleGen
{
    const int Cell = 32, X0 = 9, Y0 = 9, W = 14, H = 21;

    // Back view: plume runs down the back of the helm, white cloth on his right pauldron (viewer's left),
    // navy cloak from his left shoulder drapes over the right half of the back.
    public static readonly string[] Up =
    {
        ".......Ppq....",
        "....kkkPpqp...",
        "...kZWPpaAk...",
        "..kZWaPpaAAk..",
        ".kZWaaPpaaAAk.",
        ".kZaaaPpaaaAk.",
        ".kAaaaPpaaAAk.",
        ".kAaaaPpaaAAk.",
        ".kAAaaqPaaAAk.",
        ".kAAAAAAAAAAk.",
        "..kAaaaaaAAk..",
        "kOookAaaacCcuk",
        "kOoAkaaaccCcuk",
        "kAOAkaaacCccuk",
        "kAAAkaaacCccuk",
        "kkbbbbbbcCccuk",
        ".kaAAaaacCccuk",
        "...kaAAkcCCcuk",
        "..kAAkkAACkCk.",
        "...kAAkkAAk...",
        "..kAAAkkAAAk..",
    };

    // 3/4 facing right: visor and ridge shifted right, plume streams back to the left,
    // cloak trails behind on the left, white cloth on the near (right) pauldron, toes point right.
    public static readonly string[] Side =
    {
        "...qpP........",
        "...pqPPkkk....",
        "..PpkZWaaZak..",
        ".PpkZWaaaaZAk.",
        "PpkZWaaaaaZAk.",
        "PkZaaaaaaaZAk.",
        "PkAAAAAAAAZAk.",
        ".kAaavavavZvk.",
        ".kAaavavavZvk.",
        ".kAaaaaaaaZAk.",
        "..kAaaaaZaAk..",
        "kuckaZaaAkoOk.",
        "kucCkWaaAkooOk",
        "kucCkZaaAkOAAk",
        "kucCkZaaAkAAk.",
        "kucCbbgbbdbkk.",
        "kucCkaZaAkdk..",
        "kuCkkaZaAAk...",
        "..kCkAAkkAAk..",
        "...kAAkkAAk...",
        "...kAAAkkAAAk.",
    };

    static char[,] Grid(string[] rows)
    {
        var g = new char[W, H];
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) g[x, y] = rows[y][x];
        return g;
    }

    static void Row(char[,] g, int y, string s) { for (int x = 0; x < W; x++) g[x, y] = s[x]; }

    static bool Cloak(char c) { return c == 'u' || c == 'c' || c == 'C'; }

    // Breath: rows 0-9 (helmet + plume) move down 1 px. On row 10 the shifted helmet wins,
    // except where row 10 is cloak (kept) or where the helmet row is empty.
    static void Nod(char[,] g, string[] b, int keepTo = -1)
    {
        for (int y = 10; y >= 1; y--)
            for (int x = 0; x < W; x++)
            {
                char src = b[y - 1][x];
                if (y == 10 && (x <= keepTo || Cloak(b[10][x]) || src == '.')) { g[x, y] = b[10][x]; continue; }
                g[x, y] = src;
            }
        Row(g, 0, "..............");
    }

    public static char[,] Frame(string view, string[] b, int i)
    {
        var g = Grid(b);
        if (view == "Down")
        {
            switch (i)
            {
                case 1: Row(g, 0, "........Ppq..."); g[13, 5] = '.'; Row(g, 18, ".kCCkAAkkAAk.."); break;
                case 2: Nod(g, b, 3); g[13, 7] = 'P'; break;
                case 3: Row(g, 0, "......Ppq....."); g[13, 6] = 'p'; break;
            }
        }
        else if (view == "Up")
        {
            switch (i)
            {
                case 1: Row(g, 0, "........Ppq..."); Row(g, 18, "..kAAkkAACkCCk"); break;
                case 2: Nod(g, b); break;
                case 3: Row(g, 0, "......Ppq....."); g[7, 9] = 'q'; break;
            }
        }
        else // Side
        {
            switch (i)
            {
                case 1: Row(g, 0, "..qpP........."); g[0, 5] = '.'; g[0, 6] = '.'; Row(g, 18, ".kCCkAAkkAAk.."); break;
                case 2: Nod(g, b); break;
                case 3: Row(g, 0, "....qpP......."); g[0, 7] = 'p'; break;
            }
        }
        return g;
    }

    public static string Run(string outDir)
    {
        var c = HeroConceptGen.Roster.First(r => r.Id == "4");
        var views = new List<KeyValuePair<string, string[]>>
        {
            new KeyValuePair<string, string[]>("Down", c.Rows),
            new KeyValuePair<string, string[]>("Up", Up),
            new KeyValuePair<string, string[]>("Side", Side),
        };
        var log = new StringBuilder();
        foreach (var v in views)
        {
            var b = v.Value;
            if (b.Length != H) throw new Exception(v.Key + " rows " + b.Length);
            for (int y = 0; y < H; y++)
            {
                if (b[y].Length != W) throw new Exception(v.Key + " row " + y + " len " + b[y].Length);
                foreach (char ch in b[y]) if (ch != '.' && !c.Pal.ContainsKey(ch)) throw new Exception(v.Key + " row " + y + " unknown '" + ch + "'");
            }
            var sheet = new Bitmap(Cell * 4, Cell, PixelFormat.Format32bppArgb);
            for (int f = 0; f < 4; f++)
            {
                var g = Frame(v.Key, b, f);
                int minX = 99, maxX = -1, minY = 99, maxY = -1, changed = 0;
                for (int y = 0; y < H; y++) for (int x = 0; x < W; x++)
                    {
                        if (g[x, y] != b[y][x]) changed++;
                        if (g[x, y] == '.') continue;
                        sheet.SetPixel(f * Cell + X0 + x, Y0 + y, HeroConceptGen.Hex(c.Pal[g[x, y]]));
                        minX = Math.Min(minX, X0 + x); maxX = Math.Max(maxX, X0 + x); minY = Math.Min(minY, Y0 + y); maxY = Math.Max(maxY, Y0 + y);
                    }
                log.AppendFormat("{0} F{1}: bbox x {2}..{3} y {4}..{5} -> {6}x{7}, px changed vs F0 {8}\n", v.Key, f, minX, maxX, minY, maxY, maxX - minX + 1, maxY - minY + 1, changed);
            }
            sheet.Save(System.IO.Path.Combine(outDir, "Aren_Idle" + v.Key + "_v1.png"), ImageFormat.Png);
            sheet.Dispose();
        }
        return log.ToString();
    }
}
