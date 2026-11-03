using System;

namespace PovuciPotegni.Core
{
    /// <summary>The only way the game talks to a match. It does not know whether the opponent is a bot or a person.</summary>
    public interface IMatchService
    {
        event Action<QuestionShown> QuestionShown;
        event Action<OpponentAnswered> OpponentAnswered;
        event Action<JokerUsed> JokerUsed;
        event Action<RoundResolved> RoundResolved;
        event Action<MatchEnded> MatchEnded;

        void Start(MatchSetup setup);

        /// <param name="optionIndex">Index in the shown order; -1 means time ran out.</param>
        void SubmitAnswer(int optionIndex);

        bool UseJoker(JokerType joker);

        /// <summary>Asks for the next question once the round's animation has finished, so its timer starts then.</summary>
        void Continue();

        void Tick(double nowSeconds);
    }
}
