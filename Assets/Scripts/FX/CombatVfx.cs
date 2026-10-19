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
            SwordSlash,
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

        /// <summary>
        /// R only. Pivot sits on the blade's inner edge so the arc grows out from the weapon.
        /// </summary>
        public static CombatVfx SpawnSwordSlash(Vector3 pos, Vector2 facing, float scale = 1f)
        {
            float angle = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            float life = scale > 1.05f ? 0.2f : 0.14f;
            var slash = Spawn(Style.SwordSlash, pos, angle, scale, life,
                Color.white,
                new Color(0.75f, 0.95f, 1f, 0f),
                0.92f, scale > 1.05f ? 1.18f : 1.06f);
            Spawn(Style.SwordSlash, pos - (Vector3)(facing.normalized * 0.08f), angle, scale * 0.72f, life * 0.65f,
                new Color(0.55f, 0.9f, 1f, 0.7f),
                new Color(0.55f, 0.9f, 1f, 0f),
                0.85f, 1.02f);
            return slash;
        }

        // R1 authored slash sheets (Assets/Resources/VFX), 4 frames each: A = chain hit 1, B = hit 2, C = finisher.
        static readonly string[] SlashSheets = { "VFX/VFX_SwordSlash_A", "VFX/VFX_SwordSlash_B", "VFX/VFX_SwordSlash_C" };
        // The sprite pivot is the arc centre; pull it back so the arc sweeps across the hit zone in front of the hero.
        static readonly float[] SlashPullBack = { 1.0f, 0.9f, 1.8f };
        static readonly float[] SlashLife = { 0.14f, 0.14f, 0.2f };
        static Sprite[][] _slashFrames;

        /// <summary>
        /// Plays an authored slash at scale 1 (pixel-exact), facing snapped to 8 directions.
        /// Returns false when the sheet is missing so callers can fall back to the procedural arc.
        /// </summary>
        public static bool SpawnSwordSlashFrames(Vector3 chest, Vector2 facing, int variant)
        {
            variant = Mathf.Clamp(variant, 0, 2);
            var frames = SlashFrames(variant);
            if (frames == null) return false;

            float raw = Mathf.Atan2(facing.y, facing.x) * Mathf.Rad2Deg;
            float angle = Mathf.Round(raw / 45f) * 45f;
            Vector2 dir = new(Mathf.Cos(angle * Mathf.Deg2Rad), Mathf.Sin(angle * Mathf.Deg2Rad));
            var vfx = Spawn(Style.SwordSlash, chest - (Vector3)(dir * SlashPullBack[variant]), angle, 1f,
                SlashLife[variant], Color.white, Color.white, 1f, 1f);
            // Sheets are drawn sweeping for a rightward swing; flip when facing left so the sweep keeps its direction.
            vfx.SetFrames(frames, dir.x < -0.01f);
            return true;
        }

        static Sprite[] SlashFrames(int variant)
        {
            _slashFrames ??= new Sprite[SlashSheets.Length][];
            var cached = _slashFrames[variant];
            if (cached != null && cached.Length > 0 && cached[0] != null) return cached;
            var loaded = Resources.LoadAll<Sprite>(SlashSheets[variant]);
            if (loaded == null || loaded.Length == 0) return null;
            System.Array.Sort(loaded, (a, b) => string.CompareOrdinal(a.name, b.name));
            _slashFrames[variant] = loaded;
            return loaded;
        }

        Sprite[] _frames;

        void SetFrames(Sprite[] frames, bool flipY)
        {
            _frames = frames;
            _sr.sprite = frames[0];
            _sr.flipY = flipY;
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

        // Pool: every hit spawns 3-5 of these, so they are recycled instead of created/destroyed.
        static readonly System.Collections.Generic.Stack<CombatVfx> Pool = new();
        static Transform _poolRoot;
        static int _created;

        public static int PooledCount => Pool.Count;
        public static int CreatedCount => _created;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetPool()
        {
            // Domain Reload is off: drop references to objects destroyed with the last play session.
            Pool.Clear();
            _poolRoot = null;
            _created = 0;
            _slashFrames = null;
        }

        static CombatVfx Spawn(Style style, Vector3 pos, float angle, float scale, float life,
            Color a, Color b, float startScale, float endScale)
        {
            CombatVfx vfx = null;
            while (Pool.Count > 0 && vfx == null)
                vfx = Pool.Pop(); // skips entries destroyed by a scene unload
            if (vfx == null)
            {
                if (_poolRoot == null)
                    _poolRoot = new GameObject("VfxPool").transform;
                var go = new GameObject("VFX");
                go.transform.SetParent(_poolRoot, false);
                vfx = go.AddComponent<CombatVfx>();
                _created++;
            }
            var t = vfx.transform;
            t.position = pos;
            t.rotation = Quaternion.Euler(0f, 0f, angle);
            vfx.Init(style, life, a, b, startScale * scale, endScale * scale);
            vfx.gameObject.SetActive(true);
            return vfx;
        }

        void Release()
        {
            gameObject.SetActive(false);
            Pool.Push(this);
        }

        void Init(Style style, float life, Color a, Color b, float startScale, float endScale)
        {
            _style = style;
            _life = life;
            _age = 0f;
            _colorA = a;
            _colorB = b;
            _startScale = startScale;
            _endScale = endScale;

            if (_sr == null)
                _sr = gameObject.AddComponent<SpriteRenderer>();
            _frames = null; // pooled instance may have played a frame slash last time
            _sr.flipY = false;
            _sr.sprite = GetSprite(style);
            _sr.sharedMaterial = ProceduralSprites.Unlit;
            _sr.color = a;
            _sr.sortingOrder = 2500;
            transform.localScale = Vector3.one * _startScale;
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_age / _life);
            transform.localScale = Vector3.one * Mathf.Lerp(_startScale, _endScale, t);
            if (_frames != null)
            {
                // Authored frames carry their own fade; no tint or scale tween (keeps pixels exact).
                int idx = Mathf.Min(_frames.Length - 1, (int)(t * _frames.Length));
                _sr.sprite = _frames[idx];
            }
            else if (_sr != null)
            {
                float fade = _style == Style.Crescent || _style == Style.SwordSlash
                    ? Mathf.InverseLerp(0.55f, 1f, t)
                    : t;
                _sr.color = Color.Lerp(_colorA, _colorB, fade);
            }

            if (_age >= _life)
                Release();
        }

        static Sprite GetSprite(Style style)
        {
            return style switch
            {
                Style.Crescent => ProceduralSprites.Crescent,
                Style.SwordSlash => ProceduralSprites.SwordSlash,
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
        static Sprite _swordSlash;
        static Sprite _ring;
        static Sprite _burst;
        static Sprite _spark;
        static Sprite _flash;
        static Sprite _dust;
        static Sprite _pixel;

        static Material _unlit;

        public static Sprite Crescent => Cached(ref _crescent, BuildCrescent);
        public static Sprite SwordSlash => Cached(ref _swordSlash, BuildSwordSlash);
        public static Sprite Ring => Cached(ref _ring, BuildRing);
        public static Sprite StarBurst => Cached(ref _burst, BuildBurst);
        public static Sprite Spark => Cached(ref _spark, BuildSpark);
        public static Sprite Flash => Cached(ref _flash, BuildFlash);
        public static Sprite Dust => Cached(ref _dust, BuildDust);
        public static Sprite Pixel => Cached(ref _pixel, () => BuildSolid(8, 8, Color.white));

        public static Material Unlit
        {
            get
            {
                if (_unlit == null)
                {
                    var shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
                    if (shader == null)
                        shader = Shader.Find("Sprites/Default");
                    _unlit = new Material(shader) { hideFlags = HideFlags.HideAndDontSave };
                }
                return _unlit;
            }
        }

        static Sprite Cached(ref Sprite slot, System.Func<Sprite> build)
        {
            if (slot == null)
                slot = build();
            return slot;
        }

        static Sprite BuildSolid(int w, int h, Color c)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color[w * h];
            for (int i = 0; i < px.Length; i++) px[i] = c;
            tex.SetPixels(px);
            tex.Apply(false, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var sprite = Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 16f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static Sprite BuildCrescent()
        {
            const int s = 48;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[s * s];
            var white = new Color32(255, 255, 255, 255);
            var cyan = new Color32(120, 230, 255, 255);
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
            tex.Apply(false, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var sprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(8f / s, 0.5f), 16f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        static Sprite BuildSwordSlash()
        {
            const int s = 32;
            var tex = new Texture2D(s, s, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[s * s];
            var white = new Color32(255, 255, 255, 255);
            var cyan = new Color32(150, 236, 255, 255);
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                int dx = x - 3;
                int dy = y - 16;
                if (dx < 1) continue;
                int r2 = dx * dx + dy * dy;
                if (r2 < 8 * 8 || r2 > 14 * 14) continue;
                px[y * s + x] = r2 > 12 * 12 ? white : cyan;
            }
            tex.SetPixels32(px);
            tex.Apply(false, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var sprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(3f / s, 0.5f), 16f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
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
            tex.Apply(false, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var sprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
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
            tex.Apply(false, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var sprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
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
            tex.Apply(false, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var sprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
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
            tex.Apply(false, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var sprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 16f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
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
            tex.Apply(false, false);
            tex.hideFlags = HideFlags.HideAndDontSave;
            var sprite = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.35f), 16f);
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
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
