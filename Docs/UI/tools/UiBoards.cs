using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

/// <summary>Design-system boards, 960x540 canvas shown at x2 (1920x1080).</summary>
public class Board
{
    public Cv C = new Cv(960, 540);
    public string Name, Title;
    public Board(string name, string title, string sub)
    {
        Name = name; Title = title;
        C.Clear(P.Char0);
        // frame
        C.Fill(0, 0, 960, 34, P.WoodD); C.HLine(0, 34, 960, P.GoldD); C.HLine(0, 35, 960, P.Ink);
        Txt.Draw(C, 16, 9, title, P.GoldL, 2);
        Txt.Draw(C, 944, 14, "GAME01 RPG UI MASTER DESIGN SYSTEM", P.TextMute, 1, 2, false);
        if (sub != null) Txt.Draw(C, 16, 44, sub, P.TextBlue, 1, 0, false);
        C.HLine(0, 529, 960, P.Ink); C.Fill(0, 530, 960, 10, P.WoodD);
    }
    public void H(int x, int y, string s) { Txt.Draw(C, x, y, s, P.GoldL, 1, 0, true); C.HLine(x, y + 10, Txt.Width(s, 1), P.GoldD); }
    public void T(int x, int y, string s, Color? c = null, int align = 0) { Txt.Draw(C, x, y, s, c.HasValue ? c.Value : P.TextW, 1, align, false); }
    public void Note(int x, int y, int w, string s) { Txt.Para(C, x, y, w, s, P.TextMute, 11); }
    public void Blit(Bitmap b, int x, int y, int k = 1) { C.Blit(b, x, y, k); }
    public void Slice(string file, int x, int y, int w, int h, int k = 1)
    {
        var a = Kit.Asset(file);
        var tmp = new Cv(w, h);
        if (a.L + a.T + a.R + a.B == 0) tmp.Blit(a.Img, 0, 0); else tmp.Slice(a.Img, 0, 0, w, h, a.L, a.T, a.R, a.B);
        C.Blit(tmp.B, x, y, k);
    }
    public void Checker(int x, int y, int w, int h)
    {
        for (int j = 0; j < h; j++) for (int i = 0; i < w; i++) C.Set(x + i, y + j, ((i / 4 + j / 4) % 2 == 0) ? P.H("3A3C44") : P.H("30323A"));
    }
}

public static class Boards
{
    public static List<Board> Build(List<Cv> portraitThumbs)
    {
        var L = new List<Board>();
        L.Add(Cover(portraitThumbs)); L.Add(Colors()); L.Add(Type()); L.Add(Spacing()); L.Add(PanelsB()); L.Add(ButtonsB());
        L.Add(HudB()); L.Add(IconsB()); L.Add(SlotsB()); L.Add(BarsB()); L.Add(DialogB()); L.Add(IndexB());
        return L;
    }

    static Board Cover(List<Cv> thumbs)
    {
        var b = new Board("B01_Cover", "RPG UI MASTER DESIGN SYSTEM", "Premium pixel-art fantasy UI for GAME01. Portrait 9:16 first, landscape 16:9 second.");
        int x = 16;
        for (int i = 0; i < thumbs.Count && i < 2; i++) { b.C.Blit(thumbs[i].B, x, 64); b.C.Box(x - 1, 63, thumbs[i].W + 2, thumbs[i].Ht + 2, P.GoldD); x += thumbs[i].W + 12; }
        int nx = x + 8;
        b.H(nx, 70, "ONE KIT, EVERY SCREEN");
        b.Note(nx, 88, 960 - nx - 16, "Dark wood + charcoal foundations, forest green surfaces, muted gold trim, warm parchment for selected detail panels. Red HP, blue mana, green EXP. Press Start 2P text drawn by Unity, never baked into art.");
        string[] facts = { "UI PIXEL GRID", "Portrait 288x512 UI px  x2 = 576x1024", "Landscape 640x360 UI px x3 = 1920x1080", "World PPU 16, PPC 144x256 / 480x270 (x4)", "", "DELIVERABLES", Kit.All.Count + " UI PNGs at 1x (transparent)", "30 portrait + 8 landscape mockups", "12 design-system boards", "GAME01_UI_ASSET_MANIFEST.csv" };
        for (int i = 0; i < facts.Length; i++) b.T(nx, 170 + i * 14, facts[i], i == 0 || i == 5 ? P.GoldL : P.TextW);
        b.Note(nx, 330, 960 - nx - 16, "Mockup data is sample data from WorldContent, LevelConfig and the class kits. Purple CONCEPT tags mark UI for systems that are only placeholders in the game today.");
        return b;
    }

