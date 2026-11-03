using System.Collections.Generic;
using System.IO;
using PovuciPotegni.Game;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace PovuciPotegni.EditorTools
{
    /// <summary>Renders each character's face in the arena and saves it as the portrait the screens use.</summary>
    public static class PortraitRenderer
    {
        private const int Size = 256;
        private const string OutputFolder = "Assets/_Project/Art/Portraits";
        private const int BrandingSize = 1024;

        public const string IconPath = "Assets/_Project/Art/Branding/icon.png";
        public const string SplashLogoPath = "Assets/_Project/Art/Branding/splash-logo.png";

        public static Dictionary<string, Texture2D> RenderAll()
        {
            EditorSceneManager.OpenScene(SceneBuilder.SandboxScenePath);
            if (!AssetDatabase.IsValidFolder(OutputFolder))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Portraits");
            }

            var camera = Object.FindAnyObjectByType<CameraRig>().GetComponent<Camera>();
            camera.aspect = 1;
            camera.fieldOfView = 32;

            foreach (CharacterRig rig in Object.FindObjectsByType<CharacterRig>(FindObjectsSortMode.None))
            {
                rig.gameObject.SetActive(false);
            }

            Object.FindAnyObjectByType<RopeView>().gameObject.SetActive(false);

            var portraits = new Dictionary<string, Texture2D>();
            foreach (CharacterLook look in CharacterLook.All)
            {
                portraits[look.Id] = RenderPortrait(camera, look.Id);
            }

            // The scene changes above are never saved; the builder opens a new scene next.
            return portraits;
        }

        /// <summary>The app icon and the splash logo: the player's face, rendered large. Needs RenderAll's scene to be open.</summary>
        public static void RenderBranding()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project/Art/Branding"))
            {
                AssetDatabase.CreateFolder("Assets/_Project/Art", "Branding");
            }

            var camera = Object.FindAnyObjectByType<CameraRig>().GetComponent<Camera>();
            GameObject instance = PosedInstance(CharacterLook.Player.Id);
            Vector3 head = instance.transform.Find("Hips/Body/Head").position;
            camera.transform.position = head + new Vector3(0, 0.02f, -1.75f);
            camera.transform.LookAt(head + Vector3.down * 0.02f);

            byte[] png = Render(camera, BrandingSize);
            Object.DestroyImmediate(instance);

            File.WriteAllBytes(IconPath, png);
            File.WriteAllBytes(SplashLogoPath, png);
            AssetDatabase.ImportAsset(IconPath);
            AssetDatabase.ImportAsset(SplashLogoPath);

            var splash = (TextureImporter)AssetImporter.GetAtPath(SplashLogoPath);
            splash.textureType = TextureImporterType.Sprite;
            splash.SaveAndReimport();
        }

        private static Texture2D RenderPortrait(Camera camera, string id)
        {
            GameObject instance = PosedInstance(id);
            Vector3 head = instance.transform.Find("Hips/Body/Head").position;
            camera.transform.position = head + new Vector3(0, 0.05f, -2.05f);
            camera.transform.LookAt(head + Vector3.down * 0.12f);

            string path = $"{OutputFolder}/{id}.png";
            byte[] png = Render(camera, Size);
            File.WriteAllBytes(path, png);
            Object.DestroyImmediate(instance);

            AssetDatabase.ImportAsset(path);
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.mipmapEnabled = false;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>The character at the far end of the path, smiling, with arms down to show the face and outfit.</summary>
        private static GameObject PosedInstance(string id)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{SceneBuilder.CharacterFolder}/{id}.prefab");
            GameObject instance = Object.Instantiate(prefab, new Vector3(0, 0, ArenaLayout.OpponentZ(0)), Quaternion.Euler(0, 180, 0));

            var rig = instance.GetComponent<CharacterRig>();
            rig.SetHipsHeight(CharacterRig.StandHipsHeight);
            rig.SetBodyTilt(0);
            rig.SetHead(0, 0);
            rig.SetMood(CharacterMood.Smile);
            for (int side = 0; side < 2; side++)
            {
                rig.SetShoulder(side, 0, side == 0 ? -0.2f : 0.2f);
                rig.SetElbow(side, -0.15f);
            }

            return instance;
        }

        private static byte[] Render(Camera camera, int size)
        {
            var target = RenderTexture.GetTemporary(size, size, 24, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            RenderTexture previous = RenderTexture.active;

            camera.targetTexture = target;

            // The first render after loading a scene in batch mode can come out in a single color.
            camera.Render();
            camera.Render();
            camera.targetTexture = null;

            RenderTexture.active = target;
            var image = new Texture2D(size, size, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, size, size), 0, 0);
            image.Apply();
            byte[] png = image.EncodeToPNG();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(target);
            Object.DestroyImmediate(image);
            return png;
        }
    }
}
