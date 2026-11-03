using UnityEngine;

namespace PovuciPotegni.Game
{
    /// <summary>
    /// Small details around the arena. It uses its own random sequence, so the trees and rocks taken from the
    /// prototype stay where they are. Tall objects keep away from the view between the camera and the player.
    /// </summary>
    public sealed class ArenaDecor
    {
        private const float PathClearance = 2.1f;
        private const float RiverClearance = 1.85f;
        private const float RiverMargin = 0.35f;
        private const float CameraCorridor = 5.5f;
        private const float BushPathClearance = 4f;

        private const uint WoodColor = 0x9A6B3F;
        private const uint DarkWoodColor = 0x7F5732;
        private const uint StoneColor = 0xA7ADB8;
        private const uint PebbleColor = 0xC9CDD4;
        private const uint StemColor = 0x4B9A5A;
        private const uint ReedColor = 0x5E9C57;
        private const uint CattailColor = 0x6E4A2E;
        private const uint LilyPadColor = 0x4FA65A;
        private const uint MushroomStemColor = 0xF3EEDF;
        private const uint MushroomCapColor = 0xD94A3D;
        private const uint StumpTopColor = 0xC9A06A;
        private const float DeepWaterInset = 0.75f;
        private const float ShallowInner = 0.17f;
        private const float ShallowOuter = 0.29f;

        private const uint DeepWaterColor = 0x3FA6E4;
        private const uint ShallowWaterColor = 0x8FD6F7;

        private static readonly uint[] GrassPatchColors = { 0x86C873, 0x9BD987 };
        private static readonly uint[] TuftColors = { 0x6FBF62, 0x7DC96E, 0x5FAF58 };
        private static readonly uint[] BushColors = { 0x4F9F5C, 0x5FAF62, 0x6BB873 };
        private static readonly uint[] CrownColors = { 0x62B368, 0x74C06E, 0x8FC75E };
        private static readonly uint[] PetalColors = { 0xFFFFFF, 0xF7D84A, 0xF29BC1, 0xB98CF0 };

        private readonly ISceneAssets _assets;
        private long _seed = 77;

        public ArenaDecor(ISceneAssets assets)
        {
            _assets = assets;
        }

        /// <summary>Low details may sit anywhere off the path; tall ones must also leave the camera's view free.</summary>
        public static bool IsClear(float x, float z, bool tall)
        {
            if (Mathf.Abs(x) < PathClearance || RiverShape.DistanceToRiver(x, z) < RiverMargin)
            {
                return false;
            }

            return !tall || z > 0 || Mathf.Abs(x) >= CameraCorridor;
        }

        public void Build(Transform parent)
        {
            Transform group = SceneParts.Group("Decor", parent);

            BuildWaterDepth(group);
            BuildGrassPatches(group);
            BuildGrassTufts(group);
            BuildFlowers(group);
            BuildBushes(group);
            BuildRoundTrees(group);
            BuildStumps(group);
            BuildMushrooms(group);
            BuildPathPebbles(group);
            BuildFence(group);
            BuildSign(group);
            BuildRiverStones(group);
            BuildReeds(group);
            BuildLilyPads(group);
        }

        private float Random01()
        {
            _seed = _seed * 16807 % 2147483647;
            return _seed / 2147483647f;
        }

        private float Range(float min, float max)
        {
            return min + Random01() * (max - min);
        }

        private uint Pick(uint[] colors)
        {
            return colors[Mathf.Min(Mathf.FloorToInt(Random01() * colors.Length), colors.Length - 1)];
        }

        private Material Flat(uint color)
        {
            return _assets.GetMaterial(new Surface(color));
        }

        private Mesh Box()
        {
            return _assets.GetMesh("Box", () => ProceduralMeshes.Box("Box"));
        }

        /// <summary>Finds a free spot, three times out of four on the far side, where most of the view is.</summary>
        private bool TryPlace(bool tall, float nearLimit, float farLimit, float width, out float x, out float z)
        {
            for (int attempt = 0; attempt < 12; attempt++)
            {
                x = Range(-width, width);
                z = Random01() < 0.25f ? -Range(RiverClearance, nearLimit) : Range(RiverClearance, farLimit);
                if (IsClear(x, z, tall))
                {
                    return true;
                }
            }

            x = 0;
            z = 0;
            return false;
        }

