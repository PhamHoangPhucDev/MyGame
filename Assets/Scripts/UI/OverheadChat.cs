using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>
    /// Cream pixel speech bubble on WorldLabelsCanvas, tail down, just above a speaker's head.
    /// The box fits the line, the text types in, the bubble pops in and fades out.
    /// Chicken peck and the player chat line both call Say.
    /// </summary>
    public class OverheadChat : MonoBehaviour
    {
        const int MaxLen = 42;
        const int Px = 3;               // UI px per bubble art px
        const float MaxTextWidth = 312f;
        const float MinTextWidth = 24f;
        const float PadX = 12f;
        const float PadY = 9f;
        const float HeadGap = 3f;
        const float EdgeMargin = 8f;
        const float TypeRate = 40f;     // characters per second
        const float PopTime = 0.14f;
        const float FadeTime = 0.25f;

        static readonly Color32 Cream = new Color32(0xF6, 0xEF, 0xE2, 255);
        static readonly Color32 Shade = new Color32(0xE2, 0xD5, 0xBE, 255);
        static readonly Color32 Shine = new Color32(0xFF, 0xFB, 0xF2, 255);
        static readonly Color32 Ink = new Color32(0x2A, 0x22, 0x28, 255);
        static readonly Color ShadowTint = new Color(0f, 0f, 0f, 0.28f);

        class Bubble
        {
            public Transform Who;
            public string Line;
            public float Born;
            public float Until;
            public float HeadY;
            public RectTransform Root;
            public RectTransform Tail;
            public RectTransform TailShadow;
            public CanvasGroup Group;
            public Text Label;
        }

        static OverheadChat _host;
        static Sprite _box;
        static Sprite _body;
        static Sprite _tail;
        static AudioClip _cluck;
        static AudioClip _quack;
        readonly List<Bubble> _bubbles = new List<Bubble>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _host = null;
            _box = null;
            _body = null;
            _tail = null;
            _cluck = null;
            _quack = null;
        }

        public static Sprite Panel => Box();

        public static void Say(Transform who, string text, float seconds)
        {
            if (who == null) return;
            text = Clean(text);
            if (text.Length == 0) return;
            Ensure().Show(who, text, seconds);
        }

        public static void PlayCluck(Vector3 world)
        {
            float gain = GameAudio.SfxGain;
            if (gain <= 0.01f) return;
            AudioSource.PlayClipAtPoint(Cluck(), world, Mathf.Clamp(gain * 0.85f, 0.05f, 0.75f));
        }

        public static void PlayQuack(Vector3 world)
        {
            float gain = GameAudio.SfxGain;
            if (gain <= 0.01f) return;
            AudioSource.PlayClipAtPoint(Quack(), world, Mathf.Clamp(gain * 0.9f, 0.05f, 0.8f));
        }

        static OverheadChat Ensure()
        {
            if (_host != null) return _host;
            var canvas = WorldLabelsCanvas.Ensure();
            var go = new GameObject("OverheadChat");
            go.transform.SetParent(canvas.transform, false);
            _host = go.AddComponent<OverheadChat>();
            return _host;
        }

        void Show(Transform who, string text, float seconds)
        {
            Bubble bubble = null;
            for (int i = 0; i < _bubbles.Count; i++)
            {
                if (_bubbles[i].Who == who)
                {
                    bubble = _bubbles[i];
                    break;
                }
            }
            bool fresh = bubble == null;
            if (fresh)
            {
                bubble = Build();
                bubble.Who = who;
                _bubbles.Add(bubble);
            }
            // A new line on an open bubble types in again but does not pop again.
            bubble.Born = fresh ? Time.time : Time.time - PopTime;
            bubble.Line = text;
            bubble.HeadY = Head(who); // measured once per line, so the bubble does not bob with walk frames
            bubble.Until = Time.time + text.Length / TypeRate + Mathf.Clamp(seconds, 0.6f, 8f);
            bubble.Label.text = Typed(text, 0);
            bubble.Group.alpha = 1f;
            bubble.Root.gameObject.SetActive(true);
            Fit(bubble);
            Place(bubble);
        }

        Bubble Build()
        {
            var canvas = WorldLabelsCanvas.Ensure();
            var rootGo = new GameObject("ChatBubble");
            rootGo.transform.SetParent(canvas.Root, false);
            var root = rootGo.AddComponent<RectTransform>();
            root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
            root.pivot = new Vector2(0.5f, 0f);
            var group = rootGo.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;

            // Drop shadow one art px down, then box, then the tail on top so it covers the box's bottom edge.
            Stretch(Img("Shadow", root, Body(), ShadowTint, true), new Vector2(0f, -Px), new Vector2(0f, -Px));
            var tailShadow = TailRect(Img("TailShadow", root, TailSprite(), ShadowTint, false));
            Stretch(Img("Box", root, Body(), Color.white, true), Vector2.zero, Vector2.zero);
            var tail = TailRect(Img("Tail", root, TailSprite(), Color.white, false));

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(root, false);
            var textRt = textGo.AddComponent<RectTransform>();
            Stretch(textRt, new Vector2(PadX, PadY), new Vector2(-PadX, -PadY));
            var label = textGo.AddComponent<Text>();
            label.font = canvas.PixelFont;
            label.fontSize = UiKit.Fs(16);
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Ink;
            label.supportRichText = true;
            label.lineSpacing = 1.1f;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Overflow;
            label.raycastTarget = false;

            return new Bubble { Root = root, Tail = tail, TailShadow = tailShadow, Group = group, Label = label };
        }

        static RectTransform Img(string name, Transform parent, Sprite sprite, Color tint, bool sliced)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            var img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = tint;
            img.type = sliced ? Image.Type.Sliced : Image.Type.Simple;
            img.raycastTarget = false;
            return rt;
        }

        static void Stretch(RectTransform rt, Vector2 min, Vector2 max)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = min;
            rt.offsetMax = max;
        }

        static RectTransform TailRect(RectTransform rt)
        {
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.sizeDelta = new Vector2(TailW * Px, TailH * Px);
            return rt;
        }

        /// <summary>Box hugs the line: one row up to MaxTextWidth, then wraps. Sizes stay on the art-px grid.</summary>
        static void Fit(Bubble bubble)
        {
            var label = bubble.Label;
            string line = bubble.Line;
            if (label.font != null)
                label.font.RequestCharactersInTexture(line, label.fontSize, label.fontStyle);
            var gen = label.cachedTextGeneratorForLayout;
            float ppu = label.pixelsPerUnit > 0f ? label.pixelsPerUnit : 1f;

            var one = label.GetGenerationSettings(Vector2.zero);
            one.horizontalOverflow = HorizontalWrapMode.Overflow;
            float textW = Mathf.Ceil(gen.GetPreferredWidth(line, one) / ppu);
            textW = Mathf.Clamp(textW, MinTextWidth, MaxTextWidth);

            var wrap = label.GetGenerationSettings(new Vector2(textW, 0f));
            wrap.horizontalOverflow = HorizontalWrapMode.Wrap;
            float textH = Mathf.Ceil(gen.GetPreferredHeight(line, wrap) / ppu);
            textH = Mathf.Max(textH, label.fontSize);

            float w = Snap(textW + PadX * 2f + 2f);
            float h = Snap(textH + PadY * 2f);
            w = Mathf.Max(w, (TailW + 6) * Px);
            bubble.Root.sizeDelta = new Vector2(w, h);
        }

        static float Snap(float v) => Mathf.Ceil(v / Px) * Px;

        void LateUpdate()
        {
            var canvas = WorldLabelsCanvas.Instance;
            if (canvas == null) return;
            for (int i = _bubbles.Count - 1; i >= 0; i--)
            {
                Bubble bubble = _bubbles[i];
                if (bubble.Who == null || Time.time >= bubble.Until)
                {
                    if (bubble.Root != null) Destroy(bubble.Root.gameObject);
                    _bubbles.RemoveAt(i);
                    continue;
                }
                Animate(bubble);
                Place(bubble);
            }
        }

        static void Animate(Bubble bubble)
        {
            float age = Time.time - bubble.Born;
            int shown = Mathf.Clamp(Mathf.FloorToInt((age - PopTime * 0.5f) * TypeRate), 0, bubble.Line.Length);
            string typed = Typed(bubble.Line, shown);
            if (bubble.Label.text != typed) bubble.Label.text = typed;

            // Pop with a small overshoot (ease-out-back), then rest at exactly 1 so the pixels stay crisp.
            float u = Mathf.Clamp01(age / PopTime);
            float k = u - 1f;
            float s = u >= 1f ? 1f : 0.6f + 0.4f * (1f + 2.7f * k * k * k + 1.7f * k * k);
            bubble.Root.localScale = new Vector3(s, s, 1f);

            float left = bubble.Until - Time.time;
            bubble.Group.alpha = Mathf.Clamp01(Mathf.Min(u * 2f, left / FadeTime));
        }

        /// <summary>Tail tip sits on the head; the box slides to stay on screen while the tail keeps pointing at the speaker.</summary>
        static void Place(Bubble bubble)
        {
            var canvas = WorldLabelsCanvas.Instance;
            if (canvas == null) return;
            Vector3 world = bubble.Who.position + new Vector3(0f, bubble.HeadY, 0f);
            Vector2 screen = WorldLabelsCanvas.WorldToSnappedScreen(world);
            if (screen.x < -1000f)
            {
                bubble.Root.gameObject.SetActive(false);
                return;
            }
            bubble.Root.gameObject.SetActive(true);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.Root, screen, null, out Vector2 tip);

            Vector2 size = bubble.Root.sizeDelta;
            Rect area = canvas.Root.rect;
            float half = size.x * 0.5f;
            float x = tip.x;
            float minX = area.xMin + EdgeMargin + half;
            float maxX = area.xMax - EdgeMargin - half;
            if (minX <= maxX) x = Mathf.Clamp(x, minX, maxX);
            float y = tip.y + HeadGap + (TailH - 1) * Px;
            y = Mathf.Min(y, area.yMax - EdgeMargin - size.y);

            x = Mathf.Round(x);
            y = Mathf.Round(y);
            bubble.Root.anchoredPosition = new Vector2(x, y);

            float reach = half - (TailW * 0.5f + 2f) * Px;
            float tailX = Mathf.Round(Mathf.Clamp(tip.x - x, -reach, reach) / Px) * Px;
            float tailY = Px; // overlap the box's bottom rim by one art px
            bubble.Tail.anchoredPosition = new Vector2(tailX, tailY);
            bubble.TailShadow.anchoredPosition = new Vector2(tailX, tailY - Px);
        }

        /// <summary>Top of the speaker's body. Gear on the back (Equip_*), swords, shadows and emotes do not count.</summary>
        static float Head(Transform who)
        {
            var own = who.GetComponent<SpriteRenderer>();
            if (own != null && own.sprite != null)
                return Top(own, who) + 0.04f;
            float top = 0f;
            var renderers = who.GetComponentsInChildren<SpriteRenderer>();
            for (int i = 0; i < renderers.Length; i++)
            {
                SpriteRenderer r = renderers[i];
                if (r.sprite == null || !r.enabled) continue;
                string n = r.gameObject.name;
                if (n == "Emote" || n == "ContactShadow") continue;
                if (n.StartsWith("Equip", System.StringComparison.OrdinalIgnoreCase)) continue;
                if (n.IndexOf("Sword", System.StringComparison.OrdinalIgnoreCase) >= 0) continue;
                top = Mathf.Max(top, Top(r, who));
            }
            if (top <= 0.01f) top = 1.15f;
            return top + 0.04f;
        }

        /// <summary>Highest opaque point of a sprite (tight mesh), not its padded rect.</summary>
        static float Top(SpriteRenderer r, Transform who)
        {
            var verts = r.sprite.vertices;
            if (verts == null || verts.Length == 0) return r.bounds.max.y - who.position.y;
            Transform t = r.transform;
            float top = float.MinValue;
            for (int i = 0; i < verts.Length; i++)
            {
                Vector2 v = verts[i];
                if (r.flipX) v.x = -v.x;
                if (r.flipY) v.y = -v.y;
                top = Mathf.Max(top, t.TransformPoint(v).y);
            }
            return top - who.position.y;
        }

        /// <summary>Typed part shows; the rest stays in the string but clear, so the layout never jumps while typing.</summary>
        static string Typed(string line, int shown)
        {
            if (shown >= line.Length) return line;
            return line.Substring(0, shown) + "<color=#00000000>" + line.Substring(shown) + "</color>";
        }

        static string Clean(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            text = text.Replace('\n', ' ').Replace('\r', ' ').Replace('<', '‹').Replace('>', '›').Trim();
            if (text.Length > MaxLen) text = text.Substring(0, MaxLen);
            return text;
        }

        /// <summary>Plain square panel used by the chat input bar (PlayerChat). Unchanged.</summary>
        static Sprite Box()
        {
            if (_box != null) return _box;
            const int s = 16;
            var tex = NewTex(s, s);
            var cream = new Color32(0xF4, 0xE6, 0xC8, 255);
            var ink = new Color32(0x1A, 0x14, 0x10, 255);
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                bool edge = x < 2 || y < 2 || x >= s - 2 || y >= s - 2;
                px[y * s + x] = edge ? ink : cream;
            }
            tex.SetPixels32(px);
            tex.Apply();
            _box = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(4, 4, 4, 4));
            return _box;
        }

        const int BodySize = 9;

        /// <summary>Bubble body, 9x9 art px, 9-sliced: rounded 1-px ink rim, light top row, shaded bottom row.</summary>
        static Sprite Body()
        {
            if (_body != null) return _body;
            const int s = BodySize;
            var tex = NewTex(s, s);
            var px = new Color32[s * s];
            for (int y = 0; y < s; y++)
            for (int x = 0; x < s; x++)
            {
                bool xEdge = x == 0 || x == s - 1;
                bool yEdge = y == 0 || y == s - 1;
                bool xIn = x == 1 || x == s - 2;
                bool yIn = y == 1 || y == s - 2;
                Color32 c;
                if (xEdge && yEdge) c = default;          // cut corner
                else if (xEdge || yEdge) c = Ink;          // rim
                else if (xIn && yIn) c = Ink;              // inner corner step = rounded look
                else if (y == 1) c = Shade;                // bottom shade
                else if (y == s - 2) c = Shine;            // top shine
                else c = Cream;
                px[y * s + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply();
            _body = Sprite.Create(tex, new Rect(0, 0, s, s), new Vector2(0.5f, 0.5f), 100f / Px, 0, SpriteMeshType.FullRect, new Vector4(3, 3, 3, 3));
            return _body;
        }

        const int TailW = 9;
        const int TailH = 5;

        /// <summary>Tail, 9x5 art px. Its top row sits on the box's bottom rim and repaints it as the shade row.</summary>
        static Sprite TailSprite()
        {
            if (_tail != null) return _tail;
            string[] rows =
            {
                "ISSSSSSSI", // top: covers the box rim
                ".ICCCCCI.",
                "..ICCCI..",
                "...ICI...",
                "....I....",
            };
            var tex = NewTex(TailW, TailH);
            var px = new Color32[TailW * TailH];
            for (int r = 0; r < TailH; r++)
            for (int x = 0; x < TailW; x++)
            {
                char ch = rows[r][x];
                Color32 c = ch == 'I' ? Ink : ch == 'S' ? Shade : ch == 'C' ? Cream : default;
                px[(TailH - 1 - r) * TailW + x] = c;
            }
            tex.SetPixels32(px);
            tex.Apply();
            _tail = Sprite.Create(tex, new Rect(0, 0, TailW, TailH), new Vector2(0.5f, 1f), 100f / Px);
            return _tail;
        }

        static Texture2D NewTex(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Point;
            tex.wrapMode = TextureWrapMode.Clamp;
            return tex;
        }

        static AudioClip Cluck()
        {
            if (_cluck != null) return _cluck;
            const int rate = 22050;
            int n = (int)(rate * 0.28f);
            var data = new float[n];
            Burst(data, rate, 0.01f, 0.07f, 740f, 0.55f);
            Burst(data, rate, 0.12f, 0.1f, 460f, 0.42f);
            _cluck = AudioClip.Create("cuc-tac", n, 1, rate, false);
            _cluck.SetData(data, 0);
            return _cluck;
        }

        static AudioClip Quack()
        {
            if (_quack != null) return _quack;
            const int rate = 22050;
            int n = (int)(rate * 0.34f);
            var data = new float[n];
            Burst(data, rate, 0.01f, 0.1f, 420f, 0.62f);
            Burst(data, rate, 0.16f, 0.12f, 300f, 0.5f);
            _quack = AudioClip.Create("cap-cap", n, 1, rate, false);
            _quack.SetData(data, 0);
            return _quack;
        }

        static void Burst(float[] data, int rate, float start, float dur, float freq, float amp)
        {
            int i0 = Mathf.Clamp((int)(start * rate), 0, data.Length - 1);
            int i1 = Mathf.Clamp(i0 + (int)(dur * rate), i0 + 1, data.Length);
            for (int i = i0; i < i1; i++)
            {
                float u = (i - i0) / (float)(i1 - i0);
                float env = Mathf.Sin(u * Mathf.PI);
                float wob = 1f + 0.1f * Mathf.Sin(u * 36f);
                data[i] += Mathf.Sin(i * (freq * wob) * Mathf.PI * 2f / rate) * amp * env;
            }
        }
    }
}
