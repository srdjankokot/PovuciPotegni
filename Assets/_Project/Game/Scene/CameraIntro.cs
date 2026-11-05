using System.Collections;
using UnityEngine;

namespace PovuciPotegni.Game
{
    public sealed class CameraIntro : MonoBehaviour
    {
        private const float MaxFrameStep = 1f / 30f;

        [SerializeField] private Transform startPose;
        [SerializeField, Min(0f)] private float startDelay = 1.5f;
        [SerializeField, Min(0f)] private float duration = 1.5f;

        private IEnumerator Start()
        {
            if (startPose == null || duration <= 0f)
            {
                yield break;
            }

            Vector3 endPosition = transform.position;
            Quaternion endRotation = transform.rotation;
            Vector3 startPosition = startPose.position;
            Quaternion startRotation = startPose.rotation;
            transform.SetPositionAndRotation(startPosition, startRotation);

            if (startDelay > 0f)
            {
                yield return new WaitForSecondsRealtime(startDelay);
            }

            float elapsed = 0f;
            while (elapsed < duration)
            {
                yield return null;

                // Keep a startup hitch from skipping the visible transition.
                elapsed += Mathf.Min(Time.unscaledDeltaTime, MaxFrameStep);
                float progress = Mathf.Clamp01(elapsed / duration);
                // Smootherstep brings both speed and acceleration to zero at each end.
                float blend = progress * progress * progress * (progress * (6f * progress - 15f) + 10f);
                transform.SetPositionAndRotation(
                    Vector3.Lerp(startPosition, endPosition, blend),
                    Quaternion.Slerp(startRotation, endRotation, blend));
            }

            transform.SetPositionAndRotation(endPosition, endRotation);
        }
    }
}
