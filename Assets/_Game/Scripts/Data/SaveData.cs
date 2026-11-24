using UnityEngine;

namespace CrowdSurge
{
    /// <summary>Thin PlayerPrefs wrapper for progress and settings.</summary>
    public static class SaveData
    {
        public const int LevelCount = 3;

        private const string UnlockedKey = "cs_unlocked";
        private const string StarsKey = "cs_stars_";
        private const string SfxKey = "cs_sfx";
        private const string MusicKey = "cs_music";
        private const string VibrationKey = "cs_vibration";
        private const string TutorialKey = "cs_tutorial";
        private const string SfxVolumeKey = "cs_sfx_volume";
        private const string MusicVolumeKey = "cs_music_volume";
        private const string FpsKey = "cs_fps";
        private const string QualityKey = "cs_quality";
        private const string RunsKey = "cs_runs";
        private const string WinsKey = "cs_wins";
        private const string KillsKey = "cs_kills";
        private const string BestCrowdKey = "cs_best_crowd";

        /// <summary>Number of levels the player may play (1..LevelCount).</summary>
        public static int UnlockedLevels
        {
            get => Mathf.Clamp(PlayerPrefs.GetInt(UnlockedKey, 1), 1, LevelCount);
            set => PlayerPrefs.SetInt(UnlockedKey, Mathf.Clamp(value, 1, LevelCount));
        }

        public static int GetStars(int level) => PlayerPrefs.GetInt(StarsKey + level, 0);

        public static void SetStars(int level, int stars)
            => PlayerPrefs.SetInt(StarsKey + level, Mathf.Max(stars, GetStars(level)));

        public static int TotalStars
        {
            get
            {
                int sum = 0;
                for (int i = 0; i < LevelCount; i++) sum += GetStars(i);
                return sum;
            }
        }

        public static float SfxVolume
        {
            get => PlayerPrefs.GetFloat(SfxVolumeKey, 1f);
            set => PlayerPrefs.SetFloat(SfxVolumeKey, Mathf.Clamp01(value));
        }

        public static float MusicVolume
        {
            get => PlayerPrefs.GetFloat(MusicVolumeKey, 0.8f);
            set => PlayerPrefs.SetFloat(MusicVolumeKey, Mathf.Clamp01(value));
        }

        /// <summary>30 or 60.</summary>
        public static int FrameRate
        {
            get => PlayerPrefs.GetInt(FpsKey, 60) == 30 ? 30 : 60;
            set => PlayerPrefs.SetInt(FpsKey, value == 30 ? 30 : 60);
        }

        /// <summary>0 = battery saver, 1 = high.</summary>
        public static int Quality
        {
            get => PlayerPrefs.GetInt(QualityKey, 1);
            set => PlayerPrefs.SetInt(QualityKey, value);
        }

        public static int TotalRuns { get => PlayerPrefs.GetInt(RunsKey, 0); set => PlayerPrefs.SetInt(RunsKey, value); }
        public static int TotalWins { get => PlayerPrefs.GetInt(WinsKey, 0); set => PlayerPrefs.SetInt(WinsKey, value); }
        public static int TotalDefeated { get => PlayerPrefs.GetInt(KillsKey, 0); set => PlayerPrefs.SetInt(KillsKey, value); }
        public static int BestCrowd { get => PlayerPrefs.GetInt(BestCrowdKey, 0); set => PlayerPrefs.SetInt(BestCrowdKey, value); }

        public static bool SfxOn
        {
            get => PlayerPrefs.GetInt(SfxKey, 1) == 1;
            set => PlayerPrefs.SetInt(SfxKey, value ? 1 : 0);
        }

        public static bool MusicOn
        {
            get => PlayerPrefs.GetInt(MusicKey, 1) == 1;
            set => PlayerPrefs.SetInt(MusicKey, value ? 1 : 0);
        }

        public static bool VibrationOn
        {
            get => PlayerPrefs.GetInt(VibrationKey, 1) == 1;
            set => PlayerPrefs.SetInt(VibrationKey, value ? 1 : 0);
        }

        public static bool TutorialSeen
        {
            get => PlayerPrefs.GetInt(TutorialKey, 0) == 1;
            set => PlayerPrefs.SetInt(TutorialKey, value ? 1 : 0);
        }

        public static void Save() => PlayerPrefs.Save();
    }

    /// <summary>State that must survive a scene reload (level change, restart, back to menu).</summary>
    public static class GameSession
    {
        public static int SelectedLevel;
        public static bool SkipMenu;
    }
}
