using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Sandbox Cyclops slam. Calm face until the first hit, then the angry override stays on.
    /// Windup 1.2s spreads the red ring (cells 0-3). The hit is Attack frame 2: cell 4 flashes,
    /// the crack spawns, and the ring (center 1.5 tiles ahead, radius 3.9) deals damage.
    /// No transform scale. Stun and taunt rules come from R2CBossMark.
    /// </summary>
    public class CyclopsSlam : MonoBehaviour
    {
        Sprite[] _ring;
        Sprite _crackSprite;
        RuntimeAnimatorController _enraged;
        int _damage = 22;
        float _radius = 3.9f;
        bool _angry;
        SpriteRenderer _ringSr;
        SpriteRenderer _crackSr;
        float _crackLife;
        float _ringFade;
        Animator _anim;
        EnemyVisual _visual;
        Health _health;

        public bool Enraged => _angry;

        public void Configure(R2CMonsterDef def)
        {
            _ring = def.telegraph;
            _crackSprite = def.crack;
            _enraged = def.enraged;
            if (def.slamDamage > 0) _damage = def.slamDamage;
            if (def.slamRadius > 0.5f) _radius = def.slamRadius;
        }

        void Awake()
        {
            _anim = GetComponentInChildren<Animator>();
            _visual = GetComponent<EnemyVisual>();
            _health = GetComponent<Health>();
            if (_health != null) _health.Damaged += OnDamaged;
        }

        void OnDestroy()
        {
            if (_health != null) _health.Damaged -= OnDamaged;
        }

        void OnDamaged(Health _, int amount, Vector2 __)
        {
            if (_angry || amount <= 0 || _enraged == null || _anim == null) return;
            _angry = true;
            _anim.runtimeAnimatorController = _enraged;
        }

        public void Present(float elapsed, float windup)
        {
            if (_visual != null) _visual.HoldAction(0.2f);
            float t = windup > 0.01f ? Mathf.Clamp01(elapsed / windup) : 1f;
            PinAttack(t < 0.5f ? 0.06f : 0.32f);
            int cell = Mathf.Clamp(Mathf.FloorToInt(t * 4f), 0, 3);
            ShowRing(cell, 1f);
        }

        public void Strike(Vector2 toPlayer)
        {
            Vector2 dir = toPlayer.sqrMagnitude > 0.01f ? toPlayer.normalized : Vector2.down;
            Vector2 center = (Vector2)transform.position + dir * 1.5f;
            PinAttack(0.55f);
            ShowRing(4, 1f);
            _ringFade = 0.18f;
            SpawnCrack(center);
            var hits = Physics2D.OverlapCircleAll(center, _radius);
            foreach (var col in hits)
            {
                var hp = col.GetComponent<Health>() ?? col.GetComponentInParent<Health>();
                if (hp == null || hp == _health || hp.GetComponent<EnemyController>() != null) continue;
                if (!hp.TakeDamage(_damage, hp.transform.position, dir * 3.5f)) continue;
                if (hp.LastDamageApplied > 0)
                    DamagePopup.Spawn(hp.transform.position + Vector3.up * 0.5f, hp.LastDamageApplied, new Color(0.9f, 0.25f, 0.2f));
            }
        }

        void Update()
        {
            if (_ringFade > 0f && _ringSr != null)
            {
                _ringFade -= Time.deltaTime;
                int cell = _ringFade > 0.08f ? 4 : 5;
                if (_ring != null && cell < _ring.Length) _ringSr.sprite = _ring[cell];
                var c = _ringSr.color;
                c.a = Mathf.Clamp01(_ringFade / 0.18f);
                _ringSr.color = c;
                if (_ringFade <= 0f) _ringSr.enabled = false;
            }
            if (_crackLife > 0f && _crackSr != null)
            {
                _crackLife -= Time.deltaTime;
                var c = _crackSr.color;
                c.a = Mathf.Clamp01(_crackLife / 2f);
                _crackSr.color = c;
                if (_crackLife <= 0f) _crackSr.enabled = false;
            }
        }

        void PinAttack(float normalized)
        {
            if (_anim == null || _visual == null) return;
            string state = "Attack_" + _visual.View;
            if (!_anim.HasState(0, Animator.StringToHash(state))) state = "Attack_Down";
            _anim.speed = 0f;
            _anim.Play(state, 0, normalized);
        }

        void ShowRing(int cell, float alpha)
        {
            if (_ring == null || cell < 0 || cell >= _ring.Length) return;
            if (_ringSr == null)
            {
                var go = new GameObject("CyclopsRing");
                go.transform.SetParent(transform, false);
                _ringSr = go.AddComponent<SpriteRenderer>();
            }
            Vector2 dir = Facing();
            _ringSr.transform.position = (Vector2)transform.position + dir * 1.5f;
            _ringSr.sprite = _ring[cell];
            _ringSr.sortingOrder = 4000 + Mathf.RoundToInt(-_ringSr.transform.position.y * 100f) - 80;
            _ringSr.color = new Color(1f, 1f, 1f, alpha);
            _ringSr.enabled = true;
        }

        void SpawnCrack(Vector2 center)
        {
            if (_crackSprite == null) return;
            if (_crackSr == null)
            {
                var go = new GameObject("CyclopsCrack");
                go.transform.SetParent(transform, false);
                _crackSr = go.AddComponent<SpriteRenderer>();
                _crackSr.sprite = _crackSprite;
            }
            _crackSr.transform.position = center;
            _crackSr.sortingOrder = 4000 + Mathf.RoundToInt(-center.y * 100f) - 90;
            _crackSr.color = Color.white;
            _crackSr.enabled = true;
            _crackLife = 2f;
        }

        Vector2 Facing()
        {
            string view = _visual != null ? _visual.View : "Down";
            if (view == "Up") return Vector2.up;
            if (view == "Side")
            {
                var sr = _anim != null ? _anim.GetComponent<SpriteRenderer>() : null;
                return sr != null && sr.flipX ? Vector2.right : Vector2.left;
            }
            return Vector2.down;
        }
    }
}
