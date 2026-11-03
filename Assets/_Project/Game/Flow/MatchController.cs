using System;
using System.Collections;
using System.Linq;
using PovuciPotegni.Core;
using UnityEngine;

namespace PovuciPotegni.Game
{
    /// <summary>
    /// Connects one match to the arena and the screen, with the timing from docs/SCENA.md.
    /// The match service decides everything; this class only shows the results in order.
    /// </summary>
    public sealed class MatchController : MonoBehaviour
    {
        private const float IncomingSeconds = 1f;
        private const float RevealSeconds = 1.7f;
        private const float SheetDropSeconds = 0.48f;
        private const float BannerDelaySeconds = 0.12f;
        private const float CelebrateBeforeResultSeconds = 1.2f;
        private const int TickFromSecond = 5;

        private ArenaDirector _director;
        private MatchScreen _screen;
        private IStatsStore _store;
        private IClock _clock;
        private GameConfig _config;
        private Action<MatchEnded, string> _onFinished;
        private IAnalytics _analytics;
        private IReportSink _reportSink;
        private GameFeedback _feedback;
        private MatchAnalytics _matchAnalytics;
        private string _lastQuestionId;
        private int _lastTickSecond;

        private IMatchService _service;
        private BotProfile _bot;
        private QuestionShown _question;
        private MatchEnded _ended;
        private RoundResolved _lastRound;
        private bool _answered;
        private int _questionNumber;

        public bool IsWaitingForAnswer => _question != null && !_answered;

        public void Bind(
            ArenaDirector director,
            MatchScreen screen,
            IStatsStore store,
            IClock clock,
            GameConfig config,
            IAnalytics analytics,
            IReportSink reportSink,
            GameFeedback feedback,
            Action<MatchEnded, string> onFinished)
        {
            _analytics = analytics;
            _reportSink = reportSink;
            _feedback = feedback;
            director.Splashed += feedback.Fall;
            _director = director;
            _screen = screen;
            _store = store;
            _clock = clock;
            _config = config;
            _onFinished = onFinished;
        }

        public void Begin(IMatchService service, BotProfile bot, int seed)
        {
            Stop();

            _service = service;
            _bot = bot;
            _ended = null;
            _question = null;
            _answered = false;
            _questionNumber = 1;

            _service.QuestionShown += OnQuestionShown;
            _service.OpponentAnswered += OnOpponentAnswered;
            _service.JokerUsed += OnJokerUsed;
            _service.RoundResolved += OnRoundResolved;
            _service.MatchEnded += OnMatchEnded;
            _matchAnalytics = new MatchAnalytics(_analytics, _service, bot.Id);
            _matchAnalytics.TrackStart();

            _screen.BeginMatch(bot.Id);
            StartCoroutine(FirstQuestion(seed));
        }

        /// <summary>Leaves the match without recording a result.</summary>
        public void Stop()
        {
            StopAllCoroutines();

            if (_service != null)
            {
                _service.QuestionShown -= OnQuestionShown;
                _service.OpponentAnswered -= OnOpponentAnswered;
                _service.JokerUsed -= OnJokerUsed;
                _service.RoundResolved -= OnRoundResolved;
                _service.MatchEnded -= OnMatchEnded;
                _matchAnalytics?.Dispose();
                _matchAnalytics = null;
                _service = null;
            }

            _question = null;
        }

        public void Answer(int option)
        {
            if (!IsWaitingForAnswer)
            {
                return;
            }

            _answered = true;
            _screen.ShowPlayerPick(option);
            _service.SubmitAnswer(option);
        }

        public void UseJoker(JokerType joker)
        {
            if (IsWaitingForAnswer)
            {
                _service.UseJoker(joker);
            }
        }

        /// <summary>Reports the last question shown. It leaves this device's question pool at once.</summary>
        public void ReportLastQuestion()
        {
            if (string.IsNullOrEmpty(_lastQuestionId))
            {
                return;
            }

            _reportSink.ReportQuestion(_lastQuestionId);
            MatchAnalytics.TrackReported(_analytics, _lastQuestionId);
            _screen.ShowReported();
        }

