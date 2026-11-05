using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace GameplayPrototype
{
    public class Letter
    {
        public int id;
        public string sender, place;
        public string[] lines;
        public string questItem, questText;
        public int questQty;
        public List<KeyValuePair<string, int>> rewards = new List<KeyValuePair<string, int>>();
        public int coins;
        public bool read, claimed;
        public int arrivedDay;

        public bool HasReward => rewards.Count > 0 || coins > 0;
        public bool HasQuest => !string.IsNullOrEmpty(questItem) && questQty > 0;
        public bool QuestDone => !HasQuest || WorldSave.GetQty(questItem) >= questQty;
        public bool CanClaim => !claimed && HasReward && QuestDone;
    }

    /// <summary>Home mailbox: one letter per in-game day, quest letters and pushed event letters. Max 10, saved under gw.v1.</summary>
    public class Mailbox : MonoBehaviour
    {
        const string SaveKey = WorldCatalog.SavePrefix + "mail.list";
        const string DayKey = WorldCatalog.SavePrefix + "mail.day";
        public const int Cap = 10;

        static readonly Color Slate = new Color32(0x1C, 0x1F, 0x26, 0xFF);
        static readonly Color Border = new Color32(0x4A, 0x52, 0x60, 0xFF);
        static readonly Color Cyan = new Color32(0x8E, 0xC8, 0xF0, 0xFF);
        static readonly Color Grey = new Color32(0x8A, 0x92, 0xA0, 0xFF);
        static readonly Color Gold = new Color32(0xF2, 0xD8, 0x4A, 0xFF);
        static readonly Color Slot = new Color32(0x20, 0x20, 0x2A, 0xFF);
        static readonly Color Bevel = new Color32(0x5A, 0x5E, 0x6A, 0xFF);
        static readonly Color Green = new Color32(0x7C, 0xD6, 0x7C, 0xFF);

        static readonly string[,] Villagers =
        {
            { "MIRA", "LANG" }, { "BRAM", "LO REN" }, { "LARK", "BIA RUNG" }, { "NELL", "CHO" }, { "GAC CONG", "CONG LANG" }
        };

        struct Quest { public string item, text, reward; public int qty, rewardQty, coins; }

        static readonly Quest[] Quests =
        {
            new Quest { item = "carrot", qty = 5, text = "MANG 5 CA ROT", reward = "seed_cabbage", rewardQty = 3, coins = 10 },
            new Quest { item = "egg", qty = 3, text = "MANG 3 TRUNG GA", reward = "seed_carrot", rewardQty = 4, coins = 8 },
            new Quest { item = "cabbage", qty = 3, text = "MANG 3 BAP CAI", reward = "seed_carrot", rewardQty = 5, coins = 12 },
            new Quest { item = "duck_egg", qty = 2, text = "MANG 2 TRUNG VIT", reward = "seed_cabbage", rewardQty = 2, coins = 10 },
        };

        static List<Letter> _letters = new List<Letter>();
        static bool _loaded;
        static int _lastDay = -1;
        static int _nextId = 1;
        static int _selected = -1;

        public static Mailbox Driver { get; private set; }
        public static bool IsOpen { get; private set; }
        public static IReadOnlyList<Letter> Letters { get { Load(); return _letters; } }

        public static bool HasUnread
        {
            get
            {
                Load();
                for (int i = 0; i < _letters.Count; i++) if (!_letters[i].read) return true;
                return false;
            }
        }

        /// <summary>A read letter whose reward can be claimed now (shown as CO QUA in the list).</summary>
        public static bool HasGift
        {
            get
            {
                Load();
                for (int i = 0; i < _letters.Count; i++) if (_letters[i].read && _letters[i].CanClaim) return true;
                return false;
            }
        }

        public static int Day => (int)(GameplayClock.Now / Mathf.Max(10f, WorldContent.DayLengthSeconds));

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _letters = new List<Letter>();
            _loaded = false;
            _lastDay = -1;
            _nextId = 1;
            _selected = -1;
            Driver = null;
            IsOpen = false;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot()
        {
            if (Driver != null) return;
            var go = new GameObject("Mailbox");
            DontDestroyOnLoad(go);
            Driver = go.AddComponent<Mailbox>();
        }

        float _next;

        void Update()
        {
            if (Time.unscaledTime < _next) return;
            _next = Time.unscaledTime + 1f;
            GameplayClock.EnsureLoaded();
            Load();
            int day = Day;
            if (day <= _lastDay) return;
            bool first = _lastDay < 0;
            _lastDay = day;
            Add(DailyLetter(first ? 0 : day));
        }

        // ---------- data ----------

        /// <summary>Event or story letter from another system. Rewards are (itemId, qty) pairs.</summary>
        public static Letter Push(string sender, string place, string[] lines, int coins = 0, string questItem = null, int questQty = 0, string questText = null, params KeyValuePair<string, int>[] rewards)
        {
            Load();
            var l = new Letter
            {
                sender = Clean(sender), place = Clean(place), coins = Mathf.Max(0, coins),
                questItem = questItem, questQty = questQty, questText = Clean(questText), arrivedDay = Day
            };
            l.lines = new string[lines != null ? lines.Length : 0];
            for (int i = 0; i < l.lines.Length; i++) l.lines[i] = Clean(lines[i]);
            if (rewards != null)
                foreach (var r in rewards) if (!string.IsNullOrEmpty(r.Key) && r.Value > 0) l.rewards.Add(r);
            Add(l);
            return l;
        }

        static Letter DailyLetter(int day)
        {
            int v = day % Villagers.GetLength(0);
            string who = Villagers[v, 0], where = Villagers[v, 1];
            if (day == 0)
                return Make("GAC CONG", "CONG LANG", new[] { "CHAO MUNG VE NHA!", "VUON DA SAN SANG.", "GUI BAN IT HAT GIONG." }, 0, null,
                    new KeyValuePair<string, int>("seed_carrot", 3));
            if (day % 3 == 2)
            {
                var q = Quests[(day / 3) % Quests.Length];
                var l = Make(who, where, new[] { "BAN GIUP TOI NHE?", "TOI SE TRA ON." }, q.coins, null,
                    new KeyValuePair<string, int>(q.reward, q.rewardQty));
                l.questItem = q.item;
                l.questQty = q.qty;
                l.questText = q.text;
                return l;
            }
            bool cabbage = day % 2 == 0;
            return Make(who, where, new[] { "CHAO HANG XOM!", "TANG BAN IT QUA NHO.", "CHUC MUA MANG TOT." }, day % 4 == 1 ? 5 : 0, null,
                new KeyValuePair<string, int>(cabbage ? "seed_cabbage" : "seed_carrot", cabbage ? 2 : 3));
        }

        static Letter Make(string sender, string place, string[] lines, int coins, string questItem, params KeyValuePair<string, int>[] rewards)
        {
            var l = new Letter { sender = sender, place = place, lines = lines, coins = coins, questItem = questItem, arrivedDay = Day };
            l.rewards.AddRange(rewards);
            return l;
        }

        static void Add(Letter l)
        {
            l.id = _nextId++;
            _letters.Add(l);
            while (_letters.Count > Cap)
            {
                int drop = _letters.FindIndex(x => x.claimed || (x.read && !x.HasReward));
                _letters.RemoveAt(drop >= 0 ? drop : 0);
            }
            WorldTx.Begin().SetString(SaveKey, Encode()).SetInt(DayKey, _lastDay).Commit();
            Driver?.Render();
        }

        public static bool MarkRead(Letter l)
        {
            if (l == null || l.read) return false;
            l.read = true;
            Save();
            return true;
        }

        public static bool Claim(Letter l)
        {
            if (l == null || l.claimed) return WorldServices.Fail(Deny.AlreadyDone, "DA NHAN");
            if (!l.HasReward) return WorldServices.Fail(Deny.NotEligible, "KHONG CO QUA");
            if (!l.QuestDone) return WorldServices.Fail(Deny.InsufficientResources, "CHUA XONG NHIEM VU");
            var delta = new Dictionary<string, int>();
            if (l.HasQuest) delta[l.questItem] = -l.questQty;
            foreach (var r in l.rewards) delta[r.Key] = (delta.TryGetValue(r.Key, out int d) ? d : 0) + r.Value;
            foreach (var kv in delta)
            {
                int cap = WorldContent.StackLimit(kv.Key);
                if (kv.Value > 0 && cap > 0 && WorldSave.GetQty(kv.Key) + kv.Value > cap) return WorldServices.Fail(Deny.BagFull, "Bag full");
            }
            l.read = true;
            l.claimed = true;
            var tx = WorldTx.Begin();
            foreach (var kv in delta) tx.SetInt(WorldSave.KeyItem(kv.Key), WorldSave.GetQty(kv.Key) + kv.Value);
            int coins = l.coins;
            if (coins > 0) tx.SetInt(WorldSave.KeyCoins, WorldSave.Coins + coins);
            var rewards = new List<KeyValuePair<string, int>>(l.rewards);
            tx.SetString(SaveKey, Encode()).After(() =>
            {
                foreach (var r in rewards) WorldEvents.RaiseItemGained(r.Key, r.Value);
                if (coins > 0) WorldEvents.RaiseCoins(coins);
                Driver?.Banners(rewards, coins);
            });
            tx.Commit();
            Driver?.Render();
            return true;
        }

        static void Save()
        {
            WorldTx.Begin().SetString(SaveKey, Encode()).Commit();
        }

        static string Clean(string s) => string.IsNullOrEmpty(s) ? "" : s.Replace("|", " ").Replace("~", " ").Replace("\n", " ").ToUpperInvariant();

        static string Encode()
        {
            var sb = new StringBuilder();
            foreach (var l in _letters)
            {
                if (sb.Length > 0) sb.Append('\n');
                var rw = new StringBuilder();
                foreach (var r in l.rewards) { if (rw.Length > 0) rw.Append(','); rw.Append(r.Key).Append(':').Append(r.Value); }
                sb.Append(l.id).Append('|').Append(l.sender).Append('|').Append(l.place).Append('|')
                  .Append(string.Join("~", l.lines ?? new string[0])).Append('|')
                  .Append(l.questItem).Append('|').Append(l.questQty).Append('|').Append(l.questText).Append('|')
                  .Append(rw).Append('|').Append(l.coins).Append('|')
                  .Append(l.read ? 1 : 0).Append('|').Append(l.claimed ? 1 : 0).Append('|').Append(l.arrivedDay);
            }
            return sb.ToString();
        }

        static void Load()
        {
            if (_loaded) return;
            _loaded = true;
            _letters.Clear();
            _lastDay = PlayerPrefs.GetInt(DayKey, -1);
            string raw = PlayerPrefs.GetString(SaveKey, "");
            if (string.IsNullOrEmpty(raw)) return;
            foreach (var row in raw.Split('\n'))
            {
                var f = row.Split('|');
                if (f.Length < 12) continue;
                var l = new Letter
                {
                    id = Int(f[0]), sender = f[1], place = f[2],
                    lines = f[3].Length > 0 ? f[3].Split('~') : new string[0],
                    questItem = f[4], questQty = Int(f[5]), questText = f[6],
                    coins = Int(f[8]), read = f[9] == "1", claimed = f[10] == "1", arrivedDay = Int(f[11])
                };
                foreach (var r in f[7].Split(','))
                {
                    int c = r.IndexOf(':');
                    if (c > 0) l.rewards.Add(new KeyValuePair<string, int>(r.Substring(0, c), Int(r.Substring(c + 1))));
                }
                _letters.Add(l);
                _nextId = Mathf.Max(_nextId, l.id + 1);
            }
        }

        static int Int(string s) => int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out int v) ? v : 0;

        // ---------- window ----------

        public static void Open()
        {
            Load();
            IsOpen = true;
            _selected = _letters.Count - 1;
            if (_selected >= 0) MarkRead(_letters[_selected]);
            Driver?.Render();
        }

        public static void Close()
        {
            IsOpen = false;
            if (Driver != null && Driver._canvas != null) Destroy(Driver._canvas.gameObject);
        }

        public static void Select(int index)
        {
            if (index < 0 || index >= _letters.Count) return;
            _selected = index;
            MarkRead(_letters[index]);
            Driver?.Render();
        }

        Canvas _canvas;

        void Render()
        {
            if (!IsOpen) return;
            if (_canvas != null) Destroy(_canvas.gameObject);
            _canvas = UiKit.Canvas("MailboxWindow", 245);
            bool portrait = UiKit.IsPortrait;
            float w = portrait ? 260f : 340f, h = portrait ? 330f : 190f;

            // Dim the game behind the window; it also catches taps (a tap outside closes, like DONG).
            var dim = UiKit.Rect(_canvas.transform, "Dim").gameObject.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.45f);
            dim.raycastTarget = true;
            UiKit.Fill(dim);
            dim.gameObject.AddComponent<Button>().onClick.AddListener(Close);

            var frame = UiKit.Rect(_canvas.transform, "Panel").gameObject.AddComponent<Image>();
            frame.color = Border;
            frame.raycastTarget = true;
            UiKit.Place(frame, UiKit.Center, UiKit.Center, Vector2.zero, new Vector2(w, h));
            var panel = UiKit.Rect(frame.transform, "Body").gameObject.AddComponent<Image>();
            panel.color = Slate;
            UiKit.Fill(panel, 1f);
            var root = panel.transform;

            At(UiKit.Label(root, "Title", "HOP THU", 8, Pal.TextW), 6f, 4f, 120f, 12f);
            // One close control only: the DONG button at the bottom (Esc / tapping it).
            var div = UiKit.Rect(root, "Divider").gameObject.AddComponent<Image>();
            div.color = Border;
            div.raycastTarget = false;
            At(div, 0f, 20f, w - 2f, 1f);

            float listW = portrait ? w - 14f : 124f;
            int shown = 0;
            for (int i = _letters.Count - 1; i >= 0; i--, shown++) Row(root, i, 6f, 24f + shown * 16f, listW);

            float bx = portrait ? 6f : 138f;
            float by = portrait ? 24f + Mathf.Max(1, _letters.Count) * 16f + 6f : 24f;
            float bw = portrait ? w - 14f : w - bx - 8f;
            var letter = _selected >= 0 && _selected < _letters.Count ? _letters[_selected] : null;
            if (letter == null)
            {
                At(UiKit.Label(root, "Empty", "KHONG CO THU", 8, Grey), bx, by, bw, 10f);
            }
            else
            {
                Body(root, letter, bx, by, bw);
                var claim = UiKit.Button(root, "Claim", "Secondary", "NHAN", new Vector2(54f, 16f), () => Claim(letter));
                claim.interactable = letter.CanClaim;
                var label = claim.GetComponentInChildren<Text>();
                if (label != null) label.color = letter.CanClaim ? Green : Grey;
                if (letter.CanClaim) { var o = claim.gameObject.AddComponent<Outline>(); o.effectColor = Green; o.effectDistance = new Vector2(1f, -1f); }
                At(claim, w - 122f, h - 22f, 54f, 16f);
                UiKit.Reach(claim, 56f, 30f);
            }
            var done = UiKit.Button(root, "Done", "Secondary", "DONG", new Vector2(54f, 16f), Close);
            At(done, w - 62f, h - 22f, 54f, 16f);
            UiKit.Reach(done, 56f, 30f);
        }

        void Row(Transform root, int index, float x, float y, float width)
        {
            var l = _letters[index];
            var bg = UiKit.Rect(root, "Row" + l.id).gameObject.AddComponent<Image>();
            bg.color = Slot;
            var btn = bg.gameObject.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(() => Select(index));
            if (index == _selected) { var o = bg.gameObject.AddComponent<Outline>(); o.effectColor = Gold; o.effectDistance = new Vector2(1f, -1f); }
            At(bg, x, y, width, 15f);
            var icon = UiKit.Rect(bg.transform, "Icon").gameObject.AddComponent<Image>();
            icon.sprite = Resources.Load<Sprite>(l.HasReward && !l.HasQuest ? "World/HomeDistrict/HD_Icon_Gift" : "World/HomeDistrict/HD_Icon_Letter");
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            At(icon, 1f, 1f, 13f, 13f);
            At(UiKit.Label(bg.transform, "Sender", l.sender, 8, Pal.TextW), 16f, 2f, width - 66f, 11f);
            // Read but reward still waiting: say so, so it is not mistaken for a finished letter.
            bool gift = l.read && l.CanClaim;
            var state = UiKit.Label(bg.transform, "State", gift ? "CO QUA" : l.read ? "DA DOC" : "MOI", 8,
                gift ? Gold : l.read ? Grey : Cyan, TextAnchor.MiddleRight);
            At(state, width - 52f, 2f, 50f, 11f);
        }

        void Body(Transform root, Letter l, float x, float y, float width)
        {
            At(UiKit.Label(root, "From", "TU: " + l.sender + " - " + l.place, 8, Cyan), x, y, width, 10f);
            y += 14f;
            foreach (var line in l.lines)
            {
                At(UiKit.Label(root, "Line", line, 8, Pal.TextW), x, y, width, 10f);
                y += 11f;
            }
            if (l.HasQuest)
            {
                y += 3f;
                string q = "NHIEM VU: " + (string.IsNullOrEmpty(l.questText) ? l.questItem : l.questText)
                    + (l.claimed ? "" : " (" + Mathf.Min(WorldSave.GetQty(l.questItem), l.questQty) + "/" + l.questQty + ")");
                At(UiKit.Label(root, "Quest", q, 8, Gold), x, y, width, 20f);
                y += 22f;
            }
            if (!l.HasReward) return;
            y += 3f;
            At(UiKit.Label(root, "RewardTitle", "THUONG:", 8, Pal.TextW), x, y, 60f, 10f);
            y += 12f;
            float sx = x;
            if (!l.claimed)
                foreach (var r in l.rewards)
                {
                    var slot = UiKit.Rect(root, "Slot").gameObject.AddComponent<Image>();
                    slot.color = Slot;
                    slot.raycastTarget = false;
                    var o = slot.gameObject.AddComponent<Outline>();
                    o.effectColor = Bevel;
                    o.effectDistance = new Vector2(1f, -1f);
                    At(slot, sx, y, 18f, 18f);
                    var icon = UiKit.Icon(slot.transform, UiKit.ItemIcon(r.Key), UiKit.Center, Vector2.zero, 14f);
                    if (!icon.enabled) { icon.sprite = FarmArt.Item(r.Key); icon.enabled = icon.sprite != null; }
                    var count = UiKit.Label(slot.transform, "Count", r.Value.ToString(), 6, Pal.TextW, TextAnchor.LowerRight);
                    UiKit.Fill(count);
                    sx += 22f;
                }
            if (l.coins > 0 && !l.claimed)
                At(UiKit.Label(root, "Coins", "+" + l.coins + " XU", 8, Gold), sx + 2f, y + 4f, 70f, 10f);
            if (l.claimed)
                At(UiKit.Label(root, "Claimed", "DA NHAN", 8, Grey), x, y, 70f, 10f);
        }

        static void At(Component c, float x, float y, float w, float h)
        {
            UiKit.Place(c, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(x, -y), new Vector2(w, h));
        }

        Coroutine _banners;

        void Banners(List<KeyValuePair<string, int>> rewards, int coins)
        {
            if (_banners != null) StopCoroutine(_banners);
            _banners = StartCoroutine(BannerRun(rewards, coins));
        }

        IEnumerator BannerRun(List<KeyValuePair<string, int>> rewards, int coins)
        {
            foreach (var r in rewards)
            {
                RewardBanner.Show(r.Key, r.Value);
                yield return new WaitForSecondsRealtime(2.2f);
            }
            if (coins > 0) RewardBanner.Show("coin", coins);
            _banners = null;
        }
    }

    /// <summary>The fixed mailbox in the home yard: DOC THU prompt, NewMail sprite and a bobbing "!" while mail is unread.</summary>
    public class MailboxView : MonoBehaviour
    {
        SpriteRenderer _sr, _bang;
        Sprite _closed, _newMail;
        WorldInteractable _talk;
        Transform _hero;
        float _next;

        void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            _closed = _sr != null ? _sr.sprite : null;
            _newMail = Resources.Load<Sprite>("World/HomeDistrict/HD_Mailbox_NewMail");
            _talk = gameObject.AddComponent<WorldInteractable>();
            _talk.DisplayName = "DOC THU";
            _talk.Radius = 1f;
            _talk.OnInteract = Mailbox.Open;
            var go = new GameObject("MailFlag");
            go.transform.SetParent(transform, false);
            _bang = go.AddComponent<SpriteRenderer>();
            _bang.sprite = AlertDot();
            _bang.enabled = false;
        }

        static Sprite _dot;

        /// <summary>9x9 pixel red badge with a white "!" (16 px per unit), readable over the mailbox art.</summary>
        static Sprite AlertDot()
        {
            if (_dot != null) return _dot;
            string[] rows =
            {
                "..kkkkk..", ".krrrrrk.", "krrrwrrrk", "krrrwrrrk", "krrrwrrrk", "krrrrrrrk", "krrrwrrrk", ".krrrrrk.", "..kkkkk..",
            };
            var tex = new Texture2D(9, 9, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            var ink = new Color32(0x14, 0x10, 0x18, 0xFF);
            var red = new Color32(0xE0, 0x48, 0x48, 0xFF);
            var white = new Color32(0xF4, 0xF4, 0xF0, 0xFF);
            for (int y = 0; y < 9; y++)
                for (int x = 0; x < 9; x++)
                {
                    char c = rows[8 - y][x];
                    tex.SetPixel(x, y, c == 'k' ? (Color)ink : c == 'r' ? (Color)red : c == 'w' ? (Color)white : Color.clear);
                }
            tex.Apply();
            _dot = Sprite.Create(tex, new Rect(0, 0, 9, 9), new Vector2(0.5f, 0f), 16f);
            return _dot;
        }

        void LateUpdate()
        {
            if (_sr == null) return;
            if (Time.unscaledTime >= _next)
            {
                _next = Time.unscaledTime + 0.25f;
                bool unread = Mailbox.HasUnread;
                var want = unread && _newMail != null ? _newMail : _closed;
                if (_sr.sprite != want) _sr.sprite = want;
                // "!" also for a gift still waiting in an already-read letter.
                _bang.enabled = (unread || Mailbox.HasGift) && _bang.sprite != null;
                if (_hero == null) { var p = GameObject.FindGameObjectWithTag("Player"); if (p != null) _hero = p.transform; }
                if (Mailbox.IsOpen && _hero != null && ((Vector2)(_hero.position - transform.position)).sqrMagnitude > 2.5f * 2.5f)
                    Mailbox.Close();
            }
            if (_bang.enabled)
            {
                float top = _sr.sprite != null ? _sr.sprite.rect.height / 16f : 1.5f;
                float bob = Mathf.Round((Mathf.Sin(Time.time * 4f) * 0.5f + 0.5f) * 2f) / 16f;
                _bang.transform.localPosition = new Vector3(0f, top + 2f / 16f + bob, 0f);
                _bang.sortingOrder = _sr.sortingOrder + 1;
                _bang.sortingLayerID = _sr.sortingLayerID;
            }
        }

        void OnDisable()
        {
            if (Mailbox.IsOpen) Mailbox.Close();
        }
    }
}
