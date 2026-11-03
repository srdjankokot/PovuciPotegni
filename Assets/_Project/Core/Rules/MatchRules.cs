using System;
using System.Collections.Generic;

namespace PovuciPotegni.Core
{
    public sealed class RoundResult
    {
        public MatchState State { get; }
        public RoundOutcome Outcome { get; }

        /// <summary>Signed rope movement after jokers: positive is toward the player.</summary>
        public int Steps { get; }

        /// <summary>The side whose Shield cancelled a pull on this question.</summary>
        public Side ShieldBlockedBy { get; }

        /// <summary>The side whose Double made this pull longer.</summary>
        public Side DoubleUsedBy { get; }

        public RoundResult(MatchState state, RoundOutcome outcome, int steps, Side shieldBlockedBy, Side doubleUsedBy)
        {
            State = state;
            Outcome = outcome;
            Steps = steps;
            ShieldBlockedBy = shieldBlockedBy;
            DoubleUsedBy = doubleUsedBy;
        }
    }

    /// <summary>All match rules from docs/GDD.md, as pure functions.</summary>
    public static class MatchRules
    {
        public const int EasyDifficulty = 1;
        public const int MediumDifficulty = 2;
        public const int HardDifficulty = 3;

        public static RoundResult ResolveRound(GameConfig config, MatchState state, bool playerCorrect, bool opponentCorrect)
        {
            if (state.IsFinished)
            {
                throw new InvalidOperationException("The match is already finished.");
            }

            RoundOutcome outcome = GetOutcome(playerCorrect, opponentCorrect);
            Side puller = GetPuller(outcome);
            Side shieldBlockedBy = Side.None;
            Side doubleUsedBy = Side.None;
            int steps = 0;

            JokerState playerJokers = state.PlayerJokers;
            JokerState opponentJokers = state.OpponentJokers;

            if (puller != Side.None)
            {
                Side defender = Opposite(puller);
                if (JokersOf(state, defender).ShieldArmed)
                {
                    shieldBlockedBy = defender;
                }
                else
                {
                    bool doubled = JokersOf(state, puller).DoubleArmed;
                    int distance = doubled ? config.DoublePullSteps : 1;
                    steps = puller == Side.Player ? distance : -distance;

                    if (doubled)
                    {
                        doubleUsedBy = puller;
                        if (puller == Side.Player)
                        {
                            playerJokers = playerJokers.WithDoubleSpent();
                        }
                        else
                        {
                            opponentJokers = opponentJokers.WithDoubleSpent();
                        }
                    }
                }
            }

            // A Shield lasts for one question, whether or not a pull came.
            playerJokers = playerJokers.WithShieldSpent();
            opponentJokers = opponentJokers.WithShieldSpent();

            RopePosition rope = state.Rope.Moved(steps, config.RopeEdge);
            int questionsPlayed = state.QuestionsPlayed + 1;
            int streak = outcome == RoundOutcome.BothCorrect ? state.BothCorrectStreak + 1 : 0;

            Side winner = Side.None;
            MatchEndReason reason = MatchEndReason.None;
            bool isOvertime = state.IsOvertime;

            if (rope.IsAtEdge(config.RopeEdge))
            {
                winner = rope.FavoredSide;
                reason = MatchEndReason.Edge;
            }
            else if (state.IsOvertime)
            {
                if (steps != 0)
                {
                    winner = rope.FavoredSide;
                    reason = MatchEndReason.Overtime;
                }
            }
            else if (questionsPlayed >= config.MaxQuestions)
            {
                winner = rope.FavoredSide;
                if (winner == Side.None)
                {
                    isOvertime = true;
                }
                else
                {
                    reason = MatchEndReason.QuestionLimit;
                }
            }

            var next = new MatchState(
                rope.Value,
                questionsPlayed,
                streak,
                isOvertime,
                playerJokers,
                opponentJokers,
                winner,
                reason);

            return new RoundResult(next, outcome, steps, shieldBlockedBy, doubleUsedBy);
        }

        /// <summary>Difficulty of the next question: the curve for its number, raised by the both-correct streak.</summary>
        public static int NextDifficulty(GameConfig config, MatchState state)
        {
            int number = state.QuestionsPlayed + 1;
            int baseDifficulty = HardDifficulty;

            if (!state.IsOvertime)
            {
                if (number <= config.LastEasyQuestion)
                {
                    baseDifficulty = EasyDifficulty;
                }
                else if (number <= config.LastMediumQuestion)
                {
                    baseDifficulty = MediumDifficulty;
                }
            }

            return Math.Min(HardDifficulty, baseDifficulty + state.BothCorrectStreak);
        }

        /// <summary>Spends a joker if it is still available. Shield and Double become armed.</summary>
        public static bool TryUseJoker(MatchState state, Side side, JokerType joker, out MatchState next)
        {
            next = state;
            if (state.IsFinished || side == Side.None || !JokersOf(state, side).IsAvailable(joker))
            {
                return false;
            }

            JokerState playerJokers = state.PlayerJokers;
            JokerState opponentJokers = state.OpponentJokers;
            if (side == Side.Player)
            {
                playerJokers = playerJokers.Used(joker);
            }
            else
            {
                opponentJokers = opponentJokers.Used(joker);
            }

            next = new MatchState(
                state.Rope.Value,
                state.QuestionsPlayed,
                state.BothCorrectStreak,
                state.IsOvertime,
                playerJokers,
                opponentJokers);
            return true;
        }

        /// <summary>Picks the wrong option that 50/50 removes.</summary>
        public static int PickFiftyFiftyRemoval(int correctIndex, int optionCount, IRandom random)
        {
            var wrongOptions = new List<int>();
            for (int i = 0; i < optionCount; i++)
            {
                if (i != correctIndex)
                {
                    wrongOptions.Add(i);
                }
            }

            return wrongOptions[random.Next(wrongOptions.Count)];
        }

        private static RoundOutcome GetOutcome(bool playerCorrect, bool opponentCorrect)
        {
            if (playerCorrect)
            {
                return opponentCorrect ? RoundOutcome.BothCorrect : RoundOutcome.PlayerOnlyCorrect;
            }

            return opponentCorrect ? RoundOutcome.OpponentOnlyCorrect : RoundOutcome.NeitherCorrect;
        }

        private static Side GetPuller(RoundOutcome outcome)
        {
            switch (outcome)
            {
                case RoundOutcome.PlayerOnlyCorrect:
                    return Side.Player;
                case RoundOutcome.OpponentOnlyCorrect:
                    return Side.Opponent;
                default:
                    return Side.None;
            }
        }

        private static Side Opposite(Side side)
        {
            return side == Side.Player ? Side.Opponent : Side.Player;
        }

        private static JokerState JokersOf(MatchState state, Side side)
        {
            return side == Side.Player ? state.PlayerJokers : state.OpponentJokers;
        }
    }
}
