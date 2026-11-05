using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>
    /// Shop, storage, smith, quests, map, clock, and the training dummy.
    /// Uses WorldSave and WorldContent. Does not own the hero or the dungeon flow.
    /// </summary>
    public class WorldServices : MonoBehaviour
    {
        public static WorldServices Instance { get; private set; }

        public static string LastResult { get; private set; } = "";

        Text _body;
        Text _mapText;
        Text _clockText;
        Text _nextStep;
        GameObject _nextStepPlate;
        string _nextSource = "";
        SpriteRenderer _shade;
        GameObject _panel;
        float _clock;
        bool _rain;
        float _sellScroll = 1f, _buyScroll = 1f;
        bool _shopSell;
        string _shopId = "";
        int _shopN = 1;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() => Instance = null;

        public static void Install(WorldNavigator nav, Transform hub, Transform training, Transform resource)
        {
            if (nav == null || Instance != null) return;
            var host = new GameObject("WorldServices");
            host.transform.SetParent(nav.transform, false);
            var services = host.AddComponent<WorldServices>();
            services.BuildUi();
            services.BuildNpcs(nav, hub);
            services.BuildHerb(resource);
            services.BuildDummy(training);
            services.BuildFutureGate(nav, hub);
        }

        public static void NotifyZone(WorldZone zone)
        {
            if (Instance == null || Instance._mapText == null) return;
            string here = zone.ToString();
            if (zone == WorldZone.Field && WorldNavigator.Instance != null)
            {
                var map = WorldFoundation.FindField(WorldNavigator.Instance.FieldId);
                if (!string.IsNullOrEmpty(map.name)) here = map.name;
            }
            Instance._mapText.text = "Here: " + here;
            // Zone plate is a village HUD element: hidden on the menu and in dungeons.
            Instance._mapText.transform.parent.gameObject.SetActive(zone != WorldZone.None);
            Instance.RefreshNextStep(zone);
        }

        void RefreshNextStep(WorldZone zone)
        {
            if (_nextStepPlate == null) return;
            string line = zone == WorldZone.Housing ? NextLine() : "";
            bool show = line.Length > 0;
            if (_nextStepPlate.activeSelf != show) _nextStepPlate.SetActive(show);
            if (!show || line == _nextSource) return;
            _nextSource = line;
            _nextStep.text = line;
        }

        static string NextLine()
        {
            switch (Housing.HousingNextStep())
            {
                case HousingStep.HarvestReady: return "Next: harvest " + CropLabel(Housing.StepDetail);
                case HousingStep.ClaimMail: return "Next: claim the gift";
                case HousingStep.CollectEggs: return "Next: pick up the egg";
                case HousingStep.NeedResources: return "Next: gather wood and stone";
                case HousingStep.BuildHouse: return "Next: build the house";
                case HousingStep.HouseBuilding: return "Next: the house is being built";
                case HousingStep.PlantFirstBed: return "Next: plant a crop";
                case HousingStep.ExpandLand: return "Next: expand the land";
                case HousingStep.CheckMail: return "Next: read the mail";
                default: return "";
            }
        }

        static string CropLabel(string id)
        {
            var item = WorldContent.FindItem(id);
            return string.IsNullOrEmpty(item.displayName) ? id : item.displayName;
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            WorldEvents.ItemGained += OnItem;
            WorldEvents.EnemyKilled += OnKill;
            WorldEvents.DungeonCleared += OnDungeon;
            WorldEvents.BossDefeated += OnBoss;
            WorldEvents.ExpGained += OnExp;
            WorldEvents.CoinsChanged += OnCoins;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            WorldEvents.ItemGained -= OnItem;
            WorldEvents.EnemyKilled -= OnKill;
            WorldEvents.DungeonCleared -= OnDungeon;
            WorldEvents.BossDefeated -= OnBoss;
            WorldEvents.ExpGained -= OnExp;
            WorldEvents.CoinsChanged -= OnCoins;
        }

        void Update()
        {
            _clock += Time.unscaledDeltaTime;
            float length = Mathf.Max(10f, WorldContent.DayLengthSeconds);
            float t = Mathf.Repeat(_clock, length) / length;
            bool night = t > 0.65f;
            _rain = night && t > 0.82f;
            if (_clockText != null)
                _clockText.text = (night ? "Night" : "Day") + (_rain ? "  Rain" : "  Clear");
            var nav = WorldNavigator.Instance;
            RefreshNextStep(nav != null ? nav.Current : WorldZone.None);
            DayNight.Set(t, _rain);
            ShadeWorld(t);
        }

        public static bool Buy(string itemId, int count = 1)
        {
            var item = WorldContent.FindItem(itemId);
            int each = PayPrice(item);
            if (string.IsNullOrEmpty(item.id) || each <= 0) return Fail("Cannot buy " + itemId);
            int max = MaxBuy(item);
            if (max <= 0)
            {
                if (Gear.IsGem(itemId) && Gear.GemOwned(itemId) >= Gear.GemTypeLimit) return Fail(Deny.LimitReached, "Limit 3 " + item.displayName);
                if (Gear.IsEquipment(itemId) && Gear.OwnedCopies(itemId) > 0) return Fail(Deny.LimitReached, "Already own " + item.displayName);
                if (BagFull(itemId)) return Fail(Deny.BagFull, "Bag full");
                return Fail(Deny.InsufficientResources, "Not enough coins");
            }
            count = Mathf.Clamp(count, 1, max);
            int price = each * count;
            WorldTx.Begin()
                .SetInt(WorldSave.KeyCoins, WorldSave.Coins - price)
                .SetInt(WorldSave.KeyItem(itemId), WorldSave.GetQty(itemId) + count)
                .After(() => WorldEvents.RaiseCoins(-price))
                .After(() => WorldEvents.RaiseItemGained(itemId, count))
                .Commit();
            return Ok("Bought " + count + " " + item.displayName);
        }

        static int PayPrice(ItemDef item) => item.buy > 0 ? item.buy : item.sell;

        static int MaxBuy(ItemDef item)
        {
            int each = PayPrice(item);
            if (string.IsNullOrEmpty(item.id) || each <= 0) return 0;
            int byCoins = WorldSave.Coins / each;
            if (byCoins <= 0) return 0;
            int room = int.MaxValue;
            if (Gear.IsGem(item.id)) room = Gear.GemTypeLimit - Gear.GemOwned(item.id);
            else if (Gear.IsEquipment(item.id)) room = Gear.OwnedCopies(item.id) > 0 ? 0 : 1;
            else if (item.stack > 0) room = item.stack - WorldSave.GetQty(item.id);
            return room <= 0 ? 0 : Mathf.Min(byCoins, room);
        }

        public static bool Sell(string itemId, int count = 1)
        {
            var item = WorldContent.FindItem(itemId);
            if (string.IsNullOrEmpty(item.id) || item.sell <= 0) return Fail("Cannot sell " + itemId);
            int have = WorldSave.GetQty(itemId);
            if (have <= 0) return Fail(Deny.NotOwned, "None to sell");
            count = Mathf.Clamp(count, 1, have);
            int price = item.sell * count;
            WorldTx.Begin()
                .SetInt(WorldSave.KeyItem(itemId), have - count)
                .SetInt(WorldSave.KeyCoins, WorldSave.Coins + price)
                .After(() => WorldEvents.RaiseCoins(price))
                .After(PendingRewards.TryFulfill)
                .Commit();
            return Ok("Sold " + count + " " + item.displayName);
        }

        public static bool Deposit(string itemId)
        {
            int have = WorldSave.GetQty(itemId);
            if (have <= 0) return Fail(Deny.NotOwned, "Nothing to store");
            if (Gear.IsEquipped(itemId)) return Fail(Deny.Equipped, "Unequip it first");
            WorldTx.Begin()
                .SetInt(WorldSave.KeyItem(itemId), have - 1)
                .SetInt(WorldSave.KeyStore(itemId), WorldSave.StoreQty(itemId) + 1)
                .After(PendingRewards.TryFulfill)
                .Commit();
            if (itemId == "training_blade") NoteBladeOwnership();
            return Ok("Stored " + itemId);
        }

        public static bool Withdraw(string itemId)
        {
            int stored = WorldSave.StoreQty(itemId);
            if (stored <= 0) return Fail(Deny.NotOwned, "Chest empty");
            if (BagFull(itemId)) return Fail(Deny.BagFull, "Bag full");
            WorldTx.Begin()
                .SetInt(WorldSave.KeyStore(itemId), stored - 1)
                .SetInt(WorldSave.KeyItem(itemId), WorldSave.GetQty(itemId) + 1)
                .After(Gear.Recompute)
                .Commit();
            return Ok("Took " + itemId);
        }

        public static bool CraftBlade() => Gear.Craft(WorldContent.Blade);

        public static bool CraftArmor() => Gear.Craft(WorldContent.Armor);

        static bool BagFull(string itemId)
        {
            int cap = WorldContent.StackLimit(itemId);
            return cap > 0 && WorldSave.GetQty(itemId) >= cap;
        }

        public static bool EquipBlade()
        {
            if (WorldSave.GetQty("training_blade") <= 0) return Fail(Deny.NotOwned, "No blade");
            bool ok = Gear.Equip("training_blade");
            NoteBladeOwnership();
            return ok;
        }

        public static bool AcceptQuest(string id)
        {
            var quest = WorldContent.FindQuest(id);
            if (string.IsNullOrEmpty(quest.id)) return Fail("No quest");
            if (WorldSave.QuestState(id) == 2) return Fail("Already done");
            if (WorldSave.QuestState(id) == 1)
            {
                NoteBladeOwnership();
                return Ok("Already on " + quest.title);
            }
            if (!string.IsNullOrEmpty(quest.requires) && WorldSave.QuestState(quest.requires) != 2)
                return Fail("Finish the previous step");
            WorldSave.SetQuestState(id, 1);
            NoteBladeOwnership();
            if (id == PendingRewards.StarterSeedQuest) PendingRewards.MarkEligible(PendingRewards.StarterSeed);
            return Ok("Accepted " + quest.title);
        }

        static bool OwnsTrainingBlade()
        {
            if (WorldSave.GetQty("training_blade") > 0) return true;
            if (WorldSave.StoreQty("training_blade") > 0) return true;
            return WorldSave.Equipped("weapon") == "training_blade";
        }

        /// <summary>
        /// q_road_craft counts a Training Blade the player already owns.
        /// Bag, weapon slot, and storage all qualify. Progress is written once and is not cleared.
        /// </summary>
        static void NoteBladeOwnership()
        {
            if (!OwnsTrainingBlade()) return;
            for (int i = 0; i < WorldContent.Quests.Length; i++)
            {
                var quest = WorldContent.Quests[i];
                if (quest.goal != QuestGoal.Gather || quest.target != "training_blade") continue;
                if (WorldSave.QuestState(quest.id) != 1) continue;
                int have = WorldSave.QuestProgress(quest.id);
                if (have >= quest.count) continue;
                Advance(quest, quest.count - have);
            }
        }

        void OnItem(string itemId, int amount)
        {
            var item = WorldContent.FindItem(itemId);
            string name = string.IsNullOrEmpty(item.displayName) ? itemId : item.displayName;
            Flash(name + " +" + amount);
            bool homeCrop = false;
            var crops = HomeFarm.Crops;
            for (int c = 0; c < crops.Length; c++)
                if (crops[c] == itemId) { homeCrop = true; break; }
            for (int i = 0; i < WorldContent.Quests.Length; i++)
            {
                var quest = WorldContent.Quests[i];
                if (quest.goal != QuestGoal.Gather && quest.goal != QuestGoal.Harvest) continue;
                bool sproutAtHome = homeCrop && quest.goal == QuestGoal.Harvest && quest.target == "sprout";
                if (quest.target != itemId && !sproutAtHome) continue;
                Advance(quest, amount);
            }
        }

        void OnKill(string groupId)
        {
            for (int i = 0; i < WorldContent.Quests.Length; i++)
            {
                var quest = WorldContent.Quests[i];
                if (quest.goal != QuestGoal.Kill || quest.target != groupId) continue;
                Advance(quest, 1);
            }
        }

        void OnDungeon(int level)
        {
            var pay = WorldContent.FindDungeonPayout(level.ToString());
            string key = "dungeon." + level;
            if (pay.once && WorldFlags.Get(key) == 1)
            {
                Flash("Dungeon " + level + " reward already claimed");
            }
            else
            {
                int coins = pay.coins;
                var tx = WorldTx.Begin();
                if (pay.once) tx.SetInt(WorldSave.KeyFlag(key), 1);
                if (coins > 0) tx.SetInt(WorldSave.KeyCoins, WorldSave.Coins + coins).After(() => WorldEvents.RaiseCoins(coins));
                tx.Commit();
                Flash("Dungeon " + level + " reward " + coins + " coins");
            }
            for (int i = 0; i < WorldContent.Quests.Length; i++)
            {
                var quest = WorldContent.Quests[i];
                if (quest.goal != QuestGoal.ClearDungeon) continue;
                if (quest.target != level.ToString()) continue;
                Advance(quest, 1);
            }
        }

        void OnExp(int amount, bool leveled)
        {
            Flash(leveled ? "Level " + WorldSave.Level + "  EXP +" + amount : "EXP +" + amount);
        }

        void OnCoins(int delta)
        {
            Flash(delta > 0 ? "Coins +" + delta : "Coins " + delta);
        }

        void OnBoss(string bossId)
        {
            for (int i = 0; i < WorldContent.Quests.Length; i++)
            {
                var quest = WorldContent.Quests[i];
                if (quest.goal != QuestGoal.DefeatBoss || quest.target != bossId) continue;
                Advance(quest, 1);
            }
        }

        static void Advance(QuestDef quest, int amount)
        {
            if (WorldSave.QuestState(quest.id) != 1) return;
            if (WorldSave.QuestProgress(quest.id) >= quest.count) return;
            int next = WorldSave.QuestProgress(quest.id) + amount;
            WorldSave.SetQuestProgress(quest.id, next);
            bool ready = next >= quest.count;
            Flash(quest.title + " " + Mathf.Min(next, quest.count) + "/" + quest.count + (ready ? " ready" : ""));
            LastResult = ready ? quest.title + " ready" : quest.title + " " + next;
        }

        public static bool ClaimQuest(string id)
        {
            var quest = WorldContent.FindQuest(id);
            if (string.IsNullOrEmpty(quest.id)) return Fail("No quest");
            if (WorldSave.QuestState(id) == 2) return Fail("Already claimed");
            if (WorldSave.QuestState(id) != 1 || WorldSave.QuestProgress(id) < quest.count)
                return Fail(Deny.NotEligible, "Not ready");
            string reward = quest.rewardItem;
            if (!string.IsNullOrEmpty(reward) && BagFull(reward))
            {
                if (id == "q_pet_intro")
                {
                    PendingRewards.MarkEligible(PendingRewards.PetEgg);
                    if (PendingRewards.Get(PendingRewards.PetEgg) == Entitlement.Granted)
                        return Ok("Claimed " + quest.title);
                    return Ok("Reward pending");
                }
                return Fail(Deny.BagFull, "Bag full");
            }
            int levelBefore = WorldSave.Level;
            int coins = quest.coinReward, exp = quest.expReward;
            bool opensNext = !string.IsNullOrEmpty(quest.nextId) && WorldSave.QuestState(quest.nextId) == 0;
            string nextId = quest.nextId;
            var tx = WorldTx.Begin().SetInt(WorldSave.KeyQuest(id), 2);
            if (coins > 0) tx.SetInt(WorldSave.KeyCoins, WorldSave.Coins + coins).After(() => WorldEvents.RaiseCoins(coins));
            if (exp > 0) tx.SetInt(WorldSave.KeyExp, WorldSave.Exp + exp).After(() => WorldEvents.RaiseExp(exp, WorldSave.Level > levelBefore));
            if (!string.IsNullOrEmpty(reward))
                tx.SetInt(WorldSave.KeyItem(reward), WorldSave.GetQty(reward) + 1).After(() => WorldEvents.RaiseItemGained(reward, 1));
            if (id == "q_pet_intro")
                tx.SetInt(PendingRewards.Key(PendingRewards.PetEgg), (int)Entitlement.Granted);
            if (!string.IsNullOrEmpty(quest.unlockFlag)) tx.SetInt(WorldSave.KeyFlag(quest.unlockFlag), 1);
            if (opensNext) tx.SetInt(WorldSave.KeyQuest(nextId), 1);
            tx.Commit();
            if (opensNext && nextId == PendingRewards.StarterSeedQuest) PendingRewards.MarkEligible(PendingRewards.StarterSeed);
            NoteBladeOwnership();
            var next = WorldContent.FindQuest(quest.nextId);
            string nextLine = string.IsNullOrEmpty(next.title) ? "" : "  Next " + next.title;
            return Ok("Claimed " + quest.title + nextLine);
        }

        void BuildNpcs(WorldNavigator nav, Transform hub)
        {
            for (int i = 0; i < WorldContent.Npcs.Length; i++)
            {
                var def = WorldContent.Npcs[i];
                var go = new GameObject(def.id);
                go.transform.SetParent(hub, false);
                go.transform.position = (Vector2)hub.position + def.offset;
                var visual = go.AddComponent<WorldVisual>();
                visual.Build(def.name, WorldGraybox.Npc(def.service), new Vector2(0.7f, 1.1f));
                var talk = go.AddComponent<WorldInteractable>();
                talk.DisplayName = def.name;
                talk.Line = def.line;
                string service = def.service;
                talk.OnInteract = () => Open(nav, service);
            }
        }

        void BuildHerb(Transform resource)
        {
            if (resource == null) return;
            var go = new GameObject("Herb");
            go.transform.SetParent(resource, false);
            go.transform.position = WorldCatalog.Resource.origin + new Vector2(0f, 2.4f);
            go.AddComponent<GatherNode>().Init(WorldContent.Herb);
        }

        void BuildDummy(Transform training)
        {
            if (training == null) return;
            var go = new GameObject("TrainingDummy");
            go.transform.SetParent(training, false);
            go.transform.position = WorldCatalog.Training.spawn + new Vector2(2.2f, 0.4f);
            go.AddComponent<WorldVisual>().Build("Dummy", new Color(0.75f, 0.75f, 0.8f, 1f), new Vector2(0.7f, 1.2f));
            var health = go.AddComponent<Health>();
            health.Configure(200, true);
            Hurtbox.Attach(go, new Vector2(0.7f, 1.1f));
            var labelGo = new GameObject("Hit");
            labelGo.transform.SetParent(go.transform, false);
            labelGo.transform.localPosition = new Vector3(0f, 1.5f, 0f);
            var text = WorldNameplate.Attach(labelGo, "Dummy", Color.white).Text;
            health.Damaged += (_, amount, __) => text.text = amount.ToString();
            health.Died += _ => health.ReviveFull();
        }

        void BuildFutureGate(WorldNavigator nav, Transform hub)
        {
            var go = new GameObject("FutureRegion");
            go.transform.SetParent(hub, false);
            go.transform.position = WorldCatalog.Hub.origin + new Vector2(0f, 7.3f);
            go.AddComponent<WorldVisual>().Build("Future", WorldGraybox.Locked, new Vector2(1.2f, 0.6f));
            var talk = go.AddComponent<WorldInteractable>();
            talk.DisplayName = "Future Region";
            talk.Radius = 0.75f;
            talk.Line = "Locked.";
            talk.OnInteract = () =>
            {
                bool open = WorldFlags.Get(WorldContent.Unlocks[0].flag) == 1;
                Show(open ? "The next region is marked. No road yet." : "Future region is locked.");
            };
        }

        public bool PanelOpen => _panel != null && _panel.activeSelf;

        public void HidePanel()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        /// <summary>Opens a village service window: shop, smith, quest, storage or map.</summary>
        public void OpenService(string service)
        {
            var nav = WorldNavigator.Instance;
            if (nav == null) return;
            Open(nav, service);
        }

        void Open(WorldNavigator nav, string service)
        {
            nav.CloseDungeonBoard();
            nav.CloseBag();
            if (_panel != null) _panel.SetActive(true);
            if (service == "shop") ShowShop();
            else if (service == "smith") ShowSmith();
            else if (service == "quest") ShowQuests();
            else if (service == "storage") ShowStorage();
            else if (service == "map")
            {
                if (_panel != null) _panel.SetActive(false);
                WorldMapPanel.Open();
            }
            else Show(service);
        }

        // ------------------------------------------------------------------ shop (P19 / L20)

        void ShowShop()
        {
            if (_win != null && _win.body != null)
            {
                if (_shopSell) _sellScroll = ScrollOf(_win.body, "Sell");
                else _buyScroll = ScrollOf(_win.body, "Buy");
            }
            Begin("MIRA'S SHOP");
            UiKit.Coins(_win.body, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, 2f)).text = WorldSave.Coins.ToString();
            var tabs = UiKit.Rect(_win.body, "Tabs");
            tabs.gameObject.AddComponent<UiLayout>().Both(UiLayout.Spec.Stretch(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
                Vector2.zero, new Vector2(-76f, 18f)));
            ShopTab(tabs, "BuyTab", "BUY", !_shopSell, false, () => { if (!_shopSell) return; _shopSell = false; _shopId = ""; ShowShop(); });
            ShopTab(tabs, "SellTab", "SELL", _shopSell, true, () => { if (_shopSell) return; _shopSell = true; _shopId = ""; ShowShop(); });
            if (!string.IsNullOrEmpty(_shopId) && ShopMax(_shopId) <= 0) _shopId = "";
            int rows = 0;
            for (int i = 0; i < WorldContent.Items.Length; i++)
            {
                var item = WorldContent.Items[i];
                if (_shopSell) { if (item.sell > 0 && WorldSave.GetQty(item.id) > 0) rows++; }
                else if (item.buy > 0 || item.sell > 0) rows++;
            }
            float foot = string.IsNullOrEmpty(_shopId) ? 0f : 36f;
            var full = UiLayout.Spec.Stretch(Vector2.zero, Vector2.one, new Vector2(0.5f, 1f), new Vector2(0f, -20f), new Vector2(0f, -20f - foot));
            var root = Section(_win.body, _shopSell ? "Sell" : "Buy", full, full);
            var list = ScrollList(root, rows * 34f, 0f);
            float y = 0f;
            if (_shopSell)
            {
                for (int i = 0; i < WorldContent.Items.Length; i++)
                {
                    var item = WorldContent.Items[i];
                    if (item.sell <= 0) continue;
                    int have = WorldSave.GetQty(item.id);
                    if (have <= 0) continue;
                    ShopRow(list, y, item, true, item.id == _shopId, "HAVE " + have, item.sell);
                    y += 34f;
                }
                KeepScroll(list, _sellScroll);
            }
            else
            {
                for (int i = 0; i < WorldContent.Items.Length; i++)
                {
                    var item = WorldContent.Items[i];
                    int price = PayPrice(item);
                    if (price <= 0) continue;
                    bool gem = Gear.IsGem(item.id);
                    string sub = gem
                        ? GearStatText(item.id) + "  " + Gear.GemOwned(item.id) + "/" + Gear.GemTypeLimit
                        : "HAVE " + WorldSave.GetQty(item.id);
                    ShopRow(list, y, item, ShopMax(item.id) > 0, item.id == _shopId, sub, price);
                    y += 34f;
                }
                KeepScroll(list, _buyScroll);
            }
            ShowQtyBar();
            LastResult = "Shop coins " + WorldSave.Coins;
        }

        void PickShop(string id)
        {
            if (_shopId == id) { _shopId = ""; ShowShop(); return; }
            if (ShopMax(id) <= 0) return;
            _shopId = id;
            _shopN = 1;
            ShowShop();
        }

        void SetShopN(int n)
        {
            int max = ShopMax(_shopId);
            if (max <= 0) { _shopId = ""; ShowShop(); return; }
            _shopN = Mathf.Clamp(n, 1, max);
            ShowShop();
        }

        void ConfirmShop()
        {
            if (string.IsNullOrEmpty(_shopId)) return;
            bool ok = _shopSell ? Sell(_shopId, _shopN) : Buy(_shopId, _shopN);
            int max = ShopMax(_shopId);
            if (!ok || max <= 0) _shopId = "";
            else _shopN = Mathf.Clamp(_shopN, 1, max);
            ShowShop();
        }

        int ShopMax(string id)
        {
            var item = WorldContent.FindItem(id);
            return _shopSell ? Mathf.Max(0, WorldSave.GetQty(id)) : MaxBuy(item);
        }

        void ShopRow(RectTransform list, float top, ItemDef item, bool on, bool selected, string sub, int price)
        {
            var line = Line(list, top, 32f, on ? (selected ? "Selected" : "") : "Disabled");
            var slot = UiKit.Slot(line, "Slot");
            slot.Set(UiKit.ItemIcon(item.id), 1);
            UiKit.Place(slot, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(22f, 22f));
            string name = string.IsNullOrEmpty(item.displayName) ? item.id : item.displayName.ToUpperInvariant();
            Txt(line, name, Pal.GoldL, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 6f), new Vector2(200f, 10f), TextAnchor.MiddleLeft);
            Txt(line, sub, Pal.TextMute, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, -7f), new Vector2(200f, 10f), TextAnchor.MiddleLeft);
            UiKit.Icon(line, "UI_Icon_Currency", new Vector2(1f, 0.5f), new Vector2(-34f, 0f), 12f);
            Txt(line, price.ToString(), Pal.GoldL, new Vector2(1f, 0.5f), new Vector2(0f, 0.5f), new Vector2(-18f, 0f), new Vector2(20f, 12f), TextAnchor.MiddleLeft);
            if (!on) return;
            string id = item.id;
            var hit = UiKit.Rect(line, "Hit");
            var img = hit.gameObject.AddComponent<Image>();
            img.color = Color.clear;
            hit.gameObject.AddComponent<UiLayout>().Both(UiLayout.Spec.Stretch(Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero));
            hit.gameObject.AddComponent<ShopRowHit>().Click = () => PickShop(id);
        }

        void ShowQtyBar()
        {
            if (string.IsNullOrEmpty(_shopId)) return;
            int max = ShopMax(_shopId);
            if (max <= 0) return;
            _shopN = Mathf.Clamp(_shopN, 1, max);
            var item = WorldContent.FindItem(_shopId);
            int n = _shopN;
            int unit = _shopSell ? item.sell : PayPrice(item);
            var bar = UiKit.Rect(_win.body, "Qty");
            bar.gameObject.AddComponent<UiLayout>().Both(UiLayout.Spec.Stretch(new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f),
                new Vector2(0f, 2f), new Vector2(0f, 32f)));
            UiKit.Fill(UiKit.Row(bar, "Bg", "", Vector2.zero));
            var slot = UiKit.Slot(bar, "Slot");
            slot.Set(UiKit.ItemIcon(item.id), 1);
            UiKit.Place(slot, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(4f, 0f), new Vector2(22f, 22f));
            string name = string.IsNullOrEmpty(item.displayName) ? item.id : item.displayName.ToUpperInvariant();
            Txt(bar, name, Pal.GoldL, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 6f), new Vector2(80f, 10f), TextAnchor.MiddleLeft);
            Txt(bar, unit + "c EACH", Pal.TextMute, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, -7f), new Vector2(80f, 10f), TextAnchor.MiddleLeft);
            QtyButton(bar, "Minus", "-", 280f, 18f, n > 1, false, () => SetShopN(n - 1));
            Txt(bar, n.ToString(), Pal.GoldL, new Vector2(1f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-263f, 0f), new Vector2(26f, 14f), TextAnchor.MiddleCenter);
            QtyButton(bar, "Plus", "+", 228f, 18f, n < max, false, () => SetShopN(n + 1));
            QtyButton(bar, "Q1", "1", 198f, 20f, true, n == 1, () => SetShopN(1));
            QtyButton(bar, "Q10", "10", 172f, 22f, max >= 10, n == 10, () => SetShopN(Mathf.Min(10, max)));
            QtyButton(bar, "Q100", "100", 142f, 26f, max >= 100, n == 100, () => SetShopN(Mathf.Min(100, max)));
            QtyButton(bar, "All", "ALL", 108f, 30f, true, n == max && n != 1 && n != 10 && n != 100, () => SetShopN(max));
            UiKit.Icon(bar, "UI_Icon_Currency", new Vector2(1f, 0.5f), new Vector2(-94f, 0f), 12f);
            Txt(bar, (n * unit).ToString(), Pal.GoldL, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-58f, 0f), new Vector2(24f, 12f), TextAnchor.MiddleRight);
            QtyButton(bar, "Ok", _shopSell ? "SELL" : "BUY", 4f, 48f, true, true, ConfirmShop);
        }

        static void ShopTab(RectTransform parent, string name, string label, bool on, bool right, UnityEngine.Events.UnityAction act)
        {
            var b = UiKit.Button(parent, name, on ? "Primary" : "Secondary", label, new Vector2(80f, 18f), act);
            b.GetComponent<UiLayout>().Both(UiLayout.Spec.Stretch(
                new Vector2(right ? 0.5f : 0f, 0f), new Vector2(right ? 1f : 0.5f, 1f), new Vector2(0.5f, 0.5f),
                new Vector2(right ? -1f : 1f, 0f), new Vector2(-4f, 0f)));
        }

        static float ScrollOf(Transform body, string section)
        {
            var view = body.Find(section + "/View");
            if (view != null && view.TryGetComponent<ScrollRect>(out var scroll))
                return scroll.verticalNormalizedPosition;
            return 1f;
        }

        void KeepScroll(RectTransform content, float normalized)
        {
            var scroll = content != null ? content.GetComponentInParent<ScrollRect>() : null;
            if (scroll == null) return;
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = normalized;
            StartCoroutine(ApplyScroll(scroll, normalized));
        }

        static System.Collections.IEnumerator ApplyScroll(ScrollRect scroll, float normalized)
        {
            yield return null;
            if (scroll == null) yield break;
            Canvas.ForceUpdateCanvases();
            scroll.verticalNormalizedPosition = normalized;
        }

        static void QtyButton(RectTransform line, string name, string label, float fromRight, float width, bool on, bool lit, UnityEngine.Events.UnityAction act)
        {
            var b = UiKit.Button(line, name, lit ? "Primary" : "Secondary", label, new Vector2(width, 18f), act);
            b.interactable = on;
            Corner(b, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-fromRight, 0f));
        }

        // ------------------------------------------------------------------ smith (P20 / L21)

        void ShowSmith()
        {
            Begin("BRAM'S FORGE");
            Head(_win.body, "RECIPES", 0f);
            float y = 18f;
            int ownedAll = 0;
            foreach (var recipe in new[] { WorldContent.Blade, WorldContent.Armor })
            {
                var rec = recipe;
                var result = WorldContent.FindItem(rec.result);
                int owned = Gear.OwnedCopies(rec.result);
                ownedAll += owned;
                int a = WorldSave.GetQty(rec.a), b = WorldSave.GetQty(rec.b);
                bool enough = a >= rec.aCount && b >= rec.bCount;
                var line = ItemLine(_win.body, y, result, owned, owned > 0 ? "Selected" : "", GearStatText(rec.result));
                // Gear.Craft validates resources, the one-copy limit and bag space again.
                RightButton(line, "Positive", owned > 0 ? "OWNED" : "CRAFT", owned == 0 && enough, () => { Gear.Craft(rec); ShowSmith(); });
                y += 28f;
                Txt(_win.body, "NEED " + rec.aCount + " " + rec.a.ToUpperInvariant() + " (" + a + ")  " + rec.bCount + " " + rec.b.ToUpperInvariant() + " (" + b + ")",
                    enough || owned > 0 ? Pal.GreenL : Pal.RedL, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -y), new Vector2(240f, 10f), TextAnchor.MiddleLeft);
                y += 16f;
            }
            Txt(_win.body, "Equip gear from the bag (item detail or HERO tab).", Pal.TextMute,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(2f, -(y + 6f)), new Vector2(240f, 22f), TextAnchor.UpperLeft);
            LastResult = "Smith owned " + ownedAll;
        }

        // ------------------------------------------------------------------ quests (P17 / L19)

        void ShowQuests()
        {
            Begin("QUESTS");
            var current = CurrentRoad();
            if (string.IsNullOrEmpty(_questFocus))
                _questFocus = WorldSave.QuestState("q_pet_intro") != 2 ? "q_pet_intro" : current.id;
            var shown = WorldContent.FindQuest(_questFocus);
            if (string.IsNullOrEmpty(shown.id)) shown = current;
            var list = Section(_win.body, "List",
                UiLayout.Spec.Stretch(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 200f)),
                UiLayout.Spec.Stretch(new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(0f, 0.5f), new Vector2(0f, 0f), new Vector2(-4f, 0f)));
            Head(list, "THE ROAD", 0f);
            float y = 16f;
            for (int i = 0; i < WorldContent.Quests.Length; i++)
            {
                var quest = WorldContent.Quests[i];
                if (quest.chain != "road") continue;
                QuestRow(list, y, quest, quest.id == shown.id);
                y += 24f;
            }
            Head(list, "VILLAGE", y + 2f);
            y += 18f;
            for (int i = 0; i < WorldContent.Quests.Length; i++)
            {
                var quest = WorldContent.Quests[i];
                if (!string.IsNullOrEmpty(quest.chain)) continue;
                // Compact rows keep a button at the right edge, so the progress text moves left of it.
                var line = QuestRow(list, y, quest, quest.id == shown.id, WorldSave.QuestState(quest.id) == 2 ? 0f : 48f);
                QuestButtons(line, quest, true);
                y += 24f;
            }

            var detail = Section(_win.body, "Detail",
                UiLayout.Spec.Stretch(new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f), Vector2.zero, new Vector2(0f, 150f)),
                UiLayout.Spec.Stretch(new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(1f, 0.5f), new Vector2(0f, 0f), new Vector2(-4f, 0f)));
            var parch = UiKit.Panel(detail, "Parchment", "UI_Panel_Parchment");
            UiKit.Fill(parch);
            if (string.IsNullOrEmpty(shown.id))
            {
                Txt(detail, "No quest.", Pal.TextDark, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(160f, 10f), TextAnchor.MiddleCenter, false);
            }
            else
            {
                int prog = WorldSave.QuestProgress(shown.id);
                bool pet = shown.id == "q_pet_intro";
                Txt(detail, pet ? "VILLAGE" : "THE ROAD", Pal.WoodM, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -10f), new Vector2(200f, 10f), TextAnchor.MiddleLeft, false);
                Txt(detail, shown.title.ToUpperInvariant(), Pal.TextDark, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -24f), new Vector2(220f, 12f), TextAnchor.MiddleLeft, false);
                Txt(detail, pet ? QuestStory(shown) : GoalText(shown), Pal.TextDark, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, -40f), pet ? new Vector2(220f, 34f) : new Vector2(150f, 10f), TextAnchor.UpperLeft, false);
                Txt(detail, Mathf.Min(prog, shown.count) + "/" + shown.count, prog >= shown.count ? new Color(0.2f, 0.45f, 0.2f) : Pal.RedL,
                    new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-10f, pet ? -76f : -42f), new Vector2(60f, 10f), TextAnchor.MiddleRight, false);
                if (!pet)
                {
                    var bar = UiKit.Bar(detail, "Progress", "Progress", 100f, false, false);
                    bar.GetComponent<UiLayout>().Both(UiLayout.Spec.Stretch(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -52f), new Vector2(-20f, 10f)));
                    bar.Set(prog, Mathf.Max(1, shown.count), false);
                }
                string reward = pet
                    ? "REWARD  " + WorldContent.FindItem("mystery_pet_egg").displayName
                    : "REWARD  " + (shown.coinReward > 0 ? shown.coinReward + " COINS " : "") + (shown.expReward > 0 ? shown.expReward + " EXP" : "");
                Txt(detail, reward.Trim() == "REWARD" ? "REWARD  NEXT STEP" : reward, Pal.WoodM, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, pet ? -90f : -68f), new Vector2(220f, 10f), TextAnchor.MiddleLeft, false);
                Txt(detail, "GIVER  LARK", Pal.WoodM, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(10f, pet ? -102f : -82f), new Vector2(220f, 10f), TextAnchor.MiddleLeft, false);
                QuestButtons(detail, shown, false);
            }
            LastResult = string.IsNullOrEmpty(shown.title) ? "No quest" : shown.title + " " + Label(shown);
        }

        RectTransform QuestRow(RectTransform parent, float y, QuestDef quest, bool selected, float rightPad = 0f)
        {
            int state = WorldSave.QuestState(quest.id);
            bool locked = state == 0 && !string.IsNullOrEmpty(quest.requires) && WorldSave.QuestState(quest.requires) != 2;
            var line = Line(parent, y, 22f, selected ? "Selected" : (locked ? "Disabled" : ""));
            string icon = state == 2 ? "UI_Icon_Confirm" : locked ? "UI_Icon_Lock" : "UI_Icon_Quests";
            UiKit.Icon(line, icon, new Vector2(0f, 0.5f), new Vector2(12f, 0f));
            Color c = state == 2 ? Pal.GreenL : locked ? Pal.TextMute : Pal.GoldL;
            Txt(line, quest.title, c, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(24f, 0f), new Vector2(150f, 10f), TextAnchor.MiddleLeft);
            string right = state == 2 ? "DONE" : state == 1 ? Mathf.Min(WorldSave.QuestProgress(quest.id), quest.count) + "/" + quest.count : "";
            if (quest.id == "q_pet_intro" && state == 1 && PendingRewards.Get(PendingRewards.PetEgg) == Entitlement.Pending)
                right = "PENDING";
            Txt(line, right, state == 2 ? Pal.GreenL : Pal.TextW, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-6f - rightPad, 0f), new Vector2(50f, 10f), TextAnchor.MiddleRight);
            var hit = line.gameObject.AddComponent<Image>();
            hit.color = Color.clear;
            var pick = line.gameObject.AddComponent<Button>();
            pick.targetGraphic = hit;
            pick.transition = Selectable.Transition.None;
            string pickId = quest.id;
            pick.onClick.AddListener(() => { _questFocus = pickId; ShowQuests(); });
            return line;
        }

        static string QuestStory(QuestDef quest)
        {
            if (PendingRewards.Get(PendingRewards.PetEgg) == Entitlement.Pending)
                return "Túi đầy. Trứng sẽ vào túi khi có chỗ.";
            int state = WorldSave.QuestState(quest.id);
            if (state == 2)
                return "Hãy mở Túi Đồ và cho trứng nở. Người bạn bên trong sẽ nhận cháu làm chủ.";
            if (state == 1 && WorldSave.QuestProgress(quest.id) >= quest.count)
                return "Tốt lắm! Chiếc tổ đã sẵn sàng. Ta tin cháu sẽ chăm sóc tốt người bạn nhỏ này.";
            return "Ta có một quả trứng kỳ lạ. Bên trong có một người bạn đang chờ chủ. Hãy tìm 1 gỗ để chuẩn bị chỗ ở. Gỗ đang có sẵn không tính.";
        }

        void QuestButtons(RectTransform parent, QuestDef quest, bool compact)
        {
            int state = WorldSave.QuestState(quest.id);
            bool canAccept = state == 0 && (string.IsNullOrEmpty(quest.requires) || WorldSave.QuestState(quest.requires) == 2);
            bool canClaim = state == 1 && WorldSave.QuestProgress(quest.id) >= quest.count;
            string id = quest.id;
            if (compact)
            {
                if (state == 2) return;
                bool claim = state == 1;
                var b = UiKit.Button(parent, "Act", claim ? "Primary" : "Secondary", claim ? "CLAIM" : "TAKE", new Vector2(44f, 16f),
                    () => { if (claim) ClaimQuest(id); else AcceptQuest(id); ShowQuests(); });
                b.interactable = claim ? canClaim : canAccept;
                // Button at the row edge; QuestRow moves the progress text left of it.
                Corner(b, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-4f, 0f));
                return;
            }
            var accept = UiKit.Button(parent, "Accept", "Secondary", "ACCEPT", new Vector2(96f, 20f), () => { AcceptQuest(id); ShowQuests(); });
            accept.interactable = canAccept;
            Corner(accept, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(8f, 8f));
            var claimBtn = UiKit.Button(parent, "Claim", "Primary", "CLAIM", new Vector2(96f, 20f), () => { ClaimQuest(id); ShowQuests(); }, "UI_Icon_Confirm");
            claimBtn.interactable = canClaim;
            Corner(claimBtn, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-8f, 8f));
        }

        static string GoalText(QuestDef quest)
        {
            switch (quest.goal)
            {
                case QuestGoal.Kill: return "Defeat " + quest.target;
                case QuestGoal.Gather: return "Get " + WorldContent.FindItem(quest.target).displayName;
                case QuestGoal.Harvest: return "Harvest " + quest.target;
                case QuestGoal.ClearDungeon: return "Clear depth " + quest.target;
                case QuestGoal.DefeatBoss: return "Defeat " + quest.target;
                case QuestGoal.Talk: return "Talk to Lark";
                default: return quest.title;
            }
        }

        public static QuestDef CurrentRoad()
        {
            QuestDef offer = default;
            for (int i = 0; i < WorldContent.Quests.Length; i++)
            {
                var quest = WorldContent.Quests[i];
                if (quest.chain != "road") continue;
                int state = WorldSave.QuestState(quest.id);
                if (state == 1) return quest;
                if (state == 0 && (string.IsNullOrEmpty(quest.requires) || WorldSave.QuestState(quest.requires) == 2))
                    return quest;
                offer = quest;
            }
            return offer;
        }

        static string Label(QuestDef quest)
        {
            int state = WorldSave.QuestState(quest.id);
            if (state == 2) return "done";
            if (state == 1 && WorldSave.QuestProgress(quest.id) >= quest.count) return "ready";
            if (state == 1) return "active";
            return "new";
        }

        // ------------------------------------------------------------------ storage (P21 / L22)

        void ShowStorage()
        {
            Begin("NELL'S STORAGE");
            Head(_win.body, "ITEM", 0f);
            Txt(_win.body, "BAG / CHEST", Pal.TextMute, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-2f, -4f), new Vector2(120f, 10f), TextAnchor.MiddleRight);
            // Rows scroll inside a clipped list (like the shop); the bottom 14 px keep the stack-limit note clear.
            int rows = 0;
            for (int i = 0; i < WorldContent.Items.Length; i++) if (WorldContent.Items[i].kind != ItemKind.Currency) rows++;
            var listRoot = Section(_win.body, "Items",
                UiLayout.Spec.Stretch(Vector2.zero, Vector2.one, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, -14f)),
                UiLayout.Spec.Stretch(Vector2.zero, Vector2.one, new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, -14f)));
            var list = ScrollList(listRoot, rows * 26f);
            float y = 0f;
            for (int i = 0; i < WorldContent.Items.Length; i++)
            {
                var item = WorldContent.Items[i];
                if (item.kind == ItemKind.Currency) continue;
                int bag = WorldSave.GetQty(item.id);
                int chest = WorldSave.StoreQty(item.id);
                string id = item.id;
                var line = ItemLine(list, y, item, bag, bag + chest > 0 ? "" : "Disabled", "BAG " + bag + "  CHEST " + chest);
                var take = UiKit.Button(line, "Take", "Secondary", "TAKE", new Vector2(40f, 16f), () => { Withdraw(id); ShowStorage(); });
                take.interactable = chest > 0;
                Corner(take, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-4f, 0f));
                var store = UiKit.Button(line, "Store", "Secondary", "STORE", new Vector2(48f, 16f), () => { Deposit(id); ShowStorage(); });
                store.interactable = bag > 0;
                Corner(store, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-48f, 0f));
                y += 26f;
            }
            Txt(_win.body, "Stack limit " + WorldContent.StackLimit("ore") + " per item.", Pal.TextMute,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 2f), new Vector2(240f, 10f), TextAnchor.MiddleCenter);
            LastResult = "Bag ore " + WorldSave.GetQty("ore") + " chest ore " + WorldSave.StoreQty("ore");
        }

        // ------------------------------------------------------------------ world map (P23 / L08)

        void ShowMap(WorldNavigator nav)
        {
            Begin("WORLD MAP");
            if (_win == null || _win.body == null) return;
            var stops = new (string label, string icon, System.Action go)[]
            {
                ("VILLAGE", "UI_Icon_Housing", () => nav.Show(WorldZone.Hub)),
                ("FIELD", "UI_Icon_WorldMap", () => ShowFields(nav)),
                ("TRAINING", "UI_Icon_Training", () => nav.Show(WorldZone.Training)),
                ("HOUSING", "UI_Icon_Housing", () => nav.Show(WorldZone.Housing)),
                ("DUNGEONS", "UI_Icon_Dungeon", () => nav.OpenDungeonBoard()),
            };
            LayStops(stops);
            LastResult = "Map";
        }

        /// <summary>FIELD lists regions. Each region lists its maps. Enter travels by zone id, so more maps do not need a new travel path.</summary>
        void ShowFields(WorldNavigator nav)
        {
            Begin("FIELD");
            if (_win == null || _win.body == null) return;
            var regions = WorldFoundation.FieldRegions;
            var stops = new (string label, string icon, System.Action go)[regions.Length + 1];
            for (int i = 0; i < regions.Length; i++)
            {
                string id = regions[i].id;
                stops[i] = (regions[i].name, "UI_Icon_WorldMap", () => ShowRegion(nav, id));
            }
            stops[regions.Length] = ("BACK", "UI_Icon_Housing", () => ShowMap(nav));
            LayStops(stops);
            LastResult = "Field";
        }

        void ShowRegion(WorldNavigator nav, string regionId)
        {
            var region = WorldFoundation.FindFieldRegion(regionId);
            Begin(string.IsNullOrEmpty(region.name) ? "FIELD" : region.name);
            if (_win == null || _win.body == null) return;
            int maps = 0;
            for (int i = 0; i < WorldFoundation.FieldMaps.Length; i++)
                if (WorldFoundation.FieldMaps[i].regionId == regionId) maps++;
            var stops = new (string label, string icon, System.Action go)[maps + 1];
            int n = 0;
            for (int i = 0; i < WorldFoundation.FieldMaps.Length; i++)
            {
                var map = WorldFoundation.FieldMaps[i];
                if (map.regionId != regionId) continue;
                string zoneId = map.zoneId;
                stops[n++] = (map.name, "UI_Icon_WorldMap", () => ShowFieldEnter(nav, zoneId));
            }
            stops[n] = ("BACK", "UI_Icon_Housing", () => ShowFields(nav));
            LayStops(stops);
            LastResult = region.name;
        }

        void ShowFieldEnter(WorldNavigator nav, string zoneId)
        {
            var map = WorldFoundation.FindField(zoneId);
            Begin(string.IsNullOrEmpty(map.name) ? "FIELD" : map.name);
            if (_win == null || _win.body == null) return;
            string regionId = map.regionId;
            var stops = new (string label, string icon, System.Action go)[]
            {
                ("ENTER", "UI_Icon_WorldMap", () => nav.ShowField(zoneId)),
                ("BACK", "UI_Icon_Housing", () => ShowRegion(nav, regionId)),
            };
            LayStops(stops);
            LastResult = map.name;
        }

        void LayStops((string label, string icon, System.Action go)[] stops)
        {
            for (int i = 0; i < stops.Length; i++)
            {
                string label = stops[i].label;
                var act = stops[i].go;
                var b = UiKit.Button(_win.body, "Go_" + label, "Secondary", label, new Vector2(116f, 22f),
                    () => { HidePanel(); act(); }, stops[i].icon);
                if (b == null) continue;
                var layout = b.GetComponent<UiLayout>();
                if (layout == null) continue;
                int pc = i % 2, pr = i / 2, lc = i % 3, lr = i / 3;
                layout
                    .Portrait(UiLayout.Spec.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(pc == 0 ? -62f : 62f, -(4f + pr * 28f)), new Vector2(116f, 22f)))
                    .Landscape(UiLayout.Spec.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2((lc - 1) * 132f, -(4f + lr * 28f)), new Vector2(124f, 22f)));
            }
        }

        UiWindow _win;
        string _questFocus;

        void BuildUi()
        {
            var canvas = UiKit.Canvas("ServiceCanvas", 240);
            var canvasGo = canvas.gameObject;
            BuildShade();

            // Zone label + clock, top centre (village HUD).
            var zone = UiKit.Panel(canvasGo.transform, "ZonePlate", "UI_Panel_HUD");
            UiKit.Place(zone, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -8f), new Vector2(120f, 26f))
                .Portrait(UiLayout.Spec.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(112f, 26f)));
            _mapText = UiKit.Label(zone.transform, "Minimap", "Here: Hub", 8, Pal.TextBlue, TextAnchor.MiddleCenter);
            UiKit.Place(_mapText, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -3f), new Vector2(112f, 10f));
            _clockText = UiKit.Label(zone.transform, "Clock", "Day", 8, Pal.TextMute, TextAnchor.MiddleCenter);
            UiKit.Place(_clockText, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 3f), new Vector2(112f, 10f));
            zone.gameObject.SetActive(WorldNavigator.Instance != null && WorldNavigator.Instance.Current != WorldZone.None);

            // Cách A: one line under the zone name. Same dark plate and gold edge as the bottom hint.
            var nextBorder = UiKit.Rect(canvasGo.transform, "NextStep");
            nextBorder.gameObject.AddComponent<Image>().color = new Color32(0xB8, 0x9A, 0xA0, 0xFF);
            UiKit.Place(nextBorder, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(188f, 18f));
            var nextFill = UiKit.Rect(nextBorder, "Fill").gameObject.AddComponent<Image>();
            nextFill.color = new Color32(0x4A, 0x31, 0x40, 0xF0);
            nextFill.raycastTarget = false;
            UiKit.Fill(nextFill, 1f);
            _nextStep = UiKit.Label(nextFill.transform, "Text", "", 8, Pal.GoldL, TextAnchor.MiddleCenter);
            UiKit.Fill(_nextStep, 2f);
            _nextStep.horizontalOverflow = HorizontalWrapMode.Overflow;
            _nextStepPlate = nextBorder.gameObject;
            _nextStepPlate.SetActive(false);

            _win = UiKit.Window(canvasGo.transform, "ServicePanel", "VILLAGE", new Vector2(440f, 300f));
            _win.GetComponent<UiLayout>().Portrait(UiLayout.Spec.At(UiKit.Center, UiKit.Center, new Vector2(0f, 6f), new Vector2(272f, 420f)));
            _panel = _win.gameObject;
            _panel.SetActive(false);
        }

        void Begin(string title)
        {
            if (_win == null) BuildUi();
            if (_win == null) return;
            var nav = WorldNavigator.Instance;
            nav?.CloseBag();
            nav?.CloseDungeonBoard();
            _panel.SetActive(true);
            _win.SetTitle(title);
            _win.Clear();
        }

        /// <summary>World-space shade. Overlay UI canvases stay above it.</summary>
        void BuildShade()
        {
            var go = new GameObject("DayTintWorld");
            go.transform.SetParent(transform, false);
            _shade = go.AddComponent<SpriteRenderer>();
            _shade.sprite = ProceduralSprites.Pixel;
            _shade.sharedMaterial = ProceduralSprites.Unlit;
            _shade.sortingOrder = 30000;
            _shade.enabled = false;
        }

        void ShadeWorld(float t)
        {
            if (_shade == null) return;
            bool world = WorldNavigator.Instance != null && WorldNavigator.Instance.Current != WorldZone.None;
            _shade.enabled = world;
            if (!world) return;
            var cam = Camera.main;
            if (cam == null) return;
            var sprite = _shade.sprite;
            float height = cam.orthographicSize * 2f + 1f;
            float width = height * Mathf.Max(0.5f, cam.aspect) + 1f;
            _shade.transform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 0f);
            _shade.transform.localScale = new Vector3(
                width * sprite.pixelsPerUnit / sprite.rect.width,
                height * sprite.pixelsPerUnit / sprite.rect.height,
                1f);
            _shade.color = DayNight.Overlay(t);
        }

        void Show(string text)
        {
            LastResult = text;
            if (_win == null) return;
            Begin("VILLAGE");
            _body = Txt(_win.body, text, Pal.TextW, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -4f), new Vector2(240f, 120f), TextAnchor.UpperCenter);
        }

        /// <summary>Extra action buttons under the current text (kept for callers that still use it).</summary>
        void SetActions(params (string label, UnityEngine.Events.UnityAction action)[] actions)
        {
            if (_win == null) return;
            for (int i = 0; i < actions.Length; i++)
            {
                var b = UiKit.Button(_win.body, "Act" + i, i == 0 ? "Primary" : "Secondary", actions[i].label, new Vector2(160f, 22f), actions[i].action);
                Corner(b, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 4f + (actions.Length - 1 - i) * 26f));
            }
        }

        // ------------------------------------------------------------------ small layout helpers (kit pixels)

        static RectTransform Section(Transform parent, string name, UiLayout.Spec portrait, UiLayout.Spec landscape)
        {
            var rt = UiKit.Rect(parent, name);
            var layout = rt.gameObject.AddComponent<UiLayout>();
            layout.portrait = portrait;
            layout.landscape = landscape;
            layout.Apply(true);
            return rt;
        }

        /// <summary>Clipped, vertically scrolling list under an 18 px section head. Rows go into the returned content.</summary>
        static RectTransform ScrollList(RectTransform section, float contentPx, float headPx = 18f)
        {
            var view = UiKit.Rect(section, "View");
            view.gameObject.AddComponent<UiLayout>().Both(UiLayout.Spec.Stretch(Vector2.zero, Vector2.one, new Vector2(0.5f, 1f),
                new Vector2(0f, -headPx), new Vector2(0f, -headPx)));
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

        /// <summary>Full-width row at a top offset inside parent.</summary>
        static RectTransform Line(Transform parent, float topPx, float heightPx, string state)
        {
            var rt = UiKit.Rect(parent, "Line");
            rt.gameObject.AddComponent<UiLayout>().Both(UiLayout.Spec.Stretch(new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -topPx), new Vector2(0f, heightPx)));
            var bg = UiKit.Row(rt, "Bg", state, Vector2.zero);
            UiKit.Fill(bg);
            return rt;
        }

        static RectTransform ItemLine(Transform parent, float topPx, ItemDef item, int qty, string state, string sub, float heightPx = 26f)
        {
            var line = Line(parent, topPx, heightPx, state);
            var slot = UiKit.Slot(line, "Slot");
            slot.Set(UiKit.ItemIcon(item.id), qty);
            bool tall = heightPx > 30f;
            UiKit.Place(slot, new Vector2(0f, tall ? 1f : 0.5f), new Vector2(0f, tall ? 1f : 0.5f), new Vector2(2f, tall ? -1f : 0f), new Vector2(22f, 22f));
            Txt(line, string.IsNullOrEmpty(item.displayName) ? item.id : item.displayName.ToUpperInvariant(), Pal.GoldL,
                new Vector2(0f, tall ? 1f : 0.5f), new Vector2(0f, tall ? 1f : 0.5f), new Vector2(28f, tall ? -1f : 5f), new Vector2(tall ? 180f : 140f, 10f), TextAnchor.MiddleLeft);
            if (!string.IsNullOrEmpty(sub))
                Txt(line, sub, Pal.TextMute, new Vector2(0f, tall ? 1f : 0.5f), new Vector2(0f, tall ? 1f : 0.5f), new Vector2(28f, tall ? -12f : -6f), new Vector2(tall ? 180f : 150f, 10f), TextAnchor.MiddleLeft);
            return line;
        }

        static void RightButton(RectTransform line, string kind, string label, bool interactable, UnityEngine.Events.UnityAction click)
        {
            var b = UiKit.Button(line, label, kind, label, new Vector2(48f, 18f), click);
            b.interactable = interactable;
            Corner(b, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-4f, 0f));
        }

        static void Head(Transform parent, string text, float topPx)
        {
            Txt(parent, text, Pal.GoldL, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(2f, -topPx - 2f), new Vector2(160f, 10f), TextAnchor.MiddleLeft);
        }

        static Text Txt(Transform parent, string text, Color color, Vector2 anchor, Vector2 pivot, Vector2 posPx, Vector2 sizePx, TextAnchor align, bool shadow = true)
        {
            var t = UiKit.Label(parent, "Text", text, 8, color, align, shadow);
            UiKit.Place(t, anchor, pivot, posPx, sizePx);
            return t;
        }

        static void Corner(Component c, Vector2 anchor, Vector2 pivot, Vector2 posPx)
        {
            var layout = c.GetComponent<UiLayout>();
            var s = layout.landscape;
            layout.Both(UiLayout.Spec.At(anchor, pivot, posPx, s.size));
        }

        // ------------------------------------------------------------------ UI read helpers (no game rules here)

        /// <summary>Same combat / lava gate the progression actions use; the UI only shows it.</summary>
        public static Deny UiGate() => GameplayClock.GateApplies ? GameplayClock.CheckOutOfCombat() : Deny.None;

        /// <summary>"+5 POWER +20 HP" from the Gear tables, for equipment and gems.</summary>
        public static string GearStatText(string itemId)
        {
            GearStat s = default;
            foreach (var g in Gear.Equipment) if (g.id == itemId) s = g;
            foreach (var g in Gear.Gems) if (g.id == itemId) s = g;
            if (s.id == null) return "";
            var parts = new System.Collections.Generic.List<string>();
            if (s.power != 0) parts.Add("+" + s.power + " PWR");
            if (s.def != 0) parts.Add("+" + s.def + " DEF");
            if (s.maxHp != 0) parts.Add("+" + s.maxHp + " HP");
            return string.Join(" ", parts);
        }

        public static string SlotLabel(string slot)
        {
            switch (slot)
            {
                case "weapon": return "WEAPON";
                case "armor": return "ARMOR";
                case "accessory": return "ACCESSORY";
                default: return "";
            }
        }

        internal static bool Ok(string text)
        {
            Outcome.Last = Deny.None;
            LastResult = text;
            WorldNavigator.Instance?.Flash(text);
            return true;
        }

        internal static bool Fail(string text) => Fail(Deny.Invalid, text);

        internal static bool Fail(Deny reason, string text)
        {
            Outcome.Last = reason;
            LastResult = text;
            WorldNavigator.Instance?.Flash(text);
            return false;
        }

        static void Flash(string line) => WorldNavigator.Instance?.Flash(line);

        static Text NewText(Transform parent, string name, Vector2 anchor, Vector2 size, int font)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.sizeDelta = size;
            var text = go.AddComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.fontSize = font;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            LocText.Attach(text);
            return text;
        }

        static Button NewButton(Transform parent, string name, Vector2 anchor, string label, UnityEngine.Events.UnityAction click)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchor;
            rt.anchorMax = anchor;
            rt.sizeDelta = new Vector2(160f, 36f);
            var image = go.AddComponent<Image>();
            image.color = new Color(0.4f, 0.16f, 0.16f, 1f);
            var button = go.AddComponent<Button>();
            button.targetGraphic = image;
            button.onClick.AddListener(click);
            UiKit.Dress(button, "Primary");
            var text = NewText(go.transform, "Label", new Vector2(0.5f, 0.5f), new Vector2(150f, 32f), 14);
            text.text = label;
            return button;
        }

        static Image NewImage(Transform parent, string name)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<Image>();
        }
    }

    /// <summary>Shop row click that still lets the list scroll.</summary>
    public class ShopRowHit : MonoBehaviour, IPointerClickHandler, IInitializePotentialDragHandler, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public UnityEngine.Events.UnityAction Click;
        ScrollRect _scroll;
        bool _dragged;

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_dragged) Click?.Invoke();
            _dragged = false;
        }

        public void OnInitializePotentialDrag(PointerEventData eventData)
        {
            _scroll = GetComponentInParent<ScrollRect>();
            _dragged = false;
            if (_scroll != null) _scroll.OnInitializePotentialDrag(eventData);
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            _dragged = true;
            if (_scroll != null) _scroll.OnBeginDrag(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (_scroll != null) _scroll.OnDrag(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (_scroll != null) _scroll.OnEndDrag(eventData);
        }
    }
}
