using System;
using System.Collections.Generic;
using UnityEngine;

namespace PovuciPotegni.Game
{
    /// <summary>
    /// The river's course. Around the path it is straight and exactly as wide as docs/SCENA.md says, because the
    /// falling character and the path depend on it. Past the path it bends and its width and banks vary.
    /// </summary>
    public static class RiverShape
    {
        public const float BaseHalfWidth = 1.5f;
        public const float BankRim = 0.17f;

        private const float StraightUntil = 1.75f;
        private const float BendFullAt = 4.5f;
        private const float ExtentX = 40f;
        private const float FineStep = 0.4f;
        private const float CoarseStep = 1.6f;
        private const float FineUntil = 9f;
        private const float BankTop = 0.008f;
        private const float BankFoot = -0.4f;
        private const float BankFootInset = 0.04f;

        /// <summary>World Z of the middle of the river at x.</summary>
        public static float Center(float x)
        {
            float bend = Bend(x);
            return bend * (Mathf.Sign(x) * 1.25f + 0.55f * Mathf.Sin(x * 0.33f + 0.6f));
        }

        public static float HalfWidth(float x)
        {
            return BaseHalfWidth + Bend(x) * (0.22f * Mathf.Sin(x * 0.41f + 1f) + 0.1f * Mathf.Sin(x * 1.1f));
        }

        /// <summary>World Z of the bank on one side (-1 near the player, +1 near the opponent), with its own unevenness.</summary>
        public static float BankZ(float x, float side)
        {
            float unevenness = Bend(x) * 0.1f * Mathf.Sin(x * 2.3f + side * 1.7f);
            return Center(x) + side * (HalfWidth(x) + unevenness);
        }

        /// <summary>Distance from a point to the nearest bank; negative when the point is over the river.</summary>
        public static float DistanceToRiver(float x, float z)
        {
            return Mathf.Abs(z - Center(x)) - HalfWidth(x);
        }

        /// <summary>Direction of the river at x, as a rotation around Y that turns +X along the flow.</summary>
        public static Quaternion FlowRotation(float x)
        {
            const float delta = 0.1f;
            float slope = (Center(x + delta) - Center(x - delta)) / (2 * delta);
            return Quaternion.AngleAxis(-Mathf.Atan(slope) * Mathf.Rad2Deg, Vector3.up);
        }

        /// <summary>A flat strip along the river, between two curves of world Z.</summary>
        public static Mesh Strip(string name, Func<float, float> lowerZ, Func<float, float> upperZ)
        {
            List<float> samples = Samples();
            var vertices = new List<Vector3>(samples.Count * 2);
            var normals = new List<Vector3>(samples.Count * 2);
            var triangles = new List<int>((samples.Count - 1) * 6);

            foreach (float x in samples)
            {
                vertices.Add(new Vector3(x, 0, lowerZ(x)));
                vertices.Add(new Vector3(x, 0, upperZ(x)));
                normals.Add(Vector3.up);
                normals.Add(Vector3.up);
            }

            for (int i = 0; i < samples.Count - 1; i++)
            {
                int a = i * 2;
                triangles.AddRange(new[] { a, a + 1, a + 3, a, a + 3, a + 2 });
            }

            return ToMesh(name, vertices, normals, triangles);
        }

        /// <summary>The steep face of one bank, facing the water.</summary>
        public static Mesh BankWall(string name, float side)
        {
            List<float> samples = Samples();
            var vertices = new List<Vector3>(samples.Count * 2);
            var normals = new List<Vector3>(samples.Count * 2);
            var triangles = new List<int>((samples.Count - 1) * 6);
            Vector3 towardWater = side > 0 ? Vector3.back : Vector3.forward;

            foreach (float x in samples)
            {
                float z = BankZ(x, side) - side * BankRim;
                vertices.Add(new Vector3(x, BankTop, z));
                vertices.Add(new Vector3(x, BankFoot, z - side * BankFootInset));
                normals.Add(towardWater);
                normals.Add(towardWater);
            }

            for (int i = 0; i < samples.Count - 1; i++)
            {
                int top = i * 2;
                int foot = top + 1;
                int nextTop = top + 2;
                int nextFoot = top + 3;

                if (side < 0)
                {
                    triangles.AddRange(new[] { top, foot, nextTop, foot, nextFoot, nextTop });
                }
                else
                {
                    triangles.AddRange(new[] { top, nextTop, foot, foot, nextTop, nextFoot });
                }
            }

            return ToMesh(name, vertices, normals, triangles);
        }

        private static float Bend(float x)
        {
            return Mathf.SmoothStep(0, 1, Mathf.InverseLerp(StraightUntil, BendFullAt, Mathf.Abs(x)));
        }

        private static List<float> Samples()
        {
            var samples = new List<float>();
            for (float x = -ExtentX; x < ExtentX; x += Mathf.Abs(x) < FineUntil ? FineStep : CoarseStep)
            {
                samples.Add(x);
            }

            samples.Add(ExtentX);
            return samples;
        }

        private static Mesh ToMesh(string name, List<Vector3> vertices, List<Vector3> normals, List<int> triangles)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
