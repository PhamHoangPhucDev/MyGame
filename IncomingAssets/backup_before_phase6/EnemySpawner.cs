using System.Collections.Generic;
using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Spawns Goblin / Gobrider groups around camp tents for continuous combat.
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [SerializeField] Transform player;
        [SerializeField] int maxAlive = 6;
        [SerializeField] float respawnCheckInterval = 6f;
        [SerializeField] float minSpawnDistanceFromPlayer = 5.5f;
        [SerializeField] Sprite goblinSprite;
        [SerializeField] Sprite gobriderSprite;
        [SerializeField] Sprite goblinWalkSprite;
        [SerializeField] Sprite gobriderWalkSprite;
        [SerializeField] RuntimeAnimatorController goblinAnimator;
        [SerializeField] RuntimeAnimatorController gobriderAnimator;

        readonly List<Vector2> _campPoints = new();
        float _timer;
        Transform _enemyRoot;

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
                // Separate groups: rider and a goblin by the tents, two goblins on the approach.
                _campPoints.Add(new Vector2(2.55f, 4.45f));
                _campPoints.Add(new Vector2(-2.35f, 2.55f));
                _campPoints.Add(new Vector2(-1.85f, 0.15f));
                _campPoints.Add(new Vector2(1.85f, 1.05f));
            }

            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }

            SpawnWave(initial: true);
        }

        void Update()
        {
            _timer -= Time.deltaTime;
            if (_timer > 0f) return;
            _timer = respawnCheckInterval;

            if (player != null)
            {
                var playerHealth = player.GetComponent<Health>();
                if (playerHealth != null && playerHealth.IsDead)
                    return;
            }

            int alive = CountAlive();
            if (alive < maxAlive)
                SpawnWave(initial: false, budget: 1);
        }

        int CountAlive()
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

        void SpawnWave(bool initial, int budget = -1)
        {
            if (budget < 0)
                budget = initial ? 4 : 1;

            for (int i = 0; i < budget; i++)
            {
                if (CountAlive() >= maxAlive) break;

                Vector2 camp = initial
                    ? _campPoints[i % _campPoints.Count]
                    : _campPoints[Random.Range(0, _campPoints.Count)];
                if (!TryPickSpawn(camp, out Vector2 pos))
                    continue;

                bool rider = initial ? i == 0 : Random.value < 0.18f;
                if (rider)
                    SpawnGobrider(pos);
                else
                    SpawnGoblin(pos);
            }
        }

        bool TryPickSpawn(Vector2 camp, out Vector2 pos)
        {
            pos = camp;
            for (int attempt = 0; attempt < 8; attempt++)
            {
                Vector2 candidate = camp + Random.insideUnitCircle * 0.4f;
                if (player != null && Vector2.Distance(candidate, player.position) < minSpawnDistanceFromPlayer)
                    continue;
                if (Physics2D.OverlapCircle(candidate, 0.32f) != null)
                    continue;
                pos = candidate;
                return true;
            }
            return false;
        }

        public GameObject SpawnGoblin(Vector2 pos)
        {
            return CreateEnemy(pos, EnemyKind.Goblin, "Goblin Lv.3", 36, 1.85f, 5.0f, 0.75f, 4, 1.5f,
                goblinSprite, new Color(0.35f, 0.75f, 0.28f), new Vector2(0.45f, 0.5f));
        }

        public GameObject SpawnGobrider(Vector2 pos)
        {
            return CreateEnemy(pos, EnemyKind.Gobrider, "Gobrider Lv.5", 80, 1.45f, 5.6f, 0.95f, 8, 1.7f,
                gobriderSprite, new Color(0.25f, 0.55f, 0.22f), new Vector2(0.7f, 0.65f));
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
            if (sr.sprite == null)
                sr.color = tint;
            else
                sr.color = Color.white;
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
