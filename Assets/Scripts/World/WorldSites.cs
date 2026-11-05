using UnityEngine;

namespace GameplayPrototype
{
    public class TreasureChest : MonoBehaviour
    {
        public string Status { get; private set; } = "closed";

        public void Open()
        {
            if (WorldFlags.Get(WorldFoundation.ChestFlag) == 1)
            {
                Status = "empty";
                return;
            }
            WorldFlags.Set(WorldFoundation.ChestFlag, 1);
            WorldSave.AddCoins(WorldFoundation.ChestCoins);
            WorldSave.AddItem("quest_token", 1);
            Status = "opened";
        }
    }

    public class WardWatch : MonoBehaviour
    {
        EnemySpawner _spawner;
        int _left;

        public int Alive => _left;
        public string Status { get; private set; } = "idle";

        public void Bind(EnemySpawner spawner) => _spawner = spawner;

        public bool StartWatch()
        {
            if (WorldFlags.Get(WorldFoundation.WardFlag) == 2)
            {
                Status = "done";
                return false;
            }
            if (_left > 0) return true;
            var def = FindGoblin();
            if (def == null || _spawner == null)
            {
                Status = "no roster";
                return false;
            }
            for (int i = 0; i < 2; i++)
            {
                var pos = (Vector2)transform.position + new Vector2(i == 0 ? -1.2f : 1.2f, 1.6f);
                var go = _spawner.SpawnR2C(def, pos, false);
                if (go == null) continue;
                _left++;
                var health = go.GetComponent<Health>();
                if (health != null) health.Died += OnDied;
            }
            Status = _left > 0 ? "active" : "spawn failed";
            return _left > 0;
        }

        /// <summary>Leaving the map mid-watch does not finish it and does not pay the reward again.</summary>
        public void Abandon()
        {
            if (WorldFlags.Get(WorldFoundation.WardFlag) == 2) return;
            _left = 0;
            Status = "idle";
        }

        void OnDied(Health health)
        {
            _left = Mathf.Max(0, _left - 1);
            if (_left > 0) return;
            if (WorldFlags.Get(WorldFoundation.WardFlag) == 2) return;
            WorldFlags.Set(WorldFoundation.WardFlag, 2);
            WorldSave.AddCoins(WorldFoundation.WardCoins);
            Status = "cleared";
        }

        static R2CMonsterDef FindGoblin()
        {
            var roster = Resources.Load<R2CMonsterRoster>("R2C/R2CMonsterRoster");
            if (roster == null || roster.monsters == null) return null;
            for (int i = 0; i < roster.monsters.Length; i++)
            {
                var monster = roster.monsters[i];
                if (monster != null && monster.label == "Goblin Lv.3") return monster;
            }
            return null;
        }
    }

    /// <summary>
    /// Extra zones. The original hub, training, gather, farm, and Cyclops arena stay in WorldNavigator.
    /// </summary>
    public class WorldSites : MonoBehaviour
    {
        public static WorldSites Instance { get; private set; }

        public TreasureChest Chest { get; private set; }
        public WardWatch Ward { get; private set; }
        public BossArea ExtraBoss { get; private set; }
        public string SelectedSlot { get; private set; } = "";

        GameObject _explore;
        GameObject _treasure;
        GameObject _ward;
        GameObject _boss;
        GameObject _housing;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() => Instance = null;

        public static void Install(WorldNavigator nav, Transform root, EnemySpawner spawner)
        {
            if (nav == null || root == null || Instance != null) return;
            var host = new GameObject("WorldSites");
            host.transform.SetParent(nav.transform, false);
            var sites = host.AddComponent<WorldSites>();
            sites._explore = sites.Make(root, WorldZone.Explore, new Color(0.2f, 0.32f, 0.22f, 1f));
            sites._treasure = sites.Make(root, WorldZone.Treasure, new Color(0.32f, 0.28f, 0.16f, 1f));
            sites._ward = sites.Make(root, WorldZone.Ward, new Color(0.28f, 0.2f, 0.2f, 1f));
            sites._boss = sites.Make(root, WorldZone.BossExtra, new Color(0.32f, 0.16f, 0.16f, 1f));
            sites._housing = sites.Make(root, WorldZone.Housing, new Color(0.24f, 0.26f, 0.22f, 1f));
            sites.Chest = sites.ChestOn(sites._treasure.transform);
            sites.Ward = sites.WardOn(sites._ward.transform, spawner);
            sites.ExtraBoss = sites.BossOn(sites._boss.transform, spawner);
            sites.SlotsOn(sites._housing.transform);
            HomeDistrict.Apply(sites._housing.transform);
            sites.HazardsOn(sites._explore.transform);
            Activate(WorldZone.None);
        }

