using System.IO;
using PovuciPotegni.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PovuciPotegni.EditorTools
{
    /// <summary>Renders control frames of the Sandbox scene to TestResults/captures, for comparing with reference/slike.</summary>
    public static class SceneCapture
    {
        private const int Width = 780;
        private const int Height = 1688;
        private const int PortraitSize = 600;
        private const string OutputFolder = "TestResults/captures";
        private const float FrameStep = 1f / 30f;

        [MenuItem("Povuci-Potegni/Snimi kontrolne kadrove")]
        public static void CaptureAll()
        {
            EditorSceneManager.OpenScene(SceneBuilder.SandboxScenePath);
            Directory.CreateDirectory(OutputFolder);

            var director = Object.FindAnyObjectByType<ArenaDirector>();
            var rope = Object.FindAnyObjectByType<RopeView>();
            var rig = Object.FindAnyObjectByType<CameraRig>();
            var camera = rig.GetComponent<Camera>();

            camera.aspect = Width / (float)Height;

            // Saved separately: the first render after loading can differ from the following ones.
            Render(camera, Width, Height, "first-render.png");

            foreach (int k in new[] { 0, 1, -1, 2, -2 })
            {
                director.ShowRopePosition(k);
                director.Tick(0);
                rope.Rebuild(0);
                rig.Snap();
                Render(camera, Width, Height, $"k{k:+0;-0;0}.png");
            }

            CaptureAnimations(camera, director, rig);
            director.ResetArena();
            director.Tick(0);
            CapturePortraits(camera, rope);

            // Leave the scene as saved.
            EditorSceneManager.OpenScene(SceneBuilder.SandboxScenePath);
            Debug.Log($"Povuci-Potegni: kadrovi su u {Path.GetFullPath(OutputFolder)}.");
        }

        /// <summary>Steps the real animations at a fixed rate and saves frames at chosen moments.</summary>
        private static void CaptureAnimations(Camera camera, ArenaDirector director, CameraRig rig)
        {
            var fx = Object.FindAnyObjectByType<FxPool>();

            void Step(float seconds)
            {
                for (float elapsed = 0; elapsed < seconds; elapsed += FrameStep)
                {
                    director.Tick(FrameStep);
                    fx.SimulateForCapture(FrameStep);
                }
            }

            director.ResetArena();
            director.ShowRopePosition(0);
            director.Tick(0);
            rig.Snap();

            director.PlayPull(1);
            Step(ArenaMotion.PullSeconds * 0.55f);
            Render(camera, Width, Height, "anim-pull.png");
            Step(ArenaMotion.PullSeconds * 0.5f);

            director.PlayPull(0);
            Step(ArenaMotion.TieSeconds * 0.3f);
            Render(camera, Width, Height, "anim-tie.png");
            Step(ArenaMotion.TieSeconds * 0.8f);

            director.ShowRopePosition(2);
            Step(5f);
            Render(camera, Width, Height, "anim-crocodile.png");

            director.ShowRopePosition(3);
            director.PlayFall(PovuciPotegni.Core.Side.Opponent);
            Step(ArenaMotion.FallSeconds * 0.2f);
            Render(camera, Width, Height, "anim-teeter.png");
            Step(ArenaMotion.FallSeconds * 0.47f);
            Render(camera, Width, Height, "anim-splash.png");
            Step(ArenaMotion.FallSeconds * 0.4f);

            director.PlayCelebration(PovuciPotegni.Core.Side.Player);
            Step(1.1f);
            Render(camera, Width, Height, "anim-win.png");

            director.ResetArena();
            director.ShowRopePosition(-3);
            director.Tick(0);
            rig.Snap();
            director.PlayFall(PovuciPotegni.Core.Side.Player);
            Step(ArenaMotion.FallSeconds * 1.05f);
            director.PlayCelebration(PovuciPotegni.Core.Side.Opponent);
            Step(1.1f);
            Render(camera, Width, Height, "anim-lose.png");
        }

        private static void CapturePortraits(Camera camera, RopeView rope)
        {
            rope.Rebuild(0);
            camera.aspect = 1;
            camera.fieldOfView = 34;

            var opponentZ = ArenaLayout.OpponentZ(0);
            foreach (CharacterRig rig in Object.FindObjectsByType<CharacterRig>(FindObjectsSortMode.None))
            {
                rig.gameObject.SetActive(false);
            }

            foreach (CharacterLook look in CharacterLook.All)
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{SceneBuilder.CharacterFolder}/{look.Id}.prefab");
                GameObject instance = Object.Instantiate(prefab, new Vector3(0, 0, opponentZ), Quaternion.Euler(0, 180, 0));
                instance.GetComponent<CharacterRig>().SetMood(look == CharacterLook.Lisac ? CharacterMood.Grit : CharacterMood.Smile);

                // The whole figure from the front, slightly to the side.
                Vector3 target = instance.transform.position + Vector3.up * 0.9f;
                camera.transform.position = target + new Vector3(0.9f, 0.3f, -3.3f);
                camera.transform.LookAt(target);

                Render(camera, PortraitSize, PortraitSize, $"portrait-{look.Id}.png");
                Object.DestroyImmediate(instance);
            }
        }

        private static void Render(Camera camera, int width, int height, string fileName)
        {
            var target = RenderTexture.GetTemporary(width, height, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;

            camera.targetTexture = target;
            camera.Render();
            camera.targetTexture = null;

            RenderTexture.active = target;
            var image = new Texture2D(width, height, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            image.Apply();
            File.WriteAllBytes(Path.Combine(OutputFolder, fileName), image.EncodeToPNG());

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            Object.DestroyImmediate(image);
        }
    }
}
