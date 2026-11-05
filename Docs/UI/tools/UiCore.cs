using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Drawing.Text;
using System.IO;
using System.Linq;

// GAME01 UI kit — core: palette, pixel canvas, pixel font (Press Start 2P, 1-bit), image IO.
// All UI art is drawn at 1 UI pixel. Portrait mockups show it at x2 (288x512 -> 576x1024),
// landscape at x3 (640x360 -> 1920x1080). The world behind the HUD is drawn at x4 (PPC 144x256 / 480x270).
public static class P
{
    public static Color H(string hex) { return Color.FromArgb(255, Convert.ToInt32(hex.Substring(0, 2), 16), Convert.ToInt32(hex.Substring(2, 2), 16), Convert.ToInt32(hex.Substring(4, 2), 16)); }
    public static Color A(Color c, int a) { return Color.FromArgb(a, c.R, c.G, c.B); }
    public static Color Mix(Color a, Color b, double t) { return Color.FromArgb(255, (int)(a.R + (b.R - a.R) * t), (int)(a.G + (b.G - a.G) * t), (int)(a.B + (b.B - a.B) * t)); }
    public static string Hex(Color c) { return "#" + c.R.ToString("X2") + c.G.ToString("X2") + c.B.ToString("X2"); }

    // Foundations
    public static readonly Color Ink = H("14100E");
    public static readonly Color WoodD = H("2A1E18"), Wood = H("3D2B21"), WoodM = H("5A3F2C"), WoodL = H("8A6040");
    public static readonly Color Char0 = H("16171B"), Char = H("1E1F24"), Char2 = H("2C2E35"), Stone = H("4A4C55"), StoneL = H("6E707A");
    public static readonly Color ForestD = H("18291F"), Forest = H("223D2C"), ForestM = H("2F5A3E"), ForestL = H("4A7D52");
    // Accents
    public static readonly Color GoldD = H("7A5A24"), Gold = H("C29A45"), GoldL = H("E8C870"), GoldW = H("F8E8A8");
    public static readonly Color ParchD = H("B89A6A"), Parch = H("DCC59A"), ParchL = H("F0E2BE");
    // Text
    public static readonly Color TextW = H("F4F1E8"), TextBlue = H("BFD8F0"), TextMute = H("9A958C"), TextDark = H("2A1E18");
    // States
    public static readonly Color RedD = H("6E1A1E"), Red = H("C8343A"), RedL = H("F06A5A");
    public static readonly Color BlueD = H("1E3A6E"), Blue = H("3A78C8"), BlueL = H("7CB8F0");
    public static readonly Color GreenD = H("24502A"), Green = H("4FA040"), GreenL = H("9AD65A");
    public static readonly Color Gray = H("5E5A55"), GrayL = H("8A8680");
    public static readonly Color PurpleD = H("4A2A6E"), Purple = H("8A5AC0"), PurpleL = H("B890E8");
    public static readonly Color Orange = H("E07A30"), OrangeL = H("F0B050");

    // Characters for icon grids
    public static readonly Dictionary<char, Color> Chars = new Dictionary<char, Color>
    {
        { 'k', Ink }, { 'w', TextW },
        { 'Y', GoldW }, { 'G', GoldL }, { 'g', Gold }, { 'd', GoldD },
        { 'N', WoodL }, { 'n', WoodM }, { 'm', Wood }, { 'M', WoodD },
        { 'S', H("D0D6DC") }, { 's', H("8C96A0") }, { 'z', H("4A5260") },
        { 'R', RedL }, { 'r', Red }, { 'q', RedD },
        { 'L', GreenL }, { 'l', Green }, { 'h', GreenD },
        { 'U', BlueL }, { 'u', Blue }, { 'v', BlueD },
        { 'P', ParchL }, { 'p', Parch }, { 'o', ParchD },
        { 'c', Char2 }, { 'x', Stone }, { 'X', StoneL }, { 'y', GrayL },
        { 'V', PurpleL }, { 'i', Purple }, { 'j', PurpleD },
        { 'O', OrangeL }, { 'e', Orange },
        { 'A', H("E8D080") }, { 'a', H("C0A050") }, { 'b', H("E8E0C8") },
    };
}

