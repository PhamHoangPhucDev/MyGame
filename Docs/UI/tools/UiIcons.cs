using System;
using System.Collections.Generic;
using System.Drawing;

// GAME01 icon kit: 16x16, 1 px ink outline, light from the top-left, 3-tone shading.
public static class Icons
{
    public static readonly Dictionary<string, string[]> Grids = new Dictionary<string, string[]>
    {
        { "Inventory", new[] {
            "................",
            "......kkkk......",
            ".....kNNnnk.....",
            "......kNnk......",
            ".....kgGgdk.....",
            "....kknNnnkk....",
            "...kNNNNNNnnk...",
            "..kNNNNNNNNnnk..",
            ".kNNNNNNNNNNnnk.",
            ".kNNNNkkkkNNnnk.",
            ".kNNNkgGGdkNnnk.",
            ".kNNNkgggdkNnmk.",
            ".kNNNNkkkkNnnmk.",
            "..knNNNNNNnnmk..",
            "...kkmmmmmmkk...",
            "................" } },
        { "Equipment", new[] {
            "........kk......",
            ".......kUuk.....",
            "....kkSSSskk....",
            "...kSSSSSSssk...",
            "..kSSwSSSSSssk..",
            "..kSSSSSSSSssk..",
            "..kSSkkkkkkzsk..",
            "..kSkzkzkzkzsk..",
            "..kSSkkkkkkzsk..",
            "..kSSSSssSSssk..",
            "..kSSSSssSSszk..",
            "...kSSSssSSzk...",
            "...kzSSssSszk...",
            "....kkzzzzkk....",
            "................",
            "................" } },
        { "Quests", new[] {
            "................",
            ".kkkkkkkkkkkkkk.",
            "kPPPPPPPPPPPPppk",
            "kooooooooooooook",
            ".kkPPPPPPPPPPkk.",
            "..kPPkkkkkkPpk..",
            "..kPPPPPPPPPpk..",
            "..kPPkkkkkPPpk..",
            "..kPPPPPPPPPpk..",
            "..kPPkkkkPPPpk..",
            "..kPPPPPPPrrqk..",
            ".kkPPPPPPrRrqkk.",
            "kPPPPPPPPPrrqppk",
            "kooooooooooooook",
            ".kkkkkkkkkkkkkk.",
            "................" } },
        { "WorldMap", new[] {
            "................",
            "................",
            ".kkkkkkkkkkkkkk.",
            ".kPPPPppppPPPPk.",
            ".kPllPppppPrPrk.",
            ".kllllppppPPrPk.",
            ".kPllPppppPrPrk.",
            ".kPPkPppppPPPPk.",
            ".kPPPPkpppPPPPk.",
            ".kPPPPppkpPPPPk.",
            ".kPPPPppppkPPPk.",
            ".kPPPPppppPPllk.",
            ".kooooooooooook.",
            ".kkkkkkkkkkkkkk.",
            "................",
            "................" } },
        { "Shop", new[] {
            "................",
            "..kkkkkkkkkkkk..",
            ".kRwRwRwRwRwRwk.",
            ".krPrPrPrPrPrPk.",
            ".kqoqoqoqoqoqok.",
            "..kn........nk..",
            "..kn..kkkk..nk..",
            "..kn.kGggdk.nk..",
            ".kkkkkkkkkkkkkk.",
            ".kNNNNNNNNNNNnk.",
            ".knnnnnnnnnnnmk.",
            ".knnnknnnnknnmk.",
            ".knnnknnnnknnmk.",
            ".kmmmmmmmmmmmmk.",
            ".kkkkkkkkkkkkkk.",
            "................" } },
        { "Crafting", new[] {
            "................",
            "......kkkkkk....",
            "......kSSSsk....",
            "......kssszk....",
            "......kkkkkk....",
            "........knk.....",
            "...Y.....knk....",
            "..YGY.....knk...",
            ".kkkkkkkkkkkkk..",
            "kSSSSSSSSSSSssk.",
            ".kzsssssssssszk.",
            "...kkzsssssskk..",
            ".....kssssszk...",
            "....kSssssssszk.",
            "...kkkkkkkkkkkk.",
            "................" } },
        { "Storage", new[] {
            "................",
            "................",
            "................",
            "..kkkkkkkkkkkk..",
            ".kNNNNNNNNNNnnk.",
            ".kNnnnnnnnnnnmk.",
            ".kgGgggggggggdk.",
            ".kNnnnnggnnnnmk.",
            ".kkkkkkGGkkkkkk.",
            ".kNnnnndgdnnnmk.",
            ".kNnnnnddnnnnmk.",
            ".kgGgggggggggdk.",
            ".kNnnnnnnnnnnmk.",
            ".kmmmmmmmmmmmmk.",
            ".kkkkkkkkkkkkkk.",
            "................" } },
        { "Treasure", new[] {
            "....Y......Y....",
            "...YGY..Y.......",
            "....Y..YGY......",
            "..kkkkkkYkkkkk..",
            ".kmmmmmmmmmmmmk.",
            ".kgGgggggggggdk.",
            ".kMMMMMMMMMMMMk.",
            ".kMGGgYGgGGgYMk.",
            ".kGgGgGgGgGgGgk.",
            ".kkkkkkGGkkkkkk.",
            ".kNnnnndgdnnnmk.",
            ".kNnnnnddnnnnmk.",
            ".kgGgggggggggdk.",
            ".kNnnnnnnnnnnmk.",
            ".kmmmmmmmmmmmmk.",
            ".kkkkkkkkkkkkkk." } },
        { "Training", new[] {
            "......kkkk......",
            ".....kAAAak.....",
            ".....kAaaak.....",
            "......kkkk......",
            "..kkkkknnkkkkk..",
            ".kAAAaAAAAaAAak.",
            "..kkkkAaaakkkk..",
            ".....kRwRak.....",
            ".....kwRwak.....",
            ".....kRwRak.....",
            ".....kAaaak.....",
            "......knnk......",
            "......knnk......",
            "....kkknnkkk....",
            "....kmmmmmmk....",
            "....kkkkkkkk...." } },
        { "Mining", new[] {
            "................",
            "....kkkkkkkk....",
            "..kkSSSSSSSskk..",
            ".kSSskkkkkkzssk.",
            ".kSk...knk..ksk.",
            ".kk....knk....kk",
            ".......knk......",
            "......knk.......",
            "......knk.......",
            "......knk.......",
            ".....knk........",
            ".....knk........",
            ".....knk........",
            "....kNk.........",
            "....kk..........",
            "................" } },
        { "Farming", new[] {
            "................",
            "................",
            "...kk.....kkk...",
            "...kLLk..kLLlk..",
            "..kLLllk.kLllhk.",
            "..klllhkkllhhk..",
            "...khhhkkhhkk...",
            ".....kkllk......",
            "......klk.......",
            "......klk.......",
            "..kkkkklkkkkk...",
            ".knNnnNnnnNnnmk.",
            ".knnmnnnmnnnnmk.",
            ".kmmmmmmmmmmmmk.",
            ".kkkkkkkkkkkkkk.",
            "................" } },
        { "Dungeon", new[] {
            "................",
            ".....kkkkkk.....",
            "...kkXXGXXxkk...",
            "..kXXXkkkkxxzk..",
            ".kXXxkcccckXxzk.",
            ".kXxkcccccckxzk.",
            ".kxXkcccccckXzk.",
            ".kXxkcccccckxzk.",
            ".kxXkcccccckXzk.",
            ".kXxkcccccckxzk.",
            ".kxXkcccccckXzk.",
            ".kXxkcccccckxzk.",
            ".kxXkcccccckXzk.",
            ".kzzkcccccckzzk.",
            ".kkkkkkkkkkkkkk.",
            "................" } },
        { "Boss", new[] {
            "................",
            ".kk..........kk.",
            ".krk........krk.",
            ".krqk......kqrk.",
            "..kqrkkkkkkrqk..",
            "..kwwwwwwwwwyk..",
            ".kwwwwwwwwwwyyk.",
            ".kwwkRkwwkRkyyk.",
            ".kwwkkkwwkkkyyk.",
            ".kwwwwwkkwwwwyk.",
            "..kwwwwwwwwwyk..",
            "...kwkwkwkwyk...",
            "....kwkwkwyk....",
            ".....kkkkkk.....",
            "................",
            "................" } },
        { "HP", new[] {
            "................",
            "................",
            "................",
            "..kkkk....kkkk..",
            ".kRRrrk..kRrrqk.",
            ".kRwRrrrrrrrrqk.",
            ".kRRrrrrrrrrqqk.",
            ".krrrrrrrrrrqqk.",
            "..krrrrrrrrqqk..",
            "...krrrrrrqqk...",
            "....krrrrqqk....",
            ".....krrqqk.....",
            "......krqk......",
            ".......kk.......",
            "................",
            "................" } },
        { "EXP", new[] {
            "................",
            ".......kk.......",
            "......kLLk......",
            ".....kLwLlk.....",
            "....kLwLLllk....",
            "...kLLLLLlllk...",
            "..kLLLLLLllllk..",
            ".kLLLLLLlllllhk.",
            ".kllllllhhhhhhk.",
            "..klllllhhhhhk..",
            "...kllllhhhhk...",
            "....klllhhhk....",
            ".....kllhhk.....",
            "......klhk......",
            ".......kk.......",
            "................" } },
        { "Skill", new[] {
            "................",
            "...kkkkkkkkkk...",
            "..kViiiiiiiijk..",
            "..kViiiGiiiijk..",
            "..kViiGgGiiijk..",
            "..kViGgYgGiijk..",
            "..kViiGgGiiijk..",
            "..kViiiGiiiijk..",
            "..kViiiiiiiijk..",
            "..kViiiiiiiijk..",
            "..kjjjjjjjjjjk..",
            "..kPpppppppppk..",
            "..kjjjjjjjjjjk..",
            "...kkkkkkkkkk...",
            "................",
            "................" } },
        { "Attack", new[] {
            "............kkkk",
            "...........kSSwk",
            "..........kSwwsk",
            ".........kSwwsk.",
            "........kSwwsk..",
            ".......kSwwsk...",
            "..kk..kSwwsk....",
            "..kgk.kwwsk.....",
            "...kgkwwsk......",
            "....kgksk.......",
            "....kkgk........",
            "...knkkgk.......",
            "..knk..kgk......",
            ".knk....kk......",
            "kGk.............",
            ".k.............." } },
        { "Back", new[] {
            "................",
            "................",
            "......kk........",
            ".....kGk........",
            "....kGgk........",
            "...kGggkkkkkkk..",
            "..kGgggGGGGGGGk.",
            ".kGgggggggggggk.",
            ".kdgggggggggggk.",
            "..kdgggdddddddk.",
            "...kdggkkkkkkk..",
            "....kdgk........",
            ".....kdk........",
            "......kk........",
            "................",
            "................" } },
        { "Close", new[] {
            "................",
            "..kk........kk..",
            ".kRRk......kRrk.",
            ".kRrrk....kRrrk.",
            "..krrrk..kRrrk..",
            "...krrrkkRrrk...",
            "....krrrRrrk....",
            ".....krrrrk.....",
            ".....krrrqk.....",
            "....krrrqqqk....",
            "...krrrkkrqqk...",
            "..krrrk..krqqk..",
            ".krrqk....krqqk.",
            ".kqqk......kqqk.",
            "..kk........kk..",
            "................" } },
        { "Confirm", new[] {
            "................",
            "................",
            "................",
            "............kk..",
            "...........kLlk.",
            "..........kLlhk.",
            ".........kLlhk..",
            "..kk....kLlhk...",
            ".kLlk..kLlhk....",
            ".klLlkkLlhk.....",
            "..khlLLlhk......",
            "...khllhk.......",
            "....khhk........",
            ".....kk.........",
            "................",
            "................" } },
        { "Lock", new[] {
            "................",
            "................",
            ".....kkkkkk.....",
            "....kSkkkkzk....",
            "....kSk..kzk....",
            "....kSk..kzk....",
            "..kkkkkkkkkkkk..",
            "..kSSSSSSSSSsk..",
            "..kSsssssssszk..",
            "..kSssskkssszk..",
            "..kSssskkssszk..",
            "..kSsssskssszk..",
            "..kSsssssssszk..",
            "..kzzzzzzzzzzk..",
            "..kkkkkkkkkkkk..",
            "................" } },
        { "Talk", new[] {
            "................",
            "................",
            "..kkkkkkkkkkk...",
            ".kwwwwwwwwwwwk..",
            ".kwwwwwwwwwwyk..",
            ".kwwkwwkwwkwyk..",
            ".kwwwwwwwwwwyk..",
            ".kwwwwwwwwwyyk..",
            "..kkwwkkkkkkk...",
            "...kwk..........",
            "..kwk...........",
            "..kk............",
            "................",
            "................",
            "................",
            "................" } },
        { "Pause", new[] {
            "................",
            "................",
            "................",
            "...kkkk..kkkk...",
            "...kwyk..kwyk...",
            "...kwyk..kwyk...",
            "...kwyk..kwyk...",
            "...kwyk..kwyk...",
            "...kwyk..kwyk...",
            "...kwyk..kwyk...",
            "...kwyk..kwyk...",
            "...kwyk..kwyk...",
            "...kkkk..kkkk...",
            "................",
            "................",
            "................" } },
        { "Housing", new[] {
            "................",
            "................",
            "......kkkk......",
            ".....kRRrrk.....",
            "....kRRrrrqk....",
            "...kRRrrrrrqk...",
            "..kRRrrrrrrrqk..",
            ".kkkkkkkkkkkkkk.",
            "..kPPPPPPPPPok..",
            "..kPuuPPPnnPok..",
            "..kPuuPPPnnPok..",
            "..kPPPPPPnnPok..",
            "..kooooooNnook..",
            "..kkkkkkkkkkkk..",
            "................",
            "................" } },
        // ------------------------------------------------ items
        { "Item_Ore", new[] {
            "................",
            "................",
            "................",
            "................",
            ".....kkkkk......",
            "...kkXXXXxkk....",
            "..kXXeXXXxxxk...",
            ".kXXeOeXXxxexk..",
            ".kXXXeXXxxeOek..",
            ".kXxxxxxxxxexzk.",
            ".kxxxexxxxxxzzk.",
            "..kzxxxxxxxzzk..",
            "...kkzzzzzzkk...",
            ".....kkkkkk.....",
            "................",
            "................" } },
        { "Item_Wood", new[] {
            "................",
            "................",
            "................",
            "................",
            "................",
            "...kkkkkkkkkkk..",
            "..kNNNNNNNNkPPk.",
            "..knnnnnnnkPopPk",
            "..knmnnnmnkpoopk",
            "..knnnnnnnkPopPk",
            "..kmmmmmmmmkPPk.",
            "...kkkkkkkkkkk..",
            "................",
            "................",
            "................",
            "................" } },
        { "Item_Herb", new[] {
            "................",
            "................",
            ".......kk.......",
            "......kVVk......",
            ".....kViVik.....",
            "......kiik......",
            ".......kk.......",
            "..kk...kl..kk...",
            ".kLLk..kl.kLLk..",
            ".kLllk.kl.kllhk.",
            "..klllkklkklhk..",
            "...khhllllhhk...",
            ".....kkllkk.....",
            ".......kk.......",
            "................",
            "................" } },
        { "Item_Potion", new[] {
            "................",
            "......kkkk......",
            "......kNnk......",
            "......kkkk......",
            "......kSzk......",
            ".....kkSzkk.....",
            "...kSRRrrrrqk...",
            "..kSwRRrrrrrqk..",
            "..kSRRrrrrrrqk..",
            "..krRrrrrrrqqk..",
            "..krrrrrrrrqqk..",
            "...kqrrrrrqqk...",
            "....kkqqqqkk....",
            "......kkkk......",
            "................",
            "................" } },
        { "Item_QuestToken", new[] {
            "................",
            "................",
            ".....kkkkkk.....",
            "....kGGGGGgk....",
            "...kGuUUUuugk...",
            "..kGuUUwUuuugk..",
            "..kGuUwwwuuugk..",
            "..kgUUUwUuuvdk..",
            "..kguUUUuuuvdk..",
            "..kguuuuuuvvdk..",
            "...kguuuvvvdk...",
            "....kddddddk....",
            ".....kkkkkk.....",
            "................",
            "................",
            "................" } },
        // ------------------------------------------------ equipment ghosts
        { "Equip_Shield", new[] {
            "................",
            "..kkkkkkkkkkkk..",
            "..kSSSSSSSSSsk..",
            "..kSvvvvvvvvzk..",
            "..kSvvvwwvvvzk..",
            "..kSvwwwwwwvzk..",
            "..kSvvvwwvvvzk..",
            "..kSvvvwwvvvzk..",
            "...kSvvwwvvzk...",
            "...kSvvwwvvzk...",
            "....kSvwwvzk....",
            ".....kSvvzk.....",
            "......kszk......",
            ".......kk.......",
            "................",
            "................" } },
        { "Equip_Armor", new[] {
            "................",
            "................",
            ".kkkk....kkkk...",
            "kSSSsk..kSSssk..",
            "kSSSSskkSSSssk..",
            ".kSSSSSSSSSssk..",
            "..kSSwSSSSssk...",
            "..kSSSSSSSssk...",
            "..kSSSSSSSssk...",
            "..kzzzzzzzzzk...",
            "..kSSSSSSSssk...",
            "...kSSSSSssk....",
            "...kkkkkkkkk....",
            "................",
            "................",
            "................" } },
        { "Equip_Boots", new[] {
            "................",
            "................",
            "................",
            "...kkkk..kkkk...",
            "...kNnk..kNnk...",
            "...kNnk..kNnk...",
            "...kNnk..kNnk...",
            "...kNnk..kNnk...",
            "...kNnk..kNnk...",
            "..kNnnk..kNnnk..",
            ".kNNnnk.kNNnnk..",
            ".kmmmmk.kmmmmk..",
            ".kkkkkk.kkkkkk..",
            "................",
            "................",
            "................" } },
        { "Equip_Ring", new[] {
            "................",
            "................",
            ".......kk.......",
            "......kUuk......",
            ".....kUuuvk.....",
            "....kkkvvkkk....",
            "...kGgk..kgdk...",
            "..kGgk....kgdk..",
            "..kGk......kdk..",
            "..kGk......kdk..",
            "..kgk......kdk..",
            "..kgdk....kddk..",
            "...kgddddddk....",
            "....kkkkkkkk....",
            "................",
            "................" } },
    };

