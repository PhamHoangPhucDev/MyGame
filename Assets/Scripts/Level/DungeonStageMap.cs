using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace GameplayPrototype
{
    /// <summary>
    /// Runtime maps for Goblin Camp stages 1, 3, 4 and 5, laid out after mockups S1/S3/S4/S5
    /// (Docs/UI/mockups/landscape/dungeon_v2). Ground is painted at 16 px per tile with the mockup palettes,
    /// props reuse the camp art with Y-sorting, and each map carries its own lights, walls and spawn data.
    /// Stage 2 is DungeonFenceMaze.
    /// </summary>
    public class DungeonStageMap : MonoBehaviour
    {
        public const string RedTrail = "RedTrail";
        public const string FireCamp = "FireCamp";
        public const string Chasm = "Chasm";
        public const string BoneYard = "BoneYard";

        public struct Info
        {
            public Vector2 spawn, exit, min, max;
            public Vector2[] zones;
        }

        static readonly Dictionary<string, DungeonStageMap> Built = new Dictionary<string, DungeonStageMap>();

        /// <summary>Centre of the bone ritual ring in stage 5 (the boss spawns here).</summary>
        public static readonly Vector2 RitualCentre = new Vector2(0f, 1.5f);
        /// <summary>Stage 4 bridge: x span and centre line.</summary>
        public const float BridgeHalf = 6f;
        public const float BridgeY = 0f;

        public static bool Handles(string id) => id == RedTrail || id == FireCamp || id == Chasm || id == BoneYard;

        public static bool IsOn(string id) => Built.TryGetValue(id, out var m) && m != null && m.gameObject.activeSelf;

        public static Info Get(string id)
        {
            switch (id)
            {
                case RedTrail:
                    return new Info
                    {
                        spawn = new Vector2(0f, -15f), exit = new Vector2(13.5f, 15f),
                        min = new Vector2(-14f, -19f), max = new Vector2(20f, 19f),
                        zones = new[] { new Vector2(1.5f, -5f), new Vector2(5f, 1f), new Vector2(8.5f, 7f), new Vector2(11.5f, 11.5f) }
                    };
                case FireCamp:
                    return new Info
                    {
                        spawn = new Vector2(0f, -4f), exit = new Vector2(0f, 8f),
                        min = new Vector2(-20f, -14f), max = new Vector2(20f, 14f),
                        zones = new[] { new Vector2(0f, 8f), new Vector2(0f, -8f), new Vector2(-12f, 0f), new Vector2(12f, 0f) }
                    };
                case Chasm:
                    return new Info
                    {
                        spawn = new Vector2(-17f, 0f), exit = new Vector2(19f, 0f),
                        min = new Vector2(-22f, -11f), max = new Vector2(22f, 11f),
                        zones = new[] { new Vector2(-9f, 1.5f), new Vector2(-9f, -1.5f), new Vector2(9f, 3f), new Vector2(12f, 0f) }
                    };
                default:
                    return new Info
                    {
                        spawn = new Vector2(0f, -6f), exit = new Vector2(0f, 9f),
                        min = new Vector2(-20f, -14f), max = new Vector2(20f, 14f),
                        zones = new[] { new Vector2(-8f, 5f), new Vector2(8f, 5f), new Vector2(0f, 7.5f), new Vector2(-11f, -2f) }
                    };
            }
        }

        /// <summary>Where each wave enters (spec C). Null keeps the map's default zones.</summary>
        public static Vector2[] WaveZones(string id, int wave)
        {
            switch (id)
            {
                case RedTrail:
                    // One clearing per wave, walking up the trail; the last wave guards the gate.
                    if (wave == 1) return new[] { new Vector2(1.5f, -5f), new Vector2(3f, -3.5f) };
                    if (wave == 2) return new[] { new Vector2(5f, 1f), new Vector2(3.5f, -1f), new Vector2(6.5f, 2.5f) };
                    if (wave == 3) return new[] { new Vector2(8.5f, 7f), new Vector2(7f, 5f), new Vector2(10f, 8.5f) };
                    if (wave == 4) return new[] { new Vector2(12f, 12.5f), new Vector2(10.5f, 10f) };
                    return null;
                case FireCamp:
                {
                    var sides = DungeonCatalog.CampWaveSides(wave);
                    if (sides.Length == 0) return null;
                    var z = new Vector2[sides.Length];
                    for (int i = 0; i < sides.Length; i++) z[i] = new Vector2(sides[i].x * 12f, sides[i].y * 8f);
                    return z;
                }
                case Chasm:
                    // Segment 1: near bridge head. 2: far bank archers + riders on the bridge. 3: cyclops at the far end.
                    if (wave == 1) return new[] { new Vector2(-9f, 1.5f), new Vector2(-9f, -1.5f), new Vector2(-3f, 0f) };
                    if (wave == 2) return new[] { new Vector2(9f, 3f), new Vector2(9f, -3f), new Vector2(2f, 0f) };
                    if (wave == 3) return new[] { new Vector2(11f, 0f) };
                    return null;
                case BoneYard:
                    if (wave == 3) return new[] { RitualCentre };
                    if (wave == 2) return new[] { new Vector2(-9f, 4f), new Vector2(9f, 4f), new Vector2(-11f, -3f), new Vector2(11f, -2f) };
                    return new[] { new Vector2(-8f, 5f), new Vector2(8f, 5f), new Vector2(0f, 7.5f) };
            }
            return null;
        }

        public static void Show(string id)
        {
            HideAll();
            if (!Built.TryGetValue(id, out var map) || map == null)
            {
                var go = new GameObject("StageMap_" + id);
                map = go.AddComponent<DungeonStageMap>();
                map.Build(id);
                Built[id] = map;
            }
            map.gameObject.SetActive(true);
        }

        public static void HideAll()
        {
            foreach (var kv in Built)
                if (kv.Value != null) kv.Value.gameObject.SetActive(false);
        }

        // ------------------------------------------------------------------ build

        Transform _props;
        System.Random _rng;

        void Build(string id)
        {
            _rng = new System.Random(id.GetHashCode());
            _props = new GameObject("Props").transform;
            _props.SetParent(transform, false);
            switch (id)
            {
                case RedTrail: BuildRedTrail(); break;
                case FireCamp: BuildFireCamp(); break;
                case Chasm: BuildChasm(); break;
                default: BuildBoneYard(); break;
            }
        }

        float R(float a, float b) => a + (float)_rng.NextDouble() * (b - a);

        // Stage 1 · S1: dense blue-green forest, orange dirt trail rising from the south to the gate in the north-east,
        // two clearings to fight in.
        static readonly Vector2[] TrailPts =
        {
            new Vector2(-0.5f, -20f), new Vector2(0f, -14f), new Vector2(-1f, -9.5f), new Vector2(1.5f, -5f),
            new Vector2(4f, -1f), new Vector2(7f, 3f), new Vector2(8.5f, 7f), new Vector2(11f, 11f),
            new Vector2(13.5f, 15f), new Vector2(15f, 20f)
        };
        static readonly Vector2[] TrailClearings = { new Vector2(1.5f, -5f), new Vector2(8.5f, 7f) };

        static float TrailDist(Vector2 p)
        {
            float best = float.MaxValue;
            for (int i = 0; i < TrailPts.Length - 1; i++) best = Mathf.Min(best, SegDist(p, TrailPts[i], TrailPts[i + 1]));
            foreach (var c in TrailClearings) best = Mathf.Min(best, Mathf.Max(0f, Vector2.Distance(p, c) - 2.6f));
            return best;
        }

        void BuildRedTrail()
        {
            var info = Get(RedTrail);
            Color32[] dirt = { H(0x9A4A26), H(0xB45E32), H(0xC8763E), H(0xDC9250) };
            Color32[] grass = { H(0x2E5A2A), H(0x3C6E2C), H(0x4E8434), H(0x5E9A3C) };
            Ground(info.min - new Vector2(4f, 4f), info.max + new Vector2(4f, 4f), (x, y, bx, by) =>
            {
                var p = new Vector2(x, y);
                float edge = 1.55f + 0.55f * Mathf.PerlinNoise(x * 0.35f + 7f, y * 0.35f);
                float d = TrailDist(p);
                float n = Fbm(x * 0.18f, y * 0.18f);
                if (d < edge)
                {
                    var c = Tone(dirt, 0.25f + n * 0.75f - Mathf.Clamp01(d / edge) * 0.25f, bx, by);
                    if (Hash(bx, by, 3) < 0.012f) c = H(0xF2C88C);
                    if (Hash(bx, by, 5) < 0.008f) c = H(0x5A2A14);
                    return c;
                }
                if (d < edge + 0.25f) return H(0x3C6E2C);
                var g = Tone(grass, n * 0.9f + 0.1f - Mathf.Clamp01((d - 3f) / 6f) * 0.35f, bx, by);
                if (Hash(bx, by, 9) < 0.006f) g = H(0x9CD460);
                return g;
            });

            // Forest: trees off the trail, thicker away from it.
            string[] trees = { "Tree_Forest_A", "Tree_Forest_B", "Tree_Forest_C_Compact" };
            for (float y = info.min.y - 2f; y <= info.max.y + 3f; y += 2.4f)
                for (float x = info.min.x - 3f; x <= info.max.x + 3f; x += 2.6f)
                {
                    var p = new Vector2(x + R(-0.9f, 0.9f), y + R(-0.8f, 0.8f));
                    float d = TrailDist(p);
                    if (d < 2.9f) continue;
                    if (d < 4.3f && R(0f, 1f) < 0.45f) { Prop(Pick(new[] { "BushA", "BushB", "BushC", "BushD", "BushE" }), p, true); continue; }
                    Prop(trees[_rng.Next(trees.Length)], p, true, tree: true);
                }
            for (int i = 0; i < 18; i++)
            {
                var p = Along(TrailPts, R(0.05f, 0.95f)) + new Vector2(R(-2.2f, 2.2f), R(-1f, 1f));
                Prop(i % 3 == 0 ? "Rock_Small" : "Rock_Large", p, false, sort: false, order: 2);
            }
            Prop("Stump", new Vector2(4.5f, -6.5f), true);

            // Walls: everything farther than the trail edge + 1.4 tiles is forest.
            Walls(info.min, info.max, 1.5f, p => TrailDist(p) > 3.1f);
            Global(new Color(0.92f, 0.98f, 0.9f), 1f);
        }

        // Stage 3 · S3: night clearing, round packed-dirt yard with the campfire in the middle, tents, crates, flags.
        void BuildFireCamp()
        {
            var info = Get(FireCamp);
            Color32[] dirt = { H(0x3A3430), H(0x4A423A), H(0x5A5046), H(0x6E6252) };
            Color32[] grass = { H(0x14241A), H(0x1C3222), H(0x24402A), H(0x2E5232) };
            Ground(info.min - new Vector2(4f, 4f), info.max + new Vector2(4f, 4f), (x, y, bx, by) =>
            {
                float e = Ell(x, y, 13f, 9.5f);
                float n = Fbm(x * 0.2f, y * 0.2f);
                float wob = (Mathf.PerlinNoise(x * 0.3f, y * 0.3f) - 0.5f) * 0.12f;
                if (e + wob < 1f)
                {
                    // Concentric bands, lighter towards the fire.
                    float ring = 1f - e;
                    float band = Mathf.Repeat(e * 4.2f, 1f) < 0.12f ? 0.12f : 0f;
                    var c = Tone(dirt, 0.15f + ring * 0.55f + n * 0.3f + band, bx, by);
                    if (Hash(bx, by, 4) < 0.01f) c = H(0x8A7A66);
                    return c;
                }
                if (e + wob < 1.35f) return Tone(grass, 0.45f + n * 0.5f, bx, by);
                // Dark woods and a pond in two corners, as in the mockup.
                if (Ell(x + 16f, y + 9f, 5f, 4f) < 1f || Ell(x - 15f, y + 11f, 5f, 3.5f) < 1f) return Tone(new[] { H(0x1A2A44), H(0x22385A), H(0x2C4A6E) }, n, bx, by);
                return Tone(grass, n * 0.35f, bx, by);
            });

            var fire = Prop("Campfire", Vector2.zero, true, scale: 2.5f);
            Point(fire.transform, new Color(1f, 0.62f, 0.3f), 2.4f, 10.5f);
            Prop("TentA", new Vector2(-7.6f, 5f), true);
            Prop("TentB", new Vector2(7.6f, 5f), true);
            Prop("TentB", new Vector2(6.8f, -7.6f), true);
            Prop("Wooden_Crate_Stack", new Vector2(-4.4f, 4.4f), true);
            Prop("Goblin_Flag_Tattered", new Vector2(-10.2f, -2f), true);
            Prop("Goblin_Flag_Tattered", new Vector2(10f, -1.6f), true);
            Prop("Barrel", new Vector2(10.2f, 3f), true);
            Prop("Bone_Pile", new Vector2(-3.4f, 1.2f), false, sort: false, order: 3);
            Prop("Bone_Pile", new Vector2(2.6f, -4.2f), false, sort: false, order: 3);
            for (int i = 0; i < 14; i++)
            {
                float a = i / 14f * Mathf.PI * 2f + R(-0.15f, 0.15f);
                Prop(i % 2 == 0 ? "Rock_Large" : "BushC", new Vector2(Mathf.Cos(a) * 14.6f, Mathf.Sin(a) * 10.6f), true);
            }
            for (float x = info.min.x - 3f; x <= info.max.x + 3f; x += 2.8f)
                for (float y = info.min.y - 3f; y <= info.max.y + 3f; y += 2.6f)
                    if (Ell(x, y, 16.5f, 12f) > 1f) Prop(Pick(new[] { "Tree_Forest_A", "Tree_Forest_B" }), new Vector2(x + R(-0.8f, 0.8f), y + R(-0.6f, 0.6f)), true, tree: true);

            EllipseWall(Vector2.zero, 14.2f, 10.2f);
            Global(new Color(0.46f, 0.54f, 0.76f), 0.9f);
        }

        // Stage 4 · S4: grass bank on the left, a deep chasm in the middle crossed by a 12-tile rope bridge,
        // the goblin outpost on the right bank with torches.
        void BuildChasm()
        {
            var info = Get(Chasm);
            Color32[] grass = { H(0x2E5A2A), H(0x3C6E2C), H(0x4E8434), H(0x5E9A3C) };
            Color32[] dirt = { H(0x8A6A4A), H(0x9E7E5A), H(0xB4946C), H(0xC8A87E) };
            Color32[] deep = { H(0x0E0C1C), H(0x15122A), H(0x1C1836), H(0x262046) };
            Color32[] cliff = { H(0x3A3254), H(0x50466E), H(0x665A86), H(0x7E72A0) };
            Ground(info.min - new Vector2(4f, 4f), info.max + new Vector2(4f, 4f), (x, y, bx, by) =>
            {
                float n = Fbm(x * 0.2f, y * 0.2f);
                float jag = (Mathf.PerlinNoise(0.5f, y * 0.9f) - 0.5f) * 0.8f;
                float ax = Mathf.Abs(x);
                if (ax < BridgeHalf - 0.9f + jag)
                {
                    // Chasm: dark with drifting mist bands; the bridge throws a shadow below it.
                    var c = Tone(deep, n * 0.7f, bx, by);
                    float mist = Mathf.PerlinNoise(x * 0.12f + 3f, y * 0.5f);
                    if (mist > 0.62f) c = Color32.Lerp(c, H(0x2C2A4A), 0.7f);
                    if (y < BridgeY - 1f && y > BridgeY - 2.6f) c = Color32.Lerp(c, H(0x08060E), 0.6f);
                    return c;
                }
                if (ax < BridgeHalf + jag) return Tone(cliff, 0.2f + (ax - (BridgeHalf - 0.9f)) * 0.8f + n * 0.3f, bx, by);
                if (x < 0f)
                {
                    float path = Mathf.Abs(y - BridgeY) - 1.6f - Mathf.PerlinNoise(x * 0.4f, 1f) * 0.6f;
                    if (path < 0f) return Tone(dirt, 0.3f + n * 0.6f, bx, by);
                    return Tone(grass, 0.15f + n * 0.8f, bx, by);
                }
                // Right bank: trodden outpost dirt fading to grass at the far edge.
                float yard = Ell(x - 14f, y, 10f, 10.5f);
                if (yard < 1f) return Tone(dirt, 0.15f + n * 0.7f - yard * 0.15f, bx, by);
                return Tone(grass, 0.1f + n * 0.7f, bx, by);
            });

            // Bridge: 0,1,1,1,3,1,1,1,3,1,1,2 (README Stage4), one tile per world unit, two tiles tall.
            var tiles = DungeonFenceMaze.Art("Stage4", "Stage4_Bridge_Tiles_4x16x32");
            int[] seq = { 0, 1, 1, 1, 3, 1, 1, 1, 3, 1, 1, 2 };
            for (int i = 0; i < seq.Length; i++)
            {
                var go = new GameObject("Bridge" + i);
                go.transform.SetParent(transform, false);
                go.transform.position = new Vector3(-BridgeHalf + 0.5f + i, BridgeY, 0f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = tiles.Length > seq[i] ? tiles[seq[i]] : null;
                sr.color = sr.sprite == null ? new Color(0.55f, 0.38f, 0.22f) : Color.white;
                sr.sortingLayerName = "Ground";
                sr.sortingOrder = 10;
            }

            string[] trees = { "Tree_Forest_A", "Tree_Forest_B", "Tree_Forest_C_Compact" };
            for (float x = info.min.x - 3f; x < -7.5f; x += 2.6f)
                for (float y = info.min.y - 3f; y <= info.max.y + 3f; y += 2.4f)
                {
                    var p = new Vector2(x + R(-0.8f, 0.8f), y + R(-0.6f, 0.6f));
                    if (Mathf.Abs(p.y - BridgeY) < 4.2f && p.x > info.min.x + 2f) continue;
                    if (Mathf.Abs(p.y - BridgeY) < 6.5f && p.x > -14f && R(0f, 1f) < 0.5f) continue;
                    Prop(trees[_rng.Next(trees.Length)], p, true, tree: true);
                }
            Prop("Rock_Large", new Vector2(-8.2f, 3.4f), true);
            Prop("BushA", new Vector2(-12f, -3.6f), true);
            Prop("Goblin_Flag_Tattered", new Vector2(8.6f, 4f), true);
            Prop("Goblin_Flag_Tattered", new Vector2(8.6f, -6.4f), true);
            Prop("Goblin_Flag_Tattered", new Vector2(18.5f, 3.2f), true);
            Prop("Goblin_Flag_Tattered", new Vector2(18.5f, -3.2f), true);
            Prop("Wooden_Crate_Stack", new Vector2(15f, 7.5f), true);
            Prop("TentB", new Vector2(18f, 7.8f), true);
            Prop("Barrel", new Vector2(14f, -8f), true);
            Prop("Bone_Pile", new Vector2(16f, -5f), false, sort: false, order: 3);
            foreach (var t in new[] { new Vector2(7.2f, 2.4f), new Vector2(7.2f, -2.4f), new Vector2(12f, 5.5f), new Vector2(12f, -5.5f) })
                Torch(t);

            // Chasm walls above and below the bridge, plus the map edge.
            Box(new Vector2(0f, (BridgeY + 1f + info.max.y + 2f) * 0.5f), new Vector2(BridgeHalf * 2f - 0.2f, info.max.y + 2f - (BridgeY + 1f)));
            Box(new Vector2(0f, (BridgeY - 1f + info.min.y - 2f) * 0.5f), new Vector2(BridgeHalf * 2f - 0.2f, (BridgeY - 1f) - (info.min.y - 2f)));
            RectWall(info.min, info.max);
            Global(new Color(0.78f, 0.8f, 0.95f), 0.9f);
        }

        // Stage 5 · S5: purple dusk, oval bone yard with two horned bone tents, braziers and the ritual ring in the middle.
        void BuildBoneYard()
        {
            var info = Get(BoneYard);
            Color32[] dirt = { H(0x3E3040), H(0x4E3C4E), H(0x5E4A5C), H(0x6E5868) };
            Ground(info.min - new Vector2(4f, 4f), info.max + new Vector2(4f, 4f), (x, y, bx, by) =>
            {
                float e = Ell(x, y - 1f, 15f, 10f);
                float n = Fbm(x * 0.22f, y * 0.22f);
                bool path = Mathf.Abs(x) < 2.2f + Mathf.PerlinNoise(y * 0.5f, 2f) * 0.6f && y < -6f;
                if (e < 1f || path)
                {
                    var c = Tone(dirt, 0.2f + n * 0.75f - Mathf.Clamp01(e - 0.8f) * 1.2f, bx, by);
                    if (Hash(bx, by, 7) < 0.004f) c = H(0xB890E8);
                    if (Hash(bx, by, 8) < 0.006f) c = H(0x2A2030);
                    return c;
                }
                if (e < 1.08f) return H(0x241A26);
                var v = Tone(new[] { H(0x0E0A12), H(0x140E1A), H(0x1A1222) }, n * 0.6f, bx, by);
                if (Hash(bx, by, 11) < 0.0015f) v = H(0x6A4A8A);
                return v;
            });

            var ring = One("Stage5", "Stage5_RitualRing");
            if (ring != null)
            {
                var go = new GameObject("RitualRing");
                go.transform.SetParent(transform, false);
                go.transform.position = RitualCentre;
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = ring;
                sr.sortingLayerName = "Ground";
                sr.sortingOrder = 10;
                float s = 8f / Mathf.Max(0.01f, ring.bounds.size.x);
                go.transform.localScale = new Vector3(s, s, 1f);
            }
            Prop("Tent_GoblinHorn", new Vector2(-8.6f, -0.6f), true);
            Prop("Tent_GoblinHorn", new Vector2(7.4f, -3.4f), true);
            foreach (var b in new[] { new Vector2(5.4f, 1.4f), new Vector2(-5.4f, -4.2f) })
            {
                var br = Prop("Brazier_Bowl", b, true);
                Point(br.transform, new Color(1f, 0.55f, 0.3f), 1.4f, 4.5f);
            }
            Prop("Bone_Pile", new Vector2(9.6f, 0.4f), false, sort: false, order: 3);
            Prop("Bone_Pile", new Vector2(-3.4f, -2.2f), false, sort: false, order: 3);
            Prop("Bone_Pile", new Vector2(-3f, 4.8f), false, sort: false, order: 3);
            Prop("Rock_Small", new Vector2(-4.6f, 0.6f), true);
            Prop("Stake", new Vector2(-2.2f, -8.8f), true);
            Prop("Stake", new Vector2(2.2f, -8.8f), true);
            Point(transform, new Color(0.95f, 0.35f, 0.4f), 0.9f, 5.5f).transform.position = RitualCentre;

            EllipseWall(new Vector2(0f, 1f), 15.4f, 10.4f, gapBelow: true);
            Box(new Vector2(-3.2f, -11.5f), new Vector2(1f, 5f));
            Box(new Vector2(3.2f, -11.5f), new Vector2(1f, 5f));
            Box(new Vector2(0f, -14.2f), new Vector2(7.4f, 1f));
            Global(new Color(0.74f, 0.6f, 0.88f), 0.85f);
        }

        // ------------------------------------------------------------------ helpers

        delegate Color32 Painter(float x, float y, int bx, int by);

        static readonly float[,] Bayer =
        {
            { 0f / 16f, 8f / 16f, 2f / 16f, 10f / 16f },
            { 12f / 16f, 4f / 16f, 14f / 16f, 6f / 16f },
            { 3f / 16f, 11f / 16f, 1f / 16f, 9f / 16f },
            { 15f / 16f, 7f / 16f, 13f / 16f, 5f / 16f }
        };

        void Ground(Vector2 min, Vector2 max, Painter paint)
        {
            int w = Mathf.CeilToInt(max.x - min.x) * 16, h = Mathf.CeilToInt(max.y - min.y) * 16;
            var px = new Color32[w * h];
            for (int by = 0; by < h; by++)
            {
                float y = min.y + (by + 0.5f) / 16f;
                for (int bx = 0; bx < w; bx++)
                    px[by * w + bx] = paint(min.x + (bx + 0.5f) / 16f, y, bx, by);
            }
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            tex.SetPixels32(px);
            tex.Apply(false, true);
            var go = new GameObject("Ground");
            go.transform.SetParent(transform, false);
            go.transform.position = new Vector3(min.x, min.y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.zero, 16f);
            sr.sortingLayerName = "Ground";
            sr.sortingOrder = 0;
        }

        static Color32 Tone(Color32[] pal, float v, int bx, int by)
        {
            float t = Mathf.Clamp01(v) * (pal.Length - 1) + (Bayer[bx & 3, by & 3] - 0.5f) * 0.9f;
            return pal[Mathf.Clamp(Mathf.RoundToInt(t), 0, pal.Length - 1)];
        }

        static float Fbm(float x, float y) =>
            Mathf.Clamp01(Mathf.PerlinNoise(x, y) * 0.6f + Mathf.PerlinNoise(x * 2.3f + 11f, y * 2.3f + 5f) * 0.3f + Mathf.PerlinNoise(x * 5f + 3f, y * 5f + 9f) * 0.1f);

        static float Hash(int x, int y, int s)
        {
            uint h = (uint)(x * 374761393) ^ (uint)(y * 668265263) ^ (uint)(s * 0x5bd1e995);
            h = (h ^ (h >> 13)) * 1274126177u;
            return (h & 0xFFFF) / 65535f;
        }

        static float Ell(float x, float y, float rx, float ry) => Mathf.Sqrt((x * x) / (rx * rx) + (y * y) / (ry * ry));

        static float SegDist(Vector2 p, Vector2 a, Vector2 b)
        {
            var ab = b - a;
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / Mathf.Max(0.0001f, ab.sqrMagnitude));
            return Vector2.Distance(p, a + ab * t);
        }

        static Vector2 Along(Vector2[] pts, float t)
        {
            float f = t * (pts.Length - 1);
            int i = Mathf.Clamp(Mathf.FloorToInt(f), 0, pts.Length - 2);
            return Vector2.Lerp(pts[i], pts[i + 1], f - i);
        }

        static Color32 H(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);

        string Pick(string[] names) => names[_rng.Next(names.Length)];

        static readonly Dictionary<string, Sprite> PropArt = new Dictionary<string, Sprite>();

        static Sprite Art(string name)
        {
            if (PropArt.TryGetValue(name, out var s)) return s;
#if UNITY_EDITOR
            string[] folders = { "Assets/Art/GoblinCampPhase4/", "Assets/Art/Environment/" };
            foreach (var f in folders)
            {
                s = AssetDatabase.LoadAssetAtPath<Sprite>(f + name + ".png");
                if (s != null) break;
            }
#endif
            PropArt[name] = s;
            return s;
        }

        static Sprite One(string stage, string file)
        {
            var sheet = DungeonFenceMaze.Art(stage, file);
            return sheet.Length > 0 ? sheet[0] : null;
        }

        /// <summary>Camp prop with Y-sorting (SpriteSorter) and a small foot collider, like the scene props.</summary>
        SpriteRenderer Prop(string name, Vector2 at, bool collide, bool tree = false, bool sort = true, int order = 0, float scale = 1f)
        {
            var sprite = Art(name);
            var go = new GameObject(name);
            go.transform.SetParent(_props, false);
            go.transform.position = at;
            go.transform.localScale = new Vector3(scale, scale, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            if (sprite == null) return sr;
            if (sort) go.AddComponent<SpriteSorter>();
            else
            {
                sr.sortingLayerName = "Ground";
                sr.sortingOrder = order;
            }
            if (collide)
            {
                var b = sprite.bounds;
                var box = go.AddComponent<BoxCollider2D>();
                box.size = new Vector2(Mathf.Max(0.4f, b.size.x * (tree ? 0.18f : 0.6f)), 0.4f);
                box.offset = new Vector2(b.center.x, b.min.y + 0.22f);
            }
            if (tree) go.AddComponent<TreeCanopyOcclusion>();
            return sr;
        }

        void Torch(Vector2 at)
        {
            var stake = Prop("Stake", at, true);
            Point(stake.transform, new Color(1f, 0.66f, 0.32f), 1.3f, 3.6f).transform.localPosition = new Vector3(0f, 0.9f, 0f);
        }

        static Light2D Point(Transform parent, Color color, float intensity, float radius)
        {
            var go = new GameObject("Light");
            go.transform.SetParent(parent, false);
            var l = go.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Point;
            l.color = color;
            l.intensity = intensity;
            l.pointLightOuterRadius = radius;
            l.pointLightInnerRadius = radius * 0.25f;
            return l;
        }

        /// <summary>Map-wide light; the scene has none, so every map that adds point lights needs one.</summary>
        void Global(Color color, float intensity)
        {
            var go = new GameObject("GlobalLight");
            go.transform.SetParent(transform, false);
            var l = go.AddComponent<Light2D>();
            l.lightType = Light2D.LightType.Global;
            l.color = color;
            l.intensity = intensity;
        }

        Transform _walls;

        Transform WallRoot()
        {
            if (_walls != null) return _walls;
            _walls = new GameObject("Walls").transform;
            _walls.SetParent(transform, false);
            return _walls;
        }

        void Box(Vector2 centre, Vector2 size)
        {
            var go = new GameObject("Wall");
            go.transform.SetParent(WallRoot(), false);
            go.transform.position = centre;
            go.AddComponent<BoxCollider2D>().size = size;
        }

        void Walls(Vector2 min, Vector2 max, float cell, System.Func<Vector2, bool> blocked)
        {
            for (float y = min.y; y <= max.y; y += cell)
                for (float x = min.x; x <= max.x; x += cell)
                    if (blocked(new Vector2(x, y))) Box(new Vector2(x, y), new Vector2(cell, cell));
            RectWall(min, max);
        }

        void RectWall(Vector2 min, Vector2 max)
        {
            var edge = WallRoot().gameObject.AddComponent<EdgeCollider2D>();
            edge.points = new[] { min, new Vector2(max.x, min.y), max, new Vector2(min.x, max.y), min };
        }

        void EllipseWall(Vector2 c, float rx, float ry, bool gapBelow = false)
        {
            var pts = new List<Vector2>();
            const int n = 64;
            for (int i = 0; i <= n; i++)
            {
                float a = -Mathf.PI * 0.5f + i / (float)n * Mathf.PI * 2f;
                var p = c + new Vector2(Mathf.Cos(a) * rx, Mathf.Sin(a) * ry);
                // Leave the south entrance open (stage 5 path); its own boxes close it further down.
                if (gapBelow && Mathf.Abs(p.x) < 2.8f && p.y < c.y) { if (pts.Count > 0) Edge(pts); pts.Clear(); continue; }
                pts.Add(p);
            }
            if (pts.Count > 1) Edge(pts);
        }

        void Edge(List<Vector2> pts)
        {
            if (pts.Count < 2) return;
            var edge = WallRoot().gameObject.AddComponent<EdgeCollider2D>();
            edge.points = pts.ToArray();
        }
    }
}
