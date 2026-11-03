using System.Collections.Generic;
using UnityEngine;

namespace PovuciPotegni.Game
{
    /// <summary>
    /// Builds the river arena from docs/SCENA.md. The random sequence and its order match the prototype,
    /// so trees, rocks and clouds land in the same places as in reference/slike.
    /// </summary>
    public sealed class ArenaBuilder
    {
        public const string StaticGroupName = "Static";

        private const uint GrassColor = 0x8FD07C;
        private const uint PathColor = 0xB8E3A4;
        private const uint MarkColor = 0xFFFFFF;
        private const uint BankColor = 0xA9784A;
        private const uint WaterColor = 0x4CB2EC;
        private const uint RippleColor = 0xD8F0FF;
        private const uint TrunkColor = 0x8A5A36;
        private const uint RockColor = 0xA7ADB8;
        private const uint CloudColor = 0xFFFFFF;
        private const float CloudEmission = 0.35f;
        private const float NearLandEnd = -41.5f;
        private const float FarLandEnd = 61.5f;
        private const float BankRimHeight = 0.008f;
        private const float RiverMargin = 0.6f;

        private static readonly uint[] LeafColors = { 0x58A865, 0x4B9A5A, 0x6BB873 };

        private readonly ISceneAssets _assets;
        private long _seed = 11;

        private Mesh _box;

        public ArenaBuilder(ISceneAssets assets)
        {
            _assets = assets;
        }

        public GameObject Build()
        {
            _box = _assets.GetMesh("Box", () => ProceduralMeshes.Box("Box"));

            var root = new GameObject("ArenaRoot");
            Transform staticGroup = SceneParts.Group(StaticGroupName, root.transform);

            BuildGround(staticGroup);
            BuildWater(staticGroup);
            List<Transform> ripples = BuildRipples(root.transform, out float[] speeds, out float[] shares);
            BuildTrees(staticGroup);
            BuildRocks(staticGroup);
            BuildHills(staticGroup);
            List<Transform> clouds = BuildClouds(root.transform);
            new ArenaDecor(_assets).Build(staticGroup);

            root.AddComponent<AmbientMotion>().Bind(ripples.ToArray(), speeds, shares, clouds.ToArray());
            return root;
        }

        private float NextRandom()
        {
            _seed = _seed * 16807 % 2147483647;
            return _seed / 2147483647f;
        }

        private Material Flat(uint color, float emission = 0)
        {
            return _assets.GetMaterial(new Surface(color, emission: emission));
        }

        private void Box(string name, Transform parent, uint color, Vector3 center, Vector3 size)
        {
            SceneParts.Part(name, parent, _box, Flat(color), center, null, size, false);
        }

        private void BuildGround(Transform parent)
        {
            Mesh nearLand = _assets.GetMesh("NearLand", () => RiverShape.Strip("NearLand", _ => NearLandEnd, x => RiverShape.BankZ(x, -1)));
            Mesh farLand = _assets.GetMesh("FarLand", () => RiverShape.Strip("FarLand", x => RiverShape.BankZ(x, 1), _ => FarLandEnd));
            SceneParts.Part("NearLand", parent, nearLand, Flat(GrassColor), Vector3.zero, null, null, false);
            SceneParts.Part("FarLand", parent, farLand, Flat(GrassColor), Vector3.zero, null, null, false);
            Box("NearPath", parent, PathColor, new Vector3(0, 0.01f, -13.5f), new Vector3(3.2f, 0.02f, 24));
            Box("FarPath", parent, PathColor, new Vector3(0, 0.01f, 13.5f), new Vector3(3.2f, 0.02f, 24));

            foreach (float distance in new[] { 2.6f, 3.8f, 5f })
            {
                foreach (float side in new[] { -1f, 1f })
                {
                    Box("StepMark", parent, MarkColor, new Vector3(0, 0.03f, side * distance), new Vector3(2.6f, 0.03f, 0.09f));
                }
            }

            foreach (float side in new[] { -1f, 1f })
            {
                string suffix = side < 0 ? "Near" : "Far";
                Mesh rim = _assets.GetMesh("BankRim" + suffix, () => RiverShape.Strip(
                    "BankRim" + suffix,
                    x => RiverShape.BankZ(x, side) - RiverShape.BankRim,
                    x => RiverShape.BankZ(x, side) + RiverShape.BankRim));
                Mesh wall = _assets.GetMesh("BankWall" + suffix, () => RiverShape.BankWall("BankWall" + suffix, side));

                SceneParts.Part("BankRim", parent, rim, Flat(BankColor), new Vector3(0, BankRimHeight, 0), null, null, false);
                SceneParts.Part("BankWall", parent, wall, Flat(BankColor), Vector3.zero, null, null, false);
            }
        }

        private void BuildWater(Transform parent)
        {
            Mesh water = _assets.GetMesh("Water", () => RiverShape.Strip(
                "Water",
                x => RiverShape.BankZ(x, -1) - RiverShape.BankRim,
                x => RiverShape.BankZ(x, 1) + RiverShape.BankRim));
            SceneParts.Part("Water", parent, water, Flat(WaterColor), new Vector3(0, -0.32f, 0), null, null, false);
        }

        private List<Transform> BuildRipples(Transform root, out float[] speeds, out float[] shares)
        {
            const int count = 9;
            Transform group = SceneParts.Group("Ripples", root);
            var ripples = new List<Transform>(count);
            speeds = new float[count];
            shares = new float[count];

            for (int i = 0; i < count; i++)
            {
                float width = 0.7f + NextRandom() * 1.2f;
                float x = -18 + NextRandom() * 36;
                // The prototype's position across the river, kept as a share of its width as the ripple moves.
                float acrossShare = (-1.1f + NextRandom() * 2.2f) / RiverShape.BaseHalfWidth;
                speeds[i] = 0.3f + NextRandom() * 0.5f;

                ripples.Add(SceneParts.Part(
                    "Ripple",
                    group,
                    _box,
                    Flat(RippleColor),
                    AmbientMotion.RipplePosition(x, acrossShare),
                    RiverShape.FlowRotation(x),
                    new Vector3(width, 0.01f, 0.07f),
                    false));
                shares[i] = acrossShare;
            }

            return ripples;
        }

