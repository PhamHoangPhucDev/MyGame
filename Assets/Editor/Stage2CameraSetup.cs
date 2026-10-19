using GameplayPrototype;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;

/// <summary>
/// Portrait 9:16 pixel-perfect framing at 144x256 (ortho 8 at 16 PPU).
/// Does not touch combat components.
/// </summary>
public static class Stage2CameraSetup
{
    const string ScenePath = "Assets/Scenes/GameplayPrototype.unity";
    const string ArtFolder = "Assets/Art/Placeholders";

    [MenuItem("Tools/Gameplay Prototype/Apply Portrait Camera")]
    public static void Apply()
    {
        var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        var cam = Camera.main;
        if (cam == null)
        {
            Debug.LogError("[Camera] Main Camera missing.");
            return;
        }

        // Verify sprite PPU before applying PPC.
        var player = GameObject.Find("Player");
        var playerSr = player != null ? player.GetComponent<SpriteRenderer>() : null;
        float heroPpu = playerSr != null && playerSr.sprite != null ? playerSr.sprite.pixelsPerUnit : 16f;
        if (!Mathf.Approximately(heroPpu, 16f))
            Debug.LogWarning($"[Camera] Hero sprite PPU is {heroPpu}, expected 16. PPC assetsPPU will still use 16 for map consistency.");

        cam.orthographic = true;
        cam.backgroundColor = new Color(0.22f, 0.32f, 0.18f, 1f);

        var ppc = cam.GetComponent<PixelPerfectCamera>();
        if (ppc == null)
            ppc = cam.gameObject.AddComponent<PixelPerfectCamera>();

        // 144x256 @ 16 PPU => orthographicSize = 256*0.5/16 = 8.
        // At Game view 576x1024 => exact integer zoom 4x (no PPC resolution warning).
        ppc.assetsPPU = 16;
        ppc.refResolutionX = 144;
        ppc.refResolutionY = 256;
        ppc.cropFrame = PixelPerfectCamera.CropFrame.Windowbox;
        ppc.gridSnapping = PixelPerfectCamera.GridSnapping.UpscaleRenderTexture;

        var follow = cam.GetComponent<CameraFollow>();
        if (follow != null)
        {
            var so = new SerializedObject(follow);
            so.FindProperty("smoothTime").floatValue = 0.1f;
            // Slight upward look so hotbar does not cover the hero feet area as much.
            so.FindProperty("offset").vector3Value = new Vector3(0f, 0.5f, -10f);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        EnsurePointFilteringOnPlaceholders();
        ScalePropsInteger();
        TrySetGameView576x1024();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        float heroH = playerSr != null && playerSr.sprite != null ? playerSr.sprite.bounds.size.y : 0f;
        float screenH = 16f; // ortho 8 => world height 16
        Debug.Log($"[Camera] PPC 144x256 PPU16 (ortho~8). Hero worldH={heroH:F2} ≈ {screenH / Mathf.Max(0.001f, heroH):F1}ths of screen. Props use integer scales.");
    }

    static void ScalePropsInteger()
    {
        var forest = GameObject.Find("ForestRoot");
        if (forest == null) return;

        foreach (Transform child in forest.transform)
        {
            // Fractional scales blur/break pixel grid. Use only 1x or 2x.
            float scale = child.name switch
            {
                "Tree" => 2f,
                "Tent" => 2f,
                "Bush" => 2f,
                "Cauldron" => 2f,
                "Flag" => 2f,
                "Bones" => 1f,
                "Rock" => 1f,
                _ => 1f // ground tiles stay 1x
            };
            child.localScale = new Vector3(scale, scale, 1f);
        }
    }

    static void EnsurePointFilteringOnPlaceholders()
    {
        string[] guids = AssetDatabase.FindAssets("t:Texture2D", new[] { ArtFolder });
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) continue;
            bool dirty = false;
            if (importer.filterMode != FilterMode.Point)
            {
                importer.filterMode = FilterMode.Point;
                dirty = true;
            }
            if (importer.textureCompression != TextureImporterCompression.Uncompressed)
            {
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                dirty = true;
            }
            if (importer.mipmapEnabled)
            {
                importer.mipmapEnabled = false;
                dirty = true;
            }
            if (importer.spritePixelsPerUnit != 16f)
            {
                importer.spritePixelsPerUnit = 16f;
                dirty = true;
            }
            if (dirty)
                importer.SaveAndReimport();
        }
    }

    static void TrySetGameView576x1024()
    {
        try
        {
            var assembly = typeof(EditorWindow).Assembly;
            var sizesType = assembly.GetType("UnityEditor.GameViewSizes");
            var singleton = typeof(ScriptableSingleton<>).MakeGenericType(sizesType);
            var instance = singleton.GetProperty("instance").GetValue(null);
            var group = sizesType.GetProperty("currentGroupType").GetValue(instance);
            var groupObj = sizesType.GetMethod("GetGroup").Invoke(instance, new[] { group });

            // Prefer selecting an existing 576x1024 entry instead of adding duplicates.
            int total = (int)groupObj.GetType().GetMethod("GetTotalCount").Invoke(groupObj, null);
            int targetIndex = -1;
            var getSize = groupObj.GetType().GetMethod("GetGameViewSize");
            for (int i = 0; i < total; i++)
            {
                var size = getSize.Invoke(groupObj, new object[] { i });
                int w = (int)size.GetType().GetProperty("width").GetValue(size);
                int h = (int)size.GetType().GetProperty("height").GetValue(size);
                if (w == 576 && h == 1024)
                {
                    targetIndex = i;
                    break;
                }
            }

            if (targetIndex < 0)
            {
                var sizeType = assembly.GetType("UnityEditor.GameViewSize");
                var sizeEnum = assembly.GetType("UnityEditor.GameViewSizeType");
                var fixedRes = System.Enum.Parse(sizeEnum, "FixedResolution");
                var ctor = sizeType.GetConstructor(new[] { sizeEnum, typeof(int), typeof(int), typeof(string) });
                var custom = ctor.Invoke(new object[] { fixedRes, 576, 1024, "Portrait 9:16" });
                groupObj.GetType().GetMethod("AddCustomSize").Invoke(groupObj, new[] { custom });
                targetIndex = (int)groupObj.GetType().GetMethod("GetTotalCount").Invoke(groupObj, null) - 1;
            }

            var gameViewType = assembly.GetType("UnityEditor.GameView");
            var window = EditorWindow.GetWindow(gameViewType);
            gameViewType.GetMethod("SizeSelectionCallback",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic)
                ?.Invoke(window, new object[] { targetIndex, null });
            Debug.Log("[Camera] Game view set to 576x1024 (4x of 144x256).");
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning("[Camera] Could not set Game view size automatically: " + ex.Message
                             + " Set Game view to 576x1024 manually.");
        }
    }
}
