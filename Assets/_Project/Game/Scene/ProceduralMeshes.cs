using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace PovuciPotegni.Game
{
    /// <summary>Low-poly shapes with the same parameters and vertex layout as the three.js geometries in the prototype.</summary>
    public static class ProceduralMeshes
    {
        private const float FullCircle = Mathf.PI * 2f;

        public static Mesh Box(string name, bool flat = true)
        {
            var data = new MeshData();
            Vector3[] normals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };

            foreach (Vector3 normal in normals)
            {
                Vector3 tangent = Mathf.Abs(normal.y) > 0.5f ? Vector3.right : Vector3.up;
                Vector3 bitangent = Vector3.Cross(normal, tangent);
                int start = data.Count;

                data.Add((normal + tangent + bitangent) * 0.5f, normal, new Vector2(1, 1));
                data.Add((normal + tangent - bitangent) * 0.5f, normal, new Vector2(1, 0));
                data.Add((normal - tangent - bitangent) * 0.5f, normal, new Vector2(0, 0));
                data.Add((normal - tangent + bitangent) * 0.5f, normal, new Vector2(0, 1));
                data.Quad(start, start + 1, start + 2, start + 3);
            }

            return data.Build(name, flat);
        }

        /// <summary>A 1 x 1 plane on XZ, facing up.</summary>
        public static Mesh Plane(string name)
        {
            var data = new MeshData();
            data.Add(new Vector3(-0.5f, 0, -0.5f), Vector3.up, new Vector2(0, 0));
            data.Add(new Vector3(-0.5f, 0, 0.5f), Vector3.up, new Vector2(0, 1));
            data.Add(new Vector3(0.5f, 0, 0.5f), Vector3.up, new Vector2(1, 1));
            data.Add(new Vector3(0.5f, 0, -0.5f), Vector3.up, new Vector2(1, 0));
            data.Quad(0, 1, 2, 3);
            return data.Build(name, false);
        }

        public static Mesh Sphere(string name, float radius, int widthSegments, int heightSegments, bool flat, float thetaLength = Mathf.PI)
        {
            var data = new MeshData();

            for (int y = 0; y <= heightSegments; y++)
            {
                float theta = y / (float)heightSegments * thetaLength;
                for (int x = 0; x <= widthSegments; x++)
                {
                    float phi = x / (float)widthSegments * FullCircle;
                    var normal = new Vector3(
                        -Mathf.Cos(phi) * Mathf.Sin(theta),
                        Mathf.Cos(theta),
                        Mathf.Sin(phi) * Mathf.Sin(theta));
                    data.Add(normal * radius, normal, new Vector2(x / (float)widthSegments, 1 - y / (float)heightSegments));
                }
            }

            int stride = widthSegments + 1;
            for (int y = 0; y < heightSegments; y++)
            {
                for (int x = 0; x < widthSegments; x++)
                {
                    int a = y * stride + x;
                    data.Quad(a, a + 1, a + stride + 1, a + stride);
                }
            }

            return data.Build(name, flat);
        }

        public static Mesh Cylinder(
            string name,
            float radiusTop,
            float radiusBottom,
            float height,
            int segments,
            bool flat,
            float thetaLength = FullCircle)
        {
            var data = new MeshData();
            float halfHeight = height * 0.5f;
            float slope = (radiusBottom - radiusTop) / height;

            for (int x = 0; x <= segments; x++)
            {
                float u = x / (float)segments;
                float theta = u * thetaLength;
                float sin = Mathf.Sin(theta);
                float cos = Mathf.Cos(theta);
                Vector3 normal = new Vector3(sin, slope, cos).normalized;

                data.Add(new Vector3(radiusTop * sin, halfHeight, radiusTop * cos), normal, new Vector2(u, 1));
                data.Add(new Vector3(radiusBottom * sin, -halfHeight, radiusBottom * cos), normal, new Vector2(u, 0));
            }

            for (int x = 0; x < segments; x++)
            {
                int a = x * 2;
                data.Quad(a, a + 2, a + 3, a + 1);
            }

            if (radiusTop > 0)
            {
                AddCap(data, radiusTop, halfHeight, Vector3.up, segments, thetaLength);
            }

            if (radiusBottom > 0)
            {
                AddCap(data, radiusBottom, -halfHeight, Vector3.down, segments, thetaLength);
            }

            return data.Build(name, flat);
        }

        public static Mesh Cone(string name, float radius, float height, int segments, bool flat)
        {
            return Cylinder(name, 0, radius, height, segments, flat);
        }

        public static Mesh Torus(string name, float radius, float tube, int radialSegments, int tubularSegments, bool flat, float arc = FullCircle)
        {
            var data = new MeshData();

            for (int j = 0; j <= radialSegments; j++)
            {
                float v = j / (float)radialSegments * FullCircle;
                for (int i = 0; i <= tubularSegments; i++)
                {
                    float u = i / (float)tubularSegments * arc;
                    var center = new Vector3(radius * Mathf.Cos(u), radius * Mathf.Sin(u), 0);
                    var position = new Vector3(
                        (radius + tube * Mathf.Cos(v)) * Mathf.Cos(u),
                        (radius + tube * Mathf.Cos(v)) * Mathf.Sin(u),
                        tube * Mathf.Sin(v));
                    data.Add(position, (position - center).normalized, new Vector2(i / (float)tubularSegments, j / (float)radialSegments));
                }
            }

            int stride = tubularSegments + 1;
            for (int j = 0; j < radialSegments; j++)
            {
                for (int i = 0; i < tubularSegments; i++)
                {
                    int a = j * stride + i;
                    data.Quad(a, a + 1, a + stride + 1, a + stride);
                }
            }

            return data.Build(name, flat);
        }

        /// <summary>A flat-shaded dodecahedron of radius 1, built from the face directions of its dual icosahedron.</summary>
        public static Mesh Dodecahedron(string name)
        {
            float golden = (1 + Mathf.Sqrt(5)) / 2;
            float inverse = 1 / golden;

            var corners = new List<Vector3>();
            foreach (float a in new[] { -1f, 1f })
            {
                foreach (float b in new[] { -1f, 1f })
                {
                    corners.Add(new Vector3(0, a * inverse, b * golden).normalized);
                    corners.Add(new Vector3(a * inverse, b * golden, 0).normalized);
                    corners.Add(new Vector3(a * golden, 0, b * inverse).normalized);
                    foreach (float c in new[] { -1f, 1f })
                    {
                        corners.Add(new Vector3(a, b, c).normalized);
                    }
                }
            }

            var faces = new List<Vector3>();
            foreach (float a in new[] { -1f, 1f })
            {
                foreach (float b in new[] { -1f, 1f })
                {
                    faces.Add(new Vector3(0, a, b * golden).normalized);
                    faces.Add(new Vector3(a, b * golden, 0).normalized);
                    faces.Add(new Vector3(a * golden, 0, b).normalized);
                }
            }

            var data = new MeshData();
            foreach (Vector3 face in faces)
            {
                Vector3 tangent = Vector3.Cross(face, Mathf.Abs(face.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
                Vector3 bitangent = Vector3.Cross(face, tangent);

                List<Vector3> ring = corners
                    .OrderByDescending(corner => Vector3.Dot(corner, face))
                    .Take(5)
                    .OrderBy(corner => Mathf.Atan2(Vector3.Dot(corner, bitangent), Vector3.Dot(corner, tangent)))
                    .ToList();

                int start = data.Count;
                foreach (Vector3 corner in ring)
                {
                    data.Add(corner, face, Vector2.zero);
                }

                for (int i = 1; i < 4; i++)
                {
                    data.Triangle(start, start + i, start + i + 1);
                }
            }

            return data.Build(name, true);
        }

        /// <summary>Merges shapes into one mesh, so a small decoration costs one object.</summary>
        public static Mesh Combine(string name, params (Mesh Mesh, Matrix4x4 Transform)[] parts)
        {
            var instances = new CombineInstance[parts.Length];
            for (int i = 0; i < parts.Length; i++)
            {
                instances[i] = new CombineInstance { mesh = parts[i].Mesh, transform = parts[i].Transform };
            }

            var combined = new Mesh { name = name };
            combined.CombineMeshes(instances, true, true);
            combined.RecalculateBounds();
            return combined;
        }

        private static void AddCap(MeshData data, float radius, float y, Vector3 normal, int segments, float thetaLength)
        {
            int center = data.Count;
            data.Add(new Vector3(0, y, 0), normal, new Vector2(0.5f, 0.5f));

            for (int x = 0; x <= segments; x++)
            {
                float theta = x / (float)segments * thetaLength;
                float sin = Mathf.Sin(theta);
                float cos = Mathf.Cos(theta);
                data.Add(new Vector3(radius * sin, y, radius * cos), normal, new Vector2(cos * 0.5f + 0.5f, sin * 0.5f + 0.5f));
            }

            for (int x = 0; x < segments; x++)
            {
                data.Triangle(center, center + x + 1, center + x + 2);
            }
        }

        private sealed class MeshData
        {
            private readonly List<Vector3> _vertices = new List<Vector3>();
            private readonly List<Vector3> _normals = new List<Vector3>();
            private readonly List<Vector2> _uvs = new List<Vector2>();
            private readonly List<int> _triangles = new List<int>();

            public int Count => _vertices.Count;

            public void Add(Vector3 position, Vector3 normal, Vector2 uv)
            {
                _vertices.Add(position);
                _normals.Add(normal);
                _uvs.Add(uv);
            }

            public void Quad(int a, int b, int c, int d)
            {
                Triangle(a, b, d);
                Triangle(b, c, d);
            }

            /// <summary>Adds a triangle facing along its vertex normals; degenerate ones at poles are dropped.</summary>
            public void Triangle(int a, int b, int c)
            {
                Vector3 cross = Vector3.Cross(_vertices[b] - _vertices[a], _vertices[c] - _vertices[a]);
                if (cross.sqrMagnitude < 1e-12f)
                {
                    return;
                }

                bool facesAlongNormals = Vector3.Dot(cross, _normals[a] + _normals[b] + _normals[c]) > 0;
                _triangles.Add(a);
                _triangles.Add(facesAlongNormals ? b : c);
                _triangles.Add(facesAlongNormals ? c : b);
            }

            public Mesh Build(string name, bool flat)
            {
                var mesh = new Mesh { name = name };
                if (flat)
                {
                    SplitIntoFlatFaces(mesh);
                }
                else
                {
                    mesh.SetVertices(_vertices);
                    mesh.SetNormals(_normals);
                    mesh.SetUVs(0, _uvs);
                    mesh.SetTriangles(_triangles, 0);
                }

                mesh.RecalculateBounds();
                return mesh;
            }

            private void SplitIntoFlatFaces(Mesh mesh)
            {
                var vertices = new List<Vector3>(_triangles.Count);
                var normals = new List<Vector3>(_triangles.Count);
                var uvs = new List<Vector2>(_triangles.Count);
                var triangles = new List<int>(_triangles.Count);

                for (int i = 0; i < _triangles.Count; i += 3)
                {
                    int a = _triangles[i];
                    int b = _triangles[i + 1];
                    int c = _triangles[i + 2];
                    Vector3 normal = Vector3.Cross(_vertices[b] - _vertices[a], _vertices[c] - _vertices[a]).normalized;

                    foreach (int index in new[] { a, b, c })
                    {
                        triangles.Add(vertices.Count);
                        vertices.Add(_vertices[index]);
                        normals.Add(normal);
                        uvs.Add(_uvs[index]);
                    }
                }

                mesh.SetVertices(vertices);
                mesh.SetNormals(normals);
                mesh.SetUVs(0, uvs);
                mesh.SetTriangles(triangles, 0);
            }
        }
    }
}
