// Draws the Goblin Camp stage-select board, its fog layers, boss pins and map-type icons.
// Original pixel art, palette matched to UI_WorldMap_Outdoor_Art. C# 5 (built by run.ps1 via Add-Type).
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

public static class DungeonBoardArt
{
    public const int W = 408, H = 308;
    static Color[,] px;
    static bool[,] used;

    // Pin centres in board pixels (stage 1..5). Cursor places pins here.
    public static readonly int[,] Pins = { { 54, 262 }, { 146, 181 }, { 228, 236 }, { 318, 171 }, { 352, 120 } };

    // ---- palette (world map colours first, then camp-only colours) ----
    static Color Hex(string h) { return ColorTranslator.FromHtml(h); }
    static readonly Color F0 = Hex("#12281A"), F1 = Hex("#1C3A24"), F2 = Hex("#27502E"), F3 = Hex("#335E36"), F4 = Hex("#3A6A3A");
    static readonly Color G0 = Hex("#4F7E34"), G1 = Hex("#5E8C3A"), G2 = Hex("#6E9E44"), G3 = Hex("#82B050"), G4 = Hex("#9CC460");
    static readonly Color Y1 = Hex("#6C783A"), Y2 = Hex("#7E8A44"), Y0 = Hex("#54602E");
    static readonly Color D0 = Hex("#5A3F2C"), D1 = Hex("#7E7040"), D2 = Hex("#8E7E4A"), D3 = Hex("#A08E58"), D4 = Hex("#B89A6A");
    static readonly Color R0 = Hex("#26262E"), R1 = Hex("#464854"), R2 = Hex("#585A64"), R3 = Hex("#6A6C78"), R4 = Hex("#8A8C98");
    static readonly Color K0 = Hex("#0E0C12"), K1 = Hex("#1A1720");
    static readonly Color W0 = Hex("#3B2414"), W1 = Hex("#5C3A20"), W2 = Hex("#7D5330"), W3 = Hex("#A0703F");
    static readonly Color B0 = Hex("#8A7D66"), B1 = Hex("#C9BB98"), B2 = Hex("#ECE2C6");
    static readonly Color T0 = Hex("#24391C"), T1 = Hex("#3F5F2C"), T2 = Hex("#567A36");
    static readonly Color A0 = Hex("#4A2A1E"), A1 = Hex("#6A3C28"), A2 = Hex("#7A4A32"), A3 = Hex("#8A5638");
    static readonly Color Red = Hex("#C8303C"), RedD = Hex("#8A1E28"), Fire1 = Hex("#F08A3A"), Fire2 = Hex("#F2D84A");
    static readonly Color P1 = Color.FromArgb(120, 0x5E, 0x35, 0x80), P2 = Color.FromArgb(90, 0x8A, 0x5A, 0xB0);

    // ---- helpers ----
    static uint Hash(int x, int y, int s)
    {
        unchecked
        {
            uint h = (uint)(x * 374761393 + y * 668265263 + s * 1442695031);
            h = (h ^ (h >> 13)) * 1274126177u;
            return h ^ (h >> 16);
        }
    }
    static double Rnd(int x, int y, int s) { return (Hash(x, y, s) & 0xFFFF) / 65535.0; }
    static double VNoise(double x, double y, int s)
    {
        int xi = (int)Math.Floor(x), yi = (int)Math.Floor(y);
        double fx = x - xi, fy = y - yi;
        fx = fx * fx * (3 - 2 * fx); fy = fy * fy * (3 - 2 * fy);
        double a = Rnd(xi, yi, s), b = Rnd(xi + 1, yi, s), c = Rnd(xi, yi + 1, s), d = Rnd(xi + 1, yi + 1, s);
        return a + (b - a) * fx + (c - a) * fy + (a - b - c + d) * fx * fy;
    }
    static double Fbm(double x, double y, int s) { return 0.6 * VNoise(x / 16, y / 16, s) + 0.3 * VNoise(x / 7, y / 7, s + 1) + 0.1 * VNoise(x / 3, y / 3, s + 2); }

