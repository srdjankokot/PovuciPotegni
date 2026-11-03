using UnityEngine;

namespace PovuciPotegni.Game
{
    /// <summary>
    /// Patrols the river, lurks under whoever is close to the water, and snaps at a splash (docs/SCENA.md).
    /// The body faces +X, so a yaw of zero swims toward +X.
    /// </summary>
    public sealed class CrocodileView : MonoBehaviour
    {
        private const float MoveRate = 1.1f;
        private const float TurnRate = 4f;
        private const float RestTurnRate = 2f;
        private const float JawRate = 10f;
        private const float WaterHeight = -0.3f;
        private const float MinimumMoveToTurn = 0.0015f;
        private const float PatrolDistance = 9f;
        private const int NearEdge = 2;
        private const float LurkX = 1.5f;
        private const float LurkZ = 0.65f;
        private const float SnapOffsetX = 1.4f;
        private const float SnapZ = 0.6f;
        private const float FaceBackYaw = 180f;

        [SerializeField] private Transform jaw;
        [SerializeField] private Transform head;
        [SerializeField] private Transform[] tailSegments = new Transform[0];

        private float _x;
        private float _z;
        private float _heading;
        private float _yaw;
        private float _jawOpen;
        private bool _hasFocus;
        private Vector2 _focus;

        public float JawOpen => _jawOpen;
        public bool HasFocus => _hasFocus;

        public void Bind(Transform jawJoint, Transform headJoint, Transform[] tail)
        {
            jaw = jawJoint;
            head = headJoint;
            tailSegments = tail;
        }

        /// <summary>Swim to a splash and snap at it.</summary>
        public void FocusOn(Vector3 splash)
        {
            _hasFocus = true;
            _focus = new Vector2(splash.x + SnapOffsetX, Mathf.Sign(splash.z) * SnapZ);
        }

        public void ClearFocus()
        {
            _hasFocus = false;
        }

        /// <summary>Where the crocodile wants to be for a rope position, when no splash has its attention.</summary>
        public static Vector2 PatrolTarget(float time, int k)
        {
            if (Mathf.Abs(k) >= NearEdge)
            {
                // k > 0 pulls the opponent toward the water, and the opponent stands on positive Z.
                return new Vector2(LurkX, k > 0 ? LurkZ : -LurkZ);
            }

            float x = Mathf.Sin(time * 0.22f) * PatrolDistance;
            float across = 0.15f + Mathf.Sin(time * 0.5f) * 0.35f;
            return new Vector2(x, RiverShape.Center(x) - across);
        }

        public void Tick(float deltaTime, float time, int k)
        {
            bool nearEdge = Mathf.Abs(k) >= NearEdge;
            Vector2 target = _hasFocus ? _focus : PatrolTarget(time, k);

            float previousX = _x;
            float previousZ = _z;
            float moveBlend = 1 - Mathf.Exp(-MoveRate * deltaTime);
            _x = Mathf.Lerp(_x, target.x, moveBlend);
            _z = Mathf.Lerp(_z, target.y, moveBlend);

            float moveX = _x - previousX;
            float moveZ = _z - previousZ;
            if (new Vector2(moveX, moveZ).magnitude > MinimumMoveToTurn)
            {
                _heading = Mathf.Atan2(-moveZ, moveX) * Mathf.Rad2Deg;
            }
            else if (_hasFocus || nearEdge)
            {
                _heading = Mathf.LerpAngle(_heading, FaceBackYaw, 1 - Mathf.Exp(-RestTurnRate * deltaTime));
            }

            _yaw = Mathf.LerpAngle(_yaw, _heading, 1 - Mathf.Exp(-TurnRate * deltaTime));

            transform.SetPositionAndRotation(
                new Vector3(_x, WaterHeight + Mathf.Sin(time * 1.7f) * 0.03f, _z),
                Quaternion.Euler(0, _yaw, 0));

            float wantedJaw = JawTarget(time, nearEdge);
            _jawOpen = Mathf.Lerp(_jawOpen, wantedJaw, 1 - Mathf.Exp(-JawRate * deltaTime));
            jaw.localRotation = SceneParts.Rotation(0, 0, -_jawOpen);
            head.localRotation = SceneParts.Rotation(0, 0, _jawOpen * 0.35f);

            for (int i = 0; i < tailSegments.Length; i++)
            {
                tailSegments[i].localRotation = SceneParts.Rotation(0, Mathf.Sin(time * 3f - i * 0.8f) * 0.18f);
            }
        }

        private float JawTarget(float time, bool nearEdge)
        {
            if (_hasFocus)
            {
                return Mathf.Max(0, Mathf.Sin(time * 7f)) * 0.7f;
            }

            if (nearEdge)
            {
                return 0.25f + Mathf.Max(0, Mathf.Sin(time * 3f)) * 0.45f;
            }

            return 0.05f + Mathf.Max(0, Mathf.Sin(time * 0.9f)) * 0.1f;
        }
    }
}
