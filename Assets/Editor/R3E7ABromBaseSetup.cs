using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// R3E.7A: import the approved Brom body, hurt, death-down and gear. No skill kit, no F2 assignment.
/// Death Up/Side stay in Docs (CLASS_AUDIT_FIX_NOTE: awaiting approval). Skill sheets stay in Docs.
/// </summary>
public static class R3E7ABromBaseSetup
{
    const string Src = "Docs/ReferenceRebuild/R2/R2C_OriginalHero/unity_export_brom";
    const string Dst = "Assets/Art/Rebuild/R2C/Brom";
    const string KitDir = "Assets/Art/Rebuild/R2C/ClassKits";
    const string CtrlSrc = "Assets/Art/Rebuild/R2/Hero/Animators/Hero_R2.controller";
    static readonly string[] Views = { "Down", "Up", "Side" };
    static readonly string[] Dir8 = { "R", "UR", "U", "UL", "L", "DL", "D", "DR" };

    [MenuItem("Game01/R3E7A/Import Brom Base")]
    public static void Import()
    {
        RequirePng(Src + "/Brom_Sheet.png", 128, 288);
        RequirePng(Src + "/Gear/Brom_Hammer.png", 384, 48);
        RequirePng(Src + "/Gear/Brom_Shield.png", 48, 16);
        RequirePng(Src + "/Gear/Brom_Hand.png", 4, 4);
        RequirePng(Src + "/Gear/Brom_ComboVfx.png", 864, 48);
        foreach (var v in Views)
        {
            RequirePng(Src + "/Extra/Brom_Hurt_" + v + "_0.png", 32, 32);
            RequirePng(Src + "/Extra/Brom_Hurt_" + v + "_1.png", 32, 32);
        }
        for (int i = 0; i < 3; i++)
            RequirePng(Src + "/Extra/Brom_Death_Down_" + i + ".png", 32, 32);

        foreach (var sub in new[] { "", "/Extra", "/Gear", "/Animations", "/Animators" })
            Directory.CreateDirectory(Dst + sub);

        File.Copy(Src + "/Brom_Sheet.png", Dst + "/Brom_Sheet.png", true);
        File.Copy(Src + "/Gear/Brom_Hammer.png", Dst + "/Gear/Brom_Hammer.png", true);
        File.Copy(Src + "/Gear/Brom_Shield.png", Dst + "/Gear/Brom_Shield.png", true);
        File.Copy(Src + "/Gear/Brom_Hand.png", Dst + "/Gear/Brom_Hand.png", true);
        File.Copy(Src + "/Gear/Brom_ComboVfx.png", Dst + "/Gear/Brom_ComboVfx.png", true);
        foreach (var v in Views)
        {
            File.Copy(Src + "/Extra/Brom_Hurt_" + v + "_0.png", Dst + "/Extra/Brom_Hurt_" + v + "_0.png", true);
            File.Copy(Src + "/Extra/Brom_Hurt_" + v + "_1.png", Dst + "/Extra/Brom_Hurt_" + v + "_1.png", true);
        }
        for (int i = 0; i < 3; i++)
            File.Copy(Src + "/Extra/Brom_Death_Down_" + i + ".png", Dst + "/Extra/Brom_Death_Down_" + i + ".png", true);
        WriteApprovedAnchors();
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        var body = new List<SpriteRect>();
        string[] anims = { "Idle", "Walk", "Attack" };
        for (int a = 0; a < 3; a++)
            for (int v = 0; v < 3; v++)
                for (int f = 0; f < (a == 2 ? 3 : 4); f++)
                    body.Add(R("Brom_" + anims[a] + "_" + Views[v] + "_" + f, f * 32, (a * 3 + v) * 32, 32, 32, 288, new Vector2(0.5f, 0.0625f)));
        Slice(Dst + "/Brom_Sheet.png", body);

        foreach (var v in Views)
        {
            Single(Dst + "/Extra/Brom_Hurt_" + v + "_0.png", new Vector2(0.5f, 0.0625f));
            Single(Dst + "/Extra/Brom_Hurt_" + v + "_1.png", new Vector2(0.5f, 0.0625f));
        }
        for (int i = 0; i < 3; i++)
            Single(Dst + "/Extra/Brom_Death_Down_" + i + ".png", new Vector2(0.5f, 0.0625f));

        var hammer = new List<SpriteRect>();
        for (int d = 0; d < 8; d++)
            hammer.Add(R("Brom_Hammer_" + Dir8[d], d * 48, 0, 48, 48, 48, new Vector2(0.5f, 0.5f)));
        Slice(Dst + "/Gear/Brom_Hammer.png", hammer);

        var shield = new List<SpriteRect>();
        for (int v = 0; v < 3; v++)
            shield.Add(R("Brom_Shield_" + Views[v], v * 16, 0, 16, 16, 16, new Vector2(0.5f, 0.5f)));
        Slice(Dst + "/Gear/Brom_Shield.png", shield);
        Single(Dst + "/Gear/Brom_Hand.png", new Vector2(0.25f, 0.75f));

        var vfx = new List<SpriteRect>();
        for (int step = 0; step < 3; step++)
            for (int v = 0; v < 3; v++)
                for (int fr = 0; fr < 2; fr++)
                {
                    int cell = step * 6 + v * 2 + fr;
                    vfx.Add(R("Brom_Combo_" + step + "_" + Views[v] + "_" + fr, cell * 48, 0, 48, 48, 48, new Vector2(0.5f, 10f / 48f)));
                }
        Slice(Dst + "/Gear/Brom_ComboVfx.png", vfx);

        int sprites = Sprites(Dst + "/Brom_Sheet.png").Count
            + 6 + 3 + Sprites(Dst + "/Gear/Brom_Hammer.png").Count
            + Sprites(Dst + "/Gear/Brom_Shield.png").Count + 1
            + Sprites(Dst + "/Gear/Brom_ComboVfx.png").Count;
        if (sprites != 72)
            throw new System.Exception("[R3E7A] expected 72 sprites, got " + sprites);

        var ctrl = BuildController();
        var set = BuildSet();
        AssetDatabase.SaveAssets();
        Debug.Log("[R3E7A] Brom base imported. sprites=72 clips=13 controller=" + ctrl.name + " set=" + set.name + ". F2 kit was not assigned. VISUAL ONLY.");
    }