    static void Sw(Board b, int x, int y, Color c, string name, string role)
    {
        b.C.Fill(x, y, 40, 28, c); b.C.Box(x - 1, y - 1, 42, 30, P.Ink);
        b.T(x + 48, y + 2, name, P.TextW); b.T(x + 48, y + 12, P.Hex(c), P.GoldL);
        Txt.Draw(b.C, x + 48, y + 22, role, P.TextMute, 1, 0, false);
    }
    static Board Colors()
    {
        var b = new Board("B02_Colors", "A. COLOR PALETTE", "Every UI pixel uses these 40 colours. Text colours are tested on Char #1E1F24 and Parchment #DCC59A.");
        var groups = new List<Tuple<string, Color[], string[], string[]>>
        {
            Tuple.Create("FOUNDATION", new[] { P.Ink, P.WoodD, P.Wood, P.WoodM, P.WoodL, P.Char0, P.Char, P.Char2, P.Stone, P.StoneL },
                new[] { "INK", "WOOD D", "WOOD", "WOOD M", "WOOD L", "CHAR 0", "CHAR", "CHAR 2", "STONE", "STONE L" },
                new[] { "outlines", "frame shade", "frame", "button face", "bevel light", "deep fill", "panel body", "rows", "disabled", "disabled hi" }),
            Tuple.Create("SURFACE + ACCENT", new[] { P.ForestD, P.Forest, P.ForestM, P.ForestL, P.GoldD, P.Gold, P.GoldL, P.GoldW, P.ParchD, P.Parch },
                new[] { "FOREST D", "FOREST", "FOREST M", "FOREST L", "GOLD D", "GOLD", "GOLD L", "GOLD W", "PARCH D", "PARCH" },
                new[] { "dialogue", "reward body", "ribbon", "positive", "trim shade", "trim", "titles", "sparkle", "parch edge", "selected" }),
            Tuple.Create("TEXT + STATE", new[] { P.TextW, P.TextBlue, P.TextMute, P.TextDark, P.Red, P.RedL, P.Blue, P.BlueL, P.Green, P.GreenL },
                new[] { "TEXT W", "TEXT BLUE", "TEXT MUTE", "TEXT DARK", "HP RED", "HP LIGHT", "MANA", "MANA L", "EXP", "EXP L" },
                new[] { "body text", "info", "secondary", "on parch", "hp bar", "danger", "mana", "shield", "exp bar", "positive" }),
        };
        for (int g = 0; g < groups.Count; g++)
        {
            int gx = 16 + g * 316;
            b.H(gx, 62, groups[g].Item1);
            for (int i = 0; i < 10; i++) Sw(b, gx + (i % 2) * 150, 82 + (i / 2) * 42, groups[g].Item2[i], groups[g].Item3[i], groups[g].Item4[i]);
        }
        b.H(16, 300, "EXTRA (SKILL / CONCEPT / FIRE)");
        var ex = new[] { P.RedD, P.BlueD, P.GreenD, P.Gray, P.GrayL, P.PurpleD, P.Purple, P.PurpleL, P.Orange, P.OrangeL };
        var en = new[] { "RED D", "BLUE D", "GREEN D", "GRAY", "GRAY L", "PURPLE D", "PURPLE", "PURPLE L", "ORANGE", "ORANGE L" };
        for (int i = 0; i < 10; i++) Sw(b, 16 + (i % 5) * 188, 320 + (i / 5) * 42, ex[i], en[i], i >= 5 && i <= 7 ? "concept tag" : "");
        b.H(16, 420, "RULES");
        b.Note(16, 438, 928, "1 px INK outline on every panel, slot and icon. Light from top-left: bevel light on top/left, shade on bottom/right. No gradients, no glow, no soft shadows. HUD backplates use baked alpha (Char 0 at 170). Gold is for focus, titles and rewards only - never body text on dark backgrounds longer than a title.");
        return b;
    }

