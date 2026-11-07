using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>
    /// In-stage HUD for dungeon runs (handoff spec section 2/3, mockups S1..S5), kit px on a 640×360 canvas:
    /// minimap (2 px per tile) with stage ribbon and clock, maze fog + compass, and one stage-specific widget.
    /// Lives only while DungeonRun is active; LevelFlow calls Begin/End.
    /// </summary>
    public class DungeonHud : MonoBehaviour
    {
        const int MapPx = 68;
        const int PxPerTile = 2;

        static DungeonHud _one;

        Canvas _canvas;
        RawImage _map;
        Texture2D _tex;
        Color32[] _pix;
        RectTransform _mapArea;
        Image _hero, _compass;
        Text _ribbon, _time, _mark;
        readonly List<Image> _marks = new List<Image>();
        Image _gateEdge;
        RectTransform _bridgeBar;
        Image[] _pips;
        Text _bridgeText;
        readonly List<Image> _warn = new List<Image>();

        LevelConfig _cfg;
        int _stage;
        bool[,] _blocked;
        Vector2 _gridMin;
        float _repaint;
        int _lastWave = -1;
        float _warnUntil;

        Transform _decor;
        SpriteRenderer _gate;
        Vector3 _gateScale = Vector3.one;
        readonly List<SpriteRenderer> _arrows = new List<SpriteRenderer>();
        SpriteRenderer _encircle, _ritual, _telegraph;
        float _telegraphT = -1f;
        bool _summon66, _summon33;

        static Sprite _dirtCentre;
        static Sprite[] _arrowArt, _warnArt, _ringArt, _teleArt, _pipArt, _gateArt;

        /// <summary>Stage 4 shows the bridge bar and stage 5's boss wave shows the boss bar instead of the objective strip.</summary>
        public static bool ReplacesObjective
        {
            get
            {
                if (_one == null || !_one.isActiveAndEnabled || !DungeonRun.Active) return false;
                var flow = LevelFlow.Instance;
                if (_one._stage == 4) return true;
                return _one._stage == 5 && flow != null && flow.WaveCount > 0 && flow.WaveNumber >= flow.WaveCount;
            }
        }

        public static void Begin(LevelConfig cfg)
        {
            if (cfg == null) return;
            if (_one == null)
            {
                var canvas = UiKit.Canvas("DungeonHud", 205);
                _one = canvas.gameObject.AddComponent<DungeonHud>();
                _one._canvas = canvas;
                _one.Build(canvas.transform);
            }
            _one.gameObject.SetActive(true);
            _one.Setup(cfg);
        }

        public static void End()
        {
            if (_one == null) return;
            _one.ClearDecor();
            _one.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (_one == this) _one = null;
            ClearDecor();
        }

        // ---------------------------------------------------------------- build

        void Build(Transform root)
        {
            UiKit.Ensure();
            // Minimap frame (553,38) 80×80, draw area 68×68 at offset (6,6).
            var frame = UiKit.Panel(root, "MiniFrame", "UI_Minimap_Frame");
            UiKit.Place(frame, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-7f, -38f), new Vector2(80f, 80f));
            _mapArea = UiKit.Rect(frame.transform, "Area");
            UiKit.Place(_mapArea, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(MapPx, MapPx));
            _mapArea.gameObject.AddComponent<RectMask2D>();
            _map = new GameObject("Terrain", typeof(RectTransform)).AddComponent<RawImage>();
            _map.transform.SetParent(_mapArea, false);
            _map.raycastTarget = false;
            UiKit.Fill(_map);
            _tex = new Texture2D(MapPx, MapPx, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            _pix = new Color32[MapPx * MapPx];
            _map.texture = _tex;
            _hero = Marker(_mapArea, "UI_Minimap_Marker_Player", 7f);
            _compass = UiKit.Icon(frame.transform, "", new Vector2(0f, 1f), new Vector2(10f, -10f), 14f);
            _compass.name = "Compass";

            // Stage name ribbon (547,118) 92×18, cyan text.
            var ribbon = UiKit.Panel(root, "StageRibbon", "UI_Header_Ribbon");
            UiKit.Place(ribbon, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-1f, -118f), new Vector2(92f, 18f));
            _ribbon = UiKit.Label(ribbon.transform, "Name", "", 8, new Color32(0x5E, 0xD0, 0xE0, 0xFF), TextAnchor.MiddleCenter);
            UiKit.Fill(_ribbon).Both(UiLayout.Spec.Stretch(Vector2.zero, Vector2.one, UiKit.Center, new Vector2(0f, 2f), Vector2.zero));

            // Clock plate (555,137) 76×15: time left, star at x+36, 3-star mark right in gold.
            var plate = UiKit.Panel(root, "Clock", "UI_NamePlate");
            UiKit.Place(plate, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-9f, -137f), new Vector2(76f, 15f));
            _time = UiKit.Label(plate.transform, "Time", "00:00", 8, Pal.TextW, TextAnchor.MiddleLeft);
            UiKit.Place(_time, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(5f, 0f), new Vector2(32f, 13f));
            UiKit.Icon(plate.transform, "UI_Star_Large_Filled", new Vector2(0f, 0.5f), new Vector2(42f, 0f), 12f);
            _mark = UiKit.Label(plate.transform, "Mark", "", 8, new Color32(0xF2, 0xD8, 0x4A, 0xFF), TextAnchor.MiddleRight);
            UiKit.Place(_mark, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-4f, 0f), new Vector2(28f, 13f));

            // Stage 1: gate icon clamped to the screen edge while the exit is off camera.
            _gateEdge = UiKit.Icon(root, "", new Vector2(0.5f, 0.5f), Vector2.zero, 16f);
            _gateEdge.name = "GateEdge";
            _gateEdge.sprite = One("Stage1", "Stage1_GateEdgeIcon");
            _gateEdge.enabled = _gateEdge.sprite != null;
            _gateEdge.gameObject.SetActive(false);

            // Stage 3: up to four surround warnings on the screen edges.
            for (int i = 0; i < 4; i++)
            {
                var w = UiKit.Icon(root, "", new Vector2(0.5f, 0.5f), Vector2.zero, 16f);
                w.name = "Warn" + i;
                w.gameObject.SetActive(false);
                _warn.Add(w);
            }

            // Stage 4: bridge progress bar at (240,27) 160×28 (frame 80×14 ×2, pips ×2), replaces the objective strip.
            var bar = UiKit.Icon(root, "", new Vector2(0.5f, 1f), new Vector2(0f, -41f), 160f);
            bar.name = "BridgeBar";
            bar.sprite = One("Stage4", "Stage4_BridgeBar_Frame");
            bar.enabled = bar.sprite != null;
            UiKit.Place(bar, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -41f), new Vector2(160f, 28f));
            _bridgeBar = bar.rectTransform;
            _pips = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var pip = UiKit.Icon(bar.transform, "", new Vector2(0f, 1f), Vector2.zero, 16f);
                pip.name = "Pip" + i;
                pip.preserveAspect = false;
                // Frame slots at x = 18/38/58, y = 3 (frame px), 18×8, doubled.
                UiKit.Place(pip, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2((18f + 20f * i) * 2f, -6f), new Vector2(36f, 16f));
                _pips[i] = pip;
            }
            _bridgeText = UiKit.Label(root, "BridgeText", "", 8, Pal.TextW, TextAnchor.MiddleLeft);
            UiKit.Place(_bridgeText, new Vector2(0.5f, 1f), new Vector2(0f, 0.5f), new Vector2(84f, -41f), new Vector2(70f, 14f));

            // Stage 4: red screen border while the hero stands at the bridge edge (Sliced, no centre).
            _edgeDanger = new GameObject("EdgeDanger", typeof(RectTransform)).AddComponent<Image>();
            _edgeDanger.transform.SetParent(root, false);
            _edgeDanger.transform.SetAsFirstSibling();
            _edgeDanger.raycastTarget = false;
            _edgeDanger.sprite = One("Stage4", "Stage4_EdgeDanger_9s");
            _edgeDanger.type = Image.Type.Sliced;
            _edgeDanger.fillCenter = false;
            // 16 px border at PPU 16 on a 100-unit canvas → 3 canvas px per art px.
            _edgeDanger.pixelsPerUnitMultiplier = 100f / 16f / UiKit.Px;
            var ert = _edgeDanger.rectTransform;
            ert.anchorMin = Vector2.zero;
            ert.anchorMax = Vector2.one;
            ert.offsetMin = ert.offsetMax = Vector2.zero;
            _edgeDanger.color = new Color(1f, 1f, 1f, 0f);
            _edgeDanger.enabled = _edgeDanger.sprite != null;
        }

        Image _edgeDanger;

        static Image Marker(Transform parent, string sprite, float size)
        {
            var img = new GameObject(sprite, typeof(RectTransform)).AddComponent<Image>();
            img.transform.SetParent(parent, false);
            img.raycastTarget = false;
            img.sprite = UiKit.Get(sprite);
            img.preserveAspect = true;
            var rt = img.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(size, size) * UiKit.Px;
            return img;
        }

        // ---------------------------------------------------------------- stage setup

        void Setup(LevelConfig cfg)
        {
            _cfg = cfg;
            _stage = DungeonRun.Stage;
            _lastWave = -1;
            _warnUntil = 0f;
            _summon66 = _summon33 = false;
            _telegraphT = -1f;
            var stage = DungeonCatalog.StageAt(DungeonRun.Dungeon, _stage);
            _ribbon.text = stage.name.ToUpperInvariant();
            _mark.text = Clock(stage.threeStarSeconds);
            _compass.gameObject.SetActive(cfg.mapLayoutId == "FenceMaze");
            if (_compass.gameObject.activeSelf)
            {
                _compass.sprite = One("Stage2", "Stage2_ExitCompass");
                _compass.enabled = _compass.sprite != null;
            }
            _bridgeBar.gameObject.SetActive(_stage == 4);
            _bridgeText.gameObject.SetActive(_stage == 4);
            SampleTerrain(cfg);
            ClearDecor();
            BuildDecor(cfg);
        }

        /// <summary>One cell per world unit over the camera bounds: blocked where a solid collider sits.</summary>
        void SampleTerrain(LevelConfig cfg)
        {
            _gridMin = new Vector2(Mathf.Floor(cfg.cameraBoundsMin.x), Mathf.Floor(cfg.cameraBoundsMin.y));
            int w = Mathf.Clamp(Mathf.CeilToInt(cfg.cameraBoundsMax.x - _gridMin.x) + 1, 1, 256);
            int h = Mathf.Clamp(Mathf.CeilToInt(cfg.cameraBoundsMax.y - _gridMin.y) + 1, 1, 256);
            _blocked = new bool[w, h];
            if (cfg.mapLayoutId == "FenceMaze") return;
            Physics2D.SyncTransforms();
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    var hits = Physics2D.OverlapPointAll(_gridMin + new Vector2(x, y));
                    for (int i = 0; i < hits.Length; i++)
                    {
                        var c = hits[i];
                        if (c.isTrigger || c.attachedRigidbody != null && c.attachedRigidbody.bodyType == RigidbodyType2D.Dynamic) continue;
                        _blocked[x, y] = true;
                        break;
                    }
                }
        }

        bool Blocked(Vector2 world)
        {
            if (_cfg != null && _cfg.mapLayoutId == "FenceMaze")
            {
                var t = DungeonFenceMaze.TileAt(world);
                return DungeonFenceMaze.IsWall(t.x, t.y);
            }
            if (_blocked == null) return false;
            int x = Mathf.RoundToInt(world.x - _gridMin.x), y = Mathf.RoundToInt(world.y - _gridMin.y);
            if (x < 0 || y < 0 || x >= _blocked.GetLength(0) || y >= _blocked.GetLength(1)) return true;
            return _blocked[x, y];
        }

        bool Outside(Vector2 world)
        {
            if (_cfg == null) return true;
            var min = _cfg.cameraBoundsMin;
            var max = _cfg.cameraBoundsMax;
            return world.x < min.x || world.y < min.y || world.x > max.x || world.y > max.y;
        }

        // ---------------------------------------------------------------- per frame

        void LateUpdate()
        {
            var flow = LevelFlow.Instance;
            bool run = DungeonRun.Active && flow != null &&
                       (flow.State == LevelState.Playing || flow.State == LevelState.WaveTransition || flow.State == LevelState.Paused);
            if (_canvas != null) _canvas.enabled = run;
            if (!run) return;
            var heroGo = GameObject.FindGameObjectWithTag("Player");
            if (heroGo == null) return;
            Vector2 hero = heroGo.transform.position;

            PaintClock(flow);
            _repaint -= Time.unscaledDeltaTime;
            if (_repaint <= 0f)
            {
                _repaint = 0.1f;
                PaintMap(hero);
            }
            PlaceMarkers(hero);
            TickStage(flow, heroGo.transform, hero);
        }

        void PaintClock(LevelFlow flow)
        {
            float t = flow.RunTime;
            int mark = DungeonCatalog.StageAt(DungeonRun.Dungeon, _stage).threeStarSeconds;
            _time.text = Clock(Mathf.RoundToInt(t));
            // White under the 3-star mark, yellow once past it, red 30 s later.
            _time.color = t <= mark ? (Color)Pal.TextW
                : t <= mark + 30f ? new Color32(0xF2, 0xD8, 0x4A, 0xFF)
                : new Color32(0xE0, 0x48, 0x48, 0xFF);
        }

        static string Clock(int seconds)
        {
            seconds = Mathf.Max(0, seconds);
            return (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        }

        void PaintMap(Vector2 hero)
        {
            var floor = FloorColour();
            var wall = new Color32(0x24, 0x2A, 0x24, 0xFF);
            var outside = new Color32(0x0E, 0x10, 0x14, 0xFF);
            bool maze = _cfg.mapLayoutId == "FenceMaze";

            int half = MapPx / 2;
            for (int py = 0; py < MapPx; py++)
                for (int px = 0; px < MapPx; px++)
                {
                    var world = hero + new Vector2((px - half) / (float)PxPerTile, (py - half) / (float)PxPerTile);
                    Color32 c = Outside(world) && !maze ? outside : Blocked(world) ? wall : floor;
                    if (maze && !DungeonFenceMaze.WasSeen(DungeonFenceMaze.TileAt(world)))
                        c = Color32.Lerp(c, outside, 0.85f);
                    // Subtle checker so walking reads on a flat floor.
                    if (((px >> 1) + (py >> 1) & 1) == 0 && c.Equals(floor)) c = Color32.Lerp(c, wall, 0.12f);
                    _pix[py * MapPx + px] = c;
                }
            _tex.SetPixels32(_pix);
            _tex.Apply(false);
        }

        Color32 FloorColour()
        {
            switch (_stage)
            {
                case 1: return new Color32(0xB4, 0x5E, 0x32, 0xFF);
                case 2: return new Color32(0x6A, 0x56, 0x42, 0xFF);
                case 3: return new Color32(0x5A, 0x7A, 0x3A, 0xFF);
                case 4: return new Color32(0x6C, 0x66, 0x5C, 0xFF);
                default: return new Color32(0x5C, 0x46, 0x6A, 0xFF);
            }
        }

        void PlaceMarkers(Vector2 hero)
        {
            int n = 0;
            float px = UiKit.Px;
            float half = MapPx * 0.5f - 3f;
            var spawner = LevelFlow.Instance != null ? LevelFlow.Instance.Spawner : null;
            if (spawner != null && spawner.EnemyRoot != null)
                foreach (Transform e in spawner.EnemyRoot)
                {
                    if (!e.gameObject.activeInHierarchy) continue;
                    var hp = e.GetComponent<Health>();
                    if (hp != null && hp.IsDead) continue;
                    bool boss = e.GetComponent<BossWarlordController>() != null;
                    Put(ref n, (Vector2)e.position - hero, boss ? "UI_Minimap_Marker_Boss" : "UI_Minimap_Marker_Enemy", boss ? 7f : 5f, false);
                }
            if (_cfg.mapLayoutId == "FenceMaze")
            {
                foreach (var chest in FindObjectsByType<DungeonMazeChest>(FindObjectsSortMode.None))
                    if (!chest.Used) Put(ref n, (Vector2)chest.transform.position - hero, "UI_Minimap_Marker_Chest", 6f, false);
                var dead = DungeonFenceMaze.DeadEnds;

                if (dead != null)
                    foreach (var tile in dead)
                        if (DungeonFenceMaze.WasSeen(tile))
                            Put(ref n, (Vector2)DungeonFenceMaze.CellWorld(tile) - hero, null, 6f, false, One("Stage2", "Stage2_MiniMap_DeadEnd"));
            }
            if (_cfg.objectiveType == LevelObjectiveType.ClearWavesThenReachExit)
                Put(ref n, _cfg.exitPosition - hero, "UI_Minimap_Marker_Gate", 7f, true);
            for (int i = n; i < _marks.Count; i++) _marks[i].enabled = false;
            _hero.transform.SetAsLastSibling();

            if (_compass.gameObject.activeSelf)
            {
                Vector2 d = _cfg.exitPosition - hero;
                float angle = Mathf.Atan2(d.x, d.y) * Mathf.Rad2Deg;
                _compass.rectTransform.localEulerAngles = new Vector3(0f, 0f, -angle);
            }

            void Put(ref int i, Vector2 delta, string sprite, float size, bool clamp, Sprite direct = null)
            {
                Vector2 ui = delta * PxPerTile;
                bool inside = Mathf.Abs(ui.x) <= half && Mathf.Abs(ui.y) <= half;
                if (!inside && !clamp) return;
                if (!inside)
                {
                    float k = half / Mathf.Max(Mathf.Abs(ui.x), Mathf.Abs(ui.y));
                    ui *= k;
                }
                while (_marks.Count <= i) _marks.Add(Marker(_mapArea, "UI_Minimap_Marker_Enemy", 5f));
                var img = _marks[i++];
                img.sprite = direct != null ? direct : UiKit.Get(sprite);
                img.enabled = img.sprite != null;
                img.rectTransform.sizeDelta = new Vector2(size, size) * px;
                img.rectTransform.anchoredPosition = new Vector2(Mathf.Round(ui.x), Mathf.Round(ui.y)) * px;
            }
        }

        // ---------------------------------------------------------------- stage widgets

        void TickStage(LevelFlow flow, Transform heroT, Vector2 hero)
        {
            bool waveLive = flow.State == LevelState.Playing && !flow.WavesComplete && flow.WaveNumber > 0;
            if (flow.WaveNumber != _lastWave)
            {
                _lastWave = flow.WaveNumber;
                if (_stage == 3 && _lastWave > 0) _warnUntil = Time.unscaledTime + 1.5f;
            }

            // Gate (stages 1, 2, 4): glow once open; edge icon in stage 1 while it is off camera.
            if (_gate != null)
            {
                // Locked: dim. Open: full colour with a slow gold pulse.
                float pulse = flow.ExitUnlocked ? 1f + 0.06f * Mathf.Sin(Time.time * 5f) : 1f;
                _gate.transform.localScale = _gateScale * pulse;
                _gate.color = flow.ExitUnlocked ? new Color(1f, 0.95f, 0.7f) : new Color(0.7f, 0.7f, 0.75f);
            }
            if (_gate != null && _gateArt != null && _gateArt.Length > 1) _gate.sprite = flow.ExitUnlocked ? _gateArt[1] : _gateArt[0];
            EdgeIcon(_gateEdge, _stage == 1 && _cfg.objectiveType == LevelObjectiveType.ClearWavesThenReachExit, _cfg.exitPosition);

            // Stage 1 guide arrows: between waves and once the gate is open, point from the hero to the gate.
            bool guide = _stage == 1 && !waveLive;
            Vector2 toGate = _cfg.exitPosition - hero;
            float dist = toGate.magnitude;
            for (int i = 0; i < _arrows.Count; i++)
            {
                var a = _arrows[i];
                bool on = guide && dist > 2f + i * 1.6f;
                a.gameObject.SetActive(on);
                if (!on) continue;
                Vector2 dir = toGate / Mathf.Max(0.01f, dist);
                a.transform.position = hero + dir * (1.6f + i * 1.6f);
                a.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg);
                if (_arrowArt != null && _arrowArt.Length > 0)
                    a.sprite = _arrowArt[(int)(Time.time * 8f + i) % _arrowArt.Length];
            }

            // Stage 3: edge warnings toward the sides this wave pours in from; encircle ring under the hero.
            if (_stage == 3)
            {
                var sides = DungeonCatalog.CampWaveSides(_lastWave);
                bool warn = Time.unscaledTime < _warnUntil;
                for (int i = 0; i < _warn.Count; i++)
                {
                    bool on = warn && i < sides.Length;
                    _warn[i].gameObject.SetActive(on);
                    if (!on) continue;
                    if (_warnArt != null && _warnArt.Length > 0)
                    {
                        int[] seq = { 0, 1, 0, 2 };
                        _warn[i].sprite = _warnArt[Mathf.Min(_warnArt.Length - 1, seq[(int)(Time.unscaledTime * 10f) % 4])];
                        _warn[i].enabled = true;
                    }
                    WarnAt(_warn[i], sides[i]);
                }
                if (_encircle != null)
                {
                    bool ring = Surrounded(hero, 4f);
                    _encircle.gameObject.SetActive(ring);
                    if (ring)
                    {
                        _encircle.transform.position = hero + new Vector2(0f, -0.35f);
                        if (_ringArt != null && _ringArt.Length >= 2) _encircle.sprite = _ringArt[(int)(Time.time * 4f) % 2];
                    }
                }
            }

            // Stage 4: red border fades in within 0.15 s when the hero is on the bridge, half a tile from its edge.
            if (_edgeDanger != null && _edgeDanger.sprite != null)
            {
                bool onBridge = _cfg.mapLayoutId == DungeonStageMap.Chasm && Mathf.Abs(hero.x) < DungeonStageMap.BridgeHalf;
                bool danger = onBridge && Mathf.Abs(hero.y - DungeonStageMap.BridgeY) > 0.45f;
                var c = _edgeDanger.color;
                c.a = Mathf.MoveTowards(c.a, danger ? 1f : 0f, Time.unscaledDeltaTime / 0.15f);
                _edgeDanger.color = c;
            }

            // Stage 4: bridge progress, one segment per wave.
            if (_stage == 4 && _pips != null)
            {
                int seg = Mathf.Clamp(flow.WaveNumber, 1, 3);
                bool blink = ((int)(Time.unscaledTime * 3f) & 1) == 0;
                for (int i = 0; i < 3; i++)
                {
                    int frame = i < seg - 1 || flow.WavesComplete ? 0 : i == seg - 1 ? (blink ? 1 : 0) : 2;
                    if (i == seg - 1 && !flow.WavesComplete && !blink) frame = 1;
                    _pips[i].sprite = _pipArt != null && _pipArt.Length > frame ? _pipArt[frame] : null;
                    _pips[i].enabled = _pips[i].sprite != null;
                }
                _bridgeText.text = "ĐOẠN " + seg + "/3";
            }

            // Stage 5: ritual ring under the boss start, red telegraph before heavy attacks, summons at 66% / 33%.
            if (_stage == 5) TickBoss(flow);
        }

        void TickBoss(LevelFlow flow)
        {
            var boss = FindFirstObjectByType<BossWarlordController>();
            if (boss == null || boss.Health == null) return;
            if (_ritual != null && !_ritual.gameObject.activeSelf)
            {
                _ritual.transform.position = boss.transform.position + new Vector3(0f, -0.4f, 0f);
                _ritual.gameObject.SetActive(true);
                _telegraph.transform.position = _ritual.transform.position;
            }
            bool tele = boss.State == BossAiState.AreaTelegraph || boss.State == BossAiState.ChargeTelegraph || boss.State == BossAiState.MeleeTelegraph;
            bool strike = boss.State == BossAiState.AreaStrike || boss.State == BossAiState.ChargeActive || boss.State == BossAiState.MeleeStrike;
            if (tele && _telegraphT < 0f) _telegraphT = 0f;
            if (_telegraph != null)
            {
                if (_telegraphT >= 0f)
                {
                    _telegraphT += Time.deltaTime;
                    int frame = strike ? 3 : Mathf.Min(2, (int)(_telegraphT / 0.3f));
                    if (_teleArt != null && _teleArt.Length > frame) _telegraph.sprite = _teleArt[frame];
                    _telegraph.gameObject.SetActive(true);
                    if (!tele && (!strike || _telegraphT > 1.1f)) { _telegraphT = -1f; _telegraph.gameObject.SetActive(false); }
                }
            }
            float hp = boss.Health.Normalized;
            if (!_summon66 && hp <= 0.66f) { _summon66 = true; Summon(flow, boss.transform.position); }
            if (!_summon33 && hp <= 0.33f) { _summon33 = true; Summon(flow, boss.transform.position); }
        }

        static void Summon(LevelFlow flow, Vector2 at)
        {
            var spawner = flow.Spawner;
            if (spawner == null) return;
            spawner.SpawnGoblin(at + new Vector2(-1.6f, -0.8f));
            spawner.SpawnGoblin(at + new Vector2(1.6f, -0.8f));
        }

        bool Surrounded(Vector2 hero, float radius)
        {
            var spawner = LevelFlow.Instance != null ? LevelFlow.Instance.Spawner : null;
            if (spawner == null || spawner.EnemyRoot == null) return false;
            int quads = 0;
            foreach (Transform e in spawner.EnemyRoot)
            {
                if (!e.gameObject.activeInHierarchy) continue;
                var hp = e.GetComponent<Health>();
                if (hp != null && hp.IsDead) continue;
                Vector2 d = (Vector2)e.position - hero;
                if (d.sqrMagnitude > radius * radius) continue;
                quads |= 1 << ((d.x >= 0f ? 0 : 1) + (d.y >= 0f ? 0 : 2));
            }
            int count = 0;
            for (int i = 0; i < 4; i++) if ((quads & (1 << i)) != 0) count++;
            return count >= 3;
        }

        /// <summary>Clamps an icon to the screen edge (8 kit px in) toward a world point that is off camera.</summary>
        void EdgeIcon(Image icon, bool want, Vector2 world)
        {
            if (icon == null) return;
            var cam = Camera.main;
            if (!want || cam == null || icon.sprite == null) { icon.gameObject.SetActive(false); return; }
            Vector3 vp = cam.WorldToViewportPoint(world);
            bool onScreen = vp.z > 0f && vp.x > 0f && vp.x < 1f && vp.y > 0f && vp.y < 1f;
            icon.gameObject.SetActive(!onScreen);
            if (onScreen) return;
            var rect = ((RectTransform)_canvas.transform).rect;
            Vector2 c = new Vector2(vp.x - 0.5f, vp.y - 0.5f);
            float k = 0.5f / Mathf.Max(Mathf.Abs(c.x), Mathf.Abs(c.y), 0.001f);
            c *= k;
            float inset = 16f * UiKit.Px;
            var pos = new Vector2(c.x * (rect.width - inset * 2f), c.y * (rect.height - inset * 2f));
            // Keep clear of the wave ribbon and objective strip at the top (they end at kit y 41).
            pos.y = Mathf.Min(pos.y, rect.height * 0.5f - 54f * UiKit.Px);
            var layout = icon.GetComponent<UiLayout>();
            if (layout != null) layout.enabled = false;
            icon.rectTransform.anchoredPosition = pos;
            icon.rectTransform.sizeDelta = new Vector2(16f, 16f) * UiKit.Px;
        }

        /// <summary>Warning on the screen edge on the given side, arrow drawn pointing right, turned to point inward.</summary>
        void WarnAt(Image img, Vector2 side)
        {
            var rect = ((RectTransform)_canvas.transform).rect;
            float inset = 18f * UiKit.Px;
            var layout = img.GetComponent<UiLayout>();
            if (layout != null) layout.enabled = false;
            // The top edge sits under the wave ribbon and objective strip (kit y 41).
            float top = side.y > 0f ? rect.height * 0.5f - 54f * UiKit.Px : rect.height * 0.5f - inset;
            img.rectTransform.anchoredPosition = new Vector2(side.x * (rect.width * 0.5f - inset), side.y * top);
            img.rectTransform.sizeDelta = new Vector2(16f, 16f) * UiKit.Px;
            float angle = Mathf.Atan2(-side.y, -side.x) * Mathf.Rad2Deg;
            img.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
        }

        // ---------------------------------------------------------------- world decor

        void BuildDecor(LevelConfig cfg)
        {
            _decor = new GameObject("DungeonDecor").transform;
            _arrowArt ??= DungeonFenceMaze.Art("Stage1", "Stage1_GuideArrow_Sheet_4f");
            _warnArt ??= DungeonFenceMaze.Art("Stage3", "Stage3_SurroundWarn_Sheet_3f");
            _ringArt ??= DungeonFenceMaze.Art("Stage3", "Stage3_EncircleRing_Sheet_2f");
            _teleArt ??= DungeonFenceMaze.Art("Stage5", "Stage5_RitualTelegraph_Sheet_4f");
            _pipArt ??= DungeonFenceMaze.Art("Stage4", "Stage4_BridgeBar_Pips_3x18x8");
            _gateArt ??= DungeonFenceMaze.Art("Stage2", "Stage2_FenceGate_2f");

            bool exit = cfg.objectiveType == LevelObjectiveType.ClearWavesThenReachExit;
            if (exit && cfg.mapLayoutId != "FenceMaze")
            {
                // Wooden gate + glow replaces the white debug ring.
                _gate = Sprite("Gate", _gateArt.Length > 0 ? _gateArt[0] : null, cfg.exitPosition, 40, 1.6f);
                // No Light2D here: one point light in a scene without a global light turns every lit sprite black.
                _gateScale = _gate.transform.localScale;
            }

            if (_stage == 1)
            {
                // The RedTrail map paints its own trail; the decal is only for the old Trail layout.
                if (!DungeonStageMap.Handles(cfg.mapLayoutId)) RedDirt(cfg.heroSpawn, cfg.exitPosition);
                for (int i = 0; i < 3; i++)
                    _arrows.Add(Sprite("GuideArrow" + i, _arrowArt.Length > 0 ? _arrowArt[0] : null, cfg.heroSpawn, 15, 1f));
            }
            if (_stage == 3)
            {
                bool sheet = _ringArt.Length == 2;
                _encircle = Sprite("Encircle", sheet ? _ringArt[0] : ProceduralSprites.Ring, cfg.heroSpawn, 15, 3f);
                if (!sheet) _encircle.color = new Color(0.88f, 0.28f, 0.28f, 0.45f);
                _encircle.gameObject.SetActive(false);
            }
            if (_stage == 5)
            {
                // The BoneYard map draws the bone ring; the telegraph sits on it at the same scale.
                bool mapRing = cfg.mapLayoutId == DungeonStageMap.BoneYard;
                if (!mapRing)
                {
                    _ritual = Sprite("RitualRing", One("Stage5", "Stage5_RitualRing"), Vector2.zero, -5, 8f);
                    _ritual.gameObject.SetActive(false);
                }
                _telegraph = Sprite("RitualTelegraph", _teleArt.Length > 0 ? _teleArt[0] : null, mapRing ? DungeonStageMap.RitualCentre : Vector2.zero, -4, 8f);
                _telegraph.sortingLayerName = "Ground";
                _telegraph.sortingOrder = 11;
                _telegraph.gameObject.SetActive(false);
            }
        }

        /// <summary>Red dirt decal (Tiled, Ground layer above Details) from the spawn to the gate with a gentle S.</summary>
        void RedDirt(Vector2 from, Vector2 to)
        {
            var nine = One("Stage1", "Stage1_RedDirtPath_9s");
            if (nine == null) return;
            // Two layers per segment: the 9-slice strip keeps the soft grass edge, and a centre-only strip on
            // top hides the hard border line the 9-slice draws at every joint.
            var r = nine.rect;
            var centre = _dirtCentre != null ? _dirtCentre : _dirtCentre = UnityEngine.Sprite.Create(nine.texture, new Rect(r.x + 16f, r.y + 16f, r.width - 32f, r.height - 32f),
                new Vector2(0.5f, 0.5f), nine.pixelsPerUnit, 0, SpriteMeshType.FullRect);
            const int segments = 10;
            Vector2 axis = to - from;
            Vector2 side = new Vector2(-axis.y, axis.x).normalized;
            Vector2 prev = from;
            for (int i = 1; i <= segments; i++)
            {
                float t = i / (float)segments;
                Vector2 p = from + axis * t + side * Mathf.Sin(t * Mathf.PI * 2f) * 1.5f;
                if (i == segments) p = to;
                Vector2 d = p - prev;
                var go = new GameObject("RedDirt" + i);
                go.transform.SetParent(_decor, false);
                go.transform.position = (prev + p) * 0.5f;
                go.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
                Strip(go.transform, nine, new Vector2(d.magnitude + 2.6f, 3f), 3);
                Strip(go.transform, centre, new Vector2(d.magnitude + 1.2f, 1.4f), 4);
                prev = p;
            }
        }

        static void Strip(Transform parent, Sprite sprite, Vector2 size, int order)
        {
            var go = new GameObject("Strip");
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.size = size;
            sr.sortingLayerName = "Ground";
            sr.sortingOrder = order;
            sr.sharedMaterial = ProceduralSprites.Unlit;
        }

        SpriteRenderer Sprite(string name, Sprite sprite, Vector2 at, int order, float width)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_decor, false);
            go.transform.position = at;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            // No global 2D light in the scene: the default lit material would draw these black.
            sr.sharedMaterial = ProceduralSprites.Unlit;
            // Normalise size: some sheets were imported at PPU 100, the spec wants tile units.
            if (sprite != null && sprite.bounds.size.x > 0.001f)
            {
                float s = width / sprite.bounds.size.x;
                go.transform.localScale = new Vector3(s, s, 1f);
            }
            return sr;
        }


        void ClearDecor()
        {
            if (_decor != null) Destroy(_decor.gameObject);
            _decor = null;
            _gate = null;
            _arrows.Clear();
            _encircle = _ritual = _telegraph = null;
            foreach (var w in _warn) if (w != null) w.gameObject.SetActive(false);
            if (_gateEdge != null) _gateEdge.gameObject.SetActive(false);
        }

        static Sprite One(string stage, string file)
        {
            var sheet = DungeonFenceMaze.Art(stage, file);
            return sheet.Length > 0 ? sheet[0] : null;
        }
    }
}
