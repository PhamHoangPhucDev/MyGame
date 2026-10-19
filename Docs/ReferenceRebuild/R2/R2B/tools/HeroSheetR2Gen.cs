// HR01 generator: draws Hero_Sheet_R2.png (128x288, 33 cells of 32x32) from hand-drawn
// pixel templates. Layout and slot order match Assets/Art/GoblinCampPhase4/Hero_Sheet.png
// so the existing slices/Animator can be reused. C# 5 (Windows PowerShell Add-Type).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

public static class HeroSheetR2Gen
{
    // HR06 locked palette (+ hair outline #B4B4B8 approved).
    static readonly Dictionary<char, Color> Pal = new Dictionary<char, Color>
    {
        { 'W', Hex("FCFCFC") }, { 'S', Hex("D8D8D8") }, { 'H', Hex("B4B4B8") },
        { 'K', Hex("FCF0D8") }, { 'E', Hex("6C7CD0") },
        { 'C', Hex("3C546C") }, { 'L', Hex("60909C") }, { 'O', Hex("303C48") },
        { 'X', Hex("1C1A26") }, { 'Q', Hex("F0FCFC") }, { 'B', Hex("3C3C48") },
        { 'Y', Hex("E09A52") }, { 'Z', Hex("A0602C") },
        { 'G', Hex("C8D8E0") }, { 'M', Hex("7890A0") }, { 'N', Hex("3C4C58") }
    };

    // ---------------- DOWN (front) ----------------
    static readonly string[] DownHead = {
        "......HHHHHH......",
        "....HHWWWWWWHH....",
        "...HWWWWWWWWWWH...",
        "..HWWWWWWWWWWWWH..",
        "..HWWWWSWWSWWWWH..",
        ".HWWWSWWWWWWSWWWH.",
        ".HWWSWWSWWSWWSWWH.",
        "HWWWSKKKKKKKKSWWWH",
        "HWWWKOOKKKKOOKWWWH",
        "HWWWKEEKKKKEEKWWWH",
        "HWWSKKKKKKKKKKSWWH",
    };
    static readonly string[] DownBody = {
        "HWSOCCQQQQQQCCOSWH",
        "HWSOLCCCQQCCCCOSWH",
        "HWSOLCCCXXCCCCOSWH",
        ".HSOLCCCXXCCCCOSH.",
        ".HHOLCCCXXCCCCOHH.",
        "..OLLCCCXXCCCCCO..",
        "..OLCCCCXXCCCCCO..",
        ".OLCCCCCXXCCCCCCO.",
        ".OOOOOOOOOOOOOOOO.",
    };
    static readonly string[] FrontStand = { "....OBBO..OBBO....", "....OOOO..OOOO...." };
    static readonly string[] FrontStepL = { "....OBBO..OOOO....", "....OOOO.........." };
    static readonly string[] FrontStepR = { "....OOOO..OBBO....", "..........OOOO...." };

    // ---------------- UP (back) ----------------
    static readonly string[] UpHead = {
        "......HHHHHH......",
        "....HHWWWWWWHH....",
        "...HWWWWWWWWWWH...",
        "..HWWWWWWWWWWWWH..",
        "..HWWWSWWWSWWWWH..",
        ".HWWWSWWWWSWWWWWH.",
        ".HWWSWWWSWWWSWWWH.",
        "HWWWSWWWSWWWSWWWWH",
        "HWWSWWWWSWWWWSWWWH",
        "HWWSWWWSWWWSWWSWWH",
        "HWSWWSWWWSWWWSWSWH",
    };
    static readonly string[] UpBody = {
        "HWSWSXXXXXXXXSWSWH",
        "HWSWOXXXYZXXXOWSWH",
        ".HSWOXXXYZXXXXOWSH",
        ".HHOXXXXGMXXXXXOH.",
        "..OXXXXXGMXXXXXO..",
        "..OXXXXXGMXXXXXO..",
        "..OXXXXXGMXXXXXXO.",
        ".OXXXXXXGMXXXXXXO.",
        ".OOOOOOOGMOOOOOOO.",
    };

