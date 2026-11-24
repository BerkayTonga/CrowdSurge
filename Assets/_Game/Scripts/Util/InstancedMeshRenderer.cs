using UnityEngine;
using UnityEngine.Rendering;

namespace CrowdSurge
{
    /// <summary>
    /// Draws many animated characters with GPU instancing and no GameObjects. Every pose of the baked run cycle is a
    /// separate mesh; units are bucketed by their current pose and each bucket is one instanced draw per submesh.
    /// </summary>
    public sealed class InstancedCharacterRenderer
    {
        private readonly Mesh[] _poses;          // run frames followed by the idle pose
        private readonly RenderParams[] _params; // one per submesh (bottom, head, top)
        private readonly Matrix4x4[][] _buckets;
        private readonly int[] _counts;

        public int RunFrameCount { get; }
        public int IdlePose => _poses.Length - 1;

        public InstancedCharacterRenderer(CharacterSet set, Material[] materials, int capacity)
        {
            RunFrameCount = set.FrameCount;
            _poses = new Mesh[RunFrameCount + 1];
            for (int i = 0; i < RunFrameCount; i++) _poses[i] = set.runFrames[i];
            _poses[RunFrameCount] = set.idleFrame;

            _params = new RenderParams[materials.Length];
            for (int i = 0; i < materials.Length; i++) _params[i] = CreateParams(materials[i]);

            _buckets = new Matrix4x4[_poses.Length][];
            for (int i = 0; i < _buckets.Length; i++) _buckets[i] = new Matrix4x4[capacity];
            _counts = new int[_poses.Length];
        }

        public void Begin() => System.Array.Clear(_counts, 0, _counts.Length);

        public void Add(int pose, in Matrix4x4 matrix) => _buckets[pose][_counts[pose]++] = matrix;

        public void Flush()
        {
            for (int pose = 0; pose < _poses.Length; pose++)
            {
                int count = _counts[pose];
                if (count == 0) continue;
                for (int sub = 0; sub < _params.Length; sub++)
                    Graphics.RenderMeshInstanced(_params[sub], _poses[pose], sub, _buckets[pose], count);
            }
        }

        private static RenderParams CreateParams(Material material)
        {
            material.enableInstancing = true;
            return new RenderParams(material)
            {
                shadowCastingMode = ShadowCastingMode.Off,
                receiveShadows = false,
                layer = 0,
                worldBounds = new Bounds(Vector3.zero, Vector3.one * 100000f)
            };
        }
    }
}
