using System;
using System.Drawing;

// Owner redesign 2026-11-03: Cat (cute, haughty, lovable), Wolf (fierce, cold), Baby Dragon (flies, imposing).
public static partial class PetSpecies
{
    // Cat: silver tabby, big blue eyes, pink cheeks
    static readonly Color CtF = C(0xB8B8C6), CtD = C(0x8E8EA0), CtS = C(0x7A7A90), CtB = C(0xF2EEEC), CtP = C(0xF0A0B4), CtE = C(0x6AB8F0);
    // Wolf: dark slate fur, icy mask, glowing ice-blue eyes, red mouth for the snarl
    static readonly Color WfF = C(0x4E5666), WfD = C(0x323844), WfL = C(0xA8B6C8), WfE = C(0x9AF0FF), WfM = C(0x8A2A30);
    // Dragon: obsidian-indigo scales, gold belly and ridge, crimson wings, ivory horns, ember eyes
    static readonly Color DrO = C(0x3C4062), DrOd = C(0x262842), DrG = C(0xE0B050), DrGd = C(0xA87E2C), DrM = C(0xB8303E), DrMd = C(0x781C2A), DrH = C(0xF0E0B0), DrE = C(0xFF7A3A);

    // ---------- small drawing helpers ----------
    static double Edge(double ax, double ay, double bx, double by, double px, double py) { return (bx - ax) * (py - ay) - (by - ay) * (px - ax); }

    static void Line(D d, int x0, int y0, int x1, int y1, Color c)
    {
        int n = Math.Max(Math.Abs(x1 - x0), Math.Abs(y1 - y0));
        for (int s = 0; s <= n; s++)
            d.S(x0 + (int)Math.Round((x1 - x0) * (double)s / Math.Max(1, n)), y0 + (int)Math.Round((y1 - y0) * (double)s / Math.Max(1, n)), c);
    }

    static void Tri(D d, int x0, int y0, int x1, int y1, int x2, int y2, Color c)
    {
        int minX = Math.Min(x0, Math.Min(x1, x2)), maxX = Math.Max(x0, Math.Max(x1, x2));
        int minY = Math.Min(y0, Math.Min(y1, y2)), maxY = Math.Max(y0, Math.Max(y1, y2));
        for (int y = minY; y <= maxY; y++)
            for (int x = minX; x <= maxX; x++)
            {
                double px = x + 0.5, py = y + 0.5;
                double e0 = Edge(x0 + .5, y0 + .5, x1 + .5, y1 + .5, px, py), e1 = Edge(x1 + .5, y1 + .5, x2 + .5, y2 + .5, px, py), e2 = Edge(x2 + .5, y2 + .5, x0 + .5, y0 + .5, px, py);
                if ((e0 >= 0 && e1 >= 0 && e2 >= 0) || (e0 <= 0 && e1 <= 0 && e2 <= 0)) d.S(x, y, c);
            }
        Line(d, x0, y0, x1, y1, c); Line(d, x1, y1, x2, y2, c); Line(d, x2, y2, x0, y0, c);
    }

    /// <summary>Big chibi eye: highlight top-left, iris bottom-left, dark right column.</summary>
    static void Eye2(D d, int x, int y, Color iris) { d.S(x, y, White); d.S(x + 1, y, Nose); d.S(x, y + 1, iris); d.S(x + 1, y + 1, Nose); }
    static void Closed(D d, int x, int y) { d.S(x, y + 1, Nose); d.S(x + 1, y, Nose); d.S(x + 2, y + 1, Nose); }   // ^ shape

