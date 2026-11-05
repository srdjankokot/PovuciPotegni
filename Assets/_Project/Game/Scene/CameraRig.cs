using UnityEngine;

namespace PovuciPotegni.Game
{
    /// <summary>
    /// Follows the player from behind the right shoulder, with shake and a zoom during a pull (docs/SCENA.md).
    /// In the menu it holds a wider view of both characters.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public sealed class CameraRig : MonoBehaviour
    {
        private const float PositionRate = 4f;
        private const float FovRate = 8f;

        [SerializeField] private Transform player;

        private Camera _camera;
        private bool _menuMode;
        private Vector3 _lookTarget;
        private bool _hasLookTarget;

        private Camera Camera => _camera != null ? _camera : _camera = GetComponent<Camera>();

        public void Bind(Transform playerRoot)
        {
            player = playerRoot;
        }

        public void SetMenuMode(bool menu)
        {
            _menuMode = menu;
        }

        /// <summary>Jumps to the current view at once.</summary>
        public void Snap()
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            transform.position = WantedPosition();
            _lookTarget = WantedTarget();
            _hasLookTarget = true;
            transform.LookAt(_lookTarget);
            Camera.fieldOfView = WantedFov();
        }

        public void Tick(float deltaTime, float shake, float fovKick)
        {
            if (!isActiveAndEnabled)
            {
                return;
            }

            if (!_hasLookTarget)
            {
                _lookTarget = WantedTarget();
                _hasLookTarget = true;
            }

            Vector3 target = WantedPosition() + new Vector3(shake, shake * 0.5f, 0);
            float blend = 1f - Mathf.Exp(-deltaTime * PositionRate);

            transform.position = Vector3.Lerp(transform.position, target, blend);
            _lookTarget = Vector3.Lerp(_lookTarget, WantedTarget(), blend);
            transform.LookAt(_lookTarget);

            Camera.fieldOfView = Mathf.Lerp(Camera.fieldOfView, WantedFov() - fovKick, 1f - Mathf.Exp(-deltaTime * FovRate));
        }

        private Vector3 WantedPosition()
        {
            return _menuMode ? ArenaLayout.MenuCameraPosition : ArenaLayout.CameraPosition(player.position.z);
        }

        private Vector3 WantedTarget()
        {
            return _menuMode ? ArenaLayout.MenuCameraTarget : ArenaLayout.CameraTarget(player.position.z);
        }

        private float WantedFov()
        {
            return _menuMode ? ArenaLayout.MenuFov : ArenaLayout.CameraFov(Camera.aspect);
        }
    }
}
