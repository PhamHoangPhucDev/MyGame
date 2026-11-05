using System;
using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// World skeleton save. Keys stay under gw.v1 and never write GameProgress.
    /// </summary>
    public static class WorldSave
    {
        static WorldSave() => CompanySaveMigrate.Run();

        const string ExpKey = WorldCatalog.SavePrefix + "exp";
        const string InteractKey = WorldCatalog.SavePrefix + "interact";

        public static int Exp => PlayerPrefs.GetInt(ExpKey, 0);

        public static int Level => 1 + Exp / WorldCatalog.ExpPerLevel;

        public static void AddExp(int amount)
        {
            if (amount <= 0) return;
            int before = Level;
            PlayerPrefs.SetInt(ExpKey, Exp + amount);
            PlayerPrefs.Save();
            WorldEvents.RaiseExp(amount, Level > before);
        }

        public static int GetQty(string itemId)
        {
            if (string.IsNullOrEmpty(itemId)) return 0;
            return PlayerPrefs.GetInt(ItemKey(itemId), 0);
        }

        public static void AddItem(string itemId, int amount)
        {
            if (string.IsNullOrEmpty(itemId) || amount == 0) return;
            int have = GetQty(itemId);
            int next = Mathf.Max(0, have + amount);
            int cap = WorldContent.StackLimit(itemId);
            if (cap > 0) next = Mathf.Min(cap, next);
            int gained = next - have;
            if (gained == 0) return;
            PlayerPrefs.SetInt(ItemKey(itemId), next);
            PlayerPrefs.Save();
            if (gained > 0) WorldEvents.RaiseItemGained(itemId, gained);
        }

        public static bool RemoveItem(string itemId, int amount)
        {
            if (string.IsNullOrEmpty(itemId) || amount <= 0) return false;
            int have = GetQty(itemId);
            if (have < amount) return false;
            PlayerPrefs.SetInt(ItemKey(itemId), have - amount);
            PlayerPrefs.Save();
            return true;
        }

        public static int Coins => PlayerPrefs.GetInt(CoinKey, 0);

        public static void AddCoins(int amount)
        {
            if (amount == 0) return;
            PlayerPrefs.SetInt(CoinKey, Mathf.Max(0, Coins + amount));
            PlayerPrefs.Save();
            WorldEvents.RaiseCoins(amount);
        }

        public static bool SpendCoins(int amount)
        {
            if (amount < 0 || Coins < amount) return false;
            AddCoins(-amount);
            return true;
        }

        public static int StoreQty(string itemId) => PlayerPrefs.GetInt(StoreKey(itemId), 0);

        public static void AddStore(string itemId, int amount)
        {
            if (string.IsNullOrEmpty(itemId) || amount == 0) return;
            PlayerPrefs.SetInt(StoreKey(itemId), Mathf.Max(0, StoreQty(itemId) + amount));
            PlayerPrefs.Save();
        }

        public static string Equipped(string slot) => PlayerPrefs.GetString(EquipKey(slot), "");

        public static void Equip(string slot, string itemId)
        {
            if (string.IsNullOrEmpty(slot)) return;
            PlayerPrefs.SetString(EquipKey(slot), itemId ?? "");
            PlayerPrefs.Save();
        }

        public static int QuestState(string id) => PlayerPrefs.GetInt(QuestKey(id), 0);

        public static void SetQuestState(string id, int state)
        {
            PlayerPrefs.SetInt(QuestKey(id), state);
            PlayerPrefs.Save();
        }

        public static int QuestProgress(string id) => PlayerPrefs.GetInt(QuestProgKey(id), 0);

        public static void SetQuestProgress(string id, int value)
        {
            PlayerPrefs.SetInt(QuestProgKey(id), Mathf.Max(0, value));
            PlayerPrefs.Save();
        }

        public static int ExpToNext
        {
            get
            {
                int step = Mathf.Max(1, WorldCatalog.ExpPerLevel);
                return step - (Exp % step);
            }
        }

        public static int NodeUses(string nodeId, int fullUses)
        {
            if (!PlayerPrefs.HasKey(UsesKey(nodeId))) return fullUses;
            return PlayerPrefs.GetInt(UsesKey(nodeId), fullUses);
        }

        public static long NodeReadyUnix(string nodeId)
        {
            string raw = PlayerPrefs.GetString(ReadyKey(nodeId), "0");
            return long.TryParse(raw, out long unix) ? unix : 0L;
        }

        public static void SetNode(string nodeId, int uses, long readyUnix)
        {
            PlayerPrefs.SetInt(UsesKey(nodeId), uses);
            PlayerPrefs.SetString(ReadyKey(nodeId), readyUnix.ToString());
            PlayerPrefs.Save();
        }

        public static bool PlotPlanted(int index) => PlotUnix(index) > 0L;

        public static long PlotUnix(int index)
        {
            string raw = PlayerPrefs.GetString(PlotKey(index), "0");
            return long.TryParse(raw, out long unix) ? unix : 0L;
        }

        public static void SetPlot(int index, long plantedUnix)
        {
            PlayerPrefs.SetString(PlotKey(index), plantedUnix.ToString());
            PlayerPrefs.Save();
        }

        public static string InteractBinding => PlayerPrefs.GetString(InteractKey, "e");

        public static long UnixNow() => DateTimeOffset.UtcNow.ToUnixTimeSeconds();

        public static string KeyItem(string itemId) => ItemKey(itemId);
        public static string KeyStore(string itemId) => StoreKey(itemId);
        public static string KeyEquip(string slot) => EquipKey(slot);
        public static string KeyQuest(string id) => QuestKey(id);
        public static string KeyQuestProgress(string id) => QuestProgKey(id);
        public static string KeyCoins => CoinKey;
        public static string KeyExp => ExpKey;
        public static string KeyFlag(string id) => WorldCatalog.SavePrefix + "flag." + id;
        public static string LegacyPlotKey(int index) => PlotKey(index);

        static string ItemKey(string itemId) => WorldCatalog.SavePrefix + "item." + itemId;
        static string UsesKey(string nodeId) => WorldCatalog.SavePrefix + "uses." + nodeId;
        static string ReadyKey(string nodeId) => WorldCatalog.SavePrefix + "ready." + nodeId;
        static string PlotKey(int index) => WorldCatalog.SavePrefix + "plot." + index;
        static string CoinKey => WorldCatalog.SavePrefix + "coins";
        static string StoreKey(string itemId) => WorldCatalog.SavePrefix + "store." + itemId;
        static string EquipKey(string slot) => WorldCatalog.SavePrefix + "equip." + slot;
        static string QuestKey(string id) => WorldCatalog.SavePrefix + "quest." + id;
        static string QuestProgKey(string id) => WorldCatalog.SavePrefix + "questp." + id;
    }
}
