using System.Linq;
using NUnit.Framework;

namespace PovuciPotegni.Core.Tests
{
    public class ProjectSmokeTests
    {
        [Test]
        public void TestRunnerWorks()
        {
            Assert.Pass();
        }

        [Test]
        public void CoreDoesNotReferenceUnity()
        {
            var unityReferences = typeof(GameConfig).Assembly
                .GetReferencedAssemblies()
                .Select(assembly => assembly.Name)
                .Where(name => name.StartsWith("Unity"))
                .ToList();

            Assert.That(unityReferences, Is.Empty);
        }

        [Test]
        public void DefaultConfigMatchesGdd()
        {
            var config = new GameConfig();

            Assert.That(config.MaxQuestions, Is.EqualTo(15));
            Assert.That(config.RopeEdge, Is.EqualTo(3));
            Assert.That(config.AnswerSeconds, Is.EqualTo(15));
            Assert.That(config.DoublePullSteps, Is.EqualTo(2));
            Assert.That(config.BotEasyQuestionBonus, Is.EqualTo(0.10));
            Assert.That(config.BotHardQuestionBonus, Is.EqualTo(-0.15));
            Assert.That(config.BotStrongCategoryBonus, Is.EqualTo(0.10));
            Assert.That(config.BotWeakCategoryBonus, Is.EqualTo(-0.10));
            Assert.That(config.AllowUnverified, Is.False);
            Assert.That(config.Bots.Select(bot => bot.Accuracy), Is.EqualTo(new[] { 0.55, 0.70, 0.82, 0.90 }));
            Assert.That(config.Bots.Select(bot => bot.WinsToUnlock), Is.EqualTo(new[] { 0, 3, 10, 25 }));
        }
    }
}
