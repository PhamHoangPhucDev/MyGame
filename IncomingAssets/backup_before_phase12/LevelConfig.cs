using System;
using UnityEngine;

namespace GameplayPrototype
{
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
    }

    [CreateAssetMenu(menuName = "Gameplay Prototype/Level Config", fileName = "LevelConfig")]
    public class LevelConfig : ScriptableObject
    {
        public int levelNumber = 1;
        public string levelTitle = "GOBLIN FOREST CAMP";
        public Vector2 heroSpawn = new Vector2(0.2f, -6.2f);
        public WaveConfig[] waves = Array.Empty<WaveConfig>();

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