        private void BuildWaterDepth(Transform parent)
        {
            Mesh deep = _assets.GetMesh("DeepWater", () => RiverShape.Strip(
                "DeepWater",
                x => RiverShape.Center(x) - (RiverShape.HalfWidth(x) - DeepWaterInset),
                x => RiverShape.Center(x) + (RiverShape.HalfWidth(x) - DeepWaterInset)));
            SceneParts.Part("DeepWater", parent, deep, Flat(DeepWaterColor), new Vector3(0, -0.316f, 0), null, null, false);

            foreach (float side in new[] { -1f, 1f })
            {
                string name = side < 0 ? "ShallowNear" : "ShallowFar";
                Mesh shallow = _assets.GetMesh(name, () => RiverShape.Strip(
                    name,
                    x => side < 0 ? RiverShape.BankZ(x, side) + ShallowInner : RiverShape.BankZ(x, side) - ShallowOuter,
                    x => side < 0 ? RiverShape.BankZ(x, side) + ShallowOuter : RiverShape.BankZ(x, side) - ShallowInner));
                SceneParts.Part("ShallowWater", parent, shallow, Flat(ShallowWaterColor), new Vector3(0, -0.312f, 0), null, null, false);
            }
        }

        private void BuildGrassPatches(Transform parent)
        {
            Mesh disc = _assets.GetMesh("GroundDisc", () => ProceduralMeshes.Cylinder("GroundDisc", 1, 1, 0.01f, 9, true));

            for (int i = 0; i < 30; i++)
            {
                if (TryPlace(false, 16, 45, 34, out float x, out float z))
                {
                    var scale = new Vector3(Range(1.2f, 3.2f), 0.2f, Range(0.8f, 2.2f));

                    // A patch must not reach over the river, whichever way it is turned.
                    if (RiverShape.DistanceToRiver(x, z) - Mathf.Max(scale.x, scale.z) > RiverMargin)
                    {
                        SceneParts.Part("GrassPatch", parent, disc, Flat(Pick(GrassPatchColors)), new Vector3(x, 0.004f, z), SceneParts.Rotation(0, Range(0, Mathf.PI)), scale, false);
                    }
                }
            }
        }

        private void BuildGrassTufts(Transform parent)
        {
            Mesh tuft = _assets.GetMesh("GrassTuft", () =>
            {
                Mesh blade = ProceduralMeshes.Cone("Blade", 0.05f, 0.24f, 4, true);
                return ProceduralMeshes.Combine(
                    "GrassTuft",
                    (blade, Matrix4x4.TRS(new Vector3(0, 0.12f, 0), Quaternion.identity, Vector3.one)),
                    (blade, Matrix4x4.TRS(new Vector3(0.06f, 0.09f, 0.03f), SceneParts.Rotation(0.1f, 0, -0.35f), Vector3.one * 0.8f)),
                    (blade, Matrix4x4.TRS(new Vector3(-0.06f, 0.09f, -0.02f), SceneParts.Rotation(-0.1f, 0, 0.35f), Vector3.one * 0.8f)));
            });

            for (int i = 0; i < 110; i++)
            {
                if (TryPlace(false, 15, 28, 18, out float x, out float z))
                {
                    SceneParts.Part("GrassTuft", parent, tuft, Flat(Pick(TuftColors)), new Vector3(x, 0, z), SceneParts.Rotation(0, Range(0, Mathf.PI)), Vector3.one * Range(0.8f, 1.5f), false);
                }
            }
        }

