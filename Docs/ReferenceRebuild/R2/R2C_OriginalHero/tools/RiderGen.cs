using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

// Goblin riders — CANDIDATE (follows the reference video): hooded goblin with a tall floppy hood tip riding a stout
// pink-beige boar. Gobrider / Elite Gobrider in a 48x48 cell (sole on row 41), Warlord boss in 64x64 (sole on row 57).
// Rider + mount are separate grids composited per view. Original pixels (not traced).
public static class RiderGen
{
    public static readonly Dictionary<char, string> Pal = new Dictionary<char, string>
    {
        { 'k', "2A1420" },
        { 'M', "6A2A3A" }, { 'h', "8A3E4C" }, { 'm', "4A1A2A" }, { 'r', "D83A3A" },   // hood + red pom
        { 'G', "8CD064" }, { 'g', "A8E07A" }, { 'D', "5E9A44" }, { 'e', "F0B040" }, { 'E', "D0661E" },
        { 't', "3E7A3A" }, { 'T', "2E5A2C" },
        { 'P', "4E4858" }, { 'p', "6E6880" }, { 'Q', "2E2A38" }, { 'S', "3C3646" }, { 's', "E8402C" }, // mount: black wild boar, red eye
        { 'N', "1C1824" }, { 'n', "120E16" },                                                         // boar bristle mane + hooves
        { 'w', "FFF6E0" }, { 'B', "3A2420" },                                                         // tusk, hoof
        { 'R', "C83A3A" }, { 'c', "E8DCC0" }, { 'W', "F0E8D8" }, { 'v', "2A1420" }, { 'y', "E8C048" },  // warlord extras
    };

    // Elite: crimson-black hood with gold trim
    public static readonly Dictionary<char, string> ElitePal = MakeElite();
    static Dictionary<char, string> MakeElite()
    {
        var d = new Dictionary<char, string>(Pal);
        d['M'] = "4A1420"; d['h'] = "C8962E"; d['m'] = "2E0C14"; d['r'] = "E8C048";
        // elite rides the same black boar (iron mask added in Compose)
        return d;
    }

    // ---------- Gobrider (v3: bigger rider, big floppy hood tip, hunched splay-legged mount, maroon bands) ----------
    static readonly string[] RiderDown =
    {
        "..........kk..", ".........kMrk.", "........kMMk..", ".......kMMk...", "....kkkMMk....", "...kMMhhhMk...", "..kMhhhhhhMk..",
        ".kMMGggggGMMk.", ".kMGgGGGGgGMk.", "kgMGeGGGGeGMgk", "kGMGEGGGGEGMGk", ".kMDGGGGGGDMk.", "..kkDttttDkk..", "...kttTTttk...",
    };
    static readonly string[] RiderUp =
    {
        "..........kk..", ".........kMrk.", "........kMMk..", ".......kMMk...", "....kkkMMk....", "...kMMhhhMk...", "..kMhhhhhhMk..",
        ".kMMMhhhhMMMk.", ".kMMMMMMMMMMk.", "kgMMMMMMMMMMgk", "kGMMMMmmMMMMGk", ".kMMmmmmmmMMk.", "..kkmmmmmmkk..", "...kttTTttk...",
    };
    static readonly string[] RiderSide =
    {
        "kk............", "kMrk..........", ".kMMk.........", "..kMMk........", "...kMMkkk.....", "...kMhhhMk....", "..kMhhhhhMk...",
        ".kMMMGggggGk..", ".kMMGgGGGGGk..", "kgMGGGGGeGGGgk", "kGMGGGGGEGGGGk", ".kMDGGGGGGGk..", "..kkDttttk....", "...kttTTk.....",
    };
    // v7 mount: black wild boar - bristle mane ridge + dark patches (N/n), slate lit top / shaded flanks, red eyes,
    // ivory tusks, head low between the forelegs, dark hooves. Rider covers the top 2 rows.
    static readonly string[] BoarDown =
    {
        "...kk.kkkk.kk...", "..kNNkNNNNkNNk..", ".kSpPNnNNnNPpSk.", "kSpPPPNNNNPPPpSk", "kSPPpPPPPPPpPPSk",
        "kQSPPPNPPNPPPSQk", "kQSSPkkPPkkPSSQk", "kNQSkNsPPsNkSQNk", "kNQQwPPSSPPwQQNk", ".kNQkwSQQSwkQNk.",
        "kQSk.kkNNkk.kSQk", "kQQk........kQQk", "knnk........knnk",
    };
    static readonly string[] BoarUp =
    {
        "...kk.kkkk.kk...", "..kNNkNNNNkNNk..", ".kSpPNnNNnNPpSk.", "kSpPPPNNNNPPPpSk", "kSPPpPPNNPPpPPSk",
        "kQSPPPNnnNPPPSQk", "kQSSPPPNNPPPSSQk", "kNQSSPPNNPPSSQNk", "kNQQSSPkkPSSQQNk", ".kNQkkkNnkkkQNk.",
        "kQSk..knnk..kSQk", "kQQk........kQQk", "knnk........knnk",
    };
    static readonly string[] BoarSide =
    {
        // v6 side: long level back + mane, distinct head out front (ear, eye, snout, tusk), legs under hip and chest
        "..kkkkkkkkkk........", ".kNNnNNnNNNNk..kk...", "kNpPPPPPPPPNk.kNSk..", "kSpPPPPPPPPPNkpPPPk.", "kSPPPPPPPPPPPSPPsPk.",
        "kQSPPNPPPPNPPSPPPPSk", "kQSSPPPPPPPPPSPPSSQk", "kNQSSPPNPPPSSQkSwkk.", ".kNQQSSSSSSQQkkwQQk.", ".kQSk.....kSQkkkk...",
        "kQSk......kSQk......", "kQQk......kQQk......", "knnk......knnk......",
    };

