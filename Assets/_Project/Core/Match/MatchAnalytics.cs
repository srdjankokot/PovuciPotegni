using System;
using System.Collections.Generic;

namespace PovuciPotegni.Core
{
    /// <summary>Turns one match's events into the analytics events from docs/ARHITEKTURA.md.</summary>
    public sealed class MatchAnalytics : IDisposable
    {
        private readonly IAnalytics _analytics;
        private readonly IMatchService _service;
        private readonly string _botId;
        private QuestionShown _question;

        public MatchAnalytics(IAnalytics analytics, IMatchService service, string botId)
        {
            _analytics = analytics;
            _service = service;
            _botId = botId;

            _service.QuestionShown += OnQuestionShown;
            _service.JokerUsed += OnJokerUsed;
            _service.RoundResolved += OnRoundResolved;
            _service.MatchEnded += OnMatchEnded;
        }

        public static void TrackReported(IAnalytics analytics, string questionId)
        {
            analytics.Track("question_reported", new Dictionary<string, object> { { "question", questionId } });
        }

        public static void TrackUnlocked(IAnalytics analytics, string botId)
        {
            analytics.Track("bot_unlocked", new Dictionary<string, object> { { "bot", botId } });
        }

        public void TrackStart()
        {
            _analytics.Track("match_start", new Dictionary<string, object> { { "bot", _botId } });
        }

        public void Dispose()
        {
            _service.QuestionShown -= OnQuestionShown;
            _service.JokerUsed -= OnJokerUsed;
            _service.RoundResolved -= OnRoundResolved;
            _service.MatchEnded -= OnMatchEnded;
        }

        private void OnQuestionShown(QuestionShown question)
        {
            _question = question;
        }

        private void OnJokerUsed(JokerUsed used)
        {
            _analytics.Track("joker_used", new Dictionary<string, object> { { "joker", used.Joker.ToString() } });
        }

        private void OnRoundResolved(RoundResolved round)
        {
            _analytics.Track("question_answered", new Dictionary<string, object>
            {
                { "category", _question.Category },
                { "difficulty", _question.Difficulty },
                { "correct", round.PlayerChoice == round.CorrectIndex },
            });
        }

        private void OnMatchEnded(MatchEnded ended)
        {
            _analytics.Track("match_end", new Dictionary<string, object>
            {
                { "bot", _botId },
                { "outcome", ended.Winner == Side.Player ? "win" : "loss" },
                { "reason", ended.Reason.ToString() },
                { "questions", ended.QuestionsPlayed },
                { "duration", ended.DurationSeconds },
            });
        }
    }
}
