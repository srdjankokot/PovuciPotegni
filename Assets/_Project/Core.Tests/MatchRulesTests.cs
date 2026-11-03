using NUnit.Framework;

namespace PovuciPotegni.Core.Tests
{
    public class MatchRulesTests
    {
        private readonly GameConfig _config = new GameConfig();

        private RoundResult Round(MatchState state, bool playerCorrect, bool opponentCorrect)
        {
            return MatchRules.ResolveRound(_config, state, playerCorrect, opponentCorrect);
        }

        private static JokerState Armed(JokerType joker)
        {
            return JokerState.Full.Used(joker);
        }

        // Movement table

        [Test]
        public void OnlyPlayerCorrect_PullsOneTowardPlayer()
        {
            RoundResult result = Round(MatchState.Initial, true, false);

            Assert.That(result.Outcome, Is.EqualTo(RoundOutcome.PlayerOnlyCorrect));
            Assert.That(result.Steps, Is.EqualTo(1));
            Assert.That(result.State.Rope.Value, Is.EqualTo(1));
        }

        [Test]
        public void OnlyOpponentCorrect_PullsOneTowardOpponent()
        {
            RoundResult result = Round(MatchState.Initial, false, true);

            Assert.That(result.Outcome, Is.EqualTo(RoundOutcome.OpponentOnlyCorrect));
            Assert.That(result.Steps, Is.EqualTo(-1));
            Assert.That(result.State.Rope.Value, Is.EqualTo(-1));
        }

        [Test]
        public void BothCorrect_RopeStaysAndStreakGrows()
        {
            RoundResult result = Round(MatchState.Initial, true, true);

            Assert.That(result.Outcome, Is.EqualTo(RoundOutcome.BothCorrect));
            Assert.That(result.Steps, Is.EqualTo(0));
            Assert.That(result.State.Rope.Value, Is.EqualTo(0));
            Assert.That(result.State.BothCorrectStreak, Is.EqualTo(1));
        }

        [Test]
        public void NeitherCorrect_RopeStaysAndStreakResets()
        {
            RoundResult result = Round(new MatchState(bothCorrectStreak: 2), false, false);

            Assert.That(result.Outcome, Is.EqualTo(RoundOutcome.NeitherCorrect));
            Assert.That(result.Steps, Is.EqualTo(0));
            Assert.That(result.State.BothCorrectStreak, Is.EqualTo(0));
        }

        // Difficulty

        [TestCase(0, 1)]
        [TestCase(4, 1)]
        [TestCase(5, 2)]
        [TestCase(9, 2)]
        [TestCase(10, 3)]
        [TestCase(14, 3)]
        public void Difficulty_FollowsQuestionNumber(int questionsPlayed, int expected)
        {
            var state = new MatchState(questionsPlayed: questionsPlayed);

            Assert.That(MatchRules.NextDifficulty(_config, state), Is.EqualTo(expected));
        }

        [TestCase(1, 2)]
        [TestCase(2, 3)]
        [TestCase(5, 3)]
        public void Difficulty_RisesWithStreakUpToHard(int streak, int expected)
        {
            var state = new MatchState(bothCorrectStreak: streak);

            Assert.That(MatchRules.NextDifficulty(_config, state), Is.EqualTo(expected));
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(false, false)]
        public void Difficulty_ReturnsToBaseAfterAnyMistake(bool playerCorrect, bool opponentCorrect)
        {
            RoundResult result = Round(new MatchState(bothCorrectStreak: 2), playerCorrect, opponentCorrect);

            Assert.That(MatchRules.NextDifficulty(_config, result.State), Is.EqualTo(1));
        }

        [Test]
        public void Difficulty_IsHardInOvertime()
        {
            var state = new MatchState(questionsPlayed: 15, isOvertime: true);

            Assert.That(MatchRules.NextDifficulty(_config, state), Is.EqualTo(3));
        }

        // Shield

        [Test]
        public void Shield_CancelsPullAgainstPlayer()
        {
            var state = new MatchState(rope: -1, playerJokers: Armed(JokerType.Shield));

            RoundResult result = Round(state, false, true);

            Assert.That(result.Steps, Is.EqualTo(0));
            Assert.That(result.State.Rope.Value, Is.EqualTo(-1));
            Assert.That(result.ShieldBlockedBy, Is.EqualTo(Side.Player));
            Assert.That(result.State.PlayerJokers.ShieldArmed, Is.False);
        }

