using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;

// R2C: three ORIGINAL hero concepts (Idle Down, frame 0 only) to replace the reference-derived white-hair hero.
// Same technical frame as before: 32x32 cell, sole on row 29, pivot (0.5, 0.0625), chibi ~21 px tall.
// Identity (hair, outfit, palette, silhouette) is new; the sword stays a separate sprite (not drawn).
public static class HeroConceptGen
{
    public class Concept
    {
        public string Id, Name, Pitch;
        public Dictionary<char, string> Pal;
        public string[] Rows;
    }

    public static readonly Concept[] All =
    {
        new Concept
        {
            Id = "A", Name = "Kael", Pitch = "lang khach: toc nau roi, khan quang do, ao giap da",
            Pal = new Dictionary<char, string>
            {
                { 'k', "241A1E" }, { 'H', "5A3A2A" }, { 'h', "7A5038" }, { 's', "F4D2B0" }, { 'S', "D8A888" },
                { 'e', "2A2A3A" }, { 'r', "C8423A" }, { 'R', "8E2A2E" }, { 'j', "7A5A3C" }, { 'J', "523C2C" },
                { 'c', "E8DCC0" }, { 'b', "3A2E2A" }, { 'm', "D8B060" },
            },
            Rows = new[]
            {
                "....kkkkkk....",
                "...kHHHHHHk...",
                "..kHHhhHHHHk..",
                ".kHHhHHHHhHHk.",
                ".kHHHHHHHHHHk.",
                ".kHHHhHHhHHHk.",
                ".kHsHHsHHsHHk.",
                ".kHssssssssHk.",
                ".kHsessssesHk.",
                ".kHsessssesHk.",
                "..kSssssssSk..",
                "..krrrrrrrrk..",
                ".kjRRrrrrRRjk.",
                ".kjjjccccjjrk.",
                ".kjjJccccJjrk.",
                ".kbbbbmmbbbbk.",
                ".kjjJjjjjJjjk.",
                ".kJjjjkkjjjJk.",
                "..kJJk..kJJk..",
                "...kbk..kbk...",
                "..kbbk..kbbk..",
            },
        },
        new Concept
        {
            Id = "B", Name = "Mira", Pitch = "tho san: toc dong bim vat vai, kinh bao ho tren tran, ao choang mau man",
            Pal = new Dictionary<char, string>
            {
                { 'k', "2A1C24" }, { 'H', "C8682E" }, { 'h', "E89A4A" }, { 'D', "8A3E22" }, { 's', "F6D8BC" },
                { 'S', "DDB094" }, { 'e', "2E5A48" }, { 'g', "4A3A36" }, { 'G', "7CC8D8" }, { 'p', "6A3A6A" },
                { 'P', "46264A" }, { 'q', "8E5A8A" }, { 'c', "E2D6B8" }, { 'b', "3E3030" }, { 'l', "4A4258" },
                { 'm', "D8B060" },
            },
            Rows = new[]
            {
                "....kkkkkk....",
                "...kHHhhHHk...",
                ".kHHhHHHHhHHk.",
                ".kgGGggggGGgk.",
                ".kHHHHHHHHHHk.",
                ".kHHDHHHHDHHk.",
                ".kHssHssHssHk.",
                ".kHssssssssHk.",
                ".kHsessssesHk.",
                ".kHsessssesHk.",
                "..kSssssssSk..",
                "..kqqccccHqk..",
                ".kppqccccHDpk.",
                ".kpppccccDHpk.",
                ".kPppcbbcHDpk.",
                ".kPppccccmHPk.",
                ".kPpppccppppk.",
                ".kPPppllppPPk.",
                "..kkkllllkkk..",
                "....kbkkbk....",
                "...kbbkkbbk...",
            },
        },
        new Concept
        {
            Id = "C", Name = "Rook", Pitch = "hiep si tap su: toc den co loc trang, giap vai thep, ao choang do tham",
            Pal = new Dictionary<char, string>
            {
                { 'k', "16141C" }, { 'H', "2A2A36" }, { 'h', "4A4A5E" }, { 'w', "E8E8F0" }, { 's', "EBC8A8" },
                { 'S', "C9A088" }, { 'e', "D89A30" }, { 't', "A8323C" }, { 'T', "6E2030" }, { 'a', "A8B0BC" },
                { 'A', "6A7280" }, { 'c', "3A3A48" }, { 'b', "2A2430" }, { 'm', "D8B048" },
            },
            Rows = new[]
            {
                "....kkkkkk....",
                "...kHHwHHHk...",
                "..kHHHwwHHHk..",
                ".kHHhHHwHHhHk.",
                ".kHHHHHwHHHHk.",
                ".kHHHHHHwHHHk.",
                ".kHsHsssHwsHk.",
                ".kHssssssssHk.",
                ".kHsessssesHk.",
                ".kHsessssesHk.",
                "..kSssssssSk..",
                ".kAakttttkaAk.",
                "kAaAktmmtkAaAk",
                ".kAkcttttckAk.",
                "..kcttmmttck..",
                "..kbbbmmbbbk..",
                "..kcttttttck..",
                "..kcTttttTck..",
                "..kkTTkkTTkk..",
                "....kbkkbk....",
                "...kbbkkbbk...",
            },
        },
    };

