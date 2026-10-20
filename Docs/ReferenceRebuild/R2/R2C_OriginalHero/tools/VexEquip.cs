using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

// Vex gear + VFX + Unity export (same conventions as ArenUnityExport: anchors in SHEET orientation, Side mirrored).
public static class VexEquip
{
    static readonly string[] Views = { "Down", "Up", "Side" };
    static readonly int[] MirrorDir = { 4, 3, 2, 1, 0, 7, 6, 5 };
    public static readonly string[] Crimson = { "FFE0E0", "FF6060", "C82838", "6A1020", "C0C4D0" };

    public class P { public int MX = -99, MY, MD, MO, OX = -99, OY, OD = -1, OO; }   // main/off pos, dir, order (+front / -behind)

    static P Pz(int mx, int my, int md, int mo, int ox, int oy, int od, int oo) { return new P { MX = mx, MY = my, MD = md, MO = mo, OX = ox, OY = oy, OD = od, OO = oo }; }

    // Idle / Walk: both daggers sheathed crossed (X) in the middle of the back, hilts up, blades down; drawn on Attack.
    // Up: full X on the back (in front of the body). Side: X behind the back edge, half shows. Down: the X is hidden by
    // the body, so the two hilts peek over the shoulders (dagger sheet cells 8 / 9, drawn behind the body).
    public const int PeekL = 8, PeekR = 9;
    public static P Rest(string v)
    {
        if (v == "Down") return Pz(9, 20, PeekL, -2, 22, 20, PeekR, -1);
        if (v == "Up") return Pz(14, 20, 7, 1, 18, 20, 5, 2);
        return Pz(8, 19, 7, -2, 12, 19, 5, -1);
    }

    // hilt peeking over a shoulder (anchor = top of the grip at the shoulder notch); left points up-left, right up-right
    public static EquipGen.Spr Peek(bool left)
    {
        var w = EquipGen.Dagger; var s = new EquipGen.Spr(); int sx = left ? -1 : 1;
        s.Set(-sx, 1, EquipGen.Hex(w.Guard)); s.Set(0, 0, EquipGen.Hex(w.Wrap)); s.Set(sx, -1, EquipGen.Hex(w.Wrap)); s.Set(2 * sx, -2, EquipGen.Hex(w.Pommel));
        return EquipGen.Outline(s, EquipGen.Hex(w.Outline));
    }

    public static EquipGen.Spr DaggerSprite(int d) { return d == PeekL ? Peek(true) : d == PeekR ? Peek(false) : EquipGen.Blade(Dagger, d); }

    public static P Move(string anim, string v, int f)
    {
        var r = Rest(v);
        if (anim == "Walk" && (f == 0 || f == 2)) { r.MY++; r.OY++; }
        return r;
    }

    public static P Windup(string v)
    {
        if (v == "Down") return Pz(10, 21, 3, -2, 21, 21, 1, -1);
        if (v == "Up") return Pz(21, 21, 1, 2, 10, 21, 3, 1);
        return Pz(12, 21, 3, -2, 19, 21, 3, 1);
    }

    // strike frame per combo step: 0 stab main, 1 stab off, 2 cross, 3 X (finisher)
    public static P Strike(string v, int step)
    {
        if (v == "Down")
            switch (step)
            {
                case 0: return Pz(12, 25, 7, 2, 21, 25, 7, 1);
                case 1: return Pz(10, 25, 5, 1, 19, 25, 5, 2);
                case 2: return Pz(12, 25, 7, 2, 19, 25, 5, 3);
                default: return Pz(11, 24, 5, 2, 20, 24, 7, 3);
            }
        if (v == "Up")
            switch (step)
            {
                case 0: return Pz(19, 18, 3, -2, 10, 25, 5, 1);
                case 1: return Pz(21, 25, 7, 2, 12, 18, 1, -1);
                case 2: return Pz(19, 18, 3, -2, 12, 18, 1, -1);
                default: return Pz(20, 18, 1, -2, 11, 18, 3, -1);
            }
        switch (step)
        {
            case 0: return Pz(21, 23, 0, 2, 20, 25, 7, 1);
            case 1: return Pz(13, 24, 5, -2, 21, 24, 0, 1);
            case 2: return Pz(21, 22, 7, 2, 20, 25, 1, 3);
            default: return Pz(21, 21, 1, 2, 21, 26, 7, 3);
        }
    }

