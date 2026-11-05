using UnityEngine;

namespace GameplayPrototype
{
    public struct RegionRecord
    {
        public string id;
        public string name;
        public string biomeId;
        public string unlock;
    }

    public struct BiomeRecord
    {
        public string id;
        public string name;
        public string visualSlot;
    }

    public struct ZoneRecord
    {
        public string id;
        public string name;
        public string type;
        public WorldZone zone;
        public Vector2 origin;
        public Vector2 spawn;
        public Vector2 boundsMin;
        public Vector2 boundsMax;
        public string visualSlot;
        public string unlock;
        public string layoutSource;
    }

    public struct PoiRecord
    {
        public string id;
        public string zoneId;
        public string kind;
        public string visualSlot;
        public Vector2 localOffset;
    }

    public struct InteractionRecord
    {
        public string id;
        public string poiId;
        public string verb;
    }

    public struct ActivityRecord
    {
        public string id;
        public string zoneId;
        public string kind;
        public string rewardItem;
        public int rewardCoins;
    }

    public struct RegionBossRecord
    {
        public string id;
        public string zoneId;
        public string rosterPrefix;
        public string hpBar;
        public Vector2 spawnOffset;
        public int expReward;
        public string reentry;
    }

    public struct TransitionRecord
    {
        public string id;
        public string fromId;
        public string toId;
    }

    public struct HazardRecord
    {
        public string id;
        public string biomeId;
        public Vector2 center;
        public Vector2 halfSize;
    }

    public struct FieldMapRecord
    {
        public string regionId;
        public string zoneId;
        public string name;
        public string biomeId;
        public Vector2 origin;
        public Vector2 spawnLocal;
        public Vector2 boundsHalf;
        public string hazardBiome;
        public Vector2 hazardCenter;
        public Vector2 hazardHalf;
        public bool ward;
        public Vector2 wardLocal;
    }

    public struct FieldSpawnRecord
    {
        public string zoneId;
        public string groupId;
        public string rosterLabel;
        public int count;
        public Vector2 local;
    }

    public struct FieldNodeRecord
    {
        public string zoneId;
        public string nodeId;
        public string itemId;
        public string displayName;
        public int amount;
        public int uses;
        public float respawnSeconds;
        public Vector2 local;
    }

    public struct FieldChestRecord
    {
        public string id;
        public string zoneId;
        public string itemId;
        public int itemAmount;
        public int coins;
        public Vector2 local;
    }

    public struct FieldBossRecord
    {
        public string id;
        public string zoneId;
        public string rosterPrefix;
        public Vector2 local;
    }

    public struct SlotRecord
    {
        public string id;
        public string kind;
        public string visualSlot;
        public Vector2 localOffset;
    }

    public struct SkillNodeRecord
    {
        public string id;
        public string skillKey;
        public int requiredLevel;
    }

    public struct GemRecord
    {
        public string id;
        public string socket;
        public string effectId;
    }

    public struct EquipEffectRecord
    {
        public string itemId;
        public string effectId;
        public bool appliedToCombat;
    }

    /// <summary>
    /// World data added around the existing village. Rows are the extension point.
    /// Colors and sprites are not stored here.
    /// </summary>
    public static class WorldFoundation
    {
        public const int ChestCoins = 8;
        public const int WardCoins = 6;
        public const string ChestFlag = "chest.sample";
        public const string WardFlag = "ward.sample";
        public const string HousingFlag = "housing.selected";

        public static readonly RegionRecord[] Regions =
        {
            new RegionRecord { id = "region_village", name = "Village Reach", biomeId = "biome_grass", unlock = "always" },
            new RegionRecord { id = "region_wild", name = "Wild Reach", biomeId = "biome_grass", unlock = "always" }
        };