/// <summary>Pixel canvas. Everything is integer pixels; no anti-aliasing anywhere.</summary>
public class Cv
{
    public Bitmap B;
    public int W { get { return B.Width; } }
    public int Ht { get { return B.Height; } }
    public Cv(int w, int h) { B = new Bitmap(w, h, PixelFormat.Format32bppArgb); }
    public Cv(Bitmap b) { B = b; }

    public void Px(int x, int y, Color c)
    {
        if (x < 0 || y < 0 || x >= B.Width || y >= B.Height) return;
        if (c.A == 255) { B.SetPixel(x, y, c); return; }
        if (c.A == 0) return;
        Color d = B.GetPixel(x, y);
        B.SetPixel(x, y, Blend(d, c));
    }
    public void Set(int x, int y, Color c) { if (x >= 0 && y >= 0 && x < B.Width && y < B.Height) B.SetPixel(x, y, c); }
    public Color Get(int x, int y) { return B.GetPixel(x, y); }

    public static Color Blend(Color d, Color s)
    {
        double sa = s.A / 255.0, da = d.A / 255.0;
        double oa = sa + da * (1 - sa);
        if (oa <= 0) return Color.FromArgb(0, 0, 0, 0);
        int r = (int)Math.Round((s.R * sa + d.R * da * (1 - sa)) / oa);
        int g = (int)Math.Round((s.G * sa + d.G * da * (1 - sa)) / oa);
        int b = (int)Math.Round((s.B * sa + d.B * da * (1 - sa)) / oa);
        return Color.FromArgb((int)Math.Round(oa * 255), r, g, b);
    }

    public void Fill(int x, int y, int w, int h, Color c) { for (int j = 0; j < h; j++) for (int i = 0; i < w; i++) Px(x + i, y + j, c); }
    public void Clear(Color c) { for (int j = 0; j < Ht; j++) for (int i = 0; i < W; i++) B.SetPixel(i, j, c); }
    public void HLine(int x, int y, int w, Color c) { for (int i = 0; i < w; i++) Px(x + i, y, c); }
    public void VLine(int x, int y, int h, Color c) { for (int j = 0; j < h; j++) Px(x, y + j, c); }
    public void Box(int x, int y, int w, int h, Color c) { HLine(x, y, w, c); HLine(x, y + h - 1, w, c); VLine(x, y, h, c); VLine(x + w - 1, y, h, c); }

