using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

// Forest creatures — CANDIDATE (follows the reference video): Satyr (pink-tan goat-man, Y horns, striped torso,
// horn-butt melee) and Dryad (leafy cap, pale face, orange eyes, lavender top, red-bark legs, casts a leaf bolt).
// 48x48 canvas like the Goblin Archer: 16x16 grid at x 16..31, y 22..37 (sole row 37). Original pixels (not traced).
public static class CreatureGen
{
    public static readonly Dictionary<char, string> Pal = new Dictionary<char, string>
    {
        { 'k', "2A1420" }, { 'B', "2A1E18" },
        // satyr
        { 'c', "F0D8C0" }, { 'C', "B89080" }, { 'P', "D89880" }, { 'p', "F0B8A0" }, { 'Q', "A86A60" }, { 'M', "6A2A3A" }, { 'e', "F0E8D8" },
        // dryad
        { 'L', "6AB04A" }, { 'l', "8CD064" }, { 'd', "3E7A3A" }, { 'W', "E8E4F0" }, { 'w', "C8C4D8" }, { 'E', "F0B040" },
        { 'V', "D8D0E8" }, { 'v', "A8A0C8" }, { 'R', "A8483C" }, { 'r', "7A2E28" },
        // magic orb
        { 'o', "F0FFD0" }, { 'g', "A8E07A" },
    };

    // ---------- Satyr ----------
    static readonly string[] SatyrDown =
    {
        ".kk..........kk.", "kcck........kcck", "kcCck......kcCck", ".kcCck....kcCck.", "..kkCkkkkkkCkk..", "..kPkMMMMMMkPk..",
        ".kPPkPpppPPkPPk.", "..kkPPPPPPPPkk..", "...kPkePPekPk...", "...kPPPPPPPPk...", "....kQPkkPQk....", "...kMQQQQQQMk...",
        "..kQkPMMMMPkQk..", "...kkMPPPPMkk...", "....kQk..kQk....", "....kBk..kBk....",
    };
    static readonly string[] SatyrUp =
    {
        ".kk..........kk.", "kcck........kcck", "kcCck......kcCck", ".kcCck....kcCck.", "..kkCkkkkkkCkk..", "..kPkMMMMMMkPk..",
        ".kPPkMMMMMMkPPk.", "..kkMMMMMMMMkk..", "...kMMMMMMMMk...", "...kQMMMMMMQk...", "....kQQQQQQk....", "...kMQQQQQQMk...",
        "..kQkPMMMMPkQk..", "...kkMPppPMkk...", "....kQk..kQk....", "....kBk..kBk....",
    };
    static readonly string[] SatyrSide =
    {
        "..kk............", ".kcck...........", ".kcCck..........", "..kcCck.........", "...kkCkkkkk.....", "..kPkMMMMMMk....",
        ".kPPkMPppPPPk...", "..kkMPPPPPPPk...", "...kMPPPPkePk...", "...kMPPPPPPPPk..", "....kQPPPkQQk...", "...kMQQQQQQk....",
        "...kQMMMMPkQk...", "....kMPPPPMk....", ".....kQk.kQk....", ".....kBk.kBk....",
    };

    // ---------- Dryad ----------
    static readonly string[] DryadDown =
    {
        "....kk....kk....", "...klLk..klLk...", "..klLLLkklLLlk..", "..kLLlLLLLlLLk..", ".kdkWWWWWWWWkdk.", "kdLkWWWWWWWWkLdk",
        ".kdkWEWWWWEWkdk.", "..kkWEWWWWEWkk..", "...kwWWWWWWwk...", "...kwWwwwwWwk...", "..kVkVVVVVVkVk..", "..kvVVVVVVVVvk..",
        "...kRRRRRRRRk...", "...krRRRRRRrk...", "...kRRk..kRRk...", "...kBBk..kBBk...",
    };
    static readonly string[] DryadUp =
    {
        "....kk....kk....", "...klLk..klLk...", "..klLLLkklLLlk..", "..kLLlLLLLlLLk..", ".kdkLLLLLLLLkdk.", "kdLkLLlLLlLLkLdk",
        ".kdkLLLLLLLLkdk.", "..kkdLLLLLLdkk..", "...kwdLLLLdwk...", "...kwWWWWWWwk...", "..kVkVVVVVVkVk..", "..kvVVVVVVVVvk..",
        "...kRRRRRRRRk...", "...krRRRRRRrk...", "...kRRk..kRRk...", "...kBBk..kBBk...",
    };
    static readonly string[] DryadSide =
    {
        "...kk..kk.......", "..klLkklLk......", ".klLLLLLLlk.....", ".kLLlLLLLLLk....", "kdkLWWWWWWWk....", "kLdkWWWWWWWWk...",
        ".kdkWWWWWEWWk...", "..kkWWWWWEWWk...", "...kwWWWWWWwk...", "....kwWWWwwk....", "...kVVVVVVkVk...", "...kvVVVVVVvk...",
        "....kRRRRRRk....", "....krRRRRrk....", "....kRRk.kRRk...", "....kBBk.kBBk...",
    };

    // gathered nature magic (Dryad attack): bright seed-orb between the hands
    static readonly string[] Orb = { ".kk.", "kogk", "kggk", ".kk." };
    static readonly string[] OrbBig = { ".kkk.", "kgogk", "koook", "kgogk", ".kkk." };

