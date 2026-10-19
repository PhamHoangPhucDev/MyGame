using System;
using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Single authority for MainMenu → Playing → Waves → Victory/GameOver.
    /// Reuses EnemySpawner, GameOver survival tuning, and existing combat systems.
    /// </summary>
    public class LevelFlow : MonoBehaviour
    {
        public static LevelFlow Instance { get; private set; }

        const int HeroMaxHp = 400;
        const float HeroInvuln = 0.50f;

        [SerializeField] LevelConfig level1;
        [SerializeField] LevelConfig level2;
        [SerializeField] EnemySpawner spawner;
        [SerializeField] Health playerHealth;
        [SerializeField] PlayerMovement movement;
        [SerializeField] PlayerCombat combat;
        [SerializeField] HitComboCounter combo;
        [SerializeField] LevelFlowUI ui;
        [SerializeField] Transform player;
        [SerializeField] bool startAtMainMenu = true;

        LevelState _state = LevelState.MainMenu;
        LevelState _stateBeforePause = LevelState.Playing;
        LevelConfig _activeLevel;
        int _waveIndex = -1;
        float _transitionTimer;
        float _runTimer;
        float _bannerTimer;
        bool _ending;
        Vector2 _defaultSpawn = new Vector2(0.2f, -6.2f);

        public LevelState State => _state;
        public LevelConfig ActiveLevel => _activeLevel;
        public int WaveNumber => _waveIndex + 1;
        public int WaveCount => _activeLevel != null && _activeLevel.waves != null ? _activeLevel.waves.Length : 0;
        public int Kills => spawner != null ? spawner.KillCount : 0;
        public int BestCombo => combo != null ? combo.BestHits : 0;
        public float RunTime => _runTimer;
        public string BannerText { get; private set; }
        public float BannerNormalized => _bannerTimer <= 0f ? 0f : Mathf.Clamp01(_bannerTimer / 1.4f);

        public event Action<LevelState> StateChanged;
        public event Action HudChanged;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() => Instance = null;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            Unbind();
        }

        void Start()
        {
            ResolveRefs();
            EnsureDefaultConfigs();
            if (ui == null)
                ui = GetComponent<LevelFlowUI>() ?? gameObject.AddComponent<LevelFlowUI>();
            ui.Bind(this);

            if (playerHealth != null)
            {
                playerHealth.Configure(HeroMaxHp, true);
                playerHealth.SetSurvival(HeroInvuln, blink: true);
            }

            if (startAtMainMenu)
                EnterMainMenu();
            else
                StartLevel(level1 != null ? level1 : CreateRuntimeLevel1(), fromMenu: false);
        }

        void ResolveRefs()
        {
            if (player == null)
            {
                var p = GameObject.FindGameObjectWithTag("Player");
                if (p != null) player = p.transform;
            }
            if (player != null)
            {
                if (playerHealth == null) playerHealth = player.GetComponent<Health>();
                if (movement == null) movement = player.GetComponent<PlayerMovement>();
                if (combat == null) combat = player.GetComponent<PlayerCombat>();
                _defaultSpawn = player.position;
            }
            if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
            if (combo == null) combo = FindFirstObjectByType<HitComboCounter>();
        }

        void EnsureDefaultConfigs()
        {
            if (level1 == null) level1 = CreateRuntimeLevel1();
            if (level2 == null) level2 = CreateRuntimeLevel2();
        }

        public static LevelConfig CreateRuntimeLevel1()
        {
            var cfg = ScriptableObject.CreateInstance<LevelConfig>();
            cfg.levelNumber = 1;
            cfg.levelTitle = "GOBLIN FOREST CAMP";
            cfg.heroSpawn = new Vector2(0.2f, -6.2f);
            cfg.waves = new[]
            {
                new WaveConfig { displayName = "WAVE 1 — FIRST CONTACT", goblins = 3, gobriders = 0, aliveCap = 3, transitionDelay = 1.75f },
                new WaveConfig { displayName = "WAVE 2 — CAMP DEFENSE", goblins = 5, gobriders = 0, aliveCap = 5, transitionDelay = 1.75f },
                new WaveConfig { displayName = "WAVE 3 — GOBRIDER AMBUSH", goblins = 4, gobriders = 1, aliveCap = 5, transitionDelay = 2f },
                new WaveConfig { displayName = "WAVE 4 — FINAL ASSAULT", goblins = 7, gobriders = 2, aliveCap = 6, transitionDelay = 1.75f },
            };
            return cfg;
        }

        public static LevelConfig CreateRuntimeLevel2()
        {
            var cfg = ScriptableObject.CreateInstance<LevelConfig>();
            cfg.levelNumber = 2;
            cfg.levelTitle = "GOBLIN CAMP — HARDER";
            cfg.heroSpawn = new Vector2(0.2f, -6.2f);
            cfg.waves = new[]
            {
                new WaveConfig { displayName = "WAVE 1", goblins = 4, gobriders = 0, aliveCap = 4, transitionDelay = 1.6f },
                new WaveConfig { displayName = "WAVE 2", goblins = 5, gobriders = 1, aliveCap = 5, transitionDelay = 1.75f },
                new WaveConfig { displayName = "WAVE 3", goblins = 6, gobriders = 1, aliveCap = 5, transitionDelay = 1.75f },
                new WaveConfig { displayName = "WAVE 4", goblins = 8, gobriders = 2, aliveCap = 6, transitionDelay = 1.75f },
            };
            return cfg;
        }

        void Update()
        {
            if (_state == LevelState.Playing || _state == LevelState.WaveTransition)
                _runTimer += Time.unscaledDeltaTime;

            if (_bannerTimer > 0f)
            {
                _bannerTimer -= Time.unscaledDeltaTime;
                if (_bannerTimer <= 0f)
                    BannerText = null;
                HudChanged?.Invoke();
            }

            if (_state == LevelState.WaveTransition)
            {
                _transitionTimer -= Time.unscaledDeltaTime;
                if (_transitionTimer <= 0f)
                    AdvanceToNextWave();
            }

            if (_state == LevelState.Playing && UnityEngine.InputSystem.Keyboard.current != null &&
                UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
                TogglePause();
        }

        void BindSpawner()
        {
            if (spawner == null) return;
            spawner.WaveClearedEvent -= OnWaveCleared;
            spawner.EnemyKilled -= OnEnemyKilled;
            spawner.WaveClearedEvent += OnWaveCleared;
            spawner.EnemyKilled += OnEnemyKilled;
        }

        void Unbind()
        {
            if (spawner != null)
            {
                spawner.WaveClearedEvent -= OnWaveCleared;
                spawner.EnemyKilled -= OnEnemyKilled;
            }
            if (playerHealth != null)
                playerHealth.Died -= OnHeroDied;
        }

        void SetState(LevelState next)
        {
            if (_state == next) return;
            _state = next;
            StateChanged?.Invoke(_state);
            HudChanged?.Invoke();
        }

        public void EnterMainMenu()
        {
            _ending = false;
            Unbind();
            RestoreTime();
            if (spawner != null)
            {
                spawner.ResetSession();
                spawner.SetSpawningEnabled(false);
            }
            LockGameplay(true);
            SetState(LevelState.MainMenu);
            ui?.ShowMainMenu();
        }

        public void PlayFromMenu(bool continueProgress)
        {
            EnsureDefaultConfigs();
            int unlock = GameProgress.HighestUnlockedLevel;
            LevelConfig cfg = level1;
            if (continueProgress && unlock >= 2 && level2 != null)
                cfg = level2;
            StartLevel(cfg, fromMenu: true);
        }

        public void StartLevel(LevelConfig config, bool fromMenu)
        {
            if (config == null) return;
            SetState(LevelState.Loading);
            _ending = false;
            _activeLevel = config;
            _waveIndex = -1;
            _runTimer = 0f;
            BannerText = null;

            ResolveRefs();
            RestoreTime();
            BindSpawner();

            if (playerHealth != null)
            {
                playerHealth.Died -= OnHeroDied;
                playerHealth.Died += OnHeroDied;
                playerHealth.Configure(HeroMaxHp, true);
                playerHealth.SetSurvival(HeroInvuln, blink: true);
                playerHealth.ReviveFull();
            }

            combat?.ResetCombatState();
            combo?.ResetRun();
            spawner?.ResetSession();
            EnemyController.ClearAttackSlots();

            Vector2 spawn = config.heroSpawn.sqrMagnitude > 0.01f ? config.heroSpawn : _defaultSpawn;
            if (player != null)
            {
                player.position = spawn;
                var rb = player.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }

            ui?.HideAllPopups();
            SetState(LevelState.Preparing);
            LockGameplay(false);
            ShowBanner($"LEVEL {config.levelNumber}\n{config.levelTitle}", 1.6f);
            SetState(LevelState.Playing);
            BeginWave(0);
            HudChanged?.Invoke();
        }

        void BeginWave(int index)
        {
            if (_activeLevel == null || _activeLevel.waves == null || index < 0 || index >= _activeLevel.waves.Length)
                return;
            _waveIndex = index;
            var wave = _activeLevel.waves[index];
            ShowBanner(wave.displayName, 1.35f);
            spawner?.BeginWave(index, wave);
            SetState(LevelState.Playing);
            HudChanged?.Invoke();
        }

        void OnWaveCleared()
        {
            if (_ending) return;
            if (_state != LevelState.Playing) return;
            ShowBanner("WAVE CLEAR", 1.1f);
            SetState(LevelState.WaveTransition);
            float delay = 1.75f;
            if (_activeLevel != null && _waveIndex >= 0 && _waveIndex < _activeLevel.waves.Length)
                delay = _activeLevel.waves[_waveIndex].transitionDelay;
            _transitionTimer = delay;
            spawner?.SetSpawningEnabled(false);
        }

        void AdvanceToNextWave()
        {
            if (_activeLevel == null) return;
            int next = _waveIndex + 1;
            if (next >= _activeLevel.waves.Length)
            {
                TriggerVictory();
                return;
            }
            BeginWave(next);
        }

        void OnEnemyKilled(int _)
        {
            HudChanged?.Invoke();
        }

        void OnHeroDied(Health _)
        {
            if (_ending) return;
            if (_state == LevelState.Victory || _state == LevelState.GameOver) return;
            _ending = true;
            spawner?.StopWaves();
            LockGameplay(true);
            if (HitStop.Instance != null)
                HitStop.Instance.SetBlocked(true);
            SetState(LevelState.GameOver);
            ui?.ShowGameOver(BuildStats());
        }

        void TriggerVictory()
        {
            if (_ending) return;
            _ending = true;
            spawner?.StopWaves();
            LockGameplay(true);
            if (HitStop.Instance != null)
                HitStop.Instance.CancelAndRestore();

            int score = ComputeScore();
            GameProgress.RecordRun(_activeLevel.levelNumber, score, BestCombo);
            GameProgress.UnlockLevel(_activeLevel.levelNumber + 1);

            SetState(LevelState.Victory);
            ui?.ShowVictory(BuildStats(), score, StarsFor(score));
        }

        public int ComputeScore()
        {
            int kills = Kills;
            int comboBonus = BestCombo * 10;
            int timeBonus = Mathf.Max(0, 180 - Mathf.RoundToInt(_runTimer)) * 2;
            return kills * 25 + comboBonus + timeBonus;
        }

        public int StarsFor(int score)
        {
            if (score >= 900) return 3;
            if (score >= 550) return 2;
            return 1;
        }

        public LevelRunStats BuildStats()
        {
            return new LevelRunStats
            {
                levelNumber = _activeLevel != null ? _activeLevel.levelNumber : 1,
                levelTitle = _activeLevel != null ? _activeLevel.levelTitle : "",
                waveReached = Mathf.Max(1, WaveNumber),
                waveTotal = WaveCount,
                kills = Kills,
                bestCombo = BestCombo,
                seconds = _runTimer
            };
        }

        public void Retry()
        {
            if (_activeLevel == null) _activeLevel = level1;
            StartLevel(_activeLevel, fromMenu: false);
        }

        public void NextLevel()
        {
            EnsureDefaultConfigs();
            LevelConfig next = level2;
            if (_activeLevel != null && _activeLevel.levelNumber >= 2)
                next = level2; // remain on hardest available config
            StartLevel(next != null ? next : CreateRuntimeLevel2(), fromMenu: false);
        }

        public void Replay() => Retry();

        public void BackToMenu() => EnterMainMenu();

        public void TogglePause()
        {
            if (_state == LevelState.Paused)
            {
                Resume();
                return;
            }
            if (_state != LevelState.Playing && _state != LevelState.WaveTransition) return;
            _stateBeforePause = _state;
            if (HitStop.Instance != null)
                HitStop.Instance.SetBlocked(true);
            Time.timeScale = 0f;
            SetState(LevelState.Paused);
            ui?.ShowPause();
        }

        public void Resume()
        {
            if (_state != LevelState.Paused) return;
            if (HitStop.Instance != null)
                HitStop.Instance.SetBlocked(false);
            Time.timeScale = 1f;
            SetState(_stateBeforePause);
            ui?.HidePause();
        }

        void LockGameplay(bool locked)
        {
            if (movement != null) movement.SetMovementLocked(locked);
            if (combat != null) combat.enabled = !locked;
        }

        void RestoreTime()
        {
            if (HitStop.Instance != null)
            {
                HitStop.Instance.CancelAndRestore();
                HitStop.Instance.SetBlocked(false);
            }
            Time.timeScale = 1f;
        }

        void ShowBanner(string text, float seconds)
        {
            BannerText = text;
            _bannerTimer = seconds;
            HudChanged?.Invoke();
        }

        // Debug helpers for MCP tests
        public void DebugForceClearWave()
        {
            if (_state != LevelState.Playing) return;
            spawner?.ClearAllEnemies();
            OnWaveCleared();
        }

        public void DebugKillHero()
        {
            if (playerHealth == null || playerHealth.IsDead) return;
            playerHealth.TakeDamage(9999, playerHealth.transform.position, Vector2.zero);
        }
    }

    public struct LevelRunStats
    {
        public int levelNumber;
        public string levelTitle;
        public int waveReached;
        public int waveTotal;
        public int kills;
        public int bestCombo;
        public float seconds;
    }
}
