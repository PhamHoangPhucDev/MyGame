using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Builds the Hero frames Phase 4 is missing (Hurt, Death, attack windup) by editing the
/// existing Hero_Sheet pixels, then wires the clips into Hero.controller.
/// State names match what PlayerVisual plays: Hero_Hurt_{Down,Up,Side}, Hero_Death,
/// and Hero_Attack_* now start with a windup frame.
/// </summary>
public static class HeroExtraAnimBuilder
{
    const string Root = "Assets/Art/GoblinCampPhase4";
    const string SheetPath = Root + "/Hero_Sheet.png";
    const string FrameDir = Root + "/HeroExtra";
    const string AnimDir = Root + "/Animations";
    const string ControllerPath = Root + "/Animators/Hero.controller";
    const int Cell = 32;
    const int GroundY = 2; // matches the sheet pivot (0.5, 0.0625)

    // Attack clip timing (seconds). PlayerCombat strikes on frame 2 (Impact).
    public const float WindupHold = 0.08f;
    public const float Impact = 0.12f;
    public const float Recover = 0.19f;
    public const float AttackLength = 0.26f;
    const float ClipRate = 50f; // fine enough to place keys on 0.02 s boundaries

    static readonly Dictionary<string, Vector2Int> Cells = new()
    {
        { "Idle_Down", new Vector2Int(0, 256) },
        { "Idle_Up", new Vector2Int(0, 224) },
        { "Idle_Side", new Vector2Int(0, 192) },
        { "Attack_Down_0", new Vector2Int(0, 64) },
        { "Attack_Up_0", new Vector2Int(0, 32) },
        { "Attack_Side_0", new Vector2Int(0, 0) },
    };

    [MenuItem("Tools/Gameplay Prototype/Build Hero Hurt-Death-Windup")]
    public static void Build()
    {
        var sheet = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        sheet.LoadImage(File.ReadAllBytes(SheetPath));
        if (!AssetDatabase.IsValidFolder(FrameDir))
            AssetDatabase.CreateFolder(Root, "HeroExtra");

        var frames = new Dictionary<string, Color32[]>();
        foreach (var dir in new[] { "Down", "Up", "Side" })
        {
            Vector2Int back = BackOffset(dir);
            var idle = Read(sheet, Cells["Idle_" + dir]);
            var atk0 = Read(sheet, Cells["Attack_" + dir + "_0"]);

            // Windup: lean away from the strike, crouch 1px, glint on the blade tip.
            frames["Windup_" + dir] = Glint(Crouch(Shift(atk0, back.x, back.y), 1));
            // Hurt: knocked back 1px with a red tint, then a 1px recoil hold.
            frames["Hurt_" + dir + "_0"] = Tint(Crouch(Shift(idle, back.x, back.y), 1), 1f, 0.72f, 0.72f);
            frames["Hurt_" + dir + "_1"] = Shift(idle, back.x, back.y);
        }

        var idleDown = Read(sheet, Cells["Idle_Down"]);
        var lying = Ground(Rotate90(Read(sheet, Cells["Idle_Side"])));
        frames["Death_0"] = frames["Hurt_Down_0"];
        frames["Death_1"] = Darken(Crouch(idleDown, 3), 0.9f);
        frames["Death_2"] = lying;
        frames["Death_3"] = Darken(lying, 0.7f);

        var sprites = new Dictionary<string, Sprite>();
        foreach (var kv in frames)
            sprites[kv.Key] = SaveSprite(kv.Key, kv.Value);

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        var machine = controller.layers[0].stateMachine;
        foreach (var dir in new[] { "Down", "Up", "Side" })
        {
            // Clip length = last key + one frame, so the last key sits one frame before the end.
            var hurt = SaveClip("Hero_Hero_Hurt_" + dir, ClipRate, false,
                (0f, sprites["Hurt_" + dir + "_0"]),
                (0.08f, sprites["Hurt_" + dir + "_1"]),
                (0.18f - 1f / ClipRate, sprites["Hurt_" + dir + "_1"]));
            SetState(machine, "Hero_Hurt_" + dir, hurt);

            // Rebuild the attack as windup + the 3 original frames.
            var oldState = FindState(machine, "Hero_Attack_" + dir);
            var oldClip = oldState != null ? oldState.motion as AnimationClip : null;
            var original = oldClip != null ? OriginalAttackSprites(oldClip) : null;
            if (original == null || original.Length < 3)
            {
                Debug.LogError($"[HeroExtra] Hero_Attack_{dir} has no 3-frame clip to extend.");
                continue;
            }
            var attack = SaveClip("Hero_Hero_Attack_" + dir + "_Windup", ClipRate, false,
                (0f, sprites["Windup_" + dir]),
                (WindupHold, original[0]),
                (Impact, original[1]),
                (Recover, original[2]),
                (AttackLength - 1f / ClipRate, original[2]));
            SetState(machine, "Hero_Attack_" + dir, attack);
        }

        var death = SaveClip("Hero_Hero_Death", 12f, false,
            (0f, sprites["Death_0"]),
            (0.10f, sprites["Death_1"]),
            (0.24f, sprites["Death_2"]),
            (0.42f, sprites["Death_3"]),
            (0.60f, sprites["Death_3"]));
        SetState(machine, "Hero_Death", death);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Object.DestroyImmediate(sheet);
        Debug.Log($"[HeroExtra] Built {frames.Count} frames, Hurt x3, Death, Attack windup x3 into {ControllerPath}.");
    }

