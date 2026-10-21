using UnityEngine;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>
    /// Always-on enemy name and level on the shared WorldLabelsCanvas.
    /// The HP bar appears only after this mob actually loses HP, then hides again.
    /// Labels stay visible while the mob is alive and on-camera; nearby labels
    /// nudge apart instead of being culled.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class EnemyWorldUI : MonoBehaviour
    {
        [SerializeField] Vector3 worldOffset = new Vector3(0f, 0.95f, 0f);
        [SerializeField] float barWidth = 56f;
        [SerializeField] float barHeight = 6f;
        [SerializeField] float maxNudge = 52f;
        [SerializeField] float showHpAfterHitSeconds = 3f;

        Health _health;
        EnemyController _enemy;
        BossWarlordController _boss;
        bool _alwaysHp;
        bool AlwaysHp => _boss != null || _alwaysHp;
        RectTransform _root;
        Text _label;
        Image _barBg;
        Image _barFill;
        Image[] _statusIcons;
        Vector2 _layoutNudge;
        Vector2 _nudgeTarget;
        float _desiredY;
        float _hpVisibleUntil = -1f;
        static Sprite _white;
        static Transform _hero;
        static readonly System.Collections.Generic.List<EnemyWorldUI> Active = new();
        static int _layoutFrame = -1;
        static Camera _cam;
        const float NudgeSmooth = 14f;

        public string LabelText
        {
            get
            {
                if (_boss != null) return _boss.DisplayName;
                if (_enemy != null && !string.IsNullOrEmpty(_enemy.DisplayName))
                    return _enemy.DisplayName;
                return name;
            }
        }

        public Vector2 ScreenAnchor { get; private set; }
        public bool IsOnScreen { get; private set; }
        public RectTransform Root => _root;
        public Vector2 LayoutNudge => _layoutNudge;

        /// <summary>R3D: count on-screen label pairs still overlapping after layout (Name+HP box ≈ 86×34).</summary>
        public static int CountOverlappingPairs(float minX = 104f, float minY = 36f)
        {
            int overlaps = 0;
            for (int i = 0; i < Active.Count; i++)
            {
                var a = Active[i];
                if (a == null || !a.IsOnScreen || a._root == null) continue;
                Vector2 pa = a.ScreenAnchor + a._layoutNudge;
                for (int j = i + 1; j < Active.Count; j++)
                {
                    var b = Active[j];
                    if (b == null || !b.IsOnScreen || b._root == null) continue;
                    Vector2 pb = b.ScreenAnchor + b._layoutNudge;
                    if (Mathf.Abs(pb.x - pa.x) < minX && Mathf.Abs(pb.y - pa.y) < minY)
                        overlaps++;
                }
            }
            return overlaps;
        }

        public static int ActiveOnScreenCount
        {
            get
            {
                int n = 0;
                for (int i = 0; i < Active.Count; i++)
                    if (Active[i] != null && Active[i].IsOnScreen) n++;
                return n;
            }
        }

        void Awake()
        {
            _health = GetComponent<Health>();
            _enemy = GetComponent<EnemyController>();
            _boss = GetComponent<BossWarlordController>();
            _alwaysHp = GetComponent<R2CBossMark>() != null;
            WorldLabelsCanvas.Ensure();
            Build();
            _health.Changed += OnChanged;
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
            Refresh();
            SetLabelText(LabelText);
            ResetHpReveal();
        }

        void OnDestroy()
        {
            if (_health != null)
            {
                _health.Changed -= OnChanged;
                _health.Damaged -= OnDamaged;
                _health.Died -= OnDied;
            }
            DestroyRoot();
        }

        void OnEnable()
        {
            if (!Active.Contains(this))
                Active.Add(this);
            // Pool reuse must not keep the previous mob's reveal timer.
            if (_barBg != null)
                ResetHpReveal();
        }

        void OnDisable()
        {
            Active.Remove(this);
            if (_root != null)
                _root.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (_root == null || WorldLabelsCanvas.Instance == null || _health == null || _health.IsDead)
                return;

            if (_layoutFrame != Time.frameCount)
            {
                _layoutFrame = Time.frameCount;
                ResolveHero();
                LayoutBatch();
            }

            if (_label != null && _label.text != LabelText)
                SetLabelText(LabelText);

            TickHpBar();

            if (!IsOnScreen)
            {
                if (_root.gameObject.activeSelf)
                    _root.gameObject.SetActive(false);
                return;
            }

            if (!_root.gameObject.activeSelf)
                _root.gameObject.SetActive(true);

            // Smooth toward layout target so labels don't jitter when mobs pack/unpack.
            float t = 1f - Mathf.Exp(-NudgeSmooth * Time.unscaledDeltaTime);
            _layoutNudge = Vector2.Lerp(_layoutNudge, _nudgeTarget, t);
            Vector2 screen = ScreenAnchor + _layoutNudge;
            screen = NudgeOffHero(screen);
            _root.position = ClampLabel(screen);
            ApplyAlpha(1f);
            RefreshStatus();
        }

        static void ResolveHero()
        {
            if (_hero == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    _hero = player.transform;
            }
            _cam = Camera.main;
        }

        static void LayoutBatch()
        {
            for (int i = 0; i < Active.Count; i++)
            {
                var ui = Active[i];
                if (ui == null) continue;
                ui._nudgeTarget = Vector2.zero;
                ui.RefreshAnchor();
            }

            // Stable order: stack by world Y so labels don't swap lanes every frame.
            Active.Sort(static (a, b) =>
            {
                if (a == null && b == null) return 0;
                if (a == null) return 1;
                if (b == null) return -1;
                int byY = a.transform.position.y.CompareTo(b.transform.position.y);
                return byY != 0 ? byY : a.GetInstanceID().CompareTo(b.GetInstanceID());
            });

            bool sandbox = ReferenceSandboxController.IsActive;
            // R3D: keep-out sized to "Goblin Lv.3" + HP bar footprint (~110×40 screen px at 576×1024).
            float minX = sandbox ? 118f : 104f;
            float minY = sandbox ? 42f : 36f;
            float limit = sandbox ? 96f : 72f;

            // Resolve overlaps into _nudgeTarget (then smooth toward it in LateUpdate).
            for (int pass = 0; pass < 5; pass++)
            {
                for (int i = 0; i < Active.Count; i++)
                {
                    var a = Active[i];
                    if (a == null || !a.IsOnScreen) continue;
                    Vector2 pa = a.ScreenAnchor + a._nudgeTarget;
                    for (int j = i + 1; j < Active.Count; j++)
                    {
                        var b = Active[j];
                        if (b == null || !b.IsOnScreen) continue;
                        Vector2 pb = b.ScreenAnchor + b._nudgeTarget;
                        float dx = pb.x - pa.x;
                        float dy = pb.y - pa.y;
                        if (Mathf.Abs(dx) >= minX || Mathf.Abs(dy) >= minY)
                            continue;

                        // Prefer vertical separation by world-Y order (i above j on screen → push apart).
                        float pushY = (minY - Mathf.Abs(dy)) * 0.55f + 3f;
                        a._nudgeTarget.y -= pushY * 0.5f;
                        b._nudgeTarget.y += pushY * 0.5f;

                        if (Mathf.Abs(dx) < minX * 0.6f)
                        {
                            float pushX = (minX * 0.6f - Mathf.Abs(dx)) * 0.5f;
                            float dirX = a.transform.position.x <= b.transform.position.x ? -1f : 1f;
                            a._nudgeTarget.x += pushX * dirX * 0.5f;
                            b._nudgeTarget.x -= pushX * dirX * 0.5f;
                        }
                    }
                }
            }

            for (int i = 0; i < Active.Count; i++)
            {
                var ui = Active[i];
                if (ui == null) continue;
                ui._nudgeTarget.x = Mathf.Clamp(ui._nudgeTarget.x, -limit, limit);
                ui._nudgeTarget.y = Mathf.Clamp(ui._nudgeTarget.y, -limit, limit);
            }
        }

        void RefreshAnchor()
        {
            float heightBoost = 0.95f;
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
                heightBoost = Mathf.Max(0.75f, sr.bounds.size.y * 0.55f + 0.35f);
            if (_boss != null)
                heightBoost += 0.35f;

            Vector3 world = transform.position + new Vector3(worldOffset.x, heightBoost, 0f);
            Vector2 screen = WorldLabelsCanvas.WorldToSnappedScreen(world);
            ScreenAnchor = screen;
            _desiredY = screen.y;

            if (_cam == null)
            {
                IsOnScreen = false;
                return;
            }
            Vector3 vp = _cam.WorldToViewportPoint(transform.position);
            IsOnScreen = vp.z > 0f && vp.x > -0.05f && vp.x < 1.05f && vp.y > -0.08f && vp.y < 1.08f;
        }

        static Vector2 NudgeOffHero(Vector2 screen)
        {
            if (_hero == null || WorldLabelsCanvas.Instance == null)
                return screen;

            Vector2 hero = WorldLabelsCanvas.WorldToSnappedScreen(_hero.position + new Vector3(0f, 0.45f, 0f));
            if (hero.x < -100f)
                return screen;
            if (Mathf.Abs(screen.x - hero.x) < 64f && Mathf.Abs(screen.y - hero.y) < 40f)
            {
                screen.x += screen.x >= hero.x ? 40f : -40f;
                screen.y = Mathf.Max(screen.y, hero.y + 34f);
            }
            return screen;
        }

        Vector2 ClampLabel(Vector2 screen)
        {
            Rect safe = Screen.safeArea;
            float scale = 1f;
            var canvas = WorldLabelsCanvas.Instance != null ? WorldLabelsCanvas.Instance.Canvas : null;
            if (canvas != null)
                scale = Mathf.Max(0.01f, canvas.scaleFactor);

            float textW = _label != null ? Mathf.Max(barWidth, _label.preferredWidth) : barWidth;
            float halfW = (textW * 0.5f + 2f) * scale;
            float nameH = _label != null ? Mathf.Max(16f, _label.preferredHeight) : 16f;
            bool icon = false;
            if (_statusIcons != null)
            {
                for (int i = 0; i < _statusIcons.Length; i++)
                    icon |= _statusIcons[i] != null && _statusIcons[i].enabled;
            }
            float above = (17f + nameH) * scale;
            if (icon)
                above = Mathf.Max(above, (17f + 48f) * scale);
            float below = 19f * scale;
            float margin = 4f;

            float minX = safe.xMin + halfW + margin;
            float maxX = safe.xMax - halfW - margin;
            float minY = safe.yMin + below + margin;
            float maxY = safe.yMax - above - margin;
            if (minX > maxX)
                minX = maxX = (safe.xMin + safe.xMax) * 0.5f;
            if (minY > maxY)
                minY = maxY = (safe.yMin + safe.yMax) * 0.5f;

            screen.x = Mathf.Clamp(screen.x, minX, maxX);
            screen.y = Mathf.Clamp(screen.y, minY, maxY);
            screen.x = Mathf.Round(screen.x / 4f) * 4f;
            screen.y = Mathf.Round(screen.y / 4f) * 4f;
            screen.x = Mathf.Clamp(screen.x, minX, maxX);
            screen.y = Mathf.Clamp(screen.y, minY, maxY);
            return screen;
        }

        void ApplyAlpha(float a)
        {
            if (_label != null)
            {
                var c = _label.color;
                c.a = Mathf.Clamp01(a);
                _label.color = c;
            }
            if (_barBg != null)
            {
                var c = _barBg.color;
                c.a = 0.92f * a;
                _barBg.color = c;
            }
            if (_barFill != null)
            {
                var c = _barFill.color;
                c.a = a;
                _barFill.color = c;
            }
        }

        void Build()
        {
            EnsureWhite();
            var canvas = WorldLabelsCanvas.Instance;
            var go = new GameObject("EnemyLabel");
            go.transform.SetParent(canvas.Root, false);
            _root = go.AddComponent<RectTransform>();
            _root.sizeDelta = new Vector2(140f, 34f);

            var labelGo = new GameObject("Name");
            labelGo.transform.SetParent(_root, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = labelRt.anchorMax = new Vector2(0.5f, 1f);
            labelRt.pivot = new Vector2(0.5f, 1f);
            labelRt.anchoredPosition = Vector2.zero;
            labelRt.sizeDelta = new Vector2(180f, 16f);
            _label = labelGo.AddComponent<Text>();
            _label.font = canvas.PixelFont;
            _label.fontSize = 13;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.color = Color.white;
            _label.horizontalOverflow = HorizontalWrapMode.Overflow;
            _label.verticalOverflow = VerticalWrapMode.Overflow;
            _label.raycastTarget = false;
            WorldLabelsCanvas.ApplyPixelOutline(_label, Color.black);
            _statusIcons = new Image[4];
            for (int i = 0; i < _statusIcons.Length; i++)
                _statusIcons[i] = MakeStatusIcon("Status_" + i, 0f);

            var bgGo = new GameObject("HpBg");
            bgGo.transform.SetParent(_root, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 0f);
            bgRt.pivot = new Vector2(0.5f, 0f);
            bgRt.anchoredPosition = new Vector2(0f, 2f);
            bgRt.sizeDelta = new Vector2(barWidth, barHeight);
            _barBg = bgGo.AddComponent<Image>();
            _barBg.sprite = _white;
            _barBg.color = new Color(0.05f, 0.05f, 0.05f, 0.92f);
            _barBg.raycastTarget = false;

            var fillGo = new GameObject("HpFill");
            fillGo.transform.SetParent(bgGo.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.anchoredPosition = Vector2.zero;
            fillRt.sizeDelta = new Vector2(barWidth, 0f);
            _barFill = fillGo.AddComponent<Image>();
            _barFill.sprite = _white;
            _barFill.color = new Color(0.9f, 0.15f, 0.15f, 1f);
            _barFill.raycastTarget = false;
        }

        Image MakeStatusIcon(string iconName, float x)
        {
            var go = new GameObject(iconName);
            go.transform.SetParent(_root, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(x, 2f);
            rt.sizeDelta = new Vector2(48f, 48f);
            var img = go.AddComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            img.enabled = false;
            return img;
        }

        void RefreshStatus()
        {
            if (_statusIcons == null) return;
            var icons = ClassUiIcons.Active;
            var shown = new Sprite[4];
            int n = 0;
            if (icons != null)
            {
                // Control effects first, then bleed. Slow and taunt have no runtime yet.
                if (StatusStun.IsStunned(this) && icons.stun != null) shown[n++] = icons.stun;
                if (StatusBleed.IsBleeding(this) && icons.bleed != null && n < shown.Length) shown[n++] = icons.bleed;
            }
            const float size = 48f;
            const float gap = 2f;
            float total = n > 0 ? n * size + (n - 1) * gap : 0f;
            float start = -total * 0.5f + size * 0.5f;
            for (int i = 0; i < _statusIcons.Length; i++)
            {
                var img = _statusIcons[i];
                if (img == null) continue;
                bool on = i < n;
                img.sprite = on ? shown[i] : null;
                img.enabled = on;
                img.rectTransform.anchoredPosition = new Vector2(start + i * (size + gap), 2f);
                img.rectTransform.sizeDelta = new Vector2(size, size);
            }
        }

        void SetLabelText(string text)
        {
            if (_label != null)
                _label.text = text ?? "";
        }

        void OnChanged(Health h)
        {
            Refresh();
            // Configure / ReviveFull refill HP without a damage event. Do not treat that as a hit.
            if (AlwaysHp || h == null || h.CurrentHp < h.MaxHp)
                return;
            _hpVisibleUntil = -1f;
            SetHpBarVisible(false);
        }

        void OnDamaged(Health _, int applied, Vector2 __)
        {
            if (applied <= 0)
                return;
            Refresh();
            if (AlwaysHp)
                return;
            _hpVisibleUntil = Time.time + Mathf.Max(0f, showHpAfterHitSeconds);
            SetHpBarVisible(true);
        }

        void ResetHpReveal()
        {
            _hpVisibleUntil = -1f;
            SetHpBarVisible(AlwaysHp);
        }

        void TickHpBar()
        {
            if (AlwaysHp)
            {
                SetHpBarVisible(true);
                return;
            }
            SetHpBarVisible(_hpVisibleUntil > Time.time);
        }

        void SetHpBarVisible(bool visible)
        {
            if (AlwaysHp)
                visible = true;
            if (_barBg == null || _barBg.gameObject.activeSelf == visible)
                return;
            _barBg.gameObject.SetActive(visible);
        }

        void OnDied(Health _)
        {
            _hpVisibleUntil = -1f;
            DestroyRoot();
        }

        void DestroyRoot()
        {
            if (_root != null)
            {
                Destroy(_root.gameObject);
                _root = null;
            }
        }

        void Refresh()
        {
            float n = _health != null ? _health.Normalized : 0f;
            if (_barFill != null)
            {
                var rt = _barFill.rectTransform;
                rt.sizeDelta = new Vector2(barWidth * n, 0f);
            }
            if (AlwaysHp && _barFill != null)
                _barFill.color = new Color(0.95f, 0.35f, 0.12f, 1f);
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
