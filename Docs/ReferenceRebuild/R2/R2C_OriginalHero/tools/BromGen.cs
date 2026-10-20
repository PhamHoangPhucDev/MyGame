using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

// Brom (dwarf tank) — CANDIDATE: Up / Side base frames (Down = approved roster sprite), rest + strike gear poses,
// shield back/side views, Ground Slam key frame. Same 14x21 template, 32x32 cell, sole on row 29.
public static class BromGen
{
    const int W = 14, H = 21;
    public static string[] Down { get { return HeroConceptGen.Roster.First(r => r.Id == "1").Rows; } }
    public static Dictionary<char, string> Pal { get { return HeroConceptGen.Roster.First(r => r.Id == "1").Pal; } }

    // Back: horned helm, orange hair spilling under the rim, beard tips at the sides, back plate, belt.
    public static readonly string[] Up =
    {
        "h............h",
        "hy..kkkkkk..yh",
        ".hkaZZaaaaAkh.",
        ".kaZaaaaaaaAk.",
        ".kaaaaaaaaaAk.",
        ".kAAAAmmAAAAk.",
        ".kDHHLLHHHDDk.",
        ".kDHHLLHHHDDk.",
        "KDkaZaaaaAAkDK",
        "KDkZaaaaaaAkDK",
        "kZaKaZaaaAKaAk",
        "kaAKaaaaaaKAak",
        ".kkaKaaaaKakk.",
        "..kaaKaaKAak..",
        "..kZaaaaaAAk..",
        "..kbbbbbbbbk..",
        "..kttttttTTk..",
        "..kAaaTTaaAk..",
        "..kAAk..kAAk..",
        "..kBBk..kBBk..",
        ".kBBBBkkBBBBk.",
    };

    // 3/4 facing right: near horn front-right, face + big beard on the right, far pauldron on the left, toes right.
    public static readonly string[] Side =
    {
        "..h.......h...",
        "..hy.kkkkkyh..",
        "...hkaZZaaAkh.",
        "...kaZaaaaaAk.",
        "...kAAAAmmAAk.",
        "...kAADDssSDk.",
        "...kAAssseesk.",
        "...kAHsssSSsk.",
        "..KDHHLLLHHLHK",
        "..KDHLLHHLLHHK",
        ".kZaKDHLLHHLDK",
        ".kaAKDHLLHHDKk",
        "..kaaKDHHDKZak",
        "..kaaaKDDKaAAk",
        "..kZaaaKKaAAk.",
        "..kbbbbbmmbk..",
        "..kttttttTTk..",
        "..kAaaTTaaAk..",
        "..kAAk..kAAk..",
        "..kBBk..kBBk..",
        "..kBBBBkkBBBBk",
    };

    public static string[] Base(string v) { return v == "Down" ? Down : v == "Up" ? Up : Side; }

    public static char[,] Grid(string[] rows)
    {
        var g = new char[W, H];
        for (int y = 0; y < H; y++) for (int x = 0; x < W; x++) g[x, y] = rows[y][x];
        return g;
    }

    // strike body: whole upper body drops 1 px (heavy blow), legs braced
    public static char[,] Strike(string v)
    {
        var b = Base(v); var g = Grid(b);
        for (int y = 18; y >= 1; y--) for (int x = 0; x < W; x++) g[x, y] = b[y - 1][x];
        for (int x = 0; x < W; x++) g[x, 0] = '.';
        string l19 = v == "Side" ? ".kBBk...kBBk.." : ".kBBk....kBBk.";
        string l20 = v == "Side" ? "kBBBBk..kBBBBk" : "kBBBBk..kBBBBk";
        for (int x = 0; x < W; x++) { g[x, 19] = l19[x]; g[x, 20] = l20[x]; }
        return g;
    }

    // ---------- gear ----------
    static readonly Dictionary<char, string> ShieldPal = new Dictionary<char, string>
    {
        { 'm', "E8C048" }, { 'Z', "C8D0DC" }, { 'a', "8A94A4" }, { 'A', "5A6070" }, { 'g', "F0E0A0" },
        { 'b', "6A4428" }, { 'd', "9A6A3C" }, { 'B', "4A2E1C" },
    };

