using System;
using System.Collections.Generic;
using System.Linq;

namespace PovuciPotegni.Core
{
    /// <summary>A match against a bot, running entirely on the device.</summary>
    public sealed class BotMatchService : IMatchService
    {
        private const int NoAnswer = -1;

        private enum Phase
        {
            Idle,
            Question,
            WaitingForContinue,
            Finished,
        }

        private readonly GameConfig _config;
        private readonly QuestionPicker _picker;
        private readonly IClock _clock;

        private MatchSetup _setup;
        private IRandom _random;
        private MatchState _state;
        private Phase _phase = Phase.Idle;

        private int _correctIndex;
        private int _optionCount;
        private int _removedOption;
        private double _deadline;
        private double _matchStart;

        private bool _playerAnswered;
        private int _playerChoice;
        private bool _botAnswered;
        private BotAnswer _botAnswer;
        private double _botAnswerTime;

        private int _playerCorrect;
        private int _opponentCorrect;

        public event Action<QuestionShown> QuestionShown;
        public event Action<OpponentAnswered> OpponentAnswered;
        public event Action<JokerUsed> JokerUsed;
        public event Action<RoundResolved> RoundResolved;
        public event Action<MatchEnded> MatchEnded;

        public BotMatchService(GameConfig config, QuestionPicker picker, IClock clock)
        {
            _config = config;
            _picker = picker;
            _clock = clock;
        }

        public void Start(MatchSetup setup)
        {
            _setup = setup;
            _random = new SeededRandom(setup.Seed);
            _state = MatchState.Initial;
            _playerCorrect = 0;
            _opponentCorrect = 0;
            _matchStart = _clock.NowSeconds;
            ShowNextQuestion();
        }

        public void SubmitAnswer(int optionIndex)
        {
            if (_phase != Phase.Question || _playerAnswered)
            {
                return;
            }

            bool isValidOption = optionIndex >= 0 && optionIndex < _optionCount && optionIndex != _removedOption;
            if (optionIndex != NoAnswer && !isValidOption)
            {
                return;
            }

            _playerAnswered = true;
            _playerChoice = optionIndex;

            if (_botAnswered)
            {
                ResolveRound();
            }
        }

        public bool UseJoker(JokerType joker)
        {
            if (_phase != Phase.Question || _playerAnswered)
            {
                return false;
            }

            if (!MatchRules.TryUseJoker(_state, Side.Player, joker, out MatchState next))
            {
                return false;
            }

            _state = next;

            int removed = NoAnswer;
            if (joker == JokerType.FiftyFifty)
            {
                removed = MatchRules.PickFiftyFiftyRemoval(_correctIndex, _optionCount, _random);
                _removedOption = removed;
            }

            JokerUsed?.Invoke(new JokerUsed(joker, removed));
            return true;
        }

        public void Continue()
        {
            if (_phase == Phase.WaitingForContinue)
            {
                ShowNextQuestion();
            }
        }

        public void Tick(double nowSeconds)
        {
            if (_phase != Phase.Question)
            {
                return;
            }

            if (!_botAnswered && nowSeconds >= _botAnswerTime && _botAnswerTime < _deadline)
            {
                _botAnswered = true;
                OpponentAnswered?.Invoke(new OpponentAnswered());
            }

            if (_playerAnswered && _botAnswered)
            {
                ResolveRound();
            }
            else if (nowSeconds >= _deadline)
            {
                if (!_playerAnswered)
                {
                    _playerChoice = NoAnswer;
                }

                ResolveRound();
            }
        }

        private void ShowNextQuestion()
        {
            int difficulty = MatchRules.NextDifficulty(_config, _state);
            Question question = _picker.Pick(difficulty, _random, _setup.Categories);

            int[] order = Enumerable.Range(0, question.Options.Count).ToArray();
            Shuffle(order);

            _optionCount = order.Length;
            _correctIndex = Array.IndexOf(order, question.Answer);
            _removedOption = NoAnswer;
            _playerAnswered = false;
            _playerChoice = NoAnswer;
            _botAnswered = false;
            _botAnswer = BotBrain.Decide(
                _config,
                _setup.Opponent,
                question.Difficulty,
                question.Category,
                _correctIndex,
                _optionCount,
                _random);

            double now = _clock.NowSeconds;
            _deadline = now + _config.AnswerSeconds;
            _botAnswerTime = now + _botAnswer.DelaySeconds;
            _phase = Phase.Question;

            IReadOnlyList<string> options = order.Select(index => question.Options[index]).ToList();
            QuestionShown?.Invoke(new QuestionShown(
                _state.QuestionsPlayed + 1,
                _state.IsOvertime,
                question.Id,
                question.Category,
                question.Difficulty,
                question.Text,
                options,
                _deadline));
        }

        private void ResolveRound()
        {
            int opponentChoice = _botAnswered ? _botAnswer.OptionIndex : NoAnswer;
            bool playerCorrect = _playerChoice == _correctIndex;
            bool opponentCorrect = opponentChoice == _correctIndex;

            _playerCorrect += playerCorrect ? 1 : 0;
            _opponentCorrect += opponentCorrect ? 1 : 0;

            RoundResult result = MatchRules.ResolveRound(_config, _state, playerCorrect, opponentCorrect);
            _state = result.State;
            _phase = _state.IsFinished ? Phase.Finished : Phase.WaitingForContinue;

            RoundResolved?.Invoke(new RoundResolved(
                _correctIndex,
                _playerChoice,
                opponentChoice,
                result.Outcome,
                result.Steps,
                _state.Rope.Value,
                result.ShieldBlockedBy,
                result.DoubleUsedBy));

            if (_state.IsFinished)
            {
                MatchEnded?.Invoke(new MatchEnded(
                    _state.Winner,
                    _state.EndReason,
                    _state.QuestionsPlayed,
                    _playerCorrect,
                    _opponentCorrect,
                    _clock.NowSeconds - _matchStart));
            }
        }

        private void Shuffle(int[] values)
        {
            for (int i = values.Length - 1; i > 0; i--)
            {
                int j = _random.Next(i + 1);
                (values[i], values[j]) = (values[j], values[i]);
            }
        }
    }
}
