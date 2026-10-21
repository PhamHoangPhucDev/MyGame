using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

// Boss — CANDIDATE v4: Cyclops. Pale olive-grey giant, now heavy and muscular: broad shoulders, pot belly, thick arms
// with big fists, heavy legs, big bare feet; small bald head with ONE eye in the middle (no nose), pointed ears;
// one-shoulder leather tunic, square-buckle belt, ragged hem; plain caveman club (thin handle, big rounded head).
// Two faces: calm (heavy-lidded eye, closed mouth) before it is hit, angry (V brows, red-glowing pupil, open mouth
// with teeth) once aggroed. Side rows are a 3/4 turn (one eye toward the front).
// 64x64 cell, 30x33 grid at x 17..46, y 25..57 (sole row 57). Original pixels (not traced).
public static class BossGen
{
    public static readonly Dictionary<char, string> Pal = new Dictionary<char, string>
    {
        { 'k', "2A1420" },
        { 'h', "F0E8C8" }, { 'S', "D8CCA0" }, { 's', "B0A478" }, { 'z', "86805A" },     // pale olive skin: light / mid / shade / dark
        { 'e', "F4F0E4" }, { 'I', "8A5A30" }, { 'R', "E83A2A" }, { 'm', "5A2A28" }, { 'W', "E8E0C8" },  // sclera, iris, angry glow, mouth, teeth
        { 'C', "7A5A34" }, { 'c', "9A7444" }, { 'L', "4E3220" }, { 'y', "A08050" },     // tunic, tunic light, belt, buckle
    };

    // ---------- heads (24 wide, padded to 30) ----------
    static readonly string[] HeadDownCalm =
    {
        ".........kkkkkk.........", ".......kkhhhhhhkk.......", "......khhSSSSSShhk......", ".....khSSSSSSSSSSsk.....",
        ".....kSSSkkkkkkSSsk.....", "...kSkSSkhhhhhhkSskSk...", "...kSkSSkeIkkIekSskSk...", "....kkSSkeIIIIekSskk....",
        ".....kSSSkeeeekSSsk.....", ".....kzSSSkkkkSSszk.....", ".....kzSSSSmmSSSszk.....", "......kzzSSSSSSzzk......",
        "...kkkkkzzSSSSzzkkkkk...",
    };
    static readonly string[] HeadDownAngry =
    {
        ".........kkkkkk.........", ".......kkhhhhhhkk.......", "......khhSSSSSShhk......", ".....khSSSSSSSSSSsk.....",
        ".....kSSkkSSSSkkSsk.....", "...kSkSSkkeeeekkSskSk...", "...kSkSSkeIRRIekSskSk...", "....kkSSkeIIIIekSskk....",
        ".....kSSSkeeeekSSsk.....", ".....kzSkmmmmmmkSzk.....", ".....kzSkWmWWmWkSzk.....", "......kzzSSSSSSzzk......",
        "...kkkkkzzSSSSzzkkkkk...",
    };
    static readonly string[] HeadUp =
    {
        ".........kkkkkk.........", ".......kkhhhhhhkk.......", "......khhSSSSSShhk......", ".....khSSSSSSSSSSsk.....",
        ".....kSSSSSSSSSSSsk.....", "...kSkSSSSSSSSSSsskSk...", "...kSkhSSSSSSSSSsskSk...", "....kkSSSSSSSSSssskk....",
        ".....kSSSSSSSSSsssk.....", ".....kzSSSSSSSssszk.....", ".....kzzSSSSSsssszk.....", "......kzzSSSSSSzzk......",
        "...kkkkkzzSSSSzzkkkkk...",
    };
    static readonly string[] HeadSideCalm =
    {
        "..........kkkkkk........", "........kkhhhhhhkk......", ".......khhSSSSSShhk.....", "......khSSSSSSSSSSsk....",
        "......kSSSSSkkkkkkSk....", "....kSkSSSSkhhhhhhkSk...", "....kSkSSSSkeIkkIekSk...", ".....kkSSSSkeIIIIekSk...",
        "......kSSSSSkeeeekSSk...", "......kzSSSSSkkkkSSSk...", "......kzSSSSSSSSSmmSk...", ".......kzzSSSSSSzzkk....",
        "....kkkkkzzSSSSzzkkkk...",
    };
    static readonly string[] HeadSideAngry =
    {
        "..........kkkkkk........", "........kkhhhhhhkk......", ".......khhSSSSSShhk.....", "......khSSSSSSSSSSsk....",
        "......kSSSSkkSSSSkkk....", "....kSkSSSSkkeeeekkSk...", "....kSkSSSSkeIRRIekSk...", ".....kkSSSSkeIIIIekSk...",
        "......kSSSSSkeeeekSSk...", "......kzSSSSSkkkkSSSk...", "......kzSSSSSSkmWmWmk...", ".......kzzSSSSSSzzkk....",
        "....kkkkkzzSSSSzzkkkk...",
    };

