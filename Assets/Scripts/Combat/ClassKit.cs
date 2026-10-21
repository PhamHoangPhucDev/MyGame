using System;
using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>What a skill slot does. Each value maps to one execution routine in PlayerCombat.</summary>
    public enum SkillBehaviour
    {
        None = 0,
        BurstAhead = 1,      // AoE burst in front (legacy 2 — Flame Burst)
        DashStrike = 2,      // dash + AoE on the path start (legacy 3 — Dash Slash)
        Whirlwind = 3,       // N spinning waves around the hero (legacy 4 — Whirlwind)
        Guard = 4,           // damage-taken multiplier for a duration (legacy V — Guard)
        CrescentStrike = 5,  // anticipation swing + big frontal crescent (legacy C — Crescent)
        ShadowStep = 6,      // blink behind the nearest enemy in range; next R hit x multiplier (Vex 2)
        ProjectileFan = 7,   // count projectiles in a spread, optional bleed (Vex 3)
        Stealth = 8,         // untargetable-ish: invulnerable + translucent for duration (Vex V)
        Execute = 9,         // blink to target and strike; x multiplier under HP threshold (Vex C)
        ShieldBash = 10,     // R3D.1 Aren 2 — short dash + frontal cone + stun
        Charge = 11,         // R3D.1 Aren 3 — longer dash path hits (VFX ChargeDust; no new art pack skill)
        GroundSlam = 12,
        ShieldCharge = 13,
        Taunt = 14,
        IronWall = 15,
        Quake = 16,
        LeapBack = 17,
        ArrowRain = 18,
        WindStep = 19,
        PierceBeam = 20,
        SlowOrb = 21,
        Blink = 22,
        RuneZone = 23,
        ManaShield = 24,
        MeteorShower = 25,
    }

    public enum ComboHitShape
    {
        Arc = 0,
        Circle = 1,
        Projectile = 2
    }

    /// <summary>One hit of the R combo.</summary>
    [Serializable]
    public class ComboStep
    {
        [Tooltip("Swing direction = facing + perpendicular * offset (0.85 / -0.85 / 0 for the legacy 3-hit chain).")]
        public float swingOffset;
        public float damageMul = 1f;
        public float radiusMul = 1f;
        public float knockbackMul = 1f;
        [Tooltip("Legacy CombatVfx slash sheet: 0 = A, 1 = B, 2 = finisher C.")]
        public int vfxVariant;
        public float vfxScale = 1f;
        public bool finisher;
        [Tooltip("Bleed applied to every enemy this hit lands on (0 = none).")]
        public int bleedDamage;
        public float bleedTick = 0.5f;
        public float bleedDuration;
        public float hitstop = 0.048f;
        public float shakeAmp = 0.0625f;
        public float shakeDur = 0.07f;
        public ComboHitShape shape;
        [Tooltip("0 uses the combo's impactDelay.")]
        public float impactDelay;
        public float projectileSpeed;
        public float projectileRange;
        public int pierce = 1;
        [Tooltip("Finisher bolt explodes for this radius. 0 keeps a normal hit.")]
        public float explodeRadius;
        [Tooltip("Which row of an 8-direction projectile sheet to fire.")]
        public int arrowRow;
        [Tooltip("Body attack lock. 0 keeps the shared 0.26 s lock.")]
        public float bodyLock;
    }

    [Serializable]
    public class ComboDef
    {
        public string displayName = "Combo";
        public float cooldown = 0.35f;
        public float radius = 1.35f;
        public int damage = 18;
        public float knockback = 2.8f;
        public float autoTargetRadius = 1.8f;
        [Tooltip("Press → damage. Must match the Impact key of the body attack clip.")]
        public float impactDelay = 0.12f;
        public ComboStep[] steps = new ComboStep[0];
        public Sprite[] projectilePoses = new Sprite[0];
        public Sprite[] projectileHit = new Sprite[0];
        public Sprite[] finisherVfx = new Sprite[0];
        public Sprite[] sparkle = new Sprite[0];
    }

    /// <summary>
    /// Generic skill parameters. Each behaviour reads only the fields it needs (see PlayerCombat.Perform*).
    /// </summary>
    [Serializable]
    public class SkillDef
    {
        public string displayName = "Skill";
        public SkillBehaviour behaviour;
        public float cooldown = 5f;
        public int damage;
        public float radius;
        public float range;
        public float knockback;
        public float speed;
        public float duration;
        public float interval;
        public int count;
        [Tooltip("Guard: damage-taken multiplier.")]
        public float multiplier = 1f;
        public float anticipation;
        public float hitstop = 0.05f;
        public float shakeAmp = 0.08f;
        public float shakeDur = 0.09f;
        public Color popupColor = Color.white;

        [Header("Class extras (unused by the legacy Aren behaviours)")]
        [Tooltip("ProjectileFan: total spread in degrees.")]
        public float spreadDeg;
        [Tooltip("Execute: target HP ratio below which 'multiplier' applies. ShadowStep: unused.")]
        public float threshold;
        [Tooltip("Stealth: alpha of the hero while active.")]
        public float alpha = 1f;
        public int bleedDamage;
        public float bleedTick = 0.5f;
        public float bleedDuration;
        [Tooltip("Whirlwind etc.: draw CombatVfx crescents in tintColor instead of the legacy ice-blue set.")]
        public bool useTint;
        public Color tintColor = Color.white;
        [Tooltip("Sprite animation played at the effect point (smoke, big X...).")]
        public Sprite[] vfxFrames = new Sprite[0];
        public float vfxFps = 12f;
        [Tooltip("ProjectileFan: 8 projectile poses (R, UR, U, UL, L, DL, D, DR).")]
        public Sprite[] projectilePoses = new Sprite[0];
        [Header("R3D.1 Aren kit")]
        [Tooltip("ShieldBash: stun seconds applied to Goblin/Gobrider on hit.")]
        public float stunDuration;
        [Tooltip("ShieldBash / Charge: dash travel distance in world units.")]
        public float dashDistance = 2f;
        [Tooltip("ShieldBash cone half-angle in degrees.")]
        public float coneDeg = 70f;
    }

    /// <summary>
    /// R2C class kit: base stats, the R combo and the five skill slots (2, 3, 4, V, C) of one class.
    /// PlayerCombat runs it when assigned (Sandbox via ClassCombat); without a kit it keeps its own fields (Level 1-4).
    /// </summary>
    [CreateAssetMenu(menuName = "Gameplay Prototype/Class Kit", fileName = "ClassKit")]
    public class ClassKit : ScriptableObject
    {
        public string classId = "aren";
        public string displayName = "Aren";
        [TextArea] public string role;

        [Header("Base stats (multipliers on the hero's scene values)")]
        public float maxHpMultiplier = 1f;
        public float moveSpeedMultiplier = 1f;
        [Tooltip("Always-on damage taken multiplier. Brom is 0.85. Separate from Guard.")]
        public float damageTakenMultiplier = 1f;

        [Header("Visual (optional; applied by ClassCombat)")]
        public RuntimeAnimatorController bodyController;
        public EquipmentSet equipment;

        [Header("R — combo")]
        public ComboDef combo = new ComboDef();

        [Header("Skills")]
        public SkillDef slot2 = new SkillDef();
        public SkillDef slot3 = new SkillDef();
        public SkillDef slot4 = new SkillDef();
        public SkillDef slotV = new SkillDef();
        public SkillDef slotC = new SkillDef();

        public SkillDef Get(SkillId id)
        {
            switch (id)
            {
                case SkillId.FireDash: return slot2;
                case SkillId.MultiSlash: return slot3;
                case SkillId.MawBite: return slot4;
                case SkillId.Bone: return slotV;
                case SkillId.Mace: return slotC;
                default: return null;
            }
        }

        public float Cooldown(SkillId id) => id == SkillId.Sword ? combo.cooldown : (Get(id)?.cooldown ?? 0f);

        /// <summary>
        /// Vex (assassin) per CLASS_KIT_DESIGN.md. Combo cooldown is 0.26 s (not 0.22) so every hit still plays the
        /// full body attack clip (PlayerVisual attack lock = 0.26 s). HitStop clamps to 0.04-0.06 s.
        /// </summary>
        public void FillVex(Sprite[] smoke, Sprite[] knives, Sprite[] executeX)
        {
            classId = "vex"; displayName = "Vex";
            role = "Sat thu Ma toc: song dao, don sat thuong nhanh, co dong. Mau x0.8, toc chay x1.15.";
            maxHpMultiplier = 0.8f; moveSpeedMultiplier = 1.15f;
            combo = new ComboDef
            {
                displayName = "Song Dao", cooldown = 0.26f, radius = 1.1f, damage = 10, knockback = 1.4f,
                autoTargetRadius = 1.8f, impactDelay = 0.12f,
                steps = new[]
                {
                    new ComboStep { swingOffset = 0.6f, vfxVariant = 0, hitstop = 0.04f, shakeAmp = 0f, shakeDur = 0f },
                    new ComboStep { swingOffset = -0.6f, vfxVariant = 1, hitstop = 0.04f, shakeAmp = 0f, shakeDur = 0f },
                    new ComboStep { swingOffset = 0f, vfxVariant = 1, hitstop = 0.04f, shakeAmp = 0f, shakeDur = 0f },
                    new ComboStep { swingOffset = 0f, damageMul = 2.2f, radiusMul = 1.3f, knockbackMul = 2f, vfxVariant = 2, vfxScale = 1.3f,
                                    finisher = true, bleedDamage = 3, bleedTick = 0.5f, bleedDuration = 3f, hitstop = 0.05f, shakeAmp = 0.07f, shakeDur = 0.07f },
                },
            };
            Color crimson = new Color(0.86f, 0.16f, 0.22f), blood = new Color(1f, 0.3f, 0.35f);
            slot2 = new SkillDef { displayName = "Buoc Bong", behaviour = SkillBehaviour.ShadowStep, cooldown = 6f, range = 5f, radius = 0.7f, multiplier = 2f, vfxFrames = smoke, vfxFps = 14f, popupColor = blood };
            slot3 = new SkillDef { displayName = "Phi Dao", behaviour = SkillBehaviour.ProjectileFan, cooldown = 5f, count = 3, spreadDeg = 30f, speed = 11f, range = 6f, damage = 14, knockback = 1.2f,
                                   bleedDamage = 2, bleedTick = 0.5f, bleedDuration = 2f, projectilePoses = knives, hitstop = 0.04f, shakeAmp = 0f, shakeDur = 0f, popupColor = blood };
            slot4 = new SkillDef { displayName = "Vu Dieu Luoi Dao", behaviour = SkillBehaviour.Whirlwind, cooldown = 8f, count = 6, interval = 0.1f, radius = 1.6f, damage = 9, knockback = 1.5f,
                                   useTint = true, tintColor = crimson, hitstop = 0.04f, shakeAmp = 0.04f, shakeDur = 0.05f, popupColor = blood };
            slotV = new SkillDef { displayName = "Bom Khoi", behaviour = SkillBehaviour.Stealth, cooldown = 12f, duration = 2f, alpha = 0.35f, vfxFrames = smoke, vfxFps = 10f, popupColor = Color.white };
            slotC = new SkillDef { displayName = "Doat Menh", behaviour = SkillBehaviour.Execute, cooldown = 12f, range = 4f, damage = 35, threshold = 0.3f, multiplier = 3f, knockback = 3f,
                                   vfxFrames = executeX, vfxFps = 10f, hitstop = 0.06f, shakeAmp = 0.14f, shakeDur = 0.12f, popupColor = crimson };
        }

        /// <summary>Exact copy of PlayerCombat's serialized defaults + hard-coded constants (the current Aren behaviour).</summary>
        public void FillLegacyAren()
        {
            classId = "aren"; displayName = "Aren";
            role = "Hiep si can bang, kiem + khien. Bo nay trung 100% hanh vi PlayerCombat hien tai.";
            maxHpMultiplier = 1f; moveSpeedMultiplier = 1f;
            combo = new ComboDef
            {
                displayName = "Kiem Hiep Si", cooldown = 0.35f, radius = 1.35f, damage = 18, knockback = 2.8f,
                autoTargetRadius = 1.8f, impactDelay = 0.12f,
                steps = new[]
                {
                    new ComboStep { swingOffset = 0.85f, vfxVariant = 0, hitstop = 0.048f, shakeAmp = 0.0625f, shakeDur = 0.07f },
                    new ComboStep { swingOffset = -0.85f, vfxVariant = 1, hitstop = 0.048f, shakeAmp = 0.0625f, shakeDur = 0.07f },
                    new ComboStep { swingOffset = 0f, radiusMul = 1.35f, knockbackMul = 1.25f, vfxVariant = 2, vfxScale = 1.45f, finisher = true,
                                    hitstop = 0.058f, shakeAmp = 0.09f, shakeDur = 0.1f },
                },
            };
            Color flame = new Color(1f, 0.55f, 0.25f), cyan = new Color(0.55f, 0.9f, 1f), ice = new Color(0.8f, 0.95f, 1f), pale = new Color(0.7f, 0.95f, 1f);
            slot2 = new SkillDef { displayName = "Flame Burst", behaviour = SkillBehaviour.BurstAhead, cooldown = 5f, damage = 30, radius = 1.7f, range = 1.1f, knockback = 2.8f, hitstop = 0.05f, shakeAmp = 0.08f, shakeDur = 0.09f, popupColor = flame };
            slot3 = new SkillDef { displayName = "Dash Slash", behaviour = SkillBehaviour.DashStrike, cooldown = 4f, damage = 26, radius = 1.5f, speed = 13f, duration = 0.15f, knockback = 4.2f, hitstop = 0.05f, shakeAmp = 0.08f, shakeDur = 0.09f, popupColor = cyan };
            slot4 = new SkillDef { displayName = "Whirlwind", behaviour = SkillBehaviour.Whirlwind, cooldown = 7f, damage = 20, radius = 2.2f, count = 3, interval = 0.11f, knockback = 3.0f, hitstop = 0.05f, shakeAmp = 0.08f, shakeDur = 0.09f, popupColor = ice };
            slotV = new SkillDef { displayName = "Guard", behaviour = SkillBehaviour.Guard, cooldown = 8f, duration = 1.4f, multiplier = 0.35f, hitstop = 0.05f, shakeAmp = 0.08f, shakeDur = 0.09f, popupColor = Color.white };
            slotC = new SkillDef { displayName = "Crescent Strike", behaviour = SkillBehaviour.CrescentStrike, cooldown = 10f, damage = 42, radius = 2.1f, knockback = 5.2f, anticipation = 0.12f, hitstop = 0.06f, shakeAmp = 0.12f, shakeDur = 0.12f, popupColor = pale };
        }

        /// <summary>
        /// R3D.1 designed Aren kit: same damage/CD as legacy where possible; skill 2 = Shield Bash + stun;
        /// skill 3 keeps DashStrike numbers with ChargeDust VFX; 4/V/C get layered Aren sheets.
        /// </summary>
        public void FillDesignedAren()
        {
            FillLegacyAren();
            role = "Aren kit VFX R3D.1 — Shield Bash stun + layered Whirl/Bulwark/Dawn/Thrust.";
            Color gold = new Color(1f, 0.9f, 0.45f), ice = new Color(0.8f, 0.95f, 1f), pale = new Color(0.7f, 0.95f, 1f);
            // Keep cooldown 5s / damage near legacy 30 — stun is the new readable beat.
            slot2 = new SkillDef
            {
                displayName = "Shield Bash", behaviour = SkillBehaviour.ShieldBash,
                cooldown = 5f, damage = 30, radius = 1.4f, range = 0.5f, knockback = 4f,
                speed = 11f, duration = 0.18f, dashDistance = 2f, coneDeg = 70f, stunDuration = 0.32f,
                hitstop = 0.05f, shakeAmp = 0.08f, shakeDur = 0.09f, popupColor = gold
            };
            slot3 = new SkillDef
            {
                displayName = "Charge", behaviour = SkillBehaviour.Charge,
                cooldown = 4f, damage = 26, radius = 0.8f, speed = 14f, duration = 0.3f, dashDistance = 4f,
                knockback = 3f, hitstop = 0.05f, shakeAmp = 0.06f, shakeDur = 0.08f, popupColor = ice
            };
            slot4.displayName = "Whirl Ring";
            slotV.displayName = "Bulwark";
            slotC.displayName = "Dawn";
            slotC.popupColor = pale;
        }

        /// <summary>Brom per BROM_KIT_SPEC. Damage reduction is the class stat, not Guard.</summary>
        public void FillBrom(Sprite[] slam, Sprite[] dust, Sprite[] roar, Sprite[] tauntMark, Sprite[] wall, Sprite[] quake)
        {
            classId = "brom"; displayName = "Brom";
            role = "Do don. Mau x1.5, toc chay x0.85, giam 15% sat thuong.";
            maxHpMultiplier = 1.5f; moveSpeedMultiplier = 0.85f; damageTakenMultiplier = 0.85f;
            Color iron = new Color(0.95f, 0.63f, 0.31f);
            combo = new ComboDef
            {
                displayName = "Bua Ren", cooldown = 0.5f, radius = 1.5f, damage = 22, knockback = 3f,
                autoTargetRadius = 1.8f, impactDelay = 0.16f,
                steps = new[]
                {
                    new ComboStep { swingOffset = 0.85f, hitstop = 0.07f, shakeAmp = 0.10f, shakeDur = 0.10f, bodyLock = 0.42f },
                    new ComboStep { swingOffset = -0.85f, hitstop = 0.07f, shakeAmp = 0.10f, shakeDur = 0.10f, bodyLock = 0.42f },
                    new ComboStep { shape = ComboHitShape.Circle, damageMul = 30f / 22f, radiusMul = 1.6f / 1.5f, knockbackMul = 4f / 3f,
                                    hitstop = 0.08f, shakeAmp = 0.12f, shakeDur = 0.12f, bodyLock = 0.42f, finisher = true },
                },
            };
            slot2 = new SkillDef { displayName = "Nen Dat", behaviour = SkillBehaviour.GroundSlam, cooldown = 6f, damage = 28, radius = 2.2f, knockback = 3f, anticipation = 0.15f, stunDuration = 0.8f, vfxFrames = slam, vfxFps = 1f / 0.07f, hitstop = 0.08f, shakeAmp = 0.14f, shakeDur = 0.14f, popupColor = iron };
            slot3 = new SkillDef { displayName = "Huc Khien", behaviour = SkillBehaviour.ShieldCharge, cooldown = 6f, damage = 20, radius = 0.9f, dashDistance = 2.5f, duration = 0.28f, knockback = 4f, vfxFrames = dust, vfxFps = 1f / 0.08f, hitstop = 0.06f, shakeAmp = 0.08f, shakeDur = 0.08f, popupColor = iron };
            slot4 = new SkillDef { displayName = "Gam Thach", behaviour = SkillBehaviour.Taunt, cooldown = 10f, radius = 4f, duration = 3f, multiplier = 0.8f, vfxFrames = roar, projectilePoses = tauntMark, vfxFps = 1f / 0.08f, hitstop = 0f, shakeAmp = 0.06f, shakeDur = 0.06f, popupColor = iron };
            slotV = new SkillDef { displayName = "Tuong Sat", behaviour = SkillBehaviour.IronWall, cooldown = 10f, duration = 2f, multiplier = 0.2f, vfxFrames = wall, vfxFps = 1f / 0.06f, popupColor = iron };
            slotC = new SkillDef { displayName = "Dong Dat", behaviour = SkillBehaviour.Quake, cooldown = 14f, damage = 25, count = 3, interval = 0.35f, radius = 1.5f, knockback = 3f, vfxFrames = quake, vfxFps = 8f, hitstop = 0.07f, shakeAmp = 0.12f, shakeDur = 0.12f, popupColor = iron };
        }

        /// <summary>Lyra per LYRA_KIT_SPEC. Shots are projectiles; the finisher pierces three enemies.</summary>
        public void FillLyra(Sprite[] arrows, Sprite[] arrowHit, Sprite[] fan, Sprite[] leaf, Sprite[] vine,
            Sprite[] rainZone, Sprite[] rainArrow, Sprite[] aura, Sprite[] puff, Sprite[] beam)
        {
            classId = "lyra"; displayName = "Lyra";
            role = "Xa. Mau x0.85, toc chay x1.1.";
            maxHpMultiplier = 0.85f; moveSpeedMultiplier = 1.1f; damageTakenMultiplier = 1f;
            Color leafGreen = new Color(0.55f, 0.9f, 0.35f);
            combo = new ComboDef
            {
                displayName = "Mui Ten Rung", cooldown = 0.38f, radius = 0.3f, damage = 14, knockback = 1.2f,
                autoTargetRadius = 6f, impactDelay = 0.14f,
                projectilePoses = arrows, projectileHit = arrowHit,
                steps = new[]
                {
                    new ComboStep { shape = ComboHitShape.Projectile, impactDelay = 0.14f, projectileSpeed = 14f, projectileRange = 6f, pierce = 1, arrowRow = 0, hitstop = 0.035f, shakeAmp = 0f, shakeDur = 0f, bodyLock = 0.26f },
                    new ComboStep { shape = ComboHitShape.Projectile, impactDelay = 0.14f, projectileSpeed = 14f, projectileRange = 6f, pierce = 1, arrowRow = 0, hitstop = 0.035f, shakeAmp = 0f, shakeDur = 0f, bodyLock = 0.26f },
                    new ComboStep { shape = ComboHitShape.Projectile, damageMul = 24f / 14f, impactDelay = 0.28f, projectileSpeed = 18f, projectileRange = 7f, pierce = 3, arrowRow = 1, hitstop = 0.045f, shakeAmp = 0f, shakeDur = 0f, bodyLock = 0.42f, finisher = true },
                },
            };
            slot2 = new SkillDef { displayName = "Ten Quat", behaviour = SkillBehaviour.ProjectileFan, cooldown = 5f, count = 5, spreadDeg = 60f, damage = 16, speed = 14f, range = 6f, knockback = 1.2f, projectilePoses = arrows, vfxFrames = fan, vfxFps = 1f / 0.06f, hitstop = 0.035f, shakeAmp = 0f, popupColor = leafGreen };
            slot3 = new SkillDef { displayName = "Nhay Lui", behaviour = SkillBehaviour.LeapBack, cooldown = 6f, radius = 1.5f, duration = 2f, dashDistance = 3f, vfxFrames = leaf, projectilePoses = vine, vfxFps = 1f / 0.06f, popupColor = leafGreen };
            slot4 = new SkillDef { displayName = "Mua Ten", behaviour = SkillBehaviour.ArrowRain, cooldown = 8f, radius = 2f, count = 6, interval = 0.2f, damage = 10, vfxFrames = rainZone, projectilePoses = rainArrow, vfxFps = 1f / 0.15f, popupColor = leafGreen };
            slotV = new SkillDef { displayName = "Buoc Gio", behaviour = SkillBehaviour.WindStep, cooldown = 9f, duration = 3f, vfxFrames = aura, projectilePoses = puff, vfxFps = 1f / 0.08f, popupColor = leafGreen };
            slotC = new SkillDef { displayName = "Ten Xuyen Rung", behaviour = SkillBehaviour.PierceBeam, cooldown = 11f, damage = 45, knockback = 2f, range = 9f, radius = 0.5f, vfxFrames = beam, hitstop = 0.05f, shakeAmp = 0.10f, shakeDur = 0.10f, popupColor = leafGreen };
        }

        /// <summary>Selene per SELENE_KIT_SPEC. The third shot explodes even when it hits nothing.</summary>
        public void FillSelene(Sprite[] orb, Sprite[] sparkle, Sprite[] orbHit, Sprite[] starBurst, Sprite[] nova,
            Sprite[] blink, Sprite[] rune, Sprite[] bubble, Sprite[] meteor, Sprite[] meteorZone)
        {
            classId = "selene"; displayName = "Selene";
            role = "Phep. Mau x0.8, toc chay x1.";
            maxHpMultiplier = 0.8f; moveSpeedMultiplier = 1f; damageTakenMultiplier = 1f;
            Color violet = new Color(0.78f, 0.45f, 0.95f);
            combo = new ComboDef
            {
                displayName = "Tia Phep", cooldown = 0.4f, radius = 0.3f, damage = 15, knockback = 0.8f,
                autoTargetRadius = 5.5f, impactDelay = 0.2f,
                projectilePoses = orb, projectileHit = orbHit, finisherVfx = starBurst, sparkle = sparkle,
                steps = new[]
                {
                    new ComboStep { shape = ComboHitShape.Projectile, impactDelay = 0.2f, projectileSpeed = 10f, projectileRange = 5.5f, pierce = 1, arrowRow = 0, hitstop = 0.04f, shakeAmp = 0f, shakeDur = 0f, bodyLock = 0.32f },
                    new ComboStep { shape = ComboHitShape.Projectile, impactDelay = 0.2f, projectileSpeed = 10f, projectileRange = 5.5f, pierce = 1, arrowRow = 0, hitstop = 0.04f, shakeAmp = 0f, shakeDur = 0f, bodyLock = 0.32f },
                    new ComboStep { shape = ComboHitShape.Projectile, damageMul = 26f / 15f, explodeRadius = 1.2f, impactDelay = 0.28f, projectileSpeed = 10f, projectileRange = 5.5f, pierce = 1, arrowRow = 0, hitstop = 0.05f, shakeAmp = 0.05f, shakeDur = 0.05f, bodyLock = 0.42f, finisher = true },
                },
            };
            slot2 = new SkillDef { displayName = "Cau Tinh Tu", behaviour = SkillBehaviour.SlowOrb, cooldown = 5f, damage = 32, speed = 5f, range = 5.5f, radius = 1.8f, knockback = 2f, projectilePoses = orb, vfxFrames = nova, hitstop = 0.06f, shakeAmp = 0.08f, shakeDur = 0.08f, popupColor = violet };
            slot3 = new SkillDef { displayName = "Dich Chuyen", behaviour = SkillBehaviour.Blink, cooldown = 5f, dashDistance = 3.5f, vfxFrames = blink, popupColor = violet };
            slot4 = new SkillDef { displayName = "Vong Phu Chu", behaviour = SkillBehaviour.RuneZone, cooldown = 9f, radius = 2.2f, range = 5.5f, duration = 3f, damage = 8, count = 6, interval = 0.5f, vfxFrames = rune, popupColor = violet };
            slotV = new SkillDef { displayName = "Khien Ma Luc", behaviour = SkillBehaviour.ManaShield, cooldown = 10f, damage = 40, duration = 4f, vfxFrames = bubble, popupColor = violet };
            slotC = new SkillDef { displayName = "Mua Sao Bang", behaviour = SkillBehaviour.MeteorShower, cooldown = 13f, damage = 28, radius = 3f, count = 5, interval = 0.3f, knockback = 1.5f, vfxFrames = meteorZone, projectilePoses = meteor, hitstop = 0.04f, shakeAmp = 0.06f, shakeDur = 0.06f, popupColor = violet };
        }
    }
}
