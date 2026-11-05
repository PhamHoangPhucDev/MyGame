using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>Kit slot a skill lives in. R is the combo.</summary>
    public enum KitSlot
    {
        None = 0,
        R,
        S2,
        S3,
        S4,
        V,
        C
    }

    public enum NodeEffect
    {
        Damage,
        Duration,
        Absorption
    }

    public struct SkillNode
    {
        public string id;
        public string classId;
        public int tier;
        public string name;
        public KitSlot slot;
        public NodeEffect effect;
        public float percentPerRank;
        /// <summary>Required behaviour of the target skill. A kit without it gets no bonus (no retargeting).</summary>
        public bool comboTarget;
        public SkillBehaviour behaviour;
    }

    /// <summary>
    /// Five Classes x three tiers. Ranks 0..3, one point each. Budget per Class = max(0, Level-1) - spent.
    /// Ranks persist as gw.v1.skill.{nodeId}. Upgrade and Reset need OutOfCombat (GL-002).
    /// </summary>
    public static class SkillTree
    {
        public const int MaxRank = 3;

        public static readonly SkillNode[] Nodes =
        {
            N("aren_t1", "aren", 1, "Sword Mastery", KitSlot.R, NodeEffect.Damage, 3f),
            N("aren_t2", "aren", 2, "Shield Bash Mastery", KitSlot.S2, NodeEffect.Damage, 4f, SkillBehaviour.ShieldBash),
            N("aren_t3", "aren", 3, "Bulwark Mastery", KitSlot.V, NodeEffect.Duration, 5f, SkillBehaviour.Guard),
            N("brom_t1", "brom", 1, "Hammer Mastery", KitSlot.R, NodeEffect.Damage, 3f),
            N("brom_t2", "brom", 2, "Defensive Mastery", KitSlot.V, NodeEffect.Duration, 5f, SkillBehaviour.IronWall),
            N("brom_t3", "brom", 3, "Heavy Strike Mastery", KitSlot.C, NodeEffect.Damage, 4f, SkillBehaviour.Quake),
            N("lyra_t1", "lyra", 1, "Attack Mastery", KitSlot.R, NodeEffect.Damage, 3f),
            N("lyra_t2", "lyra", 2, "Skill Power", KitSlot.S2, NodeEffect.Damage, 4f, SkillBehaviour.ProjectileFan),
            N("lyra_t3", "lyra", 3, "Advanced Skill Mastery", KitSlot.C, NodeEffect.Damage, 4f, SkillBehaviour.PierceBeam),
            N("selene_t1", "selene", 1, "Attack Mastery", KitSlot.R, NodeEffect.Damage, 3f),
            N("selene_t2", "selene", 2, "Shield Mastery", KitSlot.V, NodeEffect.Absorption, 5f, SkillBehaviour.ManaShield),
            N("selene_t3", "selene", 3, "Advanced Skill Mastery", KitSlot.C, NodeEffect.Damage, 4f, SkillBehaviour.MeteorShower),
            N("vex_t1", "vex", 1, "Dagger Mastery", KitSlot.R, NodeEffect.Damage, 3f),
            N("vex_t2", "vex", 2, "Knife Throw Mastery", KitSlot.S3, NodeEffect.Damage, 4f, SkillBehaviour.ProjectileFan),
            N("vex_t3", "vex", 3, "Execute Mastery", KitSlot.C, NodeEffect.Damage, 4f, SkillBehaviour.Execute)
        };

        static SkillNode N(string id, string cls, int tier, string name, KitSlot slot, NodeEffect fx, float pct)
            => new SkillNode { id = id, classId = cls, tier = tier, name = name, slot = slot, effect = fx, percentPerRank = pct, comboTarget = true };

        static SkillNode N(string id, string cls, int tier, string name, KitSlot slot, NodeEffect fx, float pct, SkillBehaviour b)
            => new SkillNode { id = id, classId = cls, tier = tier, name = name, slot = slot, effect = fx, percentPerRank = pct, behaviour = b };

        public static string RankKey(string nodeId) => WorldCatalog.SavePrefix + "skill." + nodeId;

        public static int Level => WorldSave.Level;

        /// <summary>Stored rank, or -1 when the stored value is outside 0..3 (kept untouched as evidence).</summary>
        public static int RawRank(string nodeId)
        {
            int r = PlayerPrefs.GetInt(RankKey(nodeId), 0);
            return r < 0 || r > MaxRank ? -1 : r;
        }

        public static int Rank(string nodeId) => Mathf.Max(0, RawRank(nodeId));

        public static int Spent(string classId)
        {
            int n = 0;
            foreach (var node in Nodes)
                if (node.classId == classId) n += RawRank(node.id) < 0 ? MaxRank : RawRank(node.id);
            return n;
        }

        public static int Budget(string classId) => Mathf.Max(0, Mathf.Max(0, Level - 1) - Spent(classId));

        public static SkillNode Find(string classId, int tier)
        {
            foreach (var node in Nodes)
                if (node.classId == classId && node.tier == tier) return node;
            return default;
        }

        public static bool TierOpen(string classId, int tier)
        {
            int lv = Level;
            switch (tier)
            {
                case 1: return lv >= 2;
                case 2: return lv >= 5 && Rank(Find(classId, 1).id) >= 1;
                case 3: return lv >= 10 && Rank(Find(classId, 2).id) >= 1;
                default: return false;
            }
        }

        public static string ActiveClassId()
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            var classes = go != null ? go.GetComponent<ClassCombat>() : null;
            return Normalize(classes != null && classes.Active != null ? classes.Active.classId : "");
        }

        static string Normalize(string id) => string.IsNullOrEmpty(id) ? "" : id.Trim().ToLowerInvariant();

        public static bool Upgrade(string classId, int tier)
        {
            classId = Normalize(classId);
            var node = Find(classId, tier);
            if (string.IsNullOrEmpty(node.id)) return WorldServices.Fail(Deny.Invalid, "No such node");
            if (classId != ActiveClassId()) return WorldServices.Fail(Deny.Locked, "Switch to " + classId + " first");
            int raw = RawRank(node.id);
            if (raw < 0) return WorldServices.Fail(Deny.Invalid, "Stored rank invalid");
            if (raw >= MaxRank) return WorldServices.Fail(Deny.MaxRank, "Max rank");
            if (!TierOpen(classId, tier)) return WorldServices.Fail(Deny.Locked, "Tier locked");
            if (Budget(classId) < 1) return WorldServices.Fail(Deny.InsufficientResources, "No skill points");
            var gate = Gate();
            if (gate != Deny.None) return WorldServices.Fail(gate, GameplayClock.DenyText(gate));
            WorldTx.Begin().SetInt(RankKey(node.id), raw + 1).Commit();
            return WorldServices.Ok(node.name + " rank " + (raw + 1));
        }

        /// <summary>Free reset of the active Class only. Blocked while a timed buff or shield it could extend is live.</summary>
        public static bool ResetActive()
        {
            string classId = ActiveClassId();
            if (string.IsNullOrEmpty(classId)) return WorldServices.Fail(Deny.Invalid, "No class");
            var gate = Gate();
            if (gate != Deny.None) return WorldServices.Fail(gate, GameplayClock.DenyText(gate));
            var go = GameObject.FindGameObjectWithTag("Player");
            var combat = go != null ? go.GetComponent<PlayerCombat>() : null;
            var health = go != null ? go.GetComponent<Health>() : null;
            if ((combat != null && combat.IsGuarding) || (health != null && health.ShieldPoints > 0))
                return WorldServices.Fail(Deny.EffectActive, "Wait for the active effect to end");
            var tx = WorldTx.Begin();
            foreach (var node in Nodes)
                if (node.classId == classId) tx.SetInt(RankKey(node.id), 0);
            tx.Commit();
            return WorldServices.Ok("Skill tree reset for " + classId);
        }

        /// <summary>Percent bonus of the active Class node matching this kit slot, after verifying the target skill.</summary>
        public static float Percent(ClassKit kit, KitSlot slot, NodeEffect effect)
        {
            if (kit == null || slot == KitSlot.None) return 0f;
            string classId = Normalize(kit.classId);
            foreach (var node in Nodes)
            {
                if (node.classId != classId || node.slot != slot || node.effect != effect) continue;
                if (!TargetValid(kit, node)) return 0f;
                return Rank(node.id) * node.percentPerRank;
            }
            return 0f;
        }

        public static bool TargetValid(ClassKit kit, SkillNode node)
        {
            if (kit == null) return false;
            if (node.comboTarget) return node.slot == KitSlot.R && kit.combo != null;
            var def = SlotDef(kit, node.slot);
            return def != null && def.behaviour == node.behaviour;
        }

        public static SkillDef SlotDef(ClassKit kit, KitSlot slot)
        {
            if (kit == null) return null;
            switch (slot)
            {
                case KitSlot.S2: return kit.slot2;
                case KitSlot.S3: return kit.slot3;
                case KitSlot.S4: return kit.slot4;
                case KitSlot.V: return kit.slotV;
                case KitSlot.C: return kit.slotC;
                default: return null;
            }
        }

        public static KitSlot SlotOf(ClassKit kit, SkillDef def)
        {
            if (kit == null || def == null) return KitSlot.None;
            if (def == kit.slot2) return KitSlot.S2;
            if (def == kit.slot3) return KitSlot.S3;
            if (def == kit.slot4) return KitSlot.S4;
            if (def == kit.slotV) return KitSlot.V;
            if (def == kit.slotC) return KitSlot.C;
            return KitSlot.None;
        }

        static Deny Gate() => GameplayClock.GateApplies ? GameplayClock.CheckOutOfCombat() : Deny.None;
    }
}
