using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>
    /// Pooled Overlay damage numbers. Rises and fades without Instantiate/Destroy per hit.
    /// </summary>
    public class DamagePopup : MonoBehaviour
    {
        [SerializeField] float lifetime = 0.55f;
        [SerializeField] float riseSpeed = 24f;

        Text _text;
        RectTransform _rt;
        float _age;
        Color _color;
        Vector3 _worldPos;
        bool _active;

        static readonly Stack<DamagePopup> Pool = new();
        static Transform _poolRoot;
        static int _stagger;

        public static DamagePopup Spawn(Vector3 worldPos, int damage, Color color)
        {
            WorldLabelsCanvas.Ensure();
            var popup = GetFromPool();
            popup.Activate(worldPos, damage, color);
            return popup;
        }

        static DamagePopup GetFromPool()
        {
            while (Pool.Count > 0)
            {
                var p = Pool.Pop();
                if (p != null) return p;
            }

            EnsurePoolRoot();
            var go = new GameObject("DamagePopup");
            go.transform.SetParent(WorldLabelsCanvas.Instance.Root, false);
            var popup = go.AddComponent<DamagePopup>();
            popup.Build();
            return popup;
        }

        static void EnsurePoolRoot()
        {
            if (_poolRoot != null) return;
            var go = new GameObject("DamagePopupPool");
            Object.DontDestroyOnLoad(go);
            _poolRoot = go.transform;
        }

        void Build()
        {
            _rt = gameObject.GetComponent<RectTransform>();
            if (_rt == null) _rt = gameObject.AddComponent<RectTransform>();
            _rt.sizeDelta = new Vector2(120f, 36f);

            _text = gameObject.GetComponent<Text>();
            if (_text == null) _text = gameObject.AddComponent<Text>();
            _text.font = WorldLabelsCanvas.Instance.PixelFont;
            _text.fontSize = 24;
            _text.alignment = TextAnchor.MiddleCenter;
            _text.horizontalOverflow = HorizontalWrapMode.Overflow;
            _text.verticalOverflow = VerticalWrapMode.Overflow;
            _text.raycastTarget = false;
            WorldLabelsCanvas.ApplyPixelOutline(_text, Color.black);
        }

        void Activate(Vector3 worldPos, int damage, Color color)
        {
            if (_text == null) Build();
            // R3D: popup lanes sit above Name/HP (labels ~0.75–1.1 above feet). Rise clear of hero.
            int lane = _stagger++ % 4;
            float side = lane == 0 ? 0.42f : (lane == 1 ? -0.42f : (lane == 2 ? 0.18f : -0.18f));
            float lift = 1.05f + (lane % 2) * 0.18f;
            _worldPos = worldPos + new Vector3(side, lift, 0f);
            _age = 0f;
            _color = color;
            _color.a = 1f;
            _text.text = damage.ToString();
            _text.color = _color;
            _text.fontSize = damage >= 30 ? 32 : 24;
            _active = true;
            gameObject.SetActive(true);
            transform.SetParent(WorldLabelsCanvas.Instance.Root, false);
            UpdateScreenPos();
        }

        void Update()
        {
            if (!_active) return;
            // Rise in world space so it tracks correctly, timed with scaled delta (pauses in hitstop).
            _age += Time.deltaTime;
            _worldPos += Vector3.up * (riseSpeed / 64f * Time.deltaTime);
            UpdateScreenPos();

            if (_age > 0.35f)
            {
                float a = 1f - Mathf.InverseLerp(0.35f, lifetime, _age);
                _color.a = a;
                _text.color = _color;
            }

            if (_age >= lifetime)
                Release();
        }

        void UpdateScreenPos()
        {
            Vector2 screen = WorldLabelsCanvas.WorldToSnappedScreen(_worldPos);
            _rt.position = screen;
        }

        void Release()
        {
            _active = false;
            gameObject.SetActive(false);
            if (_poolRoot != null)
                transform.SetParent(_poolRoot, false);
            Pool.Push(this);
        }
    }
}
