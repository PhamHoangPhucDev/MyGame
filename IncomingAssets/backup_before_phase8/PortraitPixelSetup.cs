using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Portrait camera color only. Orthographic size is owned by Pixel Perfect Camera.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class PortraitPixelSetup : MonoBehaviour
    {
        void Awake()
        {
            var cam = GetComponent<Camera>();
            cam.orthographic = true;
            cam.backgroundColor = new Color(0.22f, 0.32f, 0.18f, 1f);
            cam.clearFlags = CameraClearFlags.SolidColor;
        }
    }
}
