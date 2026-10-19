using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Switches between authored map roots without duplicating spawners.
    /// Layouts: Camp, Trail, AbandonedCamp, GoblinOutpost.
    /// </summary>
    public class LevelMapController : MonoBehaviour
    {
        [SerializeField] GameObject campRoot;
        [SerializeField] GameObject trailRoot;
        [SerializeField] GameObject abandonedCampRoot;
        [SerializeField] GameObject goblinOutpostRoot;
        [SerializeField] CameraFollow cameraFollow;

        public string ActiveLayoutId { get; private set; } = "Camp";

        void Awake()
        {
            if (cameraFollow == null && Camera.main != null)
                cameraFollow = Camera.main.GetComponent<CameraFollow>();
            ResolveRoots();
        }

        void ResolveRoots()
        {
            var maps = GameObject.Find("LevelMaps");
            if (maps == null) return;
            if (campRoot == null)
            {
                var camp = maps.transform.Find("Camp");
                if (camp != null) campRoot = camp.gameObject;
            }
            if (trailRoot == null)
            {
                var trail = maps.transform.Find("Trail");
                if (trail != null) trailRoot = trail.gameObject;
            }
            if (abandonedCampRoot == null)
            {
                var abandoned = maps.transform.Find("AbandonedCamp");
                if (abandoned != null) abandonedCampRoot = abandoned.gameObject;
            }
            if (goblinOutpostRoot == null)
            {
                var outpost = maps.transform.Find("GoblinOutpost");
                if (outpost != null) goblinOutpostRoot = outpost.gameObject;
            }
        }

        public void ApplyLayout(LevelConfig config)
        {
            if (config == null) return;
            ResolveRoots();
            string id = string.IsNullOrEmpty(config.mapLayoutId) ? "Camp" : config.mapLayoutId;
            ActiveLayoutId = id;

            bool isCamp = id.Equals("Camp", System.StringComparison.OrdinalIgnoreCase);
            bool isTrail = id.Equals("Trail", System.StringComparison.OrdinalIgnoreCase);
            bool isAbandoned = id.Equals("AbandonedCamp", System.StringComparison.OrdinalIgnoreCase);
            bool isOutpost = id.Equals("GoblinOutpost", System.StringComparison.OrdinalIgnoreCase);

            if (campRoot != null) campRoot.SetActive(isCamp);
            if (trailRoot != null) trailRoot.SetActive(isTrail);
            if (abandonedCampRoot != null) abandonedCampRoot.SetActive(isAbandoned);
            if (goblinOutpostRoot != null) goblinOutpostRoot.SetActive(isOutpost);

            var forestGrid = GameObject.Find("ForestGrid");
            var forestProps = GameObject.Find("ForestProps");
            if (campRoot == null)
            {
                if (forestGrid != null) forestGrid.SetActive(isCamp);
                if (forestProps != null) forestProps.SetActive(isCamp);
            }

            if (cameraFollow != null)
                cameraFollow.SetBounds(config.cameraBoundsMin, config.cameraBoundsMax);
        }
    }
}
