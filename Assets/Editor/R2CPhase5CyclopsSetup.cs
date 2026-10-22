using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using GameplayPrototype;

/// <summary>R2C phase 5: import Cyclops into the sandbox roster. Does not save a scene and does not touch Level 5.</summary>
public static class R2CPhase5CyclopsSetup
{
    const string Src = "Docs/ReferenceRebuild/R2/R2C_OriginalHero/unity_export_monsters";
    const string Dst = "Assets/Art/Rebuild/R2C/Monsters";
    const string RosterPath = "Assets/Resources/R2C/R2CMonsterRoster.asset";

    struct Frame
    {
        public string sprite, file, anim, view, mode;
        public int col, row, index, fps;
    }

    [MenuItem("Game01/R2C/Phase 5 Import Cyclops")]
    public static void Import()
    {
        var frames = new List<Frame>();
        foreach (var line in File.ReadAllLines(Src + "/MONSTERS_FRAMES.csv"))
        {
            if (line.Length == 0 || line.StartsWith("sprite,")) continue;
            var p = line.Split(',');
            if (!p[1].StartsWith("Cyclops/")) continue;
            frames.Add(new Frame
            {
                sprite = p[0], file = p[1], col = int.Parse(p[2]), row = int.Parse(p[3]),
                anim = p[8], view = p[9], index = int.Parse(p[10]), fps = int.Parse(p[12]), mode = p[13]
            });
        }
        Directory.CreateDirectory(Dst + "/Cyclops");
        CopyChecked(Src + "/Cyclops/Cyclops_Sheet.png", Dst + "/Cyclops/Cyclops_Sheet.png", 384, 2016);
        CopyChecked(Src + "/Cyclops/Cyclops_Telegraph.png", Dst + "/Cyclops/Cyclops_Telegraph.png", 768, 128);
        CopyChecked(Src + "/Cyclops/Cyclops_Crack.png", Dst + "/Cyclops/Cyclops_Crack.png", 96, 48);
        SliceBody(Dst + "/Cyclops/Cyclops_Sheet.png", frames);
        var ring = SliceRow(Dst + "/Cyclops/Cyclops_Telegraph.png", "Cyclops_Telegraph", 128, 128, 6, new Vector2(0.5f, 0.5f));
        var crack = SliceRow(Dst + "/Cyclops/Cyclops_Crack.png", "Cyclops_Crack", 96, 48, 1, new Vector2(0.5f, 0.5f));
        var sprites = LoadSprites(Dst + "/Cyclops/Cyclops_Sheet.png");
        var calm = BuildController(sprites, false);
        var angry = BuildController(sprites, true);
        var over = new AnimatorOverrideController(calm);
        var pairs = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        foreach (var view in new[] { "Down", "Up", "Side" })
        {
            pairs.Add(new KeyValuePair<AnimationClip, AnimationClip>(FindClip(calm, "Idle_" + view), FindClip(angry, "Idle_" + view)));
            pairs.Add(new KeyValuePair<AnimationClip, AnimationClip>(FindClip(calm, "Walk_" + view), FindClip(angry, "Walk_" + view)));
        }
        over.ApplyOverrides(pairs);
        string overPath = Dst + "/Animators/Cyclops_Enraged.overrideController";
        Directory.CreateDirectory(Dst + "/Animators");
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(overPath);
        if (existing != null) AssetDatabase.DeleteAsset(overPath);
        AssetDatabase.CreateAsset(over, overPath);

        var roster = AssetDatabase.LoadAssetAtPath<R2CMonsterRoster>(RosterPath);
        var list = new List<R2CMonsterDef>();
        if (roster.monsters != null)
            foreach (var m in roster.monsters)
                if (m == null || m.label == null || !m.label.StartsWith("Cyclops")) list.Add(m);
        list.Add(new R2CMonsterDef
        {
            label = "Cyclops",
            controller = calm,
            hp = 280,
            damage = 12,
            speed = 1.15f,
            aggro = 7f,
            meleeRange = 3.2f,
            cooldown = 3.2f,
            colliderSize = new Vector2(2.25f, 0.875f),
            bossBar = true,
            telegraph = ring,
            crack = crack[0],
            enraged = over,
            slamDamage = 22,
            slamRadius = 3.9f
        });
        roster.monsters = list.ToArray();
        EditorUtility.SetDirty(roster);
        AssetDatabase.SaveAssets();
        Debug.Log("[R2C phase 5] Cyclops imported. states=" + (calm != null));
    }

    static AnimationClip FindClip(AnimatorController ctrl, string name)
    {
        foreach (var s in ctrl.layers[0].stateMachine.states)
            if (s.state.name == name) return s.state.motion as AnimationClip;
        throw new System.Exception("[R2C phase 5] missing state " + name);
    }

    static void CopyChecked(string src, string dst, int w, int h)
    {
        var b = File.ReadAllBytes(src);
        int iw = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
        int ih = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
        if (iw != w || ih != h) throw new System.Exception("[R2C phase 5] " + src + " is " + iw + "x" + ih);
        File.Copy(src, dst, true);
    }

