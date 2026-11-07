using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameplayPrototype
{
    /// <summary>
    /// Stage 2 stone maze. Logical grid: even index = wall line (1 tile), odd index = room (Room tiles wide).
    /// Wall autotile mask N=1 E=2 S=4 W=8. Tile (0,0) is the top-left.
    /// </summary>
    public class DungeonFenceMaze : MonoBehaviour
    {
        public const int LW = 11;
        public const int LH = 9;
        public const int Room = 3;
        public const int W = 6 + 5 * Room;
        public const int H = 5 + 4 * Room;
        public static readonly Vector2 Origin = new Vector2(-(W - 1) * 0.5f, -(H - 1) * 0.5f);

        // 5x4 rooms. Entry room bottom-left (1,7); exit top-right through (9,0).
        // Dead ends: (1,1) and (3,3) hold chests, (9,3) is empty.
        static readonly string[] Rows =
        {
            "11111111101",
            "10000010001",
            "11111010111",
            "10001000101",
            "10111110101",
            "10100000101",
            "10101111101",
            "10000000001",
            "11111111111"
        };

        static readonly Vector2Int EntryRoom = new Vector2Int(1, 7);
        static readonly Vector2Int ExitOpening = new Vector2Int(9, 0);

        static DungeonFenceMaze _one;
        static bool[,] _wall;

        SpriteRenderer _gate;
        Collider2D _gateBlock;
        UnityEngine.Rendering.Universal.Light2D _gateLight;
        Sprite _gateClosed, _gateOpen;
        readonly List<Vector2Int> _dead = new List<Vector2Int>();
        readonly HashSet<Vector2Int> _seen = new HashSet<Vector2Int>();

        /// <summary>Tile that holds the exit gate.</summary>
        public static Vector2Int ExitCell => GateTile();
        /// <summary>Tiles the hero has uncovered this run (minimap fog).</summary>
        public static IReadOnlyCollection<Vector2Int> Seen => _one != null ? _one._seen : null;
        public static bool WasSeen(Vector2Int tile) => _one != null && _one._seen.Contains(tile);
        /// <summary>Centre tile of each dead-end room.</summary>
        public static IReadOnlyList<Vector2Int> DeadEnds => _one != null ? _one._dead : null;
        public static bool IsOn => _one != null && _one.gameObject.activeSelf;

        public static Vector2 HeroSpawn => CellWorld(RoomCentre(EntryRoom));
        /// <summary>Exit trigger sits just outside the gate tile.</summary>
        public static Vector2 ExitPosition => (Vector2)CellWorld(GateTile()) + new Vector2(0f, 1f);
        public static Vector2 ExitSize => new Vector2(1f, 1f);
        public static Vector2 BoundsMin => Origin - new Vector2(4f, 4f);
        public static Vector2 BoundsMax => Origin + new Vector2(W - 1, H - 1) + new Vector2(4f, 4f);

        /// <summary>Room centres at least two rooms away from the entry. Enemies spawn here.</summary>
        public static Vector2[] SpawnZones
        {
            get
            {
                var list = new List<Vector2>();
                for (int ly = 1; ly < LH; ly += 2)
                    for (int lx = 1; lx < LW; lx += 2)
                    {
                        if (Mathf.Abs(lx - EntryRoom.x) + Mathf.Abs(ly - EntryRoom.y) <= 2) continue;
                        list.Add(CellWorld(RoomCentre(new Vector2Int(lx, ly))));
                    }
                return list.ToArray();
            }
        }

        public static void Show()
        {
            if (_one == null)
            {
                var go = new GameObject("FenceMaze");
                _one = go.AddComponent<DungeonFenceMaze>();
                _one.Build();
            }
            _one.gameObject.SetActive(true);
            _one.ResetRun();
        }

        public static void Hide()
        {
            if (_one != null) _one.gameObject.SetActive(false);
        }

        public static void OpenGate()
        {
            if (_one == null || _one._gate == null) return;
            if (_one._gateOpen != null) _one._gate.sprite = _one._gateOpen;
            if (_one._gateBlock != null) _one._gateBlock.enabled = false;
            if (_one._gateLight != null) _one._gateLight.enabled = true;
        }

        public static Vector3 CellWorld(Vector2Int tile) => new Vector3(Origin.x + tile.x, Origin.y + (H - 1 - tile.y), 0f);

        public static Vector2Int TileAt(Vector2 world) =>
            new Vector2Int(Mathf.RoundToInt(world.x - Origin.x), H - 1 - Mathf.RoundToInt(world.y - Origin.y));

        public static bool IsWall(int tx, int ty)
        {
            EnsureTiles();
            if (tx < 0 || ty < 0 || tx >= W || ty >= H) return true;
            return _wall[tx, ty];
        }

        /// <summary>Exit gate is the middle tile of the exit opening; its side tiles are wall.</summary>
        static Vector2Int GateTile() => new Vector2Int(Start(ExitOpening.x) + Room / 2, 0);

        static int Span(int i) => (i & 1) == 0 ? 1 : Room;

        static int Start(int i)
        {
            int s = 0;
            for (int k = 0; k < i; k++) s += Span(k);
            return s;
        }

        static Vector2Int RoomCentre(Vector2Int room) => new Vector2Int(Start(room.x) + Span(room.x) / 2, Start(room.y) + Span(room.y) / 2);

        static void EnsureTiles()
        {
            if (_wall != null) return;
            var wall = new bool[W, H];
            for (int ly = 0; ly < LH; ly++)
                for (int lx = 0; lx < LW; lx++)
                {
                    bool on = Rows[ly][lx] == '1';
                    for (int dy = 0; dy < Span(ly); dy++)
                        for (int dx = 0; dx < Span(lx); dx++)
                            wall[Start(lx) + dx, Start(ly) + dy] = on;
                }
            for (int dx = 0; dx < Span(ExitOpening.x); dx++)
                wall[Start(ExitOpening.x) + dx, 0] = dx != Room / 2;
            _wall = wall;
        }

        void ResetRun()
        {
            _seen.Clear();
            if (_gate != null && _gateClosed != null) _gate.sprite = _gateClosed;
            if (_gateBlock != null) _gateBlock.enabled = true;
            if (_gateLight != null) _gateLight.enabled = false;
            foreach (var chest in GetComponentsInChildren<DungeonMazeChest>(true))
                chest.ResetRun();
        }

        void Build()
        {
            EnsureTiles();
            var walls = Art("Stage2", "Stage2_StoneWall_Autotile_4x4");
            var gates = Art("Stage2", "Stage2_FenceGate_2f");
            var chests = Art("Stage2", "Stage2_Chest_2f");
            var spikes = Art("Stage2", "Stage2_Spikes");
            var spike = spikes.Length > 0 ? spikes[0] : null;
            _gateClosed = gates.Length > 0 ? gates[0] : null;
            _gateOpen = gates.Length > 1 ? gates[1] : _gateClosed;
            var chestClosed = chests.Length > 0 ? chests[0] : null;
            var chestOpen = chests.Length > 1 ? chests[1] : chestClosed;

            Ground();
            Trees();

            for (int ty = 0; ty < H; ty++)
                for (int tx = 0; tx < W; tx++)
                {
                    if (!_wall[tx, ty]) continue;
                    int mask = Mask(tx, ty);
                    var cell = new GameObject("Wall");
                    cell.transform.SetParent(transform, false);
                    cell.transform.position = CellWorld(new Vector2Int(tx, ty));
                    var sr = cell.AddComponent<SpriteRenderer>();
                    sr.sprite = walls.Length > mask ? walls[mask] : null;
                    sr.color = sr.sprite == null ? new Color(0.45f, 0.42f, 0.38f) : Color.white;
                    sr.sortingOrder = 4;
                    var box = cell.AddComponent<BoxCollider2D>();
                    box.size = Vector2.one;
                }

            // Closed gate blocks the opening until the last wave is cleared.
            var gate = new GameObject("Gate");
            gate.transform.SetParent(transform, false);
            gate.transform.position = CellWorld(GateTile());
            _gate = gate.AddComponent<SpriteRenderer>();
            _gate.sprite = _gateClosed;
            _gate.sortingOrder = 5;
            var block = gate.AddComponent<BoxCollider2D>();
            block.size = Vector2.one;
            _gateBlock = block;
            _gateLight = Lamp(gate.transform, new Color(1f, 0.85f, 0.3f), 1.6f, 3.2f);
            _gateLight.enabled = false;

            // Dead ends: rooms with one opening. The first two get a chest guarded by spikes.
            int chestsLeft = 2;
            for (int ly = 1; ly < LH; ly += 2)
                for (int lx = 1; lx < LW; lx += 2)
                {
                    var room = new Vector2Int(lx, ly);
                    if (room == EntryRoom || OpenNeighbours(lx, ly) != 1) continue;
                    var centre = RoomCentre(room);
                    _dead.Add(centre);
                    if (chestsLeft <= 0) continue;
                    chestsLeft--;
                    var chestGo = new GameObject("Chest");
                    chestGo.transform.SetParent(transform, false);
                    chestGo.transform.position = CellWorld(centre);
                    var sr = chestGo.AddComponent<SpriteRenderer>();
                    sr.sprite = chestClosed;
                    sr.sortingOrder = 6;
                    chestGo.AddComponent<DungeonMazeChest>().Init(sr, chestOpen);
                    Spike(spike, CellWorld(centre) + new Vector3(-1f, 0f, 0f));
                    Spike(spike, CellWorld(centre) + new Vector3(0f, -1f, 0f));
                }

            // Spikes on two corridor bends.
            Spike(spike, CellWorld(RoomCentre(new Vector2Int(5, 5))) + new Vector3(1f, 1f, 0f));
            Spike(spike, CellWorld(RoomCentre(new Vector2Int(7, 3))) + new Vector3(1f, -1f, 0f));

            // The scene has no global 2D light and these sprites use the lit material: add a dusk fill that
            // lives (and turns off) with the maze, then warm torch pools on alternate rooms (mockup S2).
            var fill = new GameObject("DuskFill").AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            fill.transform.SetParent(transform, false);
            fill.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Global;
            fill.color = new Color(0.86f, 0.82f, 0.95f);
            fill.intensity = 0.8f;
            for (int ly = 1; ly < LH; ly += 2)
                for (int lx = 1; lx < LW; lx += 2)
                    if (((lx + ly) & 2) == 0)
                        Lamp(transform, new Color(1f, 0.62f, 0.35f), 0.9f, 3.4f).transform.position =
                            CellWorld(RoomCentre(new Vector2Int(lx, ly))) + new Vector3(0f, 1.2f, 0f);
        }

        /// <summary>Dirt floor under the maze, grass around it. Generated once, point-filtered at 16 px per tile.</summary>
        void Ground()
        {
            const int pad = 12;
            int gw = (W + pad * 2) * 16, gh = (H + pad * 2) * 16;
            var tex = new Texture2D(gw, gh, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var px = new Color32[gw * gh];
            Color32[] grass = { new Color32(0x2C, 0x4A, 0x2A, 255), new Color32(0x35, 0x58, 0x30, 255), new Color32(0x3C, 0x6E, 0x2C, 255) };
            Color32[] dirt = { new Color32(0x5A, 0x48, 0x38, 255), new Color32(0x6A, 0x56, 0x42, 255), new Color32(0x7A, 0x64, 0x4C, 255) };
            int x0 = pad * 16 - 8, y0 = pad * 16 - 8, x1 = x0 + W * 16 + 16, y1 = y0 + H * 16 + 16;
            for (int y = 0; y < gh; y++)
                for (int x = 0; x < gw; x++)
                {
                    bool inside = x >= x0 && x < x1 && y >= y0 && y < y1;
                    uint h = (uint)(x * 374761393) ^ (uint)(y * 668265263);
                    h = (h ^ (h >> 13)) * 1274126177u;
                    float n = Mathf.PerlinNoise(x * 0.045f, y * 0.045f) + (h & 255) / 255f * 0.35f;
                    int k = n < 0.55f ? 0 : n < 0.85f ? 1 : 2;
                    var c = inside ? dirt[k] : grass[k];
                    if (inside && (h & 1023) < 6) c = new Color32(0x9A, 0x86, 0x6A, 255);
                    if (!inside && (h & 511) < 5) c = new Color32(0x9C, 0xD4, 0x60, 255);
                    px[y * gw + x] = c;
                }
            tex.SetPixels32(px);
            tex.Apply(false);
            var go = new GameObject("Ground");
            go.transform.SetParent(transform, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, gw, gh), new Vector2(0.5f, 0.5f), 16f);
            sr.sortingOrder = -50;
        }

        /// <summary>Forest around the maze (mockup S2), Y-sorted like the camp trees; the ground past the exit stays open.</summary>
        void Trees()
        {
#if UNITY_EDITOR
            string[] names = { "Tree_Forest_A", "Tree_Forest_B", "Tree_Forest_C_Compact" };
            var rng = new System.Random(2);
            float R() => (float)rng.NextDouble() * 1.4f - 0.7f;
            var gate = CellWorld(GateTile());
            for (float y = Origin.y - 7f; y <= Origin.y + H + 6f; y += 2.4f)
                for (float x = Origin.x - 8f; x <= Origin.x + W + 7f; x += 2.6f)
                {
                    var p = new Vector2(x + R(), y + R());
                    bool nearMaze = p.x > Origin.x - 2.6f && p.x < Origin.x + W + 1.6f && p.y > Origin.y - 2.6f && p.y < Origin.y + H + 2.4f;
                    if (nearMaze) continue;
                    if (Mathf.Abs(p.x - gate.x) < 3.5f && p.y > gate.y) continue;
                    var sprite = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/GoblinCampPhase4/" + names[rng.Next(names.Length)] + ".png");
                    if (sprite == null) continue;
                    var go = new GameObject("Tree");
                    go.transform.SetParent(transform, false);
                    go.transform.position = p;
                    go.AddComponent<SpriteRenderer>().sprite = sprite;
                    go.AddComponent<SpriteSorter>();
                    go.AddComponent<TreeCanopyOcclusion>();
                }
#endif
        }

        static UnityEngine.Rendering.Universal.Light2D Lamp(Transform parent, Color color, float intensity, float radius)
        {
            var lamp = new GameObject("Lamp");
            lamp.transform.SetParent(parent, false);
            var light = lamp.AddComponent<UnityEngine.Rendering.Universal.Light2D>();
            light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.pointLightOuterRadius = radius;
            return light;
        }

        void Spike(Sprite sprite, Vector3 at)
        {
            var go = new GameObject("Spike");
            go.transform.SetParent(transform, false);
            go.transform.position = at;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = sprite == null ? new Color(0.55f, 0.18f, 0.16f) : Color.white;
            sr.sortingOrder = 5;
            var box = go.AddComponent<BoxCollider2D>();
            box.isTrigger = true;
            box.size = new Vector2(0.6f, 0.6f);
            go.AddComponent<DungeonSpike>();
        }

        void Update()
        {
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null) return;
            var t = TileAt(player.transform.position);
            for (int dy = -2; dy <= 2; dy++)
                for (int dx = -2; dx <= 2; dx++)
                    if (dx * dx + dy * dy <= 5)
                        _seen.Add(new Vector2Int(t.x + dx, t.y + dy));
        }

        static int Mask(int tx, int ty)
        {
            int m = 0;
            if (IsWall(tx, ty - 1)) m |= 1;
            if (IsWall(tx + 1, ty)) m |= 2;
            if (IsWall(tx, ty + 1)) m |= 4;
            if (IsWall(tx - 1, ty)) m |= 8;
            return m;
        }

        static int OpenNeighbours(int lx, int ly)
        {
            int n = 0;
            if (!LogicalWall(lx, ly - 1)) n++;
            if (!LogicalWall(lx + 1, ly)) n++;
            if (!LogicalWall(lx, ly + 1)) n++;
            if (!LogicalWall(lx - 1, ly)) n++;
            return n;
        }

        static bool LogicalWall(int lx, int ly)
        {
            if (lx < 0 || ly < 0 || lx >= LW || ly >= LH) return true;
            return Rows[ly][lx] == '1';
        }

        /// <summary>Check: rooms reached from the entry, exit opening reached, dead-end rooms.</summary>
        public static string CheckReach()
        {
            var seen = new HashSet<Vector2Int>();
            var stack = new Stack<Vector2Int>();
            stack.Push(EntryRoom);
            var dirs = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            while (stack.Count > 0)
            {
                var c = stack.Pop();
                if (!seen.Add(c)) continue;
                foreach (var d in dirs)
                {
                    var n = c + d;
                    if (!LogicalWall(n.x, n.y) && !seen.Contains(n)) stack.Push(n);
                }
            }
            int rooms = 0, reached = 0, dead = 0;
            for (int ly = 1; ly < LH; ly += 2)
                for (int lx = 1; lx < LW; lx += 2)
                {
                    rooms++;
                    if (seen.Contains(new Vector2Int(lx, ly))) reached++;
                    if (new Vector2Int(lx, ly) != EntryRoom && OpenNeighbours(lx, ly) == 1) dead++;
                }
            return "rooms " + reached + "/" + rooms + " exit=" + seen.Contains(ExitOpening) + " dead=" + dead + " tiles=" + W + "x" + H;
        }

        /// <summary>Sprites of a dungeon art sheet, sorted by frame suffix. Editor only, like the rest of the dungeon art.</summary>
        public static Sprite[] Art(string stage, string file)
        {
#if UNITY_EDITOR
            var path = "Assets/Art/Dungeon/" + stage + "/" + file + ".png";
            var list = new List<Sprite>();
            foreach (var o in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
                if (o is Sprite s) list.Add(s);
            if (list.Count == 0)
            {
                var single = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (single != null) list.Add(single);
            }
            list.Sort((a, b) => Suffix(a.name).CompareTo(Suffix(b.name)));
            return list.ToArray();
#else
            return new Sprite[0];
#endif
        }

        static int Suffix(string name)
        {
            int us = name.LastIndexOf('_');
            return us >= 0 && int.TryParse(name.Substring(us + 1), out int n) ? n : 0;
        }
    }

    public class DungeonMazeChest : MonoBehaviour
    {
        SpriteRenderer _sr;
        Sprite _open, _closed;
        bool _used;

        public bool Used => _used;

        public void Init(SpriteRenderer sr, Sprite open)
        {
            _sr = sr;
            _open = open;
            _closed = sr != null ? sr.sprite : null;
        }

        public void ResetRun()
        {
            _used = false;
            if (_sr != null && _closed != null) _sr.sprite = _closed;
        }

        void Update()
        {
            if (_used) return;
            var player = GameObject.FindGameObjectWithTag("Player");
            if (player == null || Vector2.Distance(player.transform.position, transform.position) > 1.1f) return;
            bool key = MobileTouch.ConsumeInteract();
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame) key = true;
            if (!key) return;
            _used = true;
            if (_open != null && _sr != null) _sr.sprite = _open;
            WorldSave.AddCoins(30);
            RewardBanner.Show("coin", 30);
        }
    }

    public class DungeonSpike : MonoBehaviour
    {
        float _next;

        void OnTriggerStay2D(Collider2D other)
        {
            if (Time.time < _next) return;
            if (other.GetComponentInParent<PlayerMovement>() == null) return;
            var health = other.GetComponentInParent<Health>();
            if (health == null) return;
            int chip = Mathf.Max(1, Mathf.RoundToInt(health.MaxHp * 0.05f));
            health.TakeDamage(chip, (Vector2)transform.position, Vector2.zero);
            _next = Time.time + 1f;
        }
    }
}
