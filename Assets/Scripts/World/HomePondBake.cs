using System.Collections.Generic;
using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Dug pond art v2 (approved 2026-10-30, Docs/WorldArt/HomeDistrict/farm/FARM_POND_SPEC.md).
    /// Rasterises the HomeLand water vertices per pixel: marching-squares field, low-frequency warp, rounded corners,
    /// earth bank on the far shore, depth shading, lily pads / reeds / stones by hash and 4 glint frames.
    /// Reference algorithm: Docs/WorldArt/HomeDistrict/farm/tools/gen_pond.cs. Visual only: colliders stay in HomeDistrict.
    /// </summary>
    public static class HomePondBake
    {
        const int P = 16;            // px per unit
        const int Margin = 2;        // cells of room around the water for the bank, reeds and the warp

        static Color32 C(int rgb) => new Color32((byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb, 255);
        static readonly Color32 Clear = new Color32(0, 0, 0, 0);
        static readonly Color32 EgDark = C(0x507868), EgMid = C(0x608878);
        static readonly Color32 Face1 = C(0x9A6E48), Face2 = C(0x7E5A3A), FaceDk = C(0x5E4230);
        static readonly Color32 WShallow = C(0x6AA8BE), WMid = C(0x5E9AB4), WDeep = C(0x4C86A0), WDeepest = C(0x427894), WShade = C(0x3E6A84);
        static readonly Color32 Foam = C(0x8CC0D4), FoamHi = C(0xD6EEF4);
        static readonly Color32 Stone = C(0x9A948C), StoneLt = C(0xB2ACA4), StoneDk = C(0x6E6862), Out = C(0x3A2A26);
        static readonly Color32[] Leaf = { C(0xB0D07A), C(0x98C26A), C(0x7EAE5A), C(0x5E9050), C(0x46765A) };
        static readonly Color32 FlPink = C(0xE08AA8), FlWhite = C(0xEEEAD8), FlYellow = C(0xE8C850), Cattail = C(0x7A4A30);
        static readonly int[] Bayer = { 0, 8, 2, 10, 12, 4, 14, 6, 3, 11, 1, 9, 15, 7, 13, 5 };
        const int Seed = 7;

        static int Hash(int x, int y, int s) { unchecked { int h = x * 374761393 + y * 668265263 + s * 982451653; h = (h ^ (h >> 13)) * 1274126177; return (h ^ (h >> 16)) & 0x7fffffff; } }
        static float Smooth(float t) => t * t * (3f - 2f * t);
        static float Noise(int x, int y, int size, int seed)
        {
            int x0 = Mathf.FloorToInt((float)x / size), y0 = Mathf.FloorToInt((float)y / size);
            float fx = Smooth((x - x0 * size) / (float)size), fy = Smooth((y - y0 * size) / (float)size);
            float a = Hash(x0, y0, seed) % 1000 / 999f, b = Hash(x0 + 1, y0, seed) % 1000 / 999f;
            float c = Hash(x0, y0 + 1, seed) % 1000 / 999f, d = Hash(x0 + 1, y0 + 1, seed) % 1000 / 999f;
            float top = a + (b - a) * fx, bot = c + (d - c) * fx;
            return top + (bot - top) * fy;
        }

        /// <summary>Builds the pond sprite (4 glint frames) under host. Null when there is no water.</summary>
        public static GameObject Build(Transform host, int sortingOrder)
        {
            // water vertices from the per-cell corner masks (HomeLand keeps the set private)
            var verts = new HashSet<Vector2Int>();
            foreach (var c in HomeLand.TileCells())
            {
                int m = HomeLand.Mask(c);
                if ((m & 1) != 0) verts.Add(new Vector2Int(c.x, c.y + 1));
                if ((m & 2) != 0) verts.Add(new Vector2Int(c.x + 1, c.y + 1));
                if ((m & 4) != 0) verts.Add(new Vector2Int(c.x + 1, c.y));
                if ((m & 8) != 0) verts.Add(new Vector2Int(c.x, c.y));
            }
            if (verts.Count == 0) return null;
            int minX = int.MaxValue, minY = int.MaxValue, maxX = int.MinValue, maxY = int.MinValue;
            foreach (var v in verts) { minX = Mathf.Min(minX, v.x); minY = Mathf.Min(minY, v.y); maxX = Mathf.Max(maxX, v.x); maxY = Mathf.Max(maxY, v.y); }
            int ox = minX - Margin, oyTop = maxY + Margin;                      // local x of column 0, local y of row 0 (top edge)
            int cw = maxX - minX + 2 * Margin, ch = maxY - minY + 2 * Margin;
            int w = cw * P, h = ch * P;

            // ---- mask (rows go DOWN in this method; flipped when written to the texture) ----
            var m0 = new bool[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                int ax = ox * P + x, ay = -oyTop * P + y;                       // absolute px so warp and hashes are stable
                float wx = x + (Noise(ax, ay, 22, Seed + 1) - 0.5f) * 8f + (Noise(ax, ay, 6, Seed + 3) - 0.5f) * 1.5f;
                float wy = y + (Noise(ax, ay, 22, Seed + 2) - 0.5f) * 8f + (Noise(ax, ay, 6, Seed + 4) - 0.5f) * 1.5f;
                m0[y * w + x] = Field(verts, ox + (wx + 0.5f) / P, oyTop - (wy + 0.5f) / P) >= 0.5f;
            }
            m0 = Morph(Morph(m0, w, h, 5, true), w, h, 5, false);   // closing: round inner corners
            m0 = Morph(Morph(m0, w, h, 6, false), w, h, 6, true);   // opening: round outer corners
            var mk = m0;
            bool M(int x, int y) => x >= 0 && y >= 0 && x < w && y < h && mk[y * w + x];
            var d = Depth(mk, w, h);

            var px = new Color32[w * h];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                int i = y * w + x, ax = ox * P + x, ay = -oyTop * P + y;
                if (!mk[i])
                {
                    bool n1 = M(x - 1, y) || M(x + 1, y) || M(x, y - 1) || M(x, y + 1);
                    bool n2 = !n1 && (M(x - 2, y) || M(x + 2, y) || M(x, y + 2) || M(x, y - 2));
                    px[i] = n1 ? EgDark : (n2 && Hash(ax, ay, Seed) % 3 != 0 ? EgMid : Clear);
                    continue;
                }
                int up = 0; while (up < 8 && M(x, y - up - 1)) up++;
                Color32 c;
                if (up == 0) c = Face1;
                else if (up == 1) c = Face2;
                else if (up == 2) c = FaceDk;
                else if (up <= 4) c = WShade;
                else
                {
                    float t = Mathf.Min(1f, (d[i] - 1) / 14f) + (Noise(ax, ay, 12, Seed + 9) - 0.5f) * 0.35f + (Bayer[(ay & 3) * 4 + (ax & 3)] / 16f - 0.5f) * 0.18f;
                    c = t < 0.22f ? WShallow : t < 0.66f ? WMid : t < 0.92f ? WDeep : WDeepest;
                }
                if (up > 2 && (!M(x - 1, y) || !M(x + 1, y))) c = Face2;
                if (up > 2 && !M(x, y + 1)) c = Foam;
                else if (up > 2 && !M(x, y + 2) && Hash(ax, ay, Seed + 5) % 4 != 0) c = WShallow;
                px[i] = c;
            }

            void S(int x, int y, Color32 col) { if (x >= 0 && y >= 0 && x < w && y < h) px[y * w + x] = col; }
            // lily pads: about 1 dug cell in 6, only on open water away from the shade band
            for (int j = 0; j < ch; j++) for (int ii = 0; ii < cw; ii++)
            {
                int ci = ox + ii, cj = oyTop - j;                                // absolute cell ids for stable hashes
                if (Hash(ci >> 1, cj >> 1, Seed + 20) % 3 != 0 || Hash(ci, cj, Seed + 25) % 2 != 0) continue;
                int cx = ii * P + 4 + Hash(ci, cj, Seed + 21) % 8, cy = j * P + 5 + Hash(ci, cj, Seed + 22) % 7;
                if (!M(cx, cy) || d[cy * w + cx] < 5 || !M(cx, cy - 6)) continue;
                for (int y = -2; y <= 2; y++) for (int x = -3; x <= 3; x++)
                    if (x * x / 9f + y * y / 4f <= 1f && !(x == 1 && y < 0)) S(cx + x, cy + y, y >= 1 ? Leaf[3] : (x < 0 ? Leaf[1] : Leaf[2]));
                S(cx - 3, cy + 1, Leaf[4]); S(cx + 3, cy + 1, Leaf[4]);
                if (Hash(ci, cj, Seed + 23) % 2 == 0)
                {
                    var fl = Hash(ci, cj, Seed + 24) % 2 == 0 ? FlPink : FlWhite;
                    S(cx - 1, cy - 1, fl); S(cx, cy - 2, fl); S(cx + 1, cy - 1, fl); S(cx, cy - 1, FlYellow);
                }
            }
            // reeds: one clump per 20 px run of the far / side shore
            var used = new HashSet<int>();
            for (int y = 2; y < h - 2; y++) for (int x = 2; x < w - 2; x++)
            {
                bool shore = !M(x, y) && (M(x, y + 1) || ((M(x + 1, y) || M(x - 1, y)) && M(x, y + 4)));
                if (!shore) continue;
                int ax = ox * P + x, ay = -oyTop * P + y;
                int bx20 = Mathf.FloorToInt(ax / 20f), by20 = Mathf.FloorToInt(ay / 20f);
                int key = by20 * 100000 + bx20;
                if (used.Contains(key) || Hash(bx20, by20, Seed + 30) % 3 != 0 || Hash(ax, ay, Seed + 33) % 6 != 0) continue;
                used.Add(key);
                int n = 3 + Hash(ax, ay, Seed + 34) % 3;
                for (int b = 0; b < n; b++)
                {
                    int rx = x - n + b * 2, hgt = 5 + Hash(ox * P + rx, ay, Seed + 31) % 5, lean = b < n / 2 ? -1 : 1;
                    for (int k = 0; k < hgt; k++) S(rx + (k > hgt - 3 ? lean : 0), y + 1 - k, k < 2 ? Leaf[4] : (b % 2 == 0 ? Leaf[3] : Leaf[2]));
                    if (hgt >= 8 && Hash(ox * P + rx, ay, Seed + 32) % 2 == 0) { S(rx, y + 1 - hgt, Cattail); S(rx, y + 2 - hgt, Cattail); S(rx, y + 3 - hgt, Cattail); }
                }
            }
            // stones half sunk in the near shore: one per 24 px run at most
            used.Clear();
            for (int y = 2; y < h - 2; y++) for (int x = 3; x < w - 6; x++)
            {
                if (!M(x, y) || M(x, y + 1)) continue;
                int ax = ox * P + x, ay = -oyTop * P + y;
                int bx24 = Mathf.FloorToInt(ax / 24f), by24 = Mathf.FloorToInt(ay / 24f);
                int key = by24 * 100000 + bx24;
                if (used.Contains(key) || Hash(bx24, by24, Seed + 40) % 2 != 0 || Hash(ax, ay, Seed + 41) % 5 != 0) continue;
                used.Add(key);
                int sw = 5 + Hash(ax, ay, Seed + 42) % 3;
                for (int yy = 0; yy < 4; yy++) for (int xx = 0; xx < sw; xx++)
                {
                    if ((yy == 0 || yy == 3) && (xx == 0 || xx == sw - 1)) continue;
                    S(x + xx, y - 2 + yy, yy == 0 ? StoneLt : yy == 3 ? StoneDk : Stone);
                }
                for (int xx = 1; xx < sw - 1; xx++) S(x + xx, y + 2, Out);
                S(x + 1, y - 1, StoneLt);
                if (Hash(ax, ay, Seed + 43) % 2 == 0) { S(x + sw, y + 1, Stone); S(x + sw + 1, y + 1, StoneDk); S(x + sw, y + 2, Out); }
            }

            // 4 glint frames over the finished base (glints sit on deep water only)
            var go = new GameObject("DugPondBaked");
            go.transform.SetParent(host, false);
            go.transform.localPosition = new Vector3(ox, oyTop - ch, 0f);           // sprite pivot = bottom-left
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;
            var frames = new Sprite[4];
            var buf = new Color32[w * h];
            for (int f = 0; f < 4; f++)
            {
                System.Array.Copy(px, buf, px.Length);
                for (int gy = 6; gy < h; gy += 15) for (int gx = 5; gx < w; gx += 23)
                {
                    int ax = ox * P + gx, ay = -oyTop * P + gy;
                    int gxx = gx + Hash(ax, ay, Seed + 11) % 7, gyy = gy + Hash(ax, ay, Seed + 12) % 5;
                    if (!M(gxx, gyy) || d[gyy * w + gxx] < 5) continue;
                    if (buf[gyy * w + gxx].g > 0xB0) continue;                       // never over a lily pad
                    int ph = (f + Hash(ax, ay, Seed + 13)) % 4;
                    int len = ph == 0 ? 2 : ph == 1 ? 4 : ph == 2 ? 3 : 0;
                    for (int k = 0; k < len; k++) if (M(gxx + k, gyy)) buf[gyy * w + gxx + k] = k == 1 && ph == 1 ? FoamHi : Foam;
                    if (ph == 1 && M(gxx + 1, gyy - 1)) buf[(gyy - 1) * w + gxx + 1] = Foam;
                }
                var flip = new Color32[w * h];
                for (int y = 0; y < h; y++) System.Array.Copy(buf, y * w, flip, (h - 1 - y) * w, w);
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, name = "DugPond_" + f };
                tex.SetPixels32(flip);
                tex.Apply(false, true);
                frames[f] = Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.zero, P, 0, SpriteMeshType.FullRect);
            }
            sr.sprite = frames[0];
            go.AddComponent<DugPondFx>().frames = frames;
            return go;
        }

        /// <summary>Bilinear corner occupancy at a local point (cell corners are the water vertices).</summary>
        static float Field(HashSet<Vector2Int> verts, float lx, float ly)
        {
            int i = Mathf.FloorToInt(lx), j = Mathf.FloorToInt(ly);
            float fu = lx - i, fv = ly - j;
            float a = verts.Contains(new Vector2Int(i, j)) ? 1f : 0f, b = verts.Contains(new Vector2Int(i + 1, j)) ? 1f : 0f;
            float c = verts.Contains(new Vector2Int(i, j + 1)) ? 1f : 0f, e = verts.Contains(new Vector2Int(i + 1, j + 1)) ? 1f : 0f;
            float bot = a + (b - a) * fu, top = c + (e - c) * fu;
            return bot + (top - bot) * fv;
        }

        static bool[] Morph(bool[] m, int w, int h, int r, bool grow)
        {
            var o = new bool[m.Length];
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                bool hit = !grow;
                for (int dy = -r; dy <= r && hit != grow; dy++) for (int dx = -r; dx <= r; dx++)
                {
                    if (dx * dx + dy * dy > r * r + r) continue;
                    int xx = x + dx, yy = y + dy;
                    bool v = xx >= 0 && yy >= 0 && xx < w && yy < h && m[yy * w + xx];
                    if (grow && v) { hit = true; break; }
                    if (!grow && !v) { hit = false; break; }
                }
                o[y * w + x] = hit;
            }
            return o;
        }

        /// <summary>Chamfer distance (px) from each water pixel to the nearest dry pixel.</summary>
        static int[] Depth(bool[] m, int w, int h)
        {
            var d = new int[m.Length];
            for (int i = 0; i < d.Length; i++) d[i] = m[i] ? 9999 : 0;
            for (int y = 0; y < h; y++) for (int x = 0; x < w; x++)
            {
                int i = y * w + x; if (d[i] == 0) continue;
                d[i] = x > 0 ? Mathf.Min(d[i], d[i - 1] + 2) : 2;
                d[i] = y > 0 ? Mathf.Min(d[i], d[i - w] + 2) : 2;
                if (x > 0 && y > 0) d[i] = Mathf.Min(d[i], d[i - w - 1] + 3);
                if (x < w - 1 && y > 0) d[i] = Mathf.Min(d[i], d[i - w + 1] + 3);
            }
            for (int y = h - 1; y >= 0; y--) for (int x = w - 1; x >= 0; x--)
            {
                int i = y * w + x; if (d[i] == 0) continue;
                d[i] = x < w - 1 ? Mathf.Min(d[i], d[i + 1] + 2) : 2;
                d[i] = y < h - 1 ? Mathf.Min(d[i], d[i + w] + 2) : 2;
                if (x < w - 1 && y < h - 1) d[i] = Mathf.Min(d[i], d[i + w + 1] + 3);
                if (x > 0 && y < h - 1) d[i] = Mathf.Min(d[i], d[i + w - 1] + 3);
            }
            for (int i = 0; i < d.Length; i++) d[i] /= 2;
            return d;
        }
    }

    /// <summary>Cycles the 4 baked glint frames of the dug pond; frees the textures with the pond.</summary>
    public class DugPondFx : MonoBehaviour
    {
        public Sprite[] frames;
        const float Step = 0.18f;
        SpriteRenderer _sr;
        int _shown = -1;

        void Awake() => _sr = GetComponent<SpriteRenderer>();

        void Update()
        {
            if (frames == null || frames.Length == 0 || _sr == null) return;
            int f = (int)(Time.time / Step) % frames.Length;
            if (f == _shown) return;
            _shown = f;
            _sr.sprite = frames[f];
        }

        void OnDestroy()
        {
            if (frames == null) return;
            foreach (var s in frames)
            {
                if (s == null) continue;
                if (s.texture != null) Destroy(s.texture);
                Destroy(s);
            }
        }
    }
}
