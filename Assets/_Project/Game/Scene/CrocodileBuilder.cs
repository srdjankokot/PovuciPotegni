using UnityEngine;

namespace PovuciPotegni.Game
{
    /// <summary>Assembles the crocodile from primitives, with the shapes and measures of the prototype.</summary>
    public sealed class CrocodileBuilder
    {
        private const uint Green = 0x4F9B4A;
        private const uint DarkGreen = 0x3B7D3A;
        private const uint Belly = 0xC9D98A;
        private const uint Tooth = 0xFFFFFF;
        private const uint EyeYellow = 0xF2E36B;
        private const uint Pupil = 0x12131C;

        private const int TailSegments = 4;
        private const int TeethPerSide = 6;
        private const int BackScales = 5;

        private readonly ISceneAssets _assets;

        public CrocodileBuilder(ISceneAssets assets)
        {
            _assets = assets;
        }

        public GameObject Build()
        {
            var root = new GameObject("crocodile");

            Part("Body", root.transform, Sphere(0.5f, 16, 12), Green, Vector3.zero, null, new Vector3(2f, 0.5f, 0.85f));
            Part("Belly", root.transform, Sphere(0.48f, 14, 10), Belly, new Vector3(0, -0.07f, 0), null, new Vector3(1.9f, 0.38f, 0.75f));

            Transform[] tail = BuildTail(root.transform);
            Transform head = SceneParts.Group("Head", root.transform, new Vector3(0.95f, 0.06f, 0));
            Part("Skull", head, Sphere(0.26f, 14, 10), Green, Vector3.zero, null, new Vector3(1.1f, 0.75f, 1));
            Part("UpperJaw", head, Box(), Green, new Vector3(0.5f, 0.02f, 0), null, new Vector3(0.85f, 0.13f, 0.36f));
            Part("Nostrils", head, Sphere(0.06f, 8, 6), Green, new Vector3(0.86f, 0.1f, 0));

            Transform jaw = SceneParts.Group("Jaw", head, new Vector3(0.05f, -0.06f, 0));
            Part("LowerJaw", jaw, Box(), Belly, new Vector3(0.45f, -0.03f, 0), null, new Vector3(0.85f, 0.08f, 0.32f));

            BuildTeeth(head, jaw);
            BuildEyes(head);
            BuildScalesAndLegs(root.transform);

            root.AddComponent<CrocodileView>().Bind(jaw, head, tail);
            return root;
        }

        private Transform[] BuildTail(Transform root)
        {
            var segments = new Transform[TailSegments];
            Transform parent = SceneParts.Group("Tail", root, new Vector3(-0.95f, 0, 0));
            Mesh spike = _assets.GetMesh("CrocSpike", () => ProceduralMeshes.Cone("CrocSpike", 0.05f, 0.12f, 4, false));

            for (int i = 0; i < TailSegments; i++)
            {
                Transform segment = SceneParts.Group("Segment", parent, new Vector3(i == 0 ? 0 : -0.42f, 0, 0));
                Part("Mesh", segment, Sphere(0.26f - i * 0.05f, 12, 8), Green, new Vector3(-0.2f, 0, 0), null, new Vector3(1.6f, 0.6f, 0.9f));
                Part("Spike", segment, spike, DarkGreen, new Vector3(-0.2f, 0.15f - i * 0.02f, 0));
                segments[i] = segment;
                parent = segment;
            }

            return segments;
        }

        private void BuildTeeth(Transform head, Transform jaw)
        {
            Mesh upper = _assets.GetMesh("CrocToothUpper", () => ProceduralMeshes.Cone("CrocToothUpper", 0.025f, 0.07f, 4, false));
            Mesh lower = _assets.GetMesh("CrocToothLower", () => ProceduralMeshes.Cone("CrocToothLower", 0.022f, 0.06f, 4, false));

            for (int i = 0; i < TeethPerSide; i++)
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    Part("Tooth", head, upper, Tooth, new Vector3(0.2f + i * 0.12f, -0.07f, side * 0.15f), SceneParts.Rotation(0, 0, Mathf.PI));
                    Part("Tooth", jaw, lower, Tooth, new Vector3(0.22f + i * 0.12f, 0.03f, side * 0.13f));
                }
            }
        }

        private void BuildEyes(Transform head)
        {
            foreach (float side in new[] { -1f, 1f })
            {
                Part("Lid", head, Sphere(0.09f, 10, 8), Green, new Vector3(-0.02f, 0.17f, side * 0.13f));
                Part("Eye", head, Sphere(0.06f, 10, 8), EyeYellow, new Vector3(0.02f, 0.2f, side * 0.15f));
                Part("Pupil", head, Box(), Pupil, new Vector3(0.075f, 0.2f, side * 0.16f), null, new Vector3(0.015f, 0.07f, 0.02f));
            }
        }

        private void BuildScalesAndLegs(Transform root)
        {
            Mesh scale = _assets.GetMesh("CrocScale", () => ProceduralMeshes.Cone("CrocScale", 0.05f, 0.11f, 4, false));

            for (int i = 0; i < BackScales; i++)
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    Part("Scale", root, scale, DarkGreen, new Vector3(0.6f - i * 0.28f, 0.24f, side * 0.13f));
                }
            }

            foreach (float x in new[] { 0.55f, -0.55f })
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    Part("Leg", root, Box(), DarkGreen, new Vector3(x, -0.12f, side * 0.42f), null, new Vector3(0.14f, 0.12f, 0.3f));
                }
            }
        }

        private void Part(string name, Transform parent, Mesh mesh, uint color, Vector3 position, Quaternion? rotation = null, Vector3? scale = null)
        {
            SceneParts.Part(name, parent, mesh, _assets.GetMaterial(new Surface(color, true)), position, rotation, scale);
        }

        private Mesh Box()
        {
            return _assets.GetMesh("Box", () => ProceduralMeshes.Box("Box"));
        }

        private Mesh Sphere(float radius, int width, int height)
        {
            string key = $"Sphere_{radius:F3}_{width}_{height}";
            return _assets.GetMesh(key, () => ProceduralMeshes.Sphere(key, radius, width, height, false));
        }
    }
}
