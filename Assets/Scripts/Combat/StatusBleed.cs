using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>R2C: damage over time on an enemy (Vex bleed). Re-applying refreshes the duration.</summary>
    public class StatusBleed : MonoBehaviour
    {
        Health _health;
        int _damage;
        float _tick;
        float _remaining;
        float _next;
        Color _color = new Color(1f, 0.25f, 0.3f);

        public static void Apply(Health target, int damagePerTick, float tick, float duration)
        {
            if (target == null || target.IsDead || damagePerTick <= 0 || duration <= 0f) return;
            var b = target.GetComponent<StatusBleed>();
            if (b == null) b = target.gameObject.AddComponent<StatusBleed>();
            b._health = target;
            b._damage = damagePerTick;
            b._tick = Mathf.Max(0.1f, tick);
            b._remaining = duration;
            if (b._next <= 0f) b._next = b._tick;
            b.enabled = true;
        }

        void Update()
        {
            if (_health == null || _health.IsDead) { Destroy(this); return; }
            _remaining -= Time.deltaTime;
            _next -= Time.deltaTime;
            if (_next <= 0f)
            {
                _next = _tick;
                Vector2 p = _health.transform.position;
                if (_health.TakeDamage(_damage, p, Vector2.zero))
                    DamagePopup.Spawn(p + Vector2.up * 0.35f, _damage, _color);
            }
            if (_remaining <= 0f) Destroy(this);
        }
    }
}