    static Board Type()
    {
        var b = new Board("B03_Typography", "B. UI TYPOGRAPHY", "Press Start 2P (already in Assets/Art/Fonts). 8 px glyph cell. Size = 8 x integer only.");
        b.H(16, 62, "SCALE");
        Txt.Draw(b.C, 16, 82, "TITLE x2 - VICTORY", P.GoldL, 2);
        Txt.Draw(b.C, 16, 108, "HEADER x1 - QUESTS, BAG, WAVE 2/4", P.GoldL, 1);
        Txt.Draw(b.C, 16, 124, "Body x1 - The road starts here.", P.TextW, 1);
        Txt.Draw(b.C, 16, 140, "Info x1 - CLEAR WAVES  CD 5s", P.TextBlue, 1);
        Txt.Draw(b.C, 16, 156, "Muted x1 - 7/25 SLOTS  v1.0", P.TextMute, 1);
        b.H(16, 186, "TREATMENTS");
        b.C.Fill(16, 204, 300, 30, P.Char); Txt.Draw(b.C, 24, 215, "PANEL: 1px ink drop shadow", P.TextW, 1, 0, true);
        b.C.Fill(16, 240, 300, 30, P.Parch); Txt.Draw(b.C, 24, 251, "PARCHMENT: dark, no shadow", P.TextDark, 1, 0, false);
        b.C.Fill(16, 276, 300, 30, P.H("6A9A5A")); Txt.Outlined(b.C, 24, 287, "WORLD: full ink outline", P.TextW, 1);
        b.C.Fill(16, 312, 300, 30, P.Gold); Txt.Draw(b.C, 24, 323, "GOLD BUTTON: dark text", P.TextDark, 1, 0, true, P.GoldL);
        b.H(340, 62, "IN-GAME NUMBERS (DYNAMIC, UNITY)");
        b.C.Fill(340, 80, 300, 120, P.H("6A9A5A"));
        Txt.Outlined(b.C, 360, 92, "18", P.TextW, 2); Txt.Outlined(b.C, 420, 92, "26", P.GoldL, 2); Txt.Outlined(b.C, 480, 92, "42", P.RedL, 2);
        Txt.Outlined(b.C, 560, 92, "x12", P.GoldL, 2); Txt.Outlined(b.C, 576, 112, "HITS", P.TextW, 1);
        Txt.Outlined(b.C, 360, 140, "Goblin Lv.3", P.TextW, 1); Txt.Outlined(b.C, 360, 160, "Mira", P.GoldL, 1); Txt.Outlined(b.C, 460, 160, "CYCLOPS Lv.8", P.TextW, 1);
        b.Note(340, 210, 600, "Damage: white normal, gold strong/finisher (existing DamagePopup). Combo 'xN HITS' gold x2 + white x1. Mob names white, NPC names gold. Never bake numbers into sprites.");
        b.H(340, 270, "COLOUR BY ROLE");
        string[] r = { "Titles, selected, currency", "Body text on dark", "Info, objectives, tips", "Labels, disabled, hints", "Text on parchment / gold", "Positive (+EXP, done)", "Warning, boss, danger" };
        Color[] c = { P.GoldL, P.TextW, P.TextBlue, P.TextMute, P.TextDark, P.GreenL, P.RedL };
        for (int i = 0; i < r.Length; i++) { b.C.Fill(340, 290 + i * 16, 10, 10, c[i]); b.C.Box(340, 290 + i * 16, 10, 10, P.Ink); b.T(358, 291 + i * 16, r[i]); }
        b.Note(16, 360, 300, "Line height 11-12 UI px. Max ~32 characters per line in portrait windows. Keep labels upper case; sentences in sentence case.");
        return b;
    }

    static Board Spacing()
    {
        var b = new Board("B04_Spacing", "C. SPACING, GRID + LAYOUT", "All sizes in UI px. Portrait canvas 288x512 (x2). Landscape canvas 640x360 (x3).");
        b.H(16, 62, "SPACING SCALE");
        int[] sp = { 2, 4, 8, 12, 16, 24 };
        for (int i = 0; i < sp.Length; i++) { b.C.Fill(16 + i * 50, 82, sp[i] * 2, sp[i] * 2, P.Gold); b.T(16 + i * 50, 136, sp[i].ToString(), P.TextW); }
        b.Note(16, 156, 300, "Safe margin 8. Panel padding 12 (main) / 9 (popup) / 5 (tooltip). Gap between buttons 6-8. Touch target >= 22 UI px (44 px at 576 wide). Attack 56, skill 38, interact 40.");
        b.H(16, 236, "SCALING");
        b.Note(16, 254, 300, "Unity CanvasScaler: Scale With Screen Size, reference 288x512 (portrait profile) and 640x360 (landscape profile), match = height (portrait) / 0.5. Keep integer scale where the device allows (576x1024 x2, 1080x1920 x3.75 -> pixel snap). Sprites: PPU 16, Point, no compression, Pixels Per Unit Multiplier 1 for sliced images.");
        // portrait wireframe
        int px = 360, py = 60, pw = 144, ph = 256;
        b.C.Fill(px, py, pw, ph, P.H("2E4A36")); b.C.Box(px, py, pw, ph, P.GoldD);
        Zone(b, px + 4, py + 4, 76, 24, P.Red, "HP LV");
        Zone(b, px + 98, py + 4, 42, 12, P.Blue, "MENU");
        Zone(b, px + 40, py + 30, 64, 16, P.Gold, "WAVE");
        Zone(b, px + 24, py + 60, 96, 110, P.ForestL, "CLEAR");
        Zone(b, px + 4, py + 210, 36, 40, P.Purple, "MOVE");
        Zone(b, px + 72, py + 176, 68, 76, P.Orange, "ACT");
        b.T(px, py + ph + 6, "PORTRAIT 9:16", P.GoldL);
        // landscape wireframe
        int lx = 540, ly = 60, lw = 400, lh = 225;
        b.C.Fill(lx, ly, lw, lh, P.H("2E4A36")); b.C.Box(lx, ly, lw, lh, P.GoldD);
        Zone(b, lx + 4, ly + 4, 100, 30, P.Red, "HP LV EXP");
        Zone(b, lx + 150, ly + 4, 100, 22, P.Gold, "WAVE");
        Zone(b, lx + 320, ly + 4, 76, 18, P.Blue, "MENU");
        Zone(b, lx + 4, ly + 40, 100, 26, P.TextBlue, "QUEST");
        Zone(b, lx + 100, ly + 50, 200, 110, P.ForestL, "CLEAR COMBAT AREA");
        Zone(b, lx + 112, ly + 186, 176, 34, P.Orange, "SKILLS 1-5");
        Zone(b, lx + 356, ly + 180, 40, 40, P.Orange, "R");
        b.T(lx, ly + lh + 6, "LANDSCAPE 16:9 (R CORNER, SKILLS CENTRE)", P.GoldL);
        b.Note(360, 350, 580, "Anchors: top-left (HP), top-centre (wave/objective/boss), top-right (menu, map, bag, coins), bottom-left (joystick, mobile only), bottom-right (attack + skill arc, mobile) or bottom-centre hotbar + R corner (PC). Centre 60% stays clear for hero, mobs and VFX.");
        return b;
    }
    static void Zone(Board b, int x, int y, int w, int h, Color c, string t)
    {
        b.C.Fill(x, y, w, h, P.A(c, 110)); b.C.Box(x, y, w, h, c);
        if (Txt.Width(t, 1) < w - 2) Txt.Draw(b.C, x + w / 2, y + h / 2 - 4, t, P.TextW, 1, 1, true);
    }

