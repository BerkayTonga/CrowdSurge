using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace CrowdSurge.Editor
{
    /// <summary>
    /// One-click project setup: URP mobile settings, all materials, level asset, the Main scene and player settings.
    /// Runs automatically the first time the project opens, and is safe to run again (menu: CrowdSurge > Setup Project).
    /// </summary>
    [InitializeOnLoad]
    public static class CrowdSurgeSetup
    {
        private const string Root = "Assets/_Game";
        private const string MaterialsDir = Root + "/Materials";
        private const string SettingsDir = Root + "/Settings";
        private const string DataDir = Root + "/Data";
        private const string ResourcesDir = Root + "/Resources/CrowdSurge";
        private const string ScenesDir = Root + "/Scenes";
        private const string ScenePath = ScenesDir + "/Main.unity";
        private const string AssetsPath = ResourcesDir + "/GameAssets.asset";

        static CrowdSurgeSetup()
        {
            EditorApplication.delayCall += () =>
            {
                if (!File.Exists(AssetsPath) && !EditorApplication.isPlayingOrWillChangePlaymode)
                    Run();
            };
        }

        [MenuItem("CrowdSurge/Setup Project")]
        public static void Run()
        {
            EnsureFolders();
            ConfigureRenderPipeline();
            var levels = CreateLevels();
            CreateGameAssets(levels);
            CreateScene();
            ConfigurePlayerSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CrowdSurge] Setup complete. Open Assets/_Game/Scenes/Main.unity and press Play.");
        }

        /// <summary>Command-line entry: Unity -batchmode -executeMethod CrowdSurge.Editor.CrowdSurgeSetup.RunBatch</summary>
        public static void RunBatch() => Run();

        [MenuItem("CrowdSurge/Reset Level Data")]
        public static void ResetLevel()
        {
            for (int i = 0; i < 3; i++)
            {
                var level = AssetDatabase.LoadAssetAtPath<LevelConfig>($"{DataDir}/Level0{i + 1}.asset");
                if (level == null) continue;
                level.Build(i);
                EditorUtility.SetDirty(level);
            }
            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------------- folders
        private static void EnsureFolders()
        {
            foreach (var dir in new[] { MaterialsDir, SettingsDir, DataDir, ResourcesDir, ScenesDir })
            {
                if (Directory.Exists(dir)) continue;
                Directory.CreateDirectory(dir);
            }
            AssetDatabase.Refresh();
        }

        // ---------------------------------------------------------------- URP
        private static void ConfigureRenderPipeline()
        {
            string rpPath = SettingsDir + "/Mobile_RPAsset.asset";
            var rp = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(rpPath);
            if (rp == null)
            {
                string rendererPath = SettingsDir + "/Mobile_Renderer.asset";
                var renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, rendererPath);
                rp = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(rp, rpPath);

                var so = new SerializedObject(rp);
                SetBool(so, "m_SupportsHDR", false);
                SetInt(so, "m_MSAA", 4);
                SetBool(so, "m_MainLightShadowsSupported", false);
                SetInt(so, "m_AdditionalLightsRenderingMode", 0);
                SetBool(so, "m_RequireDepthTexture", false);
                SetBool(so, "m_RequireOpaqueTexture", false);
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(rp);
                AssetDatabase.SaveAssets();
            }

            GraphicsSettings.defaultRenderPipeline = rp;
            int current = QualitySettings.GetQualityLevel();
            for (int i = 0; i < QualitySettings.names.Length; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = rp;
                QualitySettings.vSyncCount = 0;
            }
            QualitySettings.SetQualityLevel(current, false);
        }

        private static void SetBool(SerializedObject so, string name, bool value)
        {
            var p = so.FindProperty(name);
            if (p != null) p.boolValue = value;
        }

        private static void SetInt(SerializedObject so, string name, int value)
        {
            var p = so.FindProperty(name);
            if (p != null) p.intValue = value;
        }

        // ---------------------------------------------------------------- data
        private static LevelConfig[] CreateLevels()
        {
            var levels = new LevelConfig[3];
            for (int i = 0; i < levels.Length; i++)
            {
                string path = $"{DataDir}/Level0{i + 1}.asset";
                var level = AssetDatabase.LoadAssetAtPath<LevelConfig>(path);
                if (level == null)
                {
                    level = ScriptableObject.CreateInstance<LevelConfig>();
                    level.Build(i);
                    AssetDatabase.CreateAsset(level, path);
                }
                levels[i] = level;
            }
            return levels;
        }

        private static void CreateGameAssets(LevelConfig[] levels)
        {
            var a = AssetDatabase.LoadAssetAtPath<GameAssets>(AssetsPath);
            if (a == null)
            {
                a = ScriptableObject.CreateInstance<GameAssets>();
                AssetDatabase.CreateAsset(a, AssetsPath);
            }

            a.levels = levels;
            foreach (var old in new[] { "Crowd_Body", "Crowd_Head", "Enemy_Body", "Enemy_Head", "Boss_Body", "Boss_Head" })
                AssetDatabase.DeleteAsset($"{MaterialsDir}/{old}.mat");
            ConfigureCharacters(a);

            a.roadLight = Lit("Road_Light", new Color(0.78f, 0.8f, 0.86f));
            a.roadDark = Lit("Road_Dark", new Color(0.58f, 0.6f, 0.68f));
            a.rail = Lit("Rail", new Color(0.9f, 0.9f, 0.95f));
            a.finishWhite = Lit("Finish_White", Color.white);
            a.finishBlack = Lit("Finish_Black", new Color(0.1f, 0.1f, 0.14f));
            a.cloud = Lit("Cloud", Color.white);

            a.gateGood = Transparent("Gate_Good", new Color(0.25f, 0.55f, 1f, 0.55f));
            a.gateBad = Transparent("Gate_Bad", new Color(1f, 0.25f, 0.25f, 0.55f));
            a.gateFrameGood = Lit("GateFrame_Good", new Color(0.15f, 0.32f, 0.85f));
            a.gateFrameBad = Lit("GateFrame_Bad", new Color(0.8f, 0.13f, 0.17f));
            a.sawBlade = Lit("Saw_Blade", new Color(0.9f, 0.14f, 0.18f));
            a.sawHub = Lit("Saw_Hub", new Color(1f, 0.62f, 0.1f));

            a.particle = Particle("Particle");

            EditorUtility.SetDirty(a);
            AssetDatabase.SaveAssets();
        }

        // ---------------------------------------------------------------- materials
        /// <summary>Bakes the Cube People animation (once) and creates the tinted team materials.</summary>
        private static void ConfigureCharacters(GameAssets a)
        {
            var set = AssetDatabase.LoadAssetAtPath<CharacterSet>(CharacterBaker.SetPath);
            if (set == null)
            {
                CharacterBaker.Bake();
                set = AssetDatabase.LoadAssetAtPath<CharacterSet>(CharacterBaker.SetPath);
            }
            a.character = set;

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CubePeopleDemo/Prefabs/0CP/Ch_0CP00M00.prefab");
            if (prefab == null || set == null) return;
            var src = prefab.GetComponentsInChildren<SkinnedMeshRenderer>().OrderBy(s => s.name).Select(s => s.sharedMaterials[0]).ToArray(); // bottom, head, top
            var bald = AssetDatabase.LoadAssetAtPath<Material>("Assets/CubePeopleDemo/Materials/Ch/Head/Ch_0CPM00_Head00Bald.mat");

            a.playerMaterials = Team("Player", src, new Color(0.18f, 0.3f, 0.85f), new Color(0.3f, 0.62f, 1f), src[1], Color.white);
            a.enemyMaterials = Team("Enemy", src, new Color(0.35f, 0.15f, 0.15f), new Color(1f, 0.27f, 0.27f), bald != null ? bald : src[1], Color.white);
            a.bossMaterials = Team("Boss", src, new Color(0.12f, 0.08f, 0.1f), new Color(0.65f, 0.08f, 0.2f), bald != null ? bald : src[1], new Color(1f, 0.75f, 0.7f));
        }

        private static Material[] Team(string name, Material[] src, Color bottom, Color top, Material head, Color headTint)
        {
            return new[]
            {
                Tinted(name + "_Bottom", src[0], bottom),
                Tinted(name + "_Head", head, headTint),
                Tinted(name + "_Top", src[2], top),
            };
        }

        private static Material Tinted(string name, Material source, Color tint)
        {
            string path = $"{MaterialsDir}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(source);
                AssetDatabase.CreateAsset(mat, path);
            }
            else
            {
                mat.shader = source.shader;
                mat.CopyPropertiesFromMaterial(source);
            }
            mat.SetColor("_BaseColor", tint);
            mat.enableInstancing = true;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material GetOrCreate(string name, string shaderName)
        {
            string path = $"{MaterialsDir}/{name}.mat";
            var shader = Shader.Find(shaderName);
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(shader);
                AssetDatabase.CreateAsset(mat, path);
            }
            else if (mat.shader != shader)
            {
                mat.shader = shader;
            }
            return mat;
        }

        private static Material Lit(string name, Color color)
        {
            var m = GetOrCreate(name, "Universal Render Pipeline/Lit");
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.12f);
            m.SetFloat("_Metallic", 0f);
            m.SetFloat("_SpecularHighlights", 0f);
            m.SetFloat("_EnvironmentReflections", 0f);
            m.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
            m.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
            m.enableInstancing = true;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material Transparent(string name, Color color)
        {
            var m = GetOrCreate(name, "Universal Render Pipeline/Unlit");
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Surface", 1f);
            m.SetFloat("_Blend", 0f);
            m.SetFloat("_ZWrite", 0f);
            m.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            m.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetFloat("_SrcBlendAlpha", (float)UnityEngine.Rendering.BlendMode.One);
            m.SetFloat("_DstBlendAlpha", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            m.SetOverrideTag("RenderType", "Transparent");
            m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            m.SetShaderPassEnabled("ShadowCaster", false);
            m.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(m);
            return m;
        }

        private static Material Particle(string name)
        {
            var m = GetOrCreate(name, "Universal Render Pipeline/Particles/Unlit");
            m.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(m);
            return m;
        }

        // ---------------------------------------------------------------- scene
        private static void CreateScene()
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.36f, 0.67f, 1f);
            cam.nearClipPlane = 0.3f;
            cam.farClipPlane = 220f;
            cam.fieldOfView = 58f;
            camGo.transform.SetPositionAndRotation(new Vector3(0f, 8.2f, -9.5f), Quaternion.Euler(36f, 0f, 0f));
            camGo.AddComponent<AudioListener>();

            var lightGo = new GameObject("Directional Light");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.97f, 0.9f);
            light.intensity = 0.85f;
            light.shadows = LightShadows.None;
            lightGo.transform.rotation = Quaternion.Euler(52f, -32f, 0f);
            RenderSettings.sun = light;

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.5f, 0.56f, 0.7f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = new Color(0.36f, 0.67f, 1f);
            RenderSettings.fogStartDistance = 70f;
            RenderSettings.fogEndDistance = 150f;

            new GameObject("GameBootstrap").AddComponent<GameBootstrap>();

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        // ---------------------------------------------------------------- player settings
        private static void ConfigurePlayerSettings()
        {
            PlayerSettings.companyName = "Berkay Tonga";
            PlayerSettings.productName = "Crowd Surge";
            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToPortrait = true;

            const string bundleId = "com.berkaytonga.crowdsurge";
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, bundleId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, bundleId);
            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64;
            PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel26;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Medium);
            PlayerSettings.gpuSkinning = true;
        }
    }
}
