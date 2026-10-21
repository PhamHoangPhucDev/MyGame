using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// R3E.1: append Aren_Sword cell 8 and Vex_Dagger cells 8–9 without regenerating the approved 8-dir sprite IDs.
/// Sandbox ClassKit_Aren uses EquipmentSet_Aren_BackGear. Player_Aren and EquipmentSet_Aren stay on the hand-rest table
/// so Level 1–4 / Phase4Test keep the previous rest pose.
/// </summary>
public static class R3E1BackGearSetup
{
    const string Sword = "Assets/Art/Rebuild/R2C/Aren/Gear/Aren_Sword.png";
    const string Dagger = "Assets/Art/Rebuild/R2C/Vex/Gear/Vex_Dagger.png";
    const string ArenBackCsv = "Assets/Art/Rebuild/R2C/Aren/Aren_Anchors_BackGear.csv";
    const string ArenSet = "Assets/Art/Rebuild/R2C/ClassKits/EquipmentSet_Aren.asset";
    const string BackSet = "Assets/Art/Rebuild/R2C/ClassKits/EquipmentSet_Aren_BackGear.asset";
    const string VexSet = "Assets/Art/Rebuild/R2C/ClassKits/EquipmentSet_Vex.asset";
    const string ArenKit = "Assets/Art/Rebuild/R2C/ClassKits/ClassKit_Aren.asset";

    [MenuItem("Game01/R3E1/Apply Back Gear Slices")]
    public static void Apply()
    {
        AssetDatabase.ImportAsset(Sword, ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(Dagger, ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset(ArenBackCsv, ImportAssetOptions.ForceSynchronousImport);
        AssetDatabase.ImportAsset("Assets/Art/Rebuild/R2C/Vex/Vex_Anchors.csv", ImportAssetOptions.ForceSynchronousImport);

        EnsureCell(Sword, "Aren_Sword_BackHilt", 384);
        EnsureCell(Dagger, "Vex_Dagger_PeekL", 384);
        EnsureCell(Dagger, "Vex_Dagger_PeekR", 432);

        var sword = Sprites(Sword);
        var dag = Sprites(Dagger);
        string[] dir8 = { "R", "UR", "U", "UL", "L", "DL", "D", "DR" };

        if (AssetDatabase.LoadAssetAtPath<GameplayPrototype.EquipmentSet>(BackSet) == null)
            AssetDatabase.CopyAsset(ArenSet, BackSet);
        var back = AssetDatabase.LoadAssetAtPath<GameplayPrototype.EquipmentSet>(BackSet);
        back.anchorTable = AssetDatabase.LoadAssetAtPath<TextAsset>(ArenBackCsv);
        var arenPoses = new List<string>();
        foreach (var d in dir8) arenPoses.Add("Aren_Sword_" + d);
        arenPoses.Add("Aren_Sword_BackHilt");
        back.weaponPoses = Named(sword, arenPoses);
        EditorUtility.SetDirty(back);

        var vex = AssetDatabase.LoadAssetAtPath<GameplayPrototype.EquipmentSet>(VexSet);
        vex.anchorTable = AssetDatabase.LoadAssetAtPath<TextAsset>("Assets/Art/Rebuild/R2C/Vex/Vex_Anchors.csv");
        var vexPoses = new List<string>();
        foreach (var d in dir8) vexPoses.Add("Vex_Dagger_" + d);
        vexPoses.Add("Vex_Dagger_PeekL");
        vexPoses.Add("Vex_Dagger_PeekR");
        vex.weaponPoses = Named(dag, vexPoses);
        vex.offPoses = vex.weaponPoses;
        EditorUtility.SetDirty(vex);

        var kit = AssetDatabase.LoadAssetAtPath<GameplayPrototype.ClassKit>(ArenKit);
        kit.equipment = back;
        EditorUtility.SetDirty(kit);
        AssetDatabase.SaveAssets();

        var vexKit = AssetDatabase.LoadAssetAtPath<GameplayPrototype.ClassKit>(AssetDatabase.GUIDToAssetPath("e0996327aa1a91f41abb03fdf3e15c8c"));
        if (vexKit != null)
        {
            var scene = EditorSceneManager.OpenScene("Assets/Scenes/ReferenceCombatSandbox.unity");
            var player = GameObject.Find("Player");
            var cc = player.GetComponent<GameplayPrototype.ClassCombat>();
            var cso = new SerializedObject(cc);
            var kits = cso.FindProperty("kits");
            if (kits.arraySize < 5) kits.arraySize = 5;
            kits.GetArrayElementAtIndex(4).objectReferenceValue = vexKit;
            cso.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }
        Debug.Log("[R3E1] sword=" + sword.Count + " dagger=" + dag.Count + " arenBackPoses=" + back.weaponPoses.Length + " vexPoses=" + vex.weaponPoses.Length + " vexSlot=" + (vexKit != null));
    }

    static void EnsureCell(string path, string name, int x)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(imp);
        dp.InitSpriteEditorDataProvider();
        var rects = new List<SpriteRect>(dp.GetSpriteRects());
        if (rects.Exists(r => r.name == name)) return;
        rects.Add(new SpriteRect
        {
            name = name,
            rect = new Rect(x, 0, 48, 48),
            alignment = SpriteAlignment.Custom,
            pivot = new Vector2(0.5f, 0.5f),
            spriteID = GUID.Generate(),
        });
        dp.SetSpriteRects(rects.ToArray());
        var ids = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        var pairs = new List<SpriteNameFileIdPair>();
        foreach (var r in rects) pairs.Add(new SpriteNameFileIdPair(r.name, r.spriteID));
        ids.SetNameFileIdPairs(pairs);
        dp.Apply();
        imp.SaveAndReimport();
    }

    static Dictionary<string, Sprite> Sprites(string path)
    {
        var d = new Dictionary<string, Sprite>();
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
            if (o is Sprite s) d[s.name] = s;
        return d;
    }

    static Sprite[] Named(Dictionary<string, Sprite> d, List<string> names)
    {
        var a = new Sprite[names.Count];
        for (int i = 0; i < names.Count; i++)
        {
            if (!d.TryGetValue(names[i], out a[i]) || a[i] == null)
                throw new System.Exception("missing sprite " + names[i]);
        }
        return a;
    }
}
