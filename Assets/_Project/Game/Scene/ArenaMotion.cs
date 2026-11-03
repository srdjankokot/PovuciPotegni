using UnityEngine;

namespace PovuciPotegni.Game
{
    /// <summary>Timing and motion curves from docs/SCENA.md, as pure functions. p always runs from 0 to 1.</summary>
    public static class ArenaMotion
    {
        public const float PullSeconds = 1.15f;
        public const float TieSeconds = 1f;
        public const float FallSeconds = 2f;
        public const float SplashAt = 0.62f;
        public const float DustIntervalSeconds = 0.09f;
        public const float DustStart = 0.2f;
        public const float DustEnd = 0.85f;
        public const float IdleWave = 0.012f;

        /// <summary>Resting height of a character's root in the water; lower would hide the short figure.</summary>
        public const float SwimHeight = -0.8f;

        private const float Anticipation = 0.18f;
        private const float AnticipationDistance = 0.12f;
        private const float TeeterShare = 0.3f;

        public static float EaseOutBack(float t)
        {
            const float c1 = 1.4f;
            const float c3 = c1 + 1;
            float p = t - 1;
            return 1 + c3 * p * p * p + c1 * p * p;
        }

        /// <summary>Rope offset during a pull: a small give the wrong way, then the yank with a slight overshoot.</summary>
        public static float PullOffset(float p, float from, float to)
        {
            float direction = Mathf.Sign(to - from);
            if (p < Anticipation)
            {
                return from - direction * AnticipationDistance * Mathf.Sin(p / Anticipation * Mathf.PI / 2);
            }

            float q = (p - Anticipation) / (1 - Anticipation);
            return from - direction * AnticipationDistance * (1 - Mathf.Min(1, q * 3)) + (to - from) * EaseOutBack(q);
        }

        public static float TieOffset(float p, float from)
        {
            return from + Mathf.Sin(p * Mathf.PI * 4) * 0.13f * Mathf.Sin(p * Mathf.PI);
        }

        /// <summary>The rope never sits perfectly still.</summary>
        public static float IdleOffset(float restOffset, float time)
        {
            return restOffset + Mathf.Sin(time * 1.3f) * 0.035f + Mathf.Sin(time * 3.3f) * 0.015f;
        }

        public static float PullWave(float p)
        {
            return 0.09f * Mathf.Sin(p * Mathf.PI);
        }

        public static float TieWave(float p)
        {
            return 0.05f * Mathf.Sin(p * Mathf.PI);
        }

        public static float FovKick(float p)
        {
            return 4f * Mathf.Sin(p * Mathf.PI);
        }

        public static float CameraShake(float p, float time)
        {
            return Mathf.Sin(time * 60f) * 0.05f * Mathf.Sin(p * Mathf.PI);
        }

        public static bool IsTeetering(float p)
        {
            return p < TeeterShare;
        }

        /// <summary>Distance moved toward the water during a fall.</summary>
        public static float FallForward(float p)
        {
            if (IsTeetering(p))
            {
                return 0.35f * p / TeeterShare;
            }

            return 0.35f + Mathf.Min(1, DiveProgress(p) * 1.4f);
        }

        public static float FallHeight(float p)
        {
            if (IsTeetering(p))
            {
                return 0;
            }

            float q = DiveProgress(p);
            return 0.45f * Mathf.Sin(Mathf.PI * Mathf.Min(q * 1.6f, 1)) + SwimHeight * Mathf.Min(1, q * q * 1.6f);
        }

        /// <summary>Forward tilt of the whole figure during a fall, in radians.</summary>
        public static float FallTilt(float p)
        {
            if (IsTeetering(p))
            {
                return 0.15f + Mathf.Sin(p / TeeterShare * Mathf.PI * 3) * 0.12f;
            }

            float q = DiveProgress(p);
            return 0.3f + 1.1f * Mathf.Sin(Mathf.Min(q, 1) * Mathf.PI) * (1 - q * 0.6f);
        }

        private static float DiveProgress(float p)
        {
            return (p - TeeterShare) / (1 - TeeterShare);
        }
    }
}
