using UnityEngine;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>
    /// Always-on enemy name + HP bar on the shared WorldLabelsCanvas.
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

        Health _health;
        EnemyController _enemy;
        BossWarlordController _boss;
        RectTransform _root;
        Text _label;
        Image _barBg;
        Image _barFill;
        Vector2 _layoutNudge;
        Vector2 _nudgeTarget;
        float _desiredY;
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
            WorldLabelsCanvas.Ensure();
            Build();
            _health.Changed += OnChanged;
            _health.Died += OnDied;
            Refresh();
            SetLabelText(LabelText);
        }

        void OnDestroy()
        {
            if (_health != null)
            {
                _health.Changed -= OnChanged;
                _health.Died -= OnDied;
            }
            DestroyRoot();
        }

        void OnEnable()
        {
            if (!Active.Contains(this))
                Active.Add(this);
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
            float half = barWidth * 0.5f + 10f;
            float margin = 6f;
            float top = Screen.height - 72f;
            float bottom = 150f;
            screen.x = Mathf.Clamp(screen.x, half + margin, Screen.width - half - margin);
            screen.y = Mathf.Clamp(screen.y, bottom, top);
            screen.x = Mathf.Round(screen.x / 4f) * 4f;
            screen.y = Mathf.Round(screen.y / 4f) * 4f;
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

        void SetLabelText(string text)
        {
            if (_label != null)
                _label.text = text ?? "";
        }

        void OnChanged(Health _) => Refresh();

        void OnDied(Health _)
        {
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
            if (_boss != null && _barFill != null)
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
