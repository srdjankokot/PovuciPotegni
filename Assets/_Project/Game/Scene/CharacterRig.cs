using UnityEngine;

namespace PovuciPotegni.Game
{
    public enum CharacterMood
    {
        Grit,
        Oh,
        Smile,
    }

    /// <summary>Joints and face of a character. Index 0 is the -X side, index 1 the +X side. Angles are radians.</summary>
    public sealed class CharacterRig : MonoBehaviour
    {
        public const float BraceHipsHeight = 0.54f;
        public const float StandHipsHeight = 0.6f;

        private const float BraceLean = -0.42f;

        [SerializeField] private Transform hips;
        [SerializeField] private Transform body;
        [SerializeField] private Transform head;
        [SerializeField] private Transform[] thighs = new Transform[2];
        [SerializeField] private Transform[] knees = new Transform[2];
        [SerializeField] private Transform[] shoulders = new Transform[2];
        [SerializeField] private Transform[] elbows = new Transform[2];
        [SerializeField] private Transform[] hands = new Transform[2];
        [SerializeField] private Transform[] brows = new Transform[2];
        [SerializeField] private Transform[] pupils = new Transform[2];
        [SerializeField] private GameObject mouthGrit;
        [SerializeField] private GameObject mouthOh;
        [SerializeField] private GameObject mouthSmile;

        private bool _hasRopeOverride;
        private Vector3 _ropeOverride;

        /// <summary>Where the rope end is: at the hands, or on the ground or water once the character has let go.</summary>
        public Vector3 HandsPoint => _hasRopeOverride ? _ropeOverride : RealHandsPoint;

        /// <summary>The point between the hands, whether or not they hold the rope.</summary>
        public Vector3 RealHandsPoint => (hands[0].position + hands[1].position) * 0.5f;

        public bool IsRopeReleased => _hasRopeOverride;

        /// <summary>True when this character's end of the rope lies in the river.</summary>
        public bool IsRopeInWater { get; private set; }

        public void Bind(
            Transform hipsJoint,
            Transform bodyJoint,
            Transform headJoint,
            Transform[] thighJoints,
            Transform[] kneeJoints,
            Transform[] shoulderJoints,
            Transform[] elbowJoints,
            Transform[] handParts,
            Transform[] browParts,
            Transform[] pupilParts,
            GameObject grit,
            GameObject oh,
            GameObject smile)
        {
            hips = hipsJoint;
            body = bodyJoint;
            head = headJoint;
            thighs = thighJoints;
            knees = kneeJoints;
            shoulders = shoulderJoints;
            elbows = elbowJoints;
            hands = handParts;
            brows = browParts;
            pupils = pupilParts;
            mouthGrit = grit;
            mouthOh = oh;
            mouthSmile = smile;
        }

        public void SetRopeOverride(Vector3 point, bool inWater = false)
        {
            _hasRopeOverride = true;
            _ropeOverride = point;
            IsRopeInWater = inWater;
        }

        public void ClearRopeOverride()
        {
            _hasRopeOverride = false;
            IsRopeInWater = false;
        }

        public void SetHipsHeight(float height)
        {
            hips.localPosition = new Vector3(0, height, 0);
        }

        public void SetBodyTilt(float x)
        {
            body.localRotation = SceneParts.Rotation(x);
        }

        public void SetLeg(int index, float thigh, float knee)
        {
            thighs[index].localRotation = SceneParts.Rotation(thigh);
            knees[index].localRotation = SceneParts.Rotation(knee);
        }

        public void SetShoulder(int index, float x, float z)
        {
            shoulders[index].localRotation = SceneParts.Rotation(x, 0, z);
        }

        public void SetElbow(int index, float x)
        {
            elbows[index].localRotation = SceneParts.Rotation(x);
        }

        public void SetHead(float x, float y)
        {
            head.localRotation = SceneParts.Rotation(x, y);
        }

        /// <summary>The base pose from docs/SCENA.md: crouched, leaning back, both hands on the rope.</summary>
        public void ApplyBracePose()
        {
            hips.localPosition = new Vector3(0, BraceHipsHeight, 0);
            body.localRotation = SceneParts.Rotation(BraceLean);

            thighs[0].localRotation = SceneParts.Rotation(-0.6f);
            knees[0].localRotation = SceneParts.Rotation(0.5f);
            thighs[1].localRotation = SceneParts.Rotation(0.25f);
            knees[1].localRotation = SceneParts.Rotation(0.35f);

            float shoulderX = -1.15f - BraceLean * 0.55f;
            shoulders[0].localRotation = SceneParts.Rotation(shoulderX, 0, 0.42f);
            shoulders[1].localRotation = SceneParts.Rotation(shoulderX, 0, -0.42f);
            elbows[0].localRotation = SceneParts.Rotation(-0.55f);
            elbows[1].localRotation = SceneParts.Rotation(-0.55f);
            head.localRotation = SceneParts.Rotation(-BraceLean * 0.55f);
        }

        public void SetMood(CharacterMood mood)
        {
            mouthGrit.SetActive(mood == CharacterMood.Grit);
            mouthOh.SetActive(mood == CharacterMood.Oh);
            mouthSmile.SetActive(mood == CharacterMood.Smile);

            float browAngle = mood == CharacterMood.Grit ? 0.28f : mood == CharacterMood.Oh ? -0.25f : -0.05f;
            float browHeight = mood == CharacterMood.Oh ? 0.15f : 0.125f;
            float pupilScale = mood == CharacterMood.Oh ? 0.8f : 1f;

            for (int i = 0; i < brows.Length; i++)
            {
                brows[i].localRotation = SceneParts.Rotation(0, 0, (i == 0 ? 1 : -1) * browAngle);
                Vector3 position = brows[i].localPosition;
                position.y = browHeight;
                brows[i].localPosition = position;
                pupils[i].localScale = new Vector3(pupilScale, pupilScale * 1.2f, pupilScale * 0.5f);
            }
        }
    }
}
