using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Brief time freeze on hit. Uses unscaled time so it cannot stick at timeScale 0.
    /// </summary>
    public class HitStop : MonoBehaviour
    {
        public static HitStop Instance { get; private set; }

        float _remaining;
        bool _active;
        bool _blocked;
        float _restoreScale = 1f;

        public bool IsActive => _active;
        /// <summary>Measured unscaled seconds of the last completed freeze (set when it ends).</summary>
        public float LastDuration { get; private set; }
        /// <summary>Requested duration of the active or last Trigger (clamped 0.04–0.06).</summary>
        public float LastRequested { get; private set; }
        float _startedUnscaled;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
            if (_active && !_blocked)
                Time.timeScale = _restoreScale > 0f ? _restoreScale : 1f;
        }

        void Update()
        {
            if (!_active) return;
            _remaining -= Time.unscaledDeltaTime;
            if (_remaining > 0f) return;
            _active = false;
            _remaining = 0f;
            LastDuration = Time.unscaledTime - _startedUnscaled;
            if (!_blocked)
                Time.timeScale = _restoreScale > 0f ? _restoreScale : 1f;
        }

        /// <summary>
        /// Freeze 40–60ms of real time. Later hits in the same window keep the longer remaining time.
        /// Does not start if another system already owns timeScale 0, and restores that owner's scale.
        /// </summary>
        public void Trigger(float seconds)
        {
            if (_blocked || seconds <= 0f) return;
            if (!_active && Time.timeScale <= 0f) return;
            seconds = Mathf.Clamp(seconds, 0.035f, 0.08f);
            LastRequested = seconds;
            if (_active)
            {
                // AoE multi-hit: extend remaining, never stack additive freezes.
                _remaining = Mathf.Max(_remaining, seconds);
                return;
            }

            _restoreScale = Time.timeScale;
            _remaining = seconds;
            _startedUnscaled = Time.unscaledTime;
            _active = true;
            Time.timeScale = 0f;
        }

        public void CancelAndRestore()
        {
            _active = false;
            _remaining = 0f;
            if (!_blocked)
                Time.timeScale = _restoreScale > 0f ? _restoreScale : 1f;
        }

        public void SetBlocked(bool blocked)
        {
            _blocked = blocked;
            if (blocked)
                CancelAndRestore();
        }
    }
}
