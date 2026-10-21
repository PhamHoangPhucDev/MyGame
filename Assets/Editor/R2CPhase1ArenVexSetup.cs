using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// R2C phase 1: death Up/Side for Aren and Vex, and the approved Vex skill VFX.
/// Does not rewrite PNG pixels. Does not open or save a scene.
/// </summary>
public static class R2CPhase1ArenVexSetup
{
    const string ArenSrc = "Docs/ReferenceRebuild/R2/R2C_OriginalHero/unity_export";
    const string VexSrc = "Docs/ReferenceRebuild/R2/R2C_OriginalHero/unity_export_vex";
    const string ArenDst = "Assets/Art/Rebuild/R2C/Aren";
    const string VexDst = "Assets/Art/Rebuild/R2C/Vex";
    static readonly string[] Views = { "Up", "Side" };

    [MenuItem("Game01/R2C/Phase 1 Import Aren Vex")]
    public static void Import()
    {
        RequirePng(ArenSrc + "/Gear/Aren_Sword.png", 432, 48);
        RequirePng(VexSrc + "/Gear/Vex_Dagger.png", 480, 48);
        foreach (var v in Views)
            for (int i = 0; i < 3; i++)
            {
                RequirePng(ArenSrc + "/Extra/Aren_Death_" + v + "_" + i + ".png", 32, 32);
                RequirePng(VexSrc + "/Extra/Vex_Death_" + v + "_" + i + ".png", 32, 32);
            }
        RequirePng(VexSrc + "/Gear/Vex_ShadowStep.png", 128, 32);
        RequirePng(VexSrc + "/Gear/Vex_BladeDance_Back.png", 384, 48);
        RequirePng(VexSrc + "/Gear/Vex_BladeDance_Front.png", 384, 48);
        RequirePng(VexSrc + "/Gear/Vex_KnifeHit.png", 96, 24);

        CopyDeath("Aren", ArenSrc, ArenDst);
        CopyDeath("Vex", VexSrc, VexDst);
        File.Copy(VexSrc + "/Gear/Vex_ShadowStep.png", VexDst + "/Gear/Vex_ShadowStep.png", true);
        File.Copy(VexSrc + "/Gear/Vex_BladeDance_Back.png", VexDst + "/Gear/Vex_BladeDance_Back.png", true);
        File.Copy(VexSrc + "/Gear/Vex_BladeDance_Front.png", VexDst + "/Gear/Vex_BladeDance_Front.png", true);
        File.Copy(VexSrc + "/Gear/Vex_KnifeHit.png", VexDst + "/Gear/Vex_KnifeHit.png", true);
        File.Copy(ArenSrc + "/Aren_Anchors.csv", ArenDst + "/Aren_Anchors.csv", true);
        AppendDeathRows(ArenSrc + "/Aren_Anchors.csv", ArenDst + "/Aren_Anchors_BackGear.csv");
        AppendDeathRows(VexSrc + "/Vex_Anchors.csv", VexDst + "/Vex_Anchors.csv");
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        foreach (var v in Views)
            for (int i = 0; i < 3; i++)
            {
                Single(ArenDst + "/Extra/Aren_Death_" + v + "_" + i + ".png", new Vector2(0.5f, 0.0625f));
                Single(VexDst + "/Extra/Vex_Death_" + v + "_" + i + ".png", new Vector2(0.5f, 0.0625f));
            }

        var shadow = new List<SpriteRect>();
        for (int i = 0; i < 4; i++)
            shadow.Add(R("Vex_ShadowStep_" + i, i * 32, 0, 32, 32, 32, new Vector2(0.5f, 0.0625f)));
        Slice(VexDst + "/Gear/Vex_ShadowStep.png", shadow);
        SliceRow(VexDst + "/Gear/Vex_BladeDance_Back.png", "Vex_BladeDance_Back_", 6, 64, 48, new Vector2(0.5f, 0.4167f));
        SliceRow(VexDst + "/Gear/Vex_BladeDance_Front.png", "Vex_BladeDance_Front_", 6, 64, 48, new Vector2(0.5f, 0.4167f));
        SliceRow(VexDst + "/Gear/Vex_KnifeHit.png", "Vex_KnifeHit_", 4, 24, 24, new Vector2(0.5f, 0.5f));

        int arenStates = BuildDeath("Aren", ArenDst);
        int vexStates = BuildDeath("Vex", VexDst);
        AssignVexVfx();
        AssetDatabase.SaveAssets();
        Debug.Log("[R2C phase 1] death states added Aren=" + arenStates + " Vex=" + vexStates + ". Sword and dagger PNGs were already the approved files and were not rewritten.");
    }

    static void CopyDeath(string hero, string src, string dst)
    {
        foreach (var v in Views)
            for (int i = 0; i < 3; i++)
                File.Copy(src + "/Extra/" + hero + "_Death_" + v + "_" + i + ".png", dst + "/Extra/" + hero + "_Death_" + v + "_" + i + ".png", true);
    }