        public static readonly BiomeRecord[] Biomes =
        {
            new BiomeRecord { id = "biome_grass", name = "Grass", visualSlot = "slot.biome.grass" },
            new BiomeRecord { id = "biome_dirt", name = "Dirt", visualSlot = "slot.biome.dirt" },
            new BiomeRecord { id = "biome_water", name = "Water", visualSlot = "slot.biome.water" },
            new BiomeRecord { id = "biome_lava", name = "Lava", visualSlot = "slot.biome.lava" },
            new BiomeRecord { id = "biome_frozen", name = "Frozen", visualSlot = "slot.biome.frozen" },
            new BiomeRecord { id = "biome_tropical", name = "Tropical", visualSlot = "slot.biome.tropical" },
            new BiomeRecord { id = "biome_sky", name = "Sky", visualSlot = "slot.biome.sky" }
        };

        public static readonly RegionRecord[] FieldRegions =
        {
            new RegionRecord { id = "region_volcanic", name = "VOLCANIC FIELD", biomeId = "biome_lava", unlock = "always" },
            new RegionRecord { id = "region_frozen", name = "FROZEN FIELD", biomeId = "biome_frozen", unlock = "always" },
            new RegionRecord { id = "region_tropical", name = "TROPICAL FIELD", biomeId = "biome_tropical", unlock = "always" },
            new RegionRecord { id = "region_aquatic", name = "AQUATIC FIELD", biomeId = "biome_water", unlock = "always" },
            new RegionRecord { id = "region_sky", name = "SKY FIELD", biomeId = "biome_sky", unlock = "always" }
        };

        public static readonly FieldMapRecord[] FieldMaps =
        {
            new FieldMapRecord { regionId = "region_volcanic", zoneId = "zone_field_volcanic", name = "Volcanic Field", biomeId = "biome_lava", origin = new Vector2(220f, 0f), spawnLocal = new Vector2(0f, -10f), boundsHalf = new Vector2(18f, 14f), hazardBiome = "biome_lava", hazardCenter = new Vector2(0f, 3f), hazardHalf = new Vector2(2.6f, 1.2f) },
            new FieldMapRecord { regionId = "region_frozen", zoneId = "zone_field_frozen", name = "Frozen Field", biomeId = "biome_frozen", origin = new Vector2(220f, 80f), spawnLocal = new Vector2(0f, -10f), boundsHalf = new Vector2(18f, 14f) },
            new FieldMapRecord { regionId = "region_tropical", zoneId = "zone_field_tropical", name = "Tropical Field", biomeId = "biome_tropical", origin = new Vector2(220f, 160f), spawnLocal = new Vector2(0f, -10f), boundsHalf = new Vector2(18f, 14f), ward = true, wardLocal = new Vector2(-9f, 8f) },
            new FieldMapRecord { regionId = "region_aquatic", zoneId = "zone_field_aquatic", name = "Aquatic Field", biomeId = "biome_water", origin = new Vector2(220f, 240f), spawnLocal = new Vector2(0f, -10f), boundsHalf = new Vector2(18f, 14f), hazardBiome = "biome_water", hazardCenter = new Vector2(0f, 3f), hazardHalf = new Vector2(2.6f, 1.2f) },
            new FieldMapRecord { regionId = "region_sky", zoneId = "zone_field_sky", name = "Sky Field", biomeId = "biome_sky", origin = new Vector2(220f, 320f), spawnLocal = new Vector2(0f, -10f), boundsHalf = new Vector2(18f, 14f) }
        };

