using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Profiling;

namespace GameplayPrototype
{
    /// <summary>
    /// Long-run Play Mode test. A bot walks the hero into the camp, holds R and fires skills,
    /// while the runner samples frame time, GC, memory, object/texture counts and pool sizes
    /// every interval into Logs/soak_*.csv. Start it from Play Mode:
    ///   GameplayPrototype.SoakTestRunner.Begin(300f);
    /// God mode refills the hero below 30% HP so the run is not cut short; each refill is
    /// counted as a "near death" so difficulty is still visible in the report.
    /// </summary>
    public class SoakTestRunner : MonoBehaviour
    {
        public static SoakTestRunner Current { get; private set; }
        public static string LastSummary { get; private set; } = "";
        public static bool IsRunning => Current != null && Current._running;

        float _duration;
        float _interval;
        bool _godMode;
        bool _running;
        float _elapsed;
        float _sinceSample;

        PlayerCombat _combat;
        PlayerMovement _movement;
        Health _hero;
        EnemySpawner _spawner;

        readonly List<float> _frameMs = new();
        readonly StringBuilder _csv = new();
        string _path;
        int _gcStart, _gcLastSample;
        int _nearDeaths;
        int _maxCombo;
        float _worstFrameMs;
        float _p99Sum;
        int _samples;
        long _monoStart, _monoPeak;
        int _texStart, _texLast, _goStart, _goLast;
        Vector2 _wanderTarget;
        float _wanderTimer;
        Vector2 _stuckCheckPos;
        float _stuckTimer;
        float _sidestepTimer;
        Vector2 _sidestep;
        int _unstucks;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { Current = null; LastSummary = ""; }

        public static SoakTestRunner Begin(float seconds = 300f, float sampleInterval = 10f, bool godMode = true)
        {
            if (!Application.isPlaying)
            {
                Debug.LogError("[Soak] Enter Play Mode first.");
                return null;
            }
            if (Current != null) Destroy(Current.gameObject);
            var runner = new GameObject("SoakTestRunner").AddComponent<SoakTestRunner>();
            runner._duration = seconds;
            runner._interval = sampleInterval;
            runner._godMode = godMode;
            Current = runner;
            return runner;
        }

        void Start()
        {
            _combat = FindFirstObjectByType<PlayerCombat>();
            _hero = _combat != null ? _combat.GetComponent<Health>() : null;
            _movement = _combat != null ? _combat.GetComponent<PlayerMovement>() : null;
            _spawner = FindFirstObjectByType<EnemySpawner>();
            if (_combat == null || _hero == null || _movement == null)
            {
                Debug.LogError("[Soak] No hero in scene.");
                Destroy(gameObject);
                return;
            }
            if (_spawner != null) _spawner.SetSpawningEnabled(true);

            string dir = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "Logs"));
            Directory.CreateDirectory(dir);
            _path = Path.Combine(dir, $"soak_{DateTime.Now:yyyyMMdd_HHmmss}.csv");
            _csv.AppendLine("t_s,fps_avg,frame_ms_p99,frame_ms_max,gc0_collections,mono_mb,alloc_total_mb,gameobjects,textures,enemies_alive,kills,encounter,combo_now,combo_max,hero_hp,near_deaths,vfx_created,vfx_pooled,audio_voices");

