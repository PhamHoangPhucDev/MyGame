using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

// R2A Hero_Idle_Down sample v1: 4 frames, 32x32 cells, body 14x21 (cols 9..22, rows 9..29), HR06 palette only.
// Layout traced from C06 0:01.3 (zoomed idle) and C03 0:02.1. Sword is NOT drawn (separate Sword_Back sprite).
public static class HeroIdleSampleGen
{
    static readonly Dictionary<char, Color> Pal = new Dictionary<char, Color>
    {
        { 'W', Hex("FCFCFC") }, // hair_light
        { 'h', Hex("D8D8D8") }, // hair_shade
        { 'o', Hex("B4B4B8") }, // hair_deep (hair outline)
        { 's', Hex("FCF0D8") }, // skin
        { 'e', Hex("6C7CD0") }, // eye
        { 'c', Hex("3C546C") }, // coat
        { 'l', Hex("60909C") }, // coat_light
        { 'd', Hex("303C48") }, // coat_dark (outline)
        { 'k', Hex("1C1A26") }, // cloak_back (lashes, coat opening)
        { 'w', Hex("F0FCFC") }, // collar_hi (collar, legs)
        { 'b', Hex("3C3C48") }, // boots
    };

    // Frame 0, 14 x 21. Row 0 = top of hair (cell row 9), row 20 = sole (cell row 29).
    static readonly string[] Base =
    {
        "....oWWWWo....", // 0  hair dome
        "..oWWWWWWWWo..", // 1
        ".oWWhhWWWWWWo.", // 2  shine clumps
        "oWWhhWWWWWhhWo", // 3
        "oWWWWWWWWWWhWo", // 4
        "oWWWhWWWWhWWWo", // 5
        "oWWhhshhshhWWo", // 6  fringe tips
        "oWWkkssssskkWo", // 7  lashes (row 7 = 34% of height)
        "oWWeessssseeWo", // 8  eyes
        "oWWeessssseeWo", // 9
        "oWWdssssssdWWo", // 10 chin, coat shoulders
        "oWdccwkkwccdWo", // 11 collar points
        "oodlcckkcccdoo", // 12 hair tips end
        ".dllcckkcccdd.", // 13 coat (trapezoid)
        ".dlccckkccccd.", // 14
        "dlcccckkccccdd", // 15
        "dlcccdwwdcccdd", // 16 legs between flaps
        "dlcccdwwdccccd", // 17
        ".dddddwwddddd.", // 18 hem
        "......bb......", // 19 boots
        ".....bbbb.....", // 20
    };

    const int Cell = 32, X0 = 9, Y0 = 9, BW = 14, BH = 21;

    static char[,] Grid(string[] rows)
    {
        var g = new char[BW, BH];
        for (int y = 0; y < BH; y++) for (int x = 0; x < BW; x++) g[x, y] = rows[y][x];
        return g;
    }

    static char[,] Frame(int i)
    {
        var g = Grid(Base);
        if (i == 1)
        {
            // left hair tip lifts 1 px
            g[0, 12] = '.'; g[1, 12] = 'o'; g[0, 11] = 'o'; g[1, 11] = 'W';
        }
        else if (i == 2)
        {
            // coat hem flares 1 px both sides, hair tips back down
            g[0, 18] = 'd'; g[13, 18] = 'd'; g[0, 17] = 'd'; g[1, 17] = 'l';
        }
        else if (i == 3)
        {
            // right hair tip lifts 1 px (mirror of frame 1)
            g[13, 12] = '.'; g[12, 12] = 'o'; g[13, 11] = 'o'; g[12, 11] = 'W';
        }
        return g;
    }

    // v2: no grey outline on the hair dome (rows 0-2), outline kept on the sides and lower half.
    static readonly string[] DomeV2 =
    {
        "....WWWWWW....",
        "..WWWWWWWWWW..",
        ".WWWhhWWWWWWW.",
    };

