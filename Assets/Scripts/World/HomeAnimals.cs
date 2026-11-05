using System.Globalization;
using UnityEngine;
using UnityEngine.UI;

namespace GameplayPrototype
{
    public enum FarmAnimalKind { Chicken, Duck, Dog }

    /// <summary>
    /// Housing-zone livestock (FARM_LIVESTOCK_HANDOFF §2): chickens round the coop, ducks in the pond and on
    /// its shore, one dog that roams and follows the hero. Positions are zone-local units (1 unit = 16 px).
    /// Eggs use active gameplay time. Each animal holds up to 5 waiting eggs
    /// (gw.v1.home.egg.{id}); gw.v1.home.lay.{id} is the next lay time while not full.
    /// First egg 30–45 s, then 90 s ± 15 s. Gold label: countdown, or "Đầy" at 5.
    /// </summary>
    public static class HomeAnimals
    {
        public static bool Enabled = true;
        public const float LaySeconds = 90f;
        public const float FirstLayMin = 30f;
        public const float FirstLayMax = 45f;
        public const float LayJitter = 15f;
        public const int MaxEggs = 5;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => FarmEgg.ResetClock();

        public static double FirstLayAt() => GameplayClock.Now + Random.Range(FirstLayMin, FirstLayMax);
        public static double NextLayAt() => GameplayClock.Now + LaySeconds + Random.Range(-LayJitter, LayJitter);
        public static double MaxLayAhead => LaySeconds + LayJitter + 60d;

        public static Vector2 PondCentre => HomeLayout.Pond;
        public static readonly Vector2 PondRadii = new Vector2(2.4f, 1.3f);
        public static Vector2 GardenCentre => HomeLayout.Garden;
        public static readonly Vector2 GardenRadii = new Vector2(2.9f, 2.2f);

        public static string LayKey(string id) => WorldCatalog.SavePrefix + "home.lay." + id;
        public static string EggKey(string id) => WorldCatalog.SavePrefix + "home.egg." + id;
        public static int EggCount(string id) => Mathf.Clamp(PlayerPrefs.GetInt(EggKey(id), 0), 0, MaxEggs);

        public static void Spawn(Transform root)
        {
            if (!Enabled || root == null || root.Find("Animals") != null) return;
            var host = new GameObject("Animals").transform;
            host.SetParent(root, false);
            if (CoopAnchor(out Vector2 coop))
            {
                Make(host, "chicken_white", FarmAnimalKind.Chicken, FarmArt.ChickenWhite, coop + new Vector2(-1.3f, -0.8f), coop + new Vector2(-0.5f, -1.05f), "egg");
                Make(host, "chicken_brown", FarmAnimalKind.Chicken, FarmArt.ChickenBrown, coop + new Vector2(1.2f, -0.75f), coop + new Vector2(0.55f, -1.05f), "egg");
            }
            if (!HomeLayout.Has("pond") && HomeLand.HasWater)
            {
                Make(host, "duck_white_0", FarmAnimalKind.Duck, FarmArt.DuckWhite, HomeLand.RandomWater(true), HomeLand.RandomWater(false), "duck_egg");
                Make(host, "duck_white_1", FarmAnimalKind.Duck, FarmArt.DuckWhite, HomeLand.RandomWater(true), HomeLand.RandomWater(false), "duck_egg");
                Make(host, "duck_mallard", FarmAnimalKind.Duck, FarmArt.DuckMallard, HomeLand.RandomWater(true), HomeLand.RandomWater(false), "duck_egg");
            }
            if (HomeLayout.Has("pond"))
            {
                var p = HomeLayout.PondShift;
                Make(host, "duck_white_0", FarmAnimalKind.Duck, FarmArt.DuckWhite, new Vector2(-5.6f, -3.6f) + p, new Vector2(-1.8f, -3.7f) + p, "duck_egg");
                Make(host, "duck_white_1", FarmAnimalKind.Duck, FarmArt.DuckWhite, new Vector2(-3.6f, -4.4f) + p, new Vector2(-4.9f, -2.1f) + p, "duck_egg");
                Make(host, "duck_mallard", FarmAnimalKind.Duck, FarmArt.DuckMallard, new Vector2(-4.6f, -3.2f) + p, new Vector2(-6.8f, -5.0f) + p, "duck_egg");
            }
            if (!HomeArea.WildStart)
                Make(host, "dog", FarmAnimalKind.Dog, FarmArt.Dog, new Vector2(-1.5f, -2.5f), Vector2.zero, null);
        }

        /// <summary>Drops the animals and spawns them again, so a new coop gets its chickens.</summary>
        public static void Refresh()
        {
            var root = GameObject.Find("HomeDistrict");
            if (root == null) return;
            var old = root.transform.Find("Animals");
            if (old != null)
            {
                old.name = "Animals_old";
                Object.Destroy(old.gameObject);
            }
            Spawn(root.transform);
        }

        /// <summary>Moves chickens and their eggs by the same shift as the coop.</summary>
        public static void FollowCoop(Vector2 delta)
        {
            if (delta.sqrMagnitude < 0.0001f) return;
            var root = GameObject.Find("HomeDistrict");
            var host = root != null ? root.transform.Find("Animals") : null;
            if (host == null) return;
            for (int i = 0; i < host.childCount; i++)
            {
                var child = host.GetChild(i);
                var animal = child.GetComponent<FarmAnimal>();
                if (animal != null && animal.Kind == FarmAnimalKind.Chicken) animal.Shift(delta);
                else if (child.name.StartsWith("Egg_chicken")) child.localPosition += (Vector3)delta;
            }
        }

