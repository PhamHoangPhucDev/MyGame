using System.Collections.Generic;
using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>R2C: straight projectile (Vex Phi Dao). Hits the first enemy, optional bleed, sprite picked from 8 poses.</summary>
    public class KitProjectile : MonoBehaviour
    {
        Vector2 _dir;
        float _speed;
        float _range;
        float _travelled;
        int _damage;
        float _knockback;
        int _bleedDamage;
        float _bleedTick;
        float _bleedDuration;
        LayerMask _mask;
        Color _popup;
        readonly List<Health> _buffer = new();

        public static KitProjectile Fire(Vector2 from, Vector2 dir, Sprite[] poses8, float speed, float range, int damage, float knockback,
                                         int bleedDamage, float bleedTick, float bleedDuration, LayerMask mask, Color popup)
        {
            var go = new GameObject("KitProjectile");
            go.transform.position = from;
            var p = go.AddComponent<KitProjectile>();
            p._dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
            p._speed = speed; p._range = range; p._damage = damage; p._knockback = knockback;
            p._bleedDamage = bleedDamage; p._bleedTick = bleedTick; p._bleedDuration = bleedDuration;
            p._mask = mask; p._popup = popup;
            var sr = go.AddComponent<SpriteRenderer>();
            if (poses8 != null && poses8.Length >= 8)
            {
                // 8-way index: 0 R, 1 UR, 2 U, 3 UL, 4 L, 5 DL, 6 D, 7 DR (screen up = +y)
                float a = Mathf.Atan2(p._dir.y, p._dir.x) * Mathf.Rad2Deg;
                int i = ((Mathf.RoundToInt(a / 45f) % 8) + 8) % 8;
                sr.sprite = poses8[i];
            }
            sr.sortingOrder = 4000 + Mathf.RoundToInt(-from.y * 100f) + 40;
            return p;
        }

        void Update()
        {
            float step = _speed * Time.deltaTime;
            transform.position += (Vector3)(_dir * step);
            _travelled += step;
            CombatTargeting.OverlapEnemies(transform.position, 0.3f, _buffer, _mask);
            foreach (var h in _buffer)
            {
                if (h == null || h.IsDead) continue;
                Vector2 hp = h.transform.position;
                if (h.TakeDamage(_damage, hp, _dir * _knockback))
                {
                    DamagePopup.Spawn(hp + Vector2.up * 0.25f, _damage, _popup);
                    CombatVfx.SpawnImpact(hp);
                    HitComboCounter.Instance?.RegisterHit();
                    if (_bleedDamage > 0) StatusBleed.Apply(h, _bleedDamage, _bleedTick, _bleedDuration);
                }
                Destroy(gameObject);
                return;
            }
            if (_travelled >= _range) Destroy(gameObject);
        }
    }
}
