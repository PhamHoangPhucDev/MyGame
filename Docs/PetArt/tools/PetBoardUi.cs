using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using System.IO;

// Pet board UI sprites in the video-reference style (Docs/UI/Reference/GAME01_UI_VIDEO_REFERENCE.md):
// flat near-black slate, 1 px light borders, 1 px cut corners, no bevel, no ornaments.
// Layout follows the approved mockup (owner, 2026-11-03). Sprites are 9-slice with a 3 px border unless noted.
// Mockups are composed from these sprites at 1x, then scaled x2 for text (VT323, 1-bit, no anti-aliasing).
public static class PetBoardUi
{
    static Color H(string s) { return P.H(s); }
    public static readonly Color Slate = H("1C1F26"), SlateRow = H("252A33"), SlateSel = H("2E3440"), Border = H("4A5260"),
        Light = H("AEB6C8"), Deep = H("14161B"), Off = H("20232B"), OffB = H("353A45"), Mute = H("8A92A3"), Bevel = H("5A5E6A"),
        Gold = H("F2D84A"), Cyan = H("8EC8F0"), Green = H("7ED67E"), GreenBg = H("22302A"), GreenHi = H("B4F0AC"),
        Red = H("E04848"), RedB = H("7A3A3A"), RedBg = H("2A1E22"), HoldRed = H("5A2630"),
        XpTrack = H("2A3A44"), Xp = H("BFE6EE"), XpD = H("86B4C2"), Card = H("1E2230"), White = H("F4F4F0");
    public static readonly string[] Rarity = { "Common", "Rare", "Epic" };
    public static readonly Color[] RarityC = { H("AEB6C8"), H("5ED0E0"), H("C99AF2") };

    /// <summary>Flat box: fill, 1 px border, 1 px cut corners. Optional 1 px inner top highlight.</summary>
    public static Bitmap Box(int w, int h, Color fill, Color border, Color? topLine = null)
    {
        var cv = new Cv(w, h);
        cv.Fill(1, 1, w - 2, h - 2, fill);
        cv.HLine(1, 0, w - 2, border); cv.HLine(1, h - 1, w - 2, border);
        cv.VLine(0, 1, h - 2, border); cv.VLine(w - 1, 1, h - 2, border);
        if (topLine.HasValue) cv.HLine(2, 1, w - 4, topLine.Value);
        return cv.B;
    }

    static Bitmap Solid(int w, int h, Color c) { var cv = new Cv(w, h); cv.Fill(0, 0, w, h, c); return cv.B; }

    static Bitmap XpFill()
    {
        var cv = new Cv(4, 4);
        cv.Fill(0, 0, 4, 3, Xp); cv.HLine(0, 3, 4, XpD);
        return cv.B;
    }

    static Bitmap Dot()
    {
        var cv = new Cv(5, 5);
        string[] g = { ".###.", "#ooo#", "#ooo#", "#ooo#", ".###." };
        for (int j = 0; j < 5; j++) for (int i = 0; i < 5; i++)
                if (g[j][i] == '#') cv.Set(i, j, H("24502A")); else if (g[j][i] == 'o') cv.Set(i, j, Green);
        cv.Set(1, 1, GreenHi);
        return cv.B;
    }

    static Bitmap Close()
    {
        var cv = new Cv(7, 7);
        for (int i = 0; i < 7; i++) { cv.Set(i, i, Light); cv.Set(6 - i, i, Light); }
        return cv.B;
    }