            _gcStart = _gcLastSample = GC.CollectionCount(0);
            _monoStart = _monoPeak = Profiler.GetMonoUsedSizeLong();
            _texStart = _texLast = Resources.FindObjectsOfTypeAll<Texture2D>().Length;
            _goStart = _goLast = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;
            _running = true;
            Debug.Log($"[Soak] Started {_duration:0}s, sampling every {_interval:0}s → {_path}");
        }

        void Update()
        {
            if (!_running) return;
            float dt = Time.unscaledDeltaTime;
            _elapsed += dt;
            _sinceSample += dt;
            _frameMs.Add(dt * 1000f);

            DriveBot();

            var combo = HitComboCounter.Instance;
            if (combo != null) _maxCombo = Mathf.Max(_maxCombo, combo.CurrentHits);

            if (_godMode && !_hero.IsDead && _hero.Normalized < 0.3f)
            {
                _nearDeaths++;
                _hero.Configure(_hero.MaxHp, true);
            }

            if (_sinceSample >= _interval)
                Sample();
            if (_elapsed >= _duration || _hero.IsDead)
                Finish(_hero.IsDead ? "hero died" : "completed");
        }

        void DriveBot()
        {
            var target = CombatTargeting.FindNearestEnemy(_combat.transform.position, 14f, ~0);
            Vector2 heroPos = _combat.transform.position;
            Vector2 move = Vector2.zero;

            if (target != null)
            {
                Vector2 to = (Vector2)target.transform.position - heroPos;
                float d = to.magnitude;
                if (d > 1.0f) move = to / d;

                if (d < 2.2f)
                {
                    _combat.SetSwordHeld(true);
                    TryCast(SkillId.FireDash);
                    TryCast(SkillId.MawBite);
                    TryCast(SkillId.Mace);
                    if (d > 1.3f) TryCast(SkillId.MultiSlash);
                    if (_hero.Normalized < 0.5f) TryCast(SkillId.Bone);
                }
                else
                    _combat.SetSwordHeld(false);
            }
            else
            {
                // No enemy in reach: wander around the camp so the spawner keeps feeding waves.
                _combat.SetSwordHeld(false);
                _wanderTimer -= Time.deltaTime;
                if (_wanderTimer <= 0f || Vector2.Distance(heroPos, _wanderTarget) < 0.5f)
                {
                    _wanderTarget = new Vector2(UnityEngine.Random.Range(-3f, 3f), UnityEngine.Random.Range(-1f, 4.5f));
                    _wanderTimer = 4f;
                }
                Vector2 to = _wanderTarget - heroPos;
                if (to.sqrMagnitude > 0.01f) move = to.normalized;
            }

            // No pathfinding: if the hero barely moved for 1.5 s while trying to, sidestep
            // around the obstacle for a moment and pick a new wander point.
            _stuckTimer += Time.deltaTime;
            if (_stuckTimer >= 1.5f)
            {
                bool tryingToMove = move.sqrMagnitude > 0.01f;
                if (tryingToMove && Vector2.Distance(heroPos, _stuckCheckPos) < 0.3f)
                {
                    _sidestep = Vector2.Perpendicular(move) * (UnityEngine.Random.value < 0.5f ? 1f : -1f);
                    _sidestepTimer = 0.6f;
                    _wanderTimer = 0f;
                    _unstucks++;
                }
                _stuckCheckPos = heroPos;
                _stuckTimer = 0f;
            }
            if (_sidestepTimer > 0f)
            {
                _sidestepTimer -= Time.deltaTime;
                move = (_sidestep + move * 0.3f).normalized;
            }
            _movement.SetExternalInput(move);
        }

        void TryCast(SkillId id)
        {
            if (_combat.IsReady(id)) _combat.TryCast(id);
        }

        void Sample()
        {
            _sinceSample = 0f;
            _frameMs.Sort();
            float avgMs = 0f;
            foreach (var f in _frameMs) avgMs += f;
            avgMs /= Mathf.Max(1, _frameMs.Count);
            float p99 = _frameMs.Count > 0 ? _frameMs[Mathf.Min(_frameMs.Count - 1, Mathf.FloorToInt(_frameMs.Count * 0.99f))] : 0f;
            float max = _frameMs.Count > 0 ? _frameMs[_frameMs.Count - 1] : 0f;
            _frameMs.Clear();
            _worstFrameMs = Mathf.Max(_worstFrameMs, max);
            _p99Sum += p99;
            _samples++;

            int gc = GC.CollectionCount(0);
            long mono = Profiler.GetMonoUsedSizeLong();
            _monoPeak = Math.Max(_monoPeak, mono);
            _texLast = Resources.FindObjectsOfTypeAll<Texture2D>().Length;
            _goLast = FindObjectsByType<Transform>(FindObjectsInactive.Include, FindObjectsSortMode.None).Length;

            _csv.AppendLine(string.Join(",",
                _elapsed.ToString("0"),
                (1000f / Mathf.Max(0.01f, avgMs)).ToString("0.0"),
                p99.ToString("0.00"),
                max.ToString("0.00"),
                gc - _gcLastSample,
                (mono / 1048576f).ToString("0.00"),
                (Profiler.GetTotalAllocatedMemoryLong() / 1048576f).ToString("0.0"),
                _goLast,
                _texLast,
                _spawner != null ? _spawner.CountAlive() : 0,
                _spawner != null ? _spawner.KillCount : 0,
                _spawner != null ? _spawner.CurrentEncounter.ToString() : "-",
                HitComboCounter.Instance != null ? HitComboCounter.Instance.CurrentHits : 0,
                _maxCombo,
                _hero.CurrentHp,
                _nearDeaths,
                CombatVfx.CreatedCount,
                CombatVfx.PooledCount,
                GameAudio.Instance != null ? GameAudio.Instance.ActiveVoices : 0));
            _gcLastSample = gc;
            File.WriteAllText(_path, _csv.ToString()); // flush every sample so a crash keeps the data
        }

        void Finish(string reason)
        {
            if (!_running) return;
            Sample();
            _running = false;
            _combat.SetSwordHeld(false);
            _movement.ClearExternalInput();

            LastSummary =
                $"[Soak] {reason} after {_elapsed:0}s | kills {(_spawner != null ? _spawner.KillCount : 0)} | max combo {_maxCombo} | " +
                $"near-deaths {_nearDeaths} | bot unstucks {_unstucks} | avg p99 {(_p99Sum / Mathf.Max(1, _samples)):0.0}ms, worst frame {_worstFrameMs:0.0}ms | " +
                $"GC0 {GC.CollectionCount(0) - _gcStart} | mono {_monoStart / 1048576f:0.0}→{_monoPeak / 1048576f:0.0}MB peak | " +
                $"textures {_texStart}→{_texLast} | objects {_goStart}→{_goLast} | VFX created {CombatVfx.CreatedCount}, pooled {CombatVfx.PooledCount} | {_path}";
            File.AppendAllText(_path, "# " + LastSummary + Environment.NewLine);
            Debug.Log(LastSummary);
        }

        void OnDestroy()
        {
            if (_running) Finish("stopped");
            if (Current == this) Current = null;
        }
    }
}
