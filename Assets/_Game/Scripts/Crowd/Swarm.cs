using System.Collections.Generic;
using UnityEngine;

namespace CrowdSurge
{
    /// <summary>
    /// A formation of identical animated units (player crowd, enemy squad, boss pack). Owns the unit positions and
    /// renders them instanced. The logical <see cref="Count"/> may be far larger than the number of drawn units.
    /// </summary>
    public sealed class Swarm
    {
        public struct Settings
        {
            public int maxVisible;
            public float preferredSpacing;
            public float maxRadius;
            public float unitScale;
            public float yaw;
            public float followSharpness;
            /// <summary>Run cycles per second.</summary>
            public float runRate;
        }

        private readonly Settings _s;
        private readonly Vector3[] _positions;
        private readonly float[] _phases;
        private readonly float[] _spawn;          // 0..1 pop-in scale of freshly added units
        private readonly InstancedCharacterRenderer _renderer;
        private readonly Quaternion _rotation;
        private float _spacing;

        public readonly List<Vector3> RecentDeaths = new List<Vector3>(32);

        public Vector3 Center;
        public Vector3 EngageOffset;
        /// <summary>Standing pose instead of the run cycle.</summary>
        public bool Idle;
        public int Count { get; private set; }
        public int Visible { get; private set; }
        public float Radius => _spacing * Mathf.Sqrt(Mathf.Max(1, Visible)) + 0.3f * _s.unitScale;

        public Swarm(Settings settings, CharacterSet set, Material[] materials)
        {
            _s = settings;
            _positions = new Vector3[settings.maxVisible];
            _phases = new float[settings.maxVisible];
            _spawn = new float[settings.maxVisible];
            _renderer = new InstancedCharacterRenderer(set, materials, settings.maxVisible);
            _rotation = Quaternion.Euler(0f, settings.yaw, 0f);
            for (int i = 0; i < _phases.Length; i++) _phases[i] = Random.value;
            _spacing = settings.preferredSpacing;
        }

        public void SetCount(int count)
        {
            Count = Mathf.Max(0, count);
            SyncVisible();
        }

        /// <summary>Instantly snaps every drawn unit to its formation slot (used at level start).</summary>
        public void SnapToFormation()
        {
            RefreshSpacing();
            for (int i = 0; i < Visible; i++)
            {
                _positions[i] = Center + EngageOffset + SlotOffset(i);
                _spawn[i] = 1f;
            }
        }

        public void Tick(float dt)
        {
            RefreshSpacing();
            float t = 1f - Mathf.Exp(-_s.followSharpness * dt);
            Vector3 anchor = Center + EngageOffset;
            for (int i = 0; i < Visible; i++)
            {
                _positions[i] = Vector3.Lerp(_positions[i], anchor + SlotOffset(i), t);
                if (_spawn[i] < 1f) _spawn[i] = Mathf.Min(1f, _spawn[i] + dt * 4.5f);
            }
        }

        /// <param name="time">Game time in seconds.</param>
        /// <param name="runSpeed">Animation speed multiplier (1 = normal).</param>
        /// <param name="hop">Victory hopping height in metres (0 = none).</param>
        public void Render(float time, float runSpeed, float hop)
        {
            float baseScale = _s.unitScale;
            int frames = _renderer.RunFrameCount;
            _renderer.Begin();

            for (int i = 0; i < Visible; i++)
            {
                float pop = _spawn[i];
                float scale = baseScale * (pop < 1f ? EaseOutBack(pop) : 1f);

                int pose;
                if (Idle) pose = _renderer.IdlePose;
                else pose = Mathf.Min(frames - 1, (int)(Mathf.Repeat(time * _s.runRate * runSpeed + _phases[i], 1f) * frames));

                Vector3 p = _positions[i];
                if (hop > 0f) p.y += Mathf.Abs(Mathf.Sin(time * 7f + _phases[i] * 6.2832f)) * hop;
                _renderer.Add(pose, Matrix4x4.TRS(p, _rotation, new Vector3(scale, scale, scale)));
            }
            _renderer.Flush();
        }

        /// <summary>
        /// Removes every drawn unit inside the circle (XZ). Each drawn unit stands for several logical units when
        /// Count exceeds the drawn cap. Returns the number of logical units removed.
        /// </summary>
        public int KillInCircle(float cx, float cz, float radius)
        {
            RecentDeaths.Clear();
            if (Visible == 0) return 0;

            int unitsPerVisual = Mathf.Max(1, Mathf.CeilToInt(Count / (float)Visible));
            float sqr = radius * radius;
            int removed = 0;

            for (int i = Visible - 1; i >= 0; i--)
            {
                Vector3 p = _positions[i];
                float dx = p.x - cx, dz = p.z - cz;
                if (dx * dx + dz * dz > sqr) continue;

                RecentDeaths.Add(p);
                removed += unitsPerVisual;
                _positions[i] = _positions[Visible - 1];
                _phases[i] = _phases[Visible - 1];
                _spawn[i] = _spawn[Visible - 1];
                Visible--;
            }

            if (removed > 0) SetCount(Count - removed);
            return removed;
        }

        /// <summary>World position of a random drawn unit (for effects). Falls back to the centre.</summary>
        public Vector3 RandomUnitPosition()
            => Visible > 0 ? _positions[Random.Range(0, Visible)] : Center;

        private static float EaseOutBack(float k)
        {
            const float c1 = 1.70158f, c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(k - 1f, 3f) + c1 * Mathf.Pow(k - 1f, 2f);
        }

        private void RefreshSpacing()
            => _spacing = FormationLayout.Spacing(Visible, _s.preferredSpacing, _s.maxRadius);

        private Vector3 SlotOffset(int index)
        {
            Vector2 s = FormationLayout.Slot(index);
            return new Vector3(s.x * _spacing, 0f, s.y * _spacing);
        }

        private void SyncVisible()
        {
            int desired = Mathf.Min(Count, _s.maxVisible);
            while (Visible < desired)
            {
                _positions[Visible] = Center + EngageOffset;
                _spawn[Visible] = 0f; // new members pop in
                Visible++;
            }
            Visible = Mathf.Min(Visible, desired);
        }
    }
}
