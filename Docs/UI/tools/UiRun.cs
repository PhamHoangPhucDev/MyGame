using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public static class UiRun
{
    public static string Log = "";

    public static void Run(string uiDir)
    {
        Parts.BuildPanels(); Parts.BuildButtons(); Parts.BuildHud(); Icons.Build();

        // 1. Unity-ready components at 1x
        string exp = Path.Combine(uiDir, "export");
        foreach (var a in Kit.All) new Cv(a.Img).Save(Path.Combine(exp, a.Folder.Replace('/', Path.DirectorySeparatorChar), a.File));

        // 2. mockups
        var portrait = Screens.Portrait();
        var land = Screens.Landscape();
        var thumbs = new List<Cv>();
        foreach (var s in portrait)
        {
            var full = s.Compose();
            full.Save(Path.Combine(uiDir, "mockups", "portrait", s.Name + ".png"));
            if (s.Name.StartsWith("P07") || s.Name.StartsWith("P04") || s.Name.StartsWith("P14")) thumbs.Add(Half(full));
        }
        foreach (var s in land) s.Compose().Save(Path.Combine(uiDir, "mockups", "landscape", s.Name + ".png"));

        // 3. design-system boards
        var boards = Boards.Build(thumbs);
        foreach (var b in boards) b.C.Scaled(2).Save(Path.Combine(uiDir, "boards", b.Name + ".png"));

        // 4. manifest
        var page = new Dictionary<string, string> {
            { "Panels", "Master DS p5 Panels" }, { "Buttons", "Master DS p6 Buttons" }, { "Controls", "Master DS p6 Buttons" },
            { "HUD", "Master DS p7 HUD" }, { "Bars", "Master DS p10 Bars" }, { "Slots", "Master DS p9 Slots" },
            { "Icons", "Master DS p8 Icons" }, { "Icons/Items", "Master DS p8 Icons" }, { "Icons/Equip", "Master DS p8 Icons" }, { "Icons/Minimap", "Master DS p8 Icons" } };
        var sb = new StringBuilder();
        sb.AppendLine("Asset ID,Asset Name,UI Screen,Component Type,Canva Design Reference,Export Filename,Target Dimensions,Transparency,Unity UI Usage,Normal / Selected / Disabled state,Status");
        foreach (var a in Kit.All)
        {
            string dims = a.W + "x" + a.H + " src (x2 portrait / x3 landscape)";
            string usage = a.Usage + (a.Border == "-" ? "" : ". 9-slice border L,T,R,B = " + a.Border) + (string.IsNullOrEmpty(a.Notes) ? "" : ". " + a.Notes);
            sb.AppendLine(string.Join(",", new[] {
                a.Id, Q(a.File.Replace(".png", "")), Q(a.Screen), Q(a.Type), Q((page.ContainsKey(a.Folder) ? page[a.Folder] : "Master DS") + " | https://www.canva.com/d/M_X3Uer9CvpN3mq"),
                Q("export/" + a.Folder + "/" + a.File), Q(dims), "Transparent PNG", Q(usage), Q(a.State), "Exported (local PNG 1x)" }));
        }
        File.WriteAllText(Path.Combine(uiDir, "GAME01_UI_ASSET_MANIFEST.csv"), sb.ToString(), new UTF8Encoding(false));

        // 5. screen list
        var sl = new StringBuilder();
        sl.AppendLine("file,title,orientation,size");
        foreach (var s in portrait) sl.AppendLine(s.Name + ".png," + Q(s.Title) + ",portrait,576x1024");
        foreach (var s in land) sl.AppendLine(s.Name + ".png," + Q(s.Title) + ",landscape,1920x1080");
        foreach (var b in boards) sl.AppendLine(b.Name + ".png," + Q(b.Title) + ",board,1920x1080");
        File.WriteAllText(Path.Combine(uiDir, "mockups", "SCREEN_LIST.csv"), sl.ToString(), new UTF8Encoding(false));
        Log = Kit.All.Count + " assets, " + portrait.Count + " portrait, " + land.Count + " landscape, " + boards.Count + " boards";
    }
    static string Q(string s) { return "\"" + (s ?? "").Replace("\"", "\"\"") + "\""; }
    static Cv Half(Cv c)
    {
        var o = new Cv(c.W / 2, c.Ht / 2);
        for (int j = 0; j < o.Ht; j++) for (int i = 0; i < o.W; i++) o.Set(i, j, c.Get(i * 2, j * 2));
        return o;
    }
}