    // ---------------- SIDE (drawn facing LEFT; Unity flips for right) ----------------
    static readonly string[] SideHead = {
        ".....HHHHHH.....",
        "...HHWWWWWWHH...",
        "..HWWWWWWWWWWH..",
        ".HWWWWWWWWWWWWH.",
        ".HWWSWWWWWSWWWH.",
        "HWWSWWWWWWWSWWWH",
        "HWSKKKKWWWWWSWWH",
        "OKKKKKKSWWWWWSWH",
        "OKOOKKKSWWWWWWWH",
        "OKEEKKKSWWWWWSWH",
        ".OKKKKKSWWWWSWWH",
    };
    static readonly string[] SideBody = {
        "..OQQCCCCSWWWSWH",
        "..OLQCCCCCSWWWH.",
        "..OLCCCCCCCSWWH.",
        "..OLCCCCCCCCSH..",
        "..OLCCCCCCCCCOH.",
        "..OLCCCCCCCCCCO.",
        ".OLCCCCCCCCCCCO.",
        ".OLCCCCCCCCCCCCO",
        ".OOOOOOOOOOOOOOO",
    };
    static readonly string[] SideStand = { "....OBBO.OBBO...", "....OOOO.OOOO..." };
    static readonly string[] SideStrideA = { "..OBBO....OBBO..", "..OOOO....OOOO.." };
    static readonly string[] SideStrideB = { "...OBBOOBBO.....", "...OOOOOOOO....." };

    enum Dir { Down, Up, Side }

    class Pose
    {
        public Dir dir;
        public string[] legs;
        public int crouch, bob, lean, hairL, hairR, flareL, flareR;
        public Pose(Dir d) { dir = d; }
    }

    static Pose P(Dir d, string[] legs, int crouch = 0, int bob = 0, int lean = 0, int hairL = 0, int hairR = 0, int flareL = 0, int flareR = 0)
    {
        var p = new Pose(d);
        p.legs = legs; p.crouch = crouch; p.bob = bob; p.lean = lean;
        p.hairL = hairL; p.hairR = hairR; p.flareL = flareL; p.flareR = flareR;
        return p;
    }

