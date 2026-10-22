using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// R2C equipment layers (Sandbox prototype). The body sheet never draws gear: this component reads the body
    /// sprite name every frame ("{Hero}_{Anim}_{View}_{frame}"), looks the frame up in an anchor table and places the
    /// weapon, gauntlet, off-hand (shield or second weapon) and slash sprites. Anchors are pixels in the 32x32 body cell
    /// (sheet orientation); body flipX mirrors every layer. Swapping gear / class = Configure(EquipmentSet).
    /// Per-combo-step rows ("Attack_Down_1@2") and per-step slash blocks are used when PlayerCombat runs a class kit.
    /// Opt-in per GameObject: PlayerVisual is untouched, its placeholder sword is only hidden while this is enabled.
    /// </summary>
    [DefaultExecutionOrder(70)]
    public class EquipmentVisual : MonoBehaviour
    {
        [Tooltip("Optional: when set, overrides the fields below at Awake (and on Configure).")]
        [SerializeField] EquipmentSet set;
        [SerializeField] TextAsset anchorTable;
        [Tooltip("8 poses: R, UR, U, UL, L, DL, D, DR. Pivot = hand.")]
        [SerializeField] Sprite[] weaponPoses = new Sprite[8];
        [Tooltip("Optional off-hand weapon, 8 poses (rows with off_dir >= 0).")]
        [SerializeField] Sprite[] offPoses = new Sprite[0];
        [Tooltip("Down, Up, Side (Side faces left like the body sheet). Pivot = handle.")]
        [SerializeField] Sprite[] shieldViews = new Sprite[3];
        [SerializeField] Sprite hand;
        [Tooltip("Blocks of 6 (Down0, Down1, Up0, Up1, Side0, Side1), one block per combo step. Pivot = body pivot.")]
        [SerializeField] Sprite[] slashFrames = new Sprite[6];
        [Tooltip("Selene gem glow, 2 columns x 2 rows. Shown on Attack frame 0 at the spec gem, not from the anchor.")]
        [SerializeField] Sprite[] chargeFrames = new Sprite[0];
        [SerializeField] bool showSlash = true;
        [Tooltip("This hero draws its own slash at the blade tip, so PlayerCombat skips the legacy R slash sprite. " +
                 "Only heroes carrying this component are affected (Sandbox); skills keep their own VFX.")]
        [SerializeField] bool replaceLegacyRSlash = true;
        [SerializeField] SpriteRenderer body;
        [SerializeField] float pixelsPerUnit = 16f;
        [SerializeField] Color hurtTint = new Color(1f, 0.62f, 0.62f, 1f);

        struct Anchor
        {
            public int MainX, MainY, MainDir, MainOrder, HandX, HandY, OffX, OffY, OffView, OffOrder, Slash, SlashOrder, OffDir;
        }

        readonly Dictionary<string, Anchor> _table = new Dictionary<string, Anchor>();
        SpriteRenderer _main, _hand, _off, _slash, _charge;
        SpriteRenderer _legacySword;
        PlayerCombat _combat;
        Sprite _lastSprite;
        string _lastKey;

        public string CurrentKey => _lastKey;
        public EquipmentSet Set => set;

        /// <summary>True when the hero on <paramref name="c"/> has an enabled EquipmentVisual that draws the R slash itself.</summary>
        public static bool ReplacesLegacySlash(Component c)
        {
            var e = c != null ? c.GetComponent<EquipmentVisual>() : null;
            return e != null && e.isActiveAndEnabled && e.replaceLegacyRSlash && e.showSlash && e.slashFrames != null && e.slashFrames.Length >= 6;
        }

        void Awake()
        {
            if (set != null) CopyFrom(set);
            Parse();
            _combat = GetComponent<PlayerCombat>();
        }

        /// <summary>Swap the whole gear set (class change, weapon skin). Takes effect on the next frame.</summary>
        public void Configure(EquipmentSet s)
        {
            if (s == null) return;
            set = s;
            CopyFrom(s);
            Parse();
            _lastSprite = null;
        }

        void CopyFrom(EquipmentSet s)
        {
            anchorTable = s.anchorTable;
            weaponPoses = s.weaponPoses ?? new Sprite[0];
            offPoses = s.offPoses ?? new Sprite[0];
            shieldViews = s.shieldViews ?? new Sprite[0];
            hand = s.hand;
            slashFrames = s.slashFrames ?? new Sprite[0];
            chargeFrames = s.chargeFrames ?? new Sprite[0];
            replaceLegacyRSlash = s.replaceLegacyRSlash;
        }

        void Start()
        {
            if (body == null) body = FindBodyRenderer();
            var parent = body != null ? body.transform : transform;
            _main = MakeLayer("Equip_Main", parent);
            _hand = MakeLayer("Equip_Hand", parent);
            _off = MakeLayer("Equip_Off", parent);
            _slash = MakeLayer("Equip_Slash", parent);
            _charge = MakeLayer("Equip_Charge", parent);
            var legacy = transform.Find("Sword");
            if (legacy != null) _legacySword = legacy.GetComponent<SpriteRenderer>();
        }

        SpriteRenderer FindBodyRenderer()
        {
            var pv = GetComponent<PlayerVisual>();
            if (pv != null)
            {
                var f = typeof(PlayerVisual).GetField("visualRenderer", BindingFlags.NonPublic | BindingFlags.Instance);
                if (f != null && f.GetValue(pv) is SpriteRenderer sr) return sr;
            }
            var anim = GetComponentInChildren<Animator>();
            return anim != null ? anim.GetComponent<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();
        }

        static SpriteRenderer MakeLayer(string name, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.enabled = false;
            return sr;
        }

        void Parse()
        {
            _table.Clear();
            if (anchorTable == null) return;
            var lines = anchorTable.text.Split('\n');
            for (int i = 1; i < lines.Length; i++)
            {
                var c = lines[i].Trim().Split(',');
                if (c.Length < 13) continue;
                int I(int k) => int.Parse(c[k], CultureInfo.InvariantCulture);
                _table[c[0]] = new Anchor
                {
                    MainX = I(1), MainY = I(2), MainDir = I(3), MainOrder = I(4), HandX = I(5), HandY = I(6),
                    OffX = I(7), OffY = I(8), OffView = c[9] == "Hand" ? 3 : c[9] == "Up" ? 1 : (c[9] == "Side" ? 2 : 0), OffOrder = I(10),
                    Slash = I(11), SlashOrder = I(12), OffDir = c.Length > 13 ? I(13) : -1,
                };
            }
        }

        // "Aren_Idle_Down_2" -> "Idle_Down_2"; anything else (another skin, legacy sheet) -> null.
        static string KeyOf(Sprite s)
        {
            if (s == null) return null;
            var n = s.name;
            int p = n.IndexOf('_');
            return p > 0 ? n.Substring(p + 1) : null;
        }

        void LateUpdate()
        {
            if (body == null) return;
            if (_legacySword != null) _legacySword.enabled = false;

            if (body.sprite != _lastSprite)
            {
                _lastSprite = body.sprite;
                _lastKey = KeyOf(_lastSprite);
            }
            if (_lastKey == null || !_table.TryGetValue(_lastKey, out var a) || !body.enabled)
            {
                _main.enabled = _hand.enabled = _off.enabled = _slash.enabled = false;
                if (_charge != null) _charge.enabled = false;
                return;
            }

            // Per-combo-step row ("Attack_Down_1@2") when authored; else R3B pose/VFX offsets below.
            int step = _combat != null && _lastKey.StartsWith("Attack_") ? _combat.CurrentComboStep : -1;
            if (step >= 0 && _table.TryGetValue(_lastKey + "@" + step, out var stepRow)) a = stepRow;

            bool flip = body.flipX;
            int order = body.sortingOrder;
            Color tint = body.color;
            if (_lastKey.StartsWith("Hurt_") && _lastKey.EndsWith("_1")) tint *= hurtTint;

            // R3C: one Attack body clip — differentiate A1/A2/A3 via 8-dir pose tilt + Z rotation + slash offset.
            int mainDir = a.MainDir;
            float weaponZ = 0f;
            bool authored = step >= 0 && _table.ContainsKey(_lastKey + "@" + step);
            bool wideSheet = weaponPoses != null && weaponPoses.Length > 8;
            if (!authored && !wideSheet)
            {
                if (step == 0) { mainDir = Wrap8(mainDir + 1); weaponZ = flip ? 18f : -18f; }
                else if (step == 1) { mainDir = Wrap8(mainDir - 1); weaponZ = flip ? -22f : 22f; }
                else if (step == 2) { mainDir = Wrap8(mainDir + 2); weaponZ = flip ? 28f : -28f; }
            }
            else if (wideSheet && flip && mainDir != 24)
            {
                int dir = mainDir % 8;
                int mirrored = new[] { 4, 3, 2, 1, 0, 7, 6, 5 }[dir];
                mainDir = (mainDir / 8) * 8 + mirrored;
            }

            Place(_main, a.MainX > -99 && a.MainOrder != 0 ? Pick(weaponPoses, mainDir) : null, a.MainX, a.MainY, a.MainOrder, flip, order, tint, weaponZ, !wideSheet);
            Place(_hand, a.HandX > -99 && a.MainOrder != 0 ? hand : null, a.HandX, a.HandY, a.MainOrder + 1, flip, order, tint, weaponZ * 0.35f);
            Sprite offSprite = null;
            if (a.OffX > -99 && a.OffOrder != 0)
                offSprite = a.OffView == 3 ? hand : (a.OffDir >= 0 ? Pick(offPoses, a.OffDir) : Pick(shieldViews, a.OffView));
            Place(_off, offSprite, a.OffX, a.OffY, a.OffOrder, flip, order, tint, 0f);

            int slashIndex = a.Slash;
            // Prefer dedicated per-step slash blocks (6 frames × step) when the set has them.
            if (slashIndex >= 0 && step > 0 && slashFrames != null && slashFrames.Length >= 6 * (step + 1))
                slashIndex += 6 * step;
            // R3D.1: finisher uses Aren_Thrust from combat — hide equip slash so arcs don't double.
            bool hideForThrust = ArenKitVfx.Active && step == 2;
            bool slashOn = showSlash && !hideForThrust && slashIndex >= 0 && slashFrames != null && slashIndex < slashFrames.Length && slashFrames[slashIndex] != null;
            _slash.enabled = slashOn;
            if (slashOn)
            {
                _slash.sprite = slashFrames[slashIndex];
                // Tip offset along swing so arc sits near the blade, not floating on body pivot.
                float tipX = 0f, tipY = 0.05f;
                bool slashFlip = flip;
                float slashScale = 1f;
                float slashZ = 0f;
                if (step == 0)
                {
                    tipX = flip ? -0.18f : 0.18f;
                    slashZ = flip ? 12f : -12f;
                }
                else if (step == 1)
                {
                    tipX = flip ? 0.18f : -0.18f;
                    slashFlip = !flip;
                    slashZ = flip ? -14f : 14f;
                }
                else if (step == 2)
                {
                    tipX = flip ? -0.08f : 0.08f;
                    tipY = 0.10f;
                    // Finisher arc larger (reference cyan crescents are wide) but capped so Aren stays readable.
                    slashScale = 1.55f;
                    slashZ = flip ? 10f : -10f;
                }
                if (wideSheet)
                {
                    tipX = 0f;
                    tipY = 0f;
                    slashZ = 0f;
                    slashScale = 1f;
                }
                _slash.transform.localPosition = new Vector3(tipX, tipY, 0f);
                _slash.transform.localRotation = Quaternion.Euler(0f, 0f, slashZ);
                _slash.flipX = slashFlip;
                _slash.transform.localScale = new Vector3(slashScale, slashScale, 1f);
                _slash.sortingLayerID = body.sortingLayerID;
                _slash.sortingOrder = order + a.SlashOrder;
                _slash.color = new Color(1f, 1f, 1f, tint.a);
            }
            else if (_slash != null)
            {
                _slash.transform.localScale = Vector3.one;
                _slash.transform.localRotation = Quaternion.identity;
                _slash.transform.localPosition = Vector3.zero;
            }

            if (step < 0 && _main != null)
                _main.transform.localRotation = Quaternion.identity;
            PlaceCharge(_lastKey, flip, order, tint, step);
        }

        /// <summary>
        /// Selene gem glow on Attack frame 0. Positions are the spec gem centers (sheet pixels, Side already flipped),
        /// not the anchor slash cell. Row 0 is a normal shot, row 1 is the finisher.
        /// </summary>
        void PlaceCharge(string key, bool flip, int order, Color tint, int step)
        {
            if (_charge == null) return;
            bool draw = key != null && key.StartsWith("Attack_") && key.EndsWith("_0")
                && chargeFrames != null && chargeFrames.Length >= 2;
            if (!draw)
            {
                _charge.enabled = false;
                return;
            }
            int row = step >= 2 ? 1 : 0;
            int frame = ((int)(Time.time / 0.08f) & 1);
            int index = row * 2 + frame;
            if (index >= chargeFrames.Length || chargeFrames[index] == null)
            {
                _charge.enabled = false;
                return;
            }
            bool up = key.Contains("_Up_");
            Vector2 local = up ? new Vector2(0.3125f, 1.25f) : new Vector2(-0.375f, 1.25f);
            if (flip) local.x = -local.x;
            _charge.enabled = true;
            _charge.sprite = chargeFrames[index];
            _charge.flipX = false;
            _charge.color = new Color(1f, 1f, 1f, tint.a);
            _charge.transform.localPosition = local;
            _charge.transform.localRotation = Quaternion.identity;
            _charge.transform.localScale = Vector3.one;
            _charge.sortingLayerID = body.sortingLayerID;
            _charge.sortingOrder = order + (up ? -3 : 3);
        }

        static int Wrap8(int d)
        {
            d %= 8;
            return d < 0 ? d + 8 : d;
        }

        static Sprite Pick(Sprite[] arr, int i) => arr != null && i >= 0 && i < arr.Length ? arr[i] : null;

        // Anchor pixel (x, y) from the top-left of the 32x32 cell; body pivot = (16, 30). The layer sprite's pivot
        // sits on the anchor pixel's top-left corner, so mirroring is just negating x and flipping the sprite.
        void Place(SpriteRenderer r, Sprite s, int x, int y, int relOrder, bool flip, int bodyOrder, Color tint, float localZ = 0f, bool flipSprite = true)
        {
            r.enabled = s != null;
            if (s == null) return;
            r.sprite = s;
            float lx = (x - 16) / pixelsPerUnit, ly = (30 - y) / pixelsPerUnit;
            r.transform.localPosition = new Vector3(flip ? -lx : lx, ly, 0f);
            r.transform.localRotation = Quaternion.Euler(0f, 0f, localZ);
            r.flipX = flip && flipSprite;
            r.sortingLayerID = body.sortingLayerID;
            r.sortingOrder = bodyOrder + relOrder;
            r.color = tint;
        }

        void OnDisable()
        {
            if (_main != null) _main.enabled = _hand.enabled = _off.enabled = _slash.enabled = false;
            if (_legacySword != null) _legacySword.enabled = true;
        }
    }
}
