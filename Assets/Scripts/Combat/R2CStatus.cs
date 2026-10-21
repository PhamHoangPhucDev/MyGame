using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>Strongest slow wins. A weaker slow does not replace a stronger one that is still running.</summary>
    public class StatusSlow : MonoBehaviour
    {
        float _factor = 1f;
        float _until;

        public static float Multiplier(Component c)
        {
            if (c == null || !c.TryGetComponent<StatusSlow>(out var s) || s._until <= Time.time) return 1f;
            return s._factor;
        }

        public static void Apply(Health target, float factor, float seconds)
        {
            if (target == null || target.IsDead || seconds <= 0f) return;
            factor = Mathf.Clamp(factor, 0.05f, 1f);
            var s = target.GetComponent<StatusSlow>();
            if (s == null) s = target.gameObject.AddComponent<StatusSlow>();
            bool current = s._until > Time.time;
            if (!current || factor < s._factor - 0.001f)
            {
                s._factor = factor;
                s._until = Time.time + seconds;
            }
        }
    }

    /// <summary>Forces a normal enemy to chase the hero for a few seconds. Bosses ignore it.</summary>
    public class StatusTaunt : MonoBehaviour
    {
        float _until;
        Transform _hero;
        KitVfx _mark;

        public static Transform ForcedTarget(Component enemy)
        {
            if (enemy == null || !enemy.TryGetComponent<StatusTaunt>(out var s) || s._until <= Time.time) return null;
            return s._hero;
        }

        public static void Apply(Health target, Transform hero, float seconds, Sprite[] mark)
        {
            if (target == null || hero == null || seconds <= 0f) return;
            if (target.GetComponent<R2CBossMark>() != null || target.GetComponent<BossWarlordController>() != null) return;
            if (target.GetComponent<EnemyController>() == null) return;
            var s = target.GetComponent<StatusTaunt>();
            if (s == null) s = target.gameObject.AddComponent<StatusTaunt>();
            s._hero = hero;
            s._until = Time.time + seconds;
            if (s._mark == null && mark != null && mark.Length > 0)
                s._mark = KitVfx.PlayAttached(mark, target.transform, Vector3.up * 1.05f, 5f, false, 90, true);
        }

        void Update()
        {
            if (_until > Time.time) return;
            if (_mark != null) Destroy(_mark.gameObject);
            Destroy(this);
        }
    }

    /// <summary>Lyra Wind Step: faster move and one free dodge.</summary>
    public class StatusHaste : MonoBehaviour
    {
        float _until;
        PlayerMovement _move;
        float _factor = 1f;

        public static bool Active(Component c) =>
            c != null && c.TryGetComponent<StatusHaste>(out var s) && s._until > Time.time;

        public static void Apply(PlayerMovement move, float factor, float seconds)
        {
            if (move == null || seconds <= 0f) return;
            var s = move.GetComponent<StatusHaste>();
            if (s == null) s = move.gameObject.AddComponent<StatusHaste>();
            s._move = move;
            s._factor = factor;
            s._until = Time.time + seconds;
            move.HasteMultiplier = factor;
        }

        void Update()
        {
            if (_move == null) _move = GetComponent<PlayerMovement>();
            if (_until > Time.time)
            {
                if (_move != null) _move.HasteMultiplier = _factor;
                return;
            }
            if (_move != null) _move.HasteMultiplier = 1f;
            Destroy(this);
        }
    }

    /// <summary>Ground zone that keeps refreshing a slow on whoever is standing in it.</summary>
    public class R2CSlowZone : MonoBehaviour
    {
        float _radius = 1.5f;
        float _factor = 0.5f;
        float _until;
        LayerMask _mask;
        readonly System.Collections.Generic.List<Health> _hits = new();

        public void Configure(float radius, float factor, float seconds, LayerMask mask)
        {
            _radius = radius;
            _factor = factor;
            _until = Time.time + seconds;
            _mask = mask;
        }

        void Update()
        {
            if (Time.time > _until)
            {
                Destroy(gameObject);
                return;
            }
            CombatTargeting.OverlapEnemies(transform.position, _radius, _hits, _mask);
            foreach (var h in _hits)
                StatusSlow.Apply(h, _factor, 0.25f);
        }
    }
}
