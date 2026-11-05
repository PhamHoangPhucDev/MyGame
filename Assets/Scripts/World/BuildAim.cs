using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>Where the ghost will land. The pointer on a button does not move it.</summary>
    public static class BuildAim
    {
        public static bool Active { get; private set; }
        public static Vector2 Local { get; private set; }

        public static void Set(Vector2 local)
        {
            Local = new Vector2(Mathf.Round(local.x), Mathf.Round(local.y));
            Active = true;
        }

        public static void Clear() => Active = false;

        public static Vector2 FromScreen(Vector2 screen)
        {
            var cam = Camera.main;
            if (cam == null) return Local;
            Vector3 world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, -cam.transform.position.z));
            var root = GameObject.Find("HomeDistrict");
            return root != null ? (Vector2)root.transform.InverseTransformPoint(world) : (Vector2)world;
        }
    }
}
