using UnityEngine;
using UnityEngine.InputSystem;

namespace GameplayPrototype
{
    /// <summary>
    /// Isolated combat sandbox: reuses PlayerCombat + EnemySpawner, never writes GameProgress
    /// and never drives LevelFlow waves/unlocks.
    /// </summary>
    public class ReferenceSandboxController : MonoBehaviour
    {
        public static bool IsActive { get; private set; }

        [SerializeField] EnemySpawner spawner;
        [SerializeField] Transform player;
        [SerializeField] Health playerHealth;
        [SerializeField] PlayerCombat combat;
        [SerializeField] HitComboCounter combo;
        [SerializeField] SkillHotbarUI hotbar;
        [SerializeField] AspectProfileService aspectService;
        [Tooltip("Off (default): profile follows the screen (portrait when height > width). On: always start with startProfile.")]
        [SerializeField] bool forceStartProfile = false;
        [SerializeField] AspectProfileId startProfile = AspectProfileId.Landscape;
        [SerializeField] Vector2 heroSpawn = new(0f, -1.5f);
        [SerializeField] int goblinCount = 5;
        [SerializeField] int gobriderCount = 1;
        [SerializeField] Vector2[] goblinSlots =
        {
            new(-2.2f, 1.2f), new(2.0f, 1.0f), new(-1.4f, 2.6f), new(1.6f, 2.8f), new(0.1f, 3.6f)
        };
        [SerializeField] Vector2 gobriderSlot = new(0.2f, 5.0f);

        const int HeroMaxHp = 400;
        const float HeroInvuln = 0.50f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() => IsActive = false;

        void Awake()
        {
            IsActive = true;
            ResolveRefs();
            DisableLevelFlowProgress();
        }

        void OnDestroy()
        {
            if (IsActive) IsActive = false;
        }

        void Start()
        {
            ResolveRefs();
            if (player != null)
                player.position = new Vector3(heroSpawn.x, heroSpawn.y, 0f);

            if (playerHealth != null)
            {
                playerHealth.Configure(HeroMaxHp, true);
                playerHealth.SetSurvival(HeroInvuln, blink: true);
            }

            if (hotbar != null && combat != null)
                hotbar.Bind(combat, playerHealth, combo);

            if (aspectService != null)
            {
                if (forceStartProfile) aspectService.Apply(startProfile);
                else aspectService.ApplyDetected();
            }

            var cam = Camera.main;
            if (cam != null)
            {
                var follow = cam.GetComponent<CameraFollow>();
                if (follow != null && player != null)
                    follow.SetTarget(player);
                ClampCameraToGround(follow);
            }

            SpawnMobs();
            Debug.Log("[Sandbox] ReferenceCombatSandbox ready — progress writes suppressed.");
        }

        /// <summary>R3D.1: keep Portrait framing inside Ground tiles so Camera clear-color void never shows.</summary>
        static void ClampCameraToGround(CameraFollow follow)
        {
            if (follow == null) return;
            var ground = GameObject.Find("Ground")?.GetComponent<UnityEngine.Tilemaps.Tilemap>();
            if (ground == null) return;
            var b = ground.localBounds;
            var t = ground.transform;
            Vector3 min = t.TransformPoint(b.min);
            Vector3 max = t.TransformPoint(b.max);
            // Inset slightly so edge tiles stay in frame.
            follow.SetBounds(new Vector2(min.x + 0.5f, min.y + 0.5f), new Vector2(max.x - 0.5f, max.y - 0.5f));
        }

        void ResolveRefs()
        {
            if (player == null)
            {
                var p = GameObject.Find("Player");
                if (p != null) player = p.transform;
            }
            if (playerHealth == null && player != null)
                playerHealth = player.GetComponent<Health>();
            if (combat == null && player != null)
                combat = player.GetComponent<PlayerCombat>();
            if (spawner == null)
                spawner = FindFirstObjectByType<EnemySpawner>();
            if (combo == null)
                combo = FindFirstObjectByType<HitComboCounter>();
            if (hotbar == null)
                hotbar = FindFirstObjectByType<SkillHotbarUI>();
            if (aspectService == null)
                aspectService = FindFirstObjectByType<AspectProfileService>();
        }

        void DisableLevelFlowProgress()
        {
            var flow = FindFirstObjectByType<LevelFlow>();
            if (flow != null)
            {
                flow.enabled = false;
                var ui = flow.GetComponent<LevelFlowUI>();
                if (ui != null) ui.enabled = false;
            }
            var go = FindFirstObjectByType<GameOverController>();
            if (go != null) go.enabled = false;
            var map = FindFirstObjectByType<LevelMapController>();
            if (map != null) map.enabled = false;
        }

        void SpawnMobs()
        {
            if (spawner == null) return;
            spawner.SetSpawningEnabled(false);
            spawner.SetSpawnZones(new[] { Vector2.zero });

            var roster = Resources.Load<R2CMonsterRoster>("R2C/R2CMonsterRoster");
            if (roster != null && roster.monsters != null && roster.monsters.Length > 0)
            {
                for (int i = 0; i < roster.monsters.Length; i++)
                {
                    float x = (i - (roster.monsters.Length - 1) * 0.5f) * 2.1f;
                    spawner.SpawnR2C(roster.monsters[i], new Vector2(x, 2.4f));
                }
                return;
            }

            int n = Mathf.Min(goblinCount, goblinSlots.Length);
            for (int i = 0; i < n; i++)
                spawner.SpawnGoblin(goblinSlots[i]);

            for (int i = 0; i < gobriderCount; i++)
                spawner.SpawnGobrider(gobriderSlot + new Vector2(i * 1.5f, 0f));
        }

        void Update()
        {
            // F8 toggles aspect in Play Mode for dual-aspect QA (Editor + device).
            var kb = Keyboard.current;
            if (kb != null && kb.f8Key.wasPressedThisFrame && aspectService != null)
                aspectService.Toggle();
        }
    }
}
