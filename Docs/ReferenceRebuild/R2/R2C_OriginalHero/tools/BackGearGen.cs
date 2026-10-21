using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;

// Idle / Walk / Hurt: weapons slung on the back for Aren, Brom, Selene (Lyra and Vex already do this).
// Up = full gear on the back (in front of the body); Side = behind the back edge; Down = hidden by the body, a piece
// peeks over the shoulder (drawn behind the body). Attack frames keep the weapon in hand (unchanged).
public static class BackGearGen
{
    public class G { public EquipGen.Spr S; public int X, Y; public bool Front; }
    static G L(EquipGen.Spr s, int x, int y, bool front) { return new G { S = s, X = x, Y = y, Front = front }; }

    // gear list in draw order (earlier = further back); coordinates in the 32x32 body cell, right-facing art
    public static List<G> Gear(string cls, string v, int dy)
    {
        var l = new List<G>();
        if (cls == "Aren")
        {
            var sword = EquipGen.SteelLongsword;
            if (v == "Up") { l.Add(L(EquipGen.Blade(sword, 7), 12, 16 + dy, true)); l.Add(L(EquipGen.HeaterShield("Down"), 16, 21 + dy, true)); }
            else if (v == "Side") { l.Add(L(EquipGen.Blade(sword, 7), 6, 15 + dy, false)); l.Add(L(EquipGen.HeaterShield("Side"), 9, 21 + dy, false)); }
            else l.Add(L(ArenHiltPeek(), 21, 16 + dy, false));
        }
        else if (cls == "Brom")
        {
            // hammer head lands over the right shoulder (Up / Down) or above the back shoulder (Side), clear of the shield
            if (v == "Up") { l.Add(L(BromKit.Hammer(1), 14, 22 + dy, true)); l.Add(L(BromGen.Shield("Down"), 16, 22 + dy, true)); }
            else if (v == "Side") { l.Add(L(BromKit.Hammer(3), 17, 24 + dy, false)); l.Add(L(BromGen.Shield("Side"), 9, 22 + dy, false)); }
            else l.Add(L(BromKit.Hammer(1), 15, 22 + dy, false));
        }
        else // Selene
        {
            if (v == "Up") l.Add(L(SeleneGen.Staff(1, false), 11, 26 + dy, true));
            else if (v == "Side") l.Add(L(SeleneGen.Staff(3, false), 17, 27 + dy, false));
            else l.Add(L(SeleneGen.Staff(1, false), 11, 27 + dy, false));
        }
        return l;
    }

    // Aren Down view: sword hilt (gold pommel + guard, navy grip) peeking over the right shoulder, pointing up-right
    public static EquipGen.Spr ArenHiltPeek()
    {
        var w = EquipGen.SteelLongsword; var s = new EquipGen.Spr();
        s.Set(0, 0, EquipGen.Hex(w.Wrap)); s.Set(1, -1, EquipGen.Hex(w.Wrap)); s.Set(2, -2, EquipGen.Hex(w.Pommel));
        s.Set(-1, 0, EquipGen.Hex(w.Guard)); s.Set(0, 1, EquipGen.Hex(w.Guard)); s.Set(-1, 1, EquipGen.Hex(w.Core));
        return EquipGen.Outline(s, EquipGen.Hex(w.Outline));
    }

    public static char[,] Body(string cls, string anim, string v, int f)
    {
        if (cls == "Aren") return EquipGen.ArenMoveBody(anim, v, f);
        if (cls == "Brom") return anim == "Walk" ? BromKit.Walk(v, f) : BromKit.Idle(v, f);
        return anim == "Walk" ? SeleneKit.Walk(v, f) : SeleneKit.Idle(v, f);
    }

    public static Dictionary<char, string> Pal(string cls)
    {
        if (cls == "Aren") return HeroConceptGen.Roster.First(r => r.Id == "4").Pal;
        if (cls == "Brom") return BromGen.Pal;
        return SeleneGen.Pal;
    }