    // v2 craft pass: chibi proportions (head 14 wide, body 10), 3-5 tones per material lit from top-left,
    // coloured outlines (dark of the local hue, not black), bigger eyes (2x2 with a highlight), blush.
    static readonly Dictionary<char, string> Skin = new Dictionary<char, string>
    {
        { 's', "FCDCC0" }, { 'S', "E8B494" }, { 'p', "F2A49A" }, { 'e', "2A2030" }, { 'w', "FFFFFF" },
    };

    static Dictionary<char, string> With(Dictionary<char, string> own)
    {
        var d = new Dictionary<char, string>(Skin);
        foreach (var kv in own) d[kv.Key] = kv.Value;
        return d;
    }

    public static readonly Concept[] AllV2 =
    {
        new Concept
        {
            Id = "A", Name = "Kael", Pitch = "lang khach: toc nau, khan quang do, ao da",
            Pal = With(new Dictionary<char, string>
            {
                { 'K', "3A2420" }, { 'D', "6A4030" }, { 'H', "8E5A3C" }, { 'L', "B87C50" }, { 'X', "D8A070" },
                { 'E', "4A5A8A" }, { 'r', "D84A3E" }, { 'R', "A0302E" }, { 'q', "F07A5A" },
                { 'j', "8A6A48" }, { 'J', "5E4634" }, { 'i', "AA8A60" }, { 'c', "EEE2C8" },
                { 'b', "4A3830" }, { 'm', "E8C060" }, { 'n', "4A4A5A" }, { 'B', "3A2A26" }, { 'k', "2E2228" },
            }),
            Rows = new[]
            {
                ".....KKKK.....",
                "...KKHHLLKK...",
                "..KHHLXXLHHK..",
                ".KHHLLXLLHHDK.",
                ".KHHHLLLHHHDK.",
                "KDHHHHHHHHHDDK",
                "KDHsHHsHHHsHDK",
                "KDHssssssssHDK",
                "KDHseesseesHDK",
                "KDHsEwssEwsHDK",
                ".KDpsssssspDK.",
                "..KDSssssSDK..",
                "..krqqrrrrRk..",
                "..kRrrrrrRRk..",
                "..kiJccccJrk..",
                "..kijccccjRk..",
                "..kbbbmbbbbk..",
                "..kijjjjjJJk..",
                "...kjjkkjJk...",
                "...knnkknnk...",
                "..kBBBkkBBBk..",
            },
        },
        new Concept
        {
            Id = "B", Name = "Mira", Pitch = "tho san: toc dong bim vat vai, kinh bao ho, ao choang man",
            Pal = With(new Dictionary<char, string>
            {
                { 'K', "6A2A1A" }, { 'D', "A8481E" }, { 'H', "D86A2A" }, { 'L', "F0944A" }, { 'X', "FFC07A" },
                { 'E', "3A8A68" }, { 'g', "5A4038" }, { 'G', "7CD8E8" },
                { 'P', "4A2A50" }, { 'p', "6E4078" }, { 'q', "9A68A0" }, { 'c', "EDE2C8" }, { 'C', "C8B898" },
                { 'b', "5A4038" }, { 'm', "E8C060" }, { 'n', "4A4460" }, { 'B', "4A3430" }, { 'k', "2A1A2E" },
            }),
            Rows = new[]
            {
                ".....KKKK.....",
                "...KKHHLLKK...",
                "..KHHLXXLHHK..",
                ".KgGwgLLgGwgK.",
                ".KHHHLLLHHHDK.",
                "KDHHHHHHHHHDDK",
                "KDHHHssssHHHDK",
                "KDHssssssssHDK",
                "KDHseesseesHDK",
                "KDHsEwssEwsHDK",
                ".KDpsssssspDK.",
                "..KDSssssSDHK.",
                "..kqqccccqHLk.",
                "..kpqcCCcpDHk.",
                "..kppcbbcpHLk.",
                "..kPpccccpDHk.",
                "..kPppccppmHk.",
                "..kPPpnnppPk..",
                "...kPPnnPPk...",
                "...knnkknnk...",
                "..kBBBkkBBBk..",
            },
        },
        new Concept
        {
            Id = "C", Name = "Rook", Pitch = "hiep si tap su: toc den loc trang, giap vai, ao choang do",
            Pal = With(new Dictionary<char, string>
            {
                { 'K', "14121A" }, { 'D', "24222E" }, { 'H', "363448" }, { 'L', "4E4C66" }, { 'X', "6E6C8A" },
                { 'W', "F0F0F8" }, { 'V', "B8B8C8" }, { 'E', "E0A030" },
                { 'A', "6A7484" }, { 'a', "9AA4B4" }, { 'Z', "D0D8E4" },
                { 't', "B8323C" }, { 'T', "7A2030" }, { 'u', "E05A5A" }, { 'm', "E8C048" },
                { 'c', "3A3A4A" }, { 'b', "2A2430" }, { 'n', "3A3646" }, { 'B', "2A2430" }, { 'k', "1A141C" },
            }),
            Rows = new[]
            {
                ".....KKKK.....",
                "...KKHWVHKK...",
                "..KHLXWVLHHK..",
                ".KHHLLWVLHHDK.",
                ".KHHHLLWVHHDK.",
                "KDHHHHHHWVHDDK",
                "KDHsHHsHHWsHDK",
                "KDHssssssssHDK",
                "KDHseesseesHDK",
                "KDHsEwssEwsHDK",
                ".KDSssssssSDK.",
                "..KDSssssSDK..",
                "kaZAktmmtkaZAk",
                "kAaAkuttTkAaAk",
                "..kcutmmtTck..",
                "..kbbbmmbbbk..",
                "..kcuttttTck..",
                "..kcutttTTck..",
                "...kuTkkuTk...",
                "...knnkknnk...",
                "..kBBBkkBBBk..",
            },
        },
    };

