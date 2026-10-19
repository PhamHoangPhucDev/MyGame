using UnityEngine;

namespace GameplayPrototype
{
    /// <summary>
    /// Lightweight PlayerPrefs save for unlocked levels, best scores, and audio settings.
    /// </summary>
    public static class GameProgress
    {
        const string KeyUnlocked = "gp.unlockedLevel";
        const string KeyBestScore = "gp.bestScore.l";
        const string KeyBestCombo = "gp.bestCombo.l";
        const string KeyMusic = "gp.musicVol";
        const string KeySfx = "gp.sfxVol";
        const string KeyMute = "gp.mute";

        public static int HighestUnlockedLevel
        {
            get => Mathf.Max(1, PlayerPrefs.GetInt(KeyUnlocked, 1));
            private set
            {
                PlayerPrefs.SetInt(KeyUnlocked, Mathf.Max(1, value));
                PlayerPrefs.Save();
            }
        }

        public static bool HasContinue => HighestUnlockedLevel > 1;

        public static void UnlockLevel(int levelNumber)
        {
            if (levelNumber > HighestUnlockedLevel)
                HighestUnlockedLevel = levelNumber;
        }

        public static int GetBestScore(int level) => PlayerPrefs.GetInt(KeyBestScore + level, 0);
        public static int GetBestCombo(int level) => PlayerPrefs.GetInt(KeyBestCombo + level, 0);

        public static void RecordRun(int level, int score, int bestCombo)
        {
            if (score > GetBestScore(level))
                PlayerPrefs.SetInt(KeyBestScore + level, score);
            if (bestCombo > GetBestCombo(level))
                PlayerPrefs.SetInt(KeyBestCombo + level, bestCombo);
            PlayerPrefs.Save();
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(KeyMusic, 0.7f);
            set
            {
                PlayerPrefs.SetFloat(KeyMusic, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(KeySfx, 1f);
            set
            {
                PlayerPrefs.SetFloat(KeySfx, Mathf.Clamp01(value));
                PlayerPrefs.Save();
            }
        }

        public static bool Muted
        {
            get => PlayerPrefs.GetInt(KeyMute, 0) != 0;
            set
            {
                PlayerPrefs.SetInt(KeyMute, value ? 1 : 0);
                PlayerPrefs.Save();
            }
        }

        public static void ResetAllProgress()
        {
            PlayerPrefs.DeleteKey(KeyUnlocked);
            for (int i = 1; i <= 8; i++)
            {
                PlayerPrefs.DeleteKey(KeyBestScore + i);
                PlayerPrefs.DeleteKey(KeyBestCombo + i);
            }
            PlayerPrefs.Save();
        }
    }
}