    // ================= anchor patch (Idle / Walk / Hurt rows only) =================
    static readonly int[] MirrorDir = { 4, 3, 2, 1, 0, 7, 6, 5 };
    public const int ArenHiltPeekCell = 8;   // new 9th cell in Aren_Sword.png

    // one CSV row (sheet orientation: Side mirrored). Aren = 13 columns, Brom / Selene = 14 (off_dir -1). hand = -99.
    public static string Row(string cls, string key, string v, int dy)
    {
        bool m = v == "Side";
        int mx = -99, my = 0, md = 0, mo = 0, ox = -99, oy = 0, oo = 0; string oview = v;
        Func<int, int> X = x => m ? 31 - x : x;
        Func<int, int> D = d => m ? MirrorDir[d] : d;
        if (cls == "Aren")
        {
            if (v == "Up") { mx = 12; my = 16; md = 7; mo = 1; ox = 16; oy = 21; oview = "Down"; oo = 2; }
            else if (v == "Side") { mx = X(6); my = 15; md = D(7); mo = -2; ox = X(9); oy = 21; oo = -1; }
            else { mx = 21; my = 16; md = ArenHiltPeekCell; mo = -2; }
        }
        else if (cls == "Brom")
        {
            if (v == "Up") { mx = 14; my = 22; md = 1; mo = 1; ox = 16; oy = 22; oview = "Down"; oo = 2; }
            else if (v == "Side") { mx = X(17); my = 24; md = D(3); mo = -2; ox = X(9); oy = 22; oo = -1; }
            else { mx = 15; my = 22; md = 1; mo = -2; }
        }
        else
        {
            // Selene: main_dir = staff sheet index (len 0 long -> 0..7)
            if (v == "Up") { mx = 11; my = 26; md = 1; mo = 2; }
            else if (v == "Side") { mx = X(17); my = 27; md = D(3); mo = -2; }
            else { mx = 11; my = 27; md = 1; mo = -2; }
        }
        my += dy; if (ox > -99) oy += dy;
        string tail = cls == "Aren" ? "" : ",-1";
        return string.Format("{0},{1},{2},{3},{4},-99,0,{5},{6},{7},{8},-1,0{9}", key, mx, my, md, mo, ox, oy, oview, oo, tail);
    }

    public static List<string> Rows(string cls)
    {
        var rows = new List<string>();
        foreach (var anim in new[] { "Idle", "Walk" })
            foreach (var v in new[] { "Down", "Up", "Side" })
                for (int f = 0; f < 4; f++) rows.Add(Row(cls, anim + "_" + v + "_" + f, v, anim == "Walk" && (f == 0 || f == 2) ? 1 : 0));
        foreach (var v in new[] { "Down", "Up", "Side" }) { rows.Add(Row(cls, "Hurt_" + v + "_0", v, 0)); rows.Add(Row(cls, "Hurt_" + v + "_1", v, 1)); }
        return rows;
    }

    public static Bitmap Compose(string cls, string anim, string v, int f, bool mirror)
    {
        int dy = anim == "Walk" && (f == 0 || f == 2) ? 1 : 0;
        var body = EquipGen.Body(Body(cls, anim, v, f), Pal(cls));
        var behind = new List<EquipGen.Layer>(); var front = new List<EquipGen.Layer>();
        foreach (var g in Gear(cls, v, dy)) (g.Front ? front : behind).Add(new EquipGen.Layer { S = g.S, X = g.X, Y = g.Y });
        var bmp = EquipGen.Compose(body, behind, front);
        if (!mirror) return bmp;
        var o = new Bitmap(48, 48, PixelFormat.Format32bppArgb);
        for (int y = 0; y < 48; y++) for (int x = 0; x < 48; x++) o.SetPixel(47 - x, y, bmp.GetPixel(x, y));
        return o;
    }
}
