using System.Collections.Generic;

namespace PovuciPotegni.Core
{
    public sealed class MatchSetup
    {
        public BotProfile Opponent { get; }
        public int Seed { get; }

        /// <summary>Allowed category keys; null or empty allows all.</summary>
        public IReadOnlyCollection<string> Categories { get; }

        public MatchSetup(BotProfile opponent, int seed, IReadOnlyCollection<string> categories = null)
        {
            Opponent = opponent;
            Seed = seed;
            Categories = categories;
        }
    }

    /// <summary>A question without its correct answer. Options are already shuffled; all indexes refer to this order.</summary>
    public sealed class QuestionShown
    {
        public int Number { get; }
        public bool IsOvertime { get; }
        public string QuestionId { get; }
        public string Category { get; }
        public int Difficulty { get; }
        public string Text { get; }
        public IReadOnlyList<string> Options { get; }
        public double DeadlineSeconds { get; }

        public QuestionShown(
            int number,
            bool isOvertime,
            string questionId,
            string category,
            int difficulty,
            string text,
            IReadOnlyList<string> options,
            double deadlineSeconds)
        {
            Number = number;
            IsOvertime = isOvertime;
            QuestionId = questionId;
            Category = category;
            Difficulty = difficulty;
            Text = text;
            Options = options;
            DeadlineSeconds = deadlineSeconds;
        }
    }

    /// <summary>Says only that the opponent answered, never what.</summary>
    public sealed class OpponentAnswered
    {
    }

    public sealed class JokerUsed
    {
        public JokerType Joker { get; }

        /// <summary>The option 50/50 removed; -1 for other jokers.</summary>
        public int RemovedOptionIndex { get; }

        public JokerUsed(JokerType joker, int removedOptionIndex)
        {
            Joker = joker;
            RemovedOptionIndex = removedOptionIndex;
        }
    }

    public sealed class RoundResolved
    {
        public int CorrectIndex { get; }

        /// <summary>The player's choice; -1 when time ran out.</summary>
        public int PlayerChoice { get; }

        /// <summary>The opponent's choice; -1 when time ran out.</summary>
        public int OpponentChoice { get; }

        public RoundOutcome Outcome { get; }

        /// <summary>Signed rope movement: positive is toward the player.</summary>
        public int Steps { get; }

        public int RopePosition { get; }
        public Side ShieldBlockedBy { get; }
        public Side DoubleUsedBy { get; }

        public RoundResolved(
            int correctIndex,
            int playerChoice,
            int opponentChoice,
            RoundOutcome outcome,
            int steps,
            int ropePosition,
            Side shieldBlockedBy,
            Side doubleUsedBy)
        {
            CorrectIndex = correctIndex;
            PlayerChoice = playerChoice;
            OpponentChoice = opponentChoice;
            Outcome = outcome;
            Steps = steps;
            RopePosition = ropePosition;
            ShieldBlockedBy = shieldBlockedBy;
            DoubleUsedBy = doubleUsedBy;
        }
    }

    public sealed class MatchEnded
    {
        public Side Winner { get; }
        public MatchEndReason Reason { get; }
        public int QuestionsPlayed { get; }
        public int PlayerCorrect { get; }
        public int OpponentCorrect { get; }
        public double DurationSeconds { get; }

        public MatchEnded(
            Side winner,
            MatchEndReason reason,
            int questionsPlayed,
            int playerCorrect,
            int opponentCorrect,
            double durationSeconds)
        {
            Winner = winner;
            Reason = reason;
            QuestionsPlayed = questionsPlayed;
            PlayerCorrect = playerCorrect;
            OpponentCorrect = opponentCorrect;
            DurationSeconds = durationSeconds;
        }
    }
}
