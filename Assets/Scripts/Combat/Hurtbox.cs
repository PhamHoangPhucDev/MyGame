using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>Body trigger for shots and area skills. The foot collider stays solid for movement.</summary>
    public class Hurtbox : MonoBehaviour
    {
        public static Vector2 SizeFor(string label, bool rider)
        {
            if (!string.IsNullOrEmpty(label))
            {
                if (label.StartsWith("Cyclops")) return new Vector2(2.2f, 3.6f);
                if (label.StartsWith("Warlord")) return new Vector2(1.3f, 1.6f);
            }
            return rider ? new Vector2(1f, 1.3f) : new Vector2(0.6f, 0.9f);
        }

        public static Hurtbox Attach(GameObject host, Vector2 worldSize)
        {
            if (host == null) return null;
            var existing = host.GetComponentInChildren<Hurtbox>();
            if (existing != null) return existing;

            var go = new GameObject("Hurtbox");
            go.layer = host.layer;
            go.transform.SetParent(host.transform, false);
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            Vector3 scale = host.transform.lossyScale;
            float sx = Mathf.Max(0.01f, Mathf.Abs(scale.x));
            float sy = Mathf.Max(0.01f, Mathf.Abs(scale.y));
            box.size = new Vector2(worldSize.x / sx, worldSize.y / sy);
            box.offset = new Vector2(0f, box.size.y * 0.5f);
            return go.AddComponent<Hurtbox>();
        }
    }
}
