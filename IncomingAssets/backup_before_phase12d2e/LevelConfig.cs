using System;
using UnityEngine;

namespace GameplayPrototype
{
    public enum LevelObjectiveType
    {
        ClearWaves = 0,
        ClearWavesThenReachExit = 1
    }

    [Serializable]
    public class WaveConfig
    {
        public string displayName = "WAVE 1";
        [Min(0)] public int goblins = 3;
        [Min(0)] public int gobriders;
        [Tooltip("Seconds to wait after the wave is cleared before starting the next one.")]
        [Min(0.25f)] public float transitionDelay = 1.75f;
        [Tooltip("Max simultaneous alive enemies while this wave is active (top-up spawn).")]
        [Min(1)] public int aliveCap = 6;
        [Tooltip("If true, leftover budget may top-up until wave kill target is met.")]
        public bool allowTopUp = true;
        [Tooltip("Spawn this many Gobriders as Elite (stronger stats). Counts toward gobriders budget.")]
        [Min(0)] public int eliteGobrider;
    }

    [CreateAssetMenu(menuName = "Gameplay Prototype/Level Config", fileName = "LevelConfig")]
    public class LevelConfig : ScriptableObject
    {
        [Header("Identity")]
        public int levelNumber = 1;
        public string levelId = "level_01";
        public string levelTitle = "GOBLIN FOREST CAMP";

        [Header("Map")]
        [Tooltip("Camp | Trail | AbandonedCamp | GoblinOutpost")]
        public string mapLayoutId = "Camp";
        public Vector2 heroSpawn = new Vector2(0.2f, -6.2f);
        public Vector2[] spawnZones = Array.Empty<Vector2>();
        public Vector2 cameraBoundsMin = new Vector2(-14f, -12f);
        public Vector2 cameraBoundsMax = new Vector2(14f, 14f);

        [Header("Objectives")]
        public LevelObjectiveType objectiveType = LevelObjectiveType.ClearWaves;
        public Vector2 exitPosition = new Vector2(0.2f, 8f);
        public Vector2 exitSize = new Vector2(1.6f, 1.2f);

        [Header("Waves")]
        public WaveConfig[] waves = Array.Empty<WaveConfig>();

        [Header("Difficulty / Stars (Phase 12F scaffold)")]
        [Min(30f)] public float targetCompletionTime = 150f;
        [Range(0.1f, 1f)] public float starHpRatio = 0.4f;
        [Min(1)] public int recommendedMaxAlive = 6;

        [Header("Progression")]
        public int nextLevelNumber = 2;

        public int TotalEnemyBudget
        {
            get
            {
                int n = 0;
                if (waves == null) return 0;
                for (int i = 0; i < waves.Length; i++)
                    n += waves[i].goblins + waves[i].gobriders;
                return n;
            }
        }
    }
}
