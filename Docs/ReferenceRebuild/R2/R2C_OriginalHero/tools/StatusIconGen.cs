using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;

// Status effect icons v2: 24x24, 1-px transparent margin, rounded-square badge (debuff = red rim, buff = blue rim),
// genre-standard symbols so they read at a glance. UI: show at 48 px (2x). World: PPU 16 -> 1.5 tiles above the head.
public static class StatusIconGen
{
    public const int S = 24;
    static Color C(string h) { return HeroConceptGen.Hex(h); }

    public static readonly string[] Ids = { "Stun", "Bleed", "Slow", "Taunt", "Absorb", "DamageReduction", "Haste", "Stealth", "DodgeReady", "Empowered", "Reflect" };
    public static readonly string[] NamesVi = { "Choang", "Chay mau", "Lam cham", "Khieu khich", "Khien hap thu", "Giam sat thuong", "Tang toc", "Tang hinh", "San sang ne", "Don ke x2", "Phan don" };
    public static readonly bool[] Debuff = { true, true, true, true, false, false, false, false, false, false, false };
    public static readonly string[] Source =
    {
        "Aren Khien Dap, Brom Nen Dat", "Vex combo ket, Phi Dao", "Lyra day leo, Selene Vong Phu Chu", "Brom Gam Thach",
        "Selene Khien Ma Luc", "Aren Thanh Luy, Brom Tuong Sat / Gam Thach / giam goc", "Lyra Buoc Gio", "Vex Bom Khoi",
        "Lyra Buoc Gio", "Vex Buoc Bong", "Aren Thanh Luy",
    };

    static ArenKitSheets.Cv Canvas() { return new ArenKitSheets.Cv(S, S, 0, 0); }
    static void Set(ArenKitSheets.Cv cv, double x, double y, string h) { cv.Set(x, y, C(h)); }
    static void Line(ArenKitSheets.Cv cv, double x0, double y0, double x1, double y1, string h, int w = 1) { ArenKitSheets.Line(cv, C(h), x0, y0, x1, y1, w); }
    static void Disc(ArenKitSheets.Cv cv, double x, double y, double r, string h) { ArenKitSheets.Disc(cv, C(h), x, y, r); }

    static void Grid(ArenKitSheets.Cv cv, int ox, int oy, string[] rows, Dictionary<char, string> pal)
    {
        for (int y = 0; y < rows.Length; y++) for (int x = 0; x < rows[y].Length; x++) { char c = rows[y][x]; if (c != '.') Set(cv, ox + x, oy + y, pal[c]); }
    }

    static void Star4(ArenKitSheets.Cv cv, int x, int y, string arm, string core)
    {
        Set(cv, x, y, core); Set(cv, x - 1, y, arm); Set(cv, x + 1, y, arm); Set(cv, x, y - 1, arm); Set(cv, x, y + 1, arm);
    }

    // 3x5 pixel glyphs for "x2"
    static readonly Dictionary<char, string[]> Glyph = new Dictionary<char, string[]>
    {
        { 'x', new[] { "...", "#.#", ".#.", "#.#", "..." } },
        { '2', new[] { "##.", "..#", ".#.", "#..", "###" } },
    };
    static void Text(ArenKitSheets.Cv cv, int ox, int oy, string s, string fill, string shadow)
    {
        for (int i = 0; i < s.Length; i++)
        {
            var g = Glyph[s[i]];
            for (int y = 0; y < 5; y++) for (int x = 0; x < 3; x++)
                    if (g[y][x] == '#') { Set(cv, ox + i * 4 + x + 1, oy + y + 1, shadow); }
            for (int y = 0; y < 5; y++) for (int x = 0; x < 3; x++)
                    if (g[y][x] == '#') Set(cv, ox + i * 4 + x, oy + y, fill);
        }
    }

