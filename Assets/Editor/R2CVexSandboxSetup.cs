using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// R2C: imports Vex (Docs/.../R2C_OriginalHero/unity_export_vex) under Assets/Art/Rebuild/R2C/Vex, builds clips +
/// Vex.controller, EquipmentSet_Vex / EquipmentSet_Aren, ClassKit_Vex, and puts Vex on F5 of the Sandbox ClassCombat.
/// Level 1-4 untouched.
/// </summary>
public static class R2CVexSandboxSetup
{
    const string Src = "Docs/ReferenceRebuild/R2/R2C_OriginalHero/unity_export_vex";
    const string Dst = "Assets/Art/Rebuild/R2C/Vex";
    const string AreDst = "Assets/Art/Rebuild/R2C/Aren";
    const string KitDir = "Assets/Art/Rebuild/R2C/ClassKits";
    const string CtrlSrc = "Assets/Art/Rebuild/R2/Hero/Animators/Hero_R2.controller";
    const string Sandbox = "Assets/Scenes/ReferenceCombatSandbox.unity";
    static readonly string[] Views = { "Down", "Up", "Side" };
    static readonly string[] Dir8 = { "R", "UR", "U", "UL", "L", "DL", "D", "DR" };

    [MenuItem("Tools/Gameplay Prototype/R2C/Setup Sandbox Vex (F5)")]
    public static void Setup()
    {
        foreach (var sub in new[] { "", "/Extra", "/Gear", "/Animations", "/Animators" }) Directory.CreateDirectory(Dst + sub);
        File.Copy(Src + "/Vex_Sheet.png", Dst + "/Vex_Sheet.png", true);
        File.Copy(Src + "/Vex_Anchors.csv", Dst + "/Vex_Anchors.csv", true);
        foreach (var f in Directory.GetFiles(Src + "/Extra", "*.png")) File.Copy(f, Dst + "/Extra/" + Path.GetFileName(f), true);
        foreach (var f in Directory.GetFiles(Src + "/Gear", "*.png")) File.Copy(f, Dst + "/Gear/" + Path.GetFileName(f), true);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        // body sheet (same layout as Aren / R2b)
        var rects = new List<SpriteRect>();
        string[] anims = { "Idle", "Walk", "Attack" };
        for (int a = 0; a < 3; a++) for (int v = 0; v < 3; v++) for (int f = 0; f < (a == 2 ? 3 : 4); f++)
                    rects.Add(R("Vex_" + anims[a] + "_" + Views[v] + "_" + f, f * 32, (a * 3 + v) * 32, 32, 32, 288, new Vector2(0.5f, 0.0625f)));
        Slice(Dst + "/Vex_Sheet.png", rects);
        foreach (var f in Directory.GetFiles(Dst + "/Extra", "*.png")) Single(f.Replace('\\', '/'), new Vector2(0.5f, 0.0625f));
        // gear
        var dag = new List<SpriteRect>();
        for (int d = 0; d < 8; d++) dag.Add(R("Vex_Dagger_" + Dir8[d], d * 48, 0, 48, 48, 48, new Vector2(0.5f, 0.5f)));
        Slice(Dst + "/Gear/Vex_Dagger.png", dag);
        var sl = new List<SpriteRect>();
        for (int s = 0; s < 4; s++) for (int v = 0; v < 3; v++) for (int fr = 0; fr < 2; fr++)
                    sl.Add(R("Vex_Slash_" + s + "_" + Views[v] + "_" + fr, (s * 6 + v * 2 + fr) * 48, 0, 48, 48, 48, new Vector2(0.5f, 10f / 48f)));
        Slice(Dst + "/Gear/Vex_Slash.png", sl);
        var sm = new List<SpriteRect>();
        for (int i = 0; i < 4; i++) sm.Add(R("Vex_Smoke_" + i, i * 32, 0, 32, 32, 32, new Vector2(0.5f, 0.5f)));
        Slice(Dst + "/Gear/Vex_Smoke.png", sm);
        var kn = new List<SpriteRect>();
        for (int d = 0; d < 8; d++) kn.Add(R("Vex_Knife_" + Dir8[d], d * 32, 0, 32, 32, 32, new Vector2(0.5f, 0.5f)));
        Slice(Dst + "/Gear/Vex_Knife.png", kn);
        var ex = new List<SpriteRect>();
        for (int i = 0; i < 2; i++) ex.Add(R("Vex_ExecuteX_" + i, i * 48, 0, 48, 48, 48, new Vector2(0.5f, 0.5f)));
        Slice(Dst + "/Gear/Vex_ExecuteX.png", ex);

        var ctrl = BuildController();
        var vexSet = BuildVexSet();
        var arenSet = BuildArenSet();
        BuildKitsAndAssign(ctrl, vexSet, arenSet);
        AssetDatabase.SaveAssets();
        Debug.Log("[R2C] Vex imported; F5 = Vex in Sandbox ClassCombat.");
    }

