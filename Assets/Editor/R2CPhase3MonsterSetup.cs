using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using GameplayPrototype;

/// <summary>
/// R2C phase 3: import the approved regular monsters. Does not import Cyclops and does not save a scene.
/// </summary>
public static class R2CPhase3MonsterSetup
{
    const string Src = "Docs/ReferenceRebuild/R2/R2C_OriginalHero/unity_export_monsters";
    const string Dst = "Assets/Art/Rebuild/R2C/Monsters";
    const string RosterPath = "Assets/Resources/R2C/R2CMonsterRoster.asset";

    [MenuItem("Game01/R2C/Phase 3 Import Monsters")]
    public static void Import()
    {
        var manifest = ReadManifest();
        var frames = ReadFrames();
        Directory.CreateDirectory(Dst);
        Directory.CreateDirectory("Assets/Resources/R2C");
        var used = new HashSet<string>();
        foreach (var f in frames)
            used.Add(f.file);
        foreach (var file in used)
        {
            if (file.StartsWith("Cyclops/")) continue;
            if (!manifest.TryGetValue(file, out var spec))
                throw new System.Exception("[R2C phase 3] no manifest row for " + file);
            CopyChecked(Src + "/" + file, Dst + "/" + file.Replace('/', '_'), spec);
        }
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        var sprites = new Dictionary<string, Dictionary<string, Sprite>>();
        foreach (var file in used)
        {
            if (file.StartsWith("Cyclops/")) continue;
            var spec = manifest[file];
            string path = Dst + "/" + file.Replace('/', '_');
            var mine = new List<Frame>();
            foreach (var f in frames)
                if (f.file == file) mine.Add(f);
            Slice(path, spec, mine);
            sprites[file] = LoadSprites(path);
        }

        var defs = new List<R2CMonsterDef>();
        foreach (var spec in BodySpecs())
        {
            string sheet = spec.folder + "/" + spec.folder + "_Sheet.png";
            if (spec.folder == "GoblinArcher") sheet = "GoblinArcher/GoblinArcher_Sheet.png";
            var ctrl = BuildController(spec.folder, sprites[sheet]);
            var def = spec.def;
            def.controller = ctrl;
            if (spec.bolt != null)
                def.bolt = OrderedDirs(sprites[spec.bolt], spec.boltPrefix);
            defs.Add(def);
        }

        var roster = AssetDatabase.LoadAssetAtPath<R2CMonsterRoster>(RosterPath);
        if (roster == null)
        {
            roster = ScriptableObject.CreateInstance<R2CMonsterRoster>();
            AssetDatabase.CreateAsset(roster, RosterPath);
        }
        roster.monsters = defs.ToArray();
        EditorUtility.SetDirty(roster);
        AssetDatabase.SaveAssets();
        Debug.Log("[R2C phase 3] monsters imported. count=" + defs.Count + " goblinIdle=" + (sprites["Goblin/Goblin_Sheet.png"].ContainsKey("Goblin_Idle_Down_0")));
    }

    struct Spec
    {
        public int w, h, cols, rows;
        public Vector2 pivot;
    }

    class Frame
    {
        public string sprite, file, anim, view, mode;
        public int col, row, frame, fps;
    }

    struct Body
    {
        public string folder;
        public string bolt;
        public string boltPrefix;
        public R2CMonsterDef def;
    }

