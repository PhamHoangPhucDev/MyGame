using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;

// Pet species sheets. Same grid as the farm dog (FM_Dog, Docs/WorldArt/HomeDistrict/farm):
// 6x3 cells of 24x20, rows 0 down, 1 up, 2 side (faces right), cols 0-3 walk, 4-5 sit.
// Same soft outline as the farm animals. No anti-aliasing, no semi-transparent pixels.
public static partial class PetSpecies
{
    public const int CW = 24, CH = 20;
    public static readonly string[] Names = { "Dog", "Cat", "Fox", "Rabbit", "Wolf", "BabyDragon" };

    static Color C(int rgb) { return Color.FromArgb(255, (rgb >> 16) & 255, (rgb >> 8) & 255, rgb & 255); }
    static readonly Color Out = C(0x3A2A26), Nose = C(0x2A2228), White = C(0xF4F0E8);

    class D
    {
        public Cv cv = new Cv(CW, CH);
        public void R(int x, int y, int w, int h, Color c) { for (int j = 0; j < h; j++) for (int i = 0; i < w; i++) cv.Set(x + i, y + j, c); }
        public void HL(int x0, int x1, int y, Color c) { for (int x = x0; x <= x1; x++) cv.Set(x, y, c); }
        public void VL(int x, int y0, int y1, Color c) { for (int y = Math.Min(y0, y1); y <= Math.Max(y0, y1); y++) cv.Set(x, y, c); }
        public void S(int x, int y, Color c) { cv.Set(x, y, c); }
        // row 0 is kept for the outline, so nothing pokes out of the top of the cell
        public Bitmap Done() { for (int x = 0; x < CW; x++) cv.Set(x, 0, Color.FromArgb(0, 0, 0, 0)); return Img.Outline(cv.B, Out); }
    }

    // ---------- palettes ----------
    // Dog: beagle (tri-colour, floppy ears), so it never reads as the shiba farm dog
    static readonly Color DgW = C(0xF2EEE4), DgWs = C(0xD8D0C4), DgSad = C(0x4A3A34), DgTan = C(0xC88A4A), DgTanD = C(0x9C6430);
    // Fox
    static readonly Color FxF = C(0xE07A30), FxD = C(0xB45820), FxSock = C(0x4A3430);
    // Rabbit: light brown with white belly and tail
    static readonly Color RbF = C(0xD8B890), RbD = C(0xB09068), RbP = C(0xEEA0AA);

    static void Legs4(D d, int f, int[] lx, int top, Color a, Color b, Color sock)
    {
        int ph = f % 2;
        for (int i = 0; i < 4; i++)
        {
            int off = ((i + ph) % 2 == 0) ? 0 : 1;
            int x = lx[i] + (i < 2 ? off : -off);
            d.VL(x, top, 18, i % 2 == 0 ? a : b);
            if (sock.A > 0) { d.S(x, 17, sock); d.S(x, 18, sock); }
        }
    }

    // ---------- side view (faces right) ----------
    public static Bitmap Side(int sp, int f)
    {
        if (sp == 1) return CatSide(f);
        if (sp == 4) return WolfSide(f);
        if (sp == 5) return DragonSide(f);
        return SideBase(sp, f);
    }

