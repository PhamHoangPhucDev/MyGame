using UnityEngine;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>
    /// Top-of-screen boss HP bar for Level 5. World label on the boss stays separate.
    /// </summary>
    public class BossHudUI : MonoBehaviour
    {
        public static BossHudUI Instance { get; private set; }

        RectTransform _root;
        Text _title;
        Image _fill;
        Image _bg;
        BossWarlordController _boss;
        Health _health;
        string _name;
        static Sprite _white;

        void Awake()
        {
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public static BossHudUI Ensure()
        {
            if (Instance != null) return Instance;
            var go = new GameObject("BossHudUI");
            return go.AddComponent<BossHudUI>();
        }

        public void Bind(BossWarlordController boss)
        {
            _boss = boss;
            _health = boss != null ? boss.Health : null;
            _name = boss != null ? boss.DisplayName : null;
            Show(_health != null);
        }

        public void Bind(Health health, string displayName)
        {
            _boss = null;
            _health = health;
            _name = string.IsNullOrEmpty(displayName) ? "Cyclops" : displayName;
            Show(_health != null);
        }

        void Show(bool on)
        {
            EnsureBuilt();
            if (_root != null)
                _root.gameObject.SetActive(on);
            if (_title != null && on)
                _title.text = DungeonRun.Active ? DungeonName() : _name;
            DressForDungeon();
            Refresh();
        }

        public void Clear()
        {
            _boss = null;
            _health = null;
            _name = null;
            if (_root != null)
                _root.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (_root == null || !_root.gameObject.activeSelf) return;
            var hp = _boss != null ? _boss.Health : _health;
            if (hp == null || hp.IsDead)
            {
                Clear();
                return;
            }
            Refresh();
            UpdateNotches();
            if (_title == null) return;
            _title.text = DungeonRun.Active
                ? DungeonName()
                : _boss != null
                    ? $"{_boss.DisplayName}  —  PHASE {(int)_boss.Phase}"
                    : _name;
        }

        /// <summary>Dungeon boss names follow the stage spec (stage 5: Tướng Gobrider), not the campaign name.</summary>
        string DungeonName() => _boss != null ? "TƯỚNG GOBRIDER" : (_name ?? "").ToUpperInvariant();

        /// <summary>Dungeon layout: bar takes the objective strip's place (kit 215,28 210×22) with phase notches at 66% / 33%.</summary>
        void DressForDungeon()
        {
            if (_root == null) return;
            bool dungeon = DungeonRun.Active;
            var layout = _root.GetComponent<UiLayout>();
            if (layout != null)
            {
                layout.Landscape(UiLayout.Spec.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, dungeon ? -31f : -60f), new Vector2(210f, 22f)));
                layout.Apply(true);
            }
            if (_notches == null && _bg != null)
            {
                _notches = new Image[2];
                var sheet = NotchArt;
                for (int i = 0; i < 2; i++)
                {
                    var go = new GameObject("PhaseNotch" + i, typeof(RectTransform));
                    go.transform.SetParent(_bg.transform, false);
                    var rt = (RectTransform)go.transform;
                    float x = i == 0 ? 0.66f : 0.33f;
                    rt.anchorMin = new Vector2(x, 0f);
                    rt.anchorMax = new Vector2(x, 1f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.sizeDelta = new Vector2(2f * UiKit.Px, 2f * UiKit.Px);
                    var img = go.AddComponent<Image>();
                    img.raycastTarget = false;
                    img.sprite = sheet.Length > 0 ? sheet[0] : _white;
                    img.color = sheet.Length > 0 ? Color.white : new Color32(0x14, 0x10, 0x18, 0xFF);
                    _notches[i] = img;
                }
            }
            UpdateNotches();
        }

        void UpdateNotches()
        {
            if (_notches == null) return;
            bool dungeon = DungeonRun.Active;
            var hp = _boss != null ? _boss.Health : _health;
            var lit = NotchArt;
            for (int i = 0; i < _notches.Length; i++)
            {
                if (_notches[i] == null) continue;
                _notches[i].gameObject.SetActive(dungeon && _boss != null);
                // Frame 1 marks a phase already passed.
                float at = i == 0 ? 0.66f : 0.33f;
                if (lit.Length > 1 && hp != null) _notches[i].sprite = hp.Normalized <= at ? lit[1] : lit[0];
            }
        }

        Image[] _notches;
        static Sprite[] _notchArt;
        static Sprite[] NotchArt => _notchArt ??= DungeonFenceMaze.Art("Stage5", "Stage5_BossPhaseNotch_2f");

        void Refresh()
        {
            var hp = _boss != null ? _boss.Health : _health;
            if (_fill == null || hp == null) return;
            float n = hp.Normalized;
            float w = _bg != null && _bg.rectTransform.rect.width > 1f ? _bg.rectTransform.rect.width - 6f * UiKit.Px : 520f;
            _fill.rectTransform.sizeDelta = new Vector2(w * n, _fill.rectTransform.sizeDelta.y);
        }

        void EnsureBuilt()
        {
            if (_root != null) return;
            EnsureWhite();
            var canvasGo = new GameObject("BossHudCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            UiKit.FitScaler(canvasGo);

            var rootGo = new GameObject("BossBar");
            rootGo.transform.SetParent(canvasGo.transform, false);
            _root = rootGo.AddComponent<RectTransform>();
            _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 1f);
            _root.pivot = new Vector2(0.5f, 1f);
            // Sit under Level/Wave/Objective HUD so labels stay readable.
            _root.anchoredPosition = new Vector2(0f, -78f);
            _root.sizeDelta = new Vector2(560f, 48f);
            // Kit pixels: under the objective plate and combo line in both profiles.
            rootGo.AddComponent<UiLayout>()
                .Landscape(UiLayout.Spec.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -60f), new Vector2(210f, 22f)))
                .Portrait(UiLayout.Spec.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -98f), new Vector2(200f, 22f)));

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(_root, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = Vector2.zero;
            titleRt.sizeDelta = new Vector2(560f, 22f);
            titleGo.AddComponent<UiLayout>().Both(UiLayout.Spec.Stretch(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 10f)));
            _title = titleGo.AddComponent<Text>();
            _title.font = WorldLabelsCanvas.Ensure().PixelFont;
            _title.fontSize = UiKit.Fs(16);
            _title.alignment = TextAnchor.MiddleCenter;
            _title.color = Pal.GoldL;
            _title.raycastTarget = false;
            _title.horizontalOverflow = HorizontalWrapMode.Overflow;
            // VT323 tone marks sit above the cap line; let them draw outside the 10 px row.
            _title.verticalOverflow = VerticalWrapMode.Overflow;
            WorldLabelsCanvas.ApplyPixelOutline(_title, Color.black);
            LocText.Attach(_title);
            var titleFont = titleGo.GetComponent<UiLayout>();
            titleFont.fontPx = 8;
            titleFont.Apply(true);

            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(_root, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 0f);
            bgRt.pivot = new Vector2(0.5f, 0f);
            bgRt.anchoredPosition = new Vector2(0f, 4f);
            bgRt.sizeDelta = new Vector2(520f, 12f);
            _bg = bgGo.AddComponent<Image>();
            _bg.sprite = _white;
            _bg.color = new Color(0.08f, 0.05f, 0.05f, 0.92f);
            _bg.raycastTarget = false;
            if (UiKit.Put(_bg, "UI_BossBar_Frame", true)) _bg.color = Color.white;
            bgGo.AddComponent<UiLayout>().Both(UiLayout.Spec.Stretch(new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 12f)));

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(bgGo.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.anchoredPosition = Vector2.zero;
            fillRt.sizeDelta = new Vector2(520f, 0f);
            // Inset inside the boss frame border (3 kit px each side).
            var fillTop = fillGo.AddComponent<UiLayout>();
            fillTop.Both(UiLayout.Spec.Stretch(new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(3f, 0f), new Vector2(0f, -6f)));
            _fill = fillGo.AddComponent<Image>();
            _fill.sprite = _white;
            _fill.color = new Color(0.92f, 0.22f, 0.12f, 1f);
            _fill.raycastTarget = false;
            if (UiKit.Put(_fill, "UI_Bar_Fill_Boss", false))
                _fill.color = Color.white;

            _root.gameObject.SetActive(false);
        }

        static void EnsureWhite()
        {
            if (_white != null) return;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply(false, true);
            _white = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
