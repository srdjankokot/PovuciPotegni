using System.Collections;
using NUnit.Framework;
using PovuciPotegni.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace PovuciPotegni.Game.PlayTests
{
    /// <summary>Runs the real Sandbox scene in Play mode. Any error logged during a test fails it.</summary>
    public class SandboxPlayTests
    {
        private const string SceneName = "Sandbox";
        private const float TimingTolerance = 0.2f;

        private ArenaDirector _director;
        private static string _lastDetail = "";

        [UnitySetUp]
        public IEnumerator LoadSandbox()
        {
            SceneManager.LoadScene(SceneName);
            yield return null;
            _director = Object.FindAnyObjectByType<ArenaDirector>();
            Assert.That(_director, Is.Not.Null);
        }

        [UnityTest]
        public IEnumerator FirstRender_IsNotTintedOneColor()
        {
            var camera = Camera.main;
            var target = RenderTexture.GetTemporary(64, 128, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            camera.targetTexture = target;
            camera.Render();
            camera.targetTexture = null;

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = target;
            var image = new Texture2D(64, 128, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 64, 128), 0, 0);
            image.Apply();
            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);

            // The path in front of the camera is light green. The tinted frame made everything blue.
            Color path = image.GetPixel(40, 8);
            Object.Destroy(image);
            Assert.That(path.g, Is.GreaterThan(path.b), $"Boja staze: {path}");
            yield return null;
        }

        [UnityTest]
        public IEnumerator Pull_MovesRopeInDocumentedTime()
        {
            bool done = false;
            float start = Time.time;

            _director.PlayPull(1, () => done = true);
            Assert.That(_director.IsBusy, Is.True);

            yield return new WaitUntil(() => done);

            Assert.That(Time.time - start, Is.EqualTo(ArenaMotion.PullSeconds).Within(TimingTolerance));
            Assert.That(_director.RopePosition, Is.EqualTo(1));
            Assert.That(_director.IsBusy, Is.False);
        }

        [UnityTest]
        public IEnumerator Tie_KeepsRopePosition()
        {
            bool done = false;
            float start = Time.time;

            _director.PlayPull(0, () => done = true);
            yield return new WaitUntil(() => done);

            Assert.That(Time.time - start, Is.EqualTo(ArenaMotion.TieSeconds).Within(TimingTolerance));
            Assert.That(_director.RopePosition, Is.EqualTo(0));
        }

        [UnityTest]
        public IEnumerator Fall_EndsInWater_AndWinnerCelebrates()
        {
            bool done = false;
            float start = Time.time;
            _director.ShowRopePosition(3);

            _director.PlayFall(Side.Opponent, () => done = true);
            yield return new WaitUntil(() => done);

            Assert.That(Time.time - start, Is.EqualTo(ArenaMotion.FallSeconds).Within(TimingTolerance));
            Assert.That(_director.Crocodile.HasFocus, Is.True);

            _director.PlayCelebration(Side.Player);
            yield return new WaitForSeconds(0.2f);

            Assert.That(_director.Opponent.transform.position.y, Is.LessThan(-0.5f));
            Assert.That(_director.Player.HandsPoint.y, Is.LessThan(0.1f), "Pobednik treba da pusti uže.");

            _director.ResetArena();
            yield return null;
            Assert.That(_director.Opponent.transform.position.y, Is.EqualTo(0).Within(0.01f));
            Assert.That(_director.Crocodile.HasFocus, Is.False);
        }

        /// <summary>While both hold the rope it must run past their bodies, not through them.</summary>
        [UnityTest]
        public IEnumerator DuringPlay_RopePassesBesideBothBodies()
        {
            RopeView rope = Object.FindAnyObjectByType<RopeView>();
            float closest = float.MaxValue;
            string where = "";

            void Measure(string moment)
            {
                foreach (PoseController view in new[] { _director.Player, _director.Opponent })
                {
                    float clearance = RopeClearance(rope, view.transform);
                    if (clearance < closest)
                    {
                        closest = clearance;
                        Vector3 hands = view.HandsPoint - view.transform.position;
                        where = $"{moment}, {view.name}, šake u odnosu na lika {hands}, {_lastDetail}";
                    }
                }
            }

            for (int frame = 0; frame < 30; frame++)
            {
                yield return null;
                Measure("mirovanje");
            }

            foreach (int steps in new[] { 1, -1, -1, 0 })
            {
                bool done = false;
                _director.PlayPull(steps, () => done = true);
                while (!done)
                {
                    yield return null;
                    Measure($"potez {steps}");
                }
            }

            Assert.That(closest, Is.GreaterThan(0), $"Uže ulazi u telo: {closest:F3} m ({where}).");
        }

        /// <summary>The smallest gap between the rope's surface and the torso and legs, seen as capsules.</summary>
        private static float RopeClearance(RopeView rope, Transform character)
        {
            const float RopeRadius = 0.055f;

            // The same measures as CharacterBuilder: the torso narrows from the belly to the chest.
            const float BellyRadius = 0.32f;
            const float ChestRadius = 0.27f;
            const float LegRadius = 0.145f;

            Transform hips = character.Find("Hips");
            Transform body = hips.Find("Body");
            Vector3 torsoTop = body.TransformPoint(new Vector3(0, 0.44f, 0));
            float closest = float.MaxValue;

            foreach (Vector3 point in rope.Centerline)
            {
                Vector3 axis = torsoTop - hips.position;
                float along = Mathf.Clamp01(Vector3.Dot(point - hips.position, axis) / axis.sqrMagnitude);
                float torsoRadius = Mathf.Lerp(BellyRadius, ChestRadius, along);
                float torsoGap = DistanceToSegment(point, hips.position, torsoTop) - torsoRadius - RopeRadius;
                if (torsoGap < closest)
                {
                    _lastDetail = $"trup, tačka {point - character.position}";
                }

                closest = Mathf.Min(closest, torsoGap);

                foreach (Transform thigh in hips)
                {
                    if (thigh.name != "Thigh")
                    {
                        continue;
                    }

                    Transform knee = thigh.Find("Knee");
                    Vector3 ankle = knee.TransformPoint(new Vector3(0, -0.2f, 0));
                    float legGap = Mathf.Min(
                        DistanceToSegment(point, thigh.position, knee.position),
                        DistanceToSegment(point, knee.position, ankle)) - LegRadius - RopeRadius;
                    if (legGap < closest)
                    {
                        _lastDetail = $"noga, tačka {point - character.position}";
                    }

                    closest = Mathf.Min(closest, legGap);
                }
            }

            return closest;
        }

        private static float DistanceToSegment(Vector3 point, Vector3 a, Vector3 b)
        {
            Vector3 along = b - a;
            float t = Mathf.Clamp01(Vector3.Dot(point - a, along) / along.sqrMagnitude);
            return Vector3.Distance(point, a + along * t);
        }

        /// <summary>The rope must not pass through the winner's legs or the head of whoever is in the water.</summary>
        [UnityTest]
        public IEnumerator AfterFall_RopeStaysClearOfBothCharacters([Values(Side.Player, Side.Opponent)] Side loser)
        {
            const float LegsClearance = 0.38f;
            const float HeadClearance = 0.5f;

            RopeView rope = Object.FindAnyObjectByType<RopeView>();
            PoseController loserView = loser == Side.Player ? _director.Player : _director.Opponent;
            PoseController winnerView = loser == Side.Player ? _director.Opponent : _director.Player;
            Side winner = loser == Side.Player ? Side.Opponent : Side.Player;
            bool done = false;

            _director.ShowRopePosition(loser == Side.Player ? -3 : 3);
            _director.PlayFall(loser, () => done = true);
            yield return new WaitUntil(() => done);

            _director.PlayCelebration(winner);
            Transform head = loserView.transform.Find("Hips/Body/Head");

            for (float end = Time.time + 1.5f; Time.time < end; )
            {
                yield return null;

                Vector3 feet = winnerView.transform.position;
                foreach (Vector3 point in rope.Centerline)
                {
                    float toLegs = Vector2.Distance(new Vector2(point.x, point.z), new Vector2(feet.x, feet.z));
                    Assert.That(toLegs, Is.GreaterThan(LegsClearance), "Uže prolazi kroz noge pobednika.");
                    Assert.That(Vector3.Distance(point, head.position), Is.GreaterThan(HeadClearance), "Uže prolazi kroz glavu lika u vodi.");
                }
            }
        }

        [UnityTest]
        public IEnumerator Effects_SpawnDuringPullAndSplash()
        {
            FxPool fx = Object.FindAnyObjectByType<FxPool>();
            bool done = false;

            _director.PlayPull(1, () => done = true);
            yield return new WaitForSeconds(ArenaMotion.PullSeconds * 0.6f);
            Assert.That(fx.AliveCount, Is.GreaterThan(0), "Prašina");
            yield return new WaitUntil(() => done);

            _director.ShowRopePosition(3);
            done = false;
            _director.PlayFall(Side.Opponent, () => done = true);
            yield return new WaitForSeconds(ArenaMotion.FallSeconds * (ArenaMotion.SplashAt + 0.05f));
            Assert.That(fx.AliveCount, Is.GreaterThan(20), "Pljusak");
            yield return new WaitUntil(() => done);
        }

        [UnityTest]
        public IEnumerator Crocodile_LurksNearEdgeAndOpensJaw()
        {
            _director.ShowRopePosition(2);
            yield return new WaitForSeconds(2.5f);

            Transform crocodile = _director.Crocodile.transform;
            Assert.That(crocodile.position.z, Is.GreaterThan(0), "Krokodil treba da je ispod protivnika.");
            Assert.That(_director.Crocodile.JawOpen, Is.GreaterThan(0.2f));
        }
    }
}
