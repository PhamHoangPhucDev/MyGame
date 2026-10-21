using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameplayPrototype
{
    public enum SkillId
    {
        FireDash = 0,
        MultiSlash = 1,
        MawBite = 2,
        Sword = 3,
        Bone = 4,
        Mace = 5
    }

    [RequireComponent(typeof(PlayerMovement))]
    [RequireComponent(typeof(Health))]
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Shared")]
        [SerializeField] LayerMask enemyMask = ~0;
        [SerializeField] float aimFallbackDistance = 8f;

        [Header("R — Basic Slash")]
        [SerializeField] float slashCooldown = 0.35f;
        [SerializeField] float slashRadius = 1.35f;
        [SerializeField] int slashDamage = 18;
        [SerializeField] float slashKnockback = 2.8f;
        [SerializeField] float slashAutoTargetRadius = 1.8f;
        [Tooltip("Press → damage. Matches the Impact key of Hero_Attack_* (windup 0.08 + 0.04).")]
        [SerializeField] float slashImpactDelay = 0.12f;

        [Header("2 — Flame Burst (SkillId.FireDash)")]
        [SerializeField] float flameCooldown = 5f;
        [SerializeField] float flameRadius = 1.7f;
        [SerializeField] float flameRange = 1.1f;
        [SerializeField] int flameDamage = 30;

        [Header("3 — Dash Slash (SkillId.MultiSlash)")]
        [SerializeField] float dashCooldown = 4f;
        [SerializeField] float dashSpeed = 13f;
        [SerializeField] float dashDuration = 0.15f;
        [SerializeField] float dashRadius = 1.5f;
        [SerializeField] int dashDamage = 26;

        [Header("4 — Whirlwind (SkillId.MawBite)")]
        [SerializeField] float whirlCooldown = 7f;
        [SerializeField] float whirlRadius = 2.2f;
        [SerializeField] int whirlDamage = 20;
        [SerializeField] int whirlWaves = 3;
        [SerializeField] float whirlWaveInterval = 0.11f;

        [Header("V — Guard (SkillId.Bone)")]
        [SerializeField] float guardCooldown = 8f;
        [SerializeField] float guardDuration = 1.4f;
        [SerializeField] float guardDamageMultiplier = 0.35f;

        [Header("C — Crescent Strike (SkillId.Mace)")]
        [SerializeField] float crescentCooldown = 10f;
        [SerializeField] float crescentRadius = 2.1f;
        [SerializeField] int crescentDamage = 42;
        [SerializeField] float crescentKnockback = 5.2f;
        [SerializeField] float crescentAnticipation = 0.12f;

        PlayerMovement _movement;
        PlayerVisual _visual;
        CameraFollow _cameraFollow;
        readonly float[] _cooldownRemaining = new float[6];
        readonly float[] _cooldownTotal = new float[6];
        readonly List<Health> _hitBuffer = new();
        Vector2 _facing = Vector2.right;
        SkillId _selected = SkillId.Sword;
        SkillId _activeSkill = SkillId.Sword;
        float _whirlTimer;
        int _whirlLeft;
        float _guardTimer;
        bool _crescentPending;
        float _crescentDelay;
        bool _slashPending;
        float _slashDelay;
        Vector2 _slashFacing;
        bool _slashBuffered;
        float _slashBufferTimer;
        const float SlashInputBuffer = 0.12f;
        const float SlashChainReset = 1.8f;
        int _slashChain;
        float _lastSlashUnscaled = -999f;
        float _pendingRadius;
        int _pendingDamage;
        float _pendingKnockback;
        float _pendingVfxScale = 1f;
        int _pendingSlashVariant;
        Vector2 _slashSwing;
        Health _stickyTarget;
        bool _debugSwordHeld;
        // R2C: optional class kit (Sandbox, via ClassCombat). Null = the serialized fields below drive everything.
        ClassKit _kit;
        int _pendingStep = -1;
        SkillDef _whirlDef;
        SkillDef _crescentDef;

        public ClassKit Kit => _kit;
        public event Action<ClassKit> KitChanged;

        /// <summary>Runs this class kit (null restores the scene's own values). Resets cooldowns and the combo.</summary>
        public void SetKit(ClassKit kit)
        {
            _kit = kit;
            ApplyCooldownTotals();
            ResetCombatState();
            KitChanged?.Invoke(kit);
        }

        float AutoTargetRadius => _kit != null ? _kit.combo.autoTargetRadius : slashAutoTargetRadius;

        /// <summary>Combo step of the swing in flight (0..n-1). EquipmentVisual picks per-step poses/VFX. -1 when idle/skills.</summary>
        public int CurrentComboStep => _pendingStep;
        public bool IsStealthed => _stealthTimer > 0f;
        public float NextHitMultiplier => _nextHitMul;

        float _nextHitMul = 1f;
        float _stealthTimer;
        float _stealthAlpha = 1f;

        public int SlashChainStep => _slashChain;
        public bool IsGuarding => _guardTimer > 0f;
        public bool DebugSwordHeld { get => _debugSwordHeld; set => _debugSwordHeld = value; }

        public void SetSwordHeld(bool held) => _debugSwordHeld = held;

        public Vector2 Facing => _facing;
        public SkillId SelectedSkill => _selected;
        public event Action<SkillId> SelectedChanged;
        public event Action CooldownsChanged;

        public float GetCooldownRemaining(SkillId id) => _cooldownRemaining[(int)id];
        public float GetCooldownNormalized(SkillId id)
        {
            float total = _cooldownTotal[(int)id];
            if (total <= 0.001f) return 0f;
            return Mathf.Clamp01(_cooldownRemaining[(int)id] / total);
        }

        public bool IsReady(SkillId id) => _cooldownRemaining[(int)id] <= 0f;

        public void ResetCombatState()
        {
            for (int i = 0; i < _cooldownRemaining.Length; i++)
                _cooldownRemaining[i] = 0f;
            _whirlTimer = 0f;
            _whirlLeft = 0;
            _guardTimer = 0f;
            _crescentPending = false;
            _crescentDelay = 0f;
            _slashPending = false;
            _slashDelay = 0f;
            _slashBuffered = false;
            _slashBufferTimer = 0f;
            _slashChain = 0;
            _lastSlashUnscaled = -999f;
            _debugSwordHeld = false;
            _stickyTarget = null;
            _nextHitMul = 1f;
            if (_stealthTimer > 0f) EndStealth();
            GetComponent<Health>()?.SetDamageTakenMultiplier(1f);
            enabled = true;
            CooldownsChanged?.Invoke();
        }

        void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _visual = GetComponent<PlayerVisual>();
            if (_visual == null) _visual = gameObject.AddComponent<PlayerVisual>();
            _visual.EnsureHierarchy();

            ApplyCooldownTotals();
        }

        void ApplyCooldownTotals()
        {
            if (_kit != null)
            {
                foreach (SkillId id in Enum.GetValues(typeof(SkillId)))
                    _cooldownTotal[(int)id] = _kit.Cooldown(id);
                return;
            }
            // Key map kept: 2/3/4/R/V/C. Behaviors remapped for Phase 8C (see report).
            _cooldownTotal[(int)SkillId.FireDash] = flameCooldown;
            _cooldownTotal[(int)SkillId.MultiSlash] = dashCooldown;
            _cooldownTotal[(int)SkillId.MawBite] = whirlCooldown;
            _cooldownTotal[(int)SkillId.Sword] = slashCooldown;
            _cooldownTotal[(int)SkillId.Bone] = guardCooldown;
            _cooldownTotal[(int)SkillId.Mace] = crescentCooldown;
        }

        void Start()
        {
            if (Camera.main != null)
                _cameraFollow = Camera.main.GetComponent<CameraFollow>();
        }

        void Update()
        {
            var health = GetComponent<Health>();
            if (health != null && health.IsDead)
            {
                _whirlLeft = 0;
                EndGuard();
                return;
            }

            TickCooldowns();
            TickGuard();
            UpdateFacing();
            ReadSkillHotkeys();
            TickSwordHold();
            TickSlashBuffer();
            TickSlashStrike();
            TickWhirlwind();
            TickCrescentStrike();
            TickStealth();
        }

        void TickCooldowns()
        {
            // Unscaled so hotbar countdown continues during hitstop.
            bool changed = false;
            for (int i = 0; i < _cooldownRemaining.Length; i++)
            {
                if (_cooldownRemaining[i] <= 0f) continue;
                _cooldownRemaining[i] = Mathf.Max(0f, _cooldownRemaining[i] - Time.unscaledDeltaTime);
                changed = true;
            }
            if (changed)
                CooldownsChanged?.Invoke();
        }

        void UpdateFacing()
        {
            if (_movement.MoveInput.sqrMagnitude > 0.01f)
                _facing = _movement.MoveInput.normalized;
            else
            {
                var nearest = CombatTargeting.FindNearestEnemy(transform.position, aimFallbackDistance, enemyMask);
                if (nearest != null)
                    _facing = ((Vector2)nearest.transform.position - (Vector2)transform.position).normalized;
            }
        }

        void ReadSkillHotkeys()
        {
            var kb = Keyboard.current;
            if (kb == null) return;

            if (kb.digit2Key.wasPressedThisFrame) TryCast(SkillId.FireDash);
            if (kb.digit3Key.wasPressedThisFrame) TryCast(SkillId.MultiSlash);
            if (kb.digit4Key.wasPressedThisFrame) TryCast(SkillId.MawBite);
            if (kb.rKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame)
                TryBufferSwordTap();
            if (kb.vKey.wasPressedThisFrame) TryCast(SkillId.Bone);
            if (kb.cKey.wasPressedThisFrame) TryCast(SkillId.Mace);
        }

        bool SwordHeld()
        {
            if (_debugSwordHeld) return true;
            var kb = Keyboard.current;
            return kb != null && (kb.rKey.isPressed || kb.spaceKey.isPressed);
        }

        void TryBufferSwordTap()
        {
            if (TryCast(SkillId.Sword))
            {
                _slashBuffered = false;
                _slashBufferTimer = 0f;
                return;
            }

            // Early tap only counts in the last slice of cooldown. Holding R is handled separately.
            float remaining = GetCooldownRemaining(SkillId.Sword);
            if (remaining <= 0f || remaining > SlashInputBuffer)
                return;

            _slashBuffered = true;
            _slashBufferTimer = SlashInputBuffer;
            Select(SkillId.Sword);
        }

        void TickSwordHold()
        {
            if (!SwordHeld() || _slashPending || !IsReady(SkillId.Sword))
                return;
            if (TryCast(SkillId.Sword))
            {
                _slashBuffered = false;
                _slashBufferTimer = 0f;
            }
        }

        void TickSlashBuffer()
        {
            if (!_slashBuffered) return;
            _slashBufferTimer -= Time.unscaledDeltaTime;
            if (_slashBufferTimer <= 0f)
            {
                _slashBuffered = false;
                return;
            }
            if (!IsReady(SkillId.Sword)) return;
            if (TryCast(SkillId.Sword))
            {
                _slashBuffered = false;
                _slashBufferTimer = 0f;
            }
        }

        public bool TryCast(SkillId id)
        {
            Select(id);
            if (!IsReady(id))
                return false;

            if (_kit != null && id != SkillId.Sword)
            {
                var def = _kit.Get(id);
                if (def == null || def.behaviour == SkillBehaviour.None)
                    return false;
                _activeSkill = id;
                if (!RunBehaviour(def))
                    return false;
                StartCooldown(id);
                return true;
            }

            _activeSkill = id;
            switch (id)
            {
                case SkillId.Sword: PerformSlash(); GameAudio.Play(Sfx.SlashSwing); break;
                case SkillId.FireDash: PerformFlameBurst(); GameAudio.Play(Sfx.SkillFlame); break;
                case SkillId.MultiSlash: PerformDashSlash(); GameAudio.Play(Sfx.SkillDash); break;
                case SkillId.MawBite: BeginWhirlwind(); break; // each wave plays SkillWhirl
                case SkillId.Bone: BeginGuard(); GameAudio.Play(Sfx.SkillGuard); break;
                case SkillId.Mace: BeginCrescentStrike(); GameAudio.Play(Sfx.SkillCrescent); break;
            }

            StartCooldown(id);
            return true;
        }

        bool RunBehaviour(SkillDef def)
        {
            switch (def.behaviour)
            {
                case SkillBehaviour.ShadowStep: return PerformShadowStep(def);
                case SkillBehaviour.ProjectileFan: PerformProjectileFan(def); GameAudio.Play(Sfx.SkillDash); return true;
                case SkillBehaviour.Stealth: BeginStealth(def); GameAudio.Play(Sfx.SkillGuard); return true;
                case SkillBehaviour.Execute: return PerformExecute(def);
                case SkillBehaviour.BurstAhead: PerformFlameBurst(def); GameAudio.Play(Sfx.SkillFlame); break;
                case SkillBehaviour.DashStrike: PerformDashSlash(def); GameAudio.Play(Sfx.SkillDash); break;
                case SkillBehaviour.Whirlwind: BeginWhirlwind(def); break;
                case SkillBehaviour.Guard: BeginGuard(def); GameAudio.Play(Sfx.SkillGuard); break;
                case SkillBehaviour.CrescentStrike: BeginCrescentStrike(def); GameAudio.Play(Sfx.SkillCrescent); break;
                case SkillBehaviour.ShieldBash: PerformShieldBash(def); GameAudio.Play(Sfx.SkillFlame); break;
                case SkillBehaviour.Charge: PerformCharge(def); GameAudio.Play(Sfx.SkillDash); break;
            }
            return true;
        }

        public void Select(SkillId id)
        {
            if (_selected == id) return;
            _selected = id;
            SelectedChanged?.Invoke(id);
        }

        void StartCooldown(SkillId id)
        {
            int i = (int)id;
            _cooldownRemaining[i] = _cooldownTotal[i];
            CooldownsChanged?.Invoke();
        }

        void PerformSlash()
        {
            if (_kit != null)
            {
                PerformKitCombo(_kit.combo);
                return;
            }

            if (Time.unscaledTime - _lastSlashUnscaled > SlashChainReset)
                _slashChain = 0;

            AimSlash();
            Vector2 perp = new Vector2(-_slashFacing.y, _slashFacing.x);
            _pendingRadius = slashRadius;
            _pendingDamage = slashDamage;
            _pendingKnockback = slashKnockback;
            _pendingVfxScale = 1f;
            // R3B: three readable patterns with one body Attack clip — swing / VFX / hitbox only.
            // ATTACK1 L→R, ATTACK2 R→L, ATTACK3 finisher (wider + stronger feedback).
            _pendingStep = _slashChain;
            if (_slashChain == 0)
                _slashSwing = (_slashFacing + perp * 0.85f).normalized;
            else if (_slashChain == 1)
                _slashSwing = (_slashFacing - perp * 0.85f).normalized;
            else
            {
                _slashSwing = _slashFacing;
                _pendingRadius = slashRadius * 1.35f;
                _pendingKnockback = slashKnockback * 1.25f;
                _pendingVfxScale = 1.45f;
            }

            _pendingSlashVariant = _slashChain; // 0 = VFX A, 1 = B, 2 = finisher C
            _slashChain = (_slashChain + 1) % 3;
            _lastSlashUnscaled = Time.unscaledTime;
            _visual?.PlaySlashSwing(_slashSwing);
            _slashPending = true;
            // Strike lands on the Impact frame of the attack clip (after the windup pose).
            _slashDelay = slashImpactDelay;
        }

        // Same flow as the legacy chain, every number from the kit's ComboStep.
        void PerformKitCombo(ComboDef c)
        {
            int n = Mathf.Max(1, c.steps != null ? c.steps.Length : 0);
            if (Time.unscaledTime - _lastSlashUnscaled > SlashChainReset || _slashChain >= n)
                _slashChain = 0;

            AimSlash();
            var s = c.steps != null && c.steps.Length > 0 ? c.steps[_slashChain] : null;
            Vector2 perp = new Vector2(-_slashFacing.y, _slashFacing.x);
            float offset = s != null ? s.swingOffset : 0f;
            _slashSwing = offset == 0f ? _slashFacing : (_slashFacing + perp * offset).normalized;
            _pendingRadius = c.radius * (s != null ? s.radiusMul : 1f);
            _pendingDamage = Mathf.RoundToInt(c.damage * (s != null ? s.damageMul : 1f));
            _pendingKnockback = c.knockback * (s != null ? s.knockbackMul : 1f);
            _pendingVfxScale = s != null ? s.vfxScale : 1f;
            _pendingSlashVariant = s != null ? s.vfxVariant : 0;
            _pendingStep = _slashChain;

            _slashChain = (_slashChain + 1) % n;
            _lastSlashUnscaled = Time.unscaledTime;
            _visual?.PlaySlashSwing(_slashSwing);
            _slashPending = true;
            _slashDelay = c.impactDelay;
        }

        void AimSlash()
        {
            // Keep sticky target while it stays alive and in range so facing does not flicker.
            if (_stickyTarget != null && !_stickyTarget.IsDead)
            {
                float d = Vector2.Distance(transform.position, _stickyTarget.transform.position);
                if (d <= AutoTargetRadius)
                {
                    Vector2 toSticky = (Vector2)_stickyTarget.transform.position - (Vector2)transform.position;
                    if (toSticky.sqrMagnitude > 0.0001f)
                        _facing = toSticky.normalized;
                    _slashFacing = _facing;
                    return;
                }
            }

            _stickyTarget = CombatTargeting.FindNearestEnemy(transform.position, AutoTargetRadius, enemyMask);
            if (_stickyTarget != null)
            {
                Vector2 to = (Vector2)_stickyTarget.transform.position - (Vector2)transform.position;
                if (to.sqrMagnitude > 0.0001f)
                    _facing = to.normalized;
            }
            _slashFacing = _facing.sqrMagnitude > 0.01f ? _facing.normalized : Vector2.right;
        }

        void TickSlashStrike()
        {
            if (!_slashPending) return;
            // Scaled time so a hitstop from the previous swing freezes the windup with the animation.
            _slashDelay -= Time.deltaTime;
            if (_slashDelay > 0f) return;
            _slashPending = false;
            Vector2 swing = _slashSwing.sqrMagnitude > 0.01f ? _slashSwing : _slashFacing;
            Vector2 hitOrigin = (Vector2)transform.position + swing * 0.35f;
            Vector3 slashPos = transform.position + (Vector3)(swing * 0.32f) + Vector3.up * 0.42f;
            // R1 authored slash frames; falls back to the procedural arc if the sheets are missing.
            // Skipped only when this hero's EquipmentVisual draws its own blade-tip slash (Sandbox R2C).
            // R3D.1: finisher uses Aren_Thrust sheet when available; else legacy slash frames/procedural.
            bool thrust = ArenKitVfx.Active && _pendingStep == 2;
            if (thrust)
                ArenKitVfx.PlayThrust(transform.position + Vector3.up * 0.42f, swing);
            else if (!EquipmentVisual.ReplacesLegacySlash(this) &&
                !CombatVfx.SpawnSwordSlashFrames(transform.position + Vector3.up * 0.42f, swing, _pendingSlashVariant))
                CombatVfx.SpawnSwordSlash(slashPos, swing, _pendingVfxScale);
            int strikeDamage = _pendingDamage;
            if (_kit != null && _nextHitMul != 1f)
            {
                strikeDamage = Mathf.RoundToInt(strikeDamage * _nextHitMul);
                _nextHitMul = 1f;
            }
            ApplyAreaDamage(hitOrigin, _pendingRadius, strikeDamage, _pendingKnockback, new Color(0.7f, 0.95f, 1f));
            if (_kit != null) ApplyStepBleed();
        }

        // ---------- R2C class behaviours (kit path only) ----------

        void ApplyStepBleed()
        {
            var steps = _kit.combo.steps;
            if (steps == null || _pendingStep < 0 || _pendingStep >= steps.Length) return;
            var s = steps[_pendingStep];
            if (s.bleedDamage <= 0 || s.bleedDuration <= 0f) return;
            foreach (var h in _hitBuffer)
                if (h != null && !h.IsDead) StatusBleed.Apply(h, s.bleedDamage, s.bleedTick, s.bleedDuration);
        }

        Rigidbody2D Body2D => GetComponent<Rigidbody2D>();

        void Blink(Vector2 to)
        {
            var rb = Body2D;
            if (rb != null) { rb.position = to; rb.linearVelocity = Vector2.zero; }
            transform.position = to;
        }

        // Vex 2: blink behind the nearest enemy in range; the next R hit deals x multiplier. No target = not cast.
        bool PerformShadowStep(SkillDef d)
        {
            var target = CombatTargeting.FindNearestEnemy(transform.position, d.range, enemyMask);
            if (target == null) return false;
            Vector2 from = transform.position, tp = target.transform.position;
            Vector2 dir = (tp - from).sqrMagnitude > 0.0001f ? (tp - from).normalized : _facing;
            KitVfx.Play(d.vfxFrames, from + Vector2.up * 0.5f, d.vfxFps);
            Blink(tp + dir * d.radius);
            _facing = -dir;
            _stickyTarget = target;
            _nextHitMul = d.multiplier > 0f ? d.multiplier : 1f;
            KitVfx.Play(d.vfxFrames, (Vector2)transform.position + Vector2.up * 0.5f, d.vfxFps);
            GameAudio.Play(Sfx.SkillDash);
            return true;
        }

        // Vex 3: fan of projectiles around the facing.
        void PerformProjectileFan(SkillDef d)
        {
            int n = Mathf.Max(1, d.count);
            Vector2 baseDir = _facing.sqrMagnitude > 0.01f ? _facing.normalized : Vector2.right;
            var aim = CombatTargeting.FindNearestEnemy(transform.position, d.range, enemyMask);
            if (aim != null) baseDir = ((Vector2)aim.transform.position - (Vector2)transform.position).normalized;
            if (!_slashPending) _pendingStep = -1; // skill swing: no combo-step pose/VFX
            _facing = baseDir;
            _visual?.PlaySlashSwing(baseDir, skipWindup: true);
            float start = n > 1 ? -d.spreadDeg * 0.5f : 0f, step = n > 1 ? d.spreadDeg / (n - 1) : 0f;
            for (int i = 0; i < n; i++)
            {
                Vector2 dir = Quaternion.Euler(0f, 0f, start + step * i) * baseDir;
                KitProjectile.Fire((Vector2)transform.position + Vector2.up * 0.45f + dir * 0.3f, dir, d.projectilePoses, d.speed, d.range,
                    d.damage, d.knockback, d.bleedDamage, d.bleedTick, d.bleedDuration, enemyMask, d.popupColor);
            }
        }

        // Vex V: translucent + immune for the duration (enemy AI is untouched, they just cannot hurt the hero).
        void BeginStealth(SkillDef d)
        {
            KitVfx.Play(d.vfxFrames, (Vector2)transform.position + Vector2.up * 0.5f, d.vfxFps);
            _stealthTimer = Mathf.Max(0.1f, d.duration);
            _stealthAlpha = Mathf.Clamp01(d.alpha);
            GetComponent<Health>()?.SetDamageTakenMultiplier(0f);
        }

        void TickStealth()
        {
            if (_stealthTimer <= 0f) return;
            _stealthTimer -= Time.deltaTime;
            var sr = BodyRenderer();
            if (sr != null) { var c = sr.color; c.a = _stealthAlpha; sr.color = c; }
            if (_stealthTimer <= 0f) EndStealth();
        }

        void EndStealth()
        {
            _stealthTimer = 0f;
            var sr = BodyRenderer();
            if (sr != null) { var c = sr.color; c.a = 1f; sr.color = c; }
            if (_guardTimer <= 0f) GetComponent<Health>()?.SetDamageTakenMultiplier(1f);
        }

        SpriteRenderer BodyRenderer()
        {
            var anim = GetComponentInChildren<Animator>();
            return anim != null ? anim.GetComponent<SpriteRenderer>() : null;
        }

        // Vex C: blink next to the target and strike it; x multiplier when its HP ratio is below threshold.
        bool PerformExecute(SkillDef d)
        {
            var target = CombatTargeting.FindNearestEnemy(transform.position, d.range, enemyMask);
            if (target == null) return false;
            Vector2 from = transform.position, tp = target.transform.position;
            Vector2 dir = (tp - from).sqrMagnitude > 0.0001f ? (tp - from).normalized : _facing;
            Blink(tp - dir * 0.6f);
            if (!_slashPending) _pendingStep = -1;
            _facing = dir;
            _visual?.PlaySlashSwing(dir, skipWindup: true);
            bool low = target.Normalized < d.threshold;
            int dmg = Mathf.RoundToInt(d.damage * (low ? d.multiplier : 1f));
            KitVfx.Play(d.vfxFrames, tp + Vector2.up * 0.4f, d.vfxFps, false, 80);
            if (low) KitVfx.ScreenTint(new Color(0.08f, 0.02f, 0.05f, 0.45f), 0.22f);
            ApplyAreaDamage(tp, 0.6f, dmg, d.knockback, d.popupColor);
            GameAudio.Play(Sfx.SkillCrescent);
            return true;
        }

        // Each routine takes an optional kit SkillDef; null = the serialized fields and constants (Level 1-4, unchanged).
        void PerformFlameBurst(SkillDef d = null)
        {
            // Sandbox designed Aren: route key 2 through Shield Bash when PreferDesignedSkills.
            if (ArenKitVfx.Active && d == null)
            {
                PerformShieldBash(null);
                return;
            }
            float range = d != null ? d.range : flameRange;
            Vector2 origin = (Vector2)transform.position + _facing * range;
            CombatVfx.SpawnBurst(origin, new Color(1f, 0.45f, 0.15f), 1.55f);
            CombatVfx.SpawnBurst(origin + _facing * 0.35f, new Color(1f, 0.75f, 0.2f), 1.1f);
            ApplyAreaDamage(origin, d != null ? d.radius : flameRadius, d != null ? d.damage : flameDamage,
                d != null ? d.knockback : 2.8f, d != null ? d.popupColor : new Color(1f, 0.55f, 0.25f));
        }

        /// <summary>R3D.1 Aren skill 2 — short dash then frontal cone + stun (Goblin/Gobrider).</summary>
        void PerformShieldBash(SkillDef d)
        {
            Vector2 dir = _facing.sqrMagnitude > 0.01f ? _facing.normalized : Vector2.right;
            float dashTime = d != null ? Mathf.Max(0.08f, d.duration) : 0.18f;
            float speed = d != null ? d.speed : 11f;
            if (_movement != null)
                _movement.OverrideVelocity(dir * speed, dashTime);
            ArenKitVfx.PlayChargeDust(transform.position + Vector3.down * 0.15f);
            // Play bash VFX at cast; damage lands after the dash settles.
            ArenKitVfx.PlayShieldBash(transform.position + Vector3.up * 0.35f, dir);
            if (!isActiveAndEnabled)
            {
                FinishShieldBash(dir, d);
                return;
            }
            StartCoroutine(ShieldBashImpact(dir, d, dashTime));
        }

        System.Collections.IEnumerator ShieldBashImpact(Vector2 dir, SkillDef d, float delay)
        {
            float t = 0f;
            while (t < delay)
            {
                // Unscaled: editor/agent Play Mode often runs with deltaTime≈0 when unfocused.
                t += Time.unscaledDeltaTime;
                yield return null;
            }
            FinishShieldBash(dir, d);
        }

        void FinishShieldBash(Vector2 dir, SkillDef d)
        {
            float radius = d != null ? d.radius : 1.4f;
            int damage = d != null ? d.damage : flameDamage;
            float knock = d != null ? d.knockback : 4f;
            Color pop = d != null ? d.popupColor : new Color(1f, 0.9f, 0.45f);
            float cone = d != null && d.coneDeg > 1f ? d.coneDeg : 70f;
            ApplyConeDamage((Vector2)transform.position + dir * 0.35f, dir, radius, cone, damage, knock, pop);
        }

        void ApplyConeDamage(Vector2 origin, Vector2 facing, float radius, float coneDeg, int damage, float knockback, Color popupColor)
        {
            CombatTargeting.OverlapEnemies(origin, radius, _hitBuffer, enemyMask);
            int landed = 0;
            float half = coneDeg * 0.5f;
            foreach (var health in _hitBuffer)
            {
                if (health == null || health.IsDead) continue;
                Vector2 hitPoint = health.transform.position;
                Vector2 to = hitPoint - origin;
                if (to.sqrMagnitude > 0.0001f && Vector2.Angle(facing, to) > half)
                    continue;
                Vector2 dir = to.sqrMagnitude > 0.001f ? to.normalized : facing;
                if (!health.TakeDamage(damage, hitPoint, dir * knockback)) continue;
                landed++;
                DamagePopup.Spawn((Vector3)hitPoint + Vector3.up * 1.05f, damage, popupColor);
                CombatVfx.SpawnHitSpark(hitPoint);
                StatusStun.Apply(health, StatusStun.DurationFor(health));
                HitComboCounter.Instance?.RegisterHit();
            }
            if (landed > 0)
            {
                if (_kit != null) ApplyKitHitFeedback();
                else ApplyHitFeedback(_activeSkill, false);
            }
        }

        /// <summary>R3D.1 Aren skill 3 — Charge path (same CD/dmg band as Dash Slash) + ChargeDust; no new sheet pack skill.</summary>
        void PerformCharge(SkillDef d)
        {
            Vector2 dir = _facing.sqrMagnitude > 0.01f ? _facing.normalized : Vector2.right;
            float dur = d != null ? d.duration : 0.3f;
            float speed = d != null ? d.speed : dashSpeed;
            if (_movement != null)
                _movement.OverrideVelocity(dir * speed, dur);
            if (isActiveAndEnabled)
                StartCoroutine(ChargeDustTrail(dur));
            else
                ArenKitVfx.PlayChargeDust(transform.position + Vector3.down * 0.2f);
            ApplyAreaDamage((Vector2)transform.position + dir * 0.4f,
                d != null ? d.radius : 0.8f,
                d != null ? d.damage : dashDamage,
                d != null ? d.knockback : 3f,
                d != null ? d.popupColor : new Color(0.55f, 0.9f, 1f));
        }

        System.Collections.IEnumerator ChargeDustTrail(float dur)
        {
            float t = 0f;
            while (t < dur)
            {
                ArenKitVfx.PlayChargeDust(transform.position + Vector3.down * 0.2f);
                float w = 0f;
                while (w < 0.08f) { w += Time.unscaledDeltaTime; yield return null; }
                t += 0.08f;
            }
        }

        void PerformDashSlash(SkillDef d = null)
        {
            Vector2 dir = _facing.sqrMagnitude > 0.01f ? _facing : Vector2.right;
            _movement.OverrideVelocity(dir * (d != null ? d.speed : dashSpeed), d != null ? d.duration : dashDuration);
            if (ArenKitVfx.Active)
                ArenKitVfx.PlayChargeDust(transform.position + Vector3.down * 0.2f);
            else
            {
                CombatVfx.SpawnCrescent(transform.position + (Vector3)(dir * 0.35f), dir, 1.5f);
                CombatVfx.SpawnShockwave(transform.position, 1.25f);
            }
            ApplyAreaDamage((Vector2)transform.position + dir * 0.4f, d != null ? d.radius : dashRadius, d != null ? d.damage : dashDamage,
                d != null ? d.knockback : 4.2f, d != null ? d.popupColor : new Color(0.55f, 0.9f, 1f));
        }

        SkillId _whirlSlot = SkillId.MawBite;

        void BeginWhirlwind(SkillDef d = null)
        {
            _whirlDef = d;
            _whirlSlot = d != null ? _activeSkill : SkillId.MawBite;
            _whirlLeft = d != null ? d.count : whirlWaves;
            _whirlTimer = 0f;
            WhirlWave();
            _whirlLeft--;
        }

        void TickWhirlwind()
        {
            if (_whirlLeft <= 0) return;
            _whirlTimer -= Time.deltaTime;
            if (_whirlTimer > 0f) return;
            WhirlWave();
            _whirlLeft--;
            _whirlTimer = _whirlDef != null ? _whirlDef.interval : whirlWaveInterval;
        }

        void WhirlWave()
        {
            var d = _whirlDef;
            _activeSkill = _whirlSlot;
            if (d != null && d.useTint && !_slashPending) _pendingStep = -1; // class flurry: no combo-step VFX
            _visual?.PlaySlashSwing(_facing, skipWindup: true);
            GameAudio.Play(Sfx.SkillWhirl);
            if (d != null && d.useTint)
            {
                // class-coloured flurry: crescents rotating around the hero wave by wave
                float a = (_whirlLeft * 60f) * Mathf.Deg2Rad;
                Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
                CombatVfx.SpawnCrescentTinted(transform.position + Vector3.up * 0.35f, dir, 1.5f, d.tintColor);
                CombatVfx.SpawnCrescentTinted(transform.position + Vector3.up * 0.35f, -dir, 1.2f, d.tintColor * 0.8f);
            }
            else if (ArenKitVfx.Active)
                ArenKitVfx.PlayWhirlRing(transform);
            else
            {
                CombatVfx.SpawnCrescent(transform.position, _facing, 2.0f);
                CombatVfx.SpawnCrescent(transform.position, -_facing, 1.7f);
                CombatVfx.SpawnBurst(transform.position, new Color(0.75f, 0.95f, 1f), 1.5f);
            }
            ApplyAreaDamage(transform.position, d != null ? d.radius : whirlRadius, d != null ? d.damage : whirlDamage,
                d != null ? d.knockback : 3.0f, d != null ? d.popupColor : new Color(0.8f, 0.95f, 1f));
        }

        ArenKitVfx.BulwarkHandle _bulwark;
        bool _bulwarkLooping;

        void BeginGuard(SkillDef d = null)
        {
            _guardTimer = d != null ? d.duration : guardDuration;
            var health = GetComponent<Health>();
            health?.SetDamageTakenMultiplier(d != null ? d.multiplier : guardDamageMultiplier);
            if (ArenKitVfx.Active)
            {
                _bulwark = ArenKitVfx.BeginBulwark(transform);
                _bulwarkLooping = false;
                StartCoroutine(BulwarkAppearThenLoop());
            }
            else
            {
                CombatVfx.SpawnShockwave(transform.position, 1.1f);
                CombatVfx.SpawnBurst(transform.position, new Color(0.65f, 0.85f, 1f), 0.9f);
            }
        }

        System.Collections.IEnumerator BulwarkAppearThenLoop()
        {
            float w = 0f;
            while (w < 0.12f) { w += Time.unscaledDeltaTime; yield return null; }
            if (_guardTimer <= 0f) yield break;
            ArenKitVfx.LoopBulwark(ref _bulwark, transform);
            _bulwarkLooping = true;
        }

        void TickGuard()
        {
            if (_guardTimer <= 0f) return;
            _guardTimer -= Time.unscaledDeltaTime;
            if (_guardTimer > 0f) return;
            EndGuard();
        }

        void EndGuard()
        {
            _guardTimer = 0f;
            GetComponent<Health>()?.SetDamageTakenMultiplier(1f);
            if (_bulwarkLooping || _bulwark.back != null || _bulwark.front != null)
                ArenKitVfx.EndBulwark(ref _bulwark, transform);
            _bulwarkLooping = false;
        }

        void BeginCrescentStrike(SkillDef d = null)
        {
            _crescentDef = d;
            _visual?.PlaySlashSwing(_facing);
            _crescentPending = true;
            _crescentDelay = d != null ? d.anticipation : crescentAnticipation;
        }

        void TickCrescentStrike()
        {
            if (!_crescentPending) return;
            _crescentDelay -= Time.unscaledDeltaTime;
            if (_crescentDelay > 0f) return;
            _crescentPending = false;
            var d = _crescentDef;
            Vector2 origin = (Vector2)transform.position + _facing * 0.55f;
            if (ArenKitVfx.Active)
                ArenKitVfx.PlayDawn(transform.position + Vector3.up * 0.4f, _facing);
            else
            {
                CombatVfx.SpawnCrescent(origin, _facing, 2.35f);
                if (!CombatVfx.SpawnSwordSlashFrames(transform.position + Vector3.up * 0.45f, _facing, 2))
                    CombatVfx.SpawnSwordSlash(transform.position + (Vector3)(_facing * 0.4f) + Vector3.up * 0.45f, _facing, 1.7f);
            }
            ApplyAreaDamage(origin, d != null ? d.radius : crescentRadius, d != null ? d.damage : crescentDamage,
                d != null ? d.knockback : crescentKnockback, d != null ? d.popupColor : new Color(0.7f, 0.95f, 1f));
        }

        void ApplyAreaDamage(Vector2 center, float radius, int damage, float knockback, Color popupColor)
        {
            CombatTargeting.OverlapEnemies(center, radius, _hitBuffer, enemyMask);
            int landed = 0;
            foreach (var health in _hitBuffer)
            {
                Vector2 hitPoint = health.transform.position;
                Vector2 dir = hitPoint - (Vector2)transform.position;
                if (dir.sqrMagnitude < 0.001f) dir = _facing;
                dir.Normalize();

                if (!health.TakeDamage(damage, hitPoint, dir * knockback))
                    continue;

                landed++;
                // Popup rises above mob labels; spark is at the hit body (not a second R slash).
                bool finisher = _activeSkill == SkillId.Sword && (_pendingStep == 2 || _pendingVfxScale > 1.2f);
                Vector3 popupAt = (Vector3)hitPoint + Vector3.up * 0.55f + (Vector3)(dir * 0.12f);
                DamagePopup.Spawn(popupAt, damage, popupColor);
                CombatVfx.SpawnHitSpark((Vector3)hitPoint + (Vector3)(dir * 0.05f), finisher);
                HitComboCounter.Instance?.RegisterHit();
            }

            if (landed > 0)
            {
                if (_kit != null) ApplyKitHitFeedback();
                else ApplyHitFeedback(_activeSkill, _activeSkill == SkillId.Sword && _pendingVfxScale > 1.2f);
            }
        }

        void ApplyKitHitFeedback()
        {
            float stop, amp, dur;
            bool finisher = false;
            if (_activeSkill == SkillId.Sword)
            {
                var steps = _kit.combo.steps;
                var s = steps != null && _pendingStep >= 0 && _pendingStep < steps.Length ? steps[_pendingStep] : null;
                stop = s != null ? s.hitstop : 0.048f;
                amp = s != null ? s.shakeAmp : 0.0625f;
                dur = s != null ? s.shakeDur : 0.07f;
                finisher = s != null && s.finisher;
            }
            else
            {
                var d = _kit.Get(_activeSkill);
                stop = d != null ? d.hitstop : 0.05f;
                amp = d != null ? d.shakeAmp : 0.08f;
                dur = d != null ? d.shakeDur : 0.09f;
            }
            if (HitStop.Instance != null && stop > 0f)
                HitStop.Instance.Trigger(stop);
            if (_cameraFollow != null && amp > 0f)
                _cameraFollow.Shake(amp, dur);
            if (_activeSkill == SkillId.Sword)
                GameAudio.Play(finisher ? Sfx.SlashFinisher : Sfx.SlashHit);
            else
                GameAudio.Play(Sfx.SlashHit, 0.6f);
        }

        void ApplyHitFeedback(SkillId skill, bool finisher)
        {
            float stop;
            float shakeAmp;
            float shakeDur;
            switch (skill)
            {
                case SkillId.Sword:
                    stop = finisher ? 0.058f : 0.048f;
                    shakeAmp = finisher ? 0.09f : 0.0625f;
                    shakeDur = finisher ? 0.1f : 0.07f;
                    break;
                case SkillId.Mace:
                    stop = 0.06f; shakeAmp = 0.12f; shakeDur = 0.12f; break;
                case SkillId.FireDash:
                    stop = 0.05f; shakeAmp = 0.08f; shakeDur = 0.09f; break;
                default:
                    stop = 0.05f; shakeAmp = 0.08f; shakeDur = 0.09f; break;
            }

            if (HitStop.Instance != null)
                HitStop.Instance.Trigger(stop);
            if (_cameraFollow != null)
                _cameraFollow.Shake(shakeAmp, shakeDur);

            // One impact sound per cast (not per enemy); skills already played their cast sound.
            if (skill == SkillId.Sword)
                GameAudio.Play(finisher ? Sfx.SlashFinisher : Sfx.SlashHit);
            else
                GameAudio.Play(Sfx.SlashHit, 0.6f);
        }

        public bool DebugForceCast(SkillId id) => TryCast(id);
    }
}
