using System.Linq;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace PovuciPotegni.Game.Tests
{
    public class ProceduralMeshTests
    {
        private static void AssertFacesOutward(Mesh mesh, Vector3 center)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;

            Assert.That(triangles, Is.Not.Empty);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]];
                Vector3 b = vertices[triangles[i + 1]];
                Vector3 c = vertices[triangles[i + 2]];
                Vector3 outward = (a + b + c) / 3 - center;

                // Unity draws the side where cross(b - a, c - a) points at the viewer.
                Assert.That(Vector3.Dot(Vector3.Cross(b - a, c - a), outward), Is.GreaterThan(0), mesh.name);
            }
        }

        [Test]
        public void ClosedShapes_FaceOutward()
        {
            AssertFacesOutward(ProceduralMeshes.Box("box"), Vector3.zero);
            AssertFacesOutward(ProceduralMeshes.Sphere("sphere", 1, 8, 6, true), Vector3.zero);
            AssertFacesOutward(ProceduralMeshes.Sphere("smooth", 1, 16, 10, false), Vector3.zero);
            AssertFacesOutward(ProceduralMeshes.Cylinder("cylinder", 0.5f, 0.6f, 1, 8, false), Vector3.zero);
            AssertFacesOutward(ProceduralMeshes.Cone("cone", 0.8f, 1.7f, 7, true), Vector3.zero);
            AssertFacesOutward(ProceduralMeshes.Dodecahedron("rock"), Vector3.zero);
        }

        [Test]
        public void Dodecahedron_HasTwelvePentagons()
        {
            Assert.That(ProceduralMeshes.Dodecahedron("rock").triangles.Length, Is.EqualTo(12 * 3 * 3));
        }

        [Test]
        public void Plane_FacesUp()
        {
            Mesh plane = ProceduralMeshes.Plane("plane");
            Vector3[] vertices = plane.vertices;
            int[] triangles = plane.triangles;

            Vector3 cross = Vector3.Cross(vertices[triangles[1]] - vertices[triangles[0]], vertices[triangles[2]] - vertices[triangles[0]]);
            Assert.That(cross.y, Is.GreaterThan(0));
        }

        [Test]
        public void FlatMesh_HasOneNormalPerFace()
        {
            Mesh mesh = ProceduralMeshes.Cone("cone", 1, 1, 7, true);
            Vector3[] normals = mesh.normals;

            Assert.That(mesh.vertexCount, Is.EqualTo(mesh.triangles.Length));
            for (int i = 0; i < normals.Length; i += 3)
            {
                Assert.That(normals[i], Is.EqualTo(normals[i + 1]));
                Assert.That(normals[i], Is.EqualTo(normals[i + 2]));
            }
        }
    }

    public class ArenaLayoutTests
    {
        [Test]
        public void MiddlePosition_MatchesSceneDocument()
        {
            Assert.That(ArenaLayout.PlayerZ(0), Is.EqualTo(-5));
            Assert.That(ArenaLayout.OpponentZ(0), Is.EqualTo(5));
            Assert.That(ArenaLayout.CameraPosition(-5), Is.EqualTo(new Vector3(0.75f, 3.1f, -11.2f)));
            Assert.That(ArenaLayout.CameraTarget(-5), Is.EqualTo(new Vector3(0.15f, 1f, 2.5f)));
        }

        [TestCase(3)]
        [TestCase(-3)]
        public void BothCharactersMoveTogether(int k)
        {
            float offset = ArenaLayout.RopeOffset(k);

            Assert.That(ArenaLayout.OpponentZ(offset) - ArenaLayout.PlayerZ(offset), Is.EqualTo(10).Within(1e-5));
        }

        [Test]
        public void AtEdge_LoserStandsAtRiver()
        {
            Assert.That(ArenaLayout.OpponentZ(ArenaLayout.RopeOffset(3)), Is.EqualTo(1.4f).Within(1e-5));
            Assert.That(ArenaLayout.PlayerZ(ArenaLayout.RopeOffset(-3)), Is.EqualTo(-1.4f).Within(1e-5));
        }

        [Test]
        public void Camera_StopsFollowingNearRiver()
        {
            Assert.That(ArenaLayout.CameraPosition(-1.4f).z, Is.EqualTo(-9.4f).Within(1e-5));
        }

        [TestCase(0.46f, 52f)]
        [TestCase(0.71f, 42f)]
        public void Fov_DependsOnAspect(float aspect, float expected)
        {
            Assert.That(ArenaLayout.CameraFov(aspect), Is.EqualTo(expected));
        }
    }

    public class RiverShapeTests
    {
        [TestCase(0f)]
        [TestCase(1.7f)]
        [TestCase(-1.7f)]
        public void AtThePath_RiverMatchesSceneDocument(float x)
        {
            Assert.That(RiverShape.Center(x), Is.EqualTo(0).Within(1e-5));
            Assert.That(RiverShape.BankZ(x, -1), Is.EqualTo(-1.5f).Within(1e-5));
            Assert.That(RiverShape.BankZ(x, 1), Is.EqualTo(1.5f).Within(1e-5));
        }

        [Test]
        public void PastThePath_RiverBendsAndChangesWidth()
        {
            Assert.That(Mathf.Abs(RiverShape.Center(5f)), Is.GreaterThan(0.5f));
            Assert.That(RiverShape.Center(5f), Is.Not.EqualTo(RiverShape.Center(9f)).Within(0.05f));

            float narrowest = float.MaxValue;
            float widest = float.MinValue;
            for (float x = 5; x <= 40; x += 0.5f)
            {
                narrowest = Mathf.Min(narrowest, RiverShape.HalfWidth(x));
                widest = Mathf.Max(widest, RiverShape.HalfWidth(x));
            }

            Assert.That(widest - narrowest, Is.GreaterThan(0.3f));
        }

        [Test]
        public void BanksNeverCrossAndCharactersStayOnLand()
        {
            for (float x = -40; x <= 40; x += 0.25f)
            {
                Assert.That(RiverShape.BankZ(x, 1) - RiverShape.BankZ(x, -1), Is.GreaterThan(2 * RiverShape.BankRim + 1.5f), $"x = {x}");
            }

            Assert.That(RiverShape.DistanceToRiver(0, ArenaLayout.PlayerZ(0)), Is.GreaterThan(0));
            Assert.That(RiverShape.DistanceToRiver(0, ArenaLayout.OpponentZ(0)), Is.GreaterThan(0));
        }

        [Test]
        public void Strip_FacesUp()
        {
            Mesh strip = RiverShape.Strip("strip", x => -1, x => 1);
            Vector3[] vertices = strip.vertices;
            int[] triangles = strip.triangles;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]];
                Vector3 cross = Vector3.Cross(vertices[triangles[i + 1]] - a, vertices[triangles[i + 2]] - a);
                Assert.That(cross.y, Is.GreaterThan(0));
            }
        }

        [TestCase(-1f, 1f)]
        [TestCase(1f, -1f)]
        public void BankWall_FacesTheWater(float side, float expectedZ)
        {
            Mesh wall = RiverShape.BankWall("wall", side);
            Vector3[] vertices = wall.vertices;
            int[] triangles = wall.triangles;

            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]];
                Vector3 cross = Vector3.Cross(vertices[triangles[i + 1]] - a, vertices[triangles[i + 2]] - a);
                Assert.That(cross.z * expectedZ, Is.GreaterThan(0));
            }
        }
    }

    public class ArenaDecorTests
    {
        [TestCase(1.5f, 6f, false)]
        [TestCase(6f, 1f, false)]
        public void NothingStandsOnPathOrRiver(float x, float z, bool expected)
        {
            Assert.That(ArenaDecor.IsClear(x, z, false), Is.EqualTo(expected));
            Assert.That(ArenaDecor.IsClear(x, z, true), Is.EqualTo(expected));
        }

        [Test]
        public void TallDetailsKeepCameraViewFree()
        {
            Assert.That(ArenaDecor.IsClear(3f, -6f, true), Is.False);
            Assert.That(ArenaDecor.IsClear(6f, -6f, true), Is.True);
            Assert.That(ArenaDecor.IsClear(3f, 6f, true), Is.True);
            Assert.That(ArenaDecor.IsClear(3f, -6f, false), Is.True);
        }
    }

    public class ArenaMotionTests
    {
        [TestCase(0f, 1.2f)]
        [TestCase(1.2f, 0f)]
        public void Pull_StartsAndEndsWhereItShould(float from, float to)
        {
            Assert.That(ArenaMotion.PullOffset(0, from, to), Is.EqualTo(from).Within(1e-5));
            Assert.That(ArenaMotion.PullOffset(1, from, to), Is.EqualTo(to).Within(1e-5));
        }

        [Test]
        public void Pull_FirstGivesTheWrongWay_ThenOvershoots()
        {
            float lowest = float.MaxValue;
            float highest = float.MinValue;

            for (float p = 0; p <= 1; p += 0.01f)
            {
                float offset = ArenaMotion.PullOffset(p, 0, 1.2f);
                lowest = Mathf.Min(lowest, offset);
                highest = Mathf.Max(highest, offset);
            }

            Assert.That(lowest, Is.EqualTo(-0.12f).Within(0.005f));
            Assert.That(highest, Is.GreaterThan(1.2f));
        }

        [Test]
        public void Pull_HasNoJumpBetweenItsParts()
        {
            float previous = ArenaMotion.PullOffset(0, 0, 1.2f);

            for (float p = 0.005f; p <= 1; p += 0.005f)
            {
                float offset = ArenaMotion.PullOffset(p, 0, 1.2f);
                Assert.That(Mathf.Abs(offset - previous), Is.LessThan(0.05f), $"p = {p}");
                previous = offset;
            }
        }

        [Test]
        public void Tie_ReturnsToSamePlace()
        {
            Assert.That(ArenaMotion.TieOffset(0, 0.6f), Is.EqualTo(0.6f).Within(1e-5));
            Assert.That(ArenaMotion.TieOffset(1, 0.6f), Is.EqualTo(0.6f).Within(1e-5));
            Assert.That(Mathf.Abs(ArenaMotion.TieOffset(0.375f, 0)), Is.LessThanOrEqualTo(0.13f));
        }

        [Test]
        public void Fall_TeetersFirst_ThenEndsAtSwimHeight()
        {
            Assert.That(ArenaMotion.FallHeight(0.2f), Is.EqualTo(0));
            Assert.That(ArenaMotion.FallForward(0), Is.EqualTo(0));
            Assert.That(ArenaMotion.FallHeight(1), Is.EqualTo(ArenaMotion.SwimHeight).Within(1e-4));
            Assert.That(ArenaMotion.FallHeight(0.42f), Is.GreaterThan(0), "Lik prvo skoči uvis.");
        }

        [Test]
        public void Fall_LandsInTheRiver()
        {
            float z = ArenaLayout.OpponentZ(ArenaLayout.RopeOffset(3)) - ArenaMotion.FallForward(1);

            Assert.That(RiverShape.DistanceToRiver(0, z), Is.LessThan(-0.5f));
        }

        [Test]
        public void Crocodile_LurksOnLosingSide_OtherwisePatrolsTheRiver()
        {
            Assert.That(CrocodileView.PatrolTarget(0, 2).y, Is.GreaterThan(0));
            Assert.That(CrocodileView.PatrolTarget(0, -2).y, Is.LessThan(0));

            for (float time = 0; time < 60; time += 0.5f)
            {
                Vector2 target = CrocodileView.PatrolTarget(time, 0);
                Assert.That(Mathf.Abs(target.x), Is.LessThanOrEqualTo(9));
                Assert.That(RiverShape.DistanceToRiver(target.x, target.y), Is.LessThan(0), $"t = {time}");
            }
        }
    }

    public class PoseControllerTests
    {
        private GameObject _instance;
        private PoseController _pose;

        [SetUp]
        public void CreateCharacter()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Project/Art/Characters/profesor.prefab");
            _instance = Object.Instantiate(prefab);
            _pose = _instance.GetComponent<PoseController>();
            _pose.SetFacing(180);
        }

        [TearDown]
        public void DestroyCharacter()
        {
            Object.DestroyImmediate(_instance);
        }

        [Test]
        public void Swimming_KeepsHeadAboveWater()
        {
            for (float time = 0; time < 5; time += 0.1f)
            {
                _pose.ShowSwimming();
                _pose.UpdateView(1.4f, time);
                Assert.That(_instance.transform.Find("Hips/Body/Head").position.y, Is.GreaterThan(-0.1f), $"t = {time}");
            }
        }

        [Test]
        public void Brace_KeepsFeetNearGroundDuringPull()
        {
            Transform sole = _instance.transform.Find("Hips/Thigh/Knee/Sole");

            foreach (PullRole role in new[] { PullRole.Winner, PullRole.Tie })
            {
                for (float p = 0; p <= 1; p += 0.05f)
                {
                    _pose.ShowPull(role, p);
                    _pose.UpdateView(5, p);
                    Assert.That(sole.position.y, Is.InRange(-0.15f, 0.35f), $"{role}, p = {p}");
                }
            }
        }

        [Test]
        public void Celebration_ReleasesRope_AndIdleTakesItBack()
        {
            _pose.ShowCelebration();
            _pose.UpdateView(5, 0.3f);
            Assert.That(_pose.HandsPoint.y, Is.EqualTo(0.05f).Within(1e-4));

            _pose.ShowIdle();
            _pose.UpdateView(5, 0.3f);
            Assert.That(_pose.HandsPoint.y, Is.GreaterThan(0.3f));
        }

        [Test]
        public void Facing_TurnsTowardRiverWhenFalling()
        {
            _pose.ShowFall(1);
            _pose.UpdateView(5, 0);
            Assert.That(_instance.transform.position.z, Is.LessThan(5), "Protivnik gleda ka -Z, gde je reka.");

            _pose.SetFacing(0);
            _pose.ShowFall(1);
            _pose.UpdateView(-5, 0);
            Assert.That(_instance.transform.position.z, Is.GreaterThan(-5), "Igrač gleda ka +Z, gde je reka.");
        }
    }

    public class RopeCurveTests
    {
        private static readonly Vector3[] Points =
        {
            new Vector3(0, 0, 0),
            new Vector3(1, 1, 0),
            new Vector3(2, 0, 1),
            new Vector3(3, 2, 1),
            new Vector3(5, 0, 0),
        };

        [Test]
        public void PassesThroughEveryControlPoint()
        {
            for (int i = 0; i < Points.Length; i++)
            {
                Vector3 point = RopeCurve.Evaluate(Points, i / (float)(Points.Length - 1));
                Assert.That(Vector3.Distance(point, Points[i]), Is.LessThan(1e-4f), $"Point {i}");
            }
        }

        [Test]
        public void HandlesRepeatedPoints()
        {
            Vector3[] repeated = { Vector3.zero, Vector3.zero, Vector3.one, Vector3.one };

            Vector3 point = RopeCurve.Evaluate(repeated, 0.5f);

            Assert.That(float.IsNaN(point.x), Is.False);
        }
    }

    public class BuiltSceneTests
    {
        private const string CharacterFolder = "Assets/_Project/Art/Characters";

        [Test]
        public void EveryCharacterPrefabHasAllJointsAndOneActiveMouth()
        {
            foreach (CharacterLook look in CharacterLook.All)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{CharacterFolder}/{look.Id}.prefab");
                Assert.That(prefab, Is.Not.Null, look.Id);
                Assert.That(prefab.GetComponent<CharacterRig>(), Is.Not.Null, look.Id);
                Assert.That(prefab.transform.Find("Hips/Body/Head/Mouth/Grit").gameObject.activeSelf, Is.True, look.Id);
                Assert.That(prefab.transform.Find("Hips/Body/Head/Mouth/Oh").gameObject.activeSelf, Is.False, look.Id);
                Assert.That(prefab.transform.Find("Hips/Body/Head/Mouth/Smile").gameObject.activeSelf, Is.False, look.Id);
                Assert.That(prefab.GetComponentsInChildren<MeshFilter>().All(filter => filter.sharedMesh != null), Is.True, look.Id);
            }
        }

        [Test]
        public void CrocodilePrefabHasJointsBound()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{CharacterFolder}/crocodile.prefab");

            Assert.That(prefab, Is.Not.Null);
            Assert.That(prefab.GetComponent<CrocodileView>(), Is.Not.Null);
            Assert.That(prefab.transform.Find("Head/Jaw"), Is.Not.Null);
            Assert.That(prefab.transform.Find("Tail/Segment/Segment/Segment/Segment"), Is.Not.Null);
        }

        [TestCase("Tick")]
        [TestCase("Correct")]
        [TestCase("Wrong")]
        [TestCase("Pull")]
        [TestCase("Splash")]
        [TestCase("Victory")]
        public void EverySoundIsAudible(string name)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/_Project/Audio/{name}.wav");
            Assert.That(clip, Is.Not.Null);
            Assert.That(clip.length, Is.InRange(0.03f, 1.5f));

            var samples = new float[clip.samples];
            clip.GetData(samples, 0);
            Assert.That(samples.Max(sample => Mathf.Abs(sample)), Is.GreaterThan(0.3f), "Zvuk je pretih.");
        }

        [Test]
        public void BrandingIsSet()
        {
            var icons = PlayerSettings.GetIcons(UnityEditor.Build.NamedBuildTarget.Unknown, IconKind.Any);
            Assert.That(icons.Length, Is.GreaterThan(0));
            Assert.That(icons[0], Is.Not.Null);
            Assert.That(PlayerSettings.SplashScreen.logos, Has.Length.EqualTo(1));
        }

        [Test]
        public void AndroidBuildMeetsStoreRequirements()
        {
            Assert.That(PlayerSettings.GetScriptingBackend(UnityEditor.Build.NamedBuildTarget.Android), Is.EqualTo(ScriptingImplementation.IL2CPP));
            Assert.That(PlayerSettings.Android.targetArchitectures.HasFlag(AndroidArchitecture.ARM64), Is.True);
        }

        [Test]
        public void GameSceneIsFirstInBuild()
        {
            Assert.That(EditorBuildSettings.scenes[0].path, Is.EqualTo("Assets/_Project/Scenes/Game.unity"));
            Assert.That(EditorBuildSettings.scenes.Select(scene => scene.path), Does.Contain("Assets/_Project/Scenes/Sandbox.unity"));
        }

        [TestCase("player")]
        [TestCase("segrt")]
        [TestCase("profesor")]
        [TestCase("lisac")]
        [TestCase("majstor")]
        public void EveryCharacterHasPortrait(string id)
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<Texture2D>($"Assets/_Project/Art/Portraits/{id}.png"), Is.Not.Null);
        }

        [Test]
        public void QuestionFileLoadsInDevelopment()
        {
            string json = System.IO.File.ReadAllText("Assets/StreamingAssets/questions/sr-Latn.json");
            var config = AssetDatabase.LoadAssetAtPath<GameConfigAsset>("Assets/_Project/Settings/GameConfig.asset").Config;

            Assert.That(PovuciPotegni.Core.QuestionBank.FromJson(json, config.AllowUnverified).Questions, Is.Not.Empty);
        }
    }
}