    static readonly Dictionary<char, string> ShieldPal = new Dictionary<char, string>
    {
        { 'Z', "C8D0DC" }, { 'W', "F0F4F8" }, { 'a', "8E98A8" }, { 'A', "5A6272" }, { 'u', "4A58A0" }, { 'c', "2E3A78" }, { 'C', "1E2450" }, { 'm', "E8C048" },
    };
    static readonly string[] Shield10 =
    {
        "ZZZZZaaaAA", "ZWuuuccccA", "ZuuuucccCA", "ZmmmmmmmmA", "ZuuuucccCA", "ZuuuucccCA", ".ZuuuccCA.", ".ZuuuccCA.", "..ZuucCA..", "...ZuCA...", "....ZA....",
    };

    public static Bitmap Icon(int i) { return Icon(i, 0); }   // buffs: blue badge, literal multi-colour symbols (style 0)

    // style B palette: W FFFFFF, light gold FFE9A0, gold E8C048, dark gold C8962E, dark green faces 1E4A2A / 2E6A3A / 3E8A4E
    static readonly Dictionary<string, string> GreenMap = new Dictionary<string, string>
    {
        // steel shield -> gold, navy face -> green
        { "C8D0DC", "FFE9A0" }, { "F0F4F8", "FFFFFF" }, { "8E98A8", "E8C048" }, { "5A6272", "C8962E" },
        { "4A58A0", "3E8A4E" }, { "2E3A78", "2E6A3A" }, { "1E2450", "1E4A2A" }, { "E8C048", "FFE9A0" },
        // mana bubble violet -> gold ring, dark green core
        { "D860C8", "E8C048" }, { "7A3AA8", "C8962E" }, { "4A2A70", "1E4A2A" }, { "FFB8F0", "FFFFFF" },
        // boot + wing + speed lines
        { "4E3020", "C8962E" }, { "7A4E2E", "E8C048" }, { "A06A40", "FFE9A0" }, { "D8F0FF", "FFFFFF" }, { "8CC8F0", "FFE9A0" }, { "4A8AC8", "E8C048" },
        { "B8F078", "FFFFFF" }, { "6AC040", "FFE9A0" }, { "2E7A2E", "C8962E" },
        // hood / smoke greys
        { "C0C4D0", "FFFFFF" }, { "9A94A6", "FFE9A0" }, { "6A6476", "C8962E" }, { "1C1A22", "0E1A12" },
        // blade
        { "A0AABA", "FFE9A0" }, { "6A6876", "C8962E" }, { "B02838", "C8962E" }, { "1C0A10", "0E1A12" },
        // reflect shield already gold
        { "F0C850", "FFE9A0" }, { "C89A30", "C8962E" }, { "FFF4C8", "FFFFFF" },
    };
    // per-icon accents: 5 DR arrow -> bright white on the shield; 9 Empowered aura/x2 -> gold/white. 7 Stealth eyes, 8 Dodge slash keep red.
    static readonly Dictionary<int, Dictionary<string, string>> GreenOverride = new Dictionary<int, Dictionary<string, string>>
    {
        { 5, new Dictionary<string, string> { { "FF6060", "FFFFFF" }, { "C82838", "FFE9A0" }, { "FFE0E0", "FFFFFF" } } },
        { 9, new Dictionary<string, string> { { "C82838", "C8962E" }, { "FFE0E0", "FFFFFF" }, { "FF6060", "FFE9A0" }, { "6A1020", "1E4A2A" } } },
    };

