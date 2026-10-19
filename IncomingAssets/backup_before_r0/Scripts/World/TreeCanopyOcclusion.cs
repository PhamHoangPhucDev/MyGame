using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Splits tree sprite into trunk + canopy. Fades only the canopy while the
    /// hero (or enemies) stand under the foliage. Trunk, collider, and Y-sort stay intact.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class TreeCanopyOcclusion : MonoBehaviour
    {
        [SerializeField, Range(0.25f, 0.6f)] float coveredAlpha = 0.4f;
        [SerializeField] float fadeSeconds = 0.1f;
        [SerializeField] float coverPadding = 0.15f;

        SpriteRenderer _trunk;
        SpriteRenderer _canopy;
        Transform _hero;
        float _alpha = 1f;
        Color _canopyColor = Color.white;
        static readonly Collider2D[] OverlapBuf = new Collider2D[12];

        void Awake()
        {
            _trunk = GetComponent<SpriteRenderer>();
            if (_trunk == null || _trunk.sprite == null)
                return;

            SplitCanopy();
        }

        void SplitCanopy()
        {
            var src = _trunk.sprite;
            var texRect = src.textureRect;
            int trunkPx = TrunkPixels(src.rect.height);
            if (trunkPx <= 0 || trunkPx >= texRect.height - 4)
                return;

            float ppu = src.pixelsPerUnit;
            var trunkSprite = Sprite.Create(
                src.texture,
                new Rect(texRect.x, texRect.y, texRect.width, trunkPx),
                new Vector2(0.5f, 0f),
                ppu,
                0,
                SpriteMeshType.FullRect);
            var canopySprite = Sprite.Create(
                src.texture,
                new Rect(texRect.x, texRect.y + trunkPx, texRect.width, texRect.height - trunkPx),
                new Vector2(0.5f, 0f),
                ppu,
                0,
                SpriteMeshType.FullRect);
            trunkSprite.hideFlags = HideFlags.HideAndDontSave;
            canopySprite.hideFlags = HideFlags.HideAndDontSave;

            _trunk.sprite = trunkSprite;

            // Shrink collider to trunk footprint so canopy does not block paths.
            var box = GetComponent<BoxCollider2D>();
            if (box != null)
            {
                float trunkH = trunkPx / ppu;
                float trunkW = texRect.width / ppu;
                box.size = new Vector2(Mathf.Max(0.35f, trunkW * 0.35f), Mathf.Max(0.25f, trunkH * 0.55f));
                box.offset = new Vector2(0f, trunkH * 0.28f);
            }

            var existing = transform.Find("Canopy");
            if (existing != null)
                Destroy(existing.gameObject);

            var go = new GameObject("Canopy");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(0f, trunkPx / ppu, 0f);
            go.transform.localRotation = Quaternion.identity;
            go.transform.localScale = Vector3.one;
            _canopy = go.AddComponent<SpriteRenderer>();
            _canopy.sprite = canopySprite;
            // Instance material so alpha fade never mutates shared tree materials.
            if (_trunk.sharedMaterial != null)
                _canopy.material = new Material(_trunk.sharedMaterial);
            else
                _canopy.sharedMaterial = _trunk.sharedMaterial;
            _canopy.sortingLayerID = _trunk.sortingLayerID;
            _canopy.sortingOrder = _trunk.sortingOrder + 1;
            _canopyColor = _trunk.color;
            _canopyColor.a = 1f;
            _canopy.color = _canopyColor;
        }

        static int TrunkPixels(float spriteHeight)
        {
            // Tree_Forest_A/B are ~62×78 — keep a solid trunk band.
            if (spriteHeight >= 75f) return 24;
            if (spriteHeight >= 65f) return 20;
            if (spriteHeight >= 48f) return 16;
            return Mathf.Max(8, Mathf.RoundToInt(spriteHeight * 0.30f));
        }

        void LateUpdate()
        {
            if (_canopy == null || _trunk == null)
                return;

            _canopy.sortingLayerID = _trunk.sortingLayerID;
            _canopy.sortingOrder = _trunk.sortingOrder + 1;

            if (_hero == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    _hero = player.transform;
            }

            bool covered = IsAnyoneUnderCanopy();
            float target = covered ? coveredAlpha : 1f;
            float step = fadeSeconds > 0.001f ? Time.unscaledDeltaTime / fadeSeconds : 1f;
            _alpha = Mathf.MoveTowards(_alpha, target, step);
            _canopyColor.a = _alpha;
            _canopy.color = _canopyColor;
        }

        bool IsAnyoneUnderCanopy()
        {
            if (_hero != null && CoversPoint(_hero.position))
                return true;

            // Also fade when enemies fight under the same canopy so combat stays readable.
            var bounds = _canopy.bounds;
            bounds.Expand(coverPadding * 2f);
            int count = Physics2D.OverlapBoxNonAlloc(
                bounds.center,
                new Vector2(bounds.size.x, bounds.size.y),
                0f,
                OverlapBuf);
            for (int i = 0; i < count; i++)
            {
                var col = OverlapBuf[i];
                if (col == null) continue;
                if (((1 << col.gameObject.layer) & 0) != 0) continue;
                if (col.CompareTag("Player"))
                    return true;
                if (col.GetComponentInParent<EnemyController>() != null)
                {
                    if (CoversPoint(col.transform.position))
                        return true;
                }
            }
            return false;
        }

        bool CoversPoint(Vector3 feet)
        {
            var bounds = _canopy.bounds;
            bounds.Expand(coverPadding);
            var chest = feet + Vector3.up * 0.55f;
            var head = feet + Vector3.up * 1.05f;
            // Feet near trunk base still count if chest/head sit in foliage.
            return bounds.Contains(chest) || bounds.Contains(head) ||
                   (feet.y >= transform.position.y - 0.15f &&
                    feet.y <= bounds.max.y &&
                    feet.x >= bounds.min.x && feet.x <= bounds.max.x);
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            coveredAlpha = Mathf.Clamp(coveredAlpha, 0.25f, 0.6f);
        }
#endif
    }
}
