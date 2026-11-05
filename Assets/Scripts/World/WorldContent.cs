using UnityEngine;

namespace GameplayPrototype
{
    public enum ItemKind
    {
        Resource = 0,
        Consumable = 1,
        Equipment = 2,
        Quest = 3,
        Currency = 4,
        Gem = 5,
        Material = 6
    }

    public enum QuestGoal
    {
        Kill = 0,
        Gather = 1,
        Harvest = 2,
        ClearDungeon = 3,
        DefeatBoss = 4,
        Talk = 5
    }

    public struct ItemDef
    {
        public string id;
        public string displayName;
        public ItemKind kind;
        public int stack;
        public int buy;
        public int sell;
        public string equipSlot;
    }

    public struct NpcDef
    {
        public string id;
        public string name;
        public string service;
        public Vector2 offset;
        public string line;
    }

    public struct TrainGroup
    {
        public string id;
        public bool archer;
        public int exp;
        public string lootItemId;
        public float respawnSeconds;
    }

    public struct QuestDef
    {
        public string id;
        public string title;
        public QuestGoal goal;
        public string target;
        public int count;
        public int coinReward;
        public int expReward;
        public string requires;
        public string nextId;
        public string rewardItem;
        public string unlockFlag;
        public string chain;
    }

    public struct RecipeDef
    {
        public string id;
        public string a;
        public int aCount;
        public string b;
        public int bCount;
        public string result;
    }

    /// <summary>
    /// Skeleton content. Replace rows later. Gameplay reads this table.
    /// </summary>
    public static class WorldContent
    {
        public const int SaveVersion = 1;

        public static readonly ItemDef[] Items =
        {
            new ItemDef { id = "ore", displayName = "Ore", kind = ItemKind.Resource, stack = 99, sell = 2 },
            new ItemDef { id = "wood", displayName = "Wood", kind = ItemKind.Resource, stack = 99, sell = 1 },
            new ItemDef { id = "sprout", displayName = "Sprout", kind = ItemKind.Resource, stack = 99, sell = 1 },
            new ItemDef { id = "herb", displayName = "Herb", kind = ItemKind.Resource, stack = 99, sell = 2 },
            new ItemDef { id = "potion", displayName = "Potion", kind = ItemKind.Consumable, stack = 20, buy = 5 },
            new ItemDef { id = "training_blade", displayName = "Training Blade", kind = ItemKind.Equipment, stack = 1, equipSlot = "weapon" },
            new ItemDef { id = "quest_token", displayName = "Quest Token", kind = ItemKind.Quest, stack = 10 },
            new ItemDef { id = "coin", displayName = "Coin", kind = ItemKind.Currency, stack = 9999 },
            new ItemDef { id = "seed_sprout", displayName = "Sprout Seed", kind = ItemKind.Resource, stack = 20, buy = 1 },
            new ItemDef { id = "seed_fiber", displayName = "Fiber Seed", kind = ItemKind.Resource, stack = 20, buy = 2 },
            new ItemDef { id = "fiber", displayName = "Fiber", kind = ItemKind.Resource, stack = 99 },
            new ItemDef { id = "basic_armor", displayName = "Basic Armor", kind = ItemKind.Equipment, stack = 1, equipSlot = "armor" },
            new ItemDef { id = "basic_charm", displayName = "Basic Charm", kind = ItemKind.Equipment, stack = 1, equipSlot = "accessory" },
            new ItemDef { id = "gem_ruby", displayName = "Ruby", kind = ItemKind.Gem, stack = 3, buy = 10 },
            new ItemDef { id = "gem_sapphire", displayName = "Sapphire", kind = ItemKind.Gem, stack = 3, buy = 10 },
            new ItemDef { id = "gem_emerald", displayName = "Emerald", kind = ItemKind.Gem, stack = 3, buy = 15 },
            new ItemDef { id = "egg", displayName = "Egg", kind = ItemKind.Resource, stack = 99, sell = 3 },
            new ItemDef { id = "duck_egg", displayName = "Duck Egg", kind = ItemKind.Resource, stack = 99, sell = 4 },
            new ItemDef { id = "feather", displayName = "Feather", kind = ItemKind.Resource, stack = 99, sell = 2 },
            new ItemDef { id = "carrot", displayName = "Carrot", kind = ItemKind.Resource, stack = 99, sell = 2 },
            new ItemDef { id = "cabbage", displayName = "Cabbage", kind = ItemKind.Resource, stack = 99, sell = 4 },
            new ItemDef { id = "tomato", displayName = "Tomato", kind = ItemKind.Resource, stack = 99, sell = 2 },
            new ItemDef { id = "pumpkin", displayName = "Pumpkin", kind = ItemKind.Resource, stack = 99, sell = 12 },
            new ItemDef { id = "corn", displayName = "Corn", kind = ItemKind.Resource, stack = 99, sell = 4 },
            new ItemDef { id = "seed_carrot", displayName = "Carrot Seed", kind = ItemKind.Resource, stack = 20, buy = 1 },
            new ItemDef { id = "seed_cabbage", displayName = "Cabbage Seed", kind = ItemKind.Resource, stack = 20, buy = 2 },
            new ItemDef { id = "seed_tomato", displayName = "Tomato Seed", kind = ItemKind.Resource, stack = 20, buy = 2 },
            new ItemDef { id = "seed_pumpkin", displayName = "Pumpkin Seed", kind = ItemKind.Resource, stack = 20, buy = 4 },
            new ItemDef { id = "seed_corn", displayName = "Corn Seed", kind = ItemKind.Resource, stack = 20, buy = 3 },
            new ItemDef { id = "mystery_pet_egg", displayName = "Mystery Pet Egg", kind = ItemKind.Consumable, stack = 99 },
            new ItemDef { id = "pet_egg_shard", displayName = "Pet Egg Shard", kind = ItemKind.Material, stack = 999 }
        };

