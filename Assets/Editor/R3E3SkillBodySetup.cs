using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// R3E.3: slice the approved Aren skill-body sheet and add clips to the existing Aren controller.
/// Does not rewrite Idle/Walk/Hurt/Attack clips or ClassKit damage.
/// </summary>
public static class R3E3SkillBodySetup
{
    const string SourcePng = "Docs/ReferenceRebuild/R2/R2C_OriginalHero/unity_export/Skills/Aren_SkillSheet.png";
    const string SourceCsv = "Docs/ReferenceRebuild/R2/R2C_OriginalHero/unity_export/Skills/Aren_SkillAnchors.csv";
    const string Sheet = "Assets/Art/Rebuild/R2C/Aren/Skills/Aren_SkillSheet.png";
    const string BackCsv = "Assets/Art/Rebuild/R2C/Aren/Aren_Anchors_BackGear.csv";
    const string ClipDir = "Assets/Art/Rebuild/R2C/Aren/Animations";
    const string Controller = "Assets/Art/Rebuild/R2C/Aren/Animators/Aren.controller";

    // row is top-down, matching AREN_SKILL_LAYOUT.csv. Whirl has no view.
    static readonly (int row, string skill, string view, int frames)[] Rows =
    {
        (0, "ShieldBash", "Down", 2), (1, "ShieldBash", "Up", 2), (2, "ShieldBash", "Side", 2),
        (3, "Charge", "Down", 2), (4, "Charge", "Up", 2), (5, "Charge", "Side", 2),
        (6, "Bulwark", "Down", 1), (7, "Bulwark", "Up", 1), (8, "Bulwark", "Side", 1),
        (9, "Dawnbreaker", "Down", 3), (10, "Dawnbreaker", "Up", 3), (11, "Dawnbreaker", "Side", 3),
        (12, "Whirl", "", 4),
    };