        private void BuildFlowers(Transform parent)
        {
            Mesh stem = _assets.GetMesh("FlowerStem", () => ProceduralMeshes.Cylinder("FlowerStem", 0.008f, 0.012f, 0.22f, 4, true));
            Mesh head = _assets.GetMesh("FlowerHead", () => ProceduralMeshes.Sphere("FlowerHead", 0.055f, 5, 4, true));

            for (int i = 0; i < 46; i++)
            {
                if (TryPlace(false, 14, 24, 14, out float x, out float z))
                {
                    Transform flower = SceneParts.Group("Flower", parent, new Vector3(x, 0, z));
                    flower.localScale = Vector3.one * Range(0.9f, 1.4f);
                    SceneParts.Part("Stem", flower, stem, Flat(StemColor), new Vector3(0, 0.11f, 0), null, null, false);
                    SceneParts.Part("Head", flower, head, Flat(Pick(PetalColors)), new Vector3(0, 0.24f, 0), null, new Vector3(1, 0.7f, 1), false);
                }
            }
        }

        private void BuildBushes(Transform parent)
        {
            Mesh bush = _assets.GetMesh("Bush", () =>
            {
                Mesh puff = ProceduralMeshes.Sphere("BushPuff", 0.4f, 7, 5, true);
                return ProceduralMeshes.Combine(
                    "Bush",
                    (puff, Matrix4x4.TRS(new Vector3(0, 0.32f, 0), Quaternion.identity, Vector3.one)),
                    (puff, Matrix4x4.TRS(new Vector3(0.38f, 0.22f, 0.05f), Quaternion.identity, Vector3.one * 0.72f)),
                    (puff, Matrix4x4.TRS(new Vector3(-0.34f, 0.2f, -0.06f), Quaternion.identity, Vector3.one * 0.66f)));
            });

            for (int i = 0; i < 18; i++)
            {
                if (TryPlace(true, 15, 34, 22, out float x, out float z) && Mathf.Abs(x) >= BushPathClearance)
                {
                    SceneParts.Part("Bush", parent, bush, Flat(Pick(BushColors)), new Vector3(x, 0, z), SceneParts.Rotation(0, Range(0, Mathf.PI)), Vector3.one * Range(0.8f, 1.5f));
                }
            }
        }

        private void BuildRoundTrees(Transform parent)
        {
            Mesh trunk = _assets.GetMesh("TreeTrunk", () => ProceduralMeshes.Cylinder("TreeTrunk", 0.12f, 0.17f, 0.8f, 6, true));
            Mesh crown = _assets.GetMesh("TreeCrown", () =>
            {
                Mesh puff = ProceduralMeshes.Sphere("CrownPuff", 0.8f, 8, 6, true);
                return ProceduralMeshes.Combine(
                    "TreeCrown",
                    (puff, Matrix4x4.TRS(new Vector3(0, 1.55f, 0), Quaternion.identity, Vector3.one)),
                    (puff, Matrix4x4.TRS(new Vector3(0.62f, 1.25f, 0.1f), Quaternion.identity, Vector3.one * 0.68f)),
                    (puff, Matrix4x4.TRS(new Vector3(-0.58f, 1.3f, -0.12f), Quaternion.identity, Vector3.one * 0.72f)));
            });

            for (int i = 0; i < 14; i++)
            {
                float side = Random01() < 0.5f ? -1 : 1;
                var position = new Vector3(side * Range(5.5f, 30), 0, Range(7, 48));
                Transform tree = SceneParts.Group("RoundTree", parent, position);
                tree.localScale = Vector3.one * Range(1, 1.7f);
                tree.localRotation = SceneParts.Rotation(0, Range(0, Mathf.PI));
                SceneParts.Part("Trunk", tree, trunk, Flat(DarkWoodColor), new Vector3(0, 0.4f, 0));
                SceneParts.Part("Crown", tree, crown, Flat(Pick(CrownColors)));
            }
        }

        private void BuildStumps(Transform parent)
        {
            Mesh stump = _assets.GetMesh("Stump", () => ProceduralMeshes.Cylinder("Stump", 0.22f, 0.28f, 0.3f, 8, true));
            Mesh top = _assets.GetMesh("StumpTop", () => ProceduralMeshes.Cylinder("StumpTop", 0.2f, 0.2f, 0.012f, 8, true));

            for (int i = 0; i < 5; i++)
            {
                if (TryPlace(true, 14, 24, 14, out float x, out float z))
                {
                    Transform group = SceneParts.Group("Stump", parent, new Vector3(x, 0, z));
                    SceneParts.Part("Bark", group, stump, Flat(DarkWoodColor), new Vector3(0, 0.15f, 0));
                    SceneParts.Part("Rings", group, top, Flat(StumpTopColor), new Vector3(0, 0.302f, 0), null, null, false);
                }
            }
        }

