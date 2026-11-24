using UnityEngine;

namespace CrowdSurge
{
    /// <summary>
    /// Composition root. The scene only contains a camera, a light and this component: everything else
    /// (track, crowd, enemies, UI, audio) is created here in a fixed order for the selected level.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public sealed class GameBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            QualitySettings.vSyncCount = 0;
            GraphicsOptions.Apply();
            Screen.sleepTimeout = SleepTimeout.NeverSleep;
            Screen.orientation = ScreenOrientation.Portrait;

            var assets = GameAssets.Load();
            if (assets == null || assets.levels == null || assets.levels.Length == 0) { enabled = false; return; }

            int levelIndex = Mathf.Clamp(GameSession.SelectedLevel, 0, assets.levels.Length - 1);
            LevelConfig level = assets.levels[levelIndex];

            Camera cam = Camera.main != null ? Camera.main : CreateCamera();
            ApplyTheme(cam, level, out Material roadLight, out Material roadDark, assets);

            AudioManager.GetOrCreate();
            var fx = ParticleFx.Create(transform, assets.particle);

            TrackBuilder.Build(transform, assets, level, roadLight, roadDark, assets.rail);

            var crowd = CrowdController.Create(transform, assets);
            crowd.ResetCrowd(level.startCount);
            var camera = CameraRig.Create(cam, crowd);

            var texts = FloatingTexts.Create(transform);
            var gates = new GateManager(transform, assets, level, crowd, fx, camera, texts);
            var hazards = new HazardManager(transform, assets, level, crowd, fx, camera, texts);
            var enemies = new EnemyManager(transform, assets, level, crowd, fx, camera, texts);
            var ui = UIManager.Create(transform);

            gameObject.AddComponent<GameManager>().Init(level, levelIndex, crowd, gates, hazards, enemies, camera, ui, fx);

#if UNITY_EDITOR
            if (EditorSmokeTest.Requested) gameObject.AddComponent<EditorSmokeTest>();
#endif
        }

        /// <summary>Sky, fog and road colours per level. Materials are cloned so the shared assets stay untouched.</summary>
        private static void ApplyTheme(Camera cam, LevelConfig level, out Material roadLight, out Material roadDark, GameAssets assets)
        {
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = level.skyColor;
            RenderSettings.fogColor = level.skyColor;

            roadLight = new Material(assets.roadLight) { color = level.roadLight };
            roadDark = new Material(assets.roadDark) { color = level.roadDark };
        }

        private static Camera CreateCamera()
        {
            var go = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = go.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 220f;
            go.AddComponent<AudioListener>();
            return cam;
        }
    }
}