    // minimap markers 7x7
    public static readonly Dictionary<string, string[]> Markers = new Dictionary<string, string[]>
    {
        { "Player", new[] { "...k...", "..kGk..", ".kGGgk.", "kGGggdk", "kkkgkkk", "..kdk..", "..kkk.." } },
        { "NPC", new[] { "..kkk..", ".kUuuk.", "kUUuuvk", "kUuuuvk", "kuuuvvk", ".kvvvk.", "..kkk.." } },
        { "Quest", new[] { "..kkk..", "..kGk..", "..kGk..", "..kgk..", "..kkk..", "..kGk..", "..kkk.." } },
        { "Boss", new[] { ".kkkkk.", "kwwwwyk", "kwkwkyk", "kwwwwyk", ".kwkwk.", "..kkk..", "......." } },
        { "Gate", new[] { ".kkkkk.", "kLlllhk", "klkkkhk", "klkckhk", "klkckhk", "klkckhk", "kkkkkkk" } },
        { "Chest", new[] { ".......", "kkkkkkk", "kGgggdk", "kkkGkkk", "kgggddk", "kkkkkkk", "......." } },
        { "Node", new[] { "...k...", "..kXk..", ".kXxxk.", "kXxxxzk", ".kxxzk.", "..kzk..", "...k..." } },
        { "Enemy", new[] { ".......", "..kkk..", ".kRrqk.", ".krrqk.", ".kqqqk.", "..kkk..", "......." } },
    };

