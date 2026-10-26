using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

/// <summary>Landscape (PC, 640x360 UI) versions of the remaining screens, L09-L30.
/// Small popups reuse the approved portrait panel art (lifted without its dim); big windows get a two-column layout.</summary>
public static partial class Screens
{
    static List<Shot> Landscape2()
    {
        return new List<Shot> { L09(), L10(), L11(), L12(), L13(), L14(), L15(), L16(), L17(), L18(), L19(), L20(),
            L21(), L22(), L23(), L24(), L25(), L26(), L27(), L28(), L29(), L30() };
    }

    /// <summary>Copy a rectangle of a portrait screen's UI layer (rendered without dim) onto a landscape shot.</summary>
    static void Lift(Func<Shot> portrait, Rectangle r, Shot to, int x, int y)
    {
        Shot.NoDim = true;
        Shot p;
        try { p = portrait(); } finally { Shot.NoDim = false; }
        to.Ui.Blit(p.Ui.Crop(r.X, r.Y, r.Width, r.Height).B, x, y);
    }
    static int CX(Shot s, int w) { return (s.UW - w) / 2; }

    static void FieldL(Shot s, int seed, bool hero, int heroRow = 0)
    {
        s.World = World.Ground(480, 270, seed, new[] { new World.Blob(240, 150, 150, 80) });
        World.Tree(20, 70, 'A'); World.Tree(460, 80, 'B'); World.Tree(30, 260, 'C'); World.Tree(450, 266, 'A'); World.Tree(110, 40, 'B'); World.Tree(380, 40, 'C');
        if (hero) World.Hero("Aren", 240, 160, heroRow, 0);
    }
    static void TabsAt(Shot s, int x, int y, string[] tabs, int sel)
    {
        for (int i = 0; i < tabs.Length; i++)
        {
            int w = Txt.Width(tabs[i], 1) + 14;
            var a = Kit.Asset(i == sel ? "UI_Tab_Selected.png" : "UI_Tab_Normal.png");
            s.Ui.Slice(a.Img, x, y, w, 16, a.L, a.T, a.R, a.B);
            s.T(x + w / 2, y + 6, tabs[i], i == sel ? P.GoldL : P.TextMute, 1, 1, i == sel);
            x += w + 2;
        }
    }
    static void Row(Shot s, int x, int y, int w, int h, string state)
    {
        var a = Kit.Asset(state == "sel" ? "UI_ListRow_Selected.png" : (state == "off" ? "UI_ListRow_Disabled.png" : "UI_ListRow_Normal.png"));
        s.Ui.Slice(a.Img, x, y, w, h, a.L, a.T, a.R, a.B);
    }
    static void Hint(Shot s, string text) { s.T(s.UW / 2, s.UH - 14, text, P.TextMute, 1, 1); }

