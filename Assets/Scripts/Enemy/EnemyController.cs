using UnityEngine;

namespace GameplayPrototype
{
    public enum EnemyKind
    {
        Goblin,
        Gobrider
    }

    public enum EnemyAiState
    {
        Idle,
        Patrol,
        Chase,
        Approach,
        Attack,
        Recovery,
        Stagger,
        ChargeTelegraph,
        ChargeActive,
        ChargeRecovery,
        Dead
    }

    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    public class EnemyController : MonoBehaviour
    {
        public const int MaxConcurrentAttackers = 3;
        public const float AttackSurfaceGap = 0.15f;
        const int SurroundSlots = 6;
        const float AttackSlotHold = 0.35f;
        const float StaggerSeconds = 0.15f;

        static int _activeAttackers;
        static readonly EnemyController[] SlotOwners = new EnemyController[SurroundSlots];
        static readonly Collider2D[] SeparationHits = new Collider2D[16];
        static readonly RaycastHit2D[] LosHits = new RaycastHit2D[8];
        static readonly RaycastHit2D[] ChargeHits = new RaycastHit2D[8];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => ClearAttackSlots();

        public static void ClearAttackSlots()
        {
            _activeAttackers = 0;
            for (int i = 0; i < SlotOwners.Length; i++)
                SlotOwners[i] = null;
        }

        [Header("Identity")]
        [SerializeField] EnemyKind kind = EnemyKind.Goblin;
        [SerializeField] string displayName = "Goblin Lv.3";

        [Header("Movement")]
        [SerializeField] float moveSpeed = 2.2f;
        [SerializeField] float chaseSpeed = 2.6f;
        [SerializeField] float patrolSpeed = 1.35f;

        [Header("Detection")]
        [SerializeField] float detectionRadius = 5.2f;
        [SerializeField] float loseAggroRadius = 7.5f;
        [SerializeField] float patrolRadius = 2.4f;
        [SerializeField] bool useLineOfSight = true;

        [Header("Combat")]
        [SerializeField] float attackCooldown = 1.45f;
        [SerializeField] int attackDamage = 4;
        [SerializeField] float separationRadius = 0.55f;
        [SerializeField] float separationStrength = 1.2f;
        [SerializeField] float telegraphTime = 0.18f;
        [SerializeField] float recoveryTime = 0.35f;

        [Header("Gobrider Charge")]
        [SerializeField] float chargeTelegraph = 0.55f;
        [SerializeField] float chargeSpeed = 7.5f;
        [SerializeField] float chargeDuration = 0.32f;
        [SerializeField] float chargeCooldown = 3.2f;
        [SerializeField] int chargeDamage = 10;

        Transform _player;
        Collider2D _selfCollider;
        Collider2D _heroCollider;
        Rigidbody2D _rb;
        Health _health;
        EnemyAiState _state = EnemyAiState.Idle;
        Vector2 _home;
        Vector2 _patrolTarget;
        Vector2 _avoid;
        Vector2 _chargeDir;
        Vector2 _knockStart;
        float _knockTravel;
        float _stateTimer;
        float _attackTimer;
        float _slotHoldTimer;
        float _staggerTimer;
        float _stuckTimer;
        float _idlePause;
        float _strafeSign = 1f;
        float _chargeCd;
        int _surroundSlot = -1;
        bool _holdingSlot;
        bool _chargeHit;

        SpriteRenderer _warnMark;

        public EnemyKind Kind => kind;
        public string DisplayName => displayName;
        public Health Health => _health;
        public EnemyAiState State => _state;
        public bool IsAttackingNow => _holdingSlot || _state == EnemyAiState.Attack || _state == EnemyAiState.ChargeActive;
        public bool IsStaggered => _state == EnemyAiState.Stagger;
        public bool IsTelegraphing => _state == EnemyAiState.ChargeTelegraph || (_state == EnemyAiState.Attack && _stateTimer > 0f);
        public float LastKnockbackDistance => _knockTravel;
        public float SurfaceGap { get; private set; }
        public static int ActiveAttackers => _activeAttackers;

        public void SetHitDelay(float seconds) => telegraphTime = Mathf.Max(0.02f, seconds);
        public void SetChargeDamage(int damage) => chargeDamage = damage;

        public void Setup(EnemyKind enemyKind, string nameLabel, int hp, float speed, float aggro, float range, int damage, float cooldown)
        {
            kind = enemyKind;
            displayName = nameLabel;
            moveSpeed = speed;
            chaseSpeed = enemyKind == EnemyKind.Gobrider ? speed * 0.95f : speed * 1.15f;
            patrolSpeed = speed * 0.65f;
            detectionRadius = aggro;
            loseAggroRadius = aggro + 2.2f;
            attackDamage = damage;
            attackCooldown = cooldown;
            telegraphTime = enemyKind == EnemyKind.Gobrider ? 0.22f : 0.12f;
            recoveryTime = enemyKind == EnemyKind.Gobrider ? 0.55f : 0.28f;
            patrolRadius = enemyKind == EnemyKind.Gobrider ? 2.0f : 2.6f;
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
            _home = _rb.position;
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
            EnsureWarnMark();
            PickPatrolTarget();
            Enter(EnemyAiState.Patrol);
        }

        void OnDisable()
        {
            if (_health != null)
                _health.Damaged -= OnDamaged;
            ReleaseAttackSlot();
            ReleaseSurroundSlot();
            HideWarn();
        }

        void OnDamaged(Health health, int amount, Vector2 hitPoint)
        {
            if (_health.IsDead)
            {
                Enter(EnemyAiState.Dead);
                return;
            }
            ReleaseAttackSlot();
            _knockStart = _rb.position;
            _knockTravel = 0f;
            Enter(EnemyAiState.Stagger);
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

        void EnsureWarnMark()
        {
            if (_warnMark != null) return;
            var go = new GameObject("Warn");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, 1.15f, 0f);
            _warnMark = go.AddComponent<SpriteRenderer>();
            _warnMark.sortingOrder = 80;
            _warnMark.enabled = false;
            _warnMark.sprite = WarnSprite;
        }

        // Shared by every enemy. It used to be built per spawn, leaking one texture per enemy.
        static Sprite _warnSprite;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetWarnSprite() => _warnSprite = null;

        static Sprite WarnSprite
        {
            get
            {
                if (_warnSprite != null) return _warnSprite;
                var tex = new Texture2D(8, 8, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };
                var px = new Color32[64];
                for (int y = 1; y < 6; y++)
                for (int x = 3; x < 5; x++)
                    px[y * 8 + x] = new Color32(255, 220, 60, 255);
                px[6 * 8 + 3] = new Color32(255, 220, 60, 255);
                px[6 * 8 + 4] = new Color32(255, 220, 60, 255);
                tex.SetPixels32(px);
                tex.Apply(false, false);
                _warnSprite = Sprite.Create(tex, new Rect(0, 0, 8, 8), new Vector2(0.5f, 0.5f), 16f);
                _warnSprite.hideFlags = HideFlags.HideAndDontSave;
                return _warnSprite;
            }
        }

        void ShowWarn(bool on) { if (_warnMark != null) _warnMark.enabled = on; }
        void HideWarn() => ShowWarn(false);

        void Enter(EnemyAiState next)
        {
            _state = next;
            _stateTimer = 0f;
            switch (next)
            {
                case EnemyAiState.Idle:
                    _idlePause = Random.Range(0.4f, 1.1f);
                    _rb.linearVelocity = Vector2.zero;
                    break;
                case EnemyAiState.Patrol:
                    PickPatrolTarget();
                    break;
                case EnemyAiState.Attack:
                    _stateTimer = telegraphTime;
                    GetComponent<EnemyVisual>()?.NotifyAttack();
                    break;
                case EnemyAiState.Recovery:
                    _stateTimer = recoveryTime;
                    ReleaseAttackSlot();
                    break;
                case EnemyAiState.Stagger:
                    _staggerTimer = StaggerSeconds;
                    HideWarn();
                    break;
                case EnemyAiState.ChargeTelegraph:
                    _stateTimer = chargeTelegraph;
                    _chargeDir = ((Vector2)_player.position - _rb.position).normalized;
                    if (_chargeDir.sqrMagnitude < 0.01f) _chargeDir = Vector2.right;
                    _chargeHit = false;
                    ShowWarn(true);
                    GameAudio.Play(Sfx.EnemyWarn);
                    GetComponent<EnemyVisual>()?.NotifyAttack();
                    break;
                case EnemyAiState.ChargeActive:
                    _stateTimer = chargeDuration;
                    HideWarn();
                    break;
                case EnemyAiState.ChargeRecovery:
                    _stateTimer = recoveryTime + 0.2f;
                    _chargeCd = chargeCooldown;
                    ReleaseAttackSlot();
                    HideWarn();
                    break;
                case EnemyAiState.Dead:
                    _rb.linearVelocity = Vector2.zero;
                    ReleaseAttackSlot();
                    ReleaseSurroundSlot();
                    HideWarn();
                    GetComponent<R2CMonsterShot>()?.HideAim();
                    GetComponent<EnemyVisual>()?.NotifyDeath();
                    break;
            }
        }

        void FixedUpdate()
        {
            if (_health != null && _health.IsDead)
            {
                if (_state != EnemyAiState.Dead) Enter(EnemyAiState.Dead);
                return;
            }

            var playerHealth = _player != null ? _player.GetComponent<Health>() : null;
            if (_player == null || (playerHealth != null && playerHealth.IsDead))
            {
                _rb.linearVelocity = Vector2.zero;
                ReleaseAttackSlot();
                return;
            }

            _attackTimer -= Time.fixedDeltaTime;
            _chargeCd -= Time.fixedDeltaTime;
            if (_slotHoldTimer > 0f)
            {
                _slotHoldTimer -= Time.fixedDeltaTime;
                if (_slotHoldTimer <= 0f && _state != EnemyAiState.Attack)
                    ReleaseAttackSlot();
            }

            SurfaceGap = MeasureSurfaceGap();
            Transform aimTarget = StatusTaunt.ForcedTarget(this) ?? _player;
            Vector2 toPlayer = (Vector2)aimTarget.position - _rb.position;
            float centerDist = toPlayer.magnitude;
            if (toPlayer.sqrMagnitude > 0.0001f)
                GetComponent<EnemyVisual>()?.SetAim(toPlayer);
            ComputeSeparation(toPlayer);

            switch (_state)
            {
                case EnemyAiState.Idle: TickIdle(centerDist); break;
                case EnemyAiState.Patrol: TickPatrol(centerDist); break;
                case EnemyAiState.Chase: TickChase(toPlayer, centerDist); break;
                case EnemyAiState.Approach: TickApproach(toPlayer, centerDist); break;
                case EnemyAiState.Attack: TickAttack(toPlayer); break;
                case EnemyAiState.Recovery: TickRecovery(toPlayer, centerDist); break;
                case EnemyAiState.Stagger: TickStagger(); break;
                case EnemyAiState.ChargeTelegraph: TickChargeTelegraph(); break;
                case EnemyAiState.ChargeActive: TickChargeActive(); break;
                case EnemyAiState.ChargeRecovery: TickChargeRecovery(toPlayer, centerDist); break;
            }
        }

        void TickIdle(float centerDist)
        {
            _rb.linearVelocity = Vector2.zero;
            _idlePause -= Time.fixedDeltaTime;
            if (CanDetect(centerDist)) { Enter(EnemyAiState.Chase); return; }
            if (_idlePause <= 0f) Enter(EnemyAiState.Patrol);
        }

        void TickPatrol(float centerDist)
        {
            if (CanDetect(centerDist)) { Enter(EnemyAiState.Chase); return; }

            Vector2 to = _patrolTarget - _rb.position;
            if (to.sqrMagnitude < 0.08f)
            {
                Enter(EnemyAiState.Idle);
                return;
            }

            Vector2 desired = to.normalized * patrolSpeed + _avoid * 0.5f;
            ApplyMove(desired, to);
        }

        void TickChase(Vector2 toPlayer, float centerDist)
        {
            if (centerDist > loseAggroRadius)
            {
                ReleaseSurroundSlot();
                Enter(EnemyAiState.Patrol);
                return;
            }
            if (StatusTaunt.ForcedTarget(this) == null && HoldRanged(toPlayer, centerDist)) return;

            // Prefer charge window before collapsing into melee approach.
            if (kind == EnemyKind.Gobrider && _chargeCd <= 0f && centerDist < 4.2f && centerDist > 1.4f && HasLos())
            {
                if (TryClaimAttackSlot())
                {
                    Enter(EnemyAiState.ChargeTelegraph);
                    return;
                }
            }

            if (SurfaceGap <= AttackSurfaceGap + 0.35f)
            {
                Enter(EnemyAiState.Approach);
                return;
            }

            ClaimSurroundSlot();
            Vector2 goal = SurroundGoal() - _rb.position;
            if (goal.sqrMagnitude < 0.01f) goal = toPlayer;
            Vector2 desired = goal.normalized * chaseSpeed + _avoid;
            ApplyMove(desired, goal);
        }

        void TickApproach(Vector2 toPlayer, float centerDist)
        {
            if (centerDist > loseAggroRadius) { Enter(EnemyAiState.Patrol); return; }
            if (StatusTaunt.ForcedTarget(this) == null && HoldRanged(toPlayer, centerDist)) return;

            if (SurfaceGap <= AttackSurfaceGap)
            {
                if (_attackTimer > 0f || !TryClaimAttackSlot())
                {
                    HoldNearHero(toPlayer);
                    return;
                }
                Enter(EnemyAiState.Attack);
                return;
            }

            ClaimSurroundSlot();
            Vector2 goal = SurroundGoal() - _rb.position;
            if (goal.sqrMagnitude < 0.01f) goal = AimPoint() - _rb.position;
            Vector2 desired = goal.normalized * chaseSpeed + _avoid;
            ApplyMove(desired, goal);
            if (_stuckTimer > 0.4f)
                Enter(EnemyAiState.Chase);
        }

        bool HoldRanged(Vector2 toPlayer, float centerDist)
        {
            var shot = GetComponent<R2CMonsterShot>();
            if (shot == null || !shot.Ranged) return false;
            if (centerDist < shot.StandOff)
            {
                ApplyMove(-toPlayer.normalized * moveSpeed, -toPlayer);
                return true;
            }
            if (centerDist <= shot.Range && _attackTimer <= 0f && TryClaimAttackSlot())
            {
                Enter(EnemyAiState.Attack);
                return true;
            }
            return false;
        }

        void TickAttack(Vector2 toPlayer)
        {
            _rb.linearVelocity = _avoid * 0.15f;
            var shot = GetComponent<R2CMonsterShot>();
            var slam = GetComponent<CyclopsSlam>();
            if (shot != null && shot.ShowsAim && _player != null)
                shot.ShowAim(CombatTargeting.BodyPoint(_player));
            if (slam != null)
                slam.Present(telegraphTime - _stateTimer, telegraphTime);
            _stateTimer -= Time.fixedDeltaTime;
            if (_stateTimer > 0f) return;
            if (slam != null)
                slam.Strike(toPlayer);
            else if (shot != null && shot.Ranged)
                shot.Fire(_player != null ? CombatTargeting.BodyPoint(_player) : (Vector2)transform.position + toPlayer);
            else
                DealDamage(toPlayer, attackDamage, 1.5f);
            _attackTimer = attackCooldown;
            _slotHoldTimer = AttackSlotHold;
            Enter(EnemyAiState.Recovery);
        }

        void TickRecovery(Vector2 toPlayer, float centerDist)
        {
            HoldNearHero(toPlayer);
            _stateTimer -= Time.fixedDeltaTime;
            if (_stateTimer > 0f) return;
            if (centerDist > loseAggroRadius) Enter(EnemyAiState.Patrol);
            else Enter(EnemyAiState.Approach);
        }

        void TickStagger()
        {
            _staggerTimer -= Time.fixedDeltaTime;
            _rb.linearVelocity *= 0.92f;
            _knockTravel = Vector2.Distance(_knockStart, _rb.position);
            if (_staggerTimer > 0f) return;
            _rb.linearVelocity *= 0.35f;
            Enter(EnemyAiState.Chase);
        }

        void TickChargeTelegraph()
        {
            _rb.linearVelocity = Vector2.zero;
            _stateTimer -= Time.fixedDeltaTime;
            if (_stateTimer > 0f) return;
            Enter(EnemyAiState.ChargeActive);
        }

        void TickChargeActive()
        {
            _stateTimer -= Time.fixedDeltaTime;
            Vector2 next = _rb.position + _chargeDir * (chargeSpeed * Time.fixedDeltaTime);
            float step = chargeSpeed * Time.fixedDeltaTime;
            int n = Physics2D.CircleCast(_rb.position, 0.28f, _chargeDir, new ContactFilter2D { useTriggers = false }, ChargeHits, step);
            for (int i = 0; i < n; i++)
            {
                var col = ChargeHits[i].collider;
                if (col == null || col == _selfCollider) continue;
                if (col.attachedRigidbody != null && col.attachedRigidbody.GetComponent<EnemyController>() != null) continue;
                if (col == _heroCollider || col.transform == _player)
                {
                    if (!_chargeHit)
                    {
                        _chargeHit = true;
                        DealDamage((Vector2)_player.position - _rb.position, chargeDamage, 2.8f);
                    }
                    continue;
                }
                Enter(EnemyAiState.ChargeRecovery);
                return;
            }
            _rb.MovePosition(next);
            if (_stateTimer <= 0f)
                Enter(EnemyAiState.ChargeRecovery);
        }

        void TickChargeRecovery(Vector2 toPlayer, float centerDist)
        {
            HoldNearHero(toPlayer);
            _stateTimer -= Time.fixedDeltaTime;
            if (_stateTimer > 0f) return;
            Enter(centerDist > loseAggroRadius ? EnemyAiState.Patrol : EnemyAiState.Chase);
        }

        bool CanDetect(float centerDist)
        {
            if (centerDist > detectionRadius) return false;
            if (!useLineOfSight) return true;
            return HasLos();
        }

        bool HasLos()
        {
            if (_player == null) return false;
            Vector2 origin = _rb.position + Vector2.up * 0.35f;
            Vector2 dest = (Vector2)_player.position + Vector2.up * 0.35f;
            Vector2 dir = dest - origin;
            float dist = dir.magnitude;
            if (dist < 0.01f) return true;
            int n = Physics2D.Raycast(origin, dir.normalized, new ContactFilter2D { useTriggers = false }, LosHits, dist);
            for (int i = 0; i < n; i++)
            {
                var col = LosHits[i].collider;
                if (col == null || col == _selfCollider) continue;
                if (col == _heroCollider || col.transform == _player) return true;
                if (col.GetComponent<EnemyController>() != null) continue;
                return false;
            }
            return true;
        }

        void PickPatrolTarget()
        {
            for (int i = 0; i < 8; i++)
            {
                Vector2 candidate = _home + Random.insideUnitCircle * patrolRadius;
                if (Physics2D.OverlapCircle(candidate, 0.25f) != null) continue;
                _patrolTarget = candidate;
                return;
            }
            _patrolTarget = _home;
        }

        void ClaimSurroundSlot()
        {
            if (_surroundSlot >= 0 && SlotOwners[_surroundSlot] == this) return;
            ReleaseSurroundSlot();
            int best = -1;
            float bestScore = float.MaxValue;
            for (int i = 0; i < SurroundSlots; i++)
            {
                if (SlotOwners[i] != null && SlotOwners[i] != this) continue;
                Vector2 slotPos = SlotWorld(i);
                float score = Vector2.Distance(_rb.position, slotPos);
                if (score < bestScore)
                {
                    bestScore = score;
                    best = i;
                }
            }
            if (best >= 0)
            {
                _surroundSlot = best;
                SlotOwners[best] = this;
            }
        }

        void ReleaseSurroundSlot()
        {
            if (_surroundSlot < 0) return;
            if (SlotOwners[_surroundSlot] == this)
                SlotOwners[_surroundSlot] = null;
            _surroundSlot = -1;
        }

        Vector2 SlotWorld(int index)
        {
            float ang = (Mathf.PI * 2f * index) / SurroundSlots + 0.2f;
            float radius = kind == EnemyKind.Gobrider ? 1.55f : 1.25f;
            return (Vector2)_player.position + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radius;
        }

        Vector2 SurroundGoal()
        {
            if (_surroundSlot < 0) return AimPoint();
            return SlotWorld(_surroundSlot);
        }

        float MeasureSurfaceGap()
        {
            if (_selfCollider == null || _heroCollider == null)
                return Vector2.Distance(_rb.position, _player.position);
            ColliderDistance2D gap = _selfCollider.Distance(_heroCollider);
            if (gap.isOverlapped) return 0f;
            return gap.distance;
        }

        void DealDamage(Vector2 toPlayer, int damage, float knock)
        {
            var playerHealth = _player.GetComponent<Health>();
            if (playerHealth == null || playerHealth.IsDead) return;
            Vector2 hitPoint = _player.position;
            Vector2 dir = toPlayer.sqrMagnitude > 0.001f ? toPlayer.normalized : Vector2.up;
            if (playerHealth.TakeDamage(damage, hitPoint, dir * knock) && playerHealth.LastDamageApplied > 0)
                DamagePopup.Spawn(hitPoint + Vector2.up * 0.2f, playerHealth.LastDamageApplied, new Color(1f, 0.45f, 0.45f));
        }

        void ApplyMove(Vector2 desired, Vector2 focus)
        {
            if (_rb.linearVelocity.magnitude < 0.08f) _stuckTimer += Time.fixedDeltaTime;
            else _stuckTimer = 0f;

            if (_stuckTimer > 0.35f)
            {
                Vector2 side = new Vector2(-focus.y, focus.x);
                if (side.sqrMagnitude < 0.0001f) side = Vector2.right;
                desired = side.normalized * _strafeSign * chaseSpeed + focus.normalized * (chaseSpeed * 0.4f);
            }
            _rb.linearVelocity = desired * StatusSlow.Multiplier(this);
        }

        void HoldNearHero(Vector2 toPlayer)
        {
            Vector2 tangent = new Vector2(-toPlayer.y, toPlayer.x);
            if (tangent.sqrMagnitude < 0.0001f) tangent = Vector2.right;
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
            if (_heroCollider == null) return _player.position;
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
                if (mag < 0.001f) away = Random.insideUnitCircle.normalized;
                else away /= mag;
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
