using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>
    /// Home garden in the Housing zone: 4x4 plots. A crop grows on its own. Watering cuts the current
    /// stage short, then that plot refuses water until the cooldown ends. Save row: crop|waters|stageStart|nextWater.
    /// </summary>
    public static class HomeFarm
    {
        public static bool Enabled = true;
        public const float GrowSeconds = 60f;
        public const float WaterCut = 30f;
        public const float WaterCooldown = 20f;
        public const int StarterCarrots = 3;
        public const float HoldSeconds = 0.6f;

        public static readonly string[] Crops = { "carrot", "cabbage", "tomato", "pumpkin", "corn" };
        static readonly int[] Need = { 3, 3, 4, 5, 4 };
        static readonly Vector2Int[] Yield = { new Vector2Int(2, 3), new Vector2Int(1, 2), new Vector2Int(3, 4), new Vector2Int(1, 1), new Vector2Int(2, 2) };
        static readonly float[] Xs = { 3.4f, 4.4f, 5.4f, 6.4f };
        static readonly float[] Ys = { -5.6f, -4.6f, -3.6f, -2.6f };
        public const int Count = 16;

        /// <summary>Farm mode is open and owns E / Q / R / ESC.</summary>
        public static bool Active => FarmMode.Instance != null && FarmMode.Instance.On;
        /// <summary>Also true on the frame the mode closed, so the same ESC / E press is not reused.</summary>
        public static bool Captures => Active || Time.frameCount == FarmMode.ExitFrame;

        public static string Seed(string crop) => "seed_" + crop;
        public static int NeedFor(string crop) { int i = System.Array.IndexOf(Crops, crop); return i < 0 ? 1 : Need[i]; }
        public static Vector2 Centre(int i) => new Vector2(Xs[i % 4], Ys[i / 4]) + HomeLayout.GardenShift;
        static string Key(int i) => WorldCatalog.SavePrefix + "home.plot." + i;
        static string BedKey(Vector2Int c) => WorldCatalog.SavePrefix + "home.bed." + c.x + "." + c.y;
        static string StarterKey => WorldCatalog.SavePrefix + "home.farm.starter";
        static readonly List<Vector2Int> BedCells = new List<Vector2Int>();
        static float _tickAt;

        public struct Plot
        {
            public string crop;
            public int waters;
            public double wateredAt;
            public double nextWater;
            public bool Empty => string.IsNullOrEmpty(crop);
            public bool Watered => !Empty && nextWater > GameplayClock.Now;
            public bool Ripe => !Empty && waters >= NeedFor(crop);
        }

        public static Plot Read(int i) => ReadKey(Key(i));

        static Plot ReadKey(string key)
        {
            var p = new Plot { crop = "", waters = 0, wateredAt = -1d, nextWater = -1d };
            string raw = PlayerPrefs.GetString(key, "");
            if (string.IsNullOrEmpty(raw)) return p;
            var parts = raw.Split('|');
            if (parts.Length < 3 || System.Array.IndexOf(Crops, parts[0]) < 0) return p;
            p.crop = parts[0];
            int.TryParse(parts[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out p.waters);
            if (!double.TryParse(parts[2], NumberStyles.Float, CultureInfo.InvariantCulture, out p.wateredAt)) p.wateredAt = -1d;
            if (parts.Length < 4 || !double.TryParse(parts[3], NumberStyles.Float, CultureInfo.InvariantCulture, out p.nextWater)) p.nextWater = -1d;
            return p;
        }

        static string Write(Plot p) =>
            p.crop + "|" + p.waters.ToString(CultureInfo.InvariantCulture) + "|"
            + p.wateredAt.ToString("R", CultureInfo.InvariantCulture) + "|"
            + p.nextWater.ToString("R", CultureInfo.InvariantCulture);

        /// <summary>Art stage 0 seeds .. 3 ripe. Crops that need more waterings repeat the middle stages.</summary>
        public static int Stage(Plot p)
        {
            if (p.Empty || p.waters <= 0) return 0;
            int need = NeedFor(p.crop);
            if (p.waters >= need) return 3;
            return Mathf.Clamp(1 + (p.waters - 1) * 2 / Mathf.Max(1, need - 1), 1, 2);
        }

        /// <summary>Grid cell nearest to a zone-local point (always a valid plot).</summary>
        public static int CellNear(Vector2 local)
        {
            local -= HomeLayout.GardenShift;
            int col = Mathf.Clamp(Mathf.RoundToInt(local.x - Xs[0]), 0, 3);
            int row = Mathf.Clamp(Mathf.RoundToInt(local.y - Ys[0]), 0, 3);
            return row * 4 + col;
        }

        /// <summary>Plot whose 1x1 cell contains the zone-local point, or -1.</summary>
        public static int CellAt(Vector2 local)
        {
            int i = CellNear(local);
            var d = local - Centre(i);
            return Mathf.Abs(d.x) <= 0.5f && Mathf.Abs(d.y) <= 0.5f ? i : -1;
        }

        const string BedArt = "World/HomeDistrict/Farm/FD_FarmPlot";
        /// <summary>Sliced tilled bed under the 4x4 grid (FIELD_GROUND_HANDOFF §2); null falls back to FM_Soil tiles.</summary>
        public static Sprite Bed { get; private set; }

        public static void Spawn(Transform root)
        {
            if (!Enabled || root == null || root.Find("HomeFarm") != null) return;
            var host = new GameObject("HomeFarm").transform;
            host.SetParent(root, false);
            Bed = Resources.Load<Sprite>(BedArt);
            bool garden = HomeLayout.Has("garden");
            if (garden && Bed != null)
            {
                var bed = new GameObject("FarmBed");
                bed.transform.SetParent(host, false);
                bed.transform.localPosition = (Centre(0) + Centre(Count - 1)) * 0.5f;
                var sr = bed.AddComponent<SpriteRenderer>();
                sr.sprite = Bed;
                sr.drawMode = SpriteDrawMode.Sliced;
                sr.size = new Vector2(4f, 4f);
                sr.sortingOrder = -8956;
            }
            if (garden)
            {
                for (int i = 0; i < Count; i++)
                {
                    var go = new GameObject("Plot_" + i);
                    go.transform.SetParent(host, false);
                    go.transform.localPosition = Centre(i);
                    go.AddComponent<FarmPlotView>().Init(i);
                }
            }
            var entry = new GameObject("FarmEntry");
            entry.transform.SetParent(host, false);
            host.gameObject.AddComponent<FarmMode>().Bind(root, entry.AddComponent<WorldInteractable>());
            SpawnBeds(host);
            GrantStarter();
        }

        static void SpawnBeds(Transform host)
        {
            HomeLand.Collect(HomeLand.Plot, BedCells);
            for (int i = 0; i < BedCells.Count; i++)
            {
                var c = BedCells[i];
                var go = new GameObject("Bed_" + c.x + "_" + c.y);
                go.transform.SetParent(host, false);
                go.transform.localPosition = new Vector3(c.x + 0.5f, c.y + 0.5f, 0f);
                go.AddComponent<FarmBedView>().Init(c);
            }
        }

        /// <summary>One-time starter carrot seeds.</summary>
        static void GrantStarter()
        {
            if (PlayerPrefs.HasKey(StarterKey)) return;
            string seed = Seed("carrot");
            int have = WorldSave.GetQty(seed);
            int cap = WorldContent.StackLimit(seed);
            int give = cap > 0 ? Mathf.Min(StarterCarrots, Mathf.Max(0, cap - have)) : StarterCarrots;
            WorldTx.Begin()
                .SetInt(WorldSave.KeyItem(seed), have + give)
                .SetInt(StarterKey, 1)
                .After(() => { if (give > 0) WorldEvents.RaiseItemGained(seed, give); })
                .Commit();
        }

        /// <summary>Grows every garden plot and painted bed from play time, even away from the home map.</summary>
        public static void TickAll()
        {
            if (Time.unscaledTime < _tickAt) return;
            _tickAt = Time.unscaledTime + 0.4f;
            for (int i = 0; i < Count; i++) Tick(i);
            HomeLand.Collect(HomeLand.Plot, BedCells);
            for (int i = 0; i < BedCells.Count; i++) TickBed(BedCells[i]);
        }

        public static void Tick(int i) => TickKey(Key(i));

        public static void TickBed(Vector2Int c) => TickKey(BedKey(c));

        static void TickKey(string key)
        {
            var p = ReadKey(key);
            if (p.Empty || p.Ripe) return;
            int waters = p.waters;
            double start = p.wateredAt;
            if (p.wateredAt < 0d) p.wateredAt = GameplayClock.Now;
            int guard = 0;
            while (!p.Ripe && GameplayClock.Now >= p.wateredAt + GrowSeconds && guard++ < 12)
            {
                p.waters++;
                if (p.Ripe) { p.wateredAt = -1d; break; }
                p.wateredAt += GrowSeconds;
            }
            if (p.waters != waters || p.wateredAt != start)
                WorldTx.Begin().SetString(key, Write(p)).Commit();
        }

        public static bool Sow(int i, string crop) => SowKey(Key(i), crop);

        public static bool SowBed(Vector2Int c, string crop) => SowKey(BedKey(c), crop);

        static bool SowKey(string key, string crop)
        {
            var p = ReadKey(key);
            if (!p.Empty) return WorldServices.Fail(Deny.Invalid, "Plot in use");
            string seed = Seed(crop);
            int have = WorldSave.GetQty(seed);
            if (have <= 0) return WorldServices.Fail(Deny.InsufficientResources, "No seeds (Mira sells them)");
            p.crop = crop; p.waters = 0; p.wateredAt = GameplayClock.Now; p.nextWater = -1d;
            WorldTx.Begin()
                .SetInt(WorldSave.KeyItem(seed), have - 1)
                .SetString(key, Write(p))
                .Commit();
            return true;
        }

        /// <summary>Active-play seconds to ripe if every stage is watered at once; -1 when empty or ripe.</summary>
        public static float SecondsToRipe(Plot p)
        {
            if (p.Empty || p.Ripe) return -1f;
            int left = NeedFor(p.crop) - p.waters;
            if (left <= 0) return -1f;
            float stage = p.wateredAt < 0d ? GrowSeconds : Mathf.Max(0f, (float)(p.wateredAt + GrowSeconds - GameplayClock.Now));
            return stage + (left - 1) * GrowSeconds;
        }

        /// <summary>0 empty, 1 ripe; in between = waters plus the current stage clock.</summary>
        public static float GrowProgress(Plot p)
        {
            if (p.Empty) return 0f;
            if (p.Ripe) return 1f;
            float total = NeedFor(p.crop) * GrowSeconds;
            return total <= 0.01f ? 0f : Mathf.Clamp01(1f - SecondsToRipe(p) / total);
        }

        public static float WaterWait(Plot p) => p.nextWater < 0d ? 0f : Mathf.Max(0f, (float)(p.nextWater - GameplayClock.Now));

        public static bool Water(int i) => WaterKey(Key(i));

        public static bool WaterBed(Vector2Int c) => WaterKey(BedKey(c));

        static bool WaterKey(string key)
        {
            var p = ReadKey(key);
            if (p.Empty || p.Ripe) return false;
            float wait = WaterWait(p);
            if (wait > 0f) return WorldServices.Fail(Deny.NotEligible, Loc.T("Wait " + Mathf.CeilToInt(wait) + "s"));
            if (p.wateredAt < 0d) p.wateredAt = GameplayClock.Now;
            p.wateredAt -= WaterCut;
            if (GameplayClock.Now >= p.wateredAt + GrowSeconds)
            {
                p.waters++;
                p.wateredAt = p.Ripe ? -1d : GameplayClock.Now;
            }
            p.nextWater = GameplayClock.Now + WaterCooldown;
            WorldTx.Begin().SetString(key, Write(p)).Commit();
            return true;
        }

        public static bool Harvest(int i) => HarvestKey(Key(i));

        public static bool HarvestBed(Vector2Int c) => HarvestKey(BedKey(c));

        public static void DropBed(Vector2Int c)
        {
            string key = BedKey(c);
            if (!PlayerPrefs.HasKey(key)) return;
            WorldTx.Begin().Delete(key).Commit();
        }

        public static Plot ReadBed(Vector2Int c) => ReadKey(BedKey(c));

        static bool HarvestKey(string key)
        {
            var p = ReadKey(key);
            if (!p.Ripe) return false;
            int k = System.Array.IndexOf(Crops, p.crop);
            int n = Random.Range(Yield[k].x, Yield[k].y + 1);
            string item = p.crop;
            int have = WorldSave.GetQty(item);
            int cap = WorldContent.StackLimit(item);
            if (cap > 0 && have + n > cap) return WorldServices.Fail(Deny.BagFull, "Bag full");
            WorldTx.Begin()
                .SetInt(WorldSave.KeyItem(item), have + n)
                .Delete(key)
                .After(() => WorldEvents.RaiseItemGained(item, n))
                .Commit();
            return true;
        }
    }

    /// <summary>Crop overlay for one plot, plus a dark watered tint over the shared bed (or an FM_Soil tile without it).</summary>
    public class FarmPlotView : MonoBehaviour
    {
        static readonly Color WetTint = new Color32(0x3A, 0x22, 0x10, 0x66);

        public int Index { get; private set; }
        public SpriteRenderer CropRenderer => _crop;
        SpriteRenderer _soil, _crop;
        bool _bed;
        float _next;

        public void Init(int index)
        {
            Index = index;
            _bed = HomeFarm.Bed != null;
            var soil = new GameObject("Soil");
            soil.transform.SetParent(transform, false);
            _soil = soil.AddComponent<SpriteRenderer>();
            _soil.sortingOrder = -8955;
            if (_bed)
            {
                _soil.sprite = ProceduralSprites.Pixel;
                _soil.sharedMaterial = ProceduralSprites.Unlit;
                _soil.color = WetTint;
                var r = _soil.sprite.rect;
                soil.transform.localScale = new Vector3(_soil.sprite.pixelsPerUnit / r.width, _soil.sprite.pixelsPerUnit / r.height, 1f);
            }
            var crop = new GameObject("Crop");
            crop.transform.SetParent(transform, false);
            _crop = crop.AddComponent<SpriteRenderer>();
            crop.AddComponent<SpriteSorter>();
            Refresh();
        }

        void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.4f;
            Refresh();
        }

        public void Refresh()
        {
            var p = HomeFarm.Read(Index);
            if (_bed) _soil.enabled = p.Watered;
            else _soil.sprite = FarmArt.Cell(FarmArt.Soil, 0, p.Watered ? 1 : 0);
            _crop.sprite = p.Empty ? null : FarmArt.Crop(p.crop, HomeFarm.Stage(p));
        }
    }

    /// <summary>Crop sprite on one painted luống cell.</summary>
    public class FarmBedView : MonoBehaviour
    {
        Vector2Int _cell;
        SpriteRenderer _crop;
        float _next;

        public void Init(Vector2Int cell)
        {
            _cell = cell;
            var crop = new GameObject("Crop");
            crop.transform.SetParent(transform, false);
            _crop = crop.AddComponent<SpriteRenderer>();
            crop.AddComponent<SpriteSorter>();
            Refresh();
        }

        void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.4f;
            Refresh();
        }

        public void Refresh()
        {
            var p = HomeFarm.ReadBed(_cell);
            _crop.sprite = p.Empty ? null : FarmArt.Crop(p.crop, HomeFarm.Stage(p));
        }
    }

    /// <summary>
    /// "NONG TRAI [ESC]" mode: tile cursor in front of the hero (or the tapped plot), action hint, hold-to-harvest
    /// bar and the Q ◂ seeds ▸ R picker. Lives under the Housing zone, so it closes when the zone hides.
    /// </summary>
    public class FarmMode : MonoBehaviour
    {
        public static FarmMode Instance { get; private set; }
        public static int ExitFrame = -1;
        public bool On { get; private set; }
        public int Cursor { get; private set; }
        public string SelectedCrop => HomeFarm.Crops[_seed];
        /// <summary>Round-button icon for the plot under the hero. Empty when not on a plot.</summary>
        public static string InteractIcon { get; private set; }

        /// <summary>The plot prompt while the hero stands on a bed. Nearest must prefer this over the foundation.</summary>
        public bool FarmPrompt(out WorldInteractable talk)
        {
            talk = _entry;
            return _entry != null && _entry.isActiveAndEnabled && _entry.Seekable;
        }

        static readonly Color Green = new Color32(0x7E, 0xD6, 0x7E, 0xFF);
        static readonly Color Gold = new Color32(0xF2, 0xD8, 0x4A, 0xFF);
        static readonly Color Cyan = new Color32(0x8E, 0xC8, 0xF0, 0xFF);
        static readonly Color BarFill = new Color32(0xD8, 0xB0, 0x40, 0xFF);
        static readonly Color BarBack = new Color32(0x2A, 0x2A, 0x2A, 0xFF);
        const int SlotsShown = 5;

        Transform _zone;
        WorldInteractable _entry;
        FarmPlotView[] _plots;
        SpriteRenderer _tile;
        int _enterFrame, _seed, _tap = -1;
        Vector2 _tapHero;
        float _hold;
        bool _useBed;
        Vector2Int _bedCell;

        Canvas _canvas;
        Text _mode, _hint, _seedName, _time;
        RectTransform _bar, _barFill;
        UiItemSlot[] _slots;

        public void Bind(Transform zoneRoot, WorldInteractable entry)
        {
            Instance = this;
            _zone = zoneRoot;
            _entry = entry;
            _entry.DisplayName = "Gieo";
            _entry.Radius = 1f;
            _entry.OnInteract = TouchFarm;
            _entry.enabled = false;
            _plots = GetComponentsInChildren<FarmPlotView>(true);
            var tile = new GameObject("FarmCursor");
            tile.transform.SetParent(transform, false);
            _tile = tile.AddComponent<SpriteRenderer>();
            _tile.sprite = FarmArt.Cell(FarmArt.UiTiles, 0, 0);
            _tile.enabled = false;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (_canvas != null) Destroy(_canvas.gameObject);
            if (_nextWater != null) Destroy(_nextWater.gameObject);
            if (_ripeIn != null) Destroy(_ripeIn.gameObject);
            if (_growBar != null) Destroy(_growBar.gameObject);
        }

        void OnDisable() { if (On) Exit(); }

        /// <summary>Same round button as petting the dog: one tap sows, waters or harvests.</summary>
        void TouchFarm()
        {
            var hero = Hero();
            if (hero == null || _zone == null) return;
            Vector2 local = _zone.InverseTransformPoint(hero.position);
            _useBed = NearPlot(hero, local, out _bedCell);
            if (!_useBed && !HomeAnimals.InGarden(local, 0.2f)) return;
            Cursor = _useBed ? HomeFarm.CellNear(local) : CellInFront(hero, local);
            int first = System.Array.FindIndex(HomeFarm.Crops, c => WorldSave.GetQty(HomeFarm.Seed(c)) > 0);
            if (first >= 0 && WorldSave.GetQty(HomeFarm.Seed(SelectedCrop)) <= 0) _seed = first;
            DoAction();
        }

        public void Enter()
        {
            if (On) return;
            On = true;
            _enterFrame = Time.frameCount;
            _hold = 0f;
            _tap = -1;
            _entry.enabled = false;
            int first = System.Array.FindIndex(HomeFarm.Crops, c => WorldSave.GetQty(HomeFarm.Seed(c)) > 0);
            if (first >= 0 && WorldSave.GetQty(HomeFarm.Seed(SelectedCrop)) <= 0) _seed = first;
            EnsureUi();
            _canvas.gameObject.SetActive(true);
            WorldNavigator.Instance?.SetPromptVisible(false);
        }

        public void Exit()
        {
            if (!On) return;
            On = false;
            InteractIcon = null;
            ExitFrame = Time.frameCount;
            _tile.enabled = false;
            HidePlotOverlay();
            if (_canvas != null) _canvas.gameObject.SetActive(false);
            WorldNavigator.Instance?.SetPromptVisible(true);
        }

        static Transform Hero()
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            return go != null ? go.transform : null;
        }

        static KeyControl InteractKey(Keyboard kb)
        {
            string key = WorldSave.InteractBinding;
            if (key == "f") return kb.fKey;
            if (key == "g") return kb.gKey;
            return kb.eKey;
        }

        static string InteractName()
        {
            string key = WorldSave.InteractBinding;
            return string.IsNullOrEmpty(key) ? "E" : key.ToUpperInvariant();
        }

        void Update()
        {
            var hero = Hero();
            if (hero == null || _zone == null) return;
            Vector2 local = _zone.InverseTransformPoint(hero.position);
            if (!On)
            {
                bool inside = (HomeAnimals.InGarden(local, 0.2f) || NearPlot(hero, local, out _)) && !BuildMode.IsOpen;
                if (_entry.enabled != inside) _entry.enabled = inside;
                if (inside)
                {
                    _entry.transform.position = hero.position;
                    _useBed = NearPlot(hero, local, out _bedCell);
                    Cursor = _useBed ? HomeFarm.CellNear(local) : CellInFront(hero, local);
                    var plot = _useBed ? HomeFarm.ReadBed(_bedCell) : HomeFarm.Read(Cursor);
                    _entry.DisplayName = plot.Empty ? SowName() : Loc.T(ActionWord(plot));
                    InteractIcon = IconFor(plot);
                    ShowSeedBar(plot.Empty);
                }
                else
                {
                    InteractIcon = null;
                    ShowSeedBar(false);
                }
                return;
            }

            var kb = PlayerChat.Typing ? null : Keyboard.current; // chat line open: game keys off
            bool fresh = Time.frameCount == _enterFrame;
            _useBed = NearPlot(hero, local, out _bedCell);
            if ((!HomeAnimals.InGarden(local, 1.2f) && !_useBed) || (!fresh && kb != null && kb.escapeKey.wasPressedThisFrame))
            {
                Exit();
                return;
            }
            if (!fresh && kb != null)
            {
                if (kb.qKey.wasPressedThisFrame) Cycle(-1);
                if (kb.rKey.wasPressedThisFrame) Cycle(1);
            }
            ReadTap(local);
            if (_tap >= 0 && (local - _tapHero).sqrMagnitude > 0.09f) _tap = -1;
            Cursor = _tap >= 0 ? _tap : CellInFront(hero, local);

            var p = Focus();
            InteractIcon = IconFor(p);
            bool touchUse = !fresh && MobileTouch.ConsumeInteract();
            if (p.Ripe)
            {
                bool held = !fresh && ((kb != null && InteractKey(kb).isPressed) || MobileTouch.InteractHeld);
                _hold = held ? _hold + Time.deltaTime : 0f;
                if (_hold >= HomeFarm.HoldSeconds) { _hold = 0f; DoAction(); }
            }
            else
            {
                _hold = 0f;
                if ((!fresh && kb != null && InteractKey(kb).wasPressedThisFrame) || touchUse) DoAction();
            }
        }

        HomeFarm.Plot Focus() => _useBed ? HomeFarm.ReadBed(_bedCell) : HomeFarm.Read(Cursor);

        bool NearPlot(Transform hero, Vector2 local, out Vector2Int cell)
        {
            cell = HomeLand.CellOf(local);
            if (HomeLand.Kind(cell) == HomeLand.Plot) return true;
            var combat = hero.GetComponent<PlayerCombat>();
            Vector2 face = combat != null && combat.Facing.sqrMagnitude > 0.01f ? combat.Facing.normalized : Vector2.down;
            cell = HomeLand.CellOf(local + face * 0.75f);
            return HomeLand.Kind(cell) == HomeLand.Plot;
        }

        int CellInFront(Transform hero, Vector2 local)
        {
            var combat = hero.GetComponent<PlayerCombat>();
            Vector2 facing = combat != null && combat.Facing.sqrMagnitude > 0.01f ? combat.Facing.normalized : Vector2.down;
            return HomeFarm.CellNear(local + facing * 0.7f);
        }

        void ReadTap(Vector2 heroLocal)
        {
            var pointer = Pointer.current;
            var cam = Camera.main;
            if (pointer == null || cam == null || !pointer.press.wasPressedThisFrame) return;
            var es = UnityEngine.EventSystems.EventSystem.current;
            if (es != null && es.IsPointerOverGameObject()) return;
            Vector2 world = cam.ScreenToWorldPoint(pointer.position.ReadValue());
            int cell = HomeFarm.CellAt(_zone.InverseTransformPoint(world));
            if (cell < 0) return;
            _tap = cell;
            _tapHero = heroLocal;
        }

        void Cycle(int dir)
        {
            int n = HomeFarm.Crops.Length;
            _seed = (_seed + dir + n) % n;
        }

        /// <summary>Plant, water or harvest the plot under the cursor.</summary>
        public void DoAction()
        {
            var p = HomeFarm.Read(Cursor);
            if (_useBed)
            {
                var bed = HomeFarm.ReadBed(_bedCell);
                if (bed.Empty) HomeFarm.SowBed(_bedCell, SelectedCrop);
                else if (bed.Ripe) HomeFarm.HarvestBed(_bedCell);
                else HomeFarm.WaterBed(_bedCell);
            }
            else if (p.Empty) HomeFarm.Sow(Cursor, SelectedCrop);
            else if (p.Ripe) HomeFarm.Harvest(Cursor);
            else HomeFarm.Water(Cursor);
            if (_plots != null)
                foreach (var v in _plots) if (v.Index == Cursor) v.Refresh();
        }

        static string IconFor(HomeFarm.Plot p)
        {
            if (p.Empty) return "@IC_Sow";
            if (p.Ripe) return "@IC_Harvest";
            return "@IC_Water";
        }

        static string ActionWord(HomeFarm.Plot p)
        {
            if (p.Empty) return "Sow";
            if (p.Ripe) return "Harvest";
            float wait = HomeFarm.WaterWait(p);
            return wait > 0f ? "Wait " + Mathf.CeilToInt(wait) + "s" : "Water crop";
        }

        Text _nextWater, _ripeIn;
        RectTransform _growBar, _growFill;

        void LateUpdate()
        {
            bool show = On || (_entry != null && _entry.enabled);
            AimTile(show);
            ShowPlotTimes(show);
            if (On) RenderUi(null);
        }

        /// <summary>Ripe clock, grow bar and water cooldown on the aimed plot (mockup mục 1).</summary>
        void ShowPlotTimes(bool show)
        {
            bool aimed = show && _tile != null && _tile.enabled;
            var p = aimed
                ? (_useBed ? HomeFarm.ReadBed(_bedCell) : HomeFarm.Read(Cursor))
                : default;
            float wait = aimed && !p.Empty && !p.Ripe ? HomeFarm.WaterWait(p) : 0f;
            float ripe = aimed ? HomeFarm.SecondsToRipe(p) : -1f;
            bool waterOn = wait > 0.05f;
            bool ripeOn = ripe >= 0f;
            if (!waterOn && !ripeOn)
            {
                HidePlotOverlay();
                return;
            }
            Vector2 screen = WorldLabelsCanvas.WorldToSnappedScreen(_tile.transform.position + new Vector3(0f, 0.42f, 0f));
            if (screen.x < -100f)
            {
                HidePlotOverlay();
                return;
            }
            if (ripeOn)
            {
                int s = Mathf.CeilToInt(ripe);
                string clock = (s / 60) + ":" + (s % 60).ToString("00");
                string t = Loc.T("Ripe in").ToUpperInvariant() + " " + clock;
                PlaceGrowBar(screen, HomeFarm.GrowProgress(p));
                PlaceClock(ref _ripeIn, "FarmRipeIn", Cyan, t, screen + new Vector2(0f, waterOn ? 22f : 10f));
            }
            else
            {
                if (_ripeIn != null) _ripeIn.gameObject.SetActive(false);
                HideGrowBar();
            }
            if (waterOn)
                PlaceClock(ref _nextWater, "FarmNextWater", Cyan, Mathf.CeilToInt(wait) + "s", screen + new Vector2(0f, ripeOn ? 10f : 0f));
            else if (_nextWater != null)
                _nextWater.gameObject.SetActive(false);
        }

        void HidePlotOverlay()
        {
            if (_nextWater != null) _nextWater.gameObject.SetActive(false);
            if (_ripeIn != null) _ripeIn.gameObject.SetActive(false);
            HideGrowBar();
        }

        void HideGrowBar()
        {
            if (_growBar != null) _growBar.gameObject.SetActive(false);
        }

        void PlaceGrowBar(Vector2 screen, float progress)
        {
            if (_growBar == null)
            {
                var canvas = WorldLabelsCanvas.Ensure();
                var go = new GameObject("FarmGrowBar");
                go.transform.SetParent(canvas.Root, false);
                _growBar = go.AddComponent<RectTransform>();
                _growBar.pivot = new Vector2(0.5f, 0f);
                _growBar.sizeDelta = new Vector2(48f, 6f);
                var back = go.AddComponent<Image>();
                back.color = BarBack;
                back.raycastTarget = false;
                var fillGo = new GameObject("Fill");
                fillGo.transform.SetParent(_growBar, false);
                _growFill = fillGo.AddComponent<RectTransform>();
                _growFill.anchorMin = Vector2.zero;
                _growFill.anchorMax = new Vector2(0f, 1f);
                _growFill.pivot = new Vector2(0f, 0.5f);
                _growFill.offsetMin = _growFill.offsetMax = Vector2.zero;
                var fill = fillGo.AddComponent<Image>();
                fill.color = Green;
                fill.raycastTarget = false;
            }
            if (!_growBar.gameObject.activeSelf) _growBar.gameObject.SetActive(true);
            _growFill.anchorMax = new Vector2(Mathf.Clamp01(progress), 1f);
            _growBar.position = screen;
        }

        void PlaceClock(ref Text label, string name, Color color, string text, Vector2 screen)
        {
            if (label == null)
            {
                var canvas = WorldLabelsCanvas.Ensure();
                var go = new GameObject(name);
                go.transform.SetParent(canvas.Root, false);
                var rt = go.AddComponent<RectTransform>();
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(120f, 16f);
                label = go.AddComponent<Text>();
                label.font = canvas.PixelFont;
                label.fontSize = UiKit.Fs(10);
                label.alignment = TextAnchor.MiddleCenter;
                label.color = color;
                label.raycastTarget = false;
                label.horizontalOverflow = HorizontalWrapMode.Overflow;
                WorldLabelsCanvas.ApplyPixelOutline(label, Color.black);
            }
            if (label.text != text) label.text = text;
            if (label.color != color) label.color = color;
            if (!label.gameObject.activeSelf) label.gameObject.SetActive(true);
            label.rectTransform.sizeDelta = new Vector2(120f, 16f);
            label.rectTransform.position = screen;
        }

        void AimTile(bool show)
        {
            if (_tile == null) return;
            if (!show)
            {
                _tile.enabled = false;
                return;
            }
            if (_useBed)
            {
                _tile.enabled = true;
                _tile.transform.position = _zone.TransformPoint(new Vector3(_bedCell.x + 0.5f, _bedCell.y + 0.5f, 0f));
                _tile.sortingOrder = 20;
                return;
            }
            var view = _plots != null ? System.Array.Find(_plots, v => v.Index == Cursor) : null;
            _tile.enabled = view != null;
            if (view == null) return;
            _tile.transform.position = view.transform.position;
            _tile.sortingOrder = (view.CropRenderer != null ? view.CropRenderer.sortingOrder : -8954) + 1;
        }

        // ---------- UI ----------

        void EnsureUi()
        {
            if (_canvas != null) return;
            // Above the prompt plate and HUD rows, under the reward banner (230) and service windows (240+).
            _canvas = UiKit.Canvas("FarmMode", 225);
            var root = _canvas.transform;

            _mode = UiKit.Label(root, "ModeLabel", UiKit.IsMobile ? "NONG TRAI" : "NONG TRAI [ESC]", 8, Green, TextAnchor.MiddleCenter);
            UiKit.Place(_mode, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(160f, 10f))
                .Portrait(UiLayout.Spec.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -66f), new Vector2(160f, 10f)));
            _mode.raycastTarget = true;
            var exit = _mode.gameObject.AddComponent<Button>();
            exit.transition = Selectable.Transition.None;
            exit.onClick.AddListener(Exit);
            bool touch = UiKit.IsMobile;
            if (touch)
            {
                // Touch: a real DONG button (child of the label, so it shows and hides with farm mode).
                _mode.text = "";
                var close = UiKit.Button(_mode.transform, "ExitButton", "Secondary", "DONG", new Vector2(56f, 22f), Exit);
                UiKit.Place(close, UiKit.Center, UiKit.Center, Vector2.zero, new Vector2(56f, 22f));
                UiKit.Reach(close, 60f, 36f);
            }

            _hint = UiKit.Label(root, "ActionHint", "", 8, Gold, TextAnchor.MiddleCenter);
            _hint.horizontalOverflow = HorizontalWrapMode.Overflow;
            _hint.raycastTarget = true;
            var act = _hint.gameObject.AddComponent<Button>();
            act.transition = Selectable.Transition.None;
            act.onClick.AddListener(DoAction);
            FollowAnchor(_hint.rectTransform, new Vector2(120f, 10f));

            _time = UiKit.Label(root, "HarvestTime", "", 8, Cyan, TextAnchor.MiddleCenter);
            _time.horizontalOverflow = HorizontalWrapMode.Overflow;
            FollowAnchor(_time.rectTransform, new Vector2(60f, 10f));
            _time.gameObject.SetActive(false);

            _bar = UiKit.Rect(root, "HarvestBar");
            var back = _bar.gameObject.AddComponent<Image>();
            back.color = BarBack;
            back.raycastTarget = false;
            FollowAnchor(_bar, new Vector2(24f, 3f));
            _barFill = UiKit.Rect(_bar, "Fill");
            var fill = _barFill.gameObject.AddComponent<Image>();
            fill.color = BarFill;
            fill.raycastTarget = false;
            _barFill.anchorMin = Vector2.zero;
            _barFill.anchorMax = new Vector2(0f, 1f);
            _barFill.pivot = new Vector2(0f, 0.5f);
            _barFill.offsetMin = _barFill.offsetMax = Vector2.zero;

            // Seed picker, bottom centre where the hint plate normally sits.
            _picker = UiKit.Rect(root, "SeedPicker");
            // Touch: 30 px seed slots and a 96x26 action button (thumb size); PC keeps the compact bar.
            float slotSize = touch ? 30f : 22f, slotStep = touch ? 32f : 24f;
            var pickerSize = touch ? new Vector2(240f, 44f) : new Vector2(200f, 36f);
            UiKit.Place(_picker, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 52f), pickerSize)
                .Portrait(UiLayout.Spec.At(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(200f, 36f)));
            _seedName = UiKit.Label(_picker, "SeedName", "", 8, Cyan, TextAnchor.MiddleCenter);
            UiKit.Place(_seedName, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(pickerSize.x, 10f));
            KeyButton(_picker, "Prev", touch ? "<" : "Q", touch ? -102f : -88f, () => Cycle(-1));
            KeyButton(_picker, "Next", touch ? ">" : "R", touch ? 102f : 88f, () => Cycle(1));
            _slots = new UiItemSlot[SlotsShown];
            for (int i = 0; i < SlotsShown; i++)
            {
                int slot = i;
                _slots[i] = UiKit.Slot(_picker, "Seed" + i, () => PickSlot(slot));
                UiKit.Place(_slots[i], new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2((i - 2) * slotStep, 0f), new Vector2(slotSize, slotSize));
            }
            var actSize = touch ? new Vector2(96f, 26f) : new Vector2(88f, 18f);
            _actBtn = UiKit.Button(root, "FarmAct", "Primary", "GIEO", actSize, DoAction);
            UiKit.Place(_actBtn, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, touch ? 100f : 82f), actSize);
            UiKit.Reach(_actBtn, 96f, 36f);
            _actLabel = _actBtn.GetComponentInChildren<Text>();
            _actBtn.gameObject.SetActive(UiKit.IsMobile);
            _canvas.gameObject.SetActive(false);
        }

        Button _actBtn;
        Text _actLabel;
        RectTransform _picker;

        void ShowSeedBar(bool show)
        {
            if (!show)
            {
                if (!On && _canvas != null) _canvas.gameObject.SetActive(false);
                return;
            }
            EnsureUi();
            _canvas.gameObject.SetActive(true);
            _picker.gameObject.SetActive(true);
            if (!On)
            {
                _mode.gameObject.SetActive(false);
                _hint.gameObject.SetActive(false);
                _bar.gameObject.SetActive(false);
                if (_actBtn != null) _actBtn.gameObject.SetActive(false);
            }
            RefreshSeeds();
        }

        static string SowName()
        {
            var mode = Instance;
            string crop = mode != null ? mode.SelectedCrop : HomeFarm.Crops[0];
            var seed = WorldContent.FindItem(HomeFarm.Seed(crop));
            string name = seed.displayName;
            return Loc.T("Sow") + " " + Loc.T(string.IsNullOrEmpty(name) ? crop : name);
        }

        void RefreshSeeds()
        {
            if (_slots == null) return;
            int first = FirstShown();
            for (int i = 0; i < SlotsShown; i++)
            {
                string crop = HomeFarm.Crops[first + i];
                int qty = WorldSave.GetQty(HomeFarm.Seed(crop));
                _slots[i].Set(UiKit.ItemIcon(HomeFarm.Seed(crop)), qty, first + i == _seed ? "Selected" : "");
                _slots[i].qty.text = qty.ToString();
            }
            var seed = WorldContent.FindItem(HomeFarm.Seed(SelectedCrop));
            string name = Loc.T(string.IsNullOrEmpty(seed.displayName) ? SelectedCrop : seed.displayName).ToUpperInvariant()
                + " x" + WorldSave.GetQty(HomeFarm.Seed(SelectedCrop));
            if (_seedName.text != name) _seedName.text = name;
        }

        void KeyButton(Transform parent, string name, string label, float x, UnityEngine.Events.UnityAction click)
        {
            if (!UiKit.IsMobile)
            {
                var cap = UiKit.Panel(parent, name, "UI_KeyCap");
                UiKit.Place(cap, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x, 8f), new Vector2(14f, 14f));
                var key = UiKit.Label(cap.transform, "K", label, 8, Pal.GoldL, TextAnchor.MiddleCenter, false);
                UiKit.Fill(key);
                var keyBtn = cap.gameObject.AddComponent<Button>();
                keyBtn.targetGraphic = cap;
                keyBtn.transition = Selectable.Transition.None;
                keyBtn.onClick.AddListener(click);
                return;
            }
            var t = UiKit.Label(parent, name, label, 8, Gold, TextAnchor.MiddleCenter);
            UiKit.Place(t, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(x, 6f), new Vector2(12f, 12f));
            t.raycastTarget = true;
            UiKit.Reach(t, 30f, 36f);
            var b = t.gameObject.AddComponent<Button>();
            b.transition = Selectable.Transition.None;
            b.onClick.AddListener(click);
        }

        static void FollowAnchor(RectTransform rt, Vector2 sizeKit)
        {
            UiKit.Place(rt, Vector2.zero, new Vector2(0.5f, 0f), Vector2.zero, sizeKit);
        }

        int FirstShown() => Mathf.Clamp(_seed - (SlotsShown - 1), 0, HomeFarm.Crops.Length - SlotsShown);

        void PickSlot(int slot) => _seed = Mathf.Clamp(FirstShown() + slot, 0, HomeFarm.Crops.Length - 1);

        void RenderUi(FarmPlotView view)
        {
            if (_canvas == null) return;
            var p = Focus();
            string word = ActionWord(p);
            string action = string.IsNullOrEmpty(word) ? "" : Loc.T(word).ToUpperInvariant();
            string text = UiKit.IsMobile || action.Length == 0 ? action : action + " " + InteractName();
            if (_hint.text != text) _hint.text = text;
            if (_actLabel != null && _actLabel.text != action && action.Length > 0) _actLabel.text = action;
            bool showBar = p.Ripe && _hold > 0f;
            if (_bar.gameObject.activeSelf != showBar) _bar.gameObject.SetActive(showBar);
            if (showBar) _barFill.anchorMax = new Vector2(Mathf.Clamp01(_hold / HomeFarm.HoldSeconds), 1f);
            if (_time.gameObject.activeSelf) _time.gameObject.SetActive(false);
            Vector3 anchor = _useBed ? _tile.transform.position : view != null ? view.transform.position : Vector3.zero;
            if (_useBed || view != null)
            {
                Follow(_hint.rectTransform, anchor, 14f);
                Follow(_bar, anchor, 9f);
            }

            RefreshSeeds();
        }

        /// <summary>Pins a canvas element above a world point on whole canvas units.</summary>
        void Follow(RectTransform rt, Vector3 world, float upKit)
        {
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 sp = cam.WorldToScreenPoint(world);
            float s = Mathf.Max(0.0001f, _canvas.scaleFactor);
            rt.anchoredPosition = new Vector2(Mathf.Round(sp.x / s), Mathf.Round(sp.y / s + upKit * UiKit.Px));
        }
    }
}
