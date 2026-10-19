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
                var health = Buffer[i].GetComponentInParent<Health>();
                if (health == null || health.IsDead) continue;
                if (health.GetComponent<EnemyController>() == null) continue;
                float d = ((Vector2)health.transform.position - from).sqrMagnitude;
                if (d < bestDist)
                {
                    bestDist = d;
                    best = health;
                }
            }
            return best;
        }
    }
}
