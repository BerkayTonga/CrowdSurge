using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace CrowdSurge.Editor
{
    /// <summary>
    /// Bakes the Cube People run / idle animation into plain meshes. The skinned characters are far too heavy to
    /// animate by the hundred on a phone; baked poses can be drawn thousands at a time with GPU instancing.
    /// </summary>
    public static class CharacterBaker
    {
        public const string BakedDir = "Assets/_Game/Baked";
        public const string SetPath = BakedDir + "/Runner.asset";
        private const string PrefabPath = "Assets/CubePeopleDemo/Prefabs/0CP/Ch_0CP00M00.prefab";
        private const int RunFrames = 8;

        [MenuItem("CrowdSurge/Bake Characters")]
        public static void Bake()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
            {
                Debug.LogError("[CrowdSurge] Cube People paketi bulunamadi: " + PrefabPath);
                return;
            }

            CharacterInspect.ConvertMaterials();
            Directory.CreateDirectory(BakedDir);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            AnimationClip run = LoadClip("Run00");
            AnimationClip idle = LoadClip("Idle00");
            if (run == null || idle == null) { Debug.LogError("[CrowdSurge] Run00 / Idle00 animasyonlari bulunamadi."); return; }

            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var smrs = go.GetComponentsInChildren<SkinnedMeshRenderer>().OrderBy(s => s.name).ToArray(); // Bottom, Head, Top

            AnimationMode.StartAnimationMode();
            try
            {
                var frames = new List<Mesh>();
                for (int k = 0; k < RunFrames; k++)
                {
                    AnimationMode.SampleAnimationClip(go, run, run.length * k / RunFrames);
                    frames.Add(Combine(go.transform, smrs, "Run_" + k));
                }
                AnimationMode.SampleAnimationClip(go, idle, 0f);
                Mesh idleMesh = Combine(go.transform, smrs, "Idle");

                // Stand every pose on y = 0 and centre it on x / z, using the same offset for all frames so feet do not jitter.
                Bounds all = frames[0].bounds;
                foreach (var f in frames) all.Encapsulate(f.bounds);
                all.Encapsulate(idleMesh.bounds);
                Vector3 offset = new Vector3(-all.center.x, -all.min.y, -all.center.z);
                foreach (var f in frames) Shift(f, offset);
                Shift(idleMesh, offset);

                var set = AssetDatabase.LoadAssetAtPath<CharacterSet>(SetPath);
                if (set == null)
                {
                    set = ScriptableObject.CreateInstance<CharacterSet>();
                    AssetDatabase.CreateAsset(set, SetPath);
                }
                else
                {
                    foreach (var sub in AssetDatabase.LoadAllAssetsAtPath(SetPath).OfType<Mesh>().ToArray()) Object.DestroyImmediate(sub, true);
                }

                foreach (var f in frames) AssetDatabase.AddObjectToAsset(f, set);
                AssetDatabase.AddObjectToAsset(idleMesh, set);
                set.runFrames = frames.ToArray();
                set.idleFrame = idleMesh;
                set.height = all.size.y;
                set.runCycleSeconds = run.length;
                EditorUtility.SetDirty(set);
                AssetDatabase.SaveAssets();
                Debug.Log($"[CrowdSurge] Baked {frames.Count} run frames. height={set.height:0.00} width={all.size.x:0.00} depth={all.size.z:0.00}");
            }
            finally
            {
                AnimationMode.StopAnimationMode();
                Object.DestroyImmediate(go);
            }
        }

        private static AnimationClip LoadClip(string name)
        {
            foreach (var o in AssetDatabase.LoadAllAssetsAtPath($"Assets/CubePeopleDemo/Animations/{name}.FBX"))
                if (o is AnimationClip c && c.name == name) return c;
            return null;
        }

        /// <summary>Bakes the three skinned parts in their current pose into one mesh with three submeshes.</summary>
        private static Mesh Combine(Transform root, SkinnedMeshRenderer[] smrs, string name)
        {
            var verts = new List<Vector3>();
            var normals = new List<Vector3>();
            var uvs = new List<Vector2>();
            var tris = new List<int>[smrs.Length];

            for (int s = 0; s < smrs.Length; s++)
            {
                var baked = new Mesh();
                smrs[s].BakeMesh(baked, false);
                Matrix4x4 m = root.worldToLocalMatrix * smrs[s].transform.localToWorldMatrix;
                Quaternion rot = m.rotation;
                int start = verts.Count;
                foreach (var v in baked.vertices) verts.Add(m.MultiplyPoint3x4(v));
                foreach (var n in baked.normals) normals.Add(rot * n);
                uvs.AddRange(baked.uv.Length == baked.vertexCount ? baked.uv : new Vector2[baked.vertexCount]);
                tris[s] = baked.triangles.Select(i => i + start).ToList();
                Object.DestroyImmediate(baked);
            }

            var mesh = new Mesh { name = name };
            mesh.SetVertices(verts);
            mesh.SetNormals(normals);
            mesh.SetUVs(0, uvs);
            mesh.subMeshCount = smrs.Length;
            for (int s = 0; s < smrs.Length; s++) mesh.SetTriangles(tris[s], s, false);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static void Shift(Mesh mesh, Vector3 offset)
        {
            var v = mesh.vertices;
            for (int i = 0; i < v.Length; i++) v[i] += offset;
            mesh.vertices = v;
            mesh.RecalculateBounds();
        }

        /// <summary>Renders the baked frames side by side so the result can be reviewed (Logs/Preview/runner_frames.png).</summary>
        public static void Preview()
        {
            var set = AssetDatabase.LoadAssetAtPath<CharacterSet>(SetPath);
            if (set == null) { Debug.LogError("Run Bake first."); return; }

            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var camGo = new GameObject("Cam");
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.55f, 0.78f, 0.95f);
            var light = new GameObject("Light").AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.6f, 0.65f, 0.75f);

            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            var smr = prefab.GetComponentsInChildren<SkinnedMeshRenderer>().OrderBy(s => s.name).ToArray();
            var mats = smr.Select(s => s.sharedMaterials[0]).ToArray();

            var meshes = set.runFrames.Concat(new[] { set.idleFrame }).ToArray();
            const int cell = 220;
            var sheet = new Texture2D(cell * meshes.Length, cell * 2, TextureFormat.RGB24, false);
            for (int row = 0; row < 2; row++)
            for (int i = 0; i < meshes.Length; i++)
            {
                var go = new GameObject("m");
                go.AddComponent<MeshFilter>().sharedMesh = meshes[i];
                go.AddComponent<MeshRenderer>().sharedMaterials = mats;
                go.transform.rotation = Quaternion.Euler(0f, row == 0 ? 35f : 145f, 0f); // front-ish and rear-ish views

                camGo.transform.position = new Vector3(0f, set.height * 0.6f, -set.height * 2.2f);
                camGo.transform.LookAt(new Vector3(0f, set.height * 0.5f, 0f));
                var rt = RenderTexture.GetTemporary(cell, cell, 24);
                cam.targetTexture = rt;
                cam.Render();
                cam.targetTexture = null;
                RenderTexture.active = rt;
                var tile = new Texture2D(cell, cell, TextureFormat.RGB24, false);
                tile.ReadPixels(new Rect(0, 0, cell, cell), 0, 0);
                RenderTexture.active = null;
                RenderTexture.ReleaseTemporary(rt);
                sheet.SetPixels(i * cell, (1 - row) * cell, cell, cell, tile.GetPixels());
                Object.DestroyImmediate(tile);
                Object.DestroyImmediate(go);
            }

            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Preview");
            Directory.CreateDirectory(dir);
            File.WriteAllBytes(Path.Combine(dir, "runner_frames.png"), sheet.EncodeToPNG());
            Object.DestroyImmediate(sheet);
            Debug.Log("[CrowdSurge] preview written");
        }
    }
}