        public static readonly NpcDef[] Npcs =
        {
            new NpcDef { id = "npc_shop", name = "Mira", service = "shop", offset = new Vector2(-2.3f, -1f), line = "I buy ore and sell potions." },
            new NpcDef { id = "npc_smith", name = "Bram", service = "smith", offset = new Vector2(2.3f, -1f), line = "One blade. Ore and wood." },
            new NpcDef { id = "npc_quest", name = "Lark", service = "quest", offset = new Vector2(-2.3f, -3.4f), line = "The road starts here." },
            new NpcDef { id = "npc_storage", name = "Nell", service = "storage", offset = new Vector2(2.3f, -3.4f), line = "I can hold your ore." },
            new NpcDef { id = "npc_map", name = "Map", service = "map", offset = new Vector2(0f, 0.6f), line = "Where do you want to go?" }
        };

        public static readonly TrainGroup[] TrainingGroups =
        {
            new TrainGroup { id = "goblins", archer = false, exp = 8, respawnSeconds = 6f },
            new TrainGroup { id = "archers", archer = true, exp = 8, respawnSeconds = 6f }
        };

        public static readonly QuestDef[] Quests =
        {
            new QuestDef { id = "q_ore", title = "Bring Ore", goal = QuestGoal.Gather, target = "ore", count = 1, coinReward = 5 },
            new QuestDef { id = "q_kill", title = "First Blood", goal = QuestGoal.Kill, target = "goblins", count = 1, coinReward = 5 },
            new QuestDef { id = "q_pet_intro", title = "Người Bạn Đồng Hành", goal = QuestGoal.Gather, target = "wood", count = 1, rewardItem = "mystery_pet_egg" },
            new QuestDef { id = "q_road_kill", chain = "road", title = "Clear the yard", goal = QuestGoal.Kill, target = "goblins", count = 1, coinReward = 5, nextId = "q_road_ore" },
            new QuestDef { id = "q_road_ore", chain = "road", title = "Bring ore", goal = QuestGoal.Gather, target = "ore", count = 1, requires = "q_road_kill", coinReward = 5, nextId = "q_road_farm" },
            new QuestDef { id = "q_road_farm", chain = "road", title = "Harvest a sprout", goal = QuestGoal.Harvest, target = "sprout", count = 1, requires = "q_road_ore", nextId = "q_road_craft" },
            new QuestDef { id = "q_road_craft", chain = "road", title = "Forge a blade", goal = QuestGoal.Gather, target = "training_blade", count = 1, requires = "q_road_farm", nextId = "q_road_dungeon" },
            new QuestDef { id = "q_road_dungeon", chain = "road", title = "Clear depth 1", goal = QuestGoal.ClearDungeon, target = "1", count = 1, requires = "q_road_craft", nextId = "q_road_boss" },
            new QuestDef { id = "q_road_boss", chain = "road", title = "Defeat Cyclops", goal = QuestGoal.DefeatBoss, target = "cyclops", count = 1, requires = "q_road_dungeon", coinReward = 10, expReward = 10, unlockFlag = "unlock.wild" }
        };

