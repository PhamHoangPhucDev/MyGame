using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;

namespace GameplayPrototype
{
    /// <summary>
    /// Clearable clutter on the Home map (WILD_START_HANDOFF section 3). Absent save key means the approved
    /// layout: nothing to clear and nothing regrows. Step 5 writes the key for a wild start.
    /// </summary>
    public static class HomeClear
    {
        public enum Kind { Weed, Thorn, Stump, Log, Rock, BigRock }

        public struct Node
        {
            public int id;
            public Kind kind;
            public Vector2 pos;
        }

        const string Key = WorldCatalog.SavePrefix + "home.clutter";
        const string SeenKey = WorldCatalog.SavePrefix + "home.clear.seen";
        const string DayKey = WorldCatalog.SavePrefix + "home.weed.day";
        const string Art = "World/HomeDistrict/Wild/WL_";
        const int WeedCap = 6;

        static readonly string[] Sprites = { "Weed_0", "ThornBush", "Stump", "Log", "SmallRocks", "BigRock" };
        static readonly string[] Items = { "fiber", "fiber", "wood", "wood", "ore", "ore" };
        static readonly int[] Amounts = { 1, 2, 2, 3, 1, 3 };
        static readonly Vector2[] Feet = {
            new Vector2(0.8f, 0.5f), new Vector2(2.0f, 0.8f), new Vector2(0.8f, 0.5f),
            new Vector2(1.8f, 0.5f), new Vector2(1.2f, 0.6f), new Vector2(1.4f, 0.6f)
        };

        static List<Node> _nodes;
        static bool _active;

        /// <summary>Clutter currently being held, and 0..1 of its hold. The gold bar UI reads these.</summary>
        public static HomeClearable Target { get; private set; }
        public static float Progress { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _nodes = null;
            _active = false;
            Target = null;
            Progress = 0f;
        }

        public static bool Active { get { Load(); return _active; } }
        public static IReadOnlyList<Node> Nodes { get { Load(); return _nodes; } }
        public static float HoldSeconds(Kind kind) => kind == Kind.Weed ? 0.3f : 0.6f;
        public static string Item(Kind kind) => Items[(int)kind];
        public static int Amount(Kind kind) => Amounts[(int)kind];

        public static Rect Foot(Kind kind, Vector2 pos)
        {
            var s = Feet[(int)kind];
            return new Rect(pos.x - s.x * 0.5f, pos.y, s.x, s.y);
        }

        public static bool Overlaps(Rect r)
        {
            Load();
            for (int i = 0; i < _nodes.Count; i++)
                if (r.Overlaps(Foot(_nodes[i].kind, _nodes[i].pos))) return true;
            return false;
        }

        /// <summary>Spawns the saved clutter under the Home root. Safe when the wild key is absent.</summary>
        public static void Spawn(Transform root)
        {
            Load();
            if (root.GetComponent<HomeClearDriver>() == null) root.gameObject.AddComponent<HomeClearDriver>();
            for (int i = 0; i < _nodes.Count; i++) SpawnOne(root, _nodes[i]);
        }

        /// <summary>Adds one clutter object and saves it. Turns wild regrowth on.</summary>
        public static int Place(Kind kind, Vector2 pos)
        {
            Load();
            _active = true;
            int id = 1;
            for (int i = 0; i < _nodes.Count; i++) if (_nodes[i].id >= id) id = _nodes[i].id + 1;
            var node = new Node { id = id, kind = kind, pos = pos };
            _nodes.Add(node);
            WorldTx.Begin().SetString(Key, Encode()).Commit();
            var root = GameObject.Find("HomeDistrict");
            if (root != null) SpawnOne(root.transform, node);
            return id;
        }

        static bool _request;

        /// <summary>One frame of hold from the E button (mobile) or a test. The keyboard hold is read as well.</summary>
        public static void RequestHold() { _request = true; }

