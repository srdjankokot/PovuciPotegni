using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace PovuciPotegni.Core.Tests
{
    public class BotMatchServiceTests
    {
        private const double BotDelay = 2;

        private FakeClock _clock;
        private BotMatchService _service;
        private List<string> _log;
        private QuestionShown _question;
        private RoundResolved _round;
        private MatchEnded _ended;
        private JokerUsed _joker;
        private Question[] _questions;

        private static BotProfile Bot(double accuracy)
        {
            return new BotProfile("test", accuracy, BotDelay, BotDelay, 0, "", "");
        }

        private void StartMatch(double botAccuracy, int seed = 42, int questionCount = 40)
        {
            _questions = Enumerable.Range(0, questionCount)
                .Select(i => TestQuestions.Create("q" + i, i % 3 + 1, answer: i % 3))
                .ToArray();

            // NUnit reuses one fixture instance for all tests, so results from earlier tests must be cleared.
            _question = null;
            _round = null;
            _ended = null;
            _joker = null;

            _clock = new FakeClock();
            _log = new List<string>();
            var config = new GameConfig
            {
                BotEasyQuestionBonus = 0,
                BotHardQuestionBonus = 0,
            };

            var picker = new QuestionPicker(new QuestionBank(_questions), new InMemoryStatsStore());
            _service = new BotMatchService(config, picker, _clock);
            _service.QuestionShown += shown =>
            {
                _question = shown;
                _log.Add("question");
            };
            _service.OpponentAnswered += _ => _log.Add("opponent");
            _service.JokerUsed += used =>
            {
                _joker = used;
                _log.Add("joker");
            };
            _service.RoundResolved += resolved =>
            {
                _round = resolved;
                _log.Add("round");
            };
            _service.MatchEnded += ended =>
            {
                _ended = ended;
                _log.Add("ended");
            };

            _service.Start(new MatchSetup(Bot(botAccuracy), seed));
        }

        private void Advance(double seconds)
        {
            _clock.NowSeconds += seconds;
            _service.Tick(_clock.NowSeconds);
        }

        private void PlayRound(int playerOption)
        {
            _service.SubmitAnswer(playerOption);
            Advance(BotDelay);
        }

        [Test]
        public void Start_ShowsFirstQuestionWithDeadline()
        {
            StartMatch(1);

            Assert.That(_question.Number, Is.EqualTo(1));
            Assert.That(_question.Options, Has.Count.EqualTo(3));
            Assert.That(_question.DeadlineSeconds, Is.EqualTo(15));
            Assert.That(_question.IsOvertime, Is.False);
        }

        [Test]
        public void QuestionShown_DoesNotExposeTheAnswer()
        {
            string[] properties = typeof(QuestionShown).GetProperties().Select(property => property.Name).ToArray();

            Assert.That(properties, Has.None.Contains("Answer"));
            Assert.That(properties, Has.None.Contains("Correct"));
        }

        [Test]
        public void OpponentAnswered_ComesAfterBotDelay_AndCarriesNoAnswer()
        {
            StartMatch(1);

            Advance(BotDelay - 0.5);
            Assert.That(_log, Has.No.Member("opponent"));

            Advance(0.5);
            Assert.That(_log, Has.Member("opponent"));
            Assert.That(typeof(OpponentAnswered).GetProperties(), Is.Empty);
        }

        [Test]
        public void BotAnswerStaysHiddenUntilPlayerAnswers()
        {
            StartMatch(1);

            Advance(BotDelay + 1);
            Assert.That(_round, Is.Null);

            _service.SubmitAnswer(0);
            Assert.That(_round, Is.Not.Null);
        }

        [Test]
        public void PlayerAnswerFirst_WaitsForBot()
        {
            StartMatch(1);

            _service.SubmitAnswer(0);
            Assert.That(_round, Is.Null);

            Advance(BotDelay);
            Assert.That(_round, Is.Not.Null);
        }

        [Test]
        public void Deadline_CountsAsNoAnswer()
        {
            StartMatch(1);

            Advance(15);

            Assert.That(_round.PlayerChoice, Is.EqualTo(-1));
            Assert.That(_round.Outcome, Is.EqualTo(RoundOutcome.OpponentOnlyCorrect));
            Assert.That(_round.RopePosition, Is.EqualTo(-1));
        }

        [Test]
        public void ExplicitTimeout_CountsAsNoAnswer()
        {
            StartMatch(0);

            _service.SubmitAnswer(-1);
            Advance(BotDelay);

            Assert.That(_round.PlayerChoice, Is.EqualTo(-1));
            Assert.That(_round.Outcome, Is.EqualTo(RoundOutcome.NeitherCorrect));
        }

        [Test]
        public void SpeedDoesNotChangeOutcome()
        {
            StartMatch(1);
            Advance(14.9);
            _service.SubmitAnswer(-1);
            Assert.That(_round.Outcome, Is.EqualTo(RoundOutcome.OpponentOnlyCorrect));

            StartMatch(1);
            _service.SubmitAnswer(-1);
            Advance(BotDelay);
            Assert.That(_round.Outcome, Is.EqualTo(RoundOutcome.OpponentOnlyCorrect));
        }

        [Test]
        public void NextQuestion_WaitsForContinue_AndGetsFreshDeadline()
        {
            StartMatch(1);
            PlayRound(0);

            Advance(100);
            Assert.That(_question.Number, Is.EqualTo(1));

            _service.Continue();
            Assert.That(_question.Number, Is.EqualTo(2));
            Assert.That(_question.DeadlineSeconds, Is.EqualTo(_clock.NowSeconds + 15));
        }

        [Test]
        public void SecondAnswer_IsIgnored()
        {
            StartMatch(1);

            _service.SubmitAnswer(0);
            _service.SubmitAnswer(1);
            Advance(BotDelay);

            Assert.That(_round.PlayerChoice, Is.EqualTo(0));
        }

        [TestCase(3)]
        [TestCase(-2)]
        public void InvalidOption_IsIgnored(int option)
        {
            StartMatch(1);

            _service.SubmitAnswer(option);
            Advance(BotDelay);

            Assert.That(_round, Is.Null);
        }

        [Test]
        public void FiftyFifty_ReportsWrongOption_ThatCannotBeChosen()
        {
            StartMatch(1);

            Assert.That(_service.UseJoker(JokerType.FiftyFifty), Is.True);
            int removed = _joker.RemovedOptionIndex;

            _service.SubmitAnswer(removed);
            Advance(BotDelay);
            Assert.That(_round, Is.Null);

            _service.SubmitAnswer(-1);
            Assert.That(_round.CorrectIndex, Is.Not.EqualTo(removed));
        }

        [Test]
        public void Joker_CannotBeUsedAfterAnswering()
        {
            StartMatch(1);

            _service.SubmitAnswer(0);

            Assert.That(_service.UseJoker(JokerType.Shield), Is.False);
        }

        [Test]
        public void Joker_CannotBeUsedTwice()
        {
            StartMatch(1);

            Assert.That(_service.UseJoker(JokerType.Double), Is.True);
            Assert.That(_service.UseJoker(JokerType.Double), Is.False);
        }

        [Test]
        public void Shield_BlocksPullThroughService()
        {
            StartMatch(1);

            _service.UseJoker(JokerType.Shield);
            _service.SubmitAnswer(-1);
            Advance(BotDelay);

            Assert.That(_round.ShieldBlockedBy, Is.EqualTo(Side.Player));
            Assert.That(_round.RopePosition, Is.EqualTo(0));
        }

        [Test]
        public void Double_PullsTwoThroughService()
        {
            StartMatch(0);

            _service.UseJoker(JokerType.Double);
            AnswerCorrectly();

            Assert.That(_round.DoubleUsedBy, Is.EqualTo(Side.Player));
            Assert.That(_round.RopePosition, Is.EqualTo(2));
        }

        [Test]
        public void LosingAllRounds_EndsAtEdge()
        {
            StartMatch(1);

            for (int i = 0; i < 3; i++)
            {
                PlayRound(-1);
                _service.Continue();
            }

            Assert.That(_ended.Winner, Is.EqualTo(Side.Opponent));
            Assert.That(_ended.Reason, Is.EqualTo(MatchEndReason.Edge));
            Assert.That(_ended.QuestionsPlayed, Is.EqualTo(3));
            Assert.That(_ended.OpponentCorrect, Is.EqualTo(3));
            Assert.That(_ended.PlayerCorrect, Is.EqualTo(0));
            Assert.That(_log.Last(), Is.EqualTo("ended"));
            Assert.That(_log.Count(entry => entry == "round"), Is.EqualTo(3));
        }

        [Test]
        public void NobodyCorrect_LeadsToOvertimeAfterFifteenQuestions()
        {
            StartMatch(0);

            for (int i = 0; i < 15; i++)
            {
                PlayRound(-1);
                _service.Continue();
            }

            Assert.That(_ended, Is.Null);
            Assert.That(_question.Number, Is.EqualTo(16));
            Assert.That(_question.IsOvertime, Is.True);
            Assert.That(_question.Difficulty, Is.EqualTo(3));
        }

        [Test]
        public void Overtime_EndsOnFirstPull()
        {
            StartMatch(0);

            for (int i = 0; i < 15; i++)
            {
                PlayRound(-1);
                _service.Continue();
            }

            AnswerCorrectly();

            Assert.That(_ended.Winner, Is.EqualTo(Side.Player));
            Assert.That(_ended.Reason, Is.EqualTo(MatchEndReason.Overtime));
            Assert.That(_ended.QuestionsPlayed, Is.EqualTo(16));
        }

        [Test]
        public void AfterMatchEnds_InputIsIgnored()
        {
            StartMatch(1);

            for (int i = 0; i < 3; i++)
            {
                PlayRound(-1);
                _service.Continue();
            }

            int entries = _log.Count;
            _service.SubmitAnswer(0);
            _service.Continue();
            Advance(100);

            Assert.That(_service.UseJoker(JokerType.Shield), Is.False);
            Assert.That(_log, Has.Count.EqualTo(entries));
        }

        [Test]
        public void SameSeed_GivesSameMatch()
        {
            List<string> Play()
            {
                StartMatch(0.5, seed: 99);
                var trace = new List<string>();
                for (int i = 0; i < 3; i++)
                {
                    trace.Add(_question.QuestionId + string.Join(",", _question.Options));
                    PlayRound(0);
                    trace.Add(_round.OpponentChoice.ToString());
                    _service.Continue();
                }

                return trace;
            }

            Assert.That(Play(), Is.EqualTo(Play()));
        }

        [Test]
        public void OptionsAreShuffled()
        {
            var orders = new HashSet<string>();

            for (int seed = 0; seed < 30; seed++)
            {
                StartMatch(1, seed);
                orders.Add(string.Join(",", _question.Options));
            }

            Assert.That(orders.Count, Is.GreaterThan(1));
        }

        private void AnswerCorrectly()
        {
            Question original = _questions.Single(question => question.Id == _question.QuestionId);
            string correctText = original.Options[original.Answer];

            _service.SubmitAnswer(_question.Options.ToList().IndexOf(correctText));
            Advance(BotDelay);
        }
    }
}