    /// <summary>Alpha-composite src at (x,y), optionally scaled by integer k (nearest neighbour).</summary>
    public void Blit(Bitmap src, int x, int y, int k = 1, float alpha = 1f)
    {
        for (int j = 0; j < src.Height; j++)
            for (int i = 0; i < src.Width; i++)
            {
                Color c = src.GetPixel(i, j);
                if (c.A == 0) continue;
                if (alpha < 1f) c = Color.FromArgb((int)(c.A * alpha), c.R, c.G, c.B);
                for (int b = 0; b < k; b++) for (int a = 0; a < k; a++) Px(x + i * k + a, y + j * k + b, c);
            }
    }
    public void BlitFlip(Bitmap src, int x, int y, int k = 1)
    {
        for (int j = 0; j < src.Height; j++)
            for (int i = 0; i < src.Width; i++)
            {
                Color c = src.GetPixel(src.Width - 1 - i, j);
                if (c.A == 0) continue;
                for (int b = 0; b < k; b++) for (int a = 0; a < k; a++) Px(x + i * k + a, y + j * k + b, c);
            }
    }
    /// <summary>9-slice draw of a native sprite into a w x h rect (borders in source pixels).</summary>
    public void Slice(Bitmap src, int x, int y, int w, int h, int l, int t, int r, int b)
    {
        int sw = src.Width, sh = src.Height;
        int cw = sw - l - r, ch = sh - t - b;
        int dw = w - l - r, dh = h - t - b;
        for (int j = 0; j < h; j++)
        {
            int sy = j < t ? j : (j >= h - b ? sh - (h - j) : t + ((j - t) % Math.Max(1, ch)));
            for (int i = 0; i < w; i++)
            {
                int sx = i < l ? i : (i >= w - r ? sw - (w - i) : l + ((i - l) % Math.Max(1, cw)));
                Color c = src.GetPixel(sx, sy);
                if (c.A > 0) Px(x + i, y + j, c);
            }
        }
    }
    public Cv Scaled(int k)
    {
        var o = new Cv(W * k, Ht * k);
        for (int j = 0; j < Ht; j++) for (int i = 0; i < W; i++)
            {
                Color c = B.GetPixel(i, j);
                if (c.A == 0) continue;
                for (int b = 0; b < k; b++) for (int a = 0; a < k; a++) o.B.SetPixel(i * k + a, j * k + b, c);
            }
        return o;
    }
    public Cv Crop(int x, int y, int w, int h)
    {
        var o = new Cv(w, h);
        for (int j = 0; j < h; j++) for (int i = 0; i < w; i++) if (x + i < W && y + j < Ht && x + i >= 0 && y + j >= 0) o.B.SetPixel(i, j, B.GetPixel(x + i, y + j));
        return o;
    }
    /// <summary>Darken/tint everything already drawn (screen dim for modals).</summary>
    public void Dim(Color c) { Fill(0, 0, W, Ht, c); }
    public void Save(string path) { Directory.CreateDirectory(Path.GetDirectoryName(path)); B.Save(path, ImageFormat.Png); }

    public void Circle(double cx, double cy, double r, Color c)
    {
        for (int j = (int)(cy - r - 1); j <= (int)(cy + r + 1); j++)
            for (int i = (int)(cx - r - 1); i <= (int)(cx + r + 1); i++)
            {
                double dx = i + 0.5 - cx, dy = j + 0.5 - cy;
                if (dx * dx + dy * dy <= r * r) Px(i, j, c);
            }
    }
}