    // ---------- import helpers ----------
    static TextureImporter PixelImporter(string path, SpriteImportMode mode)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite; imp.spriteImportMode = mode; imp.spritePixelsPerUnit = 16;
        imp.filterMode = FilterMode.Point; imp.mipmapEnabled = false; imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.alphaIsTransparency = true; imp.wrapMode = TextureWrapMode.Clamp;
        return imp;
    }

    static void Single(string path, Vector2 pivot)
    {
        var imp = PixelImporter(path, SpriteImportMode.Single);
        var s = new TextureImporterSettings(); imp.ReadTextureSettings(s);
        s.spriteAlignment = (int)SpriteAlignment.Custom; s.spritePivot = pivot;
        imp.SetTextureSettings(s); imp.SaveAndReimport();
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
        dp.Apply(); imp.SaveAndReimport();
    }

    static SpriteRect R(string name, int x, int yTop, int w, int h, int texH, Vector2 pivot) =>
        new SpriteRect { name = name, rect = new Rect(x, texH - yTop - h, w, h), alignment = SpriteAlignment.Custom, pivot = pivot, spriteID = GUID.Generate() };

    static Dictionary<string, Sprite> Sprites(string path)
    {
        var d = new Dictionary<string, Sprite>();
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path)) if (o is Sprite s) d[s.name] = s;
        return d;
    }

    static Sprite[] Ordered(Dictionary<string, Sprite> d, IEnumerable<string> names)
    {
        var l = new List<Sprite>();
        foreach (var n in names) { if (!d.TryGetValue(n, out var s)) throw new System.Exception("missing sprite " + n); l.Add(s); }
        return l.ToArray();
    }

    static AnimationClip Clip(string name, Sprite[] frames, float[] times, float stop, bool loop)
    {
        string path = Dst + "/Animations/" + name + ".anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null) AssetDatabase.DeleteAsset(path);
        var clip = new AnimationClip { name = name, frameRate = 60 };
        var keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++) keys[i] = new ObjectReferenceKeyframe { time = times[i], value = frames[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
        var st = AnimationUtility.GetAnimationClipSettings(clip); st.loopTime = loop; st.stopTime = stop;
        AnimationUtility.SetAnimationClipSettings(clip, st);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    // ---------- build ----------
    static AnimatorController BuildController()
    {
        var sheet = Sprites(Dst + "/Vex_Sheet.png");
        var clips = new Dictionary<string, AnimationClip>();
        foreach (var v in Views)
        {
            clips["Hero_Idle_" + v] = Clip("Vex_Idle_" + v, Ordered(sheet, new[] { 0, 1, 2, 3 }.Select4("Vex_Idle_" + v)), new[] { 0f, 0.25f, 0.40f, 0.65f }, 0.80f, true);
            clips["Hero_Walk_" + v] = Clip("Vex_Walk_" + v, Ordered(sheet, new[] { 0, 1, 2, 3 }.Select4("Vex_Walk_" + v)), new[] { 0f, 0.125f, 0.25f, 0.375f }, 0.5f, true);
            clips["Hero_Attack_" + v] = Clip("Vex_Attack_" + v, Ordered(sheet, new[] { 0, 1, 2 }.Select4("Vex_Attack_" + v)), new[] { 0f, 0.12f, 0.19f }, 0.26f, false);
            var h0 = AssetDatabase.LoadAssetAtPath<Sprite>(Dst + "/Extra/Vex_Hurt_" + v + "_0.png");
            var h1 = AssetDatabase.LoadAssetAtPath<Sprite>(Dst + "/Extra/Vex_Hurt_" + v + "_1.png");
            clips["Hero_Hurt_" + v] = Clip("Vex_Hurt_" + v, new[] { h0, h1 }, new[] { 0f, 0.06f }, 0.18f, false);
        }
        var d = new Sprite[3];
        for (int i = 0; i < 3; i++) d[i] = AssetDatabase.LoadAssetAtPath<Sprite>(Dst + "/Extra/Vex_Death_Down_" + i + ".png");
        clips["Hero_Death"] = Clip("Vex_Death", new[] { d[0], d[1], d[2], d[2] }, new[] { 0f, 0.15f, 0.30f, 0.60f }, 0.68f, false);

        string ctrlPath = Dst + "/Animators/Vex.controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null) AssetDatabase.DeleteAsset(ctrlPath);
        AssetDatabase.CopyAsset(CtrlSrc, ctrlPath);
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        int n = 0;
        foreach (var layer in ctrl.layers)
            foreach (var s in layer.stateMachine.states)
                if (clips.TryGetValue(s.state.name, out var c)) { s.state.motion = c; n++; }
        EditorUtility.SetDirty(ctrl);
        Debug.Log("[R2C] Vex.controller states remapped: " + n);
        return ctrl;
    }

    static GameplayPrototype.EquipmentSet LoadOrCreateSet(string path)
    {
        var s = AssetDatabase.LoadAssetAtPath<GameplayPrototype.EquipmentSet>(path);
        if (s == null) { s = ScriptableObject.CreateInstance<GameplayPrototype.EquipmentSet>(); AssetDatabase.CreateAsset(s, path); }
        return s;
    }

    static GameplayPrototype.EquipmentSet BuildVexSet()
    {
        Directory.CreateDirectory(KitDir);
        var set = LoadOrCreateSet(KitDir + "/EquipmentSet_Vex.asset");
        var dag = Sprites(Dst + "/Gear/Vex_Dagger.png");
        var sl = Sprites(Dst + "/Gear/Vex_Slash.png");
        set.anchorTable = AssetDatabase.LoadAssetAtPath<TextAsset>(Dst + "/Vex_Anchors.csv");
        set.weaponPoses = Ordered(dag, System.Array.ConvertAll(Dir8, x => "Vex_Dagger_" + x));
        set.offPoses = set.weaponPoses;
        set.shieldViews = new Sprite[0];
        set.hand = null;
        var names = new List<string>();
        for (int s = 0; s < 4; s++) foreach (var v in Views) for (int fr = 0; fr < 2; fr++) names.Add("Vex_Slash_" + s + "_" + v + "_" + fr);
        set.slashFrames = Ordered(sl, names);
        set.replaceLegacyRSlash = true;
        EditorUtility.SetDirty(set);
        return set;
    }

    static GameplayPrototype.EquipmentSet BuildArenSet()
    {
        var set = LoadOrCreateSet(KitDir + "/EquipmentSet_Aren.asset");
        var sword = Sprites(AreDst + "/Gear/Aren_Sword.png");
        var shield = Sprites(AreDst + "/Gear/Aren_Shield.png");
        var slash = Sprites(AreDst + "/Gear/Aren_Slash.png");
        set.anchorTable = AssetDatabase.LoadAssetAtPath<TextAsset>(AreDst + "/Aren_Anchors.csv");
        set.weaponPoses = Ordered(sword, System.Array.ConvertAll(Dir8, x => "Aren_Sword_" + x));
        set.offPoses = new Sprite[0];
        set.shieldViews = Ordered(shield, System.Array.ConvertAll(Views, x => "Aren_Shield_" + x));
        set.hand = AssetDatabase.LoadAssetAtPath<Sprite>(AreDst + "/Gear/Aren_Hand.png");
        var names = new List<string>();
        foreach (var v in Views) for (int fr = 0; fr < 2; fr++) names.Add("Aren_Slash_" + v + "_" + fr);
        set.slashFrames = Ordered(slash, names);
        set.replaceLegacyRSlash = true;
        EditorUtility.SetDirty(set);
        return set;
    }

    static void BuildKitsAndAssign(AnimatorController vexCtrl, GameplayPrototype.EquipmentSet vexSet, GameplayPrototype.EquipmentSet arenSet)
    {
        var smoke = Ordered(Sprites(Dst + "/Gear/Vex_Smoke.png"), new[] { "Vex_Smoke_0", "Vex_Smoke_1", "Vex_Smoke_2", "Vex_Smoke_3" });
        var knives = Ordered(Sprites(Dst + "/Gear/Vex_Knife.png"), System.Array.ConvertAll(Dir8, x => "Vex_Knife_" + x));
        var exX = Ordered(Sprites(Dst + "/Gear/Vex_ExecuteX.png"), new[] { "Vex_ExecuteX_0", "Vex_ExecuteX_1" });

        string vexPath = KitDir + "/ClassKit_Vex.asset";
        var vex = AssetDatabase.LoadAssetAtPath<GameplayPrototype.ClassKit>(vexPath);
        if (vex == null) { vex = ScriptableObject.CreateInstance<GameplayPrototype.ClassKit>(); AssetDatabase.CreateAsset(vex, vexPath); }
        vex.FillVex(smoke, knives, exX);
        vex.bodyController = vexCtrl;
        vex.equipment = vexSet;
        EditorUtility.SetDirty(vex);

        var aren = AssetDatabase.LoadAssetAtPath<GameplayPrototype.ClassKit>(KitDir + "/ClassKit_Aren.asset");
        if (aren != null)
        {
            // visuals only: Aren's combat numbers stay exactly as built from the scene values
            aren.bodyController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(AreDst + "/Animators/Aren.controller");
            aren.equipment = arenSet;
            EditorUtility.SetDirty(aren);
        }
        AssetDatabase.SaveAssets();

        var scene = EditorSceneManager.OpenScene(Sandbox);
        var player = GameObject.Find("Player");
        var cc = player.GetComponent<GameplayPrototype.ClassCombat>();
        var cso = new SerializedObject(cc);
        var kits = cso.FindProperty("kits"); if (kits.arraySize < 5) kits.arraySize = 5;
        kits.GetArrayElementAtIndex(4).objectReferenceValue = vex;
        cso.ApplyModifiedPropertiesWithoutUndo();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }
}

static class R2CVexSetupExt
{
    public static string[] Select4(this int[] idx, string prefix) => System.Array.ConvertAll(idx, i => prefix + "_" + i);
}
