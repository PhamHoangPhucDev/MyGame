using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>Stage 2 maze root. Stone autotile art is the visual target; this builds the walk grid.</summary>
    public class DungeonFenceMaze : MonoBehaviour
    {
        static DungeonFenceMaze _one;
        const int W = 11;
        const int H = 9;

        // 1 = stone wall. Entrance south, exit north-east, two dead-end pockets.
        static readonly string[] Rows =
        {
            "11111111111",
            "10000000001",
            "10111011101",
            "10100000101",
            "10101110101",
            "10001000101",
            "11101011101",
            "10001000001",
            "11111111111"
        };

        public static void Show()
        {
            if (_one == null)
            {
                var go = new GameObject("FenceMaze");
                _one = go.AddComponent<DungeonFenceMaze>();
                _one.Build();
            }
            _one.gameObject.SetActive(true);
        }

        public static void Hide()
        {
            if (_one != null) _one.gameObject.SetActive(false);
        }

        void Build()
        {
            var wall = Square(new Color(0.45f, 0.42f, 0.38f));
            var spike = Square(new Color(0.55f, 0.18f, 0.16f));
            float originX = -5f;
            float originY = -4f;
            for (int y = 0; y < H; y++)
            {
                for (int x = 0; x < W; x++)
                {
                    if (Rows[y][x] != '1') continue;
                    var cell = new GameObject("Wall");
                    cell.transform.SetParent(transform, false);
                    cell.transform.position = new Vector3(originX + x, originY + (H - 1 - y), 0f);
                    var sr = cell.AddComponent<SpriteRenderer>();
                    sr.sprite = wall;
                    sr.sortingOrder = 4;
                    var box = cell.AddComponent<BoxCollider2D>();
                    box.size = Vector2.one;
                }
            }
            Spike(spike, new Vector2(originX + 3f, originY + 3f));
            Spike(spike, new Vector2(originX + 7f, originY + 5f));
        }

        void Spike(Sprite sprite, Vector2 at)
        {
            var go = new GameObject("Spike");
            go.transform.SetParent(transform, false);
            go.transform.position = at;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 5;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.6f, 0.6f);
            go.AddComponent<DungeonSpike>();
        }

        static Sprite Square(Color color)
        {
            var tex = new Texture2D(16, 16);
            var px = new Color[256];
            for (int i = 0; i < px.Length; i++) px[i] = color;
            tex.SetPixels(px);
            tex.filterMode = FilterMode.Point;
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f);
        }
    }

    public class DungeonSpike : MonoBehaviour
    {
        float _next;

        void OnTriggerStay2D(Collider2D other)
        {
            if (Time.time < _next) return;
            if (other.GetComponentInParent<PlayerMovement>() == null) return;
            var health = other.GetComponentInParent<Health>();
            if (health == null) return;
            int chip = Mathf.Max(1, Mathf.RoundToInt(health.MaxHp * 0.05f));
            health.TakeDamage(chip, (Vector2)transform.position, Vector2.zero);
            _next = Time.time + 0.6f;
        }
    }
}
