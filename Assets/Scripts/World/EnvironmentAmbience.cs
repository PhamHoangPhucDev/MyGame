using System.Collections.Generic;
using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Runtime ambience on top of the static camp: drifting purple motes like the reference
    /// footage, and a flickering glow with rising embers on fire props (Campfire, Cauldron).
    /// Everything is pooled and pixel-snapped; nothing is spawned or destroyed per frame.
    /// </summary>
    public class EnvironmentAmbience : MonoBehaviour
    {
        [Header("Motes")]
        [SerializeField] int moteCount = 26;
        [SerializeField] Color[] moteColors =
        {
            new(0.62f, 0.42f, 0.95f, 1f),
            new(0.8f, 0.55f, 1f, 1f),
            new(1f, 0.82f, 0.95f, 1f)
        };
        [SerializeField] float moteMaxAlpha = 0.75f;

        [Header("Fire props")]
        [SerializeField] string[] fireNames = { "Campfire", "Cauldron" };
        [SerializeField] int embersPerFire = 6;

        const float Pixel = 1f / 16f;
        const int MoteSort = 3000; // above characters and combat VFX (2500), kept faint

        class Mote
        {
            public Transform t;
            public SpriteRenderer sr;
            public Vector2 pos, vel;
            public float age, life, phase;
        }

        class Fire
        {
            public Transform root;
            public SpriteRenderer glow;
            public Vector2 origin;
            public float seed;
            public readonly List<Mote> embers = new();
        }

        readonly List<Mote> _motes = new();
        readonly List<Fire> _fires = new();
        Camera _cam;
        static Sprite _dot, _glow;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _dot = null; _glow = null; }

        void Start()
        {
            _cam = Camera.main;
            // Scene root on purpose: SkillHotbarUI clears every child of Systems when it builds the HUD.
            var root = new GameObject("Ambience").transform;

            for (int i = 0; i < moteCount; i++)
            {
                var m = NewParticle(root, "Mote", MoteSort);
                Respawn(m, true);
                _motes.Add(m);
            }

            foreach (var sr in FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None))
            {
                if (!IsFire(sr.gameObject.name)) continue;
                var fire = new Fire { root = sr.transform, seed = Random.value * 10f };
                fire.origin = (Vector2)sr.bounds.center + Vector2.up * (sr.bounds.extents.y * 0.35f);

                var glowGo = new GameObject("FireGlow");
                glowGo.transform.SetParent(sr.transform, false);
                glowGo.transform.position = new Vector3(fire.origin.x, fire.origin.y, 0f);
                fire.glow = glowGo.AddComponent<SpriteRenderer>();
                fire.glow.sprite = GlowSprite;
                fire.glow.sharedMaterial = ProceduralSprites.Unlit;
                fire.glow.sortingOrder = sr.sortingOrder - 1; // halo behind the prop
                fire.glow.color = new Color(1f, 0.55f, 0.18f, 0.35f);

                for (int e = 0; e < embersPerFire; e++)
                {
                    var ember = NewParticle(root, "Ember", sr.sortingOrder + 1);
                    RespawnEmber(fire, ember, true);
                    fire.embers.Add(ember);
                }
                _fires.Add(fire);
            }
        }

        bool IsFire(string objectName)
        {
            foreach (var n in fireNames)
                if (objectName.StartsWith(n)) return true;
            return false;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            foreach (var m in _motes)
            {
                m.age += dt;
                if (m.age >= m.life || !OnScreen(m.pos, 1.5f))
                {
                    Respawn(m, false);
                    continue;
                }
                m.pos += (m.vel + new Vector2(Mathf.Sin(m.age * 1.3f + m.phase) * 0.12f, 0f)) * dt;
                float fade = Mathf.Min(1f, m.age / 0.8f, (m.life - m.age) / 0.8f);
                float twinkle = 0.55f + 0.45f * Mathf.Sin(m.age * 5f + m.phase);
                var c = m.sr.color;
                c.a = moteMaxAlpha * fade * twinkle;
                m.sr.color = c;
                m.t.position = Snap(m.pos);
            }

            float time = Time.time;
            foreach (var f in _fires)
            {
                if (f.root == null) continue;
                // Two summed sines read as an irregular flicker without per-frame random noise.
                float flicker = 0.5f + 0.3f * Mathf.Sin(time * 11f + f.seed) + 0.2f * Mathf.Sin(time * 23f + f.seed * 2f);
                f.glow.color = new Color(1f, 0.55f, 0.18f, 0.22f + 0.18f * flicker);
                float s = 1f + 0.08f * flicker;
                f.glow.transform.localScale = new Vector3(s, s, 1f);

                foreach (var e in f.embers)
                {
                    e.age += dt;
                    if (e.age >= e.life)
                    {
                        RespawnEmber(f, e, false);
                        continue;
                    }
                    e.pos += new Vector2(Mathf.Sin(e.age * 6f + e.phase) * 0.15f, e.vel.y) * dt;
                    var c = e.sr.color;
                    c.a = 1f - e.age / e.life;
                    e.sr.color = c;
                    e.t.position = Snap(e.pos);
                }
            }
        }

        Mote NewParticle(Transform parent, string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = DotSprite;
            sr.sharedMaterial = ProceduralSprites.Unlit;
            sr.sortingOrder = order;
            return new Mote { t = go.transform, sr = sr };
        }

        void Respawn(Mote m, bool anywhere)
        {
            Rect view = ViewRect(1f);
            m.pos = anywhere
                ? new Vector2(Random.Range(view.xMin, view.xMax), Random.Range(view.yMin, view.yMax))
                : new Vector2(Random.Range(view.xMin, view.xMax), view.yMin - 0.5f); // enter from below, drift up
            m.vel = new Vector2(Random.Range(-0.08f, 0.08f), Random.Range(0.12f, 0.3f));
            m.life = Random.Range(4f, 8f);
            m.age = anywhere ? Random.Range(0f, m.life * 0.6f) : 0f;
            m.phase = Random.value * 6.28f;
            m.sr.color = moteColors[Random.Range(0, moteColors.Length)];
            m.t.localScale = Vector3.one * (Random.value < 0.25f ? 2f : 1f); // a few 2x2 specks
        }

        void RespawnEmber(Fire f, Mote e, bool prewarm)
        {
            e.pos = f.origin + new Vector2(Random.Range(-0.15f, 0.15f), 0f);
            e.vel = new Vector2(0f, Random.Range(0.35f, 0.7f));
            e.life = Random.Range(0.6f, 1.2f);
            e.age = prewarm ? Random.Range(0f, e.life) : 0f;
            e.phase = Random.value * 6.28f;
            e.sr.color = Random.value < 0.5f ? new Color(1f, 0.75f, 0.25f, 1f) : new Color(1f, 0.45f, 0.15f, 1f);
        }

        Rect ViewRect(float margin)
        {
            if (_cam == null) _cam = Camera.main;
            if (_cam == null) return new Rect(-5f, -9f, 10f, 18f);
            float h = _cam.orthographicSize * 2f;
            float w = h * _cam.aspect;
            Vector2 c = _cam.transform.position;
            return new Rect(c.x - w * 0.5f - margin, c.y - h * 0.5f - margin, w + margin * 2f, h + margin * 2f);
        }

        bool OnScreen(Vector2 p, float margin) => ViewRect(margin).Contains(p);

        static Vector3 Snap(Vector2 p) =>
            new(Mathf.Round(p.x / Pixel) * Pixel, Mathf.Round(p.y / Pixel) * Pixel, 0f);

        static Sprite DotSprite
        {
            get
            {
                if (_dot != null) return _dot;
                var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };
                tex.SetPixel(0, 0, Color.white);
                tex.Apply(false, false);
                _dot = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 16f);
                _dot.hideFlags = HideFlags.HideAndDontSave;
                return _dot;
            }
        }

        // 24px disc with 3 stepped alpha rings, so the halo stays pixel-art instead of a smooth blur.
        static Sprite GlowSprite
        {
            get
            {
                if (_glow != null) return _glow;
                const int s = 24;
                var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };
                var px = new Color32[s * s];
                var c = new Vector2(s * 0.5f, s * 0.5f);
                for (int y = 0; y < s; y++)
                for (int x = 0; x < s; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c) / (s * 0.5f);
                    byte a = d < 0.45f ? (byte)255 : d < 0.75f ? (byte)150 : d < 1f ? (byte)70 : (byte)0;
                    px[y * s + x] = new Color32(255, 255, 255, a);
                }
                tex.SetPixels32(px);
                tex.Apply(false, false);
                _glow = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
                _glow.hideFlags = HideFlags.HideAndDontSave;
                return _glow;
            }
        }
    }
}
