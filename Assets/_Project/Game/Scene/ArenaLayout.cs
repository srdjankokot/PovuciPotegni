using UnityEngine;

namespace PovuciPotegni.Game
{
    /// <summary>
    /// Positions from docs/SCENA.md. The prototype looks along -Z and Unity along +Z, so every world Z here
    /// is the prototype's Z negated: the player stands on negative Z, the opponent on positive Z.
    /// </summary>
    public static class ArenaLayout
    {
        public const float StepMeters = 1.2f;
        public const float PlayerBaseDistance = 5f;
        public const float BaseCameraFollowDistance = 3.2f;
        public const float PortraitFov = 52f;
        public const float WideFov = 42f;
        public const float WideAspectThreshold = 0.7f;

        /// <summary>The menu's view of both characters, framed above the bottom sheet (reference/slike/home.png).</summary>
        public static readonly Vector3 MenuCameraPosition = new Vector3(-4.2f, 11f, -13.5f);
        public static readonly Vector3 MenuCameraTarget = new Vector3(0.3f, -3.6f, -1.2f);
        public const float MenuFov = 56f;

        /// <summary>Rope offset in meters for a rope position k.</summary>
        public static float RopeOffset(float k)
        {
            return k * StepMeters;
        }

        public static float PlayerZ(float ropeOffset)
        {
            return -(PlayerBaseDistance + ropeOffset);
        }

        public static float OpponentZ(float ropeOffset)
        {
            return PlayerBaseDistance - ropeOffset;
        }

        public static Vector3 CameraPosition(float playerZ)
        {
            return new Vector3(0.75f, 3.1f, -(FollowDistance(playerZ) + 6.2f));
        }

        public static Vector3 CameraTarget(float playerZ)
        {
            return new Vector3(0.15f, 1f, -(FollowDistance(playerZ) - 7.5f));
        }

        public static float CameraFov(float aspect)
        {
            return aspect > WideAspectThreshold ? WideFov : PortraitFov;
        }

        private static float FollowDistance(float playerZ)
        {
            return Mathf.Max(-playerZ, BaseCameraFollowDistance);
        }
    }
}
