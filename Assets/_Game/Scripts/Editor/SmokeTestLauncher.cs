using UnityEditor;
using UnityEditor.SceneManagement;

namespace CrowdSurge.Editor
{
    public static class SmokeTestLauncher
    {
        public const string Pref = "cs_smoke";

        /// <summary>Unity -batchmode -executeMethod CrowdSurge.Editor.SmokeTestLauncher.Run -csSmoke (or from the open editor).</summary>
        public static void Run()
        {
            EditorPrefs.SetBool(Pref, true);
            EditorSceneManager.OpenScene("Assets/_Game/Scenes/Main.unity");
            EditorApplication.EnterPlaymode();
        }
    }
}
