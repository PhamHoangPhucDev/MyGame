using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

/// <summary>
/// Restores Phase 4 Hero_Sheet pixels, imports approved R2b sheet under Art/Rebuild/R2/Hero,
/// and points only ReferenceCombatSandbox at the rebuilt controller.
/// </summary>
public static class R2CHeroSandboxSetup
{
    const string Phase4Sheet = "Assets/Art/GoblinCampPhase4/Hero_Sheet.png";
    const string BackupSheet = "IncomingAssets/backup_before_R2B/Hero_Sheet.png";
    const string SourceR2 = "Docs/ReferenceRebuild/R2/R2B/Hero_Sheet_R2b.png";
    const string DestDir = "Assets/Art/Rebuild/R2/Hero";
    const string DestSheet = DestDir + "/Hero_Sheet.png";
    const string AnimSrc = "Assets/Art/GoblinCampPhase4/Animations";
    const string AnimDst = DestDir + "/Animations";
    const string CtrlSrc = "Assets/Art/GoblinCampPhase4/Animators/Hero.controller";
    const string CtrlDst = DestDir + "/Animators/Hero_R2.controller";
    const string Sandbox = "Assets/Scenes/ReferenceCombatSandbox.unity";

    [MenuItem("Tools/Gameplay Prototype/R2C/Setup Sandbox Hero Visual")]
    public static void Setup()
    {
        if (!File.Exists(BackupSheet) || !File.Exists(SourceR2))
            throw new System.IO.FileNotFoundException("Missing backup or R2b sheet.");

        var probe = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        probe.LoadImage(File.ReadAllBytes(SourceR2));
        if (probe.width != 128 || probe.height != 288 || probe.width % 32 != 0 || probe.height % 32 != 0)
        {
            Object.DestroyImmediate(probe);
            throw new System.Exception($"R2 sheet pixel grid fail: {probe.width}x{probe.height}, expected 128x288.");
        }
        Object.DestroyImmediate(probe);

        File.Copy(BackupSheet, Phase4Sheet, true);
        AssetDatabase.ImportAsset(Phase4Sheet, ImportAssetOptions.ForceUpdate);

        Directory.CreateDirectory(DestDir);
        File.Copy(SourceR2, DestSheet, true);
        AssetDatabase.ImportAsset(DestSheet, ImportAssetOptions.ForceSynchronousImport);
        CopySlices(Phase4Sheet, DestSheet);

        RemapAnimations();
        AssignSandboxController();
        AssetDatabase.SaveAssets();
        Debug.Log("[R2C] Phase4 sheet restored. Sandbox uses " + CtrlDst);
    }

    static void CopySlices(string srcPath, string dstPath)
    {
        var srcImp = (TextureImporter)AssetImporter.GetAtPath(srcPath);
        var dstImp = (TextureImporter)AssetImporter.GetAtPath(dstPath);
        dstImp.textureType = TextureImporterType.Sprite;
        dstImp.spritePixelsPerUnit = 16;
        dstImp.filterMode = FilterMode.Point;
        dstImp.mipmapEnabled = false;
        dstImp.textureCompression = TextureImporterCompression.Uncompressed;
        dstImp.alphaIsTransparency = true;
        dstImp.spriteImportMode = SpriteImportMode.Multiple;
        dstImp.wrapMode = TextureWrapMode.Clamp;

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var src = factory.GetSpriteEditorDataProviderFromObject(srcImp);
        src.InitSpriteEditorDataProvider();
        var rects = src.GetSpriteRects();

        var dst = factory.GetSpriteEditorDataProviderFromObject(dstImp);
        dst.InitSpriteEditorDataProvider();
        var copy = new SpriteRect[rects.Length];
        for (int i = 0; i < rects.Length; i++)
        {
            copy[i] = new SpriteRect
            {
                name = rects[i].name,
                rect = rects[i].rect,
                alignment = rects[i].alignment,
                pivot = rects[i].pivot,
                border = rects[i].border,
                spriteID = GUID.Generate()
            };
            if (copy[i].rect.width != 32 || copy[i].rect.height != 32)
                throw new System.Exception($"Slice {copy[i].name} is not 32x32.");
        }
        dst.SetSpriteRects(copy);
        var names = dst.GetDataProvider<ISpriteNameFileIdDataProvider>();
        var pairs = new List<SpriteNameFileIdPair>();
        foreach (var r in copy) pairs.Add(new SpriteNameFileIdPair(r.name, r.spriteID));
        names.SetNameFileIdPairs(pairs);
        dst.Apply();
        dstImp.SaveAndReimport();
    }

