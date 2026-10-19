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

        [Header("2 — Dash Shockwave")]
        [SerializeField] float dashCooldown = 4f;
        [SerializeField] float dashSpeed = 14f;
        [SerializeField] float dashDuration = 0.16f;
        [SerializeField] float dashRadius = 1.8f;
        [SerializeField] int dashDamage = 28;

        [Header("3 — Multi Slash AoE")]
        [SerializeField] float multiCooldown = 7f;
        [SerializeField] float multiRadius = 2.4f;
        [SerializeField] int multiDamage = 22;
        [SerializeField] int multiWaves = 3;
        [SerializeField] float multiWaveInterval = 0.12f;

        [Header("4 — Maw Bite")]
        [SerializeField] float biteCooldown = 5f;
        [SerializeField] float biteRadius = 1.6f;
        [SerializeField] int biteDamage = 32;

        [Header("V — Bone Spike")]
        [SerializeField] float boneCooldown = 3.5f;
        [SerializeField] float boneRadius = 1.2f;
        [SerializeField] int boneDamage = 20;

        [Header("C — Mace Smash")]
        [SerializeField] float maceCooldown = 8f;
        [SerializeField] float maceRadius = 2.0f;
        [SerializeField] int maceDamage = 40;

        PlayerMovement _movement;
        PlayerVisual _visual;
        CameraFollow _cameraFollow;
        readonly float[] _cooldownRemaining = new float[6];
        readonly float[] _cooldownTotal = new float[6];
        readonly List<Health> _hitBuffer = new();
        Vector2 _facing = Vector2.right;
        SkillId _selected = SkillId.Sword;
        SkillId _activeSkill = SkillId.Sword;
        float _multiTimer;
        int _multiLeft;

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

            _cooldownTotal[(int)SkillId.FireDash] = dashCooldown;
            _cooldownTotal[(int)SkillId.MultiSlash] = multiCooldown;
            _cooldownTotal[(int)SkillId.MawBite] = biteCooldown;
            _cooldownTotal[(int)SkillId.Sword] = slashCooldown;
            _cooldownTotal[(int)SkillId.Bone] = boneCooldown;
            _cooldownTotal[(int)SkillId.Mace] = maceCooldown;
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
                _multiLeft = 0;
                return;
            }

            TickCooldowns();
            UpdateFacing();
            ReadSkillHotkeys();
            TickMultiSlash();
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
            if (kb.rKey.wasPressedThisFrame || kb.spaceKey.wasPressedThisFrame) TryCast(SkillId.Sword);
            if (kb.vKey.wasPressedThisFrame) TryCast(SkillId.Bone);
            if (kb.cKey.wasPressedThisFrame) TryCast(SkillId.Mace);
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
                case SkillId.FireDash: PerformDash(); break;
                case SkillId.MultiSlash: BeginMultiSlash(); break;
                case SkillId.MawBite: PerformBite(); break;
                case SkillId.Bone: PerformBone(); break;
                case SkillId.Mace: PerformMace(); break;
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
            Vector2 hitOrigin = (Vector2)transform.position + _facing * 0.35f;
            _visual?.PlaySlashSwing(_facing);
            Vector3 slashPos = transform.position + (Vector3)(_facing * 0.9f) + Vector3.up * 0.45f;
            CombatVfx.SpawnCrescent(slashPos, _facing, 2.1f, 0.32f);
            ApplyAreaDamage(hitOrigin, slashRadius, slashDamage, slashKnockback, new Color(0.7f, 0.95f, 1f));
        }

        void PerformDash()
        {
            Vector2 dir = _facing.sqrMagnitude > 0.01f ? _facing : Vector2.right;
            _movement.OverrideVelocity(dir * dashSpeed, dashDuration);
            CombatVfx.SpawnShockwave(transform.position, 1.6f);
            CombatVfx.SpawnCrescent(transform.position + (Vector3)(dir * 0.5f), dir, 1.6f);
            ApplyAreaDamage(transform.position, dashRadius, dashDamage, 4.5f, new Color(0.45f, 0.9f, 1f));
        }

        void BeginMultiSlash()
        {
            _multiLeft = multiWaves;
            _multiTimer = 0f;
            MultiWave();
            _multiLeft--;
        }

        void TickMultiSlash()
        {
            if (_multiLeft <= 0) return;
            _multiTimer -= Time.deltaTime;
            if (_multiTimer > 0f) return;
            MultiWave();
            _multiLeft--;
            _multiTimer = multiWaveInterval;
        }

        void MultiWave()
        {
            _activeSkill = SkillId.MultiSlash;
            _visual?.PlaySlashSwing(_facing);
            CombatVfx.SpawnCrescent(transform.position, _facing, 1.8f);
            CombatVfx.SpawnBurst(transform.position, new Color(0.8f, 0.95f, 1f), 1.4f);
            ApplyAreaDamage(transform.position, multiRadius, multiDamage, 3.2f, new Color(0.85f, 0.95f, 1f));
        }

        void PerformBite()
        {
            CombatVfx.SpawnBurst(transform.position + (Vector3)_facing * 0.4f, new Color(1f, 0.35f, 0.35f), 1.3f);
            ApplyAreaDamage((Vector2)transform.position + _facing * 0.4f, biteRadius, biteDamage, 2.5f, new Color(1f, 0.55f, 0.55f));
        }

        void PerformBone()
        {
            CombatVfx.SpawnImpact((Vector2)transform.position + _facing * 0.9f);
            ApplyAreaDamage((Vector2)transform.position + _facing * 0.9f, boneRadius, boneDamage, 3.5f, new Color(1f, 0.85f, 0.75f));
        }

        void PerformMace()
        {
            CombatVfx.SpawnShockwave(transform.position, 2.0f);
            CombatVfx.SpawnBurst(transform.position, new Color(0.7f, 0.7f, 0.85f), 1.6f);
            ApplyAreaDamage(transform.position, maceRadius, maceDamage, 5.5f, Color.white);
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
                    stop = 0.06f; shakeAmp = 0.1875f; shakeDur = 0.18f; break;
                default:
                    stop = 0.05f; shakeAmp = 0.125f; shakeDur = 0.12f; break;
            }

            if (HitStop.Instance != null)
                HitStop.Instance.Trigger(stop);
            if (_cameraFollow != null)
                _cameraFollow.Shake(shakeAmp, shakeDur);
        }

        public bool DebugForceCast(SkillId id) => TryCast(id);
    }
}
