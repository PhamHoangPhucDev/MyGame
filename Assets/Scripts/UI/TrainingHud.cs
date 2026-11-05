using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>
    /// Training Field HUD (Docs/WorldArt/TrainingArea/TRAINING_UI_PROPOSAL.md, phase A).
    /// Zone status plate, Damage Test panel on the dummy, class chips, back-to-village button, hit counter.
    /// Reads existing logic only: TrainingGround, the TrainingDummy Health, ClassCombat, WorldSave.
    /// The damage meter is a UI-side sum of the dummy's Health.Damaged; it never writes save.
    /// </summary>
    public class TrainingHud : MonoBehaviour
    {
        // Video reference palette (Damage Test V09, build sidebar V28).
        static readonly Color Slate = new Color32(0x1C, 0x1F, 0x26, 0xF2);
        static readonly Color SlateRow = new Color32(0x25, 0x2A, 0x33, 0xFF);
        static readonly Color SlateBorder = new Color32(0x4A, 0x52, 0x60, 0xFF);
        static readonly Color Plum = new Color32(0x4A, 0x31, 0x40, 0xF2);
        static readonly Color PlumCard = new Color32(0x6A, 0x40, 0x48, 0xFF);
        static readonly Color PlumBorder = new Color32(0xB8, 0x9A, 0xA0, 0xFF);
        static readonly Color Gold = new Color32(0xF2, 0xD8, 0x4A, 0xFF);
        static readonly Color GoldEdge = new Color32(0xF2, 0xD0, 0x30, 0xFF);
        static readonly Color Green = new Color32(0x7E, 0xD6, 0x7E, 0xFF);
        static readonly Color GreenFill = new Color32(0x1F, 0x4A, 0x2E, 0xFF);
        static readonly Color Cyan = new Color32(0x5E, 0xD0, 0xE0, 0xFF);
        static readonly Color White = new Color32(0xF4, 0xF4, 0xF0, 0xFF);
        static readonly Color Mute = new Color32(0x8A, 0x8F, 0x9A, 0xFF);
        static readonly Color HpRed = new Color32(0xC8, 0x30, 0x3C, 0xFF);
        static readonly Color HpTrack = new Color32(0x1A, 0x1A, 0x22, 0xFF);
        static readonly Color ExpFill = new Color32(0xBF, 0xE6, 0xEE, 0xFF);
        static readonly Color ExpTrack = new Color32(0x2A, 0x3A, 0x44, 0xFF);

        static readonly string[] ClassLetters = { "A", "B", "L", "S", "V" };
        const float OpenRange = 3f, ForgetRange = 4.5f, IdleStop = 3f;

        WorldNavigator _nav;
        Canvas _canvas;
        GameObject _root, _panel, _hitsGo;
        Text _quai, _hoi, _expGain, _lv, _live, _dps, _dmg, _time, _hits, _max, _hitsWorld, _playLabel;
        RectTransform _expFill, _hpFill, _hitsRt;
        Image[] _chipEdge = new Image[5];
        Text[] _chipText = new Text[5];
        bool _keyHints;

        Health _dummy;
        int _total, _count, _maxHit;
        float _elapsed, _lastHit = -99f, _tick;
        bool _paused, _dismissed, _wasNear;

        void Awake() => _nav = GetComponent<WorldNavigator>();

        void OnDestroy()
        {
            if (_dummy != null) _dummy.Damaged -= OnDummyHit;
            if (_canvas != null) Destroy(_canvas.gameObject);
        }

        void Update()
        {
            bool inZone = _nav != null && _nav.Current == WorldZone.Training;
            if (!inZone)
            {
                if (_root != null && _root.activeSelf) _root.SetActive(false);
                return;
            }
            if (_canvas == null) Build();
            if (!_root.activeSelf) _root.SetActive(true);
            BindDummy();
            TickMeter();
            TickProximity();
            TickKeys();
            _tick -= Time.unscaledDeltaTime;
            if (_tick <= 0f)
            {
                _tick = 0.25f;
                RefreshStatus();
                RefreshChips();
            }
            RefreshPanel();
            RefreshWorldHits();
        }

        // ------------------------------------------------------------------ logic hooks (read only)

        void BindDummy()
        {
            if (_dummy != null) return;
            var tg = _nav.Training;
            var t = tg != null ? tg.transform.Find("TrainingDummy") : null;
            _dummy = t != null ? t.GetComponent<Health>() : null;
            if (_dummy != null) _dummy.Damaged += OnDummyHit;
        }

        void OnDummyHit(Health h, int amount, Vector2 _)
        {
            if (_paused || amount <= 0) return;
            _total += amount;
            _count++;
            if (amount > _maxHit) _maxHit = amount;
            _lastHit = Time.time;
        }

        bool Live => !_paused && _count > 0 && Time.time - _lastHit < IdleStop;

        void TickMeter()
        {
            // The clock runs only while hits keep landing, so idle time does not dilute DPS.
            if (Live) _elapsed += Time.deltaTime;
        }

        void ResetMeter()
        {
            _total = _count = _maxHit = 0;
            _elapsed = 0f;
            _lastHit = -99f;
            if (_dummy != null) _dummy.ReviveFull();
        }

        void TickProximity()
        {
            var hero = GameObject.FindGameObjectWithTag("Player");
            if (hero == null || _dummy == null) return;
            float d = Vector2.Distance(hero.transform.position, _dummy.transform.position);
            bool near = d <= OpenRange;
            if (near && !_wasNear && !_dismissed) _panel.SetActive(true);
            if (d > ForgetRange) _dismissed = false;
            _wasNear = near;
        }

        void TickKeys()
        {
            var kb = PlayerChat.Typing ? null : Keyboard.current; // chat line open: game keys off
            if (kb == null) return;
            if (kb.f9Key.wasPressedThisFrame) ResetMeter();
            if (kb.f10Key.wasPressedThisFrame) TogglePanel();
        }

        void TogglePanel()
        {
            bool show = !_panel.activeSelf;
            _panel.SetActive(show);
            _dismissed = !show;
        }

        static ClassCombat Classes()
        {
            var hero = GameObject.FindGameObjectWithTag("Player");
            return hero != null ? hero.GetComponent<ClassCombat>() : null;
        }

        void SelectClass(int slot)
        {
            var cc = Classes();
            if (cc != null) cc.Select(slot);
            RefreshChips();
        }

        // ------------------------------------------------------------------ refresh

        void RefreshStatus()
        {
            var tg = _nav.Training;
            var cfg = WorldCatalog.TrainingGround;
            int alive = tg != null ? tg.Alive : 0;
            _quai.text = "QUAI " + alive + "/" + cfg.maxAlive;
            _hoi.text = "HOI " + Mathf.RoundToInt(cfg.respawnSeconds) + "S";
            _expGain.text = "+" + (tg != null ? tg.ExpGained : 0);
            int exp = WorldSave.Exp;
            _lv.text = "LV " + WorldSave.Level;
            float t = (exp % WorldCatalog.ExpPerLevel) / (float)WorldCatalog.ExpPerLevel;
            _expFill.anchorMax = new Vector2(Mathf.Clamp01(t), 1f);
        }

        void RefreshChips()
        {
            var cc = Classes();
            int active = cc != null ? cc.ActiveSlot : -1;
            for (int i = 0; i < 5; i++)
            {
                bool on = i == active;
                _chipEdge[i].color = on ? GoldEdge : PlumBorder;
                _chipText[i].color = on ? Gold : White;
            }
        }

        void RefreshPanel()
        {
            if (!_panel.activeSelf) return;
            float time = Mathf.Max(_elapsed, _count > 0 ? 0.5f : 0f);
            float dps = time > 0f ? _total / time : 0f;
            _dps.text = dps.ToString("0.0");
            _dmg.text = "DMG  " + _total;
            _time.text = "TIME " + _elapsed.ToString("0.0") + "S";
            _hits.text = "HITS " + _count;
            _max.text = "MAX " + _maxHit;
            if (_paused) { _live.text = "PAUSE"; _live.color = Gold; }
            else if (Live) { _live.text = "LIVE"; _live.color = Green; }
            else { _live.text = "READY"; _live.color = Mute; }
            _playLabel.color = _paused ? Green : Mute;
            float hp = _dummy != null && _dummy.MaxHp > 0 ? (float)_dummy.CurrentHp / _dummy.MaxHp : 0f;
            _hpFill.anchorMax = new Vector2(Mathf.Clamp01(hp), 1f);
        }

        void RefreshWorldHits()
        {
            bool show = _dummy != null && _count > 0 && Time.time - _lastHit < 2f;
            if (_hitsGo.activeSelf != show) _hitsGo.SetActive(show);
            if (!show) return;
            _hitsWorld.text = "x" + _count + " HITS";
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 screen = cam.WorldToScreenPoint(_dummy.transform.position + new Vector3(0.9f, 1.1f, 0f));
            var canvasRt = (RectTransform)_canvas.transform;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRt, screen, null, out var local))
            {
                // Keep the whole label on screen (left pivot, so clamp by its width).
                float half = canvasRt.rect.width * 0.5f, w = _hitsRt.rect.width;
                local.x = Mathf.Clamp(local.x, -half + 4f, half - w - 4f);
                _hitsRt.anchoredPosition = local;
            }
        }

        // ------------------------------------------------------------------ build

        void Build()
        {
            // Under WorldWindows (260) so bag/map/services cover it; above the prompt plate (30).
            _canvas = UiKit.Canvas("TrainingHud", 200);
            _root = UiKit.Rect(_canvas.transform, "Root").gameObject;
            var rootRt = (RectTransform)_root.transform;
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = rootRt.offsetMax = Vector2.zero;
            _keyHints = !UiKit.IsMobile && Keyboard.current != null;

            BuildStatus(_root.transform);
            BuildPanel(_root.transform);
            BuildChips(_root.transform);
            BuildExit(_root.transform);
            BuildWorldHits(_canvas.transform);
            _panel.SetActive(false);
        }

        void BuildStatus(Transform parent)
        {
            var plate = Flat(parent, "TrainingStatus", Slate, SlateBorder, false);
            Lay(plate, UiLayout.Spec.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -32f), new Vector2(176f, 36f)),
                       UiLayout.Spec.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -98f), new Vector2(160f, 36f)));
            var p = plate.transform;
            _quai = TopLeft(p, "Quai", "QUAI 0/3", White, new Vector2(6f, -4f), 80f);
            _hoi = TopRight(p, "Hoi", "HOI 6S", Cyan, new Vector2(-6f, -4f), 64f);
            TopLeft(p, "ExpLabel", "EXP PHIEN", Mute, new Vector2(6f, -14f), 80f);
            _expGain = TopRight(p, "ExpGain", "+0", Green, new Vector2(-6f, -14f), 64f);
            _lv = TopLeft(p, "Lv", "LV 1", Gold, new Vector2(6f, -24f), 48f);
            var track = Box(p, "ExpTrack", ExpTrack);
            Lay(track, UiLayout.Spec.Stretch(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(20f, -26f), new Vector2(-52f, 4f)), default);
            _expFill = Box(track.transform, "Fill", ExpFill).rectTransform;
            _expFill.anchorMin = Vector2.zero;
            _expFill.anchorMax = new Vector2(0f, 1f);
            _expFill.offsetMin = _expFill.offsetMax = Vector2.zero;
        }

        void BuildPanel(Transform parent)
        {
            float hint = _keyHints ? 11f : 0f;
            var panel = Flat(parent, "DamageTest", Slate, SlateBorder, true);
            // Landscape: left column under the class row, clear of the fight and the prompt plate.
            // Portrait: 82 px strip docked just above joystick/hotbar. The header row carries LIVE + DPS (title hidden),
            // the rest moves up by Up, no key hints, so the hero and dummy stay visible above it.
            Lay(panel, UiLayout.Spec.At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(6f, -104f), new Vector2(196f, 108f + hint)),
                       UiLayout.Spec.At(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 128f), new Vector2(276f, 82f)));
            _panel = panel.gameObject;
            var p = panel.transform;
            TopLeft(p, "Title", "DAMAGE TEST", White, new Vector2(6f, -4f), 120f).gameObject.AddComponent<PortraitHide>();
            var close = Flat(p, "Close", Slate, Slate, true);
            Lay(close, UiLayout.Spec.At(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-1f, -1f), new Vector2(16f, 14f)), default);
            var closeBtn = close.gameObject.AddComponent<Button>();
            closeBtn.targetGraphic = close;
            closeBtn.onClick.AddListener(() => { _panel.SetActive(false); _dismissed = true; });
            var x = UiKit.Label(close.transform, "X", "X", 8, Mute, TextAnchor.MiddleCenter, false);
            UiKit.Fill(x);
            var line = Box(p, "Line", Cyan);
            Lay(line, UiLayout.Spec.Stretch(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f), new Vector2(-2f, 1f)),
                      UiLayout.Spec.Stretch(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -22f), new Vector2(-2f, 1f)));

            var tl = new Vector2(0f, 1f);
            _live = UiKit.Label(p, "Live", "READY", 8, Mute, TextAnchor.MiddleCenter);
            Lay(_live, UiLayout.Spec.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(80f, 10f)),
                       UiLayout.Spec.At(tl, tl, new Vector2(4f, -6f), new Vector2(48f, 10f)));
            var dpsLabel = UiKit.Label(p, "DpsLabel", "DPS", 8, Gold, TextAnchor.MiddleLeft);
            Lay(dpsLabel, UiLayout.Spec.At(tl, tl, new Vector2(10f, -36f), new Vector2(32f, 10f)),
                          UiLayout.Spec.At(tl, tl, new Vector2(60f, -6f), new Vector2(32f, 10f)));
            _dps = UiKit.Label(p, "Dps", "0.0", 16, Gold, TextAnchor.MiddleRight);
            Lay(_dps, UiLayout.Spec.At(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10f, -30f), new Vector2(140f, 20f)),
                      UiLayout.Spec.At(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-22f, -2f), new Vector2(140f, 20f)));
            _dmg = TopLeft(p, "Dmg", "DMG  0", White, new Vector2(8f, -54f), 96f, Up);
            _time = TopRight(p, "Time", "TIME 0.0S", White, new Vector2(-8f, -54f), 96f, Up);
            _hits = TopLeft(p, "Hits", "HITS 0", White, new Vector2(8f, -64f), 96f, Up);
            _max = TopRight(p, "Max", "MAX 0", Gold, new Vector2(-8f, -64f), 96f, Up);

            var track = Box(p, "DummyHp", HpTrack);
            Lay(track, UiLayout.Spec.Stretch(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(-16f, 5f)),
                       UiLayout.Spec.Stretch(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -76f + Up), new Vector2(-16f, 5f)));
            _hpFill = Box(track.transform, "Fill", HpRed).rectTransform;
            _hpFill.anchorMin = Vector2.zero;
            _hpFill.anchorMax = Vector2.one;
            _hpFill.offsetMin = _hpFill.offsetMax = Vector2.zero;

            string[] labels = { "RESET", "PAUSE", "PLAY" };
            for (int i = 0; i < 3; i++)
            {
                int k = i;
                bool play = i == 2;
                var b = Flat(p, "Btn" + labels[i], play ? GreenFill : SlateRow, play ? Green : SlateBorder, true);
                var spec = new UiLayout.Spec
                {
                    set = true, anchorMin = new Vector2(i / 3f, 0f), anchorMax = new Vector2((i + 1) / 3f, 0f), pivot = new Vector2(0.5f, 0f)
                };
                // Thirds minus 12 px, shifted so the row has an 8 px margin and 10 px gaps.
                var land = spec; land.pos = new Vector2((1 - i) * 2f, 6f + hint); land.size = new Vector2(-12f, 16f);
                var port = spec; port.pos = new Vector2((1 - i) * 2f, 4f); port.size = new Vector2(-12f, 20f);
                Lay(b, land, port);
                var btn = b.gameObject.AddComponent<Button>();
                btn.targetGraphic = b.transform.GetChild(0).GetComponent<Image>();
                btn.onClick.AddListener(() => OnPanelButton(k));
                var t = UiKit.Label(b.transform, "Label", labels[i], 8, play ? Green : White, TextAnchor.MiddleCenter);
                UiKit.Fill(t);
                if (play) _playLabel = t;
            }
            if (_keyHints)
            {
                var h = UiKit.Label(p, "Keys", "F9 RESET  F10 HIEN/AN", 8, Mute, TextAnchor.MiddleLeft);
                Lay(h, UiLayout.Spec.At(Vector2.zero, Vector2.zero, new Vector2(8f, 3f), new Vector2(184f, 9f)), default);
                h.gameObject.AddComponent<PortraitHide>();
            }
        }

        void OnPanelButton(int i)
        {
            if (i == 0) ResetMeter();
            else if (i == 1) _paused = true;
            else _paused = false;
        }

        void BuildChips(Transform parent)
        {
            for (int i = 0; i < 5; i++)
            {
                int slot = i;
                var chip = Flat(parent, "Class" + ClassLetters[i], PlumCard, PlumBorder, true);
                Lay(chip, UiLayout.Spec.At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f + i * 26f, -66f), new Vector2(22f, 22f)),
                          UiLayout.Spec.At(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-6f, -100f - i * 28f), new Vector2(24f, 24f)));
                // Active chip: the 1 px border turns gold (2 px reads too heavy at Px 2).
                _chipEdge[i] = chip;
                var btn = chip.gameObject.AddComponent<Button>();
                btn.targetGraphic = chip.transform.GetChild(0).GetComponent<Image>();
                btn.onClick.AddListener(() => SelectClass(slot));
                _chipText[i] = UiKit.Label(chip.transform, "Letter", ClassLetters[i], 8, White, TextAnchor.MiddleCenter);
                UiKit.Fill(_chipText[i]);
                if (_keyHints)
                {
                    var k = UiKit.Label(chip.transform, "Key", "F" + (i + 1), 8, Mute, TextAnchor.MiddleCenter);
                    Lay(k, UiLayout.Spec.At(new Vector2(0.5f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, -2f), new Vector2(24f, 10f)), default);
                    // F-key hints under the landscape row only; portrait chips hug the right edge.
                    k.gameObject.AddComponent<PortraitHide>();
                }
            }
            var label = UiKit.Label(parent, "ClassLabel", "CLASS", 8, Mute, TextAnchor.MiddleLeft);
            Lay(label, UiLayout.Spec.At(new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(140f, -72f), new Vector2(48f, 10f)), default);
            label.gameObject.AddComponent<PortraitHide>();
        }

        void BuildExit(Transform parent)
        {
            var b = Flat(parent, "BackToVillage", Plum, PlumBorder, true);
            Lay(b, UiLayout.Spec.At(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-6f, -52f), new Vector2(82f, 18f)),
                   UiLayout.Spec.At(new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-6f, -74f), new Vector2(82f, 18f)));
            var btn = b.gameObject.AddComponent<Button>();
            btn.targetGraphic = b.transform.GetChild(0).GetComponent<Image>();
            btn.onClick.AddListener(() => _nav.Show(WorldZone.Hub));
            var t = UiKit.Label(b.transform, "Label", "< VE LANG", 8, White, TextAnchor.MiddleCenter);
            UiKit.Fill(t);
        }

        void BuildWorldHits(Transform parent)
        {
            _hitsWorld = UiKit.Label(parent, "Hits", "", 8, White, TextAnchor.MiddleLeft);
            _hitsWorld.gameObject.AddComponent<Outline>().effectColor = Pal.Ink;
            var layout = _hitsWorld.GetComponent<UiLayout>();
            layout.Both(UiLayout.Spec.At(UiKit.Center, new Vector2(0f, 0.5f), Vector2.zero, new Vector2(96f, 10f)));
            // Position is driven every frame from the dummy; the layout only owns size and font.
            layout.portrait.set = false;
            layout.landscape.set = false;
            _hitsRt = _hitsWorld.rectTransform;
            _hitsGo = _hitsWorld.gameObject;
            _hitsGo.SetActive(false);
        }

        // ------------------------------------------------------------------ helpers

        /// <summary>Flat 1 px bordered box (reference style). Returns the border; the fill is its first child.</summary>
        static Image Flat(Transform parent, string name, Color fill, Color border, bool raycast)
        {
            var b = UiKit.Rect(parent, name).gameObject.AddComponent<Image>();
            b.color = border;
            b.raycastTarget = raycast;
            var f = UiKit.Rect(b.transform, "Fill").gameObject.AddComponent<Image>();
            f.color = fill;
            f.raycastTarget = false;
            UiKit.Fill(f, 1f);
            return b;
        }

        static Image Box(Transform parent, string name, Color c)
        {
            var img = UiKit.Rect(parent, name).gameObject.AddComponent<Image>();
            img.color = c;
            img.raycastTarget = false;
            return img;
        }

        /// <summary>Landscape and portrait specs; a default portrait spec falls back to landscape.</summary>
        static void Lay(Component c, UiLayout.Spec land, UiLayout.Spec port)
        {
            var l = c.GetComponent<UiLayout>();
            if (l == null) l = c.gameObject.AddComponent<UiLayout>();
            l.landscape = land;
            l.portrait = port;
            l.Apply(true);
        }

        /// <summary>Portrait Damage Test stat rows and HP bar sit this many kit px higher (LIVE + DPS share the header).</summary>
        const float Up = 27f;

        /// <summary>Top-left label; portraitUp raises it in portrait only.</summary>
        static Text TopLeft(Transform p, string name, string text, Color c, Vector2 pos, float w, float portraitUp = 0f)
        {
            var t = UiKit.Label(p, name, text, 8, c, TextAnchor.MiddleLeft);
            var a = new Vector2(0f, 1f);
            Lay(t, UiLayout.Spec.At(a, a, pos, new Vector2(w, 10f)),
                   portraitUp != 0f ? UiLayout.Spec.At(a, a, pos + new Vector2(0f, portraitUp), new Vector2(w, 10f)) : default);
            return t;
        }

        static Text TopRight(Transform p, string name, string text, Color c, Vector2 pos, float w, float portraitUp = 0f)
        {
            var t = UiKit.Label(p, name, text, 8, c, TextAnchor.MiddleRight);
            var a = new Vector2(1f, 1f);
            Lay(t, UiLayout.Spec.At(a, a, pos, new Vector2(w, 10f)),
                   portraitUp != 0f ? UiLayout.Spec.At(a, a, pos + new Vector2(0f, portraitUp), new Vector2(w, 10f)) : default);
            return t;
        }
    }

    /// <summary>Hides its GameObject while the portrait profile is active.</summary>
    public class PortraitHide : MonoBehaviour
    {
        Text _text;
        void Awake() => _text = GetComponent<Text>();
        void LateUpdate()
        {
            if (_text != null) _text.enabled = !UiKit.IsPortrait;
        }
    }
}
