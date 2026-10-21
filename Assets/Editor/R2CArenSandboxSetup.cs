using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// R2C: imports the approved Aren set (Docs/.../R2C_OriginalHero/unity_export) under Assets/Art/Rebuild/R2C/Aren,
/// builds Aren clips + Aren.controller, and wires Sandbox / campaign Players to that shared visual.
/// Does not resurrect Phase 4 Hero_Sheet as a runtime fallback.
/// </summary>
public static class R2CArenSandboxSetup
{
    const string Src = "Docs/ReferenceRebuild/R2/R2C_OriginalHero/unity_export";
    const string Dst = "Assets/Art/Rebuild/R2C/Aren";
    const string CtrlSrc = "Assets/Art/Rebuild/R2/Hero/Animators/Hero_R2.controller";
    const string CtrlDst = Dst + "/Animators/Aren.controller";
    const string EquipSet = "Assets/Art/Rebuild/R2C/ClassKits/EquipmentSet_Aren.asset";
    const string PrefabPath = Dst + "/Prefabs/Player_Aren.prefab";
    const string Sandbox = "Assets/Scenes/ReferenceCombatSandbox.unity";
    const string Campaign = "Assets/Scenes/GameplayPrototype_Phase4Test.unity";
    static readonly string[] Views = { "Down", "Up", "Side" };
    static readonly string[] Dir8 = { "R", "UR", "U", "UL", "L", "DL", "D", "DR" };

    [MenuItem("Tools/Gameplay Prototype/R2C/Setup Sandbox Aren (equipment)")]
    public static void Setup()
    {
        CopyFiles();
        ImportSheet();
        ImportExtras();
        ImportGear();
        BuildClipsAndController();
        AssignSandbox();
        AssetDatabase.SaveAssets();
        Debug.Log("[R2C] Aren imported. Sandbox Player uses " + CtrlDst + " + EquipmentVisual.");
    }

    static void CopyFiles()
    {
        foreach (var sub in new[] { "", "/Extra", "/Gear", "/Animations", "/Animators" }) Directory.CreateDirectory(Dst + sub);
        File.Copy(Src + "/Aren_Sheet.png", Dst + "/Aren_Sheet.png", true);
        File.Copy(Src + "/Aren_Anchors.csv", Dst + "/Aren_Anchors.csv", true);
        foreach (var f in Directory.GetFiles(Src + "/Extra", "*.png")) File.Copy(f, Dst + "/Extra/" + Path.GetFileName(f), true);
        foreach (var f in Directory.GetFiles(Src + "/Gear", "*.png")) File.Copy(f, Dst + "/Gear/" + Path.GetFileName(f), true);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
    }