        public static readonly FieldSpawnRecord[] FieldSpawns =
        {
            new FieldSpawnRecord { zoneId = "zone_field_volcanic", groupId = "field_volcanic_goblins", rosterLabel = "Goblin Lv.3", count = 3, local = new Vector2(-10f, 0f) },
            new FieldSpawnRecord { zoneId = "zone_field_frozen", groupId = "field_frozen_satyrs", rosterLabel = "Satyr Lv.3", count = 2, local = new Vector2(-10f, 0f) },
            new FieldSpawnRecord { zoneId = "zone_field_tropical", groupId = "field_tropical_dryads", rosterLabel = "Dryad Lv.3", count = 2, local = new Vector2(-10f, 0f) },
            new FieldSpawnRecord { zoneId = "zone_field_aquatic", groupId = "field_aquatic_archers", rosterLabel = "Goblin Archer Lv.3", count = 2, local = new Vector2(-10f, 0f) },
            new FieldSpawnRecord { zoneId = "zone_field_sky", groupId = "field_sky_gobriders", rosterLabel = "Gobrider Lv.5", count = 2, local = new Vector2(-11f, -1f) }
        };

        public static readonly FieldNodeRecord[] FieldNodes =
        {
            new FieldNodeRecord { zoneId = "zone_field_volcanic", nodeId = "field_volcanic_ore", itemId = "ore", displayName = "Mining Node", amount = 1, uses = 2, respawnSeconds = 12f, local = new Vector2(10f, -1f) },
            new FieldNodeRecord { zoneId = "zone_field_frozen", nodeId = "field_frozen_herb", itemId = "herb", displayName = "Herb Node", amount = 1, uses = 2, respawnSeconds = 12f, local = new Vector2(10f, -1f) },
            new FieldNodeRecord { zoneId = "zone_field_tropical", nodeId = "field_tropical_wood", itemId = "wood", displayName = "Wood Node", amount = 1, uses = 2, respawnSeconds = 12f, local = new Vector2(10f, -1f) },
            new FieldNodeRecord { zoneId = "zone_field_aquatic", nodeId = "field_aquatic_herb", itemId = "herb", displayName = "Herb Node", amount = 1, uses = 2, respawnSeconds = 12f, local = new Vector2(10f, -1f) },
            new FieldNodeRecord { zoneId = "zone_field_sky", nodeId = "field_sky_sprout", itemId = "sprout", displayName = "Sprout Node", amount = 1, uses = 2, respawnSeconds = 12f, local = new Vector2(11f, -1f) }
        };

        public static readonly FieldChestRecord[] FieldChests =
        {
            new FieldChestRecord { id = "chest_field_volcanic", zoneId = "zone_field_volcanic", itemId = "quest_token", itemAmount = 1, coins = ChestCoins, local = new Vector2(8f, 8f) },
            new FieldChestRecord { id = "chest_field_frozen", zoneId = "zone_field_frozen", itemId = "quest_token", itemAmount = 1, coins = ChestCoins, local = new Vector2(8f, 8f) },
            new FieldChestRecord { id = "chest_field_tropical", zoneId = "zone_field_tropical", itemId = "quest_token", itemAmount = 1, coins = ChestCoins, local = new Vector2(8f, 8f) },
            new FieldChestRecord { id = "chest_field_aquatic", zoneId = "zone_field_aquatic", itemId = "quest_token", itemAmount = 1, coins = ChestCoins, local = new Vector2(8f, 8f) },
            new FieldChestRecord { id = "chest_field_sky", zoneId = "zone_field_sky", itemId = "quest_token", itemAmount = 1, coins = ChestCoins, local = new Vector2(4f, 9f) }
        };

        public static readonly FieldBossRecord[] FieldBosses =
        {
            new FieldBossRecord { id = "boss_field_volcanic", zoneId = "zone_field_volcanic", rosterPrefix = "Cyclops", local = new Vector2(0f, 9f) },
            new FieldBossRecord { id = "boss_field_frozen", zoneId = "zone_field_frozen", rosterPrefix = "Cyclops", local = new Vector2(0f, 9f) },
            new FieldBossRecord { id = "boss_field_tropical", zoneId = "zone_field_tropical", rosterPrefix = "Cyclops", local = new Vector2(0f, 9f) },
            new FieldBossRecord { id = "boss_field_aquatic", zoneId = "zone_field_aquatic", rosterPrefix = "Cyclops", local = new Vector2(0f, 9f) },
            new FieldBossRecord { id = "boss_field_sky", zoneId = "zone_field_sky", rosterPrefix = "Cyclops", local = new Vector2(0f, 9f) }
        };

