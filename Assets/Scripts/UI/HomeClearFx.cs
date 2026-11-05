using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>
    /// Art feedback for clearing Home clutter (UI_TODO section 6). Read-only on HomeClear:
    /// gold hold bar over HomeClear.Target, dust puff + rising "+N [icon]" on every clear,
    /// and the prompt's E keycap feeding HomeClear.RequestHold() while pressed (touch).
    /// </summary>
    public class HomeClearFx : MonoBehaviour
    {
        static HomeClearFx _instance;

        static readonly Color Track = new Color32(0x2A, 0x2A, 0x2A, 0xFF);
        static readonly Color FillGold = new Color32(0xD8, 0xB0, 0x40, 0xFF);
        static readonly Color Gold = new Color32(0xF2, 0xD8, 0x4A, 0xFF);

        const string PuffPath = "World/HomeDistrict/Wild/WL_Puff";
        const float PuffFps = 12f, FloatSeconds = 1f, FloatRise = 12f;
        const int BarW = 24, BarH = 3;

        RectTransform _canvas, _bar, _fill;
        Sprite[] _puff;
        float _nextHook;
        readonly List<Puff> _puffs = new List<Puff>();
        readonly List<Floater> _floats = new List<Floater>();

        struct Puff { public SpriteRenderer sr; public float t0; }
        struct Floater { public RectTransform rt; public CanvasGroup group; public Vector3 world; public float t0; public bool follow; }
        static int _popFrame = -1;
        static string _popItem;
        static int _popAmt;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _instance = null;
            _popFrame = -1;
            _popItem = null;
            _popAmt = 0;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Boot() => Ensure();

        static HomeClearFx Ensure()
        {
            if (_instance != null) return _instance;
            var go = new GameObject("HomeClearFx");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<HomeClearFx>();
            return _instance;
        }

        void OnEnable() => WorldEvents.ItemGained += OnItem;
        void OnDisable() => WorldEvents.ItemGained -= OnItem;

        void EnsureUi()
        {
            if (_canvas != null) return;
            var canvas = UiKit.Canvas("HomeClearFxCanvas", 140, transform);
            canvas.GetComponent<GraphicRaycaster>().enabled = false;
            _canvas = (RectTransform)canvas.transform;
            _bar = UiKit.Rect(_canvas, "HoldBar");
            var track = UiKit.Rect(_bar, "Track").gameObject.AddComponent<Image>();
            track.color = Track;
            track.raycastTarget = false;
            UiKit.Place(track, UiKit.Center, new Vector2(0.5f, 0f), Vector2.zero, new Vector2(BarW, BarH));
            var fill = UiKit.Rect(track.transform, "Fill").gameObject.AddComponent<Image>();
            fill.color = FillGold;
            fill.raycastTarget = false;
            _fill = fill.rectTransform;
            _fill.pivot = new Vector2(0f, 0.5f);
            _fill.offsetMin = Vector2.zero;
            _fill.offsetMax = Vector2.zero;
            _bar.gameObject.SetActive(false);
        }

        void LateUpdate()
        {
            HookKeycap();
            UpdateBar(HomeClear.Target);
            UpdatePuffs();
            UpdateFloats();
        }

        // ---------------------------------------------------------------- hold bar

        void UpdateBar(HomeClearable target)
        {
            if (target == null)
            {
                if (_bar != null) _bar.gameObject.SetActive(false);
                return;
            }
            EnsureUi();
            if (!ToCanvas(Top(target) + Vector3.up * 0.125f, out var local)) { _bar.gameObject.SetActive(false); return; }
            _bar.gameObject.SetActive(true);
            _bar.anchoredPosition = local;
            // Whole kit pixels only, so the fill steps like pixel art.
            float p = Mathf.Round(Mathf.Clamp01(HomeClear.Progress) * BarW) / BarW;
            _fill.anchorMin = Vector2.zero;
            _fill.anchorMax = new Vector2(p, 1f);
        }

        // ---------------------------------------------------------------- clear finished

        void OnItem(string item, int amount)
        {
            var nav = WorldNavigator.Instance;
            bool housing = nav != null && nav.Current == WorldZone.Housing;
            var c = HomeClear.Target;
            bool clearing = c != null && HomeClear.Progress >= 1f
                && HomeClear.Item(c.Kind) == item && HomeClear.Amount(c.Kind) == amount;
            if (clearing)
            {
                var sr = c.GetComponent<SpriteRenderer>();
                Vector3 centre = sr != null ? sr.bounds.center : c.transform.position;
                SpawnPuff(centre, sr);
                if (!housing) { SpawnFloat(Top(c), item, amount, false); return; }
            }
            if (housing) PopupPlayer(item, amount);
        }

        /// <summary>Rising +N [icon] over the hero. Used for every home-map gain.</summary>
        public static void PopupPlayer(string item, int amount)
        {
            if (amount <= 0 || string.IsNullOrEmpty(item)) return;
            Ensure().SpawnFloat(PlayerHead(), item, amount, true);
        }

        void SpawnPuff(Vector3 at, SpriteRenderer source)
        {
            if (_puff == null)
            {
                _puff = Resources.LoadAll<Sprite>(PuffPath);
                System.Array.Sort(_puff, (a, b) => string.CompareOrdinal(a.name, b.name));
            }
            if (_puff.Length == 0) return;
            var go = new GameObject("ClearPuff");
            go.transform.position = new Vector3(at.x, at.y, 0f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = _puff[0];
            if (source != null)
            {
                sr.sortingLayerID = source.sortingLayerID;
                sr.sortingOrder = source.sortingOrder + 1;
            }
            else sr.sortingOrder = 4000 + Mathf.RoundToInt(-at.y * 100f) + 1;
            _puffs.Add(new Puff { sr = sr, t0 = Time.unscaledTime });
        }

        void UpdatePuffs()
        {
            for (int i = _puffs.Count - 1; i >= 0; i--)
            {
                var p = _puffs[i];
                int frame = Mathf.FloorToInt((Time.unscaledTime - p.t0) * PuffFps);
                if (p.sr == null || frame >= _puff.Length)
                {
                    if (p.sr != null) Destroy(p.sr.gameObject);
                    _puffs.RemoveAt(i);
                    continue;
                }
                p.sr.sprite = _puff[frame];
            }
        }

        void SpawnFloat(Vector3 world, string item, int amount, bool follow)
        {
            if (Time.frameCount == _popFrame && _popItem == item && _popAmt == amount) return;
            _popFrame = Time.frameCount;
            _popItem = item;
            _popAmt = amount;
            EnsureUi();
            var row = UiKit.Rect(_canvas, "ClearGain");
            var group = row.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            group.interactable = false;
            string text = "+" + amount;
            var itemDef = WorldContent.FindItem(item);
            string name = Loc.T(string.IsNullOrEmpty(itemDef.displayName) ? item : itemDef.displayName);
            float aw = text.Length * 8f, nw = name.Length * 8f, total = aw + 20f + nw, left = -total / 2f;
            var label = UiKit.Label(row, "Amount", text, 8, Gold, TextAnchor.MiddleRight);
            UiKit.Place(label, UiKit.Center, UiKit.Center, new Vector2(left + aw / 2f, 0f), new Vector2(aw + 2f, 10f));
            var icon = UiKit.Icon(row, UiKit.ItemIcon(item), UiKit.Center, new Vector2(left + aw + 10f, 0f));
            icon.enabled = icon.sprite != null;
            var nameLbl = UiKit.Label(row, "Name", name, 8, Color.white, TextAnchor.MiddleLeft);
            UiKit.Place(nameLbl, UiKit.Center, UiKit.Center, new Vector2(left + aw + 20f + nw / 2f, 0f), new Vector2(nw + 8f, 10f));
            var f = new Floater
            {
                rt = row, group = group, world = world + Vector3.up * 0.25f,
                t0 = Time.unscaledTime, follow = follow
            };
            _floats.Add(f);
            PlaceFloat(f, 0f);
        }

        void UpdateFloats()
        {
            for (int i = _floats.Count - 1; i >= 0; i--)
            {
                var f = _floats[i];
                float t = (Time.unscaledTime - f.t0) / FloatSeconds;
                if (f.rt == null || t >= 1f)
                {
                    if (f.rt != null) Destroy(f.rt.gameObject);
                    _floats.RemoveAt(i);
                    continue;
                }
                PlaceFloat(f, t);
            }
        }

        void PlaceFloat(Floater f, float t)
        {
            Vector3 world = f.follow ? PlayerHead() : f.world;
            if (!ToCanvas(world, out var local)) { f.group.alpha = 0f; return; }
            float rise = Mathf.Round(FloatRise * t);
            int stack = 0;
            for (int i = 0; i < _floats.Count; i++)
                if (_floats[i].rt != f.rt && _floats[i].t0 <= f.t0) stack++;
            f.rt.anchoredPosition = local + new Vector2(0f, (rise + stack * 12f) * UiKit.Px);
            f.group.alpha = t < 0.4f ? 1f : 1f - (t - 0.4f) / 0.6f;
        }

        static Vector3 PlayerHead()
        {
            var hero = GameObject.FindGameObjectWithTag("Player");
            if (hero == null) return Vector3.zero;
            var sr = hero.GetComponent<SpriteRenderer>();
            if (sr == null) sr = hero.GetComponentInChildren<SpriteRenderer>();
            if (sr != null && sr.sprite != null)
                return new Vector3(sr.bounds.center.x, sr.bounds.max.y + 0.2f, 0f);
            return hero.transform.position + Vector3.up * 1.1f;
        }

        // ---------------------------------------------------------------- touch E keycap

        /// <summary>Lets the existing prompt keycap be held on touch screens. Design is unchanged.</summary>
        void HookKeycap()
        {
            if (Time.unscaledTime < _nextHook) return;
            _nextHook = Time.unscaledTime + 0.5f;
            var prompt = GameObject.Find("WorldPrompt");
            if (prompt == null) return;
            var key = prompt.transform.Find("PromptPlate/Key");
            if (key == null || key.GetComponent<ClearHoldRelay>() != null) return;
            var image = key.GetComponent<Image>();
            if (image == null) return;
            // The prompt canvas has raycasts off; a nested canvas makes only the keycap pressable.
            if (key.GetComponent<Canvas>() == null) key.gameObject.AddComponent<Canvas>();
            if (key.GetComponent<GraphicRaycaster>() == null) key.gameObject.AddComponent<GraphicRaycaster>();
            image.raycastTarget = true;
            key.gameObject.AddComponent<ClearHoldRelay>();
        }

        // ---------------------------------------------------------------- helpers

        static Vector3 Top(HomeClearable c)
        {
            var sr = c.GetComponent<SpriteRenderer>();
            if (sr == null) return c.transform.position + Vector3.up;
            var b = sr.bounds;
            return new Vector3(b.center.x, b.max.y, 0f);
        }

        bool ToCanvas(Vector3 world, out Vector2 local)
        {
            local = Vector2.zero;
            var cam = Camera.main;
            if (cam == null || _canvas == null) return false;
            var sp = cam.WorldToScreenPoint(world);
            if (sp.z < 0f) return false;
            return RectTransformUtility.ScreenPointToLocalPointInRectangle(_canvas, sp, null, out local);
        }
    }

    /// <summary>While pressed, asks HomeClear for one frame of hold each frame.</summary>
    public class ClearHoldRelay : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        bool _down;

        public void OnPointerDown(PointerEventData eventData) => _down = true;
        public void OnPointerUp(PointerEventData eventData) => _down = false;
        public void OnPointerExit(PointerEventData eventData) => _down = false;
        void OnDisable() => _down = false;

        void Update()
        {
            if (_down) HomeClear.RequestHold();
        }
    }
}