    // ---------- body (rows 13-26): arm 7 + torso 16 + arm 7 ----------
    static readonly string[] ArmL =
    {
        ".khSSSk", "khSSSSk", "kSSSSSk", "kSSSSsk", "kSSSssk", "kSSSssk", "kSSsssk", "kSSsssk", "kSSSssk", "kSSSSsk", "kSSSSSk",
        "kzSSSzk", ".kzzzk.", "..kkk..",
    };
    static readonly string[] ArmR = ArmL.Select(Rev).ToArray();
    static readonly string[] TorsoDown =
    {
        "hSSSSSSSSSSCCCCc", "SSShSSSSSSCCCCCc", "SShSSSSSSCCCCCCc", "SSSSSzSSCCCCCcCc", "SSzSSSSCCCCCCcCc", "SSSSzSCCCCCcCCCc",
        "SSSSSCCCCCcCCCCc", "LLLLLLyyyyLLLLLL", "LLLLLLykkyLLLLLL", "CCCcCCCCCCCCcCCC", "CCCcCCCCcCCCcCCC", "CcCCCcCCCCcCCCcC",
        "kCCkCkCCkCCkCkCk", ".kSSSSk..kSSSSk.",
    };
    static readonly string[] TorsoUp = TorsoDown.Take(7).Select(Rev)
        .Concat(new[] { "LLLLLLLLLLLLLLLL", "LLLLLLLLLLLLLLLL" }).Concat(TorsoDown.Skip(9)).ToArray();
    static readonly string[] LegsDown =
    {
        "........kSSSSk..kSSSSk........", "........kSSSsk..ksSSSk........", "........kSSSsk..ksSSSk........",
        ".......kSSSSSk..kSSSSSk.......", "......kSSSSSSk..kSSSSSSk......", "......kkkkkkkk..kkkkkkkk......",
    };
    // side: back arm 7 + torso 12 + front arm 7 (with 3 leading / 1 trailing pad)
    static readonly string[] TorsoSide =
    {
        "SSSSSSSCCCCc", "SSSSSSCCCCCc", "SShSSCCCCCCc", "SSSSCCCCCcCc", "SzSCCCCCCcCc", "SSCCCCCcCCCc", "SCCCCCcCCCCc",
        "LLLLLLLyyLLL", "LLLLLLLyyLLL", "CCcCCCCCcCCC", "CCCcCCCCCcCC", "CcCCCcCCCCcC", "kCCkCkCCkCCk", "kSSSSkkSSSSk",
    };
    static readonly string[] LegsSide =
    {
        "..........kSSSSkkSSSSk........", "..........kSSSskkSSSsk........", "..........kSSSskkSSSsk........",
        "..........kSSSSSkkSSSSSk......", "..........kSSSSSSkkSSSSSSk....", "..........kkkkkkkkkkkkkkkk....",
    };

    static string Rev(string s) { var a = s.ToCharArray(); Array.Reverse(a); return new string(a); }
    static string Pad(string s) { return "..." + s + "..."; }

    static string[] BuildFront(string[] head, string[] torso, string[] legs)
    {
        var o = head.Select(Pad).ToList();
        o[12] = "...kkkkkkkkzzSSSSzzkkkkkkkk...";   // broad shoulders
        for (int i = 0; i < torso.Length; i++) o.Add(ArmL[i] + torso[i] + ArmR[i]);
        o.AddRange(legs);
        return o.ToArray();
    }
    static string[] BuildSide(string[] head)
    {
        var o = head.Select(Pad).ToList();
        o[12] = "....kkkkkkkkzzSSSSzzkkkkkk....";
        for (int i = 0; i < TorsoSide.Length; i++) o.Add("..." + ArmL[i] + TorsoSide[i] + ArmR[i] + ".");
        o.AddRange(LegsSide);
        return o.ToArray();
    }