    static Board PanelsB()
    {
        var b = new Board("B05_Panels", "D. PANEL + WINDOW STYLES", "9-slice borders listed as L,T,R,B (UI px). Shown x2.");
        var list = new[] { "UI_Panel_Main.png", "UI_Panel_Popup.png", "UI_Panel_Parchment.png", "UI_NPC_DialoguePanel.png", "UI_Panel_Confirm.png", "UI_Panel_Reward.png" };
        var names = new[] { "MAIN WINDOW", "SMALL POPUP", "PARCHMENT", "NPC DIALOGUE", "CONFIRMATION", "REWARD WINDOW" };
        for (int i = 0; i < list.Length; i++)
        {
            int x = 16 + (i % 3) * 312, y = 60 + (i / 3) * 186;
            b.Slice(list[i], x, y + 14, 72, 64, 2);
            var a = Kit.Asset(list[i]);
            b.T(x, y, names[i], P.GoldL);
            b.T(x + 154, y + 18, list[i].Replace(".png", ""), P.TextW);
            b.T(x + 154, y + 32, "SRC " + a.W + "x" + a.H, P.TextMute);
            b.T(x + 154, y + 44, "9-SLICE " + a.Border, P.TextBlue);
            b.Note(x + 154, y + 60, 150, a.Usage);
        }
        b.H(16, 434, "SMALL PIECES");
        b.Slice("UI_Panel_Tooltip.png", 16, 452, 48, 22, 2); b.T(16, 500, "TOOLTIP", P.TextMute);
        b.Slice("UI_Panel_HUD.png", 130, 452, 48, 22, 2); b.T(130, 500, "HUD", P.TextMute);
        b.Slice("UI_Panel_Inset.png", 244, 452, 48, 22, 2); b.T(244, 500, "INSET", P.TextMute);
        b.Slice("UI_ListRow_Selected.png", 358, 452, 60, 14, 2); b.T(358, 500, "LIST ROW", P.TextMute);
        var t = new Cv(96, 18); t.Slice(Kit.Get("UI_Header_Ribbon.png"), 0, 0, 96, 18, 12, 0, 12, 0); b.Blit(t.B, 500, 450, 2); b.T(500, 500, "HEADER RIBBON (3-SLICE)", P.TextMute);
        b.Slice("UI_NamePlate.png", 720, 452, 48, 14, 2); b.T(720, 500, "NAME PLATE", P.TextMute);
        b.Blit(Kit.Get("UI_Divider_Gem.png"), 860, 456, 3); b.T(840, 500, "DIVIDER", P.TextMute);
        return b;
    }

