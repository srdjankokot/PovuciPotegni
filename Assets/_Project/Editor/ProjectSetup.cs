using System.IO;
using PovuciPotegni.Game;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace PovuciPotegni.EditorTools
{
    /// <summary>Applies the phase 0 project settings. Safe to run more than once.</summary>
    public static class ProjectSetup
    {
        private const string ApplicationId = "com.srdjankokot.povucipotegni";
        private const string ProductName = "Povuci-Potegni";
        private const string MobileRenderPipelinePath = "Assets/_Project/Settings/Mobile_RPAsset.asset";
        private const string GameConfigPath = "Assets/_Project/Settings/GameConfig.asset";
        private const string MobileQualityName = "Mobile";
        private const string BundleVersion = "0.1.0";
        private const float SplashLogoSeconds = 2f;
        private static readonly Color SplashBackground = new Color32(0xA8, 0xDC, 0xFF, 0xFF);

        [MenuItem("Povuci-Potegni/Podesi projekat")]
        public static void Apply()
        {
            ApplyPlayerSettings();
            ApplyMobileBuildSettings();
            ApplyBranding();
            ApplyRenderPipeline();
            ApplyQualityLevel();
            EnsureGameConfig();

            AssetDatabase.SaveAssets();
            Debug.Log("Povuci-Potegni: podešavanja projekta su primenjena.");
        }

        private static void ApplyPlayerSettings()
        {
            PlayerSettings.productName = ProductName;

            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Android, ApplicationId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.iOS, ApplicationId);
            PlayerSettings.SetApplicationIdentifier(NamedBuildTarget.Standalone, ApplicationId);

            PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
            PlayerSettings.allowedAutorotateToPortrait = true;
            PlayerSettings.allowedAutorotateToPortraitUpsideDown = false;
            PlayerSettings.allowedAutorotateToLandscapeLeft = false;
            PlayerSettings.allowedAutorotateToLandscapeRight = false;
        }

        /// <summary>What the stores require: 64-bit code through IL2CPP, and a version to start from.</summary>
        private static void ApplyMobileBuildSettings()
        {
            PlayerSettings.bundleVersion = BundleVersion;

            PlayerSettings.SetScriptingBackend(NamedBuildTarget.Android, ScriptingImplementation.IL2CPP);
            PlayerSettings.Android.targetArchitectures = AndroidArchitecture.ARM64 | AndroidArchitecture.ARMv7;
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.Android, ManagedStrippingLevel.Low);
            PlayerSettings.SetManagedStrippingLevel(NamedBuildTarget.iOS, ManagedStrippingLevel.Low);
        }

        private static void ApplyBranding()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(PortraitRenderer.IconPath);
            var logo = AssetDatabase.LoadAssetAtPath<Sprite>(PortraitRenderer.SplashLogoPath);
            if (icon == null || logo == null)
            {
                // The artwork comes from the arena build, which calls this method again afterwards.
                return;
            }

            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            PlayerSettings.SplashScreen.show = true;
            PlayerSettings.SplashScreen.showUnityLogo = false;
            PlayerSettings.SplashScreen.backgroundColor = SplashBackground;
            PlayerSettings.SplashScreen.unityLogoStyle = PlayerSettings.SplashScreen.UnityLogoStyle.DarkOnLight;
            PlayerSettings.SplashScreen.logos = new[] { PlayerSettings.SplashScreenLogo.Create(SplashLogoSeconds, logo) };
        }

        private static void ApplyRenderPipeline()
        {
            var mobile = AssetDatabase.LoadAssetAtPath<RenderPipelineAsset>(MobileRenderPipelinePath);
            if (mobile == null)
            {
                throw new FileNotFoundException("URP asset nije pronađen.", MobileRenderPipelinePath);
            }

            GraphicsSettings.defaultRenderPipeline = mobile;
        }

        private static void ApplyQualityLevel()
        {
            int index = System.Array.IndexOf(QualitySettings.names, MobileQualityName);
            if (index < 0)
            {
                throw new System.InvalidOperationException($"Nivo kvaliteta '{MobileQualityName}' ne postoji.");
            }

            QualitySettings.SetQualityLevel(index, true);
        }

        private static void EnsureGameConfig()
        {
            if (AssetDatabase.LoadAssetAtPath<GameConfigAsset>(GameConfigPath) != null)
            {
                return;
            }

            AssetDatabase.CreateAsset(ScriptableObject.CreateInstance<GameConfigAsset>(), GameConfigPath);
        }
    }
}
