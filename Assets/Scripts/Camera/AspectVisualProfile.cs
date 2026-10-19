using UnityEngine;

namespace GameplayPrototype
{
    public enum AspectProfileId
    {
        Landscape = 0,
        Portrait = 1
    }

    /// <summary>
    /// Dual-aspect presentation profile. Shared gameplay core; camera/HUD only.
    /// </summary>
    [CreateAssetMenu(menuName = "Gameplay Prototype/Aspect Visual Profile", fileName = "AspectVisualProfile")]
    public class AspectVisualProfile : ScriptableObject
    {
        public AspectProfileId id = AspectProfileId.Landscape;
        public Vector2Int pixelPerfectRef = new(480, 270);
        public Vector2Int testGameView = new(1920, 1080);
        public int assetsPPU = 16;
        public Vector3 cameraFollowOffset = new(0f, -0.45f, -10f);
        [Tooltip("Keep-on-screen margins: X=side, Y=top, Z=bottom (world units).")]
        public Vector3 keepOnScreenMargins = new(3f, 2.2f, 4f);
        public Vector2 hudScalerReference = new(1920f, 1080f);
        [Range(0f, 1f)] public float hudMatchWidthOrHeight = 0.5f;
        public bool showMobileJoystick = false;
        public Vector2 defaultBoundsMin = new(-8f, -10f);
        public Vector2 defaultBoundsMax = new(8f, 10f);

        public float WorldWidth => pixelPerfectRef.x / (float)Mathf.Max(1, assetsPPU);
        public float WorldHeight => pixelPerfectRef.y / (float)Mathf.Max(1, assetsPPU);
        public float AspectRatio => WorldWidth / Mathf.Max(0.0001f, WorldHeight);

        public static AspectVisualProfile CreateRuntime(AspectProfileId profileId)
        {
            var p = CreateInstance<AspectVisualProfile>();
            if (profileId == AspectProfileId.Portrait)
            {
                p.id = AspectProfileId.Portrait;
                p.name = "Portrait_9x16";
                p.pixelPerfectRef = new Vector2Int(144, 256);
                p.testGameView = new Vector2Int(576, 1024);
                p.cameraFollowOffset = new Vector3(0f, -0.2f, -10f);
                p.keepOnScreenMargins = new Vector3(1.2f, 2.0f, 3.2f);
                p.hudScalerReference = new Vector2(576f, 1024f);
                p.hudMatchWidthOrHeight = 0.65f;
                p.showMobileJoystick = true;
                p.defaultBoundsMin = new Vector2(-4.2f, -7.5f);
                p.defaultBoundsMax = new Vector2(4.2f, 7.5f);
            }
            else
            {
                p.id = AspectProfileId.Landscape;
                p.name = "Landscape_16x9";
                p.pixelPerfectRef = new Vector2Int(480, 270);
                p.testGameView = new Vector2Int(1920, 1080);
                p.cameraFollowOffset = new Vector3(0f, -0.45f, -10f);
                p.keepOnScreenMargins = new Vector3(3f, 2.2f, 4f);
                p.hudScalerReference = new Vector2(1920f, 1080f);
                p.hudMatchWidthOrHeight = 0.5f;
                p.showMobileJoystick = false;
                p.defaultBoundsMin = new Vector2(-10f, -8f);
                p.defaultBoundsMax = new Vector2(10f, 10f);
            }
            p.assetsPPU = 16;
            return p;
        }
    }
}
