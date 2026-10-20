using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

// Skill hotbar icons v2: 32x32, 1-px transparent margin, class-themed frame (bevel + corner studs + banded backing
// + faint class sigil), motif built from the hero's real gear / body with the kit's VFX colours, dark outline.
// Hotbar slot = 68 px @1080p -> show the icon at 64 px (2x, crisp).
public static class IconGen
{
    public const int S = 32;
    static Color C(string h) { return HeroConceptGen.Hex(h); }

    public class Theme
    {
        public string Outline, BevelHi, BevelMid, BevelLo, Stud, StudHi;
        public string[] Bands;            // top / mid / bottom backing
        public Func<int, int, bool> Sigil; // faint watermark mask (lightens the backing)
    }

    public static readonly Dictionary<string, Theme> Themes = new Dictionary<string, Theme>
    {
        // Aren: polished steel frame, gold rivets, night-navy field, faint knight's cross
        { "Aren", new Theme { Outline = "0E1428", BevelHi = "C8D0DC", BevelMid = "8E98A8", BevelLo = "5A6272", Stud = "E8C048", StudHi = "FFF4C8",
            Bands = new[] { "26326A", "1C2652", "141A3C" },
            Sigil = (x, y) => (x >= 14 && x <= 17 && y >= 6 && y <= 25) || (y >= 11 && y <= 13 && x >= 8 && x <= 23) } },
        // Brom: forged iron frame, brass rivets, warm forge-brown field, faint anvil
        { "Brom", new Theme { Outline = "140C08", BevelHi = "8A94A4", BevelMid = "5A6070", BevelLo = "3A3E48", Stud = "C89A30", StudHi = "F0C850",
            Bands = new[] { "4A2E1C", "3A2416", "2A1A10" },
            Sigil = (x, y) => (y >= 9 && y <= 12 && x >= 7 && x <= 24) || (y >= 13 && y <= 18 && x >= 13 && x <= 18) || (y >= 19 && y <= 22 && x >= 10 && x <= 21) } },
        // Lyra: carved wood frame, leaf-green studs, deep forest field, faint leaf
        { "Lyra", new Theme { Outline = "10200E", BevelHi = "B07A48", BevelMid = "8A5A34", BevelLo = "5A3820", Stud = "6AC040", StudHi = "B8F078",
            Bands = new[] { "2E5A2E", "22462A", "18341E" },
            Sigil = (x, y) => Math.Abs(x - 15.5) * 1.7 + Math.Abs(y - 15.5) * 0.85 <= 10 || (Math.Abs(x - 15.5) < 0.6 && y > 15 && y < 28) } },
        // Selene: violet-silver frame, gold star studs, night-violet field, faint crescent moon
        { "Selene", new Theme { Outline = "120C22", BevelHi = "D0C4EE", BevelMid = "7E66B0", BevelLo = "4A3A6A", Stud = "E8C048", StudHi = "FFF0A0",
            Bands = new[] { "3A2A5A", "2A1E46", "1E1636" },
            Sigil = (x, y) => { double a = (x - 15.5) * (x - 15.5) + (y - 15.5) * (y - 15.5), b = (x - 19.5) * (x - 19.5) + (y - 13.5) * (y - 13.5); return a <= 100 && b > 64; } } },
        // Vex: blackened steel frame, crimson studs, black-red field, faint X
        { "Vex", new Theme { Outline = "08060A", BevelHi = "8A88A0", BevelMid = "46444F", BevelLo = "2E2C36", Stud = "C82838", StudHi = "FF6060",
            Bands = new[] { "3A1420", "2A0E18", "1C0A10" },
            Sigil = (x, y) => (Math.Abs((x - 15.5) - (y - 15.5)) <= 1.6 || Math.Abs((x - 15.5) + (y - 15.5)) <= 1.6) && Math.Abs(x - 15.5) <= 10 } },
    };

    public static readonly string[] BromFx = { "FFF0C8", "F0A050", "C8692E", "7A4426", "D8D8E0" };
    public static readonly string[] LyraFx = { "F8FFE0", "B8F078", "6AC040", "2E7A2E", "F0D060" };
    public static readonly string[] SeleneFx = { "FFFFFF", "FFB8F0", "D860C8", "7A3AA8", "B8D8FF" };
    public static readonly string[] VexFx = { "FFE0E0", "FF6060", "C82838", "6A1020", "C0C4D0" };

    static HeroConceptGen.Concept Hero(string id) { return HeroConceptGen.Roster.First(r => r.Id == id); }

    // paint rows [r0, r1) of a 14-wide roster sprite at (ox, oy); fill != null paints a flat silhouette instead
    static void Body(ArenKitSheets.Cv cv, string[] rows, Dictionary<char, string> pal, int ox, int oy, int r0, int r1, string fill)
    {
        for (int y = r0; y < r1; y++) for (int x = 0; x < rows[y].Length; x++)
                if (rows[y][x] != '.') cv.Set(ox + x, oy + y - r0, C(fill ?? pal[rows[y][x]]));
    }

    // straight arrow drawn along an angle (deg), head at (x, y)
    static void ArrowAt(ArenKitSheets.Cv cv, double x, double y, double ang, int len, string shaft, string head, string fletch)
    {
        double dx = Math.Cos(ang * Math.PI / 180), dy = Math.Sin(ang * Math.PI / 180);
        for (int i = 0; i < len; i++) cv.Set(x - dx * i, y - dy * i, C(i < 2 ? head : (i >= len - 2 ? fletch : shaft)));
        double fx = x - dx * (len - 1), fy = y - dy * (len - 1);
        cv.Set(fx - dy, fy + dx, C(fletch)); cv.Set(fx + dy, fy - dx, C(fletch));
        cv.Set(x + dx, y + dy, C(head));
    }

