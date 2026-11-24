using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace CrowdSurge
{
    /// <summary>Applies the quality and frame-rate settings. Quality changes are skipped in the editor so the shared URP asset is never modified.</summary>
    public static class GraphicsOptions
    {
        public static void Apply()
        {
            Application.targetFrameRate = SaveData.FrameRate;
            if (Application.isEditor) return;

            var rp = UniversalRenderPipeline.asset;
            if (rp == null) return;
            if (SaveData.Quality == 1)
            {
                rp.renderScale = 1f;
                rp.msaaSampleCount = 4;
            }
            else
            {
                rp.renderScale = 0.75f;   // battery saver: fewer pixels, no multisampling
                rp.msaaSampleCount = 1;
            }
        }
    }
}
