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

        Sprite[] _hitFrames;
        Sprite[] _poses;
        SpriteRenderer _sr;
        int _pierce = 1;
        int _hits;
        int _dirIndex;
        int _poseRow;
        int _pairStart = -1;
        float _sparkleTimer;
        Sprite[] _sparkle;
        float _explode;
        int _explodeDamage;
        Sprite[] _explodeFrames;
        System.Action _onHit;
        readonly System.Collections.Generic.List<Health> _already = new();

        public static KitProjectile Fire(Vector2 from, Vector2 dir, Sprite[] poses8, float speed, float range, int damage, float knockback,
                                         int bleedDamage, float bleedTick, float bleedDuration, LayerMask mask, Color popup, Sprite[] hitFrames = null,
                                         int pierce = 1, float explodeRadius = 0f, Sprite[] explodeFrames = null, int poseRow = 0, int explodeDamage = 0,
                                         System.Action onHit = null, Sprite[] sparkle = null)
        {
            var go = new GameObject("KitProjectile");
            go.transform.position = from;
            var p = go.AddComponent<KitProjectile>();
            p._dir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
            p._speed = speed; p._range = range; p._damage = damage; p._knockback = knockback;
            p._bleedDamage = bleedDamage; p._bleedTick = bleedTick; p._bleedDuration = bleedDuration;
            p._mask = mask; p._popup = popup; p._hitFrames = hitFrames; p._onHit = onHit;
            p._pierce = Mathf.Max(1, pierce); p._explode = explodeRadius; p._explodeFrames = explodeFrames; p._explodeDamage = explodeDamage;
            p._poses = poses8; p._poseRow = poseRow; p._sparkle = sparkle;
            var sr = go.AddComponent<SpriteRenderer>();
            p._sr = sr;
            if (poses8 != null && poses8.Length >= 2 && poses8.Length < 8)
            {
                p._pairStart = Mathf.Clamp(poseRow, 0, poses8.Length - 2);
                sr.sprite = poses8[p._pairStart];
            }
            else if (poses8 != null && poses8.Length >= 8)
            {
                float a = Mathf.Atan2(p._dir.y, p._dir.x) * Mathf.Rad2Deg;
                int i = ((Mathf.RoundToInt(a / 45f) % 8) + 8) % 8;
                p._dirIndex = i;
                int index = poseRow * 8 + i;
                if (index < 0 || index >= poses8.Length) index = i;
                sr.sprite = poses8[index];
            }
            sr.sortingOrder = 4000 + Mathf.RoundToInt(-from.y * 100f) + 40;
            return p;
        }

        void Update()
        {
            float step = _speed * Time.deltaTime;
            transform.position += (Vector3)(_dir * step);
            _travelled += step;
            if (_pairStart >= 0 && _poses != null && _sr != null)
            {
                int flip = ((int)(Time.time / 0.08f) & 1);
                _sr.sprite = _poses[Mathf.Min(_pairStart + flip, _poses.Length - 1)];
            }
            if (_sparkle != null && _sparkle.Length > 0)
            {
                _sparkleTimer += Time.deltaTime;
                if (_sparkleTimer >= 0.04f)
                {
                    _sparkleTimer = 0f;
                    KitVfx.Play(_sparkle, transform.position, 1f / 0.06f, false, 20);
                }
            }
            if (_poseRow == 1 && _poses != null && _poses.Length >= 24 && _sr != null)
            {
                int row = ((int)(Time.time / 0.06f) & 1) == 0 ? 1 : 2;
                int index = row * 8 + _dirIndex;
                if (index < _poses.Length) _sr.sprite = _poses[index];
            }
            CombatTargeting.OverlapEnemies(transform.position, 0.3f, _buffer, _mask);
            if (_explodeDamage > 0 && (_buffer.Count > 0 || _travelled >= _range))
            {
                Explode();
                Destroy(gameObject);
                return;
            }
            foreach (var h in _buffer)
            {
                if (h == null || h.IsDead || _already.Contains(h)) continue;
                Vector2 hp = h.transform.position;
                if (h.TakeDamage(_damage, hp, _dir * _knockback))
                {
                    _already.Add(h);
                    _hits++;
                    if (h.LastDamageApplied > 0)
                        DamagePopup.Spawn(hp + Vector2.up * 0.25f, h.LastDamageApplied, _popup);
                    if (_hitFrames != null && _hitFrames.Length > 0 && _hitFrames[0] != null)
                        KitVfx.Play(_hitFrames, hp + Vector2.up * 0.5f, 20f, false, 80);
                    else
                        CombatVfx.SpawnImpact(hp);
                    HitComboCounter.Instance?.RegisterHit();
                    if (_bleedDamage > 0) StatusBleed.Apply(h, _bleedDamage, _bleedTick, _bleedDuration);
                    if (_hits == 1) _onHit?.Invoke();
                    if (_hits >= _pierce) { Destroy(gameObject); return; }
                }
            }
            if (_travelled >= _range)
            {
                if (_explode > 0.01f) Explode();
                Destroy(gameObject);
            }
        }

        void Explode()
        {
            if (_explode <= 0.01f) return;
            int dmg = _explodeDamage > 0 ? _explodeDamage : _damage;
            if (_explodeFrames != null && _explodeFrames.Length > 0)
                KitVfx.Play(_explodeFrames, transform.position, 14f, false, -20);
            var list = new System.Collections.Generic.List<Health>();
            CombatTargeting.OverlapEnemies(transform.position, _explode, list, _mask);
            foreach (var h in list)
            {
                if (h == null || !h.TakeDamage(dmg, h.transform.position, _dir * _knockback)) continue;
                if (h.LastDamageApplied > 0)
                    DamagePopup.Spawn(h.transform.position + Vector3.up * 0.3f, h.LastDamageApplied, _popup);
            }
        }
    }
}
