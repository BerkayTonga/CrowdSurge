using UnityEngine;

namespace CrowdSurge
{
    /// <summary>
    /// Central reference to every material and the level config. Created by the editor setup tool
    /// (CrowdSurge > Setup Project) and loaded from Resources so materials/shader variants are always in builds.
    /// </summary>
    public sealed class GameAssets : ScriptableObject
    {
        public const string ResourcePath = "CrowdSurge/GameAssets";

        // Team colours used by particle effects, labels and UI.
        public static readonly Color PlayerColor = new Color(0.25f, 0.6f, 1f);
        public static readonly Color EnemyColor = new Color(1f, 0.3f, 0.3f);
        public static readonly Color GoodColor = new Color(0.35f, 0.9f, 1f);
        public static readonly Color BadColor = new Color(1f, 0.4f, 0.35f);

        [Header("Level")]
        public LevelConfig[] levels;

        [Header("Characters")]
        public CharacterSet character;
        [Tooltip("Bottom, head, top (same order as the baked submeshes).")]
        public Material[] playerMaterials;
        public Material[] enemyMaterials;
        public Material[] bossMaterials;

        [Header("Track")]
        public Material roadLight;
        public Material roadDark;
        public Material rail;
        public Material finishWhite;
        public Material finishBlack;
        public Material cloud;

        [Header("Gates & Hazards")]
        public Material gateGood;
        public Material gateBad;
        public Material gateFrameGood;
        public Material gateFrameBad;
        public Material sawBlade;
        public Material sawHub;

        [Header("FX")]
        public Material particle;

        public static GameAssets Load()
        {
            var assets = Resources.Load<GameAssets>(ResourcePath);
            if (assets == null)
                Debug.LogError("GameAssets not found. Run the menu: CrowdSurge > Setup Project.");
            return assets;
        }
    }
}
