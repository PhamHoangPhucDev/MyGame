using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;

/// <summary>World backdrops for mockups, built from the project's own sprites at world pixels (PPU 16).</summary>
public static class World
{
    const string Tiles = "Assets/Art/Rebuild/Tiles/";
    const string Camp = "Assets/Art/GoblinCampPhase4/";
    const string Env = "Assets/Art/Environment/";
    const string Mon = "Assets/Art/Rebuild/R2C/Monsters/";
    public const string Heroes = "Assets/Art/Rebuild/R2C/";

    class Spr { public Bitmap B; public int X, Y, Px, Py; public bool Flip; public float Alpha = 1f; }
    static readonly List<Spr> _list = new List<Spr>();

    static double Hash(int x, int y, int s)
    {
        unchecked
        {
            int h = x * 374761393 + y * 668265263 + s * 982451653;
            h = (h ^ (h >> 13)) * 1274126177;
            return ((h ^ (h >> 16)) & 0xFFFF) / 65535.0;
        }
    }
    static double Noise(double x, double y, int s)
    {
        int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y);
        double fx = x - xi, fy = y - yi;
        double a = Hash(xi, yi, s), b = Hash(xi + 1, yi, s), c = Hash(xi, yi + 1, s), d = Hash(xi + 1, yi + 1, s);
        fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy;
    }

    public struct Blob { public double X, Y, Rx, Ry; public Blob(double x, double y, double rx, double ry) { X = x; Y = y; Rx = rx; Ry = ry; } }

    public static Cv Ground(int w, int h, int seed, Blob[] dirt)
    {
        var cv = new Cv(w, h);
        var g0 = Img.Load(Tiles + "GrassSoft.png"); var g1 = Img.Load(Tiles + "GrassSoft_V1.png"); var g2 = Img.Load(Tiles + "GrassSoft_V2.png");
        for (int ty = 0; ty < h; ty += 16)
            for (int tx = 0; tx < w; tx += 16)
            {
                double r = Hash(tx / 16, ty / 16, seed);
                cv.Blit(r < 0.6 ? g0 : (r < 0.8 ? g1 : g2), tx, ty);
            }
        if (dirt == null || dirt.Length == 0) return cv;
        var dt = Img.Load(Tiles + "Dirt.png");
        var mask = new bool[w, h];
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                double best = -9;
                foreach (var b in dirt)
                {
                    double dx = (x - b.X) / b.Rx, dy = (y - b.Y) / b.Ry;
                    best = Math.Max(best, 1 - Math.Sqrt(dx * dx + dy * dy));
                }
                double n = Noise(x / 5.0, y / 5.0, seed) * 0.22 + Noise(x / 2.0, y / 2.0, seed + 7) * 0.08;
                mask[x, y] = best + n - 0.15 > 0;
            }
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                if (!mask[x, y]) continue;
                bool edge = x == 0 || y == 0 || x == w - 1 || y == h - 1 || !mask[x - 1, y] || !mask[x + 1, y] || !mask[x, y - 1] || !mask[x, y + 1];
                Color c = dt.GetPixel(x % 16, y % 16);
                if (edge) c = P.Mix(c, P.H("5A6A3A"), 0.55);
                else if (y > 0 && !mask[x, y - 1 < 0 ? 0 : y - 1]) c = P.Mix(c, P.Ink, 0.15);
                cv.Set(x, y, c);
            }
        return cv;
    }

    public static void Clear() { _list.Clear(); }
    /// <summary>Queue a sprite with its pivot (px,py) placed at world (x,y). Drawn sorted by y.</summary>
    public static void Put(Bitmap b, int x, int y, int px, int py, bool flip = false, float alpha = 1f) { _list.Add(new Spr { B = b, X = x, Y = y, Px = px, Py = py, Flip = flip, Alpha = alpha }); }
    public static void Prop(string rel, int x, int y) { var b = Img.Load(rel); Put(b, x, y, b.Width / 2, b.Height - 1); }
    public static void Tree(int x, int y, char kind) { Shadow(x, y, 14, 4); Prop(Camp + "Tree_Forest_" + kind + ".png", x, y); }
    public static void Tent(int x, int y) { Shadow(x, y - 2, 24, 5); Prop(Env + "Tent_GoblinHorn.png", x, y); }
    public static void Brazier(int x, int y) { Prop(Camp + "Brazier_Bowl.png", x, y); }
    public static void Rock(int x, int y, bool large) { Prop(Camp + (large ? "Rock_Large.png" : "Rock_Small.png"), x, y); }
    public static void Bush(int x, int y, char k) { Prop(Env + "Bush" + k + ".png", x, y); }
    public static void Crates(int x, int y) { Prop(Camp + "Wooden_Crate_Stack.png", x, y); }
    public static void Bones(int x, int y) { Prop(Camp + "Bone_Pile.png", x, y); }
    public static void Flag(int x, int y) { Prop(Camp + "Goblin_Flag_Tattered.png", x, y); }
    public static void Barrel(int x, int y) { Prop(Env + "Barrel.png", x, y); }

    static readonly List<int[]> _shadows = new List<int[]>();
    public static void Shadow(int x, int y, int rx, int ry) { _shadows.Add(new[] { x, y, rx, ry }); }

    public static Bitmap HeroFrame(string cls, int col, int row) { return Img.Cell(Heroes + cls + "/" + cls + "_Sheet.png", 32, 32, col, row); }
    public static void Hero(string cls, int x, int y, int row = 0, int col = 0, bool flip = false)
    {
        Shadow(x, y, 5, 2);
        Put(HeroFrame(cls, col, row), x, y, 16, 30, flip);
    }
    public static void Monster(string name, int x, int y, int row = 0, int col = 0, bool flip = false)
    {
        string file = name == "Cyclops" ? Mon + "Cyclops/Cyclops_Sheet.png" : Mon + name + "_" + name + "_Sheet.png";
        int cell = name == "Cyclops" ? 96 : (name == "GobriderWarlord" ? 64 : 48);
        int px = cell / 2, py = name == "Cyclops" ? 90 : (int)(cell * 0.79);
        Shadow(x, y, cell / 6, 2);
        Put(Img.Cell(file, cell, cell, col, row), x, y, px, py, flip);
    }
    public static void Telegraph(int x, int y, int frame)
    {
        var b = Img.Cell(Mon + "Cyclops/Cyclops_Telegraph.png", 128, 128, frame, 0);
        _decals.Add(new Spr { B = b, X = x, Y = y, Px = 64, Py = 64 });
    }
    static readonly List<Spr> _decals = new List<Spr>();

    public static void Render(Cv cv)
    {
        foreach (var d in _decals) cv.Blit(d.B, d.X - d.Px, d.Y - d.Py);
        foreach (var s in _shadows)
            for (int j = -s[3]; j <= s[3]; j++)
                for (int i = -s[2]; i <= s[2]; i++)
                {
                    double e = (i * i) / (double)(s[2] * s[2]) + (j * j) / (double)(s[3] * s[3]);
                    if (e <= 1) cv.Px(s[0] + i, s[1] + j, P.A(P.H("10180F"), 70));
                }
        foreach (var s in _list.OrderBy(q => q.Y))
        {
            if (s.Flip) cv.BlitFlip(s.B, s.X - (s.B.Width - s.Px), s.Y - s.Py);
            else cv.Blit(s.B, s.X - s.Px, s.Y - s.Py, 1, s.Alpha);
        }
        _list.Clear(); _shadows.Clear(); _decals.Clear();
    }
}

