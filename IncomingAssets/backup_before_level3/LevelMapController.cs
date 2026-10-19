using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Switches between authored map roots (Camp vs Trail) without duplicating spawners.
    /// </summary>
    public class LevelMapController : MonoBehaviour
    {
        [SerializeField] GameObject campRoot;
        [SerializeField] GameObject trailRoot;
        [SerializeField] CameraFollow cameraFollow;

        public string ActiveLayoutId { get; private set; } = "Camp";

        void Awake()
        {
            if (cameraFollow == null && Camera.main != null)
                cameraFollow = Camera.main.GetComponent<CameraFollow>();
            if (campRoot == null)
            {
                var maps = GameObject.Find("LevelMaps");
                if (maps != null)
                {
                    var camp = maps.transform.Find("Camp");
                    if (camp != null) campRoot = camp.gameObject;
                    var trail = maps.transform.Find("Trail");
                    if (trail != null) trailRoot = trail.gameObject;
                }
            }
        }

        public void ApplyLayout(LevelConfig config)
        {
            if (config == null) return;
            string id = string.IsNullOrEmpty(config.mapLayoutId) ? "Camp" : config.mapLayoutId;
            ActiveLayoutId = id;
            bool trail = id.Equals("Trail", System.StringComparison.OrdinalIgnoreCase);
            if (campRoot != null) campRoot.SetActive(!trail);
            if (trailRoot != null) trailRoot.SetActive(trail);
            // Legacy scene roots if maps were not reparented yet.
            var forestGrid = GameObject.Find("ForestGrid");
            var forestProps = GameObject.Find("ForestProps");
            if (campRoot == null)
            {
                if (forestGrid != null) forestGrid.SetActive(!trail);
                if (forestProps != null) forestProps.SetActive(!trail);
            }
            if (cameraFollow != null)
                cameraFollow.SetBounds(config.cameraBoundsMin, config.cameraBoundsMax);
        }
    }
}