    static Board ButtonsB()
    {
        var b = new Board("B06_Buttons", "E. BUTTON STYLES + STATES", "32x18 source, 9-slice 4,4,4,5. Pressed shifts the label down 1 px. Shown x2.");
        string[] fam = { "Primary", "Secondary", "Positive", "Danger" };
        string[] lab = { "CONFIRM", "SETTINGS", "CRAFT", "EXIT" };
        for (int s = 0; s < 5; s++) b.T(150 + s * 160, 60, Parts.States[s].ToUpper(), P.GoldL);
        for (int f = 0; f < 4; f++)
        {
            b.T(16, 86 + f * 52, fam[f].ToUpper(), P.TextW);
            for (int s = 0; s < 5; s++)
            {
                var sh = new Shot("tmp", "", true);
                sh.Ui = new Cv(76, 22);
                sh.Btn(0, 0, 76, 22, fam[f], Parts.States[s], lab[f]);
                b.Blit(sh.Ui.B, 150 + s * 160, 76 + f * 52, 2);
            }
        }
        b.H(16, 300, "ICON BUTTON 22x22");
        for (int s = 0; s < 5; s++) { b.Blit(Kit.Get("UI_Button_Icon_" + Parts.States[s] + ".png"), 16 + s * 56, 320, 2); b.Blit(Icons.Get("Settings"), 22 + s * 56, 324 + (s == 2 ? 2 : 0), 2); }
        b.H(320, 300, "CLOSE");
        b.Blit(Kit.Get("UI_Button_Close_Normal.png"), 320, 320, 2); b.Blit(Kit.Get("UI_Button_Close_Pressed.png"), 360, 320, 2);
        b.H(420, 300, "TABS");
        b.Slice("UI_Tab_Selected.png", 420, 320, 56, 16, 2); b.Slice("UI_Tab_Normal.png", 540, 320, 56, 16, 2);
        b.H(16, 390, "TOGGLES / SLIDER / CHECKBOX");
        b.Blit(Kit.Get("UI_Toggle_Off.png"), 16, 410, 2); b.Blit(Kit.Get("UI_Toggle_On.png"), 76, 410, 2);
        b.Blit(Kit.Get("UI_Checkbox_Off.png"), 140, 410, 2); b.Blit(Kit.Get("UI_Checkbox_On.png"), 172, 410, 2);
        var sl = new Cv(100, 12); sl.Slice(Kit.Get("UI_Slider_Track.png"), 0, 3, 100, 6, 3, 2, 3, 2); for (int i = 0; i < 60; i++) sl.Blit(Kit.Get("UI_Slider_Fill.png"), 2 + i, 5); sl.Blit(Kit.Get("UI_Slider_Knob.png"), 58, 1);
        b.Blit(sl.B, 220, 406, 2);
        b.Blit(Kit.Get("UI_Scroll_Track.png"), 450, 404, 2); b.Blit(Kit.Get("UI_Scroll_Thumb.png"), 450, 408, 2);
        b.Blit(Kit.Get("UI_Cursor_Arrow.png"), 490, 410, 2); b.Blit(Kit.Get("UI_Badge_Alert.png"), 520, 410, 2);
        b.Note(620, 300, 320, "Primary = the one main action per screen. Secondary = everything neutral. Positive = craft / harvest / enter. Danger = exit / leave. Selected = keyboard or gamepad focus (gold ring). Disabled keeps its size; text goes muted.");
        return b;
    }