    static readonly string[] DownCalm = BuildFront(HeadDownCalm, TorsoDown, LegsDown);
    static readonly string[] DownAngry = BuildFront(HeadDownAngry, TorsoDown, LegsDown);
    static readonly string[] UpAll = BuildFront(HeadUp, TorsoUp, LegsDown);
    static readonly string[] SideCalm = BuildSide(HeadSideCalm);
    static readonly string[] SideAngry = BuildSide(HeadSideAngry);

    public static IEnumerable<string[]> AllGrids() { return new[] { DownCalm, DownAngry, UpAll, SideCalm, SideAngry }; }

    const int X0 = 17, Y0 = 25, Planted = 27;   // rows >= Planted (legs) stay put when the body bobs

    static void Paint(Bitmap bmp, string[] g, int ox, int oy)
    {
        for (int y = 0; y < g.Length; y++) for (int x = 0; x < g[y].Length; x++)
            {
                char c = g[y][x]; if (c == '.') continue;
                int X = ox + x, Y = oy + y; if (X < 1 || Y < 1 || X >= bmp.Width - 1 || Y >= bmp.Height - 1) continue;
                bmp.SetPixel(X, Y, HeroConceptGen.Hex(Pal[c]));
            }
    }

    static void PaintBob(Bitmap bmp, string[] g, int ox, int y0, int dy)
    {
        Paint(bmp, g.Skip(Planted).ToArray(), ox, y0 + Planted);
        Paint(bmp, g.Take(Planted).ToArray(), ox, y0 + dy);
    }

    // plain caveman club: thin 2-px handle for the first ~40%, then a fat rounded head swelling to radius 3.5
    static void Club(Bitmap bmp, int x0, int y0, int x1, int y1, double scale = 1)
    {
        var s = new EquipGen.Spr();
        double len = Math.Sqrt((x1 - x0) * (x1 - x0) + (y1 - y0) * (y1 - y0)); if (len < 1) len = 1;
        double ux = (x1 - x0) / len, uy = (y1 - y0) / len, px = -uy, py = ux;
        var lite = HeroConceptGen.Hex("B07A48"); var mid = HeroConceptGen.Hex("8A5A34"); var dark = HeroConceptGen.Hex("5E3A22");
        int steps = (int)Math.Ceiling(len * 2); int reach = (int)Math.Ceiling(5 * scale);
        for (int i = 0; i <= steps; i++)
        {
            double t = i / (double)steps; double cx = x0 + (x1 - x0) * t, cy = y0 + (y1 - y0) * t;
            double r = scale * (t < 0.4 ? 0.6 : t < 0.82 ? 0.6 + (t - 0.4) / 0.42 * 3.2 : 3.8 - Math.Max(0, (t - 0.9) / 0.1) * 1.8);
            for (int dy = -reach; dy <= reach; dy++) for (int dx = -reach; dx <= reach; dx++)
                {
                    double ax = dx * px + dy * py, along = dx * ux + dy * uy;
                    if (Math.Abs(along) > 0.6 || Math.Abs(ax) > r + 0.4) continue;
                    s.Set((int)Math.Round(cx + dx), (int)Math.Round(cy + dy), ax < -r * 0.3 ? lite : ax > r * 0.45 ? dark : mid);
                }
        }
        foreach (var t in new[] { 0.7, 0.86 })
        {
            double cx = x0 + (x1 - x0) * t, cy = y0 + (y1 - y0) * t;
            s.Set((int)Math.Round(cx + px * scale), (int)Math.Round(cy + py * scale), dark);
            if (scale > 1) s.Set((int)Math.Round(cx + px * scale + ux), (int)Math.Round(cy + py * scale + uy), dark);
        }
        var ol = EquipGen.Outline(s, HeroConceptGen.Hex(Pal['k']));
        foreach (var kv in ol.Px) if (kv.Key.X >= 1 && kv.Key.Y >= 1 && kv.Key.X < bmp.Width - 1 && kv.Key.Y < bmp.Height - 1) bmp.SetPixel(kv.Key.X, kv.Key.Y, kv.Value);
    }