    // =====================================================================
    // CAT: big head, big eyes, tail held high with a hook. Sit: chin up, eyes shut (col 4), one eye peeks (col 5)
    // =====================================================================
    static Bitmap CatSide(int f)
    {
        var d = new D();
        if (f < 4)
        {
            int bob = (f == 1 || f == 3) ? 1 : 0, by = 9 + bob, tip = f == 1 ? 1 : (f == 3 ? -1 : 0);
            // proud tail, straight up with a hook at the top
            d.S(7, by + 1, CtF); d.VL(6, by - 6, by + 1, CtF); d.S(6, by - 2, CtS); d.S(6, by - 4, CtS);
            d.S(7 + tip, by - 7, CtF); d.S(8 + tip, by - 7, CtF); d.S(8 + tip, by - 6, CtF);
            d.R(7, by + 1, 9, 4, CtF); d.S(9, by + 1, CtS); d.S(11, by + 1, CtS); d.S(13, by + 1, CtS); d.HL(9, 14, by + 4, CtB);
            Legs4(d, f, new[] { 8, 10, 13, 15 }, by + 5, CtF, CtD, Color.Empty);
            d.R(14, by - 5, 7, 6, CtF);
            d.S(14, by - 6, CtF); d.S(14, by - 7, CtF); d.S(15, by - 6, CtP); d.S(19, by - 6, CtP); d.S(20, by - 6, CtF); d.S(20, by - 7, CtF);
            d.S(16, by - 5, CtS); d.S(17, by - 5, CtS); d.S(16, by - 4, CtS);
            Eye2(d, 18, by - 3, CtE);
            d.S(17, by - 1, CtP); d.S(21, by - 2, CtP); d.S(20, by, CtB); d.S(21, by - 1, CtB);
        }
        else
        {
            int wag = f == 5 ? 1 : 0;
            d.VL(7, 8, 16, CtF); d.S(8, 7, CtF); d.S(9, 7 - wag, CtF); d.S(9, 6 - wag, CtF); d.S(7, 10, CtS); d.S(7, 13, CtS);
            d.R(9, 10, 7, 7, CtF); d.S(10, 11, CtS); d.S(10, 13, CtS); d.R(13, 11, 3, 5, CtB);
            d.R(9, 17, 4, 1, CtF); d.HL(10, 12, 18, CtF); d.VL(14, 17, 18, CtF); d.VL(16, 17, 18, CtF);
            d.R(12, 3, 7, 6, CtF);
            d.S(12, 2, CtF); d.S(12, 1, CtF); d.S(13, 2, CtP); d.S(17, 2, CtP); d.S(18, 2, CtF); d.S(18, 1, CtF);
            d.S(14, 3, CtS); d.S(15, 3, CtS); d.S(14, 4, CtS);
            if (f == 4) Closed(d, 15, 5); else Eye2(d, 15, 5, CtE);
            d.S(15, 7, CtP); d.S(19, 5, CtP); d.S(18, 6, CtB); d.S(18, 7, CtB);
        }
        return d.Done();
    }

    static Bitmap CatFB(int dir, int f)
    {
        var d = new D();
        bool sit = f >= 4, front = dir == 0;
        int bob = !sit && (f == 1 || f == 3) ? 1 : 0, ph = f % 2, w = sit && f == 5 ? 1 : 0;
        int by = (sit ? 11 : 9) + bob, bh = sit ? 7 : 6, hy = by - 6;
        d.R(8, by, 8, bh, CtF);
        if (front) d.R(10, by + 1, 4, bh - 1, CtB); else { d.HL(9, 14, by + 1, CtS); d.HL(9, 14, by + 3, CtS); }
        if (!sit) { d.VL(9, by + bh, 18 - ph, CtF); d.VL(14, by + bh, 17 + ph, CtF); d.VL(11, by + bh, 17 + ph, CtD); d.VL(12, by + bh, 18 - ph, CtD); }
        else { d.HL(9, 10, 18, CtF); d.HL(13, 14, 18, CtF); }
        if (!front)
        {
            // tail up beside the head, hooked
            d.S(16, by + 3, CtF); d.S(17, by + 2, CtF); d.VL(18 + w, by - 4, by + 1, CtF); d.S(17 + w, by - 5, CtF); d.S(18 + w, by - 1, CtS);
        }
        d.R(6, hy, 12, 7, CtF);
        d.S(6, hy - 1, CtF); d.S(6, hy - 2, CtF); d.S(7, hy - 1, CtF); d.S(17, hy - 1, CtF); d.S(17, hy - 2, CtF); d.S(16, hy - 1, CtF);
        if (front)
        {
            d.S(7, hy - 1, CtP); d.S(16, hy - 1, CtP);
            d.S(10, hy, CtS); d.S(11, hy, CtS); d.S(12, hy, CtS); d.S(13, hy, CtS); d.S(11, hy + 1, CtS); d.S(12, hy + 1, CtS);
            if (sit && f == 4) { Closed(d, 8, hy + 3); Closed(d, 13, hy + 3); }
            else if (sit) { Closed(d, 8, hy + 3); Eye2(d, 14, hy + 3, CtE); }
            else { Eye2(d, 8, hy + 3, CtE); Eye2(d, 14, hy + 3, CtE); }
            d.S(7, hy + 5, CtP); d.S(16, hy + 5, CtP); d.S(11, hy + 5, CtP); d.S(12, hy + 5, CtP); d.HL(10, 13, hy + 6, CtB);
        }
        else { d.HL(8, 15, hy + 2, CtS); d.HL(9, 14, hy + 4, CtS); }
        return d.Done();
    }

