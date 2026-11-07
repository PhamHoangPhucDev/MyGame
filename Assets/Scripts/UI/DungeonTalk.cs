using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>Plays a stage's talk lines before the wave starts.</summary>
    public class DungeonTalk : MonoBehaviour
    {
        static DungeonTalk _one;
        DungeonLine[] _lines;
        int _index;
        Action _done;
        Text _body;
        Text _speaker;

        public static bool IsOpen => _one != null && _one.gameObject.activeSelf;

        public static void Begin(DungeonLine[] lines, Action done)
        {
            UiKit.Ensure();
            if (lines == null || lines.Length == 0)
            {
                done?.Invoke();
                return;
            }
            var talk = Ensure();
            talk._lines = lines;
            talk._index = 0;
            talk._done = done;
            talk.gameObject.SetActive(true);
            talk.Show();
        }

        public static void Close()
        {
            if (_one != null) _one.gameObject.SetActive(false);
        }

        static DungeonTalk Ensure()
        {
            if (_one != null) return _one;
            var go = new GameObject("DungeonTalk");
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 470;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640f, 360f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            _one = go.AddComponent<DungeonTalk>();
            var plate = new GameObject("Plate", typeof(RectTransform));
            plate.transform.SetParent(go.transform, false);
            var rt = (RectTransform)plate.transform;
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, 14f);
            rt.sizeDelta = new Vector2(360f, 58f);
            var img = plate.AddComponent<Image>();
            // Kit dialogue panel (same as the guard card on the stage select); flat plate only as a fallback.
            if (!UiKit.Put(img, "UI_NPC_DialoguePanel", true))
                img.color = new Color(0.12f, 0.1f, 0.12f, 0.95f);
            // Speaker on the top band, line inside the dark inner box, hint on the bottom band.
            var speaker = Line(plate.transform, "Speaker", new Vector2(16f, 45f), new Vector2(-16f, -1f), true);
            speaker.color = new Color32(0xF2, 0xD8, 0x4A, 0xFF);
            _one._speaker = speaker;
            var text = Line(plate.transform, "Line", new Vector2(20f, 14f), new Vector2(-40f, -15f), false);
            text.color = new Color32(0xF4, 0xF1, 0xE8, 0xFF);
            _one._body = text;
            var hint = Line(plate.transform, "Hint", new Vector2(10f, 1f), new Vector2(-14f, -46f), false);
            hint.alignment = TextAnchor.LowerRight;
            hint.color = new Color32(0xAE, 0xB6, 0xC8, 0xFF);
            hint.text = "E ▶";
            var btn = plate.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => _one.Advance());
            return _one;
        }

        /// <summary>Text row inside the plate: offsets are from the bottom-left / top-right corners.</summary>
        static Text Line(Transform parent, string name, Vector2 min, Vector2 max, bool title)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var tr = (RectTransform)go.transform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = min;
            tr.offsetMax = max;
            var text = go.AddComponent<Text>();
            text.font = UiKit.Font;
            text.fontSize = UiKit.Fs(8);
            text.alignment = title ? TextAnchor.UpperLeft : TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            WorldLabelsCanvas.ApplyPixelOutline(text, new Color32(0x14, 0x10, 0x18, 0xFF));
            return text;
        }

        void OnDestroy()
        {
            if (_one == this) _one = null;
        }

        void Update()
        {
            var kb = Keyboard.current;
            if (kb != null && kb.eKey.wasPressedThisFrame) Advance();
            else if (kb != null && kb.escapeKey.wasPressedThisFrame) Advance();
        }

        void Show()
        {
            var line = _lines[_index];
            if (_speaker != null) _speaker.text = (line.speaker ?? "").ToUpperInvariant();
            _body.text = line.text;
        }

        void Advance()
        {
            if (!isActiveAndEnabled) return;
            _index++;
            if (_index >= _lines.Length)
            {
                gameObject.SetActive(false);
                var done = _done;
                _done = null;
                done?.Invoke();
                return;
            }
            Show();
        }
    }
}
