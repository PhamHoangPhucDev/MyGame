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
            EnsureBuilt();
            if (_root != null)
                _root.gameObject.SetActive(boss != null);
            if (_title != null && boss != null)
                _title.text = boss.DisplayName;
            Refresh();
        }

        public void Clear()
        {
            _boss = null;
            if (_root != null)
                _root.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            if (_boss == null || _root == null || !_root.gameObject.activeSelf) return;
            if (_boss.Health == null || _boss.Health.IsDead)
            {
                Clear();
                return;
            }
            Refresh();
            if (_title != null)
                _title.text = $"{_boss.DisplayName}  —  PHASE {(int)_boss.Phase}";
        }

        void Refresh()
        {
            if (_fill == null || _boss == null || _boss.Health == null) return;
            float n = _boss.Health.Normalized;
            _fill.rectTransform.sizeDelta = new Vector2(520f * n, 0f);
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

            var rootGo = new GameObject("BossBar");
            rootGo.transform.SetParent(canvasGo.transform, false);
            _root = rootGo.AddComponent<RectTransform>();
            _root.anchorMin = _root.anchorMax = new Vector2(0.5f, 1f);
            _root.pivot = new Vector2(0.5f, 1f);
            // Sit under Level/Wave/Objective HUD so labels stay readable.
            _root.anchoredPosition = new Vector2(0f, -78f);
            _root.sizeDelta = new Vector2(560f, 48f);

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(_root, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = Vector2.zero;
            titleRt.sizeDelta = new Vector2(560f, 22f);
            _title = titleGo.AddComponent<Text>();
            _title.font = WorldLabelsCanvas.Ensure().PixelFont;
            _title.fontSize = 16;
            _title.alignment = TextAnchor.MiddleCenter;
            _title.color = new Color(1f, 0.85f, 0.45f, 1f);
            _title.raycastTarget = false;
            WorldLabelsCanvas.ApplyPixelOutline(_title, Color.black);

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

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(bgGo.transform, false);
            var fillRt = fillGo.AddComponent<RectTransform>();
            fillRt.anchorMin = new Vector2(0f, 0f);
            fillRt.anchorMax = new Vector2(0f, 1f);
            fillRt.pivot = new Vector2(0f, 0.5f);
            fillRt.anchoredPosition = Vector2.zero;
            fillRt.sizeDelta = new Vector2(520f, 0f);
            _fill = fillGo.AddComponent<Image>();
            _fill.sprite = _white;
            _fill.color = new Color(0.92f, 0.22f, 0.12f, 1f);
            _fill.raycastTarget = false;

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