    public static readonly Dictionary<string, Bitmap> Bmp = new Dictionary<string, Bitmap>();

    public static Bitmap Get(string name) { return Bmp[name]; }

    public static void Build()
    {
        foreach (var kv in Grids)
        {
            foreach (var row in kv.Value) if (row.Length > 16) throw new Exception("Icon " + kv.Key + " row too long (" + row.Length + "): " + row);
            if (kv.Value.Length != 16) throw new Exception("Icon " + kv.Key + " has " + kv.Value.Length + " rows");
            var b = Img.Grid(kv.Value);
            Bmp[kv.Key] = b;
        }
        Bmp["Currency"] = Coin();
        Bmp["Settings"] = Gear();
        Bmp["Star"] = Parts.Star(16, true);
        Bmp["StarEmpty"] = Parts.Star(16, false);
        // sword variant for the training blade item (wooden blade)
        Bmp["Item_TrainingBlade"] = Img.Grid(Grids["Attack"], new Dictionary<char, char> { { 'S', 'P' }, { 'w', 'p' }, { 's', 'o' }, { 'g', 'n' }, { 'G', 'N' } });
        Bmp["Item_Sprout"] = Bmp["Farming"];
        Bmp["Item_Coin"] = Bmp["Currency"];
        Bmp["Equip_Weapon"] = Bmp["Attack"];
        Bmp["Equip_Helmet"] = Bmp["Equipment"];

        string[] system = { "Inventory", "Equipment", "Quests", "WorldMap", "Settings", "Shop", "Crafting", "Storage", "Training", "Mining", "Farming", "Dungeon", "Boss", "Treasure", "Currency", "EXP", "HP", "Skill", "Attack", "Back", "Close", "Confirm", "Lock", "Talk", "Pause", "Housing", "Star", "StarEmpty" };
        foreach (var n in system)
            Kit.Reg("Icons", "UI_Icon_" + n + ".png", "Icon kit", "Icon 16", "System icon: " + n, n == "StarEmpty" ? "Disabled" : "Normal", Bmp[n]);
        string[] items = { "Item_Ore", "Item_Wood", "Item_Herb", "Item_Sprout", "Item_Potion", "Item_TrainingBlade", "Item_QuestToken", "Item_Coin" };
        foreach (var n in items)
            Kit.Reg("Icons/Items", "UI_" + n + ".png", "Inventory / Shop / Storage", "Item icon 16", "Item icon (WorldContent id: " + n.Substring(5).ToLower() + ")", "Normal", Bmp[n]);
        string[] equip = { "Equip_Weapon", "Equip_Helmet", "Equip_Armor", "Equip_Shield", "Equip_Boots", "Equip_Ring" };
        foreach (var n in equip)
        {
            var ghost = Img.Silhouette(Bmp[n], P.Char2, P.Stone);
            Bmp[n + "_Ghost"] = ghost;
            Kit.Reg("Icons/Equip", "UI_" + n + "_Ghost.png", "Equipment", "Ghost icon 16", "Empty equipment slot hint (" + n.Substring(6) + ")", "Disabled", ghost);
        }
        foreach (var kv in Markers)
        {
            var b = Img.Grid(kv.Value);
            Bmp["Marker_" + kv.Key] = b;
            Kit.Reg("Icons/Minimap", "UI_Minimap_Marker_" + kv.Key + ".png", "Minimap / World Map", "Marker 7", "Map marker: " + kv.Key, "Normal", b);
        }
        // greyed disabled variants are produced in Unity by tint; one example exported for reference
    }

