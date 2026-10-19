using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>
    /// Portrait HUD (9:16): HP top, combo upper, skills bottom-center (2 rows), R Attack bottom-right.
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
            public Image readyGlow;
            public Image cooldownOverlay;
            public Text cooldownText;
            public Text keyText;
            public Color iconColor;
            public bool isPrimary;
            public RectTransform root;
        }

        Slot[] _slots;
        Text _comboText;
        RectTransform _comboRt;
        float _comboPop;
        float _comboEmphasis;
        Image _hpFill;
        RectTransform _hpFillRt;
        Image _hpTrail;
        RectTransform _hpTrailRt;
        RectTransform _hpRoot;
        float _hpWidth = 200f;
        const float HpBarHeight = 10f;
        float _hpShown = 1f;      // red fill, snaps to current HP
        float _hpTrailValue = 1f; // pale bar that drains after a short delay
        float _hpTrailDelay;
        float _hpShake;
        Health _playerHealth;
        bool[] _wasReady;
        float[] _readyFlash;
        float _comboFade;
        const float ReadyFlashTime = 0.22f;
        const float ComboFadeTime = 0.35f;
        RectTransform _attackPunchRt;
        float _attackPunch;
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
            SyncCombatCanvasVisibility();
            RefreshCooldowns();
            TickSlotGlows();
            TickHpBar();
            TickComboFade();
            ApplySafeArea();
            if (_attackPunch > 0f && _attackPunchRt != null)
            {
                _attackPunch -= Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(_attackPunch / 0.12f);
                _attackPunchRt.localScale = Vector3.one * (1f - 0.08f * t);
            }
            else if (_attackPunchRt != null)
                _attackPunchRt.localScale = Vector3.one;

            if (_comboRt == null) return;
            if (_comboPop > 0f)
            {
                _comboPop -= Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(_comboPop / 0.14f);
                float punch = 1f + 0.18f * t + (_comboEmphasis * 0.12f * t);
                _comboRt.localScale = new Vector3(punch, punch, 1f);
            }
            else
                _comboRt.localScale = Vector3.one;
        }

        RectTransform _safeRoot;
        GameObject _combatCanvasGo;

        void SyncCombatCanvasVisibility()
        {
            if (_combatCanvasGo == null)
            {
                var t = transform.Find("CombatCanvas");
                if (t != null) _combatCanvasGo = t.gameObject;
            }
            if (_combatCanvasGo == null) return;
            bool show = LevelFlow.Instance == null ||
                        (LevelFlow.Instance.State != LevelState.MainMenu &&
                         LevelFlow.Instance.State != LevelState.Loading);
            if (_combatCanvasGo.activeSelf != show)
                _combatCanvasGo.SetActive(show);
        }

        void ApplySafeArea()
        {
            if (_safeRoot == null) return;
            Rect safe = Screen.safeArea;
            float w = Mathf.Max(1f, Screen.width);
            float h = Mathf.Max(1f, Screen.height);
            _safeRoot.anchorMin = new Vector2(safe.xMin / w, safe.yMin / h);
            _safeRoot.anchorMax = new Vector2(safe.xMax / w, safe.yMax / h);
            _safeRoot.offsetMin = Vector2.zero;
            _safeRoot.offsetMax = Vector2.zero;
        }

        void BuildUi()
        {
            EnsureSprite();
            EnsureEventSystem();

            // Only clear prior combat HUD — never wipe LevelFlow / other Systems children.
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                var child = transform.GetChild(i);
                if (child.name == "CombatCanvas" || child.name.StartsWith("CombatCanvas"))
                    Destroy(child.gameObject);
            }

            var canvasGo = new GameObject("CombatCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(576f, 1024f);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            var safeGo = new GameObject("SafeArea");
            safeGo.transform.SetParent(canvasGo.transform, false);
            _safeRoot = safeGo.AddComponent<RectTransform>();
            ApplySafeArea();

            // Top HP (portrait): 2px dark frame, inner well, pale damage trail under the red fill.
            var hpFrame = CreateImage(_safeRoot, "HeroHpFrame", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -22f), new Vector2(_hpWidth + 8f, HpBarHeight + 8f), new Color(0.05f, 0.03f, 0.04f, 0.95f));
            hpFrame.rectTransform.pivot = new Vector2(0.5f, 1f);
            _hpRoot = hpFrame.rectTransform;
            var hpRim = CreateImage(hpFrame.transform, "Rim", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(_hpWidth + 4f, HpBarHeight + 4f), new Color(0.55f, 0.42f, 0.3f, 1f));
            var hpWell = CreateImage(hpRim.transform, "Well", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(_hpWidth, HpBarHeight), new Color(0.16f, 0.06f, 0.07f, 1f));
            _hpTrail = CreateImage(hpWell.transform, "HeroHpTrail", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(_hpWidth, HpBarHeight), new Color(1f, 0.86f, 0.62f, 1f));
            _hpTrail.rectTransform.pivot = new Vector2(0f, 0.5f);
            _hpTrailRt = _hpTrail.rectTransform;
            _hpFill = CreateImage(hpWell.transform, "HeroHpFill", new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(_hpWidth, HpBarHeight), new Color(0.85f, 0.12f, 0.12f, 1f));
            _hpFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            _hpFillRt = _hpFill.rectTransform;
            // 2px highlight along the top of the fill reads as a bevel at pixel scale.
            var hpShine = CreateImage(_hpFill.transform, "Shine", new Vector2(0f, 1f), new Vector2(1f, 1f),
                new Vector2(0f, -1f), new Vector2(0f, 2f), new Color(1f, 0.55f, 0.5f, 0.85f));
            hpShine.rectTransform.pivot = new Vector2(0.5f, 1f);

            // Combo (upper, avoids covering hero center)
            var comboGo = new GameObject("ComboText");
            comboGo.transform.SetParent(_safeRoot, false);
            _comboRt = comboGo.AddComponent<RectTransform>();
            _comboRt.anchorMin = _comboRt.anchorMax = new Vector2(0.5f, 0.82f);
            _comboRt.sizeDelta = new Vector2(260f, 48f);
            _comboText = comboGo.AddComponent<Text>();
            _comboText.font = ResolveUiFont();
            _comboText.fontSize = 18;
            _comboText.alignment = TextAnchor.MiddleCenter;
            _comboText.color = Color.white;
            _comboText.horizontalOverflow = HorizontalWrapMode.Overflow;
            _comboText.raycastTarget = false;
            WorldLabelsCanvas.ApplyPixelOutline(_comboText, Color.black);
            _comboText.text = "";

            // A) BOTTOM-LEFT — joystick pad (mobile). Low-alpha placeholder on PC.
            var stick = CreateImage(_safeRoot, "JoystickPad", new Vector2(0f, 0f), new Vector2(0f, 0f),
                new Vector2(16f, 14f), new Vector2(110f, 110f), new Color(1f, 1f, 1f, 0.12f));
            stick.rectTransform.pivot = new Vector2(0f, 0f);
            stick.raycastTarget = false;
            var stickKnob = CreateImage(stick.transform, "JoystickKnob", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(48f, 48f), new Color(1f, 1f, 1f, 0.22f));
            stickKnob.raycastTarget = false;

            // B) BOTTOM-CENTER — 5 skills in 3+2 (V,C centered under 2,3,4).
            var bar = new GameObject("SkillBar");
            bar.transform.SetParent(_safeRoot, false);
            var barRt = bar.AddComponent<RectTransform>();
            barRt.anchorMin = barRt.anchorMax = new Vector2(0.5f, 0f);
            barRt.pivot = new Vector2(0.5f, 0f);
            // Slight optical nudge right so 3+2 reads centered against large R Attack.
            barRt.anchoredPosition = new Vector2(6f, 14f);
            barRt.sizeDelta = new Vector2(240f, 140f);

            var skillDefs = new (SkillId id, string key, string icon, int row, int col)[]
            {
                (SkillId.FireDash, "2", "flame", 1, 0),
                (SkillId.MultiSlash, "3", "slash", 1, 1),
                (SkillId.MawBite, "4", "jaws", 1, 2),
                (SkillId.Bone, "V", "bone", 0, 0),
                (SkillId.Mace, "C", "mace", 0, 1),
            };

            // C) BOTTOM-RIGHT — large R Attack
            var attackRoot = new GameObject("AttackButton");
            attackRoot.transform.SetParent(_safeRoot, false);
            var attackRt = attackRoot.AddComponent<RectTransform>();
            attackRt.anchorMin = attackRt.anchorMax = new Vector2(1f, 0f);
            attackRt.pivot = new Vector2(1f, 0f);
            attackRt.anchoredPosition = new Vector2(-16f, 14f);
            attackRt.sizeDelta = new Vector2(100f, 100f);
            _attackPunchRt = attackRt;

            _slots = new Slot[skillDefs.Length + 1];
            const float slotSize = 58f;
            const float gap = 10f;
            float topRowWidth = 3f * slotSize + 2f * gap;
            float botRowWidth = 2f * slotSize + gap;
            for (int i = 0; i < skillDefs.Length; i++)
            {
                float rowWidth = skillDefs[i].row == 1 ? topRowWidth : botRowWidth;
                float startX = -rowWidth * 0.5f + slotSize * 0.5f;
                float x = startX + skillDefs[i].col * (slotSize + gap);
                float y = 14f + skillDefs[i].row * (slotSize + gap);
                _slots[i] = BuildSlot(bar.transform, skillDefs[i].id, skillDefs[i].key, UiPixelArt.Icon(skillDefs[i].icon),
                    new Vector2(x, y), slotSize, false);
            }

            _slots[skillDefs.Length] = BuildSlot(attackRoot.transform, SkillId.Sword, "R", UiPixelArt.Icon("sword"),
                Vector2.zero, 96f, true);
            WireAttackHold(_slots[skillDefs.Length]);
        }

        Slot BuildSlot(Transform parent, SkillId id, string key, Sprite iconSprite, Vector2 pos, float slotSize, bool primary)
        {
            var frame = CreateImage(parent, $"Slot_{key}", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                pos, new Vector2(slotSize, slotSize), Color.white);
            frame.sprite = UiPixelArt.Frame;
            frame.raycastTarget = true;

            var glow = CreateImage(frame.transform, "ReadyGlow", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(slotSize + 8f, slotSize + 8f), new Color(0.55f, 0.9f, 1f, 0.35f));
            glow.enabled = false;
            glow.raycastTarget = false;

            var icon = CreateImage(frame.transform, "Icon", new Vector2(0.5f, 0.55f), new Vector2(0.5f, 0.55f),
                Vector2.zero, new Vector2(slotSize * 0.72f, slotSize * 0.72f), Color.white);
            icon.sprite = iconSprite;
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            var select = CreateImage(frame.transform, "Select", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(slotSize, slotSize), Color.white);
            select.sprite = UiPixelArt.Select;
            select.enabled = false;
            select.raycastTarget = false;

            var cd = CreateImage(frame.transform, "Cooldown", new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(slotSize - 10f, slotSize - 10f), new Color(0f, 0f, 0f, 0.65f));
            cd.type = Image.Type.Filled;
            cd.fillMethod = Image.FillMethod.Radial360;
            cd.fillOrigin = (int)Image.Origin360.Top;
            cd.fillClockwise = false;
            cd.fillAmount = 0f;
            cd.raycastTarget = false;

            var cdTextGo = new GameObject("CdText");
            cdTextGo.transform.SetParent(frame.transform, false);
            var cdRt = cdTextGo.AddComponent<RectTransform>();
            cdRt.anchorMin = cdRt.anchorMax = new Vector2(0.5f, 0.5f);
            cdRt.sizeDelta = new Vector2(slotSize, slotSize);
            var cdText = cdTextGo.AddComponent<Text>();
            cdText.font = ResolveUiFont();
            cdText.fontSize = primary ? 20 : 16;
            cdText.alignment = TextAnchor.MiddleCenter;
            cdText.color = Color.white;
            cdText.text = "";
            cdText.raycastTarget = false;
            WorldLabelsCanvas.ApplyPixelOutline(cdText, Color.black);

            var keyGo = new GameObject("Key");
            keyGo.transform.SetParent(frame.transform, false);
            var keyRt = keyGo.AddComponent<RectTransform>();
            keyRt.anchorMin = new Vector2(0.5f, 0f);
            keyRt.anchorMax = new Vector2(0.5f, 0f);
            keyRt.pivot = new Vector2(0.5f, 0f);
            keyRt.anchoredPosition = new Vector2(0f, 2f);
            keyRt.sizeDelta = new Vector2(slotSize, 22f);
            var keyText = keyGo.AddComponent<Text>();
            keyText.font = ResolveUiFont();
            keyText.fontSize = primary ? 18 : 15;
            keyText.alignment = TextAnchor.LowerCenter;
            keyText.color = Color.white;
            keyText.text = key;
            keyText.raycastTarget = false;
            WorldLabelsCanvas.ApplyPixelOutline(keyText, Color.black);

            if (!primary)
            {
                var button = frame.gameObject.AddComponent<Button>();
                button.targetGraphic = frame;
                var captured = id;
                button.onClick.AddListener(() => combat?.TryCast(captured));
            }

            return new Slot
            {
                id = id,
                key = key,
                frame = frame,
                icon = icon,
                select = select,
                readyGlow = glow,
                cooldownOverlay = cd,
                cooldownText = cdText,
                keyText = keyText,
                iconColor = Color.white,
                isPrimary = primary,
                root = frame.rectTransform
            };
        }

        void WireAttackHold(Slot slot)
        {
            var trigger = slot.frame.gameObject.AddComponent<EventTrigger>();
            var down = new EventTrigger.Entry { eventID = EventTriggerType.PointerDown };
            down.callback.AddListener(_ =>
            {
                _attackPunch = 0.12f;
                combat?.SetSwordHeld(true);
                combat?.TryCast(SkillId.Sword);
            });
            var up = new EventTrigger.Entry { eventID = EventTriggerType.PointerUp };
            up.callback.AddListener(_ => combat?.SetSwordHeld(false));
            var exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
            exit.callback.AddListener(_ => combat?.SetSwordHeld(false));
            trigger.triggers.Add(down);
            trigger.triggers.Add(up);
            trigger.triggers.Add(exit);
        }

        static void EnsureEventSystem()
        {
            if (FindFirstObjectByType<EventSystem>() != null) return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            // Input System package replaces StandaloneInputModule when present.
            var inputModType = System.Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            if (inputModType != null)
                go.AddComponent(inputModType);
            else
                go.AddComponent<StandaloneInputModule>();
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
            if (hits < 2)
            {
                // Keep the last count on screen and fade it instead of popping out.
                _comboPop = 0f;
                _comboEmphasis = 0f;
                _comboFade = string.IsNullOrEmpty(_comboText.text) ? 0f : ComboFadeTime;
                return;
            }
            _comboFade = 0f;
            _comboText.text = $"x{hits} HITS";
            _comboPop = 0.14f;
            if (hits >= 20)
            {
                _comboEmphasis = 1.35f;
                _comboText.fontSize = 22;
                _comboText.color = new Color(1f, 0.92f, 0.55f, 1f);
            }
            else if (hits >= 10)
            {
                _comboEmphasis = 1f;
                _comboText.fontSize = 20;
                _comboText.color = new Color(0.85f, 0.95f, 1f, 1f);
            }
            else if (hits >= 5)
            {
                _comboEmphasis = 0.55f;
                _comboText.fontSize = 18;
                _comboText.color = Color.white;
            }
            else
            {
                _comboEmphasis = 0.2f;
                _comboText.fontSize = 16;
                _comboText.color = Color.white;
            }
        }

        void TickComboFade()
        {
            if (_comboText == null || _comboFade <= 0f) return;
            _comboFade -= Time.unscaledDeltaTime;
            var c = _comboText.color;
            if (_comboFade <= 0f)
            {
                _comboText.text = "";
                c.a = 1f;
            }
            else
                c.a = _comboFade / ComboFadeTime;
            _comboText.color = c;
        }

        void RefreshCooldowns()
        {
            if (_slots == null || combat == null) return;
            if (_wasReady == null || _wasReady.Length != _slots.Length)
            {
                _wasReady = new bool[_slots.Length];
                _readyFlash = new float[_slots.Length];
                for (int i = 0; i < _wasReady.Length; i++) _wasReady[i] = true;
            }
            for (int i = 0; i < _slots.Length; i++)
            {
                float n = combat.GetCooldownNormalized(_slots[i].id);
                float rem = combat.GetCooldownRemaining(_slots[i].id);
                bool ready = rem <= 0.01f;
                _slots[i].cooldownOverlay.fillAmount = n;
                _slots[i].cooldownText.text = rem > 0.05f ? rem.ToString("0.0") : "";
                if (_slots[i].icon != null)
                    _slots[i].icon.color = ready ? Color.white : new Color(0.75f, 0.75f, 0.75f, 1f);

                // Skills (not the 0.35s R spam) flash and tick once when they come off cooldown.
                if (ready && !_wasReady[i] && !_slots[i].isPrimary)
                {
                    _readyFlash[i] = ReadyFlashTime;
                    GameAudio.Play(Sfx.UiReady);
                }
                _wasReady[i] = ready;
            }
        }

        // Once per frame from Update (RefreshCooldowns also runs from events, several times a frame).
        void TickSlotGlows()
        {
            if (_slots == null || _wasReady == null) return;
            for (int i = 0; i < _slots.Length; i++)
                UpdateSlotGlow(i, _wasReady[i]);
        }

        void UpdateSlotGlow(int i, bool ready)
        {
            var glow = _slots[i].readyGlow;
            if (glow == null) return;
            if (_slots[i].isPrimary)
            {
                glow.enabled = ready;
                return;
            }
            if (_readyFlash[i] <= 0f)
            {
                glow.enabled = false;
                _slots[i].root.localScale = Vector3.one;
                return;
            }
            _readyFlash[i] -= Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_readyFlash[i] / ReadyFlashTime);
            glow.enabled = true;
            glow.color = new Color(1f, 1f, 1f, 0.75f * t);
            _slots[i].root.localScale = Vector3.one * (1f + 0.12f * t);
        }

        void RefreshHp()
        {
            if (_hpFillRt == null || _playerHealth == null) return;
            float n = _playerHealth.Normalized;
            if (n < _hpShown - 0.0001f)
            {
                _hpTrailDelay = 0.35f; // hold the lost chunk visible before it drains
                _hpShake = 0.18f;
            }
            else if (n > _hpTrailValue)
                _hpTrailValue = n; // heals snap the trail up too
            _hpShown = n;
            _hpFillRt.sizeDelta = new Vector2(_hpWidth * n, HpBarHeight);
        }

        void TickHpBar()
        {
            if (_hpTrailRt == null) return;
            float dt = Time.unscaledDeltaTime;
            if (_hpTrailDelay > 0f)
                _hpTrailDelay -= dt;
            else if (_hpTrailValue > _hpShown)
                _hpTrailValue = Mathf.MoveTowards(_hpTrailValue, _hpShown, 0.6f * dt);
            _hpTrailRt.sizeDelta = new Vector2(_hpWidth * _hpTrailValue, HpBarHeight);

            // Low HP: fill pulses toward a brighter red.
            if (_hpFill != null)
            {
                var baseRed = new Color(0.85f, 0.12f, 0.12f, 1f);
                if (_hpShown > 0f && _hpShown < 0.3f)
                {
                    float p = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f);
                    _hpFill.color = Color.Lerp(baseRed, new Color(1f, 0.45f, 0.35f, 1f), p);
                }
                else
                    _hpFill.color = baseRed;
            }

            if (_hpRoot != null)
            {
                float x = 0f;
                if (_hpShake > 0f)
                {
                    _hpShake -= dt;
                    x = (Mathf.Repeat(_hpShake * 40f, 1f) < 0.5f ? -2f : 2f); // 2px pixel jitter
                }
                _hpRoot.anchoredPosition = new Vector2(x, -22f);
            }
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetUiStatics()
        {
            _uiSprite = null;
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

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _frame = null;
            _select = null;
            Icons.Clear();
        }

        public static Sprite Frame => Live(_frame) ? _frame : _frame = Sheet(32, FrameRows);
        public static Sprite Select => Live(_select) ? _select : _select = Sheet(32, SelectRows);

        public static Sprite Icon(string name)
        {
            if (Icons.TryGetValue(name, out var cached) && Live(cached))
                return cached;
            var sprite = Sheet(16, Rows(name));
            Icons[name] = sprite;
            return sprite;
        }

        static bool Live(Sprite sprite) => sprite != null;

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