        /// <summary>The built coop when it is finished, otherwise the placed coop piece.</summary>
        public static bool CoopAnchor(out Vector2 at)
        {
            if (BuildMode.TryCoop(out at)) return true;
            if (HomeLayout.Has("coop")) { at = HomeLayout.Coop; return true; }
            at = default;
            return false;
        }

        static void Make(Transform host, string id, FarmAnimalKind kind, Sprite[,] frames, Vector2 at, Vector2 nest, string eggItem)
        {
            if (frames == null) return;
            var go = new GameObject("Animal_" + id);
            go.transform.SetParent(host, false);
            go.transform.localPosition = at;
            go.AddComponent<SpriteRenderer>().sprite = frames[0, 0];
            go.AddComponent<SpriteSorter>();
            var a = go.AddComponent<FarmAnimal>();
            a.Init(id, kind, frames);
            if (kind == FarmAnimalKind.Dog)
            {
                var talk = go.AddComponent<WorldInteractable>();
                talk.DisplayName = "Pet";
                talk.Line = "Good dog!";
                talk.Radius = 1f;
                talk.OnInteract = a.Pet;
            }
            if (string.IsNullOrEmpty(eggItem)) return;
            var egg = new GameObject("Egg_" + id);
            egg.transform.SetParent(host, false);
            egg.transform.localPosition = SnapToLand(nest, true);
            egg.AddComponent<FarmEgg>().Init(id, eggItem, kind == FarmAnimalKind.Duck);
        }

        // ---------- areas (zone-local) ----------

        public static bool InWalls(Vector2 p, float margin = 0.25f) =>
            p.x > -7.5f + margin && p.x < 7.5f - margin && p.y > -7f + margin && p.y < 8f - margin;

        /// <summary>Normalised ellipse distance: below 1 is inside.</summary>
        public static float Ellipse(Vector2 p, Vector2 centre, Vector2 radii, float grow = 0f)
        {
            float dx = (p.x - centre.x) / (radii.x + grow), dy = (p.y - centre.y) / (radii.y + grow);
            return dx * dx + dy * dy;
        }

        public static bool InPond(Vector2 p, float grow = 0f)
        {
            if (HomeLayout.Has("pond") && Ellipse(p, PondCentre, PondRadii, grow) < 1f) return true;
            return grow <= 0f ? HomeLand.InWater(p) : HomeLand.InDugWater(p, grow);
        }

        /// <summary>Open water only. A dug shore cell (one water corner) is bank, so the duck walks there.</summary>
        public static bool InOpenWater(Vector2 p)
        {
            if (HomeLand.HasWater) return HomeLand.InDugWater(p, 0f);
            return HomeLayout.Has("pond") && Ellipse(p, PondCentre, PondRadii) < 1f;
        }

        /// <summary>Nearest dry, open land. Eggs stay off garden plots; ducks may stay in water.</summary>
        public static Vector2 SnapToLand(Vector2 p, bool egg = false)
        {
            if (OkLand(p, egg)) return p;
            Vector2 best = p;
            float bestD = float.MaxValue;
            for (int r = 1; r <= 20; r++)
            {
                float dist = r * 0.35f;
                for (int a = 0; a < 16; a++)
                {
                    float rad = a * (Mathf.PI * 2f / 16f);
                    Vector2 q = p + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * dist;
                    if (!OkLand(q, egg)) continue;
                    float d = (q - p).sqrMagnitude;
                    if (d >= bestD) continue;
                    bestD = d;
                    best = q;
                }
                if (bestD < float.MaxValue) break;
            }
            return best;
        }

        static bool OkLand(Vector2 q, bool egg)
        {
            if (InHouse(q) || !HomeArea.Open(q)) return false;
            if (!egg)
                return !InPond(q) && !HomeLand.InWater(q);
            if (InPond(q, 1f) || HomeLand.InDugWater(q, 1.1f) || HomeLand.InWater(q)) return false;
            if (InGarden(q, 0.25f) || InCoop(q)) return false;
            return HomeLand.Kind(HomeLand.CellOf(q)) != HomeLand.Plot;
        }
        public static bool InGarden(Vector2 p, float grow = 0f) =>
            HomeLayout.Has("garden") && Ellipse(p, GardenCentre, GardenRadii, grow) < 1f;
        public static bool OnShore(Vector2 p) => !InPond(p, 0.3f) && InPond(p, 1.2f);

        /// <summary>The house body (built) or the foundation; animals keep off both.</summary>
        public static bool InHouse(Vector2 p) =>
            Mathf.Abs(p.x) < 3.5f && p.y > 2.6f && p.y < (Housing.HasHouse ? 8.5f : 3.8f);

        public static bool InCoop(Vector2 p)
        {
            if (!CoopAnchor(out Vector2 c)) return false;
            return Mathf.Abs(p.x - c.x) < 1.05f && p.y > c.y - 0.35f && p.y < c.y + 0.7f;
        }

        public static bool ByCoop(Vector2 p)
        {
            if (!CoopAnchor(out Vector2 c)) return false;
            float dx = p.x - c.x, dy = p.y - c.y;
            return dx * dx / (2.8f * 2.8f) + dy * dy / (1.8f * 1.8f) < 1f;
        }

        static readonly Collider2D[] _hits = new Collider2D[8];
        static ContactFilter2D _solid;
        static bool _solidSet;

