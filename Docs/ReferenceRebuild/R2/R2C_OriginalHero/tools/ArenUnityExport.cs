using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;

// Exports the approved Aren set for Unity (Sandbox only):
//   Aren_Sheet.png 128x288  - same cell layout as Hero_Sheet R2b (Idle/Walk x4, Attack x3; rows Down, Up, Side).
//                              Side rows are MIRRORED (PlayerVisual: "Side sheets face left", flipX shows right).
//   Extra/Aren_Hurt_{V}_{0,1}.png, Extra/Aren_Death_{0,1,2}.png  - 32x32, pivot (0.5, 0.0625).
//   Gear/Aren_Sword.png 384x48 (8 poses, pivot = cell centre = hand), Gear/Aren_Shield.png 48x16 (Down, Up, Side[mirrored]),
//   Gear/Aren_Hand.png 4x4 (gauntlet, anchor = pixel (1,1)), Gear/Aren_Slash.png 288x48 (Down0,Down1,Up0,Up1,Side0,Side1 mirrored).
//   Aren_Anchors.csv - per body sprite: hand/weapon/shield/slash placement in SHEET orientation.
public static class ArenUnityExport
{
    static readonly string[] Views = { "Down", "Up", "Side" };
    static readonly int[] MirrorDir = { 4, 3, 2, 1, 0, 7, 6, 5 };

    static Dictionary<char, string> Pal { get { return HeroConceptGen.Roster.First(r => r.Id == "4").Pal; } }

    static void PaintGrid(Bitmap bmp, int ox, int oy, char[,] g, bool mirror, Func<Color, Color> fx)
    {
        for (int y = 0; y < 21; y++) for (int x = 0; x < 14; x++)
            {
                char ch = g[x, y];
                if (ch == '.') continue;
                int cx = 9 + x; if (mirror) cx = 31 - cx;
                var c = HeroConceptGen.Hex(Pal[ch]);
                bmp.SetPixel(ox + cx, oy + 9 + y, fx == null ? c : fx(c));
            }
    }

    static Func<Color, Color> TintFx(Color to, double k)
    {
        return c => Color.FromArgb(255, (int)(c.R + (to.R - c.R) * k), (int)(c.G + (to.G - c.G) * k), (int)(c.B + (to.B - c.B) * k));
    }

    public static char[,] KneelGrid()
    {
        var b = EquipGen.Grid(HeroConceptGen.Roster.First(r => r.Id == "4").Rows);
        var g = new char[14, 21];
        for (int y = 0; y < 21; y++) for (int x = 0; x < 14; x++) g[x, y] = '.';
        for (int y = 0; y <= 17; y++) for (int x = 0; x < 14; x++) g[x, y + 2] = b[x, y];
        string knee = "..kAAAkkAAAk..";
        for (int x = 0; x < 14; x++) g[x, 20] = knee[x];
        return g;
    }

    static void SaveSprAt(Bitmap sheet, EquipGen.Spr s, int ox, int oy, int ax, int ay, bool mirror)
    {
        foreach (var kv in s.Px)
        {
            int x = mirror ? -kv.Key.X : kv.Key.X;
            int px = ox + ax + x, py = oy + ay + kv.Key.Y;
            if (px >= ox && py >= oy && px < sheet.Width && py < sheet.Height) sheet.SetPixel(px, py, kv.Value);
        }
    }

    class Row
    {
        public string Key; public int MainX = -99, MainY, MainDir, MainOrder, HandX = -99, HandY, OffX = -99, OffY, OffOrder, Slash = -1, SlashOrder;
        public string OffView = "Down";
    }

    static Row FromPose(string key, string view, EquipGen.Pose p, bool hand)
    {
        bool m = view == "Side";
        var r = new Row { Key = key, OffView = view };
        r.MainX = m ? 31 - p.MainX : p.MainX; r.MainY = p.MainY;
        r.MainDir = m ? MirrorDir[p.MainDir] : p.MainDir;
        r.MainOrder = p.MainFront ? 2 : -2;
        if (hand) { r.HandX = m ? 31 - p.MainX : p.MainX - 1; r.HandY = p.MainY - 1; }
        r.OffX = m ? 31 - p.OffX : p.OffX; r.OffY = p.OffY; r.OffOrder = p.OffFront ? 1 : -1;
        return r;
    }

