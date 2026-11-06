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
            rt.anchoredPosition = new Vector2(0f, 18f);
            rt.sizeDelta = new Vector2(420f, 48f);
            var img = plate.AddComponent<Image>();
            img.color = new Color(0.965f, 0.937f, 0.886f, 1f);
            var textGo = new GameObject("Line", typeof(RectTransform));
            textGo.transform.SetParent(plate.transform, false);
            var tr = (RectTransform)textGo.transform;
            tr.anchorMin = Vector2.zero;
            tr.anchorMax = Vector2.one;
            tr.offsetMin = new Vector2(8f, 4f);
            tr.offsetMax = new Vector2(-8f, -4f);
            var text = textGo.AddComponent<Text>();
            text.font = UiKit.Font;
            text.fontSize = 8;
            text.color = new Color(0.16f, 0.13f, 0.16f, 1f);
            text.alignment = TextAnchor.MiddleLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            _one._body = text;
            var btn = plate.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => _one.Advance());
            return _one;
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
            _body.text = line.speaker + ": " + line.text;
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
