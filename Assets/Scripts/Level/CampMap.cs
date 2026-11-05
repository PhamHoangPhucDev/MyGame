using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Goblin Forest Camp from Camp_Layout: one baked ground, then y-sorted props.
    /// Pixel origin is the top-left. world = (x / 16, -y / 16).
    /// </summary>
    public class CampMap : MonoBehaviour
    {
        const int W = 1600;
        const int H = 1120;
        const int NX = 100;
        const int NY = 70;
        const float Ppu = 16f;

        enum Kind : byte { Grass, Dark, Forest, Dirt, Water, Plateau, Cliff }

        struct Ell { public double x, y, rx, ry; public bool isl; }
        struct Path { public List<Vector2> pts; public float w; }
        struct Bridge { public int x0, x1, y0, y1; public bool hor; }
        struct Art
        {
            public Texture2D tex;
            public Color32[] pix;
            public int w, h, frames, fw, fh;
            public float pivX, pivY, fps;
            public Sprite[] framesSprites;
        }

        static CampMap _live;
        static readonly List<UnityEngine.Object> Owned = new List<UnityEngine.Object>();

        readonly Dictionary<string, Art> _art = new Dictionary<string, Art>();
        readonly List<Ell> _ell = new List<Ell>();
        readonly List<Path> _paths = new List<Path>();
        readonly List<Vector2> _stream = new List<Vector2>();
        readonly List<Ell> _ponds = new List<Ell>();
        readonly List<Bridge> _bridges = new List<Bridge>();
        readonly List<Vector2> _fires = new List<Vector2>();
        readonly int[] _rim = new int[W];
        readonly int[] _base = new int[W];
        readonly byte[] _kind = new byte[NX * NY];
        Color32[] _buf;
        int _props, _missing, _water, _dirt, _dark, _forest;
        string _missingNames = "";
        Vector2 _portal = new Vector2(760, 1092);
        Vector2 _secret = new Vector2(1452, 262);
        int _fallX = 1296;

        public static void Show(Transform hero)
        {
            Hide();
            var root = new GameObject("CampMap");
            var map = root.AddComponent<CampMap>();
            map.Build(true);
            _live = map;
            HideOldCamp();
            var cam = UnityEngine.Object.FindFirstObjectByType<CameraFollow>();
            if (cam != null) cam.SetBounds(new Vector2(0f, -H / Ppu), new Vector2(W / Ppu, 0f));
            if (hero != null)
            {
                hero.position = new Vector3(_live._portal.x / Ppu, -_live._portal.y / Ppu, hero.position.z);
                var rb = hero.GetComponent<Rigidbody2D>();
                if (rb != null) rb.linearVelocity = Vector2.zero;
            }
        }

        public static void Hide()
        {
            if (_live != null)
            {
                Destroy(_live.gameObject);
                _live = null;
            }
            ClearOwned(false);
        }

        public static string BakeCheck()
        {
            var root = new GameObject("CampMapCheck");
            var map = root.AddComponent<CampMap>();
            map.Build(false);
            string report = map.Report();
            DestroyImmediate(root);
            ClearOwned(true);
            return report;
        }

        public static string Shoot(string dir)
        {
            System.IO.Directory.CreateDirectory(dir);
            var root = new GameObject("CampShot");
            var map = root.AddComponent<CampMap>();
            map.Build(false);
            string report = map.Report();
            Shot(dir, "01-portal", 47.5f, -68.25f);
            Shot(dir, "02-yard", 47.5f, -36.2f);
            Shot(dir, "03-waterfall", 81f, -36.5f);
            Shot(dir, "04-gather", 20f, -17.8f);
            DestroyImmediate(root);
            ClearOwned(true);
            return report;
        }

        static void Shot(string dir, string name, float x, float y)
        {
            var go = new GameObject("shotcam");
            var cam = go.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 8f;
            cam.transform.position = new Vector3(x, y, -10f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.07f, 0.08f, 1f);
            cam.cullingMask = ~0;
            var rt = new RenderTexture(1280, 720, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var img = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            img.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0);
            img.Apply(false);
            System.IO.File.WriteAllBytes(System.IO.Path.Combine(dir, name + ".png"), img.EncodeToPNG());
            RenderTexture.active = null;
            cam.targetTexture = null;
            DestroyImmediate(img);
            DestroyImmediate(rt);
            DestroyImmediate(go);
        }

        static void ClearOwned(bool immediate)
        {
            for (int i = 0; i < Owned.Count; i++)
            {
                if (Owned[i] == null) continue;
                if (immediate) DestroyImmediate(Owned[i]);
                else Destroy(Owned[i]);
            }
            Owned.Clear();
        }

        string Report()
        {
            double h0 = Hash01(0, 0, 1);
            return "h0=" + h0.ToString("F6", CultureInfo.InvariantCulture)
                + " water=" + _water + " dirt=" + _dirt + " dark=" + _dark + " forest=" + _forest
                + " props=" + _props + " mobs=" + _mobs.Count + " missing=" + _missing
                + (string.IsNullOrEmpty(_missingNames) ? "" : " [" + _missingNames + "]");
        }

        void Build(bool live)
        {
            var clock = System.Diagnostics.Stopwatch.StartNew();
            LoadArt();
            Parse(Resources.Load<TextAsset>("World/OutdoorMaps/Camp_Layout")?.text ?? "");
            BakeGround();
            PlaceProps();
            PlaceExtras();
            if (live) Debug.Log("CampMap " + Report() + " ms=" + clock.ElapsedMilliseconds);
        }

        void LoadArt()
        {
            var csv = Resources.Load<TextAsset>("World/OutdoorMaps/Camp_ArtKit_Manifest");
            if (csv == null) return;
            var lines = csv.text.Split('\n');
            for (int i = 1; i < lines.Length; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0) continue;
                var p = line.Split(',');
                if (p.Length < 8) continue;
                string file = p[0].Trim();
                string name = System.IO.Path.GetFileNameWithoutExtension(file);
                string res = "World/OutdoorMaps/" + file.Replace('\\', '/').Replace(".png", "");
                var tex = Resources.Load<Texture2D>(res);
                if (tex == null)
                {
                    var sprite = Resources.Load<Sprite>(res);
                    if (sprite != null) tex = sprite.texture;
                }
                var art = new Art
                {
                    tex = tex,
                    w = tex != null ? tex.width : 0,
                    h = tex != null ? tex.height : 0,
                    frames = Int(p[3], 1),
                    fw = Int(p[4], 16),
                    fh = Int(p[5], 16),
                    pivX = Float(p[6], 0.5f),
                    pivY = Float(p[7], 0.5f),
                    fps = Fps(line)
                };
                if (tex != null && tex.isReadable)
                {
                    var up = tex.GetPixels32();
                    art.pix = new Color32[up.Length];
                    for (int y = 0; y < art.h; y++)
                        Array.Copy(up, y * art.w, art.pix, (art.h - 1 - y) * art.w, art.w);
                }
                _art[name] = art;
            }
        }

        void Parse(string json)
        {
            if (string.IsNullOrEmpty(json)) return;
            var js = new Js(json);
            if (js.Seek("clears")) ReadEllipses(js, _ell);
            if (js.Seek("patches")) ReadEllipses(js, _ell);
            if (js.Seek("paths")) ReadPaths(js);
            if (js.Seek("stream")) _stream.AddRange(ReadPairs(js));
            if (js.Seek("ponds")) ReadEllipses(js, _ponds);
            if (js.Seek("waterfallX")) _fallX = (int)js.Num();
            if (js.Seek("bridges")) ReadBridges(js);
            if (js.Seek("portal"))
            {
                js.Eat('[');
                float px = js.Num();
                js.Eat(',');
                _portal = new Vector2(px, js.Num());
            }
            js = new Js(json);
            if (js.Seek("secretChest"))
            {
                js.Eat('[');
                float sx = js.Num();
                js.Eat(',');
                _secret = new Vector2(sx, js.Num());
            }
            js = new Js(json);
            if (js.Seek("fires"))
            {
                js.Eat('[');
                while (js.Next() == '{')
                {
                    float x = 0, y = 0;
                    js.Eat('{');
                    while (js.Next() != '}' && js.Next() != '\0')
                    {
                        string key = js.Str();
                        js.Eat(':');
                        if (key == "x") x = js.Num();
                        else if (key == "y") y = js.Num();
                        else js.SkipValue();
                        if (js.Next() == ',') js.Eat(',');
                    }
                    js.Eat('}');
                    _fires.Add(new Vector2(x, y));
                    if (js.Next() == ',') js.Eat(',');
                }
            }
            foreach (Match m in Regex.Matches(json, "\"sprite\"\\s*:\\s*\"([^\"]+)\"\\s*,\\s*\"x\"\\s*:\\s*(-?\\d+)\\s*,\\s*\"y\"\\s*:\\s*(-?\\d+)"))
            {
                _itemNames.Add(m.Groups[1].Value);
                _items.Add(new Vector3(
                    float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                    float.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture), 0));
            }
            foreach (Match m in Regex.Matches(json, "\"mob\"\\s*:\\s*\"([^\"]+)\"\\s*,\\s*\"sheet\"\\s*:\\s*\"[^\"]+\"\\s*,\\s*\"x\"\\s*:\\s*(-?\\d+)\\s*,\\s*\"y\"\\s*:\\s*(-?\\d+)\\s*,\\s*\"lv\"\\s*:\\s*(\\d+)\\s*,\\s*\"boss\"\\s*:\\s*(true|false)\\s*,\\s*\"home\"\\s*:\\s*\\[[\\s\\S]*?\\]\\s*,\\s*\"radius\"\\s*:\\s*(\\d+)"))
            {
                _mobs.Add(new Mob
                {
                    name = m.Groups[1].Value,
                    x = float.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture),
                    y = float.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture),
                    lv = int.Parse(m.Groups[4].Value, CultureInfo.InvariantCulture),
                    boss = m.Groups[5].Value == "true",
                    leash = float.Parse(m.Groups[6].Value, CultureInfo.InvariantCulture)
                });
            }
        }

        readonly List<Vector3> _items = new List<Vector3>();
        readonly List<string> _itemNames = new List<string>();
        readonly List<Mob> _mobs = new List<Mob>();

        struct Mob
        {
            public string name;
            public float x, y, leash;
            public int lv;
            public bool boss;
        }

        public static int SpawnFoes(EnemySpawner spawner)
        {
            if (_live == null || spawner == null) return 0;
            int n = 0;
            for (int i = 0; i < _live._mobs.Count; i++)
            {
                var m = _live._mobs[i];
                if (m.name.IndexOf("Archer", StringComparison.Ordinal) >= 0)
                    _live.Place("Archer_Platform", m.x, m.y + 30f, -1);
                var pos = new Vector2(m.x / Ppu, -m.y / Ppu);
                var go = spawner.SpawnCampMob(m.name, pos, m.lv, m.boss, m.leash);
                if (go == null) continue;
                _live.HookFoe(go, m);
                n++;
            }
            return n;
        }

        public static void BeginTraining()
        {
            if (_live != null) _live._training = true;
        }

        readonly List<Mob> _return = new List<Mob>();
        readonly List<float> _returnAt = new List<float>();
        bool _training;

        void HookFoe(GameObject go, Mob mob)
        {
            var health = go.GetComponent<Health>();
            if (health == null) return;
            int exp = mob.boss ? 25 : 6 + mob.lv * 2;
            health.Died += _ =>
            {
                if (!_training) return;
                WorldSave.AddExp(exp);
                _return.Add(mob);
                _returnAt.Add(Time.time + (mob.boss ? 600f : 45f));
            };
        }

        void Update()
        {
            if (!_training || _return.Count == 0) return;
            var hero = GameObject.FindGameObjectWithTag("Player");
            var spawner = LevelFlow.Instance != null ? LevelFlow.Instance.Spawner : null;
            if (hero == null || spawner == null) return;
            var hp = hero.GetComponent<Health>();
            if (hp != null && hp.IsDead)
            {
                hp.ReviveFull();
                hero.transform.position = new Vector3(_portal.x / Ppu, -_portal.y / Ppu, hero.transform.position.z);
            }
            for (int i = _return.Count - 1; i >= 0; i--)
            {
                if (Time.time < _returnAt[i]) continue;
                var mob = _return[i];
                var pos = new Vector2(mob.x / Ppu, -mob.y / Ppu);
                if (Vector2.Distance(hero.transform.position, pos) < 12f) continue;
                var go = spawner.SpawnCampMob(mob.name, pos, mob.lv, mob.boss, mob.leash);
                if (go == null) continue;
                HookFoe(go, mob);
                _return.RemoveAt(i);
                _returnAt.RemoveAt(i);
            }
        }

        void BakeGround()
        {
            for (int x = 0; x < W; x++)
            {
                _rim[x] = 24 + (int)Math.Floor(Fbm(x / 46.0, 3.3, 1) * 14.0);
                _base[x] = _rim[x] + 46 + (int)Math.Floor(Fbm(x / 21.0, 7.1, 2) * 9.0);
            }
            for (int cy = 0; cy < NY; cy++)
            for (int cx = 0; cx < NX; cx++)
            {
                int px = cx * 16 + 8;
                int py = cy * 16 + 8;
                Kind k;
                if (py < _rim[px]) k = Kind.Plateau;
                else if (py < _base[px]) k = Kind.Cliff;
                else if (Any(cx, cy, WaterAt)) k = Kind.Water;
                else if (Any(cx, cy, DirtAt)) k = Kind.Dirt;
                else
                {
                    double edge = Math.Min(px, Math.Min(W - px, H - py));
                    double forest = Math.Clamp((120.0 - edge) / 70.0, 0, 1);
                    double wx = (Fbm(px / 38.0, py / 38.0, 41) - 0.5) * 1.5;
                    double wy = (Fbm(px / 38.0 + 9.0, py / 38.0, 42) - 0.5) * 1.5;
                    double n = Fbm(px / 74.0 + wx, py / 74.0 + wy, 13) - forest * 0.3;
                    k = forest > 0.55 ? Kind.Forest : n < 0.42 ? Kind.Dark : Kind.Grass;
                }
                _kind[cy * NX + cx] = (byte)k;
                if (k == Kind.Water) _water++;
                else if (k == Kind.Dirt) _dirt++;
                else if (k == Kind.Dark) _dark++;
                else if (k == Kind.Forest) _forest++;
            }

            _buf = new Color32[W * H];
            var g0 = new Color32(0x2c, 0x4a, 0x47, 255);
            for (int i = 0; i < _buf.Length; i++) _buf[i] = g0;
            for (int cy = 0; cy < NY; cy++)
            for (int cx = 0; cx < NX; cx++)
            {
                Kind k = (Kind)_kind[cy * NX + cx];
                string fill = k == Kind.Dirt ? "Fill_Dirt"
                    : k == Kind.Water ? "Fill_Water"
                    : k == Kind.Forest ? "Fill_Forest"
                    : k == Kind.Dark || k == Kind.Plateau ? "Fill_Dark"
                    : k == Kind.Grass ? "Fill_Grass" : null;
                if (fill == null || !_art.TryGetValue(fill, out var sheet)) continue;
                int frame = (int)(Hash01(cx, cy, 3) * Math.Max(1, sheet.frames)) % Math.Max(1, sheet.frames);
                Stamp(cx * 16, cy * 16, sheet, frame);
            }
            for (int i = -1; i < NX; i++)
            for (int j = -1; j < NY; j++)
                StampDual(i, j);
            ScatterDecals();
            if (_art.TryGetValue("Cliff_Face", out var cliff))
            {
                for (int col = 0; col < NX; col++)
                {
                    int jag = 1 + (int)(Hash01(col, 7, 4) * 3.0);
                    int top = _base[col * 16] - 56 + jag;
                    int variant = (int)(Hash01(col, 3, 4) * 6.0) % 6;
                    Stamp(col * 16, top, cliff, variant);
                }
            }
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.SetPixels32(_buf);
            tex.Apply(false, false);
            Owned.Add(tex);
            var sprite = Sprite.Create(tex, new Rect(0, 0, W, H), new Vector2(0f, 1f), Ppu, 0, SpriteMeshType.FullRect);
            Owned.Add(sprite);
            var ground = new GameObject("Ground");
            ground.transform.SetParent(transform, false);
            var sr = ground.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 0;
        }

        void StampDual(int i, int j)
        {
            Kind a = At(i, j), b = At(i + 1, j), c = At(i, j + 1), d = At(i + 1, j + 1);
            if (a == Kind.Cliff || b == Kind.Cliff || c == Kind.Cliff || d == Kind.Cliff) return;
            int dx = 8 + 16 * i, dy = 8 + 16 * j;
            int dark = Mask(a, b, c, d, k => k == Kind.Dark || k == Kind.Forest || k == Kind.Plateau);
            int forest = Mask(a, b, c, d, k => k == Kind.Forest);
            int dirt = Mask(a, b, c, d, k => k == Kind.Dirt);
            int water = Mask(a, b, c, d, k => k == Kind.Water);
            if (dark > 0 && dark < 15 && _art.TryGetValue("Dual_GrassToDark", out var darkSheet))
                Stamp(dx, dy, darkSheet, dark);
            if (forest > 0 && forest < 15 && _art.TryGetValue("Dual_DarkToForest_Overlay", out var forestSheet))
                Stamp(dx, dy, forestSheet, forest);
            if (dirt > 0 && dirt < 15 && _art.TryGetValue("Dual_GrassToDirt_Overlay", out var dirtSheet))
                Stamp(dx, dy, dirtSheet, dirt);
            if (water > 0 && water < 15 && _art.TryGetValue("Dual_GrassToWater_Overlay", out var waterSheet))
                Stamp(dx, dy, waterSheet, water);
        }

        void ScatterDecals()
        {
            _art.TryGetValue("Decals", out var decals);
            for (int cy = 0; cy < NY; cy++)
            for (int cx = 0; cx < NX; cx++)
            {
                Kind k = (Kind)_kind[cy * NX + cx];
                if (k != Kind.Grass && k != Kind.Dark && k != Kind.Forest) continue;
                if (decals.tex != null && Hash01(cx, cy, 21) < 0.22)
                {
                    int frame = (int)(Hash01(cx, cy, 22) * decals.frames) % Math.Max(1, decals.frames);
                    int jx = (int)(Hash01(cx, cy, 23) * 12);
                    int jy = (int)(Hash01(cx, cy, 24) * 12);
                    Stamp(cx * 16 + jx, cy * 16 + jy, decals, frame);
                }
            }
        }

        void PlaceProps()
        {
            int n = Math.Min(_items.Count, _itemNames.Count);
            for (int i = 0; i < n; i++)
                Place(_itemNames[i], _items[i].x, _items[i].y, 0);
        }

        void PlaceExtras()
        {
            Place("Tent_Ruined", _secret.x, _secret.y, -2);
            for (int i = 0; i < _fires.Count; i++)
            {
                Place("FireRing_Big", _fires[i].x, _fires[i].y, -1);
                Place("Fire_Big", _fires[i].x, _fires[i].y, 2);
                Place("Glow_Warm", _fires[i].x, _fires[i].y, 1);
            }
            int top = _rim[Mathf.Clamp(_fallX, 0, W - 1)];
            int bot = _base[Mathf.Clamp(_fallX, 0, W - 1)];
            for (int y = top; y < bot; y += 40)
                Place("Waterfall", _fallX, y, 1);
            Place("Waterfall_Foam", _fallX, bot, 2);
            for (int i = 0; i < _bridges.Count; i++)
            {
                var b = _bridges[i];
                if (!b.hor) continue;
                for (int x = b.x0; x < b.x1; x += 16)
                {
                    Place("Bridge_Deck_H", x, b.y1, 1);
                    Place("Bridge_Rail_H", x, b.y0, 2);
                }
            }
            for (int col = 1; col < NX; col += 3)
            {
                if (Hash01(col, 9, 6) > 0.55) continue;
                int vine = (int)(Hash01(col, 8, 6) * 4) % 4;
                Place("Vine_" + vine, col * 16 + 4, _rim[col * 16], 3);
            }
            PlaceGrass();
            var dust = SpriteNamed("Px_White");
            if (dust != null) gameObject.AddComponent<CampDust>().Init(dust);
        }

        void PlaceGrass()
        {
            if (!_art.TryGetValue("TallGrass", out var art) || art.tex == null) return;
            var light = new[] { Frame(ref art, 0), Frame(ref art, 1), Frame(ref art, 2) };
            var dark = new[] { Frame(ref art, 3), Frame(ref art, 4), Frame(ref art, 5) };
            _art["TallGrass"] = art;
            for (int cy = 0; cy < NY; cy++)
            for (int cx = 0; cx < NX; cx++)
            {
                Kind k = (Kind)_kind[cy * NX + cx];
                if (k != Kind.Grass && k != Kind.Dark) continue;
                if (Hash01(cx, cy, 25) > 0.045) continue;
                float px = cx * 16 + 6;
                float py = cy * 16 + 8;
                var go = new GameObject("TallGrass");
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(px / Ppu, -py / Ppu, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = k == Kind.Grass ? light[0] : dark[0];
                var sorter = go.AddComponent<SpriteSorter>();
                sorter.baseOrder = 1;
                go.AddComponent<CampGrass>().Init(k == Kind.Grass ? light : dark, px, py);
                _props++;
            }
        }

        Sprite SpriteNamed(string name)
        {
            if (!_art.TryGetValue(name, out var art) || art.tex == null) return null;
            var sprite = Frame(ref art, 0);
            _art[name] = art;
            return sprite;
        }

        void Place(string name, float x, float y, int order)
        {
            if (name == "Stone_Full") name = "Wood_Full";
            else if (name == "Herb_Full") name = "Hide_Full";
            else if (name == "Stone_Depleted") name = "Wood_Depleted";
            else if (name == "Herb_Depleted") name = "Hide_Depleted";
            if (_art.ContainsKey(name + "_Trunk"))
            {
                Spawn(name + "_Trunk", x, y, 0, true, 7f, 4f);
                Spawn(name + "_Canopy", x, y, 2, false, 0, 0);
                Spawn("Tree_Shadow", x, y, -6, false, 0, 0);
                return;
            }
            bool bush = name.StartsWith("Bush");
            Spawn(name, x, y, order, bush, bush ? 9f : 0f, bush ? 4f : 0f);
            if (name == "Portal_Frame")
                Spawn("Portal_Swirl", x, y - 22f, 3, false, 0, 0);
        }

        void Spawn(string name, float x, float y, int order, bool body, float rx, float ry)
        {
            if (!_art.TryGetValue(name, out var art) || art.tex == null || !art.tex.isReadable)
            {
                _missing++;
                if (_missingNames.Length < 80 && !_missingNames.Contains(name))
                    _missingNames += (_missingNames.Length == 0 ? "" : ",") + name;
                return;
            }
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(x / Ppu, -y / Ppu, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Frame(ref art, 0);
            sr.sortingOrder = 4000 + order;
            var sorter = go.AddComponent<SpriteSorter>();
            sorter.baseOrder = order;
            if (name.EndsWith("_Canopy"))
                go.AddComponent<CampSway>().Init(0.55f, true, x, y);
            else if (name.StartsWith("Bush"))
                go.AddComponent<CampSway>().Init(0.35f, false, x, y);
            else if (name == "Flag_Tattered" && art.pix != null)
                go.AddComponent<CampFlag>().Init(art.pix, art.w, art.h, art.pivX, art.pivY);
            else if (art.frames > 1)
                go.AddComponent<CampFrames>().Init(Frames(ref art), art.fps);
            else if (name == "Glow_Warm")
                go.AddComponent<CampFlicker>();
            if (name == "Portal_Frame")
                go.AddComponent<CampGate>();
            if (name == "Fire_Big")
            {
                var spark = SpriteNamed("Px_White");
                if (spark != null) go.AddComponent<CampFireFx>().Init(spark);
            }
            if (name == "Chest_Closed")
                go.AddComponent<CampUse>().AsChest(SpriteNamed("Chest_Open"));
            if (name == "Wood_Full" || name == "Hide_Full")
                go.AddComponent<CampUse>().AsNode(SpriteNamed(name.Replace("_Full", "_Depleted")), name == "Wood_Full" ? "wood" : "hide");
            if (body && rx > 0f)
            {
                var box = go.AddComponent<BoxCollider2D>();
                box.size = new Vector2(rx * 2f / Ppu, ry * 2f / Ppu);
            }
            _art[name] = art;
            _props++;
        }

        Sprite Frame(ref Art art, int frame)
        {
            if (art.framesSprites == null || art.framesSprites.Length != Math.Max(1, art.frames))
                art.framesSprites = new Sprite[Math.Max(1, art.frames)];
            if (art.framesSprites[frame] != null) return art.framesSprites[frame];
            int cols = Math.Max(1, art.w / Math.Max(1, art.fw));
            int col = frame % cols;
            int row = frame / cols;
            int px = col * art.fw;
            int py = art.h - (row + 1) * art.fh;
            var sprite = Sprite.Create(art.tex, new Rect(px, py, art.fw, art.fh), new Vector2(art.pivX, art.pivY), Ppu, 0, SpriteMeshType.FullRect);
            Owned.Add(sprite);
            art.framesSprites[frame] = sprite;
            return sprite;
        }

        Sprite[] Frames(ref Art art)
        {
            int n = Math.Max(1, art.frames);
            var all = new Sprite[n];
            for (int i = 0; i < n; i++) all[i] = Frame(ref art, i);
            return all;
        }

        void Stamp(int dx, int dy, Art art, int frame)
        {
            if (art.pix == null || art.fw <= 0 || art.fh <= 0) return;
            int cols = Math.Max(1, art.w / art.fw);
            int col = frame % cols;
            int row = frame / cols;
            int sx = col * art.fw;
            int sy = row * art.fh;
            for (int y = 0; y < art.fh; y++)
            {
                int dy0 = dy + y;
                if ((uint)dy0 >= H) continue;
                int src = (sy + y) * art.w + sx;
                int dst = (H - 1 - dy0) * W;
                for (int x = 0; x < art.fw; x++)
                {
                    int dx0 = dx + x;
                    if ((uint)dx0 >= W) continue;
                    var c = art.pix[src + x];
                    if (c.a < 8) continue;
                    _buf[dst + dx0] = c;
                }
            }
        }

        Kind At(int cx, int cy)
        {
            if (cy < 0) return Kind.Plateau;
            if (cx < 0 || cx >= NX || cy >= NY) return Kind.Grass;
            return (Kind)_kind[cy * NX + cx];
        }

        static int Mask(Kind a, Kind b, Kind c, Kind d, Func<Kind, bool> isB)
        {
            return (isB(a) ? 1 : 0) + (isB(b) ? 2 : 0) + (isB(c) ? 4 : 0) + (isB(d) ? 8 : 0);
        }

        bool Any(int cx, int cy, Func<int, int, bool> pred)
        {
            for (int oy = 2; oy <= 14; oy += 6)
            for (int ox = 2; ox <= 14; ox += 6)
                if (pred(cx * 16 + ox, cy * 16 + oy)) return true;
            return false;
        }

        bool WaterAt(int x, int y) => WaterF(x, y) > 0;
        bool DirtAt(int x, int y) => DirtF(x, y) > 0;

        double DirtF(double x, double y)
        {
            double m = 1e9;
            for (int i = 0; i < _ell.Count; i++)
            {
                var e = _ell[i];
                double d = EllipseDist(x, y, e);
                if (d < 0 && e.isl && Math.Sqrt((x - e.x) * (x - e.x) + ((y - e.y) * 1.4) * ((y - e.y) * 1.4)) > 58)
                {
                    double n = Fbm(x / 24.0, y / 24.0, 17);
                    if (n > 0.6) d = Math.Max(d, (n - 0.6) * 160);
                }
                if (d < m) m = d;
            }
            for (int i = 0; i < _paths.Count; i++)
            {
                if (m < -4) break;
                double d = Poly(x, y, _paths[i].pts) - _paths[i].w;
                if (d < m) m = d;
            }
            return -m + (Fbm(x / 8.0, y / 8.0, 5) - 0.5) * 9.0;
        }

        double WaterF(double x, double y)
        {
            double v = -1e9;
            if (_stream.Count > 1) v = -(Poly(x, y, _stream) - 6 - Fbm(x / 14.0, y / 14.0, 9) * 4);
            for (int i = 0; i < _ponds.Count; i++)
                v = Math.Max(v, -EllipseDist(x, y, _ponds[i]) + (Fbm(x / 10.0, y / 10.0, 8) - 0.5) * 7);
            return v;
        }

        static double EllipseDist(double x, double y, Ell e)
        {
            double dx = (x - e.x) / e.rx;
            double dy = (y - e.y) / e.ry;
            return (Math.Sqrt(dx * dx + dy * dy) - 1.0) * Math.Min(e.rx, e.ry);
        }

        static double Poly(double x, double y, List<Vector2> pts)
        {
            double m = 1e9;
            for (int i = 0; i < pts.Count - 1; i++)
            {
                double d = Seg(x, y, pts[i].x, pts[i].y, pts[i + 1].x, pts[i + 1].y);
                if (d < m) m = d;
            }
            return m;
        }

        static double Seg(double px, double py, double ax, double ay, double bx, double by)
        {
            double dx = bx - ax, dy = by - ay;
            double l = dx * dx + dy * dy;
            double t = l > 0 ? ((px - ax) * dx + (py - ay) * dy) / l : 0;
            t = Math.Clamp(t, 0, 1);
            double qx = ax + dx * t - px, qy = ay + dy * t - py;
            return Math.Sqrt(qx * qx + qy * qy);
        }

        static double Hash01(int x, int y, int s)
        {
            unchecked
            {
                uint h = (uint)(x * 374761393) ^ (uint)(y * 668265263) ^ (uint)((s + 7) * 0x5bd1e995);
                h = (h ^ (h >> 13)) * 1274126177u;
                h ^= h >> 16;
                return h / 4294967296.0;
            }
        }

        static double Vn(double x, double y, int s)
        {
            int xi = (int)Math.Floor(x);
            int yi = (int)Math.Floor(y);
            double xf = x - xi, yf = y - yi;
            double u = xf * xf * (3 - 2 * xf);
            double v = yf * yf * (3 - 2 * yf);
            double a = Hash01(xi, yi, s);
            double b = Hash01(xi + 1, yi, s);
            double c = Hash01(xi, yi + 1, s);
            double d = Hash01(xi + 1, yi + 1, s);
            return a + (b - a) * u + (c - a) * v + (a - b - c + d) * u * v;
        }

        static double Fbm(double x, double y, int s)
        {
            return Vn(x, y, s) * 0.55 + Vn(x * 2.03, y * 2.03, s + 11) * 0.28 + Vn(x * 4.1, y * 4.1, s + 23) * 0.17;
        }

        void ReadEllipses(Js js, List<Ell> dst)
        {
            js.Eat('[');
            while (js.Next() == '{')
            {
                var e = new Ell();
                js.Eat('{');
                while (js.Next() != '}' && js.Next() != '\0')
                {
                    string key = js.Str();
                    js.Eat(':');
                    if (key == "x") e.x = js.Num();
                    else if (key == "y") e.y = js.Num();
                    else if (key == "rx") e.rx = js.Num();
                    else if (key == "ry") e.ry = js.Num();
                    else if (key == "isl") e.isl = js.Bool();
                    else js.SkipValue();
                    if (js.Next() == ',') js.Eat(',');
                }
                js.Eat('}');
                dst.Add(e);
                if (js.Next() == ',') js.Eat(',');
            }
            js.Eat(']');
        }

        void ReadPaths(Js js)
        {
            js.Eat('[');
            while (js.Next() == '{')
            {
                var path = new Path { pts = new List<Vector2>() };
                js.Eat('{');
                while (js.Next() != '}' && js.Next() != '\0')
                {
                    string key = js.Str();
                    js.Eat(':');
                    if (key == "p") path.pts = ReadPairs(js);
                    else if (key == "w") path.w = js.Num();
                    else js.SkipValue();
                    if (js.Next() == ',') js.Eat(',');
                }
                js.Eat('}');
                _paths.Add(path);
                if (js.Next() == ',') js.Eat(',');
            }
            js.Eat(']');
        }

        void ReadBridges(Js js)
        {
            js.Eat('[');
            while (js.Next() == '{')
            {
                var b = new Bridge();
                js.Eat('{');
                while (js.Next() != '}' && js.Next() != '\0')
                {
                    string key = js.Str();
                    js.Eat(':');
                    if (key == "x0") b.x0 = (int)js.Num();
                    else if (key == "x1") b.x1 = (int)js.Num();
                    else if (key == "y0") b.y0 = (int)js.Num();
                    else if (key == "y1") b.y1 = (int)js.Num();
                    else if (key == "hor") b.hor = js.Bool();
                    else js.SkipValue();
                    if (js.Next() == ',') js.Eat(',');
                }
                js.Eat('}');
                _bridges.Add(b);
                if (js.Next() == ',') js.Eat(',');
            }
            js.Eat(']');
        }

        static List<Vector2> ReadPairs(Js js)
        {
            var list = new List<Vector2>();
            if (js.Next() != '[') return list;
            js.Eat('[');
            if (js.Next() != '[')
            {
                return list;
            }
            while (js.Next() == '[')
            {
                js.Eat('[');
                float x = js.Num();
                js.Eat(',');
                float y = js.Num();
                js.Eat(']');
                list.Add(new Vector2(x, y));
                if (js.Next() == ',') js.Eat(',');
            }
            js.Eat(']');
            return list;
        }

        static void HideOldCamp()
        {
            var maps = GameObject.Find("LevelMaps");
            if (maps != null)
            {
                var camp = maps.transform.Find("Camp");
                if (camp != null) camp.gameObject.SetActive(false);
            }
            var grid = GameObject.Find("ForestGrid");
            if (grid != null) grid.SetActive(false);
            var props = GameObject.Find("ForestProps");
            if (props != null) props.SetActive(false);
        }

        static int Int(string s, int fallback) => int.TryParse(s, out int v) ? v : fallback;
        static float Float(string s, float fallback) => float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out float v) ? v : fallback;
        static float Fps(string line)
        {
            var m = Regex.Match(line, "(\\d+)\\s*fps");
            return m.Success ? float.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : 8f;
        }

        sealed class Js
        {
            readonly string s;
            int i;
            public Js(string s) { this.s = s; }
            public void Skip() { while (i < s.Length && char.IsWhiteSpace(s[i])) i++; }
            public char Next() { Skip(); return i < s.Length ? s[i] : '\0'; }
            public void Eat(char c) { Skip(); if (i < s.Length && s[i] == c) i++; }
            public bool Seek(string key)
            {
                int k = s.IndexOf("\"" + key + "\"", i, StringComparison.Ordinal);
                if (k < 0) return false;
                i = k + key.Length + 2;
                Skip();
                if (i < s.Length && s[i] == ':') i++;
                return true;
            }
            public float Num()
            {
                Skip();
                int a = i;
                if (i < s.Length && s[i] == '-') i++;
                while (i < s.Length && (char.IsDigit(s[i]) || s[i] == '.')) i++;
                return float.Parse(s.Substring(a, i - a), CultureInfo.InvariantCulture);
            }
            public string Str()
            {
                Skip();
                if (i >= s.Length || s[i] != '"') return "";
                int a = ++i;
                while (i < s.Length && s[i] != '"') i++;
                string r = s.Substring(a, i - a);
                if (i < s.Length) i++;
                return r;
            }
            public bool Bool()
            {
                Skip();
                if (i < s.Length && s[i] == 't') { i += 4; return true; }
                i += 5;
                return false;
            }
            public void SkipValue()
            {
                char c = Next();
                if (c == '"') { Str(); return; }
                if (c == 't' || c == 'f') { Bool(); return; }
                if (c == '-' || char.IsDigit(c)) { Num(); return; }
                if (c == '[')
                {
                    Eat('[');
                    while (Next() != ']' && Next() != '\0') { SkipValue(); if (Next() == ',') Eat(','); }
                    Eat(']');
                    return;
                }
                if (c == '{')
                {
                    Eat('{');
                    while (Next() != '}' && Next() != '\0') { Str(); Eat(':'); SkipValue(); if (Next() == ',') Eat(','); }
                    Eat('}');
                }
            }
        }
    }

    public class CampSway : MonoBehaviour
    {
        float _amp, _px, _py, _alpha = 1f;
        bool _fade;
        Vector3 _feet;
        SpriteRenderer _sr;
        Transform _hero;

        public void Init(float ampPx, bool fade, float px, float py)
        {
            _amp = ampPx;
            _fade = fade;
            _px = px;
            _py = py;
            _feet = transform.position;
            _sr = GetComponent<SpriteRenderer>();
        }

        void LateUpdate()
        {
            float t = Time.time;
            float s = Mathf.Sin(t * 1.7f - _px * 0.022f - _py * 0.008f) + 0.6f * Mathf.Sin(t * 0.6f - _px * 0.006f);
            transform.position = _feet + new Vector3(s / 1.6f * _amp / 16f, 0f, 0f);
            if (!_fade || _sr == null) return;
            if (_hero == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) _hero = go.transform;
            }
            if (_hero == null) return;
            float dx = Mathf.Abs(_hero.position.x - _feet.x) * 16f;
            float above = (_hero.position.y - _feet.y) * 16f;
            bool hide = dx < 30f && above > 4f && above < 64f;
            _alpha = Mathf.MoveTowards(_alpha, hide ? 0.42f : 1f, Time.deltaTime * (0.58f / 0.12f));
            var c = _sr.color;
            c.a = _alpha;
            _sr.color = c;
        }
    }

    public class CampFrames : MonoBehaviour
    {
        Sprite[] _frames;
        float _fps;
        SpriteRenderer _sr;
        int _i = -1;

        public void Init(Sprite[] frames, float fps)
        {
            _frames = frames;
            _fps = fps <= 0f ? 8f : fps;
            _sr = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            if (_frames == null || _frames.Length == 0 || _sr == null) return;
            int i = (int)(Time.time * _fps) % _frames.Length;
            if (i == _i) return;
            _i = i;
            _sr.sprite = _frames[i];
        }
    }

    public class CampFlicker : MonoBehaviour
    {
        SpriteRenderer _sr;

        void Awake() => _sr = GetComponent<SpriteRenderer>();

        void Update()
        {
            if (_sr == null) return;
            float a = Mathf.Lerp(0.85f, 1f, (Mathf.Sin(Time.time * 6f) + 1f) * 0.5f);
            _sr.color = new Color(1f, 0.85f, 0.55f, a);
        }
    }

    public class CampFlag : MonoBehaviour
    {
        Color32[] _src;
        Color32[] _dst;
        Texture2D _tex;
        int _w, _h;
        SpriteRenderer _sr;

        Sprite _sprite;

        public void Init(Color32[] yDown, int w, int h, float pivX, float pivY)
        {
            _src = yDown;
            _w = w;
            _h = h;
            _dst = new Color32[w * h];
            _tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            _tex.filterMode = FilterMode.Point;
            _tex.wrapMode = TextureWrapMode.Clamp;
            _sr = GetComponent<SpriteRenderer>();
            _sprite = Sprite.Create(_tex, new Rect(0, 0, w, h), new Vector2(pivX, pivY), 16f, 0, SpriteMeshType.FullRect);
            _sr.sprite = _sprite;
        }

        void OnDestroy()
        {
            if (_sprite != null) Destroy(_sprite);
            if (_tex != null) Destroy(_tex);
        }

        void LateUpdate()
        {
            if (_src == null || _tex == null) return;
            float t = Time.time;
            for (int x = 0; x < _w; x++)
            {
                int shift = x < 4 ? 0 : Mathf.RoundToInt(Mathf.Sin(t * 4.2f - x * 0.45f) * 1.3f);
                for (int y = 0; y < _h; y++)
                {
                    int sy = y + shift;
                    Color32 c = (uint)sy < (uint)_h ? _src[sy * _w + x] : new Color32(0, 0, 0, 0);
                    _dst[x + (_h - 1 - y) * _w] = c;
                }
            }
            _tex.SetPixels32(_dst);
            _tex.Apply(false);
        }
    }

    public class CampGrass : MonoBehaviour
    {
        Sprite[] _frames;
        SpriteRenderer _sr;
        float _px, _py;
        Transform _hero;

        public void Init(Sprite[] frames, float px, float py)
        {
            _frames = frames;
            _px = px;
            _py = py;
            _sr = GetComponent<SpriteRenderer>();
        }

        void Update()
        {
            if (_frames == null || _frames.Length == 0 || _sr == null) return;
            if (_hero == null)
            {
                var go = GameObject.FindGameObjectWithTag("Player");
                if (go != null) _hero = go.transform;
            }
            int i = 0;
            bool bend = false;
            if (_hero != null)
            {
                float dx = (_hero.position.x - transform.position.x) * 16f;
                float dy = (_hero.position.y - transform.position.y) * 16f;
                bend = dx * dx + dy * dy < 81f;
                if (bend) transform.localScale = new Vector3(dx < 0f ? -1f : 1f, 1f, 1f);
            }
            if (!bend)
            {
                float phase = Mathf.Sin(Time.time * 2.2f - _px * 0.045f - _py * 0.01f);
                i = Mathf.Clamp(Mathf.RoundToInt((phase + 1f) * 0.5f * (_frames.Length - 1)), 0, _frames.Length - 1);
            }
            else i = _frames.Length - 1;
            _sr.sprite = _frames[i];
        }
    }

    public class CampUse : MonoBehaviour
    {
        Sprite _next;
        Sprite _full;
        SpriteRenderer _sr;
        string _item;
        float _wait;
        bool _chest;
        bool _used;

        public void AsChest(Sprite open)
        {
            _chest = true;
            _next = open;
            _sr = GetComponent<SpriteRenderer>();
        }

        public void AsNode(Sprite depleted, string itemId)
        {
            _next = depleted;
            _item = itemId;
            _sr = GetComponent<SpriteRenderer>();
            _full = _sr != null ? _sr.sprite : null;
        }

        void Update()
        {
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_wait > 0f)
            {
                _wait -= Time.deltaTime;
                if (_wait <= 0f && _full != null) _sr.sprite = _full;
                return;
            }
            if (_used || _next == null || _sr == null) return;
            var hero = GameObject.FindGameObjectWithTag("Player");
            if (hero == null) return;
            if (Vector2.Distance(hero.transform.position, transform.position) > 1.1f) return;
            var kb = UnityEngine.InputSystem.Keyboard.current;
            if (kb == null || (!kb.fKey.wasPressedThisFrame && !kb.spaceKey.wasPressedThisFrame)) return;
            _sr.sprite = _next;
            if (!string.IsNullOrEmpty(_item)) WorldSave.AddItem(_item, 1);
            if (_chest)
            {
                WorldSave.AddExp(15);
                _used = true;
            }
            else _wait = 90f;
        }
    }

    public class CampFireFx : MonoBehaviour
    {
        struct Bit { public Transform t; public SpriteRenderer sr; public float life; public float vy; }
        Bit[] _bits;
        float _emit;

        public void Init(Sprite dot)
        {
            _bits = new Bit[8];
            for (int i = 0; i < _bits.Length; i++)
            {
                var go = new GameObject("spark");
                go.transform.SetParent(transform, false);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = dot;
                sr.sortingOrder = 4060;
                go.SetActive(false);
                _bits[i].t = go.transform;
                _bits[i].sr = sr;
            }
        }

        void Update()
        {
            if (_bits == null) return;
            _emit -= Time.deltaTime;
            for (int i = 0; i < _bits.Length; i++)
            {
                if (_bits[i].life <= 0f)
                {
                    if (_emit > 0f) continue;
                    _emit = 0.12f;
                    _bits[i].life = 0.7f;
                    _bits[i].vy = UnityEngine.Random.Range(0.7f, 1.4f);
                    _bits[i].t.localPosition = new Vector3(UnityEngine.Random.Range(-0.15f, 0.15f), 0.1f, 0f);
                    _bits[i].t.gameObject.SetActive(true);
                }
                _bits[i].life -= Time.deltaTime;
                var p = _bits[i].t.localPosition;
                p.y += _bits[i].vy * Time.deltaTime;
                p.x += Mathf.Sin(Time.time * 8f + i) * 0.15f * Time.deltaTime;
                _bits[i].t.localPosition = p;
                var c = _bits[i].sr.color;
                c.r = 1f; c.g = 0.75f; c.b = 0.35f; c.a = Mathf.Clamp01(_bits[i].life / 0.7f);
                _bits[i].sr.color = c;
                if (_bits[i].life <= 0f) _bits[i].t.gameObject.SetActive(false);
            }
        }
    }

    public class CampDust : MonoBehaviour
    {
        struct Bit { public Transform t; public float speed; }
        Bit[] _bits;

        public void Init(Sprite dot)
        {
            _bits = new Bit[10];
            for (int i = 0; i < _bits.Length; i++)
            {
                var go = new GameObject("dust");
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(UnityEngine.Random.Range(4f, 96f), UnityEngine.Random.Range(-66f, -4f), 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = dot;
                sr.color = new Color(0.96f, 0.95f, 0.82f, 0.45f);
                sr.sortingOrder = 4020;
                _bits[i].t = go.transform;
                _bits[i].speed = UnityEngine.Random.Range(0.35f, 0.8f);
            }
        }

        void Update()
        {
            if (_bits == null) return;
            for (int i = 0; i < _bits.Length; i++)
            {
                var p = _bits[i].t.position;
                p.x += _bits[i].speed * Time.deltaTime;
                p.y += _bits[i].speed * 0.25f * Time.deltaTime;
                if (p.x > 100f) p.x = 0f;
                if (p.y > 0f) p.y = -70f;
                _bits[i].t.position = p;
            }
        }
    }

    public class CampGate : MonoBehaviour
    {
        void Update()
        {
            var hero = GameObject.FindGameObjectWithTag("Player");
            if (hero == null) return;
            if (Vector2.Distance(hero.transform.position, transform.position) > 0.85f) return;
            var nav = WorldNavigator.Instance;
            if (nav != null) nav.Show(WorldZone.Hub);
        }
    }
}
