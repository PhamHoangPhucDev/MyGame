using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Fades only the foliage while the hero stands under it. The trunk renderer,
    /// collider, and sorting layer stay on this object.
    /// </summary>
    [DefaultExecutionOrder(50)]
    public class TreeCanopyOcclusion : MonoBehaviour
    {
        const float CoveredAlpha = 0.42f;
        const float FadeSeconds = 0.08f;

        SpriteRenderer _trunk;
        SpriteRenderer _canopy;
        Transform _hero;
        float _alpha = 1f;
        Color _canopyColor = Color.white;

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
            if (trunkPx <= 0 || trunkPx >= texRect.height)
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
            _canopy.sharedMaterial = _trunk.sharedMaterial;
            _canopy.sortingLayerID = _trunk.sortingLayerID;
            _canopy.sortingOrder = _trunk.sortingOrder + 1;
            _canopyColor = _trunk.color;
            _canopyColor.a = 1f;
            _canopy.color = _canopyColor;
        }

        static int TrunkPixels(float spriteHeight)
        {
            if (spriteHeight >= 75f) return 23;
            if (spriteHeight >= 65f) return 19;
            return Mathf.RoundToInt(spriteHeight * 0.28f);
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

            bool covered = _hero != null && Covers(_hero.position);
            float target = covered ? CoveredAlpha : 1f;
            _alpha = Mathf.MoveTowards(_alpha, target, Time.deltaTime / FadeSeconds);
            _canopyColor.a = _alpha;
            _canopy.color = _canopyColor;
        }

        bool Covers(Vector3 feet)
        {
            var bounds = _canopy.bounds;
            var chest = feet + Vector3.up * 0.7f;
            return bounds.Contains(feet) || bounds.Contains(chest);
        }
    }
}
