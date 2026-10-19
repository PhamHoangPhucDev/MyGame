using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Idle/walk bob on a Visual child + sword rest/swing synced to facing.
    /// </summary>
    [DefaultExecutionOrder(60)]
    public class PlayerVisual : MonoBehaviour
    {
        [SerializeField] Transform visual;
        [SerializeField] SpriteRenderer visualRenderer;
        [SerializeField] Transform sword;
        [SerializeField] SpriteRenderer swordRenderer;
        [SerializeField] PlayerCombat combat;
        [SerializeField] PlayerMovement movement;
        [SerializeField] Sprite idleSprite;
        [SerializeField] Sprite walkASprite;
        [SerializeField] Sprite walkBSprite;
        [SerializeField] Sprite strikeSprite;
        [SerializeField] RuntimeAnimatorController bodyController;
        [Tooltip("Hurt clip length. Health flash and i-frames still run on top.")]
        [SerializeField] float hurtDuration = 0.18f;
        [Tooltip("Off: a hit during a swing only flashes, so crowds cannot cancel the combo.")]
        [SerializeField] bool hurtInterruptsAttack = false;
        Animator _bodyAnim;
        string _bodyState;
        float _bodyAttackTimer;
        float _attackStartNorm;
        float _hurtTimer;
        string _hurtDir = "Down";
        bool _hurtFaceRight;
        string _attackDir = "Down";
        bool _attackFaceRight;

        const float Pixel = 1f / 16f;
        float _bobTimer;
        bool _bobUp;
        bool _swinging;
        float _swingElapsed;
        const float SwingDuration = 0.16f;
        // Hero_Attack_* clips: windup 0.08 s, strike on 0.12 s, end at 0.26 s (HeroExtraAnimBuilder).
        const float AttackLock = 0.26f;
        const float WindupLength = 0.08f;
        Vector2 _swingFacing;
        int _restSort = 19;
        int _heroSort = 20;
        Health _health;
        bool _dead;

        public bool IsSwinging => _swinging;

        void Awake()
        {
            combat = GetComponent<PlayerCombat>();
            movement = GetComponent<PlayerMovement>();
            EnsureHierarchy();
        }

        public void EnsureHierarchy()
        {
            if (visual == null)
            {
                var existing = transform.Find("Visual");
                if (existing != null) visual = existing;
            }

            if (visual == null)
            {
                // Move existing SpriteRenderer onto Visual child.
                var rootSr = GetComponent<SpriteRenderer>();
                var visualGo = new GameObject("Visual");
                visual = visualGo.transform;
                visual.SetParent(transform, false);
                visual.localPosition = Vector3.zero;
                visualRenderer = visualGo.AddComponent<SpriteRenderer>();
                if (rootSr != null)
                {
                    visualRenderer.sprite = rootSr.sprite;
                    visualRenderer.color = rootSr.color;
                    visualRenderer.sortingOrder = rootSr.sortingOrder;
                    _heroSort = rootSr.sortingOrder;
                    Destroy(rootSr);
                }
                var sorter = GetComponent<SpriteSorter>();
                if (sorter != null)
                {
                    Destroy(sorter);
                    var vs = visualGo.AddComponent<SpriteSorter>();
                    vs.baseOrder = _heroSort;
                }
            }
            else if (visualRenderer == null)
                visualRenderer = visual.GetComponent<SpriteRenderer>();

            if (sword == null)
            {
                var existingSword = transform.Find("Sword");
                if (existingSword != null) sword = existingSword;
            }

            if (sword == null)
            {
                var swordGo = new GameObject("Sword");
                sword = swordGo.transform;
                sword.SetParent(transform, false);
                swordRenderer = swordGo.AddComponent<SpriteRenderer>();
                swordRenderer.sortingOrder = _restSort;
            }
            else if (swordRenderer == null)
                swordRenderer = sword.GetComponent<SpriteRenderer>();

            if (swordRenderer != null && swordRenderer.sprite == null)
                swordRenderer.sprite = LoadOrCreateSwordSprite();

            if (bodyController != null && visualRenderer != null)
            {
                _bodyAnim = visual.GetComponent<Animator>();
                if (_bodyAnim == null)
                    _bodyAnim = visual.gameObject.AddComponent<Animator>();
                _bodyAnim.runtimeAnimatorController = bodyController;
                _bodyAnim.updateMode = AnimatorUpdateMode.Normal;
                _bodyAnim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                visual.localPosition = Vector3.zero;
            }

            var health = GetComponent<Health>();
            health?.CacheRenderers();
            if (_health != null)
            {
                _health.Died -= OnDied;
                _health.Damaged -= OnDamaged;
            }
            _health = health;
            if (_health != null)
            {
                _health.Died += OnDied;
                _health.Damaged += OnDamaged;
            }
            RestSword();
        }

        void OnDestroy()
        {
            if (_health != null)
            {
                _health.Died -= OnDied;
                _health.Damaged -= OnDamaged;
            }
        }

        void OnDamaged(Health _, int amount, Vector2 hitPoint)
        {
            if (_dead || _bodyAnim == null || !HasBodyState("Hero_Hurt_Down")) return;
            if (_bodyAttackTimer > 0f && !hurtInterruptsAttack) return;

            // Face the attacker so the knockback lean reads correctly. Enemy hits pass the
            // hero's own position as hitPoint, so fall back to the nearest enemy.
            Vector2 toHit = hitPoint - (Vector2)transform.position;
            if (toHit.sqrMagnitude < 0.0001f)
            {
                var attacker = CombatTargeting.FindNearestEnemy(transform.position, 2.5f, ~0);
                toHit = attacker != null
                    ? (Vector2)(attacker.transform.position - transform.position)
                    : (combat != null ? combat.Facing : Vector2.down);
            }
            bool side = Mathf.Abs(toHit.x) > Mathf.Abs(toHit.y) && Mathf.Abs(toHit.x) > 0.05f;
            _hurtDir = side ? "Side" : (toHit.y > 0.2f ? "Up" : "Down");
            _hurtFaceRight = side && toHit.x > 0f;
            _bodyAttackTimer = 0f;
            _hurtTimer = hurtDuration;
            _bodyState = null;
        }

        void OnDied(Health _)
        {
            _dead = true;
            _swinging = false;
            _bodyAttackTimer = 0f;
            _hurtTimer = 0f;
            if (_bodyAnim != null)
            {
                if (HasBodyState("Hero_Death"))
                {
                    _bodyAnim.speed = 1f;
                    _bodyAnim.Play("Hero_Death", 0, 0f);
                    _bodyState = "Hero_Death";
                }
                else
                    _bodyAnim.speed = 0f;
            }
            if (swordRenderer != null)
                swordRenderer.enabled = false;
        }

        bool HasBodyState(string state) =>
            _bodyAnim != null && _bodyAnim.runtimeAnimatorController != null &&
            _bodyAnim.HasState(0, Animator.StringToHash(state));

        void Update()
        {
            if (visual == null || _dead) return;
            UpdateBob();
            UpdateFlip();
            UpdateSword();
        }

        void LateUpdate()
        {
            if (visualRenderer == null) return;
            int bodyOrder = visualRenderer.sortingOrder;
            if (swordRenderer != null && swordRenderer.enabled)
            {
                bool swingUp = _swinging && _swingFacing.y > 0.35f;
                swordRenderer.sortingOrder = swingUp ? bodyOrder - 1 : (_swinging ? bodyOrder + 2 : bodyOrder - 1);
            }
            var shadow = transform.Find("ContactShadow");
            if (shadow != null)
            {
                var shadowRenderer = shadow.GetComponent<SpriteRenderer>();
                if (shadowRenderer != null)
                    shadowRenderer.sortingOrder = bodyOrder - 4;
            }
        }

        void UpdateBob()
        {
            if (_bodyAnim != null)
            {
                visual.localPosition = Vector3.zero;
                PlayBodyState();
                return;
            }

            bool walking = movement != null && movement.IsMoving;
            float period = walking ? 0.16f : 0.5f;
            float amp = walking ? Pixel : Pixel; // idle 1px, walk same 1px amplitude but faster
            // Walk: toggle every half period for 2-frame feel
            _bobTimer += Time.deltaTime;
            if (_bobTimer >= period)
            {
                _bobTimer -= period;
                _bobUp = !_bobUp;
            }
            float y = _bobUp ? amp : 0f;
            if (walking && !_bobUp)
                y = 0f;
            else if (walking && _bobUp)
                y = Pixel;
            visual.localPosition = new Vector3(0f, y, 0f);
            if (visualRenderer == null) return;
            if (_swinging && strikeSprite != null)
                visualRenderer.sprite = strikeSprite;
            else if (walking && walkASprite != null && walkBSprite != null)
                visualRenderer.sprite = _bobUp ? walkASprite : walkBSprite;
            else if (idleSprite != null)
                visualRenderer.sprite = idleSprite;
        }

        void PlayBodyState()
        {
            if (_dead) return;
            bool frozen = HitStop.Instance != null && HitStop.Instance.IsActive;
            if (_hurtTimer > 0f)
            {
                string hurt = "Hero_Hurt_" + _hurtDir;
                _bodyAnim.speed = frozen ? 0f : 1f;
                if (!frozen)
                    _hurtTimer -= Time.deltaTime;
                if (_bodyState != hurt)
                {
                    _bodyState = hurt;
                    _bodyAnim.Play(hurt, 0, 0f);
                }
                return;
            }

            Vector2 facing = combat != null ? combat.Facing : Vector2.down;
            bool side = Mathf.Abs(facing.x) > Mathf.Abs(facing.y) && Mathf.Abs(facing.x) > 0.05f;
            bool attacking = _bodyAttackTimer > 0f;
            if (!attacking && _bodyState != null && _bodyState.StartsWith("Hero_Attack_"))
            {
                var held = _bodyAnim.GetCurrentAnimatorStateInfo(0);
                if (held.IsName(_bodyState) && held.normalizedTime < 0.98f)
                    attacking = true;
            }

            string dir = attacking ? _attackDir : (side ? "Side" : (facing.y > 0.2f ? "Up" : "Down"));
            bool walking = movement != null && movement.IsMoving && !attacking;
            string state = (attacking ? "Hero_Attack_" : (walking ? "Hero_Walk_" : "Hero_Idle_")) + dir;
            float speed = frozen ? 0f : (walking ? 1.15f : 1f);
            _bodyAnim.speed = speed;

            if (attacking)
            {
                if (!frozen)
                    _bodyAttackTimer -= Time.deltaTime;
                var info = _bodyAnim.GetCurrentAnimatorStateInfo(0);
                bool playing = info.IsName(state) && _bodyState == state;
                if (!playing)
                    _bodyAnim.Play(state, 0, _attackStartNorm);
                else if (!frozen && info.normalizedTime >= 1f)
                    _bodyAnim.Play(state, 0, 0.99f);
                _bodyState = state;
                return;
            }

            if (_bodyState == state) return;
            _bodyState = state;
            _bodyAnim.Play(state, 0, 0f);
        }

        void UpdateFlip()
        {
            if (combat == null || visualRenderer == null) return;
            Vector2 facing = combat.Facing;
            if (_bodyAnim != null)
            {
                if (_hurtTimer > 0f)
                {
                    visualRenderer.flipX = _hurtFaceRight;
                    return;
                }
                if (_bodyAttackTimer > 0f)
                {
                    visualRenderer.flipX = _attackFaceRight;
                    return;
                }
                bool side = Mathf.Abs(facing.x) > Mathf.Abs(facing.y) && Mathf.Abs(facing.x) > 0.05f;
                // Side sheets face left. Flip only when showing the right side.
                visualRenderer.flipX = side && facing.x > 0f;
                return;
            }
            if (Mathf.Abs(facing.x) > 0.05f)
                visualRenderer.flipX = facing.x < 0f;
        }

        void UpdateSword()
        {
            if (sword == null || swordRenderer == null) return;
            if (_bodyAnim != null && _bodyAttackTimer > 0f)
            {
                swordRenderer.enabled = false;
                if (_swinging)
                {
                    _swingElapsed += Time.deltaTime;
                    if (_swingElapsed >= SwingDuration)
                        _swinging = false;
                }
                return;
            }
            if (!_swinging)
            {
                if (_bodyAnim != null && _bodyAttackTimer > 0f)
                {
                    swordRenderer.enabled = false;
                    return;
                }
                swordRenderer.enabled = true;
                RestSword();
                return;
            }

            _swingElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(_swingElapsed / SwingDuration);
            // Ease-out
            float eased = 1f - (1f - t) * (1f - t);
            float baseAngle = Mathf.Atan2(_swingFacing.y, _swingFacing.x) * Mathf.Rad2Deg;
            float angle = Mathf.LerpAngle(baseAngle - 70f, baseAngle + 70f, eased);
            sword.localRotation = Quaternion.Euler(0f, 0f, angle);
            sword.localPosition = (Vector3)(_swingFacing.normalized * 0.2f);

            // Sorting: above hero when swinging down/side, below when up
            bool swingUp = _swingFacing.y > 0.35f;
            int bodyOrder = visualRenderer != null ? visualRenderer.sortingOrder : _heroSort;
            swordRenderer.sortingOrder = swingUp ? bodyOrder - 1 : bodyOrder + 2;

            if (t >= 1f)
            {
                _swinging = false;
                RestSword();
            }
        }

        /// <param name="skipWindup">True for instant skills (whirlwind waves) that strike on the same frame.</param>
        public void PlaySlashSwing(Vector2 facing, bool skipWindup = false)
        {
            _swingFacing = facing.sqrMagnitude > 0.01f ? facing.normalized : Vector2.right;
            _swinging = true;
            _swingElapsed = 0f;
            _bodyState = null;
            _hurtTimer = 0f;
            _attackStartNorm = skipWindup ? WindupLength / AttackLock : 0f;
            _bodyAttackTimer = skipWindup ? AttackLock - WindupLength : AttackLock;
            Vector2 aim = _swingFacing;
            bool side = Mathf.Abs(aim.x) > Mathf.Abs(aim.y) && Mathf.Abs(aim.x) > 0.05f;
            _attackDir = side ? "Side" : (aim.y > 0.2f ? "Up" : "Down");
            _attackFaceRight = side && aim.x > 0f;
            if (swordRenderer != null)
                swordRenderer.enabled = true;
        }

        void RestSword()
        {
            if (sword == null || swordRenderer == null) return;
            // Sheathed behind back ~30 degrees
            bool faceLeft = combat != null && combat.Facing.x < -0.05f;
            float side = faceLeft ? -1f : 1f;
            sword.localPosition = new Vector3(-0.15f * side, 0.1f, 0f);
            sword.localRotation = Quaternion.Euler(0f, 0f, 30f * side);
            int bodyOrder = visualRenderer != null ? visualRenderer.sortingOrder : _heroSort;
            swordRenderer.sortingOrder = bodyOrder - 1;
            swordRenderer.flipX = faceLeft;
        }

        static Sprite LoadOrCreateSwordSprite()
        {
            var sprite = Resources.Load<Sprite>("Placeholders/Sword");
            if (sprite != null) return sprite;

            // Runtime 6x16 placeholder if asset missing
            const int w = 6, h = 16;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            var px = new Color[w * h];
            var blade = new Color(0.75f, 0.88f, 1f, 1f);
            var edge = new Color(1f, 1f, 1f, 1f);
            var hilt = new Color(0.55f, 0.35f, 0.15f, 1f);
            for (int y = 4; y < h; y++)
            for (int x = 2; x <= 3; x++)
                px[y * w + x] = y > h - 3 ? edge : blade;
            for (int y = 0; y < 4; y++)
            for (int x = 1; x <= 4; x++)
                px[y * w + x] = hilt;
            tex.SetPixels(px);
            tex.Apply(false, true);
            // Pivot at hilt (bottom center)
            return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.12f), 16f);
        }
    }
}
