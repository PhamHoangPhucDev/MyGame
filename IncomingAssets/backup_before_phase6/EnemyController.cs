using UnityEngine;

namespace GameplayPrototype
{
    public enum EnemyKind
    {
        Goblin,
        Gobrider
    }

    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    public class EnemyController : MonoBehaviour
    {
        [SerializeField] EnemyKind kind = EnemyKind.Goblin;
        [SerializeField] string displayName = "Goblin Lv.3";
        [SerializeField] float moveSpeed = 2.2f;
        [SerializeField] float aggroRadius = 6.5f;
        [SerializeField] float attackRange = 0.85f;
        [SerializeField] float attackCooldown = 1.45f;
        [SerializeField] int attackDamage = 4;
        [SerializeField] float separationRadius = 0.55f;
        [SerializeField] float separationStrength = 1.4f;

        Transform _player;
        Rigidbody2D _rb;
        Health _health;
        float _attackTimer;
        Vector2 _avoid;

        public EnemyKind Kind => kind;
        public string DisplayName => displayName;
        public Health Health => _health;

        public void Setup(EnemyKind enemyKind, string nameLabel, int hp, float speed, float aggro, float range, int damage, float cooldown)
        {
            kind = enemyKind;
            displayName = nameLabel;
            moveSpeed = speed;
            aggroRadius = aggro;
            attackRange = range;
            attackDamage = damage;
            attackCooldown = cooldown;
            _health = GetComponent<Health>();
            _health.Configure(hp, true);
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _health = GetComponent<Health>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        void Start()
        {
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
                _player = playerGo.transform;
        }

        void FixedUpdate()
        {
            var playerHealth = _player != null ? _player.GetComponent<Health>() : null;
            if (_health.IsDead || _player == null || (playerHealth != null && playerHealth.IsDead))
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            _attackTimer -= Time.fixedDeltaTime;
            Vector2 toPlayer = (Vector2)_player.position - _rb.position;
            float dist = toPlayer.magnitude;

            if (dist > aggroRadius)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            ComputeSeparation();

            if (dist <= attackRange)
            {
                _rb.linearVelocity = _avoid * 0.35f;
                if (_attackTimer <= 0f)
                    TryAttack();
                return;
            }

            Vector2 dir = toPlayer.normalized;
            _rb.linearVelocity = (dir * moveSpeed) + _avoid;
        }

        void ComputeSeparation()
        {
            _avoid = Vector2.zero;
            var hits = Physics2D.OverlapCircleAll(_rb.position, separationRadius);
            foreach (var hit in hits)
            {
                if (hit.attachedRigidbody == null || hit.attachedRigidbody == _rb) continue;
                if (hit.GetComponent<EnemyController>() == null) continue;
                Vector2 away = _rb.position - hit.attachedRigidbody.position;
                float mag = away.magnitude;
                if (mag < 0.001f) continue;
                _avoid += away.normalized * (separationStrength * (1f - mag / separationRadius));
            }
        }

        void TryAttack()
        {
            GetComponent<EnemyVisual>()?.NotifyAttack();
            _attackTimer = attackCooldown;
            var playerHealth = _player.GetComponent<Health>();
            if (playerHealth == null || playerHealth.IsDead) return;

            Vector2 hitPoint = _player.position;
            Vector2 knock = ((Vector2)_player.position - _rb.position).normalized * 1.5f;
            if (playerHealth.TakeDamage(attackDamage, hitPoint, knock))
                DamagePopup.Spawn(hitPoint + Vector2.up * 0.2f, attackDamage, new Color(1f, 0.45f, 0.45f));
        }
    }
}
