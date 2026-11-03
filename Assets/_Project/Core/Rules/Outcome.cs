namespace PovuciPotegni.Core
{
    public enum Side
    {
        None,
        Player,
        Opponent,
    }

    public enum RoundOutcome
    {
        PlayerOnlyCorrect,
        OpponentOnlyCorrect,
        BothCorrect,
        NeitherCorrect,
    }

    public enum MatchEndReason
    {
        None,
        Edge,
        QuestionLimit,
        Overtime,
    }
}
