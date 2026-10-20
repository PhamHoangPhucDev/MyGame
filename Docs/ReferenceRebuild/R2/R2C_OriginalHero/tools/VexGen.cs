using System;
using System.Collections.Generic;
using System.Linq;

// Vex (approved v5) body frames: Idle / Walk / Attack x 3 views, Hurt, Death. Same 14x21 template as Aren.
// Down F0 = approved roster sprite; Up (back) and Side (3/4 facing right) are new.
public static class VexGen
{
    const int W = 14, H = 21;

    public static string[] Down { get { return HeroConceptGen.Roster.First(r => r.Id == "5").Rows; } }
    public static Dictionary<char, string> Pal { get { return HeroConceptGen.Roster.First(r => r.Id == "5").Pal; } }

    // Back: messy silver hair, crimson blindfold knot with two tails over the dark collar, light-grey cloak.
    public static readonly string[] Up =
    {
        "..y.K.KK.K.y..",
        "..hKHKHLKHKh..",
        ".KHHLHLXLLHHK.",
        "KHHLLXLLLLHHDK",
        "KHHHLLLLLHHHDK",
        "KDHHHLHHLHHHDK",
        "KDHHHHHHHHHHDK",
        "KRrrrrqqrrrrRK",
        "KDHHHHrRHHHHDK",
        "KDHHHrRRrHHHDK",
        ".KDHHHrRHHHDK.",
        ".KDMMMrRMMMDK.",
        "occcccRrccccco",
        "occcccrRcccCCo",
        "occccccRccccCo",
        "occcccccccccCo",
        "occcccccccCCCo",
        ".oCcccccccCCC.",
        "..CCCkkkkCCC..",
        "...knnkknnk...",
        "..kBBBkkBBBk..",
    };

    // 3/4 facing right: blindfold across the face, its tail streaming back (left), cloak behind on the left.
    public static readonly string[] Side =
    {
        "...y.KK.K.y...",
        "...hKHKHLKKh..",
        "..KHHLHLXLLHK.",
        ".KHHLLXXLLLHHK",
        "KHHHLLLLLHHHHK",
        "KDHHHHLHHHHHHK",
        "KDHHHHHHsHHsHK",
        "rRKHHrqqrrrrRK",
        "RrKHHRrrrrrrRK",
        ".RKDHHssssssHK",
        "..KDHHssspssSK",
        "..KDMMNMMMMMK.",
        "ockMMMMMMMMk..",
        "occkMNMMMjjk..",
        "occkjjMMjjjjk.",
        "occkbbggbbbbk.",
        "occkjjjjjjJk..",
        ".oCkijjjjJk...",
        "..CkijkkjJk...",
        "...knnkknnk...",
        "...kBBBkkBBBk.",
    };

    public static string[] Base(string view) { return view == "Down" ? Down : view == "Up" ? Up : Side; }

    public static char[,] Grid(string[] rows)
    {
        var g = new char[W, H];
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) g[x, y] = rows[y][x];
        return g;
    }

    static void Row(char[,] g, int y, string s) { for (int x = 0; x < W; x++) g[x, y] = s[x]; }

    static bool Cloth(char c) { return c == 'o' || c == 'c' || c == 'C'; }

    // breath: head rows 0-9 down 1 px (cloak columns on row 10 kept)
    static void Nod(char[,] g, string[] b)
    {
        for (int y = 10; y >= 1; y--)
            for (int x = 0; x < W; x++)
            {
                char src = b[y - 1][x];
                if (y == 10 && (Cloth(b[10][x]) || src == '.')) { g[x, y] = b[10][x]; continue; }
                g[x, y] = src;
            }
        Row(g, 0, "..............");
    }

    static void Drop(char[,] g, string[] b)
    {
        for (int y = 18; y >= 1; y--) for (int x = 0; x < W; x++) g[x, y] = b[y - 1][x];
        Row(g, 0, "..............");
    }

    // Idle: blindfold tail flutter (F1 / F3), breath nod (F2). 0.25 / 0.15 / 0.25 / 0.15 s like Aren.
    public static char[,] Idle(string view, int f)
    {
        var b = Base(view); var g = Grid(b);
        if (f == 2) { Nod(g, b); return g; }
        if (view == "Down")
        {
            if (f == 1) { g[13, 10] = '.'; g[13, 7] = 'R'; g[12, 7] = 'R'; }
            if (f == 3) { g[13, 10] = 'r'; g[12, 11] = 'R'; }
        }
        else if (view == "Up")
        {
            if (f == 1) { g[7, 12] = 'c'; g[8, 12] = 'R'; g[8, 13] = 'r'; g[7, 13] = 'c'; g[8, 14] = 'R'; g[7, 14] = 'c'; }
            if (f == 3) { g[7, 12] = 'c'; g[6, 12] = 'R'; g[6, 13] = 'r'; g[7, 13] = 'c'; g[6, 14] = 'R'; g[7, 14] = 'c'; }
        }
        else
        {
            if (f == 1) { g[0, 8] = '.'; g[0, 6] = 'r'; g[1, 6] = 'R'; }
            if (f == 3) { g[1, 9] = '.'; g[1, 10] = 'r'; g[0, 9] = 'R'; }
        }
        return g;
    }

    // Walk: F0/F2 contact (body drops 1 px, one foot planted on row 29), F1/F3 passing. 4 x 0.125 s.
    public static char[,] Walk(string view, int f)
    {
        var b = Base(view); var g = Grid(b);
        bool side = view == "Side";
        if (f == 0 || f == 2)
        {
            Drop(g, b);
            if (side)
            {
                if (f == 0) { Row(g, 19, "..knBk..kink.."); Row(g, 20, ".knBk....kinBk"); }
                else { Row(g, 19, "..kink..knBk.."); Row(g, 20, ".kink....knBBk"); }
            }
            else
            {
                if (f == 0) { Row(g, 19, "...kinkknink.."); Row(g, 20, "..kinBk......."); }
                else { Row(g, 19, "..kinBkkink..."); Row(g, 20, ".......kinBk.."); }
            }
        }
        else if (f == 1)
        {
            if (view == "Down") Row(g, 18, ".CCkijkkjJkC..");
            else if (view == "Up") Row(g, 18, ".CCCCkkkkCCC..");
            else Row(g, 18, ".CCkijkkjJk...");
        }
        else
        {
            if (view == "Down") { g[13, 10] = '.'; g[13, 7] = 'R'; }
            else if (side) { g[0, 8] = '.'; g[0, 6] = 'r'; }
        }
        return g;
    }

    // Attack (3 frames): F0 windup = idle F1 (tail flares), F1 strike = walk contact (lunge + stride), F2 recover = base.
    public static char[,] Attack(string view, int f)
    {
        if (f == 0) return Idle(view, 1);
        if (f == 1) return Walk(view, 0);
        return Grid(Base(view));
    }

    public static char[,] Kneel()
    {
        var b = Grid(Down);
        var g = new char[W, H];
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) g[x, y] = '.';
        for (int y = 0; y <= 17; y++) for (int x = 0; x < W; x++) g[x, y + 2] = b[x, y];
        string knee = "..kinBkkinBk..";
        for (int x = 0; x < W; x++) g[x, 20] = knee[x];
        return g;
    }
}
