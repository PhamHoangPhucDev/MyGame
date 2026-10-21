using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using GameplayPrototype;

/// <summary>Imports one R2C class body, gear and kit. Does not save a scene.</summary>
public static class R2CPhase4ClassSetup
{
    [MenuItem("Game01/R2C/Phase 4 Import Brom")]
    public static void ImportBrom() => Import("Brom", "BROM_MANIFEST.csv");

    [MenuItem("Game01/R2C/Phase 4 Import Lyra")]
    public static void ImportLyra() => Import("Lyra", "LYRA_MANIFEST.csv");

    static void Import(string className, string manifestName)
    {
        string src = "Docs/ReferenceRebuild/R2/R2C_OriginalHero/unity_export_" + className.ToLowerInvariant();
        string dst = "Assets/Art/Rebuild/R2C/" + className;
        var spec = ReadManifest(src + "/" + manifestName);
        foreach (var kv in spec)
            CopyChecked(src + "/" + kv.Key, dst + "/" + kv.Key, kv.Value);
        foreach (var png in Directory.GetFiles(src, "*.png", SearchOption.AllDirectories))
        {
            string rel = png.Substring(src.Length + 1).Replace('\\', '/');
            if (spec.ContainsKey(rel)) continue;
            CopyLoose(png, dst + "/" + rel);
        }
        string anchors = className + "_Anchors.csv";
        File.Copy(src + "/" + anchors, dst + "/" + anchors, true);
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);

        var sprites = new Dictionary<string, Sprite[]>();
        foreach (var kv in spec)
            sprites[kv.Key] = Slice(dst + "/" + kv.Key, kv.Value, className);
        foreach (var png in Directory.GetFiles(dst, "*.png", SearchOption.AllDirectories))
        {
            string rel = png.Substring(dst.Length + 1).Replace('\\', '/');
            if (sprites.ContainsKey(rel)) continue;
            sprites[rel] = Slice(png.Replace('\\', '/'), new Cell(32, 32, 1, 1, new Vector2(0.5f, 0.0625f)), className);
        }

