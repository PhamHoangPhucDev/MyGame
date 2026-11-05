using System.Collections.Generic;
using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>Axis-aligned Water or Lava area placed by WorldSites.</summary>
    public class HazardVolume : MonoBehaviour
    {
        public static readonly List<HazardVolume> Active = new();

        public bool lava;
        public Vector2 halfSize = new Vector2(0.8f, 0.6f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Active.Clear();

        void OnEnable() => Active.Add(this);
        void OnDisable() => Active.Remove(this);

        public bool Contains(Vector2 p)
        {
            Vector2 c = transform.position;
            return Mathf.Abs(p.x - c.x) <= halfSize.x && Mathf.Abs(p.y - c.y) <= halfSize.y;
        }
    }

    /// <summary>
    /// Hero only. Water: normal walking x0.75, no stacking, dash/charge/blink/knockback untouched (they use
    /// PlayerMovement.OverrideVelocity or teleport). Lava: 20 HP per completed continuous active second, first tick
    /// after one full second, reset on exit, frozen by Pause, through DEF/Guard; only true shields absorb.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class HeroHazards : MonoBehaviour
    {
        public const float WaterMultiplier = 0.75f;
        public const int LavaDamage = 20;

        Health _health;
        PlayerMovement _movement;
        float _lavaContact;

        public bool InWater { get; private set; }
        public bool InLava { get; private set; }
        public float LavaContact => _lavaContact;

        void Awake()
        {
            _health = GetComponent<Health>();
            _movement = GetComponent<PlayerMovement>();
        }

        void OnDisable()
        {
            InWater = InLava = false;
            _lavaContact = 0f;
            if (_movement != null) _movement.WaterMultiplier = 1f;
            GameplayClock.SetLava(false);
        }

        void Update()
        {
            Vector2 p = transform.position;
            bool water = false, lava = false;
            var list = HazardVolume.Active;
            for (int i = 0; i < list.Count; i++)
            {
                var v = list[i];
                if (v == null || !v.isActiveAndEnabled || !v.Contains(p)) continue;
                if (v.lava) lava = true;
                else water = true;
            }
            if (_health != null && _health.IsDead) lava = water = false;
            InWater = water;
            if (_movement != null) _movement.WaterMultiplier = water ? WaterMultiplier : 1f;

            InLava = lava;
            GameplayClock.SetLava(lava);
            if (!lava)
            {
                _lavaContact = 0f;
                return;
            }
            _lavaContact += GameplayClock.LastDelta;
            while (_lavaContact >= 1f && _health != null && !_health.IsDead)
            {
                _lavaContact -= 1f;
                _health.TakeEnvironmentDamage(LavaDamage);
            }
        }
    }
}