    // =====================================================================
    // WOLF: stalks with the head low, hackles up, slanted brows, icy eyes, fang. Sit: cold stare (col 4), snarl (col 5)
    // =====================================================================
    static Bitmap WolfSide(int f)
    {
        var d = new D();
        if (f < 4)
        {
            int bob = (f == 1 || f == 3) ? 1 : 0, by = 8 + bob;
            d.R(1, by, 4, 2, WfF); d.R(1, by + 2, 2, 1, WfD);
            d.R(4, by - 1, 13, 7, WfF); d.HL(5, 15, by - 1, WfD); d.HL(6, 13, by, WfD); d.R(7, by + 3, 9, 3, WfL);
            d.R(11, by - 2, 5, 1, WfF); d.S(12, by - 3, WfD); d.S(14, by - 3, WfD); d.S(16, by - 2, WfF);   // hackles
            Legs4(d, f, new[] { 6, 8, 14, 16 }, by + 6, WfF, WfD, Color.Empty);
            d.R(16, by - 3, 5, 5, WfF); d.R(20, by - 1, 3, 3, WfL); d.S(22, by - 1, Nose); d.S(17, by, WfL); d.S(18, by + 1, WfL);
            d.S(21, by + 2, White);                                                                             // fang
            d.S(18, by - 2, WfE); d.S(17, by - 3, WfD); d.S(18, by - 3, WfD); d.S(19, by - 2, WfD);            // icy eye under a hard brow
            d.S(16, by - 4, WfF); d.S(16, by - 5, WfD); d.S(17, by - 4, WfF); d.S(18, by - 4, WfF); d.S(18, by - 5, WfD);
        }
        else
        {
            bool snarl = f == 5;
            d.R(2, 15, 5, 2, WfF); d.HL(2, 3, 16, WfD);
            d.R(7, 6, 10, 11, WfF); d.HL(7, 13, 6, WfD); d.R(14, 8, 3, 7, WfL); d.S(13, 7, WfL);
            d.S(9, 5, WfD); d.S(11, 5, WfD);
            d.HL(8, 12, 17, WfF); d.HL(8, 12, 18, WfF); d.VL(15, 17, 18, WfF); d.VL(17, 17, 18, WfF);
            d.R(14, 2, 6, 6, WfF); d.R(19, 5, 2, 2, WfL); d.S(21, 5, Nose); d.S(15, 6, WfL); d.S(16, 7, WfL);
            d.S(17, 4, WfE); d.S(16, 3, WfD); d.S(17, 3, WfD); d.S(18, 4, WfD);
            d.S(14, 1, WfD); d.S(15, 1, WfF); d.S(17, 1, WfD); d.S(18, 1, WfF);
            if (snarl) { d.S(19, 7, WfM); d.S(20, 7, White); d.S(20, 4, WfD); }
        }
        return d.Done();
    }

