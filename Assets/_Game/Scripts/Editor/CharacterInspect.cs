using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace CrowdSurge.Editor
{
    /// <summary>Dev helper: converts the Cube People materials to URP and reports on the character prefabs and animation clips.</summary>
    public static class CharacterInspect
    {
        public static void ConvertMaterials()
        {
            var lit = Shader.Find("Universal Render Pipeline/Lit");
            int n = 0;
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { "Assets/CubePeopleDemo" }))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || mat.shader == null || mat.shader.name.StartsWith("Universal Render Pipeline")) continue;
                if (path.Contains("/Eff/")) continue;
                Texture tex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;
                mat.shader = lit;
                mat.SetTexture("_BaseMap", tex);
                mat.SetColor("_BaseColor", color);
                mat.SetFloat("_Smoothness", 0.1f);
                mat.SetFloat("_Metallic", 0f);
                mat.SetFloat("_SpecularHighlights", 0f);
                mat.SetFloat("_EnvironmentReflections", 0f);
                mat.EnableKeyword("_SPECULARHIGHLIGHTS_OFF");
                mat.EnableKeyword("_ENVIRONMENTREFLECTIONS_OFF");
                mat.enableInstancing = true;
                EditorUtility.SetDirty(mat);
                n++;
            }
            AssetDatabase.SaveAssets();
            Debug.Log("[Char] converted materials: " + n);
        }

        public static void Run()
        {
            ConvertMaterials();
            var sb = new StringBuilder();

            foreach (string path in new[]
            {
                "Assets/CubePeopleDemo/Prefabs/0CP/Ch_0CP00M00.prefab",
                "Assets/CubePeopleDemo/Prefabs/0CP/Ch_0CP00F00.prefab",
                "Assets/CubePeopleDemo/Prefabs/0CP/Ch_0CP01M00.prefab",
                "Assets/CubePeopleDemo/Prefabs/0CP/Ch_0CP00M00Bald.prefab",
            })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) { sb.AppendLine("MISSING " + path); continue; }
                sb.AppendLine("== " + path);
                foreach (var t in prefab.GetComponentsInChildren<Transform>(true).Take(60))
                {
                    var comps = string.Join(",", t.GetComponents<Component>().Select(c => c == null ? "null" : c.GetType().Name));
                    sb.AppendLine("  " + t.name + " [" + comps + "]");
                }
                foreach (var smr in prefab.GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    var mesh = smr.sharedMesh;
                    sb.AppendLine("  SMR " + smr.name + ": mesh=" + (mesh != null ? mesh.name : "null") + " verts=" + mesh.vertexCount +
                                  " tris=" + (mesh.triangles.Length / 3) + " subs=" + mesh.subMeshCount + " bones=" + smr.bones.Length +
                                  " bounds=" + mesh.bounds.size + " mats=" + string.Join("|", smr.sharedMaterials.Select(m => m == null ? "null" : m.name)));
                }
                var anim = prefab.GetComponentInChildren<Animator>(true);
                sb.AppendLine("  Animator controller=" + (anim != null && anim.runtimeAnimatorController != null ? anim.runtimeAnimatorController.name : "none") +
                              " avatar=" + (anim != null && anim.avatar != null ? anim.avatar.name : "none"));
            }

            foreach (string fbx in new[] { "Run00", "Idle00", "Walk00" })
                foreach (var clip in AssetDatabase.LoadAllAssetsAtPath("Assets/CubePeopleDemo/Animations/" + fbx + ".FBX").OfType<AnimationClip>())
                    sb.AppendLine("CLIP " + fbx + " " + clip.name + " len=" + clip.length.ToString("0.000") + " fps=" + clip.frameRate + " loop=" + clip.isLooping);

            string dir = Path.Combine(Directory.GetCurrentDirectory(), "Logs", "Preview");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "character.txt"), sb.ToString());
            Debug.Log("[Char] report written");
        }
    }
}
