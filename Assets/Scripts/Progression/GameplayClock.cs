using System;
using System.Globalization;
using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>Rejection codes shared by every progression request. UI reads <see cref="Outcome.Last"/>.</summary>
    public enum Deny
    {
        None = 0,
        Invalid,
        InCombat,
        Lava,
        Dead,
        BagFull,
        InsufficientResources,
        NotOwned,
        NotEligible,
        LegacyUnverified,
        AlreadyDone,
        Locked,
        MaxRank,
        EffectActive,
        OutOfRange,
        Equipped,
        LimitReached
    }

    public static class Outcome
    {
        public static Deny Last = Deny.None;
    }

    /// <summary>
    /// Active gameplay time and the shared OutOfCombat gate.
    /// The clock only advances while gameplay runs: Pause (timeScale 0), menus and closed-app time do not count.
    /// The clock value and the last combat/Lava marks persist, so the remaining wait survives a reload (GL-001).
    /// </summary>
    [DefaultExecutionOrder(-50)]
    public class GameplayClock : MonoBehaviour
    {
        public const float SafeSeconds = 5f;
        const string ClockKey = WorldCatalog.SavePrefix + "clock.active";
        const string CombatKey = WorldCatalog.SavePrefix + "ooc.combat";
        const string LavaKey = WorldCatalog.SavePrefix + "ooc.lava";

        public static GameplayClock Instance { get; private set; }

        /// <summary>Active gameplay seconds since the save began.</summary>
        public static double Now { get; private set; }
        public static float LastDelta { get; private set; }
        public static bool Running { get; private set; }
        public static bool InLava { get; private set; }
        public static event Action Flushing;

        static double _lastCombat;
        static double _lastLava = double.NegativeInfinity;
        static bool _loaded;

        Health _heroHealth;
        PlayerCombat _heroCombat;
        float _heroSearch;
        float _enemyScan;
        bool _enemyAggro;
        float _flushTimer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Instance = null;
            _loaded = false;
            Running = false;
            InLava = false;
            Flushing = null;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Boot()
        {
            if (Instance != null) return;
            var go = new GameObject("GameplayClock");
            DontDestroyOnLoad(go);
            go.AddComponent<GameplayClock>();
        }

        /// <summary>Loads the persisted clock. Safe to call more than once and from edit-mode tests.</summary>
        public static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            WorldTx.Recover();
            Now = ReadDouble(ClockKey, 0d);
            // Legacy save without a mark: wait the safe 5 seconds once.
            _lastCombat = PlayerPrefs.HasKey(CombatKey) ? ReadDouble(CombatKey, Now) : Now;
            _lastLava = PlayerPrefs.HasKey(LavaKey) ? ReadDouble(LavaKey, double.NegativeInfinity) : double.NegativeInfinity;
            if (_lastCombat > Now) _lastCombat = Now;
            if (_lastLava > Now) _lastLava = Now;
            ProgressionMigration.Run();
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            EnsureLoaded();
        }

        void OnDestroy()
        {
            if (Instance != this) return;
            Flush(true);
            Instance = null;
        }

        void OnApplicationPause(bool paused)
        {
            if (paused) Flush(true);
        }

        void OnApplicationQuit() => Flush(true);

        void Update()
        {
            FindHero();
            Running = Time.timeScale > 0f && InGameplay();
            LastDelta = Running ? Time.deltaTime : 0f;
            Now += LastDelta;
            if (!Running) return;

            _enemyScan -= Time.unscaledDeltaTime;
            if (_enemyScan <= 0f)
            {
                _enemyScan = 0.25f;
                _enemyAggro = AnyEnemyAggro();
            }
            if (EncounterActive() || _enemyAggro) _lastCombat = Now;

            _flushTimer -= Time.unscaledDeltaTime;
            if (_flushTimer <= 0f)
            {
                _flushTimer = 2f;
                Flush(false);
            }
        }

        /// <summary>Attack, cast, or damage. Restarts the 5-second wait.</summary>
        public static void MarkCombat() => _lastCombat = Now;

        /// <summary>Called every frame the Hero overlaps Lava.</summary>
        public static void SetLava(bool inside)
        {
            InLava = inside;
            if (inside) _lastLava = Now;
        }

        public static float CombatWaitLeft => Mathf.Max(0f, (float)(SafeSeconds - (Now - _lastCombat)));
        public static float LavaWaitLeft => InLava ? SafeSeconds : Mathf.Max(0f, (float)(SafeSeconds - (Now - _lastLava)));

        /// <summary>Shared gate for Equip, Gems, Skill Tree, Class Switch, Housing Build and Rest.</summary>
        public static Deny CheckOutOfCombat()
        {
            var inst = Instance;
            if (inst != null)
            {
                inst.FindHero();
                if (inst._heroHealth != null && inst._heroHealth.IsDead) return Deny.Dead;
                if (inst.EncounterActive() || inst._enemyAggro) return Deny.InCombat;
                if (inst._heroCombat != null && inst._heroCombat.IsActing) return Deny.InCombat;
            }
            if (InLava || LavaWaitLeft > 0f) return Deny.Lava;
            if (CombatWaitLeft > 0f) return Deny.InCombat;
            return Deny.None;
        }

        public static string DenyText(Deny reason)
        {
            switch (reason)
            {
                case Deny.Dead: return "Hero is down";
                case Deny.InCombat: return "In combat (" + Mathf.CeilToInt(CombatWaitLeft) + "s)";
                case Deny.Lava: return "Too close to lava (" + Mathf.CeilToInt(LavaWaitLeft) + "s)";
                default: return reason.ToString();
            }
        }

        /// <summary>Gate only applies where the campaign or village runs; the reference sandbox stays free.</summary>
        public static bool GateApplies => LevelFlow.Instance != null || WorldNavigator.Instance != null;

        static bool InGameplay()
        {
            var nav = WorldNavigator.Instance;
            if (nav != null && nav.Current != WorldZone.None) return true;
            var flow = LevelFlow.Instance;
            if (flow != null)
                return flow.State == LevelState.Playing || flow.State == LevelState.WaveTransition || flow.State == LevelState.Preparing;
            return true;
        }

        bool EncounterActive()
        {
            var nav = WorldNavigator.Instance;
            var flow = LevelFlow.Instance;
            bool inWorld = nav != null && nav.Current != WorldZone.None;
            if (!inWorld && flow != null)
            {
                var s = flow.State;
                if (s == LevelState.Playing || s == LevelState.WaveTransition || s == LevelState.Preparing || s == LevelState.Paused)
                    return true;
            }
            if (inWorld && nav.Current == WorldZone.Boss)
            {
                var boss = FindFirstObjectByType<BossArea>();
                if (boss != null && boss.isActiveAndEnabled && boss.CombatConnected && !boss.Defeated) return true;
            }
            return false;
        }

        static bool AnyEnemyAggro()
        {
            var enemies = FindObjectsByType<EnemyController>(FindObjectsSortMode.None);
            for (int i = 0; i < enemies.Length; i++)
            {
                var e = enemies[i];
                if (e == null || !e.isActiveAndEnabled) continue;
                switch (e.State)
                {
                    case EnemyAiState.Chase:
                    case EnemyAiState.Approach:
                    case EnemyAiState.Attack:
                    case EnemyAiState.Recovery:
                    case EnemyAiState.ChargeTelegraph:
                    case EnemyAiState.ChargeActive:
                    case EnemyAiState.ChargeRecovery:
                        return true;
                }
            }
            return false;
        }

        void FindHero()
        {
            if (_heroHealth != null) return;
            _heroSearch -= Time.unscaledDeltaTime;
            if (_heroSearch > 0f) return;
            _heroSearch = 0.5f;
            var hero = GameObject.FindGameObjectWithTag("Player");
            if (hero == null) return;
            _heroHealth = hero.GetComponent<Health>();
            _heroCombat = hero.GetComponent<PlayerCombat>();
            if (_heroHealth != null)
            {
                _heroHealth.Damaged += (_, _, _) => MarkCombat();
                _heroHealth.ShieldHit += (_, _) => MarkCombat();
            }
            if (hero.GetComponent<HeroHazards>() == null) hero.AddComponent<HeroHazards>();
        }

        public static void Flush(bool toDisk)
        {
            if (!_loaded) return;
            PlayerPrefs.SetString(ClockKey, Now.ToString("R", CultureInfo.InvariantCulture));
            PlayerPrefs.SetString(CombatKey, _lastCombat.ToString("R", CultureInfo.InvariantCulture));
            if (!double.IsNegativeInfinity(_lastLava))
                PlayerPrefs.SetString(LavaKey, _lastLava.ToString("R", CultureInfo.InvariantCulture));
            Flushing?.Invoke();
            if (toDisk) PlayerPrefs.Save();
        }

        static double ReadDouble(string key, double fallback)
        {
            string raw = PlayerPrefs.GetString(key, "");
            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : fallback;
        }

        /// <summary>Test hook: advances active time without waiting.</summary>
        public static void DebugAdvance(float seconds)
        {
            if (seconds > 0f) Now += seconds;
        }
    }
}
