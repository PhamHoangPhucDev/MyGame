using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Data for the world skeleton. Gameplay reads this instead of scattering numbers.
    /// </summary>
    public static class WorldCatalog
    {
        public const string SavePrefix = "gw.v1.";

        public static readonly ZoneLayout Hub = new ZoneLayout
        {
            origin = new Vector2(0f, 80f),
            spawn = new Vector2(0f, 75f),
            boundsMin = new Vector2(-8f, 73f),
            boundsMax = new Vector2(8f, 88f)
        };

        public static readonly ZoneLayout Training = new ZoneLayout
        {
            origin = new Vector2(60f, 0f),
            spawn = new Vector2(60f, -2.2f),
            boundsMin = new Vector2(52f, -8f),
            boundsMax = new Vector2(68f, 8f)
        };

        public static readonly ZoneLayout Resource = new ZoneLayout
        {
            origin = new Vector2(-60f, 0f),
            spawn = new Vector2(-60f, -2.2f),
            boundsMin = new Vector2(-68f, -8f),
            boundsMax = new Vector2(-52f, 8f)
        };

        public static readonly ZoneLayout Farm = new ZoneLayout
        {
            origin = new Vector2(0f, -80f),
            spawn = new Vector2(0f, -82.2f),
            boundsMin = new Vector2(-8f, -88f),
            boundsMax = new Vector2(8f, -72f)
        };

        public static readonly ZoneLayout Boss = new ZoneLayout
        {
            origin = new Vector2(60f, 80f),
            spawn = new Vector2(60f, 77.8f),
            boundsMin = new Vector2(52f, 73f),
            boundsMax = new Vector2(68f, 88f)
        };

        public static readonly TrainingConfig TrainingGround = new TrainingConfig
        {
            maxAlive = 3,
            respawnSeconds = 6f,
            expPerKill = 8,
            camps = new[]
            {
                new Vector2(1.4f, 1.6f),
                new Vector2(-1.6f, 1.2f),
                new Vector2(0.2f, 2.4f)
            }
        };

        public static readonly GatherConfig Mine = new GatherConfig
        {
            nodeId = "mine_ore",
            itemId = "ore",
            displayName = "Mining Node",
            amount = 1,
            uses = 2,
            respawnSeconds = 12f
        };

        public static readonly GatherConfig Wood = new GatherConfig
        {
            nodeId = "wood_node",
            itemId = "wood",
            displayName = "Wood Node",
            amount = 1,
            uses = 2,
            respawnSeconds = 12f
        };

        public static readonly CropConfig TestCrop = new CropConfig
        {
            cropId = "test_sprout",
            itemId = "sprout",
            displayName = "Sprout",
            growSeconds = 8f,
            yield = 1
        };

        public static readonly BossConfig BossRidge = new BossConfig
        {
            rosterPrefix = "Cyclops",
            exp = 25
        };

        public const int ExpPerLevel = 50;
    }

    public struct ZoneLayout
    {
        public Vector2 origin;
        public Vector2 spawn;
        public Vector2 boundsMin;
        public Vector2 boundsMax;
    }

    public struct TrainingConfig
    {
        public int maxAlive;
        public float respawnSeconds;
        public int expPerKill;
        public Vector2[] camps;
    }

    public struct GatherConfig
    {
        public string nodeId;
        public string itemId;
        public string displayName;
        public int amount;
        public int uses;
        public float respawnSeconds;
    }

    public struct CropConfig
    {
        public string cropId;
        public string itemId;
        public string displayName;
        public float growSeconds;
        public int yield;
    }

    public struct BossConfig
    {
        public string rosterPrefix;
        public int exp;
    }
}
