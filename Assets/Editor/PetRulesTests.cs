using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>Pure pet rules. Run from the Editor with PetRulesTests.Run().</summary>
    public static class PetRulesTests
    {
        public static string Run()
        {
            string[] oldRow = { "p1", "fox", "1", "player" };
            Pets.ReadProgress(oldRow, out int rarity, out int level, out int xp);
            Check(rarity == 0 && level == 1 && xp == 0, "old save");

            string[] neu = { "p2", "wolf", "0", "player", "2", "3", "15" };
            Pets.ReadProgress(neu, out rarity, out level, out xp);
            Check(rarity == 2 && level == 3 && xp == 15, "new save");

            Check(Pets.XpNeed(1) == 30 && Pets.XpNeed(2) == 40, "need");
            int lv = 1, cur = 0;
            bool up = Pets.ApplyXp(ref lv, ref cur, 30);
            Check(up && lv == 2 && cur == 0, "level");
            lv = 19;
            cur = 0;
            Pets.ApplyXp(ref lv, ref cur, 1000);
            Check(lv == 20 && cur == 0, "cap");

            Check(Pets.Attack("rabbit", 0, 1) == 2, "rabbit");
            Check(Pets.Attack("dragon", 2, 1) == 10, "dragon epic");
            Check(Pets.RarityFromRoll(0) == 0 && Pets.RarityFromRoll(59) == 0, "common");
            Check(Pets.RarityFromRoll(60) == 1 && Pets.RarityFromRoll(89) == 1, "rare");
            Check(Pets.RarityFromRoll(90) == 2 && Pets.RarityFromRoll(99) == 2, "epic");

            Check(Pets.SacrificeBlock(true, false, 0, false, false) == "No pet", "missing");
            Check(Pets.SacrificeBlock(false, true, 0, false, false) == "Recall first", "active");
            Check(Pets.SacrificeBlock(false, false, 1, false, false) == "Only Common", "rare");
            Check(Pets.SacrificeBlock(false, false, 0, false, true) == "In combat", "combat");
            Check(Pets.SacrificeBlock(false, false, 0, false, false).Length == 0, "ok");

            string why = Pets.CombinePlan(9, 0, out int shards, out int eggs);
            Check(why == "Need 10 shards" && shards == 9, "short");
            why = Pets.CombinePlan(10, 99, out shards, out eggs);
            Check(why == "Egg bag full", "full");
            why = Pets.CombinePlan(12, 3, out shards, out eggs);
            Check(why.Length == 0 && shards == 2 && eggs == 4, "combine");
            return "PASS";
        }

        static void Check(bool ok, string name)
        {
            if (!ok) throw new System.Exception("PetRulesTests " + name);
        }
    }
}
