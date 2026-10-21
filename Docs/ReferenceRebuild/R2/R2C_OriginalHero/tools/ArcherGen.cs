using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

// Goblin Archer — CANDIDATE (follows the reference video "Gob Archer"): bald goblin, tall ears pointing up, orange
// eyes, dark plum studded tunic, wooden shortbow (held across the waist when aiming at the viewer), quiver on the back.
// 48x48 canvas like the Goblin: 14x12 grid at x 17..30, y 26..37 (sole row 37). Original pixels (not traced).
public static class ArcherGen
{
    public static readonly Dictionary<char, string> Pal = new Dictionary<char, string>
    {
        { 'k', "2A1420" },
        { 'G', "8CD064" }, { 'g', "A8E07A" }, { 'D', "5E9A44" }, { 'e', "F0B040" }, { 'E', "D0661E" },
        { 'u', "4A2440" }, { 'U', "321828" }, { 'x', "9AA0A8" }, { 'B', "2A1E18" },   // tunic, shade, studs, feet
        { 'w', "8A5A34" }, { 'W', "B07A48" }, { 'q', "E8E0D0" },                     // bow wood, light wood, string
        { 'a', "C8A878" }, { 'i', "D0D0C8" }, { 'f', "D83A3A" }, { 'Z', "6A4028" },   // arrow shaft, head, fletch, quiver
    };

    // v2: 16x16, bigger than the plain Goblin (as in the video), tall ears standing up at the head corners
    static readonly string[] BodyDown =
    {
        ".k............k.", ".kk..........kk.", ".kgk........kgk.", ".kGgk.kkkk.kgGk.", "..kGgkGggGkgGk..", "..kGGGgGGgGGGk..",
        "..kGGGGGGGGGGk..", "..kGGkkGGkkGGk..", "..kGGeEGGeEGGk..", "..kDGGGGGGGGDk..", "...kDGGkkGGDk...", "....kkDDDDkk....",
        "...kuuuxxuuuk...", "..kUuxuuuuxuUk..", "...kkUuuuuUkk...", "....kBk..kBk....",
    };
    static readonly string[] BodyUp =
    {
        ".k............k.", ".kk..........kk.", ".kgk........kgk.", ".kGgk.kkkk.kgGk.", "..kGgkGGGGkgGk..", "..kGGGGGGGGGGk..",
        "..kGGGGGGGGGGk..", "..kGGGGGGGGGGk..", "..kDGGGGGGGGDk..", "..kDDGGGGGGDDk..", "...kDDDDDDDDk...", "....kkDDDDkk....",
        "...kuuxuuxuuk...", "..kUuuuuuuuuUk..", "...kkUuuuuUkk...", "....kBk..kBk....",
    };
    static readonly string[] BodySide =
    {
        ".k..............", ".kk.............", ".kgk............", ".kGgk.kkkkk.....", "..kGgkGgggGk....", "..kGGGgGGGGgk...",
        "..kGGGGGGGGGk...", "..kGGGGGGkkGk...", "..kGGGGGGeEGk...", "..kDGGGGGGGGGk..", "...kDGGGGGkGk...", "....kkDDDDkk....",
        "....kuuxuuuk....", "...kUuuuuuuUk...", "....kkUuuUkk....", ".....kBk.kBk....",
    };

    // quiver (Up: on the back, red fletching sticking out; Side: behind the shoulder)
    static readonly string[] QuiverUp = { "fkf.", "kfkf", "kZZk", "kZZk", "kZZk", ".kk." };
    static readonly string[] QuiverSide = { "fk", "kf", "kZ", "kZ", "kk" };

    // bows. Down: rest = vertical in the right hand; aim = across the waist, arrow pointing at the viewer
    static readonly string[] BowVert = { "kk.", "kWk", "kwq", "kwq", "kWq", "Gwq", "kwq", "kwq", "kWk", "kk." };
    static readonly string[] BowDownAim = { ".kkkk....kkkk.", "kWwwwkkkkwwwWk", "kk.qqGiiGqq.kk", "......kk......" };
    static readonly string[] BowDownLoose = { ".kkkk....kkkk.", "kWwwwkkkkwwwWk", "kk.qqGqqGqq.kk" };
    // Up: bow held across in front of the archer (away from camera): only the curved limbs show past the body
    static readonly string[] BowUpWide = { "kkk..............kkk", "kWwkk..........kkwWk", ".kkwwk........kwwkk.", "...kkk........kkk..." };
    // Side (faces right): bow upright in front, string drawn back with a nocked arrow / snapped straight after the shot
    static readonly string[] BowSideAim =
    {
        "....kk...", "....kWk..", "...qkwk..", "..q.kwk..", "kfaaGaaik", "..q.kwk..", "...qkwk..", "....kWk..", "....kk...",
    };
    static readonly string[] BowSideLoose =
    {
        ".kk.", ".kWk", "qkwk", "qkwk", "qGwk", "qkwk", "qkwk", ".kWk", ".kk.",
    };