    // ---------- Warlord (boss) ----------
    static readonly string[] WarRiderDown =
    {
        "k..............k", "kc............ck", ".kc..........ck.", ".kcc.kkkkkk.cck.", "..kcckWWWWkcck..", "...kkWWWWWWkk...",
        "....kWvWWvWk....", "...kMkWWWWkMk...", "..kMMGkkkkGMMk..", ".kgMGeGGGGeGMgk.", ".kGMGEGGGGEGMGk.", "..kMDGGkkGGDMk..",
        ".kRRkttttttkRRk.", ".kRtttyTTytttRk.", "..kttTTTTTTttk..", "..kTTTTTTTTTTk..",
    };
    static readonly string[] WarRiderUp =
    {
        "k..............k", "kc............ck", ".kc..........ck.", ".kcc.kkkkkk.cck.", "..kcckWWWWkcck..", "...kkWWWWWWkk...",
        "....kWWWWWWk....", "...kMkWWWWkMk...", "..kMMMkkkkMMMk..", ".kgMMMMMMMMMMgk.", ".kGMMMMMMMMMMGk.", "..kMMMMMMMMMMk..",
        ".kRRRRRRRRRRRRk.", ".kRRRRmmmmRRRRk.", "..kRRmmmmmmRRk..", "..kRRRRRRRRRRk..",
    };
    static readonly string[] WarRiderSide =
    {
        "k.........k.....", "kc.......ck.....", ".kc.kkkkcck.....", ".kcckWWWWck.....", "..kkWWWWWWk.....", "...kWWWvWWWk....",
        "...kMkWWWWkk....", "..kMMMGGGGGk....", ".kgMMGGGGeGGgk..", ".kGMMGGGGEGGGk..", "..kMMDGGGkkGk...", ".kRRkttttttk....",
        ".kRRtttyttTk....", "..kRRtTTTTk.....", "..kRkTk.kTk.....", "...k.kk..kk.....",
    };
    // war mount (v5): the Gobrider beast grown (extra rows/cols), red war paint on the spots, ivory tusks
    static readonly string[] WarBoarDown = Grow(Mark(BoarDown, 'R', 5, 6, 5, 9, 4, 4, 4, 11), new[] { 2, 7, 8, 13 }, new[] { 4, 5 });
    static readonly string[] WarBoarUp = Grow(Mark(BoarUp, 'R', 6, 4, 6, 11, 4, 4, 4, 11), new[] { 2, 7, 8, 13 }, new[] { 4, 5 });
    static readonly string[] WarBoarSide = Grow(Mark(BoarSide, 'R', 5, 5, 5, 10, 7, 7, 4, 4), new[] { 2, 6, 9, 12 }, new[] { 4, 7 });