    static bool In(int x, int y) { return x >= 0 && y >= 0 && x < W && y < H; }
    static void Set(int x, int y, Color c)
    {
        if (!In(x, y) || c.A == 0) return;
        if (c.A == 255) { px[x, y] = c; return; }
        Color o = px[x, y]; double a = c.A / 255.0;
        px[x, y] = Color.FromArgb(255, (int)(o.R * (1 - a) + c.R * a), (int)(o.G * (1 - a) + c.G * a), (int)(o.B * (1 - a) + c.B * a));
    }
    static void Rect(int x, int y, int w, int h, Color c) { for (int j = y; j < y + h; j++) for (int i = x; i < x + w; i++) Set(i, j, c); }
    static void Disc(double cx, double cy, double r, Color c)
    {
        for (int y = (int)(cy - r - 1); y <= cy + r + 1; y++)
            for (int x = (int)(cx - r - 1); x <= cx + r + 1; x++)
            {
                double dx = x + 0.5 - cx, dy = y + 0.5 - cy;
                if (dx * dx + dy * dy <= r * r) Set(x, y, c);
            }
    }
    static void Ell(double cx, double cy, double rx, double ry, Color c)
    {
        for (int y = (int)(cy - ry - 1); y <= cy + ry + 1; y++)
            for (int x = (int)(cx - rx - 1); x <= cx + rx + 1; x++)
            {
                double dx = (x + 0.5 - cx) / rx, dy = (y + 0.5 - cy) / ry;
                if (dx * dx + dy * dy <= 1) Set(x, y, c);
            }
    }
    static void Line(int x0, int y0, int x1, int y1, Color c)
    {
        int dx = Math.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1, dy = -Math.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1, err = dx + dy;
        while (true)
        {
            Set(x0, y0, c);
            if (x0 == x1 && y0 == y1) break;
            int e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; }
            if (e2 <= dx) { err += dx; y0 += sy; }
        }
    }
    static void Mark(double cx, double cy, double r)
    {
        for (int y = (int)(cy - r); y <= cy + r; y++)
            for (int x = (int)(cx - r); x <= cx + r; x++)
                if (In(x, y) && (x - cx) * (x - cx) + (y - cy) * (y - cy) <= r * r) used[x, y] = true;
    }
    static bool Free(int x, int y, int r)
    {
        for (int j = y - r; j <= y + r; j++) for (int i = x - r; i <= x + r; i++) if (In(i, j) && used[i, j]) return false;
        return true;
    }

    static List<PointF> Spline(double[] p, double step)
    {
        var pts = new List<PointF>();
        int n = p.Length / 2;
        for (int i = 0; i < n - 1; i++)
        {
            int i0 = Math.Max(0, i - 1), i2 = i + 1, i3 = Math.Min(n - 1, i + 2);
            for (double t = 0; t < 1; t += step)
            {
                double t2 = t * t, t3 = t2 * t;
                double x = 0.5 * (2 * p[i * 2] + (-p[i0 * 2] + p[i2 * 2]) * t + (2 * p[i0 * 2] - 5 * p[i * 2] + 4 * p[i2 * 2] - p[i3 * 2]) * t2 + (-p[i0 * 2] + 3 * p[i * 2] - 3 * p[i2 * 2] + p[i3 * 2]) * t3);
                double y = 0.5 * (2 * p[i * 2 + 1] + (-p[i0 * 2 + 1] + p[i2 * 2 + 1]) * t + (2 * p[i0 * 2 + 1] - 5 * p[i * 2 + 1] + 4 * p[i2 * 2 + 1] - p[i3 * 2 + 1]) * t2 + (-p[i0 * 2 + 1] + 3 * p[i * 2 + 1] - 3 * p[i2 * 2 + 1] + p[i3 * 2 + 1]) * t3);
                pts.Add(new PointF((float)x, (float)y));
            }
        }
        pts.Add(new PointF((float)p[p.Length - 2], (float)p[p.Length - 1]));
        return pts;
    }

    // ---- geometry ----
    static double RavX(int y) { return 318 + 5 * Math.Sin(y / 19.0) + 2 * Math.Sin(y / 6.0); }
    static double RavHW(int y) { if (y < 126) return -1; return Math.Min(11, (y - 126) * 0.45) + 1.5 * Math.Sin(y / 9.0); }
    static bool Plateau(int x, int y)
    {
        double dx = (x - 352) / 58.0, dy = (y - 66) / 54.0;
        return dx * dx + dy * dy + (Fbm(x, y, 41) - 0.5) * 0.5 < 1;
    }
    static bool Forest(int x, int y)
    {
        if (Plateau(x, y)) return false;
        double edge = 128 - 0.38 * x + 9 * Math.Sin(x / 13.0) + 6 * Math.Sin(x / 5.0);
        if (x < 250 && y < edge) return true;
        if (y > 150 && x < 9 + 5 * Math.Sin(y / 11.0)) return true;
        return false;
    }
    static bool Field(int x, int y)
    {
        double dx = (x - 232) / 46.0, dy = (y - 242) / 30.0;
        return dx * dx + dy * dy + (Fbm(x, y, 51) - 0.5) * 0.6 < 1;
    }

    // ---- painters ----
    static void PaintGround()
    {
        for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                double n = 0.6 * VNoise(x / 11.0, y / 11.0, 7) + 0.4 * VNoise(x / 4.0, y / 4.0, 8) + (Rnd(x, y, 3) - 0.5) * 0.2;
                bool dry = x > RavX(y) + 6 && y > 112 + 22 * Fbm(x, y, 5);
                Color c;
                if (dry) c = n < 0.38 ? Y0 : n < 0.55 ? Y1 : n < 0.7 ? Y2 : D2;
                else c = n < 0.3 ? G0 : n < 0.55 ? G1 : n < 0.76 ? G2 : G3;
                px[x, y] = c;
                if (!dry && Rnd(x, y, 9) > 0.985) px[x, y] = G4;
            }
        // grass tufts
        for (int i = 0; i < 260; i++)
        {
            int x = (int)(Rnd(i, 1, 11) * W), y = (int)(Rnd(i, 2, 11) * H);
            Color c = x > RavX(y) + 6 && y > 118 ? Y0 : G0;
            Set(x, y, c); Set(x - 1, y - 1, c); Set(x + 1, y - 1, c);
        }
    }

    static void PaintPlateau()
    {
        for (int y = 0; y < 130; y++)
            for (int x = 280; x < W; x++)
            {
                if (!Plateau(x, y)) continue;
                double n = Fbm(x, y, 43) + (Rnd(x, y, 44) - 0.5) * 0.25;
                px[x, y] = n < 0.4 ? A1 : n < 0.6 ? A2 : A3;
                bool edge = !Plateau(x, y + 1) || !Plateau(x, y + 2);
                if (edge) px[x, y] = A0;
                used[x, y] = true;
            }
        // faint ritual circle in front of the tent
        for (double a = 0; a < Math.PI * 2; a += 0.02)
        {
            int x = (int)Math.Round(352 + 20 * Math.Cos(a)), y = (int)Math.Round(108 + 7 * Math.Sin(a));
            if (y > 104) Set(x, y, A0);
        }
    }

    static void PaintField()
    {
        for (int y = 200; y < 290; y++)
            for (int x = 170; x < 300; x++)
            {
                if (!Field(x, y)) continue;
                double n = Fbm(x, y, 53) + (Rnd(x, y, 54) - 0.5) * 0.3;
                px[x, y] = n < 0.36 ? D1 : n < 0.62 ? D2 : D3;
                if (!Field(x, y - 1)) px[x, y] = D0;
                used[x, y] = true;
            }
    }

    static void PaintPath(double[] pts, int seed)
    {
        var line = Spline(pts, 0.04);
        foreach (var p in line) { Disc(p.X, p.Y, 4.6, D0); Mark(p.X, p.Y, 6.5); }
        foreach (var p in line) Disc(p.X, p.Y, 3.6, D1);
        foreach (var p in line) Disc(p.X - 0.6, p.Y - 0.6, 2.4, D2);
        for (int i = 0; i < line.Count; i += 3)
        {
            var p = line[i];
            int x = (int)(p.X + (Rnd(i, seed, 61) - 0.5) * 5), y = (int)(p.Y + (Rnd(i, seed, 62) - 0.5) * 5);
            if (Rnd(i, seed, 63) > 0.55) Set(x, y, D4); else Set(x, y, D3);
        }
    }

    static void PaintRavine()
    {
        for (int y = 118; y < H; y++)
        {
            double hw = RavHW(y);
            if (hw <= 0) continue;
            double cx = RavX(y);
            int l = (int)Math.Round(cx - hw), r = (int)Math.Round(cx + hw);
            for (int x = l - 2; x <= r + 2; x++)
            {
                if (x < l || x > r) { Set(x, y, Rnd(x, y, 71) > 0.5 ? R2 : R3); continue; }
                Color c = K0;
                if (x - l < 3) c = x - l == 0 ? R3 : R1;
                else if (r - x < 2) c = R0;
                else if (Rnd(x / 2, y / 3, 72) > 0.86) c = K1;
                Set(x, y, c);
                used[x, y] = true;
            }
            Set(l - 3, y, R0); Set(r + 3, y, R0);
        }
        // top rim stones
        for (int i = 0; i < 40; i++)
        {
            int y = 126 + (int)(Rnd(i, 0, 73) * 180);
            double cx = RavX(y), hw = RavHW(y);
            if (hw <= 0) continue;
            int side = Rnd(i, 1, 73) > 0.5 ? 1 : -1;
            int x = (int)(cx + side * (hw + 4));
            Ell(x, y, 2, 1.5, R2); Set(x - 1, y - 1, R4); Set(x + 1, y + 1, R0);
        }
    }

    static void PaintBridge(int y)
    {
        double hw = RavHW(y), cx = RavX(y);
        int l = (int)(cx - hw) - 5, r = (int)(cx + hw) + 5;
        for (int x = l; x <= r; x++)
            for (int j = y - 3; j <= y + 3; j++)
            {
                int slat = (x - l) / 2;
                Color c = (x - l) % 2 == 1 && slat % 2 == 1 ? W0 : slat % 2 == 0 ? W3 : W2;
                if (j == y + 3) c = W0;
                Set(x, j, c);
            }
        Line(l, y - 4, r, y - 4, W1); Line(l, y + 4, r, y + 4, W0);
        int[] posts = { l - 1, r + 1 };
        foreach (int x in posts) { Rect(x - 1, y - 7, 3, 5, W1); Set(x - 1, y - 7, W3); Rect(x - 1, y + 3, 3, 4, W1); Set(x - 1, y + 3, W3); }
        for (int x = l; x <= r; x += 4) Set(x, y - 5, W0);
    }

    static void Tree(int cx, int cy, double r, bool trunk, int s)
    {
        if (trunk) { Rect(cx - 1, cy + (int)r - 1, 3, 4, W0); Set(cx - 1, cy + (int)r - 1, W1); Ell(cx, cy + r + 3, r * 0.8, 1.6, Color.FromArgb(90, 0, 0, 0)); }
        Disc(cx, cy, r + 1, F0);
        Disc(cx, cy, r, F1);
        Disc(cx - 0.8, cy - 1, r - 1.6, F2);
        Disc(cx - 1.6, cy - 2, r - 3.4, F3);
        for (int k = 0; k < 4; k++)
        {
            int x = cx + (int)((Rnd(k, s, 81) - 0.6) * r), y = cy + (int)((Rnd(k, s, 82) - 0.7) * r);
            Set(x, y, F4);
        }
    }

    static void PaintForest()
    {
        var trees = new List<int[]>();
        for (int gy = -1; gy < 40; gy++)
            for (int gx = -1; gx < 46; gx++)
            {
                int cx = gx * 9 + (int)(Rnd(gx, gy, 91) * 6) - (gy % 2) * 4;
                int cy = gy * 8 + (int)(Rnd(gx, gy, 92) * 5);
                if (!Forest(cx, cy) || !Free(cx, cy, 3)) continue;
                trees.Add(new[] { cx, cy, 5 + (int)(Rnd(gx, gy, 93) * 3), gx * 97 + gy });
            }
        trees.Sort((a, b) => a[1].CompareTo(b[1]));
        foreach (var t in trees) Tree(t[0], t[1], t[2], false, t[3]);
    }

    static void PaintLoneTrees()
    {
        int[,] spots = { { 22, 228 }, { 96, 262 }, { 112, 284 }, { 186, 154 }, { 290, 286 }, { 172, 286 }, { 270, 160 }, { 392, 160 }, { 380, 222 }, { 368, 286 }, { 82, 168 } };
        var list = new List<int[]>();
        for (int i = 0; i < spots.GetLength(0); i++) if (Free(spots[i, 0], spots[i, 1], 4)) list.Add(new[] { spots[i, 0], spots[i, 1], i });
        list.Sort((a, b) => a[1].CompareTo(b[1]));
        foreach (var t in list) Tree(t[0], t[1], 5.5, true, t[2] + 300);
    }

    static void Bush(int x, int y)
    {
        Ell(x, y, 4.5, 3.5, G0);
        Ell(x - 0.5, y - 0.5, 3.5, 2.5, G2);
        Set(x - 2, y - 2, G3); Set(x - 1, y - 2, G4);
        Line(x - 3, y + 3, x + 3, y + 3, Color.FromArgb(80, 0, 0, 0));
    }

    static void Rocks(int x, int y, double r)
    {
        Ell(x, y + 1, r + 1, r * 0.75 + 1, R0);
        Ell(x, y, r, r * 0.75, R2);
        Ell(x - 1, y - 1, r * 0.6, r * 0.4, R3);
        Set(x - (int)(r * 0.5), y - (int)(r * 0.4), R4);
    }

    static void PaintHills()
    {
        var list = new List<int[]>();
        for (int i = 0; i < 60; i++)
        {
            int x = 228 + (int)(Rnd(i, 0, 101) * 70), y = (int)(Rnd(i, 1, 101) * 44) - 4;
            if (Plateau(x, y)) continue;
            list.Add(new[] { x, y, 4 + (int)(Rnd(i, 2, 101) * 5) });
        }
        list.Sort((a, b) => a[1].CompareTo(b[1]));
        foreach (var r in list) Rocks(r[0], r[1], r[2]);
    }

    static void Stake(int x, int y)
    {
        // a palisade post seen from the front, base at y
        Rect(x, y - 5, 2, 6, W1);
        Set(x, y - 5, W3); Set(x, y - 4, W2);
        Set(x, y - 6, W2);
        Set(x + 1, y, W0);
    }

    static void PaintMaze()
    {
        int ox = 116, oy = 152, c = 10;
        for (int y = oy; y <= oy + 60; y++)
            for (int x = ox; x <= ox + 60; x++)
            {
                double n = Fbm(x, y, 111) + (Rnd(x, y, 112) - 0.5) * 0.3;
                px[x, y] = n < 0.45 ? D1 : n < 0.65 ? D2 : D0;
                used[x, y] = true;
            }
        int[,] seg = {
            {0,0,6,0},{0,6,6,6},{0,0,0,3},{0,4,0,6},{6,0,6,4},{6,5,6,6},
            {1,1,5,1},{5,1,5,3},{1,1,1,4},{2,2,4,2},{2,2,2,5},{3,3,3,6},{4,4,6,4},{4,3,4,5},{1,5,2,5}
        };
        // vertical runs first (seen from above), then horizontal stakes so fronts overlap correctly
        for (int i = 0; i < seg.GetLength(0); i++)
        {
            if (seg[i, 0] != seg[i, 2]) continue;
            int x = ox + seg[i, 0] * c, y0 = oy + seg[i, 1] * c, y1 = oy + seg[i, 3] * c;
            for (int y = y0 - 4; y <= y1; y++)
            {
                Set(x - 1, y, W3); Set(x, y, W2); Set(x + 1, y, W1); Set(x + 2, y, Color.FromArgb(90, 0, 0, 0));
                if ((y - y0) % 3 == 0) Set(x, y, W0);
            }
        }
        for (int i = 0; i < seg.GetLength(0); i++)
        {
            if (seg[i, 1] != seg[i, 3]) continue;
            int y = oy + seg[i, 1] * c, x0 = ox + seg[i, 0] * c, x1 = ox + seg[i, 2] * c;
            for (int x = x0 - 1; x <= x1; x += 2) Stake(x, y);
            Line(x0 - 1, y + 1, x1 + 1, y + 1, Color.FromArgb(90, 0, 0, 0));
        }
        // torch at the entrance
        Rect(111, 186, 1, 6, W1); Set(111, 184, Fire1); Set(111, 185, Fire2); Set(110, 185, Fire1); Set(112, 185, Fire1);
    }

    static void Tent(int x, int y, int s)
    {
        // small goblin tent, base centre at (x,y)
        for (int j = 0; j < 10; j++)
        {
            int half = j * 7 / 10 + 1;
            for (int i = -half; i <= half; i++)
            {
                Color c = i < -half + 2 ? T2 : (i % 3 == 0 ? T0 : T1);
                if (i == -half || i == half) c = F0;
                Set(x + i, y - 10 + j, c);
            }
        }
        Line(x - 8, y, x + 8, y, F0);
        Rect(x - 1, y - 4, 3, 4, K0);
        Set(x, y - 11, B2); Set(x - 1, y - 12, B1); Set(x + 1, y - 12, B1);
        Ell(x + 1, y + 1, 8, 1.5, Color.FromArgb(70, 0, 0, 0));
    }

    static void Campfire(int x, int y)
    {
        int[,] ring = { { -3, 0 }, { -2, -2 }, { 0, -3 }, { 2, -2 }, { 3, 0 }, { 2, 2 }, { 0, 3 }, { -2, 2 } };
        for (int i = 0; i < ring.GetLength(0); i++) { Set(x + ring[i, 0], y + ring[i, 1], R3); Set(x + ring[i, 0] + 1, y + ring[i, 1], R1); }
        Set(x - 1, y, W1); Set(x + 1, y, W1); Set(x, y + 1, W1);
        Set(x, y, Fire1); Set(x, y - 1, Fire2); Set(x - 1, y - 1, Fire1); Set(x + 1, y - 1, Red); Set(x, y - 2, Fire2);
    }

    static void Watchtower(int x, int y)
    {
        // wooden lookout: four legs, a platform and a hide roof; base centre at (x,y)
        Ell(x, y + 1, 8, 2, Color.FromArgb(90, 0, 0, 0));
        Line(x - 5, y, x - 3, y - 12, W0); Line(x + 5, y, x + 3, y - 12, W0);
        Line(x - 3, y, x - 2, y - 12, W1); Line(x + 3, y, x + 2, y - 12, W1);
        Line(x - 4, y - 4, x + 4, y - 8, W1); Line(x - 4, y - 8, x + 4, y - 4, W1);
        Rect(x - 6, y - 15, 13, 3, W2); Line(x - 6, y - 15, x + 6, y - 15, W3); Line(x - 6, y - 13, x + 6, y - 13, W0);
        for (int i = -5; i <= 5; i += 2) { Set(x + i, y - 17, W1); Set(x + i, y - 16, W1); }
        for (int j = 0; j < 6; j++)
        {
            int half = j + 1;
            for (int i = -half; i <= half; i++) Set(x + i, y - 24 + j, i < 0 ? T2 : T1);
        }
        Line(x - 7, y - 18, x + 7, y - 18, T0);
        Set(x, y - 25, B2);
        Banner(x + 6, y - 18, Red);
    }

    static void Bones(int x, int y)
    {
        Line(x - 2, y, x + 2, y + 1, B1); Set(x - 3, y - 1, B2); Set(x - 3, y + 1, B2); Set(x + 3, y, B2); Set(x + 3, y + 2, B2);
        Set(x + 1, y - 2, B2); Set(x + 2, y - 2, B1); Set(x + 1, y - 1, K0);
    }

    static void Banner(int x, int y, Color c)
    {
        Rect(x, y - 12, 1, 13, W1); Set(x, y - 13, B2);
        Rect(x + 1, y - 12, 4, 6, c); Set(x + 1, y - 6, c); Set(x + 3, y - 6, c);
        Set(x + 4, y - 12, RedD); Set(x + 4, y - 11, RedD);
    }

    static void Totem(int x, int y)
    {
        Rect(x, y - 12, 1, 12, W1);
        Ell(x + 0.5, y - 13, 3, 2.5, B2);
        Set(x - 1, y - 13, K0); Set(x + 1, y - 13, K0); Set(x, y - 11, B0);
        Line(x - 3, y - 15, x - 5, y - 18, B1); Line(x + 3, y - 15, x + 5, y - 18, B1);
        Rect(x + 1, y - 10, 3, 5, Red); Set(x + 3, y - 5, RedD);
    }

    static void BoneStake(int x, int y)
    {
        Rect(x, y - 4, 2, 5, B1); Set(x, y - 5, B2); Set(x + 1, y - 4, B0); Set(x + 1, y, B0);
    }

    static void PaintBossTent()
    {
        int cx = 352, by = 104;
        // bone fence on the plateau rim
        for (double a = 0; a < Math.PI * 2; a += 0.17)
        {
            double deg = a * 180 / Math.PI;
            if (deg > 62 && deg < 118) continue; // gap at the door
            int x = (int)(cx + 50 * Math.Cos(a)), y = (int)(70 + 44 * Math.Sin(a));
            if (In(x, y) && Plateau(x, y)) BoneStake(x, y);
        }
        // shadow
        Ell(cx, by + 1, 34, 4, Color.FromArgb(110, 0, 0, 0));
        // dome
        double[] xs = { 318, 322, 334, 352, 370, 382, 386 };
        double[] ys = { 104, 84, 62, 46, 62, 84, 104 };
        for (int y = 40; y <= by; y++)
            for (int x = 314; x <= 390; x++)
            {
                if (!InPoly(x + 0.5, y + 0.5, xs, ys)) continue;
                int panel = (x - 318) % 9;
                Color c = panel == 0 ? T0 : T1;
                if (x < 330 && panel != 0) c = T2;
                if (y > by - 2) c = T0;
                Set(x, y, c);
            }
        for (int y = 40; y <= by; y++)
            for (int x = 314; x <= 390; x++)
                if (InPoly(x + 0.5, y + 0.5, xs, ys) && (!InPoly(x - 0.5, y + 0.5, xs, ys) || !InPoly(x + 1.5, y + 0.5, xs, ys) || !InPoly(x + 0.5, y - 0.5, xs, ys))) Set(x, y, F0);
        // bone ribs over the fabric
        int[] feet = { 320, 336, 368, 384 };
        foreach (int f in feet) { Line(cx, 42, f, by, B1); Line(cx + 1, 42, f + 1, by, B0); }
        Line(cx, 42, cx, 64, B1);
        // crossed spikes on top
        Line(cx - 7, 32, cx + 2, 46, B2); Line(cx + 7, 32, cx - 2, 46, B2);
        Line(cx - 6, 32, cx + 3, 46, B0); Line(cx + 8, 32, cx - 1, 46, B0);
        // door
        for (int y = 88; y <= by; y++)
            for (int x = cx - 6; x <= cx + 6; x++)
            {
                double dx = x - cx, dy = y - 92;
                if (y < 92 && dx * dx + dy * dy * 2.2 > 40) continue;
                Set(x, y, K0);
            }
        Set(cx - 1, 98, Hex("#3A2050")); Set(cx, 97, Hex("#5E3580")); Set(cx + 1, 99, Hex("#3A2050"));
        Line(cx - 7, 88, cx - 7, by, W1); Line(cx + 7, 88, cx + 7, by, W1);
        // horned skull above the door
        Line(cx - 5, 78, cx - 11, 72, B1); Line(cx - 11, 72, cx - 12, 67, B1); Line(cx - 5, 79, cx - 10, 74, B0);
        Line(cx + 5, 78, cx + 11, 72, B1); Line(cx + 11, 72, cx + 12, 67, B1); Line(cx + 5, 79, cx + 10, 74, B0);
        Ell(cx + 0.5, 80, 5, 4.5, B0);
        Ell(cx + 0.5, 79.5, 4.5, 4, B2);
        Rect(cx - 3, 79, 2, 2, K0); Rect(cx + 2, 79, 2, 2, K0); Set(cx - 2, 79, Red); Set(cx + 3, 79, Red);
        Set(cx, 82, K0); Set(cx + 1, 82, K0);
        Line(cx - 2, 84, cx + 3, 84, B1); Set(cx - 1, 85, B2); Set(cx + 1, 85, B2); Set(cx + 3, 85, B2);
        // totems either side of the door
        Totem(326, 112); Totem(378, 112);
        Banner(306, 96, Red); Banner(396, 96, Red);
        Bones(338, 116); Bones(372, 118);
        // purple miasma
        Color m0 = Color.FromArgb(170, 0x3A, 0x20, 0x50), m1 = Color.FromArgb(170, 0x5E, 0x35, 0x80), m2 = Color.FromArgb(160, 0x8A, 0x5A, 0xB0);
        var puffs = new List<double[]>();
        for (int i = 0; i < 16; i++)
        {
            double a = -Math.PI * (0.1 + 0.8 * Rnd(i, 0, 121)), d = 0.3 + 0.7 * Rnd(i, 1, 121);
            puffs.Add(new[] { cx + Math.Cos(a) * 30 * d, 44 + Math.Sin(a) * 26 * d, 3 + Rnd(i, 3, 121) * 3.5 });
        }
        puffs.Sort((p, q) => p[1].CompareTo(q[1]));
        foreach (var p in puffs) Disc(p[0], p[1], p[2] + 1, m0);
        foreach (var p in puffs) Disc(p[0] - 0.6, p[1] - 0.8, p[2], m1);
        foreach (var p in puffs) Disc(p[0] - 1.4, p[1] - 1.6, p[2] * 0.45, m2);
        for (int i = 0; i < 18; i++)
        {
            int x = cx - 30 + (int)(Rnd(i, 5, 121) * 60), y = 14 + (int)(Rnd(i, 6, 121) * 30);
            Set(x, y, Hex("#B48CDC"));
        }
    }

    static bool InPoly(double x, double y, double[] xs, double[] ys)
    {
        bool inside = false;
        for (int i = 0, j = xs.Length - 1; i < xs.Length; j = i++)
            if ((ys[i] > y) != (ys[j] > y) && x < (xs[j] - xs[i]) * (y - ys[i]) / (ys[j] - ys[i]) + xs[i]) inside = !inside;
        return inside;
    }

    static void PaintGate()
    {
        // palisade gate where the player arrives (bottom-left)
        for (int x = 2; x <= 10; x += 2) Stake(x, 282);
        for (int x = 26; x <= 40; x += 2) Stake(x, 306);
        Banner(12, 284, Red); Banner(24, 304, Red);
    }

    static void PaintDecor()
    {
        // stage 1: winding road through bushes
        int[,] bushes = { { 30, 280 }, { 72, 286 }, { 66, 248 }, { 38, 244 }, { 92, 232 }, { 58, 222 }, { 100, 214 }, { 20, 262 }, { 84, 272 }, { 46, 300 } };
        for (int i = 0; i < bushes.GetLength(0); i++) if (Free(bushes[i, 0], bushes[i, 1], 2)) Bush(bushes[i, 0], bushes[i, 1]);
        Bones(78, 252); Banner(96, 248, Red);
        // stage 3: open camp clearing
        Tent(206, 226, 1); Tent(262, 228, 2); Tent(250, 270, 3);
        Campfire(234, 252);
        Bones(212, 258); Bones(268, 250);
        Rect(196, 250, 7, 1, W1); Rect(196, 247, 1, 4, W1); Rect(202, 247, 1, 4, W1); Set(198, 246, R4); Set(200, 246, R4);
        // stage 4: rocks around the ravine
        Rocks(296, 196, 3); Rocks(300, 150, 3.5); Rocks(340, 196, 3); Rocks(342, 142, 2.5);
        // goblin lookout tower in the open middle
        Watchtower(206, 134);
        Watchtower(96, 150);
        // bush and tree clusters to fill the open grass
        for (int i = 0; i < 90; i++)
        {
            int x = 8 + (int)(Rnd(i, 0, 141) * (W - 16)), y = 8 + (int)(Rnd(i, 1, 141) * (H - 16));
            if (Forest(x, y) || Plateau(x, y) || !Free(x, y, 4)) continue;
            if (x > RavX(y) + 6 && y > 120) { if (i % 3 == 0) Rocks(x, y, 2.5); continue; }
            if (i % 4 == 0 && Free(x, y, 7)) Tree(x, y, 5, true, i + 500); else Bush(x, y);
            Mark(x, y, 4);
        }
        // loose rocks and flowers
        for (int i = 0; i < 26; i++)
        {
            int x = (int)(Rnd(i, 0, 131) * W), y = (int)(Rnd(i, 1, 131) * H);
            if (!Free(x, y, 2) || Forest(x, y)) continue;
            if (i % 3 == 0) { Set(x, y, R3); Set(x + 1, y, R2); Set(x + 1, y + 1, R0); }
            else { Set(x, y, i % 2 == 0 ? Hex("#E8D86A") : Hex("#E8E4F0")); Set(x, y + 1, G0); }
        }
    }

    // ---- outputs ----
    public static void Board(string path)
    {
        px = new Color[W, H]; used = new bool[W, H];
        PaintGround();
        PaintPlateau();
        PaintField();
        // stage route: gate → winding → maze → clearing → bridge → boss tent
        PaintPath(new double[] { 6, 304, 22, 294, 40, 296, 54, 286, 46, 272, 30, 266, 30, 252, 46, 244, 62, 250, 78, 242, 84, 228, 76, 214, 88, 200, 104, 198, 116, 197 }, 1);
        PaintPath(new double[] { 176, 207, 192, 214, 206, 236, 228, 242, 250, 234, 270, 214, 286, 190, 300, 172, 308, 171 }, 2);
        PaintPath(new double[] { 328, 171, 336, 162, 344, 146, 348, 130, 352, 116, 352, 106 }, 3);
        PaintMaze();
        PaintRavine();
        PaintBridge(171);
        PaintHills();
        PaintForest();
        PaintLoneTrees();
        PaintGate();
        PaintDecor();
        PaintBossTent();
        Save(path, px, W, H);
    }

    public static void Fog(string dir, string prefix)
    {
        // one fog layer per lockable stage (2..5); lift each as the stage unlocks
        double[,] zone = { { 146, 181, 50 }, { 232, 240, 56 }, { 318, 175, 44 }, { 352, 70, 64 } };
        Color dark = Hex("#2E2640"), light = Hex("#5A4870");
        for (int k = 0; k < 4; k++)
        {
            var img = new Color[W, H];
            for (int y = 0; y < H; y++)
                for (int x = 0; x < W; x++)
                {
                    double dx = x - zone[k, 0], dy = y - zone[k, 1], d = Math.Sqrt(dx * dx + dy * dy) / zone[k, 2];
                    double n = Fbm(x * 1.4, y * 1.4, 200 + k);
                    double a = (1.25 - d) * 1.6 + (n - 0.5) * 0.9;
                    int level = a > 0.9 ? 4 : a > 0.6 ? 3 : a > 0.35 ? 2 : a > 0.12 ? 1 : 0;
                    // ordered dither on the soft edge
                    if (level == 1 && ((x + y) & 1) == 1) level = 0;
                    int[] alpha = { 0, 70, 120, 165, 205 };
                    Color c = n > 0.55 ? light : dark;
                    img[x, y] = Color.FromArgb(alpha[level], c.R, c.G, c.B);
                }
            Save(Path.Combine(dir, prefix + (k + 2) + ".png"), img, W, H);
        }
    }

    static Color[,] Blank(int w, int h) { var a = new Color[w, h]; for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) a[x, y] = Color.Transparent; return a; }

    static void Skull(Color[,] a, int ox, int oy, Color bone, Color eye)
    {
        string[] s = { ".XXXXX.", "XXXXXXX", "XeXXXeX", "XXXXXXX", ".XX.XX.", ".X.X.X." };
        for (int y = 0; y < s.Length; y++)
            for (int x = 0; x < 7; x++)
            {
                char ch = s[y][x];
                if (ch == 'X') a[ox + x, oy + y] = bone;
                else if (ch == 'e') a[ox + x, oy + y] = eye;
            }
    }

    public static void BossPins(string dir, string lockedSrc, string unlockedSrc)
    {
        // boss pin: world-map pin shape, red border, skull inside
        var open = Load(unlockedSrc);
        Color border = Hex("#E04848");
        for (int y = 0; y < 13; y++) for (int x = 0; x < 13; x++) if (open[x, y].A > 0 && open[x, y].ToArgb() == Hex("#AEB6C8").ToArgb()) open[x, y] = border;
        Skull(open, 3, 4, B2, Red);
        Save(Path.Combine(dir, "UI_Dungeon_Pin_Boss.png"), open, 13, 13);

        var locked = Load(lockedSrc);
        Color fill = Hex("#20232A");
        for (int y = 2; y < 11; y++) for (int x = 2; x < 11; x++) locked[x, y] = fill;
        Skull(locked, 3, 4, Hex("#8A909C"), fill);
        Save(Path.Combine(dir, "UI_Dungeon_Pin_Boss_Locked.png"), locked, 13, 13);
    }

    public static void MapTypeIcons(string dir)
    {
        Color bg = Hex("#252A33"), bd = Hex("#4A5260");
        string[] names = { "Winding", "Maze", "Field", "Ledge" };
        string[][] art = {
            new[] { ".........", "..dddddd.", ".dd......", ".dd......", "..dddddd.", "......dd.", "......dd.", ".dddddd..", "........." },
            new[] { "wwwwwwwww", "w.......w", "w.wwwww.w", "w.w...w.w", "w.w.w.w.w", "w.w.w...w", "w.www.www", "w........", "wwwwwwwww" },
            new[] { ".........", "..ddddd..", ".ddddddd.", "dddrdddd.", "ddddddrdd", "dddddrddd", ".ddddddd.", "..ddddd..", "........." },
            new[] { "gg..k..gg", "gg.kk..gg", "gg..kk.gg", "ppppppppp", "PPPPPPPPP", "gg.kk..gg", "gg..k..gg", "gg.kk..gg", "gg..kk.gg" }
        };
        for (int i = 0; i < 4; i++)
        {
            var a = Blank(11, 11);
            for (int y = 0; y < 11; y++) for (int x = 0; x < 11; x++)
                {
                    bool corner = (x == 0 || x == 10) && (y == 0 || y == 10);
                    if (corner) continue;
                    a[x, y] = x == 0 || y == 0 || x == 10 || y == 10 ? bd : bg;
                }
            for (int y = 0; y < 9; y++) for (int x = 0; x < 9; x++)
                {
                    char ch = art[i][y][x];
                    Color c = Color.Empty;
                    if (ch == 'd') c = D3; else if (ch == 'w') c = W3; else if (ch == 'r') c = Red;
                    else if (ch == 'g') c = G1; else if (ch == 'k') c = R1; else if (ch == 'p') c = W3; else if (ch == 'P') c = W1;
                    if (ch == '.' && i == 3) c = K0;
                    if (c != Color.Empty) a[x + 1, y + 1] = c;
                }
            Save(Path.Combine(dir, "UI_Dungeon_MapType_" + names[i] + ".png"), a, 11, 11);
        }
    }

    // 9-slice panels: 8x8, 1px border, notched corners (same build as UI_WorldMap_Frame).
    static void Slice(string path, int size, Color fill, Color border, Color bevel)
    {
        var a = Blank(size, size);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                bool edgeX = x == 0 || x == size - 1, edgeY = y == 0 || y == size - 1;
                if (edgeX && edgeY) continue;
                a[x, y] = edgeX || edgeY ? border : fill;
            }
        if (bevel != Color.Empty)
        {
            for (int i = 1; i < size - 1; i++) { a[i, 1] = bevel; a[1, i] = bevel; }
        }
        Save(path, a, size, size);
    }

    public static void Panels(string dir)
    {
        Slice(Path.Combine(dir, "UI_Dungeon_Tab.png"), 8, Hex("#1C1F26"), Hex("#4A5260"), Color.Empty);
        Slice(Path.Combine(dir, "UI_Dungeon_Tab_Selected.png"), 8, Hex("#252A33"), Hex("#F2D84A"), Color.Empty);
        Slice(Path.Combine(dir, "UI_Dungeon_Tab_Locked.png"), 8, Hex("#16181E"), Hex("#3A4050"), Color.Empty);
        Slice(Path.Combine(dir, "UI_Dungeon_GuardStrip.png"), 8, Hex("#1E2230"), Hex("#AEB6C8"), Color.Empty);
        Slice(Path.Combine(dir, "UI_Dungeon_RewardSlot.png"), 10, Hex("#20202A"), Hex("#5A5E6A"), Hex("#2C2E38"));
    }

    // Camp guard bust, 24x24, original design: steel helm with red plume, moustache, blue tabard.
    public static void GuardPortrait(string path)
    {
        string[] g = {
            "...........rr...........",
            "..........rRrr..........",
            ".........orrrro.........",
            "........ooHHhhoo........",
            ".......oHHhhhhhdo.......",
            "......oHHhhhhhhhdo......",
            "......oHhhhhhhhhdo......",
            ".....oHhhhhhhhhhhdo.....",
            ".....oddddddddddddo.....",
            ".....odsssshhssssdo.....",
            ".....osssssHhssssSo.....",
            ".....oseessHhseesSo.....",
            ".....osssssHhsssSSo.....",
            ".....osssssSSsssSSo.....",
            ".....osmmmmmmmmmmSo.....",
            "......osmsssssmsSo......",
            ".......osssssssSo.......",
            "........oooooooo........",
            "....oooogttttttgoooo....",
            "...ohhhogTTttttgohhho...",
            "..oHhhhdotTttttodhhhdo..",
            "..oHhhhdotTttgttodhhdo..",
            "..oddddootTtttttoodddo..",
            "..ooooooottttttttoooooo."
        };
        var map = new Dictionary<char, Color>();
        map['o'] = Hex("#141018"); map['h'] = Hex("#8A94A6"); map['H'] = Hex("#C4CCD8"); map['d'] = Hex("#5A6274");
        map['s'] = Hex("#C9A58A"); map['S'] = Hex("#A07E66"); map['e'] = Hex("#141018"); map['m'] = Hex("#6A4428");
        map['t'] = Hex("#2E62A8"); map['T'] = Hex("#4A86C8"); map['g'] = Hex("#D8B040"); map['r'] = Red; map['R'] = Hex("#E04848");
        var a = Blank(24, 24);
        for (int y = 0; y < 24; y++)
        {
            string row = g[y].PadRight(24, '.');
            for (int x = 0; x < 24; x++) { Color c; if (map.TryGetValue(row[x], out c)) a[x, y] = c; }
        }
        Save(path, a, 24, 24);
    }

    static Color[,] Load(string path)
    {
        using (var b = new Bitmap(path))
        {
            var a = new Color[b.Width, b.Height];
            for (int y = 0; y < b.Height; y++) for (int x = 0; x < b.Width; x++) a[x, y] = b.GetPixel(x, y);
            return a;
        }
    }

    static void Save(string path, Color[,] a, int w, int h)
    {
        using (var b = new Bitmap(w, h, PixelFormat.Format32bppArgb))
        {
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++) b.SetPixel(x, y, a[x, y]);
            b.Save(path, ImageFormat.Png);
        }
    }
}
