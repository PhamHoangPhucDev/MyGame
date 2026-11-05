using System.Collections.Generic;
using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Time-of-day keys, tree shadows, lights and pond life (Docs/WorldArt/HomeDistrict/daynight/DAYNIGHT_POND_HANDOFF.md).
    /// The clock stays in WorldServices; it feeds <see cref="T"/> and <see cref="Raining"/> every frame.
    /// Glows sort above the world tint quad (30000) so they stay lit; UI canvases are untouched.
    /// </summary>
    public static class DayNight
    {
        const string Dir = "World/HomeDistrict/DayNight/";
        public const int GlowOrder = 30010;
        static readonly Dictionary<string, Sprite[,]> _cache = new();

        public static float T { get; private set; } = 0.2f;
        public static bool Raining { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _cache.Clear();
            T = 0.2f;
            Raining = false;
        }

        public static void Set(float t, bool rain)
        {
            T = Mathf.Repeat(t, 1f);
            Raining = rain;
        }

        // ---------- keys ----------
        static readonly float[] KeyT = { 0.00f, 0.20f, 0.40f, 0.55f, 0.65f, 0.92f, 1.00f };
        static readonly Color[] KeyTint =
        {
            Hex(0xFFE8C8), Hex(0xFFFFFF), Hex(0xFFF4E0), Hex(0xF0A868), Hex(0x5A6AA8), Hex(0x5A6AA8), Hex(0xFFE8C8)
        };
        static readonly float[] KeyK = { 0.18f, 0.00f, 0.08f, 0.35f, 0.55f, 0.55f, 0.18f };

        public static void Sample(float t, out Color tint, out float strength)
        {
            t = Mathf.Repeat(t, 1f);
            for (int i = 0; i < KeyT.Length - 1; i++)
            {
                if (t > KeyT[i + 1]) continue;
                float u = Mathf.InverseLerp(KeyT[i], KeyT[i + 1], t);
                tint = Color.Lerp(KeyTint[i], KeyTint[i + 1], u);
                strength = Mathf.Lerp(KeyK[i], KeyK[i + 1], u);
                return;
            }
            tint = KeyTint[0];
            strength = KeyK[0];
        }

        /// <summary>
        /// Alpha-blend stand-in for colour*(1-k) + colour*tint*k on the overlay quad.
        /// Blending runs in linear space when the project is linear, so the alpha is taken from the linear multiplier.
        /// </summary>
        public static Color Overlay(float t)
        {
            Sample(t, out var tint, out float k);
            float g = QualitySettings.activeColorSpace == ColorSpace.Linear ? 2.2f : 1f;
            float r = Mathf.Pow(1f - k * (1f - tint.r), g);
            float gr = Mathf.Pow(1f - k * (1f - tint.g), g);
            float b = Mathf.Pow(1f - k * (1f - tint.b), g);
            float a = Mathf.Clamp01(1f - (r + gr + b) / 3f);
            return new Color(tint.r * 0.3f, tint.g * 0.3f, tint.b * 0.3f, a);
        }

        public static int ShadowRow(float t)
        {
            if (t < 0.20f) return 0;
            if (t < 0.40f) return 1;
            if (t < 0.55f) return 2;
            if (t < 0.65f) return 3;
            if (t < 0.96f) return 4;
            return 0;
        }

        public static bool LampsOn => T >= 0.58f && T < 0.96f;
        public static bool WindowsOn => T >= 0.65f && T < 0.96f;
        public static bool IsNight => T >= 0.65f && T < 0.92f;
        public static bool Butterflies => T < 0.55f && !Raining;
        public static bool Fish => T >= 0.10f && T < 0.55f;
        public static bool Sparkles => T >= 0.10f && T < 0.55f && !Raining;
        public static bool Fireflies => IsNight && !Raining;

        // ---------- art ----------
        public static Sprite[,] Grid(string file, int cols, int rows, int cw, int ch, Vector2 pivot)
        {
            if (_cache.TryGetValue(file, out var cached) && cached != null && cached[0, 0] != null) return cached;
            var tex = Resources.Load<Texture2D>(Dir + file);
            if (tex == null || tex.width < cols * cw || tex.height < rows * ch)
            {
                Debug.LogWarning("DayNight: missing or undersized sheet " + file);
                return null;
            }
            var grid = new Sprite[rows, cols];
            for (int r = 0; r < rows; r++)
                for (int c = 0; c < cols; c++)
                {
                    var rect = new Rect(c * cw, tex.height - (r + 1) * ch, cw, ch);
                    var s = Sprite.Create(tex, rect, pivot, 16f, 0, SpriteMeshType.FullRect);
                    s.name = file + "_" + r + "_" + c;
                    grid[r, c] = s;
                }
            _cache[file] = grid;
            return grid;
        }

        public static Sprite[] Row(string file, int cols, int rows, int cw, int ch, int row)
        {
            var g = Grid(file, cols, rows, cw, ch, new Vector2(0.5f, 0.5f));
            if (g == null || row >= g.GetLength(0)) return null;
            var r = new Sprite[cols];
            for (int c = 0; c < cols; c++) r[c] = g[row, c];
            return r;
        }

        public static Sprite Single(string file) => Cell(Grid(file, 1, 1, Size(file).x, Size(file).y, new Vector2(0.5f, 0.5f)), 0, 0);

        static Vector2Int Size(string file)
        {
            switch (file)
            {
                case "DN_Glow_Lamp": return new Vector2Int(28, 28);
                case "DN_Glow_Window": return new Vector2Int(32, 32);
                case "DN_Glow_Firefly": return new Vector2Int(10, 10);
                default: return new Vector2Int(16, 16);
            }
        }

        static Sprite Cell(Sprite[,] g, int r, int c) => g == null ? null : g[r, c];

        public static Sprite HouseWindows()
        {
            var g = Grid("DN_House_NightWindows", 1, 1, 128, 104, new Vector2(0.5f, 0f));
            return Cell(g, 0, 0);
        }

        /// <summary>Shadow sheet for a tree sprite, or null when the tree has none.</summary>
        public static Sprite[] TreeShadows(Sprite tree)
        {
            if (tree == null) return null;
            string n = tree.name;
            string file = n.Contains("Leaf") ? "DN_TreeShadow_Leaf"
                : n.Contains("Teal") ? "DN_TreeShadow_Teal"
                : n.Contains("Forest") || n.Contains("Tree") ? "DN_TreeShadow_ForestC"
                : null;
            if (file == null) return null;
            var g = Grid(file, 1, 5, 208, 64, new Vector2(0.5f, 0.3125f));
            if (g == null) return null;
            var rows = new Sprite[5];
            for (int r = 0; r < 5; r++) rows[r] = g[r, 0];
            return rows;
        }

        /// <summary>Adds the 5-row shadow at a tree foot. False when the tree has no sheet (keep the blob).</summary>
        public static bool AttachTreeShadow(Transform parent, Vector2 localFeet, Sprite tree, int order)
        {
            var rows = TreeShadows(tree);
            if (rows == null || parent == null) return false;
            var go = new GameObject("TreeShadowDN");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localFeet;
            go.AddComponent<TreeShadowCycle>().Init(rows, order);
            return true;
        }

        public static NightGlow Glow(Transform parent, Vector3 local, string file, bool flicker, bool windows)
        {
            var sprite = Single(file);
            if (sprite == null || parent == null) return null;
            var go = new GameObject(file);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = GlowOrder;
            return go.AddComponent<NightGlow>().Init(flicker, windows);
        }

        public static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
    }

    /// <summary>Two renderers crossfade over 0.5 s when the shadow row changes. No pixel sliding.</summary>
    public class TreeShadowCycle : MonoBehaviour
    {
        const float Alpha = 0.35f;
        Sprite[] _rows;
        SpriteRenderer _a, _b;
        int _row = -1;
        float _fade = 1f;

        public void Init(Sprite[] rows, int order)
        {
            _rows = rows;
            _a = Make("A", order);
            _b = Make("B", order);
            _row = DayNight.ShadowRow(DayNight.T);
            _a.sprite = _rows[_row];
            _a.color = new Color(1f, 1f, 1f, Alpha);
            _b.color = new Color(1f, 1f, 1f, 0f);
        }

        SpriteRenderer Make(string name, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            return sr;
        }

        void Update()
        {
            if (_rows == null) return;
            int row = DayNight.ShadowRow(DayNight.T);
            if (row != _row && _fade >= 1f)
            {
                _row = row;
                _b.sprite = _rows[row];
                _fade = 0f;
            }
            if (_fade >= 1f) return;
            _fade = Mathf.Min(1f, _fade + Time.deltaTime / 0.5f);
            _a.color = new Color(1f, 1f, 1f, Alpha * (1f - _fade));
            _b.color = new Color(1f, 1f, 1f, Alpha * _fade);
            if (_fade < 1f) return;
            (_a, _b) = (_b, _a);
            _b.color = new Color(1f, 1f, 1f, 0f);
        }
    }

    /// <summary>Lamp / window light: fades in over 1 s at evening, lamps flicker 1.0-1.05.</summary>
    public class NightGlow : MonoBehaviour
    {
        SpriteRenderer _sr;
        bool _flicker, _windows;
        float _alpha, _nextFlick, _scale = 1f;

        public NightGlow Init(bool flicker, bool windows)
        {
            _sr = GetComponent<SpriteRenderer>();
            _flicker = flicker;
            _windows = windows;
            _alpha = On ? 1f : 0f;
            Apply();
            return this;
        }

        bool On => _windows ? DayNight.WindowsOn : DayNight.LampsOn;

        void Update()
        {
            if (_sr == null) return;
            _alpha = Mathf.MoveTowards(_alpha, On ? 1f : 0f, Time.deltaTime);
            if (_flicker && Time.time >= _nextFlick)
            {
                _nextFlick = Time.time + Random.Range(0.3f, 0.6f);
                _scale = _scale > 1.02f ? 1f : 1.05f;
            }
            Apply();
        }

        void Apply()
        {
            _sr.enabled = _alpha > 0.01f;
            _sr.color = new Color(1f, 1f, 1f, _alpha);
            transform.localScale = _flicker ? new Vector3(_scale, _scale, 1f) : Vector3.one;
        }
    }

    /// <summary>Home-map pond and garden life. Positions are Home-local (HomeDistrict root).</summary>
    public class HomeLife : MonoBehaviour
    {
        static Vector2 PondC => HomeLayout.Pond;
        static readonly Vector2 PondR = new Vector2(2.2f, 1.1f);
        static readonly Vector2[] PadsAtDefault =
        {
            new Vector2(-2.95f, -4.65f), new Vector2(-6.83f, -3.78f), new Vector2(-6.45f, -4.40f),
            new Vector2(-2.58f, -3.78f), new Vector2(-3.58f, -5.03f)
        };
        static readonly Vector2[] FireflyAreas =
        {
            new Vector2(-6.6f, 1.0f), new Vector2(-7.0f, -1.5f), new Vector2(6.8f, 3.0f), new Vector2(-6.2f, -6.0f), new Vector2(-2.5f, -2.4f)
        };

        const int PondOrder = -8958;
        Sprite[] _butterW, _butterO, _firefly, _fish, _ripple, _sparkle, _frog;
        Sprite _fireGlow;
        readonly List<GameObject> _butterflies = new();
        readonly List<GameObject> _fireflies = new();
        readonly List<GameObject> _fishes = new();
        readonly List<GameObject> _sparkles = new();
        float _nextRipple, _nextRain, _nextDuck;
        FarmAnimal[] _ducks;

        void Start()
        {
            _butterW = DayNight.Row("DN_Butterfly", 4, 2, 8, 8, 0);
            _butterO = DayNight.Row("DN_Butterfly", 4, 2, 8, 8, 1);
            _firefly = DayNight.Row("DN_Firefly", 4, 1, 8, 8, 0);
            _fish = DayNight.Row("DN_Fish", 4, 1, 16, 8, 0);
            _ripple = DayNight.Row("DN_Ripple", 4, 1, 16, 8, 0);
            _sparkle = DayNight.Row("DN_Sparkle", 3, 1, 8, 8, 0);
            _frog = DayNight.Row("DN_Frog", 5, 1, 16, 12, 0);
            _fireGlow = DayNight.Single("DN_Glow_Firefly");

            for (int i = 0; i < 3; i++) _butterflies.Add(Butterfly(i));
            for (int i = 0; i < 8; i++) _fireflies.Add(Firefly(i));
            for (int i = 0; i < 2; i++) _fishes.Add(Fish(i));
            for (int i = 0; i < 3; i++) _sparkles.Add(Sparkle(i));
            Frog();
            _nextRipple = Time.time + Random.Range(3f, 6f);
        }

        void Update()
        {
            Toggle(_butterflies, DayNight.Butterflies);
            Toggle(_fireflies, DayNight.Fireflies);
            Toggle(_fishes, DayNight.Fish);
            Toggle(_sparkles, DayNight.Sparkles);

            if (Time.time >= _nextRipple)
            {
                _nextRipple = Time.time + Random.Range(3f, 6f);
                Ripple(InPond(0.85f));
            }
            if (DayNight.Raining && Time.time >= _nextRain)
            {
                _nextRain = Time.time + Random.Range(0.15f, 0.4f);
                Ripple(InPond(0.95f));
            }
            if (Time.time >= _nextDuck)
            {
                _nextDuck = Time.time + 0.9f;
                if (_ducks == null || _ducks.Length == 0) _ducks = GetComponentsInChildren<FarmAnimal>(true);
                foreach (var d in _ducks)
                    if (d != null && d.Kind == FarmAnimalKind.Duck && d.isActiveAndEnabled && HomeAnimals.InOpenWater(d.transform.localPosition))
                        Ripple(d.transform.localPosition);
            }
        }

        static void Toggle(List<GameObject> list, bool on)
        {
            for (int i = 0; i < list.Count; i++)
                if (list[i] != null && list[i].activeSelf != on) list[i].SetActive(on);
        }

        public static Vector2 InPond(float scale)
        {
            if (!HomeLayout.Has("pond")) return HomeLand.RandomWater(true);
            var p = Random.insideUnitCircle * scale;
            return PondC + new Vector2(p.x * PondR.x, p.y * PondR.y);
        }

        GameObject Make(string name, Vector2 local, int order, out SpriteRenderer sr)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.localPosition = local;
            sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = order;
            return go;
        }

        GameObject Butterfly(int i)
        {
            var home = i == 2 ? new Vector2(4.9f, -4.1f) : new Vector2(Random.Range(-3.5f, 3.5f), Random.Range(-1.5f, 0.5f));
            var go = Make("Butterfly", home, 0, out var sr);
            go.AddComponent<SpriteSorter>();
            go.AddComponent<LifeButterfly>().Init(sr, i % 2 == 0 ? _butterW : _butterO, home);
            return go;
        }

        GameObject Firefly(int i)
        {
            var area = FireflyAreas[i % FireflyAreas.Length] + Random.insideUnitCircle * 1.2f;
            var go = Make("Firefly", area, DayNight.GlowOrder + 2, out var sr);
            if (_fireGlow != null)
            {
                var halo = new GameObject("Halo");
                halo.transform.SetParent(go.transform, false);
                var hsr = halo.AddComponent<SpriteRenderer>();
                hsr.sprite = _fireGlow;
                hsr.sortingOrder = DayNight.GlowOrder + 1;
            }
            go.AddComponent<LifeFirefly>().Init(sr, _firefly, area);
            return go;
        }

        GameObject Fish(int i)
        {
            var go = Make("Fish", InPond(0.6f), PondOrder, out var sr);
            go.AddComponent<LifeFish>().Init(sr, _fish);
            return go;
        }

        GameObject Sparkle(int i)
        {
            var go = Make("Sparkle", InPond(0.75f), PondOrder + 1, out var sr);
            go.AddComponent<LifeSparkle>().Init(sr, _sparkle);
            return go;
        }

        void Frog()
        {
            var pads = new Vector2[PadsAtDefault.Length];
            bool dug = !HomeLayout.Has("pond");
            for (int i = 0; i < pads.Length; i++) pads[i] = dug ? HomeLand.RandomWater(true) : PadsAtDefault[i] + HomeLayout.PondShift;
            var go = Make("Frog", pads[0], PondOrder + 2, out var sr);
            go.AddComponent<LifeFrog>().Init(sr, _frog, pads, this);
        }

        public void Ripple(Vector2 local)
        {
            if (_ripple == null) return;
            var go = Make("Ripple", local, PondOrder + 1, out var sr);
            go.AddComponent<LifeOneShot>().Init(sr, _ripple, 8f);
        }
    }

    public class LifeButterfly : MonoBehaviour
    {
        SpriteRenderer _sr; Sprite[] _f; Vector2 _home, _to; float _t, _sitUntil, _speed;

        public void Init(SpriteRenderer sr, Sprite[] frames, Vector2 home)
        {
            _sr = sr; _f = frames; _home = home; _t = Random.value; _speed = Random.Range(0.6f, 0.9f);
            Pick();
        }

        void Pick() => _to = _home + Random.insideUnitCircle * Random.Range(1f, 1.5f);

        void Update()
        {
            if (_f == null) return;
            if (Time.time < _sitUntil) { _sr.sprite = _f[0]; return; }
            _t += Time.deltaTime * 10f;
            _sr.sprite = _f[(int)_t % _f.Length];
            Vector2 p = transform.localPosition;
            Vector2 d = _to - p;
            if (d.sqrMagnitude < 0.01f)
            {
                if (Random.value < 0.35f) _sitUntil = Time.time + Random.Range(1f, 2f);
                Pick();
                return;
            }
            var step = d.normalized * _speed * Time.deltaTime;
            step.y += Mathf.Sin(Time.time * 6f + _t) * 0.004f;
            transform.localPosition = p + step;
            _sr.flipX = d.x < 0f;
        }
    }

    public class LifeFirefly : MonoBehaviour
    {
        SpriteRenderer _sr; Sprite[] _f; Vector2 _home, _to; float _t;

        public void Init(SpriteRenderer sr, Sprite[] frames, Vector2 home)
        {
            _sr = sr; _f = frames; _home = home; _t = Random.Range(0f, 4f);
            _to = _home + Random.insideUnitCircle;
        }

        void Update()
        {
            if (_f == null) return;
            _t += Time.deltaTime * 6f;
            _sr.sprite = _f[(int)_t % _f.Length];
            Vector2 p = transform.localPosition;
            Vector2 d = _to - p;
            if (d.sqrMagnitude < 0.01f) { _to = _home + Random.insideUnitCircle * 1.2f; return; }
            transform.localPosition = p + d.normalized * 0.2f * Time.deltaTime;
        }
    }

    public class LifeFish : MonoBehaviour
    {
        SpriteRenderer _sr; Sprite[] _f; Vector2 _dir; float _until, _t;

        public void Init(SpriteRenderer sr, Sprite[] frames)
        {
            _sr = sr; _f = frames; _t = Random.value * 4f;
            Turn();
        }

        void Turn()
        {
            _dir = new Vector2(Random.value < 0.5f ? -1f : 1f, Random.Range(-0.3f, 0.3f)).normalized;
            _until = Time.time + Random.Range(2f, 4f);
        }

        void Update()
        {
            if (_f == null) return;
            _t += Time.deltaTime * 6f;
            _sr.sprite = _f[(int)_t % _f.Length];
            Vector2 next = (Vector2)transform.localPosition + _dir * 0.5f * Time.deltaTime;
            Vector2 rel = next - HomeLayout.Pond;
            bool inside = HomeLayout.Has("pond") && rel.x * rel.x / (2.0f * 2.0f) + rel.y * rel.y / (0.95f * 0.95f) < 1f
                || HomeLand.InDugWater(next);
            if (!inside || Time.time >= _until) { Turn(); return; }
            transform.localPosition = next;
            _sr.flipX = _dir.x < 0f;
        }
    }

    public class LifeSparkle : MonoBehaviour
    {
        SpriteRenderer _sr; Sprite[] _f; float _t, _wait;

        public void Init(SpriteRenderer sr, Sprite[] frames)
        {
            _sr = sr; _f = frames; _wait = Time.time + Random.Range(0f, 2f);
        }

        void Update()
        {
            if (_f == null) return;
            if (Time.time < _wait) { _sr.enabled = false; return; }
            _sr.enabled = true;
            _t += Time.deltaTime * 4f;
            int n = _f.Length, cycle = n * 2 - 2;
            int i = (int)_t % cycle;
            _sr.sprite = _f[i < n ? i : cycle - i];
            if (_t >= cycle)
            {
                _t = 0f;
                _wait = Time.time + Random.Range(0.8f, 2.5f);
                transform.localPosition = HomeLife.InPond(0.75f);
            }
        }
    }

    public class LifeOneShot : MonoBehaviour
    {
        SpriteRenderer _sr; Sprite[] _f; float _fps, _t;

        public void Init(SpriteRenderer sr, Sprite[] frames, float fps)
        {
            _sr = sr; _f = frames; _fps = fps;
            if (_f != null) _sr.sprite = _f[0];
        }

        void Update()
        {
            if (_f == null) { Destroy(gameObject); return; }
            _t += Time.deltaTime * _fps;
            int i = (int)_t;
            if (i >= _f.Length) { Destroy(gameObject); return; }
            _sr.sprite = _f[i];
        }
    }

    public class LifeFrog : MonoBehaviour
    {
        SpriteRenderer _sr; Sprite[] _f; Vector2[] _pads; HomeLife _life;
        int _pad; float _t, _nextJump, _nextCroak, _jumpT = -1f;
        Vector2 _from, _to;

        public void Init(SpriteRenderer sr, Sprite[] frames, Vector2[] pads, HomeLife life)
        {
            _sr = sr; _f = frames; _pads = pads; _life = life;
            _nextJump = Time.time + Random.Range(8f, 15f);
            _nextCroak = Time.time + Random.Range(4f, 9f);
        }

        void Update()
        {
            if (_f == null || _f.Length < 5) return;
            if (_jumpT >= 0f)
            {
                _jumpT += Time.deltaTime * 2.5f;
                float u = Mathf.Clamp01(_jumpT);
                var p = Vector2.Lerp(_from, _to, u);
                p.y += Mathf.Sin(u * Mathf.PI) * 0.35f;
                transform.localPosition = p;
                _sr.sprite = u < 0.2f ? _f[2] : (u < 0.85f ? _f[3] : _f[4]);
                _sr.flipX = _to.x < _from.x;
                if (u >= 1f)
                {
                    _jumpT = -1f;
                    _life?.Ripple(_to);
                    _nextJump = Time.time + Random.Range(8f, 15f);
                }
                return;
            }
            _t += Time.deltaTime * 3f;
            _sr.sprite = _f[(int)_t % 2];
            if (DayNight.IsNight && Time.time >= _nextCroak)
            {
                _nextCroak = Time.time + Random.Range(4f, 9f);
                FarmEmote.Show(transform, 1, _sr);
            }
            if (Time.time < _nextJump) return;
            int next = (_pad + Random.Range(1, _pads.Length)) % _pads.Length;
            _from = _pads[_pad];
            _to = _pads[next];
            _pad = next;
            _jumpT = 0f;
        }
    }
}
