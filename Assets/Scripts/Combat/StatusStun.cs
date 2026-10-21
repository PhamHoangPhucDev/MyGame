using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// R3D.1: short stun for Goblin / Gobrider (Shield Bash). Refresh duration on re-apply; never stacks layers.
    /// Disables EnemyController movement/attacks while active; plays Aren_StunStars above the head.
    /// </summary>
    public class StatusStun : MonoBehaviour
    {
        float _remaining;
        EnemyController _ai;
        bool _aiWasEnabled;
        KitVfx _stars;
        Health _health;

        public static bool IsStunned(Component c) =>
            c != null && c.TryGetComponent<StatusStun>(out var s) && s._remaining > 0f;

        public static void Apply(Health target, float duration)
        {
            if (target == null || target.IsDead || duration <= 0f) return;
            // Only humanoid camp mobs for this pass.
            if (!IsEligible(target)) return;

            var s = target.GetComponent<StatusStun>();
            if (s == null) s = target.gameObject.AddComponent<StatusStun>();
            s._health = target;
            // Cap refresh — no infinite stack; take max remaining.
            s._remaining = Mathf.Max(s._remaining, duration);
            s.EnsureAiPaused();
            s.EnsureStars();
            s.enabled = true;
        }

        static bool IsEligible(Health h)
        {
            string n = h.gameObject.name;
            return n.IndexOf("Goblin", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("Gobrider", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("GobRider", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        /// <summary>Suggested durations from R3D.1 brief.</summary>
        public static float DurationFor(Health target)
        {
            if (target == null) return 0.3f;
            string n = target.gameObject.name;
            if (n.IndexOf("Gobrider", System.StringComparison.OrdinalIgnoreCase) >= 0
                || n.IndexOf("GobRider", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return 0.2f;
            return 0.32f; // Goblin 0.25–0.40
        }

        void EnsureAiPaused()
        {
            _ai = GetComponent<EnemyController>();
            if (_ai == null) return;
            if (!_aiWasEnabled && _ai.enabled)
            {
                _aiWasEnabled = true;
                _ai.enabled = false;
                var rb = GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }
        }

        void EnsureStars()
        {
            if (_stars != null) return;
            var frames = ArenKitVfx.StunStarFrames;
            if (frames == null || frames.Length == 0) return;
            _stars = KitVfx.PlayAttached(frames, transform, Vector3.up * 0.9f, 8f, false, 80, true);
        }

        void Update()
        {
            if (_health == null) _health = GetComponent<Health>();
            if (_health == null || _health.IsDead)
            {
                Release();
                return;
            }

            _remaining -= Time.deltaTime;
            if (_remaining > 0f) return;
            Release();
        }

        void Release()
        {
            if (_ai != null && _aiWasEnabled)
            {
                _ai.enabled = true;
                _aiWasEnabled = false;
            }
            if (_stars != null)
            {
                Destroy(_stars.gameObject);
                _stars = null;
            }
            _remaining = 0f;
            Destroy(this);
        }

        void OnDisable()
        {
            // Despawn / death — restore AI flag so we don't leave it disabled if object is reused.
            if (_ai != null && _aiWasEnabled)
            {
                _ai.enabled = true;
                _aiWasEnabled = false;
            }
        }
    }
}
