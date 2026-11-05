using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Loops a village NPC's 4-frame idle (32x32 cells, same grid as Aren).
    /// Each NPC starts at a different phase so they don't breathe in sync.
    /// </summary>
    public class NpcIdle : MonoBehaviour
    {
        public Sprite[] frames;
        public float fps = 3f;
        SpriteRenderer _sr;
        float _phase;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _phase = Random.value * 4f;
        }

        void Update()
        {
            if (_sr == null || frames == null || frames.Length == 0) return;
            int i = (int)((Time.time * fps + _phase) % frames.Length);
            if (frames[i] != null) _sr.sprite = frames[i];
        }

        public static Sprite[] Slice(Sprite sheet)
        {
            if (sheet == null) return null;
            var tex = sheet.texture;
            var r = sheet.rect;
            int n = Mathf.Max(1, Mathf.RoundToInt(r.width / 32f));
            var result = new Sprite[n];
            // pivot on the outline row under the feet (1 px above the cell bottom)
            for (int i = 0; i < n; i++)
            {
                result[i] = Sprite.Create(tex, new Rect(r.x + i * 32, r.y, 32, 32), new Vector2(0.5f, 1f / 32f), 16f, 0, SpriteMeshType.FullRect);
                result[i].name = sheet.name + "_" + i;
            }
            return result;
        }
    }
}
