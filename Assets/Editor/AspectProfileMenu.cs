using UnityEditor;
using UnityEngine;
using GameplayPrototype;

public static class AspectProfileMenu
{
    const string LandPath = "Assets/Data/AspectProfiles/Landscape_16x9.asset";
    const string PortPath = "Assets/Data/AspectProfiles/Portrait_9x16.asset";

    [MenuItem("Tools/Gameplay Prototype/R1/Aspect/Ensure Profile Assets")]
    public static void EnsureAssets()
    {
        System.IO.Directory.CreateDirectory("Assets/Data/AspectProfiles");
        Ensure(LandPath, AspectProfileId.Landscape);
        Ensure(PortPath, AspectProfileId.Portrait);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[Aspect] Profile assets ready.");
    }

    static AspectVisualProfile Ensure(string path, AspectProfileId id)
    {
        var existing = AssetDatabase.LoadAssetAtPath<AspectVisualProfile>(path);
        if (existing != null) return existing;
        var runtime = AspectVisualProfile.CreateRuntime(id);
        AssetDatabase.CreateAsset(runtime, path);
        return runtime;
    }

    [MenuItem("Tools/Gameplay Prototype/R1/Aspect/Apply Landscape 16:9 (1920x1080)")]
    public static void ApplyLandscape()
    {
        EnsureAssets();
        Apply(AspectProfileId.Landscape);
    }

    [MenuItem("Tools/Gameplay Prototype/R1/Aspect/Apply Portrait 9:16 (576x1024)")]
    public static void ApplyPortrait()
    {
        EnsureAssets();
        Apply(AspectProfileId.Portrait);
    }

    [MenuItem("Tools/Gameplay Prototype/R1/Aspect/Toggle Landscape/Portrait")]
    public static void Toggle()
    {
        EnsureAssets();
        var svc = Object.FindFirstObjectByType<AspectProfileService>();
        var next = svc != null && svc.ActiveId == AspectProfileId.Landscape
            ? AspectProfileId.Portrait
            : AspectProfileId.Landscape;
        Apply(next);
    }

    static void Apply(AspectProfileId id)
    {
        var land = AssetDatabase.LoadAssetAtPath<AspectVisualProfile>(LandPath);
        var port = AssetDatabase.LoadAssetAtPath<AspectVisualProfile>(PortPath);
        var profile = id == AspectProfileId.Portrait ? port : land;

        var cam = Camera.main;
        if (cam != null)
        {
            var ppc = cam.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
            if (ppc != null)
            {
                ppc.assetsPPU = profile.assetsPPU;
                ppc.refResolutionX = profile.pixelPerfectRef.x;
                ppc.refResolutionY = profile.pixelPerfectRef.y;
                EditorUtility.SetDirty(ppc);
            }
            var follow = cam.GetComponent<CameraFollow>();
            if (follow != null)
            {
                follow.SetFollowOffset(profile.cameraFollowOffset);
                follow.SetKeepOnScreenMargins(profile.keepOnScreenMargins.x, profile.keepOnScreenMargins.y, profile.keepOnScreenMargins.z);
                follow.SetBounds(profile.defaultBoundsMin, profile.defaultBoundsMax);
                EditorUtility.SetDirty(follow);
            }
        }

        var svc = Object.FindFirstObjectByType<AspectProfileService>();
        if (svc != null)
        {
            svc.SetProfiles(land, port);
            svc.Apply(id);
        }

        AspectGameViewHelper.SetSize(profile.testGameView.x, profile.testGameView.y);
        Debug.Log($"[Aspect] Editor applied {id} GameView {profile.testGameView.x}x{profile.testGameView.y}");
    }
}
