using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>
    /// Single active aspect profile. Applies camera/HUD only — never duplicates combat or input.
    /// Does not force Screen.SetResolution on device; Editor Game View size is optional.
    /// </summary>
    public class AspectProfileService : MonoBehaviour
    {
        public static AspectProfileService Instance { get; private set; }

        [SerializeField] AspectVisualProfile landscape;
        [SerializeField] AspectVisualProfile portrait;
        [SerializeField] AspectProfileId activeId = AspectProfileId.Landscape;
        AspectVisualProfile _active;
        CameraFollow _follow;
        PixelPerfectCamera _ppc;
        Transform _joystick;

        public AspectVisualProfile Active => _active;
        public AspectProfileId ActiveId => activeId;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatic() => Instance = null;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;
            EnsureProfiles();
            CacheCamera();
            Apply(activeId);
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        void EnsureProfiles()
        {
            if (landscape == null) landscape = AspectVisualProfile.CreateRuntime(AspectProfileId.Landscape);
            if (portrait == null) portrait = AspectVisualProfile.CreateRuntime(AspectProfileId.Portrait);
        }

        void CacheCamera()
        {
            var cam = Camera.main;
            if (cam == null) return;
            _follow = cam.GetComponent<CameraFollow>();
            _ppc = cam.GetComponent<PixelPerfectCamera>();
        }

        public void SetProfiles(AspectVisualProfile land, AspectVisualProfile port)
        {
            if (land != null) landscape = land;
            if (port != null) portrait = port;
        }

        public void Apply(AspectProfileId id)
        {
            EnsureProfiles();
            CacheCamera();
            activeId = id;
            _active = id == AspectProfileId.Portrait ? portrait : landscape;
            ApplyCamera(_active);
            ApplyHud(_active);
            ApplyJoystick(_active);
            Debug.Log($"[Aspect] Applied {_active.id} PPC {_active.pixelPerfectRef.x}x{_active.pixelPerfectRef.y} world {_active.WorldWidth:F2}x{_active.WorldHeight:F2}");
        }

        public void Toggle()
        {
            Apply(activeId == AspectProfileId.Landscape ? AspectProfileId.Portrait : AspectProfileId.Landscape);
        }

        void ApplyCamera(AspectVisualProfile p)
        {
            if (_ppc != null)
            {
                _ppc.assetsPPU = Mathf.Max(1, p.assetsPPU);
                _ppc.refResolutionX = p.pixelPerfectRef.x;
                _ppc.refResolutionY = p.pixelPerfectRef.y;
            }
            if (_follow != null)
            {
                _follow.SetFollowOffset(p.cameraFollowOffset);
                _follow.SetKeepOnScreenMargins(p.keepOnScreenMargins.x, p.keepOnScreenMargins.y, p.keepOnScreenMargins.z);
                _follow.SetBounds(p.defaultBoundsMin, p.defaultBoundsMax);
            }
        }

        void ApplyHud(AspectVisualProfile p)
        {
            var scalers = FindObjectsByType<CanvasScaler>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var s in scalers)
            {
                if (s.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) continue;
                s.referenceResolution = p.hudScalerReference;
                s.matchWidthOrHeight = p.hudMatchWidthOrHeight;
            }
        }

        void ApplyJoystick(AspectVisualProfile p)
        {
            if (_joystick == null)
            {
                var go = GameObject.Find("JoystickPad");
                if (go != null) _joystick = go.transform;
            }
            if (_joystick != null)
                _joystick.gameObject.SetActive(p.showMobileJoystick);
        }

        /// <summary>Hero sprite height / camera ortho height (viewport fraction).</summary>
        public static float MeasureHeroViewportFraction(Transform hero, Camera cam)
        {
            if (hero == null || cam == null) return 0f;
            float heroH = 0f;
            foreach (var sr in hero.GetComponentsInChildren<SpriteRenderer>())
            {
                if (sr == null || sr.sprite == null || !sr.enabled) continue;
                if (sr.name.IndexOf("Sword", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (sr.name.IndexOf("Shadow", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
                if (sr.name.IndexOf("Canopy", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
                heroH = Mathf.Max(heroH, sr.bounds.size.y);
            }
            float viewH = cam.orthographicSize * 2f;
            return viewH > 0.0001f ? heroH / viewH : 0f;
        }
    }
}