    [MenuItem("Game01/R3E7A/Preview Brom Visual (Play Mode)", true)]
    static bool PreviewValidate() => Application.isPlaying;

    [MenuItem("Game01/R3E7A/Preview Brom Visual (Play Mode)")]
    public static void Preview()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) { Debug.LogError("[R3E7A] No Player."); return; }
        var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Dst + "/Animators/Brom.controller");
        var set = AssetDatabase.LoadAssetAtPath<GameplayPrototype.EquipmentSet>(KitDir + "/EquipmentSet_Brom.asset");
        if (ctrl == null || set == null) { Debug.LogError("[R3E7A] Import Brom Base first."); return; }
        player.GetComponent<GameplayPrototype.PlayerVisual>()?.RebindBodyController(ctrl);
        player.GetComponent<GameplayPrototype.EquipmentVisual>()?.Configure(set);
        Debug.Log("[R3E7A] VISUAL ONLY. Body and gear swapped. Combat kit was not changed. F2 stays empty.");
    }

    [MenuItem("Game01/R3E7A/Restore Active Class Visual (Play Mode)", true)]
    static bool RestoreValidate() => Application.isPlaying;

    [MenuItem("Game01/R3E7A/Restore Active Class Visual (Play Mode)")]
    public static void RestoreActiveVisual()
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;
        var kit = player.GetComponent<GameplayPrototype.ClassCombat>()?.Active;
        if (kit == null) { Debug.LogError("[R3E7A] No active class kit to restore."); return; }
        if (kit.bodyController != null)
            player.GetComponent<GameplayPrototype.PlayerVisual>()?.RebindBodyController(kit.bodyController);
        if (kit.equipment != null)
            player.GetComponent<GameplayPrototype.EquipmentVisual>()?.Configure(kit.equipment);
        Debug.Log("[R3E7A] Restored visual for " + kit.displayName);
    }

    static void WriteApprovedAnchors()
    {
        var kept = new List<string>();
        foreach (var line in File.ReadAllLines(Src + "/Brom_Anchors.csv"))
        {
            if (line.StartsWith("Death_Up_") || line.StartsWith("Death_Side_")) continue;
            kept.Add(line);
        }
        File.WriteAllLines(Dst + "/Brom_Anchors.csv", kept);
    }

    static void RequirePng(string path, int w, int h)
    {
        var b = File.ReadAllBytes(path);
        int aw = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
        int ah = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
        if (aw != w || ah != h)
            throw new System.Exception("[R3E7A] " + path + " is " + aw + "x" + ah + ", expected " + w + "x" + h);
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
        var s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect;
        s.spritePixelsPerUnit = 16;
        imp.SetTextureSettings(s);
        return imp;
    }

    static void Single(string path, Vector2 pivot)
    {
        var imp = PixelImporter(path, SpriteImportMode.Single);
        var s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteAlignment = (int)SpriteAlignment.Custom;
        s.spritePivot = pivot;
        s.spriteMeshType = SpriteMeshType.FullRect;
        imp.SetTextureSettings(s);
        imp.SaveAndReimport();
    }

    static void Slice(string path, List<SpriteRect> rects)
    {
        var imp = PixelImporter(path, SpriteImportMode.Multiple);
        imp.SaveAndReimport();
        var factory = new SpriteDataProviderFactories();
        factory.Init();
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

    static SpriteRect R(string name, int x, int yTop, int w, int h, int texH, Vector2 pivot) =>
        new SpriteRect { name = name, rect = new Rect(x, texH - yTop - h, w, h), alignment = SpriteAlignment.Custom, pivot = pivot, spriteID = GUID.Generate() };

    static Dictionary<string, Sprite> Sprites(string path)
    {
        var d = new Dictionary<string, Sprite>();
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
            if (o is Sprite s) d[s.name] = s;
        return d;
    }

    static Sprite[] Ordered(Dictionary<string, Sprite> d, IEnumerable<string> names)
    {
        var l = new List<Sprite>();
        foreach (var n in names)
        {
            if (!d.TryGetValue(n, out var s)) throw new System.Exception("[R3E7A] missing sprite " + n);
            l.Add(s);
        }
        return l.ToArray();
    }

    static AnimationClip Clip(string name, Sprite[] frames, float[] times, float stop, bool loop)
    {
        string path = Dst + "/Animations/" + name + ".anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(path) != null) AssetDatabase.DeleteAsset(path);
        var clip = new AnimationClip { name = name, frameRate = 60 };
        var keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = times[i], value = frames[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
        var st = AnimationUtility.GetAnimationClipSettings(clip);
        st.loopTime = loop;
        st.stopTime = stop;
        AnimationUtility.SetAnimationClipSettings(clip, st);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    static AnimatorController BuildController()
    {
        var sheet = Sprites(Dst + "/Brom_Sheet.png");
        var clips = new Dictionary<string, AnimationClip>();
        foreach (var v in Views)
        {
            clips["Hero_Idle_" + v] = Clip("Brom_Idle_" + v, Ordered(sheet, Named(v, "Brom_Idle_", 4)), new[] { 0f, 0.25f, 0.40f, 0.65f }, 0.80f, true);
            clips["Hero_Walk_" + v] = Clip("Brom_Walk_" + v, Ordered(sheet, Named(v, "Brom_Walk_", 4)), new[] { 0f, 0.125f, 0.25f, 0.375f }, 0.5f, true);
            clips["Hero_Attack_" + v] = Clip("Brom_Attack_" + v, Ordered(sheet, Named(v, "Brom_Attack_", 3)), new[] { 0f, 0.16f, 0.29f }, 0.42f, false);
            var h0 = AssetDatabase.LoadAssetAtPath<Sprite>(Dst + "/Extra/Brom_Hurt_" + v + "_0.png");
            var h1 = AssetDatabase.LoadAssetAtPath<Sprite>(Dst + "/Extra/Brom_Hurt_" + v + "_1.png");
            clips["Hero_Hurt_" + v] = Clip("Brom_Hurt_" + v, new[] { h0, h1 }, new[] { 0f, 0.06f }, 0.18f, false);
        }
        var d0 = AssetDatabase.LoadAssetAtPath<Sprite>(Dst + "/Extra/Brom_Death_Down_0.png");
        var d1 = AssetDatabase.LoadAssetAtPath<Sprite>(Dst + "/Extra/Brom_Death_Down_1.png");
        var d2 = AssetDatabase.LoadAssetAtPath<Sprite>(Dst + "/Extra/Brom_Death_Down_2.png");
        clips["Hero_Death"] = Clip("Brom_Death", new[] { d0, d1, d2, d2 }, new[] { 0f, 0.15f, 0.30f, 0.60f }, 0.68f, false);

        string ctrlPath = Dst + "/Animators/Brom.controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath) != null) AssetDatabase.DeleteAsset(ctrlPath);
        AssetDatabase.CopyAsset(CtrlSrc, ctrlPath);
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ctrlPath);
        ctrl.name = "Brom";
        int n = 0;
        foreach (var layer in ctrl.layers)
            foreach (var s in layer.stateMachine.states)
                if (clips.TryGetValue(s.state.name, out var c)) { s.state.motion = c; n++; }
        if (n != 13) throw new System.Exception("[R3E7A] remapped " + n + " states, expected 13");
        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    static string[] Named(string view, string prefix, int count)
    {
        var names = new string[count];
        for (int i = 0; i < count; i++) names[i] = prefix + view + "_" + i;
        return names;
    }

    static GameplayPrototype.EquipmentSet BuildSet()
    {
        Directory.CreateDirectory(KitDir);
        string path = KitDir + "/EquipmentSet_Brom.asset";
        var set = AssetDatabase.LoadAssetAtPath<GameplayPrototype.EquipmentSet>(path);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<GameplayPrototype.EquipmentSet>();
            AssetDatabase.CreateAsset(set, path);
        }
        var hammer = Sprites(Dst + "/Gear/Brom_Hammer.png");
        var shield = Sprites(Dst + "/Gear/Brom_Shield.png");
        var vfx = Sprites(Dst + "/Gear/Brom_ComboVfx.png");
        set.anchorTable = AssetDatabase.LoadAssetAtPath<TextAsset>(Dst + "/Brom_Anchors.csv");
        set.weaponPoses = Ordered(hammer, System.Array.ConvertAll(Dir8, x => "Brom_Hammer_" + x));
        set.offPoses = new Sprite[0];
        set.shieldViews = Ordered(shield, System.Array.ConvertAll(Views, x => "Brom_Shield_" + x));
        set.hand = AssetDatabase.LoadAssetAtPath<Sprite>(Dst + "/Gear/Brom_Hand.png");
        var slash = new List<string>();
        for (int step = 0; step < 3; step++)
            foreach (var v in Views)
                for (int fr = 0; fr < 2; fr++)
                    slash.Add("Brom_Combo_" + step + "_" + v + "_" + fr);
        set.slashFrames = Ordered(vfx, slash);
        set.replaceLegacyRSlash = true;
        EditorUtility.SetDirty(set);
        return set;
    }
}
