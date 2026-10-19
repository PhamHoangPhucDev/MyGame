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
        // Cap how many enemies may strike the hero at once.
        const int MaxConcurrentAttackers = 3;
        const float AttackSlotHold = 0.35f;

        static int _activeAttackers;
        static readonly Collider2D[] SeparationHits = new Collider2D[16];

        [SerializeField] EnemyKind kind = EnemyKind.Goblin;
        [SerializeField] string displayName = "Goblin Lv.3";
        [SerializeField] float moveSpeed = 2.2f;
        [SerializeField] float aggroRadius = 6.5f;
        [SerializeField] float attackRange = 0.85f;
        [SerializeField] float attackCooldown = 1.45f;
        [SerializeField] int attackDamage = 4;
        [SerializeField] float separationRadius = 0.75f;
        [SerializeField] float separationStrength = 2.1f;
        [SerializeField] float holdDistance = 1.35f;
        [SerializeField] float telegraphTime = 0.28f;

        Transform _player;
        Rigidbody2D _rb;
        Health _health;
        float _attackTimer;
        Vector2 _avoid;
        bool _holdingSlot;
        bool _telegraphing;
        float _telegraphTimer;
        float _slotHoldTimer;
        float _orbitAngle;

        public EnemyKind Kind => kind;
        public string DisplayName => displayName;
        public Health Health => _health;
        public bool IsTelegraphing => _telegraphing;
        public bool IsAttackingNow => _holdingSlot;

        public static int ActiveAttackers => _activeAttackers;

        public void Setup(EnemyKind enemyKind, string nameLabel, int hp, float speed, float aggro, float range, int damage, float cooldown)
        {
            kind = enemyKind;
            displayName = nameLabel;
            moveSpeed = speed;
            aggroRadius = aggro;
            attackRange = range;
            attackDamage = damage;
            attackCooldown = cooldown;
            telegraphTime = enemyKind == EnemyKind.Gobrider ? 0.32f : 0.08f;
            holdDistance = enemyKind == EnemyKind.Gobrider ? 1.55f : 1.25f;
            separationRadius = enemyKind == EnemyKind.Gobrider ? 0.95f : 0.72f;
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
            _orbitAngle = Random.Range(0f, Mathf.PI * 2f);
        }

        void Start()
        {
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
                _player = playerGo.transform;
        }

        void OnDisable()
        {
            ReleaseAttackSlot();
            _telegraphing = false;
            _slotHoldTimer = 0f;
        }

        void FixedUpdate()
        {
            var playerHealth = _player != null ? _player.GetComponent<Health>() : null;
            if (_health.IsDead || _player == null || (playerHealth != null && playerHealth.IsDead))
            {
                _rb.linearVelocity = Vector2.zero;
                ReleaseAttackSlot();
                return;
            }

            _attackTimer -= Time.fixedDeltaTime;
            if (_slotHoldTimer > 0f)
            {
                _slotHoldTimer -= Time.fixedDeltaTime;
                if (_slotHoldTimer <= 0f && !_telegraphing)
                    ReleaseAttackSlot();
            }

            Vector2 toPlayer = (Vector2)_player.position - _rb.position;
            float dist = toPlayer.magnitude;

            if (dist > aggroRadius)
            {
                _rb.linearVelocity = Vector2.zero;
                ReleaseAttackSlot();
                _telegraphing = false;
                return;
            }

            ComputeSeparation();

            if (_telegraphing)
            {
                TickTelegraph(toPlayer);
                return;
            }

            if (dist <= attackRange)
            {
                // Recovering or already occupying a strike window — orbit, don't re-strike.
                if (_attackTimer > 0f || _slotHoldTimer > 0f)
                {
                    OrbitAroundPlayer(toPlayer, holdDistance);
                    return;
                }

                if (!TryClaimAttackSlot())
                {
                    OrbitAroundPlayer(toPlayer, holdDistance);
                    return;
                }

                _rb.linearVelocity = _avoid * 0.35f;
                BeginAttack();
                return;
            }

            ReleaseAttackSlot();
            Vector2 dir = toPlayer.normalized;
            float approach = dist < holdDistance + 0.35f ? 0.55f : 1f;
            _rb.linearVelocity = (dir * moveSpeed * approach) + _avoid;
        }

        void TickTelegraph(Vector2 toPlayer)
        {
            _rb.linearVelocity = _avoid * 0.2f;
            _telegraphTimer -= Time.fixedDeltaTime;
            if (_telegraphTimer > 0f) return;
            _telegraphing = false;
            DealDamage(toPlayer);
            FinishStrike();
        }

        void BeginAttack()
        {
            GetComponent<EnemyVisual>()?.NotifyAttack();
            if (kind == EnemyKind.Gobrider || telegraphTime > 0.1f)
            {
                _telegraphing = true;
                _telegraphTimer = telegraphTime;
                return;
            }

            DealDamage((Vector2)_player.position - _rb.position);
            FinishStrike();
        }

        void FinishStrike()
        {
            _attackTimer = attackCooldown;
            // Keep the slot occupied so other enemies cannot all strike in the same FixedUpdate.
            _slotHoldTimer = AttackSlotHold;
        }

        void DealDamage(Vector2 toPlayer)
        {
            var playerHealth = _player.GetComponent<Health>();
            if (playerHealth == null || playerHealth.IsDead) return;

            Vector2 hitPoint = _player.position;
            Vector2 knock = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized * 1.5f : Vector2.up;
            if (playerHealth.TakeDamage(attackDamage, hitPoint, knock))
                DamagePopup.Spawn(hitPoint + Vector2.up * 0.2f, attackDamage, new Color(1f, 0.45f, 0.45f));
        }

        bool TryClaimAttackSlot()
        {
            if (_holdingSlot) return true;
            if (_activeAttackers >= MaxConcurrentAttackers) return false;
            _activeAttackers++;
            _holdingSlot = true;
            return true;
        }

        void ReleaseAttackSlot()
        {
            if (!_holdingSlot) return;
            _holdingSlot = false;
            _slotHoldTimer = 0f;
            _activeAttackers = Mathf.Max(0, _activeAttackers - 1);
        }

        void OrbitAroundPlayer(Vector2 toPlayer, float radius)
        {
            _orbitAngle += Time.fixedDeltaTime * 1.6f;
            Vector2 tangent = new Vector2(-toPlayer.y, toPlayer.x);
            if (tangent.sqrMagnitude < 0.001f)
                tangent = Vector2.right;
            tangent.Normalize();
            Vector2 desired = (Vector2)_player.position + new Vector2(Mathf.Cos(_orbitAngle), Mathf.Sin(_orbitAngle)) * radius;
            Vector2 toDesired = desired - _rb.position;
            Vector2 push = toDesired.sqrMagnitude > 0.01f ? toDesired.normalized : tangent;
            _rb.linearVelocity = push * (moveSpeed * 0.7f) + tangent * (moveSpeed * 0.35f) + _avoid;
        }

        void ComputeSeparation()
        {
            _avoid = Vector2.zero;
            int count = Physics2D.OverlapCircle(_rb.position, separationRadius, ContactFilter2D.noFilter, SeparationHits);
            for (int i = 0; i < count; i++)
            {
                var hit = SeparationHits[i];
                if (hit == null || hit.attachedRigidbody == null || hit.attachedRigidbody == _rb) continue;
                if (!hit.TryGetComponent<EnemyController>(out _)) continue;
                Vector2 away = _rb.position - hit.attachedRigidbody.position;
                float mag = away.magnitude;
                if (mag < 0.001f) continue;
                _avoid += away.normalized * (separationStrength * (1f - mag / separationRadius));
            }
        }
    }
}