    // daggers still in hand, lowered (the pre-2026-10-20 rest pose); Attack F2 recover uses it
    public static P HandRest(string v)
    {
        if (v == "Down") return Pz(10, 24, 5, 2, 21, 24, 7, 1);
        if (v == "Up") return Pz(21, 24, 7, 2, 10, 24, 5, 1);
        return Pz(12, 24, 5, -2, 20, 24, 7, 1);
    }

    public static P Recover(string v) { var r = HandRest(v); r.MY++; r.OY++; return r; }

    public static EquipGen.BladeDef Dagger { get { return EquipGen.Dagger; } }

    // ---------- slash VFX (canvas 48, body cell at (8,8)); frame 0 strong, frame 1 thin & short ----------
    static void Slash(VfxConceptGen.Canvas cv, string v, int step, int fr)
    {
        var p = Crimson;
        int cx, cy;
        if (v == "Down") { cx = 24; cy = 38; } else if (v == "Up") { cx = 24; cy = 25; } else { cx = 33; cy = 31; }
        int s = fr == 0 ? 7 : 5, w = fr == 0 ? 2 : 1;
        Color mid = HeroConceptGen.Hex(fr == 0 ? p[2] : p[3]), core = HeroConceptGen.Hex(fr == 0 ? p[0] : p[1]);
        Action<int, int, int, int> L = (x0, y0, x1, y1) => { VfxConceptGen.Line(cv, mid, x0, y0, x1, y1, w); VfxConceptGen.Line(cv, core, (x0 * 3 + x1) / 4, (y0 * 3 + y1) / 4, (x0 + x1 * 3) / 4, (y0 + y1 * 3) / 4, 1); };
        bool side = v == "Side";
        switch (step)
        {
            case 0: if (side) L(cx - 9, cy, cx + 9, cy); else L(cx - s, cy - s, cx + s, cy + s); break;
            case 1: if (side) L(cx - 9, cy + 3, cx + 9, cy + 2); else L(cx + s, cy - s, cx - s, cy + s); break;
            case 2: if (side) { L(cx - 6, cy - 5, cx + 6, cy + 5); L(cx - 6, cy + 5, cx + 6, cy - 5); } else { L(cx - 10, cy + 1, cx + 10, cy + 1); L(cx - 4, cy - 3, cx + 4, cy + 5); } break;
            default:
                if (fr == 0) { VfxConceptGen.XSlash(cv, p, cx, cy, 8, 2); VfxConceptGen.Dots(cv, new[] { p[1], p[0], p[4] }, 31 + cx, cx, cy, 11, 9, 8); }
                else { L(cx - 6, cy - 6, cx + 6, cy + 6); L(cx - 6, cy + 6, cx + 6, cy - 6); }
                break;
        }
    }

