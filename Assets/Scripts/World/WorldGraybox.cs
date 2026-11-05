using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Placeholder tints for the graybox. Gameplay does not read these.
    /// </summary>
    public static class WorldGraybox
    {
        public static readonly Color Road = new Color(0.34f, 0.28f, 0.18f, 1f);
        public static readonly Color Gate = new Color(0.25f, 0.55f, 0.85f, 1f);
        public static readonly Color Shop = new Color(0.85f, 0.7f, 0.3f, 1f);
        public static readonly Color Smith = new Color(0.55f, 0.55f, 0.6f, 1f);
        public static readonly Color Quest = new Color(0.4f, 0.55f, 0.85f, 1f);
        public static readonly Color Storage = new Color(0.45f, 0.32f, 0.22f, 1f);
        public static readonly Color Map = new Color(0.35f, 0.6f, 0.4f, 1f);
        public static readonly Color Locked = new Color(0.4f, 0.4f, 0.45f, 1f);

        public static Color Npc(string service)
        {
            if (service == "shop") return Shop;
            if (service == "smith") return Smith;
            if (service == "quest") return Quest;
            if (service == "storage") return Storage;
            return Map;
        }
    }
}
