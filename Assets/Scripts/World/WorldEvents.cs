using System;

namespace GameplayPrototype
{
    public static class WorldEvents
    {
        public static event Action<string, int> ItemGained;
        public static event Action<string> EnemyKilled;
        public static event Action<int> DungeonCleared;
        public static event Action<string> BossDefeated;
        public static event Action<int, bool> ExpGained;
        public static event Action<int> CoinsChanged;

        public static void RaiseItemGained(string itemId, int amount)
        {
            if (amount > 0 && !string.IsNullOrEmpty(itemId))
                ItemGained?.Invoke(itemId, amount);
        }

        public static void RaiseEnemyKilled(string groupId)
        {
            if (!string.IsNullOrEmpty(groupId))
                EnemyKilled?.Invoke(groupId);
        }

        public static void RaiseDungeonCleared(int level)
        {
            if (level > 0) DungeonCleared?.Invoke(level);
        }

        public static void RaiseBossDefeated(string bossId)
        {
            if (!string.IsNullOrEmpty(bossId))
                BossDefeated?.Invoke(bossId);
        }

        public static void RaiseExp(int amount, bool leveled)
        {
            if (amount > 0) ExpGained?.Invoke(amount, leveled);
        }

        public static void RaiseCoins(int delta)
        {
            if (delta != 0) CoinsChanged?.Invoke(delta);
        }
    }
}
