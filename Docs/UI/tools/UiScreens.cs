using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

/// <summary>GAME01 screen mockups. Portrait UI canvas 288x512 (x2), landscape 640x360 (x3). Values shown are sample data.</summary>
public static partial class Screens
{
    // ------------------------------------------------------------- game data (from the project)
    public static readonly string[] Classes = { "Aren", "Brom", "Lyra", "Selene", "Vex" };
    public static readonly string[] Roles = { "KNIGHT", "DWARF TANK", "ELF ARCHER", "WITCH", "ASSASSIN" };
    public static readonly string[] Blurb = {
        "Balanced fighter with a shield.",
        "Slow tank. Stuns and taunts.",
        "Fast ranged kiting hunter.",
        "Area magic and a mana shield.",
        "Burst, bleed and execute." };
    // HP mult, speed mult, damage reduction (CLASS_KIT_DESIGN)
    public static readonly double[,] Stats = { { 1.0, 1.0, 0 }, { 1.5, 0.85, 0.15 }, { 0.85, 1.1, 0 }, { 0.8, 1.0, 0 }, { 0.8, 1.15, 0 } };
    public static readonly string[][] SkillFiles = {
        new[] { "R_Combo", "2_ShieldBash", "3_Charge", "4_WhirlSlash", "V_Bulwark", "C_Dawnbreaker" },
        new[] { "R_Combo", "2_GroundSlam", "3_ShieldCharge", "4_TauntRoar", "V_IronWall", "C_Earthquake" },
        new[] { "R_Combo", "2_FanShot", "3_LeapBack", "4_ArrowRain", "V_WindStep", "C_ForestPierce" },
        new[] { "R_Combo", "2_StarOrb", "3_Blink", "4_RuneCircle", "V_ManaShield", "C_MeteorShower" },
        new[] { "R_Combo", "2_ShadowStep", "3_KnifeThrow", "4_BladeDance", "V_SmokeBomb", "C_Execute" } };
    public static readonly string[][] SkillNames = {
        new[] { "KNIGHT SWORD", "SHIELD BASH", "CHARGE", "WHIRL SLASH", "BULWARK", "DAWNBREAKER" },
        new[] { "FORGE HAMMER", "GROUND SLAM", "SHIELD CHARGE", "TAUNT ROAR", "IRON WALL", "EARTHQUAKE" },
        new[] { "FOREST ARROW", "FAN SHOT", "LEAP BACK", "ARROW RAIN", "WIND STEP", "FOREST PIERCE" },
        new[] { "SPELL BOLT", "STAR ORB", "BLINK", "RUNE CIRCLE", "MANA SHIELD", "METEOR SHOWER" },
        new[] { "TWIN BLADES", "SHADOW STEP", "KNIFE THROW", "BLADE DANCE", "SMOKE BOMB", "EXECUTE" } };
    public static readonly int[][] Cooldowns = { new[] { 0, 5, 6, 7, 8, 10 }, new[] { 0, 6, 6, 10, 10, 14 }, new[] { 0, 5, 6, 8, 9, 11 }, new[] { 0, 5, 5, 9, 10, 13 }, new[] { 0, 6, 5, 8, 12, 12 } };
    public static readonly string[] Keys = { "R", "1", "2", "3", "4", "5" };
    public static readonly string[] Levels = { "GOBLIN FOREST CAMP", "FOREST TRAIL", "ABANDONED CAMP", "GOBLIN OUTPOST", "GOBRIDER STRONGHOLD" };

    static Bitmap SkillIcon(int cls, int slot) { return Img.Load(World.Heroes + "UI/Icons/Icon_" + Classes[cls] + "_" + SkillFiles[cls][slot] + ".png"); }
    static Bitmap Face(string cls)
    {
        var f = World.HeroFrame(cls, 0, 0);
        var c = new Cv(16, 16);
        for (int j = 0; j < 16; j++) for (int i = 0; i < 16; i++) c.Set(i, j, f.GetPixel(8 + i, 7 + j));
        return c.Scaled(2).B;
    }
    public static Bitmap FaceOf(string cls) { return Face(cls); }
    public static Bitmap RoundIcon(int cls) { return CircleIcon(SkillIcon(cls, 0)); }
    static Bitmap CircleIcon(Bitmap icon)
    {
        var o = new Cv(32, 32);
        for (int j = 0; j < 32; j++) for (int i = 0; i < 32; i++)
            {
                double dx = i + 0.5 - 16, dy = j + 0.5 - 16;
                if (dx * dx + dy * dy <= 16 * 16) o.Set(i, j, icon.GetPixel(i, j));
            }
        return o.B;
    }
    static readonly string[] NpcBust = {
        "......kkkk......", "....kkxxxxkk....", "...kxXXXXxxxk...", "..kxXXXXXXxxxk..", "..kxXXXXXXxxxk..", "..kxXXXXXXxxxk..",
        "...kxXXXXxxxk...", "....kkxxxxkk....", "...kkxxxxxxkk...", "..kxXXXXXXxxxk..", ".kxXXXXXXXXxxxk.", ".kxXXXXXXXXxxxk.",
        "kxXXXXXXXXXXxxxk", "kxXXXXXXXXXXxxxk", "kxXXXXXXXXXXxxxk", "kkkkkkkkkkkkkkkk" };
    static Bitmap NpcFace() { return new Cv(Img.Grid(NpcBust)).Scaled(2).B; }
    static Bitmap NpcBlock(Color tint)
    {
        var c = new Cv(10, 16);
        c.Fill(1, 1, 8, 14, tint); c.Box(0, 0, 10, 16, P.Ink); c.HLine(1, 1, 8, P.Mix(tint, P.TextW, 0.35)); c.Fill(3, 3, 4, 4, P.Mix(tint, P.Ink, 0.3));
        c.Set(0, 0, Color.Transparent); c.Set(9, 0, Color.Transparent);
        return c.B;
    }

    // ------------------------------------------------------------- worlds
    static void CampWorld(Shot s, int seed, bool fight)
    {
        if (s.Portrait)
        {
            s.World = World.Ground(144, 256, seed, new[] { new World.Blob(72, 150, 58, 48), new World.Blob(40, 80, 30, 26), new World.Blob(112, 70, 26, 22), new World.Blob(80, 235, 20, 30) });
            World.Tree(-6, 40, 'A'); World.Tree(150, 122, 'B'); World.Tree(4, 252, 'C'); World.Tree(146, 258, 'A');
            World.Tent(28, 60); World.Tent(118, 46); World.Brazier(66, 64); World.Bones(100, 96); World.Flag(18, 110); World.Crates(128, 230); World.Rock(16, 214, false);
            if (fight)
            {
                World.Hero("Aren", 72, 152, 6, 1);
                World.Monster("Goblin", 44, 146, 6, 1, true); World.Monster("Goblin", 104, 140, 6, 0); World.Monster("Goblin", 54, 192, 7, 0);
                World.Monster("GoblinArcher", 26, 104, 2, 0, true);
            }
            else { World.Hero("Aren", 72, 160); World.Monster("Goblin", 40, 120, 0, 0); World.Monster("Goblin", 104, 112, 2, 0, true); }
        }
        else
        {
            s.World = World.Ground(480, 270, seed, new[] { new World.Blob(240, 140, 120, 70), new World.Blob(130, 70, 50, 30), new World.Blob(360, 80, 60, 35), new World.Blob(250, 250, 30, 40) });
            World.Tree(20, 70, 'A'); World.Tree(70, 260, 'B'); World.Tree(455, 120, 'C'); World.Tree(430, 268, 'A'); World.Tree(14, 190, 'B'); World.Tree(470, 30, 'B');
            World.Tent(140, 64); World.Tent(340, 56); World.Tent(400, 120); World.Brazier(210, 70); World.Brazier(300, 68); World.Bones(100, 120); World.Flag(180, 100); World.Crates(370, 200); World.Rock(120, 210, true); World.Bush(300, 250, 'A');
            if (fight)
            {
                World.Hero("Aren", 240, 150, 6, 1);
                World.Monster("Goblin", 200, 140, 6, 1, true); World.Monster("Goblin", 284, 130, 6, 0); World.Monster("Goblin", 222, 198, 7, 0); World.Monster("GoblinArcher", 318, 190, 2, 0);
                World.Monster("Goblin", 150, 176, 3, 1, true);
            }
            else { World.Hero("Aren", 240, 160); }
        }
        World.Render(s.World);
    }
    static void ForestWorld(Shot s, int seed, bool hero)
    {
        if (s.Portrait)
        {
            s.World = World.Ground(144, 256, seed, new[] { new World.Blob(72, 170, 50, 40), new World.Blob(72, 260, 18, 50) });
            World.Tree(10, 70, 'A'); World.Tree(132, 60, 'C'); World.Tree(70, 40, 'B'); World.Tree(-4, 160, 'B'); World.Tree(150, 170, 'A'); World.Tree(20, 256, 'C'); World.Tree(130, 256, 'B');
            World.Bush(40, 120, 'A'); World.Rock(112, 120, true); World.Bush(104, 210, 'C');
            if (hero) World.Hero("Aren", 72, 172);
        }
        else
        {
            s.World = World.Ground(480, 270, seed, new[] { new World.Blob(240, 160, 110, 60), new World.Blob(240, 280, 30, 60) });
            for (int i = 0; i < 9; i++) World.Tree(10 + i * 58, 60 + (i % 2) * 14, "ABC"[i % 3]);
            World.Tree(20, 200, 'B'); World.Tree(460, 210, 'A'); World.Tree(90, 268, 'C'); World.Tree(400, 268, 'B');
            World.Bush(160, 120, 'A'); World.Rock(330, 120, true); World.Bush(300, 220, 'C');
            if (hero) World.Hero("Aren", 240, 162);
        }
        World.Render(s.World);
    }
    static void VillageWorld(Shot s, int seed)
    {
        if (s.Portrait)
        {
            s.World = World.Ground(144, 256, seed, new[] { new World.Blob(72, 60, 16, 70), new World.Blob(72, 160, 16, 70), new World.Blob(72, 240, 16, 40), new World.Blob(72, 140, 40, 22) });
            World.Tree(-2, 40, 'B'); World.Tree(148, 36, 'A'); World.Tree(0, 250, 'A'); World.Tree(146, 252, 'C');
            World.Crates(118, 120); World.Barrel(18, 96); World.Barrel(24, 102); World.Bush(120, 200, 'B');
            World.Put(NpcBlock(P.H("C07A4A")), 46, 140, 5, 15); World.Put(NpcBlock(P.H("6A7AA0")), 98, 140, 5, 15);
            World.Put(NpcBlock(P.H("6AA06A")), 46, 186, 5, 15); World.Put(NpcBlock(P.H("A08AC0")), 98, 186, 5, 15);
            World.Hero("Aren", 60, 150, 2, 0, true);
        }
        else
        {
            s.World = World.Ground(480, 270, seed, new[] { new World.Blob(240, 60, 20, 90), new World.Blob(240, 200, 20, 90), new World.Blob(240, 150, 110, 26) });
            for (int i = 0; i < 4; i++) { World.Tree(20 + i * 30, 40 + (i % 2) * 20, "AB"[i % 2]); World.Tree(460 - i * 30, 40 + (i % 2) * 20, "BA"[i % 2]); }
            World.Tree(30, 260, 'C'); World.Tree(450, 262, 'A');
            World.Crates(330, 128); World.Barrel(140, 126); World.Barrel(148, 132);
            World.Put(NpcBlock(P.H("C07A4A")), 200, 140, 5, 15); World.Put(NpcBlock(P.H("6A7AA0")), 290, 140, 5, 15);
            World.Put(NpcBlock(P.H("6AA06A")), 180, 190, 5, 15); World.Put(NpcBlock(P.H("A08AC0")), 300, 190, 5, 15);
            World.Hero("Aren", 222, 152, 2, 0, true);
        }
        World.Render(s.World);
    }
    static void ArenaWorld(Shot s, int seed)
    {
        if (s.Portrait)
        {
            s.World = World.Ground(144, 256, seed, new[] { new World.Blob(72, 140, 70, 90) });
            World.Rock(14, 60, true); World.Rock(130, 90, false); World.Rock(20, 220, false); World.Tree(-6, 140, 'B'); World.Tree(152, 200, 'A');
            World.Telegraph(72, 120, 2);
            World.Monster("Cyclops", 72, 112, 6, 1);
            World.Hero("Aren", 60, 172, 7, 1);
        }
        else
        {
            s.World = World.Ground(480, 270, seed, new[] { new World.Blob(240, 140, 170, 100) });
            World.Rock(60, 80, true); World.Rock(420, 90, true); World.Rock(90, 230, false); World.Tree(10, 120, 'B'); World.Tree(470, 200, 'A'); World.Tree(30, 268, 'C');
            World.Telegraph(240, 120, 2);
            World.Monster("Cyclops", 240, 104, 6, 1);
            World.Hero("Aren", 250, 196, 7, 1);
        }
        World.Render(s.World);
    }
    static void YardWorld(Shot s, int seed)
    {
        s.World = World.Ground(144, 256, seed, new[] { new World.Blob(72, 140, 60, 70) });
        World.Tree(-4, 50, 'A'); World.Tree(150, 60, 'B'); World.Tree(4, 252, 'B'); World.Tree(146, 250, 'C');
        World.Crates(24, 100); World.Rock(120, 210, false);
        World.Hero("Aren", 72, 166, 6, 0);
        World.Monster("Goblin", 52, 130, 3, 0); World.Monster("GoblinArcher", 100, 120, 0, 0); World.Monster("Goblin", 96, 172, 6, 1);
        World.Render(s.World);
    }

