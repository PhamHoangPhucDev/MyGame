using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GameplayPrototype
{
    /// <summary>
    /// Applies hero survival tuning and shows Game Over with Restart.
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        const int HeroMaxHp = 400;
        const float HeroInvuln = 0.85f;

        [SerializeField] Health playerHealth;
        [SerializeField] PlayerMovement movement;
        [SerializeField] PlayerCombat combat;
        [SerializeField] EnemySpawner spawner;

        bool _shown;
        GameObject _panel;

        void Start()
        {
            if (playerHealth == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                {
                    playerHealth = player.GetComponent<Health>();
                    movement = player.GetComponent<PlayerMovement>();
                    combat = player.GetComponent<PlayerCombat>();
                }
            }

            if (spawner == null)
                spawner = FindFirstObjectByType<EnemySpawner>();

            if (playerHealth == null) return;

            playerHealth.Configure(HeroMaxHp, true);
            playerHealth.SetSurvival(HeroInvuln, blink: true);
            playerHealth.Died += OnHeroDied;
        }

        void OnDestroy()
        {
            if (playerHealth != null)
                playerHealth.Died -= OnHeroDied;
        }

        void OnHeroDied(Health _)
        {
            if (_shown) return;
            _shown = true;

            if (HitStop.Instance != null)
                HitStop.Instance.SetBlocked(true);
            else
                Time.timeScale = 1f;

            if (movement != null) movement.SetMovementLocked(true);
            if (combat != null) combat.enabled = false;
            if (spawner != null) spawner.enabled = false;

            var rb = playerHealth.GetComponent<Rigidbody2D>();
            if (rb != null) rb.linearVelocity = Vector2.zero;

            ShowPanel();
        }

        void ShowPanel()
        {
            Canvas canvas = null;
            int bestOrder = int.MinValue;
            foreach (var c in FindObjectsByType<Canvas>(FindObjectsSortMode.None))
            {
                if (c.sortingOrder >= bestOrder)
                {
                    bestOrder = c.sortingOrder;
                    canvas = c;
                }
            }
            if (canvas == null) return;

            _panel = new GameObject("GameOver");
            _panel.transform.SetParent(canvas.transform, false);
            var rt = _panel.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var dim = _panel.AddComponent<Image>();
            dim.color = new Color(0f, 0f, 0f, 0.62f);
            dim.raycastTarget = true;

            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(_panel.transform, false);
            var titleRt = titleGo.AddComponent<RectTransform>();
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 0.58f);
            titleRt.sizeDelta = new Vector2(420f, 80f);
            var title = titleGo.AddComponent<Text>();
            title.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            title.text = "GAME OVER";
            title.fontSize = 48;
            title.fontStyle = FontStyle.Bold;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = Color.white;
            title.raycastTarget = false;

            var buttonGo = new GameObject("Restart");
            buttonGo.transform.SetParent(_panel.transform, false);
            var buttonRt = buttonGo.AddComponent<RectTransform>();
            buttonRt.anchorMin = buttonRt.anchorMax = new Vector2(0.5f, 0.42f);
            buttonRt.sizeDelta = new Vector2(220f, 64f);
            var image = buttonGo.AddComponent<Image>();
            image.color = new Color(0.85f, 0.2f, 0.15f, 1f);
            var button = buttonGo.AddComponent<Button>();
            button.onClick.AddListener(Restart);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(buttonGo.transform, false);
            var labelRt = labelGo.AddComponent<RectTransform>();
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;
            var label = labelGo.AddComponent<Text>();
            label.font = title.font;
            label.text = "Restart";
            label.fontSize = 28;
            label.alignment = TextAnchor.MiddleCenter;
            label.color = Color.white;
            label.raycastTarget = false;
        }

        public void Restart()
        {
            if (HitStop.Instance != null)
                HitStop.Instance.CancelAndRestore();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        void OnEnable()
        {
            // Scene load safety: never start paused.
            Time.timeScale = 1f;
            if (HitStop.Instance != null)
                HitStop.Instance.SetBlocked(false);
        }
    }
}
