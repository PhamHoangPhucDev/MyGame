using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>
    /// Places a RectTransform in kit pixels with separate portrait and landscape specs.
    /// Re-applies when the aspect profile changes. Also scales Text font sizes given in kit pixels.
    /// </summary>
    [DisallowMultipleComponent]
    public class UiLayout : MonoBehaviour
    {
        [Serializable]
        public struct Spec
        {
            public bool set;
            public Vector2 anchorMin, anchorMax, pivot, pos, size;

            public static Spec At(Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size) =>
                new Spec { set = true, anchorMin = anchor, anchorMax = anchor, pivot = pivot, pos = pos, size = size };

            /// <summary>Stretch between two anchors; pos/size are offsets in kit pixels (size = extra width/height).</summary>
            public static Spec Stretch(Vector2 min, Vector2 max, Vector2 pivot, Vector2 pos, Vector2 size) =>
                new Spec { set = true, anchorMin = min, anchorMax = max, pivot = pivot, pos = pos, size = size };
        }

        public Spec portrait;
        public Spec landscape;
        public int fontPx;
        int _seen = -1;

        void OnEnable() => Apply(true);

        void LateUpdate()
        {
            if (_seen != UiKit.ProfileKey) Apply(true);
        }

        public UiLayout Both(Spec s) { portrait = s; landscape = s; Apply(true); return this; }
        public UiLayout Portrait(Spec s) { portrait = s; Apply(true); return this; }
        public UiLayout Landscape(Spec s) { landscape = s; Apply(true); return this; }

        public void Apply(bool force)
        {
            int key = UiKit.ProfileKey;
            if (!force && key == _seen) return;
            _seen = key;
            var rt = transform as RectTransform;
            if (rt == null) return;
            var s = UiKit.IsPortrait ? (portrait.set ? portrait : landscape) : (landscape.set ? landscape : portrait);
            float px = UiKit.Px;
            if (s.set)
            {
                rt.anchorMin = s.anchorMin;
                rt.anchorMax = s.anchorMax;
                rt.pivot = s.pivot;
                rt.anchoredPosition = s.pos * px;
                rt.sizeDelta = s.size * px;
            }
            if (fontPx > 0)
            {
                var text = GetComponent<Text>();
                if (text != null) text.fontSize = Mathf.RoundToInt(fontPx * px * (text.font == UiKit.Font ? UiKit.FontScale : 1f));
                var shadow = GetComponent<Shadow>();
                if (shadow != null) shadow.effectDistance = new Vector2(px * 0.5f, -px * 0.5f);
            }
        }
    }

    /// <summary>Frame + fill + optional value text. Fill uses the kit's 1-column fills, so stretching is clean.</summary>
    public class UiBar : MonoBehaviour
    {
        public Image frame;
        public Image fill;
        public Text label;
        public int insetPx = 3;

        public void Set(float current, float max, bool showText = true)
        {
            float t = max > 0f ? Mathf.Clamp01(current / max) : 0f;
            if (fill != null) fill.fillAmount = t;
            if (label != null)
            {
                label.enabled = showText;
                if (showText) label.text = Mathf.RoundToInt(current) + "/" + Mathf.RoundToInt(max);
            }
        }

        public void SetFraction(float t, string text = null)
        {
            if (fill != null) fill.fillAmount = Mathf.Clamp01(t);
            if (label != null)
            {
                label.enabled = !string.IsNullOrEmpty(text);
                label.text = text ?? "";
            }
        }
    }

    /// <summary>Inventory / reward slot: frame, item icon, quantity.</summary>
    public class UiItemSlot : MonoBehaviour
    {
        public Image frame;
        public Image icon;
        public Text qty;
        public Button button;

        public void Set(string iconSprite, int amount, string state = "")
        {
            UiKit.Put(frame, "UI_Inventory_Slot" + (string.IsNullOrEmpty(state) ? "" : "_" + state), false);
            var sprite = UiKit.Get(iconSprite);
            if (icon != null)
            {
                icon.sprite = sprite;
                icon.enabled = sprite != null;
            }
            if (qty != null) qty.text = amount > 1 ? amount.ToString() : "";
        }
    }

    /// <summary>Kit window: main panel, title ribbon, close button and a body that screens refill.</summary>
    public class UiWindow : MonoBehaviour
    {
        public RectTransform body;
        public Text title;
        public Button close;
        public event Action Closed;

        public void SetTitle(string text)
        {
            if (title == null) return;
            title.text = text;
            var ribbon = title.transform.parent as RectTransform;
            var layout = ribbon != null ? ribbon.GetComponent<UiLayout>() : null;
            if (layout != null)
            {
                float w = Mathf.Max(96f, text.Length * 8f + 36f);
                var s = layout.landscape;
                s.size = new Vector2(w, 18f);
                layout.Both(s);
            }
        }

        public void Clear()
        {
            if (body == null) return;
            for (int i = body.childCount - 1; i >= 0; i--) Destroy(body.GetChild(i).gameObject);
        }

        public void Show(bool show) => gameObject.SetActive(show);

        public void Close()
        {
            gameObject.SetActive(false);
            Closed?.Invoke();
        }
    }

    /// <summary>Builders for the shared kit widgets. Sizes and positions are kit pixels.</summary>
    public static partial class UiKit
    {
        public static readonly Vector2 Center = new Vector2(0.5f, 0.5f);

        /// <summary>Overlay canvas whose scaler follows the active aspect profile.</summary>
        public static Canvas Canvas(string name, int order, Transform parent = null)
        {
            var go = new GameObject(name);
            if (parent != null) go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = order;
            FitScaler(go);
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        /// <summary>Adds or updates a CanvasScaler to the profile reference and keeps it in sync.</summary>
        public static void FitScaler(GameObject canvasGo)
        {
            var scaler = canvasGo.GetComponent<CanvasScaler>();
            if (scaler == null) scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referencePixelsPerUnit = 100f;
            if (canvasGo.GetComponent<UiScalerSync>() == null) canvasGo.AddComponent<UiScalerSync>();
            canvasGo.GetComponent<UiScalerSync>().Sync();
        }

        public static RectTransform Rect(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }

        public static UiLayout Place(Component c, Vector2 anchor, Vector2 pivot, Vector2 posPx, Vector2 sizePx)
        {
            var layout = c.GetComponent<UiLayout>();
            if (layout == null) layout = c.gameObject.AddComponent<UiLayout>();
            return layout.Both(UiLayout.Spec.At(anchor, pivot, posPx, sizePx));
        }

        public static UiLayout Fill(Component c, float insetPx = 0f)
        {
            var layout = c.GetComponent<UiLayout>();
            if (layout == null) layout = c.gameObject.AddComponent<UiLayout>();
            return layout.Both(UiLayout.Spec.Stretch(Vector2.zero, Vector2.one, Center, Vector2.zero, new Vector2(-2f * insetPx, -2f * insetPx)));
        }

        public static Image Dim(Transform parent, string name = "Dim")
        {
            var img = Rect(parent, name).gameObject.AddComponent<Image>();
            img.color = Pal.Dim;
            img.raycastTarget = true;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return img;
        }

        public static Image Panel(Transform parent, string name, string sprite, bool blocksClicks = false)
        {
            var img = Rect(parent, name).gameObject.AddComponent<Image>();
            img.raycastTarget = blocksClicks;
            if (!Put(img, sprite, true)) img.color = new Color(0.08f, 0.08f, 0.1f, 0.92f);
            return img;
        }

        public static Image Icon(Transform parent, string sprite, Vector2 anchor, Vector2 posPx, float sizePx = 16f)
        {
            var img = Rect(parent, "Icon_" + sprite).gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            img.sprite = Get(sprite);
            img.enabled = img.sprite != null;
            Place(img, anchor, Center, posPx, new Vector2(sizePx, sizePx));
            return img;
        }

        /// <summary>Pixel-font text. fontPx 8 is body text, 16 is a title.</summary>
        public static Text Label(Transform parent, string name, string text, int fontPx, Color color, TextAnchor align = TextAnchor.MiddleLeft, bool shadow = true)
        {
            var t = Rect(parent, name).gameObject.AddComponent<Text>();
            t.font = Font;
            t.text = text;
            t.color = color;
            t.alignment = align;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = false;
            LocText.Attach(t);
            if (shadow)
            {
                var s = t.gameObject.AddComponent<Shadow>();
                s.effectColor = Pal.Ink;
            }
            var layout = t.gameObject.AddComponent<UiLayout>();
            layout.fontPx = fontPx;
            layout.Apply(true);
            return t;
        }

        public static Button Button(Transform parent, string name, string kind, string label, Vector2 sizePx, UnityAction onClick, string icon = null)
        {
            var img = Rect(parent, name).gameObject.AddComponent<Image>();
            Put(img, "UI_Button_" + kind + "_Normal", true);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            if (onClick != null) btn.onClick.AddListener(onClick);
            float iconW = 0f;
            if (!string.IsNullOrEmpty(icon) && Get(icon) != null)
            {
                iconW = 18f;
                float textW = string.IsNullOrEmpty(label) ? 0f : label.Length * 8f;
                Icon(img.transform, icon, Center, new Vector2(-(textW + iconW) * 0.5f + 8f, 1f));
            }
            if (!string.IsNullOrEmpty(label))
            {
                var t = Label(img.transform, "Label", label, 8, LabelColor(kind, true), TextAnchor.MiddleCenter, kind != "Primary");
                Fill(t).Both(UiLayout.Spec.Stretch(Vector2.zero, Vector2.one, Center, new Vector2(iconW * 0.5f, 1f), Vector2.zero));
            }
            Place(img, Center, Center, Vector2.zero, sizePx);
            Dress(btn, kind);
            return btn;
        }

        public static Button IconButton(Transform parent, string name, string icon, UnityAction onClick)
        {
            var img = Rect(parent, name).gameObject.AddComponent<Image>();
            Put(img, "UI_Button_Icon_Normal", false);
            var btn = img.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.transition = Selectable.Transition.SpriteSwap;
            btn.spriteState = new SpriteState
            {
                highlightedSprite = Get("UI_Button_Icon_Hover"),
                pressedSprite = Get("UI_Button_Icon_Pressed"),
                selectedSprite = Get("UI_Button_Icon_Selected"),
                disabledSprite = Get("UI_Button_Icon_Disabled"),
            };
            if (onClick != null) btn.onClick.AddListener(onClick);
            Icon(img.transform, icon, Center, new Vector2(0f, 1f));
            Place(img, Center, Center, Vector2.zero, new Vector2(22f, 22f));
            return btn;
        }

        /// <summary>Title ribbon (3-slice) with centred text. kind: "", "Red" or "Gold".</summary>
        public static Text Ribbon(Transform parent, string text, string kind = "")
        {
            var img = Rect(parent, "Ribbon").gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            Put(img, "UI_Header_Ribbon" + (string.IsNullOrEmpty(kind) ? "" : "_" + kind), true);
            float w = Mathf.Max(96f, Loc.MaxLength(text) * 8f + 36f);
            Place(img, new Vector2(0.5f, 1f), Center, new Vector2(0f, 2f), new Vector2(w, 18f));
            var t = Label(img.transform, "Title", text, 8, kind == "Gold" ? Pal.TextDark : new Color(0.97f, 0.91f, 0.66f), TextAnchor.MiddleCenter, kind != "Gold");
            LocText.Attach(t);
            Fill(t).Both(UiLayout.Spec.Stretch(Vector2.zero, Vector2.one, Center, new Vector2(0f, 2f), Vector2.zero));
            return t;
        }

        /// <summary>Main window with ribbon and close button. Body is inset 12 kit px (24 at the top for the ribbon).</summary>
        public static UiWindow Window(Transform parent, string name, string title, Vector2 sizePx, Action onClose = null, string panel = "UI_Panel_Main")
        {
            var img = Panel(parent, name, panel, true);
            Place(img, Center, Center, Vector2.zero, sizePx);
            var win = img.gameObject.AddComponent<UiWindow>();
            var body = Rect(img.transform, "Body");
            body.gameObject.AddComponent<UiLayout>().Both(UiLayout.Spec.Stretch(Vector2.zero, Vector2.one, Center, new Vector2(0f, -4f), new Vector2(-24f, -32f)));
            win.body = body;
            if (!string.IsNullOrEmpty(title)) win.title = Ribbon(img.transform, title, panel == "UI_Panel_Reward" ? "Gold" : "");
            var close = Rect(img.transform, "Close").gameObject.AddComponent<Image>();
            Put(close, "UI_Button_Close_Normal", false);
            var btn = close.gameObject.AddComponent<Button>();
            btn.targetGraphic = close;
            btn.transition = Selectable.Transition.SpriteSwap;
            btn.spriteState = new SpriteState { pressedSprite = Get("UI_Button_Close_Pressed") };
            btn.onClick.AddListener(win.Close);
            if (onClose != null) win.Closed += onClose;
            Place(close, new Vector2(1f, 1f), Center, new Vector2(-5f, -5f), new Vector2(16f, 16f));
            win.close = btn;
            return win;
        }

        public static UiBar Bar(Transform parent, string name, string fillKind, float widthPx, bool thin = false, bool withText = true)
        {
            var frame = Rect(parent, name).gameObject.AddComponent<Image>();
            frame.raycastTarget = false;
            Put(frame, thin ? "UI_Bar_Frame_Thin" : "UI_Bar_Frame", true);
            float h = thin ? 6f : 12f;
            int inset = thin ? 2 : 3;
            Place(frame, Center, Center, Vector2.zero, new Vector2(widthPx, h));
            var fill = Rect(frame.transform, "Fill").gameObject.AddComponent<Image>();
            fill.raycastTarget = false;
            fill.sprite = Get("UI_Bar_Fill_" + fillKind);
            fill.type = UnityEngine.UI.Image.Type.Filled;
            fill.fillMethod = UnityEngine.UI.Image.FillMethod.Horizontal;
            fill.fillOrigin = 0;
            Fill(fill, inset);
            var bar = frame.gameObject.AddComponent<UiBar>();
            bar.frame = frame;
            bar.fill = fill;
            bar.insetPx = inset;
            if (withText && !thin)
            {
                var t = Label(frame.transform, "Value", "", 8, Pal.TextW, TextAnchor.MiddleCenter);
                t.gameObject.AddComponent<Outline>().effectColor = Pal.Ink;
                Fill(t).Both(UiLayout.Spec.Stretch(Vector2.zero, Vector2.one, Center, new Vector2(0f, 1f), new Vector2(0f, 6f)));
                bar.label = t;
            }
            return bar;
        }

        public static UiItemSlot Slot(Transform parent, string name, UnityAction onClick = null)
        {
            var frame = Rect(parent, name).gameObject.AddComponent<Image>();
            Put(frame, "UI_Inventory_Slot", false);
            frame.raycastTarget = onClick != null;
            Place(frame, Center, Center, Vector2.zero, new Vector2(22f, 22f));
            var slot = frame.gameObject.AddComponent<UiItemSlot>();
            slot.frame = frame;
            slot.icon = Icon(frame.transform, "UI_Item_Ore", Center, Vector2.zero);
            // 6 px digits: with the VT323 x1.5 scale, "999" still fits inside a 15 px slot.
            var q = Label(frame.transform, "Qty", "", 6, Pal.TextW, TextAnchor.LowerRight);
            q.gameObject.AddComponent<Outline>().effectColor = Pal.Ink;
            Fill(q, 0f).Both(UiLayout.Spec.Stretch(Vector2.zero, Vector2.one, Center, new Vector2(2f, -1f), Vector2.zero));
            slot.qty = q;
            if (onClick != null)
            {
                slot.button = frame.gameObject.AddComponent<Button>();
                slot.button.transition = Selectable.Transition.None;
                slot.button.onClick.AddListener(onClick);
            }
            return slot;
        }

        /// <summary>List row background. state: "", "Selected" or "Disabled".</summary>
        public static Image Row(Transform parent, string name, string state, Vector2 sizePx)
        {
            var img = Rect(parent, name).gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            Put(img, "UI_ListRow_" + (string.IsNullOrEmpty(state) ? "Normal" : state), true);
            Place(img, Center, Center, Vector2.zero, sizePx);
            return img;
        }

        public static Button Tab(Transform parent, string label, bool selected, UnityAction onClick)
        {
            var img = Rect(parent, "Tab_" + label).gameObject.AddComponent<Image>();
            Put(img, selected ? "UI_Tab_Selected" : "UI_Tab_Normal", true);
            var btn = img.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            if (onClick != null) btn.onClick.AddListener(onClick);
            var t = Label(img.transform, "Label", label, 8, selected ? Pal.GoldL : Pal.TextMute, TextAnchor.MiddleCenter, selected);
            Fill(t).Both(UiLayout.Spec.Stretch(Vector2.zero, Vector2.one, Center, new Vector2(0f, selected ? 0f : -1f), Vector2.zero));
            Place(img, Center, Center, Vector2.zero, new Vector2(label.Length * 8f + 14f, 16f));
            return btn;
        }

        /// <summary>Translucent HUD plate with a coin icon and the live coin count.</summary>
        public static Text Coins(Transform parent, Vector2 anchor, Vector2 pivot, Vector2 posPx)
        {
            var plate = Panel(parent, "Coins", "UI_Panel_HUD");
            Place(plate, anchor, pivot, posPx, new Vector2(72f, 16f));
            Icon(plate.transform, "UI_Icon_Currency", new Vector2(0f, 0.5f), new Vector2(9f, 0f));
            var t = Label(plate.transform, "Value", "0", 8, Pal.GoldL, TextAnchor.MiddleRight);
            Fill(t).Both(UiLayout.Spec.Stretch(Vector2.zero, Vector2.one, Center, new Vector2(-5f, 0f), new Vector2(-10f, 0f)));
            return t;
        }

        /// <summary>Purple CONCEPT tag used on UI for systems that are not in the game yet.</summary>
        public static Text ConceptTag(Transform parent, string text, Vector2 anchor, Vector2 posPx)
        {
            var bg = Rect(parent, "Concept").gameObject.AddComponent<Image>();
            bg.raycastTarget = false;
            bg.color = new Color(Pal.PurpleD.r, Pal.PurpleD.g, Pal.PurpleD.b, 0.92f);
            Place(bg, anchor, Center, posPx, new Vector2(text.Length * 8f + 10f, 12f));
            var t = Label(bg.transform, "Text", text, 8, Pal.PurpleL, TextAnchor.MiddleCenter, false);
            Fill(t);
            return t;
        }

        /// <summary>Reusable "coming soon" body for concept windows.</summary>
        public static void ComingSoon(UiWindow win, string line)
        {
            win.Clear();
            ConceptTag(win.body, "COMING SOON", new Vector2(0.5f, 1f), new Vector2(0f, -10f));
            var t = Label(win.body, "Line", line, 8, Pal.TextMute, TextAnchor.UpperCenter);
            Place(t, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(220f, 80f));
        }

        public const string FarmIconPrefix = "FM_Item_";

        public static string ItemIcon(string itemId)
        {
            switch (itemId)
            {
                case "ore": return "UI_Item_Ore";
                case "wood": return "UI_Item_Wood";
                case "herb": return "UI_Item_Herb";
                case "sprout": return "UI_Item_Sprout";
                case "potion": return "UI_Item_Potion";
                case "training_blade": return "UI_Item_TrainingBlade";
                case "quest_token": return "UI_Item_QuestToken";
                case "coin": return "UI_Item_Coin";
                case "mystery_pet_egg": return "UI_Item_MysteryPetEgg";
                case "pet_egg_shard": return "UI_Item_PetEggShard";
                // Stand-ins from the kit until the task 10 icons are approved.
                case "seed_sprout": return "UI_Item_Sprout";
                case "seed_fiber": return "UI_Icon_Farming";
                case "fiber": return "UI_Item_Herb";
                case "basic_armor": return "UI_Equip_Armor_Ghost";
                case "basic_charm": return "UI_Equip_Ring_Ghost";
                case "gem_ruby":
                case "gem_sapphire":
                case "gem_emerald": return "UI_Icon_Treasure";
                default: return FarmArt.IsFarmItem(itemId) ? FarmIconPrefix + itemId : "UI_Icon_Inventory";
            }
        }
    }

    /// <summary>Keeps a CanvasScaler on the active profile's reference resolution.</summary>
    [DisallowMultipleComponent]
    public class UiScalerSync : MonoBehaviour
    {
        int _seen = -1;

        void LateUpdate()
        {
            if (_seen != UiKit.ProfileKey) Sync();
        }

        public void Sync()
        {
            _seen = UiKit.ProfileKey;
            var scaler = GetComponent<CanvasScaler>();
            if (scaler == null) return;
            scaler.referenceResolution = UiKit.ScalerReference;
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = UiKit.IsPortrait ? 0.65f : 0.5f;
        }
    }
}
