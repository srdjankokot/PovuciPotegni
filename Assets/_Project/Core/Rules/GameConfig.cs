using System;
using System.Collections.Generic;

namespace PovuciPotegni.Core
{
    /// <summary>
    /// All numeric game values in one place (docs/GDD.md). Public fields keep it serializable by Unity,
    /// so GameConfigAsset can expose it in the Inspector without this assembly referencing the engine.
    /// </summary>
    [Serializable]
    public sealed class GameConfig
    {
        // Match
        public int MaxQuestions = 15;
        public int RopeEdge = 3;
        public double AnswerSeconds = 15;
        public int DoublePullSteps = 2;

        // Difficulty curve: questions up to and including these numbers use the given difficulty; later ones are hard.
        public int LastEasyQuestion = 5;
        public int LastMediumQuestion = 10;

        // Bot accuracy modifiers, as fractions (0.10 = 10 points).
        public double BotEasyQuestionBonus = 0.10;
        public double BotHardQuestionBonus = -0.15;
        public double BotStrongCategoryBonus = 0.10;
        public double BotWeakCategoryBonus = -0.10;

        // Questions
        public bool AllowUnverified;

        public List<BotProfile> Bots = new List<BotProfile>
        {
            new BotProfile("segrt", 0.55, 4, 8, 0, QuestionCategories.FilmTv, QuestionCategories.Science),
            new BotProfile("profesor", 0.70, 3, 6, 3, QuestionCategories.History, QuestionCategories.Sport),
            new BotProfile("lisac", 0.82, 2, 5, 10, QuestionCategories.Sport, QuestionCategories.ArtLiterature),
            new BotProfile("majstor", 0.90, 1.5, 4, 25, "", ""),
        };
    }
}