    // v3 roster: 5 playable race/class heroes, same template and craft rules as v2.
    public static readonly Concept[] Roster =
    {
        new Concept
        {
            Id = "1", Name = "Brom", Pitch = "Nguoi lun - Tank: mu sat sung, rau do, giap vai day",
            Pal = With(new Dictionary<char, string>
            {
                { 'k', "1C1A22" }, { 'A', "5A6070" }, { 'a', "8A94A4" }, { 'Z', "C8D0DC" }, { 'm', "E8C048" },
                { 'h', "E8DCC0" }, { 'y', "B8A888" },
                { 'K', "6A2A14" }, { 'D', "A84A1E" }, { 'H', "D8722E" }, { 'L', "F0A050" },
                { 't', "3A5A8A" }, { 'T', "24385A" }, { 'b', "4A3428" }, { 'B', "3A2A26" },
            }),
            Rows = new[]
            {
                "h............h",
                "hy..kkkkkk..yh",
                ".hkaZZaaaaAkh.",
                ".kaZaaaaaaaAk.",
                ".kAAAAmmAAAAk.",
                ".kSDDssssDDSk.",
                ".ksseesseessk.",
                ".kHsssSSsssHk.",
                "KDHLLLHHLLLHDK",
                "KDHHLLHHLLHHDK",
                "kZaKDHLLHDKaAk",
                "kaAKDHLLHDKAak",
                ".kkaKDHHDKakk.",
                "..kaaKDDKAak..",
                "..kZaaKKaAAk..",
                "..kbbbmmbbbk..",
                "..kttttttTTk..",
                "..kAaaTTaaAk..",
                "..kAAk..kAAk..",
                "..kBBk..kBBk..",
                ".kBBBBkkBBBBk.",
            },
        },
        new Concept
        {
            // v3: forest ranger elf (user mood reference). Generic traits only: golden hair with a side braid over
            // the right shoulder and a leaf ornament, pointed ears, cream sleeveless top with green capelet sleeves,
            // brown leather corset belt, jagged green skirt over a cream underskirt, green quiver with white
            // fletching behind the left shoulder, brown boots. Bow stays a separate sprite.
            Id = "2", Name = "Lyra", Pitch = "Elf - Cung thu: toc vang tet ben vai, kep la, tai nhon, ao kem, ao nit da nau, vay xanh la xe nhon, ong ten sau vai, ung nau",
            Pal = With(new Dictionary<char, string>
            {
                { 'K', "8A6A2A" }, { 'D', "C8A048" }, { 'H', "E8CC70" }, { 'L', "F8E6A0" }, { 'X', "FFF8D8" },
                { 'f', "6AA040" }, { 'F', "3E7028" }, { 'E', "5A9A30" },
                { 'o', "F2ECD4" }, { 'O', "CFC6A8" },
                { 'g', "8AB04A" }, { 'G', "5E8A34" }, { 'V', "3E6224" },
                { 'b', "7A4E2E" }, { 'B', "4E3020" }, { 'l', "A06A40" },
                { 'a', "EEE8DC" }, { 'q', "7A6A50" }, { 'k', "2A2218" },
            }),
            Rows = new[]
            {
                "....KLXXLKfF..",
                "...KHHLLHKfF..",
                "..KHHLXXLHHK..",
                ".KHHLLXLLHHHK.",
                ".KHHHLLLLHHHK.",
                "KDHHHHHHHHHHDK",
                "KDHsHHsHHHsHDK",
                "sKHssssssssHKs",
                "SKHseesseesHKS",
                "aKHsEwssEwsHK.",
                "aKDpsssssspDK.",
                "qGkHSssssSHLk.",
                "GVkgOooooOgHLk",
                "GVkgbBBbbBgDHk",
                "GVkgblbbblgHLk",
                "GVkGgoOOogGkDk",
                "BVkGgoooogGk..",
                ".kVGgOooOgGVk.",
                "..V.kssksskV..",
                "....kBbkBbk...",
                "...kBBbkBBbk..",
            },
        },
        new Concept
        {
            Id = "3", Name = "Selene", Pitch = "Phu thuy - Phap su: mu nhon tim, toc bac tim, ao choang xanh dem vien vang",
            Pal = With(new Dictionary<char, string>
            {
                { 'k', "1A1426" }, { 'P', "3A2A5A" }, { 'p', "5A4288" }, { 'q', "7E66B0" }, { 'm', "E8C048" }, { 'y', "FFF0A0" },
                { 'K', "4A3A6A" }, { 'D', "7A6AA8" }, { 'H', "A898D0" }, { 'L', "D0C4EE" },
                { 'E', "C04A8A" }, { 'r', "24305A" }, { 'R', "161E3C" }, { 'u', "3A4C88" }, { 'c', "E8E0F0" },
                { 'B', "2A2030" },
            }),
            Rows = new[]
            {
                "........kk....",
                ".......kpqk...",
                "......kpqPk...",
                ".....kppqPk...",
                "....kpyppPPk..",
                "...kPmmmmPPk..",
                "kpqqpppppppPPk",
                "KDHHsHHHsHHHDK",
                "KDHssssssssHDK",
                "KDHseesseesHDK",
                "KDHsEwssEwsHDK",
                ".KDpsssssspDK.",
                ".KDkuccccrkDK.",
                ".KDkurmmrrkDK.",
                "..kuurrmrrRk..",
                "..kuurrmrrRk..",
                ".kuurrrmrrRRk.",
                ".kuurrrmrrRRk.",
                ".kmmmmmmmmmmk.",
                "....kBkkBk....",
                "...kBBkkBBk...",
            },
        },
        new Concept
        {
            // v3: full-plate knight (user mood reference). Generic traits only: closed great helm with a centre
            // ridge and vertical visor slits, navy plume, navy cloak over the left shoulder, torn white cloth over
            // the right pauldron, silver plate, brown belt with buckle and pouch. Sword stays a separate sprite.
            Id = "4", Name = "Aren", Pitch = "Nhan toc - Kiem si: hiep si giap toan than, mu kin co khe, chom long xanh navy, ao choang navy vat vai trai, vai phai giap bac phu vai trang rach, that lung da",
            Pal = With(new Dictionary<char, string>
            {
                { 'k', "1A1C24" }, { 'A', "5A6272" }, { 'a', "8E98A8" }, { 'Z', "C8D0DC" }, { 'W', "F0F4F8" },
                { 'v', "12141A" },
                { 'P', "2A3470" }, { 'p', "4050A0" }, { 'q', "6070C0" },
                { 'C', "1E2450" }, { 'c', "2E3A78" }, { 'u', "4A58A0" },
                { 'o', "E8E2D0" }, { 'O', "BEB6A2" },
                { 'b', "6A4428" }, { 'g', "C8CCD4" }, { 'd', "8A5A34" },
            }),
            Rows = new[]
            {
                ".......Ppq....",
                "....kkkPpqp...",
                "...kZWaPpAkpq.",
                "..kZWaaZaAAkpP",
                ".kZWaaaZaaAAkP",
                ".kZaaaaZaaaAkP",
                ".kAAAAAZAAAAk.",
                ".kvavavZavavk.",
                ".kvavavZavavk.",
                ".kZaaaaZaaAAk.",
                "kuckAaZaaAAk..",
                "kucCkAZaAkooOk",
                "kuccCkWaaAkoOk",
                "kuccCkZaaAkOAk",
                "kuccCkZaaAkAAk",
                "kucCCbbgbbdbkk",
                "kucCCkaZaAkdk.",
                "kuCkkaZaAAk...",
                "..kCkAAkkAAk..",
                "...kAAkkAAk...",
                "..kAAAkkAAAk..",
            },
        },
        new Concept
        {
            // v5: user mood reference = blindfolded swordsman with messy pale hair. Kept generic traits only and
            // pushed away from a well-known anime look (white spiky hair + black blindfold): ash-silver hair with a
            // lavender shade, two small demon horns, DARK CRIMSON blindfold with a tail streaming left, black high
            // collar, light-grey open cloak over a dark outfit, silver buckle.
            Id = "5", Name = "Vex", Pitch = "Ma toc - Sat thu: toc bac tro roi, 2 sung nho, dai bit mat do tham bay duoi, co cao den, ao choang xam nhat mo truoc",
            Pal = With(new Dictionary<char, string>
            {
                { 'K', "3E3C54" }, { 'D', "8A88A0" }, { 'H', "B8B6CC" }, { 'L', "DCDAE8" }, { 'X', "FFFFFF" },
                { 'h', "2A2230" }, { 'y', "6A5A72" },
                { 's', "F4E4DA" }, { 'S', "D8C0B4" }, { 'p', "E09A9A" },
                { 'r', "9A2232" }, { 'R', "5E1420" }, { 'q', "C8404E" },
                { 'M', "1E1C24" }, { 'N', "3A3844" },
                { 'o', "C8C8D4" }, { 'c', "A8A8B8" }, { 'C', "84849A" },
                { 'j', "2E2C36" }, { 'J', "1C1A22" }, { 'i', "46444F" },
                { 'b', "24222A" }, { 'g', "B8BCC8" }, { 'n', "2A2830" }, { 'B', "18161C" }, { 'k', "0E0C12" },
            }),
            Rows = new[]
            {
                "..y.K.KK.K.y..",
                "..hKHKHLKHKh..",
                ".KHHLHLXLLHHK.",
                "KHHLLXXLLLHHDK",
                "KHHHLLLLLHHHDK",
                "KDHHLHHHHLHHDK",
                "KDHHsHHHsHHHDK",
                "KrqqrrrHrrrRDK",
                "KRrrrrHrrrrRRR",
                "KDHsssssssSHDR",
                "KDHHssppssSHDR",
                ".KDMNMMMMMMDK.",
                "ocMMMMMMMMMMco",
                "occkMNMMMMkCCo",
                "occkjjMMjjkCCo",
                "occkbbggbbkCCo",
                "occkjjjjjJkCCo",
                ".oCkijjjjJkCC.",
                "..CkijkkjJkC..",
                "...knnkknnk...",
                "..kBBBkkBBBk..",
            },
        },
    };

