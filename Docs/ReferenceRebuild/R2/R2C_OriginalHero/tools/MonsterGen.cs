using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

// Monsters in the R2C style — CANDIDATE: Goblin (small green raider). Same 14x21 grid / 32x32 cell / sole on row 29
// as the heroes, but the goblin only uses rows 5-20 (16 px tall vs 21 px heroes). Enemies hold their weapon.
public static class MonsterGen
{
    const int W = 14, H = 21;

    // v2 — follows the reference video: a tiny goblin that is mostly a wide maroon hood/cape, a round light-green
    // face with big orange eyes, pointed ears sticking out sideways at eye level, short dark-green body, stubby legs.
    // Uses rows 9-20 (12 px tall vs 21 px heroes). Original pixels (not traced).
    public static readonly Dictionary<char, string> GoblinPal = new Dictionary<char, string>
    {
        { 'k', "2A1420" },                                     // outline (dark plum)
        { 'M', "6A2A3A" }, { 'h', "8A3E4C" }, { 'm', "4A1A2A" }, // hood mid / highlight / shade
        { 'G', "8CD064" }, { 'g', "A8E07A" }, { 'D', "5E9A44" }, // face light / brightest / shade
        { 'e', "F0B040" }, { 'E', "D0661E" },                    // eye glow / eye core
        { 't', "3E7A3A" }, { 'T', "2E5A2C" },                    // tunic / tunic shade
        { 'B', "2A1E18" },                                     // feet
    };

    // v3: thin hood rim around a big bright face (8 px wide), ears sticking out at eye level, 2-tone eyes,
    // small green torso and dark legs under the face (matches the reference proportions)
    public static readonly string[] GoblinDown =
    {
        "..............", "..............", "..............", "..............", "..............",
        "..............", "..............", "..............", "..............",
        "....kkkkkk....",
        "...kMMhhMMk...",
        "..kMhhhhhhMk..",
        ".kMMGggggGMMk.",
        ".kMGgGGGGgGMk.",
        "kgMGeGGGGeGMgk",
        "kGMGEGGGGEGMGk",
        ".kMGGGGGGGGMk.",
        ".kMDGGGGGGDMk.",
        "..kkDttttDkk..",
        "...kkttTTkk...",
        "...kBk..kBk...",
    };

    public static readonly string[] GoblinUp =
    {
        "..............", "..............", "..............", "..............", "..............",
        "..............", "..............", "..............", "..............",
        "....kkkkkk....",
        "...kMMhhMMk...",
        "..kMhhhhhhMk..",
        ".kMMMhhhhMMMk.",
        ".kMMMMMMMMMMk.",
        "kgMMMMMMMMMMgk",
        "kGMMMMmmMMMMGk",
        ".kMMMmmmmMMMk.",
        ".kMmmmmmmmmMk.",
        "..kkDttttDkk..",
        "...kkTttTkk...",
        "...kBk..kBk...",
    };

    public static readonly string[] GoblinSide =
    {
        "..............", "..............", "..............", "..............", "..............",
        "..............", "..............", "..............", "..............",
        "...kkkkkk.....",
        "..kMMhhMMk....",
        ".kMhhhhhhMk...",
        ".kMMMGggggGk..",
        ".kMMGgGGGGGk..",
        "kgMGGGGGeGGGgk",
        "kGMGGGGGEGGGGk",
        ".kMGGGGGGGGGk.",
        ".kMDGGGGGGDk..",
        "..kkDtttDkk...",
        "...kkttTkk....",
        "...kBk.kBk....",
    };

    public static string[] GoblinBase(string v) { return v == "Down" ? GoblinDown : v == "Up" ? GoblinUp : GoblinSide; }

    public static char[,] Grid(string[] rows)
    {
        var g = new char[W, H];
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) g[x, y] = rows[y][x];
        return g;
    }

    static void Row(char[,] g, int y, string s) { for (int x = 0; x < W; x++) g[x, y] = s[x]; }

    // walk contact: body drops 1 px (rows 5-19 shift), one foot planted, the other lifted
    public static char[,] GoblinWalk(string v, int f)
    {
        var b = GoblinBase(v); var g = Grid(b);
        if (f == 1 || f == 3) return g;
        for (int y = 19; y >= 5; y--) for (int x = 0; x < W; x++) g[x, y] = b[y - 1][x];
        Row(g, 4, "..............");
        Row(g, 20, f == 0 ? "..kBk...kk...." : "....kk...kBk..");
        return g;
    }

    // rusty knife (short, chipped): 8 drawn directions, pivot = hand
    public static readonly EquipGen.BladeDef RustyKnife = new EquipGen.BladeDef
    {
        Id = "goblin_knife", Name = "Dao ri", Length = 3, GuardHalf = 1,
        Core = "D0D0C8", Band = "8A7A68", Guard = "5A4030", Wrap = "4E3020", Pommel = "5A4030", Outline = "2A1420",
    };

    public class P { public int X, Y, Dir; public bool Front = true; }

    public static P GoblinHold(string v, int f)
    {
        // f: 0 rest, 1 windup, 2 stab
        if (v == "Down") return f == 0 ? new P { X = 10, Y = 26, Dir = 6 } : f == 1 ? new P { X = 10, Y = 22, Dir = 3, Front = false } : new P { X = 13, Y = 28, Dir = 6 };
        if (v == "Up") return f == 0 ? new P { X = 21, Y = 26, Dir = 6 } : f == 1 ? new P { X = 21, Y = 22, Dir = 1 } : new P { X = 17, Y = 21, Dir = 2, Front = false };
        return f == 0 ? new P { X = 20, Y = 26, Dir = 7 } : f == 1 ? new P { X = 12, Y = 22, Dir = 3, Front = false } : new P { X = 22, Y = 25, Dir = 0 };
    }

    public static Bitmap Goblin(string anim, string v, int f, bool mirror)
    {
        char[,] g = anim == "Walk" ? GoblinWalk(v, f) : Grid(GoblinBase(v));
        var body = EquipGen.Body(g, GoblinPal);
        int pose = anim == "Attack" ? (f == 0 ? 1 : f == 1 ? 2 : 0) : 0;   // attack: windup, stab, recover
        var p = GoblinHold(v, pose); int dy = anim == "Walk" && (f == 0 || f == 2) ? 1 : 0;
        var knife = new EquipGen.Layer { S = EquipGen.Blade(RustyKnife, p.Dir), X = p.X, Y = p.Y + dy };
        var hand = new EquipGen.Layer { S = EquipGen.Hand("A8E07A", "8CD064", "2A1420"), X = p.X - 1, Y = p.Y - 1 + dy };
        var layers = new List<EquipGen.Layer> { knife, hand };
        var bmp = p.Front ? EquipGen.Compose(body, new List<EquipGen.Layer>(), layers) : EquipGen.Compose(body, layers, new List<EquipGen.Layer>());
        if (!mirror) return bmp;
        var o = new Bitmap(48, 48, PixelFormat.Format32bppArgb);
        for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++) o.SetPixel(47 - x, y, bmp.GetPixel(x, y));
        return o;
    }
}
