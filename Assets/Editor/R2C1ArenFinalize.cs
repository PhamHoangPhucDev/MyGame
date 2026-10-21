using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using GameplayPrototype;

/// <summary>
/// R2C.1: promote Player_Aren as shared knight prefab; connect scene Players safely;
/// add AspectProfileService to campaign (Landscape default). ClassCombat stays Sandbox-only.
/// </summary>
public static class R2C1ArenFinalize
{
    const string PrefabPath = "Assets/Art/Rebuild/R2C/Aren/Prefabs/Player_Aren.prefab";
    const string CtrlPath = "Assets/Art/Rebuild/R2C/Aren/Animators/Aren.controller";
    const string EquipSet = "Assets/Art/Rebuild/R2C/ClassKits/EquipmentSet_Aren.asset";
    const string Anchors = "Assets/Art/Rebuild/R2C/Aren/Aren_Anchors.csv";
    const string Campaign = "Assets/Scenes/GameplayPrototype_Phase4Test.unity";
    const string Sandbox = "Assets/Scenes/ReferenceCombatSandbox.unity";
    const string LandProfile = "Assets/Data/AspectProfiles/Landscape_16x9.asset";
    const string PortProfile = "Assets/Data/AspectProfiles/Portrait_9x16.asset";
    const string ArchiveNote = "Assets/Art/GoblinCampPhase4/ARCHIVED_LEGACY_HERO.txt";

