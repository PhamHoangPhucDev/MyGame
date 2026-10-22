using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameplayPrototype
{
    public enum EncounterId
    {
        None = 0,
        CampApproach = 1,
        CampDefense = 2,
        GobriderAmbush = 3,
        CombatClimax = 4,
        Complete = 5
    }

    /// <summary>
    /// Wave-driven spawn around the goblin camp. LevelFlow owns when waves start;
    /// this component only spawns, tracks alive enemies, and reports kills once.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] Transform player;
        [SerializeField] float minSpawnDistanceFromPlayer = 5.5f;
        [SerializeField] bool preferOffCameraSpawns = true;
        [SerializeField] Sprite goblinSprite;
        [SerializeField] Sprite gobriderSprite;
        [SerializeField] Sprite goblinWalkSprite;
        [SerializeField] Sprite gobriderWalkSprite;
        [SerializeField] RuntimeAnimatorController goblinAnimator;
        [SerializeField] RuntimeAnimatorController gobriderAnimator;
        [SerializeField] float topUpCheckInterval = 2.2f;

        readonly List<Vector2> _campPoints = new();
        readonly HashSet<Health> _alive = new();
        readonly HashSet<Health> _countedDeaths = new();
        Transform _enemyRoot;
        WaveConfig _activeWave;
        int _waveIndex = -1;
        int _waveSpawned;
        int _goblinsSpawned;
        int _ridersSpawned;
        int _elitesSpawned;
        int _bossesSpawned;
        int _waveKills;
        int _waveBudget;
        int _sessionKills;
        float _topUpTimer;
        bool _spawningEnabled;
        bool _waveActive;

        public EncounterId CurrentEncounter => _waveIndex < 0 ? EncounterId.None : (EncounterId)Mathf.Clamp(_waveIndex + 1, 1, 5);
        public int KillCount => _sessionKills;
        public int WaveIndex => _waveIndex;
        public int WaveKills => _waveKills;
        public int WaveBudget => _waveBudget;
        public int AliveCount => _alive.Count;
        public bool WaveActive => _waveActive;
        public bool WaveCleared => _waveActive && _waveBudget > 0 && _waveKills >= _waveBudget && AliveCount == 0;
        public Transform EnemyRoot => _enemyRoot;

        public event Action<int> EnemyKilled;
        public event Action WaveClearedEvent;
        public event Action AliveChanged;

        public void Configure(Transform playerTransform, Sprite goblin, Sprite gobrider, IEnumerable<Vector2> camps)
        {
            player = playerTransform;
            goblinSprite = goblin;
            gobriderSprite = gobrider;
            SetSpawnZones(camps);
        }

        public void SetSpawnZones(IEnumerable<Vector2> camps)
        {
            _campPoints.Clear();
            if (camps == null) return;
            foreach (var c in camps)
                _campPoints.Add(c);
        }

        void Awake()
        {
            if (_enemyRoot == null)
                _enemyRoot = new GameObject("Enemies").transform;
        }

        void Start()
        {
            EnsureCamps();
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }
            // LevelFlow owns when combat starts — do not auto-seed waves here.
            _spawningEnabled = false;
        }

        void Update()
        {
            if (!_spawningEnabled || !_waveActive || _activeWave == null) return;
            if (player != null)
            {
                var playerHealth = player.GetComponent<Health>();
                if (playerHealth != null && playerHealth.IsDead)
                    return;
            }

            PruneDeadRefs();
            if (WaveCleared)
            {
                _waveActive = false;
                WaveClearedEvent?.Invoke();
                return;
            }

            if (!_activeWave.allowTopUp) return;
            _topUpTimer -= Time.deltaTime;
            if (_topUpTimer > 0f) return;
            _topUpTimer = topUpCheckInterval;

            int remainingBudget = _waveBudget - _waveSpawned;
            if (remainingBudget <= 0) return;
            int room = Mathf.Max(0, _activeWave.aliveCap - AliveCount);
            int spawnNow = Mathf.Min(2, remainingBudget, room);
            if (spawnNow <= 0) return;
            SpawnRemaining(spawnNow);
        }

        void EnsureCamps()
        {
            if (_campPoints.Count > 0) return;
            _campPoints.Add(new Vector2(2.55f, 4.45f));
            _campPoints.Add(new Vector2(-2.35f, 2.55f));
            _campPoints.Add(new Vector2(-1.85f, 0.15f));
            _campPoints.Add(new Vector2(1.85f, 1.05f));
            _campPoints.Add(new Vector2(0.2f, 3.2f));
            _campPoints.Add(new Vector2(-3.1f, 1.4f));
        }

        public void SetSpawningEnabled(bool enabled)
        {
            _spawningEnabled = enabled;
        }

        public void BeginWave(int index, WaveConfig wave)
        {
            EnsureCamps();
            _activeWave = wave;
            _waveIndex = index;
            _waveSpawned = 0;
            _goblinsSpawned = 0;
            _ridersSpawned = 0;
            _elitesSpawned = 0;
            _bossesSpawned = 0;
            _waveKills = 0;
            _waveBudget = Mathf.Max(0, wave.goblins + wave.gobriders + wave.bossWarlord);
            _waveActive = _waveBudget > 0;
            _spawningEnabled = true;
            _topUpTimer = 0.35f;
            _countedDeaths.Clear();

            int initial = Mathf.Min(_waveBudget, Mathf.Max(1, wave.aliveCap));
            SpawnRemaining(initial);
            AliveChanged?.Invoke();
        }

        void SpawnRemaining(int count)
        {
            int goblinsLeft = Mathf.Max(0, _activeWave.goblins - _goblinsSpawned);
            int ridersLeft = Mathf.Max(0, _activeWave.gobriders - _ridersSpawned);
            int elitesLeft = Mathf.Max(0, _activeWave.eliteGobrider - _elitesSpawned);
            int bossesLeft = Mathf.Max(0, _activeWave.bossWarlord - _bossesSpawned);
            // Elite riders consume from rider budget visually but track separately.
            ridersLeft = Mathf.Max(0, ridersLeft - elitesLeft);
            for (int i = 0; i < count; i++)
            {
                if (_waveSpawned >= _waveBudget) break;
                Vector2 camp = _campPoints[UnityEngine.Random.Range(0, _campPoints.Count)];

                if (bossesLeft > 0)
                {
                    SpawnBossWarlord(camp);
                    bossesLeft--;
                    continue;
                }

                if (elitesLeft > 0)
                {
                    SpawnAt(camp, rider: true, elite: true);
                    elitesLeft--;
                    continue;
                }

                bool rider;
                if (ridersLeft > 0 && (goblinsLeft <= 0 || (_waveSpawned == 0 && _activeWave.gobriders > 0) || UnityEngine.Random.value < 0.35f))
                {
                    rider = true;
                    ridersLeft--;
                }
                else if (goblinsLeft > 0)
                {
                    rider = false;
                    goblinsLeft--;
                }
                else if (ridersLeft > 0)
                {
                    rider = true;
                    ridersLeft--;
                }
                else break;

                SpawnAt(camp, rider, elite: false);
            }
        }

        public void StopWaves()
        {
            _spawningEnabled = false;
            _waveActive = false;
            _activeWave = null;
        }

        public void ClearAllEnemies()
        {
            StopWaves();
            if (_enemyRoot != null)
            {
                for (int i = _enemyRoot.childCount - 1; i >= 0; i--)
                    Destroy(_enemyRoot.GetChild(i).gameObject);
            }
            _alive.Clear();
            _countedDeaths.Clear();
            _waveSpawned = 0;
            _waveKills = 0;
            _waveBudget = 0;
            _waveIndex = -1;
            EnemyController.ClearAttackSlots();
            AliveChanged?.Invoke();
        }

        public void ResetSession()
        {
            ClearAllEnemies();
            _sessionKills = 0;
        }

        public int CountAlive()
        {
            PruneDeadRefs();
            return _alive.Count;
        }

        void PruneDeadRefs()
        {
            if (_alive.Count == 0) return;
            _alive.RemoveWhere(h => h == null || h.IsDead);
        }

        void SpawnAt(Vector2 camp, bool rider, bool elite = false)
        {
            if (!TryPickSpawn(camp, out Vector2 pos))
                pos = camp + UnityEngine.Random.insideUnitCircle * 0.3f;
            if (rider && elite) SpawnEliteGobrider(pos);
            else if (rider) SpawnGobrider(pos);
            else SpawnGoblin(pos);
        }

        static readonly float[] SpawnDistanceSteps = { 1f, 0.7f, 0.5f };

        bool TryPickSpawn(Vector2 camp, out Vector2 pos)
        {
            if (TryPickSpawnAt(camp, minSpawnDistanceFromPlayer, out pos))
                return true;
            foreach (float step in SpawnDistanceSteps)
            {
                float minDist = Mathf.Max(2.5f, minSpawnDistanceFromPlayer * step);
                int start = UnityEngine.Random.Range(0, _campPoints.Count);
                for (int i = 0; i < _campPoints.Count; i++)
                    if (TryPickSpawnAt(_campPoints[(start + i) % _campPoints.Count], minDist, out pos))
                        return true;
            }
            return false;
        }

        bool TryPickSpawnAt(Vector2 camp, float minDistance, out Vector2 pos)
        {
            pos = camp;
            Vector2 best = camp;
            bool found = false;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                Vector2 candidate = camp + UnityEngine.Random.insideUnitCircle * 0.55f;
                if (player != null && Vector2.Distance(candidate, player.position) < minDistance)
                    continue;
                if (Physics2D.OverlapCircle(candidate, 0.32f) != null)
                    continue;
                if (preferOffCameraSpawns && IsOnScreen(candidate))
                {
                    if (!found) { best = candidate; found = true; }
                    continue;
                }
                pos = candidate;
                return true;
            }
            if (found) { pos = best; return true; }
            return false;
        }

        static bool IsOnScreen(Vector2 world)
        {
            var cam = Camera.main;
            if (cam == null) return false;
            Vector3 sp = cam.WorldToViewportPoint(world);
            return sp.z > 0f && sp.x > -0.02f && sp.x < 1.02f && sp.y > -0.02f && sp.y < 1.02f;
        }

        public GameObject SpawnR2C(R2CMonsterDef def, Vector2 pos)
        {
            if (def == null) return null;
            var kind = def.rider ? EnemyKind.Gobrider : EnemyKind.Goblin;
            var go = CreateEnemy(pos, kind, def.label, def.hp, def.speed, def.aggro, def.meleeRange, def.damage, def.cooldown,
                null, Color.white, def.colliderSize);
            var enemy = go.GetComponent<EnemyController>();
            enemy.SetHitDelay(0.1f);
            if (def.chargeDamage > 0) enemy.SetChargeDamage(def.chargeDamage);
            go.GetComponent<EnemyVisual>().UseAnimator(def.controller, true);
            if (def.ranged)
                go.AddComponent<R2CMonsterShot>().Configure(def, def.damage);
            if (def.telegraph != null && def.telegraph.Length >= 6)
            {
                enemy.SetHitDelay(1.2f);
                go.AddComponent<CyclopsSlam>().Configure(def);
            }
            if (def.bossBar)
                go.AddComponent<R2CBossMark>();
            RegisterEnemy(go, def.rider, elite: false, boss: def.bossBar, countTowardWave: false);
            return go;
        }

        public GameObject SpawnGoblin(Vector2 pos)
        {
            var go = CreateEnemy(pos, EnemyKind.Goblin, "Goblin Lv.3", 60, 2.05f, 5.2f, 0.75f, 4, 1.35f,
                goblinSprite, new Color(0.35f, 0.75f, 0.28f), new Vector2(0.45f, 0.5f));
            RegisterEnemy(go, rider: false);
            return go;
        }

        public GameObject SpawnGobrider(Vector2 pos)
        {
            var go = CreateEnemy(pos, EnemyKind.Gobrider, "Gobrider Lv.5", 100, 1.55f, 5.8f, 0.95f, 8, 1.9f,
                gobriderSprite, new Color(0.25f, 0.55f, 0.22f), new Vector2(0.7f, 0.65f));
            RegisterEnemy(go, rider: true, elite: false);
            return go;
        }

        /// <summary>
        /// Stronger Gobrider for Level 4 command wave. Does not alter normal Gobrider stats.
        /// </summary>
        public GameObject SpawnEliteGobrider(Vector2 pos)
        {
            var go = CreateEnemy(pos, EnemyKind.Gobrider, "Elite Gobrider Lv.6", 160, 1.65f, 6.4f, 1.05f, 11, 1.7f,
                gobriderSprite, new Color(0.55f, 0.22f, 0.18f), new Vector2(0.78f, 0.72f));
            // Subtle scale cue without breaking PPU / world consistency.
            go.transform.localScale = new Vector3(1.12f, 1.12f, 1f);
            RegisterEnemy(go, rider: true, elite: true);
            return go;
        }

        public GameObject SpawnBossWarlord(Vector2 pos)
        {
            if (!TryPickSpawn(pos, out Vector2 spawn))
                spawn = pos;
            var go = CreateBoss(spawn);
            RegisterEnemy(go, rider: false, elite: false, boss: true);
            BossHudUI.Ensure().Bind(go.GetComponent<BossWarlordController>());
            return go;
        }

        /// <summary>Phase-support goblins. Session kills only — not part of wave budget.</summary>
        public GameObject SpawnSupportGoblin(Vector2 pos)
        {
            if (!TryPickSpawn(pos, out Vector2 spawn))
                spawn = pos + UnityEngine.Random.insideUnitCircle * 0.4f;
            var go = CreateEnemy(spawn, EnemyKind.Goblin, "Goblin Lv.3", 60, 2.05f, 5.2f, 0.75f, 4, 1.35f,
                goblinSprite, new Color(0.35f, 0.7f, 0.3f), new Vector2(0.55f, 0.5f));
            go.AddComponent<BossSupportMarker>();
            RegisterEnemy(go, rider: false, elite: false, boss: false, countTowardWave: false);
            return go;
        }

        public int CountSupportAlive()
        {
            int n = 0;
            if (_enemyRoot == null) return 0;
            for (int i = 0; i < _enemyRoot.childCount; i++)
            {
                var m = _enemyRoot.GetChild(i).GetComponent<BossSupportMarker>();
                if (m == null) continue;
                var h = m.GetComponent<Health>();
                if (h != null && !h.IsDead) n++;
            }
            return n;
        }

        public void DespawnSupportAdds()
        {
            if (_enemyRoot == null) return;
            for (int i = _enemyRoot.childCount - 1; i >= 0; i--)
            {
                var child = _enemyRoot.GetChild(i);
                if (child.GetComponent<BossSupportMarker>() == null) continue;
                var h = child.GetComponent<Health>();
                if (h != null)
                {
                    h.Died -= OnEnemyDied;
                    _alive.Remove(h);
                }
                Destroy(child.gameObject);
            }
            AliveChanged?.Invoke();
        }

        GameObject CreateBoss(Vector2 pos)
        {
            if (_enemyRoot == null)
                _enemyRoot = new GameObject("Enemies").transform;

            var go = new GameObject("GobriderWarlord");
            go.transform.SetParent(_enemyRoot, false);
            go.transform.position = pos;
            go.transform.localScale = new Vector3(1.28f, 1.28f, 1f);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = gobriderSprite;
            sr.color = new Color(0.72f, 0.28f, 0.22f, 1f);
            sr.sortingOrder = 25;

            var sorter = go.AddComponent<SpriteSorter>();
            sorter.baseOrder = 40;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 0.8f);

            var health = go.AddComponent<Health>();
            health.Configure(400, true);

            var boss = go.AddComponent<BossWarlordController>();
            boss.Configure(this, BossWarlordController.BossDisplayName, 400);

            go.AddComponent<EnemyWorldUI>();
            // Temporary Gobrider body — dedicated Warlord art is listed as missing.
            var visual = go.AddComponent<EnemyVisual>();
            visual.Init(gobriderSprite, gobriderWalkSprite != null ? gobriderWalkSprite : gobriderSprite);
            return go;
        }

        void RegisterEnemy(GameObject go, bool rider, bool elite = false, bool boss = false, bool countTowardWave = true)
        {
            var health = go.GetComponent<Health>();
            if (health == null) return;
            _alive.Add(health);
            if (countTowardWave)
            {
                _waveSpawned++;
                if (boss)
                    _bossesSpawned++;
                else if (rider)
                {
                    _ridersSpawned++;
                    if (elite) _elitesSpawned++;
                }
                else _goblinsSpawned++;
            }
            health.Died += OnEnemyDied;
            AliveChanged?.Invoke();
        }

        void OnEnemyDied(Health health)
        {
            if (health == null) return;
            health.Died -= OnEnemyDied;
            _alive.Remove(health);
            if (!_countedDeaths.Add(health))
            {
                AliveChanged?.Invoke();
                return;
            }

            _sessionKills++;
            bool support = health.GetComponent<BossSupportMarker>() != null;
            if (_waveActive && !support)
                _waveKills++;
            EnemyKilled?.Invoke(_sessionKills);
            AliveChanged?.Invoke();

            if (WaveCleared)
            {
                _waveActive = false;
                WaveClearedEvent?.Invoke();
            }
        }

        GameObject CreateEnemy(Vector2 pos, EnemyKind kind, string label, int hp, float speed,
            float aggro, float range, int damage, float cooldown, Sprite sprite, Color tint, Vector2 colliderSize)
        {
            if (_enemyRoot == null)
                _enemyRoot = new GameObject("Enemies").transform;

            var go = new GameObject(kind.ToString());
            go.transform.SetParent(_enemyRoot, false);
            go.transform.position = pos;
            go.tag = "Untagged";
            go.layer = LayerMask.NameToLayer("Default");

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = sprite == null ? tint : Color.white;
            sr.sortingOrder = 25;

            var sorter = go.AddComponent<SpriteSorter>();
            sorter.baseOrder = 25;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = colliderSize;

            var health = go.AddComponent<Health>();
            health.Configure(hp, true);

            var enemy = go.AddComponent<EnemyController>();
            enemy.Setup(kind, label, hp, speed, aggro, range, damage, cooldown);

            var visual = go.AddComponent<EnemyVisual>();
            visual.Init(sprite, kind == EnemyKind.Goblin ? goblinWalkSprite : gobriderWalkSprite);
            var animator = kind == EnemyKind.Goblin ? goblinAnimator : gobriderAnimator;
            if (animator != null)
            {
                visual.UseAnimator(animator, kind == EnemyKind.Gobrider);
                col.offset = new Vector2(0f, colliderSize.y * 0.5f);
            }

            go.AddComponent<EnemyWorldUI>();
            return go;
        }
    }
}
