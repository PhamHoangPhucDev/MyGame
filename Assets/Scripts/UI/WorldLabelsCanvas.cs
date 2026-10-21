using UnityEngine;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>
    /// Screen Space Overlay canvas for enemy names, HP bars and damage numbers.
    /// Bypasses Pixel Perfect Camera so text stays sharp at 1920x1080 landscape.
    /// </summary>
    public class WorldLabelsCanvas : MonoBehaviour
    {
        public static WorldLabelsCanvas Instance { get; private set; }

        public Canvas Canvas { get; private set; }
        public RectTransform Root { get; private set; }
        public Font PixelFont { get; private set; }

        const float Snap = 4f;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            Build();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static WorldLabelsCanvas Ensure()
        {
            // A destroyed canvas compares as null. A scene canvas found before its Awake
            // still exists, but Instance is not set yet — that was the popup NRE.
            if (Instance != null) return Instance;
            var existing = FindFirstObjectByType<WorldLabelsCanvas>(FindObjectsInactive.Include);
            if (existing != null)
            {
                if (!existing.gameObject.activeSelf) existing.gameObject.SetActive(true);
                existing.Build();
                Instance = existing;
                return existing;
            }
            var go = new GameObject("WorldLabelsCanvas");
            return go.AddComponent<WorldLabelsCanvas>();
        }

        void Build()
        {
            Canvas = gameObject.GetComponent<Canvas>();
            if (Canvas == null) Canvas = gameObject.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            // Above world sprites / canopy, below combat HUD (100) and LevelFlow (200).
            Canvas.sortingOrder = 55;

            var scaler = gameObject.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 1f;

            if (gameObject.GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            Root = (RectTransform)transform;
            PixelFont = LoadPixelFont();
        }

        static Font LoadPixelFont()
        {
            var runtime = Resources.Load<Font>("Fonts/PressStart2P-Regular");
            if (runtime != null) return runtime;
            return Font.CreateDynamicFontFromOSFont(new[] { "Press Start 2P", "Consolas", "Courier New" }, 16);
        }

        public static Vector2 WorldToSnappedScreen(Vector3 worldPos)
        {
            var cam = Camera.main;
            if (cam == null) return Vector2.zero;
            Vector3 sp = cam.WorldToScreenPoint(worldPos);
            if (sp.z < 0f) return new Vector2(-9999f, -9999f);
            return new Vector2(
                Mathf.Round(sp.x / Snap) * Snap,
                Mathf.Round(sp.y / Snap) * Snap);
        }

        public static void ApplyPixelOutline(Text text, Color outlineColor)
        {
            var outline = text.GetComponent<Outline>();
            if (outline == null) outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(1f, -1f);
            outline.useGraphicAlpha = true;
        }
    }
}