        public static void Activate(WorldZone zone)
        {
            if (Instance == null) return;
            Instance.SetOn(Instance._explore, zone == WorldZone.Explore);
            Instance.SetOn(Instance._treasure, zone == WorldZone.Treasure);
            Instance.SetOn(Instance._ward, zone == WorldZone.Ward);
            Instance.SetOn(Instance._boss, zone == WorldZone.BossExtra);
            Instance.SetOn(Instance._housing, zone == WorldZone.Housing);
            var record = WorldFoundation.FindZone(zone);
            if (!string.IsNullOrEmpty(record.id) && string.IsNullOrEmpty(record.layoutSource))
                WorldFlags.Set("seen." + record.id, 1);
        }

        public static string Hint(WorldZone zone)
        {
            var record = WorldFoundation.FindZone(zone);
            if (string.IsNullOrEmpty(record.id) || !string.IsNullOrEmpty(record.layoutSource)) return "";
            if (zone == WorldZone.Treasure && Instance != null && Instance.Chest != null)
                return record.name + "  " + Instance.Chest.Status;
            if (zone == WorldZone.Ward && Instance != null && Instance.Ward != null)
                return record.name + "  " + Instance.Ward.Status;
            if (zone == WorldZone.Housing && Instance != null)
                return record.name + "  " + Instance.SelectedSlot;
            return record.name;
        }

        void SetOn(GameObject go, bool on)
        {
            if (go != null) go.SetActive(on);
        }

        GameObject Make(Transform root, WorldZone zone, Color ground)
        {
            var record = WorldFoundation.FindZone(zone);
            var go = new GameObject(record.id);
            go.transform.SetParent(root, false);
            go.transform.position = record.origin;
            var floor = new GameObject("Ground");
            floor.transform.SetParent(go.transform, false);
            floor.AddComponent<WorldVisual>().Build("", ground, new Vector2(14f, 16f));
            var sprite = floor.GetComponent<SpriteRenderer>();
            if (sprite != null) sprite.sortingOrder = -8;
            ReturnGate(go.transform, record.origin + new Vector2(0f, -5.8f));
            return go;
        }

        void ReturnGate(Transform parent, Vector2 world)
        {
            var go = new GameObject("Back");
            go.transform.SetParent(parent, false);
            go.transform.position = world;
            go.AddComponent<WorldVisual>().Build("Village", WorldGraybox.Gate, new Vector2(1.2f, 0.7f));
            var talk = go.AddComponent<WorldInteractable>();
            talk.DisplayName = "Village";
            talk.Radius = 0.8f;
            talk.Line = "Return to the village.";
            talk.OnInteract = () => WorldNavigator.Instance.Show(WorldZone.Hub);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Vector2.one;
            var gate = go.AddComponent<WorldGate>();
            gate.OnEnter = () => WorldNavigator.Instance.Show(WorldZone.Hub);
        }

        TreasureChest ChestOn(Transform parent)
        {
            var poi = WorldFoundation.Points[0];
            var go = new GameObject(poi.id);
            go.transform.SetParent(parent, false);
            go.transform.position = (Vector2)parent.position + poi.localOffset;
            go.AddComponent<WorldVisual>().Build("Chest", new Color(0.72f, 0.55f, 0.2f, 1f), new Vector2(0.9f, 0.7f));
            var chest = go.AddComponent<TreasureChest>();
            var talk = go.AddComponent<WorldInteractable>();
            talk.DisplayName = "Chest";
            talk.Line = "A closed chest.";
            talk.OnInteract = () =>
            {
                chest.Open();
                talk.Line = chest.Status == "opened" ? "The chest opens." : "The chest is empty.";
            };
            return chest;
        }