    public static IEnumerable<string[]> AllGrids()
    {
        return new[] { SatyrDown, SatyrUp, SatyrSide, DryadDown, DryadUp, DryadSide, Orb, OrbBig };
    }

    const int X0 = 16, Y0 = 22;

    static void Paint(Bitmap bmp, string[] g, int ox, int oy)
    {
        for (int y = 0; y < g.Length; y++) for (int x = 0; x < g[y].Length; x++)
            {
                char c = g[y][x]; if (c == '.') continue;
                int X = ox + x, Y = oy + y; if (X < 1 || Y < 1 || X >= bmp.Width - 1 || Y >= bmp.Height - 1) continue;
                bmp.SetPixel(X, Y, HeroConceptGen.Hex(Pal[c]));
            }
    }

    static string[] Base(string kind, string v)
    {
        if (kind == "Satyr") return v == "Down" ? SatyrDown : v == "Up" ? SatyrUp : SatyrSide;
        return v == "Down" ? DryadDown : v == "Up" ? DryadUp : DryadSide;
    }

    // walk contact: rows 0-13 drop 1 px (row 14 squashed away); the lifted foot's sole row turns to outline
    static string[] Step(string[] body, int step)
    {
        if (step == 0) return body;
        var o = new string[16]; o[0] = new string('.', 16);
        for (int r = 0; r < 14; r++) o[r + 1] = body[r];
        var feet = body[15].ToCharArray(); int run = -1;
        for (int x = 0; x < 16; x++)
        {
            if (feet[x] == '.') continue;
            if (x == 0 || feet[x - 1] == '.') run++;
            if (run == step - 1) feet[x] = '.';
        }
        o[15] = new string(feet);
        return o;
    }

    // pose: 0 rest, 1 windup (Satyr: head down / Dryad: gather orb), 2 strike (Satyr: butt / Dryad: big orb, cast)
    // Side faces right (mirror for left).
    public static Bitmap Compose(string kind, string v, int pose, bool mirror, int step = 0)
    {
        var bmp = new Bitmap(48, 48, PixelFormat.Format32bppArgb);
        int ox = X0, oy = Y0;
        var body = Step(Base(kind, v), step);
        Paint(bmp, body, ox, oy);
        if (kind == "Dryad" && pose > 0)
        {
            if (v == "Up") Paint(bmp, pose == 1 ? Orb : OrbBig, ox + 6, oy - 4);   // raised above the leaf cap
            if (v == "Down") Paint(bmp, pose == 1 ? Orb : OrbBig, ox + 6, oy + 10);
            else if (v == "Side") Paint(bmp, pose == 1 ? Orb : OrbBig, ox + 12, oy + 8);
        }
        if (!mirror) return bmp;
        var o = new Bitmap(48, 48, PixelFormat.Format32bppArgb);
        for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++) o.SetPixel(47 - x, y, bmp.GetPixel(x, y));
        return o;
    }

    // Dryad leaf bolt: 16x16 per direction (R UR U UL L DL D DR), pivot = cell centre, tip toward the direction
    public static Bitmap LeafBolt(int dir)
    {
        var s = new EquipGen.Spr();
        double ang = dir * Math.PI / 4; double dx = Math.Cos(ang), dy = -Math.Sin(ang);
        double nx = -dy, ny = dx;   // perpendicular
        for (int t = -3; t <= 4; t++)
        {
            int x = (int)Math.Round(t * dx), y = (int)Math.Round(t * dy);
            s.Set(x, y, HeroConceptGen.Hex(t >= 3 ? Pal['l'] : Pal['L']));
            if (t >= -1 && t <= 2)   // leaf belly: 1 px either side (diagonals fill both orthogonal neighbours, no checker)
            {
                int sx = Math.Sign(Math.Round(nx, 3)), sy = Math.Sign(Math.Round(ny, 3));
                if (dir % 2 == 1)
                {
                    s.Set(x + sx, y, HeroConceptGen.Hex(Pal['d'])); s.Set(x, y + sy, HeroConceptGen.Hex(Pal['d']));
                    s.Set(x - sx, y, HeroConceptGen.Hex(Pal['l'])); s.Set(x, y - sy, HeroConceptGen.Hex(Pal['l']));
                }
                else
                {
                    s.Set(x + sx, y + sy, HeroConceptGen.Hex(Pal['d']));
                    s.Set(x - sx, y - sy, HeroConceptGen.Hex(Pal['l']));
                }
            }
        }
        var ol = EquipGen.Outline(s, HeroConceptGen.Hex(Pal['k']));
        // sparkle trail behind the leaf (not outlined)
        for (int t = 5; t <= 6; t++) ol.Set((int)Math.Round(-t * dx), (int)Math.Round(-t * dy), HeroConceptGen.Hex(t == 5 ? Pal['g'] : Pal['o']));
        var bmp = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
        foreach (var kv in ol.Px) { int X = 8 + kv.Key.X, Y = 8 + kv.Key.Y; if (X >= 0 && Y >= 0 && X < 16 && Y < 16) bmp.SetPixel(X, Y, kv.Value); }
        return bmp;
    }
}
