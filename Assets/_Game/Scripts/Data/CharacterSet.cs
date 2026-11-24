using UnityEngine;

namespace CrowdSurge
{
    /// <summary>
    /// Pre-baked animation for one runner model. Each mesh is a complete pose (three submeshes: bottom, head, top)
    /// so crowds can be drawn with GPU instancing while still looking like animated characters.
    /// Created by the editor tool CrowdSurge > Bake Characters from the Cube People prefabs.
    /// </summary>
    public sealed class CharacterSet : ScriptableObject
    {
        [Tooltip("Looping run cycle, evenly spaced poses.")]
        public Mesh[] runFrames;
        [Tooltip("Standing pose (menu, ready screen, victory).")]
        public Mesh idleFrame;
        [Tooltip("Height of the baked model in metres.")]
        public float height = 1.8f;
        [Tooltip("Seconds one full run cycle lasts in the source animation.")]
        public float runCycleSeconds = 0.7f;

        public int FrameCount => runFrames == null ? 0 : runFrames.Length;
    }
}