    static void Leaf(ArenKitSheets.Cv cv, double x, double y, int turn)
    {
        cv.Set(x, y, C(LyraFx[1])); cv.Set(x + 1, y, C(LyraFx[2]));
        cv.Set(x + (turn % 2 == 0 ? 1 : 0), y + (turn < 2 ? 1 : -1), C(LyraFx[3]));
    }

    static void Smoke(ArenKitSheets.Cv cv, double x, double y, double r)
    {
        ArenKitSheets.Disc(cv, C("3A3444"), x, y, r);
        ArenKitSheets.Disc(cv, C("6A6476"), x - r * 0.3, y - r * 0.3, r * 0.55);
    }

    static EquipGen.Spr VexDagger(int dir, int len)
    {
        var def = new EquipGen.BladeDef { Id = "icon", Name = "icon", Length = len, GuardHalf = 1, Core = "D8DCE4", Band = "8A90A0", Guard = "6A6876", Wrap = "B02838", Pommel = "6A6876", Outline = "0E0C12" };
        return EquipGen.Blade(def, dir);
    }

    public static readonly string[] Ice = ArenKitSheets.Ice;    // FFFFFF D8F0FF 8CC8F0 4A8AC8
    public static readonly string[] Gold = ArenKitSheets.Gold;  // FFFFFF FFF4C8 F0C850 C89A30

    public static ArenKitSheets.Cv Canvas() { return new ArenKitSheets.Cv(S, S, 0, 0); }

    // tile = 1..30 with a 1-px bevel cut at the corners; frame rings: 0 outline, 1 bevel (hi top-left / lo bottom-right), 2 mid
    static int Ring(int x, int y)
    {
        if (x < 1 || y < 1 || x > 30 || y > 30) return -1;
        int dx = Math.Min(x - 1, 30 - x), dy = Math.Min(y - 1, 30 - y);
        if (dx + dy < 1) return -1;
        int d = Math.Min(dx, dy);
        if (dx + dy < 2) return 0;
        return Math.Min(d, 3);   // 0,1,2 frame ; 3 = inside
    }

    static Color Lighten(Color c, int k) { return Color.FromArgb(255, Math.Min(255, c.R + k), Math.Min(255, c.G + k), Math.Min(255, c.B + k)); }

