using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Tracks rapid hits and drives the on-screen "xN HITS" readout.
    /// </summary>
    public class HitComboCounter : MonoBehaviour
    {
        public static HitComboCounter Instance { get; private set; }

        [SerializeField] float comboResetTime = 1.8f;

        int _hits;
        float _timer;

        public int CurrentHits => _hits;
        public event System.Action<int> ComboChanged;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        void Update()
        {
            if (_hits <= 0) return;
            _timer -= Time.unscaledDeltaTime;
            if (_timer <= 0f)
            {
                _hits = 0;
                ComboChanged?.Invoke(0);
            }
        }

        public void RegisterHit()
        {
            _hits++;
            _timer = comboResetTime;
            ComboChanged?.Invoke(_hits);
        }

        public void ResetCombo()
        {
            _hits = 0;
            _timer = 0f;
            ComboChanged?.Invoke(0);
        }
    }
}
