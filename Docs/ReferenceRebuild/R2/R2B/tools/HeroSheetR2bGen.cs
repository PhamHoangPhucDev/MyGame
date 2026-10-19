// HR01b: redraws Walk_Side (4) and Attack_Side (3) as dedicated poses (C17 0:36.45 run, C25 0:02.53 strike)
// on top of Hero_Sheet_R2.png. All frames face LEFT, feet on image row 29 (pivot 0.0625), HR06 palette.
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

public static class HeroSheetR2bGen
{
    static readonly Dictionary<char, Color> Pal = new Dictionary<char, Color>
    {
        { 'W', Hex("FCFCFC") }, { 'S', Hex("D8D8D8") }, { 'H', Hex("B4B4B8") },
        { 'K', Hex("FCF0D8") }, { 'E', Hex("6C7CD0") },
        { 'C', Hex("3C546C") }, { 'L', Hex("60909C") }, { 'O', Hex("303C48") },
        { 'X', Hex("1C1A26") }, { 'Q', Hex("F0FCFC") }, { 'B', Hex("3C3C48") },
        { 'Y', Hex("E09A52") }, { 'Z', Hex("A0602C") },
        { 'G', Hex("C8D8E0") }, { 'M', Hex("7890A0") }, { 'N', Hex("3C4C58") }
    };

    // Run upper body: head low and forward, hair streaming back, coat tail flowing back (≈30° lean).
    static readonly string[] RunUpper = {
        ".......HHHHHH.............",
        ".....HHWWWWWWHHH..........",
        "....HWWWWWWWWWWWHHH.......",
        "...HWWSWWWWWWSWWWWWHH.....",
        "...HWSWWWWWWWWSWWWWWWHH...",
        "..HWSKKKKWWWWWWSWWSWWWWH..",
        "..OKKKKKKSWWWWWWSWWSHHH...",
        "..OKOOKKKSWWWWWWWWHH......",
        "..OKEEKKKSWWWSWWWH........",
        "...OKKKKQSWWSWWH..........",
        "....OOQQLCCCSWH...........",
        ".....OLCCCCCCCCO..........",
        "......OLCCCCCCCCCO........",
        ".......OCCCCCCCCCCCO......",
        "........OOOOOOOOOOOOO.....",
    };
    // Contact pose: near leg (B) reaching forward, far leg (X) pushing back, cream feet visible.
    static readonly string[] LegsContactA = {
        ".......OBBO.....OXXO......",
        "......OBBO.......OXXO.....",
        ".....OKKO.........OKKO....",
        ".....OOO...........OOO....",
    };
    static readonly string[] LegsContactB = {
        ".......OXXO.....OBBO......",
        "......OXXO.......OBBO.....",
        ".....OKKO.........OKKO....",
        ".....OOO...........OOO....",
    };
    // Passing pose (body lifted 1 px): legs gathered under the hips.
    static readonly string[] LegsPass = {
        "..........OBBOOXXO........",
        "..........OBBOOXXO........",
        "...........OBBOXXO........",
        "...........OKKOKKO........",
        "...........OOO.OOO........",
    };

    // Attack strike (C25 0:02.53): deep crouch, cloak streams horizontally behind, lunge stance.
    static readonly string[] AttackStrike = {
        "......HHHHHH................",
        "....HHWWWWWWHHH.............",
        "...HWWWWWWWWWWWHHH..........",
        "..HWWSWWWWWWSWWWWWHHH.......",
        "..HWSKKKKWWWWWSWWWWWWHH.....",
        ".OKKKKKKSWWWWWWSWWSWWWWH....",
        ".OKOOKKKSWWWWWWWWSHHHH......",
        ".OKEEKKKSWWWSWWWHH..........",
        "..OKKKKQSWWSWWH.............",
        "...OOQQLCCCCSCCCCCCCOO......",
        "....OLCCCCCCCCCCCCCCCCCOO...",
        ".....OLCCCCCCCCCCCCCCCCCCCO.",
        "......OOCCCCCOOOOOOOOOOOOO..",
        ".....OBBOOOOO..OXXO.........",
        "...OBBBO........OXXO........",
        "..OKKO............OXXO......",
        "..OOO...............OKKO....",
        ".....................OOO....",
    };