    [MenuItem("Tools/Gameplay Prototype/R2C/R2C.1 Finalize Aren Integration")]
    public static void RunIntegration()
    {
        if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CtrlPath) == null)
            throw new System.Exception("Missing Aren.controller");

        AspectProfileMenu.EnsureAssets();
        WriteArchiveNote();

        // --- 1) Canonical prefab = campaign Player (knight core, NO ClassCombat) ---
        var camp = EditorSceneManager.OpenScene(Campaign);
        var campPlayer = FindPlayer();
        if (campPlayer == null) throw new System.Exception("Campaign Player missing");
        StripClassCombat(campPlayer);
        WireAren(campPlayer);
        Directory.CreateDirectory(Path.GetDirectoryName(PrefabPath)!.Replace('\\', '/'));
        if (PrefabUtility.GetPrefabInstanceStatus(campPlayer) != PrefabInstanceStatus.Connected)
            PrefabUtility.SaveAsPrefabAssetAndConnect(campPlayer, PrefabPath, InteractionMode.AutomatedAction);
        else
            PrefabUtility.ApplyPrefabInstance(campPlayer, InteractionMode.AutomatedAction);
        campPlayer = FindPlayer();
        if (campPlayer != null && campPlayer.name != "Player") campPlayer.name = "Player";
        EnsureCameraFollow(campPlayer.transform);
        EnsureCampaignAspect();
        EditorSceneManager.MarkSceneDirty(camp);
        EditorSceneManager.SaveScene(camp);
        Debug.Log("[R2C.1] Campaign Player connected → " + PrefabPath);

        // --- 2) Sandbox: convert to same prefab; keep ClassCombat as instance override ---
        ConnectSandboxToPrefab();

        // Re-apply campaign as source of truth so sandbox ClassCombat cannot pollute prefab.
        camp = EditorSceneManager.OpenScene(Campaign);
        campPlayer = FindPlayer();
        if (campPlayer != null && PrefabUtility.IsPartOfPrefabInstance(campPlayer))
            PrefabUtility.ApplyPrefabInstance(campPlayer, InteractionMode.AutomatedAction);

        AssetDatabase.SaveAssets();
        Debug.Log("[R2C.1] Done. Prefab=Player_Aren; Campaign Aspect=Landscape default; Sandbox ClassCombat instance-only.");
    }

    [MenuItem("Tools/Gameplay Prototype/R2C/R2C.1 Connect Sandbox Player Only")]
    public static void ConnectSandboxToPrefab()
    {
        var sand = EditorSceneManager.OpenScene(Sandbox);
        var sandPlayer = FindPlayer();
        if (sandPlayer == null) throw new System.Exception("Sandbox Player missing");

        bool wantClassCombat = sandPlayer.GetComponent<ClassCombat>() != null;
        var pos = sandPlayer.transform.position;
        StripClassCombat(sandPlayer);

        var prefabAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefabAsset == null) throw new System.Exception("Missing prefab " + PrefabPath);

        var status = PrefabUtility.GetPrefabInstanceStatus(sandPlayer);
        if (status != PrefabInstanceStatus.Connected)
        {
            try
            {
                var settings = new ConvertToPrefabInstanceSettings
                {
                    objectMatchMode = ObjectMatchMode.ByHierarchy,
                    componentsNotMatchedBecomesOverride = true,
                    gameObjectsNotMatchedBecomesOverride = true,
                    recordPropertyOverridesOfMatches = true
                };
                PrefabUtility.ConvertToPrefabInstance(sandPlayer, prefabAsset, settings, InteractionMode.AutomatedAction);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning("[R2C.1] ConvertToPrefabInstance failed (" + ex.Message + "); falling back to instantiate+transfer.");
                sandPlayer = ReplaceWithPrefabInstance(sandPlayer, prefabAsset);
            }
        }

        sandPlayer = FindPlayer();
        if (sandPlayer == null) throw new System.Exception("Sandbox Player missing after prefab connect");
        if (sandPlayer.name != "Player") sandPlayer.name = "Player"; // prefab asset name must not rename scene GO
        sandPlayer.transform.position = pos;
        WireAren(sandPlayer);
        if (wantClassCombat && sandPlayer.GetComponent<ClassCombat>() == null)
            sandPlayer.AddComponent<ClassCombat>();

        EnsureCameraFollow(sandPlayer.transform);
        EditorSceneManager.MarkSceneDirty(sand);
        EditorSceneManager.SaveScene(sand);
        Debug.Log("[R2C.1] Sandbox Player connected. prefabStatus=" +
                  PrefabUtility.GetPrefabInstanceStatus(sandPlayer) +
                  " classCombat=" + (sandPlayer.GetComponent<ClassCombat>() != null));
    }

    static GameObject FindPlayer()
    {
        var go = GameObject.Find("Player") ?? GameObject.Find("Player_Aren");
        if (go != null) return go;
        try
        {
            var tagged = GameObject.FindGameObjectWithTag("Player");
            if (tagged != null) return tagged;
        }
        catch { /* tag may be missing */ }
        var pcs = Object.FindObjectsByType<PlayerCombat>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        return pcs != null && pcs.Length > 0 ? pcs[0].gameObject : null;
    }

    /// <summary>
    /// Last-resort: instantiate prefab, copy transform, destroy old Player, rewire LevelFlow target refs.
    /// </summary>
    static GameObject ReplaceWithPrefabInstance(GameObject oldPlayer, GameObject prefabAsset)
    {
        var pos = oldPlayer.transform.position;
        var rot = oldPlayer.transform.rotation;
        var parent = oldPlayer.transform.parent;
        var sibling = oldPlayer.transform.GetSiblingIndex();

        // Capture LevelFlow / CameraFollow refs that pointed at oldPlayer.
        var flows = Object.FindObjectsByType<LevelFlow>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var follows = Object.FindObjectsByType<CameraFollow>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        Object.DestroyImmediate(oldPlayer);
        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefabAsset);
        instance.name = "Player";
        instance.transform.SetParent(parent);
        instance.transform.SetSiblingIndex(sibling);
        instance.transform.SetPositionAndRotation(pos, rot);

        foreach (var f in flows)
        {
            if (f == null) continue;
            var so = new SerializedObject(f);
            AssignIfNull(so, "player", instance.transform);
            AssignIfNull(so, "playerHealth", instance.GetComponent<Health>());
            AssignIfNull(so, "movement", instance.GetComponent<PlayerMovement>());
            AssignIfNull(so, "combat", instance.GetComponent<PlayerCombat>());
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        foreach (var c in follows)
        {
            if (c != null) c.SetTarget(instance.transform);
        }
        return instance;
    }

    static void StripClassCombat(GameObject player)
    {
        var cc = player.GetComponent<ClassCombat>();
        if (cc != null) Object.DestroyImmediate(cc, true);
    }

    static void WireAren(GameObject player)
    {
        if (player == null) throw new System.Exception("WireAren: player is null");
        var visual = player.GetComponent<PlayerVisual>();
        if (visual == null) throw new System.Exception("PlayerVisual missing on " + player.name);
        var so = new SerializedObject(visual);
        so.FindProperty("bodyController").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CtrlPath);
        so.ApplyModifiedPropertiesWithoutUndo();

        var eq = player.GetComponent<EquipmentVisual>() ?? player.AddComponent<EquipmentVisual>();
        var eso = new SerializedObject(eq);
        eso.FindProperty("set").objectReferenceValue = AssetDatabase.LoadAssetAtPath<EquipmentSet>(EquipSet);
        eso.FindProperty("anchorTable").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(Anchors);
        eso.FindProperty("replaceLegacyRSlash").boolValue = true;
        eso.ApplyModifiedPropertiesWithoutUndo();
        eq.enabled = true;
    }

    static void EnsureCameraFollow(Transform player)
    {
        var cam = Camera.main;
        if (cam == null) return;
        var follow = cam.GetComponent<CameraFollow>();
        if (follow != null) follow.SetTarget(player);
    }

    static void EnsureCampaignAspect()
    {
        var systems = GameObject.Find("Systems");
        if (systems == null) systems = new GameObject("Systems");
        var svc = systems.GetComponent<AspectProfileService>() ?? systems.AddComponent<AspectProfileService>();
        var so = new SerializedObject(svc);
        so.FindProperty("landscape").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AspectVisualProfile>(LandProfile);
        so.FindProperty("portrait").objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AspectVisualProfile>(PortProfile);
        so.FindProperty("activeId").enumValueIndex = (int)AspectProfileId.Landscape;
        // Campaign keeps Landscape as the shipped default; Portrait via F8 / menu for compare.
        so.FindProperty("autoDetectFromScreen").boolValue = false;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void AssignIfNull(SerializedObject so, string field, Object value)
    {
        if (value == null) return;
        var sp = so.FindProperty(field);
        if (sp == null || sp.propertyType != SerializedPropertyType.ObjectReference) return;
        // Always rebind after Player replace — old refs are destroyed.
        sp.objectReferenceValue = value;
    }

    static void WriteArchiveNote()
    {
        File.WriteAllText(ArchiveNote,
            "ARCHIVED LEGACY HERO (Phase 4)\n" +
            "Do NOT use Hero_Sheet.png / Hero.controller / HeroExtra as runtime fallback.\n" +
            "Authoritative knight: Assets/Art/Rebuild/R2C/Aren (Aren.controller + Player_Aren.prefab).\n" +
            "Kept on disk for comparison only. Do not delete without approval.\n");
        AssetDatabase.ImportAsset(ArchiveNote);
    }
}
