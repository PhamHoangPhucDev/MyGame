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

        /// <summary>Foot colliders stay for movement. Combat queries use the body trigger when one exists.</summary>
        static bool CountsForCombat(Collider2D col)
        {
            if (col == null) return false;
            if (col.GetComponent<Hurtbox>() != null) return true;
            return col.GetComponentInChildren<Hurtbox>() == null;
        }
    }
}
