using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

/// <summary>One exported Unity-ready PNG (1 UI px = 1 image px) and its manifest row.</summary>
public class UiAsset
{
    public string Id, File, Screen, Type, Folder, Usage, State, Notes;
    public int W, H;
    public int L, T, R, B;          // 9-slice borders (0 = not sliced)
    public bool Transparent = true;
    public Bitmap Img;
    public string Border { get { return (L + T + R + B) == 0 ? "-" : (L + "," + T + "," + R + "," + B); } }
}

public static class Kit
{
    public static readonly List<UiAsset> All = new List<UiAsset>();
    static readonly Dictionary<string, UiAsset> ByFile = new Dictionary<string, UiAsset>();
    public static int Counter = 0;

    public static Bitmap Reg(string folder, string file, string screen, string type, string usage, string state, Bitmap img,
        int l = 0, int t = 0, int r = 0, int b = 0, string notes = "")
    {
        if (ByFile.ContainsKey(file)) return ByFile[file].Img;
        Counter++;
        var a = new UiAsset
        {
            Id = "UI" + Counter.ToString("000"), File = file, Folder = folder, Screen = screen, Type = type, Usage = usage,
            State = state, Img = img, W = img.Width, H = img.Height, L = l, T = t, R = r, B = b, Notes = notes
        };
        All.Add(a); ByFile[file] = a;
        return img;
    }
    public static Bitmap Get(string file) { return ByFile[file].Img; }
    public static UiAsset Asset(string file) { return ByFile[file]; }
}

/// <summary>Procedural pixel frames. Rings are drawn from the outside in: (top/left colour, bottom/right colour).</summary>
public static class Parts
{
    public struct Ring { public Color A, B; public Ring(Color a, Color b) { A = a; B = b; } public Ring(Color a) { A = a; B = a; } }

    public static Cv Frame(int w, int h, Ring[] rings, Color fill, int cut = 1, Color? innerTop = null)
    {
        var cv = new Cv(w, h);
        int n = rings.Length;
        cv.Fill(n, n, w - 2 * n, h - 2 * n, fill);
        if (innerTop.HasValue) cv.HLine(n, n, w - 2 * n, innerTop.Value);
        for (int i = 0; i < n; i++)
        {
            Color a = rings[i].A, b = rings[i].B;
            int x0 = i, y0 = i, x1 = w - 1 - i, y1 = h - 1 - i;
            for (int x = x0; x <= x1; x++) { cv.Set(x, y0, a); cv.Set(x, y1, b); }
            for (int y = y0; y <= y1; y++) { cv.Set(x0, y, a); cv.Set(x1, y, b); }
            cv.Set(x1, y0, Mid(a, b)); cv.Set(x0, y1, Mid(a, b));
        }
        if (cut >= 1 && n >= 2)
        {
            Color t = Color.FromArgb(0, 0, 0, 0);
            cv.Set(0, 0, t); cv.Set(w - 1, 0, t); cv.Set(0, h - 1, t); cv.Set(w - 1, h - 1, t);
            Color o = rings[0].A;
            cv.Set(1, 1, o); cv.Set(w - 2, 1, o); cv.Set(1, h - 2, o); cv.Set(w - 2, h - 2, o);
        }
        if (cut >= 2 && n >= 3)
        {
            Color t = Color.FromArgb(0, 0, 0, 0);
            foreach (var p in new[] { new Point(1, 0), new Point(0, 1) })
            {
                cv.Set(p.X, p.Y, t); cv.Set(w - 1 - p.X, p.Y, t); cv.Set(p.X, h - 1 - p.Y, t); cv.Set(w - 1 - p.X, h - 1 - p.Y, t);
            }
            Color o = rings[0].A;
            cv.Set(2, 1, o); cv.Set(1, 2, o); cv.Set(w - 3, 1, o); cv.Set(w - 2, 2, o);
            cv.Set(2, h - 2, o); cv.Set(1, h - 3, o); cv.Set(w - 3, h - 2, o); cv.Set(w - 2, h - 3, o);
        }
        return cv;
    }
    static Color Mid(Color a, Color b) { return a.ToArgb() == b.ToArgb() ? a : (a.GetBrightness() > b.GetBrightness() ? b : a); }

    static void Rivet(Cv cv, int x, int y)
    {
        cv.Set(x, y, P.GoldW); cv.Set(x + 1, y, P.Gold); cv.Set(x, y + 1, P.Gold); cv.Set(x + 1, y + 1, P.GoldD);
    }
    static void Sparkle(Cv cv, int x, int y)
    {
        cv.Set(x, y, P.GoldW); cv.Set(x - 1, y, P.GoldL); cv.Set(x + 1, y, P.GoldL); cv.Set(x, y - 1, P.GoldL); cv.Set(x, y + 1, P.GoldL);
    }

