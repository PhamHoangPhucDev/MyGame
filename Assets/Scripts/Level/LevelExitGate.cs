using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Exit trigger for trail-style levels. Only fires when LevelFlow marks the exit unlocked.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class LevelExitGate : MonoBehaviour
    {
        [SerializeField] SpriteRenderer visual;
        [SerializeField] Color lockedColor = new Color(0.35f, 0.35f, 0.4f, 0.85f);
        [SerializeField] Color openColor = new Color(0.45f, 0.9f, 0.55f, 0.95f);

        bool _unlocked;
        bool _consumed;

        public bool IsUnlocked => _unlocked;

        void Reset()
        {
            var col = GetComponent<Collider2D>();
            if (col != null) col.isTrigger = true;
        }

        public void Configure(Vector2 position, Vector2 size, bool unlocked)
        {
            transform.position = new Vector3(position.x, position.y, 0f);
            var box = GetComponent<BoxCollider2D>();
            if (box == null) box = gameObject.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            // Collider stays in local space; keep size stable regardless of sprite scale.
            box.size = Vector2.one;
            transform.localScale = new Vector3(Mathf.Max(0.5f, size.x), Mathf.Max(0.5f, size.y), 1f);
            if (visual == null) visual = GetComponent<SpriteRenderer>();
            SetUnlocked(unlocked);
            _consumed = false;
            gameObject.SetActive(true);
        }

        public void SetUnlocked(bool unlocked)
        {
            _unlocked = unlocked;
            if (visual != null)
                visual.color = unlocked ? openColor : lockedColor;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            if (!_unlocked || _consumed) return;
            if (other == null || !other.CompareTag("Player")) return;
            _consumed = true;
            var flow = LevelFlow.Instance != null
                ? LevelFlow.Instance
                : FindFirstObjectByType<LevelFlow>();
            flow?.NotifyExitReached();
        }
    }
}