        [Test]
        public void Shield_SavesFromLosingAtEdge()
        {
            var state = new MatchState(rope: -2, playerJokers: Armed(JokerType.Shield));

            RoundResult result = Round(state, false, true);

            Assert.That(result.State.Rope.Value, Is.EqualTo(-2));
            Assert.That(result.State.IsFinished, Is.False);
        }

        [Test]
        public void Shield_IsSpentEvenWithoutAPull()
        {
            var state = new MatchState(playerJokers: Armed(JokerType.Shield));

            RoundResult result = Round(state, true, true);

            Assert.That(result.State.PlayerJokers.ShieldArmed, Is.False);
            Assert.That(result.State.PlayerJokers.ShieldAvailable, Is.False);
            Assert.That(result.ShieldBlockedBy, Is.EqualTo(Side.None));
        }

        [Test]
        public void Shield_DoesNotAffectOwnPull()
        {
            var state = new MatchState(playerJokers: Armed(JokerType.Shield));

            Assert.That(Round(state, true, false).Steps, Is.EqualTo(1));
        }

        // Double

        [Test]
        public void Double_PullsTwo()
        {
            var state = new MatchState(playerJokers: Armed(JokerType.Double));

            RoundResult result = Round(state, true, false);

            Assert.That(result.Steps, Is.EqualTo(2));
            Assert.That(result.State.Rope.Value, Is.EqualTo(2));
            Assert.That(result.DoubleUsedBy, Is.EqualTo(Side.Player));
            Assert.That(result.State.PlayerJokers.DoubleArmed, Is.False);
        }

        [TestCase(2, true, false, 3)]
        [TestCase(-2, false, true, -3)]
        public void Double_DoesNotCrossEdge(int rope, bool playerCorrect, bool opponentCorrect, int expected)
        {
            var state = new MatchState(rope: rope, playerJokers: Armed(JokerType.Double), opponentJokers: Armed(JokerType.Double));

            RoundResult result = Round(state, playerCorrect, opponentCorrect);

            Assert.That(result.State.Rope.Value, Is.EqualTo(expected));
            Assert.That(result.State.EndReason, Is.EqualTo(MatchEndReason.Edge));
        }

        [TestCase(true, true)]
        [TestCase(false, false)]
        [TestCase(false, true)]
        public void Double_StaysArmedUntilItPulls(bool playerCorrect, bool opponentCorrect)
        {
            var state = new MatchState(playerJokers: Armed(JokerType.Double));

            RoundResult result = Round(state, playerCorrect, opponentCorrect);

            Assert.That(result.State.PlayerJokers.DoubleArmed, Is.True);
            Assert.That(result.DoubleUsedBy, Is.EqualTo(Side.None));
        }

        [Test]
        public void Double_StaysArmedWhenPullIsBlockedByShield()
        {
            var state = new MatchState(playerJokers: Armed(JokerType.Double), opponentJokers: Armed(JokerType.Shield));

            RoundResult result = Round(state, true, false);

            Assert.That(result.Steps, Is.EqualTo(0));
            Assert.That(result.ShieldBlockedBy, Is.EqualTo(Side.Opponent));
            Assert.That(result.State.PlayerJokers.DoubleArmed, Is.True);
        }

        // End of match

        [TestCase(2, true, false, Side.Player)]
        [TestCase(-2, false, true, Side.Opponent)]
        public void CrossingEdge_WinsImmediately(int rope, bool playerCorrect, bool opponentCorrect, Side winner)
        {
            RoundResult result = Round(new MatchState(rope: rope), playerCorrect, opponentCorrect);

            Assert.That(result.State.Winner, Is.EqualTo(winner));
            Assert.That(result.State.EndReason, Is.EqualTo(MatchEndReason.Edge));
        }

        [TestCase(1, Side.Player)]
        [TestCase(-1, Side.Opponent)]
        public void AfterLastQuestion_RopeSideDecidesWinner(int rope, Side winner)
        {
            RoundResult result = Round(new MatchState(rope: rope, questionsPlayed: 14), true, true);

            Assert.That(result.State.Winner, Is.EqualTo(winner));
            Assert.That(result.State.EndReason, Is.EqualTo(MatchEndReason.QuestionLimit));
        }

