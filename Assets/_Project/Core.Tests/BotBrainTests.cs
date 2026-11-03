using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace PovuciPotegni.Core.Tests
{
    public class BotBrainTests
    {
        private const int Simulations = 10000;
        private const double Tolerance = 0.02;
        private const string NeutralCategory = QuestionCategories.Music;

        private static readonly GameConfig Config = new GameConfig();

        private static IEnumerable<TestCaseData> AccuracyCases()
        {
            foreach (BotProfile bot in Config.Bots)
            {
                foreach (int difficulty in new[] { 1, 2, 3 })
                {
                    var categories = new List<string> { NeutralCategory };
                    if (bot.StrongCategory != "")
                    {
                        categories.Add(bot.StrongCategory);
                        categories.Add(bot.WeakCategory);
                    }

                    foreach (string category in categories)
                    {
                        yield return new TestCaseData(bot.Id, difficulty, category);
                    }
                }
            }
        }

        [TestCaseSource(nameof(AccuracyCases))]
        public void SeededBot_HitsExpectedAccuracy(string botId, int difficulty, string category)
        {
            BotProfile bot = Config.Bots.Single(profile => profile.Id == botId);
            double expected = ExpectedAccuracy(bot, difficulty, category);
            var random = new SeededRandom(1234);
            int correct = 0;

            for (int i = 0; i < Simulations; i++)
            {
                BotAnswer answer = BotBrain.Decide(Config, bot, difficulty, category, 1, 3, random);
                correct += answer.OptionIndex == 1 ? 1 : 0;
            }

            Assert.That(correct / (double)Simulations, Is.EqualTo(expected).Within(Tolerance));
        }

        [Test]
        public void Probability_MatchesGddExamples()
        {
            BotProfile professor = Config.Bots.Single(profile => profile.Id == "profesor");

            Assert.That(BotBrain.CorrectProbability(Config, professor, 2, NeutralCategory), Is.EqualTo(0.70).Within(1e-9));
            Assert.That(BotBrain.CorrectProbability(Config, professor, 1, QuestionCategories.History), Is.EqualTo(0.90).Within(1e-9));
            Assert.That(BotBrain.CorrectProbability(Config, professor, 3, QuestionCategories.Sport), Is.EqualTo(0.45).Within(1e-9));
        }

        [Test]
        public void Probability_IsClampedToOne()
        {
            var bot = new BotProfile("test", 0.95, 1, 2, 0, QuestionCategories.Sport, "");

            Assert.That(BotBrain.CorrectProbability(Config, bot, 1, QuestionCategories.Sport), Is.EqualTo(1));
        }

        [Test]
        public void WrongAnswer_PicksEveryWrongOptionAndNeverCorrect()
        {
            var bot = new BotProfile("test", 0, 1, 2, 0, "", "");
            var random = new SeededRandom(7);
            var counts = new int[3];

            for (int i = 0; i < Simulations; i++)
            {
                counts[BotBrain.Decide(Config, bot, 2, NeutralCategory, 1, 3, random).OptionIndex]++;
            }

            Assert.That(counts[1], Is.EqualTo(0));
            Assert.That(counts[0] / (double)Simulations, Is.EqualTo(0.5).Within(Tolerance));
            Assert.That(counts[2] / (double)Simulations, Is.EqualTo(0.5).Within(Tolerance));
        }

        [Test]
        public void Delay_StaysInBotRange()
        {
            var random = new SeededRandom(3);

            foreach (BotProfile bot in Config.Bots)
            {
                for (int i = 0; i < 1000; i++)
                {
                    double delay = BotBrain.Decide(Config, bot, 2, NeutralCategory, 0, 3, random).DelaySeconds;
                    Assert.That(delay, Is.InRange(bot.MinAnswerSeconds, bot.MaxAnswerSeconds));
                }
            }
        }

        private static double ExpectedAccuracy(BotProfile bot, int difficulty, string category)
        {
            double expected = bot.Accuracy;
            expected += difficulty == 1 ? 0.10 : difficulty == 3 ? -0.15 : 0;
            expected += category == bot.StrongCategory ? 0.10 : category == bot.WeakCategory ? -0.10 : 0;
            return System.Math.Max(0, System.Math.Min(1, expected));
        }
    }
}
