using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// R3E.6: slice the approved Vex skill-body sheet and add clips to the existing Vex controller.
/// Does not rewrite Vex Idle/Walk/Hurt/Attack, the dagger sheet, or any Aren asset.
/// </summary>
public static class R3E6VexSkillBodySetup
{
    const string SourcePng = "Docs/ReferenceRebuild/R2/R2C_OriginalHero/unity_export_vex/Skills/Vex_SkillSheet.png";
    const string SourceCsv = "Docs/ReferenceRebuild/R2/R2C_OriginalHero/unity_export_vex/Skills/Vex_SkillAnchors.csv";
    const string Sheet = "Assets/Art/Rebuild/R2C/Vex/Skills/Vex_SkillSheet.png";
    const string Anchors = "Assets/Art/Rebuild/R2C/Vex/Vex_Anchors.csv";
    const string ClipDir = "Assets/Art/Rebuild/R2C/Vex/Animations";
    const string Controller = "Assets/Art/Rebuild/R2C/Vex/Animators/Vex.controller";

    // Top row is 0. Sheet is 128×416. Blade Dance has no view.
    static readonly (int row, string skill, string view, int frames)[] Rows =
    {
        (0, "ShadowStep", "Down", 2), (1, "ShadowStep", "Up", 2), (2, "ShadowStep", "Side", 2),
        (3, "KnifeThrow", "Down", 2), (4, "KnifeThrow", "Up", 2), (5, "KnifeThrow", "Side", 2),
        (6, "SmokeBomb", "Down", 1), (7, "SmokeBomb", "Up", 1), (8, "SmokeBomb", "Side", 1),
        (9, "Execute", "Down", 2), (10, "Execute", "Up", 2), (11, "Execute", "Side", 2),
        (12, "BladeDance", "", 4),
    };

