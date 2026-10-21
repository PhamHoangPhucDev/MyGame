using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>
/// R2C phase 2: copy the approved hotbar and status icons. Does not open or save a scene.
/// </summary>
public static class R2CPhase2IconSetup
{
    const string Src = "Docs/ReferenceRebuild/R2/R2C_OriginalHero/icons";
    const string Hotbar = "Assets/Art/Rebuild/R2C/UI/Icons";
    const string Status = "Assets/Art/Rebuild/R2C/UI/Icons/Status";
    const string AssetPath = "Assets/Art/Rebuild/R2C/UI/ClassUiIcons_Sandbox.asset";

    static readonly string[] Classes = { "Aren", "Brom", "Lyra", "Selene", "Vex" };
    static readonly string[] Keys =
    {
        "R_Combo", "2_ShieldBash", "3_Charge", "4_WhirlSlash", "V_Bulwark", "C_Dawnbreaker",
        "R_Combo", "2_GroundSlam", "3_ShieldCharge", "4_TauntRoar", "V_IronWall", "C_Earthquake",
        "R_Combo", "2_FanShot", "3_LeapBack", "4_ArrowRain", "V_WindStep", "C_ForestPierce",
        "R_Combo", "2_StarOrb", "3_Blink", "4_RuneCircle", "V_ManaShield", "C_MeteorShower",
        "R_Combo", "2_ShadowStep", "3_KnifeThrow", "4_BladeDance", "V_SmokeBomb", "C_Execute",
    };

    static readonly string[] StatusFiles =
    {
        "Status_Stun", "Status_Bleed", "Status_Slow", "Status_Taunt", "Status_Absorb",
        "Status_DamageReduction", "Status_Haste", "Status_Stealth", "Status_DodgeReady",
        "Status_Empowered", "Status_Reflect",
    };

    [MenuItem("Game01/R2C/Phase 2 Import Icons")]
    public static void Import()
    {
        Directory.CreateDirectory(Hotbar);
        Directory.CreateDirectory(Status);
        for (int i = 0; i < Keys.Length; i++)
        {
            string file = "Icon_" + Classes[i / 6] + "_" + Keys[i] + ".png";
            CopyPng(Path.Combine(Src, "export", file), Path.Combine(Hotbar, file), 32, 32);
        }
        foreach (var name in StatusFiles)
            CopyPng(Path.Combine(Src, "status", name + ".png"), Path.Combine(Status, name + ".png"), 24, 24);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        for (int i = 0; i < Keys.Length; i++)
            Point(Hotbar + "/Icon_" + Classes[i / 6] + "_" + Keys[i] + ".png");
        foreach (var name in StatusFiles)
            Point(Status + "/" + name + ".png");

        var icons = AssetDatabase.LoadAssetAtPath<GameplayPrototype.ClassUiIcons>(AssetPath);
        if (icons == null)
        {
            icons = ScriptableObject.CreateInstance<GameplayPrototype.ClassUiIcons>();
            AssetDatabase.CreateAsset(icons, AssetPath);
        }
        Assign(icons);
        EditorUtility.SetDirty(icons);
        AssetDatabase.SaveAssetIfDirty(icons);
        Debug.Log("[R2C phase 2] icons imported. arenR=" + (icons.arenR != null) + " bromR=" + (icons.bromR != null)
            + " lyraR=" + (icons.lyraR != null) + " seleneR=" + (icons.seleneR != null) + " vexR=" + (icons.vexR != null)
            + " stun=" + (icons.stun != null) + " reflect=" + (icons.reflect != null));
    }

    static void Assign(GameplayPrototype.ClassUiIcons icons)
    {
        icons.arenR = Sp("Icon_Aren_R_Combo");
        icons.aren2 = Sp("Icon_Aren_2_ShieldBash");
        icons.aren3 = Sp("Icon_Aren_3_Charge");
        icons.aren4 = Sp("Icon_Aren_4_WhirlSlash");
        icons.arenV = Sp("Icon_Aren_V_Bulwark");
        icons.arenC = Sp("Icon_Aren_C_Dawnbreaker");
        icons.bromR = Sp("Icon_Brom_R_Combo");
        icons.brom2 = Sp("Icon_Brom_2_GroundSlam");
        icons.brom3 = Sp("Icon_Brom_3_ShieldCharge");
        icons.brom4 = Sp("Icon_Brom_4_TauntRoar");
        icons.bromV = Sp("Icon_Brom_V_IronWall");
        icons.bromC = Sp("Icon_Brom_C_Earthquake");
        icons.lyraR = Sp("Icon_Lyra_R_Combo");
        icons.lyra2 = Sp("Icon_Lyra_2_FanShot");
        icons.lyra3 = Sp("Icon_Lyra_3_LeapBack");
        icons.lyra4 = Sp("Icon_Lyra_4_ArrowRain");
        icons.lyraV = Sp("Icon_Lyra_V_WindStep");
        icons.lyraC = Sp("Icon_Lyra_C_ForestPierce");
        icons.seleneR = Sp("Icon_Selene_R_Combo");
        icons.selene2 = Sp("Icon_Selene_2_StarOrb");
        icons.selene3 = Sp("Icon_Selene_3_Blink");
        icons.selene4 = Sp("Icon_Selene_4_RuneCircle");
        icons.seleneV = Sp("Icon_Selene_V_ManaShield");
        icons.seleneC = Sp("Icon_Selene_C_MeteorShower");
        icons.vexR = Sp("Icon_Vex_R_Combo");
        icons.vex2 = Sp("Icon_Vex_2_ShadowStep");
        icons.vex3 = Sp("Icon_Vex_3_KnifeThrow");
        icons.vex4 = Sp("Icon_Vex_4_BladeDance");
        icons.vexV = Sp("Icon_Vex_V_SmokeBomb");
        icons.vexC = Sp("Icon_Vex_C_Execute");
        icons.stun = St("Status_Stun");
        icons.bleed = St("Status_Bleed");
        icons.slow = St("Status_Slow");
        icons.taunt = St("Status_Taunt");
        icons.absorb = St("Status_Absorb");
        icons.damageReduction = St("Status_DamageReduction");
        icons.haste = St("Status_Haste");
        icons.stealth = St("Status_Stealth");
        icons.dodgeReady = St("Status_DodgeReady");
        icons.empowered = St("Status_Empowered");
        icons.reflect = St("Status_Reflect");
    }

    static void CopyPng(string src, string dst, int w, int h)
    {
        var b = File.ReadAllBytes(src);
        int aw = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
        int ah = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
        if (aw != w || ah != h)
            throw new System.Exception("[R2C phase 2] " + src + " is " + aw + "x" + ah + ", expected " + w + "x" + h);
        File.Copy(src, dst, true);
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
        s.spriteMeshType = SpriteMeshType.FullRect;
        s.filterMode = FilterMode.Point;
        s.mipmapEnabled = false;
        s.alphaIsTransparency = true;
        imp.SetTextureSettings(s);
        imp.SaveAndReimport();
    }

    static Sprite Sp(string name)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(Hotbar + "/" + name + ".png");
        if (s == null) throw new System.Exception("[R2C phase 2] missing " + name);
        return s;
    }

    static Sprite St(string name)
    {
        var s = AssetDatabase.LoadAssetAtPath<Sprite>(Status + "/" + name + ".png");
        if (s == null) throw new System.Exception("[R2C phase 2] missing " + name);
        return s;
    }
}