    static Bitmap Coin()
    {
        var cv = Parts.Round(14, (b, lt) =>
        {
            if (b == 0) return P.GoldD;
            if (b == 1) return lt ? P.GoldW : P.Gold;
            if (b == 2) return lt ? P.GoldL : P.Gold;
            return lt ? P.GoldL : P.Gold;
        });
        // engraved inner ring + shine
        var o = new Cv(16, 16);
        o.Blit(cv.B, 1, 1);
        o.Box(5, 5, 6, 6, P.GoldD); o.Set(5, 5, P.Gold); o.Set(10, 5, P.Gold); o.Set(5, 10, P.Gold); o.Set(10, 10, P.Gold);
        o.Set(7, 7, P.GoldW); o.Set(8, 7, P.GoldL); o.Set(7, 8, P.GoldL);
        o.Set(4, 3, P.GoldW); o.Set(3, 4, P.GoldW);
        return Img.Outline(o.B, P.Ink);
    }
    static Bitmap Gear()
    {
        var cv = new Cv(16, 16);
        double cx = 8, cy = 8;
        for (int j = 0; j < 16; j++)
            for (int i = 0; i < 16; i++)
            {
                double dx = i + 0.5 - cx, dy = j + 0.5 - cy;
                double r = Math.Sqrt(dx * dx + dy * dy), a = Math.Atan2(dy, dx);
                bool tooth = Math.Cos(8 * a) > 0.25 && r <= 7.0;
                bool body = r <= 5.0;
                if (!(body || tooth) || r < 2.0) continue;
                bool lt = (dx + dy) < 0;
                Color c = r < 3.0 ? P.Chars['z'] : (lt ? P.Chars['S'] : P.Chars['s']);
                if (!lt && r > 4.2) c = P.Chars['z'];
                cv.Set(i, j, c);
            }
        // re-outline (inner hole too)
        return Img.Outline(cv.B, P.Ink);
    }
}
