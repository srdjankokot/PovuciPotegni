using NUnit.Framework;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;
using UnityEngine.Rendering;

namespace PovuciPotegni.Game.Tests
{
    public class ProjectSetupTests
    {
        private const string ApplicationId = "com.srdjankokot.povucipotegni";

        [Test]
        public void GameConfigAssetLoadsWithGddValues()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameConfigAsset>("Assets/_Project/Settings/GameConfig.asset");

            Assert.That(asset, Is.Not.Null);
            Assert.That(asset.Config.MaxQuestions, Is.EqualTo(15));
            Assert.That(asset.Config.AnswerSeconds, Is.EqualTo(15));
            Assert.That(asset.Config.Bots, Has.Count.EqualTo(4));
            Assert.That(asset.Config.Bots[3].MinAnswerSeconds, Is.EqualTo(1.5));
        }

        [Test]
        public void PlayerSettingsArePortraitWithAppId()
        {
            Assert.That(PlayerSettings.defaultInterfaceOrientation, Is.EqualTo(UIOrientation.Portrait));
            Assert.That(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android), Is.EqualTo(ApplicationId));
            Assert.That(PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.iOS), Is.EqualTo(ApplicationId));
        }

        [Test]
        public void MobileRenderPipelineIsDefault()
        {
            Assert.That(GraphicsSettings.defaultRenderPipeline, Is.Not.Null);
            Assert.That(GraphicsSettings.defaultRenderPipeline.name, Is.EqualTo("Mobile_RPAsset"));
            Assert.That(QualitySettings.names[QualitySettings.GetQualityLevel()], Is.EqualTo("Mobile"));
        }
    }
}