        [Test]
        public void EdgeOnLastQuestion_CountsAsEdge()
        {
            RoundResult result = Round(new MatchState(rope: 2, questionsPlayed: 14), true, false);

            Assert.That(result.State.EndReason, Is.EqualTo(MatchEndReason.Edge));
        }

        [Test]
        public void BeforeLastQuestion_MatchContinues()
        {
            RoundResult result = Round(new MatchState(rope: 1, questionsPlayed: 13), true, true);

            Assert.That(result.State.IsFinished, Is.False);
        }

        [Test]
        public void RopeInMiddleAfterLastQuestion_StartsOvertime()
        {
            RoundResult result = Round(new MatchState(questionsPlayed: 14), true, true);

            Assert.That(result.State.IsFinished, Is.False);
            Assert.That(result.State.IsOvertime, Is.True);
        }

        // Overtime

        [TestCase(true, true)]
        [TestCase(false, false)]
        public void Overtime_ContinuesWithoutAPull(bool playerCorrect, bool opponentCorrect)
        {
            var state = new MatchState(questionsPlayed: 15, isOvertime: true);

            RoundResult result = Round(state, playerCorrect, opponentCorrect);

            Assert.That(result.State.IsFinished, Is.False);
            Assert.That(result.State.IsOvertime, Is.True);
            Assert.That(result.State.QuestionsPlayed, Is.EqualTo(16));
        }

        [TestCase(true, false, Side.Player)]
        [TestCase(false, true, Side.Opponent)]
        public void Overtime_EndsOnFirstPull(bool playerCorrect, bool opponentCorrect, Side winner)
        {
            var state = new MatchState(questionsPlayed: 15, isOvertime: true);

            RoundResult result = Round(state, playerCorrect, opponentCorrect);

            Assert.That(result.State.Winner, Is.EqualTo(winner));
            Assert.That(result.State.EndReason, Is.EqualTo(MatchEndReason.Overtime));
        }

        [Test]
        public void Overtime_ShieldCancelsPullAndContinues()
        {
            var state = new MatchState(questionsPlayed: 15, isOvertime: true, playerJokers: Armed(JokerType.Shield));

            RoundResult result = Round(state, false, true);

            Assert.That(result.State.IsFinished, Is.False);
            Assert.That(result.State.Rope.Value, Is.EqualTo(0));
        }

        [Test]
        public void ResolvingFinishedMatch_Throws()
        {
            var finished = new MatchState(rope: 3, winner: Side.Player, endReason: MatchEndReason.Edge);

            Assert.That(() => Round(finished, true, false), Throws.InvalidOperationException);
        }

        // Jokers

        [TestCase(JokerType.FiftyFifty)]
        [TestCase(JokerType.Shield)]
        [TestCase(JokerType.Double)]
        public void Joker_CanBeUsedOnlyOnce(JokerType joker)
        {
            Assert.That(MatchRules.TryUseJoker(MatchState.Initial, Side.Player, joker, out MatchState afterFirst), Is.True);
            Assert.That(MatchRules.TryUseJoker(afterFirst, Side.Player, joker, out _), Is.False);
        }

        [Test]
        public void Joker_OfOneSideDoesNotAffectTheOther()
        {
            MatchRules.TryUseJoker(MatchState.Initial, Side.Player, JokerType.Shield, out MatchState next);

            Assert.That(next.OpponentJokers.ShieldAvailable, Is.True);
            Assert.That(next.OpponentJokers.ShieldArmed, Is.False);
        }

        [Test]
        public void FiftyFifty_NeverRemovesCorrectAnswer()
        {
            var random = new SeededRandom(1);
            var removedCounts = new int[3];

            for (int correct = 0; correct < 3; correct++)
            {
                for (int i = 0; i < 3000; i++)
                {
                    int removed = MatchRules.PickFiftyFiftyRemoval(correct, 3, random);
                    Assert.That(removed, Is.Not.EqualTo(correct));
                    removedCounts[removed]++;
                }
            }

            Assert.That(removedCounts, Has.All.GreaterThan(0));
        }
    }
}
