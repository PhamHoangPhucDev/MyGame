using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Fades a village building while the hero walks behind it (north of its base, inside its
    /// silhouette), the same way TreeCanopyOcclusion fades tree canopies.
    /// </summary>
    public class BuildingFade : MonoBehaviour
    {
        public float coveredAlpha = 0.45f;
        public float fadeSeconds = 0.12f;

        SpriteRenderer _sr;
        Transform _hero;
        float _alpha = 1f;

        void Awake() => _sr = GetComponent<SpriteRenderer>();

        void LateUpdate()
        {
            if (_sr == null || _sr.sprite == null) return;
            if (_hero == null)
            {
                var go = GameObject.FindWithTag("Player");
                if (go == null) return;
                _hero = go.transform;
            }
            var b = _sr.bounds;
            Vector2 h = _hero.position;
            float baseY = transform.position.y;
            bool behind = h.y > baseY + 0.3f && h.y < b.max.y - 0.2f && h.x > b.min.x + 0.2f && h.x < b.max.x - 0.2f;
            float target = behind ? coveredAlpha : 1f;
            float step = fadeSeconds <= 0f ? 1f : Time.deltaTime / fadeSeconds;
            _alpha = Mathf.MoveTowards(_alpha, target, step);
            var c = _sr.color;
            if (!Mathf.Approximately(c.a, _alpha)) { c.a = _alpha; _sr.color = c; }
        }
    }
}
