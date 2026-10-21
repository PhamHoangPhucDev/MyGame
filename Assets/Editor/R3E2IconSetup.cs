using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// R3E.2: import the approved Aren/Vex hotbar icons and the status icons the code can actually show.
/// Assigns them only on the Sandbox ClassCombat. Does not rewrite ClassKit damage.
/// </summary>
public static class R3E2IconSetup
{
    const string Src = "Docs/ReferenceRebuild/R2/R2C_OriginalHero/icons";
    const string Hotbar = "Assets/Art/Rebuild/R2C/UI/Icons";
    const string Status = "Assets/Art/Rebuild/R2C/UI/Icons/Status";
    const string AssetPath = "Assets/Art/Rebuild/R2C/UI/ClassUiIcons_Sandbox.asset";

    static readonly string[] HotbarFiles =
    {
        "Icon_Aren_R_Combo.png", "Icon_Aren_2_ShieldBash.png", "Icon_Aren_3_Charge.png",
        "Icon_Aren_4_WhirlSlash.png", "Icon_Aren_V_Bulwark.png", "Icon_Aren_C_Dawnbreaker.png",
        "Icon_Vex_R_Combo.png", "Icon_Vex_2_ShadowStep.png", "Icon_Vex_3_KnifeThrow.png",
        "Icon_Vex_4_BladeDance.png", "Icon_Vex_V_SmokeBomb.png", "Icon_Vex_C_Execute.png",
    };

    static readonly string[] StatusFiles =
    {
        "Status_Stun.png", "Status_Bleed.png", "Status_DamageReduction.png",
        "Status_Stealth.png", "Status_Empowered.png",
    };

    [MenuItem("Game01/R3E2/Import Sandbox Icons")]
    public static void Apply()
    {
        Directory.CreateDirectory(Hotbar);
        Directory.CreateDirectory(Status);
        foreach (var f in HotbarFiles)
            File.Copy(Path.Combine(Src, "export", f), Path.Combine(Hotbar, f), true);
        foreach (var f in StatusFiles)
            File.Copy(Path.Combine(Src, "status", f), Path.Combine(Status, f), true);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        foreach (var f in HotbarFiles) Point(Hotbar + "/" + f);
        foreach (var f in StatusFiles) Point(Status + "/" + f);

        var icons = AssetDatabase.LoadAssetAtPath<GameplayPrototype.ClassUiIcons>(AssetPath);
        if (icons == null)
        {
            icons = ScriptableObject.CreateInstance<GameplayPrototype.ClassUiIcons>();
            AssetDatabase.CreateAsset(icons, AssetPath);
        }
        icons.arenR = Sp(Hotbar, "Icon_Aren_R_Combo");
        icons.aren2 = Sp(Hotbar, "Icon_Aren_2_ShieldBash");
        icons.aren3 = Sp(Hotbar, "Icon_Aren_3_Charge");
        icons.aren4 = Sp(Hotbar, "Icon_Aren_4_WhirlSlash");
        icons.arenV = Sp(Hotbar, "Icon_Aren_V_Bulwark");
        icons.arenC = Sp(Hotbar, "Icon_Aren_C_Dawnbreaker");
        icons.vexR = Sp(Hotbar, "Icon_Vex_R_Combo");
        icons.vex2 = Sp(Hotbar, "Icon_Vex_2_ShadowStep");
        icons.vex3 = Sp(Hotbar, "Icon_Vex_3_KnifeThrow");
        icons.vex4 = Sp(Hotbar, "Icon_Vex_4_BladeDance");
        icons.vexV = Sp(Hotbar, "Icon_Vex_V_SmokeBomb");
        icons.vexC = Sp(Hotbar, "Icon_Vex_C_Execute");
        icons.stun = Sp(Status, "Status_Stun");
        icons.bleed = Sp(Status, "Status_Bleed");
        icons.damageReduction = Sp(Status, "Status_DamageReduction");
        icons.stealth = Sp(Status, "Status_Stealth");
        icons.empowered = Sp(Status, "Status_Empowered");
        EditorUtility.SetDirty(icons);
        AssetDatabase.SaveAssetIfDirty(icons);
        Debug.Log("[R3E2] icons arenR=" + (icons.arenR != null) + " vexR=" + (icons.vexR != null) + " stun=" + (icons.stun != null) + ". Scene was not saved.");
    }

    static void Point(string path)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.mipmapEnabled = false;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.alphaIsTransparency = true;
        imp.npotScale = TextureImporterNPOTScale.None;
        var s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteMode = (int)SpriteImportMode.Single;
        s.spritePixelsPerUnit = 16f;
        s.spriteAlignment = (int)SpriteAlignment.Center;
        s.filterMode = FilterMode.Point;
        s.mipmapEnabled = false;
        s.alphaIsTransparency = true;
        imp.SetTextureSettings(s);
        var def = imp.GetDefaultPlatformTextureSettings();
        def.textureCompression = TextureImporterCompression.Uncompressed;
        def.crunchedCompression = false;
        imp.SetPlatformTextureSettings(def);
        var standalone = imp.GetPlatformTextureSettings("Standalone");
        standalone.overridden = true;
        standalone.textureCompression = TextureImporterCompression.Uncompressed;
        standalone.crunchedCompression = false;
        imp.SetPlatformTextureSettings(standalone);
        imp.SaveAndReimport();
    }

    static Sprite Sp(string folder, string name)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(folder + "/" + name + ".png");
        if (s == null) throw new System.Exception("missing icon " + name);
        return s;
    }
}