    static Board HudB()
    {
        var b = new Board("B07_HUD", "F. HUD COMPONENTS", "Shown x2. Dynamic values (HP, EXP, cooldown, combo, damage) are Unity text.");
        var sh = new Shot("hud", "", true); sh.Ui = new Cv(200, 60);
        sh.PortraitAt(Screens.FaceOf("Aren"), 0, 0); sh.Pic(Kit.Get("UI_LevelBadge.png"), 26, 26); sh.T(34, 30, "2", P.GoldL, 1, 1, false);
        sh.Bar(42, 5, 104, "HP", 0.8, false, "320/400"); sh.Bar(42, 18, 104, "EXP", 0.4, true); sh.T(44, 27, "AREN", P.GoldL);
        b.H(16, 60, "HERO BLOCK (TOP-LEFT)"); b.Blit(sh.Ui.B, 16, 78, 2);
        b.H(340, 60, "SKILL SLOT STATES");
        var ic = Img.Load(World.Heroes + "UI/Icons/Icon_Aren_2_ShieldBash.png");
        string[] st = { "Default", "Selected", "Disabled" };
        for (int i = 0; i < 3; i++) { var s2 = new Shot("s", "", true); s2.Ui = new Cv(40, 46); s2.Skill(0, 0, ic, st[i], 0, null, "1"); b.Blit(s2.Ui.B, 340 + i * 90, 78, 2); b.T(340 + i * 90, 172, st[i].ToUpper(), P.TextMute); }
        var s3 = new Shot("s", "", true); s3.Ui = new Cv(40, 46); s3.Skill(0, 0, ic, "Default", 0.6, "4", "1"); b.Blit(s3.Ui.B, 610, 78, 2); b.T(610, 172, "COOLDOWN", P.TextMute);
        b.Blit(Kit.Get("UI_SkillSlot_ReadyFlash.png"), 700, 78, 2); b.T(700, 172, "READY FLASH", P.TextMute);
        b.Blit(Kit.Get("UI_KeyCap.png"), 800, 90, 3); b.T(800, 172, "KEY CAP", P.TextMute);
        b.H(16, 196, "MOBILE ACTIONS");
        b.Blit(Kit.Get("UI_AttackButton_Normal.png"), 16, 214, 2); b.Blit(Screens.RoundIcon(0), 40, 238, 2); b.Blit(Kit.Get("UI_AttackButton_Pressed.png"), 136, 214, 2); b.Blit(Screens.RoundIcon(0), 160, 240, 2);
        b.Blit(Kit.Get("UI_Joystick_Base.png"), 256, 206, 2); b.Blit(Kit.Get("UI_Joystick_Knob.png"), 304, 230, 2);
        b.Blit(Kit.Get("UI_InteractButton_Normal.png"), 400, 222, 2); b.Blit(Icons.Get("Talk"), 424, 242, 2);
        b.T(16, 332, "ATTACK N / P", P.TextMute); b.T(256, 340, "JOYSTICK", P.TextMute); b.T(400, 312, "INTERACT", P.TextMute);
        b.H(520, 196, "BOSS + COMBAT FEEDBACK");
        var bs = new Shot("b", "", true); bs.Ui = new Cv(200, 70);
        bs.TO(100, 0, "CYCLOPS Lv.8", P.TextW, 1, 1); bs.BossBar(0, 12, 200, 0.64);
        bs.Ribbon(100, 36, "WAVE 2/4");
        b.Blit(bs.Ui.B, 520, 214, 2);
        var cb = new Shot("c", "", true); cb.Ui = new Cv(140, 50); cb.Ui.Fill(0, 0, 140, 50, P.H("6A9A5A"));
        cb.TO(130, 6, "x12", P.GoldL, 2, 2); cb.TO(130, 24, "HITS", P.TextW, 1, 2); cb.TO(30, 10, "26", P.GoldL, 2, 1);
        cb.TO(60, 38, "Goblin Lv.3", P.TextW, 1, 1);
        b.Blit(cb.Ui.B, 520, 364, 2);
        b.H(16, 370, "WORLD + HUD PLATES");
        var q = new Shot("q", "", true); q.Ui = new Cv(240, 70);
        q.Panel("UI_Panel_HUD.png", 0, 0, 150, 38); q.Icon("Quests", 3, 3); q.T(22, 7, "THE ROAD", P.GoldL); q.T(8, 22, "Bring ore", P.TextW); q.T(144, 22, "0/1", P.GreenL, 1, 2);
        q.Panel("UI_Panel_HUD.png", 160, 0, 70, 16); q.Icon("Currency", 162, 0); q.T(226, 4, "128", P.GoldL, 1, 2);
        q.Panel("UI_Panel_Tooltip.png", 160, 24, 70, 16); q.Ui.Slice(Kit.Get("UI_KeyCap.png"), 163, 27, 11, 11, 3, 3, 3, 3); q.T(169, 29, "E", P.GoldL, 1, 1, false); q.T(178, 29, "SHOP", P.TextW);
        b.Blit(q.Ui.B, 16, 390, 2);
        return b;
    }

    static Board IconsB()
    {
        var b = new Board("B08_Icons", "G. ICON KIT", "16x16, 1 px ink outline, top-left light, 3 tones. Transparent PNG. Shown x3.");
        var icons = Kit.All.Where(a => a.Folder == "Icons").ToList();
        for (int i = 0; i < icons.Count; i++)
        {
            int x = 16 + (i % 10) * 94, y = 60 + (i / 10) * 74;
            b.Checker(x, y, 52, 52); b.Blit(icons[i].Img, x + 2, y + 2, 3);
            b.T(x, y + 56, icons[i].File.Replace("UI_Icon_", "").Replace(".png", "").ToUpper(), P.TextW);
        }
        int y0 = 60 + ((icons.Count + 9) / 10) * 74 + 4;
        b.H(16, y0, "ITEMS");
        var items = Kit.All.Where(a => a.Folder == "Icons/Items").ToList();
        for (int i = 0; i < items.Count; i++) { b.Blit(items[i].Img, 16 + i * 60, y0 + 18, 3); b.T(16 + i * 60, y0 + 70, items[i].File.Replace("UI_Item_", "").Replace(".png", "").ToUpper().Replace("TRAININGBLADE", "BLADE").Replace("QUESTTOKEN", "TOKEN"), P.TextMute); }
        b.H(520, y0, "EQUIP GHOSTS + MAP MARKERS");
        var gh = Kit.All.Where(a => a.Folder == "Icons/Equip").ToList();
        for (int i = 0; i < gh.Count; i++) { b.C.Fill(520 + i * 40, y0 + 18, 36, 36, P.Char0); b.Blit(gh[i].Img, 522 + i * 40, y0 + 20, 2); }
        var mk = Kit.All.Where(a => a.Folder == "Icons/Minimap").ToList();
        for (int i = 0; i < mk.Count; i++) b.Blit(mk[i].Img, 520 + i * 30, y0 + 64, 3);
        return b;
    }