    [MenuItem("Game01/R3E3/Import Aren Skill Bodies")]
    public static void Apply()
    {
        Directory.CreateDirectory("Assets/Art/Rebuild/R2C/Aren/Skills");
        File.Copy(SourcePng, Sheet, true);
        AssetDatabase.ImportAsset(Sheet, ImportAssetOptions.ForceSynchronousImport);
        Slice();
        var sprites = Sprites();
        if (sprites.Count != 28)
            throw new System.Exception("Aren skill sheet sliced " + sprites.Count + " sprites, expected 28");

        var shield = ClipPair("ShieldBash", new[] { 0f, 0.08f, 0.26f }, false, sprites);
        var charge = ClipPair("Charge", new[] { 0f, 0.075f, 0.15f }, true, sprites);
        var bulwark = ClipSingle("Bulwark", sprites);
        // F1 starts at the live Dawn hit (anticipation 0.12s), then 0.08s impact, 0.15s recovery.
        var dawn = ClipTriple("Dawnbreaker", new[] { 0f, 0.12f, 0.20f, 0.35f }, sprites);
        var whirl = ClipWhirl(sprites);

        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(Controller);
        var sm = ctrl.layers[0].stateMachine;
        int added = 0;
        added += EnsureState(sm, "Aren_ShieldBash_Down", shield[0]);
        added += EnsureState(sm, "Aren_ShieldBash_Up", shield[1]);
        added += EnsureState(sm, "Aren_ShieldBash_Side", shield[2]);
        added += EnsureState(sm, "Aren_Charge_Down", charge[0]);
        added += EnsureState(sm, "Aren_Charge_Up", charge[1]);
        added += EnsureState(sm, "Aren_Charge_Side", charge[2]);
        added += EnsureState(sm, "Aren_Bulwark_Down", bulwark[0]);
        added += EnsureState(sm, "Aren_Bulwark_Up", bulwark[1]);
        added += EnsureState(sm, "Aren_Bulwark_Side", bulwark[2]);
        added += EnsureState(sm, "Aren_Dawnbreaker_Down", dawn[0]);
        added += EnsureState(sm, "Aren_Dawnbreaker_Up", dawn[1]);
        added += EnsureState(sm, "Aren_Dawnbreaker_Side", dawn[2]);
        added += EnsureState(sm, "Aren_Whirl", whirl);
        EditorUtility.SetDirty(ctrl);
        AssetDatabase.SaveAssetIfDirty(ctrl);

        int anchors = AppendAnchors();
        AssetDatabase.ImportAsset(BackCsv, ImportAssetOptions.ForceSynchronousImport);
        Debug.Log("[R3E3] sprites=28 statesAdded=" + added + " anchorsAppended=" + anchors);
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
                string name = SpriteName(row.skill, row.view, f);
                float y = 416f - (row.row + 1) * 32f;
                rects.Add(new SpriteRect
                {
                    name = name,
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
        if (skill == "Whirl") return "Aren_Skill_Whirl_" + frame;
        return "Aren_Skill_" + skill + "_" + view + "_" + frame;
    }

    static Dictionary<string, Sprite> Sprites()
    {
        var d = new Dictionary<string, Sprite>();
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(Sheet))
            if (o is Sprite s) d[s.name] = s;
        return d;
    }

    static AnimationClip[] ClipPair(string skill, float[] times, bool loop, Dictionary<string, Sprite> sprites)
    {
        var clips = new AnimationClip[3];
        int i = 0;
        foreach (var view in new[] { "Down", "Up", "Side" })
        {
            var frames = new[] { sprites[SpriteName(skill, view, 0)], sprites[SpriteName(skill, view, 1)] };
            // times: frame0, frame1, end (frame1 held).
            clips[i++] = SaveClip("Aren_" + skill + "_" + view, loop, new[]
            {
                (times[0], frames[0]), (times[1], frames[1]), (times[2], frames[1]),
            });
        }
        return clips;
    }

    static AnimationClip[] ClipSingle(string skill, Dictionary<string, Sprite> sprites)
    {
        var clips = new AnimationClip[3];
        int i = 0;
        foreach (var view in new[] { "Down", "Up", "Side" })
        {
            var frame = sprites[SpriteName(skill, view, 0)];
            clips[i++] = SaveClip("Aren_" + skill + "_" + view, false, new[] { (0f, frame), (1f, frame) });
        }
        return clips;
    }

    static AnimationClip[] ClipTriple(string skill, float[] times, Dictionary<string, Sprite> sprites)
    {
        var clips = new AnimationClip[3];
        int i = 0;
        foreach (var view in new[] { "Down", "Up", "Side" })
        {
            clips[i++] = SaveClip("Aren_" + skill + "_" + view, false, new[]
            {
                (times[0], sprites[SpriteName(skill, view, 0)]),
                (times[1], sprites[SpriteName(skill, view, 1)]),
                (times[2], sprites[SpriteName(skill, view, 2)]),
                (times[3], sprites[SpriteName(skill, view, 2)]),
            });
        }
        return clips;
    }

    static AnimationClip ClipWhirl(Dictionary<string, Sprite> sprites)
    {
        var keys = new (float, Sprite)[5];
        for (int f = 0; f < 4; f++)
            keys[f] = (f * 0.05f, sprites[SpriteName("Whirl", "", f)]);
        keys[4] = (0.20f, sprites[SpriteName("Whirl", "", 3)]);
        return SaveClip("Aren_Whirl", true, keys);
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

    static int EnsureState(AnimatorStateMachine sm, string name, Motion motion)
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
        var existing = File.ReadAllText(BackCsv);
        if (existing.Contains("Skill_ShieldBash_Down_0")) return 0;
        var lines = File.ReadAllLines(SourceCsv);
        var add = new List<string>();
        for (int i = 1; i < lines.Length; i++)
        {
            var line = lines[i].Trim();
            if (line.Length == 0) continue;
            add.Add(line);
        }
        File.AppendAllText(BackCsv, "\n" + string.Join("\n", add) + "\n");
        return add.Count;
    }
}