public static class Img
{
    static readonly Dictionary<string, Bitmap> Cache = new Dictionary<string, Bitmap>();
    public static string Root;   // project root
    public static Bitmap Load(string rel)
    {
        Bitmap b;
        if (Cache.TryGetValue(rel, out b)) return b;
        string p = Path.Combine(Root, rel);
        using (var tmp = new Bitmap(p)) { b = new Bitmap(tmp.Width, tmp.Height, PixelFormat.Format32bppArgb); using (var g = Graphics.FromImage(b)) { g.DrawImageUnscaled(tmp, 0, 0); } }
        Cache[rel] = b;
        return b;
    }
    public static Bitmap Cell(string rel, int cw, int ch, int col, int row)
    {
        var src = Load(rel);
        var o = new Bitmap(cw, ch, PixelFormat.Format32bppArgb);
        for (int j = 0; j < ch; j++) for (int i = 0; i < cw; i++) o.SetPixel(i, j, src.GetPixel(col * cw + i, row * ch + j));
        return o;
    }
    public static Bitmap Grid(string[] rows) { return Grid(rows, null); }
    public static Bitmap Grid(string[] rows, Dictionary<char, char> remap)
    {
        int w = rows.Max(r => r.Length), h = rows.Length;
        var o = new Bitmap(w, h, PixelFormat.Format32bppArgb);
        for (int j = 0; j < h; j++)
            for (int i = 0; i < rows[j].Length; i++)
            {
                char ch = rows[j][i];
                if (remap != null && remap.ContainsKey(ch)) ch = remap[ch];
                if (ch == '.' || ch == ' ') continue;
                Color c;
                if (!P.Chars.TryGetValue(ch, out c)) throw new Exception("Unknown grid char '" + ch + "' in row " + j + ": " + rows[j]);
                o.SetPixel(i, j, c);
            }
        return o;
    }
    /// <summary>Put a 1px ink outline around every opaque pixel (4-neighbour).</summary>
    public static Bitmap Outline(Bitmap src, Color ink)
    {
        var o = (Bitmap)src.Clone();
        for (int j = 0; j < src.Height; j++)
            for (int i = 0; i < src.Width; i++)
            {
                if (src.GetPixel(i, j).A > 0) continue;
                bool n = false;
                int[] dx = { 1, -1, 0, 0 }, dy = { 0, 0, 1, -1 };
                for (int d = 0; d < 4; d++)
                {
                    int x = i + dx[d], y = j + dy[d];
                    if (x >= 0 && y >= 0 && x < src.Width && y < src.Height && src.GetPixel(x, y).A > 128) n = true;
                }
                if (n) o.SetPixel(i, j, ink);
            }
        return o;
    }
    /// <summary>Flat silhouette in one colour (ghost equipment slots, disabled icons).</summary>
    public static Bitmap Silhouette(Bitmap src, Color c, Color edge)
    {
        var o = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
        for (int j = 0; j < src.Height; j++)
            for (int i = 0; i < src.Width; i++)
            {
                Color s = src.GetPixel(i, j);
                if (s.A == 0) continue;
                bool ink = s.R < 40 && s.G < 40 && s.B < 40;
                o.SetPixel(i, j, ink ? edge : c);
            }
        return o;
    }
    public static Bitmap Gray(Bitmap src, double darken)
    {
        var o = new Bitmap(src.Width, src.Height, PixelFormat.Format32bppArgb);
        for (int j = 0; j < src.Height; j++)
            for (int i = 0; i < src.Width; i++)
            {
                Color s = src.GetPixel(i, j);
                if (s.A == 0) continue;
                int l = (int)((s.R * 0.3 + s.G * 0.59 + s.B * 0.11) * darken);
                o.SetPixel(i, j, Color.FromArgb(s.A, l, l, Math.Min(255, l + 6)));
            }
        return o;
    }
}

/// <summary>1-bit pixel text from Press Start 2P (the font already in the Unity project). 8 px cell.</summary>
public static class Txt
{
    static readonly Dictionary<char, bool[,]> Glyphs = new Dictionary<char, bool[,]>();
    static FontFamily _fam;
    public static string FontPath;
    const int Cell = 8;