    // ------------------------------------------------------------- shared HUD pieces
    static void HudTopLeft(Shot s, int cls, int lvl, int hp, int hpMax, double exp, string name)
    {
        int x = 8, y = 8;
        s.Panel("UI_Panel_HUD.png", x + 30, y + 2, 120, 38);
        s.PortraitAt(Face(Classes[cls]), x, y);
        s.Pic(Kit.Get("UI_LevelBadge.png"), x + 26, y + 26);
        s.T(x + 34, y + 30, lvl.ToString(), P.GoldL, 1, 1, false);
        s.Bar(x + 42, y + 5, 104, "HP", hp / (double)hpMax, false, hp + "/" + hpMax);
        s.Bar(x + 42, y + 18, 104, "EXP", exp, true);
        s.T(x + 44, y + 27, name, P.GoldL);
    }
    static void HudTopRight(Shot s, bool combat, bool badge = false)
    {
        s.IconBtn(258, 8, combat ? "Pause" : "Settings");
        s.IconBtn(234, 8, "WorldMap");
        s.IconBtn(210, 8, "Inventory", "Normal", badge);
    }
    static void Coins(Shot s, int x, int y, int n)
    {
        int w = Txt.Width(n.ToString(), 1) + 26;
        s.Panel("UI_Panel_HUD.png", x - w, y, w, 16);
        s.Icon("Currency", x - w + 2, y);
        s.T(x - 4, y + 4, n.ToString(), P.GoldL, 1, 2);
    }
    static void Joystick(Shot s, int x, int y)
    {
        s.Pic(Kit.Get("UI_Joystick_Base.png"), x, y);
        s.Pic(Kit.Get("UI_Joystick_Knob.png"), x + 24, y + 12);
    }
    static readonly double[] ArcAngles = { 180, 157.5, 135, 112.5, 90 };
    static void MobileCombat(Shot s, int cls, int[] states, double[] cds)
    {
        int cx = 254, cy = 474, r = 104;
        s.Pic(Kit.Get("UI_AttackButton_Normal.png"), cx - 28, cy - 28);
        s.Pic(CircleIcon(SkillIcon(cls, 0)), cx - 16, cy - 16);
        for (int i = 0; i < 5; i++)
        {
            double a = ArcAngles[i] * Math.PI / 180;
            int x = (int)Math.Round(cx + r * Math.Cos(a)) - 19, y = (int)Math.Round(cy - r * Math.Sin(a)) - 19;
            string st = states == null ? "Default" : (states[i] == 1 ? "Selected" : (states[i] == 2 ? "Disabled" : "Default"));
            double cd = cds == null ? 0 : cds[i];
            s.Skill(x, y, SkillIcon(cls, i + 1), st, cd, cd > 0 ? Math.Max(1, (int)Math.Ceiling(cd * Cooldowns[cls][i + 1])).ToString() : null);
        }
    }
    static void PcHotbar(Shot s, int cls, double[] cds, int sel = -1)
    {
        // landscape: five slots bottom-centre with keys 1-5, R big at bottom-right (approved layout)
        int n = 5, gap = 6, w = 38, total = n * w + (n - 1) * gap;
        int x0 = (s.UW - total) / 2, y = s.UH - 56;
        s.Panel("UI_Panel_HUD.png", x0 - 6, y - 4, total + 12, 58);
        for (int i = 0; i < n; i++)
        {
            double cd = cds == null ? 0 : cds[i];
            s.Skill(x0 + i * (w + gap), y, SkillIcon(cls, i + 1), i == sel ? "Selected" : "Default", cd, cd > 0 ? Math.Max(1, (int)Math.Ceiling(cd * Cooldowns[cls][i + 1])).ToString() : null, Keys[i + 1]);
        }
        int rx = s.UW - 8 - 56, ry = s.UH - 8 - 66;
        s.Pic(Kit.Get("UI_AttackButton_Normal.png"), rx, ry);
        s.Pic(CircleIcon(SkillIcon(cls, 0)), rx + 12, ry + 12);
        var kc = Kit.Asset("UI_KeyCap.png");
        int kw2 = Txt.Width("R/SPACE", 1) + 8; s.Ui.Slice(kc.Img, rx + 28 - kw2 / 2, ry + 52, kw2, 11, 3, 3, 3, 3);
        s.T(rx + 28, ry + 54, "R/SPACE", P.GoldL, 1, 1, false);
    }
    static void Objective(Shot s, int cx, int y, string wave, string obj)
    {
        if (wave != null) s.Ribbon(cx, y, wave);
        if (obj != null)
        {
            int w = Txt.Width(obj, 1) + 14;
            s.Panel("UI_Panel_HUD.png", cx - w / 2, y + 20, w, 14);
            s.T(cx, y + 23, obj, P.TextBlue, 1, 1);
        }
    }
    static void Combo(Shot s, int x, int y, int hits)
    {
        s.TO(x, y, "x" + hits, P.GoldL, 2, 2);
        s.TO(x, y + 18, "HITS", P.TextW, 1, 2);
    }
    static void Damage(Shot s, int x, int y, string n, Color c) { s.TO(x, y, n, c, 2, 1); }
    static void QuestTracker(Shot s, int x, int y, int w, string title, string line, string prog)
    {
        s.Panel("UI_Panel_HUD.png", x, y, w, 38);
        s.Icon("Quests", x + 3, y + 3);
        s.T(x + 22, y + 7, title, P.GoldL);
        s.T(x + 8, y + 22, line, P.TextW);
        s.T(x + w - 6, y + 22, prog, P.GreenL, 1, 2);
    }
    static void InteractPrompt(Shot s, int wx, int wy, string key, string text)
    {
        int ux = s.WX(wx), uy = s.WY(wy - 16) - 32;
        int w = Txt.Width(text, 1) + 26;
        s.Panel("UI_Panel_Tooltip.png", ux - w / 2, uy, w, 16);
        var kc = Kit.Asset("UI_KeyCap.png");
        s.Ui.Slice(kc.Img, ux - w / 2 + 3, uy + 3, 11, 11, 3, 3, 3, 3);
        s.T(ux - w / 2 + 9, uy + 5, key, P.GoldL, 1, 1, false);
        s.T(ux - w / 2 + 18, uy + 5, text, P.TextW);
    }
    static void InteractBtn(Shot s, int x, int y, string icon, string label)
    {
        s.Pic(Kit.Get("UI_InteractButton_Normal.png"), x, y);
        s.Icon(icon, x + 12, y + 10);
        s.TO(x + 20, y + 44, label, P.GreenL, 1, 1);
    }
    static void Footer(Shot s, string note)
    {
        // annotation strip (not part of the in-game UI)
        s.Ui.Fill(0, s.UH - 0, s.UW, 0, P.Ink);
    }

    // ============================================================= PORTRAIT SCREENS
    public static List<Shot> Portrait()
    {
        var L = new List<Shot>();
        L.Add(P01()); L.Add(P02()); L.Add(P03()); L.Add(P04()); L.Add(P05()); L.Add(P06());
        L.Add(P07()); L.Add(P08()); L.Add(P09()); L.Add(P10()); L.Add(P11()); L.Add(P12()); L.Add(P13());
        L.Add(P14()); L.Add(P15()); L.Add(P16()); L.Add(P17()); L.Add(P18()); L.Add(P19()); L.Add(P20()); L.Add(P21()); L.Add(P22());
        L.Add(P23()); L.Add(P24()); L.Add(P25()); L.Add(P26()); L.Add(P27()); L.Add(P28()); L.Add(P29()); L.Add(P30());
        return L;
    }

    static void Logo(Shot s, int cx, int y, int scale)
    {
        s.TO(cx, y, "GAME01", P.GoldL, scale, 1);
        s.Pic(Kit.Get("UI_Divider_Gem.png"), cx - 3, y + 8 * scale + 6);
        int w = Txt.Width("GAME01", scale);
        for (int i = 0; i < w / 2 - 8; i++) { s.Ui.Px(cx - 6 - i, y + 8 * scale + 9, P.GoldD); s.Ui.Px(cx + 6 + i, y + 8 * scale + 9, P.GoldD); }
    }

