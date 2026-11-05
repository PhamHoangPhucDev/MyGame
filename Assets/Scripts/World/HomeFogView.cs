using System.Collections.Generic;
using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Visual only: a translucent drifting mist over the locked Home chunks (grass and trees show faintly through),
    /// with a soft, breathing edge, and a few-second clearing when a chunk opens.
    /// Reads HomeArea (mask of open chunks); it never opens chunks, spends resources or writes the save.
    /// Art: Resources/World/HomeDistrict/Fog. Contract: Docs/WorldArt/HomeDistrict/build/FOG_EXPANSION_CONTRACT.md.
    /// </summary>
    public class HomeFogView : MonoBehaviour
    {
        public static bool Enabled = true;
        /// <summary>Preview only (no save): when >= 0 it replaces HomeArea.Mask for this view.</summary>
        public static int PreviewMask = -1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { PreviewMask = -1; }

        const string Dir = "World/HomeDistrict/Fog/";
        const int PadU = 16;                                       // mist past the grass, same art as the locked chunks
        const int X0 = -17 - PadU, Y0 = -8 - PadU, WU = 34 + PadU * 2, HU = 18 + PadU * 2, PPU = 16;
        const int TW = WU * PPU, TH = HU * PPU;
        const int Order = 2800;                                     // where HomeArea drew its fog cells
        const float Dissolve = 3.2f;                                // seconds for a chunk to clear
        const float Ramp = 44f;                                     // px from the open edge to full mist
        const float RimU = 0.5f;                                    // mist overlaps the grass edge so the clear colour cannot show
        const int All = 0x1FF;

        // mist colours: warm white highlights, soft blue-grey body; alpha steps keep the pixel-art look
        static readonly Color32 C0 = new Color32(0xF6, 0xF8, 0xF0, 0);
        static readonly Color32 C1 = new Color32(0xDE, 0xE8, 0xE6, 0);
        static readonly Color32 C2 = new Color32(0xBC, 0xCC, 0xD0, 0);
        static readonly Color32 Glow = new Color32(0xFF, 0xFC, 0xE8, 0);   // the clearing front catches the light
        static readonly byte[] Steps = { 0, 36, 72, 108, 140, 168, 192 };  // 75% at most: grass and trees stay readable
        static readonly byte[] RimSteps = { 0, 210, 235, 248, 255, 255, 255 };  // the rim has no art under it, so it goes solid

        public static void Spawn(Transform root)
        {
            if (!Enabled || root == null || root.Find("HomeFog") != null) return;
            var go = new GameObject("HomeFog");
            go.transform.SetParent(root, false);
            go.AddComponent<HomeFogView>();
        }

        static int CurrentMask() => PreviewMask >= 0 ? PreviewMask : HomeArea.Mask;

        int _shown;
        Texture2D _tex;
        SpriteRenderer _sr;
        Color32[] _px;
        float[] _dIn;                                  // px distance to open ground for the shown mask
        float _nextBake;
        static float[] _mistA, _mistB, _streak;        // 256x256 tileable maps
        // clearing
        bool _dissolving;
        float _t;
        float[] _key;                                  // per texel: when it clears (0..1), -1 when it does not change
        readonly HashSet<int> _seenBounds = new HashSet<int>();
        float _nextBoundsCheck;

        static void EnsureMaps()
        {
            if (_mistA != null && _mistB != null && _streak != null) return;   // statics are lost when scripts reload in Play Mode
            _mistA = Mist(64, 32, 11, 1f);
            _mistB = Mist(32, 16, 29, 1f);
            _streak = Mist(64, 32, 53, 0.25f);                             // long horizontal wisps
        }

        void Start()
        {
            EnsureMaps();
            _tex = new Texture2D(TW, TH, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "HomeFog" };
            _px = new Color32[TW * TH];
            var go = new GameObject("FogMist");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(X0, Y0, 0f);
            _sr = go.AddComponent<SpriteRenderer>();
            _sr.sprite = Sprite.Create(_tex, new Rect(0, 0, TW, TH), Vector2.zero, PPU, 0, SpriteMeshType.FullRect);
            _sr.sortingOrder = Order;

            _shown = CurrentMask();
            _dIn = Chamfer(OpenGrid(_shown));
            Bake(Time.unscaledTime);
            HideCellFog();
        }

        void Update()
        {
            if (_tex == null || _sr == null || _dIn == null) return;
            EnsureMaps();
            if (_px == null) _px = new Color32[TW * TH];
            float now = Time.unscaledTime;
            if (now >= _nextBoundsCheck) { _nextBoundsCheck = now + 0.25f; HideCellFog(); }

            int mask = CurrentMask();
            if (!_dissolving && mask != _shown)
            {
                if ((mask & ~_shown) != 0 && (_shown & ~mask) == 0) BeginDissolve(mask);
                else { _shown = mask; _dIn = Chamfer(OpenGrid(mask)); _nextBake = 0f; }   // locked again (reload, preview reset)
            }
            if (_dissolving)
            {
                _t += Time.unscaledDeltaTime;
                if (_t >= Dissolve) { _dissolving = false; _key = null; _nextBake = 0f; }
            }
            if (now >= _nextBake) { _nextBake = now + 1f / 12f; Bake(now); }
        }

        // ---------- state ----------

        static bool[] OpenGrid(int mask)
        {
            var o = new bool[TW * TH];
            for (int y = 0; y < TH; y++) for (int x = 0; x < TW; x++)
            {
                float lx = X0 + (x + 0.5f) / PPU, ly = Y0 + (y + 0.5f) / PPU;
                bool grass = lx >= -17f + RimU && lx < 17f - RimU && ly >= -8f + RimU && ly < 10f - RimU;
                o[y * TW + x] = grass && (mask == All || HomeArea.OpenAt(new Vector2(lx, ly), mask));
            }
            return o;
        }

        /// <summary>Pixel distance to the nearest open texel (0 on open ground), 3-4 chamfer.</summary>
        static float[] Chamfer(bool[] open)
        {
            var d = new float[TW * TH];
            for (int i = 0; i < d.Length; i++) d[i] = open[i] ? 0f : 1e6f;
            for (int y = 0; y < TH; y++) for (int x = 0; x < TW; x++)
            {
                int i = y * TW + x; float v = d[i];
                if (v == 0f) continue;
                if (x > 0) v = Mathf.Min(v, d[i - 1] + 3f);
                if (y > 0)
                {
                    v = Mathf.Min(v, d[i - TW] + 3f);
                    if (x > 0) v = Mathf.Min(v, d[i - TW - 1] + 4f);
                    if (x < TW - 1) v = Mathf.Min(v, d[i - TW + 1] + 4f);
                }
                d[i] = v;
            }
            for (int y = TH - 1; y >= 0; y--) for (int x = TW - 1; x >= 0; x--)
            {
                int i = y * TW + x; float v = d[i];
                if (v == 0f) continue;
                if (x < TW - 1) v = Mathf.Min(v, d[i + 1] + 3f);
                if (y < TH - 1)
                {
                    v = Mathf.Min(v, d[i + TW] + 3f);
                    if (x < TW - 1) v = Mathf.Min(v, d[i + TW + 1] + 4f);
                    if (x > 0) v = Mathf.Min(v, d[i + TW - 1] + 4f);
                }
                d[i] = v;
            }
            for (int i = 0; i < d.Length; i++) d[i] /= 3f;
            return d;
        }

        // ---------- bake ----------

        void Bake(float now)
        {
            // three layers drift in different directions; offsets are whole pixels so the motion stays crisp
            int ax = (int)(now * 6f), ay = (int)(now * 1.5f);
            int bx = (int)(now * -4f), by = (int)(now * 2.5f);
            int sx = (int)(now * 11f), sy = (int)(now * 0.5f);
            float p = _dissolving ? Mathf.SmoothStep(0f, 1.05f, _t / Dissolve) : 1f;   // Unity SmoothStep = eased lerp
            var dist = _dIn;
            if (_dissolving)
            {
                // open whatever has cleared so far and measure again, so the mist keeps a soft edge all the way
                var open = new bool[TW * TH];
                for (int i = 0; i < open.Length; i++) open[i] = _key[i] < 0f ? _dIn[i] <= 0f : _key[i] <= p;
                dist = Chamfer(open);
            }
            for (int y = 0; y < TH; y++)
            {
                int row = y * TW;
                int ra = ((y + ay) & 255) << 8, rb = ((y + by) & 255) << 8, rs = ((y + sy) & 255) << 8;
                for (int x = 0; x < TW; x++)
                {
                    int i = row + x;
                    float a = _mistA[ra + ((x + ax) & 255)], b = _mistB[rb + ((x + bx) & 255)];
                    float n = a * 0.6f + b * 0.4f;                                 // 0..1 billowing mist
                    float s = _streak[rs + ((x - sx) & 255)];                      // fast thin streaks
                    // soft edge that breathes: the drifting mist pushes it in and out by ~14 px
                    float lx = X0 + (x + 0.5f) / PPU, ly = Y0 + (y + 0.5f) / PPU;
                    bool rim = lx < -17f + RimU || lx >= 17f - RimU || ly < -8f + RimU || ly >= 10f - RimU;
                    float dens = rim ? DensRim(dist[i]) : Dens(dist[i], n);
                    // the texels next in line to clear catch the light
                    bool front = _dissolving && _key[i] > p && _key[i] - p < 0.06f && dist[i] > 0f;
                    if (dens <= 0.001f) { _px[i] = default; continue; }
                    float lum = n * 0.8f + s * 0.3f + (Bayer4(x, y) - 0.5f) * 0.12f;   // dithered tone bands, no hard blotches
                    Color32 c = lum > 0.66f ? C0 : lum > 0.36f ? C1 : C2;
                    if (front) c = Color32.Lerp(c, Glow, 0.7f);
                    // denser where the mist billows; ordered dither between alpha steps for smooth pixel gradients
                    byte[] steps = rim ? RimSteps : Steps;
                    float alpha = rim
                        ? dens * (steps.Length - 1)
                        : dens * (0.6f + 0.36f * n + Mathf.Max(0f, s - 0.55f) * 0.5f) * (steps.Length - 1);
                    if (front) alpha += 0.8f;
                    int lo = Mathf.Clamp((int)alpha, 0, steps.Length - 1);
                    if (alpha - lo > Bayer4(x, y) && lo < steps.Length - 1) lo++;
                    c.a = steps[lo];
                    _px[i] = c;
                }
            }
            _tex.SetPixels32(_px);
            _tex.Apply(false);
            // Outside the grass stays mist even after every chunk is open, so the sprite cannot switch off.
            _sr.enabled = true;
        }

        static float Smooth(float e0, float e1, float x)
        {
            float t = Mathf.Clamp01((x - e0) / (e1 - e0));
            return t * t * (3f - 2f * t);
        }

        static float Dens(float d, float n)
        {
            if (d <= 0f) return 0f;
            return Mathf.Clamp01((d - 2f + (n - 0.5f) * 28f) / Ramp);
        }

        // The void past the grass has no art under it, so the rim cannot breathe outward.
        static float DensRim(float d)
        {
            if (d <= 0f) return 0f;
            return Mathf.Clamp01((d + 2f) / 6f);
        }

        // ---------- clearing ----------

        void BeginDissolve(int to)
        {
            var newD = Chamfer(OpenGrid(to));
            var oldD = _dIn;
            _key = new float[TW * TH];
            float maxD = 1f;
            for (int i = 0; i < _key.Length; i++) if (newD[i] <= 0f && oldD[i] > maxD) maxD = oldD[i];
            var cells = new List<int>();
            for (int i = 0; i < _key.Length; i++)
            {
                if (newD[i] <= 0f && oldD[i] > 0f)
                {
                    // the mist pulls back from the land that is already open, with a wavy front
                    _key[i] = 0.72f * (oldD[i] / maxD) + 0.28f * ValueNoise(i % TW, i / TW, 26f);
                    cells.Add(i);
                }
                else _key[i] = -1f;
            }
            _dIn = newD;
            _shown = to;
            _t = 0f;
            _dissolving = cells.Count > 0;
            SpawnWisps(cells);
        }

        void SpawnWisps(List<int> cells)
        {
            if (cells.Count == 0) return;
            var frames = new Sprite[4];
            for (int f = 0; f < 4; f++) frames[f] = Resources.Load<Sprite>(Dir + "FG_Wisp_" + f);
            if (frames[0] == null) return;
            int n = Mathf.Clamp(cells.Count / 2000, 8, 22);
            for (int w = 0; w < n; w++)
            {
                int i = cells[(int)(Hash01(w, cells.Count) * (cells.Count - 1))];
                var go = new GameObject("FogWisp");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(X0 + (i % TW + 0.5f) / PPU, Y0 + (i / TW + 0.5f) / PPU, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sortingOrder = Order + 1;
                sr.color = new Color(1f, 1f, 1f, 0.75f);
                sr.enabled = false;
                go.AddComponent<HomeFogWisp>().Init(frames, _key[i] * Dissolve * 0.8f);
            }
        }

        // ---------- helpers ----------

        /// <summary>HomeArea still spawns plain per-cell fog sprites; this view replaces their look (colliders and signs stay).</summary>
        void HideCellFog()
        {
            var root = transform.parent;
            if (root == null) return;
            foreach (Transform t in root)
            {
                if (t.name != "HomeBounds" || !_seenBounds.Add(t.GetInstanceID())) continue;
                foreach (Transform c in t)
                    if (c.name == "Fog" && c.TryGetComponent<SpriteRenderer>(out var sr)) sr.enabled = false;
            }
        }

        /// <summary>256x256 tileable value noise (two octaves), 0..1. yScale &lt; 1 stretches it into horizontal streaks.</summary>
        static float[] Mist(int cell, int cell2, int seed, float yScale)
        {
            var m = new float[256 * 256];
            for (int y = 0; y < 256; y++) for (int x = 0; x < 256; x++)
            {
                int yy = (int)(y / yScale) & 255;
                m[y * 256 + x] = 0.7f * Periodic(x, yy, cell, seed) + 0.3f * Periodic(x, yy, cell2, seed + 7);
            }
            return m;
        }

        static float Periodic(int x, int y, int cell, int seed)
        {
            int n = 256 / cell;
            float fx = (float)x / cell, fy = (float)y / cell;
            int ix = (int)fx, iy = (int)fy;
            float tx = fx - ix, ty = fy - iy;
            tx = tx * tx * (3f - 2f * tx); ty = ty * ty * (3f - 2f * ty);
            int x0 = ix % n, x1 = (ix + 1) % n, y0 = iy % n, y1 = (iy + 1) % n;
            float a = Mathf.Lerp(Hash01(x0 + seed, y0), Hash01(x1 + seed, y0), tx);
            float b = Mathf.Lerp(Hash01(x0 + seed, y1), Hash01(x1 + seed, y1), tx);
            return Mathf.Lerp(a, b, ty);
        }

        static readonly int[] B4 = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
        static float Bayer4(int x, int y) => (B4[(y & 3) * 4 + (x & 3)] + 0.5f) / 16f;

        static int Hash(int x, int y) { unchecked { int h = x * 73856093 ^ y * 19349663 ^ 0x5bd1e995; h = (h ^ (h >> 13)) * 1274126177; return (h ^ (h >> 16)) & 0x7fffffff; } }
        static float Hash01(int x, int y) => (Hash(x, y) & 0xFFFF) / 65535f;

        static float ValueNoise(int x, int y, float cell)
        {
            float fx = x / cell, fy = y / cell;
            int ix = Mathf.FloorToInt(fx), iy = Mathf.FloorToInt(fy);
            float tx = fx - ix, ty = fy - iy;
            tx = tx * tx * (3f - 2f * tx); ty = ty * ty * (3f - 2f * ty);
            float a = Mathf.Lerp(Hash01(ix, iy), Hash01(ix + 1, iy), tx);
            float b = Mathf.Lerp(Hash01(ix, iy + 1), Hash01(ix + 1, iy + 1), tx);
            return Mathf.Lerp(a, b, ty);
        }

        void OnDestroy()
        {
            if (_tex != null) Destroy(_tex);
        }
    }

    /// <summary>One small cloud lifting and breaking apart (FG_Wisp_0..3) while a chunk clears.</summary>
    public class HomeFogWisp : MonoBehaviour
    {
        Sprite[] _frames;
        float _delay, _age;
        SpriteRenderer _sr;
        Vector3 _start;
        const float Life = 1.2f;

        public void Init(Sprite[] frames, float delay)
        {
            _frames = frames; _delay = delay;
            _sr = GetComponent<SpriteRenderer>();
            _start = transform.localPosition;
        }

        void Update()
        {
            _age += Time.unscaledDeltaTime;
            float t = (_age - _delay) / Life;
            if (t < 0f) return;
            if (t >= 1f) { Destroy(gameObject); return; }
            _sr.enabled = true;
            int f = Mathf.Min(3, (int)(t * 4f));
            if (_frames[f] != null) _sr.sprite = _frames[f];
            // rises ~14 px and drifts with the mist
            transform.localPosition = _start + new Vector3(Mathf.Round(t * 6f) / 16f, Mathf.Round(t * 14f) / 16f, 0f);
        }
    }
}