    static Bitmap SideBase(int sp, int f)
    {
        var d = new D();
        bool sit = f >= 4;
        int bob = !sit && (f == 1 || f == 3) ? 1 : 0, by = 8 + bob, wag = f == 5 ? 1 : 0;
        switch (sp)
        {
            case 0: // dog
                if (!sit)
                {
                    d.R(5, by, 12, 6, DgW); d.R(6, by, 9, 2, DgSad); d.HL(6, 16, by + 5, DgWs);
                    d.R(15, by - 5, 6, 6, DgTan); d.R(18, by - 2, 4, 3, DgW); d.VL(19, by - 5, by - 3, DgW); d.S(22, by - 2, Nose); d.S(18, by - 4, Nose);
                    d.R(15, by - 4, 2, 4, DgTanD);
                    d.VL(4, by - 4, by, DgSad); d.S(4, by - 5, DgW);
                    Legs4(d, f, new[] { 6, 9, 13, 15 }, by + 6, DgW, DgWs, Color.Empty);
                }
                else
                {
                    d.R(8, 8, 9, 9, DgW); d.R(8, 8, 7, 3, DgSad); d.HL(8, 16, 16, DgWs);
                    d.R(13, 2, 7, 7, DgTan); d.R(16, 5, 4, 3, DgW); d.VL(17, 2, 4, DgW); d.S(20, 5, Nose); d.S(16, 4, Nose);
                    d.R(13, 3, 2, 4, DgTanD);
                    d.VL(6 - wag, 7 - wag, 10, DgSad); d.S(6 - wag, 6 - wag, DgW);
                    d.VL(14, 17, 18, DgW); d.VL(16, 17, 18, DgW); d.HL(9, 12, 18, DgW);
                }
                break;
            case 2: // fox
                if (!sit)
                {
                    d.R(6, by, 10, 5, FxF); d.R(8, by + 3, 6, 2, White); d.HL(6, 7, by + 4, FxD);
                    d.R(15, by - 4, 5, 5, FxF); d.R(19, by - 2, 3, 1, FxF); d.R(19, by - 1, 3, 2, White); d.S(22, by - 2, Nose); d.S(16, by - 1, White); d.S(17, by, White); d.S(18, by - 3, Nose);
                    d.VL(15, by - 7, by - 5, FxF); d.S(16, by - 5, FxF); d.S(15, by - 7, FxSock); d.VL(18, by - 7, by - 5, FxF); d.S(19, by - 5, FxF); d.S(18, by - 7, FxSock);
                    d.R(1, by - 2, 5, 4, FxF); d.R(2, by - 3, 3, 1, FxF); d.R(1, by - 2, 2, 3, White); d.HL(3, 5, by + 1, FxD);
                    Legs4(d, f, new[] { 7, 9, 13, 15 }, by + 5, FxF, FxD, FxSock);
                }
                else
                {
                    d.R(8, 8, 8, 9, FxF); d.R(13, 10, 3, 6, White);
                    d.R(13, 3, 6, 6, FxF); d.R(17, 6, 3, 2, White); d.S(20, 6, Nose); d.S(16, 5, Nose);
                    d.VL(13, 0, 2, FxF); d.S(14, 2, FxF); d.S(13, 0, FxSock); d.VL(16, 0, 2, FxF); d.S(17, 2, FxF); d.S(16, 0, FxSock);
                    d.R(3, 13 - wag, 6, 4, FxF); d.R(3, 13 - wag, 2, 3, White);
                    d.VL(14, 17, 18, FxSock); d.VL(16, 17, 18, FxSock); d.HL(9, 12, 18, FxF);
                }
                break;
            case 3: // rabbit (hops: crouch, push, air, land)
                if (!sit)
                {
                    int[] hop = { 0, -2, -3, -1 };
                    int rb = 10 + hop[f];
                    d.R(7, rb, 7, 1, RbF); d.R(6, rb + 1, 9, 5, RbF); d.R(9, rb + 3, 5, 2, White);
                    d.R(14, rb - 3, 5, 5, RbF); d.S(19, rb - 1, RbF); d.S(19, rb - 1, RbP); d.S(16, rb - 2, Nose); d.S(17, rb, White);
                    int et = Math.Max(1, rb - 9);   // ears flatten back in the air instead of leaving the cell
                    d.VL(15, et, rb - 4, RbF); d.VL(16, et, rb - 4, RbP); d.VL(17, et + 1, rb - 4, RbD);
                    d.R(4, rb + 1, 2, 2, White);
                    if (f == 0) { d.R(6, rb + 6, 2, 2, RbF); d.HL(6, 9, 18, RbF); d.VL(13, rb + 6, 18, RbD); }
                    else if (f == 1) { d.VL(6, rb + 6, 18, RbF); d.VL(7, rb + 6, 18, RbF); d.HL(5, 6, 18, RbF); d.HL(13, 14, rb + 6, RbD); }
                    else if (f == 2) { d.HL(4, 7, rb + 6, RbF); d.HL(13, 15, rb + 6, RbD); }
                    else { d.HL(6, 8, rb + 6, RbF); d.VL(14, rb + 6, 18, RbD); d.S(15, 18, RbD); }
                }
                else
                {
                    int tw = wag;
                    d.R(7, 10, 7, 1, RbF); d.R(6, 11, 9, 6, RbF); d.R(10, 13, 4, 3, White);
                    d.R(13, 5, 6, 5, RbF); d.S(18, 7, RbP); d.S(15, 6, Nose); d.S(16, 8, White);
                    d.VL(14, 0, 4, RbF); d.VL(15, 0, 4, RbP); d.VL(16 + tw, 1 + tw, 4, RbD);
                    d.R(4, 13, 2, 2, White);
                    d.R(7, 17, 4, 2, RbF); d.VL(14, 17, 18, RbD);
                }
                break;
        }
        return d.Done();
    }