    public static Bitmap SlashSheet()
    {
        var bmp = new Bitmap(48 * 24, 48, PixelFormat.Format32bppArgb);
        for (int step = 0; step < 4; step++)
            for (int v = 0; v < 3; v++)
                for (int fr = 0; fr < 2; fr++)
                {
                    var cv = new VfxConceptGen.Canvas();
                    Slash(cv, Views[v], step, fr);
                    int cell = step * 6 + v * 2 + fr;
                    for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++)
                        {
                            if (x >= VfxConceptGen.W || y >= VfxConceptGen.H || !cv.Px[x, y].HasValue) continue;
                            int X = Views[v] == "Side" ? 47 - x : x;
                            bmp.SetPixel(cell * 48 + X, y, cv.Px[x, y].Value);
                        }
                }
        return bmp;
    }

    // ---------- skill VFX ----------
    public static Bitmap Smoke()
    {
        var bmp = new Bitmap(32 * 4, 32, PixelFormat.Format32bppArgb);
        var puffs = new[] { new[] { 16, 18, 5 }, new[] { 11, 15, 4 }, new[] { 21, 14, 4 }, new[] { 13, 21, 4 }, new[] { 20, 21, 4 }, new[] { 16, 11, 3 } };
        for (int f = 0; f < 4; f++)
        {
            var cv = new VfxConceptGen.Canvas();
            double grow = 0.6 + f * 0.28;
            foreach (var pf in puffs)
            {
                double cx = 16 + (pf[0] - 16) * (1 + f * 0.25), cy = 18 + (pf[1] - 18) * (1 + f * 0.25), r = pf[2] * grow;
                if (f == 3 && pf[2] < 4) continue;
                VfxConceptGen.Disc(cv, HeroConceptGen.Hex(f < 2 ? "2A2630" : "4A4452"), cx, cy, r);
                VfxConceptGen.Disc(cv, HeroConceptGen.Hex(f < 2 ? "4A4452" : "6A6476"), cx - 1, cy - 1, Math.Max(0.5, r - 2));
            }
            if (f < 2) { cv.Set(13, 16, HeroConceptGen.Hex("FF4040")); cv.Set(18, 16, HeroConceptGen.Hex("FF4040")); }
            for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++) if (cv.Px[x, y].HasValue) bmp.SetPixel(f * 32 + x, y, cv.Px[x, y].Value);
        }
        return bmp;
    }

    public static Bitmap Knives()
    {
        var bmp = new Bitmap(32 * 8, 32, PixelFormat.Format32bppArgb);
        for (int d = 0; d < 8; d++)
        {
            var dir = EquipGen.Dir[d];
            int ox = d * 32 + 16 - dir.X * 3, oy = 16 - dir.Y * 3;   // centre the blade, not the grip
            for (int i = 2; i <= 7; i++)   // red streak behind
            {
                int x = ox - dir.X * i, y = oy - dir.Y * i;
                if (x >= d * 32 && x < d * 32 + 32 && y >= 0 && y < 32) bmp.SetPixel(x, y, HeroConceptGen.Hex(i < 4 ? Crimson[1] : Crimson[2]));
            }
            foreach (var kv in EquipGen.Blade(Dagger, d).Px)
            {
                int x = ox + kv.Key.X, y = oy + kv.Key.Y;
                if (x >= d * 32 && x < d * 32 + 32 && y >= 0 && y < 32) bmp.SetPixel(x, y, kv.Value);
            }
        }
        return bmp;
    }

    public static Bitmap ExecuteX()
    {
        var bmp = new Bitmap(48 * 2, 48, PixelFormat.Format32bppArgb);
        for (int f = 0; f < 2; f++)
        {
            var cv = new VfxConceptGen.Canvas();
            if (f == 0) { VfxConceptGen.XSlash(cv, Crimson, 24, 24, 15, 3); VfxConceptGen.Dots(cv, new[] { Crimson[1], Crimson[0] }, 77, 24, 24, 20, 20, 18); }
            else { VfxConceptGen.Line(cv, HeroConceptGen.Hex(Crimson[3]), 11, 11, 37, 37, 2); VfxConceptGen.Line(cv, HeroConceptGen.Hex(Crimson[3]), 11, 37, 37, 11, 2); VfxConceptGen.Line(cv, HeroConceptGen.Hex(Crimson[1]), 15, 15, 33, 33, 1); VfxConceptGen.Line(cv, HeroConceptGen.Hex(Crimson[1]), 15, 33, 33, 15, 1); }
            for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++) if (cv.Px[x, y].HasValue) bmp.SetPixel(f * 48 + x, y, cv.Px[x, y].Value);
        }
        return bmp;
    }

    // ---------- export ----------
    static void PaintGrid(Bitmap bmp, int ox, int oy, char[,] g, bool mirror, Func<Color, Color> fx)
    {
        var pal = VexGen.Pal;
        for (int y = 0; y < 21; y++) for (int x = 0; x < 14; x++)
            {
                char ch = g[x, y];
                if (ch == '.') continue;
                int cx = 9 + x; if (mirror) cx = 31 - cx;
                var c = HeroConceptGen.Hex(pal[ch]);
                bmp.SetPixel(ox + cx, oy + 9 + y, fx == null ? c : fx(c));
            }
    }

    static Func<Color, Color> TintFx(Color to, double k)
    {
        return c => Color.FromArgb(255, (int)(c.R + (to.R - c.R) * k), (int)(c.G + (to.G - c.G) * k), (int)(c.B + (to.B - c.B) * k));
    }

    static string Row(string key, string v, P p, int slash, int slashOrder)
    {
        bool m = v == "Side";
        int mx = p.MX, md = p.MD, ox = p.OX, od = p.OD;
        if (m && mx > -99) { mx = 31 - mx; if (md < 8) md = MirrorDir[md]; }
        if (m && ox > -99) { ox = 31 - ox; if (od >= 0 && od < 8) od = MirrorDir[od]; }
        // key,main_x,main_y,main_dir,main_order,hand_x,hand_y,off_x,off_y,off_view,off_order,slash,slash_order,off_dir
        return string.Format("{0},{1},{2},{3},{4},-99,0,{5},{6},{7},{8},{9},{10},{11}\n", key, mx, p.MY, md, p.MO, ox, p.OY, v, p.OO, slash, slashOrder, od);
    }

    public static string Run(string outDir)
    {
        Directory.CreateDirectory(Path.Combine(outDir, "Extra"));
        Directory.CreateDirectory(Path.Combine(outDir, "Gear"));
        // body sheet: same layout as Aren / R2b
        var sheet = new Bitmap(128, 288, PixelFormat.Format32bppArgb);
        for (int a = 0; a < 3; a++)
            for (int v = 0; v < 3; v++)
                for (int f = 0; f < (a == 2 ? 3 : 4); f++)
                {
                    char[,] g = a == 0 ? VexGen.Idle(Views[v], f) : a == 1 ? VexGen.Walk(Views[v], f) : VexGen.Attack(Views[v], f);
                    PaintGrid(sheet, f * 32, (a * 3 + v) * 32, g, Views[v] == "Side", null);
                }
        sheet.Save(Path.Combine(outDir, "Vex_Sheet.png"), ImageFormat.Png); sheet.Dispose();
        foreach (var v in Views)
            for (int f = 0; f < 2; f++)
            {
                var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
                if (f == 0) PaintGrid(bmp, 0, 0, VexGen.Idle(v, 0), v == "Side", TintFx(Color.White, 0.8));
                else PaintGrid(bmp, 0, 0, VexGen.Walk(v, 0), v == "Side", TintFx(HeroConceptGen.Hex("FF3030"), 0.35));
                bmp.Save(Path.Combine(outDir, "Extra", "Vex_Hurt_" + v + "_" + f + ".png"), ImageFormat.Png); bmp.Dispose();
            }
        {
            var d0 = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
            PaintGrid(d0, 0, 0, VexGen.Kneel(), false, null);
            d0.Save(Path.Combine(outDir, "Extra", "Vex_Death_Down_0.png"), ImageFormat.Png); d0.Dispose();
            var lying = FxGen.Lying(VexGen.Down);
            for (int f = 1; f <= 2; f++)
            {
                var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
                for (int y = 0; y < 14; y++) for (int x = 0; x < 21; x++)
                        if (lying[x, y] != '.') bmp.SetPixel(5 + x, 16 + y, HeroConceptGen.Hex(VexGen.Pal[lying[x, y]]));
                bmp.Save(Path.Combine(outDir, "Extra", "Vex_Death_Down_" + f + ".png"), ImageFormat.Png); bmp.Dispose();
            }
        }
        // gear
        var dag = new Bitmap(48 * 10, 48, PixelFormat.Format32bppArgb);   // 0-7 dirs, 8 / 9 shoulder-hilt peeks (Down idle)
        for (int d = 0; d < 10; d++) foreach (var kv in DaggerSprite(d).Px) dag.SetPixel(d * 48 + 24 + kv.Key.X, 24 + kv.Key.Y, kv.Value);
        dag.Save(Path.Combine(outDir, "Gear", "Vex_Dagger.png"), ImageFormat.Png); dag.Dispose();
        using (var b = SlashSheet()) b.Save(Path.Combine(outDir, "Gear", "Vex_Slash.png"), ImageFormat.Png);
        using (var b = Smoke()) b.Save(Path.Combine(outDir, "Gear", "Vex_Smoke.png"), ImageFormat.Png);
        using (var b = Knives()) b.Save(Path.Combine(outDir, "Gear", "Vex_Knife.png"), ImageFormat.Png);
        using (var b = ExecuteX()) b.Save(Path.Combine(outDir, "Gear", "Vex_ExecuteX.png"), ImageFormat.Png);
        // anchors
        var csv = new StringBuilder("key,main_x,main_y,main_dir,main_order,hand_x,hand_y,off_x,off_y,off_view,off_order,slash,slash_order,off_dir\n");
        int rows = 0;
        foreach (var anim in new[] { "Idle", "Walk" })
            foreach (var v in Views)
                for (int f = 0; f < 4; f++) { csv.Append(Row(anim + "_" + v + "_" + f, v, Move(anim, v, f), -1, 0)); rows++; }
        for (int vi = 0; vi < 3; vi++)
        {
            var v = Views[vi]; int so = v == "Up" ? -3 : 4;
            csv.Append(Row("Attack_" + v + "_0", v, Windup(v), -1, 0));
            csv.Append(Row("Attack_" + v + "_1", v, Strike(v, 0), vi * 2, so));
            csv.Append(Row("Attack_" + v + "_2", v, Recover(v), vi * 2 + 1, so));
            for (int step = 0; step < 4; step++)
            {
                csv.Append(Row("Attack_" + v + "_1@" + step, v, Strike(v, step), vi * 2, so));
                csv.Append(Row("Attack_" + v + "_2@" + step, v, Recover(v), vi * 2 + 1, so));
            }
            csv.Append(Row("Hurt_" + v + "_0", v, Move("Idle", v, 0), -1, 0));
            csv.Append(Row("Hurt_" + v + "_1", v, Move("Walk", v, 0), -1, 0));
            rows += 13;
        }
        csv.Append(Row("Death_Down_0", "Down", Pz(10, 26, 6, 2, 21, 26, 6, 1), -1, 0));
        csv.Append("Death_Down_1,-99,0,0,0,-99,0,-99,0,Down,0,-1,0,-1\n");
        csv.Append(Row("Death_Down_2", "Down", Pz(7, 31, 0, -2, 26, 31, 4, -1), -1, 0));
        rows += 3;
        File.WriteAllText(Path.Combine(outDir, "Vex_Anchors.csv"), csv.ToString(), new UTF8Encoding(false));
        return "rows " + rows;
    }

    // ---------- preview composite (right-facing art, unmirrored) ----------
    public static Bitmap Preview(string anim, string v, int f, int step)
    {
        char[,] g = anim == "Idle" ? VexGen.Idle(v, f) : anim == "Walk" ? VexGen.Walk(v, f) : VexGen.Attack(v, f);
        var body = EquipGen.Body(g, VexGen.Pal);
        P p = anim == "Attack" ? (f == 0 ? Windup(v) : f == 1 ? Strike(v, step) : Recover(v)) : Move(anim, v, f);
        var behind = new List<EquipGen.Layer>(); var front = new List<EquipGen.Layer>();
        var main = new EquipGen.Layer { S = DaggerSprite(p.MD), X = p.MX, Y = p.MY };
        var off = new EquipGen.Layer { S = DaggerSprite(p.OD), X = p.OX, Y = p.OY };
        var items = new List<KeyValuePair<int, EquipGen.Layer>> { new KeyValuePair<int, EquipGen.Layer>(p.MO, main), new KeyValuePair<int, EquipGen.Layer>(p.OO, off) };
        foreach (var it in items.OrderBy(i => i.Key)) (it.Key > 0 ? front : behind).Add(it.Value);
        var bmp = EquipGen.Compose(body, behind, front);
        if (anim == "Attack" && f > 0)
        {
            var cv = new VfxConceptGen.Canvas();
            Slash(cv, v, step, f - 1);
            for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++) if (cv.Px[x, y].HasValue) bmp.SetPixel(x, y, cv.Px[x, y].Value);
        }
        return bmp;
    }
}