    // Mark(grid, ch, r, c, r, c, ..., ch2, r, c, ...): set cells; a char argument switches the paint char
    static string[] Mark(string[] g, params object[] a)
    {
        var rows = g.Select(s => s.ToCharArray()).ToArray(); char ch = 'R';
        for (int i = 0; i < a.Length; )
        {
            if (a[i] is char) { ch = (char)a[i]; i++; continue; }
            rows[(int)a[i]][(int)a[i + 1]] = ch; i += 2;
        }
        return rows.Select(r => new string(r)).ToArray();
    }
    // duplicate the listed columns and rows once each (source indices)
    static string[] Grow(string[] g, int[] cols, int[] rows)
    {
        var o = new List<string>();
        for (int y = 0; y < g.Length; y++)
        {
            var sb = new System.Text.StringBuilder();
            for (int x = 0; x < g[y].Length; x++) { sb.Append(g[y][x]); if (cols.Contains(x)) sb.Append(g[y][x]); }
            o.Add(sb.ToString()); if (rows.Contains(y)) o.Add(sb.ToString());
        }
        return o.ToArray();
    }

    public static IEnumerable<string[]> AllGrids()
    {
        return new[] { RiderDown, RiderUp, RiderSide, BoarDown, BoarUp, BoarSide, WarRiderDown, WarRiderUp, WarRiderSide, WarBoarDown, WarBoarUp, WarBoarSide };
    }

    static void Paint(Bitmap bmp, string[] g, Dictionary<char, string> pal, int ox, int oy)
    {
        for (int y = 0; y < g.Length; y++) for (int x = 0; x < g[y].Length; x++)
            {
                char c = g[y][x]; if (c == '.') continue;
                int X = ox + x, Y = oy + y; if (X < 1 || Y < 1 || X >= bmp.Width - 1 || Y >= bmp.Height - 1) continue;
                bmp.SetPixel(X, Y, HeroConceptGen.Hex(pal[c]));
            }
    }

