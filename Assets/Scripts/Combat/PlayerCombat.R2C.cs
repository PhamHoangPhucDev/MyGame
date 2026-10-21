using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameplayPrototype
{
    public partial class PlayerCombat
    {
        void FireKitArrow(ComboStep step, int damage)
        {
            var poses = _kit != null && _kit.combo != null ? _kit.combo.projectilePoses : null;
            var hits = _kit != null && _kit.combo != null ? _kit.combo.projectileHit : null;
            Vector2 dir = _slashFacing.sqrMagnitude > 0.01f ? _slashFacing : _facing;
            int explodeDamage = step.shape == ComboHitShape.Projectile && step.radiusMul > 1.01f ? damage : 0;
            float explode = explodeDamage > 0 ? _pendingRadius : 0f;
            KitProjectile.Fire((Vector2)transform.position + Vector2.up * 0.4f + dir * 0.2f, dir, poses,
                step.projectileSpeed, step.projectileRange, explodeDamage > 0 ? 0 : damage, _pendingKnockback,
                0, 0.5f, 0f, enemyMask, new Color(0.85f, 1f, 0.7f), hits, step.pierce, explode, hits, step.arrowRow, explodeDamage);
        }

        void BeginGroundSlam(SkillDef d)
        {
            if (!_slashPending) _pendingStep = -1;
            _visual?.PlaySlashSwing(_facing, false, 0.42f);
            StartCoroutine(After(d.anticipation, () =>
            {
                KitVfx.Play(d.vfxFrames, transform.position, d.vfxFps > 1f ? d.vfxFps : 14f, false, -30);
                ApplyAreaDamage(transform.position, d.radius, d.damage, d.knockback, d.popupColor);
                StunInRadius(transform.position, d.radius, d.stunDuration);
            }));
        }

        void BeginShieldCharge(SkillDef d)
        {
            if (!_slashPending) _pendingStep = -1;
            Vector2 dir = _facing.sqrMagnitude > 0.01f ? _facing.normalized : Vector2.right;
            float dur = d.duration > 0.05f ? d.duration : 0.28f;
            GetComponent<PlayerMovement>()?.OverrideVelocity(dir * (d.dashDistance / dur), dur);
            StartCoroutine(ChargeHits(d, dir, dur));
        }

        IEnumerator ChargeHits(SkillDef d, Vector2 dir, float dur)
        {
            var hit = new HashSet<Health>();
            float t = 0f;
            while (t < dur)
            {
                KitVfx.Play(d.vfxFrames, transform.position, d.vfxFps > 1f ? d.vfxFps : 12f, false, -15);
                var list = new List<Health>();
                CombatTargeting.OverlapEnemies((Vector2)transform.position + dir * 0.4f, d.radius, list, enemyMask);
                foreach (var h in list)
                {
                    if (!hit.Add(h)) continue;
                    Vector2 side = new Vector2(-dir.y, dir.x);
                    float sign = Mathf.Sign(Vector2.Dot((Vector2)h.transform.position - (Vector2)transform.position, side));
                    if (Mathf.Abs(sign) < 0.01f) sign = 1f;
                    h.TakeDamage(d.damage, h.transform.position, side * sign * d.knockback);
                    if (h.LastDamageApplied > 0)
                        DamagePopup.Spawn(h.transform.position + Vector3.up * 0.4f, h.LastDamageApplied, d.popupColor);
                }
                t += 0.08f;
                yield return new WaitForSeconds(0.08f);
            }
            if (hit.Count > 0) ApplyKitHitFeedback();
        }

        void BeginTaunt(SkillDef d)
        {
            if (!_slashPending) _pendingStep = -1;
            _visual?.PlaySlashSwing(_facing, false, 0.3f);
            KitVfx.Play(d.vfxFrames, transform.position + Vector3.up * 0.2f, d.vfxFps > 1f ? d.vfxFps : 12f, false, 40);
            var list = new List<Health>();
            CombatTargeting.OverlapEnemies(transform.position, d.radius, list, enemyMask);
            foreach (var h in list)
                StatusTaunt.Apply(h, transform, d.duration, d.projectilePoses);
            GetComponent<Health>()?.SetExtraDamageTaken(d.multiplier > 0f ? d.multiplier : 0.8f);
            StartCoroutine(After(d.duration, () => GetComponent<Health>()?.SetExtraDamageTaken(1f)));
            if (d.shakeAmp > 0f) _cameraFollow?.Shake(d.shakeAmp, d.shakeDur);
        }

        void BeginIronWall(SkillDef d)
        {
            var move = GetComponent<PlayerMovement>();
            move?.SetMovementLocked(true);
            GetComponent<Health>()?.SetDamageTakenMultiplier(d.multiplier);
            StartCoroutine(IronWallRoutine(d, move));
        }

        IEnumerator IronWallRoutine(SkillDef d, PlayerMovement move)
        {
            var frames = d.vfxFrames;
            int half = frames != null ? frames.Length / 2 : 0;
            Sprite[] back = half > 0 ? Slice(frames, 0, half) : frames;
            Sprite[] front = half > 0 ? Slice(frames, half, half) : null;
            var rear = KitVfx.PlayAttached(back, transform, Vector3.zero, 8f, false, -8, true);
            var fore = front != null ? KitVfx.PlayAttached(front, transform, Vector3.zero, 8f, false, 30, true) : null;
            yield return new WaitForSeconds(d.duration);
            if (rear != null) Destroy(rear.gameObject);
            if (fore != null) Destroy(fore.gameObject);
            move?.SetMovementLocked(false);
            if (_stealthTimer <= 0f) GetComponent<Health>()?.SetDamageTakenMultiplier(1f);
        }

        void BeginQuake(SkillDef d)
        {
            if (!_slashPending) _pendingStep = -1;
            StartCoroutine(QuakeRoutine(d));
        }

        IEnumerator QuakeRoutine(SkillDef d)
        {
            int waves = Mathf.Max(1, d.count);
            for (int i = 0; i < waves; i++)
            {
                _visual?.PlaySlashSwing(_facing, false, 0.3f);
                float radius = d.radius + i * 1f;
                if (d.vfxFrames != null && d.vfxFrames.Length >= 2)
                {
                    int row = Mathf.Min(i, d.vfxFrames.Length / 2 - 1);
                    KitVfx.Play(new[] { d.vfxFrames[row * 2], d.vfxFrames[row * 2 + 1] }, transform.position, 8f, false, -30);
                }
                ApplyAreaDamage(transform.position, radius, d.damage, d.knockback, d.popupColor);
                if (i < waves - 1) yield return new WaitForSeconds(d.interval);
            }
        }

        void BeginLeapBack(SkillDef d)
        {
            Vector2 dir = _facing.sqrMagnitude > 0.01f ? _facing.normalized : Vector2.down;
            Vector2 from = transform.position;
            GetComponent<PlayerMovement>()?.OverrideVelocity(-dir * (3f / 0.22f), 0.22f);
            KitVfx.Play(d.vfxFrames, from, 16f, false, 20);
            var zone = new GameObject("VineSnare");
            zone.transform.position = from;
            var slow = zone.AddComponent<R2CSlowZone>();
            slow.Configure(d.radius > 0.1f ? d.radius : 1.5f, 0.5f, d.duration > 0.1f ? d.duration : 2f, enemyMask);
            if (d.projectilePoses != null && d.projectilePoses.Length > 0)
                KitVfx.PlayAttached(d.projectilePoses, zone.transform, Vector3.zero, 5f, false, -25, true);
        }

        void BeginArrowRain(SkillDef d)
        {
            Vector2 dir = _facing.sqrMagnitude > 0.01f ? _facing.normalized : Vector2.down;
            Vector2 center = (Vector2)transform.position + dir * 4f;
            StartCoroutine(RainRoutine(d, center));
        }

        IEnumerator RainRoutine(SkillDef d, Vector2 center)
        {
            if (d.vfxFrames != null && d.vfxFrames.Length > 0)
                KitVfx.PlayAttached(d.vfxFrames, NewAnchor(center), Vector3.zero, 6f, false, -20, true);
            int waves = Mathf.Max(1, d.count);
            for (int i = 0; i < waves; i++)
            {
                ApplyAreaDamage(center, d.radius, d.damage, d.knockback, d.popupColor);
                if (i < waves - 1) yield return new WaitForSeconds(d.interval > 0.01f ? d.interval : 0.2f);
            }
        }

        void BeginWindStep(SkillDef d)
        {
            var move = GetComponent<PlayerMovement>();
            StatusHaste.Apply(move, 1.4f, d.duration > 0.1f ? d.duration : 3f);
            var health = GetComponent<Health>();
            health?.ArmDodge();
            if (d.vfxFrames != null && d.vfxFrames.Length > 0)
                KitVfx.PlayAttached(d.vfxFrames, transform, Vector3.zero, d.vfxFps > 1f ? d.vfxFps : 12f, false, -5, true);
            if (health != null)
            {
                Sprite[] puff = d.projectilePoses;
                health.Dodged += OnDodge;
                void OnDodge(Health h)
                {
                    h.Dodged -= OnDodge;
                    if (puff != null && puff.Length > 0)
                        KitVfx.Play(puff, transform.position, 16f, false, 40);
                }
            }
        }

        void BeginPierceBeam(SkillDef d)
        {
            if (!_slashPending) _pendingStep = -1;
            _visual?.PlaySlashSwing(_facing, false, 0.4f);
            Vector2 dir = _facing.sqrMagnitude > 0.01f ? _facing.normalized : Vector2.right;
            var hit = new HashSet<Health>();
            var list = new List<Health>();
            for (float dist = 0.4f; dist <= 9f; dist += 0.45f)
            {
                CombatTargeting.OverlapEnemies((Vector2)transform.position + dir * dist, 0.5f, list, enemyMask);
                foreach (var h in list)
                {
                    if (!hit.Add(h)) continue;
                    h.TakeDamage(d.damage, h.transform.position, dir * d.knockback);
                    if (h.LastDamageApplied > 0)
                        DamagePopup.Spawn(h.transform.position + Vector3.up * 0.4f, h.LastDamageApplied, d.popupColor);
                }
            }
            if (d.vfxFrames != null && d.vfxFrames.Length > 0)
                KitVfx.Play(d.vfxFrames, (Vector2)transform.position + dir * 4.5f + Vector2.up * 0.4f, 20f, dir.x < 0f, 35);
            if (hit.Count > 0) ApplyKitHitFeedback();
            else if (d.shakeAmp > 0f) _cameraFollow?.Shake(d.shakeAmp, d.shakeDur);
        }

        void BeginSlowOrb(SkillDef d)
        {
            Vector2 dir = _facing.sqrMagnitude > 0.01f ? _facing.normalized : Vector2.right;
            KitProjectile.Fire((Vector2)transform.position + Vector2.up * 0.5f + dir * 0.3f, dir, d.projectilePoses,
                d.speed, d.range, 0, d.knockback, 0, 0.5f, 0f, enemyMask, d.popupColor, null, 1, d.radius, d.vfxFrames, 0, d.damage);
            _visual?.PlaySlashSwing(dir, false, 0.35f);
        }

        void BeginBlink(SkillDef d)
        {
            Vector2 dir = _facing.sqrMagnitude > 0.01f ? _facing.normalized : Vector2.right;
            float dist = d.dashDistance > 0.1f ? d.dashDistance : 3.5f;
            Vector2 from = transform.position;
            var hit = Physics2D.CircleCast(from, 0.2f, dir, dist);
            float travel = dist;
            if (hit.collider != null && hit.collider.GetComponentInParent<PlayerCombat>() == null && hit.collider.GetComponentInParent<EnemyController>() == null)
                travel = Mathf.Max(0.2f, hit.distance - 0.25f);
            Vector2 to = from + dir * travel;
            if (d.vfxFrames != null && d.vfxFrames.Length > 0)
                KitVfx.Play(d.vfxFrames, from, 20f, false, 30);
            transform.position = to;
            GetComponent<Rigidbody2D>().position = to;
            if (d.projectilePoses != null && d.projectilePoses.Length > 0)
                KitVfx.Play(d.projectilePoses, to, 20f, false, 30);
            else if (d.vfxFrames != null)
                KitVfx.Play(d.vfxFrames, to, 20f, false, 30);
            GetComponent<Health>()?.GrantInvulnerability(0.1f);
        }

        void BeginRuneZone(SkillDef d)
        {
            var target = CombatTargeting.FindNearestEnemy(transform.position, d.range > 0.1f ? d.range : 5.5f, enemyMask);
            Vector2 dir = _facing.sqrMagnitude > 0.01f ? _facing.normalized : Vector2.down;
            Vector2 center = target != null ? (Vector2)target.transform.position : (Vector2)transform.position + dir * 3f;
            if (d.vfxFrames != null && d.vfxFrames.Length > 0)
                KitVfx.PlayAttached(d.vfxFrames, NewAnchor(center), Vector3.zero, 10f, false, -22, true);
            var zone = new GameObject("RuneZone");
            zone.transform.position = center;
            zone.AddComponent<R2CSlowZone>().Configure(d.radius, 0.6f, d.duration, enemyMask);
            StartCoroutine(TickZone(d, center));
        }

        IEnumerator TickZone(SkillDef d, Vector2 center)
        {
            int ticks = Mathf.Max(1, d.count);
            for (int i = 0; i < ticks; i++)
            {
                ApplyAreaDamage(center, d.radius, d.damage, 0.2f, d.popupColor);
                if (i < ticks - 1) yield return new WaitForSeconds(d.interval > 0.01f ? d.interval : 0.5f);
            }
        }

        void BeginManaShield(SkillDef d)
        {
            GetComponent<Health>()?.SetShield(d.damage > 0 ? d.damage : 40, d.duration > 0.1f ? d.duration : 4f);
            if (d.vfxFrames != null && d.vfxFrames.Length > 0)
                KitVfx.PlayAttached(d.vfxFrames, transform, Vector3.up * 0.4f, 8f, false, 25, true);
        }

        void BeginMeteor(SkillDef d)
        {
            var target = CombatTargeting.FindNearestEnemy(transform.position, 8f, enemyMask);
            Vector2 dir = _facing.sqrMagnitude > 0.01f ? _facing.normalized : Vector2.down;
            Vector2 center = target != null ? (Vector2)target.transform.position : (Vector2)transform.position + dir * 4f;
            StartCoroutine(MeteorRoutine(d, center));
        }

        IEnumerator MeteorRoutine(SkillDef d, Vector2 center)
        {
            if (d.vfxFrames != null && d.vfxFrames.Length > 0)
                KitVfx.PlayAttached(d.vfxFrames, NewAnchor(center), Vector3.zero, 6f, false, -20, true);
            int stars = Mathf.Max(1, d.count);
            for (int i = 0; i < stars; i++)
            {
                Vector2 at = center + Random.insideUnitCircle * d.radius;
                if (d.projectilePoses != null && d.projectilePoses.Length > 0)
                    KitVfx.Play(d.projectilePoses, at, 12f, false, 40);
                ApplyAreaDamage(at, 0.8f, d.damage, d.knockback, d.popupColor);
                if (i == 0) KitVfx.ScreenTint(new Color(0.45f, 0.2f, 0.7f, 0.08f), 0.1f);
                if (i < stars - 1) yield return new WaitForSeconds(d.interval > 0.01f ? d.interval : 0.3f);
            }
        }

        void StunInRadius(Vector2 center, float radius, float seconds)
        {
            var list = new List<Health>();
            CombatTargeting.OverlapEnemies(center, radius, list, enemyMask);
            foreach (var h in list)
                StatusStun.Apply(h, seconds);
        }

        IEnumerator After(float seconds, System.Action then)
        {
            if (seconds > 0f) yield return new WaitForSeconds(seconds);
            then?.Invoke();
        }

        static Sprite[] Slice(Sprite[] all, int start, int count)
        {
            var a = new Sprite[count];
            for (int i = 0; i < count; i++)
                a[i] = start + i < all.Length ? all[start + i] : null;
            return a;
        }

        static Transform NewAnchor(Vector2 pos)
        {
            var go = new GameObject("R2CAnchor");
            go.transform.position = pos;
            Object.Destroy(go, 4f);
            return go.transform;
        }
    }
}
