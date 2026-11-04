using UnityEngine;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>Outdoor world map. Region N uses Level N. Art is used as exported; text is Unity text.</summary>
    public class WorldMapPanel : MonoBehaviour
    {
        struct Place
        {
            public string name, level, layout, enemies, boss, gather;
        }

        struct Pin
        {
            public Image halo, icon, check;
            public Text number;
        }

        struct Row
        {
            public Image bg, badge;
            public Text name, number;
        }

        static readonly Place[] Places =
        {
            new Place { name = "VILLAGE", level = "Safe zone", layout = "Hub", enemies = "None", boss = "-", gather = "Shops, Housing" },
            new Place { name = "GOBLIN FOREST CAMP", level = "Lv 1-4", layout = "Ring", enemies = "Goblin, Gob Archer", boss = "Goblin Chief", gather = "Wood, Hide" },
            new Place { name = "FOREST TRAIL", level = "Lv 3-7", layout = "Path", enemies = "Goblin, Gob Archer, Gobrider", boss = "Patrol Captain", gather = "Herbs, Mushrooms" },
            new Place { name = "ABANDONED CAMP", level = "Lv 6-10", layout = "Ring", enemies = "Satyr, Dryad, Dryad Rider", boss = "Dryad Rider Alpha", gather = "Tree Sap, Spirit Bloom" },
            new Place { name = "GOBLIN OUTPOST", level = "Lv 9-14", layout = "Path", enemies = "Gobrider, Elite Gobrider, Warlord", boss = "Cyclops", gather = "Stone, Iron Ore" },
            new Place { name = "GOBRIDER STRONGHOLD", level = "Lv 13-18", layout = "Ring", enemies = "Goblin, Gobrider", boss = "Warlord", gather = "Iron, Spoils" }
        };

        static readonly Vector2[] PinAt =
        {
            new Vector2(236f, 262f), new Vector2(122f, 232f), new Vector2(62f, 128f),
            new Vector2(232f, 126f), new Vector2(352f, 216f), new Vector2(364f, 96f)
        };

        static readonly Color Gold = Hex(0xF2D84A), Cyan = Hex(0x5ED0E0), LBlue = Hex(0x8EC8F0);
        static readonly Color Green = Hex(0x7ED67E), RedE = Hex(0xE04848), White = Hex(0xF4F4F0), Mute = Hex(0x8A909C);

        static WorldMapPanel _one;
        static int _seen = -1;

        Pin[] _pins;
        Row[] _rows;
        Image[] _fog;
        float[] _fade;
        Text _name, _level, _layout, _enemies, _boss, _gather, _travelText;
        Button _travel;
        int _sel;

        public static bool IsOpen => _one != null && _one.gameObject.activeSelf;

        public static void Toggle()
        {
            if (IsOpen) Close();
            else Open();
        }

        public static void Open()
        {
            var panel = Ensure();
            panel.gameObject.SetActive(true);
            panel._sel = Mathf.Clamp(GameProgress.HighestUnlockedLevel, 1, 5);
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

        /// <summary>Test hook: the next open treats levels above this as newly unlocked.</summary>
        public static void Remember(int level) => _seen = level;

        public void Select(int index)
        {
            if (index < 0 || index >= Places.Length) return;
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
                float a = Mathf.Clamp01(_fade[i] / 0.5f);
                var c = _fog[i].color;
                c.r = c.g = c.b = 1f;
                c.a = a;
                _fog[i].color = c;
                if (_fade[i] <= 0f)
                {
                    _fade[i] = -1f;
                    _fog[i].gameObject.SetActive(false);
                }
            }
        }

        public string PinSprite(int region) => _pins[region].icon.sprite != null ? _pins[region].icon.sprite.name : "missing";
        public bool HaloOn(int region) => _pins[region].halo.gameObject.activeSelf;
        public bool CheckOn(int region) => _pins[region].check.gameObject.activeSelf;
        public bool FogOn(int region) => _fog[region - 1].gameObject.activeSelf && _fog[region - 1].color.a > 0.5f;
        public bool TravelOn => _travel.interactable;
        public Color RowColor(int index) => _rows[index].name.color;

        static WorldMapPanel Ensure()
        {
            if (_one != null) return _one;
            var go = new GameObject("WorldMapCanvas");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 450;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640f, 360f);
            scaler.matchWidthOrHeight = 0.5f;
            scaler.referencePixelsPerUnit = 16f;
            go.AddComponent<GraphicRaycaster>();
            _one = go.AddComponent<WorldMapPanel>();
            _one.Build();
            go.SetActive(false);
            return _one;
        }

        void OnDestroy()
        {
            if (_one == this) _one = null;
        }

        void Update() => TickFade(Time.unscaledDeltaTime);

        void Build()
        {
            UiKit.Ensure();
            var root = (RectTransform)transform;
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
            root.offsetMin = Vector2.zero;
            root.offsetMax = Vector2.zero;

            var frame = Sprite(root, "Frame", "UI_WorldMap_Frame", true);
            Stretch(frame.rectTransform);
            frame.raycastTarget = true;

            var title = Label(root, "Title", "WORLD MAP", 14f, 11f, 120f, 12f, Gold, TextAnchor.UpperLeft);
            Label(root, "Subtitle", "Outdoor Regions", 14f + title.preferredWidth + 8f, 11f, 160f, 12f, Mute, TextAnchor.UpperLeft);
            Label(root, "CloseHint", "[M] Close", 500f, 11f, 126f, 12f, LBlue, TextAnchor.UpperRight);

            var map = New("Map", root);
            Corner(map, 14f, 34f, 408f, 308f);
            var art = Sprite(map, "Art", "UI_WorldMap_Outdoor_Art", false);
            Stretch(art.rectTransform);
            art.raycastTarget = false;

            _fog = new Image[5];
            _fade = new float[5];
            for (int i = 0; i < 5; i++)
            {
                _fog[i] = Sprite(map, "Fog" + (i + 1), "UI_WorldMap_Fog_Region" + (i + 1), false);
                Stretch(_fog[i].rectTransform);
                _fog[i].raycastTarget = false;
                _fade[i] = -1f;
            }

            Sprite(map, "Compass", "UI_WorldMap_Compass", false).rectTransform.SetAsLastSibling();
            var compass = map.Find("Compass").GetComponent<RectTransform>();
            Corner(compass, 388f, 1f, 9f, 19f);
            compass.GetComponent<Image>().raycastTarget = false;

            _pins = new Pin[6];
            for (int i = 0; i < 6; i++)
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
                var icon = Sprite(host, "Icon", i == 0 ? "UI_WorldMap_Pin_Village" : "UI_WorldMap_Pin_Unlocked", false);
                Stretch(icon.rectTransform);
                var check = Sprite(host, "Check", "UI_WorldMap_Pin_Check", false);
                var crt = check.rectTransform;
                crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
                crt.pivot = new Vector2(0.5f, 0.5f);
                crt.anchoredPosition = new Vector2(5f, 7.5f);
                crt.sizeDelta = new Vector2(9f, 8f);
                check.raycastTarget = false;
                var number = Label(host, "Num", i == 0 ? "" : i.ToString(), 0f, 0f, 13f, 13f, White, TextAnchor.MiddleCenter);
                Stretch(number.rectTransform);
                var btn = icon.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => Select(captured));
                _pins[i] = new Pin { halo = halo, icon = icon, check = check, number = number };
            }

            var you = Sprite(map, "You", "UI_WorldMap_YouMarker", false);
            Mid(you.rectTransform, 200.5f, 262.5f, 8f, 8f);
            you.raycastTarget = false;
            MapOutline(Label(map, "YouLabel", "YOU", 170f, 270f, 60f, 10f, White, TextAnchor.UpperCenter));
            MapOutline(Label(map, "Hint", "[LMB] Select   [E] Travel", 8f, 294f, 220f, 12f, LBlue, TextAnchor.UpperLeft));

            var side = Sprite(root, "Side", "UI_WorldMap_SidePanel", true);
            Corner(side.rectTransform, 432f, 34f, 196f, 312f);
            side.raycastTarget = true;
            Label(side.rectTransform, "Regions", "REGIONS", 8f, 7f, 80f, 12f, Mute, TextAnchor.UpperLeft);

            _rows = new Row[6];
            for (int i = 0; i < 6; i++)
            {
                int captured = i;
                var row = Sprite(side.rectTransform, "Row" + i, "UI_WorldMap_Row", true);
                Corner(row.rectTransform, 4f, 20f + i * 19f, 188f, 17f);
                var badge = Sprite(row.rectTransform, "Badge", "UI_WorldMap_Pin_Unlocked", false);
                Corner(badge.rectTransform, 4f, 2f, 13f, 13f);
                badge.raycastTarget = false;
                var num = Label(row.rectTransform, "Num", i == 0 ? "" : i.ToString(), 4f, 2f, 13f, 13f, White, TextAnchor.MiddleCenter);
                var name = Label(row.rectTransform, "Name", Places[i].name, 21f, 3f, 164f, 12f, White, TextAnchor.UpperLeft);
                var btn = row.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
                btn.onClick.AddListener(() => Select(captured));
                _rows[i] = new Row { bg = row, badge = badge, name = name, number = num };
            }

            _name = Label(side.rectTransform, "DetailName", "", 8f, 145f, 180f, 12f, Gold, TextAnchor.UpperLeft);
            _level = Label(side.rectTransform, "DetailLevel", "", 8f, 157f, 90f, 12f, Cyan, TextAnchor.UpperLeft);
            _layout = Label(side.rectTransform, "DetailLayout", "", 100f, 157f, 88f, 12f, Mute, TextAnchor.UpperRight);
            _enemies = Body(side.rectTransform, "Enemies", "ENEMIES", 173f, White);
            _boss = Body(side.rectTransform, "Boss", "REGION BOSS", 205f, RedE);
            _gather = Body(side.rectTransform, "Gather", "GATHER", 227f, Green);

            var travel = Sprite(side.rectTransform, "Travel", "UI_WorldMap_Button_Travel", true);
            Corner(travel.rectTransform, 8f, 284f, 180f, 20f);
            _travelText = Label(travel.rectTransform, "Label", "TRAVEL  [E]", 0f, 4f, 180f, 12f, Green, TextAnchor.UpperCenter);
            _travel = travel.gameObject.AddComponent<Button>();
            _travel.transition = Selectable.Transition.None;
            var colors = _travel.colors;
            colors.disabledColor = Color.white;
            _travel.colors = colors;
            _travel.onClick.AddListener(Go);
        }

        void Apply(bool restartFog)
        {
            int now = GameProgress.HighestUnlockedLevel;
            if (_seen < 0) _seen = now;
            for (int n = 1; n <= 5; n++)
            {
                bool locked = RegionLocked(n);
                bool fade = restartFog && !locked && n > _seen;
                _fog[n - 1].gameObject.SetActive(locked || fade);
                var c = Color.white;
                c.a = locked || fade ? 1f : 0f;
                _fog[n - 1].color = c;
                _fade[n - 1] = fade ? 0.5f : -1f;
            }
            if (restartFog) _seen = now;

            for (int i = 0; i < 6; i++)
            {
                bool locked = RegionLocked(i);
                bool cleared = RegionCleared(i);
                bool selected = i == _sel;
                string pin = i == 0 ? "UI_WorldMap_Pin_Village"
                    : locked ? "UI_WorldMap_Pin_Locked"
                    : selected ? "UI_WorldMap_Pin_Selected"
                    : cleared ? "UI_WorldMap_Pin_Cleared"
                    : "UI_WorldMap_Pin_Unlocked";
                var sprite = UiKit.Get(pin);
                if (sprite != null) _pins[i].icon.sprite = sprite;
                _pins[i].halo.gameObject.SetActive(selected && i != 0 && !locked);
                _pins[i].check.gameObject.SetActive(cleared && !selected);
                bool showNum = i != 0 && !locked;
                _pins[i].number.gameObject.SetActive(showNum);
                _pins[i].number.color = selected ? Gold : White;

                var rowSprite = UiKit.Get(selected ? "UI_WorldMap_Row_Selected" : "UI_WorldMap_Row");
                if (rowSprite != null) _rows[i].bg.sprite = rowSprite;
                var badge = UiKit.Get(pin);
                if (badge != null) _rows[i].badge.sprite = badge;
                _rows[i].name.color = locked ? Mute : selected ? Gold : White;
                _rows[i].number.gameObject.SetActive(showNum);
                _rows[i].number.color = selected ? Gold : White;
            }

            var place = Places[_sel];
            _name.text = place.name;
            _level.text = place.level;
            _layout.text = place.layout;
            _enemies.text = place.enemies;
            _boss.text = place.boss;
            _gather.text = place.gather;
            bool go = CanTravel(_sel);
            _travel.interactable = go;
            _travelText.color = go ? Green : Mute;
        }

        void Go()
        {
            if (!CanTravel(_sel)) return;
            int sel = _sel;
            Close();
            var nav = WorldNavigator.Instance;
            if (sel == 0)
            {
                if (nav == null) return;
                if (nav.Current == WorldZone.None) nav.ReturnFromDungeon();
                else nav.Show(WorldZone.Hub);
                return;
            }
            if (nav != null) nav.CloseDungeonBoard();
            var flow = LevelFlow.Instance;
            var cfg = flow != null ? flow.GetLevelConfig(sel) : null;
            if (cfg != null) flow.StartLevel(cfg, true);
        }

        public static bool RegionLocked(int region) => region >= 1 && region <= 5 && !GameProgress.IsLevelUnlocked(region);
        public static bool RegionCleared(int region) => region >= 1 && region < 5 && GameProgress.IsLevelUnlocked(region + 1);
        public static bool CanTravel(int index) => index == 0 || (index >= 1 && index <= 5 && !RegionLocked(index));

        static Text Body(Transform parent, string name, string title, float y, Color body)
        {
            Label(parent, name + "Title", title, 8f, y, 180f, 10f, Mute, TextAnchor.UpperLeft);
            return Label(parent, name, "", 8f, y + 11f, 180f, 18f, body, TextAnchor.UpperLeft);
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

        static Text Label(Transform parent, string name, string text, float x, float y, float w, float h, Color color, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = UiKit.Font;
            t.fontSize = 8;
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

        static void MapOutline(Text text) => WorldLabelsCanvas.ApplyPixelOutline(text, Hex(0x141018));

        static Color Hex(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
    }
}
