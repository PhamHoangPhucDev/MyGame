using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// R3D.1 Aren kit VFX — loads sliced sheets from Assets/Art/Rebuild/R2C/Aren/VFX
    /// (Editor: AssetDatabase; Player: Resources mirror under VFX/ArenKit if present).
    /// </summary>
    public static class ArenKitVfx
    {
        const string ArtRoot = "Assets/Art/Rebuild/R2C/Aren/VFX/";
        const string ResRoot = "VFX/ArenKit/";

        static Sprite[] _thrust, _shieldBash, _glint, _stun, _dust;
        static Sprite[] _whirlBack, _whirlFront, _bulwarkBack, _bulwarkFront, _dawn;
        static bool _loaded;
        static bool _preferDesigned;

        /// <summary>Sandbox designed Aren kit (Shield Bash etc.). Off by default for campaign.</summary>
        public static bool PreferDesignedSkills
        {
            get => _preferDesigned;
            set => _preferDesigned = value;
        }

        public static bool Ready
        {
            get
            {
                EnsureLoaded();
                return _thrust != null && _thrust.Length > 0;
            }
        }

        /// <summary>True when sheets are loaded and the designed Aren kit is the active class.</summary>
        public static bool Active => PreferDesignedSkills && Ready;

        /// <summary>Whirl ring sheets are loaded. Does not require the designed-kit flag.</summary>
        public static bool HasWhirlRing
        {
            get
            {
                EnsureLoaded();
                return _whirlBack != null && _whirlBack.Length > 0
                    && _whirlFront != null && _whirlFront.Length > 0;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic()
        {
            _loaded = false;
            _preferDesigned = false;
            _thrust = _shieldBash = _glint = _stun = _dust = null;
            _whirlBack = _whirlFront = _bulwarkBack = _bulwarkFront = _dawn = null;
        }

        public static void EnsureLoaded()
        {
            if (_loaded) return;
            _loaded = true;
            _thrust = LoadSheet("Aren_Thrust");
            _shieldBash = LoadSheet("Aren_ShieldBash");
            _glint = LoadSheet("Aren_ShieldGlint");
            _stun = LoadSheet("Aren_StunStars");
            _dust = LoadSheet("Aren_ChargeDust");
            _whirlBack = LoadSheet("Aren_WhirlRing_Back");
            _whirlFront = LoadSheet("Aren_WhirlRing_Front");
            _bulwarkBack = LoadSheet("Aren_Bulwark_Back");
            _bulwarkFront = LoadSheet("Aren_Bulwark_Front");
            _dawn = LoadSheet("Aren_Dawn");
        }

        static Sprite[] LoadSheet(string name)
        {
#if UNITY_EDITOR
            var guids = UnityEditor.AssetDatabase.FindAssets(name + " t:Texture2D", new[] { "Assets/Art/Rebuild/R2C/Aren/VFX" });
            if (guids != null && guids.Length > 0)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
                var all = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(path);
                var list = new System.Collections.Generic.List<Sprite>();
                foreach (var o in all)
                    if (o is Sprite sp) list.Add(sp);
                list.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
                if (list.Count > 0) return list.ToArray();
            }
#endif
            var res = Resources.LoadAll<Sprite>(ResRoot + name);
            if (res != null && res.Length > 0)
            {
                System.Array.Sort(res, (a, b) => string.CompareOrdinal(a.name, b.name));
                return res;
            }
            return null;
        }

        /// <summary>Facing → sheet row: Down=0, Up=1, Side=2 (left-facing art; flipX when facing right).</summary>
        public static int FacingRow(Vector2 facing, out bool flipX)
        {
            flipX = false;
            if (facing.sqrMagnitude < 0.0001f) facing = Vector2.down;
            facing.Normalize();
            if (Mathf.Abs(facing.y) >= Mathf.Abs(facing.x))
                return facing.y > 0f ? 1 : 0; // Up / Down
            flipX = facing.x > 0f; // art faces left
            return 2;
        }

        static Sprite[] RowFrames(Sprite[] sheet, int row, int cols)
        {
            if (sheet == null || sheet.Length < cols) return null;
            int start = row * cols;
            if (start + cols > sheet.Length) start = 0;
            var slice = new Sprite[cols];
            for (int i = 0; i < cols; i++) slice[i] = sheet[start + i];
            return slice;
        }

        public static void PlayThrust(Vector3 bodyPos, Vector2 facing)
        {
            EnsureLoaded();
            int row = FacingRow(facing, out bool flip);
            var frames = RowFrames(_thrust, row, 3);
            // Up row draws behind body (order -3).
            int order = row == 1 ? -3 : 3;
            KitVfx.Play(frames, bodyPos, 16f, flip, order);
        }

        public static void PlayShieldBash(Vector3 bodyPos, Vector2 facing)
        {
            EnsureLoaded();
            int row = FacingRow(facing, out bool flip);
            KitVfx.Play(RowFrames(_shieldBash, row, 3), bodyPos, 18f, flip, 4);
            KitVfx.Play(_glint, bodyPos + (Vector3)(facing.normalized * 0.15f) + Vector3.up * 0.2f, 16f, flip, 5);
        }

        public static void PlayDawn(Vector3 bodyPos, Vector2 facing)
        {
            EnsureLoaded();
            int row = FacingRow(facing, out bool flip);
            KitVfx.Play(RowFrames(_dawn, row, 3), bodyPos + (Vector3)(facing.normalized * 0.25f), 14f, flip, 4);
        }

        public static void PlayWhirlRing(Transform host)
        {
            EnsureLoaded();
            if (host == null) return;
            KitVfx.PlayAttached(_whirlBack, host, Vector3.zero, 16f, false, -3, false);
            KitVfx.PlayAttached(_whirlFront, host, Vector3.zero, 16f, false, 3, false);
        }

        /// <summary>Bulwark: appear (0-1) → loop (2-5) while guarding → break (6-7) on end.</summary>
        public static BulwarkHandle BeginBulwark(Transform host)
        {
            EnsureLoaded();
            if (host == null || _bulwarkBack == null) return default;
            var h = new BulwarkHandle
            {
                back = KitVfx.PlayAttached(Slice(_bulwarkBack, 0, 2), host, Vector3.zero, 14f, false, -3, false),
                front = KitVfx.PlayAttached(Slice(_bulwarkFront, 0, 2), host, Vector3.zero, 14f, false, 3, false)
            };
            return h;
        }

        public static void LoopBulwark(ref BulwarkHandle h, Transform host)
        {
            EnsureLoaded();
            if (host == null) return;
            if (h.back != null) Object.Destroy(h.back.gameObject);
            if (h.front != null) Object.Destroy(h.front.gameObject);
            h.back = KitVfx.PlayAttached(Slice(_bulwarkBack, 2, 4), host, Vector3.zero, 8f, false, -3, true);
            h.front = KitVfx.PlayAttached(Slice(_bulwarkFront, 2, 4), host, Vector3.zero, 8f, false, 3, true);
        }

        public static void EndBulwark(ref BulwarkHandle h, Transform host)
        {
            EnsureLoaded();
            if (h.back != null) Object.Destroy(h.back.gameObject);
            if (h.front != null) Object.Destroy(h.front.gameObject);
            h = default;
            if (host == null) return;
            KitVfx.PlayAttached(Slice(_bulwarkBack, 6, 2), host, Vector3.zero, 14f, false, -3, false);
            KitVfx.PlayAttached(Slice(_bulwarkFront, 6, 2), host, Vector3.zero, 14f, false, 3, false);
        }

        public static void PlayChargeDust(Vector3 feetPos)
        {
            EnsureLoaded();
            KitVfx.Play(_dust, feetPos, 14f, false, 2);
        }

        public static void SpawnChargeGhost(SpriteRenderer src)
        {
            ChargeGhost.Spawn(src);
        }

        public static Sprite[] StunStarFrames
        {
            get { EnsureLoaded(); return _stun; }
        }

        static Sprite[] Slice(Sprite[] src, int start, int count)
        {
            if (src == null || start >= src.Length) return null;
            count = Mathf.Min(count, src.Length - start);
            var a = new Sprite[count];
            for (int i = 0; i < count; i++) a[i] = src[start + i];
            return a;
        }

        public struct BulwarkHandle
        {
            public KitVfx back, front;
        }
    }

    /// <summary>Aren charge afterimage. Fades in 0.15 s and never more than four at once.</summary>
    public class ChargeGhost : MonoBehaviour
    {
        static readonly System.Collections.Generic.List<ChargeGhost> Live = new();
        SpriteRenderer _sr;
        float _life;
        const float Fade = 0.15f;
        static readonly Color Tint = new Color(0.55f, 0.8f, 1f, 0.55f);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() => Live.Clear();

        public static void Spawn(SpriteRenderer src)
        {
            if (src == null || src.sprite == null) return;
            while (Live.Count >= 4)
            {
                var old = Live[0];
                Live.RemoveAt(0);
                if (old != null) Destroy(old.gameObject);
            }
            var go = new GameObject("ChargeGhost");
            go.transform.position = src.transform.position;
            go.transform.rotation = src.transform.rotation;
            Vector3 scale = src.transform.lossyScale;
            go.transform.localScale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = src.sprite;
            sr.flipX = src.flipX;
            sr.flipY = src.flipY;
            sr.sortingLayerID = src.sortingLayerID;
            sr.sortingOrder = src.sortingOrder - 1;
            sr.color = Tint;
            var ghost = go.AddComponent<ChargeGhost>();
            ghost._sr = sr;
            Live.Add(ghost);
        }

        void Update()
        {
            _life += Time.deltaTime;
            float a = Tint.a * (1f - Mathf.Clamp01(_life / Fade));
            if (_sr != null) _sr.color = new Color(Tint.r, Tint.g, Tint.b, a);
            if (_life < Fade) return;
            Live.Remove(this);
            Destroy(gameObject);
        }
    }
}
