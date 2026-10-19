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
        Vector2 _slashSwing;
        Health _stickyTarget;
        bool _debugSwordHeld;

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

        void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            _visual = GetComponent<PlayerVisual>();
            if (_visual == null) _visual = gameObject.AddComponent<PlayerVisual>();
            _visual.EnsureHierarchy();

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

            _activeSkill = id;
            switch (id)
            {
                case SkillId.Sword: PerformSlash(); break;
                case SkillId.FireDash: PerformFlameBurst(); break;
                case SkillId.MultiSlash: PerformDashSlash(); break;
                case SkillId.MawBite: BeginWhirlwind(); break;
                case SkillId.Bone: BeginGuard(); break;
                case SkillId.Mace: BeginCrescentStrike(); break;
            }

            StartCooldown(id);
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
            if (Time.unscaledTime - _lastSlashUnscaled > SlashChainReset)
                _slashChain = 0;

            AimSlash();
            Vector2 perp = new Vector2(-_slashFacing.y, _slashFacing.x);
            _pendingRadius = slashRadius;
            _pendingDamage = slashDamage;
            _pendingKnockback = slashKnockback;
            _pendingVfxScale = 1f;
            if (_slashChain == 0)
                _slashSwing = (_slashFacing + perp * 0.85f).normalized;
            else if (_slashChain == 1)
                _slashSwing = (_slashFacing - perp * 0.85f).normalized;
            else
            {
                // Finisher: wider arc, stronger push, larger cyan slash.
                _slashSwing = _slashFacing;
                _pendingRadius = slashRadius * 1.35f;
                _pendingKnockback = slashKnockback * 1.25f;
                _pendingVfxScale = 1.45f;
            }

            _slashChain = (_slashChain + 1) % 3;
            _lastSlashUnscaled = Time.unscaledTime;
            _visual?.PlaySlashSwing(_slashSwing);
            _slashPending = true;
            _slashDelay = 0.07f;
        }

        void AimSlash()
        {
            // Keep sticky target while it stays alive and in range so facing does not flicker.
            if (_stickyTarget != null && !_stickyTarget.IsDead)
            {
                float d = Vector2.Distance(transform.position, _stickyTarget.transform.position);
                if (d <= slashAutoTargetRadius)
                {
                    Vector2 toSticky = (Vector2)_stickyTarget.transform.position - (Vector2)transform.position;
                    if (toSticky.sqrMagnitude > 0.0001f)
                        _facing = toSticky.normalized;
                    _slashFacing = _facing;
                    return;
                }
            }

            _stickyTarget = CombatTargeting.FindNearestEnemy(transform.position, slashAutoTargetRadius, enemyMask);
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
            _slashDelay -= Time.unscaledDeltaTime;
            if (_slashDelay > 0f) return;
            _slashPending = false;
            Vector2 swing = _slashSwing.sqrMagnitude > 0.01f ? _slashSwing : _slashFacing;
            Vector2 hitOrigin = (Vector2)transform.position + swing * 0.35f;
            Vector3 slashPos = transform.position + (Vector3)(swing * 0.28f) + Vector3.up * 0.55f;
            CombatVfx.SpawnSwordSlash(slashPos, swing, _pendingVfxScale);
            ApplyAreaDamage(hitOrigin, _pendingRadius, _pendingDamage, _pendingKnockback, new Color(0.7f, 0.95f, 1f));
        }

        void PerformFlameBurst()
        {
            Vector2 origin = (Vector2)transform.position + _facing * flameRange;
            CombatVfx.SpawnBurst(origin, new Color(1f, 0.45f, 0.15f), 1.55f);
            CombatVfx.SpawnBurst(origin + _facing * 0.35f, new Color(1f, 0.75f, 0.2f), 1.1f);
            ApplyAreaDamage(origin, flameRadius, flameDamage, 2.8f, new Color(1f, 0.55f, 0.25f));
        }

        void PerformDashSlash()
        {
            Vector2 dir = _facing.sqrMagnitude > 0.01f ? _facing : Vector2.right;
            _movement.OverrideVelocity(dir * dashSpeed, dashDuration);
            CombatVfx.SpawnCrescent(transform.position + (Vector3)(dir * 0.35f), dir, 1.5f);
            CombatVfx.SpawnShockwave(transform.position, 1.25f);
            ApplyAreaDamage((Vector2)transform.position + dir * 0.4f, dashRadius, dashDamage, 4.2f, new Color(0.55f, 0.9f, 1f));
        }

        void BeginWhirlwind()
        {
            _whirlLeft = whirlWaves;
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
            _whirlTimer = whirlWaveInterval;
        }

        void WhirlWave()
        {
            _activeSkill = SkillId.MawBite;
            _visual?.PlaySlashSwing(_facing);
            CombatVfx.SpawnCrescent(transform.position, _facing, 2.0f);
            CombatVfx.SpawnCrescent(transform.position, -_facing, 1.7f);
            CombatVfx.SpawnBurst(transform.position, new Color(0.75f, 0.95f, 1f), 1.5f);
            ApplyAreaDamage(transform.position, whirlRadius, whirlDamage, 3.0f, new Color(0.8f, 0.95f, 1f));
        }

        void BeginGuard()
        {
            _guardTimer = guardDuration;
            var health = GetComponent<Health>();
            health?.SetDamageTakenMultiplier(guardDamageMultiplier);
            CombatVfx.SpawnShockwave(transform.position, 1.1f);
            CombatVfx.SpawnBurst(transform.position, new Color(0.65f, 0.85f, 1f), 0.9f);
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
        }

        void BeginCrescentStrike()
        {
            _visual?.PlaySlashSwing(_facing);
            _crescentPending = true;
            _crescentDelay = crescentAnticipation;
        }

        void TickCrescentStrike()
        {
            if (!_crescentPending) return;
            _crescentDelay -= Time.unscaledDeltaTime;
            if (_crescentDelay > 0f) return;
            _crescentPending = false;
            Vector2 origin = (Vector2)transform.position + _facing * 0.55f;
            CombatVfx.SpawnCrescent(origin, _facing, 2.35f);
            CombatVfx.SpawnSwordSlash(transform.position + (Vector3)(_facing * 0.4f) + Vector3.up * 0.45f, _facing, 1.7f);
            ApplyAreaDamage(origin, crescentRadius, crescentDamage, crescentKnockback, new Color(0.7f, 0.95f, 1f));
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
                DamagePopup.Spawn(hitPoint + Vector2.up * 0.25f, damage, popupColor);
                CombatVfx.SpawnImpact(hitPoint);
                HitComboCounter.Instance?.RegisterHit();
            }

            if (landed > 0)
                ApplyHitFeedback(_activeSkill);
        }

        void ApplyHitFeedback(SkillId skill)
        {
            float stop;
            float shakeAmp;
            float shakeDur;
            switch (skill)
            {
                case SkillId.Sword:
                    stop = 0.045f; shakeAmp = 0.0625f; shakeDur = 0.08f; break;
                case SkillId.Mace:
                    stop = 0.065f; shakeAmp = 0.16f; shakeDur = 0.16f; break;
                case SkillId.FireDash:
                    stop = 0.05f; shakeAmp = 0.1f; shakeDur = 0.1f; break;
                default:
                    stop = 0.05f; shakeAmp = 0.11f; shakeDur = 0.11f; break;
            }

            if (HitStop.Instance != null)
                HitStop.Instance.Trigger(stop);
            if (_cameraFollow != null)
                _cameraFollow.Shake(shakeAmp, shakeDur);
        }

        public bool DebugForceCast(SkillId id) => TryCast(id);
    }
}
