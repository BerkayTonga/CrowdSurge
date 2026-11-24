using UnityEngine;

namespace CrowdSurge
{
    /// <summary>Builds the road, rails, finish arch and decorative clouds as a few combined meshes.</summary>
    public static class TrackBuilder
    {
        private const float StripeLength = 12f;

        public static Transform Build(Transform parent, GameAssets assets, LevelConfig level, Material roadLight, Material roadDark, Material rail)
        {
            var root = new GameObject("Track").transform;
            root.SetParent(parent, false);

            float finishZ = level.finishZ;
            float endZ = finishZ + GameConstants.TrackEndPadding;
            BuildRoad(root, roadLight, roadDark, endZ);
            BuildRails(root, rail, endZ);
            BuildFinish(root, assets, finishZ);
            BuildClouds(root, assets, endZ);
            return root;
        }

        private static void BuildRoad(Transform root, Material light, Material dark, float endZ)
        {
            var b = new LowPolyMeshBuilder(2);
            float half = GameConstants.RoadHalfWidth;
            int stripe = 0;
            for (float z = GameConstants.TrackStartZ; z < endZ; z += StripeLength, stripe++)
            {
                float z1 = Mathf.Min(z + StripeLength, endZ);
                int sub = stripe % 2;
                b.AddQuad(new Vector3(-half, 0f, z), new Vector3(-half, 0f, z1), new Vector3(half, 0f, z1), new Vector3(half, 0f, z), sub);
            }

            var go = new GameObject("Road");
            go.transform.SetParent(root, false);
            go.AddComponent<MeshFilter>().sharedMesh = b.Build("Road");
            var mr = go.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new[] { light, dark };
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;
        }

        private static void BuildRails(Transform root, Material rail, float endZ)
        {
            float length = endZ - GameConstants.TrackStartZ;
            float centerZ = GameConstants.TrackStartZ + length * 0.5f;
            float x = GameConstants.RoadHalfWidth + 0.25f;
            MeshFactory.CreateBox(root, "RailL", new Vector3(-x, 0.2f, centerZ), new Vector3(0.5f, 0.4f, length), rail);
            MeshFactory.CreateBox(root, "RailR", new Vector3(x, 0.2f, centerZ), new Vector3(0.5f, 0.4f, length), rail);
        }

        private static void BuildFinish(Transform root, GameAssets assets, float finishZ)
        {
            var finish = new GameObject("Finish").transform;
            finish.SetParent(root, false);

            // Checkered strip
            var b = new LowPolyMeshBuilder(2);
            const int cols = 10, rows = 3;
            float tile = GameConstants.RoadWidth / cols;
            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                float x0 = -GameConstants.RoadHalfWidth + c * tile, z0 = finishZ + r * tile;
                int sub = (r + c) % 2;
                b.AddQuad(new Vector3(x0, 0.02f, z0), new Vector3(x0, 0.02f, z0 + tile), new Vector3(x0 + tile, 0.02f, z0 + tile), new Vector3(x0 + tile, 0.02f, z0), sub);
            }
            var strip = new GameObject("Checker");
            strip.transform.SetParent(finish, false);
            strip.AddComponent<MeshFilter>().sharedMesh = b.Build("Checker");
            var mr = strip.AddComponent<MeshRenderer>();
            mr.sharedMaterials = new[] { assets.finishWhite, assets.finishBlack };
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            mr.receiveShadows = false;

            // Arch
            float x = GameConstants.RoadHalfWidth + 0.25f;
            MeshFactory.CreateBox(finish, "PostL", new Vector3(-x, 2.4f, finishZ), new Vector3(0.6f, 4.8f, 0.6f), assets.finishBlack);
            MeshFactory.CreateBox(finish, "PostR", new Vector3(x, 2.4f, finishZ), new Vector3(0.6f, 4.8f, 0.6f), assets.finishBlack);
            MeshFactory.CreateBox(finish, "Banner", new Vector3(0f, 4.6f, finishZ), new Vector3(GameConstants.RoadWidth + 1.4f, 1.4f, 0.4f), assets.finishWhite);

            var label = WorldLabel.Create(finish, "FinishLabel", 1.1f, new Color(0.1f, 0.1f, 0.15f));
            label.transform.position = new Vector3(0f, 4.6f, finishZ - 0.35f);
            label.SetText("FINISH");
        }

        private static void BuildClouds(Transform root, GameAssets assets, float endZ)
        {
            var mesh = MeshFactory.CreateCloud();
            var rng = new System.Random(42);
            var cloudRoot = new GameObject("Clouds").transform;
            cloudRoot.SetParent(root, false);

            for (int i = 0; i < 26; i++)
            {
                float side = i % 2 == 0 ? -1f : 1f;
                float x = side * (24f + (float)rng.NextDouble() * 40f);
                float y = -10f + (float)rng.NextDouble() * 12f;
                float z = GameConstants.TrackStartZ + (float)rng.NextDouble() * (endZ + 40f);
                float scale = 2.5f + (float)rng.NextDouble() * 4f;

                var go = new GameObject("Cloud");
                go.transform.SetParent(cloudRoot, false);
                go.transform.localPosition = new Vector3(x, y, z);
                go.transform.localScale = new Vector3(scale * 1.6f, scale * 0.8f, scale);
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var mr = go.AddComponent<MeshRenderer>();
                mr.sharedMaterial = assets.cloud;
                mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                mr.receiveShadows = false;
            }
        }
    }
}
