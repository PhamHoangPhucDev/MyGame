using UnityEditor;
using UnityEngine;

namespace GameplayPrototype.EditorTools
{
    /// <summary>
    /// Writes ClassKit_Aren_Designed from FillDesignedAren. Never touches ClassKit_Aren.
    /// </summary>
    public static class R3E5ADesignedKitSetup
    {
        const string Dest = "Assets/Art/Rebuild/R2C/ClassKits/ClassKit_Aren_Designed.asset";
        const string Controller = "Assets/Art/Rebuild/R2C/Aren/Animators/Aren.controller";
        const string Gear = "Assets/Art/Rebuild/R2C/ClassKits/EquipmentSet_Aren_BackGear.asset";

        [MenuItem("Game01/R3E5A/Set Campaign Build Entry")]
        public static void SetBuildEntry()
        {
            var scenes = new[]
            {
                new EditorBuildSettingsScene("Assets/Scenes/GameplayPrototype_Phase4Test.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/ReferenceCombatSandbox.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/SampleScene.unity", false),
                new EditorBuildSettingsScene("Assets/Scenes/GameplayPrototype.unity", false),
            };
            EditorBuildSettings.scenes = scenes;
            Debug.Log("[R3E5A] build0=" + scenes[0].path + " enabled=" + scenes[0].enabled
                + " count=" + scenes.Length);
        }

        [MenuItem("Game01/R3E5A/Create Designed Aren Kit")]
        public static void Create()
        {
            var kit = AssetDatabase.LoadAssetAtPath<ClassKit>(Dest);
            bool created = kit == null;
            if (created)
            {
                kit = ScriptableObject.CreateInstance<ClassKit>();
                AssetDatabase.CreateAsset(kit, Dest);
            }

            kit.FillDesignedAren();
            kit.bodyController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Controller);
            kit.equipment = AssetDatabase.LoadAssetAtPath<EquipmentSet>(Gear);
            EditorUtility.SetDirty(kit);
            AssetDatabase.SaveAssetIfDirty(kit);
            AssetDatabase.ImportAsset(Dest);

            Debug.Log("[R3E5A] created=" + created
                + " slot2=" + kit.slot2.displayName + "/" + kit.slot2.behaviour + " dmg=" + kit.slot2.damage + " cd=" + kit.slot2.cooldown
                + " slot3=" + kit.slot3.displayName + " dmg=" + kit.slot3.damage + " spd=" + kit.slot3.speed + " dur=" + kit.slot3.duration
                + " slot4=" + kit.slot4.displayName + " x" + kit.slot4.count
                + " V=" + kit.slotV.displayName + " mul=" + kit.slotV.multiplier
                + " C=" + kit.slotC.displayName + " dmg=" + kit.slotC.damage
                + " ctrl=" + (kit.bodyController != null) + " gear=" + (kit.equipment != null));
        }
    }
}
