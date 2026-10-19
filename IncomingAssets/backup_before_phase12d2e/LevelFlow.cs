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
        [SerializeField] LevelConfig level3;
        [SerializeField] LevelConfig level4;
        [SerializeField] LevelConfig[] levels;
        [SerializeField] EnemySpawner spawner;
        [SerializeField] Health playerHealth;
        [SerializeField] PlayerMovement movement;
        [SerializeField] PlayerCombat combat;
        [SerializeField] HitComboCounter combo;
        [SerializeField] LevelFlowUI ui;
        [SerializeField] LevelMapController mapController;
        [SerializeField] LevelExitGate exitGate;
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
        bool _wavesComplete;
        bool _exitUnlocked;
        Vector2 _defaultSpawn = new Vector2(0.2f, -6.2f);

        public LevelState State => _state;
        public LevelConfig ActiveLevel => _activeLevel;
        public LevelConfig[] AllLevels => levels;
        public int WaveNumber => _waveIndex + 1;
        public int WaveCount => _activeLevel != null && _activeLevel.waves != null ? _activeLevel.waves.Length : 0;
        public int Kills => spawner != null ? spawner.KillCount : 0;
        public int BestCombo => combo != null ? combo.BestHits : 0;
        public float RunTime => _runTimer;
        public bool ExitUnlocked => _exitUnlocked;
        public bool WavesComplete => _wavesComplete;
        public string ObjectiveText { get; private set; }
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
            if (level3 == null) level3 = CreateRuntimeLevel3();
            if (level4 == null) level4 = CreateRuntimeLevel4();
            if (levels == null || levels.Length < 4)
                levels = new[] { level1, level2, level3, level4 };
            else
            {
                if (levels.Length >= 1 && levels[0] == null) levels[0] = level1;
                if (levels.Length >= 2 && levels[1] == null) levels[1] = level2;
                if (levels.Length >= 3 && levels[2] == null) levels[2] = level3;
                if (levels.Length >= 4 && levels[3] == null) levels[3] = level4;
            }
            if (mapController == null)
                mapController = FindFirstObjectByType<LevelMapController>() ?? gameObject.AddComponent<LevelMapController>();
            if (exitGate == null)
                exitGate = FindFirstObjectByType<LevelExitGate>(FindObjectsInactive.Include);
        }

        public LevelConfig GetLevelConfig(int levelNumber)
        {
            EnsureDefaultConfigs();
            if (levels != null)
            {
                for (int i = 0; i < levels.Length; i++)
                    if (levels[i] != null && levels[i].levelNumber == levelNumber)
                        return levels[i];
            }
            if (levelNumber == 1) return level1;
            if (levelNumber == 2) return level2;
            if (levelNumber == 3) return level3;
            if (levelNumber == 4) return level4;
            return null;
        }

        public static LevelConfig CreateRuntimeLevel1()
        {
            var cfg = ScriptableObject.CreateInstance<LevelConfig>();
            cfg.levelNumber = 1;
            cfg.levelId = "level_01";
            cfg.levelTitle = "GOBLIN FOREST CAMP";
            cfg.mapLayoutId = "Camp";
            cfg.heroSpawn = new Vector2(0.2f, -6.2f);
            cfg.objectiveType = LevelObjectiveType.ClearWaves;
            cfg.targetCompletionTime = 150f;
            cfg.starHpRatio = 0.4f;
            cfg.recommendedMaxAlive = 6;
            cfg.nextLevelNumber = 2;
            cfg.spawnZones = new[]
            {
                new Vector2(2.55f, 4.45f), new Vector2(-2.35f, 2.55f), new Vector2(-1.85f, 0.15f),
                new Vector2(1.85f, 1.05f), new Vector2(0.2f, 3.2f), new Vector2(-3.1f, 1.4f)
            };
            cfg.cameraBoundsMin = new Vector2(-18f, -13f);
            cfg.cameraBoundsMax = new Vector2(18f, 15f);
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
            cfg.levelId = "level_02";
            cfg.levelTitle = "FOREST TRAIL";
            cfg.mapLayoutId = "Trail";
            cfg.heroSpawn = new Vector2(0.2f, -7.5f);
            cfg.objectiveType = LevelObjectiveType.ClearWavesThenReachExit;
            cfg.exitPosition = new Vector2(0.3f, 8.2f);
            cfg.exitSize = new Vector2(1.8f, 1.4f);
            cfg.targetCompletionTime = 140f;
            cfg.starHpRatio = 0.4f;
            cfg.recommendedMaxAlive = 6;
            cfg.nextLevelNumber = 3;
            cfg.spawnZones = new[]
            {
                new Vector2(-1.2f, -4.2f), new Vector2(1.6f, -2.4f), new Vector2(-1.8f, -0.2f),
                new Vector2(1.4f, 2.0f), new Vector2(-1.0f, 4.2f), new Vector2(1.2f, 6.0f)
            };
            cfg.cameraBoundsMin = new Vector2(-18f, -12f);
            cfg.cameraBoundsMax = new Vector2(18f, 14f);
            cfg.waves = new[]
            {
                new WaveConfig { displayName = "WAVE 1 — TRAIL SCOUTS", goblins = 3, gobriders = 0, aliveCap = 3, transitionDelay = 1.6f },
                new WaveConfig { displayName = "WAVE 2 — FLANK AMBUSH", goblins = 4, gobriders = 0, aliveCap = 4, transitionDelay = 1.75f },
                new WaveConfig { displayName = "WAVE 3 — RIDER PATROL", goblins = 3, gobriders = 1, aliveCap = 4, transitionDelay = 1.85f },
                new WaveConfig { displayName = "WAVE 4 — PATH BLOCKADE", goblins = 4, gobriders = 1, aliveCap = 5, transitionDelay = 1.75f },
            };
            return cfg;
        }

        public static LevelConfig CreateRuntimeLevel3()
        {
            var cfg = ScriptableObject.CreateInstance<LevelConfig>();
            cfg.levelNumber = 3;
            cfg.levelId = "level_03";
            cfg.levelTitle = "ABANDONED CAMP";
            cfg.mapLayoutId = "AbandonedCamp";
            cfg.heroSpawn = new Vector2(0.2f, -8.2f);
            cfg.objectiveType = LevelObjectiveType.ClearWaves;
            cfg.targetCompletionTime = 160f;
            cfg.starHpRatio = 0.4f;
            cfg.recommendedMaxAlive = 8;
            cfg.nextLevelNumber = 4;
            // Multi-approach zones: west camp, east storage, central rim, north clearing.
            cfg.spawnZones = new[]
            {
                new Vector2(-4.8f, 1.4f), new Vector2(-5.4f, -0.6f),
                new Vector2(4.6f, 0.8f), new Vector2(5.1f, 2.2f),
                new Vector2(-2.6f, 2.4f), new Vector2(2.7f, 1.8f),
                new Vector2(-2.2f, 6.4f), new Vector2(2.3f, 6.1f),
                new Vector2(0.1f, 7.6f)
            };
            cfg.cameraBoundsMin = new Vector2(-18f, -12f);
            cfg.cameraBoundsMax = new Vector2(18f, 14f);
            cfg.waves = new[]
            {
                new WaveConfig { displayName = "WAVE 1 — ABANDONED GUARDS", goblins = 3, gobriders = 0, aliveCap = 3, transitionDelay = 1.8f },
                new WaveConfig { displayName = "WAVE 2 — SIDE AMBUSH", goblins = 5, gobriders = 0, aliveCap = 5, transitionDelay = 1.85f },
                new WaveConfig { displayName = "WAVE 3 — RIDER ARRIVAL", goblins = 5, gobriders = 1, aliveCap = 5, transitionDelay = 2f },
                new WaveConfig { displayName = "WAVE 4 — FINAL DEFENSE", goblins = 6, gobriders = 1, aliveCap = 6, transitionDelay = 1.9f },
            };
            return cfg;
        }

        public static LevelConfig CreateRuntimeLevel4()
        {
            var cfg = ScriptableObject.CreateInstance<LevelConfig>();
            cfg.levelNumber = 4;
            cfg.levelId = "level_04";
            cfg.levelTitle = "GOBLIN OUTPOST";
            cfg.mapLayoutId = "GoblinOutpost";
            cfg.heroSpawn = new Vector2(0.2f, -9.0f);
            cfg.objectiveType = LevelObjectiveType.ClearWavesThenReachExit;
            cfg.exitPosition = new Vector2(0.2f, 10.5f);
            cfg.exitSize = new Vector2(2.0f, 1.5f);
            cfg.targetCompletionTime = 210f;
            cfg.starHpRatio = 0.4f;
            cfg.recommendedMaxAlive = 9;
            cfg.nextLevelNumber = 5;
            cfg.spawnZones = new[]
            {
                // Gate / approach flanks
                new Vector2(-3.5f, -2.5f), new Vector2(3.6f, -2.2f),
                // West supply
                new Vector2(-6.2f, 1.2f), new Vector2(-5.4f, 3.0f),
                // East watch
                new Vector2(5.8f, 1.5f), new Vector2(6.4f, 3.4f),
                // North courtyard
                new Vector2(-2.8f, 7.2f), new Vector2(2.9f, 7.0f), new Vector2(0.2f, 8.6f)
            };
            cfg.cameraBoundsMin = new Vector2(-22f, -22f);
            cfg.cameraBoundsMax = new Vector2(22f, 24f);
            cfg.waves = new[]
            {
                new WaveConfig { displayName = "WAVE 1 — OUTPOST GUARDS", goblins = 4, gobriders = 0, aliveCap = 4, transitionDelay = 1.85f },
                new WaveConfig { displayName = "WAVE 2 — SUPPLY AMBUSH", goblins = 4, gobriders = 1, aliveCap = 5, transitionDelay = 1.9f },
                new WaveConfig { displayName = "WAVE 3 — REINFORCEMENTS", goblins = 6, gobriders = 0, aliveCap = 6, transitionDelay = 2f },
                new WaveConfig { displayName = "WAVE 4 — OUTPOST COMMANDER", goblins = 5, gobriders = 1, eliteGobrider = 1, aliveCap = 6, transitionDelay = 2.1f },
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
            LevelConfig cfg = GetLevelConfig(continueProgress ? unlock : 1) ?? level1;
            StartLevel(cfg, fromMenu: true);
        }

        public void StartLevelByNumber(int levelNumber)
        {
            if (!GameProgress.IsLevelUnlocked(levelNumber))
                return;
            var cfg = GetLevelConfig(levelNumber);
            if (cfg != null)
                StartLevel(cfg, fromMenu: true);
        }

        public void StartLevel(LevelConfig config, bool fromMenu)
        {
            if (config == null) return;
            EnsureDefaultConfigs();
            SetState(LevelState.Loading);
            _ending = false;
            _wavesComplete = false;
            _exitUnlocked = false;
            _activeLevel = config;
            _waveIndex = -1;
            _runTimer = 0f;
            BannerText = null;
            ObjectiveText = "CLEAR WAVES";

            ResolveRefs();
            RestoreTime();
            BindSpawner();
            mapController?.ApplyLayout(config);

            if (config.spawnZones != null && config.spawnZones.Length > 0)
                spawner?.SetSpawnZones(config.spawnZones);

            SetupExitGate(config);

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

        void SetupExitGate(LevelConfig config)
        {
            bool needsExit = config.objectiveType == LevelObjectiveType.ClearWavesThenReachExit;
            Transform layout = null;
            var maps = GameObject.Find("LevelMaps");
            if (maps != null && config != null && !string.IsNullOrEmpty(config.mapLayoutId))
                layout = maps.transform.Find(config.mapLayoutId);

            if (!needsExit)
            {
                if (layout != null)
                {
                    var local = layout.GetComponentInChildren<LevelExitGate>(true);
                    if (local != null) local.gameObject.SetActive(false);
                }
                return;
            }

            // Use the gate that lives on this level's map. A shared scene gate under Trail
            // stays inactive while Goblin Outpost is showing, so the trigger never runs.
            LevelExitGate gate = null;
            if (layout != null)
                gate = layout.GetComponentInChildren<LevelExitGate>(true);
            if (gate == null)
            {
                var go = new GameObject("LevelExitGate");
                if (layout != null) go.transform.SetParent(layout, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = ProceduralSprites.Ring;
                sr.sortingOrder = 40;
                go.transform.localScale = new Vector3(config.exitSize.x * 0.55f, config.exitSize.y * 0.55f, 1f);
                gate = go.AddComponent<LevelExitGate>();
            }

            exitGate = gate;
            exitGate.Configure(config.exitPosition, config.exitSize, unlocked: false);
            ObjectiveText = "CLEAR WAVES TO OPEN EXIT";
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
                OnAllWavesCleared();
                return;
            }
            BeginWave(next);
        }

        void OnAllWavesCleared()
        {
            _wavesComplete = true;
            if (_activeLevel != null &&
                _activeLevel.objectiveType == LevelObjectiveType.ClearWavesThenReachExit)
            {
                _exitUnlocked = true;
                exitGate?.SetUnlocked(true);
                ObjectiveText = "REACH THE EXIT";
                ShowBanner("EXIT OPEN", 1.4f);
                SetState(LevelState.Playing);
                HudChanged?.Invoke();
                return;
            }
            TriggerVictory();
        }

        public void NotifyExitReached()
        {
            if (_ending) return;
            if (!_exitUnlocked) return;
            if (_state != LevelState.Playing && _state != LevelState.WaveTransition) return;
            TriggerVictory();
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
            int stars = ComputeStars();
            GameProgress.RecordRun(_activeLevel.levelNumber, score, BestCombo, stars, _runTimer);
            int next = _activeLevel != null ? _activeLevel.nextLevelNumber : 2;
            GameProgress.UnlockLevel(next);

            SetState(LevelState.Victory);
            ui?.ShowVictory(BuildStats(), score, stars);
        }

        public int ComputeScore()
        {
            int kills = Kills;
            int comboBonus = BestCombo * 10;
            float target = _activeLevel != null ? _activeLevel.targetCompletionTime : 180f;
            int timeBonus = Mathf.Max(0, Mathf.RoundToInt(target - _runTimer)) * 2;
            return kills * 25 + comboBonus + timeBonus;
        }

        public int ComputeStars()
        {
            // Star 1: complete. Star 2: HP threshold. Star 3: under target time.
            int stars = 1;
            float hpRatio = playerHealth != null ? playerHealth.Normalized : 1f;
            float needHp = _activeLevel != null ? _activeLevel.starHpRatio : 0.4f;
            float target = _activeLevel != null ? _activeLevel.targetCompletionTime : 150f;
            if (hpRatio >= needHp) stars++;
            if (_runTimer > 0f && _runTimer <= target) stars++;
            return Mathf.Clamp(stars, 1, 3);
        }

        public int StarsFor(int score) => ComputeStars();

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
            int nextNum = _activeLevel != null ? _activeLevel.nextLevelNumber : 2;
            var next = GetLevelConfig(nextNum);
            if (next == null)
                next = GetLevelConfig(Mathf.Min(nextNum, GameProgress.HighestUnlockedLevel));
            if (next != null)
                StartLevel(next, fromMenu: false);
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