    // v2 sway: both hair tips lift together, 1 px (F1, F3) then 2 px (F2), so the motion reads at 1x.
    static char[,] FrameV2(int i)
    {
        var rows = (string[])Base.Clone();
        for (int r = 0; r < DomeV2.Length; r++) rows[r] = DomeV2[r];
        var g = Grid(rows);
        if (i == 1 || i == 3)
        {
            g[0, 12] = '.'; g[13, 12] = '.';
        }
        else if (i == 2)
        {
            g[0, 12] = '.'; g[13, 12] = '.'; g[0, 11] = '.'; g[13, 11] = '.';
            g[1, 12] = '.'; g[12, 12] = '.'; g[1, 11] = 'o'; g[12, 11] = 'o';
            g[0, 18] = 'd'; g[13, 18] = 'd'; // hem flares as the hair lifts
        }
        return g;
    }

    // v3 side-to-side sway (v2 dome): F1 left tip lifts 2 px, F2 both tips 1 px (passing centre), F3 right tip lifts 2 px.
    static char[,] FrameV3(int i)
    {
        var rows = (string[])Base.Clone();
        for (int r = 0; r < DomeV2.Length; r++) rows[r] = DomeV2[r];
        var g = Grid(rows);
        if (i == 1) LiftTip(g, 0, 1);
        else if (i == 2) { g[0, 12] = '.'; g[13, 12] = '.'; }
        else if (i == 3) LiftTip(g, 13, 12);
        return g;
    }

    static void LiftTip(char[,] g, int outer, int inner)
    {
        g[outer, 12] = '.'; g[outer, 11] = '.'; g[inner, 12] = '.'; g[inner, 11] = 'o';
    }

    public static string Run(string outDir) { return Run(outDir, 1); }

    public static string Run(string outDir, int version)
    {
        var sheet = new Bitmap(Cell * 4, Cell, PixelFormat.Format32bppArgb);
        for (int f = 0; f < 4; f++)
        {
            var g = version == 3 ? FrameV3(f) : version == 2 ? FrameV2(f) : Frame(f);
            for (int y = 0; y < BH; y++) for (int x = 0; x < BW; x++)
                    if (g[x, y] != '.') sheet.SetPixel(f * Cell + X0 + x, Y0 + y, Pal[g[x, y]]);
        }
        sheet.Save(System.IO.Path.Combine(outDir, "HeroIdle_Sample_v" + version + ".png"), ImageFormat.Png);

        // Self-check: bbox per frame, colours outside HR06.
        var report = new System.Text.StringBuilder();
        var hr06 = new HashSet<int>();
        foreach (var hx in new[] { "FCFCFC", "D8D8D8", "B4B4B8", "FCF0D8", "6C7CD0", "3C546C", "60909C", "303C48", "1C1A26", "F0FCFC", "3C3C48", "E09A52", "A0602C", "C8D8E0", "7890A0", "3C4C58" })
            hr06.Add(Hex(hx).ToArgb());
        for (int f = 0; f < 4; f++)
        {
            int minX = 99, minY = 99, maxX = -1, maxY = -1, bad = 0;
            for (int y = 0; y < Cell; y++) for (int x = 0; x < Cell; x++)
                {
                    var c = sheet.GetPixel(f * Cell + x, y);
                    if (c.A == 0) continue;
                    if (!hr06.Contains(Color.FromArgb(255, c.R, c.G, c.B).ToArgb())) bad++;
                    minX = Math.Min(minX, x); maxX = Math.Max(maxX, x); minY = Math.Min(minY, y); maxY = Math.Max(maxY, y);
                }
            report.AppendFormat("frame {0}: x {1}..{2} y {3}..{4} -> {5}x{6}, off-palette {7}\n", f, minX, maxX, minY, maxY, maxX - minX + 1, maxY - minY + 1, bad);
        }
        sheet.Dispose();
        return report.ToString();
    }

    static Color Hex(string h) { return Color.FromArgb(255, Convert.ToInt32(h.Substring(0, 2), 16), Convert.ToInt32(h.Substring(2, 2), 16), Convert.ToInt32(h.Substring(4, 2), 16)); }
}
