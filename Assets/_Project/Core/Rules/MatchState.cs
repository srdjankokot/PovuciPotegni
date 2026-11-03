namespace PovuciPotegni.Core
{
    /// <summary>Immutable match state; <see cref="MatchRules"/> produces the next one.</summary>
    public sealed class MatchState
    {
        public RopePosition Rope { get; }
        public int QuestionsPlayed { get; }

        /// <summary>Consecutive questions both sides answered correctly; each one makes the next question harder.</summary>
        public int BothCorrectStreak { get; }

        public bool IsOvertime { get; }
        public JokerState PlayerJokers { get; }
        public JokerState OpponentJokers { get; }
        public Side Winner { get; }
        public MatchEndReason EndReason { get; }

        public bool IsFinished => Winner != Side.None;

        public static MatchState Initial => new MatchState();

        public MatchState(
            int rope = 0,
            int questionsPlayed = 0,
            int bothCorrectStreak = 0,
            bool isOvertime = false,
            JokerState? playerJokers = null,
            JokerState? opponentJokers = null,
            Side winner = Side.None,
            MatchEndReason endReason = MatchEndReason.None)
        {
            Rope = new RopePosition(rope);
            QuestionsPlayed = questionsPlayed;
            BothCorrectStreak = bothCorrectStreak;
            IsOvertime = isOvertime;
            PlayerJokers = playerJokers ?? JokerState.Full;
            OpponentJokers = opponentJokers ?? JokerState.Full;
            Winner = winner;
            EndReason = endReason;
        }
    }
}
