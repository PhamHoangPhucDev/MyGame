using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEditor.SceneManagement;
using UnityEngine;
using GameplayPrototype;

/// <summary>
/// Imports Goblin Camp phase-4 art into a test scene. Does not save the main gameplay scene.
/// </summary>
public static class Phase4ArtSetup
{
    const string SourceName = "Goblin_Camp_Phase4";
    const string ArtRoot = "Assets/Art/GoblinCampPhase4";
    const string MainScene = "Assets/Scenes/GameplayPrototype.unity";
    const string TestScene = "Assets/Scenes/GameplayPrototype_Phase4Test.unity";

    [MenuItem("Tools/Gameplay Prototype/Build Phase 4 Test Scene")]
    public static void Build()
    {
        CopySources();
        var hero = SliceSheet("Hero_Sheet.png", 32, 32, 2f / 32f, new[]
        {
            Row("Hero_Idle_Down", 4), Row("Hero_Idle_Up", 4), Row("Hero_Idle_Side", 4),
            Row("Hero_Walk_Down", 4), Row("Hero_Walk_Up", 4), Row("Hero_Walk_Side", 4),
            Row("Hero_Attack_Down", 3), Row("Hero_Attack_Up", 3), Row("Hero_Attack_Side", 3)
        });
        var goblin = SliceSheet("Goblin_Sheet.png", 26, 22, 2f / 22f, new[]
        {
            Row("Idle", 2), Row("Walk", 4), Row("Attack", 3), Row("Hurt", 1)
        });
        var rider = SliceSheet("Gobrider_Sheet.png", 30, 24, 1f / 24f, new[]
        {
            Row("Idle", 2), Row("Walk", 4), Row("Attack", 3), Row("Hurt", 1)
        });
        ImportSingle("Hero_Idle.png", new Vector2(0.5f, 2f / 20f));
        ImportSingle("Goblin_Idle.png", new Vector2(0.5f, 0f));
        ImportSingle("Gobrider_Idle.png", new Vector2(0.5f, 0f));
        ImportSingle("Tree_Forest_A.png", new Vector2(0.5f, 0f));
        ImportSingle("Tree_Forest_B.png", new Vector2(0.5f, 0f));
        ImportSingle("Bone_Pile.png", new Vector2(0.5f, 0f));
        ImportSingle("Brazier_Bowl.png", new Vector2(0.5f, 0f));
        ImportSingle("Goblin_Flag_Tattered.png", new Vector2(0.5f, 0f));
        ImportSingle("Wooden_Crate_Stack.png", new Vector2(0.5f, 0f));
        ImportSingle("Rock_Large.png", new Vector2(0.5f, 0f));
        ImportSingle("Rock_Small.png", new Vector2(0.5f, 0f));
        ImportSingle("Shadow_Large.png", new Vector2(0.5f, 0.5f));
        ImportSingle("Shadow_Medium.png", new Vector2(0.5f, 0.5f));
        ImportSingle("Shadow_Small.png", new Vector2(0.5f, 0.5f));
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        var heroController = BuildController("Hero", hero, new[]
        {
            Clip("Hero_Idle_Down", 6, true), Clip("Hero_Idle_Up", 6, true), Clip("Hero_Idle_Side", 6, true),
            Clip("Hero_Walk_Down", 8, true), Clip("Hero_Walk_Up", 8, true), Clip("Hero_Walk_Side", 8, true),
            Clip("Hero_Attack_Down", 14, false), Clip("Hero_Attack_Up", 14, false), Clip("Hero_Attack_Side", 14, false)
        });
        var goblinController = BuildController("Goblin", goblin, new[]
        {
            Clip("Idle", 4, true), Clip("Walk", 8, true), Clip("Attack", 10, false), Clip("Hurt", 8, false)
        });
        var riderController = BuildController("Gobrider", rider, new[]
        {
            Clip("Idle", 4, true), Clip("Walk", 8, true), Clip("Attack", 10, false), Clip("Hurt", 8, false)
        });

        SavePropPrefab("Tree_Forest_A", LoadSprite("Tree_Forest_A.png"), new Vector2(0.55f, 0.4f), new Vector2(0f, 0.22f), "Shadow_Large.png");
        SavePropPrefab("Tree_Forest_B", LoadSprite("Tree_Forest_B.png"), new Vector2(0.5f, 0.4f), new Vector2(0f, 0.2f), "Shadow_Large.png");
        SavePropPrefab("Bone_Pile", LoadSprite("Bone_Pile.png"), Vector2.zero, Vector2.zero, "Shadow_Small.png");
        SavePropPrefab("Brazier_Bowl", LoadSprite("Brazier_Bowl.png"), new Vector2(0.55f, 0.35f), new Vector2(0f, 0.16f), "Shadow_Medium.png");
        SavePropPrefab("Goblin_Flag", LoadSprite("Goblin_Flag_Tattered.png"), new Vector2(0.2f, 0.28f), new Vector2(0f, 0.12f), "Shadow_Small.png");
        SavePropPrefab("Wooden_Crate_Stack", LoadSprite("Wooden_Crate_Stack.png"), new Vector2(0.9f, 0.4f), new Vector2(0f, 0.22f), "Shadow_Medium.png");
        SavePropPrefab("Rock_Large", LoadSprite("Rock_Large.png"), new Vector2(0.7f, 0.35f), new Vector2(0f, 0.16f), "Shadow_Small.png");
        SavePropPrefab("Rock_Small", LoadSprite("Rock_Small.png"), new Vector2(0.45f, 0.28f), new Vector2(0f, 0.12f), "Shadow_Small.png");

        if (!File.Exists(TestScene))
            AssetDatabase.CopyAsset(MainScene, TestScene);
        var scene = EditorSceneManager.OpenScene(TestScene, OpenSceneMode.Single);
        DressScene(heroController, goblinController, riderController, hero["Hero_Idle_Down"][0]);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[Phase4] Test scene ready. Hero opaque height is 20px at PPU 16 (1.25 units, about 1/13 of the 16-unit view). Trees are 78px and 68px tall.");
    }

