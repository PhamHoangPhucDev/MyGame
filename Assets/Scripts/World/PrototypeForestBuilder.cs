using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Builds a simple forest camp layout from placeholder sprites for Stage 1.
    /// Runs once in Edit Mode via context menu or at Awake if scene is empty.
    /// </summary>
    public class PrototypeForestBuilder : MonoBehaviour
    {
        [SerializeField] bool buildOnAwake = false;
        [SerializeField] Transform parentRoot;

        public void BuildForest()
        {
            if (parentRoot == null)
            {
                var existing = transform.Find("ForestRoot");
                parentRoot = existing != null
                    ? existing
                    : new GameObject("ForestRoot").transform;
                parentRoot.SetParent(transform, false);
            }

            ClearChildren(parentRoot);

            // Ground tiles
            for (int x = -10; x <= 10; x++)
            {
                for (int y = -14; y <= 14; y++)
                {
                    bool dirt = Mathf.Abs(x) <= 2 && Mathf.Abs(y) <= 6;
                    bool darkPatch = !dirt && ((x + y * 3) % 7 == 0);
                    string sprite = dirt ? "DirtPath" : (darkPatch ? "GrassDark" : "Grass");
                    Spawn(sprite, new Vector3(x * 0.9f, y * 0.9f, 0f), 0, parentRoot, "Ground");
                }
            }

            // Trees (obstacles)
            Vector2[] trees =
            {
                new(-6.5f, 4f), new(-7.2f, -2f), new(-5.8f, -7f),
                new(6.2f, 5f), new(7f, -1.5f), new(5.5f, -6.5f),
                new(-3f, 9f), new(2.5f, 10f), new(0f, -11f),
                new(-8f, 8f), new(8f, 8f), new(-8f, -10f), new(8f, -9f)
            };
            foreach (var p in trees)
                Spawn("Tree", p, 5, parentRoot, "Props", true, new Vector2(0.6f, 0.35f), new Vector2(0f, -0.55f));

            // Bushes
            Vector2[] bushes =
            {
                new(-4f, 2f), new(-2.5f, -4f), new(3.2f, 3.5f),
                new(4.5f, -3f), new(-1f, 6.5f), new(1.5f, -8f),
                new(-5.5f, -5f), new(5.8f, 1f)
            };
            foreach (var p in bushes)
                Spawn("Bush", p, 3, parentRoot, "Props", true, new Vector2(0.55f, 0.35f), Vector2.zero);

            // Camp tents
            Spawn("Tent", new Vector3(-2.2f, 2.8f, 0f), 8, parentRoot, "Props", true, new Vector2(1.1f, 0.45f), new Vector2(0f, -0.5f));
            Spawn("Tent", new Vector3(2.4f, 1.5f, 0f), 8, parentRoot, "Props", true, new Vector2(1.1f, 0.45f), new Vector2(0f, -0.5f));

            // Camp props
            Spawn("Cauldron", new Vector3(0.2f, 0.4f, 0f), 6, parentRoot, "Props", true, new Vector2(0.55f, 0.4f), Vector2.zero);
            Spawn("Flag", new Vector3(-0.8f, 3.6f, 0f), 7, parentRoot, "Props", true, new Vector2(0.15f, 0.2f), new Vector2(0f, -0.4f));
            Spawn("Bones", new Vector3(1.2f, -0.8f, 0f), 2, parentRoot, "Props", false);
            Spawn("Bones", new Vector3(-3.4f, 0.6f, 0f), 2, parentRoot, "Props", false);

            // Border rocks / cliffs as blockers
            for (int i = -9; i <= 9; i++)
            {
                Spawn("Rock", new Vector3(i * 0.95f, 12.5f, 0f), 4, parentRoot, "Props", true, new Vector2(0.7f, 0.4f), Vector2.zero);
                Spawn("Rock", new Vector3(i * 0.95f, -12.5f, 0f), 4, parentRoot, "Props", true, new Vector2(0.7f, 0.4f), Vector2.zero);
            }
            for (int i = -12; i <= 12; i++)
            {
                Spawn("Rock", new Vector3(-9.5f, i * 0.95f, 0f), 4, parentRoot, "Props", true, new Vector2(0.5f, 0.55f), Vector2.zero);
                Spawn("Rock", new Vector3(9.5f, i * 0.95f, 0f), 4, parentRoot, "Props", true, new Vector2(0.5f, 0.55f), Vector2.zero);
            }
        }

        void Awake()
        {
            if (buildOnAwake && (parentRoot == null || parentRoot.childCount == 0))
                BuildForest();
        }

        GameObject Spawn(string spriteName, Vector3 position, int sortingOrder, Transform parent, string layerName,
            bool collider = false, Vector2? colliderSize = null, Vector2? colliderOffset = null)
        {
            var go = new GameObject(spriteName);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.layer = LayerMask.NameToLayer("Default");

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>($"Placeholders/{spriteName}");
            sr.sortingLayerName = "Default";
            sr.sortingOrder = sortingOrder;
            if (sr.sprite == null)
            {
                // Fallback colored quad via generated runtime sprite if Resources missing.
                sr.sprite = CreateSolidSprite(ColorFor(spriteName), SizeFor(spriteName));
                sr.color = Color.white;
            }

            // Y-sort approximation for top-down
            var sort = go.AddComponent<SpriteSorter>();
            sort.baseOrder = sortingOrder;

            if (collider)
            {
                var box = go.AddComponent<BoxCollider2D>();
                box.size = colliderSize ?? new Vector2(0.6f, 0.5f);
                box.offset = colliderOffset ?? Vector2.zero;
            }

            return go;
        }

        static void ClearChildren(Transform root)
        {
            for (int i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i).gameObject;
                if (Application.isPlaying)
                    Object.Destroy(child);
                else
                    Object.DestroyImmediate(child);
            }
        }

        static Color ColorFor(string name) => name switch
        {
            "Grass" => new Color(0.55f, 0.72f, 0.38f),
            "GrassDark" => new Color(0.42f, 0.58f, 0.30f),
            "DirtPath" => new Color(0.62f, 0.55f, 0.40f),
            "Tree" => new Color(0.28f, 0.52f, 0.28f),
            "Bush" => new Color(0.30f, 0.48f, 0.24f),
            "Tent" => new Color(0.35f, 0.62f, 0.32f),
            "Cauldron" => new Color(0.45f, 0.32f, 0.22f),
            "Flag" => new Color(0.35f, 0.18f, 0.35f),
            "Bones" => new Color(0.85f, 0.82f, 0.72f),
            "Rock" => new Color(0.40f, 0.36f, 0.32f),
            "Player" => new Color(0.92f, 0.92f, 0.95f),
            _ => Color.magenta
        };

        static Vector2Int SizeFor(string name) => name switch
        {
            "Tree" => new Vector2Int(24, 32),
            "Tent" => new Vector2Int(40, 36),
            "Bush" => new Vector2Int(18, 14),
            "Cauldron" => new Vector2Int(18, 16),
            "Flag" => new Vector2Int(10, 28),
            "Bones" => new Vector2Int(16, 10),
            "Rock" => new Vector2Int(18, 14),
            "Player" => new Vector2Int(16, 20),
            _ => new Vector2Int(16, 16)
        };

        static Sprite CreateSolidSprite(Color color, Vector2Int size)
        {
            var tex = new Texture2D(size.x, size.y, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                name = $"Runtime_{color}"
            };

            var pixels = new Color[size.x * size.y];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;

            // Simple outline for readability
            Color outline = new Color(color.r * 0.45f, color.g * 0.45f, color.b * 0.45f, 1f);
            for (int x = 0; x < size.x; x++)
            {
                pixels[x] = outline;
                pixels[(size.y - 1) * size.x + x] = outline;
            }
            for (int y = 0; y < size.y; y++)
            {
                pixels[y * size.x] = outline;
                pixels[y * size.x + (size.x - 1)] = outline;
            }

            tex.SetPixels(pixels);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size.x, size.y), new Vector2(0.5f, 0.5f), 16f);
        }
    }
}