    // ---------------------------------------------------------------- panels
    public static void BuildPanels()
    {
        var I = new Ring(P.Ink);
        // Main window: wood frame, gold inner line, charcoal body
        var main = Frame(32, 32, new[] { I, new Ring(P.WoodL, P.Wood), new Ring(P.WoodM, P.WoodD), new Ring(P.WoodM, P.WoodD), new Ring(P.Wood, P.WoodD), I, new Ring(P.GoldL, P.GoldD), I }, P.Char, 2, P.Char0);
        Rivet(main, 2, 2); Rivet(main, 28, 2); Rivet(main, 2, 28); Rivet(main, 28, 28);
        Kit.Reg("Panels", "UI_Panel_Main.png", "All windows", "Panel 9-slice", "Main window background (Inventory, Quest, Shop, Settings...)", "Normal", main.B, 9, 9, 9, 9, "Content padding 12 UI px");

        var popup = Frame(24, 24, new[] { I, new Ring(P.StoneL, P.Stone), new Ring(P.Stone, P.Char2), I, new Ring(P.GoldL, P.GoldD), I }, P.Char, 1, P.Char0);
        Kit.Reg("Panels", "UI_Panel_Popup.png", "Popups", "Panel 9-slice", "Small popup (gather, plot, info)", "Normal", popup.B, 7, 7, 7, 7, "Content padding 9");

        var parch = Frame(24, 24, new[] { I, new Ring(P.WoodL, P.Wood), new Ring(P.WoodM, P.WoodD), I, new Ring(P.ParchL, P.ParchD) }, P.Parch, 1);
        Kit.Reg("Panels", "UI_Panel_Parchment.png", "Details / selected", "Panel 9-slice", "Selected item details, map sheet, quest detail. Text colour #2A1E18", "Normal", parch.B, 5, 5, 5, 5, "Content padding 7");

        var dlg = Frame(32, 32, new[] { I, new Ring(P.ForestL, P.ForestM), new Ring(P.ForestM, P.ForestD), new Ring(P.ForestM, P.ForestD), I, new Ring(P.GoldL, P.GoldD), I }, P.A(P.Char, 240), 2, P.A(P.Char0, 240));
        Rivet(dlg, 2, 2); Rivet(dlg, 28, 2); Rivet(dlg, 2, 28); Rivet(dlg, 28, 28);
        Kit.Reg("Panels", "UI_NPC_DialoguePanel.png", "NPC Dialogue", "Panel 9-slice", "Shared NPC dialogue box (all 5 NPCs)", "Normal", dlg.B, 8, 8, 8, 8, "Body alpha 240");

        var tip = Frame(12, 12, new[] { I, new Ring(P.GoldL, P.GoldD), I }, P.A(P.Char2, 245), 1);
        Kit.Reg("Panels", "UI_Panel_Tooltip.png", "Tooltip", "Panel 9-slice", "Tooltip / small label backplate", "Normal", tip.B, 4, 4, 4, 4, "Content padding 5");

        var conf = Frame(32, 40, new[] { I, new Ring(P.StoneL, P.Stone), new Ring(P.Stone, P.Char2), I, new Ring(P.GoldL, P.GoldD), I }, P.Char, 1, P.Char0);
        conf.Fill(6, 6, 20, 11, P.WoodM); conf.HLine(6, 6, 20, P.WoodL); conf.HLine(6, 16, 20, P.Wood); conf.HLine(6, 17, 20, P.Ink); conf.HLine(6, 18, 20, P.GoldD);
        Kit.Reg("Panels", "UI_Panel_Confirm.png", "Confirmation", "Panel 9-slice", "Window with title bar (confirm, boss entrance, settings)", "Normal", conf.B, 7, 19, 7, 7, "Title text centred in rows 6-16");

        var rew = Frame(32, 32, new[] { I, new Ring(P.GoldL, P.GoldD), new Ring(P.Gold, P.GoldD), I, new Ring(P.ForestL, P.ForestD), I }, P.Forest, 2, P.ForestD);
        Sparkle(rew, 3, 3); Sparkle(rew, 28, 3); Sparkle(rew, 3, 28); Sparkle(rew, 28, 28);
        Kit.Reg("Panels", "UI_Panel_Reward.png", "Reward / Level Complete", "Panel 9-slice", "Reward window, treasure, level complete", "Normal", rew.B, 7, 7, 7, 7, "Content padding 9");

        var hud = Frame(8, 8, new[] { new Ring(P.A(P.Ink, 210)), new Ring(P.A(P.Char2, 220), P.A(P.Char0, 220)) }, P.A(P.Char0, 170), 1);
        Kit.Reg("Panels", "UI_Panel_HUD.png", "HUD", "Panel 9-slice", "Translucent HUD backplate (quest tracker, currency, objective)", "Normal", hud.B, 3, 3, 3, 3, "Over gameplay; alpha baked");

        var inset = Frame(8, 8, new[] { new Ring(P.Ink, P.Char2) }, P.Char0, 0);
        Kit.Reg("Panels", "UI_Panel_Inset.png", "Lists", "Panel 9-slice", "Recessed list / grid area inside a window", "Normal", inset.B, 2, 2, 2, 2);

        // Row (list item) normal / selected
        var row = Frame(16, 14, new[] { new Ring(P.Char2, P.Ink) }, P.Char, 0);
        Kit.Reg("Panels", "UI_ListRow_Normal.png", "Lists", "Panel 9-slice", "List row (levels, quests, shop rows)", "Normal", row.B, 2, 2, 2, 2);
        var rowS = Frame(16, 14, new[] { new Ring(P.GoldL, P.GoldD), new Ring(P.Ink) }, P.Mix(P.Char, P.WoodM, 0.35), 0);
        Kit.Reg("Panels", "UI_ListRow_Selected.png", "Lists", "Panel 9-slice", "List row selected", "Selected", rowS.B, 3, 3, 3, 3);
        var rowD = Frame(16, 14, new[] { new Ring(P.Char0, P.Ink) }, P.Char0, 0);
        Kit.Reg("Panels", "UI_ListRow_Disabled.png", "Lists", "Panel 9-slice", "List row locked / disabled", "Disabled", rowD.B, 2, 2, 2, 2);

        // Header ribbon (window titles, wave banner)
        Kit.Reg("Panels", "UI_Header_Ribbon.png", "Window headers / Wave banner", "Header 3-slice", "Title ribbon over a window top edge; wave/area banner", "Normal", Ribbon(64, P.ForestL, P.ForestM, P.Forest, P.ForestD).B, 12, 0, 12, 0, "Stretch horizontally only; height fixed 18");
        Kit.Reg("Panels", "UI_Header_Ribbon_Red.png", "Game Over / Boss", "Header 3-slice", "Danger ribbon (DEFEATED, boss title)", "Normal", Ribbon(64, P.RedL, P.Red, P.Mix(P.Red, P.RedD, 0.5), P.RedD).B, 12, 0, 12, 0, "Stretch horizontally only");
        Kit.Reg("Panels", "UI_Header_Ribbon_Gold.png", "Level Complete / Reward", "Header 3-slice", "Victory ribbon", "Normal", Ribbon(64, P.GoldW, P.Gold, P.Mix(P.Gold, P.GoldD, 0.5), P.GoldD).B, 12, 0, 12, 0, "Text #2A1E18 on gold");

        // dividers
        var div = new Cv(8, 2); div.HLine(0, 0, 8, P.GoldD); div.HLine(0, 1, 8, P.Ink);
        Kit.Reg("Panels", "UI_Divider_Line.png", "All windows", "Tile", "Horizontal divider (tile)", "Normal", div.B);
        var gem = Img.Grid(new[] { "...k...", "..kGk..", ".kGgdk.", "kGgYgdk", ".kgddk.", "..kdk..", "...k..." });
        Kit.Reg("Panels", "UI_Divider_Gem.png", "All windows", "Ornament", "Centre gem on a divider / title flourish", "Normal", gem);
        var nameplate = Frame(24, 12, new[] { I, new Ring(P.GoldL, P.GoldD), I }, P.WoodM, 1, P.WoodL);
        Kit.Reg("Panels", "UI_NamePlate.png", "NPC Dialogue", "Panel 9-slice", "NPC name tag on the dialogue box", "Normal", nameplate.B, 4, 4, 4, 4);
    }

