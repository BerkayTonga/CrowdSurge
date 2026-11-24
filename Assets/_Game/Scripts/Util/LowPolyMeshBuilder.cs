using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace CrowdSurge
{
    /// <summary>Builds flat-shaded low-poly meshes with multiple submeshes (one material per submesh).</summary>
    public sealed class LowPolyMeshBuilder
    {
        private readonly List<Vector3> _vertices = new List<Vector3>(256);
        private readonly List<Vector3> _normals = new List<Vector3>(256);
        private readonly List<int>[] _indices;

        public LowPolyMeshBuilder(int submeshCount)
        {
            _indices = new List<int>[submeshCount];
            for (int i = 0; i < submeshCount; i++) _indices[i] = new List<int>(256);
        }

        /// <summary>Adds a triangle (clockwise winding when viewed from the front) with a flat normal.</summary>
        public void AddTriangle(Vector3 a, Vector3 b, Vector3 c, int submesh)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a).normalized;
            int start = _vertices.Count;
            _vertices.Add(a); _vertices.Add(b); _vertices.Add(c);
            _normals.Add(normal); _normals.Add(normal); _normals.Add(normal);
            var list = _indices[submesh];
            list.Add(start); list.Add(start + 1); list.Add(start + 2);
        }

        public void AddQuad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, int submesh)
        {
            AddTriangle(a, b, c, submesh);
            AddTriangle(a, c, d, submesh);
        }

        public void AddBox(Vector3 center, Vector3 size, Quaternion rotation, int submesh)
        {
            Vector3 h = size * 0.5f;
            Vector3 Corner(float sx, float sy, float sz) => center + rotation * new Vector3(h.x * sx, h.y * sy, h.z * sz);

            Vector3 lbf = Corner(-1, -1, -1), rbf = Corner(1, -1, -1), rtf = Corner(1, 1, -1), ltf = Corner(-1, 1, -1);
            Vector3 lbb = Corner(-1, -1, 1), rbb = Corner(1, -1, 1), rtb = Corner(1, 1, 1), ltb = Corner(-1, 1, 1);

            AddQuad(lbf, ltf, rtf, rbf, submesh); // -z
            AddQuad(rbb, rtb, ltb, lbb, submesh); // +z
            AddQuad(lbb, ltb, ltf, lbf, submesh); // -x
            AddQuad(rbf, rtf, rtb, rbb, submesh); // +x
            AddQuad(ltf, ltb, rtb, rtf, submesh); // +y
            AddQuad(lbb, lbf, rbf, rbb, submesh); // -y
        }

        public void AddSphere(Vector3 center, float radius, int segments, int rings, int submesh)
        {
            for (int r = 0; r < rings; r++)
            {
                float v0 = Mathf.PI * r / rings, v1 = Mathf.PI * (r + 1) / rings;
                for (int s = 0; s < segments; s++)
                {
                    float u0 = Mathf.PI * 2f * s / segments, u1 = Mathf.PI * 2f * (s + 1) / segments;
                    Vector3 p00 = center + SpherePoint(radius, u0, v0);
                    Vector3 p10 = center + SpherePoint(radius, u1, v0);
                    Vector3 p01 = center + SpherePoint(radius, u0, v1);
                    Vector3 p11 = center + SpherePoint(radius, u1, v1);

                    if (r == 0) AddTriangle(p00, p11, p01, submesh);
                    else if (r == rings - 1) AddTriangle(p00, p10, p01, submesh);
                    else AddQuad(p00, p10, p11, p01, submesh);
                }
            }
        }

        /// <summary>Upright cylinder / cone frustum around the Y axis.</summary>
        public void AddCylinder(Vector3 baseCenter, float bottomRadius, float topRadius, float height, int segments, int submesh)
        {
            Vector3 top = baseCenter + Vector3.up * height;
            for (int s = 0; s < segments; s++)
            {
                float a0 = Mathf.PI * 2f * s / segments, a1 = Mathf.PI * 2f * (s + 1) / segments;
                Vector3 b0 = baseCenter + Ring(bottomRadius, a0), b1 = baseCenter + Ring(bottomRadius, a1);
                Vector3 t0 = top + Ring(topRadius, a0), t1 = top + Ring(topRadius, a1);
                AddQuad(b0, t0, t1, b1, submesh);
                AddTriangle(top, t1, t0, submesh);
                AddTriangle(baseCenter, b0, b1, submesh);
            }
        }

        public Mesh Build(string name)
        {
            var mesh = new Mesh { name = name, indexFormat = IndexFormat.UInt16 };
            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.subMeshCount = _indices.Length;
            for (int i = 0; i < _indices.Length; i++) mesh.SetTriangles(_indices[i], i, false);
            mesh.RecalculateBounds();
            mesh.UploadMeshData(true);
            return mesh;
        }

        private static Vector3 SpherePoint(float radius, float u, float v)
            => new Vector3(Mathf.Cos(u) * Mathf.Sin(v), Mathf.Cos(v), Mathf.Sin(u) * Mathf.Sin(v)) * radius;

        private static Vector3 Ring(float radius, float angle)
            => new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
    }
}
