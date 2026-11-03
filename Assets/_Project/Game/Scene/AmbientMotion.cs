using UnityEngine;

namespace PovuciPotegni.Game
{
    /// <summary>Slides ripples along the water and clouds across the sky.</summary>
    public sealed class AmbientMotion : MonoBehaviour
    {
        private const float RippleLimit = 22f;
        private const float CloudLimit = 36f;
        private const float CloudSpeed = 0.35f;
        private const float RippleHeight = -0.3f;

        [SerializeField] private Transform[] ripples = new Transform[0];
        [SerializeField] private float[] rippleSpeeds = new float[0];
        [SerializeField] private float[] rippleShares = new float[0];
        [SerializeField] private Transform[] clouds = new Transform[0];

        /// <summary>Where a ripple floats: acrossShare is -1 to 1 between the banks, so it follows the river's bends.</summary>
        public static Vector3 RipplePosition(float x, float acrossShare)
        {
            return new Vector3(x, RippleHeight, RiverShape.Center(x) + acrossShare * RiverShape.HalfWidth(x));
        }

        public void Bind(Transform[] rippleParts, float[] speeds, float[] shares, Transform[] cloudParts)
        {
            ripples = rippleParts;
            rippleSpeeds = speeds;
            rippleShares = shares;
            clouds = cloudParts;
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            for (int i = 0; i < ripples.Length; i++)
            {
                Slide(ripples[i], rippleSpeeds[i] * deltaTime, RippleLimit);
                float x = ripples[i].localPosition.x;
                ripples[i].localPosition = RipplePosition(x, rippleShares[i]);
                ripples[i].localRotation = RiverShape.FlowRotation(x);
            }

            foreach (Transform cloud in clouds)
            {
                Slide(cloud, CloudSpeed * deltaTime, CloudLimit);
            }
        }

        private static void Slide(Transform part, float distance, float limit)
        {
            Vector3 position = part.localPosition;
            position.x += distance;
            if (position.x > limit)
            {
                position.x = -limit;
            }

            part.localPosition = position;
        }
    }
}