        WardWatch WardOn(Transform parent, EnemySpawner spawner)
        {
            var go = new GameObject("poi_ward");
            go.transform.SetParent(parent, false);
            go.transform.position = (Vector2)parent.position + new Vector2(0f, 1.4f);
            go.AddComponent<WorldVisual>().Build("Ward", new Color(0.55f, 0.28f, 0.28f, 1f), new Vector2(1.2f, 1.2f));
            var ward = go.AddComponent<WardWatch>();
            ward.Bind(spawner);
            var talk = go.AddComponent<WorldInteractable>();
            talk.DisplayName = "Ward";
            talk.Line = "Hold this ground.";
            talk.OnInteract = () =>
            {
                ward.StartWatch();
                talk.Line = ward.Status;
            };
            return ward;
        }

        BossArea BossOn(Transform parent, EnemySpawner spawner)
        {
            var area = parent.gameObject.AddComponent<BossArea>();
            parent.gameObject.SetActive(false);
            area.Bind(spawner);
            return area;
        }

        void SlotsOn(Transform parent)
        {
            var made = new WorldInteractable[WorldFoundation.VillageSlots.Length];
            for (int i = 0; i < WorldFoundation.VillageSlots.Length; i++)
            {
                var slot = WorldFoundation.VillageSlots[i];
                var go = new GameObject(slot.id);
                go.transform.SetParent(parent, false);
                go.transform.position = (Vector2)parent.position + slot.localOffset;
                go.AddComponent<WorldVisual>().Build(slot.kind, SlotColor(slot.kind), new Vector2(0.9f, 0.9f));
                var talk = go.AddComponent<WorldInteractable>();
                bool foundation = slot.id == "slot_house";
                talk.Seekable = foundation;
                talk.DisplayName = foundation ? "Foundation" : slot.kind;
                talk.Line = slot.id;
                string id = slot.id;
                Housing.Register(id, talk);
                if (foundation)
                    talk.OnInteract = () => { BuildMode.Open(); BuildMode.SetTab(0); };
                else
                    talk.OnInteract = () =>
                    {
                        Housing.Use(id);
                        SelectedSlot = Housing.Selected;
                    };
                made[i] = talk;
            }
            WorldInteractable face = null;
            for (int i = 0; i < made.Length; i++)
                if (made[i] != null && made[i].Seekable) face = made[i];
            if (face == null) return;
            float reach = face.Radius;
            var origin = (Vector2)face.transform.position;
            for (int i = 0; i < made.Length; i++)
            {
                float d = Vector2.Distance(origin, made[i].transform.position) + 1.15f;
                if (d > reach) reach = d;
            }
            face.Radius = reach;
        }

        void HazardsOn(Transform parent)
        {
            for (int i = 0; i < WorldFoundation.Hazards.Length; i++)
            {
                var hazard = WorldFoundation.Hazards[i];
                var go = new GameObject(hazard.id);
                go.transform.SetParent(parent, false);
                go.transform.localPosition = hazard.center;
                var color = hazard.biomeId == "biome_lava"
                    ? new Color(0.7f, 0.28f, 0.12f, 1f)
                    : new Color(0.2f, 0.4f, 0.7f, 1f);
                go.AddComponent<WorldVisual>().Build(hazard.id, color, hazard.halfSize * 2f);
                var volume = go.AddComponent<HazardVolume>();
                volume.lava = hazard.biomeId == "biome_lava";
                volume.halfSize = hazard.halfSize;
            }
        }

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        static Color SlotColor(string kind)
        {
            if (kind == "HousingSlot") return new Color(0.45f, 0.38f, 0.28f, 1f);
            if (kind == "DecorationSlot") return new Color(0.35f, 0.45f, 0.35f, 1f);
            if (kind == "NPCServiceSlot") return new Color(0.4f, 0.4f, 0.55f, 1f);
            return new Color(0.4f, 0.36f, 0.3f, 1f);
        }
    }
}