        /// <summary>Hold progressed this frame. Finishes the clear when the bar fills. Releasing resets it.</summary>
        public static void Pump(float dt)
        {
            bool held = _request || InteractHeld();
            _request = false;
            Target = null;
            Progress = 0f;
            if (!held || dt <= 0f || BuildMode.IsOpen || Mailbox.IsOpen)
            {
                Release();
                return;
            }
            var hero = GameObject.FindGameObjectWithTag("Player");
            var root = GameObject.Find("HomeDistrict");
            if (hero == null || root == null) { Release(); return; }
            var zone = root.transform.parent != null ? root.transform.parent : root.transform;
            var all = zone.GetComponentsInChildren<WorldInteractable>(false);
            WorldInteractable best = null;
            float bestD = float.MaxValue;
            var hp = (Vector2)hero.transform.position;
            for (int i = 0; i < all.Length; i++)
            {
                if (!all[i].isActiveAndEnabled || !all[i].InRange(hp)) continue;
                float d = ((Vector2)all[i].transform.position - hp).sqrMagnitude;
                if (d < bestD) { bestD = d; best = all[i]; }
            }
            if (best == null || !best.TryGetComponent<HomeClearable>(out var clear)) { Release(); return; }
            Release(clear);
            Target = clear;
            clear.Held += dt;
            Progress = Mathf.Clamp01(clear.Held / HoldSeconds(clear.Kind));
            if (clear.Held >= HoldSeconds(clear.Kind)) Finish(clear.Id);
        }

        public static bool Finish(int id)
        {
            Load();
            int at = _nodes.FindIndex(n => n.id == id);
            if (at < 0) return false;
            var node = _nodes[at];
            _nodes.RemoveAt(at);
            string item = Item(node.kind);
            int n = Amount(node.kind);
            int seen = PlayerPrefs.GetInt(SeenKey, 0);
            bool first = (seen & (1 << (int)node.kind)) == 0;
            if (first) seen |= 1 << (int)node.kind;
            var tx = WorldTx.Begin()
                .SetInt(WorldSave.KeyItem(item), WorldSave.GetQty(item) + n)
                .SetString(Key, Encode())
                .SetInt(SeenKey, seen);
            tx.After(() => WorldEvents.RaiseItemGained(item, n));
            if (first) tx.After(() => RewardBanner.Show(item, n));
            tx.Commit();
            var go = GameObject.Find("Clutter_" + id);
            if (go != null) Object.Destroy(go);
            Target = null;
            Progress = 0f;
            return true;
        }

        /// <summary>One weed per in-game day on untouched centre-chunk grass, and never more than 6 weeds.</summary>
        public static bool Regrow()
        {
            Load();
            if (!_active) return false;
            int day = Mailbox.Day;
            if (!PlayerPrefs.HasKey(DayKey))
            {
                PlayerPrefs.SetInt(DayKey, day);
                return false;
            }
            if (PlayerPrefs.GetInt(DayKey, day) >= day) return false;
            PlayerPrefs.SetInt(DayKey, day);
            int weeds = 0;
            for (int i = 0; i < _nodes.Count; i++) if (_nodes[i].kind == Kind.Weed) weeds++;
            if (weeds >= WeedCap) return false;
            var open = new List<Vector2Int>();
            for (int y = -5; y <= 5; y++)
                for (int x = -5; x <= 4; x++)
                    if (OpenGrass(new Vector2Int(x, y))) open.Add(new Vector2Int(x, y));
            if (open.Count == 0) return false;
            var c = open[(day * 13) % open.Count];
            Place(Kind.Weed, new Vector2(c.x + 0.5f, c.y + 0.15f));
            return true;
        }

