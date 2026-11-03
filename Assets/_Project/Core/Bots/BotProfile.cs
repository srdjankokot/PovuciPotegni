using System;

namespace PovuciPotegni.Core
{
    /// <summary>
    /// Tunable values for one bot. The name the player sees comes from the Localization table, keyed by <see cref="Id"/>.
    /// </summary>
    [Serializable]
    public sealed class BotProfile
    {
        public string Id = "";
        public double Accuracy;
        public double MinAnswerSeconds;
        public double MaxAnswerSeconds;
        public int WinsToUnlock;

        /// <summary>Category key from <see cref="QuestionCategories"/>; empty when the bot has none.</summary>
        public string StrongCategory = "";

        /// <summary>Category key from <see cref="QuestionCategories"/>; empty when the bot has none.</summary>
        public string WeakCategory = "";

        public BotProfile()
        {
        }

        public BotProfile(
            string id,
            double accuracy,
            double minAnswerSeconds,
            double maxAnswerSeconds,
            int winsToUnlock,
            string strongCategory,
            string weakCategory)
        {
            Id = id;
            Accuracy = accuracy;
            MinAnswerSeconds = minAnswerSeconds;
            MaxAnswerSeconds = maxAnswerSeconds;
            WinsToUnlock = winsToUnlock;
            StrongCategory = strongCategory;
            WeakCategory = weakCategory;
        }
    }
}
