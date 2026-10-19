using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Lightweight procedural combat VFX (crescent slash, shockwave, burst).
    /// </summary>
    public class CombatVfx : MonoBehaviour
    {
        public enum Style
        {
            Crescent,
            Shockwave,
            Burst,
            Impact,
            Flash,
            Dust
        }

        Style _style;
        float _life;
        float _age;
        float _startScale;
        float _endScale;
        SpriteRenderer _sr;
        Color _colorA;
        Color _colorB;

        public static CombatVfx SpawnCrescent(Vector3 pos, Vector2 facing, float scale = 1.35f, float life = -1f)
        {
            float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            float duration = life > 0f ? life : 0.14f;
            return Spawn(Style.Crescent, pos, angle, scale, duration,
                new Color(0.55f, 0.92f, 1f, 1f),
                new Color(1f, 1f, 1f, 0f),
                0.96f, 1.08f);
        }

        public static CombatVfx SpawnShockwave(Vector3 pos, float scale = 2.2f)
        {
            return Spawn(Style.Shockwave, pos, 0f, scale, 0.35f,
                new Color(0.4f, 0.85f, 1f, 0.85f),
                new Color(1f, 1f, 1f, 0f),
                0.4f, 2.4f);
        }

        public static CombatVfx SpawnBurst(Vector3 pos, Color color, float scale = 1.1f)
        {
            return Spawn(Style.Burst, pos, Random.Range(0f, 360f), scale, 0.22f,
                color, new Color(color.r, color.g, color.b, 0f), 0.5f, 1.4f);
        }

        public static CombatVfx SpawnImpact(Vector3 pos)
        {
            Spawn(Style.Flash, pos, 0f, 0.7f, 0.08f,
                new Color(1f, 1f, 1f, 1f),
                new Color(1f, 1f, 1f, 0f),
                0.75f, 1.05f);
            Spawn(Style.Dust, pos + Vector3.down * 0.08f, Random.Range(-12f, 12f), 0.55f, 0.16f,
                new Color(0.72f, 0.62f, 0.42f, 0.9f),
                new Color(0.72f, 0.62f, 0.42f, 0f),
                0.7f, 1.15f);
            return Spawn(Style.Impact, pos, Random.Range(0f, 360f), 0.55f, 0.12f,
                new Color(0.85f, 0.96f, 1f, 1f),
                new Color(1f, 1f, 1f, 0f),
                0.8f, 1.05f);
        }

        static CombatVfx Spawn(Style style, Vector3 pos, float angle, float scale, float life,
            Color a, Color b, float startScale, float endScale)
        {
            var go = new GameObject($"VFX_{style}");
            go.transform.position = pos;
            go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            var vfx = go.AddComponent<CombatVfx>();
            vfx.Init(style, life, a, b, startScale * scale, endScale * scale);
            return vfx;
        }

        void Init(Style style, float life, Color a, Color b, float startScale, float endScale)
        {
            _style = style;
            _life = life;
            _colorA = a;
            _colorB = b;
            _startScale = startScale;
            _endScale = endScale;

            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = GetSprite(style);
            _sr.color = a;
            _sr.sortingOrder = 2500;
            transform.localScale = Vector3.one * _startScale;
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_age / _life);
            transform.localScale = Vector3.one * Mathf.Lerp(_startScale, _endScale, t);
            if (_sr != null)
            {
                float fade = _style == Style.Crescent ? Mathf.InverseLerp(0.55f, 1f, t) : t;
                _sr.color = Color.Lerp(_colorA, _colorB, fade);
            }

            if (_age >= _life)
                Destroy(gameObject);
        }

        static Sprite GetSprite(Style style)
        {
            return style switch
            {
                Style.Crescent => ProceduralSprites.Crescent,
                Style.Shockwave => ProceduralSprites.Ring,
                Style.Burst => ProceduralSprites.StarBurst,
                Style.Flash => ProceduralSprites.Flash,
                Style.Dust => ProceduralSprites.Dust,
                _ => ProceduralSprites.Spark
            };
        }
    }

    public static class ProceduralSprites
    {
        static Sprite _crescent;
        static Sprite _ring;
        static Sprite _burst;
        static Sprite _spark;
        static Sprite _flash;
        static Sprite _dust;
        static Sprite _pixel;

        public static Sprite Crescent => _crescent ??= BuildCrescent();
        public static Sprite Ring => _ring ??= BuildRing();
        public static Sprite StarBurst => _burst ??= BuildBurst();
        public static Sprite Spark => _spark ??= BuildSpark();
        public static Sprite Flash => _flash ??= BuildFlash();
        public static Sprite Dust => _dust ??= BuildDust();
        public static Sprite Pixel => _pixel ??= BuildSolid(8, 8, Color.white);

        static Sprite BuildSolid(int w, int h, Color c)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = c;
            tex.SetPixels(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
        }

        static Sprite BuildCrescent()
        {
            const int s = 48;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[s * s];
            var white = new Color32(255, 255, 255, 255);
            var cyan = new Color32(120, 220, 255, 255);
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                int dx = x - 8;
                int dy = y - 24;
                if (dx < 6) continue;
                int r2 = dx * dx + dy * dy;
                if (r2 < 14 * 14 || r2 > 22 * 22) continue;
                px[y * s + x] = r2 > 19 * 19 ? white : cyan;
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(8f / s, 0.5f), 16f);
        }

        static Sprite BuildRing()
        {
            const int s = 48;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color[s * s];
            Vector2 c = new(s * 0.5f, s * 0.5f);
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), c);
                if (d > 16f && d < 21f)
                    px[y * s + x] = Color.white;
            }
            tex.SetPixels(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
        }

        static Sprite BuildBurst()
        {
            const int s = 32;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color[s * s];
            Vector2 c = new(s * 0.5f, s * 0.5f);
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                Vector2 p = new Vector2(x + 0.5f, y + 0.5f) - c;
                float ang = Mathf.Atan2(p.y, p.x);
                float spoke = Mathf.Abs(Mathf.Sin(ang * 4f));
                float d = p.magnitude;
                if (d < 12f * (0.45f + spoke * 0.55f))
                    px[y * s + x] = Color.white;
            }
            tex.SetPixels(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
        }

        static Sprite BuildSpark()
        {
            const int s = 12;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[s * s];
            var c = new Color32(230, 250, 255, 255);
            for (int i = 3; i <= 8; i++)
            {
                px[6 * s + i] = c;
                px[i * s + 6] = c;
            }
            px[6 * s + 6] = new Color32(255, 255, 255, 255);
            px[5 * s + 5] = c;
            px[7 * s + 7] = c;
            px[5 * s + 7] = c;
            px[7 * s + 5] = c;
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
        }

        static Sprite BuildFlash()
        {
            const int s = 10;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[s * s];
            var c = new Color32(255, 255, 255, 255);
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                int d = Mathf.Abs(x - 5) + Mathf.Abs(y - 5);
                if (d <= 3)
                    px[y * s + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
        }

        static Sprite BuildDust()
        {
            const int s = 12;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[s * s];
            var a = new Color32(168, 140, 96, 220);
            var b = new Color32(120, 98, 70, 180);
            Stamp(px, s, 3, 4, a);
            Stamp(px, s, 7, 5, b);
            Stamp(px, s, 5, 7, a);
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.35f), 16f);
        }

        static void Stamp(Color32[] px, int w, int x, int y, Color32 c)
        {
            for (int oy = 0; oy < 2; oy++)
            for (int ox = 0; ox < 2; ox++)
            {
                int i = (y + oy) * w + (x + ox);
                if ((uint)i < (uint)px.Length) px[i] = c;
            }
        }
    }
}
