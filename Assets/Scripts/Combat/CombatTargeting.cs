using System.Collections.Generic;
using UnityEngine;

namespace GameplayPrototype
{
    public static class CombatTargeting
    {
        static readonly Collider2D[] Buffer = new Collider2D[64];

        public static int OverlapEnemies(Vector2 center, float radius, List<Health> results, LayerMask mask)
        {
            results.Clear();
            var filter = new ContactFilter2D();
            filter.SetLayerMask(mask);
            filter.useTriggers = true;
            filter.useLayerMask = true;

            int count = Physics2D.OverlapCircle(center, radius, filter, Buffer);
            for (int i = 0; i < count; i++)
            {
                if (!CountsForCombat(Buffer[i])) continue;
                var health = Buffer[i].GetComponentInParent<Health>();
                if (health == null || health.IsDead) continue;
                if (health.GetComponent<EnemyController>() == null) continue;
                if (!results.Contains(health))
                    results.Add(health);
            }
            return results.Count;
        }

        public static Health FindNearestEnemy(Vector2 from, float radius, LayerMask mask)
        {
            var filter = new ContactFilter2D();
            filter.SetLayerMask(mask);
            filter.useTriggers = true;
            filter.useLayerMask = true;

            int count = Physics2D.OverlapCircle(from, radius, filter, Buffer);
            Health best = null;
            float bestDist = float.MaxValue;
            for (int i = 0; i < count; i++)
            {
                if (!CountsForCombat(Buffer[i])) continue;
                var health = Buffer[i].GetComponentInParent<Health>();
                if (health == null || health.IsDead) continue;
                if (health.GetComponent<EnemyController>() == null) continue;
                Vector2 point = Buffer[i].ClosestPoint(from);
                float d = (point - from).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = health;
                }
            }
            return best;
        }

        /// <summary>Keep the locked target while it is alive and in range. Otherwise the nearest body in range.</summary>
        public static Health LockedOrNearest(Health sticky, Vector2 from, float radius, LayerMask mask)
        {
            if (sticky != null && !sticky.IsDead)
            {
                float body = Vector2.Distance(from, BodyPoint(sticky));
                float feet = Vector2.Distance(from, sticky.transform.position);
                if (Mathf.Min(body, feet) <= radius) return sticky;
            }
            return FindNearestEnemy(from, radius, mask);
        }

        /// <summary>Aim a little ahead of a moving target. The lead is capped at one tile.</summary>
        public static Vector2 LeadPoint(Component target, Vector2 from, Vector2 aim, float speed)
        {
            var body = target != null ? target.GetComponent<Rigidbody2D>() : null;
            Vector2 vel = body != null ? body.linearVelocity : Vector2.zero;
            float dist = Vector2.Distance(from, aim);
            float flight = speed > 0.05f ? dist / speed : 0f;
            Vector2 extra = vel * flight;
            if (extra.sqrMagnitude > 1f) extra = extra.normalized;
            return aim + extra;
        }

        /// <summary>Chest of a hurtbox, or a short lift above the feet when none is attached.</summary>
        public static Vector2 BodyPoint(Component target, float lift = 0.45f)
        {
            if (target == null) return Vector2.zero;
            var box = target.GetComponentInChildren<Hurtbox>();
            var col = box != null ? box.GetComponent<Collider2D>() : null;
            if (col != null) return col.bounds.center;
            return (Vector2)target.transform.position + Vector2.up * lift;
        }

        public static Vector2 ShotDirection(Vector2 from, Vector2 aim, Vector2 fallback)
        {
            Vector2 to = aim - from;
            if (to.sqrMagnitude > 0.0001f) return to.normalized;
            return fallback.sqrMagnitude > 0.01f ? fallback.normalized : Vector2.right;
        }

        /// <summary>Foot colliders stay for movement. Combat queries use the body trigger when one exists.</summary>
        static bool CountsForCombat(Collider2D col)
        {
            if (col == null) return false;
            if (col.GetComponent<Hurtbox>() != null) return true;
            return col.GetComponentInChildren<Hurtbox>() == null;
        }
    }
}
