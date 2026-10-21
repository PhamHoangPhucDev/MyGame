using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>R2C: plays a pixel sprite animation once (class skill VFX), then destroys itself.</summary>
    public class KitVfx : MonoBehaviour
    {
        Sprite[] _frames;
        float _fps;
        float _t;
        SpriteRenderer _sr;
        float _life = -1f;
        Color _from, _to;
        bool _loop;
        Transform _follow;
        Vector3 _followOffset;
        int _orderOffset;

        public static KitVfx Play(Sprite[] frames, Vector3 pos, float fps = 12f, bool flipX = false, int orderOffset = 60)
        {
            if (frames == null || frames.Length == 0 || frames[0] == null) return null;
            var go = new GameObject("KitVfx");
            go.transform.position = pos;
            var v = go.AddComponent<KitVfx>();
            v._frames = frames;
            v._fps = Mathf.Max(1f, fps);
            v._orderOffset = orderOffset;
            v._sr = go.AddComponent<SpriteRenderer>();
            v._sr.sprite = frames[0];
            v._sr.flipX = flipX;
            v._sr.sortingOrder = 4000 + Mathf.RoundToInt(-pos.y * 100f) + orderOffset;
            return v;
        }

        /// <summary>R3D.1: follow a host (body) with relative sorting. loop=true for stun stars / bulwark sustain.</summary>
        public static KitVfx PlayAttached(Sprite[] frames, Transform host, Vector3 localOffset, float fps, bool flipX, int orderOffset, bool loop)
        {
            if (frames == null || frames.Length == 0 || frames[0] == null || host == null) return null;
            var go = new GameObject("KitVfxAttached");
            go.transform.SetParent(host, false);
            go.transform.localPosition = localOffset;
            var v = go.AddComponent<KitVfx>();
            v._frames = frames;
            v._fps = Mathf.Max(1f, fps);
            v._loop = loop;
            v._follow = host;
            v._followOffset = localOffset;
            v._orderOffset = orderOffset;
            v._sr = go.AddComponent<SpriteRenderer>();
            v._sr.sprite = frames[0];
            v._sr.flipX = flipX;
            var body = host.GetComponentInChildren<SpriteRenderer>();
            int baseOrder = body != null ? body.sortingOrder : 4000;
            v._sr.sortingOrder = baseOrder + orderOffset;
            if (body != null) v._sr.sortingLayerID = body.sortingLayerID;
            return v;
        }

        /// <summary>Full-screen tint that fades out (Execute darkening). Uses unscaled time.</summary>
        public static void ScreenTint(Color color, float life)
        {
            var cam = Camera.main;
            if (cam == null) return;
            var go = new GameObject("KitScreenTint");
            go.transform.SetParent(cam.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 1f);
            var v = go.AddComponent<KitVfx>();
            v._sr = go.AddComponent<SpriteRenderer>();
            var tex = Texture2D.whiteTexture;
            v._sr.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), tex.width);
            float h = cam.orthographicSize * 2.2f;
            go.transform.localScale = new Vector3(h * cam.aspect * 1.2f, h, 1f);
            v._sr.sortingOrder = 32000;
            v._from = color; v._to = new Color(color.r, color.g, color.b, 0f);
            v._sr.color = color;
            v._life = life;
        }

        void Update()
        {
            if (_life > 0f)
            {
                _t += Time.unscaledDeltaTime;
                _sr.color = Color.Lerp(_from, _to, _t / _life);
                if (_t >= _life) Destroy(gameObject);
                return;
            }
            if (_follow != null)
            {
                // Keep world offset stable if unparented; parented children already follow.
                var body = _follow.GetComponentInChildren<SpriteRenderer>();
                if (body != null && _sr != null)
                    _sr.sortingOrder = body.sortingOrder + _orderOffset;
            }

            _t += Time.deltaTime;
            int i = Mathf.FloorToInt(_t * _fps);
            if (_loop)
            {
                if (_frames.Length == 0) return;
                _sr.sprite = _frames[i % _frames.Length];
                return;
            }
            if (i >= _frames.Length) { Destroy(gameObject); return; }
            _sr.sprite = _frames[i];
        }
    }
}
