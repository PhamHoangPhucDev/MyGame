using System;
using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>Placeholder visual. Swap the sprite later without touching zone logic.</summary>
    public class WorldVisual : MonoBehaviour
    {
        public void Build(string label, Color color, Vector2 size)
        {
            var sr = gameObject.GetComponent<SpriteRenderer>();
            if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
            sr.sprite = ProceduralSprites.Pixel;
            sr.color = color;
            sr.sortingOrder = 4;
            transform.localScale = new Vector3(size.x, size.y, 1f);

            // "Label" stays a plain anchor: village dressing scripts move it to sit above the art.
            var anchor = transform.Find("Label");
            if (anchor == null)
            {
                anchor = new GameObject("Label").transform;
                anchor.SetParent(transform, false);
                anchor.localPosition = new Vector3(0f, 1.1f, 0f);
            }
            if (!string.IsNullOrEmpty(label)) WorldNameplate.Attach(anchor.gameObject, label, Color.white);
        }
    }

    /// <summary>
    /// Pixel-font label on a screen-space canvas that follows a world anchor. World-space text
    /// goes through the upscaled pixel-perfect render (16 px per unit) and smears to a few pixels.
    /// </summary>
    public class WorldNameplate : MonoBehaviour
    {
        static Canvas _canvas;

        public UnityEngine.UI.Text Text { get; private set; }

        public static WorldNameplate Attach(GameObject anchor, string label, Color color)
        {
            var plate = anchor.GetComponent<WorldNameplate>();
            if (plate == null) plate = anchor.AddComponent<WorldNameplate>();
            if (plate.Text == null)
            {
                // Under the prompt plate (30), the HUD (220) and service windows (240+).
                if (_canvas == null) _canvas = UiKit.Canvas("WorldNameplates", 25);
                var owner = anchor.transform.parent != null ? anchor.transform.parent.name : anchor.name;
                plate.Text = UiKit.Label(_canvas.transform, owner, label, 8, color, TextAnchor.MiddleCenter);
                plate.Text.horizontalOverflow = HorizontalWrapMode.Overflow;
                var rt = plate.Text.rectTransform;
                rt.anchorMin = rt.anchorMax = Vector2.zero;
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(200f, 10f);
            }
            plate.Text.text = label;
            plate.Text.color = color;
            plate.Text.gameObject.SetActive(plate.isActiveAndEnabled);
            return plate;
        }

        void OnEnable()
        {
            if (Text != null) Text.gameObject.SetActive(true);
        }

        void OnDisable()
        {
            if (Text != null) Text.gameObject.SetActive(false);
        }

        void OnDestroy()
        {
            if (Text != null) Destroy(Text.gameObject);
        }

        void LateUpdate()
        {
            if (Text == null || _canvas == null) return;
            var cam = Camera.main;
            bool show = cam != null && !string.IsNullOrEmpty(Text.text);
            Vector3 sp = show ? cam.WorldToScreenPoint(transform.position) : Vector3.zero;
            show &= sp.z >= 0f && sp.x > -100f && sp.x < Screen.width + 100f && sp.y > -50f && sp.y < Screen.height + 50f;
            if (Text.enabled != show) Text.enabled = show;
            if (!show) return;
            // Whole canvas units keep the 8 px font on the UI pixel grid.
            float s = Mathf.Max(0.0001f, _canvas.scaleFactor);
            Text.rectTransform.anchoredPosition = new Vector2(Mathf.Round(sp.x / s), Mathf.Round(sp.y / s));
        }
    }

    public class WorldInteractable : MonoBehaviour
    {
        public string DisplayName;
        public string Line;
        public float Radius = 1.15f;
        /// <summary>False keeps the object as a position anchor without winning the interact prompt.</summary>
        public bool Seekable = true;
        public Action OnInteract;

        public bool InRange(Vector2 hero)
        {
            return ((Vector2)transform.position - hero).sqrMagnitude <= Radius * Radius;
        }

        public void Interact()
        {
            OnInteract?.Invoke();
        }
    }

    public class WorldGate : MonoBehaviour
    {
        public Action OnEnter;
        float _lock;

        void OnTriggerEnter2D(Collider2D other)
        {
            if (other == null || !other.CompareTag("Player")) return;
            if (Time.unscaledTime < _lock) return;
            _lock = Time.unscaledTime + 0.6f;
            OnEnter?.Invoke();
        }
    }
}