        public static readonly ZoneRecord[] Zones =
        {
            new ZoneRecord { id = "zone_hub", name = "Village Hub", type = "hub", zone = WorldZone.Hub, visualSlot = "slot.zone.hub", unlock = "always", layoutSource = "WorldCatalog.Hub" },
            new ZoneRecord { id = "zone_training", name = "Training Field", type = "training", zone = WorldZone.Training, visualSlot = "slot.zone.training", unlock = "always", layoutSource = "WorldCatalog.Training" },
            new ZoneRecord { id = "zone_resource", name = "Resource Field", type = "gather", zone = WorldZone.Resource, visualSlot = "slot.zone.resource", unlock = "always", layoutSource = "WorldCatalog.Resource" },
            new ZoneRecord { id = "zone_farm", name = "Hero Farm", type = "farm", zone = WorldZone.Farm, visualSlot = "slot.zone.farm", unlock = "always", layoutSource = "WorldCatalog.Farm" },
            new ZoneRecord { id = "zone_boss", name = "Cyclops Ridge", type = "boss", zone = WorldZone.Boss, visualSlot = "slot.zone.boss", unlock = "always", layoutSource = "WorldCatalog.Boss" },
            new ZoneRecord { id = "zone_explore", name = "Exploration Field", type = "explore", zone = WorldZone.Explore, origin = new Vector2(120f, 0f), spawn = new Vector2(120f, -3.2f), boundsMin = new Vector2(112f, -8f), boundsMax = new Vector2(128f, 8f), visualSlot = "slot.zone.explore", unlock = "always" },
            new ZoneRecord { id = "zone_treasure", name = "Treasure Area", type = "treasure", zone = WorldZone.Treasure, origin = new Vector2(120f, 80f), spawn = new Vector2(120f, 76.8f), boundsMin = new Vector2(112f, 73f), boundsMax = new Vector2(128f, 88f), visualSlot = "slot.zone.treasure", unlock = "always" },
            new ZoneRecord { id = "zone_ward", name = "Ward Field", type = "ward", zone = WorldZone.Ward, origin = new Vector2(-120f, 0f), spawn = new Vector2(-120f, -3.2f), boundsMin = new Vector2(-128f, -8f), boundsMax = new Vector2(-112f, 8f), visualSlot = "slot.zone.ward", unlock = "always" },
            new ZoneRecord { id = "zone_boss_extra", name = "Second Ridge", type = "boss", zone = WorldZone.BossExtra, origin = new Vector2(120f, -80f), spawn = new Vector2(120f, -83.2f), boundsMin = new Vector2(112f, -88f), boundsMax = new Vector2(128f, -72f), visualSlot = "slot.zone.boss_extra", unlock = "always" },
            new ZoneRecord { id = "zone_housing", name = "Village Expansion", type = "housing", zone = WorldZone.Housing, origin = new Vector2(-120f, 80f), spawn = new Vector2(-120f, 76.8f), boundsMin = new Vector2(-128f, 73f), boundsMax = new Vector2(-112f, 90f), visualSlot = "slot.zone.housing", unlock = "always" }
        };

        public static readonly PoiRecord[] Points =
        {
            new PoiRecord { id = "poi_chest", zoneId = "zone_treasure", kind = "chest", visualSlot = "slot.poi.chest", localOffset = new Vector2(0f, 1.6f) },
            new PoiRecord { id = "poi_ward", zoneId = "zone_ward", kind = "ward", visualSlot = "slot.poi.ward", localOffset = new Vector2(0f, 1.4f) },
            new PoiRecord { id = "poi_water", zoneId = "zone_explore", kind = "hazard", visualSlot = "slot.biome.water", localOffset = new Vector2(-2.2f, 1.6f) },
            new PoiRecord { id = "poi_lava", zoneId = "zone_explore", kind = "hazard", visualSlot = "slot.biome.lava", localOffset = new Vector2(2.2f, 1.6f) }
        };

