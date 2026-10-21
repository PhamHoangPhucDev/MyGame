#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameplayPrototype.Editor
{
    /// <summary>R3D.1: slice Aren kit VFX sheets per AREN_KIT_VFX_MANIFEST.csv.</summary>
    public static class ArenKitVfxImporter
    {
        const string Folder = "Assets/Art/Rebuild/R2C/Aren/VFX";
        const string ResourcesFolder = "Assets/Resources/VFX/ArenKit";

        struct Spec
        {
            public string file;
            public int cellW, cellH, cols, rows;
            public float pivotX, pivotY;
        }

        static readonly Spec[] Specs =
        {
            new() { file = "Aren_Thrust.png", cellW = 80, cellH = 80, cols = 3, rows = 3, pivotX = 0.5f, pivotY = 0.375f },
            new() { file = "Aren_ShieldBash.png", cellW = 64, cellH = 64, cols = 3, rows = 3, pivotX = 0.5f, pivotY = 0.375f },
            new() { file = "Aren_ShieldGlint.png", cellW = 16, cellH = 16, cols = 2, rows = 1, pivotX = 0.5f, pivotY = 0.5f },
            new() { file = "Aren_StunStars.png", cellW = 16, cellH = 16, cols = 4, rows = 1, pivotX = 0.5f, pivotY = 0.5f },
            new() { file = "Aren_ChargeDust.png", cellW = 16, cellH = 16, cols = 3, rows = 1, pivotX = 0.5f, pivotY = 0.0625f },
            new() { file = "Aren_WhirlRing_Back.png", cellW = 64, cellH = 48, cols = 4, rows = 1, pivotX = 0.5f, pivotY = 0.2917f },
            new() { file = "Aren_WhirlRing_Front.png", cellW = 64, cellH = 48, cols = 4, rows = 1, pivotX = 0.5f, pivotY = 0.2917f },
            new() { file = "Aren_Bulwark_Back.png", cellW = 48, cellH = 48, cols = 8, rows = 1, pivotX = 0.5f, pivotY = 0.1667f },
            new() { file = "Aren_Bulwark_Front.png", cellW = 48, cellH = 48, cols = 8, rows = 1, pivotX = 0.5f, pivotY = 0.1667f },
            new() { file = "Aren_Dawn.png", cellW = 96, cellH = 96, cols = 3, rows = 3, pivotX = 0.5f, pivotY = 0.3333f },
        };

        [MenuItem("Game01/R3D1/Import Aren Kit VFX Sheets")]
        public static void ImportAll()
        {
            if (!AssetDatabase.IsValidFolder(Folder))
                AssetDatabase.CreateFolder("Assets/Art/Rebuild/R2C/Aren", "VFX");

            EnsureFolder(ResourcesFolder);
            int ok = 0;
            foreach (var s in Specs)
            {
                string path = $"{Folder}/{s.file}";
                if (!File.Exists(Path.GetFullPath(path)))
                {
                    Debug.LogWarning($"[ArenKitVfx] Missing {path}");
                    continue;
                }
                Slice(path, s);
                // Player builds: mirror under Resources with identical slice settings.
                string resPath = $"{ResourcesFolder}/{s.file}";
                AssetDatabase.CopyAsset(path, resPath);
                Slice(resPath, s);
                ok++;
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[ArenKitVfx] Imported/sliced {ok}/{Specs.Length} sheets → {Folder} + {ResourcesFolder}");
        }

        static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder)) return;
            string[] parts = folder.Split('/');
            string cur = parts[0];
            for (int i = 1; i < parts.Length; i++)
            {
                string next = cur + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(cur, parts[i]);
                cur = next;
            }
        }

        static void Slice(string path, Spec s)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.spritePivot = new Vector2(s.pivotX, s.pivotY);

            var metas = new List<SpriteMetaData>();
            int idx = 0;
            for (int row = 0; row < s.rows; row++)
            {
                for (int col = 0; col < s.cols; col++)
                {
                    // Unity slice Y origin is bottom-left; sheet row 0 is top (Down).
                    int x = col * s.cellW;
                    int y = (s.rows - 1 - row) * s.cellH;
                    metas.Add(new SpriteMetaData
                    {
                        name = Path.GetFileNameWithoutExtension(s.file) + "_" + idx,
                        rect = new Rect(x, y, s.cellW, s.cellH),
                        alignment = (int)SpriteAlignment.Custom,
                        pivot = new Vector2(s.pivotX, s.pivotY),
                        border = Vector4.zero
                    });
                    idx++;
                }
            }
            importer.spritesheet = metas.ToArray();
            EditorUtility.SetDirty(importer);
            importer.SaveAndReimport();
        }
    }
}
#endif