    // pose: 0 rest (club hanging), 1 windup (club overhead), 2 slam (club smashed down in front). dy: body bob.
    // angry: aggro face (Up has no face). Side faces right (mirror for left).
    // Club fist: Down x+26,y+23 / Up x+3,y+23 / Side x+25,y+23 (grid coords).
    public static Bitmap Cyclops(string v, int pose, bool mirror, int dy = 0, bool angry = false)
    {
        var bmp = new Bitmap(64, 64, PixelFormat.Format32bppArgb);
        int ox = X0, oy = Y0 + dy;
        if (v == "Down")
        {
            if (pose == 1) Club(bmp, ox + 26, oy + 22, ox + 22, oy + 0);   // raised behind the head
            PaintBob(bmp, angry ? DownAngry : DownCalm, ox, oy - dy, dy);
            if (pose == 0) Club(bmp, ox + 26, oy + 23, ox + 27, oy + 30);
            else if (pose == 2) Club(bmp, ox + 25, oy + 23, ox + 16, oy + 33);
        }
        else if (v == "Up")
        {
            PaintBob(bmp, UpAll, ox, oy - dy, dy);
            if (pose == 0) Club(bmp, ox + 3, oy + 23, ox + 2, oy + 30);
            else if (pose == 1) Club(bmp, ox + 3, oy + 21, ox + 0, oy + 1);    // raised up beside the head
            else Club(bmp, ox + 4, oy + 21, ox + 10, oy - 3);
        }
        else
        {
            if (pose == 1) Club(bmp, ox + 24, oy + 22, ox + 13, oy + 0);
            PaintBob(bmp, angry ? SideAngry : SideCalm, ox, oy - dy, dy);
            if (pose == 0) Club(bmp, ox + 25, oy + 23, ox + 28, oy + 30);
            else if (pose == 2) Club(bmp, ox + 25, oy + 23, ox + 37, oy + 32);
        }
        if (!mirror) return bmp;
        var o = new Bitmap(64, 64, PixelFormat.Format32bppArgb);
        for (int y = 0; y < 64; y++) for (int x = 0; x < 64; x++) o.SetPixel(63 - x, y, bmp.GetPixel(x, y));
        return o;
    }