    public static IEnumerable<string[]> AllGrids()
    {
        return new[] { BodyDown, BodyUp, BodySide, QuiverUp, QuiverSide, BowVert, BowDownAim, BowDownLoose, BowUpWide, BowSideAim, BowSideLoose };
    }

    const int X0 = 16, Y0 = 22;   // 16x16 grid at x 16..31, y 22..37 (sole row 37)

    static void Paint(Bitmap bmp, string[] g, int ox, int oy)
    {
        for (int y = 0; y < g.Length; y++) for (int x = 0; x < g[y].Length; x++)
            {
                char c = g[y][x]; if (c == '.') continue;
                int X = ox + x, Y = oy + y; if (X < 1 || Y < 1 || X >= bmp.Width - 1 || Y >= bmp.Height - 1) continue;
                bmp.SetPixel(X, Y, HeroConceptGen.Hex(Pal[c]));
            }
    }

    // walk contact: rows 0-13 drop 1 px (tunic hem row 14 squashed away), feet row swapped so one foot is lifted
    static string[] Step(string[] body, string v, int step)
    {
        if (step == 0) return body;
        var o = new string[16]; o[0] = new string('.', 16);
        for (int r = 0; r < 14; r++) o[r + 1] = body[r];
        o[15] = v == "Side" ? (step == 1 ? "....kBk..kk....." : "......kk.kBk....") : (step == 1 ? "...kBk...kk....." : ".....kk...kBk...");
        return o;
    }

    // pose: 0 rest, 1 aim (drawn), 2 loose (just shot). dy: body bob. step: walk contact (0 none, 1/2 alternate feet).
    // Side faces right (mirror for left).
    public static Bitmap Compose(string v, int pose, bool mirror, int dy = 0, int step = 0)
    {
        var bmp = new Bitmap(48, 48, PixelFormat.Format32bppArgb);
        int ox = X0, oy = Y0 + dy;
        string[] bd = Step(BodyDown, v, step), bu = Step(BodyUp, v, step), bs = Step(BodySide, v, step);
        int body0 = oy; if (step > 0) oy += 1;   // overlays follow the dropped body
        if (v == "Down")
        {
            Paint(bmp, bd, ox, body0);
            if (pose == 0) Paint(bmp, BowVert, ox + 13, oy + 5);
            else Paint(bmp, pose == 1 ? BowDownAim : BowDownLoose, ox + 1, oy + 11);
        }
        else if (v == "Up")
        {
            // aim: the bow is held across in front (away from camera) - only its limbs show either side of the body
            if (pose == 0) Paint(bmp, BowVert, ox, oy + 5);
            else Paint(bmp, BowUpWide, ox - 2, oy + 8 - (pose == 1 ? 1 : 0));
            Paint(bmp, bu, ox, body0);
            Paint(bmp, QuiverUp, ox + 9, oy + 6);
        }
        else
        {
            Paint(bmp, QuiverSide, ox + 2, oy + 9);
            Paint(bmp, bs, ox, body0);
            if (pose == 0) Paint(bmp, BowVert, ox + 12, oy + 6);
            else if (pose == 1) Paint(bmp, BowSideAim, ox + 9, oy + 5);
            else Paint(bmp, BowSideLoose, ox + 13, oy + 5);
        }
        if (!mirror) return bmp;
        var o = new Bitmap(48, 48, PixelFormat.Format32bppArgb);
        for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++) o.SetPixel(47 - x, y, bmp.GetPixel(x, y));
        return o;
    }

    // arrow projectile: 16x16 cell per direction (R UR U UL L DL D DR), pivot = cell centre, head toward the direction
    public static Bitmap Arrow(int dir)
    {
        var s = new EquipGen.Spr();
        double ang = dir * Math.PI / 4; double dx = Math.Cos(ang), dy = -Math.Sin(ang);
        for (int t = -5; t <= 5; t++)
        {
            int x = (int)Math.Round(t * dx), y = (int)Math.Round(t * dy);
            string col = t >= 4 ? Pal['i'] : t <= -4 ? Pal['f'] : Pal['a'];
            s.Set(x, y, HeroConceptGen.Hex(col));
        }
        // fletch flare: one px either side of the tail
        int tx = (int)Math.Round(-5 * dx), ty = (int)Math.Round(-5 * dy);
        s.Set(tx + (int)Math.Round(-dy), ty + (int)Math.Round(dx), HeroConceptGen.Hex(Pal['f']));
        s.Set(tx - (int)Math.Round(-dy), ty - (int)Math.Round(dx), HeroConceptGen.Hex(Pal['f']));
        var ol = EquipGen.Outline(s, HeroConceptGen.Hex(Pal['k']));
        var bmp = new Bitmap(16, 16, PixelFormat.Format32bppArgb);
        foreach (var kv in ol.Px) { int X = 8 + kv.Key.X, Y = 8 + kv.Key.Y; if (X >= 0 && Y >= 0 && X < 16 && Y < 16) bmp.SetPixel(X, Y, kv.Value); }
        return bmp;
    }
}