    public static EquipGen.Spr Shield(string v)
    {
        if (v == "Down") return EquipGen.BromShield();
        string[] r; int ax, ay;
        if (v == "Up") { r = new[] { "..mmmmm..", ".mBbbbBm.", "mbbdddbbm", "mbdbbbdbm", "mbdbbbdbm", "mbdbbbdbm", "mbbdddbbm", ".mBbbbBm.", "..mmmmm.." }; ax = 4; ay = 4; }
        else { r = new[] { ".mm.", "mZam", "maAm", "magm", "magm", "maAm", "maAm", "mAAm", ".mm." }; ax = 1; ay = 4; }
        return EquipGen.FromGrid(r, ShieldPal, ax, ay, "1C1A22");
    }

    public static EquipGen.Spr Hammer(int dir) { return EquipGen.Hammer(dir, 7, "7A4E2E", "A06A40", "8A94A4", "C8D0DC", "5A6070", "1C1A22"); }

    public class Pose { public int HX, HY, HD; public bool HFront; public int SX, SY; public bool SFront = true; }

    // rest: hammer upright in the off-shield hand, shield in front on the shield arm
    public static Pose Rest(string v)
    {
        if (v == "Down") return new Pose { HX = 6, HY = 24, HD = 2, HFront = false, SX = 19, SY = 23 };
        if (v == "Up") return new Pose { HX = 25, HY = 24, HD = 2, HFront = true, SX = 8, SY = 23 };
        return new Pose { HX = 11, HY = 24, HD = 2, HFront = false, SX = 21, SY = 23 };
    }

    // strike: hammer brought down in front (Down), over the head away from camera (Up), forward (Side)
    public static Pose StrikePose(string v)
    {
        if (v == "Down") return new Pose { HX = 8, HY = 24, HD = 6, HFront = true, SX = 20, SY = 23 };
        if (v == "Up") return new Pose { HX = 16, HY = 16, HD = 2, HFront = false, SX = 8, SY = 24 };
        return new Pose { HX = 20, HY = 24, HD = 0, HFront = true, SX = 18, SY = 24 };
    }

    public static Bitmap Compose(string v, bool strike, bool mirror)
    {
        var g = strike ? Strike(v) : Grid(Base(v));
        var body = EquipGen.Body(g, Pal);
        var p = strike ? StrikePose(v) : Rest(v);
        var hammer = new EquipGen.Layer { S = Hammer(p.HD), X = p.HX, Y = p.HY };
        var shield = new EquipGen.Layer { S = Shield(v), X = p.SX, Y = p.SY };
        var behind = new List<EquipGen.Layer>(); var front = new List<EquipGen.Layer>();
        (p.HFront ? front : behind).Add(hammer);
        (p.SFront ? front : behind).Add(shield);
        var bmp = EquipGen.Compose(body, behind, front);
        if (!mirror) return bmp;
        var m = new Bitmap(48, 48, PixelFormat.Format32bppArgb);
        for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++) m.SetPixel(47 - x, y, bmp.GetPixel(x, y));
        return m;
    }

    // ---------- Ground Slam key frame (96x64 tile, hero at (8,8)) ----------
    public static Bitmap GroundSlamTile()
    {
        var p = VfxConceptGen.Pal["Brom"];
        var under = new VfxConceptGen.Canvas(); var over = new VfxConceptGen.Canvas();
        VfxConceptGen.Ring(under, HeroConceptGen.Hex(p[2]), 32, 46, 18, 2.4, 0.42);
        VfxConceptGen.Ring(under, HeroConceptGen.Hex(p[1]), 32, 46, 12, 1.5, 0.42);
        VfxConceptGen.Crack(under, HeroConceptGen.Hex(p[3]), 10, 32, 46, 8, 14, 0.45);
        VfxConceptGen.Dots(over, new[] { p[2], p[3], "8A8A8A" }, 11, 32, 30, 18, 9, 18, 2);
        VfxConceptGen.Dots(over, new[] { p[4], p[1] }, 12, 34, 44, 6, 3, 8);
        var bmp = new Bitmap(96, 64, PixelFormat.Format32bppArgb);
        using (var gr = Graphics.FromImage(bmp)) gr.Clear(Color.FromArgb(255, 0x74, 0xA5, 0x87));
        for (int y = 0; y < 64; y++) for (int x = 0; x < 96; x++) if (under.Px[x, y].HasValue) bmp.SetPixel(x, y, under.Px[x, y].Value);
        var hero = Compose("Down", true, false);
        for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++) { var c = hero.GetPixel(x, y); if (c.A > 0) bmp.SetPixel(8 + x, 8 + y, c); }
        for (int y = 0; y < 64; y++) for (int x = 0; x < 96; x++) if (over.Px[x, y].HasValue) bmp.SetPixel(x, y, over.Px[x, y].Value);
        return bmp;
    }
}
