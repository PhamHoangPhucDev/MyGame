using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>
    /// GAME01 UI kit access. Sprites come from the runtime catalog (Resources/UI/UiStyleCatalog),
    /// so players do not need AssetDatabase. Kit art is drawn in UI pixels: one UI pixel is
    /// 3 canvas units in the landscape profile (1920x1080 reference) and 2 in portrait (576x1024).
    /// UI sprites import at PPU 100 to match the overlay canvases; world sprites stay at PPU 16.
    /// </summary>
    public static partial class UiKit
    {
        static UiStyleCatalog _catalog;
        static Dictionary<string, Sprite> _editorSprites;
        static Font _font;
        public static bool Ready { get; private set; }

        public static void Ensure()
        {
            if (Ready) return;
            _catalog = Resources.Load<UiStyleCatalog>(UiStyleCatalog.ResourcePath);
            Ready = _catalog != null && _catalog.Count > 0;
#if UNITY_EDITOR
            // Editor-only fallback while the catalog asset is missing or stale.
            if (!Ready)
            {
                _editorSprites = new Dictionary<string, Sprite>();
                string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Sprite", new[] { "Assets/Art/UI/Game01" });
                for (int i = 0; i < guids.Length; i++)
                {
                    var sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(UnityEditor.AssetDatabase.GUIDToAssetPath(guids[i]));
                    if (sprite != null) _editorSprites[sprite.name] = sprite;
                }
                Ready = _editorSprites.Count > 0;
            }
#endif
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _catalog = null;
            _editorSprites = null;
            _font = null;
            Ready = false;
        }

        public static Sprite Get(string name)
        {
            Ensure();
            if (string.IsNullOrEmpty(name)) return null;
            if (_catalog != null)
            {
                var s = _catalog.Find(name);
                if (s != null) return s;
            }
            if (_editorSprites != null && _editorSprites.TryGetValue(name, out var e)) return e;
            if (name.StartsWith(FarmIconPrefix)) return FarmArt.Item(name.Substring(FarmIconPrefix.Length));
            return null;
        }

        /// <summary>Game pixel font: VT323 (OFL, has Vietnamese tone marks). Press Start 2P stays as a fallback.</summary>
        public const string PixelFontPath = "Fonts/VT323-Regular";

        /// <summary>
        /// VT323 caps are about 2/3 the height of Press Start 2P at the same size, so sizes written for
        /// Press Start 2P (multiples of 8) are scaled x1.5 (multiples of 12 = VT323's crisp pixel grid).
        /// </summary>
        public static float FontScale => Font != null && Font.name.StartsWith("VT323") ? 1.5f : 1f;

        /// <summary>A font size written for Press Start 2P, converted for the current pixel font.</summary>
        public static int Fs(int pressStartSize) => Mathf.RoundToInt(pressStartSize * FontScale);

        /// <summary>VT323 from Resources, else the catalog font (Press Start 2P), else the built-in font.</summary>
        public static Font Font
        {
            get
            {
                if (_font != null) return _font;
                Ensure();
                _font = Resources.Load<Font>(PixelFontPath);
                if (_font == null && _catalog != null && _catalog.font != null) _font = _catalog.font;
                if (_font == null) _font = Resources.Load<Font>("Fonts/PressStart2P-Regular");
                if (_font == null) _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                return _font;
            }
        }

        // ------------------------------------------------------------------ profile scale

        /// <summary>Portrait 9:16 is retired. PC and mobile both use the 16:9 landscape layout.</summary>
        public static bool IsPortrait => false;

        /// <summary>Phone player, or the editor Game view. Set ForceDesktop in the Editor to preview the PC layout.</summary>
        public static bool ForceDesktop { get; set; }

        public static bool IsMobile => !ForceDesktop && (Application.isMobilePlatform || Application.isEditor);

        /// <summary>
        /// Touch only: grows the invisible tap area of a control to at least minW x minH kit pixels
        /// (about 40 kit px = 44 pt). The art keeps its size. Call after the rect is placed.
        /// </summary>
        public static void Reach(Component c, float minW = 40f, float minH = 40f)
        {
            if (!IsMobile || c == null) return;
            var g = c.GetComponent<Graphic>();
            var rt = c.transform as RectTransform;
            if (g == null || rt == null) return;
            g.raycastTarget = true;
            float px = Px;
            float w = rt.sizeDelta.x / px, h = rt.sizeDelta.y / px;
            float x = Mathf.Max(0f, (minW - w) * 0.5f) * px, y = Mathf.Max(0f, (minH - h) * 0.5f) * px;
            // Negative padding grows the hit rect: (left, bottom, right, top).
            g.raycastPadding = new Vector4(-x, -y, -x, -y);
        }

        /// <summary>Canvas units per kit pixel: 3 in landscape (1080/360), 2 in portrait (1024/512).</summary>
        public static float Px
        {
            get
            {
                var a = AspectProfileService.Instance;
                if (a != null && a.Active != null)
                {
                    float h = a.ActiveId == AspectProfileId.Portrait ? 512f : 360f;
                    return Mathf.Max(1f, Mathf.Round(a.Active.hudScalerReference.y / h));
                }
                return IsPortrait ? 2f : 3f;
            }
        }

        /// <summary>Changes whenever the active profile or its scale changes.</summary>
        public static int ProfileKey => (IsPortrait ? 1000 : 0) + Mathf.RoundToInt(Px);

        public static Vector2 ScalerReference
        {
            get
            {
                var a = AspectProfileService.Instance;
                if (a != null && a.Active != null) return a.Active.hudScalerReference;
                return IsPortrait ? new Vector2(576f, 1024f) : new Vector2(1920f, 1080f);
            }
        }

        // ------------------------------------------------------------------ dressing existing controls

        public static bool Put(Image image, string name, bool sliced)
        {
            var sprite = Get(name);
            if (image == null || sprite == null) return false;
            image.sprite = sprite;
            image.color = Color.white;
            if (sliced && image.type != Image.Type.Filled)
            {
                image.type = Image.Type.Sliced;
                image.fillCenter = true;
                PixelBorders(image);
            }
            return true;
        }

        /// <summary>Keeps 9-slice borders at kit pixel size on the current profile.</summary>
        public static void PixelBorders(Image image)
        {
            if (image == null) return;
            var fit = image.GetComponent<UiPixelFit>();
            if (fit == null) fit = image.gameObject.AddComponent<UiPixelFit>();
            fit.Refresh();
        }

        public static void Dress(Button button, string kind)
        {
            if (button == null || Get("UI_Button_" + kind + "_Normal") == null) return;
            var colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = Color.white;
            colors.pressedColor = Color.white;
            colors.selectedColor = Color.white;
            colors.disabledColor = Color.white;
            colors.fadeDuration = 0f;
            button.colors = colors;
            button.transition = Selectable.Transition.None;
            var skin = button.GetComponent<UiButtonSkin>();
            if (skin == null) skin = button.gameObject.AddComponent<UiButtonSkin>();
            skin.Kind = kind;
            skin.Apply();
            var label = button.GetComponentInChildren<Text>();
            if (label != null)
            {
                // Scene-made labels were sized for Press Start 2P: rescale once, when the font actually changes.
                if (label.font != Font) { label.font = Font; label.fontSize = Fs(label.fontSize); }
                label.color = LabelColor(kind, button.interactable);
            }
        }

        public static Color LabelColor(string kind, bool interactable)
        {
            if (!interactable) return Pal.TextMute;
            if (kind == "Primary") return Pal.TextDark;
            if (kind == "Secondary") return Pal.GoldL;
            return Pal.TextW;
        }
    }

    /// <summary>GAME01 UI palette (Docs/UI boards, page A).</summary>
    public static class Pal
    {
        static Color H(int rgb) => new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
        public static readonly Color Ink = H(0x14100E);
        public static readonly Color Char0 = H(0x16171B);
        public static readonly Color Char = H(0x1E1F24);
        public static readonly Color GoldL = H(0xE8C870);
        public static readonly Color GoldD = H(0x7A5A24);
        public static readonly Color TextW = H(0xF4F1E8);
        public static readonly Color TextBlue = H(0xBFD8F0);
        public static readonly Color TextMute = H(0x9A958C);
        public static readonly Color TextDark = H(0x2A1E18);
        public static readonly Color WoodM = H(0x5A3F2C);
        public static readonly Color GreenL = H(0x9AD65A);
        public static readonly Color RedL = H(0xF06A5A);
        public static readonly Color PurpleL = H(0xB890E8);
        public static readonly Color PurpleD = H(0x4A2A6E);
        public static Color Dim => new Color(0.02f, 0.03f, 0.03f, 0.72f);
    }

    /// <summary>Scales a sliced kit image's borders to the profile pixel size.</summary>
    [DisallowMultipleComponent]
    public class UiPixelFit : MonoBehaviour
    {
        Image _image;
        int _seen = -1;

        void OnEnable() => Refresh();

        void LateUpdate()
        {
            if (_seen != UiKit.ProfileKey) Refresh();
        }

        public void Refresh()
        {
            if (_image == null) _image = GetComponent<Image>();
            if (_image == null) return;
            _seen = UiKit.ProfileKey;
            _image.pixelsPerUnitMultiplier = 1f / UiKit.Px;
        }
    }

    /// <summary>Swaps the shared button sprites by state. Clicks stay on the Button.</summary>
    public class UiButtonSkin : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
    {
        public string Kind = "Primary";
        Image _image;
        Button _button;
        Text _label;
        bool _over;
        bool _down;
        string _shown;
        Vector2 _labelBase;
        bool _labelBaseSet;

        void Awake()
        {
            _image = GetComponent<Image>();
            _button = GetComponent<Button>();
            Apply();
        }

        void Update() => Apply();

        public void OnPointerEnter(PointerEventData eventData) { _over = true; Apply(); }
        public void OnPointerExit(PointerEventData eventData) { _over = false; _down = false; Apply(); }
        public void OnPointerDown(PointerEventData eventData) { _down = true; Apply(); }
        public void OnPointerUp(PointerEventData eventData) { _down = false; Apply(); }

        public void Apply()
        {
            if (_image == null) _image = GetComponent<Image>();
            if (_button == null) _button = GetComponent<Button>();
            if (_image == null) return;
            bool disabled = _button != null && !_button.interactable;
            bool selected = EventSystem.current != null && EventSystem.current.currentSelectedGameObject == gameObject;
            string state = disabled ? "Disabled" : _down ? "Pressed" : _over ? "Hover" : selected ? "Selected" : "Normal";
            string key = Kind + state;
            if (key == _shown) return;
            _shown = key;
            UiKit.Put(_image, "UI_Button_" + Kind + "_" + state, true);
            if (_label == null) _label = GetComponentInChildren<Text>();
            if (_label != null)
            {
                _label.color = UiKit.LabelColor(Kind, !disabled);
                var rt = _label.rectTransform;
                if (!_labelBaseSet) { _labelBase = rt.anchoredPosition; _labelBaseSet = true; }
                rt.anchoredPosition = _labelBase + (state == "Pressed" ? new Vector2(0f, -UiKit.Px) : Vector2.zero);
            }
        }
    }
}