    static Board SlotsB()
    {
        var b = new Board("B09_Slots", "H. INVENTORY + EQUIPMENT SLOTS", "Item icon 16 at (3,3) of the 22 slot. Quantity: Unity text, bottom-right, outlined. Shown x3.");
        string[] st = { "", "Hover", "Selected", "Disabled" }; string[] nm = { "NORMAL", "HOVER", "SELECTED", "DISABLED" };
        for (int i = 0; i < 4; i++)
        {
            var s = new Shot("s", "", true); s.Ui = new Cv(22, 22); s.Slot(0, 0, i == 3 ? null : "Item_Ore", i == 3 ? 0 : 3, st[i]);
            b.Blit(s.Ui.B, 16 + i * 110, 70, 3); b.T(16 + i * 110, 140, nm[i], P.TextMute);
        }
        string[] eq = { "UI_Equip_Slot.png", "UI_Equip_Slot_Selected.png", "UI_Equip_Slot_Locked.png" };
        for (int i = 0; i < 3; i++) { b.Blit(Kit.Get(eq[i]), 480 + i * 110, 66, 3); b.Blit(Icons.Get(i == 1 ? "Item_TrainingBlade" : "Equip_Armor_Ghost"), 495 + i * 110, 81, 3); }
        b.T(480, 152, "EQUIP SLOT  NORMAL / SELECTED / LOCKED", P.TextMute);
        b.H(16, 180, "BAG GRID EXAMPLE (x2)");
        var g = new Shot("g", "", true); g.Ui = new Cv(252, 106);
        g.Panel("UI_Panel_Inset.png", 0, 0, 252, 106);
        string[] it = { "Item_Ore", "Item_Wood", "Item_Herb", "Item_Sprout", "Item_Potion", "Item_TrainingBlade", "Item_QuestToken" }; int[] q = { 3, 2, 1, 2, 1, 1, 1 };
        for (int i = 0; i < 40; i++) { int x = 6 + (i % 10) * 24, y = 6 + (i / 10) * 24; if (i < 4 * 10) { if (i < 7) g.Slot(x, y, it[i], q[i], i == 5 ? "Selected" : ""); else g.Slot(x, y, null, 0, i >= 35 ? "Disabled" : ""); } }
        b.Blit(g.Ui.B, 16, 200, 2);
        b.Note(540, 200, 400, "Grid: 22 px slots, 2 px gap, 6 px inset. Categories follow ItemKind: RESOURCE, CONSUMABLE, EQUIPMENT, QUEST (coins shown as currency, not a slot). Selected item opens the parchment detail panel: name, category, quantity, description, actions (EQUIP for weapons). Locked slots use the disabled hatch.");
        return b;
    }

    static Board BarsB()
    {
        var b = new Board("B10_Bars", "I. PROGRESS BAR STYLES", "Frames are 9-slice; fills are 4 px wide columns (Filled Horizontal). Shown x3.");
        string[] f = { "HP", "Mana", "Progress", "Shield", "Ghost" }; double[] v = { 0.8, 0.55, 0.62, 0.3, 0.9 };
        string[] nm = { "HP (RED)", "MANA / ABSORB (BLUE)", "PROGRESS (GOLD)", "SHIELD OVERLAY", "DAMAGE TRAIL" };
        for (int i = 0; i < f.Length; i++)
        {
            var s = new Shot("s", "", true); s.Ui = new Cv(120, 12); s.Bar(0, 0, 120, f[i], v[i]);
            b.Blit(s.Ui.B, 16, 66 + i * 50, 3); b.T(390, 76 + i * 50, nm[i], P.TextW);
        }
        var e = new Shot("e", "", true); e.Ui = new Cv(120, 6); e.Bar(0, 0, 120, "EXP", 0.4, true); b.Blit(e.Ui.B, 16, 320, 3); b.T(390, 322, "EXP (THIN)", P.TextW);
        var bo = new Shot("b", "", true); bo.Ui = new Cv(220, 16); bo.BossBar(0, 0, 220, 0.64); b.Blit(bo.Ui.B, 16, 360, 3); b.T(690, 368, "BOSS BAR", P.TextW);
        var en = new Shot("n", "", true); en.Ui = new Cv(60, 24); en.Ui.Fill(0, 0, 60, 24, P.H("6A9A5A")); en.MobLabel(0, 0, -16, "", 0.55);
        var ef = Kit.Asset("UI_EnemyBar_Frame.png"); var em = new Cv(28, 4); em.Slice(ef.Img, 0, 0, 28, 4, 1, 1, 1, 1); for (int i = 0; i < 14; i++) em.Blit(Kit.Get("UI_Bar_Fill_Enemy.png"), 1 + i, 1);
        b.Blit(em.B, 16, 440, 4); b.T(150, 444, "ENEMY HP (SHOWN AFTER FIRST DAMAGE, EXISTING RULE)", P.TextW);
        b.Note(520, 76, 420, "HP 6 px fill, 3 px frame. Damage: snap HP fill, then ease the ghost (parchment) trail behind it for 0.4 s. Boss bar: name above in white, numbers optional below in muted text.");
        return b;
    }