        public static readonly InteractionRecord[] Interactions =
        {
            new InteractionRecord { id = "use_chest", poiId = "poi_chest", verb = "open" },
            new InteractionRecord { id = "use_ward", poiId = "poi_ward", verb = "start" },
            new InteractionRecord { id = "use_slot", poiId = "poi_housing", verb = "select" }
        };

        public static readonly ActivityRecord[] Activities =
        {
            new ActivityRecord { id = "act_chest", zoneId = "zone_treasure", kind = "treasure", rewardItem = "quest_token", rewardCoins = ChestCoins },
            new ActivityRecord { id = "act_ward", zoneId = "zone_ward", kind = "ward", rewardCoins = WardCoins },
            new ActivityRecord { id = "act_explore", zoneId = "zone_explore", kind = "explore" }
        };

        public static readonly RegionBossRecord[] Bosses =
        {
            new RegionBossRecord { id = "boss_cyclops", zoneId = "zone_boss", rosterPrefix = "Cyclops", hpBar = "roster.bossBar", spawnOffset = new Vector2(0f, 2.2f), expReward = 25, reentry = "on_enter" },
            new RegionBossRecord { id = "boss_ridge_2", zoneId = "zone_boss_extra", rosterPrefix = "Cyclops", hpBar = "roster.bossBar", spawnOffset = new Vector2(0f, 2.2f), expReward = 25, reentry = "on_enter" }
        };

        public static readonly TransitionRecord[] Transitions =
        {
            new TransitionRecord { id = "to_explore", fromId = "zone_hub", toId = "zone_explore" },
            new TransitionRecord { id = "to_treasure", fromId = "zone_hub", toId = "zone_treasure" },
            new TransitionRecord { id = "to_ward", fromId = "zone_hub", toId = "zone_ward" },
            new TransitionRecord { id = "to_ridge_2", fromId = "zone_hub", toId = "zone_boss_extra" },
            new TransitionRecord { id = "to_housing", fromId = "zone_hub", toId = "zone_housing" }
        };

        public static readonly HazardRecord[] Hazards =
        {
            new HazardRecord { id = "hazard_water", biomeId = "biome_water", center = new Vector2(-2.2f, 1.6f), halfSize = new Vector2(0.8f, 0.6f) },
            new HazardRecord { id = "hazard_lava", biomeId = "biome_lava", center = new Vector2(2.2f, 1.6f), halfSize = new Vector2(0.8f, 0.6f) }
        };

        public static readonly SlotRecord[] VillageSlots =
        {
            new SlotRecord { id = "slot_building", kind = "BuildingSlot", visualSlot = "slot.build.building", localOffset = new Vector2(-2.2f, 1.5f) },
            new SlotRecord { id = "slot_house", kind = "HousingSlot", visualSlot = "slot.build.house", localOffset = new Vector2(-0.7f, 1.5f) },
            new SlotRecord { id = "slot_decor", kind = "DecorationSlot", visualSlot = "slot.build.decor", localOffset = new Vector2(0.8f, 1.5f) },
            new SlotRecord { id = "slot_service", kind = "NPCServiceSlot", visualSlot = "slot.build.service", localOffset = new Vector2(2.3f, 1.5f) }
        };

        public static readonly SkillNodeRecord[] SkillNodes =
        {
            new SkillNodeRecord { id = "node_slash", skillKey = "R", requiredLevel = 1 },
            new SkillNodeRecord { id = "node_slot_1", skillKey = "1", requiredLevel = 1 }
        };

        public static readonly GemRecord[] Gems =
        {
            new GemRecord { id = "gem_blank", socket = "weapon", effectId = "effect_none" }
        };