    /// <summary>All sprites by file name.</summary>
    public static Dictionary<string, Bitmap> Sprites()
    {
        var d = new Dictionary<string, Bitmap>();
        d["UI_PetBoard_Panel"] = Box(12, 12, Slate, Border);
        d["UI_PetBoard_Modal"] = Box(12, 12, Card, Light);
        d["UI_PetBoard_Row_Normal"] = Box(12, 12, SlateRow, SlateRow);
        d["UI_PetBoard_Row_Selected"] = Box(12, 12, SlateSel, Light);
        d["UI_PetBoard_Button_Summon_Normal"] = Box(12, 12, GreenBg, Green);
        d["UI_PetBoard_Button_Summon_Hover"] = Box(12, 12, GreenBg, GreenHi, H("2E4236"));
        d["UI_PetBoard_Button_Summon_Pressed"] = Box(12, 12, H("1A241F"), Green);
        d["UI_PetBoard_Button_Summon_Disabled"] = Box(12, 12, Off, OffB);
        d["UI_PetBoard_Button_Normal"] = Box(12, 12, SlateRow, Bevel);
        d["UI_PetBoard_Button_Hover"] = Box(12, 12, SlateRow, Light, SlateSel);
        d["UI_PetBoard_Button_Pressed"] = Box(12, 12, Slate, Bevel);
        d["UI_PetBoard_Button_Disabled"] = Box(12, 12, Off, OffB);
        d["UI_PetBoard_Button_Danger_Normal"] = Box(12, 12, RedBg, RedB);
        d["UI_PetBoard_Button_Danger_Hover"] = Box(12, 12, RedBg, Red, H("3A2228"));
        d["UI_PetBoard_Button_Danger_Pressed"] = Box(12, 12, H("201418"), RedB);
        d["UI_PetBoard_Button_Danger_Disabled"] = Box(12, 12, Off, OffB);
        d["UI_PetBoard_HoldFill"] = Solid(4, 4, HoldRed);
        d["UI_PetBoard_XP_Track"] = Solid(4, 4, XpTrack);
        d["UI_PetBoard_XP_Fill"] = XpFill();
        d["UI_PetBoard_ActiveDot"] = Dot();
        d["UI_PetBoard_Close"] = Close();
        for (int r = 0; r < 3; r++)
        {
            d["UI_PetBoard_Badge_" + Rarity[r]] = Box(8, 8, Slate, RarityC[r]);
            d["UI_PetBoard_PortraitFrame_" + Rarity[r]] = Box(12, 12, Deep, RarityC[r]);
        }
        return d;
    }

    // ---------- mockups ----------

    public const int W = 440, Ht = 300;               // board size in canvas px (landscape 640x360 reference)
    const int Head = 20, LX = 6, LW = 134, MX = 146, MW = 176, RX = 328, RW = 106, TopY = 26;

    class Doc
    {
        public Cv cv; public List<Tuple<int, int, string, Color, int>> text = new List<Tuple<int, int, string, Color, int>>();
        public void T(int x, int y, string s, Color c, int align = 0) { text.Add(Tuple.Create(x, y, s, c, align)); }
    }

    static void Nine(Cv cv, Bitmap s, int x, int y, int w, int h) { cv.Slice(s, x, y, w, h, 3, 3, 3, 3); }

    static Bitmap Portrait(string sp) { return Img.Load("Docs/PetArt/Species/Pet_" + sp + "_Portrait.png"); }

    struct PetRow { public string sp, name; public int rar, lv; public bool active; }

    static readonly PetRow[] Pets =
    {
        new PetRow { sp = "BabyDragon", name = "Baby Dragon #4", rar = 2, lv = 12, active = true },
        new PetRow { sp = "Wolf", name = "Wolf #3", rar = 1, lv = 8 },
        new PetRow { sp = "Fox", name = "Fox #2", rar = 0, lv = 5 },
        new PetRow { sp = "Rabbit", name = "Rabbit #5", rar = 0, lv = 1 },
        new PetRow { sp = "Cat", name = "Cat #1", rar = 0, lv = 3 },
    };