    public static string Build(string outPath)
    {
        var rows = new List<Pose[]>();
        // Row order = Hero_Sheet slices top->bottom (Unity rect y 256 ... 0).
        rows.Add(new[] { P(Dir.Down, FrontStand), P(Dir.Down, FrontStand, hairL: 1, hairR: 1), P(Dir.Down, FrontStand, hairL: 1, hairR: 1, flareL: 1, flareR: 1), P(Dir.Down, FrontStand, flareL: 1, flareR: 1) });   // Idle_Down
        rows.Add(new[] { P(Dir.Up, FrontStand), P(Dir.Up, FrontStand, hairL: 1, hairR: 1), P(Dir.Up, FrontStand, hairL: 1, hairR: 1, flareL: 1, flareR: 1), P(Dir.Up, FrontStand, flareL: 1, flareR: 1) });             // Idle_Up
        rows.Add(new[] { P(Dir.Side, SideStand), P(Dir.Side, SideStand, hairR: 1), P(Dir.Side, SideStand, hairR: 1, flareR: 1), P(Dir.Side, SideStand, flareR: 1) });                                                   // Idle_Side
        rows.Add(new[] { P(Dir.Down, FrontStepL, flareL: 1), P(Dir.Down, FrontStand, bob: 1), P(Dir.Down, FrontStepR, flareR: 1), P(Dir.Down, FrontStand, bob: 1) });                                                  // Walk_Down
        rows.Add(new[] { P(Dir.Up, FrontStepL, flareL: 1), P(Dir.Up, FrontStand, bob: 1), P(Dir.Up, FrontStepR, flareR: 1), P(Dir.Up, FrontStand, bob: 1) });                                                          // Walk_Up
        rows.Add(new[] { P(Dir.Side, SideStrideA, lean: -2, hairR: 2, flareR: 1), P(Dir.Side, SideStrideB, bob: 1, lean: -2, hairR: 1), P(Dir.Side, SideStrideA, lean: -2, hairR: 2, flareR: 2), P(Dir.Side, SideStrideB, bob: 1, lean: -2, hairR: 1) }); // Walk_Side
        rows.Add(new[] { P(Dir.Down, FrontStand, crouch: 1, flareL: 1, flareR: 1), P(Dir.Down, FrontStand, crouch: 2, hairL: 1, hairR: 1, flareL: 3, flareR: 3), P(Dir.Down, FrontStand, crouch: 1, flareL: 2, flareR: 2) });  // Attack_Down
        rows.Add(new[] { P(Dir.Up, FrontStand, crouch: 1, flareL: 1, flareR: 1), P(Dir.Up, FrontStand, crouch: 2, hairL: 1, hairR: 1, flareL: 3, flareR: 3), P(Dir.Up, FrontStand, crouch: 1, flareL: 2, flareR: 2) });        // Attack_Up
        rows.Add(new[] { P(Dir.Side, SideStand, crouch: 1, lean: -1, flareR: 2), P(Dir.Side, SideStrideA, crouch: 2, lean: -3, hairR: 2, flareL: 1, flareR: 4), P(Dir.Side, SideStand, crouch: 1, lean: -1, hairR: 1, flareR: 2) }); // Attack_Side

        var sheet = new Bitmap(128, 288, PixelFormat.Format32bppArgb);
        var report = new System.Text.StringBuilder();
        for (int r = 0; r < rows.Count; r++)
        {
            for (int c = 0; c < rows[r].Length; c++)
            {
                var g = Compose(rows[r][c]);
                for (int y = 0; y < 32; y++)
                    for (int x = 0; x < 32; x++)
                    {
                        char ch = g[y, x];
                        if (ch == '.') continue;
                        sheet.SetPixel(c * 32 + x, r * 32 + y, Pal[ch]);
                    }
                int minX, minY, maxX, maxY;
                Bounds(g, out minX, out minY, out maxX, out maxY);
                report.AppendFormat("r{0}c{1}:{2}x{3}@bottom{4} ", r, c, maxX - minX + 1, maxY - minY + 1, maxY);
            }
            report.Append("\n");
        }
        sheet.Save(outPath, ImageFormat.Png);
        sheet.Dispose();
        return report.ToString();
    }

    static char[,] Compose(Pose p)
    {
        string[] head = p.dir == Dir.Down ? DownHead : p.dir == Dir.Up ? UpHead : SideHead;
        var body = new List<string>(p.dir == Dir.Down ? DownBody : p.dir == Dir.Up ? UpBody : SideBody);
        for (int i = 0; i < p.crouch; i++) body.RemoveAt(2); // crouch: drop torso rows, head sinks
        int w = head[0].Length;
        foreach (var s in head) Check(s, w); foreach (var s in body) Check(s, w); foreach (var s in p.legs) Check(s, w);

        var g = new char[32, 32];
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) g[y, x] = '.';
        int h = head.Length + body.Count + p.legs.Length;
        int top = 29 - (h - 1) - p.bob; // feet on row 29 (= pixel row 2 from bottom, pivot 0.0625)
        int x0 = (32 - w) / 2;

        int y0 = top;
        for (int i = 0; i < head.Length; i++) Put(g, head[i], x0 + p.lean, y0 + i);
        int bodyStart = y0 + head.Length;
        // Side lean (C17 0:36.45): shoulders follow the head forward by half the lean.
        int shoulderShift = p.dir == Dir.Side && p.lean < 0 ? p.lean / 2 : 0;
        for (int i = 0; i < body.Count; i++) Put(g, body[i], x0 + (i < 3 ? shoulderShift : 0), bodyStart + i);
        int legsStart = bodyStart + body.Count;
        for (int i = 0; i < p.legs.Length; i++) Put(g, p.legs[i], x0, legsStart + i);

        // Hair sway / streaming: widen hair rows outward.
        int hairFrom = p.dir == Dir.Up ? 3 : 5;
        int bodyHairRows = p.dir == Dir.Side ? 4 : 5;
        for (int i = hairFrom; i < head.Length; i++) ExtendHair(g, y0 + i, p.hairL, p.hairR);
        for (int i = 0; i < Math.Min(bodyHairRows, body.Count); i++) ExtendHair(g, bodyStart + i, p.hairL, p.hairR);