    static Body[] BodySpecs()
    {
        var walker = new Vector2(0.625f, 0.375f);
        var rider = new Vector2(1f, 0.5f);
        return new[]
        {
            BodyOf("Goblin", "Goblin Lv.3", 60, 4, 2.05f, false, false, walker, 0, null, null),
            Arrow("GoblinArcher", "Goblin Archer Lv.3", 60, 4, 1.85f, walker),
            BodyOf("Satyr", "Satyr Lv.3", 60, 4, 2.30f, false, false, walker, 0, null, null),
            Leaf("Dryad", "Dryad Lv.3", 60, 4, 1.80f, walker, new Vector2(0f, 0.25f), new Vector2(0f, 1.125f), new Vector2(-0.4375f, 0.375f), 5.5f),
            Leaf("DryadRider", "Dryad Rider Lv.4", 78, 5, 2.67f, rider, new Vector2(0f, 0.5625f), new Vector2(0f, 1.5f), new Vector2(-0.625f, 0.6875f), 6f),
            BodyOf("Gobrider", "Gobrider Lv.5", 90, 6, 1.70f, true, false, rider, 10, null, null),
            BodyOf("GobriderElite", "Elite Gobrider Lv.6", 120, 8, 1.60f, true, false, rider, 14, null, null),
            BodyOf("GobriderWarlord", "Warlord Lv.7", 180, 10, 1.50f, true, true, new Vector2(1.25f, 0.625f), 16, null, null),
        };
    }

    static Body Arrow(string folder, string label, int hp, int dmg, float speed, Vector2 col)
    {
        var b = BodyOf(folder, label, hp, dmg, speed, false, false, col, 0, "GoblinArcher/GoblinArcher_Arrow.png", "GoblinArcher_Arrow_");
        b.def.aimLine = true;
        b.def.standOff = 2.4f;
        b.def.shotRange = 7f;
        b.def.shotSpeed = 9f;
        b.def.spawnDown = new Vector2(0f, 0.125f);
        b.def.spawnUp = new Vector2(0f, 0.625f);
        b.def.spawnSide = new Vector2(-0.625f, 0.4375f);
        return b;
    }

    static Body Leaf(string folder, string label, int hp, int dmg, float speed, Vector2 col, Vector2 down, Vector2 up, Vector2 side, float range)
    {
        var b = BodyOf(folder, label, hp, dmg, speed, folder == "DryadRider", false, col, 0, "Dryad/Dryad_LeafBolt.png", "Dryad_LeafBolt_");
        b.def.standOff = 2.2f;
        b.def.shotRange = range;
        b.def.shotSpeed = 5f;
        b.def.spawnDown = down;
        b.def.spawnUp = up;
        b.def.spawnSide = side;
        return b;
    }

    static Body BodyOf(string folder, string label, int hp, int dmg, float speed, bool rider, bool boss, Vector2 col, int charge, string bolt, string prefix)
    {
        return new Body
        {
            folder = folder,
            bolt = bolt,
            boltPrefix = prefix,
            def = new R2CMonsterDef
            {
                label = label,
                hp = hp,
                damage = dmg,
                speed = speed,
                aggro = 5.2f,
                meleeRange = 0.75f,
                cooldown = rider ? 1.7f : 1.35f,
                colliderSize = col,
                rider = rider,
                bossBar = boss,
                chargeDamage = charge,
                ranged = bolt != null
            }
        };
    }

    static Dictionary<string, Spec> ReadManifest()
    {
        var d = new Dictionary<string, Spec>();
        foreach (var line in File.ReadAllLines(Src + "/MONSTERS_MANIFEST.csv"))
        {
            if (line.Length == 0 || line.StartsWith("file,")) continue;
            var p = line.Split(',');
            d[p[0]] = new Spec
            {
                w = int.Parse(p[1]),
                h = int.Parse(p[2]),
                cols = int.Parse(p[3]),
                rows = int.Parse(p[4]),
                pivot = new Vector2(float.Parse(p[7], System.Globalization.CultureInfo.InvariantCulture), float.Parse(p[8], System.Globalization.CultureInfo.InvariantCulture))
            };
        }
        return d;
    }

    static List<Frame> ReadFrames()
    {
        var list = new List<Frame>();
        foreach (var line in File.ReadAllLines(Src + "/MONSTERS_FRAMES.csv"))
        {
            if (line.Length == 0 || line.StartsWith("sprite,")) continue;
            var p = line.Split(',');
            if (p[1].StartsWith("Cyclops/")) continue;
            list.Add(new Frame
            {
                sprite = p[0],
                file = p[1],
                col = int.Parse(p[2]),
                row = int.Parse(p[3]),
                anim = p[8],
                view = p[9],
                frame = int.Parse(p[10]),
                fps = int.Parse(p[12]),
                mode = p[13]
            });
        }
        return list;
    }