    static Doc Board(Dictionary<string, Bitmap> s, int sel)
    {
        var d = new Doc { cv = new Cv(W, Ht) };
        var cv = d.cv;
        Nine(cv, s["UI_PetBoard_Panel"], 0, 0, W, Ht);
        cv.HLine(1, Head, W - 2, Border);
        cv.VLine(MX - 6, Head + 1, Ht - Head - 2, Border);
        cv.VLine(RX - 6, Head + 1, Ht - Head - 2, Border);
        d.T(8, 6, "PETS", Gold);
        d.T(W / 2 + 20, 6, "Pick a pet on the left, then Summon", Cyan, 1);
        cv.Blit(s["UI_PetBoard_Close"], W - 14, 7);

        // left: collection
        d.T(LX + 2, TopY, "COLLECTION 5", Gold);
        for (int i = 0; i < Pets.Length; i++)
        {
            var p = Pets[i];
            int y = TopY + 12 + i * 30;
            Nine(cv, s[i == sel ? "UI_PetBoard_Row_Selected" : "UI_PetBoard_Row_Normal"], LX, y, LW, 28);
            cv.Blit(Portrait(p.sp), LX + 2, y + 2);
            d.T(LX + 30, y + 3, p.name, White);
            d.T(LX + 30, y + 15, Rarity[p.rar], RarityC[p.rar]);
            if (p.active) { cv.Blit(s["UI_PetBoard_ActiveDot"], LX + 66, y + 17); d.T(LX + 73, y + 15, "ACTIVE", Green); }
            d.T(LX + LW - 4, y + 9, "Lv." + p.lv, White, 2);
        }
        // shard area
        int sy = Ht - 54;
        cv.HLine(LX, sy - 4, LW, Border);
        d.T(LX + 2, sy, "EGG SHARDS", Gold);
        var shard = Img.Load("Docs/PetArt/Shard/UI_Item_PetEggShard.png");
        Nine(cv, s["UI_PetBoard_Row_Normal"], LX, sy + 12, 22, 22);
        cv.Blit(shard, LX + 3, sy + 15);
        d.T(LX + 28, sy + 13, "7 / 10", White);
        Nine(cv, s["UI_PetBoard_XP_Track"], LX + 28, sy + 27, 50, 4);
        cv.Slice(s["UI_PetBoard_XP_Fill"], LX + 28, sy + 27, 35, 4, 0, 0, 0, 1);
        Nine(cv, s["UI_PetBoard_Button_Disabled"], LX + 84, sy + 14, 48, 18);
        d.T(LX + 108, sy + 18, "COMBINE", OffB, 1);

        // middle: selected pet
        var q = Pets[sel];
        int fx = MX + (MW - 88) / 2, fy = TopY + 4;
        Nine(cv, s["UI_PetBoard_PortraitFrame_" + Rarity[q.rar]], fx, fy, 88, 88);
        cv.Blit(Portrait(q.sp), fx + 8, fy + 8, 3);
        int bw = Rarity[q.rar].Length * 6 + 10;
        Nine(cv, s["UI_PetBoard_Badge_" + Rarity[q.rar]], fx + 44 - bw / 2, fy - 5, bw, 11);
        d.T(fx + 44, fy - 4, Rarity[q.rar].ToUpper(), RarityC[q.rar], 1);
        d.T(MX + MW / 2, fy + 94, q.name, Gold, 1);
        d.T(MX + MW / 2, fy + 108, q.active ? "ACTIVE - FOLLOW" : "INACTIVE", q.active ? Green : Mute, 1);
        int xy = fy + 124, xw = 130, xx = MX + (MW - xw) / 2;
        int need = 20 + 10 * q.lv, have = q.active ? 340 : 0;
        if (q.active) need = 560;
        d.T(xx, xy, "Lv." + q.lv, White);
        d.T(xx + xw, xy, have + " / " + need + " XP", Cyan, 2);
        cv.Fill(xx, xy + 12, xw, 5, XpTrack);
        cv.Slice(s["UI_PetBoard_XP_Fill"], xx, xy + 12, Math.Max(2, xw * have / need), 5, 0, 0, 0, 1);
        string[] k = { "ATK", "SPEED", "STYLE" };
        string[] v = q.sp == "BabyDragon" ? new[] { "14", "1.5s / hit", "Fire breath, ranged" } : new[] { "2", "0.7s / hit", "Hop stomp, melee" };
        for (int i = 0; i < 3; i++) { d.T(xx, xy + 24 + i * 12, k[i], Gold); d.T(xx + 40, xy + 24 + i * 12, v[i], i == 2 ? Cyan : White); }

        // right: commands
        d.T(RX + 2, TopY, "COMMAND", Gold);
        bool act = q.active;
        Nine(cv, s[act ? "UI_PetBoard_Button_Summon_Disabled" : "UI_PetBoard_Button_Summon_Normal"], RX, TopY + 12, RW, 28);
        d.T(RX + RW / 2, TopY + 21, "SUMMON", act ? OffB : Green, 1);
        d.T(RX + RW / 2, TopY + 43, act ? "With you now" : "Replaces Baby Dragon", Mute, 1);
        string[] cmd = { "RECALL", "FOLLOW", "STAY", "CALL" };
        bool[] on = act ? new[] { true, false, true, false } : new[] { false, false, false, false };
        for (int i = 0; i < 4; i++)
        {
            int bx = RX + (i % 2) * (RW / 2 + 1), by = TopY + 58 + (i / 2) * 22;
            Nine(cv, s[on[i] ? "UI_PetBoard_Button_Normal" : "UI_PetBoard_Button_Disabled"], bx, by, RW / 2 - 1, 19);
            d.T(bx + RW / 4, by + 5, cmd[i], on[i] ? White : OffB, 1);
        }
        int dy = Ht - 40;
        cv.HLine(RX, dy - 6, RW, Border);
        bool canSac = !act && q.rar == 0;
        Nine(cv, s[canSac ? "UI_PetBoard_Button_Danger_Normal" : "UI_PetBoard_Button_Danger_Disabled"], RX + 8, dy, RW - 16, 15);
        d.T(RX + RW / 2, dy + 3, canSac ? "Sacrifice +1" : "Sacrifice", canSac ? Red : OffB, 1);
        if (!canSac) d.T(RX + RW / 2, dy + 19, "Recall first", Mute, 1);
        return d;
    }

