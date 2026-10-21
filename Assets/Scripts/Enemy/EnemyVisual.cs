using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Flip and walk frames. Uses an Animator when one is assigned; otherwise the original two-frame swap.
    /// </summary>
    public class EnemyVisual : MonoBehaviour
    {
        Sprite _idle;
        Sprite _walk;
        SpriteRenderer _sr;
        Rigidbody2D _rb;
        Health _health;
        Animator _anim;
        float _timer;
        bool _step;
        bool _facesLeft;
        float _actionTimer;
        string _state;
        Vector2 _aim;
        bool _dead;

        public string View { get; private set; } = "Down";
        public bool FacingRight => _sr != null && _sr.flipX;

        public void Init(Sprite idle, Sprite walk)
        {
            _idle = idle;
            _walk = walk;
            if (_sr != null && _idle != null && _anim == null)
                _sr.sprite = _idle;
        }

        public void UseAnimator(RuntimeAnimatorController controller, bool facesLeft)
        {
            _facesLeft = facesLeft;
            if (controller == null || _sr == null) return;
            _anim = GetComponent<Animator>();
            if (_anim == null)
                _anim = gameObject.AddComponent<Animator>();
            _anim.runtimeAnimatorController = controller;
            _anim.updateMode = AnimatorUpdateMode.Normal;
            _anim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            Play("Idle", true);
        }

        public void SetAim(Vector2 worldDir)
        {
            if (worldDir.sqrMagnitude > 0.0001f)
                _aim = worldDir;
        }

        public void NotifyAttack()
        {
            if (_anim == null || _dead) return;
            _actionTimer = 0.3f;
            Play("Attack", true);
        }

        public void NotifyDeath()
        {
            _dead = true;
            _actionTimer = 10f;
            if (_rb != null) _rb.linearVelocity = Vector2.zero;
            Play("Death", true);
        }

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _rb = GetComponent<Rigidbody2D>();
            _health = GetComponent<Health>();
        }

        void OnEnable()
        {
            if (_health != null)
            {
                _health.Damaged += OnDamaged;
                _health.Died += OnDied;
            }
        }

        void OnDisable()
        {
            if (_health != null)
            {
                _health.Damaged -= OnDamaged;
                _health.Died -= OnDied;
            }
        }

        void OnDied(Health _) => NotifyDeath();

        void OnDamaged(Health health, int amount, Vector2 point)
        {
            if (_anim == null) return;
            _actionTimer = 0.16f;
            Play("Hurt", true);
        }

        void Update()
        {
            if (_sr == null || _dead) return;
            Vector2 velocity = _rb != null ? _rb.linearVelocity : Vector2.zero;
            UpdateFacing(_aim.sqrMagnitude > 0.01f ? _aim : velocity);
            if (_anim != null)
            {
                if (_actionTimer > 0f)
                {
                    _actionTimer -= Time.deltaTime;
                    return;
                }
                bool moving = velocity.sqrMagnitude > 0.05f;
                Play(moving ? "Walk" : "Idle", false);
                return;
            }

            if (Mathf.Abs(velocity.x) > 0.05f)
                _sr.flipX = velocity.x < 0f;

            bool legacyMoving = velocity.sqrMagnitude > 0.05f;
            if (!legacyMoving || _walk == null)
            {
                if (_idle != null)
                    _sr.sprite = _idle;
                return;
            }

            _timer += Time.deltaTime;
            if (_timer >= 0.16f)
            {
                _timer -= 0.16f;
                _step = !_step;
            }
            _sr.sprite = _step ? _walk : (_idle != null ? _idle : _sr.sprite);
        }

        void UpdateFacing(Vector2 velocity)
        {
            if (Mathf.Abs(velocity.x) <= 0.05f) return;
            if (_facesLeft)
                _sr.flipX = velocity.x > 0f;
            else
                _sr.flipX = velocity.x < 0f;
        }

        void Play(string state, bool restart)
        {
            if (_anim == null) return;
            ResolveView();
            string full = state + "_" + View;
            if (!Has(full)) full = state;
            if (!Has(full)) return;
            if (!restart && _state == full) return;
            _state = full;
            _anim.Play(full, 0, 0f);
        }

        void ResolveView()
        {
            Vector2 facing = _aim.sqrMagnitude > 0.01f ? _aim : (_rb != null ? _rb.linearVelocity : Vector2.down);
            if (facing.sqrMagnitude < 0.0001f) return;
            bool side = Mathf.Abs(facing.x) > Mathf.Abs(facing.y);
            View = side ? "Side" : (facing.y > 0f ? "Up" : "Down");
        }

        bool Has(string state) => _anim != null && _anim.HasState(0, Animator.StringToHash(state));
    }
}