    public static Cv Ribbon(int w, Color hi, Color face, Color shade, Color dark)
    {
        var cv = new Cv(w, 18);
        // tails
        for (int side = 0; side < 2; side++)
        {
            for (int y = 5; y <= 16; y++)
                for (int x = 0; x <= 8; x++)
                {
                    int dy = (int)Math.Abs(y - 10.5);
                    int notch = dy <= 1 ? 3 : (dy == 2 ? 2 : (dy == 3 ? 1 : 0));
                    if (x < notch) continue;
                    int px = side == 0 ? x : w - 1 - x;
                    bool edge = y == 5 || y == 16 || x == notch;
                    cv.Set(px, y, edge ? P.Ink : (y < 8 ? shade : dark));
                }
            // fold shadow
            for (int k = 0; k < 3; k++) { int px = side == 0 ? 7 + k : w - 8 - k; cv.Set(px, 14 + k > 16 ? 16 : 14 + k, P.Ink); }
        }
        // band
        int x0 = 6, x1 = w - 7;
        for (int x = x0; x <= x1; x++)
        {
            cv.Set(x, 1, P.Ink); cv.Set(x, 2, P.GoldL); cv.Set(x, 3, hi);
            for (int y = 4; y <= 11; y++) cv.Set(x, y, face);
            cv.Set(x, 12, shade); cv.Set(x, 13, P.GoldD); cv.Set(x, 14, P.Ink);
        }
        for (int y = 1; y <= 14; y++) { cv.Set(x0, y, P.Ink); cv.Set(x1, y, P.Ink); }
        cv.Set(x0, 1, Color.Transparent); cv.Set(x1, 1, Color.Transparent);
        return cv;
    }

    // ---------------------------------------------------------------- buttons
    public struct Fam { public Color Hi, Face, Shade, Depth, Text; public Fam(Color hi, Color face, Color shade, Color depth, Color text) { Hi = hi; Face = face; Shade = shade; Depth = depth; Text = text; } }
    public static readonly Fam Primary = new Fam(P.GoldW, P.Gold, P.Mix(P.Gold, P.GoldD, 0.5), P.GoldD, P.TextDark);
    public static readonly Fam Secondary = new Fam(P.WoodL, P.WoodM, P.Wood, P.WoodD, P.GoldL);
    public static readonly Fam Positive = new Fam(P.Mix(P.ForestL, P.GreenL, 0.45), P.ForestL, P.ForestM, P.ForestD, P.TextW);
    public static readonly Fam Danger = new Fam(P.RedL, P.Red, P.Mix(P.Red, P.RedD, 0.5), P.RedD, P.TextW);
    public static readonly Fam Disabled = new Fam(P.StoneL, P.Stone, P.Mix(P.Stone, P.Char2, 0.5), P.Char2, P.TextMute);