    static Bitmap WolfFB(int dir, int f)
    {
        var d = new D();
        bool sit = f >= 4, front = dir == 0;
        int bob = !sit && (f == 1 || f == 3) ? 1 : 0;
        int by = (sit ? 10 : 8) + bob, hy = by - 5, bh = sit ? 8 : 7, w = sit && f == 5 ? 1 : 0;
        d.R(6, by - 1, 12, bh + 1, WfF);
        d.S(6, by - 2, WfD); d.S(7, by - 2, WfF); d.S(16, by - 2, WfF); d.S(17, by - 2, WfD);                // hackles
        if (front) d.R(9, by, 6, bh - 1, WfL); else d.R(7, by - 1, 10, 3, WfD);
        LegsFB(d, by, f, sit, 7, 16, WfF, WfD, Color.Empty);
        d.R(8, hy, 8, 7, WfF);
        d.S(8, hy - 1, WfF); d.S(8, hy - 2, WfD); d.S(9, hy - 1, WfF); d.S(15, hy - 1, WfF); d.S(15, hy - 2, WfD); d.S(14, hy - 1, WfF);
        if (front)
        {
            d.R(10, hy + 4, 4, 3, WfL);
            d.S(9, hy + 2, WfD); d.S(10, hy + 2, WfD); d.S(11, hy + 3, WfD); d.S(12, hy + 3, WfD); d.S(13, hy + 2, WfD); d.S(14, hy + 2, WfD);
            d.S(10, hy + 3, WfE); d.S(13, hy + 3, WfE);
            d.S(11, hy + 4, Nose); d.S(12, hy + 4, Nose);
            d.S(10, hy + 6, White); d.S(13, hy + 6, White);
            if (sit && f == 5) { d.S(11, hy + 6, WfM); d.S(12, hy + 6, WfM); d.S(11, hy + 5, WfD); d.S(12, hy + 5, WfD); }
        }
        else { d.HL(9, 14, hy, WfD); d.S(8, hy - 2, WfL); d.S(15, hy - 2, WfL); d.HL(7, 16, by + 2, WfF); d.R(10 + w, by + 3, 4, 6, WfF); d.VL(10 + w, by + 3, by + 7, WfD); d.HL(10 + w, 13 + w, by + 8, WfL); }
        return d.Done();
    }

    // =====================================================================
    // BABY DRAGON: flies. Cols 0-3 = flight loop (wings up, mid, down, mid; the body never touches the ground).
    // Cols 4-5 = landed pose, chest out: wings half-folded (col 4) then flared with an ember breath (col 5).
    // =====================================================================
    static void WingSide(D d, int sx, int sy, int ph, Color mem, Color bone)
    {
        int tx, ty;
        if (ph == 0) { tx = sx - 5; ty = sy - 9; Tri(d, sx, sy, tx, ty, sx - 9, sy - 2, mem); Line(d, tx, ty, sx - 7, sy - 2, bone); }
        else if (ph == 1) { tx = sx - 10; ty = sy - 5; Tri(d, sx, sy, tx, ty, sx - 9, sy + 1, mem); Line(d, tx, ty, sx - 6, sy + 1, bone); }
        else { tx = sx - 7; ty = sy + 6; Tri(d, sx, sy, tx, ty, sx - 2, sy + 5, mem); Line(d, sx - 4, sy + 3, sx - 3, sy + 5, bone); }
        Line(d, sx, sy, tx, ty, bone);
        d.S(tx, ty, DrH);   // wing claw
    }

