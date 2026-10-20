using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

// Aren skill body frames — CANDIDATE. Same 14x21 template / anchors as the approved Aren set; skills get their own
// body variants (drop, stride, crouch, spin) + sword / shield poses (sword never rotated: 8 drawn directions).
//  ShieldBash 2f (brace, bash) · Charge 2f loop (run, shield forward) · Whirl 4f (spin, view-independent)
//  Bulwark 1f hold (crouch, shield up) · Dawnbreaker 3f (raise, cleave, follow-through)
public static class ArenSkillGen
{
    const int W = 14, H = 21;
    static Dictionary<char, string> Pal { get { return HeroConceptGen.Roster.First(r => r.Id == "4").Pal; } }

    static void Row(char[,] g, int y, string s) { for (int x = 0; x < W; x++) g[x, y] = s[x]; }

    // body variants built from the approved frames
    public static char[,] Lunge(string view, int stride) { return ArenWalkGen.Frame(view, EquipGen.ArenBase(view), stride == 0 ? 0 : 2); }

    // Side lean: upper body (rows 0-13) pushed 1 px toward the facing (right)
    public static char[,] Lean(char[,] g)
    {
        var o = (char[,])g.Clone();
        for (int y = 0; y <= 13; y++) { for (int x = W - 1; x >= 1; x--) o[x, y] = g[x - 1, y]; o[0, y] = '.'; }
        return o;
    }

    public static char[,] Crouch(string view)
    {
        var b = EquipGen.ArenBase(view); var g = EquipGen.Grid(b);
        for (int y = 18; y >= 1; y--) for (int x = 0; x < W; x++) g[x, y] = b[y - 1][x];
        Row(g, 0, "..............");
        if (view == "Side") { Row(g, 19, ".kZak...kaAk.."); Row(g, 20, "kZaAk...kaAAk."); }
        else { Row(g, 19, ".kZak....kZak."); Row(g, 20, "kZaAk....kZaAk"); }
        return g;
    }

    public class Pose { public int SX, SY, SD; public bool SFront, Hand = true; public int OX, OY; public bool OFront = true; public string OView; }
    static Pose P(int sx, int sy, int sd, bool sf, int ox, int oy, bool of) { return new Pose { SX = sx, SY = sy, SD = sd, SFront = sf, OX = ox, OY = oy, OFront = of }; }

    public class Frame { public string BodyView; public bool Mirror; public char[,] Body; public Pose Gear; public string Note; }

    // ---------- skills (right-facing preview; Side is mirrored on export like the rest of the set) ----------
    public static Frame[] ShieldBash(string v)
    {
        if (v == "Down") return new[]
        {
            new Frame { BodyView = v, Body = Lunge(v, 0), Gear = P(10, 24, 6, false, 18, 22, true), Note = "thu the: khien keo vao giua" },
            new Frame { BodyView = v, Body = Lunge(v, 1), Gear = P(10, 20, 3, false, 16, 26, true), Note = "dap: khien day ra truoc, kiem keo sau vai" },
        };
        if (v == "Up") return new[]
        {
            new Frame { BodyView = v, Body = Lunge(v, 0), Gear = P(21, 24, 6, true, 13, 20, false), Note = "thu the" },
            new Frame { BodyView = v, Body = Lunge(v, 1), Gear = P(21, 24, 6, true, 16, 15, false), Note = "dap ve phia truoc (xa camera)" },
        };
        return new[]
        {
            new Frame { BodyView = v, Body = Lunge(v, 0), Gear = P(11, 23, 4, false, 21, 22, true), Note = "thu the: khien ngang nguc" },
            new Frame { BodyView = v, Body = Lean(Lunge(v, 1)), Gear = P(10, 22, 4, false, 25, 22, true), Note = "dap: khien dam thang, nguoi nga toi" },
        };
    }

    public static Frame[] Charge(string v)
    {
        var bash = ShieldBash(v)[1].Gear;
        return new[]
        {
            new Frame { BodyView = v, Body = v == "Side" ? Lean(Lunge(v, 0)) : Lunge(v, 0), Gear = bash, Note = "chay: khien chan truoc" },
            new Frame { BodyView = v, Body = v == "Side" ? Lean(Lunge(v, 1)) : Lunge(v, 1), Gear = new Pose { SX = bash.SX, SY = bash.SY - 1, SD = bash.SD, SFront = bash.SFront, OX = bash.OX, OY = bash.OY - 1, OFront = bash.OFront }, Note = "chay: chan doi" },
        };
    }

