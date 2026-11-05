using System.Globalization;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>
    /// Build mode step 1: four existing village slots only. No free-grid placement.
    /// The house finishes into Housing.BuiltKey so rest and the big-house art stay on that flag.
    /// </summary>
    public class BuildMode : MonoBehaviour
    {
        const string SaveKey = WorldCatalog.SavePrefix + "build.sites";
        const string Art = "World/HomeDistrict/Build/";

        public struct Def
        {
            public string id, name, slot, sprite;
            public int wood, ore, skipCoins, seconds, level;
        }

        public static readonly Def[] Defs =
        {
            new Def { id = "nha_go", name = "NHA GO", slot = "slot_house", sprite = "HD_BigHouse", wood = 3, ore = 2, skipCoins = 20, seconds = 30, level = 1 },
            new Def { id = "chuong_ga", name = "CHUONG GA", slot = "slot_building", sprite = "HD_Coop", wood = 2, ore = 0, skipCoins = 8, seconds = 20, level = 1 },
            new Def { id = "kho_thoc", name = "KHO THOC", slot = "slot_decor", sprite = "HD_Foundation", wood = 3, ore = 1, skipCoins = 12, seconds = 24, level = 1 },
            new Def { id = "xuong_moc", name = "XUONG MOC", slot = "slot_service", sprite = "HD_Foundation", wood = 4, ore = 2, skipCoins = 16, seconds = 36, level = 3 }
        };

        struct Site
        {
            public string id;
            public double start;
            public float extra;
            public int seconds;
            public bool done;
            public bool flip;
            public int turn;
            public bool free;
            public float x, y;
        }

        static Site[] _sites = new Site[4];
        static int _count;
        static bool _loaded;
        static int _pick = -1;
        static int _last = -1;
        static bool _flip;
        static int _quarter;
        static float _saveIn;
        static bool _dirty;
        static string _banner;
        static float _bannerUntil;
        static SkillHotbarUI _bar;
        static bool _barHidden;

        public static bool IsOpen { get; private set; }
        /// <summary>0 CONG TRINH (slot buildings), 1 TRANG TRI (free placement), 2 X (remove).</summary>
        public static int Tab { get; private set; }

        public static void SetTab(int tab)
        {
            YardHand.CancelArea();
            YardHand.CancelDig();
            Tab = Mathf.Clamp(tab, 0, 3);
            SpendConfirm.Clear();
            MobileTouch.SetErase(false);
            _pick = -1;
            HomeDecor.Pick = -1;
            LandPick = -1;
            Driver?.HideGhost();
            HomeDecor.Hide();
        }

        static void OnCard(int n)
        {
            SpendConfirm.Clear();
            if (Tab == 0) { _pick = n; YardHand.ParkGhost(); }
            else if (Tab == 1)
            {
                int i = HomeDecor.Page * HomeDecor.PageSize + n;
                if (i < HomeDecor.Defs.Length) { HomeDecor.Pick = i; YardHand.ParkGhost(); }
            }
            else if (Tab == 3 && n < HomeLand.BrushNames.Length) { YardHand.CancelArea(); YardHand.CancelDig(); LandPick = n; }
        }

        /// <summary>Selected terrain brush on tab DAT (HomeLand.Brush), -1 none.</summary>
        public enum LandSize { One, Two, Rect }
        static readonly LandSize[] _landSize = { LandSize.One, LandSize.One, LandSize.One, LandSize.One };
        public static LandSize LandSizeOf(int pick) => pick >= 0 && pick < _landSize.Length ? _landSize[pick] : LandSize.One;
        public static void SetLandSize(LandSize size)
        {
            if (LandPick < 0 || LandPick >= _landSize.Length) return;
            _landSize[LandPick] = size;
            YardHand.CancelArea();
            YardHand.CancelDig();
        }
        public static void CycleLandSize(int dir)
        {
            if (LandPick < 0 || LandPick >= _landSize.Length) return;
            int n = (((int)_landSize[LandPick] + dir) % 3 + 3) % 3;
            SetLandSize((LandSize)n);
        }

        public static int LandPick { get; private set; } = -1;
        static bool _landDirty;
        static float _landRebuiltAt;

        /// <summary>Paints the cell under the cursor; rebuilds now or leaves it to the throttled drag rebuild.</summary>
        static bool PaintLand(HomeLand.Brush brush, bool rebuildNow, bool confirm = false)
        {
            var cell = HomeLand.CellOf(HomeDecor.CursorRaw());
            string why = HomeLand.Check(brush, cell);
            if (why.Length > 0) return WorldServices.Fail(Deny.Invalid, why);
            if (!HomeLand.Paint(brush, cell)) return WorldServices.Fail(Deny.Invalid, "KHONG DOI");
            if (brush == HomeLand.Brush.Grass || brush == HomeLand.Brush.Path) HomeFarm.DropBed(cell);
            _landDirty = true;
            if (rebuildNow) FlushLand();
            return WorldServices.Ok(HomeLand.BrushNames[(int)brush]);
        }

        static void FlushLand()
        {
            if (!_landDirty) return;
            _landDirty = false;
            _landRebuiltAt = Time.unscaledTime;
            HomeDistrict.Rebuild();
        }

        static void Turn(int delta)
        {
            HomeDecor.Page = (HomeDecor.Page + delta + HomeDecor.Pages) % HomeDecor.Pages;
            HomeDecor.Pick = -1;
        }
        public static BuildMode Driver { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Driver != null) return;
            var go = new GameObject("BuildMode");
            DontDestroyOnLoad(go);
            Driver = go.AddComponent<BuildMode>();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _sites = new Site[4];
            _count = 0;
            _loaded = false;
            _pick = -1;
            _last = -1;
            _flip = false;
            _quarter = 0;
            IsOpen = false;
            Tab = 0;
            LandPick = -1;
            _landDirty = false;
            _landRebuiltAt = 0f;
            Driver = null;
            _dirty = false;
            _banner = null;
            _bar = null;
            _barHidden = false;
            _filter = 0;
            _sheetOpen = false;
            GhostWhy = "";
            GhostShown = false;
            SheetCollapsed = false;
        }

        public static void Toggle()
        {
            if (IsOpen) Close(); else Open();
        }

        public static void Open()
        {
            if (WorldNavigator.Instance == null || WorldNavigator.Instance.Current != WorldZone.Housing) return;
            Load();
            IsOpen = true;
            WorldNavigator.Instance.SetPromptVisible(false);
            _bar = Object.FindAnyObjectByType<SkillHotbarUI>();
            if (_bar != null) { _bar.SetSkillsVisible(false); _barHidden = true; }
            Driver?.ShowUi();
            LockHero(true);
        }

        public static void Close()
        {
            IsOpen = false;
            _pick = -1;
            _last = -1;
            SpendConfirm.Clear();
            MobileTouch.SetErase(false);
            HomeDecor.Pick = -1;
            HomeDecor.Hide();
            LandPick = -1;
            FlushLand();
            HomeLayout.Flush();
            YardHand.Reset();
            WorldNavigator.Instance?.SetPromptVisible(true);
            if (_barHidden && _bar != null) _bar.SetSkillsVisible(true);
            _barHidden = false;
            Driver?.HideUi();
            Driver?.HideGhost();
            LockHero(false);
        }

        static void LockHero(bool locked)
        {
            var hero = GameObject.FindGameObjectWithTag("Player");
            if (hero != null) hero.GetComponent<PlayerMovement>()?.SetMovementLocked(locked);
        }

        public static void Escape()
        {
            if (YardHand.AreaLive) { YardHand.CancelArea(); return; }
            if (YardHand.DigWaiting) { YardHand.CancelDig(); return; }
            if (SpendConfirm.Pending) { SpendConfirm.Clear(); return; }
            if (_pick >= 0) { _pick = -1; Driver?.HideGhost(); return; }
            if (HomeDecor.Pick >= 0) { HomeDecor.Pick = -1; HomeDecor.Hide(); return; }
            if (LandPick >= 0) { LandPick = -1; Driver?.HideGhost(); return; }
            if (Tab != 0) { SetTab(0); return; }
            Close();
        }

        public static bool FocusSlot(string slotId)
        {
            for (int i = 0; i < Defs.Length; i++)
                if (Defs[i].slot == slotId) { _pick = i; return WorldServices.Ok(Defs[i].name); }
            return WorldServices.Fail(Deny.Invalid, "No such slot");
        }

        public static int BuildPick => _pick;
        public Canvas Hud => _canvas;
        public static bool Flip { get => _flip; set { _flip = value; SpendConfirm.Clear(); } }
        /// <summary>0..3, each step a clockwise quarter turn.</summary>
        public static int Quarter { get => _quarter; set { _quarter = ((value % 4) + 4) % 4; SpendConfirm.Clear(); } }
        public static void RotateQuarter() => Quarter = _quarter + 1;

        public static void MarkLand() => _landDirty = true;
        public static void FlushLandNow() => FlushLand();
        public static void FlushLandSoon()
        {
            if (_landDirty && Time.unscaledTime - _landRebuiltAt > 0.3f) FlushLand();
        }
        public static void RequestDig() { }

        public static void DropTool()
        {
            _pick = -1;
            HomeDecor.Pick = -1;
            LandPick = -1;
            SpendConfirm.Clear();
            HomeDecor.Hide();
            Driver?.HideGhost();
        }

        public static int FindSite(Vector2 local) => SiteAt(local);

        /// <summary>World bounds of a placed building's sprites. False when that site is not on screen.</summary>
        public static bool TrySiteBounds(int i, out Bounds b)
        {
            b = default;
            var driver = Driver;
            if (driver == null || (uint)i >= (uint)driver._stage.Length || driver._stage[i] == null) return false;
            bool any = false;
            foreach (var sr in driver._stage[i].GetComponentsInChildren<SpriteRenderer>())
            {
                if (sr.sprite == null || !sr.enabled || !sr.gameObject.activeInHierarchy) continue;
                if (!any) { b = sr.bounds; any = true; }
                else b.Encapsulate(sr.bounds);
            }
            return any;
        }

        public static int SiteCount { get { Load(); return _count; } }

        public struct SiteView
        {
            public string id, name;
            public Vector2 local;
            public bool done, flip, free;
            public int turn;
            public float progress, secondsLeft;
            public int skipCoins;
        }

        public static SiteView GetSite(int i)
        {
            Load();
            var view = new SiteView();
            if ((uint)i >= (uint)_count) return view;
            var s = _sites[i];
            var def = FindDef(s.id);
            view.id = s.id;
            view.name = def.name;
            view.local = SiteLocal(i);
            view.done = s.done;
            view.flip = s.flip;
            view.turn = s.turn;
            view.free = s.free;
            view.progress = Progress(s);
            view.secondsLeft = s.done ? 0f : Mathf.Max(0f, s.seconds * (1f - view.progress));
            view.skipCoins = def.skipCoins;
            return view;
        }

        public static bool MoveSite(int i, Vector2 local)
        {
            Load();
            if ((uint)i >= (uint)_count) return WorldServices.Fail(Deny.Invalid, "KHONG CO CONG TRINH");
            var def = FindDef(_sites[i].id);
            if (def.id == "nha_go") return WorldServices.Fail(Deny.Invalid, "NHA GO TREN MONG");
            string why = FitMove(def, local, i);
            if (why.Length > 0) return WorldServices.Fail(Deny.Invalid, why);
            bool coop = def.id == "chuong_ga" && _sites[i].done;
            Vector2 was = SiteLocal(i);
            var s = _sites[i];
            s.free = true;
            s.flip = _flip;
            s.turn = _quarter;
            s.x = local.x;
            s.y = local.y;
            _sites[i] = s;
            _dirty = true;
            Flush();
            Driver?.PaintSites();
            if (coop) HomeAnimals.FollowCoop(local - was);
            return WorldServices.Ok("DA DOI");
        }

        public static bool FlipSite(int i)
        {
            Load();
            if ((uint)i >= (uint)_count) return false;
            var s = _sites[i];
            s.flip = !s.flip;
            s.turn = 0;
            _flip = s.flip;
            _quarter = 0;
            _sites[i] = s;
            _dirty = true;
            Flush();
            Driver?.PaintSites();
            return true;
        }

        public static bool SkipSite(int i, bool confirm = false)
        {
            Load();
            if ((uint)i >= (uint)_count || _sites[i].done) return WorldServices.Fail(Deny.Invalid, "CHON CONG TRINH");
            var def = FindDef(_sites[i].id);
            int cost = def.skipCoins;
            if (WorldSave.Coins < cost) return WorldServices.Fail(Deny.InsufficientResources, "THIEU XU");
            string key = "skip:" + def.id;
            if (!(confirm && SpendConfirm.Take(key)))
            {
                int keep = i;
                SpendConfirm.Arm(key, "XAC NHAN " + def.name + "  " + cost + " XU", () => SkipSite(keep, true));
                return false;
            }
            _sites[i].done = true;
            var tx = WorldTx.Begin()
                .SetInt(WorldSave.KeyCoins, WorldSave.Coins - cost)
                .SetString(SaveKey, Encode())
                .After(() => WorldEvents.RaiseCoins(-cost));
            if (def.id == "nha_go") tx.SetString(Housing.BuiltKey, def.slot);
            tx.Commit();
            _dirty = false;
            SpendConfirm.Clear();
            Banner("HOAN THANH: " + def.name);
            Driver?.Puff(SiteWorld(i));
            if (def.id == "chuong_ga") HomeAnimals.Refresh();
            return true;
        }

        public static bool RemoveSite(int i, bool confirm)
        {
            Load();
            if ((uint)i >= (uint)_count) return WorldServices.Fail(Deny.Invalid, "KHONG CO CONG TRINH");
            var def = FindDef(_sites[i].id);
            string key = "rmsite:" + def.id;
            if (!confirm || !SpendConfirm.Take(key))
            {
                int keep = i;
                SpendConfirm.Arm(key, "GO " + def.name + "? HOAN GO " + def.wood + " DA " + def.ore, () => RemoveSite(keep, true));
                return false;
            }
            return RemoveSite(i);
        }

        public static bool RemoveFound(Vector2 local)
        {
            int site = SiteAt(local);
            if (site < 0) return false;
            return RemoveSite(site);
        }

        public static void TickKeys(Keyboard kb)
        {
            if (kb == null || !IsOpen) return;
            if (kb.rKey.wasPressedThisFrame) YardHand.Rotate();
            if (kb.zKey.wasPressedThisFrame) YardHand.Undo();
            if (kb.enterKey.wasPressedThisFrame || kb.numpadEnterKey.wasPressedThisFrame) YardHand.Confirm();
            if (kb.tabKey.wasPressedThisFrame) SetTab((Tab + 1) % 4);
            if (kb.leftBracketKey.wasPressedThisFrame && Tab == 1) Turn(-1);
            if (kb.rightBracketKey.wasPressedThisFrame && Tab == 1) Turn(1);
            if (kb.leftBracketKey.wasPressedThisFrame && Tab == 3) CycleLandSize(-1);
            if (kb.rightBracketKey.wasPressedThisFrame && Tab == 3) CycleLandSize(1);
            if (kb.digit1Key.wasPressedThisFrame) OnCard(0);
            if (kb.digit2Key.wasPressedThisFrame) OnCard(1);
            if (kb.digit3Key.wasPressedThisFrame) OnCard(2);
            if (kb.digit4Key.wasPressedThisFrame) OnCard(3);
        }

        public static bool TryPlace(bool confirm = false)
        {
            if (!IsOpen) return false;
            if (Tab == 1) return HomeDecor.Pick >= 0 && HomeDecor.TryPlace(_flip, confirm);
            if (Tab == 2) return false;
            if (Tab == 3) return LandPick >= 0 && PaintLand((HomeLand.Brush)LandPick, true, confirm);
            if (_pick < 0) return false;
            var def = Defs[_pick];
            if (WorldSave.Level < def.level) return WorldServices.Fail(Deny.Locked, "CAN CAP " + def.level);
            if (Taken(def.slot)) return WorldServices.Fail(Deny.AlreadyDone, "DA CO");
            int wood = WorldSave.GetQty("wood"), ore = WorldSave.GetQty("ore");
            if (wood < def.wood || ore < def.ore) return WorldServices.Fail(Deny.InsufficientResources, Short(def, wood, ore));
            var spot = Aim(def);
            string ground = Fit(def, spot);
            if (ground.Length > 0) return WorldServices.Fail(Deny.Invalid, ground);
            var site = new Site
            {
                id = def.id,
                start = GameplayClock.Now,
                extra = 0f,
                seconds = def.seconds,
                done = false,
                flip = _flip,
                turn = _quarter,
                free = def.id != "nha_go",
                x = spot.x,
                y = spot.y
            };
            Add(site);
            WorldTx.Begin()
                .SetInt(WorldSave.KeyItem("wood"), wood - def.wood)
                .SetInt(WorldSave.KeyItem("ore"), ore - def.ore)
                .SetString(SaveKey, Encode())
                .Commit();
            _dirty = false;
            SpendConfirm.Clear();
            _last = _pick;
            _pick = -1;
            WorldServices.Ok("DANG XAY " + def.name);
            return true;
        }

        /// <summary>What is missing, with amounts: "THIEU 3 GO 2 DA".</summary>
        static string Short(Def def, int wood, int ore)
        {
            string s = "THIEU";
            if (wood < def.wood) s += " " + (def.wood - wood) + " GO";
            if (ore < def.ore) s += " " + (def.ore - ore) + " DA";
            return s;
        }

        static int SkipTarget()
        {
            foreach (int d in new[] { _pick, _last })
            {
                if (d < 0) continue;
                int site = IndexOf(Defs[d].id);
                if (site >= 0 && !_sites[site].done) return d;
            }
            return -1;
        }

        public static bool Skip(bool confirm = false)
        {
            int pick = SkipTarget();
            if (pick < 0) return WorldServices.Fail(Deny.Invalid, "CHON CONG TRINH");
            var def = Defs[pick];
            int index = IndexOf(def.id);
            int cost = def.skipCoins;
            if (WorldSave.Coins < cost) return WorldServices.Fail(Deny.InsufficientResources, "THIEU XU");
            string skipAsk = "XAC NHAN " + def.name + "  " + cost + " XU";
            string skipKey = "skip:" + def.id;
            if (!(confirm && SpendConfirm.Take(skipKey)))
            {
                SpendConfirm.Arm(skipKey, skipAsk, () => Skip(true));
                return false;
            }
            _sites[index].done = true;
            var tx = WorldTx.Begin()
                .SetInt(WorldSave.KeyCoins, WorldSave.Coins - cost)
                .SetString(SaveKey, Encode())
                .After(() => WorldEvents.RaiseCoins(-cost));
            if (def.id == "nha_go") tx.SetString(Housing.BuiltKey, def.slot);
            tx.Commit();
            _dirty = false;
            SpendConfirm.Clear();
            Banner("HOAN THANH: " + def.name);
                Driver?.Puff(SiteWorld(index));
            if (def.id == "chuong_ga") HomeAnimals.Refresh();
            return true;
        }

        public static bool HouseBuildProgress(out float progress)
        {
            Load();
            progress = 0f;
            int i = IndexOf("nha_go");
            if (i < 0 || _sites[i].done) return false;
            progress = Progress(_sites[i]);
            return true;
        }

        /// <summary>Finished chicken coop, in home-local units.</summary>
        public static bool TryCoop(out Vector2 local)
        {
            Load();
            local = default;
            int i = IndexOf("chuong_ga");
            if (i < 0 || !_sites[i].done) return false;
            local = SiteLocal(i);
            return true;
        }

        void Update()
        {
            if (!Application.isPlaying) return;
            EnsureAsk();
            Load();
            float dt = GameplayClock.LastDelta;
            if (dt > 0f) Advance(dt);
            if (IsOpen)
            {
                YardHand.Tick();
                RefreshUi();
            }
            else HideGhost();
            if (_bannerUntil > 0f && Time.unscaledTime > _bannerUntil && _bannerLabel != null)
                _bannerLabel.gameObject.SetActive(false);
        }

        void OnApplicationQuit() => Flush();

        static void Advance(float dt)
        {
            for (int i = 0; i < _count; i++)
            {
                if (_sites[i].done) continue;
                var def = FindDef(_sites[i].id);
                if (Near(i)) { _sites[i].extra += dt; _dirty = true; }
                if (Progress(_sites[i]) < 1f) continue;
                _sites[i].done = true;
                var tx = WorldTx.Begin().SetString(SaveKey, Encode());
                if (def.id == "nha_go") tx.SetString(Housing.BuiltKey, def.slot);
                tx.Commit();
                _dirty = false;
                Banner("HOAN THANH: " + def.name);
                Driver?.Puff(SiteWorld(i));
                if (def.id == "chuong_ga") HomeAnimals.Refresh();
            }
            _saveIn -= dt;
            if (_dirty && _saveIn <= 0f) { Flush(); _saveIn = 1f; }
            Driver?.PaintSites();
        }

        public static void Flush()
        {
            if (!_dirty) return;
            WorldTx.Begin().SetString(SaveKey, Encode()).Commit();
            _dirty = false;
        }

        static float Progress(Site s)
        {
            if (s.done) return 1f;
            if (s.seconds <= 0) return 1f;
            return Mathf.Clamp01((float)((GameplayClock.Now - s.start + s.extra) / s.seconds));
        }

        static Vector2 Aim(Def def)
        {
            if (def.id != "nha_go") return HomeDecor.Cursor();
            var slot = GameObject.Find(def.slot);
            var root = GameObject.Find("HomeDistrict");
            if (slot == null || root == null) return new Vector2(0f, 3f);
            return root.transform.InverseTransformPoint(slot.transform.position);
        }

        /// <summary>Empty when a CHON building may stand here. The house stays on its foundation.</summary>
        static string Fit(Def def, Vector2 pos)
        {
            if (def.id == "nha_go") return "";
            var size = FootSize(def);
            var r = new Rect(pos.x - size.x * 0.5f, pos.y - size.y * 0.5f, size.x, size.y);
            string fix = HomeDecor.FixedBlock(r);
            if (fix.Length > 0) return fix;
            string fog = HomeArea.Block(r);
            if (fog.Length > 0) return fog;
            if (HomeClear.Overlaps(r)) return "DON TRUOC";
            var pieces = HomeLayout.Pieces;
            for (int i = 0; i < pieces.Count; i++)
                if (r.Overlaps(HomeDecor.Foot(pieces[i].def, pieces[i].pos))) return "VUONG DO KHAC";
            return GroundUnder(r);
        }

        /// <summary>Empty when the foot stands on dirt path.</summary>
        static string GroundUnder(Rect r)
        {
            Vector2[] pts =
            {
                new Vector2(r.center.x, r.center.y),
                new Vector2(r.xMin + 0.05f, r.yMin + 0.05f),
                new Vector2(r.xMax - 0.05f, r.yMin + 0.05f),
                new Vector2(r.xMin + 0.05f, r.yMax - 0.05f),
                new Vector2(r.xMax - 0.05f, r.yMax - 0.05f)
            };
            for (int i = 0; i < pts.Length; i++)
            {
                string ground = GroundWhy(pts[i]);
                if (ground.Length > 0) return ground;
            }
            return "";
        }

        /// <summary>Relocate rules. The new spot must be dirt path. The house is refused earlier.</summary>
        static string FitMove(Def def, Vector2 pos, int ignore)
        {
            var size = FootSize(def);
            var r = new Rect(pos.x - size.x * 0.5f, pos.y - size.y * 0.5f, size.x, size.y);
            string fix = HomeDecor.FixedBlock(r);
            if (fix.Length > 0) return fix;
            string fog = HomeArea.Block(r);
            if (fog.Length > 0) return fog;
            if (HomeLand.HasWater)
                for (int x = Mathf.FloorToInt(r.xMin); x < Mathf.CeilToInt(r.xMax); x++)
                    for (int y = Mathf.FloorToInt(r.yMin); y < Mathf.CeilToInt(r.yMax); y++)
                        if (HomeLand.Mask(new Vector2Int(x, y)) != 0) return "VUONG NUOC";
            if (HomeClear.Overlaps(r)) return "DON TRUOC";
            var pieces = HomeLayout.Pieces;
            for (int n = 0; n < pieces.Count; n++)
                if (r.Overlaps(HomeDecor.Foot(pieces[n].def, pieces[n].pos))) return "VUONG DO KHAC";
            for (int n = 0; n < _count; n++)
            {
                if (n == ignore || _sites[n].id == "nha_go") continue;
                if (r.Overlaps(SiteFoot(n))) return "VUONG DO KHAC";
            }
            return GroundUnder(r);
        }

        /// <summary>Foot used by Fit (also drawn under the ghost by the build UI).</summary>
        static Vector2 FootSize(Def def) => def.id == "chuong_ga" ? new Vector2(2f, 0.8f) : new Vector2(1.4f, 1f);

        static string GroundWhy(Vector2 p) => Dirt(p) ? "" : "VE DUONG DAT TRUOC";

        static bool Dirt(Vector2 p)
        {
            var c = HomeLand.CellOf(p);
            if (HomeLand.Mask(c) != 0) return false;
            return HomeLand.Kind(c) == HomeLand.Path;
        }

        static bool RemoveAtCursor()
        {
            Load();
            int site = SiteAt(HomeDecor.CursorRaw());
            if (site >= 0) return RemoveSite(site);
            return HomeDecor.TryRemove();
        }

        static int SiteAt(Vector2 local)
        {
            int best = -1;
            float bestD = float.MaxValue;
            for (int i = 0; i < _count; i++)
            {
                if (!Hits(i, local)) continue;
                float d = (SiteLocal(i) - local).sqrMagnitude;
                if (d < bestD) { bestD = d; best = i; }
            }
            return best;
        }

        public static bool SiteCovers(Vector2Int cell)
        {
            Load();
            var r = new Rect(cell.x, cell.y, 1f, 1f);
            for (int i = 0; i < _count; i++)
                if (SiteFoot(i).Overlaps(r)) return true;
            return false;
        }

        static Rect SiteFoot(int i)
        {
            var p = SiteLocal(i);
            string id = _sites[i].id;
            if (id == "nha_go") return new Rect(p.x - 3f, p.y - 1f, 6f, 6f);
            if (id == "chuong_ga") return new Rect(p.x - 1.2f, p.y - 0.7f, 2.4f, 1.4f);
            return new Rect(p.x - 0.8f, p.y - 0.6f, 1.6f, 1.2f);
        }

        static bool Hits(int i, Vector2 local)
        {
            if (SpriteHit(i, local)) return true;
            var p = SiteLocal(i);
            string id = _sites[i].id;
            var r = id == "nha_go" ? new Rect(p.x - 3f, p.y - 1f, 6f, 6f)
                : id == "chuong_ga" ? new Rect(p.x - 1.6f, p.y - 0.8f, 3.2f, 2.6f)
                : new Rect(p.x - 1.1f, p.y - 0.7f, 2.2f, 2.2f);
            return r.Contains(local);
        }

        /// <summary>Tap the drawn sprite, with a margin, not only the small foot under it.</summary>
        static bool SpriteHit(int i, Vector2 local)
        {
            if (!TrySiteBounds(i, out var b)) return false;
            var root = GameObject.Find("HomeDistrict");
            if (root == null) return false;
            b.Expand(0.7f);
            Vector3 w = root.transform.TransformPoint(new Vector3(local.x, local.y, 0f));
            return w.x >= b.min.x && w.x <= b.max.x && w.y >= b.min.y && w.y <= b.max.y;
        }

        static Vector2 SiteLocal(int i)
        {
            if (_sites[i].free) return new Vector2(_sites[i].x, _sites[i].y);
            var def = FindDef(_sites[i].id);
            var slot = GameObject.Find(def.slot);
            var root = GameObject.Find("HomeDistrict");
            if (slot == null || root == null) return Vector2.zero;
            return root.transform.InverseTransformPoint(slot.transform.position);
        }

        static bool RemoveSite(int i)
        {
            var def = FindDef(_sites[i].id);
            var stage = Driver != null ? Driver._stage : null;
            if (stage != null && stage[i] != null) Object.Destroy(stage[i].gameObject);
            for (int k = i; k < _count - 1; k++)
            {
                _sites[k] = _sites[k + 1];
                if (stage != null) stage[k] = stage[k + 1];
            }
            _count--;
            _sites[_count] = default;
            if (stage != null) stage[_count] = null;
            var tx = WorldTx.Begin()
                .SetInt(WorldSave.KeyItem("wood"), WorldSave.GetQty("wood") + def.wood)
                .SetInt(WorldSave.KeyItem("ore"), WorldSave.GetQty("ore") + def.ore)
                .SetString(SaveKey, Encode());
            if (def.id == "nha_go") tx.SetString(Housing.BuiltKey, "");
            tx.Commit();
            _dirty = false;
            if (def.id == "chuong_ga") HomeAnimals.Refresh();
            return WorldServices.Ok("DA GO " + def.name);
        }

        static Vector3 SiteWorld(int index)
        {
            var s = _sites[index];
            var def = FindDef(s.id);
            if (s.free)
            {
                var root = GameObject.Find("HomeDistrict");
                if (root != null) return root.transform.TransformPoint(new Vector3(s.x, s.y, 0f));
            }
            return Anchor(def.slot);
        }

        static bool Near(int index)
        {
            var hero = GameObject.FindGameObjectWithTag("Player");
            if (hero == null) return false;
            return ((Vector2)hero.transform.position - (Vector2)SiteWorld(index)).sqrMagnitude < 2.2f * 2.2f;
        }

        static bool Taken(string slot)
        {
            if (slot == "slot_house" && Housing.HasHouse) return true;
            for (int i = 0; i < _count; i++)
            {
                var def = FindDef(_sites[i].id);
                if (def.slot == slot) return true;
            }
            return false;
        }

        static void Add(Site site)
        {
            Load();
            int i = IndexOf(site.id);
            if (i < 0) { i = _count++; }
            _sites[i] = site;
        }

        static int IndexOf(string id)
        {
            for (int i = 0; i < _count; i++)
                if (_sites[i].id == id) return i;
            return -1;
        }

        static Def FindDef(string id)
        {
            for (int i = 0; i < Defs.Length; i++)
                if (Defs[i].id == id) return Defs[i];
            return default;
        }

        static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            _count = 0;
            var raw = PlayerPrefs.GetString(SaveKey, "");
            if (string.IsNullOrEmpty(raw)) return;
            var rows = raw.Split(';');
            for (int i = 0; i < rows.Length && _count < _sites.Length; i++)
            {
                var p = rows[i].Split('|');
                if (p.Length < 6) continue;
                _sites[_count++] = new Site
                {
                    id = p[0],
                    start = double.Parse(p[1], CultureInfo.InvariantCulture),
                    extra = float.Parse(p[2], CultureInfo.InvariantCulture),
                    seconds = int.Parse(p[3], CultureInfo.InvariantCulture),
                    done = p[4] == "1",
                    flip = p[5] == "1",
                    free = p.Length >= 8,
                    x = p.Length >= 8 ? float.Parse(p[6], CultureInfo.InvariantCulture) : 0f,
                    y = p.Length >= 8 ? float.Parse(p[7], CultureInfo.InvariantCulture) : 0f,
                    turn = TurnOf(p)
                };
            }
        }

        static int TurnOf(string[] p)
        {
            int i = p.Length >= 9 ? 8 : p.Length == 7 ? 6 : -1;
            if (i < 0 || !int.TryParse(p[i], NumberStyles.Integer, CultureInfo.InvariantCulture, out int t)) return 0;
            return t & 3;
        }

        static string Encode()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _count; i++)
            {
                if (i > 0) sb.Append(';');
                var s = _sites[i];
                sb.Append(s.id).Append('|')
                    .Append(s.start.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(s.extra.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(s.seconds.ToString(CultureInfo.InvariantCulture)).Append('|')
                    .Append(s.done ? "1" : "0").Append('|')
                    .Append(s.flip ? "1" : "0");
                if (s.free)
                    sb.Append('|').Append(s.x.ToString("0.##", CultureInfo.InvariantCulture))
                        .Append('|').Append(s.y.ToString("0.##", CultureInfo.InvariantCulture));
                sb.Append('|').Append(s.turn & 3);
            }
            return sb.ToString();
        }

        static void Banner(string text)
        {
            _banner = text;
            _bannerUntil = Time.unscaledTime + 2f;
            WorldNavigator.Instance?.Flash(text);
        }

        Canvas _canvas;
        GameObject _ask;
        Text _askLine;
        string _askKey;

        void EnsureAsk()
        {
            if (_ask != null)
            {
                bool show = SpendConfirm.Pending;
                if (_ask.activeSelf != show) _ask.SetActive(show);
                if (show && _askLine != null && _askKey != SpendConfirm.Line)
                {
                    _askKey = SpendConfirm.Line;
                    _askLine.text = SpendConfirm.Line;
                }
                if (!show) _askKey = null;
                return;
            }
            var canvas = UiKit.Canvas("SpendAsk", 275);
            _ask = canvas.gameObject;
            var plate = Flat(canvas.transform, "Plate", new Color(Plum.r, Plum.g, Plum.b, 0.94f), PlumBorder, true);
            Lay(plate, At(0.5f, 0f, 0f, 78f, 252f, 54f), At(0.5f, 0f, 0f, 168f, 252f, 54f));
            _askLine = UiKit.Label(plate.transform, "Ask", "", 8, Pal.GoldL, TextAnchor.MiddleCenter);
            UiKit.Place(_askLine, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(240f, 14f));
            _askLine.horizontalOverflow = HorizontalWrapMode.Overflow;
            var yes = UiKit.Button(plate.transform, "Yes", "Primary", "XAC NHAN", new Vector2(84f, 18f), () => SpendConfirm.Accept());
            UiKit.Place(yes, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(-46f, 8f), new Vector2(84f, 18f));
            UiKit.Reach(yes, 88f, 28f);
            var no = UiKit.Button(plate.transform, "No", "Secondary", "HUY", new Vector2(64f, 18f), () => SpendConfirm.Clear());
            UiKit.Place(no, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(48f, 8f), new Vector2(64f, 18f));
            UiKit.Reach(no, 72f, 28f);
            _ask.SetActive(SpendConfirm.Pending);
        }

        Text _title, _hint, _bannerLabel, _speed;
        ScrollRect _scroll;
        RectTransform _content, _viewport, _panel, _safe;
        Button _skip;
        Text _skipLabel;
        SpriteRenderer _ghost;
        Transform[] _stage = new Transform[4];
        Button _undoBtn, _sheetBtn;
        Image _sheetArrow;
        Image[] _chipBorder, _chipFill;
        Text[] _chipText;
        readonly System.Collections.Generic.List<CardUi> _pool = new System.Collections.Generic.List<CardUi>();
        readonly System.Collections.Generic.List<int> _decorList = new System.Collections.Generic.List<int>();
        readonly System.Collections.Generic.List<Lift> _lift = new System.Collections.Generic.List<Lift>();
        string[] _seenIds;
        Vector3[] _seenAt;

        /// <summary>Decor list filter: 0 all, 1 plants, 2 fences, 3 things.</summary>
        static int _filter;
        /// <summary>Portrait: the player opened the sheet again while holding a tool.</summary>
        static bool _sheetOpen;

        // ---- read by BuildUxView (display only) ----
        /// <summary>Why the ghost in hand cannot be placed here, "" when it can (same rules as TryPlace).</summary>
        internal static string GhostWhy { get; private set; } = "";
        internal static bool GhostShown { get; private set; }
        internal static Bounds GhostBounds { get; private set; }
        internal static Vector2 GhostLocal { get; private set; }
        internal static Vector2 GhostFoot { get; private set; }
        internal static bool SheetCollapsed { get; private set; }
        internal RectTransform Panel => _panel;
        internal RectTransform Safe => _safe;
        /// <summary>A site was added (true) or removed (false): world position, wood, ore.</summary>
        internal static event System.Action<Vector3, int, int, bool> SiteFx;

        class CardUi
        {
            public Button btn;
            public Image border, fill, pic, lockIcon, woodIcon, stoneIcon, clockIcon, track, bar;
            public Text name, wood, stone, time, badge;
        }

        struct Lift { public RectTransform rt; public float baseY; }

        // Build sidebar palette from the video reference (V28): plum panel, card, light border; gold marks the selection.
        static readonly Color Plum = new Color32(0x4A, 0x31, 0x40, 0xF2);
        static readonly Color PlumCard = new Color32(0x6A, 0x40, 0x48, 0xFF);
        static readonly Color PlumCardOn = new Color32(0x82, 0x52, 0x5A, 0xFF);
        static readonly Color PlumBorder = new Color32(0xB8, 0x9A, 0xA0, 0xFF);
        static readonly Color PlumDim = new Color32(0x6E, 0x55, 0x60, 0xFF);
        static readonly Color UxRed = new Color32(0xE0, 0x48, 0x48, 0xFF);
        static readonly Color UxCyan = new Color32(0x8E, 0xC8, 0xF0, 0xFF);
        static readonly Color UxGreen = new Color32(0x7E, 0xD6, 0x7E, 0xFF);
        static readonly Color UxGold = new Color32(0xF2, 0xD8, 0x4A, 0xFF);
        static readonly Color UxGrey = new Color32(0x8A, 0x92, 0xA0, 0xFF);
        static readonly Color Track = new Color32(0x2A, 0x2A, 0x2A, 0xFF);
        static readonly Color TrackFill = new Color32(0xD8, 0xB0, 0x40, 0xFF);
        /// <summary>Visual tab order CONG TRINH / DAT / TRANG TRI / X; each entry is the logic tab index.</summary>
        static readonly int[] TabOrder = { 0, 3, 1, 2 };
        static readonly string[] TabIcons = { "BD_Tab_Buildings", "BD_Tab_Decor", "BD_Tab_Remove", "BD_Tab_Land" };
        static readonly string[] LandIcons = { "Build/BD_Swatch_Path", "Build/BD_Swatch_Plot", "HD_PondBig", "Build/BD_Swatch_Grass" };
        static readonly string[] ChipNames = { "TAT CA", "CAY", "RAO", "VAT" };
        // 16:9 chip x / width (kit px): the first chip is wider for "TAT CA".
        static readonly float[] ChipX = { 6f, 57f, 86f, 115f };
        static readonly float[] ChipW = { 50f, 28f, 28f, 28f };

        /// <summary>Flat 1 px bordered box (reference style). Returns the border; the fill is its first child.</summary>
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

        internal static Sprite Ux(string n) => Resources.Load<Sprite>(Art + "Ux/BD_Ux_" + n);

        /// <summary>Flat plum button with a tinted pixel icon.</summary>
        internal static Button IconButton(Transform parent, string name, string icon, Color ink, UnityEngine.Events.UnityAction act)
        {
            var box = Flat(parent, name, PlumCard, PlumBorder, true);
            var btn = box.gameObject.AddComponent<Button>();
            btn.targetGraphic = box.transform.GetChild(0).GetComponent<Image>();
            btn.onClick.AddListener(act);
            var im = Img(box.transform, "Icon", Ux(icon));
            im.color = ink;
            UiKit.Place(im, UiKit.Center, UiKit.Center, Vector2.zero, new Vector2(10f, 10f));
            return btn;
        }

        static UiLayout Lay(Component c, UiLayout.Spec landscape, UiLayout.Spec portrait)
        {
            var l = c.GetComponent<UiLayout>();
            if (l == null) l = c.gameObject.AddComponent<UiLayout>();
            return l.Landscape(landscape).Portrait(portrait);
        }

        static UiLayout.Spec At(float ax, float ay, float x, float y, float w, float h) =>
            UiLayout.Spec.At(new Vector2(ax, ay), new Vector2(ax, ay), new Vector2(x, y), new Vector2(w, h));

        void ShowUi()
        {
            if (_canvas != null) { _canvas.gameObject.SetActive(true); return; }
            _canvas = UiKit.Canvas("BuildModeHud", 240);
            // Everything sits in a frame fitted to Screen.safeArea (notch, home bar).
            _safe = UiKit.Rect(_canvas.transform, "Safe");
            _safe.gameObject.AddComponent<BuildSafeArea>();
            var root = _safe.transform;

            // Sidebar: left column in 16:9, bottom sheet in 9:16. Every rect re-lays itself when the profile changes.
            var panel = Flat(root, "Panel", Plum, PlumBorder, true);
            _panel = panel.rectTransform;
            Lay(panel, At(0f, 0.5f, 8f, 0f, 150f, 256f),
                UiLayout.Spec.Stretch(new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(-8f, 128f)));
            _title = UiKit.Label(panel.transform, "Title", "CONG TRINH", 8, Pal.TextW, TextAnchor.MiddleLeft);
            Lay(_title, At(0f, 1f, 6f, -5f, 100f, 12f), At(0f, 1f, 20f, -5f, 92f, 12f));
            // Touch: bigger header buttons and tabs, plus an invisible tap margin (UiKit.Reach) toward ~44 pt.
            bool touch = UiKit.IsMobile;
            var done = UiKit.Button(panel.transform, "Done", "Positive", "XONG", touch ? new Vector2(44f, 20f) : new Vector2(34f, 14f), Close);
            Lay(done, touch ? At(1f, 1f, -4f, -3f, 44f, 20f) : At(1f, 1f, -4f, -4f, 34f, 14f), At(1f, 1f, -4f, -4f, 34f, 14f));
            UiKit.Reach(done, 48f, 30f);
            // TODO(C5): grey out while YardHand has nothing to undo (needs YardHand.CanUndo).
            _undoBtn = IconButton(panel.transform, "Undo", "Undo", Pal.TextW, YardHand.Undo);
            Lay(_undoBtn, touch ? At(1f, 1f, -52f, -3f, 22f, 20f) : At(1f, 1f, -42f, -4f, 16f, 14f), At(1f, 1f, -42f, -4f, 16f, 14f));
            UiKit.Reach(_undoBtn, 26f, 30f);
            _sheetBtn = IconButton(panel.transform, "Sheet", "Down", Pal.TextW, () => _sheetOpen = !_sheetOpen);
            _sheetArrow = _sheetBtn.transform.Find("Icon").GetComponent<Image>();
            Lay(_sheetBtn, At(0f, 1f, 4f, -4f, 14f, 14f), At(0f, 1f, 4f, -4f, 14f, 14f));

            _tabs = new Image[TabIcons.Length];
            _tabBorder = new Image[TabIcons.Length];
            _tabFill = new Image[TabIcons.Length];
            for (int s = 0; s < TabOrder.Length; s++)
            {
                int n = TabOrder[s];
                var box = Flat(panel.transform, "Tab" + n, Plum, PlumDim, true);
                box.gameObject.AddComponent<Button>().onClick.AddListener(() => SetTab(n));
                Lay(box, touch ? At(0f, 1f, 6f + s * 34f, -26f, 32f, 26f) : At(0f, 1f, 6f + s * 22f, -20f, 20f, 18f),
                    At(0f, 1f, 112f + s * 22f, -3f, 20f, 18f));
                UiKit.Reach(box, 34f, 30f);
                var icon = UiKit.Rect(box.transform, "Icon").gameObject.AddComponent<Image>();
                icon.sprite = Resources.Load<Sprite>(Art + TabIcons[n]);
                icon.preserveAspect = true;
                icon.raycastTarget = false;
                UiKit.Place(icon, UiKit.Center, UiKit.Center, Vector2.zero, new Vector2(14f, 14f));
                _tabs[n] = icon;
                _tabBorder[n] = box;
                _tabFill[n] = box.transform.GetChild(0).GetComponent<Image>();
            }

            // Decor filter chips (tab TRANG TRI): under the tabs in 16:9, in the title spot in 9:16.
            _chipBorder = new Image[ChipNames.Length];
            _chipFill = new Image[ChipNames.Length];
            _chipText = new Text[ChipNames.Length];
            for (int k = 0; k < ChipNames.Length; k++)
            {
                int n = k;
                var chip = Flat(panel.transform, "Chip" + k, Plum, PlumDim, true);
                chip.gameObject.AddComponent<Button>().onClick.AddListener(() => _filter = n);
                Lay(chip, At(0f, 1f, ChipX[k], touch ? -55f : -41f, ChipW[k], touch ? 16f : 13f),
                    At(0f, 1f, 19f + k * 23f, -5f, 23f, 13f));
                var t = UiKit.Label(chip.transform, "Text", ChipNames[k], 8, Pal.TextW, TextAnchor.MiddleCenter);
                UiKit.Fill(t);
                t.horizontalOverflow = HorizontalWrapMode.Overflow;
                _chipBorder[k] = chip;
                _chipFill[k] = chip.transform.GetChild(0).GetComponent<Image>();
                _chipText[k] = t;
            }

            // Card list: vertical scroll in 16:9, horizontal scroll in the 9:16 bottom sheet.
            _viewport = UiKit.Rect(panel.transform, "Viewport");
            var hit = _viewport.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            _viewport.gameObject.AddComponent<RectMask2D>();
            Lay(_viewport, At(0f, 1f, 6f, -44f, 138f, 206f),
                UiLayout.Spec.Stretch(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -24f), new Vector2(-12f, 96f)));
            _content = UiKit.Rect(_viewport, "Content");
            Lay(_content, At(0f, 1f, 0f, 0f, 138f, 185f), At(0f, 1f, 0f, 0f, 265f, 96f));
            _scroll = _viewport.gameObject.AddComponent<ScrollRect>();
            _scroll.viewport = _viewport;
            _scroll.content = _content;
            _scroll.movementType = ScrollRect.MovementType.Clamped;
            _scroll.scrollSensitivity = 12f;
            for (int i = 0; i < Defs.Length; i++) _pool.Add(MakeCard(i));

            _prev = UiKit.Button(panel.transform, "Prev", "Secondary", "<", new Vector2(16f, 14f), () => Turn(-1));
            _next = UiKit.Button(panel.transform, "Next", "Secondary", ">", new Vector2(16f, 14f), () => Turn(1));
            Lay(_prev, At(0f, 0f, 6f, 6f, 16f, 14f), At(1f, 1f, -62f, -4f, 16f, 14f));
            Lay(_next, At(1f, 0f, -6f, 6f, 16f, 14f), At(1f, 1f, -42f, -4f, 16f, 14f));
            _prev.gameObject.SetActive(false);
            _next.gameObject.SetActive(false);

            // Mode, hint, banner and speed sit at the bottom centre (16:9) or just above the sheet (9:16),
            // clear of the zone HUD and the hero card at the top. In 9:16 they drop when the sheet folds.
            // Plum plate behind the mode label + hint so they read on grass and fog (sized to the text in RefreshUi).
            _hintPlate = Flat(root, "HintPlate", new Color(Plum.r, Plum.g, Plum.b, 0.86f), PlumBorder, false);
            Lay(_hintPlate, At(0.5f, 0f, 0f, 27f, 200f, 32f), At(0.5f, 0f, 0f, 133f, 200f, 32f));
            _lift.Add(new Lift { rt = _hintPlate.rectTransform, baseY = 133f });
            _hint = UiKit.Label(root, "Hint", "", 8, Pal.GoldL, TextAnchor.MiddleCenter);
            Lay(_hint, At(0.5f, 0f, 0f, 30f, 280f, 12f), At(0.5f, 0f, 0f, 136f, 272f, 12f));
            _modeLabel = UiKit.Label(root, "Mode", "XAY DUNG [ESC]", 8, new Color(0.49f, 0.84f, 0.49f), TextAnchor.MiddleCenter);
            Lay(_modeLabel, At(0.5f, 0f, 0f, 44f, 160f, 12f), At(0.5f, 0f, 0f, 150f, 160f, 12f));
            _bannerLabel = UiKit.Label(root, "Banner", "", 8, new Color(0.49f, 0.84f, 0.49f), TextAnchor.MiddleCenter);
            Lay(_bannerLabel, At(0.5f, 0f, 0f, 58f, 220f, 12f), At(0.5f, 0f, 0f, 164f, 220f, 12f));
            _skip = UiKit.Button(root, "Skip", "Secondary", "XONG NGAY", new Vector2(110f, 16f), () => Skip());
            Lay(_skip, At(0.5f, 0f, 0f, 8f, 110f, 16f), At(0.5f, 0f, 0f, 180f, 110f, 16f));
            _skipLabel = _skip.GetComponentInChildren<Text>();
            _speed = UiKit.Label(root, "Speed", "", 8, Pal.TextW, TextAnchor.MiddleCenter);
            Lay(_speed, At(0.5f, 0f, 0f, 72f, 160f, 12f), At(0.5f, 0f, 0f, 200f, 160f, 12f));
            _lift.Add(new Lift { rt = _hint.rectTransform, baseY = 136f });
            _lift.Add(new Lift { rt = _modeLabel.rectTransform, baseY = 150f });
            _lift.Add(new Lift { rt = _bannerLabel.rectTransform, baseY = 164f });
            _lift.Add(new Lift { rt = (RectTransform)_skip.transform, baseY = 180f });
            _lift.Add(new Lift { rt = _speed.rectTransform, baseY = 200f });
        }

        Text _modeLabel;
        Image _hintPlate;
        Image[] _tabs, _tabBorder, _tabFill;
        Button _prev, _next;

        /// <summary>Card v2: picture, name, cost chips (wood/stone icon + number), build time, lock, progress, stock badge.</summary>
        CardUi MakeCard(int i)
        {
            var c = new CardUi();
            var card = Flat(_content, "Card" + i, PlumCard, PlumBorder, true);
            Lay(card, At(0f, 1f, 0f, 0f, 138f, 44f), At(0f, 1f, 0f, 0f, 64f, 96f));
            c.border = card;
            c.fill = card.transform.GetChild(0).GetComponent<Image>();
            int n = i;
            c.btn = card.gameObject.AddComponent<Button>();
            c.btn.targetGraphic = c.fill;
            c.btn.onClick.AddListener(() => OnCardUi(n));
            var t = card.transform;
            c.pic = Img(t, "Picture", null);
            Lay(c.pic, At(0f, 0.5f, 4f, 0f, 36f, 36f), At(0.5f, 1f, 0f, -4f, 36f, 36f));
            c.lockIcon = Img(t, "Lock", Ux("Lock"));
            c.lockIcon.color = UxGold;
            Lay(c.lockIcon, At(0f, 1f, 31f, -3f, 7f, 7f), At(1f, 1f, -3f, -3f, 7f, 7f));
            c.name = UiKit.Label(t, "Name", "", 8, Pal.TextW, TextAnchor.UpperLeft);
            Lay(c.name, At(0f, 1f, 44f, -4f, 92f, 10f), At(0.5f, 1f, 0f, -42f, 62f, 20f));
            c.woodIcon = Img(t, "WoodIcon", Resources.Load<Sprite>(Art + "BD_Icon_Wood"));
            Lay(c.woodIcon, At(0f, 1f, 44f, -16f, 8f, 8f), At(0f, 1f, 5f, -63f, 8f, 8f));
            c.wood = UiKit.Label(t, "Wood", "", 8, Pal.TextW, TextAnchor.UpperLeft);
            Lay(c.wood, At(0f, 1f, 53f, -15f, 28f, 10f), At(0f, 1f, 14f, -62f, 20f, 10f));
            c.stoneIcon = Img(t, "StoneIcon", Resources.Load<Sprite>(Art + "BD_Icon_Stone"));
            Lay(c.stoneIcon, At(0f, 1f, 84f, -16f, 8f, 8f), At(0f, 1f, 34f, -63f, 8f, 8f));
            c.stone = UiKit.Label(t, "Stone", "", 8, Pal.TextW, TextAnchor.UpperLeft);
            Lay(c.stone, At(0f, 1f, 93f, -15f, 28f, 10f), At(0f, 1f, 43f, -62f, 20f, 10f));
            c.clockIcon = Img(t, "Clock", Ux("Clock"));
            c.clockIcon.color = UxCyan;
            Lay(c.clockIcon, At(0f, 1f, 44f, -29f, 8f, 8f), At(0f, 1f, 6f, -76f, 8f, 8f));
            c.time = UiKit.Label(t, "Time", "", 8, UxCyan, TextAnchor.UpperLeft);
            Lay(c.time, At(0f, 1f, 54f, -28f, 82f, 10f), At(0f, 1f, 16f, -75f, 46f, 10f));
            c.track = Img(t, "Track", null);
            c.track.color = Track;
            c.track.preserveAspect = false;
            Lay(c.track, At(0f, 0f, 44f, 4f, 88f, 3f), At(0.5f, 0f, 0f, 4f, 56f, 3f));
            c.bar = Img(c.track.transform, "Bar", null);
            c.bar.color = TrackFill;
            c.bar.preserveAspect = false;
            c.bar.rectTransform.pivot = new Vector2(0f, 0.5f);
            c.bar.rectTransform.offsetMin = Vector2.zero;
            c.bar.rectTransform.offsetMax = Vector2.zero;
            c.badge = UiKit.Label(t, "Badge", "", 8, Pal.TextW, TextAnchor.LowerRight);
            Lay(c.badge, At(1f, 0f, -4f, 3f, 40f, 9f), At(0.5f, 1f, 0f, -32f, 58f, 9f));
            return c;
        }

        /// <summary>Fills one card. costRow shows wood/stone chips (red when short); line goes on the time row.</summary>
        void ShowCard(int k, Sprite pic, string name, bool costRow, int wood, int ore, int haveW, int haveO,
            string line, Color lineColor, bool clock, string badge, float progress, bool locked, bool selected)
        {
            while (_pool.Count <= k) _pool.Add(MakeCard(_pool.Count));
            var c = _pool[k];
            c.btn.gameObject.SetActive(true);
            c.pic.sprite = pic;
            c.pic.enabled = pic != null;
            c.pic.color = locked ? new Color(0.6f, 0.6f, 0.68f, 0.5f) : Color.white;
            c.lockIcon.enabled = locked;
            c.name.text = name;
            c.name.alignment = UiKit.IsPortrait ? TextAnchor.UpperCenter : TextAnchor.UpperLeft;
            c.woodIcon.enabled = costRow;
            c.wood.enabled = costRow;
            c.stoneIcon.enabled = costRow;
            c.stone.enabled = costRow;
            if (costRow)
            {
                c.wood.text = wood.ToString();
                c.wood.color = haveW < wood ? UxRed : Pal.TextW;
                c.stone.text = ore.ToString();
                c.stone.color = haveO < ore ? UxRed : Pal.TextW;
            }
            c.clockIcon.enabled = clock;
            c.time.text = line;
            c.time.color = lineColor;
            // Time row: after the clock icon; without it the line takes the whole row (centred in 9:16).
            var tr = c.time.rectTransform;
            float px = UiKit.Px;
            bool port = UiKit.IsPortrait;
            var a = port && !clock ? new Vector2(0.5f, 1f) : new Vector2(0f, 1f);
            tr.anchorMin = tr.anchorMax = tr.pivot = a;
            tr.anchoredPosition = (port ? new Vector2(clock ? 16f : 0f, -75f) : new Vector2(clock ? 54f : 44f, -28f)) * px;
            tr.sizeDelta = (port ? new Vector2(clock ? 46f : 64f, 10f) : new Vector2(clock ? 82f : 92f, 10f)) * px;
            c.time.alignment = port && !clock ? TextAnchor.UpperCenter : TextAnchor.UpperLeft;
            c.time.horizontalOverflow = HorizontalWrapMode.Overflow;
            c.track.enabled = progress >= 0f;
            c.bar.enabled = progress >= 0f;
            if (progress >= 0f) c.bar.rectTransform.anchorMax = new Vector2(Mathf.Round(Mathf.Clamp01(progress) * 28f) / 28f, 1f);
            c.badge.text = badge;
            c.badge.color = Pal.TextW;
            c.name.color = Pal.TextW;
            c.border.color = selected ? Pal.GoldL : PlumBorder;
            c.fill.color = selected ? PlumCardOn : PlumCard;
            // Positions: column in 16:9 (below the chips on TRANG TRI), row in 9:16.
            var rt = c.border.rectTransform;
            rt.anchoredPosition = UiKit.IsPortrait ? new Vector2(k * 67f * UiKit.Px, 0f) : new Vector2(0f, -k * 47f * UiKit.Px);
        }

        void OnCardUi(int k)
        {
            if (Tab != 1) { OnCard(k); return; }
            if (k >= _decorList.Count) return;
            SpendConfirm.Clear();
            HomeDecor.Pick = _decorList[k];
            YardHand.ParkGhost();
        }

        /// <summary>Old decor cards replaced by tab DAT (pond, garden) and the chuong_ga building (coop). Data is kept.</summary>
        static bool IsLegacyDecor(string id) => id == "pond" || id == "garden" || id == "coop";

        static int DecorKind(string id)
        {
            if (id.StartsWith("tree") || id.StartsWith("bush") || id.StartsWith("sapling") || id.StartsWith("flower")
                || id.StartsWith("planter") || id == "potted_plant" || id == "stump") return 1;
            if (id.Contains("fence") || id == "garden_gate") return 2;
            return 3;
        }

        void HideUi()
        {
            if (_canvas != null) _canvas.gameObject.SetActive(false);
        }

        void RefreshUi()
        {
            if (_canvas == null || !_canvas.gameObject.activeSelf) ShowUi();
            int wood = WorldSave.GetQty("wood"), ore = WorldSave.GetQty("ore");
            var red = new Color(0.88f, 0.28f, 0.28f);
            bool portrait = UiKit.IsPortrait;
            float px = UiKit.Px;
            bool hand = _pick >= 0 || HomeDecor.Pick >= 0 || YardHand.Moving >= 0 || (Tab == 3 && LandPick >= 0);
            bool picked = YardHand.Selected >= 0 && YardHand.Moving < 0;
            if (!hand && !picked) _sheetOpen = false;
            SheetCollapsed = portrait && (hand || picked) && !_sheetOpen;
            _scroll.vertical = !portrait;
            _scroll.horizontal = portrait;

            // ---- cards ----
            int shown = 0;
            if (Tab == 0)
            {
                for (int i = 0; i < Defs.Length; i++)
                {
                    var d = Defs[i];
                    bool locked = WorldSave.Level < d.level;
                    int site = IndexOf(d.id);
                    bool built = site >= 0 && _sites[site].done || d.id == "nha_go" && Housing.HasHouse;
                    bool building = site >= 0 && !_sites[site].done;
                    string line = locked ? "CAN CAP " + d.level : built ? "XONG" : building
                        ? Mathf.RoundToInt(Progress(_sites[site]) * 100f) + "%" : d.seconds + "S";
                    Color lineColor = locked ? UxRed : built ? UxGreen : building ? UxGold : UxCyan;
                    ShowCard(shown++, Resources.Load<Sprite>("World/HomeDistrict/" + d.sprite), d.name, !locked && !built && !building,
                        d.wood, d.ore, wood, ore, line, lineColor, !locked && !built && !building, "",
                        building ? Progress(_sites[site]) : -1f, locked, i == _pick);
                }
            }
            else if (Tab == 3)
            {
                for (int i = 0; i < HomeLand.BrushNames.Length; i++)
                {
                    var icon = i < LandIcons.Length ? Resources.Load<Sprite>("World/HomeDistrict/" + LandIcons[i]) : null;
                    ShowCard(shown++, icon, HomeLand.BrushNames[i], false, 0, 0, wood, ore,
                        "MIEN PHI", UxCyan, false, "", -1f, false, i == LandPick);
                }
            }
            else if (Tab == 1)
            {
                _decorList.Clear();
                for (int pass = 0; pass < 2; pass++)
                    for (int i = 0; i < HomeDecor.Defs.Length; i++)
                    {
                        var def = HomeDecor.Defs[i];
                        if (_filter != 0 && DecorKind(def.id) != _filter) continue;
                        // pond / garden / coop now live in tab DAT and CONG TRINH: no decor card; stock keys are kept (FARM_POND_CONTRACT F1)
                        if (IsLegacyDecor(def.id)) continue;
                        if ((HomeDecor.Stock(def.id) > 0) == (pass == 0)) _decorList.Add(i);
                    }
                for (int k = 0; k < _decorList.Count; k++)
                {
                    var def = HomeDecor.Defs[_decorList[k]];
                    int stock = HomeDecor.Stock(def.id);
                    ShowCard(shown++, HomeDistrict.PieceSprite(def.id), def.name, false, 0, 0, 0, 0,
                        def.beauty > 0f ? "DEP +" + def.beauty.ToString("0.##", CultureInfo.InvariantCulture) : "", UxGreen, false,
                        "x" + stock, -1f, false, _decorList[k] == HomeDecor.Pick);
                    if (stock <= 0)
                    {
                        // Out of stock: whole card reads red, badge says HET.
                        _pool[shown - 1].pic.color = new Color(1f, 1f, 1f, 0.45f);
                        _pool[shown - 1].name.color = UxRed;
                        _pool[shown - 1].badge.text = "HET";
                        _pool[shown - 1].badge.color = UxRed;
                        _pool[shown - 1].fill.color = new Color32(0x6A, 0x28, 0x30, 0xFF);
                        _pool[shown - 1].border.color = UxRed;
                    }
                }
            }
            for (int k = shown; k < _pool.Count; k++) _pool[k].btn.gameObject.SetActive(false);
            _content.sizeDelta = portrait ? new Vector2(Mathf.Max(1, shown) * 67f * px, 96f * px) : new Vector2(138f * px, shown * 47f * px);

            // ---- header, tabs, chips, sheet ----
            for (int i = 0; i < _tabs.Length; i++)
            {
                bool on = i == Tab;
                _tabs[i].color = on ? Color.white : new Color(1f, 1f, 1f, 0.55f);
                _tabBorder[i].color = on ? Pal.GoldL : PlumDim;
                _tabFill[i].color = on ? PlumCardOn : Plum;
            }
            bool chips = Tab == 1 && !SheetCollapsed;
            for (int k = 0; k < _chipBorder.Length; k++)
            {
                _chipBorder[k].gameObject.SetActive(chips);
                bool on = k == _filter;
                _chipBorder[k].color = on ? Pal.GoldL : PlumDim;
                _chipFill[k].color = on ? PlumCardOn : Plum;
                _chipText[k].color = on ? Pal.TextW : UxGrey;
            }
            // 16:9: cards start under the chips on TRANG TRI.
            if (!portrait)
            {
                bool touch = UiKit.IsMobile;
                float top = Tab == 1 ? (touch ? 74f : 58f) : (touch ? 58f : 44f);
                // Decor + filters need four full 47-tall cards; GO BO stays a header strip.
                float tall = Tab == 2 ? (touch ? 58f : 44f) : Tab == 1 && touch ? 278f : 256f;
                _viewport.anchoredPosition = new Vector2(6f * px, -top * px);
                _viewport.sizeDelta = new Vector2(138f * px, (tall - top - 6f) * px);
                if (!Mathf.Approximately(_panel.sizeDelta.y, tall * px))
                {
                    // Pivot is the left middle: move down by half the cut so the header stays where it was.
                    _panel.sizeDelta = new Vector2(_panel.sizeDelta.x, tall * px);
                    _panel.anchoredPosition = new Vector2(_panel.anchoredPosition.x, (256f - tall) * 0.5f * px);
                }
            }
            _undoBtn.gameObject.SetActive(Tab == 3);
            _sheetBtn.gameObject.SetActive(portrait && (hand || picked));
            _sheetArrow.sprite = Ux(SheetCollapsed ? "Up" : "Down");
            _viewport.gameObject.SetActive(!SheetCollapsed);
            if (portrait) _panel.sizeDelta = new Vector2(_panel.sizeDelta.x, (SheetCollapsed ? 26f : 128f) * px);
            float drop = SheetCollapsed ? 102f : 0f;
            foreach (var l in _lift)
                if (portrait) l.rt.anchoredPosition = new Vector2(l.rt.anchoredPosition.x, (l.baseY - drop) * px);
            string held = _pick >= 0 ? Defs[_pick].name : HomeDecor.Pick >= 0 ? HomeDecor.Defs[HomeDecor.Pick].name
                : Tab == 3 && LandPick >= 0 ? HomeLand.BrushNames[LandPick] : "";
            _title.gameObject.SetActive(!(portrait && chips));
            _title.text = SheetCollapsed && held.Length > 0 ? held
                : Tab == 0 ? "CONG TRINH" : Tab == 1 ? "TRANG TRI" : Tab == 2 ? "GO BO" : "DAT";

            // ---- why the ghost is red (shown as a chip by BuildUxView) ----
            string why = "";
            if (YardHand.Moving >= 0)
            {
                var pieces = HomeLayout.Pieces;
                if ((uint)YardHand.Moving < (uint)pieces.Count) why = HomeDecor.Check(pieces[YardHand.Moving].def, HomeDecor.Cursor(), YardHand.Moving);
            }
            else if (YardHand.MovingSite >= 0)
            {
                int carried = YardHand.MovingSite;
                var carriedDef = (uint)carried < (uint)_count ? FindDef(_sites[carried].id) : default;
                if (carriedDef.id != null && carriedDef.id != "nha_go") why = FitMove(carriedDef, HomeDecor.Cursor(), carried);
            }
            else if (Tab == 0 && _pick >= 0)
            {
                var d = Defs[_pick];
                why = WorldSave.Level < d.level ? "CAN CAP " + d.level : Taken(d.slot) ? "DA CO"
                    : wood < d.wood || ore < d.ore ? Short(d, wood, ore) : d.id == "nha_go" ? "" : Fit(d, Aim(d));
            }
            else if (Tab == 1 && HomeDecor.Pick >= 0)
            {
                var def = HomeDecor.Defs[HomeDecor.Pick];
                why = HomeDecor.Stock(def.id) <= 0 ? "HET " + def.name : HomeDecor.Check(def.id, HomeDecor.Cursor());
            }
            else if (Tab == 3 && LandPick >= 0)
                why = HomeLand.Check((HomeLand.Brush)LandPick, HomeLand.CellOf(HomeDecor.CursorRaw()));
            GhostWhy = why;

            // ---- hint: how to act, per platform (reasons go in the chip) ----
            if (_hint != null)
            {
                bool touch = BuildUxView.IsTouch;
                string h;
                if (SpendConfirm.Pending) h = SpendConfirm.Line + (touch ? "" : "   E XAC NHAN   ESC HUY");
                else if (YardHand.Blocked) h = YardHand.Status;
                // House: fixed on its foundation, so say what blocks it, else what to press.
                else if (Tab == 0 && _pick >= 0 && Defs[_pick].id == "nha_go")
                    h = why.Length > 0 ? "KIEM GO, DA O KHU TAI NGUYEN" : touch ? "CHAM V DE XAY NHA" : "NHAP V / E DE XAY NHA";
                else if (YardHand.Moving >= 0 || (hand && Tab != 3)) h = touch ? "CHAM / KEO DE DOI CHO" : "NHAP / E DAT   R XOAY   ESC HUY";
                else if (Tab == 3 && LandPick >= 0) h = touch ? "KEO MOT NET   2 NGON DE NHIN" : "GIU CHUOT TRAI VE   Z LUI";
                else if (picked) h = touch ? "CHAM CHO TRONG DE BO CHON" : "NHAP CHO TRONG DE BO CHON";
                else if (Tab == 2) h = touch ? "THA TAY TREN DO DE CAT" : "NHAP DO DE GO   ESC HUY";
                else h = touch ? "KEO DE NHIN   CHAM DO DE CHON" : "NHAP DO DE CHON   CHUOT PHAI KEO";
                _hint.text = h;
                _hint.color = YardHand.Blocked || (Tab == 0 && _pick >= 0 && why.Length > 0) ? red : Pal.GoldL;
                // Keyboard key only where there is a keyboard.
                string mode = touch ? "XAY DUNG" : "XAY DUNG [ESC]";
                if (_modeLabel.text != mode) _modeLabel.text = mode;
                if (_hintPlate != null)
                {
                    // Width follows the longer row (+8 kit px each side); height stays 32 kit px.
                    float w = Mathf.Max(_hint.preferredWidth, _modeLabel.preferredWidth) / px + 16f;
                    var size = new Vector2(Mathf.Clamp(w, 80f, 300f) * px, 32f * px);
                    if (_hintPlate.rectTransform.sizeDelta != size) _hintPlate.rectTransform.sizeDelta = size;
                }
            }
            bool speeding = false;
            for (int i = 0; i < _count; i++)
                if (!_sites[i].done && Near(i)) speeding = true;
            if (_speed != null) _speed.text = speeding ? "2.0x TOC DO" : "";
            if (_bannerLabel != null)
            {
                bool show = !string.IsNullOrEmpty(_banner) && Time.unscaledTime < _bannerUntil;
                _bannerLabel.gameObject.SetActive(show);
                if (show) _bannerLabel.text = _banner;
            }
            int target = SkipTarget();
            bool canSkip = target >= 0;
            if (canSkip && _skipLabel != null)
                _skipLabel.text = SpendConfirm.Is("skip:" + Defs[target].id) ? "XAC NHAN " + Defs[target].skipCoins + " XU" : "XONG NGAY " + Defs[target].skipCoins + " XU";
            if (_skip != null) _skip.gameObject.SetActive(canSkip);
            PaintGhost();
            if (Tab == 1 || Tab == 2 || YardHand.Moving >= 0 || YardHand.Selected >= 0) HomeDecor.Paint(Tab, _flip);
            PaintLandGhost();
            PaintSites();
            WatchSites();
        }

        /// <summary>Tells the build UI when a site appears or goes (dust, rising resource numbers).</summary>
        void WatchSites()
        {
            if (_seenIds == null || _seenIds.Length != _sites.Length) { _seenIds = new string[_sites.Length]; _seenAt = new Vector3[_sites.Length]; _seenSites = -1; }
            if (_seenSites >= 0 && _count != _seenSites)
            {
                for (int i = 0; i < _count; i++)
                {
                    bool old = false;
                    for (int k = 0; k < _seenSites; k++) if (_seenIds[k] == _sites[i].id) old = true;
                    if (!old) { var d = FindDef(_sites[i].id); SiteFx?.Invoke(SiteWorld(i), d.wood, d.ore, true); }
                }
                for (int k = 0; k < _seenSites; k++)
                {
                    if (IndexOf(_seenIds[k]) >= 0) continue;
                    var d = FindDef(_seenIds[k]);
                    SiteFx?.Invoke(_seenAt[k], d.wood, d.ore, false);
                }
            }
            _seenSites = _count;
            for (int i = 0; i < _count; i++) { _seenIds[i] = _sites[i].id; _seenAt[i] = SiteWorld(i); }
        }

        int _seenSites = -1;

        SpriteRenderer _landGhost;

        void PaintLandGhost()
        {
            if (Tab != 3 || LandPick < 0)
            {
                if (_landGhost != null) _landGhost.enabled = false;
                return;
            }
            var root = GameObject.Find("HomeDistrict");
            if (_landGhost == null)
            {
                _landGhost = new GameObject("LandGhost").AddComponent<SpriteRenderer>();
                _landGhost.sortingOrder = 5000;
            }
            var cell = HomeLand.CellOf(HomeDecor.CursorRaw());
            bool ok = HomeLand.Check((HomeLand.Brush)LandPick, cell).Length == 0;
            var tiles = FarmArt.UiTiles;
            _landGhost.sprite = tiles != null ? tiles[0, ok ? 1 : 2] : null;
            var local = new Vector3(cell.x + 0.5f, cell.y + 0.5f, 0f);
            _landGhost.transform.position = root != null ? root.transform.TransformPoint(local) : local;
            // Invalid cells stay red (tile 2); a cream/blue tint was washing that out.
            _landGhost.color = !ok
                ? new Color(1f, 0.28f, 0.28f, 1f)
                : LandPick == (int)HomeLand.Brush.Grass
                    ? new Color(0.55f, 0.82f, 1f, 0.9f)
                    : new Color(1f, 0.92f, 0.7f, 0.9f);
            _landGhost.enabled = true;
        }

        void PaintGhost()
        {
            if (YardHand.MovingSite >= 0)
            {
                var view = GetSite(YardHand.MovingSite);
                var def = FindDef(view.id);
                if (def.id == null || def.id == "nha_go") { HideGhost(); return; }
                var spot = HomeDecor.Cursor();
                var home = GameObject.Find("HomeDistrict");
                bool fit = FitMove(def, spot, YardHand.MovingSite).Length == 0;
                Vector3 at = home != null ? home.transform.TransformPoint(new Vector3(spot.x, spot.y, 0f)) : (Vector3)spot;
                if (_ghost == null)
                {
                    var go = new GameObject("BuildGhost");
                    _ghost = go.AddComponent<SpriteRenderer>();
                    _ghost.sortingOrder = 5000;
                }
                _ghost.sprite = Resources.Load<Sprite>("World/HomeDistrict/" + def.sprite);
                _ghost.transform.localScale = def.id == "chuong_ga" ? Vector3.one : new Vector3(0.45f, 0.45f, 1f);
                _ghost.transform.position = at;
                _ghost.flipX = _flip;
                _ghost.transform.localEulerAngles = new Vector3(0f, 0f, -_quarter * 90f);
                _ghost.color = fit ? new Color(0.4f, 0.8f, 0.4f, 0.45f) : new Color(0.88f, 0.28f, 0.28f, 0.45f);
                _ghost.enabled = true;
                GhostShown = _ghost.sprite != null;
                GhostBounds = _ghost.bounds;
                GhostLocal = spot;
                GhostFoot = FootSize(def);
                return;
            }
            if (Tab != 0 || _pick < 0 || Taken(Defs[_pick].slot)) { HideGhost(); return; }
            var d = Defs[_pick];
            bool house = d.id == "nha_go";
            Vector3 world;
            bool ok = WorldSave.Level >= d.level && WorldSave.GetQty("wood") >= d.wood && WorldSave.GetQty("ore") >= d.ore;
            if (house)
            {
                var slot = GameObject.Find(d.slot);
                if (slot == null) { HideGhost(); return; }
                world = slot.transform.position;
            }
            else
            {
                var spot = Aim(d);
                var root = GameObject.Find("HomeDistrict");
                ok = ok && Fit(d, spot).Length == 0;
                world = root != null ? root.transform.TransformPoint(new Vector3(spot.x, spot.y, 0f)) : (Vector3)spot;
            }
            if (_ghost == null)
            {
                var go = new GameObject("BuildGhost");
                _ghost = go.AddComponent<SpriteRenderer>();
                _ghost.sortingOrder = 5000;
            }
            _ghost.sprite = Resources.Load<Sprite>("World/HomeDistrict/" + d.sprite);
            _ghost.transform.localScale = Vector3.one;
            _ghost.transform.position = world;
            _ghost.flipX = _flip;
            _ghost.transform.localEulerAngles = new Vector3(0f, 0f, -_quarter * 90f);
            _ghost.color = ok ? new Color(0.4f, 0.8f, 0.4f, 0.45f) : new Color(0.88f, 0.28f, 0.28f, 0.45f);
            _ghost.enabled = true;
            GhostShown = _ghost.sprite != null;
            GhostBounds = _ghost.bounds;
            GhostLocal = house ? Vector2.zero : Aim(d);
            GhostFoot = house ? Vector2.zero : FootSize(d);
        }

        void HideGhost()
        {
            GhostShown = false;
            if (_ghost != null) _ghost.enabled = false;
            if (_landGhost != null) _landGhost.enabled = false;
        }

        public void PaintSites()
        {
            for (int i = 0; i < _count; i++)
            {
                var def = FindDef(_sites[i].id);
                if (def.id == "nha_go") continue;
                if (i == YardHand.MovingSite)
                {
                    if (_stage[i] != null) _stage[i].gameObject.SetActive(false);
                    continue;
                }
                Transform parent;
                Vector3 local = Vector3.zero;
                if (_sites[i].free)
                {
                    var root = GameObject.Find("HomeDistrict");
                    if (root == null) continue;
                    parent = root.transform;
                    local = new Vector3(_sites[i].x, _sites[i].y, 0f);
                }
                else
                {
                    var slot = GameObject.Find(def.slot);
                    if (slot == null) continue;
                    parent = slot.transform;
                }
                if (_stage[i] == null || _stage[i].parent != parent)
                {
                    var go = new GameObject("BuildStage");
                    go.transform.SetParent(parent, false);
                    go.AddComponent<SpriteRenderer>();
                    go.AddComponent<SpriteSorter>();
                    go.AddComponent<BuildingFade>();
                    _stage[i] = go.transform;
                }
                if (_stage[i].GetComponent<BuildingFade>() == null)
                {
                    if (_stage[i].GetComponent<SpriteSorter>() == null) _stage[i].gameObject.AddComponent<SpriteSorter>();
                    _stage[i].gameObject.AddComponent<BuildingFade>();
                }
                if (!_stage[i].gameObject.activeSelf) _stage[i].gameObject.SetActive(true);
                _stage[i].localPosition = local;
                _stage[i].localEulerAngles = new Vector3(0f, 0f, -(_sites[i].turn & 3) * 90f);
                var sr = _stage[i].GetComponent<SpriteRenderer>();
                float p = Progress(_sites[i]);
                // under construction: rising building + scaffold + tools (BuildSiteFx); falls through once done
                var finished = Resources.Load<Sprite>("World/HomeDistrict/" + def.sprite);
                if (BuildSiteFx.Paint(_stage[i], def.id, finished, def.id == "chuong_ga" ? 1f : 0.45f, _sites[i].flip, p, !_sites[i].done && p < 1f, Near(i))) continue;
                string sprite = _sites[i].done || p >= 1f ? def.sprite : "HD_Foundation";
                if (p < 0.3f) sprite = "Build/BD_BlueprintTile";
                else if (!_sites[i].done && p < 1f) sprite = "Build/BD_Scaffold";
                sr.sprite = Resources.Load<Sprite>(sprite.StartsWith("Build/") ? "World/HomeDistrict/" + sprite : "World/HomeDistrict/" + sprite);
                sr.flipX = _sites[i].flip;
                _stage[i].localScale = def.id == "chuong_ga" ? Vector3.one : new Vector3(0.45f, 0.45f, 1f);
                if (!_sites[i].done && p >= 0.3f) _stage[i].localScale = new Vector3(0.35f, 0.35f, 1f);
            }
        }

        static Vector3 Anchor(string slotId)
        {
            var slot = GameObject.Find(slotId);
            return slot != null ? slot.transform.position : Vector3.zero;
        }

        public void Puff(Vector3 pos)
        {
            var sprite = Resources.Load<Sprite>(Art + "BD_BlueprintTile");
            if (sprite == null) return;
            var go = new GameObject("BuildDust");
            go.transform.position = pos + Vector3.up * 0.4f;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = new Color(0.9f, 0.9f, 0.85f, 0.8f);
            sr.sortingOrder = 6000;
            Destroy(go, 0.4f);
        }
    }
}