    static Bitmap DragonSide(int f)
    {
        var d = new D();
        if (f < 4)
        {
            int[] lift = { 1, 0, -1, 0 };
            int by = 10 + lift[f], ph = f == 0 ? 0 : (f == 2 ? 2 : 1);
            WingSide(d, 15, by - 1, ph, DrMd, DrOd);                                                       // far wing
            d.R(3, by + 1, 6, 2, DrO); d.HL(4, 8, by + 2, DrOd);
            d.S(2, by + 1, DrM); d.S(1, by + 1, DrM); d.S(2, by, DrM); d.S(2, by + 2, DrM);                // spade tail
            d.R(8, by, 9, 4, DrO); d.R(10, by + 2, 6, 2, DrG); d.HL(10, 15, by + 3, DrGd);
            d.S(9, by - 1, DrG); d.S(11, by - 1, DrG);
            d.R(16, by - 2, 2, 3, DrO); d.R(17, by - 4, 5, 3, DrO); d.R(21, by - 3, 2, 2, DrO); d.HL(19, 22, by - 2, DrOd);
            d.S(22, by - 3, DrOd);
            d.S(19, by - 3, DrE); d.S(18, by - 4, DrOd); d.S(19, by - 4, DrOd);
            d.S(17, by - 5, DrH); d.S(16, by - 6, DrH); d.S(15, by - 7, DrH); d.S(19, by - 5, DrH); d.S(18, by - 6, DrH);
            d.S(10, by + 4, DrOd); d.S(11, by + 5, DrOd); d.S(15, by + 4, DrOd); d.S(16, by + 5, DrH);    // tucked claws
            WingSide(d, 13, by, ph, DrM, DrOd);                                                            // near wing
        }
        else
        {
            bool flare = f == 5;
            if (!flare) { Tri(d, 11, 9, 5, 3, 4, 12, DrM); Line(d, 11, 9, 5, 3, DrOd); Line(d, 5, 3, 6, 11, DrOd); d.S(5, 3, DrH); }
            else { Tri(d, 11, 9, 2, 1, 1, 11, DrM); Line(d, 11, 9, 2, 1, DrOd); Line(d, 2, 1, 4, 10, DrOd); Line(d, 2, 1, 1, 7, DrOd); d.S(2, 1, DrH); }
            d.R(3, 16, 7, 2, DrO); d.S(2, 15, DrM); d.S(1, 16, DrM); d.S(2, 17, DrM); d.S(2, 16, DrM);
            d.R(8, 9, 7, 8, DrO); d.R(12, 10, 3, 7, DrG); d.HL(12, 14, 12, DrGd); d.HL(12, 14, 14, DrGd);
            d.R(8, 17, 4, 2, DrO); d.VL(13, 17, 18, DrOd); d.VL(15, 17, 18, DrOd); d.S(16, 18, DrH);
            d.R(13, 5, 3, 4, DrO); d.S(12, 5, DrG); d.S(12, 7, DrG);
            d.R(14, 2, 6, 3, DrO); d.R(19, 3, 3, 2, DrO); d.HL(18, 21, 4, DrOd);
            d.S(17, 3, DrE); d.S(16, 2, DrOd); d.S(17, 2, DrOd);
            d.S(15, 1, DrH); d.S(14, 1, DrH); d.S(13, 1, DrH); d.S(17, 1, DrH);
            if (flare) { d.S(22, 3, DrE); d.S(22, 2, DrG); }
        }
        return d.Done();
    }

    /// <summary>Front/back head dressing: cut top corners, swept-back horns, cheek spikes.</summary>
    static void DragonHead(D d, int l, int r, int top, int bottom)
    {
        var clear = Color.FromArgb(0, 0, 0, 0);
        d.S(l, top, clear); d.S(r, top, clear); d.S(l, bottom, clear); d.S(r, bottom, clear);
        d.S(l, top - 1, DrH); d.S(l - 1, top - 1, DrH); d.S(l - 2, top - 2, DrH); d.S(l - 2, top - 3, DrH);
        d.S(r, top - 1, DrH); d.S(r + 1, top - 1, DrH); d.S(r + 2, top - 2, DrH); d.S(r + 2, top - 3, DrH);
        d.S(l - 1, top + 2, DrOd); d.S(l - 1, top + 3, DrOd); d.S(r + 1, top + 2, DrOd); d.S(r + 1, top + 3, DrOd);
    }

    static int Mx(int x) { return CW - 1 - x; }

    static void WingPair(D d, int sx, int sy, int[] pts, int[] rib)
    {
        // pts: tip x,y then trailing corner x,y (left wing); right wing is mirrored
        for (int side = 0; side < 2; side++)
        {
            Func<int, int> X = x => side == 0 ? x : Mx(x);
            Tri(d, X(sx), sy, X(pts[0]), pts[1], X(pts[2]), pts[3], DrM);
            Line(d, X(rib[0]), rib[1], X(rib[2]), rib[3], DrOd);
            Line(d, X(sx), sy, X(pts[0]), pts[1], DrOd);
            d.S(X(pts[0]), pts[1], DrH);
        }
    }