        public static readonly EquipEffectRecord[] EquipEffects =
        {
            new EquipEffectRecord { itemId = "training_blade", effectId = "power_5", appliedToCombat = true },
            new EquipEffectRecord { itemId = "basic_armor", effectId = "def_10_hp_40", appliedToCombat = true },
            new EquipEffectRecord { itemId = "basic_charm", effectId = "power_5_hp_20", appliedToCombat = true }
        };

        public static bool TryLayout(WorldZone zone, out ZoneLayout layout)
        {
            for (int i = 0; i < Zones.Length; i++)
            {
                if (Zones[i].zone != zone || !string.IsNullOrEmpty(Zones[i].layoutSource)) continue;
                layout = new ZoneLayout
                {
                    origin = Zones[i].origin,
                    spawn = Zones[i].spawn,
                    boundsMin = Zones[i].boundsMin,
                    boundsMax = Zones[i].boundsMax
                };
                return true;
            }
            layout = default;
            return false;
        }

        public static ZoneRecord FindZone(WorldZone zone)
        {
            for (int i = 0; i < Zones.Length; i++)
                if (Zones[i].zone == zone) return Zones[i];
            return default;
        }

        public static FieldMapRecord FindField(string zoneId)
        {
            for (int i = 0; i < FieldMaps.Length; i++)
                if (FieldMaps[i].zoneId == zoneId) return FieldMaps[i];
            return default;
        }

        public static RegionRecord FindFieldRegion(string regionId)
        {
            for (int i = 0; i < FieldRegions.Length; i++)
                if (FieldRegions[i].id == regionId) return FieldRegions[i];
            return default;
        }

        public static bool TryFieldLayout(string zoneId, out ZoneLayout layout)
        {
            var map = FindField(zoneId);
            if (string.IsNullOrEmpty(map.zoneId))
            {
                layout = default;
                return false;
            }
            layout = new ZoneLayout
            {
                origin = map.origin,
                spawn = map.origin + map.spawnLocal,
                boundsMin = map.origin - map.boundsHalf,
                boundsMax = map.origin + map.boundsHalf
            };
            return true;
        }

        public static string FieldChestFlag(string chestId) => "field.chest." + chestId;

        public static string FieldBossFlag(string encounterId) => "field.boss." + encounterId;

        public static string HazardAt(Vector2 localPoint)
        {
            for (int i = 0; i < Hazards.Length; i++)
            {
                var hazard = Hazards[i];
                if (Mathf.Abs(localPoint.x - hazard.center.x) <= hazard.halfSize.x &&
                    Mathf.Abs(localPoint.y - hazard.center.y) <= hazard.halfSize.y)
                    return hazard.id;
            }
            return "";
        }
    }

    public static class WorldFlags
    {
        public static int Get(string id) => PlayerPrefs.GetInt(Key(id), 0);

        public static void Set(string id, int value)
        {
            PlayerPrefs.SetInt(Key(id), value);
            PlayerPrefs.Save();
        }

        public static string GetText(string id) => PlayerPrefs.GetString(Key(id), "");

        public static void SetText(string id, string value)
        {
            PlayerPrefs.SetString(Key(id), value ?? "");
            PlayerPrefs.Save();
        }

        static string Key(string id) => WorldCatalog.SavePrefix + "flag." + id;
    }

    /// <summary>
    /// Optional travel. Off unless Enabled is set. It does not read keys and does not change combat.
    /// </summary>
    public static class HeroTravel
    {
        public static bool Enabled;
        public const float RollSpeed = 8f;
        public const float RollSeconds = 0.12f;

        public static bool SampleRoll()
        {
            if (!Enabled) return false;
            var hero = GameObject.FindGameObjectWithTag("Player");
            var move = hero != null ? hero.GetComponent<PlayerMovement>() : null;
            if (move == null) return false;
            move.OverrideVelocity(Vector2.right * RollSpeed, RollSeconds);
            return true;
        }
    }
}
