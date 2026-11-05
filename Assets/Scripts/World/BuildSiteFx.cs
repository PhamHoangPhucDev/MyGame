using System.Collections.Generic;
using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Construction look for any Home building except the wooden house (that one has hand-drawn frames,
    /// see HomeHouseBuildFx). Works from the finished sprite alone, so new buildings get it for free:
    /// a faint blueprint of the whole building, the built part rising from the ground in four steps,
    /// a timber scaffold sized to the building, hammers on the top plank, a saw at the side and dust.
    /// A building can ship its own frames instead: Resources "World/HomeDistrict/Build/BD_{id}_Frame_0..3".
    /// Display only: BuildMode.PaintSites feeds it progress; it never writes the save.
    /// </summary>
    public class BuildSiteFx : MonoBehaviour
    {
        const string Dir = "World/HomeDistrict/Build/";
        const float Ppu = 16f;
        static readonly float[] Rise = { 0.08f, 0.38f, 0.66f, 0.88f };   // built height per stage
        static readonly Color Ghost = new Color(0.85f, 0.94f, 1f, 0.45f);
        static Sprite _pole, _plank;
        static readonly Dictionary<Sprite, Sprite[]> Cut = new Dictionary<Sprite, Sprite[]>();
        static readonly Dictionary<string, Sprite[]> Frames = new Dictionary<string, Sprite[]>();

        SpriteRenderer _sr, _ghost, _saw;
        SpriteRenderer[] _hammers = new SpriteRenderer[2];
        readonly List<SpriteRenderer> _poles = new List<SpriteRenderer>();
        readonly List<SpriteRenderer> _planks = new List<SpriteRenderer>();
        Transform _rig;                    // children here are drawn at world pixel size (undo the stage scale)
        int _stage = -1;
        bool _building;
        float _speed = 1f, _sawClock;
        float[] _hammerClock = { 0f, 0.24f };
        int[] _hammerFrame = new int[2];
        Rect _box;                         // building box in rig units: x/width centred, y from the ground up

        /// <summary>
        /// Paints one site. Returns true while it is under construction (the caller then leaves the stage alone);
        /// false once it is done or when the art is missing, so the caller paints the finished building.
        /// </summary>
        public static bool Paint(Transform stage, string id, Sprite finished, float scale, bool flip, float progress, bool building, bool near)
        {
            var fx = stage.GetComponent<BuildSiteFx>();
            if (!building)
            {
                if (fx != null) fx.Finish();
                return false;
            }
            if (finished == null || !HomeHouseBuildFx.LoadArt() || !LoadScaffold()) return false;
            if (fx == null) fx = stage.gameObject.AddComponent<BuildSiteFx>();
            fx.Show(id, finished, scale, flip, progress, near);
            return true;
        }

        static bool LoadScaffold()
        {
            if (_pole == null) _pole = Resources.Load<Sprite>(Dir + "BD_Pole");
            if (_plank == null) _plank = Resources.Load<Sprite>(Dir + "BD_Plank");
            return _pole != null && _plank != null;
        }

        void Show(string id, Sprite finished, float scale, bool flip, float progress, bool near)
        {
            if (_sr == null) Build();
            _speed = near ? 2f : 1f;
            transform.localScale = new Vector3(scale, scale, 1f);
            _rig.localScale = new Vector3(1f / scale, 1f / scale, 1f);
            _sr.color = Color.white;
            _sr.flipX = flip;
            _ghost.flipX = flip;
            _ghost.sprite = finished;
            int stage = progress < 0.25f ? 0 : progress < 0.5f ? 1 : progress < 0.75f ? 2 : 3;
            var own = OwnFrames(id);
            _sr.sprite = own != null ? own[stage] : Rising(finished)[stage];
            _ghost.enabled = own == null;

            var b = finished.bounds;           // sprite units; the rig works in world units
            _box = new Rect(b.min.x * scale, b.min.y * scale, b.size.x * scale, b.size.y * scale);
            if (stage != _stage)
            {
                bool first = _stage < 0;
                _stage = stage;
                Layout(stage);
                if (!first) for (int i = 0; i < 3; i++) Dust(new Vector3(_box.xMin + _box.width * (0.2f + i * 0.3f), _box.yMin, 0f), 1f, i * 0.06f);
            }
            SetActive(true);
            _building = true;
        }

        void Build()
        {
            _sr = GetComponent<SpriteRenderer>();
            _ghost = Child(transform, "Blueprint");
            _ghost.color = Ghost;
            _rig = new GameObject("Rig").transform;
            _rig.SetParent(transform, false);
            for (int i = 0; i < 2; i++) _hammers[i] = Child(_rig, "Hammer" + i);
            _saw = Child(_rig, "Saw");
        }

        static SpriteRenderer Child(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<SpriteRenderer>();
        }

        /// <summary>Places the scaffold and the tools around the part built so far.</summary>
        void Layout(int stage)
        {
            float w = _box.width, h = _box.height, x0 = _box.xMin - 0.15f, x1 = _box.xMax + 0.15f, y0 = _box.yMin;
            float top = y0 + Mathf.Max(0.35f, h * Rise[stage]);
            float poleTop = Mathf.Min(y0 + h + 0.1f, top + 0.6f);
            if (stage == 0) poleTop = y0 + 0.5f;

            // poles: both ends, plus one every ~2.5 units on wide buildings
            int count = 2 + Mathf.Max(0, Mathf.FloorToInt(w / 2.5f) - 1);
            for (int i = 0; i < count; i++)
            {
                float x = Mathf.Lerp(x0, x1, count == 1 ? 0.5f : i / (float)(count - 1));
                var pole = Pooled(_poles, i, _pole, "Pole");
                pole.transform.localPosition = new Vector3(x, y0, 0f);
                pole.size = new Vector2(_pole.rect.width / Ppu, poleTop - y0);
            }
            for (int i = count; i < _poles.Count; i++) _poles[i].enabled = false;

            // planks: one on the ground, one walkway at the built height, one half way on tall buildings
            var heights = new List<float> { y0 + 0.05f };
            if (stage > 0) heights.Add(top);
            if (stage > 1 && h > 2.5f) heights.Add((y0 + top) * 0.5f);
            for (int i = 0; i < heights.Count; i++)
            {
                var plank = Pooled(_planks, i, _plank, "Plank");
                plank.transform.localPosition = new Vector3((x0 + x1) * 0.5f, heights[i], 0f);
                plank.size = new Vector2(x1 - x0 + 0.3f, _plank.rect.height / Ppu);
            }
            for (int i = heights.Count; i < _planks.Count; i++) _planks[i].enabled = false;

            float work = stage == 0 ? y0 + 0.3f : top + 0.15f;
            _hammers[0].transform.localPosition = new Vector3(_box.xMin + w * 0.3f, work, 0f);
            _hammers[1].transform.localPosition = new Vector3(_box.xMin + w * 0.72f, work, 0f);
            _saw.transform.localPosition = new Vector3(x1 + 0.7f, y0, 0f);
        }

        SpriteRenderer Pooled(List<SpriteRenderer> list, int i, Sprite sprite, string name)
        {
            while (list.Count <= i)
            {
                var sr = Child(_rig, name + list.Count);
                sr.sprite = sprite;
                sr.drawMode = SpriteDrawMode.Tiled;
                list.Add(sr);
            }
            list[i].enabled = true;
            return list[i];
        }

        void Update()
        {
            if (!_building) return;
            float dt = Time.deltaTime * _speed;
            var hammer = HomeHouseBuildFx.HammerFrames;
            for (int i = 0; i < 2; i++)
            {
                _hammerClock[i] += dt;
                float hold = HomeHouseBuildFx.HammerBeat[_hammerFrame[i]];
                if (_hammerClock[i] >= hold)
                {
                    _hammerClock[i] -= hold;
                    _hammerFrame[i] = (_hammerFrame[i] + 1) % HomeHouseBuildFx.HammerBeat.Length;
                    if (_hammerFrame[i] == 2) Dust(_hammers[i].transform.localPosition, 0.5f, 0f);
                }
                _hammers[i].sprite = hammer[_hammerFrame[i]];
            }
            _sawClock += dt;
            var saw = HomeHouseBuildFx.SawFrames;
            _saw.sprite = saw[(int)(_sawClock / 0.09f) % saw.Length];
        }

        void LateUpdate()
        {
            if (!_building) return;
            int o = _sr.sortingOrder;
            _ghost.sortingOrder = o - 1;
            foreach (var p in _poles) p.sortingOrder = o + 1;
            foreach (var p in _planks) p.sortingOrder = o + 2;
            for (int i = 0; i < 2; i++) _hammers[i].sortingOrder = o + 3;
            _saw.sortingOrder = o + 3;
        }

        /// <summary>Building finished: hide the site dressing and puff a dust cloud across it.</summary>
        void Finish()
        {
            if (!_building) return;
            _building = false;
            _stage = -1;
            SetActive(false);
            int n = Mathf.Clamp(Mathf.RoundToInt(_box.width / 0.9f), 2, 7);
            float size = Mathf.Clamp(_box.width / n * 1.6f, 1f, 2.4f);
            for (int i = 0; i < n; i++)
            {
                float x = Mathf.Lerp(_box.xMin, _box.xMax, n == 1 ? 0.5f : i / (float)(n - 1));
                Dust(new Vector3(x, _box.yMin, 0f), size, (i % 3) * 0.05f);
                if (_box.height > 1.5f) Dust(new Vector3(x * 0.8f, _box.yMin + _box.height * 0.5f, 0f), size * 0.8f, 0.1f + (i % 2) * 0.06f);
            }
        }

        void SetActive(bool on)
        {
            if (_ghost != null) _ghost.enabled = on && _ghost.sprite != null && _ghost.enabled;
            if (!on && _ghost != null) _ghost.enabled = false;
            foreach (var p in _poles) if (!on) p.enabled = false;
            foreach (var p in _planks) if (!on) p.enabled = false;
            for (int i = 0; i < 2; i++) if (_hammers[i] != null) _hammers[i].enabled = on;
            if (_saw != null) _saw.enabled = on;
        }

        void Dust(Vector3 rigLocal, float scale, float delay)
        {
            var go = new GameObject("BuildDust");
            go.transform.SetParent(_rig, false);
            go.transform.localPosition = rigLocal;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            go.AddComponent<HomeBuildDustPuff>().Init(HomeHouseBuildFx.DustFrames, delay, () => _speed, () => _sr.sortingOrder + 4);
        }

        /// <summary>The finished sprite cut at each stage height, bottom part only.</summary>
        static Sprite[] Rising(Sprite s)
        {
            if (Cut.TryGetValue(s, out var list) && list[0] != null) return list;
            list = new Sprite[Rise.Length];
            var r = s.rect;
            for (int i = 0; i < Rise.Length; i++)
            {
                float h = Mathf.Max(1f, Mathf.Round(r.height * Rise[i]));
                var pivot = new Vector2(s.pivot.x / r.width, s.pivot.y / h);
                list[i] = Sprite.Create(s.texture, new Rect(r.x, r.y, r.width, h), pivot, s.pixelsPerUnit, 0, SpriteMeshType.FullRect);
                list[i].name = s.name + "_Rise" + i;
            }
            Cut[s] = list;
            return list;
        }

        static Sprite[] OwnFrames(string id)
        {
            if (Frames.TryGetValue(id, out var list)) return list;
            list = new Sprite[4];
            for (int i = 0; i < 4; i++)
            {
                list[i] = Resources.Load<Sprite>(Dir + "BD_" + id + "_Frame_" + i);
                if (list[i] == null) { list = null; break; }
            }
            Frames[id] = list;
            return list;
        }
    }
}