        public static readonly RecipeDef[] Recipes =
        {
            new RecipeDef { id = "recipe_blade", a = "ore", aCount = 1, b = "wood", bCount = 1, result = "training_blade" },
            new RecipeDef { id = "recipe_armor", a = "ore", aCount = 2, b = "wood", bCount = 2, result = "basic_armor" }
        };

        public static RecipeDef Blade => Recipes[0];
        public static RecipeDef Armor => Recipes[1];

        public struct LootRow
        {
            public string sourceId;
            public string itemId;
            public int amount;
        }

        public static readonly LootRow[] Loot =
        {
            new LootRow { sourceId = "goblins", itemId = "quest_token", amount = 1 }
        };

        public struct Payout
        {
            public string id;
            public int coins;
            public int exp;
            public bool once;
        }

        public static readonly Payout[] DungeonPayouts =
        {
            new Payout { id = "1", coins = 5, once = true }
        };

        public static readonly Payout[] BossPayouts =
        {
            new Payout { id = "cyclops", coins = 0, exp = 25, once = false }
        };

        public struct UnlockRow
        {
            public string flag;
            public string afterQuest;
            public string zoneId;
        }

        public static readonly UnlockRow[] Unlocks =
        {
            new UnlockRow { flag = "unlock.wild", afterQuest = "q_road_boss", zoneId = "zone_future" }
        };

        public static readonly CropConfig[] Crops =
        {
            new CropConfig { cropId = "test_sprout", itemId = "sprout", displayName = "Sprout", growSeconds = 8f, yield = 1 },
            new CropConfig { cropId = "fiber", itemId = "fiber", displayName = "Fiber", growSeconds = 12f, yield = 1 }
        };

        public static GatherConfig Herb = new GatherConfig
        {
            nodeId = "herb_node",
            itemId = "herb",
            displayName = "Herb Node",
            amount = 1,
            uses = 2,
            respawnSeconds = 12f
        };

        public const float DayLengthSeconds = 120f;
        public const string BossId = "cyclops";
        public const int DungeonCoinReward = 5;

        public static ItemDef FindItem(string id)
        {
            for (int i = 0; i < Items.Length; i++)
                if (Items[i].id == id) return Items[i];
            return default;
        }

        public static int StackLimit(string id)
        {
            var item = FindItem(id);
            return string.IsNullOrEmpty(item.id) ? 0 : item.stack;
        }

        public static QuestDef FindQuest(string id)
        {
            for (int i = 0; i < Quests.Length; i++)
                if (Quests[i].id == id) return Quests[i];
            return default;
        }

        public static LootRow FindLoot(string sourceId)
        {
            for (int i = 0; i < Loot.Length; i++)
                if (Loot[i].sourceId == sourceId) return Loot[i];
            return default;
        }

        public static Payout FindDungeonPayout(string levelId)
        {
            for (int i = 0; i < DungeonPayouts.Length; i++)
                if (DungeonPayouts[i].id == levelId) return DungeonPayouts[i];
            return new Payout { id = levelId, coins = DungeonCoinReward, once = true };
        }
    }
}