    public static Cv Button(int w, int h, Fam f, string state)
    {
        var cv = new Cv(w, h);
        if (state == "Disabled") f = Disabled;
        Color hi = f.Hi, face = f.Face, shade = f.Shade, depth = f.Depth;
        if (state == "Hover") { face = P.Mix(f.Face, f.Hi, 0.4); hi = P.Mix(f.Hi, P.TextW, 0.35); }
        int x0 = 2, x1 = w - 3, y0 = 2, y1 = h - 3;
        if (state == "Pressed")
        {
            Color pf = P.Mix(face, shade, 0.45);
            cv.Fill(x0, y0, x1 - x0 + 1, y1 - y0 + 1, pf);
            cv.HLine(x0, y0, x1 - x0 + 1, depth);
            cv.HLine(x0, y0 + 1, x1 - x0 + 1, shade);
            cv.VLine(x0, y0 + 1, y1 - y0, shade);
            cv.HLine(x0, y1, x1 - x0 + 1, shade);
        }
        else
        {
            cv.Fill(x0, y0, x1 - x0 + 1, y1 - y0 + 1, face);
            cv.HLine(x0, y0, x1 - x0 + 1, hi);
            cv.VLine(x0, y0, y1 - y0 - 2, hi);
            cv.VLine(x1, y0 + 1, y1 - y0 - 2, shade);
            cv.HLine(x0, y1 - 2, x1 - x0 + 1, shade);
            cv.HLine(x0, y1 - 1, x1 - x0 + 1, depth);
            cv.HLine(x0, y1, x1 - x0 + 1, depth);
        }
        // outline with rounded corners
        Color o = P.Ink;
        cv.HLine(2, 1, w - 4, o); cv.HLine(2, h - 2, w - 4, o); cv.VLine(1, 2, h - 4, o); cv.VLine(w - 2, 2, h - 4, o);
        cv.Set(2, 2, o); cv.Set(w - 3, 2, o); cv.Set(2, h - 3, o); cv.Set(w - 3, h - 3, o);
        if (state == "Selected")
        {
            Color g = P.GoldL;
            cv.HLine(2, 0, w - 4, g); cv.HLine(2, h - 1, w - 4, g); cv.VLine(0, 2, h - 4, g); cv.VLine(w - 1, 2, h - 4, g);
            cv.Set(1, 1, g); cv.Set(w - 2, 1, g); cv.Set(1, h - 2, g); cv.Set(w - 2, h - 2, g);
            cv.Set(3, 3, P.GoldW); cv.Set(w - 4, 3, P.GoldW);
        }
        return cv;
    }
    public static readonly string[] States = { "Normal", "Hover", "Pressed", "Selected", "Disabled" };

    public static void BuildButtons()
    {
        var fams = new[] { "Primary", "Secondary", "Positive", "Danger" };
        var famv = new[] { Primary, Secondary, Positive, Danger };
        var use = new[] { "Main call to action: PLAY, CONFIRM, CLAIM, START (text #2A1E18)", "Menu / neutral action (text #E8C870)", "Positive action: CRAFT, HARVEST, ENTER (text #F4F1E8)", "Exit / destructive: EXIT, QUIT, LEAVE (text #F4F1E8)" };
        for (int i = 0; i < fams.Length; i++)
            foreach (var s in States)
                Kit.Reg("Buttons", "UI_Button_" + fams[i] + "_" + s + ".png", "All screens", "Button 9-slice", use[i], s, Button(32, 18, famv[i], s).B, 4, 4, 4, 5,
                    s == "Pressed" ? "Shift label +1 px down when pressed" : "Label centred, 1 px above centre");
        foreach (var s in States)
            Kit.Reg("Buttons", "UI_Button_Icon_" + s + ".png", "HUD / windows", "Button", "Square icon button 22x22 (Settings, Map, Bag, Pause)", s, Button(22, 22, Secondary, s).B, 0, 0, 0, 0, "Icon 16x16 centred (3,2)");
        // close button with baked X (static art)
        foreach (var s in new[] { "Normal", "Pressed" })
        {
            var cb = Button(16, 16, Danger, s);
            var x = Img.Grid(new[] { "w....w", ".w..w.", "..ww..", "..ww..", ".w..w.", "w....w" });
            int oy = s == "Pressed" ? 5 : 4;
            cb.Blit(Img.Silhouette(x, P.RedD, P.RedD), 6, oy + 1); cb.Blit(x, 5, oy);
            Kit.Reg("Buttons", "UI_Button_Close_" + s + ".png", "All windows", "Button", "Window close (top-right corner, overlaps frame by 4 px)", s, cb.B);
        }
        // tabs
        foreach (var sel in new[] { false, true })
        {
            var t = new Cv(32, 16);
            int top = sel ? 0 : 2;
            Color face = sel ? P.Char : P.WoodD;
            t.Fill(1, top + 1, 30, 15 - top, face);
            t.HLine(2, top, 28, P.Ink); t.VLine(0, top + 2, 14 - top, P.Ink); t.VLine(31, top + 2, 14 - top, P.Ink);
            t.Set(1, top + 1, P.Ink); t.Set(30, top + 1, P.Ink);
            t.HLine(2, top + 1, 28, sel ? P.GoldL : P.Wood);
            if (sel) t.HLine(2, top + 2, 28, P.GoldD);
            if (!sel) t.HLine(1, 15, 30, P.Ink);
            Kit.Reg("Buttons", "UI_Tab_" + (sel ? "Selected" : "Normal") + ".png", "Inventory / Quest / Shop / Settings", "Tab 9-slice", "Window tab; selected tab merges into UI_Panel_Main body", sel ? "Selected" : "Normal", t.B, 4, 4, 4, 1, sel ? "Text #E8C870" : "Text #9A958C");
        }
    }

    // ---------------------------------------------------------------- HUD
    public static Cv Round(int d, Func<int, bool, Color> band, double cx = -1, double cy = -1)
    {
        var cv = new Cv(d, d);
        double c = d / 2.0; if (cx < 0) cx = c; if (cy < 0) cy = c;
        for (int j = 0; j < d; j++)
            for (int i = 0; i < d; i++)
            {
                double dx = i + 0.5 - cx, dy = j + 0.5 - cy;
                double dist = Math.Sqrt(dx * dx + dy * dy);
                double fromEdge = c - dist;
                if (fromEdge < 0) continue;
                bool light = (dx + dy) < 0;
                Color col = band((int)Math.Floor(fromEdge), light);
                if (col.A > 0) cv.Set(i, j, col);
            }
        return cv;
    }

