using UnityEngine;

namespace PovuciPotegni.Game
{
    /// <summary>The rope as a tube mesh rebuilt each frame between both characters' hands, with the red knot in the middle.</summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public sealed class RopeView : MonoBehaviour
    {
        private const int TubeSegments = 160;
        private const int RadialSegments = 6;
        private const float TubeRadius = 0.055f;
        private const float MiddleSag = 0.12f;
        private const float TextureRepeats = 110f;
        private const int TautSamples = 6;
        private const float IdleWave = 0.012f;
        private const float BankClearHeight = 0.07f;
        private const float BodyPassSide = 0.55f;
        private const float BodyPassHeight = 0.62f;
        private const int TailPoints = 4;
        private const int TautEnd = TailPoints + TautSamples;

        [SerializeField] private CharacterRig player;
        [SerializeField] private CharacterRig opponent;
        [SerializeField] private Transform knot;

        private readonly Vector3[] _controlPoints = new Vector3[2 * TailPoints + 2 + TautSamples - 1];
        private readonly Vector3[] _centers = new Vector3[TubeSegments + 1];
        private readonly Vector3[] _vertices = new Vector3[(TubeSegments + 1) * (RadialSegments + 1)];
        private readonly Vector3[] _normals = new Vector3[(TubeSegments + 1) * (RadialSegments + 1)];
        private readonly Vector2[] _uvs = new Vector2[(TubeSegments + 1) * (RadialSegments + 1)];

        private Mesh _mesh;

        /// <summary>Height of the wave running along the taut part; phase 3 raises it during a pull.</summary>
        public float Wave { get; set; } = IdleWave;

        /// <summary>Points along the middle of the rope, updated with each rebuild.</summary>
        public System.Collections.Generic.IReadOnlyList<Vector3> Centerline => _centers;

        public void Bind(CharacterRig playerRig, CharacterRig opponentRig, Transform knotPart)
        {
            player = playerRig;
            opponent = opponentRig;
            knot = knotPart;
        }

        public void SetOpponent(CharacterRig opponentRig)
        {
            opponent = opponentRig;
        }

        public void Rebuild(float time)
        {
            if (player == null || opponent == null)
            {
                return;
            }

            EnsureMesh();
            FillControlPoints(time);
            FillCenters();
            FillTube();

            _mesh.SetVertices(_vertices);
            _mesh.SetNormals(_normals);
            _mesh.SetUVs(0, _uvs);
            _mesh.RecalculateBounds();

            if (knot != null)
            {
                knot.position = RopeCurve.Evaluate(_controlPoints, 0.5f);
                knot.rotation = SceneParts.Rotation(0, Mathf.Sin(time * 1.3f) * 0.4f, Mathf.Sin(time * 5f) * 0.15f);
            }
        }

        private void LateUpdate()
        {
            // In Play mode ArenaDirector rebuilds the rope, in step with everything else.
            if (!Application.isPlaying)
            {
                Rebuild(0f);
            }
        }

        private void OnDestroy()
        {
            if (_mesh == null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Destroy(_mesh);
            }
            else
            {
                DestroyImmediate(_mesh);
            }
        }

        private void EnsureMesh()
        {
            if (_mesh != null)
            {
                return;
            }

            _mesh = new Mesh { name = "Rope", hideFlags = HideFlags.DontSave };
            _mesh.MarkDynamic();
            _mesh.SetVertices(_vertices);
            _mesh.SetTriangles(CreateTriangles(), 0);
            GetComponent<MeshFilter>().sharedMesh = _mesh;
        }

        private void FillControlPoints(float time)
        {
            Vector3 a = player.HandsPoint;
            Vector3 b = opponent.HandsPoint;

            FillTail(player, a, 1);
            _controlPoints[TailPoints] = a;

            if (player.IsRopeInWater)
            {
                FillOverBank(b, a, TautEnd - 1, -1);
            }
            else if (opponent.IsRopeInWater)
            {
                FillOverBank(a, b, TailPoints + 1, 1);
            }
            else
            {
                for (int i = 1; i < TautSamples; i++)
                {
                    float f = i / (float)TautSamples;
                    Vector3 point = Vector3.Lerp(a, b, f);
                    float arch = Mathf.Sin(Mathf.PI * f);
                    point.y += -MiddleSag * arch + Wave * arch * Mathf.Sin(time * 34f - f * 9f);
                    _controlPoints[TailPoints + i] = point;
                }
            }

            _controlPoints[TautEnd] = b;
            FillTail(opponent, b, -1);
        }

        /// <summary>
        /// The loose end. A holder has it past the side of their body and along the ground behind them;
        /// once they have let go it lies off to the side. Either way it stays clear of the body and legs.
        /// </summary>
        private void FillTail(CharacterRig holder, Vector3 end, float side)
        {
            int direction = side > 0 ? 1 : -1;
            int farIndex = side > 0 ? 0 : _controlPoints.Length - 1;

            if (holder.IsRopeReleased)
            {
                _controlPoints[farIndex] = new Vector3(end.x + side * 1.9f, end.y, end.z - side * 0.6f);
                _controlPoints[farIndex + direction] = new Vector3(end.x + side * 1.1f, end.y, end.z - side * 0.25f);
                _controlPoints[farIndex + 2 * direction] = new Vector3(end.x + side * 0.5f, end.y, end.z - side * 0.05f);
                _controlPoints[farIndex + 3 * direction] = new Vector3(end.x + side * 0.2f, end.y, end.z);
            }
            else
            {
                Vector3 root = holder.transform.position;
                _controlPoints[farIndex] = new Vector3(root.x + side * 1.3f, 0.06f, root.z - side * 3.2f);
                _controlPoints[farIndex + direction] = new Vector3(root.x + side * 0.8f, 0.1f, root.z - side * 1.5f);
                _controlPoints[farIndex + 2 * direction] = new Vector3(root.x + side * BodyPassSide, BodyPassHeight, root.z - side * 0.1f);

                // First out to the side, still in front of the body, and only then back past it.
                _controlPoints[farIndex + 3 * direction] = new Vector3(
                    root.x + side * BodyPassSide,
                    (end.y + BodyPassHeight) * 0.5f,
                    end.z);
            }
        }

        /// <summary>From the land end across the ground, over the bank's edge and down onto the water.</summary>
        private void FillOverBank(Vector3 land, Vector3 water, int first, int step)
        {
            float bankSide = land.z >= water.z ? 1 : -1;
            float rimZ = RiverShape.BankZ((land.x + water.x) * 0.5f, bankSide) - bankSide * RiverShape.BankRim;

            for (int i = 0; i < 3; i++)
            {
                float f = (i + 1) * 0.28f;
                _controlPoints[first + i * step] = new Vector3(
                    Mathf.Lerp(land.x, water.x, f * 0.7f),
                    Mathf.Lerp(land.y, BankClearHeight, f),
                    Mathf.Lerp(land.z, rimZ, f));
            }

            _controlPoints[first + 3 * step] = new Vector3(Mathf.Lerp(land.x, water.x, 0.7f), BankClearHeight, rimZ);
            _controlPoints[first + 4 * step] = new Vector3(
                Mathf.Lerp(land.x, water.x, 0.85f),
                Mathf.Lerp(BankClearHeight, water.y, 0.8f),
                rimZ - bankSide * 0.35f);
        }

        private void FillCenters()
        {
            for (int i = 0; i <= TubeSegments; i++)
            {
                _centers[i] = RopeCurve.Evaluate(_controlPoints, i / (float)TubeSegments);
            }
        }

        private void FillTube()
        {
            Vector3 tangent = (_centers[1] - _centers[0]).normalized;
            Vector3 normal = Vector3.Cross(tangent, Vector3.right).normalized;
            float totalLength = 0;
            float travelled = 0;

            for (int i = 0; i < TubeSegments; i++)
            {
                totalLength += Vector3.Distance(_centers[i], _centers[i + 1]);
            }

            for (int i = 0; i <= TubeSegments; i++)
            {
                if (i > 0)
                {
                    travelled += Vector3.Distance(_centers[i - 1], _centers[i]);
                    Vector3 next = (_centers[Mathf.Min(i + 1, TubeSegments)] - _centers[i - 1]).normalized;
                    normal = (Quaternion.FromToRotation(tangent, next) * normal).normalized;
                    tangent = next;
                }

                Vector3 binormal = Vector3.Cross(tangent, normal);
                float u = travelled / totalLength * TextureRepeats;

                for (int j = 0; j <= RadialSegments; j++)
                {
                    float angle = j / (float)RadialSegments * Mathf.PI * 2f;
                    Vector3 direction = Mathf.Cos(angle) * normal + Mathf.Sin(angle) * binormal;
                    int vertex = i * (RadialSegments + 1) + j;

                    _vertices[vertex] = _centers[i] + direction * TubeRadius;
                    _normals[vertex] = direction;
                    _uvs[vertex] = new Vector2(u, j / (float)RadialSegments);
                }
            }
        }

        private static int[] CreateTriangles()
        {
            var triangles = new int[TubeSegments * RadialSegments * 6];
            int stride = RadialSegments + 1;
            int index = 0;

            for (int i = 0; i < TubeSegments; i++)
            {
                for (int j = 0; j < RadialSegments; j++)
                {
                    int a = i * stride + j;
                    int b = a + stride;
                    int c = a + 1;
                    int d = b + 1;

                    triangles[index++] = a;
                    triangles[index++] = c;
                    triangles[index++] = b;
                    triangles[index++] = c;
                    triangles[index++] = d;
                    triangles[index++] = b;
                }
            }

            return triangles;
        }
    }
}
