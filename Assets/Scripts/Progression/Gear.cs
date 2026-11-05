using UnityEngine;

namespace GameplayPrototype
{
    public struct GearStat
    {
        public string id;
        public int power;
        public int def;
        public int maxHp;
    }

    /// <summary>
    /// Equipment (Weapon/Armor/Accessory), Gem sockets and the derived Hero stats.
    /// Each equipment item exists at most once, so its item ID is its instance identity and the socket key follows it
    /// through Bag, Storage and the worn slot. An equipped item stays counted in the Bag (legacy layout).
    /// </summary>
    public static class Gear
    {
        public static readonly string[] Slots = { "weapon", "armor", "accessory" };

        public static readonly GearStat[] Equipment =
        {
            new GearStat { id = "training_blade", power = 5 },
            new GearStat { id = "basic_armor", def = 10, maxHp = 40 },
            new GearStat { id = "basic_charm", power = 5, maxHp = 20 }
        };

        public static readonly GearStat[] Gems =
        {
            new GearStat { id = "gem_ruby", power = 3 },
            new GearStat { id = "gem_sapphire", def = 5 },
            new GearStat { id = "gem_emerald", maxHp = 20 }
        };

        public const int GemTypeLimit = 3;
        const string HpKey = WorldCatalog.SavePrefix + "hero.hp";

        public static int TotalPower { get; private set; }
        public static int TotalDef { get; private set; }
        public static int BonusMaxHp { get; private set; }
        /// <summary>Hero base Max HP before Class and gear. LevelFlow sets the campaign value.</summary>
        public static int BaseMaxHp = 400;
        public static event System.Action Changed;

        static bool _hpRestored;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _hpRestored = false;
            Changed = null;
            GameplayClock.Flushing -= SaveHp;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            GameplayClock.Flushing -= SaveHp;
            GameplayClock.Flushing += SaveHp;
            Recompute();
        }

        public static bool IsEquipment(string itemId) => FindStat(Equipment, itemId).id != null;
        public static bool IsGem(string itemId) => FindStat(Gems, itemId).id != null;

        public static string SlotOf(string itemId) => WorldContent.FindItem(itemId).equipSlot ?? "";

        public static string SocketKey(string equipId) => WorldCatalog.SavePrefix + "socket." + equipId;
        public static string Socket(string equipId) => PlayerPrefs.GetString(SocketKey(equipId), "");

        public static bool IsEquipped(string itemId)
        {
            string slot = SlotOf(itemId);
            return !string.IsNullOrEmpty(slot) && WorldSave.Equipped(slot) == itemId && WorldSave.GetQty(itemId) > 0;
        }

        /// <summary>Copies across Bag and Storage (equipped copies live in the Bag).</summary>
        public static int OwnedCopies(string itemId) => WorldSave.GetQty(itemId) + WorldSave.StoreQty(itemId);

        /// <summary>Gem copies across Bag, Storage and every equipment socket.</summary>
        public static int GemOwned(string gemId)
        {
            int n = WorldSave.GetQty(gemId) + WorldSave.StoreQty(gemId);
            for (int i = 0; i < Equipment.Length; i++)
                if (Socket(Equipment[i].id) == gemId) n++;
            return n;
        }

        public static void Recompute()
        {
            int p = 0, d = 0, hp = 0;
            for (int i = 0; i < Slots.Length; i++)
            {
                string id = WorldSave.Equipped(Slots[i]);
                if (string.IsNullOrEmpty(id) || WorldSave.GetQty(id) <= 0 || SlotOf(id) != Slots[i]) continue;
                var s = FindStat(Equipment, id);
                p += s.power; d += s.def; hp += s.maxHp;
                var g = FindStat(Gems, Socket(id));
                p += g.power; d += g.def; hp += g.maxHp;
            }
            TotalPower = p;
            TotalDef = d;
            BonusMaxHp = hp;
        }

        /// <summary>round(Base * (1 + Power/100) * (1 + NodePercent/100)), rounded once.</summary>
        public static int ScaleOutgoing(int baseDamage, float nodePercent)
        {
            if (baseDamage <= 0) return baseDamage;
            double v = baseDamage * (1d + TotalPower / 100d) * (1d + nodePercent / 100d);
            return Mathf.Max(1, (int)System.Math.Round(v, System.MidpointRounding.AwayFromZero));
        }

