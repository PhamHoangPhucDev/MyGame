using UnityEngine;

namespace GameplayPrototype
{
    public class TrainingGround : MonoBehaviour
    {
        EnemySpawner _spawner;
        int _alive;
        bool _running;

        public int Alive => _alive;
        public int ExpGained { get; private set; }

        public void Bind(EnemySpawner spawner) => _spawner = spawner;

        void OnEnable()
        {
            _running = true;
            CancelInvoke();
            TryFill();
        }

        void OnDisable()
        {
            _running = false;
            CancelInvoke();
            _alive = 0;
        }

        public void TryFill()
        {
            if (!_running || _spawner == null) return;
            var cfg = WorldCatalog.TrainingGround;
            while (_alive < cfg.maxAlive)
                SpawnOne();
        }

        public bool ArcherReady { get; private set; }

        void SpawnOne()
        {
            var cfg = WorldCatalog.TrainingGround;
            if (_alive >= cfg.maxAlive || _spawner == null) return;
            var groups = WorldContent.TrainingGroups;
            var group = groups[_alive % groups.Length];
            Vector2 camp = cfg.camps[_alive % cfg.camps.Length];
            Vector2 pos = (Vector2)transform.position + camp;
            GameObject go = null;
            if (group.archer)
            {
                var def = FindArcher();
                ArcherReady = def != null;
                if (def != null) go = _spawner.SpawnR2C(def, pos, false);
            }
            if (go == null) go = _spawner.SpawnGoblin(pos);
            if (go == null) return;
            _alive++;
            var health = go.GetComponent<Health>();
            if (health == null) return;
            string groupId = group.id;
            string loot = group.lootItemId;
            int exp = group.exp > 0 ? group.exp : cfg.expPerKill;
            float delay = group.respawnSeconds > 0f ? group.respawnSeconds : cfg.respawnSeconds;
            health.Died += h => OnDied(h, groupId, loot, exp, delay);
        }

        void OnDied(Health health, string groupId, string loot, int exp, float delay)
        {
            _alive = Mathf.Max(0, _alive - 1);
            WorldSave.AddExp(exp);
            Pets.OnEnemyKilled(exp);
            ExpGained += exp;
            WorldEvents.RaiseEnemyKilled(groupId);
            var drop = WorldContent.FindLoot(groupId);
            if (drop.amount > 0) WorldSave.AddItem(drop.itemId, drop.amount);
            else if (!string.IsNullOrEmpty(loot)) WorldSave.AddItem(loot, 1);
            if (_running)
                Invoke(nameof(TryFill), delay);
        }

        R2CMonsterDef FindArcher()
        {
            var roster = Resources.Load<R2CMonsterRoster>("R2C/R2CMonsterRoster");
            if (roster == null || roster.monsters == null) return null;
            for (int i = 0; i < roster.monsters.Length; i++)
            {
                var m = roster.monsters[i];
                if (m != null && m.label != null && m.label.StartsWith("Goblin Archer"))
                    return m;
            }
            return null;
        }
    }

    public class GatherNode : MonoBehaviour
    {
        GatherConfig _cfg;
        WorldInteractable _talk;
        WorldVisual _visual;

        public string ItemId => _cfg.itemId;
        public bool Depleted { get; private set; }

        public void Init(GatherConfig cfg)
        {
            _cfg = cfg;
            _visual = gameObject.AddComponent<WorldVisual>();
            _visual.Build(cfg.displayName, cfg.itemId == "ore"
                ? new Color(0.45f, 0.48f, 0.55f, 1f)
                : new Color(0.35f, 0.55f, 0.28f, 1f), new Vector2(1.1f, 1.1f));
            _talk = gameObject.AddComponent<WorldInteractable>();
            _talk.DisplayName = cfg.displayName;
            _talk.Radius = 1.3f;
            _talk.OnInteract = Gather;
            Refresh();
        }

