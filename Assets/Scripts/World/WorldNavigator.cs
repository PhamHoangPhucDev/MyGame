using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameplayPrototype
{
    public enum WorldZone
    {
        None = 0,
        Hub = 1,
        Training = 2,
        Resource = 3,
        Farm = 4,
        Boss = 5,
        Explore = 6,
        Treasure = 7,
        Ward = 8,
        BossExtra = 9,
        Housing = 10,
        Field = 11
    }

    /// <summary>
    /// Moves the existing hero between world zones. Does not spawn a second player or camera.
    /// </summary>
    public class WorldNavigator : MonoBehaviour
    {
        public static WorldNavigator Instance { get; private set; }

        public WorldZone Current { get; private set; } = WorldZone.None;
        public string FieldId { get; private set; } = "";
        public TrainingGround Training { get; private set; }
        public GatherNode Mine { get; private set; }
        public GatherNode Wood { get; private set; }
        public FarmPlot[] Plots { get; private set; }
        public BossArea Boss { get; private set; }

        GameObject _root;
        GameObject _hub;
        GameObject _training;
        GameObject _resource;
        GameObject _farm;
        GameObject _boss;
        GameObject _board;
        GameObject _bagPanel;
        Text _bagText;
        Text _prompt;
        Text _boardBody;
        bool _built;
        string _note = "";
        float _noteUntil;
        float _noteFrame;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            Build();
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        AspectProfileId _seenAspect;
        bool _seenAspectSet;

        /// <summary>Interactable the hero can use right now (read by the mobile E button); null when nothing is in range.</summary>
        public WorldInteractable Target { get; private set; }

        void Update()
        {
            Target = null;
            HomeFarm.TickAll();
            if (Current != WorldZone.None)
            {
                var aspect = AspectProfileService.Instance;
                if (aspect != null && (!_seenAspectSet || aspect.ActiveId != _seenAspect))
                {
                    _seenAspectSet = true;
                    _seenAspect = aspect.ActiveId;
                    var follow = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
                    var layout = Layout(Current);
                    follow?.SetBounds(layout.boundsMin, layout.boundsMax);
                }
            }
            if (Current == WorldZone.None) return;
            GateHud();
            var hero = Hero();
            if (hero != null)
            {
                var health = hero.GetComponent<Health>();
                if (health != null && health.IsDead)
                {
                    health.ReviveFull();
                    Show(WorldZone.Hub);
                    SetPrompt("You fell. Back in the village.");
                    return;
                }
            }
            if (HomeFarm.Captures)
            {
                if (_coinText != null) _coinText.text = WorldSave.Coins.ToString();
                RefreshZoneStatus();
                return;
            }
            var kb = PlayerChat.Typing ? null : Keyboard.current; // chat line open: game keys off
            if (kb != null && kb.bKey.wasPressedThisFrame && Current == WorldZone.Housing)
                BuildMode.Toggle();
            if (kb != null && BuildMode.IsOpen)
                BuildMode.TickKeys(kb);
            if (kb != null && kb.mKey.wasPressedThisFrame)
                WorldMapPanel.Toggle();
            if (kb != null && kb.escapeKey.wasPressedThisFrame)
            {
                if (WorldMapPanel.IsOpen) { WorldMapPanel.Close(); return; }
                if (SpendConfirm.Pending) { SpendConfirm.Clear(); return; }
                if (Mailbox.IsOpen) { Mailbox.Close(); return; }
                if (BuildMode.IsOpen) { BuildMode.Escape(); return; }
                if (_board != null && _board.activeSelf) _board.SetActive(false);
                else ReturnToMenu();
                return;
            }
            if (kb != null && kb.iKey.wasPressedThisFrame)
                ToggleBag();
            var nearest = Nearest(hero);
            Target = nearest;
            if (_prompt != null && (_board == null || !_board.activeSelf))
            {
                if (Time.unscaledTime < _noteUntil && !string.IsNullOrEmpty(_note))
                    _prompt.text = _note;
                else
                {
                    if (HomeFarm.Active) SetPromptVisible(false);
                    _prompt.text = nearest == null ? ZoneHint() : nearest.DisplayName;
                }
            }
            if (_promptKey != null)
            {
                _promptKey.transform.parent.gameObject.SetActive(nearest != null && !UiKit.IsMobile);
                _promptKey.text = InteractName();
            }
            if (_coinText != null) _coinText.text = WorldSave.Coins.ToString();
            TickBagGate();
            RefreshZoneStatus();
            if (WorldMapPanel.IsOpen && kb != null && kb.eKey.wasPressedThisFrame)
            {
                WorldMapPanel.Travel();
                return;
            }
            bool touchUse = MobileTouch.ConsumeInteract();
            bool use = (kb != null && kb.eKey.wasPressedThisFrame) || touchUse;
            if (use && SpendConfirm.Pending)
                SpendConfirm.Accept();
            else if (BuildMode.IsOpen && use)
                YardHand.Confirm();
            else if (nearest != null && (InteractPressed(kb) || touchUse))
            {
                nearest.Interact();
                // Keep the reply on screen for a few seconds; ids such as "slot_house" are not player text.
                string said = nearest.Line;
                // A result already flashed this frame (Ok / Fail reason) wins over the generic line.
                bool answered = Mathf.Abs(Time.unscaledTime - _noteFrame) < 0.05f && Time.unscaledTime < _noteUntil;
                if (!answered && !string.IsNullOrEmpty(said) && said.IndexOf(' ') > 0) Flash(said);
            }
        }

        public void Flash(string line)
        {
            if (string.IsNullOrEmpty(line)) return;
            if (Mathf.Abs(Time.unscaledTime - _noteFrame) < 0.05f && !string.IsNullOrEmpty(_note))
                _note = _note + "  |  " + line;
            else
                _note = line;
            _noteFrame = Time.unscaledTime;
            _noteUntil = Time.unscaledTime + 4f;
            if (_prompt != null && _prompt.transform.parent != null && (_board == null || !_board.activeSelf))
                _prompt.transform.parent.gameObject.SetActive(Current != WorldZone.None);
            if (_prompt != null && (_board == null || !_board.activeSelf))
                _prompt.text = _note;
        }

        public void CloseDungeonBoard()
        {
            if (_board != null) _board.SetActive(false);
        }

        public void EnterOutdoorCamp()
        {
            Build();
            CloseBag();
            CloseDungeonBoard();
            WorldServices.Instance?.HidePanel();
            Current = WorldZone.Training;
            if (_hub != null) _hub.SetActive(false);
            if (_training != null) _training.SetActive(false);
            if (_resource != null) _resource.SetActive(false);
            if (_farm != null) _farm.SetActive(false);
            if (_boss != null) _boss.SetActive(false);
            WorldSites.Activate(WorldZone.None);
            FieldWorld.Activate("");
            if (_root != null) _root.SetActive(false);
            if (_worldHud != null) _worldHud.SetActive(false);
            LevelFlow.Instance?.BeginOutdoor();
            CampMap.Show(Hero());
            var spawner = LevelFlow.Instance != null ? LevelFlow.Instance.Spawner : null;
            CampMap.SpawnFoes(spawner);
            CampMap.BeginTraining();
            Pets.Appear();
            Flash("Goblin Forest Camp");
        }

        public void EnterFromMenu()
        {
            LevelFlow.Instance?.ReleaseToWorld();
            Show(WorldZone.Hub);
            var hero = Hero();
            Gear.RestoreSavedHp(hero != null ? hero.GetComponent<Health>() : null);
            PendingRewards.TryFulfill();
        }

        public void ReturnFromDungeon()
        {
            LevelFlow.Instance?.ReleaseToWorld();
            Show(WorldZone.Hub);
        }

        public void ReturnToMenu()
        {
            Show(WorldZone.None);
            LevelFlow.Instance?.EnterMainMenu();
        }

        public void NotifyDungeonEntered()
        {
            if (_board != null) _board.SetActive(false);
            Show(WorldZone.None);
        }

        public void ShowField(string zoneId)
        {
            Build();
            var map = WorldFoundation.FindField(zoneId);
            if (string.IsNullOrEmpty(map.zoneId))
            {
                Show(WorldZone.Hub);
                return;
            }
            FieldId = map.zoneId;
            Show(WorldZone.Field);
        }

        public void Show(WorldZone zone)
        {
            Build();
            if (zone == WorldZone.Farm) zone = WorldZone.Housing;
            if (zone != WorldZone.Field) FieldId = "";
            if (zone == WorldZone.Field && string.IsNullOrEmpty(FieldId)) zone = WorldZone.Hub;
            Current = zone;
            CampMap.Hide();
            // Zone travel dismisses village windows so the map, bag, and services cannot cover the destination.
            CloseBag();
            CloseDungeonBoard();
            WorldServices.Instance?.HidePanel();
            var flow = LevelFlow.Instance;
            if (flow != null && flow.Spawner != null && zone != WorldZone.None)
            {
                flow.Spawner.ResetSession();
                flow.Spawner.SetSpawningEnabled(false);
            }
            if (_hub != null) _hub.SetActive(zone == WorldZone.Hub);
            if (_training != null) _training.SetActive(zone == WorldZone.Training);
            if (_resource != null) _resource.SetActive(zone == WorldZone.Resource);
            if (_farm != null) _farm.SetActive(zone == WorldZone.Farm);
            if (_boss != null) _boss.SetActive(zone == WorldZone.Boss);
            WorldSites.Activate(zone);
            FieldWorld.Activate(zone == WorldZone.Field ? FieldId : "");
            if (_root != null) _root.SetActive(zone != WorldZone.None);
            if (_worldHud != null) _worldHud.SetActive(zone != WorldZone.None);
            if (_prompt != null && _prompt.transform.parent != null) _prompt.transform.parent.gameObject.SetActive(zone != WorldZone.None);
            if (zone == WorldZone.None && _bagPanel != null) _bagPanel.SetActive(false);
            if (zone == WorldZone.None)
            {
                if (_board != null) _board.SetActive(false);
                WorldServices.Instance?.HidePanel();
                WorldServices.NotifyZone(zone);
                Pets.Appear();
                PetHud.SetShown(false);
                return;
            }

            if (BuildMode.IsOpen) BuildMode.Close();
            ZoneLayout layout = Layout(zone);
            PlaceHero(layout.spawn);
            var cam = Camera.main != null ? Camera.main.GetComponent<CameraFollow>() : null;
            if (cam != null)
            {
                cam.ClearPan();
                cam.SetBounds(layout.boundsMin, layout.boundsMax);
                var hero = Hero();
                if (hero != null) cam.SetTarget(hero);
            }
            if (zone == WorldZone.Resource)
            {
                Mine?.Refresh();
                Wood?.Refresh();
            }
            if (zone == WorldZone.Farm && Plots != null)
            {
                for (int i = 0; i < Plots.Length; i++) Plots[i]?.Refresh();
            }
            SetPrompt(ZoneHint());
            WorldServices.NotifyZone(zone);
            PetHud.Ensure();
            PetHud.SetShown(true);
            Pets.Appear();
        }

        public void OpenDungeonBoard()
        {
            Build();
            WorldServices.Instance?.HidePanel();
            CloseBag();
            if (_boardWin == null) return;
            _board.SetActive(true);
            _boardWin.Clear();
            var body = _boardWin.body;
            var flow = LevelFlow.Instance;
            for (int level = 1; level <= LevelFlow.CampaignLevelCount + 1; level++)
            {
                var cfg = flow != null ? flow.GetLevelConfig(level) : null;
                bool campaign = level <= LevelFlow.CampaignLevelCount;
                if (!campaign && cfg == null) break;
                string title = cfg != null ? cfg.levelTitle : ("LEVEL " + level);
                bool open = campaign && GameProgress.IsLevelUnlocked(level);
                var row = UiKit.Rect(body, "Dungeon" + level);
                row.gameObject.AddComponent<UiLayout>()
                    .Portrait(UiLayout.Spec.Stretch(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -(level - 1) * 60f), new Vector2(0f, 56f)))
                    .Landscape(UiLayout.Spec.Stretch(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -(level - 1) * 44f), new Vector2(0f, 40f)));
                UiKit.Fill(UiKit.Row(row, "Bg", open ? "" : "Disabled", Vector2.zero));
                UiKit.Icon(row, !open ? "UI_Icon_Lock" : level == LevelFlow.CampaignLevelCount ? "UI_Icon_Boss" : "UI_Icon_Dungeon", new Vector2(0f, 0.5f), new Vector2(14f, 0f));
                Line(row, level + ". " + title, new Vector2(28f, -6f), open ? Pal.GoldL : Pal.TextMute);
                if (!campaign)
                {
                    Line(row, "COMING SOON", new Vector2(28f, -20f), Pal.TextMute);
                    continue;
                }
                if (!open)
                {
                    Line(row, "LOCKED", new Vector2(28f, -20f), Pal.TextMute);
                    continue;
                }
                int stars = GameProgress.GetBestStars(level);
                Line(row, "BEST " + GameProgress.GetBestScore(level), new Vector2(28f, -20f), Pal.TextW);
                for (int k = 0; k < 3; k++)
                    UiKit.Icon(row, k < stars ? "UI_Icon_Star" : "UI_Icon_StarEmpty", new Vector2(0f, 1f), new Vector2(124f + k * 16f, -24f));
                int captured = level;
                var start = UiKit.Button(row, "Start", "Primary", "START", new Vector2(56f, 18f), () =>
                {
                    if (!GameProgress.IsLevelUnlocked(captured)) return;
                    EnterDungeon(captured);
                });
                Corner(start, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-6f, 0f));
            }
            var note = Line(body, "WARDEN  Unlocks follow the dungeon save.", Vector2.zero, Pal.TextMute);
            UiKit.Place(note, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(0f, 2f), new Vector2(260f, 10f));
        }

        public void EnterDungeon(int level)
        {
            if (_board != null) _board.SetActive(false);
            var flow = LevelFlow.Instance;
            if (flow == null) return;
            flow.StartLevelByNumber(level);
        }

        void Build()
        {
            if (_built) return;
            _built = true;
            _root = new GameObject("WorldRoot");

            _hub = Zone("Hub", WorldCatalog.Hub.origin, new Color(0.22f, 0.38f, 0.24f, 1f));
            Road(_hub.transform, new Vector2(0f, 0.2f), new Vector2(1.7f, 12.6f));
            Road(_hub.transform, new Vector2(0f, 5.4f), new Vector2(7.2f, 1.15f));
            var warden = Prop(_hub.transform, "Warden", WorldCatalog.Hub.origin + new Vector2(0f, 3.2f),
                "Warden", new Color(0.85f, 0.72f, 0.35f, 1f), new Vector2(0.8f, 1.3f));
            var talk = warden.GetComponent<WorldInteractable>();
            talk.DisplayName = "Warden";
            talk.Line = "Four depths are open from here.";
            talk.OnInteract = OpenDungeonBoard;
            Gate(_hub.transform, "ToTraining", WorldCatalog.Hub.origin + new Vector2(-2.7f, 5.4f), "Training", () => Show(WorldZone.Training));
            Gate(_hub.transform, "ToFarm", WorldCatalog.Hub.origin + new Vector2(-0.9f, 5.4f), "Housing", () => Show(WorldZone.Housing));
            var board = Prop(_hub.transform, "Notice", WorldCatalog.Hub.origin + new Vector2(3.4f, -6.3f),
                "Menu", new Color(0.55f, 0.45f, 0.3f, 1f), new Vector2(0.9f, 1.1f));
            var menuTalk = board.GetComponent<WorldInteractable>();
            menuTalk.DisplayName = "Notice";
            menuTalk.Line = "Return to the main menu.";
            menuTalk.OnInteract = ReturnToMenu;

            _training = Zone("Training", WorldCatalog.Training.origin, new Color(0.28f, 0.24f, 0.18f, 1f));
            Road(_training.transform, new Vector2(0f, -1.6f), new Vector2(1.5f, 8f));
            Training = _training.AddComponent<TrainingGround>();
            Gate(_training.transform, "Back", WorldCatalog.Training.origin + new Vector2(0f, -5.8f), "Village", () => Show(WorldZone.Hub));

            _resource = Zone("Resource", WorldCatalog.Resource.origin, new Color(0.2f, 0.24f, 0.22f, 1f));
            Road(_resource.transform, new Vector2(0f, -1.6f), new Vector2(1.5f, 8f));
            var mine = Marker(_resource.transform, "Mine", WorldCatalog.Resource.origin + new Vector2(-1.6f, 1.4f));
            Mine = mine.AddComponent<GatherNode>();
            Mine.Init(WorldCatalog.Mine);
            var wood = Marker(_resource.transform, "Wood", WorldCatalog.Resource.origin + new Vector2(1.6f, 1.4f));
            Wood = wood.AddComponent<GatherNode>();
            Wood.Init(WorldCatalog.Wood);
            Gate(_resource.transform, "Back", WorldCatalog.Resource.origin + new Vector2(0f, -5.8f), "Village", () => Show(WorldZone.Hub));

            _farm = Zone("Farm", WorldCatalog.Farm.origin, new Color(0.24f, 0.32f, 0.18f, 1f));
            Road(_farm.transform, new Vector2(0f, -1.6f), new Vector2(1.5f, 8f));
            Plots = new FarmPlot[3];
            for (int i = 0; i < 3; i++)
            {
                var plot = Marker(_farm.transform, "Plot" + i, WorldCatalog.Farm.origin + new Vector2(-2f + i * 2f, 1.2f));
                Plots[i] = plot.AddComponent<FarmPlot>();
                Plots[i].Init(i);
            }
            Gate(_farm.transform, "Back", WorldCatalog.Farm.origin + new Vector2(0f, -5.8f), "Village", () => Show(WorldZone.Hub));

            _boss = Zone("Boss", WorldCatalog.Boss.origin, new Color(0.32f, 0.16f, 0.16f, 1f));
            Road(_boss.transform, new Vector2(0f, -1.6f), new Vector2(1.5f, 8f));
            Boss = _boss.AddComponent<BossArea>();
            Gate(_boss.transform, "Back", WorldCatalog.Boss.origin + new Vector2(0f, -5.8f), "Village", () => Show(WorldZone.Hub));

            var spawner = LevelFlow.Instance != null ? LevelFlow.Instance.Spawner : null;
            if (spawner == null) spawner = FindFirstObjectByType<EnemySpawner>();
            Training.Bind(spawner);
            Boss.Bind(spawner);
            WorldServices.Install(this, _hub.transform, _training.transform, _resource.transform);
            VillageHubDress.Apply(_hub.transform);
            OriginalVillage.Apply(_hub.transform);
            WorldSites.Install(this, _root.transform, spawner);
            FieldWorld.Install(_root.transform, spawner);

            BuildBoard();
            if (GetComponent<TrainingHud>() == null) gameObject.AddComponent<TrainingHud>();
            _hub.SetActive(false);
            _training.SetActive(false);
            _resource.SetActive(false);
            _farm.SetActive(false);
            _boss.SetActive(false);
            _root.SetActive(false);
        }

        GameObject Zone(string name, Vector2 origin, Color ground)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_root.transform, false);
            go.transform.position = origin;
            var floor = new GameObject("Ground");
            floor.transform.SetParent(go.transform, false);
            floor.transform.localPosition = Vector3.zero;
            var visual = floor.AddComponent<WorldVisual>();
            visual.Build("", ground, new Vector2(14f, 16f));
            var sr = floor.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = -8;
            return go;
        }

        static void Road(Transform parent, Vector2 local, Vector2 size)
        {
            var go = new GameObject("Road");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.AddComponent<WorldVisual>().Build("", WorldGraybox.Road, size);
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr != null) sr.sortingOrder = -7;
        }

        static GameObject Marker(Transform parent, string name, Vector2 world)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = world;
            return go;
        }

        GameObject Prop(Transform parent, string name, Vector2 world, string label, Color color, Vector2 size)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = world;
            go.AddComponent<WorldVisual>().Build(label, color, size);
            var talk = go.AddComponent<WorldInteractable>();
            talk.DisplayName = label;
            return go;
        }

        void Gate(Transform parent, string name, Vector2 world, string label, System.Action enter)
        {
            var go = Prop(parent, name, world, label, WorldGraybox.Gate, new Vector2(0.9f, 0.7f));
            var talk = go.GetComponent<WorldInteractable>();
            talk.DisplayName = label;
            talk.Radius = 0.8f;
            talk.Line = "Enter " + label + ".";
            talk.OnInteract = () => enter();
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Vector2.one;
            var gate = go.AddComponent<WorldGate>();
            gate.OnEnter = () => enter();
        }

        UiWindow _boardWin;
        UiWindow _bagWin;
        GameObject _worldHud;
        GraphicRaycaster _hudRay, _tapRay, _combatRay, _farmRay;

        void GateHud()
        {
            bool open = (_bagPanel != null && _bagPanel.activeSelf)
                || (_board != null && _board.activeSelf)
                || (WorldServices.Instance != null && WorldServices.Instance.PanelOpen)
                || Mailbox.IsOpen;
            Hero()?.GetComponent<PlayerMovement>()?.SetMovementLocked(open);
            HudRay(ref _hudRay, "WorldHud", !open);
            HudRay(ref _tapRay, "WorldHudTaps", !open);
            HudRay(ref _combatRay, "CombatCanvas", !open);
            HudRay(ref _farmRay, "FarmMode", !open);
        }

        static void HudRay(ref GraphicRaycaster ray, string canvasName, bool on)
        {
            if (ray == null)
            {
                var go = GameObject.Find(canvasName);
                if (go == null) return;
                ray = go.GetComponent<GraphicRaycaster>();
            }
            if (ray != null && ray.enabled != on) ray.enabled = on;
        }

        Text _coinText;
        Text _promptKey;
        int _bagTab;
        string _bagSel = "";

        void BuildBoard()
        {
            var canvas = UiKit.Canvas("WorldWindows", 260);
            // Dungeon selection (P10 / L07).
            _boardWin = UiKit.Window(canvas.transform, "DungeonBoard", "DUNGEONS", new Vector2(440f, 300f));
            _boardWin.GetComponent<UiLayout>().Portrait(UiLayout.Spec.At(UiKit.Center, UiKit.Center, new Vector2(0f, 6f), new Vector2(272f, 420f)));
            _board = _boardWin.gameObject;
            _board.SetActive(false);

            // Bag / hero / skills (P14-P16, P22 / L05, L17, L18, L23).
            _bagWin = UiKit.Window(canvas.transform, "BagPanel", "BAG", new Vector2(440f, 300f));
            _bagWin.GetComponent<UiLayout>().Portrait(UiLayout.Spec.At(UiKit.Center, UiKit.Center, new Vector2(0f, 6f), new Vector2(272f, 440f)));
            _bagPanel = _bagWin.gameObject;
            _bagPanel.SetActive(false);

            // Interaction / NPC line plate, bottom centre (P18 / L06 base).
            var promptCanvas = UiKit.Canvas("WorldPrompt", 30);
            promptCanvas.GetComponent<GraphicRaycaster>().enabled = false;
            var plate = UiKit.Panel(promptCanvas.transform, "PromptPlate", "UI_NPC_DialoguePanel");
            // Landscape PC: above the skill row (bottom 8..36 px).
            // Touch: low and narrow between the joystick and the E button / skill arc, so it hides less of the yard.
            UiKit.Place(plate, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                    UiKit.IsMobile ? new Vector2(-40f, 6f) : new Vector2(0f, 42f), UiKit.IsMobile ? new Vector2(260f, 30f) : new Vector2(300f, 30f))
                .Portrait(UiLayout.Spec.At(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 150f), new Vector2(248f, 48f)));
            var key = UiKit.Panel(plate.transform, "Key", "UI_KeyCap");
            UiKit.Place(key, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(11f, 11f));
            _promptKey = UiKit.Label(key.transform, "K", "E", 8, Pal.GoldL, TextAnchor.MiddleCenter, false);
            UiKit.Fill(_promptKey);
            _prompt = UiKit.Label(plate.transform, "Prompt", "", 8, Pal.TextW, TextAnchor.MiddleLeft);
            _prompt.GetComponent<UiLayout>().Both(UiLayout.Spec.Stretch(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), new Vector2(8f, 0f), new Vector2(-40f, -8f)));
            plate.gameObject.SetActive(Current != WorldZone.None);

            // Village HUD buttons: bag, map, coins (top-right, left of pause).
            var hud = UiKit.Canvas("WorldHud", 220);
            _worldHud = hud.gameObject;
            // Bag and map sit above hidden windows (build 240, mailbox 245, bag window 260, reward 270).
            var taps = UiKit.Canvas("WorldHudTaps", 280);
            var bag = UiKit.IconButton(taps.transform, "BagButton", "UI_Icon_Inventory", ToggleBag);
            // Touch devices get thumb-sized bag/map taps; the coin row and housing plate move down to match.
            bool touch = UiKit.IsMobile;
            UiKit.Place(bag, new Vector2(1f, 1f), new Vector2(1f, 1f), touch ? new Vector2(-74f, -6f) : new Vector2(-62f, -8f), touch ? new Vector2(30f, 30f) : new Vector2(22f, 22f));
            var map = UiKit.IconButton(taps.transform, "MapButton", "UI_Icon_WorldMap", () => WorldMapPanel.Toggle());
            UiKit.Place(map, new Vector2(1f, 1f), new Vector2(1f, 1f), touch ? new Vector2(-40f, -6f) : new Vector2(-36f, -8f), touch ? new Vector2(30f, 30f) : new Vector2(22f, 22f));
            _coinText = UiKit.Coins(hud.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10f, touch ? -40f : -34f));

            // Zone status plate (Farming P26 / Housing P30): read-only state from existing logic.
            var status = UiKit.Panel(hud.transform, "ZoneStatus", "UI_Panel_HUD");
            UiKit.Place(status, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f), new Vector2(184f, 66f))
                .Portrait(UiLayout.Spec.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f), new Vector2(184f, 66f)));
            _zoneStatus = UiKit.Label(status.transform, "Text", "", 8, Pal.TextW, TextAnchor.MiddleLeft);
            UiKit.Fill(_zoneStatus, 5f);
            _zoneStatus.lineSpacing = 1.25f;
            _zoneStatusPlate = status.gameObject;
            _zoneStatusPlate.SetActive(false);

            // Housing gets its own compact plate under the coins (top-right), so the yard stays visible in 9:16.
            // Title + have/need chips (wood, stone icons); red only for what is short. Coins live in the coin row above.
            var house = UiKit.Panel(hud.transform, "HouseStatus", "UI_Panel_HUD");
            UiKit.Place(house, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-6f, touch ? -56f : -46f), new Vector2(96f, 28f))
                .Portrait(UiLayout.Spec.At(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-6f, -82f), new Vector2(96f, 28f)));
            _housePanel = house;
            _houseStatus = UiKit.Label(house.transform, "Text", "", 8, Pal.TextW, TextAnchor.MiddleLeft);
            UiKit.Place(_houseStatus, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(5f, -4f), new Vector2(88f, 10f));
            _houseStatus.horizontalOverflow = HorizontalWrapMode.Overflow;
            _houseWoodIcon = HouseChip(house.transform, "Wood", 5f, out _houseWood);
            _houseOreIcon = HouseChip(house.transform, "Stone", 50f, out _houseOre);
            BuildHouseButton(house.transform);
            _houseStatusPlate = house.gameObject;
            _houseStatusPlate.SetActive(false);
            _worldHud.SetActive(false);
        }

        Text _zoneStatus, _houseStatus;
        GameObject _zoneStatusPlate, _houseStatusPlate;
        Image _housePanel, _houseWoodIcon, _houseOreIcon;
        Text _houseWood, _houseOre;

        /// <summary>Housing: what to do next, from the build state (shown when nothing is in reach).</summary>
        static string HousingHint()
        {
            if (BuildMode.HouseBuildProgress(out float p)) return "Your house is being built: " + Mathf.RoundToInt(p * 100f) + "%";
            if (Housing.HasHouse) return "Go to the + sign to expand land. BUILD decorates.";
            var d = BuildMode.Defs[0];
            if (WorldSave.GetQty("wood") < d.wood || WorldSave.GetQty("ore") < d.ore)
                return "House needs " + d.wood + " wood + " + d.ore + " ore. Find them in the Resource Field.";
            return UiKit.IsMobile ? "Tap BUILD to build your house." : "Press B or BUILD to build your house.";
        }

        /// <summary>
        /// Farm: each plot's EMPTY / GROWING / READY from FarmPlot. Housing: the selected slot from WorldFlags.
        /// No timers here: the farm clock belongs to logic (see Docs/UI/Contracts).
        /// </summary>
        void RefreshZoneStatus()
        {
            if (_zoneStatusPlate == null) return;
            string text = null, houseText = null;
            bool houseButton = false;
            if (Current == WorldZone.Farm)
            {
                // Farming owns the clock (active gameplay time); the UI only prints Farming.State / SecondsLeft.
                var sb = new System.Text.StringBuilder("FARM  SEEDS S" + WorldSave.GetQty("seed_sprout") + " F" + WorldSave.GetQty("seed_fiber"));
                for (int i = 0; i < Farming.PlotCount; i++)
                {
                    var st = Farming.State(i);
                    sb.Append("\n").Append(i + 1).Append(" ");
                    if (st == PlotState.Empty) { sb.Append("EMPTY"); continue; }
                    var crop = Farming.Crop(Farming.CropAt(i));
                    sb.Append((string.IsNullOrEmpty(crop.displayName) ? crop.cropId : crop.displayName).ToUpperInvariant()).Append(" ");
                    sb.Append(st == PlotState.Ready ? "READY" : Mathf.CeilToInt(Farming.SecondsLeft(i)) + "S");
                }
                text = sb.ToString();
            }
            else if (Current == WorldZone.Housing)
            {
                // Housing owns select / build / rest. Cost and stock are shown so BUILD's state is readable.
                // Cost comes from the build card (BuildMode NHA GO); the coin price is only the optional XONG NGAY.
                int wood = WorldSave.GetQty("wood"), ore = WorldSave.GetQty("ore");
                var def = BuildMode.Defs[0];
                bool building = BuildMode.HouseBuildProgress(out float progress);
                bool chips = !Housing.HasHouse && !building;
                // House done: only the XAY DUNG button stays (decor / land); the plate itself is hidden.
                houseText = Housing.HasHouse ? "" : building ? "DANG XAY " + Mathf.RoundToInt(progress * 100f) + "%" : def.name;
                if (_housePanel != null && _housePanel.enabled == Housing.HasHouse) _housePanel.enabled = !Housing.HasHouse;
                SetHouseChip(_houseWoodIcon, _houseWood, chips, wood, def.wood);
                SetHouseChip(_houseOreIcon, _houseOre, chips, ore, def.ore);
                houseButton = true;
                if (_houseButton != null)
                {
                    var gate = WorldServices.UiGate();
                    _houseButtonLabel.text = BuildMode.IsOpen ? "DONG" : "XAY DUNG";
                    _houseButton.interactable = gate == Deny.None;
                }
            }
            bool show = text != null;
            if (_zoneStatusPlate.activeSelf != show) _zoneStatusPlate.SetActive(show);
            if (show && _zoneStatus.text != text) _zoneStatus.text = text;
            // Build mode has its own XONG button and cost chips, so the plate steps aside while it is open.
            bool showHouse = houseText != null && _houseStatusPlate != null && !BuildMode.IsOpen;
            if (_houseStatusPlate != null && _houseStatusPlate.activeSelf != showHouse) _houseStatusPlate.SetActive(showHouse);
            if (showHouse && _houseStatus.text != houseText) _houseStatus.text = houseText;
            if (_houseButton != null && _houseButton.gameObject.activeSelf != houseButton) _houseButton.gameObject.SetActive(houseButton);
        }

        Button _houseButton;
        Text _houseButtonLabel;

        void BuildHouseButton(Transform plate)
        {
            var size = UiKit.IsMobile ? new Vector2(96f, 26f) : new Vector2(72f, 18f);
            _houseButton = UiKit.Button(plate, "HouseAction", "Positive", "XAY DUNG", size, () =>
            {
                if (BuildMode.IsOpen) BuildMode.Close(); else BuildMode.Open();
            });
            UiKit.Place(_houseButton, new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), size);
            UiKit.Reach(_houseButton);
            _houseButtonLabel = _houseButton.GetComponentInChildren<Text>();
            if (!UiKit.IsMobile)
            {
                // PC: the B shortcut on the button itself (same keycap art as the prompt plate).
                var key = UiKit.Panel(_houseButton.transform, "Key", "UI_KeyCap");
                UiKit.Place(key, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-3f, 0f), new Vector2(11f, 11f));
                var k = UiKit.Label(key.transform, "K", "B", 8, Pal.GoldL, TextAnchor.MiddleCenter, false);
                UiKit.Fill(k);
            }
            _houseButton.gameObject.SetActive(false);
        }

        static Image HouseChip(Transform plate, string icon, float x, out Text label)
        {
            var img = UiKit.Rect(plate, icon + "Icon").gameObject.AddComponent<Image>();
            img.sprite = Resources.Load<Sprite>("World/HomeDistrict/Build/BD_Icon_" + icon);
            img.preserveAspect = true;
            img.raycastTarget = false;
            UiKit.Place(img, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -16f), new Vector2(8f, 8f));
            label = UiKit.Label(plate, icon, "", 8, Pal.TextW, TextAnchor.MiddleLeft);
            UiKit.Place(label, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x + 10f, -15f), new Vector2(36f, 10f));
            label.horizontalOverflow = HorizontalWrapMode.Overflow;
            return img;
        }

        static void SetHouseChip(Image icon, Text label, bool show, int have, int need)
        {
            if (icon == null || label == null) return;
            if (icon.enabled != show) icon.enabled = show;
            if (label.enabled != show) label.enabled = show;
            if (!show) return;
            // have/need, capped so big stocks still fit ("99+/3").
            string text = (have > 99 ? "99+" : have.ToString()) + "/" + need;
            if (label.text != text) label.text = text;
            var color = have < need ? new Color(0.88f, 0.28f, 0.28f) : Pal.TextW;
            if (label.color != color) label.color = color;
        }

        public void ToggleBag()
        {
            if (_bagPanel == null) return;
            bool show = !_bagPanel.activeSelf;
            if (show)
            {
                // One window at a time: the service and board windows sit on other canvases.
                WorldServices.Instance?.HidePanel();
                if (_board != null) _board.SetActive(false);
            }
            _bagPanel.SetActive(show);
            if (show) RefreshBag();
        }

        public void CloseBag()
        {
            if (_bagPanel != null) _bagPanel.SetActive(false);
        }

        void RefreshBag()
        {
            if (_bagWin == null) return;
            _bagWin.SetTitle(_bagTab == 1 ? "HERO" : _bagTab == 2 ? "SKILLS" : "BAG");
            _bagWin.Clear();
            var body = _bagWin.body;
            string[] tabs = { "ITEMS", "HERO", "SKILLS" };
            float tx = 0f;
            for (int i = 0; i < tabs.Length; i++)
            {
                int captured = i;
                var tab = UiKit.Tab(body, tabs[i], i == _bagTab, () => { _bagTab = captured; RefreshBag(); });
                float w = Loc.MaxLength(tabs[i]) * 8f + 14f;
                UiKit.Place(tab, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(tx, 0f), new Vector2(w, 16f));
                tx += w + 2f;
            }
            if (_bagTab == 1) BagHero(body);
            else if (_bagTab == 2) BagSkills(body);
            else BagItems(body);
        }

        /// <summary>Skill tree (P22 / L23) for the active class: 3 tiers, rank 0..3, points, free reset. Rules live in SkillTree.</summary>
        Deny _bagGateSeen;
        float _bagGateTick;

        void TickBagGate()
        {
            if (_bagPanel == null || !_bagPanel.activeSelf || _bagTab == 1) return;
            var gate = WorldServices.UiGate();
            _bagGateTick -= Time.unscaledDeltaTime;
            if (gate == _bagGateSeen && (gate == Deny.None || _bagGateTick > 0f)) return;
            _bagGateSeen = gate;
            _bagGateTick = 1f;
            RefreshBag();
        }

        void BagSkills(RectTransform body)
        {
            string cls = SkillTree.ActiveClassId();
            if (string.IsNullOrEmpty(cls))
            {
                Note(body, "No active class.", -24f, Pal.TextMute);
                return;
            }
            var gate = WorldServices.UiGate();
            int points = SkillTree.Budget(cls);
            Line(body, cls.ToUpperInvariant() + "  LV " + WorldSave.Level, new Vector2(0f, -22f), Pal.GoldL);
            Line(body, "POINTS  " + points, new Vector2(0f, -34f), points > 0 ? Pal.GreenL : Pal.TextMute);
            bool anyRank = false;
            for (int tier = 1; tier <= 3; tier++)
            {
                var node = SkillTree.Find(cls, tier);
                if (string.IsNullOrEmpty(node.id)) continue;
                int rank = SkillTree.Rank(node.id);
                anyRank |= rank > 0;
                bool open = SkillTree.TierOpen(cls, tier);
                var row = UiKit.Rect(body, "Tier" + tier);
                row.gameObject.AddComponent<UiLayout>().Both(UiLayout.Spec.Stretch(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -48f - (tier - 1) * 40f), new Vector2(0f, 36f)));
                UiKit.Fill(UiKit.Row(row, "Bg", open ? (rank > 0 ? "Selected" : "") : "Disabled", Vector2.zero));
                Line(row, "T" + tier + "  " + node.name.ToUpperInvariant(), new Vector2(8f, -4f), open ? Pal.GoldL : Pal.TextMute);
                string effect = node.effect == NodeEffect.Duration ? "DUR" : "DMG";
                for (int k = 0; k < SkillTree.MaxRank; k++)
                    UiKit.Icon(row, k < rank ? "UI_Icon_Star" : "UI_Icon_StarEmpty", new Vector2(0f, 1f), new Vector2(13f + k * 11f, -21f), 10f);
                Line(row, open ? "+" + (node.percentPerRank * rank).ToString("0.#") + "% " + effect + "  +" + node.percentPerRank.ToString("0.#") + "%/RANK" : TierLockText(tier),
                    new Vector2(44f, -16f), open ? Pal.TextW : Pal.TextMute);
                int t = tier;
                var up = UiKit.Button(row, "Up", "Primary", "+", new Vector2(22f, 18f), () => { SkillTree.Upgrade(cls, t); RefreshBag(); });
                up.interactable = open && rank < SkillTree.MaxRank && points > 0 && gate == Deny.None;
                Corner(up, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-6f, 0f));
            }
            var reset = UiKit.Button(body, "Reset", "Danger", "RESET (FREE)", new Vector2(110f, 20f), () => { SkillTree.ResetActive(); RefreshBag(); });
            reset.interactable = anyRank && gate == Deny.None;
            Corner(reset, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -172f));
            string status = gate != Deny.None ? GameplayClock.DenyText(gate) : WorldServices.LastResult;
            Line(body, string.IsNullOrEmpty(status) ? "" : status.ToUpperInvariant(), new Vector2(0f, -198f), gate != Deny.None ? Pal.RedL : Pal.TextMute);
        }

        /// <summary>Display copy of SkillTree.TierOpen's rule, shown on a locked tier.</summary>
        static string TierLockText(int tier)
        {
            switch (tier)
            {
                case 1: return "NEEDS LV 2";
                case 2: return "LV 5 + T1 RANK 1";
                default: return "LV 10 + T2 RANK 1";
            }
        }

        void BagItems(RectTransform body)
        {
            var grid = UiKit.Rect(body, "Grid");
            grid.gameObject.AddComponent<UiLayout>()
                .Portrait(UiLayout.Spec.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(240f, 96f)))
                .Landscape(UiLayout.Spec.At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -18f), new Vector2(240f, 96f)));
            var inset = UiKit.Panel(grid, "Inset", "UI_Panel_Inset");
            UiKit.Fill(inset);
            int n = 0;
            ItemDef selected = default;
            for (int i = 0; i < WorldContent.Items.Length; i++)
            {
                var item = WorldContent.Items[i];
                if (item.kind == ItemKind.Currency) continue;
                int qty = WorldSave.GetQty(item.id);
                int loose = item.kind == ItemKind.Equipment && Gear.IsEquipped(item.id) ? qty - 1 : qty;
                // Worn gear leaves the bag. Unequip puts that copy back.
                if (loose <= 0)
                {
                    if (_bagSel == item.id) _bagSel = "";
                    continue;
                }
                if (string.IsNullOrEmpty(_bagSel)) _bagSel = item.id;
                bool sel = item.id == _bagSel;
                if (sel) selected = item;
                string id = item.id;
                var slot = UiKit.Slot(grid, "Slot_" + id, () => { _bagSel = id; RefreshBag(); });
                slot.Set(UiKit.ItemIcon(id), loose, sel ? "Selected" : "");
                UiKit.Place(slot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(4f + (n % 10) * 23f, -4f - (n / 10) * 23f), new Vector2(22f, 22f));
                n++;
            }
            for (; n < 40; n++)
            {
                var slot = UiKit.Slot(grid, "Empty" + n);
                slot.Set(null, 0, n >= 25 ? "Disabled" : "");
                UiKit.Place(slot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(4f + (n % 10) * 23f, -4f - (n / 10) * 23f), new Vector2(22f, 22f));
            }
            var coins = UiKit.Coins(body, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, 0f));
            coins.text = WorldSave.Coins.ToString();

            var detail = UiKit.Panel(body, "Detail", "UI_Panel_Parchment");
            detail.gameObject.AddComponent<UiLayout>()
                .Portrait(UiLayout.Spec.Stretch(new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 0f), new Vector2(0f, 150f)))
                .Landscape(UiLayout.Spec.Stretch(new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(250f, -10f), new Vector2(166f, -20f)));
            if (string.IsNullOrEmpty(selected.id))
            {
                Note(detail.transform, "Empty bag. Gather, farm or buy items.", -12f, Pal.TextDark);
                return;
            }
            int have = WorldSave.GetQty(selected.id);
            if (selected.kind == ItemKind.Equipment && Gear.IsEquipped(selected.id)) have--;
            var big = UiKit.Panel(detail.transform, "Frame", "UI_Equip_Slot_Selected");
            UiKit.Place(big, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -8f), new Vector2(26f, 26f));
            UiKit.Icon(big.transform, UiKit.ItemIcon(selected.id), UiKit.Center, Vector2.zero);
            Line(detail.transform, selected.displayName.ToUpperInvariant(), new Vector2(40f, -10f), Pal.TextDark);
            Line(detail.transform, KindName(selected.kind), new Vector2(40f, -22f), Pal.WoodM);
            Line(detail.transform, "QTY  " + have, new Vector2(8f, -42f), Pal.TextDark);
            Line(detail.transform, selected.sell > 0 ? "SELL " + selected.sell + " COINS" : selected.buy > 0 ? "BUY " + selected.buy + " COINS" : "NOT FOR SALE", new Vector2(8f, -54f), Pal.TextDark);
            if (selected.id == Pets.EggId)
            {
                string why = Pets.CanHatch();
                var hatch = UiKit.Button(detail.transform, "Hatch", why.Length == 0 ? "Primary" : "Secondary", "HATCH", new Vector2(100f, 20f), PetHud.ConfirmHatch);
                hatch.interactable = why.Length == 0;
                Corner(hatch, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f));
                if (why.Length > 0) Line(detail.transform, why.ToUpperInvariant(), new Vector2(8f, -66f), Pal.RedL);
            }
            else if (selected.id == Pets.ShardId)
            {
                string why = Pets.CanCombine();
                var combine = UiKit.Button(detail.transform, "Combine", why.Length == 0 ? "Primary" : "Secondary", "COMBINE", new Vector2(100f, 20f), () =>
                    PetHud.PlayCombine(RefreshBag));
                combine.interactable = why.Length == 0;
                Corner(combine, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f));
                if (why.Length > 0) Line(detail.transform, why.ToUpperInvariant(), new Vector2(8f, -66f), Pal.RedL);
            }
            else if (selected.kind == ItemKind.Equipment)
            {
                // Equip / unequip and the gem socket go through Gear; the UI never writes the save.
                bool on = Gear.IsEquipped(selected.id);
                var gate = WorldServices.UiGate();
                string slot = selected.equipSlot;
                string id = selected.id;
                var equip = UiKit.Button(detail.transform, "Equip", on ? "Secondary" : "Primary", on ? "UNEQUIP" : "EQUIP", new Vector2(100f, 20f), () =>
                {
                    if (on) Gear.Unequip(slot); else Gear.Equip(id);
                    RefreshBag();
                }, "UI_Icon_Equipment");
                equip.interactable = have > 0 && gate == Deny.None;
                Corner(equip, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f));
                string socket = Gear.Socket(id);
                Line(detail.transform, WorldServices.GearStatText(id), new Vector2(8f, -66f), Pal.PurpleD);
                Line(detail.transform, "SOCKET  " + (string.IsNullOrEmpty(socket) ? "EMPTY" : WorldContent.FindItem(socket).displayName.ToUpperInvariant()), new Vector2(8f, -78f), Pal.TextDark);
                GemButtons(detail.transform, id, socket, gate);
                // The prompt plate is hidden behind this window, so a refused action shows its reason here.
                if (gate != Deny.None) Line(detail.transform, GameplayClock.DenyText(gate).ToUpperInvariant(), new Vector2(8f, -90f), Pal.RedL);
                else if (Outcome.Last != Deny.None && !string.IsNullOrEmpty(WorldServices.LastResult))
                    Line(detail.transform, WorldServices.LastResult.ToUpperInvariant(), new Vector2(8f, -90f), Pal.RedL);
            }
            else if (selected.kind == ItemKind.Gem)
            {
                Line(detail.transform, WorldServices.GearStatText(selected.id), new Vector2(8f, -66f), Pal.PurpleD);
                Line(detail.transform, "OWN " + Gear.GemOwned(selected.id) + "/" + Gear.GemTypeLimit + "  SOCKET IN GEAR", new Vector2(8f, -78f), Pal.TextDark);
            }
        }

        /// <summary>Insert / replace with each loose gem the bag holds, or remove the socketed one.</summary>
        void GemButtons(Transform detail, string equipId, string socket, Deny gate)
        {
            float x = 8f;
            foreach (var g in Gear.Gems)
            {
                if (WorldSave.GetQty(g.id) <= 0 || g.id == socket) continue;
                string gem = g.id;
                string label = Loc.T(WorldContent.FindItem(gem).displayName).ToUpperInvariant();
                var b = UiKit.Button(detail, "Gem_" + gem, "Secondary", (label.Length > 5 ? label.Substring(0, 5) : label).TrimEnd(), new Vector2(52f, 16f), () =>
                {
                    if (string.IsNullOrEmpty(socket)) Gear.InsertGem(equipId, gem); else Gear.ReplaceGem(equipId, gem);
                    RefreshBag();
                });
                b.interactable = gate == Deny.None;
                Corner(b, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x, 32f));
                x += 54f;
            }
            if (!string.IsNullOrEmpty(socket))
            {
                var r = UiKit.Button(detail, "GemRemove", "Danger", "REMOVE", new Vector2(56f, 16f), () => { Gear.RemoveGem(equipId); RefreshBag(); });
                r.interactable = gate == Deny.None;
                Corner(r, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(x, 32f));
            }
        }

        void BagHero(RectTransform body)
        {
            var hero = Hero();
            var health = hero != null ? hero.GetComponent<Health>() : null;
            var cls = hero != null ? hero.GetComponent<ClassCombat>() : null;
            string className = cls != null && !string.IsNullOrEmpty(cls.IconClassId) ? cls.IconClassId.ToUpperInvariant() : "AREN";
            var frame = UiKit.Panel(body, "Portrait", "UI_PortraitFrame");
            UiKit.Place(frame, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -20f), new Vector2(38f, 38f));
            var back = UiKit.Panel(frame.transform, "Back", "UI_PortraitBack");
            back.transform.SetAsFirstSibling();
            UiKit.Place(back, UiKit.Center, UiKit.Center, Vector2.zero, new Vector2(32f, 32f));
            // Class emblem: the class's own basic-attack icon, same as the HUD hero block.
            var icons = ClassUiIcons.Active;
            string classId = cls != null ? cls.IconClassId : null;
            var emblem = UiKit.Icon(frame.transform, "UI_Icon_Attack", UiKit.Center, Vector2.zero, 26f);
            emblem.preserveAspect = true;
            Sprite classIcon = icons != null && !string.IsNullOrEmpty(classId) ? icons.Skill(classId, SkillId.Sword) : null;
            if (classIcon != null) emblem.sprite = classIcon;
            var badge = UiKit.Panel(frame.transform, "Level", "UI_LevelBadge");
            UiKit.Place(badge, new Vector2(1f, 0f), UiKit.Center, new Vector2(-2f, 2f), new Vector2(16f, 16f));
            var lv = UiKit.Label(badge.transform, "Lv", WorldSave.Level.ToString(), 8, Pal.GoldL, TextAnchor.MiddleCenter);
            UiKit.Fill(lv);
            Line(body, className, new Vector2(46f, -24f), Pal.GoldL);
            Line(body, "LV " + WorldSave.Level, new Vector2(46f, -38f), Pal.TextBlue);
            int step = Mathf.Max(1, WorldCatalog.ExpPerLevel);
            var exp = UiKit.Bar(body, "Exp", "EXP", 150f, true, false);
            UiKit.Place(exp, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(46f, -52f), new Vector2(150f, 6f));
            exp.Set(WorldSave.Exp % step, step, false);
            Line(body, "EXP " + (WorldSave.Exp % step) + "/" + step + "  NEXT " + WorldSave.ExpToNext, new Vector2(46f, -62f), Pal.TextMute);
            var hp = UiKit.Bar(body, "Hp", "HP", 200f);
            UiKit.Place(hp, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -82f), new Vector2(200f, 12f));
            hp.Set(health != null ? health.CurrentHp : 0, health != null ? health.MaxHp : 1);
            Line(body, "COINS  " + WorldSave.Coins, new Vector2(0f, -102f), Pal.GoldL);

            // Worn gear is hidden from the item grid. Take it off here and it returns to the bag.
            var equipRoot = UiKit.Rect(body, "Equipment");
            equipRoot.gameObject.AddComponent<UiLayout>()
                .Portrait(UiLayout.Spec.Stretch(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -116f), new Vector2(0f, 136f)))
                .Landscape(UiLayout.Spec.Stretch(new Vector2(0.5f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(0f, 136f)));
            Line(equipRoot, "EQUIPMENT", Vector2.zero, Pal.GoldL);
            string[] slotKeys = { "weapon", "armor", "accessory" };
            string[] ghosts = { "UI_Equip_Weapon_Ghost", "UI_Equip_Armor_Ghost", "UI_Equip_Ring_Ghost" };
            for (int i = 0; i < slotKeys.Length; i++)
                EquipRow(equipRoot, slotKeys[i], WorldServices.SlotLabel(slotKeys[i]), ghosts[i], -14f - i * 36f);
            Line(equipRoot, "BONUS +" + Gear.TotalPower + " PWR +" + Gear.TotalDef + " DEF +" + Gear.BonusMaxHp + " HP",
                new Vector2(0f, -124f), Pal.TextBlue).GetComponent<UiLayout>().Both(UiLayout.Spec.At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -124f), new Vector2(240f, 10f)));

            // One-time rewards (Charm for the first Level 1 clear, starter seed): state comes from PendingRewards.
            var rewards = UiKit.Rect(body, "Rewards");
            rewards.gameObject.AddComponent<UiLayout>()
                .Portrait(UiLayout.Spec.Stretch(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -258f), new Vector2(0f, 40f)))
                .Landscape(UiLayout.Spec.Stretch(new Vector2(0f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, 1f), new Vector2(0f, -118f), new Vector2(-8f, 40f)));
            Line(rewards, "REWARDS", Vector2.zero, Pal.GoldL);
            Line(rewards, "L1 CHARM  " + EntitlementText(PendingRewards.Get(PendingRewards.CharmL1)), new Vector2(0f, -12f), Pal.TextW);
            Line(rewards, "STARTER SEED  " + EntitlementText(PendingRewards.Get(PendingRewards.StarterSeed)), new Vector2(0f, -24f), Pal.TextW);
        }

        static string EntitlementText(Entitlement e)
        {
            switch (e)
            {
                case Entitlement.Pending: return "PENDING";
                case Entitlement.Granted: return "GRANTED";
                case Entitlement.Unverified: return "UNVERIFIED";
                default: return "NOT YET";
            }
        }

        /// <summary>One equipment slot: icon, slot + where the item is, item name, stats and socket.</summary>
        void EquipRow(Transform parent, string slot, string slotName, string ghost, float topPx)
        {
            string equipped = WorldSave.Equipped(slot);
            ItemDef item = default;
            for (int k = 0; k < WorldContent.Items.Length; k++)
            {
                var it = WorldContent.Items[k];
                if (it.equipSlot != slot) continue;
                if (string.IsNullOrEmpty(equipped) || it.id == equipped) { item = it; break; }
            }
            bool hasItem = !string.IsNullOrEmpty(item.id);
            bool on = hasItem && Gear.IsEquipped(item.id);
            var frame = UiKit.Panel(parent, "Equip_" + slot, !hasItem ? "UI_Equip_Slot_Locked" : on ? "UI_Equip_Slot_Selected" : "UI_Equip_Slot");
            UiKit.Place(frame, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, topPx), new Vector2(30f, 30f));
            UiKit.Icon(frame.transform, on ? UiKit.ItemIcon(item.id) : ghost, UiKit.Center, Vector2.zero);
            string where = !hasItem ? "EMPTY"
                : on ? "EQUIPPED"
                : WorldSave.GetQty(item.id) > 0 ? "IN BAG"
                : WorldSave.StoreQty(item.id) > 0 ? "IN STORAGE"
                : "NOT OWNED";
            Line(parent, slotName + "  " + where, new Vector2(36f, topPx - 1f), on ? Pal.GoldL : Pal.TextMute);
            Line(parent, hasItem ? item.displayName.ToUpperInvariant() : "NO ITEM", new Vector2(36f, topPx - 11f), hasItem ? Pal.TextW : Pal.TextMute);
            if (!hasItem) return;
            string socket = Gear.Socket(item.id);
            string gemName = string.IsNullOrEmpty(socket) ? "" : "  " + WorldContent.FindItem(socket).displayName.ToUpperInvariant();
            Line(parent, WorldServices.GearStatText(item.id) + gemName, new Vector2(36f, topPx - 21f), Pal.PurpleL);
            if (!on) return;
            string worn = slot;
            var off = UiKit.Button(parent, "Off_" + slot, "Secondary", "UNEQUIP", new Vector2(58f, 14f), () =>
            {
                Gear.Unequip(worn);
                RefreshBag();
            });
            UiKit.Place(off, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-4f, topPx - 8f), new Vector2(58f, 14f));
        }

        static string KindName(ItemKind kind)
        {
            switch (kind)
            {
                case ItemKind.Resource: return "RESOURCE";
                case ItemKind.Consumable: return "CONSUMABLE";
                case ItemKind.Equipment: return "EQUIPMENT";
                case ItemKind.Quest: return "QUEST ITEM";
                case ItemKind.Gem: return "GEM";
                default: return kind.ToString().ToUpperInvariant();
            }
        }

        static Text Line(Transform parent, string text, Vector2 posPx, Color color)
        {
            var t = UiKit.Label(parent, "Text", text, 8, color, TextAnchor.MiddleLeft, color != Pal.TextDark && color != Pal.WoodM);
            UiKit.Place(t, new Vector2(0f, 1f), new Vector2(0f, 1f), posPx, new Vector2(200f, 10f));
            return t;
        }

        static void Note(Transform parent, string text, float topPx, Color color)
        {
            var t = UiKit.Label(parent, "Note", text, 8, color, TextAnchor.UpperLeft, false);
            UiKit.Place(t, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, topPx), new Vector2(150f, 22f));
        }

        static void Corner(Component c, Vector2 anchor, Vector2 pivot, Vector2 posPx)
        {
            var layout = c.GetComponent<UiLayout>();
            layout.Both(UiLayout.Spec.At(anchor, pivot, posPx, layout.landscape.size));
        }

        static Text NewText(Transform parent, string name, Vector2 anchor, Vector2 size, int font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.sizeDelta = size;
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = font;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            LocText.Attach(text);
            return text;
        }

        static Button NewButton(Transform parent, string name, Vector2 anchor, string label, UnityEngine.Events.UnityAction click)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(220f, 40f);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.45f, 0.16f, 0.16f, 1f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(click);
            UiKit.Dress(button, "Primary");
            var text = NewText(go.transform, "Label", new Vector2(0.5f, 0.5f), new Vector2(200f, 36f), 16);
            text.text = label;
            return button;
        }

        WorldInteractable Nearest(Transform hero)
        {
            if (hero == null || _root == null || !_root.activeSelf) return null;
            if (FarmMode.Instance != null && FarmMode.Instance.FarmPrompt(out var farm) && farm.InRange(hero.position))
                return farm;
            WorldInteractable best = null;
            float bestD = float.MaxValue;
            var all = _root.GetComponentsInChildren<WorldInteractable>(false);
            for (int i = 0; i < all.Length; i++)
            {
                if (!all[i].isActiveAndEnabled || !all[i].Seekable || !all[i].InRange(hero.position)) continue;
                float d = ((Vector2)all[i].transform.position - (Vector2)hero.position).sqrMagnitude;
                if (d < bestD)
                {
                    bestD = d;
                    best = all[i];
                }
            }
            return best;
        }

        static Transform Hero()
        {
            var go = GameObject.FindGameObjectWithTag("Player");
            return go != null ? go.transform : null;
        }

        static void PlaceHero(Vector2 spawn)
        {
            var hero = Hero();
            if (hero == null) return;
            var rb = hero.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.position = spawn;
            }
            hero.position = spawn;
        }

        static ZoneLayout Layout(WorldZone zone)
        {
            if (zone == WorldZone.Field && Instance != null && WorldFoundation.TryFieldLayout(Instance.FieldId, out var fieldLayout))
                return fieldLayout;
            if (WorldFoundation.TryLayout(zone, out var extra)) return extra;
            if (zone == WorldZone.Training) return WorldCatalog.Training;
            if (zone == WorldZone.Resource) return WorldCatalog.Resource;
            if (zone == WorldZone.Farm) return WorldCatalog.Farm;
            if (zone == WorldZone.Boss) return WorldCatalog.Boss;
            return OriginalVillage.Extend(WorldCatalog.Hub);
        }

        string ZoneHint()
        {
            if (Current == WorldZone.Field)
            {
                var map = WorldFoundation.FindField(FieldId);
                return string.IsNullOrEmpty(map.name) ? "Field" : map.name;
            }
            if (Current == WorldZone.Housing) return HousingHint();
            string site = WorldSites.Hint(Current);
            if (!string.IsNullOrEmpty(site)) return site;
            if (Current == WorldZone.Hub)
            {
                if (UiKit.IsMobile)
                    return OriginalVillage.Enabled
                        ? "Signposts lead out of town. Tap the round button to talk."
                        : "Village street. Gates are north. Tap the round button to talk.";
                return OriginalVillage.Enabled
                    ? "Signposts lead out of town. E talks. I opens the bag."
                    : "Village street. Gates are north. E talks. I opens the bag.";
            }
            if (Current == WorldZone.Training) return "Training. EXP " + WorldSave.Exp + "  Lv " + WorldSave.Level;
            if (Current == WorldZone.Resource) return "Ore " + WorldSave.GetQty("ore") + "   Wood " + WorldSave.GetQty("wood");
            if (Current == WorldZone.Farm) return "Sprouts " + WorldSave.GetQty(WorldCatalog.TestCrop.itemId);
            if (Current == WorldZone.Boss) return Boss != null ? Boss.Status : "Boss ridge";
            return "";
        }

        static string InteractName()
        {
            string key = WorldSave.InteractBinding;
            if (string.IsNullOrEmpty(key)) return "E";
            return key.ToUpperInvariant();
        }

        static bool InteractPressed(Keyboard kb)
        {
            if (kb == null) return false;
            string key = WorldSave.InteractBinding;
            if (key == "f") return kb.fKey.wasPressedThisFrame;
            if (key == "g") return kb.gKey.wasPressedThisFrame;
            return kb.eKey.wasPressedThisFrame;
        }

        /// <summary>Farm mode hides the hint plate while it is open.</summary>
        public void SetPromptVisible(bool show)
        {
            if (_prompt != null && _prompt.transform.parent != null)
                _prompt.transform.parent.gameObject.SetActive(show && Current != WorldZone.None);
        }

        void SetPrompt(string text)
        {
            if (_prompt != null) _prompt.text = text;
        }
    }
}
