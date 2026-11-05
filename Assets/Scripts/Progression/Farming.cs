using System.Globalization;
using UnityEngine;

namespace GameplayPrototype
{
    public enum PlotState
    {
        Empty,
        Growing,
        Ready
    }

    /// <summary>
    /// Three plots. EMPTY -> GROWING -> READY -> EMPTY. Growth uses active gameplay time (<see cref="GameplayClock"/>),
    /// so Pause and closed-app time never grow crops. State: gw.v1.farm.{i}.crop and gw.v1.farm.{i}.ready.
    /// </summary>
    public static class Farming
    {
        public const int PlotCount = 3;

        public static string CropKey(int i) => WorldCatalog.SavePrefix + "farm." + i + ".crop";
        public static string ReadyKey(int i) => WorldCatalog.SavePrefix + "farm." + i + ".ready";

        public static string SeedFor(string cropId)
        {
            switch (cropId)
            {
                case "test_sprout": return "seed_sprout";
                case "fiber": return "seed_fiber";
                default: return "";
            }
        }

        public static CropConfig Crop(string cropId)
        {
            for (int i = 0; i < WorldContent.Crops.Length; i++)
                if (WorldContent.Crops[i].cropId == cropId) return WorldContent.Crops[i];
            return default;
        }

        public static string CropAt(int i) => PlayerPrefs.GetString(CropKey(i), "");

        public static double ReadyAt(int i)
        {
            string raw = PlayerPrefs.GetString(ReadyKey(i), "");
            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : 0d;
        }

        public static PlotState State(int i)
        {
            if (string.IsNullOrEmpty(CropAt(i))) return PlotState.Empty;
            return GameplayClock.Now >= ReadyAt(i) ? PlotState.Ready : PlotState.Growing;
        }

        public static float SecondsLeft(int i) =>
            State(i) == PlotState.Growing ? Mathf.Max(0f, (float)(ReadyAt(i) - GameplayClock.Now)) : 0f;

        public static bool Plant(int i, string cropId)
        {
            if (i < 0 || i >= PlotCount) return WorldServices.Fail(Deny.Invalid, "No such plot");
            var crop = Crop(cropId);
            string seed = SeedFor(cropId);
            if (string.IsNullOrEmpty(crop.cropId) || string.IsNullOrEmpty(seed)) return WorldServices.Fail(Deny.Invalid, "Unknown crop");
            if (State(i) != PlotState.Empty) return WorldServices.Fail(Deny.AlreadyDone, "Plot in use");
            int have = WorldSave.GetQty(seed);
            if (have <= 0) return WorldServices.Fail(Deny.InsufficientResources, "Need " + seed);
            double ready = GameplayClock.Now + crop.growSeconds;
            WorldTx.Begin()
                .SetInt(WorldSave.KeyItem(seed), have - 1)
                .SetString(CropKey(i), cropId)
                .SetString(ReadyKey(i), ready.ToString("R", CultureInfo.InvariantCulture))
                .After(PendingRewards.TryFulfill)
                .Commit();
            return WorldServices.Ok("Planted " + crop.displayName);
        }

        /// <summary>Full bag keeps the crop READY and pays nothing.</summary>
        public static bool Harvest(int i)
        {
            if (i < 0 || i >= PlotCount) return WorldServices.Fail(Deny.Invalid, "No such plot");
            var st = State(i);
            if (st == PlotState.Empty) return WorldServices.Fail(Deny.NotEligible, "Nothing planted");
            if (st == PlotState.Growing) return WorldServices.Fail(Deny.NotEligible, "Still growing");
            var crop = Crop(CropAt(i));
            if (string.IsNullOrEmpty(crop.cropId)) return WorldServices.Fail(Deny.Invalid, "Unknown crop on plot");
            int have = WorldSave.GetQty(crop.itemId);
            int cap = WorldContent.StackLimit(crop.itemId);
            if (cap > 0 && have + crop.yield > cap) return WorldServices.Fail(Deny.BagFull, "Bag full");
            string item = crop.itemId;
            int amount = crop.yield;
            WorldTx.Begin()
                .SetInt(WorldSave.KeyItem(item), have + amount)
                .Delete(CropKey(i))
                .Delete(ReadyKey(i))
                .After(() => WorldEvents.RaiseItemGained(item, amount))
                .Commit();
            return WorldServices.Ok("Harvested " + crop.displayName);
        }

        /// <summary>Plot interaction: plant a Sprout seed first, else a Fiber seed; harvest when ready.</summary>
        public static bool Use(int i)
        {
            switch (State(i))
            {
                case PlotState.Empty:
                    if (WorldSave.GetQty("seed_sprout") > 0) return Plant(i, "test_sprout");
                    if (WorldSave.GetQty("seed_fiber") > 0) return Plant(i, "fiber");
                    return WorldServices.Fail(Deny.InsufficientResources, "Need a seed (Mira sells them)");
                case PlotState.Ready:
                    return Harvest(i);
                default:
                    return WorldServices.Fail(Deny.NotEligible, "Growing " + Mathf.CeilToInt(SecondsLeft(i)) + "s");
            }
        }

        /// <summary>Old saves stored a real-time planting stamp for the free test sprout. It becomes a fresh Sprout.</summary>
        public static void MigrateLegacy()
        {
            var tx = WorldTx.Begin();
            for (int i = 0; i < PlotCount; i++)
            {
                if (PlayerPrefs.HasKey(CropKey(i))) continue;
                if (!WorldSave.PlotPlanted(i)) continue;
                var crop = Crop("test_sprout");
                double ready = GameplayClock.Now + crop.growSeconds;
                tx.SetString(CropKey(i), "test_sprout")
                  .SetString(ReadyKey(i), ready.ToString("R", CultureInfo.InvariantCulture));
            }
            if (!tx.Empty) tx.Commit();
        }
    }
}