        static bool OpenGrass(Vector2Int c)
        {
            var centre = new Vector2(c.x + 0.5f, c.y + 0.5f);
            if (centre.x < -5f || centre.x > 5f || centre.y < -4.5f || centre.y > 6f) return false;
            var r = new Rect(c.x, c.y, 1f, 1f);
            if (HomeDecor.FixedBlock(r).Length > 0) return false;
            int painted = HomeLand.Kind(c);
            if (painted == HomeLand.Path || painted == HomeLand.Plot) return false;
            if (HomeLand.Mask(c) != 0) return false;
            if (HomeDistrict.BaseKind(centre) != 0) return false;
            if (Overlaps(r)) return false;
            var pieces = HomeLayout.Pieces;
            for (int i = 0; i < pieces.Count; i++)
                if (r.Overlaps(HomeDecor.Foot(pieces[i].def, pieces[i].pos))) return false;
            return true;
        }

        static void SpawnOne(Transform root, Node node)
        {
            string file = Sprites[(int)node.kind];
            if (node.kind == Kind.Weed && node.id % 2 == 1) file = "Weed_1";
            var go = new GameObject("Clutter_" + node.id);
            go.transform.SetParent(root, false);
            go.transform.localPosition = node.pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>(Art + file);
            go.AddComponent<SpriteSorter>();
            if (node.kind == Kind.Thorn || node.kind == Kind.Stump || node.kind == Kind.Log || node.kind == Kind.BigRock)
            {
                var box = go.AddComponent<BoxCollider2D>();
                var foot = Feet[(int)node.kind];
                box.size = new Vector2(foot.x * 0.8f, 0.35f);
                box.offset = new Vector2(0f, 0.18f);
            }
            var talk = go.AddComponent<WorldInteractable>();
            talk.DisplayName = "DON";
            talk.Radius = 1f;
            var clear = go.AddComponent<HomeClearable>();
            clear.Id = node.id;
            clear.Kind = node.kind;
        }

        static void Release(HomeClearable keep = null)
        {
            var root = GameObject.Find("HomeDistrict");
            if (root == null) return;
            var all = root.GetComponentsInChildren<HomeClearable>(false);
            for (int i = 0; i < all.Length; i++)
                if (all[i] != keep) all[i].Held = 0f;
        }

        static bool InteractHeld()
        {
            var kb = PlayerChat.Typing ? null : Keyboard.current; // chat line open: game keys off
            if (kb == null) return false;
            string key = WorldSave.InteractBinding;
            if (key == "f") return kb.fKey.isPressed;
            if (key == "g") return kb.gKey.isPressed;
            return kb.eKey.isPressed;
        }

        static void Load()
        {
            if (_nodes != null) return;
            _nodes = new List<Node>();
            if (!PlayerPrefs.HasKey(Key)) return;
            _active = true;
            foreach (var row in PlayerPrefs.GetString(Key, "").Split(';'))
            {
                var f = row.Split(',');
                if (f.Length < 4) continue;
                if (!int.TryParse(f[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out int id)) continue;
                if (!int.TryParse(f[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out int k) || k < 0 || k > 5) continue;
                if (!float.TryParse(f[2], NumberStyles.Float, CultureInfo.InvariantCulture, out float x)) continue;
                if (!float.TryParse(f[3], NumberStyles.Float, CultureInfo.InvariantCulture, out float y)) continue;
                _nodes.Add(new Node { id = id, kind = (Kind)k, pos = new Vector2(x, y) });
            }
        }

        static string Encode()
        {
            var sb = new StringBuilder();
            for (int i = 0; i < _nodes.Count; i++)
            {
                if (sb.Length > 0) sb.Append(';');
                var n = _nodes[i];
                sb.Append(n.id).Append(',').Append((int)n.kind).Append(',')
                    .Append(n.pos.x.ToString("0.##", CultureInfo.InvariantCulture)).Append(',')
                    .Append(n.pos.y.ToString("0.##", CultureInfo.InvariantCulture));
            }
            return sb.ToString();
        }
    }

    public class HomeClearable : MonoBehaviour
    {
        public int Id;
        public HomeClear.Kind Kind;
        public float Held;
    }

    public class HomeClearDriver : MonoBehaviour
    {
        float _next;

        void Update()
        {
            if (!Application.isPlaying) return;
            HomeClear.Pump(GameplayClock.LastDelta);
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.5f;
            HomeClear.Regrow();
        }
    }
}
