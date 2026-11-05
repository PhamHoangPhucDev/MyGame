using System.Collections.Generic;
using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>First unmet housing beat. Claude's hint bar reads this; it does not draw UI.</summary>
    public enum HousingStep
    {
        HarvestReady,
        ClaimMail,
        CollectEggs,
        NeedResources,
        BuildHouse,
        HouseBuilding,
        PlantFirstBed,
        ExpandLand,
        CheckMail,
        None
    }

    /// <summary>
    /// Four Village slots; one Basic House may ever be built (3 Wood + 2 Ore + 20 Coins, atomic).
    /// Selected (gw.v1.flag.housing.selected) is never ownership; built lives in gw.v1.house.built.
    /// Rest at the built house sets HP to effective Max HP. No upgrade, move, demolish or refund.
    /// </summary>
    public static class Housing
    {
        public const int WoodCost = 3;
        public const int OreCost = 2;
        public const int CoinCost = 20;
        public const string BuiltKey = WorldCatalog.SavePrefix + "house.built";

        static readonly Dictionary<string, WorldInteractable> _slots = new();
        static readonly List<Vector2Int> _beds = new List<Vector2Int>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _slots.Clear(); _beds.Clear(); }

        public static void Register(string slotId, WorldInteractable talk)
        {
            if (!string.IsNullOrEmpty(slotId) && talk != null) _slots[slotId] = talk;
        }

        public static bool IsSlot(string slotId)
        {
            foreach (var s in WorldFoundation.VillageSlots)
                if (s.id == slotId) return true;
            return false;
        }

        public static string Selected => WorldFlags.GetText(WorldFoundation.HousingFlag);
        public static string Built => PlayerPrefs.GetString(BuiltKey, "");
        public static bool HasHouse => !string.IsNullOrEmpty(Built);

        public static bool Select(string slotId)
        {
            if (!IsSlot(slotId)) return WorldServices.Fail(Deny.Invalid, "No such slot");
            if (HasHouse) return WorldServices.Fail(Deny.AlreadyDone, "House already built");
            WorldFlags.SetText(WorldFoundation.HousingFlag, slotId);
            return WorldServices.Ok("Selected " + slotId + " for a house");
        }

        public static bool Build()
        {
            if (HasHouse) return WorldServices.Fail(Deny.AlreadyDone, "House already built");
            string slot = Selected;
            if (!IsSlot(slot)) return WorldServices.Fail(Deny.Invalid, "Select a slot first");
            var hero = Hero();
            if (hero == null) return WorldServices.Fail(Deny.Invalid, "No hero");
            var health = hero.GetComponent<Health>();
            if (health != null && health.IsDead) return WorldServices.Fail(Deny.Dead, "Hero is down");
            if (!InRange(slot, hero)) return WorldServices.Fail(Deny.OutOfRange, "Stand at the slot");
            var gate = GameplayClock.CheckOutOfCombat();
            if (gate != Deny.None) return WorldServices.Fail(gate, GameplayClock.DenyText(gate));
            int wood = WorldSave.GetQty("wood"), ore = WorldSave.GetQty("ore"), coins = WorldSave.Coins;
            if (wood < WoodCost) return WorldServices.Fail(Deny.InsufficientResources, "Need " + WoodCost + " wood");
            if (ore < OreCost) return WorldServices.Fail(Deny.InsufficientResources, "Need " + OreCost + " ore");
            if (coins < CoinCost) return WorldServices.Fail(Deny.InsufficientResources, "Need " + CoinCost + " coins");
            WorldTx.Begin()
                .SetInt(WorldSave.KeyItem("wood"), wood - WoodCost)
                .SetInt(WorldSave.KeyItem("ore"), ore - OreCost)
                .SetInt(WorldSave.KeyCoins, coins - CoinCost)
                .SetString(BuiltKey, slot)
                .After(() => WorldEvents.RaiseCoins(-CoinCost))
                .After(PendingRewards.TryFulfill)
                .Commit();
            return WorldServices.Ok("Built a Basic House");
        }

        public static bool Rest()
        {
            string slot = Built;
            if (string.IsNullOrEmpty(slot)) return WorldServices.Fail(Deny.NotOwned, "No house");
            if (!IsSlot(slot)) return WorldServices.Fail(Deny.Invalid, "House record invalid");
            var hero = Hero();
            var health = hero != null ? hero.GetComponent<Health>() : null;
            if (health == null) return WorldServices.Fail(Deny.Invalid, "No hero");
            if (health.IsDead) return WorldServices.Fail(Deny.Dead, "Hero is down");
            if (!InRange(slot, hero)) return WorldServices.Fail(Deny.OutOfRange, "Stand at your house");
            var gate = GameplayClock.CheckOutOfCombat();
            if (gate != Deny.None) return WorldServices.Fail(gate, GameplayClock.DenyText(gate));
            Gear.ApplyToHero(health, false);
            health.SetCurrent(health.MaxHp);
            return WorldServices.Ok("Rested. HP full");
        }

        /// <summary>Crop id when the step is HarvestReady, otherwise empty. Set by HousingNextStep.</summary>
        public static string StepDetail { get; private set; } = "";

        /// <summary>Next housing beat from the save. Does not mark the tip as seen.</summary>
        public static HousingStep HousingNextStep()
        {
            StepDetail = "";
            if (RipeCrop(out string crop)) { StepDetail = crop; return HousingStep.HarvestReady; }
            if (Mailbox.HasGift) return HousingStep.ClaimMail;
            if (EggWaiting()) return HousingStep.CollectEggs;
            if (!HasHouse)
            {
                var d = BuildMode.Defs[0];
                if (WorldSave.GetQty("wood") < d.wood || WorldSave.GetQty("ore") < d.ore)
                    return HousingStep.NeedResources;
                return HousingStep.BuildHouse;
            }
            if (BuildMode.HouseBuildProgress(out _)) return HousingStep.HouseBuilding;
            if (!HasFirstBed()) return HousingStep.PlantFirstBed;
            if ((HomeArea.Mask & ~16) == 0) return HousingStep.ExpandLand;
            if (Mailbox.HasUnread) return HousingStep.CheckMail;
            return HousingStep.None;
        }

        /// <summary>True until MarkTipSeen. The banner uses this once; the hint bar ignores it.</summary>
        public static bool TipPending(HousingStep step) =>
            step != HousingStep.None && WorldFlags.Get(TipId(step)) == 0;

        public static void MarkTipSeen(HousingStep step)
        {
            if (step == HousingStep.None || !TipPending(step)) return;
            WorldFlags.Set(TipId(step), 1);
        }

        static string TipId(HousingStep step) => "tip.housing." + step;

        static readonly string[] EggLayers = { "chicken_white", "chicken_brown", "duck_white_0", "duck_white_1", "duck_mallard" };

        static bool RipeCrop(out string crop)
        {
            crop = "";
            for (int i = 0; i < HomeFarm.Count; i++)
            {
                var p = HomeFarm.Read(i);
                if (!p.Ripe) continue;
                crop = p.crop;
                return true;
            }
            HomeLand.Collect(HomeLand.Plot, _beds);
            for (int i = 0; i < _beds.Count; i++)
            {
                var p = HomeFarm.ReadBed(_beds[i]);
                if (!p.Ripe) continue;
                crop = p.crop;
                return true;
            }
            return false;
        }

        static bool EggWaiting()
        {
            for (int i = 0; i < EggLayers.Length; i++)
            {
                HomeAnimals.CatchUp(EggLayers[i]);
                if (HomeAnimals.EggCount(EggLayers[i]) > 0) return true;
            }
            return false;
        }

        static bool HasFirstBed()
        {
            for (int i = 0; i < 16; i++)
                if (WorldSave.PlotPlanted(i)) return true;
            for (int i = 0; i < HomeFarm.Count; i++)
                if (!HomeFarm.Read(i).Empty) return true;
            HomeLand.Collect(HomeLand.Plot, _beds);
            for (int i = 0; i < _beds.Count; i++)
                if (!HomeFarm.ReadBed(_beds[i]).Empty) return true;
            return false;
        }

        /// <summary>Slot interaction: select a slot, or rest at the built house. Placement is BuildMode.</summary>
        public static bool Use(string slotId)
        {
            if (!IsSlot(slotId)) return WorldServices.Fail(Deny.Invalid, "No such slot");
            if (HasHouse && Built == slotId) return Rest();
            if (BuildMode.IsOpen) return BuildMode.FocusSlot(slotId);
            if (!HasHouse) return Select(slotId);
            return WorldServices.Fail(Deny.AlreadyDone, "Open XAY DUNG");
        }

        static GameObject Hero() => GameObject.FindGameObjectWithTag("Player");

        static bool InRange(string slotId, GameObject hero)
        {
            if (!_slots.TryGetValue(slotId, out var talk) || talk == null) return false;
            return talk.isActiveAndEnabled && talk.InRange(hero.transform.position);
        }
    }
}
