using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>
    /// One gesture language for the yard on portrait, landscape and mouse.
    /// An empty hand drags the camera. A held tool follows the pointer and places only on tick.
    /// A ground brush commits one stroke on release. Remove stores only when the finger lifts on the same piece.
    /// </summary>
    public static class YardHand
    {
        public static int Moving { get; private set; } = -1;
        public static int MovingSite { get; private set; } = -1;
        public static int Selected { get; private set; } = -1;
        public static int SelectedSite { get; private set; } = -1;
        public static int RemoveAim { get; private set; } = -1;
        public static int RemoveAimSite { get; private set; } = -1;
        public static string Status { get; private set; } = "";
        public static bool Blocked { get; private set; }
        public static bool CanUndo => _hist.Count > 0 && _mode != Stroke;
        public static int UndoDepth => _hist.Count;
        public static bool Stroking => _mode == Stroke;
        public static int StrokeCells => _stroke.Count;
        public static int StrokeOre =>
            AreaPending || _areaDrag ? PendingPlan.ore :
            _mode == Stroke && BuildMode.LandPick == (int)HomeLand.Brush.Dig ? _stroke.Count * HomeLand.DigOre : 0;
        public static IReadOnlyList<Vector2Int> StrokeList => _stroke;
        public static bool AreaPending { get; private set; }
        public static bool AreaLive => AreaPending || _areaDrag;
        public static RectInt PendingArea { get; private set; }
        public static HomeLand.AreaPlan PendingPlan { get; private set; }
        public static bool DigGo => _digGo;

        const int Idle = 0, Watch = 1, Panning = 2, Stroke = 3, Aim = 4;
        const float TapSlop = 12f;

        static int _mode;
        static Vector2 _start, _last;
        static int _aimPiece = -1, _aimSite = -1;
        static string _pendLand, _pendWater, _block = "";
        static bool _changed, _dragGhost, _areaDrag, _awaitDig, _digGo;
        static int _digSpent;
        static Vector2Int _areaStart, _areaEnd;
        static Vector2 _downLocal;
        static float _pinch;
        static readonly List<Vector2Int> _stroke = new List<Vector2Int>();
        static readonly List<Step> _hist = new List<Step>();

        struct Step
        {
            public bool land;
            public string a, b;
            public int[] stock;
            public int ore;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _bar = null;
            _hist.Clear();
            _stroke.Clear();
            Moving = MovingSite = Selected = SelectedSite = -1;
            RemoveAim = RemoveAimSite = -1;
            _mode = Idle;
            BuildAim.Clear();
        }
        static Vector2Int _cell = new Vector2Int(int.MinValue, int.MinValue);
        static GameObject _bar;
        static GameObject _tick, _cancel, _turn, _undo, _move, _store, _done;

        static bool _wasHeld, _wasTwo;
        /// <summary>Ghost is down and waiting. The tick / turn / close bar stays hidden while a finger is dragging it.</summary>
        public static bool Parked { get; private set; }

        /// <summary>Show the floating bar after a sidebar pick (no drag yet).</summary>
        public static void ParkGhost() => Parked = true;

        public static void Tick()
        {
            if (!BuildMode.IsOpen) { Reset(); return; }
            var cam = Camera.main;
            var follow = cam != null ? cam.GetComponent<CameraFollow>() : null;
            if (follow != null) follow.LooseFollow = true;
            EnsureBar();
            ReadPointer(out bool began, out bool held, out bool over, out Vector2 screen);
            bool ended = _wasHeld && !held;
            _wasHeld = held;
            bool two = TwoFingers(out Vector2 centroid);
            if (two)
            {
                if (_areaDrag) CancelArea();
                if (!_wasTwo) { _last = centroid; _pinch = PinchSpan(); }
                if (_mode == Stroke && !_awaitDig) CloseStroke();
                Pan(centroid - _last);
                float span = PinchSpan();
                if (_wasTwo && _pinch > 8f && span > 8f && follow != null && !over)
                    follow.ZoomAt(span / _pinch, centroid);
                _pinch = span;
                _last = centroid;
                _mode = Panning;
                _wasTwo = true;
                Refresh();
                return;
            }
            if (_wasTwo) { _mode = Idle; _wasTwo = false; }

            var mouse = Mouse.current;
            if (mouse != null && mouse.rightButton.isPressed)
                Pan(mouse.delta.ReadValue());
            if (mouse != null && !over && follow != null)
            {
                float scroll = mouse.scroll.ReadValue().y;
                if (Mathf.Abs(scroll) > 0.01f)
                    follow.ZoomAt(scroll > 0f ? 1.08f : 1f / 1.08f, screen);
            }
            PanKeys();

            if (began && !over)
            {
                _start = _last = screen;
                _downLocal = BuildAim.FromScreen(screen);
                if (BuildMode.Tab == 2)
                {
                    PickDown(_downLocal, out _aimSite, out _aimPiece);
                    _mode = Aim;
                }
                else if (BuildMode.Tab == 3 && BuildMode.LandPick >= 0)
                {
                    int lamp = HomeDecor.SpriteAt(_downLocal);
                    if (lamp >= 0)
                    {
                        Selected = lamp;
                        SelectedSite = -1;
                        _mode = Watch;
                    }
                    else if (BuildMode.LandSizeOf(BuildMode.LandPick) == BuildMode.LandSize.Rect) BeginArea();
                    else if (_awaitDig) _mode = Stroke;
                    else BeginStroke();
                }
                else if (!Armed())
                    _mode = Watch;
            }
            else if (held && _mode == Watch && (screen - _start).sqrMagnitude > TapSlop * TapSlop)
            {
                PickDown(_downLocal, out int site, out int piece);
                var view = site >= 0 ? BuildMode.GetSite(site) : default;
                if (SelectedSite >= 0 && site == SelectedSite && view.id != "nha_go")
                {
                    MovingSite = SelectedSite;
                    BuildMode.Flip = view.flip;
                    BuildMode.Quarter = view.turn;
                    BuildAim.Set(view.local);
                    Parked = false;
                    _dragGhost = true;
                    _mode = Idle;
                }
                else if (Selected >= 0 && piece == Selected)
                {
                    Moving = Selected;
                    if ((uint)Selected < (uint)HomeLayout.Pieces.Count)
                    {
                        BuildAim.Set(HomeLayout.Pieces[Selected].pos);
                        BuildMode.Flip = HomeLayout.Pieces[Selected].flip;
                        BuildMode.Quarter = HomeLayout.Pieces[Selected].turn;
                    }
                    Parked = false;
                    _dragGhost = true;
                    _mode = Idle;
                }
                else _mode = Panning;
            }

            if (held && _mode == Panning)
                Pan(screen - _last);
            if (held && _areaDrag) DragArea();
            if (held && _mode == Stroke) PaintStroke();
            if (ended)
            {
                if (_areaDrag)
                {
                    _areaDrag = false;
                    AreaPending = true;
                    RefreshPlan();
                }
                if (_mode == Aim)
                    MarkRemove();
                else if (_mode == Watch)
                {
                    PickDown(_downLocal, out int site, out int piece);
                    if (site >= 0) { SelectedSite = site; Selected = -1; }
                    else { Selected = piece; SelectedSite = -1; }
                    Moving = MovingSite = -1;
                }
                else if (_mode == Stroke && !_awaitDig)
                    CloseStroke();
                if (_dragGhost && _mode != Panning) Parked = true;
                _dragGhost = false;
                _mode = Idle;
            }
            _last = two ? centroid : screen;
            TrackAim(screen, began, held, over);
            Refresh();
        }

        public static bool DigWaiting => _awaitDig;

        public static void CancelDig()
        {
            _awaitDig = false;
            _digGo = false;
            SpendConfirm.Clear();
        }

        static bool PointerHeld()
        {
            var mouse = Mouse.current;
            if (mouse != null && mouse.leftButton.isPressed) return true;
            var pad = Touchscreen.current;
            if (pad == null) return false;
            for (int i = 0; i < pad.touches.Count; i++)
                if (pad.touches[i].press.isPressed) return true;
            return false;
        }

        public static void Confirm()
        {
            if (AreaPending) { ConfirmArea(); return; }
            if (_awaitDig)
            {
                SpendConfirm.Accept();
                if (_mode != Stroke)
                {
                    _mode = Stroke;
                    _changed = false;
                    _cell = new Vector2Int(int.MinValue, int.MinValue);
                }
                PaintStroke();
                if (!PointerHeld()) EndStroke();
                return;
            }
            if (SpendConfirm.Pending) { SpendConfirm.Accept(); return; }
            if (MovingSite >= 0)
            {
                if (BuildMode.MoveSite(MovingSite, HomeDecor.Cursor()))
                {
                    SelectedSite = MovingSite;
                    MovingSite = -1;
                    BuildAim.Clear();
                }
                return;
            }
            if (Moving >= 0)
            {
                if (HomeDecor.TryMove(Moving, HomeDecor.Cursor(), BuildMode.Flip, BuildMode.Quarter))
                {
                    Selected = Moving;
                    Moving = -1;
                    BuildAim.Clear();
                }
                return;
            }
            BuildMode.TryPlace(true);
        }

        public static void ConfirmRemove()
        {
            if (RemoveAimSite >= 0) BuildMode.RemoveSite(RemoveAimSite, false);
            else if (RemoveAim >= 0) HomeDecor.RemoveIndex(RemoveAim);
            RemoveAim = RemoveAimSite = -1;
            Selected = SelectedSite = -1;
        }

        public static void CancelHand()
        {
            Moving = MovingSite = -1;
            Selected = SelectedSite = -1;
            RemoveAim = RemoveAimSite = -1;
            BuildMode.DropTool();
            BuildAim.Clear();
            Parked = false;
            _dragGhost = false;
            _block = "";
        }

        public static void Rotate()
        {
            if (Moving >= 0 || MovingSite >= 0 || HomeDecor.Pick >= 0 || BuildMode.BuildPick >= 0)
            {
                BuildMode.Flip = !BuildMode.Flip;
                BuildMode.Quarter = 0;
            }
            else if (Selected >= 0)
                HomeDecor.FlipIndex(Selected);
            else if (SelectedSite >= 0)
                BuildMode.FlipSite(SelectedSite);
            else
            {
                BuildMode.Flip = !BuildMode.Flip;
                BuildMode.Quarter = 0;
            }
        }

        public static void PushDecor()
        {
            var stock = new int[HomeDecor.Defs.Length];
            for (int i = 0; i < stock.Length; i++) stock[i] = HomeDecor.Stock(HomeDecor.Defs[i].id);
            Push(new Step { land = false, a = HomeLayout.Capture(), stock = stock });
        }

        public static void Undo()
        {
            if (!CanUndo) return;
            int n = _hist.Count - 1;
            var step = _hist[n];
            _hist.RemoveAt(n);
            if (step.land) HomeLand.WriteSave(step.a, step.b, step.ore);
            else
            {
                HomeLayout.Restore(step.a);
                if (step.stock != null)
                    for (int i = 0; i < step.stock.Length && i < HomeDecor.Defs.Length; i++)
                        HomeDecor.SetStock(HomeDecor.Defs[i].id, step.stock[i]);
                PlayerPrefs.Save();
            }
        }

        public static void BeginMove()
        {
            if (SelectedSite >= 0 && BuildMode.GetSite(SelectedSite).id != "nha_go")
            {
                var view = BuildMode.GetSite(SelectedSite);
                MovingSite = SelectedSite;
                BuildMode.Flip = view.flip;
                BuildMode.Quarter = view.turn;
                BuildAim.Set(view.local);
                Parked = true;
            }
            else if (Selected >= 0)
            {
                Moving = Selected;
                if ((uint)Selected < (uint)HomeLayout.Pieces.Count)
                {
                    BuildAim.Set(HomeLayout.Pieces[Selected].pos);
                    BuildMode.Flip = HomeLayout.Pieces[Selected].flip;
                    BuildMode.Quarter = HomeLayout.Pieces[Selected].turn;
                }
            }
        }

        public static void Store()
        {
            if (Selected < 0) return;
            if (HomeDecor.RemoveIndex(Selected))
                Selected = Moving = -1;
        }

        public static void FinishSelect()
        {
            Selected = SelectedSite = -1;
            Moving = MovingSite = -1;
        }

        public static void Reset()
        {
            _mode = Idle;
            Moving = MovingSite = Selected = SelectedSite = -1;
            RemoveAim = RemoveAimSite = -1;
            _aimPiece = _aimSite = -1;
            _block = "";
            _hist.Clear();
            _stroke.Clear();
            _areaDrag = AreaPending = _awaitDig = _digGo = false;
            _digSpent = 0;
            SpendConfirm.Clear();
            _wasHeld = _wasTwo = false;
            _dragGhost = false;
            Parked = false;
            Blocked = false;
            Status = "";
            BuildAim.Clear();
            var cam = Camera.main;
            var follow = cam != null ? cam.GetComponent<CameraFollow>() : null;
            if (follow != null)
            {
                follow.LooseFollow = false;
                follow.ClearPan();
            }
            if (_bar != null) _bar.SetActive(false);
        }

        static bool Armed() =>
            BuildMode.BuildPick >= 0 || HomeDecor.Pick >= 0 || Moving >= 0 || MovingSite >= 0 ||
            (BuildMode.Tab == 3 && BuildMode.LandPick >= 0);

        static void BeginStroke()
        {
            HomeLand.ReadSave(out _pendLand, out _pendWater);
            _changed = false;
            _block = "";
            _cell = new Vector2Int(int.MinValue, int.MinValue);
            _stroke.Clear();
            _mode = Stroke;
            _digSpent = 0;
            _digGo = _awaitDig = false;
        }

        static void CloseStroke()
        {
            if (_awaitDig) { SpendConfirm.Clear(); _awaitDig = false; _digGo = false; _mode = Idle; return; }
            EndStroke();
        }

        static void BeginArea()
        {
            _areaStart = HomeLand.CellOf(HomeDecor.CursorRaw());
            _areaEnd = _areaStart;
            _areaDrag = true;
            AreaPending = false;
            RefreshPlan();
        }

        static void DragArea()
        {
            _areaEnd = HomeLand.CellOf(HomeDecor.CursorRaw());
            RefreshPlan();
        }

        static void RefreshPlan()
        {
            PendingArea = ClampArea(_areaStart, _areaEnd);
            if (BuildMode.LandPick < 0) return;
            PendingPlan = HomeLand.PlanArea((HomeLand.Brush)BuildMode.LandPick, PendingArea);
        }

        static RectInt ClampArea(Vector2Int a, Vector2Int b)
        {
            int x0 = Mathf.Min(a.x, b.x), y0 = Mathf.Min(a.y, b.y);
            int x1 = Mathf.Max(a.x, b.x), y1 = Mathf.Max(a.y, b.y);
            if (x1 - x0 > 7) x1 = x0 + 7;
            if (y1 - y0 > 7) y1 = y0 + 7;
            return new RectInt(x0, y0, x1 - x0 + 1, y1 - y0 + 1);
        }

        public static void ConfirmArea()
        {
            if (!AreaPending || BuildMode.LandPick < 0) { CancelArea(); return; }
            var brush = (HomeLand.Brush)BuildMode.LandPick;
            HomeLand.ReadSave(out string a, out string b);
            var cells = PendingPlan.ok;
            if (HomeLand.PaintArea(brush, cells, out int ore))
            {
                Push(new Step { land = true, a = a, b = b, ore = ore });
                BuildMode.MarkLand();
                BuildMode.FlushLandNow();
            }
            else if (brush == HomeLand.Brush.Dig) _block = "THIEU DA";
            CancelArea();
        }

        public static void CancelArea()
        {
            _areaDrag = false;
            AreaPending = false;
            PendingArea = default;
            PendingPlan = default;
        }

        static void PaintStroke()
        {
            if (_awaitDig || BuildMode.LandPick < 0) return;
            var brush = (HomeLand.Brush)BuildMode.LandPick;
            var stamp = Stamp();
            if (stamp.Count == 0 || stamp[0] == _cell) return;
            _cell = stamp[0];
            for (int i = 0; i < stamp.Count; i++)
            {
                var cell = stamp[i];
                string why = HomeLand.Check(brush, cell);
                if (why == "THIEU DA") { _block = why; EndStroke(); return; }
                if (why.Length > 0)
                {
                    if (_block.Length == 0) _block = why;
                    continue;
                }
                if (!HomeLand.Paint(brush, cell)) continue;
                if (brush == HomeLand.Brush.Grass || brush == HomeLand.Brush.Path) HomeFarm.DropBed(cell);
                BuildMode.MarkLand();
                BuildMode.FlushLandSoon();
                _changed = true;
                if (!_stroke.Contains(cell)) _stroke.Add(cell);
            }
        }

        static List<Vector2Int> Stamp()
        {
            var list = new List<Vector2Int>(4);
            if (BuildMode.LandSizeOf(BuildMode.LandPick) == BuildMode.LandSize.Two)
            {
                var raw = HomeDecor.CursorRaw();
                int x = Mathf.FloorToInt(raw.x - 0.5f);
                int y = Mathf.FloorToInt(raw.y - 0.5f);
                list.Add(new Vector2Int(x, y));
                list.Add(new Vector2Int(x + 1, y));
                list.Add(new Vector2Int(x, y + 1));
                list.Add(new Vector2Int(x + 1, y + 1));
            }
            else list.Add(HomeLand.CellOf(HomeDecor.CursorRaw()));
            return list;
        }

        static void EndStroke()
        {
            BuildMode.FlushLandNow();
            if (_changed) Push(new Step { land = true, a = _pendLand, b = _pendWater, ore = 0 });
            _digGo = false;
            _mode = Idle;
        }

        /// <summary>A drawn decor sprite wins over the house box behind it. Otherwise the building, then the foot.</summary>
        static void PickDown(Vector2 local, out int site, out int piece)
        {
            piece = HomeDecor.SpriteAt(local);
            if (piece >= 0) { site = -1; return; }
            site = BuildMode.FindSite(local);
            piece = site >= 0 ? -1 : HomeDecor.PieceAt(local);
        }

        static void MarkRemove()
        {
            PickDown(_downLocal, out int site, out int piece);
            if (_aimSite >= 0 && site == _aimSite) { RemoveAimSite = site; RemoveAim = -1; }
            else if (_aimPiece >= 0 && piece == _aimPiece) { RemoveAim = piece; RemoveAimSite = -1; }
            else RemoveAim = RemoveAimSite = -1;
            _aimPiece = _aimSite = -1;
        }

        static void Push(Step step)
        {
            _hist.Add(step);
            while (_hist.Count > 10) _hist.RemoveAt(0);
        }

        static void PanKeys()
        {
            var kb = Keyboard.current;
            if (kb == null) return;
            Vector2 d = Vector2.zero;
            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) d.x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) d.x += 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed) d.y += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed) d.y -= 1f;
            if (d.sqrMagnitude < 0.01f) return;
            var cam = Camera.main;
            var follow = cam != null ? cam.GetComponent<CameraFollow>() : null;
            if (follow != null) follow.AddPan(d.normalized * 6f * Time.unscaledDeltaTime);
        }

        static void TrackAim(Vector2 screen, bool began, bool held, bool over)
        {
            bool holding = HomeDecor.Pick >= 0 || BuildMode.BuildPick >= 0 || Moving >= 0 || MovingSite >= 0;
            if (!holding)
            {
                BuildAim.Clear();
                _dragGhost = false;
                Parked = false;
                return;
            }
            if (!BuildAim.Active)
                BuildAim.Set(HeroFront());
            if (over || _mode == Panning) return;
            if (began)
            {
                Parked = false;
                _dragGhost = true;
            }
            if (!_dragGhost || !held) return;
            Vector2 local = BuildAim.FromScreen(screen);
            bool touch = MobileTouch.TryFinger(out _, out _, out _, out _);
            BuildAim.Set(touch ? local + new Vector2(0f, 1.5f) : local);
        }

        static Vector2 HeroFront()
        {
            var hero = GameObject.FindGameObjectWithTag("Player");
            var root = GameObject.Find("HomeDistrict");
            Vector2 world = hero != null ? (Vector2)hero.transform.position + Vector2.down * 1.5f : Vector2.zero;
            return root != null ? (Vector2)root.transform.InverseTransformPoint(world) : world;
        }

        static float PinchSpan()
        {
            var pad = Touchscreen.current;
            if (pad == null) return 0f;
            Vector2 a = Vector2.zero, b = Vector2.zero;
            int n = 0;
            for (int i = 0; i < pad.touches.Count; i++)
            {
                var t = pad.touches[i];
                if (!t.press.isPressed) continue;
                if (n == 0) a = t.position.ReadValue();
                else b = t.position.ReadValue();
                n++;
            }
            return n >= 2 ? Vector2.Distance(a, b) : 0f;
        }

        static void Pan(Vector2 screenDelta)
        {
            if (screenDelta.sqrMagnitude < 0.01f) return;
            var cam = Camera.main;
            if (cam == null) return;
            var follow = cam.GetComponent<CameraFollow>();
            if (follow == null) return;
            float scale = cam.orthographicSize * 2f / Mathf.Max(1f, Screen.height);
            follow.AddPan(new Vector2(-screenDelta.x, -screenDelta.y) * scale);
        }

        static void ReadPointer(out bool began, out bool held, out bool over, out Vector2 screen)
        {
            began = held = over = false;
            screen = _last;
            if (MobileTouch.TryFinger(out screen, out began, out held, out int id))
            {
                over = MobileTouch.OverUi(id);
                return;
            }
            var mouse = Mouse.current;
            if (mouse == null) return;
            screen = mouse.position.ReadValue();
            began = mouse.leftButton.wasPressedThisFrame;
            held = mouse.leftButton.isPressed;
            over = HomeDecor.PointerOverUi;
        }

        static bool TwoFingers(out Vector2 centroid)
        {
            centroid = _last;
            var pad = Touchscreen.current;
            if (pad == null) return false;
            int n = 0;
            Vector2 sum = Vector2.zero;
            for (int i = 0; i < pad.touches.Count; i++)
            {
                var t = pad.touches[i];
                if (!t.press.isPressed) continue;
                n++;
                sum += t.position.ReadValue();
            }
            if (n < 2) return false;
            centroid = sum / n;
            return true;
        }

        static void Refresh()
        {
            Blocked = _block.Length > 0 && _mode == Stroke;
            if (SpendConfirm.Pending) Status = SpendConfirm.Line + "   TICK";
            else if (AreaPending) Status = "VUNG " + (PendingPlan.ok != null ? PendingPlan.ok.Count : 0) + " O  -" + PendingPlan.ore + " DA   TICK";
            else if (Moving >= 0 || MovingSite >= 0) Status = "DANG DOI   TICK DAT   HUY TRA LAI";
            else if (HomeDecor.Pick >= 0) Status = "DANG CAM " + HomeDecor.Defs[HomeDecor.Pick].name + "   TICK DAT";
            else if (BuildMode.BuildPick >= 0) Status = "DANG CAM " + BuildMode.Defs[BuildMode.BuildPick].name + "   TICK DAT";
            else if (Blocked) Status = _block;
            else if (BuildMode.Tab == 2) Status = RemoveAim >= 0 || RemoveAimSite >= 0 ? "DA CHON   X DE GO" : "CHAM DE DANH DAU";
            else if (BuildMode.Tab == 3 && BuildMode.LandPick >= 0 && BuildMode.LandSizeOf(BuildMode.LandPick) == BuildMode.LandSize.Rect) Status = "KEO VUNG   TICK DAT";
            else if (BuildMode.Tab == 3 && BuildMode.LandPick >= 0) Status = "KEO MOT NET   LUI DE TRA";
            else if (SelectedSite >= 0) Status = "CONG TRINH   DOI   XONG";
            else if (Selected >= 0) Status = "DOI   CAT   XONG";
            else Status = "KEO DE NHIN   CHAM DO DE CHON";
            if (_mode != Stroke) _block = "";

            bool place = Parked && (Moving >= 0 || MovingSite >= 0 || HomeDecor.Pick >= 0 || BuildMode.BuildPick >= 0);
            bool picked = (Selected >= 0 || SelectedSite >= 0) && Moving < 0 && MovingSite < 0;
            if (_bar != null) _bar.SetActive(true);
            Show(_tick, place || SpendConfirm.Pending || AreaPending);
            Show(_cancel, place || picked);
            bool canTurn = Moving >= 0 || MovingSite >= 0 || HomeDecor.Pick >= 0
                || (BuildMode.BuildPick >= 0 && BuildMode.Defs[BuildMode.BuildPick].id != "nha_go");
            Show(_turn, place && canTurn);
            Show(_undo, CanUndo);
            Show(_move, picked);
            Show(_store, Selected >= 0 && Moving < 0);
            Show(_done, picked);
        }

        static void Show(GameObject go, bool on)
        {
            if (go != null && go.activeSelf != on) go.SetActive(on);
        }

        static void EnsureBar()
        {
            if (_bar != null || BuildMode.Driver == null || BuildMode.Driver.Hud == null) return;
            var root = BuildMode.Driver.Hud.transform;
            _bar = new GameObject("YardHand");
            _bar.transform.SetParent(root, false);
            _bar.AddComponent<RectTransform>();
            _tick = Btn("Tick", "TICK", new Vector2(-78f, 0f), Confirm);
            _cancel = Btn("Cancel", "HUY", new Vector2(-26f, 0f), CancelHand);
            _turn = Btn("Turn", "XOAY", new Vector2(26f, 0f), Rotate);
            _undo = Btn("Undo", "LUI", new Vector2(78f, 0f), Undo);
            _move = Btn("Move", "DOI", new Vector2(-52f, 28f), BeginMove);
            _store = Btn("Store", "CAT", new Vector2(0f, 28f), Store);
            _done = Btn("Done", "XONG", new Vector2(52f, 28f), FinishSelect);
        }

        static GameObject Btn(string name, string label, Vector2 pos, UnityEngine.Events.UnityAction act)
        {
            var button = UiKit.Button(_bar.transform, name, name == "Tick" ? "Primary" : "Secondary", label, new Vector2(48f, 18f), act);
            var lay = button.GetComponent<UiLayout>();
            var port = UiLayout.Spec.At(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), pos + new Vector2(0f, 158f), new Vector2(48f, 18f));
            var land = UiLayout.Spec.At(new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), pos + new Vector2(0f, 8f), new Vector2(48f, 18f));
            lay.Landscape(land).Portrait(port);
            return button.gameObject;
        }
    }
}