    struct RowDef
    {
        public string name;
        public int frames;
    }

    struct ClipDef
    {
        public string name;
        public int fps;
        public bool loop;
    }

    static RowDef Row(string name, int frames) => new RowDef { name = name, frames = frames };
    static ClipDef Clip(string name, int fps, bool loop) => new ClipDef { name = name, fps = fps, loop = loop };

    static void CopySources()
    {
        string project = Directory.GetParent(Application.dataPath).FullName;
        string src = Path.Combine(project, "IncomingAssets", SourceName);
        string art = Path.Combine(Application.dataPath, "Art/GoblinCampPhase4");
        Directory.CreateDirectory(art);
        foreach (var file in Directory.GetFiles(src, "*.png"))
        {
            string dest = Path.Combine(ArtRoot, Path.GetFileName(file));
            if (!File.Exists(dest))
                File.Copy(file, dest);
        }
        AssetDatabase.Refresh();
    }

    static Dictionary<string, Sprite[]> SliceSheet(string file, int cellX, int cellY, float pivotY, RowDef[] rows)
    {
        string path = $"{ArtRoot}/{file}";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        int height = tex.height;
        var metas = new List<SpriteMetaData>();
        int rowIndex = 0;
        foreach (var row in rows)
        {
            int y = height - (rowIndex + 1) * cellY;
            for (int col = 0; col < row.frames; col++)
            {
                metas.Add(new SpriteMetaData
                {
                    name = $"{Path.GetFileNameWithoutExtension(file)}_{row.name}_{col}",
                    rect = new Rect(col * cellX, y, cellX, cellY),
                    alignment = (int)SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, pivotY),
                    border = Vector4.zero
                });
            }
            rowIndex++;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.isReadable = true;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.filterMode = FilterMode.Point;
        importer.SetTextureSettings(settings);
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        var data = factory.GetSpriteEditorDataProviderFromObject(importer);
        data.InitSpriteEditorDataProvider();
        var spriteRects = new SpriteRect[metas.Count];
        for (int i = 0; i < metas.Count; i++)
        {
            spriteRects[i] = new SpriteRect
            {
                name = metas[i].name,
                rect = metas[i].rect,
                alignment = SpriteAlignment.Custom,
                pivot = metas[i].pivot,
                spriteID = GUID.Generate()
            };
        }
        data.SetSpriteRects(spriteRects);
        data.Apply();
        importer.SaveAndReimport();

        var loaded = new Dictionary<string, Sprite>();
        foreach (var asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
        {
            if (asset is Sprite sprite)
                loaded[sprite.name] = sprite;
        }

        var result = new Dictionary<string, Sprite[]>();
        foreach (var row in rows)
        {
            var frames = new Sprite[row.frames];
            for (int i = 0; i < row.frames; i++)
                frames[i] = loaded[$"{Path.GetFileNameWithoutExtension(file)}_{row.name}_{i}"];
            result[row.name] = frames;
        }
        return result;
    }

    static void ImportSingle(string file, Vector2 pivot)
    {
        string path = $"{ArtRoot}/{file}";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        settings.filterMode = FilterMode.Point;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    static AnimatorController BuildController(string name, Dictionary<string, Sprite[]> frames, ClipDef[] clips)
    {
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Art/GoblinCampPhase4/Animations"));
        string controllerPath = $"{ArtRoot}/Animators/{name}.controller";
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Art/GoblinCampPhase4/Animators"));
        if (File.Exists(controllerPath))
            AssetDatabase.DeleteAsset(controllerPath);
        var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
        var machine = controller.layers[0].stateMachine;
        AnimatorState first = null;
        foreach (var clipDef in clips)
        {
            var clip = BuildClip($"{ArtRoot}/Animations/{name}_{clipDef.name}.anim", frames[clipDef.name], clipDef.fps, clipDef.loop);
            var state = machine.AddState(clipDef.name);
            state.motion = clip;
            if (first == null)
                first = state;
        }
        machine.defaultState = first;
        return controller;
    }

    static AnimationClip BuildClip(string path, Sprite[] sprites, int fps, bool loop)
    {
        if (File.Exists(path))
            AssetDatabase.DeleteAsset(path);
        var clip = new AnimationClip { frameRate = fps };
        var keys = new ObjectReferenceKeyframe[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / (float)fps, value = sprites[i] };
        var binding = new EditorCurveBinding
        {
            type = typeof(SpriteRenderer),
            path = "",
            propertyName = "m_Sprite"
        };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        var serialized = new SerializedObject(clip);
        var settings = serialized.FindProperty("m_AnimationClipSettings");
        settings.FindPropertyRelative("m_LoopTime").boolValue = loop;
        settings.FindPropertyRelative("m_StopTime").floatValue = sprites.Length / (float)fps;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(clip, path);
        return clip;
    }

    static Sprite LoadSprite(string file)
    {
        var assets = AssetDatabase.LoadAllAssetRepresentationsAtPath($"{ArtRoot}/{file}");
        foreach (var asset in assets)
        {
            if (asset is Sprite sprite)
                return sprite;
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtRoot}/{file}");
    }

    static void SavePropPrefab(string name, Sprite sprite, Vector2 collider, Vector2 colliderOffset, string shadowFile)
    {
        var root = new GameObject(name);
        var renderer = root.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        var sorter = root.AddComponent<SpriteSorter>();
        sorter.baseOrder = 0;
        if (collider != Vector2.zero)
        {
            var box = root.AddComponent<BoxCollider2D>();
            box.size = collider;
            box.offset = colliderOffset;
        }
        var shadowGo = new GameObject("ContactShadow");
        shadowGo.transform.SetParent(root.transform, false);
        shadowGo.transform.localPosition = new Vector3(0f, 0.05f, 0f);
        var shadow = shadowGo.AddComponent<SpriteRenderer>();
        shadow.sprite = LoadSprite(shadowFile);
        shadow.sortingLayerName = "Ground";
        shadow.sortingOrder = 3;
        Directory.CreateDirectory(Path.Combine(Application.dataPath, "Art/GoblinCampPhase4/Prefabs"));
        string path = $"{ArtRoot}/Prefabs/{name}.prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
    }

    static void DressScene(AnimatorController hero, AnimatorController goblin, AnimatorController rider, Sprite heroIdle)
    {
        var player = GameObject.FindGameObjectWithTag("Player");
        var visual = player.GetComponent<PlayerVisual>();
        visual.EnsureHierarchy();
        var so = new SerializedObject(visual);
        so.FindProperty("bodyController").objectReferenceValue = hero;
        so.ApplyModifiedPropertiesWithoutUndo();
        var body = player.transform.Find("Visual").GetComponent<SpriteRenderer>();
        body.sprite = heroIdle;

        var spawner = Object.FindAnyObjectByType<EnemySpawner>();
        var spawnerSo = new SerializedObject(spawner);
        spawnerSo.FindProperty("goblinAnimator").objectReferenceValue = goblin;
        spawnerSo.FindProperty("gobriderAnimator").objectReferenceValue = rider;
        spawnerSo.ApplyModifiedPropertiesWithoutUndo();

        var props = GameObject.Find("ForestProps").transform;
        int tree = 0;
        foreach (Transform child in props)
        {
            if (child.name == "Tree")
            {
                bool alt = tree % 2 == 1;
                ApplySprite(child, LoadSprite(alt ? "Tree_Forest_B.png" : "Tree_Forest_A.png"),
                    alt ? new Vector2(0.5f, 0.4f) : new Vector2(0.55f, 0.4f),
                    alt ? new Vector2(0f, 0.2f) : new Vector2(0f, 0.22f),
                    "Shadow_Large.png");
                tree++;
            }
            else if (child.name == "Flag")
                ApplySprite(child, LoadSprite("Goblin_Flag_Tattered.png"), new Vector2(0.2f, 0.28f), new Vector2(0f, 0.12f), "Shadow_Small.png");
            else if (child.name == "Campfire")
                ApplySprite(child, LoadSprite("Brazier_Bowl.png"), new Vector2(0.55f, 0.35f), new Vector2(0f, 0.16f), "Shadow_Medium.png");
            else if (child.name == "Crate")
                ApplySprite(child, LoadSprite("Wooden_Crate_Stack.png"), new Vector2(0.9f, 0.4f), new Vector2(0f, 0.22f), "Shadow_Medium.png");
            else if (child.name == "Bones")
                ApplySprite(child, LoadSprite("Bone_Pile.png"), Vector2.zero, Vector2.zero, "Shadow_Small.png");
            else if (child.name == "Rock")
            {
                bool large = child.GetSiblingIndex() % 2 == 0;
                ApplySprite(child, LoadSprite(large ? "Rock_Large.png" : "Rock_Small.png"),
                    large ? new Vector2(0.7f, 0.35f) : new Vector2(0.45f, 0.28f),
                    large ? new Vector2(0f, 0.16f) : new Vector2(0f, 0.12f),
                    "Shadow_Small.png");
            }
        }

        var cam = Camera.main.GetComponent<UnityEngine.Rendering.Universal.PixelPerfectCamera>();
        Debug.Log($"[Phase4] PPC {cam.refResolutionX}x{cam.refResolutionY} ppu {cam.assetsPPU} trees {tree}");
    }

    static void ApplySprite(Transform target, Sprite sprite, Vector2 collider, Vector2 offset, string shadowFile)
    {
        var renderer = target.GetComponent<SpriteRenderer>();
        if (renderer != null)
            renderer.sprite = sprite;
        var box = target.GetComponent<BoxCollider2D>();
        if (collider == Vector2.zero)
        {
            if (box != null)
                Object.DestroyImmediate(box);
        }
        else if (box == null)
        {
            box = target.gameObject.AddComponent<BoxCollider2D>();
            box.size = collider;
            box.offset = offset;
        }
        else
        {
            box.size = collider;
            box.offset = offset;
        }

        var shadow = target.Find("ContactShadow");
        if (shadow == null)
        {
            var shadowGo = new GameObject("ContactShadow");
            shadow = shadowGo.transform;
            shadow.SetParent(target, false);
        }
        shadow.localPosition = new Vector3(0f, 0.06f, 0f);
        var shadowRenderer = shadow.GetComponent<SpriteRenderer>();
        if (shadowRenderer == null)
            shadowRenderer = shadow.gameObject.AddComponent<SpriteRenderer>();
        shadowRenderer.sprite = LoadSprite(shadowFile);
        shadowRenderer.sortingLayerName = "Ground";
        shadowRenderer.sortingOrder = 3;
    }
}
