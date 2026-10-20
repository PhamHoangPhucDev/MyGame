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
        SpriteRenderer _main, _hand, _off, _slash;
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
                    OffX = I(7), OffY = I(8), OffView = c[9] == "Up" ? 1 : (c[9] == "Side" ? 2 : 0), OffOrder = I(10),
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
                return;
            }

            // Per-combo-step row ("Attack_Down_1@2") when authored; else R3B pose/VFX offsets below.
            int step = _combat != null && _lastKey.StartsWith("Attack_") ? _combat.CurrentComboStep : -1;
            if (step >= 0 && _table.TryGetValue(_lastKey + "@" + step, out var stepRow)) a = stepRow;

            bool flip = body.flipX;
            int order = body.sortingOrder;
            Color tint = body.color;
            if (_lastKey.StartsWith("Hurt_") && _lastKey.EndsWith("_1")) tint *= hurtTint;

            // R3B: with one Attack body clip, tilt the 8-dir weapon pose by combo step so A1/A2/A3 read differently.
            int mainDir = a.MainDir;
            if (step == 0) mainDir = Wrap8(mainDir + 1);
            else if (step == 1) mainDir = Wrap8(mainDir - 1);
            // step 2 (finisher): keep center pose; slash scale carries the read.

            Place(_main, a.MainX > -99 && a.MainOrder != 0 ? Pick(weaponPoses, mainDir) : null, a.MainX, a.MainY, a.MainOrder, flip, order, tint);
            Place(_hand, a.HandX > -99 && a.MainOrder != 0 ? hand : null, a.HandX, a.HandY, a.MainOrder + 1, flip, order, tint);
            Sprite offSprite = null;
            if (a.OffX > -99 && a.OffOrder != 0)
                offSprite = a.OffDir >= 0 ? Pick(offPoses, a.OffDir) : Pick(shieldViews, a.OffView);
            Place(_off, offSprite, a.OffX, a.OffY, a.OffOrder, flip, order, tint);

            int slashIndex = a.Slash;
            // Prefer dedicated per-step slash blocks (6 frames × step) when the set has them.
            if (slashIndex >= 0 && step > 0 && slashFrames != null && slashFrames.Length >= 6 * (step + 1))
                slashIndex += 6 * step;
            bool slashOn = showSlash && slashIndex >= 0 && slashFrames != null && slashIndex < slashFrames.Length && slashFrames[slashIndex] != null;
            _slash.enabled = slashOn;
            if (slashOn)
            {
                _slash.sprite = slashFrames[slashIndex];
                _slash.transform.localPosition = Vector3.zero;
                // Single slash block: invert facing on step 1 (R→L), enlarge step 2 finisher — do not cover the whole body.
                bool slashFlip = flip;
                float slashScale = 1f;
                if (step == 1) slashFlip = !flip;
                else if (step == 2) slashScale = 1.38f;
                _slash.flipX = slashFlip;
                _slash.transform.localScale = new Vector3(slashScale, slashScale, 1f);
                _slash.sortingLayerID = body.sortingLayerID;
                _slash.sortingOrder = order + a.SlashOrder;
                _slash.color = new Color(1f, 1f, 1f, tint.a);
            }
            else if (_slash != null)
            {
                _slash.transform.localScale = Vector3.one;
            }
        }

        static int Wrap8(int d)
        {
            d %= 8;
            return d < 0 ? d + 8 : d;
        }

        static Sprite Pick(Sprite[] arr, int i) => arr != null && i >= 0 && i < arr.Length ? arr[i] : null;

        // Anchor pixel (x, y) from the top-left of the 32x32 cell; body pivot = (16, 30). The layer sprite's pivot
        // sits on the anchor pixel's top-left corner, so mirroring is just negating x and flipping the sprite.
        void Place(SpriteRenderer r, Sprite s, int x, int y, int relOrder, bool flip, int bodyOrder, Color tint)
        {
            r.enabled = s != null;
            if (s == null) return;
            r.sprite = s;
            float lx = (x - 16) / pixelsPerUnit, ly = (30 - y) / pixelsPerUnit;
            r.transform.localPosition = new Vector3(flip ? -lx : lx, ly, 0f);
            r.flipX = flip;
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
