using UnityEngine;

namespace GameplayPrototype
{
    public static class DungeonSelectRulesTests
    {
        public static string Run()
        {
            int[] saved = new int[DungeonCatalog.Dungeons.Length];
            for (int i = 0; i < saved.Length; i++) saved[i] = DungeonProgress.Cleared(i);
            try
            {
                UiKit.Ensure();
                string kit = Catalog();
                string chain = Chain();
                DungeonProgress.SetCleared(0, 2);
                DungeonSelectPanel.Remember(1);
                string fade = Fade();
                string board = At(0) + " || " + At(2) + " || " + At(5);
                return kit + " || " + chain + " || " + fade + " || " + board;
            }
            finally
            {
                var canvas = GameObject.Find("DungeonSelectCanvas");
                if (canvas != null) Object.DestroyImmediate(canvas);
                for (int i = 0; i < saved.Length; i++) DungeonProgress.SetCleared(i, saved[i]);
                DungeonSelectPanel.Remember(-1);
            }
        }

        static string Catalog()
        {
            int hit = 0;
            for (int i = 0; i < DungeonCatalog.SpriteNames.Length; i++)
                if (UiKit.Get(DungeonCatalog.SpriteNames[i]) != null) hit++;
            return "kit=" + hit + "/" + DungeonCatalog.SpriteNames.Length;
        }

        static string Chain()
        {
            DungeonProgress.SetCleared(0, 0);
            DungeonProgress.SetCleared(1, 0);
            bool s1 = DungeonProgress.StageUnlocked(0, 1);
            bool s2 = DungeonProgress.StageUnlocked(0, 2);
            DungeonProgress.Clear(0, 1);
            bool s2b = DungeonProgress.StageUnlocked(0, 2);
            DungeonProgress.SetCleared(0, 5);
            bool tab2 = DungeonProgress.DungeonUnlocked(1);
            DungeonProgress.SetCleared(1, 5);
            bool tab3 = DungeonProgress.DungeonUnlocked(2);
            return "chain " + (s1 ? "1on" : "1off") + " " + (s2 ? "2on" : "2off") + "->" + (s2b ? "2on" : "2off")
                + " tab2=" + (tab2 ? "on" : "off") + " tab3=" + (tab3 ? "on" : "off");
        }

        static string At(int cleared)
        {
            DungeonProgress.SetCleared(0, cleared);
            DungeonSelectPanel.Remember(DungeonProgress.HighestUnlocked(0));
            DungeonSelectPanel.Open();
            var map = Object.FindFirstObjectByType<DungeonSelectPanel>();
            string fog = "", pin = "";
            for (int n = 1; n <= 5; n++)
            {
                fog += map.FogOn(n) ? "1" : "0";
                pin += (n == 1 ? "" : ",") + Short(map.PinSprite(n));
            }
            map.Select(3);
            string blocked = map.TravelOn ? "travel4=on" : "travel4=off";
            map.Select(DungeonProgress.HighestUnlocked(0) - 1);
            string own = map.TravelOn ? "travelN=on" : "travelN=off";
            bool halo = map.HaloOn(DungeonProgress.HighestUnlocked(0) - 1);
            string label = map.TravelLabel;
            DungeonSelectPanel.Close();
            return "C" + cleared + " fog=" + fog + " pin=" + pin + " " + blocked + " " + own + " halo=" + halo + " btn=" + label;
        }

        static string Fade()
        {
            DungeonSelectPanel.Open();
            var map = Object.FindFirstObjectByType<DungeonSelectPanel>();
            bool start = map.FogOn(2);
            map.TickFade(0.5f);
            bool end = map.FogOn(2);
            bool stay = map.FogOn(4);
            DungeonSelectPanel.Close();
            return "fade2 " + (start ? "shown" : "hidden") + "->" + (end ? "shown" : "gone") + " fog4=" + (stay ? "on" : "off");
        }

        static string Short(string sprite)
        {
            if (sprite == "UI_WorldMap_Pin_Locked") return "L";
            if (sprite == "UI_WorldMap_Pin_Unlocked") return "U";
            if (sprite == "UI_WorldMap_Pin_Cleared") return "C";
            if (sprite == "UI_WorldMap_Pin_Selected") return "S";
            if (sprite == "UI_Dungeon_Pin_Boss") return "B";
            if (sprite == "UI_Dungeon_Pin_Boss_Locked") return "BL";
            return sprite;
        }
    }
}