        public static int EffectiveMaxHp(ClassKit kit)
        {
            float mult = kit != null ? kit.maxHpMultiplier : 1f;
            return Mathf.Max(1, Mathf.RoundToInt(BaseMaxHp * mult) + BonusMaxHp);
        }

        /// <summary>Applies Max HP and DEF to the Hero. Never heals unless refill is asked for (level start, first spawn).</summary>
        public static void ApplyToHero(Health health, bool refill)
        {
            if (health == null) return;
            Recompute();
            var classes = health.GetComponent<ClassCombat>();
            var kit = classes != null ? classes.Active : null;
            health.Defense = TotalDef;
            health.Configure(EffectiveMaxHp(kit), refill);
            Changed?.Invoke();
        }

        static Health Hero()
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            return go != null ? go.GetComponent<Health>() : null;
        }

        static void Refresh()
        {
            ApplyToHero(Hero(), false);
        }

        // ---------- Health persistence (GL-006) ----------

        static void SaveHp()
        {
            var h = Hero();
            if (h == null || h.IsDead || !_hpRestored) return;
            PlayerPrefs.SetInt(HpKey, h.CurrentHp);
        }

        /// <summary>First entry to the world this session: derive Max HP, then clamp the saved absolute HP.</summary>
        public static void RestoreSavedHp(Health health)
        {
            if (health == null || _hpRestored) return;
            _hpRestored = true;
            ApplyToHero(health, false);
            if (!PlayerPrefs.HasKey(HpKey)) return;
            int saved = PlayerPrefs.GetInt(HpKey, health.MaxHp);
            if (saved > 0 && !health.IsDead) health.SetCurrent(saved);
        }

        // ---------- Equip ----------

        public static bool Equip(string itemId)
        {
            if (!IsEquipment(itemId)) return WorldServices.Fail(Deny.Invalid, "Cannot equip " + itemId);
            if (WorldSave.GetQty(itemId) <= 0) return WorldServices.Fail(Deny.NotOwned, "Not in bag");
            if (IsEquipped(itemId)) return WorldServices.Ok("Already equipped");
            var gate = Gate();
            if (gate != Deny.None) return WorldServices.Fail(gate, GameplayClock.DenyText(gate));
            WorldTx.Begin().SetString(WorldSave.KeyEquip(SlotOf(itemId)), itemId).After(Refresh).Commit();
            return WorldServices.Ok("Equipped " + WorldContent.FindItem(itemId).displayName);
        }

        public static bool Unequip(string slot)
        {
            string id = WorldSave.Equipped(slot);
            if (string.IsNullOrEmpty(id)) return WorldServices.Fail(Deny.NotOwned, "Nothing equipped");
            var gate = Gate();
            if (gate != Deny.None) return WorldServices.Fail(gate, GameplayClock.DenyText(gate));
            WorldTx.Begin().SetString(WorldSave.KeyEquip(slot), "").After(Refresh).Commit();
            return WorldServices.Ok("Unequipped " + id);
        }

        public static bool Craft(RecipeDef recipe)
        {
            if (OwnedCopies(recipe.result) > 0) return WorldServices.Fail(Deny.LimitReached, "Already own " + recipe.result);
            int a = WorldSave.GetQty(recipe.a), b = WorldSave.GetQty(recipe.b);
            if (a < recipe.aCount) return WorldServices.Fail(Deny.InsufficientResources, "Need " + recipe.aCount + " " + recipe.a);
            if (b < recipe.bCount) return WorldServices.Fail(Deny.InsufficientResources, "Need " + recipe.bCount + " " + recipe.b);
            string result = recipe.result;
            WorldTx.Begin()
                .SetInt(WorldSave.KeyItem(recipe.a), a - recipe.aCount)
                .SetInt(WorldSave.KeyItem(recipe.b), b - recipe.bCount)
                .SetInt(WorldSave.KeyItem(result), WorldSave.GetQty(result) + 1)
                .After(() => WorldEvents.RaiseItemGained(result, 1))
                .After(PendingRewards.TryFulfill)
                .Commit();
            return WorldServices.Ok("Crafted " + WorldContent.FindItem(result).displayName);
        }

        // ---------- Gems ----------

        public static bool InsertGem(string equipId, string gemId)
        {
            if (!IsEquipment(equipId) || !IsGem(gemId)) return WorldServices.Fail(Deny.Invalid, "Invalid gem or item");
            if (WorldSave.GetQty(equipId) <= 0) return WorldServices.Fail(Deny.NotOwned, "Take the item out of storage first");
            if (!string.IsNullOrEmpty(Socket(equipId))) return WorldServices.Fail(Deny.AlreadyDone, "Socket is full");
            int have = WorldSave.GetQty(gemId);
            if (have <= 0) return WorldServices.Fail(Deny.NotOwned, "No loose " + gemId);
            var gate = Gate();
            if (gate != Deny.None) return WorldServices.Fail(gate, GameplayClock.DenyText(gate));
            WorldTx.Begin()
                .SetString(SocketKey(equipId), gemId)
                .SetInt(WorldSave.KeyItem(gemId), have - 1)
                .After(Refresh)
                .Commit();
            return WorldServices.Ok("Socketed " + gemId);
        }

        public static bool RemoveGem(string equipId)
        {
            if (!IsEquipment(equipId)) return WorldServices.Fail(Deny.Invalid, "Invalid item");
            if (WorldSave.GetQty(equipId) <= 0) return WorldServices.Fail(Deny.NotOwned, "Take the item out of storage first");
            string gem = Socket(equipId);
            if (string.IsNullOrEmpty(gem)) return WorldServices.Fail(Deny.NotOwned, "Socket is empty");
            int have = WorldSave.GetQty(gem);
            if (have + 1 > WorldContent.StackLimit(gem)) return WorldServices.Fail(Deny.BagFull, "Bag full");
            var gate = Gate();
            if (gate != Deny.None) return WorldServices.Fail(gate, GameplayClock.DenyText(gate));
            WorldTx.Begin()
                .SetString(SocketKey(equipId), "")
                .SetInt(WorldSave.KeyItem(gem), have + 1)
                .After(Refresh)
                .Commit();
            return WorldServices.Ok("Removed " + gem);
        }

        /// <summary>Single exchange. Capacity is checked on the final state, so a full stack of the new gem is fine.</summary>
        public static bool ReplaceGem(string equipId, string newGem)
        {
            if (!IsEquipment(equipId) || !IsGem(newGem)) return WorldServices.Fail(Deny.Invalid, "Invalid gem or item");
            if (WorldSave.GetQty(equipId) <= 0) return WorldServices.Fail(Deny.NotOwned, "Take the item out of storage first");
            string old = Socket(equipId);
            if (string.IsNullOrEmpty(old)) return InsertGem(equipId, newGem);
            if (old == newGem) return WorldServices.Ok("Same gem");
            int newHave = WorldSave.GetQty(newGem);
            int oldHave = WorldSave.GetQty(old);
            if (newHave <= 0) return WorldServices.Fail(Deny.NotOwned, "No loose " + newGem);
            if (oldHave + 1 > WorldContent.StackLimit(old)) return WorldServices.Fail(Deny.BagFull, "Bag full");
            var gate = Gate();
            if (gate != Deny.None) return WorldServices.Fail(gate, GameplayClock.DenyText(gate));
            WorldTx.Begin()
                .SetString(SocketKey(equipId), newGem)
                .SetInt(WorldSave.KeyItem(newGem), newHave - 1)
                .SetInt(WorldSave.KeyItem(old), oldHave + 1)
                .After(Refresh)
                .Commit();
            return WorldServices.Ok("Swapped " + old + " for " + newGem);
        }

        static Deny Gate() => GameplayClock.GateApplies ? GameplayClock.CheckOutOfCombat() : Deny.None;

        static GearStat FindStat(GearStat[] table, string id)
        {
            if (string.IsNullOrEmpty(id)) return default;
            for (int i = 0; i < table.Length; i++)
                if (table[i].id == id) return table[i];
            return default;
        }
    }
}
