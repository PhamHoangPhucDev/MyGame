using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>
    /// Dungeon stage select. Same canvas scale as the outdoor world map (640x360, shown at 3x on 1920x1080).
    /// </summary>
    public class DungeonSelectPanel : MonoBehaviour
    {
        struct Pin
        {
            public Image halo, icon, check, badge;
            public Text number;
        }

        static readonly Vector2[] PinAt =
        {
            new Vector2(54f, 262f), new Vector2(146f, 181f), new Vector2(228f, 236f),
            new Vector2(318f, 171f), new Vector2(352f, 120f)
        };

        static readonly Color Gold = Hex(0xF2D84A);
        static readonly Color Cyan = Hex(0x5ED0E0);
        static readonly Color Green = Hex(0x7ED67E);
        static readonly Color White = Hex(0xF4F4F0);
        static readonly Color Mute = Hex(0x8A909C);
        static readonly Color Cream = Hex(0xF6EFE2);
        static readonly Color Ink = Hex(0x141018);

        static DungeonSelectPanel _one;
        static int _seen = -1;

        Pin[] _pins;
        Image[] _fog;
        float[] _fade;
        Image _board;
        Image[] _tabBg;
        Text[] _tabLabel;
        Text _status, _name, _kind, _level, _waves, _goal, _record, _unlock, _guardLine, _travelText;
        Image _kindIcon;
        Image[] _stars;
        Image[] _rewardIcon;
        Text[] _rewardText;
        Button _travel;
        int _dungeon;
        int _sel;

        public static bool IsOpen => _one != null && _one.gameObject.activeSelf;

        public static void Open()
        {
            if (DungeonRun.Active) return;
            var panel = Ensure();
            panel.gameObject.SetActive(true);
            panel._dungeon = 0;
            panel._sel = DungeonProgress.HighestUnlocked(0) - 1;
            panel.Apply(true);
        }

        public static void Close()
        {
            if (_one != null) _one.gameObject.SetActive(false);
        }

        public static void Travel()
        {
            if (_one != null) _one.Go();
        }

        public static void Remember(int stage) => _seen = stage;

        public void Select(int index)
        {
            if (index < 0 || index > 4) return;
            _sel = index;
            Apply(false);
        }

        public void TickFade(float dt)
        {
            if (_fade == null) return;
            for (int i = 0; i < _fade.Length; i++)
            {
                if (_fade[i] < 0f) continue;
                _fade[i] -= dt;
                var c = _fog[i].color;
                c.a = Mathf.Clamp01(_fade[i] / 0.5f);
                _fog[i].color = c;
                if (_fade[i] <= 0f)
                {
                    _fade[i] = -1f;
                    _fog[i].gameObject.SetActive(false);
                }
            }
        }

        public string PinSprite(int stage) => _pins[stage - 1].icon.sprite != null ? _pins[stage - 1].icon.sprite.name : "missing";
        public bool HaloOn(int index) => _pins[index].halo.gameObject.activeSelf;
        public bool CheckOn(int index) => _pins[index].check.gameObject.activeSelf;

        public bool FogOn(int stage)
        {
            if (stage < 2 || stage > 5) return false;
            var fog = _fog[stage - 2];
            return fog.gameObject.activeSelf && fog.color.a > 0.5f;
        }

        public bool TravelOn => _travel.interactable;
        public string TravelLabel => _travelText.text;

        static DungeonSelectPanel Ensure()
        {
            if (_one != null) return _one;
            var go = new GameObject("DungeonSelectCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 460;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640f, 360f);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 16f;
            go.AddComponent<GraphicRaycaster>();
            _one = go.AddComponent<DungeonSelectPanel>();
            _one.Build();
            go.SetActive(false);
            return _one;
        }

        void OnDestroy()
        {
            if (_one == this) _one = null;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && gameObject.activeSelf)
            {
                if (kb.aKey.wasPressedThisFrame || kb.leftArrowKey.wasPressedThisFrame) Select(Mathf.Max(0, _sel - 1));
                if (kb.dKey.wasPressedThisFrame || kb.rightArrowKey.wasPressedThisFrame) Select(Mathf.Min(4, _sel + 1));
                if (kb.enterKey.wasPressedThisFrame || kb.eKey.wasPressedThisFrame) Go();
            }
            TickFade(Time.unscaledDeltaTime);
            if (_pins == null) return;
            var halo = _pins[_sel].halo;
            if (!halo.gameObject.activeSelf) return;
            var c = halo.color;
            c.a = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 4f));
            halo.color = c;
        }

        void Build()
        {
            UiKit.Ensure();
            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;
            var backdrop = New("Backdrop", root);
            Stretch(backdrop);
            var bg = backdrop.gameObject.AddComponent<Image>();
            bg.color = new Color(0x0E / 255f, 0x10 / 255f, 0x14 / 255f, 170f / 255f);
            bg.raycastTarget = true;

            var window = Sprite(root, "Window", "UI_Panel_Main", true);
            Corner(window.rectTransform, 6f, 8f, 628f, 350f);
            var ribbon = Sprite(root, "Ribbon", "UI_Header_Ribbon", true);
            Corner(ribbon.rectTransform, 240f, 0f, 160f, 18f);
            Outline(Label(ribbon.rectTransform, "Title", "BẢN ĐỒ PHÓ BẢN", 0f, 2f, 160f, 14f, White, 8, TextAnchor.UpperCenter));
            _tabBg = new Image[3];
            _tabLabel = new Text[3];
            for (int i = 0; i < 3; i++)
            {
                int captured = i;
                var tab = Sprite(root, "Tab" + i, "UI_Button_Secondary_Normal", true);
                Corner(tab.rectTransform, i == 0 ? 17f : i == 1 ? 149f : 281f, 19f, 128f, 18f);
                var btn = tab.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.targetGraphic = tab;
                btn.onClick.AddListener(() => ChooseDungeon(captured));
                var label = Label(tab.rectTransform, "Label", DungeonCatalog.Get(i).name, 0f, 3f, 100f, 14f, White, 8, TextAnchor.UpperCenter);
                var lockPin = Sprite(tab.rectTransform, "Lock", "UI_Icon_Lock", false);
                Corner(lockPin.rectTransform, 108f, 2f, 14f, 14f);
                lockPin.raycastTarget = false;
                _tabBg[i] = tab;
                _tabLabel[i] = label;
            }
            CloseButton(root);

            var frame = Sprite(root, "BoardFrame", "UI_Panel_Inset", true);
            Corner(frame.rectTransform, 16f, 40f, 412f, 312f);

            var map = New("Map", root);
            Corner(map, 18f, 42f, 408f, 308f);
            _board = Sprite(map, "Art", "UI_Dungeon_Board_GoblinCamp", false);
            Stretch(_board.rectTransform);
            _fog = new Image[4];
            _fade = new float[4];
            for (int i = 0; i < 4; i++)
            {
                _fog[i] = Sprite(map, "Fog" + (i + 2), "UI_Dungeon_Fog_GoblinCamp_Stage" + (i + 2), false);
                Stretch(_fog[i].rectTransform);
                _fog[i].raycastTarget = false;
                _fade[i] = -1f;
            }

            _pins = new Pin[5];
            for (int i = 0; i < 5; i++)
            {
                int captured = i;
                var host = New("Pin" + i, map);
                Mid(host, PinAt[i].x + 0.5f, PinAt[i].y + 0.5f, 13f, 13f);
                var halo = Sprite(host, "Halo", "UI_WorldMap_Pin_SelectHalo", false);
                var hrt = halo.rectTransform;
                hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 0.5f);
                hrt.pivot = new Vector2(0.5f, 0.5f);
                hrt.anchoredPosition = Vector2.zero;
                hrt.sizeDelta = new Vector2(25f, 25f);
                halo.raycastTarget = false;
                var icon = Sprite(host, "Icon", i == 4 ? "UI_Dungeon_Pin_Boss" : "UI_WorldMap_Pin_Unlocked", false);
                Stretch(icon.rectTransform);
                var check = Sprite(host, "Check", "UI_WorldMap_Pin_Check", false);
                var crt = check.rectTransform;
                crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
                crt.pivot = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = new Vector2(7.5f, 6f);
                crt.sizeDelta = new Vector2(9f, 8f);
                check.raycastTarget = false;
                var badge = Sprite(host, "Badge", "UI_Dungeon_MapType_Winding", false);
                var brt = badge.rectTransform;
                brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0.5f);
                brt.pivot = new Vector2(0.5f, 0.5f);
                brt.anchoredPosition = new Vector2(0.5f, -13.5f);
                brt.sizeDelta = new Vector2(11f, 11f);
                badge.raycastTarget = false;
                var number = Label(host, "Num", (i + 1).ToString(), 0f, 0f, 13f, 13f, White, 8, TextAnchor.MiddleCenter);
                Stretch(number.rectTransform);
                var you = Sprite(host, "You", "UI_WorldMap_YouMarker", false);
                var yrt = you.rectTransform;
                yrt.anchorMin = yrt.anchorMax = new Vector2(0.5f, 0.5f);
                yrt.pivot = new Vector2(0.5f, 0.5f);
                yrt.anchoredPosition = new Vector2(0f, 18f);
                yrt.sizeDelta = new Vector2(8f, 8f);
                you.raycastTarget = false;
                you.gameObject.SetActive(false);
                var hit = icon.gameObject.AddComponent<Button>();
                hit.transition = Selectable.Transition.None;
                hit.targetGraphic = icon;
                hit.onClick.AddListener(() => Select(captured));
                _pins[i] = new Pin { halo = halo, icon = icon, check = check, badge = badge, number = number };
            }
            DrawLinks(map);
            var compass = Sprite(map, "Compass", "UI_WorldMap_Compass", false);
            Corner(compass.rectTransform, 388f, 6f, 16f, 16f);

            var side = Sprite(root, "Side", "UI_Panel_Inset", true);
            Corner(side.rectTransform, 436f, 40f, 188f, 312f);
            side.raycastTarget = true;
            _status = Outline(Label(side.rectTransform, "Status", "", 10f, 6f, 168f, 12f, Gold, 8, TextAnchor.UpperLeft));
            _name = Outline(Label(side.rectTransform, "Name", "", 10f, 17f, 168f, 22f, White, 20, TextAnchor.UpperLeft));
            _kindIcon = Sprite(side.rectTransform, "KindIcon", "UI_Dungeon_MapType_Field", false);
            Corner(_kindIcon.rectTransform, 10f, 40f, 13f, 13f);
            _kind = Label(side.rectTransform, "Kind", "", 26f, 40f, 150f, 14f, Hex(0xAEB6C8), 8, TextAnchor.UpperLeft);
            _level = Label(side.rectTransform, "Level", "", 10f, 54f, 168f, 12f, Cyan, 8, TextAnchor.UpperLeft);
            _waves = Label(side.rectTransform, "Waves", "", 10f, 66f, 168f, 12f, White, 8, TextAnchor.UpperLeft);
            _goal = Label(side.rectTransform, "Goal", "", 10f, 78f, 168f, 12f, White, 8, TextAnchor.UpperLeft);
            var div1 = Sprite(side.rectTransform, "Div1", "UI_Divider_Line", true);
            Corner(div1.rectTransform, 10f, 94f, 168f, 2f);
            _stars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                _stars[i] = Sprite(side.rectTransform, "Star" + i, "UI_Star_Large_Filled", false);
                Corner(_stars[i].rectTransform, 10f + i * 14f, 99f, 12f, 12f);
            }
            _record = Label(side.rectTransform, "Record", "", 52f, 99f, 120f, 12f, Gold, 8, TextAnchor.UpperLeft);
            var div2 = Sprite(side.rectTransform, "Div2", "UI_Divider_Line", true);
            Corner(div2.rectTransform, 10f, 116f, 168f, 2f);
            Label(side.rectTransform, "RewardTitle", "PHẦN THƯỞNG", 10f, 120f, 160f, 12f, White, 8, TextAnchor.UpperLeft);
            _rewardText = new Text[3];
            _rewardIcon = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var slot = Sprite(side.rectTransform, "Reward" + i, "UI_Dungeon_RewardSlot", true);
                Corner(slot.rectTransform, 10f + i * 42f, 134f, 36f, 36f);
                _rewardIcon[i] = Sprite(slot.rectTransform, "Icon", "UI_Item_Coin", false);
                Corner(_rewardIcon[i].rectTransform, 2f, 2f, 32f, 32f);
                _rewardText[i] = Outline(Label(slot.rectTransform, "Text", "", 8f, 22f, 26f, 12f, White, 8, TextAnchor.UpperRight));
            }
            var div3 = Sprite(side.rectTransform, "Div3", "UI_Divider_Line", true);
            Corner(div3.rectTransform, 10f, 174f, 168f, 2f);
            _unlock = Label(side.rectTransform, "Unlock", "", 10f, 178f, 168f, 16f, Green, 8, TextAnchor.UpperLeft);
            var travel = Sprite(side.rectTransform, "Travel", "UI_Button_Positive_Normal", true);
            Corner(travel.rectTransform, 10f, 192f, 168f, 22f);
            _travelText = Label(travel.rectTransform, "Label", "DỊCH CHUYỂN", 0f, 4f, 140f, 14f, Green, 8, TextAnchor.UpperCenter);
            var key = Sprite(travel.rectTransform, "Key", "UI_KeyCap", false);
            Corner(key.rectTransform, 146f, 3f, 16f, 16f);
            Label(key.rectTransform, "E", "E", 0f, 2f, 16f, 12f, Ink, 8, TextAnchor.UpperCenter);
            _travel = travel.gameObject.AddComponent<Button>();
            _travel.transition = Selectable.Transition.None;
            _travel.targetGraphic = travel;
            var colors = _travel.colors;
            colors.disabledColor = Color.white;
            _travel.colors = colors;
            _travel.onClick.AddListener(Go);

            var strip = Sprite(side.rectTransform, "Guard", "UI_NPC_DialoguePanel", true);
            Corner(strip.rectTransform, 10f, 222f, 168f, 84f);
            var portraitSlot = Sprite(strip.rectTransform, "Slot", "UI_PortraitFrame", true);
            Corner(portraitSlot.rectTransform, 6f, 6f, 30f, 30f);
            var portrait = Sprite(strip.rectTransform, "Portrait", "UI_Dungeon_Guard_Portrait", false);
            Corner(portrait.rectTransform, 8f, 8f, 26f, 26f);
            Outline(Label(strip.rectTransform, "Who", "LÍNH CANH", 40f, 6f, 120f, 12f, Gold, 8, TextAnchor.UpperLeft));
            _guardLine = Label(strip.rectTransform, "Line", "", 6f, 40f, 156f, 40f, Cream, 8, TextAnchor.UpperLeft);
        }

        void ChooseDungeon(int index)
        {
            if (!DungeonProgress.DungeonUnlocked(index)) return;
            _dungeon = index;
            _sel = DungeonProgress.HighestUnlocked(index) - 1;
            Apply(true);
        }

        void Apply(bool restartFog)
        {
            var def = DungeonCatalog.Get(_dungeon);
            bool board = def.hasBoard;
            _board.gameObject.SetActive(board);
            int now = DungeonProgress.HighestUnlocked(_dungeon);
            if (_seen < 0) _seen = now;
            for (int n = 2; n <= 5; n++)
            {
                bool locked = !DungeonProgress.StageUnlocked(_dungeon, n);
                bool fade = board && restartFog && !locked && n > _seen;
                var fog = _fog[n - 2];
                fog.gameObject.SetActive(board && (locked || fade));
                var c = Color.white;
                c.a = locked || fade ? 1f : 0f;
                fog.color = c;
                _fade[n - 2] = fade ? 0.5f : -1f;
            }
            if (restartFog) _seen = now;

            for (int i = 0; i < 3; i++)
            {
                bool open = DungeonProgress.DungeonUnlocked(i);
                bool selected = i == _dungeon;
                string tab = !open ? "UI_Button_Secondary_Disabled" : selected ? "UI_Button_Primary_Normal" : "UI_Button_Secondary_Normal";
                var sprite = UiKit.Get(tab);
                if (sprite != null) _tabBg[i].sprite = sprite;
                _tabLabel[i].color = !open ? Hex(0x7A8090) : selected ? Hex(0x3A2412) : White;
                var lockPin = _tabBg[i].transform.Find("Lock");
                if (lockPin != null) lockPin.gameObject.SetActive(!open);
            }

            for (int i = 0; i < 5; i++)
            {
                int stage = i + 1;
                bool locked = !DungeonProgress.StageUnlocked(_dungeon, stage);
                bool cleared = DungeonProgress.StageCleared(_dungeon, stage);
                bool selected = i == _sel;
                string pin;
                if (stage == 5) pin = locked ? "UI_Dungeon_Pin_Boss_Locked" : "UI_Dungeon_Pin_Boss";
                else if (locked) pin = "UI_WorldMap_Pin_Locked";
                else if (selected) pin = "UI_WorldMap_Pin_Selected";
                else if (cleared) pin = "UI_WorldMap_Pin_Cleared";
                else pin = "UI_WorldMap_Pin_Unlocked";
                var sprite = UiKit.Get(pin);
                if (sprite != null) _pins[i].icon.sprite = sprite;
                _pins[i].halo.gameObject.SetActive(selected && !locked);
                _pins[i].check.gameObject.SetActive(cleared && !selected && stage < 5);
                _pins[i].number.gameObject.SetActive(!locked);
                _pins[i].number.color = selected ? Gold : White;
                var kind = def.stages[i].kind;
                string badgeName = DungeonCatalog.KindSprite(kind);
                bool showBadge = stage < 5 && badgeName.Length > 0;
                _pins[i].badge.gameObject.SetActive(showBadge);
                if (showBadge)
                {
                    var badge = UiKit.Get(badgeName);
                    if (badge != null) _pins[i].badge.sprite = badge;
                }
                var you = _pins[i].icon.transform.parent.Find("You");
                if (you != null) you.gameObject.SetActive(selected && !locked);
            }

            var place = def.stages[_sel];
            bool stageLocked = !DungeonProgress.StageUnlocked(_dungeon, _sel + 1);
            bool stageCleared = DungeonProgress.StageCleared(_dungeon, _sel + 1);
            _status.text = "Ải " + (_sel + 1) + " · " + (stageLocked ? "Bị khoá" : stageCleared ? "Đã qua" : "Đang mở");
            _name.text = place.name;
            _kind.text = DungeonCatalog.KindName(place.kind);
            var kindSprite = UiKit.Get(DungeonCatalog.KindSprite(place.kind));
            _kindIcon.gameObject.SetActive(kindSprite != null);
            if (kindSprite != null) _kindIcon.sprite = kindSprite;
            _level.text = "ĐỀ NGHỊ LV." + place.level;
            _level.color = DungeonCatalog.LevelWarn(place.level) ? Hex(0xE04848) : Cyan;
            _waves.text = DungeonCatalog.WaveLine(place);
            _goal.text = "MỤC TIÊU: " + DungeonCatalog.ObjectiveLine(place).ToUpperInvariant();
            _record.text = DungeonRecords.Line(_dungeon, _sel + 1);
            int stars = DungeonRecords.Stars(_dungeon, _sel + 1);
            for (int i = 0; i < 3; i++)
            {
                var star = UiKit.Get(i < stars ? "UI_Star_Large_Filled" : "UI_Star_Large_Empty");
                if (star != null) _stars[i].sprite = star;
            }
            for (int i = 0; i < 3; i++)
            {
                bool has = place.rewards != null && i < place.rewards.Length;
                _rewardText[i].text = has ? place.rewards[i].amount.ToString() : "";
                if (!has) continue;
                var icon = UiKit.Get(UiKit.ItemIcon(place.rewards[i].itemId));
                if (icon != null) _rewardIcon[i].sprite = icon;
            }
            if (stageLocked) _unlock.text = "Hạ ải " + _sel + " để mở ải " + (_sel + 1);
            else if (_sel < 4) _unlock.text = stageCleared ? "Đã mở ải " + (_sel + 2) : "Hạ ải " + (_sel + 1) + " để mở ải " + (_sel + 2);
            else _unlock.text = stageCleared ? "Đã mở phó bản sau" : "Hạ boss để mở phó bản sau";
            _guardLine.text = place.guard;
            if (place.talk != null && place.talk.Length > 0)
            {
                _guardLine.text = "";
                for (int i = 0; i < place.talk.Length && i < 3; i++)
                    _guardLine.text += place.talk[i].text + "\n";
            }
            _travel.interactable = !stageLocked;
            _travelText.text = stageLocked ? "BỊ KHOÁ" : stageCleared ? "VÀO LẠI" : "DỊCH CHUYỂN";
            _travelText.color = stageLocked ? Mute : Green;
        }

        void Go()
        {
            int stage = _sel + 1;
            if (!DungeonProgress.StageUnlocked(_dungeon, stage)) return;
            int dungeon = _dungeon;
            var flow = LevelFlow.Instance;
            var cfg = DungeonCatalog.BuildRun(dungeon, stage);
            if (flow == null || cfg == null) return;
            Close();
            DungeonRun.Arm(dungeon, stage);
            flow.StartLevel(cfg, true);
        }

        static void CloseButton(RectTransform root)
        {
            var host = New("Close", root);
            Corner(host, 618f, 2f, 16f, 16f);
            var pad = host.gameObject.AddComponent<Image>();
            pad.color = new Color(1f, 1f, 1f, 0f);
            pad.raycastTarget = true;
            var icon = Sprite(host, "Icon", "UI_Button_Close_Normal", false);
            var irt = icon.rectTransform;
            irt.anchorMin = irt.anchorMax = new Vector2(0.5f, 0.5f);
            irt.pivot = new Vector2(0.5f, 0.5f);
            irt.anchoredPosition = Vector2.zero;
            irt.sizeDelta = new Vector2(16f, 16f);
            var btn = host.gameObject.AddComponent<Button>();
            btn.targetGraphic = icon;
            btn.transition = Selectable.Transition.SpriteSwap;
            btn.spriteState = new SpriteState { pressedSprite = UiKit.Get("UI_Button_Close_Pressed") };
            btn.onClick.AddListener(Close);
        }

        static void DrawLinks(RectTransform map)
        {
            for (int i = 0; i < PinAt.Length - 1; i++)
            {
                Vector2 a = PinAt[i];
                Vector2 b = PinAt[i + 1];
                float dist = Vector2.Distance(a, b);
                int steps = Mathf.Max(1, Mathf.RoundToInt(dist / 6f));
                for (int s = 1; s < steps; s++)
                {
                    Vector2 p = Vector2.Lerp(a, b, s / (float)steps);
                    var dot = new GameObject("Link", typeof(RectTransform));
                    dot.transform.SetParent(map, false);
                    var img = dot.AddComponent<Image>();
                    img.sprite = UiKit.Get("UI_Divider_Line");
                    img.color = Hex(0x7A8090);
                    img.raycastTarget = false;
                    dot.transform.SetAsFirstSibling();
                    Mid((RectTransform)dot.transform, p.x, p.y, 2f, 2f);
                }
            }
        }

        static Text Outline(Text text)
        {
            WorldLabelsCanvas.ApplyPixelOutline(text, Ink);
            return text;
        }

        static Image Sprite(Transform parent, string name, string sprite, bool sliced)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = UiKit.Get(sprite);
            img.color = Color.white;
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            img.raycastTarget = false;
            return img;
        }

        static Text Label(Transform parent, string name, string text, float x, float y, float w, float h, Color color, int size, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = UiKit.Font;
            t.fontSize = size;
            t.text = text;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            Corner(t.rectTransform, x, y, w, h);
            return t;
        }

        static RectTransform New(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return (RectTransform)go.transform;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }

        static void Corner(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        static void Mid(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
        }

        static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
    }
}
