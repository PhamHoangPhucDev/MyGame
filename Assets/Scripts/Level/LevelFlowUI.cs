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
        Text _objectiveText;
        Text _bannerText;
        GameObject _menuRoot;
        GameObject _levelSelectRoot;
        GameObject _pauseRoot;
        GameObject _gameOverRoot;
        GameObject _victoryRoot;
        GameObject _settingsRoot;
        Text _langLabel;
        Text _goStats;
        Text _vicStats;
        Text _victoryTitle;
        Text _starText;
        Button _victoryNextBtn;
        Button _victorySelectBtn;
        readonly System.Collections.Generic.List<Text> _selectSlotLabels = new();
        readonly System.Collections.Generic.List<Button> _selectPlayButtons = new();
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
            // StateChanged can fire before LevelFlow.State updates, so hide the run HUD from the new state too.
            if (!InRun(state)) HideRunHud();
        }

        // Guard: something re-enables the wave / objective plates on the result screens; keep them off there.
        void LateUpdate()
        {
            if (_flow == null || InRun(_flow.State) || _safe == null) return;
            var wave = _safe.Find("WavePlate");
            if (wave != null && wave.gameObject.activeSelf) HideRunHud();
        }

        void HideRunHud()
        {
            if (_safe == null) return;
            foreach (var name in new[] { "WavePlate", "ObjectivePlate" })
            {
                var plate = _safe.Find(name);
                if (plate != null) plate.gameObject.SetActive(false);
            }
            if (_bannerText != null) _bannerText.gameObject.SetActive(false);
        }

        void RefreshHud()
        {
            if (_flow == null) return;
            bool playing = InRun(_flow.State);
            if (_pauseBtn != null) _pauseBtn.SetActive(playing);
            if (_levelText != null)
            {
                // Dungeon: the stage tag joins the objective on one line; the stage name sits on the minimap ribbon.
                ShowOn(_levelText, playing && !DungeonRun.Active);
                if (playing && _flow.ActiveLevel != null && !DungeonRun.Active)
                    _levelText.text = $"L{_flow.ActiveLevel.levelNumber}";
            }
            if (_waveText != null)
            {
                ShowOn(_waveText, playing);
                if (playing)
                    _waveText.text = DungeonRun.Active
                        ? "ĐỢT " + _flow.WaveNumber + "/" + Mathf.Max(1, _flow.WaveCount)
                        : $"WAVE {_flow.WaveNumber}/{Mathf.Max(1, _flow.WaveCount)}";
            }
            if (_objectiveText != null)
            {
                // Stage 4 (bridge bar) and 5 (boss bar) replace the objective strip.
                bool strip = playing && !string.IsNullOrEmpty(_flow.ObjectiveText) && !DungeonHud.ReplacesObjective;
                ShowOn(_objectiveText, strip);
                if (playing)
                    _objectiveText.text = DungeonRun.Active
                        ? "ẢI " + DungeonRun.Stage + " · " + (_flow.ObjectiveText ?? "")
                        : _flow.ObjectiveText ?? "";
            }
            if (!playing) HideRunHud();
            if (_bannerText != null)
            {
                // No wave banner over the result screens.
                bool show = playing && !string.IsNullOrEmpty(_flow.BannerText) && _flow.BannerNormalized > 0f;
                _bannerText.gameObject.SetActive(show);
                if (show)
                {
                    _bannerText.text = _flow.BannerText;
                    float a = Mathf.Clamp01(_flow.BannerNormalized * 2f);
                    // Dungeon wave banners are orange (#F08A3A); the stage banner and the rest stay white.
                    bool waveBanner = DungeonRun.Active && _flow.BannerText.StartsWith("ĐỢT");
                    var c = waveBanner ? new Color(0.941f, 0.541f, 0.227f) : Color.white;
                    c.a = a;
                    _bannerText.color = c;
                }
            }
            RefreshLevelSelectSlots();
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
            if (_levelSelectRoot != null) _levelSelectRoot.SetActive(false);
            if (_pauseRoot != null) _pauseRoot.SetActive(false);
            if (_gameOverRoot != null) _gameOverRoot.SetActive(false);
            if (_victoryRoot != null) _victoryRoot.SetActive(false);
            if (_settingsRoot != null) _settingsRoot.SetActive(false);
        }

        public void ShowLevelSelect()
        {
            EnsureUi();
            if (_menuRoot != null) _menuRoot.SetActive(false);
            if (_levelSelectRoot != null) _levelSelectRoot.SetActive(true);
            RefreshLevelSelectSlots();
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
            DressDungeonDefeat(stats);
        }

        public void ShowVictory(LevelRunStats stats, int score, int stars)
        {
            EnsureUi();
            if (_victoryRoot != null) _victoryRoot.SetActive(true);
            if (_victoryTitle != null)
                _victoryTitle.text = "LEVEL COMPLETE!";
            if (_victoryNextBtn != null)
                _victoryNextBtn.gameObject.SetActive(true);
            if (_victorySelectBtn != null)
                _victorySelectBtn.gameObject.SetActive(false);
            FillVictoryStats(stats, score, stars, bossDefeated: false);
            RefreshLevelSelectSlots();
            DressDungeonResult(stats, score);
        }

        public void ShowJourneyComplete(LevelRunStats stats, int score, int stars)
        {
            EnsureUi();
            if (_victoryRoot != null) _victoryRoot.SetActive(true);
            if (_victoryTitle != null)
                _victoryTitle.text = "GAME COMPLETE";
            RestoreResultButtons();
            if (_victoryNextBtn != null)
                _victoryNextBtn.gameObject.SetActive(false);
            if (_victorySelectBtn != null)
                _victorySelectBtn.gameObject.SetActive(true);
            FillVictoryStats(stats, score, stars, bossDefeated: false);
            RefreshLevelSelectSlots();
        }

        public void ShowChapterComplete(LevelRunStats stats, int score, int stars)
        {
            EnsureUi();
            if (_victoryRoot != null) _victoryRoot.SetActive(true);
            if (_victoryTitle != null)
                _victoryTitle.text = "CHAPTER 1 COMPLETE!";
            RestoreResultButtons();
            if (_victoryNextBtn != null)
                _victoryNextBtn.gameObject.SetActive(false);
            if (_victorySelectBtn != null)
                _victorySelectBtn.gameObject.SetActive(true);
            FillVictoryStats(stats, score, stars, bossDefeated: true);
            RefreshLevelSelectSlots();
        }

        void FillVictoryStats(LevelRunStats stats, int score, int stars, bool bossDefeated)
        {
            if (_vicStats != null)
            {
                string bossLine = bossDefeated ? "Boss Defeated: YES\n" : "";
                _vicStats.text =
                    $"Level {stats.levelNumber}\n" +
                    $"Total Enemies Defeated: {stats.kills}\n" +
                    bossLine +
                    $"Highest Combo: x{stats.bestCombo}\n" +
                    $"Completion Time: {FormatTime(stats.seconds)}\n" +
                    $"Score {score}";
            }
            if (_starText != null)
                _starText.text = new string('*', Mathf.Clamp(stars, 1, 3));
            if (_stars != null)
                for (int i = 0; i < _stars.Length; i++)
                    if (_stars[i] != null) UiKit.Put(_stars[i], i < Mathf.Clamp(stars, 1, 3) ? "UI_Star_Large_Filled" : "UI_Star_Large_Empty", false);
        }

        static string FormatTime(float seconds)
        {
            int s = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{s / 60:00}:{s % 60:00}";
        }

        void EnsureUi()
        {
            EnsureFont();
            EnsureSprite();

            var root = transform.Find("LevelFlowRoot");
            if (root == null)
            {
                var rootGo = new GameObject("LevelFlowRoot");
                rootGo.transform.SetParent(transform, false);
                root = rootGo.transform;
            }

            // Rebuild when migrating portrait → landscape so stale canvases are not reused.
            var existingCanvas = root.Find("LevelFlowCanvas");
            if (existingCanvas != null)
            {
                var existingScaler = existingCanvas.GetComponent<CanvasScaler>();
                bool landscapeReady = existingScaler != null &&
                    Mathf.Approximately(existingScaler.referenceResolution.x, 1920f) &&
                    Mathf.Approximately(existingScaler.referenceResolution.y, 1080f);
                bool hasChapterUi = existingCanvas.Find("SafeArea/Victory/Select") != null ||
                                    existingCanvas.Find("Victory/Select") != null;
                // Also scan recursively for the chapter Level Select button.
                if (!hasChapterUi)
                {
                    foreach (var t in existingCanvas.GetComponentsInChildren<Transform>(true))
                    {
                        if (t.name == "Select" && t.parent != null && t.parent.name == "Victory")
                        {
                            hasChapterUi = true;
                            break;
                        }
                    }
                }
                if (landscapeReady && hasChapterUi && _canvas != null)
                    return;
                Destroy(existingCanvas.gameObject);
                _canvas = null;
                _safe = null;
                _menuRoot = null;
                _levelSelectRoot = null;
                _pauseRoot = null;
                _gameOverRoot = null;
                _victoryRoot = null;
                _settingsRoot = null;
                _selectSlotLabels.Clear();
                _selectPlayButtons.Clear();
            }
            else if (_canvas != null)
                return;

            var go = new GameObject("LevelFlowCanvas");
            go.transform.SetParent(root, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 200;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            UiKit.FitScaler(go);
            go.AddComponent<GraphicRaycaster>();

            var safeGo = new GameObject("SafeArea");
            safeGo.transform.SetParent(go.transform, false);
            _safe = safeGo.AddComponent<RectTransform>();
            ApplySafeArea(_safe);

            // TOP-LEFT under HP: level + wave; objective near top-center.
            _levelText = CreateText(_safe, "LevelLabel", new Vector2(0f, 1f), new Vector2(24f, -48f), new Vector2(120f, 28f), 18, TextAnchor.UpperLeft);
            _waveText = CreateText(_safe, "WaveLabel", new Vector2(0f, 1f), new Vector2(150f, -48f), new Vector2(200f, 28f), 18, TextAnchor.UpperLeft);
            _objectiveText = CreateText(_safe, "Objective", new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(520f, 28f), 16, TextAnchor.UpperCenter);
            BuildHudPlates();
            _bannerText = CreateText(_safe, "Banner", new Vector2(0.5f, 0.78f), Vector2.zero, new Vector2(720f, 90f), 28, TextAnchor.MiddleCenter);
            _bannerText.fontStyle = FontStyle.Bold;
            _bannerText.gameObject.SetActive(false);

            CreatePauseButton();
            BuildMainMenu();
            BuildLevelSelect();
            BuildPause();
            BuildGameOver();
            BuildVictory();
            BuildSettings();
        }

        void CreatePauseButton()
        {
            var btn = CreateButton(_safe, "PauseBtn", new Vector2(1f, 1f), new Vector2(-24f, -18f), new Vector2(64f, 40f), "II",
                () => _flow?.TogglePause());
            var rt = btn.GetComponent<RectTransform>();
            rt.pivot = new Vector2(1f, 1f);
            _pauseBtn = btn.gameObject;
            // The canvas can be rebuilt mid-menu (level select), so start in the right state.
            _pauseBtn.SetActive(_flow != null && InRun(_flow.State));
            // Kit icon button top-right; the village Bag/Map buttons sit to its left.
            if (UiKit.Get("UI_Button_Icon_Normal") == null) return;
            UiKit.Dress(btn, "Icon");
            var label = btn.GetComponentInChildren<Text>();
            if (label != null) label.gameObject.SetActive(false);
            UiKit.Icon(btn.transform, "UI_Icon_Pause", UiKit.Center, Vector2.zero, 14f);
            UiKit.Place(btn, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10f, -8f), new Vector2(22f, 22f));
        }

        void BuildMainMenu()
        {
            _menuRoot = CreateDim(_safe, "MainMenu");
            CreateText(_menuRoot.transform, "Title", new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(460f, 90f), 34, TextAnchor.MiddleCenter).text = "GOBLIN CAMP";
            CreateText(_menuRoot.transform, "Sub", new Vector2(0.5f, 0.64f), Vector2.zero, new Vector2(420f, 40f), 16, TextAnchor.MiddleCenter).text = "PIXEL ACTION RPG";
            CreateButton(_menuRoot.transform, "Play", new Vector2(0.5f, 0.50f), Vector2.zero, new Vector2(220f, 52f), "VILLAGE",
                () =>
                {
                    if (WorldNavigator.Instance != null) WorldNavigator.Instance.EnterFromMenu();
                    else _flow?.PlayFromMenu(false);
                });
            // LEVEL SELECT and CONTINUE started the old campaign levels 1-5, retired for the Goblin Camp dungeon
            // (entered from the village guard).
            CreateButton(_menuRoot.transform, "Settings", new Vector2(0.5f, 0.40f), Vector2.zero, new Vector2(220f, 48f), "SETTINGS",
                () => { if (_settingsRoot != null) _settingsRoot.SetActive(true); });
#if UNITY_STANDALONE || UNITY_EDITOR
            CreateButton(_menuRoot.transform, "Exit", new Vector2(0.5f, 0.30f), Vector2.zero, new Vector2(220f, 48f), "EXIT", QuitGame);
#endif
            KitPanel(_menuRoot, null, 0.24f, 0.565f, 230f);
            _menuRoot.SetActive(false);
        }

        void BuildLevelSelect()
        {
            _levelSelectRoot = CreateDim(_safe, "LevelSelect");
            CreateText(_levelSelectRoot.transform, "Title", new Vector2(0.5f, 0.88f), Vector2.zero, new Vector2(400f, 48f), 26, TextAnchor.MiddleCenter).text = "SELECT LEVEL";
            _selectSlotLabels.Clear();
            _selectPlayButtons.Clear();
            // Show 5 slots; only 1-2 playable in this Phase 12 step.
            string[] names =
            {
                "1  FOREST CAMP",
                "2  FOREST TRAIL",
                "3  ABANDONED CAMP",
                "4  GOBLIN OUTPOST",
                "5  GOBRIDER STRONGHOLD"
            };
            for (int i = 0; i < 5; i++)
            {
                int level = i + 1;
                float y = 0.72f - i * 0.11f;
                var label = CreateText(_levelSelectRoot.transform, $"SlotInfo{level}", new Vector2(0.5f, y + 0.035f),
                    Vector2.zero, new Vector2(420f, 28f), 14, TextAnchor.MiddleCenter);
                label.text = names[i];
                _selectSlotLabels.Add(label);
                var playBtn = CreateButton(_levelSelectRoot.transform, $"Play{level}", new Vector2(0.5f, y - 0.02f),
                    Vector2.zero, new Vector2(200f, 36f), "PLAY", () => _flow?.StartLevelByNumber(level));
                _selectPlayButtons.Add(playBtn);
            }
            CreateButton(_levelSelectRoot.transform, "Back", new Vector2(0.5f, 0.10f), Vector2.zero, new Vector2(200f, 44f), "BACK",
                () =>
                {
                    if (_levelSelectRoot != null) _levelSelectRoot.SetActive(false);
                    if (_menuRoot != null) _menuRoot.SetActive(true);
                });
            KitPanel(_levelSelectRoot, "SELECT LEVEL", 0.04f, 0.92f, 260f);
            for (int i = 0; i < _selectSlotLabels.Count; i++)
                SlotRow(_levelSelectRoot.transform, 0.72f - i * 0.11f);
            _levelSelectRoot.SetActive(false);
        }

        void RefreshLevelSelectSlots()
        {
            if (_selectSlotLabels == null || _selectSlotLabels.Count == 0 || _flow == null) return;
            for (int i = 0; i < _selectSlotLabels.Count; i++)
            {
                int level = i + 1;
                bool unlocked = GameProgress.IsLevelUnlocked(level);
                var cfg = _flow.GetLevelConfig(level);
                bool playable = unlocked && cfg != null && level <= LevelFlow.CampaignLevelCount;
                string title = cfg != null ? cfg.levelTitle : (level == 3 ? "ABANDONED CAMP" : level == 4 ? "GOBLIN OUTPOST" : level == 5 ? "GOBRIDER STRONGHOLD" : $"LEVEL {level}");
                int stars = GameProgress.GetBestStars(level);
                int score = GameProgress.GetBestScore(level);
                if (!unlocked)
                {
                    _selectSlotLabels[i].text = $"L{level}  {title}\nLOCKED";
                    _selectSlotLabels[i].color = new Color(0.65f, 0.65f, 0.65f, 1f);
                }
                else if (!playable)
                {
                    _selectSlotLabels[i].text = $"L{level}  {title}\nCOMING SOON";
                    _selectSlotLabels[i].color = new Color(0.85f, 0.8f, 0.55f, 1f);
                }
                else
                {
                    string starStr = stars > 0 ? new string('*', stars) : "-";
                    _selectSlotLabels[i].text = $"L{level}  {title}\n{starStr}   Best {score}";
                    _selectSlotLabels[i].color = Color.white;
                }

                if (i < _selectPlayButtons.Count && _selectPlayButtons[i] != null)
                {
                    _selectPlayButtons[i].interactable = playable;
                    var label = _selectPlayButtons[i].GetComponentInChildren<Text>();
                    if (label != null)
                        label.text = playable ? "PLAY" : (unlocked ? "SOON" : "LOCKED");
                }
            }

            // Dungeon runs: DressDungeonResult owns the Next button. The campaign rule below would lock it,
            // because a stage's nextLevelNumber (2..5) points at campaign levels the player may not have unlocked.
            if (_victoryNextBtn != null && _flow != null && !DungeonRun.Active)
            {
                var active = _flow.ActiveLevel;
                int nextNum = active != null ? active.nextLevelNumber : 0;
                bool canNext = nextNum > 0 && GameProgress.IsLevelUnlocked(nextNum) && _flow.GetLevelConfig(nextNum) != null;
                _victoryNextBtn.interactable = canNext;
            }
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
            KitPanel(_pauseRoot, "PAUSED", 0.22f, 0.68f, 170f);
            _pauseRoot.SetActive(false);
        }

        void BuildGameOver()
        {
            _gameOverRoot = CreateDim(_safe, "GameOver");
            CreateText(_gameOverRoot.transform, "Title", new Vector2(0.5f, 0.68f), Vector2.zero, new Vector2(400f, 60f), 36, TextAnchor.MiddleCenter).text = "GAME OVER";
            _goStats = CreateText(_gameOverRoot.transform, "Stats", new Vector2(0.5f, 0.52f), Vector2.zero, new Vector2(360f, 140f), 18, TextAnchor.MiddleCenter);
            CreateButton(_gameOverRoot.transform, "Retry", new Vector2(0.5f, 0.32f), Vector2.zero, new Vector2(220f, 52f), "RETRY",
                () => _flow?.Retry());
            CreateButton(_gameOverRoot.transform, "Village", new Vector2(0.5f, 0.42f), Vector2.zero, new Vector2(220f, 44f), "VILLAGE",
                () => WorldNavigator.Instance?.ReturnFromDungeon());
            CreateButton(_gameOverRoot.transform, "Menu", new Vector2(0.5f, 0.22f), Vector2.zero, new Vector2(220f, 48f), "BACK TO MENU",
                () => _flow?.BackToMenu());
            KitPanel(_gameOverRoot, "GAME OVER", 0.16f, 0.74f, 210f, "Red");
            _gameOverRoot.SetActive(false);
        }

        void BuildVictory()
        {
            _victoryRoot = CreateDim(_safe, "Victory");
            _victoryTitle = CreateText(_victoryRoot.transform, "Title", new Vector2(0.5f, 0.72f), Vector2.zero, new Vector2(520f, 56f), 30, TextAnchor.MiddleCenter);
            _victoryTitle.text = "LEVEL COMPLETE!";
            // Fit inside the narrower portrait panel instead of spilling past its border.
            _victoryTitle.resizeTextForBestFit = true;
            _victoryTitle.resizeTextMinSize = 16;
            _victoryTitle.resizeTextMaxSize = _victoryTitle.fontSize;
            _victoryTitle.horizontalOverflow = HorizontalWrapMode.Wrap;
            _victoryTitle.gameObject.AddComponent<UiLayout>().Both(UiLayout.Spec.At(new Vector2(0.5f, 0.72f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(224f, 28f)));
            _starText = CreateText(_victoryRoot.transform, "Stars", new Vector2(0.5f, 0.64f), Vector2.zero, new Vector2(200f, 36f), 28, TextAnchor.MiddleCenter);
            _starText.color = new Color(1f, 0.9f, 0.35f, 1f);
            _vicStats = CreateText(_victoryRoot.transform, "Stats", new Vector2(0.5f, 0.48f), Vector2.zero, new Vector2(420f, 160f), 17, TextAnchor.MiddleCenter);
            CreateButton(_victoryRoot.transform, "Village", new Vector2(0.5f, 0.39f), Vector2.zero, new Vector2(220f, 40f), "VILLAGE",
                () => WorldNavigator.Instance?.ReturnFromDungeon());
            _victoryNextBtn = CreateButton(_victoryRoot.transform, "Next", new Vector2(0.5f, 0.30f), Vector2.zero, new Vector2(220f, 52f), "NEXT LEVEL",
                () => _flow?.NextLevel());
            _victorySelectBtn = CreateButton(_victoryRoot.transform, "Select", new Vector2(0.5f, 0.30f), Vector2.zero, new Vector2(220f, 52f), "LEVEL SELECT",
                () =>
                {
                    if (_victoryRoot != null) _victoryRoot.SetActive(false);
                    _flow?.BackToMenu();
                    ShowLevelSelect();
                });
            _victorySelectBtn.gameObject.SetActive(false);
            CreateButton(_victoryRoot.transform, "Replay", new Vector2(0.5f, 0.21f), Vector2.zero, new Vector2(220f, 44f), "REPLAY LEVEL",
                () => _flow?.Replay());
            CreateButton(_victoryRoot.transform, "Menu", new Vector2(0.5f, 0.13f), Vector2.zero, new Vector2(220f, 44f), "MAIN MENU",
                () => _flow?.BackToMenu());
            KitPanel(_victoryRoot, null, 0.08f, 0.79f, 240f, "Gold", "UI_Panel_Reward");
            _stars = new Image[3];
            for (int i = 0; i < 3; i++)
            {
                var star = new GameObject("Star" + i).AddComponent<Image>();
                star.transform.SetParent(_victoryRoot.transform, false);
                star.raycastTarget = false;
                UiKit.Put(star, "UI_Star_Large_Empty", false);
                var layout = star.gameObject.AddComponent<UiLayout>();
                layout.Both(UiLayout.Spec.At(new Vector2(0.5f, 0.64f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 30f, i == 1 ? 4f : 0f), new Vector2(24f, 24f)));
                _stars[i] = star;
            }
            if (_starText != null) _starText.gameObject.SetActive(false);
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
            CreateButton(_settingsRoot.transform, "Mute", new Vector2(0.5f, 0.27f), Vector2.zero, new Vector2(220f, 44f), "MUTE TOGGLE", () =>
            {
                GameProgress.Muted = !GameProgress.Muted;
                GameAudio.SetMuted(GameProgress.Muted);
            });
            var lang = CreateButton(_settingsRoot.transform, "Language", new Vector2(0.5f, 0.19f), Vector2.zero, new Vector2(220f, 44f), "LANGUAGE: VI",
                () => { Loc.Toggle(); RefreshLang(); });
            _langLabel = lang.GetComponentInChildren<Text>();
            RefreshLang();
            CreateButton(_settingsRoot.transform, "Close", new Vector2(0.5f, 0.10f), Vector2.zero, new Vector2(220f, 48f), "CLOSE",
                () => _settingsRoot.SetActive(false));
            KitPanel(_settingsRoot, "SETTINGS", 0.03f, 0.73f, 210f);
            _settingsRoot.SetActive(false);
        }

        void RefreshLang()
        {
            if (_langLabel == null) return;
            _langLabel.text = Loc.Current == Lang.VI ? "LANGUAGE: VI" : "LANGUAGE: EN";
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
            text.fontSize = UiKit.Fs(Mathf.Max(8, Mathf.RoundToInt(fontSize / 8f) * 8));
            text.alignment = align;
            text.color = Color.white;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            WorldLabelsCanvas.ApplyPixelOutline(text, Color.black);
            LocText.Attach(text);
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
            text.fontSize = UiKit.Fs(16);
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            LocText.Attach(text);
            UiKit.Dress(btn, KindFor(name));
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
            UiKit.Put(bg, "UI_Slider_Track", true);
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
            UiKit.Put(fImg, "UI_Slider_Fill", false);
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
            if (UiKit.Put(hImg, "UI_Slider_Knob", false)) hRt.sizeDelta = new Vector2(10f, 12f) * UiKit.Px;
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

        Image[] _stars;
        GameObject _pauseBtn;

        static bool InRun(LevelState s) => s == LevelState.Playing || s == LevelState.WaveTransition || s == LevelState.Paused;

        /// <summary>Button kind by role: main action Primary, leave/exit Danger, the rest Secondary.</summary>
        static string KindFor(string name)
        {
            switch (name)
            {
                case "Play":
                case "Resume":
                case "Retry":
                case "Next":
                case "Close":
                    return "Primary";
                case "Exit":
                case "Menu":
                    return "Danger";
                default:
                    return name.StartsWith("Play") ? "Primary" : "Secondary";
            }
        }

        /// <summary>Kit window behind an existing popup: panel between two screen heights and a title ribbon.</summary>
        /// <summary>
        /// Dungeon result: stars, score, time, reward slots (icon + amount) and three buttons
        /// ẢI TIẾP / CHƠI LẠI / VỀ LÀNG. Campaign results keep their own layout (RestoreResultButtons).
        /// </summary>
        void DressDungeonResult(LevelRunStats stats, int score)
        {
            if (!DungeonRun.Active)
            {
                RestoreResultButtons();
                return;
            }
            int dungeon = DungeonRun.Dungeon, stageNo = DungeonRun.Stage;
            var stage = DungeonCatalog.StageAt(dungeon, stageNo);
            if (_victoryTitle != null) _victoryTitle.text = "ẢI " + stageNo + " XONG";
            if (_vicStats != null)
            {
                int mark = stage.threeStarSeconds;
                _vicStats.text =
                    stage.name.ToUpperInvariant() + "\n" +
                    "ĐIỂM " + score + "\n" +
                    "THỜI GIAN " + FormatTime(stats.seconds) + "  ·  MỐC ★ " + FormatTime(mark) + "\n" +
                    "HẠ " + stats.kills + " QUÁI  ·  COMBO x" + stats.bestCombo;
                Anchor(_vicStats.transform, 0.53f);
            }

            bool more = stageNo < DungeonCatalog.Get(dungeon).stages.Length && DungeonProgress.StageUnlocked(dungeon, stageNo + 1);
            ResultButton(_victoryRoot, "Next", more, "ẢI TIẾP", 0.30f);
            if (_victoryNextBtn != null) _victoryNextBtn.interactable = more;
            ResultButton(_victoryRoot, "Replay", true, "CHƠI LẠI", more ? 0.225f : 0.28f);
            ResultButton(_victoryRoot, "Village", true, "VỀ LÀNG", more ? 0.15f : 0.205f);
            ResultButton(_victoryRoot, "Menu", false, null, 0f);
            if (_victorySelectBtn != null) _victorySelectBtn.gameObject.SetActive(false);
            ShowRewards(stage);
        }

        void ShowRewards(DungeonStage stage)
        {
            if (_victoryRoot == null) return;
            if (_rewardRow == null)
            {
                _rewardRow = UiKit.Rect(_victoryRoot.transform, "DungeonRewards");
                UiKit.Place(_rewardRow, new Vector2(0.5f, 0.395f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(120f, 36f));
                for (int i = 0; i < 3; i++)
                {
                    var slot = UiKit.Panel(_rewardRow, "Slot" + i, "UI_Dungeon_RewardSlot");
                    UiKit.Place(slot, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 42f, 0f), new Vector2(36f, 36f));
                    var icon = UiKit.Icon(slot.transform, "", new Vector2(0.5f, 0.5f), new Vector2(0f, 2f), 32f);
                    icon.name = "Icon";
                    var amount = UiKit.Label(slot.transform, "Amount", "", 8, Pal.TextW, TextAnchor.LowerRight);
                    UiKit.Place(amount, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-3f, 1f), new Vector2(30f, 12f));
                    WorldLabelsCanvas.ApplyPixelOutline(amount, new Color32(0x14, 0x10, 0x18, 0xFF));
                }
            }
            _rewardRow.gameObject.SetActive(true);
            int mul = Mathf.Max(1, DungeonCatalog.LastGrantMultiplier);
            for (int i = 0; i < 3; i++)
            {
                var slot = _rewardRow.Find("Slot" + i);
                if (slot == null) continue;
                bool has = stage.rewards != null && i < stage.rewards.Length;
                slot.gameObject.SetActive(has);
                if (!has) continue;
                var icon = slot.Find("Icon").GetComponent<Image>();
                icon.sprite = UiKit.Get(UiKit.ItemIcon(stage.rewards[i].itemId));
                icon.enabled = icon.sprite != null;
                slot.Find("Amount").GetComponent<Text>().text = (stage.rewards[i].amount * mul).ToString();
            }
        }

        /// <summary>Dungeon defeat: THỬ LẠI / VỀ LÀNG and the guard's hint.</summary>
        void DressDungeonDefeat(LevelRunStats stats)
        {
            if (!DungeonRun.Active)
            {
                ResultButton(_gameOverRoot, "Retry", true, "RETRY", 0.32f);
                ResultButton(_gameOverRoot, "Village", true, "VILLAGE", 0.42f);
                ResultButton(_gameOverRoot, "Menu", true, "BACK TO MENU", 0.22f);
                return;
            }
            var stage = DungeonCatalog.StageAt(DungeonRun.Dungeon, DungeonRun.Stage);
            if (_goStats != null)
                _goStats.text = "ẢI " + DungeonRun.Stage + " · " + stage.name.ToUpperInvariant() + "\n" +
                                "ĐỢT " + stats.waveReached + "/" + stats.waveTotal + "  ·  HẠ " + stats.kills + " QUÁI\n" +
                                "THỜI GIAN " + FormatTime(stats.seconds) + "\n\n" + stage.guard;
            ResultButton(_gameOverRoot, "Retry", true, "THỬ LẠI", 0.32f);
            ResultButton(_gameOverRoot, "Village", true, "VỀ LÀNG", 0.23f);
            ResultButton(_gameOverRoot, "Menu", false, null, 0f);
        }

        void RestoreResultButtons()
        {
            if (_rewardRow != null) _rewardRow.gameObject.SetActive(false);
            if (_vicStats != null) Anchor(_vicStats.transform, 0.48f);
            ResultButton(_victoryRoot, "Village", true, "VILLAGE", 0.39f);
            ResultButton(_victoryRoot, "Next", _victoryNextBtn != null && _victoryNextBtn.gameObject.activeSelf, "NEXT LEVEL", 0.30f);
            ResultButton(_victoryRoot, "Replay", true, "REPLAY LEVEL", 0.21f);
            ResultButton(_victoryRoot, "Menu", true, "MAIN MENU", 0.13f);
        }

        static void ResultButton(GameObject root, string name, bool on, string label, float y)
        {
            if (root == null) return;
            var t = root.transform.Find(name);
            if (t == null) return;
            t.gameObject.SetActive(on);
            if (!on) return;
            Anchor(t, y);
            var text = t.GetComponentInChildren<Text>(true);
            if (text != null && label != null) text.text = label;
        }

        static void Anchor(Transform t, float y)
        {
            var rt = t as RectTransform;
            if (rt == null) return;
            rt.anchorMin = new Vector2(rt.anchorMin.x, y);
            rt.anchorMax = new Vector2(rt.anchorMax.x, y);
        }

        RectTransform _rewardRow;

        static void KitPanel(GameObject root, string title, float yMin, float yMax, float widthPx, string ribbon = "", string sprite = "UI_Panel_Main")
        {
            if (root == null) return;
            var panel = new GameObject("KitPanel").AddComponent<Image>();
            panel.transform.SetParent(root.transform, false);
            panel.transform.SetSiblingIndex(0);
            panel.raycastTarget = false;
            if (!UiKit.Put(panel, sprite, true))
            {
                Object.Destroy(panel.gameObject);
                return;
            }
            panel.gameObject.AddComponent<UiLayout>().Both(UiLayout.Spec.Stretch(new Vector2(0.5f, yMin), new Vector2(0.5f, yMax),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(widthPx, 0f)));
            if (string.IsNullOrEmpty(title)) title = ribbon == "Gold" ? "VICTORY" : null;
            if (string.IsNullOrEmpty(title)) return;
            UiKit.Ribbon(panel.transform, title, ribbon);
            var old = root.transform.Find("Title");
            if (old != null && ribbon != "Gold") old.gameObject.SetActive(false);
        }

        static void SlotRow(Transform parent, float y)
        {
            var row = new GameObject("SlotRow").AddComponent<Image>();
            row.transform.SetParent(parent, false);
            row.transform.SetSiblingIndex(1);
            row.raycastTarget = false;
            if (!UiKit.Put(row, "UI_ListRow_Normal", true))
            {
                Object.Destroy(row.gameObject);
                return;
            }
            row.gameObject.AddComponent<UiLayout>().Both(UiLayout.Spec.Stretch(new Vector2(0.5f, y - 0.05f), new Vector2(0.5f, y + 0.06f),
                new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(236f, 0f)));
        }

        /// <summary>Wave ribbon (top centre) and objective plate under it, with the level tag on the plate.</summary>
        void BuildHudPlates()
        {
            var wave = new GameObject("WavePlate").AddComponent<Image>();
            wave.transform.SetParent(_safe, false);
            wave.raycastTarget = false;
            UiKit.Put(wave, "UI_Header_Ribbon", true);
            wave.gameObject.AddComponent<UiLayout>()
                .Landscape(UiLayout.Spec.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -15f), new Vector2(108f, 18f)))
                .Portrait(UiLayout.Spec.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(120f, 18f)));
            Adopt(_waveText, wave.transform, new Vector2(0f, 2f), TextAnchor.MiddleCenter, new Color(0.97f, 0.91f, 0.66f));

            var plate = new GameObject("ObjectivePlate").AddComponent<Image>();
            plate.transform.SetParent(_safe, false);
            plate.raycastTarget = false;
            UiKit.Put(plate, "UI_Panel_HUD", true);
            plate.gameObject.AddComponent<UiLayout>()
                .Landscape(UiLayout.Spec.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(188f, 14f)))
                .Portrait(UiLayout.Spec.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(170f, 14f)));
            Adopt(_objectiveText, plate.transform, new Vector2(10f, 0f), TextAnchor.MiddleCenter, new Color(0.75f, 0.85f, 0.94f));
            Adopt(_levelText, plate.transform, new Vector2(6f, 0f), TextAnchor.MiddleLeft, new Color(0.91f, 0.78f, 0.44f));
        }

        static void Adopt(Text text, Transform plate, Vector2 padPx, TextAnchor align, Color color)
        {
            if (text == null) return;
            text.transform.SetParent(plate, false);
            text.alignment = align;
            text.color = color;
            var layout = text.gameObject.AddComponent<UiLayout>();
            layout.fontPx = 8;
            layout.Both(UiLayout.Spec.Stretch(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), padPx, new Vector2(-padPx.x * 2f, 0f)));
        }

        /// <summary>Shows a HUD text and its plate. The objective plate stays up while the objective is visible.</summary>
        static void ShowOn(Text text, bool on)
        {
            if (text == null) return;
            text.gameObject.SetActive(on);
            var parent = text.transform.parent;
            if (parent == null || !parent.name.EndsWith("Plate")) return;
            if (parent.name == "ObjectivePlate" && text.name != "Objective") return;
            parent.gameObject.SetActive(on);
        }

        static void EnsureFont()
        {
            if (_font != null) return;
            _font = UiKit.Font;
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
