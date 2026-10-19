using UnityEngine;
using UnityEngine.EventSystems;

namespace GameplayPrototype
{
    /// <summary>
    /// Bottom-left touch pad. While a finger is down it drives PlayerMovement;
    /// releasing it hands movement back to WASD.
    /// </summary>
    public class TouchMovePad : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        RectTransform _pad;
        RectTransform _knob;
        PlayerMovement _movement;

        public void Bind(RectTransform knob)
        {
            _pad = (RectTransform)transform;
            _knob = knob;
        }

        public void OnPointerDown(PointerEventData eventData) => Apply(eventData);

        public void OnDrag(PointerEventData eventData) => Apply(eventData);

        public void OnPointerUp(PointerEventData eventData)
        {
            Resolve()?.ClearExternalInput();
            if (_knob != null)
                _knob.anchoredPosition = Vector2.zero;
        }

        void Apply(PointerEventData eventData)
        {
            if (_pad == null) _pad = (RectTransform)transform;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    _pad, eventData.position, eventData.pressEventCamera, out var local))
                return;

            Vector2 center = _pad.rect.center;
            Vector2 delta = local - center;
            float radius = Mathf.Max(8f, _pad.rect.width * 0.42f);
            if (delta.magnitude > radius)
                delta = delta.normalized * radius;
            if (_knob != null)
                _knob.anchoredPosition = delta;

            Vector2 input = delta / radius;
            if (input.magnitude < 0.18f)
                input = Vector2.zero;
            Resolve()?.SetExternalInput(input);
        }

        PlayerMovement Resolve()
        {
            if (_movement != null) return _movement;
            var hero = GameObject.FindGameObjectWithTag("Player");
            if (hero != null) _movement = hero.GetComponent<PlayerMovement>();
            return _movement;
        }
    }
}