    public static string Run(string outDir)
    {
        Directory.CreateDirectory(Path.Combine(outDir, "Extra"));
        Directory.CreateDirectory(Path.Combine(outDir, "Gear"));
        var log = new StringBuilder();

        // ---- body sheet ----
        var sheet = new Bitmap(128, 288, PixelFormat.Format32bppArgb);
        string[] anims = { "Idle", "Walk", "Attack" };
        for (int a = 0; a < 3; a++)
            for (int v = 0; v < 3; v++)
            {
                int top = (a * 3 + v) * 32, frames = a == 2 ? 3 : 4;
                var b = EquipGen.ArenBase(Views[v]);
                for (int f = 0; f < frames; f++)
                {
                    char[,] g = a == 0 ? ArenIdleGen.Frame(Views[v], b, f) : a == 1 ? ArenWalkGen.Frame(Views[v], b, f) : EquipGen.ArenAttackBody(Views[v], f);
                    PaintGrid(sheet, f * 32, top, g, Views[v] == "Side", null);
                }
            }
        sheet.Save(Path.Combine(outDir, "Aren_Sheet.png"), ImageFormat.Png); sheet.Dispose();

        // ---- hurt / death ----
        foreach (var v in Views)
        {
            var b = EquipGen.ArenBase(v);
            for (int f = 0; f < 2; f++)
            {
                var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
                if (f == 0) PaintGrid(bmp, 0, 0, ArenIdleGen.Frame(v, b, 0), v == "Side", TintFx(Color.White, 0.8));
                else PaintGrid(bmp, 0, 0, ArenWalkGen.Frame(v, b, 0), v == "Side", TintFx(HeroConceptGen.Hex("FF3030"), 0.35));
                bmp.Save(Path.Combine(outDir, "Extra", "Aren_Hurt_" + v + "_" + f + ".png"), ImageFormat.Png); bmp.Dispose();
            }
        }
        {
            var d0 = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
            PaintGrid(d0, 0, 0, KneelGrid(), false, null);
            d0.Save(Path.Combine(outDir, "Extra", "Aren_Death_Down_0.png"), ImageFormat.Png); d0.Dispose();
            var lying = FxGen.Lying(HeroConceptGen.Roster.First(r => r.Id == "4").Rows);
            for (int f = 1; f <= 2; f++)
            {
                var bmp = new Bitmap(32, 32, PixelFormat.Format32bppArgb);
                for (int y = 0; y < 14; y++) for (int x = 0; x < 21; x++)
                        if (lying[x, y] != '.') bmp.SetPixel(5 + x, 16 + y, HeroConceptGen.Hex(Pal[lying[x, y]]));
                bmp.Save(Path.Combine(outDir, "Extra", "Aren_Death_Down_" + f + ".png"), ImageFormat.Png); bmp.Dispose();
            }
        }

        // ---- gear ----
        var sword = new Bitmap(48 * 8, 48, PixelFormat.Format32bppArgb);
        for (int d = 0; d < 8; d++) SaveSprAt(sword, EquipGen.Blade(EquipGen.SteelLongsword, d), d * 48, 0, 24, 24, false);
        sword.Save(Path.Combine(outDir, "Gear", "Aren_Sword.png"), ImageFormat.Png); sword.Dispose();
        var shield = new Bitmap(48, 16, PixelFormat.Format32bppArgb);
        for (int v = 0; v < 3; v++) SaveSprAt(shield, EquipGen.HeaterShield(Views[v]), v * 16, 0, 8, 8, Views[v] == "Side");
        shield.Save(Path.Combine(outDir, "Gear", "Aren_Shield.png"), ImageFormat.Png); shield.Dispose();
        var hand = new Bitmap(4, 4, PixelFormat.Format32bppArgb);
        SaveSprAt(hand, EquipGen.Gauntlet, 0, 0, 1, 1, false);
        hand.Save(Path.Combine(outDir, "Gear", "Aren_Hand.png"), ImageFormat.Png); hand.Dispose();
        var slash = new Bitmap(48 * 6, 48, PixelFormat.Format32bppArgb);
        for (int v = 0; v < 3; v++)
            for (int fr = 0; fr < 2; fr++)
            {
                int cell = v * 2 + fr;
                foreach (var kv in FxGen.SlashPixels(FxGen.ArenSlash(Views[v]), fr))
                {
                    int X = 8 + kv.Key.X, Y = 8 + kv.Key.Y;
                    if (Views[v] == "Side") X = 47 - X;
                    if (X >= 0 && X < 48 && Y >= 0 && Y < 48) slash.SetPixel(cell * 48 + X, Y, kv.Value);
                }
            }
        slash.Save(Path.Combine(outDir, "Gear", "Aren_Slash.png"), ImageFormat.Png); slash.Dispose();

        // ---- anchors ----
        var rows = new List<Row>();
        foreach (var anim in new[] { "Idle", "Walk" })
            foreach (var v in Views)
                for (int f = 0; f < 4; f++) rows.Add(FromPose(anim + "_" + v + "_" + f, v, EquipGen.ArenMovePose(anim, v, f), true));
        foreach (var v in Views)
        {
            var ps = EquipGen.ArenAttackPoses(v);
            for (int f = 0; f < 3; f++)
            {
                var r = FromPose("Attack_" + v + "_" + f, v, ps[f], false);
                if (f > 0) { r.Slash = Array.IndexOf(Views, v) * 2 + (f - 1); r.SlashOrder = v == "Up" ? -3 : 3; }
                rows.Add(r);
            }
            rows.Add(FromPose("Hurt_" + v + "_0", v, EquipGen.ArenMovePose("Idle", v, 0), true));
            rows.Add(FromPose("Hurt_" + v + "_1", v, EquipGen.ArenMovePose("Walk", v, 0), true));
        }
        rows.Add(new Row { Key = "Death_Down_0", MainX = 10, MainY = 25, MainDir = 6, MainOrder = 2, HandX = 9, HandY = 24, OffX = 21, OffY = 26, OffOrder = 1, OffView = "Down" });
        rows.Add(new Row { Key = "Death_Down_1" });
        rows.Add(new Row { Key = "Death_Down_2", MainX = 7, MainY = 31, MainDir = 0, MainOrder = -2, OffX = 28, OffY = 27, OffOrder = 1, OffView = "Down" });

        var csv = new StringBuilder("key,main_x,main_y,main_dir,main_order,hand_x,hand_y,off_x,off_y,off_view,off_order,slash,slash_order\n");
        foreach (var r in rows)
            csv.AppendFormat("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},{11},{12}\n", r.Key, r.MainX, r.MainY, r.MainDir, r.MainOrder, r.HandX, r.HandY, r.OffX, r.OffY, r.OffView, r.OffOrder, r.Slash, r.SlashOrder);
        File.WriteAllText(Path.Combine(outDir, "Aren_Anchors.csv"), csv.ToString(), new UTF8Encoding(false));
        log.AppendFormat("rows {0}\n", rows.Count);
        return log.ToString();
    }
}