    public static Bitmap Finish(ArenKitSheets.Cv motif, Theme t)
    {
        var bmp = new Bitmap(S, S, PixelFormat.Format32bppArgb);
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                int r = Ring(x, y); if (r < 0) continue;
                Color c;
                if (r == 0) c = C(t.Outline);
                else if (r == 1) c = C((x + y) < 31 ? t.BevelHi : t.BevelLo);
                else if (r == 2) c = C(t.BevelMid);
                else
                {
                    c = C(t.Bands[y < 12 ? 0 : y < 21 ? 1 : 2]);
                    if (t.Sigil != null && t.Sigil(x, y)) c = Lighten(c, 10);
                }
                bmp.SetPixel(x, y, c);
            }
        // corner rivets (2x2 on the bevel, highlight top-left pixel)
        foreach (var p in new[] { new Point(2, 2), new Point(28, 2), new Point(2, 28), new Point(28, 28) })
        {
            bmp.SetPixel(p.X, p.Y, C(t.StudHi)); bmp.SetPixel(p.X + 1, p.Y, C(t.Stud)); bmp.SetPixel(p.X, p.Y + 1, C(t.Stud)); bmp.SetPixel(p.X + 1, p.Y + 1, C(t.Stud));
        }
        // dark outline around the motif (inside the field only)
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
            {
                if (Ring(x, y) != 3 || motif.Px[x, y].HasValue) continue;
                bool near = false;
                foreach (var n in new[] { new Point(x + 1, y), new Point(x - 1, y), new Point(x, y + 1), new Point(x, y - 1) })
                    if (n.X >= 0 && n.Y >= 0 && n.X < S && n.Y < S && motif.Px[n.X, n.Y].HasValue) near = true;
                if (near) bmp.SetPixel(x, y, C(t.Outline));
            }
        for (int y = 0; y < S; y++) for (int x = 0; x < S; x++)
                if (motif.Px[x, y].HasValue && Ring(x, y) == 3) bmp.SetPixel(x, y, motif.Px[x, y].Value);
        return bmp;
    }

    static void Grid(ArenKitSheets.Cv cv, int ox, int oy, string[] rows, Dictionary<char, string> pal)
    {
        for (int y = 0; y < rows.Length; y++) for (int x = 0; x < rows[y].Length; x++)
            {
                char c = rows[y][x]; if (c == '.') continue;
                cv.Set(ox + x, oy + y, C(pal[c]));
            }
    }

    static void Spr(ArenKitSheets.Cv cv, EquipGen.Spr s, int ox, int oy)
    {
        foreach (var kv in s.Px) cv.Set(ox + kv.Key.X, oy + kv.Key.Y, kv.Value);
    }

    // arc of a ring (angles in degrees, screen y down)
    static void Arc(ArenKitSheets.Cv cv, string col, double cx, double cy, double R, double th, double a0, double a1)
    {
        ArenKitSheets.Ring(cv, C(col), cx, cy, R, th, 1.0, (bx, by) =>
        {
            double a = Math.Atan2(by - cy, bx - cx) * 180 / Math.PI;
            if (a0 <= a1) return a >= a0 && a <= a1;
            return a >= a0 || a <= a1;
        });
    }

    // ---------------- Aren gear (icon scale) ----------------
    static readonly Dictionary<char, string> ArenShieldPal = new Dictionary<char, string>
    {
        { 'Z', "C8D0DC" }, { 'W', "F0F4F8" }, { 'a', "8E98A8" }, { 'A', "5A6272" },
        { 'u', "4A58A0" }, { 'c', "2E3A78" }, { 'C', "1E2450" }, { 'm', "E8C048" }, { 'y', "FFF4C8" },
    };
    static readonly string[] ShieldBig =
    {
        "ZZZZZaaaaAAA",
        "ZWuuuucccccA",
        "ZuuuucccccCA",
        "ZuuuucccccCA",
        "ZuuuuccccCCA",
        "ZymmmmmmmmmA",
        "ZuuuucccccCA",
        "ZuuuuccccCCA",
        ".ZuuucccccA.",
        ".ZuuuccccCA.",
        "..ZuucccCA..",
        "..ZuuccCCA..",
        "...ZucCA....",
        "....ZCA.....",
        ".....A......",
    };
    static readonly string[] ShieldSmall =
    {
        "ZZZZaaAA",
        "ZWuucccA",
        "ZuuuccCA",
        "ZymmmmmA",
        "ZuucccCA",
        ".ZuuccA.",
        ".ZuccCA.",
        "..ZcCA..",
        "...ZA...",
    };

    static EquipGen.Spr ArenSword(int dir, int len)
    {
        var def = new EquipGen.BladeDef { Id = "icon", Name = "icon", Length = len, GuardHalf = 2, Core = "F0F4F8", Band = "A0AABA", Guard = "E8C048", Wrap = "2E3A78", Pommel = "E8C048", Outline = "1A1C24" };
        return EquipGen.Blade(def, dir);
    }

    // ================= Aren (white / ice / gold) =================
    public static Bitmap Aren(int i)
    {
        var cv = Canvas(); var t = Themes["Aren"];
        if (i == 0)
        {
            // 2 Shield Bash: Aren's crest shield thrust forward + gold shock cone + stun sparks
            Grid(cv, 5, 9, ShieldBig, ArenShieldPal);
            Arc(cv, Gold[1], 16, 16, 5, 1.2, -55, 55);
            Arc(cv, Gold[2], 16, 16, 8.5, 1.4, -50, 50);
            Arc(cv, Gold[3], 16, 16, 12, 1.4, -45, 45);
            ArenKitSheets.Star(cv, new[] { "FFFFFF", "FFF4C8", "F0C850" }, 24, 7, 2);
            ArenKitSheets.Star(cv, new[] { "FFFFFF", "FFF4C8", "F0C850" }, 26, 23, 1);
            cv.Set(6, 6, C(Gold[1])); cv.Set(9, 5, C(Gold[0]));
        }
        else if (i == 1)
        {
            // 3 Charge: Aren (side view, shield forward) with two ice afterimages and kicked-up dust
            var pal = HeroConceptGen.Roster.First(r => r.Id == "4").Pal;
            var rows = ArenIdleGen.Side; int bx = 12, by = 6;
            foreach (var ghost in new[] { Tuple.Create(-8, Ice[3]), Tuple.Create(-4, Ice[2]) })
                for (int y = 0; y < 21; y++) for (int x = 0; x < 14; x++)
                        if (rows[y][x] != '.') cv.Set(bx + ghost.Item1 + x, by + y, C(ghost.Item2));
            for (int y = 0; y < 21; y++) for (int x = 0; x < 14; x++)
                    if (rows[y][x] != '.') cv.Set(bx + x, by + y, C(pal[rows[y][x]]));
            Grid(cv, bx + 11, by + 10, new[] { "ZZaA", "ZucA", "ZucA", "mmmA", "ZucA", "uccA", ".cCA", ".cA." }, ArenShieldPal);
            foreach (var d in new[] { new[] { 5.0, 26.0, 1.6 }, new[] { 8.0, 27.0, 1.2 }, new[] { 3.0, 27.5, 1.0 } })
            {
                ArenKitSheets.Disc(cv, C("8A7A6A"), d[0], d[1], d[2]); cv.Set(d[0] - 0.5, d[1] - 0.8, C("B8A898"));
            }
            for (int k = 0; k < 3; k++) ArenKitSheets.Line(cv, C(k == 1 ? Ice[0] : Ice[1]), 4, 10 + k * 5, 8, 10 + k * 5, 1);
        }
        else if (i == 2)
        {
            // 4 Whirl Slash: Aren's longsword at the centre of a spinning ice ring (3 blade arcs)
            for (int k = 0; k < 3; k++)
            {
                double a0 = -80 + k * 120;
                ArenKitSheets.Crescent(cv, Ice, 16, 15.5, 11, a0 + 95, 95, 3.0);
            }
            Spr(cv, ArenSword(1, 12), 11, 20);
            ArenKitSheets.Star(cv, new[] { "FFFFFF", "D8F0FF", "8CC8F0" }, 25, 7, 1);
        }
        else if (i == 3)
        {
            // V Bulwark: Aren's shield inside a golden hexagon barrier with a faint honeycomb
            Func<double, double, double> hex = (x, y) => { double ax = Math.Abs(x - 16), ay = Math.Abs(y - 16); return Math.Max(ay / 0.866, ax + ay * 0.577); };
            for (int y = 3; y < 29; y++) for (int x = 3; x < 29; x++)
                {
                    double h = hex(x + 0.5, y + 0.5);
                    if (h <= 13.2 && h > 11.2) cv.Set(x, y, C(h > 12.3 ? Gold[3] : Gold[2]));
                    else if (h <= 8.4 && h > 7.6) cv.Set(x, y, C(Gold[3]));
                }
            for (int k = 0; k < 6; k++)
            {
                double a = k * Math.PI / 3;
                ArenKitSheets.Line(cv, C(Gold[3]), 16 + Math.Cos(a) * 8.2, 16 + Math.Sin(a) * 8.2 * 0.95, 16 + Math.Cos(a) * 11.4, 16 + Math.Sin(a) * 11.4 * 0.95, 1);
            }
            Grid(cv, 12, 11, ShieldSmall, ArenShieldPal);
            ArenKitSheets.Star(cv, new[] { "FFFFFF", "FFF4C8", "F0C850" }, 23, 6, 2);
            cv.Set(8, 22, C(Gold[1])); cv.Set(7, 21, C(Gold[0]));
        }
        else
        {
            // C Dawnbreaker: dawn rays behind, huge white-gold crescent, Aren's sword raised through it
            for (int k = 0; k < 10; k++)
            {
                double a = (k * 36 - 90) * Math.PI / 180, r1 = k % 2 == 0 ? 13 : 9;
                ArenKitSheets.Line(cv, C(k % 2 == 0 ? Gold[1] : Gold[3]), 20 + Math.Cos(a) * 4, 11 + Math.Sin(a) * 4, 20 + Math.Cos(a) * r1, 11 + Math.Sin(a) * r1, 1);
            }
            ArenKitSheets.Disc(cv, C(Gold[2]), 20, 11, 3.6);
            ArenKitSheets.Disc(cv, C(Gold[1]), 19.5, 10.5, 2.2);
            ArenKitSheets.Crescent(cv, new[] { "FFFFFF", "FFF4C8", "F0C850", "C89A30" }, 15, 17, 11, -160, 170, 4.6);
            Spr(cv, ArenSword(1, 10), 8, 24);
        }
        return Finish(cv, t);
    }

    public static readonly string[] ArenNames = { "2 Khien Dap", "3 Xung Phong", "4 Loc Kiem", "V Thanh Luy", "C Chem Binh Minh" };

    // ================= Brom (forge orange / earth / steel) =================
    static readonly Dictionary<char, string> Steel = new Dictionary<char, string> { { 'W', "F0F4F8" }, { 'Z', "C8D0DC" }, { 'a', "8A94A4" }, { 'A', "5A6070" }, { 'm', "E8C048" }, { 'g', "F0E0A0" }, { 'b', "7A4E2E" }, { 'l', "A06A40" } };

    public static Bitmap Brom(int i)
    {
        var cv = Canvas(); var t = Themes["Brom"]; var fx = BromFx;
        if (i == 0)
        {
            // 2 Ground Slam: Brom's hammer smashing into a cracked, glowing ground ring; rocks fly
            ArenKitSheets.Ring(cv, C(fx[2]), 15, 24, 11, 1.6, 0.42, null);
            ArenKitSheets.Ring(cv, C(fx[1]), 15, 24, 7, 1.2, 0.42, null);
            foreach (var a in new[] { 200, 250, 290, 340 }) { double r = a * Math.PI / 180; ArenKitSheets.Line(cv, C(fx[3]), 15, 24, 15 + Math.Cos(r) * 9, 24 + Math.Sin(r) * 4, 1); }
            for (int k = 0; k < 9; k++) ArenKitSheets.Line(cv, C(k % 3 == 0 ? "A06A40" : "7A4E2E"), 25 - k * 0.7, 4 + k, 26 - k * 0.7, 4 + k, 1);
            Grid(cv, 10, 13, new[] { "ZZZZZZZZZa", "ZWZZmmZZaA", "ZZaammaaAA", "ZaaammaaAA", "aaaammaAAA", ".AAAAAAAA." }, Steel);
            ArenKitSheets.Disc(cv, C(fx[0]), 15, 22, 1.4);
            foreach (var r in new[] { new[] { 6, 14 }, new[] { 24, 15 }, new[] { 8, 9 }, new[] { 22, 10 } }) { ArenKitSheets.Disc(cv, C("8A8A8A"), r[0], r[1], 1.2); cv.Set(r[0] - 1, r[1] - 1, C("A8A8A8")); }
            ArenKitSheets.Star(cv, new[] { "FFFFFF", fx[0], fx[1] }, 5, 21, 1); ArenKitSheets.Star(cv, new[] { "FFFFFF", fx[0], fx[1] }, 26, 21, 1);
        }
        else if (i == 1)
        {
            // 3 Shield Charge: Brom's big round shield ramming right, gold rim flare, dust trail behind
            var pal = new Dictionary<char, string> { { 'm', "E8C048" }, { 'y', "FFF4C8" }, { 'Z', "C8D0DC" }, { 'a', "8A94A4" }, { 'A', "5A6070" }, { 'g', "F0E0A0" } };
            Grid(cv, 11, 8, new[]
            {
                "....mmmmm....", "..mmZZaaamm..", ".mZZaaaaaaAm.", ".mZaaaaaaaAm.", "mZaaagggaaaAm", "mZaagyyggaaAm", "mZaagyygaaAAm",
                "mZaaggggaaAAm", "maaaaaaaaaAAm", ".maaaaaaaAAm.", ".mAaaaaaAAAm.", "..mmAAAAAmm..", "....mmmmm....",
            }, pal);
            ArenKitSheets.Ring(cv, C("FFF4C8"), 17, 14.5, 8.2, 1.0, 1.0, (bx, by) => Math.Atan2(by - 14.5, bx - 17) * 180 / Math.PI < -30 && Math.Atan2(by - 14.5, bx - 17) * 180 / Math.PI > -120);
            ArenKitSheets.Star(cv, new[] { "FFFFFF", "FFF4C8", "F0C850" }, 25, 6, 2);
            for (int k = 0; k < 3; k++) ArenKitSheets.Line(cv, C(k == 1 ? fx[1] : fx[2]), 4, 10 + k * 5, 9, 10 + k * 5, 1);
            foreach (var d in new[] { new[] { 6.0, 25.0, 2.0 }, new[] { 10.0, 26.0, 1.5 }, new[] { 4.0, 21.0, 1.2 } }) { ArenKitSheets.Disc(cv, C("A08060"), d[0], d[1], d[2]); cv.Set(d[0] - 0.7, d[1] - 0.7, C("C8A880")); }
        }
        else if (i == 2)
        {
            // 4 Taunt Roar: Brom's horned helm and beard bellowing, orange-red sound rings and a red "!"
            var h = Hero("1");
            Body(cv, h.Rows, h.Pal, 4, 9, 0, 15, null);
            Arc(cv, fx[1], 11, 16, 8.5, 1.3, -55, 55); Arc(cv, fx[2], 11, 16, 12, 1.3, -50, 50); Arc(cv, "B83A2A", 11, 16, 15.5, 1.3, -45, 45);
            for (int y = 4; y <= 9; y++) { cv.Set(24, y, C("D83A2A")); cv.Set(25, y, C(y < 6 ? "FF7A5A" : "B82A1A")); }
            cv.Set(24, 11, C("D83A2A")); cv.Set(25, 11, C("B82A1A")); cv.Set(24, 12, C("B82A1A")); cv.Set(25, 12, C("8A1A10"));
        }
        else if (i == 3)
        {
            // V Iron Wall: riveted steel dome over Brom's horned helm
            var h = Hero("1");
            Body(cv, h.Rows, h.Pal, 9, 15, 0, 11, null);
            ArenKitSheets.Ring(cv, C("5A6070"), 16, 25, 12, 2.2, 1.15, (bx, by) => by <= 25);
            ArenKitSheets.Ring(cv, C("C8D0DC"), 16, 25, 10.8, 1.0, 1.15, (bx, by) => by <= 25 && bx < 16);
            ArenKitSheets.Ring(cv, C("8A94A4"), 16, 25, 10.8, 1.0, 1.15, (bx, by) => by <= 25 && bx >= 16);
            ArenKitSheets.Line(cv, C("5A6070"), 4, 26, 27, 26, 2);
            foreach (var a in new[] { 200, 235, 270, 305, 340 }) { double r = a * Math.PI / 180; cv.Set(16 + Math.Cos(r) * 12, 25 + Math.Sin(r) * 12 * 1.15, C("F0F4F8")); }
            ArenKitSheets.Star(cv, new[] { "FFFFFF", "F0F4F8", "C8D0DC" }, 9, 9, 2);
        }
        else
        {
            // C Earthquake: three spreading crack rings, a jagged split and rocks thrown up
            for (int k = 0; k < 3; k++) ArenKitSheets.Ring(cv, C(k == 0 ? fx[1] : k == 1 ? fx[2] : fx[3]), 16, 21, 5 + k * 4.5, 1.4, 0.45, (bx, by) => k < 2 || ((int)bx % 4) != 0);
            var pts = new[] { new Point(6, 13), new Point(10, 17), new Point(13, 15), new Point(16, 21), new Point(19, 18), new Point(22, 23), new Point(27, 20) };
            for (int k = 0; k < pts.Length - 1; k++) ArenKitSheets.Line(cv, C(fx[0]), pts[k].X, pts[k].Y, pts[k + 1].X, pts[k + 1].Y, 1);
            foreach (var r in new[] { new[] { 8, 7, 2 }, new[] { 16, 5, 2 }, new[] { 24, 9, 1 }, new[] { 12, 10, 1 }, new[] { 21, 4, 1 } })
            { ArenKitSheets.Disc(cv, C("8A8A8A"), r[0], r[1], r[2] * 0.9 + 0.4); cv.Set(r[0] - 1, r[1] - 1, C("A8A8A8")); }
            ArenKitSheets.Star(cv, new[] { "FFFFFF", fx[0], fx[1] }, 16, 21, 2);
        }
        return Finish(cv, t);
    }

    // ================= Lyra (leaf green / gold) =================
    public static Bitmap Lyra(int i)
    {
        var cv = Canvas(); var t = Themes["Lyra"]; var fx = LyraFx;
        string sh = "B07A48", hd = "D8DCE4";
        if (i == 0)
        {
            // 2 Fan Shot: five arrows fanning out from the bow hand with green trails
            foreach (var a in new[] { -44, -22, 0, 22, 44 })
            {
                double r = a * Math.PI / 180, ex = 6 + Math.Cos(r) * 20, ey = 16 + Math.Sin(r) * 13;
                ArenKitSheets.Line(cv, C(a == 0 ? fx[1] : fx[3]), 6 + Math.Cos(r) * 5, 16 + Math.Sin(r) * 3, ex - Math.Cos(r) * 8, ey - Math.Sin(r) * 5, 1);
                ArrowAt(cv, ex, ey, Math.Atan2(Math.Sin(r) * 13, Math.Cos(r) * 20) * 180 / Math.PI, 8, sh, hd, fx[2]);
            }
            ArenKitSheets.Disc(cv, C(fx[2]), 5, 16, 2.6); ArenKitSheets.Disc(cv, C(fx[1]), 5, 16, 1.5); cv.Set(5, 16, C(fx[0]));
        }
        else if (i == 1)
        {
            // 3 Leap Back: Lyra springing back off a leaf burst, curved green motion arc, vine snare left on the ground
            var h = Hero("2");
            ArenKitSheets.Ring(cv, C(fx[3]), 21, 26, 7, 1.2, 0.4, null);
            for (int k = 0; k < 5; k++) { double a = k * 1.25; Leaf(cv, 21 + Math.Cos(a) * 6, 25 + Math.Sin(a) * 2.5, k % 4); }
            Body(cv, LyraGen.Side, h.Pal, 3, 4, 0, 21, null);
            ArenKitSheets.Crescent(cv, new[] { fx[0], fx[1], fx[2], fx[3] }, 16, 20, 9, -10, -110, 1.6);
        }
        else if (i == 2)
        {
            // 4 Arrow Rain: arrows streaking down into a marked circle, green impact sparks
            ArenKitSheets.Ring(cv, C(fx[2]), 16, 23, 11, 1.2, 0.42, (bx, by) => ((int)((Math.Atan2(by - 23, bx - 16) * 180 / Math.PI + 360) / 20) % 2) == 0);
            foreach (var p in new[] { new[] { 10, 22 }, new[] { 16, 25 }, new[] { 22, 21 }, new[] { 14, 18 } })
            {
                ArenKitSheets.Line(cv, C(fx[1]), p[0] + 6, p[1] - 15, p[0] + 3, p[1] - 8, 1);
                ArrowAt(cv, p[0], p[1], 112, 9, sh, hd, fx[2]);
                cv.Set(p[0] - 1, p[1] + 1, C(fx[0])); cv.Set(p[0] + 1, p[1] + 1, C(fx[1]));
            }
        }
        else if (i == 3)
        {
            // V Wind Step: Lyra's boot lifted on swirling light-green wind
            var pal = new Dictionary<char, string> { { 'B', "4E3020" }, { 'b', "7A4E2E" }, { 'l', "A06A40" }, { 'g', "8AB04A" }, { 'G', "5E8A34" } };
            Grid(cv, 10, 4, new[] { "gGGGGg....", "bllbbb....", "blbbbb....", "blbbbb....", "blbbbb....", "blbbbb....", "bbbbbbb...", "bbbbbbbbbb", "bbbbbbbbbb", "BBBBBBBBBB" }, pal);
            // three curling wind streaks under and behind the boot
            for (int k = 0; k < 3; k++)
            {
                int y = 18 + k * 3, x0 = 5 + k * 2, x1 = 24 - k;
                for (int x = x0; x <= x1; x++) cv.Set(x, y + (int)Math.Round(Math.Sin((x - x0) * 0.45 + k) * 0.8), C(x > x1 - 3 ? fx[0] : k == 0 ? fx[1] : fx[2]));
                cv.Set(x1 + 1, y - 1, C(fx[1])); cv.Set(x1 + 1, y - 2, C(fx[2]));
            }
            for (int k = 0; k < 2; k++) ArenKitSheets.Line(cv, C(fx[1]), 3, 8 + k * 5, 7, 8 + k * 5, 1);
            Leaf(cv, 24, 8, 0); Leaf(cv, 26, 13, 2);
        }
        else
        {
            // C Forest Pierce: a glowing arrow-beam bursting diagonally across, leaves spiralling around it
            ArenKitSheets.Line(cv, C(fx[3]), 5, 26, 26, 5, 5);
            ArenKitSheets.Line(cv, C(fx[2]), 5, 26, 26, 5, 3);
            ArenKitSheets.Line(cv, C(fx[0]), 6, 25, 25, 6, 1);
            for (int k = 0; k < 4; k++) { double s = 0.2 + k * 0.2, x = 5 + 21 * s, y = 26 - 21 * s; int side = k % 2 == 0 ? 1 : -1; Leaf(cv, x + 4 * side, y + 4 * side, k); }
            ArrowAt(cv, 27, 4, -45, 6, "F0D060", fx[0], fx[1]);
            ArenKitSheets.Star(cv, new[] { fx[0], fx[4], fx[1] }, 6, 25, 2);
        }
        return Finish(cv, t);
    }

    // ================= Selene (pink-violet / star white) =================
    public static Bitmap Selene(int i)
    {
        var cv = Canvas(); var t = Themes["Selene"]; var fx = SeleneFx; var h = Hero("3");
        if (i == 0)
        {
            // 2 Star Orb: a large violet orb with a four-point star core and sparkles
            ArenKitSheets.Disc(cv, C(fx[3]), 16, 16, 9.5);
            ArenKitSheets.Disc(cv, C(fx[2]), 16, 16, 8.2);
            ArenKitSheets.Disc(cv, C(fx[1]), 14, 14, 4.5);
            SeleneGen.StarBurst(cv, 16, 16, 6);
            cv.Set(12, 11, C(fx[0])); cv.Set(13, 11, C(fx[0]));
            ArenKitSheets.Star(cv, new[] { fx[0], fx[1], fx[2] }, 26, 6, 2); cv.Set(5, 25, C(fx[4])); cv.Set(26, 25, C(fx[1]));
        }
        else if (i == 1)
        {
            // 3 Blink: Selene's ghost fading on the left, Selene reappearing on the right, star-dust arc between
            Body(cv, h.Rows, h.Pal, 1, 10, 0, 18, fx[3]);
            Body(cv, h.Rows, h.Pal, 15, 9, 0, 19, null);
            for (int k = 0; k <= 6; k++) { double a = Math.PI + k * Math.PI / 6; double x = 15 + Math.Cos(a) * 7, y = 10 + Math.Sin(a) * 6; cv.Set(x, y, C(k % 2 == 0 ? fx[0] : fx[1])); }
            ArenKitSheets.Star(cv, new[] { fx[0], fx[1], fx[2] }, 15, 4, 2); ArenKitSheets.Dots(cv, new[] { fx[1], fx[4] }, 31, 7, 20, 4, 6, 5);
        }
        else if (i == 2)
        {
            // 4 Rune Circle: violet rune ring on the ground with Selene's four-point star turning inside
            double cx = 16, cy = 18, R = 13, ys = 0.55;
            ArenKitSheets.Ring(cv, C(fx[2]), cx, cy, R, 1.2, ys, null);
            ArenKitSheets.Ring(cv, C(fx[3]), cx, cy, R - 3.5, 1.0, ys, null);
            var pts = new List<Point>();
            for (int k = 0; k < 8; k++) { double a = -Math.PI / 2 + k * Math.PI / 4 + 0.2, rr = k % 2 == 0 ? R - 4 : (R - 4) * 0.32; pts.Add(new Point((int)Math.Round(cx + Math.Cos(a) * rr), (int)Math.Round(cy + Math.Sin(a) * rr * ys))); }
            for (int k = 0; k < 8; k++) ArenKitSheets.Line(cv, C(fx[1]), pts[k].X, pts[k].Y, pts[(k + 1) % 8].X, pts[(k + 1) % 8].Y, 1);
            for (int k = 0; k < 8; k++) { double a = k * Math.PI / 4 + 0.4; cv.Set(cx + Math.Cos(a) * (R - 1.7), cy + Math.Sin(a) * (R - 1.7) * ys, C(fx[0])); }
            ArenKitSheets.Star(cv, new[] { fx[0], fx[1], fx[2] }, 16, 6, 2); cv.Set(16, 18, C(fx[0]));
        }
        else if (i == 3)
        {
            // V Mana Shield: a shimmering bubble around Selene (hat + face)
            Body(cv, h.Rows, h.Pal, 9, 6, 0, 17, null);
            ArenKitSheets.Ring(cv, C(fx[2]), 16, 15, 12, 1.3, 1.0, null);
            ArenKitSheets.Ring(cv, C(fx[0]), 16, 15, 9.8, 1.0, 1.0, (bx, by) => { double a = Math.Atan2(by - 15, bx - 16) * 180 / Math.PI; return a > -165 && a < -110; });
            ArenKitSheets.Star(cv, new[] { fx[0], fx[1], fx[2] }, 25, 7, 2); cv.Set(7, 22, C(fx[4])); cv.Set(24, 23, C(fx[1]));
        }
        else
        {
            // C Meteor Shower: three falling stars with pink trails and a star burst where one lands
            foreach (var m in new[] { new[] { 9, 13, 0 }, new[] { 22, 9, 1 }, new[] { 17, 22, 2 } })
            {
                double hx = m[0], hy = m[1];
                ArenKitSheets.Line(cv, C(fx[3]), hx + 8, hy - 9, hx, hy, 2);
                ArenKitSheets.Line(cv, C(fx[2]), hx + 6, hy - 7, hx, hy, 1);
                ArenKitSheets.Disc(cv, C(fx[1]), hx, hy, 1.6); cv.Set(hx, hy, C(fx[0]));
            }
            SeleneGen.StarBurst(cv, 14, 25, 4);
        }
        return Finish(cv, t);
    }

    // ================= Vex (crimson / black smoke / silver) =================
    public static Bitmap Vex(int i)
    {
        var cv = Canvas(); var t = Themes["Vex"]; var fx = VexFx; var h = Hero("5");
        if (i == 0)
        {
            // 2 Shadow Step: Vex's head (silver hair, crimson blindfold) emerging from a burst of black smoke
            Smoke(cv, 8, 20, 5); Smoke(cv, 13, 24, 4.5); Smoke(cv, 6, 12, 3.5); Smoke(cv, 22, 25, 3.5);
            Body(cv, h.Rows, h.Pal, 12, 7, 0, 13, null);
            cv.Set(15, 15, C(fx[1])); cv.Set(21, 15, C(fx[1]));
            ArenKitSheets.Star(cv, new[] { fx[0], fx[1], fx[2] }, 27, 9, 1);
        }
        else if (i == 1)
        {
            // 3 Knife Throw: three silver daggers flying right in a fan, red trails
            // three daggers flying right on parallel lanes, each with its own crimson streak
            foreach (var y in new[] { 8, 16, 24 })
            {
                int tip = y == 16 ? 27 : 24;
                ArenKitSheets.Line(cv, C(fx[3]), 4, y, tip - 10, y, 1);
                ArenKitSheets.Line(cv, C(fx[2]), tip - 15, y, tip - 10, y, 1);
                cv.Set(tip - 12, y - 1, C(fx[1]));
                Spr(cv, VexDagger(0, 6), tip - 8, y);
            }
        }
        else if (i == 2)
        {
            // 4 Blade Dance: Vex's crossed daggers ringed by a flurry of crimson crescents
            for (int k = 0; k < 6; k++) { double a0 = k * 60; ArenKitSheets.Crescent(cv, new[] { fx[0], fx[1], fx[2], fx[3] }, 16, 16, 11, a0 + 40, 40, 2.2); }
            Spr(cv, VexDagger(1, 6), 12, 20); Spr(cv, VexDagger(3, 6), 20, 20);
        }
        else if (i == 3)
        {
            // V Smoke Bomb: a black bomb with a red-hot fuse bursting into a grey cloud
            // pale grey cloud (lighter than the bomb), black bomb with a silver rim light and a red-hot fuse
            foreach (var s in new[] { new[] { 18.0, 12.0, 6.5 }, new[] { 25.0, 17.0, 4.5 }, new[] { 11.0, 8.0, 4.0 }, new[] { 23.0, 7.0, 3.5 } })
            {
                ArenKitSheets.Disc(cv, C("6A6476"), s[0], s[1], s[2]);
                ArenKitSheets.Disc(cv, C("9A94A6"), s[0] - s[2] * 0.3, s[1] - s[2] * 0.3, s[2] * 0.55);
            }
            ArenKitSheets.Disc(cv, C("1C1A22"), 10, 21, 5.4);
            ArenKitSheets.Ring(cv, C("C0C4D0"), 10, 21, 4.6, 1.0, 1.0, (bx, by) => { double a = Math.Atan2(by - 21, bx - 10) * 180 / Math.PI; return a > -170 && a < -100; });
            ArenKitSheets.Disc(cv, C("46444F"), 8.5, 19.5, 1.2);
            ArenKitSheets.Line(cv, C("8A7A5A"), 13, 16, 15, 14, 1);
            ArenKitSheets.Star(cv, new[] { fx[0], fx[1], fx[2] }, 16, 13, 2);
        }
        else
        {
            // C Execute: a huge crimson X slash over a dark flare, daggers at its ends
            ArenKitSheets.Disc(cv, C("1C0A10"), 16, 16, 9);
            ArenKitSheets.Line(cv, C(fx[3]), 5, 5, 26, 26, 5); ArenKitSheets.Line(cv, C(fx[3]), 26, 5, 5, 26, 5);
            ArenKitSheets.Line(cv, C(fx[2]), 5, 5, 26, 26, 3); ArenKitSheets.Line(cv, C(fx[2]), 26, 5, 5, 26, 3);
            ArenKitSheets.Line(cv, C(fx[1]), 7, 7, 24, 24, 1); ArenKitSheets.Line(cv, C(fx[1]), 24, 7, 7, 24, 1);
            ArenKitSheets.Star(cv, new[] { "FFFFFF", fx[0], fx[1] }, 16, 16, 3);
            ArenKitSheets.Dots(cv, new[] { fx[1], fx[0], fx[4] }, 41, 16, 16, 12, 12, 8);
        }
        return Finish(cv, t);
    }

    public static readonly string[] Classes = { "Aren", "Brom", "Lyra", "Selene", "Vex" };
    public static readonly string[] Keys = { "2", "3", "4", "V", "C" };
    public static readonly Dictionary<string, string[]> SkillIds = new Dictionary<string, string[]>
    {
        { "Aren", new[] { "ShieldBash", "Charge", "WhirlSlash", "Bulwark", "Dawnbreaker" } },
        { "Brom", new[] { "GroundSlam", "ShieldCharge", "TauntRoar", "IronWall", "Earthquake" } },
        { "Lyra", new[] { "FanShot", "LeapBack", "ArrowRain", "WindStep", "ForestPierce" } },
        { "Selene", new[] { "StarOrb", "Blink", "RuneCircle", "ManaShield", "MeteorShower" } },
        { "Vex", new[] { "ShadowStep", "KnifeThrow", "BladeDance", "SmokeBomb", "Execute" } },
    };
    public static readonly Dictionary<string, string[]> SkillNamesVi = new Dictionary<string, string[]>
    {
        { "Aren", new[] { "Khien Dap", "Xung Phong", "Loc Kiem", "Thanh Luy", "Chem Binh Minh" } },
        { "Brom", new[] { "Nen Dat", "Huc Khien", "Gam Thach", "Tuong Sat", "Dong Dat" } },
        { "Lyra", new[] { "Ten Quat", "Nhay Lui", "Mua Ten", "Buoc Gio", "Ten Xuyen Rung" } },
        { "Selene", new[] { "Cau Tinh Tu", "Dich Chuyen", "Vong Phu Chu", "Khien Ma Luc", "Mua Sao Bang" } },
        { "Vex", new[] { "Buoc Bong", "Phi Dao", "Vu Dieu Luoi Dao", "Bom Khoi", "Doat Menh" } },
    };

    public static Bitmap Icon(string cls, int i)
    {
        switch (cls) { case "Aren": return Aren(i); case "Brom": return Brom(i); case "Lyra": return Lyra(i); case "Selene": return Selene(i); default: return Vex(i); }
    }
}
