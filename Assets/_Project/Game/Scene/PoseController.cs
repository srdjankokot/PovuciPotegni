using UnityEngine;

namespace PovuciPotegni.Game
{
    /// <summary>
    /// Procedural poses from docs/SCENA.md. Angles match the prototype; hip heights and crouch depth
    /// are scaled to the shorter legs of the current characters.
    /// </summary>
    [RequireComponent(typeof(CharacterRig))]
    public sealed class PoseController : MonoBehaviour, ICharacterView
    {
        private enum State
        {
            Idle,
            Pull,
            Falling,
            Swimming,
            Celebrating,
        }

        private const float BaseLean = -0.42f;
        private const float CrouchScale = 0.63f;
        private const float RopeDropDistance = 0.7f;
        private const float RopeDropHeight = 0.05f;
        private const float RopeDropSide = 0.15f;
        private const float RopeFloatHeight = -0.29f;
        private const float RopeFloatSide = 0.85f;
        private const float RopeLetGoStart = 0.3f;
        private const float RopeLetGoEnd = 0.62f;

        [SerializeField] private CharacterRig rig;
        [SerializeField] private float phase;
        [SerializeField] private float facingYaw;

        private State _state;
        private PullRole _role;
        private float _progress;
        private float _fallForward;
        private float _fallHeight;
        private float _fallTilt;
        private float _jump;

        public Vector3 HandsPoint => rig.HandsPoint;

        public void Bind(CharacterRig characterRig, float motionPhase, float yawDegrees)
        {
            rig = characterRig;
            phase = motionPhase;
            facingYaw = yawDegrees;
        }

        /// <summary>Which way the figure faces, in degrees around Y. It always faces the river.</summary>
        public void SetFacing(float yawDegrees)
        {
            facingYaw = yawDegrees;
        }

        public void SetMood(CharacterMood mood)
        {
            rig.SetMood(mood);
        }

        public void ShowIdle()
        {
            _state = State.Idle;
            _fallForward = 0;
            _fallHeight = 0;
            _fallTilt = 0;
            _jump = 0;
            rig.ClearRopeOverride();
        }

        public void ShowPull(PullRole role, float progress)
        {
            _state = State.Pull;
            _role = role;
            _progress = progress;
        }

        public void ShowFall(float progress)
        {
            _state = State.Falling;
            _progress = progress;
            _fallForward = ArenaMotion.FallForward(progress);
            _fallHeight = ArenaMotion.FallHeight(progress);
            _fallTilt = ArenaMotion.FallTilt(progress);
        }

        public void ShowSwimming()
        {
            _state = State.Swimming;
            _fallForward = ArenaMotion.FallForward(1);
            _fallHeight = ArenaMotion.SwimHeight;
            _fallTilt = 0;
        }

        public void ShowCelebration()
        {
            _state = State.Celebrating;
        }

        public void UpdateView(float baseZ, float time)
        {
            switch (_state)
            {
                case State.Swimming:
                    ApplySwimming(time);
                    break;
                case State.Celebrating:
                    ApplyCelebration(time);
                    break;
                case State.Falling:
                    ApplyFall(time);
                    break;
                default:
                    ApplyBrace(time);
                    break;
            }

            Place(baseZ, time);
        }

        private void Place(float baseZ, float time)
        {
            Quaternion facing = Quaternion.Euler(0, facingYaw, 0);
            float bob = _state == State.Swimming ? Mathf.Sin(time * 3f + phase) * 0.05f : 0;

            Vector3 position = new Vector3(0, _fallHeight + _jump + bob, baseZ) + facing * Vector3.forward * _fallForward;
            transform.SetPositionAndRotation(position, facing * SceneParts.Rotation(_fallTilt));

            float sideOfRope = facingYaw < 90 ? 1 : -1;

            if (_state == State.Celebrating)
            {
                // A celebrating winner lets go, and the rope drops to the ground in front of them.
                Vector3 drop = new Vector3(0, RopeDropHeight, baseZ) + facing * Vector3.forward * RopeDropDistance;
                drop.x = sideOfRope * RopeDropSide;
                rig.SetRopeOverride(drop);
            }
            else if (_state == State.Swimming || (_state == State.Falling && _progress > RopeLetGoStart))
            {
                // Whoever falls in lets go, and the rope floats beside them instead of running through them.
                var floating = new Vector3(sideOfRope * RopeFloatSide, RopeFloatHeight, position.z);
                float letGo = _state == State.Swimming
                    ? 1
                    : Mathf.SmoothStep(0, 1, Mathf.InverseLerp(RopeLetGoStart, RopeLetGoEnd, _progress));
                rig.SetRopeOverride(Vector3.Lerp(rig.RealHandsPoint, floating, letGo), true);
            }
        }

