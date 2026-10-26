using System;
using System.Drawing;
using System.IO;
using System.Linq;

public static class UiPreview
{
    public static void Run(string uiDir)
    {
        Parts.BuildPanels(); Parts.BuildButtons(); Parts.BuildHud(); Icons.Build();
        var cv = new Cv(480, 300);
        cv.Clear(P.H("3A5A44"));
        int x = 4, y = 4, rowH = 0;
        foreach (var a in Kit.All)
        {
            if (x + a.W + 4 > 480) { x = 4; y += rowH + 4; rowH = 0; }
            cv.Blit(a.Img, x, y);
            x += a.W + 4; rowH = Math.Max(rowH, a.H);
        }
        Txt.Draw(cv, 4, 286, "Press Start 2P: GAME01 Lv.3 320/400 x12 HITS", P.TextW);
        cv.Scaled(3).Save(Path.Combine(uiDir, "_test", "preview_parts.png"));
        var icons = Kit.All.Where(a => a.Folder.StartsWith("Icons")).ToList();
        var ic = new Cv(10 * 20 + 4, ((icons.Count + 9) / 10) * 20 + 4);
        ic.Clear(P.Char);
        for (int i = 0; i < icons.Count; i++) ic.Blit(icons[i].Img, 4 + (i % 10) * 20, 4 + (i / 10) * 20);
        ic.Scaled(5).Save(Path.Combine(uiDir, "_test", "preview_icons.png"));
    }
}
