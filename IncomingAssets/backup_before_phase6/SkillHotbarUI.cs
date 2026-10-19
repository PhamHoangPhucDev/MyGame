using UnityEngine;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>
    /// Bottom-anchored skill hotbar: 2, 3, 4, R, V, C with yellow select + cooldown overlay.
    /// </summary>
    public class SkillHotbarUI : MonoBehaviour
    {
        [SerializeField] PlayerCombat combat;
        [SerializeField] HitComboCounter combo;

        struct Slot
        {
            public SkillId id;
            public string key;
            public Image frame;
            public Image icon;
            public Image select;
            public Image cooldownOverlay;
            public Text cooldownText;
            public Text keyText;
            public Color iconColor;
        }

        Slot[] _slots;
        Text _comboText;
        RectTransform _comboRt;
        float _comboPop;
        Image _hpFill;
        Health _playerHealth;
        static Sprite _uiSprite;

        public void Bind(PlayerCombat playerCombat, Health playerHealth, HitComboCounter hitCombo)
        {
            combat = playerCombat;
            _playerHealth = playerHealth;
            combo = hitCombo;
        }

        void Start()
        {
            if (combat == null)
                combat = FindFirstObjectByType<PlayerCombat>();
            if (combo == null)
                combo = FindFirstObjectByType<HitComboCounter>();
            if (_playerHealth == null && combat != null)
                _playerHealth = combat.GetComponent<Health>();

            BuildUi();
            if (combat != null)
            {
                combat.SelectedChanged += OnSelected;
                combat.CooldownsChanged += RefreshCooldowns;
                OnSelected(combat.SelectedSkill);
            }
            if (combo != null)
                combo.ComboChanged += OnCombo;
            if (_playerHealth != null)
                _playerHealth.Changed += _ => RefreshHp();

            RefreshCooldowns();
            RefreshHp();
        }

        void OnDestroy()
        {
            if (combat != null)
            {
                combat.SelectedChanged -= OnSelected;
                combat.CooldownsChanged -= RefreshCooldowns;
            }
            if (combo != null)
                combo.ComboChanged -= OnCombo;
        }

        void Update()
        {
            RefreshCooldowns();
            if (_comboRt == null) return;
            if (_comboPop > 0f)
            {
                _comboPop -= Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(_comboPop / 0.12f);
                _comboRt.anchoredPosition = new Vector2(0f, 8f * t);
            }
            else
                _comboRt.anchoredPosition = Vector2.zero;
        }

        void BuildUi()
        {
            EnsureSprite();

            var canvasGo = new GameObject("CombatCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(576, 1024);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Top HP bar
            var hpBg = CreateImage(canvasGo.transform, "HeroHpBg", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -28f), new Vector2(168f, 10f), new Color(0.1f, 0.05f, 0.05f, 0.9f));
            _hpFill = CreateImage(hpBg.transform, "HeroHpFill", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(0f, 0f), new Vector2(168f, 10f), new Color(0.85f, 0.12f, 0.12f, 1f));
            _hpFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            _hpFill.rectTransform.anchoredPosition = Vector2.zero;

            // Combo text (right-center-ish)
            var comboGo = new GameObject("ComboText");
            comboGo.transform.SetParent(canvasGo.transform, false);
            _comboRt = comboGo.AddComponent<RectTransform>();
            // Upper-right so combo never covers the hero at screen center.
            _comboRt.anchorMin = new Vector2(0.82f, 0.78f);
            _comboRt.anchorMax = new Vector2(0.82f, 0.78f);
            _comboRt.sizeDelta = new Vector2(220f, 48f);
            _comboText = comboGo.AddComponent<Text>();
            _comboText.font = ResolveUiFont();
            _comboText.fontSize = 16;
            _comboText.alignment = TextAnchor.MiddleCenter;
            _comboText.color = Color.white;
            _comboText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _comboText.raycastTarget = false;
            WorldLabelsCanvas.ApplyPixelOutline(_comboText, Color.black);
            _comboText.text = "";

            // Hotbar root
            var bar = new GameObject("Hotbar");
            bar.transform.SetParent(canvasGo.transform, false);
            var barRt = bar.AddComponent<RectTransform>();
            barRt.anchorMin = new Vector2(0.5f, 0f);
            barRt.anchorMax = new Vector2(0.5f, 0f);
            barRt.pivot = new Vector2(0.5f, 0f);
            barRt.anchoredPosition = new Vector2(0f, 28f);
            barRt.sizeDelta = new Vector2(520f, 92f);

            var defs = new (SkillId id, string key, Sprite icon)[]
            {
                (SkillId.FireDash, "2", UiPixelArt.Icon("flame")),
                (SkillId.MultiSlash, "3", UiPixelArt.Icon("slash")),
                (SkillId.MawBite, "4", UiPixelArt.Icon("jaws")),
                (SkillId.Sword, "R", UiPixelArt.Icon("sword")),
                (SkillId.Bone, "V", UiPixelArt.Icon("bone")),
                (SkillId.Mace, "C", UiPixelArt.Icon("mace")),
            };

            _slots = new Slot[defs.Length];
            float slotSize = 64f;
            float gap = 8f;
            float total = defs.Length * slotSize + (defs.Length - 1) * gap;
            float startX = -total * 0.5f + slotSize * 0.5f;

            for (int i = 0; i < defs.Length; i++)
            {
                float x = startX + i * (slotSize + gap);
                var frame = CreateImage(bar.transform, $"Slot_{defs[i].key}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(x, 8f), new Vector2(slotSize, slotSize), Color.white);
                frame.sprite = UiPixelArt.Frame;

                var icon = CreateImage(frame.transform, "Icon", new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f),
                    Vector2.zero, new Vector2(48f, 48f), Color.white);
                icon.sprite = defs[i].icon;
                icon.preserveAspect = true;

                var select = CreateImage(frame.transform, "Select", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(slotSize, slotSize), Color.white);
                select.sprite = UiPixelArt.Select;
                select.enabled = false;

                var cd = CreateImage(frame.transform, "Cooldown", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    Vector2.zero, new Vector2(slotSize - 8f, slotSize - 8f), new Color(0f, 0f, 0f, 0.65f));
                cd.type = Image.Type.Filled;
                cd.fillMethod = Image.FillMethod.Radial360;
                cd.fillOrigin = (int)Image.Origin360.Top;
                cd.fillClockwise = false;
                cd.fillAmount = 0f;

                var cdTextGo = new GameObject("CdText");
                cdTextGo.transform.SetParent(frame.transform, false);
                var cdRt = cdTextGo.AddComponent<RectTransform>();
                cdRt.anchorMin = cdRt.anchorMax = new Vector2(0.5f, 0.5f);
                cdRt.sizeDelta = new Vector2(slotSize, slotSize);
                var cdText = cdTextGo.AddComponent<Text>();
                cdText.font = _comboText.font;
                cdText.fontSize = 16;
                cdText.fontStyle = FontStyle.Normal;
                cdText.alignment = TextAnchor.MiddleCenter;
                cdText.color = Color.white;
                cdText.text = "";
                WorldLabelsCanvas.ApplyPixelOutline(cdText, Color.black);

                var keyGo = new GameObject("Key");
                keyGo.transform.SetParent(frame.transform, false);
                var keyRt = keyGo.AddComponent<RectTransform>();
                keyRt.anchorMin = new Vector2(0.5f, 0f);
                keyRt.anchorMax = new Vector2(0.5f, 0f);
                keyRt.pivot = new Vector2(0.5f, 0f);
                keyRt.anchoredPosition = new Vector2(0f, 2f);
                keyRt.sizeDelta = new Vector2(slotSize, 20f);
                var keyText = keyGo.AddComponent<Text>();
                keyText.font = _comboText.font;
                keyText.fontSize = 16;
                keyText.alignment = TextAnchor.LowerCenter;
                keyText.color = Color.white;
                keyText.text = defs[i].key;
                WorldLabelsCanvas.ApplyPixelOutline(keyText, Color.black);

                _slots[i] = new Slot
                {
                    id = defs[i].id,
                    key = defs[i].key,
                    frame = frame,
                    icon = icon,
                    select = select,
                    cooldownOverlay = cd,
                    cooldownText = cdText,
                    keyText = keyText,
                    iconColor = Color.white
                };
            }
        }

        void OnSelected(SkillId id)
        {
            if (_slots == null) return;
            for (int i = 0; i < _slots.Length; i++)
            {
                _slots[i].select.enabled = _slots[i].id == id;
            }
        }

        void OnCombo(int hits)
        {
            if (_comboText == null) return;
            _comboText.text = hits >= 2 ? $"x{hits} HITS" : "";
            _comboPop = hits >= 2 ? 0.12f : 0f;
        }

        void RefreshCooldowns()
        {
            if (_slots == null || combat == null) return;
            for (int i = 0; i < _slots.Length; i++)
            {
                float n = combat.GetCooldownNormalized(_slots[i].id);
                float rem = combat.GetCooldownRemaining(_slots[i].id);
                _slots[i].cooldownOverlay.fillAmount = n;
                _slots[i].cooldownText.text = rem > 0.05f ? rem.ToString("0.0") : "";
            }
        }

        void RefreshHp()
        {
            if (_hpFill == null || _playerHealth == null) return;
            float n = _playerHealth.Normalized;
            _hpFill.rectTransform.sizeDelta = new Vector2(168f * n, 10f);
        }

        static Image CreateImage(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 anchoredPos, Vector2 size, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.sprite = _uiSprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static void EnsureSprite()
        {
            if (_uiSprite != null) return;
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color[16];
            for (int i = 0; i < 16; i++) px[i] = Color.white;
            tex.SetPixels(px);
            tex.Apply(false, true);
            _uiSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 4f);
        }

        static Font ResolveUiFont()
        {
            var pixel = Resources.Load<Font>("Fonts/PressStart2P-Regular");
            if (pixel != null) return pixel;
            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (font != null) return font;
            return Font.CreateDynamicFontFromOSFont(new[] { "Press Start 2P", "Consolas", "Courier New" }, 16);
        }
    }

    static class UiPixelArt
    {
        static Sprite _frame;
        static Sprite _select;
        static readonly System.Collections.Generic.Dictionary<string, Sprite> Icons = new();

        public static Sprite Frame => _frame ??= Sheet(32, FrameRows);
        public static Sprite Select => _select ??= Sheet(32, SelectRows);
        public static Sprite Icon(string name) => Icons.TryGetValue(name, out var s) ? s : Icons[name] = Sheet(16, Rows(name));

        static string[] Rows(string name)
        {
            switch (name)
            {
                case "flame": return Flame;
                case "slash": return Slash;
                case "jaws": return Jaws;
                case "sword": return Sword;
                case "bone": return Bone;
                default: return Mace;
            }
        }

        static Sprite Sheet(int size, string[] rows)
        {
            var px = new Color32[size * size];
            for (int row = 0; row < size; row++)
            for (int x = 0; x < size; x++)
                px[(size - 1 - row) * size + x] = Ink(rows[row][x]);
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
        }

        static Color32 Ink(char c)
        {
            switch (c)
            {
                case '.': return new Color32(0, 0, 0, 0);
                case '#': return new Color32(42, 26, 22, 255);
                case '+': return new Color32(92, 58, 40, 255);
                case 'F': return new Color32(92, 28, 36, 255);
                case 'Y': return new Color32(242, 210, 64, 255);
                case 'O': return new Color32(232, 112, 36, 255);
                case 'o': return new Color32(255, 196, 64, 255);
                case 'R': return new Color32(196, 42, 42, 255);
                case 'W': return new Color32(244, 240, 230, 255);
                case 'S': return new Color32(186, 198, 214, 255);
                case 'G': return new Color32(196, 154, 64, 255);
                case 'B': return new Color32(232, 220, 196, 255);
                case 'D': return new Color32(72, 64, 78, 255);
                case 'd': return new Color32(48, 42, 52, 255);
                default: return new Color32(0, 0, 0, 0);
            }
        }

        static readonly string[] FrameRows =
        {
            "################################",
            "##++++++++++++++++++++++++++++##",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "#+FFFFFFFFFFFFFFFFFFFFFFFFFFFF+#",
            "##++++++++++++++++++++++++++++##",
            "################################",
        };

        static readonly string[] SelectRows =
        {
            "YYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYY",
            "Y..............................Y",
            "Y.YYYYYYYYYYYYYYYYYYYYYYYYYYYY.Y",
            "Y.Y............................Y",
            "Y.Y............................Y",
            "Y.Y............................Y",
            "Y.Y............................Y",
            "Y.Y............................Y",
            "Y.Y............................Y",
            "Y.Y............................Y",
            "Y.Y............................Y",
            "Y.Y............................Y",
            "Y.Y............................Y",
            "Y.Y............................Y",
            "Y.Y............................Y",
            "Y.Y............................Y",
            "Y.Y............................Y",
            "Y.Y............................Y",
            "Y.Y............................Y",
            "Y.Y............................Y",
            "Y.Y............................Y",
            "Y.Y............................Y",
            "Y.Y............................Y",
            "Y.Y............................Y",
            "Y.Y............................Y",
            "Y.Y............................Y",
            "Y.Y............................Y",
            "Y.Y............................Y",
            "Y.YYYYYYYYYYYYYYYYYYYYYYYYYYYY.Y",
            "Y..............................Y",
            "YYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYY",
            "YYYYYYYYYYYYYYYYYYYYYYYYYYYYYYYY",
        };

        static readonly string[] Flame =
        {
            "................",
            ".......oo.......",
            "......oOOo......",
            ".....oOOOOo.....",
            ".....oORROo.....",
            "....oORRRROo....",
            "...oOORRRROOo...",
            "...oORRRRRROo...",
            "..oOORRRRRROOo..",
            "..oORRRRRRRROo..",
            "..oORRRRRRRROo..",
            "...oRRRRRRROo...",
            "...oORRRRROo....",
            "....oORROo......",
            ".....oOo........",
            "................",
        };

        static readonly string[] Slash =
        {
            "................",
            "..........RR....",
            ".........RR.....",
            "....RR..RR......",
            "...RR..RR.......",
            "..RR..RR........",
            ".RR..RR.........",
            "RR..RR..........",
            "...RR...........",
            "..RR............",
            ".RR.............",
            "RR..............",
            "................",
            "................",
            "................",
            "................",
        };

        static readonly string[] Jaws =
        {
            "................",
            "..DDDD....DDDD..",
            ".DWWWD....DWWWD.",
            ".DWWWD....DWWWD.",
            ".DWWWD....DWWWD.",
            "..DDDDDDDDDDDD..",
            "....WW.WW.WW....",
            "....DDDDDDDD....",
            "....D......D....",
            "...DW.WW.WWD....",
            "...DWWWWWWWD....",
            "....DDDDDDDD....",
            "................",
            "................",
            "................",
            "................",
        };

        static readonly string[] Sword =
        {
            "................",
            ".......SS.......",
            "......SSSS......",
            "......SSSS......",
            "......SSSS......",
            "......SSSS......",
            "......SSSS......",
            "......SSSS......",
            "......SSSS......",
            ".....SGGGS......",
            "....GGGGGGGG....",
            "......dddd......",
            "......dddd......",
            "......dddd......",
            "................",
            "................",
        };

        static readonly string[] Bone =
        {
            "................",
            "................",
            "....WWW.........",
            "...WWWWW........",
            "...WW.WW........",
            "....WWW.........",
            ".....BBBBBB.....",
            ".....BBBBBB.....",
            ".........WWW....",
            "........WW.WW...",
            "........WWWWW...",
            ".........WWW....",
            "................",
            "................",
            "................",
            "................",
        };

        static readonly string[] Mace =
        {
            "................",
            ".....dddddd.....",
            "....dDDDDDDd....",
            "...dDWDWDWDDd...",
            "...dDDDDDDDDd...",
            "...dDWDWDWDDd...",
            "....dDDDDDDd....",
            ".....dddddd.....",
            ".......dd.......",
            ".......dd.......",
            ".......dd.......",
            "......dddd......",
            "................",
            "................",
            "................",
            "................",
        };
    }
}