    public static void BuildHud()
    {
        var I = new Ring(P.Ink);
        // skill slot 38 (32 icon + 3 px)
        var slot = Frame(38, 38, new[] { I, new Ring(P.WoodL, P.WoodD), I }, P.Char0, 1);
        Kit.Reg("HUD", "UI_SkillSlot_Default.png", "Combat HUD", "Slot", "Skill slot frame; skill icon 32x32 drawn at (3,3)", "Normal", slot.B, 0, 0, 0, 0, "Mobile and PC share this slot");
        var slotS = Frame(38, 38, new[] { new Ring(P.GoldL), new Ring(P.GoldW, P.Gold), I }, P.Char0, 1);
        slotS.Set(0, 0, P.A(P.GoldL, 0));
        Kit.Reg("HUD", "UI_SkillSlot_Selected.png", "Combat HUD", "Slot", "Skill slot highlighted (selected / ready pulse)", "Selected", slotS.B);
        var slotD = Frame(38, 38, new[] { I, new Ring(P.Stone, P.Char2), I }, P.Char0, 1);
        Kit.Reg("HUD", "UI_SkillSlot_Disabled.png", "Combat HUD", "Slot", "Skill slot locked / unavailable (draw icon greyed)", "Disabled", slotD.B);
        var cd = new Cv(32, 32); cd.Fill(0, 0, 32, 32, P.A(P.Ink, 170));
        Kit.Reg("HUD", "UI_SkillSlot_CooldownFill.png", "Combat HUD", "Overlay", "Cooldown overlay: Image Filled Radial360, clockwise, fill = remaining/total", "Overlay", cd.B, 0, 0, 0, 0, "Cooldown seconds drawn by Unity, centred");
        var flash = new Cv(38, 38); flash.Box(0, 0, 38, 38, P.A(P.GoldW, 230)); flash.Box(1, 1, 36, 36, P.A(P.GoldL, 150)); flash.Box(2, 2, 34, 34, P.A(P.GoldL, 70));
        Kit.Reg("HUD", "UI_SkillSlot_ReadyFlash.png", "Combat HUD", "Overlay", "Flash when a skill comes off cooldown (fade 0.25 s)", "Overlay", flash.B);
        var key = Frame(11, 11, new[] { I, new Ring(P.StoneL, P.Char0) }, P.Char2, 1);
        Kit.Reg("HUD", "UI_KeyCap.png", "PC HUD", "Panel 9-slice", "Key label plate (1-5, R, E, I, ESC). Overlaps slot bottom edge", "Normal", key.B, 3, 3, 3, 3, "Key glyph by Unity");

        // attack button (round 56)
        foreach (var pressed in new[] { false, true })
        {
            var atk = Round(56, (b, lt) =>
            {
                if (b == 0) return P.Ink;
                if (b == 1) return pressed ? P.GoldD : (lt ? P.GoldW : P.Gold);
                if (b == 2) return pressed ? P.Mix(P.Gold, P.GoldD, .5) : (lt ? P.GoldL : P.GoldD);
                if (b == 3) return P.Ink;
                if (b <= 5) return lt ? P.WoodM : P.WoodD;
                if (b == 6) return P.Ink;
                return pressed ? P.Char0 : P.Char;
            }, -1, pressed ? 28.5 : -1);
            Kit.Reg("HUD", "UI_AttackButton_" + (pressed ? "Pressed" : "Normal") + ".png", "Mobile Combat HUD", "Button", "Basic attack (R). Class R-combo icon 32x32 masked by UI_Mask_Circle32 at centre", pressed ? "Pressed" : "Normal", atk.B);
        }
        var mask = Round(32, (b, lt) => P.TextW);
        Kit.Reg("HUD", "UI_Mask_Circle32.png", "Mobile Combat HUD", "Mask", "Circle mask for the attack icon (Unity Mask / SpriteMask)", "Mask", mask.B);

        // joystick
        var jb = Round(64, (b, lt) =>
        {
            if (b == 0) return P.A(P.Ink, 150);
            if (b == 1) return P.A(lt ? P.StoneL : P.Stone, 170);
            if (b == 2) return P.A(P.Ink, 120);
            return P.A(P.Char0, 95);
        });
        var chev = new[] { new Point(32, 7), new Point(32, 56), new Point(7, 32), new Point(56, 32) };
        // direction ticks
        for (int k = -2; k <= 2; k++) { jb.Set(32 + k, 7 + Math.Abs(k), P.A(P.GoldL, 200)); jb.Set(32 + k, 56 - Math.Abs(k), P.A(P.GoldL, 200)); jb.Set(7 + Math.Abs(k), 32 + k, P.A(P.GoldL, 200)); jb.Set(56 - Math.Abs(k), 32 + k, P.A(P.GoldL, 200)); }
        Kit.Reg("HUD", "UI_Joystick_Base.png", "Mobile HUD", "Joystick", "Virtual joystick ring (TouchMovePad). Appears under the thumb", "Normal", jb.B);
        var kn = Round(28, (b, lt) =>
        {
            if (b == 0) return P.Ink;
            if (b == 1) return lt ? P.GoldL : P.GoldD;
            if (b == 2) return P.Ink;
            if (b <= 4) return lt ? P.StoneL : P.Stone;
            return lt ? P.Mix(P.StoneL, P.TextW, 0.3) : P.StoneL;
        });
        Kit.Reg("HUD", "UI_Joystick_Knob.png", "Mobile HUD", "Joystick", "Joystick thumb knob", "Normal", kn.B);

        // interact (round 40)
        foreach (var pressed in new[] { false, true })
        {
            var it = Round(40, (b, lt) =>
            {
                if (b == 0) return P.Ink;
                if (b == 1) return pressed ? P.ForestM : (lt ? P.GreenL : P.ForestL);
                if (b == 2) return pressed ? P.ForestD : (lt ? P.ForestL : P.ForestM);
                if (b == 3) return P.Ink;
                return pressed ? P.A(P.Char0, 240) : P.A(P.Char, 235);
            }, -1, pressed ? 20.5 : -1);
            Kit.Reg("HUD", "UI_InteractButton_" + (pressed ? "Pressed" : "Normal") + ".png", "Village / World HUD", "Button", "Contextual interact (E on PC). Shows the NPC/service icon 16x16 + short label", pressed ? "Pressed" : "Normal", it.B);
        }

        // portrait frame + back
        var pf = Frame(38, 38, new[] { I, new Ring(P.GoldL, P.GoldD), I }, Color.Transparent, 2);
        Rivet(pf, 1, 1);
        Kit.Reg("HUD", "UI_PortraitFrame.png", "HUD / Hero Info / Dialogue", "Frame", "Hero / NPC portrait frame; portrait 32x32 at (3,3) under the frame", "Normal", pf.B);
        var pb = new Cv(32, 32);
        for (int y = 0; y < 32; y++) pb.HLine(0, y, 32, P.Mix(P.ForestM, P.ForestD, y / 31.0));
        Kit.Reg("HUD", "UI_PortraitBack.png", "HUD / Hero Info / Dialogue", "Background", "Portrait background", "Normal", pb.B);
        var lb = Round(16, (b, lt) => b == 0 ? P.Ink : (b == 1 ? (lt ? P.GoldL : P.GoldD) : (b == 2 ? P.Ink : P.WoodD)));
        Kit.Reg("HUD", "UI_LevelBadge.png", "HUD", "Badge", "Hero level badge (number by Unity, gold text)", "Normal", lb.B);

        // bars
        var bf = Frame(24, 12, new[] { I, new Ring(P.WoodL, P.WoodD), I }, P.Char0, 1);
        Kit.Reg("Bars", "UI_Bar_Frame.png", "HUD", "Bar 9-slice", "Hero HP / mana / progress bar frame; fill area inset 3 px (6 px tall)", "Normal", bf.B, 3, 3, 3, 3);
        var bt = Frame(24, 6, new[] { I, new Ring(P.Char2, P.Char0) }, P.Char0, 1);
        Kit.Reg("Bars", "UI_Bar_Frame_Thin.png", "HUD", "Bar 9-slice", "EXP bar / small progress (fill 2 px tall, inset 2)", "Normal", bt.B, 2, 2, 2, 2);
        var eb = Frame(20, 4, new[] { I }, P.Char0, 0);
        Kit.Reg("Bars", "UI_EnemyBar_Frame.png", "World labels", "Bar 9-slice", "Enemy HP under 'Name Lv.N' (shown after first damage)", "Normal", eb.B, 1, 1, 1, 1);
        Kit.Reg("Bars", "UI_BossBar_Frame.png", "Boss HUD", "Bar 3-slice", "Screen boss bar; fill area x16..w-17, y4..11", "Normal", BossFrame(128).B, 16, 0, 16, 0, "Stretch horizontally; name above in TextW");

        Fill("UI_Bar_Fill_HP.png", "Hero HP", 6, P.RedL, P.Red, P.RedD);
        Fill("UI_Bar_Fill_Mana.png", "Mana / absorb (reserved, Selene shield)", 6, P.BlueL, P.Blue, P.BlueD);
        Fill("UI_Bar_Fill_EXP.png", "EXP (thin)", 2, P.GreenL, P.Green, P.GreenD);
        Fill("UI_Bar_Fill_Progress.png", "Gather / craft / loading progress", 6, P.GoldW, P.Gold, P.GoldD);
        Fill("UI_Bar_Fill_Ghost.png", "Delayed damage trail behind HP", 6, P.ParchL, P.Parch, P.ParchD);
        Fill("UI_Bar_Fill_Boss.png", "Boss HP", 8, P.RedL, P.Red, P.RedD);
        Fill("UI_Bar_Fill_Enemy.png", "Enemy HP (2 px)", 2, P.RedL, P.Red, P.RedD);
        Fill("UI_Bar_Fill_Shield.png", "Shield / absorb overlay (Bulwark, Mana Shield)", 6, P.TextW, P.TextBlue, P.BlueL);

        // inventory slots
        var sNormal = Frame(22, 22, new[] { new Ring(P.WoodD, P.WoodM), new Ring(P.Ink, P.Char2) }, P.Char0, 1);
        var sHover = Frame(22, 22, new[] { new Ring(P.GoldD, P.Gold), new Ring(P.Ink, P.Char2) }, P.Char0, 1);
        var sSel = Frame(22, 22, new[] { new Ring(P.GoldL), new Ring(P.GoldW, P.Gold) }, P.Mix(P.Char0, P.WoodD, 0.6), 1);
        sSel.Set(2, 2, P.GoldW); sSel.Set(19, 2, P.GoldW); sSel.Set(2, 19, P.GoldW); sSel.Set(19, 19, P.GoldW);
        var sDis = Frame(22, 22, new[] { new Ring(P.Char, P.Char2), new Ring(P.Ink, P.Char) }, P.Char, 1);
        for (int k = 3; k < 19; k += 3) for (int j = 3; j < 19; j++) { int i = j + k - 3; if (i < 19) sDis.Set(i, j, P.Char2); }
        Kit.Reg("Slots", "UI_Inventory_Slot.png", "Inventory / Storage / Shop", "Slot", "Item slot; item icon 16x16 at (3,3); quantity bottom-right by Unity", "Normal", sNormal.B);
        Kit.Reg("Slots", "UI_Inventory_Slot_Hover.png", "Inventory / Storage / Shop", "Slot", "Item slot hover / focus", "Hover", sHover.B);
        Kit.Reg("Slots", "UI_Inventory_Slot_Selected.png", "Inventory / Storage / Shop", "Slot", "Item slot selected", "Selected", sSel.B);
        Kit.Reg("Slots", "UI_Inventory_Slot_Disabled.png", "Inventory / Storage / Shop", "Slot", "Locked / unavailable slot", "Disabled", sDis.B);
        var eq = Frame(26, 26, new[] { I, new Ring(P.WoodL, P.WoodD), new Ring(P.Ink, P.Char2) }, P.Char0, 2);
        var eqS = Frame(26, 26, new[] { new Ring(P.GoldL), new Ring(P.GoldW, P.Gold), new Ring(P.Ink, P.Char2) }, P.Mix(P.Char0, P.WoodD, 0.6), 2);
        var eqL = Frame(26, 26, new[] { I, new Ring(P.Stone, P.Char2), new Ring(P.Ink, P.Char) }, P.Char, 2);
        Kit.Reg("Slots", "UI_Equip_Slot.png", "Equipment", "Slot", "Equipment slot; ghost or item icon at (5,5)", "Normal", eq.B);
        Kit.Reg("Slots", "UI_Equip_Slot_Selected.png", "Equipment", "Slot", "Equipment slot selected", "Selected", eqS.B);
        Kit.Reg("Slots", "UI_Equip_Slot_Locked.png", "Equipment", "Slot", "Equipment slot not in the game yet (concept)", "Disabled", eqL.B);

        // controls: scroll, slider, toggle, checkbox
        var st = Frame(6, 16, new[] { I }, P.Char0, 0);
        Kit.Reg("Controls", "UI_Scroll_Track.png", "Lists", "Bar 9-slice", "Scrollbar track", "Normal", st.B, 2, 2, 2, 2);
        var sth = Frame(6, 10, new[] { I, new Ring(P.WoodL, P.WoodD) }, P.WoodM, 0);
        Kit.Reg("Controls", "UI_Scroll_Thumb.png", "Lists", "Bar 9-slice", "Scrollbar handle", "Normal", sth.B, 2, 3, 2, 3);
        var sl = Frame(32, 6, new[] { I, new Ring(P.Char0, P.Char2) }, P.Char0, 1);
        Kit.Reg("Controls", "UI_Slider_Track.png", "Settings", "Bar 9-slice", "Slider track (fill area inset 2)", "Normal", sl.B, 3, 2, 3, 2);
        Fill("UI_Slider_Fill.png", "Slider fill", 2, P.GoldL, P.Gold, P.GoldD);
        var knob = Img.Grid(new[] { "..kkkkkk..", ".kGGGGggk.", "kGGYGGgddk", "kGGGGggddk", "kGGgggggdk", "kGggggggdk", "kgggggggdk", "kggggggddk", ".kddddddk.", "..kkkkkk.." });
        Kit.Reg("Controls", "UI_Slider_Knob.png", "Settings", "Handle", "Slider handle", "Normal", knob);
        foreach (var on in new[] { false, true })
        {
            var tg = new Cv(26, 14);
            var track = Frame(26, 12, new[] { I, new Ring(on ? P.ForestD : P.Char0, on ? P.ForestL : P.Char2) }, on ? P.ForestM : P.Char0, 2);
            tg.Blit(track.B, 0, 1);
            var k = Round(14, (b, lt) => b == 0 ? P.Ink : (b == 1 ? (on ? (lt ? P.GoldW : P.GoldD) : (lt ? P.StoneL : P.Char2)) : (on ? (lt ? P.GoldL : P.Gold) : (lt ? P.StoneL : P.Stone))));
            tg.Blit(k.B, on ? 12 : 0, 0);
            Kit.Reg("Controls", "UI_Toggle_" + (on ? "On" : "Off") + ".png", "Settings", "Toggle", "On/off switch", on ? "Selected" : "Normal", tg.B);
            var cb = Frame(12, 12, new[] { I, new Ring(P.Char2, P.Char0) }, P.Char0, 1);
            if (on) cb.Blit(Img.Grid(new[] { "......L", ".....Ll", "L...Ll.", "lL.Ll..", ".lLl...", "..l...." }), 3, 3);
            Kit.Reg("Controls", "UI_Checkbox_" + (on ? "On" : "Off") + ".png", "Settings / lists", "Toggle", "Checkbox", on ? "Selected" : "Normal", cb.B);
        }
        var badge = Img.Grid(new[] { "..kkkkk..", ".kRRRrrk.", "kRRwwRrqk", "kRRwwrrqk", "kRrwwrrqk", "kRrrrrrqk", "kRrwwrrqk", ".kqqqqqk.", "..kkkkk.." });
        Kit.Reg("Controls", "UI_Badge_Alert.png", "HUD / menus", "Badge", "New / claimable alert dot on a button corner", "Normal", badge);
        var cur = Img.Grid(new[] { "kk.....", "kGk....", "kGgk...", "kGggk..", "kGgggk.", "kGggdk.", "kGgdk..", "kgdk...", "kdk....", "kk....." });
        Kit.Reg("Controls", "UI_Cursor_Arrow.png", "Menus", "Pointer", "Menu focus arrow (keyboard / gamepad focus)", "Normal", cur);
        var next = Img.Grid(new[] { "kkkkkkkkk", "kGGGGGgdk", ".kGggddk.", "..kgddk..", "...kdk...", "....k...." });
        Kit.Reg("Controls", "UI_Dialogue_Next.png", "NPC Dialogue", "Pointer", "'More text' blinking arrow, bottom-right of the dialogue box", "Normal", next);

        // stars
        Kit.Reg("HUD", "UI_Star_Large_Filled.png", "Level Complete / Dungeon Select", "Icon", "Earned star", "Normal", Star(24, true));
        Kit.Reg("HUD", "UI_Star_Large_Empty.png", "Level Complete / Dungeon Select", "Icon", "Missing star", "Disabled", Star(24, false));

        // minimap frame
        var mm = Frame(80, 80, new[] { I, new Ring(P.WoodL, P.WoodD), new Ring(P.WoodM, P.WoodD), I, new Ring(P.GoldL, P.GoldD), I }, Color.Transparent, 2);
        var npt = Img.Grid(new[] { "...k...", "..kRk..", ".kRrqk.", "kRrrrqk", "kkkkkkk" });
        mm.Blit(npt, 36, 0);
        Kit.Reg("HUD", "UI_Minimap_Frame.png", "Minimap", "Frame", "Minimap frame (map RenderTexture 68x68 at (6,6), north pointer on top)", "Normal", mm.B, 0, 0, 0, 0, "Concept: game has a zone label only today");
        var mmb = new Cv(68, 68); mmb.Fill(0, 0, 68, 68, P.A(P.ForestD, 220));
        Kit.Reg("HUD", "UI_Minimap_Back.png", "Minimap", "Background", "Minimap background (until a map texture exists)", "Normal", mmb.B);
    }

