using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Idle/walk bob on a Visual child + sword rest/swing synced to facing.
    /// </summary>
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
        Animator _bodyAnim;
        string _bodyState;
        float _bodyAttackTimer;
        string _attackDir = "Down";
        bool _attackFaceRight;

        const float Pixel = 1f / 16f;
        float _bobTimer;
        bool _bobUp;
        bool _swinging;
        float _swingElapsed;
        const float SwingDuration = 0.12f;
        Vector2 _swingFacing;
        int _restSort = 19;
        int _heroSort = 20;

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
                _bodyAnim.updateMode = AnimatorUpdateMode.UnscaledTime;
                _bodyAnim.cullingMode = AnimatorCullingMode.AlwaysAnimate;
                visual.localPosition = Vector3.zero;
            }

            var health = GetComponent<Health>();
            health?.CacheRenderers();
            RestSword();
        }

        void Update()
        {
            if (visual == null) return;
            UpdateBob();
            UpdateFlip();
            UpdateSword();
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
            Vector2 facing = combat != null ? combat.Facing : Vector2.down;
            bool side = Mathf.Abs(facing.x) > Mathf.Abs(facing.y) && Mathf.Abs(facing.x) > 0.05f;
            string dir = _bodyAttackTimer > 0f ? _attackDir : (side ? "Side" : (facing.y > 0.2f ? "Up" : "Down"));
            bool walking = movement != null && movement.IsMoving && _bodyAttackTimer <= 0f;
            string state = (_bodyAttackTimer > 0f ? "Hero_Attack_" : (walking ? "Hero_Walk_" : "Hero_Idle_")) + dir;
            if (_bodyAttackTimer > 0f)
            {
                _bodyAttackTimer -= Time.unscaledDeltaTime;
                _bodyAnim.speed = 1f;
                var info = _bodyAnim.GetCurrentAnimatorStateInfo(0);
                bool playing = info.IsName(state);
                if (!playing)
                    _bodyAnim.Play(state, 0, 0f);
                else if (info.normalizedTime >= 1f)
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
            swordRenderer.sortingOrder = swingUp ? _heroSort - 1 : _heroSort + 2;

            if (t >= 1f)
            {
                _swinging = false;
                RestSword();
            }
        }

        public void PlaySlashSwing(Vector2 facing)
        {
            _swingFacing = facing.sqrMagnitude > 0.01f ? facing.normalized : Vector2.right;
            _swinging = true;
            _swingElapsed = 0f;
            _bodyState = null;
            _bodyAttackTimer = 0.32f;
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
            swordRenderer.sortingOrder = _restSort;
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
