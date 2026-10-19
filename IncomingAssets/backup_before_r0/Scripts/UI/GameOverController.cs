using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Legacy survival bootstrap. When LevelFlow is present it owns Game Over / Retry.
    /// This component only applies hero HP tuning if LevelFlow is absent.
    /// </summary>
    public class GameOverController : MonoBehaviour
    {
        const int HeroMaxHp = 400;
        const float HeroInvuln = 0.50f;

        [SerializeField] Health playerHealth;

        void Start()
        {
            if (FindFirstObjectByType<LevelFlow>() != null)
            {
                enabled = false;
                return;
            }

            if (playerHealth == null)
            {
                var player = GameObject.FindGameObjectWithTag("Player");
                if (player != null)
                    playerHealth = player.GetComponent<Health>();
            }

            if (playerHealth == null) return;
            playerHealth.Configure(HeroMaxHp, true);
            playerHealth.SetSurvival(HeroInvuln, blink: true);
        }

        void OnEnable()
        {
            Time.timeScale = 1f;
            if (HitStop.Instance != null)
                HitStop.Instance.SetBlocked(false);
        }
    }
}
