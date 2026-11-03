using System;

namespace PovuciPotegni.Core
{
    public readonly struct BotAnswer
    {
        public int OptionIndex { get; }
        public double DelaySeconds { get; }

        public BotAnswer(int optionIndex, double delaySeconds)
        {
            OptionIndex = optionIndex;
            DelaySeconds = delaySeconds;
        }
    }

    /// <summary>Decides a bot's answer. Accuracy decides the outcome; the delay only makes the bot look natural.</summary>
    public static class BotBrain
    {
        public static double CorrectProbability(GameConfig config, BotProfile bot, int difficulty, string category)
        {
            double probability = bot.Accuracy;

            if (difficulty == MatchRules.EasyDifficulty)
            {
                probability += config.BotEasyQuestionBonus;
            }
            else if (difficulty == MatchRules.HardDifficulty)
            {
                probability += config.BotHardQuestionBonus;
            }

            if (!string.IsNullOrEmpty(category))
            {
                if (category == bot.StrongCategory)
                {
                    probability += config.BotStrongCategoryBonus;
                }
                else if (category == bot.WeakCategory)
                {
                    probability += config.BotWeakCategoryBonus;
                }
            }

            return Math.Max(0, Math.Min(1, probability));
        }

        public static BotAnswer Decide(
            GameConfig config,
            BotProfile bot,
            int difficulty,
            string category,
            int correctIndex,
            int optionCount,
            IRandom random)
        {
            int optionIndex = correctIndex;
            if (random.NextDouble() >= CorrectProbability(config, bot, difficulty, category))
            {
                int wrongChoice = random.Next(optionCount - 1);
                optionIndex = wrongChoice >= correctIndex ? wrongChoice + 1 : wrongChoice;
            }

            double range = bot.MaxAnswerSeconds - bot.MinAnswerSeconds;
            return new BotAnswer(optionIndex, bot.MinAnswerSeconds + random.NextDouble() * range);
        }
    }
}