    public static string Build(string basePath, string outPath)
    {
        var sheet = new Bitmap(basePath);
        var report = new System.Text.StringBuilder();

        var runA = Concat(TrimHair(RunUpper, 3), LegsContactA); // C17: rounded hair mass, not a long spike
        var runPass = Concat(TrimHair(RunUpper, 4), LegsPass);
        var runB = Concat(TrimHair(SwayHair(RunUpper), 3), LegsContactB);
        var runs = new[] { runA, runPass, runB, runPass };

        var lunge = Concat(Take(RunUpper, 11), new[] {
            "......OLCCCCCCCCCCO.......",
            ".......OLCCCCCCCCCCCCO....",
            "........OOOOOOOOOOOOOOO...",
            "........OBBO...OXXO.......",
            ".......OBBO.....OXXO......",
            "......OBBO.......OXXO.....",
            ".....OKKO.........OKKO....",
            ".....OOO...........OOO....",
        });
        var recover = Concat(Take(TrimHair(RunUpper, 1), 11), new[] {
            "......OLCCCCCCCCCO........",
            ".......OLCCCCCCCCCCO......",
            "........OOOOOOOOOOOO......",
            "..........OBBOOXXO........",
            ".........OBBO.OXXO........",
            "........OBBO...OXXO.......",
            "........OKKO....OKKO......",
            "........OOO......OOO......",
        });
        var attacks = new[] { lunge, AttackStrike, recover };

        // Row 5 = Walk_Side (image y 160), row 8 = Attack_Side (image y 256).
        for (int i = 0; i < 4; i++) report.Append(Draw(sheet, runs[i], i, 5, "Walk_Side_" + i));
        for (int i = 0; i < 3; i++) report.Append(Draw(sheet, attacks[i], i, 8, "Attack_Side_" + i));
        sheet.Save(outPath, ImageFormat.Png);
        sheet.Dispose();
        return report.ToString();
    }

    static string Draw(Bitmap sheet, string[] rows, int col, int row, string name)
    {
        int cx = col * 32, cy = row * 32;
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) sheet.SetPixel(cx + x, cy + y, Color.Transparent);
        int width = 0;
        foreach (var r in rows) { width = Math.Max(width, r.Length); foreach (char c in r) if (c != '.' && !Pal.ContainsKey(c)) throw new Exception(name + ": bad char " + c); }
        int top = 29 - (rows.Length - 1);
        int left = 3;
        if (left + width > 32 || top < 0) throw new Exception(name + " does not fit: w=" + width + " h=" + rows.Length);
        int minX = 99, maxX = -1;
        for (int y = 0; y < rows.Length; y++)
            for (int x = 0; x < rows[y].Length; x++)
            {
                char c = rows[y][x];
                if (c == '.') continue;
                sheet.SetPixel(cx + left + x, cy + top + y, Pal[c]);
                minX = Math.Min(minX, left + x); maxX = Math.Max(maxX, left + x);
            }
        return string.Format("{0}: {1}x{2} top{3} bottom29 x{4}-{5}\n", name, maxX - minX + 1, rows.Length, top, minX, maxX);
    }

    static string[] Concat(string[] a, string[] b) { var l = new List<string>(a); l.AddRange(b); return l.ToArray(); }

    static string[] Take(string[] a, int n) { var l = new List<string>(); for (int i = 0; i < n; i++) l.Add(a[i]); return l.ToArray(); }

    // Shorten the hair stream: drop the n right-most hair pixels on head rows 1..8 and re-edge with H.
    static string[] TrimHair(string[] rows, int n)
    {
        var o = (string[])rows.Clone();
        for (int y = 1; y <= 8 && y < o.Length; y++)
        {
            var c = o[y].ToCharArray();
            int max = -1;
            for (int x = 0; x < c.Length; x++) if (c[x] != '.') max = x;
            if (max < 0 || !IsHair(c[max])) continue;
            int removed = 0;
            for (int x = max; x >= 0 && removed < n && IsHair(c[x]); x--, removed++) c[x] = '.';
            int edge = max - removed;
            if (edge >= 0 && IsHair(c[edge])) c[edge] = 'H';
            o[y] = new string(c);
        }
        return o;
    }

    // Second contact frame: hair tips wave (rows 3/5 one shorter, row 4 one longer) so A and B differ.
    static string[] SwayHair(string[] rows)
    {
        var o = (string[])rows.Clone();
        o[3] = TrimRow(o[3], 1);
        o[5] = TrimRow(o[5], 1);
        o[4] = o[4].Substring(0, 21) + "WWWH" + "."; // stream one pixel longer than frame A
        return o;
    }

    static string TrimRow(string r, int n)
    {
        var tmp = TrimHair(new[] { "", r, "", "", "", "", "", "", "" }, n);
        return tmp[1];
    }

    static bool IsHair(char c) { return c == 'H' || c == 'W' || c == 'S'; }

    static Color Hex(string h)
    {
        return Color.FromArgb(255, Convert.ToInt32(h.Substring(0, 2), 16), Convert.ToInt32(h.Substring(2, 2), 16), Convert.ToInt32(h.Substring(4, 2), 16));
    }
}