    // ---------- front (dir 0) and back (dir 1) ----------
    static void LegsFB(D d, int by, int f, bool sit, int l, int r, Color a, Color b, Color sock)
    {
        int ph = f % 2;
        if (!sit)
        {
            d.VL(l, by + 7, 18 - ph, a); d.VL(r, by + 7, 17 + ph, a); d.VL(l + 2, by + 7, 17 + ph, b); d.VL(r - 2, by + 7, 18 - ph, b);
            if (sock.A > 0) { d.S(l, 18 - ph, sock); d.S(r, 17 + ph, sock); d.S(l + 2, 17 + ph, sock); d.S(r - 2, 18 - ph, sock); }
        }
        else { d.HL(l, l + 2, by + 8, sock.A > 0 ? sock : a); d.HL(r - 2, r, by + 8, sock.A > 0 ? sock : a); }
    }

    public static Bitmap FrontBack(int sp, int dir, int f)
    {
        if (sp == 1) return CatFB(dir, f);
        if (sp == 4) return WolfFB(dir, f);
        if (sp == 5) return DragonFB(dir, f);
        return FrontBackBase(sp, dir, f);
    }

    static Bitmap FrontBackBase(int sp, int dir, int f)
    {
        var d = new D();
        bool sit = f >= 4, front = dir == 0;
        int bob = !sit && (f == 1 || f == 3) ? 1 : 0;
        // head overlaps the top of the body so ears stay inside the cell; sit feet land on row 18
        int by = (sit ? 10 : 8) + bob, hy = by - 5, bh = sit ? 8 : 7, w = sit && f == 5 ? 1 : 0;
        switch (sp)
        {
            case 0: // dog
                d.R(7, by, 10, bh, DgW); d.HL(7, 16, by + bh - 1, DgWs);
                if (!front) d.R(7, by, 10, 3, DgSad);
                LegsFB(d, by, f, sit, 8, 15, DgW, DgWs, Color.Empty);
                d.R(8, hy, 8, 7, DgTan);
                d.R(6, hy + 1, 2, 4, DgTanD); d.R(16, hy + 1, 2, 4, DgTanD);
                if (front) { d.VL(11, hy, hy + 2, DgW); d.VL(12, hy, hy + 2, DgW); d.R(10, hy + 3, 4, 3, DgW); d.S(11, hy + 3, Nose); d.S(12, hy + 3, Nose); d.S(9, hy + 2, Nose); d.S(14, hy + 2, Nose); d.S(12, hy + 5, C(0xC8403C)); }
                else { d.VL(12 + w, by - 2, by + 1, DgSad); d.S(12 + w, by - 3, DgW); }
                break;
            case 2: // fox
                d.R(7, by, 10, bh, FxF);
                if (front) d.R(9, by + 1, 6, bh - 2, White);
                LegsFB(d, by, f, sit, 8, 15, FxF, FxD, FxSock);
                d.R(8, hy + 1, 8, 6, FxF);
                d.VL(8, hy - 2, hy, FxF); d.S(9, hy, FxF); d.S(9, hy - 1, FxF); d.S(8, hy - 2, FxSock);
                d.VL(15, hy - 2, hy, FxF); d.S(14, hy, FxF); d.S(14, hy - 1, FxF); d.S(15, hy - 2, FxSock);
                if (front) { d.R(9, hy + 4, 6, 3, White); d.S(11, hy + 5, Nose); d.S(12, hy + 5, Nose); d.S(9, hy + 3, Nose); d.S(14, hy + 3, Nose); }
                else { d.R(9 + w, by + 2, 6, 6, FxF); d.HL(9 + w, 14 + w, by + 2, FxD); d.R(10 + w, by + 7, 4, 2, White); }
                break;
            case 3: // rabbit
            {
                int[] hop = { 0, -2, -3, -1 };
                int rb = 11 + (sit ? 0 : hop[f]), rh = rb - 7, et = Math.Max(1, rh - 3);
                d.R(7, rb + 1, 10, 6, RbF);
                if (front) d.R(9, rb + 2, 6, 4, White);
                d.HL(8, 10, rb + 7, RbF); d.HL(13, 15, rb + 7, RbF);
                d.R(8, rh + 2, 8, 6, RbF);
                if (front)
                {
                    d.VL(9, et, rh + 1, RbF); d.VL(10, et, rh + 1, RbP); d.VL(13, et + w, rh + 1, RbP); d.VL(14, et + w, rh + 1, RbF);
                    d.S(10, rh + 4, Nose); d.S(13, rh + 4, Nose); d.S(11, rh + 5, RbP); d.S(12, rh + 5, RbP); d.HL(11, 12, rh + 6, White);
                }
                else { d.VL(9, et, rh + 1, RbF); d.VL(10, et, rh + 1, RbF); d.VL(13, et + w, rh + 1, RbF); d.VL(14, et + w, rh + 1, RbF); d.R(11, rb + 4, 2, 2, White); }
                break;
            }
        }
        return d.Done();
    }

