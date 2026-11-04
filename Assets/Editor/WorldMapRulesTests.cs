using UnityEngine;

namespace GameplayPrototype
{
    public static class WorldMapRulesTests
    {
        public static string Run()
        {
            int saved = PlayerPrefs.GetInt("gp.unlockedLevel", 1);
            try
            {
                UiKit.Ensure();
                string a = At(1);
                string b = At(3);
                string fade = Fade();
                string c = At(5);
                return a + " || " + b + " || " + fade + " || " + c;
            }
            finally
            {
                var canvas = GameObject.Find("WorldMapCanvas");
                if (canvas != null) Object.DestroyImmediate(canvas);
                PlayerPrefs.SetInt("gp.unlockedLevel", saved);
                PlayerPrefs.Save();
                WorldMapPanel.Remember(-1);
            }
        }

        static string At(int level)
        {
            PlayerPrefs.SetInt("gp.unlockedLevel", level);
            WorldMapPanel.Remember(level);
            WorldMapPanel.Open();
            var map = Object.FindFirstObjectByType<WorldMapPanel>();
            map.Select(0);
            string fog = "", pin = "", row = "";
            for (int n = 1; n <= 5; n++)
            {
                fog += map.FogOn(n) ? "1" : "0";
                pin += (n == 1 ? "" : ",") + Short(map.PinSprite(n));
                row += (n == 1 ? "" : ",") + Tone(map.RowColor(n));
            }
            map.Select(2);
            string blocked = map.TravelOn ? "travel2=on" : "travel2=off";
            map.Select(level > 5 ? 5 : level);
            string own = map.TravelOn ? "travelN=on" : "travelN=off";
            bool halo = map.HaloOn(Mathf.Clamp(level, 1, 5));
            WorldMapPanel.Close();
            return "L" + level + " fog=" + fog + " pin=" + pin + " row=" + row + " " + blocked + " " + own + " halo=" + halo;
        }

        static string Fade()
        {
            PlayerPrefs.SetInt("gp.unlockedLevel", 3);
            WorldMapPanel.Remember(1);
            WorldMapPanel.Open();
            var map = Object.FindFirstObjectByType<WorldMapPanel>();
            bool start = map.FogOn(2);
            map.TickFade(0.5f);
            bool end = map.FogOn(2);
            bool stay = map.FogOn(4);
            WorldMapPanel.Close();
            return "fade2 " + (start ? "shown" : "hidden") + "->" + (end ? "shown" : "gone") + " fog4=" + (stay ? "on" : "off");
        }

        static string Short(string sprite)
        {
            if (sprite == "UI_WorldMap_Pin_Locked") return "L";
            if (sprite == "UI_WorldMap_Pin_Unlocked") return "U";
            if (sprite == "UI_WorldMap_Pin_Cleared") return "C";
            if (sprite == "UI_WorldMap_Pin_Selected") return "S";
            return sprite;
        }

        static string Tone(Color c)
        {
            if (c.g > 0.8f && c.r > 0.9f) return "W";
            if (c.r > 0.8f && c.g > 0.7f && c.b < 0.4f) return "G";
            return "M";
        }
    }
}
