using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Shows the wooden house going up while it is being built: the timber frame grows in four stages
    /// with the build progress, hammers knock, a saw cuts planks and dust puffs rise. A big dust burst
    /// plays when the house is done. Display only: it reads BuildMode and never writes the save.
    /// </summary>
    public class HomeHouseBuildFx : MonoBehaviour
    {
        const string Dir = "World/HomeDistrict/Build/";
        const float Ppu = 16f;
        const float NearRadius = 2.2f;          // same radius BuildMode uses for the 2x speed-up

        // Where tools work on each stage, in frame pixels (x from left, y from top of the 128x104 canvas).
        static readonly Vector2[][] HammerAt =
        {
            new[] { new Vector2(42f, 91f), new Vector2(86f, 91f) },
            new[] { new Vector2(40f, 75f), new Vector2(60f, 61f) },
            new[] { new Vector2(38f, 58f), new Vector2(64f, 21f) },
            new[] { new Vector2(50f, 37f), new Vector2(80f, 38f) },
        };
        static readonly Vector2[] SawAt = { new Vector2(60f, 95f), new Vector2(92f, 95f), new Vector2(92f, 95f), new Vector2(66f, 95f) };
        static readonly float[] HammerHold = { 0.18f, 0.07f, 0.12f, 0.1f };   // raise, swing, strike, rebound

        static Sprite[] _frames, _hammer, _saw, _dust;

        SpriteRenderer _frame;
        SpriteRenderer[] _hammers = new SpriteRenderer[2];
        SpriteRenderer _sawSr;
        Transform _slot;
        int _stage = -1;
        bool _building, _seen;
        float _sawClock;
        float[] _hammerClock = new float[2];
        int[] _hammerFrame = new int[2];

        public static HomeHouseBuildFx Spawn(Transform root, Vector3 local)
        {
            if (!LoadArt()) return null;
            var go = new GameObject("HouseBuildFx");
            go.transform.SetParent(root, false);
            go.transform.localPosition = local;
            var fx = go.AddComponent<HomeHouseBuildFx>();
            fx.Init();
            return fx;
        }

        /// <summary>True while the frame art is shown, so the old scaffold can stay hidden.</summary>
        public bool Showing => _building;

        internal static Sprite[] HammerFrames => _hammer;
        internal static Sprite[] SawFrames => _saw;
        internal static Sprite[] DustFrames => _dust;
        internal static readonly float[] HammerBeat = HammerHold;

        internal static bool LoadArt()
        {
            // Domain reload may be off: sprites made at runtime die with Play Mode while the static array survives.
            if (_frames != null && _frames[0] != null && _hammer[0] != null && _saw[0] != null && _dust[0] != null) return true;
            _frames = null;
            var frames = new Sprite[4];
            for (int i = 0; i < 4; i++)
            {
                frames[i] = Resources.Load<Sprite>(Dir + "BD_HouseFrame_" + i);
                if (frames[i] == null) return false;
            }
            _hammer = Slice("BD_Hammer_Sheet", 16, new Vector2(3f / 16f, 2f / 16f));
            _saw = Slice("BD_Saw_Sheet", 24, new Vector2(0.5f, 1f / 16f));
            _dust = Slice("BD_Dust_Sheet", 24, new Vector2(0.5f, 2f / 24f));
            if (_hammer == null || _saw == null || _dust == null) return false;
            _frames = frames;
            return true;
        }

        static Sprite[] Slice(string name, int cell, Vector2 pivot)
        {
            var tex = Resources.Load<Texture2D>(Dir + name);
            if (tex == null) return null;
            int count = tex.width / cell;
            var list = new Sprite[count];
            for (int i = 0; i < count; i++)
            {
                list[i] = Sprite.Create(tex, new Rect(i * cell, 0, cell, tex.height), pivot, Ppu, 0, SpriteMeshType.FullRect);
                list[i].name = name + "_" + i;
            }
            return list;
        }

        void Init()
        {
            _frame = gameObject.AddComponent<SpriteRenderer>();
            gameObject.AddComponent<SpriteSorter>();
            for (int i = 0; i < 2; i++)
            {
                _hammers[i] = Child("Hammer" + i, _hammer[0]);
                _hammerClock[i] = i * 0.24f;   // second hammer knocks off-beat
            }
            _sawSr = Child("Saw", _saw[0]);
            Refresh(true);
        }

        SpriteRenderer Child(string name, Sprite sprite)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            return sr;
        }

        void Update()
        {
            Refresh(false);
            if (!_building) return;
            float dt = Time.deltaTime * (HeroNear() ? 2f : 1f);
            for (int i = 0; i < 2; i++) TickHammer(i, dt);
            _sawClock += dt;
            _sawSr.sprite = _saw[(int)(_sawClock / 0.09f) % _saw.Length];
        }

        void LateUpdate()
        {
            if (!_building) return;
            int order = _frame.sortingOrder;
            for (int i = 0; i < 2; i++) _hammers[i].sortingOrder = order + 1;
            _sawSr.sortingOrder = order + 1;
        }

        void Refresh(bool first)
        {
            bool building = BuildMode.HouseBuildProgress(out float progress);
            if (building)
            {
                int stage = progress < 0.2f ? 0 : progress < 0.45f ? 1 : progress < 0.7f ? 2 : 3;
                if (stage != _stage)
                {
                    _stage = stage;
                    _frame.sprite = _frames[stage];
                    for (int i = 0; i < 2; i++) _hammers[i].transform.localPosition = Px(HammerAt[stage][i]);
                    _sawSr.transform.localPosition = Px(SawAt[stage]);
                    if (!first) for (int i = 0; i < 3; i++) Dust(Px(new Vector2(30f + i * 34f, 92f)), 1.2f, i * 0.06f);
                }
            }
            else if (_building && _seen && Housing.HasHouse) Burst();

            if (building) _seen = true;
            _building = building;
            _frame.enabled = building;
            for (int i = 0; i < 2; i++) _hammers[i].enabled = building;
            _sawSr.enabled = building;
            if (!building) _stage = -1;
        }

        void TickHammer(int i, float dt)
        {
            _hammerClock[i] += dt;
            float hold = HammerHold[_hammerFrame[i]];
            if (_hammerClock[i] < hold) return;
            _hammerClock[i] -= hold;
            _hammerFrame[i] = (_hammerFrame[i] + 1) % HammerHold.Length;
            _hammers[i].sprite = _hammer[_hammerFrame[i]];
            if (_hammerFrame[i] == 2) Dust(_hammers[i].transform.localPosition + new Vector3(0.05f, -0.05f, 0f), 0.6f, 0f);
        }

        /// <summary>Big dust cloud over the whole frame as the finished house appears.</summary>
        void Burst()
        {
            for (int i = 0; i < 7; i++)
            {
                float x = -3.2f + i * 1.07f;
                Dust(new Vector3(x, 0.2f + (i % 2) * 0.5f, 0f), 2.4f, (i % 3) * 0.05f);
                Dust(new Vector3(x * 0.8f, 2.4f + (i % 3) * 0.6f, 0f), 2f, 0.1f + (i % 2) * 0.06f);
            }
        }

        void Dust(Vector3 local, float scale, float delay)
        {
            var go = new GameObject("BuildDust");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = local;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var puff = go.AddComponent<HomeBuildDustPuff>();
            puff.Init(_dust, delay, () => Speed, () => _frame.sortingOrder + 2);
        }

        public float Speed => HeroNear() ? 2f : 1f;

        bool HeroNear()
        {
            if (_slot == null) { var s = GameObject.Find("slot_house"); _slot = s != null ? s.transform : transform; }
            var hero = GameObject.FindGameObjectWithTag("Player");
            if (hero == null) return false;
            return ((Vector2)hero.transform.position - (Vector2)_slot.position).sqrMagnitude < NearRadius * NearRadius;
        }

        static Vector3 Px(Vector2 p) => new Vector3((p.x - 64f) / Ppu, (104f - p.y) / Ppu, 0f);
    }

    /// <summary>One dust puff: plays the dust sheet once while drifting up, then removes itself.</summary>
    public class HomeBuildDustPuff : MonoBehaviour
    {
        const float FrameTime = 0.08f;
        Sprite[] _frames;
        SpriteRenderer _sr;
        System.Func<float> _speed;
        System.Func<int> _order;
        float _t, _delay;

        /// <summary>speed: animation rate (2 near the hero); order: sorting order to draw at, read each frame.</summary>
        public void Init(Sprite[] frames, float delay, System.Func<float> speed, System.Func<int> order)
        {
            _frames = frames; _delay = delay; _speed = speed; _order = order;
            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.enabled = false;
        }

        void Update()
        {
            float speed = 1f, order = 0;
            try { speed = _speed(); order = _order(); } catch (MissingReferenceException) { Destroy(gameObject); return; }
            _t += Time.deltaTime * speed;
            if (_t < _delay) return;
            int f = (int)((_t - _delay) / FrameTime);
            if (f >= _frames.Length) { Destroy(gameObject); return; }
            _sr.enabled = true;
            _sr.sprite = _frames[f];
            _sr.sortingOrder = (int)order;
            transform.localPosition += Vector3.up * (0.25f * Time.deltaTime * speed);
        }
    }
}