    // view-independent spin: Down -> Side (right) -> Up -> Side (left); sword held straight out from the body
    public static Frame[] Whirl()
    {
        var down = EquipGen.Grid(EquipGen.ArenBase("Down")); var side = EquipGen.Grid(EquipGen.ArenBase("Side")); var up = EquipGen.Grid(EquipGen.ArenBase("Up"));
        return new[]
        {
            new Frame { BodyView = "Down", Body = down, Gear = new Pose { SX = 21, SY = 22, SD = 0, SFront = true, OX = 11, OY = 23, OFront = true, OView = "Down" }, Note = "xoay 1: kiem chia phai" },
            new Frame { BodyView = "Side", Mirror = true, Body = side, Gear = new Pose { SX = 18, SY = 25, SD = 6, SFront = true, OX = 12, OY = 22, OFront = false, OView = "Side" }, Note = "xoay 2: kiem chia ve camera" },
            new Frame { BodyView = "Up", Body = up, Gear = new Pose { SX = 10, SY = 22, SD = 4, SFront = true, OX = 21, OY = 23, OFront = true, OView = "Up" }, Note = "xoay 3: kiem chia trai" },
            new Frame { BodyView = "Side", Mirror = true, Body = side, Gear = new Pose { SX = 15, SY = 16, SD = 2, SFront = false, OX = 20, OY = 22, OFront = true, OView = "Side" }, Note = "xoay 4: kiem chia ra xa (sau than)" },
        };
    }

    public static Frame[] Bulwark(string v)
    {
        if (v == "Down") return new[] { new Frame { BodyView = v, Body = Crouch(v), Gear = P(9, 23, 3, false, 16, 22, true), Note = "thu: khien giua than, kiem sau khien" } };
        if (v == "Up") return new[] { new Frame { BodyView = v, Body = Crouch(v), Gear = P(22, 22, 1, true, 16, 18, false), Note = "thu ve phia truoc" } };
        return new[] { new Frame { BodyView = v, Body = Crouch(v), Gear = P(12, 22, 3, false, 23, 21, true), Note = "thu: khien chan truoc mat" } };
    }

    public static Frame[] Dawnbreaker(string v)
    {
        if (v == "Down") return new[]
        {
            new Frame { BodyView = v, Body = EquipGen.Grid(EquipGen.ArenBase(v)), Gear = P(13, 17, 1, false, 21, 23, true), Note = "gio kiem cheo qua vai" },
            new Frame { BodyView = v, Body = Lunge(v, 1), Gear = P(17, 26, 7, true, 21, 24, true), Note = "chem bo xuong" },
            new Frame { BodyView = v, Body = Lunge(v, 1), Gear = P(10, 26, 5, true, 21, 24, true), Note = "vung theo" },
        };
        if (v == "Up") return new[]
        {
            new Frame { BodyView = v, Body = EquipGen.Grid(EquipGen.ArenBase(v)), Gear = P(19, 17, 2, true, 10, 23, true), Note = "gio kiem qua dau" },
            new Frame { BodyView = v, Body = Lunge(v, 1), Gear = P(16, 13, 2, false, 10, 24, true), Note = "chem ve phia truoc (xa camera)" },
            new Frame { BodyView = v, Body = Lunge(v, 1), Gear = P(12, 17, 3, false, 10, 24, true), Note = "vung theo" },
        };
        return new[]
        {
            new Frame { BodyView = v, Body = EquipGen.Grid(EquipGen.ArenBase(v)), Gear = P(12, 17, 3, false, 21, 23, true), Note = "gio kiem cheo qua vai" },
            new Frame { BodyView = v, Body = Lean(Lunge(v, 1)), Gear = P(21, 22, 0, true, 19, 24, true), Note = "chem ngang ve truoc" },
            new Frame { BodyView = v, Body = Lunge(v, 1), Gear = P(20, 26, 7, true, 20, 24, true), Note = "vung theo xuong thap" },
        };
    }

    // ================= export =================
    static readonly int[] MirrorDir = { 4, 3, 2, 1, 0, 7, 6, 5 };
    static readonly string[] Views = { "Down", "Up", "Side" };

