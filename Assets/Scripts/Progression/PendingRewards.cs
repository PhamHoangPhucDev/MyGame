using UnityEngine;

namespace GameplayPrototype
{
    public enum Entitlement
    {
        NotEligible = 0,
        Pending = 1,
        Granted = 2,
        Unverified = 3
    }

    /// <summary>
    /// Once-only rewards (GL-007). PENDING grants itself as soon as the Bag can hold it; never twice.
    /// Old saves only become PENDING with reliable evidence, otherwise UNVERIFIED.
    /// </summary>
    public static class PendingRewards
    {
        public const string CharmL1 = "charm_l1";
        public const string StarterSeed = "starter_seed";
        /// <summary>Quest whose activation makes the starter Sprout seed eligible.</summary>
        public const string StarterSeedQuest = "q_road_farm";
        public const string PetEgg = "pet_egg";

        struct Row
        {
            public string id;
            public string itemId;
            public int amount;
        }

        static readonly Row[] Rows =
        {
            new Row { id = CharmL1, itemId = "basic_charm", amount = 1 },
            new Row { id = StarterSeed, itemId = "seed_sprout", amount = 1 },
            new Row { id = PetEgg, itemId = "mystery_pet_egg", amount = 1 }
        };

        public static string Key(string id) => WorldCatalog.SavePrefix + "ent." + id;
        public const string MigratedKey = WorldCatalog.SavePrefix + "ent.migrated";

        public static Entitlement Get(string id) => (Entitlement)PlayerPrefs.GetInt(Key(id), 0);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => WorldEvents.DungeonCleared -= OnDungeonCleared;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            WorldEvents.DungeonCleared -= OnDungeonCleared;
            WorldEvents.DungeonCleared += OnDungeonCleared;
        }

        static void OnDungeonCleared(int level)
        {
            if (level == 1) MarkEligible(CharmL1);
        }

        public static void MarkEligible(string id)
        {
            if (Get(id) != Entitlement.NotEligible) return;
            WorldTx.Begin().SetInt(Key(id), (int)Entitlement.Pending).Commit();
            TryFulfill();
        }

        /// <summary>Grants every PENDING row that fits. Safe to call any time.</summary>
        public static void TryFulfill()
        {
            for (int i = 0; i < Rows.Length; i++)
            {
                var row = Rows[i];
                if (Get(row.id) != Entitlement.Pending) continue;
                int have = WorldSave.GetQty(row.itemId);
                int cap = WorldContent.StackLimit(row.itemId);
                if (cap > 0 && have + row.amount > cap) continue;
                if (Gear.IsEquipment(row.itemId) && Gear.OwnedCopies(row.itemId) > 0) continue;
                string item = row.itemId;
                int amount = row.amount;
                var tx = WorldTx.Begin()
                    .SetInt(WorldSave.KeyItem(item), have + amount)
                    .SetInt(Key(row.id), (int)Entitlement.Granted)
                    .After(() => WorldEvents.RaiseItemGained(item, amount))
                    .After(() => WorldNavigator.Instance?.Flash("Reward: " + WorldContent.FindItem(item).displayName));
                if (row.id == PetEgg && WorldSave.QuestState("q_pet_intro") == 1)
                    tx.SetInt(WorldSave.KeyQuest("q_pet_intro"), 2);
                tx.Commit();
            }
        }

        /// <summary>One-time classification of saves made before the ledger existed.</summary>
        public static void MigrateLegacy()
        {
            if (PlayerPrefs.GetInt(MigratedKey, 0) == 1) return;
            var tx = WorldTx.Begin();
            if (!PlayerPrefs.HasKey(Key(CharmL1)))
            {
                // The world's own dungeon.1 flag is written only by a real Level 1 victory.
                if (WorldFlags.Get("dungeon.1") == 1) tx.SetInt(Key(CharmL1), (int)Entitlement.Pending);
                else if (GameProgress.HighestUnlockedLevel >= 2) tx.SetInt(Key(CharmL1), (int)Entitlement.Unverified);
            }
            if (!PlayerPrefs.HasKey(Key(StarterSeed)))
            {
                int q = WorldSave.QuestState(StarterSeedQuest);
                if (q == 1) tx.SetInt(Key(StarterSeed), (int)Entitlement.Pending);
                else if (q == 2) tx.SetInt(Key(StarterSeed), (int)Entitlement.Unverified);
            }
            tx.SetInt(MigratedKey, 1).Commit();
        }
    }
}