    public static Cv BossFrame(int w)
    {
        var cv = new Cv(w, 16);
        // bar body
        int x0 = 6, x1 = w - 7;
        for (int x = x0; x <= x1; x++)
        {
            cv.Set(x, 1, P.Ink); cv.Set(x, 2, P.GoldL); cv.Set(x, 3, P.Ink);
            for (int y = 4; y <= 11; y++) cv.Set(x, y, P.Char0);
            cv.Set(x, 12, P.Ink); cv.Set(x, 13, P.GoldD); cv.Set(x, 14, P.Ink);
        }
        // diamond caps
        foreach (var cx in new[] { 8.0, w - 8.0 })
        {
            for (int j = 0; j < 16; j++)
                for (int i = (int)cx - 9; i <= (int)cx + 9; i++)
                {
                    double dx = i + 0.5 - cx, dy = j + 0.5 - 8.0;
                    double m = Math.Abs(dx) + Math.Abs(dy);
                    if (m > 8) continue;
                    Color c;
                    bool lt = (dx + dy) < 0;
                    if (m > 7) c = P.Ink;
                    else if (m > 5.5) c = lt ? P.GoldL : P.GoldD;
                    else if (m > 4.5) c = P.Ink;
                    else if (m > 1.5) c = lt ? P.Red : P.RedD;
                    else c = P.RedL;
                    cv.Set(i, j, c);
                }
        }
        return cv;
    }