    static void AppendDeathRows(string sourceCsv, string destCsv)
    {
        var dest = File.ReadAllText(destCsv);
        if (dest.Contains("Death_Up_0")) return;
        var add = new List<string>();
        foreach (var line in File.ReadAllLines(sourceCsv))
        {
            if (line.StartsWith("Death_Up_") || line.StartsWith("Death_Side_"))
                add.Add(line.Trim());
        }
        if (add.Count != 6) throw new System.Exception("[R2C phase 1] " + sourceCsv + " death rows " + add.Count);
        if (!dest.EndsWith("\n")) dest += "\n";
        int at = dest.IndexOf("Death_Down_2");
        if (at < 0) throw new System.Exception("[R2C phase 1] no Death_Down_2 in " + destCsv);
        int nl = dest.IndexOf('\n', at);
        dest = dest.Insert(nl + 1, string.Join("\n", add) + "\n");
        File.WriteAllText(destCsv, dest);
    }

    static int BuildDeath(string hero, string dst)
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(dst + "/Animators/" + hero + ".controller");
        int added = 0;
        foreach (var v in new[] { "Down", "Up", "Side" })
        {
            var frames = new Sprite[4];
            for (int i = 0; i < 3; i++)
                frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(dst + "/Extra/" + hero + "_Death_" + v + "_" + i + ".png");
            frames[3] = frames[2];
            if (frames[0] == null) throw new System.Exception("[R2C phase 1] missing " + hero + " death " + v);
            var clip = Clip(dst, hero + "_Death_" + v, frames, new[] { 0f, 0.15f, 0.30f, 0.60f });
            added += Ensure(ctrl.layers[0].stateMachine, "Hero_Death_" + v, clip);
        }
        EditorUtility.SetDirty(ctrl);
        return added;
    }

    static void AssignVexVfx()
    {
        var kit = AssetDatabase.LoadAssetAtPath<GameplayPrototype.ClassKit>("Assets/Art/Rebuild/R2C/ClassKits/ClassKit_Vex.asset");
        if (kit == null) throw new System.Exception("[R2C phase 1] ClassKit_Vex missing");
        var shadow = Sprites(VexDst + "/Gear/Vex_ShadowStep.png");
        var back = Sprites(VexDst + "/Gear/Vex_BladeDance_Back.png");
        var front = Sprites(VexDst + "/Gear/Vex_BladeDance_Front.png");
        var hit = Sprites(VexDst + "/Gear/Vex_KnifeHit.png");
        kit.slot2.vfxFrames = Ordered(shadow, new[] { "Vex_ShadowStep_0", "Vex_ShadowStep_1", "Vex_ShadowStep_2", "Vex_ShadowStep_3" });
        kit.slot2.vfxFps = 1f / 0.06f;
        var dance = new List<string>();
        for (int i = 0; i < 6; i++) dance.Add("Vex_BladeDance_Back_" + i);
        for (int i = 0; i < 6; i++) dance.Add("Vex_BladeDance_Front_" + i);
        kit.slot4.vfxFrames = Ordered(Merge(back, front), dance);
        kit.slot3.vfxFrames = Ordered(hit, new[] { "Vex_KnifeHit_0", "Vex_KnifeHit_1", "Vex_KnifeHit_2", "Vex_KnifeHit_3" });
        EditorUtility.SetDirty(kit);
    }

    static Dictionary<string, Sprite> Merge(Dictionary<string, Sprite> a, Dictionary<string, Sprite> b)
    {
        foreach (var kv in b) a[kv.Key] = kv.Value;
        return a;
    }

    static void RequirePng(string path, int w, int h)
    {
        var b = File.ReadAllBytes(path);
        int aw = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
        int ah = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
        if (aw != w || ah != h)
            throw new System.Exception("[R2C phase 1] " + path + " is " + aw + "x" + ah + ", manifest expected " + w + "x" + h);
    }

    static TextureImporter Pixel(string path, SpriteImportMode mode)
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
        var imp = Pixel(path, SpriteImportMode.Single);
        var s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteAlignment = (int)SpriteAlignment.Custom;
        s.spritePivot = pivot;
        s.spriteMeshType = SpriteMeshType.FullRect;
        imp.SetTextureSettings(s);
        imp.SaveAndReimport();
    }

    static void SliceRow(string path, string prefix, int count, int w, int h, Vector2 pivot)
    {
        var rects = new List<SpriteRect>();
        for (int i = 0; i < count; i++)
            rects.Add(R(prefix + i, i * w, 0, w, h, h, pivot));
        Slice(path, rects);
    }

    static void Slice(string path, List<SpriteRect> rects)
    {
        var imp = Pixel(path, SpriteImportMode.Multiple);
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
            if (!d.TryGetValue(n, out var s)) throw new System.Exception("[R2C phase 1] missing sprite " + n);
            l.Add(s);
        }
        return l.ToArray();
    }

    static AnimationClip Clip(string dst, string name, Sprite[] frames, float[] times)
    {
        string path = dst + "/Animations/" + name + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = name, frameRate = 60 };
            AssetDatabase.CreateAsset(clip, path);
        }
        var keys = new ObjectReferenceKeyframe[frames.Length];
        for (int i = 0; i < frames.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = times[i], value = frames[i] };
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
        var st = AnimationUtility.GetAnimationClipSettings(clip);
        st.loopTime = false;
        st.stopTime = 0.68f;
        AnimationUtility.SetAnimationClipSettings(clip, st);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    static int Ensure(AnimatorStateMachine sm, string name, Motion motion)
    {
        foreach (var child in sm.states)
        {
            if (child.state.name != name) continue;
            child.state.motion = motion;
            return 0;
        }
        var state = sm.AddState(name);
        state.motion = motion;
        return 1;
    }
}
