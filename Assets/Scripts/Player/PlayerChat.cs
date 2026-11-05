using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>
    /// Enter opens a one-line chat. Enter again, or the Gửi button, shows that line over the hero.
    /// On touch, a round button beside the interact button opens the line and the on-screen keyboard.
    /// Escape closes the line. While the line is open, game keys and clicks are off:
    /// movement is suppressed, key readers check Typing, and a clear full-screen blocker eats clicks
    /// (a click or tap outside the line closes it without sending).
    /// </summary>
    [DefaultExecutionOrder(100)]
    public class PlayerChat : MonoBehaviour
    {
        const int MaxLen = 42;

        static bool s_open;
        static int s_closedFrame = -1;

        /// <summary>True while the chat line is open, and on the frame it closes (so its Enter/Escape do not leak).</summary>
        public static bool Typing => s_open || Time.frameCount == s_closedFrame;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            s_open = false;
            s_closedFrame = -1;
        }

        InputField _field;
        Text _place;
        GameObject _blocker;
        GameObject _bar;
        RectTransform _barRt;
        GameObject _openBtn;
        RectTransform _useRt;
        PlayerMovement _move;
        int _arm;
        bool _open;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (FindFirstObjectByType<PlayerChat>() != null) return;
            var go = new GameObject("PlayerChat");
            go.AddComponent<PlayerChat>();
        }

        void Awake() => Build();

        void OnDestroy()
        {
            if (_move != null) _move.SuppressInput = false;
            if (_open) s_open = false;
        }

        void Update()
        {
            bool touch = UiKit.IsMobile;
            bool showOpen = touch && !_open && CanOpen();
            if (_openBtn != null && _openBtn.activeSelf != showOpen)
                _openBtn.SetActive(showOpen);

            if (_open) LiftBar();
            DockBesideInteract();

            var keyboard = Keyboard.current;
            if (!_open)
            {
                if (keyboard != null && Pressed(keyboard) && CanOpen()) Open();
                return;
            }

            if (BuildMode.IsOpen || !InWorld())
            {
                Close(false);
                return;
            }

            if (_arm > 0)
            {
                _arm--;
                if (_arm == 0) FocusField();
                return;
            }

            if (keyboard == null) return;
            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                Close(false);
                return;
            }

            if (Pressed(keyboard))
                Close(true);
        }

        bool CanOpen()
        {
            if (BuildMode.IsOpen) return false;
            if (!InWorld()) return false;
            var hero = GameObject.FindGameObjectWithTag("Player");
            if (hero == null || !hero.activeInHierarchy) return false;
            var move = hero.GetComponent<PlayerMovement>();
            return move == null || !move.MovementLocked;
        }

        static bool InWorld()
        {
            if (ReferenceSandboxController.IsActive) return true;
            var nav = WorldNavigator.Instance;
            if (nav != null && nav.Current != WorldZone.None) return true;
            var flow = LevelFlow.Instance;
            if (flow == null) return true;
            return flow.State == LevelState.Playing || flow.State == LevelState.WaveTransition;
        }

        void Open()
        {
            var hero = GameObject.FindGameObjectWithTag("Player");
            _move = hero != null ? hero.GetComponent<PlayerMovement>() : null;
            if (_move != null) _move.SuppressInput = true;
            _open = true;
            s_open = true;
            if (_blocker != null) _blocker.SetActive(true);
            _arm = 2;
            if (_place != null) _place.text = UiKit.IsMobile ? "Chat" : "Enter: chat";
            if (_field != null) _field.text = "";
            if (_bar != null) _bar.SetActive(true);
            if (_openBtn != null) _openBtn.SetActive(false);
            LiftBar();
        }

        void FocusField()
        {
            if (_field == null) return;
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(_field.gameObject);
            _field.ActivateInputField();
        }

        void Close(bool send)
        {
            string text = _field != null ? _field.text : "";
            _open = false;
            s_open = false;
            s_closedFrame = Time.frameCount;
            if (_blocker != null) _blocker.SetActive(false);
            _arm = 0;
            if (_move != null) _move.SuppressInput = false;
            _move = null;
            if (_field != null) _field.DeactivateInputField();
            if (_bar != null) _bar.SetActive(false);
            if (EventSystem.current != null) EventSystem.current.SetSelectedGameObject(null);
            if (!send) return;
            var hero = GameObject.FindGameObjectWithTag("Player");
            if (hero == null) return;
            OverheadChat.Say(hero.transform, text, 4f);
        }

        static bool Pressed(Keyboard keyboard) =>
            keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame;

        void Build()
        {
            var canvasGo = new GameObject("PlayerChatCanvas");
            canvasGo.transform.SetParent(transform, false);
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 300; // above every world HUD canvas (WorldHudTaps 280) so the blocker covers them
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            canvasGo.AddComponent<GraphicRaycaster>();

            // Clear full-screen blocker under the chat line: eats every click/tap meant for the HUD or world.
            _blocker = new GameObject("ChatBlocker");
            _blocker.transform.SetParent(canvasGo.transform, false);
            var blockRt = _blocker.AddComponent<RectTransform>();
            blockRt.anchorMin = Vector2.zero;
            blockRt.anchorMax = Vector2.one;
            blockRt.offsetMin = Vector2.zero;
            blockRt.offsetMax = Vector2.zero;
            var blockImg = _blocker.AddComponent<Image>();
            blockImg.color = new Color(0f, 0f, 0f, 0f);
            blockImg.raycastTarget = true;
            var blockBtn = _blocker.AddComponent<Button>();
            blockBtn.transition = Selectable.Transition.None;
            blockBtn.targetGraphic = blockImg;
            blockBtn.onClick.AddListener(() => { if (_open) Close(false); });
            _blocker.SetActive(false);

            _bar = new GameObject("ChatLine");
            _bar.transform.SetParent(canvasGo.transform, false);
            var bar = _bar.AddComponent<RectTransform>();
            bar.anchorMin = bar.anchorMax = new Vector2(0.5f, 0f);
            bar.pivot = new Vector2(0.5f, 0f);
            bar.anchoredPosition = new Vector2(0f, 168f);
            bar.sizeDelta = new Vector2(480f, 56f);
            _barRt = bar;
            var bg = _bar.AddComponent<Image>();
            bg.sprite = OverheadChat.Panel;
            bg.type = Image.Type.Sliced;
            bg.color = Color.white;

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(_bar.transform, false);
            var textRt = textGo.AddComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(12f, 6f);
            textRt.offsetMax = new Vector2(-96f, -6f);
            var font = WorldLabelsCanvas.Ensure().PixelFont;
            var text = textGo.AddComponent<Text>();
            text.font = font;
            text.fontSize = UiKit.Fs(16);
            text.color = new Color32(0x2A, 0x21, 0x18, 255);
            text.alignment = TextAnchor.MiddleLeft;
            text.supportRichText = false;

            var placeGo = new GameObject("Placeholder");
            placeGo.transform.SetParent(_bar.transform, false);
            var placeRt = placeGo.AddComponent<RectTransform>();
            placeRt.anchorMin = Vector2.zero;
            placeRt.anchorMax = Vector2.one;
            placeRt.offsetMin = new Vector2(12f, 6f);
            placeRt.offsetMax = new Vector2(-96f, -6f);
            var place = placeGo.AddComponent<Text>();
            place.font = font;
            place.fontSize = text.fontSize;
            place.fontStyle = FontStyle.Italic;
            place.color = new Color32(0x6E, 0x5A, 0x40, 255);
            place.alignment = TextAnchor.MiddleLeft;
            place.text = "Enter: chat";
            _place = place;

            var sendGo = new GameObject("Send");
            sendGo.transform.SetParent(_bar.transform, false);
            var sendRt = sendGo.AddComponent<RectTransform>();
            sendRt.anchorMin = sendRt.anchorMax = new Vector2(1f, 0.5f);
            sendRt.pivot = new Vector2(1f, 0.5f);
            sendRt.anchoredPosition = new Vector2(-6f, 0f);
            sendRt.sizeDelta = new Vector2(84f, 44f);
            var sendImg = sendGo.AddComponent<Image>();
            sendImg.sprite = OverheadChat.Panel;
            sendImg.type = Image.Type.Sliced;
            sendImg.color = new Color32(0x2A, 0x21, 0x18, 255);
            var sendLabelGo = new GameObject("Label");
            sendLabelGo.transform.SetParent(sendGo.transform, false);
            var sendLabelRt = sendLabelGo.AddComponent<RectTransform>();
            sendLabelRt.anchorMin = Vector2.zero;
            sendLabelRt.anchorMax = Vector2.one;
            sendLabelRt.offsetMin = Vector2.zero;
            sendLabelRt.offsetMax = Vector2.zero;
            var sendLabel = sendLabelGo.AddComponent<Text>();
            sendLabel.font = font;
            sendLabel.fontSize = UiKit.Fs(16);
            sendLabel.alignment = TextAnchor.MiddleCenter;
            sendLabel.color = new Color32(0xF4, 0xE6, 0xC8, 255);
            sendLabel.text = "Gửi";
            sendLabel.raycastTarget = false;
            var send = sendGo.AddComponent<Button>();
            send.targetGraphic = sendImg;
            send.onClick.AddListener(() => Close(true));

            var openGo = new GameObject("ChatOpen");
            openGo.transform.SetParent(canvasGo.transform, false);
            var openImg = openGo.AddComponent<Image>();
            openImg.preserveAspect = true;
            if (!UiKit.Put(openImg, "UI_InteractButton_Normal", false))
                openImg.sprite = OverheadChat.Panel;
            var openIconGo = new GameObject("Icon");
            openIconGo.transform.SetParent(openGo.transform, false);
            var openIconRt = openIconGo.AddComponent<RectTransform>();
            openIconRt.anchorMin = new Vector2(0.22f, 0.22f);
            openIconRt.anchorMax = new Vector2(0.78f, 0.78f);
            openIconRt.offsetMin = Vector2.zero;
            openIconRt.offsetMax = Vector2.zero;
            var openIcon = openIconGo.AddComponent<Image>();
            openIcon.sprite = UiKit.Get("UI_Icon_Talk");
            openIcon.preserveAspect = true;
            openIcon.raycastTarget = false;
            openIcon.enabled = openIcon.sprite != null;
            var open = openGo.AddComponent<Button>();
            open.targetGraphic = openImg;
            open.transition = Selectable.Transition.SpriteSwap;
            var pressed = open.spriteState;
            pressed.pressedSprite = UiKit.Get("UI_InteractButton_Pressed");
            open.spriteState = pressed;
            open.onClick.AddListener(() => { if (!_open && CanOpen()) Open(); });
            _openBtn = openGo;
            _openBtn.SetActive(UiKit.IsMobile);

            _field = _bar.AddComponent<InputField>();
            _field.textComponent = text;
            _field.placeholder = place;
            _field.characterLimit = MaxLen;
            _field.lineType = InputField.LineType.SingleLine;
            _field.targetGraphic = bg;
            _bar.SetActive(false);
        }

        void DockBesideInteract()
        {
            if (_openBtn == null) return;
            if (_useRt == null)
            {
                var hotbar = FindFirstObjectByType<SkillHotbarUI>(FindObjectsInactive.Include);
                var use = hotbar != null
                    ? hotbar.transform.Find("CombatCanvas/SafeArea/MobileActions/UseButton")
                    : null;
                _useRt = use as RectTransform;
                if (_useRt == null) return;
                _openBtn.transform.SetParent(_useRt.parent, false);
            }
            var rt = (RectTransform)_openBtn.transform;
            rt.anchorMin = _useRt.anchorMin;
            rt.anchorMax = _useRt.anchorMax;
            rt.pivot = _useRt.pivot;
            rt.sizeDelta = _useRt.sizeDelta;
            float gap = 6f * UiKit.Px;
            rt.anchoredPosition = _useRt.anchoredPosition + new Vector2(-(_useRt.sizeDelta.x + gap), 0f);
        }

        void LiftBar()
        {
            if (_barRt == null) return;
            float y = 168f;
            if (TouchScreenKeyboard.visible)
            {
                var canvas = _barRt.GetComponentInParent<Canvas>();
                float scale = canvas != null ? Mathf.Max(0.01f, canvas.scaleFactor) : 1f;
                y = TouchScreenKeyboard.area.height / scale + 16f;
            }
            _barRt.anchoredPosition = new Vector2(0f, y);
        }
    }
}
