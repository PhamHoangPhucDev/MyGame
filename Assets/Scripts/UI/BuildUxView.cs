using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>
    /// Build UX v2 (Docs/WorldArt/HomeDistrict/build/BUILD_UX_V2_SPEC.md), display only.
    /// Grid while building, building foot cells, floating TICK / TURN / CANCEL bar over the ghost, red reason chip,
    /// context menu over a selected decor piece, recentre button, dust + rising resource numbers + haptic on place/remove.
    /// Calls only existing public APIs (YardHand, HomeDecor, BuildMode); gaps waiting on Cursor are marked TODO(Cn),
    /// see BUILD_UX_V2_CONTRACT.md.
    /// </summary>
    public class BuildUxView : MonoBehaviour
    {
        static BuildUxView _instance;

        static readonly Color Plum = new Color32(0x4A, 0x31, 0x40, 0xF0);
        static readonly Color PlumCard = new Color32(0x6A, 0x40, 0x48, 0xFF);
        static readonly Color PlumBorder = new Color32(0xB8, 0x9A, 0xA0, 0xFF);
        static readonly Color GreenDk = new Color32(0x3E, 0x7A, 0x3E, 0xFF);
        static readonly Color Green = new Color32(0x7E, 0xD6, 0x7E, 0xFF);
        static readonly Color Red = new Color32(0xE0, 0x48, 0x48, 0xFF);
        static readonly Color Gold = new Color32(0xF2, 0xD8, 0x4A, 0xFF);
        static readonly Color Cyan = new Color32(0x8E, 0xC8, 0xF0, 0xFF);
        static readonly Color GreyBtn = new Color32(0x4A, 0x4A, 0x52, 0xFF);
        static readonly Color Grey = new Color32(0x8A, 0x92, 0xA0, 0xFF);
        static readonly Color KeyFill = new Color32(0x20, 0x20, 0x2A, 0xFF);
        static readonly Color KeyBorder = new Color32(0x5A, 0x5E, 0x6A, 0xFF);
        static readonly Color CancelInk = new Color32(0xF0, 0xB0, 0xB0, 0xFF);

        const int GridOrder = -8950;   // above the baked ground (HomeDistrict GroundOrder -9000 .. -8960), under props
        const string PuffPath = "World/HomeDistrict/Wild/WL_Puff";

        RectTransform _safe;
        Canvas _canvas;
        // floating bar
        RectTransform _bar, _chip, _menu, _tag, _info, _keys;
        Image _tickBorder, _tickFill, _tickIcon, _woodIcon, _stoneIcon;
        Text _woodText, _stoneText, _stockText, _chipText, _tagText, _infoText;
        Button _turn, _cancel, _aim;
        GameObject _turnKey;
        float _shakeUntil;
        // world
        SpriteRenderer _grid, _spot, _foot;
        Transform _root;
        SpriteRenderer _decorGhost, _landGhost;
        float _findAt;
        // selection
        int _selIndex = -1;
        Bounds _selBounds;
        float _selAt;
        bool _infoOpen;
        // fx
        Sprite[] _puff;
        readonly List<Puff> _puffs = new List<Puff>();
        readonly List<Floater> _floats = new List<Floater>();
        readonly List<string> _seenDefs = new List<string>();
        readonly List<Vector2> _seenPos = new List<Vector2>();
        bool _seenValid;

        struct Puff { public SpriteRenderer sr; public float t0; }
        struct Floater { public RectTransform rt; public CanvasGroup group; public Vector3 world; public float t0; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _instance = null; ForceTouch = null; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (_instance != null) return;
            var go = new GameObject("BuildUxView");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<BuildUxView>();
        }

        void OnEnable() => BuildMode.SiteFx += OnSite;
        void OnDisable() => BuildMode.SiteFx -= OnSite;

        /// <summary>Test hook: force touch (true) or PC (false) hints/keycaps; null = detect.</summary>
        public static bool? ForceTouch;

        /// <summary>Touch layout (no keycaps): portrait, a phone/tablet, or a touch screen without a mouse.</summary>
        internal static bool IsTouch => ForceTouch ?? UiKit.IsMobile || (Touchscreen.current != null && Mouse.current == null);

        // ------------------------------------------------------------------ frame

        void LateUpdate()
        {
            UpdatePuffs();
            UpdateFloats();
            bool open = BuildMode.IsOpen && BuildMode.Driver != null && BuildMode.Driver.Hud != null;
            if (!open)
            {
                if (_canvas != null && _canvas.gameObject.activeSelf) _canvas.gameObject.SetActive(false);
                ShowWorld(false);
                _seenValid = false;
                _selIndex = -1;
                _infoOpen = false;
                return;
            }
            EnsureUi();
            if (!_canvas.gameObject.activeSelf) _canvas.gameObject.SetActive(true);
            MuteOldBar();
            FindWorld();
            WatchPieces();
            PaintGrid();
            PaintFoot();
            PaintBar();
            PaintMenu();
            PlaceAim();
        }

        /// <summary>The v1 TICK/HUY/DOI bar (YardHand) is replaced by the floating bar; keep it alive but invisible.</summary>
        void MuteOldBar()
        {
            var old = BuildMode.Driver.Hud.transform.Find("YardHand");
            if (old == null) return;
            var group = old.GetComponent<CanvasGroup>();
            if (group == null) group = old.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
        }

        void FindWorld()
        {
            if (_root == null || Time.unscaledTime > _findAt)
            {
                _findAt = Time.unscaledTime + 1f;
                var r = GameObject.Find("HomeDistrict");
                _root = r != null ? r.transform : null;
                if (_decorGhost == null) { var g = GameObject.Find("DecorGhost"); if (g != null) _decorGhost = g.GetComponent<SpriteRenderer>(); }
                if (_landGhost == null) { var g = GameObject.Find("LandGhost"); if (g != null) _landGhost = g.GetComponent<SpriteRenderer>(); }
            }
        }

        // ------------------------------------------------------------------ build UI

        void EnsureUi()
        {
            if (_canvas != null) return;
            _canvas = UiKit.Canvas("BuildUxCanvas", 242, transform);
            _safe = UiKit.Rect(_canvas.transform, "Safe");
            _safe.gameObject.AddComponent<BuildSafeArea>();
            var s = _safe.transform;

            // Floating bar: [TICK cost] [TURN] [CANCEL], keycaps under it on PC.
            _bar = UiKit.Rect(s, "FloatBar");
            Size(_bar, 98f, 22f);
            var tick = Flat(_bar, "Tick", GreenDk, Green, true);
            Put(tick.rectTransform, 0f, 0f, 46f, 22f);
            _tickBorder = tick;
            _tickFill = tick.transform.GetChild(0).GetComponent<Image>();
            var tickBtn = tick.gameObject.AddComponent<Button>();
            tickBtn.targetGraphic = _tickFill;
            tickBtn.onClick.AddListener(OnTick);
            _tickIcon = Img(tick.transform, "Icon", BuildMode.Ux("Check"));
            Put(_tickIcon.rectTransform, 3f, -6f, 11f, 9f);
            _woodIcon = Img(tick.transform, "Wood", Resources.Load<Sprite>("World/HomeDistrict/Build/BD_Icon_Wood"));
            Put(_woodIcon.rectTransform, 15f, -7f, 8f, 8f);
            _woodText = Lbl(tick.transform, "WoodN", TextAnchor.UpperLeft);
            Put(_woodText.rectTransform, 23f, -6f, 10f, 10f);
            _stoneIcon = Img(tick.transform, "Stone", Resources.Load<Sprite>("World/HomeDistrict/Build/BD_Icon_Stone"));
            Put(_stoneIcon.rectTransform, 32f, -7f, 8f, 8f);
            _stoneText = Lbl(tick.transform, "StoneN", TextAnchor.UpperLeft);
            Put(_stoneText.rectTransform, 40f, -6f, 10f, 10f);
            _stockText = Lbl(tick.transform, "Stock", TextAnchor.UpperLeft);
            Put(_stockText.rectTransform, 16f, -6f, 30f, 10f);
            _turn = BuildMode.IconButton(_bar, "Turn", "Turn", Color.white, YardHand.Rotate);
            Put((RectTransform)_turn.transform, 50f, 0f, 22f, 22f);
            _cancel = BuildMode.IconButton(_bar, "Cancel", "Cross", CancelInk, YardHand.CancelHand);
            Put((RectTransform)_cancel.transform, 76f, 0f, 22f, 22f);
            _keys = UiKit.Rect(_bar, "Keys");
            Put(_keys, 0f, -24f, 98f, 12f);
            Key(_keys, "E", 23f);
            _turnKey = Key(_keys, "R", 61f);
            Key(_keys, "ESC", 87f);

            // Reason chip.
            _chip = UiKit.Rect(s, "Reason");
            var chipBox = Flat(_chip, "Box", Plum, Red, false);
            UiKit.Fill(chipBox);
            _chipText = Lbl(_chip, "Text", TextAnchor.MiddleCenter);
            _chipText.color = Red;
            UiKit.Fill(_chipText);

            // Selected piece: name tag, context menu, info card.
            _tag = UiKit.Rect(s, "NameTag");
            UiKit.Fill(Flat(_tag, "Box", Plum, Gold, false));
            _tagText = Lbl(_tag, "Text", TextAnchor.MiddleCenter);
            UiKit.Fill(_tagText);
            _menu = UiKit.Rect(s, "Menu");
            Size(_menu, 5 * 30f + 4 * 4f + 6f, 42f);
            UiKit.Fill(Flat(_menu, "Back", Plum, PlumBorder, true));
            MenuBtn(0, "Move", "DOI", Color.white, YardHand.BeginMove);
            MenuBtn(1, "Turn", "XOAY", Color.white, TurnSelected);
            MenuBtn(2, "Store", "CAT", Color.white, YardHand.Store);
            MenuBtn(3, "Info", "INFO", Cyan, () => _infoOpen = !_infoOpen);
            MenuBtn(4, "Cross", "XONG", CancelInk, YardHand.FinishSelect);
            _info = UiKit.Rect(s, "Info");
            Size(_info, 120f, 40f);
            UiKit.Fill(Flat(_info, "Box", Plum, PlumBorder, false));
            _infoText = Lbl(_info, "Text", TextAnchor.UpperLeft);
            UiKit.Fill(_infoText, 4f);

            // Recentre on the hero (pan exists; zoom waits on TODO(C7)).
            _aim = BuildMode.IconButton(s, "Recentre", "Aim", Color.white, Recentre);
        }

        void MenuBtn(int i, string icon, string label, Color ink, UnityEngine.Events.UnityAction act)
        {
            var b = BuildMode.IconButton(_menu, "M_" + icon, icon, ink, act);
            Put((RectTransform)b.transform, 3f + i * 34f, -3f, 30f, 22f);
            var t = Lbl(_menu, "L_" + icon, TextAnchor.UpperCenter);
            t.text = label;
            Put(t.rectTransform, 1f + i * 34f, -27f, 34f, 10f);
        }

        // ------------------------------------------------------------------ floating bar + reason chip

        void PaintBar()
        {
            bool placing = BuildMode.BuildPick >= 0 && BuildMode.GhostShown;
            bool carrying = YardHand.MovingSite >= 0 && BuildMode.GhostShown;
            bool decor = (HomeDecor.Pick >= 0 || YardHand.Moving >= 0) && _decorGhost != null && _decorGhost.enabled;
            bool brush = BuildMode.Tab == 3 && BuildMode.LandPick >= 0 && _landGhost != null && _landGhost.enabled;
            Bounds b = default;
            if (placing || carrying) b = BuildMode.GhostBounds;
            else if (decor) b = _decorGhost.bounds;
            else if (brush) b = _landGhost.bounds;
            // The house sits on its fixed foundation: nothing to drag, so its ✓/✕ and reason chip show at once.
            bool house = placing && BuildMode.Defs[BuildMode.BuildPick].id == "nha_go";
            bool barOn = (placing || carrying || decor) && (YardHand.Parked || house);
            string why = BuildMode.GhostWhy ?? "";

            _bar.gameObject.SetActive(barOn);
            if (barOn)
            {
                bool ok = why.Length == 0;
                _tickBorder.color = ok ? Green : Grey;
                _tickFill.color = ok ? GreenDk : GreyBtn;
                _tickIcon.color = ok ? Color.white : Grey;
                int wood = 0, ore = 0;
                string stock = "";
                if (placing) { var d = BuildMode.Defs[BuildMode.BuildPick]; wood = d.wood; ore = d.ore; }
                else if (YardHand.Moving < 0 && HomeDecor.Pick >= 0) stock = "x" + HomeDecor.Stock(HomeDecor.Defs[HomeDecor.Pick].id);
                bool cost = placing;
                int haveW = WorldSave.GetQty("wood"), haveO = WorldSave.GetQty("ore");
                _woodIcon.enabled = _woodText.enabled = cost;
                _woodText.text = wood.ToString();
                _woodText.color = haveW < wood ? Red : Color.white;
                _stoneIcon.enabled = _stoneText.enabled = cost && (ore > 0 || placing);
                _stoneText.text = ore.ToString();
                _stoneText.color = haveO < ore ? Red : Color.white;
                Put(_stoneIcon.rectTransform, 32f, -7f, 8f, 8f); Put(_stoneText.rectTransform, 40f, -6f, 10f, 10f);
                _stockText.enabled = stock.Length > 0;
                _stockText.text = stock;
                bool turn = decor || carrying || (placing && BuildMode.Defs[BuildMode.BuildPick].id != "nha_go");
                _turn.gameObject.SetActive(turn);
                _turnKey.SetActive(turn);
                // close the gap when there is no turn button
                Put((RectTransform)_cancel.transform, turn ? 76f : 50f, 0f, 22f, 22f);
                Put((RectTransform)_keys.Find("Key_ESC"), (turn ? 87f : 61f) - 15f, 0f, 30f, 12f);
                Size(_bar, turn ? 98f : 72f, 22f);
                _keys.gameObject.SetActive(!IsTouch);
                // above the ghost; flips under the foot near the top edge
                float shake = Time.unscaledTime < _shakeUntil ? Mathf.Sin(Time.unscaledTime * 60f) * 2f : 0f;
                Anchor(_bar, new Vector3(b.center.x, b.max.y, 0f), new Vector2(0f, 6f), new Vector3(b.center.x, b.min.y, 0f), new Vector2(0f, -(IsTouch ? 28f : 40f)), shake);
            }

            bool chipOn = (barOn || brush) && why.Length > 0;
            _chip.gameObject.SetActive(chipOn);
            if (chipOn)
            {
                _chipText.text = why;
                Size(_chip, why.Length * 8f + 8f, 14f);
                float px = UiKit.Px;
                if (barOn)
                {
                    // Sit on the floating bar (mockup 2/4 #3). Flip under it only if the top edge would clip.
                    var bar = _bar.anchoredPosition;
                    float gap = 3f * px;
                    float above = bar.y + (_bar.sizeDelta.y + _chip.sizeDelta.y) * 0.5f + gap;
                    float below = bar.y - (_bar.sizeDelta.y + _chip.sizeDelta.y) * 0.5f - gap;
                    float topLimit = _safe.rect.size.y * 0.5f - _chip.sizeDelta.y * 0.5f - 8f * px;
                    _chip.anchoredPosition = new Vector2(bar.x, above > topLimit ? below : above);
                    Clamp(_chip);
                }
                else
                {
                    // Land brush: sit just under the cell; flip above it near the hint / village gate.
                    var cell = brush && _landGhost != null
                        ? _landGhost.transform.position
                        : new Vector3(b.center.x, b.min.y, 0f);
                    if (!brush && (placing || carrying) && BuildMode.GhostFoot != Vector2.zero && _root != null)
                        cell = _root.TransformPoint(BuildMode.GhostLocal - new Vector2(0f, BuildMode.GhostFoot.y * 0.5f));
                    var at = Anchor(_chip, cell, new Vector2(0f, -8f), cell, new Vector2(0f, 8f), 0f);
                    float floor = -_safe.rect.size.y * 0.5f + (brush ? 160f : BottomRows) * px;
                    if (at.y - _chip.sizeDelta.y * 0.5f < floor)
                        Anchor(_chip, cell, new Vector2(0f, 8f), cell, new Vector2(0f, 8f), 0f);
                }
            }
        }

        /// <summary>
        /// Kit px at the bottom for the build mode label + hint plate (top at 59) plus a gap; a reason chip whose
        /// bottom would fall below this (or get clamped against it, over the ghost's foot) flips above the bar.
        /// </summary>
        const float BottomRows = 90f;

        void OnTick()
        {
            if ((BuildMode.GhostWhy ?? "").Length > 0 && !SpendConfirm.Pending)
            {
                _shakeUntil = Time.unscaledTime + 0.25f;
                Haptic();
                return;
            }
            // TODO(C1): with a pointer on this button HomeDecor.CursorRaw() reads the button's spot, not the ghost's.
            YardHand.Confirm();
        }

        // ------------------------------------------------------------------ selection menu (decor)

        void PaintMenu()
        {
            int sel = YardHand.Moving < 0 ? YardHand.Selected : -1;
            int site = YardHand.MovingSite < 0 ? YardHand.SelectedSite : -1;
            if (site >= 0 && BuildMode.GetSite(site).id == "nha_go") site = -1;
            var pieces = HomeLayout.Pieces;
            bool decor = sel >= 0 && sel < pieces.Count && _root != null;
            bool building = !decor && site >= 0 && site < BuildMode.SiteCount && _root != null;
            bool on = decor || building;
            int key = decor ? sel : building ? 1000 + site : -1;
            if (on && (key != _selIndex || Time.unscaledTime > _selAt))
            {
                _selAt = Time.unscaledTime + 0.25f;
                on = decor ? PieceBounds(sel, out _selBounds) : BuildMode.TrySiteBounds(site, out _selBounds);
            }
            if (key != _selIndex) _infoOpen = false;
            _selIndex = on ? key : -1;
            ShowMenuSlot("Store", decor);
            _menu.gameObject.SetActive(on);
            _tag.gameObject.SetActive(on);
            _info.gameObject.SetActive(on && _infoOpen);
            if (!on) return;
            string name;
            string info = "";
            if (decor)
            {
                var piece = pieces[sel];
                int d = HomeDecor.IndexOf(piece.def);
                name = d >= 0 ? HomeDecor.Defs[d].name : piece.def;
                float beauty = d >= 0 ? HomeDecor.Defs[d].beauty : 0f;
                info = name + "\n" + (beauty > 0f ? "DEP +" + beauty.ToString("0.##") : "TRANG TRI") + "\nTRONG KHO x" + HomeDecor.Stock(piece.def);
            }
            else
            {
                var view = BuildMode.GetSite(site);
                name = string.IsNullOrEmpty(view.name) ? view.id : view.name;
                info = name + "\n" + (view.done ? "XONG" : "DANG XAY " + Mathf.RoundToInt(view.progress * 100f) + "%");
            }
            _tagText.text = name;
            Size(_tag, name.Length * 8f + 10f, 14f);
            var b = _selBounds;
            Vector2 top = Anchor(_tag, new Vector3(b.center.x, b.max.y, 0f), new Vector2(0f, 4f), new Vector3(b.center.x, b.max.y, 0f), new Vector2(0f, 4f), 0f);
            Anchor(_menu, new Vector3(b.center.x, b.min.y, 0f), new Vector2(0f, -6f), new Vector3(b.center.x, b.max.y, 0f), new Vector2(0f, 22f), 0f);
            if (_infoOpen)
            {
                _infoText.text = info;
                _info.anchoredPosition = top + new Vector2(0f, 7f + 2f + 20f) * UiKit.Px;
                Clamp(_info);
            }
        }

        static void ShowMenuSlot(Transform menu, string icon, bool on)
        {
            var button = menu.Find("M_" + icon);
            var label = menu.Find("L_" + icon);
            if (button != null && button.gameObject.activeSelf != on) button.gameObject.SetActive(on);
            if (label != null && label.gameObject.activeSelf != on) label.gameObject.SetActive(on);
        }

        void ShowMenuSlot(string icon, bool on) => ShowMenuSlot(_menu, icon, on);

        void TurnSelected()
        {
            int site = YardHand.SelectedSite;
            if (site >= 0 && BuildMode.GetSite(site).id != "nha_go")
            {
                BuildMode.FlipSite(site);
                return;
            }
            int sel = YardHand.Selected;
            var pieces = HomeLayout.Pieces;
            if (sel < 0 || sel >= pieces.Count) return;
            HomeDecor.FlipIndex(sel);
        }

        bool PieceBounds(int index, out Bounds b)
        {
            b = default;
            bool any = false;
            foreach (var tag in _root.GetComponentsInChildren<HomePieceRef>())
            {
                if (tag.index != index) continue;
                foreach (var sr in tag.GetComponentsInChildren<SpriteRenderer>())
                {
                    if (sr.sprite == null || !sr.enabled) continue;
                    if (!any) { b = sr.bounds; any = true; } else b.Encapsulate(sr.bounds);
                }
            }
            return any;
        }

        // ------------------------------------------------------------------ recentre

        void PlaceAim()
        {
            var rt = (RectTransform)_aim.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
            float px = UiKit.Px;
            float y = UiKit.IsPortrait ? (BuildMode.SheetCollapsed ? 48f : 150f) : 6f;
            rt.anchoredPosition = new Vector2(-6f * px, y * px);
            rt.sizeDelta = new Vector2(22f, 22f) * px;
        }

        static void Recentre()
        {
            var cam = Camera.main;
            var follow = cam != null ? cam.GetComponent<CameraFollow>() : null;
            if (follow != null) follow.ClearPan();
        }

        // ------------------------------------------------------------------ world: grid + building foot

        void PaintGrid()
        {
            var cam = Camera.main;
            if (_root == null || cam == null) { ShowWorld(false); return; }
            if (_grid == null)
            {
                _grid = MakeTiled("BuildGrid", GridSprite(), GridOrder);
                _spot = MakeTiled("BuildGridSpot", GridSprite(), GridOrder + 1);
                _foot = new GameObject("BuildFoot").AddComponent<SpriteRenderer>();
                _foot.sprite = CellSprite();
                _foot.sortingOrder = 4999;
            }
            ParentTo(_grid.transform);
            ParentTo(_spot.transform);
            float h = cam.orthographicSize * 2f + 2f, w = h * cam.aspect + 2f;
            Vector2 c = _root.InverseTransformPoint(cam.transform.position);
            var origin = new Vector2(Mathf.Floor(c.x - w * 0.5f), Mathf.Floor(c.y - h * 0.5f));
            _grid.transform.localPosition = new Vector3(origin.x, origin.y, 0f);
            _grid.size = new Vector2(Mathf.Ceil(w) + 1f, Mathf.Ceil(h) + 1f);
            _grid.color = new Color(1f, 1f, 1f, 0.10f);
            _grid.enabled = true;
            // brighter cells around the thing in hand
            bool hand = BuildMode.GhostShown || (_decorGhost != null && _decorGhost.enabled) || (_landGhost != null && _landGhost.enabled);
            if (hand)
            {
                Vector3 at = BuildMode.GhostShown ? BuildMode.GhostBounds.center
                    : _decorGhost != null && _decorGhost.enabled ? _decorGhost.transform.position : _landGhost.transform.position;
                Vector2 l = _root.InverseTransformPoint(at);
                _spot.transform.localPosition = new Vector3(Mathf.Floor(l.x) - 4f, Mathf.Floor(l.y) - 4f, 0f);
                _spot.size = new Vector2(9f, 9f);
                _spot.color = new Color(1f, 1f, 1f, 0.16f);
            }
            _spot.enabled = hand;
        }

        void PaintFoot()
        {
            if (_foot == null) return;
            bool on = BuildMode.GhostShown && BuildMode.GhostFoot != Vector2.zero && _root != null;
            _foot.enabled = on;
            if (!on) return;
            // TODO(C2): per-cell colours need HomeDecor.CellWhy / BuildMode.FootOf; one colour for the whole foot until then.
            bool ok = (BuildMode.GhostWhy ?? "").Length == 0;
            _foot.transform.position = _root.TransformPoint(BuildMode.GhostLocal);
            _foot.transform.localScale = new Vector3(BuildMode.GhostFoot.x, BuildMode.GhostFoot.y, 1f);
            _foot.color = ok ? new Color(0.49f, 0.84f, 0.49f, 0.55f) : new Color(0.88f, 0.28f, 0.28f, 0.55f);
        }

        void ParentTo(Transform t)
        {
            if (t.parent != _root) t.SetParent(_root, false);
        }

        void ShowWorld(bool on)
        {
            if (_grid != null) _grid.enabled = on;
            if (_spot != null) _spot.enabled = on;
            if (_foot != null) _foot.enabled = on;
        }

        static SpriteRenderer MakeTiled(string name, Sprite sprite, int order)
        {
            var sr = new GameObject(name).AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.tileMode = SpriteTileMode.Continuous;
            sr.sortingOrder = order;
            return sr;
        }

        static Sprite _gridSprite, _cellSprite;

        /// <summary>One 1-unit cell: 1 px line on the left and bottom edge (PPU 16).</summary>
        static Sprite GridSprite()
        {
            if (_gridSprite != null) return _gridSprite;
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Repeat };
            var px = new Color32[256];
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                    px[y * 16 + x] = x == 0 || y == 0 ? new Color32(255, 255, 255, 255) : new Color32(0, 0, 0, 0);
            tex.SetPixels32(px);
            tex.Apply();
            _gridSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), Vector2.zero, 16f, 0, SpriteMeshType.FullRect);
            return _gridSprite;
        }

        /// <summary>Filled cell with a 1 px brighter border, centred, 1 unit.</summary>
        static Sprite CellSprite()
        {
            if (_cellSprite != null) return _cellSprite;
            var tex = new Texture2D(16, 16, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color32[256];
            for (int y = 0; y < 16; y++)
                for (int x = 0; x < 16; x++)
                    px[y * 16 + x] = x == 0 || y == 0 || x == 15 || y == 15 ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 150);
            tex.SetPixels32(px);
            tex.Apply();
            _cellSprite = Sprite.Create(tex, new Rect(0, 0, 16, 16), new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect);
            return _cellSprite;
        }

        // ------------------------------------------------------------------ feedback

        void OnSite(Vector3 world, int wood, int ore, bool added)
        {
            SpawnPuffs(world, 1.2f);
            SpawnFloat(world + Vector3.up * 1.2f, wood, ore, added);
            GameAudio.Play(Sfx.UiReady);
            Haptic();
        }

        /// <summary>A decor piece appeared (placed) or went (stored / removed): dust and a sound.</summary>
        void WatchPieces()
        {
            var pieces = HomeLayout.Pieces;
            if (_seenValid && _root != null)
            {
                if (pieces.Count > _seenDefs.Count)
                {
                    var p = pieces[pieces.Count - 1];
                    SpawnPuffs(_root.TransformPoint(p.pos), 0.6f);
                    GameAudio.Play(Sfx.UiReady);
                    Haptic();
                }
                else if (pieces.Count < _seenDefs.Count)
                {
                    int gone = _seenDefs.Count - 1;
                    for (int i = 0; i < pieces.Count; i++)
                        if (pieces[i].def != _seenDefs[i] || pieces[i].pos != _seenPos[i]) { gone = i; break; }
                    SpawnPuffs(_root.TransformPoint(_seenPos[gone]), 0.6f);
                    Haptic();
                }
            }
            _seenDefs.Clear();
            _seenPos.Clear();
            for (int i = 0; i < pieces.Count; i++) { _seenDefs.Add(pieces[i].def); _seenPos.Add(pieces[i].pos); }
            _seenValid = true;
        }

        void SpawnPuffs(Vector3 at, float spread)
        {
            if (_puff == null)
            {
                _puff = Resources.LoadAll<Sprite>(PuffPath);
                System.Array.Sort(_puff, (a, b) => string.CompareOrdinal(a.name, b.name));
            }
            if (_puff.Length == 0) return;
            for (int k = -1; k <= 1; k++)
            {
                var go = new GameObject("BuildPuff");
                go.transform.position = new Vector3(at.x + k * spread * 0.5f, at.y + (k == 0 ? 0.15f : 0f), 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = _puff[0];
                sr.sortingOrder = 6000;
                _puffs.Add(new Puff { sr = sr, t0 = Time.unscaledTime + (k + 1) * 0.04f });
            }
        }

        void UpdatePuffs()
        {
            for (int i = _puffs.Count - 1; i >= 0; i--)
            {
                var p = _puffs[i];
                float t = Time.unscaledTime - p.t0;
                int frame = Mathf.Max(0, Mathf.FloorToInt(t * 12f));
                if (p.sr == null || frame >= _puff.Length)
                {
                    if (p.sr != null) Destroy(p.sr.gameObject);
                    _puffs.RemoveAt(i);
                    continue;
                }
                p.sr.enabled = t >= 0f;
                p.sr.sprite = _puff[frame];
            }
        }

        /// <summary>"-3 [wood] -2 [ore]" (placed, pale red) or "+3 [wood]" (removed, green) rising 12 kit px over 1 s.</summary>
        void SpawnFloat(Vector3 world, int wood, int ore, bool spent)
        {
            if (wood <= 0 && ore <= 0) return;
            EnsureUi();
            var row = UiKit.Rect(_safe, "BuildGain");
            var group = row.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            var color = spent ? new Color32(0xF0, 0x90, 0x88, 0xFF) : (Color32)Green;
            float x = 0f;
            var parts = new List<KeyValuePair<string, int>>();
            if (wood > 0) parts.Add(new KeyValuePair<string, int>("wood", wood));
            if (ore > 0) parts.Add(new KeyValuePair<string, int>("ore", ore));
            float total = 0f;
            foreach (var p in parts) total += (p.Value.ToString().Length + 1) * 8f + 14f;
            x = -total / 2f;
            foreach (var p in parts)
            {
                string text = (spent ? "-" : "+") + p.Value;
                float aw = text.Length * 8f;
                var label = UiKit.Label(row, "N_" + p.Key, text, 8, color, TextAnchor.MiddleRight);
                UiKit.Place(label, UiKit.Center, UiKit.Center, new Vector2(x + aw / 2f, 0f), new Vector2(aw + 2f, 10f));
                var icon = UiKit.Icon(row, UiKit.ItemIcon(p.Key), UiKit.Center, new Vector2(x + aw + 6f, 0f), 12f);
                icon.enabled = icon.sprite != null;
                x += aw + 14f;
            }
            var f = new Floater { rt = row, group = group, world = world, t0 = Time.unscaledTime };
            _floats.Add(f);
            PlaceFloat(f, 0f);
        }

        void UpdateFloats()
        {
            for (int i = _floats.Count - 1; i >= 0; i--)
            {
                var f = _floats[i];
                float t = (Time.unscaledTime - f.t0) / 1f;
                if (f.rt == null || t >= 1f)
                {
                    if (f.rt != null) Destroy(f.rt.gameObject);
                    _floats.RemoveAt(i);
                    continue;
                }
                PlaceFloat(f, t);
            }
        }

        void PlaceFloat(Floater f, float t)
        {
            if (!ToSafe(f.world, out var local)) { f.group.alpha = 0f; return; }
            f.rt.anchoredPosition = local + new Vector2(0f, Mathf.Round(12f * t) * UiKit.Px);
            f.group.alpha = t < 0.4f ? 1f : 1f - (t - 0.4f) / 0.6f;
        }

        static void Haptic()
        {
#if UNITY_ANDROID || UNITY_IOS
            Handheld.Vibrate();
#endif
        }

        // ------------------------------------------------------------------ layout helpers (kit px)

        bool ToSafe(Vector3 world, out Vector2 local)
        {
            local = Vector2.zero;
            var cam = Camera.main;
            if (cam == null || _safe == null) return false;
            var sp = cam.WorldToScreenPoint(world);
            if (sp.z < 0f) return false;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(_safe, sp, null, out local);
        }

        /// <summary>Places a centre-pivot rect at a world point plus a kit-px offset (above: bottom edge; below: top edge).
        /// Falls back to the second point when the first leaves the safe frame, then clamps clear of the build panel.</summary>
        Vector2 Anchor(RectTransform rt, Vector3 world, Vector2 offset, Vector3 alt, Vector2 altOffset, float shakeX)
        {
            float px = UiKit.Px;
            var size = rt.sizeDelta;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            ToSafe(world, out var p);
            var pos = p + offset * px + new Vector2(0f, Mathf.Sign(offset.y) * size.y * 0.5f);
            var half = _safe.rect.size * 0.5f;
            if (pos.y + size.y * 0.5f > half.y)
            {
                ToSafe(alt, out var q);
                pos = q + altOffset * px + new Vector2(0f, Mathf.Sign(altOffset.y) * size.y * 0.5f);
            }
            rt.anchoredPosition = pos;
            Clamp(rt);
            rt.anchoredPosition += new Vector2(shakeX * px, 0f);
            return rt.anchoredPosition;
        }

        /// <summary>Keeps a centre-pivot rect inside the safe frame and off the sidebar (16:9) or sheet (9:16).</summary>
        void Clamp(RectTransform rt)
        {
            var half = _safe.rect.size * 0.5f;
            var s = rt.sizeDelta * 0.5f;
            float minX = -half.x + s.x, maxX = half.x - s.x, minY = -half.y + s.y, maxY = half.y - s.y;
            var panel = BuildMode.Driver != null ? BuildMode.Driver.Panel : null;
            if (panel != null && panel.gameObject.activeInHierarchy)
            {
                var corners = new Vector3[4];
                panel.GetWorldCorners(corners);
                RectTransformUtility.ScreenPointToLocalPointInRectangle(_safe, corners[2], null, out var topRight);
                if (UiKit.IsPortrait) minY = Mathf.Max(minY, topRight.y + s.y + 16f * UiKit.Px);
                else minX = Mathf.Max(minX, topRight.x + s.x + 4f * UiKit.Px);
            }
            var p = rt.anchoredPosition;
            rt.anchoredPosition = new Vector2(Mathf.Clamp(p.x, minX, Mathf.Max(minX, maxX)), Mathf.Clamp(p.y, minY, Mathf.Max(minY, maxY)));
        }

        static void Size(RectTransform rt, float w, float h) => rt.sizeDelta = new Vector2(w, h) * UiKit.Px;

        /// <summary>Top-left anchored child rect, kit px.</summary>
        static void Put(RectTransform rt, float x, float y, float w, float h)
        {
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(x, y) * UiKit.Px;
            rt.sizeDelta = new Vector2(w, h) * UiKit.Px;
        }

        static Image Flat(Transform parent, string name, Color fill, Color border, bool raycast)
        {
            var b = UiKit.Rect(parent, name).gameObject.AddComponent<Image>();
            b.color = border;
            b.raycastTarget = raycast;
            var f = UiKit.Rect(b.transform, "Fill").gameObject.AddComponent<Image>();
            f.color = fill;
            f.raycastTarget = false;
            UiKit.Fill(f, 1f);
            return b;
        }

        static Image Img(Transform parent, string name, Sprite sprite)
        {
            var im = UiKit.Rect(parent, name).gameObject.AddComponent<Image>();
            im.sprite = sprite;
            im.raycastTarget = false;
            im.preserveAspect = true;
            return im;
        }

        static Text Lbl(Transform parent, string name, TextAnchor align)
        {
            var t = UiKit.Label(parent, name, "", 8, Color.white, align);
            t.raycastTarget = false;
            return t;
        }

        static GameObject Key(RectTransform parent, string key, float cx)
        {
            float w = key.Length * 8f + 6f;
            var box = Flat(parent, "Key_" + key, KeyFill, KeyBorder, false);
            Put(box.rectTransform, cx - w / 2f, 0f, w, 12f);
            var t = Lbl(box.transform, "Text", TextAnchor.MiddleCenter);
            t.text = key;
            t.color = Gold;
            UiKit.Fill(t);
            return box.gameObject;
        }
    }

    /// <summary>Fits a full-screen RectTransform to Screen.safeArea (notch, rounded corners, home bar).</summary>
    public class BuildSafeArea : MonoBehaviour
    {
        Rect _seen;
        Vector2Int _screen;

        void OnEnable() => Fit(true);
        void LateUpdate() => Fit(false);

        void Fit(bool force)
        {
            var area = Screen.safeArea;
            var screen = new Vector2Int(Screen.width, Screen.height);
            if (!force && area == _seen && screen == _screen) return;
            _seen = area;
            _screen = screen;
            var rt = (RectTransform)transform;
            if (screen.x <= 0 || screen.y <= 0) return;
            rt.anchorMin = new Vector2(area.xMin / screen.x, area.yMin / screen.y);
            rt.anchorMax = new Vector2(area.xMax / screen.x, area.yMax / screen.y);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            rt.pivot = new Vector2(0.5f, 0.5f);
        }
    }
}