/// <summary>One mockup: world layer (x4) + UI layer (x2 portrait / x3 landscape).</summary>
public class Shot
{
    public bool Portrait;
    public Cv World, Ui;
    public int UiK { get { return Portrait ? 2 : 3; } }
    public int UW { get { return Ui.W; } }
    public int UH { get { return Ui.Ht; } }
    public string Name, Title;

    public Shot(string name, string title, bool portrait)
    {
        Name = name; Title = title; Portrait = portrait;
        World = portrait ? new Cv(144, 256) : new Cv(480, 270);
        Ui = portrait ? new Cv(288, 512) : new Cv(640, 360);
    }
    /// <summary>World pixel -> UI pixel.</summary>
    public int WX(int x) { return Portrait ? x * 2 : (int)Math.Round(x * 4 / 3.0); }
    public int WY(int y) { return Portrait ? y * 2 : (int)Math.Round(y * 4 / 3.0); }

    public Cv Compose()
    {
        int sw = Portrait ? 576 : 1920, sh = Portrait ? 1024 : 1080;
        var o = new Cv(sw, sh);
        o.Clear(P.Char0);
        o.Blit(World.B, 0, 0, 4);
        o.Blit(Ui.B, 0, 0, UiK);
        return o;
    }

    // ------------------------------------------------------------- drawing helpers (UI px)
    public void Panel(string file, int x, int y, int w, int h)
    {
        var a = Kit.Asset(file);
        if (a.L + a.T + a.R + a.B == 0) Ui.Blit(a.Img, x, y);
        else Ui.Slice(a.Img, x, y, w, h, a.L, a.T, a.R, a.B);
    }
    public void Window(int x, int y, int w, int h, string title, bool close = true, string ribbon = "UI_Header_Ribbon.png")
    {
        Panel("UI_Panel_Main.png", x, y, w, h);
        if (title != null)
        {
            int rw = Math.Max(Txt.Width(title, 1) + 36, 96);
            Panel(ribbon, x + (w - rw) / 2, y - 7, rw, 18);
            Txt.Draw(Ui, x + w / 2, y - 3, title, ribbon.Contains("Gold") ? P.TextDark : P.GoldW, 1, 1, !ribbon.Contains("Gold"));
        }
        if (close) Ui.Blit(Kit.Get("UI_Button_Close_Normal.png"), x + w - 13, y - 3);
    }
    public void T(int x, int y, string s, Color c, int scale = 1, int align = 0, bool shadow = true) { Txt.Draw(Ui, x, y, s, c, scale, align, shadow); }
    public void TO(int x, int y, string s, Color c, int scale = 1, int align = 0) { Txt.Outlined(Ui, x, y, s, c, scale, align); }
    public void Icon(string name, int x, int y, int k = 1) { Ui.Blit(Icons.Get(name), x, y, k); }
    public void Pic(Bitmap b, int x, int y, int k = 1, float alpha = 1f) { Ui.Blit(b, x, y, k, alpha); }

