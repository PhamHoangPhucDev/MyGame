using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Owned Home chunks (WILD_START_HANDOFF section 2 and 4). The centre chunk starts open.
    /// A save that already has a house, layout, land or crops keeps every chunk open.
    /// Locked ground is fog: nothing can be built or painted there. Expanding spends wood and ore.
    /// </summary>
    public static class HomeArea
    {
        const string Key = WorldCatalog.SavePrefix + "home.area";
        const int Centre = 4;
        const int All = 0x1FF;

        static int _mask;
        static bool _ready;

        /// <summary>Raised after a chunk is paid for and the mask is saved. The argument is the chunk index.</summary>
        public static event System.Action<int> Expanded;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() { _mask = 0; _ready = false; Expanded = null; }

        public static int Mask { get { Load(); return _mask; } }

        /// <summary>No housing progress yet: grass, the foundation, the gate and the mailbox only.</summary>
        public static bool WildStart { get { Load(); return !HasProgress(); } }

        public static bool Open(Vector2 local) { Load(); return OpenAt(local, _mask); }

        public static bool OpenAt(Vector2 local, int mask)
        {
            if (local.x < -17f || local.x >= 17f || local.y < -8f || local.y >= 10f) return false;
            if (local.x >= -1.5f && local.x <= 1.5f && local.y < -4.5f && local.y >= -7.5f) return true;
            int i = ChunkOf(local);
            return i >= 0 && (mask & (1 << i)) != 0;
        }

        /// <summary>Open ground outside the starting centre chunk (where a field may be laid).</summary>
        public static bool IsExpanded(Vector2 local)
        {
            Load();
            int i = ChunkOf(local);
            return i >= 0 && i != Centre && (_mask & (1 << i)) != 0;
        }

        public static string Block(Rect r)
        {
            if (Open(new Vector2(r.xMin + 0.02f, r.yMin + 0.02f))
                && Open(new Vector2(r.xMax - 0.02f, r.yMin + 0.02f))
                && Open(new Vector2(r.xMin + 0.02f, r.yMax - 0.02f))
                && Open(new Vector2(r.xMax - 0.02f, r.yMax - 0.02f))) return "";
            return "SUONG MU";
        }

        public static void Price(int openedBeyondCentre, out int wood, out int ore)
        {
            float scale = Mathf.Pow(1.6f, Mathf.Max(0, openedBeyondCentre));
            wood = Mathf.RoundToInt(20f * scale);
            ore = Mathf.RoundToInt(10f * scale);
        }

        /// <summary>Opens every locked side at once. One board, one payment. Empty string on success.</summary>
        public static string TryExpand(int index)
        {
            Load();
            const int Rim = All ^ (1 << Centre);
            if ((_mask & Rim) == Rim) return "DA MO";
            Cost(out int wood, out int ore);
            if (WorldSave.GetQty("wood") < wood) return "THIEU GO";
            if (WorldSave.GetQty("ore") < ore) return "THIEU DA";
            if (!WorldSave.RemoveItem("wood", wood) || !WorldSave.RemoveItem("ore", ore)) return "THIEU GO";
            _mask |= Rim;
            PlayerPrefs.SetInt(Key, _mask);
            PlayerPrefs.Save();
            var district = GameObject.Find("HomeDistrict");
            if (district != null) Spawn(district.transform);
            Expanded?.Invoke(index);
            return "";
        }

        public static void Reload()
        {
            _ready = false;
            Load();
            var district = GameObject.Find("HomeDistrict");
            if (district != null) Spawn(district.transform);
        }

        public static void Spawn(Transform root)
        {
            if (root == null) return;
            Load();
            var old = root.Find("HomeBounds");
            if (old != null) Object.Destroy(old.gameObject);
            var host = new GameObject("HomeBounds").transform;
            host.SetParent(root, false);
            BeyondGrass(host);
            if (_mask == All) return;
            var fog = Resources.Load<Sprite>("World/HomeDistrict/Wild/WL_Fog");
            var edge = Resources.Load<Sprite>("World/HomeDistrict/Wild/WL_FogEdge");
            for (int y = -8; y <= 9; y++)
                for (int x = -17; x <= 16; x++)
                {
                    var centre = new Vector2(x + 0.5f, y + 0.5f);
                    if (Open(centre)) continue;
                    var go = new GameObject("Fog");
                    go.transform.SetParent(host, false);
                    go.transform.localPosition = centre;
                    var sr = go.AddComponent<SpriteRenderer>();
                    bool rim = Open(centre + Vector2.left) || Open(centre + Vector2.right)
                        || Open(centre + Vector2.up) || Open(centre + Vector2.down);
                    sr.sprite = rim && edge != null ? edge : fog;
                    sr.sortingOrder = 2800;
                }
            SeamVertical(host, -5f, 0);
            SeamVertical(host, 5f, 1);
            SeamHorizontal(host, -4.5f, 0);
            SeamHorizontal(host, 6f, 1);
            for (int i = 0; i < 9; i++)
                if ((_mask & (1 << i)) == 0) BlockChunk(host, i);
            const int Rim = All ^ (1 << Centre);
            if ((_mask & Rim) != Rim) Sign(host);
        }

        static void Load()
        {
            if (_ready) return;
            _ready = true;
            if (PlayerPrefs.HasKey(Key)) _mask = PlayerPrefs.GetInt(Key, 1 << Centre) & All;
            else if (HasProgress()) _mask = All;
            else _mask = 1 << Centre;
            if (_mask == 0) _mask = 1 << Centre;
            if (PlayerPrefs.GetInt(Key, -1) != _mask)
            {
                PlayerPrefs.SetInt(Key, _mask);
                PlayerPrefs.Save();
            }
        }

        static bool HasProgress()
        {
            string p = WorldCatalog.SavePrefix;
            if (PlayerPrefs.HasKey(p + "home.layout")) return true;
            if (PlayerPrefs.HasKey(p + "house.built")) return true;
            if (PlayerPrefs.HasKey(p + "home.land")) return true;
            if (PlayerPrefs.HasKey(p + "home.water")) return true;
            if (PlayerPrefs.HasKey(p + "home.clutter")) return true;
            if (PlayerPrefs.HasKey(p + "home.farm.starter")) return true;
            for (int i = 0; i < 16; i++)
                if (PlayerPrefs.HasKey(p + "home.plot." + i)) return true;
            return false;
        }

        /// <summary>0..8, row-major. Centre is 4. -1 when outside the 3x3.</summary>
        public static int ChunkOf(Vector2 p)
        {
            if (p.x < -15f || p.x >= 15f || p.y < -15f || p.y >= 16.5f) return -1;
            int col = p.x < -5f ? 0 : p.x < 5f ? 1 : 2;
            int row = p.y < -4.5f ? 0 : p.y < 6f ? 1 : 2;
            return col + row * 3;
        }

        static bool Touches(int index)
        {
            int col = index % 3, row = index / 3;
            if (col > 0 && Bit(index - 1)) return true;
            if (col < 2 && Bit(index + 1)) return true;
            if (row > 0 && Bit(index - 3)) return true;
            if (row < 2 && Bit(index + 3)) return true;
            return false;
        }

        static bool Bit(int index) => index >= 0 && index < 9 && (_mask & (1 << index)) != 0;

        static int BitCount(int mask)
        {
            int n = 0;
            for (int i = 0; i < 9; i++) if ((mask & (1 << i)) != 0) n++;
            return n;
        }

        /// <summary>Outside the baked grass (x -17..17, y -8..10) stays fog and solid, even after every chunk is open.</summary>
        static void BeyondGrass(Transform host)
        {
            const float x0 = -17f, x1 = 17f, y0 = -8f, y1 = 10f, pad = 16f;
            Wall(host, (x0 + x1) * 0.5f, y0 - pad * 0.5f, x1 - x0 + pad * 2f, pad);
            Wall(host, (x0 + x1) * 0.5f, y1 + pad * 0.5f, x1 - x0 + pad * 2f, pad);
            Wall(host, x0 - pad * 0.5f, (y0 + y1) * 0.5f, pad, y1 - y0);
            Wall(host, x1 + pad * 0.5f, (y0 + y1) * 0.5f, pad, y1 - y0);
        }

        static void SeamVertical(Transform host, float x, int leftCol)
        {
            for (int row = 0; row < 3; row++)
            {
                bool left = Bit(leftCol + row * 3), right = Bit(leftCol + 1 + row * 3);
                if (left == right) continue;
                Wall(host, x, RowMid(row), 0.2f, 10.5f);
            }
        }

        static void SeamHorizontal(Transform host, float y, int lowRow)
        {
            for (int col = 0; col < 3; col++)
            {
                bool low = Bit(col + lowRow * 3), high = Bit(col + (lowRow + 1) * 3);
                if (low == high) continue;
                float x0 = -15f + col * 10f, x1 = x0 + 10f;
                if (y < -4f && col == 1)
                {
                    Wall(host, (x0 + -1.5f) * 0.5f, y, -1.5f - x0, 0.2f);
                    Wall(host, (1.5f + x1) * 0.5f, y, x1 - 1.5f, 0.2f);
                }
                else Wall(host, (x0 + x1) * 0.5f, y, 10f, 0.2f);
            }
        }

        /// <summary>One payment opens the whole rim.</summary>
        public static void Cost(out int wood, out int ore)
        {
            Price(0, out wood, out ore);
        }

        static void BlockChunk(Transform host, int index)
        {
            int col = index % 3, row = index / 3;
            float x0 = -15f + col * 10f, x1 = x0 + 10f;
            float y0 = row == 0 ? -15f : row == 1 ? -4.5f : 6f;
            float y1 = row == 0 ? -4.5f : row == 1 ? 6f : 16.5f;
            const float gateX0 = -1.5f, gateX1 = 1.5f, gateY = -7.5f;
            if (row == 0 && x1 > gateX0 && x0 < gateX1)
            {
                Box(host, x0, x1, y0, gateY);
                Box(host, x0, Mathf.Min(x1, gateX0), gateY, y1);
                Box(host, Mathf.Max(x0, gateX1), x1, gateY, y1);
                return;
            }
            Box(host, x0, x1, y0, y1);
        }

        static void Box(Transform host, float x0, float x1, float y0, float y1)
        {
            if (x1 - x0 < 0.05f || y1 - y0 < 0.05f) return;
            Wall(host, (x0 + x1) * 0.5f, (y0 + y1) * 0.5f, x1 - x0, y1 - y0);
        }

        static float RowMid(int row) => row == 0 ? -9.75f : row == 1 ? 0.75f : 11.25f;

        static void Wall(Transform host, float x, float y, float w, float h)
        {
            if (w < 0.05f || h < 0.05f) return;
            var go = new GameObject("Bound");
            go.transform.SetParent(host, false);
            go.transform.localPosition = new Vector3(x, y, 0f);
            go.AddComponent<BoxCollider2D>().size = new Vector2(w, h);
        }

        static void Sign(Transform host)
        {
            var go = new GameObject("Expand");
            go.transform.SetParent(host, false);
            go.transform.localPosition = new Vector3(2.6f, -3.6f, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Resources.Load<Sprite>("World/HomeDistrict/Wild/WL_PriceSign");
            go.AddComponent<SpriteSorter>();
            var talk = go.AddComponent<WorldInteractable>();
            talk.DisplayName = "MO RONG";
            talk.Radius = 1.3f;
            talk.OnInteract = () =>
            {
                Cost(out int wood, out int ore);
                // Short of wood/stone: say how much is missing instead of offering a confirm that can only fail.
                int haveW = WorldSave.GetQty("wood"), haveO = WorldSave.GetQty("ore");
                if (haveW < wood || haveO < ore)
                {
                    string miss = "THIEU";
                    if (haveW < wood) miss += " " + (wood - haveW) + " GO";
                    if (haveO < ore) miss += " " + (ore - haveO) + " DA";
                    WorldServices.Fail(Deny.InsufficientResources, miss);
                    return;
                }
                string ask = "XAC NHAN MO 4 PHIA  GO " + wood + "  DA " + ore;
                SpendConfirm.Arm("expand", ask, () =>
                {
                    if (!SpendConfirm.Take("expand")) return;
                    string why = TryExpand(1);
                    if (why.Length == 0) WorldServices.Ok("DAT MOI: 4 PHIA");
                    else WorldServices.Fail(why);
                });
            };
            go.AddComponent<HomeExpandSign>();
        }

        public static string Side(int index)
        {
            int col = index % 3, row = index / 3;
            string h = col == 0 ? "TAY" : col == 2 ? "DONG" : "";
            string v = row == 0 ? "NAM" : row == 2 ? "BAC" : "";
            if (h.Length == 0) return "PHIA " + v;
            if (v.Length == 0) return "PHIA " + h;
            return "PHIA " + v + " " + h;
        }
    }

    /// <summary>Refreshes the expand prompt with wood and ore the player has.</summary>
    public class HomeExpandSign : MonoBehaviour
    {
        public int Index;
        float _next;

        void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 0.5f;
            var talk = GetComponent<WorldInteractable>();
            if (talk == null) return;
            HomeArea.Cost(out int wood, out int ore);
            if (SpendConfirm.Is("expand"))
                // Key names only with a keyboard; touch has the XAC NHAN / HUY buttons.
                talk.Line = SpendConfirm.Line + (UiKit.IsMobile ? "" : "   E XAC NHAN   ESC HUY");
            else
                talk.Line = "MO RONG 4 PHIA  GO " + WorldSave.GetQty("wood") + "/" + wood
                    + "  DA " + WorldSave.GetQty("ore") + "/" + ore;
        }
    }
}