    static void WingsFlight(D d, int by, int ph)
    {
        if (ph == 0) WingPair(d, 9, by + 1, new[] { 2, by - 6, 1, by + 1 }, new[] { 2, by - 6, 4, by + 1 });
        else if (ph == 1) WingPair(d, 9, by + 1, new[] { 1, by - 2, 2, by + 4 }, new[] { 1, by - 2, 4, by + 3 });
        else WingPair(d, 9, by + 1, new[] { 1, by + 6, 5, by + 7 }, new[] { 5, by + 3, 4, by + 7 });
    }

    static Bitmap DragonFB(int dir, int f)
    {
        var d = new D();
        bool front = dir == 0, sit = f >= 4;
        if (!sit)
        {
            int[] lift = { 1, 0, -1, 0 };
            int by = 9 + lift[f], hy = by - 5, ph = f == 0 ? 0 : (f == 2 ? 2 : 1);
            if (front) WingsFlight(d, by, ph);                                     // wings behind the body
            d.R(9, by, 6, 6, DrO);
            if (front) { d.R(10, by + 1, 4, 5, DrG); d.HL(10, 13, by + 3, DrGd); d.S(16, by + 6, DrO); d.S(17, by + 7, DrO); d.S(18, by + 7, DrM); }
            else
            {
                d.S(11, by + 1, DrG); d.S(12, by + 3, DrG);
                d.VL(11, by + 6, by + 8, DrO); d.VL(12, by + 6, by + 8, DrO);
                d.HL(10, 13, by + 9, DrM); d.HL(11, 12, by + 10, DrM);
            }
            d.S(10, by + 6, DrOd); d.S(13, by + 6, DrOd);
            d.R(9, hy, 6, 5, DrO);
            DragonHead(d, 9, 14, hy, hy + 4);
            if (front)
            {
                d.S(10, hy + 2, DrE); d.S(13, hy + 2, DrE);
                d.S(10, hy + 1, DrOd); d.S(11, hy + 2, DrOd); d.S(13, hy + 1, DrOd); d.S(12, hy + 2, DrOd);
                d.S(11, hy + 4, DrOd); d.S(12, hy + 4, DrOd);
            }
            else { d.S(11, hy + 1, DrG); d.S(12, hy + 3, DrG); }
            if (!front) WingsFlight(d, by, ph);                                    // seen from behind, wings are in front
        }
        else
        {
            bool flare = f == 5;
            int by = 10, hy = 4, bh = 8;
            int[] pts = flare ? new[] { 1, by - 7, 1, by + 4 } : new[] { 3, by - 4, 4, by + 5 };
            int[] rib = flare ? new[] { 1, by - 7, 3, by + 3 } : new[] { 3, by - 4, 5, by + 4 };
            if (front) WingPair(d, 8, by + 1, pts, rib);
            d.R(8, by, 8, bh, DrO);
            if (front) { d.R(10, by + 1, 4, bh - 1, DrG); d.HL(10, 13, by + 3, DrGd); d.HL(10, 13, by + 5, DrGd); }
            else { d.S(11, by + 1, DrG); d.S(12, by + 3, DrG); d.S(11, by + 5, DrG); d.HL(14, 18, 17, DrO); d.S(19, 16, DrM); d.S(19, 17, DrM); d.S(20, 17, DrM); }
            d.HL(8, 10, 18, DrOd); d.HL(13, 15, 18, DrOd); d.S(8, 18, DrH); d.S(15, 18, DrH);
            d.R(8, hy, 8, 6, DrO);
            DragonHead(d, 8, 15, hy, hy + 5);
            if (front)
            {
                d.S(10, hy + 2, DrE); d.S(13, hy + 2, DrE);
                d.S(10, hy + 1, DrOd); d.S(11, hy + 2, DrOd); d.S(13, hy + 1, DrOd); d.S(12, hy + 2, DrOd);
                d.S(11, hy + 4, DrOd); d.S(12, hy + 4, DrOd);
                if (flare) { d.S(11, hy + 5, DrE); d.S(12, hy + 5, DrE); }
            }
            else { d.S(11, hy + 1, DrG); d.S(12, hy + 3, DrG); WingPair(d, 8, by + 1, pts, rib); }
        }
        return d.Done();
    }
}