    // sheet rows: ShieldBash D/U/S (2f), Charge D/U/S (2f), Bulwark D/U/S (1f), Dawnbreaker D/U/S (3f), Whirl (4f)
    public class SheetRow { public string Skill, View; public Frame[] Frames; }
    public static List<SheetRow> Rows()
    {
        var rows = new List<SheetRow>();
        foreach (var skill in new[] { "ShieldBash", "Charge", "Bulwark", "Dawnbreaker" })
            foreach (var v in Views)
            {
                Frame[] fs = skill == "ShieldBash" ? ShieldBash(v) : skill == "Charge" ? Charge(v) : skill == "Bulwark" ? Bulwark(v) : Dawnbreaker(v);
                if (v == "Side") foreach (var f in fs) f.Mirror = true;   // sheet convention: Side faces left
                rows.Add(new SheetRow { Skill = skill, View = v, Frames = fs });
            }
        rows.Add(new SheetRow { Skill = "Whirl", View = null, Frames = Whirl() });
        return rows;
    }

    static string Csv(string key, Frame fr)
    {
        var g = fr.Gear; bool m = fr.Mirror; string view = g.OView ?? fr.BodyView;
        int sx = m ? 31 - g.SX : g.SX, sd = m ? MirrorDir[g.SD] : g.SD;
        int hx = m ? 31 - g.SX : g.SX - 1, hy = g.SY - 1;
        int ox = m ? 31 - g.OX : g.OX;
        // key,main_x,main_y,main_dir,main_order,hand_x,hand_y,off_x,off_y,off_view,off_order,slash,slash_order
        return string.Format("{0},{1},{2},{3},{4},{5},{6},{7},{8},{9},{10},-1,0\n", key, sx, g.SY, sd, g.SFront ? 2 : -2, hx, hy, ox, g.OY, view, g.OFront ? 1 : -1);
    }

    public static string Export(string outDir)
    {
        System.IO.Directory.CreateDirectory(outDir);
        var rows = Rows();
        var sheet = new Bitmap(128, 32 * rows.Count, PixelFormat.Format32bppArgb);
        var csv = new System.Text.StringBuilder("key,main_x,main_y,main_dir,main_order,hand_x,hand_y,off_x,off_y,off_view,off_order,slash,slash_order\n");
        var layout = new System.Text.StringBuilder("row,skill,view,frames,keys\n");
        for (int r = 0; r < rows.Count; r++)
        {
            var row = rows[r]; var keys = new List<string>();
            for (int f = 0; f < row.Frames.Length; f++)
            {
                var fr = row.Frames[f];
                var body = EquipGen.Body(fr.Body, Pal);
                for (int y = 0; y < 32; y++) for (int x = 0; x < 32; x++)
                        if (body[x, y].HasValue) sheet.SetPixel(f * 32 + (fr.Mirror ? 31 - x : x), r * 32 + y, body[x, y].Value);
                string key = "Skill_" + row.Skill + (row.View != null ? "_" + row.View : "") + "_" + f;
                csv.Append(Csv(key, fr)); keys.Add(key);
            }
            layout.AppendFormat("{0},{1},{2},{3},{4}\n", r, row.Skill, row.View ?? "Any", row.Frames.Length, string.Join(" ", keys));
        }
        sheet.Save(System.IO.Path.Combine(outDir, "Aren_SkillSheet.png"), ImageFormat.Png); sheet.Dispose();
        System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "Aren_SkillAnchors.csv"), csv.ToString(), new System.Text.UTF8Encoding(false));
        System.IO.File.WriteAllText(System.IO.Path.Combine(outDir, "AREN_SKILL_LAYOUT.csv"), layout.ToString(), new System.Text.UTF8Encoding(false));
        return "rows " + rows.Count;
    }

    public static Bitmap Compose(Frame fr)
    {
        var body = EquipGen.Body(fr.Body, Pal);
        var g = fr.Gear; var view = g.OView ?? fr.BodyView;
        var sword = new EquipGen.Layer { S = EquipGen.Blade(EquipGen.SteelLongsword, g.SD), X = g.SX, Y = g.SY };
        var hand = new EquipGen.Layer { S = EquipGen.Gauntlet, X = g.SX - 1, Y = g.SY - 1 };
        var shield = new EquipGen.Layer { S = EquipGen.HeaterShield(view), X = g.OX, Y = g.OY };
        var behind = new List<EquipGen.Layer>(); var front = new List<EquipGen.Layer>();
        if (g.OFront) front.Add(shield); else behind.Add(shield);
        if (g.SFront) { front.Add(sword); front.Add(hand); } else { behind.Insert(0, hand); behind.Insert(0, sword); }
        var bmp = EquipGen.Compose(body, behind, front);
        if (!fr.Mirror) return bmp;
        var o = new Bitmap(48, 48, PixelFormat.Format32bppArgb);
        for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++) o.SetPixel(47 - x, y, bmp.GetPixel(x, y));
        return o;
    }
}
