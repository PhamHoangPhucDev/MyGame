using UnityEngine;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameplayPrototype
{
    /// <summary>
    /// Portrait overlays: Main Menu, Pause, Wave banner, Victory, Game Over, HUD extras.
    /// Reuses one overlay canvas under Systems — does not stack extra combat canvases.
    /// </summary>
    public class LevelFlowUI : MonoBehaviour
    {
        LevelFlow _flow;
        Canvas _canvas;
        RectTransform _safe;
        Text _levelText;
        Text _waveText;
        Text _bannerText;
        GameObject _menuRoot;
        GameObject _pauseRoot;
        GameObject _gameOverRoot;
        GameObject _victoryRoot;
        GameObject _settingsRoot;
        Text _goStats;
        Text _vicStats;
        Text _starText;
        Button _continueBtn;
        static Font _font;
        static Sprite _pixel;

        public void Bind(LevelFlow flow)
        {
            _flow = flow;
            EnsureUi();
            _flow.StateChanged += OnState;
            _flow.HudChanged += RefreshHud;
            RefreshHud();
            OnState(_flow.State);
        }

        void OnDestroy()
        {
            if (_flow == null) return;
            _flow.StateChanged -= OnState;
            _flow.HudChanged -= RefreshHud;
        }

        void OnState(LevelState state)
        {
            bool menu = state == LevelState.MainMenu;
            if (_menuRoot != null) _menuRoot.SetActive(menu);
            if (_pauseRoot != null && state != LevelState.Paused)
                _pauseRoot.SetActive(false);
            RefreshHud();
        }

        void RefreshHud()
        {
            if (_flow == null) return;
            bool playing = _flow.State == LevelState.Playing || _flow.State == LevelState.WaveTransition || _flow.State == LevelState.Paused;
            if (_levelText != null)
            {
                _levelText.gameObject.SetActive(playing);
                if (playing && _flow.ActiveLevel != null)
                    _levelText.text = $"L{_flow.ActiveLevel.levelNumber}";
            }
            if (_waveText != null)
            {
                _waveText.gameObject.SetActive(playing);
                if (playing)
                    _waveText.text = $"WAVE {_flow.WaveNumber}/{Mathf.Max(1, _flow.WaveCount)}";
            }
            if (_bannerText != null)
            {
                bool show = !string.IsNullOrEmpty(_flow.BannerText) && _flow.BannerNormalized > 0f;
                _bannerText.gameObject.SetActive(show);
                if (show)
                {
                    _bannerText.text = _flow.BannerText;
                    float a = Mathf.Clamp01(_flow.BannerNormalized * 2f);
                    var c = _bannerText.color;
                    c.a = a;
                    _bannerText.color = c;
                }
            }
            if (_continueBtn != null)
                _continueBtn.interactable = GameProgress.HasContinue;
        }

        public void ShowMainMenu()
        {
            HideAllPopups();
            if (_menuRoot != null) _menuRoot.SetActive(true);
            RefreshHud();
        }

        public void HideAllPopups()
        {
            if (_menuRoot != null) _menuRoot.SetActive(false);
            if (_pauseRoot != null) _pauseRoot.SetActive(false);
            if (_gameOverRoot != null) _gameOverRoot.SetActive(false);
            if (_victoryRoot != null) _victoryRoot.SetActive(false);
            if (_settingsRoot != null) _settingsRoot.SetActive(false);
        }

        public void ShowPause()
        {
            if (_pauseRoot != null) _pauseRoot.SetActive(true);
        }

        public void HidePause()
        {
            if (_pauseRoot != null) _pauseRoot.SetActive(false);
        }

        public void ShowGameOver(LevelRunStats stats)
        {
            EnsureUi();
            if (_gameOverRoot != null) _gameOverRoot.SetActive(true);
            if (_goStats != null)
            {
                _goStats.text =
                    $"Wave {stats.waveReached}/{stats.waveTotal}\n" +
                    $"Kills {stats.kills}\n" +
                    $"Best Combo x{stats.bestCombo}\n" +
                    $"Time {FormatTime(stats.seconds)}";
            }
        }

        public void ShowVictory(LevelRunStats stats, int score, int stars)
        {
            EnsureUi();
            if (_victoryRoot != null) _victoryRoot.SetActive(true);
            if (_vicStats != null)
            {
                _vicStats.text =
                    $"Level {stats.levelNumber}\n" +
                    $"Enemies {stats.kills}\n" +
                    $"Best Combo x{stats.bestCombo}\n" +
                    $"Time {FormatTime(stats.seconds)}\n" +
                    $"Score {score}";
            }
            if (_starText != null)
                _starText.text = new string('*', Mathf.Clamp(stars, 1, 3));
        }

        static string FormatTime(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{s / 60:00}:{s % 60:00}";
        }

        void EnsureUi()
        {
            if (_canvas != null) return;
            EnsureFont();
            EnsureSprite();

            var root = transform.Find("LevelFlowRoot");
            if (root == null)
            {
                var rootGo = new GameObject("LevelFlowRoot");
                rootGo.transform.SetParent(transform, false);
                root = rootGo.transform;
            }

            var go = new GameObject("LevelFlowCanvas");
            go.transform.SetParent(root, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(576f, 1024f);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();

            var safeGo = new GameObject("SafeArea");
            safeGo.transform.SetParent(go.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            _levelText = CreateText(_safe, "LevelLabel", new Vector2(0f, 1f), new Vector2(12f, -18f), new Vector2(90f, 28f), 16, TextAnchor.UpperLeft);
            _waveText = CreateText(_safe, "WaveLabel", new Vector2(1f, 1f), new Vector2(-12f, -18f), new Vector2(160f, 28f), 16, TextAnchor.UpperRight);
            _bannerText = CreateText(_safe, "Banner", new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(420f, 80f), 22, TextAnchor.MiddleCenter);
            _bannerText.fontStyle = FontStyle.Bold;
            _bannerText.gameObject.SetActive(false);

            CreatePauseButton();
            BuildMainMenu();
            BuildPause();
            BuildGameOver();
            BuildVictory();
            BuildSettings();
        }

        void CreatePauseButton()
        {
            var btn = CreateButton(_safe, "PauseBtn", new Vector2(1f, 1f), new Vector2(-18f, -52f), new Vector2(56f, 32f), "II",
                () => _flow?.TogglePause());
            var rt = btn.GetComponent<RectTransform>();
            rt.pivot = new Vector2(1f, 1f);
        }

        void BuildMainMenu()
        {
            _menuRoot = CreateDim(_safe, "MainMenu");
            CreateText(_menuRoot.transform, "Title", new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(460f, 90f), 34, TextAnchor.MiddleCenter).text = "GOBLIN CAMP";
            CreateText(_menuRoot.transform, "Sub", new Vector2(0.5f, 0.64f), Vector2.zero, new Vector2(420f, 40f), 16, TextAnchor.MiddleCenter).text = "PIXEL ACTION RPG";
            CreateButton(_menuRoot.transform, "Play", new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(220f, 56f), "PLAY",
                () => _flow?.PlayFromMenu(false));
            _continueBtn = CreateButton(_menuRoot.transform, "Continue", new Vector2(0.5f, 0.38f), Vector2.zero, new Vector2(220f, 48f), "CONTINUE",
                () => _flow?.PlayFromMenu(true));
            CreateButton(_menuRoot.transform, "Settings", new Vector2(0.5f, 0.28f), Vector2.zero, new Vector2(220f, 48f), "SETTINGS",
                () => { if (_settingsRoot != null) _settingsRoot.SetActive(true); });
#if UNITY_STANDALONE || UNITY_EDITOR
            CreateButton(_menuRoot.transform, "Exit", new Vector2(0.5f, 0.18f), Vector2.zero, new Vector2(220f, 48f), "EXIT", QuitGame);
#endif
            _menuRoot.SetActive(false);
        }

        void BuildPause()
        {
            _pauseRoot = CreateDim(_safe, "Pause");
            CreateText(_pauseRoot.transform, "Title", new Vector2(0.5f, 0.62f), Vector2.zero, new Vector2(300f, 50f), 28, TextAnchor.MiddleCenter).text = "PAUSED";
            CreateButton(_pauseRoot.transform, "Resume", new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(220f, 52f), "RESUME",
                () => _flow?.Resume());
            CreateButton(_pauseRoot.transform, "Restart", new Vector2(0.5f, 0.38f), Vector2.zero, new Vector2(220f, 48f), "RESTART",
                () => _flow?.Retry());
            CreateButton(_pauseRoot.transform, "Menu", new Vector2(0.5f, 0.28f), Vector2.zero, new Vector2(220f, 48f), "MAIN MENU",
                () => _flow?.BackToMenu());
            _pauseRoot.SetActive(false);
        }

        void BuildGameOver()
        {
            _gameOverRoot = CreateDim(_safe, "GameOver");
            CreateText(_gameOverRoot.transform, "Title", new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(400f, 60f), 36, TextAnchor.MiddleCenter).text = "GAME OVER";
            _goStats = CreateText(_gameOverRoot.transform, "Stats", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(360f, 140f), 18, TextAnchor.MiddleCenter);
            CreateButton(_gameOverRoot.transform, "Retry", new Vector2(0.5f, 0.32f), Vector2.zero, new Vector2(220f, 52f), "RETRY",
                () => _flow?.Retry());
            CreateButton(_gameOverRoot.transform, "Menu", new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(220f, 48f), "BACK TO MENU",
                () => _flow?.BackToMenu());
            _gameOverRoot.SetActive(false);
        }

        void BuildVictory()
        {
            _victoryRoot = CreateDim(_safe, "Victory");
            CreateText(_victoryRoot.transform, "Title", new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(420f, 56f), 30, TextAnchor.MiddleCenter).text = "LEVEL COMPLETE!";
            _starText = CreateText(_victoryRoot.transform, "Stars", new Vector2(0.5f, 0.64f), Vector2.zero, new Vector2(200f, 36f), 28, TextAnchor.MiddleCenter);
            _starText.color = new Color(1f, 0.9f, 0.35f, 1f);
            _vicStats = CreateText(_victoryRoot.transform, "Stats", new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(360f, 150f), 17, TextAnchor.MiddleCenter);
            CreateButton(_victoryRoot.transform, "Next", new Vector2(0.5f, 0.30f), Vector2.zero, new Vector2(220f, 52f), "NEXT LEVEL",
                () => _flow?.NextLevel());
            CreateButton(_victoryRoot.transform, "Replay", new Vector2(0.5f, 0.21f), Vector2.zero, new Vector2(220f, 44f), "REPLAY",
                () => _flow?.Replay());
            CreateButton(_victoryRoot.transform, "Menu", new Vector2(0.5f, 0.13f), Vector2.zero, new Vector2(220f, 44f), "MAIN MENU",
                () => _flow?.BackToMenu());
            _victoryRoot.SetActive(false);
        }

        void BuildSettings()
        {
            _settingsRoot = CreateDim(_safe, "Settings");
            CreateText(_settingsRoot.transform, "Title", new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(300f, 48f), 26, TextAnchor.MiddleCenter).text = "SETTINGS";
            CreateText(_settingsRoot.transform, "Music", new Vector2(0.5f, 0.56f), Vector2.zero, new Vector2(280f, 28f), 16, TextAnchor.MiddleCenter).text = "MUSIC";
            CreateSlider(_settingsRoot.transform, "MusicSlider", new Vector2(0.5f, 0.50f), GameProgress.MusicVolume, v =>
            {
                GameProgress.MusicVolume = v;
                GameAudio.SetMusicVolume(v);
            });
            CreateText(_settingsRoot.transform, "Sfx", new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(280f, 28f), 16, TextAnchor.MiddleCenter).text = "SFX";
            CreateSlider(_settingsRoot.transform, "SfxSlider", new Vector2(0.5f, 0.36f), GameProgress.SfxVolume, v =>
            {
                GameProgress.SfxVolume = v;
                GameAudio.SetSfxVolume(v);
            });
            CreateButton(_settingsRoot.transform, "Mute", new Vector2(0.5f, 0.26f), Vector2.zero, new Vector2(220f, 44f), "MUTE TOGGLE", () =>
            {
                GameProgress.Muted = !GameProgress.Muted;
                GameAudio.SetMuted(GameProgress.Muted);
            });
            CreateButton(_settingsRoot.transform, "Close", new Vector2(0.5f, 0.16f), Vector2.zero, new Vector2(220f, 48f), "CLOSE",
                () => _settingsRoot.SetActive(false));
            _settingsRoot.SetActive(false);
        }

        static void QuitGame()
        {
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        GameObject CreateDim(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            var img = go.AddComponent<Image>();
            img.sprite = _pixel;
            img.color = new Color(0.02f, 0.05f, 0.04f, 0.78f);
            img.raycastTarget = true;
            return go;
        }

        Text CreateText(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, int fontSize, TextAnchor align)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            if (anchor.x <= 0.01f) rt.pivot = new Vector2(0f, 1f);
            if (anchor.x >= 0.99f) rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = align;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            WorldLabelsCanvas.ApplyPixelOutline(text, Color.black);
            return text;
        }

        Button CreateButton(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size, string label, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var img = go.AddComponent<Image>();
            img.sprite = _pixel;
            img.color = new Color(0.55f, 0.16f, 0.14f, 1f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);
            var colors = btn.colors;
            colors.highlightedColor = new Color(0.75f, 0.25f, 0.18f, 1f);
            colors.pressedColor = new Color(0.35f, 0.1f, 0.08f, 1f);
            btn.colors = colors;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var lrt = labelGo.AddComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
            var text = labelGo.AddComponent<Text>();
            text.font = _font;
            text.text = label;
            text.fontSize = 18;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            return btn;
        }

        void CreateSlider(Transform parent, string name, Vector2 anchor, float value, System.Action<float> onChanged)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(260f, 24f);
            var bg = go.AddComponent<Image>();
            bg.sprite = _pixel;
            bg.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
            var slider = go.AddComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = value;

            var fillArea = new GameObject("Fill Area");
            fillArea.transform.SetParent(go.transform, false);
            var faRt = fillArea.AddComponent<RectTransform>();
            faRt.anchorMin = new Vector2(0f, 0.25f);
            faRt.anchorMax = new Vector2(1f, 0.75f);
            faRt.offsetMin = new Vector2(6f, 0f);
            faRt.offsetMax = new Vector2(-6f, 0f);
            var fill = new GameObject("Fill");
            fill.transform.SetParent(fillArea.transform, false);
            var fRt = fill.AddComponent<RectTransform>();
            fRt.anchorMin = Vector2.zero;
            fRt.anchorMax = Vector2.one;
            fRt.offsetMin = Vector2.zero;
            fRt.offsetMax = Vector2.zero;
            var fImg = fill.AddComponent<Image>();
            fImg.sprite = _pixel;
            fImg.color = new Color(0.35f, 0.7f, 0.4f, 1f);
            slider.fillRect = fRt;

            var handleArea = new GameObject("Handle Slide Area");
            handleArea.transform.SetParent(go.transform, false);
            var haRt = handleArea.AddComponent<RectTransform>();
            haRt.anchorMin = Vector2.zero;
            haRt.anchorMax = Vector2.one;
            haRt.offsetMin = new Vector2(8f, 0f);
            haRt.offsetMax = new Vector2(-8f, 0f);
            var handle = new GameObject("Handle");
            handle.transform.SetParent(handleArea.transform, false);
            var hRt = handle.AddComponent<RectTransform>();
            hRt.sizeDelta = new Vector2(18f, 22f);
            var hImg = handle.AddComponent<Image>();
            hImg.sprite = _pixel;
            hImg.color = Color.white;
            slider.handleRect = hRt;
            slider.targetGraphic = hImg;
            slider.onValueChanged.AddListener(v => onChanged?.Invoke(v));
        }

        static void ApplySafeArea(RectTransform safe)
        {
            Rect area = Screen.safeArea;
            float w = Mathf.Max(1f, Screen.width);
            float h = Mathf.Max(1f, Screen.height);
            safe.anchorMin = new Vector2(area.xMin / w, area.yMin / h);
            safe.anchorMax = new Vector2(area.xMax / w, area.yMax / h);
            safe.offsetMin = Vector2.zero;
            safe.offsetMax = Vector2.zero;
        }

        static void EnsureFont()
        {
            if (_font != null) return;
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        static void EnsureSprite()
        {
            if (_pixel != null) return;
            var tex = new Texture2D(1, 1, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            tex.SetPixel(0, 0, Color.white);
            tex.Apply(false, true);
            _pixel = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        }
    }
}