    // bone club: 2-px cream shaft with knobbly ends, outlined
    static void Bone(Bitmap bmp, int x0, int y0, int x1, int y1)
    {
        var s = new EquipGen.Spr();
        int n = Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0));
        for (int i = 0; i <= n; i++)
        {
            int x = (int)Math.Round(x0 + (x1 - x0) * (double)i / n), y = (int)Math.Round(y0 + (y1 - y0) * (double)i / n);
            s.Set(x, y, HeroConceptGen.Hex("F0E8D8")); s.Set(x + 1, y, HeroConceptGen.Hex("C8B898"));
        }
        foreach (var e in new[] { new Point(x0, y0), new Point(x1, y1) })
            foreach (var o in new[] { new Point(-1, -1), new Point(1, -1), new Point(-1, 1), new Point(2, 1), new Point(0, -1), new Point(2, -1) })
                s.Set(e.X + o.X, e.Y + o.Y, HeroConceptGen.Hex(o.Y < 0 ? "F0E8D8" : "C8B898"));
        var ol = EquipGen.Outline(s, HeroConceptGen.Hex("2A1420"));
        foreach (var kv in ol.Px) if (kv.Key.X >= 1 && kv.Key.Y >= 1 && kv.Key.X < bmp.Width - 1 && kv.Key.Y < bmp.Height - 1) bmp.SetPixel(kv.Key.X, kv.Key.Y, kv.Value);
    }

    // kind: "Gobrider", "Elite", "Warlord"
    public static Bitmap Compose(string kind, string v, bool mirror) { return Compose(kind, v, mirror, 0, -1, 0); }

    // lift one leg (index into the leg runs of the bottom row, left to right) by 1 px for the walk cycle
    static string[] LiftLeg(string[] g, int leg)
    {
        if (leg < 0) return g;
        var rows = g.Select(s => s.ToCharArray()).ToArray(); int bh = rows.Length, n = -1;
        var last = g[bh - 1];
        for (int x = 0; x < last.Length; x++)
        {
            if (last[x] == '.') continue;
            if (x == 0 || last[x - 1] == '.') n++;
            if (n != leg) continue;
            for (int r = bh - 4; r < bh - 1; r++) rows[r][x] = rows[r + 1][x];
            rows[bh - 1][x] = '.';
        }
        return rows.Select(r => new string(r)).ToArray();
    }

    // riderDy: rider bob (+1 = down); leg: lifted leg run (-1 none); bonePose: 0 rest, 1 raised (windup), 2 struck
    public static Bitmap Compose(string kind, string v, bool mirror, int riderDy, int leg, int bonePose)
    {
        bool war = kind == "Warlord"; bool elite = kind == "Elite";
        int S = war ? 64 : 48; var bmp = new Bitmap(S, S, PixelFormat.Format32bppArgb);
        var pal = elite ? ElitePal : Pal;
        string[] rider = war ? (v == "Down" ? WarRiderDown : v == "Up" ? WarRiderUp : WarRiderSide) : (v == "Down" ? RiderDown : v == "Up" ? RiderUp : RiderSide);
        string[] boar = LiftLeg(war ? (v == "Down" ? WarBoarDown : v == "Up" ? WarBoarUp : WarBoarSide) : (v == "Down" ? BoarDown : v == "Up" ? BoarUp : BoarSide), leg);
        int sole = war ? 57 : 41;
        int bw = boar[0].Length, bh = boar.Length, rw = rider[0].Length, rh = rider.Length;
        int bx = S / 2 - bw / 2, by = sole - bh + 1;
        // v4: taller mount, rider sits ON its back (2-row overlap), feet dangle over the flanks
        int rx = v == "Side" ? bx + (war ? 1 : 0) : S / 2 - rw / 2, ry = by - rh + 2 + riderDy;
        Paint(bmp, boar, pal, bx, by); Paint(bmp, rider, pal, rx, ry);
        var foot = new[] { "kGk", "kDk", "kBk", "kkk" };
        if (v == "Side") Paint(bmp, foot, pal, rx + rw / 2 - 2, by + 2 + riderDy);
        else { Paint(bmp, foot, pal, bx + 1, by + 2 + riderDy); Paint(bmp, foot, pal, bx + bw - 4, by + 2 + riderDy); }
        // elite: iron faceplate over the mount's face (Down) / head (Side)
        if (elite && v != "Up")
        {
            var steel = new[] { "C8D0DC", "8A94A4", "5A6070" };
            if (v == "Down") for (int y = 7; y <= 9; y++) for (int x = 5; x <= 10; x++) if ("PpSQ".IndexOf(boar[y][x]) >= 0) bmp.SetPixel(bx + x, by + y, HeroConceptGen.Hex(steel[y == 7 ? 0 : x < 8 ? 1 : 2]));
            if (v == "Side") for (int y = 3; y <= 6; y++) for (int x = 14; x <= 19; x++) if ("PpSQ".IndexOf(boar[y][x]) >= 0) bmp.SetPixel(bx + x, by + y, HeroConceptGen.Hex(steel[y == 3 ? 0 : 1]));
        }
        // weapons: elite and warlord carry a bone club (rest / raised / struck)
        if (elite || war)
        {
            int k = war ? 1 : 0;
            if (v == "Down")
            {
                if (bonePose == 1) Bone(bmp, rx + rw + 2, ry - 3 - k * 2, rx + rw - 1, ry + rh - 2);
                else if (bonePose == 2) Bone(bmp, rx + rw - 3, ry + rh - 2, rx + rw + 3 + k * 2, ry + rh + 6 + k * 2);
                else Bone(bmp, rx + rw + 1, ry + 2 - k * 2, rx + rw - 2, ry + rh + 2 + k * 3);
            }
            else if (v == "Side")
            {
                if (bonePose == 1) Bone(bmp, rx + rw - 3, ry + rh - 4, rx + rw - 2, ry - 4 - k * 2);
                else if (bonePose == 2) Bone(bmp, rx + rw - 2, ry + rh - 4, rx + rw + 8 + k * 3, ry + rh - 1);
                else Bone(bmp, rx + rw - 2, ry + rh - 3, rx + rw + 5 + k * 3, ry + 2 - k * 2);
            }
            else
            {
                if (bonePose == 1) Bone(bmp, rx - 4, ry + rh + 1, rx + 1, ry + rh - 5);
                else if (bonePose == 2) Bone(bmp, rx + 1, ry + rh - 4, rx - 2, ry - 5 - k * 2);
                else Bone(bmp, rx - 2, ry + 2, rx + 1, ry + rh + 1);
            }
        }
        if (!mirror) return bmp;
        var o = new Bitmap(S, S, PixelFormat.Format32bppArgb);
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++) o.SetPixel(S - 1 - x, y, bmp.GetPixel(x, y));
        return o;
    }
}