        private void BuildTrees(Transform parent)
        {
            Mesh trunk = _assets.GetMesh("TreeTrunk", () => ProceduralMeshes.Cylinder("TreeTrunk", 0.12f, 0.17f, 0.8f, 6, true));
            Mesh lowerLeaves = _assets.GetMesh("TreeLeavesLow", () => ProceduralMeshes.Cone("TreeLeavesLow", 0.85f, 1.7f, 7, true));
            Mesh upperLeaves = _assets.GetMesh("TreeLeavesHigh", () => ProceduralMeshes.Cone("TreeLeavesHigh", 0.6f, 1.2f, 7, true));
            Transform group = SceneParts.Group("Trees", parent);

            for (int i = 0; i < 60; i++)
            {
                float side = NextRandom() < 0.5f ? -1 : 1;
                float x = side * (4.2f + NextRandom() * 24);
                float z = NextRandom() < 0.35f ? 3.5f + NextRandom() * 20 : -(3.5f + NextRandom() * 45);

                // No trees between the camera and the player.
                if (z > 7 && Mathf.Abs(x) < 9)
                {
                    continue;
                }

                uint leafColor = LeafColors[Mathf.FloorToInt(NextRandom() * 3)];
                float scale = 0.8f + NextRandom() * 0.7f;
                float rotation = NextRandom() * Mathf.PI;

                // Checked after drawing all random values, so the prototype's sequence stays intact.
                if (RiverShape.DistanceToRiver(x, -z) < RiverMargin)
                {
                    continue;
                }

                Transform tree = SceneParts.Group("Tree", group, new Vector3(x, 0, -z));
                tree.localScale = Vector3.one * scale;
                tree.localRotation = SceneParts.Rotation(0, -rotation);

                SceneParts.Part("Trunk", tree, trunk, Flat(TrunkColor), new Vector3(0, 0.4f, 0));
                SceneParts.Part("LeavesLow", tree, lowerLeaves, Flat(leafColor), new Vector3(0, 1.5f, 0));
                SceneParts.Part("LeavesHigh", tree, upperLeaves, Flat(leafColor), new Vector3(0, 2.25f, 0));
            }
        }

        private void BuildRocks(Transform parent)
        {
            Mesh rock = _assets.GetMesh("Rock", () => ProceduralMeshes.Dodecahedron("Rock"));
            Transform group = SceneParts.Group("Rocks", parent);

            for (int i = 0; i < 12; i++)
            {
                float radius = 0.25f + NextRandom() * 0.35f;
                float side = NextRandom() < 0.5f ? -1 : 1;
                float x = side * (2.4f + NextRandom() * 12);
                float zSide = NextRandom() < 0.5f ? 1 : -1;
                float z = zSide * (2 + NextRandom() * 6);
                Quaternion rotation = SceneParts.Rotation(NextRandom(), NextRandom(), NextRandom());

                if (RiverShape.DistanceToRiver(x, -z) < RiverMargin)
                {
                    continue;
                }

                SceneParts.Part("Rock", group, rock, Flat(RockColor), new Vector3(x, 0.05f, -z), rotation, Vector3.one * radius);
            }
        }

        private void BuildHills(Transform parent)
        {
            Mesh hill = _assets.GetMesh("Hill", () => ProceduralMeshes.Sphere("Hill", 1, 8, 6, true));
            Transform group = SceneParts.Group("Hills", parent);
            var hills = new[]
            {
                (X: -34f, Radius: 18f, Color: 0x9FCFA6u),
                (X: -10f, Radius: 22f, Color: 0x8CC49Au),
                (X: 16f, Radius: 20f, Color: 0x9FCFA6u),
                (X: 40f, Radius: 17f, Color: 0x8CC49Au),
                (X: 2f, Radius: 15f, Color: 0xA8D6AEu),
            };

            foreach ((float x, float radius, uint color) in hills)
            {
                SceneParts.Part(
                    "Hill",
                    group,
                    hill,
                    Flat(color),
                    new Vector3(x, -radius * 0.62f, 72),
                    null,
                    Vector3.one * radius,
                    false);
            }
        }

        private List<Transform> BuildClouds(Transform root)
        {
            Mesh puff = _assets.GetMesh("CloudPuff", () => ProceduralMeshes.Sphere("CloudPuff", 1, 7, 6, true));
            Transform group = SceneParts.Group("Clouds", root);
            var clouds = new List<Transform>();

            for (int i = 0; i < 7; i++)
            {
                Transform cloud = SceneParts.Group("Cloud", group);
                int puffs = 3 + Mathf.FloorToInt(NextRandom() * 2);

                for (int j = 0; j < puffs; j++)
                {
                    float radius = 1 + NextRandom() * 0.9f;
                    var position = new Vector3(j * 1.4f - puffs * 0.6f, NextRandom() * 0.5f, NextRandom() * 0.6f);
                    SceneParts.Part("Puff", cloud, puff, Flat(CloudColor, CloudEmission), position, null, Vector3.one * radius, false);
                }

                float x = -30 + NextRandom() * 60;
                float y = 13 + NextRandom() * 5;
                float z = -30 - NextRandom() * 25;
                cloud.localPosition = new Vector3(x, y, -z);
                clouds.Add(cloud);
            }

            return clouds;
        }
    }
}
