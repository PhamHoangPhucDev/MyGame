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
            if (_active)
                Time.timeScale = 1f;
        }

        void Update()
        {
            if (!_active) return;
            _remaining -= Time.unscaledDeltaTime;
            if (_remaining > 0f) return;
            _active = false;
            _remaining = 0f;
            if (!_blocked)
                Time.timeScale = 1f;
        }

        /// <summary>Freeze briefly. New calls extend to the max remaining, never stack additively.</summary>
        public void Trigger(float seconds)
        {
            if (_blocked || seconds <= 0f) return;
            if (_active)
                _remaining = Mathf.Max(_remaining, seconds);
            else
            {
                _remaining = seconds;
                _active = true;
                Time.timeScale = 0.05f;
            }
        }

        public void CancelAndRestore()
        {
            _active = false;
            _remaining = 0f;
            Time.timeScale = 1f;
        }

        public void SetBlocked(bool blocked)
        {
            _blocked = blocked;
            if (blocked)
                CancelAndRestore();
        }
    }
}