        // Coat hem flare on the bottom three body rows.
        int bottom = legsStart - 1;
        Flare(g, bottom, p.flareL, p.flareR, true);
        Flare(g, bottom - 1, p.flareL, p.flareR, false);
        Flare(g, bottom - 2, (p.flareL + 1) / 2, (p.flareR + 1) / 2, false);

        // Sword hilt worn on the back (front/side only; back view has it in the template).
        int hx = x0 + p.lean, hy = y0;
        // C05 0:00.8: the hilt pokes out on the right of the hair at ear height, not above the head.
        if (p.dir == Dir.Down) { Set(g, hx + 17, hy + 4, 'Y'); Set(g, hx + 17, hy + 5, 'Y'); Set(g, hx + 17, hy + 6, 'Z'); }
        if (p.dir == Dir.Side) { Set(g, hx + 16, hy + 3, 'Y'); Set(g, hx + 16, hy + 4, 'Y'); Set(g, hx + 16, hy + 5, 'Z'); }
        return g;
    }

    static bool IsHair(char c) { return c == 'H' || c == 'W' || c == 'S'; }

    static void ExtendHair(char[,] g, int y, int left, int right)
    {
        int minX, maxX;
        if (!RowSpan(g, y, out minX, out maxX)) return;
        if (left > 0 && IsHair(g[y, minX]))
        {
            g[y, minX] = 'W';
            for (int k = 1; k <= left; k++) Set(g, minX - k, y, k == left ? 'H' : 'W');
        }
        if (right > 0 && IsHair(g[y, maxX]))
        {
            g[y, maxX] = 'W';
            for (int k = 1; k <= right; k++) Set(g, maxX + k, y, k == right ? 'H' : 'W');
        }
    }

    static void Flare(char[,] g, int y, int left, int right, bool outlineRow)
    {
        int minX, maxX;
        if (!RowSpan(g, y, out minX, out maxX)) return;
        if (left > 0)
        {
            char edge = g[y, minX];
            for (int k = 0; k < left; k++) Set(g, minX - k, y, outlineRow ? 'O' : 'C');
            Set(g, minX - left, y, outlineRow ? 'O' : edge);
        }
        if (right > 0)
        {
            char edge = g[y, maxX];
            for (int k = 0; k < right; k++) Set(g, maxX + k, y, outlineRow ? 'O' : 'C');
            Set(g, maxX + right, y, outlineRow ? 'O' : edge);
        }
    }

    static bool RowSpan(char[,] g, int y, out int minX, out int maxX)
    {
        minX = 99; maxX = -1;
        if (y < 0 || y > 31) return false;
        for (int x = 0; x < 32; x++) if (g[y, x] != '.') { if (x < minX) minX = x; if (x > maxX) maxX = x; }
        return maxX >= 0;
    }

    static void Bounds(char[,] g, out int minX, out int minY, out int maxX, out int maxY)
    {
        minX = minY = 99; maxX = maxY = -1;
        for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) if (g[y, x] != '.')
                { if (x < minX) minX = x; if (x > maxX) maxX = x; if (y < minY) minY = y; if (y > maxY) maxY = y; }
    }

    static void Put(char[,] g, string row, int x, int y)
    {
        for (int i = 0; i < row.Length; i++) if (row[i] != '.') Set(g, x + i, y, row[i]);
    }

    static void Set(char[,] g, int x, int y, char c)
    {
        if (x < 0 || y < 0 || x > 31 || y > 31) throw new Exception("Pixel out of cell at " + x + "," + y);
        g[y, x] = c;
    }

    static void Check(string s, int w)
    {
        if (s.Length != w) throw new Exception("Template row width " + s.Length + " != " + w + ": " + s);
        foreach (char c in s) if (c != '.' && !Pal.ContainsKey(c)) throw new Exception("Unknown palette char " + c);
    }

    static Color Hex(string h)
    {
        return Color.FromArgb(255, Convert.ToInt32(h.Substring(0, 2), 16), Convert.ToInt32(h.Substring(2, 2), 16), Convert.ToInt32(h.Substring(4, 2), 16));
    }
}
