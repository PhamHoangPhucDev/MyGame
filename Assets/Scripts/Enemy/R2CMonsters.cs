using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>Sandbox roster for the approved R2C monster sheets. Campaign spawners do not load this.</summary>
    [CreateAssetMenu(menuName = "Gameplay Prototype/R2C Monster Roster", fileName = "R2CMonsterRoster")]
    public class R2CMonsterRoster : ScriptableObject
    {
        public R2CMonsterDef[] monsters;
    }

    [System.Serializable]
    public class R2CMonsterDef
    {
        public string label;
        public RuntimeAnimatorController controller;
        public int hp = 60;
        public int damage = 4;
        public float speed = 2.05f;
        public float aggro = 5.2f;
        public float meleeRange = 0.75f;
        public float cooldown = 1.35f;
        public Vector2 colliderSize = new Vector2(0.625f, 0.375f);
        public bool rider;
        public bool bossBar;
        public int chargeDamage;
        public bool ranged;
        public bool aimLine;
        public float standOff = 2.2f;
        public float shotRange = 6.5f;
        public float shotSpeed = 8f;
        public Sprite[] bolt;
        public Vector2 spawnDown;
        public Vector2 spawnUp;
        public Vector2 spawnSide;
        public Sprite[] telegraph;
        public Sprite crack;
        public RuntimeAnimatorController enraged;
        public int slamDamage;
        public float slamRadius;
    }

    /// <summary>Marks a sandbox sub-boss so the HP bar stays visible. Not the Level 5 warlord machine.</summary>
    public class R2CBossMark : MonoBehaviour { }

    /// <summary>Ranged attack for Goblin Archer and the two Dryads. Melee monsters do not get this.</summary>
    public class R2CMonsterShot : MonoBehaviour
    {
        bool _ranged;
        bool _aimLine;
        float _standOff;
        float _range;
        float _speed;
        int _damage;
        Sprite[] _bolt;
        Vector2 _down, _up, _side;
        Transform _line;
        SpriteRenderer _lineSr;

        public bool Ranged => _ranged;
        public bool ShowsAim => _aimLine;
        public float StandOff => _standOff;
        public float Range => _range;

        public void Configure(R2CMonsterDef def, int damage)
        {
            _ranged = def.ranged;
            _aimLine = def.aimLine;
            _standOff = def.standOff;
            _range = def.shotRange;
            _speed = def.shotSpeed;
            _damage = damage;
            _bolt = def.bolt;
            _down = def.spawnDown;
            _up = def.spawnUp;
            _side = def.spawnSide;
        }

        public void ShowAim(Vector2 target)
        {
            if (!_aimLine) return;
            EnsureLine();
            Vector2 from = (Vector2)transform.position + SpawnLocal();
            Vector2 delta = target - from;
            float len = delta.magnitude;
            if (len < 0.05f) return;
            _line.gameObject.SetActive(true);
            _line.position = from + delta * 0.5f;
            _line.right = delta;
            _lineSr.size = new Vector2(len, 1f / 16f);
        }

        public void HideAim()
        {
            if (_line != null) _line.gameObject.SetActive(false);
        }

        public void Fire(Vector2 target)
        {
            HideAim();
            if (_bolt == null || _bolt.Length < 8) return;
            Vector2 from = (Vector2)transform.position + SpawnLocal();
            Vector2 dir = target - from;
            if (dir.sqrMagnitude < 0.0001f) dir = Vector2.down;
            R2CBolt.Fire(from, dir, _bolt, _speed, _range, _damage);
        }

        Vector2 SpawnLocal()
        {
            var visual = GetComponent<EnemyVisual>();
            Vector2 local = visual != null && visual.View == "Up" ? _up
                : visual != null && visual.View == "Side" ? _side
                : _down;
            if (visual != null && visual.FacingRight)
                local.x = -local.x;
            return local;
        }

        void EnsureLine()
        {
            if (_line != null) return;
            var go = new GameObject("AimLine");
            go.transform.SetParent(transform, false);
            _line = go.transform;
            _lineSr = go.AddComponent<SpriteRenderer>();
            _lineSr.sprite = LineSprite();
            _lineSr.color = new Color(0.847f, 0.227f, 0.227f, 1f);
            _lineSr.drawMode = SpriteDrawMode.Sliced;
            _lineSr.sortingOrder = 60;
            go.SetActive(false);
        }

        static Sprite _lineSprite;

        static Sprite LineSprite()
        {
            if (_lineSprite != null) return _lineSprite;
            var tex = new Texture2D(4, 4) { filterMode = FilterMode.Point, hideFlags = HideFlags.HideAndDontSave };
            var px = new Color32[16];
            for (int i = 0; i < px.Length; i++) px[i] = new Color32(255, 255, 255, 255);
            tex.SetPixels32(px);
            tex.Apply(false, false);
            _lineSprite = Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 16f, 0, SpriteMeshType.FullRect, new Vector4(1, 1, 1, 1));
            _lineSprite.hideFlags = HideFlags.HideAndDontSave;
            return _lineSprite;
        }
    }

    public class R2CBolt : MonoBehaviour
    {
        Vector2 _dir;
        float _speed;
        float _range;
        float _travel;
        int _damage;
        static readonly Collider2D[] Hits = new Collider2D[8];

        public static void Fire(Vector2 from, Vector2 dir, Sprite[] poses8, float speed, float range, int damage)
        {
            var go = new GameObject("R2CBolt");
            go.transform.position = from;
            var bolt = go.AddComponent<R2CBolt>();
            bolt._dir = dir.normalized;
            bolt._speed = speed;
            bolt._range = range;
            bolt._damage = damage;
            var sr = go.AddComponent<SpriteRenderer>();
            float a = Mathf.Atan2(bolt._dir.y, bolt._dir.x) * Mathf.Rad2Deg;
            int i = ((Mathf.RoundToInt(a / 45f) % 8) + 8) % 8;
            sr.sprite = poses8[i];
            sr.sortingOrder = 50;
        }

        void Update()
        {
            float step = _speed * Time.deltaTime;
            transform.position += (Vector3)(_dir * step);
            _travel += step;
            int n = Physics2D.OverlapCircle(transform.position, 0.2f, ContactFilter2D.noFilter, Hits);
            for (int i = 0; i < n; i++)
            {
                var health = Hits[i].GetComponentInParent<Health>();
                if (health == null || health.IsDead) continue;
                if (health.GetComponent<EnemyController>() != null) continue;
                if (health.TakeDamage(_damage, health.transform.position, _dir * 1.2f))
                    DamagePopup.Spawn(health.transform.position + Vector3.up * 0.2f, _damage, new Color(1f, 0.45f, 0.45f));
                Destroy(gameObject);
                return;
            }
            if (_travel >= _range) Destroy(gameObject);
        }
    }
}