    public static Bitmap Frame(int sp, int row, int col) { return row < 2 ? FrontBack(sp, row, col) : Side(sp, col); }

    /// <summary>List portrait: front sit frame on a 24x24 canvas, feet on the bottom row.</summary>
    public static Bitmap Portrait(int sp)
    {
        var o = new Cv(24, 24);
        o.Blit(FrontBack(sp, 0, sp == 5 ? 5 : 4), 0, 4);   // dragon portrait uses the flared-wing pose
        return o.B;
    }

    public static void Run(string outDir)
    {
        Directory.CreateDirectory(outDir);
        int k = 3, gap = 16;
        var board = new Cv(16 + (6 * CW) * k + gap + 24 * k + 16, 16 + Names.Length * (3 * CH * k + gap));
        board.Clear(P.Char);
        for (int sp = 0; sp < Names.Length; sp++)
        {
            var sheet = new Cv(6 * CW, 3 * CH);
            for (int r = 0; r < 3; r++) for (int c = 0; c < 6; c++) sheet.Blit(Frame(sp, r, c), c * CW, r * CH);
            sheet.Save(Path.Combine(outDir, "Pet_" + Names[sp] + ".png"));
            var por = Portrait(sp);
            new Cv(por).Save(Path.Combine(outDir, "Pet_" + Names[sp] + "_Portrait.png"));
            int y = 16 + sp * (3 * CH * k + gap);
            board.Blit(sheet.B, 16, y, k);
            board.Blit(por, 16 + 6 * CW * k + gap, y, k);
        }
        board.Save(Path.Combine(outDir, "Pet_Species_Review_Board.png"));
    }
}