        var ctrl = BuildBody(dst, className, sprites);
        var equip = BuildGear(dst, className, sprites, anchors);
        var kit = BuildKit(className, ctrl, equip, sprites);
        Debug.Log("[R2C phase 4] " + className + " imported. states=" + (ctrl != null) + " kit=" + (kit != null));
    }

    struct Cell
    {
        public int w, h, cols, rows;
        public Vector2 pivot;
        public Cell(int w, int h, int cols, int rows, Vector2 pivot)
        {
            this.w = w; this.h = h; this.cols = cols; this.rows = rows; this.pivot = pivot;
        }
    }

    static Dictionary<string, Cell> ReadManifest(string path)
    {
        var d = new Dictionary<string, Cell>();
        foreach (var line in File.ReadAllLines(path))
        {
            if (line.Length == 0 || line.StartsWith("file,")) continue;
            var p = line.Split(',');
            d[p[0]] = new Cell(int.Parse(p[1]), int.Parse(p[2]), int.Parse(p[3]), int.Parse(p[4]),
                new Vector2(float.Parse(p[7], System.Globalization.CultureInfo.InvariantCulture), float.Parse(p[8], System.Globalization.CultureInfo.InvariantCulture)));
        }
        return d;
    }

    static void CopyChecked(string src, string dst, Cell spec)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dst));
        var b = File.ReadAllBytes(src);
        int w = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
        int h = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
        if (w != spec.w * spec.cols || h != spec.h * spec.rows)
            throw new System.Exception("[R2C phase 4] " + src + " is " + w + "x" + h);
        File.Copy(src, dst, true);
    }

    static void CopyLoose(string src, string dst)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(dst));
        File.Copy(src, dst, true);
    }

    static Sprite[] Slice(string path, Cell spec, string className)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = spec.cols * spec.rows > 1 ? SpriteImportMode.Multiple : SpriteImportMode.Single;
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
        s.spriteAlignment = (int)SpriteAlignment.Custom;
        s.spritePivot = spec.pivot;
        imp.SetTextureSettings(s);
        if (spec.cols * spec.rows == 1)
        {
            imp.SaveAndReimport();
            return new[] { AssetDatabase.LoadAssetAtPath<Sprite>(path) };
        }
        imp.SaveAndReimport();
        int texH = spec.h * spec.rows;
        string file = Path.GetFileNameWithoutExtension(path);
        bool body = file == className + "_Sheet";
        string[] anims = { "Idle", "Walk", "Attack" };
        string[] views = { "Down", "Up", "Side" };
        var rects = new List<SpriteRect>();
        for (int row = 0; row < spec.rows; row++)
        {
            int cols = spec.cols;
            if (body && row >= 6) cols = 3;
            for (int col = 0; col < cols; col++)
            {
                string name = file + "_" + col + "_" + row;
                if (body)
                    name = className + "_" + anims[row / 3] + "_" + views[row % 3] + "_" + col;
                rects.Add(new SpriteRect
                {
                    name = name,
                    rect = new Rect(col * spec.w, texH - (row + 1) * spec.h, spec.w, spec.h),
                    alignment = SpriteAlignment.Custom,
                    pivot = spec.pivot,
                    spriteID = GUID.Generate()
                });
            }
        }
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
        var map = new Dictionary<string, Sprite>();
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
            if (o is Sprite sp) map[sp.name] = sp;
        var ordered = new Sprite[rects.Count];
        for (int i = 0; i < rects.Count; i++) ordered[i] = map[rects[i].name];
        return ordered;
    }

    static AnimatorController BuildBody(string dst, string className, Dictionary<string, Sprite[]> sprites)
    {
        string dir = dst + "/Animators";
        Directory.CreateDirectory(dir);
        string path = dir + "/" + className + ".controller";
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        var sm = ctrl.layers[0].stateMachine;
        AnimatorState idle = null;
        foreach (var anim in new[] { "Idle", "Walk", "Attack", "Hurt", "Death" })
        {
            foreach (var view in new[] { "Down", "Up", "Side" })
            {
                var frames = Frames(className, anim, view, sprites);
                var clip = Clip(dst, className, anim, view, frames);
                var state = sm.AddState("Hero_" + anim + "_" + view);
                state.motion = clip;
                if (anim == "Idle" && view == "Down") idle = state;
            }
        }
        sm.defaultState = idle;
        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    static List<Sprite> Frames(string className, string anim, string view, Dictionary<string, Sprite[]> sprites)
    {
        var list = new List<Sprite>();
        if (anim == "Idle" || anim == "Walk" || anim == "Attack")
        {
            int count = anim == "Attack" ? 3 : 4;
            var sheet = sprites[className + "_Sheet.png"];
            string prefix = className + "_" + anim + "_" + view + "_";
            foreach (var sp in sheet)
                if (sp.name.StartsWith(prefix)) list.Add(sp);
            list.Sort((a, b) => a.name.CompareTo(b.name));
            if (list.Count > count) list.RemoveRange(count, list.Count - count);
        }
        else
        {
            int count = anim == "Hurt" ? 2 : 3;
            for (int i = 0; i < count; i++)
            {
                string key = "Extra/" + className + "_" + anim + "_" + view + "_" + i + ".png";
                if (!sprites.TryGetValue(key, out var arr) || arr.Length == 0)
                    throw new System.Exception("[R2C phase 4] missing " + key);
                list.Add(arr[0]);
            }
        }
        if (list.Count == 0) throw new System.Exception("[R2C phase 4] no frames " + className + " " + anim + " " + view);
        return list;
    }

    static AnimationClip Clip(string dst, string className, string anim, string view, List<Sprite> frames)
    {
        string dir = dst + "/Animations";
        Directory.CreateDirectory(dir);
        string path = dir + "/" + className + "_" + anim + "_" + view + ".anim";
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
        int fps = anim == "Idle" ? 6 : anim == "Walk" || anim == "Death" ? 8 : 10;
        bool loop = anim == "Idle" || anim == "Walk";
        var clip = new AnimationClip { name = "Hero_" + anim + "_" + view, frameRate = 60 };
        var keys = new ObjectReferenceKeyframe[loop ? frames.Count : frames.Count + 1];
        for (int i = 0; i < frames.Count; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / (float)fps, value = frames[i] };
        if (!loop)
            keys[frames.Count] = new ObjectReferenceKeyframe { time = frames.Count / (float)fps, value = frames[frames.Count - 1] };
        if (anim == "Attack")
        {
            float t1 = className == "Lyra" ? 0.06f : 0.16f;
            float t2 = className == "Lyra" ? 0.14f : 0.32f;
            float t3 = className == "Lyra" ? 0.26f : 0.42f;
            keys = new ObjectReferenceKeyframe[4];
            keys[0] = new ObjectReferenceKeyframe { time = 0f, value = frames[0] };
            keys[1] = new ObjectReferenceKeyframe { time = t1, value = frames[1] };
            keys[2] = new ObjectReferenceKeyframe { time = t2, value = frames[Mathf.Min(2, frames.Count - 1)] };
            keys[3] = new ObjectReferenceKeyframe { time = t3, value = frames[Mathf.Min(2, frames.Count - 1)] };
        }
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
        var st = AnimationUtility.GetAnimationClipSettings(clip);
        st.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, st);
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    static EquipmentSet BuildGear(string dst, string className, Dictionary<string, Sprite[]> sprites, string anchors)
    {
        string path = "Assets/Art/Rebuild/R2C/ClassKits/EquipmentSet_" + className + ".asset";
        Directory.CreateDirectory("Assets/Art/Rebuild/R2C/ClassKits");
        var set = AssetDatabase.LoadAssetAtPath<EquipmentSet>(path);
        if (set == null)
        {
            set = ScriptableObject.CreateInstance<EquipmentSet>();
            AssetDatabase.CreateAsset(set, path);
        }
        set.anchorTable = AssetDatabase.LoadAssetAtPath<TextAsset>(dst + "/" + anchors);
        if (className == "Lyra")
        {
            set.weaponPoses = sprites["Gear/Lyra_Bow.png"];
            set.shieldViews = new Sprite[0];
        }
        else
        {
            set.weaponPoses = sprites["Gear/" + className + "_Hammer.png"];
            set.shieldViews = sprites["Gear/" + className + "_Shield.png"];
        }
        set.hand = sprites["Gear/" + className + "_Hand.png"][0];
        set.slashFrames = sprites["Gear/" + className + "_ComboVfx.png"];
        set.replaceLegacyRSlash = true;
        EditorUtility.SetDirty(set);
        return set;
    }

    static ClassKit BuildKit(string className, AnimatorController ctrl, EquipmentSet equip, Dictionary<string, Sprite[]> sprites)
    {
        Directory.CreateDirectory("Assets/Resources/R2C");
        string path = "Assets/Resources/R2C/ClassKit_" + className + ".asset";
        var kit = AssetDatabase.LoadAssetAtPath<ClassKit>(path);
        if (kit == null)
        {
            kit = ScriptableObject.CreateInstance<ClassKit>();
            AssetDatabase.CreateAsset(kit, path);
        }
        if (className == "Lyra")
            kit.FillLyra(
                sprites["Gear/Lyra_Arrow.png"],
                sprites["Gear/Lyra_ArrowHit.png"],
                sprites["Skills/Lyra_FanBurst.png"],
                sprites["Skills/Lyra_LeafBurst.png"],
                sprites["Skills/Lyra_VineSnare.png"],
                sprites["Skills/Lyra_RainZone.png"],
                sprites["Skills/Lyra_RainArrow.png"],
                sprites["Skills/Lyra_WindAura.png"],
                sprites["Skills/Lyra_DodgePuff.png"],
                sprites["Skills/Lyra_Beam.png"]);
        else
            kit.FillBrom(
                sprites["Skills/Brom_GroundSlam.png"],
                sprites["Skills/Brom_ChargeDust.png"],
                sprites["Skills/Brom_Roar.png"],
                sprites["Skills/Brom_TauntMark.png"],
                Join(sprites["Skills/Brom_IronWall_Back.png"], sprites["Skills/Brom_IronWall_Front.png"]),
                sprites["Skills/Brom_Quake.png"]);
        kit.bodyController = ctrl;
        kit.equipment = equip;
        EditorUtility.SetDirty(kit);
        AssetDatabase.SaveAssets();
        return kit;
    }

    static Sprite[] Join(Sprite[] a, Sprite[] b)
    {
        var r = new Sprite[a.Length + b.Length];
        a.CopyTo(r, 0);
        b.CopyTo(r, a.Length);
        return r;
    }
}