        private void BuildMushrooms(Transform parent)
        {
            Mesh stem = _assets.GetMesh("MushroomStem", () => ProceduralMeshes.Cylinder("MushroomStem", 0.035f, 0.05f, 0.14f, 6, true));
            Mesh cap = _assets.GetMesh("MushroomCap", () => ProceduralMeshes.Sphere("MushroomCap", 0.11f, 7, 4, true, Mathf.PI * 0.55f));

            for (int i = 0; i < 12; i++)
            {
                if (TryPlace(false, 12, 18, 11, out float x, out float z))
                {
                    Transform mushroom = SceneParts.Group("Mushroom", parent, new Vector3(x, 0, z));
                    mushroom.localScale = Vector3.one * Range(0.8f, 1.4f);
                    SceneParts.Part("Stem", mushroom, stem, Flat(MushroomStemColor), new Vector3(0, 0.07f, 0), null, null, false);
                    SceneParts.Part("Cap", mushroom, cap, Flat(MushroomCapColor), new Vector3(0, 0.13f, 0), null, null, false);
                }
            }
        }

        private void BuildPathPebbles(Transform parent)
        {
            Mesh rock = _assets.GetMesh("Rock", () => ProceduralMeshes.Dodecahedron("Rock"));

            foreach (float zSide in new[] { -1f, 1f })
            {
                foreach (float xSide in new[] { -1f, 1f })
                {
                    for (float distance = 2.1f; distance < 25; distance += Range(0.5f, 1.4f))
                    {
                        var position = new Vector3(xSide * Range(1.68f, 1.82f), 0.02f, zSide * distance);
                        var scale = new Vector3(Range(0.06f, 0.13f), Range(0.04f, 0.07f), Range(0.06f, 0.12f));
                        SceneParts.Part("Pebble", parent, rock, Flat(PebbleColor), position, SceneParts.Rotation(0, Range(0, Mathf.PI)), scale, false);
                    }
                }
            }
        }

        private void BuildFence(Transform parent)
        {
            const float start = 6.5f;
            const float end = 24f;
            const float spacing = 2.2f;

            foreach (float side in new[] { -1f, 1f })
            {
                float x = side * 2.45f;
                for (float z = start; z <= end; z += spacing)
                {
                    SceneParts.Part("Post", parent, Box(), Flat(DarkWoodColor), new Vector3(x, 0.36f, z), null, new Vector3(0.11f, 0.72f, 0.11f));

                    if (z + spacing <= end)
                    {
                        float middle = z + spacing / 2;
                        SceneParts.Part("Rail", parent, Box(), Flat(WoodColor), new Vector3(x, 0.56f, middle), null, new Vector3(0.05f, 0.07f, spacing));
                        SceneParts.Part("Rail", parent, Box(), Flat(WoodColor), new Vector3(x, 0.28f, middle), null, new Vector3(0.05f, 0.07f, spacing));
                    }
                }
            }
        }

        private void BuildSign(Transform parent)
        {
            Transform sign = SceneParts.Group("Sign", parent, new Vector3(2.75f, 0, 4.4f));
            sign.localRotation = SceneParts.Rotation(0, 0.3f);
            sign.localScale = Vector3.one * 0.8f;
            SceneParts.Part("Post", sign, Box(), Flat(DarkWoodColor), new Vector3(0, 0.55f, 0), null, new Vector3(0.1f, 1.1f, 0.1f));
            SceneParts.Part("Board", sign, Box(), Flat(WoodColor), new Vector3(0, 0.95f, -0.06f), null, new Vector3(0.8f, 0.36f, 0.05f));
            SceneParts.Part("BoardEdge", sign, Box(), Flat(DarkWoodColor), new Vector3(0, 1.14f, -0.06f), null, new Vector3(0.86f, 0.05f, 0.07f));
        }