    [MenuItem("Game01/R3E6/Import Vex Skill Bodies")]
    public static void Apply()
    {
        if (!File.Exists(SourcePng) || !File.Exists(SourceCsv))
            throw new System.Exception("Vex skill source missing");

        Directory.CreateDirectory("Assets/Art/Rebuild/R2C/Vex/Skills");
        File.Copy(SourcePng, Sheet, true);
        AssetDatabase.ImportAsset(Sheet, ImportAssetOptions.ForceSynchronousImport);
        Slice();
        var sprites = Sprites();
        if (sprites.Count != 25)
            throw new System.Exception("Vex skill sheet sliced " + sprites.Count + " sprites, expected 25");

        var shadow = ClipPair("ShadowStep", new[] { 0f, 0.08f, 0.23f }, sprites);
        var knife = ClipPair("KnifeThrow", new[] { 0f, 0.10f, 0.22f }, sprites);
        var smoke = ClipSingle("SmokeBomb", 0.15f, sprites);
        var execute = ClipPair("Execute", new[] { 0f, 0.08f, 0.20f }, sprites);
        var dance = ClipDance(sprites);

        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
        var sm = ctrl.layers[0].stateMachine;
        int added = 0;
        added += Ensure(sm, "Vex_ShadowStep_Down", shadow[0]);
        added += Ensure(sm, "Vex_ShadowStep_Up", shadow[1]);
        added += Ensure(sm, "Vex_ShadowStep_Side", shadow[2]);
        added += Ensure(sm, "Vex_KnifeThrow_Down", knife[0]);
        added += Ensure(sm, "Vex_KnifeThrow_Up", knife[1]);
        added += Ensure(sm, "Vex_KnifeThrow_Side", knife[2]);
        added += Ensure(sm, "Vex_SmokeBomb_Down", smoke[0]);
        added += Ensure(sm, "Vex_SmokeBomb_Up", smoke[1]);
        added += Ensure(sm, "Vex_SmokeBomb_Side", smoke[2]);
        added += Ensure(sm, "Vex_Execute_Down", execute[0]);
        added += Ensure(sm, "Vex_Execute_Up", execute[1]);
        added += Ensure(sm, "Vex_Execute_Side", execute[2]);
        added += Ensure(sm, "Vex_BladeDance", dance);
        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssetIfDirty(ctrl);

        int anchors = AppendAnchors();
        AssetDatabase.ImportAsset(Anchors, ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("[R3E6] sprites=25 statesAdded=" + added + " anchorsAppended=" + anchors);
    }

    static void Slice()
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(Sheet);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.mipmapEnabled = false;
        imp.filterMode = FilterMode.Point;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.alphaIsTransparency = true;
        imp.npotScale = TextureImporterNPOTScale.None;
        imp.spritePixelsPerUnit = 16f;
        var def = imp.GetDefaultPlatformTextureSettings();
        def.textureCompression = TextureImporterCompression.Uncompressed;
        def.crunchedCompression = false;
        imp.SetPlatformTextureSettings(def);
        var standalone = imp.GetPlatformTextureSettings("Standalone");
        standalone.overridden = true;
        standalone.textureCompression = TextureImporterCompression.Uncompressed;
        standalone.crunchedCompression = false;
        imp.SetPlatformTextureSettings(standalone);

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var dp = factory.GetSpriteEditorDataProviderFromObject(imp);
        dp.InitSpriteEditorDataProvider();
        var rects = new List<SpriteRect>();
        foreach (var row in Rows)
        {
            for (int f = 0; f < row.frames; f++)
            {
                float y = 416f - (row.row + 1) * 32f;
                rects.Add(new SpriteRect
                {
                    name = SpriteName(row.skill, row.view, f),
                    rect = new Rect(f * 32f, y, 32f, 32f),
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0.0625f),
                    spriteID = GUID.Generate(),
                });
            }
        }
        dp.SetSpriteRects(rects.ToArray());
        var ids = dp.GetDataProvider<ISpriteNameFileIdDataProvider>();
        var pairs = new List<SpriteNameFileIdPair>();
        foreach (var r in rects) pairs.Add(new SpriteNameFileIdPair(r.name, r.spriteID));
        ids.SetNameFileIdPairs(pairs);
        dp.Apply();
        imp.SaveAndReimport();
    }

    static string SpriteName(string skill, string view, int frame)
    {
        if (skill == "BladeDance") return "Vex_Skill_BladeDance_" + frame;
        return "Vex_Skill_" + skill + "_" + view + "_" + frame;
    }

    static Dictionary<string, Sprite> Sprites()
    {
        var d = new Dictionary<string, Sprite>();
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Sheet))
            if (o is Sprite s) d[s.name] = s;
        return d;
    }

    static AnimationClip[] ClipPair(string skill, float[] times, Dictionary<string, Sprite> sprites)
    {
        var clips = new AnimationClip[3];
        int i = 0;
        foreach (var view in new[] { "Down", "Up", "Side" })
        {
            var a = sprites[SpriteName(skill, view, 0)];
            var b = sprites[SpriteName(skill, view, 1)];
            clips[i++] = SaveClip("Vex_" + skill + "_" + view, false, new[]
            {
                (times[0], a), (times[1], b), (times[2], b),
            });
        }
        return clips;
    }

    static AnimationClip[] ClipSingle(string skill, float hold, Dictionary<string, Sprite> sprites)
    {
        var clips = new AnimationClip[3];
        int i = 0;
        foreach (var view in new[] { "Down", "Up", "Side" })
        {
            var frame = sprites[SpriteName(skill, view, 0)];
            clips[i++] = SaveClip("Vex_" + skill + "_" + view, false, new[] { (0f, frame), (hold, frame) });
        }
        return clips;
    }

    static AnimationClip ClipDance(Dictionary<string, Sprite> sprites)
    {
        var keys = new (float, Sprite)[5];
        for (int f = 0; f < 4; f++)
            keys[f] = (f * 0.05f, sprites[SpriteName("BladeDance", "", f)]);
        keys[4] = (0.20f, sprites[SpriteName("BladeDance", "", 3)]);
        return SaveClip("Vex_BladeDance", true, keys);
    }

    static AnimationClip SaveClip(string name, bool loop, (float time, Sprite sprite)[] keys)
    {
        string path = ClipDir + "/" + name + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.frameRate = 60f;
        clip.name = name;
        var bind = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var curve = new ObjectReferenceKeyframe[keys.Length];
        for (int i = 0; i < keys.Length; i++)
            curve[i] = new ObjectReferenceKeyframe { time = keys[i].time, value = keys[i].sprite };
        AnimationUtility.SetObjectReferenceCurve(clip, bind, curve);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssetIfDirty(clip);
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

    static int AppendAnchors()
    {
        var existing = File.ReadAllText(Anchors);
        if (existing.Contains("Skill_ShadowStep_Down_0")) return 0;
        var lines = File.ReadAllLines(SourceCsv);
        var add = new List<string>();
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0) continue;
            add.Add(line);
        }
        if (!existing.EndsWith("\n")) existing += "\n";
        File.WriteAllText(Anchors, existing + string.Join("\n", add) + "\n");
        return add.Count;
    }
}
