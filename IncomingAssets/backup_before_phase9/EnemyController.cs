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
        public const int MaxConcurrentAttackers = 3;
        public const float AttackSurfaceGap = 0.15f;
        const float AttackSlotHold = 0.35f;
        const float StaggerSeconds = 0.15f;

        static int _activeAttackers;
        static readonly Collider2D[] SeparationHits = new Collider2D[16];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _activeAttackers = 0;
        }

        [SerializeField] EnemyKind kind = EnemyKind.Goblin;
        [SerializeField] string displayName = "Goblin Lv.3";
        [SerializeField] float moveSpeed = 2.2f;
        [SerializeField] float aggroRadius = 6.5f;
        [SerializeField] float attackRange = 0.85f;
        [SerializeField] float attackCooldown = 1.45f;
        [SerializeField] int attackDamage = 4;
        [SerializeField] float separationRadius = 0.55f;
        [SerializeField] float separationStrength = 1.2f;
        [SerializeField] float telegraphTime = 0.28f;

        Transform _player;
        Collider2D _selfCollider;
        Collider2D _heroCollider;
        Rigidbody2D _rb;
        Health _health;
        float _attackTimer;
        Vector2 _avoid;
        bool _holdingSlot;
        bool _telegraphing;
        float _telegraphTimer;
        float _slotHoldTimer;
        float _staggerTimer;
        float _stuckTimer;
        float _strafeSign = 1f;
        Vector2 _knockStart;
        float _knockTravel;

        public EnemyKind Kind => kind;
        public string DisplayName => displayName;
        public Health Health => _health;
        public bool IsTelegraphing => _telegraphing;
        public bool IsAttackingNow => _holdingSlot;
        public bool IsStaggered => _staggerTimer > 0f;
        public float LastKnockbackDistance => _knockTravel;
        public float SurfaceGap { get; private set; }

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
            _health = GetComponent<Health>();
            _health.Configure(hp, true);
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _health = GetComponent<Health>();
            _selfCollider = GetComponent<Collider2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _strafeSign = Random.value < 0.5f ? -1f : 1f;
        }

        void OnEnable()
        {
            if (_health != null)
                _health.Damaged += OnDamaged;
        }

        void Start()
        {
            var playerGo = GameObject.FindGameObjectWithTag("Player");
            if (playerGo != null)
            {
                _player = playerGo.transform;
                _heroCollider = playerGo.GetComponent<Collider2D>();
            }
            IgnoreEnemyCollisions();
        }

        void OnDisable()
        {
            if (_health != null)
                _health.Damaged -= OnDamaged;
            ReleaseAttackSlot();
            _telegraphing = false;
            _slotHoldTimer = 0f;
            _staggerTimer = 0f;
        }

        void OnDamaged(Health health, int amount, Vector2 hitPoint)
        {
            _telegraphing = false;
            ReleaseAttackSlot();
            if (_health.IsDead)
                return;

            _staggerTimer = StaggerSeconds;
            _knockStart = _rb.position;
            _knockTravel = 0f;
        }

        void IgnoreEnemyCollisions()
        {
            if (_selfCollider == null) return;
            var peers = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            for (int i = 0; i < peers.Length; i++)
            {
                if (peers[i] == this) continue;
                var other = peers[i].GetComponent<Collider2D>();
                if (other == null) continue;
                Physics2D.IgnoreCollision(_selfCollider, other, true);
            }
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

            SurfaceGap = MeasureSurfaceGap();
            Vector2 toPlayer = (Vector2)_player.position - _rb.position;
            float centerDist = toPlayer.magnitude;

            if (_staggerTimer > 0f)
            {
                _staggerTimer -= Time.fixedDeltaTime;
                _rb.linearVelocity *= 0.92f;
                _knockTravel = Vector2.Distance(_knockStart, _rb.position);
                if (_staggerTimer <= 0f)
                    _rb.linearVelocity *= 0.35f;
                return;
            }

            if (centerDist > aggroRadius)
            {
                _rb.linearVelocity = Vector2.zero;
                ReleaseAttackSlot();
                _telegraphing = false;
                _stuckTimer = 0f;
                return;
            }

            ComputeSeparation(toPlayer);

            if (_telegraphing)
            {
                TickTelegraph(toPlayer);
                return;
            }

            if (SurfaceGap <= AttackSurfaceGap)
            {
                _stuckTimer = 0f;
                if (_attackTimer > 0f || _slotHoldTimer > 0f)
                {
                    HoldNearHero(toPlayer);
                    return;
                }

                if (!TryClaimAttackSlot())
                {
                    HoldNearHero(toPlayer);
                    return;
                }

                _rb.linearVelocity = _avoid * 0.25f;
                BeginAttack(toPlayer);
                return;
            }

            ReleaseAttackSlot();
            Chase(toPlayer);
        }

        float MeasureSurfaceGap()
        {
            if (_selfCollider == null || _heroCollider == null)
                return Vector2.Distance(_rb.position, _player.position);

            ColliderDistance2D gap = _selfCollider.Distance(_heroCollider);
            if (gap.isOverlapped)
                return 0f;
            return gap.distance;
        }

        void TickTelegraph(Vector2 toPlayer)
        {
            _rb.linearVelocity = _avoid * 0.15f;
            _telegraphTimer -= Time.fixedDeltaTime;
            if (_telegraphTimer > 0f) return;
            _telegraphing = false;
            DealDamage(toPlayer);
            FinishStrike();
        }

        void BeginAttack(Vector2 toPlayer)
        {
            GetComponent<EnemyVisual>()?.NotifyAttack();
            if (kind == EnemyKind.Gobrider || telegraphTime > 0.1f)
            {
                _telegraphing = true;
                _telegraphTimer = telegraphTime;
                return;
            }

            DealDamage(toPlayer);
            FinishStrike();
        }

        void FinishStrike()
        {
            _attackTimer = attackCooldown;
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

        void Chase(Vector2 toPlayer)
        {
            Vector2 aim = AimPoint() - _rb.position;
            if (aim.sqrMagnitude < 0.0001f)
                aim = toPlayer.sqrMagnitude > 0.0001f ? toPlayer : Vector2.right;

            Vector2 desired = aim.normalized * moveSpeed + _avoid;
            if (_rb.linearVelocity.magnitude < 0.08f)
                _stuckTimer += Time.fixedDeltaTime;
            else
                _stuckTimer = 0f;

            if (_stuckTimer > 0.35f)
            {
                Vector2 side = new Vector2(-aim.y, aim.x);
                if (side.sqrMagnitude < 0.0001f) side = Vector2.right;
                desired = side.normalized * _strafeSign * moveSpeed + aim.normalized * (moveSpeed * 0.45f);
            }

            _rb.linearVelocity = desired;
        }

        void HoldNearHero(Vector2 toPlayer)
        {
            Vector2 tangent = new Vector2(-toPlayer.y, toPlayer.x);
            if (tangent.sqrMagnitude < 0.0001f)
                tangent = Vector2.right;
            tangent.Normalize();

            Vector2 velocity = tangent * _strafeSign * (moveSpeed * 0.28f) + _avoid * 0.35f;
            if (SurfaceGap > 0.04f)
            {
                Vector2 aim = AimPoint() - _rb.position;
                if (aim.sqrMagnitude > 0.0001f)
                    velocity += aim.normalized * (moveSpeed * 0.45f);
            }
            _rb.linearVelocity = velocity;
        }

        Vector2 AimPoint()
        {
            if (_heroCollider == null)
                return _player.position;
            return _heroCollider.ClosestPoint(_rb.position);
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

        void ComputeSeparation(Vector2 toPlayer)
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
                if (mag < 0.001f)
                    away = Random.insideUnitCircle.normalized;
                else
                    away /= mag;
                float falloff = 1f - Mathf.Clamp01(mag / separationRadius);
                _avoid += away * (separationStrength * falloff);
            }

            if (toPlayer.sqrMagnitude > 0.01f)
            {
                Vector2 offHero = -toPlayer.normalized;
                float radial = Vector2.Dot(_avoid, offHero);
                if (radial > 0f)
                    _avoid -= offHero * radial;
            }
        }
    }
}
