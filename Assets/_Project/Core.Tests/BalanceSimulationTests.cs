using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace PovuciPotegni.Core.Tests
{
    /// <summary>
    /// Prints win rates for the balance targets in docs/GDD.md. The numbers are read from the test output;
    /// the assertions only guard that they make sense, because the targets are still being tuned.
    /// </summary>
    public class BalanceSimulationTests
    {
        private const int Matches = 10000;
        private const double AveragePlayerAccuracy = 0.65;

        private enum PlayerModel
        {
            /// <summary>65% on every question, as docs/ZADACI.md specifies.</summary>
            Flat,

            /// <summary>65% on medium questions, with the same difficulty modifiers as the bots.</summary>
            FeelsDifficulty,
        }

        private static readonly (string Category, double Share)[] CategoryShares =
        {
            (QuestionCategories.History, 0.15),
            (QuestionCategories.Geography, 0.15),
            (QuestionCategories.Sport, 0.15),
            (QuestionCategories.FilmTv, 0.12),
            (QuestionCategories.Music, 0.12),
            (QuestionCategories.Science, 0.11),
            (QuestionCategories.ArtLiterature, 0.10),
            (QuestionCategories.General, 0.10),
        };

        private static readonly Dictionary<string, double> WinTargets = new Dictionary<string, double>
        {
            { "segrt", 0.75 },
            { "profesor", 0.50 },
            { "lisac", 0.30 },
        };

        private readonly struct SimulationResult
        {
            public double WinRate { get; }
            public double AverageQuestions { get; }
            public double OvertimeRate { get; }
            public double EdgeRate { get; }

            public SimulationResult(double winRate, double averageQuestions, double overtimeRate, double edgeRate)
            {
                WinRate = winRate;
                AverageQuestions = averageQuestions;
                OvertimeRate = overtimeRate;
                EdgeRate = edgeRate;
            }
        }

        [Test]
        public void AveragePlayerAgainstEveryBot()
        {
            var config = new GameConfig();
            var flatWinRates = new List<double>();
            var realisticWinRates = new List<double>();

            TestContext.Out.WriteLine($"BALANS igrac 65% | {Matches} partija po botu i scenariju");
            foreach (BotProfile bot in config.Bots)
            {
                SimulationResult flat = Simulate(config, bot, PlayerModel.Flat, false, 1000);
                SimulationResult flatJokers = Simulate(config, bot, PlayerModel.Flat, true, 2000);
                SimulationResult realistic = Simulate(config, bot, PlayerModel.FeelsDifficulty, false, 3000);
                SimulationResult realisticJokers = Simulate(config, bot, PlayerModel.FeelsDifficulty, true, 4000);
                flatWinRates.Add(flat.WinRate);
                realisticWinRates.Add(realistic.WinRate);

                string target = WinTargets.TryGetValue(bot.Id, out double value) ? $"{value:P0}" : "-";
                TestContext.Out.WriteLine(
                    $"BALANS {bot.Id,-9} cilj {target,5} | stalnih 65%: {Describe(flat)} / dzokeri {Describe(flatJokers)} | " +
                    $"sa tezinom: {Describe(realistic)} / dzokeri {Describe(realisticJokers)} | " +
                    $"ivica {realistic.EdgeRate:P0}, produzeci {realistic.OvertimeRate:P1}");

                Assert.That(flat.AverageQuestions, Is.InRange(3.0, 17.0));
                Assert.That(realistic.AverageQuestions, Is.InRange(3.0, 17.0));
                Assert.That(flatJokers.WinRate, Is.GreaterThan(flat.WinRate));
                Assert.That(realisticJokers.WinRate, Is.GreaterThan(realistic.WinRate));
            }

            Assert.That(flatWinRates, Is.Ordered.Descending);
            Assert.That(realisticWinRates, Is.Ordered.Descending);
        }

        [Test]
        public void AccuracySweep_ShowsWhichBotAccuracyHitsEachTarget()
        {
            var config = new GameConfig();

            TestContext.Out.WriteLine("BALANS tacnost bota (tezina kao u GDD, bez jake i slabe kategorije)");
            for (double accuracy = 0.40; accuracy < 0.901; accuracy += 0.05)
            {
                var bot = new BotProfile("sweep", accuracy, 1, 2, 0, "", "");
                SimulationResult flat = Simulate(config, bot, PlayerModel.Flat, false, 5000);
                SimulationResult flatJokers = Simulate(config, bot, PlayerModel.Flat, true, 6000);
                SimulationResult realistic = Simulate(config, bot, PlayerModel.FeelsDifficulty, false, 7000);
                SimulationResult realisticJokers = Simulate(config, bot, PlayerModel.FeelsDifficulty, true, 8000);
                TestContext.Out.WriteLine(
                    $"BALANS tacnost {accuracy:P0} | stalnih 65%: {flat.WinRate:P1} / dzokeri {flatJokers.WinRate:P1} | " +
                    $"sa tezinom: {realistic.WinRate:P1} / dzokeri {realisticJokers.WinRate:P1}");
            }
        }

        private static string Describe(SimulationResult result)
        {
            return $"{result.WinRate:P1} ({result.AverageQuestions:F1} pit.)";
        }

        private static SimulationResult Simulate(GameConfig config, BotProfile bot, PlayerModel model, bool useJokers, int seed)
        {
            var random = new SeededRandom(seed);
            int wins = 0;
            int questions = 0;
            int overtimes = 0;
            int edges = 0;

            for (int i = 0; i < Matches; i++)
            {
                MatchState end = PlayMatch(config, bot, model, useJokers, random);
                wins += end.Winner == Side.Player ? 1 : 0;
                questions += end.QuestionsPlayed;
                overtimes += end.EndReason == MatchEndReason.Overtime ? 1 : 0;
                edges += end.EndReason == MatchEndReason.Edge ? 1 : 0;
            }

            return new SimulationResult(
                wins / (double)Matches,
                questions / (double)Matches,
                overtimes / (double)Matches,
                edges / (double)Matches);
        }

        private static MatchState PlayMatch(GameConfig config, BotProfile bot, PlayerModel model, bool useJokers, IRandom random)
        {
            MatchState state = MatchState.Initial;

            while (!state.IsFinished)
            {
                int difficulty = MatchRules.NextDifficulty(config, state);
                string category = PickCategory(random);
                double playerAccuracy = PlayerAccuracy(config, model, difficulty);

                if (useJokers)
                {
                    state = ApplyJokerPolicy(state, ref playerAccuracy);
                }

                bool playerCorrect = random.NextDouble() < playerAccuracy;
                bool botCorrect = random.NextDouble() < BotBrain.CorrectProbability(config, bot, difficulty, category);

                state = MatchRules.ResolveRound(config, state, playerCorrect, botCorrect).State;
            }

            return state;
        }

        private static double PlayerAccuracy(GameConfig config, PlayerModel model, int difficulty)
        {
            if (model == PlayerModel.Flat)
            {
                return AveragePlayerAccuracy;
            }

            var player = new BotProfile("player", AveragePlayerAccuracy, 0, 0, 0, "", "");
            return BotBrain.CorrectProbability(config, player, difficulty, "");
        }

        /// <summary>A simple player: arms Double at once, and uses 50/50 and Shield when falling behind.</summary>
        private static MatchState ApplyJokerPolicy(MatchState state, ref double playerAccuracy)
        {
            if (MatchRules.TryUseJoker(state, Side.Player, JokerType.Double, out MatchState afterDouble))
            {
                state = afterDouble;
            }

            if (state.Rope.Value <= -1 && MatchRules.TryUseJoker(state, Side.Player, JokerType.FiftyFifty, out MatchState afterFifty))
            {
                state = afterFifty;
                playerAccuracy = AccuracyAfterFiftyFifty(playerAccuracy);
            }

            if (state.Rope.Value <= -2 && MatchRules.TryUseJoker(state, Side.Player, JokerType.Shield, out MatchState afterShield))
            {
                state = afterShield;
            }

            return state;
        }

        /// <summary>
        /// The player knows some answers and guesses among three on the rest: p = known + (1 - known) / 3.
        /// Removing one option turns the guess into one of two: known + (1 - known) / 2 = (1 + 3p) / 4.
        /// </summary>
        private static double AccuracyAfterFiftyFifty(double accuracy)
        {
            return (1 + 3 * accuracy) / 4;
        }

        private static string PickCategory(IRandom random)
        {
            double roll = random.NextDouble();
            foreach ((string category, double share) in CategoryShares)
            {
                roll -= share;
                if (roll < 0)
                {
                    return category;
                }
            }

            return CategoryShares.Last().Category;
        }
    }
}
