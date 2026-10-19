using UnityEngine;

namespace GameplayPrototype
{
    public enum BossPhase
    {
        Intro = 0,
        Phase1 = 1,
        Phase2 = 2,
        Phase3 = 3,
        Dead = 4
    }

    public enum BossAiState
    {
        Intro,
        Approach,
        MeleeTelegraph,
        MeleeStrike,
        ChargeTelegraph,
        ChargeActive,
        ChargeRecovery,
        AreaTelegraph,
        AreaStrike,
        AreaRecovery,
        PhaseTransition,
        Recovery,
        Stagger,
        Dead
    }

    /// <summary>
    /// Level 5 Gobrider Warlord — dedicated phase machine, not a scaled Elite Gobrider.
    /// Uses Gobrider art as temporary body until dedicated boss sprites exist.
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(Health))]
    public class BossWarlordController : MonoBehaviour
    {
        public const string BossDisplayName = "Gobrider Warlord Lv.8";

        [SerializeField] string displayName = BossDisplayName;
        [SerializeField] float moveSpeed = 2.15f;
        [SerializeField] float meleeRange = 1.35f;
        [SerializeField] int meleeDamage = 14;
        [SerializeField] float meleeTelegraph = 0.35f;
        [SerializeField] float meleeRecovery = 0.45f;
        [SerializeField] float chargeTelegraph = 0.7f;
        [SerializeField] float chargeSpeed = 8.2f;
        [SerializeField] float chargeDuration = 0.38f;
        [SerializeField] int chargeDamage = 18;
        [SerializeField] float chargeCooldown = 3.6f;
        [SerializeField] float areaTelegraph = 0.85f;
        [SerializeField] float areaRadius = 2.4f;
        [SerializeField] int areaDamage = 16;
        [SerializeField] float areaCooldown = 5.5f;
        [SerializeField] int maxSupportAlive = 3;
        [SerializeField] int supportPerSummon = 2;
        [SerializeField] int maxSummonsPerFight = 2;

        Transform _player;
        Rigidbody2D _rb;
        Health _health;
        Collider2D _selfCol;
        BossAiState _state = BossAiState.Intro;
        BossPhase _phase = BossPhase.Intro;
        Vector2 _chargeDir;
        Vector2 _home;
        float _stateTimer;
        float _chargeCd;
        float _areaCd;
        float _meleeCd;
        float _introTimer = 1.4f;
        int _summonsUsed;
        bool _chargeHit;
        bool _areaHit;
        bool _phaseFxPlayed;
        LineRenderer _telegraph;
        SpriteRenderer _warn;
        EnemySpawner _spawner;

        public string DisplayName => displayName;
        public BossPhase Phase => _phase;
        public BossAiState State => _state;
        public Health Health => _health;
        public float NormalizedHp => _health != null ? _health.Normalized : 0f;

        public event System.Action<BossPhase> PhaseChanged;
        public event System.Action Died;

        public void Configure(EnemySpawner spawner, string nameLabel, int hp)
        {
            _spawner = spawner;
            displayName = string.IsNullOrEmpty(nameLabel) ? BossDisplayName : nameLabel;
            _health = GetComponent<Health>();
            _health.Configure(Mathf.Max(1, hp), true);
            _health.SetSurvival(0.12f, false);
        }

        void Awake()
        {
            _rb = GetComponent<Rigidbody2D>();
            _health = GetComponent<Health>();
            _selfCol = GetComponent<Collider2D>();
            _rb.gravityScale = 0f;
            _rb.freezeRotation = true;
            _rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            _rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            _home = _rb.position;
            BuildTelegraph();
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
            HideTelegraph();
        }

        void Start()
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) _player = p.transform;
            if (_spawner == null)
                _spawner = FindFirstObjectByType<EnemySpawner>();
            Enter(BossAiState.Intro);
            SetPhase(BossPhase.Intro);
        }

        void FixedUpdate()
        {
            if (_state == BossAiState.Dead || _health == null || _health.IsDead)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }
            if (_player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) _player = p.transform;
            }
            TickPhaseFromHp();
            TickState(Time.fixedDeltaTime);
        }

        void TickPhaseFromHp()
        {
            if (_phase == BossPhase.Dead || _state == BossAiState.PhaseTransition || _state == BossAiState.Intro)
                return;
            float n = _health.Normalized;
            BossPhase want = BossPhase.Phase1;
            if (n < 0.35f) want = BossPhase.Phase3;
            else if (n < 0.70f) want = BossPhase.Phase2;
            if (want != _phase && want > _phase)
                BeginPhaseTransition(want);
        }

        void BeginPhaseTransition(BossPhase next)
        {
            SetPhase(next);
            Enter(BossAiState.PhaseTransition);
            _stateTimer = 1.1f;
            _phaseFxPlayed = false;
            _rb.linearVelocity = Vector2.zero;
            HideTelegraph();
            CombatVfx.SpawnBurst(transform.position, new Color(0.9f, 0.35f, 0.15f, 0.9f), 1.4f);
            if (HitStop.Instance != null)
                HitStop.Instance.Trigger(0.06f);
            ShakeCam(0.18f, 0.22f);
            if (next == BossPhase.Phase2)
                TrySummonSupport();
            else if (next == BossPhase.Phase3)
            {
                chargeCooldown = 2.8f;
                areaCooldown = 4.2f;
                meleeTelegraph = 0.28f;
            }
        }

        void SetPhase(BossPhase phase)
        {
            if (_phase == phase) return;
            _phase = phase;
            PhaseChanged?.Invoke(phase);
        }

        void TickState(float dt)
        {
            _chargeCd = Mathf.Max(0f, _chargeCd - dt);
            _areaCd = Mathf.Max(0f, _areaCd - dt);
            _meleeCd = Mathf.Max(0f, _meleeCd - dt);
            _stateTimer -= dt;

            switch (_state)
            {
                case BossAiState.Intro:
                    _introTimer -= dt;
                    _rb.linearVelocity = Vector2.zero;
                    if (_introTimer <= 0f)
                    {
                        SetPhase(BossPhase.Phase1);
                        Enter(BossAiState.Approach);
                    }
                    break;

                case BossAiState.Approach:
                    DoApproach();
                    break;

                case BossAiState.MeleeTelegraph:
                    FacePlayer();
                    _rb.linearVelocity = Vector2.zero;
                    ShowWarn(true);
                    if (_stateTimer <= 0f)
                        Enter(BossAiState.MeleeStrike);
                    break;

                case BossAiState.MeleeStrike:
                    _rb.linearVelocity = Vector2.zero;
                    DealMelee();
                    Enter(BossAiState.Recovery);
                    _stateTimer = meleeRecovery;
                    ShowWarn(false);
                    break;

                case BossAiState.ChargeTelegraph:
                    _rb.linearVelocity = Vector2.zero;
                    UpdateChargeTelegraph();
                    if (_stateTimer <= 0f)
                    {
                        Enter(BossAiState.ChargeActive);
                        _stateTimer = chargeDuration;
                        _chargeHit = false;
                    }
                    break;

                case BossAiState.ChargeActive:
                    _rb.linearVelocity = _chargeDir * chargeSpeed;
                    TryChargeHit();
                    if (_stateTimer <= 0f)
                    {
                        Enter(BossAiState.ChargeRecovery);
                        _stateTimer = _phase == BossPhase.Phase3 ? 0.55f : 0.75f;
                        HideTelegraph();
                        ShowWarn(false);
                        if (_phase == BossPhase.Phase3 && Random.value < 0.55f)
                            QueueAreaAfterCharge();
                    }
                    break;

                case BossAiState.ChargeRecovery:
                    _rb.linearVelocity = Vector2.zero;
                    if (_stateTimer <= 0f)
                    {
                        if (_queuedArea)
                        {
                            _queuedArea = false;
                            Enter(BossAiState.AreaTelegraph);
                            _stateTimer = areaTelegraph;
                            ShowAreaTelegraph();
                        }
                        else
                            Enter(BossAiState.Approach);
                    }
                    break;

                case BossAiState.AreaTelegraph:
                    _rb.linearVelocity = Vector2.zero;
                    UpdateAreaTelegraph();
                    if (_stateTimer <= 0f)
                    {
                        Enter(BossAiState.AreaStrike);
                        _areaHit = false;
                    }
                    break;

                case BossAiState.AreaStrike:
                    DealArea();
                    Enter(BossAiState.AreaRecovery);
                    _stateTimer = 0.55f;
                    HideTelegraph();
                    ShowWarn(false);
                    break;

                case BossAiState.AreaRecovery:
                case BossAiState.Recovery:
                case BossAiState.Stagger:
                    _rb.linearVelocity = Vector2.zero;
                    if (_stateTimer <= 0f)
                        Enter(BossAiState.Approach);
                    break;

                case BossAiState.PhaseTransition:
                    _rb.linearVelocity = Vector2.zero;
                    if (!_phaseFxPlayed)
                    {
                        _phaseFxPlayed = true;
                        ShowWarn(true);
                    }
                    if (_stateTimer <= 0f)
                    {
                        ShowWarn(false);
                        Enter(BossAiState.Approach);
                    }
                    break;
            }
        }

        bool _queuedArea;

        void QueueAreaAfterCharge()
        {
            _queuedArea = true;
            _areaCd = areaCooldown;
        }

        void DoApproach()
        {
            if (_player == null)
            {
                _rb.linearVelocity = Vector2.zero;
                return;
            }

            Vector2 to = (Vector2)_player.position - _rb.position;
            float dist = to.magnitude;

            if (_phase >= BossPhase.Phase2 && _areaCd <= 0f && dist < areaRadius + 1.2f && dist > 0.8f)
            {
                Enter(BossAiState.AreaTelegraph);
                _stateTimer = areaTelegraph;
                ShowAreaTelegraph();
                return;
            }

            if (_chargeCd <= 0f && dist > 1.6f && dist < 5.5f)
            {
                _chargeDir = to.sqrMagnitude > 0.01f ? to.normalized : Vector2.right;
                Enter(BossAiState.ChargeTelegraph);
                _stateTimer = chargeTelegraph * (_phase == BossPhase.Phase3 ? 0.85f : 1f);
                ShowChargeTelegraph();
                return;
            }

            if (dist <= meleeRange && _meleeCd <= 0f)
            {
                Enter(BossAiState.MeleeTelegraph);
                _stateTimer = meleeTelegraph;
                return;
            }

            Vector2 dir = to.sqrMagnitude > 0.01f ? to.normalized : Vector2.zero;
            float speed = moveSpeed * (_phase == BossPhase.Phase3 ? 1.12f : 1f);
            _rb.linearVelocity = dir * speed;
            FacePlayer();
        }

        void FacePlayer()
        {
            if (_player == null) return;
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.flipX = _player.position.x < transform.position.x;
        }

        void DealMelee()
        {
            _meleeCd = 1.1f;
            if (_player == null) return;
            if (Vector2.Distance(_rb.position, _player.position) > meleeRange + 0.35f) return;
            var h = _player.GetComponent<Health>();
            if (h != null && !h.IsDead)
            {
                Vector2 kb = ((Vector2)_player.position - _rb.position).normalized * 2.2f;
                h.TakeDamage(meleeDamage, _player.position, kb);
                Vector2 facing = ((Vector2)_player.position - _rb.position).normalized;
                CombatVfx.SpawnSwordSlash(_player.position, facing, 0.9f);
            }
        }

        void TryChargeHit()
        {
            if (_chargeHit || _player == null) return;
            if (Vector2.Distance(_rb.position, _player.position) > 1.15f) return;
            _chargeHit = true;
            var h = _player.GetComponent<Health>();
            if (h != null && !h.IsDead)
            {
                Vector2 kb = _chargeDir * 3.4f;
                h.TakeDamage(chargeDamage, _player.position, kb);
            }
        }

        void DealArea()
        {
            _areaCd = areaCooldown;
            if (_areaHit || _player == null) return;
            _areaHit = true;
            if (Vector2.Distance(_rb.position, _player.position) > areaRadius) return;
            var h = _player.GetComponent<Health>();
            if (h != null && !h.IsDead)
            {
                Vector2 kb = ((Vector2)_player.position - _rb.position).normalized * 2.8f;
                h.TakeDamage(areaDamage, _player.position, kb);
            }
            CombatVfx.SpawnBurst(transform.position, new Color(1f, 0.45f, 0.2f, 0.85f), areaRadius * 0.55f);
            ShakeCam(0.12f, 0.16f);
        }

        static void ShakeCam(float amp, float dur)
        {
            var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            cam?.Shake(amp, dur);
        }

        void TrySummonSupport()
        {
            if (_spawner == null || _summonsUsed >= maxSummonsPerFight) return;
            int alive = _spawner.CountSupportAlive();
            int room = Mathf.Max(0, maxSupportAlive - alive);
            int n = Mathf.Min(supportPerSummon, room);
            if (n <= 0) return;
            _summonsUsed++;
            for (int i = 0; i < n; i++)
            {
                Vector2 offset = Random.insideUnitCircle.normalized * Random.Range(1.6f, 2.8f);
                _spawner.SpawnSupportGoblin(_rb.position + offset);
            }
        }

        void OnDamaged(Health _, int __, Vector2 ___)
        {
            if (_state == BossAiState.ChargeActive || _state == BossAiState.PhaseTransition || _state == BossAiState.Intro)
                return;
            if (_state == BossAiState.MeleeTelegraph || _state == BossAiState.ChargeTelegraph || _state == BossAiState.AreaTelegraph)
                return;
            if (Random.value > 0.22f) return;
            Enter(BossAiState.Stagger);
            _stateTimer = 0.18f;
            HideTelegraph();
        }

        void OnDied(Health _)
        {
            Enter(BossAiState.Dead);
            SetPhase(BossPhase.Dead);
            _rb.linearVelocity = Vector2.zero;
            HideTelegraph();
            ShowWarn(false);
            if (_selfCol != null) _selfCol.enabled = false;
            _spawner?.DespawnSupportAdds();
            CombatVfx.SpawnBurst(transform.position, new Color(1f, 0.55f, 0.2f, 1f), 1.8f);
            Died?.Invoke();
        }

        void Enter(BossAiState next)
        {
            _state = next;
            if (next == BossAiState.ChargeTelegraph)
                _chargeCd = chargeCooldown;
            if (next == BossAiState.AreaTelegraph)
                _areaCd = areaCooldown;
        }

        void BuildTelegraph()
        {
            var go = new GameObject("BossTelegraph");
            go.transform.SetParent(transform, false);
            _telegraph = go.AddComponent<LineRenderer>();
            _telegraph.positionCount = 2;
            _telegraph.startWidth = 0.12f;
            _telegraph.endWidth = 0.12f;
            _telegraph.material = new Material(Shader.Find("Sprites/Default"));
            _telegraph.startColor = new Color(1f, 0.35f, 0.15f, 0.85f);
            _telegraph.endColor = new Color(1f, 0.55f, 0.2f, 0.35f);
            _telegraph.sortingOrder = 4500;
            _telegraph.enabled = false;

            var warnGo = new GameObject("BossWarn");
            warnGo.transform.SetParent(transform, false);
            warnGo.transform.localPosition = new Vector3(0f, 1.35f, 0f);
            _warn = warnGo.AddComponent<SpriteRenderer>();
            _warn.sprite = CreateWarnSprite();
            _warn.color = new Color(1f, 0.35f, 0.2f, 0.95f);
            _warn.sortingOrder = 4600;
            _warn.enabled = false;
        }

        void ShowChargeTelegraph()
        {
            ShowWarn(true);
            if (_telegraph == null) return;
            _telegraph.enabled = true;
            _telegraph.loop = false;
            _telegraph.positionCount = 2;
            UpdateChargeTelegraph();
        }

        void UpdateChargeTelegraph()
        {
            if (_telegraph == null || !_telegraph.enabled) return;
            Vector3 a = transform.position;
            Vector3 b = a + (Vector3)(_chargeDir * 5.5f);
            _telegraph.SetPosition(0, a);
            _telegraph.SetPosition(1, b);
        }

        void ShowAreaTelegraph()
        {
            ShowWarn(true);
            if (_telegraph == null) return;
            _telegraph.enabled = true;
            _telegraph.loop = true;
            const int seg = 28;
            _telegraph.positionCount = seg + 1;
            UpdateAreaTelegraph();
        }

        void UpdateAreaTelegraph()
        {
            if (_telegraph == null || !_telegraph.enabled) return;
            if (_telegraph.positionCount < 3)
                return;
            int n = _telegraph.positionCount - 1;
            Vector3 c = transform.position;
            for (int i = 0; i <= n; i++)
            {
                float ang = (i / (float)n) * Mathf.PI * 2f;
                _telegraph.SetPosition(i, c + new Vector3(Mathf.Cos(ang), Mathf.Sin(ang), 0f) * areaRadius);
            }
        }

        void HideTelegraph()
        {
            if (_telegraph != null)
                _telegraph.enabled = false;
        }

        void ShowWarn(bool on)
        {
            if (_warn != null)
                _warn.enabled = on;
        }

        static Sprite CreateWarnSprite()
        {
            const int s = 8;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                bool edge = x == 0 || y == 0 || x == s - 1 || y == s - 1;
                bool cross = x == s / 2 || y == s / 2;
                tex.SetPixel(x, y, edge || cross ? Color.white : Color.clear);
            }
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}