    // style 0 = v3 colours ; 1 = green badge + white/gold 3-tone symbol ; 2 = blue badge + white/ice 3-tone symbol (buffs only)
    public static Bitmap Icon(int i, int style)
    {
        var cv = Canvas();
        switch (i)
        {
            case 0: // Stun: dizzy swirl with three stars riding an orbit above it
                {
                    ArenKitSheets.Ring(cv, C("C89A30"), 12, 8, 7.5, 1.0, 0.4, (bx, by) => ((int)bx % 2) == 0);
                    for (double t = 0; t < 4.5 * Math.PI; t += 0.12)
                    {
                        double r = 0.55 * t; if (r > 6.2) break;
                        Set(cv, 12 + Math.Cos(t) * r, 15 + Math.Sin(t) * r * 0.85, t > 2.5 * Math.PI ? "F0F4F8" : "FFFFFF");
                    }
                    Star4(cv, 5, 8, "FFE060", "FFFFFF"); Star4(cv, 12, 5, "FFE060", "FFFFFF"); Star4(cv, 19, 8, "FFE060", "FFFFFF");
                    break;
                }
            case 1: // Bleed: fresh slash wound with blood dripping off it
                Line(cv, 6, 5, 17, 14, "6A1020", 3); Line(cv, 6, 5, 17, 14, "C82838", 2); Line(cv, 7, 6, 16, 13, "FF6060", 1);
                Grid(cv, 15, 15, new[] { ".R..", ".R..", "RrR.", "RwrR", ".RR." }, new Dictionary<char, string> { { 'R', "C82838" }, { 'r', "FF6060" }, { 'w', "FFE0E0" } });
                Grid(cv, 9, 16, new[] { ".R.", "RrR", "RwR", ".R." }, new Dictionary<char, string> { { 'R', "C82838" }, { 'r', "FF6060" }, { 'w', "FFE0E0" } });
                Set(cv, 12, 13, "C82838"); Set(cv, 10, 14, "C82838");
                break;
            case 2: // Slow: an icy-blue snail
                {
                    Disc(cv, 11, 11, 5.6, "2A4A7A"); Disc(cv, 11, 11, 4.7, "4A8AC8");
                    for (double t = 0; t < 3.6 * Math.PI; t += 0.15) { double r = 0.42 * t; if (r > 4.2) break; Set(cv, 11 + Math.Cos(t) * r, 11 + Math.Sin(t) * r, "D8F0FF"); }
                    Set(cv, 9, 8, "FFFFFF");
                    Line(cv, 4, 17, 19, 17, "8CC8F0"); Line(cv, 5, 18, 18, 18, "4A8AC8");
                    Grid(cv, 16, 12, new[] { "..d.d", "..d.d", ".pp..", "pppp.", "pppp.", "ppp.." }, new Dictionary<char, string> { { 'p', "8CC8F0" }, { 'd', "D8F0FF" } });
                    Set(cv, 19, 14, "1E2450");
                    Set(cv, 4, 6, "D8F0FF"); Set(cv, 3, 7, "8CC8F0"); Set(cv, 20, 5, "D8F0FF");
                    break;
                }
            case 3: // Taunt: red target reticle locked on, big "!" in the middle
                ArenKitSheets.Ring(cv, C("C82838"), 12, 12, 8, 1.4, 1.0, null);
                foreach (var t in new[] { new[] { 12, 2, 12, 5 }, new[] { 12, 19, 12, 22 }, new[] { 2, 12, 5, 12 }, new[] { 19, 12, 22, 12 } }) Line(cv, t[0], t[1], t[2], t[3], "FF6060");
                for (int y = 7; y <= 14; y++) { Set(cv, 11, y, "FF6060"); Set(cv, 12, y, y <= 8 ? "FFE0E0" : "C82838"); }
                Set(cv, 11, 16, "FF6060"); Set(cv, 12, 16, "C82838"); Set(cv, 11, 17, "C82838"); Set(cv, 12, 17, "6A1020");
                break;
            case 4: // Absorb: violet mana bubble around a small shield; red hits break apart on the bubble skin
                {
                    for (int y = 2; y <= 21; y++) for (int x = 2; x <= 21; x++)
                        {
                            double d = Math.Sqrt((x + 0.5 - 12) * (x + 0.5 - 12) + (y + 0.5 - 12) * (y + 0.5 - 12));
                            if (d <= 8.6 && d > 7.2) Set(cv, x, y, d > 7.9 ? "7A3AA8" : "D860C8");
                            else if (d <= 7.2 && (x + y) % 2 == 0) Set(cv, x, y, "3A2460");
                        }
                    ArenKitSheets.Ring(cv, C("FFB8F0"), 12, 12, 6.0, 1.0, 1.0, (bx, by) => { double a = Math.Atan2(by - 12, bx - 12) * 180 / Math.PI; return a > -165 && a < -110; });
                    Grid(cv, 8, 8, new[] { "ZZZZaaAA", "ZWuuccCA", "ZuuuccCA", "ZmmmmmmA", "ZuuuccCA", ".ZuuccA.", ".ZuccCA.", "..ZcCA..", "...ZA..." }, ShieldPal);
                    // incoming hits splintering on the bubble (top-right and bottom-left)
                    Line(cv, 22, 2, 19, 5, "FF6060"); Set(cv, 18, 6, "FFE0E0"); Set(cv, 20, 7, "FF6060"); Set(cv, 17, 4, "FF6060");
                    Line(cv, 1, 21, 4, 18, "FF6060"); Set(cv, 5, 17, "FFE0E0"); Set(cv, 3, 16, "FF6060"); Set(cv, 6, 19, "FF6060");
                    break;
                }
            case 5: // Damage reduction: a red arrow strikes the steel shield and shatters into chips
                {
                    Grid(cv, 3, 5, Shield10, ShieldPal);
                    // shaft coming in from the top-right, broken at the shield rim
                    Line(cv, 21, 3, 15, 8, "C82838", 2); Line(cv, 21, 3, 15, 8, "FF6060", 1);
                    // shattered head pieces bouncing off
                    Set(cv, 14, 9, "FFE0E0"); Set(cv, 16, 11, "FF6060"); Set(cv, 18, 10, "C82838"); Set(cv, 17, 13, "FF6060"); Set(cv, 20, 12, "C82838"); Set(cv, 15, 14, "C82838");
                    ArenKitSheets.Star(cv, new[] { "FFFFFF", "FFF4C8", "F0C850" }, 14, 8, 1);
                    // small "-" beside it: less damage
                    Line(cv, 16, 18, 20, 18, "FFFFFF"); Line(cv, 16, 19, 20, 19, "8CC8F0");
                    break;
                }
            case 6: // Haste: running boot, three bold speed streaks and a green "faster" up-arrow
                {
                    var pal = new Dictionary<char, string> { { 'B', "4E3020" }, { 'b', "7A4E2E" }, { 'l', "A06A40" }, { 'g', "E8C048" } };
                    Grid(cv, 9, 6, new[]
                    {
                        "gggg......", "bllb......", "blbb......", "blbb......", "blbb......", "blbbb.....", "bbbbbbb...", "bbbbbbbbbb", "bbbbbbbbbb", "BBBBBBBBBB",
                    }, pal);
                    Line(cv, 2, 9, 7, 9, "D8F0FF", 2); Line(cv, 3, 13, 8, 13, "8CC8F0", 2); Line(cv, 2, 17, 7, 17, "D8F0FF", 2);
                    // green up-arrow: pointed head widening downward (rows 2-4), shaft rows 5-9
                    for (int k = 0; k <= 2; k++) for (int x = 18 - k; x <= 19 + k; x++) Set(cv, x, 2 + k, x <= 18 ? "B8F078" : "6AC040");
                    for (int y = 5; y <= 9; y++) { Set(cv, 18, y, "B8F078"); Set(cv, 19, y, "6AC040"); }
                    break;
                }
            case 7: // Stealth: an eye struck through — "cannot be seen"
                {
                    var eye = new Dictionary<char, string> { { 'o', "C0C4D0" }, { 'O', "9A94A6" }, { 'w', "F0F4F8" }, { 'p', "3A3444" }, { 'i', "6A6476" } };
                    Grid(cv, 3, 7, new[]
                    {
                        "......oooooo......", "....oowwwwwwoo....", "..oowwwiiiiwwwoo..", ".owwwwippppiwwwwo.", "OwwwwwippwpiwwwwwO",
                        ".OwwwwippppiwwwwO.", "..OOwwwiiiiwwwOO..", "....OOwwwwwwOO....", "......OOOOOO......",
                    }, eye);
                    Line(cv, 4, 20, 19, 3, "6A1020", 3); Line(cv, 4, 20, 19, 3, "C82838", 2); Line(cv, 5, 19, 19, 4, "FF6060", 1);
                    break;
                }
            case 8: // Dodge ready: the red slash cuts only an afterimage; the real figure has leaned away. "MISS"
                {
                    var g = new Dictionary<char, string[]>
                    {
                        { 'M', new[] { "#.#", "###", "#.#", "#.#", "#.#" } }, { 'I', new[] { "#", "#", "#", "#", "#" } }, { 'S', new[] { "###", "#..", "###", "..#", "###" } },
                    };
                    int tx = 6;
                    foreach (var ch in "MISS") { var gl = g[ch]; for (int y = 0; y < 5; y++) for (int x = 0; x < gl[y].Length; x++) if (gl[y][x] == '#') { Set(cv, tx + x + 1, 3 + y + 1, "1E3A5A"); Set(cv, tx + x, 3 + y, "FFFFFF"); } tx += gl[0].Length + 1; }
                    // simple figure 6x12: head, torso, two legs
                    var figRows = new[] { "..##..", ".####.", ".####.", "..##..", ".####.", "######", "#.##.#", "..##..", "..##..", ".#..#.", ".#..#.", "##..##" };
                    for (int y = 0; y < 12; y++) for (int x = 0; x < 6; x++) if (figRows[y][x] == '#')
                            {
                                if ((x + y) % 2 == 0) Set(cv, 4 + x, 10 + y, "4A7AAA");          // afterimage (dithered) — what the slash hits
                                Set(cv, 15 + x, 10 + y, y < 4 ? "F0F4F8" : y < 9 ? "D8F0FF" : "8CC8F0");   // real figure, already stepped aside
                            }
                    Line(cv, 11, 15, 14, 15, "D8F0FF"); Line(cv, 11, 17, 13, 17, "8CC8F0");   // sidestep streaks
                    Line(cv, 3, 20, 11, 10, "6A1020", 3); Line(cv, 3, 20, 11, 10, "C82838", 2); Line(cv, 4, 19, 11, 11, "FF6060", 1);
                    break;
                }
            case 9: // Empowered: big bold "x2" over an orange power burst
                {
                    for (int k = 0; k < 8; k++)
                    {
                        double a = k * Math.PI / 4 + 0.39, r0 = 6, r1 = k % 2 == 0 ? 10.5 : 8.5;
                        Line(cv, 12 + Math.Cos(a) * r0, 12 + Math.Sin(a) * r0, 12 + Math.Cos(a) * r1, 12 + Math.Sin(a) * r1, k % 2 == 0 ? "F0A050" : "C8692E");
                    }
                    var big = new Dictionary<char, string[]>
                    {
                        { 'x', new[] { "#...#", "##.##", ".###.", ".###.", "##.##", "#...#" } },
                        { '2', new[] { ".###.", "##.##", "...##", "..##.", ".##..", "#####" } },
                    };
                    int ox = 6, oy = 9;
                    foreach (var ch in "x2")
                    {
                        var gl = big[ch];
                        for (int y = 0; y < 6; y++) for (int x = 0; x < 5; x++) if (gl[y][x] == '#') Set(cv, ox + x + 1, oy + y + 1, "6A1020");
                        for (int y = 0; y < 6; y++) for (int x = 0; x < 5; x++) if (gl[y][x] == '#') Set(cv, ox + x, oy + y, y < 2 ? "FFE0E0" : ch == 'x' ? "FF6060" : "FFE060");
                        ox += 7;
                    }
                    break;
                }
            default: // Reflect: a red arrow flies into the shield; a gold arrow flies straight back at the attacker
                {
                    Grid(cv, 13, 5, new[] { "ZZZZaaAA", "ZWuuccCA", "ZuuuccCA", "ZmmmmmmA", "ZuuuccCA", "ZuuuccCA", ".ZuuccA.", ".ZuccCA.", "..ZcCA..", "...ZA..." }, ShieldPal);
                    // incoming (red) along the upper lane, pointing at the shield
                    Line(cv, 3, 8, 11, 8, "C82838", 1); Line(cv, 3, 9, 11, 9, "FF6060", 1);
                    Set(cv, 12, 8, "FFE0E0"); Set(cv, 12, 9, "FFE0E0"); Set(cv, 11, 7, "FF6060"); Set(cv, 11, 10, "FF6060");
                    // outgoing (gold) along the lower lane, pointing back left
                    Line(cv, 4, 15, 12, 15, "F0C850", 1); Line(cv, 4, 16, 12, 16, "C89A30", 1);
                    Set(cv, 3, 15, "FFF4C8"); Set(cv, 3, 16, "FFF4C8"); Set(cv, 4, 14, "F0C850"); Set(cv, 4, 17, "F0C850"); Set(cv, 5, 13, "F0C850"); Set(cv, 5, 18, "C89A30");
                    ArenKitSheets.Star(cv, new[] { "FFFFFF", "FFF4C8", "F0C850" }, 13, 12, 1);
                    break;
                }
        }
        // rounded-square badge with a 1-px transparent margin
        bool debuff = Debuff[i];
        string fill = debuff ? "2A1218" : "121E2C", rim = debuff ? "8A2030" : "3A6A9A", rimHi = debuff ? "C04050" : "6A9AC8";
        if (!debuff && style == 1) { fill = "10241A"; rim = "2E7A3E"; rimHi = "6AC878"; }
        string[] tone = !debuff && style == 1 ? new[] { "FFFFFF", "FFE9A0", "C8962E" } : !debuff && style == 2 ? new[] { "FFFFFF", "BEE4FF", "5A9AD8" } : null;
        var bmp = new Bitmap(S, S, PixelFormat.Format32bppArgb);
        Func<int, int, bool> inside = (x, y) =>
        {
            if (x < 1 || y < 1 || x > 22 || y > 22) return false;
            int dx = Math.Min(x - 1, 22 - x), dy = Math.Min(y - 1, 22 - y);
            return dx + dy >= 2;
        };
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                if (!inside(x, y)) continue;
                bool rimPx = !inside(x - 1, y) || !inside(x + 1, y) || !inside(x, y - 1) || !inside(x, y + 1);
                Color c = rimPx ? C(x + y < 23 ? rimHi : rim) : C(fill);
                if (!rimPx && cv.Px[x, y].HasValue)
                {
                    c = cv.Px[x, y].Value;
                    if (!debuff && style == 1)
                    {
                        // hand-mapped style B: white / gold symbols on green; key meaning accents kept (see GreenMap)
                        string hx = string.Format("{0:X2}{1:X2}{2:X2}", c.R, c.G, c.B), to;
                        if (GreenOverride.ContainsKey(i) && GreenOverride[i].TryGetValue(hx, out to)) c = C(to);
                        else if (GreenMap.TryGetValue(hx, out to)) c = C(to);
                    }
                    else if (tone != null) { double lum = 0.3 * c.R + 0.59 * c.G + 0.11 * c.B; c = C(lum > 175 ? tone[0] : lum > 95 ? tone[1] : tone[2]); }
                }
                bmp.SetPixel(x, y, c);
            }
        return bmp;
    }
}