        public void Next()
        {
            if (_lastRound == null || _service == null)
            {
                return;
            }

            _lastRound = null;
            _screen.HideBanner();
            _screen.HideChips();
            StartCoroutine(NextQuestion());
        }

        private void Update()
        {
            if (_service == null)
            {
                return;
            }

            double now = _clock.NowSeconds;
            _service.Tick(now);

            if (_question != null)
            {
                double remaining = Math.Max(0, _question.DeadlineSeconds - now);
                int seconds = (int)Math.Ceiling(remaining);
                _screen.ShowTimer((float)(remaining / _config.AnswerSeconds), seconds);

                if (!_answered && seconds != _lastTickSecond && seconds <= TickFromSecond && seconds > 0)
                {
                    _feedback.TimerTick();
                }

                _lastTickSecond = seconds;
            }
        }

        private IEnumerator FirstQuestion(int seed)
        {
            _screen.ShowIncoming(1);
            yield return new WaitForSeconds(IncomingSeconds);
            _service.Start(new MatchSetup(_bot, seed));
        }

        private IEnumerator NextQuestion()
        {
            _screen.ShowIncoming(_questionNumber);
            yield return new WaitForSeconds(IncomingSeconds);
            _service.Continue();
        }

        private void OnQuestionShown(QuestionShown question)
        {
            _question = question;
            _answered = false;
            _questionNumber = question.Number;
            _lastQuestionId = question.QuestionId;
            _screen.ShowQuestion(question);
        }

        private void OnOpponentAnswered(OpponentAnswered answered)
        {
            if (!_answered)
            {
                _screen.ShowOpponentAnswered();
            }
        }

        private void OnJokerUsed(JokerUsed used)
        {
            _screen.ShowJoker(used);
        }

        private void OnRoundResolved(RoundResolved round)
        {
            bool playerCorrect = round.PlayerChoice == round.CorrectIndex;
            _store.Data.RecordAnswer(_question.Category, playerCorrect);

            if (playerCorrect)
            {
                _feedback.Correct();
            }
            else
            {
                _feedback.Wrong();
            }

            QuestionShown shown = _question;
            _question = null;
            _answered = true;
            _questionNumber = shown.Number + 1;

            _screen.ShowRoundResult(round);
            StartCoroutine(PlayRound(round));
        }

        private void OnMatchEnded(MatchEnded ended)
        {
            _ended = ended;

            var lockedBefore = _config.Bots.Where(bot => !_store.Data.IsUnlocked(bot)).ToList();
            _store.Data.RecordMatch(_bot.Id, ended.Winner == Side.Player);
            _store.Save();

            foreach (BotProfile unlocked in lockedBefore.Where(_store.Data.IsUnlocked))
            {
                MatchAnalytics.TrackUnlocked(_analytics, unlocked.Id);
            }
        }

        private IEnumerator PlayRound(RoundResolved round)
        {
            yield return new WaitForSeconds(RevealSeconds);

            _screen.HideSheet();
            _screen.ShowChips();
            yield return new WaitForSeconds(SheetDropSeconds);

            bool pullDone = false;
            if (round.Steps != 0)
            {
                _feedback.Pull();
            }

            _director.PlayPull(round.Steps, () => pullDone = true);
            yield return new WaitUntil(() => pullDone);
            _screen.ShowRope(round.RopePosition);

            if (_ended == null)
            {
                yield return new WaitForSeconds(BannerDelaySeconds);
                _lastRound = round;
                _screen.ShowBanner(round);
                yield break;
            }

            _screen.HideChips();
            yield return PlayEnding();
        }

        private IEnumerator PlayEnding()
        {
            Side winner = _ended.Winner;

            if (_ended.Reason == MatchEndReason.Edge)
            {
                bool fallDone = false;
                _director.PlayFall(winner == Side.Player ? Side.Opponent : Side.Player, () => fallDone = true);
                yield return new WaitUntil(() => fallDone);
            }

            _director.PlayCelebration(winner);
            if (winner == Side.Player)
            {
                _feedback.Victory();
            }

            yield return new WaitForSeconds(CelebrateBeforeResultSeconds);

            MatchEnded ended = _ended;
            string botId = _bot.Id;
            Stop();
            _onFinished(ended, botId);
        }
    }
}
