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
    /// Encounter-driven spawn around the goblin camp. Reuses one spawner (no parallel systems).
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] Transform player;
        [SerializeField] int maxAlive = 10;
        [SerializeField] float respawnCheckInterval = 3.5f;
        [SerializeField] float minSpawnDistanceFromPlayer = 5.5f;
        [SerializeField] bool preferOffCameraSpawns = true;
        [SerializeField] Sprite goblinSprite;
        [SerializeField] Sprite gobriderSprite;
        [SerializeField] Sprite goblinWalkSprite;
        [SerializeField] Sprite gobriderWalkSprite;
        [SerializeField] RuntimeAnimatorController goblinAnimator;
        [SerializeField] RuntimeAnimatorController gobriderAnimator;
        [SerializeField] float approachTriggerY = -1.2f;
        [SerializeField] float defenseTriggerY = 1.0f;
        [SerializeField] float ambushTriggerY = 2.8f;

        readonly List<Vector2> _campPoints = new();
        float _timer;
        Transform _enemyRoot;
        EncounterId _encounter = EncounterId.None;
        bool _approachSpawned;
        bool _defenseSpawned;
        bool _ambushSpawned;
        bool _climaxSpawned;
        int _kills;

        public EncounterId CurrentEncounter => _encounter;
        public int KillCount => _kills;
        public Transform EnemyRoot => _enemyRoot;

        public void Configure(Transform playerTransform, Sprite goblin, Sprite gobrider, IEnumerable<Vector2> camps)
        {
            player = playerTransform;
            goblinSprite = goblin;
            gobriderSprite = gobrider;
            _campPoints.Clear();
            foreach (var c in camps)
                _campPoints.Add(c);
        }

        void Awake()
        {
            _enemyRoot = new GameObject("Enemies").transform;
        }

        void Start()
        {
            if (_campPoints.Count == 0)
            {
                _campPoints.Add(new Vector2(2.55f, 4.45f));
                _campPoints.Add(new Vector2(-2.35f, 2.55f));
                _campPoints.Add(new Vector2(-1.85f, 0.15f));
                _campPoints.Add(new Vector2(1.85f, 1.05f));
                _campPoints.Add(new Vector2(0.2f, 3.2f));
                _campPoints.Add(new Vector2(-3.1f, 1.4f));
            }

            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }

        }

        void Update()
        {
            if (player != null)
            {
                var playerHealth = player.GetComponent<Health>();
                if (playerHealth != null && playerHealth.IsDead)
                    return;
            }

            EvaluateEncounterProgress();

            _timer -= Time.deltaTime;
            if (_timer > 0f) return;

            int alive = CountAlive();
            int cap = EncounterCap();
            if (alive >= cap || _encounter == EncounterId.Complete || _encounter == EncounterId.None)
            {
                _timer = respawnCheckInterval;
                return;
            }

            int missing = cap - alive;
            _timer = missing >= 3 ? 2.8f : 4.0f;
            SpawnWave(initial: false, budget: Mathf.Min(2, missing), preferRider: false);
        }

        void EvaluateEncounterProgress()
        {
            if (player == null) return;
            float y = player.position.y;
            int alive = CountAlive();

            if (!_approachSpawned && y >= approachTriggerY)
                BeginEncounter(EncounterId.CampApproach);

            alive = CountAlive();

            if (_encounter == EncounterId.CampApproach && (y >= defenseTriggerY || _kills >= 2))
                BeginEncounter(EncounterId.CampDefense);

            if (_encounter == EncounterId.CampDefense && (y >= ambushTriggerY || _kills >= 5))
                BeginEncounter(EncounterId.GobriderAmbush);

            if (_encounter == EncounterId.GobriderAmbush && (_kills >= 8 || (alive <= 2 && _ambushSpawned && _kills >= 6)))
                BeginEncounter(EncounterId.CombatClimax);

            if (_encounter == EncounterId.CombatClimax && _kills >= 14 && CountAlive() == 0)
                _encounter = EncounterId.Complete;
        }

        void BeginEncounter(EncounterId id)
        {
            if (_encounter == id && EncounterAlreadySeeded(id)) return;
            _encounter = id;
            switch (id)
            {
                case EncounterId.CampApproach:
                    if (_approachSpawned) return;
                    _approachSpawned = true;
                    maxAlive = 3;
                    SpawnAt(_campPoints[2], false);
                    SpawnAt(_campPoints[3], false);
                    if (_campPoints.Count > 4) SpawnAt(_campPoints[4], false);
                    break;
                case EncounterId.CampDefense:
                    if (_defenseSpawned) return;
                    _defenseSpawned = true;
                    maxAlive = 7;
                    SpawnWave(false, 3, false);
                    break;
                case EncounterId.GobriderAmbush:
                    if (_ambushSpawned) return;
                    _ambushSpawned = true;
                    maxAlive = 8;
                    SpawnAt(_campPoints[0], true);
                    SpawnWave(false, 2, false);
                    break;
                case EncounterId.CombatClimax:
                    if (_climaxSpawned) return;
                    _climaxSpawned = true;
                    maxAlive = 10;
                    SpawnWave(false, 3, false);
                    break;
            }
        }

        bool EncounterAlreadySeeded(EncounterId id) => id switch
        {
            EncounterId.CampApproach => _approachSpawned,
            EncounterId.CampDefense => _defenseSpawned,
            EncounterId.GobriderAmbush => _ambushSpawned,
            EncounterId.CombatClimax => _climaxSpawned,
            _ => true
        };

        int EncounterCap() => _encounter switch
        {
            EncounterId.CampApproach => 3,
            EncounterId.CampDefense => 7,
            EncounterId.GobriderAmbush => 8,
            EncounterId.CombatClimax => 10,
            EncounterId.Complete => 0,
            _ => maxAlive
        };

        public int CountAlive()
        {
            if (_enemyRoot == null) return 0;
            int n = 0;
            for (int i = 0; i < _enemyRoot.childCount; i++)
            {
                var h = _enemyRoot.GetChild(i).GetComponent<Health>();
                if (h != null && !h.IsDead) n++;
            }
            return n;
        }

        void SpawnWave(bool initial, int budget = -1, bool preferRider = false)
        {
            if (budget < 0)
                budget = initial ? 3 : 1;

            for (int i = 0; i < budget; i++)
            {
                if (CountAlive() >= EncounterCap()) break;
                Vector2 camp = _campPoints[Random.Range(0, _campPoints.Count)];
                if (!TryPickSpawn(camp, out Vector2 pos))
                    continue;
                bool rider = preferRider || (!initial && Random.value < 0.12f && _encounter >= EncounterId.GobriderAmbush);
                if (rider) SpawnGobrider(pos);
                else SpawnGoblin(pos);
            }
        }

        void SpawnAt(Vector2 camp, bool rider)
        {
            if (!TryPickSpawn(camp, out Vector2 pos))
                pos = camp + Random.insideUnitCircle * 0.3f;
            if (rider) SpawnGobrider(pos);
            else SpawnGoblin(pos);
        }

        bool TryPickSpawn(Vector2 camp, out Vector2 pos)
        {
            pos = camp;
            Vector2 best = camp;
            bool found = false;
            for (int attempt = 0; attempt < 12; attempt++)
            {
                Vector2 candidate = camp + Random.insideUnitCircle * 0.55f;
                if (player != null && Vector2.Distance(candidate, player.position) < minSpawnDistanceFromPlayer)
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

        public GameObject SpawnGoblin(Vector2 pos)
        {
            var go = CreateEnemy(pos, EnemyKind.Goblin, "Goblin Lv.3", 60, 2.05f, 5.2f, 0.75f, 4, 1.35f,
                goblinSprite, new Color(0.35f, 0.75f, 0.28f), new Vector2(0.45f, 0.5f));
            WireKill(go);
            return go;
        }

        public GameObject SpawnGobrider(Vector2 pos)
        {
            var go = CreateEnemy(pos, EnemyKind.Gobrider, "Gobrider Lv.5", 100, 1.55f, 5.8f, 0.95f, 8, 1.9f,
                gobriderSprite, new Color(0.25f, 0.55f, 0.22f), new Vector2(0.7f, 0.65f));
            WireKill(go);
            return go;
        }

        void WireKill(GameObject go)
        {
            var health = go.GetComponent<Health>();
            if (health == null) return;
            health.Died += _ => { _kills++; };
        }

        GameObject CreateEnemy(Vector2 pos, EnemyKind kind, string label, int hp, float speed,
            float aggro, float range, int damage, float cooldown, Sprite sprite, Color tint, Vector2 colliderSize)
        {
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
