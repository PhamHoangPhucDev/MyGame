using System;
using UnityEngine;

namespace GameplayPrototype
{
    public class Health : MonoBehaviour
    {
        [SerializeField] int maxHp = 100;
        [SerializeField] float invulnAfterHit = 0.05f;
        [SerializeField] bool blinkWhileInvulnerable = false;
        [SerializeField] bool destroyOnDeath = true;
        [SerializeField] float destroyDelay = 0.25f;

        int _hp;
        float _invulnTimer;
        float _damageTakenMultiplier = 1f;
        SpriteRenderer[] _renderers;
        Color[] _baseColors;
        float _flashTimer;

        public int CurrentHp => _hp;
        public int MaxHp => maxHp;
        public bool IsDead => _hp <= 0;
        public float Normalized => maxHp <= 0 ? 0f : Mathf.Clamp01((float)_hp / maxHp);

        public event Action<Health, int, Vector2> Damaged;
        public event Action<Health> Died;
        public event Action<Health> Changed;

        public void Configure(int newMaxHp, bool refill = true)
        {
            maxHp = Mathf.Max(1, newMaxHp);
            if (refill || _hp > maxHp)
                _hp = maxHp;
            Changed?.Invoke(this);
        }

        /// <summary>
        /// Hero survival: longer i-frames and a blink while immune.
        /// </summary>
        public void SetSurvival(float invulnSeconds, bool blink)
        {
            invulnAfterHit = Mathf.Max(0f, invulnSeconds);
            blinkWhileInvulnerable = blink;
        }

        public void SetDamageTakenMultiplier(float multiplier)
        {
            _damageTakenMultiplier = Mathf.Clamp(multiplier, 0f, 2f);
        }

        public bool IsInvulnerable => _invulnTimer > 0f;

        void Awake()
        {
            _hp = maxHp;
            CacheRenderers();
        }

        public void CacheRenderers()
        {
            _renderers = GetComponentsInChildren<SpriteRenderer>(true);
            _baseColors = new Color[_renderers.Length];
            for (int i = 0; i < _renderers.Length; i++)
                _baseColors[i] = _renderers[i].color;
        }

        void Update()
        {
            if (_invulnTimer > 0f)
                _invulnTimer -= Time.deltaTime;

            if (blinkWhileInvulnerable && _invulnTimer > 0f)
            {
                bool dim = Mathf.Repeat(Time.time * 12f, 1f) > 0.5f;
                for (int i = 0; i < _renderers.Length; i++)
                {
                    var c = _baseColors[i];
                    c.a = dim ? 0.35f : 1f;
                    _renderers[i].color = c;
                }
                return;
            }

            if (_flashTimer > 0f)
            {
                _flashTimer -= Time.deltaTime;
                float t = Mathf.PingPong(_flashTimer * 20f, 1f);
                for (int i = 0; i < _renderers.Length; i++)
                    _renderers[i].color = Color.Lerp(_baseColors[i], Color.white, t);
                if (_flashTimer <= 0f)
                    RestoreColors();
            }
            else if (blinkWhileInvulnerable)
                RestoreColors();
        }

        void RestoreColors()
        {
            for (int i = 0; i < _renderers.Length; i++)
                _renderers[i].color = _baseColors[i];
        }

        public bool TakeDamage(int amount, Vector2 hitPoint, Vector2 knockback)
        {
            if (IsDead || amount <= 0 || _invulnTimer > 0f)
                return false;

            int applied = Mathf.Max(1, Mathf.RoundToInt(amount * _damageTakenMultiplier));
            if (_damageTakenMultiplier <= 0.001f)
                applied = 0;
            if (applied <= 0)
            {
                _flashTimer = 0.08f;
                return false;
            }

            _hp = Mathf.Max(0, _hp - applied);
            _invulnTimer = invulnAfterHit;
            _flashTimer = 0.12f;
            Changed?.Invoke(this);
            Damaged?.Invoke(this, applied, hitPoint);

            var rb = GetComponent<Rigidbody2D>();
            if (rb != null && knockback.sqrMagnitude > 0.001f)
                rb.AddForce(knockback, ForceMode2D.Impulse);

            if (_hp <= 0)
                Die();
            else
                GameAudio.Play(IsHero ? Sfx.HeroHurt : Sfx.EnemyHurt);

            return true;
        }

        bool IsHero => CompareTag("Player");

        void Die()
        {
            GameAudio.Play(IsHero ? Sfx.HeroDeath : Sfx.EnemyDeath);
            Died?.Invoke(this);
            var col = GetComponent<Collider2D>();
            if (col != null) col.enabled = false;
            var enemy = GetComponent<EnemyController>();
            if (enemy != null) enemy.enabled = false;
            var rb = GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            if (destroyOnDeath)
                Destroy(gameObject, destroyDelay);
        }
    }
}