    // ------------------------------------------------------------- L09 splash
    static Shot L09()
    {
        var s = new Shot("L09_Splash_Loading", "Splash / Loading (PC)", false);
        ForestWorld(s, 50, false);
        s.Dim(170);
        Logo(s, 320, 90, 5);
        s.ConceptTag(280, 150, "LOGO SLOT");
        s.Pic(World.HeroFrame("Aren", 0, 0), 296, 170, 2);
        s.T(320, 248, "LOADING", P.TextBlue, 1, 1);
        s.Bar(170, 262, 300, "Progress", 0.62);
        s.Panel("UI_Panel_Tooltip.png", 170, 290, 300, 28);
        s.T(178, 297, "TIP", P.GoldL); s.T(210, 297, "Hold R to keep attacking.", P.TextW);
        s.T(632, 346, "v1.0", P.TextMute, 1, 2);
        return s;
    }
    // ------------------------------------------------------------- L10 class select
    static Shot L10()
    {
        var s = new Shot("L10_Class_Select", "Class Selection (PC)", false);
        ForestWorld(s, 51, false);
        s.Dim(160);
        int c = 0;
        s.Window(30, 22, 580, 318, "CHOOSE YOUR HERO", true);
        s.Panel("UI_Panel_Inset.png", 44, 40, 150, 170);
        s.Ui.Fill(70, 186, 98, 6, P.ForestM); s.Ui.HLine(72, 192, 94, P.ForestD);
        s.Pic(World.HeroFrame(Classes[c], 0, 0), 55, 66, 4);
        s.T(206, 44, Classes[c].ToUpper(), P.GoldL, 2);
        s.T(206, 64, Roles[c], P.TextBlue);
        s.T(206, 80, Blurb[c], P.TextW);
        string[] st = { "HP", "SPD", "DEF" };
        double[] v = { Stats[c, 0] / 1.5, Stats[c, 1] / 1.15, 0.25 + Stats[c, 2] * 4 };
        string[] vt = { "x1.0", "x1.0", "0%" };
        for (int i = 0; i < 3; i++) { s.T(206, 98 + i * 13, st[i], P.TextMute); s.Bar(240, 99 + i * 13, 120, "Progress", Math.Min(1, v[i]), true); s.T(410, 98 + i * 13, vt[i], P.TextW, 1, 2); }
        s.T(206, 142, "SKILLS", P.GoldL);
        for (int i = 0; i < 6; i++) s.Skill(206 + i * 44, 154, SkillIcon(c, i), i == 1 ? "Selected" : "Default", 0, null, Keys[i]);
        s.Panel("UI_Panel_Tooltip.png", 206, 204, 390, 34);
        s.T(214, 210, SkillNames[c][1], P.GoldL); s.T(588, 210, "CD 5s", P.TextBlue, 1, 2);
        s.T(214, 223, "Dash 2 tiles with the shield. Stuns 0.6s.", P.TextW);
        s.T(44, 220, "CLASS", P.GoldL);
        for (int i = 0; i < 5; i++)
        {
            int x = 46 + i * 54, y = 234;
            s.PortraitAt(Face(Classes[i]), x, y);
            if (i == c) { s.Ui.Box(x - 2, y - 2, 42, 42, P.GoldL); s.Ui.Box(x - 1, y - 1, 40, 40, P.GoldW); }
            s.T(x + 19, y + 42, Classes[i].ToUpper(), i == c ? P.GoldL : P.TextMute, 1, 1);
        }
        s.Btn(330, 292, 126, 26, "Secondary", "Normal", "BACK", "Back");
        s.Btn(464, 292, 132, 26, "Primary", "Selected", "CONFIRM");
        s.T(330, 262, "F1-F5 SWITCH CLASS", P.TextMute);
        return s;
    }
    // ------------------------------------------------------------- L11 pause (lifted)
    static Shot L11()
    {
        var s = new Shot("L11_Pause", "Pause Menu (PC)", false);
        CampWorld(s, 52, true);
        LTopLeft(s, 320, 2, 0.4);
        s.Dim(170);
        Lift(P05, new Rectangle(20, 128, 248, 254), s, CX(s, 248), 50);
        Hint(s, "ESC RESUME   ENTER SELECT");
        return s;
    }
    // ------------------------------------------------------------- L12 settings
    static Shot L12()
    {
        var s = new Shot("L12_Settings", "Settings (PC)", false);
        ForestWorld(s, 53, true);
        s.Dim(170);
        s.Window(110, 24, 420, 312, "SETTINGS", true);
        TabsAt(s, 126, 42, new[] { "AUDIO", "CONTROLS", "DISPLAY" }, 0);
        string[] rows = { "MUSIC", "SOUND FX" }; double[] vals = { 0.7, 0.85 };
        for (int i = 0; i < 2; i++)
        {
            int y = 78 + i * 32;
            s.T(130, y, rows[i], P.TextW);
            var t = Kit.Asset("UI_Slider_Track.png"); s.Ui.Slice(t.Img, 250, y - 1, 220, 6, t.L, t.T, t.R, t.B);
            var f = Kit.Get("UI_Slider_Fill.png"); for (int k = 0; k < (int)(216 * vals[i]); k++) s.Ui.Blit(f, 252 + k, y + 1);
            s.Pic(Kit.Get("UI_Slider_Knob.png"), 250 + (int)(216 * vals[i]) - 3, y - 4);
            s.T(512, y, ((int)(vals[i] * 100)).ToString(), P.GoldL, 1, 2);
        }
        for (int k = 0; k < 384; k += 8) s.Pic(Kit.Get("UI_Divider_Line.png"), 128 + k, 146);
        s.T(130, 162, "INTERACT KEY", P.TextW);
        string[] keys = { "E", "F", "G" };
        for (int i = 0; i < 3; i++) s.Btn(330 + i * 40, 156, 34, 20, i == 0 ? "Primary" : "Secondary", i == 0 ? "Selected" : "Normal", keys[i]);
        s.T(130, 194, "LAYOUT", P.TextW);
        s.Btn(330, 188, 86, 20, "Secondary", "Normal", "PORTRAIT"); s.Btn(420, 188, 70, 20, "Primary", "Selected", "WIDE");
        s.T(130, 224, "KEYS", P.TextW); s.T(330, 224, "WASD  R/SPACE  1-5", P.TextMute);
        s.Btn(130, 292, 140, 26, "Secondary", "Normal", "DEFAULTS");
        s.Btn(370, 292, 140, 26, "Primary", "Normal", "SAVE");
        return s;
    }
    // ------------------------------------------------------------- L13 training HUD
    static Shot L13()
    {
        var s = new Shot("L13_Training_HUD", "Training Ground HUD (PC)", false);
        FieldL(s, 54, false);
        World.Crates(80, 120); World.Hero("Aren", 240, 156, 6, 0);
        World.Monster("Goblin", 200, 130, 3, 0); World.Monster("GoblinArcher", 290, 118, 0, 0); World.Monster("Goblin", 280, 176, 6, 1);
        World.Render(s.World);
        s.MobLabel(200, 130, 14, "Goblin Lv.3", 0.6); s.MobLabel(290, 118, 16, "Goblin Archer Lv.3"); s.MobLabel(280, 176, 14, "Goblin Lv.3", 0.25);
        LTopLeft(s, 380, 2, 0.72); LTopRight(s, true);
        s.Ribbon(320, 8, "TRAINING YARD");
        s.Panel("UI_Panel_HUD.png", 254, 28, 132, 26); s.Icon("Training", 258, 33); s.T(278, 32, "GOBLINS 3/3", P.TextW); s.T(278, 42, "+8 EXP EACH", P.GreenL);
        s.Panel("UI_Panel_HUD.png", 8, 56, 92, 26); s.T(14, 61, "LAST HIT", P.TextMute); s.T(14, 71, "26", P.GoldL);
        Damage(s, 380, 170, "26", P.GoldL);
        Combo(s, 632, 120, 5);
        PcHotbar(s, 0, new[] { 0.3, 0, 0, 0, 0 });
        return s;
    }
    // ------------------------------------------------------------- L14-L16 results (lifted)
    static Shot L14()
    {
        var s = new Shot("L14_Level_Complete", "Level Complete (PC)", false);
        CampWorld(s, 55, false);
        s.Dim(170);
        Lift(P11, new Rectangle(16, 88, 256, 316), s, CX(s, 256), 22);
        return s;
    }
    static Shot L15()
    {
        var s = new Shot("L15_Game_Over", "Game Over / Retry (PC)", false);
        CampWorld(s, 56, false);
        s.Tint(P.A(P.RedD, 90)); s.Dim(140);
        Lift(P12, new Rectangle(28, 138, 232, 236), s, CX(s, 232), 56);
        Hint(s, "ENTER RETRY   ESC MENU");
        return s;
    }
    static Shot L16()
    {
        var s = new Shot("L16_Reward_Popup", "Reward Popup (PC)", false);
        VillageWorld(s, 57);
        LTopLeft(s, 400, 2, 0.36);
        s.Dim(150);
        Lift(P13, new Rectangle(30, 148, 228, 206), s, CX(s, 228), 70);
        return s;
    }
    // ------------------------------------------------------------- L17 equipment
    static Shot L17()
    {
        var s = new Shot("L17_Equipment", "Equipment (PC)", false);
        VillageWorld(s, 58);
        s.Dim(170);
        s.Window(50, 24, 540, 312, "EQUIPMENT", true);
        s.Panel("UI_Panel_Inset.png", 120, 46, 130, 170);
        s.Ui.Fill(146, 194, 78, 6, P.ForestM);
        s.Pic(World.HeroFrame("Aren", 0, 0), 137, 92, 3);
        string[] slots = { "Equip_Helmet", "Equip_Armor", "Equip_Boots", "Equip_Weapon", "Equip_Shield", "Equip_Ring" };
        string[] names = { "HEAD", "BODY", "FEET", "WEAPON", "OFF-HAND", "TRINKET" };
        for (int i = 0; i < 6; i++)
        {
            bool left = i < 3; int x = left ? 72 : 272, y = 50 + (i % 3) * 56;
            bool weapon = i == 3;
            s.Pic(Kit.Get(weapon ? "UI_Equip_Slot_Selected.png" : "UI_Equip_Slot_Locked.png"), x, y);
            if (weapon) s.Icon("Item_TrainingBlade", x + 5, y + 5); else s.Icon(slots[i] + "_Ghost", x + 5, y + 5);
            s.T(x + 13, y + 30, names[i], weapon ? P.GoldL : P.TextMute, 1, 1);
        }
        s.ConceptTag(66, 226, "ONLY WEAPON SLOT EXISTS TODAY");
        s.Panel("UI_Panel_Parchment.png", 330, 46, 246, 230);
        s.T(340, 56, "TRAINING BLADE", P.TextDark, 1, 0, false); s.T(566, 56, "EQUIPPED", P.ForestM, 1, 2, false);
        s.Ui.HLine(340, 70, 226, P.ParchD);
        string[] k = { "CLASS", "HP", "SPEED", "DAMAGE TAKEN", "WEAPON BONUS" }; string[] v = { "KNIGHT", "400", "x1.0", "100%", "NONE" };
        for (int i = 0; i < 5; i++) { s.T(340, 80 + i * 16, k[i], P.WoodM, 1, 0, false); s.T(566, 80 + i * 16, v[i], P.TextDark, 1, 2, false); }
        Txt.Para(s.Ui, 340, 168, 226, "Made by Bram from 1 ore and 1 wood. The slot is saved; combat numbers do not change yet.", P.TextDark, 11);
        s.Btn(340, 238, 108, 26, "Secondary", "Normal", "UNEQUIP");
        s.Btn(456, 238, 110, 26, "Primary", "Normal", "BAG", "Inventory");
        Hint(s, "I BAG   ESC CLOSE");
        return s;
    }
    // ------------------------------------------------------------- L18 hero info
    static Shot L18()
    {
        var s = new Shot("L18_Hero_Info", "Hero Information (PC)", false);
        VillageWorld(s, 59);
        s.Dim(170);
        s.Window(40, 24, 560, 312, "HERO", true);
        s.PortraitAt(Face("Aren"), 54, 44);
        s.Pic(Kit.Get("UI_LevelBadge.png"), 80, 70); s.T(88, 74, "2", P.GoldL, 1, 1, false);
        s.T(102, 46, "AREN", P.GoldL, 2); s.T(102, 66, "KNIGHT", P.TextBlue);
        s.T(54, 92, "EXP", P.TextMute); s.Bar(90, 90, 210, "EXP", 0.16, true); s.T(300, 100, "58/100", P.TextW, 1, 2);
        s.T(54, 116, "HP", P.TextMute); s.Bar(90, 114, 210, "HP", 1, false, "400/400");
        s.Panel("UI_Panel_Inset.png", 50, 136, 254, 92);
        string[] k = { "SPEED", "DAMAGE TAKEN", "BASIC ATTACK", "COINS", "DUNGEON STARS" }; string[] v = { "x1.0", "100%", "MELEE 3-HIT", "128", "12/12" };
        for (int i = 0; i < 5; i++) { s.T(58, 144 + i * 16, k[i], P.TextMute); s.T(296, 144 + i * 16, v[i], P.TextW, 1, 2); }
        s.T(320, 44, "SKILLS", P.GoldL);
        for (int i = 0; i < 6; i++)
        {
            int y = 56 + i * 36;
            Row(s, 316, y, 270, 34, i == 1 ? "sel" : "");
            s.Pic(SkillIcon(0, i), 318, y + 1);
            s.Ui.Blit(Kit.Get("UI_KeyCap.png"), 352, y + 12); s.T(357, y + 14, Keys[i], P.GoldL, 1, 1, false);
            s.T(370, y + 14, SkillNames[0][i], P.TextW);
            s.T(578, y + 14, i == 0 ? "0.35s" : Cooldowns[0][i] + "s", P.TextBlue, 1, 2);
        }
        Hint(s, "ESC CLOSE");
        return s;
    }
    // ------------------------------------------------------------- L19 quests
    static Shot L19()
    {
        var s = new Shot("L19_Quest_Journal", "Quest Journal (PC)", false);
        VillageWorld(s, 60);
        s.Dim(170);
        s.Window(40, 24, 560, 312, "QUESTS", true);
        TabsAt(s, 52, 40, new[] { "ACTIVE", "DONE" }, 0);
        string[] q = { "Clear the yard", "Bring ore", "Harvest a sprout", "Forge a blade", "Clear depth 1", "Defeat Cyclops" };
        for (int i = 0; i < 6; i++)
        {
            int y = 58 + i * 26; bool done = i == 0, act = i == 1, locked = i > 1;
            Row(s, 50, y, 252, 24, act ? "sel" : (locked ? "off" : ""));
            s.Icon(done ? "Confirm" : (locked ? "Lock" : "Quests"), 54, y + 4);
            s.T(74, y + 8, (i + 1) + ". " + q[i], locked ? P.TextMute : (done ? P.GreenL : P.GoldL));
            s.T(294, y + 8, done ? "DONE" : (act ? "0/1" : ""), done ? P.GreenL : P.TextW, 1, 2);
        }
        s.T(56, 222, "Steps unlock one by one.", P.TextMute);
        s.Panel("UI_Panel_Parchment.png", 314, 40, 272, 280);
        s.T(324, 50, "THE ROAD - STEP 2", P.WoodM, 1, 0, false);
        s.T(324, 64, "BRING ORE", P.TextDark, 2, 0, false);
        s.Ui.HLine(324, 84, 252, P.ParchD);
        s.T(324, 92, "OBJECTIVE", P.WoodM, 1, 0, false);
        s.Icon("Item_Ore", 324, 104); s.T(344, 109, "Gather ore", P.TextDark, 1, 0, false); s.T(574, 109, "0/1", P.RedD, 1, 2, false);
        s.Bar(324, 126, 252, "Progress", 0.0, true);
        s.T(324, 142, "REWARD", P.WoodM, 1, 0, false);
        s.Slot(324, 154, "Currency"); s.T(352, 161, "5 COINS", P.TextDark, 1, 0, false);
        s.T(324, 186, "GIVER: LARK", P.WoodM, 1, 0, false);
        s.Btn(324, 280, 120, 26, "Secondary", "Normal", "TRACK");
        s.Btn(454, 280, 122, 26, "Primary", "Disabled", "CLAIM");
        return s;
    }
    // ------------------------------------------------------------- L20 shop
    static Shot L20()
    {
        var s = new Shot("L20_Shop", "Shop (PC)", false);
        VillageWorld(s, 61);
        s.Dim(170);
        s.Window(40, 24, 560, 312, "MIRA'S SHOP", true);
        TabsAt(s, 52, 40, new[] { "BUY", "SELL" }, 1);
        Coins(s, 302, 40, 128);
        string[] it = { "Item_Ore", "Item_Wood", "Item_Herb", "Item_Sprout" }; string[] nm = { "ORE", "WOOD", "HERB", "SPROUT" }; int[] have = { 3, 2, 1, 2 }; int[] price = { 2, 1, 2, 1 };
        for (int i = 0; i < 4; i++)
        {
            int y = 60 + i * 32;
            Row(s, 50, y, 254, 30, i == 0 ? "sel" : "");
            s.Slot(54, y + 4, it[i]);
            s.T(82, y + 7, nm[i], P.GoldL); s.T(82, y + 18, "HAVE " + have[i], P.TextMute);
            s.Icon("Currency", 252, y + 7); s.T(296, y + 11, price[i].ToString(), P.GoldL, 1, 2);
        }
        s.T(56, 196, "BUY TAB: POTION 5 COINS", P.TextMute);
        s.Slot(54, 210, "Item_Potion"); s.T(82, 217, "POTION", P.GoldL); s.Icon("Currency", 252, 213); s.T(296, 217, "5", P.GoldL, 1, 2);
        s.Panel("UI_Panel_Parchment.png", 314, 40, 272, 200);
        s.T(324, 50, "SELL ORE", P.TextDark, 2, 0, false);
        s.T(324, 80, "AMOUNT", P.WoodM, 1, 0, false);
        s.Btn(420, 74, 26, 20, "Secondary", "Normal", "-"); s.T(476, 80, "3", P.TextDark, 1, 1, false); s.Btn(500, 74, 26, 20, "Secondary", "Normal", "+");
        s.T(324, 110, "YOU GET", P.WoodM, 1, 0, false); s.Icon("Currency", 520, 106); s.T(574, 110, "6", P.TextDark, 1, 2, false);
        s.Btn(324, 196, 252, 28, "Primary", "Normal", "SELL", "Currency");
        Hint(s, "MOUSE SELECT   ESC CLOSE");
        return s;
    }
    // ------------------------------------------------------------- L21 blacksmith
    static Shot L21()
    {
        var s = new Shot("L21_Blacksmith", "Blacksmith / Crafting (PC)", false);
        VillageWorld(s, 62);
        s.Dim(170);
        s.Window(40, 24, 560, 312, "BRAM'S FORGE", true);
        s.T(52, 42, "RECIPES", P.GoldL);
        Row(s, 50, 54, 254, 32, "sel");
        s.Slot(54, 59, "Item_TrainingBlade"); s.T(82, 63, "TRAINING BLADE", P.GoldL); s.T(82, 74, "WEAPON", P.TextMute);
        Row(s, 50, 88, 254, 32, "off");
        s.Slot(54, 93, null, 0, "Disabled"); s.Icon("Lock", 57, 96); s.T(82, 104, "MORE RECIPES LATER", P.TextMute);
        s.Panel("UI_Panel_Parchment.png", 314, 40, 272, 280);
        s.Pic(Kit.Get("UI_Equip_Slot_Selected.png"), 437, 50); s.Icon("Item_TrainingBlade", 442, 55);
        s.T(450, 82, "TRAINING BLADE", P.TextDark, 1, 1, false);
        s.Ui.HLine(324, 96, 252, P.ParchD);
        s.T(324, 104, "NEEDS", P.WoodM, 1, 0, false);
        s.Slot(324, 118, "Item_Ore"); s.T(352, 125, "ORE", P.TextDark, 1, 0, false); s.T(574, 125, "3/1", P.ForestM, 1, 2, false);
        s.Slot(324, 144, "Item_Wood"); s.T(352, 151, "WOOD", P.TextDark, 1, 0, false); s.T(574, 151, "2/1", P.ForestM, 1, 2, false);
        s.Icon("Crafting", 324, 176); s.T(344, 180, "Result goes to the bag.", P.TextDark, 1, 0, false);
        s.Bar(324, 204, 252, "Progress", 0.0);
        s.Btn(324, 228, 252, 28, "Positive", "Normal", "CRAFT", "Crafting");
        s.Btn(324, 280, 252, 24, "Secondary", "Normal", "BACK", "Back");
        return s;
    }
    // ------------------------------------------------------------- L22 storage
    static Shot L22()
    {
        var s = new Shot("L22_Storage", "Storage (PC)", false);
        VillageWorld(s, 63);
        s.Dim(170);
        s.Window(40, 24, 560, 312, "NELL'S STORAGE", true);
        string[] it = { "Item_Ore", "Item_Wood", "Item_Herb", "Item_Sprout", "Item_Potion", "Item_TrainingBlade", "Item_QuestToken" }; int[] q = { 3, 2, 1, 2, 1, 1, 1 };
        s.T(52, 42, "STORAGE", P.GoldL); s.T(304, 42, "2 KINDS", P.TextMute, 1, 2);
        s.Panel("UI_Panel_Inset.png", 50, 54, 254, 202);
        string[] st = { "Item_Ore", "Item_Wood" }; int[] sq = { 12, 6 };
        for (int i = 0; i < 40; i++) s.Slot(56 + (i % 10) * 24, 60 + (i / 10) * 24, i < 2 ? st[i] : null, i < 2 ? sq[i] : 0, i == 0 ? "Selected" : "");
        s.T(56, 162, "ORE x12", P.GoldL);
        s.Btn(56, 222, 118, 24, "Secondary", "Normal", "TAKE 1"); s.Btn(180, 222, 118, 24, "Secondary", "Normal", "TAKE ALL");
        s.T(338, 42, "BAG", P.GoldL);
        s.Panel("UI_Panel_Inset.png", 336, 54, 254, 202);
        for (int i = 0; i < 40; i++) s.Slot(342 + (i % 10) * 24, 60 + (i / 10) * 24, i < 7 ? it[i] : null, i < 7 ? q[i] : 0);
        s.Btn(342, 222, 118, 24, "Secondary", "Normal", "STORE 1"); s.Btn(466, 222, 118, 24, "Primary", "Normal", "STORE ALL");
        s.T(320, 278, "Stack limit 99 per item", P.TextMute, 1, 1);
        return s;
    }
    // ------------------------------------------------------------- L23 skill tree
    static Shot L23()
    {
        var s = new Shot("L23_Skill_Tree", "Skill Tree concept (PC)", false);
        ForestWorld(s, 64, false);
        s.Dim(180);
        s.Window(40, 24, 560, 312, "SKILL TREE", true);
        s.ConceptTag(52, 42, "CONCEPT - NO UNLOCKS YET");
        s.Panel("UI_Panel_Inset.png", 50, 58, 350, 268);
        int[][] pos = { new[] { 206, 64 }, new[] { 136, 124 }, new[] { 276, 124 }, new[] { 80, 190 }, new[] { 170, 190 }, new[] { 242, 190 }, new[] { 330, 190 }, new[] { 206, 262 } };
        int[][] links = { new[] { 0, 1 }, new[] { 0, 2 }, new[] { 1, 3 }, new[] { 1, 4 }, new[] { 2, 5 }, new[] { 2, 6 }, new[] { 4, 7 }, new[] { 5, 7 } };
        foreach (var l in links)
        {
            int x0 = pos[l[0]][0] + 19, y0 = pos[l[0]][1] + 19, x1 = pos[l[1]][0] + 19, y1 = pos[l[1]][1] + 19;
            bool lit = l[0] == 0 && l[1] == 1; int n = Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0));
            for (int i = 0; i <= n; i++) s.Ui.Fill(x0 + (x1 - x0) * i / n - 1, y0 + (y1 - y0) * i / n - 1, 3, 3, lit ? P.GoldL : P.Stone);
        }
        for (int i = 0; i < pos.Length; i++)
        {
            s.Skill(pos[i][0], pos[i][1], SkillIcon(0, Math.Min(5, i % 6)), i == 0 ? "Selected" : (i == 1 ? "Default" : "Disabled"));
            if (i > 1) s.Icon("Lock", pos[i][0] + 11, pos[i][1] + 11);
        }
        s.Panel("UI_Panel_Tooltip.png", 410, 58, 176, 120);
        s.T(418, 66, "SHIELD BASH +", P.GoldL); s.T(578, 80, "1 PT", P.TextBlue, 1, 2);
        Txt.Para(s.Ui, 418, 96, 160, "Node row exists in WorldFoundation. Effect not designed.", P.TextMute, 11);
        s.T(418, 194, "POINTS", P.TextMute); s.T(578, 194, "0", P.GoldL, 1, 2);
        s.Btn(414, 296, 172, 26, "Primary", "Disabled", "UNLOCK");
        return s;
    }
    // ------------------------------------------------------------- L24 minimap HUD
    static Shot L24()
    {
        var s = new Shot("L24_Minimap", "Minimap Components (PC)", false);
        CampWorld(s, 65, false);
        LTopLeft(s, 400, 2, 0.16); LTopRight(s, false);
        int mx = 552, my = 40;
        s.Pic(Kit.Get("UI_Minimap_Back.png"), mx + 6, my + 6);
        var mini = new Cv(68, 68);
        for (int y = 0; y < 68; y++) for (int x = 0; x < 68; x++) { var c = s.World.Get(Math.Min(479, 80 + x * 320 / 68), Math.Min(269, 20 + y * 230 / 68)); mini.Set(x, y, P.Mix(c, P.ForestD, 0.35)); }
        s.Pic(mini.B, mx + 6, my + 6);
        s.Pic(Kit.Get("UI_Minimap_Frame.png"), mx, my);
        s.Pic(Icons.Get("Marker_Player"), mx + 37, my + 37); s.Pic(Icons.Get("Marker_Enemy"), mx + 22, my + 26); s.Pic(Icons.Get("Marker_Gate"), mx + 37, my + 9); s.Pic(Icons.Get("Marker_Quest"), mx + 14, my + 56);
        s.Panel("UI_Panel_HUD.png", mx, my + 82, 80, 14); s.T(mx + 40, my + 85, "CAMP", P.TextBlue, 1, 1);
        s.Panel("UI_Panel_Main.png", 150, 230, 340, 116);
        s.T(320, 242, "MAP MARKERS", P.GoldL, 1, 1);
        string[] mk = { "Player", "NPC", "Quest", "Boss", "Gate", "Chest", "Node", "Enemy" };
        string[] lb = { "YOU", "NPC", "QUEST", "BOSS", "GATE", "CHEST", "GATHER", "ENEMY" };
        for (int i = 0; i < 8; i++) { int x = 166 + (i % 4) * 80, y = 260 + (i / 4) * 24; s.Pic(Icons.Get("Marker_" + mk[i]), x, y, 2); s.T(x + 18, y + 3, lb[i], P.TextW); }
        s.ConceptTag(166, 312, "CONCEPT (TODAY: ZONE LABEL)");
        return s;
    }
    // ------------------------------------------------------------- L25-L26 world activities (lifted popups)
    static Shot L25()
    {
        var s = new Shot("L25_Gathering", "Resource Gathering (PC)", false);
        FieldL(s, 66, false);
        World.Rock(200, 140, true); World.Rock(214, 146, false); World.Hero("Aren", 230, 160, 1, 0);
        World.Render(s.World);
        LTopLeft(s, 400, 2, 0.2); LTopRight(s, false); Coins(s, 632, 40, 128);
        s.Panel("UI_Panel_HUD.png", 280, 8, 80, 14); s.T(320, 11, "RESOURCE", P.TextBlue, 1, 1);
        Lift(P25, new Rectangle(56, 192, 176, 132), s, 360, 120);
        InteractPrompt(s, 214, 146, "E", "MINE");
        return s;
    }
    static Shot L26()
    {
        var s = new Shot("L26_Farming", "Farming Interface (PC)", false);
        FieldL(s, 67, true, 1);
        World.Render(s.World);
        LTopLeft(s, 400, 2, 0.2); LTopRight(s, false); Coins(s, 632, 40, 128);
        s.Panel("UI_Panel_HUD.png", 280, 8, 80, 14); s.T(320, 11, "FARM", P.TextBlue, 1, 1);
        Lift(P26, new Rectangle(12, 198, 264, 196), s, CX(s, 264), 130);
        Hint(s, "E PLANT / HARVEST   ESC CLOSE");
        return s;
    }
    // ------------------------------------------------------------- L27-L29 popups (lifted)
    static Shot L27()
    {
        var s = new Shot("L27_Training_Info", "Training Information (PC)", false);
        FieldL(s, 68, true);
        World.Monster("Goblin", 200, 130, 0, 0); World.Monster("GoblinArcher", 290, 120, 0, 0);
        World.Render(s.World);
        s.Dim(160);
        Lift(P27, new Rectangle(16, 116, 256, 278), s, CX(s, 256), 40);
        return s;
    }
    static Shot L28()
    {
        var s = new Shot("L28_Boss_Entrance", "Boss Entrance (PC)", false);
        ArenaWorld(s, 69);
        s.Dim(170);
        Lift(P28, new Rectangle(16, 92, 256, 328), s, CX(s, 256), 16);
        return s;
    }
    static Shot L29()
    {
        var s = new Shot("L29_Treasure_Reward", "Treasure Reward (PC)", false);
        ForestWorld(s, 70, true);
        s.Dim(150);
        Lift(P29, new Rectangle(30, 138, 228, 226), s, CX(s, 228), 64);
        return s;
    }
    // ------------------------------------------------------------- L30 housing
    static Shot L30()
    {
        var s = new Shot("L30_Housing", "Village Expansion / Housing (PC)", false);
        VillageWorld(s, 71);
        s.Dim(170);
        s.Window(40, 24, 560, 312, "VILLAGE EXPANSION", true);
        s.ConceptTag(52, 42, "CONCEPT: NO PLACEMENT YET");
        string[] nm = { "BUILDING", "HOUSE", "DECOR", "SERVICE" }; string[] ic = { "Storage", "Housing", "Farming", "Shop" };
        for (int i = 0; i < 4; i++)
        {
            int x = 52 + i * 134, y = 62; bool sel = i == 1;
            s.Panel(sel ? "UI_Panel_Parchment.png" : "UI_Panel_Popup.png", x, y, 126, 130);
            s.Icon(ic[i], x + 47, y + 20, 2);
            s.T(x + 63, y + 66, nm[i], sel ? P.TextDark : P.GoldL, 1, 1, !sel);
            s.T(x + 63, y + 80, "SLOT " + (i + 1), sel ? P.WoodM : P.TextMute, 1, 1, false);
            if (sel) s.Icon("Confirm", x + 55, y + 100);
        }
        s.Panel("UI_Panel_Tooltip.png", 52, 206, 536, 40);
        s.T(60, 214, "HOUSE", P.GoldL); s.T(580, 214, "SELECTED", P.GreenL, 1, 2);
        s.T(60, 228, "Saved as slot_house.", P.TextMute);
        s.Btn(52, 292, 150, 26, "Secondary", "Normal", "BACK", "Back");
        s.Btn(438, 292, 150, 26, "Primary", "Normal", "SELECT", "Confirm");
        return s;
    }
}