    static TextureImporter PixelImporter(string path, SpriteImportMode mode)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = mode;
        imp.spritePixelsPerUnit = 16;
        imp.filterMode = FilterMode.Point;
        imp.mipmapEnabled = false;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.alphaIsTransparency = true;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.isReadable = false;
        return imp;
    }

    static void Slice(string path, List<SpriteRect> rects)
    {
        var imp = PixelImporter(path, SpriteImportMode.Multiple);
        imp.SaveAndReimport();
        var factory = new SpriteDataProviderFactories(); factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(imp);
        dp.InitSpriteEditorDataProvider();
        dp.SetSpriteRects(rects.ToArray());
        var names = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        var pairs = new List<SpriteNameFileIdPair>();
        foreach (var r in rects) pairs.Add(new SpriteNameFileIdPair(r.name, r.spriteID));
        names.SetNameFileIdPairs(pairs);
        dp.Apply();
        imp.SaveAndReimport();
    }

    static SpriteRect R(string name, int x, int yTop, int w, int h, int texH, Vector2 pivot)
    {
        return new SpriteRect { name = name, rect = new Rect(x, texH - yTop - h, w, h), alignment = SpriteAlignment.Custom, pivot = pivot, spriteID = GUID.Generate() };
    }

    // Same cell layout as Hero_Sheet R2b: rows Idle D/U/S, Walk D/U/S, Attack D/U/S (top to bottom).
    static void ImportSheet()
    {
        var rects = new List<SpriteRect>();
        string[] anims = { "Idle", "Walk", "Attack" };
        for (int a = 0; a < 3; a++)
            for (int v = 0; v < 3; v++)
                for (int f = 0; f < (a == 2 ? 3 : 4); f++)
                    rects.Add(R("Aren_" + anims[a] + "_" + Views[v] + "_" + f, f * 32, (a * 3 + v) * 32, 32, 32, 288, new Vector2(0.5f, 0.0625f)));
        Slice(Dst + "/Aren_Sheet.png", rects);
    }

    static void ImportExtras()
    {
        foreach (var f in Directory.GetFiles(Dst + "/Extra", "*.png"))
        {
            var path = f.Replace('\\', '/');
            var imp = PixelImporter(path, SpriteImportMode.Single);
            var s = new TextureImporterSettings(); imp.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.Custom; s.spritePivot = new Vector2(0.5f, 0.0625f);
            imp.SetTextureSettings(s);
            imp.SaveAndReimport();
        }
    }

    static void ImportGear()
    {
        var sword = new List<SpriteRect>();
        for (int d = 0; d < 8; d++) sword.Add(R("Aren_Sword_" + Dir8[d], d * 48, 0, 48, 48, 48, new Vector2(0.5f, 0.5f)));
        Slice(Dst + "/Gear/Aren_Sword.png", sword);
        var shield = new List<SpriteRect>();
        for (int v = 0; v < 3; v++) shield.Add(R("Aren_Shield_" + Views[v], v * 16, 0, 16, 16, 16, new Vector2(0.5f, 0.5f)));
        Slice(Dst + "/Gear/Aren_Shield.png", shield);
        var slash = new List<SpriteRect>();
        for (int v = 0; v < 3; v++) for (int fr = 0; fr < 2; fr++)
                slash.Add(R("Aren_Slash_" + Views[v] + "_" + fr, (v * 2 + fr) * 48, 0, 48, 48, 48, new Vector2(0.5f, 10f / 48f)));
        Slice(Dst + "/Gear/Aren_Slash.png", slash);
        // gauntlet: 4x4 png, the fist's top-left pixel (1,1) is the anchor -> pivot (0.25, 0.75)
        var hp = Dst + "/Gear/Aren_Hand.png";
        var himp = PixelImporter(hp, SpriteImportMode.Single);
        var hs = new TextureImporterSettings(); himp.ReadTextureSettings(hs);
        hs.spriteAlignment = (int)SpriteAlignment.Custom; hs.spritePivot = new Vector2(0.25f, 0.75f);
        himp.SetTextureSettings(hs);
        himp.SaveAndReimport();
    }

    static Dictionary<string, Sprite> Sprites(string path)
    {
        var d = new Dictionary<string, Sprite>();
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path)) if (o is Sprite s) d[s.name] = s;
        return d;
    }

    static AnimationClip Clip(string name, Sprite[] frames, float[] times, float stop, bool loop)
    {
        string path = Dst + "/Animations/" + name + ".anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null) AssetDatabase.DeleteAsset(path);
        var clip = new AnimationClip { name = name, frameRate = 60 };
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = times[i], value = frames[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var st = AnimationUtility.GetAnimationClipSettings(clip);
        st.loopTime = loop; st.stopTime = stop;
        AnimationUtility.SetAnimationClipSettings(clip, st);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    static void BuildClipsAndController()
    {
        var sheet = Sprites(Dst + "/Aren_Sheet.png");
        var clips = new Dictionary<string, AnimationClip>();
        foreach (var v in Views)
        {
            // Idle: 0.25 / 0.15 / 0.25 / 0.15 (approved). Walk: 4 x 0.125. Attack keys match PlayerCombat (impact 0.12, end 0.26).
            clips["Hero_Idle_" + v] = Clip("Aren_Idle_" + v, Frames(sheet, "Aren_Idle_" + v, 4), new[] { 0f, 0.25f, 0.40f, 0.65f }, 0.80f, true);
            clips["Hero_Walk_" + v] = Clip("Aren_Walk_" + v, Frames(sheet, "Aren_Walk_" + v, 4), new[] { 0f, 0.125f, 0.25f, 0.375f }, 0.5f, true);
            clips["Hero_Attack_" + v] = Clip("Aren_Attack_" + v, Frames(sheet, "Aren_Attack_" + v, 3), new[] { 0f, 0.12f, 0.19f }, 0.26f, false);
            var h0 = AssetDatabase.LoadAssetAtPath<Sprite>(Dst + "/Extra/Aren_Hurt_" + v + "_0.png");
            var h1 = AssetDatabase.LoadAssetAtPath<Sprite>(Dst + "/Extra/Aren_Hurt_" + v + "_1.png");
            clips["Hero_Hurt_" + v] = Clip("Aren_Hurt_" + v, new[] { h0, h1 }, new[] { 0f, 0.06f }, 0.18f, false);
        }
        var d = new Sprite[3];
        for (int i = 0; i < 3; i++) d[i] = AssetDatabase.LoadAssetAtPath<Sprite>(Dst + "/Extra/Aren_Death_Down_" + i + ".png");
        clips["Hero_Death"] = Clip("Aren_Death", new[] { d[0], d[1], d[2], d[2] }, new[] { 0f, 0.15f, 0.30f, 0.60f }, 0.68f, false);

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(CtrlDst) != null) AssetDatabase.DeleteAsset(CtrlDst);
        AssetDatabase.CopyAsset(CtrlSrc, CtrlDst);
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(CtrlDst);
        int n = 0;
        foreach (var layer in ctrl.layers)
            foreach (var s in layer.stateMachine.states)
                if (clips.TryGetValue(s.state.name, out var c)) { s.state.motion = c; n++; }
                else Debug.LogWarning("[R2C] state without Aren clip: " + s.state.name);
        EditorUtility.SetDirty(ctrl);
        Debug.Log("[R2C] Aren.controller states remapped: " + n);
    }

    const string KitDir = "Assets/Art/Rebuild/R2C/ClassKits";
    const string ArenKitPath = KitDir + "/ClassKit_Aren.asset";

    /// <summary>
    /// Builds ClassKit_Aren from the values actually serialized on the Sandbox Player's PlayerCombat (+ the code's
    /// hard-coded constants), so the kit reproduces the current behaviour exactly, then adds ClassCombat (F1..F5).
    /// </summary>
    [MenuItem("Tools/Gameplay Prototype/R2C/Setup Sandbox ClassKit (Aren)")]
    public static void SetupClassKit()
    {
        var scene = EditorSceneManager.OpenScene(Sandbox);
        var player = GameObject.Find("Player");
        var combat = player.GetComponent<GameplayPrototype.PlayerCombat>();
        var so = new SerializedObject(combat);
        float F(string n) => so.FindProperty(n).floatValue;
        int I(string n) => so.FindProperty(n).intValue;

        Directory.CreateDirectory(KitDir);
        var kit = AssetDatabase.LoadAssetAtPath<GameplayPrototype.ClassKit>(ArenKitPath);
        if (kit == null)
        {
            kit = ScriptableObject.CreateInstance<GameplayPrototype.ClassKit>();
            AssetDatabase.CreateAsset(kit, ArenKitPath);
        }
        kit.FillLegacyAren();
        var c = kit.combo;
        c.cooldown = F("slashCooldown"); c.radius = F("slashRadius"); c.damage = I("slashDamage");
        c.knockback = F("slashKnockback"); c.autoTargetRadius = F("slashAutoTargetRadius"); c.impactDelay = F("slashImpactDelay");
        kit.slot2.cooldown = F("flameCooldown"); kit.slot2.radius = F("flameRadius"); kit.slot2.range = F("flameRange"); kit.slot2.damage = I("flameDamage");
        kit.slot3.cooldown = F("dashCooldown"); kit.slot3.speed = F("dashSpeed"); kit.slot3.duration = F("dashDuration"); kit.slot3.radius = F("dashRadius"); kit.slot3.damage = I("dashDamage");
        kit.slot4.cooldown = F("whirlCooldown"); kit.slot4.radius = F("whirlRadius"); kit.slot4.damage = I("whirlDamage"); kit.slot4.count = I("whirlWaves"); kit.slot4.interval = F("whirlWaveInterval");
        kit.slotV.cooldown = F("guardCooldown"); kit.slotV.duration = F("guardDuration"); kit.slotV.multiplier = F("guardDamageMultiplier");
        kit.slotC.cooldown = F("crescentCooldown"); kit.slotC.radius = F("crescentRadius"); kit.slotC.damage = I("crescentDamage"); kit.slotC.knockback = F("crescentKnockback"); kit.slotC.anticipation = F("crescentAnticipation");
        EditorUtility.SetDirty(kit);
        AssetDatabase.SaveAssets();

        var cc = player.GetComponent<GameplayPrototype.ClassCombat>();
        if (cc == null) cc = player.AddComponent<GameplayPrototype.ClassCombat>();
        var cso = new SerializedObject(cc);
        var kits = cso.FindProperty("kits"); kits.arraySize = 5;
        kits.GetArrayElementAtIndex(0).objectReferenceValue = kit;
        for (int i = 1; i < 5; i++) kits.GetArrayElementAtIndex(i).objectReferenceValue = null;
        cso.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[R2C] ClassKit_Aren built from Sandbox PlayerCombat values; ClassCombat added (F1 Aren, F2-F5 empty).");
    }

    static Sprite[] Frames(Dictionary<string, Sprite> sheet, string prefix, int count)
    {
        var a = new Sprite[count];
        for (int i = 0; i < count; i++)
            if (!sheet.TryGetValue(prefix + "_" + i, out a[i])) throw new System.Exception("missing sprite " + prefix + "_" + i);
        return a;
    }

    [MenuItem("Tools/Gameplay Prototype/R2C/Apply Aren Shared Hero (Sandbox + L1-4 scene)")]
    public static void ApplySharedHero()
    {
        if (AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CtrlDst) == null)
            throw new System.Exception("Missing " + CtrlDst + ". Run Setup Sandbox Aren first.");
        AssignPlayerInScene(Sandbox, savePrefab: true);
        AssignPlayerInScene(Campaign, savePrefab: false);
        AssetDatabase.SaveAssets();
        Debug.Log("[R2C] Aren shared visual applied to Sandbox + Phase4Test. Prefab: " + PrefabPath);
    }

    static void AssignSandbox() => AssignPlayerInScene(Sandbox, savePrefab: false);

    static void AssignPlayerInScene(string scenePath, bool savePrefab)
    {
        var scene = EditorSceneManager.OpenScene(scenePath);
        var player = GameObject.Find("Player");
        if (player == null) throw new System.Exception(scenePath + " has no Player");
        WireArenVisual(player);
        if (savePrefab)
        {
            Directory.CreateDirectory(Dst + "/Prefabs");
            PrefabUtility.SaveAsPrefabAsset(player, PrefabPath);
        }
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void WireArenVisual(GameObject player)
    {
        var visual = player.GetComponent<GameplayPrototype.PlayerVisual>();
        var so = new SerializedObject(visual);
        so.FindProperty("bodyController").objectReferenceValue = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CtrlDst);
        so.ApplyModifiedPropertiesWithoutUndo();

        var eq = player.GetComponent<GameplayPrototype.EquipmentVisual>();
        if (eq == null) eq = player.AddComponent<GameplayPrototype.EquipmentVisual>();
        var eso = new SerializedObject(eq);
        var set = AssetDatabase.LoadAssetAtPath<GameplayPrototype.EquipmentSet>(EquipSet);
        if (set != null)
            eso.FindProperty("set").objectReferenceValue = set;
        eso.FindProperty("anchorTable").objectReferenceValue = AssetDatabase.LoadAssetAtPath<TextAsset>(Dst + "/Aren_Anchors.csv");
        var sword = Sprites(Dst + "/Gear/Aren_Sword.png");
        var shield = Sprites(Dst + "/Gear/Aren_Shield.png");
        var slash = Sprites(Dst + "/Gear/Aren_Slash.png");
        var wp = eso.FindProperty("weaponPoses"); wp.arraySize = 8;
        for (int d = 0; d < 8; d++) wp.GetArrayElementAtIndex(d).objectReferenceValue = sword["Aren_Sword_" + Dir8[d]];
        var sv = eso.FindProperty("shieldViews"); sv.arraySize = 3;
        for (int v = 0; v < 3; v++) sv.GetArrayElementAtIndex(v).objectReferenceValue = shield["Aren_Shield_" + Views[v]];
        var sf = eso.FindProperty("slashFrames"); sf.arraySize = 6;
        for (int v = 0; v < 3; v++) for (int fr = 0; fr < 2; fr++) sf.GetArrayElementAtIndex(v * 2 + fr).objectReferenceValue = slash["Aren_Slash_" + Views[v] + "_" + fr];
        eso.FindProperty("hand").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(Dst + "/Gear/Aren_Hand.png");
        eso.FindProperty("replaceLegacyRSlash").boolValue = true;
        eso.ApplyModifiedPropertiesWithoutUndo();
    }
}