    static Doc Confirm(Dictionary<string, Bitmap> s, int step)
    {
        var d = Board(s, 3);
        var cv = d.cv;
        // dim the board: mix every pixel 65 % toward the deep slate (stays opaque)
        for (int j = 0; j < Ht; j++)
            for (int i = 0; i < W; i++)
            {
                Color c = cv.B.GetPixel(i, j);
                if (c.A > 0) cv.Set(i, j, P.Mix(c, Deep, 0.65));
            }
        d.text.Clear();
        int mw = 200, mh = 118, mx = (W - mw) / 2, my = (Ht - mh) / 2;
        Nine(cv, s["UI_PetBoard_Modal"], mx, my, mw, mh);
        cv.HLine(mx + 6, my + 18, mw - 12, Border);
        d.T(mx + 8, my + 6, step == 1 ? "SACRIFICE PET?" : "CONFIRM", Red);
        d.T(mx + mw - 8, my + 6, step + " / 2", Mute, 2);
        if (step == 1)
        {
            Nine(cv, s["UI_PetBoard_PortraitFrame_Common"], mx + 22, my + 26, 36, 36);
            cv.Blit(Portrait("Rabbit"), mx + 28, my + 32);
            d.T(mx + 40, my + 64, "Rabbit #5", White, 1);
            d.T(mx + 40, my + 74, "Common Lv.1", RarityC[0], 1);
            d.T(mx + 100, my + 38, "->", Gold, 1);
            Nine(cv, s["UI_PetBoard_PortraitFrame_Common"], mx + 142, my + 26, 36, 36);
            cv.Blit(Img.Load("Docs/PetArt/Shard/UI_PetEggShard_32.png"), mx + 144, my + 28);
            d.T(mx + 160, my + 64, "+1 Egg Shard", Gold, 1);
            d.T(mx + 160, my + 74, "7 -> 8 / 10", Mute, 1);
            Nine(cv, s["UI_PetBoard_Button_Normal"], mx + 8, my + mh - 24, 88, 17);
            d.T(mx + 52, my + mh - 20, "CANCEL", White, 1);
            Nine(cv, s["UI_PetBoard_Button_Danger_Normal"], mx + mw - 96, my + mh - 24, 88, 17);
            d.T(mx + mw - 52, my + mh - 20, "NEXT", Red, 1);
        }
        else
        {
            d.T(mx + mw / 2, my + 34, "Hold to sacrifice Rabbit #5", White, 1);
            d.T(mx + mw / 2, my + 48, "Let go to cancel", Mute, 1);
            Nine(cv, s["UI_PetBoard_Button_Normal"], mx + 8, my + mh - 24, 60, 17);
            d.T(mx + 38, my + mh - 20, "BACK", White, 1);
            int hx = mx + 74, hw = mw - 82;
            Nine(cv, s["UI_PetBoard_Button_Danger_Hover"], hx, my + mh - 24, hw, 17);
            cv.Fill(hx + 1, my + mh - 23, hw * 62 / 100, 15, HoldRed);      // hold progress 62 %
            d.T(hx + hw / 2, my + mh - 20, "HOLD TO SACRIFICE", Red, 1);
        }
        return d;
    }

