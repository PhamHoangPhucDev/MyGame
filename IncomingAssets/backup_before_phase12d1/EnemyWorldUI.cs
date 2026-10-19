using UnityEngine;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>
    /// Enemy HP bar as Overlay UI. Name labels stay hidden by default to avoid clutter;
    /// HP appears after damage and fades out. Distant / overlapping labels are culled.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class EnemyWorldUI : MonoBehaviour
    {
        [SerializeField] Vector3 worldOffset = new Vector3(0f, 0.85f, 0f);
        [SerializeField] float barWidth = 56f;
        [SerializeField] float barHeight = 6f;
        [SerializeField] float maxLabelDistance = 9.5f;
        [SerializeField] int maxVisibleBars = 4;

        Health _health;
        EnemyController _enemy;
        RectTransform _root;
        Text _label;
        Image _barBg;
        Image _barFill;
        float _barVisibleTimer;
        bool _barVisible;
        float _distToHero = 999f;
        static Sprite _white;
        static Transform _hero;
        static readonly System.Collections.Generic.List<EnemyWorldUI> Active = new();
        static int _batchFrame = -1;
        static int _batchCount;

        void Awake()
        {
            _health = GetComponent<Health>();
            _enemy = GetComponent<EnemyController>();
            WorldLabelsCanvas.Ensure();
            Build();
            _health.Changed += OnChanged;
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
            SetBarVisible(false);
            if (_label != null)
                _label.gameObject.SetActive(false); // Names off — HP only when needed.
            Refresh();
        }

        void OnDestroy()
        {
            if (_health != null)
            {
                _health.Changed -= OnChanged;
                _health.Damaged -= OnDamaged;
                _health.Died -= OnDied;
            }
            if (_root != null)
                Destroy(_root.gameObject);
        }

        void OnEnable()
        {
            if (!Active.Contains(this))
                Active.Add(this);
        }

        void OnDisable()
        {
            Active.Remove(this);
        }

        void LateUpdate()
        {
            if (_root == null || WorldLabelsCanvas.Instance == null) return;

            if (_batchFrame != Time.frameCount)
            {
                _batchFrame = Time.frameCount;
                _batchCount = 0;
                ResolveHero();
                RankDistances();
            }
            _batchCount++;

            bool show = ShouldShow();
            if (_root.gameObject.activeSelf != show)
                _root.gameObject.SetActive(show);
            if (!show)
                return;

            Vector2 above = WorldLabelsCanvas.WorldToSnappedScreen(transform.position + worldOffset);
            Vector2 below = WorldLabelsCanvas.WorldToSnappedScreen(transform.position + new Vector3(0f, -0.2f, 0f));
            Vector2 screen = above.y > Screen.height - 96f ? below : above;
            screen = NudgeOffHero(screen);
            _root.position = ClampLabel(screen);

            // Soft alpha for farther bars.
            float a = Mathf.Clamp01(1.15f - _distToHero / maxLabelDistance);
            a = Mathf.Lerp(0.45f, 1f, a);
            ApplyAlpha(a);

            if (_barVisible)
            {
                _barVisibleTimer -= Time.unscaledDeltaTime;
                if (_barVisibleTimer <= 0f)
                    SetBarVisible(false);
            }
        }

        static void ResolveHero()
        {
            if (_hero == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    _hero = player.transform;
            }
        }

        static void RankDistances()
        {
            if (_hero == null) return;
            Vector3 hp = _hero.position;
            for (int i = 0; i < Active.Count; i++)
            {
                var ui = Active[i];
                if (ui == null) continue;
                ui._distToHero = Vector2.Distance(ui.transform.position, hp);
            }
        }

        bool ShouldShow()
        {
            if (!_barVisible || _health == null || _health.IsDead)
                return false;
            if (_distToHero > maxLabelDistance)
                return false;

            Vector2 above = WorldLabelsCanvas.WorldToSnappedScreen(transform.position + worldOffset);
            bool inView = above.x >= 8f && above.x <= Screen.width - 8f
                && above.y >= 72f && above.y <= Screen.height - 64f;
            if (!inView)
                return false;

            // Only keep the nearest damaged enemies so clusters don't stack bars.
            int rank = 0;
            for (int i = 0; i < Active.Count; i++)
            {
                var other = Active[i];
                if (other == null || !other._barVisible || other._health == null || other._health.IsDead)
                    continue;
                if (other._distToHero + 0.01f < _distToHero)
                    rank++;
            }
            return rank < maxVisibleBars;
        }

        void ApplyAlpha(float a)
        {
            if (_barBg != null)
            {
                var c = _barBg.color;
                c.a = 0.9f * a;
                _barBg.color = c;
            }
            if (_barFill != null)
            {
                var c = _barFill.color;
                c.a = a;
                _barFill.color = c;
            }
        }

        static Vector2 NudgeOffHero(Vector2 screen)
        {
            if (_hero == null || WorldLabelsCanvas.Instance == null)
                return screen;

            Vector2 hero = WorldLabelsCanvas.WorldToSnappedScreen(_hero.position + new Vector3(0f, 0.45f, 0f));
            if (hero.x < -100f)
                return screen;
            if (Mathf.Abs(screen.x - hero.x) < 72f && Mathf.Abs(screen.y - hero.y) < 36f)
            {
                screen.x += screen.x >= hero.x ? 48f : -48f;
                screen.y = Mathf.Max(screen.y, hero.y + 28f);
            }
            return screen;
        }

        Vector2 ClampLabel(Vector2 screen)
        {
            float half = barWidth * 0.5f + 8f;
            float margin = 8f;
            float top = Screen.height - 56f;
            float bottom = 140f;
            screen.x = Mathf.Clamp(screen.x, half + margin, Screen.width - half - margin);
            screen.y = Mathf.Clamp(screen.y, bottom, top);
            screen.x = Mathf.Round(screen.x / 4f) * 4f;
            screen.y = Mathf.Round(screen.y / 4f) * 4f;
            return screen;
        }

        void Build()
        {
            EnsureWhite();
            var canvas = WorldLabelsCanvas.Instance;
            var go = new GameObject("EnemyLabel");
            go.transform.SetParent(canvas.Root, false);
            _root = go.AddComponent<RectTransform>();
            _root.sizeDelta = new Vector2(120f, 28f);

            var labelGo = new GameObject("Name");
            labelGo.transform.SetParent(_root, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = labelRt.anchorMax = new Vector2(0.5f, 1f);
            labelRt.pivot = new Vector2(0.5f, 1f);
            labelRt.anchoredPosition = Vector2.zero;
            labelRt.sizeDelta = new Vector2(160f, 16f);
            _label = labelGo.AddComponent<Text>();
            _label.font = canvas.PixelFont;
            _label.fontSize = 12;
            _label.alignment = TextAnchor.MiddleCenter;
            _label.color = new Color(1f, 1f, 1f, 0.75f);
            _label.horizontalOverflow = HorizontalWrapMode.Overflow;
            _label.verticalOverflow = VerticalWrapMode.Overflow;
            _label.raycastTarget = false;
            WorldLabelsCanvas.ApplyPixelOutline(_label, Color.black);
            labelGo.SetActive(false);

            var bgGo = new GameObject("HpBg");
            bgGo.transform.SetParent(_root, false);
            var bgRt = bgGo.AddComponent<RectTransform>();
            bgRt.anchorMin = bgRt.anchorMax = new Vector2(0.5f, 0.5f);
            bgRt.pivot = new Vector2(0.5f, 0.5f);
            bgRt.anchoredPosition = Vector2.zero;
            bgRt.sizeDelta = new Vector2(barWidth, barHeight);
            _barBg = bgGo.AddComponent<Image>();
            _barBg.sprite = _white;
            _barBg.color = new Color(0.05f, 0.05f, 0.05f, 0.9f);
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

        void OnChanged(Health _) => Refresh();

        void OnDamaged(Health _, int __, Vector2 ___)
        {
            SetBarVisible(true);
            _barVisibleTimer = 2.2f;
            Refresh();
        }

        void OnDied(Health _)
        {
            if (_root != null)
                Destroy(_root.gameObject);
            _root = null;
        }

        void SetBarVisible(bool visible)
        {
            _barVisible = visible;
            if (_barBg != null) _barBg.gameObject.SetActive(visible);
        }

        void Refresh()
        {
            float n = _health != null ? _health.Normalized : 0f;
            if (_barFill != null)
            {
                var rt = _barFill.rectTransform;
                rt.sizeDelta = new Vector2(barWidth * n, 0f);
            }
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