    static Board DialogB()
    {
        var b = new Board("B11_Tooltip_Dialogue", "J. TOOLTIP + DIALOGUE STYLES", "One dialogue foundation for all NPCs; only name, portrait, line, service action and icon change.");
        var t = new Shot("t", "", true); t.Ui = new Cv(180, 60);
        t.Panel("UI_Panel_Tooltip.png", 0, 0, 180, 54); t.T(8, 8, "SHIELD BASH", P.GoldL); t.T(172, 8, "CD 5s", P.TextBlue, 1, 2);
        Txt.Para(t.Ui, 8, 22, 164, "Dash 2 tiles, stun 0.6s.", P.TextW, 11);
        b.H(16, 60, "TOOLTIP"); b.Blit(t.Ui.B, 16, 78, 2);
        var d = new Shot("d", "", true); d.Ui = new Cv(280, 190);
        Screens.Dialogue(d, "BRAM", "SMITH", "One blade. Ore and wood.", "Crafting", new[] { "OPEN FORGE", "GOODBYE" }, 4, 26, 270);
        b.H(400, 60, "NPC DIALOGUE"); b.Blit(d.Ui.B, 400, 64, 2);
        b.H(16, 220, "NPC VARIANTS (SAME PANEL)");
        string[] n = { "WARDEN", "MIRA", "BRAM", "LARK", "NELL" }; string[] sv = { "DUNGEONS", "SHOP", "SMITH", "QUESTS", "STORAGE" };
        string[] ic = { "Dungeon", "Shop", "Crafting", "Quests", "Storage" }; string[] ac = { "START 1-4", "OPEN SHOP", "OPEN FORGE", "SHOW QUESTS", "OPEN STORAGE" };
        for (int i = 0; i < 5; i++)
        {
            int y = 240 + i * 52;
            b.C.Fill(16, y, 360, 46, P.Char); b.C.Box(16, y, 360, 46, P.Ink);
            b.Blit(Icons.Get(ic[i]), 22, y + 7, 2);
            b.T(64, y + 8, n[i] + " - " + sv[i], P.GoldL);
            b.T(64, y + 26, "ACTION: " + ac[i], P.TextW);
        }
        b.Note(16, 506, 360, "Warden line is not in WorldContent yet; others use the existing NPC lines.");
        return b;
    }

    static Board IndexB()
    {
        var b = new Board("B12_Asset_Index", "UNITY-READY ASSET LIBRARY", "Docs/UI/export/<folder>/<file>.png at 1 UI px. Full list: GAME01_UI_ASSET_MANIFEST.csv");
        var groups = Kit.All.GroupBy(a => a.Folder).ToList();
        int x = 16, y = 62;
        foreach (var g in groups)
        {
            b.H(x, y, g.Key.ToUpper() + " (" + g.Count() + ")");
            int cx = x, cy = y + 18, rowH = 0;
            foreach (var a in g)
            {
                int w = Math.Min(a.W, 64), h = Math.Min(a.H, 64);
                if (cx + w > x + 300) { cx = x; cy += rowH + 4; rowH = 0; }
                var c = new Cv(a.Img).Crop(0, 0, w, h);
                b.C.Blit(c.B, cx, cy); cx += w + 4; rowH = Math.Max(rowH, h);
            }
            y = cy + rowH + 12;
            if (y > 420) { x += 316; y = 62; }
        }
        b.Note(16, 490, 928, "Naming: UI_<Component>_<Variant>_<State>.png. States: Normal, Hover, Pressed, Selected, Disabled. No text or numbers baked except the static X on the close button and ! on the alert badge.");
        return b;
    }
}