    static void Fill(string file, string use, int h, Color hi, Color face, Color dark)
    {
        var cv = new Cv(4, h);
        for (int y = 0; y < h; y++)
        {
            Color c = y == 0 ? hi : (y >= h - Math.Max(1, h / 3) ? dark : face);
            if (h == 2) c = y == 0 ? hi : face;
            cv.HLine(0, y, 4, c);
        }
        Kit.Reg("Bars", file, "HUD", "Bar fill", use + ". Image Type Filled Horizontal (or Sliced width)", "Normal", cv.B, 0, 0, 0, 0, "Columns identical: stretches without distortion");
    }

    public static Bitmap Star(int s, bool filled)
    {
        var cv = new Cv(s, s);
        double cx = s / 2.0, cy = s / 2.0 + s * 0.04, R = s * 0.48, r = s * 0.21;
        var pts = new PointF[10];
        for (int i = 0; i < 10; i++)
        {
            double a = -Math.PI / 2 + i * Math.PI / 5;
            double rr = i % 2 == 0 ? R : r;
            pts[i] = new PointF((float)(cx + rr * Math.Cos(a)), (float)(cy + rr * Math.Sin(a)));
        }
        var inside = new bool[s, s];
        for (int j = 0; j < s; j++) for (int i = 0; i < s; i++) inside[i, j] = InPoly(pts, i + 0.5, j + 0.5);
        for (int j = 0; j < s; j++)
            for (int i = 0; i < s; i++)
            {
                if (!inside[i, j]) continue;
                bool edge = i == 0 || j == 0 || i == s - 1 || j == s - 1 || !inside[i - 1, j] || !inside[i + 1, j] || !inside[i, j - 1] || !inside[i, j + 1];
                Color c;
                if (edge) c = P.Ink;
                else if (filled) c = (j < cy - 1) ? (i < cx ? P.GoldW : P.GoldL) : (i < cx ? P.Gold : P.GoldD);
                else c = (j < cy - 1) ? P.Stone : P.Char2;
                cv.Set(i, j, c);
            }
        return cv.B;
    }
    static bool InPoly(PointF[] p, double x, double y)
    {
        bool c = false;
        for (int i = 0, j = p.Length - 1; i < p.Length; j = i++)
            if (((p[i].Y > y) != (p[j].Y > y)) && (x < (p[j].X - p[i].X) * (y - p[i].Y) / (p[j].Y - p[i].Y) + p[i].X)) c = !c;
        return c;
    }
}