    /// <summary>x2 render with 1-bit VT323 text (no anti-aliasing).</summary>
    static void Render(Doc d, string fontPath, string path)
    {
        var big = d.cv.Scaled(2).B;
        var pfc = new PrivateFontCollection();
        pfc.AddFontFile(fontPath);
        using (var g = Graphics.FromImage(big))
        using (var f = new Font(pfc.Families[0], 18, FontStyle.Regular, GraphicsUnit.Pixel))
        {
            g.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
            g.SmoothingMode = SmoothingMode.None;
            foreach (var t in d.text)
            {
                var size = g.MeasureString(t.Item3, f, 2000, StringFormat.GenericTypographic);
                float x = t.Item1 * 2 - (t.Item5 == 1 ? size.Width / 2 : t.Item5 == 2 ? size.Width : 0);
                float y = t.Item2 * 2 - 3;
                using (var sh = new SolidBrush(H("0E0F12"))) g.DrawString(t.Item3, f, sh, (float)Math.Round(x) + 2, y + 2, StringFormat.GenericTypographic);
                using (var b = new SolidBrush(t.Item4)) g.DrawString(t.Item3, f, b, (float)Math.Round(x), y, StringFormat.GenericTypographic);
            }
        }
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        big.Save(path, System.Drawing.Imaging.ImageFormat.Png);
    }

    public static void Run(string outDir, string fontPath)
    {
        var s = Sprites();
        foreach (var kv in s) new Cv(kv.Value).Save(Path.Combine(outDir, "export", kv.Key + ".png"));
        Render(Board(s, 0), fontPath, Path.Combine(outDir, "mockups", "PetBoard_1_Active_x2.png"));
        Render(Board(s, 3), fontPath, Path.Combine(outDir, "mockups", "PetBoard_2_Switch_x2.png"));
        Render(Confirm(s, 1), fontPath, Path.Combine(outDir, "mockups", "PetBoard_3_Sacrifice_Step1_x2.png"));
        Render(Confirm(s, 2), fontPath, Path.Combine(outDir, "mockups", "PetBoard_4_Sacrifice_Step2_x2.png"));

        // sprite sheet board x4 (review only)
        int k = 4, x = 16, y = 16, rowH = 0;
        var board = new Cv(900, 360);
        board.Clear(H("101216"));
        foreach (var kv in s)
        {
            var b = kv.Value;
            int dw = Math.Max(b.Width, 12) * k;
            if (x + dw > 884) { x = 16; y += rowH + 12; rowH = 0; }
            board.Blit(b, x, y, k);
            x += dw + 12; rowH = Math.Max(rowH, b.Height * k);
        }
        board.Save(Path.Combine(outDir, "PetBoard_Sprites_Board.png"));
    }
}