        private void BuildRiverStones(Transform parent)
        {
            Mesh rock = _assets.GetMesh("Rock", () => ProceduralMeshes.Dodecahedron("Rock"));

            for (int i = 0; i < 34; i++)
            {
                float side = Random01() < 0.5f ? -1 : 1;
                float x = Range(-24, 24);
                if (Mathf.Abs(x) < 1.8f)
                {
                    continue;
                }

                var position = new Vector3(x, -0.3f, RiverShape.Center(x) + side * Range(0.63f, 0.87f) * RiverShape.HalfWidth(x));
                var scale = new Vector3(Range(0.1f, 0.24f), Range(0.07f, 0.14f), Range(0.1f, 0.2f));
                SceneParts.Part("RiverStone", parent, rock, Flat(StoneColor), position, SceneParts.Rotation(0, Range(0, Mathf.PI)), scale, false);
            }
        }

        private void BuildReeds(Transform parent)
        {
            Mesh reeds = _assets.GetMesh("Reeds", () =>
            {
                Mesh blade = ProceduralMeshes.Cone("ReedBlade", 0.03f, 0.75f, 4, true);
                return ProceduralMeshes.Combine(
                    "Reeds",
                    (blade, Matrix4x4.TRS(new Vector3(0, 0.37f, 0), Quaternion.identity, Vector3.one)),
                    (blade, Matrix4x4.TRS(new Vector3(0.08f, 0.3f, 0.03f), SceneParts.Rotation(0, 0, -0.18f), Vector3.one * 0.85f)),
                    (blade, Matrix4x4.TRS(new Vector3(-0.08f, 0.3f, -0.03f), SceneParts.Rotation(0, 0, 0.18f), Vector3.one * 0.8f)),
                    (blade, Matrix4x4.TRS(new Vector3(0.02f, 0.26f, 0.08f), SceneParts.Rotation(0.2f), Vector3.one * 0.7f)));
            });
            Mesh cattail = _assets.GetMesh("Cattail", () => ProceduralMeshes.Cylinder("Cattail", 0.03f, 0.03f, 0.16f, 6, true));

            for (int i = 0; i < 26; i++)
            {
                float side = Random01() < 0.5f ? -1 : 1;
                float x = Range(-22, 22);
                if (Mathf.Abs(x) < 2.6f)
                {
                    continue;
                }

                Transform clump = SceneParts.Group("Reeds", parent, new Vector3(x, -0.32f, RiverShape.Center(x) + side * Range(0.79f, 0.87f) * RiverShape.HalfWidth(x)));
                clump.localScale = Vector3.one * Range(0.8f, 1.25f);
                SceneParts.Part("Blades", clump, reeds, Flat(ReedColor));
                SceneParts.Part("Cattail", clump, cattail, Flat(CattailColor), new Vector3(0, 0.62f, 0), null, null, false);
            }
        }

        private void BuildLilyPads(Transform parent)
        {
            Mesh pad = _assets.GetMesh("LilyPad", () => ProceduralMeshes.Cylinder("LilyPad", 0.2f, 0.2f, 0.012f, 9, true));
            Mesh bloom = _assets.GetMesh("LilyBloom", () => ProceduralMeshes.Sphere("LilyBloom", 0.06f, 6, 4, true));

            for (int i = 0; i < 10; i++)
            {
                float x = Range(-16, 16);
                if (Mathf.Abs(x) < 2.2f)
                {
                    continue;
                }

                var position = new Vector3(x, -0.3f, RiverShape.Center(x) + Range(-0.63f, 0.63f) * RiverShape.HalfWidth(x));
                SceneParts.Part("LilyPad", parent, pad, Flat(LilyPadColor), position, null, Vector3.one * Range(0.8f, 1.4f), false);

                if (Random01() < 0.4f)
                {
                    SceneParts.Part("Bloom", parent, bloom, Flat(PetalColors[2]), position + Vector3.up * 0.035f, null, null, false);
                }
            }
        }
    }
}