    const int Cell = 32, X0 = 9, Y0 = 9;

    public static string Run(string outDir) { return Run(outDir, 1); }

    public static string Run(string outDir, int version)
    {
        var log = new StringBuilder();
        foreach (var c in version == 3 ? Roster : version == 2 ? AllV2 : All)
        {
            if (c.Rows.Length != 21) throw new Exception(c.Id + ": rows " + c.Rows.Length);
            for (int y = 0; y < 21; y++)
            {
                if (c.Rows[y].Length != 14) throw new Exception(c.Id + " row " + y + " len " + c.Rows[y].Length);
                foreach (char ch in c.Rows[y]) if (ch != '.' && !c.Pal.ContainsKey(ch)) throw new Exception(c.Id + " row " + y + " unknown '" + ch + "'");
            }
            var bmp = new Bitmap(Cell, Cell, PixelFormat.Format32bppArgb);
            int minX = 99, maxX = -1, minY = 99, maxY = -1;
            for (int y = 0; y < 21; y++) for (int x = 0; x < 14; x++)
                {
                    char ch = c.Rows[y][x];
                    if (ch == '.') continue;
                    bmp.SetPixel(X0 + x, Y0 + y, Hex(c.Pal[ch]));
                    minX = Math.Min(minX, X0 + x); maxX = Math.Max(maxX, X0 + x); minY = Math.Min(minY, Y0 + y); maxY = Math.Max(maxY, Y0 + y);
                }
            bmp.Save(System.IO.Path.Combine(outDir, "HeroConcept_" + c.Id + "_" + c.Name + "_IdleDown_F0" + (version == 3 ? "_roster" : version == 2 ? "_v2" : "") + ".png"), ImageFormat.Png);
            bmp.Dispose();
            log.AppendFormat("{0} {1}: bbox x {2}..{3} y {4}..{5} -> {6}x{7}, colours {8}\n", c.Id, c.Name, minX, maxX, minY, maxY, maxX - minX + 1, maxY - minY + 1, c.Pal.Count);
        }
        return log.ToString();
    }

    public static Color Hex(string h) { return Color.FromArgb(255, Convert.ToInt32(h.Substring(0, 2), 16), Convert.ToInt32(h.Substring(2, 2), 16), Convert.ToInt32(h.Substring(4, 2), 16)); }
}