    static void RemapAnimations()
    {
        Directory.CreateDirectory(AnimDst);
        Directory.CreateDirectory(Path.GetDirectoryName(CtrlDst));
        var map = SpriteMap(Phase4Sheet, DestSheet);

        foreach (var src in Directory.GetFiles(AnimSrc, "Hero_*.anim"))
        {
            string name = Path.GetFileName(src);
            string dst = Path.Combine(AnimDst, name).Replace('\\', '/');
            if (AssetDatabase.LoadAssetAtPath<AnimationClip>(dst) != null)
                AssetDatabase.DeleteAsset(dst);
            AssetDatabase.CopyAsset(src, dst);
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(dst);
            var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
            foreach (var b in bindings)
            {
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, b);
                bool changed = false;
                for (int i = 0; i < keys.Length; i++)
                {
                    if (keys[i].value is Sprite sp && map.TryGetValue(sp, out var neu))
                    {
                        keys[i].value = neu;
                        changed = true;
                    }
                }
                if (changed)
                    AnimationUtility.SetObjectReferenceCurve(clip, b, keys);
            }
            RetimAttackClip(clip);
            EditorUtility.SetDirty(clip);
        }

        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(CtrlDst) != null)
            AssetDatabase.DeleteAsset(CtrlDst);
        AssetDatabase.CopyAsset(CtrlSrc, CtrlDst);
        var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(CtrlDst);
        RemapController(ctrl);
        EditorUtility.SetDirty(ctrl);
    }

    static void RemapController(AnimatorController ctrl)
    {
        var clips = new Dictionary<string, AnimationClip>();
        foreach (var guid in AssetDatabase.FindAssets("t:AnimationClip", new[] { AnimDst }))
        {
            var c = AssetDatabase.LoadAssetAtPath<AnimationClip>(AssetDatabase.GUIDToAssetPath(guid));
            if (c != null) clips[c.name] = c;
        }
        foreach (var layer in ctrl.layers)
            Walk(layer.stateMachine, clips);
    }

    static void Walk(AnimatorStateMachine sm, Dictionary<string, AnimationClip> clips)
    {
        foreach (var s in sm.states)
        {
            if (s.state.name.StartsWith("Hero_Attack_") && !s.state.name.Contains("Windup"))
            {
                string dir = s.state.name.Substring("Hero_Attack_".Length);
                if (clips.TryGetValue("Hero_Hero_Attack_" + dir, out var attack))
                {
                    s.state.motion = attack;
                    continue;
                }
            }
            if (s.state.motion is AnimationClip clip && clips.TryGetValue(clip.name, out var neu))
                s.state.motion = neu;
        }
        foreach (var child in sm.stateMachines)
            Walk(child.stateMachine, clips);
    }

    static void RetimAttackClip(AnimationClip clip)
    {
        if (clip == null || !clip.name.Contains("Attack") || clip.name.Contains("Windup"))
            return;
        var bindings = AnimationUtility.GetObjectReferenceCurveBindings(clip);
        if (bindings.Length == 0) return;
        var keys = AnimationUtility.GetObjectReferenceCurve(clip, bindings[0]);
        if (keys.Length < 3) return;
        keys[0].time = 0f;
        keys[1].time = 0.12f;
        keys[2].time = 0.19f;
        AnimationUtility.SetObjectReferenceCurve(clip, bindings[0], keys);
        var settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.stopTime = 0.26f;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
    }

    static Dictionary<Sprite, Sprite> SpriteMap(string a, string b)
    {
        var map = new Dictionary<Sprite, Sprite>();
        var dst = new Dictionary<string, Sprite>();
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(b))
            if (o is Sprite s) dst[s.name] = s;
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(a))
            if (o is Sprite s && dst.TryGetValue(s.name, out var n))
                map[s] = n;
        if (map.Count < 30)
            throw new System.Exception("Sprite remap incomplete: " + map.Count);
        return map;
    }

    static void AssignSandboxController()
    {
        var ctrl = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(CtrlDst);
        var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(Sandbox);
        var player = GameObject.Find("Player");
        var visual = player.GetComponent<GameplayPrototype.PlayerVisual>();
        var so = new SerializedObject(visual);
        so.FindProperty("bodyController").objectReferenceValue = ctrl;
        so.ApplyModifiedPropertiesWithoutUndo();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
    }
}