        /// <summary>True when a non-trigger collider other than the hero (and the pond for ducks) covers the spot.</summary>
        public static bool Blocked(Vector2 world, float radius, bool ignorePond)
        {
            if (!_solidSet)
            {
                _solid = ContactFilter2D.noFilter;
                _solid.useTriggers = false;
                _solidSet = true;
            }
            int n = Physics2D.OverlapCircle(world, radius, _solid, _hits);
            for (int i = 0; i < n; i++)
            {
                var c = _hits[i];
                if (c == null || c.isTrigger || c.CompareTag("Player")) continue;
                if (ignorePond && c.name == "PondEdge") continue;
                return true;
            }
            return false;
        }

        public static double LayAt(string id)
        {
            string raw = PlayerPrefs.GetString(LayKey(id), "");
            return double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double v) ? v : -1d;
        }

        public static string Stamp(double t) => t.ToString("R", CultureInfo.InvariantCulture);

        /// <summary>Turns elapsed lay times into waiting eggs, up to <see cref="MaxEggs"/>.</summary>
        public static void CatchUp(string id)
        {
            if (string.IsNullOrEmpty(id)) return;
            GameplayClock.EnsureLoaded();
            int stock = EggCount(id);
            double at = LayAt(id);
            if (stock >= MaxEggs)
            {
                if (at >= 0d) WorldTx.Begin().Delete(LayKey(id)).Commit();
                return;
            }
            if (at < 0d)
            {
                WorldTx.Begin().SetString(LayKey(id), Stamp(FirstLayAt())).Commit();
                return;
            }
            if (at > GameplayClock.Now + MaxLayAhead)
            {
                WorldTx.Begin().SetString(LayKey(id), Stamp(FirstLayAt())).Commit();
                return;
            }
            int nextStock = stock;
            double nextAt = at;
            int guard = 0;
            while (nextStock < MaxEggs && nextAt >= 0d && GameplayClock.Now >= nextAt && guard++ < MaxEggs)
            {
                nextStock++;
                nextAt = nextStock < MaxEggs ? nextAt + LaySeconds : -1d;
            }
            if (nextStock == stock && System.Math.Abs(nextAt - at) < 0.001d) return;
            var tx = WorldTx.Begin().SetInt(EggKey(id), nextStock);
            if (nextAt < 0d) tx.Delete(LayKey(id));
            else tx.SetString(LayKey(id), Stamp(nextAt));
            tx.Commit();
        }
    }

    /// <summary>Wander/follow movement and frame animation for one farm animal.</summary>
    public class FarmAnimal : MonoBehaviour
    {
        enum Mode { Idle, Walk, Special }

        string _id;
        FarmAnimalKind _kind;
        Sprite[,] _frames;
        SpriteRenderer _sr;
        SpriteSorter _sorter;
        Transform _hero;

        Mode _mode = Mode.Idle;
        Vector2 _target;
        float _modeUntil;
        int _row = 2;          // 0 down, 1 up, 2 side
        bool _left;
        float _animT;
        bool _placed;

        // dog. HomeLand.CellOf maps 1 local unit to 1 cell, so one ô is 1 world unit.
        bool _following, _catchUp, _stuck;
        float _stillSince, _lastZz, _greetCooldown, _sitUntil;
        float _farSince = -1f, _repathAt;
        Vector2 _heroLast, _lead;
        int _slide;
        float _faceLock;
        float _idleSince;
        float _cluckAt;
        // Heel sits behind the hero, clear of the 1.5-unit sprite. Exit is far from enter so a running hero keeps the dog.
        const float DogFollowEnter = 4f, DogFollowExit = 7f, DogFollowGrace = 1.5f;
        const float DogHeel = 1.1f, DogHeelSide = 0.35f, DogHeelStop = 1.5f, DogResettle = 0.6f;
        const float DogCatchEnter = 3f, DogCatchExit = 1.6f;
        const float DogWanderSpeed = 1.6f, DogFollowSpeed = 4.5f, DogCatchSpeed = 5.1f;
        const float DogAccel = 14f, DogDecel = 20f;
        float _curSpeed, _heroSpeed, _walkPhase;

        const float WalkFps = 8f, SpecialFps = 4f;
        const int SwimOrder = -8955;

        public FarmAnimalKind Kind => _kind;

        /// <summary>Keeps a chicken beside the coop after the coop is moved.</summary>
        public void Shift(Vector2 delta)
        {
            transform.localPosition += (Vector3)delta;
            _target += delta;
        }

        public void Init(string id, FarmAnimalKind kind, Sprite[,] frames)
        {
            _id = id;
            _kind = kind;
            _frames = frames;
            _sr = GetComponent<SpriteRenderer>();
            _sorter = GetComponent<SpriteSorter>();
            _modeUntil = Time.time + Random.Range(0.2f, 1.5f);
            _animT = Random.value;
            _idleSince = Time.time;
        }

        Vector2 Local => transform.localPosition;

        float Speed
        {
            get
            {
                switch (_kind)
                {
                    case FarmAnimalKind.Chicken: return 0.8f;
                    case FarmAnimalKind.Duck: return HomeAnimals.InOpenWater(Local) ? 0.45f : 0.6f;
                    default:
                        if (!_following || _hero == null) return DogWanderSpeed;
                        // Match the hero's real pace (haste, water) instead of a fixed number.
                        return _catchUp ? Mathf.Max(DogCatchSpeed, _heroSpeed * 1.25f) : Mathf.Max(DogFollowSpeed, _heroSpeed * 1.05f);
                }
            }
        }

        bool Swimming => _kind == FarmAnimalKind.Duck && HomeAnimals.InOpenWater(Local);

        /// <summary>Spot rules per animal (zone-local).</summary>
        bool Allowed(Vector2 p)
        {
            if (_kind == FarmAnimalKind.Chicken)
            {
                if (!HomeAnimals.ByCoop(p) || !HomeArea.Open(p)) return false;
                if (HomeAnimals.InGarden(p, 0.2f) || HomeAnimals.InPond(p, 1.2f) || HomeAnimals.InCoop(p) || HomeAnimals.InHouse(p)) return false;
                return !HomeAnimals.Blocked(transform.parent.TransformPoint(p), 0.18f, false);
            }
            if (!HomeAnimals.InWalls(p)) return false;
            switch (_kind)
            {
                case FarmAnimalKind.Duck:
                    if (!HomeAnimals.InPond(p, 1.2f)) return false;
                    if (HomeAnimals.InGarden(p, 0.2f)) return false;
                    if (HomeAnimals.InPond(p)) return true;               // water: the pond collider does not apply
                    break;
                default:
                    return DogFootOk(p);
            }
            return !HomeAnimals.Blocked(transform.parent.TransformPoint(p), 0.18f, _kind == FarmAnimalKind.Duck);
        }

        /// <summary>Dog sprite is 1.5 units wide at the feet. Keep that span, not only the pivot, off the water.</summary>
        static bool DogOnWater(Vector2 p)
        {
            const float pad = 0.75f;
            if (HomeAnimals.InPond(p, pad) || HomeLand.InWater(p)) return true;
            return HomeLand.InWater(p + new Vector2(pad, 0.15f)) || HomeLand.InWater(p + new Vector2(-pad, 0.15f));
        }

        /// <summary>Center of the dog. Pond boxes are ignored; water is the cell test, otherwise the bank freezes it.</summary>
        bool DogFootOk(Vector2 p)
        {
            if (!HomeAnimals.InWalls(p)) return false;
            if (HomeAnimals.InGarden(p, 0.15f) || HomeAnimals.InHouse(p) || HomeAnimals.InCoop(p)) return false;
            if (HomeAnimals.InPond(p, 0.15f) || HomeLand.InWater(p)) return false;
            return transform.parent == null || !HomeAnimals.Blocked(transform.parent.TransformPoint(p), 0.15f, true);
        }

        bool SpotClear(Vector2 q)
        {
            if (!HomeAnimals.InWalls(q)) return false;
            if (HomeLand.InWater(q) || HomeAnimals.InPond(q, 0.15f)) return false;
            if (HomeAnimals.InGarden(q, 0.2f) || HomeAnimals.InHouse(q) || HomeAnimals.InCoop(q)) return false;
            return transform.parent == null || !HomeAnimals.Blocked(transform.parent.TransformPoint(q), 0.12f, false);
        }

        /// <summary>Nearest dry point to <paramref name="want"/> that the dog can walk to. Same point when none exists.</summary>
        Vector2 ReachableNear(Vector2 want)
        {
            Vector2 here = Local;
            if (Allowed(want) && PathClear(here, want)) return want;
            Vector2 best = here;
            float bestD = float.MaxValue;
            for (int r = 1; r <= 10; r++)
            {
                float dist = r * 0.35f;
                for (int a = 0; a < 12; a++)
                {
                    float rad = a * (Mathf.PI * 2f / 12f);
                    Vector2 q = want + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * dist;
                    if (!Allowed(q) || !PathClear(here, q)) continue;
                    float d = (q - want).sqrMagnitude;
                    if (d >= bestD) continue;
                    bestD = d;
                    best = q;
                }
                if (bestD < float.MaxValue) return best;
            }
            return here;
        }

        /// <summary>Nearest legal footing, ignoring the wet path. Used only to walk a dog out of the pond.</summary>
        Vector2 NearestDry(Vector2 from)
        {
            if (Allowed(from)) return from;
            Vector2 best = from;
            float bestD = float.MaxValue;
            for (int r = 1; r <= 24; r++)
            {
                float dist = r * 0.4f;
                for (int a = 0; a < 16; a++)
                {
                    float rad = a * (Mathf.PI * 2f / 16f);
                    Vector2 q = from + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * dist;
                    if (!Allowed(q)) continue;
                    float d = (q - from).sqrMagnitude;
                    if (d >= bestD) continue;
                    bestD = d;
                    best = q;
                }
                if (bestD < float.MaxValue) return best;
            }
            return from;
        }

        /// <summary>A step out of the water: closer to dry land, still blocked by the house, garden, coop and yard wall.</summary>
        bool EscapeStep(Vector2 next, Vector2 goal)
        {
            if (!HomeAnimals.InWalls(next)) return false;
            if (HomeAnimals.InGarden(next, 0.2f) || HomeAnimals.InHouse(next) || HomeAnimals.InCoop(next)) return false;
            if (transform.parent != null && HomeAnimals.Blocked(transform.parent.TransformPoint(next), 0.12f, true)) return false;
            return (next - goal).sqrMagnitude + 0.0004f < (Local - goal).sqrMagnitude;
        }

        bool TryStepOut(Vector2 here, Vector2 goal, float step, out Vector2 next)
        {
            Vector2 dir = goal - here;
            if (dir.sqrMagnitude < 0.0001f) { next = here; return false; }
            dir.Normalize();
            next = here;
            float best = float.MaxValue;
            bool found = false;
            for (int i = 0; i < 7; i++)
            {
                float ang = (i / 2) * (i % 2 == 0 ? 1f : -1f) * 0.45f;
                float c = Mathf.Cos(ang), s = Mathf.Sin(ang);
                Vector2 n = here + new Vector2(dir.x * c - dir.y * s, dir.x * s + dir.y * c) * step;
                if (!EscapeStep(n, goal) && !Allowed(n)) continue;
                float d = (n - goal).sqrMagnitude;
                if (d >= best) continue;
                best = d;
                next = n;
                found = true;
            }
            return found;
        }

        Vector2 PickTarget()
        {
            Vector2 here = Local;
            for (int i = 0; i < 14; i++)
            {
                Vector2 p;
                switch (_kind)
                {
                    case FarmAnimalKind.Chicken:
                        if (!HomeAnimals.CoopAnchor(out Vector2 coop)) coop = here;
                        p = coop + new Vector2(Random.Range(-2.2f, 2.2f), Random.Range(-1.4f, 0.9f));
                        break;
                    case FarmAnimalKind.Duck:
                    {
                        // Mostly stay in the current medium; sometimes cross the rim.
                        bool water = Swimming ? Random.value < 0.7f : Random.value < 0.45f;
                        float ang = Random.value * Mathf.PI * 2f;
                        float r = water ? Mathf.Sqrt(Random.value) * 0.85f : 1f;
                        float grow = water ? 0f : Random.Range(0.4f, 1.1f);
                        p = HomeLayout.Has("pond")
                            ? HomeAnimals.PondCentre + new Vector2(Mathf.Cos(ang) * (HomeAnimals.PondRadii.x + grow) * r, Mathf.Sin(ang) * (HomeAnimals.PondRadii.y + grow) * r)
                            : HomeLand.RandomWater(water, grow);
                        break;
                    }
                    default:
                        p = here + Random.insideUnitCircle * 5f;
                        break;
                }
                if (Allowed(p) && PathClear(here, p)) return p;
            }
            return here;
        }

        bool PathClear(Vector2 a, Vector2 b)
        {
            int steps = Mathf.CeilToInt(Vector2.Distance(a, b) / 0.4f);
            for (int i = 1; i < steps; i++)
                if (!Allowed(Vector2.Lerp(a, b, i / (float)steps))) return false;
            return true;
        }

        void Update()
        {
            if (_frames == null) return;
            if (!_placed)
            {
                _placed = true;
                if (_kind != FarmAnimalKind.Dog && !Allowed(Local)) transform.localPosition = PickTarget();
                else if (_kind == FarmAnimalKind.Dog && !Allowed(Local))
                {
                    _stuck = true;
                    _repathAt = Time.time + 0.2f;
                    _idleSince = Time.time;
                }
            }
            float now = Time.time;
            if (_kind == FarmAnimalKind.Dog) DogThink(now);
            else if (now >= _modeUntil && _mode != Mode.Walk) Wander(now);

            Vector2 moved = Vector2.zero;
            if (_mode == Mode.Walk)
            {
                Vector2 here = Local;
                Vector2 to = _target - here;
                float step = (_kind == FarmAnimalKind.Dog ? DogStepSpeed(to.magnitude) : Speed) * Time.deltaTime;
                if (to.magnitude <= step + 0.01f)
                {
                    if (Allowed(_target)) transform.localPosition = _target;
                    if (!(_kind == FarmAnimalKind.Dog && _following)) Arrive(now);
                }
                else
                {
                    Vector2 next = here + to.normalized * step;
                    if (Allowed(next)) { transform.localPosition = next; moved = next - here; _slide = 0; }
                    else if (_kind == FarmAnimalKind.Dog && FeetInWater(here) && TryStepOut(here, _target, step, out Vector2 escape))
                    {
                        transform.localPosition = escape;
                        moved = escape - here;
                    }
                    else if (_kind == FarmAnimalKind.Dog && TrySlide(here, to, step, out Vector2 slide))
                    {
                        transform.localPosition = slide;
                        moved = slide - here;
                    }
                    else if (!(_kind == FarmAnimalKind.Dog && _following)) Arrive(now);   // a wandering dog gives up a blocked spot
                }
            }
            if (_kind == FarmAnimalKind.Dog && moved.sqrMagnitude <= 0f) _curSpeed = 0f;
            Face(moved);
            Animate(moved.sqrMagnitude > 0f);
            SortForWater();
        }

        void Wander(float now)
        {
            _target = PickTarget();
            if ((_target - Local).sqrMagnitude < 0.04f) { Rest(now); return; }
            _mode = Mode.Walk;
        }

        void Arrive(float now)
        {
            if (_kind == FarmAnimalKind.Chicken && Random.value < 0.5f)
            {
                _mode = Mode.Special;                                   // peck
                _modeUntil = now + Random.Range(1f, 2f);
                if (now >= _cluckAt)
                {
                    _cluckAt = now + 6f;
                    OverheadChat.Say(transform, "CỤC TÁC", 1.8f);
                    OverheadChat.PlayCluck(transform.position);
                }
                return;
            }
            if (_kind == FarmAnimalKind.Duck && Random.value < 0.5f && now >= _cluckAt)
            {
                _cluckAt = now + 6f;
                OverheadChat.Say(transform, "CẠP CẠP", 1.8f);
                OverheadChat.PlayQuack(transform.position);
            }
            Rest(now);
        }

        void Rest(float now)
        {
            _mode = Mode.Idle;
            _modeUntil = now + Random.Range(1f, 3f);
            if (_kind == FarmAnimalKind.Chicken) _row = 2;
        }

        // ---------- dog ----------

        /// <summary>Dog speed this frame: ramps up and down, and slows while closing on the target instead of snapping.</summary>
        float DogStepSpeed(float dist)
        {
            float want = Mathf.Min(Speed, (_following ? 1.2f : 0.6f) + dist * (_following ? 3f : 2f));
            float rate = want > _curSpeed ? DogAccel : DogDecel;
            _curSpeed = Mathf.MoveTowards(_curSpeed, want, rate * Time.deltaTime);
            return Mathf.Max(_curSpeed, 0.3f);
        }

        void LeaveWater(float now)
        {
            _stuck = true;
            _idleSince = now;
            if (_mode == Mode.Walk && now < _repathAt && (_target - Local).sqrMagnitude > 0.04f) return;
            Vector2 shore = NearestDry(Local);
            if ((shore - Local).sqrMagnitude < 0.04f) { HoldDog(now); return; }
            _target = shore;
            _mode = Mode.Walk;
            _repathAt = now + 0.5f;
        }

        void HoldDog(float now)
        {
            _stuck = true;
            _mode = Mode.Idle;
            _repathAt = now + 0.45f;
            _idleSince = now;
        }

        void DogThink(float now)
        {
            if (_hero == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                _hero = go != null ? go.transform : null;
                if (_hero != null) _heroLast = _hero.position;
            }
            float d = _hero != null && _hero.gameObject.activeInHierarchy ? Vector2.Distance(transform.position, _hero.position) : 99f;
            Vector2 heroDelta = _hero != null ? (Vector2)_hero.position - _heroLast : Vector2.zero;
            if (_hero != null)
            {
                float dt = Time.deltaTime;
                if (dt > 0f && heroDelta.sqrMagnitude < 9f)          // skip teleports / scene loads
                    _heroSpeed = Mathf.Lerp(_heroSpeed, Mathf.Min(heroDelta.magnitude / dt, 12f), 1f - Mathf.Exp(-6f * dt));
                if (heroDelta.sqrMagnitude > 0.0004f)
                {
                    _stillSince = now;
                    Vector2 dir = heroDelta.normalized;
                    _lead = _lead.sqrMagnitude < 0.01f ? dir : Vector2.Lerp(_lead, dir, 0.2f).normalized;
                }
                _heroLast = _hero.position;
            }

            Vector2 heroLocal = _hero != null ? (Vector2)transform.parent.InverseTransformPoint(_hero.position) : Vector2.zero;
            bool heroHome = _hero != null && HomeAnimals.InWalls(heroLocal, -1f);
            if (!heroHome)
            {
                if (_following) { EndFollow(); _mode = Mode.Idle; _modeUntil = now; }
            }
            else if (!_following)
            {
                _farSince = -1f;
                if (d < DogFollowEnter) BeginFollow(now);
            }
            else if (d > DogFollowExit)
            {
                if (_farSince < 0f) _farSince = now;
                else if (now - _farSince >= DogFollowGrace) { EndFollow(); _mode = Mode.Idle; _modeUntil = now; }
            }
            else _farSince = -1f;

            if (FeetInWater(Local)) { LeaveWater(now); IdleClock(now); return; }

            if (now < _sitUntil) { _mode = Mode.Special; _row = 0; IdleClock(now); return; }

            if (_following) FollowMove(now, d, heroLocal);
            else WanderDog(now);
            IdleClock(now);
        }

        void BeginFollow(float now)
        {
            _following = true;
            _catchUp = false;
            _stuck = false;
            _farSince = -1f;
            if (now < _greetCooldown) return;
            FarmEmote.Show(transform, 1, _sr);
            _greetCooldown = now + 12f;
        }

        void EndFollow()
        {
            _following = false;
            _catchUp = false;
            _farSince = -1f;
        }

        void FollowMove(float now, float d, Vector2 heroLocal)
        {
            if (d > DogCatchEnter) _catchUp = true;
            else if (d <= DogCatchExit) _catchUp = false;

            bool heroStill = now - _stillSince > 0.5f;
            if (heroStill && d <= DogHeelStop)
            {
                _stuck = false;
                _catchUp = false;
                _slide = 0;
                _mode = Mode.Special;
                FacePlayer(heroLocal);
                return;
            }

            Vector2 heel = HeelPoint(heroLocal);
            if (!Allowed(heel)) heel = NearestDry(heel);
            // Once settled, wait until the heel drifts a bit before trotting again (no stop-start jitter).
            float settle = _mode == Mode.Walk ? 0.02f : DogResettle * DogResettle;
            if ((heel - Local).sqrMagnitude < settle)
            {
                _mode = Mode.Idle;
                FacePlayer(heroLocal);
                return;
            }
            _stuck = false;
            _target = heel;
            _mode = Mode.Walk;
        }

        Vector2 HeelPoint(Vector2 hero)
        {
            if (_lead.sqrMagnitude > 0.01f)
            {
                // Behind and a little to the side the dog is already on, so it does not run through the hero on a turn.
                Vector2 perp = new Vector2(-_lead.y, _lead.x);
                float sign = Vector2.Dot(Local - hero, perp) >= 0f ? 1f : -1f;
                return hero - _lead * DogHeel + perp * (DogHeelSide * sign);
            }
            Vector2 side = Local - hero;
            if (side.sqrMagnitude < 0.04f) side = Vector2.down;
            return hero + side.normalized * DogHeel;
        }

        static bool FeetInWater(Vector2 p) => HomeAnimals.InPond(p) || HomeLand.InWater(p);

        bool TrySlide(Vector2 here, Vector2 to, float step, out Vector2 next)
        {
            Vector2 dir = to.sqrMagnitude > 0.0001f ? to.normalized : Vector2.right;
            Vector2 perp = new Vector2(-dir.y, dir.x);
            int side = _slide == 0 ? 1 : _slide;
            Vector2 a = here + (dir + perp * side).normalized * step;
            Vector2 b = here + (dir - perp * side).normalized * step;
            if (Allowed(a)) { next = a; _slide = side; return true; }
            if (Allowed(b)) { next = b; _slide = -side; return true; }
            next = here;
            return false;
        }

        void FacePlayer(Vector2 heroLocal)
        {
            Vector2 v = heroLocal - Local;
            if (v.sqrMagnitude < 1e-4f) return;
            int row;
            bool left = _left;
            if (Mathf.Abs(v.x) > Mathf.Abs(v.y) * 0.8f) { row = 2; left = v.x < 0f; }
            else row = v.y > 0f ? 1 : 0;
            if (row == _row && left == _left) return;
            _row = row;
            _left = left;
        }

        void WanderDog(float now)
        {
            if (_mode == Mode.Walk) return;
            if (now < _modeUntil || now < _repathAt) return;
            if (Random.value < 0.35f)
            {
                _stuck = false;
                _mode = Mode.Special;
                _row = 0;
                _modeUntil = now + Random.Range(2f, 5f);
                return;
            }
            _target = PickTarget();
            if ((_target - Local).sqrMagnitude < 0.04f) { HoldDog(now); return; }
            _stuck = false;
            _mode = Mode.Walk;
        }

        void IdleClock(float now)
        {
            if (_mode == Mode.Walk || _following || _stuck) { _idleSince = now; return; }
            if (now - _idleSince > 20f && now - _lastZz > 6f)
            {
                FarmEmote.Show(transform, 2, _sr);                      // zz
                _lastZz = now;
            }
        }

        public void Pet()
        {
            FarmEmote.Show(transform, 0, _sr);                          // heart
            _sitUntil = Time.time + 1.5f;
            _idleSince = Time.time;
        }

        // ---------- frames ----------

        void Face(Vector2 moved)
        {
            if (moved.sqrMagnitude <= 0f) return;
            FaceToward(moved);
        }

        void FaceToward(Vector2 v)
        {
            if (v.sqrMagnitude < 1e-6f) return;
            if (_kind == FarmAnimalKind.Dog && Time.time < _faceLock) return;
            int row;
            bool left = _left;
            bool side = _kind == FarmAnimalKind.Dog
                ? Mathf.Abs(v.y) <= Mathf.Abs(v.x) * 1.4f
                : Mathf.Abs(v.x) > Mathf.Abs(v.y) * 0.8f;
            if (side) { row = 2; left = v.x < 0f; }
            else row = v.y > 0f ? 1 : 0;
            if (row == _row && left == _left) return;
            _row = row;
            _left = left;
            if (_kind == FarmAnimalKind.Dog) _faceLock = Time.time + 0.25f;
        }

        void Animate(bool walking)
        {
            _animT += Time.deltaTime;
            int col;
            if (Swimming)
                col = 4 + (int)(_animT * SpecialFps) % 2;
            else if (walking && _kind == FarmAnimalKind.Dog)
            {
                // Leg cycle follows real speed so the dog does not skate when running.
                _walkPhase += Time.deltaTime * WalkFps * Mathf.Clamp(_curSpeed / 2.4f, 0.7f, 2f);
                col = (int)_walkPhase % 4;
            }
            else if (walking)
                col = (int)(_animT * WalkFps) % 4;
            else if (_mode == Mode.Special)
                col = 4 + (int)(_animT * SpecialFps) % 2;
            else
                col = 0;
            var s = FarmArt.Cell(_frames, _row, col);
            if (s != null) _sr.sprite = s;
            _sr.flipX = _row == 2 && _left;
        }

        void SortForWater()
        {
            if (_kind != FarmAnimalKind.Duck || _sorter == null) return;
            bool swim = Swimming;
            if (_sorter.enabled == swim) _sorter.enabled = !swim;
            if (swim) _sr.sortingOrder = SwimOrder;
        }
    }

    /// <summary>A laid egg near the coop or on the shore. Shows once the lay time passes; E picks it up.</summary>
    public class FarmEgg : MonoBehaviour
    {
        string _animalId, _item;
        SpriteRenderer _sr;
        WorldInteractable _talk;
        float _next;

        public void Init(string animalId, string item, bool duck)
        {
            _animalId = animalId;
            _item = item;
            _sr = gameObject.AddComponent<SpriteRenderer>();
            var icon = FarmArt.Item(item);
            _sr.sprite = icon != null ? Sprite.Create(icon.texture, icon.rect, new Vector2(0.5f, 0.15f), 16f * 1.6f, 0, SpriteMeshType.FullRect) : null;
            gameObject.AddComponent<SpriteSorter>();
            _talk = gameObject.AddComponent<WorldInteractable>();
            _talk.DisplayName = "Pick up egg";
            _talk.Radius = 1f;
            _talk.OnInteract = PickUp;
            GameplayClock.EnsureLoaded();
            transform.localPosition = HomeAnimals.SnapToLand(transform.localPosition, true);
            HomeAnimals.CatchUp(animalId);
            Refresh();
        }

        public bool Ready => HomeAnimals.EggCount(_animalId) > 0;
        public bool Full => HomeAnimals.EggCount(_animalId) >= HomeAnimals.MaxEggs;
        public float SecondsLeft => Full ? 0f : Mathf.Max(0f, (float)(HomeAnimals.LayAt(_animalId) - GameplayClock.Now));

        static readonly Color Gold = new Color32(0xF2, 0xD8, 0x4A, 0xFF);
        static Canvas _hud;
        Text _clock;

        public static void ResetClock()
        {
            if (_hud != null)
            {
                Object.Destroy(_hud.gameObject);
                _hud = null;
            }
        }

        void OnDestroy()
        {
            if (_clock != null) Destroy(_clock.gameObject);
        }

        void OnDisable() => HideLayClock();

        void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.5f;
            Refresh();
        }

        void LateUpdate() => TickLayClock();

        void Refresh()
        {
            HomeAnimals.CatchUp(_animalId);
            bool on = Ready;
            if (_sr.enabled != on) _sr.enabled = on;
            if (_talk.enabled != on) _talk.enabled = on;
        }

        void TickLayClock()
        {
            if (BuildMode.IsOpen)
            {
                HideLayClock();
                return;
            }
            ShowLayClock();
        }

        Transform ClockHost()
        {
            var animal = transform.parent != null ? transform.parent.Find("Animal_" + _animalId) : null;
            return animal != null ? animal : transform;
        }

        static void EnsureHud()
        {
            if (_hud != null) return;
            _hud = UiKit.Canvas("EggLayHud", 130);
        }

        void ShowLayClock()
        {
            EnsureHud();
            string text = Full ? Loc.T("Egg full") : Mathf.Max(1, Mathf.CeilToInt(SecondsLeft)) + "s";
            if (_clock == null)
            {
                var go = new GameObject("EggLayClock_" + _animalId);
                go.transform.SetParent(_hud.transform, false);
                var rt = go.AddComponent<RectTransform>();
                rt.pivot = new Vector2(0.5f, 0f);
                rt.sizeDelta = new Vector2(88f, 28f);
                _clock = go.AddComponent<Text>();
                _clock.font = UiKit.Font;
                _clock.fontSize = 22;
                _clock.alignment = TextAnchor.MiddleCenter;
                _clock.color = Gold;
                _clock.raycastTarget = false;
                _clock.horizontalOverflow = HorizontalWrapMode.Overflow;
                var outline = go.AddComponent<Outline>();
                outline.effectColor = Color.black;
                outline.effectDistance = new Vector2(1f, -1f);
            }
            if (!_clock.gameObject.activeSelf) _clock.gameObject.SetActive(true);
            if (_clock.text != text) _clock.text = text;
            var host = ClockHost();
            var sr = host.GetComponent<SpriteRenderer>();
            float top = sr != null && sr.sprite != null ? (sr.bounds.max.y - host.position.y) + 0.25f : 1.1f;
            var cam = Camera.main;
            if (cam == null) { HideLayClock(); return; }
            Vector2 screen = cam.WorldToScreenPoint(host.position + new Vector3(0f, top, 0f));
            if (screen.x < -100f) { HideLayClock(); return; }
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                (RectTransform)_hud.transform, screen, null, out var local);
            _clock.rectTransform.anchoredPosition = local;
        }

        void HideLayClock()
        {
            if (_clock != null && _clock.gameObject.activeSelf) _clock.gameObject.SetActive(false);
        }

        void PickUp()
        {
            HomeAnimals.CatchUp(_animalId);
            int stock = HomeAnimals.EggCount(_animalId);
            if (stock <= 0) return;
            int have = WorldSave.GetQty(_item);
            int cap = WorldContent.StackLimit(_item);
            if (cap > 0 && have > cap)
            {
                WorldTx.Begin().SetInt(WorldSave.KeyItem(_item), cap).Commit();
                have = cap;
            }
            if (cap > 0 && have + 1 > cap) { WorldServices.Fail(Deny.BagFull, "Bag full"); return; }
            bool feather = Random.value < 0.15f;
            int feathers = WorldSave.GetQty("feather");
            int featherCap = WorldContent.StackLimit("feather");
            if (feather && featherCap > 0 && feathers + 1 > featherCap) feather = false;
            int left = stock - 1;
            string item = _item;
            var tx = WorldTx.Begin()
                .SetInt(WorldSave.KeyItem(item), have + 1)
                .SetInt(HomeAnimals.EggKey(_animalId), left)
                .After(() => { WorldEvents.RaiseItemGained(item, 1); RewardBanner.Show(item, 1); });
            if (left < HomeAnimals.MaxEggs && HomeAnimals.LayAt(_animalId) < 0d)
                tx.SetString(HomeAnimals.LayKey(_animalId), HomeAnimals.Stamp(HomeAnimals.NextLayAt()));
            if (feather)
                tx.SetInt(WorldSave.KeyItem("feather"), feathers + 1)
                    .After(() => { WorldEvents.RaiseItemGained("feather", 1); RewardBanner.Show("feather", 1); });
            tx.Commit();
            Refresh();
        }
    }

    /// <summary>8x8 emote over an animal's head: shows 1.2 s and rises 4 px. 0 heart, 1 note, 2 zz, 3 "!".</summary>
    public class FarmEmote : MonoBehaviour
    {
        SpriteRenderer _sr, _owner;
        float _born;
        Vector3 _base;

        public static void Show(Transform animal, int kind, SpriteRenderer owner)
        {
            var s = FarmArt.Emote(kind);
            if (s == null || animal == null) return;
            var old = animal.Find("Emote");
            if (old != null) Destroy(old.gameObject);
            var go = new GameObject("Emote");
            go.transform.SetParent(animal, false);
            float top = owner != null && owner.sprite != null ? owner.sprite.rect.height / 16f : 1f;
            var e = go.AddComponent<FarmEmote>();
            e._base = new Vector3(0f, top + 0.05f, 0f);
            go.transform.localPosition = e._base;
            e._sr = go.AddComponent<SpriteRenderer>();
            e._sr.sprite = s;
            e._owner = owner;
            e._born = Time.time;
        }

        void LateUpdate()
        {
            float t = (Time.time - _born) / 1.2f;
            if (t >= 1f) { Destroy(gameObject); return; }
            transform.localPosition = _base + new Vector3(0f, Mathf.Round(t * 4f) / 16f, 0f);
            if (_owner != null) _sr.sortingOrder = _owner.sortingOrder + 1;
            _sr.flipX = false;
        }
    }
}