    static bool[,] Glyph(char ch)
    {
        bool[,] g;
        if (Glyphs.TryGetValue(ch, out g)) return g;
        if (_fam == null)
        {
            var pfc = new PrivateFontCollection();
            pfc.AddFontFile(FontPath);
            _fam = pfc.Families[0];
        }
        g = new bool[Cell, Cell];
        using (var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb))
        using (var gr = Graphics.FromImage(bmp))
        using (var f = new Font(_fam, 8, FontStyle.Regular, GraphicsUnit.Pixel))
        {
            gr.Clear(Color.Black);
            gr.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
            gr.DrawString(ch.ToString(), f, Brushes.White, 0, 0, StringFormat.GenericTypographic);
            // find offset using the reference glyph 'H' so all glyphs share a baseline
            int ox = RefX, oy = RefY;
            for (int j = 0; j < Cell; j++)
                for (int i = 0; i < Cell; i++)
                {
                    int sx = ox + i, sy = oy + j;
                    if (sx < 32 && sy < 32) g[i, j] = bmp.GetPixel(sx, sy).R > 128;
                }
        }
        Glyphs[ch] = g;
        return g;
    }
    static int _refX = -1, _refY = -1;
    static int RefX { get { EnsureRef(); return _refX; } }
    static int RefY { get { EnsureRef(); return _refY; } }
    static void EnsureRef()
    {
        if (_refX >= 0) return;
        if (_fam == null)
        {
            var pfc = new PrivateFontCollection();
            pfc.AddFontFile(FontPath);
            _fam = pfc.Families[0];
        }
        using (var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb))
        using (var gr = Graphics.FromImage(bmp))
        using (var f = new Font(_fam, 8, FontStyle.Regular, GraphicsUnit.Pixel))
        {
            gr.Clear(Color.Black);
            gr.TextRenderingHint = TextRenderingHint.SingleBitPerPixelGridFit;
            gr.DrawString("H", f, Brushes.White, 0, 0, StringFormat.GenericTypographic);
            int minX = 99, minY = 99;
            for (int j = 0; j < 32; j++) for (int i = 0; i < 32; i++) if (bmp.GetPixel(i, j).R > 128) { minX = Math.Min(minX, i); minY = Math.Min(minY, j); }
            _refX = minX; _refY = minY;   // 'H' fills the 7x7 box from its top-left
        }
    }

    public static int Width(string s, int scale) { return s.Length * Cell * scale - scale; }

    /// <summary>Draw text. align: 0 left, 1 center, 2 right (x is the anchor). shadow: 1px ink drop (x scale).</summary>
    public static void Draw(Cv cv, int x, int y, string s, Color c, int scale = 1, int align = 0, bool shadow = true, Color? shadowColor = null)
    {
        int w = Width(s, scale);
        if (align == 1) x -= w / 2;
        else if (align == 2) x -= w;
        Color sc = shadowColor.HasValue ? shadowColor.Value : P.Ink;
        if (shadow) Raw(cv, x + scale, y + scale, s, sc, scale);
        Raw(cv, x, y, s, c, scale);
    }
    /// <summary>Text with a full 1px outline (damage numbers, combo, labels over the world).</summary>
    public static void Outlined(Cv cv, int x, int y, string s, Color c, int scale = 1, int align = 0)
    {
        int w = Width(s, scale);
        if (align == 1) x -= w / 2; else if (align == 2) x -= w;
        int[] dx = { -1, 1, 0, 0, -1, 1, -1, 1 }, dy = { 0, 0, -1, 1, -1, -1, 1, 1 };
        for (int d = 0; d < 8; d++) Raw(cv, x + dx[d], y + dy[d], s, P.Ink, scale);
        Raw(cv, x + 1, y + 2, s, P.Ink, scale);
        Raw(cv, x, y, s, c, scale);
    }
    static void Raw(Cv cv, int x, int y, string s, Color c, int scale)
    {
        for (int n = 0; n < s.Length; n++)
        {
            if (s[n] == ' ') continue;
            var g = Glyph(s[n]);
            for (int j = 0; j < Cell; j++)
                for (int i = 0; i < Cell; i++)
                    if (g[i, j])
                        for (int b = 0; b < scale; b++) for (int a = 0; a < scale; a++) cv.Px(x + (n * Cell + i) * scale + a, y + j * scale + b, c);
        }
    }
    /// <summary>Word-wrap into lines of at most maxChars.</summary>
    public static List<string> Wrap(string s, int maxChars)
    {
        var lines = new List<string>();
        foreach (var para in s.Split('\n'))
        {
            string cur = "";
            foreach (var word in para.Split(' '))
            {
                if (cur.Length == 0) cur = word;
                else if (cur.Length + 1 + word.Length <= maxChars) cur += " " + word;
                else { lines.Add(cur); cur = word; }
            }
            lines.Add(cur);
        }
        return lines;
    }
    public static int Para(Cv cv, int x, int y, int widthPx, string s, Color c, int lineH = 11, bool shadow = false)
    {
        var lines = Wrap(s, Math.Max(1, (widthPx + 1) / Cell));
        for (int i = 0; i < lines.Count; i++) Draw(cv, x, y + i * lineH, lines[i], c, 1, 0, shadow);
        return lines.Count * lineH;
    }
}
