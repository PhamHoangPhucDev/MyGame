using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace GameplayPrototype
{
    /// <summary>
    /// Portrait touch. Keyboard and mouse keep working on the 16:9 profile.
    /// A finger that is not on a button is the world pointer (place, paint).
    /// </summary>
    public static class MobileTouch
    {
        public static bool On => UiKit.IsMobile;
        public static bool EraseHeld { get; private set; }
        public static bool InteractHeld { get; private set; }
        static bool _interact;

        public static void SetErase(bool held) => EraseHeld = held;

        public static void SetInteractHeld(bool held)
        {
            if (held && !InteractHeld) _interact = true;
            InteractHeld = held;
        }

        public static bool ConsumeInteract()
        {
            bool v = _interact;
            _interact = false;
            return v;
        }

        public static bool TryFinger(out Vector2 screen, out bool began, out bool held, out int id)
        {
            screen = default;
            began = held = false;
            id = -1;
            var screenTouch = Touchscreen.current;
            if (screenTouch == null) return false;
            var p = screenTouch.primaryTouch;
            held = p.press.isPressed;
            began = p.press.wasPressedThisFrame;
            if (!held) return false;
            screen = p.position.ReadValue();
            id = p.touchId.ReadValue();
            return true;
        }

        public static bool OverUi(int pointerId)
        {
            var es = EventSystem.current;
            if (es == null) return false;
            if (pointerId >= 0 && es.IsPointerOverGameObject(pointerId)) return true;
            return es.IsPointerOverGameObject();
        }
    }

    /// <summary>Portrait buttons: hold E, hold erase, tap to open build.</summary>
    public class MobileActionButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public enum Kind { Interact, Erase, Build }
        public Kind kind;
        bool _down;

        public void OnPointerDown(PointerEventData eventData)
        {
            _down = true;
            if (kind == Kind.Interact) MobileTouch.SetInteractHeld(true);
            else if (kind == Kind.Erase) MobileTouch.SetErase(!MobileTouch.EraseHeld);
            else BuildMode.Toggle();
        }

        public void OnPointerUp(PointerEventData eventData) { if (kind != Kind.Erase) Release(); }
        public void OnPointerExit(PointerEventData eventData) { if (kind != Kind.Erase) Release(); }
        void OnDisable() => Release();

        void Release()
        {
            if (!_down) return;
            _down = false;
            if (kind == Kind.Interact) MobileTouch.SetInteractHeld(false);
        }

        void Update()
        {
            if (_down && kind == Kind.Interact) HomeClear.RequestHold();
        }
    }
}
