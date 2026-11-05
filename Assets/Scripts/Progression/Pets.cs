using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>
    /// Owned pets. Farm eggs and the yard dog stay on their own keys.
    /// One hatch transaction writes the egg count and the new pet together.
    /// Save row per pet: id,species,follow,owner,rarity,level,xp. Missing columns load as Common, level 1, xp 0.
    /// </summary>
    public static class Pets
    {
        public const string EggId = "mystery_pet_egg";
        public const string ShardId = "pet_egg_shard";
        public const string IntroQuest = "q_pet_intro";
        /// <summary>One local hero save, so every pet belongs to this owner.</summary>
        public const string LocalOwner = "player";

        const string ListKey = WorldCatalog.SavePrefix + "pet.list";
        const string ActiveKey = WorldCatalog.SavePrefix + "pet.active";
        const string NextKey = WorldCatalog.SavePrefix + "pet.next";

        static readonly string[] Pool = { "dog", "cat", "fox", "rabbit", "wolf", "dragon" };
        static readonly string[] Names = { "Dog", "Cat", "Fox", "Rabbit", "Wolf", "Baby Dragon" };
        static readonly string[] Sheets =
        {
            "Pet_Dog", "Pet_Cat", "Pet_Fox", "Pet_Rabbit", "Pet_Wolf", "Pet_BabyDragon"
        };
        static readonly Vector2 FeetPivot = new Vector2(0.5f, 0f);
        static readonly Vector2 MidPivot = new Vector2(0.5f, 0.5f);

        // Tuning. 1 tile = 1 world unit (16 px), same grid as HomeAnimals / HomeLand.
        const float Heel = 0.6f, HeelStart = 0.9f, HeelStop = 0.35f, HeelKeep = 0.5f, WarpDist = 12f, ReturnDist = 6f;
        const float FollowMul = 1.15f, CatchMul = 1.6f, Accel = 25f, Decel = 35f, EngageReach = 4f;
        const float BodyRadius = 0.2f, AnimIdle = 0.12f, XpShare = 0.5f, XpSaveEvery = 2f;
        const int MaxLevel = 20, ShardCost = 10, EggCap = 99;
        static readonly int[] BaseAtk = { 4, 3, 4, 2, 5, 6 };
        static readonly float[] RarityMul = { 1f, 1.3f, 1.7f };
        static readonly float[] AtkEvery = { 1.1f, 0.8f, 1f, 0.7f, 1.2f, 1.5f };
        static readonly float[] Detours = { 0f, 35f, -35f, 70f, -70f, 105f, -105f };

        enum Act { Follow, Engage, Return }

        struct Pet
        {
            public string id, species, owner;
            public bool follow;
            public int rarity, level, xp;
        }

        static readonly List<Pet> _pets = new();
        static bool _loaded, _lock, _catch, _stepping, _haveHeel, _xpDirty;
        static int _detourSign = 1;
        static float _xpAt;
        static Vector2 _vel, _heldHeel;
        static Transform _heroTf, _housingTf;
        static WorldZone _zoneCached = (WorldZone)(-1);
        static Act _act;
        static Health _foe;
        static float _swing;
        static PetCompanion _body;
        static readonly Dictionary<string, Sprite[,]> _sheets = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _pets.Clear();
            _loaded = _lock = _catch = _stepping = _haveHeel = _xpDirty = false;
            _detourSign = 1;
            _xpAt = 0f;
            _vel = _heldHeel = Vector2.zero;
            _heroTf = _housingTf = null;
            _zoneCached = (WorldZone)(-1);
            _act = Act.Follow;
            _foe = null;
            _swing = 0f;
            _body = null;
            _sheets.Clear();
        }

        public static int Count { get { Load(); return _pets.Count; } }
        public static string ActiveId { get { Load(); return PlayerPrefs.GetString(ActiveKey, ""); } }
        public static bool HatchBusy => _lock;

        public static string SpeciesName(string species)
        {
            int i = System.Array.IndexOf(Pool, species);
            return i < 0 ? "Unknown" : Names[i];
        }

        public static bool Known(string species) => System.Array.IndexOf(Pool, species) >= 0;

        public static string IdAt(int i) { Load(); return (uint)i < (uint)_pets.Count ? _pets[i].id : ""; }
        public static string SpeciesAt(int i) { Load(); return (uint)i < (uint)_pets.Count ? _pets[i].species : ""; }
        public static bool FollowAt(int i) { Load(); return (uint)i < (uint)_pets.Count && _pets[i].follow; }
        public static int RarityAt(int i) { Load(); return (uint)i < (uint)_pets.Count ? _pets[i].rarity : 0; }
        public static int LevelAt(int i) { Load(); return (uint)i < (uint)_pets.Count ? _pets[i].level : 1; }
        public static int XpAt(int i) { Load(); return (uint)i < (uint)_pets.Count ? _pets[i].xp : 0; }
        public static int XpNeedAt(int i) { int level = LevelAt(i); return level >= MaxLevel ? 0 : XpNeed(level); }
        public static int AtkAt(int i)
        {
            Load();
            if ((uint)i >= (uint)_pets.Count) return 1;
            return Attack(_pets[i].species, _pets[i].rarity, _pets[i].level);
        }

        public static int XpNeed(int level) => 20 + 10 * Mathf.Max(1, level);

        public static int RarityFromRoll(int roll0to99)
        {
            if (roll0to99 < 60) return 0;
            return roll0to99 < 90 ? 1 : 2;
        }

        /// <summary>Adds XP and carries the overflow into later levels. Level 20 keeps no extra XP.</summary>
        public static bool ApplyXp(ref int level, ref int xp, int gain)
        {
            if (gain <= 0) return false;
            if (level < 1) level = 1;
            if (level >= MaxLevel) { xp = 0; return false; }
            xp += gain;
            bool up = false;
            while (level < MaxLevel && xp >= XpNeed(level))
            {
                xp -= XpNeed(level);
                level++;
                up = true;
            }
            if (level >= MaxLevel) xp = 0;
            return up;
        }

        public static int Attack(string species, int rarity, int level)
        {
            int s = System.Array.IndexOf(Pool, species);
            if (s < 0) s = 0;
            int r = Mathf.Clamp(rarity, 0, 2);
            int lv = Mathf.Clamp(level < 1 ? 1 : level, 1, MaxLevel);
            int atk = Mathf.RoundToInt(BaseAtk[s] * RarityMul[r] * (1f + 0.08f * (lv - 1)));
            return Mathf.Max(1, atk);
        }

        public static void ReadProgress(string[] columns, out int rarity, out int level, out int xp)
        {
            rarity = columns != null && columns.Length > 4 && int.TryParse(columns[4], out int r) ? Mathf.Clamp(r, 0, 2) : 0;
            level = columns != null && columns.Length > 5 && int.TryParse(columns[5], out int lv) ? Mathf.Clamp(lv, 1, MaxLevel) : 1;
            xp = columns != null && columns.Length > 6 && int.TryParse(columns[6], out int x) ? Mathf.Max(0, x) : 0;
            if (level >= MaxLevel) xp = 0;
        }

        public static string SacrificeBlock(bool missing, bool active, int rarity, bool busy, bool combat)
        {
            if (busy) return "Hatch in progress";
            if (missing) return "No pet";
            if (active) return "Recall first";
            if (rarity != 0) return "Only Common";
            if (combat) return "In combat";
            return "";
        }

        public static string CombinePlan(int shards, int eggs, out int shardsAfter, out int eggsAfter)
        {
            shardsAfter = shards;
            eggsAfter = eggs;
            if (shards < ShardCost) return "Need 10 shards";
            if (eggs >= EggCap) return "Egg bag full";
            shardsAfter = shards - ShardCost;
            eggsAfter = eggs + 1;
            return "";
        }
        public static string OwnerAt(int i) { Load(); return (uint)i < (uint)_pets.Count ? _pets[i].owner : ""; }
        public static int IndexOf(string id) { Load(); return Index(id); }
        public static bool IsActive(string id) => !string.IsNullOrEmpty(id) && id == ActiveId;

        /// <summary>Species sheet, sliced like the farm dog: 6 x 3 cells of 24 x 20, feet pivot.</summary>
        public static Sprite[,] FramesFor(string species)
        {
            int i = System.Array.IndexOf(Pool, species);
            if (i < 0) return FarmArt.Dog;
            if (_sheets.TryGetValue(Sheets[i], out var cached) && cached != null) return cached;
            var grid = FarmArt.Grid(Sheets[i], 6, 3, 24, 20, FeetPivot);
            if (grid == null) return FarmArt.Dog;
            _sheets[Sheets[i]] = grid;
            return grid;
        }

        /// <summary>Attack sheet: 4 x 3 cells of 32 x 24, feet at the bottom center. Damage lands on column 2.</summary>
        public static Sprite[,] AttackFrames(string species)
        {
            string file = species == "dragon" ? "Pet_BabyDragon_Attack" : "Pet_" + Cap(species) + "_Attack";
            return FarmArt.Grid(file, 4, 3, 32, 24, FeetPivot);
        }

        static string Cap(string species)
        {
            if (string.IsNullOrEmpty(species)) return "Dog";
            return char.ToUpperInvariant(species[0]) + species.Substring(1);
        }

        public static Sprite[,] Frames => FramesFor("dog");

        public static Sprite Portrait(string species)
        {
            int i = System.Array.IndexOf(Pool, species);
            if (i < 0) return FarmArt.Cell(FarmArt.Dog, 0, 4);
            var face = FarmArt.Cell(FarmArt.Grid(Sheets[i] + "_Portrait", 1, 1, 24, 24, MidPivot), 0, 0);
            return face != null ? face : FarmArt.Cell(FramesFor(species), 0, 4);
        }

        /// <summary>Hatch sheet, one row of 7 cells, 48 x 64, feet pivot. 0 idle … 6 open.</summary>
        public static Sprite EggFrame(int index)
        {
            var grid = FarmArt.Grid("Pet_Egg_Hatch_Sheet", 7, 1, 48, 64, FeetPivot);
            return FarmArt.Cell(grid, 0, index);
        }

        public static string CanHatch()
        {
            if (_lock) return "Hatch in progress";
            if (Pool.Length == 0) return "No pet species";
            var nav = WorldNavigator.Instance;
            if (nav == null || nav.Current == WorldZone.None) return "Not here";
            var hero = GameObject.FindGameObjectWithTag("Player");
            var health = hero != null ? hero.GetComponent<Health>() : null;
            if (health == null || health.IsDead) return "Hero is down";
            var gate = GameplayClock.CheckOutOfCombat();
            if (gate != Deny.None) return GameplayClock.DenyText(gate);
            if (WorldSave.GetQty(EggId) < 1) return "No egg";
            return "";
        }

        /// <summary>Consumes one egg and adds one pet in one journaled write. Memory changes only after the commit.</summary>
        public static bool Hatch(out string petId, out string species)
        {
            petId = species = "";
            string why = CanHatch();
            if (why.Length > 0) return WorldServices.Fail(why);
            _lock = true;
            bool done = false;
            try
            {
                Load();
                int eggs = WorldSave.GetQty(EggId);
                if (eggs < 1) return WorldServices.Fail("No egg");
                species = Pool[Random.Range(0, Pool.Length)];
                int rarity = RarityFromRoll(Random.Range(0, 100));
                int n = Mathf.Max(1, PlayerPrefs.GetInt(NextKey, 1));
                while (Index("p" + n) >= 0) n++;
                petId = "p" + n;
                var row = new Pet { id = petId, species = species, owner = LocalOwner, follow = true, rarity = rarity, level = 1, xp = 0 };
                _pets.Add(row);
                string list = Encode();
                _pets.RemoveAt(_pets.Count - 1);
                bool summon = string.IsNullOrEmpty(ActiveId) && SafeSpot(out _);
                var tx = WorldTx.Begin()
                    .SetInt(WorldSave.KeyItem(EggId), eggs - 1)
                    .SetString(ListKey, list)
                    .SetInt(NextKey, n + 1);
                if (summon) tx.SetString(ActiveKey, petId);
                tx.Commit();
                _pets.Add(row);
                done = true;
                if (summon) Appear();
                return WorldServices.Ok("Hatched " + SpeciesName(species));
            }
            finally
            {
                if (!done) _loaded = false;
                _lock = false;
            }
        }

        /// <summary>Makes this pet the one companion. A different active pet is replaced on the same body.</summary>
        public static bool Summon(string id)
        {
            Load();
            int i = Index(id);
            if (i < 0) return WorldServices.Fail("No pet");
            if (!Known(_pets[i].species)) return WorldServices.Fail("Unknown species");
            if (IsActive(id)) return WorldServices.Ok("Already with you");
            var nav = WorldNavigator.Instance;
            if (nav == null || nav.Current == WorldZone.None) return WorldServices.Fail("Not here");
            if (!SafeSpot(out _)) return WorldServices.Fail("No safe spot");
            var p = _pets[i];
            p.follow = true;
            _pets[i] = p;
            WorldTx.Begin().SetString(ActiveKey, id).SetString(ListKey, Encode()).Commit();
            Appear();
            return WorldServices.Ok("Summoned " + SpeciesName(p.species));
        }

        public static bool Recall()
        {
            Load();
            if (string.IsNullOrEmpty(ActiveId)) return WorldServices.Fail("No active pet");
            WorldTx.Begin().SetString(ActiveKey, "").Commit();
            if (_body != null) _body.gameObject.SetActive(false);
            return WorldServices.Ok("Recalled");
        }

        public static bool SetFollow(string id, bool follow)
        {
            Load();
            int i = Index(id);
            if (i < 0) return false;
            var p = _pets[i];
            p.follow = follow;
            _pets[i] = p;
            WorldTx.Begin().SetString(ListKey, Encode()).Commit();
            _catch = false;
            return true;
        }

        /// <summary>Active pet walks after the hero.</summary>
        public static bool Follow()
        {
            string id = ActiveId;
            int i = Index(id);
            if (i < 0) return WorldServices.Fail("No active pet");
            if (_pets[i].follow) return WorldServices.Ok("Already following");
            SetFollow(id, true);
            return WorldServices.Ok("Following");
        }

        /// <summary>Active pet holds its current spot.</summary>
        public static bool Stay()
        {
            string id = ActiveId;
            int i = Index(id);
            if (i < 0) return WorldServices.Fail("No active pet");
            if (!_pets[i].follow) return WorldServices.Ok("Already staying");
            SetFollow(id, false);
            return WorldServices.Ok("Staying");
        }

        /// <summary>A staying pet runs back to the hero. Same body, no new instance; not a recall.</summary>
        public static bool Call()
        {
            string id = ActiveId;
            int i = Index(id);
            if (i < 0) return WorldServices.Fail("No active pet");
            if (_pets[i].follow) return WorldServices.Fail("Already following");
            SetFollow(id, true);
            _catch = true;
            return WorldServices.Ok("Coming");
        }

        /// <summary>Places the one pet body beside the hero (zone change, summon, load). Never makes a second body.</summary>
        public static void Appear()
        {
            Load();
            _heroTf = null;
            _zoneCached = (WorldZone)(-1);
            _haveHeel = false;
            _vel = Vector2.zero;
            _act = Act.Follow;
            _foe = null;
            BindWorld();
            string id = ActiveId;
            int i = Index(id);
            var nav = WorldNavigator.Instance;
            if (i < 0 || !Known(_pets[i].species) || nav == null || nav.Current == WorldZone.None || !SafeSpot(out Vector2 spot))
            {
                if (_body != null) _body.gameObject.SetActive(false);
                return;
            }
            if (_body == null)
            {
                var go = new GameObject("PetCompanion");
                _body = go.AddComponent<PetCompanion>();
            }
            _body.gameObject.SetActive(true);
            _body.Show(_pets[i].species, spot);
            _catch = false;
        }

        public static void Tick(float dt)
        {
            FlushXp(false);
            if (_body == null || !_body.gameObject.activeSelf) return;
            BindWorld();
            if (_heroTf == null) return;
            int i = Index(ActiveId);
            if (i < 0) { _body.gameObject.SetActive(false); return; }
            Vector2 hero = _heroTf.position;
            Vector2 here = _body.transform.position;
            if (!_pets[i].follow)
            {
                _act = Act.Follow;
                _foe = null;
                _vel = Vector2.zero;
                _stepping = false;
                _body.Animate(Vector2.zero, 0f, HeroSpeed(), Vector2.zero);
                return;
            }

            Vector2 rawHeel = HeelPoint(hero);
            Vector2 heel = HoldHeel(rawHeel);
            if (Vector2.Distance(here, hero) > WarpDist && SafeSpot(rawHeel, out Vector2 warp))
            {
                _body.transform.position = warp;
                _vel = Vector2.zero;
                _heldHeel = warp;
                _haveHeel = true;
                here = warp;
            }

            Think(hero, here);
            Vector2 goal = _act == Act.Engage && _foe != null ? (Vector2)_foe.transform.position : heel;
            float speed = (_act == Act.Follow && !_catch) ? HeroSpeed() * FollowMul : HeroSpeed() * CatchMul;
            float gap = Vector2.Distance(here, goal);
            bool close = _act == Act.Engage && _foe != null && gap <= RangeOf(_pets[i].species);
            bool go;
            if (_act == Act.Engage) go = !close;
            else
            {
                if (!_stepping && gap > HeelStart) _stepping = true;
                else if (_stepping && gap < HeelStop) _stepping = false;
                go = _stepping;
                if (_act == Act.Return && gap < HeelStop) { _act = Act.Follow; _catch = false; _stepping = false; go = false; }
            }

            Vector2 look = _act == Act.Engage && _foe != null ? (Vector2)_foe.transform.position - here : Vector2.zero;
            if (close) Strike(i, look, dt);
            Move(here, goal, speed, dt, go, look);
        }

        static void Think(Vector2 hero, Vector2 here)
        {
            bool home = _zoneCached == WorldZone.Housing;
            bool combat = GameplayClock.CheckOutOfCombat() == Deny.InCombat;
            var health = _heroTf != null ? _heroTf.GetComponent<Health>() : null;
            bool dead = health != null && health.IsDead;
            bool far = Vector2.Distance(here, hero) > ReturnDist;
            if (_act == Act.Engage && (dead || home || !combat || far || _foe == null || _foe.IsDead))
            {
                if (!dead && !home && combat && !far)
                {
                    var next = PickFoe(hero);
                    if (next != null) { _foe = next; return; }
                }
                _foe = null;
                _act = Act.Return;
            }
            if (_act == Act.Follow && _catch) _act = Act.Return;
            if (_act != Act.Engage && !home && !dead && combat && !far)
            {
                var foe = PickFoe(hero);
                if (foe != null) { _foe = foe; _act = Act.Engage; }
            }
        }

        static Health PickFoe(Vector2 hero)
        {
            if (_heroTf == null) return null;
            var combat = _heroTf.GetComponent<PlayerCombat>();
            if (combat == null) return null;
            return CombatTargeting.LockedOrNearest(combat.StickyTarget, hero, EngageReach, combat.EnemyMask);
        }

        static void Strike(int i, Vector2 look, float dt)
        {
            if (_foe == null || _body == null || look.sqrMagnitude < 0.0001f) return;
            int s = System.Array.IndexOf(Pool, _pets[i].species);
            if (s < 0) s = 0;
            if (_body.Attacking)
            {
                if (_body.AdvanceAttack(dt) == 2 && _foe != null && !_foe.IsDead)
                    _foe.TakeDamage(Attack(_pets[i].species, _pets[i].rarity, _pets[i].level), _foe.transform.position, look.normalized * 0.5f);
                if (!_body.Attacking) _swing = AtkEvery[s];
                return;
            }
            _swing -= dt;
            if (_swing > 0f) return;
            _body.Aim(_foe.transform.position);
            _body.BeginAttack(look);
        }

        static void Move(Vector2 here, Vector2 goal, float speed, float dt, bool go, Vector2 look)
        {
            Vector2 desired = Vector2.zero;
            if (go)
            {
                Vector2 to = goal - here;
                if (to.sqrMagnitude > 0.0001f) desired = to.normalized * speed;
            }
            float rate = desired.sqrMagnitude + 0.0001f >= _vel.sqrMagnitude ? Accel : Decel;
            _vel = Vector2.MoveTowards(_vel, desired, rate * dt);
            float heroSpeed = HeroSpeed();
            if (_vel.sqrMagnitude > 0.0001f && TryStep(here, _vel.normalized, _vel.magnitude * dt, out Vector2 next))
            {
                _body.transform.position = next;
                _body.Animate(next - here, _vel.magnitude, heroSpeed, look);
            }
            else
            {
                if (!go) _vel = Vector2.zero;
                _body.Animate(Vector2.zero, 0f, heroSpeed, look);
            }
        }

        public static void OnEnemyKilled(int exp)
        {
            if (exp <= 0) return;
            Load();
            int i = Index(ActiveId);
            if (i < 0) return;
            var p = _pets[i];
            if (p.level >= MaxLevel) return;
            int gain = Mathf.FloorToInt(exp * XpShare);
            if (gain <= 0) return;
            bool up = ApplyXp(ref p.level, ref p.xp, gain);
            _pets[i] = p;
            _xpDirty = true;
            if (up) FlushXp(true);
        }

        static void FlushXp(bool force)
        {
            if (!_xpDirty) return;
            if (!force && Time.unscaledTime < _xpAt + XpSaveEvery) return;
            Load();
            WorldTx.Begin().SetString(ListKey, Encode()).Commit();
            _xpDirty = false;
            _xpAt = Time.unscaledTime;
        }

        public static string CanSacrifice(string id)
        {
            Load();
            int i = Index(id);
            bool combat = GameplayClock.CheckOutOfCombat() == Deny.InCombat;
            return SacrificeBlock(i < 0, i >= 0 && IsActive(id), i >= 0 ? _pets[i].rarity : 0, _lock, combat);
        }

        public static bool Sacrifice(string id)
        {
            string why = CanSacrifice(id);
            if (why.Length > 0) return WorldServices.Fail(why);
            _lock = true;
            bool done = false;
            try
            {
                Load();
                int i = Index(id);
                if (i < 0) return WorldServices.Fail("No pet");
                var row = _pets[i];
                _pets.RemoveAt(i);
                string list = Encode();
                _pets.Insert(i, row);
                int shards = WorldSave.GetQty(ShardId) + 1;
                WorldTx.Begin().SetString(ListKey, list).SetInt(WorldSave.KeyItem(ShardId), shards).Commit();
                _pets.RemoveAt(i);
                done = true;
                return WorldServices.Ok("Sacrificed");
            }
            finally
            {
                if (!done) _loaded = false;
                _lock = false;
            }
        }

        public static string CanCombine()
        {
            if (_lock) return "Hatch in progress";
            return CombinePlan(WorldSave.GetQty(ShardId), WorldSave.GetQty(EggId), out _, out _);
        }

        public static bool Combine()
        {
            if (_lock) return WorldServices.Fail("Hatch in progress");
            _lock = true;
            try
            {
                string why = CombinePlan(WorldSave.GetQty(ShardId), WorldSave.GetQty(EggId), out int shards, out int eggs);
                if (why.Length > 0) return WorldServices.Fail(why);
                WorldTx.Begin()
                    .SetInt(WorldSave.KeyItem(ShardId), shards)
                    .SetInt(WorldSave.KeyItem(EggId), eggs)
                    .Commit();
                return WorldServices.Ok("Combined");
            }
            finally
            {
                _lock = false;
            }
        }

        /// <summary>Straight step, else the nearest detour angle that stays legal. Keeps the last detour side to avoid jitter.</summary>
        static bool TryStep(Vector2 from, Vector2 dir, float len, out Vector2 next)
        {
            for (int k = 0; k < Detours.Length; k++)
            {
                float a = Detours[k] * _detourSign;
                next = from + Rotate(dir, a) * len;
                if (!Legal(next)) continue;
                if (a != 0f) _detourSign = a > 0f ? 1 : -1;
                return true;
            }
            next = from;
            return false;
        }

        static Vector2 Rotate(Vector2 v, float deg)
        {
            float r = deg * Mathf.Deg2Rad, c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(v.x * c - v.y * s, v.x * s + v.y * c);
        }

        static int Index(string id)
        {
            if (string.IsNullOrEmpty(id)) return -1;
            for (int i = 0; i < _pets.Count; i++)
                if (_pets[i].id == id) return i;
            return -1;
        }

        static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            _pets.Clear();
            var seen = new HashSet<string>();
            string raw = PlayerPrefs.GetString(ListKey, "");
            if (!string.IsNullOrEmpty(raw))
            {
                string[] rows = raw.Split(';');
                for (int i = 0; i < rows.Length; i++)
                {
                    if (rows[i].Length == 0) continue;
                    string[] c = rows[i].Split(',');
                    if (c.Length < 2 || c[0].Length == 0 || !seen.Add(c[0])) continue;
                    ReadProgress(c, out int rarity, out int level, out int xp);
                    _pets.Add(new Pet
                    {
                        id = c[0],
                        species = c[1],
                        follow = c.Length < 3 || c[2] != "0",
                        owner = c.Length >= 4 && c[3].Length > 0 ? c[3] : LocalOwner,
                        rarity = rarity,
                        level = level,
                        xp = xp
                    });
                }
            }
            string active = PlayerPrefs.GetString(ActiveKey, "");
            if (active.Length > 0 && Index(active) < 0)
                PlayerPrefs.SetString(ActiveKey, "");
        }

        static string Encode()
        {
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < _pets.Count; i++)
            {
                if (i > 0) sb.Append(';');
                sb.Append(_pets[i].id).Append(',').Append(_pets[i].species).Append(',').Append(_pets[i].follow ? "1" : "0")
                  .Append(',').Append(string.IsNullOrEmpty(_pets[i].owner) ? LocalOwner : _pets[i].owner)
                  .Append(',').Append(_pets[i].rarity).Append(',').Append(_pets[i].level).Append(',').Append(_pets[i].xp);
            }
            return sb.ToString();
        }

        static void BindWorld()
        {
            var nav = WorldNavigator.Instance;
            var zone = nav != null ? nav.Current : WorldZone.None;
            if (_heroTf == null)
            {
                var hero = GameObject.FindGameObjectWithTag("Player");
                if (hero != null) _heroTf = hero.transform;
            }
            if (zone == _zoneCached) return;
            _zoneCached = zone;
            var housing = GameObject.Find("zone_housing");
            _housingTf = housing != null ? housing.transform : null;
            _haveHeel = false;
        }

        static float HeroSpeed()
        {
            var move = _heroTf != null ? _heroTf.GetComponent<PlayerMovement>() : null;
            if (move == null) return 4.5f;
            return Mathf.Max(0.1f, move.MoveSpeed * move.SpeedMultiplier);
        }

        static float RangeOf(string species) => species == "dragon" ? 2.5f : 0.9f;

        static Vector2 HeelPoint(Vector2 hero)
        {
            Vector2 facing = Vector2.down;
            var visual = _heroTf != null ? _heroTf.GetComponent<PlayerVisual>() : null;
            if (visual != null) facing = visual.Facing;
            if (facing.sqrMagnitude < 0.0001f) facing = Vector2.down;
            return hero - facing.normalized * Heel;
        }

        static Vector2 HoldHeel(Vector2 raw)
        {
            if (_haveHeel && (raw - _heldHeel).sqrMagnitude < HeelKeep * HeelKeep) return _heldHeel;
            if (!SafeSpot(raw, out Vector2 safe)) return raw;
            _heldHeel = safe;
            _haveHeel = true;
            return safe;
        }

        static bool SafeSpot(out Vector2 world)
        {
            BindWorld();
            Vector2 hero = _heroTf != null ? (Vector2)_heroTf.position : Vector2.zero;
            return SafeSpot(HeelPoint(hero), out world);
        }

        static bool SafeSpot(Vector2 want, out Vector2 world)
        {
            world = want;
            if (Legal(want)) return true;
            for (int r = 1; r <= 8; r++)
            {
                float dist = r * 0.35f;
                for (int a = 0; a < 8; a++)
                {
                    float rad = a * (Mathf.PI * 2f / 8f);
                    Vector2 q = want + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * dist;
                    if (!Legal(q)) continue;
                    world = q;
                    return true;
                }
            }
            return false;
        }

        /// <summary>No solid collider, no Water/Lava volume, and inside the housing walls when that zone is up.</summary>
        static bool Legal(Vector2 world)
        {
            var hazards = HazardVolume.Active;
            for (int i = 0; i < hazards.Count; i++)
                if (hazards[i] != null && hazards[i].Contains(world)) return false;
            if (HomeAnimals.Blocked(world, BodyRadius, false)) return false;
            if (_housingTf == null || !_housingTf.gameObject.activeInHierarchy) return true;
            Vector2 local = _housingTf.InverseTransformPoint(world);
            const float pad = 0.75f;
            if (!HomeAnimals.InWalls(local)) return false;
            if (HomeAnimals.InGarden(local, 0.2f) || HomeAnimals.InHouse(local) || HomeAnimals.InCoop(local)) return false;
            if (HomeAnimals.InPond(local, pad) || HomeLand.InWater(local)) return false;
            if (HomeLand.InWater(local + new Vector2(pad, 0.15f)) || HomeLand.InWater(local + new Vector2(-pad, 0.15f))) return false;
            return true;
        }

        /// <summary>Species sheets are already colored, so the body stays untinted.</summary>
        public static Color Tint(string species) => Color.white;
    }

    /// <summary>One pet body. Sheet rows: 0 down, 1 up, 2 side; cols 0-3 walk, 4-5 sit. Dragon walk is flight.</summary>
    public class PetCompanion : MonoBehaviour
    {
        const float SitFps = 4f, SitAfter = 1.5f;
        SpriteRenderer _sr, _shadow;
        string _species;
        Sprite[,] _frames;
        int _row = 2;
        bool _left;
        float _walk, _idle, _faceLock;
        float _atkT = -1f;
        int _atkMark;
        Vector2 _atkDir, _fxGoal;
        SpriteRenderer _fx;
        int _fxRow;
        float _fxT;
        bool _fxOn;

        public void Show(string species, Vector2 world)
        {
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();
            if (GetComponent<SpriteSorter>() == null) gameObject.AddComponent<SpriteSorter>();
            _species = species;
            _frames = Pets.FramesFor(species);
            var s = Pets.Portrait(species);
            _sr.sprite = s != null ? s : ProceduralSprites.Pixel;
            _sr.color = Color.white;
            transform.localScale = s != null ? new Vector3(0.8f, 0.8f, 1f) : new Vector3(0.45f, 0.45f, 1f);
            transform.position = world;
            gameObject.name = "Pet_" + species;
            _row = 2;
            _idle = 1f;
            _atkT = -1f;
            _fxOn = false;
            if (_fx != null) _fx.enabled = false;
            EnsureShadow();
        }

        void EnsureShadow()
        {
            if (_species != "dragon")
            {
                if (_shadow != null) _shadow.enabled = false;
                return;
            }
            if (_shadow == null)
            {
                var go = new GameObject("GroundShadow");
                go.transform.SetParent(transform, false);
                go.transform.localPosition = new Vector3(0f, 0.06f, 0f);
                _shadow = go.AddComponent<SpriteRenderer>();
                _shadow.sprite = FarmArt.Cell(FarmArt.Grid("Pet_GroundShadow", 1, 1, 12, 5, new Vector2(0.5f, 0.5f)), 0, 0);
                _shadow.color = Color.white;
            }
            _shadow.enabled = true;
        }

        public bool Attacking => _atkT >= 0f;

        public void Aim(Vector2 world) { _fxGoal = world; }

        public void BeginAttack(Vector2 dir)
        {
            _atkDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
            _atkT = 0f;
            _atkMark = 0;
            bool side = Mathf.Abs(_atkDir.y) <= Mathf.Abs(_atkDir.x) * 1.4f;
            if (side) _left = _atkDir.x < 0f;
            _row = side ? 2 : (_atkDir.y > 0f ? 1 : 0);
            PaintAttack(0);
        }

        /// <summary>2 = the hit frame (apply damage once). 1 = the clip finished.</summary>
        public int AdvanceAttack(float dt)
        {
            if (_atkT < 0f) return 0;
            _atkT += dt;
            int col = _atkT < 0.10f ? 0 : _atkT < 0.18f ? 1 : _atkT < 0.28f ? 2 : _atkT < 0.40f ? 3 : -1;
            if (col < 0)
            {
                _atkT = -1f;
                return 1;
            }
            PaintAttack(col);
            int hit = 0;
            if (col >= 2 && _atkMark < 2)
            {
                _atkMark = 2;
                hit = 2;
                StartFx(_species == "dragon" ? -1 : 0);
            }
            if (col >= 3 && _species == "wolf" && _atkMark < 3)
            {
                _atkMark = 3;
                StartFx(2);
            }
            return hit;
        }

        void PaintAttack(int col)
        {
            var s = FarmArt.Cell(Pets.AttackFrames(_species), _row, col);
            if (s != null && _sr != null) _sr.sprite = s;
            if (_sr != null) _sr.flipX = _row == 2 && _left;
            if (_shadow != null) _shadow.enabled = _species == "dragon";
        }

        void StartFx(int row)
        {
            if (row == 0 && _species != "dog" && _species != "wolf")
                row = _species == "rabbit" ? 3 : 1;
            _fxRow = row;
            _fxT = 0f;
            _fxOn = true;
            if (_fx == null)
            {
                var go = new GameObject("PetHitFx");
                go.transform.SetParent(transform, false);
                _fx = go.AddComponent<SpriteRenderer>();
            }
            _fx.enabled = true;
            TickFx(0f);
        }

        void TickFx(float dt)
        {
            if (!_fxOn || _fx == null) return;
            _fxT += dt;
            if (_fxRow < 0)
            {
                float u = Mathf.Clamp01(_fxT / 0.22f);
                Vector2 mouth = (Vector2)transform.position + new Vector2(_left ? -0.4f : 0.4f, 0.5f);
                _fx.transform.position = Vector2.Lerp(mouth, _fxGoal, u);
                int cell = u < 1f ? (int)(_fxT / 0.08f) % 2 : 2 + Mathf.Min(2, (int)((_fxT - 0.22f) / 0.07f));
                _fx.sprite = FarmArt.Cell(FarmArt.Grid("Pet_Dragon_Fire_Sheet", 5, 1, 24, 24, new Vector2(0.5f, 0.5f)), 0, cell);
                _fx.flipX = _left;
                if (_fxT >= 0.43f) _fxOn = _fx.enabled = false;
                return;
            }
            int frame = (int)(_fxT / 0.06f);
            if (frame > 2) { _fxOn = _fx.enabled = false; return; }
            _fx.sprite = FarmArt.Cell(FarmArt.Grid("Pet_Hit_FX_Sheet", 3, 4, 16, 16, new Vector2(0.5f, 0.5f)), _fxRow, frame);
            _fx.flipX = false;
            _fx.transform.position = _fxRow == 3 ? transform.position : (Vector3)_fxGoal;
        }

        public void Animate(Vector2 moved, float speed, float heroSpeed, Vector2 look)
        {
            TickFx(Time.deltaTime);
            if (_atkT >= 0f) return;
            var frames = _frames;
            if (_sr == null || frames == null) return;
            Vector2 face = moved.sqrMagnitude > 1e-8f ? moved : look;
            bool walking = moved.sqrMagnitude > 1e-8f;
            if (walking) _idle = 0f;
            else _idle += Time.deltaTime;
            bool showWalk = walking || _idle < 0.12f;
            int col;
            if (showWalk && face.sqrMagnitude > 1e-8f)
            {
                bool side = Mathf.Abs(face.y) <= Mathf.Abs(face.x) * 1.4f;
                if (side) _left = face.x < 0f;
                if (Time.time >= _faceLock)
                {
                    int row = side ? 2 : (face.y > 0f ? 1 : 0);
                    bool axis = (row == 2) != (_row == 2);
                    if (axis) _faceLock = Time.time + 0.15f;
                    _row = row;
                }
                float fps = heroSpeed > 0.01f ? 8f * speed / heroSpeed : 8f;
                if (walking) _walk += Time.deltaTime * Mathf.Clamp(fps, 6f, 12f);
                col = (int)_walk % 4;
            }
            else
            {
                if (_idle >= SitAfter) { _row = 0; col = 4 + (int)(_idle * SitFps) % 2; }
                else col = 0;
            }
            var s = FarmArt.Cell(frames, _row, col);
            if (s != null) _sr.sprite = s;
            _sr.flipX = _row == 2 && _left;
            if (_shadow != null) _shadow.enabled = _species == "dragon" && col < 4;
        }

        void LateUpdate()
        {
            if (_shadow != null && _sr != null) _shadow.sortingOrder = _sr.sortingOrder - 4;
            if (_fx != null && _sr != null) _fx.sortingOrder = _sr.sortingOrder + 2;
            Pets.Tick(Time.deltaTime);
        }
    }

    /// <summary>Landscape pet button, hatch confirm / animation / result, and the 3-column collection.</summary>
    public class PetHud : MonoBehaviour
    {
        // Idle, then (TiltL, Idle, TiltR, Idle) x2, Crack1, one wobble, Crack2, Burst. Open is held after.
        static readonly int[] HatchFrame = { 0, 1, 0, 2, 0, 1, 0, 2, 0, 3, 1, 0, 2, 0, 4, 5 };
        static readonly float[] HatchDur = { 0.08f, 0.08f, 0.08f, 0.08f, 0.08f, 0.08f, 0.08f, 0.08f, 0.08f, 0.4f, 0.08f, 0.08f, 0.08f, 0.08f, 0.4f, 0.15f };
        static PetHud _one;
        Canvas _canvas;
        Button _paw;
        RectTransform _bookRoot, _askRoot, _sacRoot, _hatchRoot;
        UiWindow _book, _ask, _sac, _hatch;
        string _sel = "", _newId = "", _newSpecies = "";
        bool _busy, _revealed;
        float _hatchStart;
        Image _preview, _egg;
        float _anim;

        public static void Ensure()
        {
            if (_one != null) return;
            var go = new GameObject("PetHud");
            _one = go.AddComponent<PetHud>();
        }

        public static void SetShown(bool on)
        {
            if (_one == null) return;
            if (_one._paw != null) _one._paw.gameObject.SetActive(on);
            if (!on) _one.CloseBook();
        }

        void Awake()
        {
            _canvas = UiKit.Canvas("PetCanvas", 35);
            transform.SetParent(_canvas.transform, false);
            _paw = UiKit.Button(_canvas.transform, "PetButton", "Secondary", "PET", new Vector2(28f, 16f), OpenBook);
            UiKit.Place(_paw, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -52f), new Vector2(28f, 16f));
            UiKit.Reach(_paw);
        }

        void Update()
        {
            _anim += Time.unscaledDeltaTime;
            if (_preview != null && _preview.isActiveAndEnabled) AnimatePreview();
            if (_hatchRoot == null || !_hatchRoot.gameObject.activeSelf || _revealed || _egg == null) return;
            float elapsed = Time.unscaledTime - _hatchStart;
            int frame = 6;
            float at = 0f;
            for (int i = 0; i < HatchDur.Length; i++)
            {
                if (elapsed < at + HatchDur[i]) { frame = HatchFrame[i]; break; }
                at += HatchDur[i];
            }
            var egg = Pets.EggFrame(frame);
            if (egg != null) _egg.sprite = egg;
            if (frame == 6) Reveal();
        }

        void AnimatePreview()
        {
            int i = Pets.IndexOf(_sel);
            if (i < 0) return;
            string species = Pets.SpeciesAt(i);
            bool walking = Pets.IsActive(_sel) && Pets.FollowAt(i);
            int row = walking ? 2 : 0;
            int col = walking ? (int)(_anim * 8f) % 4 : 4 + (int)(_anim * 4f) % 2;
            var s = FarmArt.Cell(Pets.FramesFor(species), row, col);
            if (s != null) _preview.sprite = s;
        }

        // ---------- modal helpers ----------

        RectTransform Modal(string name, string title, Vector2 size, out UiWindow win, System.Action onClose)
        {
            var root = UiKit.Rect(_canvas.transform, name);
            UiKit.Fill(root);
            UiKit.Dim(root);
            win = UiKit.Window(root, name + "Win", title, size, onClose);
            return root;
        }

        static Text Line(Transform parent, string text, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size, Color color, TextAnchor align = TextAnchor.MiddleLeft)
        {
            var t = UiKit.Label(parent, "Line", text, 8, color, align, false);
            UiKit.Place(t, anchor, pivot, pos, size);
            return t;
        }

        static RectTransform Section(Transform parent, string name, float x, float w)
        {
            var img = UiKit.Panel(parent, name, "UI_Panel_Parchment");
            img.gameObject.AddComponent<UiLayout>().Both(UiLayout.Spec.Stretch(new Vector2(0f, 0f), new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                new Vector2(x, 0f), new Vector2(w, 0f)));
            return img.rectTransform;
        }

        // ---------- collection ----------

        void OpenBook() => ShowBook(Pets.ActiveId);

        void CloseBook()
        {
            if (_bookRoot != null) _bookRoot.gameObject.SetActive(false);
            _preview = null;
        }

        void ShowBook(string select)
        {
            if (_bookRoot == null)
                _bookRoot = Modal("PetBook", "PETS", new Vector2(440f, 300f), out _book, CloseBook);
            _bookRoot.gameObject.SetActive(true);
            _book.Show(true);
            _bookRoot.SetAsLastSibling();
            _sel = select;
            if (Pets.IndexOf(_sel) < 0) _sel = Pets.Count > 0 ? (Pets.IndexOf(Pets.ActiveId) >= 0 ? Pets.ActiveId : Pets.IdAt(0)) : "";
            Refresh();
        }

        void Refresh()
        {
            if (_book == null) return;
            _book.Clear();
            _preview = null;
            var body = _book.body;
            int sel = Pets.IndexOf(_sel);

            // LEFT: owned pets, one row per instance, scrolls.
            var list = Section(body, "Collection", 0f, 136f);
            Line(list, "COLLECTION  " + Pets.Count, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -4f), new Vector2(120f, 12f), Pal.TextDark);
            const float rowH = 26f;
            var content = ScrollList(list, Pets.Count * (rowH + 2f) + 4f);
            if (Pets.Count == 0)
                Line(content, "No pets yet.", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(120f, 12f), Pal.WoodM, TextAnchor.MiddleCenter);
            for (int i = 0; i < Pets.Count; i++)
                PetRow(content, i, 2f + i * (rowH + 2f), rowH);

            // CENTER: big preview.
            var mid = Section(body, "Preview", 140f, 132f);
            if (sel >= 0)
            {
                string species = Pets.SpeciesAt(sel);
                var frame = UiKit.Panel(mid, "Frame", "UI_Equip_Slot_Selected");
                UiKit.Place(frame, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(112f, 104f));
                _preview = UiKit.Rect(frame.transform, "Sprite").gameObject.AddComponent<Image>();
                _preview.raycastTarget = false;
                _preview.preserveAspect = true;
                _preview.sprite = Pets.Portrait(species);
                _preview.color = Pets.Tint(species);
                UiKit.Place(_preview, UiKit.Center, UiKit.Center, new Vector2(0f, -4f), new Vector2(96f, 80f));
                Line(mid, Pets.SpeciesName(species).ToUpperInvariant(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -122f), new Vector2(124f, 12f), Pal.TextDark, TextAnchor.MiddleCenter);
                bool on = Pets.IsActive(_sel);
                Line(mid, on ? "ACTIVE" : "INACTIVE", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -138f), new Vector2(124f, 12f), on ? Pal.GreenL : Pal.WoodM, TextAnchor.MiddleCenter);
            }
            else Line(mid, "Hatch a Mystery\nPet Egg from\nyour BAG.", UiKit.Center, UiKit.Center, Vector2.zero, new Vector2(120f, 40f), Pal.WoodM, TextAnchor.MiddleCenter);

            // RIGHT: details and commands. Only valid commands are enabled.
            var right = Section(body, "Details", 276f, 140f);
            bool has = sel >= 0;
            bool active = has && Pets.IsActive(_sel);
            bool follow = has && Pets.FollowAt(sel);
            string sp = has ? Pets.SpeciesName(Pets.SpeciesAt(sel)) : "-";
            Line(right, "SPECIES  " + sp.ToUpperInvariant(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -6f), new Vector2(128f, 12f), Pal.TextDark);
            Line(right, "OWNER    " + (has ? (Pets.OwnerAt(sel) == Pets.LocalOwner ? "YOU" : Pets.OwnerAt(sel).ToUpperInvariant()) : "-"), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -20f), new Vector2(128f, 12f), Pal.TextDark);
            Line(right, "STATUS   " + (!has ? "-" : active ? "ACTIVE" : "INACTIVE"), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -34f), new Vector2(128f, 12f), active ? Pal.GreenL : Pal.TextDark);
            Line(right, "COMMAND  " + (!active ? "-" : follow ? "FOLLOW" : "STAY"), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -48f), new Vector2(128f, 12f), Pal.TextDark);

            string id = _sel;
            Cmd(right, "SUMMON", "Primary", 0, has && !active, () => Pets.Summon(id));
            Cmd(right, "RECALL", "Secondary", 1, active, () => Pets.Recall());
            Cmd(right, "FOLLOW", "Secondary", 2, active && !follow, () => Pets.Follow());
            Cmd(right, "STAY", "Secondary", 3, active && follow, () => Pets.Stay());
            Cmd(right, "CALL", "Secondary", 4, active && !follow, () => Pets.Call());
            string block = has ? Pets.CanSacrifice(id) : "No pet";
            Cmd(right, "SACRIFICE", "Secondary", 5, has && block.Length == 0, () => { ConfirmSacrifice(id); return true; });
            if (has && block.Length > 0)
                Line(right, block.ToUpperInvariant(), new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(8f, -62f), new Vector2(128f, 12f), Pal.RedL);
        }

        void ConfirmSacrifice(string id)
        {
            if (_sacRoot == null)
                _sacRoot = Modal("SacrificeAsk", "SACRIFICE PET?", new Vector2(240f, 120f), out _sac, () => _sacRoot.gameObject.SetActive(false));
            _sac.Clear();
            _sac.SetTitle("SACRIFICE PET?");
            Line(_sac.body, "Sacrifice this Common pet\nfor 1 egg shard?", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(210f, 28f), Pal.TextW, TextAnchor.MiddleCenter);
            var yes = UiKit.Button(_sac.body, "Yes", "Primary", "SACRIFICE", new Vector2(90f, 18f), () =>
            {
                Pets.Sacrifice(id);
                _sacRoot.gameObject.SetActive(false);
                if (Pets.IndexOf(id) < 0) _sel = "";
                Refresh();
            });
            UiKit.Place(yes, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(10f, 6f), new Vector2(90f, 18f));
            var no = UiKit.Button(_sac.body, "No", "Secondary", "CANCEL", new Vector2(80f, 18f), () => _sacRoot.gameObject.SetActive(false));
            UiKit.Place(no, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-10f, 6f), new Vector2(80f, 18f));
            _sacRoot.gameObject.SetActive(true);
            _sac.Show(true);
            _sacRoot.SetAsLastSibling();
        }

        void Cmd(Transform parent, string label, string kind, int slot, bool ok, System.Func<bool> run)
        {
            var b = UiKit.Button(parent, label, ok ? kind : "Secondary", label, new Vector2(112f, 18f), () =>
            {
                run();
                Refresh();
            });
            b.interactable = ok;
            UiKit.Place(b, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f + (4 - slot) * 21f), new Vector2(112f, 18f));
        }

        void PetRow(RectTransform content, int i, float top, float h)
        {
            string id = Pets.IdAt(i);
            string species = Pets.SpeciesAt(i);
            bool on = Pets.IsActive(id);
            var rt = UiKit.Rect(content, "Pet_" + id);
            rt.gameObject.AddComponent<UiLayout>().Both(UiLayout.Spec.Stretch(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -top), new Vector2(-8f, h)));
            var bg = UiKit.Row(rt, "Bg", id == _sel ? "Selected" : "", Vector2.zero);
            UiKit.Fill(bg);
            var hit = rt.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = hit;
            btn.onClick.AddListener(() => { _sel = id; Refresh(); });
            var icon = UiKit.Rect(rt, "Icon").gameObject.AddComponent<Image>();
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            icon.sprite = Pets.Portrait(species);
            icon.color = Pets.Tint(species);
            UiKit.Place(icon, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(3f, 0f), new Vector2(24f, 20f));
            string num = id.StartsWith("p") ? " #" + id.Substring(1) : "";
            Line(rt, Pets.SpeciesName(species).ToUpperInvariant() + num, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -2f), new Vector2(96f, 11f), Pal.GoldL);
            string state = !on ? "INACTIVE" : Pets.FollowAt(i) ? "ACTIVE - FOLLOW" : "ACTIVE - STAY";
            Line(rt, state, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(30f, 2f), new Vector2(96f, 10f), on ? Pal.GreenL : Pal.TextMute);
        }

        /// <summary>Clipped vertical list under the 18 px section head (same build as the village service lists).</summary>
        static RectTransform ScrollList(RectTransform section, float contentPx, float headPx = 18f)
        {
            var view = UiKit.Rect(section, "View");
            view.gameObject.AddComponent<UiLayout>().Both(UiLayout.Spec.Stretch(Vector2.zero, Vector2.one, new Vector2(0.5f, 1f),
                new Vector2(0f, -headPx), new Vector2(0f, -headPx - 4f)));
            view.gameObject.AddComponent<RectMask2D>();
            var hit = view.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var content = UiKit.Rect(view, "Content");
            content.gameObject.AddComponent<UiLayout>().Both(UiLayout.Spec.Stretch(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, contentPx)));
            var scroll = view.gameObject.AddComponent<ScrollRect>();
            scroll.content = content;
            scroll.viewport = view;
            scroll.horizontal = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 24f;
            return content;
        }

        // ---------- hatch ----------

        public static void ConfirmHatch()
        {
            Ensure();
            if (_one._busy) return;
            string why = Pets.CanHatch();
            if (why.Length > 0) { WorldNavigator.Instance?.Flash(why); return; }
            _one.Ask();
        }

        void Ask()
        {
            if (_askRoot == null)
            {
                _askRoot = Modal("HatchAsk", "HATCH PET EGG?", new Vector2(240f, 120f), out _ask, () => _askRoot.gameObject.SetActive(false));
                Line(_ask.body, "Hatch one Mystery Pet Egg\nto meet a new companion.", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -6f), new Vector2(210f, 28f), Pal.TextW, TextAnchor.MiddleCenter);
                var yes = UiKit.Button(_ask.body, "Hatch", "Primary", "HATCH", new Vector2(80f, 18f), DoHatch);
                UiKit.Place(yes, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(10f, 6f), new Vector2(80f, 18f));
                var no = UiKit.Button(_ask.body, "Cancel", "Secondary", "CANCEL", new Vector2(80f, 18f), () => _askRoot.gameObject.SetActive(false));
                UiKit.Place(no, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-10f, 6f), new Vector2(80f, 18f));
            }
            _askRoot.gameObject.SetActive(true);
            _ask.Show(true);
            _askRoot.SetAsLastSibling();
        }

        void DoHatch()
        {
            if (_busy) return; // double click or a second confirm: one transaction only
            if (_askRoot != null) _askRoot.gameObject.SetActive(false);
            _busy = true;
            if (!Pets.Hatch(out string id, out string species)) { _busy = false; return; }
            WorldNavigator.Instance?.CloseBag();
            _newId = id;
            _newSpecies = species;
            _revealed = false;
            _hatchStart = Time.unscaledTime;
            if (_hatchRoot == null) _hatchRoot = Modal("HatchResult", "HATCHING...", new Vector2(260f, 200f), out _hatch, EndHatch);
            _hatchRoot.gameObject.SetActive(true);
            _hatch.Show(true);
            _hatchRoot.SetAsLastSibling();
            _hatch.SetTitle("HATCHING...");
            _hatch.Clear();
            _egg = UiKit.Rect(_hatch.body, "Egg").gameObject.AddComponent<Image>();
            _egg.raycastTarget = false;
            _egg.preserveAspect = true;
            _egg.sprite = Pets.EggFrame(0) ?? UiKit.Get(UiKit.ItemIcon(Pets.EggId));
            _egg.rectTransform.localRotation = Quaternion.identity;
            UiKit.Place(_egg, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, -78f), new Vector2(48f, 64f));
            var skip = UiKit.Button(_hatch.body, "Skip", "Secondary", "SKIP", new Vector2(64f, 18f), Reveal);
            UiKit.Place(skip, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(64f, 18f));
        }

        /// <summary>Shows the result. Visual only: the pet was saved before the animation started.</summary>
        void Reveal()
        {
            if (_revealed || _hatch == null) return;
            _revealed = true;
            var open = Pets.EggFrame(6);
            if (_egg != null && open != null) _egg.sprite = open;
            var skip = _hatch.body.Find("Skip");
            if (skip != null) Destroy(skip.gameObject);
            _hatch.SetTitle("NEW COMPANION");
            var img = UiKit.Rect(_hatch.body, "Pet").gameObject.AddComponent<Image>();
            img.raycastTarget = false;
            img.preserveAspect = true;
            img.sprite = Pets.Portrait(_newSpecies);
            img.color = Pets.Tint(_newSpecies);
            UiKit.Place(img, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, -30f), new Vector2(24f, 24f));
            Line(_hatch.body, Pets.SpeciesName(_newSpecies).ToUpperInvariant(), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -92f), new Vector2(220f, 12f), Pal.GoldL, TextAnchor.MiddleCenter);
            bool on = Pets.IsActive(_newId);
            Line(_hatch.body, on ? "OWNED - NOW WITH YOU" : "OWNED - RESTING IN COLLECTION", new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -106f), new Vector2(230f, 12f), on ? Pal.GreenL : Pal.TextW, TextAnchor.MiddleCenter);
            var view = UiKit.Button(_hatch.body, "View", "Primary", "VIEW COLLECTION", new Vector2(130f, 18f), () =>
            {
                string id = _newId;
                EndHatch();
                ShowBook(id);
            });
            UiKit.Place(view, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(130f, 18f));
        }

        void EndHatch()
        {
            _revealed = true;
            _egg = null;
            if (_hatchRoot != null) _hatchRoot.gameObject.SetActive(false);
            _busy = false;
        }
    }
}