    static void CopyChecked(string src, string dst, Spec spec)
    {
        var b = File.ReadAllBytes(src);
        int w = (b[16] << 24) | (b[17] << 16) | (b[18] << 8) | b[19];
        int h = (b[20] << 24) | (b[21] << 16) | (b[22] << 8) | b[23];
        if (w != spec.w * spec.cols || h != spec.h * spec.rows)
            throw new System.Exception("[R2C phase 3] " + src + " is " + w + "x" + h + ", manifest " + (spec.w * spec.cols) + "x" + (spec.h * spec.rows));
        File.Copy(src, dst, true);
    }

    static void Slice(string path, Spec spec, List<Frame> frames)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(path);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Multiple;
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
        imp.SaveAndReimport();
        int texH = spec.h * spec.rows;
        var rects = new List<SpriteRect>();
        foreach (var f in frames)
        {
            rects.Add(new SpriteRect
            {
                name = f.sprite,
                rect = new Rect(f.col * spec.w, texH - (f.row + 1) * spec.h, spec.w, spec.h),
                alignment = SpriteAlignment.Custom,
                pivot = spec.pivot,
                spriteID = GUID.Generate()
            });
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
    }

    static Dictionary<string, Sprite> LoadSprites(string path)
    {
        var d = new Dictionary<string, Sprite>();
        foreach (var o in AssetDatabase.LoadAllAssetsAtPath(path))
            if (o is Sprite s) d[s.name] = s;
        return d;
    }

    static Sprite[] OrderedDirs(Dictionary<string, Sprite> d, string prefix)
    {
        var dirs = new[] { "R", "UR", "U", "UL", "L", "DL", "D", "DR" };
        var a = new Sprite[dirs.Length];
        for (int i = 0; i < dirs.Length; i++)
        {
            string name = prefix + dirs[i];
            if (!d.TryGetValue(name, out a[i]))
                throw new System.Exception("[R2C phase 3] missing " + name);
        }
        return a;
    }

    static AnimatorController BuildController(string folder, Dictionary<string, Sprite> sprites)
    {
        string dir = Dst + "/Animators";
        Directory.CreateDirectory(dir);
        string path = dir + "/" + folder + ".controller";
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
        var ctrl = AnimatorController.CreateAnimatorControllerAtPath(path);
        var sm = ctrl.layers[0].stateMachine;
        AnimatorState idle = null;
        foreach (var anim in new[] { "Idle", "Walk", "Attack", "Hurt", "Death" })
        {
            foreach (var view in new[] { "Down", "Up", "Side" })
            {
                var frames = new List<Sprite>();
                for (int i = 0; i < 8; i++)
                {
                    string name = folder + "_" + anim + "_" + view + "_" + i;
                    if (!sprites.TryGetValue(name, out var sprite)) break;
                    frames.Add(sprite);
                }
                if (frames.Count == 0) throw new System.Exception("[R2C phase 3] no frames " + folder + " " + anim + " " + view);
                var clip = Clip(folder, anim, view, frames);
                var state = sm.AddState(anim + "_" + view);
                state.motion = clip;
                if (anim == "Idle" && view == "Down") idle = state;
            }
        }
        sm.defaultState = idle;
        EditorUtility.SetDirty(ctrl);
        return ctrl;
    }

    static AnimationClip Clip(string folder, string anim, string view, List<Sprite> frames)
    {
        string dir = Dst + "/Animations";
        Directory.CreateDirectory(dir);
        string path = dir + "/" + folder + "_" + anim + "_" + view + ".anim";
        if (File.Exists(path)) AssetDatabase.DeleteAsset(path);
        var clip = new AnimationClip { name = anim + "_" + view, frameRate = 60 };
        int fps = anim == "Idle" ? 6 : anim == "Walk" || anim == "Death" ? 8 : 10;
        bool loop = anim == "Idle" || anim == "Walk";
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