    static Shot P01()
    {
        var s = new Shot("P01_Splash_Loading", "Splash / Loading", true);
        ForestWorld(s, 3, false);
        s.Dim(170);
        Logo(s, 144, 120, 4);
        s.ConceptTag(104, 172, "LOGO SLOT");
        s.Pic(World.HeroFrame("Aren", 0, 0), 120, 230, 3);
        s.Bar(44, 400, 200, "Progress", 0.62);
        s.T(144, 386, "LOADING", P.TextBlue, 1, 1);
        s.Panel("UI_Panel_Tooltip.png", 24, 430, 240, 40);
        s.T(36, 438, "TIP", P.GoldL);
        Txt.Para(s.Ui, 36, 452, 220, "Hold R to keep attacking.", P.TextW);
        s.T(280, 498, "v1.0", P.TextMute, 1, 2);
        return s;
    }
    static Shot P02()
    {
        var s = new Shot("P02_Main_Menu", "Main Menu", true);
        ForestWorld(s, 5, true);
        s.Dim(120);
        Logo(s, 144, 58, 4);
        s.ConceptTag(104, 110, "LOGO SLOT");
        int x = 50, w = 188, h = 26, y = 236, g = 34;
        s.Panel("UI_Panel_Main.png", x - 14, y - 16, w + 28, g * 5 + 28);
        s.Btn(x, y, w, h, "Primary", "Selected", "PLAY / VILLAGE");
        s.Pic(Kit.Get("UI_Cursor_Arrow.png"), x - 10, y + 8);
        s.Btn(x, y + g, w, h, "Secondary", "Normal", "CONTINUE");
        s.Btn(x, y + g * 2, w, h, "Secondary", "Normal", "LEVEL SELECT");
        s.Btn(x, y + g * 3, w, h, "Secondary", "Normal", "SETTINGS", "Settings");
        s.Btn(x, y + g * 4, w, h, "Danger", "Normal", "EXIT");
        s.T(8, 498, "v1.0", P.TextMute);
        s.T(280, 498, "LEVEL 4/4 CLEAR", P.TextMute, 1, 2);
        return s;
    }
    static Shot P03()
    {
        var s = new Shot("P03_Class_Select", "Character / Class Selection", true);
        ForestWorld(s, 8, false);
        s.Dim(150);
        int c = 0;
        s.Window(8, 28, 272, 404, "CHOOSE YOUR HERO", true);
        // showcase
        s.Panel("UI_Panel_Inset.png", 20, 46, 104, 116);
        s.Ui.Fill(36, 140, 72, 6, P.A(P.ForestM, 255)); s.Ui.HLine(38, 146, 68, P.ForestD);
        s.Pic(World.HeroFrame(Classes[c], 0, 0), 24, 52, 3);
        s.T(134, 52, Classes[c].ToUpper(), P.GoldL, 2);
        s.T(134, 72, Roles[c], P.TextBlue);
        Txt.Para(s.Ui, 134, 88, 138, Blurb[c], P.TextW, 11);
        string[] st = { "HP", "SPD", "DEF" };
        double[] v = { Stats[c, 0] / 1.5, Stats[c, 1] / 1.15, 0.25 + Stats[c, 2] * 4 };
        string[] vt = { "x1.0", "x1.0", "0%" };
        for (int i = 0; i < 3; i++) { s.T(134, 122 + i * 13, st[i], P.TextMute); s.Bar(164, 123 + i * 13, 70, "Progress", Math.Min(1, v[i]), true); s.T(272, 122 + i * 13, vt[i], P.TextW, 1, 2); }
        // skills
        s.T(20, 172, "SKILLS", P.GoldL);
        for (int i = 0; i < 6; i++) s.Skill(20 + i * 42, 184, SkillIcon(c, i), i == 1 ? "Selected" : "Default", 0, null, Keys[i]);
        s.Panel("UI_Panel_Tooltip.png", 20, 236, 248, 44);
        s.T(28, 243, SkillNames[c][1], P.GoldL); s.T(260, 243, "CD 5s", P.TextBlue, 1, 2);
        Txt.Para(s.Ui, 28, 257, 236, "Dash 2 tiles with the shield. Stuns 0.6s.", P.TextW, 11);
        // class picker
        s.T(20, 292, "CLASS", P.GoldL);
        for (int i = 0; i < 5; i++)
        {
            int x = 22 + i * 50, y = 304;
            s.PortraitAt(Face(Classes[i]), x, y);
            if (i == c) { s.Ui.Box(x - 2, y - 2, 42, 42, P.GoldL); s.Ui.Box(x - 1, y - 1, 40, 40, P.GoldW); }
            s.T(x + 19, y + 42, Classes[i].ToUpper(), i == c ? P.GoldL : P.TextMute, 1, 1);
        }
        s.Btn(32, 380, 110, 26, "Secondary", "Normal", "BACK", "Back");
        s.Btn(150, 380, 110, 26, "Primary", "Normal", "CONFIRM");
        return s;
    }
    static Shot P04()
    {
        var s = new Shot("P04_Village_HUD", "Village Gameplay HUD", true);
        VillageWorld(s, 11);
        HudTopLeft(s, 0, 2, 400, 400, 0.16, "AREN");
        HudTopRight(s, false, true);
        Coins(s, 280, 34, 128);
        s.Panel("UI_Panel_HUD.png", 98, 54, 92, 14); s.T(144, 57, "VILLAGE", P.TextBlue, 1, 1);
        QuestTracker(s, 8, 74, 150, "THE ROAD", "Bring ore", "0/1");
        s.MobLabel(46, 140, 18, "Mira", 1, P.GoldL); s.MobLabel(98, 140, 18, "Bram", 1, P.GoldL);
        s.MobLabel(46, 186, 18, "Lark", 1, P.GoldL); s.MobLabel(98, 186, 18, "Nell", 1, P.GoldL);
        InteractPrompt(s, 46, 140, "E", "SHOP");
        Joystick(s, 12, 432);
        s.Pic(Kit.Get("UI_AttackButton_Normal.png"), 226, 446); s.Pic(CircleIcon(SkillIcon(0, 0)), 238, 458);
        InteractBtn(s, 172, 452, "Shop", "SHOP");
        s.ConceptTag(150, 418, "HUB: NO SKILLS");
        return s;
    }
    static Shot P05()
    {
        var s = new Shot("P05_Pause", "Pause Menu", true);
        CampWorld(s, 2, true);
        HudTopLeft(s, 0, 2, 320, 400, 0.4, "AREN");
        s.Dim(170);
        s.Window(44, 140, 200, 214, "PAUSED", false);
        int y = 168;
        s.Btn(64, y, 160, 26, "Primary", "Selected", "RESUME");
        s.Btn(64, y + 34, 160, 26, "Secondary", "Normal", "SETTINGS", "Settings");
        s.Btn(64, y + 68, 160, 26, "Secondary", "Normal", "VILLAGE", "Housing");
        s.Btn(64, y + 102, 160, 26, "Secondary", "Normal", "LEVEL SELECT");
        s.Btn(64, y + 136, 160, 26, "Danger", "Normal", "MAIN MENU");
        s.T(144, 370, "GOBLIN FOREST CAMP  WAVE 2/4", P.TextMute, 1, 1);
        return s;
    }
    static Shot P06()
    {
        var s = new Shot("P06_Settings", "Settings", true);
        ForestWorld(s, 6, true);
        s.Dim(170);
        s.Window(12, 70, 264, 360, "SETTINGS", true);
        string[] tabs = { "AUDIO", "CONTROLS", "DISPLAY" };
        for (int i = 0; i < 3; i++) { var a = Kit.Asset(i == 0 ? "UI_Tab_Selected.png" : "UI_Tab_Normal.png"); s.Ui.Slice(a.Img, 26 + i * 80, 90, 76, 16, a.L, a.T, a.R, a.B); s.T(64 + i * 80, 96, tabs[i], i == 0 ? P.GoldL : P.TextMute, 1, 1, i == 0); }
        string[] rows = { "MUSIC", "SOUND FX" }; double[] vals = { 0.7, 0.85 };
        for (int i = 0; i < 2; i++)
        {
            int y = 124 + i * 36;
            s.T(28, y, rows[i], P.TextW);
            var t = Kit.Asset("UI_Slider_Track.png"); s.Ui.Slice(t.Img, 120, y - 1, 120, 6, t.L, t.T, t.R, t.B);
            var f = Kit.Get("UI_Slider_Fill.png"); for (int k = 0; k < (int)(116 * vals[i]); k++) s.Ui.Blit(f, 122 + k, y + 1, 1);
            s.Pic(Kit.Get("UI_Slider_Knob.png"), 120 + (int)(116 * vals[i]) - 3, y - 4);
            s.T(262, y, ((int)(vals[i] * 100)).ToString(), P.GoldL, 1, 2);
        }
        for (int k = 0; k < 232; k += 8) s.Pic(Kit.Get("UI_Divider_Line.png"), 28 + k, 196);
        s.T(28, 210, "INTERACT KEY", P.TextW);
        string[] keys = { "E", "F", "G" };
        for (int i = 0; i < 3; i++) s.Btn(150 + i * 38, 204, 32, 20, i == 0 ? "Primary" : "Secondary", i == 0 ? "Selected" : "Normal", keys[i]);
        s.T(28, 242, "LAYOUT", P.TextW);
        s.Btn(110, 236, 78, 20, "Primary", "Selected", "PORTRAIT"); s.Btn(192, 236, 70, 20, "Secondary", "Normal", "WIDE");
        
        s.Btn(28, 386, 110, 26, "Secondary", "Normal", "DEFAULTS");
        s.Btn(150, 386, 110, 26, "Primary", "Normal", "SAVE");
        return s;
    }
    static Shot P07()
    {
        var s = new Shot("P07_Dungeon_HUD", "Dungeon Gameplay HUD (mobile)", true);
        CampWorld(s, 1, true);
        s.MobLabel(44, 146, 14, "Goblin Lv.3", 0.55); s.MobLabel(104, 140, 14, "Goblin Lv.3", 0.8);
        s.MobLabel(54, 192, 14, "Goblin Lv.3"); s.MobLabel(26, 104, 16, "Goblin Archer Lv.3", 0.3);
        Damage(s, 40, 222, "18", P.TextW); Damage(s, 240, 210, "26", P.GoldL);
        HudTopLeft(s, 0, 2, 320, 400, 0.4, "AREN");
        HudTopRight(s, true);
        Objective(s, 144, 54, "WAVE 2/4", "CLEAR WAVES");
        Combo(s, 280, 150, 12);
        Joystick(s, 12, 432);
        MobileCombat(s, 0, new[] { 0, 0, 0, 1, 0 }, new[] { 0, 0.55, 0, 0, 0.8 });
        return s;
    }
    static Shot P08()
    {
        var s = new Shot("P08_Training_HUD", "Training Ground HUD", true);
        YardWorld(s, 4);
        s.MobLabel(52, 130, 14, "Goblin Lv.3", 0.6); s.MobLabel(100, 120, 16, "Goblin Archer Lv.3"); s.MobLabel(96, 172, 14, "Goblin Lv.3", 0.25);
        HudTopLeft(s, 0, 2, 380, 400, 0.72, "AREN");
        HudTopRight(s, true);
        s.Ribbon(144, 54, "TRAINING YARD");
        s.Panel("UI_Panel_HUD.png", 72, 74, 144, 28);
        s.Icon("Training", 76, 80); s.T(96, 79, "GOBLINS 3/3", P.TextW); s.T(96, 90, "+8 EXP EACH", P.GreenL);
        s.Panel("UI_Panel_HUD.png", 8, 110, 92, 26); s.T(14, 115, "LAST HIT", P.TextMute); s.T(14, 125, "26", P.GoldL);
        Combo(s, 280, 150, 5);
        Damage(s, 192, 230, "26", P.GoldL);
        Joystick(s, 12, 432);
        MobileCombat(s, 0, null, new[] { 0.3, 0, 0, 0, 0 });
        return s;
    }
    static Shot P09()
    {
        var s = new Shot("P09_Boss_HUD", "Boss Fight HUD", true);
        ArenaWorld(s, 9);
        HudTopLeft(s, 0, 3, 210, 400, 0.6, "AREN");
        HudTopRight(s, true);
        s.TO(144, 54, "CYCLOPS Lv.8", P.TextW, 1, 1);
        s.BossBar(16, 66, 256, 0.64);
        s.TO(144, 86, "288/450", P.TextW, 1, 1);
        Damage(s, 196, 150, "42", P.GoldL);
        Combo(s, 280, 150, 7);
        Joystick(s, 12, 432);
        MobileCombat(s, 0, new[] { 0, 0, 0, 0, 1 }, new[] { 0.4, 0.7, 0, 0, 0 });
        return s;
    }
    static Shot P10()
    {
        var s = new Shot("P10_Dungeon_Select", "Dungeon Selection (Warden)", true);
        VillageWorld(s, 12);
        s.Dim(170);
        s.Window(8, 40, 272, 420, "DUNGEONS", true);
        int[] best = { 1174, 832, 1037, 953 };
        for (int i = 0; i < 5; i++)
        {
            int y = 62 + i * 62;
            bool locked = i == 4, sel = i == 1;
            var a = Kit.Asset(locked ? "UI_ListRow_Disabled.png" : (sel ? "UI_ListRow_Selected.png" : "UI_ListRow_Normal.png"));
            s.Ui.Slice(a.Img, 20, y, 248, 56, a.L, a.T, a.R, a.B);
            s.Icon(locked ? "Lock" : (i == 3 ? "Boss" : "Dungeon"), 28, y + 8, 1);
            s.T(50, y + 8, (i + 1) + ". " + Levels[i], locked ? P.TextMute : P.GoldL);
            if (locked) { s.T(50, y + 24, "COMING SOON", P.TextMute); continue; }
            s.T(50, y + 24, (i == 1 || i == 3) ? "WAVES + EXIT" : "CLEAR 4 WAVES", P.TextBlue);
            s.T(50, y + 38, "BEST " + best[i], P.TextW);
            for (int k = 0; k < 3; k++) s.Icon("Star", 196 + k * 18, y + 32);
        }
        s.Btn(20, 380, 118, 26, "Secondary", "Normal", "BACK", "Back");
        s.Btn(150, 380, 118, 26, "Primary", "Normal", "START", "Dungeon");
        s.T(144, 420, "WARDEN", P.TextMute, 1, 1);
        return s;
    }
    static Shot P11()
    {
        var s = new Shot("P11_Level_Complete", "Level Complete", true);
        CampWorld(s, 13, false);
        s.Dim(170);
        s.Panel("UI_Panel_Reward.png", 20, 100, 248, 300);
        s.Ribbon(144, 92, "LEVEL COMPLETE", "UI_Header_Ribbon_Gold.png", 180);
        s.T(144, 122, "FOREST TRAIL", P.TextBlue, 1, 1);
        s.Pic(Kit.Get("UI_Star_Large_Filled.png"), 92, 138); s.Pic(Kit.Get("UI_Star_Large_Filled.png"), 132, 132); s.Pic(Kit.Get("UI_Star_Large_Empty.png"), 172, 138);
        string[] k = { "TIME", "KILLS", "BEST COMBO", "SCORE" }; string[] v = { "2:31", "16", "x24", "832" };
        for (int i = 0; i < 4; i++) { s.T(44, 180 + i * 18, k[i], P.TextMute); s.T(244, 180 + i * 18, v[i], i == 3 ? P.GoldL : P.TextW, 1, 2); }
        s.T(44, 254, "NEW BEST!", P.GreenL);
        s.Panel("UI_Panel_HUD.png", 40, 270, 208, 22); s.Icon("Currency", 46, 273); s.T(66, 277, "+5 COINS (FIRST CLEAR)", P.GoldL);
        s.Btn(36, 304, 216, 26, "Primary", "Selected", "NEXT LEVEL");
        s.Btn(36, 336, 104, 24, "Secondary", "Normal", "RETRY");
        s.Btn(148, 336, 104, 24, "Secondary", "Normal", "VILLAGE");
        s.Btn(36, 366, 216, 24, "Secondary", "Normal", "LEVEL SELECT");
        return s;
    }
    static Shot P12()
    {
        var s = new Shot("P12_Game_Over", "Game Over / Retry", true);
        CampWorld(s, 14, false);
        s.Tint(P.A(P.RedD, 90));
        s.Dim(140);
        s.Panel("UI_Panel_Main.png", 32, 150, 224, 220);
        s.Ribbon(144, 142, "DEFEATED", "UI_Header_Ribbon_Red.png", 150);
        s.Pic(Img.Load(World.Heroes + "Aren/Extra/Aren_Death_Down_2.png"), 112, 168, 2);
        string[] k = { "WAVE", "KILLS", "TIME" }; string[] v = { "3/4", "11", "1:48" };
        for (int i = 0; i < 3; i++) { s.T(56, 238 + i * 16, k[i], P.TextMute); s.T(232, 238 + i * 16, v[i], P.TextW, 1, 2); }
        s.Btn(52, 292, 184, 26, "Primary", "Selected", "RETRY");
        s.Btn(52, 324, 88, 24, "Secondary", "Normal", "VILLAGE");
        s.Btn(148, 324, 88, 24, "Danger", "Normal", "MENU");
        return s;
    }
    static Shot P13()
    {
        var s = new Shot("P13_Reward_Popup", "Reward Popup", true);
        VillageWorld(s, 15);
        HudTopLeft(s, 0, 2, 400, 400, 0.36, "AREN");
        s.Dim(150);
        s.Panel("UI_Panel_Reward.png", 34, 160, 220, 190);
        s.Ribbon(144, 152, "QUEST COMPLETE", "UI_Header_Ribbon_Gold.png", 170);
        s.T(144, 180, "CLEAR THE YARD", P.GoldL, 1, 1);
        string[] ic = { "Currency", "Item_QuestToken", "EXP" }; string[] t = { "+5 COINS", "QUEST TOKEN x1", "+8 EXP" };
        for (int i = 0; i < 3; i++)
        {
            int y = 200 + i * 28;
            s.Slot(56, y, ic[i]);
            s.T(86, y + 7, t[i], i == 2 ? P.GreenL : P.TextW);
        }
        s.Btn(64, 300, 160, 26, "Primary", "Selected", "CLAIM", "Confirm");
        return s;
    }
    static Shot P14()
    {
        var s = new Shot("P14_Inventory", "Inventory / Bag", true);
        VillageWorld(s, 16);
        s.Dim(170);
        s.Window(8, 26, 272, 462, "BAG", true);
        string[] tabs = { "ALL", "RESOURCE", "GEAR", "QUEST" };
        int tx = 18;
        for (int i = 0; i < 4; i++) { int w = Txt.Width(tabs[i], 1) + 14; var a = Kit.Asset(i == 0 ? "UI_Tab_Selected.png" : "UI_Tab_Normal.png"); s.Ui.Slice(a.Img, tx, 44, w, 16, a.L, a.T, a.R, a.B); s.T(tx + w / 2, 50, tabs[i], i == 0 ? P.GoldL : P.TextMute, 1, 1, i == 0); tx += w + 2; }
        s.Panel("UI_Panel_Inset.png", 18, 60, 252, 154);
        string[] items = { "Item_Ore", "Item_Wood", "Item_Herb", "Item_Sprout", "Item_Potion", "Item_TrainingBlade", "Item_QuestToken" };
        int[] qty = { 3, 2, 1, 2, 1, 1, 1 };
        for (int i = 0; i < 50; i++)
        {
            int x = 24 + (i % 10) * 24, y = 66 + (i / 10) * 24;
            if (y + 22 > 212) break;
            if (i < items.Length) s.Slot(x, y, items[i], qty[i], i == 5 ? "Selected" : "");
            else s.Slot(x, y, null, 0, i >= 25 ? "Disabled" : "");
        }
        s.Ui.Fill(18, 186, 252, 1, P.Ink);
        s.T(24, 196, "7/25 SLOTS", P.TextMute);
        Coins(s, 266, 192, 128);
        // detail
        s.Panel("UI_Panel_Parchment.png", 18, 222, 252, 210);
        s.Pic(Kit.Get("UI_Equip_Slot_Selected.png"), 28, 232); s.Icon("Item_TrainingBlade", 33, 237);
        s.T(62, 234, "TRAINING BLADE", P.TextDark, 1, 0, false);
        s.T(62, 248, "EQUIPMENT - WEAPON", P.WoodM, 1, 0, false);
        s.Ui.HLine(28, 264, 232, P.ParchD);
        Txt.Para(s.Ui, 28, 272, 232, "A wooden practice blade made by Bram from 1 ore and 1 wood.", P.TextDark, 11);
        s.T(28, 316, "QTY", P.WoodM, 1, 0, false); s.T(258, 316, "1", P.TextDark, 1, 2, false);
        s.T(28, 330, "SELL", P.WoodM, 1, 0, false); s.T(258, 330, "-", P.TextDark, 1, 2, false);
        s.ConceptTag(28, 346, "DAMAGE BONUS NOT APPLIED YET");
        s.Btn(28, 394, 112, 26, "Secondary", "Normal", "DETAILS");
        s.Btn(148, 394, 112, 26, "Primary", "Normal", "EQUIP", "Equipment");
        s.T(144, 444, "LV 2   EXP 58/100", P.TextBlue, 1, 1);
        return s;
    }
    static Shot P15()
    {
        var s = new Shot("P15_Equipment", "Equipment", true);
        VillageWorld(s, 17);
        s.Dim(170);
        s.Window(8, 30, 272, 440, "EQUIPMENT", true);
        s.Panel("UI_Panel_Inset.png", 84, 56, 120, 150);
        s.Ui.Fill(108, 186, 72, 6, P.ForestM);
        s.Pic(World.HeroFrame("Aren", 0, 0), 96, 92, 3);
        string[] slots = { "Equip_Helmet", "Equip_Armor", "Equip_Boots", "Equip_Weapon", "Equip_Shield", "Equip_Ring" };
        string[] names = { "HEAD", "BODY", "FEET", "WEAPON", "OFF-HAND", "TRINKET" };
        for (int i = 0; i < 6; i++)
        {
            bool left = i < 3; int x = left ? 30 : 232, y = 60 + (i % 3) * 50;
            bool weapon = i == 3;
            s.Pic(Kit.Get(weapon ? "UI_Equip_Slot_Selected.png" : "UI_Equip_Slot_Locked.png"), x, y);
            if (weapon) s.Icon("Item_TrainingBlade", x + 5, y + 5); else s.Icon(slots[i] + "_Ghost", x + 5, y + 5);
            s.T(x + 13, y + 30, names[i], weapon ? P.GoldL : P.TextMute, 1, 1);
        }
        s.ConceptTag(30, 212, "ONLY WEAPON SLOT EXISTS TODAY");
        s.Panel("UI_Panel_Parchment.png", 18, 232, 252, 130);
        s.T(28, 242, "TRAINING BLADE", P.TextDark, 1, 0, false); s.T(260, 242, "EQUIPPED", P.ForestM, 1, 2, false);
        s.Ui.HLine(28, 256, 232, P.ParchD);
        string[] k = { "CLASS", "HP", "SPEED", "DAMAGE TAKEN" }; string[] v = { "KNIGHT", "400", "x1.0", "100%" };
        for (int i = 0; i < 4; i++) { s.T(28, 264 + i * 14, k[i], P.WoodM, 1, 0, false); s.T(260, 264 + i * 14, v[i], P.TextDark, 1, 2, false); }
        s.T(28, 326, "WEAPON BONUS: NONE", P.WoodM, 1, 0, false);
        s.Btn(28, 380, 112, 26, "Secondary", "Normal", "UNEQUIP");
        s.Btn(148, 380, 112, 26, "Primary", "Normal", "BAG", "Inventory");
        s.Pic(Kit.Get("UI_Tab_Selected.png"), -40, -40);
        return s;
    }
    static Shot P16()
    {
        var s = new Shot("P16_Hero_Info", "Hero Information", true);
        VillageWorld(s, 18);
        s.Dim(170);
        s.Window(8, 30, 272, 448, "HERO", true);
        s.PortraitAt(Face("Aren"), 22, 48);
        s.Pic(Kit.Get("UI_LevelBadge.png"), 48, 74); s.T(56, 78, "2", P.GoldL, 1, 1, false);
        s.T(70, 50, "AREN", P.GoldL, 2); s.T(70, 70, "KNIGHT", P.TextBlue);
        s.T(22, 96, "EXP", P.TextMute); s.Bar(54, 94, 210, "EXP", 0.16, true); s.T(264, 104, "58/100", P.TextW, 1, 2);
        s.T(22, 120, "HP", P.TextMute); s.Bar(54, 118, 210, "HP", 1, false, "400/400");
        s.Panel("UI_Panel_Inset.png", 18, 140, 252, 74);
        string[] k = { "SPEED", "DAMAGE TAKEN", "BASIC ATTACK", "COINS" }; string[] v = { "x1.0", "100%", "MELEE 3-HIT", "128" };
        for (int i = 0; i < 4; i++) { s.T(26, 148 + i * 16, k[i], P.TextMute); s.T(262, 148 + i * 16, v[i], P.TextW, 1, 2); }
        s.T(22, 224, "SKILLS", P.GoldL);
        for (int i = 0; i < 6; i++)
        {
            int y = 236 + i * 34;
            s.Pic(Kit.Get("UI_ListRow_Normal.png"), -99, -99);
            var a = Kit.Asset("UI_ListRow_Normal.png"); s.Ui.Slice(a.Img, 18, y, 252, 32, a.L, a.T, a.R, a.B);
            var ic = SkillIcon(0, i); s.Pic(ic, 20, y);
            s.Ui.Blit(Kit.Get("UI_KeyCap.png"), 46, y + 10);
            s.T(51, y + 12, Keys[i], P.GoldL, 1, 1, false);
            s.T(64, y + 12, SkillNames[0][i], P.TextW);
            s.T(262, y + 12, i == 0 ? "0.35s" : Cooldowns[0][i] + "s", P.TextBlue, 1, 2);
        }
        return s;
    }
    static Shot P17()
    {
        var s = new Shot("P17_Quest_Journal", "Quest Journal", true);
        VillageWorld(s, 19);
        s.Dim(170);
        s.Window(8, 26, 272, 462, "QUESTS", true);
        string[] tabs = { "ACTIVE", "DONE" };
        for (int i = 0; i < 2; i++) { var a = Kit.Asset(i == 0 ? "UI_Tab_Selected.png" : "UI_Tab_Normal.png"); s.Ui.Slice(a.Img, 18 + i * 66, 44, 64, 16, a.L, a.T, a.R, a.B); s.T(50 + i * 66, 50, tabs[i], i == 0 ? P.GoldL : P.TextMute, 1, 1, i == 0); }
        string[] q = { "Clear the yard", "Bring ore", "Harvest a sprout", "Forge a blade", "Clear depth 1", "Defeat Cyclops" };
        for (int i = 0; i < 6; i++)
        {
            int y = 64 + i * 24;
            bool done = i == 0, act = i == 1, locked = i > 1;
            var a = Kit.Asset(act ? "UI_ListRow_Selected.png" : (locked ? "UI_ListRow_Disabled.png" : "UI_ListRow_Normal.png"));
            s.Ui.Slice(a.Img, 18, y, 252, 22, a.L, a.T, a.R, a.B);
            s.Icon(done ? "Confirm" : (locked ? "Lock" : "Quests"), 22, y + 3);
            s.T(42, y + 7, (i + 1) + ". " + q[i], locked ? P.TextMute : (done ? P.GreenL : P.GoldL));
            s.T(262, y + 7, done ? "DONE" : (act ? "0/1" : ""), done ? P.GreenL : P.TextW, 1, 2);
        }
        s.Panel("UI_Panel_Parchment.png", 18, 216, 252, 222);
        s.T(28, 226, "THE ROAD - STEP 2", P.WoodM, 1, 0, false);
        s.T(28, 240, "BRING ORE", P.TextDark, 2, 0, false);
        s.Ui.HLine(28, 260, 232, P.ParchD);
        s.T(28, 268, "OBJECTIVE", P.WoodM, 1, 0, false);
        s.Icon("Item_Ore", 28, 280); s.T(48, 285, "Gather ore", P.TextDark, 1, 0, false); s.T(258, 285, "0/1", P.RedD, 1, 2, false);
        s.Bar(28, 300, 232, "Progress", 0.0, true);
        s.T(28, 316, "REWARD", P.WoodM, 1, 0, false);
        s.Slot(28, 328, "Currency"); s.T(56, 335, "5 COINS", P.TextDark, 1, 0, false);
        s.T(28, 360, "GIVER: LARK", P.WoodM, 1, 0, false);
        s.Btn(28, 396, 112, 26, "Secondary", "Normal", "TRACK");
        s.Btn(148, 396, 112, 26, "Primary", "Disabled", "CLAIM");
        s.T(144, 448, "Claim at Lark when done", P.TextMute, 1, 1);
        return s;
    }
    public static void Dialogue(Shot s, string name, string service, string line, string icon, string[] choices, int x, int y, int w)
    {
        int h = 94 + choices.Length * 30;
        s.Panel("UI_NPC_DialoguePanel.png", x, y, w, h);
        s.PortraitAt(NpcFace(), x + 10, y - 22);
        s.ConceptTag(x + 52, y - 18, "NPC PORTRAIT SLOT");
        var np = Kit.Asset("UI_NamePlate.png");
        int nw = Txt.Width(name + " - " + service, 1) + 26;
        s.Ui.Slice(np.Img, x + 52, y - 4, nw, 14, np.L, np.T, np.R, np.B);
        s.Icon(icon, x + 54, y - 5);
        s.T(x + 72, y, name + " - " + service, P.GoldW);
        Txt.Para(s.Ui, x + 14, y + 22, w - 28, line, P.TextW, 12);
        s.Pic(Kit.Get("UI_Dialogue_Next.png"), x + w - 20, y + 66);
        for (int i = 0; i < choices.Length; i++)
            s.Btn(x + 12, y + 80 + i * 30, w - 24, 26, i == 0 ? "Primary" : "Secondary", i == 0 ? "Selected" : "Normal", choices[i], i == 0 ? icon : null);
    }
    static Shot P18()
    {
        var s = new Shot("P18_NPC_Dialogue", "NPC Dialogue", true);
        VillageWorld(s, 20);
        HudTopLeft(s, 0, 2, 400, 400, 0.16, "AREN");
        Dialogue(s, "LARK", "QUESTS", "The road starts here. Clear the yard, then bring me ore.", "Quests", new[] { "SHOW QUESTS", "GOODBYE" }, 8, 330, 272);
        return s;
    }
    static Shot P19()
    {
        var s = new Shot("P19_Shop", "Shop (Mira)", true);
        VillageWorld(s, 21);
        s.Dim(170);
        s.Window(8, 36, 272, 440, "MIRA'S SHOP", true);
        string[] tabs = { "BUY", "SELL" };
        for (int i = 0; i < 2; i++) { var a = Kit.Asset(i == 1 ? "UI_Tab_Selected.png" : "UI_Tab_Normal.png"); s.Ui.Slice(a.Img, 18 + i * 66, 54, 64, 16, a.L, a.T, a.R, a.B); s.T(50 + i * 66, 60, tabs[i], i == 1 ? P.GoldL : P.TextMute, 1, 1, i == 1); }
        Coins(s, 270, 54, 128);
        string[] it = { "Item_Ore", "Item_Wood", "Item_Herb", "Item_Sprout" }; string[] nm = { "ORE", "WOOD", "HERB", "SPROUT" }; int[] have = { 3, 2, 1, 2 }; int[] price = { 2, 1, 2, 1 };
        for (int i = 0; i < 4; i++)
        {
            int y = 76 + i * 30;
            var a = Kit.Asset(i == 0 ? "UI_ListRow_Selected.png" : "UI_ListRow_Normal.png"); s.Ui.Slice(a.Img, 18, y, 252, 28, a.L, a.T, a.R, a.B);
            s.Slot(22, y + 3, it[i]);
            s.T(50, y + 6, nm[i], P.GoldL); s.T(50, y + 17, "HAVE " + have[i], P.TextMute);
            s.Icon("Currency", 214, y + 6); s.T(262, y + 10, price[i].ToString(), P.GoldL, 1, 2);
        }
        s.Panel("UI_Panel_Parchment.png", 18, 206, 252, 140);
        s.T(28, 216, "SELL ORE", P.TextDark, 2, 0, false);
        s.T(28, 240, "AMOUNT", P.WoodM, 1, 0, false);
        s.Btn(110, 234, 26, 20, "Secondary", "Normal", "-"); s.T(160, 240, "3", P.TextDark, 1, 1, false); s.Btn(184, 234, 26, 20, "Secondary", "Normal", "+");
        s.T(28, 266, "YOU GET", P.WoodM, 1, 0, false); s.Icon("Currency", 200, 262); s.T(258, 266, "6", P.TextDark, 1, 2, false);
        s.Btn(28, 304, 232, 26, "Primary", "Normal", "SELL", "Currency");
        s.T(28, 360, "BUY TAB: POTION 5 COINS", P.TextMute);
        s.Slot(28, 374, "Item_Potion"); s.T(56, 381, "POTION", P.GoldL); s.Icon("Currency", 214, 377); s.T(262, 381, "5", P.GoldL, 1, 2);
        return s;
    }
    static Shot P20()
    {
        var s = new Shot("P20_Blacksmith", "Blacksmith / Crafting (Bram)", true);
        VillageWorld(s, 22);
        s.Dim(170);
        s.Window(8, 50, 272, 400, "BRAM'S FORGE", true);
        s.T(20, 70, "RECIPES", P.GoldL);
        var a = Kit.Asset("UI_ListRow_Selected.png"); s.Ui.Slice(a.Img, 18, 82, 252, 30, a.L, a.T, a.R, a.B);
        s.Slot(22, 86, "Item_TrainingBlade"); s.T(50, 92, "TRAINING BLADE", P.GoldL); s.T(50, 102, "WEAPON", P.TextMute);
        var d = Kit.Asset("UI_ListRow_Disabled.png"); s.Ui.Slice(d.Img, 18, 114, 252, 30, d.L, d.T, d.R, d.B);
        s.Slot(22, 118, null, 0, "Disabled"); s.Icon("Lock", 25, 121); s.T(50, 128, "MORE RECIPES LATER", P.TextMute);
        s.Panel("UI_Panel_Parchment.png", 18, 154, 252, 240);
        s.Pic(Kit.Get("UI_Equip_Slot_Selected.png"), 131, 164); s.Icon("Item_TrainingBlade", 136, 169);
        s.T(144, 196, "TRAINING BLADE", P.TextDark, 1, 1, false);
        s.Ui.HLine(28, 210, 232, P.ParchD);
        s.T(28, 218, "NEEDS", P.WoodM, 1, 0, false);
        s.Slot(28, 232, "Item_Ore"); s.T(56, 239, "ORE", P.TextDark, 1, 0, false); s.T(258, 239, "3/1", P.ForestM, 1, 2, false);
        s.Slot(28, 258, "Item_Wood"); s.T(56, 265, "WOOD", P.TextDark, 1, 0, false); s.T(258, 265, "2/1", P.ForestM, 1, 2, false);
        s.Icon("Crafting", 28, 290); s.T(48, 294, "Result goes to the bag.", P.TextDark, 1, 0, false);
        s.Bar(28, 316, 232, "Progress", 0.0);
        s.Btn(28, 340, 232, 28, "Positive", "Normal", "CRAFT", "Crafting");
        s.Btn(64, 404, 160, 24, "Secondary", "Normal", "BACK", "Back");
        return s;
    }
    static Shot P21()
    {
        var s = new Shot("P21_Storage", "Storage (Nell)", true);
        VillageWorld(s, 23);
        s.Dim(170);
        s.Window(8, 26, 272, 462, "NELL'S STORAGE", true);
        s.T(20, 46, "STORAGE", P.GoldL); s.T(268, 46, "2 KINDS", P.TextMute, 1, 2);
        s.Panel("UI_Panel_Inset.png", 18, 58, 252, 106);
        string[] st = { "Item_Ore", "Item_Wood" }; int[] sq = { 12, 6 };
        for (int i = 0; i < 20; i++) s.Slot(24 + (i % 10) * 24, 64 + (i / 10) * 24, i < 2 ? st[i] : null, i < 2 ? sq[i] : 0, i == 0 ? "Selected" : "");
        s.T(24, 118, "ORE x12", P.GoldL);
        s.Btn(24, 132, 116, 24, "Secondary", "Normal", "TAKE 1"); s.Btn(146, 132, 118, 24, "Secondary", "Normal", "TAKE ALL");
        for (int k = 0; k < 248; k += 8) s.Pic(Kit.Get("UI_Divider_Line.png"), 20 + k, 172);
        s.Pic(Kit.Get("UI_Divider_Gem.png"), 141, 169);
        s.T(20, 184, "BAG", P.GoldL);
        s.Panel("UI_Panel_Inset.png", 18, 196, 252, 106);
        string[] it = { "Item_Ore", "Item_Wood", "Item_Herb", "Item_Sprout", "Item_Potion", "Item_TrainingBlade", "Item_QuestToken" }; int[] q = { 3, 2, 1, 2, 1, 1, 1 };
        for (int i = 0; i < 20; i++) s.Slot(24 + (i % 10) * 24, 202 + (i / 10) * 24, i < 7 ? it[i] : null, i < 7 ? q[i] : 0);
        s.Btn(24, 256, 116, 24, "Secondary", "Normal", "STORE 1"); s.Btn(146, 256, 118, 24, "Primary", "Normal", "STORE ALL");
        s.T(144, 320, "Stack limit 99 per item", P.TextMute, 1, 1);
        return s;
    }
    static Shot P22()
    {
        var s = new Shot("P22_Skill_Tree", "Skill Tree (concept)", true);
        ForestWorld(s, 24, false);
        s.Dim(180);
        s.Window(8, 30, 272, 448, "SKILL TREE", true);
        s.ConceptTag(18, 50, "CONCEPT - NO UNLOCKS YET");
        s.Panel("UI_Panel_Inset.png", 18, 68, 252, 300);
        int[][] pos = { new[] { 128, 84 }, new[] { 72, 150 }, new[] { 184, 150 }, new[] { 40, 220 }, new[] { 104, 220 }, new[] { 152, 220 }, new[] { 216, 220 }, new[] { 128, 300 } };
        int[][] links = { new[] { 0, 1 }, new[] { 0, 2 }, new[] { 1, 3 }, new[] { 1, 4 }, new[] { 2, 5 }, new[] { 2, 6 }, new[] { 4, 7 }, new[] { 5, 7 } };
        foreach (var l in links)
        {
            int x0 = pos[l[0]][0] + 19, y0 = pos[l[0]][1] + 19, x1 = pos[l[1]][0] + 19, y1 = pos[l[1]][1] + 19;
            bool lit = l[0] == 0 && l[1] == 1;
            int n = Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0));
            for (int i = 0; i <= n; i++) { int x = x0 + (x1 - x0) * i / n, y = y0 + (y1 - y0) * i / n; s.Ui.Fill(x - 1, y - 1, 3, 3, lit ? P.GoldL : P.Stone); }
        }
        for (int i = 0; i < pos.Length; i++)
        {
            string st = i == 0 ? "Selected" : (i == 1 ? "Default" : "Disabled");
            s.Skill(pos[i][0], pos[i][1], SkillIcon(0, Math.Min(5, i % 6)), st);
            if (i > 1) s.Icon("Lock", pos[i][0] + 11, pos[i][1] + 11);
        }
        s.Panel("UI_Panel_Tooltip.png", 18, 374, 252, 54);
        s.T(26, 382, "SHIELD BASH +", P.GoldL); s.T(262, 382, "1 PT", P.TextBlue, 1, 2);
        Txt.Para(s.Ui, 26, 396, 236, "Node row exists in WorldFoundation. Effect not designed.", P.TextMute, 11);
        s.Btn(64, 436, 160, 26, "Primary", "Disabled", "UNLOCK");
        return s;
    }
    static Shot P23()
    {
        var s = new Shot("P23_World_Map", "World Map", true);
        VillageWorld(s, 25);
        s.Dim(180);
        s.Window(8, 26, 272, 462, "WORLD MAP", true);
        s.Panel("UI_Panel_Parchment.png", 18, 44, 252, 268);
        var map = new Cv(242, 258);
        for (int y = 0; y < 258; y++) for (int x = 0; x < 242; x++) { double n = Math.Sin(x * 0.07) * Math.Cos(y * 0.05); if (n > 0.55) map.Set(x, y, P.Mix(P.Parch, P.GreenL, 0.35)); else if (n < -0.7) map.Set(x, y, P.Mix(P.Parch, P.BlueL, 0.3)); }
        s.Pic(map.B, 23, 49);
        string[] zn = { "VILLAGE", "TRAINING", "RESOURCE", "FARM", "CYCLOPS", "DUNGEONS", "EXPLORE", "TREASURE", "WARD", "RIDGE 2", "HOUSING", "???" };
        string[] ic = { "Housing", "Training", "Mining", "Farming", "Boss", "Dungeon", "WorldMap", "Treasure", "Skill", "Boss", "Housing", "Lock" };
        int[][] p = { new[] { 136, 196 }, new[] { 56, 196 }, new[] { 216, 196 }, new[] { 72, 250 }, new[] { 200, 250 }, new[] { 136, 100 }, new[] { 56, 100 }, new[] { 216, 100 }, new[] { 56, 150 }, new[] { 216, 150 }, new[] { 136, 250 }, new[] { 136, 62 } };
        for (int i = 1; i < p.Length; i++)
        {
            int x0 = p[0][0], y0 = p[0][1], x1 = p[i][0], y1 = p[i][1]; int n = Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0));
            for (int k = 0; k <= n; k += 4) s.Ui.Fill(x0 + (x1 - x0) * k / n, y0 + (y1 - y0) * k / n, 2, 2, P.ParchD);
        }
        for (int i = 0; i < p.Length; i++)
        {
            bool locked = i == 11, here = i == 0;
            s.Pic(Kit.Get(here ? "UI_Inventory_Slot_Selected.png" : (locked ? "UI_Inventory_Slot_Disabled.png" : "UI_Inventory_Slot.png")), p[i][0] - 11, p[i][1] - 11);
            s.Icon(ic[i], p[i][0] - 8, p[i][1] - 8);
            s.TO(p[i][0], p[i][1] + 13, zn[i], here ? P.GoldL : (locked ? P.TextMute : P.TextW), 1, 1);
        }
        s.Pic(Icons.Get("Marker_Player"), 145, 176);
        s.ConceptTag(24, 296, "CONCEPT: DRAWN MAP");
        s.Panel("UI_Panel_Tooltip.png", 18, 320, 252, 58);
        s.T(26, 328, "CYCLOPS ARENA", P.GoldL); s.T(262, 328, "BOSS", P.RedL, 1, 2);
        Txt.Para(s.Ui, 26, 342, 236, "Regional boss. +25 EXP each defeat.", P.TextW, 11);
        s.Btn(18, 388, 120, 26, "Secondary", "Normal", "PAGE 2");
        s.Btn(146, 388, 124, 26, "Primary", "Normal", "TRAVEL", "WorldMap");
        return s;
    }
    static Shot P24()
    {
        var s = new Shot("P24_Minimap", "Minimap Components", true);
        CampWorld(s, 26, false);
        HudTopLeft(s, 0, 2, 400, 400, 0.16, "AREN");
        s.IconBtn(258, 8, "Settings"); s.IconBtn(234, 8, "Inventory");
        // minimap in HUD
        s.Pic(Kit.Get("UI_Minimap_Back.png"), 206, 40);
        var mini = new Cv(68, 68);
        for (int y = 0; y < 68; y++) for (int x = 0; x < 68; x++) { var c = s.World.Get(Math.Min(143, x * 144 / 68), Math.Min(255, y * 256 / 68 / 2 + 40)); mini.Set(x, y, P.Mix(c, P.ForestD, 0.35)); }
        s.Pic(mini.B, 206, 40);
        s.Pic(Kit.Get("UI_Minimap_Frame.png"), 200, 34);
        s.Pic(Icons.Get("Marker_Player"), 237, 71); s.Pic(Icons.Get("Marker_Enemy"), 222, 60); s.Pic(Icons.Get("Marker_Enemy"), 250, 56); s.Pic(Icons.Get("Marker_Gate"), 238, 44); s.Pic(Icons.Get("Marker_Quest"), 212, 92);
        s.Panel("UI_Panel_HUD.png", 200, 116, 80, 14); s.T(240, 119, "CAMP", P.TextBlue, 1, 1);
        // legend panel
        s.Panel("UI_Panel_Main.png", 16, 250, 256, 176);
        s.T(144, 262, "MAP MARKERS", P.GoldL, 1, 1);
        string[] mk = { "Player", "NPC", "Quest", "Boss", "Gate", "Chest", "Node", "Enemy" };
        string[] lb = { "YOU", "NPC", "QUEST", "BOSS", "GATE", "CHEST", "GATHER", "ENEMY" };
        for (int i = 0; i < 8; i++) { int x = 32 + (i % 2) * 120, y = 282 + (i / 2) * 26; s.Pic(Icons.Get("Marker_" + mk[i]), x, y, 2); s.T(x + 22, y + 3, lb[i], P.TextW); }
        s.ConceptTag(28, 392, "CONCEPT (TODAY: ZONE LABEL)");
        return s;
    }
    static Shot P25()
    {
        var s = new Shot("P25_Gathering", "Resource Gathering Popup", true);
        s.World = World.Ground(144, 256, 27, new[] { new World.Blob(72, 140, 60, 60) });
        World.Tree(0, 60, 'A'); World.Tree(146, 70, 'B'); World.Rock(54, 120, true); World.Rock(66, 126, false); World.Tree(140, 250, 'C');
        World.Hero("Aren", 76, 150, 1, 0); World.Render(s.World);
        HudTopLeft(s, 0, 2, 400, 400, 0.2, "AREN"); HudTopRight(s, false); Coins(s, 280, 34, 128);
        s.Panel("UI_Panel_HUD.png", 98, 54, 92, 14); s.T(144, 57, "RESOURCE", P.TextBlue, 1, 1);
        s.Panel("UI_Panel_Popup.png", 60, 196, 168, 98);
        s.Icon("Mining", 70, 206); s.T(92, 210, "ORE VEIN", P.GoldL);
        s.T(70, 230, "MINING...", P.TextW);
        s.Bar(70, 242, 148, "Progress", 0.6);
        s.Slot(70, 262, "Item_Ore", 2); s.T(98, 270, "+2 ORE", P.GreenL);
        s.T(218, 270, "2/3", P.TextMute, 1, 2);
        s.Panel("UI_Panel_Tooltip.png", 66, 302, 156, 18); s.T(144, 307, "STAND ON THE NODE", P.TextMute, 1, 1);
        Joystick(s, 12, 432);
        InteractBtn(s, 230, 448, "Mining", "MINE");
        return s;
    }
    static Shot P26()
    {
        var s = new Shot("P26_Farming", "Farming Interface", true);
        s.World = World.Ground(144, 256, 28, new[] { new World.Blob(72, 140, 60, 60) });
        World.Tree(0, 60, 'B'); World.Tree(146, 70, 'A'); World.Hero("Aren", 72, 176, 1, 0); World.Render(s.World);
        HudTopLeft(s, 0, 2, 400, 400, 0.2, "AREN"); HudTopRight(s, false); Coins(s, 280, 34, 128);
        s.Panel("UI_Panel_HUD.png", 98, 54, 92, 14); s.T(144, 57, "FARM", P.TextBlue, 1, 1);
        s.Panel("UI_Panel_Main.png", 16, 210, 256, 214);
        s.Ribbon(144, 202, "HERO FARM");
        string[] st = { "GROWING", "EMPTY", "READY" };
        for (int i = 0; i < 3; i++)
        {
            int x = 30 + i * 80, y = 228;
            s.Pic(Kit.Get(i == 2 ? "UI_Equip_Slot_Selected.png" : "UI_Equip_Slot.png"), x + 15, y);
            if (i != 1) s.Icon("Farming", x + 20, y + 5);
            s.T(x + 28, y + 30, "PLOT " + (i + 1), P.TextMute, 1, 1);
            s.T(x + 28, y + 42, st[i], i == 2 ? P.GreenL : P.TextW, 1, 1);
            if (i == 0) s.Bar(x + 6, y + 54, 44, "EXP", 0.45, true);
        }
        s.T(28, 300, "SEEDS", P.GoldL);
        s.Slot(28, 312, "Item_Sprout", 2, "Selected"); s.T(56, 319, "SPROUT  8s", P.TextW);
        s.Slot(150, 312, "Item_Herb", 0, "Disabled"); s.T(178, 319, "FIBER", P.TextMute);
        s.ConceptTag(150, 338, "DATA ONLY");
        s.Btn(28, 360, 112, 26, "Positive", "Normal", "PLANT", "Farming");
        s.Btn(148, 360, 112, 26, "Primary", "Normal", "HARVEST", "Confirm");
        Joystick(s, 12, 432);
        return s;
    }
    static Shot P27()
    {
        var s = new Shot("P27_Training_Info", "Training Information", true);
        YardWorld(s, 29);
        s.Dim(160);
        s.Panel("UI_Panel_Confirm.png", 20, 120, 248, 270);
        s.T(144, 128, "TRAINING YARD", P.GoldW, 1, 1);
        s.Icon("Training", 32, 150); s.T(54, 154, "Fight groups for EXP.", P.TextW);
        string[] k = { "GROUPS", "ALIVE CAP", "EXP PER KILL", "RESPAWN", "DUMMY" }; string[] v = { "GOBLIN / ARCHER", "3", "+8", "6s", "LAST HIT" };
        for (int i = 0; i < 5; i++) { s.T(34, 178 + i * 18, k[i], P.TextMute); s.T(256, 178 + i * 18, v[i], i == 2 ? P.GreenL : P.TextW, 1, 2); }
        s.Pic(Img.Cell("Assets/Art/Rebuild/R2C/Monsters/Goblin_Goblin_Sheet.png", 48, 48, 0, 0), 60, 262, 1);
        s.Pic(Img.Cell("Assets/Art/Rebuild/R2C/Monsters/GoblinArcher_GoblinArcher_Sheet.png", 48, 48, 0, 0), 180, 262, 1);
        s.T(84, 306, "GOBLIN", P.TextW, 1, 1); s.T(204, 306, "ARCHER", P.TextW, 1, 1);
        s.Btn(34, 340, 104, 26, "Secondary", "Normal", "VILLAGE", "Back");
        s.Btn(150, 340, 104, 26, "Positive", "Normal", "TRAIN", "Attack");
        return s;
    }
    static Shot P28()
    {
        var s = new Shot("P28_Boss_Entrance", "Boss Entrance", true);
        ArenaWorld(s, 30);
        s.Dim(170);
        s.Panel("UI_Panel_Confirm.png", 20, 96, 248, 320);
        s.T(144, 104, "CYCLOPS ARENA", P.GoldW, 1, 1);
        s.Panel("UI_Panel_Inset.png", 34, 128, 220, 112);
        s.Pic(Img.Cell("Assets/Art/Rebuild/R2C/Monsters/Cyclops/Cyclops_Sheet.png", 96, 96, 0, 0), 96, 132, 1);
        s.TO(144, 226, "CYCLOPS Lv.8", P.RedL, 1, 1);
        string[] k = { "BOSS HP", "SLAM", "REWARD" }; string[] v = { "450", "RED RING 1.2s", "+25 EXP" };
        for (int i = 0; i < 3; i++) { s.T(38, 252 + i * 18, k[i], P.TextMute); s.T(250, 252 + i * 18, v[i], i == 2 ? P.GreenL : P.TextW, 1, 2); }
        Txt.Para(s.Ui, 38, 312, 212, "Get out of the red ring before the slam lands.", P.TextBlue, 11);
        s.Btn(34, 370, 104, 26, "Secondary", "Normal", "BACK", "Back");
        s.Btn(150, 370, 104, 26, "Danger", "Normal", "ENTER", "Boss");
        return s;
    }
    static Shot P29()
    {
        var s = new Shot("P29_Treasure_Reward", "Treasure Reward", true);
        ForestWorld(s, 31, true);
        s.Dim(150);
        s.Panel("UI_Panel_Reward.png", 34, 150, 220, 210);
        s.Ribbon(144, 142, "TREASURE FOUND", "UI_Header_Ribbon_Gold.png", 170);
        s.Icon("Treasure", 120, 170, 3);
        s.Slot(66, 230, "Currency", 8); s.T(96, 237, "8 COINS", P.GoldL);
        s.Slot(66, 258, "Item_QuestToken", 1); s.T(96, 265, "QUEST TOKEN", P.TextW);
        s.T(144, 290, "This chest opens once.", P.TextMute, 1, 1);
        s.Btn(64, 314, 160, 26, "Primary", "Selected", "TAKE ALL", "Confirm");
        return s;
    }
    static Shot P30()
    {
        var s = new Shot("P30_Housing", "Village Expansion / Housing", true);
        VillageWorld(s, 32);
        s.Dim(170);
        s.Window(8, 40, 272, 420, "VILLAGE EXPANSION", true);
        s.ConceptTag(18, 60, "CONCEPT: NO PLACEMENT YET");
        string[] nm = { "BUILDING", "HOUSE", "DECOR", "SERVICE" }; string[] ic = { "Storage", "Housing", "Farming", "Shop" };
        for (int i = 0; i < 4; i++)
        {
            int x = 20 + (i % 2) * 128, y = 80 + (i / 2) * 120;
            bool sel = i == 1;
            s.Panel(sel ? "UI_Panel_Parchment.png" : "UI_Panel_Popup.png", x, y, 120, 112);
            s.Icon(ic[i], x + 44, y + 16, 2);
            s.T(x + 60, y + 58, nm[i], sel ? P.TextDark : P.GoldL, 1, 1, !sel);
            s.T(x + 60, y + 72, "SLOT " + (i + 1), sel ? P.WoodM : P.TextMute, 1, 1, false);
            if (sel) { s.Icon("Confirm", x + 52, y + 86); }
        }
        s.Panel("UI_Panel_Tooltip.png", 20, 326, 248, 40);
        s.T(28, 334, "HOUSE", P.GoldL); s.T(260, 334, "SELECTED", P.GreenL, 1, 2);
        s.T(28, 348, "Saved as slot_house.", P.TextMute);
        s.Btn(20, 380, 120, 26, "Secondary", "Normal", "BACK", "Back");
        s.Btn(148, 380, 120, 26, "Primary", "Normal", "SELECT", "Confirm");
        return s;
    }

    // ============================================================= LANDSCAPE (PC) SCREENS
    public static List<Shot> Landscape()
    {
        var l = new List<Shot> { L01(), L02(), L03(), L04(), L05(), L06(), L07(), L08() }; l.AddRange(Landscape2()); return l;
    }
    static void LTopLeft(Shot s, int hp, int lvl, double exp)
    {
        HudTopLeft(s, 0, lvl, hp, 400, exp, "AREN");
    }
    static void LTopRight(Shot s, bool combat)
    {
        int x = s.UW - 8 - 22;
        string[] ic = { combat ? "Pause" : "Settings", "WorldMap", "Inventory" }; string[] k = { "ESC", "", "I" };
        for (int i = 0; i < 3; i++)
        {
            s.IconBtn(x - i * 26, 8, ic[i]);
            if (k[i] != "") { int kw = Txt.Width(k[i], 1) + 5; s.Ui.Slice(Kit.Get("UI_KeyCap.png"), x - i * 26 + 11 - kw / 2, 26, kw, 11, 3, 3, 3, 3); s.T(x - i * 26 + 11, 28, k[i], P.GoldL, 1, 1, false); }
        }
    }
    static Shot L01()
    {
        var s = new Shot("L01_Main_Menu", "Main Menu (PC)", false);
        ForestWorld(s, 40, true);
        s.Dim(130);
        Logo(s, 180, 90, 5);
        s.ConceptTag(140, 150, "LOGO SLOT");
        int x = 410, w = 200, h = 28, y = 110, g = 36;
        s.Panel("UI_Panel_Main.png", x - 16, y - 18, w + 32, g * 5 + 30);
        s.Btn(x, y, w, h, "Primary", "Selected", "PLAY / VILLAGE"); s.Pic(Kit.Get("UI_Cursor_Arrow.png"), x - 12, y + 9);
        s.Btn(x, y + g, w, h, "Secondary", "Hover", "CONTINUE");
        s.Btn(x, y + g * 2, w, h, "Secondary", "Normal", "LEVEL SELECT");
        s.Btn(x, y + g * 3, w, h, "Secondary", "Normal", "SETTINGS", "Settings");
        s.Btn(x, y + g * 4, w, h, "Danger", "Normal", "EXIT");
        s.T(8, 346, "v1.0", P.TextMute);
        s.T(632, 346, "ENTER SELECT   ESC BACK", P.TextMute, 1, 2);
        return s;
    }
    static Shot L02()
    {
        var s = new Shot("L02_Village_HUD", "Village HUD (PC keyboard)", false);
        VillageWorld(s, 41);
        LTopLeft(s, 400, 2, 0.16); LTopRight(s, false);
        Coins(s, 632, 40, 128);
        QuestTracker(s, 8, 56, 170, "THE ROAD", "Bring ore", "0/1");
        s.Panel("UI_Panel_HUD.png", 280, 8, 80, 14); s.T(320, 11, "VILLAGE", P.TextBlue, 1, 1);
        s.MobLabel(200, 140, 18, "Mira", 1, P.GoldL); s.MobLabel(290, 140, 18, "Bram", 1, P.GoldL); s.MobLabel(180, 190, 18, "Lark", 1, P.GoldL); s.MobLabel(300, 190, 18, "Nell", 1, P.GoldL);
        InteractPrompt(s, 200, 140, "E", "SHOP");
        s.Panel("UI_Panel_HUD.png", 8, 334, 300, 18);
        s.T(16, 339, "WASD MOVE  E TALK  I BAG  ESC MENU", P.TextMute);
        return s;
    }
    static Shot L03()
    {
        var s = new Shot("L03_Dungeon_HUD", "Dungeon HUD (PC keyboard)", false);
        CampWorld(s, 42, true);
        s.MobLabel(200, 140, 14, "Goblin Lv.3", 0.5); s.MobLabel(284, 130, 14, "Goblin Lv.3", 0.8); s.MobLabel(222, 198, 14, "Goblin Lv.3");
        s.MobLabel(318, 190, 16, "Goblin Archer Lv.3", 0.3); s.MobLabel(150, 176, 14, "Goblin Lv.3");
        Damage(s, 290, 172, "18", P.TextW); Damage(s, 352, 150, "26", P.GoldL);
        LTopLeft(s, 320, 2, 0.4); LTopRight(s, true);
        Objective(s, 320, 8, "WAVE 2/4", "CLEAR WAVES");
        Combo(s, 632, 120, 12);
        PcHotbar(s, 0, new[] { 0, 0.55, 0, 0, 0.8 }, 3);
        return s;
    }
    static Shot L04()
    {
        var s = new Shot("L04_Boss_HUD", "Boss HUD (PC keyboard)", false);
        ArenaWorld(s, 43);
        LTopLeft(s, 210, 3, 0.6); LTopRight(s, true);
        s.TO(320, 50, "CYCLOPS Lv.8", P.TextW, 1, 1);
        s.BossBar(160, 62, 320, 0.64);
        Damage(s, 360, 190, "42", P.GoldL);
        Combo(s, 632, 120, 7);
        PcHotbar(s, 0, new[] { 0.4, 0.7, 0, 0, 0 }, 4);
        return s;
    }
    static Shot L05()
    {
        var s = new Shot("L05_Inventory", "Inventory (PC)", false);
        VillageWorld(s, 44);
        s.Dim(170);
        s.Window(40, 40, 560, 290, "BAG", true);
        string[] tabs = { "ALL", "RESOURCE", "GEAR", "QUEST" }; int tx = 54;
        for (int i = 0; i < 4; i++) { int w = Txt.Width(tabs[i], 1) + 14; var a = Kit.Asset(i == 0 ? "UI_Tab_Selected.png" : "UI_Tab_Normal.png"); s.Ui.Slice(a.Img, tx, 58, w, 16, a.L, a.T, a.R, a.B); s.T(tx + w / 2, 64, tabs[i], i == 0 ? P.GoldL : P.TextMute, 1, 1, i == 0); tx += w + 2; }
        s.Panel("UI_Panel_Inset.png", 52, 74, 252, 202);
        string[] items = { "Item_Ore", "Item_Wood", "Item_Herb", "Item_Sprout", "Item_Potion", "Item_TrainingBlade", "Item_QuestToken" }; int[] qty = { 3, 2, 1, 2, 1, 1, 1 };
        for (int i = 0; i < 40; i++) { int x = 58 + (i % 10) * 24, y = 80 + (i / 10) * 24; if (i < 7) s.Slot(x, y, items[i], qty[i], i == 0 ? "Selected" : (i == 1 ? "Hover" : "")); else s.Slot(x, y, null, 0, i >= 25 ? "Disabled" : ""); }
        Coins(s, 302, 282, 128);
        s.Panel("UI_Panel_Parchment.png", 314, 74, 274, 236);
        s.Pic(Kit.Get("UI_Equip_Slot_Selected.png"), 324, 84); s.Icon("Item_Ore", 329, 89);
        s.T(358, 86, "ORE", P.TextDark, 2, 0, false); s.T(358, 104, "RESOURCE", P.WoodM, 1, 0, false);
        s.Ui.HLine(324, 118, 254, P.ParchD);
        Txt.Para(s.Ui, 324, 126, 254, "Mined at the ore vein. Bram forges it into a blade. Mira buys it.", P.TextDark, 11);
        string[] k = { "QTY", "STACK", "SELL" }; string[] v = { "3", "99", "2 COINS" };
        for (int i = 0; i < 3; i++) { s.T(324, 176 + i * 14, k[i], P.WoodM, 1, 0, false); s.T(578, 176 + i * 14, v[i], P.TextDark, 1, 2, false); }
        s.Btn(324, 270, 124, 26, "Secondary", "Disabled", "USE"); s.Btn(456, 270, 122, 26, "Primary", "Disabled", "EQUIP");
        s.T(320, 338, "I CLOSE   MOUSE SELECT", P.TextMute, 1, 1);
        return s;
    }
    static Shot L06()
    {
        var s = new Shot("L06_NPC_Dialogue", "NPC Dialogue (PC)", false);
        VillageWorld(s, 45);
        LTopLeft(s, 400, 2, 0.16);
        s.MobLabel(200, 140, 18, "Mira", 1, P.GoldL);
        Dialogue(s, "MIRA", "SHOP", "I buy ore and sell potions.", "Shop", new[] { "OPEN SHOP", "GOODBYE" }, 120, 196, 400);
        return s;
    }
    static Shot L07()
    {
        var s = new Shot("L07_Dungeon_Select", "Dungeon Selection (PC)", false);
        VillageWorld(s, 46);
        s.Dim(170);
        s.Window(80, 30, 480, 300, "DUNGEONS", true);
        int[] best = { 1174, 832, 1037, 953 };
        for (int i = 0; i < 5; i++)
        {
            int y = 50 + i * 44; bool locked = i == 4, sel = i == 1;
            var a = Kit.Asset(locked ? "UI_ListRow_Disabled.png" : (sel ? "UI_ListRow_Selected.png" : "UI_ListRow_Normal.png"));
            s.Ui.Slice(a.Img, 92, y, 270, 40, a.L, a.T, a.R, a.B);
            s.Icon(locked ? "Lock" : (i == 3 ? "Boss" : "Dungeon"), 98, y + 12);
            s.T(120, y + 8, (i + 1) + ". " + Levels[i], locked ? P.TextMute : P.GoldL);
            if (locked) { s.T(120, y + 22, "COMING SOON", P.TextMute); continue; }
            s.T(120, y + 22, "BEST " + best[i], P.TextW);
            for (int k = 0; k < 3; k++) s.Icon("Star", 300 + k * 18, y + 14);
        }
        s.Panel("UI_Panel_Parchment.png", 372, 50, 176, 216);
        s.T(380, 60, "FOREST TRAIL", P.TextDark, 1, 0, false);
        s.Ui.HLine(380, 74, 160, P.ParchD);
        string[] k2 = { "WAVES", "OBJECTIVE", "ENEMIES" }; string[] v2 = { "4", "EXIT", "GOBLIN, ARCHER" };
        for (int i = 0; i < 3; i++) { s.T(380, 82 + i * 26, k2[i], P.WoodM, 1, 0, false); s.T(380, 94 + i * 26, v2[i], P.TextDark, 1, 0, false); }
        s.T(380, 164, "+ GOBRIDER", P.TextDark, 1, 0, false);
        s.T(380, 186, "BEST TIME 21.49", P.WoodM, 1, 0, false);
        s.Btn(380, 228, 160, 28, "Primary", "Selected", "START", "Dungeon");
        s.Btn(92, 280, 120, 26, "Secondary", "Normal", "BACK", "Back");
        return s;
    }
    static Shot L08()
    {
        var s = new Shot("L08_World_Map", "World Map (PC)", false);
        VillageWorld(s, 47);
        s.Dim(180);
        s.Window(40, 26, 560, 312, "WORLD MAP", true);
        s.Panel("UI_Panel_Parchment.png", 52, 44, 340, 282);
        string[] zn = { "VILLAGE", "TRAINING", "RESOURCE", "FARM", "CYCLOPS", "DUNGEONS", "EXPLORE", "TREASURE", "WARD", "RIDGE 2", "HOUSING", "???" };
        string[] ic = { "Housing", "Training", "Mining", "Farming", "Boss", "Dungeon", "WorldMap", "Treasure", "Skill", "Boss", "Housing", "Lock" };
        int[][] p = { new[] { 222, 225 }, new[] { 112, 225 }, new[] { 332, 225 }, new[] { 130, 285 }, new[] { 314, 285 }, new[] { 222, 120 }, new[] { 112, 120 }, new[] { 332, 120 }, new[] { 112, 175 }, new[] { 332, 175 }, new[] { 222, 285 }, new[] { 222, 70 } };
        for (int i = 1; i < p.Length; i++) { int x0 = p[0][0], y0 = p[0][1], x1 = p[i][0], y1 = p[i][1]; int n = Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0)); for (int k = 0; k <= n; k += 4) s.Ui.Fill(x0 + (x1 - x0) * k / n, y0 + (y1 - y0) * k / n, 2, 2, P.ParchD); }
        for (int i = 0; i < p.Length; i++)
        {
            bool locked = i == 11, here = i == 0;
            s.Pic(Kit.Get(here ? "UI_Inventory_Slot_Selected.png" : (locked ? "UI_Inventory_Slot_Disabled.png" : "UI_Inventory_Slot.png")), p[i][0] - 11, p[i][1] - 11);
            s.Icon(ic[i], p[i][0] - 8, p[i][1] - 8);
            s.TO(p[i][0], p[i][1] + 13, zn[i], here ? P.GoldL : (locked ? P.TextMute : P.TextW), 1, 1);
        }
        s.ConceptTag(58, 50, "DRAWN MAP = CONCEPT");
        s.Panel("UI_Panel_Inset.png", 400, 44, 188, 282);
        for (int i = 0; i < 6; i++)
        {
            int y = 50 + i * 34; var a = Kit.Asset(i == 4 ? "UI_ListRow_Selected.png" : "UI_ListRow_Normal.png"); s.Ui.Slice(a.Img, 404, y, 180, 30, a.L, a.T, a.R, a.B);
            s.Icon(ic[i], 410, y + 7); s.T(432, y + 11, zn[i], i == 4 ? P.GoldL : P.TextW);
        }
        s.Btn(404, 262, 180, 26, "Primary", "Normal", "TRAVEL", "WorldMap");
        s.Btn(404, 294, 180, 24, "Secondary", "Normal", "PAGE 2");
        return s;
    }
}