    static void SliceBody(string path, List<Frame> frames)
    {
        var imp = Prepare(path, 16);
        int texH = 2016;
        var rects = new List<SpriteRect>();
        foreach (var f in frames)
        {
            if (!f.file.EndsWith("Cyclops_Sheet.png")) continue;
            rects.Add(new SpriteRect
            {
                name = f.sprite,
                rect = new Rect(f.col * 96, texH - (f.row + 1) * 96, 96, 96),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2(0.5f, 0.0625f),
                spriteID = GUID.Generate()
            });
        }
        Apply(imp, rects);
    }

    static Sprite[] SliceRow(string path, string prefix, int w, int h, int cols, Vector2 pivot)
    {
        var imp = Prepare(path, 16);
        var rects = new List<SpriteRect>();
        for (int i = 0; i < cols; i++)
        {
            rects.Add(new SpriteRect
            {
                name = cols == 1 ? prefix : prefix + "_" + i,
                rect = new Rect(i * w, 0, w, h),
                alignment = SpriteAlignment.Custom,
                pivot = pivot,
                spriteID = GUID.Generate()
            });
        }
        Apply(imp, rects);
        var all = LoadSprites(path);
        var a = new Sprite[cols];
        for (int i = 0; i < cols; i++)
            a[i] = all[cols == 1 ? prefix : prefix + "_" + i];
        return a;
    }

    static TextureImporter Prepare(string path, float ppu)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Multiple;
        imp.spritePixelsPerUnit = ppu;
        imp.filterMode = FilterMode.Point;
        imp.mipmapEnabled = false;
        imp.textureCompression = TextureImporterCompression.Uncompressed;
        imp.alphaIsTransparency = true;
        imp.wrapMode = TextureWrapMode.Clamp;
        var s = new TextureImporterSettings();
        imp.ReadTextureSettings(s);
        s.spriteMeshType = SpriteMeshType.FullRect;
        s.spritePixelsPerUnit = ppu;
        imp.SetTextureSettings(s);
        imp.SaveAndReimport();
        return imp;
    }

    static void Apply(TextureImporter imp, List<SpriteRect> rects)
    {
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

    static Dictionary<string, Sprite> LoadSprites(string path)
    {
        var d = new Dictionary<string, Sprite>();
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
            if (o is Sprite s) d[s.name] = s;
        return d;
    }

    static AnimatorController BuildController(Dictionary<string, Sprite> sprites, bool angry)
    {
        string path = Dst + "/Animators/Cyclops" + (angry ? "_Angry" : "") + ".controller";
        Directory.CreateDirectory(Dst + "/Animators");
        Directory.CreateDirectory(Dst + "/Animations");
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        var sm = ctrl.layers[0].stateMachine;
        AnimatorState idle = null;
        var map = angry
            ? new[] { ("Idle", "IdleAngry", 6, true), ("Walk", "WalkAngry", 7, true), ("Attack", "Attack", 8, false), ("Hurt", "Hurt", 10, false), ("Death", "Death", 6, false) }
            : new[] { ("Idle", "IdleCalm", 5, true), ("Walk", "WalkCalm", 6, true), ("Attack", "Attack", 8, false), ("Hurt", "Hurt", 10, false), ("Death", "Death", 6, false) };
        foreach (var (stateName, sheetAnim, fps, loop) in map)
        {
            foreach (var view in new[] { "Down", "Up", "Side" })
            {
                var frames = new List<Sprite>();
                for (int i = 0; i < 8; i++)
                {
                    string name = "Cyclops_" + sheetAnim + "_" + view + "_" + i;
                    if (!sprites.TryGetValue(name, out var sprite)) break;
                    frames.Add(sprite);
                }
                if (frames.Count == 0) throw new System.Exception("[R2C phase 5] no frames " + sheetAnim + " " + view);
                var clip = Clip(angry ? "CyclopsAngry" : "Cyclops", stateName, view, frames, fps, loop);
                var state = sm.AddState(stateName + "_" + view);
                state.motion = clip;
                if (stateName == "Idle" && view == "Down") idle = state;
            }
        }
        sm.defaultState = idle;
        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    static AnimationClip Clip(string folder, string anim, string view, List<Sprite> frames, int fps, bool loop)
    {
        string path = Dst + "/Animations/" + folder + "_" + anim + "_" + view + ".anim";
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
        var clip = new AnimationClip { name = anim + "_" + view, frameRate = 60 };
        var keys = new ObjectReferenceKeyframe[loop ? frames.Count : frames.Count + 1];
        for (int i = 0; i < frames.Count; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / (float)fps, value = frames[i] };
        if (!loop)
            keys[frames.Count] = new ObjectReferenceKeyframe { time = frames.Count / (float)fps, value = frames[frames.Count - 1] };
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
        var st = AnimationUtility.GetAnimationClipSettings(clip);
        st.loopTime = loop;
        st.stopTime = (loop ? frames.Count : frames.Count + 1) / (float)fps;
        AnimationUtility.SetAnimationClipSettings(clip, st);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }
}