        public void Refresh()
        {
            long now = WorldSave.UnixNow();
            long ready = WorldSave.NodeReadyUnix(_cfg.nodeId);
            int uses = WorldSave.NodeUses(_cfg.nodeId, _cfg.uses);
            if (uses <= 0 && ready > 0 && now >= ready)
            {
                uses = _cfg.uses;
                WorldSave.SetNode(_cfg.nodeId, uses, 0);
            }
            Depleted = uses <= 0;
            if (_talk != null)
                _talk.Line = Depleted ? "Empty. It will return." : "Take " + _cfg.itemId + ".";
        }

        public void Gather()
        {
            Refresh();
            if (!HeroClose() || Depleted) return;
            int uses = WorldSave.NodeUses(_cfg.nodeId, _cfg.uses) - 1;
            long ready = uses <= 0 ? WorldSave.UnixNow() + (long)_cfg.respawnSeconds : 0L;
            WorldSave.SetNode(_cfg.nodeId, uses, ready);
            WorldSave.AddItem(_cfg.itemId, _cfg.amount);
            Refresh();
        }

        bool HeroClose()
        {
            var hero = GameObject.FindGameObjectWithTag("Player");
            return hero != null && _talk != null && _talk.InRange(hero.transform.position);
        }
    }

    public class FarmPlot : MonoBehaviour
    {
        public int Index;
        WorldInteractable _talk;

        public bool Planted => Farming.State(Index) != PlotState.Empty;
        public bool Ready => Farming.State(Index) == PlotState.Ready;

        public void Init(int index)
        {
            Index = index;
            gameObject.AddComponent<WorldVisual>().Build("Plot " + (index + 1), new Color(0.42f, 0.32f, 0.2f, 1f), new Vector2(1.3f, 0.8f));
            _talk = gameObject.AddComponent<WorldInteractable>();
            _talk.DisplayName = "Plot " + (index + 1);
            _talk.Radius = 1.25f;
            _talk.OnInteract = Use;
            Refresh();
        }

        public void Refresh()
        {
            if (_talk == null) return;
            if (!Planted) _talk.Line = "Empty soil. Plant a seed.";
            else if (Ready) _talk.Line = "Ready to harvest.";
            else _talk.Line = "Growing.";
        }

        public void Use()
        {
            Refresh();
            var hero = GameObject.FindGameObjectWithTag("Player");
            if (hero == null || _talk == null || !_talk.InRange(hero.transform.position)) return;
            Farming.Use(Index);
            Refresh();
        }
    }

    public class BossArea : MonoBehaviour
    {
        EnemySpawner _spawner;
        public bool CombatConnected { get; private set; }
        public bool Defeated { get; private set; }
        public string Status { get; private set; } = "Not started";

        public void Bind(EnemySpawner spawner) => _spawner = spawner;

        void OnEnable()
        {
            Defeated = false;
            CombatConnected = false;
            Status = "Arena open";
            if (_spawner == null) return;
            var roster = Resources.Load<R2CMonsterRoster>("R2C/R2CMonsterRoster");
            R2CMonsterDef def = null;
            if (roster != null && roster.monsters != null)
            {
                for (int i = 0; i < roster.monsters.Length; i++)
                {
                    var m = roster.monsters[i];
                    if (m != null && m.label != null && m.label.StartsWith(WorldCatalog.BossRidge.rosterPrefix))
                    {
                        def = m;
                        break;
                    }
                }
            }
            if (def == null)
            {
                Status = "Cyclops roster missing";
                return;
            }
            var go = _spawner.SpawnR2C(def, (Vector2)transform.position + new Vector2(0f, 2.2f), false);
            CombatConnected = go != null;
            Status = CombatConnected ? "Cyclops is in the arena" : "Spawn failed";
            if (!CombatConnected) return;
            var health = go.GetComponent<Health>();
            if (health != null) health.Died += OnBossDied;
        }

        void OnBossDied(Health health)
        {
            if (health != null) health.Died -= OnBossDied;
            Defeated = true;
            Status = "Cyclops defeated";
            WorldSave.AddExp(WorldCatalog.BossRidge.exp);
            Pets.OnEnemyKilled(WorldCatalog.BossRidge.exp);
            WorldEvents.RaiseBossDefeated(WorldContent.BossId);
        }
    }
}