        private void ApplyBrace(float time)
        {
            float lean = BaseLean + Mathf.Sin(time * 2.2f + phase) * 0.025f;
            float crouch = 0;
            float armA = 0;
            float armB = 0;
            float armReach = 0;
            float legCycle = 0;
            float legAmplitude = 0;
            float headTilt = 0;

            // Leaning back pulls the short arms into the chest, so a hauling figure straightens them to compensate.
            float armExtend = 0;

            if (_state == State.Pull)
            {
                float p = _progress;
                float s = Mathf.Sin(p * Mathf.PI);

                switch (_role)
                {
                    case PullRole.Winner:
                        float load = p < 0.18f ? Mathf.Sin(p / 0.18f * Mathf.PI / 2) : 1;
                        lean -= 0.28f * load * (p < 0.18f ? 1 : 1 - (p - 0.18f) / 0.82f * 0.6f);
                        crouch = 0.12f * s;
                        armA = Mathf.Sin(p * Mathf.PI * 4) * 0.45f * s;
                        armB = -armA;
                        legCycle = p * Mathf.PI * 4;
                        legAmplitude = 0.45f * s;
                        armExtend = 0.75f * Mathf.Max(load, s);
                        break;
                    case PullRole.Loser:
                        lean = BaseLean + 0.95f * s + 0.15f * Mathf.Sin(p * Mathf.PI * 9) * s;
                        armReach = 0.55f * s;
                        legCycle = p * Mathf.PI * 6;
                        legAmplitude = 0.6f * s;
                        headTilt = -0.35f * s;
                        break;
                    default:
                        lean -= 0.18f * s + Mathf.Sin(p * Mathf.PI * 8) * 0.06f * s;
                        crouch = 0.08f * s;
                        armExtend = 0.35f * s;
                        break;
                }
            }
            else
            {
                lean += Mathf.Sin(time * 1.2f + phase) * 0.03f;
            }

            rig.SetHipsHeight(CharacterRig.BraceHipsHeight - crouch * CrouchScale);
            rig.SetBodyTilt(lean);

            float stepA = Mathf.Sin(legCycle) * legAmplitude;
            float stepB = Mathf.Sin(legCycle + Mathf.PI) * legAmplitude;
            rig.SetLeg(0, -0.6f - crouch * 1.5f + stepA, 0.5f + crouch * 2 + Mathf.Max(0, -stepA) * 1.2f);
            rig.SetLeg(1, 0.25f - crouch * 1.2f + stepB, 0.35f + crouch * 1.8f + Mathf.Max(0, -stepB) * 1.2f);

            float shoulder = -1.15f - lean * 0.55f - armExtend * 0.5f;
            rig.SetShoulder(0, shoulder + armA - armReach, 0.42f);
            rig.SetShoulder(1, shoulder + armB - armReach, -0.42f);
            rig.SetElbow(0, Mathf.Min(0, -0.55f + armExtend + armA * 0.8f + armReach * 0.9f));
            rig.SetElbow(1, Mathf.Min(0, -0.55f + armExtend + armB * 0.8f + armReach * 0.9f));
            rig.SetHead(-lean * 0.55f + headTilt, Mathf.Sin(time * 0.7f + phase) * 0.06f);
        }

        private void ApplyFall(float time)
        {
            if (ArenaMotion.IsTeetering(_progress))
            {
                float q = _progress / 0.3f;
                rig.SetBodyTilt(0.35f);
                rig.SetShoulder(0, -time * 18f, 0.9f);
                rig.SetShoulder(1, -time * 18f + Mathf.PI, -0.9f);
                rig.SetElbow(0, -0.2f);
                rig.SetElbow(1, -0.2f);
                rig.SetLeg(0, -0.2f, 0.3f);
                rig.SetLeg(1, 0.6f * Mathf.Sin(q * 12), 0.3f);
            }
            else
            {
                rig.SetBodyTilt(0.2f);
                rig.SetShoulder(0, Mathf.PI - 0.3f, 0.4f);
                rig.SetShoulder(1, Mathf.PI - 0.3f, -0.4f);
                rig.SetLeg(0, -0.4f, 0.3f);
                rig.SetLeg(1, 0.4f, 0.3f);
            }
        }

        private void ApplySwimming(float time)
        {
            rig.SetHipsHeight(CharacterRig.StandHipsHeight);
            rig.SetBodyTilt(0.1f);

            for (int i = 0; i < 2; i++)
            {
                float side = i == 0 ? 1 : -1;
                rig.SetShoulder(i, Mathf.PI - 0.4f + Mathf.Sin(time * 9f + i * 2) * 0.5f, side * 0.5f);
                rig.SetElbow(i, -0.4f + Mathf.Sin(time * 9f + i) * 0.3f);
                rig.SetLeg(i, Mathf.Sin(time * 6f + i * 3) * 0.4f, 0.4f);
            }

            rig.SetHead(-0.2f, 0);
        }

        private void ApplyCelebration(float time)
        {
            float jump = Mathf.Abs(Mathf.Sin(time * 5.5f));
            _jump = jump * 0.5f;

            rig.SetHipsHeight(CharacterRig.StandHipsHeight);
            rig.SetBodyTilt(-0.08f);
            rig.SetShoulder(0, Mathf.PI - 0.25f + Mathf.Sin(time * 11f) * 0.15f, 0.35f);
            rig.SetShoulder(1, Mathf.PI - 0.25f + Mathf.Sin(time * 11f + 1.5f) * 0.15f, -0.35f);
            rig.SetElbow(0, -0.3f);
            rig.SetElbow(1, -0.3f);
            rig.SetLeg(0, (1 - jump) * -0.5f * 0.6f, (1 - jump) * 0.9f);
            rig.SetLeg(1, (1 - jump) * -0.5f, (1 - jump) * 0.9f);
            rig.SetHead(-0.15f, 0);
        }
    }
}