    // ---------- boss size: 2x enlargement at the same pixel density as the heroes ----------
    // Scale2x (EPX) on the char grid keeps curves smooth, then the doubled silhouette outline is thinned back to 1 px.
    static string[] Scale2x(string[] g)
    {
        int h = g.Length, w = g[0].Length;
        Func<int, int, char> at = (x, y) => x < 0 || y < 0 || x >= w || y >= h ? '.' : g[y][x];
        var o = new char[h * 2][]; for (int i = 0; i < h * 2; i++) o[i] = new char[w * 2];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                char P = g[y][x], A = at(x, y - 1), B = at(x + 1, y), C = at(x - 1, y), D = at(x, y + 1);
                char e1 = P, e2 = P, e3 = P, e4 = P;
                if (C == A && C != D && A != B) e1 = A;
                if (A == B && A != C && B != D) e2 = B;
                if (D == C && D != B && C != A) e3 = C;
                if (B == D && B != A && D != C) e4 = D;
                o[2 * y][2 * x] = e1; o[2 * y][2 * x + 1] = e2; o[2 * y + 1][2 * x] = e3; o[2 * y + 1][2 * x + 1] = e4;
            }
        return o.Select(r => new string(r)).ToArray();
    }

    static string[] ThinOutline(string[] g)
    {
        int h = g.Length, w = g[0].Length;
        Func<int, int, char> at = (x, y) => x < 0 || y < 0 || x >= w || y >= h ? '.' : g[y][x];
        var outer = new bool[h, w];
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
                outer[y, x] = g[y][x] == 'k' && (at(x - 1, y) == '.' || at(x + 1, y) == '.' || at(x, y - 1) == '.' || at(x, y + 1) == '.');
        var o = g.Select(r => r.ToCharArray()).ToArray();
        int[] dx4 = { -1, 1, 0, 0 }, dy4 = { 0, 0, -1, 1 };
        for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                if (g[y][x] != 'k' || outer[y, x]) continue;
                bool nearDot = false, nearOuter = false; char fill = '\0';
                for (int yy = -1; yy <= 1; yy++) for (int xx = -1; xx <= 1; xx++) if (at(x + xx, y + yy) == '.') nearDot = true;
                if (nearDot) continue;
                for (int i = 0; i < 4; i++)
                {
                    int X = x + dx4[i], Y = y + dy4[i]; if (X < 0 || Y < 0 || X >= w || Y >= h) continue;
                    if (outer[Y, X]) nearOuter = true;
                    char c = g[Y][X]; if (c != 'k' && c != '.' && fill == '\0') fill = c;
                }
                if (nearOuter && fill != '\0') o[y][x] = fill;
            }
        return o.Select(r => new string(r)).ToArray();
    }

    // walk: lift the legs inside columns c0..c1 by 1 px (1x grid), sole row cleared
    static string[] LiftCols(string[] g, int c0, int c1)
    {
        var rows = g.Select(s => s.ToCharArray()).ToArray(); int h = rows.Length;
        for (int c = c0; c <= c1; c++)
        {
            for (int r = h - 6; r < h - 1; r++) rows[r][c] = rows[r + 1][c];
            rows[h - 1][c] = '.';
        }
        return rows.Select(r => new string(r)).ToArray();
    }

    static readonly Dictionary<string, string[]> BigCache = new Dictionary<string, string[]>();
    static string[] Big(string[] g, string key)
    {
        string[] r; if (BigCache.TryGetValue(key, out r)) return r;
        r = ThinOutline(Scale2x(g)); BigCache[key] = r; return r;
    }

    public const int BigCell = 96, BigSole = 89;
    const int BX0 = 18, BY0 = 24;   // 60x66 big grid at x 18..77, y 24..89 inside the 96x96 cell

    // Boss at full size: 96x96 cell, sole row 89. pose/angry as Cyclops(); dy: bob (big px); leg: walk lift
    // (-1 none, 0 left/back leg, 1 right/front leg). Club fist (big px): Down 18+53,24+47 / Up 18+7 / Side 18+51.
    public static Bitmap CyclopsBig(string v, int pose, bool mirror, int dy = 0, bool angry = false, int leg = -1)
    {
        var bmp = new Bitmap(BigCell, BigCell, PixelFormat.Format32bppArgb);
        string[] g1 = v == "Down" ? (angry ? DownAngry : DownCalm) : v == "Up" ? UpAll : (angry ? SideAngry : SideCalm);
        string key = v + (angry ? "A" : "C") + leg;
        if (leg >= 0)
        {
            bool side = v == "Side";
            int c0 = side ? (leg == 0 ? 0 : 17) : (leg == 0 ? 0 : 15), c1 = side ? (leg == 0 ? 16 : 29) : (leg == 0 ? 14 : 29);
            g1 = LiftCols(g1, c0, c1);
        }
        var g = Big(g1, key);
        int ox = BX0, oy = BY0;
        Func<double, int> X = gx => ox + (int)Math.Round(gx * 2);
        Func<double, int> Y = gy => oy + dy + (int)Math.Round(gy * 2);
        const double S2 = 2;
        if (v == "Down")
        {
            if (pose == 1) Club(bmp, X(26.5), Y(22), X(22), Y(0), S2);
            PaintBob(bmp, g, ox, oy, dy, Planted * 2);
            if (pose == 0) Club(bmp, X(26.5), Y(23.5), X(27.5), Y(30), S2);
            else if (pose == 2) Club(bmp, X(25.5), Y(23.5), X(16), Y(33), S2);
        }
        else if (v == "Up")
        {
            PaintBob(bmp, g, ox, oy, dy, Planted * 2);
            if (pose == 0) Club(bmp, X(3.5), Y(23.5), X(2.5), Y(30), S2);
            else if (pose == 1) Club(bmp, X(3.5), Y(21), X(0.5), Y(1), S2);
            else Club(bmp, X(4.5), Y(21), X(10), Y(-3), S2);
        }
        else
        {
            if (pose == 1) Club(bmp, X(24.5), Y(22), X(13), Y(0), S2);
            PaintBob(bmp, g, ox, oy, dy, Planted * 2);
            if (pose == 0) Club(bmp, X(25.5), Y(23.5), X(28.5), Y(30), S2);
            else if (pose == 2) Club(bmp, X(25.5), Y(23.5), X(37), Y(32), S2);
        }
        if (!mirror) return bmp;
        var o = new Bitmap(BigCell, BigCell, PixelFormat.Format32bppArgb);
        for (int y = 0; y < BigCell; y++) for (int x = 0; x < BigCell; x++) o.SetPixel(BigCell - 1 - x, y, bmp.GetPixel(x, y));
        return o;
    }

    static void PaintBob(Bitmap bmp, string[] g, int ox, int y0, int dy, int planted)
    {
        Paint(bmp, g.Skip(planted).ToArray(), ox, y0 + planted);
        Paint(bmp, g.Take(planted).ToArray(), ox, y0 + dy);
    }

    // ---------- AoE telegraph (ground layer, drawn under every unit) ----------
    // 128x128 per frame, base radius 62 px (3.875 units at PPU 16). Frames 0-3: red ring + translucent fill growing from
    // the centre (25/50/75/100% radius). Frame 4: impact flash (fill brightens), frame 5: fade.
    public static Bitmap Telegraph(int f)
    {
        const int N = 128; double c = (N - 1) / 2.0, R = 62;
        var bmp = new Bitmap(N, N, PixelFormat.Format32bppArgb);
        double fillR = f <= 3 ? R * (f + 1) / 4.0 : R;
        int fillA = f <= 3 ? 70 : f == 4 ? 150 : 40, ringA = f == 5 ? 110 : 255;
        var red = Color.FromArgb(255, 0xD8, 0x3A, 0x3A); var hot = Color.FromArgb(255, 0xFF, 0x8A, 0x6A);
        for (int y = 0; y < N; y++) for (int x = 0; x < N; x++)
            {
                double d = Math.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                if (d > R + 0.5) continue;
                if (d >= R - 1.5) { bmp.SetPixel(x, y, Color.FromArgb(ringA, red)); continue; }                // 2-px ring
                if (f <= 3 && d >= R - 4.5 && d < R - 3.5) { bmp.SetPixel(x, y, Color.FromArgb(90, red)); continue; } // faint inner guide
                if (d <= fillR)
                {
                    bool edge = f <= 3 && d >= fillR - 1;                                                        // leading edge
                    bmp.SetPixel(x, y, Color.FromArgb(edge ? Math.Min(255, fillA + 80) : fillA, f == 4 ? hot : red));
                }
            }
        return bmp;
    }

    // ground crack decal left by the slam: 96x48, dark cracks + chipped rocks, pivot centre
    public static Bitmap Crack()
    {
        var bmp = new Bitmap(96, 48, PixelFormat.Format32bppArgb);
        var dark = HeroConceptGen.Hex("3A2420"); var mid = HeroConceptGen.Hex("6A4A3A"); var lit = HeroConceptGen.Hex("A07A60");
        var rnd = new Random(7);
        // radial cracks from the centre, squashed vertically (top-down ground)
        for (int a = 0; a < 9; a++)
        {
            double ang = a * Math.PI * 2 / 9 + rnd.NextDouble() * 0.4; double x = 48, y = 24;
            int len = 15 + rnd.Next(21);
            for (int i = 0; i < len; i++)
            {
                ang += (rnd.NextDouble() - 0.5) * 0.5;
                x += Math.Cos(ang); y += Math.Sin(ang) * 0.5;
                int X = (int)Math.Round(x), Y = (int)Math.Round(y);
                if (X < 1 || Y < 1 || X > 94 || Y > 46) break;
                bmp.SetPixel(X, Y, i < len / 2 ? dark : mid);
                if (i < 5 && Y + 1 < 47) bmp.SetPixel(X, Y + 1, dark);
            }
        }
        // centre crater + chips
        for (int y = 19; y <= 29; y++) for (int x = 38; x <= 58; x++)
            {
                double d = Math.Sqrt((x - 48) * (x - 48) / 90.0 + (y - 24) * (y - 24) / 22.0);
                if (d <= 1) bmp.SetPixel(x, y, d > 0.75 ? mid : dark);
            }
        foreach (var p in new[] { new Point(27, 15), new Point(69, 31), new Point(60, 12), new Point(33, 35), new Point(78, 20), new Point(16, 26) })
        {
            bmp.SetPixel(p.X, p.Y, lit); bmp.SetPixel(p.X + 1, p.Y, mid); bmp.SetPixel(p.X, p.Y + 1, dark); bmp.SetPixel(p.X + 1, p.Y + 1, dark);
        }
        return bmp;
    }
}
