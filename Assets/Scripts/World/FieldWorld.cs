using System.Collections.Generic;
using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Playable field maps. Travel only reads WorldFoundation rows, so another map is another row.
    /// Art is the existing graybox, roster, gather node, chest banner, and Cyclops placeholder.
    /// </summary>
    public static class FieldWorld
    {
        static readonly List<FieldMapBody> Maps = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Maps.Clear();

        public static void Install(Transform root, EnemySpawner spawner)
        {
            if (root == null || root.Find("FieldMaps") != null) return;
            Maps.Clear();
            var host = new GameObject("FieldMaps");
            host.transform.SetParent(root, false);
            for (int i = 0; i < WorldFoundation.FieldMaps.Length; i++)
                Maps.Add(Build(host.transform, WorldFoundation.FieldMaps[i], spawner));
            Activate("");
        }

        public static void Activate(string zoneId)
        {
            for (int i = 0; i < Maps.Count; i++)
            {
                var map = Maps[i];
                if (map == null) continue;
                bool on = !string.IsNullOrEmpty(zoneId) && map.ZoneId == zoneId;
                if (map.gameObject.activeSelf != on) map.gameObject.SetActive(on);
            }
        }

        public static FieldMapBody Find(string zoneId)
        {
            for (int i = 0; i < Maps.Count; i++)
                if (Maps[i] != null && Maps[i].ZoneId == zoneId) return Maps[i];
            return null;
        }

        static FieldMapBody Build(Transform parent, FieldMapRecord map, EnemySpawner spawner)
        {
            var go = new GameObject(map.zoneId);
            go.SetActive(false);
            go.transform.SetParent(parent, false);
            go.transform.position = map.origin;

            var ground = new GameObject("Ground");
            ground.transform.SetParent(go.transform, false);
            ground.AddComponent<WorldVisual>().Build("", GroundColor(map.biomeId), map.boundsHalf * 2f);
            var groundSprite = ground.GetComponent<SpriteRenderer>();
            if (groundSprite != null) groundSprite.sortingOrder = -8;

            if (map.biomeId == "biome_sky")
                FillSky(go.transform);
            else
            {
                Road(go.transform, new Vector2(0f, -1f), new Vector2(2.6f, 22f));
                Road(go.transform, new Vector2(0f, 0f), new Vector2(26f, 2.4f));
                Rock(go.transform, map.biomeId, new Vector2(-14f, -6f), new Vector2(3.2f, 2.6f));
                Rock(go.transform, map.biomeId, new Vector2(14f, 6f), new Vector2(3.2f, 2.6f));
                Rock(go.transform, map.biomeId, new Vector2(-5f, 6f), new Vector2(2.4f, 2.2f));
                Rock(go.transform, map.biomeId, new Vector2(5f, -6f), new Vector2(2.4f, 2.2f));
                Wall(go.transform, new Vector2(-17.4f, 0f), new Vector2(1.2f, 28f));
                Wall(go.transform, new Vector2(17.4f, 0f), new Vector2(1.2f, 28f));
                Wall(go.transform, new Vector2(0f, 13.4f), new Vector2(36f, 1.2f));
                Wall(go.transform, new Vector2(0f, -13.4f), new Vector2(36f, 1.2f));
            }

            if (!string.IsNullOrEmpty(map.hazardBiome))
                Hazard(go.transform, map);

            for (int i = 0; i < WorldFoundation.FieldNodes.Length; i++)
                if (WorldFoundation.FieldNodes[i].zoneId == map.zoneId)
                    Node(go.transform, WorldFoundation.FieldNodes[i]);
            for (int i = 0; i < WorldFoundation.FieldChests.Length; i++)
                if (WorldFoundation.FieldChests[i].zoneId == map.zoneId)
                    Chest(go.transform, WorldFoundation.FieldChests[i]);

            WardWatch ward = null;
            if (map.ward) ward = Ward(go.transform, map.wardLocal, spawner);
            VillageGate(go.transform, new Vector2(0f, -12f));

            var body = go.AddComponent<FieldMapBody>();
            body.Init(map.zoneId, spawner, ward);
            return body;
        }

        static void Road(Transform parent, Vector2 local, Vector2 size)
        {
            var go = new GameObject("Road");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.AddComponent<WorldVisual>().Build("", WorldGraybox.Road, size);
            var sprite = go.GetComponent<SpriteRenderer>();
            if (sprite != null) sprite.sortingOrder = -7;
        }

        static void Rock(Transform parent, string biome, Vector2 local, Vector2 size)
        {
            var go = new GameObject("Rock");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.AddComponent<WorldVisual>().Build("", RockColor(biome), size);
            var sprite = go.GetComponent<SpriteRenderer>();
            if (sprite != null) sprite.sortingOrder = 2;
            go.AddComponent<BoxCollider2D>().size = Vector2.one;
        }

        static void Wall(Transform parent, Vector2 local, Vector2 size)
        {
            var go = new GameObject("Wall");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.AddComponent<WorldVisual>().Build("", new Color(0.1f, 0.09f, 0.08f, 1f), size);
            var sprite = go.GetComponent<SpriteRenderer>();
            if (sprite != null) sprite.sortingOrder = 2;
            go.AddComponent<BoxCollider2D>().size = Vector2.one;
        }

        static readonly Vector4[] SkyWalk =
        {
            new Vector4(-6f, -13f, 6f, -7f),
            new Vector4(-1.2f, -7f, 1.2f, -1.5f),
            new Vector4(-16f, -4f, -6f, 3f),
            new Vector4(6f, -4f, 16f, 3f),
            new Vector4(-6f, -1.5f, 6f, 1.5f),
            new Vector4(-1.2f, 1.5f, 1.2f, 6f),
            new Vector4(-7f, 6f, 7f, 13f)
        };

        static void FillSky(Transform parent)
        {
            const float step = 1f;
            const float minX = -18f, maxX = 18f, minY = -14f, maxY = 14f;
            for (float y = minY; y < maxY; y += step)
            {
                float run = float.NaN;
                for (float x = minX; x < maxX; x += step)
                {
                    bool blocked = !SkyOpen(new Vector2(x + step * 0.5f, y + step * 0.5f));
                    if (blocked && float.IsNaN(run)) run = x;
                    if (!blocked && !float.IsNaN(run))
                    {
                        Void(parent, new Vector2((run + x) * 0.5f, y + step * 0.5f), new Vector2(x - run, step));
                        run = float.NaN;
                    }
                }
                if (!float.IsNaN(run))
                    Void(parent, new Vector2((run + maxX) * 0.5f, y + step * 0.5f), new Vector2(maxX - run, step));
            }
        }

        static bool SkyOpen(Vector2 local)
        {
            for (int i = 0; i < SkyWalk.Length; i++)
            {
                var r = SkyWalk[i];
                if (local.x >= r.x && local.x <= r.z && local.y >= r.y && local.y <= r.w) return true;
            }
            return false;
        }

        static void Void(Transform parent, Vector2 local, Vector2 size)
        {
            var go = new GameObject("Void");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.AddComponent<WorldVisual>().Build("", new Color(0.04f, 0.06f, 0.1f, 1f), size);
            var sprite = go.GetComponent<SpriteRenderer>();
            if (sprite != null) sprite.sortingOrder = -5;
            go.AddComponent<BoxCollider2D>().size = Vector2.one;
        }

        static void Hazard(Transform parent, FieldMapRecord map)
        {
            bool lava = map.hazardBiome == "biome_lava";
            var go = new GameObject(lava ? "Lava" : "Water");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = map.hazardCenter;
            var color = lava ? new Color(0.7f, 0.28f, 0.12f, 1f) : new Color(0.2f, 0.45f, 0.75f, 1f);
            go.AddComponent<WorldVisual>().Build(lava ? "Lava" : "Water", color, map.hazardHalf * 2f);
            var sprite = go.GetComponent<SpriteRenderer>();
            if (sprite != null) sprite.sortingOrder = -6;
            var volume = go.AddComponent<HazardVolume>();
            volume.lava = lava;
            volume.halfSize = map.hazardHalf;
        }

        static void Node(Transform parent, FieldNodeRecord node)
        {
            var go = new GameObject(node.nodeId);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = node.local;
            go.AddComponent<GatherNode>().Init(new GatherConfig
            {
                nodeId = node.nodeId,
                itemId = node.itemId,
                displayName = node.displayName,
                amount = node.amount,
                uses = node.uses,
                respawnSeconds = node.respawnSeconds
            });
        }

        static void Chest(Transform parent, FieldChestRecord chest)
        {
            var go = new GameObject(chest.id);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = chest.local;
            go.AddComponent<WorldVisual>().Build("Chest", new Color(0.72f, 0.55f, 0.2f, 1f), new Vector2(0.9f, 0.7f));
            go.AddComponent<FieldChest>().Init(chest);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = new Vector2(1.1f, 0.9f);
        }

        static WardWatch Ward(Transform parent, Vector2 local, EnemySpawner spawner)
        {
            var go = new GameObject("poi_ward");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
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

        static void VillageGate(Transform parent, Vector2 local)
        {
            var go = new GameObject("Back");
            go.transform.SetParent(parent, false);
            go.transform.localPosition = local;
            go.AddComponent<WorldVisual>().Build("Village", WorldGraybox.Gate, new Vector2(1.2f, 0.7f));
            var talk = go.AddComponent<WorldInteractable>();
            talk.DisplayName = "Village";
            talk.Radius = 0.8f;
            talk.Line = "Return to the village.";
            talk.OnInteract = () => WorldNavigator.Instance?.Show(WorldZone.Hub);
            var col = go.AddComponent<BoxCollider2D>();
            col.isTrigger = true;
            col.size = Vector2.one;
            var gate = go.AddComponent<WorldGate>();
            gate.OnEnter = () => WorldNavigator.Instance?.Show(WorldZone.Hub);
        }

        static Color GroundColor(string biome)
        {
            if (biome == "biome_lava") return new Color(0.36f, 0.16f, 0.1f, 1f);
            if (biome == "biome_frozen") return new Color(0.78f, 0.86f, 0.92f, 1f);
            if (biome == "biome_tropical") return new Color(0.14f, 0.36f, 0.16f, 1f);
            if (biome == "biome_water") return new Color(0.1f, 0.28f, 0.42f, 1f);
            if (biome == "biome_sky") return new Color(0.55f, 0.72f, 0.9f, 1f);
            return new Color(0.3f, 0.3f, 0.3f, 1f);
        }

        static Color RockColor(string biome)
        {
            if (biome == "biome_lava") return new Color(0.22f, 0.16f, 0.14f, 1f);
            if (biome == "biome_frozen") return new Color(0.9f, 0.95f, 0.98f, 1f);
            if (biome == "biome_tropical") return new Color(0.08f, 0.24f, 0.1f, 1f);
            if (biome == "biome_water") return new Color(0.12f, 0.32f, 0.38f, 1f);
            return new Color(0.25f, 0.25f, 0.25f, 1f);
        }

        public static R2CMonsterDef FindRoster(string label, bool prefix)
        {
            var roster = Resources.Load<R2CMonsterRoster>("R2C/R2CMonsterRoster");
            if (roster == null || roster.monsters == null || string.IsNullOrEmpty(label)) return null;
            for (int i = 0; i < roster.monsters.Length; i++)
            {
                var monster = roster.monsters[i];
                if (monster == null || string.IsNullOrEmpty(monster.label)) continue;
                if (prefix ? monster.label.StartsWith(label) : monster.label == label) return monster;
            }
            return null;
        }
    }

    public class FieldMapBody : MonoBehaviour
    {
        readonly List<GameObject> _spawned = new();
        EnemySpawner _spawner;
        WardWatch _ward;
        GameObject _boss;
        string _encounter = "";

        public string ZoneId { get; private set; }
        public string SpawnReport { get; private set; } = "";
        public int SpawnedEnemies => _spawned.Count;
        public bool BossSpawned => _boss != null;

        public void Init(string zoneId, EnemySpawner spawner, WardWatch ward)
        {
            ZoneId = zoneId;
            _spawner = spawner;
            _ward = ward;
        }

        void OnEnable()
        {
            SpawnPack();
            SpawnBoss();
        }

        void OnDisable()
        {
            if (_ward != null) _ward.Abandon();
            ClearPack();
            if (_boss != null)
            {
                Destroy(_boss);
                _boss = null;
            }
        }

        void SpawnPack()
        {
            ClearPack();
            SpawnReport = "";
            if (_spawner == null)
            {
                SpawnReport = "no spawner";
                return;
            }
            for (int i = 0; i < WorldFoundation.FieldSpawns.Length; i++)
            {
                var group = WorldFoundation.FieldSpawns[i];
                if (group.zoneId != ZoneId) continue;
                var def = FieldWorld.FindRoster(group.rosterLabel, false);
                if (def == null)
                {
                    SpawnReport = "missing " + group.rosterLabel;
                    continue;
                }
                int made = 0;
                for (int n = 0; n < group.count; n++)
                {
                    var pos = (Vector2)transform.position + group.local + new Vector2(n * 1.5f, 0f);
                    var go = _spawner.SpawnR2C(def, pos, false);
                    if (go == null) continue;
                    _spawned.Add(go);
                    made++;
                }
                SpawnReport = group.groupId + " " + made + "/" + group.count + " " + group.rosterLabel;
            }
        }

        void SpawnBoss()
        {
            if (_boss != null || _spawner == null) return;
            FieldBossRecord boss = default;
            bool found = false;
            for (int i = 0; i < WorldFoundation.FieldBosses.Length; i++)
            {
                if (WorldFoundation.FieldBosses[i].zoneId != ZoneId) continue;
                boss = WorldFoundation.FieldBosses[i];
                found = true;
                break;
            }
            if (!found) return;
            _encounter = boss.id;
            if (WorldFlags.Get(WorldFoundation.FieldBossFlag(boss.id)) == 1) return;
            var def = FieldWorld.FindRoster(boss.rosterPrefix, true);
            if (def == null)
            {
                SpawnReport += " boss missing";
                return;
            }
            _boss = _spawner.SpawnR2C(def, (Vector2)transform.position + boss.local, false);
            if (_boss == null)
            {
                SpawnReport += " boss failed";
                return;
            }
            var health = _boss.GetComponent<Health>();
            if (health != null) health.Died += OnBossDied;
        }

        void OnBossDied(Health health)
        {
            if (health != null) health.Died -= OnBossDied;
            if (!string.IsNullOrEmpty(_encounter))
                WorldFlags.Set(WorldFoundation.FieldBossFlag(_encounter), 1);
            _boss = null;
        }

        void ClearPack()
        {
            for (int i = 0; i < _spawned.Count; i++)
                if (_spawned[i] != null) Destroy(_spawned[i]);
            _spawned.Clear();
        }
    }

    public class FieldChest : MonoBehaviour
    {
        FieldChestRecord _chest;
        WorldInteractable _talk;

        public string Status { get; private set; } = "closed";

        public void Init(FieldChestRecord chest)
        {
            _chest = chest;
            _talk = gameObject.AddComponent<WorldInteractable>();
            _talk.DisplayName = "Chest";
            _talk.Radius = 1.2f;
            _talk.OnInteract = Open;
            if (WorldFlags.Get(Flag) == 1)
            {
                Status = "EMPTY";
                _talk.Line = "EMPTY";
            }
            else _talk.Line = "A closed chest.";
        }

        string Flag => WorldFoundation.FieldChestFlag(_chest.id);

        public void Open()
        {
            if (WorldFlags.Get(Flag) == 1)
            {
                Status = "EMPTY";
                if (_talk != null) _talk.Line = "EMPTY";
                WorldNavigator.Instance?.Flash("EMPTY");
                return;
            }
            int cap = WorldContent.StackLimit(_chest.itemId);
            int have = WorldSave.GetQty(_chest.itemId);
            if (_chest.itemAmount > 0 && (cap <= 0 || have + _chest.itemAmount > cap))
            {
                Status = "INVENTORY FULL";
                WorldNavigator.Instance?.Flash("INVENTORY FULL");
                return;
            }
            int coinsBefore = WorldSave.Coins;
            if (_chest.coins > 0) WorldSave.AddCoins(_chest.coins);
            if (_chest.itemAmount > 0) WorldSave.AddItem(_chest.itemId, _chest.itemAmount);
            if (_chest.itemAmount > 0 && WorldSave.GetQty(_chest.itemId) < have + _chest.itemAmount)
            {
                int rolled = WorldSave.Coins - coinsBefore;
                if (rolled != 0) WorldSave.AddCoins(-rolled);
                Status = "INVENTORY FULL";
                WorldNavigator.Instance?.Flash("INVENTORY FULL");
                return;
            }
            WorldFlags.Set(Flag, 1);
            Status = "OPENED";
            if (_talk != null) _talk.Line = "EMPTY";
            if (_chest.itemAmount > 0) RewardBanner.Show(_chest.itemId, _chest.itemAmount);
            WorldNavigator.Instance?.Flash("REWARD  +" + _chest.coins + " COINS  +" + _chest.itemAmount + " " + _chest.itemId);
        }
    }
}
