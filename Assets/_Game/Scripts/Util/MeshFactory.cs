using UnityEngine;

namespace CrowdSurge
{
    /// <summary>Procedural low-poly meshes so the project needs no external art to look good.</summary>
    public static class MeshFactory
    {
        private static Mesh s_unitCube;

        /// <summary>1x1x1 cube centred on the origin, flat shaded. Shared by every box in the level.</summary>
        public static Mesh UnitCube()
        {
            if (s_unitCube == null)
            {
                var b = new LowPolyMeshBuilder(1);
                b.AddBox(Vector3.zero, Vector3.one, Quaternion.identity, 0);
                s_unitCube = b.Build("UnitCube");
            }
            return s_unitCube;
        }

        /// <summary>Creates a renderer-only box (no collider) under <paramref name="parent"/>.</summary>
        public static Transform CreateBox(Transform parent, string name, Vector3 localPos, Vector3 size, Material material)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = size;
            go.AddComponent<MeshFilter>().sharedMesh = UnitCube();
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterial = material;
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
            return go.transform;
        }

        /// <summary>Submesh 0 = toothed blade, submesh 1 = hub. Lies flat on the XZ plane, pivot at centre.</summary>
        public static Mesh CreateSawBlade(float radius = 1.45f, int teeth = 12)
        {
            var b = new LowPolyMeshBuilder(2);
            float inner = radius * 0.78f;
            const float halfThickness = 0.14f;
            float step = Mathf.PI * 2f / teeth;

            for (int i = 0; i < teeth; i++)
            {
                float a0 = i * step;
                float aTip = a0 + step * 0.75f;
                float a1 = a0 + step;

                Vector3 v0 = Polar(inner, a0), tip = Polar(radius, aTip), v1 = Polar(inner, a1);
                Vector3 up = Vector3.up * halfThickness, down = Vector3.down * halfThickness;

                // top and bottom faces
                b.AddTriangle(up, tip + up, v0 + up, 0);
                b.AddTriangle(up, v1 + up, tip + up, 0);
                b.AddTriangle(down, v0 + down, tip + down, 0);
                b.AddTriangle(down, tip + down, v1 + down, 0);

                // side walls
                b.AddQuad(v0 + down, v0 + up, tip + up, tip + down, 0);
                b.AddQuad(tip + down, tip + up, v1 + up, v1 + down, 0);
            }

            b.AddCylinder(new Vector3(0f, halfThickness - 0.02f, 0f), radius * 0.34f, radius * 0.26f, 0.12f, 10, 1);
            return b.Build("SawBlade");
        }

        /// <summary>Fluffy low-poly cloud made from squashed spheres.</summary>
        public static Mesh CreateCloud()
        {
            var b = new LowPolyMeshBuilder(1);
            b.AddSphere(new Vector3(0f, 0f, 0f), 1.0f, 7, 4, 0);
            b.AddSphere(new Vector3(1.1f, -0.15f, 0.1f), 0.75f, 7, 4, 0);
            b.AddSphere(new Vector3(-1.05f, -0.2f, -0.1f), 0.7f, 7, 4, 0);
            b.AddSphere(new Vector3(0.2f, 0.35f, 0.5f), 0.65f, 7, 4, 0);
            return b.Build("Cloud");
        }

        private static Vector3 Polar(float radius, float angle)
            => new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
    }
}
