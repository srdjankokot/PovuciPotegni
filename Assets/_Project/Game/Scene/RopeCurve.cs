using System.Collections.Generic;
using UnityEngine;

namespace PovuciPotegni.Game
{
    /// <summary>Centripetal Catmull-Rom curve, with the same formulas as three.js CatmullRomCurve3.</summary>
    public static class RopeCurve
    {
        private const float MinimumSpan = 1e-4f;

        public static Vector3 Evaluate(IReadOnlyList<Vector3> points, float t)
        {
            int count = points.Count;
            float scaled = (count - 1) * t;
            int index = Mathf.FloorToInt(scaled);
            float weight = scaled - index;

            if (index >= count - 1)
            {
                index = count - 2;
                weight = 1;
            }

            Vector3 p0 = index > 0 ? points[index - 1] : 2 * points[0] - points[1];
            Vector3 p1 = points[index];
            Vector3 p2 = points[index + 1];
            Vector3 p3 = index + 2 < count ? points[index + 2] : 2 * points[count - 1] - points[count - 2];

            float span0 = Mathf.Pow((p1 - p0).sqrMagnitude, 0.25f);
            float span1 = Mathf.Pow((p2 - p1).sqrMagnitude, 0.25f);
            float span2 = Mathf.Pow((p3 - p2).sqrMagnitude, 0.25f);

            if (span1 < MinimumSpan)
            {
                span1 = 1;
            }

            if (span0 < MinimumSpan)
            {
                span0 = span1;
            }

            if (span2 < MinimumSpan)
            {
                span2 = span1;
            }

            return new Vector3(
                Component(p0.x, p1.x, p2.x, p3.x, span0, span1, span2, weight),
                Component(p0.y, p1.y, p2.y, p3.y, span0, span1, span2, weight),
                Component(p0.z, p1.z, p2.z, p3.z, span0, span1, span2, weight));
        }

        private static float Component(float x0, float x1, float x2, float x3, float span0, float span1, float span2, float weight)
        {
            float tangent1 = ((x1 - x0) / span0 - (x2 - x0) / (span0 + span1) + (x2 - x1) / span1) * span1;
            float tangent2 = ((x2 - x1) / span1 - (x3 - x1) / (span1 + span2) + (x3 - x2) / span2) * span1;

            float c2 = -3 * x1 + 3 * x2 - 2 * tangent1 - tangent2;
            float c3 = 2 * x1 - 2 * x2 + tangent1 + tangent2;
            return x1 + weight * (tangent1 + weight * (c2 + weight * c3));
        }
    }
}