    // Lean direction for windup and knockback: opposite of where the hero faces.
    // Side frames in the sheet face left, so "back" is +x.
    static Vector2Int BackOffset(string dir) => dir switch
    {
        "Down" => new Vector2Int(0, 1),
        "Up" => new Vector2Int(0, -1),
        _ => new Vector2Int(1, 0)
    };

    static Sprite[] OriginalAttackSprites(AnimationClip clip)
    {
        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            if (binding.propertyName != "m_Sprite") continue;
            var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            var list = new List<Sprite>();
            foreach (var k in keys)
                if (k.value is Sprite s && !list.Contains(s) && !s.name.Contains("Windup"))
                    list.Add(s);
            return list.ToArray();
        }
        return null;
    }

    static Color32[] Read(Texture2D sheet, Vector2Int cell) => sheet.GetPixels32Block(cell.x, cell.y);

    static Color32[] GetPixels32Block(this Texture2D tex, int x, int y)
    {
        var src = tex.GetPixels32();
        var dst = new Color32[Cell * Cell];
        for (int row = 0; row < Cell; row++)
        for (int col = 0; col < Cell; col++)
            dst[row * Cell + col] = src[(y + row) * tex.width + x + col];
        return dst;
    }

    static Color32[] Shift(Color32[] f, int dx, int dy)
    {
        var o = new Color32[f.Length];
        for (int y = 0; y < Cell; y++)
        for (int x = 0; x < Cell; x++)
        {
            int sx = x - dx, sy = y - dy;
            if (sx < 0 || sy < 0 || sx >= Cell || sy >= Cell) continue;
            o[y * Cell + x] = f[sy * Cell + sx];
        }
        return o;
    }

    // Removes rows at the waist so the head drops while the feet stay planted.
    static Color32[] Crouch(Color32[] f, int rows)
    {
        Bounds(f, out _, out int minY, out _, out int maxY);
        if (maxY < 0) return f;
        int waist = minY + Mathf.RoundToInt((maxY - minY) * 0.4f);
        var o = new Color32[f.Length];
        for (int y = 0; y < Cell; y++)
        for (int x = 0; x < Cell; x++)
        {
            int sy = y < waist ? y : y + rows;
            if (sy >= Cell) continue;
            o[y * Cell + x] = f[sy * Cell + x];
        }
        return o;
    }

    static Color32[] Tint(Color32[] f, float r, float g, float b)
    {
        var o = (Color32[])f.Clone();
        for (int i = 0; i < o.Length; i++)
        {
            var c = o[i];
            if (c.a == 0 || Luma(c) < 0.18f) continue; // keep the dark outline
            o[i] = new Color32((byte)(c.r * r), (byte)(c.g * g), (byte)(c.b * b), c.a);
        }
        return o;
    }

    static Color32[] Darken(Color32[] f, float k)
    {
        var o = (Color32[])f.Clone();
        for (int i = 0; i < o.Length; i++)
        {
            var c = o[i];
            if (c.a == 0) continue;
            float grey = Luma(c) * 255f;
            o[i] = new Color32(
                (byte)(Mathf.Lerp(c.r, grey, 0.3f) * k),
                (byte)(Mathf.Lerp(c.g, grey, 0.3f) * k),
                (byte)(Mathf.Lerp(c.b, grey, 0.3f) * k), c.a);
        }
        return o;
    }

    // Brightens the blade pixel farthest from the body centre and adds a 1px cross.
    static Color32[] Glint(Color32[] f)
    {
        var o = (Color32[])f.Clone();
        Vector2 centre = new(Cell * 0.5f, 10f);
        int best = -1;
        float bestDist = -1f;
        for (int i = 0; i < f.Length; i++)
        {
            var c = f[i];
            if (c.a < 200 || c.b < 170 || c.g < 150 || c.r > c.b) continue; // pale blue steel
            float d = Vector2.Distance(new Vector2(i % Cell, i / Cell), centre);
            if (d > bestDist) { bestDist = d; best = i; }
        }
        if (best < 0) return o;
        int bx = best % Cell, by = best / Cell;
        var white = new Color32(255, 255, 255, 255);
        var soft = new Color32(220, 245, 255, 200);
        o[best] = white;
        foreach (var (dx, dy) in new[] { (1, 0), (-1, 0), (0, 1), (0, -1) })
        {
            int x = bx + dx, y = by + dy;
            if (x < 0 || y < 0 || x >= Cell || y >= Cell) continue;
            if (o[y * Cell + x].a == 0) o[y * Cell + x] = soft;
        }
        return o;
    }

    // Quarter turn clockwise: the side-facing hero ends up lying on the ground.
    static Color32[] Rotate90(Color32[] f)
    {
        var o = new Color32[f.Length];
        for (int y = 0; y < Cell; y++)
        for (int x = 0; x < Cell; x++)
            o[(Cell - 1 - x) * Cell + y] = f[y * Cell + x];
        return o;
    }

    static Color32[] Ground(Color32[] f)
    {
        Bounds(f, out int minX, out int minY, out int maxX, out _);
        if (minY < 0) return f;
        int dx = Cell / 2 - (minX + maxX + 1) / 2;
        return Shift(f, dx, GroundY - minY);
    }

    static void Bounds(Color32[] f, out int minX, out int minY, out int maxX, out int maxY)
    {
        minX = minY = int.MaxValue;
        maxX = maxY = -1;
        for (int i = 0; i < f.Length; i++)
        {
            if (f[i].a == 0) continue;
            int x = i % Cell, y = i / Cell;
            minX = Mathf.Min(minX, x); maxX = Mathf.Max(maxX, x);
            minY = Mathf.Min(minY, y); maxY = Mathf.Max(maxY, y);
        }
        if (maxY < 0) minX = minY = -1;
    }

    static float Luma(Color32 c) => (0.299f * c.r + 0.587f * c.g + 0.114f * c.b) / 255f;

    static Sprite SaveSprite(string name, Color32[] px)
    {
        string path = $"{FrameDir}/Hero_{name}.png";
        var tex = new Texture2D(Cell, Cell, TextureFormat.RGBA32, false);
        tex.SetPixels32(px);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 16;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0.5f, GroundY / (float)Cell);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static AnimationClip SaveClip(string name, float sampleRate, bool loop, params (float time, Sprite sprite)[] keys)
    {
        string path = $"{AnimDir}/{name}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip();
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.ClearCurves();
        clip.frameRate = sampleRate;
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        var frames = new ObjectReferenceKeyframe[keys.Length];
        for (int i = 0; i < keys.Length; i++)
            frames[i] = new ObjectReferenceKeyframe { time = keys[i].time, value = keys[i].sprite };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, frames);
        var clipSettings = AnimationUtility.GetAnimationClipSettings(clip);
        clipSettings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, clipSettings);
        EditorUtility.SetDirty(clip);
        return clip;
    }

    static AnimatorState FindState(AnimatorStateMachine machine, string name)
    {
        foreach (var child in machine.states)
            if (child.state.name == name)
                return child.state;
        return null;
    }

    static void SetState(AnimatorStateMachine machine, string name, AnimationClip clip)
    {
        var state = FindState(machine, name) ?? machine.AddState(name);
        state.motion = clip;
        state.writeDefaultValues = false;
    }
}
