using UnityEngine;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>Top-centre "+3 [icon] NAME" banner: fades in, holds 2 s, fades out. No modal.</summary>
    public class RewardBanner : MonoBehaviour
    {
        static RewardBanner _instance;
        static readonly Color Gold = new Color32(0xF2, 0xD8, 0x4A, 0xFF);

        CanvasGroup _group;
        RectTransform _row;
        Text _amount, _name;
        Image _icon;
        float _shownAt = -99f;

        const float FadeIn = 0.2f, Hold = 2f, FadeOut = 0.4f;

        public static void Show(string itemId, int amount)
        {
            if (amount <= 0 || string.IsNullOrEmpty(itemId)) return;
            var nav = WorldNavigator.Instance;
            if (nav != null && nav.Current == WorldZone.Housing)
            {
                HomeClearFx.PopupPlayer(itemId, amount);
                return;
            }
            var item = WorldContent.FindItem(itemId);
            string name = string.IsNullOrEmpty(item.displayName) ? itemId : item.displayName;
            Ensure().Present("+" + amount, UiKit.Get(UiKit.ItemIcon(itemId)), name);
        }

        static RewardBanner Ensure()
        {
            if (_instance != null) return _instance;
            // Above every world window (mailbox 245, bag 260) so a reward is never hidden; it never blocks clicks.
            var canvas = UiKit.Canvas("RewardBanner", 270);
            var ray = canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (ray != null) ray.enabled = false;
            _instance = canvas.gameObject.AddComponent<RewardBanner>();
            _instance.Build(canvas.transform);
            return _instance;
        }

        void Build(Transform parent)
        {
            _row = UiKit.Rect(parent, "Banner");
            _group = _row.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            PlaceRow(false);
            _amount = UiKit.Label(_row, "Amount", "", 8, Gold, TextAnchor.MiddleRight);
            _icon = UiKit.Icon(_row, "", UiKit.Center, Vector2.zero);
            _name = UiKit.Label(_row, "Name", "", 8, Color.white, TextAnchor.MiddleLeft);
        }

        /// <summary>Drops one row lower while a mode label (farm) holds the top-centre line.</summary>
        void PlaceRow(bool lowered)
        {
            float drop = lowered ? 12f : 0f;
            UiKit.Place(_row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -38f - drop), new Vector2(200f, 18f))
                .Portrait(UiLayout.Spec.At(new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -80f - drop), new Vector2(200f, 18f)));
        }

        void Present(string amount, Sprite icon, string name)
        {
            PlaceRow(HomeFarm.Active);
            _amount.text = amount;
            _name.text = name;
            _icon.sprite = icon;
            _icon.enabled = icon != null;
            // Centre the whole row: amount, 2 px, icon 16, 2 px, name.
            float aw = amount.Length * 8f, nw = Loc.T(name).Length * 8f;
            float total = aw + 20f + nw, left = -total / 2f;
            UiKit.Place(_amount, UiKit.Center, UiKit.Center, new Vector2(left + aw / 2f, 0f), new Vector2(aw + 2f, 10f));
            UiKit.Place(_icon, UiKit.Center, UiKit.Center, new Vector2(left + aw + 10f, 0f), new Vector2(16f, 16f));
            UiKit.Place(_name, UiKit.Center, UiKit.Center, new Vector2(left + aw + 20f + nw / 2f, 0f), new Vector2(nw + 8f, 10f));
            _shownAt = Time.unscaledTime;
        }

        void Update()
        {
            float t = Time.unscaledTime - _shownAt;
            float a = t < FadeIn ? t / FadeIn : t < FadeIn + Hold ? 1f : 1f - (t - FadeIn - Hold) / FadeOut;
            _group.alpha = Mathf.Clamp01(a);
        }
    }
}