    public void Btn(int x, int y, int w, int h, string fam, string state, string label, string icon = null)
    {
        var a = Kit.Asset("UI_Button_" + fam + "_" + state + ".png");
        Ui.Slice(a.Img, x, y, w, h, a.L, a.T, a.R, a.B);
        Color tc = state == "Disabled" ? P.TextMute : (fam == "Primary" ? P.TextDark : (fam == "Secondary" ? P.GoldL : P.TextW));
        int dy = state == "Pressed" ? 1 : 0;
        int tw = label == null ? 0 : Txt.Width(label, 1);
        int iw = icon == null ? 0 : 18;
        int cx = x + w / 2 - (tw + iw) / 2;
        int ty = y + (h - 8) / 2 - 1 + dy;
        if (icon != null) { var ib = Icons.Get(icon); if (state == "Disabled") ib = global::Img.Gray(ib, 0.7); Ui.Blit(ib, cx, y + (h - 16) / 2 - 1 + dy); }
        if (label != null) Txt.Draw(Ui, cx + iw, ty, label, tc, 1, 0, fam != "Primary" && state != "Disabled", fam == "Primary" ? (Color?)P.GoldL : null);
    }
    public void IconBtn(int x, int y, string icon, string state = "Normal", bool badge = false)
    {
        Ui.Blit(Kit.Get("UI_Button_Icon_" + state + ".png"), x, y);
        Ui.Blit(Icons.Get(icon), x + 3, y + 2 + (state == "Pressed" ? 1 : 0));
        if (badge) Ui.Blit(Kit.Get("UI_Badge_Alert.png"), x + 15, y - 3);
    }
    public void Bar(int x, int y, int w, string fill, double frac, bool thin = false, string text = null)
    {
        var f = Kit.Asset(thin ? "UI_Bar_Frame_Thin.png" : "UI_Bar_Frame.png");
        Ui.Slice(f.Img, x, y, w, thin ? 6 : 12, f.L, f.T, f.R, f.B);
        int inset = thin ? 2 : 3;
        int fw = (int)Math.Round((w - inset * 2) * frac);
        var fb = Kit.Get("UI_Bar_Fill_" + fill + ".png");
        for (int i = 0; i < fw; i++) Ui.Blit(Crop1(fb), x + inset + i, y + inset);
        if (text != null) TO(x + w / 2, y + 2, text, P.TextW, 1, 1);
    }
    static readonly Dictionary<Bitmap, Bitmap> _col = new Dictionary<Bitmap, Bitmap>();
    static Bitmap Crop1(Bitmap b)
    {
        Bitmap c;
        if (_col.TryGetValue(b, out c)) return c;
        c = new Bitmap(1, b.Height);
        for (int j = 0; j < b.Height; j++) c.SetPixel(0, j, b.GetPixel(0, j));
        _col[b] = c; return c;
    }
    public void BossBar(int x, int y, int w, double frac)
    {
        var a = Kit.Asset("UI_BossBar_Frame.png");
        Ui.Slice(a.Img, x, y, w, 16, a.L, a.T, a.R, a.B);
        int fw = (int)((w - 32) * frac);
        var fb = Crop1(Kit.Get("UI_Bar_Fill_Boss.png"));
        for (int i = 0; i < fw; i++) Ui.Blit(fb, x + 16 + i, y + 4);
    }
    public void Ribbon(int cx, int y, string text, string file = "UI_Header_Ribbon.png", int minW = 96)
    {
        int rw = Math.Max(Txt.Width(text, 1) + 36, minW);
        Panel(file, cx - rw / 2, y, rw, 18);
        Txt.Draw(Ui, cx, y + 4, text, file.Contains("Gold") ? P.TextDark : P.GoldW, 1, 1, !file.Contains("Gold"));
    }
    public void Skill(int x, int y, Bitmap icon, string state = "Default", double cd = 0, string cdText = null, string key = null)
    {
        Ui.Blit(Kit.Get("UI_SkillSlot_" + state + ".png"), x, y);
        if (icon != null) Ui.Blit(state == "Disabled" ? global::Img.Gray(icon, 0.6) : icon, x + 3, y + 3);
        if (cd > 0)
        {
            // radial wedge preview of the Filled Radial overlay
            for (int j = 0; j < 32; j++)
                for (int i = 0; i < 32; i++)
                {
                    double a = Math.Atan2(i + 0.5 - 16, -(j + 0.5 - 16));
                    if (a < 0) a += 2 * Math.PI;
                    if (a / (2 * Math.PI) >= 1 - cd) Ui.Px(x + 3 + i, y + 3 + j, P.A(P.Ink, 170));
                }
            if (cdText != null) TO(x + 19, y + 15, cdText, P.TextW, 1, 1);
        }
        if (key != null)
        {
            int kw = Math.Max(11, Txt.Width(key, 1) + 5);
            var kc = Kit.Asset("UI_KeyCap.png");
            Ui.Slice(kc.Img, x + 19 - kw / 2, y + 32, kw, 11, 3, 3, 3, 3);
            Txt.Draw(Ui, x + 19, y + 34, key, P.GoldL, 1, 1, false);
        }
    }
    public void Slot(int x, int y, string iconName, int qty = 0, string state = "")
    {
        Ui.Blit(Kit.Get("UI_Inventory_Slot" + (state == "" ? "" : "_" + state) + ".png"), x, y);
        if (iconName != null) Ui.Blit(Icons.Get(iconName), x + 3, y + 3);
        if (qty > 1) TO(x + 21, y + 14, qty.ToString(), P.TextW, 1, 2);
    }
    /// <summary>World label: "Name Lv.N" + thin HP bar (only when damaged), at a world position (feet).</summary>
    public void MobLabel(int wx, int wy, int headPx, string text, double hp = 1, Color? color = null)
    {
        int ux = WX(wx), uy = WY(wy - headPx) - 13;
        int half = Txt.Width(text, 1) / 2 + 3;
        ux = Math.Max(half, Math.Min(UW - half, ux));
        TO(ux, uy, text, color.HasValue ? color.Value : P.TextW, 1, 1);
        if (hp < 1)
        {
            var f = Kit.Asset("UI_EnemyBar_Frame.png");
            Ui.Slice(f.Img, ux - 14, uy + 10, 28, 4, 1, 1, 1, 1);
            var fb = Crop1(Kit.Get("UI_Bar_Fill_Enemy.png"));
            for (int i = 0; i < (int)(26 * hp); i++) Ui.Blit(fb, ux - 13 + i, uy + 11);
        }
    }
    public void Dim(int a = 150) { Ui.Fill(0, 0, UW, UH, P.A(P.Ink, a)); }
    public void PortraitAt(Bitmap face, int x, int y)
    {
        Ui.Blit(Kit.Get("UI_PortraitBack.png"), x + 3, y + 3);
        if (face != null) Ui.Blit(face, x + 3, y + 3);
        Ui.Blit(Kit.Get("UI_PortraitFrame.png"), x, y);
    }
    public void ConceptTag(int x, int y, string text = "CONCEPT")
    {
        int w = Txt.Width(text, 1) + 10;
        Ui.Fill(x, y, w, 12, P.A(P.PurpleD, 235)); Ui.Box(x, y, w, 12, P.Purple);
        Txt.Draw(Ui, x + 5, y + 2, text, P.PurpleL, 1, 0, false);
    }
}
